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
    [ObjectId("d9374375-f3bb-402f-8c43-354168741995")]
    internal static class ShellOps
    {
        #region Private Constants
        #region Interactive Command Prefix
        internal static readonly char InteractiveCommandPrefixChar =
            Characters.NumberSign;

        internal static readonly string InteractiveCommandPrefix =
            InteractiveCommandPrefixChar.ToString();

        internal static readonly string InteractiveSystemCommandPrefix =
            StringOps.StrRepeat(2, InteractiveCommandPrefix);

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

        private static readonly string[] InteractiveVerbatimCommandPrefixes = {
            StringOps.StrRepeat(4, InteractiveCommandPrefix),
            InteractiveSystemCommandPrefix,
            StringOps.StrRepeat(3, InteractiveCommandPrefix),
            InteractiveCommandPrefix
        };

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
        internal static long PauseMicroseconds = 2000000; /* 2 seconds */
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Script Update Checking
        //
        // NOTE: These procedure names are all RESERVED; however, they may
        //       legally be redefined to do nothing.
        //
        private const string CheckForEngineScript = "checkForEngine";
        private const string CheckForPluginScript = "checkForPlugin";
        private const string FetchUpdateScript = "fetchUpdate";
        private const string RunUpdateAndExitScript = "runUpdateAndExit";
        private const string DownloadAndExtractUpdate = "downloadAndExtractUpdate";
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region AppSettings Support
        private static string ArgumentSettingPrefix = typeof(ShellOps).Name;
        private const string ArgumentCountSettingFormat = "{0}ArgumentCount";
        private const string ArgumentStringSettingFormat = "{0}Argument{1}String";
        private const string ArgumentListSettingFormat = "{0}Argument{1}List";
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Logging & Tracing
#if TEST
        //
        // HACK: This is purposely not read-only.
        //
        private static string MainListenerName =
            typeof(Interpreter).FullName + ".ShellMain";
#endif
        #endregion
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Shell Support Methods
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

        public static bool ShouldUseArgumentsFileNames(
            IList<string> argv
            )
        {
            return !HaveArgumentValue(
                argv, CommandLineOption.NoArgumentsFileNames);
        }

        ///////////////////////////////////////////////////////////////////////

        public static bool ShouldLockHostArguments(
            IList<string> argv
            )
        {
            return HaveArgumentValue(
                argv, CommandLineOption.LockHostArguments);
        }

        ///////////////////////////////////////////////////////////////////////

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

        public static StringList GetArgumentsFileNames(
            string fileName
            )
        {
            return PathOps.GetOverrideFileNames(
                fileName, FileExtension.Arguments, true, false);
        }

        ///////////////////////////////////////////////////////////////////////

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

        public static void CommitToArguments(
            IList<string> newArgv,
            int count,
            bool append,
            ref IList<string> argv
            )
        {
            //
            // NOTE: *WARNING* This assumes that the arguments that need
            //       to be removed are at the start of the list provided by
            //       the caller; the ShellMainCore method can guarantee that
            //       will be the case and other callers should do so as well.
            //
            while ((argv != null) && (count-- > 0))
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
            // NOTE: The count may already be zero at this point (if the
            //       above loop was actually fully executed); however, we
            //       must be 100% sure that it is zero beyond this point
            //       (for the loop below).
            //
            count = 0;

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
            foreach (string arg in newArgv)
            {
                if (append)
                    argv.Add(arg);
                else
                    argv.Insert(count++, arg);
            }
        }

        ///////////////////////////////////////////////////////////////////////

        private static ReturnCode ReadArgumentsFromTextReader(
            Interpreter interpreter,
            TextReader textReader,
            int count,
            bool append,
            ref IList<string> argv,
            ref Result error
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

            CommitToArguments(newArgv, count, append, ref argv);
            return ReturnCode.Ok;
        }

        ///////////////////////////////////////////////////////////////////////

        public static ReturnCode ReadArgumentsFromHost(
            Interpreter interpreter,
            StringList argvFileNames,
            Encoding encoding,
            int count,
            bool append,
            bool strict,
            ref string argvFileName,
            ref IList<string> argv,
            ref bool readArgv,
            ref ResultList errors
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
                        interpreter, textReader, count, append, ref argv,
                        ref localResult);

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
                        //       know that the command line arguemnts,
                        //       if any, were read from the text reader.
                        //
                        readArgv = true;
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

            if (strict)
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

        public static ReturnCode ReadArgumentsFromFile(
            Interpreter interpreter,
            Encoding encoding,
            string fileName,
            int count,
            bool console,
            bool append,
            ref IList<string> argv,
            ref Result error
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
                    interpreter, textReader, count, append, ref argv,
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

        public static bool ShouldUseArgumentsAppSettings(
            IList<string> argv
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

        public static ReturnCode ReadArgumentsFromAppSettings(
            Interpreter interpreter,
            int count,
            bool append,
            ref IList<string> argv,
            ref Result error
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

            CommitToArguments(newArgv, count, append, ref argv);
            return ReturnCode.Ok;
        }

        ///////////////////////////////////////////////////////////////////////

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

        public static ReturnCode UnknownArgument(
            Interpreter interpreter,          /* in */
            IInteractiveHost interactiveHost, /* in */
            IClientData clientData,           /* in */
            IShellCallbackData callbackData,  /* in */
            int count,                        /* in */
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
                    code = unknownArgumentCallback(
                        interpreter, interactiveHost, clientData,
                        count, arg, whatIf, ref argv, ref result);
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

        private static bool BeginHighContrastColors(
            IColorHost colorHost,
            ref ConsoleColor savedForegroundColor,
            ref ConsoleColor savedBackgroundColor
            )
        {
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

        private static bool EndHighContrastColors(
            IColorHost colorHost,
            ref ConsoleColor savedForegroundColor,
            ref ConsoleColor savedBackgroundColor
            )
        {
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

        private static void WriteCore(
            IInteractiveHost interactiveHost,
            string value
            )
        {
            if (interactiveHost != null)
            {
                IColorHost colorHost = interactiveHost as IColorHost;

                ConsoleColor savedForegroundColor = _ConsoleColor.None;
                ConsoleColor savedBackgroundColor = _ConsoleColor.None;

                BeginHighContrastColors(
                    colorHost, ref savedForegroundColor,
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
                        colorHost, ref savedForegroundColor,
                        ref savedBackgroundColor);
                }
            }

#if CONSOLE
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
#endif

            /* NO RESULT */
            DebugOps.WriteWithoutFail(
                interactiveHost as IDebugHost, value);
        }

        ///////////////////////////////////////////////////////////////////////

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

        public static void WritePrompt(
            IInteractiveHost interactiveHost,
            string value
            )
        {
            WriteCore(interactiveHost, value);
        }

        ///////////////////////////////////////////////////////////////////////

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

            ShellMainCoreError(interpreter, savedArg, arg, localCode,
                localResult, false, ref argv, ref interactiveHost,
                ref quiet, ref result);
        }

        ///////////////////////////////////////////////////////////////////////

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
            ShellMainCoreError(interpreter, savedArg, arg, ReturnCode.Error,
                localResult, whatIf, ref argv, ref interactiveHost, ref quiet,
                ref result);
        }

        ///////////////////////////////////////////////////////////////////////

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

        public static ExitCode SuccessExitCode()
        {
            return ResultOps.SuccessExitCode();
        }

        ///////////////////////////////////////////////////////////////////////

        public static ExitCode FailureExitCode(
            Interpreter interpreter
            )
        {
            ExitCode exitCode = ResultOps.FailureExitCode();

            TraceOps.DebugTrace(String.Format(
                "FailureExitCode: using exit code {0} for interpreter {1}",
                FormatOps.WrapOrNull(exitCode), FormatOps.InterpreterNoThrow(
                interpreter)), typeof(ShellOps).Name, TracePriority.ShellDebug2);

            return exitCode;
        }

        ///////////////////////////////////////////////////////////////////////

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
                typeof(ShellOps).Name, TracePriority.ShellDebug2);

            return exitCode;
        }

        ///////////////////////////////////////////////////////////////////////

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
                    TracePriority.ShellDebug2);
            }
            else
            {
                exitCode = FailureExitCode(interpreter);
            }
        }

        ///////////////////////////////////////////////////////////////////////

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
                        false, false, false, ref timedOut, ref error);

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

        public static bool LooksLikeAnyInteractiveCommand(
            string text
            )
        {
            int nextIndex = 0;

            return LooksLikeAnyInteractiveCommand(text, ref nextIndex);
        }

        ///////////////////////////////////////////////////////////////////////

        public static bool LooksLikeAnyInteractiveCommand(
            string text,
            ref int nextIndex
            )
        {
            return LooksLikeInteractiveCommand(text, ref nextIndex) ||
                LooksLikeInteractiveSystemCommand(text, ref nextIndex);
        }

        ///////////////////////////////////////////////////////////////////////

        public static bool LooksLikeInteractiveCommand(
            string text
            )
        {
            int nextIndex = 0;

            return LooksLikeInteractiveCommand(text, ref nextIndex);
        }

        ///////////////////////////////////////////////////////////////////////

        private static bool LooksLikeInteractiveCommand(
            string text,
            ref int nextIndex
            )
        {
            if (!String.IsNullOrEmpty(text))
            {
                string prefix = InteractiveCommandPrefix;

                if (!String.IsNullOrEmpty(prefix))
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
            }

            return false;
        }

        ///////////////////////////////////////////////////////////////////////

        public static bool LooksLikeInteractiveSystemCommand(
            string text
            )
        {
            int nextIndex = 0;

            return LooksLikeInteractiveSystemCommand(text, ref nextIndex);
        }

        ///////////////////////////////////////////////////////////////////////

        private static bool LooksLikeInteractiveSystemCommand(
            string text,
            ref int nextIndex
            )
        {
            if (!String.IsNullOrEmpty(text))
            {
                string prefix = InteractiveSystemCommandPrefix;

                if (!String.IsNullOrEmpty(prefix))
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
            }

            return false;
        }

        ///////////////////////////////////////////////////////////////////////

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

        public static string StripInteractiveCommand(
            string text
            )
        {
            if (String.IsNullOrEmpty(text))
                return text;

            int nextIndex = 0;

            if (!LooksLikeInteractiveSystemCommand(text, ref nextIndex))
                return text;
            else if (!LooksLikeInteractiveCommand(text, ref nextIndex))
                return text;

            int index = text.IndexOfAny(Characters.WhiteSpaceChars, nextIndex);

            if (index == Index.Invalid)
                return text;

            return text.Substring(index + 1).Trim();
        }

        ///////////////////////////////////////////////////////////////////////

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

        private static string GetUpdateProcedureName(
            ActionType actionType
            )
        {
            if (actionType == ActionType.DownloadAndExtractUpdate)
                return DownloadAndExtractUpdate;

            return FetchUpdateScript;
        }

        ///////////////////////////////////////////////////////////////////////

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
            });
        }
#endif

        ///////////////////////////////////////////////////////////////////////

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
