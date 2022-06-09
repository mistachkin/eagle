/*
 * Rfc2898DataProvider.cs --
 *
 * Copyright (c) 2007-2012 by Joe Mistachkin.  All rights reserved.
 *
 * See the file "license.terms" for information on usage and redistribution of
 * this file, and for a DISCLAIMER OF ALL WARRANTIES.
 *
 * RCS: @(#) $Id: $
 */

using Eagle._Attributes;
using Eagle._Components.Public;

namespace Eagle._Interfaces.Public
{
    [ObjectId("9cb2dc05-8b35-4950-81f9-0506a214721d")]
    public interface IRfc2898DataProvider
    {
        //
        // WARNING: This method may not throw exceptions.
        //
        // BUGBUG: The use of a plain string here instead of something like
        //         the SecureString class is due to the requirements of the
        //         Rfc2898DeriveBytes class.
        //
        [Throw(false)]
        ReturnCode GetData(
            string fileName,              /* in: OPTIONAL */
            string encodingName,          /* in: OPTIONAL */
            ref string password,          /* in, out */
            ref string salt,              /* in, out */
            ref int iterationCount,       /* in, out */
            ref string hashAlgorithmName, /* in, out */
            ref Result error              /* out */
        );
    }
}
