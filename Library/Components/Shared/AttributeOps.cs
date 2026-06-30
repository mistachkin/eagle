/*
 * AttributeOps.cs --
 *
 * Copyright (c) 2007-2012 by Joe Mistachkin.  All rights reserved.
 *
 * See the file "license.terms" for information on usage and redistribution of
 * this file, and for a DISCLAIMER OF ALL WARRANTIES.
 *
 * RCS: @(#) $Id: $
 */

using System;
using System.Reflection;

#if !EAGLE
using System.Runtime.InteropServices;
#endif

using Eagle._Attributes;

#if EAGLE
using Eagle._Components.Private;
#endif

namespace Eagle._Components.Shared
{
    /// <summary>
    /// This class provides shared helper methods for retrieving the custom
    /// assembly attributes (such as date/time, release, source identifier, and
    /// various base URIs) that the Eagle software associates with its
    /// assemblies.
    /// </summary>
#if EAGLE
    [ObjectId("b7db31a5-539b-4457-9123-6cdacd4f930c")]
#else
    [Guid("b7db31a5-539b-4457-9123-6cdacd4f930c")]
#endif
    internal static class AttributeOps
    {
        #region Private Constants
        //
        // HACK: This value must be kept synchronized with the UpdateUriName
        //       of the in the Eagle._Components.Private.AttributeOps class.
        //
        /// <summary>
        /// The name identifying the assembly URI used as the base for software
        /// update operations.
        /// </summary>
        private static readonly string UpdateUriName = "update";

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// The name identifying the assembly URI used as the base for download
        /// operations.
        /// </summary>
        private static readonly string DownloadUriName = "download";

        /// <summary>
        /// The name identifying the assembly URI used as the base for script
        /// operations.
        /// </summary>
        private static readonly string ScriptUriName = "script";

        /// <summary>
        /// The name identifying the assembly URI used as the base for auxiliary
        /// operations.
        /// </summary>
        private static readonly string AuxiliaryUriName = "auxiliary";

        /// <summary>
        /// The name identifying the assembly URI used for the XML schema.
        /// </summary>
        private static readonly string XmlSchemaName = "xmlSchema";

