/*
 * UlongList.cs --
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
    /// This class represents a list of unsigned 64-bit integers
    /// (<see cref="ulong" />).
    /// </summary>
#if SERIALIZATION
    [Serializable()]
#endif
    [ObjectId("4ce0158e-36a3-429b-b793-f8c6622451aa")]
    public sealed class UlongList : List<ulong>, ICloneable
    {
        #region Public Constructors
        /// <summary>
        /// Constructs an empty list of unsigned 64-bit integers.
        /// </summary>
        public UlongList()
            : base()
        {
            // do nothing.
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Constructs an empty list of unsigned 64-bit integers that has the
        /// specified initial capacity.
        /// </summary>
        /// <param name="capacity">
        /// The number of values the new list can initially store without
        /// resizing.
        /// </param>
        public UlongList(
            int capacity
            )
            : base(capacity)
        {
            // do nothing.
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Constructs a list of unsigned 64-bit integers that contains the
        /// values copied from the specified collection.
        /// </summary>
        /// <param name="collection">
        /// The collection of values whose elements are copied into the new
        /// list.
        /// </param>
        public UlongList(
            IEnumerable<ulong> collection
            )
            : base(collection)
        {
            // do nothing.
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Factory Methods
        /// <summary>
        /// This method creates a list of unsigned 64-bit integers by parsing
        /// the elements of the specified string as a list.
        /// </summary>
        /// <param name="value">
        /// The string to parse into a list of unsigned 64-bit integers.  This
        /// parameter may be null.
        /// </param>
        /// <returns>
        /// The new list on success; otherwise, null.
        /// </returns>
        public static UlongList FromString(
            string value
            )
        {
            Result error = null;

            return FromString(value, ref error);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method creates a list of unsigned 64-bit integers by parsing
        /// the elements of the specified string as a list.
        /// </summary>
        /// <param name="value">
        /// The string to parse into a list of unsigned 64-bit integers.  This
        /// parameter may be null.
        /// </param>
        /// <param name="error">
        /// Upon failure, receives information about the error.
        /// </param>
        /// <returns>
        /// The new list on success; otherwise, null.
        /// </returns>
        public static UlongList FromString(
            string value,
            ref Result error
            )
        {
            StringList list = null;

            if (ParserOps<string>.SplitList(
                    null, value, 0, Length.Invalid, true,
                    ref list, ref error) == ReturnCode.Ok)
            {
                UlongList list2 = new UlongList(list.Count);

                foreach (string element in list)
                {
                    ulong ulongValue = 0;

                    if (Value.GetUnsignedWideInteger2(element,
                            ValueFlags.AnyWideInteger | ValueFlags.Unsigned,
                            null, ref ulongValue, ref error) != ReturnCode.Ok)
                    {
                        return null;
                    }

                    list2.Add(ulongValue);
                }

                return list2;
            }

            return null;
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region ToString Methods
        /// <summary>
        /// This method returns a string representation of this list, with the
        /// values separated by spaces.
        /// </summary>
        /// <param name="pattern">
        /// The optional pattern used to filter the values included in the
        /// result.  This parameter may be null.
        /// </param>
        /// <param name="noCase">
        /// Non-zero if pattern matching should be performed in a
        /// case-insensitive manner.
        /// </param>
        /// <returns>
        /// The string representation of this list.
        /// </returns>
        public string ToString(
            string pattern,
            bool noCase
            )
        {
            return ParserOps<ulong>.ListToString(
                this, Index.Invalid, Index.Invalid, ToStringFlags.None,
                Characters.SpaceString, pattern, noCase);
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region System.Object Overrides
        /// <summary>
        /// This method returns a string representation of this list, with the
        /// values separated by spaces.
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
        /// This method creates a new list of unsigned 64-bit integers that is a
        /// copy of this list.
        /// </summary>
        /// <returns>
        /// The new list that is a copy of this list.
        /// </returns>
        public object Clone()
        {
            return new UlongList(this);
        }
        #endregion
    }
}

