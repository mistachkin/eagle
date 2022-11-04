/*
 * ConfigurationOps.cs --
 *
 * Copyright (c) 2007-2012 by Joe Mistachkin.  All rights reserved.
 *
 * See the file "license.terms" for information on usage and redistribution of
 * this file, and for a DISCLAIMER OF ALL WARRANTIES.
 *
 * RCS: @(#) $Id: $
 */

using System;

#if XML
using System.Collections.Generic;
#endif

using System.Collections.Specialized;

#if CONFIGURATION
using System.Configuration;
#endif

#if XML
using System.IO;
#endif

using System.Reflection;

#if XML
using System.Threading;
using System.Xml;
#endif

using Eagle._Attributes;
using Eagle._Components.Public;
using Eagle._Constants;
using Eagle._Containers.Private;
using Eagle._Containers.Public;
using Eagle._Interfaces.Public;

#if XML
using StringDictionary = Eagle._Containers.Public.StringDictionary;
using SharedStringOps = Eagle._Components.Shared.StringOps;
#endif

namespace Eagle._Components.Private
{
    [ObjectId("df98c383-ae1f-46b5-a3ab-a3902d186498")]
    internal static class ConfigurationOps
    {
        #region Private Constants
#if CONFIGURATION || XML
        //
        // NOTE: This is the name of the XML element that contains settings
        //       for the application.
        //
        private static readonly string AppSettingsName = "appSettings";
#endif

        ///////////////////////////////////////////////////////////////////////

#if XML
        //
        // NOTE: This is the name of the XML element that contains the
        //       whole configuration for the application.
        //
        private static readonly string ConfigurationName = "configuration";

        ///////////////////////////////////////////////////////////////////////

        //
        // NOTE: This is the namespace name for application configuration
        //       files, at the <configuration> element level.
        //
        private static readonly string NamespaceName = "dnfcfg";

        ///////////////////////////////////////////////////////////////////////

        //
        // NOTE: This is the namespace URI for application configuration
        //       files, at the <configuration> element level.
        //
        private static readonly Uri NamespaceUri = new Uri(
            "http://schemas.microsoft.com/.NetConfiguration/v2.0",
            UriKind.Absolute);

        ///////////////////////////////////////////////////////////////////////

        //
        // NOTE: The candidate XPath queries used to extract appSettings
        //       from an XML document.  The first query that returns some
        //       nodes wins.
        //
        private static readonly StringList ReadXPathList = new StringList(
            new string[] {
            //
            // NOTE: First, check for the necessary elements using the
            //       name of our namespace.
            //
            (NamespaceName != null) ?
                "//" + NamespaceName + ":" + ConfigurationName + "/" +
                NamespaceName + ":" + AppSettingsName + "/*" : null,

            //
            // NOTE: Second, check for the necessary elements using the
            //       default namespace.
            //
            "//" + ConfigurationName + "/" + AppSettingsName + "/*",

            //
            // NOTE: These list elements are reserved for future use by
            //       the core library.  Please do not change them.
            //
            null,
            null,
            null,
            null,

            //
            // NOTE: These list elements are reserved for future use by
            //       third-party code.
            //
            null,
            null,
            null,
            null
        });

        ///////////////////////////////////////////////////////////////////////

        //
        // NOTE: The candidate XPath queries used to add the appSettings
        //       to an XML document.  The first query that returns exactly
        //       one node wins.
        //
        private static readonly StringList WriteXPathList = new StringList(
            new string[] {
            //
            // NOTE: First, check for the necessary elements using the
            //       name of our namespace.
            //
            (NamespaceName != null) ?
                "//" + NamespaceName + ":" + ConfigurationName + "/" +
                NamespaceName + ":" + AppSettingsName : null,

            //
            // NOTE: Second, check for the necessary elements using the
            //       default namespace.
            //
            "//" + ConfigurationName + "/" + AppSettingsName,

            //
            // NOTE: These list elements are reserved for future use by
            //       the core library.  Please do not change them.
            //
            null,
            null,
            null,
            null,

            //
            // NOTE: These list elements are reserved for future use by
            //       third-party code.
            //
            null,
            null,
            null,
            null
        });

        ///////////////////////////////////////////////////////////////////////

        //
        // HACK: This is purposely not read-only.
        //
        private static string TemplateXml =
            "<?xml version=\"1.0\" encoding=\"UTF-8\" ?>\r\n" +
            "<" + ConfigurationName + "><" + AppSettingsName +
            "></" + AppSettingsName + "></" + ConfigurationName + ">";

        ///////////////////////////////////////////////////////////////////////

        //
        // HACK: These are purposely not read-only.
        //
        private static string ClearElementName = "clear";
        private static string AddElementName = "add";
        private static string RemoveElementName = "remove";
        private static string SetElementName = "set";

        ///////////////////////////////////////////////////////////////////////

        //
        // HACK: These are purposely not read-only.
        //
        private static string ResetCacheElementName = "resetCache";
        private static string ReplaceCacheElementName = "replaceCache";
        private static string ResetOverrideElementName = "resetOverride";
        private static string ReplaceOverrideElementName = "replaceOverride";

        ///////////////////////////////////////////////////////////////////////

        //
        // HACK: These are purposely not read-only.
        //
        private static string KeyAttributeName = "key";
        private static string ValueAttributeName = "value";
#endif
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Private Data
        private static readonly object syncRoot = new object();

