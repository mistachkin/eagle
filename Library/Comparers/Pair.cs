/*
 * Pair.cs --
 *
 * Copyright (c) 2007-2012 by Joe Mistachkin.  All rights reserved.
 *
 * See the file "license.terms" for information on usage and redistribution of
 * this file, and for a DISCLAIMER OF ALL WARRANTIES.
 *
 * RCS: @(#) $Id: $
 */

#if SERIALIZATION
using System;
#endif

using System.Collections.Generic;
using Eagle._Attributes;
using Eagle._Components.Private;
using Eagle._Components.Public;
using Eagle._Interfaces.Public;

namespace Eagle._Comparers
{
    /// <summary>
    /// This class compares and tests for equality pairs of values (instances
    /// of <see cref="IPair{T}" />), using the configured
    /// <see cref="PairComparison" /> mode to select which element (the X or Y
    /// component) of each pair participates in the comparison.
    /// </summary>
    /// <typeparam name="T">
    /// The type of the elements contained by the pairs being compared.
    /// </typeparam>
#if SERIALIZATION
    [Serializable()]
#endif
    [ObjectId("340d5f84-23a1-4394-b45e-6275cb47af1b")]
    internal sealed class Pair<T> : IComparer<IPair<T>>, IEqualityComparer<IPair<T>>
    {
        #region Private Data
        /// <summary>
        /// The mode that selects which element of each pair (the X or Y
        /// component) is used when comparing or testing for equality.
        /// </summary>
        private PairComparison comparisonType;

        /// <summary>
        /// The comparer used to order the selected pair elements, or null to
        /// use the default comparer for the element type.
        /// </summary>
        private IComparer<T> comparer;

        /// <summary>
        /// The equality comparer used to test the selected pair elements for
        /// equality and to compute hash codes, or null to use the default
        /// equality comparer for the element type.
        /// </summary>
        private IEqualityComparer<T> equalityComparer;

        /// <summary>
        /// When true, an unrecognized comparison mode causes a
        /// <see cref="ScriptException" /> to be thrown instead of returning a
        /// default result.
        /// </summary>
        private bool throwOnError;
        #endregion

        ///////////////////////////////////////////////////////////////////////////////////////////////

