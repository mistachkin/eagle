/*
 * ProvideEntropy.cs --
 *
 * Copyright (c) 2007-2012 by Joe Mistachkin.  All rights reserved.
 *
 * See the file "license.terms" for information on usage and redistribution of
 * this file, and for a DISCLAIMER OF ALL WARRANTIES.
 *
 * RCS: @(#) $Id: $
 */

using System;
using Eagle._Attributes;

namespace Eagle._Interfaces.Public
{
    /// <summary>
    /// This interface is implemented by entities that can supply entropy (i.e.
    /// random bytes), typically for use as a source of randomness by the Eagle
    /// core or by hosted scripts.
    /// </summary>
    [ObjectId("c6ac4416-85e1-4c15-8a34-e91bda56fe1a")]
    public interface IProvideEntropy
    {
        /// <summary>
        /// Fills the specified array with entropy (i.e. random bytes).
        /// </summary>
        /// <param name="data">
        /// The array to fill with random bytes.  This parameter should not be
        /// null.
        /// </param>
        [Obsolete()]
        void GetBytes(byte[] data);

        /// <summary>
        /// Fills the specified array with entropy (i.e. random bytes), none of
        /// which are zero.
        /// </summary>
        /// <param name="data">
        /// The array to fill with non-zero random bytes.  This parameter should
        /// not be null.
        /// </param>
        [Obsolete()]
        void GetNonZeroBytes(byte[] data);

        //
        // BUGFIX: The "bytes" parameter must be "ref"; otherwise,
        //         cross-domain marshalling does not work right.
        //
        /// <summary>
        /// Fills the specified array with entropy (i.e. random bytes).
        /// </summary>
        /// <param name="data">
        /// The array to fill with random bytes.  This parameter is passed by
        /// reference to support cross-domain marshalling.  This parameter should
        /// not be null.
        /// </param>
        void GetBytes(ref byte[] data);
        /// <summary>
        /// Fills the specified array with entropy (i.e. random bytes), none of
        /// which are zero.
        /// </summary>
        /// <param name="data">
        /// The array to fill with non-zero random bytes.  This parameter is
        /// passed by reference to support cross-domain marshalling.  This
        /// parameter should not be null.
        /// </param>
        void GetNonZeroBytes(ref byte[] data);
    }
}
