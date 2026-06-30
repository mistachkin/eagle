/*
 * MutableAnyTriplet.cs --
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
    /// This class represents a triplet of values, each of which may have a
    /// different type, and that may optionally be mutable after construction.
    /// It implements the various triplet, comparison, equality, and string
    /// conversion interfaces.
    /// </summary>
    /// <typeparam name="T1">
    /// The type of the first value of the triplet.
    /// </typeparam>
    /// <typeparam name="T2">
    /// The type of the second value of the triplet.
    /// </typeparam>
    /// <typeparam name="T3">
    /// The type of the third value of the triplet.
    /// </typeparam>
#if SERIALIZATION
    [Serializable()]
#endif
    [ObjectId("b70c6b5f-6507-44ef-bca7-abd1b14efff8")]
    public class MutableAnyTriplet<T1, T2, T3> :
        ITriplet,
        IAnyTriplet,
        IMutableAnyTriplet,
        IMutableAnyTriplet<T1, T2, T3>,
        IComparer<IMutableAnyTriplet<T1, T2, T3>>,
        IComparable<IMutableAnyTriplet<T1, T2, T3>>,
        IEquatable<IMutableAnyTriplet<T1, T2, T3>>,
        IComparable,
        IToString
    {
        #region Public Constructors
        /// <summary>
        /// Constructs an immutable triplet with all values set to their
        /// default.
        /// </summary>
        //
        // WARNING: This constructor produces an immutable null triplet object.
        //
        public MutableAnyTriplet()
            : this(false)
        {
            // do nothing.
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Constructs an immutable triplet with the first value set to the
        /// specified value and the remaining values set to their default.
        /// </summary>
        /// <param name="x">
        /// The first value of the triplet.
        /// </param>
        public MutableAnyTriplet(
            T1 x
            )
            : this(false, x)
        {
            // do nothing.
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Constructs an immutable triplet with the first two values set to the
        /// specified values and the third value set to its default.
        /// </summary>
        /// <param name="x">
        /// The first value of the triplet.
        /// </param>
        /// <param name="y">
        /// The second value of the triplet.
        /// </param>
        public MutableAnyTriplet(
            T1 x,
            T2 y
            )
            : this(false, x, y)
        {
            // do nothing.
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Constructs an immutable triplet with all values set to the specified
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
        public MutableAnyTriplet(
            T1 x,
            T2 y,
            T3 z
            )
            : this(false, x, y, z)
        {
            // do nothing.
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Constructs a triplet, with all values set to their default, whose
        /// mutability is determined by the specified value.
        /// </summary>
        /// <param name="mutable">
        /// Non-zero if the values of this triplet may be changed after
        /// construction.
        /// </param>
        public MutableAnyTriplet(
            bool mutable
            )
            : base()
        {
            this.mutable = mutable;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Constructs a triplet, with the first value set to the specified
        /// value and the remaining values set to their default, whose
        /// mutability is determined by the specified value.
        /// </summary>
        /// <param name="mutable">
        /// Non-zero if the values of this triplet may be changed after
        /// construction.
        /// </param>
        /// <param name="x">
        /// The first value of the triplet.
        /// </param>
        public MutableAnyTriplet(
            bool mutable,
            T1 x
            )
            : this(mutable)
        {
            this.x = x;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Constructs a triplet, with the first two values set to the specified
        /// values and the third value set to its default, whose mutability is
        /// determined by the specified value.
        /// </summary>
        /// <param name="mutable">
        /// Non-zero if the values of this triplet may be changed after
        /// construction.
        /// </param>
        /// <param name="x">
        /// The first value of the triplet.
        /// </param>
        /// <param name="y">
        /// The second value of the triplet.
        /// </param>
        public MutableAnyTriplet(
            bool mutable,
            T1 x,
            T2 y
            )
            : this(mutable, x)
        {
            this.y = y;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Constructs a triplet, with all values set to the specified values,
        /// whose mutability is determined by the specified value.
        /// </summary>
        /// <param name="mutable">
        /// Non-zero if the values of this triplet may be changed after
        /// construction.
        /// </param>
        /// <param name="x">
        /// The first value of the triplet.
        /// </param>
        /// <param name="y">
        /// The second value of the triplet.
        /// </param>
        /// <param name="z">
        /// The third value of the triplet.
        /// </param>
        public MutableAnyTriplet(
            bool mutable,
            T1 x,
            T2 y,
            T3 z
            )
            : this(mutable, x, y)
        {
            this.z = z;
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Private Methods
        /// <summary>
        /// This method throws an exception if this triplet is not mutable.
        /// </summary>
        private void CheckMutable()
        {
            if (!mutable)
                throw new InvalidOperationException();
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region IAnyTriplet Members
        /// <summary>
        /// Gets the first value of the triplet as an object.
        /// </summary>
        object IAnyTriplet.X
        {
            get { return x; }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Gets the second value of the triplet as an object.
        /// </summary>
        object IAnyTriplet.Y
        {
            get { return y; }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Gets the third value of the triplet as an object.
        /// </summary>
        object IAnyTriplet.Z
        {
            get { return z; }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region IMutableAnyTriplet Members
        /// <summary>
        /// Gets a value indicating whether the values of this triplet may be
        /// changed after construction.
        /// </summary>
        bool IMutableAnyTriplet.Mutable
        {
            get { return mutable; }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Gets or sets the first value of the triplet as an object.  Setting
        /// this value requires the triplet to be mutable.
        /// </summary>
        object IMutableAnyTriplet.X
        {
            get { return x; }
            set { CheckMutable(); x = (T1)value; }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Gets or sets the second value of the triplet as an object.  Setting
        /// this value requires the triplet to be mutable.
        /// </summary>
        object IMutableAnyTriplet.Y
        {
            get { return y; }
            set { CheckMutable(); y = (T2)value; }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Gets or sets the third value of the triplet as an object.  Setting
        /// this value requires the triplet to be mutable.
        /// </summary>
        object IMutableAnyTriplet.Z
        {
            get { return z; }
            set { CheckMutable(); z = (T3)value; }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method attempts to set the first value of the triplet,
        /// succeeding only if the triplet is mutable and the value is of a
        /// compatible type.
        /// </summary>
        /// <param name="value">
        /// The new first value of the triplet.
        /// </param>
        /// <returns>
        /// True if the value was set; otherwise, false.
        /// </returns>
        bool IMutableAnyTriplet.TrySetX(
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
        /// This method attempts to set the second value of the triplet,
        /// succeeding only if the triplet is mutable and the value is of a
        /// compatible type.
        /// </summary>
        /// <param name="value">
        /// The new second value of the triplet.
        /// </param>
        /// <returns>
        /// True if the value was set; otherwise, false.
        /// </returns>
        bool IMutableAnyTriplet.TrySetY(
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

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method attempts to set the third value of the triplet,
        /// succeeding only if the triplet is mutable and the value is of a
        /// compatible type.
        /// </summary>
        /// <param name="value">
        /// The new third value of the triplet.
        /// </param>
        /// <returns>
        /// True if the value was set; otherwise, false.
        /// </returns>
        bool IMutableAnyTriplet.TrySetZ(
            object value
            )
        {
            if (!mutable)
                return false;

            if (!MarshalOps.DoesValueMatchType(typeof(T3), value))
                return false;

            z = (T3)value;
            return true;
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region IMutableAnyTriplet<T1, T2, T3> Members
        /// <summary>
        /// Non-zero if the values of this triplet may be changed after
        /// construction.
        /// </summary>
        private bool mutable;
        /// <summary>
        /// Gets a value indicating whether the values of this triplet may be
        /// changed after construction.
        /// </summary>
        public virtual bool Mutable
        {
            get { return mutable; }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// The first value of the triplet.
        /// </summary>
        private T1 x;
        /// <summary>
        /// Gets or sets the first value of the triplet.  Setting this value
        /// requires the triplet to be mutable.
        /// </summary>
        public virtual T1 X
        {
            get { return x; }
            set { CheckMutable(); x = value; }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// The second value of the triplet.
        /// </summary>
        private T2 y;
        /// <summary>
        /// Gets or sets the second value of the triplet.  Setting this value
        /// requires the triplet to be mutable.
        /// </summary>
        public virtual T2 Y
        {
            get { return y; }
            set { CheckMutable(); y = value; }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// The third value of the triplet.
        /// </summary>
        private T3 z;
        /// <summary>
        /// Gets or sets the third value of the triplet.  Setting this value
        /// requires the triplet to be mutable.
        /// </summary>
        public virtual T3 Z
        {
            get { return z; }
            set { CheckMutable(); z = value; }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Static Conversion Helpers
        /// <summary>
        /// This method creates a triplet whose first value is set to the
        /// specified value and whose remaining values are set to their default.
        /// </summary>
        /// <param name="value">
        /// The first value of the new triplet.
        /// </param>
        /// <returns>
        /// The newly created triplet.
        /// </returns>
        public static MutableAnyTriplet<T1, T2, T3> FromType1(
            T1 value
            )
        {
            return new MutableAnyTriplet<T1, T2, T3>(
                value, default(T2), default(T3));
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method creates a triplet whose second value is set to the
        /// specified value and whose remaining values are set to their default.
        /// </summary>
        /// <param name="value">
        /// The second value of the new triplet.
        /// </param>
        /// <returns>
        /// The newly created triplet.
        /// </returns>
        public static MutableAnyTriplet<T1, T2, T3> FromType2(
            T2 value
            )
        {
            return new MutableAnyTriplet<T1, T2, T3>(
                default(T1), value, default(T3));
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method creates a triplet whose third value is set to the
        /// specified value and whose remaining values are set to their default.
        /// </summary>
        /// <param name="value">
        /// The third value of the new triplet.
        /// </param>
        /// <returns>
        /// The newly created triplet.
        /// </returns>
        public static MutableAnyTriplet<T1, T2, T3> FromType3(
            T3 value
            )
        {
            return new MutableAnyTriplet<T1, T2, T3>(
                default(T1), default(T2), value);
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Conversion Operators
        /// <summary>
        /// This operator implicitly converts a value of the first type into a
        /// triplet whose first value is set to it.
        /// </summary>
        /// <param name="value">
        /// The first value of the new triplet.
        /// </param>
        /// <returns>
        /// The newly created triplet.
        /// </returns>
        public static implicit operator MutableAnyTriplet<T1, T2, T3>(
            T1 value
            )
        {
            return FromType1(value);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This operator implicitly converts a value of the second type into a
        /// triplet whose second value is set to it.
        /// </summary>
        /// <param name="value">
        /// The second value of the new triplet.
        /// </param>
        /// <returns>
        /// The newly created triplet.
        /// </returns>
        public static implicit operator MutableAnyTriplet<T1, T2, T3>(
            T2 value
            )
        {
            return FromType2(value);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This operator implicitly converts a value of the third type into a
        /// triplet whose third value is set to it.
        /// </summary>
        /// <param name="value">
        /// The third value of the new triplet.
        /// </param>
        /// <returns>
        /// The newly created triplet.
        /// </returns>
        public static implicit operator MutableAnyTriplet<T1, T2, T3>(
            T3 value
            )
        {
            return FromType3(value);
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region System.Object Overrides
        /// <summary>
        /// This method determines whether the specified object is equal to this
        /// triplet.
        /// </summary>
        /// <param name="obj">
        /// The object to compare with this triplet.
        /// </param>
        /// <returns>
        /// True if the specified object is a triplet with equal values;
        /// otherwise, false.
        /// </returns>
        public override bool Equals(
            object obj
            )
        {
            IMutableAnyTriplet<T1, T2, T3> anyTriplet =
                obj as IMutableAnyTriplet<T1, T2, T3>;

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
        /// This method returns a string representation of this triplet.
        /// </summary>
        /// <returns>
        /// A list string containing the three values of the triplet.
        /// </returns>
        public override string ToString()
        {
            return StringList.MakeList(this.X, this.Y, this.Z);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method returns a hash code for this triplet.
        /// </summary>
        /// <returns>
        /// A hash code derived from the three values of the triplet.
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

        #region IComparer<IMutableAnyTriplet<T1, T2, T3>> Members
        /// <summary>
        /// This method compares two triplets and returns a value indicating
        /// their relative order.
        /// </summary>
        /// <param name="x">
        /// The first triplet to compare.  This parameter may be null.
        /// </param>
        /// <param name="y">
        /// The second triplet to compare.  This parameter may be null.
        /// </param>
        /// <returns>
        /// Zero if the triplets are equal, a negative number if
        /// <paramref name="x" /> is less than <paramref name="y" />, or a
        /// positive number if <paramref name="x" /> is greater than
        /// <paramref name="y" />.
        /// </returns>
        public virtual int Compare(
            IMutableAnyTriplet<T1, T2, T3> x,
            IMutableAnyTriplet<T1, T2, T3> y
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

        #region IComparable<IMutableAnyTriplet<T1, T2, T3>> Members
        /// <summary>
        /// This method compares this triplet with another triplet and returns
        /// a value indicating their relative order.
        /// </summary>
        /// <param name="other">
        /// The triplet to compare with this triplet.  This parameter may be
        /// null.
        /// </param>
        /// <returns>
        /// Zero if the triplets are equal, a negative number if this triplet is
        /// less than <paramref name="other" />, or a positive number if this
        /// triplet is greater than <paramref name="other" />.
        /// </returns>
        public virtual int CompareTo(
            IMutableAnyTriplet<T1, T2, T3> other
            )
        {
            return Compare(this, other);
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region IEquatable<IMutableAnyTriplet<T1, T2, T3>> Members
        /// <summary>
        /// This method determines whether the specified triplet is equal to
        /// this triplet.
        /// </summary>
        /// <param name="other">
        /// The triplet to compare with this triplet.  This parameter may be
        /// null.
        /// </param>
        /// <returns>
        /// True if the specified triplet is equal to this triplet; otherwise,
        /// false.
        /// </returns>
        public virtual bool Equals(
            IMutableAnyTriplet<T1, T2, T3> other
            )
        {
            return CompareTo(other) == 0;
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region IComparable Members
        /// <summary>
        /// This method compares this triplet with another object and returns a
        /// value indicating their relative order.
        /// </summary>
        /// <param name="obj">
        /// The object to compare with this triplet.  It must be a compatible
        /// triplet.
        /// </param>
        /// <returns>
        /// Zero if the objects are equal, a negative number if this triplet is
        /// less than <paramref name="obj" />, or a positive number if this
        /// triplet is greater than <paramref name="obj" />.
        /// </returns>
        public virtual int CompareTo(
            object obj
            )
        {
            IMutableAnyTriplet<T1, T2, T3> anyTriplet =
                obj as IMutableAnyTriplet<T1, T2, T3>;

            if (anyTriplet == null)
                throw new ArgumentException();

            return CompareTo(anyTriplet);
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region IToString Members
        /// <summary>
        /// This method returns a string representation of this triplet, subject
        /// to the specified flags.
        /// </summary>
        /// <param name="flags">
        /// The flags that control how the string representation is produced.
        /// </param>
        /// <returns>
        /// A string representation of this triplet.
        /// </returns>
        public virtual string ToString(
            ToStringFlags flags
            )
        {
            return ToString(flags, null);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method returns a string representation of this triplet, subject
        /// to the specified flags, or a default value.
        /// </summary>
        /// <param name="flags">
        /// The flags that control how the string representation is produced.
        /// </param>
        /// <param name="default">
        /// The default value to return when there is no string representation.
        /// </param>
        /// <returns>
        /// A string representation of this triplet.
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
        /// This method returns a string representation of this triplet using
        /// the specified format string.
        /// </summary>
        /// <param name="format">
        /// The composite format string used to format the three values of the
        /// triplet.
        /// </param>
        /// <returns>
        /// The formatted string representation of this triplet.
        /// </returns>
        public virtual string ToString(
            string format
            )
        {
            return String.Format(format, this.X, this.Y, this.Z);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method returns a string representation of this triplet using
        /// the specified format string, truncated to the specified length.
        /// </summary>
        /// <param name="format">
        /// The composite format string used to format the three values of the
        /// triplet.
        /// </param>
        /// <param name="limit">
        /// The maximum length of the resulting string.
        /// </param>
        /// <param name="strict">
        /// Non-zero to strictly enforce the length limit.
        /// </param>
        /// <returns>
        /// The formatted, possibly truncated, string representation of this
        /// triplet.
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
