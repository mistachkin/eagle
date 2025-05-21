/*
 * InterpreterSettingsData.cs --
 *
 * Copyright (c) 2007-2012 by Joe Mistachkin.  All rights reserved.
 *
 * See the file "license.terms" for information on usage and redistribution of
 * this file, and for a DISCLAIMER OF ALL WARRANTIES.
 *
 * RCS: @(#) $Id: $
 */

using System;
using System.Collections.Generic;
using Eagle._Attributes;
using Eagle._Components.Public;
using Eagle._Containers.Public;

namespace Eagle._Interfaces.Public
{
    [ObjectId("3464af98-80fd-4d15-a82d-b4c5a7cc24c6")]
    public interface IInterpreterSettingsData
    {
        IRuleSet RuleSet { get; set; }
        IEnumerable<string> Args { get; set; }
        string Culture { get; set; }
        CreateFlags CreateFlags { get; set; }
        HostCreateFlags HostCreateFlags { get; set; }
        InitializeFlags InitializeFlags { get; set; }
        ScriptFlags ScriptFlags { get; set; }
        InterpreterFlags InterpreterFlags { get; set; }
        InterpreterTestFlags InterpreterTestFlags { get; set; }
        PluginFlags PluginFlags { get; set; }

#if NATIVE && TCL
        FindFlags FindFlags { get; set; }
        LoadFlags LoadFlags { get; set; }
#endif

        AppDomain AppDomain { get; set; }
        IHost Host { get; set; }
        string Profile { get; set; }
        object Owner { get; set; }
        object ApplicationObject { get; set; }
        object PolicyObject { get; set; }
        object ResolverObject { get; set; }
        object UserObject { get; set; }
        PolicyList Policies { get; set; }
        TraceList Traces { get; set; }
        string Text { get; set; }
        string LibraryPath { get; set; }
        StringList AutoPathList { get; set; }
    }
}
