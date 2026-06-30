/*
 * IntList.cs --
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
    /// This class represents a list of 32-bit signed integers.  It extends the
    /// standard generic list with the ability to be created from a string in
    /// the Eagle list format, to be converted back to that format, and to be
    /// cloned.
    /// </summary>
#if SERIALIZATION
    [Serializable()]
#endif
    [ObjectId("778491dd-a2f1-4a89-85f0-a82b3d5d9555")]
    public sealed class IntList : List<int>, ICloneable
    {
        #region Public Constructors
        /// <summary>
        /// Constructs an empty instance of this class.
        /// </summary>
        public IntList()
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
        public IntList(
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
        public IntList(
            IEnumerable<int> collection
            )
            : base(collection)
        {
            // do nothing.
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Constructs an instance of this class that contains the elements
        /// copied from the specified collection of unsigned integers, each
        /// converted to its signed representation.
        /// </summary>
        /// <param name="collection">
        /// The collection of unsigned integers whose elements are copied into
        /// the new list.
        /// </param>
        public IntList(
            IEnumerable<uint> collection
            )
            : base()
        {
            Add(collection);
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Factory Methods
        /// <summary>
        /// Creates a new list from a string in the Eagle list format, where
        /// each element is parsed as an integer.
        /// </summary>
        /// <param name="value">
        /// The string, in the Eagle list format, to parse.
        /// </param>
        /// <returns>
        /// The new list, or null if the string could not be parsed.
        /// </returns>
        public static IntList FromString(
            string value
            )
        {
            Result error = null;

            return FromString(value, ref error);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Creates a new list from a string in the Eagle list format, where
        /// each element is parsed as an integer.
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
        public static IntList FromString(
            string value,
            ref Result error
            )
        {
            StringList list = null;

            if (ParserOps<string>.SplitList(
                    null, value, 0, Length.Invalid, true,
                    ref list, ref error) == ReturnCode.Ok)
            {
                IntList list2 = new IntList(list.Count);

                foreach (string element in list)
                {
                    int intValue = 0;

                    if (Value.GetInteger2(element, ValueFlags.AnyInteger, null,
                            ref intValue, ref error) != ReturnCode.Ok)
                    {
                        return null;
                    }

                    list2.Add(intValue);
                }

                return list2;
            }

            return null;
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Add Methods
        /// <summary>
        /// Appends the elements of the specified collection of unsigned
        /// integers to the end of this list, each converted to its signed
        /// representation.
        /// </summary>
        /// <param name="collection">
        /// The collection of unsigned integers whose elements are appended to
        /// this list.
        /// </param>
        public void Add(
            IEnumerable<uint> collection
            )
        {
            foreach (uint element in collection)
                Add(ConversionOps.ToInt(element));
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
            return ParserOps<int>.ListToString(
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
            return new IntList(this);
        }
        #endregion
    }
}
