/*
 * SecretData.cs --
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
using Eagle._Containers.Public;

namespace Eagle._Interfaces.Public
{
    [ObjectId("767b3968-685a-408b-bf3d-e86590b3dd58")]
    public interface ISecretData : ISynchronizeBase
    {
        SecretDataFlags Flags { get; set; }

        bool HaveInput { get; }
        bool HaveAuxiliary { get; }
        bool HaveOutput { get; }
        bool HaveSignature { get; }

        string InputString { get; set; }
        ByteList InputBytes { get; set; }

        string AuxiliaryString { get; set; }
        ByteList AuxiliaryBytes { get; set; }

        string OutputString { get; set; }
        ByteList OutputBytes { get; set; }

        string SignatureString { get; set; }
        ByteList SignatureBytes { get; set; }

        ReturnCode Process(ref Result error);
    }
}
