/*
 * PackageAliasDictionary.cs --
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

using Eagle._Attributes;
using Eagle._Components.Private;
using Eagle._Components.Public;
using Eagle._Constants;
using Eagle._Containers.Public;

#if FAST_DICTIONARY
using SomeDictionary = Eagle._Containers.Public.FastDictionary<
    string, Eagle._Components.Public.AnyTriplet<string,
    System.Version, Eagle._Components.Public.PackageFlags?>>;
#else
using SomeDictionary = System.Collections.Generic.Dictionary<
    string, Eagle._Components.Public.AnyTriplet<string,
    System.Version, Eagle._Components.Public.PackageFlags?>>;
#endif

#if NET_STANDARD_21
using Index = Eagle._Constants.Index;
#endif

namespace Eagle._Containers.Private
{
    /// <summary>
    /// This class represents a dictionary that maps a package name to the
    /// alias information for that package (its target name, version, and
    /// optional package flags), keyed by name.  It extends the underlying
    /// dictionary with a type name suitable for use within Eagle and the
    /// ability to be converted to the Eagle list format.
    /// </summary>
#if SERIALIZATION
    [Serializable()]
#endif
    [ObjectId("6659ef52-e343-4c50-8d2b-24c8d055ae5d")]
    internal sealed class PackageAliasDictionary : SomeDictionary
    {
        #region Public Constructors
        /// <summary>
        /// Constructs an empty instance of this class.
        /// </summary>
        public PackageAliasDictionary()
            : base()
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
        public PackageAliasDictionary(
            PackageAliasDictionary dictionary
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
        /// Constructs an instance of this class from previously serialized
        /// data.
        /// </summary>
        /// <param name="info">
        /// The object that holds the serialized data for this dictionary.
        /// </param>
        /// <param name="context">
        /// The source and destination of the serialized stream associated with
        /// this dictionary.
        /// </param>
        private PackageAliasDictionary(
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
        /// The pattern used to filter the keys, or null to include all of them.
        /// </param>
        /// <param name="noCase">
        /// Non-zero if pattern matching should be case-insensitive.
        /// </param>
        /// <returns>
        /// The string, in the Eagle list format, that represents the matching
        /// keys of this dictionary.
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
        /// The string, in the Eagle list format, that represents the keys of
        /// this dictionary.
        /// </returns>
        public override string ToString()
        {
            return ToString(null, false);
        }
        #endregion
    }
}
