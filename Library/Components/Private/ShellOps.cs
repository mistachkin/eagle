/*
 * ShellOps.cs --
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
using System.Globalization;
using System.IO;
using System.Text;
using System.Threading;
using Eagle._Attributes;
using Eagle._Components.Public;
using Eagle._Components.Public.Delegates;
using Eagle._Components.Shared;
using Eagle._Constants;
using Eagle._Containers.Public;
using Eagle._Interfaces.Private;
using Eagle._Interfaces.Public;

#if !CONSOLE
using ConsoleColor = Eagle._Components.Public.ConsoleColor;
#endif

using SharedStringOps = Eagle._Components.Shared.StringOps;

#if NET_STANDARD_21
using Index = Eagle._Constants.Index;
#endif

namespace Eagle._Components.Private
{
    /// <summary>
    /// This class provides internal helper methods and shared state used to
    /// implement the Eagle shell, including command line argument processing,
    /// interactive loop support, kiosk mode handling, and update checking.
    /// </summary>
    [ObjectId("d9374375-f3bb-402f-8c43-354168741995")]
    internal static class ShellOps
    {
        #region Private Constants
        #region Interactive Command Prefix
        /// <summary>
        /// The character used as the prefix for an interactive (i.e. shell)
        /// command.
        /// </summary>
        internal static readonly char InteractiveCommandPrefixChar =
            Characters.NumberSign;

        /// <summary>
        /// The string used as the prefix for an interactive (i.e. shell)
        /// command.
        /// </summary>
        internal static readonly string InteractiveCommandPrefix =
            InteractiveCommandPrefixChar.ToString();

        /// <summary>
        /// The string used as the prefix for an interactive system command
        /// (i.e. one that is forwarded to the operating system command
        /// processor).
        /// </summary>
        internal static readonly string InteractiveSystemCommandPrefix =
            StringOps.StrRepeat(2, InteractiveCommandPrefix);

        /// <summary>
        /// The array of supported interactive command prefixes, paired with
        /// their associated human-readable descriptions, ordered from longest
        /// (most specific) to shortest.
        /// </summary>
        internal static readonly string[] InteractiveCommandPrefixes = {
            StringOps.StrRepeat(4, InteractiveCommandPrefix),
            "interactive verbatim system command",
            StringOps.StrRepeat(3, InteractiveCommandPrefix),
            "interactive verbatim command",
            InteractiveSystemCommandPrefix,
            "interactive system command",
            InteractiveCommandPrefix,
            "interactive command"
        };

        /// <summary>
        /// The array of interactive verbatim command prefixes, paired with
        /// their replacement prefixes, used to detect commands that should be
        /// executed verbatim.
        /// </summary>
        private static readonly string[] InteractiveVerbatimCommandPrefixes = {
            StringOps.StrRepeat(4, InteractiveCommandPrefix),
            InteractiveSystemCommandPrefix,
            StringOps.StrRepeat(3, InteractiveCommandPrefix),
            InteractiveCommandPrefix
        };

        /// <summary>
        /// The default prefix used for an interactive (i.e. shell) command.
        /// </summary>
        internal static readonly string DefaultInteractiveCommandPrefix =
            InteractiveCommandPrefix;
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Pause / Unpause Handling
        //
        // NOTE: This is the number of microseconds to wait in between
        //       checking if the current interactive loop is (still)
        //       paused.
        //
        // HACK: This is purposely not read-only.
        //
        /// <summary>
        /// The number of microseconds to wait in between checking whether the
        /// current interactive loop is (still) paused.
        /// </summary>
        internal static long PauseMicroseconds = 2000000; /* 2 seconds */
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Script Update Checking
        //
        // NOTE: These procedure names are all RESERVED; however, they may
        //       legally be redefined to do nothing.
        //
        /// <summary>
        /// The name of the reserved procedure used to check for updates to the
        /// script engine.
        /// </summary>
        private const string CheckForEngineScript = "checkForEngine";
        /// <summary>
        /// The name of the reserved procedure used to check for updates to a
        /// binary plugin.
        /// </summary>
        private const string CheckForPluginScript = "checkForPlugin";
        /// <summary>
        /// The name of the reserved procedure used to fetch an available
        /// update.
        /// </summary>
        private const string FetchUpdateScript = "fetchUpdate";
        /// <summary>
        /// The name of the reserved procedure used to run the external updater
        /// tool and exit.
        /// </summary>
        private const string RunUpdateAndExitScript = "runUpdateAndExit";
        /// <summary>
        /// The name of the reserved procedure used to download and extract an
        /// available update.
        /// </summary>
        private const string DownloadAndExtractUpdate = "downloadAndExtractUpdate";
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region AppSettings Support
        /// <summary>
        /// The prefix used when constructing the application setting names that
        /// supply command line arguments.
        /// </summary>
        private static string ArgumentSettingPrefix = typeof(ShellOps).Name;
        /// <summary>
        /// The format string used to construct the application setting name
        /// that contains the count of arguments.
        /// </summary>
        private const string ArgumentCountSettingFormat = "{0}ArgumentCount";
        /// <summary>
        /// The format string used to construct the application setting name
        /// that contains an argument as a string value.
        /// </summary>
        private const string ArgumentStringSettingFormat = "{0}Argument{1}String";
        /// <summary>
        /// The format string used to construct the application setting name
        /// that contains an argument as a list value.
        /// </summary>
        private const string ArgumentListSettingFormat = "{0}Argument{1}List";
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Logging & Tracing
#if TEST
        //
        // HACK: This is purposely not read-only.
        //
        /// <summary>
        /// The name used for the primary trace listener associated with the
        /// shell.
        /// </summary>
        private static string MainListenerName =
            typeof(Interpreter).FullName + ".ShellMain";
