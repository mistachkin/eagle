/*
 * LongList.cs --
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

#if NET_STANDARD_21
using Index = Eagle._Constants.Index;
#endif

namespace Eagle._Containers.Public
{
    /// <summary>
    /// This class represents a list of 64-bit signed integers.  It extends the
    /// standard generic list with the ability to be created from a string in
    /// the Eagle list format, to be converted back to that format, and to be
    /// cloned.
    /// </summary>
#if SERIALIZATION
    [Serializable()]
#endif
    [ObjectId("fe3fadb3-f497-4e7c-a5fb-d7209a1c9f37")]
    public sealed class LongList : List<long>, ICloneable
    {
        #region Public Constructors
        /// <summary>
        /// Constructs an empty instance of this class.
        /// </summary>
        public LongList()
            : base()
        {
            // do nothing.
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Constructs an empty instance of this class that has the specified
        /// initial capacity.
        /// </summary>
        /// <param name="capacity">
        /// The number of elements that the new list can initially store.
        /// </param>
        public LongList(
            int capacity
            )
            : base(capacity)
        {
            // do nothing.
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Constructs an instance of this class that contains the elements
        /// copied from the specified collection.
        /// </summary>
        /// <param name="collection">
        /// The collection whose elements are copied into the new list.
        /// </param>
        public LongList(
            IEnumerable<long> collection
            )
            : base(collection)
        {
            // do nothing.
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Factory Methods
        /// <summary>
        /// Creates a new list from a string in the Eagle list format, where
        /// each element is parsed as a wide (64-bit) integer.
        /// </summary>
        /// <param name="value">
        /// The string, in the Eagle list format, to parse.
        /// </param>
        /// <returns>
        /// The new list, or null if the string could not be parsed.
        /// </returns>
        public static LongList FromString(
            string value
            )
        {
            Result error = null;

            return FromString(value, ref error);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Creates a new list from a string in the Eagle list format, where
        /// each element is parsed as a wide (64-bit) integer.
        /// </summary>
        /// <param name="value">
        /// The string, in the Eagle list format, to parse.
        /// </param>
        /// <param name="error">
        /// Upon failure, receives information about the error.
        /// </param>
        /// <returns>
        /// The new list, or null if the string could not be parsed.
        /// </returns>
        public static LongList FromString(
            string value,
            ref Result error
            )
        {
            StringList list = null;

            if (ParserOps<string>.SplitList(
                    null, value, 0, Length.Invalid, true,
                    ref list, ref error) == ReturnCode.Ok)
            {
                LongList list2 = new LongList(list.Count);

                foreach (string element in list)
                {
                    long longValue = 0;

                    if (Value.GetWideInteger2(
                            element, ValueFlags.AnyWideInteger, null,
                            ref longValue, ref error) != ReturnCode.Ok)
                    {
                        return null;
                    }

                    list2.Add(longValue);
                }

                return list2;
            }

            return null;
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Public Methods
        /// <summary>
        /// Removes every occurrence of each element of the specified collection
        /// from this list.  If the specified collection is this same list, the
        /// list is simply cleared.
        /// </summary>
        /// <param name="collection">
        /// The collection of elements to remove from this list.  This parameter
        /// may not be null.
        /// </param>
        public void RemoveRange( /* O(N^2) */
            IEnumerable<long> collection
            )
        {
            if (collection == null)
                throw new ArgumentNullException("collection");

            if (Object.ReferenceEquals(collection, this))
            {
                this.Clear();
            }
            else
            {
                foreach (long item in collection)
                {
                    int count = this.Count;

                    for (int index = count - 1; index >= 0; index--)
                        if (this[index] == item)
                            this.RemoveAt(index);
                }
            }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region ToString Methods
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
            return ParserOps<long>.ListToString(
                this, Index.Invalid, Index.Invalid, ToStringFlags.None,
                Characters.SpaceString, pattern, noCase);
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

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

        ///////////////////////////////////////////////////////////////////////

        #region ICloneable Members
        /// <summary>
        /// Creates a new list that is a shallow copy of this list.
        /// </summary>
        /// <returns>
        /// The newly created copy of this list.
        /// </returns>
        public object Clone()
        {
            return new LongList(this);
        }
        #endregion
    }
}
