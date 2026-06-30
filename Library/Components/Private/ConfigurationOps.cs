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
    /// <summary>
    /// This class provides the helper methods used to read, write, and
    /// otherwise manage the application settings (i.e. the "appSettings")
    /// used by the Eagle core library, including support for loading and
    /// saving those settings via external XML configuration files.
    /// </summary>
    [ObjectId("df98c383-ae1f-46b5-a3ab-a3902d186498")]
    internal static class ConfigurationOps
    {
        #region Private Constants
#if CONFIGURATION || XML
        //
        // NOTE: This is the name of the XML element that contains settings
        //       for the application.
        //
        /// <summary>
        /// This is the name of the XML element that contains the settings for
        /// the application.
        /// </summary>
        private static readonly string AppSettingsName = "appSettings";
#endif

        ///////////////////////////////////////////////////////////////////////

#if XML
        //
        // NOTE: This is the name of the XML element that contains the
        //       whole configuration for the application.
        //
        /// <summary>
        /// This is the name of the XML element that contains the whole
        /// configuration for the application.
        /// </summary>
        private static readonly string ConfigurationName = "configuration";

        ///////////////////////////////////////////////////////////////////////

        //
        // NOTE: This is the namespace name for application configuration
        //       files, at the <configuration> element level.
        //
        /// <summary>
        /// This is the namespace name for application configuration files, at
        /// the <c>configuration</c> element level.
        /// </summary>
        private static readonly string NamespaceName = "dnfcfg";

        ///////////////////////////////////////////////////////////////////////

        //
        // NOTE: This is the namespace URI for application configuration
        //       files, at the <configuration> element level.
        //
        /// <summary>
        /// This is the namespace URI for application configuration files, at
        /// the <c>configuration</c> element level.
        /// </summary>
        private static readonly Uri NamespaceUri = new Uri(
            "http://schemas.microsoft.com/.NetConfiguration/v2.0",
            UriKind.Absolute);

        ///////////////////////////////////////////////////////////////////////

        //
        // NOTE: The candidate XPath queries used to extract appSettings
        //       from an XML document.  The first query that returns some
        //       nodes wins.
        //
        /// <summary>
        /// This is the list of candidate XPath queries used to extract the
        /// appSettings from an XML document.  The first query that returns
        /// one or more nodes is used.
        /// </summary>
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
        /// <summary>
        /// This is the list of candidate XPath queries used to locate where
        /// the appSettings should be added within an XML document.  The first
        /// query that returns exactly one node is used.
        /// </summary>
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
        /// <summary>
        /// This is the XML template used to create a new, empty application
        /// configuration document.
        /// </summary>
        private static string TemplateXml =
            "<?xml version=\"1.0\" encoding=\"UTF-8\" ?>\r\n" +
            "<" + ConfigurationName + "><" + AppSettingsName +
            "></" + AppSettingsName + "></" + ConfigurationName + ">";

        ///////////////////////////////////////////////////////////////////////

        //
        // HACK: These are purposely not read-only.
        //
        /// <summary>
        /// This is the name of the XML element used to clear all of the
        /// application settings.
        /// </summary>
        private static string ClearElementName = "clear";
        /// <summary>
        /// This is the name of the XML element used to add an application
        /// setting.
        /// </summary>
        private static string AddElementName = "add";
        /// <summary>
        /// This is the name of the XML element used to remove an application
        /// setting.
        /// </summary>
        private static string RemoveElementName = "remove";
        /// <summary>
        /// This is the name of the XML element used to set the value of an
        /// application setting.
        /// </summary>
        private static string SetElementName = "set";

        ///////////////////////////////////////////////////////////////////////

        //
        // HACK: These are purposely not read-only.
        //
        /// <summary>
        /// This is the name of the XML element used to reset the cached XML
        /// application settings.
        /// </summary>
        private static string ResetCacheElementName = "resetCache";
        /// <summary>
        /// This is the name of the XML element used to replace the cached XML
        /// application settings.
        /// </summary>
        private static string ReplaceCacheElementName = "replaceCache";
        /// <summary>
        /// This is the name of the XML element used to reset the overridden
        /// application settings.
        /// </summary>
        private static string ResetOverrideElementName = "resetOverride";
        /// <summary>
        /// This is the name of the XML element used to replace the overridden
        /// application settings.
        /// </summary>
        private static string ReplaceOverrideElementName = "replaceOverride";

        ///////////////////////////////////////////////////////////////////////

        //
        // HACK: These are purposely not read-only.
        //
        /// <summary>
        /// This is the name of the XML attribute that contains the name (key)
        /// of an application setting.
        /// </summary>
        private static string KeyAttributeName = "key";
        /// <summary>
        /// This is the name of the XML attribute that contains the value of an
        /// application setting.
        /// </summary>
        private static string ValueAttributeName = "value";
#endif
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Private Data
        /// <summary>
        /// This object is used to synchronize access to the static state of
        /// this class.
        /// </summary>
        private static readonly object syncRoot = new object();

        ///////////////////////////////////////////////////////////////////////

#if XML
        /// <summary>
        /// When greater than zero, application settings may be read from
        /// external XML configuration files.  This value defaults to enabled
        /// when running on .NET Core.
        /// </summary>
        private static int useXmlFiles = CommonOps.Runtime.IsDotNetCore() ?
            1 : 0;

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// When greater than zero, web-style XML configuration files are also
        /// considered when reading application settings.  This value defaults
        /// to enabled when running on .NET Core.
        /// </summary>
        private static int useWebFiles = CommonOps.Runtime.IsDotNetCore() ?
            1 : 0;

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// When true, application settings read from multiple XML
        /// configuration files are merged together instead of using only the
        /// first file found.
        /// </summary>
        private static bool mergeXmlAppSettings = false;

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// When true, application settings read from the XML configuration
        /// files are merged with those provided by the configuration manager.
        /// </summary>
        private static bool mergeAllAppSettings = false;

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This is the cached collection of application settings that were
        /// read from the XML configuration files, if any.
        /// </summary>
        private static NameValueCollection xmlAppSettings;

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This is the cached collection of application settings produced by
        /// merging the XML and configuration manager settings, if any.
        /// </summary>
        private static NameValueCollection mergedAppSettings;
#endif

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// When non-null, this indicates whether errors encountered while
        /// getting (reading) an application setting should be suppressed
        /// instead of reported.
        /// </summary>
        private static bool? noComplainGet;
        /// <summary>
        /// When non-null, this indicates whether errors encountered while
        /// setting (writing) an application setting should be suppressed
        /// instead of reported.
        /// </summary>
        private static bool? noComplainSet;
        /// <summary>
        /// When non-null, this indicates whether errors encountered while
        /// unsetting (removing) an application setting should be suppressed
        /// instead of reported.
        /// </summary>
        private static bool? noComplainUnset;

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This is the cached reflection information for the protected
        /// IsReadOnly property of the NameValueCollection class, used to
        /// determine whether a settings collection is read-only.
        /// </summary>
        private static PropertyInfo isReadOnlyPropertyInfo;

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This is the collection of application settings that has been
        /// explicitly set to override those normally provided by the
        /// configuration manager or XML files, if any.
        /// </summary>
        private static NameValueCollection overriddenAppSettings;
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Introspection Support Methods
        //
        // NOTE: Used by the _Hosts.Default.BuildEngineInfoList method.
        //
        /// <summary>
        /// This method adds rows of diagnostic information about the
        /// configuration subsystem to the specified list.
        /// </summary>
        /// <param name="list">
        /// Upon return, the rows of diagnostic information are added to this
        /// list.  If this value is null, this method does nothing.
        /// </param>
        /// <param name="detailFlags">
        /// The flags used to control the level of detail included in the
        /// diagnostic information.
        /// </param>
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
        /// <summary>
        /// This method adds a diagnostic trace message related to the
        /// configuration subsystem.
        /// </summary>
        /// <param name="message">
        /// The diagnostic message to be added to the trace log.
        /// </param>
        /// <param name="priority">
        /// The priority of the diagnostic message.
        /// </param>
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
        /// <summary>
        /// This method initializes the various XML configuration file related
        /// settings based on the presence of their associated environment
        /// variables.
        /// </summary>
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

        /// <summary>
        /// This method determines whether application settings should be read
        /// from external XML configuration files.
        /// </summary>
        /// <returns>
        /// True if XML configuration files should be used; otherwise, false.
        /// </returns>
        private static bool ShouldUseXmlFiles()
        {
            return Interlocked.CompareExchange(ref useXmlFiles, 0, 0) > 0;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method enables or disables reading application settings from
        /// external XML configuration files.
        /// </summary>
        /// <param name="enable">
        /// Non-zero to enable use of XML configuration files; zero to disable
        /// it.
        /// </param>
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

        /// <summary>
        /// This method determines whether web-style XML configuration files
        /// should be considered when reading application settings.
        /// </summary>
        /// <returns>
        /// True if web-style XML configuration files should be used;
        /// otherwise, false.
        /// </returns>
        private static bool ShouldUseWebFiles()
        {
            return Interlocked.CompareExchange(ref useWebFiles, 0, 0) > 0;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method enables or disables considering web-style XML
        /// configuration files when reading application settings.
        /// </summary>
        /// <param name="enable">
        /// Non-zero to enable use of web-style XML configuration files; zero
        /// to disable it.
        /// </param>
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

        /// <summary>
        /// This method sets the cached collection of application settings read
        /// from the XML configuration files.
        /// </summary>
        /// <param name="appSettings">
        /// The collection of application settings to be cached.
        /// </param>
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

        /// <summary>
        /// This method clears and resets the cached collection of application
        /// settings read from the XML configuration files.
        /// </summary>
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

        /// <summary>
        /// This method sets the cached collection of merged application
        /// settings.
        /// </summary>
        /// <param name="appSettings">
        /// The collection of merged application settings to be cached.
        /// </param>
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

        /// <summary>
        /// This method clears and resets the cached collection of merged
        /// application settings.
        /// </summary>
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

        /// <summary>
        /// This method determines whether application settings read from
        /// multiple XML configuration files should be merged together.
        /// </summary>
        /// <returns>
        /// True if the XML application settings should be merged; otherwise,
        /// false.
        /// </returns>
        private static bool ShouldMergeXmlAppSettings()
        {
            lock (syncRoot)
            {
                return mergeXmlAppSettings;
            }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method enables or disables merging of application settings
        /// read from multiple XML configuration files.
        /// </summary>
        /// <param name="enable">
        /// True to enable merging of the XML application settings; false to
        /// disable it.
        /// </param>
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

        /// <summary>
        /// This method determines whether the XML application settings should
        /// be merged with those provided by the configuration manager.
        /// </summary>
        /// <returns>
        /// True if all of the application settings should be merged;
        /// otherwise, false.
        /// </returns>
        private static bool ShouldMergeAllAppSettings()
        {
            lock (syncRoot)
            {
                return mergeAllAppSettings;
            }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method enables or disables merging of the XML application
        /// settings with those provided by the configuration manager.
        /// </summary>
        /// <param name="enable">
        /// True to enable merging of all of the application settings; false to
        /// disable it.
        /// </param>
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

        /// <summary>
        /// This method returns the set of file system locations that should be
        /// searched for XML configuration files.
        /// </summary>
        /// <returns>
        /// The set of candidate locations to be searched.
        /// </returns>
        private static IEnumerable<string> GetAppSettingsLocations()
        {
            return new string[] {
                GlobalState.GetEntryAssemblyLocation(),
                GlobalState.GetAssemblyLocation(),
                PathOps.GetExecutableName()
            };
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method returns the fallback file system location that should
        /// be searched for XML configuration files.
        /// </summary>
        /// <returns>
        /// The fallback location to be searched.
        /// </returns>
        private static string GetAppSettingsFallbackLocation()
        {
            return PathOps.GetManagedExecutableName();
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method builds the list of unique XML configuration file names
        /// to be searched, based on the specified candidate locations.
        /// </summary>
        /// <param name="locations">
        /// The candidate file system locations to be searched.  If this value
        /// is null, no file names are returned.
        /// </param>
        /// <param name="fallbackLocation">
        /// The fallback file system location to be searched.
        /// </param>
        /// <param name="includeFallback">
        /// Non-zero to always include configuration file names derived from
        /// the fallback location.
        /// </param>
        /// <param name="includeWeb">
        /// Non-zero to include web-style configuration file names.
        /// </param>
        /// <returns>
        /// The list of unique XML configuration file names, or null if there
        /// are none.
        /// </returns>
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

        /// <summary>
        /// This method reads application settings from the specified XML
        /// configuration file.
        /// </summary>
        /// <param name="fileName">
        /// The name of the XML configuration file to be read.
        /// </param>
        /// <param name="error">
        /// Upon failure, this contains an appropriate error message.
        /// </param>
        /// <returns>
        /// The collection of application settings read from the file, or null
        /// if they could not be read.
        /// </returns>
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

        /// <summary>
        /// This method writes the specified application settings to an XML
        /// configuration file.
        /// </summary>
        /// <param name="fileName">
        /// The name of the XML configuration file to be written.
        /// </param>
        /// <param name="appSettings">
        /// The collection of application settings to be written.
        /// </param>
        /// <param name="error">
        /// Upon failure, this contains an appropriate error message.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise, an appropriate
        /// error code.
        /// </returns>
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

        /// <summary>
        /// This method merges the application settings from one collection
        /// into another.
        /// </summary>
        /// <param name="appSettings1">
        /// Upon success, this contains the merged application settings.  If
        /// this value is null, a new collection is created.
        /// </param>
        /// <param name="appSettings2">
        /// The collection of application settings to be merged into the first
        /// collection.  If this value is null, this method does nothing.
        /// </param>
        /// <param name="unique">
        /// Non-zero to skip settings whose name already exists in the
        /// destination collection.
        /// </param>
        /// <param name="append">
        /// Non-zero to add the merged settings (allowing duplicates); zero to
        /// set them (replacing any existing value).
        /// </param>
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

        /// <summary>
        /// This method reads application settings from the available XML
        /// configuration files, optionally merging the settings from multiple
        /// files together.
        /// </summary>
        /// <param name="locations">
        /// The candidate file system locations to be searched.
        /// </param>
        /// <param name="fallbackLocation">
        /// The fallback file system location to be searched.
        /// </param>
        /// <param name="includeFallback">
        /// Non-zero to always include configuration file names derived from
        /// the fallback location.
        /// </param>
        /// <param name="includeWeb">
        /// Non-zero to include web-style configuration file names.
        /// </param>
        /// <param name="merge">
        /// Non-zero to merge the settings read from all of the files together;
        /// zero to use only the first file successfully read.
        /// </param>
        /// <returns>
        /// The collection of application settings read from the XML
        /// configuration files, or null if there are none.
        /// </returns>
        private static NameValueCollection GetAppSettingsViaXmlFiles(
            IEnumerable<string> locations, /* in */
            string fallbackLocation,       /* in */
            bool includeFallback,          /* in */
            bool includeWeb,               /* in */
            bool merge                     /* in */
            )
        {
#if DEBUG && VERBOSE
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
#endif

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
#if DEBUG && VERBOSE
                        traceDescription = GetTraceDescription(
                            appSettings);

                        DebugTrace(String.Format(
                            "GetAppSettingsViaXmlFiles: {0} file " +
                            "{1} read from {2}", merge ? "merging" :
                            "using", traceDescription,
                            FormatOps.WrapOrNull(fileName)),
                            TracePriority.StartupDebug);
#endif

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
#if DEBUG && VERBOSE
                    lock (syncRoot) /* TRANSACTIONAL */
                    {
                        traceDescription = GetTraceDescription(
                            xmlAppSettings);
                    }

                    DebugTrace(String.Format(
                        "GetAppSettingsViaXmlFiles: using merged {0}",
                        traceDescription), TracePriority.StartupDebug);
#endif

                    lock (syncRoot)
                    {
                        return xmlAppSettings;
                    }
                }
            }

#if DEBUG && VERBOSE
            //
            // NOTE: This is not an error.  Just return no settings
            //       because no XML configuration files are present.
            //
            DebugTrace(
                "GetAppSettingsViaXmlFiles: skipping files because " +
                "they did not exist", TracePriority.StartupDebug);
#endif

            return null;
        }
#endif
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Private Methods
        /// <summary>
        /// This method returns the application settings provided by the
        /// configuration manager.
        /// </summary>
        /// <returns>
        /// The collection of application settings provided by the
        /// configuration manager, or null if they are unavailable.
        /// </returns>
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

#if DEBUG && VERBOSE
            DebugTrace(String.Format(
                "GetAppSettingsViaManager: using built-in {0}",
                GetTraceDescription(appSettings)),
                TracePriority.StartupDebug2);
#endif

            return appSettings;
#else
#if DEBUG && VERBOSE
            DebugTrace(
                "GetAppSettingsViaManager: built-in settings unavailable",
                TracePriority.StartupDebug2);
#endif

            return null;
#endif
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method builds a human-readable description of the specified
        /// collection of application settings, suitable for use in a
        /// diagnostic trace message.
        /// </summary>
        /// <param name="appSettings">
        /// The collection of application settings to be described.
        /// </param>
        /// <returns>
        /// The description of the collection of application settings.  This
        /// method will never return null.
        /// </returns>
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

        /// <summary>
        /// This method initializes the static state of this class, including
        /// the default values used to control whether errors are reported.
        /// </summary>
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

        /// <summary>
        /// This method returns the effective application settings, preferring
        /// the overridden settings, then those read from the XML configuration
        /// files, and finally those provided by the configuration manager.
        /// </summary>
        /// <returns>
        /// The effective collection of application settings, or null if none
        /// are available.
        /// </returns>
        private static NameValueCollection GetAppSettingsViaAny()
        {
#if DEBUG && VERBOSE
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
#endif

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
#if DEBUG && VERBOSE
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
#endif

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

        /// <summary>
        /// This method ensures that the collection of overridden application
        /// settings has been created.
        /// </summary>
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

        /// <summary>
        /// This method determines whether the collection of overridden
        /// application settings has been created.
        /// </summary>
        /// <returns>
        /// True if the overridden application settings exist; otherwise,
        /// false.
        /// </returns>
        private static bool HaveAppSettings()
        {
            lock (syncRoot)
            {
                return (overriddenAppSettings != null);
            }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method returns the collection of overridden application
        /// settings.
        /// </summary>
        /// <returns>
        /// The collection of overridden application settings, or null if there
        /// is none.
        /// </returns>
        public static NameValueCollection GetAppSettings()
        {
            lock (syncRoot)
            {
                return overriddenAppSettings;
            }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method sets the collection of overridden application settings.
        /// </summary>
        /// <param name="appSettings">
        /// The collection of application settings to be used to override the
        /// normal application settings.
        /// </param>
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

        /// <summary>
        /// This method clears and resets the collection of overridden
        /// application settings.
        /// </summary>
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

        /// <summary>
        /// This method loads application settings from the specified XML
        /// configuration file, optionally merging them with any existing
        /// overridden settings.
        /// </summary>
        /// <param name="fileName">
        /// The name of the XML configuration file to be loaded.
        /// </param>
        /// <param name="merge">
        /// Non-zero to merge the loaded settings with any existing overridden
        /// settings; zero to require that no settings have been loaded yet.
        /// </param>
        /// <param name="error">
        /// Upon failure, this contains an appropriate error message.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise, an appropriate
        /// error code.
        /// </returns>
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

        /// <summary>
        /// This method saves the overridden application settings to the
        /// specified XML configuration file.
        /// </summary>
        /// <param name="fileName">
        /// The name of the XML configuration file to be written.
        /// </param>
        /// <param name="error">
        /// Upon failure, this contains an appropriate error message.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise, an appropriate
        /// error code.
        /// </returns>
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

        /// <summary>
        /// This method determines whether the specified collection of
        /// application settings is read-only.
        /// </summary>
        /// <param name="appSettings">
        /// The collection of application settings to be checked.
        /// </param>
        /// <returns>
        /// True if the collection of application settings is read-only;
        /// otherwise, false.
        /// </returns>
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
        /// <summary>
        /// This method determines whether any application settings are
        /// available.  This method assumes the lock is already held.
        /// </summary>
        /// <param name="moreThanZero">
        /// Non-zero to require that at least one application setting is
        /// present; zero to require only that a settings collection is
        /// available.
        /// </param>
        /// <returns>
        /// True if the required application settings are available; otherwise,
        /// false.
        /// </returns>
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

        /// <summary>
        /// This method determines whether errors encountered during the
        /// specified configuration operation should be suppressed instead of
        /// reported.
        /// </summary>
        /// <param name="operation">
        /// The configuration operation being performed.
        /// </param>
        /// <returns>
        /// True if errors for the specified operation should be suppressed;
        /// otherwise, false.
        /// </returns>
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
        /// <summary>
        /// This method determines whether any application settings are
        /// available.
        /// </summary>
        /// <param name="moreThanZero">
        /// Non-zero to require that at least one application setting is
        /// present; zero to require only that a settings collection is
        /// available.
        /// </param>
        /// <returns>
        /// True if the required application settings are available; otherwise,
        /// false.
        /// </returns>
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

        /// <summary>
        /// This method returns the value of the named application setting.
        /// </summary>
        /// <param name="name">
        /// The name of the application setting to return.
        /// </param>
        /// <returns>
        /// The value of the named application setting, or null if it does not
        /// exist.
        /// </returns>
        public static string GetAppSetting(
            string name /* in */
            )
        {
            return GetAppSetting(name, null);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method returns the value of the named application setting,
        /// returning a default value if it does not exist.
        /// </summary>
        /// <param name="name">
        /// The name of the application setting to return.
        /// </param>
        /// <param name="default">
        /// The value to return if the named application setting does not
        /// exist.
        /// </param>
        /// <returns>
        /// The value of the named application setting, or the specified
        /// default value if it does not exist.
        /// </returns>
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

        /// <summary>
        /// This method attempts to get the value of the named application
        /// setting.
        /// </summary>
        /// <param name="name">
        /// The name of the application setting to get.
        /// </param>
        /// <param name="value">
        /// Upon success, this contains the value of the named application
        /// setting.  Upon failure, this is null.
        /// </param>
        /// <param name="error">
        /// Upon failure, this contains an appropriate error message.
        /// </param>
        /// <returns>
        /// True if the named application setting was found; otherwise, false.
        /// </returns>
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
        /// <summary>
        /// This method attempts to get the value of the named application
        /// setting as an integer.
        /// </summary>
        /// <param name="name">
        /// The name of the application setting to get.
        /// </param>
        /// <param name="value">
        /// Upon success, this contains the integer value of the named
        /// application setting.  Upon failure, this is the default integer
        /// value.
        /// </param>
        /// <returns>
        /// True if the named application setting was found and successfully
        /// converted to an integer; otherwise, false.
        /// </returns>
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
        /// <summary>
        /// This method attempts to get the value of the named application
        /// setting as an integer.
        /// </summary>
        /// <param name="name">
        /// The name of the application setting to get.
        /// </param>
        /// <param name="value">
        /// Upon success, this contains the integer value of the named
        /// application setting.  Upon failure, this is the default integer
        /// value.
        /// </param>
        /// <param name="error">
        /// Upon failure, this contains an appropriate error message.
        /// </param>
        /// <returns>
        /// True if the named application setting was found and successfully
        /// converted to an integer; otherwise, false.
        /// </returns>
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
        /// <summary>
        /// This method attempts to get the value of the named application
        /// setting as a list.
        /// </summary>
        /// <param name="name">
        /// The name of the application setting to get.
        /// </param>
        /// <param name="value">
        /// Upon success, this contains the list value of the named application
        /// setting.  Upon failure, this is null.
        /// </param>
        /// <param name="error">
        /// Upon failure, this contains an appropriate error message.
        /// </param>
        /// <returns>
        /// True if the named application setting was found and successfully
        /// parsed as a list; otherwise, false.
        /// </returns>
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
        /// <summary>
        /// This method attempts to get the value of the named application
        /// setting as a boolean.
        /// </summary>
        /// <param name="name">
        /// The name of the application setting to get.
        /// </param>
        /// <param name="value">
        /// Upon success, this contains the boolean value of the named
        /// application setting.  Upon failure, this is the default boolean
        /// value.
        /// </param>
        /// <param name="error">
        /// Upon failure, this contains an appropriate error message.
        /// </param>
        /// <returns>
        /// True if the named application setting was found and successfully
        /// converted to a boolean; otherwise, false.
        /// </returns>
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
        /// <summary>
        /// This method attempts to get the value of the named application
        /// setting as a value of the specified enumerated type.
        /// </summary>
        /// <param name="name">
        /// The name of the application setting to get.
        /// </param>
        /// <param name="enumType">
        /// The enumerated type that the application setting value should be
        /// converted to.
        /// </param>
        /// <param name="oldValue">
        /// The existing enumerated value to combine with the parsed value, used
        /// when the enumerated type has the flags attribute.
        /// </param>
        /// <param name="value">
        /// Upon success, this contains the enumerated value of the named
        /// application setting.  Upon failure, this is null.
        /// </param>
        /// <param name="error">
        /// Upon failure, this contains an appropriate error message.
        /// </param>
        /// <returns>
        /// True if the named application setting was found and successfully
        /// converted to the enumerated type; otherwise, false.
        /// </returns>
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
        /// <summary>
        /// This method attempts to get the value of the named application
        /// setting as an opaque object value, resolving the setting value as an
        /// object handle within the specified interpreter.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter context used to resolve the object handle.
        /// </param>
        /// <param name="name">
        /// The name of the application setting to get.
        /// </param>
        /// <param name="lookupFlags">
        /// The flags used to control how the object handle is resolved.
        /// </param>
        /// <param name="value">
        /// Upon success, this contains the object value associated with the
        /// named application setting.  Upon failure, this is null.
        /// </param>
        /// <param name="error">
        /// Upon failure, this contains an appropriate error message.
        /// </param>
        /// <returns>
        /// True if the named application setting was found and successfully
        /// resolved to an object value; otherwise, false.
        /// </returns>
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
        /// <summary>
        /// This method sets the value of the named application setting.
        /// </summary>
        /// <param name="name">
        /// The name of the application setting to set.
        /// </param>
        /// <param name="value">
        /// The value to be assigned to the named application setting.
        /// </param>
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

        /// <summary>
        /// This method attempts to set the value of the named application
        /// setting.
        /// </summary>
        /// <param name="name">
        /// The name of the application setting to set.
        /// </param>
        /// <param name="value">
        /// The value to be assigned to the named application setting.
        /// </param>
        /// <param name="error">
        /// Upon failure, this contains an appropriate error message.
        /// </param>
        /// <returns>
        /// True if the named application setting was successfully set;
        /// otherwise, false.
        /// </returns>
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
        /// <summary>
        /// This method unsets (removes) the named application setting.
        /// </summary>
        /// <param name="name">
        /// The name of the application setting to unset.
        /// </param>
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

        /// <summary>
        /// This method attempts to unset (remove) the named application
        /// setting.
        /// </summary>
        /// <param name="name">
        /// The name of the application setting to unset.
        /// </param>
        /// <param name="error">
        /// Upon failure, this contains an appropriate error message.
        /// </param>
        /// <returns>
        /// True if the named application setting was successfully unset;
        /// otherwise, false.
        /// </returns>
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

        /// <summary>
        /// This method releases the cached and overridden application
        /// settings, returning the total number of settings that were
        /// released.
        /// </summary>
        /// <param name="full">
        /// Non-zero to also release the overridden application settings; zero
        /// to release only the cached settings.
        /// </param>
        /// <returns>
        /// The total number of application settings that were released.
        /// </returns>
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
