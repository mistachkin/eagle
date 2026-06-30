/*
 * EnumDictionary.cs --
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

#if SERIALIZATION
using System.Runtime.Serialization;
#endif

using Eagle._Attributes;
using Eagle._Components.Private;
using Eagle._Components.Public;
using Eagle._Constants;
using SharedStringOps = Eagle._Components.Shared.StringOps;

#if FAST_DICTIONARY
using SomeDictionary = Eagle._Containers.Public.FastDictionary<
    string, System.Enum>;
#else
using SomeDictionary = System.Collections.Generic.Dictionary<
    string, System.Enum>;
#endif

#if NET_STANDARD_21
using Index = Eagle._Constants.Index;
#endif

namespace Eagle._Containers.Public
{
    /// <summary>
    /// This class represents a dictionary that maps string names to enumerated
    /// values (instances of <see cref="Enum" />).  It extends the underlying
    /// generic dictionary with helpers for adding enumerated values keyed by
    /// their name, for converting stored values to a specific enumerated type,
    /// and for producing a filtered string form of its keys.
    /// </summary>
#if SERIALIZATION
    [Serializable()]
#endif
    [ObjectId("f09ea871-dac3-41d0-baca-454765dd8d08")]
    public sealed class EnumDictionary : SomeDictionary
    {
        #region Public Constructors
        /// <summary>
        /// Constructs an empty enumerated value dictionary that compares its
        /// string keys using the default system comparison semantics.
        /// </summary>
        public EnumDictionary()
            : base(new _Comparers.StringCustom(
                SharedStringOps.GetSystemComparisonType(true)))
        {
            // do nothing.
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Constructs an empty enumerated value dictionary that has the
        /// specified initial capacity and compares its string keys using the
        /// default system comparison semantics.
        /// </summary>
        /// <param name="capacity">
        /// The initial number of entries that the dictionary can contain
        /// before resizing is required.
        /// </param>
        public EnumDictionary(
            int capacity
            )
            : base(capacity, new _Comparers.StringCustom(
                SharedStringOps.GetSystemComparisonType(true)))
        {
            // do nothing.
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Constructs an enumerated value dictionary that is initialized with
        /// the entries copied from the specified dictionary and compares its
        /// string keys using the default system comparison semantics.
        /// </summary>
        /// <param name="dictionary">
        /// The dictionary whose key/value pairs are copied into the new
        /// dictionary.
        /// </param>
        public EnumDictionary(
            IDictionary<string, Enum> dictionary
            )
            : base(dictionary, new _Comparers.StringCustom(
                SharedStringOps.GetSystemComparisonType(true)))
        {
            // do nothing.
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Constructs an empty enumerated value dictionary that compares its
        /// string keys using the specified equality comparer.
        /// </summary>
        /// <param name="comparer">
        /// The equality comparer used when comparing string keys, or null to
        /// use the default comparer for the key type.
        /// </param>
        public EnumDictionary(
            IEqualityComparer<string> comparer
            )
            : base(comparer)
        {
            // do nothing.
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Constructs an enumerated value dictionary that is initialized with
        /// the entries copied from the specified dictionary and compares its
        /// string keys using the specified equality comparer.
        /// </summary>
        /// <param name="dictionary">
        /// The dictionary whose key/value pairs are copied into the new
        /// dictionary.
        /// </param>
        /// <param name="comparer">
        /// The equality comparer used when comparing string keys, or null to
        /// use the default comparer for the key type.
        /// </param>
        public EnumDictionary(
            IDictionary<string, Enum> dictionary,
            IEqualityComparer<string> comparer
            )
            : base(dictionary, comparer)
        {
            // do nothing.
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Constructs an enumerated value dictionary that compares its string
        /// keys using the default system comparison semantics and is
        /// initialized with the enumerated values contained in the specified
        /// collection.
        /// </summary>
        /// <param name="collection">
        /// The collection of enumerated values to add to the new dictionary;
        /// each element is expected to be an instance of <see cref="Enum" />.
        /// </param>
        public EnumDictionary(
            IEnumerable collection
            )
            : base(new _Comparers.StringCustom(
                SharedStringOps.GetSystemComparisonType(true)))
        {
            Add(collection);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Constructs an enumerated value dictionary that compares its string
        /// keys using the default system comparison semantics and is
        /// initialized with the enumerated values contained in the specified
        /// collection.
        /// </summary>
        /// <param name="collection">
        /// The collection of enumerated values to add to the new dictionary.
        /// </param>
        public EnumDictionary(
            IEnumerable<Enum> collection
            )
            : base(new _Comparers.StringCustom(
                SharedStringOps.GetSystemComparisonType(true)))
        {
            Add(collection);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Constructs an enumerated value dictionary that compares its string
        /// keys using the specified equality comparer and is initialized with
        /// the enumerated values contained in the specified collection.
        /// </summary>
        /// <param name="collection">
        /// The collection of enumerated values to add to the new dictionary.
        /// </param>
        /// <param name="comparer">
        /// The equality comparer used when comparing string keys, or null to
        /// use the default comparer for the key type.
        /// </param>
        public EnumDictionary(
            IEnumerable<Enum> collection,
            IEqualityComparer<string> comparer
            )
            : base(comparer)
        {
            Add(collection);
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Protected Constructors
#if SERIALIZATION
        /// <summary>
        /// Constructs an enumerated value dictionary from previously serialized
        /// data.  This constructor is used during deserialization.
        /// </summary>
        /// <param name="info">
        /// The object that holds the serialized data for the dictionary.
        /// </param>
        /// <param name="context">
        /// The streaming context that describes the source of the serialized
        /// data.
        /// </param>
        private EnumDictionary(
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

        /// <summary>
        /// This method adds the specified enumerated value to the dictionary,
        /// using its string representation as the key.
        /// </summary>
        /// <param name="item">
        /// The enumerated value to add to the dictionary.
        /// </param>
        public void Add(
            Enum item
            )
        {
            this.Add(item.ToString(), item);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method adds each enumerated value contained in the specified
        /// collection to the dictionary, using its string representation as
        /// the key.
        /// </summary>
        /// <param name="collection">
        /// The collection of enumerated values to add to the dictionary; each
        /// element is expected to be an instance of <see cref="Enum" />.
        /// </param>
        public void Add(
            IEnumerable collection
            )
        {
            foreach (object item in collection)
                this.Add((Enum)item);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method adds each enumerated value contained in the specified
        /// collection to the dictionary, using its string representation as
        /// the key.
        /// </summary>
        /// <param name="collection">
        /// The collection of enumerated values to add to the dictionary.
        /// </param>
        public void Add(
            IEnumerable<Enum> collection
            )
        {
            foreach (Enum item in collection)
                this.Add(item);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method attempts to look up the enumerated value associated with
        /// the specified key and convert it to the specified enumerated type.
        /// </summary>
        /// <param name="key">
        /// The name of the enumerated value to look up.
        /// </param>
        /// <param name="enumType">
        /// The enumerated type that the stored value should be converted to.
        /// </param>
        /// <param name="value">
        /// Upon success, receives the enumerated value converted to the
        /// specified type.  Upon failure, receives the converted zero value for
        /// the specified type.
        /// </param>
        /// <returns>
        /// True if the key was found in the dictionary; otherwise, false.
        /// </returns>
        public bool TryGetValue(
            string key,
            Type enumType,
            out object value
            )
        {
            Result error = null;

            return TryGetValue(key, enumType, out value, ref error);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method attempts to look up the enumerated value associated with
        /// the specified key and convert it to the specified enumerated type.
        /// </summary>
        /// <param name="key">
        /// The name of the enumerated value to look up.
        /// </param>
        /// <param name="enumType">
        /// The enumerated type that the stored value should be converted to.
        /// </param>
        /// <param name="value">
        /// Upon success, receives the enumerated value converted to the
        /// specified type.  Upon failure, receives the converted zero value for
        /// the specified type.
        /// </param>
        /// <param name="error">
        /// Upon failure, receives information about the error that was
        /// encountered while converting the value to the specified type.
        /// </param>
        /// <returns>
        /// True if the key was found in the dictionary; otherwise, false.
        /// </returns>
        public bool TryGetValue(
            string key,
            Type enumType,
            out object value,
            ref Result error
            )
        {
            Enum enumValue;

            if (this.TryGetValue(key, out enumValue))
            {
                value = EnumOps.TryGet(enumType, enumValue, ref error);

                return true;
            }
            else
            {
                value = EnumOps.TryGet(enumType, 0, ref error);

                return false;
            }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method produces a string that contains the keys of the
        /// dictionary, optionally filtered using the specified pattern,
        /// separated by spaces.
        /// </summary>
        /// <param name="pattern">
        /// The pattern used to filter the keys, or null to include every key.
        /// </param>
        /// <param name="noCase">
        /// Non-zero if the pattern matching should be case-insensitive.
        /// </param>
        /// <returns>
        /// The string containing the matching keys separated by spaces.
        /// </returns>
        public string ToString(
            string pattern,
            bool noCase
            )
        {
            return ToString(Characters.SpaceString, pattern, noCase);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method produces a string that contains the keys of the
        /// dictionary, optionally filtered using the specified pattern,
        /// separated by the specified separator.
        /// </summary>
        /// <param name="separator">
        /// The string used to separate adjacent keys in the resulting string.
        /// </param>
        /// <param name="pattern">
        /// The pattern used to filter the keys, or null to include every key.
        /// </param>
        /// <param name="noCase">
        /// Non-zero if the pattern matching should be case-insensitive.
        /// </param>
        /// <returns>
        /// The string containing the matching keys separated by the specified
        /// separator.
        /// </returns>
        public string ToString(
            string separator,
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
        /// This method produces a string that contains all of the keys of the
        /// dictionary, separated by spaces.
        /// </summary>
        /// <returns>
        /// The string containing the keys separated by spaces.
        /// </returns>
        public override string ToString()
        {
            return ToString(Characters.SpaceString, null, false);
        }
        #endregion
    }
}
