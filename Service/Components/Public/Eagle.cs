/*
 * Eagle.cs --
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
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Reflection;
using System.Web.Services;
using Eagle._Attributes;
using Eagle._Components.Public;

namespace Eagle._Services
{
    [WebService(
        Name = Eagle.Name,
        Description = Eagle.Description,
        Namespace = Eagle.Namespace)]
    [ObjectId("36ce542c-dc1c-4a9a-affa-ce22aebb1173")]
    public sealed class Eagle : IEagle
    {
        #region Private Constants
        private const string Name = "Eagle Web Service";

        private const string Description =
            "This service is used to handle dynamic content (i.e. expressions, " +
            "scripts, and/or text blocks) for the Tcl and/or Eagle languages.";

        private const string Namespace = "https://eagle.to/";

        ///////////////////////////////////////////////////////////////////////

        private static readonly Assembly assembly =
            Assembly.GetExecutingAssembly();
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Default Settings
        //
        // NOTE: By default, no console.
        //
        private static readonly bool DefaultConsole = false;

        ///////////////////////////////////////////////////////////////////////

        //
        // NOTE: By default:
        //
        //       1. We want to initialize the script library.
        //       2. We want to throw an exception if disposed objects are
        //          accessed.
        //       3. We want to have only directories that actually exist in
        //          the auto-path.
        //       4. We want to provide a "safe" subset of commands.
        //
        private static readonly CreateFlags DefaultCreateFlags =
            CreateFlags.SafeEmbeddedUse & ~CreateFlags.ThrowOnError;

        ///////////////////////////////////////////////////////////////////////

        //
        // NOTE: By default:
        //
        //       1. We do not want to change the console title.
        //       2. We do not want to change the console icon.
        //       3. We do not want to intercept the Ctrl-C keypress.
        //
        private static readonly HostCreateFlags DefaultHostCreateFlags =
            HostCreateFlags.SafeEmbeddedUse;

        ///////////////////////////////////////////////////////////////////////

        //
        // NOTE: By default:
        //
        //       1. We want no special engine flags.
        //
        private static readonly EngineFlags DefaultEngineFlags =
            EngineFlags.None;

        ///////////////////////////////////////////////////////////////////////

        //
        // NOTE: By default:
        //
        //       1. We want all substitution types to be performed.
        //
        private static readonly SubstitutionFlags DefaultSubstitutionFlags =
            SubstitutionFlags.Default;

        ///////////////////////////////////////////////////////////////////////

        //
        // NOTE: By default:
        //
        //       1. We want to handle events targeted to the engine.
        //
        private static readonly EventFlags DefaultEventFlags =
            EventFlags.Default;

        ///////////////////////////////////////////////////////////////////////

        //
        // NOTE: By default:
        //
        //       1. We want all expression types to be performed.
        //
        private static readonly ExpressionFlags DefaultExpressionFlags =
            ExpressionFlags.Default;
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Public Constructors
        public Eagle()
        {
            // do nothing.
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Private Settings Management Class
        [ObjectId("1fca91ae-46dd-4e13-98d6-c21815c8131c")]
        private static class Settings
        {
            #region Setting Names
            public static readonly string EagleLibrary = EnvVars.EagleLibrary;
            public static readonly string SetupScript = "SetupScript";
            public static readonly string LibraryPath = "LibraryPath";
            public static readonly string CreateFlags = "CreateFlags";
            public static readonly string HostCreateFlags = "HostCreateFlags";
            public static readonly string EngineFlags = "EngineFlags";
            public static readonly string SubstitutionFlags = "SubstitutionFlags";
            public static readonly string EventFlags = "EventFlags";
            public static readonly string ExpressionFlags = "ExpressionFlags";
            public static readonly string TrustedSetup = "TrustedSetup";
            public static readonly string NeedConsole = "NeedConsole";
            #endregion

            ///////////////////////////////////////////////////////////////////

            public static bool GetTrustedSetup()
            {
                try
                {
                    if (Utility.GetEnvironmentVariable(
                            TrustedSetup, true, false) != null)
                    {
                        return true;
                    }
                    else
                    {
                        string value = Utility.GetAppSetting(TrustedSetup);

                        if (!String.IsNullOrEmpty(value))
                        {
                            bool result = false;

                            return bool.TryParse(value, out result) && result;
                        }
                    }
                }
                catch
                {
                    // do nothing.
                }

                return false;
            }

            ///////////////////////////////////////////////////////////////////

            public static bool GetNeedConsole()
            {
                return GetNeedConsole(DefaultConsole);
            }

            ///////////////////////////////////////////////////////////////////

            private static bool GetNeedConsole(
                bool @default
                )
            {
                //
                // HACK: By default, assume that a console-based host is not
                //       available.  Then, attempt to check and see if the
                //       user believes that one is available.  We use this
                //       very clumsy method because ASP.NET does not seem to
                //       expose an easy way for us to determine if we have a
                //       console-like host available to output diagnostic
                //       [and other] information to.
                //
                try
                {
                    if (@default)
                    {
                        if (Utility.GetEnvironmentVariable(
                                EnvVars.NoConsole, true, false) != null)
                        {
                            return false;
                        }
                    }
                    else
                    {
                        if ((Utility.GetEnvironmentVariable(
                                NeedConsole, true, false) != null) ||
                            (Utility.GetEnvironmentVariable(
                                EnvVars.Console, true, false) != null))
                        {
                            return true;
                        }
                    }

                    string value = Utility.GetAppSetting(NeedConsole);

                    if (!String.IsNullOrEmpty(value))
                    {
                        bool result = false;

                        return bool.TryParse(value, out result) && result;
                    }
                }
                catch
                {
                    // do nothing.
                }

                return @default;
            }

            ///////////////////////////////////////////////////////////////////

            public static CreateFlags GetCreateFlags()
            {
                return GetCreateFlags(DefaultCreateFlags);
            }

            ///////////////////////////////////////////////////////////////////

            private static CreateFlags GetCreateFlags(
                CreateFlags @default
                )
            {
                try
                {
                    string value = Utility.GetEnvironmentVariable(
                        CreateFlags, true, true);

                    if (String.IsNullOrEmpty(value))
                        value = Utility.GetAppSetting(CreateFlags);

                    //
                    // NOTE: Were we able to get the value from somewhere?
                    //
                    if (!String.IsNullOrEmpty(value))
                    {
                        Result error = null;

                        object enumValue = Utility.TryParseFlagsEnum(
                            null, typeof(CreateFlags), @default.ToString(),
                            value, null, true, true, true, ref error);

                        if (enumValue is CreateFlags)
                            return (CreateFlags)enumValue;
                    }
                }
                catch
                {
                    // do nothing.
                }

                return @default;
            }

            ///////////////////////////////////////////////////////////////////

            public static HostCreateFlags GetHostCreateFlags()
            {
                return GetHostCreateFlags(DefaultHostCreateFlags);
            }

            ///////////////////////////////////////////////////////////////////

            private static HostCreateFlags GetHostCreateFlags(
                HostCreateFlags @default
                )
            {
                try
                {
                    string value = Utility.GetEnvironmentVariable(
                        HostCreateFlags, true, true);

                    if (String.IsNullOrEmpty(value))
                        value = Utility.GetAppSetting(HostCreateFlags);

                    //
                    // NOTE: Were we able to get the value from somewhere?
                    //
                    if (!String.IsNullOrEmpty(value))
                    {
                        Result error = null;

                        object enumValue = Utility.TryParseFlagsEnum(
                            null, typeof(HostCreateFlags), @default.ToString(),
                            value, null, true, true, true, ref error);

                        if (enumValue is HostCreateFlags)
                            return (HostCreateFlags)enumValue;
                    }
                }
                catch
                {
                    // do nothing.
                }

                return @default;
            }

            ///////////////////////////////////////////////////////////////////

            public static EngineFlags GetEngineFlags()
            {
                return GetEngineFlags(DefaultEngineFlags);
            }

            ///////////////////////////////////////////////////////////////////

            private static EngineFlags GetEngineFlags(
                EngineFlags @default
                )
            {
                try
                {
                    string value = Utility.GetEnvironmentVariable(
                        EngineFlags, true, true);

                    if (String.IsNullOrEmpty(value))
                        value = Utility.GetAppSetting(EngineFlags);

                    //
                    // NOTE: Were we able to get the value from somewhere?
                    //
                    if (!String.IsNullOrEmpty(value))
                    {
                        Result error = null;

                        object enumValue = Utility.TryParseFlagsEnum(
                            null, typeof(EngineFlags), @default.ToString(),
                            value, null, true, true, true, ref error);

                        if (enumValue is EngineFlags)
                            return (EngineFlags)enumValue;
                    }
                }
                catch
                {
                    // do nothing.
                }

                return @default;
            }

            ///////////////////////////////////////////////////////////////////

            public static SubstitutionFlags GetSubstitutionFlags()
            {
                return GetSubstitutionFlags(DefaultSubstitutionFlags);
            }

            ///////////////////////////////////////////////////////////////////

            private static SubstitutionFlags GetSubstitutionFlags(
                SubstitutionFlags @default
                )
            {
                try
                {
                    string value = Utility.GetEnvironmentVariable(
                        SubstitutionFlags, true, true);

                    if (String.IsNullOrEmpty(value))
                        value = Utility.GetAppSetting(SubstitutionFlags);

                    //
                    // NOTE: Were we able to get the value from somewhere?
                    //
                    if (!String.IsNullOrEmpty(value))
                    {
                        Result error = null;

                        object enumValue = Utility.TryParseFlagsEnum(
                            null, typeof(SubstitutionFlags),
                            @default.ToString(), value, null, true,
                            true, true, ref error);

                        if (enumValue is SubstitutionFlags)
                            return (SubstitutionFlags)enumValue;
                    }
                }
                catch
                {
                    // do nothing.
                }

                return @default;
            }

            ///////////////////////////////////////////////////////////////////

            public static EventFlags GetEventFlags()
            {
                return GetEventFlags(DefaultEventFlags);
            }

            ///////////////////////////////////////////////////////////////////

            private static EventFlags GetEventFlags(
                EventFlags @default
                )
            {
                try
                {
                    string value = Utility.GetEnvironmentVariable(
                        EventFlags, true, true);

                    if (String.IsNullOrEmpty(value))
                        value = Utility.GetAppSetting(EventFlags);

                    //
                    // NOTE: Were we able to get the value from somewhere?
                    //
                    if (!String.IsNullOrEmpty(value))
                    {
                        Result error = null;

                        object enumValue = Utility.TryParseFlagsEnum(
                            null, typeof(EventFlags), @default.ToString(),
                            value, null, true, true, true, ref error);

                        if (enumValue is EventFlags)
                            return (EventFlags)enumValue;
                    }
                }
                catch
                {
                    // do nothing.
                }

                return @default;
            }

            ///////////////////////////////////////////////////////////////////

            public static ExpressionFlags GetExpressionFlags()
            {
                return GetExpressionFlags(DefaultExpressionFlags);
            }

            ///////////////////////////////////////////////////////////////////

            private static ExpressionFlags GetExpressionFlags(
                ExpressionFlags @default
                )
            {
                try
                {
                    string value = Utility.GetEnvironmentVariable(
                        ExpressionFlags, true, true);

                    if (String.IsNullOrEmpty(value))
                        value = Utility.GetAppSetting(ExpressionFlags);

                    //
                    // NOTE: Were we able to get the value from somewhere?
                    //
                    if (!String.IsNullOrEmpty(value))
                    {
                        Result error = null;

                        object enumValue = Utility.TryParseFlagsEnum(
                            null, typeof(ExpressionFlags), @default.ToString(),
                            value, null, true, true, true, ref error);

                        if (enumValue is ExpressionFlags)
                            return (ExpressionFlags)enumValue;
                    }
                }
                catch
                {
                    // do nothing.
                }

                return @default;
            }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Private Interpreter Creation & Setup Methods
        private static bool SetupLibraryPath(
            ref Result error
            )
        {
            try
            {
                //
                // HACK: We must make sure that Eagle can find the script
                //       library to initialize the created interpreter(s).
                //
                string directory = Utility.GetAppSetting(
                    Settings.LibraryPath);

                if (String.IsNullOrEmpty(directory))
                {
                    directory = Utility.GetAppSetting(
                        Settings.EagleLibrary);
                }

                if (!String.IsNullOrEmpty(directory))
                {
                    //
                    // NOTE: Expand any environment variable references
                    //       that may be present in the path.
                    //
                    directory = Utility.ExpandEnvironmentVariables(
                        directory);

#if false
                        //
                        // NOTE: Set the library path to the location from
                        //       our application configuration.  This will
                        //       only work if the Interpreter type has not
                        //       yet been loaded from the Eagle assembly.
                        //
                        Utility.SetEnvironmentVariable(
                            EnvVars.EagleLibrary, directory);
#else
                    //
                    // NOTE: This is the "preferred" way of setting the
                    //       library path as it does not depend on the
                    //       Interpreter type not having been loaded
                    //       from the Eagle assembly yet.
                    //
                    Utility.SetLibraryPath(directory, true);
#endif

                    return true;
                }
#if true
                else
                {
                    //
                    // NOTE: This is the "preferred" way to have Eagle
                    //       automatically detect the library path to
                    //       use.  The assembly location is used along
                    //       with the various Eagle-related environment
                    //       variables and/or registry settings.
                    //
                    return Utility.DetectLibraryPath(
                        assembly, null, DetectFlags.Default);
                }
#endif
            }
            catch (Exception e)
            {
                error = e;
            }

            return false;
        }

        ///////////////////////////////////////////////////////////////////////

        private static ReturnCode SetupInterpreter(
            Interpreter interpreter,
            ref Result result
            )
        {
            try
            {
                string value = Utility.GetEnvironmentVariable(
                    Settings.SetupScript, true, true);

                if (value == null)
                    value = Utility.GetAppSetting(Settings.SetupScript);

                //
                // NOTE: Were we able to get the value from somewhere?
                //
                if (value != null)
                {
                    //
                    // NOTE: Get the normal engine flags for script
                    //       evaluation.
                    //
                    EngineFlags engineFlags = Settings.GetEngineFlags();

                    //
                    // NOTE: If the setup script is considered "trusted"
                    //       add the IgnoreHidden flag to override the
                    //       normal safe interpreter behavior.
                    //
                    if (Settings.GetTrustedSetup())
                        engineFlags |= EngineFlags.IgnoreHidden;

                    //
                    // NOTE: Evaluate the setup script and return the
                    //       results to the caller verbatim.
                    //
                    return Engine.EvaluateScript(
                        interpreter, value, engineFlags,
                        Settings.GetSubstitutionFlags(),
                        Settings.GetEventFlags(),
                        Settings.GetExpressionFlags(),
                        ref result);
                }

                //
                // NOTE: No setup script to evaluate, this is fine.
                //
                return ReturnCode.Ok;
            }
            catch (Exception e)
            {
                result = e;
            }

            return ReturnCode.Error;
        }

        ///////////////////////////////////////////////////////////////////////

        private static Interpreter CreateInterpreter(
            IEnumerable<string> args,
            ref Result result
            )
        {
            ReturnCode code = ReturnCode.Ok;
            Interpreter interpreter = null;

            try
            {
                if (SetupLibraryPath(ref result))
                {
                    bool console = Settings.GetNeedConsole();

                    CreateFlags createFlags =
                        Interpreter.GetStartupCreateFlags(
                        args, Settings.GetCreateFlags(),
                        OptionOriginFlags.Any, console, true);

                    HostCreateFlags hostCreateFlags =
                        Interpreter.GetStartupHostCreateFlags(
                        args, Settings.GetHostCreateFlags(),
                        OptionOriginFlags.Any, console, true);

                    string text = null;

                    code = Interpreter.GetStartupPreInitializeText(
                        args, createFlags, OptionOriginFlags.Standard,
                        console, true, ref text, ref result);

                    string libraryPath = null;

                    if (code == ReturnCode.Ok)
                    {
                        code = Interpreter.GetStartupLibraryPath(
                            args, createFlags, OptionOriginFlags.Standard,
                            console, true, ref libraryPath, ref result);
                    }

                    if (code == ReturnCode.Ok)
                    {
                        interpreter = Interpreter.Create(
                            args, createFlags, hostCreateFlags,
                            libraryPath, ref result);

                        if (interpreter != null)
                        {
                            code = Interpreter.ProcessStartupOptions(
                                interpreter, args, createFlags,
                                OptionOriginFlags.Standard, console,
                                true, ref result);

                            if (code == ReturnCode.Ok)
                            {
                                code = SetupInterpreter(
                                    interpreter, ref result);
                            }
                        }
                    }
                }
                else
                {
                    code = ReturnCode.Error;
                }
            }
            catch (Exception e)
            {
                result = e;
                code = ReturnCode.Error;
            }
            finally
            {
                if ((code != ReturnCode.Ok) && (interpreter != null))
                {
                    interpreter.Dispose();
                    interpreter = null;
                }
            }

            return interpreter;
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region IEagle Members
        public MethodResult EvaluateExpression(
            string text
            )
        {
            return EvaluateExpressionWithArgs(text, null);
        }

        ///////////////////////////////////////////////////////////////////////

        public MethodResult EvaluateExpressionWithArgs(
            string text,
            Collection<string> args
            )
        {
            ReturnCode code;
            Result result = null;

            using (Interpreter interpreter = CreateInterpreter(
                    args, ref result))
            {
                if (interpreter != null)
                {
                    code = Engine.EvaluateExpression(
                        interpreter, text,
                        Settings.GetEngineFlags(),
                        Settings.GetSubstitutionFlags(),
                        Settings.GetEventFlags(),
                        Settings.GetExpressionFlags(),
                        ref result);
                }
                else
                {
                    code = ReturnCode.Error;
                }
            }

            return new MethodResult(code, result);
        }

        ///////////////////////////////////////////////////////////////////////

        public MethodResult EvaluateScript(
            string text
            )
        {
            return EvaluateScriptWithArgs(text, null);
        }

        ///////////////////////////////////////////////////////////////////////

        public MethodResult EvaluateScriptWithArgs(
            string text,
            Collection<string> args
            )
        {
            ReturnCode code;
            Result result = null;
            int errorLine = 0;

            using (Interpreter interpreter = CreateInterpreter(
                    args, ref result))
            {
                if (interpreter != null)
                {
                    code = Engine.EvaluateScript(
                        interpreter, text,
                        Settings.GetEngineFlags(),
                        Settings.GetSubstitutionFlags(),
                        Settings.GetEventFlags(),
                        Settings.GetExpressionFlags(),
                        ref result, ref errorLine);
                }
                else
                {
                    code = ReturnCode.Error;
                }
            }

            return new MethodResult(code, result, errorLine);
        }

        ///////////////////////////////////////////////////////////////////////

        public MethodResult EvaluateFile(
            string fileName
            )
        {
            return EvaluateFileWithArgs(fileName, null);
        }

        ///////////////////////////////////////////////////////////////////////

        public MethodResult EvaluateFileWithArgs(
            string fileName,
            Collection<string> args
            )
        {
            ReturnCode code;
            Result result = null;
            int errorLine = 0;

            using (Interpreter interpreter = CreateInterpreter(
                    args, ref result))
            {
                if (interpreter != null)
                {
                    code = Engine.EvaluateFile(
                        interpreter, fileName,
                        Settings.GetEngineFlags(),
                        Settings.GetSubstitutionFlags(),
                        Settings.GetEventFlags(),
                        Settings.GetExpressionFlags(),
                        ref result, ref errorLine);
                }
                else
                {
                    code = ReturnCode.Error;
                }
            }

            return new MethodResult(code, result, errorLine);
        }

        ///////////////////////////////////////////////////////////////////////

        public MethodResult SubstituteString(
            string text
            )
        {
            return SubstituteStringWithArgs(text, null);
        }

        ///////////////////////////////////////////////////////////////////////

        public MethodResult SubstituteStringWithArgs(
            string text,
            Collection<string> args
            )
        {
            ReturnCode code;
            Result result = null;

            using (Interpreter interpreter = CreateInterpreter(
                    args, ref result))
            {
                if (interpreter != null)
                {
                    code = Engine.SubstituteString(
                        interpreter, text,
                        Settings.GetEngineFlags(),
                        Settings.GetSubstitutionFlags(),
                        Settings.GetEventFlags(),
                        Settings.GetExpressionFlags(),
                        ref result);
                }
                else
                {
                    code = ReturnCode.Error;
                }
            }

            return new MethodResult(code, result);
        }

        ///////////////////////////////////////////////////////////////////////

        public MethodResult SubstituteFile(
            string fileName
            )
        {
            return SubstituteFileWithArgs(fileName, null);
        }

        ///////////////////////////////////////////////////////////////////////

        public MethodResult SubstituteFileWithArgs(
            string fileName,
            Collection<string> args
            )
        {
            ReturnCode code;
            Result result = null;

            using (Interpreter interpreter = CreateInterpreter(
                    args, ref result))
            {
                if (interpreter != null)
                {
                    code = Engine.SubstituteFile(
                        interpreter, fileName,
                        Settings.GetEngineFlags(),
                        Settings.GetSubstitutionFlags(),
                        Settings.GetEventFlags(),
                        Settings.GetExpressionFlags(),
                        ref result);
                }
                else
                {
                    code = ReturnCode.Error;
                }
            }

            return new MethodResult(code, result);
        }

        ///////////////////////////////////////////////////////////////////////

        public bool IsSuccess(
            ReturnCode code,
            bool exceptions
            )
        {
            return Utility.IsSuccess(code, exceptions);
        }

        ///////////////////////////////////////////////////////////////////////

        public string FormatResult(
            ReturnCode code,
            string result,
            int errorLine
            )
        {
            return Utility.FormatResult(code, result, errorLine);
        }

        ///////////////////////////////////////////////////////////////////////

        public string FormatMethodResult(MethodResult result)
        {
            return (result != null) ? FormatResult(
                result.ReturnCode, result.Result, result.ErrorLine) : null;
        }
        #endregion
    }
}
