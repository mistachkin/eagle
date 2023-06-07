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
    [ObjectId("c6ac4416-85e1-4c15-8a34-e91bda56fe1a")]
    public interface IProvideEntropy
    {
        [Obsolete()]
        void GetBytes(byte[] data);

        [Obsolete()]
        void GetNonZeroBytes(byte[] data);

        //
        // BUGFIX: The "bytes" parameter must be "ref"; otherwise,
        //         cross-domain marshalling does not work right.
        //
        void GetBytes(ref byte[] data);
        void GetNonZeroBytes(ref byte[] data);
    }
}
