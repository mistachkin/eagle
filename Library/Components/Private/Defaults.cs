/*
 * Defaults.cs --
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

namespace Eagle._Components.Private
{
    /// <summary>
    /// This class provides the centralized, hard-coded default values for the
    /// various flag enumerations used throughout the interpreter.  These values
    /// abstract away the literal defaults so they can be overridden as needed.
    /// The fields are intentionally not read-only and should not be changed
    /// unless their exact usage is fully understood.
    /// </summary>
    [ObjectId("ae7a1c2c-0e8d-4830-8004-d59af53f3aff")]
    internal static class Defaults
    {
        ///////////////////////////////////////////////////////////////////////
        //    *WARNING* *WARNING* *WARNING* *WARNING* *WARNING* *WARNING*    //
        ///////////////////////////////////////////////////////////////////////
        //
        // HACK: These are all purposely not read-only.  They are used to
        //       abstract away hard-coded default flag values.  Please do
        //       not change these values unless you know exactly how they
        //       are used.
        //
        ///////////////////////////////////////////////////////////////////////
        //    *WARNING* *WARNING* *WARNING* *WARNING* *WARNING* *WARNING*    //
        ///////////////////////////////////////////////////////////////////////

        #region Public Static Data
        /// <summary>
        /// The default creation flag types used when creating an interpreter.
        /// </summary>
        public static CreationFlagTypes CreationFlagTypes =
            CreationFlagTypes.Default;

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// The default creation flags used when creating an interpreter.
        /// </summary>
        public static CreateFlags CreateFlags = CreateFlags.Default;

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// The default flags used when creating an interpreter host.
        /// </summary>
        public static HostCreateFlags HostCreateFlags =
            HostCreateFlags.Default;

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// The default flags used when initializing an interpreter.
        /// </summary>
        public static InitializeFlags InitializeFlags =
            InitializeFlags.Default;

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// The default flags used when locating and loading scripts.
        /// </summary>
        public static ScriptFlags ScriptFlags = ScriptFlags.Default;

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// The default per-interpreter behavior flags.
        /// </summary>
        public static InterpreterFlags InterpreterFlags =
            InterpreterFlags.Default;

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// The default per-interpreter flags used by the test suite.
        /// </summary>
        public static InterpreterTestFlags InterpreterTestFlags =
            InterpreterTestFlags.Default;

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// The default per-interpreter state flags.
        /// </summary>
        public static InterpreterStateFlags InterpreterStateFlags =
            InterpreterStateFlags.Default;

        ///////////////////////////////////////////////////////////////////////

#if SHELL
        /// <summary>
        /// The default flags used by the interactive loop of the shell.
        /// </summary>
        public static InteractiveLoopFlags InteractiveLoopFlags =
            InteractiveLoopFlags.Default;

        /// <summary>
        /// The default flags used when displaying the interactive prompt.
        /// </summary>
        public static PromptFlags PromptFlags = PromptFlags.Default;
#endif

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// The default flags used when loading and managing plugins.
        /// </summary>
        public static PluginFlags PluginFlags = PluginFlags.Default;

        ///////////////////////////////////////////////////////////////////////

#if NATIVE && TCL
        /// <summary>
        /// The default flags used when finding native Tcl libraries.
        /// </summary>
        public static FindFlags FindFlags = FindFlags.Default;

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// The default flags used when loading native Tcl libraries.
        /// </summary>
        public static LoadFlags LoadFlags = LoadFlags.Default;
#endif

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// The default flags used when processing package index files.
        /// </summary>
        public static PackageIndexFlags PackageIndexFlags =
            PackageIndexFlags.Default;

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// The default flags used by data-related operations.
        /// </summary>
        public static DataFlags DataFlags = DataFlags.Default;

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// The default flags used when creating and invoking delegates.
        /// </summary>
        public static DelegateFlags DelegateFlags = DelegateFlags.Default;

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// The default flags used when parsing and converting values.
        /// </summary>
        public static ValueFlags ValueFlags = ValueFlags.None;

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// The default flags controlling the behavior of command options.
        /// </summary>
        public static OptionBehaviorFlags OptionBehaviorFlags =
            OptionBehaviorFlags.None;

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// The default keep-alive setting for sockets, or null to use the
        /// system default.
        /// </summary>
        public static bool? SocketKeepAlive = null; // TODO: Good default?
        #endregion
    }
}
