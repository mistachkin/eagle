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
    /// <summary>
    /// This class is a container for the well-known names, prefixes, and values
    /// associated with the Tcl compatibility layer of Eagle, grouped into nested
    /// classes by purpose (e.g. package naming, core library variables,
    /// expression processing, path handling, command handling, namespaces, and
    /// the "tcl_platform" array).  Many of these names are referred to directly
    /// from scripts and, where applicable, are named identically to their Tcl
    /// counterparts.
    /// </summary>
    [ObjectId("f48e798e-7e3a-4a21-b27b-3d20d30c91bf")]
    internal static class TclVars
    {
        #region Tcl Package Naming
        /// <summary>
        /// This class is a container for the names, prefixes, and version values
        /// associated with the "Tcl" package that Eagle emulates.
        /// </summary>
        [ObjectId("9b266cce-c72d-4b6a-88c9-7f4bb7e26470")]
        internal static class Package
        {
            //
            // NOTE: The name used by the native Tcl core "package".
            //
            /// <summary>
            /// The name used by the native Tcl core "package".
            /// </summary>
            public static readonly string Name = "Tcl";

            ///////////////////////////////////////////////////////////////////

            //
            // NOTE: Used to set the variables in the platform array(s),
            //       etc.
            //
            /// <summary>
            /// The lower-case form of the package name, used to set the
            /// variables in the platform array(s), etc.
            /// </summary>
            public static readonly string NameNoCase = Name.ToLower();

            ///////////////////////////////////////////////////////////////////

            //
            // NOTE: The typical prefix used for reserved variable names.
            //
            /// <summary>
            /// The typical prefix used for reserved variable names.
            /// </summary>
            public static readonly string Prefix = NameNoCase + "_";

            ///////////////////////////////////////////////////////////////////

            #region Version & Patch-Level "Constants"
            //
            // NOTE: This is the version of Tcl we are "emulating".
            //
            /// <summary>
            /// The version of Tcl that Eagle is "emulating".
            /// </summary>
            public static readonly string VersionValue = "8.4";
            /// <summary>
            /// The name of the variable that holds the emulated Tcl version.
            /// </summary>
            public static readonly string VersionName = Prefix + "version";

            ///////////////////////////////////////////////////////////////////

            //
            // NOTE: This is the patch level of Tcl we are "emulating".  This
            //       is the last officially released patch level of Tcl 8.4.x,
            //       plus one.
            //
            /// <summary>
            /// The patch level of Tcl that Eagle is "emulating".  This is the
            /// last officially released patch level of Tcl 8.4.x, plus one.
            /// </summary>
            public static readonly string PatchLevelValue = "8.4.21";
            /// <summary>
            /// The name of the variable that holds the emulated Tcl patch
            /// level.
            /// </summary>
            public static readonly string PatchLevelName = Prefix + "patchLevel";

            ///////////////////////////////////////////////////////////////////

            //
            // NOTE: This value is only used when providing the "Tcl" package
            //       to the interpreter.
            //
            /// <summary>
            /// The parsed version object, built from the emulated Tcl patch
            /// level, used only when providing the "Tcl" package to the
            /// interpreter.
            /// </summary>
            public static readonly Version Version = new Version(
                PatchLevelValue);
            #endregion
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Tcl Library Variables
        /// <summary>
        /// This class is a container for the names of the Tcl core library
        /// variables used for package management, the script library, shell
        /// support, error handling, argument handling, and other purposes.
        /// </summary>
        [ObjectId("e83cd6e7-ca40-4ec1-a920-3d9936e21f00")]
        internal static class Core
        {
            #region For Package Management Use Only
            /// <summary>
            /// The name of the array that maps command and package names to the
            /// scripts used to auto-load them.
            /// </summary>
            public static readonly string AutoIndex = "auto_index";
            /// <summary>
            /// The name of the variable that, when set, disables package
            /// auto-loading.
            /// </summary>
            public static readonly string AutoNoLoad = "auto_noload";
            /// <summary>
            /// The name of the variable that holds the previous value of the
            /// auto-load path.
            /// </summary>
            public static readonly string AutoOldPath = "auto_oldpath";
            /// <summary>
            /// The name of the variable that holds the list of directories
            /// searched when auto-loading packages.
            /// </summary>
            public static readonly string AutoPath = "auto_path";
            /// <summary>
            /// The name of the variable that holds the list of directories
            /// searched when auto-sourcing scripts.
            /// </summary>
            public static readonly string AutoSourcePath = "auto_source_path";

            ///////////////////////////////////////////////////////////////////

            //
            // NOTE: These are transient (temporary) global variables only
            //       for use when evaluating package index files.  Do NOT
            //       use them for any other purpose.  The "dir" variable
            //       will contain the fully qualified name of the directory
            //       containing the package index file being evaluated and
            //       the "tag" variable will contain a 16 digit hexadecimal
            //       tag value, which generally represents the associated
            //       public key token, if any.
            //
            /// <summary>
            /// The name of the transient global variable that, while a package
            /// index file is being evaluated, contains the fully qualified name
            /// of the directory containing that file.  Do NOT use it for any
            /// other purpose.
            /// </summary>
            public static readonly string Directory = "dir";
            /// <summary>
            /// The name of the transient global variable that, while a package
            /// index file is being evaluated, contains a 16 digit hexadecimal
            /// tag value, which generally represents the associated public key
            /// token, if any.  Do NOT use it for any other purpose.
            /// </summary>
            public static readonly string Tag = "tag";

            ///////////////////////////////////////////////////////////////////

            //
            // NOTE: These two variables are used by Tcl only; however,
            //       they are not intended to be used directly by (most?)
            //       scripts.
            //
            /// <summary>
            /// The name of the variable, used by Tcl only, that holds the
            /// library search path.  It is not intended to be used directly by
            /// (most?) scripts.
            /// </summary>
            public static readonly string LibraryPath =
                Package.Prefix + "libPath";

            /// <summary>
            /// The name of the variable, used by Tcl only, that holds the
            /// package search path.  It is not intended to be used directly by
            /// (most?) scripts.
            /// </summary>
            public static readonly string PackagePath =
                Package.Prefix + "pkgPath";
            #endregion

            ///////////////////////////////////////////////////////////////////

            #region For Script Library Use Only
            //
            // NOTE: This variable contains the location of the core script
            //       library.  This is used by Tcl and Eagle.
            //
            /// <summary>
            /// The name of the variable that contains the location of the core
            /// script library.  This is used by Tcl and Eagle.
            /// </summary>
            public static readonly string Library =
                Package.Prefix + "library";

            ///////////////////////////////////////////////////////////////////

            //
            // NOTE: This variable contains the location of the shell script
            //       library.  This is only used by Eagle.
            //
            /// <summary>
            /// The name of the variable that contains the location of the shell
            /// script library.  This is only used by Eagle.
            /// </summary>
            public static readonly string ShellLibrary =
                Package.Prefix + "shellLibrary";
            #endregion

            ///////////////////////////////////////////////////////////////////

            #region For Shell Support Use Only
            //
            // NOTE: These are used by the native Tcl auto-execution
            //       mechanism.  They are not used by Eagle.
            //
            /// <summary>
            /// The name of the array that caches the resolved locations of
            /// auto-executed external programs.  This is used by the native Tcl
            /// auto-execution mechanism and is not used by Eagle.
            /// </summary>
            public static readonly string AutoExecutables = "auto_execs";
            /// <summary>
            /// The name of the variable that, when set, disables the
            /// auto-execution of external programs.  This is used by the native
            /// Tcl auto-execution mechanism and is not used by Eagle.
            /// </summary>
            public static readonly string AutoNoExecute = "auto_noexec";

            ///////////////////////////////////////////////////////////////////

            /// <summary>
            /// The name of the variable that, when non-zero, indicates the
            /// interpreter is running interactively.
            /// </summary>
            public static readonly string Interactive =
                Package.Prefix + "interactive";

            ///////////////////////////////////////////////////////////////////

            //
            // NOTE: These are for the normal interactive prompt (without
            //       debug, without queue).
            //
            /// <summary>
            /// The name of the variable that holds the primary prompt script
            /// for the normal interactive prompt (without debug, without
            /// queue).
            /// </summary>
            public static readonly string Prompt1 =
                Package.Prefix + "prompt1";

            /// <summary>
            /// The name of the variable that holds the secondary (continuation)
            /// prompt script for the normal interactive prompt (without debug,
            /// without queue).
            /// </summary>
            public static readonly string Prompt2 =
                Package.Prefix + "prompt2";

            //
            // NOTE: These are for the debug interactive prompt (without
            //       queue).  These do not exist in Tcl (Eagle only).
            //
            /// <summary>
            /// The name of the variable that holds the primary prompt script
            /// for the debug interactive prompt (without queue).  This does not
            /// exist in Tcl (Eagle only).
            /// </summary>
            public static readonly string Prompt3 =
                Package.Prefix + "prompt3";

            /// <summary>
            /// The name of the variable that holds the secondary (continuation)
            /// prompt script for the debug interactive prompt (without queue).
            /// This does not exist in Tcl (Eagle only).
            /// </summary>
            public static readonly string Prompt4 =
                Package.Prefix + "prompt4";

            //
            // NOTE: These are for the queue interactive prompt (without
            //       debug).  These do not exist in Tcl (Eagle only).
            //
            /// <summary>
            /// The name of the variable that holds the primary prompt script
            /// for the queue interactive prompt (without debug).  This does not
            /// exist in Tcl (Eagle only).
            /// </summary>
            public static readonly string Prompt5 =
                Package.Prefix + "prompt5";

            /// <summary>
            /// The name of the variable that holds the secondary (continuation)
            /// prompt script for the queue interactive prompt (without debug).
            /// This does not exist in Tcl (Eagle only).
            /// </summary>
            public static readonly string Prompt6 =
                Package.Prefix + "prompt6";

            //
            // NOTE: These are for the debug, queue interactive prompt.
            //       These do not exist in Tcl (Eagle only).
            //
            /// <summary>
            /// The name of the variable that holds the primary prompt script
            /// for the debug, queue interactive prompt.  This does not exist in
            /// Tcl (Eagle only).
            /// </summary>
            public static readonly string Prompt7 =
                Package.Prefix + "prompt7";

            /// <summary>
            /// The name of the variable that holds the secondary (continuation)
            /// prompt script for the debug, queue interactive prompt.  This
            /// does not exist in Tcl (Eagle only).
            /// </summary>
            public static readonly string Prompt8 =
                Package.Prefix + "prompt8";

            ///////////////////////////////////////////////////////////////////

            //
            // NOTE: This does not exist in Tcl (Eagle only).
            //
            /// <summary>
            /// The name of the variable that holds the count of active nested
            /// interactive loops.  This does not exist in Tcl (Eagle only).
            /// </summary>
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
            /// <summary>
            /// The name of the variable that holds the name of the run-commands
            /// (startup) script file.  This is supported (and used) by the Tcl
            /// and Eagle shells; the file, if it exists, is evaluated after the
            /// interpreter has been fully initialized and the interactive loop
            /// is about to be entered.
            /// </summary>
            public static readonly string RunCommandsFileName =
                Package.Prefix + "rcFileName";

            ///////////////////////////////////////////////////////////////////

            //
            // NOTE: This is used by native Tcl on Mac OS Classic only.
            //
            /// <summary>
            /// The name of the variable that holds the name of the
            /// run-commands resource.  This is used by native Tcl on Mac OS
            /// Classic only.
            /// </summary>
            public static readonly string RunCommandsResourceName =
                Package.Prefix + "rcRsrcName";
            #endregion

            ///////////////////////////////////////////////////////////////////

            #region For Script Error Handling Use Only
            /// <summary>
            /// The name of the variable that holds the machine-readable error
            /// code for the most recent error.
            /// </summary>
            public static readonly string ErrorCode = "errorCode";
            /// <summary>
            /// The name of the variable that holds the human-readable stack
            /// trace information for the most recent error.
            /// </summary>
            public static readonly string ErrorInfo = "errorInfo";

            ///////////////////////////////////////////////////////////////////

            /// <summary>
            /// The error code element name used to report the exit status of a
            /// child process.
            /// </summary>
            public static readonly string ChildStatus = "CHILDSTATUS";
            #endregion

            ///////////////////////////////////////////////////////////////////

            #region For Shell Argument Handling Use Only
            /// <summary>
            /// The name of the variable that holds the count of command line
            /// arguments passed to the shell.
            /// </summary>
            public static readonly string ShellArgumentCount = "argc";
            /// <summary>
            /// The name of the variable that holds the list of command line
            /// arguments passed to the shell.
            /// </summary>
            public static readonly string ShellArguments = "argv";
            /// <summary>
            /// The name of the variable that holds the name of the script being
            /// executed by the shell.
            /// </summary>
            public static readonly string ShellArgument0 = "argv0";
            #endregion

            ///////////////////////////////////////////////////////////////////

            #region For "word.tcl" Use Only
            /// <summary>
            /// The name of the variable that holds the regular expression
            /// matching non-word characters, used by "word.tcl".
            /// </summary>
            public static readonly string NonWordCharacters =
                Package.Prefix + "nonwordchars";

            /// <summary>
            /// The name of the variable that holds the regular expression
            /// matching word characters, used by "word.tcl".
            /// </summary>
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
            /// <summary>
            /// The special argument name used by (and with) the [proc] command
            /// to indicate a procedure that accepts a variable number of
            /// arguments (i.e. it may only be used as the last argument).
            /// </summary>
            public static readonly string Arguments = "args";
            #endregion

            ///////////////////////////////////////////////////////////////////

            #region For "env" Array Use Only
            //
            // NOTE: This variable can be used to access the environment
            //       variables applicable to the current process.  Do NOT
            //       use it for any other purpose.
            //
            /// <summary>
            /// The name of the array used to access the environment variables
            /// applicable to the current process.  Do NOT use it for any other
            /// purpose.
            /// </summary>
            public static readonly string Environment = "env";
            #endregion

            ///////////////////////////////////////////////////////////////////

            #region For Tcl Byte-Code Compiler Use Only
            //
            // NOTE: These are used by (special debugging builds of) Tcl
            //       only in order to emit extra information pertaining to
            //       the byte-code compilation and execution of commands.
            //
            /// <summary>
            /// The name of the variable that controls emitting extra
            /// information pertaining to byte-code compilation.  This is used by
            /// (special debugging builds of) Tcl only.
            /// </summary>
            public static readonly string TraceCompile =
                Package.Prefix + "traceCompile";

            /// <summary>
            /// The name of the variable that controls emitting extra
            /// information pertaining to byte-code execution.  This is used by
            /// (special debugging builds of) Tcl only.
            /// </summary>
            public static readonly string TraceExecute =
                Package.Prefix + "traceExec";
            #endregion

            ///////////////////////////////////////////////////////////////////

            //
            // NOTE: This variable is used by Tcl and Eagle to set the
            //       precision to be used for double result values in
            //       expressions.
            //
            /// <summary>
            /// The name of the variable used by Tcl and Eagle to set the
            /// precision to be used for double result values in expressions.
            /// </summary>
            public static readonly string PrecisionName =
                Package.Prefix + "precision";
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region For Expression Processing Use Only
        /// <summary>
        /// This class is a container for the constants used when processing Tcl
        /// expressions, such as the default precision and the strings
        /// recognized as special double values.
        /// </summary>
        [ObjectId("c473d05a-27e6-4437-82c3-facc333373ed")]
        internal static class Expression
        {
            //
            // NOTE: The default precision value for doubles.
            //
            /// <summary>
            /// The default precision value for doubles.
            /// </summary>
            public static readonly int DefaultPrecision = 0;

            ///////////////////////////////////////////////////////////////////

            //
            // NOTE: These strings are recognized as doubles by the Tcl
            //       expression parser.
            //
            /// <summary>
            /// The string recognized as positive infinity by the Tcl
            /// expression parser.
            /// </summary>
            public static readonly string Infinity = "Inf";
            /// <summary>
            /// The string recognized as "not a number" by the Tcl expression
            /// parser.
            /// </summary>
            public static readonly string NaN = "NaN";
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region For Script File & Path Handling Only
        /// <summary>
        /// This class is a container for the default file names and directory
        /// path fragments used for script file and path handling, including the
        /// directories searched for packages on Unix-like systems.
        /// </summary>
        [ObjectId("ca3f1768-f9cf-40e1-8fe4-b684d0426f76")]
        internal static class Path
        {
            //
            // NOTE: This is the default file name value to be used for
            //       the "tcl_rcFileName" variable, which is allowed to
            //       contain a script to evaluate upon entry into the
            //       interactive shell.
            //
            /// <summary>
            /// The default file name value to be used for the "tcl_rcFileName"
            /// variable, which is allowed to contain a script to evaluate upon
            /// entry into the interactive shell.
            /// </summary>
            public static readonly string RunCommands = "~/tclshrc.tcl";

            ///////////////////////////////////////////////////////////////////

            #region For Package Management Use Only
            /// <summary>
            /// The conventional "bin" directory name fragment.
            /// </summary>
            public static readonly string Bin = "bin";
            /// <summary>
            /// The conventional "lib" directory name fragment.
            /// </summary>
            public static readonly string Lib = "lib";

            ///////////////////////////////////////////////////////////////////

#if UNIX
            /// <summary>
            /// The conventional "libdata" directory name fragment.
            /// </summary>
            public static readonly string LibData = "libdata";

            /// <summary>
            /// The conventional "/usr/local" base directory.
            /// </summary>
            public static readonly string UserLocal = "/usr/local";

            /// <summary>
            /// The conventional "/usr/lib" library directory.
            /// </summary>
            public static readonly string UserLib = "/usr/" + Lib;

            /// <summary>
            /// The directory name suffix used by Linux GNU multi-arch library
            /// directories.
            /// </summary>
            public static readonly string LinuxGnuSuffix = "-linux-gnu";

            /// <summary>
            /// The conventional "/usr/local/lib" library directory.
            /// </summary>
            public static readonly string UserLocalLib =
                UserLocal + "/" + Lib;

            /// <summary>
            /// The conventional "/usr/local/libdata" library data directory.
            /// </summary>
            public static readonly string UserLocalLibData =
                UserLocal + "/" + LibData;

            ///////////////////////////////////////////////////////////////////

            //
            // TODO: Are any other Homebrew paths needed here, e.g.
            //       "/opt/homebrew/Cellar/tcl-tk@8/<version>/lib/",
            //       etc?
            //
            /* NOTE: macOS only. */
            /// <summary>
            /// The format string used to build the optional Homebrew Tcl/Tk
            /// library directory, where the format placeholder is the major
            /// version.  macOS only.
            /// </summary>
            public static readonly string OptionalHomebrewLibFormat =
                "/opt/homebrew/opt/tcl-tk@{0}/lib";
#endif
            #endregion
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region For Script Command Handling Only
        /// <summary>
        /// This class is a container for the names of the (global) commands used
        /// to handle unknown commands, unknown packages, and background errors.
        /// </summary>
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
            /// <summary>
            /// The name of the (global) command used to handle (possibly
            /// finding) unknown commands.  It is somewhat special because it is
            /// used by the engine and it is always treated as "user-defined",
            /// even when it is defined (i.e. as a stub) by the core library.
            /// </summary>
            public static readonly string Unknown =
                Namespace.Global + "unknown";

            ///////////////////////////////////////////////////////////////////

            #region For Package Management Use Only
            //
            // NOTE: This is the name of the (global) command that is used
            //       to handle (possibly finding) unknown packages.
            //
            /// <summary>
            /// The name of the (global) command used to handle (possibly
            /// finding) unknown packages.
            /// </summary>
            public static readonly string PackageUnknown =
                Namespace.Global + "tcl::tm::UnknownHandler " +
                Namespace.Global + "tclPkgUnknown";
            #endregion

            ///////////////////////////////////////////////////////////////////

            #region For Script Error Handling Use Only
            /// <summary>
            /// The name of the (global) command used to handle errors that
            /// occur in the background (i.e. outside of any active script).
            /// </summary>
            public static readonly string BackgroundError = "bgerror";
            #endregion
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region For Namespace Use Only
        /// <summary>
        /// This class is a container for the names and separators associated
        /// with Tcl namespaces, including the special namespaces used to support
        /// [expr] math functions and operators.
        /// </summary>
        [ObjectId("237a1f93-61b8-423e-98a3-75bfaeb3629e")]
        internal static class Namespace
        {
            /// <summary>
            /// The string used to separate the components of a namespace
            /// qualified name.
            /// </summary>
            public static readonly string Separator = "::";
            /// <summary>
            /// The qualified name prefix that refers to the global namespace.
            /// </summary>
            public static readonly string Global = Separator;
            /// <summary>
            /// The (empty) simple name of the global namespace.
            /// </summary>
            public static readonly string GlobalName = String.Empty;

            ///////////////////////////////////////////////////////////////////

            //
            // HACK: This is part of an ugly hack to add "tcl::mathfunc::*"
            //       and "tcl::mathop::*" support for [expr] functions and
            //       operators to Eagle, respectively.
            //
            /// <summary>
            /// The name of the namespace that contains the [expr] math
            /// functions.  This is part of an ugly hack to add
            /// "tcl::mathfunc::*" support to Eagle.
            /// </summary>
            public static readonly string MathFunctionName = "tcl::mathfunc";
            /// <summary>
            /// The name of the namespace that contains the [expr] math
            /// operators.  This is part of an ugly hack to add "tcl::mathop::*"
            /// support to Eagle.
            /// </summary>
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
        /// <summary>
        /// This class is a container for the name of the "tcl_platform" script
        /// array and the names and values of its "well known" elements, which
        /// expose platform specific information to scripts.  These names are
        /// referred to directly from scripts and where applicable are named
        /// identically to their Tcl counterparts.
        /// </summary>
        [ObjectId("17c544ad-f0f2-415c-af5c-ec7cb6bd88f0")]
        internal static class Platform
        {
            //
            // NOTE: The name of the script array that contains the
            //       Tcl compatible platform specific information.
            //
            /// <summary>
            /// The name of the script array that contains the Tcl compatible
            /// platform specific information.
            /// </summary>
            public static readonly string Name = Package.Prefix + "platform";

            ///////////////////////////////////////////////////////////////////

            //
            // NOTE: The various "well known" elements of the array.
            //
            /// <summary>
            /// The element name that holds the native byte order of the
            /// platform.
            /// </summary>
            public static readonly string ByteOrder = "byteOrder";
            /// <summary>
            /// The element name that holds the size, in bytes, of a character,
            /// in format ("min-max").  Not in Tcl.
            /// </summary>
            public static readonly string CharacterSize = "characterSize"; // NOTE: In format ("min-max"). Not in Tcl.

#if DEBUG
            /// <summary>
            /// The element name that, when present, indicates a debug build.
            /// ActiveTcl only?
            /// </summary>
            public static readonly string Debug = "debug"; // NOTE: ActiveTcl only?
#endif

            /// <summary>
            /// The element name that holds the emulated Tcl version.
            /// </summary>
            public static readonly string Version = "version";
            /// <summary>
            /// The element name that holds the emulated Tcl patch level.
            /// </summary>
            public static readonly string PatchLevel = "patchLevel";
            /// <summary>
            /// The element name that holds the name of the script engine
            /// actually being used.  Not in Tcl.
            /// </summary>
            public static readonly string Engine = "engine"; // COMPAT: What engine are we really using? Not in Tcl.
            /// <summary>
            /// The element name that holds the host name.  Not in Tcl.
            /// </summary>
            public static readonly string Host = "host"; // NOTE: Not in Tcl.
            /// <summary>
            /// The element name that holds the machine (processor architecture)
            /// name.
            /// </summary>
            public static readonly string Machine = "machine";
            /// <summary>
            /// The element name that holds the operating system name.
            /// </summary>
            public static readonly string OsName = "os";
            /// <summary>
            /// The element name that holds the full operating system
            /// description string.  Not in Tcl.
            /// </summary>
            public static readonly string OsString = "osString"; // NOTE: Not in Tcl.
            /// <summary>
            /// The element name that holds the operating system version.
            /// </summary>
            public static readonly string OsVersion = "osVersion";
            /// <summary>
            /// The element name that holds the operating system patch level.
            /// Not in Tcl.
            /// </summary>
            public static readonly string OsPatchLevel = "osPatchLevel"; // NOTE: Not in Tcl.
            /// <summary>
            /// The element name that holds the operating system product type.
            /// Not in Tcl.
            /// </summary>
            public static readonly string OsProductType = "osProductType"; // NOTE: Not in Tcl.
            /// <summary>
            /// The element name that holds the operating system release
            /// identifier.  Not in Tcl.
            /// </summary>
            public static readonly string OsReleaseId = "osReleaseId"; // NOTE: Not in Tcl.
            /// <summary>
            /// The element name that holds the operating system service pack.
            /// Not in Tcl.
            /// </summary>
            public static readonly string OsServicePack = "osServicePack"; // NOTE: Not in Tcl.
            /// <summary>
            /// The element name that holds extra operating system information.
            /// Not in Tcl.
            /// </summary>
            public static readonly string OsExtra = "osExtra"; // NOTE: Not in Tcl.
            /// <summary>
            /// The element name that holds the process bitness (32-bit or
            /// 64-bit, etc).  Not in Tcl.
            /// </summary>
            public static readonly string ProcessBits = "processBits"; // NOTE: Not in Tcl (32-bit or 64-bit, etc).
            /// <summary>
            /// The element name that holds the platform name.  Tcl compatible.
            /// </summary>
            public static readonly string PlatformName = "platform"; // COMPAT: Tcl.
            /// <summary>
            /// The element name that holds the size, in bytes, of a pointer.
            /// </summary>
            public static readonly string PointerSize = "pointerSize";
            /// <summary>
            /// The element name that holds the number of processors.  Not in
            /// Tcl.
            /// </summary>
            public static readonly string Processors = "processors"; // NOTE: Not in Tcl.
            /// <summary>
            /// The element name that, when present, indicates a threaded build.
            /// </summary>
            public static readonly string Threaded = "threaded"; // TODO: Double-check this for consistency.
            /// <summary>
            /// The element name that holds the maximum Unicode value supported.
            /// Not in Tcl.
            /// </summary>
            public static readonly string Unicode = "unicode"; // NOTE: Not in Tcl.
            /// <summary>
            /// The element name that holds the current user name.
            /// </summary>
            public static readonly string User = "user";
            /// <summary>
            /// The element name that holds the size, in bytes, of a machine
            /// word.
            /// </summary>
            public static readonly string WordSize = "wordSize";
            /// <summary>
            /// The element name that holds the primary directory separator
            /// character.  Not in Tcl.
            /// </summary>
            public static readonly string DirectorySeparator = "dirSeparator"; // NOTE: Not in Tcl.
            /// <summary>
            /// The element name that holds the alternate directory separator
            /// character.  Not in Tcl.
            /// </summary>
            public static readonly string AlternateDirectorySeparator = "altDirSeparator"; // NOTE: Not in Tcl.
            /// <summary>
            /// The element name that holds the path list separator character.
            /// Not in Tcl, proposed by TIP #315.
            /// </summary>
            public static readonly string PathSeparator = "pathSeparator"; // NOTE: Not in Tcl, proposed by TIP #315.

            ///////////////////////////////////////////////////////////////////

            //
            // NOTE: The various "well known" values...
            //
            /// <summary>
            /// The "byteOrder" element value indicating a little-endian
            /// platform.
            /// </summary>
            public static readonly string LittleEndianValue = "littleEndian"; // byteOrder
            /// <summary>
            /// The "byteOrder" element value indicating a big-endian platform.
            /// </summary>
            public static readonly string BigEndianValue = "bigEndian";       // byteOrder

            ///////////////////////////////////////////////////////////////////

#if NATIVE && UNIX
            /// <summary>
            /// The "platform" element value indicating a Unix-like platform.
            /// </summary>
            public static readonly string UnixValue = "unix";
#endif
        }
        #endregion
    }
}
