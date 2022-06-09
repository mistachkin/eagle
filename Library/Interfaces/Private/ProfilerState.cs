/*
 * ProfilerState.cs --
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
using Eagle._Interfaces.Public;

namespace Eagle._Interfaces.Private
{
    [ObjectId("9f2e1444-ac22-4a42-8b31-76489caab961")]
    internal interface IProfilerState : IDisposable, IMaybeDisposed
    {
        void Start();
        double Stop();
        double Stop(bool obfuscate);
        IStringList ToList();
    }
}
