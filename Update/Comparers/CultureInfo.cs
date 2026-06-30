/*
 * CultureInfo.cs --
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
using System.Globalization;
using System.Runtime.InteropServices;

namespace Eagle._Comparers
{
    /// <summary>
    /// This class implements an equality comparer for
    /// <see cref="CultureInfo" /> instances, treating two null references as
    /// equal and otherwise deferring to the value-based equality of the
    /// instances.
    /// </summary>
    [Guid("d0b3d9fa-fdf5-46bf-838b-637b2d2083f6")]
    internal sealed class _CultureInfo : IEqualityComparer<CultureInfo>
    {
        #region IEqualityComparer<CultureInfo> Members
        /// <summary>
        /// This method determines whether two <see cref="CultureInfo" />
        /// instances are equal.
        /// </summary>
        /// <param name="x">
        /// The first instance to compare.  This parameter may be null.
        /// </param>
        /// <param name="y">
        /// The second instance to compare.  This parameter may be null.
        /// </param>
        /// <returns>
        /// True if the two instances are equal (including when both are null);
        /// otherwise, false.
        /// </returns>
        public bool Equals(
            CultureInfo x,
            CultureInfo y
            )
        {
            if ((x == null) && (y == null))
                return true;
            else if ((x == null) || (y == null))
                return false;
            else
                return x.Equals(y);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method returns a hash code for the specified
        /// <see cref="CultureInfo" /> instance.
        /// </summary>
        /// <param name="obj">
        /// The instance for which to compute a hash code.  This parameter may
        /// be null.
        /// </param>
        /// <returns>
        /// The hash code for the specified instance, or zero if it is null.
        /// </returns>
        public int GetHashCode(
            CultureInfo obj
            )
        {
            return (obj != null) ? obj.GetHashCode() : 0;
        }
        #endregion
    }
}