        #region Private Constructors
        /// <summary>
        /// Constructs an instance of this class with no comparison mode, no
        /// custom comparers, and error throwing disabled.
        /// </summary>
        private Pair()
        {
            this.comparisonType = PairComparison.None;
            this.comparer = null;
            this.equalityComparer = null;
            this.throwOnError = false;
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////////////////////////////

        #region Public Constructors
        /// <summary>
        /// Constructs an instance of this class using the specified comparison
        /// mode and the default comparers for the element type.
        /// </summary>
        /// <param name="comparisonType">
        /// The mode that selects which element of each pair is used when
        /// comparing or testing for equality.
        /// </param>
        /// <param name="throwOnError">
        /// When true, an unrecognized comparison mode causes a
        /// <see cref="ScriptException" /> to be thrown.
        /// </param>
        public Pair(
            PairComparison comparisonType,
            bool throwOnError
            )
            : this()
        {
            this.comparisonType = comparisonType;
            this.throwOnError = throwOnError;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Constructs an instance of this class using the specified comparison
        /// mode and the specified custom comparers.
        /// </summary>
        /// <param name="comparisonType">
        /// The mode that selects which element of each pair is used when
        /// comparing or testing for equality.
        /// </param>
        /// <param name="comparer">
        /// The comparer used to order the selected pair elements, or null to
        /// use the default comparer for the element type.
        /// </param>
        /// <param name="equalityComparer">
        /// The equality comparer used to test the selected pair elements for
        /// equality and to compute hash codes, or null to use the default
        /// equality comparer for the element type.
        /// </param>
        /// <param name="throwOnError">
        /// When true, an unrecognized comparison mode causes a
        /// <see cref="ScriptException" /> to be thrown.
        /// </param>
        public Pair(
            PairComparison comparisonType,
            IComparer<T> comparer,
            IEqualityComparer<T> equalityComparer,
            bool throwOnError
            )
            : this(comparisonType, throwOnError)
        {
            this.comparer = comparer;
            this.equalityComparer = equalityComparer;
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////////////////////////////

        #region IComparer<IPair<T>> Members
        /// <summary>
        /// Compares two pairs and returns a value indicating their relative
        /// order, using the configured comparison mode to select which element
        /// of each pair is compared.  A null pair sorts before a non-null pair.
        /// </summary>
        /// <param name="left">
        /// The first pair to compare.
        /// </param>
        /// <param name="right">
        /// The second pair to compare.
        /// </param>
        /// <returns>
        /// Less than zero if <paramref name="left" /> is less than
        /// <paramref name="right" />, zero if they are equal, and greater than
        /// zero if <paramref name="left" /> is greater than
        /// <paramref name="right" />.
        /// </returns>
        public int Compare(
            IPair<T> left,
            IPair<T> right
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
                IComparer<T> localComparer = (comparer != null) ?
                    comparer : Comparer<T>.Default;

                switch (comparisonType)
                {
                    case PairComparison.LXRX:
                        {
                            return localComparer.Compare(left.X, right.X);
                        }
                    case PairComparison.LXRY:
                        {
                            return localComparer.Compare(left.X, right.Y);
                        }
                    case PairComparison.LYRX:
                        {
                            return localComparer.Compare(left.Y, right.X);
                        }
                    case PairComparison.LYRY:
                        {
                            return localComparer.Compare(left.Y, right.Y);
                        }
                }
            }

            if (throwOnError)
                throw new ScriptException();
            else
                return 0;
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////////////////////////////

        #region IEqualityComparer<IPair<T>> Members
        /// <summary>
        /// Determines whether two pairs are equal, using the configured
        /// comparison mode to select which element of each pair is tested.
        /// </summary>
        /// <param name="left">
        /// The first pair to compare.
        /// </param>
        /// <param name="right">
        /// The second pair to compare.
        /// </param>
        /// <returns>
        /// True if the pairs are considered equal; otherwise, false.
        /// </returns>
        public bool Equals(
            IPair<T> left,
            IPair<T> right
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
                IEqualityComparer<T> localEqualityComparer = (equalityComparer != null) ?
                    equalityComparer : EqualityComparer<T>.Default;

                switch (comparisonType)
                {
                    case PairComparison.LXRX:
                        {
                            return localEqualityComparer.Equals(left.X, right.X);
                        }
                    case PairComparison.LXRY:
                        {
                            return localEqualityComparer.Equals(left.X, right.Y);
                        }
                    case PairComparison.LYRX:
                        {
                            return localEqualityComparer.Equals(left.Y, right.X);
                        }
                    case PairComparison.LYRY:
                        {
                            return localEqualityComparer.Equals(left.Y, right.Y);
                        }
                }
            }

            if (throwOnError)
                throw new ScriptException();
            else
                return false;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Returns a hash code for the specified pair, using the configured
        /// comparison mode to select which element (or combination of elements)
        /// of the pair contributes to the hash code.
        /// </summary>
        /// <param name="value">
        /// The pair for which a hash code is to be computed.
        /// </param>
        /// <returns>
        /// A hash code for the specified pair, or zero if it is null.
        /// </returns>
        public int GetHashCode(
            IPair<T> value
            )
        {
            if (value == null)
            {
                return 0;
            }
            else
            {
                IEqualityComparer<T> localEqualityComparer = (equalityComparer != null) ?
                    equalityComparer : EqualityComparer<T>.Default;

                switch (comparisonType)
                {
                    case PairComparison.LXRX:
                        {
                            return localEqualityComparer.GetHashCode(value.X);
                        }
                    case PairComparison.LXRY:
                        {
                            return CommonOps.HashCodes.Combine(
                                localEqualityComparer.GetHashCode(value.X),
                                localEqualityComparer.GetHashCode(value.Y));
                        }
                    case PairComparison.LYRX:
                        {
                            return CommonOps.HashCodes.Combine(
                                localEqualityComparer.GetHashCode(value.Y),
                                localEqualityComparer.GetHashCode(value.X));
                        }
                    case PairComparison.LYRY:
                        {
                            return localEqualityComparer.GetHashCode(value.Y);
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
