/*
 * Package.cs --
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
using Eagle._Components.Public;

namespace Eagle._Interfaces.Public
{
    [ObjectId("ccf91a70-911c-4253-9599-27fa40adcb2f")]
    public interface IPackage : IPackageData, IState
    {
        [Throw(true)]
        ReturnCode Select(PackagePreference preference, ref Version version, ref Result error);

        //
        // TODO: Change this to use the IInterpreter type.
        //
        [Throw(true)]
        ReturnCode Load(Interpreter interpreter, Version version, ref Result result);
    }
}
