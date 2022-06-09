/*
 * ScriptData.cs --
 *
 * Copyright (c) 2007-2012 by Joe Mistachkin.  All rights reserved.
 *
 * See the file "license.terms" for information on usage and redistribution of
 * this file, and for a DISCLAIMER OF ALL WARRANTIES.
 *
 * RCS: @(#) $Id: $
 */

#if XML
using System;
#endif

using System.Collections;

#if CAS_POLICY
using System.Security.Cryptography;
using System.Security.Policy;
#endif

using Eagle._Attributes;
using Eagle._Components.Public;

namespace Eagle._Interfaces.Public
{
    [ObjectId("16affcc8-1ef3-48cf-ae63-8ecd49271d95")]
    public interface IScriptData
    {
        string Type { get; }
        IList Parts { get; }
        string Text { get; }

#if XML
        XmlBlockType BlockType { get; }
        DateTime TimeStamp { get; }
        string PublicKeyToken { get; }
        byte[] Signature { get; }
#endif

#if CAS_POLICY
        Evidence Evidence { get; }
        byte[] HashValue { get; }
        HashAlgorithm HashAlgorithm { get; }
#endif
    }
}
