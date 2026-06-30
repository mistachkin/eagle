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
    /// <summary>
    /// This class provides comparison and equality comparison for
    /// <see cref="IAnyPair{T1, T2}" /> instances.  The specific components of
    /// each pair that are compared, and the order in which they are compared,
    /// are determined by a <see cref="PairComparison" /> value, optionally
    /// using caller-supplied comparers and converting between the two component
    /// types when necessary.
    /// </summary>
    /// <typeparam name="T1">
    /// The type of the first (X) component of each pair.
    /// </typeparam>
    /// <typeparam name="T2">
    /// The type of the second (Y) component of each pair.
    /// </typeparam>
#if SERIALIZATION
    [Serializable()]
#endif
    [ObjectId("f9da1d4e-8c47-47e9-a1b8-f975e3fb2408")]
    internal sealed class AnyPair<T1, T2> :
            IComparer<IAnyPair<T1, T2>>, IEqualityComparer<IAnyPair<T1, T2>>
    {
        #region Private Data
        /// <summary>
        /// The value that determines which components of each pair are compared
        /// and in what order.
        /// </summary>
        private PairComparison comparisonType;

        /// <summary>
        /// The comparer used for the first (X) component, or null to use the
        /// default comparer.
        /// </summary>
        private IComparer<T1> xComparer;

        /// <summary>
        /// The equality comparer used for the first (X) component, or null to
        /// use the default equality comparer.
        /// </summary>
        private IEqualityComparer<T1> xEqualityComparer;

        /// <summary>
        /// The comparer used for the second (Y) component, or null to use the
        /// default comparer.
        /// </summary>
        private IComparer<T2> yComparer;

        /// <summary>
        /// The equality comparer used for the second (Y) component, or null to
        /// use the default equality comparer.
        /// </summary>
        private IEqualityComparer<T2> yEqualityComparer;

        /// <summary>
        /// The format provider used when converting a component value between
        /// the two component types.
        /// </summary>
        private IFormatProvider formatProvider;

        /// <summary>
        /// When non-zero, an exception is thrown when a comparison cannot be
        /// performed; otherwise, a default result is returned.
        /// </summary>
        private bool throwOnError;
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Private Constructors
        /// <summary>
        /// Constructs an instance of this class with all fields set to their
        /// default (none) values.
        /// </summary>
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
        /// <summary>
        /// Constructs an instance of this class that uses the default comparers
        /// for both component types.
        /// </summary>
        /// <param name="comparisonType">
        /// The value that determines which components of each pair are compared
        /// and in what order.
        /// </param>
        /// <param name="throwOnError">
        /// Non-zero to throw an exception when a comparison cannot be performed;
        /// otherwise, zero to return a default result.
        /// </param>
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

        /// <summary>
        /// Constructs an instance of this class that uses the specified
        /// comparers and format provider.
        /// </summary>
        /// <param name="comparisonType">
        /// The value that determines which components of each pair are compared
        /// and in what order.
        /// </param>
        /// <param name="xComparer">
        /// The comparer to use for the first (X) component, or null to use the
        /// default comparer.
        /// </param>
        /// <param name="xEqualityComparer">
        /// The equality comparer to use for the first (X) component, or null to
        /// use the default equality comparer.
        /// </param>
        /// <param name="yComparer">
        /// The comparer to use for the second (Y) component, or null to use the
        /// default comparer.
        /// </param>
        /// <param name="yEqualityComparer">
        /// The equality comparer to use for the second (Y) component, or null
        /// to use the default equality comparer.
        /// </param>
        /// <param name="formatProvider">
        /// The format provider to use when converting a component value between
        /// the two component types.
        /// </param>
        /// <param name="throwOnError">
        /// Non-zero to throw an exception when a comparison cannot be performed;
        /// otherwise, zero to return a default result.
        /// </param>
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
        /// <summary>
        /// This method converts a value from one component type to another,
        /// using the configured format provider when the source value is
        /// convertible.
        /// </summary>
        /// <typeparam name="T1A">
        /// The type of the source value to convert from.
        /// </typeparam>
        /// <typeparam name="T2A">
        /// The type to convert the source value to.
        /// </typeparam>
        /// <param name="value">
        /// The value to convert.
        /// </param>
        /// <returns>
        /// The converted value, or the default value of
        /// <typeparamref name="T2A" /> if the conversion cannot be performed and
        /// errors are not configured to throw.
        /// </returns>
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
        /// <summary>
        /// This method compares two <see cref="IAnyPair{T1, T2}" /> instances,
        /// comparing the components selected by the configured comparison type.
        /// </summary>
        /// <param name="left">
        /// The first pair to compare. This parameter may be null.
        /// </param>
        /// <param name="right">
        /// The second pair to compare. This parameter may be null.
        /// </param>
        /// <returns>
        /// Zero if the pairs are equal, a negative number if
        /// <paramref name="left" /> sorts before <paramref name="right" />, or a
        /// positive number if <paramref name="left" /> sorts after
        /// <paramref name="right" />. A null pair sorts before a non-null pair.
        /// </returns>
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
        /// <summary>
        /// This method determines whether two
        /// <see cref="IAnyPair{T1, T2}" /> instances are equal, comparing the
        /// components selected by the configured comparison type.
        /// </summary>
        /// <param name="left">
        /// The first pair to compare. This parameter may be null.
        /// </param>
        /// <param name="right">
        /// The second pair to compare. This parameter may be null.
        /// </param>
        /// <returns>
        /// True if the two pairs are equal; otherwise, false.
        /// </returns>
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

        /// <summary>
        /// This method returns a hash code for the specified
        /// <see cref="IAnyPair{T1, T2}" /> instance, computed from the
        /// components selected by the configured comparison type.
        /// </summary>
        /// <param name="value">
        /// The pair to compute a hash code for. This parameter may be null.
        /// </param>
        /// <returns>
        /// A hash code for the specified pair, or zero if it is null.
        /// </returns>
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
