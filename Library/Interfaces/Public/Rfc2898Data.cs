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
    /// <summary>
    /// This interface represents the set of parameters used to derive bytes via
    /// the RFC 2898 (PBKDF2) algorithm.  Each input value is exposed as a
    /// write-only property paired with a companion read-only property that
    /// indicates whether that value has been set.
    /// </summary>
    [ObjectId("f869f4b6-ec84-4eff-9b55-d2b5b1b4ec7c")]
    public interface IRfc2898Data
    {
        /// <summary>
        /// Sets the password used as the basis for the derivation.
        /// </summary>
        string Password { set; }

        /// <summary>
        /// Gets a value indicating whether the password has been set.
        /// </summary>
        bool PasswordSet { get; }

        /// <summary>
        /// Sets the salt used for the derivation.
        /// </summary>
        string Salt { set; }

        /// <summary>
        /// Gets a value indicating whether the salt has been set.
        /// </summary>
        bool SaltSet { get; }

        /// <summary>
        /// Sets the number of iterations used for the derivation.
        /// </summary>
        int IterationCount { set; }

        /// <summary>
        /// Gets a value indicating whether the iteration count has been set.
        /// </summary>
        bool IterationCountSet { get; }

        /// <summary>
        /// Sets the name of the hash algorithm used for the derivation.
        /// </summary>
        string HashAlgorithmName { set; }

        /// <summary>
        /// Gets a value indicating whether the hash algorithm name has been
        /// set.
        /// </summary>
        bool HashAlgorithmNameSet { get; }

        /// <summary>
        /// Sets the signature associated with this data.
        /// </summary>
        string Signature { set; }

        /// <summary>
        /// Gets a value indicating whether the signature has been set.
        /// </summary>
        bool SignatureSet { get; }
    }
}
