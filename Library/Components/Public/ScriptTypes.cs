/*
 * ScriptTypes.cs --
 *
 * Copyright (c) 2007-2012 by Joe Mistachkin.  All rights reserved.
 *
 * See the file "license.terms" for information on usage and redistribution of
 * this file, and for a DISCLAIMER OF ALL WARRANTIES.
 *
 * RCS: @(#) $Id: $
 */

using Eagle._Attributes;

namespace Eagle._Components.Public
{
    /// <summary>
    /// This class provides the well-known names used to identify the various
    /// categories ("types") of script handled by the engine, such as library
    /// initialization scripts, test suite scripts, and queued, idle, or timer
    /// scripts.
    /// </summary>
    [ObjectId("79224114-3ad9-4052-8df0-5e3442b594a5")]
    public static class ScriptTypes
    {
        /// <summary>
        /// The sentinel used to represent an invalid or unspecified script type
        /// (a null string).
        /// </summary>
        public static readonly string Invalid = null;

        /// <summary>
        /// The script type indicating that no specific script type applies.
        /// </summary>
        public static readonly string None = "none";

        /// <summary>
        /// The script type used as the test suite wrapper (via GetData).
        /// </summary>
        public static readonly string All = "all";                     // test suite wrapper (via GetData)

        /// <summary>
        /// The script type indicating that the actual type should be determined
        /// automatically based on context.
        /// </summary>
        public static readonly string Automatic = "automatic";         // automatically determine based on context

        /// <summary>
        /// The script type used for the test suite constraints (via GetData).
        /// </summary>
        public static readonly string Constraints = "constraints";     // test suite constraints (via GetData)

        /// <summary>
        /// The script type representing the empty string (via GetData).
        /// </summary>
        public static readonly string Empty = "empty";                 // empty string (via GetData)

        /// <summary>
        /// The script type used for the test suite epilogue (via GetData).
        /// </summary>
        public static readonly string Epilogue = "epilogue";           // test suite epilogue (via GetData)

        /// <summary>
        /// The script type used for the plugin loader routines (via GetData).
        /// </summary>
        public static readonly string Loader = "loader";               // plugin loader routines (via GetData)

        /// <summary>
        /// The script type used for the library initialization routines (via
        /// GetData).
        /// </summary>
        public static readonly string Initialization = "init";         // library initialization / routines (via GetData)

        /// <summary>
        /// The script type used for application embedding initialization (via
        /// GetData).
        /// </summary>
        public static readonly string Embedding = "embed";             // application embedding initialization (via GetData)

        /// <summary>
        /// The script type used for vendor initialization (via GetData).
        /// </summary>
        public static readonly string Vendor = "vendor";               // vendor initialization (via GetData)

        /// <summary>
        /// The script type used for trusted remote initialization (via
        /// GetData).
        /// </summary>
        public static readonly string TrustedRemote = "trustedRemote"; // trusted remote initialization (via GetData)

        /// <summary>
        /// The script type used for synchronous application or user
        /// initialization.
        /// </summary>
        public static readonly string Startup = "startup";             // application / user initialization (synchronous)

        /// <summary>
        /// The script type used for asynchronous application or user
        /// initialization.
        /// </summary>
        public static readonly string Worker = "worker";               // application / user initialization (asynchronous)

        /// <summary>
        /// The script type used for safe library initialization routines (via
        /// GetData).
        /// </summary>
        public static readonly string Safe = "safe";                   // safe library initialization / routines (via GetData)

        /// <summary>
        /// The script type used for synchronous interactive shell customization
        /// (via GetData).
        /// </summary>
        public static readonly string Shell = "shell";                 // interactive shell customization (synchronous, via GetData)

        /// <summary>
        /// The script type used for asynchronous interactive shell
        /// customization (via GetData).
        /// </summary>
        public static readonly string ShellWorker = "shellWorker";     // interactive shell customization (asynchronous, via GetData)

        /// <summary>
        /// The script type used for test library initialization routines (via
        /// GetData).
        /// </summary>
        public static readonly string Test = "test";                   // test library initialization / routines (via GetData)

        /// <summary>
        /// The script type used for the package index (via GetData).
        /// </summary>
        public static readonly string PackageIndex = "pkgIndex";       // package index (via GetData)

        /// <summary>
        /// The script type used for the test suite prologue (via GetData).
        /// </summary>
        public static readonly string Prologue = "prologue";           // test suite prologue (via GetData)

        /// <summary>
        /// The script type used for a queued script (via QueueScript).
        /// </summary>
        public static readonly string Queue = "queue";                 // queued script (via QueueScript)

        /// <summary>
        /// The script type used for an idle script (via [after idle]).
        /// </summary>
        public static readonly string Idle = "idle";                   // idle script (via [after idle])

        /// <summary>
        /// The script type used for a timer script (via [after ms]).
        /// </summary>
        public static readonly string Timer = "timer";                 // timer script (via [after ms])

        /// <summary>
        /// The script type used for a script read via the engine method
        /// ReadScriptStream.
        /// </summary>
        public static readonly string Stream = "stream";               // via the engine method ReadScriptStream

        /// <summary>
        /// The script type used for a script read via the engine method
        /// ReadScriptFile.
        /// </summary>
        public static readonly string File = "file";                   // via the engine method ReadScriptFile

        /// <summary>
        /// The script type used for a script obtained via the ISnippetManager
        /// interface.
        /// </summary>
        public static readonly string Snippet = "snippet";             // via the ISnippetManager interface

#if XML
        /// <summary>
        /// The script type used for an XML script block (via ReadScriptXml).
        /// </summary>
        public static readonly string Block = "block";                 // XML script block (via ReadScriptXml)
#endif

        /// <summary>
        /// The script type used for a bundle of scripts.
        /// </summary>
        public static readonly string Bundle = "bundle";
    }
}
