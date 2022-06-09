/*
 * Custom.cs --
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

namespace Eagle._Comparers
{
#if SERIALIZATION
    [Serializable()]
#endif
    [ObjectId("938e1374-e595-4e83-adda-132a3a363424")]
    internal sealed class Custom : IComparer<string>, IEqualityComparer<string>
    {
        #region Private Data
        private StringComparison comparisonType;
        private IComparer<string> comparer;
        private IEqualityComparer<string> equalityComparer;
        #endregion

        ///////////////////////////////////////////////////////////////////////////////////////////////

        #region Public Constructors
        public Custom(
            StringComparison comparisonType
            )
        {
            this.comparisonType = comparisonType;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public Custom(
            IComparer<string> comparer,
            IEqualityComparer<string> equalityComparer
            )
        {
            this.comparer = comparer;
            this.equalityComparer = equalityComparer;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        #region Dead Code
#if DEAD_CODE
        private Custom(
            IComparer<string> comparer
            )
            : this(comparer, null)
        {
            // do nothing.
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        private Custom(
            IEqualityComparer<string> equalityComparer
            )
            : this(null, equalityComparer)
        {
            // do nothing.
        }
#endif
        #endregion
        #endregion

        ///////////////////////////////////////////////////////////////////////////////////////////////

        #region IComparer<string> Members
        public int Compare(
            string left,
            string right
            )
        {
            if (comparer != null)
                return comparer.Compare(left, right);
            else
                return StringOps.GetStringComparer(comparisonType).Compare(left, right);
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////////////////////////////

        #region IEqualityComparer<string> Members
        public bool Equals(
            string left,
            string right
            )
        {
            if (equalityComparer != null)
                return equalityComparer.Equals(left, right);
            else
                return StringOps.GetStringComparer(comparisonType).Equals(left, right);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public int GetHashCode(
            string value
            )
        {
            if (equalityComparer != null)
                return equalityComparer.GetHashCode(value);
            else
                return StringOps.GetStringComparer(comparisonType).GetHashCode(value);
        }
        #endregion
    }
}
