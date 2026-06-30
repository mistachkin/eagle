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
    /// <summary>
    /// This interface is implemented by entities that supply the parameters
    /// used for RFC 2898 (PBKDF2) byte derivation, optionally reading them from
    /// an external source such as a file.
    /// </summary>
    [ObjectId("9cb2dc05-8b35-4950-81f9-0506a214721d")]
    public interface IRfc2898DataProvider
    {
        /// <summary>
        /// This method obtains the parameters used for RFC 2898 (PBKDF2) byte
        /// derivation, optionally reading them from the specified file.  For
        /// each of the byref parameters, an existing value is used as the
        /// default and may be overwritten with the value supplied by this
        /// provider.
        /// </summary>
        /// <param name="fileName">
        /// The name of the file to read the data from, if any.  This parameter
        /// may be null.
        /// </param>
        /// <param name="encodingName">
        /// The name of the text encoding to use when reading the file, if any.
        /// This parameter may be null.
        /// </param>
        /// <param name="password">
        /// The password used as the basis for the derivation.  On input, this
        /// may contain a default value; on output, it may contain the value
        /// supplied by this provider.
        /// </param>
        /// <param name="salt">
        /// The salt used for the derivation.  On input, this may contain a
        /// default value; on output, it may contain the value supplied by this
        /// provider.
        /// </param>
        /// <param name="iterationCount">
        /// The number of iterations used for the derivation.  On input, this
        /// may contain a default value; on output, it may contain the value
        /// supplied by this provider.
        /// </param>
        /// <param name="hashAlgorithmName">
        /// The name of the hash algorithm used for the derivation.  On input,
        /// this may contain a default value; on output, it may contain the
        /// value supplied by this provider.
        /// </param>
        /// <param name="signature">
        /// The signature associated with the data.  On input, this may contain
        /// a default value; on output, it may contain the value supplied by
        /// this provider.
        /// </param>
        /// <param name="error">
        /// Upon failure, this will contain an appropriate error message.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise, a non-Ok value
        /// with details placed in the <paramref name="error" /> parameter.
        /// </returns>
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
            ref string signature,         /* in, out */
            ref Result error              /* out */
        );
    }
}
