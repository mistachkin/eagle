/*
 * BundleData.cs --
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
    [ObjectId("553d7eb3-28f8-42dc-8bd6-96e7fc678aba")]
    public interface IBundleData : IHaveInterpreter
    {
        string Language { get; }
        long Sequence { get; }
        string Vendor { get; }
        string Path { get; }
        string FullName { get; }
        string HashAlgorithmName { get; }
        byte[] FileBytes { get; }
        IsolationLevel IsolationLevel { get; }
        SecurityLevel SecurityLevel { get; }
        ScriptSecurityFlags SecurityFlags { get; }
        IRuleSet RuleSet { get; }
        void MakeImmutable();
    }
}
