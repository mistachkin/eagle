/*
 * HashOps.cs --
 *
 * Copyright (c) 2007-2012 by Joe Mistachkin.  All rights reserved.
 *
 * See the file "license.terms" for information on usage and redistribution of
 * this file, and for a DISCLAIMER OF ALL WARRANTIES.
 *
 * RCS: @(#) $Id: $
 */

using System.Runtime.InteropServices;

namespace Eagle._Components.Private
{
    [Guid("16f682ec-f7a4-4e9a-b15d-eecfdd07acba")]
    internal static class HashOps
    {
        #region Private Constants
        private const uint FnvOffsetBasis32 = 2166136261;
        private const uint FnvPrime32 = 16777619;
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Hash Support Methods
        public static uint HashFnv1UInt(
            byte[] bytes,
            bool alternate
            )
        {
            if (bytes == null)
                return 0;

            int length = bytes.Length;
            uint result = FnvOffsetBasis32;

            if (length > 0)
            {
                if (alternate)
                {
                    for (int index = 0; index < length; index++)
                    {
                        result ^= bytes[index];
                        result = unchecked(result * FnvPrime32);
                    }
                }
                else
                {
                    for (int index = 0; index < length; index++)
                    {
                        result = unchecked(result * FnvPrime32);
                        result ^= bytes[index];
                    }
                }
            }

            return result;
        }
        #endregion
    }
}
