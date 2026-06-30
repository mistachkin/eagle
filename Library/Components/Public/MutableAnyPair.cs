/*
 * MutableAnyPair.cs --
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
    /// This class represents a pair of values, each of which may have a
    /// different type, and that may optionally be mutable after construction.
    /// It implements the various pair, comparison, equality, and string
    /// conversion interfaces.
    /// </summary>
    /// <typeparam name="T1">
    /// The type of the first value of the pair.
    /// </typeparam>
    /// <typeparam name="T2">
    /// The type of the second value of the pair.
    /// </typeparam>
#if SERIALIZATION
    [Serializable()]
#endif
    [ObjectId("f8861536-186a-43f7-bb75-e3e17d4e6fc2")]
    public class MutableAnyPair<T1, T2> :
        IPair,
        IAnyPair,
        IMutableAnyPair,
        IMutableAnyPair<T1, T2>,
        IComparer<IMutableAnyPair<T1, T2>>,
        IComparable<IMutableAnyPair<T1, T2>>,
        IEquatable<IMutableAnyPair<T1, T2>>,
        IComparable, IToString
    {
        #region Public Constructors
        /// <summary>
        /// Constructs an immutable pair with both values set to their default.
        /// </summary>
        //
        // WARNING: This constructor produces an immutable null pair object.
        //
        public MutableAnyPair()
            : this(false)
        {
            // do nothing.
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Constructs an immutable pair with the first value set to the
        /// specified value and the second value set to its default.
        /// </summary>
        /// <param name="x">
        /// The first value of the pair.
        /// </param>
        public MutableAnyPair(
            T1 x
            )
            : this(false, x)
        {
            // do nothing.
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Constructs an immutable pair with both values set to the specified
        /// values.
        /// </summary>
        /// <param name="x">
        /// The first value of the pair.
        /// </param>
        /// <param name="y">
        /// The second value of the pair.
        /// </param>
        public MutableAnyPair(
            T1 x,
            T2 y
            )
            : this(false, x, y)
        {
            // do nothing.
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Constructs a pair, with both values set to their default, whose
        /// mutability is determined by the specified value.
        /// </summary>
        /// <param name="mutable">
        /// Non-zero if the values of this pair may be changed after
        /// construction.
        /// </param>
        public MutableAnyPair(
            bool mutable
            )
            : base()
        {
            this.mutable = mutable;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Constructs a pair, with the first value set to the specified value
        /// and the second value set to its default, whose mutability is
        /// determined by the specified value.
        /// </summary>
        /// <param name="mutable">
        /// Non-zero if the values of this pair may be changed after
        /// construction.
        /// </param>
        /// <param name="x">
        /// The first value of the pair.
        /// </param>
        public MutableAnyPair(
            bool mutable,
            T1 x
            )
            : this(mutable)
        {
            this.x = x;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Constructs a pair, with both values set to the specified values,
        /// whose mutability is determined by the specified value.
        /// </summary>
        /// <param name="mutable">
        /// Non-zero if the values of this pair may be changed after
        /// construction.
        /// </param>
        /// <param name="x">
        /// The first value of the pair.
        /// </param>
        /// <param name="y">
        /// The second value of the pair.
        /// </param>
        public MutableAnyPair(
            bool mutable,
            T1 x,
            T2 y
            )
            : this(mutable, x)
        {
            this.y = y;
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Private Methods
        /// <summary>
        /// This method throws an exception if this pair is not mutable.
        /// </summary>
        private void CheckMutable()
        {
            if (!mutable)
                throw new InvalidOperationException();
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region IAnyPair Members
        /// <summary>
        /// Gets the first value of the pair as an object.
        /// </summary>
        object IAnyPair.X
        {
            get { return x; }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Gets the second value of the pair as an object.
        /// </summary>
        object IAnyPair.Y
        {
            get { return y; }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region IMutableAnyPair Members
        /// <summary>
        /// Gets a value indicating whether the values of this pair may be
        /// changed after construction.
        /// </summary>
        bool IMutableAnyPair.Mutable
        {
            get { return mutable; }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Gets or sets the first value of the pair as an object.  Setting this
        /// value requires the pair to be mutable.
        /// </summary>
        object IMutableAnyPair.X
        {
            get { return x; }
            set { CheckMutable(); x = (T1)value; }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Gets or sets the second value of the pair as an object.  Setting
        /// this value requires the pair to be mutable.
        /// </summary>
        object IMutableAnyPair.Y
        {
            get { return y; }
            set { CheckMutable(); y = (T2)value; }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method attempts to set the first value of the pair, succeeding
        /// only if the pair is mutable and the value is of a compatible type.
        /// </summary>
        /// <param name="value">
        /// The new first value of the pair.
        /// </param>
        /// <returns>
        /// True if the value was set; otherwise, false.
        /// </returns>
        bool IMutableAnyPair.TrySetX(
            object value
            )
        {
            if (!mutable)
                return false;

            if (!MarshalOps.DoesValueMatchType(typeof(T1), value))
                return false;

            x = (T1)value;
            return true;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method attempts to set the second value of the pair,
        /// succeeding only if the pair is mutable and the value is of a
        /// compatible type.
        /// </summary>
        /// <param name="value">
        /// The new second value of the pair.
        /// </param>
        /// <returns>
        /// True if the value was set; otherwise, false.
        /// </returns>
        bool IMutableAnyPair.TrySetY(
            object value
            )
        {
            if (!mutable)
                return false;

            if (!MarshalOps.DoesValueMatchType(typeof(T2), value))
                return false;

            y = (T2)value;
            return true;
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region IMutableAnyPair<T1, T2> Members
        /// <summary>
        /// Non-zero if the values of this pair may be changed after
        /// construction.
        /// </summary>
        private bool mutable;
        /// <summary>
        /// Gets a value indicating whether the values of this pair may be
        /// changed after construction.
        /// </summary>
        public virtual bool Mutable
        {
            get { return mutable; }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// The first value of the pair.
        /// </summary>
        private T1 x;
        /// <summary>
        /// Gets or sets the first value of the pair.  Setting this value
        /// requires the pair to be mutable.
        /// </summary>
        public virtual T1 X
        {
            get { return x; }
            set { CheckMutable(); x = value; }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// The second value of the pair.
        /// </summary>
        private T2 y;
        /// <summary>
        /// Gets or sets the second value of the pair.  Setting this value
        /// requires the pair to be mutable.
        /// </summary>
        public virtual T2 Y
        {
            get { return y; }
            set { CheckMutable(); y = value; }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Static Conversion Helpers
        /// <summary>
        /// This method creates a pair whose first value is set to the specified
        /// value and whose second value is set to its default.
        /// </summary>
        /// <param name="value">
        /// The first value of the new pair.
        /// </param>
        /// <returns>
        /// The newly created pair.
        /// </returns>
        public static MutableAnyPair<T1, T2> FromType1(
            T1 value
            )
        {
            return new MutableAnyPair<T1, T2>(
                value, default(T2));
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method creates a pair whose second value is set to the
        /// specified value and whose first value is set to its default.
        /// </summary>
        /// <param name="value">
        /// The second value of the new pair.
        /// </param>
        /// <returns>
        /// The newly created pair.
        /// </returns>
        public static MutableAnyPair<T1, T2> FromType2(
            T2 value
            )
        {
            return new MutableAnyPair<T1, T2>(
                default(T1), value);
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Conversion Operators
        /// <summary>
        /// This operator implicitly converts a value of the first type into a
        /// pair whose first value is set to it.
        /// </summary>
        /// <param name="value">
        /// The first value of the new pair.
        /// </param>
        /// <returns>
        /// The newly created pair.
        /// </returns>
        public static implicit operator MutableAnyPair<T1, T2>(
            T1 value
            )
        {
            return FromType1(value);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This operator implicitly converts a value of the second type into a
        /// pair whose second value is set to it.
        /// </summary>
        /// <param name="value">
        /// The second value of the new pair.
        /// </param>
        /// <returns>
        /// The newly created pair.
        /// </returns>
        public static implicit operator MutableAnyPair<T1, T2>(
            T2 value
            )
        {
            return FromType2(value);
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region System.Object Overrides
        /// <summary>
        /// This method determines whether the specified object is equal to this
        /// pair.
        /// </summary>
        /// <param name="obj">
        /// The object to compare with this pair.
        /// </param>
        /// <returns>
        /// True if the specified object is a pair with equal values; otherwise,
        /// false.
        /// </returns>
        public override bool Equals(
            object obj
            )
        {
            IMutableAnyPair<T1, T2> anyPair =
                obj as IMutableAnyPair<T1, T2>;

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
        /// This method returns a string representation of this pair.
        /// </summary>
        /// <returns>
        /// A list string containing the two values of the pair.
        /// </returns>
        public override string ToString()
        {
            return StringList.MakeList(this.X, this.Y);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method returns a hash code for this pair.
        /// </summary>
        /// <returns>
        /// A hash code derived from the two values of the pair.
        /// </returns>
        public override int GetHashCode()
        {
            return CommonOps.HashCodes.Combine(
                GenericOps<T1>.GetHashCode(this.X),
                GenericOps<T2>.GetHashCode(this.Y));
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region IComparer<IMutableAnyPair<T1, T2>> Members
        /// <summary>
        /// This method compares two pairs and returns a value indicating their
        /// relative order.
        /// </summary>
        /// <param name="x">
        /// The first pair to compare.  This parameter may be null.
        /// </param>
        /// <param name="y">
        /// The second pair to compare.  This parameter may be null.
        /// </param>
        /// <returns>
        /// Zero if the pairs are equal, a negative number if
        /// <paramref name="x" /> is less than <paramref name="y" />, or a
        /// positive number if <paramref name="x" /> is greater than
        /// <paramref name="y" />.
        /// </returns>
        public virtual int Compare(
            IMutableAnyPair<T1, T2> x,
            IMutableAnyPair<T1, T2> y
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

        #region IComparable<IMutableAnyPair<T1, T2>> Members
        /// <summary>
        /// This method compares this pair with another pair and returns a value
        /// indicating their relative order.
        /// </summary>
        /// <param name="other">
        /// The pair to compare with this pair.  This parameter may be null.
        /// </param>
        /// <returns>
        /// Zero if the pairs are equal, a negative number if this pair is less
        /// than <paramref name="other" />, or a positive number if this pair is
        /// greater than <paramref name="other" />.
        /// </returns>
        public virtual int CompareTo(
            IMutableAnyPair<T1, T2> other
            )
        {
            return Compare(this, other);
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region IEquatable<IMutableAnyPair<T1, T2>> Members
        /// <summary>
        /// This method determines whether the specified pair is equal to this
        /// pair.
        /// </summary>
        /// <param name="other">
        /// The pair to compare with this pair.  This parameter may be null.
        /// </param>
        /// <returns>
        /// True if the specified pair is equal to this pair; otherwise, false.
        /// </returns>
        public virtual bool Equals(
            IMutableAnyPair<T1, T2> other
            )
        {
            return CompareTo(other) == 0;
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region IComparable Members
        /// <summary>
        /// This method compares this pair with another object and returns a
        /// value indicating their relative order.
        /// </summary>
        /// <param name="obj">
        /// The object to compare with this pair.  It must be a compatible pair.
        /// </param>
        /// <returns>
        /// Zero if the objects are equal, a negative number if this pair is
        /// less than <paramref name="obj" />, or a positive number if this pair
        /// is greater than <paramref name="obj" />.
        /// </returns>
        public virtual int CompareTo(
            object obj
            )
        {
            IMutableAnyPair<T1, T2> anyPair =
                obj as IMutableAnyPair<T1, T2>;

            if (anyPair == null)
                throw new ArgumentException();

            return CompareTo(anyPair);
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region IToString Members
        /// <summary>
        /// This method returns a string representation of this pair, subject to
        /// the specified flags.
        /// </summary>
        /// <param name="flags">
        /// The flags that control how the string representation is produced.
        /// </param>
        /// <returns>
        /// A string representation of this pair.
        /// </returns>
        public virtual string ToString(
            ToStringFlags flags
            )
        {
            return ToString(flags, null);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method returns a string representation of this pair, subject to
        /// the specified flags, or a default value.
        /// </summary>
        /// <param name="flags">
        /// The flags that control how the string representation is produced.
        /// </param>
        /// <param name="default">
        /// The default value to return when there is no string representation.
        /// </param>
        /// <returns>
        /// A string representation of this pair.
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
        /// This method returns a string representation of this pair using the
        /// specified format string.
        /// </summary>
        /// <param name="format">
        /// The composite format string used to format the two values of the
        /// pair.
        /// </param>
        /// <returns>
        /// The formatted string representation of this pair.
        /// </returns>
        public virtual string ToString(
            string format
            )
        {
            return String.Format(format, this.X, this.Y);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method returns a string representation of this pair using the
        /// specified format string, truncated to the specified length.
        /// </summary>
        /// <param name="format">
        /// The composite format string used to format the two values of the
        /// pair.
        /// </param>
        /// <param name="limit">
        /// The maximum length of the resulting string.
        /// </param>
        /// <param name="strict">
        /// Non-zero to strictly enforce the length limit.
        /// </param>
        /// <returns>
        /// The formatted, possibly truncated, string representation of this
        /// pair.
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
