/*
 * InterpreterSettings.cs --
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

#if XML
using System.Xml;
#endif

#if XML && SERIALIZATION
using System.Xml.Serialization;
#endif

using Eagle._Attributes;
using Eagle._Components.Private;
using Eagle._Constants;
using Eagle._Containers.Public;
using Eagle._Interfaces.Public;
using SharedStringOps = Eagle._Components.Shared.StringOps;
using _RuleSet = Eagle._Components.Public.RuleSet;

namespace Eagle._Components.Public
{
#if SERIALIZATION
    [Serializable()]
#endif
    [ObjectId("1d0263ae-929f-4ea1-a6c6-cd8b749d55bb")]
    public sealed class InterpreterSettings :
#if ISOLATED_INTERPRETERS || ISOLATED_PLUGINS
        ScriptMarshalByRefObject,
#endif
        IInterpreterSettings
    {
        #region Private Constants
        private static readonly string LoadFromFileNameFormat =
            "{0}.settings{1}";

        ///////////////////////////////////////////////////////////////////////

        private const CreateFlags SafeCreateFlags =
            (CreateFlags.FastSingleUse & ~(CreateFlags.Initialize |
            CreateFlags.ThrowOnError)) | CreateFlags.IfNecessary |
            CreateFlags.IfCannotLock | CreateFlags.MeasureTime |
            CreateFlags.SafeAndHideUnsafe | CreateFlags.NoDispose |
            CreateFlags.NoCommands | CreateFlags.NoFunctions |
            CreateFlags.NoCoreTraces | CreateFlags.NoCorePolicies;

        ///////////////////////////////////////////////////////////////////////

        private const HostCreateFlags SafeHostCreateFlags =
            HostCreateFlags.FastSingleUse;
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Private Data
        private IRuleSet ruleSet;
        private IEnumerable<string> args;
        private string culture;
        private CreateFlags createFlags;
        private HostCreateFlags hostCreateFlags;
        private InitializeFlags initializeFlags;
        private ScriptFlags scriptFlags;
        private InterpreterFlags interpreterFlags;
        private InterpreterTestFlags interpreterTestFlags;
        private PluginFlags pluginFlags;

#if NATIVE && TCL
        private FindFlags findFlags;
        private LoadFlags loadFlags;
#endif

#if SERIALIZATION
        [NonSerialized()]
#endif
        private AppDomain appDomain;

        private IHost host;
        private string profile;
        private object owner;
        private object applicationObject;
        private object policyObject;
        private object resolverObject;
        private object userObject;
        private PolicyList policies;
        private TraceList traces;
        private string text;
        private string libraryPath;
        private StringList autoPathList;
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Private Constructors
        private InterpreterSettings()
        {
            // do nothing.
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Static "Factory" Methods
        public static IInterpreterSettings Create()
        {
            return new InterpreterSettings();
        }

        ///////////////////////////////////////////////////////////////////////

        public static IInterpreterSettings CreateDefault()
        {
            return CreateDefault(null, null);
        }

        ///////////////////////////////////////////////////////////////////////

        public static IInterpreterSettings CreateDefault(
            IRuleSet ruleSet,        /* in */
            IEnumerable<string> args /* in */
            )
        {
            IInterpreterSettings interpreterSettings = Create();

            if (interpreterSettings != null)
            {
                interpreterSettings.ResetEverything();
                interpreterSettings.UseDefaultsForFlags();

                if (ruleSet != null)
                    interpreterSettings.RuleSet = ruleSet;

                if (args != null)
                    interpreterSettings.Args = args;
            }

            return interpreterSettings;
        }

        ///////////////////////////////////////////////////////////////////////

        public static IInterpreterSettings CreateSafe(
            IRuleSet ruleSet,        /* in */
            IEnumerable<string> args /* in */
            )
        {
            IInterpreterSettings interpreterSettings = CreateDefault();

            if (ruleSet != null)
                interpreterSettings.RuleSet = ruleSet;

            if (args != null)
                interpreterSettings.Args = args;

            interpreterSettings.CreateFlags = SafeCreateFlags;
            interpreterSettings.HostCreateFlags = SafeHostCreateFlags;

            return interpreterSettings;
        }

        ///////////////////////////////////////////////////////////////////////

        public static IInterpreterSettings CreateNonCritical(
            IRuleSet ruleSet,        /* in */
            IEnumerable<string> args /* in */
            )
        {
            IInterpreterSettings interpreterSettings = CreateDefault();

            if (ruleSet != null)
                interpreterSettings.RuleSet = ruleSet;

            if (args != null)
                interpreterSettings.Args = args;

            interpreterSettings.InitializeFlags |= InitializeFlags.NoCritical;

            return interpreterSettings;
        }

        ///////////////////////////////////////////////////////////////////////

        public static IInterpreterSettings CreateFrom(
            string fileName,         /* in */
            CultureInfo cultureInfo, /* in */
            bool merge,              /* in */
            bool expand,             /* in */
            ref Result error         /* out */
            )
        {
            IInterpreterSettings interpreterSettings = null;

            if (LoadFrom(fileName,
                    cultureInfo, merge, expand,
                    ref interpreterSettings,
                    ref error) == ReturnCode.Ok)
            {
                return interpreterSettings;
            }

            return null;
        }

        ///////////////////////////////////////////////////////////////////////

        public static IInterpreterSettings CreateShell( /* PrivateShellMain */
            IRuleSet ruleSet,              /* in */
            IEnumerable<string> args,      /* in */
            OptionOriginFlags originFlags, /* in */
            bool console,                  /* in */
            bool verbose,                  /* in */
            ref Result error               /* out */
            )
        {
            //
            // NOTE: Initially, all flags are set to "None" here; they may
            //       be modified via the GetFlagsForShell method.
            //
            CreateFlags createFlags = CreateFlags.None;
            HostCreateFlags hostCreateFlags = HostCreateFlags.None;
            InitializeFlags initializeFlags = InitializeFlags.None;
            ScriptFlags scriptFlags = ScriptFlags.None;

            GetFlagsForShell(
                args, originFlags, console, verbose, ref createFlags,
                ref hostCreateFlags, ref initializeFlags, ref scriptFlags);

            //
            // BUGFIX: If the "ShellPreInitialize" environment variable is
            //         present, pre-scan all the command line arguments for
            //         the pre-initialize script to evaluate.  Otherwise,
            //         this should be skipped to prevent the pre-initialize
            //         script from being evaluated more than once (COMPAT:
            //         Eagle Beta).
            //
            string text = null;

            if (GlobalConfiguration.DoesValueExist(
                    EnvVars.ShellPreInitialize,
                    ConfigurationFlags.InterpreterVerbose))
            {
                if (Interpreter.GetStartupPreInitializeText(
                        args, createFlags, originFlags, console, verbose,
                        ref text, ref error) != ReturnCode.Ok)
                {
                    return null;
                }
            }

            string libraryPath = null;

            if (Interpreter.GetStartupLibraryPath(
                    args, createFlags, originFlags, console, verbose,
                    ref libraryPath, ref error) != ReturnCode.Ok)
            {
                return null;
            }

            return Create(
                ruleSet, args, createFlags, hostCreateFlags, initializeFlags,
                scriptFlags, text, libraryPath);
        }

        ///////////////////////////////////////////////////////////////////////

        internal static IInterpreterSettings Create(
            IRuleSet ruleSet,            /* in */
            IEnumerable<string> args,    /* in */
            SecurityLevel securityLevel, /* in */
            ref Result error             /* out */
            )
        {
            if (FlagOps.HasFlags(
                    securityLevel, SecurityLevel.Sdk, true))
            {
                error = String.Format(
                    "security level {0} is unavailable",
                    securityLevel);

                return null;
            }

            if (FlagOps.HasFlags(
                    securityLevel, SecurityLevel.Safe, true))
            {
                return CreateSafe(ruleSet, args);
            }
            else
            {
                return CreateDefault(ruleSet, args);
            }
        }

        ///////////////////////////////////////////////////////////////////////

        private static IInterpreterSettings Create(
            IRuleSet ruleSet,                /* in */
            IEnumerable<string> args,        /* in */
            CreateFlags createFlags,         /* in */
            HostCreateFlags hostCreateFlags, /* in */
            InitializeFlags initializeFlags, /* in */
            ScriptFlags scriptFlags,         /* in */
            string text,                     /* in */
            string libraryPath               /* in */
            )
        {
            IInterpreterSettings interpreterSettings = CreateDefault();

            interpreterSettings.RuleSet = ruleSet;
            interpreterSettings.Args = args;
            interpreterSettings.CreateFlags = createFlags;
            interpreterSettings.HostCreateFlags = hostCreateFlags;
            interpreterSettings.InitializeFlags = initializeFlags;
            interpreterSettings.ScriptFlags = scriptFlags;
            interpreterSettings.Text = text;
            interpreterSettings.LibraryPath = libraryPath;

            return interpreterSettings;
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Private Static Methods
        private static void GetFlagsForShell(
            IEnumerable<string> args,            /* in */
            OptionOriginFlags originFlags,       /* in */
            bool console,                        /* in */
            bool verbose,                        /* in */
            ref CreateFlags createFlags,         /* out */
            ref HostCreateFlags hostCreateFlags, /* out */
            ref InitializeFlags initializeFlags, /* out */
            ref ScriptFlags scriptFlags          /* out */
            )
        {
            //
            // NOTE: Setup the appropriate interpreter creation flags
            //       for a shell.
            //
            createFlags = CreateFlags.CoreShellUse; /* EXEMPT */

            //
            // NOTE: Get the effective interpreter creation flags for
            //       the shell from the environment, etc.
            //
            createFlags = Interpreter.GetStartupCreateFlags(
                args, createFlags, originFlags, console, verbose);

            //
            // NOTE: Setup the appropriate interpreter host creation
            //       flags for a shell.
            //
            hostCreateFlags = HostCreateFlags.CoreShellUse; /* EXEMPT */

            //
            // NOTE: Get the effective interpreter creation flags for
            //       the shell from the environment, etc.
            //
            hostCreateFlags = Interpreter.GetStartupHostCreateFlags(
                args, hostCreateFlags, originFlags, console, verbose);

            //
            // NOTE: Setup the appropriate interpreter initialization
            //       flags for a shell.
            //
            initializeFlags = InitializeFlags.CoreShellUse; /* EXEMPT */

            //
            // NOTE: Get the effective interpreter initialization flags
            //       for the shell from the environment, etc.
            //
            initializeFlags = Interpreter.GetStartupInitializeFlags(
                args, initializeFlags, originFlags, console, verbose);

            //
            // NOTE: Are we creating a safe interpreter?  If so, make
            //       sure the "full initialize" option is not present,
            //       then disable evaluating "init.eagle" and evaluate
            //       "safe.eagle" instead.
            //
            if (FlagOps.HasFlags(createFlags, CreateFlags.Safe, true))
            {
                initializeFlags &= ~InitializeFlags.Loader;
                initializeFlags &= ~InitializeFlags.Initialization;
                initializeFlags |= InitializeFlags.Safe;
            }

            //
            // NOTE: Setup the appropriate interpreter script flags
            //       for a shell.
            //
            scriptFlags = Defaults.ScriptFlags;

            //
            // NOTE: Get the effective interpreter script flags for
            //       the shell from the environment, etc.
            //
            scriptFlags = Interpreter.GetStartupScriptFlags(
                args, scriptFlags, originFlags, console, verbose);
        }

        ///////////////////////////////////////////////////////////////////////

        private static bool CouldBeDocument(
            string path /* in */
            )
        {
            if (String.IsNullOrEmpty(path))
                return false;

            string extension = PathOps.GetExtension(path);

            if (String.IsNullOrEmpty(extension))
                return false;

            if (SharedStringOps.Equals(extension,
                    FileExtension.Configuration, PathOps.ComparisonType))
            {
                return true;
            }

            return false;
        }

        ///////////////////////////////////////////////////////////////////////

        private static string Expand(
            string value /* in */
            )
        {
            if (String.IsNullOrEmpty(value))
                return value;

            return CommonOps.Environment.ExpandVariables(value);
        }

        ///////////////////////////////////////////////////////////////////////

        internal static void Expand(
            IInterpreterSettings interpreterSettings /* in, out */
            )
        {
            if (interpreterSettings != null)
            {
                IEnumerable<string> args = interpreterSettings.Args;

                if (args != null)
                {
                    StringList newArgs = new StringList();

                    foreach (string arg in args)
                        newArgs.Add(Expand(arg));

                    interpreterSettings.Args = newArgs;
                }

                interpreterSettings.Culture = Expand(
                    interpreterSettings.Culture);

                interpreterSettings.Profile = Expand(
                    interpreterSettings.Profile);

                interpreterSettings.Text = Expand(interpreterSettings.Text);

                interpreterSettings.LibraryPath = Expand(
                    interpreterSettings.LibraryPath);

                StringList autoPathList = interpreterSettings.AutoPathList;

                if (autoPathList != null)
                {
                    for (int index = 0; index < autoPathList.Count; index++)
                        autoPathList[index] = Expand(autoPathList[index]);
                }
            }
        }

        ///////////////////////////////////////////////////////////////////////

        private static ReturnCode FromInterpreter(
            Interpreter interpreter,                  /* in */
            IInterpreterSettings interpreterSettings, /* in, out */
            bool recreate,                            /* in */
            bool full,                                /* in */
            ref Result error                          /* out */
            )
        {
            if (interpreter == null)
            {
                error = "invalid interpreter";
                return ReturnCode.Error;
            }

            if (interpreterSettings == null)
            {
                error = "invalid interpreter settings";
                return ReturnCode.Error;
            }

            lock (interpreter.InternalSyncRoot) /* TRANSACTIONAL */
            {
                interpreterSettings.UseFlagsFromInterpreter(interpreter);

                if (interpreter.PopulateInterpreterSettings(recreate, full,
                        ref interpreterSettings, ref error) != ReturnCode.Ok)
                {
                    return ReturnCode.Error;
                }

                interpreterSettings.UseObjectsFromInterpreter(interpreter);
                interpreterSettings.LibraryPath = interpreter.LibraryPath;
            }

            return ReturnCode.Ok;
        }

        ///////////////////////////////////////////////////////////////////////

        internal static StringList Copy(
            IInterpreterSettings sourceInterpreterSettings, /* in */
            IInterpreterSettings targetInterpreterSettings, /* in, out */
            bool forceMissing                               /* in */
            )
        {
            StringList result = null;

            if ((sourceInterpreterSettings == null) ||
                (targetInterpreterSettings == null))
            {
                return result;
            }

            result = new StringList();

            IRuleSet ruleSet = sourceInterpreterSettings.RuleSet;

            if (forceMissing || (ruleSet != null))
            {
                targetInterpreterSettings.RuleSet = ruleSet;
                result.Add("ruleSet");
            }

            IEnumerable<string> args = sourceInterpreterSettings.Args;

            if (forceMissing || (args != null))
            {
                targetInterpreterSettings.Args = args;
                result.Add("args");
            }

            string culture = sourceInterpreterSettings.Culture;

            if (forceMissing || (culture != null))
            {
                targetInterpreterSettings.Culture = culture;
                result.Add("culture");
            }

            CreateFlags createFlags = sourceInterpreterSettings.CreateFlags;

            if (forceMissing || (createFlags != CreateFlags.None))
            {
                targetInterpreterSettings.CreateFlags = createFlags;
                result.Add("createFlags");
            }

            HostCreateFlags hostCreateFlags =
                sourceInterpreterSettings.HostCreateFlags;

            if (forceMissing || (hostCreateFlags != HostCreateFlags.None))
            {
                targetInterpreterSettings.HostCreateFlags = hostCreateFlags;
                result.Add("hostCreateFlags");
            }

            InitializeFlags initializeFlags =
                sourceInterpreterSettings.InitializeFlags;

            if (forceMissing || (initializeFlags != InitializeFlags.None))
            {
                targetInterpreterSettings.InitializeFlags = initializeFlags;
                result.Add("initializeFlags");
            }

            ScriptFlags scriptFlags = sourceInterpreterSettings.ScriptFlags;

            if (forceMissing || (scriptFlags != ScriptFlags.None))
            {
                targetInterpreterSettings.ScriptFlags = scriptFlags;
                result.Add("scriptFlags");
            }

            InterpreterFlags interpreterFlags =
                sourceInterpreterSettings.InterpreterFlags;

            if (forceMissing || (interpreterFlags != InterpreterFlags.None))
            {
                targetInterpreterSettings.InterpreterFlags = interpreterFlags;
                result.Add("interpreterFlags");
            }

            InterpreterTestFlags interpreterTestFlags =
                sourceInterpreterSettings.InterpreterTestFlags;

            if (forceMissing || (interpreterTestFlags != InterpreterTestFlags.None))
            {
                targetInterpreterSettings.InterpreterTestFlags = interpreterTestFlags;
                result.Add("interpreterTestFlags");
            }

            PluginFlags pluginFlags = sourceInterpreterSettings.PluginFlags;

            if (forceMissing || (pluginFlags != PluginFlags.None))
            {
                targetInterpreterSettings.PluginFlags = pluginFlags;
                result.Add("pluginFlags");
            }

#if NATIVE && TCL
            FindFlags findFlags = sourceInterpreterSettings.FindFlags;

            if (forceMissing || (findFlags != FindFlags.None))
            {
                targetInterpreterSettings.FindFlags = findFlags;
                result.Add("findFlags");
            }

            LoadFlags loadFlags = sourceInterpreterSettings.LoadFlags;

            if (forceMissing || (loadFlags != LoadFlags.None))
            {
                targetInterpreterSettings.LoadFlags = loadFlags;
                result.Add("loadFlags");
            }
#endif

            AppDomain appDomain = sourceInterpreterSettings.AppDomain;

            if (forceMissing || (appDomain != null))
            {
                targetInterpreterSettings.AppDomain = appDomain;
                result.Add("appDomain");
            }

            IHost host = sourceInterpreterSettings.Host;

            if (forceMissing || (host != null))
            {
                targetInterpreterSettings.Host = host;
                result.Add("host");
            }

            string profile = sourceInterpreterSettings.Profile;

            if (forceMissing || (profile != null))
            {
                targetInterpreterSettings.Profile = profile;
                result.Add("profile");
            }

            object owner = sourceInterpreterSettings.Owner;

            if (forceMissing || (owner != null))
            {
                targetInterpreterSettings.Owner = owner;
                result.Add("owner");
            }

            object applicationObject = sourceInterpreterSettings.ApplicationObject;

            if (forceMissing || (applicationObject != null))
            {
                targetInterpreterSettings.ApplicationObject = applicationObject;
                result.Add("applicationObject");
            }

            object policyObject = sourceInterpreterSettings.PolicyObject;

            if (forceMissing || (policyObject != null))
            {
                targetInterpreterSettings.PolicyObject = policyObject;
                result.Add("policyObject");
            }

            object resolverObject = sourceInterpreterSettings.ResolverObject;

            if (forceMissing || (resolverObject != null))
            {
                targetInterpreterSettings.ResolverObject = resolverObject;
                result.Add("resolverObject");
            }

            object userObject = sourceInterpreterSettings.UserObject;

            if (forceMissing || (userObject != null))
            {
                targetInterpreterSettings.UserObject = userObject;
                result.Add("userObject");
            }

            PolicyList policies = sourceInterpreterSettings.Policies;

            if (forceMissing || (policies != null))
            {
                targetInterpreterSettings.Policies = policies;
                result.Add("policies");
            }

            TraceList traces = sourceInterpreterSettings.Traces;

            if (forceMissing || (traces != null))
            {
                targetInterpreterSettings.Traces = traces;
                result.Add("traces");
            }

            string text = sourceInterpreterSettings.Text;

            if (forceMissing || (text != null))
            {
                targetInterpreterSettings.Text = text;
                result.Add("text");
            }

            string libraryPath = sourceInterpreterSettings.LibraryPath;

            if (forceMissing || (libraryPath != null))
            {
                targetInterpreterSettings.LibraryPath = libraryPath;
                result.Add("libraryPath");
            }

            StringList autoPathList = sourceInterpreterSettings.AutoPathList;

            if (forceMissing || (autoPathList != null))
            {
                targetInterpreterSettings.AutoPathList = autoPathList;
                result.Add("autoPathList");
            }

            return result;
        }

        ///////////////////////////////////////////////////////////////////////

        private static ReturnCode LoadFromIni(
            string fileName,                              /* in */
            Stream stream,                                /* in */
            CultureInfo cultureInfo,                      /* in */
            bool merge,                                   /* in */
            bool expand,                                  /* in */
            ref IInterpreterSettings interpreterSettings, /* out */
            ref Result error                              /* out */
            )
        {
            return SettingsOps.LoadForInterpreter(
                null, fileName, stream, cultureInfo,
                merge, expand, ref interpreterSettings,
                ref error);
        }

        ///////////////////////////////////////////////////////////////////////

        private static ReturnCode LoadFromIni(
            string fileName,                              /* in */
            CultureInfo cultureInfo,                      /* in */
            bool merge,                                   /* in */
            bool expand,                                  /* in */
            ref IInterpreterSettings interpreterSettings, /* out */
            ref Result error                              /* out */
            )
        {
            return SettingsOps.LoadForInterpreter(
                null, fileName, cultureInfo, merge,
                expand, ref interpreterSettings,
                ref error);
        }

        ///////////////////////////////////////////////////////////////////////

        private static ReturnCode SaveToIni(
            string fileName,                          /* in */
            bool expand,                              /* in */
            IInterpreterSettings interpreterSettings, /* in */
            ref Result error                          /* out */
            )
        {
            return SettingsOps.SaveForInterpreter(null,
                fileName, expand, interpreterSettings,
                ref error);
        }

        ///////////////////////////////////////////////////////////////////////

#if XML
        private static ReturnCode FromDocument(
            XmlDocument document,                     /* in */
            CultureInfo cultureInfo,                  /* in */
            IInterpreterSettings interpreterSettings, /* in, out */
            ref Result error                          /* out */
            )
        {
            if (document == null)
            {
                error = "invalid xml document";
                return ReturnCode.Error;
            }

            if (interpreterSettings == null)
            {
                error = "invalid interpreter settings";
                return ReturnCode.Error;
            }

            XmlElement documentElement = document.DocumentElement;

            if (documentElement == null)
            {
                error = "invalid xml document element";
                return ReturnCode.Error;
            }

            XmlNode node;
            StringList list; /* REUSED */
            object enumValue; /* REUSED */

            node = documentElement.SelectSingleNode("RuleSet");

            if ((node != null) && !String.IsNullOrEmpty(node.InnerText))
            {
                IRuleSet ruleSet = _RuleSet.Create(
                    node.InnerText, cultureInfo, ref error);

                if (ruleSet == null)
                    return ReturnCode.Error;

                interpreterSettings.RuleSet = ruleSet;
            }

            node = documentElement.SelectSingleNode("Args");

            if ((node != null) && !String.IsNullOrEmpty(node.InnerText))
            {
                list = null;

                if (ParserOps<string>.SplitList(
                        null, node.InnerText, 0, Length.Invalid, false,
                        ref list, ref error) == ReturnCode.Ok)
                {
                    interpreterSettings.Args = list;
                }
                else
                {
                    return ReturnCode.Error;
                }
            }

            node = documentElement.SelectSingleNode("CreateFlags");

            if ((node != null) && !String.IsNullOrEmpty(node.InnerText))
            {
                enumValue = EnumOps.TryParseFlags(
                    null, typeof(CreateFlags),
                    interpreterSettings.CreateFlags.ToString(),
                    node.InnerText, cultureInfo, true, true, true,
                    ref error);

                if (enumValue is CreateFlags)
                    interpreterSettings.CreateFlags = (CreateFlags)enumValue;
                else
                    return ReturnCode.Error;
            }

            node = documentElement.SelectSingleNode("HostCreateFlags");

            if ((node != null) && !String.IsNullOrEmpty(node.InnerText))
            {
                enumValue = EnumOps.TryParseFlags(
                    null, typeof(HostCreateFlags),
                    interpreterSettings.HostCreateFlags.ToString(),
                    node.InnerText, cultureInfo, true, true, true,
                    ref error);

                if (enumValue is HostCreateFlags)
                    interpreterSettings.HostCreateFlags = (HostCreateFlags)enumValue;
                else
                    return ReturnCode.Error;
            }

            node = documentElement.SelectSingleNode("InitializeFlags");

            if ((node != null) && !String.IsNullOrEmpty(node.InnerText))
            {
                enumValue = EnumOps.TryParseFlags(
                    null, typeof(InitializeFlags),
                    interpreterSettings.InitializeFlags.ToString(),
                    node.InnerText, cultureInfo, true, true, true,
                    ref error);

                if (enumValue is InitializeFlags)
                    interpreterSettings.InitializeFlags = (InitializeFlags)enumValue;
                else
                    return ReturnCode.Error;
            }

            node = documentElement.SelectSingleNode("ScriptFlags");

            if ((node != null) && !String.IsNullOrEmpty(node.InnerText))
            {
                enumValue = EnumOps.TryParseFlags(
                    null, typeof(ScriptFlags),
                    interpreterSettings.ScriptFlags.ToString(),
                    node.InnerText, cultureInfo, true, true, true,
                    ref error);

                if (enumValue is ScriptFlags)
                    interpreterSettings.ScriptFlags = (ScriptFlags)enumValue;
                else
                    return ReturnCode.Error;
            }

            node = documentElement.SelectSingleNode("InterpreterFlags");

            if ((node != null) && !String.IsNullOrEmpty(node.InnerText))
            {
                enumValue = EnumOps.TryParseFlags(
                    null, typeof(InterpreterFlags),
                    interpreterSettings.InterpreterFlags.ToString(),
                    node.InnerText, cultureInfo, true, true, true,
                    ref error);

                if (enumValue is InterpreterFlags)
                    interpreterSettings.InterpreterFlags = (InterpreterFlags)enumValue;
                else
                    return ReturnCode.Error;
            }

            node = documentElement.SelectSingleNode("InterpreterTestFlags");

            if ((node != null) && !String.IsNullOrEmpty(node.InnerText))
            {
                enumValue = EnumOps.TryParseFlags(
                    null, typeof(InterpreterTestFlags),
                    interpreterSettings.InterpreterTestFlags.ToString(),
                    node.InnerText, cultureInfo, true, true, true,
                    ref error);

                if (enumValue is InterpreterTestFlags)
                    interpreterSettings.InterpreterTestFlags = (InterpreterTestFlags)enumValue;
                else
                    return ReturnCode.Error;
            }

            node = documentElement.SelectSingleNode("PluginFlags");

            if ((node != null) && !String.IsNullOrEmpty(node.InnerText))
            {
                enumValue = EnumOps.TryParseFlags(
                    null, typeof(PluginFlags),
                    interpreterSettings.PluginFlags.ToString(),
                    node.InnerText, cultureInfo, true, true, true,
                    ref error);

                if (enumValue is PluginFlags)
                    interpreterSettings.PluginFlags = (PluginFlags)enumValue;
                else
                    return ReturnCode.Error;
            }

#if NATIVE && TCL
            node = documentElement.SelectSingleNode("FindFlags");

            if ((node != null) && !String.IsNullOrEmpty(node.InnerText))
            {
                enumValue = EnumOps.TryParseFlags(
                    null, typeof(FindFlags),
                    interpreterSettings.FindFlags.ToString(),
                    node.InnerText, cultureInfo, true, true, true,
                    ref error);

                if (enumValue is FindFlags)
                    interpreterSettings.FindFlags = (FindFlags)enumValue;
                else
                    return ReturnCode.Error;
            }

            node = documentElement.SelectSingleNode("LoadFlags");

            if ((node != null) && !String.IsNullOrEmpty(node.InnerText))
            {
                enumValue = EnumOps.TryParseFlags(
                    null, typeof(LoadFlags),
                    interpreterSettings.LoadFlags.ToString(),
                    node.InnerText, cultureInfo, true, true, true,
                    ref error);

                if (enumValue is LoadFlags)
                    interpreterSettings.LoadFlags = (LoadFlags)enumValue;
                else
                    return ReturnCode.Error;
            }
#endif

            node = documentElement.SelectSingleNode("AutoPathList");

            if ((node != null) && !String.IsNullOrEmpty(node.InnerText))
            {
                list = null;

                if (ParserOps<string>.SplitList(
                        null, node.InnerText, 0, Length.Invalid, false,
                        ref list, ref error) == ReturnCode.Ok)
                {
                    interpreterSettings.AutoPathList = list;
                }
                else
                {
                    return ReturnCode.Error;
                }
            }

            return ReturnCode.Ok;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        private static ReturnCode ToDocument(
            XmlDocument document,                     /* in */
            IInterpreterSettings interpreterSettings, /* in */
            ref Result error                          /* out */
            )
        {
            if (document == null)
            {
                error = "invalid xml document";
                return ReturnCode.Error;
            }

            if (interpreterSettings == null)
            {
                error = "invalid interpreter settings";
                return ReturnCode.Error;
            }

            XmlElement documentElement = document.DocumentElement;

            if (documentElement == null)
            {
                error = "invalid xml document element";
                return ReturnCode.Error;
            }

            XmlNode node;

            if (interpreterSettings.RuleSet != null)
            {
                node = document.CreateElement("RuleSet");
                node.InnerText = interpreterSettings.RuleSet.ToString();
                documentElement.AppendChild(node);
            }

            if (interpreterSettings.Args != null)
            {
                node = document.CreateElement("Args");

                node.InnerText = new StringList(
                    interpreterSettings.Args).ToString();

                documentElement.AppendChild(node);
            }

            node = document.CreateElement("CreateFlags");
            node.InnerText = interpreterSettings.CreateFlags.ToString();
            documentElement.AppendChild(node);

            node = document.CreateElement("HostCreateFlags");
            node.InnerText = interpreterSettings.HostCreateFlags.ToString();
            documentElement.AppendChild(node);

            node = document.CreateElement("InitializeFlags");
            node.InnerText = interpreterSettings.InitializeFlags.ToString();
            documentElement.AppendChild(node);

            node = document.CreateElement("ScriptFlags");
            node.InnerText = interpreterSettings.ScriptFlags.ToString();
            documentElement.AppendChild(node);

            node = document.CreateElement("InterpreterFlags");
            node.InnerText = interpreterSettings.InterpreterFlags.ToString();
            documentElement.AppendChild(node);

            node = document.CreateElement("InterpreterTestFlags");
            node.InnerText = interpreterSettings.InterpreterTestFlags.ToString();
            documentElement.AppendChild(node);

            node = document.CreateElement("PluginFlags");
            node.InnerText = interpreterSettings.PluginFlags.ToString();
            documentElement.AppendChild(node);

#if NATIVE && TCL
            node = document.CreateElement("FindFlags");
            node.InnerText = interpreterSettings.FindFlags.ToString();
            documentElement.AppendChild(node);

            node = document.CreateElement("LoadFlags");
            node.InnerText = interpreterSettings.LoadFlags.ToString();
            documentElement.AppendChild(node);
#endif

            if (interpreterSettings.AutoPathList != null)
            {
                node = document.CreateElement("AutoPathList");
                node.InnerText = interpreterSettings.AutoPathList.ToString();
                documentElement.AppendChild(node);
            }

            return ReturnCode.Ok;
        }

        ///////////////////////////////////////////////////////////////////////

#if SERIALIZATION
        private static ReturnCode LoadFromXml(
            XmlDocument document,                         /* in */
            CultureInfo cultureInfo,                      /* in */
            bool merge,                                   /* in */
            bool expand,                                  /* in */
            ref IInterpreterSettings interpreterSettings, /* out */
            ref Result error                              /* out */
            )
        {
            if (document == null)
            {
                error = "invalid xml document";
                return ReturnCode.Error;
            }

            if (!merge && (interpreterSettings != null))
            {
                error = "cannot overwrite valid interpreter settings";
                return ReturnCode.Error;
            }

            //
            // NOTE: The XmlNodeReader constructor call here cannot
            //       throw an exception.  The document was already
            //       checked for null (above) and there is no other
            //       way for the XmlNodeReader constructor to throw.
            //
            using (XmlNodeReader reader = new XmlNodeReader(document))
            {
                object @object = null;

                if (XmlOps.Deserialize(
                        typeof(InterpreterSettings), reader,
                        ref @object, ref error) == ReturnCode.Ok)
                {
                    IInterpreterSettings documentInterpreterSettings =
                        @object as IInterpreterSettings;

                    if (FromDocument(document,
                            cultureInfo, documentInterpreterSettings,
                            ref error) == ReturnCode.Ok)
                    {
                        if (expand)
                            Expand(documentInterpreterSettings);

                        IInterpreterSettings newInterpreterSettings;

                        if (merge && (interpreterSettings != null))
                            newInterpreterSettings = interpreterSettings;
                        else
                            newInterpreterSettings = new InterpreterSettings();

                        StringList merged = Copy(
                            documentInterpreterSettings,
                            newInterpreterSettings, false);

                        TraceOps.DebugTrace(String.Format(
                            "LoadFromXml: merged = {0}",
                            FormatOps.WrapOrNull(merged)),
                            typeof(InterpreterSettings).Name,
                            TracePriority.StartupDebug3);

                        interpreterSettings = newInterpreterSettings;
                        return ReturnCode.Ok;
                    }
                }
            }

            return ReturnCode.Error;
        }

        ///////////////////////////////////////////////////////////////////////

        private static ReturnCode LoadFromXml(
            string fileName,                              /* in */
            Stream stream,                                /* in */
            CultureInfo cultureInfo,                      /* in */
            bool merge,                                   /* in */
            bool expand,                                  /* in */
            ref IInterpreterSettings interpreterSettings, /* out */
            ref Result error                              /* out */
            )
        {
            if (stream == null)
            {
                error = "invalid stream";
                return ReturnCode.Error;
            }

            XmlDocument document = null;

            try
            {
                document = new XmlDocument();
                document.Load(stream); /* throw */
            }
            catch (Exception e)
            {
                error = e;
                return ReturnCode.Error;
            }

            return LoadFromXml(
                document, cultureInfo, merge, expand,
                ref interpreterSettings, ref error);
        }

        ///////////////////////////////////////////////////////////////////////

        private static ReturnCode LoadFromXml(
            string fileName,                              /* in */
            CultureInfo cultureInfo,                      /* in */
            bool merge,                                   /* in */
            bool expand,                                  /* in */
            ref IInterpreterSettings interpreterSettings, /* out */
            ref Result error                              /* out */
            )
        {
            if (String.IsNullOrEmpty(fileName))
            {
                error = "invalid file name";
                return ReturnCode.Error;
            }

            if (!File.Exists(fileName))
            {
                error = String.Format(
                    "cannot read \"{0}\": no such file",
                    fileName);

                return ReturnCode.Error;
            }

            XmlDocument document = null;

            try
            {
                document = new XmlDocument();
                document.Load(fileName); /* throw */
            }
            catch (Exception e)
            {
                error = e;
                return ReturnCode.Error;
            }

            return LoadFromXml(
                document, cultureInfo, merge, expand,
                ref interpreterSettings, ref error);
        }

        ///////////////////////////////////////////////////////////////////////

        private static ReturnCode SaveToXml(
            string fileName,                          /* in */
            bool expand,                              /* in */
            IInterpreterSettings interpreterSettings, /* in */
            ref Result error                          /* out */
            )
        {
            if (String.IsNullOrEmpty(fileName))
            {
                error = "invalid file name";
                return ReturnCode.Error;
            }

            if (File.Exists(fileName))
            {
                error = String.Format(
                    "cannot write \"{0}\": file already exists",
                    fileName);

                return ReturnCode.Error;
            }

            if (interpreterSettings == null)
            {
                error = "invalid interpreter settings";
                return ReturnCode.Error;
            }

            try
            {
                using (Stream stream = new FileStream(fileName,
                        FileMode.CreateNew, FileAccess.Write)) /* EXEMPT */
                {
                    using (MemoryStream stream2 = new MemoryStream())
                    {
                        using (XmlTextWriter writer = new XmlTextWriter(
                                stream2, null))
                        {
                            if (expand)
                                Expand(interpreterSettings);

                            if (XmlOps.Serialize(
                                    interpreterSettings,
                                    typeof(InterpreterSettings), writer,
                                    null, ref error) != ReturnCode.Ok)
                            {
                                return ReturnCode.Error;
                            }

                            writer.Flush();

                            XmlDocument document;

                            using (MemoryStream stream3 = new MemoryStream(
                                    stream2.ToArray(), false))
                            {
                                writer.Close();

                                document = new XmlDocument();
                                document.Load(stream3);
                            }

                            if (ToDocument(
                                    document, interpreterSettings,
                                    ref error) != ReturnCode.Ok)
                            {
                                return ReturnCode.Error;
                            }

                            XmlWriterSettings writerSettings =
                                new XmlWriterSettings();

                            writerSettings.Indent = true;

                            using (XmlWriter writer2 = XmlWriter.Create(
                                    stream, writerSettings))
                            {
                                document.WriteTo(writer2);
                            }

                            return ReturnCode.Ok;
                        }
                    }
                }
            }
            catch (Exception e)
            {
                error = e;
            }

            return ReturnCode.Error;
        }
#endif
#endif

        ///////////////////////////////////////////////////////////////////////

        private static void CheckPoliciesAndTraces(
            IInterpreterSettings interpreterSettings, /* in, out */
            PolicyList policies,                      /* in */
            TraceList traces                          /* in */
            )
        {
            if (interpreterSettings != null)
            {
                CreateFlags createFlags = interpreterSettings.CreateFlags;

                if ((policies != null) &&
                    PolicyOps.HasExecuteCallbacks(policies))
                {
                    createFlags |= CreateFlags.NoCorePolicies;
                }

                if ((traces != null) &&
                    Interpreter.HasTraceCallbacks(traces, true))
                {
                    createFlags |= CreateFlags.NoCoreTraces;
                }

                interpreterSettings.CreateFlags = createFlags;
            }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Internal Static Methods
        internal static void CheckPoliciesAndTraces(
            IInterpreterSettings interpreterSettings /* in, out */
            )
        {
            if (interpreterSettings != null)
            {
                CheckPoliciesAndTraces(
                    interpreterSettings, interpreterSettings.Policies,
                    interpreterSettings.Traces);
            }
        }

        ///////////////////////////////////////////////////////////////////////

        internal static ReturnCode UseStartupDefaults(
            IInterpreterSettings interpreterSettings, /* in, out */
            CreateFlags createFlags,                  /* in */
            HostCreateFlags hostCreateFlags,          /* in */
            ref Result error                          /* out */
            )
        {
            if (interpreterSettings == null)
            {
                error = "invalid interpreter settings";
                return ReturnCode.Error;
            }

            //
            // NOTE: Use the creation flags specified by the caller,
            //       ignoring the creation flags in the interpreter
            //       settings.
            //
            interpreterSettings.CreateFlags = createFlags;
            interpreterSettings.HostCreateFlags = hostCreateFlags;

            //
            // NOTE: If there are existing policies and/or traces, make
            //       sure creation flags are modified to skip adding the
            //       default policies and/or traces during interpreter
            //       creation.
            //
            CheckPoliciesAndTraces(interpreterSettings,
                interpreterSettings.Policies, interpreterSettings.Traces);

            //
            // NOTE: The interpreter host may be disposed now -OR- may
            //       end up being disposed later, so avoid copying it.
            //
            interpreterSettings.Host = null;

            //
            // NOTE: Nulling these out should not be necessary when the
            //       creation flags are modified to skip adding default
            //       policies and traces (above).
            //
            // interpreterSettings.Policies = null;
            // interpreterSettings.Traces = null;

            //
            // NOTE: These startup settings are reset by this method to
            //       avoid having their values used when the command line
            //       arguments have been "locked" by the interpreter host.
            //
            interpreterSettings.Text = null;
            interpreterSettings.LibraryPath = null;

            return ReturnCode.Ok;
        }

        ///////////////////////////////////////////////////////////////////////

#if SHELL
        internal static ReturnCode UseShellDefaults(
            IInterpreterSettings interpreterSettings, /* in, out */
            CreateFlags createFlags,                  /* in */
            ref Result error                          /* out */
            )
        {
            if (interpreterSettings == null)
            {
                error = "invalid interpreter settings";
                return ReturnCode.Error;
            }

            InterpreterFlags interpreterFlags =
                interpreterSettings.InterpreterFlags;

            interpreterFlags |= InterpreterFlags.ForShellUse;

            if (FlagOps.HasFlags(
                    createFlags, CreateFlags.Safe, true))
            {
                //
                // HACK: Remove interpreter flags that are not
                //       designed for "safe" interpreters.
                //
                interpreterFlags &= ~InterpreterFlags.UnsafeMask;
            }

            InterpreterTestFlags interpreterTestFlags =
                interpreterSettings.InterpreterTestFlags;

            interpreterTestFlags |= InterpreterTestFlags.ForShellUse;

            if (FlagOps.HasFlags(
                    createFlags, CreateFlags.Safe, true))
            {
                //
                // HACK: Remove interpreter test flags that are
                //       not designed for "safe" interpreters.
                //
                interpreterTestFlags &= ~InterpreterTestFlags.UnsafeMask;
            }

            interpreterSettings.InterpreterFlags = interpreterFlags;
            interpreterSettings.InterpreterTestFlags = interpreterTestFlags;

            return ReturnCode.Ok;
        }
#endif

        ///////////////////////////////////////////////////////////////////////

        internal static ReturnCode LoadFrom(
            CultureInfo cultureInfo,                      /* in */
            bool optional,                                /* in */
            bool merge,                                   /* in */
            bool expand,                                  /* in */
            ref IInterpreterSettings interpreterSettings, /* out */
            ref Result error                              /* out */
            )
        {
            string baseFileName = PathOps.GetManagedExecutableName();

            string[] fileNames = {
                String.Format(
                    LoadFromFileNameFormat, baseFileName,
                    FileExtension.Configuration),
#if XML && SERIALIZATION
                String.Format(
                    LoadFromFileNameFormat, baseFileName,
                    FileExtension.Markup),
#endif
                String.Format(
                    LoadFromFileNameFormat, baseFileName,
                    FileExtension.Profile)
            };

            foreach (string fileName in fileNames)
            {
                if (String.IsNullOrEmpty(fileName))
                    continue;

                if (File.Exists(fileName))
                {
                    return LoadFrom(
                        fileName, cultureInfo, merge, expand,
                        ref interpreterSettings, ref error);
                }
            }

            if (optional)
            {
                return ReturnCode.Ok;
            }
            else
            {
                error = String.Format(
                    "cannot read \"{0}\": no such file",
                    String.Format(LoadFromFileNameFormat,
                    baseFileName, FileExtension.Any));

                return ReturnCode.Error;
            }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Public Static Methods
        public static ReturnCode LoadFrom(
            Interpreter interpreter,                      /* in */
            bool expand,                                  /* in */
            bool recreate,                                /* in */
            bool full,                                    /* in */
            ref IInterpreterSettings interpreterSettings, /* out */
            ref Result error                              /* out */
            )
        {
            if (interpreter == null)
            {
                error = "invalid interpreter";
                return ReturnCode.Error;
            }

            if (interpreterSettings != null)
            {
                error = "cannot overwrite valid interpreter settings";
                return ReturnCode.Error;
            }

            try
            {
                IInterpreterSettings newInterpreterSettings = Create();

                if (FromInterpreter(
                        interpreter, newInterpreterSettings,
                        recreate, full, ref error) == ReturnCode.Ok)
                {
                    if (expand)
                        Expand(newInterpreterSettings);

                    interpreterSettings = newInterpreterSettings;
                    return ReturnCode.Ok;
                }
            }
            catch (Exception e)
            {
                error = e;
            }

            return ReturnCode.Error;
        }

        ///////////////////////////////////////////////////////////////////////

        public static ReturnCode LoadFrom(
            string fileName,                              /* in */
            CultureInfo cultureInfo,                      /* in */
            bool merge,                                   /* in */
            bool expand,                                  /* in */
            ref IInterpreterSettings interpreterSettings, /* out */
            ref Result error                              /* out */
            )
        {
            bool couldBeDocument = CouldBeDocument(fileName);

#if XML && SERIALIZATION
            if (XmlOps.CouldBeDocument(fileName) ||
                (couldBeDocument && XmlOps.FileLooksLikeDocument(fileName)))
            {
                return LoadFromXml(
                    fileName, cultureInfo, merge, expand,
                    ref interpreterSettings, ref error);
            }
#endif

            if (SettingsOps.CouldBeDocument(fileName) || couldBeDocument)
            {
                return LoadFromIni(
                    fileName, cultureInfo, merge, expand,
                    ref interpreterSettings, ref error);
            }

            error = "unsupported settings file format";
            return ReturnCode.Error;
        }

        ///////////////////////////////////////////////////////////////////////

        public static ReturnCode LoadFrom(
            string fileName,                              /* in */
            Stream stream,                                /* in */
            CultureInfo cultureInfo,                      /* in */
            bool merge,                                   /* in */
            bool expand,                                  /* in */
            ref IInterpreterSettings interpreterSettings, /* out */
            ref Result error                              /* out */
            )
        {
            bool couldBeDocument = CouldBeDocument(fileName);

#if XML && SERIALIZATION
            if (XmlOps.CouldBeDocument(fileName) ||
                (couldBeDocument && XmlOps.FileLooksLikeDocument(fileName)))
            {
                return LoadFromXml(fileName,
                    stream, cultureInfo, merge, expand,
                    ref interpreterSettings, ref error);
            }
#endif

            if (SettingsOps.CouldBeDocument(fileName) || couldBeDocument)
            {
                return LoadFromIni(fileName,
                    stream, cultureInfo, merge, expand,
                    ref interpreterSettings, ref error);
            }

            error = "unsupported settings file format";
            return ReturnCode.Error;
        }

        ///////////////////////////////////////////////////////////////////////

        public static ReturnCode SaveTo(
            string fileName,                          /* in */
            bool expand,                              /* in */
            IInterpreterSettings interpreterSettings, /* in */
            ref Result error                          /* out */
            )
        {
            bool couldBeDocument = CouldBeDocument(fileName);

#if XML && SERIALIZATION
            if (XmlOps.CouldBeDocument(fileName) ||
                (couldBeDocument && XmlOps.FileLooksLikeDocument(fileName)))
            {
                return SaveToXml(
                    fileName, expand, interpreterSettings,
                    ref error);
            }
#endif

            if (SettingsOps.CouldBeDocument(fileName) || couldBeDocument)
            {
                return SaveToIni(
                    fileName, expand, interpreterSettings,
                    ref error);
            }

            error = "unsupported settings file format";
            return ReturnCode.Error;
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region IInterpreterSettingsData Members
#if XML && SERIALIZATION
        [XmlIgnore()]
#endif
        public IRuleSet RuleSet
        {
            get { return ruleSet; }
            set { ruleSet = value; }
        }

        ///////////////////////////////////////////////////////////////////////

#if XML && SERIALIZATION
        [XmlIgnore()]
#endif
        public IEnumerable<string> Args
        {
            get { return args; }
            set { args = value; }
        }

        ///////////////////////////////////////////////////////////////////////

        public string Culture
        {
            get { return culture; }
            set { culture = value; }
        }

        ///////////////////////////////////////////////////////////////////////

#if XML && SERIALIZATION
        [XmlIgnore()]
#endif
        public CreateFlags CreateFlags
        {
            get { return createFlags; }
            set { createFlags = value; }
        }

        ///////////////////////////////////////////////////////////////////////

#if XML && SERIALIZATION
        [XmlIgnore()]
#endif
        public HostCreateFlags HostCreateFlags
        {
            get { return hostCreateFlags; }
            set { hostCreateFlags = value; }
        }

        ///////////////////////////////////////////////////////////////////////

#if XML && SERIALIZATION
        [XmlIgnore()]
#endif
        public InitializeFlags InitializeFlags
        {
            get { return initializeFlags; }
            set { initializeFlags = value; }
        }

        ///////////////////////////////////////////////////////////////////////

#if XML && SERIALIZATION
        [XmlIgnore()]
#endif
        public ScriptFlags ScriptFlags
        {
            get { return scriptFlags; }
            set { scriptFlags = value; }
        }

        ///////////////////////////////////////////////////////////////////////

#if XML && SERIALIZATION
        [XmlIgnore()]
#endif
        public InterpreterFlags InterpreterFlags
        {
            get { return interpreterFlags; }
            set { interpreterFlags = value; }
        }

        ///////////////////////////////////////////////////////////////////////

#if XML && SERIALIZATION
        [XmlIgnore()]
#endif
        public InterpreterTestFlags InterpreterTestFlags
        {
            get { return interpreterTestFlags; }
            set { interpreterTestFlags = value; }
        }

        ///////////////////////////////////////////////////////////////////////

#if XML && SERIALIZATION
        [XmlIgnore()]
#endif
        public PluginFlags PluginFlags
        {
            get { return pluginFlags; }
            set { pluginFlags = value; }
        }

        ///////////////////////////////////////////////////////////////////////

#if NATIVE && TCL
#if XML && SERIALIZATION
        [XmlIgnore()]
#endif
        public FindFlags FindFlags
        {
            get { return findFlags; }
            set { findFlags = value; }
        }

        ///////////////////////////////////////////////////////////////////////

#if XML && SERIALIZATION
        [XmlIgnore()]
#endif
        public LoadFlags LoadFlags
        {
            get { return loadFlags; }
            set { loadFlags = value; }
        }
#endif

        ///////////////////////////////////////////////////////////////////////

#if XML && SERIALIZATION
        [XmlIgnore()]
#endif
        public AppDomain AppDomain
        {
            get { return appDomain; }
            set { appDomain = value; }
        }

        ///////////////////////////////////////////////////////////////////////

#if XML && SERIALIZATION
        [XmlIgnore()]
#endif
        public IHost Host
        {
            get { return host; }
            set { host = value; }
        }

        ///////////////////////////////////////////////////////////////////////

        public string Profile
        {
            get { return profile; }
            set { profile = value; }
        }

        ///////////////////////////////////////////////////////////////////////

#if XML && SERIALIZATION
        [XmlIgnore()]
#endif
        public object Owner
        {
            get { return owner; }
            set { owner = value; }
        }

        ///////////////////////////////////////////////////////////////////////

#if XML && SERIALIZATION
        [XmlIgnore()]
#endif
        public object ApplicationObject
        {
            get { return applicationObject; }
            set { applicationObject = value; }
        }

        ///////////////////////////////////////////////////////////////////////

#if XML && SERIALIZATION
        [XmlIgnore()]
#endif
        public object PolicyObject
        {
            get { return policyObject; }
            set { policyObject = value; }
        }

        ///////////////////////////////////////////////////////////////////////

#if XML && SERIALIZATION
        [XmlIgnore()]
#endif
        public object ResolverObject
        {
            get { return resolverObject; }
            set { resolverObject = value; }
        }

        ///////////////////////////////////////////////////////////////////////

#if XML && SERIALIZATION
        [XmlIgnore()]
#endif
        public object UserObject
        {
            get { return userObject; }
            set { userObject = value; }
        }

        ///////////////////////////////////////////////////////////////////////

#if XML && SERIALIZATION
        [XmlIgnore()]
#endif
        public PolicyList Policies
        {
            get { return policies; }
            set { policies = value; }
        }

        ///////////////////////////////////////////////////////////////////////

#if XML && SERIALIZATION
        [XmlIgnore()]
#endif
        public TraceList Traces
        {
            get { return traces; }
            set { traces = value; }
        }

        ///////////////////////////////////////////////////////////////////////

        public string Text
        {
            get { return text; }
            set { text = value; }
        }

        ///////////////////////////////////////////////////////////////////////

        public string LibraryPath
        {
            get { return libraryPath; }
            set { libraryPath = value; }
        }

        ///////////////////////////////////////////////////////////////////////

#if XML && SERIALIZATION
        [XmlIgnore()]
#endif
        public StringList AutoPathList
        {
            get { return autoPathList; }
            set { autoPathList = value; }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region IInterpreterSettings Members
        public void MakeSafe() /* DO NOT USE: PrivateShellMainCore ONLY. */
        {
            createFlags |= CreateFlags.SafeAndHideUnsafe;
        }

        ///////////////////////////////////////////////////////////////////////

        public void MakeStandard() /* DO NOT USE: PrivateShellMainCore ONLY. */
        {
            createFlags |= CreateFlags.StandardAndHideNonStandard;
        }

        ///////////////////////////////////////////////////////////////////////

        public void DisableInitialize()
        {
            createFlags &= ~CreateFlags.Initialize;
        }

        ///////////////////////////////////////////////////////////////////////

        public void EnableNamespaces() /* DO NOT USE: TESTS ONLY. */
        {
            createFlags |= CreateFlags.UseNamespaces;
        }

        ///////////////////////////////////////////////////////////////////////

        public void DisableNamespaces() /* DO NOT USE: TESTS ONLY. */
        {
            createFlags &= ~CreateFlags.UseNamespaces;
        }

        ///////////////////////////////////////////////////////////////////////

        public void DisableLoader() /* DO NOT USE: TESTS ONLY. */
        {
            initializeFlags &= ~InitializeFlags.Loader;
        }

        ///////////////////////////////////////////////////////////////////////

        public void DisableInitialization() /* DO NOT USE: TESTS ONLY. */
        {
            initializeFlags &= ~InitializeFlags.Initialization;
        }

        ///////////////////////////////////////////////////////////////////////

        public void DisableSetAutoPath() /* DO NOT USE: TESTS ONLY. */
        {
            initializeFlags &= ~InitializeFlags.SetAutoPath;
        }

        ///////////////////////////////////////////////////////////////////////

        public void RemoveUnsafeOptions() /* DO NOT USE: TESTS ONLY. */
        {
            interpreterFlags &= ~InterpreterFlags.UnsafeMask;
        }

        ///////////////////////////////////////////////////////////////////////

        public void RemoveUnsafeTestOptions() /* DO NOT USE: TESTS ONLY. */
        {
            interpreterTestFlags &= ~InterpreterTestFlags.UnsafeMask;
        }

        ///////////////////////////////////////////////////////////////////////

        public void EnableSecurity() /* DO NOT USE: TESTS ONLY. */
        {
            initializeFlags |= InitializeFlags.Security;
        }

        ///////////////////////////////////////////////////////////////////////

        public void DisableSecurity() /* DO NOT USE: TESTS ONLY. */
        {
            initializeFlags &= ~InitializeFlags.Security;
        }

        ///////////////////////////////////////////////////////////////////////

        public void ResetEverything()
        {
            //
            // TODO: Update whenever the list of fields is updated.
            //
            ruleSet = null;
            args = null;
            culture = null;
            createFlags = CreateFlags.None;
            hostCreateFlags = HostCreateFlags.None;
            initializeFlags = InitializeFlags.None;
            scriptFlags = ScriptFlags.None;
            interpreterFlags = InterpreterFlags.None;
            interpreterTestFlags = InterpreterTestFlags.None;
            pluginFlags = PluginFlags.None;

#if NATIVE && TCL
            findFlags = FindFlags.None;
            loadFlags = LoadFlags.None;
#endif

            appDomain = null;
            host = null;
            profile = null;
            owner = null;
            applicationObject = null;
            policyObject = null;
            resolverObject = null;
            userObject = null;
            policies = null;
            traces = null;
            text = null;
            libraryPath = null;
            autoPathList = null;
        }

        ///////////////////////////////////////////////////////////////////////

        public void UseDefaultsForFlags()
        {
            //
            // TODO: Update whenever the list of flags fields is updated.
            //
            createFlags = Defaults.CreateFlags;
            hostCreateFlags = Defaults.HostCreateFlags;
            initializeFlags = Defaults.InitializeFlags;
            scriptFlags = Defaults.ScriptFlags;
            interpreterFlags = Defaults.InterpreterFlags;
            interpreterTestFlags = Defaults.InterpreterTestFlags;
            pluginFlags = Defaults.PluginFlags;

#if NATIVE && TCL
            findFlags = Defaults.FindFlags;
            loadFlags = Defaults.LoadFlags;
#endif
        }

        ///////////////////////////////////////////////////////////////////////

        public void UseFlagsFromInterpreter(
            Interpreter interpreter /* in */
            )
        {
            if (interpreter == null)
                return;

            createFlags = interpreter.CreateFlags;
            hostCreateFlags = interpreter.HostCreateFlags;
            initializeFlags = interpreter.InitializeFlags;
            scriptFlags = interpreter.ScriptFlags;
            interpreterFlags = interpreter.InterpreterFlags;
            interpreterTestFlags = interpreter.InterpreterTestFlags;
            pluginFlags = interpreter.PluginFlags;

#if NATIVE && TCL
            findFlags = interpreter.TclFindFlags;
            loadFlags = interpreter.TclLoadFlags;
#endif
        }

        ///////////////////////////////////////////////////////////////////////

        public void UseObjectsFromInterpreter(
            Interpreter interpreter /* in */
            )
        {
            if (interpreter == null)
                return;

            owner = interpreter.Owner;
            applicationObject = interpreter.ApplicationObject;
            policyObject = interpreter.PolicyObject;
            resolverObject = interpreter.ResolverObject;
            userObject = interpreter.UserObject;
        }

        ///////////////////////////////////////////////////////////////////////

#if NATIVE && WINDOWS
        public void AttachOrOpenNativeConsole(
            bool ignoreOpen, /* in */
            bool attach,     /* in */
            bool force       /* in */
            )
        {
            if (!ignoreOpen && NativeConsole.IsOpen())
                return;

            hostCreateFlags |= HostCreateFlags.EmbeddedConsoleUse;

            if (!attach)
                hostCreateFlags &= ~HostCreateFlags.AttachConsole;

            if (force)
                hostCreateFlags |= HostCreateFlags.ForceConsole;
        }
#endif

        ///////////////////////////////////////////////////////////////////////

        public ReturnCode MaybeSetRuleSet(
            IRuleSet ruleSet, /* in */
            ref Result error  /* out */
            )
        {
            if (ruleSet == null)
            {
                error = "invalid ruleset";
                return ReturnCode.Error;
            }

            if (this.ruleSet != null)
            {
                error = String.Format(
                    "settings ruleset {0} already specified, " +
                    "cannot use specified immediate ruleset {1}",
                    FormatOps.WrapOrNull(this.ruleSet.GetName()),
                    FormatOps.WrapOrNull(ruleSet.GetName()));

                return ReturnCode.Error;
            }

            this.ruleSet = ruleSet;
            return ReturnCode.Ok;
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region System.Object Overrides
        public override string ToString()
        {
            StringList list = new StringList();

            list.Add("ruleSet");
            list.Add((ruleSet != null) ? ruleSet.ToString() : null);

            list.Add("args");
            list.Add((args != null) ? args.ToString() : null);

            list.Add("culture");
            list.Add(culture);

            list.Add("createFlags");
            list.Add(createFlags.ToString());

            list.Add("hostCreateFlags");
            list.Add(hostCreateFlags.ToString());

            list.Add("initializeFlags");
            list.Add(initializeFlags.ToString());

            list.Add("scriptFlags");
            list.Add(scriptFlags.ToString());

            list.Add("interpreterFlags");
            list.Add(interpreterFlags.ToString());

            list.Add("interpreterTestFlags");
            list.Add(interpreterTestFlags.ToString());

            list.Add("pluginFlags");
            list.Add(pluginFlags.ToString());

#if NATIVE && TCL
            list.Add("findFlags");
            list.Add(findFlags.ToString());

            list.Add("loadFlags");
            list.Add(loadFlags.ToString());
#endif

            list.Add("appDomain");
            list.Add((appDomain != null) ? appDomain.ToString() : null);

            list.Add("host");
            list.Add((host != null) ? host.ToString() : null);

            list.Add("profile");
            list.Add(profile);

            list.Add("owner");
            list.Add((owner != null) ? owner.ToString() : null);

            list.Add("applicationObject");
            list.Add((applicationObject != null) ?
                applicationObject.ToString() : null);

            list.Add("policyObject");
            list.Add((policyObject != null) ?
                policyObject.ToString() : null);

            list.Add("resolverObject");
            list.Add((resolverObject != null) ?
                resolverObject.ToString() : null);

            list.Add("userObject");
            list.Add((userObject != null) ? userObject.ToString() : null);

            list.Add("policies");
            list.Add((policies != null) ? policies.ToString() : null);

            list.Add("traces");
            list.Add((traces != null) ? traces.ToString() : null);

            list.Add("text");
            list.Add(text);

            list.Add("libraryPath");
            list.Add(libraryPath);

            list.Add("autoPathList");
            list.Add((autoPathList != null) ? autoPathList.ToString() : null);

            return list.ToString();
        }
        #endregion
    }
}
