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
using Eagle._Components.Public;
using Eagle._Interfaces.Public;

namespace Eagle._Comparers
{
#if SERIALIZATION
    [Serializable()]
#endif
    [ObjectId("f9da1d4e-8c47-47e9-a1b8-f975e3fb2408")]
    internal sealed class AnyPair<T1, T2> :
            IComparer<IAnyPair<T1, T2>>, IEqualityComparer<IAnyPair<T1, T2>>
    {
        #region Private Data
        private PairComparison comparisonType;
        private IComparer<T1> xComparer;
        private IEqualityComparer<T1> xEqualityComparer;
        private IComparer<T2> yComparer;
        private IEqualityComparer<T2> yEqualityComparer;
        private IFormatProvider formatProvider;
        private bool throwOnError;
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Private Constructors
        private AnyPair()
        {
            this.comparisonType = PairComparison.None;
            this.xComparer = null;
            this.xEqualityComparer = null;
            this.yComparer = null;
            this.yEqualityComparer = null;
            this.formatProvider = null;
            this.throwOnError = false;
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Public Constructors
        public AnyPair(
            PairComparison comparisonType,
            bool throwOnError
            )
            : this()
        {
            this.comparisonType = comparisonType;
            this.throwOnError = throwOnError;
        }

        ///////////////////////////////////////////////////////////////////////

        public AnyPair(
            PairComparison comparisonType,
            IComparer<T1> xComparer,
            IEqualityComparer<T1> xEqualityComparer,
            IComparer<T2> yComparer,
            IEqualityComparer<T2> yEqualityComparer,
            IFormatProvider formatProvider,
            bool throwOnError
            )
            : this(comparisonType, throwOnError)
        {
            this.xComparer = xComparer;
            this.xEqualityComparer = xEqualityComparer;
            this.yComparer = yComparer;
            this.yEqualityComparer = yEqualityComparer;
            this.formatProvider = formatProvider;
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Private Methods
        private T2A CastToTypeParameter<T1A, T2A>(T1A value)
        {
            IConvertible convertible = value as IConvertible;

            if (convertible != null)
            {
                try
                {
                    return (T2A)convertible.ToType(
                        typeof(T2A), formatProvider); /* throw */
                }
                catch
                {
                    if (throwOnError)
                        throw;
                    else
                        return default(T2A);
                }
            }

            //
            // NOTE: Callers should already be checking that the types
            //       are equal, which means we should not get here.
            //
            if (throwOnError)
                throw new ScriptException();
            else
                return default(T2A);
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region IComparer<IAnyPair<T1, T2>> Members
        public int Compare(
            IAnyPair<T1, T2> left,
            IAnyPair<T1, T2> right
            )
        {
            if ((left == null) && (right == null))
            {
                return 0;
            }
            else if (left == null)
            {
                return -1;
            }
            else if (right == null)
            {
                return 1;
            }
            else
            {
                switch (comparisonType)
                {
                    case PairComparison.LXRX:
                        {
                            IComparer<T1> xLocalComparer = (xComparer != null) ?
                                xComparer : Comparer<T1>.Default;

                            return xLocalComparer.Compare(left.X, right.X);
                        }
                    case PairComparison.LXRY:
                        {
                            if (typeof(T1) != typeof(T2))
                                break;

                            IComparer<T1> xLocalComparer = (xComparer != null) ?
                                xComparer : Comparer<T1>.Default;

                            return xLocalComparer.Compare(
                                left.X, CastToTypeParameter<T2, T1>(right.Y));
                        }
                    case PairComparison.LYRX:
                        {
                            if (typeof(T1) != typeof(T2))
                                break;

                            IComparer<T2> yLocalComparer = (yComparer != null) ?
                                yComparer : Comparer<T2>.Default;

                            return yLocalComparer.Compare(
                                left.Y, CastToTypeParameter<T1, T2>(right.X));
                        }
                    case PairComparison.LYRY:
                        {
                            IComparer<T2> yLocalComparer = (yComparer != null) ?
                                yComparer : Comparer<T2>.Default;

                            return yLocalComparer.Compare(left.Y, right.Y);
                        }
                }
            }

            if (throwOnError)
                throw new ScriptException();
            else
                return 0;
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region IEqualityComparer<IAnyPair<T1, T2>> Members
        public bool Equals(
            IAnyPair<T1, T2> left,
            IAnyPair<T1, T2> right
            )
        {
            if ((left == null) && (right == null))
            {
                return true;
            }
            else if ((left == null) || (right == null))
            {
                return false;
            }
            else
            {
                switch (comparisonType)
                {
                    case PairComparison.LXRX:
                        {
                            IEqualityComparer<T1> xlocalEqualityComparer =
                                (xEqualityComparer != null) ?
                                    xEqualityComparer : EqualityComparer<T1>.Default;

                            return xlocalEqualityComparer.Equals(left.X, right.X);
                        }
                    case PairComparison.LXRY:
                        {
                            if (typeof(T1) != typeof(T2))
                                break;

                            IEqualityComparer<T1> xlocalEqualityComparer =
                                (xEqualityComparer != null) ?
                                    xEqualityComparer : EqualityComparer<T1>.Default;

                            return xlocalEqualityComparer.Equals(
                                left.X, CastToTypeParameter<T2, T1>(right.Y));
                        }
                    case PairComparison.LYRX:
                        {
                            if (typeof(T1) != typeof(T2))
                                break;

                            IEqualityComparer<T2> ylocalEqualityComparer =
                                (yEqualityComparer != null) ?
                                    yEqualityComparer : EqualityComparer<T2>.Default;

                            return ylocalEqualityComparer.Equals(
                                left.Y, CastToTypeParameter<T1, T2>(right.X));
                        }
                    case PairComparison.LYRY:
                        {
                            IEqualityComparer<T2> ylocalEqualityComparer =
                                (yEqualityComparer != null) ?
                                    yEqualityComparer : EqualityComparer<T2>.Default;

                            return ylocalEqualityComparer.Equals(left.Y, right.Y);
                        }
                }
            }

            if (throwOnError)
                throw new ScriptException();
            else
                return false;
        }

        ///////////////////////////////////////////////////////////////////////

        public int GetHashCode(
            IAnyPair<T1, T2> value
            )
        {
            if (value == null)
            {
                return 0;
            }
            else
            {
                switch (comparisonType)
                {
                    case PairComparison.LXRX:
                        {
                            IEqualityComparer<T1> xLocalEqualityComparer =
                                (xEqualityComparer != null) ?
                                    xEqualityComparer : EqualityComparer<T1>.Default;

                            return xLocalEqualityComparer.GetHashCode(value.X);
                        }
                    case PairComparison.LXRY:
                        {
                            IEqualityComparer<T1> xLocalEqualityComparer =
                                (xEqualityComparer != null) ?
                                    xEqualityComparer : EqualityComparer<T1>.Default;

                            IEqualityComparer<T2> yLocalEqualityComparer =
                                (yEqualityComparer != null) ?
                                    yEqualityComparer : EqualityComparer<T2>.Default;

                            return CommonOps.HashCodes.Combine(
                                xLocalEqualityComparer.GetHashCode(value.X),
                                yLocalEqualityComparer.GetHashCode(value.Y));
                        }
                    case PairComparison.LYRX:
                        {
                            IEqualityComparer<T1> xLocalEqualityComparer =
                                (xEqualityComparer != null) ?
                                    xEqualityComparer : EqualityComparer<T1>.Default;

                            IEqualityComparer<T2> yLocalEqualityComparer =
                                (yEqualityComparer != null) ?
                                    yEqualityComparer : EqualityComparer<T2>.Default;

                            return CommonOps.HashCodes.Combine(
                                yLocalEqualityComparer.GetHashCode(value.Y),
                                xLocalEqualityComparer.GetHashCode(value.X));
                        }
                    case PairComparison.LYRY:
                        {
                            IEqualityComparer<T2> yLocalEqualityComparer =
                                (yEqualityComparer != null) ?
                                    yEqualityComparer : EqualityComparer<T2>.Default;

                            return yLocalEqualityComparer.GetHashCode(value.Y);
                        }
                }
            }

            if (throwOnError)
                throw new ScriptException();
            else
                return 0;
        }
        #endregion
    }
}
