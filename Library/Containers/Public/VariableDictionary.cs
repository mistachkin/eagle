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

#if SERIALIZATION || DEAD_CODE
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

using VariablePair = System.Collections.Generic.KeyValuePair<
    string, Eagle._Interfaces.Public.IVariable>;

#if FAST_DICTIONARY
using SomeDictionary = Eagle._Containers.Public.FastDictionary<
    string, Eagle._Interfaces.Public.IVariable>;
#else
using SomeDictionary = System.Collections.Generic.Dictionary<
    string, Eagle._Interfaces.Public.IVariable>;
#endif

#if NET_STANDARD_21
using Index = Eagle._Constants.Index;
#endif

namespace Eagle._Containers.Public
{
    /// <summary>
    /// This class represents a dictionary that maps variable names to variables
    /// (<see cref="IVariable" />); it backs the variable storage for a call
    /// frame.
    /// </summary>
#if SERIALIZATION
    [Serializable()]
#endif
    [ObjectId("c1e0a819-c899-4d10-92ff-fea8b14841df")]
    public sealed class VariableDictionary : SomeDictionary
    {
        #region Public Constructors
        /// <summary>
        /// Constructs an empty dictionary of variables.
        /// </summary>
        public VariableDictionary()
            : base()
        {
            // do nothing.
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Constructs a dictionary of variables that contains the entries copied
        /// from the specified dictionary.
        /// </summary>
        /// <param name="dictionary">
        /// The dictionary whose entries are copied into the new dictionary.
        /// </param>
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
        /// <summary>
        /// This method creates a new dictionary of variables by cloning the
        /// entries of an existing dictionary, subject to the specified clone
        /// flags.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter used when cloning variables and determining whether
        /// a variable is special.  This parameter may be null.
        /// </param>
        /// <param name="oldDictionary">
        /// The dictionary whose variables are cloned into the new dictionary.
        /// This parameter may be null.
        /// </param>
        /// <param name="flags">
        /// The flags controlling how the variables are cloned.
        /// </param>
        /// <param name="error">
        /// Upon failure, receives information about the error.
        /// </param>
        /// <returns>
        /// The new dictionary on success; otherwise, null.
        /// </returns>
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
        /// <summary>
        /// Constructs a dictionary of variables from previously serialized
        /// data.
        /// </summary>
        /// <param name="info">
        /// The object that holds the serialized data for the dictionary.
        /// </param>
        /// <param name="context">
        /// The streaming context describing the source and destination of the
        /// serialized data.
        /// </param>
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
        /// <summary>
        /// This method returns a string representation of the variable names in
        /// this dictionary, with the names separated by spaces.
        /// </summary>
        /// <param name="pattern">
        /// The optional pattern used to filter the variable names included in
        /// the result.  This parameter may be null.
        /// </param>
        /// <param name="noCase">
        /// Non-zero if pattern matching should be performed in a
        /// case-insensitive manner.
        /// </param>
        /// <returns>
        /// The string representation of the variable names in this dictionary.
        /// </returns>
        public string ToString(
            string pattern,
            bool noCase
            )
        {
            StringList list = new StringList(this.Keys);

            return ParserOps<string>.ListToString(
                list, Index.Invalid, Index.Invalid, ToStringFlags.None,
                Characters.SpaceString, pattern, noCase);
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Private Methods
        /// <summary>
        /// This method copies the variables from the specified dictionary into
        /// this dictionary, cloning each variable and optionally skipping
        /// special variables.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter used when cloning variables and determining whether
        /// a variable is special.  This parameter may be null.
        /// </param>
        /// <param name="dictionary">
        /// The dictionary whose variables are copied into this dictionary.
        /// This parameter may be null.
        /// </param>
        /// <param name="flags">
        /// The flags controlling how the variables are cloned.
        /// </param>
        /// <param name="error">
        /// Upon failure, receives information about the error.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise, an error code.
        /// </returns>
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

                foreach (VariablePair pair in dictionary)
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

        /// <summary>
        /// This method adds a variable to this dictionary, or updates the
        /// existing variable that has the specified name, cloning or copying the
        /// supplied variable as needed.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter used when cloning variables and determining whether
        /// a variable is special.  This parameter may be null.
        /// </param>
        /// <param name="varName">
        /// The name of the variable to add or update.  This parameter may be
        /// null, in which case an error is returned.
        /// </param>
        /// <param name="variable">
        /// The variable whose value is added or used to update the existing
        /// variable.  This parameter may be null.
        /// </param>
        /// <param name="frame">
        /// The call frame to associate with a newly cloned variable.  This
        /// parameter may be null.
        /// </param>
        /// <param name="flags">
        /// The flags controlling how the variable is cloned or copied.
        /// </param>
        /// <param name="error">
        /// Upon failure, receives information about the error.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise, an error code.
        /// </returns>
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

        /// <summary>
        /// This method returns the names of the usable, defined variables in
        /// this dictionary whose read-only state matches the specified value
        /// and whose names match the optional pattern.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter used when matching variable names against the
        /// pattern.  This parameter may be null.
        /// </param>
        /// <param name="pattern">
        /// The optional pattern used to filter the variable names.  This
        /// parameter may be null.
        /// </param>
        /// <param name="readOnly">
        /// The read-only state that a variable must have to be included.
        /// </param>
        /// <returns>
        /// The list of matching variable names.
        /// </returns>
        internal StringList GetReadOnly(
            Interpreter interpreter,
            string pattern,
            bool readOnly
            )
        {
            StringList result = new StringList();

            foreach (VariablePair pair in this)
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

        /// <summary>
        /// This method sets the read-only state of the usable, defined
        /// variables in this dictionary whose names match the optional pattern.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter used when matching variable names against the
        /// pattern.  This parameter may be null.
        /// </param>
        /// <param name="pattern">
        /// The optional pattern used to filter the variable names.  This
        /// parameter may be null.
        /// </param>
        /// <param name="readOnly">
        /// The read-only state to set on the matching variables.
        /// </param>
        /// <returns>
        /// The number of variables whose read-only state was changed.
        /// </returns>
        internal int SetReadOnly(
            Interpreter interpreter,
            string pattern,
            bool readOnly
            )
        {
            int result = 0;

            foreach (VariablePair pair in this)
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

        /// <summary>
        /// This method returns the number of defined variables in this
        /// dictionary.
        /// </summary>
        /// <returns>
        /// The number of defined variables in this dictionary.
        /// </returns>
        internal int GetDefinedCount()
        {
            int result = 0;

            foreach (VariablePair pair in this)
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

        /// <summary>
        /// This method returns the names of the defined variables in this
        /// dictionary whose names match the optional pattern.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter used when matching variable names against the
        /// pattern.  This parameter may be null.
        /// </param>
        /// <param name="pattern">
        /// The optional pattern used to filter the variable names.  This
        /// parameter may be null.
        /// </param>
        /// <returns>
        /// The list of matching defined variable names.
        /// </returns>
        internal StringList GetDefined(
            Interpreter interpreter,
            string pattern
            )
        {
            StringList result = new StringList();

            foreach (VariablePair pair in this)
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

        /// <summary>
        /// This method sets the undefined state of the usable variables in this
        /// dictionary whose names match the optional pattern.  This method is
        /// exempt from the normal requirement that the variables be defined.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter used when matching variable names against the
        /// pattern.  This parameter may be null.
        /// </param>
        /// <param name="pattern">
        /// The optional pattern used to filter the variable names.  This
        /// parameter may be null.
        /// </param>
        /// <param name="undefined">
        /// The undefined state to set on the matching variables.
        /// </param>
        /// <returns>
        /// The number of variables whose undefined state was changed.
        /// </returns>
        internal int SetUndefined(
            Interpreter interpreter,
            string pattern,
            bool undefined
            )
        {
            int result = 0;

            foreach (VariablePair pair in this)
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

        /// <summary>
        /// This method returns the names of the local variables in this
        /// dictionary whose names match the optional pattern, excluding
        /// undefined variables, links, and variables belonging to the global or
        /// a namespace call frame.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter used when matching variable names against the
        /// pattern and identifying global and namespace call frames.  This
        /// parameter may be null.
        /// </param>
        /// <param name="pattern">
        /// The optional pattern used to filter the variable names.  This
        /// parameter may be null.
        /// </param>
        /// <returns>
        /// The list of matching local variable names.
        /// </returns>
        internal StringList GetLocals(
            Interpreter interpreter,
            string pattern
            )
        {
            if (pattern != null)
                pattern = ScriptOps.MakeVariableName(pattern);

            StringList result = new StringList();

            foreach (VariablePair pair in this)
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

        /// <summary>
        /// This method returns a list describing the variables in this
        /// dictionary that have watchpoint flags set; each entry is a two
        /// element sub-list of the variable name and its watch types.
        /// </summary>
        /// <returns>
        /// The list of variable names and their watch types.
        /// </returns>
        internal StringList GetWatchpoints()
        {
            StringList result = new StringList();

            foreach (VariablePair pair in this)
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
        /// <summary>
        /// This method creates a new dictionary of variables that is a copy of
        /// this dictionary.
        /// </summary>
        /// <returns>
        /// The new dictionary that is a copy of this dictionary.
        /// </returns>
        private VariableDictionary Copy()
        {
            //
            // BUGBUG: If/when this code is eventually used, is this the
            //         right copying method here?
            //
            return new VariableDictionary(this);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method removes from the old dictionary the variables whose
        /// names are not present in the new dictionary.
        /// </summary>
        /// <param name="oldDictionary">
        /// The dictionary from which missing variables are removed.  This
        /// parameter may be null.
        /// </param>
        /// <param name="newDictionary">
        /// The dictionary whose keys determine which variables are retained.
        /// This parameter may be null.
        /// </param>
        /// <param name="removed">
        /// Receives the running count of variables removed, incremented by the
        /// number removed by this call.
        /// </param>
        private void Remove(
            IDictionary<string, IVariable> oldDictionary, /* in */
            IDictionary<string, IVariable> newDictionary, /* in */
            ref int removed                               /* in, out */
            )
        {
            if ((oldDictionary == null) || (newDictionary == null))
                return;

            StringList varNames = null;

            foreach (VariablePair pair in oldDictionary)
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

        /// <summary>
        /// This method clears this dictionary and then replaces its contents
        /// with the entries from the specified dictionary.
        /// </summary>
        /// <param name="dictionary">
        /// The dictionary whose entries are committed into this dictionary.
        /// This parameter may be null.
        /// </param>
        private void Commit(
            IDictionary<string, IVariable> dictionary /* in, out */
            )
        {
            if (dictionary == null)
                return;

            Clear();

            foreach (VariablePair pair in dictionary)
                Add(pair.Key, pair.Value);
        }

        ///////////////////////////////////////////////////////////////////////

        //
        // TODO: Use this method to implement a SetVariableValues method that
        //       uses transactional semantics.  Figure out how to handle any
        //       variables that use non-standard variable traces.
        //
        /// <summary>
        /// This method merges the variables from the specified dictionary into
        /// this dictionary, optionally overwriting existing variables, removing
        /// missing variables, and treating existence or non-existence as an
        /// error.  The merge is performed on a working copy that is committed
        /// only if it succeeds.
        /// </summary>
        /// <param name="dictionary">
        /// The dictionary whose variables are merged into this dictionary.
        /// This parameter may be null, in which case an error is returned.
        /// </param>
        /// <param name="overwriteOld">
        /// Non-zero to overwrite variables that already exist in this
        /// dictionary.
        /// </param>
        /// <param name="removeMissing">
        /// Non-zero to remove variables from this dictionary that are not
        /// present in the supplied dictionary.
        /// </param>
        /// <param name="errorOnExist">
        /// Non-zero to return an error if a variable being merged already
        /// exists.
        /// </param>
        /// <param name="errorOnNotExist">
        /// Non-zero to return an error if a variable being merged does not
        /// already exist.
        /// </param>
        /// <param name="added">
        /// Receives the running count of variables added, incremented by the
        /// number added by this call.
        /// </param>
        /// <param name="changed">
        /// Receives the running count of variables changed, incremented by the
        /// number changed by this call.
        /// </param>
        /// <param name="removed">
        /// Receives the running count of variables removed, incremented by the
        /// number removed by this call.
        /// </param>
        /// <param name="error">
        /// Upon failure, receives information about the error.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise, an error code.
        /// </returns>
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

            foreach (VariablePair pair in dictionary)
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
        /// <summary>
        /// This method returns a string representation of the variable names in
        /// this dictionary, with the names separated by spaces.
        /// </summary>
        /// <returns>
        /// The string representation of the variable names in this dictionary.
        /// </returns>
        public override string ToString()
        {
            return ToString(null, false);
        }
        #endregion
    }
}
