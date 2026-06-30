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
    /// <summary>
    /// This class implements the Eagle ASP.NET web service.  It exposes the
    /// <see cref="IEagle" /> contract over the web, allowing remote callers to
    /// evaluate expressions, scripts, and files, perform string and file
    /// substitution, and format the results, each within a freshly created and
    /// configured Eagle interpreter.
    /// </summary>
    [WebService(
        Name = Eagle.Name,
        Description = Eagle.Description,
        Namespace = Eagle.Namespace)]
    [ObjectId("36ce542c-dc1c-4a9a-affa-ce22aebb1173")]
    public sealed class Eagle : IEagle
    {
        #region Private Constants
        /// <summary>
        /// The display name of this web service.
        /// </summary>
        private const string Name = "Eagle Web Service";

        /// <summary>
        /// The human-readable description of this web service.
        /// </summary>
        private const string Description =
            "This service is used to handle dynamic content (i.e. expressions, " +
            "scripts, and/or text blocks) for the Tcl and/or Eagle languages.";

        /// <summary>
        /// The XML namespace used to identify this web service.
        /// </summary>
        private const string Namespace = "https://urn.to/r/eagle";

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// The currently executing assembly, used when detecting the script
        /// library path.
        /// </summary>
        private static readonly Assembly assembly =
            Assembly.GetExecutingAssembly();
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Default Settings
        //
        // NOTE: By default, no console.
        //
        /// <summary>
        /// The default setting indicating whether a console-based interpreter
        /// host is needed.  By default, no console is used.
        /// </summary>
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
        /// <summary>
        /// The default flags used when creating an interpreter.  By default,
        /// this requests a "safe" subset of commands suitable for embedded
        /// use, without throwing an exception on error.
        /// </summary>
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
        /// <summary>
        /// The default flags used when creating an interpreter host.  By
        /// default, the console title and icon are left unchanged and the
        /// Ctrl-C keypress is not intercepted.
        /// </summary>
        private static readonly HostCreateFlags DefaultHostCreateFlags =
            HostCreateFlags.SafeEmbeddedUse;

        ///////////////////////////////////////////////////////////////////////

        //
        // NOTE: By default:
        //
        //       1. We want no special engine flags.
        //
        /// <summary>
        /// The default engine flags used when evaluating expressions, scripts,
        /// and files.  By default, no special engine flags are used.
        /// </summary>
        private static readonly EngineFlags DefaultEngineFlags =
            EngineFlags.None;

        ///////////////////////////////////////////////////////////////////////

        //
        // NOTE: By default:
        //
        //       1. We want all substitution types to be performed.
        //
        /// <summary>
        /// The default substitution flags used when evaluating or
        /// substituting.  By default, all substitution types are performed.
        /// </summary>
        private static readonly SubstitutionFlags DefaultSubstitutionFlags =
            SubstitutionFlags.Default;

        ///////////////////////////////////////////////////////////////////////

        //
        // NOTE: By default:
        //
        //       1. We want to handle events targeted to the engine.
        //
        /// <summary>
        /// The default event flags used when evaluating or substituting.  By
        /// default, events targeted to the engine are handled.
        /// </summary>
        private static readonly EventFlags DefaultEventFlags =
            EventFlags.Default;

        ///////////////////////////////////////////////////////////////////////

        //
        // NOTE: By default:
        //
        //       1. We want all expression types to be performed.
        //
        /// <summary>
        /// The default expression flags used when evaluating or substituting.
        /// By default, all expression types are performed.
        /// </summary>
        private static readonly ExpressionFlags DefaultExpressionFlags =
            ExpressionFlags.Default;
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Public Constructors
        /// <summary>
        /// Constructs an instance of the <see cref="Eagle" /> web service.
        /// </summary>
        public Eagle()
        {
            // do nothing.
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Private Settings Management Class
        /// <summary>
        /// This class manages the configuration settings used by the web
        /// service, reading them from the environment variables and/or the
        /// application configuration settings, and falling back to the
        /// built-in defaults when they are not otherwise specified.
        /// </summary>
        [ObjectId("1fca91ae-46dd-4e13-98d6-c21815c8131c")]
        private static class Settings
        {
            #region Setting Names
            /// <summary>
            /// The name of the setting that specifies the Eagle script library
            /// path.
            /// </summary>
            public static readonly string EagleLibrary = EnvVars.EagleLibrary;

            /// <summary>
            /// The name of the setting that specifies the setup script to
            /// evaluate within each newly created interpreter.
            /// </summary>
            public static readonly string SetupScript = "SetupScript";

            /// <summary>
            /// The name of the setting that specifies the script library path.
            /// </summary>
            public static readonly string LibraryPath = "LibraryPath";

            /// <summary>
            /// The name of the setting that specifies the interpreter creation
            /// flags.
            /// </summary>
            public static readonly string CreateFlags = "CreateFlags";

            /// <summary>
            /// The name of the setting that specifies the interpreter host
            /// creation flags.
            /// </summary>
            public static readonly string HostCreateFlags = "HostCreateFlags";

            /// <summary>
            /// The name of the setting that specifies the engine flags.
            /// </summary>
            public static readonly string EngineFlags = "EngineFlags";

            /// <summary>
            /// The name of the setting that specifies the substitution flags.
            /// </summary>
            public static readonly string SubstitutionFlags = "SubstitutionFlags";

            /// <summary>
            /// The name of the setting that specifies the event flags.
            /// </summary>
            public static readonly string EventFlags = "EventFlags";

            /// <summary>
            /// The name of the setting that specifies the expression flags.
            /// </summary>
            public static readonly string ExpressionFlags = "ExpressionFlags";

            /// <summary>
            /// The name of the setting that specifies whether the setup script
            /// should be considered trusted.
            /// </summary>
            public static readonly string TrustedSetup = "TrustedSetup";

            /// <summary>
            /// The name of the setting that specifies whether a console-based
            /// interpreter host is needed.
            /// </summary>
            public static readonly string NeedConsole = "NeedConsole";
            #endregion

            ///////////////////////////////////////////////////////////////////

            /// <summary>
            /// This method determines whether the setup script should be
            /// considered trusted, based on the configured settings.
            /// </summary>
            /// <returns>
            /// True if the setup script is configured to be trusted;
            /// otherwise, false.
            /// </returns>
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

            /// <summary>
            /// This method determines whether a console-based interpreter host
            /// is needed, based on the configured settings, using the built-in
            /// default value as the starting point.
            /// </summary>
            /// <returns>
            /// True if a console-based interpreter host is needed; otherwise,
            /// false.
            /// </returns>
            public static bool GetNeedConsole()
            {
                return GetNeedConsole(DefaultConsole);
            }

            ///////////////////////////////////////////////////////////////////

            /// <summary>
            /// This method determines whether a console-based interpreter host
            /// is needed, based on the configured settings, falling back to the
            /// specified default value.
            /// </summary>
            /// <param name="default">
            /// The default value to return if the setting is not otherwise
            /// specified.
            /// </param>
            /// <returns>
            /// True if a console-based interpreter host is needed; otherwise,
            /// false.
            /// </returns>
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

            /// <summary>
            /// This method gets the interpreter creation flags from the
            /// configured settings, using the built-in default value as the
            /// starting point.
            /// </summary>
            /// <returns>
            /// The interpreter creation flags to use.
            /// </returns>
            public static CreateFlags GetCreateFlags()
            {
                return GetCreateFlags(DefaultCreateFlags);
            }

            ///////////////////////////////////////////////////////////////////

            /// <summary>
            /// This method gets the interpreter creation flags from the
            /// configured settings, falling back to the specified default
            /// value.
            /// </summary>
            /// <param name="default">
            /// The default flags to return if the setting is not otherwise
            /// specified.
            /// </param>
            /// <returns>
            /// The interpreter creation flags to use.
            /// </returns>
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

            /// <summary>
            /// This method gets the interpreter host creation flags from the
            /// configured settings, using the built-in default value as the
            /// starting point.
            /// </summary>
            /// <returns>
            /// The interpreter host creation flags to use.
            /// </returns>
            public static HostCreateFlags GetHostCreateFlags()
            {
                return GetHostCreateFlags(DefaultHostCreateFlags);
            }

            ///////////////////////////////////////////////////////////////////

            /// <summary>
            /// This method gets the interpreter host creation flags from the
            /// configured settings, falling back to the specified default
            /// value.
            /// </summary>
            /// <param name="default">
            /// The default flags to return if the setting is not otherwise
            /// specified.
            /// </param>
            /// <returns>
            /// The interpreter host creation flags to use.
            /// </returns>
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

            /// <summary>
            /// This method gets the engine flags from the configured settings,
            /// using the built-in default value as the starting point.
            /// </summary>
            /// <returns>
            /// The engine flags to use.
            /// </returns>
            public static EngineFlags GetEngineFlags()
            {
                return GetEngineFlags(DefaultEngineFlags);
            }

            ///////////////////////////////////////////////////////////////////

            /// <summary>
            /// This method gets the engine flags from the configured settings,
            /// falling back to the specified default value.
            /// </summary>
            /// <param name="default">
            /// The default flags to return if the setting is not otherwise
            /// specified.
            /// </param>
            /// <returns>
            /// The engine flags to use.
            /// </returns>
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

            /// <summary>
            /// This method gets the substitution flags from the configured
            /// settings, using the built-in default value as the starting
            /// point.
            /// </summary>
            /// <returns>
            /// The substitution flags to use.
            /// </returns>
            public static SubstitutionFlags GetSubstitutionFlags()
            {
                return GetSubstitutionFlags(DefaultSubstitutionFlags);
            }

            ///////////////////////////////////////////////////////////////////

            /// <summary>
            /// This method gets the substitution flags from the configured
            /// settings, falling back to the specified default value.
            /// </summary>
            /// <param name="default">
            /// The default flags to return if the setting is not otherwise
            /// specified.
            /// </param>
            /// <returns>
            /// The substitution flags to use.
            /// </returns>
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

            /// <summary>
            /// This method gets the event flags from the configured settings,
            /// using the built-in default value as the starting point.
            /// </summary>
            /// <returns>
            /// The event flags to use.
            /// </returns>
            public static EventFlags GetEventFlags()
            {
                return GetEventFlags(DefaultEventFlags);
            }

            ///////////////////////////////////////////////////////////////////

            /// <summary>
            /// This method gets the event flags from the configured settings,
            /// falling back to the specified default value.
            /// </summary>
            /// <param name="default">
            /// The default flags to return if the setting is not otherwise
            /// specified.
            /// </param>
            /// <returns>
            /// The event flags to use.
            /// </returns>
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

            /// <summary>
            /// This method gets the expression flags from the configured
            /// settings, using the built-in default value as the starting
            /// point.
            /// </summary>
            /// <returns>
            /// The expression flags to use.
            /// </returns>
            public static ExpressionFlags GetExpressionFlags()
            {
                return GetExpressionFlags(DefaultExpressionFlags);
            }

            ///////////////////////////////////////////////////////////////////

            /// <summary>
            /// This method gets the expression flags from the configured
            /// settings, falling back to the specified default value.
            /// </summary>
            /// <param name="default">
            /// The default flags to return if the setting is not otherwise
            /// specified.
            /// </param>
            /// <returns>
            /// The expression flags to use.
            /// </returns>
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
        /// <summary>
        /// This method configures the Eagle script library path so that newly
        /// created interpreters can be initialized, using the configured
        /// settings when available and otherwise attempting to automatically
        /// detect it based on the executing assembly location.
        /// </summary>
        /// <param name="error">
        /// Upon failure, this will contain an appropriate error message.
        /// </param>
        /// <returns>
        /// True if the library path was successfully configured; otherwise,
        /// false.
        /// </returns>
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

        /// <summary>
        /// This method evaluates the configured setup script, if any, within
        /// the specified interpreter.  If the setup script is considered
        /// trusted, the normal safe interpreter behavior is overridden so that
        /// hidden commands are ignored.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter in which to evaluate the setup script.
        /// </param>
        /// <param name="result">
        /// Upon success, this may contain the result of evaluating the setup
        /// script.  Upon failure, this will contain an appropriate error
        /// message.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise, a non-Ok value
        /// with details placed in the <paramref name="result" /> parameter.
        /// </returns>
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

        /// <summary>
        /// This method creates and configures a new interpreter, including
        /// setting up the script library path, applying the configured
        /// creation flags, processing any startup options, and evaluating the
        /// configured setup script.  If any step fails, the interpreter is
        /// disposed before returning.
        /// </summary>
        /// <param name="args">
        /// The extra arguments to use when creating the interpreter, if any.
        /// This parameter may be null.
        /// </param>
        /// <param name="result">
        /// Upon failure, this will contain an appropriate error message.
        /// </param>
        /// <returns>
        /// The newly created and configured interpreter, or null if it could
        /// not be created.
        /// </returns>
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
        /// <summary>
        /// This method evaluates the specified expression within a newly
        /// created interpreter.
        /// </summary>
        /// <param name="text">
        /// The expression to evaluate.
        /// </param>
        /// <returns>
        /// A <see cref="MethodResult" /> containing the return code and result
        /// of evaluating the expression.
        /// </returns>
        public MethodResult EvaluateExpression(
            string text
            )
        {
            return EvaluateExpressionWithArgs(text, null);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method evaluates the specified expression within a newly
        /// created interpreter, using the specified extra arguments when
        /// creating the interpreter.
        /// </summary>
        /// <param name="text">
        /// The expression to evaluate.
        /// </param>
        /// <param name="args">
        /// The extra arguments to use when creating the interpreter, if any.
        /// This parameter may be null.
        /// </param>
        /// <returns>
        /// A <see cref="MethodResult" /> containing the return code and result
        /// of evaluating the expression.
        /// </returns>
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

        /// <summary>
        /// This method evaluates the specified script within a newly created
        /// interpreter.
        /// </summary>
        /// <param name="text">
        /// The script to evaluate.
        /// </param>
        /// <returns>
        /// A <see cref="MethodResult" /> containing the return code, result,
        /// and error line of evaluating the script.
        /// </returns>
        public MethodResult EvaluateScript(
            string text
            )
        {
            return EvaluateScriptWithArgs(text, null);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method evaluates the specified script within a newly created
        /// interpreter, using the specified extra arguments when creating the
        /// interpreter.
        /// </summary>
        /// <param name="text">
        /// The script to evaluate.
        /// </param>
        /// <param name="args">
        /// The extra arguments to use when creating the interpreter, if any.
        /// This parameter may be null.
        /// </param>
        /// <returns>
        /// A <see cref="MethodResult" /> containing the return code, result,
        /// and error line of evaluating the script.
        /// </returns>
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

        /// <summary>
        /// This method evaluates the script contained in the specified file
        /// within a newly created interpreter.
        /// </summary>
        /// <param name="fileName">
        /// The name of the file containing the script to evaluate.
        /// </param>
        /// <returns>
        /// A <see cref="MethodResult" /> containing the return code, result,
        /// and error line of evaluating the file.
        /// </returns>
        public MethodResult EvaluateFile(
            string fileName
            )
        {
            return EvaluateFileWithArgs(fileName, null);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method evaluates the script contained in the specified file
        /// within a newly created interpreter, using the specified extra
        /// arguments when creating the interpreter.
        /// </summary>
        /// <param name="fileName">
        /// The name of the file containing the script to evaluate.
        /// </param>
        /// <param name="args">
        /// The extra arguments to use when creating the interpreter, if any.
        /// This parameter may be null.
        /// </param>
        /// <returns>
        /// A <see cref="MethodResult" /> containing the return code, result,
        /// and error line of evaluating the file.
        /// </returns>
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

        /// <summary>
        /// This method performs substitution on the specified string within a
        /// newly created interpreter.
        /// </summary>
        /// <param name="text">
        /// The string to perform substitution on.
        /// </param>
        /// <returns>
        /// A <see cref="MethodResult" /> containing the return code and result
        /// of the substitution.
        /// </returns>
        public MethodResult SubstituteString(
            string text
            )
        {
            return SubstituteStringWithArgs(text, null);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method performs substitution on the specified string within a
        /// newly created interpreter, using the specified extra arguments when
        /// creating the interpreter.
        /// </summary>
        /// <param name="text">
        /// The string to perform substitution on.
        /// </param>
        /// <param name="args">
        /// The extra arguments to use when creating the interpreter, if any.
        /// This parameter may be null.
        /// </param>
        /// <returns>
        /// A <see cref="MethodResult" /> containing the return code and result
        /// of the substitution.
        /// </returns>
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

        /// <summary>
        /// This method performs substitution on the contents of the specified
        /// file within a newly created interpreter.
        /// </summary>
        /// <param name="fileName">
        /// The name of the file to perform substitution on.
        /// </param>
        /// <returns>
        /// A <see cref="MethodResult" /> containing the return code and result
        /// of the substitution.
        /// </returns>
        public MethodResult SubstituteFile(
            string fileName
            )
        {
            return SubstituteFileWithArgs(fileName, null);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method performs substitution on the contents of the specified
        /// file within a newly created interpreter, using the specified extra
        /// arguments when creating the interpreter.
        /// </summary>
        /// <param name="fileName">
        /// The name of the file to perform substitution on.
        /// </param>
        /// <param name="args">
        /// The extra arguments to use when creating the interpreter, if any.
        /// This parameter may be null.
        /// </param>
        /// <returns>
        /// A <see cref="MethodResult" /> containing the return code and result
        /// of the substitution.
        /// </returns>
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

        /// <summary>
        /// This method determines whether the specified return code represents
        /// a successful outcome.
        /// </summary>
        /// <param name="code">
        /// The return code to check.
        /// </param>
        /// <param name="exceptions">
        /// Non-zero if return codes representing exceptions should be treated
        /// as successful.
        /// </param>
        /// <returns>
        /// True if the return code represents a successful outcome; otherwise,
        /// false.
        /// </returns>
        public bool IsSuccess(
            ReturnCode code,
            bool exceptions
            )
        {
            return Utility.IsSuccess(code, exceptions);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method formats the specified return code, result, and error
        /// line into a single human-readable string.
        /// </summary>
        /// <param name="code">
        /// The return code to format.
        /// </param>
        /// <param name="result">
        /// The result string to format.
        /// </param>
        /// <param name="errorLine">
        /// The error line number to format, or zero if there is none.
        /// </param>
        /// <returns>
        /// The formatted result string.
        /// </returns>
        public string FormatResult(
            ReturnCode code,
            string result,
            int errorLine
            )
        {
            return Utility.FormatResult(code, result, errorLine);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method formats the return code, result, and error line
        /// contained in the specified <see cref="MethodResult" /> into a single
        /// human-readable string.
        /// </summary>
        /// <param name="result">
        /// The method result to format.  This parameter may be null.
        /// </param>
        /// <returns>
        /// The formatted result string, or null if <paramref name="result" />
        /// is null.
        /// </returns>
        public string FormatMethodResult(MethodResult result)
        {
            return (result != null) ? FormatResult(
                result.ReturnCode, result.Result, result.ErrorLine) : null;
        }
        #endregion
    }
}
