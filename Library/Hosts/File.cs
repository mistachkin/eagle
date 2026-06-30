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

using PluginPair = System.Collections.Generic.KeyValuePair<
    string, Eagle._Wrappers.Plugin>;

using ResourceManagerPair = Eagle._Interfaces.Public.IAnyPair<
    string, System.Resources.ResourceManager>;

#if NET_STANDARD_21
using Index = Eagle._Constants.Index;
#endif

namespace Eagle._Hosts
{
    /// <summary>
    /// This class provides an abstract host implementation, derived from the
    /// <see cref="Engine" /> host, that is able to locate and load script (and
    /// other) data on behalf of the script engine.  When the engine requests a
    /// named piece of data (e.g. a library script), this host searches a series
    /// of sources, in order: the bundle manager, the snippet manager, the file
    /// system, the loaded plugins, the various resource managers (host,
    /// application, library, packages, kit, and interpreter), and finally the
    /// assembly manifest.  This allows scripts to be embedded as managed
    /// resources so they need not exist elsewhere on the file system.  It is
    /// abstract because concrete hosts (e.g. the "Default" host) are expected
    /// to derive from it.
    /// </summary>
    [ObjectId("514896d2-7003-45cf-b7fa-69fd443af625")]
    public abstract class File : Engine, IDisposable, IHaveInterpreter
    {
        #region Private Constants
        /// <summary>
        /// The default base name for the resource manager that contains the
        /// embedded core script library.
        /// </summary>
        private const string DefaultLibraryResourceBaseName = "library";

        /// <summary>
        /// The default base name for the resource manager that contains the
        /// embedded core script packages.
        /// </summary>
        private const string DefaultPackagesResourceBaseName = "packages";

        /// <summary>
        /// The default base name for the resource manager that contains the
        /// embedded kit packages.
        /// </summary>
        private const string DefaultKitResourceBaseName = "kit";

        /// <summary>
        /// The default base name for the resource manager that contains the
        /// embedded application-specific (vendor) packages.
        /// </summary>
        private const string DefaultApplicationResourceBaseName = "application";

        /// <summary>
        /// The file name suffix used when checking whether a manifest resource
        /// for a given resource base name is present.
        /// </summary>
        private const string DefaultResourceNameSuffix = ".resources";

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// The name of a resource that is not expected to exist, used to probe
        /// a freshly created resource manager to verify that it is actually
        /// functional.
        /// </summary>
        private const string NotFoundResourceName = "empty";
        #endregion

        ///////////////////////////////////////////////////////////////////////////////////////////////

        #region Private Static Data
        //
        // HACK: For performance reasons, stop trying to query the (possibly
        //       missing) application resource manager for every interpreter
        //       that is created in this AppDomain.
        //
        /// <summary>
        /// The cached application-specific resource manager, shared across all
        /// interpreters created in this application domain so the (possibly
        /// missing) application resource manager need not be queried for each
        /// new interpreter.
        /// </summary>
        private static ResourceManager staticApplicationResourceManager = null;

        /// <summary>
        /// A counter used to ensure that the (potentially expensive) attempt to
        /// set up the application-specific resource manager is only performed
        /// once per application domain.
        /// </summary>
        private static int setupStaticApplicationResourceManager = 0;
        #endregion

        ///////////////////////////////////////////////////////////////////////////////////////////////

        #region Protected Constructors
        /// <summary>
        /// Constructs an instance of this host class.
        /// </summary>
        /// <param name="hostData">
        /// Optional data used to initialize the new host, including the
        /// interpreter that owns it and any custom resource manager.
        /// </param>
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
            kitResourceBaseName = DefaultKitResourceBaseName;
            applicationResourceBaseName = DefaultApplicationResourceBaseName;

            ///////////////////////////////////////////////////////////////////

