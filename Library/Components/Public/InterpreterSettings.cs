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
    public sealed class InterpreterSettings
#if ISOLATED_INTERPRETERS || ISOLATED_PLUGINS
        : ScriptMarshalByRefObject
#endif
    {
        #region Private Constants
        private static readonly string LoadFromFileNameFormat =
            "{0}.settings{1}";
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
        private PluginFlags pluginFlags;

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
        internal InterpreterSettings()
        {
            // do nothing.
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Private Methods
        private void ResetEverything()
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
            pluginFlags = PluginFlags.None;
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

        private void UseDefaultsForFlags()
        {
            //
            // TODO: Update whenever the list of flags fields is updated.
            //
            createFlags = Defaults.CreateFlags;
            hostCreateFlags = Defaults.HostCreateFlags;
            initializeFlags = Defaults.InitializeFlags;
            scriptFlags = Defaults.ScriptFlags;
            interpreterFlags = Defaults.InterpreterFlags;
            pluginFlags = Defaults.PluginFlags;
        }

        ///////////////////////////////////////////////////////////////////////

        private void UseFlagsFromInterpreter(
            Interpreter interpreter
            )
        {
            if (interpreter == null)
                return;

            createFlags = interpreter.CreateFlags;
            hostCreateFlags = interpreter.HostCreateFlags;
            initializeFlags = interpreter.InitializeFlags;
            scriptFlags = interpreter.ScriptFlags;
            interpreterFlags = interpreter.InterpreterFlags;
            pluginFlags = interpreter.PluginFlags;
        }

        ///////////////////////////////////////////////////////////////////////

        private void UseObjectsFromInterpreter(
            Interpreter interpreter
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

        internal void MakeSafe() /* DO NOT USE: PrivateShellMainCore ONLY. */
        {
            createFlags |= CreateFlags.SafeAndHideUnsafe;
        }

        ///////////////////////////////////////////////////////////////////////

        internal void MakeStandard() /* DO NOT USE: PrivateShellMainCore ONLY. */
        {
            createFlags |= CreateFlags.StandardAndHideNonStandard;
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Static "Factory" Methods
        public static InterpreterSettings Create()
        {
            return new InterpreterSettings();
        }

        ///////////////////////////////////////////////////////////////////////

        public static InterpreterSettings CreateDefault()
        {
            InterpreterSettings interpreterSettings = Create();

            if (interpreterSettings != null)
            {
                interpreterSettings.ResetEverything();
                interpreterSettings.UseDefaultsForFlags();
            }

            return interpreterSettings;
        }

        ///////////////////////////////////////////////////////////////////////

        public static InterpreterSettings CreateFrom(
            string fileName,
            CultureInfo cultureInfo,
            bool merge,
            bool expand,
            ref Result error
            )
        {
            InterpreterSettings interpreterSettings = null;

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

        internal static InterpreterSettings Create( /* PrivateShellMain */
            IRuleSet ruleSet,
            IEnumerable<string> args,
            CreateFlags createFlags,
            HostCreateFlags hostCreateFlags,
            InitializeFlags initializeFlags,
            ScriptFlags scriptFlags,
            string text,
            string libraryPath
            )
        {
            InterpreterSettings interpreterSettings = CreateDefault();

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
        private static bool CouldBeDocument(
            string path
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
            string value
            )
        {
            if (String.IsNullOrEmpty(value))
                return value;

            return CommonOps.Environment.ExpandVariables(value);
        }

        ///////////////////////////////////////////////////////////////////////

        internal static void Expand(
            InterpreterSettings interpreterSettings
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
            Interpreter interpreter,
            InterpreterSettings interpreterSettings,
            bool recreate,
            bool full,
            ref Result error
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
            InterpreterSettings sourceInterpreterSettings,
            InterpreterSettings targetInterpreterSettings,
            bool forceMissing
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

            PluginFlags pluginFlags = sourceInterpreterSettings.PluginFlags;

            if (forceMissing || (pluginFlags != PluginFlags.None))
            {
                targetInterpreterSettings.PluginFlags = pluginFlags;
                result.Add("pluginFlags");
            }

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
            string fileName,
            Stream stream,
            CultureInfo cultureInfo,
            bool merge,
            bool expand,
            ref InterpreterSettings interpreterSettings,
            ref Result error
            )
        {
            return SettingsOps.LoadForInterpreter(
                null, fileName, stream, cultureInfo,
                merge, expand, ref interpreterSettings,
                ref error);
        }

        ///////////////////////////////////////////////////////////////////////

        private static ReturnCode LoadFromIni(
            string fileName,
            CultureInfo cultureInfo,
            bool merge,
            bool expand,
            ref InterpreterSettings interpreterSettings,
            ref Result error
            )
        {
            return SettingsOps.LoadForInterpreter(
                null, fileName, cultureInfo, merge,
                expand, ref interpreterSettings,
                ref error);
        }

        ///////////////////////////////////////////////////////////////////////

        private static ReturnCode SaveToIni(
            string fileName,
            bool expand,
            InterpreterSettings interpreterSettings,
            ref Result error
            )
        {
            return SettingsOps.SaveForInterpreter(null,
                fileName, expand, interpreterSettings,
                ref error);
        }

        ///////////////////////////////////////////////////////////////////////

#if XML
        private static ReturnCode FromDocument(
            XmlDocument document,
            CultureInfo cultureInfo,
            InterpreterSettings interpreterSettings,
            ref Result error
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
            XmlDocument document,
            InterpreterSettings interpreterSettings,
            ref Result error
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

            node = document.CreateElement("PluginFlags");
            node.InnerText = interpreterSettings.PluginFlags.ToString();
            documentElement.AppendChild(node);

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
            XmlDocument document,
            CultureInfo cultureInfo,
            bool merge,
            bool expand,
            ref InterpreterSettings interpreterSettings,
            ref Result error
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
                    InterpreterSettings documentInterpreterSettings =
                        @object as InterpreterSettings;

                    if (FromDocument(document,
                            cultureInfo, documentInterpreterSettings,
                            ref error) == ReturnCode.Ok)
                    {
                        if (expand)
                            Expand(documentInterpreterSettings);

                        InterpreterSettings newInterpreterSettings;

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
            string fileName,
            Stream stream,
            CultureInfo cultureInfo,
            bool merge,
            bool expand,
            ref InterpreterSettings interpreterSettings,
            ref Result error
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
            string fileName,
            CultureInfo cultureInfo,
            bool merge,
            bool expand,
            ref InterpreterSettings interpreterSettings,
            ref Result error
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
            string fileName,
            bool expand,
            InterpreterSettings interpreterSettings,
            ref Result error
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
            InterpreterSettings interpreterSettings,
            PolicyList policies,
            TraceList traces
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
            InterpreterSettings interpreterSettings
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
            InterpreterSettings interpreterSettings,
            CreateFlags createFlags,
            HostCreateFlags hostCreateFlags,
            ref Result error
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
            InterpreterSettings interpreterSettings,
            CreateFlags createFlags,
            ref Result error
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

            interpreterSettings.InterpreterFlags = interpreterFlags;
            return ReturnCode.Ok;
        }
#endif

        ///////////////////////////////////////////////////////////////////////

        internal static ReturnCode LoadFrom(
            CultureInfo cultureInfo,
            bool optional,
            bool merge,
            bool expand,
            ref InterpreterSettings interpreterSettings,
            ref Result error
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
            Interpreter interpreter,
            bool expand,
            bool recreate,
            bool full,
            ref InterpreterSettings interpreterSettings,
            ref Result error
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
                InterpreterSettings newInterpreterSettings = Create();

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
            string fileName,
            CultureInfo cultureInfo,
            bool merge,
            bool expand,
            ref InterpreterSettings interpreterSettings,
            ref Result error
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
            string fileName,
            Stream stream,
            CultureInfo cultureInfo,
            bool merge,
            bool expand,
            ref InterpreterSettings interpreterSettings,
            ref Result error
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
            string fileName,
            bool expand,
            InterpreterSettings interpreterSettings,
            ref Result error
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

        #region Public Properties
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
        public PluginFlags PluginFlags
        {
            get { return pluginFlags; }
            set { pluginFlags = value; }
        }

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

            list.Add("pluginFlags");
            list.Add(pluginFlags.ToString());

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