        ///////////////////////////////////////////////////////////////////////

#if NETWORK && OFFICIAL_BINARY && !ENTERPRISE_LOCKDOWN
        /// <summary>
        /// The name identifying the assembly URI used for the trusted remote
        /// endpoint.
        /// </summary>
        private static readonly string TrustedRemoteUriName = "trustedRemote";
#endif
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Shared Assembly Attribute Methods
        /// <summary>
        /// This method gets the build date/time associated with the specified
        /// assembly.
        /// </summary>
        /// <param name="assembly">
        /// The assembly to query.  This parameter may be null.
        /// </param>
        /// <returns>
        /// The build date/time associated with the assembly, or
        /// <see cref="DateTime.MinValue" /> if it cannot be determined.
        /// </returns>
        public static DateTime GetAssemblyDateTime(
            Assembly assembly /* in */
            )
        {
            return GetAssemblyDateTime(assembly, null);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method gets the build date/time associated with the specified
        /// assembly, optionally restricting the lookup to the assembly
        /// attribute only.
        /// </summary>
        /// <param name="assembly">
        /// The assembly to query.  This parameter may be null.
        /// </param>
        /// <param name="attributeOnly">
        /// Non-zero to consider the assembly attribute only; when null, this is
        /// auto-detected based on whether the assembly is the core library.
        /// </param>
        /// <returns>
        /// The build date/time associated with the assembly, or
        /// <see cref="DateTime.MinValue" /> if it cannot be determined.
        /// </returns>
        private static DateTime GetAssemblyDateTime(
            Assembly assembly,  /* in */
            bool? attributeOnly /* in */
            )
        {
            if (assembly != null)
            {
                try
                {
                    //
                    // HACK: From now (beta 50) on, this attribute will
                    //       not be defined for any assemblies that do
                    //       not include the associated C# source code
                    //       file in their projects unless an explicit
                    //       DateTime value has been set for the build,
                    //       e.g. during an official stable release.
                    //
                    // NOTE: Currently, only the core library itelf and
                    //       the updater tool will always include this
                    //       attribute.
                    //
                    if (assembly.IsDefined(
                            typeof(AssemblyDateTimeAttribute), false))
                    {
                        AssemblyDateTimeAttribute dateTime =
                            (AssemblyDateTimeAttribute)
                            assembly.GetCustomAttributes(
                                typeof(AssemblyDateTimeAttribute), false)[0];

                        return dateTime.DateTime;
                    }
                }
                catch
                {
                    // do nothing.
                }

                ///////////////////////////////////////////////////////////////

#if EAGLE
                try
                {
                    //
                    // TODO: This auto-detection logic is ugly and most
                    //       likely no longer needed, consider removing
                    //       it.
                    //
                    if (attributeOnly == null)
                        attributeOnly = GlobalState.IsAssembly(assembly);

                    if (!(bool)attributeOnly)
                    {
                        string location = assembly.Location; /* throw */
                        DateTime dateTimeValue = DateTime.MinValue;

                        if (FileOps.GetPeFileDateTime(
                                location, ref dateTimeValue))
                        {
                            return dateTimeValue;
                        }
                    }
                }
                catch
                {
                    // do nothing.
                }
#endif
            }

            return DateTime.MinValue;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method gets the release string associated with the specified
        /// assembly.
        /// </summary>
        /// <param name="assembly">
        /// The assembly to query.  This parameter may be null.
        /// </param>
        /// <returns>
        /// The release string associated with the assembly, or null if it
        /// cannot be determined.
        /// </returns>
        public static string GetAssemblyRelease(
            Assembly assembly /* in */
            )
        {
            if (assembly != null)
            {
                try
                {
                    if (assembly.IsDefined(
                            typeof(AssemblyReleaseAttribute), false))
                    {
                        AssemblyReleaseAttribute release =
                            (AssemblyReleaseAttribute)
                            assembly.GetCustomAttributes(
                                typeof(AssemblyReleaseAttribute), false)[0];

                        return release.Release;
                    }
                }
                catch
                {
                    // do nothing.
                }
            }

            return null;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method gets the source identifier associated with the specified
        /// assembly.
        /// </summary>
        /// <param name="assembly">
        /// The assembly to query.  This parameter may be null.
        /// </param>
        /// <returns>
        /// The source identifier associated with the assembly, or null if it
        /// cannot be determined.
        /// </returns>
        public static string GetAssemblySourceId(
            Assembly assembly /* in */
            )
        {
            if (assembly != null)
            {
                try
                {
                    if (assembly.IsDefined(
                            typeof(AssemblySourceIdAttribute), false))
                    {
                        AssemblySourceIdAttribute sourceId =
                            (AssemblySourceIdAttribute)
                            assembly.GetCustomAttributes(
                                typeof(AssemblySourceIdAttribute), false)[0];

                        return sourceId.SourceId;
                    }
                }
                catch
                {
                    // do nothing.
                }
            }

            return null;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method gets the source time stamp associated with the specified
        /// assembly.
        /// </summary>
        /// <param name="assembly">
        /// The assembly to query.  This parameter may be null.
        /// </param>
        /// <returns>
        /// The source time stamp associated with the assembly, or null if it
        /// cannot be determined.
        /// </returns>
        public static string GetAssemblySourceTimeStamp(
            Assembly assembly /* in */
            )
        {
            if (assembly != null)
            {
                try
                {
                    if (assembly.IsDefined(
                            typeof(AssemblySourceTimeStampAttribute), false))
                    {
                        AssemblySourceTimeStampAttribute sourceTimeStamp =
                            (AssemblySourceTimeStampAttribute)
                            assembly.GetCustomAttributes(
                                typeof(AssemblySourceTimeStampAttribute),
                                false)[0];

                        return sourceTimeStamp.SourceTimeStamp;
                    }
                }
                catch
                {
                    // do nothing.
                }
            }

            return null;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method gets the strong name tag associated with the specified
        /// assembly.
        /// </summary>
        /// <param name="assembly">
        /// The assembly to query.  This parameter may be null.
        /// </param>
        /// <returns>
        /// The strong name tag associated with the assembly, or null if it
        /// cannot be determined.
        /// </returns>
        public static string GetAssemblyStrongNameTag(
            Assembly assembly /* in */
            )
        {
            if (assembly != null)
            {
                try
                {
                    if (assembly.IsDefined(
                            typeof(AssemblyStrongNameTagAttribute), false))
                    {
                        AssemblyStrongNameTagAttribute strongNameTag =
                            (AssemblyStrongNameTagAttribute)
                            assembly.GetCustomAttributes(
                                typeof(AssemblyStrongNameTagAttribute),
                                false)[0];

                        return strongNameTag.StrongNameTag;
                    }
                }
                catch
                {
                    // do nothing.
                }
            }

            return null;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method gets the tag associated with the specified assembly.
        /// </summary>
        /// <param name="assembly">
        /// The assembly to query.  This parameter may be null.
        /// </param>
        /// <returns>
        /// The tag associated with the assembly, or null if it cannot be
        /// determined.
        /// </returns>
        public static string GetAssemblyTag(
            Assembly assembly /* in */
            )
        {
            if (assembly != null)
            {
                try
                {
                    if (assembly.IsDefined(
                            typeof(AssemblyTagAttribute), false))
                    {
                        AssemblyTagAttribute tag =
                            (AssemblyTagAttribute)
                            assembly.GetCustomAttributes(
                                typeof(AssemblyTagAttribute), false)[0];

                        return tag.Tag;
                    }
                }
                catch
                {
                    // do nothing.
                }
            }

            return null;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method gets the descriptive text associated with the specified
        /// assembly.
        /// </summary>
        /// <param name="assembly">
        /// The assembly to query.  This parameter may be null.
        /// </param>
        /// <returns>
        /// The descriptive text associated with the assembly, or null if it
        /// cannot be determined.
        /// </returns>
        public static string GetAssemblyText(
            Assembly assembly /* in */
            )
        {
            if (assembly != null)
            {
                try
                {
                    if (assembly.IsDefined(
                            typeof(AssemblyTextAttribute), false))
                    {
                        AssemblyTextAttribute text =
                            (AssemblyTextAttribute)
                            assembly.GetCustomAttributes(
                                typeof(AssemblyTextAttribute), false)[0];

                        return text.Text;
                    }
                }
                catch
                {
                    // do nothing.
                }
            }

            return null;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method gets the title associated with the specified assembly.
        /// </summary>
        /// <param name="assembly">
        /// The assembly to query.  This parameter may be null.
        /// </param>
        /// <returns>
        /// The title associated with the assembly, or null if it cannot be
        /// determined.
        /// </returns>
        public static string GetAssemblyTitle(
            Assembly assembly /* in */
            )
        {
            if (assembly != null)
            {
                try
                {
                    if (assembly.IsDefined(
                            typeof(AssemblyTitleAttribute), false))
                    {
                        AssemblyTitleAttribute title =
                            (AssemblyTitleAttribute)
                            assembly.GetCustomAttributes(
                                typeof(AssemblyTitleAttribute), false)[0];

                        return title.Title;
                    }
                }
                catch
                {
                    // do nothing.
                }
            }

            return null;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method gets the default URI associated with the specified
        /// assembly.
        /// </summary>
        /// <param name="assembly">
        /// The assembly to query.  This parameter may be null.
        /// </param>
        /// <returns>
        /// The default URI associated with the assembly, or null if it cannot
        /// be determined.
        /// </returns>
        public static Uri GetAssemblyUri(
            Assembly assembly /* in */
            )
        {
            return GetAssemblyUri(assembly, null);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method gets the URI with the specified name that is associated
        /// with the specified assembly.
        /// </summary>
        /// <param name="assembly">
        /// The assembly to query.  This parameter may be null.
        /// </param>
        /// <param name="name">
        /// The name identifying the URI to retrieve.  This parameter may be
        /// null.
        /// </param>
        /// <returns>
        /// The matching URI associated with the assembly, or null if it cannot
        /// be determined.
        /// </returns>
        public static Uri GetAssemblyUri(
            Assembly assembly, /* in */
            string name        /* in */
            )
        {
            if (assembly != null)
            {
                try
                {
                    if (assembly.IsDefined(
                            typeof(AssemblyUriAttribute), false))
                    {
                        object[] attributes = assembly.GetCustomAttributes(
                            typeof(AssemblyUriAttribute), false);

                        if (attributes != null)
                        {
                            foreach (object attribute in attributes)
                            {
                                AssemblyUriAttribute uri =
                                    attribute as AssemblyUriAttribute;

                                if ((uri != null) &&
                                    StringOps.SystemEquals(uri.Name, name))
                                {
                                    return uri.Uri;
                                }
                            }
                        }
                    }
                }
                catch
                {
                    // do nothing.
                }
            }

            return null;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method gets the base URI used for software update operations
        /// associated with the specified assembly.
        /// </summary>
        /// <param name="assembly">
        /// The assembly to query.  This parameter may be null.
        /// </param>
        /// <returns>
        /// The update base URI associated with the assembly, or null if it
        /// cannot be determined.
        /// </returns>
        public static Uri GetAssemblyUpdateBaseUri(
            Assembly assembly /* in */
            )
        {
            //
            // TODO: Make a new assembly attribute for this?  In addition,
            //       the GlobalState.thisAssemblyUpdateBaseUri field would
            //       most likely need to be changed as well.
            //
            Uri uri = GetAssemblyUri(assembly, UpdateUriName);

            if (uri != null)
                return uri;

            return GetAssemblyUri(assembly); /* COMPAT: Eagle beta */
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method gets the base URI used for download operations
        /// associated with the specified assembly.
        /// </summary>
        /// <param name="assembly">
        /// The assembly to query.  This parameter may be null.
        /// </param>
        /// <returns>
        /// The download base URI associated with the assembly, or null if it
        /// cannot be determined.
        /// </returns>
        public static Uri GetAssemblyDownloadBaseUri(
            Assembly assembly /* in */
            )
        {
            //
            // TODO: Make a new assembly attribute for this?  In addition,
            //       the GlobalState.thisAssemblyDownloadBaseUri field would
            //       most likely need to be changed as well.
            //
            Uri uri = GetAssemblyUri(assembly, DownloadUriName);

            if (uri != null)
                return uri;

            return GetAssemblyUri(assembly); /* COMPAT: Eagle beta */
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method gets the base URI used for script operations associated
        /// with the specified assembly.
        /// </summary>
        /// <param name="assembly">
        /// The assembly to query.  This parameter may be null.
        /// </param>
        /// <returns>
        /// The script base URI associated with the assembly, or null if it
        /// cannot be determined.
        /// </returns>
        public static Uri GetAssemblyScriptBaseUri(
            Assembly assembly /* in */
            )
        {
            //
            // TODO: Make a new assembly attribute for this?  In addition,
            //       the GlobalState.thisAssemblyScriptBaseUri field would
            //       most likely need to be changed as well.
            //
            Uri uri = GetAssemblyUri(assembly, ScriptUriName);

            if (uri != null)
                return uri;

            return GetAssemblyUri(assembly); /* COMPAT: Eagle beta */
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method gets the base URI used for auxiliary operations
        /// associated with the specified assembly.
        /// </summary>
        /// <param name="assembly">
        /// The assembly to query.  This parameter may be null.
        /// </param>
        /// <returns>
        /// The auxiliary base URI associated with the assembly, or null if it
        /// cannot be determined.
        /// </returns>
        public static Uri GetAssemblyAuxiliaryBaseUri(
            Assembly assembly /* in */
            )
        {
            //
            // TODO: Make a new assembly attribute for this?  In addition,
            //       the GlobalState.thisAssemblyAuxiliaryBaseUri field would
            //       most likely need to be changed as well.
            //
            Uri uri = GetAssemblyUri(assembly, AuxiliaryUriName);

            if (uri != null)
                return uri;

            return GetAssemblyUri(assembly); /* COMPAT: Eagle beta */
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method gets the XML schema URI associated with the specified
        /// assembly.
        /// </summary>
        /// <param name="assembly">
        /// The assembly to query.  This parameter may be null.
        /// </param>
        /// <returns>
        /// The XML schema URI associated with the assembly, or null if it
        /// cannot be determined.
        /// </returns>
        public static Uri GetAssemblyXmlSchemaUri(
            Assembly assembly /* in */
            )
        {
            //
            // TODO: Make a new assembly attribute for this?  In addition,
            //       the GlobalState.thisAssemblyNamespaceUri field would
            //       most likely need to be changed as well.
            //
            Uri uri = GetAssemblyUri(assembly, XmlSchemaName);

            if (uri != null)
                return uri;

            return GetAssemblyUri(assembly); /* COMPAT: Eagle beta */
        }

        ///////////////////////////////////////////////////////////////////////

#if NETWORK && OFFICIAL_BINARY && !ENTERPRISE_LOCKDOWN
        /// <summary>
        /// This method gets the trusted remote URI associated with the
        /// specified assembly.
        /// </summary>
        /// <param name="assembly">
        /// The assembly to query.  This parameter may be null.
        /// </param>
        /// <returns>
        /// The trusted remote URI associated with the assembly, or null if it
        /// cannot be determined.
        /// </returns>
        public static Uri GetAssemblyTrustedRemoteUri(
            Assembly assembly /* in */
            )
        {
            return GetAssemblyUri(assembly, TrustedRemoteUriName);
        }
#endif
        #endregion
    }
}
