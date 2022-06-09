/*
 * VariableDictionary.cs --
 *
 * Copyright (c) 2007-2012 by Joe Mistachkin.  All rights reserved.
 *
 * See the file "license.terms" for information on usage and redistribution of
 * this file, and for a DISCLAIMER OF ALL WARRANTIES.
 *
 * RCS: @(#) $Id: $
 */

#if SERIALIZATION
using System;
#endif

using System.Collections.Generic;

#if SERIALIZATION
using System.Runtime.Serialization;
#endif

using System.Threading;
using Eagle._Attributes;
using Eagle._Components.Private;
using Eagle._Components.Public;
using Eagle._Constants;
using Eagle._Interfaces.Public;

#if NET_STANDARD_21
using Index = Eagle._Constants.Index;
#endif

namespace Eagle._Containers.Public
{
#if SERIALIZATION
    [Serializable()]
#endif
    [ObjectId("c1e0a819-c899-4d10-92ff-fea8b14841df")]
    public sealed class VariableDictionary : Dictionary<string, IVariable>
    {
        #region Public Constructors
        public VariableDictionary()
            : base()
        {
            // do nothing.
        }

        ///////////////////////////////////////////////////////////////////////

        public VariableDictionary(
            IDictionary<string, IVariable> dictionary
            )
            : base(dictionary)
        {
            // do nothing.
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Static "Factory" Methods
        internal static VariableDictionary Create(
            Interpreter interpreter,
            IDictionary<string, IVariable> oldDictionary,
            CloneFlags flags,
            ref Result error
            )
        {
            VariableDictionary newDictionary =
                new VariableDictionary();

            if (newDictionary.MaybeCopyFrom(
                    interpreter, oldDictionary, flags,
                    ref error) == ReturnCode.Ok)
            {
                return newDictionary;
            }

            return null;
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Protected Constructors
#if SERIALIZATION
        private VariableDictionary(
            SerializationInfo info,
            StreamingContext context
            )
            : base(info, context)
        {
            // do nothing.
        }
#endif
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Public Methods
        public string ToString(
            string pattern,
            bool noCase
            )
        {
            StringList list = new StringList(this.Keys);

            return ParserOps<string>.ListToString(
                list, Index.Invalid, Index.Invalid, ToStringFlags.None,
                Characters.Space.ToString(), pattern, noCase);
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Private Methods
        private ReturnCode MaybeCopyFrom(
            Interpreter interpreter,
            IDictionary<string, IVariable> dictionary,
            CloneFlags flags,
            ref Result error
            )
        {
            if (dictionary != null)
            {
                bool allowSpecial = FlagOps.HasFlags(
                    flags, CloneFlags.AllowSpecial, true);

                foreach (KeyValuePair<string, IVariable> pair in dictionary)
                {
                    IVariable variable = pair.Value;

                    if (variable != null)
                    {
                        if (!allowSpecial && (interpreter != null) &&
                            interpreter.IsSpecialVariable(variable))
                        {
                            continue;
                        }

                        variable = variable.Clone(
                            interpreter, flags, ref error);

                        if (variable == null)
                            return ReturnCode.Error;
                    }

                    this[pair.Key] = variable;
                }
            }

            return ReturnCode.Ok;
        }

        ///////////////////////////////////////////////////////////////////////

        internal ReturnCode AddOrUpdate(
            Interpreter interpreter,
            string varName,
            IVariable variable,
            ICallFrame frame,
            CloneFlags flags,
            ref Result error
            )
        {
            if (varName == null)
            {
                error = "invalid variable name";
                return ReturnCode.Error;
            }

            bool allowSpecial = FlagOps.HasFlags(
                flags, CloneFlags.AllowSpecial, true);

            if (!allowSpecial && (interpreter != null) &&
                interpreter.IsSpecialVariable(variable))
            {
                error = "special variable cannot be used";
                return ReturnCode.Error;
            }

            IVariable localVariable;

            if (TryGetValue(varName, out localVariable))
            {
                if (localVariable != null)
                {
                    if (localVariable.CopyValueFrom(
                            interpreter, variable, flags,
                            ref error) == ReturnCode.Error)
                    {
                        return ReturnCode.Error;
                    }
                }
                else if (variable != null)
                {
                    localVariable = variable.Clone(
                        interpreter, flags, ref error);

                    if (localVariable == null)
                        return ReturnCode.Error;

                    EntityOps.ResetCallFrame(
                        interpreter, localVariable, frame);

                    this[varName] = localVariable;
                }
                else
                {
                    this[varName] = null;
                }
            }
            else if (variable != null)
            {
                localVariable = variable.Clone(
                    interpreter, flags, ref error);

                if (localVariable == null)
                    return ReturnCode.Error;

                EntityOps.ResetCallFrame(
                    interpreter, localVariable, frame);

                Add(varName, localVariable);
            }
            else
            {
                Add(varName, null);
            }

            return ReturnCode.Ok;
        }

        ///////////////////////////////////////////////////////////////////////

        internal StringList GetReadOnly(
            Interpreter interpreter,
            string pattern,
            bool readOnly
            )
        {
            StringList result = new StringList();

            foreach (KeyValuePair<string, IVariable> pair in this)
            {
                IVariable variable = pair.Value;

                if (variable == null)
                    continue;

                if (!variable.IsUsable())
                    continue;

                if (EntityOps.IsUndefined(variable))
                    continue;

                string name = variable.Name;

                if ((pattern != null) && !StringOps.Match(
                        interpreter, StringOps.DefaultMatchMode,
                        name, pattern, false))
                {
                    continue;
                }

                if (EntityOps.IsReadOnly(variable) == readOnly)
                    result.Add(name);
            }

            return result;
        }

        ///////////////////////////////////////////////////////////////////////

        internal int SetReadOnly(
            Interpreter interpreter,
            string pattern,
            bool readOnly
            )
        {
            int result = 0;

            foreach (KeyValuePair<string, IVariable> pair in this)
            {
                IVariable variable = pair.Value;

                if (variable == null)
                    continue;

                if (!variable.IsUsable())
                    continue;

                if (EntityOps.IsUndefined(variable))
                    continue;

                string name = variable.Name;

                if ((pattern != null) && !StringOps.Match(
                        interpreter, StringOps.DefaultMatchMode,
                        name, pattern, false))
                {
                    continue;
                }

                if (EntityOps.IsReadOnly(variable) == readOnly)
                    continue;

                if (EntityOps.SetReadOnly(variable, readOnly))
                    result++;
            }

            return result;
        }

        ///////////////////////////////////////////////////////////////////////

        internal int GetDefinedCount()
        {
            int result = 0;

            foreach (KeyValuePair<string, IVariable> pair in this)
            {
                IVariable variable = pair.Value;

                if (variable == null)
                    continue;

                if (EntityOps.IsUndefined(variable))
                    continue;

                result++;
            }

            return result;
        }

        ///////////////////////////////////////////////////////////////////////

        internal StringList GetDefined(
            Interpreter interpreter,
            string pattern
            )
        {
            StringList result = new StringList();

            foreach (KeyValuePair<string, IVariable> pair in this)
            {
                IVariable variable = pair.Value;

                if (variable == null)
                    continue;

                if (EntityOps.IsUndefined(variable))
                    continue;

                string name = variable.Name;

                if ((pattern == null) || StringOps.Match(
                        interpreter, StringOps.DefaultMatchMode,
                        name, pattern, false))
                {
                    result.Add(name);
                }
            }

            return result;
        }

        ///////////////////////////////////////////////////////////////////////

        internal int SetUndefined(
            Interpreter interpreter,
            string pattern,
            bool undefined
            )
        {
            int result = 0;

            foreach (KeyValuePair<string, IVariable> pair in this)
            {
                IVariable variable = pair.Value;

                if (variable == null)
                    continue;

                if (!variable.IsUsable())
                    continue;

                //
                // NOTE: This method is EXEMPT from the normal requirement
                //       that all the variables operated on must be defined.
                //
                // if (EntityOps.IsUndefined(variable))
                //     continue;

                string name = variable.Name;

                if ((pattern == null) || StringOps.Match(
                        interpreter, StringOps.DefaultMatchMode,
                        name, pattern, false))
                {
                    if (EntityOps.IsUndefined(variable) == undefined)
                        continue;

                    if (EntityOps.SetUndefined(variable, undefined))
                    {
                        result++;
                    }
                }
            }

            return result;
        }

        ///////////////////////////////////////////////////////////////////////

        internal StringList GetLocals(
            Interpreter interpreter,
            string pattern
            )
        {
            if (pattern != null)
                pattern = ScriptOps.MakeVariableName(pattern);

            StringList result = new StringList();

            foreach (KeyValuePair<string, IVariable> pair in this)
            {
                IVariable variable = pair.Value;

                if (variable == null)
                    continue;

                if (EntityOps.IsUndefined(variable) ||
                    EntityOps.IsLink(variable))
                {
                    continue;
                }

                ICallFrame frame = CallFrameOps.FollowNext(variable.Frame);

                if (interpreter != null)
                {
                    if (interpreter.IsGlobalCallFrame(frame))
                        continue;

                    if (Interpreter.IsNamespaceCallFrame(frame))
                        continue;
                }

                string name = variable.Name;

                if ((pattern == null) || StringOps.Match(
                        interpreter, StringOps.DefaultMatchMode,
                        name, pattern, false))
                {
                    result.Add(name);
                }
            }

            return result;
        }

        ///////////////////////////////////////////////////////////////////////

        internal StringList GetWatchpoints()
        {
            StringList result = new StringList();

            foreach (KeyValuePair<string, IVariable> pair in this)
            {
                IVariable variable = pair.Value;

                if (variable == null)
                    continue;

                VariableFlags flags = EntityOps.GetWatchpointFlags(
                    variable.Flags);

                if (flags != VariableFlags.None)
                {
                    //
                    // NOTE: Two element sub-list of name and watch types.
                    //
                    result.Add(new StringList(
                        variable.Name, flags.ToString()).ToString());
                }
            }

            return result;
        }

        ///////////////////////////////////////////////////////////////////////

        #region Dead Code
#if DEAD_CODE
        private VariableDictionary Copy()
        {
            //
            // BUGBUG: If/when this code is eventually used, is this the
            //         right copying method here?
            //
            return new VariableDictionary(this);
        }

        ///////////////////////////////////////////////////////////////////////

        private void Remove(
            IDictionary<string, IVariable> oldDictionary, /* in */
            IDictionary<string, IVariable> newDictionary, /* in */
            ref int removed                               /* in, out */
            )
        {
            if ((oldDictionary == null) || (newDictionary == null))
                return;

            StringList varNames = null;

            foreach (KeyValuePair<string, IVariable> pair in oldDictionary)
            {
                string varName = pair.Key;

                if (varName == null) /* IMPOSSIBLE (?) */
                    continue;

                if (!newDictionary.ContainsKey(varName))
                {
                    if (varNames == null)
                        varNames = new StringList();

                    varNames.Add(varName);
                }
            }

            if (varNames != null)
            {
                int localRemoved = 0;

                foreach (string varName in varNames)
                {
                    if (varName == null) /* IMPOSSIBLE (?) */
                        continue;

                    if (oldDictionary.Remove(varName))
                        localRemoved++;
                }

                removed += localRemoved;
            }
        }

        ///////////////////////////////////////////////////////////////////////

        private void Commit(
            IDictionary<string, IVariable> dictionary /* in, out */
            )
        {
            if (dictionary == null)
                return;

            Clear();

            foreach (KeyValuePair<string, IVariable> pair in dictionary)
                Add(pair.Key, pair.Value);
        }

        ///////////////////////////////////////////////////////////////////////

        //
        // TODO: Use this method to implement a SetVariableValues method that
        //       uses transactional semantics.  Figure out how to handle any
        //       variables that use non-standard variable traces.
        //
        internal ReturnCode Merge(
            IDictionary<string, IVariable> dictionary, /* in */
            bool overwriteOld,                         /* in */
            bool removeMissing,                        /* in */
            bool errorOnExist,                         /* in */
            bool errorOnNotExist,                      /* in */
            ref int added,                             /* in, out */
            ref int changed,                           /* in, out */
            ref int removed,                           /* in, out */
            ref Result error                           /* out */
            )
        {
            if (dictionary == null)
            {
                error = "invalid dictionary";
                return ReturnCode.Error;
            }

            VariableDictionary localDictionary = Copy();
            int localRemoved = 0;

            if (removeMissing)
                Remove(localDictionary, dictionary, ref localRemoved);

            int localAdded = 0;
            int localChanged = 0;

            foreach (KeyValuePair<string, IVariable> pair in dictionary)
            {
                string varName = pair.Key;

                if (varName == null) /* IMPOSSIBLE (?) */
                    continue;

                bool? add = null; /* DO NOTHING */

                if (localDictionary.ContainsKey(varName))
                {
                    if (errorOnExist)
                    {
                        error = String.Format(
                            "can't merge: variable {0} already exists",
                            FormatOps.WrapOrNull(varName));

                        return ReturnCode.Error;
                    }
                    else if (overwriteOld)
                    {
                        add = false;
                    }
                }
                else if (errorOnNotExist)
                {
                    error = String.Format(
                        "can't merge: variable {0} does not exist",
                        FormatOps.WrapOrNull(varName));

                    return ReturnCode.Error;
                }
                else
                {
                    add = true;
                }

                if (add == null)
                    continue;

                IVariable variable = pair.Value;

                if ((bool)add)
                {
                    localDictionary.Add(varName, variable);
                    localAdded++;
                }
                else
                {
                    localDictionary[varName] = variable;
                    localChanged++;
                }
            }

            Commit(localDictionary);

            added += localAdded;
            changed += localChanged;
            removed += localRemoved;

            return ReturnCode.Ok;
        }
#endif
        #endregion
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region System.Object Overrides
        public override string ToString()
        {
            return ToString(null, false);
        }
        #endregion
    }
}
