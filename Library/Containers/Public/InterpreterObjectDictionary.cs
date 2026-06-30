/*
 * InterpreterObjectDictionary.cs --
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
using Eagle._Interfaces.Public;

#if FAST_DICTIONARY
using SomeDictionary = Eagle._Containers.Public.FastDictionary<
    Eagle._Interfaces.Public.IInterpreter, object>;
#else
using SomeDictionary = System.Collections.Generic.Dictionary<
    Eagle._Interfaces.Public.IInterpreter, object>;
#endif

#if NET_STANDARD_21
using Index = Eagle._Constants.Index;
#endif

namespace Eagle._Containers.Public
{
    /// <summary>
    /// This class represents a dictionary that maps interpreters to arbitrary
    /// object values, using an interpreter-aware equality comparer for its
    /// keys.  It extends the standard dictionary with a variety of methods for
    /// converting its keys, values, or both to the Eagle string list format,
    /// including optional pattern matching.
    /// </summary>
#if SERIALIZATION
    [Serializable()]
#endif
    [ObjectId("c0907e4b-5a32-468b-9dd5-4de8c2b057f6")]
    public sealed class InterpreterObjectDictionary : SomeDictionary
    {
        #region Public Constructors
        /// <summary>
        /// Constructs an empty instance of this class.
        /// </summary>
        public InterpreterObjectDictionary()
            : base(new _Comparers._Interpreter())
        {
            // do nothing.
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Constructs an empty instance of this class that has the specified
        /// initial capacity.
        /// </summary>
        /// <param name="capacity">
        /// The number of elements that the new dictionary can initially store.
        /// </param>
        public InterpreterObjectDictionary(
            int capacity
            )
            : base(capacity, new _Comparers._Interpreter())
        {
            // do nothing.
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Constructs an instance of this class that contains the elements
        /// copied from the specified dictionary.
        /// </summary>
        /// <param name="dictionary">
        /// The dictionary whose elements are copied into the new dictionary.
        /// </param>
        public InterpreterObjectDictionary(
            IDictionary<IInterpreter, object> dictionary
            )
            : base(dictionary, new _Comparers._Interpreter())
        {
            // do nothing.
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Protected Constructors
#if SERIALIZATION
        /// <summary>
        /// Constructs an instance of this class from previously serialized
        /// data.  This constructor is used during deserialization.
        /// </summary>
        /// <param name="info">
        /// The object that holds the serialized data for this dictionary.
        /// </param>
        /// <param name="context">
        /// The streaming context, describing the source and destination of the
        /// serialized data.
        /// </param>
        private InterpreterObjectDictionary(
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

        #region ToString Methods
        /// <summary>
        /// Converts the keys of this dictionary to a string in the Eagle list
        /// format, optionally including only those keys matching the specified
        /// pattern using the specified matching mode.
        /// </summary>
        /// <param name="mode">
        /// The matching mode used to compare each key against the pattern.
        /// </param>
        /// <param name="pattern">
        /// The pattern that each key must match in order to be included in the
        /// resulting string.  This parameter may be null, in which case all
        /// keys are included.
        /// </param>
        /// <param name="noCase">
        /// Non-zero if pattern matching should be case-insensitive.
        /// </param>
        /// <param name="regExOptions">
        /// The regular expression options to use when the matching mode is
        /// regular expression based.
        /// </param>
        /// <returns>
        /// The string representation of the keys of this dictionary.
        /// </returns>
        public string KeysToString(
            MatchMode mode,
            string pattern,
            bool noCase,
            RegexOptions regExOptions
            )
        {
            StringList list = GenericOps<IInterpreter, object>.KeysAndValues(
                this, false, true, false, mode, pattern, null, null, null,
                null, noCase, regExOptions) as StringList;

            return ParserOps<string>.ListToString(
                list, Index.Invalid, Index.Invalid, ToStringFlags.None,
                Characters.SpaceString, null, false);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Converts the keys of this dictionary to a string, joining the keys
        /// with the specified separator.
        /// </summary>
        /// <param name="separator">
        /// The string used to separate the keys in the resulting string.
        /// </param>
        /// <returns>
        /// The string representation of the keys of this dictionary.
        /// </returns>
        public string KeysToString(
            string separator
            )
        {
            StringList list = new StringList(this.Keys);

            return ParserOps<string>.ListToString(
                list, Index.Invalid, Index.Invalid, ToStringFlags.None,
                separator, null, false);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Converts the keys of this dictionary to a string in the Eagle list
        /// format, optionally including only those keys matching the specified
        /// pattern.
        /// </summary>
        /// <param name="pattern">
        /// The pattern that each key must match in order to be included in the
        /// resulting string.  This parameter may be null, in which case all
        /// keys are included.
        /// </param>
        /// <param name="noCase">
        /// Non-zero if pattern matching should be case-insensitive.
        /// </param>
        /// <returns>
        /// The string representation of the keys of this dictionary.
        /// </returns>
        public string KeysToString(
            string pattern,
            bool noCase
            )
        {
            StringList list = new StringList(this.Keys);

            return ParserOps<string>.ListToString(
                list, Index.Invalid, Index.Invalid, ToStringFlags.None,
                Characters.SpaceString, pattern, noCase);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Converts the keys of this dictionary to a string in the Eagle list
        /// format, optionally including only those keys matching the specified
        /// regular expression pattern.
        /// </summary>
        /// <param name="pattern">
        /// The regular expression pattern that each key must match in order to
        /// be included in the resulting string.  This parameter may be null, in
        /// which case all keys are included.
        /// </param>
        /// <param name="regExOptions">
        /// The regular expression options to use when matching keys against the
        /// pattern.
        /// </param>
        /// <returns>
        /// The string representation of the keys of this dictionary.
        /// </returns>
        public string KeysToString(
            string pattern,
            RegexOptions regExOptions
            )
        {
            StringList list = new StringList(this.Keys);

            return ParserOps<string>.ListToString(
                list, Index.Invalid, Index.Invalid, ToStringFlags.None,
                Characters.SpaceString, pattern, regExOptions);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Converts the values of this dictionary to a string in the Eagle list
        /// format, optionally including only those values matching the
        /// specified pattern using the specified matching mode.
        /// </summary>
        /// <param name="mode">
        /// The matching mode used to compare each value against the pattern.
        /// </param>
        /// <param name="pattern">
        /// The pattern that each value must match in order to be included in
        /// the resulting string.  This parameter may be null, in which case all
        /// values are included.
        /// </param>
        /// <param name="noCase">
        /// Non-zero if pattern matching should be case-insensitive.
        /// </param>
        /// <param name="regExOptions">
        /// The regular expression options to use when the matching mode is
        /// regular expression based.
        /// </param>
        /// <returns>
        /// The string representation of the values of this dictionary.
        /// </returns>
        public string ValuesToString(
            MatchMode mode,
            string pattern,
            bool noCase,
            RegexOptions regExOptions
            )
        {
            StringList list = GenericOps<IInterpreter, object>.KeysAndValues(
                this, false, false, true, mode, null, pattern, null, null,
                null, noCase, regExOptions) as StringList;

            return ParserOps<string>.ListToString(
                list, Index.Invalid, Index.Invalid, ToStringFlags.None,
                Characters.SpaceString, null, false);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Converts the values of this dictionary to a string in the Eagle list
        /// format, optionally including only those values matching the
        /// specified pattern.
        /// </summary>
        /// <param name="pattern">
        /// The pattern that each value must match in order to be included in
        /// the resulting string.  This parameter may be null, in which case all
        /// values are included.
        /// </param>
        /// <param name="noCase">
        /// Non-zero if pattern matching should be case-insensitive.
        /// </param>
        /// <returns>
        /// The string representation of the values of this dictionary.
        /// </returns>
        public string ValuesToString(
            string pattern,
            bool noCase
            )
        {
            StringList list = new StringList(this.Values);

            return ParserOps<string>.ListToString(
                list, Index.Invalid, Index.Invalid, ToStringFlags.None,
                Characters.SpaceString, pattern, noCase);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Converts the values of this dictionary to a string in the Eagle list
        /// format, optionally including only those values matching the
        /// specified regular expression pattern.
        /// </summary>
        /// <param name="pattern">
        /// The regular expression pattern that each value must match in order
        /// to be included in the resulting string.  This parameter may be null,
        /// in which case all values are included.
        /// </param>
        /// <param name="regExOptions">
        /// The regular expression options to use when matching values against
        /// the pattern.
        /// </param>
        /// <returns>
        /// The string representation of the values of this dictionary.
        /// </returns>
        public string ValuesToString(
            string pattern,
            RegexOptions regExOptions
            )
        {
            StringList list = new StringList(this.Values);

            return ParserOps<string>.ListToString(
                list, Index.Invalid, Index.Invalid, ToStringFlags.None,
                Characters.SpaceString, pattern, regExOptions);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Converts the keys and values of this dictionary to a string in the
        /// Eagle list format, optionally including only those entries whose key
        /// matches the specified pattern.
        /// </summary>
        /// <param name="pattern">
        /// The pattern that each key must match in order for its entry to be
        /// included in the resulting string.  This parameter may be null, in
        /// which case all entries are included.
        /// </param>
        /// <param name="noCase">
        /// Non-zero if pattern matching should be case-insensitive.
        /// </param>
        /// <returns>
        /// The string representation of the keys and values of this dictionary.
        /// </returns>
        public string KeysAndValuesToString(
            string pattern,
            bool noCase
            )
        {
            StringList list = GenericOps<IInterpreter, object>.KeysAndValues(
                this, false, true, true, StringOps.DefaultMatchMode, pattern,
                null, null, null, null, noCase, RegexOptions.None) as StringList;

            return ParserOps<string>.ListToString(
                list, Index.Invalid, Index.Invalid, ToStringFlags.None,
                Characters.SpaceString, null, false);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Converts the keys and values of this dictionary to a string in the
        /// Eagle list format, optionally including only those entries whose key
        /// matches the specified regular expression pattern.
        /// </summary>
        /// <param name="pattern">
        /// The regular expression pattern that each key must match in order for
        /// its entry to be included in the resulting string.  This parameter
        /// may be null, in which case all entries are included.
        /// </param>
        /// <param name="regExOptions">
        /// The regular expression options to use when matching keys against the
        /// pattern.
        /// </param>
        /// <returns>
        /// The string representation of the keys and values of this dictionary.
        /// </returns>
        public string KeysAndValuesToString(
            string pattern,
            RegexOptions regExOptions
            )
        {
            StringList list = GenericOps<IInterpreter, object>.KeysAndValues(
                this, false, true, true, MatchMode.RegExp, pattern, null, null,
                null, null, false, regExOptions) as StringList;

            return ParserOps<string>.ListToString(
                list, Index.Invalid, Index.Invalid, ToStringFlags.None,
                Characters.SpaceString, null, false);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Converts the keys of this dictionary to a string in the Eagle list
        /// format, optionally including only those keys matching the specified
        /// pattern.
        /// </summary>
        /// <param name="pattern">
        /// The pattern that each key must match in order to be included in the
        /// resulting string.  This parameter may be null, in which case all
        /// keys are included.
        /// </param>
        /// <param name="noCase">
        /// Non-zero if pattern matching should be case-insensitive.
        /// </param>
        /// <returns>
        /// The string representation of this dictionary.
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

        #region System.Object Overrides
        /// <summary>
        /// Converts the keys of this dictionary to a string in the Eagle list
        /// format.
        /// </summary>
        /// <returns>
        /// The string representation of this dictionary.
        /// </returns>
        public override string ToString()
        {
            return ToString(null, false);
        }
        #endregion
    }
}
