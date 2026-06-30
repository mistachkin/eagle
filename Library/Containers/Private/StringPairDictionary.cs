/*
 * StringPairDictionary.cs --
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
using Eagle._Attributes;
using Eagle._Components.Private;
using Eagle._Components.Public;
using Eagle._Constants;
using Eagle._Containers.Public;
using Eagle._Interfaces.Public;

#if FAST_DICTIONARY
using SomeDictionary = Eagle._Containers.Public.FastDictionary<
    string, Eagle._Interfaces.Public.IPair<string>>;
#else
using SomeDictionary = System.Collections.Generic.Dictionary<
    string, Eagle._Interfaces.Public.IPair<string>>;
#endif

#if NET_STANDARD_21
using Index = Eagle._Constants.Index;
#endif

namespace Eagle._Containers.Private
{
    /// <summary>
    /// This class represents a dictionary that maps string names to string
    /// pairs (<see cref="IPair{T}" /> of string).  It extends the underlying
    /// generic dictionary with helpers for populating, filtering, and producing
    /// a string form of its keys.
    /// </summary>
#if SERIALIZATION
    [Serializable()]
#endif
    [ObjectId("a22cdd6d-d3b5-4336-a4f0-c54cd618004f")]
    internal sealed class StringPairDictionary : SomeDictionary
    {
        #region Public Constructors
        /// <summary>
        /// Constructs an empty string pair dictionary.
        /// </summary>
        public StringPairDictionary()
            : base()
        {
            // do nothing.
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Constructs a string pair dictionary whose keys are the items of the
        /// specified collection, each associated with a null value.
        /// </summary>
        /// <param name="collection">
        /// The collection of strings to add as keys of the new dictionary.
        /// </param>
        public StringPairDictionary(
            IEnumerable<string> collection
            )
            : base()
        {
            Add(collection);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Constructs a string pair dictionary initialized with the entries of
        /// the specified dictionary, wrapping each value in a string pair.
        /// </summary>
        /// <param name="dictionary">
        /// The dictionary whose key/value pairs are copied into the new
        /// dictionary.
        /// </param>
        public StringPairDictionary(
            IDictionary<string, string> dictionary
            )
            : base()
        {
            Add(dictionary);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Constructs a string pair dictionary initialized with the entries
        /// copied from the specified dictionary.
        /// </summary>
        /// <param name="dictionary">
        /// The dictionary whose key/value pairs are copied into the new
        /// dictionary.
        /// </param>
        public StringPairDictionary(
            IDictionary<string, IPair<string>> dictionary
            )
            : base(dictionary)
        {
            // do nothing.
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Public Methods
        /// <summary>
        /// This method adds each item of the specified collection as a key with
        /// a null value.  Items that are null are skipped.
        /// </summary>
        /// <param name="collection">
        /// The collection of strings to add as keys.
        /// </param>
        public void Add(
            IEnumerable<string> collection
            )
        {
            foreach (string item in collection)
            {
                if (item == null)
                    continue;

                Add(item, null);
            }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method adds an entry for each key/value pair in the specified
        /// dictionary, wrapping each value in a string pair.
        /// </summary>
        /// <param name="dictionary">
        /// The dictionary whose key/value pairs are added.
        /// </param>
        public void Add(
            IDictionary<string, string> dictionary
            )
        {
            foreach (KeyValuePair<string, string> pair in dictionary)
                Add(pair.Key, new StringPair(pair.Value));
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method creates a new dictionary that contains only the entries
        /// whose keys match the specified pattern.
        /// </summary>
        /// <param name="pattern">
        /// The pattern used to select the keys that are included in the result.
        /// This parameter may be null, in which case all entries are included.
        /// </param>
        /// <param name="noCase">
        /// Non-zero if the pattern matching should be performed in a
        /// case-insensitive manner.
        /// </param>
        /// <returns>
        /// The newly created dictionary containing the matching entries.
        /// </returns>
        public StringPairDictionary Filter(
            string pattern,
            bool noCase
            )
        {
            StringPairDictionary dictionary = new StringPairDictionary();

            foreach (KeyValuePair<string, IPair<string>> pair in this)
            {
                if ((pattern == null) || Parser.StringMatch(
                        null, pair.Key, 0, pattern, 0, noCase))
                {
                    dictionary.Add(pair.Key, pair.Value);
                }
            }

            return dictionary;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method produces a string containing the keys of the dictionary
        /// that match the specified pattern.
        /// </summary>
        /// <param name="pattern">
        /// The pattern used to filter the keys that are included in the result.
        /// This parameter may be null, in which case all keys are included.
        /// </param>
        /// <param name="noCase">
        /// Non-zero if the pattern matching should be performed in a
        /// case-insensitive manner.
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
                list, Index.Invalid, Index.Invalid,
                ToStringFlags.None, Characters.SpaceString,
                pattern, noCase);
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region System.Object Overrides
        /// <summary>
        /// This method produces a string containing all of the keys of the
        /// dictionary.
        /// </summary>
        /// <returns>
        /// The keys of the dictionary formatted as a string.
        /// </returns>
        public override string ToString()
        {
            return ToString(null, false);
        }
        #endregion
    }
}
