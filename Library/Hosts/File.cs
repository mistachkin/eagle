/*
 * File.cs --
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
using System.Reflection;
using System.Resources;
using System.Text;
using System.Threading;
using Eagle._Attributes;
using Eagle._Components.Private;
using Eagle._Components.Public;
using Eagle._Constants;
using Eagle._Containers.Private;
using Eagle._Containers.Public;
using Eagle._Interfaces.Public;

using _File = System.IO.File;
using _Engine = Eagle._Components.Public.Engine;
using SharedStringOps = Eagle._Components.Shared.StringOps;

using PluginKeyValuePair = System.Collections.Generic.KeyValuePair<
    string, Eagle._Wrappers.Plugin>;

using ResourceManagerPair = Eagle._Interfaces.Public.IAnyPair<
    string, System.Resources.ResourceManager>;

using ResourceManagerTriplet = Eagle._Interfaces.Public.IAnyTriplet<
    Eagle._Interfaces.Public.IAnyPair<string,
        System.Resources.ResourceManager>, string, string>;

using PluginDataTriplet = Eagle._Interfaces.Public.IAnyTriplet<
    Eagle._Interfaces.Public.IPluginData, string, string>;

using AssemblyTriplet = Eagle._Interfaces.Public.IAnyTriplet<
    System.Reflection.Assembly, string, string>;

#if NET_STANDARD_21
using Index = Eagle._Constants.Index;
#endif

namespace Eagle._Hosts
{
    [ObjectId("514896d2-7003-45cf-b7fa-69fd443af625")]
    public abstract class File : Engine, IDisposable, IHaveInterpreter
    {
        #region Private Constants
        private const string DefaultLibraryResourceBaseName = "library";
        private const string DefaultPackagesResourceBaseName = "packages";
        private const string DefaultApplicationResourceBaseName = "application";

        ///////////////////////////////////////////////////////////////////////////////////////////////

        private const string NotFoundResourceName = "empty";
        #endregion

        ///////////////////////////////////////////////////////////////////////////////////////////////

        #region Private Static Data
        //
        // HACK: For performance reasons, stop trying to query the (possibly
        //       missing) application resource manager for every interpreter
        //       that is created in this AppDomain.
        //
        private static ResourceManager staticApplicationResourceManager = null;
        private static int setupStaticApplicationResourceManager = 0;
        #endregion

        ///////////////////////////////////////////////////////////////////////////////////////////////

        #region Protected Constructors
        protected File(
            IHostData hostData
            )
            : base(hostData)
        {
            if (hostData != null)
            {
                //
                // NOTE: Keep track of the interpreter that we are provided,
                //       if any.
                //
                interpreter = hostData.Interpreter;

                //
                // NOTE: Keep the resource manager provided by the custom
                //       IHost implementation, if any.
                //
                resourceManager = hostData.ResourceManager;
            }

            ///////////////////////////////////////////////////////////////////

            //
            // NOTE: Set the default resource base names.
            //
            libraryResourceBaseName = DefaultLibraryResourceBaseName;
            packagesResourceBaseName = DefaultPackagesResourceBaseName;
            applicationResourceBaseName = DefaultApplicationResourceBaseName;

            ///////////////////////////////////////////////////////////////////

            if (HasCreateFlags(HostCreateFlags.ResourceManager, true))
            {
                /* IGNORED */
                SetupLibraryResourceManager();

                /* IGNORED */
                SetupPackagesResourceManager();

                ///////////////////////////////////////////////////////////////

                //
                // HACK: The very first time (i.e. in this AppDomain), maybe
                //       attempt to actually setup the application-specific
                //       resource manager.  This could throw an exception,
                //       which is somewhat expensive; however, on subsequent
                //       creations of this class, use the cached application
                //       resource manager instance, if any.
                //
                if (HasCreateFlags(
                        HostCreateFlags.ApplicationResourceManager, true))
                {
                    if (Interlocked.Increment(
                            ref setupStaticApplicationResourceManager) == 1)
                    {
                        /* IGNORED */
                        SetupApplicationResourceManager();

                        /* IGNORED */
                        CopyFromApplicationResourceManager();
                    }
                    else
                    {
                        /* IGNORED */
                        CopyToApplicationResourceManager();
                    }
                }
            }

            ///////////////////////////////////////////////////////////////////

#if ISOLATED_PLUGINS
            /* IGNORED */
            SetupInterpreterIsolatedHost();
