/*
 * CryptographyData.cs --
 *
 * Copyright (c) 2007-2012 by Joe Mistachkin.  All rights reserved.
 *
 * See the file "license.terms" for information on usage and redistribution of
 * this file, and for a DISCLAIMER OF ALL WARRANTIES.
 *
 * RCS: @(#) $Id: $
 */

using System.Security.Cryptography;
using Eagle._Attributes;
using Eagle._Containers.Public;

namespace Eagle._Interfaces.Public
{
    /// <summary>
    /// This interface is implemented by objects that hold the parameters used
    /// to perform symmetric encryption and decryption, including the
    /// algorithm name, cipher mode, padding mode, initialization vector, and
    /// key.
    /// </summary>
    [ObjectId("f711e569-4f88-4e98-b76e-80a4f9fb5b0c")]
    public interface ICryptographyData
    {
        /// <summary>
        /// Gets or sets the name of the symmetric algorithm to use.
        /// </summary>
        string SymmetricAlgorithmName { get; set; }

        /// <summary>
        /// Gets or sets the <see cref="CipherMode" /> used by the symmetric
        /// algorithm.
        /// </summary>
        CipherMode CipherMode { get; set; }

        /// <summary>
        /// Gets or sets the <see cref="PaddingMode" /> used by the symmetric
        /// algorithm.
        /// </summary>
        PaddingMode PaddingMode { get; set; }

        /// <summary>
        /// Gets or sets the initialization vector used by the symmetric
        /// algorithm.
        /// </summary>
        ByteList Iv { get; set; }

        /// <summary>
        /// Gets or sets the secret key used by the symmetric algorithm.
        /// </summary>
        ByteList Key { get; set; }
    }
}
