/*
 * AnyTriplet.cs --
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
    /// This class represents an immutable triplet of values, each of a possibly
    /// distinct type.  It supports comparison, equality, ordering, and several
    /// string-formatting conventions, and provides implicit conversions from
    /// any of the three value types.
    /// </summary>
#if SERIALIZATION
    [Serializable()]
#endif
    [ObjectId("ddf3e5a3-1536-40ee-93cf-2194ad259880")]
    public class AnyTriplet<T1, T2, T3> :
        ITriplet,
        IAnyTriplet,
        IAnyTriplet<T1, T2, T3>,
        IComparer<IAnyTriplet<T1, T2, T3>>,
        IComparable<IAnyTriplet<T1, T2, T3>>,
        IEquatable<IAnyTriplet<T1, T2, T3>>,
        IComparable,
        IToString
    {
        #region Public Constructors
        //
        // WARNING: This constructor produces an immutable null triplet object.
        //
        /// <summary>
        /// Constructs an empty triplet whose values are all the default for
        /// their respective types.
        /// </summary>
        public AnyTriplet()
            : base()
        {
            // do nothing.
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Constructs a triplet with the specified first value; the remaining
        /// values are the default for their types.
        /// </summary>
        /// <param name="x">
        /// The first value of the triplet.
        /// </param>
        public AnyTriplet(
            T1 x
            )
            : this()
        {
            this.x = x;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Constructs a triplet with the specified first and second values; the
        /// third value is the default for its type.
        /// </summary>
        /// <param name="x">
        /// The first value of the triplet.
        /// </param>
        /// <param name="y">
        /// The second value of the triplet.
        /// </param>
        public AnyTriplet(
            T1 x,
            T2 y
            )
            : this(x)
        {
            this.y = y;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Constructs a triplet with the specified first, second, and third
        /// values.
        /// </summary>
        /// <param name="x">
        /// The first value of the triplet.
        /// </param>
        /// <param name="y">
        /// The second value of the triplet.
        /// </param>
        /// <param name="z">
        /// The third value of the triplet.
        /// </param>
        public AnyTriplet(
            T1 x,
            T2 y,
            T3 z
            )
            : this(x, y)
        {
            this.z = z;
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region IAnyTriplet Members
        /// <summary>
        /// Gets the first value of this triplet as an object.
        /// </summary>
        object IAnyTriplet.X
        {
            get { return x; }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Gets the second value of this triplet as an object.
        /// </summary>
        object IAnyTriplet.Y
        {
            get { return y; }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Gets the third value of this triplet as an object.
        /// </summary>
        object IAnyTriplet.Z
        {
            get { return z; }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region IAnyTriplet<T1, T2, T3> Members
        /// <summary>
        /// The first value of this triplet.
        /// </summary>
        private T1 x;
        /// <summary>
        /// Gets the first value of this triplet.
        /// </summary>
        public virtual T1 X
        {
            get { return x; }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// The second value of this triplet.
        /// </summary>
        private T2 y;
        /// <summary>
        /// Gets the second value of this triplet.
        /// </summary>
        public virtual T2 Y
        {
            get { return y; }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// The third value of this triplet.
        /// </summary>
        private T3 z;
        /// <summary>
        /// Gets the third value of this triplet.
        /// </summary>
        public virtual T3 Z
        {
            get { return z; }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Static Conversion Helpers
        /// <summary>
        /// This method creates a triplet from the specified first value; the
        /// remaining values are the default for their types.
        /// </summary>
        /// <param name="value">
        /// The first value of the triplet.
        /// </param>
        /// <returns>
        /// The newly created triplet.
        /// </returns>
        public static AnyTriplet<T1, T2, T3> FromType1(
            T1 value
            )
        {
            return new AnyTriplet<T1, T2, T3>(
                value, default(T2), default(T3));
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method creates a triplet from the specified second value; the
        /// remaining values are the default for their types.
        /// </summary>
        /// <param name="value">
        /// The second value of the triplet.
        /// </param>
        /// <returns>
        /// The newly created triplet.
        /// </returns>
        public static AnyTriplet<T1, T2, T3> FromType2(
            T2 value
            )
        {
            return new AnyTriplet<T1, T2, T3>(
                default(T1), value, default(T3));
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method creates a triplet from the specified third value; the
        /// remaining values are the default for their types.
        /// </summary>
        /// <param name="value">
        /// The third value of the triplet.
        /// </param>
        /// <returns>
        /// The newly created triplet.
        /// </returns>
        public static AnyTriplet<T1, T2, T3> FromType3(
            T3 value
            )
        {
            return new AnyTriplet<T1, T2, T3>(
                default(T1), default(T2), value);
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Conversion Operators
        /// <summary>
        /// This operator implicitly converts the specified value into a triplet
        /// containing it as the first value.
        /// </summary>
        /// <param name="value">
        /// The first value of the triplet.
        /// </param>
        /// <returns>
        /// The newly created triplet.
        /// </returns>
        public static implicit operator AnyTriplet<T1, T2, T3>(
            T1 value
            )
        {
            return FromType1(value);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This operator implicitly converts the specified value into a triplet
        /// containing it as the second value.
        /// </summary>
        /// <param name="value">
        /// The second value of the triplet.
        /// </param>
        /// <returns>
        /// The newly created triplet.
        /// </returns>
        public static implicit operator AnyTriplet<T1, T2, T3>(
            T2 value
            )
        {
            return FromType2(value);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This operator implicitly converts the specified value into a triplet
        /// containing it as the third value.
        /// </summary>
        /// <param name="value">
        /// The third value of the triplet.
        /// </param>
        /// <returns>
        /// The newly created triplet.
        /// </returns>
        public static implicit operator AnyTriplet<T1, T2, T3>(
            T3 value
            )
        {
            return FromType3(value);
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region System.Object Overrides
        /// <summary>
        /// This method determines whether the specified object is a triplet
        /// equal to this triplet.
        /// </summary>
        /// <param name="obj">
        /// The object to compare with this triplet.  This parameter may be
        /// null.
        /// </param>
        /// <returns>
        /// True if the specified object is a triplet with equal values;
        /// otherwise, false.
        /// </returns>
        public override bool Equals(
            object obj
            )
        {
            IAnyTriplet<T1, T2, T3> anyTriplet =
                obj as IAnyTriplet<T1, T2, T3>;

            if (anyTriplet != null)
            {
                return GenericOps<T1>.Equals(this.X, anyTriplet.X) &&
                       GenericOps<T2>.Equals(this.Y, anyTriplet.Y) &&
                       GenericOps<T3>.Equals(this.Z, anyTriplet.Z);
            }
            else
            {
                return false;
            }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method returns the string form of this triplet, formatted as a
        /// list of its values.
        /// </summary>
        /// <returns>
        /// The string form of this triplet.
        /// </returns>
        public override string ToString()
        {
            return StringList.MakeList(this.X, this.Y, this.Z);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method returns a hash code for this triplet, combining the hash
        /// codes of its values.
        /// </summary>
        /// <returns>
        /// A hash code for this triplet.
        /// </returns>
        public override int GetHashCode()
        {
            return CommonOps.HashCodes.Combine(
                GenericOps<T1>.GetHashCode(this.X),
                GenericOps<T2>.GetHashCode(this.Y),
                GenericOps<T3>.GetHashCode(this.Z));
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region IComparer<IAnyTriplet<T1, T2, T3>> Members
        /// <summary>
        /// This method compares two triplets, ordering by their first values,
        /// then their second values, and then their third values.
        /// </summary>
        /// <param name="x">
        /// The first triplet to compare.  This parameter may be null.
        /// </param>
        /// <param name="y">
        /// The second triplet to compare.  This parameter may be null.
        /// </param>
        /// <returns>
        /// Zero if the triplets are equal, a negative number if the first
        /// triplet is less than the second, or a positive number if the first
        /// triplet is greater than the second.
        /// </returns>
        public virtual int Compare(
            IAnyTriplet<T1, T2, T3> x,
            IAnyTriplet<T1, T2, T3> y
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

                result = Comparer<T2>.Default.Compare(x.Y, y.Y);

                if (result != 0)
                    return result;

                return Comparer<T3>.Default.Compare(x.Z, y.Z);
            }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region IComparable<IAnyTriplet<T1, T2, T3>> Members
        /// <summary>
        /// This method compares this triplet to another triplet.
        /// </summary>
        /// <param name="other">
        /// The triplet to compare to this triplet.  This parameter may be null.
        /// </param>
        /// <returns>
        /// Zero if the triplets are equal, a negative number if this triplet is
        /// less than the other triplet, or a positive number if this triplet is
        /// greater than the other triplet.
        /// </returns>
        public virtual int CompareTo(
            IAnyTriplet<T1, T2, T3> other
            )
        {
            return Compare(this, other);
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region IEquatable<IAnyTriplet<T1, T2, T3>> Members
        /// <summary>
        /// This method determines whether this triplet is equal to another
        /// triplet.
        /// </summary>
        /// <param name="other">
        /// The triplet to compare with this triplet.  This parameter may be
        /// null.
        /// </param>
        /// <returns>
        /// True if the two triplets have equal values; otherwise, false.
        /// </returns>
        public virtual bool Equals(
            IAnyTriplet<T1, T2, T3> other
            )
        {
            return CompareTo(other) == 0;
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region IComparable Members
        /// <summary>
        /// This method compares this triplet to another object, which must be a
        /// triplet.
        /// </summary>
        /// <param name="obj">
        /// The object to compare to this triplet.  It must be a triplet.
        /// </param>
        /// <returns>
        /// Zero if the triplets are equal, a negative number if this triplet is
        /// less than the other triplet, or a positive number if this triplet is
        /// greater than the other triplet.
        /// </returns>
        public virtual int CompareTo(
            object obj
            )
        {
            IAnyTriplet<T1, T2, T3> anyTriplet =
                obj as IAnyTriplet<T1, T2, T3>;

            if (anyTriplet == null)
                throw new ArgumentException();

            return CompareTo(anyTriplet);
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region IToString Members
        /// <summary>
        /// This method returns the string form of this triplet, subject to the
        /// specified flags.
        /// </summary>
        /// <param name="flags">
        /// The flags that control how the string form is produced.
        /// </param>
        /// <returns>
        /// The string form of this triplet.
        /// </returns>
        public virtual string ToString(
            ToStringFlags flags
            )
        {
            return ToString(flags, null);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method returns the string form of this triplet, subject to the
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
        /// The string form of this triplet.
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
        /// This method returns the string form of this triplet, using the
        /// specified composite format string.
        /// </summary>
        /// <param name="format">
        /// The composite format string used to format the values of this
        /// triplet.
        /// </param>
        /// <returns>
        /// The formatted string form of this triplet.
        /// </returns>
        public virtual string ToString(
            string format
            )
        {
            return String.Format(format, this.X, this.Y, this.Z);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method returns the string form of this triplet, using the
        /// specified composite format string, limited to the specified length.
        /// </summary>
        /// <param name="format">
        /// The composite format string used to format the values of this
        /// triplet.
        /// </param>
        /// <param name="limit">
        /// The maximum length of the resulting string; longer strings are
        /// truncated with an ellipsis.
        /// </param>
        /// <param name="strict">
        /// Non-zero to strictly enforce the length limit.
        /// </param>
        /// <returns>
        /// The formatted, possibly truncated, string form of this triplet.
        /// </returns>
        public virtual string ToString(
            string format,
            int limit,
            bool strict
            )
        {
            return FormatOps.Ellipsis(
                String.Format(format, this.X, this.Y, this.Z), limit, strict);
        }
        #endregion
    }
}
