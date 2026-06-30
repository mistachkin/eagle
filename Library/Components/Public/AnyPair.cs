/*
 * AnyPair.cs --
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
using Eagle._Containers.Public;
using Eagle._Interfaces.Private;
using Eagle._Interfaces.Public;

namespace Eagle._Components.Public
{
    /// <summary>
    /// This class represents an immutable pair of values, each of a possibly
    /// distinct type.  It supports comparison, equality, ordering, and several
    /// string-formatting conventions, and provides implicit conversions from
    /// either value type.
    /// </summary>
#if SERIALIZATION
    [Serializable()]
#endif
    [ObjectId("2dacac40-7b18-4fc6-841a-35ffa7550dc9")]
    public class AnyPair<T1, T2> :
        IPair,
        IAnyPair,
        IAnyPair<T1, T2>,
        IComparer<IAnyPair<T1, T2>>,
        IComparable<IAnyPair<T1, T2>>,
        IEquatable<IAnyPair<T1, T2>>,
        IComparable,
        IToString
    {
        #region Public Constructors
        //
        // WARNING: This constructor produces an immutable null pair object.
        //
        /// <summary>
        /// Constructs an empty pair whose values are both the default for their
        /// respective types.
        /// </summary>
        public AnyPair()
            : base()
        {
            // do nothing.
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Constructs a pair with the specified first value; the second value
        /// is the default for its type.
        /// </summary>
        /// <param name="x">
        /// The first value of the pair.
        /// </param>
        public AnyPair(
            T1 x
            )
            : this()
        {
            this.x = x;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Constructs a pair with the specified first and second values.
        /// </summary>
        /// <param name="x">
        /// The first value of the pair.
        /// </param>
        /// <param name="y">
        /// The second value of the pair.
        /// </param>
        public AnyPair(
            T1 x,
            T2 y
            )
            : this(x)
        {
            this.y = y;
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region IAnyPair Members
        /// <summary>
        /// Gets the first value of this pair as an object.
        /// </summary>
        object IAnyPair.X
        {
            get { return x; }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Gets the second value of this pair as an object.
        /// </summary>
        object IAnyPair.Y
        {
            get { return y; }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region IAnyPair<T1, T2> Members
        /// <summary>
        /// The first value of this pair.
        /// </summary>
        private T1 x;
        /// <summary>
        /// Gets the first value of this pair.
        /// </summary>
        public virtual T1 X
        {
            get { return x; }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// The second value of this pair.
        /// </summary>
        private T2 y;
        /// <summary>
        /// Gets the second value of this pair.
        /// </summary>
        public virtual T2 Y
        {
            get { return y; }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Static Conversion Helpers
        /// <summary>
        /// This method creates a pair from the specified first value; the
        /// second value is the default for its type.
        /// </summary>
        /// <param name="value">
        /// The first value of the pair.
        /// </param>
        /// <returns>
        /// The newly created pair.
        /// </returns>
        public static AnyPair<T1, T2> FromType1(
            T1 value
            )
        {
            return new AnyPair<T1, T2>(
                value, default(T2));
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method creates a pair from the specified second value; the
        /// first value is the default for its type.
        /// </summary>
        /// <param name="value">
        /// The second value of the pair.
        /// </param>
        /// <returns>
        /// The newly created pair.
        /// </returns>
        public static AnyPair<T1, T2> FromType2(
            T2 value
            )
        {
            return new AnyPair<T1, T2>(
                default(T1), value);
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Conversion Operators
        /// <summary>
        /// This operator implicitly converts the specified value into a pair
        /// containing it as the first value.
        /// </summary>
        /// <param name="value">
        /// The first value of the pair.
        /// </param>
        /// <returns>
        /// The newly created pair.
        /// </returns>
        public static implicit operator AnyPair<T1, T2>(
            T1 value
            )
        {
            return FromType1(value);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This operator implicitly converts the specified value into a pair
        /// containing it as the second value.
        /// </summary>
        /// <param name="value">
        /// The second value of the pair.
        /// </param>
        /// <returns>
        /// The newly created pair.
        /// </returns>
        public static implicit operator AnyPair<T1, T2>(
            T2 value
            )
        {
            return FromType2(value);
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region System.Object Overrides
        /// <summary>
        /// This method determines whether the specified object is a pair equal
        /// to this pair.
        /// </summary>
        /// <param name="obj">
        /// The object to compare with this pair.  This parameter may be null.
        /// </param>
        /// <returns>
        /// True if the specified object is a pair with equal values; otherwise,
        /// false.
        /// </returns>
        public override bool Equals(
            object obj
            )
        {
            IAnyPair<T1, T2> anyPair =
                obj as IAnyPair<T1, T2>;

            if (anyPair != null)
            {
                return GenericOps<T1>.Equals(this.X, anyPair.X) &&
                       GenericOps<T2>.Equals(this.Y, anyPair.Y);
            }
            else
            {
                return false;
            }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method returns the string form of this pair, formatted as a
        /// list of its values.
        /// </summary>
        /// <returns>
        /// The string form of this pair.
        /// </returns>
        public override string ToString()
        {
            return StringList.MakeList(this.X, this.Y);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method returns a hash code for this pair, combining the hash
        /// codes of its values.
        /// </summary>
        /// <returns>
        /// A hash code for this pair.
        /// </returns>
        public override int GetHashCode()
        {
            return CommonOps.HashCodes.Combine(
                GenericOps<T1>.GetHashCode(this.X),
                GenericOps<T2>.GetHashCode(this.Y));
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region IComparer<IAnyPair<T1, T2>> Members
        /// <summary>
        /// This method compares two pairs, ordering by their first values and
        /// then by their second values.
        /// </summary>
        /// <param name="x">
        /// The first pair to compare.  This parameter may be null.
        /// </param>
        /// <param name="y">
        /// The second pair to compare.  This parameter may be null.
        /// </param>
        /// <returns>
        /// Zero if the pairs are equal, a negative number if the first pair is
        /// less than the second, or a positive number if the first pair is
        /// greater than the second.
        /// </returns>
        public virtual int Compare(
            IAnyPair<T1, T2> x,
            IAnyPair<T1, T2> y
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
                int result = Comparer<T1>.Default.Compare(x.X, y.X);

                if (result != 0)
                    return result;

                return Comparer<T2>.Default.Compare(x.Y, y.Y);
            }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region IComparable<IAnyPair<T1, T2>> Members
        /// <summary>
        /// This method compares this pair to another pair.
        /// </summary>
        /// <param name="other">
        /// The pair to compare to this pair.  This parameter may be null.
        /// </param>
        /// <returns>
        /// Zero if the pairs are equal, a negative number if this pair is less
        /// than the other pair, or a positive number if this pair is greater
        /// than the other pair.
        /// </returns>
        public virtual int CompareTo(
            IAnyPair<T1, T2> other
            )
        {
            return Compare(this, other);
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region IEquatable<IAnyPair<T1, T2>> Members
        /// <summary>
        /// This method determines whether this pair is equal to another pair.
        /// </summary>
        /// <param name="other">
        /// The pair to compare with this pair.  This parameter may be null.
        /// </param>
        /// <returns>
        /// True if the two pairs have equal values; otherwise, false.
        /// </returns>
        public virtual bool Equals(
            IAnyPair<T1, T2> other
            )
        {
            return CompareTo(other) == 0;
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region IComparable Members
        /// <summary>
        /// This method compares this pair to another object, which must be a
        /// pair.
        /// </summary>
        /// <param name="obj">
        /// The object to compare to this pair.  It must be a pair.
        /// </param>
        /// <returns>
        /// Zero if the pairs are equal, a negative number if this pair is less
        /// than the other pair, or a positive number if this pair is greater
        /// than the other pair.
        /// </returns>
        public virtual int CompareTo(
            object obj
            )
        {
            IAnyPair<T1, T2> anyPair =
                obj as IAnyPair<T1, T2>;

            if (anyPair == null)
                throw new ArgumentException();

            return CompareTo(anyPair);
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region IToString Members
        /// <summary>
        /// This method returns the string form of this pair, subject to the
        /// specified flags.
        /// </summary>
        /// <param name="flags">
        /// The flags that control how the string form is produced.
        /// </param>
        /// <returns>
        /// The string form of this pair.
        /// </returns>
        public virtual string ToString(
            ToStringFlags flags
            )
        {
            return ToString(flags, null);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method returns the string form of this pair, subject to the
        /// specified flags and default value.
        /// </summary>
        /// <param name="flags">
        /// The flags that control how the string form is produced.  This
        /// parameter is not used.
        /// </param>
        /// <param name="default">
        /// The default value to return.  This parameter is not used.
        /// </param>
        /// <returns>
        /// The string form of this pair.
        /// </returns>
        public virtual string ToString(
            ToStringFlags flags, /* NOT USED */
            string @default /* NOT USED */
            )
        {
            return ToString();
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method returns the string form of this pair, using the
        /// specified composite format string.
        /// </summary>
        /// <param name="format">
        /// The composite format string used to format the values of this pair.
        /// </param>
        /// <returns>
        /// The formatted string form of this pair.
        /// </returns>
        public virtual string ToString(
            string format
            )
        {
            return String.Format(format, this.X, this.Y);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method returns the string form of this pair, using the
        /// specified composite format string, limited to the specified length.
        /// </summary>
        /// <param name="format">
        /// The composite format string used to format the values of this pair.
        /// </param>
        /// <param name="limit">
        /// The maximum length of the resulting string; longer strings are
        /// truncated with an ellipsis.
        /// </param>
        /// <param name="strict">
        /// Non-zero to strictly enforce the length limit.
        /// </param>
        /// <returns>
        /// The formatted, possibly truncated, string form of this pair.
        /// </returns>
        public virtual string ToString(
            string format,
            int limit,
            bool strict
            )
        {
            return FormatOps.Ellipsis(
                String.Format(format, this.X, this.Y), limit, strict);
        }
        #endregion
    }
}
