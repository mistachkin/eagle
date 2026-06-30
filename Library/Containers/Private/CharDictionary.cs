/*
 * CharDictionary.cs --
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
using Eagle._Attributes;
using Eagle._Components.Private;
using Eagle._Components.Public;
using Eagle._Constants;
using Eagle._Containers.Public;

#if FAST_DICTIONARY
using SomeDictionary = Eagle._Containers.Public.FastDictionary<char, object>;
#else
using SomeDictionary = System.Collections.Generic.Dictionary<char, object>;
#endif

#if NET_STANDARD_21
using Index = Eagle._Constants.Index;
#endif

namespace Eagle._Containers.Private
{
    /// <summary>
    /// This class represents a dictionary that maps characters to arbitrary
    /// objects.  It can be populated from a sequence of characters (where each
    /// character is mapped to its insertion ordinal), converted to a string,
    /// and cloned.
    /// </summary>
    [ObjectId("a292e544-fcf4-4ca7-9e13-a7978c14ebbb")]
    internal sealed class CharDictionary : SomeDictionary, ICloneable
    {
        /// <summary>
        /// Constructs an empty instance of this class.
        /// </summary>
        public CharDictionary()
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
        public CharDictionary(
            IDictionary<char, object> dictionary
            )
            : base(dictionary)
        {
            // do nothing.
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Constructs an instance of this class that contains the characters
        /// from the specified collection, each mapped to its insertion
        /// ordinal.
        /// </summary>
        /// <param name="collection">
        /// The collection of characters to add to the new dictionary.
        /// </param>
        public CharDictionary(
            IEnumerable<char> collection
            )
            : this()
        {
            Add(collection);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        #region Dead Code
#if DEAD_CODE
        /// <summary>
        /// Constructs an empty instance of this class that has the specified
        /// initial capacity.
        /// </summary>
        /// <param name="capacity">
        /// The number of elements that the new dictionary can initially store.
        /// </param>
        private CharDictionary(
            int capacity
            )
            : base(capacity)
        {
            // do nothing.
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Constructs an instance of this class that contains the bytes from
        /// the specified collection, each converted to a character and mapped
        /// to its insertion ordinal.
        /// </summary>
        /// <param name="collection">
        /// The collection of bytes to add to the new dictionary.
        /// </param>
        private CharDictionary(
            IEnumerable<byte> collection
            )
            : this()
        {
            Add(collection);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method adds the bytes from the specified collection to the
        /// dictionary, each converted to a character and mapped to its
        /// insertion ordinal.
        /// </summary>
        /// <param name="collection">
        /// The collection of bytes to add to the dictionary.
        /// </param>
        private void Add(
            IEnumerable<byte> collection
            )
        {
            foreach (byte item in collection)
                this.Add((char)item, this.Count);
        }
#endif
        #endregion

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method adds the characters from the specified collection to the
        /// dictionary, each mapped to its insertion ordinal.
        /// </summary>
        /// <param name="collection">
        /// The collection of characters to add to the dictionary.
        /// </param>
        public void Add(
            IEnumerable<char> collection
            )
        {
            foreach (char item in collection)
                this.Add(item, this.Count);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

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

            return ParserOps<string>.ListToString(list, Index.Invalid, Index.Invalid,
                ToStringFlags.None, Characters.SpaceString, pattern, noCase);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

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

        ///////////////////////////////////////////////////////////////////////////////////////////////

        #region ICloneable Members
        /// <summary>
        /// Creates a new dictionary that is a shallow copy of this dictionary.
        /// </summary>
        /// <returns>
        /// The newly created copy of this dictionary.
        /// </returns>
        public object Clone()
        {
            return new CharDictionary(this);
        }
        #endregion
    }
}