#endif
        #endregion
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Shell Support Methods
        /// <summary>
        /// This method is the cross-application-domain entry point used to
        /// start the Eagle shell using the command line arguments for the
        /// current process.
        /// </summary>
        public static void StartupShellMain() /* System.CrossAppDomainDelegate */
        {
            /* IGNORED */
            Interpreter.ShellMain(Environment.GetCommandLineArgs());
        }

        ///////////////////////////////////////////////////////////////////////

        //
        // WARNING: This method is for use by the PrivateShellMainCore
        //          method only.
        //
        // NOTE: The rationale is that the active interpreter may be null
        //       (i.e. when operating in "what-if" mode) -AND- when that
        //       is the case, any argument value conversions should just
        //       fallback to using the (system) default culture.
        //
        /// <summary>
        /// This method gets the culture to use for argument value conversions,
        /// falling back to the system default culture when there is no active
        /// interpreter.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter context to use, which may be null.
        /// </param>
        /// <returns>
        /// The culture associated with the specified interpreter, or null if
        /// there is no interpreter.
        /// </returns>
        public static CultureInfo GetCultureInfo(
            Interpreter interpreter
            )
        {
            if (interpreter == null)
                return null;

            return interpreter.InternalCultureInfo;
        }

        ///////////////////////////////////////////////////////////////////////

        #region Kiosk Support
        /// <summary>
        /// This method applies the specified kiosk mode flags to the
        /// interpreter, optionally also configuring the argument-related kiosk
        /// behavior.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter to configure, which may be null.
        /// </param>
        /// <param name="flags">
        /// The kiosk mode flags to apply.
        /// </param>
        /// <param name="loops">
        /// The number of interactive loops that have already been entered.
        /// Some settings are only applied when this is zero.
        /// </param>
        /// <returns>
        /// True if one or more kiosk settings were changed; otherwise, false.
        /// </returns>
        private static bool ProcessKioskFlags(
            Interpreter interpreter,
            KioskFlags flags,
            int loops
            )
        {
            int count = 0;

            if (interpreter != null)
            {
                if (FlagOps.HasFlags(
                        flags, KioskFlags.Enable, true))
                {
                    interpreter.SetKioskLock();
                    count++;
                }
                else if (loops == 0)
                {
                    interpreter.UnsetKioskLock();
                    count++;
                }

                if (loops == 0)
                {
                    if (FlagOps.HasFlags(
                            flags, KioskFlags.UseArgv, true))
                    {
                        interpreter.SetKioskArgv();
                        count++;
                    }
                    else
                    {
                        interpreter.UnsetKioskArgv();
                        count++;
                    }
                }
            }

            return (count > 0);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method parses the specified value as kiosk mode flags, an
        /// integer, or a boolean and applies the resulting kiosk mode
        /// configuration to the interpreter.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter to configure.
        /// </param>
        /// <param name="value">
        /// The string value to interpret as kiosk mode settings.
        /// </param>
        /// <param name="loops">
        /// The number of interactive loops that have already been entered.
        /// </param>
        /// <param name="processed">
        /// Upon success, this is set to non-zero if the kiosk settings were
        /// changed.
        /// </param>
        /// <param name="error">
        /// Upon failure, this contains an appropriate error message.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise, an error code.
        /// </returns>
        public static ReturnCode ProcessKioskArgument(
            Interpreter interpreter,
            string value,
            int loops,
            ref bool processed,
            ref Result error
            )
        {
            if (interpreter == null)
            {
                error = "invalid interpreter";
                return ReturnCode.Error;
            }

            KioskFlags flags = KioskFlags.Default;
            CultureInfo cultureInfo = interpreter.InternalCultureInfo;
            Result localError; /* REUSED */
            ResultList errors = null;

            localError = null;

            object enumValue = EnumOps.TryParseFlags(
                interpreter, typeof(KioskFlags), null, value,
                cultureInfo, false, true, true, ref localError);

            if (enumValue is KioskFlags)
            {
                flags |= (KioskFlags)enumValue;

                processed = ProcessKioskFlags(
                    interpreter, flags, loops);

                return ReturnCode.Ok;
            }

            if (localError != null)
            {
                if (errors == null)
                    errors = new ResultList();

                errors.Add(localError);
            }

            int intValue = 0;

            localError = null;

            if (Value.GetInteger2(
                    value, ValueFlags.AnyInteger,
                    cultureInfo, ref intValue,
                    ref localError) == ReturnCode.Ok)
            {
                if (intValue > 0)
                    flags |= KioskFlags.Enable;

                if (intValue > 1)
                    flags |= KioskFlags.UseArgv;

                processed = ProcessKioskFlags(
                    interpreter, flags, loops);

                return ReturnCode.Ok;
            }

            if (localError != null)
            {
                if (errors == null)
                    errors = new ResultList();

                errors.Add(localError);
            }

            bool boolValue = false;

            localError = null;

            if (Value.GetBoolean2(
                    value, ValueFlags.AnyInteger,
                    cultureInfo, ref boolValue,
                    ref localError) == ReturnCode.Ok)
            {
                if (boolValue)
                    flags |= KioskFlags.Enable;

                processed = ProcessKioskFlags(
                    interpreter, flags, loops);

                return ReturnCode.Ok;
            }

            if (localError != null)
            {
                if (errors == null)
                    errors = new ResultList();

                errors.Add(localError);
            }

            error = errors;
            return ReturnCode.Error;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method returns a human-readable description of the current
        /// kiosk mode state for the interpreter.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter to query, which may be null.
        /// </param>
        /// <returns>
        /// A string describing the kiosk mode state, or null if there is no
        /// interpreter.
        /// </returns>
        public static string GetKioskDescription(
            Interpreter interpreter
            )
        {
            if (interpreter != null)
            {
                return interpreter.IsKioskLock() ?
                    interpreter.IsKioskArgv() ?
                        "enabled with argv refresh" :
                    "enabled" : "disabled";
            }

            return null;
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

#if TEST
        /// <summary>
        /// This method constructs the name of a trace listener from the
        /// specified base name and identifier.
        /// </summary>
        /// <param name="name">
        /// The base name to use, or null to use the default main listener
        /// name.
        /// </param>
        /// <param name="id">
        /// The identifier to append to the name.
        /// </param>
        /// <returns>
        /// The constructed trace listener name.
        /// </returns>
        public static string GetTraceListenerName(
            string name, /* in */
            long id      /* in */
            )
        {
            return String.Format("{0}:{1}",
                (name != null) ? name : MainListenerName, id);
        }
#endif

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method scans the command line arguments for the named option
        /// and, if found, returns its associated value, optionally removing
        /// the option and its value from the list.
        /// </summary>
        /// <param name="argv">
        /// The list of command line arguments to scan, which may be null.
        /// When removal is requested, this list may be modified.
        /// </param>
        /// <param name="name">
        /// The bare name of the command line option to find.
        /// </param>
        /// <param name="remove">
        /// Non-zero to remove the option and its value from the argument list
        /// when found.
        /// </param>
        /// <param name="value">
        /// Upon success, when the option is found, this receives its value (an
        /// empty string is converted to null).
        /// </param>
        /// <param name="error">
        /// Upon failure, this contains an appropriate error message.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise, an error code.
        /// </returns>
        public static ReturnCode GetArgumentValue(
            StringList argv,  /* in, out */
            string name,      /* in */
            bool remove,      /* in */
            ref string value, /* out */
            ref Result error  /* out */
            )
        {
            ReturnCode code = ReturnCode.Ok;

            if (argv != null)
            {
                //
                // NOTE: Scan all the command line arguments, searching for the
                //       ones we are interested in (i.e. mainly those that must
                //       be processed prior to interpreter creation for them to
                //       take effect).
                //
                int count = argv.Count;
                int nameIndex = Index.Invalid;

                for (int index = 0; index < count; index++)
                {
                    //
                    // NOTE: Grab the current command line argument.
                    //
                    string arg = argv[index];

                    //
                    // NOTE: This is the number of switch chars in front of
                    //       the current argument.
                    //
                    int argCount = 0;

                    //
                    // NOTE: Trims any leading switch chars from the current
                    //       command line argument and sets the count to the
                    //       number of switch chars actually removed.
                    //
                    arg = StringOps.TrimSwitchChars(arg, ref argCount);

                    //
                    // NOTE: Check the current argument to see if it matches
                    //       the named command line option (i.e. must have a
                    //       switch character prefix and must match the bare
                    //       name).
                    //
                    if ((argCount > 0) && StringOps.MatchSwitch(arg, name))
                    {
                        //
                        // NOTE: There must be a value after the option name.
                        //
                        if ((index + 1) >= count)
                        {
                            error = String.Format(
                                "wrong # args: should be \"-{0} <value>\"",
                                name);

                            code = ReturnCode.Error;
                            break;
                        }

                        //
                        // NOTE: There is a valid; grab it -AND- convert an
                        //       empty string to null, if necessary.
                        //
                        string localValue = argv[index + 1];

                        if (String.IsNullOrEmpty(localValue))
                            localValue = null;

                        nameIndex = index;
                        value = localValue;

                        break;
                    }
                }

                if ((code == ReturnCode.Ok) && remove &&
                    (nameIndex >= 0) && (nameIndex < count))
                {
                    argv.RemoveRange(nameIndex, 2);
                }
            }

            return code;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method determines whether the named command line option is
        /// present in the specified argument list.
        /// </summary>
        /// <param name="argv">
        /// The list of command line arguments to scan.  When this is null, the
        /// option is treated as present.
        /// </param>
        /// <param name="name">
        /// The bare name of the command line option to find.
        /// </param>
        /// <returns>
        /// True if the named option is present (or the list is null);
        /// otherwise, false.
        /// </returns>
        private static bool HaveArgumentValue(
            IList<string> argv,
            string name
            )
        {
            if (argv == null)
                return true;

            for (int index = 0; index < argv.Count; index++)
            {
                string arg = argv[index];
                int count = 0;

                arg = StringOps.TrimSwitchChars(arg, ref count);

                if ((count > 0) && StringOps.MatchSwitch(arg, name))
                    return true;
            }

            return false;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method determines whether arguments file names should be used,
        /// based on the presence of the associated command line option.
        /// </summary>
        /// <param name="argv">
        /// The list of command line arguments to scan, which may be null.
        /// </param>
        /// <returns>
        /// True if arguments file names should be used; otherwise, false.
        /// </returns>
        public static bool ShouldUseArgumentsFileNames(
            IList<string> argv
            )
        {
            return !HaveArgumentValue(
                argv, CommandLineOption.NoArgumentsFileNames);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method determines whether the interpreter host arguments
        /// should be locked, based on the presence of the associated command
        /// line option.
        /// </summary>
        /// <param name="argv">
        /// The list of command line arguments to scan, which may be null.
        /// </param>
        /// <returns>
        /// True if the host arguments should be locked; otherwise, false.
        /// </returns>
        public static bool ShouldLockHostArguments(
            IList<string> argv
            )
        {
            return HaveArgumentValue(
                argv, CommandLineOption.LockHostArguments);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method determines whether any of the specified files exists,
        /// returning the name of the first one found.
        /// </summary>
        /// <param name="fileNames">
        /// The list of candidate file names to check, which may be null.
        /// </param>
        /// <param name="fileName">
        /// Upon success, this receives the name of the first existing file.
        /// </param>
        /// <returns>
        /// True if one of the specified files exists; otherwise, false.
        /// </returns>
        public static bool SomeFileExists(
            StringList fileNames,
            ref string fileName
            )
        {
            if (fileNames == null)
                return false;

            foreach (string localFileName in fileNames)
            {
                if (String.IsNullOrEmpty(localFileName))
                    continue;

                if (File.Exists(localFileName))
                {
                    fileName = localFileName;
                    return true;
                }
            }

            return false;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method returns the most preferred arguments file name
        /// associated with the specified file name.
        /// </summary>
        /// <param name="fileName">
        /// The base file name used to derive the arguments file names.
        /// </param>
        /// <returns>
        /// The arguments file name to use, or null if none could be
        /// determined.
        /// </returns>
        public static string GetArgumentsFileName(
            string fileName
            )
        {
            StringList list = GetArgumentsFileNames(fileName);

            if (list == null)
                return null;

            int count = list.Count;

            if (count == 0)
                return null;

            return list[count - 1];
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method returns the list of candidate arguments file names
        /// associated with the specified file name.
        /// </summary>
        /// <param name="fileName">
        /// The base file name used to derive the arguments file names.
        /// </param>
        /// <returns>
        /// The list of candidate arguments file names, or null if none could
        /// be determined.
        /// </returns>
        public static StringList GetArgumentsFileNames(
            string fileName
            )
        {
            return PathOps.GetOverrideFileNames(
                fileName, FileExtension.Arguments, true, false);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method creates a text reader that reads from the specified
        /// string.
        /// </summary>
        /// <param name="text">
        /// The string to be read.
        /// </param>
        /// <param name="dispose">
        /// Upon success, this is set to non-zero to indicate that the returned
        /// reader should be disposed when no longer needed.
        /// </param>
        /// <param name="error">
        /// Upon failure, this contains an appropriate error message.
        /// </param>
        /// <returns>
        /// The created text reader, or null if it could not be created.
        /// </returns>
        private static TextReader GetTextReaderForString(
            string text,
            ref bool dispose,
            ref Result error
            )
        {
            try
            {
                dispose = true; /* NOTE: Do close all streams. */

                return new StringReader(text); /* throw */
            }
            catch (Exception e)
            {
                error = e;
            }

            return null;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method creates a text reader that reads from the specified
        /// file, optionally reading from the standard input channel.
        /// </summary>
        /// <param name="encoding">
        /// The encoding to use, or null to use the default encoding.
        /// </param>
        /// <param name="fileName">
        /// The name of the file to read, or a standard input designator.
        /// </param>
        /// <param name="console">
        /// Non-zero to permit reading from the console standard input channel
        /// when the file name designates standard input.
        /// </param>
        /// <param name="dispose">
        /// Upon success, this is set to non-zero if the returned reader should
        /// be disposed when no longer needed.
        /// </param>
        /// <param name="error">
        /// Upon failure, this contains an appropriate error message.
        /// </param>
        /// <returns>
        /// The created text reader, or null if it could not be created.
        /// </returns>
        private static TextReader GetTextReaderForFile(
            Encoding encoding,
            string fileName,
            bool console,
            ref bool dispose,
            ref Result error
            )
        {
            try
            {
#if CONSOLE
                if (console && (SharedStringOps.SystemNoCaseEquals(
                        fileName, CommandLineArgument.StandardInput) ||
                    SharedStringOps.SystemNoCaseEquals(
                        fileName, StandardChannel.Input)))
                {
                    //
                    // TODO: Allow the interpreter host (if available) to be
                    //       used here instead?
                    //
                    dispose = false; /* NOTE: Do not close standard input. */

                    return Console.In;
                }
                else
#endif
                {
                    dispose = true; /* NOTE: Do close all other files. */

                    return (encoding != null) ?
                        new StreamReader(fileName, encoding) :
                        new StreamReader(fileName); /* throw */
                }
            }
            catch (Exception e)
            {
                error = e;
            }

            return null;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method removes a number of leading arguments from the argument
        /// list and then inserts (or appends) the specified new arguments in
        /// their place.
        /// </summary>
        /// <param name="newArgv">
        /// The new arguments to insert or append, which may be null.
        /// </param>
        /// <param name="popCount">
        /// The number of leading arguments to remove from the argument list.
        /// </param>
        /// <param name="append">
        /// Non-zero to append the new arguments to the end of the list; zero
        /// to insert them where the removed arguments were.
        /// </param>
        /// <param name="argv">
        /// Upon return, this contains the resulting argument list.  A new list
        /// is created when necessary.
        /// </param>
        public static void CommitToArguments(
            IList<string> newArgv, /* in: OPTIONAL */
            int popCount,          /* in */
            bool append,           /* in */
            ref IList<string> argv /* in, out */
            )
        {
            //
            // NOTE: *WARNING* This assumes the arguments that need to be
            //       removed are at the start of the list provided by the
            //       caller.
            //
            while ((argv != null) && (popCount-- > 0))
                GenericOps<string>.PopFirstArgument(ref argv);

            //
            // NOTE: If we used up all the arguments (i.e. there were only
            //       "count" arguments in the list), the original argument
            //       list (i.e. "argv") will now be null.  If that is the
            //       case, use a new list.
            //
            if (argv == null)
                argv = new StringList();

            //
            // NOTE: If there are no new arguments then there is nothing
            //       left to do.
            //
            if (newArgv == null)
                return;

            //
            // NOTE: Insert each argument read from the file, in order,
            //       where the original argument(s) was/were removed.
            //
            int index = 0;

            foreach (string arg in newArgv)
            {
                if (append)
                    argv.Add(arg);
                else
                    argv.Insert(index++, arg);
            }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method reads command line arguments, one list per line, from
        /// the specified text reader and commits them to the argument list.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter context to use when parsing each line as a list.
        /// </param>
        /// <param name="textReader">
        /// The text reader to read the arguments from.
        /// </param>
        /// <param name="popCount">
        /// The number of leading arguments to remove from the argument list.
        /// </param>
        /// <param name="append">
        /// Non-zero to append the read arguments to the end of the list.
        /// </param>
        /// <param name="argv">
        /// Upon success, this contains the resulting argument list.
        /// </param>
        /// <param name="error">
        /// Upon failure, this contains an appropriate error message.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise, an error code.
        /// </returns>
        private static ReturnCode ReadArgumentsFromTextReader(
            Interpreter interpreter, /* in */
            TextReader textReader,   /* in */
            int popCount,            /* in */
            bool append,             /* in */
            ref IList<string> argv,  /* in, out */
            ref Result error         /* out */
            )
        {
            if (textReader == null)
            {
                error = "invalid stream";
                return ReturnCode.Error;
            }

            StringList newArgv = new StringList();

            while (true)
            {
                string line = textReader.ReadLine();

                if (line == null) // NOTE: End-of-file?
                    break;

                string trimLine = line.Trim();

                if (!String.IsNullOrEmpty(trimLine))
                {
                    if ((trimLine[0] != Characters.Comment) &&
                        (trimLine[0] != Characters.AltComment))
                    {
                        StringList list = null;

                        if (ParserOps<string>.SplitList(
                                interpreter, trimLine, 0, Length.Invalid,
                                true, ref list, ref error) == ReturnCode.Ok)
                        {
                            newArgv.Add(list);
                        }
                        else
                        {
                            //
                            // NOTE: The line read from the file cannot be
                            //       parsed as a list, fail now.
                            //
                            return ReturnCode.Error;
                        }
                    }
                }
            }

            CommitToArguments(newArgv, popCount, append, ref argv);
            return ReturnCode.Ok;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method reads command line arguments from one of the specified
        /// host scripts and commits them to the argument list.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter used to obtain and parse the host scripts.
        /// </param>
        /// <param name="argvFileNames">
        /// The list of candidate host script names to try, which may be null.
        /// </param>
        /// <param name="encoding">
        /// The encoding to use when reading from a file, or null to use the
        /// default encoding.
        /// </param>
        /// <param name="popCount">
        /// The number of leading arguments to remove from the argument list.
        /// </param>
        /// <param name="append">
        /// Non-zero to append the read arguments to the end of the list.
        /// </param>
        /// <param name="errorOnNotFound">
        /// Non-zero to treat the absence of any arguments as an error.
        /// </param>
        /// <param name="argvFileName">
        /// Upon success, this receives the name of the host script the
        /// arguments were read from.
        /// </param>
        /// <param name="argv">
        /// Upon success, this contains the resulting argument list.
        /// </param>
        /// <param name="readArgv">
        /// Upon success, this is incremented when the arguments are read from a
        /// host script.
        /// </param>
        /// <param name="errors">
        /// Upon failure, this contains one or more appropriate error messages.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise, an error code.
        /// </returns>
        public static ReturnCode ReadArgumentsFromHost(
            Interpreter interpreter,  /* in */
            StringList argvFileNames, /* in */
            Encoding encoding,        /* in */
            int popCount,             /* in */
            bool append,              /* in */
            bool errorOnNotFound,     /* in */
            ref string argvFileName,  /* out */
            ref IList<string> argv,   /* in, out */
            ref int readArgv,         /* in, out */
            ref ResultList errors     /* in, out */
            )
        {
            if (interpreter == null)
            {
                if (errors == null)
                    errors = new ResultList();

                errors.Add("invalid interpreter");
                return ReturnCode.Error;
            }

            if (argvFileNames == null)
                return ReturnCode.Ok;

            foreach (string localArgvFileName in argvFileNames)
            {
                ScriptFlags scriptFlags = ScriptOps.GetFlags(
                    interpreter, ScriptFlags.ApplicationOptionalFile |
                    ScriptFlags.Data, false, false);

                ReturnCode localCode;
                Result localResult = null;

                localCode = interpreter.GetScript(
                    localArgvFileName, ref scriptFlags, ref localResult);

                if (localCode != ReturnCode.Ok)
                {
                    if (localResult != null)
                    {
                        if (errors == null)
                            errors = new ResultList();

                        errors.Add(localResult);
                    }

                    continue;
                }

                if (localResult == null)
                {
                    if (errors == null)
                        errors = new ResultList();

                    errors.Add(String.Format(
                        "invalid host script for {0}",
                        FormatOps.WrapOrNull(localArgvFileName)));

                    return ReturnCode.Error;
                }

                bool dispose = true; /* EXEMPT */
                TextReader textReader = null;

                try
                {
                    string localFileName = null;

                    if (FlagOps.HasFlags(
                            scriptFlags, ScriptFlags.File, true))
                    {
                        localFileName = localResult;

                        textReader = GetTextReaderForFile(
                            encoding, localFileName, false, ref dispose,
                            ref localResult);
                    }
                    else
                    {
                        string text = localResult;

                        textReader = GetTextReaderForString(
                            text, ref dispose, ref localResult);
                    }

                    if (textReader == null)
                    {
                        if (localResult != null)
                        {
                            if (errors == null)
                                errors = new ResultList();

                            errors.Add(localResult);
                        }

                        return ReturnCode.Error;
                    }

                    localCode = ReadArgumentsFromTextReader(
                        interpreter, textReader, popCount, append,
                        ref argv, ref localResult);

                    if (localCode == ReturnCode.Ok)
                    {
                        //
                        // NOTE: If the interpreter host returned a file
                        //       name (even if it is different), use it;
                        //       otherwise, use the one originally given
                        //       to us by the caller.
                        //
                        if (localFileName != null)
                            argvFileName = localFileName;
                        else
                            argvFileName = localArgvFileName;

                        //
                        // NOTE: At this point (and only this point), we
                        //       know that the command line arguments,
                        //       if any, were read from the text reader.
                        //
                        readArgv++;
                    }
                    else
                    {
                        if (localResult != null)
                        {
                            if (errors == null)
                                errors = new ResultList();

                            errors.Add(localResult);
                        }
                    }

                    return localCode;
                }
                catch (Exception e)
                {
                    if (errors == null)
                        errors = new ResultList();

                    errors.Add(e);
                    return ReturnCode.Error;
                }
                finally
                {
                    if (textReader != null)
                    {
                        if (dispose)
                            textReader.Dispose();

                        textReader = null;
                    }
                }
            }

            if (errorOnNotFound)
            {
                if (errors == null)
                    errors = new ResultList();

                if (errors.Count == 0)
                    errors.Add("no arguments found via host");

                return ReturnCode.Error;
            }

            return ReturnCode.Ok;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method reads command line arguments from the specified file
        /// and commits them to the argument list.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter context to use when parsing each line as a list.
        /// </param>
        /// <param name="encoding">
        /// The encoding to use, or null to use the default encoding.
        /// </param>
        /// <param name="fileName">
        /// The name of the file to read the arguments from.
        /// </param>
        /// <param name="popCount">
        /// The number of leading arguments to remove from the argument list.
        /// </param>
        /// <param name="console">
        /// Non-zero to permit reading from the console standard input channel
        /// when the file name designates standard input.
        /// </param>
        /// <param name="append">
        /// Non-zero to append the read arguments to the end of the list.
        /// </param>
        /// <param name="argv">
        /// Upon success, this contains the resulting argument list.
        /// </param>
        /// <param name="error">
        /// Upon failure, this contains an appropriate error message.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise, an error code.
        /// </returns>
        public static ReturnCode ReadArgumentsFromFile(
            Interpreter interpreter, /* in */
            Encoding encoding,       /* in */
            string fileName,         /* in */
            int popCount,            /* in */
            bool console,            /* in */
            bool append,             /* in */
            ref IList<string> argv,  /* in, out */
            ref Result error         /* out */
            )
        {
            //
            // NOTE: Get the stream reader for the file containing the
            //       arguments to process.  If the file name is "-" or
            //       "stdin", we will end up reading arguments from the
            //       standard input stream.  Currently, this is always
            //       done via the Console; however, in the future it
            //       may use the interpreter host.
            //
            bool dispose = true; /* EXEMPT */
            TextReader textReader = null;

            try
            {
                textReader = GetTextReaderForFile(
                    encoding, fileName, console, ref dispose, ref error);

                if (textReader == null)
                    return ReturnCode.Error;

                return ReadArgumentsFromTextReader(
                    interpreter, textReader, popCount, append, ref argv,
                    ref error);
            }
            finally
            {
                if (textReader != null)
                {
                    if (dispose)
                        textReader.Dispose();

                    textReader = null;
                }
            }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method determines whether command line arguments should be
        /// read from the application settings.
        /// </summary>
        /// <param name="argv">
        /// The list of command line arguments to scan, which may be null.
        /// </param>
        /// <returns>
        /// True if arguments should be read from the application settings;
        /// otherwise, false.
        /// </returns>
        public static bool ShouldUseArgumentsAppSettings(
            IList<string> argv /* in */
            )
        {
            //
            // NOTE: This configuration parameter is considered to be
            //       part of the configuration of the interpreter itself,
            //       hence those flags are used here.
            //
            if (GlobalConfiguration.DoesValueExist(EnvVars.NoAppSettings,
                    ConfigurationFlags.InterpreterVerbose)) /* EXEMPT */
            {
                return false;
            }

            if (!ConfigurationOps.HaveAppSettings(true))
                return false;

            if (argv == null)
                return true;

            return !HaveArgumentValue(
                argv, CommandLineOption.NoAppSettings);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method reads command line arguments from the application
        /// settings and commits them to the argument list.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter context to use when parsing list-valued settings.
        /// </param>
        /// <param name="popCount">
        /// The number of leading arguments to remove from the argument list.
        /// </param>
        /// <param name="append">
        /// Non-zero to append the read arguments to the end of the list.
        /// </param>
        /// <param name="argv">
        /// Upon success, this contains the resulting argument list.
        /// </param>
        /// <param name="error">
        /// Upon failure, this contains an appropriate error message.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise, an error code.
        /// </returns>
        public static ReturnCode ReadArgumentsFromAppSettings(
            Interpreter interpreter, /* in */
            int popCount,            /* in */
            bool append,             /* in */
            ref IList<string> argv,  /* in, out */
            ref Result error         /* out */
            )
        {
            int newArgc;

            if (!ConfigurationOps.TryGetIntegerAppSetting(String.Format(
                    ArgumentCountSettingFormat, ArgumentSettingPrefix),
                    out newArgc))
            {
                return ReturnCode.Ok;
            }

            if (newArgc < 0)
            {
                error = "argument count cannot be negative";
                return ReturnCode.Error;
            }

            if (newArgc == 0)
            {
                if (argv == null)
                    argv = new StringList();

                return ReturnCode.Ok;
            }

            StringList newArgv = new StringList();

            for (int index = 0; index < newArgc; index++)
            {
                string value;
                Result localError = null;
                ResultList errors = null;

                if (ConfigurationOps.TryGetAppSetting(String.Format(
                        ArgumentStringSettingFormat, ArgumentSettingPrefix,
                        index), out value, ref localError))
                {
                    newArgv.Add(value);
                    continue;
                }
                else if (localError != null)
                {
                    if (errors == null)
                        errors = new ResultList();

                    errors.Add(localError);
                }

                localError = null;

                if (ConfigurationOps.TryGetAppSetting(String.Format(
                        ArgumentListSettingFormat, ArgumentSettingPrefix,
                        index), out value, ref localError))
                {
                    StringList list = null;

                    localError = null;

                    if (ParserOps<string>.SplitList(
                            interpreter, value, 0, Length.Invalid, true,
                            ref list, ref localError) != ReturnCode.Ok)
                    {
                        if (localError != null)
                        {
                            if (errors == null)
                                errors = new ResultList();

                            errors.Add(localError);
                        }

                        error = errors;
                        return ReturnCode.Error;
                    }

                    newArgv.Add(list);
                    continue;
                }
                else if (localError != null)
                {
                    if (errors == null)
                        errors = new ResultList();

                    errors.Add(localError);
                }

                error = errors;
                return ReturnCode.Error;
            }

            CommitToArguments(newArgv, popCount, append, ref argv);
            return ReturnCode.Ok;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method attempts to get the number of arguments in the
        /// specified list.
        /// </summary>
        /// <param name="argv">
        /// The list of arguments to query, which may be null.
        /// </param>
        /// <param name="count">
        /// Upon success, this receives the number of arguments in the list.
        /// </param>
        /// <returns>
        /// True if the count was obtained; otherwise, false.
        /// </returns>
        public static bool MaybeGetArgumentCount(
            IList<string> argv, /* in */
            ref int count       /* in, out */
            )
        {
            if (argv == null)
                return false;

            count = argv.Count;
            return true;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method attempts to get the argument at the specified index,
        /// optionally trimming surrounding whitespace.
        /// </summary>
        /// <param name="argv">
        /// The list of arguments to query, which may be null.
        /// </param>
        /// <param name="index">
        /// The zero-based index of the argument to get.
        /// </param>
        /// <param name="noTrim">
        /// Non-zero to return the argument without trimming surrounding
        /// whitespace.
        /// </param>
        /// <param name="arg">
        /// Upon success, this receives the argument value.
        /// </param>
        /// <returns>
        /// True if the argument was obtained; otherwise, false.
        /// </returns>
        public static bool MaybeGetArgument(
            IList<string> argv, /* in */
            int index,          /* in */
            bool noTrim,        /* in */
            out string arg      /* out */
            )
        {
            if (argv == null)
            {
                arg = null;
                return false;
            }

            int count = argv.Count;

            if ((index < 0) || (index >= count))
            {
                arg = null;
                return false;
            }

            arg = argv[index];

            if (!noTrim && (arg != null))
                arg = arg.Trim();

            return true;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method attempts to set the argument at the specified index to
        /// a new value.
        /// </summary>
        /// <param name="argv">
        /// The list of arguments to modify, which may be null.
        /// </param>
        /// <param name="index">
        /// The zero-based index of the argument to set.
        /// </param>
        /// <param name="arg">
        /// The new argument value.
        /// </param>
        /// <param name="count">
        /// Upon success, this receives the number of arguments in the list.
        /// </param>
        /// <returns>
        /// True if the argument was set; otherwise, false.
        /// </returns>
        public static bool MaybeSetArgument(
            IList<string> argv, /* in */
            int index,          /* in */
            string arg,         /* in */
            ref int count       /* in, out */
            )
        {
            if (argv == null)
                return false;

            count = argv.Count;

            if ((index < 0) || (index >= count))
                return false;

            argv[index] = arg;
            return true;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method gets the argument at the specified index and passes it
        /// to the configured preview argument callback, advancing the index
        /// and handling any error.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter context to use.
        /// </param>
        /// <param name="callbackData">
        /// The shell callback data containing the preview argument callback.
        /// </param>
        /// <param name="clientData">
        /// The client data to pass to the callback.
        /// </param>
        /// <param name="phase">
        /// The argument processing phase.
        /// </param>
        /// <param name="noTrim">
        /// Non-zero to obtain the argument without trimming surrounding
        /// whitespace.
        /// </param>
        /// <param name="whatIf">
        /// Non-zero to perform a trial run without making any persistent
        /// changes.
        /// </param>
        /// <param name="interactiveHost">
        /// Upon return, this may contain the refreshed interactive host.
        /// </param>
        /// <param name="index">
        /// Upon return, this contains the (possibly advanced) argument index.
        /// </param>
        /// <param name="arg">
        /// Upon return, this receives the (possibly modified) argument value.
        /// </param>
        /// <param name="gotArg">
        /// Upon return, this is set to non-zero if an argument was obtained.
        /// </param>
        /// <param name="savedArg">
        /// Upon return, this receives the original argument value before any
        /// preview.
        /// </param>
        /// <param name="argv">
        /// Upon return, this contains the (possibly modified) argument list.
        /// </param>
        /// <param name="result">
        /// Upon failure, this contains an appropriate error message.
        /// </param>
        /// <param name="quiet">
        /// Upon return, this is set to non-zero if output should be
        /// suppressed.
        /// </param>
        /// <param name="exitCode">
        /// Upon failure, this receives the failure exit code.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise, an error code.
        /// </returns>
        public static ReturnCode MaybeGetAndPreviewArgument(
            Interpreter interpreter,              /* in */
            IShellCallbackData callbackData,      /* in */
            IClientData clientData,               /* in */
            ArgumentPhase phase,                  /* in */
            bool noTrim,                          /* in */
            bool whatIf,                          /* in */
            ref IInteractiveHost interactiveHost, /* in, out */
            ref int index,                        /* in, out */
            out string arg,                       /* out */
            out bool gotArg,                      /* out */
            out string savedArg,                  /* out */
            ref IList<string> argv,               /* in, out */
            ref Result result,                    /* in, out */
            ref bool quiet,                       /* out */
            ref ExitCode exitCode                 /* out */
            )
        {
            if (MaybeGetArgument(argv, index, noTrim, out arg))
            {
                gotArg = true;
                savedArg = arg;
            }
            else
            {
                gotArg = false;
                savedArg = null;
            }

            ReturnCode code;
            int savedIndex = index;

            code = PreviewArgument(
                interpreter, interactiveHost, clientData, callbackData,
                phase, whatIf, ref index, ref arg, ref argv, ref result);

            if (code == ReturnCode.Ok)
            {
                //
                // NOTE: If the callback did not change the argument index,
                //       advance to the next argument index; otherwise, we
                //       leave it alone.
                //
                if (index == savedIndex)
                    index++;
            }
            else
            {
                //
                // BUGFIX: We may have evaluated some code and the host may
                //         have been changed; grab it again.
                //
                ShellMainCoreError(
                    interpreter, savedArg, arg, code, result, whatIf,
                    ref argv, ref interactiveHost, ref quiet, ref result);

                exitCode = FailureExitCode(interpreter);
            }

            return code;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method invokes the configured preview argument callback, if
        /// any, to inspect or modify the current argument.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter context to use.
        /// </param>
        /// <param name="interactiveHost">
        /// The interactive host to pass to the callback.
        /// </param>
        /// <param name="clientData">
        /// The client data to pass to the callback.
        /// </param>
        /// <param name="callbackData">
        /// The shell callback data containing the preview argument callback.
        /// </param>
        /// <param name="phase">
        /// The argument processing phase.
        /// </param>
        /// <param name="whatIf">
        /// Non-zero to perform a trial run without making any persistent
        /// changes.
        /// </param>
        /// <param name="index">
        /// Upon return, this contains the (possibly modified) argument index.
        /// </param>
        /// <param name="arg">
        /// Upon return, this contains the (possibly modified) argument value.
        /// </param>
        /// <param name="argv">
        /// Upon return, this contains the (possibly modified) argument list.
        /// </param>
        /// <param name="result">
        /// Upon failure, this contains an appropriate error message.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise, an error code.
        /// </returns>
        public static ReturnCode PreviewArgument(
            Interpreter interpreter,          /* in */
            IInteractiveHost interactiveHost, /* in */
            IClientData clientData,           /* in */
            IShellCallbackData callbackData,  /* in */
            ArgumentPhase phase,              /* in */
            bool whatIf,                      /* in */
            ref int index,                    /* in, out */
            ref string arg,                   /* in, out */
            ref IList<string> argv,           /* in, out */
            ref Result result                 /* in, out */
            )
        {
            ReturnCode code = ReturnCode.Ok;
            PreviewArgumentCallback previewArgumentCallback;

            if (ShellCallbackData.GetPreviewArgumentCallback(
                    callbackData, out previewArgumentCallback))
            {
                try
                {
                    code = previewArgumentCallback(
                        interpreter, interactiveHost, clientData, phase,
                        whatIf, ref index, ref arg, ref argv, ref result);
                }
                catch (Exception e)
                {
                    result = e;
                    code = ReturnCode.Error;
                }

                //
                // NOTE: The shell callbacks may have been changed via the
                //       executed callback; therefore, refresh those which
                //       were not directly supplied by the caller.
                //
                if ((interpreter != null) && !whatIf)
                {
                    /* NO RESULT */
                    interpreter.RefreshShellCallbacks();
                }
            }

            return code;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method invokes the configured unknown argument callback, if
        /// any, to handle an argument that was not otherwise recognized.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter context to use.
        /// </param>
        /// <param name="interactiveHost">
        /// The interactive host to pass to the callback.
        /// </param>
        /// <param name="clientData">
        /// The client data to pass to the callback.
        /// </param>
        /// <param name="callbackData">
        /// The shell callback data containing the unknown argument callback.
        /// </param>
        /// <param name="switchCount">
        /// The number of switch characters that prefixed the argument.
        /// </param>
        /// <param name="arg">
        /// The unrecognized argument value.
        /// </param>
        /// <param name="whatIf">
        /// Non-zero to perform a trial run without making any persistent
        /// changes.
        /// </param>
        /// <param name="wasHandled">
        /// Upon return, this is set to non-zero if the callback handled the
        /// argument.
        /// </param>
        /// <param name="argv">
        /// Upon return, this contains the (possibly modified) argument list.
        /// </param>
        /// <param name="result">
        /// Upon failure, this contains an appropriate error message.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise, an error code.
        /// </returns>
        public static ReturnCode UnknownArgument(
            Interpreter interpreter,          /* in */
            IInteractiveHost interactiveHost, /* in */
            IClientData clientData,           /* in */
            IShellCallbackData callbackData,  /* in */
            int switchCount,                  /* in */
            string arg,                       /* in */
            bool whatIf,                      /* in */
            ref bool wasHandled,              /* out */
            ref IList<string> argv,           /* in, out */
            ref Result result                 /* in, out */
            )
        {
            ReturnCode code = ReturnCode.Ok;
            UnknownArgumentCallback unknownArgumentCallback;

            if (ShellCallbackData.GetUnknownArgumentCallback(
                    callbackData, out unknownArgumentCallback))
            {
                try
                {
                    code = unknownArgumentCallback(interpreter,
                        interactiveHost, clientData, switchCount,
                        arg, whatIf, ref argv, ref result);
                }
                catch (Exception e)
                {
                    result = e;
                    code = ReturnCode.Error;
                }

                //
                // NOTE: The shell callbacks may have been changed via the
                //       executed callback; therefore, refresh those which
                //       were not directly supplied by the caller.
                //
                if ((interpreter != null) && !whatIf)
                {
                    /* NO RESULT */
                    interpreter.RefreshShellCallbacks();
                }

                wasHandled = true;
            }

            return code;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method changes the colors of the specified interactive host to
        /// high-contrast colors, saving the previous colors so they can be
        /// restored later.
        /// </summary>
        /// <param name="interactiveHost">
        /// The interactive host whose colors should be changed, which may be
        /// null.
        /// </param>
        /// <param name="savedForegroundColor">
        /// Upon success, this receives the previous foreground color.
        /// </param>
        /// <param name="savedBackgroundColor">
        /// Upon success, this receives the previous background color.
        /// </param>
        /// <returns>
        /// True if the colors were changed; otherwise, false.
        /// </returns>
        private static bool BeginHighContrastColors(
            IInteractiveHost interactiveHost,
            ref ConsoleColor savedForegroundColor,
            ref ConsoleColor savedBackgroundColor
            )
        {
            if (interactiveHost == null)
                return false;

            IColorHost colorHost = interactiveHost as IColorHost;

            if (colorHost == null)
                return false;

            if (!colorHost.GetColors(
                    ref savedForegroundColor, ref savedBackgroundColor))
            {
                return false;
            }

            //
            // TODO: Maybe change the background color here as well?
            //
            if (!colorHost.SetColors(
                    true, true, HostOps.GetHighContrastColor(
                    savedBackgroundColor), savedBackgroundColor))
            {
                return false;
            }

            return true;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method restores the previously saved colors of the specified
        /// interactive host.
        /// </summary>
        /// <param name="interactiveHost">
        /// The interactive host whose colors should be restored, which may be
        /// null.
        /// </param>
        /// <param name="savedForegroundColor">
        /// Upon entry, this contains the foreground color to restore; upon
        /// success, it is reset.
        /// </param>
        /// <param name="savedBackgroundColor">
        /// Upon entry, this contains the background color to restore; upon
        /// success, it is reset.
        /// </param>
        /// <returns>
        /// True if the colors were restored; otherwise, false.
        /// </returns>
        private static bool EndHighContrastColors(
            IInteractiveHost interactiveHost,
            ref ConsoleColor savedForegroundColor,
            ref ConsoleColor savedBackgroundColor
            )
        {
            if (interactiveHost == null)
                return false;

            IColorHost colorHost = interactiveHost as IColorHost;

            if (colorHost == null)
                return false;

            if (!colorHost.SetColors(
                    true, true, savedForegroundColor, savedBackgroundColor))
            {
                return false;
            }

            savedForegroundColor = _ConsoleColor.None;
            savedBackgroundColor = _ConsoleColor.None;

            return true;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method writes a line of output to the specified interactive
        /// host, temporarily using high-contrast colors and synchronizing
        /// access where supported.
        /// </summary>
        /// <param name="interactiveHost">
        /// The interactive host to write to, which may be null.
        /// </param>
        /// <param name="value">
        /// The value to write.
        /// </param>
        private static void WriteHost(
            IInteractiveHost interactiveHost,
            string value
            )
        {
            if (interactiveHost == null)
                return;

            ISynchronizeStatic synchronizeStatic =
                interactiveHost as ISynchronizeStatic;

            bool locked = false;

            if (synchronizeStatic != null)
                synchronizeStatic.StaticTryLock(ref locked);

            try
            {
                ConsoleColor savedForegroundColor = _ConsoleColor.None;
                ConsoleColor savedBackgroundColor = _ConsoleColor.None;

                BeginHighContrastColors(
                    interactiveHost, ref savedForegroundColor,
                    ref savedBackgroundColor);

                try
                {
                    try
                    {
                        /* IGNORED */
                        interactiveHost.WriteLine(value);

                        return;
                    }
                    catch
                    {
                        // do nothing.
                    }
                }
                finally
                {
                    EndHighContrastColors(
                        interactiveHost, ref savedForegroundColor,
                        ref savedBackgroundColor);
                }
            }
            finally
            {
                if (synchronizeStatic != null)
                    synchronizeStatic.StaticExitLock(ref locked);
            }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method writes a line of output to the specified interactive
        /// host and, where appropriate, also to the console and the debug
        /// host.
        /// </summary>
        /// <param name="interactiveHost">
        /// The interactive host to write to, which may be null.
        /// </param>
        /// <param name="value">
        /// The value to write.
        /// </param>
        private static void WriteCore(
            IInteractiveHost interactiveHost,
            string value
            )
        {
            /* NO RESULT */
            WriteHost(interactiveHost, value);

#if CONSOLE
            //
            // BUGFIX: *HACK* Avoid duplicate console output.
            //
            if (!(interactiveHost is _Hosts.Console))
            {
                try
                {
                    /* NO RESULT */
                    ConsoleOps.WriteCore(value); /* throw */

                    return;
                }
                catch
                {
                    // do nothing.
                }
            }

#endif

            /* NO RESULT */
            DebugOps.WriteWithoutFail(
                interactiveHost as IDebugHost, value);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method writes a prompt to the interactive host associated with
        /// the specified interpreter.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter whose interactive host should be used, which may be
        /// null.
        /// </param>
        /// <param name="value">
        /// The prompt value to write.
        /// </param>
        public static void WritePrompt(
            Interpreter interpreter,
            string value
            )
        {
            IInteractiveHost interactiveHost = null;

            if (interpreter != null)
                interactiveHost = interpreter.GetInteractiveHost();

            WriteCore(interactiveHost, value);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method writes a prompt to the specified interactive host.
        /// </summary>
        /// <param name="interactiveHost">
        /// The interactive host to write to, which may be null.
        /// </param>
        /// <param name="value">
        /// The prompt value to write.
        /// </param>
        public static void WritePrompt(
            IInteractiveHost interactiveHost,
            string value
            )
        {
            WriteCore(interactiveHost, value);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method formats and writes a result to the specified
        /// interactive host.
        /// </summary>
        /// <param name="interactiveHost">
        /// The interactive host to write to, which may be null.
        /// </param>
        /// <param name="code">
        /// The return code associated with the result.
        /// </param>
        /// <param name="result">
        /// The result to format and write.
        /// </param>
        /// <param name="errorLine">
        /// The error line number associated with the result, or zero if none.
        /// </param>
        public static void WriteResult( /* FOR WriteAccessError USE ONLY. */
            IInteractiveHost interactiveHost,
            ReturnCode code,
            Result result,
            int errorLine
            )
        {
            WriteCore(
                interactiveHost, ResultOps.Format(code, result, errorLine));
        }

        ///////////////////////////////////////////////////////////////////////

        //
        // NOTE: These methods are private because it currently seems unlikely
        //       that they will be useful to any external callers (i.e. those
        //       other than ShellMainCore).
        //
        /// <summary>
        /// This method reports a shell error using the interactive host and
        /// quiet setting obtained from the specified interpreter.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter to report the error for, which may be null.
        /// </param>
        /// <param name="savedArg">
        /// The original argument value associated with the error.
        /// </param>
        /// <param name="arg">
        /// The (possibly modified) argument value associated with the error.
        /// </param>
        /// <param name="localCode">
        /// The return code associated with the error.
        /// </param>
        /// <param name="localResult">
        /// The result or error message to report.
        /// </param>
        public static void ShellMainCoreError( /* FOR ShellMain USE ONLY. */
            Interpreter interpreter,
            string savedArg,
            string arg,
            ReturnCode localCode,
            Result localResult
            )
        {
            IInteractiveHost interactiveHost = null;
            bool quiet = false;

            if (interpreter != null)
            {
                interactiveHost = interpreter.GetInteractiveHost();
                quiet = interpreter.ShouldBeQuiet();
            }

            ShellMainCoreError(
                interpreter, savedArg, arg, localCode, localResult,
                ref interactiveHost, ref quiet);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method reports a non-API generated shell error, for which no
        /// error line, script stack trace, or return code information is
        /// needed.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter to report the error for, which may be null.
        /// </param>
        /// <param name="savedArg">
        /// The original argument value associated with the error.
        /// </param>
        /// <param name="arg">
        /// The (possibly modified) argument value associated with the error.
        /// </param>
        /// <param name="localCode">
        /// The return code associated with the error.
        /// </param>
        /// <param name="localResult">
        /// The result or error message to report.
        /// </param>
        /// <param name="interactiveHost">
        /// Upon return, this may contain the refreshed interactive host.
        /// </param>
        /// <param name="quiet">
        /// Upon return, this is set to non-zero if output should be
        /// suppressed.
        /// </param>
        private static void ShellMainCoreError(
            Interpreter interpreter,
            string savedArg,
            string arg,
            ReturnCode localCode,
            Result localResult,
            ref IInteractiveHost interactiveHost,
            ref bool quiet
            )
        {
            //
            // NOTE: This method overload is for non-API generated errors only.
            //       No error line info, script stack trace, or return code is
            //       needed.
            //
            IList<string> argv = null;
            Result result = null;

            ShellMainCoreError(
                interpreter, savedArg, arg, localCode, localResult,
                false, ref argv, ref interactiveHost, ref quiet,
                ref result);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method reports a non-API generated shell error, for which no
        /// error line, script stack trace, or return code information is
        /// needed.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter to report the error for, which may be null.
        /// </param>
        /// <param name="savedArg">
        /// The original argument value associated with the error.
        /// </param>
        /// <param name="arg">
        /// The (possibly modified) argument value associated with the error.
        /// </param>
        /// <param name="localResult">
        /// The result or error message to report.
        /// </param>
        /// <param name="whatIf">
        /// Non-zero to capture the result instead of writing it to the
        /// interactive host.
        /// </param>
        /// <param name="argv">
        /// Upon return, this contains the (possibly modified) argument list.
        /// </param>
        /// <param name="interactiveHost">
        /// Upon return, this may contain the refreshed interactive host.
        /// </param>
        /// <param name="quiet">
        /// Upon return, this is set to non-zero if output should be
        /// suppressed.
        /// </param>
        /// <param name="result">
        /// Upon return, when performing a trial run, this receives a copy of
        /// the reported result.
        /// </param>
        public static void ShellMainCoreError(
            Interpreter interpreter,
            string savedArg,
            string arg,
            Result localResult,
            bool whatIf,
            ref IList<string> argv,
            ref IInteractiveHost interactiveHost,
            ref bool quiet,
            ref Result result
            )
        {
            //
            // NOTE: This method overload is for non-API generated errors only.
            //       No error line info, script stack trace, or return code is
            //       needed.
            //
            ShellMainCoreError(
                interpreter, savedArg, arg, ReturnCode.Error, localResult,
                whatIf, ref argv, ref interactiveHost, ref quiet, ref result);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method reports a [non-script] API generated shell error, for
        /// which no error line or script stack trace information is needed.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter to report the error for, which may be null.
        /// </param>
        /// <param name="savedArg">
        /// The original argument value associated with the error.
        /// </param>
        /// <param name="arg">
        /// The (possibly modified) argument value associated with the error.
        /// </param>
        /// <param name="localCode">
        /// The return code associated with the error.
        /// </param>
        /// <param name="localResult">
        /// The result or error message to report.
        /// </param>
        /// <param name="whatIf">
        /// Non-zero to capture the result instead of writing it to the
        /// interactive host.
        /// </param>
        /// <param name="argv">
        /// Upon return, this contains the (possibly modified) argument list.
        /// </param>
        /// <param name="interactiveHost">
        /// Upon return, this may contain the refreshed interactive host.
        /// </param>
        /// <param name="quiet">
        /// Upon return, this is set to non-zero if output should be
        /// suppressed.
        /// </param>
        /// <param name="result">
        /// Upon return, when performing a trial run, this receives a copy of
        /// the reported result.
        /// </param>
        public static void ShellMainCoreError(
            Interpreter interpreter,
            string savedArg,
            string arg,
            ReturnCode localCode,
            Result localResult,
            bool whatIf,
            ref IList<string> argv,
            ref IInteractiveHost interactiveHost,
            ref bool quiet,
            ref Result result
            )
        {
            //
            // NOTE: This method overload is for [non-script] API generated
            //       errors only.  No error line info or script stack trace is
            //       required.
            //
            ShellMainCoreError(interpreter, savedArg, arg, localCode,
                localResult, 0, false, true, whatIf, ref argv,
                ref interactiveHost, ref quiet, ref result);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method reports a shell error, optionally writing it (and any
        /// associated script stack trace) to the interactive host, or
        /// capturing it when performing a trial run.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter to report the error for, which may be null.
        /// </param>
        /// <param name="savedArg">
        /// The original argument value associated with the error.
        /// </param>
        /// <param name="arg">
        /// The (possibly modified) argument value associated with the error.
        /// </param>
        /// <param name="localCode">
        /// The return code associated with the error.
        /// </param>
        /// <param name="localResult">
        /// The result or error message to report.
        /// </param>
        /// <param name="errorLine">
        /// The error line number associated with the result, or zero if none.
        /// </param>
        /// <param name="errorInfo">
        /// Non-zero to also report the script stack trace when debugging is
        /// enabled.
        /// </param>
        /// <param name="strict">
        /// Non-zero to report the error even when no interpreter or interactive
        /// host is available.
        /// </param>
        /// <param name="whatIf">
        /// Non-zero to capture the result instead of writing it to the
        /// interactive host.
        /// </param>
        /// <param name="argv">
        /// Upon return, this contains the (possibly modified) argument list.
        /// </param>
        /// <param name="interactiveHost">
        /// Upon return, this may contain the refreshed interactive host.
        /// </param>
        /// <param name="quiet">
        /// Upon return, this is set to non-zero if output should be
        /// suppressed.
        /// </param>
        /// <param name="result">
        /// Upon return, when performing a trial run, this receives a copy of
        /// the reported result.
        /// </param>
        public static void ShellMainCoreError(
            Interpreter interpreter,
            string savedArg,
            string arg,
            ReturnCode localCode,
            Result localResult,
            int errorLine,
            bool errorInfo,
            bool strict,
            bool whatIf,
            ref IList<string> argv,
            ref IInteractiveHost interactiveHost,
            ref bool quiet,
            ref Result result
            )
        {
            TraceOps.DebugTrace(String.Format(
                "ShellMainCoreError: interpreter = {0}, " +
                "savedArg = {1}, arg = {2}, localCode = {3}, " +
                "localResult = {4}, errorLine = {5}, " +
                "errorInfo = {6}, strict = {7}, whatIf = {8}, " +
                "argv = {9}, interactiveHost = {10}, quiet = {11}, " +
                "result = {12}",
                FormatOps.InterpreterNoThrow(interpreter),
                FormatOps.WrapOrNull(true, true, savedArg),
                FormatOps.WrapOrNull(true, true, arg), localCode,
                FormatOps.WrapOrNull(true, true, localResult),
                errorLine, errorInfo, strict, whatIf,
                FormatOps.WrapOrNull(true, true, argv),
                FormatOps.WrapOrNull(interactiveHost), quiet,
                FormatOps.WrapOrNull(true, true, result)),
                typeof(ShellOps).Name, TracePriority.ShellError);

            if (interpreter == null)
            {
                if (strict)
                {
                    if (whatIf)
                    {
                        result = Result.Copy(
                            localResult, ResultFlags.CopyObject); /* COPY */
                    }
                    else
                    {
                        /* NO RESULT */
                        HostOps.WriteConsoleOrComplain(
                            localCode, localResult, errorLine);
                    }
                }

                //
                // NOTE: Nothing else to do, return now.
                //
                return;
            }

            //
            // NOTE: Always grab interpreter host fresh as it can change
            //       after any user code has been evaluated or executed.
            //
            interactiveHost = interpreter.GetInteractiveHost();

            //
            // NOTE: See if quiet mode is enabled for the interpreter.
            //       If so, we skip any output because MSBuild may be
            //       watching us (i.e. it can cause the build to fail).
            //
            quiet = interpreter.ShouldBeQuiet();

            if (quiet)
                return;

            //
            // NOTE: Is the interpreter host unavailable now?
            //
            if (interactiveHost == null)
            {
                if (strict)
                {
                    //
                    // NOTE: No interpreter host is available.
                    //
                    if (whatIf)
                    {
                        result = Result.Copy(localResult,
                            ResultFlags.CopyObject); /* COPY */
                    }
                    else
                    {
                        /* NO RESULT */
                        HostOps.WriteConsoleOrComplain(
                            localCode, localResult, errorLine);
                    }
                }

                //
                // NOTE: Nothing else to do, return now.
                //
                return;
            }

            //
            // NOTE: Write the result to the interpreter host.  If the
            //       error line is zero, it will not actually be output.
            //
            if (whatIf)
            {
                result = Result.Copy(
                    localResult, ResultFlags.CopyObject); /* COPY */
            }
            else
            {
                /* IGNORED */
                interactiveHost.WriteResultLine(
                    localCode, localResult, errorLine);
            }

            //
            // NOTE: Do we want to report the script stack trace as well?
            //       First, see if debug mode has been enabled for the
            //       interpreter.
            //
            if (errorInfo && interpreter.Debug)
            {
                Result localError = null;

                if (interpreter.InternalCopyErrorInformation(
                        VariableFlags.None, false,
                        ref localError) == ReturnCode.Ok)
                {
                    if ((localError != null) && !whatIf)
                    {
                        /* IGNORED */
                        interactiveHost.WriteResultLine(
                            localCode, localError.ErrorInfo);
                    }
                }
            }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method returns the exit code that represents successful
        /// completion of the shell.
        /// </summary>
        /// <returns>
        /// The success exit code.
        /// </returns>
        public static ExitCode SuccessExitCode()
        {
            return ResultOps.SuccessExitCode();
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method returns the exit code that represents a failure of the
        /// shell.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter associated with the failure, which may be null.  It
        /// is used for diagnostic purposes only.
        /// </param>
        /// <returns>
        /// The failure exit code.
        /// </returns>
        public static ExitCode FailureExitCode(
            Interpreter interpreter
            )
        {
            ExitCode exitCode = ResultOps.FailureExitCode();

            TraceOps.DebugTrace(String.Format(
                "FailureExitCode: using exit code {0} for interpreter {1}",
                FormatOps.WrapOrNull(exitCode), FormatOps.InterpreterNoThrow(
                interpreter)), typeof(ShellOps).Name, TracePriority.ShellDebug);

            return exitCode;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method converts the specified return code into the
        /// corresponding shell exit code.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter associated with the conversion, which may be null.
        /// It is used for diagnostic purposes only.
        /// </param>
        /// <param name="returnCode">
        /// The return code to convert.
        /// </param>
        /// <param name="exceptions">
        /// Non-zero to treat exceptional return codes as errors.
        /// </param>
        /// <returns>
        /// The exit code corresponding to the specified return code.
        /// </returns>
        public static ExitCode ReturnCodeToExitCode(
            Interpreter interpreter,
            ReturnCode returnCode,
            bool exceptions
            )
        {
            ExitCode exitCode = ResultOps.ReturnCodeToExitCode(
                returnCode, exceptions);

            TraceOps.DebugTrace(String.Format(
                "ReturnCodeToExitCode: using exit code {0} based on " +
                "return code {1} for interpreter {2}", FormatOps.WrapOrNull(
                exitCode), FormatOps.WrapOrNull(returnCode),
                FormatOps.InterpreterNoThrow(interpreter)),
                typeof(ShellOps).Name, TracePriority.ShellDebug);

            return exitCode;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method gets the exit code currently associated with the
        /// specified interpreter.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter to query, which may be null.
        /// </param>
        /// <param name="exitCode">
        /// Upon return, this receives the exit code.  When there is no
        /// interpreter, this receives the failure exit code.
        /// </param>
        public static void GetExitCode(
            Interpreter interpreter,
            out ExitCode exitCode
            )
        {
            if (interpreter != null)
            {
                exitCode = interpreter.InternalExitCode;

                TraceOps.DebugTrace(String.Format(
                    "GetExitCode: using exit code {0} from interpreter {1}",
                    FormatOps.WrapOrNull(exitCode), FormatOps.InterpreterNoThrow(
                    interpreter)), typeof(ShellOps).Name,
                    TracePriority.ShellDebug);
            }
            else
            {
                exitCode = FailureExitCode(interpreter);
            }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method gets the exit code to use, based on the specified
        /// return code and the exit code currently associated with the
        /// interpreter.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter to query, which may be null.
        /// </param>
        /// <param name="returnCode">
        /// The return code to convert when the interpreter does not already
        /// have a non-success exit code.
        /// </param>
        /// <param name="exitCode">
        /// Upon return, this receives the exit code.
        /// </param>
        public static void GetExitCode(
            Interpreter interpreter,
            ReturnCode returnCode,
            out ExitCode exitCode
            )
        {
            if ((interpreter == null) ||
                (interpreter.InternalExitCode == SuccessExitCode()))
            {
                exitCode = ReturnCodeToExitCode(
                    interpreter, returnCode, true);
            }
            else
            {
                GetExitCode(interpreter, out exitCode);
            }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Interactive Loop Support Methods
        /// <summary>
        /// This method is the cross-application-domain entry point used to
        /// create an interpreter and enter its interactive loop using the
        /// command line arguments for the current process.
        /// </summary>
        public static void StartupInteractiveLoop() /* System.CrossAppDomainDelegate */
        {
            ReturnCode code;
            Result result; /* REUSED */

            result = null;

            using (Interpreter interpreter = Interpreter.Create(ref result))
            {
                if (interpreter != null)
                {
                    result = null;

                    code = Interpreter.InteractiveLoop(
                        null, Environment.GetCommandLineArgs(), ref result);
                }
                else
                {
                    code = ReturnCode.Error;
                }

                if (code != ReturnCode.Ok)
                {
                    TraceOps.DebugTrace(String.Format(
                        "StartupInteractiveLoop: code = {0}, result = {1}",
                        code, FormatOps.WrapOrNull(result)),
                        typeof(Utility).Name, TracePriority.ShellError);
                }
            }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method waits for the interactive loop associated with the
        /// specified thread to be unpaused, periodically rechecking until it is
        /// no longer paused.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter that owns the interactive loop.
        /// </param>
        /// <param name="appDomainId">
        /// The identifier of the application domain that owns the interactive
        /// loop.
        /// </param>
        /// <param name="threadId">
        /// The identifier of the thread that owns the interactive loop.
        /// </param>
        /// <param name="microseconds">
        /// The number of microseconds to wait between checks.
        /// </param>
        /// <param name="error">
        /// Upon failure, this contains an appropriate error message.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise, an error code.
        /// </returns>
        public static ReturnCode WaitPausedInteractiveLoop(
            Interpreter interpreter,
            int appDomainId,
            long threadId,
            long microseconds,
            ref Result error
            )
        {
            if (interpreter == null)
            {
                error = "invalid interpreter";
                return ReturnCode.Error;
            }

            try
            {
                ReturnCode code = ReturnCode.Ok;

                while (true)
                {
                    bool done = false;

                    code = interpreter.IsPausedInteractiveLoop(
                        appDomainId, threadId, ref done, ref error);

                    if (code != ReturnCode.Ok)
                        break;

                    if (done)
                        break;

                    bool timedOut = false;

                    code = EventOps.Wait(
                        interpreter, null, microseconds, null, true,
                        false, false, false, false, ref timedOut,
                        ref error);

                    if ((code != ReturnCode.Ok) && !timedOut)
                        break;
                }

                return code;
            }
            finally
            {
                ReturnCode unpauseCode;
                Result unpauseResult = null;

                unpauseCode = interpreter.UnpauseInteractiveLoop(
                    appDomainId, threadId, false, true, false, false,
                    ref unpauseResult);

                if (unpauseCode != ReturnCode.Ok)
                {
                    //
                    // HACK: It is possible to use Complain() here; however,
                    //       that makes things a bit less robust.
                    //
                    TraceOps.DebugTrace(String.Format(
                        "WaitPausedInteractiveLoop: code = {0}, result = {1}",
                        unpauseCode, unpauseResult), typeof(ShellOps).Name,
                        TracePriority.ShellError);
                }
            }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method normalizes the specified interactive command,
        /// optionally adding the interactive command and system command
        /// prefixes.
        /// </summary>
        /// <param name="command">
        /// The interactive command to normalize, which may be null.
        /// </param>
        /// <param name="usePrefix">
        /// Non-zero to add the interactive command prefixes when they are not
        /// already present.
        /// </param>
        /// <param name="normalCommand">
        /// Upon return, this receives the normalized command with the
        /// interactive command prefix, or null.
        /// </param>
        /// <param name="systemCommand">
        /// Upon return, this receives the normalized command with the
        /// interactive system command prefix, or null.
        /// </param>
        public static void NormalizeInteractiveCommand(
            string command,
            bool usePrefix,
            out string normalCommand,
            out string systemCommand
            )
        {
            if ((command != null) && usePrefix)
            {
                string prefix = InteractiveCommandPrefix;

                if (!String.IsNullOrEmpty(prefix) && !command.StartsWith(
                        prefix, SharedStringOps.SystemNoCaseComparisonType))
                {
                    normalCommand = prefix + command;
                }
                else
                {
                    normalCommand = null;
                }

                prefix = InteractiveSystemCommandPrefix;

                if (!String.IsNullOrEmpty(prefix) && !command.StartsWith(
                        prefix, SharedStringOps.SystemNoCaseComparisonType))
                {
                    systemCommand = prefix + command;
                }
                else
                {
                    systemCommand = null;
                }
            }
            else
            {
                //
                // NOTE: Do not use any interactive command prefix.
                //
                normalCommand = command;
                systemCommand = null;
            }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method determines whether the specified text looks like an
        /// interactive command or an interactive system command.
        /// </summary>
        /// <param name="text">
        /// The text to examine.
        /// </param>
        /// <returns>
        /// True if the text looks like an interactive command; otherwise,
        /// false.
        /// </returns>
        public static bool LooksLikeAnyInteractiveCommand(
            string text
            )
        {
            int nextIndex = Index.Invalid; /* NOT USED */

            return LooksLikeAnyInteractiveCommand(text, ref nextIndex);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method determines whether the specified text looks like an
        /// interactive command or an interactive system command, returning the
        /// index immediately following the matched prefix.
        /// </summary>
        /// <param name="text">
        /// The text to examine.
        /// </param>
        /// <param name="nextIndex">
        /// Upon success, this receives the index immediately following the
        /// matched prefix.
        /// </param>
        /// <returns>
        /// True if the text looks like an interactive command; otherwise,
        /// false.
        /// </returns>
        public static bool LooksLikeAnyInteractiveCommand(
            string text,
            ref int nextIndex
            )
        {
            return LooksLikeInteractiveCommand(
                text, InteractiveSystemCommandPrefix, ref nextIndex) ||
            LooksLikeInteractiveCommand(
                text, InteractiveCommandPrefix, ref nextIndex);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method determines whether the specified text begins with the
        /// specified interactive command prefix, returning the index
        /// immediately following it.
        /// </summary>
        /// <param name="text">
        /// The text to examine.
        /// </param>
        /// <param name="prefix">
        /// The interactive command prefix to look for.
        /// </param>
        /// <param name="nextIndex">
        /// Upon success, this receives the index immediately following the
        /// matched prefix.
        /// </param>
        /// <returns>
        /// True if the text begins with the specified prefix; otherwise,
        /// false.
        /// </returns>
        private static bool LooksLikeInteractiveCommand(
            string text,
            string prefix,
            ref int nextIndex
            )
        {
            if (!String.IsNullOrEmpty(text) &&
                !String.IsNullOrEmpty(prefix))
            {
                int prefixLength = prefix.Length;
                string localText = text.Trim();

                if (localText.StartsWith(prefix,
                        SharedStringOps.SystemNoCaseComparisonType))
                {
                    int localIndex = text.IndexOf(prefix,
                        SharedStringOps.SystemNoCaseComparisonType);

                    if (localIndex != Index.Invalid)
                        nextIndex = localIndex + prefixLength;

                    return true;
                }
            }

            return false;
        }

        ///////////////////////////////////////////////////////////////////////

        //
        // WARNING: For use by the InteractiveOps.CanExecuteCommand
        //          method only.
        //
        /// <summary>
        /// This method determines whether the specified text looks like an
        /// interactive system command.
        /// </summary>
        /// <param name="text">
        /// The text to examine.
        /// </param>
        /// <returns>
        /// True if the text looks like an interactive system command;
        /// otherwise, false.
        /// </returns>
        public static bool LooksLikeInteractiveSystemCommand(
            string text
            )
        {
            int nextIndex = Index.Invalid; /* NOT USED */

            return LooksLikeInteractiveCommand(
                text, InteractiveSystemCommandPrefix, ref nextIndex);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method determines whether the specified text begins with one
        /// of the interactive verbatim command prefixes, returning the
        /// replacement prefix and the index immediately following the matched
        /// prefix.
        /// </summary>
        /// <param name="text">
        /// The text to examine.
        /// </param>
        /// <param name="newPrefix">
        /// Upon success, this receives the replacement prefix associated with
        /// the matched verbatim prefix.
        /// </param>
        /// <param name="nextIndex">
        /// Upon success, this receives the index immediately following the
        /// matched prefix.
        /// </param>
        /// <returns>
        /// True if the text begins with a verbatim command prefix; otherwise,
        /// false.
        /// </returns>
        public static bool LooksLikeInteractiveVerbatimCommand(
            string text,
            ref string newPrefix,
            ref int nextIndex
            )
        {
            if (!String.IsNullOrEmpty(text))
            {
                string[] prefixes = InteractiveVerbatimCommandPrefixes;

                if (prefixes == null)
                    return false;

                int prefixesLength = prefixes.Length;

                if ((prefixesLength % 2) != 0)
                    return false;

                for (int index = 0; index < prefixesLength; index += 2)
                {
                    string oldPrefix = prefixes[index];

                    if (String.IsNullOrEmpty(oldPrefix))
                        continue;

                    int prefixLength = oldPrefix.Length;
                    string localText = text.Trim();

                    if (localText.StartsWith(oldPrefix,
                            SharedStringOps.SystemNoCaseComparisonType))
                    {
                        newPrefix = prefixes[index + 1];

                        int localIndex = text.IndexOf(oldPrefix,
                            SharedStringOps.SystemNoCaseComparisonType);

                        if (localIndex != Index.Invalid)
                            nextIndex = localIndex + prefixLength;
                        else
                            nextIndex = Index.Invalid;

                        return true;
                    }
                }
            }

            return false;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method removes the leading interactive command prefix and
        /// command name from the specified text, returning the remaining
        /// arguments.
        /// </summary>
        /// <param name="text">
        /// The text to strip the interactive command from.
        /// </param>
        /// <returns>
        /// The text with the leading interactive command removed, or the
        /// original text if it does not contain one.
        /// </returns>
        public static string StripInteractiveCommand(
            string text
            )
        {
            if (String.IsNullOrEmpty(text))
                return text;

            int nextIndex = Index.Invalid;

            if (!LooksLikeInteractiveCommand(text,
                    InteractiveSystemCommandPrefix, ref nextIndex))
            {
                return text;
            }
            else if (!LooksLikeInteractiveCommand(
                    text, InteractiveCommandPrefix, ref nextIndex))
            {
                return text;
            }

            int index = text.IndexOfAny(
                Characters.WhiteSpaceChars, nextIndex);

            if (index == Index.Invalid)
                return text;

            return text.Substring(index + 1).Trim();
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method builds the cancel flags used when resetting the
        /// cancellation state for the shell.
        /// </summary>
        /// <param name="force">
        /// Non-zero to also ignore any pending cancellation.
        /// </param>
        /// <returns>
        /// The cancel flags to use.
        /// </returns>
        public static CancelFlags GetResetCancelFlags(
            bool force
            )
        {
            CancelFlags cancelFlags = CancelFlags.Default;

            if (force)
                cancelFlags |= CancelFlags.IgnorePending;

            return cancelFlags | CancelFlags.ShellResetCancel;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method returns the name of the reserved procedure used to
        /// perform the specified update action.
        /// </summary>
        /// <param name="actionType">
        /// The update action to get the procedure name for.
        /// </param>
        /// <returns>
        /// The name of the procedure used to perform the specified action.
        /// </returns>
        private static string GetUpdateProcedureName(
            ActionType actionType
            )
        {
            if (actionType == ActionType.DownloadAndExtractUpdate)
                return DownloadAndExtractUpdate;

            return FetchUpdateScript;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method returns the name associated with the specified update
        /// data, falling back to the package name when none is present.
        /// </summary>
        /// <param name="updateData">
        /// The update data to query, which may be null.
        /// </param>
        /// <returns>
        /// The update name to use.
        /// </returns>
        private static string GetUpdateName(
            IUpdateData updateData
            )
        {
            if (updateData != null)
            {
                string name = updateData.Name;

                if (name != null)
                    return name;
            }

            return Vars.Package.Name;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method returns the directory to use for the specified update
        /// action.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter context to use, which may be null.
        /// </param>
        /// <param name="targetDirectory">
        /// The target directory associated with the update.
        /// </param>
        /// <param name="actionType">
        /// The update action to get the directory for.
        /// </param>
        /// <returns>
        /// The directory to use, or null if it could not be determined.
        /// </returns>
        private static string GetUpdateDirectory(
            Interpreter interpreter,
            string targetDirectory,
            ActionType actionType
            )
        {
            if (actionType == ActionType.FetchUpdate)
            {
                return PathOps.GetTempPath(interpreter);
            }
            else if (actionType == ActionType.DownloadAndExtractUpdate)
            {
                return targetDirectory;
            }
            else
            {
                return null;
            }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method returns the target directory associated with the update
        /// data for the specified interpreter.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter to query, which may be null.
        /// </param>
        /// <returns>
        /// The target update directory, or null if there is none.
        /// </returns>
        public static string GetUpdateDirectory(
            Interpreter interpreter
            )
        {
            if (interpreter == null)
                return null;

            IUpdateData updateData = interpreter.UpdateData;

            if (updateData == null)
                return null;

            return updateData.TargetDirectory;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method determines whether the specified update data contains
        /// all of the required information.
        /// </summary>
        /// <param name="updateData">
        /// The update data to validate, which may be null.
        /// </param>
        /// <returns>
        /// True if all required update data is present; otherwise, false.
        /// </returns>
        public static bool HaveRequiredUpdateData(
            IUpdateData updateData
            )
        {
            if (updateData == null)
                return false;

            if (updateData.TargetDirectory == null)
                return false;

            if (updateData.Uri == null)
                return false;

            if (updateData.PublicKeyToken == null)
                return false;

            if (updateData.Name == null)
                return false;

            //
            // HACK: Technically, this is optional.
            //
            // if (updateData.Culture == null)
            //     return false;

            if (updateData.PatchLevel == null)
                return false;

            //
            // HACK: Technically, this is optional.
            //
            // if (updateData.TimeStamp == null)
            //     return false;

            return true;
        }

        ///////////////////////////////////////////////////////////////////////

#if !NET_STANDARD_20 && THREADING
        /// <summary>
        /// This method asynchronously checks for an update to the core
        /// library, on behalf of the interactive loop, using a queued work
        /// item.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter to use for the update check.
        /// </param>
        /// <param name="loopData">
        /// The interactive loop data associated with the update check.
        /// </param>
        /// <param name="missing">
        /// Non-zero if the setup information is (apparently) missing, in which
        /// case the last-update-check marker is not updated.
        /// </param>
        public static void CheckForUpdate(
            Interpreter interpreter,
            IInteractiveLoopData loopData,
            bool missing
            )
        {
            ThreadOps.QueueUserWorkItem(delegate(object state)
            {
                //
                // HACK: Prior to evaluating the script that is used to
                //       check for updates, make sure to set the value
                //       (in the registry) to keep track of last update
                //       check.  This must be done before evaluating the
                //       script because the script itself may [exit].
                //
                // HACK: This is not done if there is (apparently?) no
                //       setup information present.
                //
                if (!missing)
                    SetupOps.MarkCheckCoreUpdatesNow();

                //
                // NOTE: Check for an update to the core library now,
                //       specifically the appropriate setup package.
                //
                ReturnCode code;
                int errorLine = 0;
                Result result = null;

                code = CheckForUpdate(
                    interpreter, new UpdateData((string)null,
                    ActionType.CheckForUpdate, ReleaseType.Setup,
                    UpdateType.Engine, false, true, true, true),
                    loopData.Debug, ref errorLine, ref result);

                TraceOps.DebugTrace(String.Format(
                    "CheckForUpdate: missing = {0}, code = {1}, " +
                    "errorLine = {2}, result = {3}", missing, code,
                    errorLine, FormatOps.WrapOrNull(result)),
                    typeof(ShellOps).Name, TracePriority.SetupDebug);

                //
                // BUGFIX: If the update checking script (somehow) set
                //         the exit flag for the interpreter, bail out
                //         before entering the actual interactive loop
                //         and without displaying any debugging related
                //         information.
                //
                Interpreter.CheckExit(interpreter, loopData);
            }, false);
        }
#endif

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method checks for an update using the specified update data,
        /// deriving the engine, substitution, event, and expression flags from
        /// the interpreter.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter to use for the update check.
        /// </param>
        /// <param name="updateData">
        /// The update data describing the update to check for.
        /// </param>
        /// <param name="debug">
        /// Non-zero to query the flags in debug mode.
        /// </param>
        /// <param name="errorLine">
        /// Upon failure, this receives the line number where the error
        /// occurred.
        /// </param>
        /// <param name="result">
        /// Upon success, this receives the result of the check; upon failure,
        /// this receives an appropriate error message.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise, an error code.
        /// </returns>
        public static ReturnCode CheckForUpdate(
            Interpreter interpreter,
            IUpdateData updateData,
            bool debug,
            ref int errorLine,
            ref Result result
            )
        {
            EngineFlags engineFlags;
            SubstitutionFlags substitutionFlags;
            EventFlags eventFlags;
            ExpressionFlags expressionFlags;

            Interpreter.QueryFlagsNoThrow(
                interpreter, debug, out engineFlags, out substitutionFlags,
                out eventFlags, out expressionFlags);

            return CheckForUpdate(
                interpreter, updateData, engineFlags, substitutionFlags,
                eventFlags, expressionFlags, ref errorLine, ref result);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method checks for an update using the specified update data,
        /// evaluating the appropriate reserved scripts and optionally fetching
        /// or downloading the update.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter to use for the update check.
        /// </param>
        /// <param name="updateData">
        /// The update data describing the update to check for.
        /// </param>
        /// <param name="engineFlags">
        /// The engine flags to use when evaluating the scripts.
        /// </param>
        /// <param name="substitutionFlags">
        /// The substitution flags to use when evaluating the scripts.
        /// </param>
        /// <param name="eventFlags">
        /// The event flags to use when evaluating the scripts.
        /// </param>
        /// <param name="expressionFlags">
        /// The expression flags to use when evaluating the scripts.
        /// </param>
        /// <param name="errorLine">
        /// Upon failure, this receives the line number where the error
        /// occurred.
        /// </param>
        /// <param name="result">
        /// Upon success, this receives the result of the check; upon failure,
        /// this receives an appropriate error message.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise, an error code.
        /// </returns>
        public static ReturnCode CheckForUpdate(
            Interpreter interpreter,
            IUpdateData updateData,
            EngineFlags engineFlags,
            SubstitutionFlags substitutionFlags,
            EventFlags eventFlags,
            ExpressionFlags expressionFlags,
            ref int errorLine,
            ref Result result
            )
        {
            if (updateData == null)
            {
                result = "invalid update data";
                return ReturnCode.Error;
            }

            ReturnCode code;
            UpdateType updateType = updateData.UpdateType;
            bool automatic = updateData.Automatic;

            if (updateType == UpdateType.Engine)
            {
                //
                // NOTE: Evaluate the script used to check for
                //       updates to the script engine.  If this
                //       procedure has been redefined, this may
                //       not actually do anything, which would
                //       be fine.
                //
                errorLine = 0;

                code = Engine.EvaluateScript(interpreter,
                    StringList.MakeList(CheckForEngineScript,
                    updateData.WantScripts, updateData.Quiet,
                    updateData.Prompt, automatic), engineFlags,
                    substitutionFlags, eventFlags, expressionFlags,
                    ref result, ref errorLine);
            }
            else if (updateType == UpdateType.Plugin)
            {
                //
                // NOTE: Evaluate the script used to check for
                //       updates to a binary plugin.  If this
                //       procedure has been redefined, this may
                //       not actually do anything, which would
                //       be fine.
                //
                errorLine = 0;

                code = Engine.EvaluateScript(interpreter,
                    StringList.MakeList(CheckForPluginScript,
                    updateData.Uri, ArrayOps.ToHexadecimalString(
                    updateData.PublicKeyToken), updateData.Name,
                    updateData.Culture, updateData.PatchLevel,
                    FormatOps.UpdateDateTime(updateData.TimeStamp),
                    updateData.WantScripts, updateData.Quiet,
                    updateData.Prompt, automatic), engineFlags,
                    substitutionFlags, eventFlags, expressionFlags,
                    ref result, ref errorLine);
            }
            else
            {
                result = String.Format(
                    "unsupported update type {0}", updateType);

                code = ReturnCode.Error;
            }

            //
            // NOTE: Evaluate the script we use to fetch an update to the
            //       script engine, if necessary.  If the proc has been
            //       redefined, this may not actually do anything.
            //
            if (code == ReturnCode.Ok)
            {
                //
                // NOTE: Attempt to parse the result of the check-for-update
                //       script as a list.
                //
                StringList list = null;

                code = ParserOps<string>.SplitList(
                    interpreter, result, 0, Length.Invalid, true, ref list,
                    ref result);

                if (code == ReturnCode.Ok)
                {
                    //
                    // NOTE: We know the result was successfully converted
                    //       into a list; therefore, grab the count now as
                    //       we will always need it below.
                    //
                    int count = list.Count;

                    //
                    // NOTE: If specified action is greater than zero, we
                    //       need to actively interpret the result and then
                    //       fetch the update if necessary; otherwise, we
                    //       do nothing but simply reporting the result of
                    //       the check-for-update script.
                    //
                    ActionType actionType = updateData.ActionType;

                    if ((actionType != ActionType.None) &&
                        (actionType != ActionType.CheckForUpdate))
                    {
                        //
                        // NOTE: If the result from the check-for-update
                        //       script is a list containing more than
                        //       one element, then another operation of
                        //       some kind (e.g. fetch, download, etc)
                        //       must be necessary.
                        //
                        if (count > 1)
                        {
                            string actionScript = null;

                            if (actionType == ActionType.RunUpdateAndExit)
                            {
                                //
                                // NOTE: This action is simple, it just
                                //       runs the external updater tool,
                                //       passing one boolean argument to
                                //       indicate if the process should
                                //       be fully automatic.
                                //
                                actionScript = StringList.MakeList(
                                    RunUpdateAndExitScript, automatic);
                            }
                            else if ((actionType == ActionType.FetchUpdate) ||
                                (actionType == ActionType.DownloadAndExtractUpdate))
                            {
                                //
                                // NOTE: Parse the second element of the
                                //       list as a nested list containing
                                //       [most of] the arguments to pass
                                //       to the fetch-an-update script.
                                //
                                string procedureName = GetUpdateProcedureName(
                                    actionType);

                                if (procedureName != null)
                                {
                                    //
                                    // NOTE: The first argument here is the
                                    //       base URI.  The second argument
                                    //       is the patch level.  The third
                                    //       (and final) argument is the
                                    //       temporary directory to be used
                                    //       to contain downloaded files.
                                    //
                                    string directory = PathOps.GetUnixPath(
                                        GetUpdateDirectory(interpreter,
                                            updateData.TargetDirectory,
                                            actionType));

                                    actionScript = StringList.MakeList(
                                        procedureName,
                                        updateData.ReleaseType,
                                        ArrayOps.ToHexadecimalString(
                                            updateData.PublicKeyToken),
                                        GetUpdateName(updateData),
                                        updateData.Culture,
                                        updateData.PatchLevel,
                                        directory, list[1], null);
                                }
                                else
                                {
                                    result = String.Format(
                                        "missing {0} procedure", actionType);

                                    code = ReturnCode.Error;
                                }
                            }
                            else
                            {
                                result = String.Format(
                                    "unsupported action type {0}", actionType);

                                code = ReturnCode.Error;
                            }

                            if ((code == ReturnCode.Ok) &&
                                (actionScript != null))
                            {
                                IUpdateData savedUpdateData = null;

                                if (interpreter != null)
                                {
                                    interpreter.PushUpdateData(
                                        updateData, ref savedUpdateData);
                                }

                                try
                                {
                                    errorLine = 0;

                                    code = Engine.EvaluateScript(
                                        interpreter, actionScript,
                                        engineFlags, substitutionFlags,
                                        eventFlags, expressionFlags,
                                        ref result, ref errorLine);

                                    //
                                    // NOTE: To form the final result, combine
                                    //       the check-for-update script result
                                    //       with the fetch script result with
                                    //       a line-ending in between.
                                    //
                                    result = String.Format("{0}{1}{2}",
                                        list[0], Environment.NewLine,
                                        result);
                                }
                                finally
                                {
                                    if (interpreter != null)
                                    {
                                        interpreter.PopUpdateData(
                                            ref savedUpdateData);
                                    }
                                }
                            }
                        }
                        else if (count > 0)
                        {
                            //
                            // NOTE: Return informational message itself as
                            //       the result.
                            //
                            result = list[0];
                        }
                        else
                        {
                            //
                            // NOTE: Return a generic error message because
                            //       the result was malformed.  This is now
                            //       considered an error.
                            //
                            result = "malformed check-for-update script result (2)";
                            code = ReturnCode.Error;
                        }
                    }
                    else if (count > 0)
                    {
                        //
                        // NOTE: Return informational message itself as the
                        //       result.
                        //
                        result = list[0];
                    }
                    else
                    {
                        //
                        // NOTE: Return a generic error message because the
                        //       result was malformed.  This is now considered
                        //       an error.
                        //
                        result = "malformed check-for-update script result (1)";
                        code = ReturnCode.Error;
                    }
                }
            }

            return code;
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Interactive Loop Thread Support
        /// <summary>
        /// This method is the thread entry point used to run an interactive
        /// loop, using the interpreter and interactive loop data supplied as a
        /// pair.
        /// </summary>
        /// <param name="obj">
        /// The thread argument, which must be a pair containing the interpreter
        /// and the interactive loop data.
        /// </param>
        private static void InteractiveLoopThreadStart(
            object obj
            )
        {
            try
            {
                IAnyPair<Interpreter, IInteractiveLoopData> anyPair =
                    obj as IAnyPair<Interpreter, IInteractiveLoopData>;

                if (anyPair == null)
                {
                    DebugOps.Complain(ReturnCode.Error,
                        "thread argument is not a pair");

                    return;
                }

                TraceOps.DebugTrace(String.Format(
                    "InteractiveLoopThreadStart: entered, " +
                    "interpreter = {0}, loopData = {1}",
                    FormatOps.InterpreterNoThrow(anyPair.X),
                    FormatOps.InteractiveLoopData(anyPair.Y)),
                    typeof(ShellOps).Name,
                    TracePriority.ThreadDebug);

                ReturnCode code = ReturnCode.Ok;
                Result result = null;

                try
                {
                    code = Interpreter.InteractiveLoop(
                        anyPair.X, anyPair.Y, ref result);
                }
                catch (Exception e)
                {
                    result = e;
                    code = ReturnCode.Error;
                }
                finally
                {
                    TraceOps.DebugTrace(String.Format(
                        "InteractiveLoopThreadStart: exited, " +
                        "interpreter = {0}, loopData = {1}, " +
                        "code = {2}, result = {3}",
                        FormatOps.InterpreterNoThrow(anyPair.X),
                        FormatOps.InteractiveLoopData(anyPair.Y),
                        code, FormatOps.WrapOrNull(true, true, result)),
                        typeof(ShellOps).Name,
                        TracePriority.ThreadDebug);
                }
            }
            catch (Exception e)
            {
                TraceOps.DebugTrace(
                    e, typeof(ShellOps).Name,
                    TracePriority.ThreadError);
            }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method creates, and optionally starts, a thread that runs an
        /// interactive loop for the specified interpreter.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter to run the interactive loop for.
        /// </param>
        /// <param name="loopData">
        /// The interactive loop data to use.
        /// </param>
        /// <param name="start">
        /// Non-zero to start the thread before returning.
        /// </param>
        /// <param name="error">
        /// Upon failure, this contains an appropriate error message.
        /// </param>
        /// <returns>
        /// The created thread, or null if it could not be created.
        /// </returns>
        public static Thread CreateInteractiveLoopThread(
            Interpreter interpreter,
            IInteractiveLoopData loopData,
            bool start,
            ref Result error
            ) /* ENTRY-POINT, THREAD-SAFE, RE-ENTRANT */
        {
            try
            {
                Thread thread = Engine.CreateThread(
                    interpreter, InteractiveLoopThreadStart, 0,
                    true, false, true);

                if (thread != null)
                {
                    thread.Name = String.Format(
                        "interactiveLoopThread: {0}",
                        FormatOps.InterpreterNoThrow(interpreter));

                    if (start)
                    {
                        IAnyPair<Interpreter, IInteractiveLoopData> anyPair =
                            new AnyPair<Interpreter, IInteractiveLoopData>(
                                interpreter, loopData);

                        thread.Start(anyPair); /* throw */
                    }

                    return thread;
                }
                else
                {
                    error = "failed to create engine thread";
                }
            }
            catch (Exception e)
            {
                TraceOps.DebugTrace(
                    e, typeof(ShellOps).Name,
                    TracePriority.ThreadError);

                error = e;
            }

            return null;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method stops the specified interactive loop thread by
        /// signaling its done event, canceling its host input, and waiting for
        /// it to exit.
        /// </summary>
        /// <param name="thread">
        /// The interactive loop thread to stop.
        /// </param>
        /// <param name="interpreter">
        /// The interpreter that owns the interactive loop.
        /// </param>
        /// <param name="force">
        /// Non-zero to forcibly cancel any pending host input.
        /// </param>
        /// <param name="error">
        /// Upon failure, this contains an appropriate error message.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise, an error code.
        /// </returns>
        public static ReturnCode StopInteractiveLoopThread(
            Thread thread,
            Interpreter interpreter,
            bool force,
            ref Result error
            ) /* ENTRY-POINT, THREAD-SAFE */
        {
            ///////////////////////////////////////////////////////////////////
            // PHASE 0: Parameter validation.
            ///////////////////////////////////////////////////////////////////

            if (!ThreadOps.IsAlive(thread))
            {
                error = String.Format(
                    "interactive loop thread {0} is not alive",
                    FormatOps.ThreadIdOrNull(thread));

                return ReturnCode.Error;
            }

            if (interpreter == null)
            {
                error = "invalid interpreter";
                return ReturnCode.Error;
            }

            ///////////////////////////////////////////////////////////////////
            // PHASE 1: Grab event and host.
            ///////////////////////////////////////////////////////////////////

            EventWaitHandle @event;
            IDebugHost debugHost;

            lock (interpreter.InternalSyncRoot) /* TRANSACTIONAL */
            {
                if (interpreter.Disposed)
                {
                    error = "interpreter is disposed";
                    return ReturnCode.Error;
                }

                @event = interpreter.InteractiveLoopDoneEvent;

                debugHost = interpreter.GetInteractiveHost(
                    typeof(IDebugHost)) as IDebugHost;
            }

            ///////////////////////////////////////////////////////////////////
            // PHASE 2: Signal the interactive loop.
            ///////////////////////////////////////////////////////////////////

            if (@event == null)
            {
                error = "interactive loop done event not available";
                return ReturnCode.Error;
            }

            if (!ThreadOps.SetEvent(@event))
            {
                error = "failed to signal interactive loop done";
                return ReturnCode.Error;
            }

            ///////////////////////////////////////////////////////////////////
            // PHASE 3: Cancel interpreter host input.
            ///////////////////////////////////////////////////////////////////

            if (debugHost == null)
            {
                error = "interpreter host not available";
                return ReturnCode.Error;
            }

            try
            {
                if (debugHost.Cancel(force, ref error) != ReturnCode.Ok)
                    return ReturnCode.Error;
            }
            catch (Exception e)
            {
                error = e;
                return ReturnCode.Error;
            }

            ///////////////////////////////////////////////////////////////////
            // PHASE 4: Wait for interactive loop thread to exit.
            ///////////////////////////////////////////////////////////////////

            try
            {
                if (!thread.Join(ThreadOps.DefaultJoinTimeout))
                {
                    error = "timeout waiting for interactive loop thread";
                    return ReturnCode.Error;
                }
            }
            catch (Exception e)
            {
                error = e;
                return ReturnCode.Error;
            }

            return ReturnCode.Ok;
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Shell Thread Support
        /// <summary>
        /// This method is the thread entry point used to run the Eagle shell,
        /// using the command line arguments supplied as the thread argument.
        /// </summary>
        /// <param name="obj">
        /// The thread argument, which must be an enumerable of command line
        /// argument strings.
        /// </param>
        private static void ShellMainThreadStart(
            object obj
            )
        {
            try
            {
                IEnumerable<string> args = obj as IEnumerable<string>;

                TraceOps.DebugTrace(String.Format(
                    "ShellMainThreadStart: entered, args = {0}",
                    FormatOps.WrapArgumentsOrNull(true, true, args)),
                    typeof(ShellOps).Name,
                    TracePriority.ThreadDebug);

                ExitCode exitCode = Interpreter.ShellMain(args);

                TraceOps.DebugTrace(String.Format(
                    "ShellMainThreadStart: exited, args = {0}, " +
                    "exitCode = {1}",
                    FormatOps.WrapArgumentsOrNull(true, true, args),
                    exitCode),
                    typeof(ShellOps).Name,
                    TracePriority.ThreadDebug);
            }
            catch (Exception e)
            {
                TraceOps.DebugTrace(
                    e, typeof(ShellOps).Name,
                    TracePriority.ThreadError);
            }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method creates, and optionally starts, a thread that runs the
        /// Eagle shell using the specified command line arguments.
        /// </summary>
        /// <param name="args">
        /// The command line arguments to pass to the shell.
        /// </param>
        /// <param name="start">
        /// Non-zero to start the thread before returning.
        /// </param>
        /// <returns>
        /// The created thread, or null if it could not be created.
        /// </returns>
        public static Thread CreateShellMainThread(
            IEnumerable<string> args,
            bool start
            ) /* ENTRY-POINT, THREAD-SAFE, RE-ENTRANT */
        {
            try
            {
                Thread shellMainThread = Engine.CreateThread(
                    ShellMainThreadStart, 0, true, false, true);

                if (shellMainThread != null)
                {
                    shellMainThread.Name = "shellMainThread";

                    if (start)
                        shellMainThread.Start(args);

                    return shellMainThread;
                }
            }
            catch (Exception e)
            {
                TraceOps.DebugTrace(
                    e, typeof(ShellOps).Name,
                    TracePriority.ThreadError);
            }

            return null;
        }
        #endregion
    }
}
