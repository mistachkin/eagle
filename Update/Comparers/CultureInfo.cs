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
    [Guid("d0b3d9fa-fdf5-46bf-838b-637b2d2083f6")]
    internal sealed class _CultureInfo : IEqualityComparer<CultureInfo>
    {
        #region IEqualityComparer<CultureInfo> Members
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

        public int GetHashCode(
            CultureInfo obj
            )
        {
            return (obj != null) ? obj.GetHashCode() : 0;
        }
        #endregion
    }
}
