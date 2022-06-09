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
    [ObjectId("79224114-3ad9-4052-8df0-5e3442b594a5")]
    public static class ScriptTypes
    {
        public static readonly string Invalid = null;
        public static readonly string None = "none";
        public static readonly string All = "all";                 // test suite wrapper (via GetData)
        public static readonly string Automatic = "automatic";     // automatically determine based on context
        public static readonly string Constraints = "constraints"; // test suite constraints (via GetData)
        public static readonly string Empty = "empty";             // empty string (via GetData)
        public static readonly string Epilogue = "epilogue";       // test suite epilogue (via GetData)
        public static readonly string Initialization = "init";     // library initialization / routines (via GetData)
        public static readonly string Embedding = "embed";         // application embedding initialization (via GetData)
        public static readonly string Vendor = "vendor";           // vendor initialization (via GetData)
        public static readonly string Startup = "startup";         // application / user initialization
        public static readonly string Safe = "safe";               // safe library initialization / routines (via GetData)
        public static readonly string Shell = "shell";             // interactive shell customization (via GetData)
        public static readonly string Test = "test";               // test library initialization / routines (via GetData)
        public static readonly string PackageIndex = "pkgIndex";   // package index (via GetData)
        public static readonly string Prologue = "prologue";       // test suite prologue (via GetData)
        public static readonly string Queue = "queue";             // queued script (via QueueScript)
        public static readonly string Idle = "idle";               // idle script (via [after idle])
        public static readonly string Timer = "timer";             // timer script (via [after ms])
        public static readonly string Stream = "stream";           // via the engine method ReadScriptStream
        public static readonly string File = "file";               // via the engine method ReadScriptFile

#if XML
        public static readonly string Block = "block";             // XML script block (via ReadScriptXml)
#endif
    }
}
