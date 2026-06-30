/*
 * EncodingDictionary.cs --
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

using System.Text;
using Eagle._Attributes;
using Eagle._Components.Private;
using Eagle._Components.Public;
using Eagle._Constants;
using Eagle._Containers.Public;

#if FAST_DICTIONARY
using SomeDictionary = Eagle._Containers.Public.FastDictionary<
    string, System.Text.Encoding>;
#else
using SomeDictionary = System.Collections.Generic.Dictionary<
    string, System.Text.Encoding>;
#endif

#if NET_STANDARD_21
using Index = Eagle._Constants.Index;
#endif

namespace Eagle._Containers.Private
{
    /// <summary>
    /// This class represents a dictionary that maps string keys to text
    /// encoding values.  It extends the underlying generic dictionary of
    /// encodings with a helper for producing a filtered string form of its
    /// keys.
    /// </summary>
#if SERIALIZATION
    [Serializable()]
#endif
    [ObjectId("78930c01-9509-4f65-89d6-3e1f03a6e0fd")]
    internal sealed class EncodingDictionary : SomeDictionary
    {
        /// <summary>
        /// Constructs an empty encoding dictionary.
        /// </summary>
        public EncodingDictionary()
            : base()
        {
            // do nothing.
        }

        ///////////////////////////////////////////////////////////////////////

        #region Protected Constructors
#if SERIALIZATION
        /// <summary>
        /// Constructs an encoding dictionary from previously serialized data.
        /// This constructor is used during deserialization.
        /// </summary>
        /// <param name="info">
        /// The object that holds the serialized data for the dictionary.
        /// </param>
        /// <param name="context">
        /// The streaming context that describes the source of the serialized
        /// data.
        /// </param>
        private EncodingDictionary(
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

        #region Dead Code
#if DEAD_CODE
        #region Private Constructors
        /// <summary>
        /// Constructs an encoding dictionary that is initialized with the
        /// entries copied from the specified dictionary.
        /// </summary>
        /// <param name="dictionary">
        /// The dictionary whose key/value pairs are copied into the new
        /// dictionary.
        /// </param>
        private EncodingDictionary(
            IDictionary<string, Encoding> dictionary
            )
            : base(dictionary)
        {
            // do nothing.
        }
        #endregion
#endif
        #endregion

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Constructs an empty encoding dictionary that uses the specified
        /// equality comparer when comparing keys.
        /// </summary>
        /// <param name="comparer">
        /// The equality comparer used to compare keys, or null to use the
        /// default comparer.
        /// </param>
        public EncodingDictionary(
            IEqualityComparer<string> comparer
            )
            : base(comparer)
        {
            // do nothing.
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method produces a string containing the keys of the dictionary
        /// that match the specified pattern.
        /// </summary>
        /// <param name="pattern">
        /// The pattern used to select which keys are included.  This parameter
        /// may be null to include all keys.
        /// </param>
        /// <param name="noCase">
        /// Non-zero if pattern matching should be case-insensitive.
        /// </param>
        /// <returns>
        /// The list of matching keys formatted as a string.
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

        ///////////////////////////////////////////////////////////////////////

        #region System.Object Overrides
        /// <summary>
        /// This method produces a string containing all the keys of the
        /// dictionary.
        /// </summary>
        /// <returns>
        /// The list of keys formatted as a string.
        /// </returns>
        public override string ToString()
        {
            return ToString(null, false);
        }
        #endregion
    }
}
