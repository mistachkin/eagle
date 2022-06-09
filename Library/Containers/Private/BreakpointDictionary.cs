/*
 * BreakpointDictionary.cs --
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

using System.Text.RegularExpressions;
using Eagle._Attributes;
using Eagle._Components.Private;
using Eagle._Components.Public;
using Eagle._Constants;
using Eagle._Containers.Public;
using Eagle._Interfaces.Public;

#if NET_STANDARD_21
using Index = Eagle._Constants.Index;
#endif

namespace Eagle._Containers.Private
{
#if SERIALIZATION
    [Serializable()]
#endif
    [ObjectId("7e6e8490-b7eb-41c7-add8-75de1e3d87c0")]
    internal sealed class BreakpointDictionary :
            PathDictionary<ScriptLocationIntDictionary>,
            IDictionary<string, ScriptLocationIntDictionary>
    {
        #region Public Constructors
        public BreakpointDictionary()
            : base()
        {
            // do nothing.
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        #region Dead Code
#if DEAD_CODE
        public BreakpointDictionary(
            IEqualityComparer<string> comparer
            )
            : base(comparer)
        {
            // do nothing.
        }
#endif
        #endregion
        #endregion

        ///////////////////////////////////////////////////////////////////////////////////////////////

        #region Protected Constructors
#if SERIALIZATION
        private BreakpointDictionary(
            SerializationInfo info,
            StreamingContext context
            )
            : base(info, context)
        {
            // do nothing.
        }
#endif
        #endregion

        ///////////////////////////////////////////////////////////////////////////////////////////////

        #region Public Methods
        #region Dead Code
#if DEAD_CODE
        bool IDictionary<string, ScriptLocationIntDictionary>.TryGetValue( /* NOT USED */
            string key,
            out ScriptLocationIntDictionary value
            )
        {
#if false
            return base.TryGetValue(key, out value);
#else
            return TryGetValue(null, key, out value);
#endif
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public new bool TryGetValue( /* NOT USED */
            string key,
            out ScriptLocationIntDictionary value
            )
        {
#if false
            return base.TryGetValue(key, out value);
#else
            return TryGetValue(null, key, out value);
#endif
        }
#endif
        #endregion

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public bool TryGetValue(
            Interpreter interpreter,
            string key,
            out ScriptLocationIntDictionary value
            )
        {
            value = null;

            if (key == null)
                return false;

            foreach (KeyValuePair<string, ScriptLocationIntDictionary> pair in this)
            {
                if (ScriptLocation.MatchFileName(
                        interpreter, key, pair.Key))
                {
                    value = pair.Value;
                    return true;
                }
            }

            return false;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public string KeysAndValuesToString(
            string pattern,
            bool noCase
            )
        {
            StringList list = GenericOps<string, ScriptLocationIntDictionary>.KeysAndValues(
                this, false, true, true, StringOps.DefaultMatchMode, pattern, null, null,
                null, null, noCase, RegexOptions.None) as StringList;

            return ParserOps<string>.ListToString(list, Index.Invalid, Index.Invalid,
                ToStringFlags.None, Characters.Space.ToString(), null, false);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        #region Dead Code
#if DEAD_CODE
        public string KeysAndValuesToString(
            string pattern,
            RegexOptions regExOptions
            )
        {
            StringList list = GenericOps<string, ScriptLocationIntDictionary>.KeysAndValues(
                this, false, true, true, MatchMode.RegExp, pattern, null, null, null,
                null, false, regExOptions) as StringList;

            return ParserOps<string>.ListToString(list, Index.Invalid, Index.Invalid,
                ToStringFlags.None, Characters.Space.ToString(), null, false);
        }
#endif
        #endregion

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public IStringList ToList(
            string pattern,
            bool noCase
            )
        {
            IStringList list = new StringPairList();

            foreach (KeyValuePair<string, ScriptLocationIntDictionary> pair in this)
            {
                if ((pattern == null) ||
                    StringOps.Match(null, MatchMode.Glob, pair.Key, pattern, noCase))
                {
                    list.Add("Name", pair.Key);

                    if (pair.Value != null)
                    {
                        //
                        // HACK: This is a bit clumsy.
                        //
                        IEnumerable<IPair<string>> collection =
                            pair.Value.ToList() as IEnumerable<IPair<string>>;

                        if (collection != null)
                            foreach (IPair<string> item in collection)
                                list.Add(item.X, item.Y);
                    }
                }
            }

            return list;
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////////////////////////////

        #region System.Object Overrides
        public override string ToString()
        {
            return KeysAndValuesToString(null, false);
        }
        #endregion
    }
}
