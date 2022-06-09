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
        //
        // WARNING: This constructor produces an immutable null pair object.
        //
        public MutableAnyPair()
            : this(false)
        {
            // do nothing.
        }

        ///////////////////////////////////////////////////////////////////////

        public MutableAnyPair(
            T1 x
            )
            : this(false, x)
        {
            // do nothing.
        }

        ///////////////////////////////////////////////////////////////////////

        public MutableAnyPair(
            T1 x,
            T2 y
            )
            : this(false, x, y)
        {
            // do nothing.
        }

        ///////////////////////////////////////////////////////////////////////

        public MutableAnyPair(
            bool mutable
            )
            : base()
        {
            this.mutable = mutable;
        }

        ///////////////////////////////////////////////////////////////////////

        public MutableAnyPair(
            bool mutable,
            T1 x
            )
            : this(mutable)
        {
            this.x = x;
        }

        ///////////////////////////////////////////////////////////////////////

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
        private void CheckMutable()
        {
            if (!mutable)
                throw new InvalidOperationException();
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region IAnyPair Members
        object IAnyPair.X
        {
            get { return x; }
        }

        ///////////////////////////////////////////////////////////////////////

        object IAnyPair.Y
        {
            get { return y; }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region IMutableAnyPair Members
        bool IMutableAnyPair.Mutable
        {
            get { return mutable; }
        }

        ///////////////////////////////////////////////////////////////////////

        object IMutableAnyPair.X
        {
            get { return x; }
            set { CheckMutable(); x = (T1)value; }
        }

        ///////////////////////////////////////////////////////////////////////

        object IMutableAnyPair.Y
        {
            get { return y; }
            set { CheckMutable(); y = (T2)value; }
        }

        ///////////////////////////////////////////////////////////////////////

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
        private bool mutable;
        public virtual bool Mutable
        {
            get { return mutable; }
        }

        ///////////////////////////////////////////////////////////////////////

        private T1 x;
        public virtual T1 X
        {
            get { return x; }
            set { CheckMutable(); x = value; }
        }

        ///////////////////////////////////////////////////////////////////////

        private T2 y;
        public virtual T2 Y
        {
            get { return y; }
            set { CheckMutable(); y = value; }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Static Conversion Helpers
        public static MutableAnyPair<T1, T2> FromType1(
            T1 value
            )
        {
            return new MutableAnyPair<T1, T2>(
                value, default(T2));
        }

        ///////////////////////////////////////////////////////////////////////

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
        public static implicit operator MutableAnyPair<T1, T2>(
            T1 value
            )
        {
            return FromType1(value);
        }

        ///////////////////////////////////////////////////////////////////////

        public static implicit operator MutableAnyPair<T1, T2>(
            T2 value
            )
        {
            return FromType2(value);
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region System.Object Overrides
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

        public override string ToString()
        {
            return StringList.MakeList(this.X, this.Y);
        }

        ///////////////////////////////////////////////////////////////////////

        public override int GetHashCode()
        {
            return CommonOps.HashCodes.Combine(
                GenericOps<T1>.GetHashCode(this.X),
                GenericOps<T2>.GetHashCode(this.Y));
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region IComparer<IMutableAnyPair<T1, T2>> Members
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
        public virtual int CompareTo(
            IMutableAnyPair<T1, T2> other
            )
        {
            return Compare(this, other);
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region IEquatable<IMutableAnyPair<T1, T2>> Members
        public virtual bool Equals(
            IMutableAnyPair<T1, T2> other
            )
        {
            return CompareTo(other) == 0;
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region IComparable Members
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
        public virtual string ToString(
            ToStringFlags flags
            )
        {
            return ToString(flags, null);
        }

        ///////////////////////////////////////////////////////////////////////

        public virtual string ToString(
            ToStringFlags flags, /* NOT USED */
            string @default /* NOT USED */
            )
        {
            return ToString();
        }

        ///////////////////////////////////////////////////////////////////////

        public virtual string ToString(
            string format
            )
        {
            return String.Format(format, this.X, this.Y);
        }

        ///////////////////////////////////////////////////////////////////////

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
