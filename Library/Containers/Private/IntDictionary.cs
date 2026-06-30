/*
 * IntDictionary.cs --
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
using System.Globalization;

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

#if FAST_DICTIONARY
using SomeDictionary = Eagle._Containers.Public.FastDictionary<string, int>;
#else
using SomeDictionary = System.Collections.Generic.Dictionary<string, int>;
#endif

#if NET_STANDARD_21
using Index = Eagle._Constants.Index;
#endif

namespace Eagle._Containers.Private
{
    /// <summary>
    /// This class represents a dictionary that maps string keys to integer
    /// values.  It extends the underlying generic dictionary of integers with
    /// helpers for populating itself from collections of strings, for counting
    /// occurrences of keys, for serializing to and from a flat string form, and
    /// for producing filtered string forms of its keys and values.
    /// </summary>
#if SERIALIZATION
    [Serializable()]
#endif
    [ObjectId("d0f3e89e-8835-471a-aaff-81a0b97c49ef")]
    internal sealed class IntDictionary : SomeDictionary
    {
        #region Public Constructors
        /// <summary>
        /// Constructs an empty integer dictionary.
        /// </summary>
        public IntDictionary()
            : base()
        {
            // do nothing.
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Constructs an empty integer dictionary that is sized to hold the
        /// specified number of entries without further growth.
        /// </summary>
        /// <param name="capacity">
        /// The initial number of entries the dictionary can contain.
        /// </param>
        public IntDictionary(
            int capacity
            )
            : base(capacity)
        {
            // do nothing.
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Constructs an empty integer dictionary that uses the specified
        /// equality comparer when comparing keys.
        /// </summary>
        /// <param name="comparer">
        /// The equality comparer used to compare keys, or null to use the
        /// default comparer.
        /// </param>
        public IntDictionary(
            IEqualityComparer<string> comparer
            )
            : base(comparer)
        {
            // do nothing.
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Constructs an integer dictionary that is initialized with the entries
        /// copied from the specified dictionary.
        /// </summary>
        /// <param name="dictionary">
        /// The dictionary whose key/value pairs are copied into the new
        /// dictionary.
        /// </param>
        public IntDictionary(
            IDictionary<string, int> dictionary
            )
            : base(dictionary)
        {
            // do nothing.
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Constructs an integer dictionary that is populated from the specified
        /// collection of name/value pairs, accumulating values for duplicate
        /// keys.
        /// </summary>
        /// <param name="collection">
        /// The collection of strings, each of which is parsed as a list whose
        /// first element is a key and whose second element is an integer value.
        /// </param>
        /// <param name="cultureInfo">
        /// The culture used when parsing the integer values.
        /// </param>
        public IntDictionary(
            IEnumerable<string> collection,
            CultureInfo cultureInfo
            )
            : this()
        {
            Add(collection, cultureInfo);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Constructs an integer dictionary that counts the occurrences of each
        /// key in the specified collection.
        /// </summary>
        /// <param name="collection">
        /// The collection of keys whose occurrences are counted.
        /// </param>
        public IntDictionary(
            IEnumerable<string> collection
            )
            : this()
        {
            AddKeys(collection);
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Protected Constructors
#if SERIALIZATION
        /// <summary>
        /// Constructs an integer dictionary from previously serialized data.
        /// This constructor is used during deserialization.
        /// </summary>
        /// <param name="info">
        /// The object that holds the serialized data for the dictionary.
        /// </param>
        /// <param name="context">
        /// The streaming context that describes the source of the serialized
        /// data.
        /// </param>
        private IntDictionary(
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

        #region Public Static Methods
        /// <summary>
        /// This method serializes the specified integer dictionary into a flat
        /// string consisting of alternating keys and values.
        /// </summary>
        /// <param name="dictionary">
        /// The integer dictionary to serialize.
        /// </param>
        /// <param name="error">
        /// Upon failure, receives information about the error.
        /// </param>
        /// <returns>
        /// The serialized string form of the dictionary, or null if it could
        /// not be serialized.
        /// </returns>
        public static string FastSerialize(
            IntDictionary dictionary,
            ref Result error
            )
        {
            if (dictionary == null)
            {
                error = "invalid dictionary value";
                return null;
            }

            int count = dictionary.Count;

            if (count == 0)
                return String.Empty;

            //
            // HACK: Using StringList in this method feels a bit too heavy;
            //       however, it is certainly easy.
            //
            StringList list = new StringList(count * 2);

            foreach (KeyValuePair<string, int> pair in dictionary)
            {
                list.Add(pair.Key);
                list.Add(pair.Value.ToString(CultureInfo.InvariantCulture));
            }

            return list.ToString();
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method deserializes an integer dictionary from a flat string
        /// consisting of alternating keys and values, as produced by
        /// <see cref="FastSerialize" />.
        /// </summary>
        /// <param name="value">
        /// The serialized string form of the dictionary.
        /// </param>
        /// <param name="failOnError">
        /// Non-zero to abandon deserialization and return null upon encountering
        /// a malformed element; zero to stop processing at the malformed element
        /// and return the entries parsed so far.
        /// </param>
        /// <param name="error">
        /// Upon failure, receives information about the error.
        /// </param>
        /// <returns>
        /// The deserialized integer dictionary, or null if it could not be
        /// deserialized.
        /// </returns>
        public static IntDictionary FastDeserialize(
            string value,
            bool failOnError,
            ref Result error
            )
        {
            if (value == null)
            {
                error = "invalid string value";
                return null;
            }

            //
            // HACK: Using StringList in this method feels a bit too heavy;
            //       however, it is certainly easy.
            //
            StringList list = StringList.FromString(value, ref error);

            if (list == null)
                return null;

            int count = list.Count;

            if (count == 0)
                return new IntDictionary();

            IntDictionary dictionary = new IntDictionary(count / 2);

            for (int index = 0; index < count; index += 2)
            {
                int nextIndex = index + 1;

                if (nextIndex >= count)
                {
                    error = String.Format(
                        "integer element at index {0} missing", nextIndex);

                    if (failOnError)
                        return null;
                    else
                        break;
                }

                string stringValue = list[nextIndex];
                int intValue;

                if (!int.TryParse(stringValue,
                        NumberStyles.Integer, CultureInfo.InvariantCulture,
                        out intValue))
                {
                    error = String.Format(
                        "list element {0} at index {1} is not an integer",
                        FormatOps.WrapOrNull(stringValue), nextIndex);

                    if (failOnError)
                        return null;
                    else
                        break;
                }

                string localKey = list[index];

                if (localKey == null)
                {
                    error = String.Format(
                        "list element {0} at index {1} is an invalid key",
                        FormatOps.WrapOrNull(localKey), index);

                    if (failOnError)
                        return null;
                    else
                        break;
                }

                dictionary.Add(localKey, intValue);
            }

            return dictionary;
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Public Methods
        /// <summary>
        /// This method adds the name/value pairs contained in the specified
        /// collection to the dictionary, accumulating values for keys that are
        /// already present.  Elements that cannot be parsed into a key and a
        /// valid integer value are silently ignored.
        /// </summary>
        /// <param name="collection">
        /// The collection of strings, each of which is parsed as a list whose
        /// first element is a key and whose second element is an integer value.
        /// </param>
        /// <param name="cultureInfo">
        /// The culture used when parsing the integer values.
        /// </param>
        public void Add(
            IEnumerable<string> collection,
            CultureInfo cultureInfo
            )
        {
            foreach (string item in collection)
            {
                //
                // NOTE: We require a list of lists to process into name/value
                //       pairs.  The name is always a string and the value must
                //       parse as a valid integer.
                //
                StringList list = null;

                if (ParserOps<string>.SplitList(
                        null, item, 0, Length.Invalid, true,
                        ref list) == ReturnCode.Ok)
                {
                    //
                    // NOTE: We require at least a name and a value, extra
                    //       elements are silently ignored.
                    //
                    if (list.Count >= 2)
                    {
                        string key = list[0];

                        //
                        // NOTE: *WARNING* Empty array element names are
                        //       allowed, please do not change this to
                        //       "!String.IsNullOrEmpty".
                        //
                        if (key != null)
                        {
                            //
                            // NOTE: Attempt to parse the list element as a
                            //       valid integer; if not, it will be silently
                            //       ignored.
                            //
                            int value = 0;

                            if (Value.GetInteger2(list[1], ValueFlags.AnyInteger,
                                    cultureInfo, ref value) == ReturnCode.Ok)
                            {
                                if (this.ContainsKey(key))
                                    this[key] += value;
                                else
                                    this.Add(key, value);
                            }
                        }
                    }
                }
            }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method counts the occurrences of each key in the specified
        /// collection, incrementing the value stored for keys that are already
        /// present and adding new keys with a value of one.  Null elements are
        /// ignored.
        /// </summary>
        /// <param name="collection">
        /// The collection of keys whose occurrences are counted.
        /// </param>
        public void AddKeys(
            IEnumerable<string> collection
            )
        {
            foreach (string item in collection)
            {
                if (item == null)
                    continue;

                int value;

                if (TryGetValue(item, out value))
                    value += 1;
                else
                    value = 1;

                this[item] = value;
            }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method produces a string containing the keys and values of the
        /// dictionary whose keys match the specified pattern.
        /// </summary>
        /// <param name="pattern">
        /// The pattern used to select which keys (and their values) are
        /// included.  This parameter may be null to include all entries.
        /// </param>
        /// <param name="noCase">
        /// Non-zero if pattern matching should be case-insensitive.
        /// </param>
        /// <returns>
        /// The matching keys and values formatted as a string.
        /// </returns>
        public string KeysAndValuesToString(
            string pattern,
            bool noCase
            )
        {
            StringList list = GenericOps<string, int>.KeysAndValues(
                this, false, true, true, StringOps.DefaultMatchMode, pattern,
                null, null, null, null, noCase, RegexOptions.None) as StringList;

            return ParserOps<string>.ListToString(
                list, Index.Invalid, Index.Invalid, ToStringFlags.None,
                Characters.SpaceString, null, false);
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
        #endregion

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
