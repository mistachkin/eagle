/*
 * Module.cs --
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
using Eagle._Interfaces.Public;

namespace Eagle._Interfaces.Private
{
    [ObjectId("e56d5052-dcb4-48b7-bdd4-e9530427d1a6")]
    internal interface IModule : IIdentifier, IWrapperData
    {
        ModuleFlags Flags { get; }
        string FileName { get; }
        IntPtr Module { get; }
        int ReferenceCount { get; }

        ReturnCode Load(ref Result error);
        ReturnCode Load(ref int loaded, ref Result error);

        ReturnCode Unload(ref Result error);
        ReturnCode Unload(ref int loaded, ref Result error);
    }
}
