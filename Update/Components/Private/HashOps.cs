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
    /// <summary>
    /// This class provides hashing support methods, including an
    /// implementation of the 32-bit FNV-1 hash algorithm.
    /// </summary>
    [Guid("16f682ec-f7a4-4e9a-b15d-eecfdd07acba")]
    internal static class HashOps
    {
        #region Private Constants
        /// <summary>
        /// The 32-bit FNV offset basis used to seed the hash computation.
        /// </summary>
        private const uint FnvOffsetBasis32 = 2166136261;

        /// <summary>
        /// The 32-bit FNV prime used during the hash computation.
        /// </summary>
        private const uint FnvPrime32 = 16777619;
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Hash Support Methods
        /// <summary>
        /// This method computes a 32-bit FNV-1 hash over the specified bytes.
        /// </summary>
        /// <param name="bytes">
        /// The bytes to be hashed; if this value is null, zero is returned.
        /// </param>
        /// <param name="alternate">
        /// Non-zero to use the alternate (FNV-1a) ordering, in which each byte
        /// is mixed in before the multiplication; zero to use the standard
        /// FNV-1 ordering.
        /// </param>
        /// <returns>
        /// The computed 32-bit hash value.
        /// </returns>
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
