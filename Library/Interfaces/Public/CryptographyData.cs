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
    [ObjectId("f711e569-4f88-4e98-b76e-80a4f9fb5b0c")]
    public interface ICryptographyData
    {
        string SymmetricAlgorithmName { get; set; }
        CipherMode CipherMode { get; set; }
        PaddingMode PaddingMode { get; set; }
        ByteList Iv { get; set; }
        ByteList Key { get; set; }
    }
}
