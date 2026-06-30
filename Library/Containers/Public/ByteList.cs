/*
 * ByteList.cs --
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
    /// This class represents a list of bytes.  It extends the standard generic
    /// list with the ability to be created from a string in the Eagle list
    /// format, to be converted back to that format, and to be cloned.
    /// </summary>
#if SERIALIZATION
    [Serializable()]
#endif
    [ObjectId("09838087-059f-477f-a224-679d6e983984")]
    public sealed class ByteList : List<byte>, ICloneable
    {
        #region Public Constructors
        /// <summary>
        /// Constructs an empty instance of this class.
        /// </summary>
        public ByteList()
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
        public ByteList(
            int capacity
            )
            : base(capacity)
        {
            // do nothing.
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Constructs an instance of this class that contains the elements
        /// copied from the specified collection of characters, each converted to
        /// its byte representation.
        /// </summary>
        /// <param name="collection">
        /// The collection of characters whose elements are copied into the new
        /// list.
        /// </param>
        public ByteList(
            IEnumerable<char> collection
            )
            : this()
        {
            foreach (char item in collection)
                this.Add(ConversionOps.ToByte(item));
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Constructs an instance of this class that contains the elements
        /// copied from the specified collection.
        /// </summary>
        /// <param name="collection">
        /// The collection whose elements are copied into the new list.
        /// </param>
        public ByteList(
            IEnumerable<byte> collection
            )
            : base(collection)
        {
            // do nothing.
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Constructs an instance of this class that contains the elements
        /// copied from the specified collection, optionally reversing their
        /// order.
        /// </summary>
        /// <param name="collection">
        /// The collection whose elements are copied into the new list.
        /// </param>
        /// <param name="reverse">
        /// Non-zero if the order of the copied elements should be reversed.
        /// </param>
        public ByteList(
            IEnumerable<byte> collection,
            bool reverse
            )
            : this(collection)
        {
            if (reverse)
                Reverse(); /* O(N) */
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Constructs an instance of this class that contains the bytes copied
        /// from the specified array, starting at the specified index.
        /// </summary>
        /// <param name="array">
        /// The array of bytes whose elements are copied into the new list.
        /// </param>
        /// <param name="startIndex">
        /// The index, within the array, of the first byte to copy.
        /// </param>
        public ByteList(
            byte[] array,
            int startIndex
            )
            : this()
        {
            Add(array, startIndex);
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Factory Methods
        /// <summary>
        /// Creates a new list from a string in the Eagle list format, where
        /// each element is parsed as a byte.
        /// </summary>
        /// <param name="value">
        /// The string, in the Eagle list format, to parse.
        /// </param>
        /// <returns>
        /// The new list, or null if the string could not be parsed.
        /// </returns>
        public static ByteList FromString(
            string value
            )
        {
            Result error = null;

            return FromString(value, ref error);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Creates a new list from a string in the Eagle list format, where
        /// each element is parsed as a byte.
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
        public static ByteList FromString(
            string value,
            ref Result error
            )
        {
            StringList list = null;

            if (ParserOps<string>.SplitList(
                    null, value, 0, Length.Invalid, true,
                    ref list, ref error) == ReturnCode.Ok)
            {
                ByteList list2 = new ByteList(list.Count);

                foreach (string element in list)
                {
                    byte byteValue = 0;

                    if (Value.GetByte2(element, ValueFlags.AnyByte, null,
                            ref byteValue, ref error) != ReturnCode.Ok)
                    {
                        return null;
                    }

                    list2.Add(byteValue);
                }

                return list2;
            }

            return null;
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Public Methods
        /// <summary>
        /// Appends the bytes copied from the specified array, starting at the
        /// specified index, to the end of this list.
        /// </summary>
        /// <param name="array">
        /// The array of bytes whose elements are appended to this list.
        /// </param>
        /// <param name="startIndex">
        /// The index, within the array, of the first byte to copy.
        /// </param>
        public void Add(
            byte[] array,
            int startIndex
            )
        {
            int newLength = array.Length - startIndex;

            byte[] newArray = new byte[newLength];

            Array.Copy(array, startIndex, newArray, 0, newLength);

            this.AddRange(newArray);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Appends the elements of the specified collection to the end of this
        /// list, unless the collection is null, in which case this method does
        /// nothing.
        /// </summary>
        /// <param name="collection">
        /// The collection whose elements are appended to this list.  This
        /// parameter may be null.
        /// </param>
        public void MaybeAddRange(
            IEnumerable<byte> collection
            )
        {
            if (collection == null)
                return;

            base.AddRange(collection);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Parses a string in the Eagle list format, where each element is
        /// parsed as a byte, and appends the resulting bytes to the end of this
        /// list.
        /// </summary>
        /// <param name="value">
        /// The string, in the Eagle list format, to parse.
        /// </param>
        /// <param name="error">
        /// Upon failure, receives information about the error.
        /// </param>
        /// <returns>
        /// True if the string was parsed and its bytes appended successfully;
        /// otherwise, false.
        /// </returns>
        public bool AddFromString(
            string value,
            ref Result error
            )
        {
            ByteList list = FromString(value, ref error);

            if (list == null)
                return false;

            this.AddRange(list);
            return true;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Computes a new list where each byte of this list is combined, using
        /// the bitwise exclusive-or operation, with a byte from the specified
        /// list.  The bytes of the specified list are reused cyclically when it
        /// is shorter than this list.
        /// </summary>
        /// <param name="list">
        /// The list of bytes to combine with the bytes of this list.
        /// </param>
        /// <returns>
        /// The new list containing the combined bytes, or null if the specified
        /// list is null, if either list is empty, or if the specified list is
        /// longer than this list.
        /// </returns>
        public ByteList Xor(
            IList<byte> list
            )
        {
            if (list == null)
                return null;

            int count1 = this.Count;

            if (count1 == 0)
                return null;

            int count2 = list.Count;

            if (count2 == 0)
                return null;

            if (count2 > count1)
                return null;

            ByteList result = new ByteList(count1);

            for (int index = 0; index < count1; index++)
            {
                result.Add((byte)(
                    this[index] ^ list[index % count2]
                ));
            }

            return result;
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
            return ParserOps<byte>.ListToString(
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
            return new ByteList(this);
        }
        #endregion
    }
}
