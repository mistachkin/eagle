/*
 * CharList.cs --
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
using System.Text;
using Eagle._Attributes;
using Eagle._Components.Private;
using Eagle._Components.Public;
using Eagle._Constants;

#if NET_STANDARD_21
using Index = Eagle._Constants.Index;
#endif

namespace Eagle._Containers.Private
{
    /// <summary>
    /// This class represents a list of characters.  It extends the standard
    /// generic list with bulk-append helpers, conversion to the Eagle string
    /// list format (with optional pattern matching), and support for cloning.
    /// </summary>
    [ObjectId("a498e733-db5d-4111-ae0c-222948e8543c")]
    internal sealed class CharList : List<char>, ICloneable
    {
        /// <summary>
        /// Constructs an empty instance of this class.
        /// </summary>
        public CharList()
            : base()
        {
            // do nothing.
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Constructs an empty instance of this class that has the specified
        /// initial capacity.
        /// </summary>
        /// <param name="capacity">
        /// The number of elements that the new list can initially store.
        /// </param>
        public CharList(
            int capacity
            )
            : base(capacity)
        {
            // do nothing.
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Constructs an instance of this class that contains the elements
        /// copied from the specified collection.
        /// </summary>
        /// <param name="collection">
        /// The collection whose elements are copied into the new list.
        /// </param>
        public CharList(
            IEnumerable<char> collection
            )
            : base(collection)
        {
            // do nothing.
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        #region Dead Code
#if DEAD_CODE
        /// <summary>
        /// Constructs an instance of this class that contains the elements
        /// copied from the specified collection, with each byte converted to
        /// its corresponding character.
        /// </summary>
        /// <param name="collection">
        /// The collection of bytes whose elements are converted to characters
        /// and copied into the new list.
        /// </param>
        private CharList(
            IEnumerable<byte> collection
            )
        {
            foreach (byte item in collection)
                this.Add((char)item);
        }
#endif
        #endregion

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Constructs an instance of this class that contains the elements
        /// copied from each of the specified collections.
        /// </summary>
        /// <param name="collections">
        /// The array of collections whose elements are copied, in order, into
        /// the new list.
        /// </param>
        public CharList(
            params IEnumerable<char>[] collections
            )
            : base()
        {
            foreach (IEnumerable<char> item in collections)
                Add(item);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method appends all of the elements of the specified collection
        /// to the end of this list.
        /// </summary>
        /// <param name="collection">
        /// The collection whose elements are appended to this list.
        /// </param>
        public void Add(
            IEnumerable<char> collection
            )
        {
            foreach (char item in collection)
                this.Add(item);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        #region Dead Code
#if DEAD_CODE
        /// <summary>
        /// This method produces a string by concatenating all of the characters
        /// in this list, without any separators or other formatting.
        /// </summary>
        /// <returns>
        /// The concatenation of all characters in this list.
        /// </returns>
        public string ToRawString()
        {
            StringBuilder result = StringBuilderFactory.Create();

            foreach (char element in this)
                    result.Append(element);

            return StringBuilderCache.GetStringAndRelease(ref result);
        }
#endif
        #endregion

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Converts this list to a string in the Eagle list format, optionally
        /// including only those elements matching the specified pattern.
        /// </summary>
        /// <param name="pattern">
        /// The pattern that each element must match in order to be included in
        /// the resulting string.  This parameter may be null, in which case all
        /// elements are included.
        /// </param>
        /// <param name="noCase">
        /// Non-zero if pattern matching should be case-insensitive.
        /// </param>
        /// <returns>
        /// The string representation of this list.
        /// </returns>
        public string ToString(
            string pattern,
            bool noCase
            )
        {
            return ParserOps<char>.ListToString(this, Index.Invalid, Index.Invalid,
                ToStringFlags.None, Characters.SpaceString, pattern, noCase);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        #region System.Object Overrides
        /// <summary>
        /// Converts this list to a string in the Eagle list format.
        /// </summary>
        /// <returns>
        /// The string representation of this list.
        /// </returns>
        public override string ToString()
        {
            return ToString(null, false);
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////////////////////////////

        #region ICloneable Members
        /// <summary>
        /// This method creates a new list that is a copy of this list.
        /// </summary>
        /// <returns>
        /// The newly created copy of this list.
        /// </returns>
        public object Clone()
        {
            return new CharList(this);
        }
        #endregion
    }
}
