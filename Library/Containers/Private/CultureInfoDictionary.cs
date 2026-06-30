/*
 * CultureInfoDictionary.cs --
 *
 * Copyright (c) 2007-2012 by Joe Mistachkin.  All rights reserved.
 *
 * See the file "license.terms" for information on usage and redistribution of
 * this file, and for a DISCLAIMER OF ALL WARRANTIES.
 *
 * RCS: @(#) $Id: $
 */

using System.Collections.Generic;
using System.Globalization;
using Eagle._Attributes;
using Eagle._Components.Private;
using Eagle._Components.Public;
using Eagle._Constants;
using Eagle._Containers.Public;

#if FAST_DICTIONARY
using SomeDictionary = Eagle._Containers.Public.FastDictionary<
    string, System.Globalization.CultureInfo>;
#else
using SomeDictionary = System.Collections.Generic.Dictionary<
    string, System.Globalization.CultureInfo>;
#endif

#if NET_STANDARD_21
using Index = Eagle._Constants.Index;
#endif

namespace Eagle._Containers.Private
{
    /// <summary>
    /// This class represents a dictionary that maps culture names to their
    /// associated <see cref="CultureInfo" /> objects.  It extends the underlying
    /// generic dictionary with bulk-population helpers and conversion of its
    /// keys to the Eagle string list format, including optional pattern
    /// matching.
    /// </summary>
    [ObjectId("87f76a7b-7e68-4ac9-937a-d225520971b9")]
    internal sealed class CultureInfoDictionary : SomeDictionary
    {
        /// <summary>
        /// Constructs an empty instance of this class.
        /// </summary>
        public CultureInfoDictionary()
            : base()
        {
            // do nothing.
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Constructs an instance of this class that contains an entry for each
        /// culture in the specified collection, keyed by its string form.
        /// </summary>
        /// <param name="collection">
        /// The collection of cultures whose entries are added to the new
        /// dictionary.
        /// </param>
        public CultureInfoDictionary(IEnumerable<CultureInfo> collection)
            : base()
        {
            Add(collection);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method adds an entry for each culture in the specified
        /// collection, keying each entry by the string form of the culture.
        /// </summary>
        /// <param name="collection">
        /// The collection of cultures whose entries are added to this
        /// dictionary.
        /// </param>
        public void Add(IEnumerable<CultureInfo> collection)
        {
            foreach (CultureInfo item in collection)
                this.Add(item.ToString(), item);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

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
        public string ToString(string pattern, bool noCase)
        {
            StringList list = new StringList(this.Keys);

            return ParserOps<string>.ListToString(list, Index.Invalid, Index.Invalid,
                ToStringFlags.None, Characters.SpaceString, pattern, noCase);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

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
