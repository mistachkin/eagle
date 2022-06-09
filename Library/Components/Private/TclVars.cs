/*
 * TclVars.cs --
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

namespace Eagle._Components.Private
{
    [ObjectId("f48e798e-7e3a-4a21-b27b-3d20d30c91bf")]
    internal static class TclVars
    {
        #region Tcl Package Naming
        [ObjectId("9b266cce-c72d-4b6a-88c9-7f4bb7e26470")]
        internal static class Package
        {
            //
            // NOTE: The name used by the native Tcl core "package".
            //
            public static readonly string Name = "Tcl";

            ///////////////////////////////////////////////////////////////////

            //
            // NOTE: Used to set the variables in the platform array(s),
            //       etc.
            //
            public static readonly string NameNoCase = Name.ToLower();

            ///////////////////////////////////////////////////////////////////

            //
            // NOTE: The typical prefix used for reserved variable names.
            //
            public static readonly string Prefix = NameNoCase + "_";

            ///////////////////////////////////////////////////////////////////

            #region Version & Patch-Level "Constants"
            //
            // NOTE: This is the version of Tcl we are "emulating".
            //
            public static readonly string VersionValue = "8.4";
            public static readonly string VersionName = Prefix + "version";

            ///////////////////////////////////////////////////////////////////

            //
            // NOTE: This is the patch level of Tcl we are "emulating".  This
            //       is the last officially released patch level of Tcl 8.4.x,
            //       plus one.
            //
            public static readonly string PatchLevelValue = "8.4.21";
            public static readonly string PatchLevelName = Prefix + "patchLevel";

            ///////////////////////////////////////////////////////////////////

            //
            // NOTE: This value is only used when providing the "Tcl" package
            //       to the interpreter.
            //
            public static readonly Version Version = new Version(
                PatchLevelValue);
            #endregion
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Tcl Library Variables
        [ObjectId("e83cd6e7-ca40-4ec1-a920-3d9936e21f00")]
        internal static class Core
        {
            #region For Package Management Use Only
            public static readonly string AutoIndex = "auto_index";
            public static readonly string AutoNoLoad = "auto_noload";
            public static readonly string AutoOldPath = "auto_oldpath";
            public static readonly string AutoPath = "auto_path";
            public static readonly string AutoSourcePath = "auto_source_path";

            ///////////////////////////////////////////////////////////////////

            //
            // NOTE: This is a transient (temporary) global variable only
            //       for use when evaluating package index files.  Do NOT
            //       use it for any other purpose.  It will contain the
            //       fully qualified name of the directory containing the
            //       package index file being evaluated.
            //
            public static readonly string Directory = "dir";

            ///////////////////////////////////////////////////////////////////

            //
            // NOTE: These two variables are used by Tcl only; however,
            //       they are not intended to be used directly by (most?)
            //       scripts.
            //
            public static readonly string LibraryPath =
                Package.Prefix + "libPath";

            public static readonly string PackagePath =
                Package.Prefix + "pkgPath";
            #endregion

            ///////////////////////////////////////////////////////////////////

            #region For Script Library Use Only
            //
            // NOTE: This variable contains the location of the core script
            //       library.  This is used by Tcl and Eagle.
            //
            public static readonly string Library =
                Package.Prefix + "library";

            ///////////////////////////////////////////////////////////////////

            //
            // NOTE: This variable contains the location of the shell script
            //       library.  This is only used by Eagle.
            //
            public static readonly string ShellLibrary =
                Package.Prefix + "shellLibrary";
            #endregion

            ///////////////////////////////////////////////////////////////////

            #region For Shell Support Use Only
            //
            // NOTE: These are used by the native Tcl auto-execution
            //       mechanism.  They are not used by Eagle.
            //
            public static readonly string AutoExecutables = "auto_execs";
            public static readonly string AutoNoExecute = "auto_noexec";

            ///////////////////////////////////////////////////////////////////

            public static readonly string Interactive =
                Package.Prefix + "interactive";

            ///////////////////////////////////////////////////////////////////

            //
            // NOTE: These are for the normal interactive prompt (without
            //       debug, without queue).
            //
            public static readonly string Prompt1 =
                Package.Prefix + "prompt1";

            public static readonly string Prompt2 =
                Package.Prefix + "prompt2";

            //
            // NOTE: These are for the debug interactive prompt (without
            //       queue).  These do not exist in Tcl (Eagle only).
            //
            public static readonly string Prompt3 =
                Package.Prefix + "prompt3";

            public static readonly string Prompt4 =
                Package.Prefix + "prompt4";

            //
            // NOTE: These are for the queue interactive prompt (without
            //       debug).  These do not exist in Tcl (Eagle only).
            //
            public static readonly string Prompt5 =
                Package.Prefix + "prompt5";

            public static readonly string Prompt6 =
                Package.Prefix + "prompt6";

            //
            // NOTE: These are for the debug, queue interactive prompt.
            //       These do not exist in Tcl (Eagle only).
            //
            public static readonly string Prompt7 =
                Package.Prefix + "prompt7";

            public static readonly string Prompt8 =
                Package.Prefix + "prompt8";

            ///////////////////////////////////////////////////////////////////

            //
            // NOTE: This does not exist in Tcl (Eagle only).
            //
            public static readonly string InteractiveLoops =
                Package.Prefix + "interactiveLoops";

            ///////////////////////////////////////////////////////////////////

            //
            // NOTE: These are supported (and used) by the Tcl and Eagle
            //       shells.  The script file, if it exists, will be
            //       evaluated after the interpreter has been fully
            //       initialized and the interactive loop is about to be
            //       entered.
            //
            public static readonly string RunCommandsFileName =
                Package.Prefix + "rcFileName";

            ///////////////////////////////////////////////////////////////////

            //
            // NOTE: This is used by native Tcl on Mac OS Classic only.
            //
            public static readonly string RunCommandsResourceName =
                Package.Prefix + "rcRsrcName";
            #endregion

            ///////////////////////////////////////////////////////////////////

            #region For Script Error Handling Use Only
            public static readonly string ErrorCode = "errorCode";
            public static readonly string ErrorInfo = "errorInfo";
            #endregion

            ///////////////////////////////////////////////////////////////////

            #region For Shell Argument Handling Use Only
            public static readonly string ShellArgumentCount = "argc";
            public static readonly string ShellArguments = "argv";
            public static readonly string ShellArgument0 = "argv0";
            #endregion

            ///////////////////////////////////////////////////////////////////

            #region For "word.tcl" Use Only
            public static readonly string NonWordCharacters =
                Package.Prefix + "nonwordchars";

            public static readonly string WordCharacters =
                Package.Prefix + "wordchars";
            #endregion

            ///////////////////////////////////////////////////////////////////

            #region For [proc] Use Only
            //
            // NOTE: This is used by (and with) the [proc] command to indicate
            //       a procedure that accepts a variable number of arguments
            //       (i.e. it may only be used as the last argument).
            //
            public static readonly string Arguments = "args";
            #endregion

            ///////////////////////////////////////////////////////////////////

            #region For "env" Array Use Only
            //
            // NOTE: This variable can be used to access the environment
            //       variables applicable to the current process.  Do NOT
            //       use it for any other purpose.
            //
            public static readonly string Environment = "env";
            #endregion

            ///////////////////////////////////////////////////////////////////

            #region For Tcl Byte-Code Compiler Use Only
            //
            // NOTE: These are used by (special debugging builds of) Tcl
            //       only in order to emit extra information pertaining to
            //       the byte-code compilation and execution of commands.
            //
            public static readonly string TraceCompile =
                Package.Prefix + "traceCompile";

            public static readonly string TraceExecute =
                Package.Prefix + "traceExec";
            #endregion

            ///////////////////////////////////////////////////////////////////

            //
            // NOTE: This variable is used by Tcl and Eagle to set the
            //       precision to be used for double result values in
            //       expressions.
            //
            public static readonly string PrecisionName =
                Package.Prefix + "precision";
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region For Expression Processing Use Only
        [ObjectId("c473d05a-27e6-4437-82c3-facc333373ed")]
        internal static class Expression
        {
            //
            // NOTE: The default precision value for doubles.
            //
            public static readonly int DefaultPrecision = 0;

            ///////////////////////////////////////////////////////////////////

            //
            // NOTE: These strings are recognized as doubles by the Tcl
            //       expression parser.
            //
            public static readonly string Infinity = "Inf";
            public static readonly string NaN = "NaN";
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region For Script File & Path Handling Only
        [ObjectId("ca3f1768-f9cf-40e1-8fe4-b684d0426f76")]
        internal static class Path
        {
            //
            // NOTE: This is the default file name value to be used for
            //       the "tcl_rcFileName" variable, which is allowed to
            //       contain a script to evaluate upon entry into the
            //       interactive shell.
            //
            public static readonly string RunCommands = "~/tclshrc.tcl";

            ///////////////////////////////////////////////////////////////////

            #region For Package Management Use Only
            public static readonly string Bin = "bin";
            public static readonly string Lib = "lib";

            ///////////////////////////////////////////////////////////////////

#if UNIX
            public static readonly string LibData = "libdata";

            public static readonly string UserLocal = "/usr/local";
            public static readonly string UserLib = "/usr/" + Lib;
            public static readonly string LinuxGnuSuffix = "-linux-gnu";

            public static readonly string UserLocalLib =
                UserLocal + "/" + Lib;

            public static readonly string UserLocalLibData =
                UserLocal + "/" + LibData;
#endif
            #endregion
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region For Script Command Handling Only
        [ObjectId("bd0d257d-9541-41f9-875a-31594114ab58")]
        internal static class Command
        {
            //
            // NOTE: This is the name of the (global) command that is used
            //       to handle (possibly finding) unknown commands.  It is
            //       somewhat special because it is used by the engine and
            //       it is always treated as "user-defined", even when it
            //       is defined (i.e. as a stub) by the core library.
            //
            public static readonly string Unknown =
                Namespace.Global + "unknown";

            ///////////////////////////////////////////////////////////////////

            #region For Package Management Use Only
            //
            // NOTE: This is the name of the (global) command that is used
            //       to handle (possibly finding) unknown packages.
            //
            public static readonly string PackageUnknown =
                Namespace.Global + "tcl::tm::UnknownHandler " +
                Namespace.Global + "tclPkgUnknown";
            #endregion

            ///////////////////////////////////////////////////////////////////

            #region For Script Error Handling Use Only
            public static readonly string BackgroundError = "bgerror";
            #endregion
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region For Namespace Use Only
        [ObjectId("237a1f93-61b8-423e-98a3-75bfaeb3629e")]
        internal static class Namespace
        {
            public static readonly string Separator = "::";
            public static readonly string Global = Separator;
            public static readonly string GlobalName = String.Empty;

            ///////////////////////////////////////////////////////////////////

            //
            // HACK: This is part of an ugly hack to add "tcl::mathfunc::*"
            //       and "tcl::mathop::*" support for [expr] functions and
            //       operators to Eagle, respectively.
            //
            public static readonly string MathFunctionName = "tcl::mathfunc";
            public static readonly string MathOperatorName = "tcl::mathop";
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region For "tcl_platform" Array Use Only
        //
        // NOTE: These names are referred to directly from scripts and where
        //       applicable are named identically to their Tcl counterparts,
        //       please do not change.
        //
        [ObjectId("17c544ad-f0f2-415c-af5c-ec7cb6bd88f0")]
        internal static class Platform
        {
            //
            // NOTE: The name of the script array that contains the
            //       Tcl compatible platform specific information.
            //
            public static readonly string Name = Package.Prefix + "platform";

            ///////////////////////////////////////////////////////////////////

            //
            // NOTE: The various "well known" elements of the array.
            //
            public static readonly string ByteOrder = "byteOrder";
            public static readonly string CharacterSize = "characterSize"; // NOTE: In format ("min-max"). Not in Tcl.

#if DEBUG
            public static readonly string Debug = "debug"; // NOTE: ActiveTcl only?
#endif

            public static readonly string Version = "version";
            public static readonly string PatchLevel = "patchLevel";
            public static readonly string Engine = "engine"; // COMPAT: What engine are we really using? Not in Tcl.
            public static readonly string Host = "host"; // NOTE: Not in Tcl.
            public static readonly string Machine = "machine";
            public static readonly string OsName = "os";
            public static readonly string OsString = "osString"; // NOTE: Not in Tcl.
            public static readonly string OsVersion = "osVersion";
            public static readonly string OsPatchLevel = "osPatchLevel"; // NOTE: Not in Tcl.
            public static readonly string OsProductType = "osProductType"; // NOTE: Not in Tcl.
            public static readonly string OsReleaseId = "osReleaseId"; // NOTE: Not in Tcl.
            public static readonly string OsServicePack = "osServicePack"; // NOTE: Not in Tcl.
            public static readonly string OsExtra = "osExtra"; // NOTE: Not in Tcl.
            public static readonly string ProcessBits = "processBits"; // NOTE: Not in Tcl (32-bit or 64-bit, etc).
            public static readonly string PlatformName = "platform"; // COMPAT: Tcl.
            public static readonly string PointerSize = "pointerSize";
            public static readonly string Processors = "processors"; // NOTE: Not in Tcl.
            public static readonly string Threaded = "threaded"; // TODO: Double-check this for consistency.
            public static readonly string Unicode = "unicode"; // NOTE: Not in Tcl.
            public static readonly string User = "user";
            public static readonly string WordSize = "wordSize";
            public static readonly string DirectorySeparator = "dirSeparator"; // NOTE: Not in Tcl.
            public static readonly string PathSeparator = "pathSeparator"; // NOTE: Not in Tcl, proposed by TIP #315.

            ///////////////////////////////////////////////////////////////////

            //
            // NOTE: The various "well known" values...
            //
            public static readonly string LittleEndianValue = "littleEndian"; // byteOrder
            public static readonly string BigEndianValue = "bigEndian";       // byteOrder

            ///////////////////////////////////////////////////////////////////

#if NATIVE && UNIX
            public static readonly string UnixValue = "unix";
#endif
        }
        #endregion
    }
}