#endif
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////////////////////////////

        #region Interpreter Support
        protected internal Interpreter InternalSafeGetInterpreter(
            bool trace
            )
        {
            try
            {
                return Interpreter; /* throw */
            }
            catch (Exception e)
            {
                if (trace)
                {
                    TraceOps.DebugTrace(
                        e, typeof(File).Name,
                        TracePriority.HostError);
                }
            }

            return null;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

#if TEST
        internal void ResetInterpreter(
            bool trace
            )
        {
            try
            {
                Interpreter = null; /* throw */
            }
            catch (Exception e)
            {
                if (trace)
                {
                    TraceOps.DebugTrace(
                        e, typeof(File).Name,
                        TracePriority.HostError);
                }
            }
        }
#endif

        ///////////////////////////////////////////////////////////////////////////////////////////////

        protected Interpreter UnsafeGetInterpreter()
        {
            return Interpreter; /* throw */
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

#if ISOLATED_PLUGINS
        protected bool SafeIsIsolated()
        {
            try
            {
                Interpreter localInterpreter = UnsafeGetInterpreter();

                if (localInterpreter == null)
                    return false;

                return AppDomainOps.IsIsolated(localInterpreter);
            }
            catch (Exception e)
            {
                TraceOps.DebugTrace(
                    e, typeof(File).Name,
                    TracePriority.HostError);
            }

            return false;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        private bool SetupInterpreterIsolatedHost()
        {
            try
            {
                return AppDomainOps.MaybeSetIsolatedHost(
                    interpreter, this, HasCreateFlags(
                    HostCreateFlags.ResetIsolated, true));
            }
            catch (Exception e)
            {
                TraceOps.DebugTrace(
                    e, typeof(File).Name,
                    TracePriority.HostError);
            }

            return false;
        }
#endif
        #endregion

        ///////////////////////////////////////////////////////////////////////////////////////////////

        #region Plugin Support
        protected static bool SafeHasFlags(
            IPluginData pluginData,
            PluginFlags hasFlags,
            bool all
            )
        {
            if (pluginData == null)
                return false;

            try
            {
                return FlagOps.HasFlags(pluginData.Flags, hasFlags, all);
            }
            catch (Exception e)
            {
                TraceOps.DebugTrace(
                    e, typeof(File).Name,
                    TracePriority.HostError);
            }

            return false;
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////////////////////////////

        #region Host Script Support
        #region Script Flags & Package Types Support Methods
        private static void ExtractResourceNameScriptFlags(
            ScriptFlags scriptFlags,      /* in */
            out bool skipQualified,       /* out */
            out bool skipNonQualified,    /* out */
            out bool skipRelative,        /* out */
            out bool skipRawName,         /* out */
            out bool skipFileName,        /* out */
            out bool skipFileNameOnly,    /* out */
            out bool skipNonFileNameOnly, /* out */
            out bool skipLibraryToLib,    /* out */
            out bool skipTestsToLib,      /* out */
            out bool libraryPackage,      /* out */
            out bool testPackage,         /* out */
            out bool automaticPackage,    /* out */
            out bool preferDeepFileNames  /* out */
            )
        {
            skipQualified = FlagOps.HasFlags(
                scriptFlags, ScriptFlags.SkipQualified, true);

            skipNonQualified = FlagOps.HasFlags(
                scriptFlags, ScriptFlags.SkipNonQualified, true);

            skipRelative = FlagOps.HasFlags(
                scriptFlags, ScriptFlags.SkipRelative, true);

            skipRawName = FlagOps.HasFlags(
                scriptFlags, ScriptFlags.SkipRawName, true);

            skipFileName = FlagOps.HasFlags(
                scriptFlags, ScriptFlags.SkipFileName, true);

            skipFileNameOnly = FlagOps.HasFlags(
                scriptFlags, ScriptFlags.SkipFileNameOnly, true);

            skipNonFileNameOnly = FlagOps.HasFlags(
                scriptFlags, ScriptFlags.SkipNonFileNameOnly, true);

            skipLibraryToLib = FlagOps.HasFlags(
                scriptFlags, ScriptFlags.SkipLibraryToLib, true);

            skipTestsToLib = FlagOps.HasFlags(
                scriptFlags, ScriptFlags.SkipTestsToLib, true);

            libraryPackage = FlagOps.HasFlags(
                scriptFlags, ScriptFlags.LibraryPackage, true);

            testPackage = FlagOps.HasFlags(
                scriptFlags, ScriptFlags.TestPackage, true);

            automaticPackage = FlagOps.HasFlags(
                scriptFlags, ScriptFlags.AutomaticPackage, true);

            preferDeepFileNames = FlagOps.HasFlags(
                scriptFlags, ScriptFlags.PreferDeepFileNames, true);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        private static void ExtractResourceNameScriptFlags(
            ScriptFlags scriptFlags,         /* in */
            out bool filterOnSuffixMatch,    /* out */
            out bool preferDeepResourceNames /* out */
            )
        {
            filterOnSuffixMatch = FlagOps.HasFlags(
                scriptFlags, ScriptFlags.FilterOnSuffixMatch, true);

            preferDeepResourceNames = FlagOps.HasFlags(
                scriptFlags, ScriptFlags.PreferDeepResourceNames, true);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        private static void ExtractPluginScriptFlags(
            ScriptFlags scriptFlags,       /* in */
            out bool noPluginResourceName, /* out */
            out bool noRawResourceName,    /* out */
            out bool failOnException,      /* out */
            out bool stopOnException,      /* out */
            out bool failOnError,          /* out */
            out bool stopOnError,          /* out */
            out bool ignoreCanRetry        /* out */
            )
        {
            noPluginResourceName = FlagOps.HasFlags(
                scriptFlags, ScriptFlags.NoPluginResourceName, true);

            noRawResourceName = FlagOps.HasFlags(
                scriptFlags, ScriptFlags.NoRawResourceName, true);

            failOnException = FlagOps.HasFlags(
                scriptFlags, ScriptFlags.FailOnException, true);

            stopOnException = FlagOps.HasFlags(
                scriptFlags, ScriptFlags.StopOnException, true);

            failOnError = FlagOps.HasFlags(
                scriptFlags, ScriptFlags.FailOnError, true);

            stopOnError = FlagOps.HasFlags(
                scriptFlags, ScriptFlags.StopOnError, true);

            ignoreCanRetry = FlagOps.HasFlags(
                scriptFlags, ScriptFlags.IgnoreCanRetry, true);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        private static void ExtractErrorHandlingScriptFlags(
            ScriptFlags scriptFlags,  /* in */
            out bool failOnException, /* out */
            out bool stopOnException, /* out */
            out bool failOnError,     /* out */
            out bool stopOnError,     /* out */
            out bool ignoreCanRetry   /* out */
            )
        {
            failOnException = FlagOps.HasFlags(
                scriptFlags, ScriptFlags.FailOnException, true);

            stopOnException = FlagOps.HasFlags(
                scriptFlags, ScriptFlags.StopOnException, true);

            failOnError = FlagOps.HasFlags(
                scriptFlags, ScriptFlags.FailOnError, true);

            stopOnError = FlagOps.HasFlags(
                scriptFlags, ScriptFlags.StopOnError, true);

            ignoreCanRetry = FlagOps.HasFlags(
                scriptFlags, ScriptFlags.IgnoreCanRetry, true);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        private static void ExtractResourceNamePackageTypes(
            PackageType packageType,      /* in */
            out bool haveLibraryPackage,  /* out */
            out bool haveTestPackage,     /* out */
            out bool haveAutomaticPackage /* out */
            )
        {
            haveLibraryPackage = FlagOps.HasFlags(
                packageType, PackageType.Library, true);

            haveTestPackage = FlagOps.HasFlags(
                packageType, PackageType.Test, true);

            haveAutomaticPackage = FlagOps.HasFlags(
                packageType, PackageType.Automatic, true);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        private static void ScriptFlagsToFileSearchFlags(
            ScriptFlags scriptFlags,            /* in */
            out FileSearchFlags fileSearchFlags /* out */
            )
        {
            fileSearchFlags = FileSearchFlags.Default;

            if (FlagOps.HasFlags(
                    scriptFlags, ScriptFlags.SpecificPath, true))
            {
                fileSearchFlags |= FileSearchFlags.SpecificPath;
            }

            if (FlagOps.HasFlags(
                    scriptFlags, ScriptFlags.Mapped, true))
            {
                fileSearchFlags |= FileSearchFlags.Mapped;
            }

            if (FlagOps.HasFlags(
                    scriptFlags, ScriptFlags.AutoSourcePath, true))
            {
                fileSearchFlags |= FileSearchFlags.AutoSourcePath;
            }

            if (FlagOps.HasFlags(
                    scriptFlags, ScriptFlags.User, true))
            {
                fileSearchFlags |= FileSearchFlags.User;
            }

            if (FlagOps.HasFlags(
                    scriptFlags, ScriptFlags.Application, true))
            {
                fileSearchFlags |= FileSearchFlags.Application;
            }

            if (FlagOps.HasFlags(
                    scriptFlags, ScriptFlags.Vendor, true))
            {
                fileSearchFlags |= FileSearchFlags.Vendor;
            }

            if (FlagOps.HasFlags(
                    scriptFlags, ScriptFlags.StrictGetFile, true))
            {
                fileSearchFlags |= FileSearchFlags.Strict;
            }

            if (FlagOps.HasFlags(
                    scriptFlags, ScriptFlags.SearchDirectory, true))
            {
                fileSearchFlags |= FileSearchFlags.DirectoryLocation;
            }

            if (FlagOps.HasFlags(
                    scriptFlags, ScriptFlags.SearchFile, true))
            {
                fileSearchFlags |= FileSearchFlags.FileLocation;
            }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////////////////////////////

        #region AnyTriplet Static "Factory" Methods
        private static PluginDataTriplet NewPluginDataTriplet(
            IPlugin plugin,      /* in */
            string methodName,   /* in */
            string resourceName, /* in */
            bool isolated        /* in: NOT USED */
            )
        {
            return new AnyTriplet<IPluginData, string, string>(
                plugin, methodName, resourceName);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        private static ResourceManagerTriplet NewResourceManagerTriplet(
            ResourceManagerPair resourceManagerAnyPair, /* in */
            string methodName,                          /* in */
            string resourceName,                        /* in */
            bool isolated                               /* in */
            )
        {
            return new AnyTriplet<ResourceManagerPair, string, string>(
#if ISOLATED_PLUGINS
                !isolated ? resourceManagerAnyPair :
                    new AnyPair<string, ResourceManager>(
                        (resourceManagerAnyPair != null) ?
                            resourceManagerAnyPair.X : null, null),
#else
                        resourceManagerAnyPair,
#endif
                methodName, resourceName);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        private static AssemblyTriplet NewAssemblyTriplet(
            Assembly assembly,   /* in */
            string methodName,   /* in */
            string resourceName, /* in */
            bool isolated        /* in */
            )
        {
            return new AnyTriplet<Assembly, string, string>(
#if ISOLATED_PLUGINS
                !isolated ? assembly : null,
#else
                assembly,
#endif
                methodName, resourceName);
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////////////////////////////

        #region Tracing Support Methods
        protected virtual TracePriority GetDataTracePriority(
            ScriptFlags scriptFlags, /* in */
            ReturnCode returnCode    /* in */
            )
        {
            bool isRequired = FlagOps.HasFlags(
                scriptFlags, ScriptFlags.Required, true);

            if (returnCode == ReturnCode.Ok)
            {
                return isRequired ?
                    TracePriority.GetDataDebug :
                    TracePriority.GetDataDebug2;
            }
            else
            {
                return isRequired ?
                    TracePriority.GetDataError :
                    TracePriority.GetDataError2;
            }
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        protected virtual void GetDataTrace(
            Interpreter interpreter, /* in */
            string prefix,           /* in */
            string name,             /* in */
            DataFlags dataFlags,     /* in */
            ScriptFlags scriptFlags, /* in */
            IClientData clientData,  /* in */
            ReturnCode returnCode,   /* in */
            Result result            /* in */
            )
        {
            if (!FlagOps.HasFlags(scriptFlags, ScriptFlags.NoTrace, true))
            {
                TracePriority priority = GetDataTracePriority(
                    scriptFlags, returnCode);

                TraceOps.DebugTrace(interpreter, String.Format(
                    "GetData: {0}, interpreter = {1}, name = {2}, " +
                    "dataFlags = {3}, scriptFlags = {4}, " +
                    "clientData = {5}, returnCode = {6}, result = {7}",
                    prefix, FormatOps.InterpreterNoThrow(interpreter),
                    FormatOps.WrapOrNull(name), FormatOps.WrapOrNull(
                    dataFlags), FormatOps.WrapOrNull(scriptFlags),
                    FormatOps.WrapOrNull(clientData), FormatOps.WrapOrNull(
                    returnCode), FormatOps.WrapOrNull(true, true, result)),
                    typeof(File).Name, priority, 1);
            }
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        protected virtual void FilterScriptResourceNamesTrace(
            Interpreter interpreter,           /* in */
            string name,                       /* in */
            IEnumerable<string> resourceNames, /* in */
            DataFlags dataFlags,               /* in */
            ScriptFlags scriptFlags,           /* in */
            string message                     /* in */
            )
        {
            if (!FlagOps.HasFlags(scriptFlags, ScriptFlags.NoTrace, true))
            {
                TracePriority priority = GetDataTracePriority(
                    scriptFlags, ReturnCode.Ok);

                StringList list = (resourceNames != null) ?
                    new StringList(resourceNames) : null;

                TraceOps.DebugTrace(interpreter, String.Format(
                    "FilterScriptResourceNames: interpreter = {0}, " +
                    "name = {1}, resourceNames = {2}, dataFlags = {3}, " +
                    "scriptFlags = {4}, {5}", FormatOps.InterpreterNoThrow(
                    interpreter), FormatOps.WrapOrNull(name),
                    FormatOps.WrapOrNull(list), FormatOps.WrapOrNull(
                    dataFlags), FormatOps.WrapOrNull(scriptFlags),
                    (message != null) ? message : FormatOps.DisplayNull),
                    typeof(File).Name, priority, 1);
            }
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        protected virtual void GetUniqueResourceNamesTrace(
            Interpreter interpreter,              /* in */
            string name,                          /* in */
            IEnumerable<string> resourceNames,    /* in */
            StringDictionary uniqueResourceNames, /* in */
            DataFlags dataFlags,                  /* in */
            ScriptFlags scriptFlags               /* in */
            )
        {
            if (!FlagOps.HasFlags(scriptFlags, ScriptFlags.NoTrace, true))
            {
                TracePriority priority = GetDataTracePriority(
                    scriptFlags, ReturnCode.Ok);

                StringList list = (resourceNames != null) ?
                    new StringList(resourceNames) : null;

                int[] counts = {
                    Count.Invalid, Count.Invalid, Count.Invalid
                };

                if (list != null)
                    counts[0] = list.Count;

                if (uniqueResourceNames != null)
                    counts[1] = uniqueResourceNames.Count;

                if ((counts[0] != Count.Invalid) &&
                    (counts[1] != Count.Invalid))
                {
                    counts[2] = counts[0] - counts[1];
                }

                TraceOps.DebugTrace(interpreter, String.Format(
                    "GetUniqueResourceNames: interpreter = {0}, " +
                    "name = {1}, resourceNames = {2}, " +
                    "uniqueResourceNames = {3}, dataFlags = {4}, " +
                    "scriptFlags = {5}, had {6} names, have {7} " +
                    "names, removed {8} names",
                    FormatOps.InterpreterNoThrow(interpreter),
                    FormatOps.WrapOrNull(name),
                    FormatOps.WrapOrNull(list),
                    FormatOps.WrapOrNull(uniqueResourceNames),
                    FormatOps.WrapOrNull(dataFlags),
                    FormatOps.WrapOrNull(scriptFlags),
                    counts[0], counts[1], counts[2]),
                    typeof(File).Name, priority, 1);
            }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////////////////////////////

        #region Reserved Names Support Methods
        protected virtual IDictionary<string, string> GetReservedDataNames()
        {
            //
            // NOTE: This data comes from the base class (i.e. the "Default"
            //       host).
            //
            return wellKnownDataNames;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        //
        // NOTE: This method cannot fail.  Returning false simply means that
        //       the specified script does not contain a "reserved" name.
        //
        protected virtual bool IsReservedDataName(
            Interpreter interpreter, /* in: NOT USED */
            string name,             /* in */
            DataFlags dataFlags,     /* in */
            ScriptFlags scriptFlags, /* in: NOT USED */
            IClientData clientData   /* in: NOT USED */
            )
        {
            if (name == null)
                return false;

            IDictionary<string, string> dictionary = GetReservedDataNames();

            if (dictionary == null)
                return false;

            return dictionary.ContainsKey(name);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        //
        // NOTE: This method cannot fail.  Returning false simply means that
        //       the specified [file] name contains directory information as
        //       well.
        //
        protected virtual bool IsFileNameOnlyDataName(
            string name /* in */
            )
        {
            return !PathOps.HasDirectory(name);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        //
        // NOTE: This method cannot fail.  Returning false simply means that
        //       the specified [file] name does not contain an absolute path.
        //
        protected virtual bool IsAbsoluteFileNameDataName(
            string name,    /* in */
            ref bool exists /* out */
            )
        {
            try
            {
                exists = _File.Exists(name); /* throw */

                if (Path.IsPathRooted(name)) /* throw */
                    return true;
            }
            catch (Exception e)
            {
                TraceOps.DebugTrace(
                    e, typeof(File).Name,
                    TracePriority.HostError);
            }

            return false;
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////////////////////////////

        #region Parameter Customization Support Methods
        //
        // NOTE: If this method returns false in a derived class, it must set
        //       the error message as well.
        //
        protected virtual bool CheckDataParameters(
            Interpreter interpreter,     /* in: NOT USED */
            ref string name,             /* in, out: NOT USED */
            ref DataFlags dataFlags,     /* in, out: NOT USED */
            ref ScriptFlags scriptFlags, /* in, out */
            ref IClientData clientData,  /* in, out: NOT USED */
            ref Result error             /* out */
            )
        {
            try
            {
                ScriptFlags newScriptFlags = LibraryScriptFlags;

                if (newScriptFlags != ScriptFlags.None)
                    scriptFlags |= newScriptFlags;

                return true;
            }
            catch (Exception e)
            {
                error = e;
            }

            return false;
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////////////////////////////

        #region Allow / Deny Support Methods
        //
        // NOTE: If this method returns false in a derived class, it must set
        //       the error message as well.
        //
        protected virtual bool ShouldAllowDataParameters(
            Interpreter interpreter,     /* in: NOT USED */
            ref string name,             /* in, out: NOT USED */
            ref DataFlags dataFlags,     /* in, out: NOT USED */
            ref ScriptFlags scriptFlags, /* in, out: NOT USED */
            ref IClientData clientData,  /* in, out: NOT USED */
            ref Result error             /* out: NOT USED */
            )
        {
            return true; /* STUB */
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////////////////////////////

        #region Resource Name Support Methods
        protected virtual PackageType GetPackageTypeForResourceName(
            Interpreter interpreter, /* in: NOT USED */
            string name,             /* in */
            DataFlags dataFlags,     /* in: NOT USED */
            ScriptFlags scriptFlags, /* in: NOT USED */
            PackageType packageType  /* in */
            )
        {
            packageType &= ~PackageType.Mask;

            if (name != null)
            {
                string unixName = PathOps.GetUnixPath(name);

                if (unixName.IndexOf(ScriptPaths.LibraryPackage,
                        SharedStringOps.SystemComparisonType) != Index.Invalid)
                {
                    packageType |= PackageType.Library;
                }

                if (unixName.IndexOf(ScriptPaths.TestPackage,
                        SharedStringOps.SystemComparisonType) != Index.Invalid)
                {
                    packageType |= PackageType.Test;
                }
            }

            return packageType;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        protected virtual IEnumerable<string> GetDataResourceNames(
            Interpreter interpreter, /* in */
            string name,             /* in */
            DataFlags dataFlags,     /* in: NOT USED */
            ScriptFlags scriptFlags, /* in */
            bool verbose             /* in */
            )
        {
            //
            // NOTE: Does the caller wish to skip treating the name as the raw
            //       resource name and/or a file name?  Also, does the caller
            //       wish to skip the qualified and/or non-qualified name?
            //
            bool skipQualified;
            bool skipNonQualified;
            bool skipRelative;
            bool skipRawName;
            bool skipFileName;
            bool skipFileNameOnly;
            bool skipNonFileNameOnly;
            bool skipLibraryToLib;
            bool skipTestsToLib;
            bool libraryPackage;
            bool testPackage;
            bool automaticPackage;
            bool preferDeepFileNames;

            ExtractResourceNameScriptFlags(
                scriptFlags, out skipQualified, out skipNonQualified,
                out skipRelative, out skipRawName, out skipFileName,
                out skipFileNameOnly, out skipNonFileNameOnly,
                out skipLibraryToLib, out skipTestsToLib,
                out libraryPackage, out testPackage,
                out automaticPackage, out preferDeepFileNames);

            PackageType packageType = PackageType.None;

            if (libraryPackage)
                packageType |= PackageType.Library;

            if (testPackage)
                packageType |= PackageType.Test;

            if (automaticPackage)
                packageType |= PackageType.Automatic;

            packageType = GetPackageTypeForResourceName(interpreter,
                name, dataFlags, scriptFlags, packageType);

            bool haveLibraryPackage;
            bool haveTestPackage;
            bool haveAutomaticPackage;

            ExtractResourceNamePackageTypes(
                packageType, out haveLibraryPackage, out haveTestPackage,
                out haveAutomaticPackage);

            string[] fileNames = {
                null, null, null, null, null, null, null, null
            };

            if ((name != null) &&
                (!skipQualified || !skipRelative) && !skipFileName)
            {
                if (!skipNonFileNameOnly)
                {
                    if (haveLibraryPackage || haveAutomaticPackage)
                    {
                        fileNames[0] = FormatOps.ScriptTypeToFileName(
                            name, PackageType.Library, false, true);

                        if (!skipLibraryToLib || !skipTestsToLib)
                        {
                            fileNames[1] = PathOps.MaybeToLib(
                                fileNames[0], skipLibraryToLib,
                                skipTestsToLib, false);
                        }
                    }

                    if (haveTestPackage || haveAutomaticPackage)
                    {
                        fileNames[2] = FormatOps.ScriptTypeToFileName(
                            name, PackageType.Test, false, true);

                        if (!skipLibraryToLib || !skipTestsToLib)
                        {
                            fileNames[3] = PathOps.MaybeToLib(
                                fileNames[2], skipLibraryToLib,
                                skipTestsToLib, false);
                        }
                    }
                }

                if (!skipFileNameOnly)
                {
                    if (haveLibraryPackage || haveAutomaticPackage)
                    {
                        fileNames[4] = FormatOps.ScriptTypeToFileName(
                            name, PackageType.Library, true, true);

                        if (!skipLibraryToLib || !skipTestsToLib)
                        {
                            fileNames[5] = PathOps.MaybeToLib(
                                fileNames[4], skipLibraryToLib,
                                skipTestsToLib, false);
                        }
                    }

                    if (haveTestPackage || haveAutomaticPackage)
                    {
                        fileNames[6] = FormatOps.ScriptTypeToFileName(
                            name, PackageType.Test, true, true);

                        if (!skipLibraryToLib || !skipTestsToLib)
                        {
                            fileNames[7] = PathOps.MaybeToLib(
                                fileNames[6], skipLibraryToLib,
                                skipTestsToLib, false);
                        }
                    }
                }
            }

            string baseName = null;

            if ((!skipRawName || !skipFileName) && !skipNonQualified)
                baseName = (name != null) ? Path.GetFileName(name) : null;

            string[] baseFileNames = {
                null, null, null, null, null, null, null, null
            };

            if ((baseName != null) && !skipNonQualified && !skipFileName)
            {
                if (!skipNonFileNameOnly)
                {
                    if (haveLibraryPackage || haveAutomaticPackage)
                    {
                        baseFileNames[0] = FormatOps.ScriptTypeToFileName(
                            baseName, PackageType.Library, false, true);

                        if (!skipLibraryToLib || !skipTestsToLib)
                        {
                            baseFileNames[1] = PathOps.MaybeToLib(
                                baseFileNames[0], skipLibraryToLib,
                                skipTestsToLib, false);
                        }
                    }

                    if (haveTestPackage || haveAutomaticPackage)
                    {
                        baseFileNames[2] = FormatOps.ScriptTypeToFileName(
                            baseName, PackageType.Test, false, true);

                        if (!skipLibraryToLib || !skipTestsToLib)
                        {
                            baseFileNames[3] = PathOps.MaybeToLib(
                                baseFileNames[2], skipLibraryToLib,
                                skipTestsToLib, false);
                        }
                    }
                }

                if (!skipFileNameOnly)
                {
                    if (haveLibraryPackage || haveAutomaticPackage)
                    {
                        baseFileNames[4] = FormatOps.ScriptTypeToFileName(
                            baseName, PackageType.Library, true, true);

                        if (!skipLibraryToLib || !skipTestsToLib)
                        {
                            baseFileNames[5] = PathOps.MaybeToLib(
                                baseFileNames[4], skipLibraryToLib,
                                skipTestsToLib, false);
                        }
                    }

                    if (haveTestPackage || haveAutomaticPackage)
                    {
                        baseFileNames[6] = FormatOps.ScriptTypeToFileName(
                            baseName, PackageType.Test, true, true);

                        if (!skipLibraryToLib || !skipTestsToLib)
                        {
                            baseFileNames[7] = PathOps.MaybeToLib(
                                baseFileNames[6], skipLibraryToLib,
                                skipTestsToLib, false);
                        }
                    }
                }
            }

            PathComparisonType pathComparisonType = preferDeepFileNames ?
                PathComparisonType.DeepestFirst : PathComparisonType.Default;

            //
            // NOTE: Try the following ways to get the script via an embedded
            //       resource name, in order:
            //
            //       1. The provided name verbatim as a resource name, with and
            //          without a file extension.
            //
            //       2. Repeat step #1, treating the provided name as a fully
            //          qualified file name to be converted into a package
            //          relative file name, with and without a file extension.
            //
            //       3. Repeat step #1, treating the provided name as a fully
            //          qualified file name to be converted into a relative
            //          file name, with and without a file extension.
            //
            //       4. There is no step #4.
            //
            return new string[] {
                ///////////////////////////////////////////////////////////////
                // STEP #1
                ///////////////////////////////////////////////////////////////

                !skipQualified && !skipRawName ? name : null,
                !skipQualified && !skipRawName &&
                    (!skipLibraryToLib || !skipTestsToLib) ?
                        PathOps.MaybeToLib(name, skipLibraryToLib,
                            skipTestsToLib, false) : null,
                !skipQualified && !skipFileName && !skipNonFileNameOnly ?
                    fileNames[0] : null,
                !skipQualified && !skipFileName && !skipNonFileNameOnly &&
                    !skipLibraryToLib ? fileNames[1] : null,
                !skipQualified && !skipFileName && !skipNonFileNameOnly ?
                    fileNames[2] : null,
                !skipQualified && !skipFileName && !skipNonFileNameOnly &&
                    !skipLibraryToLib ? fileNames[3] : null,
                !skipQualified && !skipFileName && !skipFileNameOnly ?
                    fileNames[4] : null,
                !skipQualified && !skipFileName && !skipFileNameOnly &&
                    !skipLibraryToLib ? fileNames[5] : null,
                !skipQualified && !skipFileName && !skipFileNameOnly ?
                    fileNames[6] : null,
                !skipQualified && !skipFileName && !skipFileNameOnly &&
                    !skipLibraryToLib ? fileNames[7] : null,

                ///////////////////////////////////////////////////////////////
                // STEP #2
                ///////////////////////////////////////////////////////////////

                !skipRelative && !skipRawName ?
                    PackageOps.GetRelativeFileName(interpreter,
                        name, pathComparisonType, verbose) : null,
                !skipRelative && !skipRawName &&
                    (!skipLibraryToLib || !skipTestsToLib) ?
                        PathOps.MaybeToLib(name, skipLibraryToLib,
                            skipTestsToLib, true) : null,
                !skipRelative && !skipFileName && !skipNonFileNameOnly ?
                    PackageOps.GetRelativeFileName(interpreter,
                        fileNames[0], pathComparisonType, verbose) : null,
                !skipRelative && !skipFileName && !skipNonFileNameOnly &&
                    (!skipLibraryToLib || !skipTestsToLib) ?
                        PathOps.MaybeToLib(fileNames[0], skipLibraryToLib,
                            skipTestsToLib, true) : null,
                !skipRelative && !skipFileName && !skipNonFileNameOnly ?
                    PackageOps.GetRelativeFileName(interpreter,
                        fileNames[1], pathComparisonType, verbose) : null,
                !skipRelative && !skipFileName && !skipNonFileNameOnly &&
                    (!skipLibraryToLib || !skipTestsToLib) ?
                        PathOps.MaybeToLib(fileNames[1], skipLibraryToLib,
                            skipTestsToLib, true) : null,
                !skipRelative && !skipFileName && !skipFileNameOnly ?
                    PackageOps.GetRelativeFileName(interpreter,
                        fileNames[2], pathComparisonType, verbose) : null,
                !skipRelative && !skipFileName && !skipFileNameOnly &&
                    (!skipLibraryToLib || !skipTestsToLib) ?
                        PathOps.MaybeToLib(fileNames[2], skipLibraryToLib,
                            skipTestsToLib, true) : null,
                !skipRelative && !skipFileName && !skipFileNameOnly ?
                    PackageOps.GetRelativeFileName(interpreter,
                        fileNames[3], pathComparisonType, verbose) : null,
                !skipRelative && !skipFileName && !skipFileNameOnly &&
                    (!skipLibraryToLib || !skipTestsToLib) ?
                        PathOps.MaybeToLib(fileNames[3], skipLibraryToLib,
                            skipTestsToLib, true) : null,

                ///////////////////////////////////////////////////////////////
                // STEP #3
                ///////////////////////////////////////////////////////////////

                !skipNonQualified && !skipRawName ? baseName : null,
                !skipNonQualified && !skipRawName &&
                    (!skipLibraryToLib || !skipTestsToLib) ?
                        PathOps.MaybeToLib(baseName, skipLibraryToLib,
                            skipTestsToLib, false) : null,
                !skipNonQualified && !skipFileName && !skipNonFileNameOnly ?
                    baseFileNames[0] : null,
                !skipNonQualified && !skipFileName && !skipNonFileNameOnly &&
                    !skipLibraryToLib ? baseFileNames[1] : null,
                !skipNonQualified && !skipFileName && !skipNonFileNameOnly ?
                    baseFileNames[2] : null,
                !skipNonQualified && !skipFileName && !skipNonFileNameOnly &&
                    !skipLibraryToLib ? baseFileNames[3] : null,
                !skipNonQualified && !skipFileName && !skipFileNameOnly ?
                    baseFileNames[4] : null,
                !skipNonQualified && !skipFileName && !skipFileNameOnly &&
                    !skipLibraryToLib ? baseFileNames[5] : null,
                !skipNonQualified && !skipFileName && !skipFileNameOnly ?
                    baseFileNames[6] : null,
                !skipNonQualified && !skipFileName && !skipFileNameOnly &&
                    !skipLibraryToLib ? baseFileNames[7] : null
            };
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        protected virtual IEnumerable<string> FilterScriptResourceNames(
            Interpreter interpreter,           /* in */
            string name,                       /* in */
            IEnumerable<string> resourceNames, /* in */
            DataFlags dataFlags,               /* in */
            ScriptFlags scriptFlags,           /* in */
            bool verbose                       /* in */
            )
        {
            if (resourceNames != null)
            {
                bool filterOnSuffixMatch;
                bool preferDeepResourceNames;

                ExtractResourceNameScriptFlags(scriptFlags,
                    out filterOnSuffixMatch, out preferDeepResourceNames);

                if (filterOnSuffixMatch || preferDeepResourceNames)
                {
                    if (verbose)
                    {
                        FilterScriptResourceNamesTrace(
                            interpreter, name, resourceNames, dataFlags,
                            scriptFlags, "original");
                    }

                    StringList newResourceNames = new StringList();
                    StringBuilder builder = null;

                    if (filterOnSuffixMatch)
                    {
                        StringOps.AppendWithComma("filtered", ref builder);

                        foreach (string resourceName in resourceNames)
                        {
                            if (resourceName == null)
                            {
                                if (verbose)
                                {
                                    FilterScriptResourceNamesTrace(
                                        interpreter, name, null,
                                        dataFlags, scriptFlags,
                                        "skipped null resource name");
                                }

                                continue;
                            }

                            if (PathOps.MatchSuffix(name, resourceName))
                            {
                                newResourceNames.Add(resourceName);

                                if (verbose)
                                {
                                    FilterScriptResourceNamesTrace(
                                        interpreter, name, null,
                                        dataFlags, scriptFlags,
                                        String.Format(
                                            "added resource name {0}, " +
                                            "matched suffix {1}",
                                        FormatOps.WrapOrNull(resourceName),
                                        FormatOps.WrapOrNull(name)));
                                }
                            }
                            else
                            {
                                if (verbose)
                                {
                                    FilterScriptResourceNamesTrace(
                                        interpreter, name, null,
                                        dataFlags, scriptFlags,
                                        String.Format(
                                            "skipped resource name {0}, " +
                                            "mismatched suffix {1}",
                                        FormatOps.WrapOrNull(resourceName),
                                        FormatOps.WrapOrNull(name)));
                                }
                            }
                        }
                    }
                    else
                    {
                        newResourceNames.AddRange(resourceNames);

                        if (verbose)
                        {
                            FilterScriptResourceNamesTrace(
                                interpreter, name, null,
                                dataFlags, scriptFlags,
                                "added resource names verbatim");
                        }
                    }

                    if (preferDeepResourceNames)
                    {
                        StringOps.AppendWithComma("sorted", ref builder);

                        newResourceNames.Sort(_Comparers.FileName.Create(
                            PathComparisonType.DeepestFirst));
                    }

                    FilterScriptResourceNamesTrace(
                        interpreter, name, newResourceNames,
                        dataFlags, scriptFlags, (builder != null) ?
                            builder.ToString() : null);

                    return newResourceNames.ToArray();
                }
            }

            FilterScriptResourceNamesTrace(
                interpreter, name, resourceNames, dataFlags,
                scriptFlags, "verbatim");

            return resourceNames;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        protected virtual StringDictionary GetUniqueResourceNames(
            Interpreter interpreter,           /* in */
            string name,                       /* in */
            IEnumerable<string> resourceNames, /* in */
            DataFlags dataFlags,               /* in */
            ScriptFlags scriptFlags,           /* in */
            bool verbose                       /* in */
            )
        {
            //
            // NOTE: Create a string dictionary with the resource names so
            //       that we do not search needlessly for duplicates.
            //
            StringDictionary uniqueResourceNames = new StringDictionary();

            if (resourceNames != null)
            {
                foreach (string resourceName in resourceNames)
                {
                    if (resourceName == null)
                        continue;

                    if (!uniqueResourceNames.ContainsKey(resourceName))
                        uniqueResourceNames.Add(resourceName, null);
                }
            }

            GetUniqueResourceNamesTrace(
                interpreter, name, resourceNames, uniqueResourceNames,
                dataFlags, scriptFlags);

            return uniqueResourceNames;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        protected virtual void PopulateUniqueResourceNames(
            Interpreter interpreter,                 /* in */
            string name,                             /* in */
            DataFlags dataFlags,                     /* in */
            ScriptFlags scriptFlags,                 /* in */
            bool verbose,                            /* in */
            ref StringDictionary uniqueResourceNames /* out */
            )
        {
            IEnumerable<string> resourceNames;

            resourceNames = GetDataResourceNames(
                interpreter, name, dataFlags, scriptFlags,
                verbose);

            resourceNames = FilterScriptResourceNames(
                interpreter, name, resourceNames, dataFlags,
                scriptFlags, verbose);

            uniqueResourceNames = GetUniqueResourceNames(
                interpreter, name, resourceNames, dataFlags,
                scriptFlags, verbose);
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////////////////////////////

        #region File System Support Methods
        protected virtual ReturnCode GetDataViaFileSystem(
            Interpreter interpreter,     /* in */
            string name,                 /* in */
            DataFlags dataFlags,         /* in */
            int[] counts,                /* in, out */
            bool verbose,                /* in */
            bool isolated,               /* in: NOT USED */
            ref ScriptFlags scriptFlags, /* in, out */
            ref IClientData clientData,  /* out: NOT USED */
            ref Result result,           /* out */
            ref ResultList errors        /* in, out: NOT USED */
            )
        {
            FileSearchFlags fileSearchFlags;

            ScriptFlagsToFileSearchFlags(scriptFlags, out fileSearchFlags);

            //
            // BUGFIX: *HACK* Do not permit just the tail portion of the
            //         file name to be used during the search for script
            //         files that are evaluated pursuant to a [package]
            //         command.
            //
            if (interpreter != null)
            {
                int levels = interpreter.EnterPackageLevel();

                try
                {
                    if (levels > 1)
                        fileSearchFlags &= ~FileSearchFlags.TailOnly;
                }
                finally
                {
                    interpreter.ExitPackageLevel();
                }
            }

            if (verbose)
                fileSearchFlags |= FileSearchFlags.Verbose;

            if (isolated)
                fileSearchFlags |= FileSearchFlags.Isolated;

            if ((counts != null) && (counts.Length > 0))
                counts[0]++;

            int count = 0;

            string value = PathOps.Search(
                interpreter, name, fileSearchFlags, ref count);

            if ((counts != null) && (counts.Length > 0))
            {
                counts[0]--; /* UNDO */
                counts[0] += count;
            }

            if ((counts != null) && (counts.Length > 1))
                counts[1] += count;

            if (value != null)
            {
                scriptFlags |= ScriptFlags.File;
                result = value;

                return ReturnCode.Ok;
            }

            return ReturnCode.Continue;
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////////////////////////////

        #region Plugin Support Methods
        //
        // WARNING: The "internal" use is designed for
        //          the HostOps.GetScript method only.
        //
        protected internal virtual EngineFlags GetEngineFlagsForReadScriptStream(
            Interpreter interpreter, /* in */
            DataFlags dataFlags,     /* in: NOT USED */
            ScriptFlags scriptFlags  /* in */
            )
        {
            //
            // NOTE: Grab the engine flags as we need them for the calls into
            //       the engine.
            //
            EngineFlags engineFlags = EngineFlags.None;

            if (interpreter != null)
                engineFlags |= interpreter.EngineFlags;

#if XML
            if (FlagOps.HasFlags(scriptFlags, ScriptFlags.NoXml, true))
                engineFlags |= EngineFlags.NoXml;
#endif

            if (FlagOps.HasFlags(scriptFlags, ScriptFlags.NoPolicy, true))
                engineFlags |= EngineFlags.NoPolicy;

            return engineFlags;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        protected virtual ReturnCode GetDataViaPlugin(
            Interpreter interpreter,              /* in */
            string name,                          /* in */
            IPlugin plugin,                       /* in */
            StringDictionary uniqueResourceNames, /* in */
            CultureInfo cultureInfo,              /* in */
            EngineFlags engineFlags,              /* in */
            DataFlags dataFlags,                  /* in */
            bool verbose,                         /* in */
            bool isolated,                        /* in */
            int[] counts,                         /* in, out */
            ref ScriptFlags scriptFlags,          /* in, out */
            ref IClientData clientData,           /* out */
            ref Result result,                    /* out */
            ref ResultList errors                 /* in, out */
            )
        {
            //
            // HACK: Skip all invalid and static system (i.e. "core") plugins.
            //       Also, skip doing anything if the data type is unsupported.
            //
            if ((plugin == null) ||
                SafeHasFlags(plugin, PluginFlags.System, true) ||
                (!FlagOps.HasFlags(dataFlags, DataFlags.Bytes, true) &&
                !FlagOps.HasFlags(dataFlags, DataFlags.Text, true)))
            {
#if TEST
                //
                // HACK: Always allow the test plugin when compiled with the
                //       "TEST" compile-time option enabled.
                //
                if (!SafeHasFlags(plugin, PluginFlags.Test, true))
                    return ReturnCode.Continue;
#else
                return ReturnCode.Continue;
#endif
            }

            if (uniqueResourceNames == null)
                return ReturnCode.Continue;

            bool noPluginResourceName;
            bool noRawResourceName;
            bool failOnException;
            bool stopOnException;
            bool failOnError;
            bool stopOnError;
            bool ignoreCanRetry;

            ExtractPluginScriptFlags(scriptFlags,
                out noPluginResourceName, out noRawResourceName,
                out failOnException, out stopOnException,
                out failOnError, out stopOnError, out ignoreCanRetry);

            string pluginName = FormatOps.PluginSimpleName(plugin);

            if ((noPluginResourceName || (pluginName == null)) &&
                noRawResourceName)
            {
                //
                // NOTE: The loop below would do nothing, just skip it and
                //       return now.
                //
                return ReturnCode.Continue;
            }

            if (FlagOps.HasFlags(dataFlags, DataFlags.Bytes, true))
            {
                if (FlagOps.HasFlags(dataFlags, DataFlags.NoStream, true) ||
                    FlagOps.HasFlags(dataFlags, DataFlags.NoPluginStream, true))
                {
                    return ReturnCode.Continue;
                }

                if (SafeHasFlags(plugin, PluginFlags.NoGetStream, true))
                    return ReturnCode.Continue;

                foreach (string uniqueResourceName in uniqueResourceNames.Keys)
                {
                    Stream resourceStream = null;

                    if (!noPluginResourceName && (pluginName != null))
                    {
                        string pluginUniqueResourceName =
                            pluginName + Characters.Period + uniqueResourceName;

                        try
                        {
                            if ((counts != null) && (counts.Length > 2))
                                counts[2]++;

                            Result error = null;

                            resourceStream = plugin.GetStream(
                                interpreter, pluginUniqueResourceName,
                                cultureInfo, ref error); /* throw */

                            if ((counts != null) && (counts.Length > 3))
                                counts[3]++;

                            if (resourceStream == null)
                            {
                                if (verbose && (error != null))
                                {
                                    if (errors == null)
                                        errors = new ResultList();

                                    /* VERBOSE */
                                    errors.Add(error);
                                }
                            }
                        }
                        catch (Exception e)
                        {
                            if (verbose)
                            {
                                if (errors == null)
                                    errors = new ResultList();

                                /* VERBOSE */
                                errors.Add(e);
                            }

                            if (failOnException)
                                return ReturnCode.Error;
                            else if (stopOnException)
                                break;
                        }

                        if (resourceStream != null)
                        {
                            using (BinaryReader binaryReader =
                                    new BinaryReader(resourceStream)) /* throw */
                            {
                                try
                                {
                                    byte[] bytes = binaryReader.ReadBytes(
                                        (int)resourceStream.Length); /* throw */

                                    PluginDataTriplet anyTriplet = NewPluginDataTriplet(
                                        plugin, "GetStream", pluginUniqueResourceName,
                                        isolated);

                                    scriptFlags |= ScriptFlags.ClientData;
                                    clientData = new ClientData(anyTriplet);
                                    result = bytes;

                                    return ReturnCode.Ok;
                                }
                                catch (Exception e)
                                {
                                    if (verbose)
                                    {
                                        if (errors == null)
                                            errors = new ResultList();

                                        /* VERBOSE */
                                        errors.Add(e);
                                    }

                                    if (failOnException)
                                        return ReturnCode.Error;
                                    else if (stopOnException)
                                        break;
                                }
                            }
                        }
                    }

                    if (!noRawResourceName)
                    {
                        try
                        {
                            if ((counts != null) && (counts.Length > 2))
                                counts[2]++;

                            Result error = null;

                            resourceStream = plugin.GetStream(
                                interpreter, uniqueResourceName,
                                cultureInfo, ref error); /* throw */

                            if ((counts != null) && (counts.Length > 3))
                                counts[3]++;

                            if (resourceStream == null)
                            {
                                if (verbose && (error != null))
                                {
                                    if (errors == null)
                                        errors = new ResultList();

                                    /* VERBOSE */
                                    errors.Add(error);
                                }
                            }
                        }
                        catch (Exception e)
                        {
                            if (verbose)
                            {
                                if (errors == null)
                                    errors = new ResultList();

                                /* VERBOSE */
                                errors.Add(e);
                            }

                            if (failOnException)
                                return ReturnCode.Error;
                            else if (stopOnException)
                                break;
                        }
                    }

                    if (resourceStream != null)
                    {
                        using (BinaryReader binaryReader =
                                new BinaryReader(resourceStream)) /* throw */
                        {
                            try
                            {
                                byte[] bytes = binaryReader.ReadBytes(
                                    (int)resourceStream.Length); /* throw */

                                PluginDataTriplet anyTriplet = NewPluginDataTriplet(
                                    plugin, "GetStream", uniqueResourceName,
                                    isolated);

                                scriptFlags |= ScriptFlags.ClientData;
                                clientData = new ClientData(anyTriplet);
                                result = bytes;

                                return ReturnCode.Ok;
                            }
                            catch (Exception e)
                            {
                                if (verbose)
                                {
                                    if (errors == null)
                                        errors = new ResultList();

                                    /* VERBOSE */
                                    errors.Add(e);
                                }

                                if (failOnException)
                                    return ReturnCode.Error;
                                else if (stopOnException)
                                    break;
                            }
                        }
                    }
                }
            }
            else if (FlagOps.HasFlags(dataFlags, DataFlags.Text, true))
            {
                if (FlagOps.HasFlags(dataFlags, DataFlags.NoString, true) ||
                    FlagOps.HasFlags(dataFlags, DataFlags.NoPluginString, true))
                {
                    return ReturnCode.Continue;
                }

                if (SafeHasFlags(plugin, PluginFlags.NoGetString, true))
                    return ReturnCode.Continue;

                foreach (string uniqueResourceName in uniqueResourceNames.Keys)
                {
                    string resourceValue = null;

                    if (!noPluginResourceName && (pluginName != null))
                    {
                        string pluginUniqueResourceName =
                            pluginName + Characters.Period + uniqueResourceName;

                        try
                        {
                            if ((counts != null) && (counts.Length > 2))
                                counts[2]++;

                            Result error = null;

                            resourceValue = plugin.GetString(
                                interpreter, pluginUniqueResourceName,
                                cultureInfo, ref error); /* throw */

                            if ((counts != null) && (counts.Length > 3))
                                counts[3]++;

                            if (resourceValue == null)
                            {
                                if (verbose && (error != null))
                                {
                                    if (errors == null)
                                        errors = new ResultList();

                                    /* VERBOSE */
                                    errors.Add(error);
                                }
                            }
                        }
                        catch (Exception e)
                        {
                            if (verbose)
                            {
                                if (errors == null)
                                    errors = new ResultList();

                                /* VERBOSE */
                                errors.Add(e);
                            }

                            if (failOnException)
                                return ReturnCode.Error;
                            else if (stopOnException)
                                break;
                        }

                        if (resourceValue != null)
                        {
                            using (StringReader stringReader =
                                    new StringReader(resourceValue)) /* throw */
                            {
                                string text = null;
                                bool canRetry = false;
                                Result error = null;

                                if (_Engine.ReadScriptStream(
                                        interpreter, name, stringReader,
                                        0, Count.Invalid, ref engineFlags,
                                        ref text, ref canRetry,
                                        ref error) == ReturnCode.Ok)
                                {
                                    PluginDataTriplet anyTriplet = NewPluginDataTriplet(
                                        plugin, "GetString", pluginUniqueResourceName,
                                        isolated);

                                    scriptFlags |= ScriptFlags.ClientData;
                                    clientData = new ClientData(anyTriplet);
                                    result = text;

                                    return ReturnCode.Ok;
                                }
                                else
                                {
                                    if (verbose && (error != null))
                                    {
                                        if (errors == null)
                                            errors = new ResultList();

                                        /* VERBOSE */
                                        errors.Add(error);
                                    }

                                    if (failOnError)
                                        return ReturnCode.Error;
                                    else if (stopOnError)
                                        break;
                                    else if (!ignoreCanRetry && !canRetry)
                                        return ReturnCode.Error;
                                }
                            }
                        }
                    }

                    if (!noRawResourceName)
                    {
                        try
                        {
                            if ((counts != null) && (counts.Length > 2))
                                counts[2]++;

                            Result error = null;

                            resourceValue = plugin.GetString(
                                interpreter, uniqueResourceName,
                                cultureInfo, ref error); /* throw */

                            if ((counts != null) && (counts.Length > 3))
                                counts[3]++;

                            if (resourceValue == null)
                            {
                                if (verbose && (error != null))
                                {
                                    if (errors == null)
                                        errors = new ResultList();

                                    /* VERBOSE */
                                    errors.Add(error);
                                }
                            }
                        }
                        catch (Exception e)
                        {
                            if (verbose)
                            {
                                if (errors == null)
                                    errors = new ResultList();

                                /* VERBOSE */
                                errors.Add(e);
                            }

                            if (failOnException)
                                return ReturnCode.Error;
                            else if (stopOnException)
                                break;
                        }
                    }

                    if (resourceValue != null)
                    {
                        using (StringReader stringReader =
                                new StringReader(resourceValue)) /* throw */
                        {
                            string text = null;
                            bool canRetry = false;
                            Result error = null;

                            if (_Engine.ReadScriptStream(
                                    interpreter, name, stringReader,
                                    0, Count.Invalid, ref engineFlags,
                                    ref text, ref canRetry,
                                    ref error) == ReturnCode.Ok)
                            {
                                PluginDataTriplet anyTriplet = NewPluginDataTriplet(
                                    plugin, "GetString", uniqueResourceName,
                                    isolated);

                                scriptFlags |= ScriptFlags.ClientData;
                                clientData = new ClientData(anyTriplet);
                                result = text;

                                return ReturnCode.Ok;
                            }
                            else
                            {
                                if (verbose && (error != null))
                                {
                                    if (errors == null)
                                        errors = new ResultList();

                                    /* VERBOSE */
                                    errors.Add(error);
                                }

                                if (failOnError)
                                    return ReturnCode.Error;
                                else if (stopOnError)
                                    break;
                                else if (!ignoreCanRetry && !canRetry)
                                    return ReturnCode.Error;
                            }
                        }
                    }
                }
            }

            return ReturnCode.Continue;
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////////////////////////////

        #region Resource Manager Support Methods
        //
        // WARNING: The "internal" use is designed for
        //          the HostOps.GetScript method only.
        //
        protected internal virtual ReturnCode GetDataViaResourceManager(
            Interpreter interpreter,                    /* in */
            string name,                                /* in */
            ResourceManagerPair resourceManagerAnyPair, /* in */
            StringDictionary uniqueResourceNames,       /* in */
            EngineFlags engineFlags,                    /* in */
            DataFlags dataFlags,                        /* in */
            bool verbose,                               /* in */
            bool isolated,                              /* in */
            int[] counts,                               /* in, out */
            ref ScriptFlags scriptFlags,                /* in, out */
            ref IClientData clientData,                 /* out */
            ref Result result,                          /* out */
            ref ResultList errors                       /* in, out */
            )
        {
            //
            // HACK: Skip all invalid resource managers.  Also, skip doing
            //       anything if the data type is unsupported.
            //
            if ((resourceManagerAnyPair == null) ||
                (!FlagOps.HasFlags(dataFlags, DataFlags.Bytes, true) &&
                !FlagOps.HasFlags(dataFlags, DataFlags.Text, true)))
            {
                return ReturnCode.Continue;
            }

            if (uniqueResourceNames == null)
                return ReturnCode.Continue;

            ResourceManager resourceManager = resourceManagerAnyPair.Y;

            if (resourceManager == null)
                return ReturnCode.Continue;

            bool failOnException;
            bool stopOnException;
            bool failOnError;
            bool stopOnError;
            bool ignoreCanRetry;

            ExtractErrorHandlingScriptFlags(
                scriptFlags, out failOnException, out stopOnException,
                out failOnError, out stopOnError, out ignoreCanRetry);

            if (FlagOps.HasFlags(dataFlags, DataFlags.Bytes, true))
            {
                if (FlagOps.HasFlags(dataFlags, DataFlags.NoStream, true) ||
                    FlagOps.HasFlags(
                        dataFlags, DataFlags.NoResourceManagerStream, true))
                {
                    return ReturnCode.Continue;
                }

                foreach (string uniqueResourceName in uniqueResourceNames.Keys)
                {
                    try
                    {
                        if ((counts != null) && (counts.Length > 4))
                            counts[4]++;

                        Stream resourceStream = resourceManager.GetStream(
                            uniqueResourceName); /* throw */

                        if ((counts != null) && (counts.Length > 5))
                            counts[5]++;

                        //
                        // NOTE: In order to continue, we must have the found the
                        //       resource stream associated with the named resource.
                        //
                        if (resourceStream != null)
                        {
                            using (BinaryReader binaryReader =
                                    new BinaryReader(resourceStream)) /* throw */
                            {
                                try
                                {
                                    byte[] bytes = binaryReader.ReadBytes(
                                        (int)resourceStream.Length); /* throw */

                                    ResourceManagerTriplet anyTriplet = NewResourceManagerTriplet(
                                        resourceManagerAnyPair, "GetStream", uniqueResourceName,
                                        isolated);

                                    scriptFlags |= ScriptFlags.ClientData;
                                    clientData = new ClientData(anyTriplet);
                                    result = bytes;

                                    return ReturnCode.Ok;
                                }
                                catch (Exception e)
                                {
                                    if (verbose)
                                    {
                                        if (errors == null)
                                            errors = new ResultList();

                                        /* VERBOSE */
                                        errors.Add(e);
                                    }

                                    if (failOnException)
                                        return ReturnCode.Error;
                                    else if (stopOnException)
                                        break;
                                }
                            }
                        }
                    }
                    catch (MissingManifestResourceException) /* EXPECTED */
                    {
                        // do nothing.
                    }
                    catch (InvalidOperationException) /* EXPECTED */
                    {
                        // do nothing.
                    }
                    catch (Exception e)
                    {
                        if (verbose)
                        {
                            if (errors == null)
                                errors = new ResultList();

                            /* VERBOSE */
                            errors.Add(e);
                        }

                        if (failOnException)
                            return ReturnCode.Error;
                        else if (stopOnException)
                            break;
                    }
                }
            }
            else if (FlagOps.HasFlags(dataFlags, DataFlags.Text, true))
            {
                bool getStringOnly = false;

                if (FlagOps.HasFlags(dataFlags, DataFlags.NoStream, true) ||
                    FlagOps.HasFlags(
                        dataFlags, DataFlags.NoResourceManagerStream, true))
                {
                    getStringOnly = true;
                }

                foreach (string uniqueResourceName in uniqueResourceNames.Keys)
                {
                    bool useGetString = getStringOnly;

                    if (!useGetString)
                    {
                        try
                        {
                            if ((counts != null) && (counts.Length > 4))
                                counts[4]++;

                            Stream resourceStream = resourceManager.GetStream(
                                uniqueResourceName); /* throw */

                            if ((counts != null) && (counts.Length > 5))
                                counts[5]++;

                            //
                            // NOTE: In order to continue, we must have the found the
                            //       resource stream associated with the named resource.
                            //
                            if (resourceStream != null)
                            {
                                using (StreamReader streamReader =
                                        new StreamReader(resourceStream)) /* throw */
                                {
                                    string text = null;
                                    bool canRetry = false;
                                    Result error = null;

                                    if (_Engine.ReadScriptStream(
                                            interpreter, name, streamReader,
                                            0, Count.Invalid, ref engineFlags,
                                            ref text, ref canRetry,
                                            ref error) == ReturnCode.Ok)
                                    {
                                        ResourceManagerTriplet anyTriplet = NewResourceManagerTriplet(
                                            resourceManagerAnyPair, "GetStream", uniqueResourceName,
                                            isolated);

                                        scriptFlags |= ScriptFlags.ClientData;
                                        clientData = new ClientData(anyTriplet);
                                        result = text;

                                        return ReturnCode.Ok;
                                    }
                                    else
                                    {
                                        if (verbose && (error != null))
                                        {
                                            if (errors == null)
                                                errors = new ResultList();

                                            /* VERBOSE */
                                            errors.Add(error);
                                        }

                                        if (failOnError)
                                            return ReturnCode.Error;
                                        else if (stopOnError)
                                            break;
                                        else if (!ignoreCanRetry && !canRetry)
                                            return ReturnCode.Error;
                                    }
                                }
                            }
                        }
                        catch (MissingManifestResourceException) /* EXPECTED */
                        {
                            // do nothing.
                        }
                        catch (InvalidOperationException) /* EXPECTED */
                        {
                            //
                            // NOTE: If we get to this point, it means that the
                            //       resource does exist; however, it cannot be
                            //       accessed via stream.  Attempt to fetch the
                            //       script as a resource string.
                            //
                            useGetString = true;
                        }
                        catch (Exception e)
                        {
                            if (verbose)
                            {
                                if (errors == null)
                                    errors = new ResultList();

                                /* VERBOSE */
                                errors.Add(e);
                            }

                            if (failOnException)
                                return ReturnCode.Error;
                            else if (stopOnException)
                                break;
                        }
                    }

                    if (useGetString)
                    {
                        string resourceValue = null;

                        try
                        {
                            if ((counts != null) && (counts.Length > 4))
                                counts[4]++;

                            resourceValue = resourceManager.GetString(
                                uniqueResourceName); /* throw */

                            if ((counts != null) && (counts.Length > 5))
                                counts[5]++;
                        }
                        catch (MissingManifestResourceException) /* EXPECTED */
                        {
                            // do nothing.
                        }
                        catch (InvalidOperationException) /* EXPECTED */
                        {
                            // do nothing.
                        }
                        catch (Exception e)
                        {
                            if (verbose)
                            {
                                if (errors == null)
                                    errors = new ResultList();

                                /* VERBOSE */
                                errors.Add(e);
                            }

                            if (failOnException)
                                return ReturnCode.Error;
                            else if (stopOnException)
                                break;
                        }

                        //
                        // NOTE: In order to continue, we must have the found
                        //       the resource stream associated with the named
                        //       resource.
                        //
                        if (resourceValue != null)
                        {
                            using (StringReader stringReader =
                                    new StringReader(resourceValue)) /* throw */
                            {
                                string text = null;
                                bool canRetry = false;
                                Result error = null;

                                if (_Engine.ReadScriptStream(
                                        interpreter, name, stringReader,
                                        0, Count.Invalid, ref engineFlags,
                                        ref text, ref canRetry,
                                        ref error) == ReturnCode.Ok)
                                {
                                    ResourceManagerTriplet anyTriplet = NewResourceManagerTriplet(
                                        resourceManagerAnyPair, "GetString", uniqueResourceName,
                                        isolated);

                                    scriptFlags |= ScriptFlags.ClientData;
                                    clientData = new ClientData(anyTriplet);
                                    result = text;

                                    return ReturnCode.Ok;
                                }
                                else
                                {
                                    if (verbose && (error != null))
                                    {
                                        if (errors == null)
                                            errors = new ResultList();

                                        /* VERBOSE */
                                        errors.Add(error);
                                    }

                                    if (failOnError)
                                        return ReturnCode.Error;
                                    else if (stopOnError)
                                        break;
                                    else if (!ignoreCanRetry && !canRetry)
                                        return ReturnCode.Error;
                                }
                            }
                        }
                    }
                }
            }

            return ReturnCode.Continue;
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////////////////////////////

        #region Assembly Manifest Support Methods
        protected virtual ReturnCode GetDataViaAssemblyManifest(
            Interpreter interpreter,              /* in */
            string name,                          /* in */
            Assembly assembly,                    /* in */
            StringDictionary uniqueResourceNames, /* in */
            EngineFlags engineFlags,              /* in */
            DataFlags dataFlags,                  /* in */
            bool verbose,                         /* in */
            bool isolated,                        /* in */
            int[] counts,                         /* in, out */
            ref ScriptFlags scriptFlags,          /* in, out */
            ref IClientData clientData,           /* out */
            ref Result result,                    /* out */
            ref ResultList errors                 /* in, out */
            )
        {
            //
            // HACK: Skip all invalid assemblies.  Also, skip doing anything
            //       if the data type is unsupported.
            //
            if ((assembly == null) ||
                (!FlagOps.HasFlags(dataFlags, DataFlags.Bytes, true) &&
                !FlagOps.HasFlags(dataFlags, DataFlags.Text, true)))
            {
                return ReturnCode.Continue;
            }

            if (uniqueResourceNames == null)
                return ReturnCode.Continue;

            bool failOnException;
            bool stopOnException;
            bool failOnError;
            bool stopOnError;
            bool ignoreCanRetry;

            ExtractErrorHandlingScriptFlags(
                scriptFlags, out failOnException, out stopOnException,
                out failOnError, out stopOnError, out ignoreCanRetry);

            if (FlagOps.HasFlags(dataFlags, DataFlags.Bytes, true))
            {
                if (FlagOps.HasFlags(dataFlags, DataFlags.NoStream, true) ||
                    FlagOps.HasFlags(
                        dataFlags, DataFlags.NoAssemblyManifestStream, true))
                {
                    return ReturnCode.Continue;
                }

                foreach (string uniqueResourceName in uniqueResourceNames.Keys)
                {
                    try
                    {
                        if ((counts != null) && (counts.Length > 6))
                            counts[6]++;

                        Stream resourceStream = assembly.GetManifestResourceStream(
                            uniqueResourceName); /* throw */

                        if ((counts != null) && (counts.Length > 7))
                            counts[7]++;

                        //
                        // NOTE: In order to continue, we must have the found the
                        //       resource stream associated with the named resource.
                        //
                        if (resourceStream != null)
                        {
                            using (BinaryReader binaryReader =
                                    new BinaryReader(resourceStream)) /* throw */
                            {
                                try
                                {
                                    byte[] bytes = binaryReader.ReadBytes(
                                        (int)resourceStream.Length); /* throw */

                                    AssemblyTriplet anyTriplet = NewAssemblyTriplet(
                                        assembly, "GetStream", uniqueResourceName,
                                        isolated);

                                    scriptFlags |= ScriptFlags.ClientData;
                                    clientData = new ClientData(anyTriplet);
                                    result = bytes;

                                    return ReturnCode.Ok;
                                }
                                catch (Exception e)
                                {
                                    if (verbose)
                                    {
                                        if (errors == null)
                                            errors = new ResultList();

                                        /* VERBOSE */
                                        errors.Add(e);
                                    }

                                    if (failOnException)
                                        return ReturnCode.Error;
                                    else if (stopOnException)
                                        break;
                                }
                            }
                        }
                    }
                    catch (MissingManifestResourceException) /* EXPECTED */
                    {
                        // do nothing.
                    }
                    catch (Exception e)
                    {
                        if (verbose)
                        {
                            if (errors == null)
                                errors = new ResultList();

                            /* VERBOSE */
                            errors.Add(e);
                        }

                        if (failOnException)
                            return ReturnCode.Error;
                        else if (stopOnException)
                            break;
                    }
                }
            }
            else if (FlagOps.HasFlags(dataFlags, DataFlags.Text, true))
            {
                if (FlagOps.HasFlags(dataFlags, DataFlags.NoStream, true) ||
                    FlagOps.HasFlags(
                        dataFlags, DataFlags.NoAssemblyManifestStream, true))
                {
                    return ReturnCode.Continue;
                }

                foreach (string uniqueResourceName in uniqueResourceNames.Keys)
                {
                    try
                    {
                        if ((counts != null) && (counts.Length > 6))
                            counts[6]++;

                        Stream resourceStream = assembly.GetManifestResourceStream(
                            uniqueResourceName); /* throw */

                        if ((counts != null) && (counts.Length > 7))
                            counts[7]++;

                        //
                        // NOTE: In order to continue, we must have the found the
                        //       resource stream associated with the named resource.
                        //
                        if (resourceStream != null)
                        {
                            using (StreamReader streamReader =
                                    new StreamReader(resourceStream)) /* throw */
                            {
                                string text = null;
                                bool canRetry = false;
                                Result error = null;

                                if (_Engine.ReadScriptStream(
                                        interpreter, name, streamReader,
                                        0, Count.Invalid, ref engineFlags,
                                        ref text, ref canRetry,
                                        ref error) == ReturnCode.Ok)
                                {
                                    AssemblyTriplet anyTriplet = NewAssemblyTriplet(
                                        assembly, "GetStream", uniqueResourceName,
                                        isolated);

                                    scriptFlags |= ScriptFlags.ClientData;
                                    clientData = new ClientData(anyTriplet);
                                    result = text;

                                    return ReturnCode.Ok;
                                }
                                else
                                {
                                    if (verbose && (error != null))
                                    {
                                        if (errors == null)
                                            errors = new ResultList();

                                        /* VERBOSE */
                                        errors.Add(error);
                                    }

                                    if (failOnError)
                                        return ReturnCode.Error;
                                    else if (stopOnError)
                                        break;
                                    else if (!ignoreCanRetry && !canRetry)
                                        return ReturnCode.Error;
                                }
                            }
                        }
                    }
                    catch (MissingManifestResourceException) /* EXPECTED */
                    {
                        // do nothing.
                    }
                    catch (Exception e)
                    {
                        if (verbose)
                        {
                            if (errors == null)
                                errors = new ResultList();

                            /* VERBOSE */
                            errors.Add(e);
                        }

                        if (failOnException)
                            return ReturnCode.Error;
                        else if (stopOnException)
                            break;
                    }
                }
            }

            return ReturnCode.Continue;
        }
        #endregion
        #endregion

        ///////////////////////////////////////////////////////////////////////////////////////////////

        #region Host Flags Support
        private void PrivateResetHostFlagsOnly()
        {
            hostFlags = HostFlags.Invalid;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        private bool PrivateResetHostFlags()
        {
            PrivateResetHostFlagsOnly();
            return base.ResetHostFlags();
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        protected override HostFlags MaybeInitializeHostFlags()
        {
            if (hostFlags == HostFlags.Invalid)
            {
                //
                // NOTE: We support the "GetStream" and "GetData"
                //       methods.
                //
                hostFlags = HostFlags.Stream | HostFlags.Data |
                            base.MaybeInitializeHostFlags();

#if ISOLATED_PLUGINS
                //
                // NOTE: If this host is not running in the same
                //       application domain as the parent interpreter,
                //       also add the "Isolated" flag.
                //
                if (SafeIsIsolated())
                    hostFlags |= HostFlags.Isolated;
#endif
            }

            return hostFlags;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        protected override void SetReadException(
            bool exception
            )
        {
            base.SetReadException(exception);
            PrivateResetHostFlagsOnly();
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        protected override void SetWriteException(
            bool exception
            )
        {
            base.SetWriteException(exception);
            PrivateResetHostFlagsOnly();
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////////////////////////////

        #region IGetInterpreter / ISetInterpreter Members
        private Interpreter interpreter;
        public virtual Interpreter Interpreter
        {
            get { CheckDisposed(); return interpreter; }
            set { CheckDisposed(); interpreter = value; }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////////////////////////////

        #region Protected Properties
        private string libraryResourceBaseName;
        protected internal virtual string LibraryResourceBaseName
        {
            get { return libraryResourceBaseName; }
            set { libraryResourceBaseName = value; }
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        private ResourceManager libraryResourceManager;
        protected internal virtual ResourceManager LibraryResourceManager
        {
            get { return libraryResourceManager; }
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        private string packagesResourceBaseName;
        protected internal virtual string PackagesResourceBaseName
        {
            get { return packagesResourceBaseName; }
            set { packagesResourceBaseName = value; }
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        private ResourceManager packagesResourceManager;
        protected internal virtual ResourceManager PackagesResourceManager
        {
            get { return packagesResourceManager; }
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        private string applicationResourceBaseName;
        protected internal virtual string ApplicationResourceBaseName
        {
            get { return applicationResourceBaseName; }
            set { applicationResourceBaseName = value; }
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        private ResourceManager applicationResourceManager;
        protected internal virtual ResourceManager ApplicationResourceManager
        {
            get { return applicationResourceManager; }
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        private ResourceManager resourceManager;
        protected internal virtual ResourceManager ResourceManager
        {
            get { return resourceManager; }
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        private ScriptFlags libraryScriptFlags;
        protected internal virtual ScriptFlags LibraryScriptFlags
        {
            get { return libraryScriptFlags; }
            set { libraryScriptFlags = value; }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////////////////////////////

        #region Script Resource Support
        private bool SetupLibraryResourceManager()
        {
            try
            {
                //
                // NOTE: Create a resource manager for the embedded core script
                //       library, if any.
                //
                libraryResourceManager = new ResourceManager(
                    LibraryResourceBaseName, GlobalState.GetAssembly());

                //
                // NOTE: Now, since creating it will pretty much always succeed,
                //       we need to test it to make sure it is really available.
                //
                /* IGNORED */
                libraryResourceManager.GetString(
                    NotFoundResourceName); /* throw */

                //
                // NOTE: If we get this far, the resource manager is created and
                //       functional.
                //
                return true;
            }
            catch (Exception e)
            {
                TraceOps.DebugTrace(
                    e, typeof(File).Name,
                    TracePriority.HostError);

                //
                // NOTE: The resource manager we created does not appear to work,
                //       null it out so that it will not be used later.
                //
                if (libraryResourceManager != null)
                    libraryResourceManager = null;
            }

            return false;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        private bool SetupPackagesResourceManager()
        {
            try
            {
                //
                // NOTE: Create a resource manager for the embedded core script
                //       packages, if any.
                //
                packagesResourceManager = new ResourceManager(
                    PackagesResourceBaseName, GlobalState.GetAssembly());

                //
                // NOTE: Now, since creating it will pretty much always succeed,
                //       we need to test it to make sure it is really available.
                //
                /* IGNORED */
                packagesResourceManager.GetString(
                    NotFoundResourceName); /* throw */

                //
                // NOTE: If we get this far, the resource manager is created and
                //       functional.
                //
                return true;
            }
            catch (Exception e)
            {
                TraceOps.DebugTrace(
                    e, typeof(File).Name,
                    TracePriority.HostError);

                //
                // NOTE: The resource manager we created does not appear to work,
                //       null it out so that it will not be used later.
                //
                if (packagesResourceManager != null)
                    packagesResourceManager = null;
            }

            return false;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        private bool CopyFromApplicationResourceManager()
        {
            if (applicationResourceManager != null)
            {
                staticApplicationResourceManager = applicationResourceManager;
                return true;
            }

            staticApplicationResourceManager = null;
            return false;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        private bool CopyToApplicationResourceManager()
        {
            if (staticApplicationResourceManager != null)
            {
                applicationResourceManager = staticApplicationResourceManager;
                return true;
            }

            applicationResourceManager = null;
            return false;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        private bool SetupApplicationResourceManager()
        {
            try
            {
                //
                // NOTE: Create a resource manager for the embedded application
                //       scripts, if any.
                //
                applicationResourceManager = new ResourceManager(
                    ApplicationResourceBaseName, GlobalState.GetAssembly());

                //
                // NOTE: Now, since creating it will pretty much always succeed,
                //       we need to test it to make sure it is really available.
                //
                /* IGNORED */
                applicationResourceManager.GetString(
                    NotFoundResourceName); /* throw */

                //
                // NOTE: If we get this far, the resource manager is created and
                //       functional.
                //
                return true;
            }
#if (DEBUG || FORCE_TRACE) && VERBOSE
            catch (Exception e)
#else
            catch
#endif
            {
#if (DEBUG || FORCE_TRACE) && VERBOSE
                TraceOps.DebugTrace(
                    e, typeof(File).Name,
                    TracePriority.HostError);
#endif

                //
                // NOTE: The resource manager we created does not appear to work,
                //       null it out so that it will not be used later.
                //
                if (applicationResourceManager != null)
                    applicationResourceManager = null;
            }

            return false;
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////////////////////////////

        #region IInteractiveHost Members
        private HostFlags hostFlags = HostFlags.Invalid;
        public override HostFlags GetHostFlags()
        {
            CheckDisposed();

            return MaybeInitializeHostFlags();
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////////////////////////////

        #region IFileSystemHost Members
        public override ReturnCode GetStream(
            string path,
            FileMode mode,
            FileAccess access,
            FileShare share,
            int bufferSize,
            FileOptions options,
            ref HostStreamFlags hostStreamFlags,
            ref string fullPath,
            ref Stream stream,
            ref Result error
            )
        {
            CheckDisposed();

            try
            {
                return RuntimeOps.NewStream(
                    UnsafeGetInterpreter(), path, mode, access, share,
                    bufferSize, options, ref hostStreamFlags, ref fullPath,
                    ref stream, ref error);
            }
            catch (Exception e)
            {
                error = e;
            }

            return ReturnCode.Error;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public override ReturnCode GetData(
            string name,                 /* in */
            DataFlags dataFlags,         /* in */
            ref ScriptFlags scriptFlags, /* in, out */
            ref IClientData clientData,  /* out */
            ref Result result            /* out */
            )
        {
            CheckDisposed();

            //
            // NOTE: The purpose of this routine is to redirect requests for
            //       library scripts made by the script engine to our internal
            //       resources (i.e. so that the scripts do not have to exist
            //       elsewhere on the file system).
            //
            Interpreter localInterpreter = InternalSafeGetInterpreter(false);

            GetDataTrace(
                localInterpreter, "entered",
                name, dataFlags, scriptFlags,
                clientData, ReturnCode.Ok,
                result);

            //
            // NOTE: Permit the key parameters to be customized by derived
            //       classes as as well with the configured core script flags,
            //       if any.
            //
            if (!CheckDataParameters(
                    localInterpreter, ref name, ref dataFlags,
                    ref scriptFlags, ref clientData, ref result)) /* HOOK */
            {
                GetDataTrace(
                    localInterpreter,
                    "exited, bad parameters",
                    name, dataFlags, scriptFlags,
                    clientData, ReturnCode.Error,
                    result);

                return ReturnCode.Error;
            }

            //
            // NOTE: Check if the requested data name is allowed.  If not,
            //       then return an error now.
            //
            if (!ShouldAllowDataParameters(
                    localInterpreter, ref name, ref dataFlags,
                    ref scriptFlags, ref clientData, ref result)) /* HOOK */
            {
                GetDataTrace(
                    localInterpreter,
                    "exited, access denied",
                    name, dataFlags, scriptFlags,
                    clientData, ReturnCode.Error,
                    result);

                return ReturnCode.Error;
            }
            //
            // NOTE: Otherwise, if the script name appears to be a file name
            //       with no directory information -AND- the script name is
            //       reserved by the host (e.g. "pkgIndex.eagle"), issue a
            //       warning now.
            //
            else if (IsReservedDataName(
                    localInterpreter, name, dataFlags, scriptFlags,
                    clientData)) /* HOOK */
            {
                dataFlags |= DataFlags.ReservedName;

                bool exists = false;

                if (IsFileNameOnlyDataName(name))
                {
                    GetDataTrace(localInterpreter,
                        "WARNING: detected reserved script name without directory",
                        name, dataFlags, scriptFlags, clientData, ReturnCode.Ok,
                        result);
                }
                else if (!IsAbsoluteFileNameDataName(
                        name, ref exists) && !exists)
                {
                    GetDataTrace(localInterpreter,
                        "WARNING: detected reserved script name with relative path",
                        name, dataFlags, scriptFlags, clientData, ReturnCode.Ok,
                        result);
                }
            }

            ///////////////////////////////////////////////////////////////////

            //
            // NOTE: Make sure the script name is [still?] valid.
            //
            if (name == null)
            {
                result = "invalid script name";

                GetDataTrace(
                    localInterpreter,
                    "exited, invalid script name",
                    name, dataFlags, scriptFlags,
                    clientData, ReturnCode.Error,
                    result);

                return ReturnCode.Error;
            }

            //
            // NOTE: An interpreter instance is required in order to help
            //       locate the script.  If we do not have one, bail out now.
            //
            if (localInterpreter == null)
            {
                result = "invalid interpreter";

                GetDataTrace(
                    localInterpreter,
                    "exited, invalid interpreter",
                    name, dataFlags, scriptFlags,
                    clientData, ReturnCode.Error,
                    result);

                return ReturnCode.Error;
            }

            ///////////////////////////////////////////////////////////////////

            //
            // NOTE: Are we operating in the "quiet" error handling
            //       mode?
            //
            bool quiet = FlagOps.HasFlags(scriptFlags,
                ScriptFlags.Quiet, true);

            //
            // NOTE: These are the tracking flags for which subsystems were
            //       actually checked.  The first element is for the file
            //       system.  The second element is for all the loaded
            //       plugins, excluding system plugins.  The third element
            //       is the customizable resource manager associated with
            //       this host.  The fourth element is the application
            //       resource manager for the assembly this host belongs
            //       to.  The fifth element is the library resource manager
            //       for the assembly this host belongs to.  The sixth
            //       element is the resource manager associated with the
            //       parent interpreter.  The seventh element is the core
            //       library assembly manifest.
            //
            bool[] @checked = {
                false, false, false, false, false, false, false, false
            };

            //
            // NOTE: These are the tracking counts for how many tries
            //       were performed using the file system, plugins, and
            //       resource managers.
            //
            int[] counts = { 0, 0, 0, 0, 0, 0, 0, 0 };

            //
            // NOTE: This is the list of errors encountered during the
            //       search for the requested script.
            //
            ResultList errors = null;

            ///////////////////////////////////////////////////////////////////

            //
            // NOTE: Are we operating in the "verbose" error handling mode?
            //
            bool verbose = FlagOps.HasFlags(
                scriptFlags, ScriptFlags.Verbose, true);

            //
            // NOTE: When compiled with isolated plugin support, check if
            //       the current method is running in an application domain
            //       isolated from our parent interpreter.
            //
#if ISOLATED_PLUGINS
            bool isolated = AppDomainOps.IsIsolated(localInterpreter);
#else
            bool isolated = false;
#endif

            ///////////////////////////////////////////////////////////////////

            //
            // NOTE: First, if it has not been prohibited by the caller,
            //       try to get the requested script externally, using
            //       our standard file system search routine.
            //
            if (!FlagOps.HasFlags(
                    scriptFlags, ScriptFlags.NoFileSystem, true))
            {
                @checked[0] = true;

                ReturnCode code = GetDataViaFileSystem(
                    localInterpreter, name, dataFlags, counts,
                    verbose, isolated, ref scriptFlags,
                    ref clientData, ref result, ref errors);

                if ((code == ReturnCode.Ok) ||
                    (code == ReturnCode.Error))
                {
                    GetDataTrace(
                        localInterpreter,
                        "exited, via file system",
                        name, dataFlags, scriptFlags,
                        clientData, code, result);

                    return code;
                }
            }

            ///////////////////////////////////////////////////////////////////

            if (!FlagOps.HasFlags(
                    scriptFlags, ScriptFlags.NoResources, true))
            {
                StringDictionary uniqueResourceNames = null;

                PopulateUniqueResourceNames(
                    localInterpreter, name, dataFlags, scriptFlags,
                    verbose, ref uniqueResourceNames);

                EngineFlags engineFlags = GetEngineFlagsForReadScriptStream(
                    localInterpreter, dataFlags, scriptFlags);

                ///////////////////////////////////////////////////////////////

                //
                // NOTE: See if we are allowed to search for the script via
                //       plugin resource strings.
                //
                if (!FlagOps.HasFlags(
                        scriptFlags, ScriptFlags.NoPlugins, true))
                {
                    PluginWrapperDictionary plugins =
                        localInterpreter.CopyPlugins();

                    if (plugins != null)
                    {
                        @checked[1] = true;

                        CultureInfo cultureInfo =
                            localInterpreter.InternalCultureInfo;

                        foreach (PluginKeyValuePair pair in plugins)
                        {
                            IPlugin plugin = pair.Value;

                            //
                            // NOTE: This method *MUST* return
                            //       "ReturnCode.Continue" in
                            //       order to keep searching.
                            //
                            ReturnCode code = GetDataViaPlugin(
                                localInterpreter, name, plugin,
                                uniqueResourceNames, cultureInfo,
                                engineFlags, dataFlags, verbose,
                                isolated, counts, ref scriptFlags,
                                ref clientData, ref result,
                                ref errors);

                            if ((code == ReturnCode.Ok) ||
                                (code == ReturnCode.Error))
                            {
                                GetDataTrace(
                                    localInterpreter,
                                    "exited, via plugin",
                                    name, dataFlags, scriptFlags,
                                    clientData, code, result);

                                return code;
                            }
                        }
                    }
                }

                ///////////////////////////////////////////////////////////////

                //
                // NOTE: Are we being forbidden from using any resource
                //       managers?
                //
                if (!FlagOps.HasFlags(
                        scriptFlags, ScriptFlags.NoResourceManager, true))
                {
                    //
                    // NOTE: In order to use the interpreter resource
                    //       manager, we must be in the same application
                    //       domain.  We should always be able to use
                    //       both our own resource manager and the one
                    //       associated with the assembly containing
                    //       this host.  Grab and check them both now.
                    //
                    ResourceManager thisResourceManager = !FlagOps.HasFlags(
                        scriptFlags, ScriptFlags.NoHostResourceManager, true) ?
                            this.ResourceManager : null;

                    if (thisResourceManager != null)
                        @checked[2] = true;

                    ResourceManager applicationResourceManager = !FlagOps.HasFlags(
                        scriptFlags, ScriptFlags.NoApplicationResourceManager, true) ?
                            this.ApplicationResourceManager : null;

                    if (applicationResourceManager != null)
                        @checked[3] = true;

                    ResourceManager libraryResourceManager = !FlagOps.HasFlags(
                        scriptFlags, ScriptFlags.NoLibraryResourceManager, true) ?
                            this.LibraryResourceManager : null;

                    if (libraryResourceManager != null)
                        @checked[4] = true;

                    ResourceManager packagesResourceManager = !FlagOps.HasFlags(
                        scriptFlags, ScriptFlags.NoPackagesResourceManager, true) ?
                            this.PackagesResourceManager : null;

                    if (packagesResourceManager != null)
                        @checked[5] = true;

                    //
                    // NOTE: If this host is running isolated (i.e. in
                    //       an isolated application domain, via a
                    //       plugin), skip using the resource manager
                    //       from the interpreter because it cannot be
                    //       marshalled from the other application
                    //       domain (it's a private field).
                    //
                    ResourceManager interpreterResourceManager =
#if ISOLATED_PLUGINS
                        !isolated ? localInterpreter.ResourceManager : null;
#else
                        localInterpreter.ResourceManager;
#endif

                    if (interpreterResourceManager != null)
                        @checked[6] = true;

                    //
                    // NOTE: We prefer to use the customizable resource
                    //       manager, then the application resource
                    //       manager, then the library resource manager,
                    //       and finally the resource manager for the
                    //       interpreter that we are associated with,
                    //       which may contain scripts.
                    //
                    ResourceManagerPair[] resourceManagers =
                        new AnyPair<string, ResourceManager>[] {
                        new AnyPair<string, ResourceManager>(
                            null, thisResourceManager),
                        new AnyPair<string, ResourceManager>(
                            GlobalState.GetAssemblyLocation(),
                            applicationResourceManager),
                        new AnyPair<string, ResourceManager>(
                            GlobalState.GetAssemblyLocation(),
                            packagesResourceManager),
                        new AnyPair<string, ResourceManager>(
                            GlobalState.GetAssemblyLocation(),
                            libraryResourceManager),
                        new AnyPair<string, ResourceManager>(
                            null, interpreterResourceManager)
                    };

                    foreach (ResourceManagerPair anyPair
                            in resourceManagers)
                    {
                        //
                        // NOTE: This method *MUST* return
                        //       "ReturnCode.Continue" in
                        //       order to keep searching.
                        //
                        ReturnCode code = GetDataViaResourceManager(
                            localInterpreter, name, anyPair,
                            uniqueResourceNames, engineFlags,
                            dataFlags, verbose, isolated, counts,
                            ref scriptFlags, ref clientData,
                            ref result, ref errors);

                        if ((code == ReturnCode.Ok) ||
                            (code == ReturnCode.Error))
                        {
                            GetDataTrace(
                                localInterpreter,
                                "exited, via resource manager",
                                name, dataFlags, scriptFlags,
                                clientData, code, result);

                            return code;
                        }
                    }
                }

                ///////////////////////////////////////////////////////////////

                //
                // NOTE: Are we being forbidden from using the assembly
                //       manifest?
                //
                if (!FlagOps.HasFlags(
                        scriptFlags, ScriptFlags.NoAssemblyManifest, true))
                {
                    Assembly assembly = GlobalState.GetAssembly();

                    if (assembly != null)
                        @checked[7] = true;

                    //
                    // NOTE: This method *MUST* return
                    //       "ReturnCode.Continue" in
                    //       order to keep searching.
                    //
                    ReturnCode code = GetDataViaAssemblyManifest(
                        localInterpreter, name, assembly,
                        uniqueResourceNames, engineFlags,
                        dataFlags, verbose, isolated, counts,
                        ref scriptFlags, ref clientData,
                        ref result, ref errors);

                    if ((code == ReturnCode.Ok) ||
                        (code == ReturnCode.Error))
                    {
                        GetDataTrace(
                            localInterpreter,
                            "exited, via assembly manifest",
                            name, dataFlags, scriptFlags,
                            clientData, code, result);

                        return code;
                    }
                }
            }

            ///////////////////////////////////////////////////////////////////

            if (errors == null)
                errors = new ResultList();

            /* NOT VERBOSE */
            errors.Insert(0, String.Format(
                "data \"{0}\" not found",
                name));

            //
            // NOTE: In quiet mode, skip the other error information.
            //
            if (!quiet)
            {
                if (!@checked[0])
                    /* NOT VERBOSE */
                    errors.Add("skipped file system");

                if (counts[0] == 0)
                    /* NOT VERBOSE */
                    errors.Add("no files were checked");

                if (counts[0] != counts[1])
                    /* NOT VERBOSE */
                    errors.Add("error while checking files");

                if (!@checked[1])
                    /* NOT VERBOSE */
                    errors.Add("skipped plugin list");

                if (counts[2] == 0)
                    /* NOT VERBOSE */
                    errors.Add("no plugins were queried");

                if (counts[2] != counts[3])
                    /* NOT VERBOSE */
                    errors.Add("error while querying plugins");

                if (!@checked[2])
                    /* NOT VERBOSE */
                    errors.Add("skipped extension resource manager");

                if (!@checked[3])
                    /* NOT VERBOSE */
                    errors.Add("skipped application resource manager");

                if (!@checked[4])
                    /* NOT VERBOSE */
                    errors.Add("skipped library resource manager");

                if (!@checked[5])
                    /* NOT VERBOSE */
                    errors.Add("skipped packages resource manager");

                if (!@checked[6])
                    /* NOT VERBOSE */
                    errors.Add("skipped interpreter resource manager");

                if (counts[4] == 0)
                    /* NOT VERBOSE */
                    errors.Add("no resource managers were queried");

                if (counts[4] != counts[5])
                    /* NOT VERBOSE */
                    errors.Add("error while querying resource managers");

                if (!@checked[7])
                    /* NOT VERBOSE */
                    errors.Add("skipped assembly manifest");

                if (counts[6] == 0)
                    /* NOT VERBOSE */
                    errors.Add("no assembly manifests were queried");

                if (counts[6] != counts[7])
                    /* NOT VERBOSE */
                    errors.Add("error while querying assembly manifests");
            }

            result = errors;

            GetDataTrace(
                localInterpreter,
                "exited, not found",
                name, dataFlags, scriptFlags,
                clientData, ReturnCode.Error,
                result);

            return ReturnCode.Error;
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////////////////////////////

        #region IHost Members
        public override bool ResetHostFlags()
        {
            CheckDisposed();

            return PrivateResetHostFlags();
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public override ReturnCode Reset(
            ref Result error
            )
        {
            CheckDisposed();

            if (base.Reset(ref error) == ReturnCode.Ok)
            {
                if (!PrivateResetHostFlags()) /* NON-VIRTUAL */
                {
                    error = "failed to reset flags";
                    return ReturnCode.Error;
                }

                return ReturnCode.Ok;
            }

            return ReturnCode.Error;
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////////////////////////////

        #region IMaybeDisposed Members
        public override bool Disposed
        {
            get { return disposed; }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////////////////////////////

        #region IDisposable "Pattern" Members
        private bool disposed;
        private void CheckDisposed() /* throw */
        {
#if THROW_ON_DISPOSED
            if (disposed &&
                _Engine.IsThrowOnDisposed(interpreter /* EXEMPT */, null))
            {
                throw new InterpreterDisposedException(typeof(Script));
            }
#endif
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        protected override void Dispose(bool disposing)
        {
            try
            {
                if (!disposed)
                {
                    //if (disposing)
                    //{
                    //    ////////////////////////////////////
                    //    // dispose managed resources here...
                    //    ////////////////////////////////////
                    //}

                    //////////////////////////////////////
                    // release unmanaged resources here...
                    //////////////////////////////////////
                }
            }
            finally
            {
                base.Dispose(disposing);

                disposed = true;
            }
        }
        #endregion
    }
}