            if (HasCreateFlags(HostCreateFlags.ResourceManager, true))
            {
                /* IGNORED */
                SetupResourceAssembly(GlobalState.GetAssembly());

                /* IGNORED */
                SetupLibraryResourceManager();

                /* IGNORED */
                SetupPackagesResourceManager();

                /* IGNORED */
                SetupKitResourceManager();

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
        /// <summary>
        /// Gets the interpreter associated with this host, catching and
        /// optionally tracing any exception thrown while doing so.
        /// </summary>
        /// <param name="trace">
        /// Non-zero to emit a trace message if an exception is caught.
        /// </param>
        /// <returns>
        /// The associated interpreter, or null if it could not be obtained.
        /// </returns>
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
        /// <summary>
        /// Clears the interpreter associated with this host, catching and
        /// optionally tracing any exception thrown while doing so.  This method
        /// is only available when compiled with the "TEST" option.
        /// </summary>
        /// <param name="trace">
        /// Non-zero to emit a trace message if an exception is caught.
        /// </param>
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

        /// <summary>
        /// Gets the interpreter associated with this host without catching any
        /// exception thrown while doing so.
        /// </summary>
        /// <returns>
        /// The associated interpreter.
        /// </returns>
        protected Interpreter UnsafeGetInterpreter()
        {
            return Interpreter; /* throw */
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

#if ISOLATED_PLUGINS
        /// <summary>
        /// Determines whether this host is running in an application domain
        /// isolated from its parent interpreter, catching and tracing any
        /// exception thrown while doing so.  This method is only available when
        /// compiled with the "ISOLATED_PLUGINS" option.
        /// </summary>
        /// <returns>
        /// True if this host is isolated; otherwise, false.
        /// </returns>
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

        /// <summary>
        /// Associates this host with its parent interpreter as the isolated
        /// host, catching and tracing any exception thrown while doing so.  This
        /// method is only available when compiled with the "ISOLATED_PLUGINS"
        /// option.
        /// </summary>
        /// <returns>
        /// True if the isolated host was set; otherwise, false.
        /// </returns>
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
        /// <summary>
        /// Determines whether the specified plugin has the specified flags,
        /// catching and tracing any exception thrown while doing so.
        /// </summary>
        /// <param name="pluginData">
        /// The plugin whose flags should be checked; may be null.
        /// </param>
        /// <param name="hasFlags">
        /// The flags to check for.
        /// </param>
        /// <param name="all">
        /// Non-zero to require that all of the specified flags are present;
        /// otherwise, any one of them is sufficient.
        /// </param>
        /// <returns>
        /// True if the plugin has the specified flags; otherwise, false.
        /// </returns>
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
        /// <summary>
        /// Decomposes the specified script flags into the individual boolean
        /// values that control how candidate resource names are generated.
        /// </summary>
        /// <param name="scriptFlags">
        /// The script flags to decompose.
        /// </param>
        /// <param name="skipQualified">
        /// Upon return, non-zero if the fully qualified name should be skipped.
        /// </param>
        /// <param name="skipNonQualified">
        /// Upon return, non-zero if the non-qualified name should be skipped.
        /// </param>
        /// <param name="skipRelative">
        /// Upon return, non-zero if the relative name should be skipped.
        /// </param>
        /// <param name="skipRawName">
        /// Upon return, non-zero if the raw (verbatim) name should be skipped.
        /// </param>
        /// <param name="skipFileName">
        /// Upon return, non-zero if file name based names should be skipped.
        /// </param>
        /// <param name="skipFileNameOnly">
        /// Upon return, non-zero if file-name-only names should be skipped.
        /// </param>
        /// <param name="skipNonFileNameOnly">
        /// Upon return, non-zero if non-file-name-only names should be skipped.
        /// </param>
        /// <param name="skipLibraryToLib">
        /// Upon return, non-zero if rewriting "library" to "lib" should be
        /// skipped.
        /// </param>
        /// <param name="skipTestsToLib">
        /// Upon return, non-zero if rewriting "tests" to "lib" should be
        /// skipped.
        /// </param>
        /// <param name="loaderPackage">
        /// Upon return, non-zero if the loader package type was requested.
        /// </param>
        /// <param name="libraryPackage">
        /// Upon return, non-zero if the library package type was requested.
        /// </param>
        /// <param name="testPackage">
        /// Upon return, non-zero if the test package type was requested.
        /// </param>
        /// <param name="kitPackage">
        /// Upon return, non-zero if the kit package type was requested.
        /// </param>
        /// <param name="automaticPackage">
        /// Upon return, non-zero if the automatic package type was requested.
        /// </param>
        /// <param name="preferDeepFileNames">
        /// Upon return, non-zero if deeper (more nested) file names should be
        /// preferred.
        /// </param>
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
            out bool loaderPackage,       /* out */
            out bool libraryPackage,      /* out */
            out bool testPackage,         /* out */
            out bool kitPackage,          /* out */
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

            loaderPackage = FlagOps.HasFlags(
                scriptFlags, ScriptFlags.LoaderPackage, true);

            libraryPackage = FlagOps.HasFlags(
                scriptFlags, ScriptFlags.LibraryPackage, true);

            testPackage = FlagOps.HasFlags(
                scriptFlags, ScriptFlags.TestPackage, true);

            kitPackage = FlagOps.HasFlags(
                scriptFlags, ScriptFlags.KitPackage, true);

            automaticPackage = FlagOps.HasFlags(
                scriptFlags, ScriptFlags.AutomaticPackage, true);

            preferDeepFileNames = FlagOps.HasFlags(
                scriptFlags, ScriptFlags.PreferDeepFileNames, true);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Decomposes the specified script flags into the individual boolean
        /// values that control how generated resource names are filtered.
        /// </summary>
        /// <param name="scriptFlags">
        /// The script flags to decompose.
        /// </param>
        /// <param name="filterOnSuffixMatch">
        /// Upon return, non-zero if resource names should be filtered to only
        /// those whose suffix matches the requested name.
        /// </param>
        /// <param name="preferDeepResourceNames">
        /// Upon return, non-zero if deeper (more nested) resource names should
        /// be preferred.
        /// </param>
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

        /// <summary>
        /// Decomposes the specified script flags into the individual boolean
        /// values that control bundle manager error handling.
        /// </summary>
        /// <param name="scriptFlags">
        /// The script flags to decompose.
        /// </param>
        /// <param name="failOnError">
        /// Upon return, non-zero if an error reading a script should cause the
        /// overall operation to fail.
        /// </param>
        /// <param name="ignoreCanRetry">
        /// Upon return, non-zero if the "can retry" hint should be ignored when
        /// deciding whether to fail.
        /// </param>
        private static void ExtractBundleManagerScriptFlags(
            ScriptFlags scriptFlags,       /* in */
            out bool failOnError,          /* out */
            out bool ignoreCanRetry        /* out */
            )
        {
            failOnError = FlagOps.HasFlags(
                scriptFlags, ScriptFlags.FailOnError, true);

            ignoreCanRetry = FlagOps.HasFlags(
                scriptFlags, ScriptFlags.IgnoreCanRetry, true);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Decomposes the specified script flags into the individual boolean
        /// values that control how plugins are queried and how errors are
        /// handled while doing so.
        /// </summary>
        /// <param name="scriptFlags">
        /// The script flags to decompose.
        /// </param>
        /// <param name="noPluginResourceName">
        /// Upon return, non-zero if the plugin-qualified resource name should
        /// not be used.
        /// </param>
        /// <param name="noRawResourceName">
        /// Upon return, non-zero if the raw (unqualified) resource name should
        /// not be used.
        /// </param>
        /// <param name="failOnException">
        /// Upon return, non-zero if an exception should cause the overall
        /// operation to fail.
        /// </param>
        /// <param name="stopOnException">
        /// Upon return, non-zero if an exception should stop the search loop.
        /// </param>
        /// <param name="failOnError">
        /// Upon return, non-zero if an error should cause the overall operation
        /// to fail.
        /// </param>
        /// <param name="stopOnError">
        /// Upon return, non-zero if an error should stop the search loop.
        /// </param>
        /// <param name="ignoreCanRetry">
        /// Upon return, non-zero if the "can retry" hint should be ignored when
        /// deciding whether to fail.
        /// </param>
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

        /// <summary>
        /// Decomposes the specified script flags into the individual boolean
        /// values that control error handling while reading script data.
        /// </summary>
        /// <param name="scriptFlags">
        /// The script flags to decompose.
        /// </param>
        /// <param name="failOnException">
        /// Upon return, non-zero if an exception should cause the overall
        /// operation to fail.
        /// </param>
        /// <param name="stopOnException">
        /// Upon return, non-zero if an exception should stop the search loop.
        /// </param>
        /// <param name="failOnError">
        /// Upon return, non-zero if an error should cause the overall operation
        /// to fail.
        /// </param>
        /// <param name="stopOnError">
        /// Upon return, non-zero if an error should stop the search loop.
        /// </param>
        /// <param name="ignoreCanRetry">
        /// Upon return, non-zero if the "can retry" hint should be ignored when
        /// deciding whether to fail.
        /// </param>
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

        /// <summary>
        /// Decomposes the specified package type flags into the individual
        /// boolean values indicating which package types are present.
        /// </summary>
        /// <param name="packageType">
        /// The package type flags to decompose.
        /// </param>
        /// <param name="haveLoaderPackage">
        /// Upon return, non-zero if the loader package type is present.
        /// </param>
        /// <param name="haveLibraryPackage">
        /// Upon return, non-zero if the library package type is present.
        /// </param>
        /// <param name="haveTestPackage">
        /// Upon return, non-zero if the test package type is present.
        /// </param>
        /// <param name="haveKitPackage">
        /// Upon return, non-zero if the kit package type is present.
        /// </param>
        /// <param name="haveAutomaticPackage">
        /// Upon return, non-zero if the automatic package type is present.
        /// </param>
        private static void ExtractResourceNamePackageTypes(
            PackageType packageType,      /* in */
            out bool haveLoaderPackage,   /* out */
            out bool haveLibraryPackage,  /* out */
            out bool haveTestPackage,     /* out */
            out bool haveKitPackage,      /* out */
            out bool haveAutomaticPackage /* out */
            )
        {
            haveLoaderPackage = FlagOps.HasFlags(
                packageType, PackageType.Loader, true);

            haveLibraryPackage = FlagOps.HasFlags(
                packageType, PackageType.Library, true);

            haveTestPackage = FlagOps.HasFlags(
                packageType, PackageType.Test, true);

            haveKitPackage = FlagOps.HasFlags(
                packageType, PackageType.Kit, true);

            haveAutomaticPackage = FlagOps.HasFlags(
                packageType, PackageType.Automatic, true);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Translates the specified script flags into the corresponding file
        /// search flags used when searching the file system for a script.
        /// </summary>
        /// <param name="scriptFlags">
        /// The script flags to translate.
        /// </param>
        /// <param name="fileSearchFlags">
        /// Upon return, the file search flags corresponding to the specified
        /// script flags.
        /// </param>
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
                    scriptFlags, ScriptFlags.NullOnNotFound, true))
            {
                fileSearchFlags |= FileSearchFlags.NullOnNotFound;
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

        #region Static Isolation Support Methods
        /// <summary>
        /// Returns the specified resource manager unless this host is running
        /// isolated, in which case null is returned because the resource manager
        /// cannot be marshalled across application domains.
        /// </summary>
        /// <param name="resourceManager">
        /// The resource manager to conditionally return; may be null.
        /// </param>
        /// <param name="isolated">
        /// Non-zero if this host is running in an isolated application domain.
        /// </param>
        /// <returns>
        /// The specified resource manager, or null if running isolated.
        /// </returns>
        private static ResourceManager MaybeGetResourceManager(
            ResourceManager resourceManager, /* in: OPTIONAL */
            bool isolated                    /* in */
            )
        {
#if ISOLATED_PLUGINS
            if (isolated)
                return null;
#endif

            return resourceManager;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Returns the specified assembly unless this host is running isolated,
        /// in which case null is returned because the assembly cannot be
        /// marshalled across application domains.
        /// </summary>
        /// <param name="assembly">
        /// The assembly to conditionally return; may be null.
        /// </param>
        /// <param name="isolated">
        /// Non-zero if this host is running in an isolated application domain.
        /// </param>
        /// <returns>
        /// The specified assembly, or null if running isolated.
        /// </returns>
        private static Assembly MaybeGetAssembly(
            Assembly assembly, /* in: OPTIONAL */
            bool isolated      /* in */
            )
        {
#if ISOLATED_PLUGINS
            if (isolated)
                return null;
#endif

            return assembly;
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////////////////////////////

        #region Tracing Support Methods
        /// <summary>
        /// Determines the trace priority to use when emitting a trace message
        /// for a data request, based on whether the request is required and
        /// whether it succeeded.
        /// </summary>
        /// <param name="scriptFlags">
        /// The script flags for the data request.
        /// </param>
        /// <param name="returnCode">
        /// The return code of the data request.
        /// </param>
        /// <returns>
        /// The trace priority to use.
        /// </returns>
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

        /// <summary>
        /// Emits a trace message describing a data request, if tracing is
        /// enabled via the specified data flags.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter associated with the data request.
        /// </param>
        /// <param name="prefix">
        /// A short prefix describing the point at which the trace is emitted.
        /// </param>
        /// <param name="name">
        /// The name of the requested data.
        /// </param>
        /// <param name="dataFlags">
        /// The data flags for the request, including whether tracing is enabled.
        /// </param>
        /// <param name="scriptFlags">
        /// The script flags for the request.
        /// </param>
        /// <param name="clientData">
        /// The client data associated with the request, if any.
        /// </param>
        /// <param name="returnCode">
        /// The current return code of the request.
        /// </param>
        /// <param name="result">
        /// The current result of the request.
        /// </param>
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
            if (FlagOps.HasFlags(dataFlags, DataFlags.Trace, true))
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

        /// <summary>
        /// Emits a trace message describing the filtering of candidate script
        /// resource names, if tracing is enabled via the specified data flags.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter associated with the data request.
        /// </param>
        /// <param name="name">
        /// The name of the requested data.
        /// </param>
        /// <param name="resourceNames">
        /// The collection of candidate resource names being filtered, if any.
        /// </param>
        /// <param name="dataFlags">
        /// The data flags for the request, including whether tracing is enabled.
        /// </param>
        /// <param name="scriptFlags">
        /// The script flags for the request.
        /// </param>
        /// <param name="message">
        /// An additional message describing the current filtering step.
        /// </param>
        protected virtual void FilterScriptResourceNamesTrace(
            Interpreter interpreter,           /* in */
            string name,                       /* in */
            IEnumerable<string> resourceNames, /* in */
            DataFlags dataFlags,               /* in */
            ScriptFlags scriptFlags,           /* in */
            string message                     /* in */
            )
        {
            if (FlagOps.HasFlags(dataFlags, DataFlags.Trace, true))
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

        /// <summary>
        /// Emits a trace message describing the de-duplication of candidate
        /// script resource names, if tracing is enabled via the specified data
        /// flags.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter associated with the data request.
        /// </param>
        /// <param name="name">
        /// The name of the requested data.
        /// </param>
        /// <param name="resourceNames">
        /// The collection of candidate resource names before de-duplication.
        /// </param>
        /// <param name="uniqueResourceNames">
        /// The dictionary of unique resource names after de-duplication.
        /// </param>
        /// <param name="dataFlags">
        /// The data flags for the request, including whether tracing is enabled.
        /// </param>
        /// <param name="scriptFlags">
        /// The script flags for the request.
        /// </param>
        protected virtual void GetUniqueResourceNamesTrace(
            Interpreter interpreter,              /* in */
            string name,                          /* in */
            IEnumerable<string> resourceNames,    /* in */
            StringDictionary uniqueResourceNames, /* in */
            DataFlags dataFlags,                  /* in */
            ScriptFlags scriptFlags               /* in */
            )
        {
            if (FlagOps.HasFlags(dataFlags, DataFlags.Trace, true))
            {
                TracePriority priority = GetDataTracePriority(
                    scriptFlags, ReturnCode.Ok);

                StringList list = (resourceNames != null) ?
                    new StringList(resourceNames) : null;

                int[] counts = {
                    Count.Invalid, Count.Invalid, Count.Invalid,
                    Count.Invalid, Count.Invalid
                };

                if (list != null)
                    counts[2] = list.Count;

                if (uniqueResourceNames != null)
                    counts[3] = uniqueResourceNames.Count;

                if ((counts[2] != Count.Invalid) &&
                    (counts[3] != Count.Invalid))
                {
                    counts[4] = counts[2] - counts[3];
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
                    counts[2], counts[3], counts[4]),
                    typeof(File).Name, priority, 1);
            }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////////////////////////////

        #region Reserved Names Support Methods
        /// <summary>
        /// Determines whether a file system search may match using only the
        /// tail (file name) portion of the requested name.  This is forbidden
        /// for scripts evaluated pursuant to a [package] command, for reserved
        /// absolute names, and for core or package scripts.
        /// </summary>
        /// <param name="levels">
        /// The current package nesting level.
        /// </param>
        /// <param name="dataFlags">
        /// The data flags for the request.
        /// </param>
        /// <param name="scriptFlags">
        /// The script flags for the request.
        /// </param>
        /// <returns>
        /// True if a tail-only file search should be allowed; otherwise, false.
        /// </returns>
        protected virtual bool ShouldAllowTailOnlyFileSearch(
            int levels,             /* in */
            DataFlags dataFlags,    /* in */
            ScriptFlags scriptFlags /* in */
            )
        {
            if (levels > 1)
                return false;

            if (FlagOps.HasFlags(
                    dataFlags, DataFlags.ReservedAbsoluteName, true))
            {
                return false;
            }

            if (FlagOps.HasFlags(scriptFlags, ScriptFlags.Core, true) ||
                FlagOps.HasFlags(scriptFlags, ScriptFlags.Package, true))
            {
                return false;
            }

            return true;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Gets the dictionary of well-known (reserved) data names, which comes
        /// from the base class (i.e. the "Default" host).
        /// </summary>
        /// <returns>
        /// The dictionary of reserved data names.
        /// </returns>
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
        /// <summary>
        /// Determines whether the specified data name is one of the reserved
        /// (well-known) names.  This method cannot fail; returning false simply
        /// means that the specified script does not contain a reserved name.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter associated with the request; not used.
        /// </param>
        /// <param name="name">
        /// The name of the requested data.
        /// </param>
        /// <param name="dataFlags">
        /// The data flags for the request; the reserved name flag is added if
        /// the name is reserved.
        /// </param>
        /// <param name="scriptFlags">
        /// The script flags for the request; not used.
        /// </param>
        /// <param name="clientData">
        /// The client data associated with the request; not used.
        /// </param>
        /// <returns>
        /// True if the specified name is reserved; otherwise, false.
        /// </returns>
        protected virtual bool IsReservedDataName(
            Interpreter interpreter, /* in: NOT USED */
            string name,             /* in */
            ref DataFlags dataFlags, /* in, out */
            ScriptFlags scriptFlags, /* in: NOT USED */
            IClientData clientData   /* in: NOT USED */
            )
        {
            if (name == null)
                return false;

            IDictionary<string, string> dictionary = GetReservedDataNames();

            if (dictionary == null)
                return false;

            if (!dictionary.ContainsKey(name))
                return false;

            dataFlags |= DataFlags.ReservedName;
            return true;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        //
        // NOTE: This method cannot fail.  Returning false simply means that
        //       the specified [file] name contains directory information as
        //       well.
        //
        /// <summary>
        /// Determines whether the specified data name consists of a file name
        /// only, with no directory information.  This method cannot fail;
        /// returning false simply means that the name contains directory
        /// information as well.
        /// </summary>
        /// <param name="name">
        /// The name of the requested data.
        /// </param>
        /// <param name="dataFlags">
        /// The data flags for the request; the reserved tail-only name flag is
        /// added if the name is a file name only.
        /// </param>
        /// <returns>
        /// True if the specified name is a file name only; otherwise, false.
        /// </returns>
        protected virtual bool IsFileNameOnlyDataName(
            string name,            /* in */
            ref DataFlags dataFlags /* in */
            )
        {
            if (PathOps.HasDirectory(name))
                return false;

            dataFlags |= DataFlags.ReservedTailOnlyName;
            return true;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        //
        // NOTE: This method cannot fail.  Returning false simply means that
        //       the specified [file] name does not contain an absolute path.
        //
        /// <summary>
        /// Determines whether the specified data name is an absolute (rooted)
        /// file name.  This method cannot fail; returning false simply means
        /// that the name does not contain an absolute path.
        /// </summary>
        /// <param name="name">
        /// The name of the requested data.
        /// </param>
        /// <param name="dataFlags">
        /// The data flags for the request; the reserved absolute name flag is
        /// added if the name is an absolute file name.
        /// </param>
        /// <param name="exists">
        /// Upon return, non-zero if a file with the specified name exists.
        /// </param>
        /// <returns>
        /// True if the specified name is an absolute file name; otherwise,
        /// false.
        /// </returns>
        protected virtual bool IsAbsoluteFileNameDataName(
            string name,             /* in */
            ref DataFlags dataFlags, /* in, out */
            ref bool exists          /* out */
            )
        {
            try
            {
                exists = _File.Exists(name); /* throw */

                if (Path.IsPathRooted(name)) /* throw */
                {
                    dataFlags |= DataFlags.ReservedAbsoluteName;
                    return true;
                }
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
        /// <summary>
        /// Permits the parameters of a data request to be customized by derived
        /// classes; this base implementation merges in the configured library
        /// script flags, if any.  If this method returns false in a derived
        /// class, it must set the error message as well.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter associated with the request; not used.
        /// </param>
        /// <param name="name">
        /// The name of the requested data; not used.
        /// </param>
        /// <param name="dataFlags">
        /// The data flags for the request; not used.
        /// </param>
        /// <param name="scriptFlags">
        /// The script flags for the request; the configured library script
        /// flags are merged into this value.
        /// </param>
        /// <param name="clientData">
        /// The client data associated with the request; not used.
        /// </param>
        /// <param name="error">
        /// Upon failure, the error message.
        /// </param>
        /// <returns>
        /// True if the parameters were checked successfully; otherwise, false.
        /// </returns>
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
        /// <summary>
        /// Determines whether a data request with the specified parameters is
        /// allowed.  This base implementation is a stub that always allows the
        /// request; if a derived class returns false, it must set the error
        /// message as well.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter associated with the request; not used.
        /// </param>
        /// <param name="name">
        /// The name of the requested data; not used.
        /// </param>
        /// <param name="dataFlags">
        /// The data flags for the request; not used.
        /// </param>
        /// <param name="scriptFlags">
        /// The script flags for the request; not used.
        /// </param>
        /// <param name="clientData">
        /// The client data associated with the request; not used.
        /// </param>
        /// <param name="error">
        /// Upon failure, the error message; not used.
        /// </param>
        /// <returns>
        /// True if the data request is allowed; otherwise, false.
        /// </returns>
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
        /// <summary>
        /// Infers the package type(s) implied by the specified resource name,
        /// based on the well-known package sub-path components it contains,
        /// and merges them into the specified package type flags.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter associated with the request; not used.
        /// </param>
        /// <param name="name">
        /// The resource name to inspect.
        /// </param>
        /// <param name="dataFlags">
        /// The data flags for the request; not used.
        /// </param>
        /// <param name="scriptFlags">
        /// The script flags for the request; not used.
        /// </param>
        /// <param name="packageType">
        /// The initial package type flags to augment.
        /// </param>
        /// <returns>
        /// The package type flags, with any inferred package types added.
        /// </returns>
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

                if (unixName.IndexOf(ScriptPaths.LoaderPackage,
                        SharedStringOps.SystemComparisonType) != Index.Invalid)
                {
                    packageType |= PackageType.Loader;
                }

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

                if (unixName.IndexOf(ScriptPaths.KitPackage,
                        SharedStringOps.SystemComparisonType) != Index.Invalid)
                {
                    packageType |= PackageType.Kit;
                }

                string libName = PathOps.MaybeToLib(unixName, false, false, false);

                if ((libName != null) &&
                    !SharedStringOps.SystemEquals(libName, unixName))
                {
                    if (libName.IndexOf(ScriptPaths.LoaderPackage,
                            SharedStringOps.SystemComparisonType) != Index.Invalid)
                    {
                        packageType |= PackageType.Loader;
                    }

                    if (libName.IndexOf(ScriptPaths.LibraryPackage,
                            SharedStringOps.SystemComparisonType) != Index.Invalid)
                    {
                        packageType |= PackageType.Library;
                    }

                    if (libName.IndexOf(ScriptPaths.TestPackage,
                            SharedStringOps.SystemComparisonType) != Index.Invalid)
                    {
                        packageType |= PackageType.Test;
                    }

                    if (libName.IndexOf(ScriptPaths.KitPackage,
                            SharedStringOps.SystemComparisonType) != Index.Invalid)
                    {
                        packageType |= PackageType.Kit;
                    }
                }
            }

            return packageType;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Generates the ordered list of candidate embedded resource names to
        /// try when locating the script associated with the specified name.  The
        /// candidates cover the verbatim name, package-relative names, and
        /// relative names, each with and without a file extension, as controlled
        /// by the specified script flags.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter associated with the request.
        /// </param>
        /// <param name="name">
        /// The name of the requested data.
        /// </param>
        /// <param name="dataFlags">
        /// The data flags for the request; not used.
        /// </param>
        /// <param name="scriptFlags">
        /// The script flags for the request, which control which candidate
        /// names are generated.
        /// </param>
        /// <param name="verbose">
        /// Non-zero to enable verbose tracing/diagnostics while generating the
        /// candidate names.
        /// </param>
        /// <returns>
        /// The ordered collection of candidate resource names; some elements
        /// may be null.
        /// </returns>
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
            bool loaderPackage;
            bool libraryPackage;
            bool testPackage;
            bool kitPackage;
            bool automaticPackage;
            bool preferDeepFileNames;

            ExtractResourceNameScriptFlags(
                scriptFlags, out skipQualified,
                out skipNonQualified, out skipRelative,
                out skipRawName, out skipFileName,
                out skipFileNameOnly, out skipNonFileNameOnly,
                out skipLibraryToLib, out skipTestsToLib,
                out loaderPackage, out libraryPackage,
                out testPackage, out kitPackage,
                out automaticPackage, out preferDeepFileNames);

            PackageType packageType = PackageType.None;

            if (loaderPackage)
                packageType |= PackageType.Loader;

            if (libraryPackage)
                packageType |= PackageType.Library;

            if (testPackage)
                packageType |= PackageType.Test;

            if (kitPackage)
                packageType |= PackageType.Kit;

            if (automaticPackage)
                packageType |= PackageType.Automatic;

            packageType = GetPackageTypeForResourceName(
                interpreter, name, dataFlags, scriptFlags,
                packageType);

            bool haveLoaderPackage;
            bool haveLibraryPackage;
            bool haveTestPackage;
            bool haveKitPackage;
            bool haveAutomaticPackage;

            ExtractResourceNamePackageTypes(packageType,
                out haveLoaderPackage, out haveLibraryPackage,
                out haveTestPackage, out haveKitPackage,
                out haveAutomaticPackage);

            string[] fileNames = {
                null, null, null, null, null, null, null, null,
                null, null, null, null, null, null, null, null
            };

            if ((name != null) &&
                (!skipQualified || !skipRelative) && !skipFileName)
            {
                if (!skipNonFileNameOnly)
                {
                    if (haveLoaderPackage || haveAutomaticPackage)
                    {
                        fileNames[0] = FormatOps.ScriptTypeToFileName(
                            name, PackageType.Loader, false, true);

                        if (!skipLibraryToLib || !skipTestsToLib)
                        {
                            fileNames[1] = PathOps.MaybeToLib(
                                fileNames[0], skipLibraryToLib,
                                skipTestsToLib, false);
                        }
                    }

                    if (haveLibraryPackage || haveAutomaticPackage)
                    {
                        fileNames[2] = FormatOps.ScriptTypeToFileName(
                            name, PackageType.Library, false, true);

                        if (!skipLibraryToLib || !skipTestsToLib)
                        {
                            fileNames[3] = PathOps.MaybeToLib(
                                fileNames[2], skipLibraryToLib,
                                skipTestsToLib, false);
                        }
                    }

                    if (haveTestPackage || haveAutomaticPackage)
                    {
                        fileNames[4] = FormatOps.ScriptTypeToFileName(
                            name, PackageType.Test, false, true);

                        if (!skipLibraryToLib || !skipTestsToLib)
                        {
                            fileNames[5] = PathOps.MaybeToLib(
                                fileNames[4], skipLibraryToLib,
                                skipTestsToLib, false);
                        }
                    }

                    if (haveKitPackage || haveAutomaticPackage)
                    {
                        fileNames[6] = FormatOps.ScriptTypeToFileName(
                            name, PackageType.Kit, false, true);

                        if (!skipLibraryToLib || !skipTestsToLib)
                        {
                            fileNames[7] = PathOps.MaybeToLib(
                                fileNames[6], skipLibraryToLib,
                                skipTestsToLib, false);
                        }
                    }
                }

                if (!skipFileNameOnly)
                {
                    if (haveLoaderPackage || haveAutomaticPackage)
                    {
                        fileNames[8] = FormatOps.ScriptTypeToFileName(
                            name, PackageType.Loader, true, true);

                        if (!skipLibraryToLib || !skipTestsToLib)
                        {
                            fileNames[9] = PathOps.MaybeToLib(
                                fileNames[8], skipLibraryToLib,
                                skipTestsToLib, false);
                        }
                    }

                    if (haveLibraryPackage || haveAutomaticPackage)
                    {
                        fileNames[10] = FormatOps.ScriptTypeToFileName(
                            name, PackageType.Library, true, true);

                        if (!skipLibraryToLib || !skipTestsToLib)
                        {
                            fileNames[11] = PathOps.MaybeToLib(
                                fileNames[10], skipLibraryToLib,
                                skipTestsToLib, false);
                        }
                    }

                    if (haveTestPackage || haveAutomaticPackage)
                    {
                        fileNames[12] = FormatOps.ScriptTypeToFileName(
                            name, PackageType.Test, true, true);

                        if (!skipLibraryToLib || !skipTestsToLib)
                        {
                            fileNames[13] = PathOps.MaybeToLib(
                                fileNames[12], skipLibraryToLib,
                                skipTestsToLib, false);
                        }
                    }

                    if (haveKitPackage || haveAutomaticPackage)
                    {
                        fileNames[14] = FormatOps.ScriptTypeToFileName(
                            name, PackageType.Kit, true, true);

                        if (!skipLibraryToLib || !skipTestsToLib)
                        {
                            fileNames[15] = PathOps.MaybeToLib(
                                fileNames[14], skipLibraryToLib,
                                skipTestsToLib, false);
                        }
                    }
                }
            }

            string baseName = null;

            if ((!skipRawName || !skipFileName) && !skipNonQualified)
                baseName = (name != null) ? Path.GetFileName(name) : null;

            string[] baseFileNames = {
                null, null, null, null, null, null, null, null,
                null, null, null, null, null, null, null, null
            };

            if ((baseName != null) && !skipNonQualified && !skipFileName)
            {
                if (!skipNonFileNameOnly)
                {
                    if (haveLoaderPackage || haveAutomaticPackage)
                    {
                        baseFileNames[0] = FormatOps.ScriptTypeToFileName(
                            baseName, PackageType.Loader, false, true);

                        if (!skipLibraryToLib || !skipTestsToLib)
                        {
                            baseFileNames[1] = PathOps.MaybeToLib(
                                baseFileNames[0], skipLibraryToLib,
                                skipTestsToLib, false);
                        }
                    }

                    if (haveLibraryPackage || haveAutomaticPackage)
                    {
                        baseFileNames[2] = FormatOps.ScriptTypeToFileName(
                            baseName, PackageType.Library, false, true);

                        if (!skipLibraryToLib || !skipTestsToLib)
                        {
                            baseFileNames[3] = PathOps.MaybeToLib(
                                baseFileNames[2], skipLibraryToLib,
                                skipTestsToLib, false);
                        }
                    }

                    if (haveTestPackage || haveAutomaticPackage)
                    {
                        baseFileNames[4] = FormatOps.ScriptTypeToFileName(
                            baseName, PackageType.Test, false, true);

                        if (!skipLibraryToLib || !skipTestsToLib)
                        {
                            baseFileNames[5] = PathOps.MaybeToLib(
                                baseFileNames[4], skipLibraryToLib,
                                skipTestsToLib, false);
                        }
                    }

                    if (haveKitPackage || haveAutomaticPackage)
                    {
                        baseFileNames[6] = FormatOps.ScriptTypeToFileName(
                            baseName, PackageType.Kit, false, true);

                        if (!skipLibraryToLib || !skipTestsToLib)
                        {
                            baseFileNames[7] = PathOps.MaybeToLib(
                                baseFileNames[6], skipLibraryToLib,
                                skipTestsToLib, false);
                        }
                    }
                }

                if (!skipFileNameOnly)
                {
                    if (haveLoaderPackage || haveAutomaticPackage)
                    {
                        baseFileNames[8] = FormatOps.ScriptTypeToFileName(
                            baseName, PackageType.Loader, true, true);

                        if (!skipLibraryToLib || !skipTestsToLib)
                        {
                            baseFileNames[9] = PathOps.MaybeToLib(
                                baseFileNames[8], skipLibraryToLib,
                                skipTestsToLib, false);
                        }
                    }

                    if (haveLibraryPackage || haveAutomaticPackage)
                    {
                        baseFileNames[10] = FormatOps.ScriptTypeToFileName(
                            baseName, PackageType.Library, true, true);

                        if (!skipLibraryToLib || !skipTestsToLib)
                        {
                            baseFileNames[11] = PathOps.MaybeToLib(
                                baseFileNames[10], skipLibraryToLib,
                                skipTestsToLib, false);
                        }
                    }

                    if (haveTestPackage || haveAutomaticPackage)
                    {
                        baseFileNames[12] = FormatOps.ScriptTypeToFileName(
                            baseName, PackageType.Test, true, true);

                        if (!skipLibraryToLib || !skipTestsToLib)
                        {
                            baseFileNames[13] = PathOps.MaybeToLib(
                                baseFileNames[12], skipLibraryToLib,
                                skipTestsToLib, false);
                        }
                    }

                    if (haveKitPackage || haveAutomaticPackage)
                    {
                        baseFileNames[14] = FormatOps.ScriptTypeToFileName(
                            baseName, PackageType.Kit, true, true);

                        if (!skipLibraryToLib || !skipTestsToLib)
                        {
                            baseFileNames[15] = PathOps.MaybeToLib(
                                baseFileNames[14], skipLibraryToLib,
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
                    fileNames[2] : null,
                !skipQualified && !skipFileName && !skipNonFileNameOnly &&
                    !skipLibraryToLib ? fileNames[3] : null,
                !skipQualified && !skipFileName && !skipNonFileNameOnly ?
                    fileNames[4] : null,
                !skipQualified && !skipFileName && !skipNonFileNameOnly &&
                    !skipLibraryToLib ? fileNames[5] : null,
                !skipQualified && !skipFileName && !skipNonFileNameOnly ?
                    fileNames[6] : null,
                !skipQualified && !skipFileName && !skipNonFileNameOnly &&
                    !skipLibraryToLib ? fileNames[7] : null,
                !skipQualified && !skipFileName && !skipFileNameOnly ?
                    fileNames[10] : null,
                !skipQualified && !skipFileName && !skipFileNameOnly &&
                    !skipLibraryToLib ? fileNames[11] : null,
                !skipQualified && !skipFileName && !skipFileNameOnly ?
                    fileNames[12] : null,
                !skipQualified && !skipFileName && !skipFileNameOnly &&
                    !skipLibraryToLib ? fileNames[13] : null,

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
                        fileNames[2], pathComparisonType, verbose) : null,
                !skipRelative && !skipFileName && !skipNonFileNameOnly &&
                    (!skipLibraryToLib || !skipTestsToLib) ?
                        PathOps.MaybeToLib(fileNames[2], skipLibraryToLib,
                            skipTestsToLib, true) : null,
                !skipRelative && !skipFileName && !skipNonFileNameOnly ?
                    PackageOps.GetRelativeFileName(interpreter,
                        fileNames[3], pathComparisonType, verbose) : null,
                !skipRelative && !skipFileName && !skipNonFileNameOnly &&
                    (!skipLibraryToLib || !skipTestsToLib) ?
                        PathOps.MaybeToLib(fileNames[3], skipLibraryToLib,
                            skipTestsToLib, true) : null,
                !skipRelative && !skipFileName && !skipFileNameOnly ?
                    PackageOps.GetRelativeFileName(interpreter,
                        fileNames[4], pathComparisonType, verbose) : null,
                !skipRelative && !skipFileName && !skipFileNameOnly &&
                    (!skipLibraryToLib || !skipTestsToLib) ?
                        PathOps.MaybeToLib(fileNames[4], skipLibraryToLib,
                            skipTestsToLib, true) : null,
                !skipRelative && !skipFileName && !skipFileNameOnly ?
                    PackageOps.GetRelativeFileName(interpreter,
                        fileNames[5], pathComparisonType, verbose) : null,
                !skipRelative && !skipFileName && !skipFileNameOnly &&
                    (!skipLibraryToLib || !skipTestsToLib) ?
                        PathOps.MaybeToLib(fileNames[5], skipLibraryToLib,
                            skipTestsToLib, true) : null,
                !skipRelative && !skipFileName && !skipFileNameOnly ?
                    PackageOps.GetRelativeFileName(interpreter,
                        fileNames[6], pathComparisonType, verbose) : null,
                !skipRelative && !skipFileName && !skipFileNameOnly &&
                    (!skipLibraryToLib || !skipTestsToLib) ?
                        PathOps.MaybeToLib(fileNames[6], skipLibraryToLib,
                            skipTestsToLib, true) : null,
                !skipRelative && !skipFileName && !skipFileNameOnly ?
                    PackageOps.GetRelativeFileName(interpreter,
                        fileNames[7], pathComparisonType, verbose) : null,
                !skipRelative && !skipFileName && !skipFileNameOnly &&
                    (!skipLibraryToLib || !skipTestsToLib) ?
                        PathOps.MaybeToLib(fileNames[7], skipLibraryToLib,
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
                    baseFileNames[2] : null,
                !skipNonQualified && !skipFileName && !skipNonFileNameOnly &&
                    !skipLibraryToLib ? baseFileNames[3] : null,
                !skipNonQualified && !skipFileName && !skipNonFileNameOnly ?
                    baseFileNames[4] : null,
                !skipNonQualified && !skipFileName && !skipNonFileNameOnly &&
                    !skipLibraryToLib ? baseFileNames[5] : null,
                !skipNonQualified && !skipFileName && !skipNonFileNameOnly ?
                    baseFileNames[6] : null,
                !skipNonQualified && !skipFileName && !skipNonFileNameOnly &&
                    !skipLibraryToLib ? baseFileNames[7] : null,
                !skipNonQualified && !skipFileName && !skipFileNameOnly ?
                    baseFileNames[10] : null,
                !skipNonQualified && !skipFileName && !skipFileNameOnly &&
                    !skipLibraryToLib ? baseFileNames[11] : null,
                !skipNonQualified && !skipFileName && !skipFileNameOnly ?
                    baseFileNames[12] : null,
                !skipNonQualified && !skipFileName && !skipFileNameOnly &&
                    !skipLibraryToLib ? baseFileNames[13] : null,
                !skipNonQualified && !skipFileName && !skipFileNameOnly ?
                    baseFileNames[14] : null,
                !skipNonQualified && !skipFileName && !skipFileNameOnly &&
                    !skipLibraryToLib ? baseFileNames[15] : null
            };
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Filters and/or sorts the specified collection of candidate resource
        /// names according to the specified script flags, optionally keeping
        /// only those whose suffix matches the requested name and optionally
        /// preferring deeper (more nested) names.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter associated with the request.
        /// </param>
        /// <param name="name">
        /// The name of the requested data.
        /// </param>
        /// <param name="resourceNames">
        /// The collection of candidate resource names to filter.
        /// </param>
        /// <param name="dataFlags">
        /// The data flags for the request.
        /// </param>
        /// <param name="scriptFlags">
        /// The script flags for the request, which control the filtering.
        /// </param>
        /// <param name="verbose">
        /// Non-zero to enable verbose tracing while filtering.
        /// </param>
        /// <returns>
        /// The filtered (and possibly sorted) collection of resource names.
        /// </returns>
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

                        newResourceNames.Sort(_Comparers.StringFileName.Create(
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

        /// <summary>
        /// Builds a dictionary of unique resource names from the specified
        /// collection of candidate resource names, so that duplicates are not
        /// searched for needlessly.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter associated with the request.
        /// </param>
        /// <param name="name">
        /// The name of the requested data.
        /// </param>
        /// <param name="resourceNames">
        /// The collection of candidate resource names, which may contain
        /// duplicates and null entries.
        /// </param>
        /// <param name="dataFlags">
        /// The data flags for the request.
        /// </param>
        /// <param name="scriptFlags">
        /// The script flags for the request.
        /// </param>
        /// <param name="verbose">
        /// Non-zero to enable verbose tracing.
        /// </param>
        /// <returns>
        /// A dictionary whose keys are the unique, non-null resource names.
        /// </returns>
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

        /// <summary>
        /// Generates, filters, and de-duplicates the candidate resource names
        /// for the specified data name, producing the dictionary of unique
        /// resource names to search.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter associated with the request.
        /// </param>
        /// <param name="name">
        /// The name of the requested data.
        /// </param>
        /// <param name="dataFlags">
        /// The data flags for the request.
        /// </param>
        /// <param name="scriptFlags">
        /// The script flags for the request.
        /// </param>
        /// <param name="verbose">
        /// Non-zero to enable verbose tracing.
        /// </param>
        /// <param name="uniqueResourceNames">
        /// Upon return, the dictionary of unique resource names to search.
        /// </param>
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

        #region GetData Support Methods
        /// <summary>
        /// Adds the specified count to the tracking counter at the specified
        /// index, ignoring the request if the array is null, empty, or the
        /// index is out of range.
        /// </summary>
        /// <param name="counts">
        /// The array of tracking counters to update.
        /// </param>
        /// <param name="index">
        /// The index of the counter to update.
        /// </param>
        /// <param name="count">
        /// The amount to add to the counter (may be negative).
        /// </param>
        protected virtual void IncrementGetDataCount(
            int[] counts, /* in, out */
            int index,    /* in */
            int count     /* in */
            )
        {
            if (counts == null)
                return;

            int length = counts.Length;

            if (length == 0)
                return;

            if ((index < 0) || (index >= length))
                return;

            counts[index] += count;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Splits the specified data name into its path parts, removing and
        /// returning the leading directory parts while leaving only the file
        /// name portion in the name parameter.
        /// </summary>
        /// <param name="name">
        /// On input, the data name to split; upon return, the file name portion
        /// only.
        /// </param>
        /// <returns>
        /// The list of leading directory parts, or null if the name is empty or
        /// contains no directory information.
        /// </returns>
        protected virtual StringList GetDataSubParts(
            ref string name /* in, out */
            )
        {
            if (String.IsNullOrEmpty(name))
                return null;

            //
            // HACK: Break the data name into parts and remove the
            //       file name portion; otherwise, just return null.
            //
            StringList subParts = PathOps.SplitPath(null, name);

            if (subParts == null)
                return null;

            int count = subParts.Count;

            if (count <= 1)
                return null;

            name = subParts[count - 1]; /* GRAB FILE NAME ONLY */
            subParts.RemoveAt(count - 1); /* REMOVE FILE NAME */

            return subParts;
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////////////////////////////

        #region Snippet Manager Support Methods
        /// <summary>
        /// Attempts to satisfy the data request using the snippet manager
        /// associated with the specified interpreter.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter whose snippet manager should be consulted.
        /// </param>
        /// <param name="name">
        /// The name of the requested data (snippet).
        /// </param>
        /// <param name="dataFlags">
        /// The data flags for the request; not used.
        /// </param>
        /// <param name="counts">
        /// The array of tracking counters to update.
        /// </param>
        /// <param name="verbose">
        /// Non-zero to enable verbose tracing; not used.
        /// </param>
        /// <param name="isolated">
        /// Non-zero if this host is running isolated; not used.
        /// </param>
        /// <param name="scriptFlags">
        /// The script flags for the request; not used.
        /// </param>
        /// <param name="clientData">
        /// Upon return, the client data associated with the result; not used.
        /// </param>
        /// <param name="result">
        /// Upon success, the snippet bytes, XML, or text.
        /// </param>
        /// <param name="errors">
        /// The list of errors encountered; not used.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> if the snippet was found;
        /// <see cref="ReturnCode.Continue" /> to keep searching other sources.
        /// </returns>
        protected virtual ReturnCode GetDataViaSnippetManager(
            Interpreter interpreter,     /* in */
            string name,                 /* in */
            DataFlags dataFlags,         /* in: NOT USED */
            int[] counts,                /* in, out */
            bool verbose,                /* in: NOT USED */
            bool isolated,               /* in: NOT USED */
            ref ScriptFlags scriptFlags, /* in, out: NOT USED */
            ref IClientData clientData,  /* out: NOT USED */
            ref Result result,           /* out */
            ref ResultList errors        /* in, out: NOT USED */
            )
        {
            IncrementGetDataCount(counts, 2, 1);

            if (interpreter != null)
            {
                ISnippet snippet = null;

                if ((interpreter.InternalGetSnippet(name,
                        SnippetFlags.FileHostMask, LookupFlags.FileHostMask,
                        ref snippet) == ReturnCode.Ok) && (snippet != null))
                {
                    SnippetFlags snippetFlags = snippet.SnippetFlags;

                    if (FlagOps.HasFlags(
                            snippetFlags, SnippetFlags.UseBytes, true))
                    {
                        result = snippet.Bytes;
                    }
#if XML
                    else if (FlagOps.HasFlags(
                            snippetFlags, SnippetFlags.UseXml, true))
                    {
                        result = snippet.Xml;
                    }
#endif
                    else
                    {
                        result = snippet.Text;
                    }

                    IncrementGetDataCount(counts, 3, 1);
                    return ReturnCode.Ok;
                }
            }

            IncrementGetDataCount(counts, 3, 1);
            return ReturnCode.Continue;
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////////////////////////////

        #region File System Support Methods
        /// <summary>
        /// Attempts to satisfy the data request by searching the file system,
        /// optionally searching parent directories as well.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter associated with the request.
        /// </param>
        /// <param name="name">
        /// The name of the requested data (script file).
        /// </param>
        /// <param name="dataFlags">
        /// The data flags for the request, which control whether parent
        /// directories are searched.
        /// </param>
        /// <param name="counts">
        /// The array of tracking counters to update.
        /// </param>
        /// <param name="verbose">
        /// Non-zero to enable verbose searching.
        /// </param>
        /// <param name="isolated">
        /// Non-zero if this host is running isolated; not used.
        /// </param>
        /// <param name="scriptFlags">
        /// The script flags for the request, which are translated into file
        /// search flags and updated to indicate a file was found.
        /// </param>
        /// <param name="clientData">
        /// Upon return, the client data associated with the result; not used.
        /// </param>
        /// <param name="result">
        /// Upon success, the full path of the located script file.
        /// </param>
        /// <param name="errors">
        /// The list of errors encountered; not used.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> if the file was found;
        /// <see cref="ReturnCode.Continue" /> to keep searching other sources.
        /// </returns>
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
                    if (!ShouldAllowTailOnlyFileSearch(
                            levels, dataFlags, scriptFlags))
                    {
                        fileSearchFlags &= ~FileSearchFlags.TailOnly;
                    }
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

            int count; /* REUSED */
            string value; /* REUSED */

            IncrementGetDataCount(counts, 4, 1);

            count = 0;

            value = PathOps.Search(
                interpreter, name, fileSearchFlags, ref count);

            IncrementGetDataCount(counts, 4, -1); /* UNDO */
            IncrementGetDataCount(counts, 4, count);
            IncrementGetDataCount(counts, 5, count);

            if (value != null)
            {
                scriptFlags |= ScriptFlags.File;
                result = value;

                return ReturnCode.Ok;
            }

            if (!FlagOps.HasFlags(
                    dataFlags, DataFlags.NoSearchParents, true) &&
                FlagOps.HasFlags(
                    dataFlags, DataFlags.SearchParents, true))
            {
                string nameOnly = name;

                StringList subParts = GetDataSubParts(ref nameOnly);
                StringList patterns = new StringList(nameOnly);
                StringList paths = null;

                IncrementGetDataCount(counts, 4, 1);

                count = PathOps.SearchParents(interpreter,
                    GlobalState.InitializeOrGetBinaryPath(false),
                    subParts, patterns, 1, null, ref paths);

                IncrementGetDataCount(counts, 4, -1); /* UNDO */
                IncrementGetDataCount(counts, 4, count);
                IncrementGetDataCount(counts, 5, count);

                if ((count > 0) &&
                    (paths != null) && (paths.Count > 0))
                {
                    scriptFlags |= ScriptFlags.File;
                    result = paths[0];

                    return ReturnCode.Ok;
                }
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
        /// <summary>
        /// Gets the engine flags that should be used when reading a script
        /// stream for the specified interpreter and flags.  The "internal" use
        /// is designed for the HostOps.GetScript method only.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter associated with the request.
        /// </param>
        /// <param name="dataFlags">
        /// The data flags for the request; not used.
        /// </param>
        /// <param name="scriptFlags">
        /// The script flags for the request.
        /// </param>
        /// <returns>
        /// The engine flags to use when reading the script stream.
        /// </returns>
        protected internal virtual EngineFlags GetEngineFlagsForReadScriptStream(
            Interpreter interpreter, /* in */
            DataFlags dataFlags,     /* in: NOT USED */
            ScriptFlags scriptFlags  /* in */
            )
        {
            return ScriptOps.GetEngineFlagsForReadScriptStream(
                interpreter, dataFlags, scriptFlags);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

#if DATA
        /// <summary>
        /// Attempts to satisfy the data request using the bundle manager
        /// associated with the specified interpreter, mounting the bundle
        /// database if necessary.  This method is only available when compiled
        /// with the "DATA" option.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter whose bundle manager should be consulted.
        /// </param>
        /// <param name="name">
        /// The name of the requested data.
        /// </param>
        /// <param name="cultureInfo">
        /// The culture to use when retrieving the data.
        /// </param>
        /// <param name="engineFlags">
        /// The engine flags to use when reading the script text.
        /// </param>
        /// <param name="dataFlags">
        /// The data flags for the request, which indicate whether bytes or text
        /// are wanted.
        /// </param>
        /// <param name="verbose">
        /// Non-zero to collect verbose error information.
        /// </param>
        /// <param name="isolated">
        /// Non-zero if this host is running isolated.
        /// </param>
        /// <param name="counts">
        /// The array of tracking counters to update.
        /// </param>
        /// <param name="scriptFlags">
        /// The script flags for the request, updated to indicate that client
        /// data is present.
        /// </param>
        /// <param name="clientData">
        /// Upon success, the client data describing the retrieved data.
        /// </param>
        /// <param name="result">
        /// Upon success, the retrieved bytes or text.
        /// </param>
        /// <param name="errors">
        /// The list of errors encountered while searching.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> if the data was found;
        /// <see cref="ReturnCode.Error" /> on a fatal error;
        /// <see cref="ReturnCode.Continue" /> to keep searching other sources.
        /// </returns>
        protected virtual ReturnCode GetDataViaBundleManager(
            Interpreter interpreter,     /* in */
            string name,                 /* in */
            CultureInfo cultureInfo,     /* in */
            EngineFlags engineFlags,     /* in */
            DataFlags dataFlags,         /* in */
            bool verbose,                /* in */
            bool isolated,               /* in */
            int[] counts,                /* in, out */
            ref ScriptFlags scriptFlags, /* in, out */
            ref IClientData clientData,  /* out */
            ref Result result,           /* out */
            ref ResultList errors        /* in, out */
            )
        {
            IncrementGetDataCount(counts, 0, 1);

            if (interpreter == null)
            {
                IncrementGetDataCount(counts, 1, 1);
                return ReturnCode.Continue;
            }

            IBundleManager bundleManager = interpreter.BundleManager;

            if (bundleManager == null)
            {
                IncrementGetDataCount(counts, 1, 1);
                return ReturnCode.Continue;
            }

            Encoding encoding = StringOps.GetEncoding(
                EncodingType.Script);

            if (encoding == null)
            {
                IncrementGetDataCount(counts, 1, 1);
                return ReturnCode.Continue;
            }

            bool failOnError;
            bool ignoreCanRetry;

            ExtractBundleManagerScriptFlags(scriptFlags,
                out failOnError, out ignoreCanRetry);

            string path = null; /* REUSED */
            Result error; /* REUSED */
            string fileName = bundleManager.FileName;
            string fullName; /* REUSED */

            if (fileName == null)
            {
                path = name; /* NOTE: Database qualified? */
                error = null;

                if (!DataOps.VerifyBundlePath(
                        path, true, out fileName,
                        out fullName, ref error) ||
                    (bundleManager.Mount(
                        interpreter, fileName, null, false,
                        ref error) != ReturnCode.Ok))
                {
                    if (verbose && (error != null))
                    {
                        if (errors == null)
                            errors = new ResultList();

                        /* VERBOSE */
                        errors.Add(error);
                    }

                    IncrementGetDataCount(counts, 1, 1);
                    return ReturnCode.Continue;
                }
            }

            if (path == null)
            {
                //
                // HACK: If a script name being requested
                //       happens to be database qualified,
                //       ignore the mounted database file
                //       name.
                //
                string newFileName;

                path = name; /* NOTE: Database qualified? */
                error = null;

                if (DataOps.VerifyBundlePath(
                        path, true, out newFileName,
                        out fullName, ref error))
                {
                    fileName = newFileName;
                    name = fullName;
                }

                error = null;

                path = DataOps.BuildBundlePath(
                    fileName, name, true, ref error);

                if (path == null)
                {
                    if (verbose && (error != null))
                    {
                        if (errors == null)
                            errors = new ResultList();

                        /* VERBOSE */
                        errors.Add(error);
                    }

                    IncrementGetDataCount(counts, 1, 1);
                    return ReturnCode.Continue;
                }
            }

            byte[] data = null;

            error = null;

            if (bundleManager.GetData(
                    interpreter, cultureInfo, encoding, path,
                    ref data, ref error) != ReturnCode.Ok)
            {
                if (verbose && (error != null))
                {
                    if (errors == null)
                        errors = new ResultList();

                    /* VERBOSE */
                    errors.Add(error);
                }

                IncrementGetDataCount(counts, 1, 1);
                return ReturnCode.Continue;
            }

            if (FlagOps.HasFlags(dataFlags, DataFlags.Bytes, true))
            {
                scriptFlags |= ScriptFlags.ClientData;

                clientData = new GetScriptClientData(
                    null, name, null, null, new ByteList(data),
                    !verbose, bundleManager, "GetBundle", path,
                    isolated);

                result = data;

                IncrementGetDataCount(counts, 1, 1);
                return ReturnCode.Ok;
            }
            else if (FlagOps.HasFlags(dataFlags, DataFlags.Text, true))
            {
                string text = encoding.GetString(data);

                using (StringReader stringReader = new StringReader(text))
                {
                    string originalText = null;
                    bool canRetry = false;

                    error = null;

                    if (_Engine.ReadScriptStream(
                            interpreter, name, stringReader,
                            0, Count.Invalid, ref engineFlags,
                            ref originalText, ref text,
                            ref canRetry,
                            ref error) == ReturnCode.Ok)
                    {
                        scriptFlags |= ScriptFlags.ClientData;

                        clientData = new GetScriptClientData(
                            null, name, originalText, text,
                            null, !verbose, bundleManager,
                            "GetBundle", path, isolated);

                        result = text;

                        IncrementGetDataCount(counts, 1, 1);
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
                        else if (!ignoreCanRetry && !canRetry)
                            return ReturnCode.Error;
                    }
                }
            }

            IncrementGetDataCount(counts, 1, 1);
            return ReturnCode.Continue;
        }
#endif

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Attempts to satisfy the data request using the specified plugin,
        /// trying each of the unique candidate resource names (both
        /// plugin-qualified and raw) as a stream or string.  Invalid and static
        /// system plugins are skipped.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter associated with the request.
        /// </param>
        /// <param name="name">
        /// The name of the requested data.
        /// </param>
        /// <param name="plugin">
        /// The plugin to query; may be null.
        /// </param>
        /// <param name="uniqueResourceNames">
        /// The dictionary of unique candidate resource names to try.
        /// </param>
        /// <param name="cultureInfo">
        /// The culture to use when retrieving the data.
        /// </param>
        /// <param name="engineFlags">
        /// The engine flags to use when reading the script text.
        /// </param>
        /// <param name="dataFlags">
        /// The data flags for the request, which indicate whether bytes or text
        /// are wanted.
        /// </param>
        /// <param name="verbose">
        /// Non-zero to collect verbose error information.
        /// </param>
        /// <param name="isolated">
        /// Non-zero if this host is running isolated.
        /// </param>
        /// <param name="counts">
        /// The array of tracking counters to update.
        /// </param>
        /// <param name="scriptFlags">
        /// The script flags for the request, updated to indicate that client
        /// data is present.
        /// </param>
        /// <param name="clientData">
        /// Upon success, the client data describing the retrieved data.
        /// </param>
        /// <param name="result">
        /// Upon success, the retrieved bytes or text.
        /// </param>
        /// <param name="errors">
        /// The list of errors encountered while searching.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> if the data was found;
        /// <see cref="ReturnCode.Error" /> on a fatal error;
        /// <see cref="ReturnCode.Continue" /> to keep searching other sources.
        /// </returns>
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
                            IncrementGetDataCount(counts, 6, 1);

                            Result error = null;

                            resourceStream = plugin.GetStream(
                                interpreter, pluginUniqueResourceName,
                                cultureInfo, ref error); /* throw */

                            IncrementGetDataCount(counts, 7, 1);

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

                                    scriptFlags |= ScriptFlags.ClientData;

                                    clientData = new GetScriptClientData(
                                        null, name, null, null, new ByteList(
                                        bytes), !verbose, plugin, "GetStream",
                                        pluginUniqueResourceName, isolated);

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
                            IncrementGetDataCount(counts, 6, 1);

                            Result error = null;

                            resourceStream = plugin.GetStream(
                                interpreter, uniqueResourceName,
                                cultureInfo, ref error); /* throw */

                            IncrementGetDataCount(counts, 7, 1);

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

                                scriptFlags |= ScriptFlags.ClientData;

                                clientData = new GetScriptClientData(
                                    null, name, null, null, new ByteList(
                                    bytes), !verbose, plugin, "GetStream",
                                    uniqueResourceName, isolated);

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
                            IncrementGetDataCount(counts, 6, 1);

                            Result error = null;

                            resourceValue = plugin.GetString(
                                interpreter, pluginUniqueResourceName,
                                cultureInfo, ref error); /* throw */

                            IncrementGetDataCount(counts, 7, 1);

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
                                string originalText = null;
                                string text = null;
                                bool canRetry = false;
                                Result error = null;

                                if (_Engine.ReadScriptStream(
                                        interpreter, name, stringReader,
                                        0, Count.Invalid, ref engineFlags,
                                        ref originalText, ref text,
                                        ref canRetry,
                                        ref error) == ReturnCode.Ok)
                                {
                                    scriptFlags |= ScriptFlags.ClientData;

                                    clientData = new GetScriptClientData(
                                        null, name, originalText, text,
                                        null, !verbose, plugin, "GetString",
                                        pluginUniqueResourceName, isolated);

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
                            IncrementGetDataCount(counts, 6, 1);

                            Result error = null;

                            resourceValue = plugin.GetString(
                                interpreter, uniqueResourceName,
                                cultureInfo, ref error); /* throw */

                            IncrementGetDataCount(counts, 7, 1);

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
                            string originalText = null;
                            string text = null;
                            bool canRetry = false;
                            Result error = null;

                            if (_Engine.ReadScriptStream(
                                    interpreter, name, stringReader,
                                    0, Count.Invalid, ref engineFlags,
                                    ref originalText, ref text,
                                    ref canRetry,
                                    ref error) == ReturnCode.Ok)
                            {
                                scriptFlags |= ScriptFlags.ClientData;

                                clientData = new GetScriptClientData(
                                    null, name, originalText, text,
                                    null, !verbose, plugin, "GetString",
                                    uniqueResourceName, isolated);

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
        /// <summary>
        /// Attempts to satisfy the data request using the specified resource
        /// manager, trying each of the unique candidate resource names as a
        /// stream and, failing that, as a string.  The "internal" use is
        /// designed for the HostOps.GetScript method only.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter associated with the request.
        /// </param>
        /// <param name="name">
        /// The name of the requested data.
        /// </param>
        /// <param name="resourceManagerAnyPair">
        /// A pair containing the resource manager to query and an associated
        /// location string; may be null.
        /// </param>
        /// <param name="uniqueResourceNames">
        /// The dictionary of unique candidate resource names to try.
        /// </param>
        /// <param name="engineFlags">
        /// The engine flags to use when reading the script text.
        /// </param>
        /// <param name="dataFlags">
        /// The data flags for the request, which indicate whether bytes or text
        /// are wanted.
        /// </param>
        /// <param name="verbose">
        /// Non-zero to collect verbose error information.
        /// </param>
        /// <param name="isolated">
        /// Non-zero if this host is running isolated.
        /// </param>
        /// <param name="counts">
        /// The array of tracking counters to update.
        /// </param>
        /// <param name="scriptFlags">
        /// The script flags for the request, updated to indicate that client
        /// data is present.
        /// </param>
        /// <param name="clientData">
        /// Upon success, the client data describing the retrieved data.
        /// </param>
        /// <param name="result">
        /// Upon success, the retrieved bytes or text.
        /// </param>
        /// <param name="errors">
        /// The list of errors encountered while searching.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> if the data was found;
        /// <see cref="ReturnCode.Error" /> on a fatal error;
        /// <see cref="ReturnCode.Continue" /> to keep searching other sources.
        /// </returns>
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
                        IncrementGetDataCount(counts, 8, 1);

                        Stream resourceStream = resourceManager.GetStream(
                            uniqueResourceName); /* throw */

                        IncrementGetDataCount(counts, 9, 1);

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

                                    scriptFlags |= ScriptFlags.ClientData;

                                    clientData = new GetScriptClientData(
                                        null, name, null, null, new ByteList(
                                        bytes), !verbose, resourceManagerAnyPair.X,
                                        MaybeGetResourceManager(
                                            resourceManager, isolated),
                                        "GetStream", uniqueResourceName,
                                        isolated);

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
                            IncrementGetDataCount(counts, 8, 1);

                            Stream resourceStream = resourceManager.GetStream(
                                uniqueResourceName); /* throw */

                            IncrementGetDataCount(counts, 9, 1);

                            //
                            // NOTE: In order to continue, we must have the found the
                            //       resource stream associated with the named resource.
                            //
                            if (resourceStream != null)
                            {
                                using (StreamReader streamReader =
                                        new StreamReader(resourceStream)) /* throw */
                                {
                                    string originalText = null;
                                    string text = null;
                                    bool canRetry = false;
                                    Result error = null;

                                    if (_Engine.ReadScriptStream(
                                            interpreter, name, streamReader,
                                            0, Count.Invalid, ref engineFlags,
                                            ref originalText, ref text,
                                            ref canRetry,
                                            ref error) == ReturnCode.Ok)
                                    {
                                        scriptFlags |= ScriptFlags.ClientData;

                                        clientData = new GetScriptClientData(
                                            null, name, originalText, text,
                                            null, !verbose, resourceManagerAnyPair.X,
                                            MaybeGetResourceManager(
                                                resourceManager, isolated),
                                            "GetStream", uniqueResourceName,
                                            isolated);

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
                            IncrementGetDataCount(counts, 8, 1);

                            resourceValue = resourceManager.GetString(
                                uniqueResourceName); /* throw */

                            IncrementGetDataCount(counts, 9, 1);
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
                                string originalText = null;
                                string text = null;
                                bool canRetry = false;
                                Result error = null;

                                if (_Engine.ReadScriptStream(
                                        interpreter, name, stringReader,
                                        0, Count.Invalid, ref engineFlags,
                                        ref originalText, ref text,
                                        ref canRetry,
                                        ref error) == ReturnCode.Ok)
                                {
                                    scriptFlags |= ScriptFlags.ClientData;

                                    clientData = new GetScriptClientData(
                                        null, name, originalText, text,
                                        null, !verbose, resourceManagerAnyPair.X,
                                        MaybeGetResourceManager(
                                            resourceManager, isolated),
                                        "GetString", uniqueResourceName,
                                        isolated);

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
        /// <summary>
        /// Attempts to satisfy the data request using the manifest resources of
        /// the specified assembly, trying each of the unique candidate resource
        /// names as a manifest resource stream.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter associated with the request.
        /// </param>
        /// <param name="name">
        /// The name of the requested data.
        /// </param>
        /// <param name="assembly">
        /// The assembly whose manifest resources should be queried; may be null.
        /// </param>
        /// <param name="uniqueResourceNames">
        /// The dictionary of unique candidate resource names to try.
        /// </param>
        /// <param name="engineFlags">
        /// The engine flags to use when reading the script text.
        /// </param>
        /// <param name="dataFlags">
        /// The data flags for the request, which indicate whether bytes or text
        /// are wanted.
        /// </param>
        /// <param name="verbose">
        /// Non-zero to collect verbose error information.
        /// </param>
        /// <param name="isolated">
        /// Non-zero if this host is running isolated.
        /// </param>
        /// <param name="counts">
        /// The array of tracking counters to update.
        /// </param>
        /// <param name="scriptFlags">
        /// The script flags for the request, updated to indicate that client
        /// data is present.
        /// </param>
        /// <param name="clientData">
        /// Upon success, the client data describing the retrieved data.
        /// </param>
        /// <param name="result">
        /// Upon success, the retrieved bytes or text.
        /// </param>
        /// <param name="errors">
        /// The list of errors encountered while searching.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> if the data was found;
        /// <see cref="ReturnCode.Error" /> on a fatal error;
        /// <see cref="ReturnCode.Continue" /> to keep searching other sources.
        /// </returns>
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
                        IncrementGetDataCount(counts, 10, 1);

                        Stream resourceStream = assembly.GetManifestResourceStream(
                            uniqueResourceName); /* throw */

                        IncrementGetDataCount(counts, 11, 1);

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

                                    scriptFlags |= ScriptFlags.ClientData;

                                    clientData = new GetScriptClientData(
                                        null, name, null, null, new ByteList(
                                        bytes), !verbose, MaybeGetAssembly(
                                            assembly, isolated), "GetStream",
                                        uniqueResourceName, isolated);

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
                        IncrementGetDataCount(counts, 10, 1);

                        Stream resourceStream = assembly.GetManifestResourceStream(
                            uniqueResourceName); /* throw */

                        IncrementGetDataCount(counts, 11, 1);

                        //
                        // NOTE: In order to continue, we must have the found the
                        //       resource stream associated with the named resource.
                        //
                        if (resourceStream != null)
                        {
                            using (StreamReader streamReader =
                                    new StreamReader(resourceStream)) /* throw */
                            {
                                string originalText = null;
                                string text = null;
                                bool canRetry = false;
                                Result error = null;

                                if (_Engine.ReadScriptStream(
                                        interpreter, name, streamReader,
                                        0, Count.Invalid, ref engineFlags,
                                        ref originalText, ref text,
                                        ref canRetry,
                                        ref error) == ReturnCode.Ok)
                                {
                                    scriptFlags |= ScriptFlags.ClientData;

                                    clientData = new GetScriptClientData(
                                        null, name, originalText, text,
                                        null, !verbose, MaybeGetAssembly(
                                            assembly, isolated), "GetStream",
                                        uniqueResourceName, isolated);

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
        /// <summary>
        /// Resets only the cached host flags for this class so that they will be
        /// recomputed the next time they are requested.
        /// </summary>
        private void PrivateResetHostFlagsOnly()
        {
            hostFlags = HostFlags.Invalid;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Resets the cached host flags for this class and then resets the host
        /// flags in the base class.
        /// </summary>
        /// <returns>
        /// True if the base class host flags were reset; otherwise, false.
        /// </returns>
        private bool PrivateResetHostFlags()
        {
            PrivateResetHostFlagsOnly();
            return base.ResetHostFlags();
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Computes and caches the host flags for this class, if they have not
        /// already been computed, adding the flags indicating support for the
        /// "GetStream" and "GetData" methods (and the isolated flag, when
        /// applicable) to those provided by the base class.
        /// </summary>
        /// <returns>
        /// The host flags for this host.
        /// </returns>
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

        /// <summary>
        /// Sets whether a read exception has occurred, also resetting the cached
        /// host flags for this class.
        /// </summary>
        /// <param name="exception">
        /// Non-zero if a read exception has occurred.
        /// </param>
        protected override void SetReadException(
            bool exception
            )
        {
            base.SetReadException(exception);
            PrivateResetHostFlagsOnly();
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Sets whether a write exception has occurred, also resetting the
        /// cached host flags for this class.
        /// </summary>
        /// <param name="exception">
        /// Non-zero if a write exception has occurred.
        /// </param>
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
        /// <summary>
        /// The interpreter that owns this host.
        /// </summary>
        private Interpreter interpreter;

        /// <summary>
        /// Gets or sets the interpreter that owns this host.
        /// </summary>
        public virtual Interpreter Interpreter
        {
            get { CheckDisposed(); return interpreter; }
            set { CheckDisposed(); interpreter = value; }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////////////////////////////

        #region Protected Properties
        /// <summary>
        /// The assembly whose embedded resources are searched for scripts.
        /// </summary>
        private Assembly resourceAssembly;

        /// <summary>
        /// Gets or sets the assembly whose embedded resources are searched for
        /// scripts.  Setting this property also rebuilds the cached set of
        /// manifest resource names.
        /// </summary>
        protected virtual Assembly ResourceAssembly
        {
            get { return resourceAssembly; }
            set { SetupResourceNames(value); resourceAssembly = value; }
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// The cached set of manifest resource names present in the resource
        /// assembly.
        /// </summary>
        private StringDictionary resourceNames;

        /// <summary>
        /// Gets or sets the cached set of manifest resource names present in the
        /// resource assembly.
        /// </summary>
        protected virtual StringDictionary ResourceNames
        {
            get { return resourceNames; }
            set { resourceNames = value; }
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// The base name for the resource manager that contains the embedded
        /// core script library.
        /// </summary>
        private string libraryResourceBaseName;

        /// <summary>
        /// Gets or sets the base name for the resource manager that contains the
        /// embedded core script library.
        /// </summary>
        protected internal virtual string LibraryResourceBaseName
        {
            get { return libraryResourceBaseName; }
            set { libraryResourceBaseName = value; }
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// The resource manager that contains the embedded core script library.
        /// </summary>
        private ResourceManager libraryResourceManager;

        /// <summary>
        /// Gets the resource manager that contains the embedded core script
        /// library.
        /// </summary>
        protected internal virtual ResourceManager LibraryResourceManager
        {
            get { return libraryResourceManager; }
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// The base name for the resource manager that contains the embedded
        /// core script packages.
        /// </summary>
        private string packagesResourceBaseName;

        /// <summary>
        /// Gets or sets the base name for the resource manager that contains the
        /// embedded core script packages.
        /// </summary>
        protected internal virtual string PackagesResourceBaseName
        {
            get { return packagesResourceBaseName; }
            set { packagesResourceBaseName = value; }
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// The resource manager that contains the embedded core script
        /// packages.
        /// </summary>
        private ResourceManager packagesResourceManager;

        /// <summary>
        /// Gets the resource manager that contains the embedded core script
        /// packages.
        /// </summary>
        protected internal virtual ResourceManager PackagesResourceManager
        {
            get { return packagesResourceManager; }
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// The base name for the resource manager that contains the embedded
        /// kit packages.
        /// </summary>
        private string kitResourceBaseName;

        /// <summary>
        /// Gets or sets the base name for the resource manager that contains the
        /// embedded kit packages.
        /// </summary>
        protected internal virtual string KitResourceBaseName
        {
            get { return kitResourceBaseName; }
            set { kitResourceBaseName = value; }
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// The resource manager that contains the embedded kit packages.
        /// </summary>
        private ResourceManager kitResourceManager;

        /// <summary>
        /// Gets the resource manager that contains the embedded kit packages.
        /// </summary>
        protected internal virtual ResourceManager KitResourceManager
        {
            get { return kitResourceManager; }
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// The base name for the resource manager that contains the embedded
        /// application-specific (vendor) packages.
        /// </summary>
        private string applicationResourceBaseName;

        /// <summary>
        /// Gets or sets the base name for the resource manager that contains the
        /// embedded application-specific (vendor) packages.
        /// </summary>
        protected internal virtual string ApplicationResourceBaseName
        {
            get { return applicationResourceBaseName; }
            set { applicationResourceBaseName = value; }
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// The resource manager that contains the embedded application-specific
        /// (vendor) packages.
        /// </summary>
        private ResourceManager applicationResourceManager;

        /// <summary>
        /// Gets the resource manager that contains the embedded
        /// application-specific (vendor) packages.
        /// </summary>
        protected internal virtual ResourceManager ApplicationResourceManager
        {
            get { return applicationResourceManager; }
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// The customizable resource manager associated with this host, as
        /// provided by a custom IHost implementation.
        /// </summary>
        private ResourceManager resourceManager;

        /// <summary>
        /// Gets the customizable resource manager associated with this host, as
        /// provided by a custom IHost implementation.
        /// </summary>
        protected internal virtual ResourceManager ResourceManager
        {
            get { return resourceManager; }
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// The additional script flags that are merged into every data request
        /// handled by this host.
        /// </summary>
        private ScriptFlags libraryScriptFlags;

        /// <summary>
        /// Gets or sets the additional script flags that are merged into every
        /// data request handled by this host.
        /// </summary>
        protected internal virtual ScriptFlags LibraryScriptFlags
        {
            get { return libraryScriptFlags; }
            set { libraryScriptFlags = value; }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////////////////////////////

        #region Script Resource Support
        /// <summary>
        /// Sets the resource assembly to the specified assembly and caches its
        /// manifest resource names.
        /// </summary>
        /// <param name="assembly">
        /// The assembly to use as the resource assembly; may be null.
        /// </param>
        /// <returns>
        /// True if the resource assembly was set; otherwise, false.
        /// </returns>
        private bool SetupResourceAssembly(
            Assembly assembly
            )
        {
            if ((assembly != null) && SetupResourceNames(assembly))
            {
                resourceAssembly = assembly;
                return true;
            }

            return false;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Rebuilds the cached set of manifest resource names from the specified
        /// assembly.
        /// </summary>
        /// <param name="assembly">
        /// The assembly whose manifest resource names should be cached; may be
        /// null.
        /// </param>
        /// <returns>
        /// True if the resource names were cached; otherwise, false.
        /// </returns>
        private bool SetupResourceNames(
            Assembly assembly
            )
        {
            if (assembly == null)
                return false;

            string assemblyString = assembly.ToString();

            resourceNames = new StringDictionary();

            foreach (string name in assembly.GetManifestResourceNames())
                resourceNames[name] = assemblyString;

            return true;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Determines whether a manifest resource corresponding to the specified
        /// resource base name is present in the resource assembly.
        /// </summary>
        /// <param name="resourceBaseName">
        /// The resource base name to check for.
        /// </param>
        /// <returns>
        /// True if the corresponding manifest resource is present; otherwise,
        /// false.
        /// </returns>
        private bool HaveResourceBaseName(
            string resourceBaseName
            )
        {
            if (String.IsNullOrEmpty(resourceBaseName))
                return false;

            if (resourceNames == null)
                return false;

            string resourceName = String.Format("{0}{1}",
                resourceBaseName, DefaultResourceNameSuffix);

            return resourceNames.ContainsKey(resourceName);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Creates and verifies the resource manager for the embedded core
        /// script library, if the corresponding manifest resource is present.
        /// Any exception is caught and traced, and the resource manager is
        /// cleared if it does not appear to work.
        /// </summary>
        /// <returns>
        /// True if the library resource manager was created and is functional;
        /// otherwise, false.
        /// </returns>
        private bool SetupLibraryResourceManager()
        {
            try
            {
                string resourceBaseName = LibraryResourceBaseName;

                if (HaveResourceBaseName(resourceBaseName))
                {
                    //
                    // NOTE: Create a resource manager for the embedded core
                    //       script library, if any.
                    //
                    libraryResourceManager = new ResourceManager(
                        resourceBaseName, ResourceAssembly);

                    //
                    // NOTE: Now, since creating it will pretty much always
                    //       succeed, we need to test it to make sure it is
                    //       really available.
                    //
                    /* IGNORED */
                    libraryResourceManager.GetString(
                        NotFoundResourceName); /* throw */

                    //
                    // NOTE: If we get this far, the resource manager is
                    //       created and functional.
                    //
                    return true;
                }
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

        /// <summary>
        /// Creates and verifies the resource manager for the embedded core
        /// script packages, if the corresponding manifest resource is present.
        /// Any exception is caught and traced, and the resource manager is
        /// cleared if it does not appear to work.
        /// </summary>
        /// <returns>
        /// True if the packages resource manager was created and is functional;
        /// otherwise, false.
        /// </returns>
        private bool SetupPackagesResourceManager()
        {
            try
            {
                string resourceBaseName = PackagesResourceBaseName;

                if (HaveResourceBaseName(resourceBaseName))
                {
                    //
                    // NOTE: Create a resource manager for the embedded core
                    //       script packages, if any.
                    //
                    packagesResourceManager = new ResourceManager(
                        resourceBaseName, ResourceAssembly);

                    //
                    // NOTE: Now, since creating it will pretty much always
                    //       succeed, we need to test it to make sure it is
                    //       really available.
                    //
                    /* IGNORED */
                    packagesResourceManager.GetString(
                        NotFoundResourceName); /* throw */

                    //
                    // NOTE: If we get this far, the resource manager is
                    //       created and functional.
                    //
                    return true;
                }
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

        /// <summary>
        /// Creates and verifies the resource manager for the embedded kit
        /// packages, if the corresponding manifest resource is present.  Any
        /// exception is caught and traced, and the resource manager is cleared
        /// if it does not appear to work.
        /// </summary>
        /// <returns>
        /// True if the kit resource manager was created and is functional;
        /// otherwise, false.
        /// </returns>
        private bool SetupKitResourceManager()
        {
            try
            {
                string resourceBaseName = KitResourceBaseName;

                if (HaveResourceBaseName(resourceBaseName))
                {
                    //
                    // NOTE: Create a resource manager for the embedded kit
                    //       packages, if any.
                    //
                    kitResourceManager = new ResourceManager(
                        resourceBaseName, ResourceAssembly);

                    //
                    // NOTE: Now, since creating it will pretty much always
                    //       succeed, we need to test it to make sure it is
                    //       really available.
                    //
                    /* IGNORED */
                    kitResourceManager.GetString(
                        NotFoundResourceName); /* throw */

                    //
                    // NOTE: If we get this far, the resource manager is
                    //       created and functional.
                    //
                    return true;
                }
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
                if (kitResourceManager != null)
                    kitResourceManager = null;
            }

            return false;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Copies this host's application resource manager into the shared,
        /// per-application-domain cache.
        /// </summary>
        /// <returns>
        /// True if a non-null application resource manager was cached;
        /// otherwise, false.
        /// </returns>
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

        /// <summary>
        /// Copies the shared, per-application-domain cached application resource
        /// manager into this host.
        /// </summary>
        /// <returns>
        /// True if a non-null cached application resource manager was copied;
        /// otherwise, false.
        /// </returns>
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

        /// <summary>
        /// Creates and verifies the resource manager for the embedded
        /// application-specific (vendor) packages, if the corresponding manifest
        /// resource is present.  Any exception is caught (and traced when
        /// compiled with verbose tracing), and the resource manager is cleared
        /// if it does not appear to work.
        /// </summary>
        /// <returns>
        /// True if the application resource manager was created and is
        /// functional; otherwise, false.
        /// </returns>
        private bool SetupApplicationResourceManager()
        {
            try
            {
                string resourceBaseName = ApplicationResourceBaseName;

                if (HaveResourceBaseName(resourceBaseName))
                {
                    //
                    // NOTE: Create a resource manager for the embedded vendor
                    //       packages, if any.
                    //
                    applicationResourceManager = new ResourceManager(
                        resourceBaseName, ResourceAssembly);

                    //
                    // NOTE: Now, since creating it will pretty much always
                    //       succeed, we need to test it to make sure it is
                    //       really available.
                    //
                    /* IGNORED */
                    applicationResourceManager.GetString(
                        NotFoundResourceName); /* throw */

                    //
                    // NOTE: If we get this far, the resource manager is
                    //       created and functional.
                    //
                    return true;
                }
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
        /// <summary>
        /// The cached host flags for this host, or the invalid value if they
        /// have not yet been computed.
        /// </summary>
        private HostFlags hostFlags = HostFlags.Invalid;

        /// <summary>
        /// Gets the host flags for this host, computing and caching them if
        /// necessary.
        /// </summary>
        /// <returns>
        /// The host flags for this host.
        /// </returns>
        public override HostFlags GetHostFlags()
        {
            CheckDisposed();

            return MaybeInitializeHostFlags();
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////////////////////////////

        #region IFileSystemHost Members
        /// <summary>
        /// Opens a stream for the specified file on behalf of the script engine,
        /// catching any exception thrown while doing so.
        /// </summary>
        /// <param name="path">
        /// The path of the file to open.
        /// </param>
        /// <param name="mode">
        /// The file mode that specifies how the file should be opened.
        /// </param>
        /// <param name="access">
        /// The file access that specifies the operations permitted on the file.
        /// </param>
        /// <param name="share">
        /// The file share that specifies how the file may be shared with others.
        /// </param>
        /// <param name="bufferSize">
        /// The size, in bytes, of the stream buffer.
        /// </param>
        /// <param name="options">
        /// The file options to use when opening the file.
        /// </param>
        /// <param name="hostStreamFlags">
        /// On input, the requested host stream flags; upon return, the effective
        /// host stream flags.
        /// </param>
        /// <param name="fullPath">
        /// Upon success, the full path of the opened file.
        /// </param>
        /// <param name="stream">
        /// Upon success, the opened stream.
        /// </param>
        /// <param name="error">
        /// Upon failure, the error message.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success;
        /// <see cref="ReturnCode.Error" /> on failure.
        /// </returns>
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

        /// <summary>
        /// Locates and retrieves the named data (typically a library script) on
        /// behalf of the script engine, redirecting requests to the host's
        /// internal sources so the scripts need not exist elsewhere on the file
        /// system.  The bundle manager, snippet manager, file system, loaded
        /// plugins, the various resource managers, and finally the assembly
        /// manifest are searched in order.
        /// </summary>
        /// <param name="name">
        /// The name of the requested data.
        /// </param>
        /// <param name="dataFlags">
        /// The data flags for the request, which control the search behavior and
        /// whether bytes or text are wanted.
        /// </param>
        /// <param name="scriptFlags">
        /// On input, the script flags for the request; upon return, updated to
        /// reflect how and from where the data was obtained.
        /// </param>
        /// <param name="clientData">
        /// Upon success, the client data describing the retrieved data, if any.
        /// </param>
        /// <param name="result">
        /// Upon success, the retrieved data; upon failure, error information.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> if the data was found;
        /// <see cref="ReturnCode.Error" /> otherwise.
        /// </returns>
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
            // NOTE: Permit key parameters to be customized by derived classes
            //       as as well with the configured core script flags, if any.
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
            // NOTE: Check if requested data name is allowed.  If not, then
            //       return an error now.
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
            // NOTE: Otherwise, if script name appears to be a file name with
            //       no directory information -AND- script name is reserved by
            //       the host (e.g. "pkgIndex.eagle"), issue a warning now.
            //
            else if (IsReservedDataName(
                    localInterpreter, name, ref dataFlags, scriptFlags,
                    clientData)) /* HOOK */
            {
                bool exists = false;

                if (IsFileNameOnlyDataName(name, ref dataFlags))
                {
                    GetDataTrace(localInterpreter,
                        "WARNING: detected reserved script name without directory",
                        name, dataFlags, scriptFlags, clientData, ReturnCode.Ok,
                        result);
                }
                else if (!IsAbsoluteFileNameDataName(
                        name, ref dataFlags, ref exists) && !exists)
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
            // NOTE: Are we operating in the "quiet" error handling mode?
            //
            bool quiet = FlagOps.HasFlags(dataFlags, DataFlags.Quiet, true);

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
                false, false, false, false, false, false, false, false,
                false, false, false, false
            };

            //
            // NOTE: These are the tracking counts for how many tries were
            //       performed using the file system, plugins, and resource
            //       managers.
            //
            int[] counts = { 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 };

            //
            // NOTE: This is the list of errors encountered during the
            //       search for the requested script.
            //
            ReturnCode code; /* REUSED */
            ResultList errors = null;

            ///////////////////////////////////////////////////////////////////

            //
            // NOTE: Are we operating in the "verbose" error handling mode?
            //
            bool verbose = FlagOps.HasFlags(dataFlags, DataFlags.Verbose, true);

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

            CultureInfo cultureInfo = localInterpreter.InternalCultureInfo;

            ///////////////////////////////////////////////////////////////////

            EngineFlags engineFlags = GetEngineFlagsForReadScriptStream(
                localInterpreter, dataFlags, scriptFlags);

            ///////////////////////////////////////////////////////////////////

#if DATA
            //
            // HACK: *SECURITY* Always check via IBundleManager interface
            //       first.  Also, this behavior CANNOT be disabled.
            //
            {
                @checked[0] = true;

                code = GetDataViaBundleManager(
                    localInterpreter, name, cultureInfo, engineFlags,
                    dataFlags, verbose, isolated, counts, ref scriptFlags,
                    ref clientData, ref result, ref errors);

                if ((code == ReturnCode.Ok) ||
                    (code == ReturnCode.Error))
                {
                    GetDataTrace(
                        localInterpreter,
                        "exited, via bundle manager",
                        name, dataFlags, scriptFlags,
                        clientData, ReturnCode.Ok,
                        result);

                    return code;
                }
            }
#endif

            ///////////////////////////////////////////////////////////////////

            //
            // HACK: *SECURITY* Always check via ISnippetManager interface
            //       second.  Also, this behavior CANNOT be disabled.
            //
            {
                @checked[1] = true;

                code = GetDataViaSnippetManager(
                    localInterpreter, name, dataFlags, counts,
                    verbose, isolated, ref scriptFlags,
                    ref clientData, ref result, ref errors);

                if ((code == ReturnCode.Ok) ||
                    (code == ReturnCode.Error))
                {
                    GetDataTrace(
                        localInterpreter,
                        "exited, via snippet manager",
                        name, dataFlags, scriptFlags,
                        clientData, ReturnCode.Ok,
                        result);

                    return code;
                }
            }

            ///////////////////////////////////////////////////////////////////

            //
            // NOTE: First, if it has not been prohibited by the caller,
            //       try to get the requested script externally, using
            //       our standard file system search routine.
            //
            if (!FlagOps.HasFlags(
                    scriptFlags, ScriptFlags.NoFileSystem, true))
            {
                @checked[2] = true;

                code = GetDataViaFileSystem(
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
                        @checked[3] = true;

                        foreach (PluginPair pair in plugins)
                        {
                            IPlugin plugin = pair.Value;

                            //
                            // NOTE: This method *MUST* return
                            //       "ReturnCode.Continue" in
                            //       order to keep searching.
                            //
                            code = GetDataViaPlugin(
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
                        @checked[4] = true;

                    ResourceManager applicationResourceManager = !FlagOps.HasFlags(
                        scriptFlags, ScriptFlags.NoApplicationResourceManager, true) ?
                            this.ApplicationResourceManager : null;

                    if (applicationResourceManager != null)
                        @checked[5] = true;

                    ResourceManager libraryResourceManager = !FlagOps.HasFlags(
                        scriptFlags, ScriptFlags.NoLibraryResourceManager, true) ?
                            this.LibraryResourceManager : null;

                    if (libraryResourceManager != null)
                        @checked[6] = true;

                    ResourceManager packagesResourceManager = !FlagOps.HasFlags(
                        scriptFlags, ScriptFlags.NoPackagesResourceManager, true) ?
                            this.PackagesResourceManager : null;

                    if (packagesResourceManager != null)
                        @checked[7] = true;

                    ResourceManager kitResourceManager = !FlagOps.HasFlags(
                        scriptFlags, ScriptFlags.NoKitResourceManager, true) ?
                            this.KitResourceManager : null;

                    if (kitResourceManager != null)
                        @checked[8] = true;

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
                        @checked[9] = true;

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
                            kitResourceManager),
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
                        code = GetDataViaResourceManager(
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
                    Assembly assembly = ResourceAssembly;

                    if (assembly != null)
                        @checked[10] = true;

                    //
                    // NOTE: This method *MUST* return
                    //       "ReturnCode.Continue" in
                    //       order to keep searching.
                    //
                    code = GetDataViaAssemblyManifest(
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

            ///////////////////////////////////////////////////////////////////

            //
            // NOTE: In quiet mode, skip the other error information.
            //
            if (!quiet)
            {
                if (!@checked[0])
                    /* NOT VERBOSE */
                    errors.Add("skipped bundle manager");

                if (counts[0] == 0)
                    /* NOT VERBOSE */
                    errors.Add("no bundles were checked");

                if (counts[0] != counts[1])
                    /* NOT VERBOSE */
                    errors.Add("error while checking bundles");

                if (!@checked[1])
                    /* NOT VERBOSE */
                    errors.Add("skipped snippet manager");

                if (counts[2] == 0)
                    /* NOT VERBOSE */
                    errors.Add("no snippets were checked");

                if (counts[2] != counts[3])
                    /* NOT VERBOSE */
                    errors.Add("error while checking snippets");

                if (!@checked[2])
                    /* NOT VERBOSE */
                    errors.Add("skipped file system");

                if (counts[4] == 0)
                    /* NOT VERBOSE */
                    errors.Add("no files were checked");

                if (counts[4] != counts[5])
                    /* NOT VERBOSE */
                    errors.Add("error while checking files");

                if (!@checked[3])
                    /* NOT VERBOSE */
                    errors.Add("skipped plugin list");

                if (counts[6] == 0)
                    /* NOT VERBOSE */
                    errors.Add("no plugins were queried");

                if (counts[6] != counts[7])
                    /* NOT VERBOSE */
                    errors.Add("error while querying plugins");

                if (!@checked[4])
                    /* NOT VERBOSE */
                    errors.Add("skipped extension resource manager");

                if (!@checked[5])
                    /* NOT VERBOSE */
                    errors.Add("skipped application resource manager");

                if (!@checked[6])
                    /* NOT VERBOSE */
                    errors.Add("skipped library resource manager");

                if (!@checked[7])
                    /* NOT VERBOSE */
                    errors.Add("skipped packages resource manager");

                if (!@checked[8])
                    /* NOT VERBOSE */
                    errors.Add("skipped kit resource manager");

                if (!@checked[9])
                    /* NOT VERBOSE */
                    errors.Add("skipped interpreter resource manager");

                if (counts[8] == 0)
                    /* NOT VERBOSE */
                    errors.Add("no resource managers were queried");

                if (counts[8] != counts[9])
                    /* NOT VERBOSE */
                    errors.Add("error while querying resource managers");

                if (!@checked[10])
                    /* NOT VERBOSE */
                    errors.Add("skipped assembly manifest");

                if (counts[10] == 0)
                    /* NOT VERBOSE */
                    errors.Add("no assembly manifests were queried");

                if (counts[10] != counts[11])
                    /* NOT VERBOSE */
                    errors.Add("error while querying assembly manifests");
            }

            ///////////////////////////////////////////////////////////////////

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
        /// <summary>
        /// Resets the cached host flags for this host so that they will be
        /// recomputed the next time they are requested.
        /// </summary>
        /// <returns>
        /// True if the host flags were reset; otherwise, false.
        /// </returns>
        public override bool ResetHostFlags()
        {
            CheckDisposed();

            return PrivateResetHostFlags();
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Resets this host to its initial state, including resetting the base
        /// class and the cached host flags.
        /// </summary>
        /// <param name="error">
        /// Upon failure, the error message.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success;
        /// <see cref="ReturnCode.Error" /> on failure.
        /// </returns>
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
        /// <summary>
        /// Gets a value indicating whether this host has been disposed.
        /// </summary>
        public override bool Disposed
        {
            get { return disposed; }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////////////////////////////

        #region IDisposable "Pattern" Members
        /// <summary>
        /// Non-zero if this host has been disposed and is no longer usable.
        /// </summary>
        private bool disposed;

        /// <summary>
        /// Throws an exception if this host has been disposed and the
        /// interpreter is configured to throw on access to disposed objects.
        /// </summary>
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

        /// <summary>
        /// Releases the resources used by this host.
        /// </summary>
        /// <param name="disposing">
        /// Non-zero if this method is being called from the
        /// <c>Dispose()</c> method; zero if it is being called from the
        /// finalizer.
        /// </param>
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
