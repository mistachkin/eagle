/*
 * Rfc2898Data.cs --
 *
 * Copyright (c) 2007-2012 by Joe Mistachkin.  All rights reserved.
 *
 * See the file "license.terms" for information on usage and redistribution of
 * this file, and for a DISCLAIMER OF ALL WARRANTIES.
 *
 * RCS: @(#) $Id: $
 */

using Eagle._Attributes;

namespace Eagle._Interfaces.Public
{
    [ObjectId("f869f4b6-ec84-4eff-9b55-d2b5b1b4ec7c")]
    public interface IRfc2898Data
    {
        string Password { set; }
        bool PasswordSet { get; }

        string Salt { set; }
        bool SaltSet { get; }

        int IterationCount { set; }
        bool IterationCountSet { get; }

        string HashAlgorithmName { set; }
        bool HashAlgorithmNameSet { get; }
    }
}
