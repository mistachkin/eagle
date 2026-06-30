/*
 * VersionStringDictionary.cs --
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

using System.Text.RegularExpressions;
using Eagle._Attributes;
using Eagle._Components.Private;
using Eagle._Components.Public;
using Eagle._Constants;

#if FAST_DICTIONARY
using SomeDictionary = Eagle._Containers.Public.FastDictionary<
    System.Version, string>;
#else
using SomeDictionary = System.Collections.Generic.Dictionary<
    System.Version, string>;
#endif

#if NET_STANDARD_21
using Index = Eagle._Constants.Index;
#endif

namespace Eagle._Containers.Public
{
    /// <summary>
    /// This class represents a dictionary that maps versions
    /// (<see cref="Version" />) to strings.
    /// </summary>
#if SERIALIZATION
    [Serializable()]
#endif
    [ObjectId("39232b98-6fc0-409c-98cb-76a811ddf1db")]
    public sealed class VersionStringDictionary : SomeDictionary
    {
        #region Public Constructors
        /// <summary>
        /// Constructs an empty dictionary of versions and strings.
        /// </summary>
        public VersionStringDictionary()
            : base()
        {
            // do nothing.
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Protected Constructors
#if SERIALIZATION
        /// <summary>
        /// Constructs a dictionary of versions and strings from previously
        /// serialized data.
        /// </summary>
        /// <param name="info">
        /// The object that holds the serialized data for the dictionary.
        /// </param>
        /// <param name="context">
        /// The streaming context describing the source and destination of the
        /// serialized data.
        /// </param>
        private VersionStringDictionary(
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
        /// This method returns a string representation of the keys and values
        /// of this dictionary, with the elements separated by spaces.
        /// </summary>
        /// <param name="pattern">
        /// The optional pattern used to filter the keys and values included in
        /// the result.  This parameter may be null.
        /// </param>
        /// <param name="noCase">
        /// Non-zero if pattern matching should be performed in a
        /// case-insensitive manner.
        /// </param>
        /// <returns>
        /// The string representation of the keys and values of this dictionary.
        /// </returns>
        public string KeysAndValuesToString(
            string pattern,
            bool noCase
            )
        {
            StringList list = GenericOps<Version, string>.KeysAndValues(
                this, false, true, true, StringOps.DefaultMatchMode, pattern,
                null, null, null, null, noCase, RegexOptions.None) as StringList;

            return ParserOps<string>.ListToString(
                list, Index.Invalid, Index.Invalid, ToStringFlags.None,
                Characters.SpaceString, null, false);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method returns a string representation of the keys of this
        /// dictionary, with the versions separated by spaces.
        /// </summary>
        /// <param name="pattern">
        /// The optional pattern used to filter the keys included in the result.
        /// This parameter may be null.
        /// </param>
        /// <param name="noCase">
        /// Non-zero if pattern matching should be performed in a
        /// case-insensitive manner.
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
                list, Index.Invalid, Index.Invalid, ToStringFlags.None,
                Characters.SpaceString, pattern, noCase);
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region System.Object Overrides
        /// <summary>
        /// This method returns a string representation of the keys of this
        /// dictionary, with the versions separated by spaces.
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
