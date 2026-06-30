/*
 * StringLongPairStringDictionary.cs --
 *
 * Copyright (c) 2007-2012 by Joe Mistachkin.  All rights reserved.
 *
 * See the file "license.terms" for information on usage and redistribution of
 * this file, and for a DISCLAIMER OF ALL WARRANTIES.
 *
 * RCS: @(#) $Id: $
 */

using System;
using System.Collections;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Threading;
using Eagle._Attributes;
using Eagle._Components.Private;
using Eagle._Components.Public;
using Eagle._Constants;
using Eagle._Containers.Public;
using Eagle._Interfaces.Public;
using IStringLongPair = Eagle._Interfaces.Public.IAnyPair<string, long>;

#if NET_STANDARD_21
using Index = Eagle._Constants.Index;
#endif

namespace Eagle._Containers.Private
{
    /// <summary>
    /// This class represents a dictionary that maps string keys to string
    /// values while preserving the original insertion order of the keys.  Each
    /// string key is internally paired with a monotonically increasing sequence
    /// number so that duplicate string keys can coexist and the entries remain
    /// ordered as they were added.
    /// </summary>
#if SERIALIZATION
    [Serializable()]
#endif
    [ObjectId("21469290-3380-40dc-9331-c235c6bed124")]
    internal sealed class StringLongPairStringDictionary :
            SortedDictionary<IAnyPair<string, long>, string>
    {
        #region StringLongPair Class
        /// <summary>
        /// This class represents a key that pairs a string with a long integer
        /// sequence number, ordering entries by their sequence number so that
        /// the original insertion order of the string keys is maintained.
        /// </summary>
#if SERIALIZATION
        [Serializable()]
#endif
        [ObjectId("415b245b-ebf5-4607-9d5e-e81da3c6850b")]
        private sealed class StringLongPair : AnyPair<string, long>
        {
            #region Public Constructors
            /// <summary>
            /// Constructs a string and sequence number pair using the specified
            /// string key and sequence number.
            /// </summary>
            /// <param name="x">
            /// The string key for this pair.
            /// </param>
            /// <param name="y">
            /// The long integer sequence number for this pair.
            /// </param>
            public StringLongPair(
                string x, /* in */
                long y    /* in */
                )
                : base(x, y)
            {
                // do nothing.
            }
            #endregion

            ///////////////////////////////////////////////////////////////////

            #region IComparer<IAnyPair<string, long>> Overrides
            /// <summary>
            /// This method compares two pairs, ordering them first by their
            /// sequence numbers and then by their string keys.  Null pairs are
            /// ordered before non-null pairs.
            /// </summary>
            /// <param name="x">
            /// The first pair to compare.
            /// </param>
            /// <param name="y">
            /// The second pair to compare.
            /// </param>
            /// <returns>
            /// Zero if the pairs are equal, a negative value if the first pair
            /// is less than the second, or a positive value if the first pair
            /// is greater than the second.
            /// </returns>
            public override int Compare(
                IAnyPair<string, long> x, /* in */
                IAnyPair<string, long> y  /* in */
                )
            {
                if ((x == null) && (y == null))
                {
                    return 0;
                }
                else if (x == null)
                {
                    return -1;
                }
                else if (y == null)
                {
                    return 1;
                }
                else
                {
                    //
                    // HACK: Compare the sequence numbers first as they are
                    //       used to maintain the original (i.e. "as added")
                    //       ordering of the string keys.
                    //
                    int result = Comparer<long>.Default.Compare(x.Y, y.Y);

                    if (result != 0)
                        return result;

                    return Comparer<string>.Default.Compare(x.X, y.X);
                }
            }
            #endregion

            ///////////////////////////////////////////////////////////////////

            #region System.Object Overrides
            /// <summary>
            /// This method returns the string representation of this pair, which
            /// is the string key only.
            /// </summary>
            /// <returns>
            /// The string key of this pair.
            /// </returns>
            public override string ToString()
            {
                //
                // HACK: Return the string key only; ignore the long integer
                //       sequence number.
                //
                return this.X;
            }
            #endregion
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Private Static Data
        /// <summary>
        /// The most recently assigned sequence number; this is incremented to
        /// produce the next sequence number used to order string keys.
        /// </summary>
        private static long nextId = 0;
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Private Data
        /// <summary>
        /// The comparer used to compare the string portion of the keys.
        /// </summary>
        private IComparer<string> stringComparer = Comparer<string>.Default;
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Public Constructors
        /// <summary>
        /// Constructs an empty dictionary, optionally using a key type that
        /// reports only its string portion when converted to a string.
        /// </summary>
        /// <param name="useStringKeyOnly">
        /// Non-zero to use keys that report only their string portion when
        /// converted to a string; otherwise, the default pair representation is
        /// used.
        /// </param>
        public StringLongPairStringDictionary(
            bool useStringKeyOnly
            )
            : base()
        {
            this.useStringKeyOnly = useStringKeyOnly;
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Private Static Methods
        /// <summary>
        /// This method atomically produces the next sequence number used to
        /// order string keys.
        /// </summary>
        /// <returns>
        /// The next sequence number.
        /// </returns>
        private static long NextId()
        {
            return Interlocked.Increment(ref nextId);
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Private Methods
        /// <summary>
        /// This method creates a new key that pairs the specified string with a
        /// freshly allocated sequence number.
        /// </summary>
        /// <param name="key">
        /// The string key to pair with a sequence number.
        /// </param>
        /// <returns>
        /// The newly created key pair.
        /// </returns>
        private IAnyPair<string, long> GetAnyPairForStringKey(
            string key /* in */
            )
        {
            if (useStringKeyOnly)
                return new StringLongPair(key, NextId());
            else
                return new AnyPair<string, long>(key, NextId());
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method compares two string keys using the configured string
        /// comparer.
        /// </summary>
        /// <param name="x">
        /// The first string key to compare.
        /// </param>
        /// <param name="y">
        /// The second string key to compare.
        /// </param>
        /// <returns>
        /// Zero if the keys are equal, a negative value if the first key is less
        /// than the second, or a positive value if the first key is greater than
        /// the second.
        /// </returns>
        private int CompareStringKey(
            string x, /* in */
            string y  /* in */
            )
        {
            IComparer<string> comparer = stringComparer;

            if (comparer == null)
                throw new InvalidOperationException(); /* IMPOSSIBLE? */

            return comparer.Compare(x, y);
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Public Properties
        /// <summary>
        /// The backing field for the <see cref="UseStringKeyOnly" /> property.
        /// </summary>
        private bool useStringKeyOnly;
        /// <summary>
        /// Gets a value indicating whether keys that report only their string
        /// portion when converted to a string are used by this dictionary.
        /// </summary>
        public bool UseStringKeyOnly
        {
            get { return useStringKeyOnly; }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Public Methods (Dictionary<string, string>)
        /// <summary>
        /// This method adds the specified string key and value to the
        /// dictionary.  The key is paired with a fresh sequence number, so this
        /// method cannot fail even when the same string key is added more than
        /// once.
        /// </summary>
        /// <param name="key">
        /// The string key of the entry to add.
        /// </param>
        /// <param name="value">
        /// The string value to associate with the specified key.
        /// </param>
        public void Add( /* O(1) */
            string key,  /* in */
            string value /* in */
            )
        {
            //
            // NOTE: This method cannot fail, even for "duplicate" keys,
            //       e.g. the same namespace name.
            //
            this.Add(GetAnyPairForStringKey(key), value);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method determines whether the dictionary contains an entry with
        /// the specified string key.
        /// </summary>
        /// <param name="key">
        /// The string key to locate in the dictionary.
        /// </param>
        /// <returns>
        /// True if an entry with the specified string key is present;
        /// otherwise, false.
        /// </returns>
        public bool ContainsKey( /* O(N) */
            string key /* in */
            )
        {
            foreach (KeyValuePair<IStringLongPair, string> pair in this)
            {
                IStringLongPair anyPair = pair.Key;

                if (anyPair == null)
                    continue;

                int compare = CompareStringKey(anyPair.X, key);

                if (compare == 0)
                    return true;
            }

            return false;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method removes all entries whose string key matches the
        /// specified string key.
        /// </summary>
        /// <param name="key">
        /// The string key of the entries to remove.
        /// </param>
        /// <returns>
        /// True if at least one entry was removed; otherwise, false.
        /// </returns>
        public bool Remove( /* O(N) */
            string key /* in */
            )
        {
            IList<IStringLongPair> keys = null;

            foreach (KeyValuePair<IStringLongPair, string> pair in this)
            {
                IStringLongPair anyPair = pair.Key;

                if (anyPair == null)
                    continue;

                int compare = CompareStringKey(anyPair.X, key);

                if (compare == 0)
                {
                    if (keys == null)
                        keys = new List<IStringLongPair>();

                    keys.Add(anyPair);
                }
            }

            if (keys != null)
            {
                int count = 0;

                foreach (IStringLongPair anyPair in keys)
                {
                    if (anyPair == null)
                        continue;

                    if (this.Remove(anyPair))
                        count++;
                }

                return (count > 0);
            }
            else
            {
                return false;
            }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Public Methods (Other)
        /// <summary>
        /// This method adds an entry for each item in the specified collection,
        /// using the string form of each item as the key and the specified
        /// value for every entry.
        /// </summary>
        /// <param name="collection">
        /// The collection of items whose string forms are added as keys.
        /// </param>
        /// <param name="value">
        /// The string value to associate with each added key.
        /// </param>
        public void AddKeys(
            IEnumerable collection, /* in */
            string value            /* in */
            )
        {
            foreach (object item in collection)
                this.Add(StringOps.GetStringFromObject(item), value);
        }

        ///////////////////////////////////////////////////////////////////////

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
        public string KeysAndValuesToString(
            string pattern, /* in */
            bool noCase     /* in */
            )
        {
            StringList list = GenericOps<IStringLongPair, string>.KeysAndValues(
                this, false, true, true, StringOps.DefaultMatchMode, pattern,
                null, null, null, null, noCase, RegexOptions.None) as StringList;

            return ParserOps<string>.ListToString(
                list, Index.Invalid, Index.Invalid, ToStringFlags.None,
                Characters.SpaceString, null, false);
        }
        #endregion
    }
}
