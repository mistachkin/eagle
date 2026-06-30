/*
 * DbConnectionTypeDictionary.cs --
 *
 * Copyright (c) 2007-2012 by Joe Mistachkin.  All rights reserved.
 *
 * See the file "license.terms" for information on usage and redistribution of
 * this file, and for a DISCLAIMER OF ALL WARRANTIES.
 *
 * RCS: @(#) $Id: $
 */

using System;
using System.Collections.Generic;

#if SERIALIZATION
using System.Runtime.Serialization;
#endif

using Eagle._Attributes;
using Eagle._Components.Private;
using Eagle._Components.Public;
using Eagle._Constants;
using Eagle._Containers.Public;

using ConnectionTriplet = Eagle._Components.Public.AnyTriplet<
    string, string, byte[]>;

#if FAST_DICTIONARY
using SomeDictionary = Eagle._Containers.Public.FastDictionary<
    Eagle._Components.Public.DbConnectionType,
    Eagle._Components.Public.AnyTriplet<string, string, byte[]>>;
#else
using SomeDictionary = System.Collections.Generic.Dictionary<
    Eagle._Components.Public.DbConnectionType,
    Eagle._Components.Public.AnyTriplet<string, string, byte[]>>;
#endif

#if NET_STANDARD_21
using Index = Eagle._Constants.Index;
#endif

namespace Eagle._Containers.Private
{
    /// <summary>
    /// This class represents a dictionary that maps each
    /// <see cref="DbConnectionType" /> to a triplet describing the associated
    /// database connection (its type name, invariant name, and assembly bytes).
    /// It extends the underlying generic dictionary with conversion of its keys
    /// to the Eagle string list format, including optional pattern matching.
    /// </summary>
#if SERIALIZATION
    [Serializable()]
#endif
    [ObjectId("68dabc08-7849-439f-bcc7-bf5d6fb1a43e")]
    internal sealed class DbConnectionTypeDictionary : SomeDictionary
    {
        #region Public Constructors
        /// <summary>
        /// Constructs an empty instance of this class.
        /// </summary>
        public DbConnectionTypeDictionary()
            : base()
        {
            // do nothing.
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Constructs an instance of this class that is initialized with the
        /// entries copied from the specified dictionary.
        /// </summary>
        /// <param name="dictionary">
        /// The dictionary whose key/value pairs are copied into the new
        /// dictionary.
        /// </param>
        public DbConnectionTypeDictionary(
            IDictionary<DbConnectionType, ConnectionTriplet> dictionary
            )
            : base(dictionary)
        {
            // do nothing.
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

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
        private DbConnectionTypeDictionary(
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
        /// Converts the keys of this dictionary to a string in the Eagle list
        /// format, optionally including only those keys matching the specified
        /// pattern.
        /// </summary>
        /// <param name="pattern">
        /// The pattern that each key must match in order to be included in the
        /// resulting string.  This parameter may be null, in which case all keys
        /// are included.
        /// </param>
        /// <param name="noCase">
        /// Non-zero if pattern matching should be case-insensitive.
        /// </param>
        /// <returns>
        /// The string representation of the keys of this dictionary.
        /// </returns>
        public string ToString(
            string pattern,
            bool noCase
            )
        {
            StringList list = new StringList(this.Keys);

            return ParserOps<string>.ListToString(
                list, Index.Invalid, Index.Invalid,
                ToStringFlags.None, Characters.SpaceString,
                pattern, noCase);
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region System.Object Overrides
        /// <summary>
        /// Converts the keys of this dictionary to a string in the Eagle list
        /// format.
        /// </summary>
        /// <returns>
        /// The string representation of the keys of this dictionary.
        /// </returns>
        public override string ToString()
        {
            return ToString(null, false);
        }
        #endregion
    }
}
