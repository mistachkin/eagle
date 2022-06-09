/*
 * ScriptManager.cs --
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
    [ObjectId("f7cba501-c759-46d9-9e6b-e42e0ed8a044")]
    public interface IScriptManager
    {
        ///////////////////////////////////////////////////////////////////////
        // LIBRARY & HOST SCRIPT MANAGEMENT
        ///////////////////////////////////////////////////////////////////////

        string LibraryPath { get; set; }
        StringList AutoPathList { get; set; }

        ReturnCode PreInitialize(bool force, ref Result error);
        ReturnCode Initialize(bool force, ref Result error);

#if SHELL
        ReturnCode InitializeShell(bool force, ref Result error);
#endif

        ReturnCode GetScript(
            string name,
            ref ScriptFlags scriptFlags,
            ref IClientData clientData,
            ref Result result
            );
    }
}
