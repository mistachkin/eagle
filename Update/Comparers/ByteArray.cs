/*
 * ByteArray.cs --
 *
 * Copyright (c) 2007-2012 by Joe Mistachkin.  All rights reserved.
 *
 * See the file "license.terms" for information on usage and redistribution of
 * this file, and for a DISCLAIMER OF ALL WARRANTIES.
 *
 * RCS: @(#) $Id: $
 */

using System.Collections.Generic;
using System.Runtime.InteropServices;
using Eagle._Components.Private;

namespace Eagle._Comparers
{
    [Guid("0b8549d0-a0ce-4bb7-9517-b10ad13148ef")]
    internal sealed class ByteArray : IEqualityComparer<byte[]>
    {
        #region IEqualityComparer<byte[]> Members
        public bool Equals(
            byte[] x,
            byte[] y
            )
        {
            return GenericOps<byte>.Equals(x, y);
        }

        ///////////////////////////////////////////////////////////////////////

        public int GetHashCode(
            byte[] obj
            )
        {
            return unchecked((int)HashOps.HashFnv1UInt(obj, true));
        }
        #endregion
    }
}