        ///////////////////////////////////////////////////////////////////////

#if XML
        private static int useXmlFiles = CommonOps.Runtime.IsDotNetCore() ?
            1 : 0;

        ///////////////////////////////////////////////////////////////////////

        private static int useWebFiles = CommonOps.Runtime.IsDotNetCore() ?
            1 : 0;

        ///////////////////////////////////////////////////////////////////////

        private static bool mergeXmlAppSettings = false;

        ///////////////////////////////////////////////////////////////////////

        private static bool mergeAllAppSettings = false;

        ///////////////////////////////////////////////////////////////////////

        private static NameValueCollection xmlAppSettings;

        ///////////////////////////////////////////////////////////////////////

        private static NameValueCollection mergedAppSettings;
#endif

        ///////////////////////////////////////////////////////////////////////

        private static bool? noComplainGet;
        private static bool? noComplainSet;
        private static bool? noComplainUnset;

        ///////////////////////////////////////////////////////////////////////

        private static PropertyInfo isReadOnlyPropertyInfo;

        ///////////////////////////////////////////////////////////////////////

        private static NameValueCollection overriddenAppSettings;
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Introspection Support Methods
        //
        // NOTE: Used by the _Hosts.Default.BuildEngineInfoList method.
        //
        public static void AddInfo(
            StringPairList list,    /* in, out */
            DetailFlags detailFlags /* in */
            )
        {
            if (list == null)
                return;

            lock (syncRoot) /* TRANSACTIONAL */
            {
                bool empty = HostOps.HasEmptyContent(detailFlags);
                StringPairList localList = new StringPairList();

#if XML
                if (empty || (useXmlFiles > 0))
                    localList.Add("UseXmlFiles", useXmlFiles.ToString());

                if (empty || (useWebFiles > 0))
                    localList.Add("UseWebFiles", useWebFiles.ToString());

                if (empty || mergeXmlAppSettings)
                {
                    localList.Add("MergeXmlAppSettings",
                        mergeXmlAppSettings.ToString());
                }

                if (empty || mergeAllAppSettings)
                {
                    localList.Add("MergeAllAppSettings",
                        mergeAllAppSettings.ToString());
                }

                if (empty || ((xmlAppSettings != null) &&
                        (xmlAppSettings.Count > 0)))
                {
                    localList.Add("XmlAppSettings",
                        (xmlAppSettings != null) ?
                            xmlAppSettings.Count.ToString() :
                            FormatOps.DisplayNull);
                }

                if (empty || ((mergedAppSettings != null) &&
                        (mergedAppSettings.Count > 0)))
                {
                    localList.Add("MergedAppSettings",
                        (mergedAppSettings != null) ?
                            mergedAppSettings.Count.ToString() :
                            FormatOps.DisplayNull);
                }
#endif

                if (empty || (noComplainGet != null))
                {
                    localList.Add("NoComplainGet",
                        FormatOps.WrapOrNull(noComplainGet));
                }

                if (empty || (noComplainSet != null))
                {
                    localList.Add("NoComplainSet",
                        FormatOps.WrapOrNull(noComplainSet));
                }

                if (empty || (noComplainUnset != null))
                {
                    localList.Add("NoComplainUnset",
                        FormatOps.WrapOrNull(noComplainUnset));
                }

                if (empty || (isReadOnlyPropertyInfo != null))
                {
                    localList.Add("IsReadOnlyPropertyInfo",
                        (isReadOnlyPropertyInfo != null) ?
                            isReadOnlyPropertyInfo.ToString() :
                            FormatOps.DisplayNull);
                }

                if (empty || ((overriddenAppSettings != null) &&
                        (overriddenAppSettings.Count > 0)))
                {
                    localList.Add("OverriddenAppSettings",
                        (overriddenAppSettings != null) ?
                            overriddenAppSettings.Count.ToString() :
                            FormatOps.DisplayNull);
                }

                if (localList.Count > 0)
                {
                    list.Add((IPair<string>)null);
                    list.Add("Configuration Information");
                    list.Add((IPair<string>)null);
                    list.Add(localList);
                }
            }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Diagnostic Support Methods
        private static void DebugTrace(
            string message,        /* in */
            TracePriority priority /* in */
            )
        {
            TraceOps.DebugTrace(
                message, typeof(ConfigurationOps).Name, priority);
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Xml Support Methods
#if XML
        private static void InitializeXmlFiles()
        {
            if (!ShouldUseXmlFiles())
            {
                if (CommonOps.Environment.DoesVariableExist(
                        EnvVars.UseXmlFiles))
                {
                    EnableUseXmlFiles(true);
                }
            }

            ///////////////////////////////////////////////////////////////////

            if (!ShouldMergeXmlAppSettings())
            {
                if (CommonOps.Environment.DoesVariableExist(
                        EnvVars.MergeXmlAppSettings))
                {
                    EnableMergeXmlAppSettings(true);
                }
            }

            ///////////////////////////////////////////////////////////////////

            if (!ShouldMergeAllAppSettings())
            {
                if (CommonOps.Environment.DoesVariableExist(
                        EnvVars.MergeAllAppSettings))
                {
                    EnableMergeAllAppSettings(true);
                }
            }
        }

        ///////////////////////////////////////////////////////////////////////

        private static bool ShouldUseXmlFiles()
        {
            return Interlocked.CompareExchange(ref useXmlFiles, 0, 0) > 0;
        }

        ///////////////////////////////////////////////////////////////////////

        private static void EnableUseXmlFiles(
            bool enable /* in */
            )
        {
            if (enable)
                Interlocked.Increment(ref useXmlFiles);
            else
                Interlocked.Decrement(ref useXmlFiles);
        }

        ///////////////////////////////////////////////////////////////////////

        private static bool ShouldUseWebFiles()
        {
            return Interlocked.CompareExchange(ref useWebFiles, 0, 0) > 0;
        }

        ///////////////////////////////////////////////////////////////////////

        private static void EnableUseWebFiles(
            bool enable /* in */
            )
        {
            if (enable)
                Interlocked.Increment(ref useWebFiles);
            else
                Interlocked.Decrement(ref useWebFiles);
        }

        ///////////////////////////////////////////////////////////////////////

        private static void SetXmlAppSettings(
            NameValueCollection appSettings /* in */
            )
        {
            lock (syncRoot)
            {
                xmlAppSettings = appSettings;
            }
        }

        ///////////////////////////////////////////////////////////////////////

        private static void ResetXmlAppSettings()
        {
            lock (syncRoot) /* TRANSACTIONAL */
            {
                if (xmlAppSettings != null)
                {
                    xmlAppSettings.Clear();
                    xmlAppSettings = null;
                }
            }
        }

        ///////////////////////////////////////////////////////////////////////

        private static void SetMergedAppSettings(
            NameValueCollection appSettings /* in */
            )
        {
            lock (syncRoot)
            {
                mergedAppSettings = appSettings;
            }
        }

        ///////////////////////////////////////////////////////////////////////

        private static void ResetMergedAppSettings()
        {
            lock (syncRoot) /* TRANSACTIONAL */
            {
                if (mergedAppSettings != null)
                {
                    mergedAppSettings.Clear();
                    mergedAppSettings = null;
                }
            }
        }

        ///////////////////////////////////////////////////////////////////////

        private static bool ShouldMergeXmlAppSettings()
        {
            lock (syncRoot)
            {
                return mergeXmlAppSettings;
            }
        }

        ///////////////////////////////////////////////////////////////////////

        private static void EnableMergeXmlAppSettings(
            bool enable /* in */
            )
        {
            lock (syncRoot)
            {
                mergeXmlAppSettings = enable;
            }
        }

        ///////////////////////////////////////////////////////////////////////

        private static bool ShouldMergeAllAppSettings()
        {
            lock (syncRoot)
            {
                return mergeAllAppSettings;
            }
        }

        ///////////////////////////////////////////////////////////////////////

        private static void EnableMergeAllAppSettings(
            bool enable /* in */
            )
        {
            lock (syncRoot)
            {
                mergeAllAppSettings = enable;
            }
        }

        ///////////////////////////////////////////////////////////////////////

        private static IEnumerable<string> GetAppSettingsLocations()
        {
            return new string[] {
                GlobalState.GetEntryAssemblyLocation(),
                GlobalState.GetAssemblyLocation(),
                PathOps.GetExecutableName()
            };
        }

        ///////////////////////////////////////////////////////////////////////

        private static string GetAppSettingsFallbackLocation()
        {
            return PathOps.GetManagedExecutableName();
        }

        ///////////////////////////////////////////////////////////////////////

        private static IEnumerable<string> GetXmlFileNames(
            IEnumerable<string> locations, /* in */
            string fallbackLocation,       /* in */
            bool includeFallback,          /* in */
            bool includeWeb                /* in */
            )
        {
            if (locations == null)
                return null;

            StringList allFileNames = null;

            foreach (string location in locations)
            {
                if (String.IsNullOrEmpty(location))
                    continue;

                StringList overrideFileNames = PathOps.GetOverrideFileNames(
                    location, FileExtension.Configuration, includeFallback ||
                    !PathOps.IsSameFile(null, location, fallbackLocation),
                    includeWeb);

                if (overrideFileNames == null)
                    continue;

                if (allFileNames == null)
                    allFileNames = new StringList();

                allFileNames.MaybeAddRange(overrideFileNames);
            }

            if (allFileNames == null)
                return null;

            PathDictionary<object> uniqueFileNames = null;

            foreach (string fileName in allFileNames)
            {
                if (String.IsNullOrEmpty(fileName))
                    continue;

                if (uniqueFileNames == null)
                    uniqueFileNames = new PathDictionary<object>();

                if (uniqueFileNames.ContainsKey(fileName))
                    continue;

                uniqueFileNames.Add(fileName, null);
            }

            if (uniqueFileNames == null)
                return null;

            return uniqueFileNames.GetKeysInOrder(false);
        }

        ///////////////////////////////////////////////////////////////////////

        public static NameValueCollection ReadFromXmlFile(
            string fileName, /* in */
            ref Result error /* out */
            )
        {
            XmlDocument document = null;

            if (XmlOps.LoadFile(
                    fileName, ref document, ref error) != ReturnCode.Ok)
            {
                return null;
            }

            XmlNodeList nodeList = null;

            if (XmlOps.GetNodeList(
                    document, NamespaceName, NamespaceUri, ReadXPathList,
                    ref nodeList, ref error) != ReturnCode.Ok)
            {
                return null;
            }

            if ((nodeList == null) || (nodeList.Count == 0))
            {
                error = "no configuration settings were found";
                return null;
            }

            try
            {
                NameValueCollection appSettings = new NameValueCollection();

                foreach (XmlNode node in nodeList)
                {
                    XmlElement element = node as XmlElement;

                    if (element == null)
                        continue;

                    string elementName = element.LocalName;

                    if (SharedStringOps.SystemEquals(
                            elementName, ClearElementName))
                    {
                        appSettings.Clear();
                    }
                    else if (SharedStringOps.SystemEquals(
                            elementName, AddElementName))
                    {
                        appSettings.Add(
                            element.GetAttribute(KeyAttributeName),
                            element.GetAttribute(ValueAttributeName));
                    }
                    else if (SharedStringOps.SystemEquals(
                            elementName, SetElementName))
                    {
                        appSettings.Set(
                            element.GetAttribute(KeyAttributeName),
                            element.GetAttribute(ValueAttributeName));
                    }
                    else if (SharedStringOps.SystemEquals(
                            elementName, RemoveElementName))
                    {
                        appSettings.Remove(
                            element.GetAttribute(KeyAttributeName));
                    }
                    else if (SharedStringOps.SystemEquals(
                            elementName, ResetCacheElementName))
                    {
                        ResetXmlAppSettings();
                    }
                    else if (SharedStringOps.SystemEquals(
                            elementName, ReplaceCacheElementName))
                    {
                        SetXmlAppSettings(appSettings);
                    }
                    else if (SharedStringOps.SystemEquals(
                            elementName, ResetOverrideElementName))
                    {
                        ResetAppSettings();
                    }
                    else if (SharedStringOps.SystemEquals(
                            elementName, ReplaceOverrideElementName))
                    {
                        SetAppSettings(appSettings);
                    }
                }

                return appSettings;
            }
            catch (Exception e)
            {
                error = e;
            }

            return null;
        }

        ///////////////////////////////////////////////////////////////////////

        public static ReturnCode WriteToXmlFile(
            string fileName,                 /* in */
            NameValueCollection appSettings, /* in */
            ref Result error                 /* out */
            )
        {
            if (appSettings == null)
            {
                error = "invalid application settings";
                return ReturnCode.Error;
            }

            XmlDocument document = null;

            if (XmlOps.LoadString(
                    TemplateXml, ref document, ref error) != ReturnCode.Ok)
            {
                return ReturnCode.Error;
            }

            XmlNodeList nodeList = null;

            if (XmlOps.GetNodeList(
                    document, NamespaceName, NamespaceUri, WriteXPathList,
                    ref nodeList, ref error) != ReturnCode.Ok)
            {
                return ReturnCode.Error;
            }

            if ((nodeList == null) || (nodeList.Count != 1))
            {
                error = "no configuration settings were found";
                return ReturnCode.Error;
            }

            try
            {
                XmlNode node = nodeList[0]; /* <configuration><appSettings> */

                foreach (string name in appSettings)
                {
                    XmlElement element = document.CreateElement(
                        AddElementName);

                    if (name != null)
                        element.SetAttribute(KeyAttributeName, name);

                    string value = appSettings.Get(name);

                    if (value != null)
                        element.SetAttribute(ValueAttributeName, value);

                    node.AppendChild(element);
                }
            }
            catch (Exception e)
            {
                error = e;
            }

            if (XmlOps.SaveFile(
                    fileName, document, ref error) != ReturnCode.Ok)
            {
                return ReturnCode.Error;
            }

            return ReturnCode.Ok;
        }

        ///////////////////////////////////////////////////////////////////////

        public static void MergeAppSettings(
            ref NameValueCollection appSettings1, /* in, out */
            NameValueCollection appSettings2,     /* in */
            bool unique,                          /* in */
            bool append                           /* in */
            )
        {
            if (appSettings2 == null)
                return;

            if (appSettings1 == null)
                appSettings1 = new NameValueCollection();

            foreach (string name in appSettings2)
            {
                string oldValue = appSettings1.Get(name);

                if (unique && (oldValue != null))
                    continue;

                string newValue = appSettings2.Get(name);

                if (append)
                    appSettings1.Add(name, newValue);
                else
                    appSettings1.Set(name, newValue);
            }
        }

        ///////////////////////////////////////////////////////////////////////

        private static NameValueCollection GetAppSettingsViaXmlFiles(
            IEnumerable<string> locations, /* in */
            string fallbackLocation,       /* in */
            bool includeFallback,          /* in */
            bool includeWeb,               /* in */
            bool merge                     /* in */
            )
        {
            string traceDescription = null;

            lock (syncRoot) /* TRANSACTIONAL */
            {
                if (xmlAppSettings != null)
                    traceDescription = GetTraceDescription(xmlAppSettings);
            }

            if (traceDescription != null)
            {
                DebugTrace(String.Format(
                    "GetAppSettingsViaXmlFiles: using cached {0}",
                    traceDescription), TracePriority.StartupDebug2);
            }

            lock (syncRoot) /* TRANSACTIONAL */
            {
                if (xmlAppSettings != null)
                    return xmlAppSettings;
            }

            IEnumerable<string> fileNames = GetXmlFileNames(
                locations, fallbackLocation, includeFallback, includeWeb);

            if (fileNames != null)
            {
                foreach (string fileName in fileNames)
                {
                    if (String.IsNullOrEmpty(fileName))
                        continue;

                    if (!File.Exists(fileName))
                        continue;

                    NameValueCollection appSettings;
                    Result error = null;

                    appSettings = ReadFromXmlFile(fileName, ref error);

                    if (appSettings != null)
                    {
                        traceDescription = GetTraceDescription(
                            appSettings);

                        DebugTrace(String.Format(
                            "GetAppSettingsViaXmlFiles: {0} file " +
                            "{1} read from {2}", merge ? "merging" :
                            "using", traceDescription,
                            FormatOps.WrapOrNull(fileName)),
                            TracePriority.StartupDebug);

                        lock (syncRoot) /* TRANSACTIONAL */
                        {
                            if (merge)
                            {
                                MergeAppSettings(
                                    ref xmlAppSettings,
                                    appSettings, true, false);

                                continue;
                            }
                            else
                            {
                                xmlAppSettings = appSettings;
                                return xmlAppSettings;
                            }
                        }
                    }
                    else
                    {
                        DebugTrace(String.Format(
                            "GetAppSettingsViaXmlFiles: failed " +
                            "to read from file {0}, error = {1}",
                            FormatOps.WrapOrNull(fileName),
                            FormatOps.WrapOrNull(error)),
                            TracePriority.StartupError);

                        if (merge)
                            continue;
                    }

                    return null;
                }

                if (merge)
                {
                    lock (syncRoot) /* TRANSACTIONAL */
                    {
                        traceDescription = GetTraceDescription(
                            xmlAppSettings);
                    }

                    DebugTrace(String.Format(
                        "GetAppSettingsViaXmlFiles: using merged {0}",
                        traceDescription), TracePriority.StartupDebug);

                    lock (syncRoot)
                    {
                        return xmlAppSettings;
                    }
                }
            }

            //
            // NOTE: This is not an error.  Just return no settings
            //       because no XML configuration files are present.
            //
            DebugTrace(
                "GetAppSettingsViaXmlFiles: skipping files because " +
                "they did not exist", TracePriority.StartupDebug);

            return null;
        }
#endif
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Private Methods
        private static NameValueCollection GetAppSettingsViaManager()
        {
#if CONFIGURATION
            //
            // WARNING: Do not use the ConfigurationManager class directly
            //          from anywhere else.
            //
            if (CommonOps.Environment.DoesVariableExistOnce(
                    EnvVars.RefreshAppSettings))
            {
                ConfigurationManager.RefreshSection(AppSettingsName);

                DebugTrace(
                    "GetAppSettingsViaManager: forcibly refreshed settings",
                    TracePriority.StartupDebug);
            }

            NameValueCollection appSettings = ConfigurationManager.AppSettings;

#if false
            DebugTrace(String.Format(
                "GetAppSettingsViaManager: using built-in {0}",
                GetTraceDescription(appSettings)),
                TracePriority.StartupDebug2);
#endif

            return appSettings;
#else
#if false
            DebugTrace(
                "GetAppSettingsViaManager: built-in settings unavailable",
                TracePriority.StartupDebug2);
#endif

            return null;
#endif
        }

        ///////////////////////////////////////////////////////////////////////

        private static string GetTraceDescription(
            NameValueCollection appSettings
            ) /* CANNOT RETURN NULL */
        {
            return String.Format(
                "instance {0} with {1} settings", FormatOps.WrapOrNull(
                appSettings), (appSettings != null) ? appSettings.Count :
                Count.Invalid);
        }

        ///////////////////////////////////////////////////////////////////////

        private static void Initialize()
        {
            lock (syncRoot) /* TRANSACTIONAL */
            {
                bool isMono = CommonOps.Runtime.IsMono();

                ///////////////////////////////////////////////////////////////

                //
                // HACK: It is expected that attempting to read application
                //       settings will fail a large percentage of the time
                //       because they have not been set; therefore, disable
                //       those complaints by default.
                //
                if (noComplainGet == null)
                    noComplainGet = true;

                //
                // HACK: *MONO* There seems to be a subtle incompatibility
                //       on Mono that results in the AppSettings collection
                //       returned by the ConfigurationManager.AppSettings
                //       property being read-only (e.g. perhaps this only
                //       happens in non-default application domains?).  In
                //       order to facilitate better Mono support, we do not
                //       want to complain about these errors.
                //
                if (noComplainSet == null)
                    noComplainSet = isMono;

                if (noComplainUnset == null)
                    noComplainUnset = isMono;

                ///////////////////////////////////////////////////////////////

                if (isReadOnlyPropertyInfo == null)
                {
                    //
                    // HACK: Why must we do this?  This member is marked as
                    //       "protected"; however, we really need to know
                    //       this information (e.g. on Mono where it seems
                    //       that the collection may actually be read-only).
                    //       Therefore, just use Reflection.  We cache the
                    //       PropertyInfo object so that we do not need to
                    //       look it up more than once.
                    //
                    Type type = typeof(NameValueCollection);

                    isReadOnlyPropertyInfo = type.GetProperty(
                        "IsReadOnly", ObjectOps.GetBindingFlags(
                            MetaBindingFlags.PrivateInstance, true));
                }
            }

            ///////////////////////////////////////////////////////////////////

#if XML
            InitializeXmlFiles();
#endif
        }

        ///////////////////////////////////////////////////////////////////////

        private static NameValueCollection GetAppSettingsViaAny()
        {
            string traceDescription = null;

            lock (syncRoot) /* TRANSACTIONAL */
            {
                if (overriddenAppSettings != null)
                {
                    traceDescription = GetTraceDescription(
                        overriddenAppSettings);
                }
            }

            if (traceDescription != null)
            {
                DebugTrace(String.Format(
                    "GetAppSettingsViaAny: using overridden {0}",
                    traceDescription), TracePriority.StartupDebug);
            }

            lock (syncRoot) /* TRANSACTIONAL */
            {
                if (overriddenAppSettings != null)
                    return overriddenAppSettings;
            }

            ///////////////////////////////////////////////////////////////////

#if XML
            if (ShouldUseXmlFiles())
            {
                NameValueCollection appSettings0 = GetAppSettingsViaXmlFiles(
                    GetAppSettingsLocations(), GetAppSettingsFallbackLocation(),
                    !ShouldMergeAllAppSettings(), ShouldUseWebFiles(),
                    ShouldMergeXmlAppSettings());

                if (ShouldMergeAllAppSettings())
                {
                    traceDescription = null;

                    lock (syncRoot) /* TRANSACTIONAL */
                    {
                        if (mergedAppSettings != null)
                        {
                            traceDescription = GetTraceDescription(
                                mergedAppSettings);
                        }
                    }

                    if (traceDescription != null)
                    {
                        DebugTrace(String.Format(
                            "GetAppSettingsViaAny: using cached merged {0}",
                            traceDescription), TracePriority.StartupDebug2);
                    }

                    lock (syncRoot) /* TRANSACTIONAL */
                    {
                        if (mergedAppSettings != null)
                            return mergedAppSettings;
                    }

                    NameValueCollection appSettings1 = (appSettings0 != null) ?
                        new NameValueCollection(appSettings0) : null;

                    NameValueCollection appSettings2 =
                        GetAppSettingsViaManager(); /* READ-ONLY */

                    MergeAppSettings(
                        ref appSettings1, appSettings2, true, false);

                    lock (syncRoot) /* TRANSACTIONAL */
                    {
                        mergedAppSettings = appSettings1;
                        return mergedAppSettings;
                    }
                }

                return appSettings0;
            }
#endif

            ///////////////////////////////////////////////////////////////////

            return GetAppSettingsViaManager();
        }

        ///////////////////////////////////////////////////////////////////////

        private static void InitializeAppSettings()
        {
            lock (syncRoot) /* TRANSACTIONAL */
            {
                if (overriddenAppSettings != null)
                    return;

                overriddenAppSettings = new NameValueCollection();
            }
        }

        ///////////////////////////////////////////////////////////////////////

        private static bool HaveAppSettings()
        {
            lock (syncRoot)
            {
                return (overriddenAppSettings != null);
            }
        }

        ///////////////////////////////////////////////////////////////////////

        public static NameValueCollection GetAppSettings()
        {
            lock (syncRoot)
            {
                return overriddenAppSettings;
            }
        }

        ///////////////////////////////////////////////////////////////////////

        public static void SetAppSettings(
            NameValueCollection appSettings /* in */
            )
        {
            lock (syncRoot)
            {
                overriddenAppSettings = appSettings;
            }
        }

        ///////////////////////////////////////////////////////////////////////

        private static void ResetAppSettings()
        {
            lock (syncRoot) /* TRANSACTIONAL */
            {
                if (overriddenAppSettings != null)
                {
                    overriddenAppSettings.Clear();
                    overriddenAppSettings = null;
                }
            }
        }

        ///////////////////////////////////////////////////////////////////////

        private static ReturnCode LoadAppSettings(
            string fileName, /* in */
            bool merge,      /* in */
            ref Result error /* out */
            )
        {
#if XML
            lock (syncRoot) /* TRANSACTIONAL */
            {
                NameValueCollection appSettings = ReadFromXmlFile(
                    fileName, ref error);

                if (appSettings == null)
                    return ReturnCode.Error;

                if (overriddenAppSettings == null)
                {
                    overriddenAppSettings = appSettings;
                }
                else if (merge)
                {
                    MergeAppSettings(
                        ref overriddenAppSettings, appSettings,
                        true, false);
                }
                else
                {
                    error = "application settings already loaded";
                    return ReturnCode.Error;
                }

                return ReturnCode.Ok;
            }
#else
            error = "not implemented";
            return ReturnCode.Error;
#endif
        }

        ///////////////////////////////////////////////////////////////////////

        private static ReturnCode SaveAppSettings(
            string fileName, /* in */
            ref Result error /* out */
            )
        {
#if XML
            lock (syncRoot) /* TRANSACTIONAL */
            {
                return WriteToXmlFile(
                    fileName, overriddenAppSettings, ref error);
            }
#else
            error = "not implemented";
            return ReturnCode.Error;
#endif
        }

        ///////////////////////////////////////////////////////////////////////

        private static bool IsReadOnly(
            NameValueCollection appSettings /* in */
            )
        {
            if (appSettings == null)
                return false;

            try
            {
                lock (syncRoot) /* TRANSACTIONAL */
                {
                    if (isReadOnlyPropertyInfo == null)
                        return false;

                    return (bool)isReadOnlyPropertyInfo.GetValue(
                        appSettings, null);
                }
            }
            catch
            {
                // do nothing.
            }

            return false;
        }

        ///////////////////////////////////////////////////////////////////////

        //
        // NOTE: This method assumes the lock is already held.
        //
        private static bool PrivateHaveAppSettings(
            bool moreThanZero /* in */
            )
        {
            NameValueCollection appSettings = GetAppSettingsViaAny();

            if (appSettings == null)
                return false;

            return !moreThanZero || (appSettings.Count > 0);
        }

        ///////////////////////////////////////////////////////////////////////

        private static bool GetNoComplain(
            ConfigurationOperation operation /* in */
            )
        {
            switch (operation)
            {
                case ConfigurationOperation.Get:
                    {
                        lock (syncRoot) /* TRANSACTIONAL */
                        {
                            if (!PrivateHaveAppSettings(false))
                                return true;

                            if (noComplainGet != null)
                                return (bool)noComplainGet;
                        }

                        break;
                    }
                case ConfigurationOperation.Set:
                    {
                        lock (syncRoot) /* TRANSACTIONAL */
                        {
                            if (!PrivateHaveAppSettings(false))
                                return true;

                            if (noComplainSet != null)
                                return (bool)noComplainSet;
                        }

                        break;
                    }
                case ConfigurationOperation.Unset:
                    {
                        lock (syncRoot) /* TRANSACTIONAL */
                        {
                            if (!PrivateHaveAppSettings(false))
                                return true;

                            if (noComplainUnset != null)
                                return (bool)noComplainUnset;
                        }

                        break;
                    }
            }

            return false;
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Public Methods
        #region Getting (Read) Values
        public static bool HaveAppSettings(
            bool moreThanZero /* in */
            )
        {
            Initialize();

            lock (syncRoot) /* TRANSACTIONAL */
            {
                return PrivateHaveAppSettings(moreThanZero);
            }
        }

        ///////////////////////////////////////////////////////////////////////

        public static string GetAppSetting(
            string name /* in */
            )
        {
            return GetAppSetting(name, null);
        }

        ///////////////////////////////////////////////////////////////////////

        public static string GetAppSetting(
            string name,    /* in */
            string @default /* in */
            )
        {
            string value = null;
            Result error = null;

            if (!TryGetAppSetting(name, out value, ref error))
            {
                bool noComplain = GetNoComplain(ConfigurationOperation.Get);

                if (!noComplain)
                    DebugOps.Complain(ReturnCode.Error, error);

                return @default;
            }

            return value;
        }

        ///////////////////////////////////////////////////////////////////////

        public static bool TryGetAppSetting(
            string name,      /* in */
            out string value, /* out */
            ref Result error  /* out */
            )
        {
            Initialize();

            lock (syncRoot) /* TRANSACTIONAL */
            {
                NameValueCollection appSettings = GetAppSettingsViaAny();

                if (appSettings == null)
                {
                    value = null;

                    error = "invalid application settings";

                    return false;
                }

                string stringValue = appSettings.Get(name);

                if (stringValue == null)
                {
                    value = null;

                    error = String.Format(
                        "setting {0} not found", FormatOps.WrapOrNull(name));

                    return false;
                }

                value = stringValue;
                return true;
            }
        }

        ///////////////////////////////////////////////////////////////////////

        #region Strongly Typed Setting Values
        //
        // WARNING: Do not use this method from the GlobalConfiguration class.
        //
        public static bool TryGetIntegerAppSetting(
            string name,  /* in */
            out int value /* out */
            )
        {
            Result error = null;

            return TryGetIntegerAppSetting(name, out value, ref error);
        }

        ///////////////////////////////////////////////////////////////////////

        //
        // WARNING: Do not use this method from the GlobalConfiguration class.
        //
        public static bool TryGetIntegerAppSetting(
            string name,     /* in */
            out int value,   /* out */
            ref Result error /* out */
            )
        {
            string stringValue;

            if (!TryGetAppSetting(name, out stringValue, ref error))
            {
                value = default(int);
                return false;
            }

            int intValue = default(int);

            if (Value.GetInteger2(
                    stringValue, ValueFlags.AnyInteger, null,
                    ref intValue, ref error) != ReturnCode.Ok)
            {
                value = default(int);
                return false;
            }

            value = intValue;
            return true;
        }

        ///////////////////////////////////////////////////////////////////////

        #region Dead Code
#if DEAD_CODE
        //
        // WARNING: Do not use this method from the GlobalConfiguration class.
        //
        private static bool TryGetListAppSetting(
            string name,          /* in */
            out StringList value, /* out */
            ref Result error      /* out */
            )
        {
            string stringValue;

            if (!TryGetAppSetting(name, out stringValue, ref error))
            {
                value = null;
                return false;
            }

            StringList listValue = null;

            if (ParserOps<string>.SplitList(
                    null, stringValue, 0, Length.Invalid, false,
                    ref listValue, ref error) != ReturnCode.Ok)
            {
                value = null;
                return false;
            }

            value = listValue;
            return true;
        }

        ///////////////////////////////////////////////////////////////////////

        //
        // WARNING: Do not use this method from the GlobalConfiguration class.
        //
        private static bool TryGetBooleanAppSetting(
            string name,     /* in */
            out bool value,  /* out */
            ref Result error /* out */
            )
        {
            string stringValue;

            if (!TryGetAppSetting(name, out stringValue, ref error))
            {
                value = default(bool);
                return false;
            }

            bool boolValue = default(bool);

            if (Value.GetBoolean2(
                    stringValue, ValueFlags.AnyBoolean, null,
                    ref boolValue, ref error) != ReturnCode.Ok)
            {
                value = default(bool);
                return false;
            }

            value = boolValue;
            return true;
        }

        ///////////////////////////////////////////////////////////////////////

        //
        // WARNING: Do not use this method from the GlobalConfiguration class.
        //
        private static bool TryGetEnumAppSetting(
            string name,      /* in */
            Type enumType,    /* in */
            string oldValue,  /* in */
            out object value, /* out */
            ref Result error  /* out */
            )
        {
            string stringValue;

            if (!TryGetAppSetting(name, out stringValue, ref error))
            {
                value = null;
                return false;
            }

            object enumValue;

            if (EnumOps.IsFlags(enumType))
            {
                enumValue = EnumOps.TryParseFlags(
                    null, enumType, oldValue, stringValue,
                    null, true, true, true, ref error);
            }
            else
            {
                enumValue = EnumOps.TryParse(
                    enumType, stringValue, true, true,
                    ref error);
            }

            if (!(enumValue is Enum))
            {
                value = null;
                return false;
            }

            value = enumValue;
            return true;
        }

        ///////////////////////////////////////////////////////////////////////

        //
        // WARNING: Do not use this method from the GlobalConfiguration class.
        //
        private static bool TryGetObjectAppSetting(
            Interpreter interpreter, /* in */
            string name,             /* in */
            LookupFlags lookupFlags, /* in */
            out object value,        /* out */
            ref Result error         /* out */
            )
        {
            if (interpreter == null)
            {
                value = null;
                error = "invalid interpreter";

                return false;
            }

            string stringValue;

            if (!TryGetAppSetting(name, out stringValue, ref error))
            {
                value = null;
                return false;
            }

            IObject @object = null;

            if (interpreter.GetObject(
                    stringValue, lookupFlags, ref @object,
                    ref error) != ReturnCode.Ok)
            {
                value = null;
                return false;
            }

            value = (@object != null) ? @object.Value : null;
            return true;
        }
#endif
        #endregion
        #endregion
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Setting (Write) Values
        public static void SetAppSetting(
            string name, /* in */
            string value /* in */
            )
        {
            Result error = null;

            if (!TrySetAppSetting(name, value, ref error))
            {
                bool noComplain = GetNoComplain(ConfigurationOperation.Set);

                if (!noComplain)
                    DebugOps.Complain(ReturnCode.Error, error);
            }
        }

        ///////////////////////////////////////////////////////////////////////

        public static bool TrySetAppSetting(
            string name,     /* in */
            string value,    /* in */
            ref Result error /* out */
            )
        {
            Initialize();

            lock (syncRoot) /* TRANSACTIONAL */
            {
                NameValueCollection appSettings = GetAppSettingsViaAny();

                if (appSettings == null)
                {
                    error = "invalid application settings";
                    return false;
                }

                if (IsReadOnly(appSettings))
                {
                    error = "application settings are read-only";
                    return false;
                }

                appSettings.Set(name, value);
                return true;
            }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Unsetting (Write) Values
        public static void UnsetAppSetting(
            string name /* in */
            )
        {
            Result error = null;

            if (!TryUnsetAppSetting(name, ref error))
            {
                bool noComplain = GetNoComplain(ConfigurationOperation.Unset);

                if (!noComplain)
                    DebugOps.Complain(ReturnCode.Error, error);
            }
        }

        ///////////////////////////////////////////////////////////////////////

        public static bool TryUnsetAppSetting(
            string name,     /* in */
            ref Result error /* out */
            )
        {
            Initialize();

            lock (syncRoot) /* TRANSACTIONAL */
            {
                NameValueCollection appSettings = GetAppSettingsViaAny();

                if (appSettings == null)
                {
                    error = "invalid application settings";
                    return false;
                }

                if (IsReadOnly(appSettings))
                {
                    error = "application settings are read-only";
                    return false;
                }

                appSettings.Remove(name);
                return true;
            }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        public static int Cleanup(
            bool full /* in */
            )
        {
            lock (syncRoot) /* TRANSACTIONAL */
            {
                int result = 0;

#if XML
                if (xmlAppSettings != null)
                {
                    result += xmlAppSettings.Count;

                    xmlAppSettings.Clear();
                    xmlAppSettings = null;
                }

                if (mergedAppSettings != null)
                {
                    result += mergedAppSettings.Count;

                    mergedAppSettings.Clear();
                    mergedAppSettings = null;
                }
#endif

                if (full && (overriddenAppSettings != null))
                {
                    result += overriddenAppSettings.Count;

                    overriddenAppSettings.Clear();
                    overriddenAppSettings = null;
                }

                return result;
            }
        }
        #endregion
    }
}
