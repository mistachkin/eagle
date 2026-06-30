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
    /// <summary>
    /// This class represents a dictionary that maps script file names to the
    /// breakpoints defined within them.  Each value is a dictionary that maps
    /// script locations to integer breakpoint identifiers.  Lookups support
    /// matching file names in a manner that accounts for path normalization.
    /// </summary>
#if SERIALIZATION
    [Serializable()]
#endif
    [ObjectId("7e6e8490-b7eb-41c7-add8-75de1e3d87c0")]
    internal sealed class BreakpointDictionary :
            PathDictionary<ScriptLocationIntDictionary>,
            IDictionary<string, ScriptLocationIntDictionary>
    {
        #region Public Constructors
        /// <summary>
        /// Constructs an empty instance of this class.
        /// </summary>
        public BreakpointDictionary()
            : base()
        {
            // do nothing.
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        #region Dead Code
#if DEAD_CODE
        /// <summary>
        /// Constructs an empty instance of this class that uses the specified
        /// equality comparer for its keys.
        /// </summary>
        /// <param name="comparer">
        /// The equality comparer used to compare keys.
        /// </param>
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
        /// <summary>
        /// Constructs an instance of this class from previously serialized
        /// data.  This constructor is used during deserialization.
        /// </summary>
        /// <param name="info">
        /// The object that holds the serialized data for the dictionary.
        /// </param>
        /// <param name="context">
        /// The streaming context that describes the source of the serialized
        /// data.
        /// </param>
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
        /// <summary>
        /// This method attempts to get the value associated with the specified
        /// key.  This is the explicit interface implementation.
        /// </summary>
        /// <param name="key">
        /// The key whose associated value is to be retrieved.
        /// </param>
        /// <param name="value">
        /// Upon success, receives the value associated with the specified key;
        /// otherwise, receives null.
        /// </param>
        /// <returns>
        /// True if the key was found; otherwise, false.
        /// </returns>
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

        /// <summary>
        /// This method attempts to get the value associated with the specified
        /// key, hiding the base class method of the same name.
        /// </summary>
        /// <param name="key">
        /// The key whose associated value is to be retrieved.
        /// </param>
        /// <param name="value">
        /// Upon success, receives the value associated with the specified key;
        /// otherwise, receives null.
        /// </param>
        /// <returns>
        /// True if the key was found; otherwise, false.
        /// </returns>
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

        /// <summary>
        /// This method attempts to get the value associated with the specified
        /// key, matching the key against the stored file names in a manner that
        /// accounts for path normalization.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter context used when matching file names.  This
        /// parameter may be null.
        /// </param>
        /// <param name="key">
        /// The file name whose associated value is to be retrieved.
        /// </param>
        /// <param name="value">
        /// Upon success, receives the value associated with the matching key;
        /// otherwise, receives null.
        /// </param>
        /// <returns>
        /// True if a matching key was found; otherwise, false.
        /// </returns>
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

        /// <summary>
        /// This method produces a string containing the keys and values of the
        /// dictionary that match the specified pattern.
        /// </summary>
        /// <param name="pattern">
        /// The pattern used to filter the entries that are included in the
        /// result.  This parameter may be null, in which case all entries are
        /// included.
        /// </param>
        /// <param name="noCase">
        /// Non-zero if the pattern matching should be performed in a
        /// case-insensitive manner.
        /// </param>
        /// <returns>
        /// The list of matching keys and values formatted as a string.
        /// </returns>
        public string KeysAndValuesToString(
            string pattern,
            bool noCase
            )
        {
            StringList list = GenericOps<string, ScriptLocationIntDictionary>.KeysAndValues(
                this, false, true, true, StringOps.DefaultMatchMode, pattern, null, null,
                null, null, noCase, RegexOptions.None) as StringList;

            return ParserOps<string>.ListToString(list, Index.Invalid, Index.Invalid,
                ToStringFlags.None, Characters.SpaceString, null, false);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        #region Dead Code
#if DEAD_CODE
        /// <summary>
        /// This method produces a string containing the keys and values of the
        /// dictionary whose keys match the specified regular expression
        /// pattern.
        /// </summary>
        /// <param name="pattern">
        /// The regular expression pattern used to filter the entries that are
        /// included in the result.  This parameter may be null, in which case
        /// all entries are included.
        /// </param>
        /// <param name="regExOptions">
        /// The options used when performing the regular expression matching.
        /// </param>
        /// <returns>
        /// The list of matching keys and values formatted as a string.
        /// </returns>
        public string KeysAndValuesToString(
            string pattern,
            RegexOptions regExOptions
            )
        {
            StringList list = GenericOps<string, ScriptLocationIntDictionary>.KeysAndValues(
                this, false, true, true, MatchMode.RegExp, pattern, null, null, null,
                null, false, regExOptions) as StringList;

            return ParserOps<string>.ListToString(list, Index.Invalid, Index.Invalid,
                ToStringFlags.None, Characters.SpaceString, null, false);
        }
#endif
        #endregion

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method builds a list of name/value pairs describing the
        /// entries of the dictionary whose keys match the specified pattern.
        /// </summary>
        /// <param name="pattern">
        /// The pattern that each key must match in order to be included.  This
        /// parameter may be null, in which case all entries are included.
        /// </param>
        /// <param name="noCase">
        /// Non-zero if the pattern matching should be performed in a
        /// case-insensitive manner.
        /// </param>
        /// <returns>
        /// The list of name/value pairs describing the matching entries.
        /// </returns>
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
        /// <summary>
        /// This method produces a string containing all of the keys and values
        /// of the dictionary.
        /// </summary>
        /// <returns>
        /// The keys and values of the dictionary formatted as a string.
        /// </returns>
        public override string ToString()
        {
            return KeysAndValuesToString(null, false);
        }
        #endregion
    }
}
