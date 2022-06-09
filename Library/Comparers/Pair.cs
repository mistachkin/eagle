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
#if SERIALIZATION
    [Serializable()]
#endif
    [ObjectId("340d5f84-23a1-4394-b45e-6275cb47af1b")]
    internal sealed class Pair<T> : IComparer<IPair<T>>, IEqualityComparer<IPair<T>>
    {
        #region Private Data
        private PairComparison comparisonType;
        private IComparer<T> comparer;
        private IEqualityComparer<T> equalityComparer;
        private bool throwOnError;
        #endregion

        ///////////////////////////////////////////////////////////////////////////////////////////////

        #region Private Constructors
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
