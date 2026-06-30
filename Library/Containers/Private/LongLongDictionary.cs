/*
 * LongLongDictionary.cs --
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

#if FAST_DICTIONARY
using SomeDictionary = Eagle._Containers.Public.FastDictionary<long, long>;
#else
using SomeDictionary = System.Collections.Generic.Dictionary<long, long>;
#endif

#if NET_STANDARD_21
using Index = Eagle._Constants.Index;
#endif

namespace Eagle._Containers.Private
{
    /// <summary>
    /// This class represents a dictionary that maps 64-bit signed integer keys
    /// to 64-bit signed integer values.  It adds support for producing a
    /// filtered string form of its keys and values.
    /// </summary>
#if SERIALIZATION
    [Serializable()]
#endif
    [ObjectId("4aa7c66d-41e8-4041-a858-5da1036ce8c4")]
    internal sealed class LongLongDictionary : SomeDictionary
    {
        #region Private Constants
        /// <summary>
        /// The numeric format specifier used when rendering the integer keys as
        /// strings (hexadecimal).
        /// </summary>
        private static readonly string KeyFormat = "X";
        #endregion

        ///////////////////////////////////////////////////////////////////////////////////////////////

        #region Public Constructors
        /// <summary>
        /// Constructs an empty instance of this class.
        /// </summary>
        public LongLongDictionary()
            : base()
        {
            // do nothing.
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Constructs an instance of this class that is initialized with the
        /// entries copied from the specified dictionary.
        /// </summary>
        /// <param name="dictionary">
        /// The dictionary whose key/value pairs are copied into the new
        /// dictionary.
        /// </param>
        public LongLongDictionary(
            IDictionary<long, long> dictionary
            )
            : base(dictionary)
        {
            // do nothing.
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////////////////////////////

        #region Protected Constructors
#if SERIALIZATION
        /// <summary>
        /// Constructs an instance of this class from previously serialized data.
        /// This constructor is used during deserialization.
        /// </summary>
        /// <param name="info">
        /// The object that holds the serialized data for the dictionary.
        /// </param>
        /// <param name="context">
        /// The streaming context that describes the source of the serialized
        /// data.
        /// </param>
        private LongLongDictionary(
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
        /// <summary>
        /// This method produces a string containing the keys and values of the
        /// dictionary whose keys match the specified pattern.
        /// </summary>
        /// <param name="pattern">
        /// The pattern used to filter the keys that are included in the result.
        /// This parameter may be null, in which case all entries are included.
        /// </param>
        /// <param name="noCase">
        /// Non-zero if the pattern matching should be performed in a
        /// case-insensitive manner.
        /// </param>
        /// <returns>
        /// The matching keys and values formatted as a string.
        /// </returns>
        public string ToString(
            string pattern,
            bool noCase
            )
        {
            StringList list = GenericOps<long, long>.KeysAndValues(
                this, false, true, true, StringOps.DefaultMatchMode, pattern, null,
                KeyFormat, null, null, noCase, RegexOptions.None) as StringList;

            return ParserOps<string>.ListToString(list, Index.Invalid, Index.Invalid,
                ToStringFlags.None, Characters.SpaceString, null, false);
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
            return ToString(null, false);
        }
        #endregion
    }
}
