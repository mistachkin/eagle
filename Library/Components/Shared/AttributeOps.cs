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
        private static readonly string UpdateUriName = "update";

        ///////////////////////////////////////////////////////////////////////

        private static readonly string DownloadUriName = "download";
        private static readonly string ScriptUriName = "script";
        private static readonly string AuxiliaryUriName = "auxiliary";
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Shared Assembly Attribute Methods
        public static DateTime GetAssemblyDateTime(
            Assembly assembly
            )
        {
            return GetAssemblyDateTime(assembly, null);
        }

        ///////////////////////////////////////////////////////////////////////

        private static DateTime GetAssemblyDateTime(
            Assembly assembly,
            bool? attributeOnly
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

        public static string GetAssemblyRelease(
            Assembly assembly
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

        public static string GetAssemblySourceId(
            Assembly assembly
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

        public static string GetAssemblySourceTimeStamp(
            Assembly assembly
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

        public static string GetAssemblyStrongNameTag(
            Assembly assembly
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

        public static string GetAssemblyTag(
            Assembly assembly
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

        public static string GetAssemblyText(
            Assembly assembly
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

        public static string GetAssemblyTitle(
            Assembly assembly
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

        public static Uri GetAssemblyUri(
            Assembly assembly
            )
        {
            return GetAssemblyUri(assembly, null);
        }

        ///////////////////////////////////////////////////////////////////////

        public static Uri GetAssemblyUri(
            Assembly assembly,
            string name
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

        public static Uri GetAssemblyUpdateBaseUri(
            Assembly assembly
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

        public static Uri GetAssemblyDownloadBaseUri(
            Assembly assembly
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

        public static Uri GetAssemblyScriptBaseUri(
            Assembly assembly
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

        public static Uri GetAssemblyAuxiliaryBaseUri(
            Assembly assembly
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
        #endregion
    }
}
