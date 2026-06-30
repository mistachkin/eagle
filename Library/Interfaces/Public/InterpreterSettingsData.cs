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
    /// <summary>
    /// This interface defines the data that describes the settings used to
    /// create and configure an interpreter, including its rule set, arguments,
    /// culture, the various flags that control its behavior, the host and
    /// application domain it runs in, the well-known objects associated with
    /// it, and its policies, traces, and library paths.
    /// </summary>
    [ObjectId("3464af98-80fd-4d15-a82d-b4c5a7cc24c6")]
    public interface IInterpreterSettingsData
    {
        /// <summary>
        /// Gets or sets the rule set to associate with the interpreter.
        /// </summary>
        IRuleSet RuleSet { get; set; }

        /// <summary>
        /// Gets or sets the arguments to pass to the interpreter.
        /// </summary>
        IEnumerable<string> Args { get; set; }

        /// <summary>
        /// Gets or sets the name of the culture to use for the interpreter.
        /// </summary>
        string Culture { get; set; }

        /// <summary>
        /// Gets or sets the <see cref="CreateFlags" /> used when creating the
        /// interpreter.
        /// </summary>
        CreateFlags CreateFlags { get; set; }

        /// <summary>
        /// Gets or sets the <see cref="HostCreateFlags" /> used when creating
        /// the interpreter host.
        /// </summary>
        HostCreateFlags HostCreateFlags { get; set; }

        /// <summary>
        /// Gets or sets the <see cref="InitializeFlags" /> used when
        /// initializing the interpreter.
        /// </summary>
        InitializeFlags InitializeFlags { get; set; }

        /// <summary>
        /// Gets or sets the <see cref="ScriptFlags" /> used when locating and
        /// evaluating scripts for the interpreter.
        /// </summary>
        ScriptFlags ScriptFlags { get; set; }

        /// <summary>
        /// Gets or sets the <see cref="InterpreterFlags" /> used to control the
        /// behavior of the interpreter.
        /// </summary>
        InterpreterFlags InterpreterFlags { get; set; }

        /// <summary>
        /// Gets or sets the <see cref="InterpreterTestFlags" /> used to control
        /// the test-related behavior of the interpreter.
        /// </summary>
        InterpreterTestFlags InterpreterTestFlags { get; set; }

        /// <summary>
        /// Gets or sets the <see cref="PluginFlags" /> used to control plugin
        /// loading for the interpreter.
        /// </summary>
        PluginFlags PluginFlags { get; set; }

#if NATIVE && TCL
        /// <summary>
        /// Gets or sets the <see cref="FindFlags" /> used to control how the
        /// native Tcl library is located.
        /// </summary>
        FindFlags FindFlags { get; set; }
        /// <summary>
        /// Gets or sets the <see cref="LoadFlags" /> used to control how the
        /// native Tcl library is loaded.
        /// </summary>
        LoadFlags LoadFlags { get; set; }
#endif

        /// <summary>
        /// Gets or sets the application domain in which the interpreter should
        /// be created.
        /// </summary>
        AppDomain AppDomain { get; set; }

        /// <summary>
        /// Gets or sets the host to associate with the interpreter.
        /// </summary>
        IHost Host { get; set; }

        /// <summary>
        /// Gets or sets the name of the profile to use for the interpreter.
        /// </summary>
        string Profile { get; set; }

        /// <summary>
        /// Gets or sets the object that owns the interpreter.
        /// </summary>
        object Owner { get; set; }

        /// <summary>
        /// Gets or sets the application object to associate with the
        /// interpreter.
        /// </summary>
        object ApplicationObject { get; set; }

        /// <summary>
        /// Gets or sets the policy object to associate with the interpreter.
        /// </summary>
        object PolicyObject { get; set; }

        /// <summary>
        /// Gets or sets the resolver object to associate with the interpreter.
        /// </summary>
        object ResolverObject { get; set; }

        /// <summary>
        /// Gets or sets the user object to associate with the interpreter.
        /// </summary>
        object UserObject { get; set; }

        /// <summary>
        /// Gets or sets the list of policies to associate with the
        /// interpreter.
        /// </summary>
        PolicyList Policies { get; set; }

        /// <summary>
        /// Gets or sets the list of traces to associate with the interpreter.
        /// </summary>
        TraceList Traces { get; set; }

        /// <summary>
        /// Gets or sets the script text to evaluate when initializing the
        /// interpreter.
        /// </summary>
        string Text { get; set; }

        /// <summary>
        /// Gets or sets the path to the script library to use for the
        /// interpreter.
        /// </summary>
        string LibraryPath { get; set; }

        /// <summary>
        /// Gets or sets the list of paths that make up the auto-path for the
        /// interpreter.
        /// </summary>
        StringList AutoPathList { get; set; }
    }
}
