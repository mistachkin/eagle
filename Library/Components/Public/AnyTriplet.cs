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
        public AnyTriplet()
            : base()
        {
            // do nothing.
        }

        ///////////////////////////////////////////////////////////////////////

        public AnyTriplet(
            T1 x
            )
            : this()
        {
            this.x = x;
        }

        ///////////////////////////////////////////////////////////////////////

        public AnyTriplet(
            T1 x,
            T2 y
            )
            : this(x)
        {
            this.y = y;
        }

        ///////////////////////////////////////////////////////////////////////

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
        object IAnyTriplet.X
        {
            get { return x; }
        }

        ///////////////////////////////////////////////////////////////////////

        object IAnyTriplet.Y
        {
            get { return y; }
        }

        ///////////////////////////////////////////////////////////////////////

        object IAnyTriplet.Z
        {
            get { return z; }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region IAnyTriplet<T1, T2, T3> Members
        private T1 x;
        public virtual T1 X
        {
            get { return x; }
        }

        ///////////////////////////////////////////////////////////////////////

        private T2 y;
        public virtual T2 Y
        {
            get { return y; }
        }

        ///////////////////////////////////////////////////////////////////////

        private T3 z;
        public virtual T3 Z
        {
            get { return z; }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Static Conversion Helpers
        public static AnyTriplet<T1, T2, T3> FromType1(
            T1 value
            )
        {
            return new AnyTriplet<T1, T2, T3>(
                value, default(T2), default(T3));
        }

        ///////////////////////////////////////////////////////////////////////

        public static AnyTriplet<T1, T2, T3> FromType2(
            T2 value
            )
        {
            return new AnyTriplet<T1, T2, T3>(
                default(T1), value, default(T3));
        }

        ///////////////////////////////////////////////////////////////////////

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
        public static implicit operator AnyTriplet<T1, T2, T3>(
            T1 value
            )
        {
            return FromType1(value);
        }

        ///////////////////////////////////////////////////////////////////////

        public static implicit operator AnyTriplet<T1, T2, T3>(
            T2 value
            )
        {
            return FromType2(value);
        }

        ///////////////////////////////////////////////////////////////////////

        public static implicit operator AnyTriplet<T1, T2, T3>(
            T3 value
            )
        {
            return FromType3(value);
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region System.Object Overrides
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

        public override string ToString()
        {
            return StringList.MakeList(this.X, this.Y, this.Z);
        }

        ///////////////////////////////////////////////////////////////////////

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
        public virtual int CompareTo(
            IAnyTriplet<T1, T2, T3> other
            )
        {
            return Compare(this, other);
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region IEquatable<IAnyTriplet<T1, T2, T3>> Members
        public virtual bool Equals(
            IAnyTriplet<T1, T2, T3> other
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
            IAnyTriplet<T1, T2, T3> anyTriplet =
                obj as IAnyTriplet<T1, T2, T3>;

            if (anyTriplet == null)
                throw new ArgumentException();

            return CompareTo(anyTriplet);
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
            return String.Format(format, this.X, this.Y, this.Z);
        }

        ///////////////////////////////////////////////////////////////////////

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
