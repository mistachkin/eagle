/*
 * GlobalState.cs --
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
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Reflection;

#if REMOTING
using System.Runtime.Remoting;
#endif

#if CAS_POLICY
using System.Security.Policy;
#endif

using System.Text;
using System.Threading;
using Eagle._Attributes;
using Eagle._Components.Public;
using Eagle._Constants;
using Eagle._Containers.Private;
using Eagle._Containers.Public;
using Eagle._Interfaces.Public;
using SharedAttributeOps = Eagle._Components.Shared.AttributeOps;
using SharedStringOps = Eagle._Components.Shared.StringOps;

using TokenInterpreterDictionary = System.Collections.Generic.Dictionary<
    ulong, Eagle._Components.Public.Interpreter>;

using ActiveInterpreterPair = Eagle._Interfaces.Public.IAnyPair<
    Eagle._Components.Public.Interpreter, Eagle._Interfaces.Public.IClientData>;

using AllInterpreterPair = System.Collections.Generic.KeyValuePair<
    string, Eagle._Components.Public.Interpreter>;

using TokenInterpreterPair = System.Collections.Generic.KeyValuePair<
    ulong, Eagle._Components.Public.Interpreter>;

using EngineThreadList = System.Collections.Generic.List<
    Eagle._Components.Private.EngineThread>;

using PluginDataTriplet = Eagle._Components.Public.AnyTriplet<
    Eagle._Containers.Public.StringList, Eagle._Interfaces.Public.IPluginData, bool>;

using AutoPathDictionary = Eagle._Containers.Public.PathDictionary<
    Eagle._Components.Private.PathClientData>;

using PathClientDataPair = System.Collections.Generic.KeyValuePair<
    string, Eagle._Components.Private.PathClientData>;

using ThreadList = System.Collections.Generic.List<System.Threading.Thread>;

using InterpreterDictionaryCache = System.Collections.Generic.Dictionary<
    System.Threading.Thread, Eagle._Containers.Public.InterpreterDictionary>;

namespace Eagle._Components.Private
{
    /// <summary>
    /// This class provides the global, application-domain-wide state shared
    /// by the Eagle core library, including assembly and package identity,
    /// path overrides, interpreter and thread tracking, object identifier
    /// generation, and the cooperative locking used to serialize access to
    /// that state.
    /// </summary>
    [ObjectId("e8491fec-2fd3-455e-92fd-cf2a84c75e8a")]
    internal static class GlobalState
    {
        #region Private Read-Only Data (Logical Constants)
        /// <summary>
        /// The object used to synchronize access to the static state of this
        /// class.
        /// </summary>
        private static readonly object syncRoot = new object();

        ///////////////////////////////////////////////////////////////////////

        #region Application Domain Data
        /// <summary>
        /// The application domain that this class is associated with (i.e. the
        /// one in which it was first loaded).
        /// </summary>
        private static readonly AppDomain appDomain = AppDomain.CurrentDomain;

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// The base directory of the application domain that this class is
        /// associated with, or null if it is not available.
        /// </summary>
        private static readonly string appDomainBaseDirectory =
            (appDomain != null) ? appDomain.BaseDirectory : null;

        ///////////////////////////////////////////////////////////////////////

#if USE_APPDOMAIN_FOR_ID
        //
        // NOTE: Normally, zero would be used here; however, Mono appears
        //       to use zero for the default application domain; therefore,
        //       we must use a negative value here.
        //
        // NOTE: The value used here *MUST* be manually kept in sync with
        //       the value of the AppDomainOps.InvalidId static read-only
        //       field.
        //
        /// <summary>
        /// The identifier of the application domain that this class is
        /// associated with, or negative one if it is not available.
        /// </summary>
        private static readonly int appDomainId = (appDomain != null) ?
            appDomain.Id : -1;

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Non-zero if the application domain that this class is associated
        /// with is the default application domain for the process.
        /// </summary>
        private static readonly bool isDefaultAppDomain =
            (appDomain != null) ? appDomain.IsDefaultAppDomain() : false;
#endif
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Default Package Name & Version Data
        /// <summary>
        /// The default package name for the Eagle core library.
        /// </summary>
        private static readonly string DefaultPackageName = "Eagle";
        /// <summary>
        /// The default package name for the Eagle core library, in lower-case.
        /// </summary>
        private static readonly string DefaultPackageNameNoCase = "eagle";

        ///////////////////////////////////////////////////////////////////////

        //
        // TODO: This version information should be changed if the major or
        //       minor version of the assembly changes.
        //
        /// <summary>
        /// The default major version number for the Eagle core library.
        /// </summary>
        private static readonly int DefaultMajorVersion = 1;
        /// <summary>
        /// The default minor version number for the Eagle core library.
        /// </summary>
        private static readonly int DefaultMinorVersion = 0;

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// The default (two-part) version for the Eagle core library, built
        /// from the default major and minor version numbers.
        /// </summary>
        private static readonly Version DefaultVersion = GetTwoPartVersion(
            DefaultMajorVersion, DefaultMinorVersion);
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Package Name Data
        //
        // NOTE: This package may contain a set of built-in routines for use
        //       when loading binary plugins.
        //
        /// <summary>
        /// The name of the package that may contain the built-in routines used
        /// when loading binary plugins.
        /// </summary>
        private static readonly string LoaderPackageName = "Loader";

        //
        // NOTE: This package contains the Eagle [core] script library.  Its
        //       primary contents are script files that are required during
        //       initialization of an interpreter (e.g. the "embed.eagle",
        //       "init.eagle", and "vendor.eagle" files, etc).
        //
        /// <summary>
        /// The name of the package that contains the Eagle core script library.
        /// </summary>
        private static readonly string LibraryPackageName = DefaultPackageName;

        //
        // NOTE: This package contains the Eagle test [suite infrastructure].
        //       Its primary contents are script files that are required when
        //       running the Eagle test suite (e.g. the "constraints.eagle",
        //       "prologue.eagle", and "epilogue.eagle" files).  They are also
        //       designed to be used by third-party test suites.
        //
        /// <summary>
        /// The name of the package that contains the Eagle test suite
        /// infrastructure.
        /// </summary>
        private static readonly string TestPackageName = "Test";

        //
        // NOTE: This package may contain a set of built-in packages included
        //       with the Eagle [core] library, e.g. Harpy, et al.
        //
        /// <summary>
        /// The name of the package that may contain the built-in packages
        /// included with the Eagle core library.
        /// </summary>
        private static readonly string KitPackageName = "Kit";
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Package Name & Version Formatting
        /// <summary>
        /// The composite format string used to build a two-part version string.
        /// </summary>
        private static readonly string UpdateVersionFormat = "{0}.{1}";

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// The composite format string used to build a package name from its
        /// constituent parts.
        /// </summary>
        private static readonly string PackageNameFormat = "{0}{1}{2}";
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Stub Assembly Data
        /// <summary>
        /// The simple name of the stub assembly.
        /// </summary>
        private const string StubAssemblyName = "Eagle.Eye";

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// The fully qualified type name of the stub class within the stub
        /// assembly.
        /// </summary>
        private const string StubAssemblyTypeName =
            "Eagle._Components.Private.Stub";

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// The cached reflected method used to execute via the stub assembly,
        /// or null if it has not yet been resolved.
        /// </summary>
        private static MethodInfo StubExecuteMethodInfo = null;

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// The composite format string used to build a successful stub result.
        /// </summary>
        private const string StubOkResultFormat = "ok:{0}";
        /// <summary>
        /// The stub result string used to indicate an invalid interpreter.
        /// </summary>
        private const string StubErrorResult = "invalid interpreter";

        ///////////////////////////////////////////////////////////////////////

        //
        // HACK: This is purposely not read-only.
        //
        // NOTE: This value was determined by looking at the stub assembly
        //       file sizes from the latest release (as of May 2025) -AND-
        //       coming up with a reasonable margin-of-error for a minimum
        //       file size based on those values.
        //
        // TODO: Verify and/or update this value for each release.
        //
        /// <summary>
        /// The minimum allowed file size, in bytes, for a valid stub assembly
        /// file.
        /// </summary>
        private static long minimumStubAssemblyFileSize = 50000;
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Entry & Executing Assembly Data
        /// <summary>
        /// The string comparison type used when comparing assembly names.
        /// </summary>
        private static StringComparison assemblyNameComparisonType =
            StringComparison.OrdinalIgnoreCase;

        ///////////////////////////////////////////////////////////////////////

        #region Executing Assembly Data
        /// <summary>
        /// The assembly that contains the Eagle core library (i.e. the
        /// currently executing assembly).
        /// </summary>
        private static readonly Assembly thisAssembly =
            Assembly.GetExecutingAssembly();

#if CAS_POLICY
        /// <summary>
        /// The security evidence for the assembly that contains the Eagle core
        /// library, or null if it is not available.
        /// </summary>
        private static readonly Evidence thisAssemblyEvidence =
            (thisAssembly != null) ? thisAssembly.Evidence : null;
#endif

        /// <summary>
        /// The name of the assembly that contains the Eagle core library, or
        /// null if it is not available.
        /// </summary>
        private static readonly AssemblyName thisAssemblyName =
            (thisAssembly != null) ? thisAssembly.GetName() : null;

        /// <summary>
        /// The title of the assembly that contains the Eagle core library, or
        /// null if it is not available.
        /// </summary>
        private static readonly string thisAssemblyTitle =
            SharedAttributeOps.GetAssemblyTitle(thisAssembly);

        /// <summary>
        /// The file system location of the assembly that contains the Eagle
        /// core library, or null if it is not available.
        /// </summary>
        private static readonly string thisAssemblyLocation =
            (thisAssembly != null) ? thisAssembly.Location : null;

        /// <summary>
        /// The date and time associated with the assembly that contains the
        /// Eagle core library.
        /// </summary>
        private static readonly DateTime thisAssemblyDateTime =
            (thisAssembly != null) ? SharedAttributeOps.GetAssemblyDateTime(
                thisAssembly) : DateTime.MinValue; /* MUST BE AFTER LOCATION */

        /// <summary>
        /// The simple name of the assembly that contains the Eagle core
        /// library, or null if it is not available.
        /// </summary>
        private static readonly string thisAssemblySimpleName =
            (thisAssemblyName != null) ? thisAssemblyName.Name : null;

        /// <summary>
        /// The full name of the assembly that contains the Eagle core library,
        /// or null if it is not available.
        /// </summary>
        private static readonly string thisAssemblyFullName =
            (thisAssemblyName != null) ? thisAssemblyName.FullName : null;

        /// <summary>
        /// The version of the assembly that contains the Eagle core library, or
        /// null if it is not available.
        /// </summary>
        private static readonly Version thisAssemblyVersion =
            (thisAssemblyName != null) ? thisAssemblyName.Version : null;

        /// <summary>
        /// The culture associated with the assembly that contains the Eagle
        /// core library, or null if it is not available.
        /// </summary>
        private static readonly CultureInfo thisAssemblyCultureInfo =
            (thisAssemblyName != null) ? thisAssemblyName.CultureInfo : null;

        /// <summary>
        /// The public key token of the assembly that contains the Eagle core
        /// library, or null if it is not available.
        /// </summary>
        private static readonly byte[] thisAssemblyPublicKeyToken =
            (thisAssemblyName != null) ? thisAssemblyName.GetPublicKeyToken() : null;

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// The cached directory path of the assembly that contains the Eagle
        /// core library, or null if it has not yet been determined.
        /// </summary>
        private static string thisAssemblyPath = null;

        /// <summary>
        /// The URI associated with the assembly that contains the Eagle core
        /// library, or null if it is not available.
        /// </summary>
        private static readonly Uri thisAssemblyUri =
            SharedAttributeOps.GetAssemblyUri(thisAssembly);

        ///////////////////////////////////////////////////////////////////////

        //
        // TODO: Change this if the update URI needs to be something
        //       other than the one embedded in the assembly.  In
        //       addition, the AttributeOps.GetAssemblyUpdateBaseUri
        //       method would most likely need to be changed as well.
        //
        /// <summary>
        /// The base URI used when checking for updates, as embedded in the
        /// assembly that contains the Eagle core library.
        /// </summary>
        private static readonly Uri thisAssemblyUpdateBaseUri =
            SharedAttributeOps.GetAssemblyUpdateBaseUri(thisAssembly);

        //
        // TODO: Change this if the download URI needs to be something
        //       other than the one embedded in the assembly.  In
        //       addition, the AttributeOps.GetAssemblyDownloadBaseUri
        //       method would most likely need to be changed as well.
        //
        /// <summary>
        /// The base URI used when downloading files, as embedded in the
        /// assembly that contains the Eagle core library.
        /// </summary>
        private static readonly Uri thisAssemblyDownloadBaseUri =
            SharedAttributeOps.GetAssemblyDownloadBaseUri(thisAssembly);

        //
        // TODO: Change this if the script URI needs to be something
        //       other than the one embedded in the assembly.  In
        //       addition, the AttributeOps.GetAssemblyScriptBaseUri
        //       method would most likely need to be changed as well.
        //
        /// <summary>
        /// The base URI used when fetching scripts, as embedded in the assembly
        /// that contains the Eagle core library.
        /// </summary>
        private static readonly Uri thisAssemblyScriptBaseUri =
            SharedAttributeOps.GetAssemblyScriptBaseUri(thisAssembly);

        //
        // TODO: Change this if the auxiliary URI needs to be something
        //       other than the one embedded in the assembly.  In
        //       addition, the AttributeOps.GetAssemblyAuxiliaryBaseUri
        //       method would most likely need to be changed as well.
        //
        /// <summary>
        /// The base URI used for auxiliary purposes, as embedded in the
        /// assembly that contains the Eagle core library.
        /// </summary>
        private static readonly Uri thisAssemblyAuxiliaryBaseUri =
            SharedAttributeOps.GetAssemblyAuxiliaryBaseUri(thisAssembly);

        //
        // TODO: Change this if the XSD schema URI changes.
        //
        /// <summary>
        /// The URI used as the XML schema namespace for the assembly that
        /// contains the Eagle core library.
        /// </summary>
        private static readonly Uri thisAssemblyNamespaceUri =
            SharedAttributeOps.GetAssemblyXmlSchemaUri(thisAssembly);

        //
        // NOTE: These are the (cached) plugin flags for the core library
        //       assembly.
        //
        /// <summary>
        /// The cached plugin flags for the assembly that contains the Eagle
        /// core library, or null if they have not yet been determined.
        /// </summary>
        private static PluginFlags? thisAssemblyPluginFlags = null;

        //
        // NOTE: The number of times the assembly plugin flags callback has
        //       been invoked for this AppDomain.
        //
        /// <summary>
        /// The number of times the assembly plugin flags callback has been
        /// invoked within this application domain.
        /// </summary>
        private static int thisAssemblyPluginFlagsCount = 0;

        ///////////////////////////////////////////////////////////////////////

        #region Package Data
        //
        // NOTE: This is the base package name (e.g. "Eagle").
        //
        /// <summary>
        /// The base package name for the Eagle core library (e.g. "Eagle").
        /// </summary>
        private static readonly string packageName = GetPackageName(
            PackageType.Library, false);

        ///////////////////////////////////////////////////////////////////////

        //
        // NOTE: This is the base package name (e.g. "Eagle") in lower-case.
        //
        /// <summary>
        /// The base package name for the Eagle core library, in lower-case.
        /// </summary>
        private static readonly string packageNameNoCase =
            GetPackageName(PackageType.Library, true);

        ///////////////////////////////////////////////////////////////////////

        //
        // HACK: The package version *IS* the assembly version.
        //
        /// <summary>
        /// The package version for the Eagle core library, which is the same as
        /// the assembly version.
        /// </summary>
        private static readonly Version packageVersion =
            thisAssemblyVersion;

        ///////////////////////////////////////////////////////////////////////

        //
        // NOTE: When this value is non-zero, the full (four-part) package
        //       version (e.g. "1.0.9999.88888") will be used whenever it
        //       is possible and reasonable to do so (i.e. when there are
        //       no backward compatibility breaks, etc).
        //
        // HACK: This is purposely not read-only.
        //
        /// <summary>
        /// When non-zero, the full (four-part) package version will be used
        /// whenever it is possible and reasonable to do so.
        /// </summary>
        private static bool useLongPackageVersion = false;

        ///////////////////////////////////////////////////////////////////////

#if UNIX
        //
        // NOTE: This is the base package name (e.g. "Eagle") in lower-case
        //       for use on Unix.
        //
        /// <summary>
        /// The base package name for the Eagle core library, in lower-case, for
        /// use on Unix.
        /// </summary>
        private static readonly string unixPackageName = packageNameNoCase;

        //
        // HACK: The Unix package version *IS* the assembly version.
        //
        /// <summary>
        /// The package version for the Eagle core library on Unix, which is the
        /// same as the assembly version.
        /// </summary>
        private static readonly Version unixPackageVersion =
            thisAssemblyVersion;
#endif

        ///////////////////////////////////////////////////////////////////////

#if DEBUGGER
        /// <summary>
        /// The name used for the script debugger, derived from the package
        /// name.
        /// </summary>
        private static readonly string debuggerName = String.Format(
            "{0} {1}", packageName, typeof(Debugger).Name).Trim();
#endif
        #endregion
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Entry Assembly Data
        /// <summary>
        /// The entry assembly for the process, or null if it has not yet been
        /// determined.
        /// </summary>
        private static Assembly entryAssembly = null;
        /// <summary>
        /// The name of the entry assembly for the process, or null if it has
        /// not yet been determined.
        /// </summary>
        private static AssemblyName entryAssemblyName = null;

#if DEAD_CODE
        /// <summary>
        /// The title of the entry assembly for the process, or null if it has
        /// not yet been determined.
        /// </summary>
        private static string entryAssemblyTitle = null;
#endif

        /// <summary>
        /// The file system location of the entry assembly for the process, or
        /// null if it has not yet been determined.
        /// </summary>
        private static string entryAssemblyLocation = null;
        /// <summary>
        /// The version of the entry assembly for the process, or null if it has
        /// not yet been determined.
        /// </summary>
        private static Version entryAssemblyVersion = null;
        /// <summary>
        /// The cached directory path of the entry assembly for the process, or
        /// null if it has not yet been determined.
        /// </summary>
        private static string entryAssemblyPath = null;

        ///////////////////////////////////////////////////////////////////////

        //
        // HACK: *LEGACY* Make sure the entry assembly information is setup
        //       now.  It may be changed later; however, setting it up here
        //       is necessary for backward compatibility.
        //
#if MONO_BUILD
#pragma warning disable 414
#endif
        /// <summary>
        /// Non-zero if the entry assembly information has been set up.  This is
        /// performed eagerly for backward compatibility.
        /// </summary>
        private static bool entryAssemblySetup = RefreshEntryAssembly(null);
#if MONO_BUILD
#pragma warning restore 414
#endif
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Resource Management Data
        //
        // BUGFIX: Non-default builds may use a file name other than the
        //         normal "Eagle.dll"; therefore, the base resource name
        //         must match that value, not the package name.
        //
        /// <summary>
        /// The base resource name used when looking up managed resources for
        /// the Eagle core library.
        /// </summary>
        private static readonly string resourceBaseName =
            thisAssemblySimpleName;
        #endregion
        #endregion
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Private Read-Only Primary Thread Data (Logical Constants)
        /// <summary>
        /// The identifier of the primary thread for this application domain.
        /// </summary>
        private static long primaryThreadId;
        /// <summary>
        /// The managed identifier of the primary thread for this application
        /// domain.
        /// </summary>
        private static long primaryManagedThreadId;
        /// <summary>
        /// The native identifier of the primary thread for this application
        /// domain.
        /// </summary>
        private static long primaryNativeThreadId;

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// The primary thread for this application domain.
        /// </summary>
        private static readonly Thread primaryThread = SetupPrimaryThread();
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Private Read-Write Data
        #region Diagnostic Data
        //
        // HACK: Which thread currently holds the static lock?
        //
        /// <summary>
        /// The identifier of the thread that currently holds the static lock,
        /// or zero if it is not held.
        /// </summary>
        private static long lockThreadId = 0;

        ///////////////////////////////////////////////////////////////////////

        //
        // HACK: This is purposely not read-only.
        //
        /// <summary>
        /// The default value indicating whether complaints should be
        /// suppressed.
        /// </summary>
        private static bool defaultNoComplain = !Build.Debug;

        ///////////////////////////////////////////////////////////////////////

#if POLICY_TRACE
        //
        // NOTE: When this is non-zero, policy trace diagnostics will always
        //       be written by the engine, regardless of the per-interpreter
        //       settings.
        //
        /// <summary>
        /// When non-zero, policy trace diagnostics will always be written by
        /// the engine, regardless of the per-interpreter settings.
        /// </summary>
        private static int policyTrace = 0;
#endif
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Object Identity Data
        //
        // BUGBUG: Maybe an RNGCryptoServiceProvider instance should be used
        //         here instead of this?  Technically, this source of random
        //         numbers does not need to be "secure"; however, what would
        //         be the harm (i.e. other than a minor performance impact)?
        //
        /// <summary>
        /// The pseudo-random number generator used when producing initial
        /// object identifier values.
        /// </summary>
        private static Random random = new Random(); /* EXEMPT */

        ///////////////////////////////////////////////////////////////////////

        #region Randomized Initial Integer Identifiers
#if RANDOMIZE_ID
        /// <summary>
        /// The next available object identifier value.
        /// </summary>
        private static long nextId = Math.Abs((random != null) ?
            random.Next() : 0);

#if !SHARED_ID_POOL
        /// <summary>
        /// The next available token identifier value.
        /// </summary>
        private static long nextTokenId = Math.Abs((random != null) ?
            random.Next() : 0);
#endif

#if !SHARED_ID_POOL
        /// <summary>
        /// The next available complaint identifier value.
        /// </summary>
        private static long nextComplaintId = Math.Abs((random != null) ?
            random.Next() : 0);
#endif

#if !SHARED_ID_POOL
        /// <summary>
        /// The next available interpreter identifier value.
        /// </summary>
        private static long nextInterpreterId = Math.Abs((random != null) ?
            random.Next() : 0);
#endif

#if !SHARED_ID_POOL
        /// <summary>
        /// The next available script thread identifier value.
        /// </summary>
        private static long nextScriptThreadId = Math.Abs((random != null) ?
            random.Next() : 0);
#endif

#if !SHARED_ID_POOL
        /// <summary>
        /// The next available entry identifier value.
        /// </summary>
        private static long nextEntryId = Math.Abs((random != null) ?
            random.Next() : 0);
#endif

#if !SHARED_ID_POOL
        /// <summary>
        /// The next available rule set identifier value.
        /// </summary>
        private static long nextRuleSetId = Math.Abs((random != null) ?
            random.Next() : 0);
#endif

#if !SHARED_ID_POOL
        /// <summary>
        /// The next available file identifier value.
        /// </summary>
        private static long nextFileId = Math.Abs((random != null) ?
            random.Next() : 0);
#endif
#endif
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Non-Randomized Initial Integer Identifiers
#if !RANDOMIZE_ID
        /// <summary>
        /// The next available object identifier value.
        /// </summary>
        private static long nextId = 0;

#if !SHARED_ID_POOL
        /// <summary>
        /// The next available token identifier value.
        /// </summary>
        private static long nextTokenId = 0;
#endif

#if !SHARED_ID_POOL
        /// <summary>
        /// The next available complaint identifier value.
        /// </summary>
        private static long nextComplaintId = 0;
#endif

#if !SHARED_ID_POOL
        /// <summary>
        /// The next available interpreter identifier value.
        /// </summary>
        private static long nextInterpreterId = 0;
#endif

#if !SHARED_ID_POOL
        /// <summary>
        /// The next available script thread identifier value.
        /// </summary>
        private static long nextScriptThreadId = 0;
#endif

#if !SHARED_ID_POOL
        /// <summary>
        /// The next available entry identifier value.
        /// </summary>
        private static long nextEntryId = 0;
#endif

#if !SHARED_ID_POOL
        /// <summary>
        /// The next available rule set identifier value.
        /// </summary>
        private static long nextRuleSetId = 0;
#endif

#if !SHARED_ID_POOL
        /// <summary>
        /// The next available file identifier value.
        /// </summary>
        private static long nextFileId = 0;
#endif
#endif
        #endregion
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Threading Data
#if NATIVE && (WINDOWS || UNIX)
#if MONO || MONO_HACKS || NET_STANDARD_20
        /// <summary>
        /// When non-zero, indicates the native thread identifier support has
        /// been initialized.
        /// </summary>
        private static int threadInitialized;
#endif

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Non-zero if the native thread identifier should be obtained via a
        /// platform invoke call.
        /// </summary>
        private static bool pinvokeThreadId;
#endif

        ///////////////////////////////////////////////////////////////////////

#if NATIVE_THREAD_ID
        //
        // WARNING: This value should not be changed while any interpreter
        //          objects are active; otherwise, the wrong context state
        //          may be used.
        //
        /// <summary>
        /// When non-zero, the native thread identifier is used when selecting
        /// per-thread context state.
        /// </summary>
        private static int useNativeThreadIdForContexts = 0;
#endif
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region "First" / "Active" / "All" Interpreter Tracking
        /// <summary>
        /// The total number of active interpreters across all threads in this
        /// application domain.
        /// </summary>
        private static long totalActiveCount;

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// The first interpreter created within this application domain, or
        /// null if there is none.
        /// </summary>
        private static Interpreter firstInterpreter = null;

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// The per-thread stack of interpreters that are currently active on
        /// the calling thread.
        /// </summary>
        [ThreadStatic()] /* ThreadSpecificData */
        private static InterpreterStackList activeInterpreters;

        /// <summary>
        /// The collection of all interpreters in this application domain.
        /// </summary>
        private static readonly InterpreterDictionary allInterpreters =
            new InterpreterDictionary();

        /// <summary>
        /// The per-thread cache of interpreter collections used to speed up
        /// lookups in this application domain.
        /// </summary>
        private static readonly InterpreterDictionaryCache allInterpretersCache =
            new InterpreterDictionaryCache();

        /// <summary>
        /// The collection of all interpreters in this application domain, keyed
        /// by their token identifier.
        /// </summary>
        private static readonly TokenInterpreterDictionary tokenInterpreters =
            new TokenInterpreterDictionary();
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region "All" Thread Tracking
        /// <summary>
        /// The collection of all engine threads in this application domain.
        /// </summary>
        private static readonly EngineThreadList allEngineThreads =
            new EngineThreadList();
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Path Override Data
        #region Environment Variable Path Overrides
        /// <summary>
        /// The override for the library path, as obtained from an environment
        /// variable, or null if there is none.
        /// </summary>
        private static string libraryPath = null;
        /// <summary>
        /// The override for the auto-path list, as obtained from an environment
        /// variable, or null if there is none.
        /// </summary>
        private static StringList autoPathList = null;
        /// <summary>
        /// The override for the Tcl library path, as obtained from an
        /// environment variable, or null if there is none.
        /// </summary>
        private static string tclLibraryPath = null;
        /// <summary>
        /// The override for the Tcl auto-path list, as obtained from an
        /// environment variable, or null if there is none.
        /// </summary>
        private static StringList tclAutoPathList = null;
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Shared Binary / Base / Library Path Overrides
        //
        // NOTE: This is no longer read-only; also, it has been renamed
        //       from "binaryPath" to "sharedBinaryPath".
        //
        /// <summary>
        /// The shared override for the binary path, or null if there is none.
        /// </summary>
        private static string sharedBinaryPath = null;

        ///////////////////////////////////////////////////////////////////////

        //
        // NOTE: Shared override for the base path.
        //
        /// <summary>
        /// The shared override for the base path, or null if there is none.
        /// </summary>
        private static string sharedBasePath = null;

        ///////////////////////////////////////////////////////////////////////

        //
        // NOTE: Shared override for the library path.
        //
        /// <summary>
        /// The shared override for the library path, or null if there is none.
        /// </summary>
        private static string sharedLibraryPath = null;
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Shared Externals Path Overrides
        //
        // NOTE: Shared override for the externals path.
        //
        /// <summary>
        /// The shared override for the externals path, or null if there is
        /// none.
        /// </summary>
        private static string sharedExternalsPath = null;
        #endregion
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Package Path Data
        /// <summary>
        /// The cached package name path relative to the assembly location, or
        /// null if it has not yet been determined.
        /// </summary>
        private static string assemblyPackageNamePath = null;
        /// <summary>
        /// The cached package root path relative to the assembly location, or
        /// null if it has not yet been determined.
        /// </summary>
        private static string assemblyPackageRootPath = null;

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// The cached raw package name path relative to the binary base
        /// directory, or null if it has not yet been determined.
        /// </summary>
        private static string rawBinaryBasePackageNamePath = null;
        /// <summary>
        /// The cached raw package root path relative to the binary base
        /// directory, or null if it has not yet been determined.
        /// </summary>
        private static string rawBinaryBasePackageRootPath = null;

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// The cached raw package name path relative to the base directory, or
        /// null if it has not yet been determined.
        /// </summary>
        private static string rawBasePackageNamePath = null;
        /// <summary>
        /// The cached raw package root path relative to the base directory, or
        /// null if it has not yet been determined.
        /// </summary>
        private static string rawBasePackageRootPath = null;

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// The cached package path that is a peer of the binary directory, or
        /// null if it has not yet been determined.
        /// </summary>
        private static string packagePeerBinaryPath = null;
        /// <summary>
        /// The cached package path that is a peer of the assembly directory, or
        /// null if it has not yet been determined.
        /// </summary>
        private static string packagePeerAssemblyPath = null;
        /// <summary>
        /// The cached package root path, or null if it has not yet been
        /// determined.
        /// </summary>
        private static string packageRootPath = null;

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// The cached package name path relative to the binary directory, or
        /// null if it has not yet been determined.
        /// </summary>
        private static string packageNameBinaryPath = null;
        /// <summary>
        /// The cached package name path relative to the assembly directory, or
        /// null if it has not yet been determined.
        /// </summary>
        private static string packageNameAssemblyPath = null;
        /// <summary>
        /// The cached package name root path, or null if it has not yet been
        /// determined.
        /// </summary>
        private static string packageNameRootPath = null;

        ///////////////////////////////////////////////////////////////////////

#if UNIX
        /// <summary>
        /// The cached local package name path for use on Unix, or null if it
        /// has not yet been determined.
        /// </summary>
        private static string unixPackageNameLocalPath = null;
        /// <summary>
        /// The cached package name path for use on Unix, or null if it has not
        /// yet been determined.
        /// </summary>
        private static string unixPackageNamePath = null;
#endif

        ///////////////////////////////////////////////////////////////////////

#if NATIVE && TCL
        /// <summary>
        /// The cached Tcl package name path, or null if it has not yet been
        /// determined.
        /// </summary>
        private static string tclPackageNamePath = null;
        /// <summary>
        /// The cached Tcl package name root path, or null if it has not yet
        /// been determined.
        /// </summary>
        private static string tclPackageNameRootPath = null;
#endif
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Shared Auto-Path List
        //
        // NOTE: Shared auto-path list.  The list is automatically initialized
        //       [once] when necessary; however, it may be overridden later to
        //       influence [package] behavior of interpreters created after it
        //       has been changed.
        //
        /// <summary>
        /// The shared auto-path list used to influence package behavior of
        /// interpreters created after it has been changed, or null if it has
        /// not yet been initialized.
        /// </summary>
        private static StringList sharedAutoPathList = null;
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Trusted Hashes List
        //
        // NOTE: The global list of trusted hashes.  Used for all interpreters
        //       in this application domain.  Set via the Utility class, by an
        //       external caller.
        //
        /// <summary>
        /// The global list of trusted hashes used for all interpreters in this
        /// application domain, or null if there is none.
        /// </summary>
        private static StringList trustedHashes = null;
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Stub Assembly Data
        /// <summary>
        /// The cached raw bytes of the stub assembly, or null if they have not
        /// yet been loaded.
        /// </summary>
        private static byte[] stubAssemblyBytes = null;
        #endregion
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Threading Cooperative Locking Diagnostic Methods
        /// <summary>
        /// This method queries the identifier of the thread that currently
        /// holds the static lock.
        /// </summary>
        /// <returns>
        /// The identifier of the thread that currently holds the static lock,
        /// or zero if it is not held.
        /// </returns>
        private static long MaybeWhoHasLock()
        {
            return Interlocked.CompareExchange(ref lockThreadId, 0, 0);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method records the calling thread as the holder of the static
        /// lock, when the lock has been acquired.
        /// </summary>
        /// <param name="locked">
        /// Non-zero if the static lock has been acquired by the calling thread.
        /// </param>
        private static void MaybeSomebodyHasLock(
            bool locked /* in */
            )
        {
            if (locked)
            {
                /* IGNORED */
                Interlocked.CompareExchange(
                    ref lockThreadId, GetCurrentLockThreadId(), 0);
            }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method clears the record of the thread holding the static lock,
        /// when the calling thread is releasing it.
        /// </summary>
        /// <param name="locked">
        /// Non-zero if the static lock is being released by the calling thread.
        /// </param>
        private static void MaybeNobodyHasLock(
            bool locked /* in */
            )
        {
            if (locked)
            {
                /* IGNORED */
                Interlocked.CompareExchange(
                    ref lockThreadId, 0, GetCurrentLockThreadId());
            }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Threading Cooperative Locking Methods
        #region Dead Code
#if DEAD_CODE
        /// <summary>
        /// This method attempts to acquire the static lock without waiting.
        /// </summary>
        /// <param name="locked">
        /// Upon return, this parameter will be non-zero if the static lock was
        /// acquired by the calling thread.
        /// </param>
        private static void TryLock(
            ref bool locked
            )
        {
            if (syncRoot == null)
                return;

            locked = Monitor.TryEnter(syncRoot);
            MaybeSomebodyHasLock(locked);
        }
#endif
        #endregion

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method attempts to acquire the static lock, waiting up to the
        /// specified timeout.
        /// </summary>
        /// <param name="timeout">
        /// The maximum amount of time, in milliseconds, to wait for the lock to
        /// be acquired.
        /// </param>
        /// <param name="locked">
        /// Upon return, this parameter will be non-zero if the static lock was
        /// acquired by the calling thread.
        /// </param>
        public static void TryLock(
            int timeout,
            ref bool locked
            )
        {
            if (syncRoot == null)
                return;

            locked = Monitor.TryEnter(syncRoot, timeout);
            MaybeSomebodyHasLock(locked);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method releases the static lock, if it is currently held by the
        /// calling thread.
        /// </summary>
        /// <param name="locked">
        /// Upon entry, non-zero if the static lock is held by the calling
        /// thread; upon return, this parameter will be zero.
        /// </param>
        public static void ExitLock(
            ref bool locked
            )
        {
            if (syncRoot == null)
                return;

            if (locked)
            {
                MaybeNobodyHasLock(locked);
                Monitor.Exit(syncRoot);
                locked = false;
            }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method attempts to acquire the static lock and then immediately
        /// releases it, reporting whether the lock could be acquired.
        /// </summary>
        /// <param name="timeout">
        /// The maximum amount of time, in milliseconds, to wait for the lock to
        /// be acquired, or null to use the default soft-lock timeout.
        /// </param>
        /// <returns>
        /// True if the static lock was acquired; otherwise, false.
        /// </returns>
        public static bool TryLockAndExit(
            int? timeout
            )
        {
            bool locked = false;

            try
            {
                if (timeout != null)
                    TryLock((int)timeout, ref locked);
                else
                    SoftTryLock(ref locked);

                return locked;
            }
            finally
            {
                ExitLock(ref locked);
            }
        }

        ///////////////////////////////////////////////////////////////////////

        #region Special Timeout Locking Methods
        /// <summary>
        /// This method attempts to acquire the static lock using the soft-lock
        /// timeout.
        /// </summary>
        /// <param name="locked">
        /// Upon return, this parameter will be non-zero if the static lock was
        /// acquired by the calling thread.
        /// </param>
        private static void SoftTryLock(
            ref bool locked
            )
        {
            TryLock(ThreadOps.GetTimeout(
                null, null, TimeoutType.SoftLock), ref locked);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method attempts to acquire the static lock using the firm-lock
        /// timeout.
        /// </summary>
        /// <param name="locked">
        /// Upon return, this parameter will be non-zero if the static lock was
        /// acquired by the calling thread.
        /// </param>
        private static void FirmTryLock(
            ref bool locked
            )
        {
            TryLock(ThreadOps.GetTimeout(
                null, null, TimeoutType.FirmLock), ref locked);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method attempts to acquire the static lock using the hard-lock
        /// timeout.
        /// </summary>
        /// <param name="locked">
        /// Upon return, this parameter will be non-zero if the static lock was
        /// acquired by the calling thread.
        /// </param>
        private static void HardTryLock(
            ref bool locked
            )
        {
            TryLock(ThreadOps.GetTimeout(
                null, null, TimeoutType.HardLock), ref locked);
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Health Support Methods
        /// <summary>
        /// This method attempts to acquire the static lock for the purpose of a
        /// health check, recording an error when it cannot be acquired.
        /// </summary>
        /// <param name="timeout">
        /// The maximum amount of time, in milliseconds, to wait for the lock to
        /// be acquired, or null to use the default soft-lock timeout.
        /// </param>
        /// <param name="locked">
        /// Upon return, this parameter will be non-zero if the static lock was
        /// acquired by the calling thread.
        /// </param>
        /// <param name="errors">
        /// Upon failure, this parameter will receive any error messages
        /// produced by this method.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise, an appropriate
        /// error code.
        /// </returns>
        public static ReturnCode TryLockForHealth(
            int? timeout,
            ref bool locked,
            ref ResultList errors
            )
        {
            if (timeout != null)
                TryLock((int)timeout, ref locked);
            else
                SoftTryLock(ref locked);

            if (locked)
            {
                return ReturnCode.Ok;
            }
            else
            {
                if (errors == null)
                    errors = new ResultList();

                errors.Add(
                    "tryLock: unable to acquire global lock");

                return ReturnCode.Error;
            }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Path Locking Methods
        /// <summary>
        /// This method attempts to acquire the static lock for a path metadata
        /// operation.
        /// </summary>
        /// <param name="locked">
        /// Upon return, this parameter will be non-zero if the static lock was
        /// acquired by the calling thread.
        /// </param>
        private static void PathMetaTryLock(
            ref bool locked
            )
        {
            SoftTryLock(ref locked);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method attempts to acquire the static lock for a path
        /// operation, using the soft-lock timeout.
        /// </summary>
        /// <param name="locked">
        /// Upon return, this parameter will be non-zero if the static lock was
        /// acquired by the calling thread.
        /// </param>
        private static void PathSoftTryLock(
            ref bool locked
            )
        {
            SoftTryLock(ref locked);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method attempts to acquire the static lock for a path
        /// operation, using the hard-lock timeout.
        /// </summary>
        /// <param name="locked">
        /// Upon return, this parameter will be non-zero if the static lock was
        /// acquired by the calling thread.
        /// </param>
        private static void PathHardTryLock(
            ref bool locked
            )
        {
            HardTryLock(ref locked);
        }
        #endregion
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Object Identity Methods
#if USE_APPDOMAIN_FOR_ID
        /// <summary>
        /// This method may combine the specified integer identifier with the
        /// identifier of the current application domain, producing a composite
        /// long integer identifier that is unique across application domains.
        /// This handling never applies to the default application domain.
        /// </summary>
        /// <param name="id">
        /// The original integer identifier to be combined with the identifier
        /// of the current application domain.
        /// </param>
        /// <param name="noComplain">
        /// Non-zero to suppress any error reporting (i.e. complaints) via the
        /// <c>DebugOps.Complain</c> subsystem in the event the generated
        /// identifier is invalid (e.g. negative).
        /// </param>
        /// <returns>
        /// The composite long integer identifier; otherwise, the original
        /// identifier verbatim if it could not be combined with the
        /// application domain identifier.
        /// </returns>
        private static long MaybeCombineWithAppDomainId(
            long id,
            bool noComplain
            ) /* THREAD-SAFE, NO-LOCK */
        {
            //
            // NOTE: This handling never applies to the default application
            //       domain (COMPAT: Eagle Beta).
            //
            if (isDefaultAppDomain) /* NO-LOCK, READ-ONLY */
                return id;

            //
            // NOTE: Make sure the application domain identifier is positive
            //       and fits completely within a 32-bit signed integer (which
            //       it must, since it is declared as "int"); otherwise, just
            //       return the original identifier verbatim.
            //
            // HACK: This method knows the application domain identifier will
            //       be used for the top-half of the resulting composite long
            //       integer identifier and this class "guarantees" that no
            //       integer identifiers will be negative; therefore, the top
            //       bit of the application domain identifier cannot be set
            //       (i.e. it cannot be negative).
            //
            if (appDomainId < 0) /* NO-LOCK, READ-ONLY */
            {
                //
                // HACK: This method may not be able to use the DebugOps
                //       methods because they may call into us (e.g. the
                //       Complain method).
                //
                if (!noComplain)
                {
                    DebugOps.Complain(ReturnCode.Error,
                        "application domain identifier is negative");
                }

                return id;
            }

            //
            // NOTE: Make sure the original identifier fits completely within
            //       a 32-bit unsigned integer; otherwise, just return the
            //       original identifier verbatim.
            //
            // HACK: This method knows the original identifier will be used
            //       for the bottom-half of resulting composite long integer
            //       identifier; therefore, any value that can fit within
            //       32-bits is fair game.
            //
            if (id < 0)
            {
                //
                // HACK: This method may not be able to use the DebugOps
                //       methods because they may call into us (e.g. the
                //       Complain method).
                //
                if (!noComplain)
                {
                    DebugOps.Complain(ReturnCode.Error,
                        "original identifier is negative");
                }

                return id;
            }

            if (id > uint.MaxValue)
            {
                //
                // HACK: This method may not be able to use the DebugOps
                //       methods because they may call into us (e.g. the
                //       Complain method).
                //
                if (!noComplain)
                {
                    DebugOps.Complain(ReturnCode.Error, String.Format(
                        "original identifier is greater than {0}",
                        uint.MaxValue));
                }

                return id;
            }

            return ConversionOps.MakeLong(
                appDomainId /* NO-LOCK, READ-ONLY */, id);
        }
#endif

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method generates the next available global integer identifier,
        /// suitable for use with script visible entities (e.g. channel names).
        /// </summary>
        /// <returns>
        /// The next available global integer identifier.
        /// </returns>
        public static long NextId() /* THREAD-SAFE */
        {
            return NextId(defaultNoComplain);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method generates the next available global integer identifier,
        /// suitable for use with script visible entities (e.g. channel names).
        /// </summary>
        /// <param name="noComplain">
        /// Non-zero to suppress any error reporting (i.e. complaints) via the
        /// <c>DebugOps.Complain</c> subsystem in the event the generated
        /// identifier is invalid (e.g. negative).
        /// </param>
        /// <returns>
        /// The next available global integer identifier.
        /// </returns>
        private static long NextId(
            bool noComplain
            ) /* THREAD-SAFE */
        {
            long result;

            //
            // NOTE: This is our cheap unique Id generator for
            //       the various script visible identifiers
            //       (such as channel names, etc).  This value
            //       is not per-interpreter; therefore, use with
            //       caution.
            //
            result = Interlocked.Increment(ref nextId);

#if USE_APPDOMAIN_FOR_ID
            result = MaybeCombineWithAppDomainId(result, noComplain);
#endif

            //
            // HACK: This method may not be able to use the DebugOps
            //       methods because they may call into us (e.g. the
            //       Complain method).
            //
            if (!noComplain && (result < 0))
            {
                DebugOps.Complain(ReturnCode.Error,
                    "next global identifier is negative");
            }

            return result;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method generates the next available integer identifier using
        /// the specified interpreter, falling back to the global identifier
        /// pool when no interpreter is available.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter context to use when generating the identifier, or
        /// null to use the global identifier pool.
        /// </param>
        /// <returns>
        /// The next available integer identifier.
        /// </returns>
        public static long NextId(
            Interpreter interpreter
            ) /* THREAD-SAFE */
        {
            return NextId(interpreter, defaultNoComplain);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method generates the next available integer identifier using
        /// the specified interpreter, falling back to the global identifier
        /// pool when no interpreter is available.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter context to use when generating the identifier, or
        /// null to use the global identifier pool.
        /// </param>
        /// <param name="noComplain">
        /// Non-zero to suppress any error reporting (i.e. complaints) via the
        /// <c>DebugOps.Complain</c> subsystem in the event the generated
        /// identifier is invalid (e.g. negative).
        /// </param>
        /// <returns>
        /// The next available integer identifier.
        /// </returns>
        private static long NextId(
            Interpreter interpreter,
            bool noComplain
            ) /* THREAD-SAFE */
        {
            return (interpreter != null) ?
                interpreter.NextId() : NextId(noComplain);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method generates the next available global token identifier.
        /// Token identifiers are unique to the application domain so that
        /// entities possessing a token may be safely shared between multiple
        /// interpreters.
        /// </summary>
        /// <returns>
        /// The next available global token identifier.
        /// </returns>
        public static long NextTokenId() /* THREAD-SAFE */
        {
            return NextTokenId(defaultNoComplain);
        }

        ///////////////////////////////////////////////////////////////////////

        //
        // NOTE: Token identifiers require unique identifiers that are global
        //       to the AppDomain just in case an entity with a token ends up
        //       actually being shared by multiple interpreters.  Furthermore,
        //       it is highly recommended that the USE_APPDOMAIN_FOR_ID define
        //       always be used so that isolated interpreters do not pose any
        //       problem for this sharing setup.
        //
        /// <summary>
        /// This method generates the next available global token identifier.
        /// Token identifiers are unique to the application domain so that
        /// entities possessing a token may be safely shared between multiple
        /// interpreters.
        /// </summary>
        /// <param name="noComplain">
        /// Non-zero to suppress any error reporting (i.e. complaints) via the
        /// <c>DebugOps.Complain</c> subsystem in the event the generated
        /// identifier is invalid (e.g. negative).
        /// </param>
        /// <returns>
        /// The next available global token identifier.
        /// </returns>
        private static long NextTokenId(
            bool noComplain
            ) /* THREAD-SAFE */
        {
            long result;

#if SHARED_ID_POOL
            result = NextId(noComplain);
#else
            result = Interlocked.Increment(ref nextTokenId);
#endif

#if USE_APPDOMAIN_FOR_ID
            result = MaybeCombineWithAppDomainId(result, noComplain);
#endif

            if (!noComplain && (result < 0))
            {
                DebugOps.Complain(ReturnCode.Error,
                    "next token identifier is negative");
            }

            return result;
        }

        ///////////////////////////////////////////////////////////////////////

        //
        // WARNING: This is used by the DebugOps.Complain subsystem.
        //
        /// <summary>
        /// This method generates the next available global complaint
        /// identifier, for use by the complaint (<c>DebugOps.Complain</c>)
        /// subsystem.
        /// </summary>
        /// <returns>
        /// The next available global complaint identifier.
        /// </returns>
        public static long NextComplaintId() /* THREAD-SAFE */
        {
            return NextComplaintId(true); // HACK: Hard-coded, do not change.
        }

        ///////////////////////////////////////////////////////////////////////

        //
        // WARNING: This is used by the DebugOps.Complain subsystem.
        //
        /// <summary>
        /// This method generates the next available global complaint
        /// identifier, for use by the complaint (<c>DebugOps.Complain</c>)
        /// subsystem.
        /// </summary>
        /// <param name="noComplain">
        /// Non-zero to suppress any error reporting (i.e. complaints) via the
        /// <c>DebugOps.Complain</c> subsystem in the event the generated
        /// identifier is invalid (e.g. negative).
        /// </param>
        /// <returns>
        /// The next available global complaint identifier.
        /// </returns>
        private static long NextComplaintId(
            bool noComplain
            ) /* THREAD-SAFE */
        {
            long result;

#if SHARED_ID_POOL
            result = NextId(noComplain);
#else
            result = Interlocked.Increment(ref nextComplaintId);
#endif

#if USE_APPDOMAIN_FOR_ID
            result = MaybeCombineWithAppDomainId(result, noComplain);
#endif

            //
            // HACK: This method may not be able to use the DebugOps
            //       methods because they may call into us (e.g. the
            //       Complain method).
            //
            if (!noComplain && (result < 0))
            {
                DebugOps.Complain(ReturnCode.Error,
                    "next complaint identifier is negative");
            }

            return result;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method generates the next available global interpreter
        /// identifier.  Interpreter names must be totally unique within the
        /// application domain.
        /// </summary>
        /// <returns>
        /// The next available global interpreter identifier.
        /// </returns>
        public static long NextInterpreterId() /* THREAD-SAFE */
        {
            return NextInterpreterId(defaultNoComplain);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method generates the next available global interpreter
        /// identifier.  Interpreter names must be totally unique within the
        /// application domain.
        /// </summary>
        /// <param name="noComplain">
        /// Non-zero to suppress any error reporting (i.e. complaints) via the
        /// <c>DebugOps.Complain</c> subsystem in the event the generated
        /// identifier is invalid (e.g. negative).
        /// </param>
        /// <returns>
        /// The next available global interpreter identifier.
        /// </returns>
        private static long NextInterpreterId(
            bool noComplain
            ) /* THREAD-SAFE */
        {
            long result;

            //
            // NOTE: Interpreter names should be totally unique within the
            //       application domain (or the process?); therefore, this
            //       must be global.
            //
#if SHARED_ID_POOL
            result = NextId(noComplain);
#else
            result = Interlocked.Increment(ref nextInterpreterId);
#endif

#if USE_APPDOMAIN_FOR_ID
            result = MaybeCombineWithAppDomainId(result, noComplain);
#endif

            if (!noComplain && (result < 0))
            {
                DebugOps.Complain(ReturnCode.Error,
                    "next interpreter identifier is negative");
            }

            return result;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method generates the next available global script thread
        /// identifier.  Script thread names must be totally unique within the
        /// application domain.
        /// </summary>
        /// <returns>
        /// The next available global script thread identifier.
        /// </returns>
        public static long NextScriptThreadId() /* THREAD-SAFE */
        {
            return NextScriptThreadId(defaultNoComplain);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method generates the next available global script thread
        /// identifier.  Script thread names must be totally unique within the
        /// application domain.
        /// </summary>
        /// <param name="noComplain">
        /// Non-zero to suppress any error reporting (i.e. complaints) via the
        /// <c>DebugOps.Complain</c> subsystem in the event the generated
        /// identifier is invalid (e.g. negative).
        /// </param>
        /// <returns>
        /// The next available global script thread identifier.
        /// </returns>
        private static long NextScriptThreadId(
            bool noComplain
            ) /* THREAD-SAFE */
        {
            long result;

            //
            // NOTE: ScriptThread names should be totally unique within the
            //       application domain (or the process?); therefore, this
            //       must be global.
            //
#if SHARED_ID_POOL
            result = NextId(noComplain);
#else
            result = Interlocked.Increment(ref nextScriptThreadId);
#endif

#if USE_APPDOMAIN_FOR_ID
            result = MaybeCombineWithAppDomainId(result, noComplain);
#endif

            if (!noComplain && (result < 0))
            {
                DebugOps.Complain(ReturnCode.Error,
                    "next script thread identifier is negative");
            }

            return result;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method generates the next available global entry identifier.
        /// </summary>
        /// <returns>
        /// The next available global entry identifier.
        /// </returns>
        public static long NextEntryId() /* THREAD-SAFE */
        {
            return NextEntryId(defaultNoComplain);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method generates the next available global entry identifier.
        /// </summary>
        /// <param name="noComplain">
        /// Non-zero to suppress any error reporting (i.e. complaints) via the
        /// <c>DebugOps.Complain</c> subsystem in the event the generated
        /// identifier is invalid (e.g. negative).
        /// </param>
        /// <returns>
        /// The next available global entry identifier.
        /// </returns>
        private static long NextEntryId(
            bool noComplain
            ) /* THREAD-SAFE */
        {
            long result;

#if SHARED_ID_POOL
            result = NextId(noComplain);
#else
            result = Interlocked.Increment(ref nextEntryId);
#endif

#if USE_APPDOMAIN_FOR_ID
            result = MaybeCombineWithAppDomainId(result, noComplain);
#endif

            if (!noComplain && (result < 0))
            {
                DebugOps.Complain(ReturnCode.Error,
                    "next entry identifier is negative");
            }

            return result;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method generates the next available global event identifier.
        /// Event names must be totally unique within the application domain.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter context to use when generating the identifier, or
        /// null to use the global identifier pool.
        /// </param>
        /// <returns>
        /// The next available global event identifier.
        /// </returns>
        public static long NextEventId(
            Interpreter interpreter
            ) /* THREAD-SAFE */
        {
            return NextEventId(interpreter, defaultNoComplain);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method generates the next available global event identifier.
        /// Event names must be totally unique within the application domain.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter context to use when generating the identifier, or
        /// null to use the global identifier pool.
        /// </param>
        /// <param name="noComplain">
        /// Non-zero to suppress any error reporting (i.e. complaints) via the
        /// <c>DebugOps.Complain</c> subsystem in the event the generated
        /// identifier is invalid (e.g. negative).
        /// </param>
        /// <returns>
        /// The next available global event identifier.
        /// </returns>
        private static long NextEventId(
            Interpreter interpreter,
            bool noComplain
            ) /* THREAD-SAFE */
        {
            //
            // NOTE: Event names must be totally unique within the application
            //       domain (or the process?); therefore, this must be global.
            //
            return NextId(noComplain);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method generates the next available global rule set
        /// identifier.
        /// </summary>
        /// <returns>
        /// The next available global rule set identifier.
        /// </returns>
        public static long NextRuleSetId() /* THREAD-SAFE */
        {
            return NextRuleSetId(defaultNoComplain);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method generates the next available global rule set
        /// identifier.
        /// </summary>
        /// <param name="noComplain">
        /// Non-zero to suppress any error reporting (i.e. complaints) via the
        /// <c>DebugOps.Complain</c> subsystem in the event the generated
        /// identifier is invalid (e.g. negative).
        /// </param>
        /// <returns>
        /// The next available global rule set identifier.
        /// </returns>
        private static long NextRuleSetId(
            bool noComplain
            ) /* THREAD-SAFE */
        {
            long result;

#if SHARED_ID_POOL
            result = NextId(noComplain);
#else
            result = Interlocked.Increment(ref nextRuleSetId);
#endif

#if USE_APPDOMAIN_FOR_ID
            result = MaybeCombineWithAppDomainId(result, noComplain);
#endif

            if (!noComplain && (result < 0))
            {
                DebugOps.Complain(ReturnCode.Error,
                    "next ruleset identifier is negative");
            }

            return result;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method generates the next available global file identifier.
        /// </summary>
        /// <returns>
        /// The next available global file identifier.
        /// </returns>
        public static long NextFileId() /* THREAD-SAFE */
        {
            return NextFileId(defaultNoComplain);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method generates the next available global file identifier.
        /// </summary>
        /// <param name="noComplain">
        /// Non-zero to suppress any error reporting (i.e. complaints) via the
        /// <c>DebugOps.Complain</c> subsystem in the event the generated
        /// identifier is invalid (e.g. negative).
        /// </param>
        /// <returns>
        /// The next available global file identifier.
        /// </returns>
        private static long NextFileId(
            bool noComplain
            ) /* THREAD-SAFE */
        {
            long result;

#if SHARED_ID_POOL
            result = NextId(noComplain);
#else
            result = Interlocked.Increment(ref nextFileId);
#endif

#if USE_APPDOMAIN_FOR_ID
            result = MaybeCombineWithAppDomainId(result, noComplain);
#endif

            if (!noComplain && (result < 0))
            {
                DebugOps.Complain(ReturnCode.Error,
                    "next file identifier is negative");
            }

            return result;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method generates the next available global type identifier.
        /// Type names must be totally unique within the application domain.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter context to use when generating the identifier, or
        /// null to use the global identifier pool.
        /// </param>
        /// <returns>
        /// The next available global type identifier.
        /// </returns>
        public static long NextTypeId(
            Interpreter interpreter
            ) /* THREAD-SAFE */
        {
            return NextTypeId(interpreter, defaultNoComplain);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method generates the next available global type identifier.
        /// Type names must be totally unique within the application domain.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter context to use when generating the identifier, or
        /// null to use the global identifier pool.
        /// </param>
        /// <param name="noComplain">
        /// Non-zero to suppress any error reporting (i.e. complaints) via the
        /// <c>DebugOps.Complain</c> subsystem in the event the generated
        /// identifier is invalid (e.g. negative).
        /// </param>
        /// <returns>
        /// The next available global type identifier.
        /// </returns>
        private static long NextTypeId(
            Interpreter interpreter,
            bool noComplain
            ) /* THREAD-SAFE */
        {
            //
            // NOTE: Type names must be totally unique within
            //       the application domain; therefore, this
            //       must be global.
            //
            return NextId(noComplain);
        }

        ///////////////////////////////////////////////////////////////////////

        #region Dead Code
#if DEAD_CODE
        /// <summary>
        /// This method generates the next available global thread identifier.
        /// Thread names must be totally unique within the process.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter context to use when generating the identifier, or
        /// null to use the global identifier pool.
        /// </param>
        /// <returns>
        /// The next available global thread identifier.
        /// </returns>
        public static long NextThreadId(
            Interpreter interpreter
            ) /* THREAD-SAFE */
        {
            return NextThreadId(interpreter, defaultNoComplain);
        }
#endif
        #endregion

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method generates the next available global thread identifier.
        /// Thread names must be totally unique within the process.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter context to use when generating the identifier, or
        /// null to use the global identifier pool.
        /// </param>
        /// <param name="noComplain">
        /// Non-zero to suppress any error reporting (i.e. complaints) via the
        /// <c>DebugOps.Complain</c> subsystem in the event the generated
        /// identifier is invalid (e.g. negative).
        /// </param>
        /// <returns>
        /// The next available global thread identifier.
        /// </returns>
        public static long NextThreadId(
            Interpreter interpreter,
            bool noComplain
            ) /* THREAD-SAFE */
        {
            //
            // NOTE: Thread names should be totally unique within the process;
            //       therefore, this must be global.
            //
            return NextId(noComplain);
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Random Number Generation Methods
        //
        // WARNING: This method is used during interpreter creation.
        //
        /// <summary>
        /// This method fills the specified byte array with cryptographically
        /// random bytes obtained from the global random number generator.
        /// </summary>
        /// <param name="bytes">
        /// Upon success, the elements of this byte array are overwritten with
        /// random bytes.  The length of the array determines how many bytes
        /// are generated.
        /// </param>
        /// <returns>
        /// True if the random bytes were generated successfully; otherwise,
        /// false.
        /// </returns>
        public static bool GetRandomBytes(
            ref byte[] bytes
            )
        {
            bool locked = false;

            try
            {
                HardTryLock(ref locked); /* TRANSACTIONAL */

                if (locked)
                {
                    if (random != null)
                    {
                        random.NextBytes(bytes);
                        return true;
                    }
                }
                else
                {
                    TraceOps.LockTrace(
                        "GetRandomBytes",
                        typeof(GlobalState).Name, true,
                        TracePriority.LockError,
                        MaybeWhoHasLock());
                }
            }
            finally
            {
                ExitLock(ref locked); /* TRANSACTIONAL */
            }

            return false;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method generates a signed 64-bit pseudo-random number using
        /// the global random number generator.
        /// </summary>
        /// <returns>
        /// The generated random number, or zero if the underlying random
        /// bytes could not be obtained.
        /// </returns>
        public static long GetSignedRandomNumber()
        {
            byte[] bytes = new byte[sizeof(long)];

            if (!GetRandomBytes(ref bytes))
            {
                TraceOps.DebugTrace(
                    "GetSignedRandomNumber: could not get random bytes",
                    typeof(GlobalState).Name, TracePriority.ScriptError);

                return 0;
            }

            return BitConverter.ToInt64(bytes, 0);
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Threading Global Variable Access Methods
        /// <summary>
        /// This method gets the identifier of the current thread, using
        /// either the native or managed thread identifier depending on the
        /// active build configuration.
        /// </summary>
        /// <returns>
        /// The identifier of the current thread.
        /// </returns>
        public static long GetCurrentThreadId() /* THREAD-SAFE */
        {
#if NATIVE_THREAD_ID
            return GetCurrentNativeThreadId();
#else
            return GetCurrentManagedThreadId();
#endif
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method gets the system thread identifier of the current
        /// thread, using either the native or managed thread identifier
        /// depending on the active build configuration.
        /// </summary>
        /// <returns>
        /// The system thread identifier of the current thread.
        /// </returns>
        public static long GetCurrentSystemThreadId() /* THREAD-SAFE */
        {
#if NATIVE_THREAD_ID
            return GetCurrentNativeThreadId();
#else
            return GetCurrentManagedThreadId();
#endif
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method gets the thread identifier used for tracking ownership
        /// of the static lock on the current thread.
        /// </summary>
        /// <returns>
        /// The lock thread identifier for the current thread.
        /// </returns>
        public static long GetCurrentLockThreadId() /* THREAD-SAFE */
        {
            return AppDomain.GetCurrentThreadId(); /* HOT-PATH */
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method gets the thread identifier used for interpreter
        /// contexts on the current thread, using the native or managed thread
        /// identifier as configured.
        /// </summary>
        /// <returns>
        /// The context thread identifier for the current thread.
        /// </returns>
        public static long GetCurrentContextThreadId() /* THREAD-SAFE */
        {
#if NATIVE_THREAD_ID
            if (Interlocked.CompareExchange(
                    ref useNativeThreadIdForContexts, 0, 0) > 0)
            {
                return GetCurrentNativeThreadId();
            }
            else
#endif
            {
                return GetCurrentManagedThreadId();
            }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method gets the managed thread identifier of the current
        /// thread.
        /// </summary>
        /// <returns>
        /// The managed thread identifier of the current thread, or zero if it
        /// cannot be determined.
        /// </returns>
        public static long GetCurrentManagedThreadId() /* THREAD-SAFE */
        {
            Thread thread = Thread.CurrentThread;

            if (thread == null)
                return 0;

            return thread.ManagedThreadId;
        }

        ///////////////////////////////////////////////////////////////////////

#if NATIVE && (WINDOWS || UNIX)
        /// <summary>
        /// This method gets the native operating system thread identifier of
        /// the current thread by calling the native platform API directly.
        /// </summary>
        /// <returns>
        /// The native thread identifier of the current thread.
        /// </returns>
        /* THREAD-SAFE */
        private static long GetCurrentNativeThreadIdViaPInvoke()
        {
            //
            // HACK: This only applies when running on the .NET Core, not
            //       when simply restricting subsets of features to that
            //       of the .NET Standard.
            //
#if MONO || MONO_HACKS || NET_STANDARD_20
            if (Interlocked.Increment(ref threadInitialized) == 1)
            {
                // ObjectOps.Initialize(true); // TODO: Not needed yet?
                PlatformOps.Initialize(true);
            }
#endif

            return NativeOps.GetCurrentThreadId().ToInt64();
        }
#endif

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method gets the native operating system thread identifier of
        /// the current thread.
        /// </summary>
        /// <returns>
        /// The native thread identifier of the current thread.
        /// </returns>
        public static long GetCurrentNativeThreadId() /* THREAD-SAFE */
        {
#if NATIVE && (WINDOWS || UNIX)
            //
            // NOTE: The .NET Core runtime did not (at one point) return
            //       the real native thread ID when using the AppDomain
            //       class; therefore, use the native API directly.
            //
            // HACK: Also, the .NET Core runtime does not initialize the
            //       static classes in an order that guarantees the call
            //       to NativeOps will work; therefore, we forcibly call
            //       the PlatformOps.Initialize method.
            //
            if (pinvokeThreadId)
            {
                return GetCurrentNativeThreadIdViaPInvoke();
            }
            else
#endif
            {
                return AppDomain.GetCurrentThreadId();
            }
        }

        ///////////////////////////////////////////////////////////////////////

#if NATIVE_THREAD_ID
        /// <summary>
        /// This method optionally enables or disables the use of native
        /// thread identifiers for interpreter contexts, subject to whether
        /// any interpreters currently exist.
        /// </summary>
        /// <param name="force">
        /// Non-zero to skip the check that prevents changing the setting
        /// while one or more interpreters exist.
        /// </param>
        /// <param name="enable">
        /// When non-null on input, indicates whether to enable (true) or
        /// disable (false) the use of native thread identifiers for contexts.
        /// Upon return, this is set to whether such use is now in effect.
        /// </param>
        /// <param name="error">
        /// Upon failure, receives information about the error.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise, an appropriate
        /// error code.
        /// </returns>
        private static ReturnCode MaybeUseNativeThreadIdForContexts( /* NOT USED */
            bool force,       /* in */
            ref bool? enable, /* in, out */
            ref Result error  /* out */
            ) /* THREAD-SAFE */
        {
            if (!force && (enable != null))
            {
                int count = CountInterpreters(true);

                if (count == Count.Invalid)
                {
                    error = "unable to count interpreters";
                    return ReturnCode.Error;
                }

                if (count > 0)
                {
                    error = String.Format(
                        "cannot {0} native thread Id use for contexts, " +
                        "{1} {2}", (bool)enable ? "enable" : "disable",
                        count, (count == 1) ? "interpreter exists" :
                        "interpreters exist");

                    return ReturnCode.Error;
                }
            }

            if (enable != null)
            {
                if ((bool)enable)
                {
                    enable = Interlocked.Increment(
                        ref useNativeThreadIdForContexts) > 0;
                }
                else
                {
                    enable = Interlocked.Decrement(
                        ref useNativeThreadIdForContexts) > 0;
                }
            }
            else
            {
                enable = Interlocked.CompareExchange(
                    ref useNativeThreadIdForContexts, 0, 0) > 0;
            }

            return ReturnCode.Ok;
        }
#endif

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method records the current thread as the primary thread for
        /// the library, sets up its name and associated thread identifiers,
        /// and emits diagnostic trace output.
        /// </summary>
        /// <returns>
        /// The current thread, now established as the primary thread.
        /// </returns>
        private static Thread SetupPrimaryThread() /* THREAD-SAFE */
        {
            long threadId = AppDomain.GetCurrentThreadId();

#if NATIVE && (WINDOWS || UNIX)
            //
            // HACK: To get the real native thread ID when running on .NET
            //       Core (or a non-Windows operating system), we must use
            //       the native API.
            //
            pinvokeThreadId = CommonOps.Runtime.IsDotNetCore() ||
                !PlatformOps.IsWindowsOperatingSystem();
#endif

            Thread thread = Thread.CurrentThread;
            string threadName = FormatOps.DisplayThread(thread);

            TraceOps.DebugTrace(threadId, String.Format(
                "SetupPrimaryThread: library initialized in {0}application " +
                "domain {1} on managed thread with [{2}], next Id {3}, next " +
                "complaint Id {4}, next interpreter Id {5}, and next script " +
                "thread Id {6}.", AppDomainOps.IsCurrentDefault() ? "default " :
                String.Empty, AppDomainOps.GetCurrentId(), threadName, nextId,
                nextComplaintId, nextInterpreterId, nextScriptThreadId),
                typeof(GlobalState).Name, TracePriority.StartupDebug);

            /* IGNORED */
            MaybeSetupPrimaryThreadName(thread);

            /* LEGACY */
            SetupPrimaryThreadIds(true);

            return thread;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method assigns a generated name to the specified thread if it
        /// does not already have one.
        /// </summary>
        /// <param name="thread">
        /// The thread whose name should be set.  This parameter may be null.
        /// </param>
        /// <returns>
        /// True if a name was assigned to the thread; otherwise, false.
        /// </returns>
        private static bool MaybeSetupPrimaryThreadName(
            Thread thread
            )
        {
            if (thread != null)
            {
                try
                {
                    string name = thread.Name;

                    if (name == null)
                    {
                        thread.Name = String.Format(
                            "primaryThread#{0}", NextId());

                        return true;
                    }
                }
                catch
                {
                    // do nothing.
                }
            }

            return false;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method initializes the primary thread identifiers (neutral,
        /// managed, and native) for the library and emits diagnostic trace
        /// output.
        /// </summary>
        /// <param name="force">
        /// Non-zero to unconditionally overwrite the existing primary thread
        /// identifiers; zero to set them only if they have not already been
        /// set.
        /// </param>
        public static void SetupPrimaryThreadIds(
            bool force
            )
        {
            if (force)
            {
                /* IGNORED */
                Interlocked.Exchange(ref primaryThreadId,
                    GetCurrentThreadId());

                /* IGNORED */
                Interlocked.Exchange(ref primaryManagedThreadId,
                    GetCurrentManagedThreadId());

                /* IGNORED */
                Interlocked.Exchange(ref primaryNativeThreadId,
                    GetCurrentNativeThreadId());
            }
            else
            {
                /* IGNORED */
                Interlocked.CompareExchange(ref primaryThreadId,
                    GetCurrentThreadId(), 0);

                /* IGNORED */
                Interlocked.CompareExchange(ref primaryManagedThreadId,
                    GetCurrentManagedThreadId(), 0);

                /* IGNORED */
                Interlocked.CompareExchange(ref primaryNativeThreadId,
                    GetCurrentNativeThreadId(), 0);
            }

            long threadId = AppDomain.GetCurrentThreadId();

            TraceOps.DebugTrace(threadId, String.Format(
                "SetupPrimaryThreadIds: {0}initialized in {1}application " +
                "domain {2} with neutral Id {3}, managed Id {4}, native " +
                "Id {5}.", force ? "forcibly " : String.Empty,
                AppDomainOps.IsCurrentDefault() ? "default " : String.Empty,
                AppDomainOps.GetCurrentId(), primaryThreadId,
                primaryManagedThreadId, primaryNativeThreadId),
                typeof(GlobalState).Name, TracePriority.StartupDebug);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method gets the thread that has been established as the
        /// primary thread for the library.
        /// </summary>
        /// <returns>
        /// The primary thread, or null if it has not been set.
        /// </returns>
        private static Thread GetPrimaryThread() /* THREAD-SAFE */
        {
            return primaryThread; /* READ-ONLY */
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method gets the neutral thread identifier of the primary
        /// thread.
        /// </summary>
        /// <returns>
        /// The primary thread identifier, or zero if it has not been set.
        /// </returns>
        public static long GetPrimaryThreadId() /* THREAD-SAFE */
        {
            return Interlocked.CompareExchange(
                ref primaryThreadId, 0, 0);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method gets the managed thread identifier of the primary
        /// thread.
        /// </summary>
        /// <returns>
        /// The primary managed thread identifier, or zero if it has not been
        /// set.
        /// </returns>
        public static long GetPrimaryManagedThreadId() /* THREAD-SAFE */
        {
            return Interlocked.CompareExchange(
                ref primaryManagedThreadId, 0, 0);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method gets the native thread identifier of the primary
        /// thread.
        /// </summary>
        /// <returns>
        /// The primary native thread identifier, or zero if it has not been
        /// set.
        /// </returns>
        public static long GetPrimaryNativeThreadId() /* THREAD-SAFE */
        {
            return Interlocked.CompareExchange(
                ref primaryNativeThreadId, 0, 0);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method determines whether the current thread is the primary
        /// thread for the library.
        /// </summary>
        /// <returns>
        /// True if the current thread is the primary thread; otherwise,
        /// false.
        /// </returns>
        public static bool IsPrimaryThread() /* THREAD-SAFE */
        {
            return IsPrimaryThread(GetCurrentThreadId());
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method determines whether the current thread is the primary
        /// thread for the library, comparing managed thread identifiers.
        /// </summary>
        /// <returns>
        /// True if the current thread is the primary thread; otherwise,
        /// false.
        /// </returns>
        public static bool IsPrimaryManagedThread() /* THREAD-SAFE */
        {
            return IsPrimaryManagedThread(GetCurrentManagedThreadId());
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method determines whether the current thread is the primary
        /// thread for the library, comparing native thread identifiers.
        /// </summary>
        /// <returns>
        /// True if the current thread is the primary thread; otherwise,
        /// false.
        /// </returns>
        public static bool IsPrimaryNativeThread() /* THREAD-SAFE */
        {
            return IsPrimaryNativeThread(GetCurrentNativeThreadId());
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method determines whether the specified thread identifier
        /// matches the neutral identifier of the primary thread.
        /// </summary>
        /// <param name="threadId">
        /// The thread identifier to compare against the primary thread
        /// identifier.
        /// </param>
        /// <returns>
        /// True if the specified thread identifier is that of the primary
        /// thread; otherwise, false.
        /// </returns>
        private static bool IsPrimaryThread(
            long threadId
            ) /* THREAD-SAFE */
        {
            return (threadId == GetPrimaryThreadId());
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method determines whether the specified thread identifier
        /// matches the managed identifier of the primary thread.
        /// </summary>
        /// <param name="threadId">
        /// The managed thread identifier to compare against the primary
        /// managed thread identifier.
        /// </param>
        /// <returns>
        /// True if the specified thread identifier is that of the primary
        /// thread; otherwise, false.
        /// </returns>
        private static bool IsPrimaryManagedThread(
            long threadId
            ) /* THREAD-SAFE */
        {
            return (threadId == GetPrimaryManagedThreadId());
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method determines whether the specified thread identifier
        /// matches the native identifier of the primary thread.
        /// </summary>
        /// <param name="threadId">
        /// The native thread identifier to compare against the primary native
        /// thread identifier.
        /// </param>
        /// <returns>
        /// True if the specified thread identifier is that of the primary
        /// thread; otherwise, false.
        /// </returns>
        private static bool IsPrimaryNativeThread(
            long threadId
            ) /* THREAD-SAFE */
        {
            return (threadId == GetPrimaryNativeThreadId());
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region "First" / "Active" / "All" Interpreter Tracking Methods
        #region Interpreter Matching Methods
        /// <summary>
        /// This method determines whether the specified interpreter matches
        /// the criteria implied by the specified creation flags, primarily
        /// the "safe" versus "unsafe" distinction.
        /// </summary>
        /// <param name="createFlags">
        /// The creation flags used to match the interpreter, or null to match
        /// any non-null interpreter.
        /// </param>
        /// <param name="interpreter">
        /// The interpreter to test.  This parameter may be null.
        /// </param>
        /// <returns>
        /// True if the interpreter matches the specified criteria; otherwise,
        /// false.
        /// </returns>
        private static bool MatchInterpreter(
            CreateFlags? createFlags,
            Interpreter interpreter
            )
        {
            if (createFlags != null)
            {
                if (FlagOps.HasFlags(
                        (CreateFlags)createFlags,
                        CreateFlags.Safe, true))
                {
                    if ((interpreter != null) &&
                        interpreter.InternalIsSafe())
                    {
                        return true;
                    }
                }
                else
                {
                    if ((interpreter != null) &&
                        !interpreter.InternalIsSafe())
                    {
                        return true;
                    }
                }
            }
            else if (interpreter != null)
            {
                return true;
            }

            return false;
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Stub Interpreter Tracking Methods
        /// <summary>
        /// This method gets an interpreter suitable for use as a stub,
        /// preferring the first interpreter and falling back to any token
        /// interpreter.
        /// </summary>
        /// <returns>
        /// A suitable stub interpreter, or null if none is available.
        /// </returns>
        public static Interpreter GetStubInterpreter()
        {
            Interpreter interpreter = GetFirstInterpreter();

            if (interpreter != null)
                return interpreter;

            interpreter = GetAnyTokenInterpreter();

            if (interpreter != null)
                return interpreter;

            return null;
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region "First" Interpreter Tracking Methods
        /// <summary>
        /// This method determines whether the specified interpreter is the
        /// first interpreter tracked by the global state.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter to test.  This parameter may be null.
        /// </param>
        /// <returns>
        /// True if the specified interpreter is the first interpreter;
        /// otherwise, false.
        /// </returns>
        public static bool IsFirstInterpreter(
            Interpreter interpreter
            ) /* THREAD-SAFE */
        {
            bool locked = false;

            try
            {
                SoftTryLock(ref locked); /* TRANSACTIONAL */

                if (locked)
                {
                    if ((interpreter == null) || (firstInterpreter == null))
                        return false;

                    return Object.ReferenceEquals(
                        interpreter, firstInterpreter);
                }
                else
                {
                    TraceOps.LockTrace(
                        "IsFirstInterpreter",
                        typeof(GlobalState).Name, true,
                        TracePriority.LockError,
                        MaybeWhoHasLock());
                }
            }
            finally
            {
                ExitLock(ref locked); /* TRANSACTIONAL */
            }

            return false;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method gets the first interpreter tracked by the global
        /// state.
        /// </summary>
        /// <returns>
        /// The first interpreter, or null if there is none.
        /// </returns>
        public static Interpreter GetFirstInterpreter() /* THREAD-SAFE */
        {
            return GetFirstInterpreter(null);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method gets the first interpreter tracked by the global state
        /// that matches the specified creation flags, emitting diagnostic
        /// trace output upon failure.
        /// </summary>
        /// <param name="createFlags">
        /// The creation flags used to match the interpreter, or null to match
        /// any interpreter.
        /// </param>
        /// <returns>
        /// The matching first interpreter, or null if there is none.
        /// </returns>
        private static Interpreter GetFirstInterpreter(
            CreateFlags? createFlags
            ) /* THREAD-SAFE */
        {
            Interpreter interpreter;
            Result error = null;

            interpreter = GetFirstInterpreter(createFlags, ref error);

            if (interpreter != null)
                return interpreter;

            TraceOps.DebugTrace(String.Format(
                "GetFirstInterpreter: error = {0}", FormatOps.WrapOrNull(
                error)), typeof(GlobalState).Name, TracePriority.StartupError2);

            return null;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method gets the first interpreter tracked by the global state
        /// that matches the specified creation flags.
        /// </summary>
        /// <param name="createFlags">
        /// The creation flags used to match the interpreter, or null to match
        /// any interpreter.
        /// </param>
        /// <param name="error">
        /// Upon failure, receives information about the error.
        /// </param>
        /// <returns>
        /// The matching first interpreter, or null if there is none.
        /// </returns>
        private static Interpreter GetFirstInterpreter(
            CreateFlags? createFlags,
            ref Result error
            ) /* THREAD-SAFE */
        {
            bool notLocked = false;
            bool notFound = false;

            return GetFirstInterpreter(
                createFlags, ref notLocked, ref notFound, ref error);
        }

        ///////////////////////////////////////////////////////////////////////

        //
        // WARNING: This method is used during interpreter creation.
        //
        /// <summary>
        /// This method gets the first interpreter tracked by the global state
        /// that matches the specified creation flags, reporting whether the
        /// static lock could not be acquired or no match was found.
        /// </summary>
        /// <param name="createFlags">
        /// The creation flags used to match the interpreter, or null to match
        /// any interpreter.
        /// </param>
        /// <param name="notLocked">
        /// Upon failure, set to non-zero if the static lock could not be
        /// acquired.
        /// </param>
        /// <param name="notFound">
        /// Upon failure, set to non-zero if no matching interpreter was
        /// found.
        /// </param>
        /// <param name="error">
        /// Upon failure, receives information about the error.
        /// </param>
        /// <returns>
        /// The matching first interpreter, or null if there is none.
        /// </returns>
        public static Interpreter GetFirstInterpreter(
            CreateFlags? createFlags,
            ref bool notLocked,
            ref bool notFound,
            ref Result error
            ) /* THREAD-SAFE */
        {
            bool locked = false;

            try
            {
                HardTryLock(ref locked); /* TRANSACTIONAL */

                if (locked)
                {
                    if (MatchInterpreter(
                            createFlags, firstInterpreter))
                    {
                        return firstInterpreter;
                    }

                    notFound = true;
                    error = "invalid or \"safe\" mismatch";
                }
                else
                {
                    notLocked = true;
                    error = "unable to acquire static lock";

                    TraceOps.LockTrace(
                        "GetFirstInterpreter",
                        typeof(GlobalState).Name, true,
                        TracePriority.LockError,
                        MaybeWhoHasLock());
                }
            }
            finally
            {
                ExitLock(ref locked); /* TRANSACTIONAL */
            }

            return null;
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region "All" Interpreters Cache Methods
        //
        // WARNING: Assumes the static lock is already held.
        //
        /// <summary>
        /// This method gets the cached collection of interpreters associated
        /// with the specified thread.  This method assumes the static lock is
        /// already held.
        /// </summary>
        /// <param name="thread">
        /// The thread whose cached interpreters should be returned.  This
        /// parameter may be null.
        /// </param>
        /// <returns>
        /// The cached interpreter collection for the thread, or null if there
        /// is none.
        /// </returns>
        private static InterpreterDictionary GetCachedInterpreters(
            Thread thread /* in */
            )
        {
            if (allInterpretersCache == null)
                return null;

            if (thread == null)
                return null;

            InterpreterDictionary interpreters;

            if (!allInterpretersCache.TryGetValue(thread, out interpreters))
                return null;

            return interpreters;
        }

        ///////////////////////////////////////////////////////////////////////

        //
        // WARNING: Assumes the static lock is already held.
        //
        /// <summary>
        /// This method sets or removes the cached collection of interpreters
        /// associated with the specified thread.  This method assumes the
        /// static lock is already held.
        /// </summary>
        /// <param name="thread">
        /// The thread whose cached interpreters should be set or removed.
        /// This parameter may be null.
        /// </param>
        /// <param name="interpreters">
        /// The interpreter collection to cache for the thread, or null to
        /// remove any existing cached collection.
        /// </param>
        /// <returns>
        /// True if the cache was updated; otherwise, false.
        /// </returns>
        private static bool SetCachedInterpreters(
            Thread thread,                     /* in */
            InterpreterDictionary interpreters /* in */
            )
        {
            if (allInterpretersCache == null)
                return false;

            if (thread == null)
                return false;

            if (interpreters != null)
            {
                allInterpretersCache[thread] = interpreters;
                return true;
            }
            else
            {
                return allInterpretersCache.Remove(thread);
            }
        }

        ///////////////////////////////////////////////////////////////////////

        //
        // WARNING: Assumes the static lock is already held.
        //
        /// <summary>
        /// This method rebuilds the per-thread interpreter cache so that
        /// every live thread is associated with a fresh copy of the specified
        /// interpreter collection.  This method assumes the static lock is
        /// already held.
        /// </summary>
        /// <param name="interpreters">
        /// The interpreter collection to copy into the cache for each live
        /// thread.  This parameter may be null.
        /// </param>
        private static void RebuildInterpreterCache(
            InterpreterDictionary interpreters /* in: OPTIONAL */
            )
        {
            if (allInterpretersCache == null)
                return;

            InterpreterDictionary localInterpreters =
                (interpreters != null) ? interpreters.DeepCopy() : null;

            ThreadList threads = new ThreadList(allInterpretersCache.Keys);

            allInterpretersCache.Clear();

            foreach (Thread thread in threads) /* THREAD-ID */
            {
                if (!ThreadOps.IsAlive(thread))
                    continue;

                allInterpretersCache[thread] = localInterpreters;
            }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method rebuilds the per-thread interpreter cache from the
        /// master collection of all interpreters, acquiring the static lock
        /// as needed.
        /// </summary>
        /// <returns>
        /// True if the cache was rebuilt; otherwise, false.
        /// </returns>
        public static bool RebuildInterpreterCache()
        {
            bool locked = false;

            try
            {
                HardTryLock(ref locked); /* TRANSACTIONAL */

                if (locked)
                {
                    RebuildInterpreterCache(allInterpreters);
                    return true;
                }
                else
                {
                    TraceOps.LockTrace(
                        "RebuildInterpreterCache",
                        typeof(GlobalState).Name, true,
                        TracePriority.LockError,
                        MaybeWhoHasLock());

                    return false;
                }
            }
            finally
            {
                ExitLock(ref locked); /* TRANSACTIONAL */
            }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region "First" / "All" Interpreter Tracking Methods
        //
        // WARNING: This method is used during interpreter creation.
        //
        /// <summary>
        /// This method adds the specified interpreter to the global state,
        /// tracking it as the first interpreter when applicable and adding it
        /// to the master collection of all interpreters.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter to add.
        /// </param>
        /// <param name="notLocked">
        /// Upon failure, set to non-zero if the static lock could not be
        /// acquired.
        /// </param>
        /// <param name="error">
        /// Upon failure, receives information about the error.
        /// </param>
        /// <returns>
        /// The number of places the interpreter was added to within the
        /// global state.
        /// </returns>
        public static int AddInterpreter(
            Interpreter interpreter, /* in */
            ref bool notLocked,      /* out */
            ref Result error         /* out */
            ) /* THREAD-SAFE */
        {
            if (interpreter == null)
            {
                error = "invalid interpreter";
                return 0;
            }

            bool locked = false;

            try
            {
                HardTryLock(ref locked); /* TRANSACTIONAL */

                if (locked)
                {
                    int count = 0;

                    if (firstInterpreter == null)
                    {
                        firstInterpreter = interpreter;
                        count++;
                    }

                    if (allInterpreters != null)
                    {
                        allInterpreters.Add(interpreter);
                        count++;
                    }

                    if (count > 0)
                    {
                        /* NO RESULT */
                        interpreter.AddedToState();
                    }
                    else
                    {
                        error = String.Format(
                            "interpreter {0} was not added",
                            FormatOps.InterpreterNoThrow(
                            interpreter));
                    }

                    RebuildInterpreterCache(allInterpreters);
                    return count;
                }
                else
                {
                    notLocked = true;
                    error = "unable to acquire static lock";

                    TraceOps.LockTrace(
                        "AddInterpreter",
                        typeof(GlobalState).Name, true,
                        TracePriority.LockError,
                        MaybeWhoHasLock());
                }
            }
            finally
            {
                ExitLock(ref locked); /* TRANSACTIONAL */
            }

            return 0;
        }

        ///////////////////////////////////////////////////////////////////////

        //
        // WARNING: This method is used during interpreter disposal.
        //
        /// <summary>
        /// This method removes the specified interpreter from the global
        /// state, emitting diagnostic trace output if it could not be
        /// removed.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter to remove.
        /// </param>
        /// <returns>
        /// The number of places the interpreter was removed from within the
        /// global state.
        /// </returns>
        public static int RemoveInterpreter(
            Interpreter interpreter /* in */
            ) /* THREAD-SAFE */
        {
            int count;
            bool notLocked = false;
            Result error = null;

            count = RemoveInterpreter(
                interpreter, ref notLocked, ref error);

            if (count <= 0)
            {
                TraceOps.DebugTrace(String.Format(
                    "RemoveInterpreter: interpreter = {0}, " +
                    "notLocked = {1}, error = {2}",
                    FormatOps.InterpreterNoThrow(interpreter),
                    notLocked, FormatOps.WrapOrNull(error)),
                    typeof(GlobalState).Name,
                    TracePriority.CleanupError2);
            }

            return count;
        }

        ///////////////////////////////////////////////////////////////////////

        //
        // WARNING: This method is used during interpreter disposal.
        //
        /// <summary>
        /// This method removes the specified interpreter from the global
        /// state, clearing it as the first interpreter when applicable and
        /// removing it from the master collection of all interpreters.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter to remove.
        /// </param>
        /// <param name="notLocked">
        /// Upon failure, set to non-zero if the static lock could not be
        /// acquired.
        /// </param>
        /// <param name="error">
        /// Upon failure, receives information about the error.
        /// </param>
        /// <returns>
        /// The number of places the interpreter was removed from within the
        /// global state.
        /// </returns>
        private static int RemoveInterpreter(
            Interpreter interpreter, /* in */
            ref bool notLocked,      /* out */
            ref Result error         /* out */
            ) /* THREAD-SAFE */
        {
            if (interpreter == null)
            {
                error = "invalid interpreter";
                return 0;
            }

            bool locked = false;

            try
            {
                HardTryLock(ref locked); /* TRANSACTIONAL */

                if (locked)
                {
                    int count = 0;

                    if (Object.ReferenceEquals(
                            interpreter, firstInterpreter))
                    {
                        firstInterpreter = null;
                        count++;
                    }

                    if ((allInterpreters != null) &&
                        allInterpreters.Remove(interpreter))
                    {
                        count++;
                    }

                    if (count > 0)
                    {
                        /* NO RESULT */
                        interpreter.NotAddedToState();
                    }
                    else
                    {
                        error = String.Format(
                            "interpreter {0} was not removed",
                            FormatOps.InterpreterNoThrow(
                            interpreter));
                    }

                    RebuildInterpreterCache(allInterpreters);
                    return count;
                }
                else
                {
                    notLocked = true;
                    error = "unable to acquire static lock";

                    TraceOps.LockTrace(
                        "RemoveInterpreter",
                        typeof(GlobalState).Name, true,
                        TracePriority.LockError,
                        MaybeWhoHasLock());
                }
            }
            finally
            {
                ExitLock(ref locked); /* TRANSACTIONAL */
            }

            return 0;
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region "All" Interpreter Tracking Methods
        /// <summary>
        /// This method gets the first available interpreter matching the
        /// specified lookup and creation flags.
        /// </summary>
        /// <param name="lookupFlags">
        /// The flags controlling how the interpreter is looked up, including
        /// validation and verbose error reporting.
        /// </param>
        /// <param name="createFlags">
        /// The creation flags used to match the interpreter, or null to match
        /// any interpreter.
        /// </param>
        /// <param name="interpreter">
        /// Upon success, receives the matching interpreter.
        /// </param>
        /// <param name="error">
        /// Upon failure, receives information about the error.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise, an appropriate
        /// error code.
        /// </returns>
        public static ReturnCode GetInterpreter( /* NOTE: GetAnyInterpreter */
            LookupFlags lookupFlags,
            CreateFlags? createFlags,
            ref Interpreter interpreter,
            ref Result error
            ) /* THREAD-SAFE */
        {
            bool locked = false;

            try
            {
                SoftTryLock(ref locked); /* TRANSACTIONAL */

                if (locked)
                {
                    if (allInterpreters != null)
                    {
                        //
                        // NOTE: Grab the first available [and valid?]
                        //       interpreter.
                        //
                        bool validate = FlagOps.HasFlags(
                            lookupFlags, LookupFlags.Validate, true);

                        foreach (AllInterpreterPair pair in allInterpreters)
                        {
                            Interpreter localInterpreter = pair.Value;

                            if (validate && (localInterpreter == null))
                                continue;

                            if (MatchInterpreter(
                                    createFlags, localInterpreter))
                            {
                                interpreter = localInterpreter;
                                return ReturnCode.Ok;
                            }
                        }

                        error = FlagOps.HasFlags(
                            lookupFlags, LookupFlags.Verbose, true) ?
                            String.Format(
                                "no {0}interpreter found",
                                validate ? "valid " : String.Empty) :
                            "no interpreter found";
                    }
                    else
                    {
                        error = "no interpreters available";
                    }
                }
                else
                {
                    error = "unable to acquire static lock";

                    TraceOps.LockTrace(
                        "GetInterpreter",
                        typeof(GlobalState).Name, true,
                        TracePriority.LockError,
                        MaybeWhoHasLock());
                }
            }
            finally
            {
                ExitLock(ref locked); /* TRANSACTIONAL */
            }

            return ReturnCode.Error;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method gets the interpreter having the specified name that
        /// also matches the specified lookup and creation flags.
        /// </summary>
        /// <param name="name">
        /// The name of the interpreter to look up.  An empty name is
        /// permitted.
        /// </param>
        /// <param name="lookupFlags">
        /// The flags controlling how the interpreter is looked up, including
        /// validation and verbose error reporting.
        /// </param>
        /// <param name="createFlags">
        /// The creation flags used to match the interpreter, or null to match
        /// any interpreter.
        /// </param>
        /// <param name="interpreter">
        /// Upon success, receives the matching interpreter.
        /// </param>
        /// <param name="error">
        /// Upon failure, receives information about the error.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise, an appropriate
        /// error code.
        /// </returns>
        public static ReturnCode GetInterpreter(
            string name,
            LookupFlags lookupFlags,
            CreateFlags? createFlags,
            ref Interpreter interpreter,
            ref Result error
            ) /* THREAD-SAFE */
        {
            bool locked = false;

            try
            {
                SoftTryLock(ref locked); /* TRANSACTIONAL */

                if (locked)
                {
                    if (allInterpreters != null)
                    {
                        //
                        // NOTE: *WARNING* Empty interpreter names are
                        //       technically allowed, please do not
                        //       change this to "!String.IsNullOrEmpty".
                        //
                        if (name != null)
                        {
                            Interpreter localInterpreter;

                            if (allInterpreters.TryGetValue(
                                    name, out localInterpreter))
                            {
                                if ((localInterpreter != null) || !FlagOps.HasFlags(
                                        lookupFlags, LookupFlags.Validate, true))
                                {
                                    if (MatchInterpreter(
                                            createFlags, localInterpreter))
                                    {
                                        interpreter = localInterpreter;
                                        return ReturnCode.Ok;
                                    }
                                    else
                                    {
                                        error = FlagOps.HasFlags(
                                            lookupFlags, LookupFlags.Verbose, true) ?
                                            String.Format(
                                                "mismatch interpreter name {0}",
                                                FormatOps.DisplayName(name)) :
                                            "mismatch interpreter name";
                                    }
                                }
                                else
                                {
                                    error = FlagOps.HasFlags(
                                        lookupFlags, LookupFlags.Verbose, true) ?
                                        String.Format(
                                            "invalid interpreter name {0}",
                                            FormatOps.DisplayName(name)) :
                                        "invalid interpreter name";
                                }
                            }
                            else
                            {
                                error = FlagOps.HasFlags(
                                    lookupFlags, LookupFlags.Verbose, true) ?
                                    String.Format(
                                        "interpreter {0} not found",
                                        FormatOps.DisplayName(name)) :
                                    "interpreter not found";
                            }
                        }
                        else
                        {
                            error = "invalid interpreter name";
                        }
                    }
                    else
                    {
                        error = "no interpreters available";
                    }
                }
                else
                {
                    error = "unable to acquire static lock";

                    TraceOps.LockTrace(
                        "GetInterpreter",
                        typeof(GlobalState).Name, true,
                        TracePriority.LockError,
                        MaybeWhoHasLock());
                }
            }
            finally
            {
                ExitLock(ref locked); /* TRANSACTIONAL */
            }

            return ReturnCode.Error;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method formats the collection of all interpreters as a
        /// string, optionally filtered by a pattern.
        /// </summary>
        /// <param name="pattern">
        /// The optional pattern used to filter the interpreters by name, or
        /// null to include all of them.
        /// </param>
        /// <param name="noCase">
        /// Non-zero to perform pattern matching in a case-insensitive manner.
        /// </param>
        /// <returns>
        /// The string representation of the matching interpreters, or null if
        /// it could not be produced.
        /// </returns>
        public static string InterpretersToString(
            string pattern,
            bool noCase
            ) /* THREAD-SAFE */
        {
            bool locked = false;

            try
            {
                SoftTryLock(ref locked); /* TRANSACTIONAL */

                if (locked)
                {
                    if (allInterpreters != null)
                        return allInterpreters.ToString(pattern, noCase);
                }
                else
                {
                    TraceOps.LockTrace(
                        "InterpretersToString",
                        typeof(GlobalState).Name, true,
                        TracePriority.LockError,
                        MaybeWhoHasLock());
                }
            }
            finally
            {
                ExitLock(ref locked); /* TRANSACTIONAL */
            }

            return null;
        }

        ///////////////////////////////////////////////////////////////////////

        //
        // BUGFIX: *DEADLOCK* Prevent deadlocks here by using the TryLock
        //         pattern.
        //
        /// <summary>
        /// This method counts the interpreters currently tracked by the
        /// global state.
        /// </summary>
        /// <param name="withTokens">
        /// Non-zero to also include interpreters tracked only by token in the
        /// count.
        /// </param>
        /// <returns>
        /// The number of tracked interpreters, or <see cref="Count.Invalid" />
        /// if the static lock could not be acquired.
        /// </returns>
        public static int CountInterpreters(
            bool withTokens /* in */
            )
        {
            bool locked = false;

            try
            {
                SoftTryLock(ref locked); /* TRANSACTIONAL */

                if (locked)
                {
                    int count = 0;

                    if (allInterpreters != null)
                        count += allInterpreters.Count;

                    if (withTokens && (tokenInterpreters != null))
                        count += tokenInterpreters.Count;

                    return count;
                }
                else
                {
                    TraceOps.LockTrace(
                        "CountInterpreters",
                        typeof(GlobalState).Name, true,
                        TracePriority.LockError,
                        MaybeWhoHasLock());
                }
            }
            finally
            {
                ExitLock(ref locked); /* TRANSACTIONAL */
            }

            return Count.Invalid;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method gets a snapshot of all interpreters currently tracked
        /// by the global state.
        /// </summary>
        /// <returns>
        /// A new list containing the tracked interpreters, or null if it
        /// could not be produced.
        /// </returns>
        public static IEnumerable<Interpreter> GetInterpreters()
        {
            bool locked = false;

            try
            {
                SoftTryLock(ref locked); /* TRANSACTIONAL */

                if (locked)
                {
                    if (allInterpreters != null)
                    {
                        return new List<Interpreter>(
                            allInterpreters.Values);
                    }
                }
                else
                {
                    TraceOps.LockTrace(
                        "GetInterpreters",
                        typeof(GlobalState).Name, true,
                        TracePriority.LockError,
                        MaybeWhoHasLock());
                }
            }
            finally
            {
                ExitLock(ref locked); /* TRANSACTIONAL */
            }

            return null;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method gets the collection of interpreters associated with
        /// the current thread, using the per-thread cache when available.
        /// </summary>
        /// <returns>
        /// The interpreter collection for the current thread, or null if it
        /// could not be produced.
        /// </returns>
        /* THREAD-SAFE */
        public static InterpreterDictionary GetInterpreterPairs()
        {
            return GetInterpreterPairs(Thread.CurrentThread);
        }

        ///////////////////////////////////////////////////////////////////////

        //
        // BUGFIX: *DEADLOCK* Prevent deadlocks here by using the TryLock
        //         pattern.
        //
        /// <summary>
        /// This method gets the collection of interpreters associated with
        /// the specified thread, using the per-thread cache when available
        /// and populating it otherwise.
        /// </summary>
        /// <param name="thread">
        /// The thread whose interpreter collection should be returned.
        /// </param>
        /// <returns>
        /// The interpreter collection for the thread, or null if it could not
        /// be produced.
        /// </returns>
        private static InterpreterDictionary GetInterpreterPairs(
            Thread thread
            ) /* THREAD-SAFE */
        {
            bool locked = false;

            try
            {
                SoftTryLock(ref locked); /* TRANSACTIONAL */

                if (locked)
                {
                    InterpreterDictionary interpreters =
                        GetCachedInterpreters(thread);

                    if (interpreters != null)
                        return interpreters;

                    if (allInterpreters == null)
                        return null;

                    interpreters = allInterpreters.DeepCopy();

                    /* IGNORED */
                    SetCachedInterpreters(thread, interpreters);

                    return interpreters;
                }
                else
                {
                    TraceOps.LockTrace(
                        "GetInterpreterPairs",
                        typeof(GlobalState).Name, true,
                        TracePriority.LockError,
                        MaybeWhoHasLock());
                }
            }
            finally
            {
                ExitLock(ref locked); /* TRANSACTIONAL */
            }

            return null;
        }

        ///////////////////////////////////////////////////////////////////////

        //
        // BUGFIX: *DEADLOCK* Prevent deadlocks here by using the TryLock
        //         pattern.
        //
        /// <summary>
        /// This method gets a deep copy of the master collection of all
        /// interpreters.
        /// </summary>
        /// <returns>
        /// A deep copy of the interpreter collection, or null if it could not
        /// be produced.
        /// </returns>
        /* THREAD-SAFE */
        public static InterpreterDictionary CloneInterpreterPairs()
        {
            bool locked = false;

            try
            {
                SoftTryLock(ref locked); /* TRANSACTIONAL */

                if (locked)
                {
                    if (allInterpreters != null)
                        return allInterpreters.DeepCopy();
                }
                else
                {
                    TraceOps.LockTrace(
                        "CloneInterpreterPairs",
                        typeof(GlobalState).Name, true,
                        TracePriority.LockError,
                        MaybeWhoHasLock());
                }
            }
            finally
            {
                ExitLock(ref locked); /* TRANSACTIONAL */
            }

            return null;
        }

        ///////////////////////////////////////////////////////////////////////

        //
        // WARNING: This method is used during interpreter disposal.
        //
        /// <summary>
        /// This method filters the specified interpreters based on their
        /// presence in the master collection of all interpreters and,
        /// optionally, whether they belong to a non-primary thread.
        /// </summary>
        /// <param name="interpreters">
        /// The interpreters to filter.  This parameter may be null.
        /// </param>
        /// <param name="found">
        /// Non-zero to keep interpreters that are present in the master
        /// collection; zero to keep those that are absent from it.
        /// </param>
        /// <param name="nonPrimary">
        /// Non-zero to also keep interpreters that do not belong to the
        /// primary system thread.
        /// </param>
        /// <returns>
        /// The filtered list of interpreters, or null if it could not be
        /// produced.
        /// </returns>
        private static IEnumerable<IInterpreter> FilterInterpreters(
            IEnumerable<IInterpreter> interpreters,
            bool found,
            bool nonPrimary
            ) /* THREAD-SAFE */
        {
            if (interpreters == null)
                return null;

            bool locked = false;

            try
            {
                HardTryLock(ref locked); /* TRANSACTIONAL */

                if (locked)
                {
                    if (allInterpreters != null)
                    {
                        IList<IInterpreter> result = new List<IInterpreter>();

                        foreach (IInterpreter interpreter in interpreters)
                        {
                            Interpreter value = interpreter as Interpreter;

                            if (value == null)
                                continue;

                            if (allInterpreters.ContainsValue(value) == found)
                            {
                                result.Add(value);
                            }
                            else if (nonPrimary &&
                                    !value.IsPrimarySystemThread())
                            {
                                result.Add(value);
                            }
                        }

                        return result;
                    }
                }
                else
                {
                    TraceOps.LockTrace(
                        "FilterInterpreters",
                        typeof(GlobalState).Name, true,
                        TracePriority.LockError,
                        MaybeWhoHasLock());
                }
            }
            finally
            {
                ExitLock(ref locked); /* TRANSACTIONAL */
            }

            return null;
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region "Active" Interpreter Tracking Methods
        /// <summary>
        /// This method determines whether the specified interpreter is currently
        /// being tracked as an active interpreter.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter to check for active status.
        /// </param>
        /// <returns>
        /// True if the specified interpreter is active; otherwise, false.
        /// </returns>
        public static bool IsActiveInterpreter(
            Interpreter interpreter
            ) /* THREAD-SAFE */
        {
            if (activeInterpreters == null)
                return false;

            return activeInterpreters.ContainsInterpreter(interpreter);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method returns the interpreter associated with the topmost active
        /// interpreter pair, if any.
        /// </summary>
        /// <returns>
        /// The active interpreter, or null if there is no active interpreter.
        /// </returns>
        /* THREAD-SAFE */
        public static Interpreter GetActiveInterpreterOnly()
        {
            ActiveInterpreterPair anyPair = GetActiveInterpreter();
            return (anyPair != null) ? anyPair.X : null;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method returns the topmost active interpreter pair, if any.
        /// </summary>
        /// <returns>
        /// The active <see cref="ActiveInterpreterPair" />, or null if there is no
        /// active interpreter.
        /// </returns>
        /* THREAD-SAFE */
        private static ActiveInterpreterPair GetActiveInterpreter()
        {
            return GetActiveInterpreter(null);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method returns an active interpreter pair, optionally matching the
        /// specified client data type.
        /// </summary>
        /// <param name="type">
        /// The type of client data to match against the active interpreter pairs; if
        /// null, the topmost active interpreter pair is returned.
        /// </param>
        /// <returns>
        /// The matching active <see cref="ActiveInterpreterPair" />, or null if none
        /// was found.
        /// </returns>
        public static ActiveInterpreterPair GetActiveInterpreter(
            Type type
            ) /* THREAD-SAFE */
        {
            if ((activeInterpreters != null) &&
                !activeInterpreters.IsEmpty)
            {
                if (type != null)
                {
                    int count = activeInterpreters.Count;

                    for (int index = 0; index < count; index++)
                    {
                        ActiveInterpreterPair anyPair =
                            activeInterpreters.Peek(index);

                        if (anyPair == null)
                            continue;

                        IClientData clientData = anyPair.Y;

                        if (clientData == null)
                            continue;

                        if (Object.ReferenceEquals(
                                AppDomainOps.MaybeGetTypeOrObject(
                                clientData), type))
                        {
                            return anyPair;
                        }
                    }
                }
                else
                {
                    return activeInterpreters.Peek();
                }
            }

            return null;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method returns a string representation of the active interpreters,
        /// optionally filtered by a pattern.
        /// </summary>
        /// <param name="pattern">
        /// The optional pattern used to filter the active interpreters; if null, all
        /// active interpreters are included.
        /// </param>
        /// <param name="noCase">
        /// Non-zero if the pattern matching should be case-insensitive.
        /// </param>
        /// <returns>
        /// The string representation of the active interpreters, or null if there are
        /// no active interpreters.
        /// </returns>
        public static string ActiveInterpretersToString(
            string pattern,
            bool noCase
            ) /* THREAD-SAFE */
        {
            if (activeInterpreters != null)
                return activeInterpreters.ToString(pattern, noCase);
            else
                return null;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method returns a deep copy of the list of active interpreters.
        /// </summary>
        /// <returns>
        /// A deep copy of the active interpreter list, or null if there are no active
        /// interpreters.
        /// </returns>
        /* THREAD-SAFE */
        private static InterpreterStackList GetActiveInterpreters()
        {
            return (activeInterpreters != null) ?
                activeInterpreters.DeepCopy() : null;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method filters a sequence of interpreters based on their active status
        /// and whether they are running on a non-primary thread.
        /// </summary>
        /// <param name="interpreters">
        /// The sequence of interpreters to filter.
        /// </param>
        /// <param name="found">
        /// The active status to match; an interpreter is included when its active
        /// status equals this value.
        /// </param>
        /// <param name="nonPrimary">
        /// Non-zero to also include interpreters that are not running on the primary
        /// system thread.
        /// </param>
        /// <returns>
        /// The filtered sequence of interpreters, or null if no filtering could be
        /// performed.
        /// </returns>
        public static IEnumerable<IInterpreter> FilterActiveInterpreters(
            IEnumerable<IInterpreter> interpreters,
            bool found,
            bool nonPrimary
            ) /* THREAD-SAFE */
        {
            if (interpreters == null)
                return null;

            if (activeInterpreters != null)
            {
                IList<IInterpreter> result = new List<IInterpreter>();

                foreach (IInterpreter interpreter in interpreters)
                {
                    Interpreter value = interpreter as Interpreter;

                    if (value == null)
                        continue;

                    if (activeInterpreters.ContainsInterpreter(value) == found)
                        result.Add(value);
                    else if (nonPrimary && !value.IsPrimarySystemThread())
                        result.Add(value);
                }

                return result;
            }

            return null;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method counts the number of times the specified interpreter appears in
        /// the active interpreter list.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter to count; if null, the total number of active interpreters
        /// is returned.
        /// </param>
        /// <returns>
        /// The number of matching active interpreters.
        /// </returns>
        public static int CountActiveInterpreters(
            Interpreter interpreter
            ) /* THREAD-SAFE */
        {
            int result = 0;

            if ((activeInterpreters != null) && !activeInterpreters.IsEmpty)
            {
                if (interpreter != null)
                {
                    int count = activeInterpreters.Count;

                    for (int index = 0; index < count; index++)
                    {
                        ActiveInterpreterPair anyPair =
                            activeInterpreters.Peek(index);

                        if (anyPair == null)
                            continue;

                        Interpreter activeInterpreter = anyPair.X;

                        if (activeInterpreter == null)
                            continue;

                        if (Object.ReferenceEquals(
                                activeInterpreter, interpreter))
                        {
                            result++;
                        }
                    }
                }
                else
                {
                    result += activeInterpreters.Count;
                }
            }

            return result;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method removes the specified interpreter from the active interpreter
        /// list, or clears all active interpreters.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter to remove; if null, all active interpreters are removed.
        /// </param>
        /// <returns>
        /// The number of active interpreters that were removed.
        /// </returns>
        public static int ClearActiveInterpreters(
            Interpreter interpreter
            ) /* THREAD-SAFE */
        {
            int result = 0;

            if ((activeInterpreters != null) && !activeInterpreters.IsEmpty)
            {
                if (interpreter != null)
                {
                    int count = activeInterpreters.Count;

                    for (int index = count - 1; index >= 0; index--)
                    {
                        ActiveInterpreterPair anyPair =
                            activeInterpreters.Peek(index);

                        if (anyPair == null)
                            continue;

                        Interpreter activeInterpreter = anyPair.X;

                        if (activeInterpreter == null)
                            continue;

                        if (!Object.ReferenceEquals(
                                activeInterpreter, interpreter))
                        {
                            continue;
                        }

                        activeInterpreters.RemoveAt(index);
                        result++;
                    }
                }
                else
                {
                    result += activeInterpreters.Count;
                    activeInterpreters.Clear();
                }
            }

            return result;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method peeks at the topmost interpreter on the specified stack and
        /// checks whether it matches the specified interpreter.
        /// </summary>
        /// <param name="interpreters">
        /// The interpreter stack to peek at.
        /// </param>
        /// <param name="interpreter">
        /// The optional interpreter to match against the topmost stack entry; if null,
        /// any topmost entry matches.
        /// </param>
        /// <returns>
        /// True if a matching topmost entry was found; otherwise, false.
        /// </returns>
        private static bool PeekAndCheckInterpreter(
            InterpreterStackList interpreters, /* in */
            Interpreter interpreter            /* in: OPTIONAL */
            )
        {
            ActiveInterpreterPair anyPair; /* NOT USED */

            return PeekAndCheckInterpreter(
                interpreters, interpreter, out anyPair);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method peeks at the topmost interpreter on the specified stack, checks
        /// whether it matches the specified interpreter, and returns the matching pair.
        /// </summary>
        /// <param name="interpreters">
        /// The interpreter stack to peek at.
        /// </param>
        /// <param name="interpreter">
        /// The optional interpreter to match against the topmost stack entry; if null,
        /// any topmost entry matches.
        /// </param>
        /// <param name="anyPair">
        /// Upon success, receives the matching active interpreter pair; otherwise, it
        /// is set to null.
        /// </param>
        /// <returns>
        /// True if a matching topmost entry was found; otherwise, false.
        /// </returns>
        private static bool PeekAndCheckInterpreter(
            InterpreterStackList interpreters, /* in */
            Interpreter interpreter,           /* in: OPTIONAL */
            out ActiveInterpreterPair anyPair  /* out */
            )
        {
            anyPair = null;

            ActiveInterpreterPair localAnyPair;
            Interpreter localInterpreter;

            try
            {
                if ((interpreters != null) && !interpreters.IsEmpty)
                {
                    localAnyPair = interpreters.Peek();

                    if (interpreter == null)
                    {
                        anyPair = localAnyPair;
                        return true;
                    }

                    if (localAnyPair != null)
                    {
                        localInterpreter = localAnyPair.X;

                        if (Object.ReferenceEquals(
                                localInterpreter, interpreter))
                        {
                            anyPair = localAnyPair;
                            return true;
                        }
                    }
                }

                return false;
            }
            finally
            {
                localInterpreter = null;
                localAnyPair = null;
            }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method associates the specified log client data with the topmost active
        /// interpreter pair, pushing a new active interpreter if necessary.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter to push if there is no active interpreter pair.
        /// </param>
        /// <param name="clientData">
        /// The log client data to associate with the active interpreter; if null, this
        /// method does nothing.
        /// </param>
        /// <param name="pushed">
        /// A counter that is incremented when an active interpreter is pushed.
        /// </param>
        public static void MaybePushActiveLogClientData(
            Interpreter interpreter,
            IClientData clientData,
            ref int pushed
            ) /* THREAD-SAFE */
        {
            if (clientData == null)
                return;

            ActiveInterpreterPair anyPair = PeekActiveInterpreter();

            if (anyPair == null)
            {
                PushActiveInterpreter(interpreter, clientData, ref pushed);
                return;
            }

            IBaseClientData baseClientData = anyPair.Y as IBaseClientData;

            if ((baseClientData != null) && (baseClientData.Log == null))
            {
                baseClientData.Log = clientData; /* ScriptLogClientData? */
                return;
            }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method clears the log client data associated with the topmost active
        /// interpreter pair, optionally popping the active interpreter.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter to match when popping the active interpreter.
        /// </param>
        /// <param name="pushed">
        /// A counter tracking the number of pushed active interpreters.
        /// </param>
        /// <returns>
        /// The affected active <see cref="ActiveInterpreterPair" />, or null if there
        /// was no active interpreter.
        /// </returns>
        public static ActiveInterpreterPair MaybePopActiveLogClientData(
            Interpreter interpreter,
            ref int pushed
            ) /* THREAD-SAFE */
        {
            if (Interlocked.Increment(ref pushed) > 1) /* RARE */
            {
                try
                {
                    return MaybePopActiveInterpreter(
                        interpreter, ref pushed);
                }
                finally
                {
                    /* IGNORED */
                    Interlocked.Decrement(ref pushed);
                }
            }
            else
            {
                /* IGNORED */
                Interlocked.Decrement(ref pushed);
            }

            ///////////////////////////////////////////////////////////////////

            ActiveInterpreterPair anyPair = PeekActiveInterpreter();

            if (anyPair == null)
                return null;

            IBaseClientData baseClientData = anyPair.Y as IBaseClientData;

            if ((baseClientData != null) && (baseClientData.Log != null))
                baseClientData.Log = null; /* ScriptLogClientData? */

            return anyPair;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method returns the total number of active interpreters across all
        /// threads.
        /// </summary>
        /// <returns>
        /// The total active interpreter count.
        /// </returns>
        public static long GetTotalActiveCount()
        {
            return Interlocked.CompareExchange(
                ref totalActiveCount, 0, 0);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method atomically increments the total active interpreter count.
        /// </summary>
        /// <returns>
        /// The new total active interpreter count.
        /// </returns>
        private static long IncreaseActiveCount()
        {
            return Interlocked.Increment(ref totalActiveCount);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method atomically decrements the total active interpreter count.
        /// </summary>
        /// <returns>
        /// The new total active interpreter count.
        /// </returns>
        private static long DecreaseActiveCount()
        {
            return Interlocked.Decrement(ref totalActiveCount);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method pushes the specified interpreter onto the active interpreter
        /// stack.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter to push onto the active interpreter stack.
        /// </param>
        public static void PushActiveInterpreter(
            Interpreter interpreter
            ) /* THREAD-SAFE */
        {
            PushActiveInterpreter(interpreter, null);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method pushes the specified interpreter and its associated client data
        /// onto the active interpreter stack.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter to push onto the active interpreter stack.
        /// </param>
        /// <param name="clientData">
        /// The client data to associate with the pushed interpreter.
        /// </param>
        public static void PushActiveInterpreter(
            Interpreter interpreter,
            IClientData clientData
            ) /* THREAD-SAFE */
        {
            int pushed = 0;

            PushActiveInterpreter(interpreter, clientData, ref pushed);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method pushes the specified interpreter and its associated client data
        /// onto the active interpreter stack, updating the active counters and issuing
        /// notifications.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter to push onto the active interpreter stack.
        /// </param>
        /// <param name="clientData">
        /// The client data to associate with the pushed interpreter.
        /// </param>
        /// <param name="pushed">
        /// A counter that is incremented when the interpreter is pushed.
        /// </param>
        private static void PushActiveInterpreter(
            Interpreter interpreter,
            IClientData clientData,
            ref int pushed
            ) /* THREAD-SAFE */
        {
            if (activeInterpreters == null)
                activeInterpreters = new InterpreterStackList();

            activeInterpreters.Push(
                new MutableAnyPair<Interpreter, IClientData>(
                    true, interpreter, clientData));

            /* IGNORED */
            Interlocked.Increment(ref pushed);

            /* IGNORED */
            IncreaseActiveCount();

            ///////////////////////////////////////////////////////////////////

            if (interpreter != null)
            {
                /* IGNORED */
                interpreter.IncreaseActiveCount();
            }

            ///////////////////////////////////////////////////////////////////
            // BEGIN NOT THREAD-SAFE
            ///////////////////////////////////////////////////////////////////

#if NOTIFY && NOTIFY_GLOBAL && NOTIFY_ACTIVE
            if ((interpreter != null) &&
                interpreter.ShouldGlobalNotify)
            {
                /* IGNORED */
                Interpreter.CheckNotifications(null, false,
                    NotifyType.Interpreter, NotifyFlags.Pushed,
                    null, interpreter, clientData, null, null);
            }
#endif

            ///////////////////////////////////////////////////////////////////
            // END NOT THREAD-SAFE
            ///////////////////////////////////////////////////////////////////
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method returns the topmost active interpreter pair without removing it
        /// from the active interpreter stack.
        /// </summary>
        /// <returns>
        /// The topmost active <see cref="ActiveInterpreterPair" />, or null if there is
        /// no active interpreter.
        /// </returns>
        /* THREAD-SAFE */
        public static ActiveInterpreterPair PeekActiveInterpreter()
        {
            int pushed = 1; // required, or no peek.

            return MaybePeekActiveInterpreter(null, ref pushed);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method returns the topmost active interpreter pair if it matches the
        /// specified interpreter.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter to match against the topmost active interpreter pair.
        /// </param>
        /// <returns>
        /// The matching active <see cref="ActiveInterpreterPair" />, or null if none
        /// matched.
        /// </returns>
        public static ActiveInterpreterPair MaybePeekActiveInterpreter(
            Interpreter interpreter
            ) /* THREAD-SAFE */
        {
            int pushed = 1; // required, or no peek.

            return MaybePopActiveInterpreter(interpreter, ref pushed);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method returns the topmost active interpreter pair if it matches the
        /// specified interpreter and an interpreter has been pushed.
        /// </summary>
        /// <param name="interpreter">
        /// The optional interpreter to match against the topmost active interpreter
        /// pair; if null, the topmost pair is returned.
        /// </param>
        /// <param name="pushed">
        /// A counter tracking the number of pushed active interpreters; it is
        /// decremented when a matching pair is found.
        /// </param>
        /// <returns>
        /// The matching active <see cref="ActiveInterpreterPair" />, or null if none
        /// matched.
        /// </returns>
        private static ActiveInterpreterPair MaybePeekActiveInterpreter(
            Interpreter interpreter,
            ref int pushed
            ) /* THREAD-SAFE */
        {
            ActiveInterpreterPair anyPair = null;

            if (Interlocked.CompareExchange(ref pushed, 0, 0) > 0)
            {
                if ((activeInterpreters != null) &&
                    !activeInterpreters.IsEmpty)
                {
                    if (interpreter != null)
                    {
                        ActiveInterpreterPair localAnyPair;

                        if (PeekAndCheckInterpreter(
                                activeInterpreters, interpreter,
                                out localAnyPair))
                        {
                            anyPair = localAnyPair;

                            /* IGNORED */
                            Interlocked.Decrement(ref pushed);
                        }
                    }
                    else
                    {
                        anyPair = activeInterpreters.Peek();

                        /* IGNORED */
                        Interlocked.Decrement(ref pushed);
                    }
                }
            }

            return anyPair;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method removes and returns the topmost active interpreter pair from the
        /// active interpreter stack.
        /// </summary>
        /// <returns>
        /// The removed active <see cref="ActiveInterpreterPair" />, or null if there is
        /// no active interpreter.
        /// </returns>
        /* THREAD-SAFE */
        public static ActiveInterpreterPair PopActiveInterpreter()
        {
            int pushed = 1; // required, or no pop.

            return MaybePopActiveInterpreter(null, ref pushed);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method removes and returns the topmost active interpreter pair if it
        /// matches the specified interpreter.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter to match against the topmost active interpreter pair.
        /// </param>
        /// <returns>
        /// The removed active <see cref="ActiveInterpreterPair" />, or null if none
        /// matched.
        /// </returns>
        public static ActiveInterpreterPair MaybePopActiveInterpreter(
            Interpreter interpreter
            ) /* THREAD-SAFE */
        {
            int pushed = 1; // required, or no pop.

            return MaybePopActiveInterpreter(interpreter, ref pushed);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method removes and returns the topmost active interpreter pair if it
        /// matches the specified interpreter and an interpreter has been pushed,
        /// updating the active counters and issuing notifications.
        /// </summary>
        /// <param name="interpreter">
        /// The optional interpreter to match against the topmost active interpreter
        /// pair; if null, the topmost pair is removed.
        /// </param>
        /// <param name="pushed">
        /// A counter tracking the number of pushed active interpreters; it is
        /// decremented when a pair is removed.
        /// </param>
        /// <returns>
        /// The removed active <see cref="ActiveInterpreterPair" />, or null if none
        /// matched.
        /// </returns>
        private static ActiveInterpreterPair MaybePopActiveInterpreter(
            Interpreter interpreter,
            ref int pushed
            ) /* THREAD-SAFE */
        {
            ActiveInterpreterPair anyPair = null; /* REUSED */

            if (Interlocked.CompareExchange(ref pushed, 0, 0) > 0)
            {
                if ((activeInterpreters != null) &&
                    !activeInterpreters.IsEmpty)
                {
                    if ((interpreter != null) &&
                        !PeekAndCheckInterpreter(
                            activeInterpreters, interpreter))
                    {
                        return null;
                    }

                    anyPair = activeInterpreters.Pop();

                    /* IGNORED */
                    Interlocked.Decrement(ref pushed);

                    /* IGNORED */
                    DecreaseActiveCount();
                }
            }

            ///////////////////////////////////////////////////////////////////

            Interpreter localInterpreter = null;

            if (anyPair != null)
            {
                localInterpreter = anyPair.X;

                if (localInterpreter != null)
                {
                    /* IGNORED */
                    localInterpreter.DecreaseActiveCount();
                }
            }

            ///////////////////////////////////////////////////////////////////
            // BEGIN NOT THREAD-SAFE
            ///////////////////////////////////////////////////////////////////

#if NOTIFY && NOTIFY_GLOBAL && NOTIFY_ACTIVE
            if ((localInterpreter != null) &&
                localInterpreter.ShouldGlobalNotify)
            {
                IClientData clientData = (anyPair != null) ?
                    anyPair.Y : null;

                /* IGNORED */
                Interpreter.CheckNotifications(null, false,
                    NotifyType.Interpreter, NotifyFlags.Popped,
                    null, localInterpreter, clientData, null,
                    null);
            }
#endif

            ///////////////////////////////////////////////////////////////////
            // END NOT THREAD-SAFE
            ///////////////////////////////////////////////////////////////////

            return anyPair;
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region "Token" Interpreter Tracking Methods
        /// <summary>
        /// This method returns the interpreter associated with the specified token.
        /// </summary>
        /// <param name="token">
        /// The token identifying the interpreter to retrieve.
        /// </param>
        /// <returns>
        /// The interpreter associated with the specified token, or null if no such
        /// interpreter exists.
        /// </returns>
        public static Interpreter GetTokenInterpreter(
            ulong token
            ) /* THREAD-SAFE */
        {
            Interpreter interpreter;
            Result error = null;

            interpreter = GetTokenInterpreter(token, ref error);

            if (interpreter != null)
                return interpreter;

            TraceOps.DebugTrace(String.Format(
                "GetTokenInterpreter: error = {0}", FormatOps.WrapOrNull(
                error)), typeof(GlobalState).Name, TracePriority.StartupError2);

            return null;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method returns the interpreter associated with the specified token,
        /// reporting any error encountered.
        /// </summary>
        /// <param name="token">
        /// The token identifying the interpreter to retrieve.
        /// </param>
        /// <param name="error">
        /// Upon failure, receives information about the error encountered.
        /// </param>
        /// <returns>
        /// The interpreter associated with the specified token, or null if no such
        /// interpreter exists.
        /// </returns>
        public static Interpreter GetTokenInterpreter(
            ulong token,
            ref Result error
            ) /* THREAD-SAFE */
        {
            bool notLocked = false;
            bool notFound = false;

            return GetTokenInterpreter(
                token, ref notLocked, ref notFound, ref error);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method returns the interpreter associated with the specified token,
        /// reporting whether the static lock could not be acquired, whether the token
        /// could not be found, and any error encountered.
        /// </summary>
        /// <param name="token">
        /// The token identifying the interpreter to retrieve.
        /// </param>
        /// <param name="notLocked">
        /// Upon return, set to non-zero if the static lock could not be acquired.
        /// </param>
        /// <param name="notFound">
        /// Upon return, set to non-zero if no interpreter matched the specified token.
        /// </param>
        /// <param name="error">
        /// Upon failure, receives information about the error encountered.
        /// </param>
        /// <returns>
        /// The interpreter associated with the specified token, or null if no such
        /// interpreter exists.
        /// </returns>
        //
        // WARNING: This method is used during interpreter creation.
        //
        public static Interpreter GetTokenInterpreter(
            ulong token,
            ref bool notLocked,
            ref bool notFound,
            ref Result error
            ) /* THREAD-SAFE */
        {
            bool locked = false;

            try
            {
                HardTryLock(ref locked); /* TRANSACTIONAL */

                if (locked)
                {
                    if (tokenInterpreters != null)
                    {
                        Interpreter interpreter;

                        if (tokenInterpreters.TryGetValue(
                                token, out interpreter))
                        {
                            return interpreter;
                        }
                        else
                        {
                            notFound = true;
                            error = "unmatched interpreter token";
                        }
                    }
                    else
                    {
                        error = "no interpreters available";
                    }
                }
                else
                {
                    notLocked = true;
                    error = "unable to acquire static lock";

                    TraceOps.LockTrace(
                        "GetTokenInterpreter",
                        typeof(GlobalState).Name, true,
                        TracePriority.LockError,
                        MaybeWhoHasLock());
                }
            }
            finally
            {
                ExitLock(ref locked); /* TRANSACTIONAL */
            }

            return null;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method returns the first available interpreter from the token
        /// interpreter collection.
        /// </summary>
        /// <returns>
        /// An available interpreter, or null if none was found.
        /// </returns>
        private static Interpreter GetAnyTokenInterpreter()
        {
            Interpreter interpreter = null;
            Result error = null; /* NOT USED */

            if (GetAnyTokenInterpreter(
                    LookupFlags.Interpreter, null, ref interpreter,
                    ref error) == ReturnCode.Ok)
            {
                return interpreter;
            }

            return null;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method returns the first available interpreter from the token
        /// interpreter collection that matches the specified lookup and creation flags.
        /// </summary>
        /// <param name="lookupFlags">
        /// The flags controlling how interpreters are looked up and validated.
        /// </param>
        /// <param name="createFlags">
        /// The optional creation flags an interpreter must match; if null, no creation
        /// flag matching is performed.
        /// </param>
        /// <param name="interpreter">
        /// Upon success, receives the matching interpreter.
        /// </param>
        /// <param name="error">
        /// Upon failure, receives information about the error encountered.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise, an appropriate error
        /// code.
        /// </returns>
        private static ReturnCode GetAnyTokenInterpreter(
            LookupFlags lookupFlags,
            CreateFlags? createFlags,
            ref Interpreter interpreter,
            ref Result error
            ) /* THREAD-SAFE */
        {
            bool locked = false;

            try
            {
                SoftTryLock(ref locked); /* TRANSACTIONAL */

                if (locked)
                {
                    if (tokenInterpreters != null)
                    {
                        //
                        // NOTE: Grab the first available [and valid?]
                        //       interpreter.
                        //
                        bool validate = FlagOps.HasFlags(
                            lookupFlags, LookupFlags.Validate, true);

                        foreach (TokenInterpreterPair pair in tokenInterpreters)
                        {
                            Interpreter localInterpreter = pair.Value;

                            if (validate && (localInterpreter == null))
                                continue;

                            if (MatchInterpreter(
                                    createFlags, localInterpreter))
                            {
                                interpreter = localInterpreter;
                                return ReturnCode.Ok;
                            }
                        }

                        error = FlagOps.HasFlags(
                            lookupFlags, LookupFlags.Verbose, true) ?
                            String.Format(
                                "no {0}interpreter found",
                                validate ? "valid " : String.Empty) :
                            "no interpreter found";
                    }
                    else
                    {
                        error = "no interpreters available";
                    }
                }
                else
                {
                    error = "unable to acquire static lock";

                    TraceOps.LockTrace(
                        "GetAnyTokenInterpreter",
                        typeof(GlobalState).Name, true,
                        TracePriority.LockError,
                        MaybeWhoHasLock());
                }
            }
            finally
            {
                ExitLock(ref locked); /* TRANSACTIONAL */
            }

            return ReturnCode.Error;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method adds the specified interpreter to the token interpreter
        /// collection, keyed by its token.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter to add to the token interpreter collection.
        /// </param>
        /// <param name="notLocked">
        /// Upon return, set to non-zero if the static lock could not be acquired.
        /// </param>
        /// <param name="error">
        /// Upon failure, receives information about the error encountered.
        /// </param>
        /// <returns>
        /// True if the interpreter was added; otherwise, false.
        /// </returns>
        //
        // WARNING: This method is used during interpreter creation.
        //
        public static bool AddTokenInterpreter(
            Interpreter interpreter,
            ref bool notLocked,
            ref Result error
            )
        {
            if (interpreter == null)
            {
                error = "invalid interpreter";
                return false;
            }

            ulong? token = interpreter.Token;

            if (token == null)
            {
                error = "invalid interpreter token";
                return false;
            }

            ulong localToken = (ulong)token;

            if (localToken == 0)
            {
                error = "zero interpreter token";
                return false;
            }

            bool locked = false;

            try
            {
                HardTryLock(ref locked); /* TRANSACTIONAL */

                if (locked)
                {
                    if (tokenInterpreters != null)
                    {
                        if (!tokenInterpreters.ContainsKey(
                                localToken))
                        {
                            tokenInterpreters.Add(
                                localToken, interpreter);

                            /* NO RESULT */
                            interpreter.AddedToState();

                            return true;
                        }
                        else
                        {
                            error = "duplicate interpreter token";
                        }
                    }
                    else
                    {
                        error = "no interpreters available";
                    }
                }
                else
                {
                    notLocked = true;
                    error = "unable to acquire static lock";

                    TraceOps.LockTrace(
                        "AddTokenInterpreter",
                        typeof(GlobalState).Name, true,
                        TracePriority.LockError,
                        MaybeWhoHasLock());
                }
            }
            finally
            {
                ExitLock(ref locked); /* TRANSACTIONAL */
            }

            return false;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method removes the specified interpreter from the token interpreter
        /// collection.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter to remove from the token interpreter collection.
        /// </param>
        /// <returns>
        /// True if the interpreter was removed; otherwise, false.
        /// </returns>
        public static bool RemoveTokenInterpreter(
            Interpreter interpreter
            )
        {
            bool notLocked = false;
            Result error = null;

            if (RemoveTokenInterpreter(
                    interpreter, ref notLocked, ref error))
            {
                return true;
            }

            TraceOps.DebugTrace(String.Format(
                "RemoveTokenInterpreter: interpreter = {0}, " +
                "notLocked = {1}, error = {2}",
                FormatOps.InterpreterNoThrow(interpreter),
                notLocked, FormatOps.WrapOrNull(error)),
                typeof(GlobalState).Name,
                TracePriority.CleanupError2);

            return false;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method removes the specified interpreter from the token interpreter
        /// collection, reporting whether the static lock could not be acquired and any
        /// error encountered.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter to remove from the token interpreter collection.
        /// </param>
        /// <param name="notLocked">
        /// Upon return, set to non-zero if the static lock could not be acquired.
        /// </param>
        /// <param name="error">
        /// Upon failure, receives information about the error encountered.
        /// </param>
        /// <returns>
        /// True if the interpreter was removed; otherwise, false.
        /// </returns>
        //
        // WARNING: This method is used during interpreter disposal.
        //
        private static bool RemoveTokenInterpreter(
            Interpreter interpreter,
            ref bool notLocked,
            ref Result error
            )
        {
            if (interpreter == null)
            {
                error = "invalid interpreter";
                return false;
            }

            ulong? token = interpreter.Token;

            if (token == null)
                return true;

            ulong localToken = (ulong)token;

            if (localToken == 0)
            {
                error = "zero interpreter token";
                return false;
            }

            bool locked = false;

            try
            {
                HardTryLock(ref locked); /* TRANSACTIONAL */

                if (locked)
                {
                    if (tokenInterpreters != null)
                    {
                        if (tokenInterpreters.Remove(localToken))
                        {
                            /* NO RESULT */
                            interpreter.NotAddedToState();

                            return true;
                        }
                        else
                        {
                            error = "missing interpreter token";
                        }
                    }
                    else
                    {
                        error = "no interpreters available";
                    }
                }
                else
                {
                    notLocked = true;
                    error = "unable to acquire static lock";

                    TraceOps.LockTrace(
                        "RemoveTokenInterpreter",
                        typeof(GlobalState).Name, true,
                        TracePriority.LockError,
                        MaybeWhoHasLock());
                }
            }
            finally
            {
                ExitLock(ref locked); /* TRANSACTIONAL */
            }

            return false;
        }
        #endregion
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region "All" Thread Tracking Methods
        /// <summary>
        /// This method adds the specified engine thread to the global collection of
        /// tracked threads.
        /// </summary>
        /// <param name="engineThread">
        /// The engine thread to add to the global thread collection.
        /// </param>
        /// <returns>
        /// True if the engine thread was added; otherwise, false.
        /// </returns>
        public static bool AddThread(
            EngineThread engineThread
            ) /* THREAD-SAFE */
        {
            bool locked = false;

            try
            {
                FirmTryLock(ref locked); /* TRANSACTIONAL */

                if (locked)
                {
                    if ((engineThread != null) && (allEngineThreads != null))
                    {
                        allEngineThreads.Add(engineThread);
                        return true;
                    }
                }
                else
                {
                    TraceOps.LockTrace(
                        "AddThread",
                        typeof(GlobalState).Name, true,
                        TracePriority.LockError,
                        MaybeWhoHasLock());
                }
            }
            finally
            {
                ExitLock(ref locked); /* TRANSACTIONAL */
            }

            return false;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method removes the specified engine thread from the global collection
        /// of tracked threads.
        /// </summary>
        /// <param name="engineThread">
        /// The engine thread to remove from the global thread collection.
        /// </param>
        /// <returns>
        /// True if the engine thread was removed; otherwise, false.
        /// </returns>
        public static bool RemoveThread(
            EngineThread engineThread
            ) /* THREAD-SAFE */
        {
            bool locked = false;

            try
            {
                FirmTryLock(ref locked); /* TRANSACTIONAL */

                if (locked)
                {
                    if ((engineThread != null) && (allEngineThreads != null))
                        return allEngineThreads.Remove(engineThread);
                }
                else
                {
                    TraceOps.LockTrace(
                        "RemoveThread",
                        typeof(GlobalState).Name, true,
                        TracePriority.LockError,
                        MaybeWhoHasLock());
                }
            }
            finally
            {
                ExitLock(ref locked); /* TRANSACTIONAL */
            }

            return false;
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Context Manager Support Methods
#if THREADING
        /// <summary>
        /// This method filters the specified list of interpreters down to those
        /// whose contexts are eligible to be purged, excluding any interpreters
        /// that are not currently valid as well as any that are present on the
        /// active interpreter stack.
        /// </summary>
        /// <param name="interpreters">
        /// The list of interpreters to be filtered.  This value may be null.
        /// </param>
        /// <param name="nonPrimary">
        /// When true, primary interpreters are excluded from the resulting list.
        /// </param>
        /// <returns>
        /// The filtered list of interpreters whose contexts may be purged.
        /// </returns>
        public static IEnumerable<IInterpreter> FilterInterpretersToPurge(
            IEnumerable<IInterpreter> interpreters,
            bool nonPrimary
            )
        {
            //
            // NOTE: First, filter the specified list of interpreters to
            //       those that are not present in the list of all valid
            //       (i.e. created and not disposed) interpreters.
            //
            IEnumerable<IInterpreter> result = FilterInterpreters(
                interpreters, false, nonPrimary);

            //
            // HACK: If an interpreter is present on the active stack, we
            //       never want to purge its contexts.
            //
            result = FilterActiveInterpreters(result, false, false);

            //
            // NOTE: Finally, return the resulting list of interpreters.
            //
            return result;
        }
#endif
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Policy Trace Support Properties
#if POLICY_TRACE
        /// <summary>
        /// Gets or sets a value indicating whether diagnostic tracing of policy
        /// decisions is enabled.  This property is thread-safe.
        /// </summary>
        public static bool PolicyTrace /* THREAD-SAFE */
        {
            get
            {
                return Interlocked.CompareExchange(
                    ref policyTrace, 0, 0) != 0;
            }
            set
            {
                Interlocked.Exchange(
                    ref policyTrace, value ? 1 : 0);
            }
        }
#endif
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Version Support Methods
        /// <summary>
        /// This method compares two version numbers and returns the one that is
        /// considered more specific, optionally comparing them when their parts
        /// differ or when no more specific version can be determined.
        /// </summary>
        /// <param name="version1">
        /// The first version to consider.  This value may be null.
        /// </param>
        /// <param name="version2">
        /// The second version to consider.  This value may be null.
        /// </param>
        /// <param name="errorOnNull">
        /// When true, null is returned if either specified version is null.
        /// </param>
        /// <param name="stopOnNotEqual">
        /// When true, processing stops at the first version part that differs
        /// between the two versions.
        /// </param>
        /// <param name="compareOnNotEqual">
        /// When true and a differing version part is encountered, the two
        /// versions are compared to select the result; otherwise, null is
        /// returned.
        /// </param>
        /// <param name="compareOnNotFound">
        /// When true and no more specific version part can be found, the two
        /// versions are compared to select the result.
        /// </param>
        /// <returns>
        /// The version considered more specific, or null if one cannot be
        /// determined.
        /// </returns>
        public static Version GetMoreSpecificVersion(
            Version version1,
            Version version2,
            bool errorOnNull,
            bool stopOnNotEqual,
            bool compareOnNotEqual,
            bool compareOnNotFound
            )
        {
            if ((version1 == null) || (version2 == null))
            {
                if (errorOnNull)
                {
                    return null;
                }
                else
                {
                    return (version1 != null) ?
                        version1 : version2;
                }
            }

            int[] parts1 = {
                version1.Major, version1.Minor,
                version1.Build, version1.Revision
            };

            int[] parts2 = {
                version2.Major, version2.Minor,
                version2.Build, version2.Revision
            };

            int length = Math.Min(parts1.Length, parts2.Length);

            for (int index = 0; index < length; index++)
            {
                int part1 = parts1[index];
                int part2 = parts2[index];

                if (part1 > 0)
                {
                    if (part2 > 0)
                    {
                        if (stopOnNotEqual &&
                            (part1 != part2))
                        {
                            if (compareOnNotEqual)
                            {
                                if (PackageOps.VersionCompare(
                                        version1, version2) >= 0)
                                {
                                    return version1;
                                }
                                else
                                {
                                    return version2;
                                }
                            }
                            else
                            {
                                return null;
                            }
                        }
                    }
                    else
                    {
                        return version1;
                    }
                }
                else if (part2 > 0)
                {
                    return version2;
                }
            }

            if (compareOnNotFound)
            {
                if (PackageOps.VersionCompare(
                        version1, version2) >= 0)
                {
                    return version1;
                }
                else
                {
                    return version2;
                }
            }
            else
            {
                return version1; // TODO: Good idea?
            }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method creates a version number from the specified major and
        /// minor components, replacing any component below the minimum allowed
        /// value with zero.
        /// </summary>
        /// <param name="major">
        /// The major version component.
        /// </param>
        /// <param name="minor">
        /// The minor version component.
        /// </param>
        /// <returns>
        /// The constructed version.  This method cannot return null.
        /// </returns>
        public static Version GetTwoPartVersion( /* CANNOT RETURN NULL */
            int major,
            int minor
            )
        {
            int newMajor;

            if (major >= _Constants._Version.Minimum)
                newMajor = major;
            else
                newMajor = 0;

            int newMinor;

            if (minor >= _Constants._Version.Minimum)
                newMinor = minor;
            else
                newMinor = 0;

            return new Version(newMajor, newMinor);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method creates a version number containing only the major and
        /// minor components of the specified version.
        /// </summary>
        /// <param name="version">
        /// The version whose major and minor components are used.  This value
        /// may be null.
        /// </param>
        /// <returns>
        /// The constructed two-part version, or null if the specified version
        /// is null.
        /// </returns>
        public static Version GetTwoPartVersion( /* MAY RETURN NULL */
            Version version
            )
        {
            if (version == null)
                return null;

            return GetTwoPartVersion(version.Major, version.Minor);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method creates a version number from the specified major, minor,
        /// and build components, replacing any component below the minimum
        /// allowed value with zero.
        /// </summary>
        /// <param name="major">
        /// The major version component.
        /// </param>
        /// <param name="minor">
        /// The minor version component.
        /// </param>
        /// <param name="build">
        /// The build version component.
        /// </param>
        /// <returns>
        /// The constructed version.  This method cannot return null.
        /// </returns>
        public static Version GetThreePartVersion( /* CANNOT RETURN NULL */
            int major,
            int minor,
            int build
            )
        {
            int newMajor;

            if (major >= _Constants._Version.Minimum)
                newMajor = major;
            else
                newMajor = 0;

            int newMinor;

            if (minor >= _Constants._Version.Minimum)
                newMinor = minor;
            else
                newMinor = 0;

            int newBuild;

            if (build >= _Constants._Version.Minimum)
                newBuild = build;
            else
                newBuild = 0;

            return new Version(newMajor, newMinor, newBuild);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method creates a version number containing only the major,
        /// minor, and build components of the specified version.
        /// </summary>
        /// <param name="version">
        /// The version whose major, minor, and build components are used.  This
        /// value may be null.
        /// </param>
        /// <returns>
        /// The constructed three-part version, or null if the specified version
        /// is null.
        /// </returns>
        public static Version GetThreePartVersion( /* MAY RETURN NULL */
            Version version
            )
        {
            if (version == null)
                return null;

            return GetThreePartVersion(
                version.Major, version.Minor, version.Build);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method creates a version number from the specified major, minor,
        /// build, and revision components, replacing any component below the
        /// minimum allowed value with zero.
        /// </summary>
        /// <param name="major">
        /// The major version component.
        /// </param>
        /// <param name="minor">
        /// The minor version component.
        /// </param>
        /// <param name="build">
        /// The build version component.
        /// </param>
        /// <param name="revision">
        /// The revision version component.
        /// </param>
        /// <returns>
        /// The constructed version.  This method cannot return null.
        /// </returns>
        public static Version GetFourPartVersion( /* CANNOT RETURN NULL */
            int major,
            int minor,
            int build,
            int revision
            )
        {
            int newMajor;

            if (major >= _Constants._Version.Minimum)
                newMajor = major;
            else
                newMajor = 0;

            int newMinor;

            if (minor >= _Constants._Version.Minimum)
                newMinor = minor;
            else
                newMinor = 0;

            int newBuild;

            if (build >= _Constants._Version.Minimum)
                newBuild = build;
            else
                newBuild = 0;

            int newRevision;

            if (revision >= _Constants._Version.Minimum)
                newRevision = revision;
            else
                newRevision = 0;

            return new Version(newMajor, newMinor, newBuild, newRevision);
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Assembly Plugin Flags Support Methods
        /// <summary>
        /// This method determines whether the plugin flags for the core library
        /// assembly can be quickly obtained from the cached value without further
        /// calculation.  This method assumes the global lock is held.
        /// </summary>
        /// <param name="anyTriplet">
        /// The plugin data triplet associated with the request.  This value may
        /// be null.
        /// </param>
        /// <returns>
        /// True if the cached assembly plugin flags may be used; otherwise,
        /// false.
        /// </returns>
        //
        // NOTE: This method assumes the global lock is held.
        //
        /* ASYNCHRONOUS */
        private static bool ShouldTryFastGrabAssemblyPluginFlags(
            PluginDataTriplet anyTriplet /* in */
            )
        {
            return (anyTriplet != null) && !anyTriplet.Z &&
                (thisAssemblyPluginFlags != null);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method attempts to obtain the plugin flags for the core library
        /// assembly from the cached value.  This method assumes the global lock
        /// is held.
        /// </summary>
        /// <param name="pluginFlags">
        /// Upon success, receives the cached assembly plugin flags.  Upon
        /// failure, receives <see cref="PluginFlags.None" />.
        /// </param>
        /// <returns>
        /// True if the cached assembly plugin flags were available; otherwise,
        /// false.
        /// </returns>
        //
        // NOTE: This method assumes the global lock is held.
        //
        /* SYNCHRONOUS / ASYNCHRONOUS */
        private static bool TryFastGrabAssemblyPluginFlags(
            out PluginFlags pluginFlags /* out */
            )
        {
            pluginFlags = PluginFlags.None;

            if (thisAssemblyPluginFlags != null)
            {
                pluginFlags = (PluginFlags)thisAssemblyPluginFlags;
                return true;
            }

            return false;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method determines the assembly associated with the specified
        /// plugin data triplet, falling back to the core library assembly when
        /// one cannot be determined.  This method does not require the global
        /// lock.
        /// </summary>
        /// <param name="anyTriplet">
        /// The plugin data triplet associated with the request.  This value may
        /// be null.
        /// </param>
        /// <param name="assembly">
        /// Upon return, receives the resolved assembly, which falls back to the
        /// core library assembly.
        /// </param>
        //
        // NOTE: This method does not require the global lock.
        //
        /* ASYNCHRONOUS */
        private static void SomeAssemblyRequired(
            PluginDataTriplet anyTriplet, /* in: OPTIONAL */
            out Assembly assembly         /* out */
            )
        {
            if (anyTriplet != null)
            {
                IPluginData pluginData = anyTriplet.Y;

                if ((pluginData != null) &&
                    !AppDomainOps.IsCross(pluginData))
                {
                    try
                    {
                        assembly = pluginData.Assembly;

                        if (assembly != null)
                            return;
                    }
                    catch (Exception e)
                    {
                        TraceOps.DebugTrace(
                            e, typeof(GlobalState).Name,
                            TracePriority.SecurityError);
                    }
                }
            }

            assembly = thisAssembly;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method calculates the plugin flags for the specified assembly,
        /// using the specified list of trusted hashes.  This method does not
        /// require the global lock.
        /// </summary>
        /// <param name="hashes">
        /// The list of trusted hashes to use when calculating the plugin flags.
        /// This value may be null.
        /// </param>
        /// <param name="assembly">
        /// The assembly whose plugin flags are calculated.  This value may be
        /// null.
        /// </param>
        /// <param name="pluginFlags">
        /// Upon return, receives the calculated assembly plugin flags.
        /// </param>
        //
        // NOTE: This method does not require the global lock.
        //
        /* ASYNCHRONOUS */
        private static void GetAssemblyPluginFlags(
            StringList hashes,          /* in: OPTIONAL */
            Assembly assembly,          /* in: OPTIONAL */
            out PluginFlags pluginFlags /* out */
            )
        {
            pluginFlags = RuntimeOps.GetAssemblyPluginFlags(
                null, hashes, assembly);

            TraceOps.DebugTrace(String.Format(
                "GetAssemblyPluginFlags: " +
                "hashes = {0}, assembly = {1}, " +
                "pluginFlags = {2}",
                FormatOps.WrapOrNull(hashes),
                FormatOps.WrapOrNull(assembly),
                FormatOps.WrapOrNull(pluginFlags)),
                typeof(GlobalState).Name,
                TracePriority.StartupDebug2);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method is the queued work item callback used to calculate and
        /// store the plugin flags for the core library assembly, optionally
        /// combining them into the flags of an associated plugin.
        /// </summary>
        /// <param name="state">
        /// The callback state, which should be a plugin data triplet, or null.
        /// </param>
        /* ASYNCHRONOUS */
        private static void AssemblyPluginFlagsCallback(
            object state /* in, out */
            )
        {
            try
            {
                //
                // HACK: If this is not the first time this callback
                //       has been invoked, wait a bit prior to seeing
                //       if we really need to get the assembly plugin
                //       flags.
                //
                if (Interlocked.Increment(
                        ref thisAssemblyPluginFlagsCount) > 1)
                {
                    HostOps.ThreadSleep(ThreadOps.GetDefaultTimeout(
                        null, TimeoutType.Start)); /* throw */
                }

                PluginDataTriplet anyTriplet = state as PluginDataTriplet;
                PluginFlags pluginFlags;

                ///////////////////////////////////////////////////////////////

                //
                // NOTE: Do the plugin flags for the core library assembly
                //       need to be calculated and stored, e.g. signature
                //       checks, etc?
                //
                if (!ShouldTryFastGrabAssemblyPluginFlags(anyTriplet) ||
                    !TryFastGrabAssemblyPluginFlags(out pluginFlags))
                {
                    Assembly assembly;

                    /* NO RESULT */
                    SomeAssemblyRequired(null, out assembly);

                    /* NO RESULT */
                    GetAssemblyPluginFlags(
                        anyTriplet.X, assembly, out pluginFlags);

                    /* IGNORED */
                    SetAssemblyPluginFlags(pluginFlags);
                }

                ///////////////////////////////////////////////////////////////

                if (anyTriplet != null)
                {
                    IPluginData pluginData = anyTriplet.Y;

                    if (pluginData != null)
                        pluginData.Flags |= pluginFlags;
                }
            }
            catch (ThreadAbortException e)
            {
                Thread.ResetAbort();

                TraceOps.DebugTrace(
                    e, typeof(GlobalState).Name,
                    TracePriority.ThreadError2);
            }
            catch (ThreadInterruptedException e)
            {
                TraceOps.DebugTrace(
                    e, typeof(GlobalState).Name,
                    TracePriority.ThreadError2);
            }
            catch (Exception e)
            {
                TraceOps.DebugTrace(
                    e, typeof(GlobalState).Name,
                    TracePriority.StartupError);
            }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method populates the plugin flags for the core library assembly,
        /// applying them to the specified plugin data synchronously when possible
        /// and otherwise queuing the work to be performed asynchronously.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter associated with the request.  This value may be null.
        /// </param>
        /// <param name="pluginData">
        /// The plugin data whose flags are populated.  This value may be null.
        /// </param>
        /// <param name="refresh">
        /// When true, the cached assembly plugin flags are recalculated instead
        /// of being reused.
        /// </param>
        /// <returns>
        /// True if the plugin flags were applied synchronously or the work was
        /// successfully queued; otherwise, false.
        /// </returns>
        /* SYNCHRONOUS */
        public static bool PopulateAssemblyPluginFlags(
            Interpreter interpreter,
            IPluginData pluginData,
            bool refresh
            )
        {
            #region Try Setting Plugin Flags Synchronously
            if ((pluginData != null) && !refresh)
            {
                bool locked = false;

                try
                {
                    SoftTryLock(ref locked); /* TRANSACTIONAL */

                    if (locked)
                    {
                        PluginFlags pluginFlags;

                        if (TryFastGrabAssemblyPluginFlags(
                                out pluginFlags))
                        {
                            pluginData.Flags |= pluginFlags;
                            return true;
                        }
                    }
                    else
                    {
                        TraceOps.LockTrace(
                            "PopulateAssemblyPluginFlags",
                            typeof(GlobalState).Name, true,
                            TracePriority.LockWarning2,
                            MaybeWhoHasLock());
                    }
                }
                finally
                {
                    ExitLock(ref locked); /* TRANSACTIONAL */
                }
            }
            #endregion

            ///////////////////////////////////////////////////////////////////

            #region Queue Setting Plugin Flags Asynchronously
            return Engine.QueueWorkItem(
                interpreter, AssemblyPluginFlagsCallback,
                new PluginDataTriplet(
                    RuntimeOps.CombineOrCopyTrustedHashes(
                        interpreter, false, false, false),
                    pluginData, refresh),
                ThreadOps.GetQueueFlags(false));
            #endregion
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Environment Variable Wrapper Methods
        /// <summary>
        /// This method gets the value of the specified environment variable.
        /// This method is thread-safe.
        /// </summary>
        /// <param name="variable">
        /// The name of the environment variable to query.
        /// </param>
        /// <returns>
        /// The value of the environment variable, or null if it is not set.
        /// </returns>
        private static string GetEnvironmentVariable(
            string variable
            ) /* THREAD-SAFE */
        {
            return CommonOps.Environment.GetVariable(variable);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method sets the value of the specified environment variable.
        /// This method is thread-safe.
        /// </summary>
        /// <param name="variable">
        /// The name of the environment variable to set.
        /// </param>
        /// <param name="value">
        /// The value to assign to the environment variable.
        /// </param>
        /// <returns>
        /// True if the environment variable was set successfully; otherwise,
        /// false.
        /// </returns>
        private static bool SetEnvironmentVariable(
            string variable,
            string value
            ) /* THREAD-SAFE */
        {
            return CommonOps.Environment.SetVariable(variable, value);
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Application Domain Variable Access Methods
        /// <summary>
        /// This method gets the application domain associated with the global
        /// state.  This method is thread-safe.
        /// </summary>
        /// <returns>
        /// The application domain, or null if one is not available.
        /// </returns>
        public static AppDomain GetAppDomain() /* THREAD-SAFE */
        {
            return appDomain;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method gets the base directory of the application domain
        /// associated with the global state.  This method is thread-safe.
        /// </summary>
        /// <returns>
        /// The application domain base directory, or null if one is not
        /// available.
        /// </returns>
        public static string GetAppDomainBaseDirectory() /* THREAD-SAFE */
        {
            return appDomainBaseDirectory;
        }

        ///////////////////////////////////////////////////////////////////////

#if APPDOMAINS || ISOLATED_INTERPRETERS || ISOLATED_PLUGINS
        /// <summary>
        /// This method verifies that the core library assembly is located
        /// underneath the base directory of the application domain.  This method
        /// is thread-safe.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter associated with the verification.  This value may be
        /// null.
        /// </param>
        /// <param name="friendlyName">
        /// The friendly name of the application domain being verified.
        /// </param>
        /// <returns>
        /// True if the core library assembly resides underneath the application
        /// domain base directory; otherwise, false.
        /// </returns>
        public static bool VerifyAppDomainBaseDirectory(
            Interpreter interpreter,
            string friendlyName
            ) /* THREAD-SAFE */
        {
            Result error = null;

            return VerifyAppDomainBaseDirectory(
                interpreter, friendlyName, ref error);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method verifies that the core library assembly is located
        /// underneath the base directory of the application domain.  This method
        /// is thread-safe.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter associated with the verification.  This value may be
        /// null.
        /// </param>
        /// <param name="friendlyName">
        /// The friendly name of the application domain being verified.
        /// </param>
        /// <param name="error">
        /// Upon failure, receives an error message describing why the
        /// verification did not succeed.
        /// </param>
        /// <returns>
        /// True if the core library assembly resides underneath the application
        /// domain base directory; otherwise, false.
        /// </returns>
        public static bool VerifyAppDomainBaseDirectory(
            Interpreter interpreter,
            string friendlyName,
            ref Result error
            ) /* THREAD-SAFE */
        {
            //
            // HACK: For the (isolated) plugin loader to actually work right,
            //       the core library assembly must be located underneath the
            //       application domain base directory; otherwise, the plugin
            //       assembly cannot properly reference types from the core
            //       library assembly (e.g. using Eagle via Garuda from a Tcl
            //       shell that is located somewhere else, which ends up with
            //       an application domain base directory like "C:\Tcl\bin").
            //
            string baseDirectory = GetAppDomainBaseDirectory();

            if (String.IsNullOrEmpty(baseDirectory))
            {
                error = String.Format(
                    "cannot use application domain base directory " +
                    "for interpreter {0} ({1}) because it is invalid",
                    FormatOps.InterpreterNoThrow(interpreter),
                    FormatOps.WrapOrNull(friendlyName));

                return false;
            }

            string location = GetAssemblyLocation();

            if (String.IsNullOrEmpty(location))
            {
                error = String.Format(
                    "cannot use application domain base directory " +
                    "{0} for interpreter {1} ({2}) because core " +
                    "library assembly location is invalid",
                    FormatOps.DisplayPath(baseDirectory),
                    FormatOps.InterpreterNoThrow(interpreter),
                    FormatOps.WrapOrNull(friendlyName));

                return false;
            }

            if (PathOps.IsUnderPath(interpreter, location, baseDirectory))
            {
                return true;
            }
            else
            {
                error = String.Format(
                    "cannot use application domain base directory {0} " +
                    "for interpreter {1} ({2}) because core library " +
                    "assembly location {3} is not underneath it",
                    FormatOps.DisplayPath(baseDirectory),
                    FormatOps.InterpreterNoThrow(interpreter),
                    FormatOps.WrapOrNull(friendlyName),
                    FormatOps.DisplayPath(location));

                return false;
            }
        }
#endif
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Assembly Global Variable Access Methods
        #region Stub Assembly Access Methods
        /// <summary>
        /// This method gets the directory path used when locating the stub
        /// assembly.
        /// </summary>
        /// <returns>
        /// The configured stub assembly path, if available; otherwise, the
        /// default assembly path.
        /// </returns>
        private static string GetStubAssemblyPath()
        {
            string path = CommonOps.Environment.GetVariable(
                EnvVars.StubPath);

            if (path != null)
                return path;

            return AlwaysGetAssemblyPath();
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method gets the file name (without any directory information)
        /// for the stub assembly.
        /// </summary>
        /// <returns>
        /// The stub assembly file name, without any directory information.
        /// </returns>
        private static string GetStubAssemblyFileNameOnly()
        {
            return String.Format("{0}{1}",
                StubAssemblyName, FileExtension.Library);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method gets the fully qualified file name for the stub
        /// assembly.
        /// </summary>
        /// <returns>
        /// The fully qualified stub assembly file name, including its
        /// directory.
        /// </returns>
        public static string GetStubAssemblyFileName()
        {
            return Path.Combine(
                GetStubAssemblyPath(), GetStubAssemblyFileNameOnly());
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method determines whether the specified type belongs to the
        /// stub assembly.
        /// </summary>
        /// <param name="type">
        /// The type to check.
        /// </param>
        /// <returns>
        /// True if the specified type belongs to the stub assembly;
        /// otherwise, false.
        /// </returns>
        private static bool IsStubAssemblyType(
            Type type /* in */
            )
        {
            if (type == null)
                return false;

            Assembly assembly = type.Assembly;

            if (assembly == null)
                return false;

            AssemblyName assemblyName = assembly.GetName();

            if (assemblyName == null)
                return false;

            return SharedStringOps.SystemEquals(
                assemblyName.Name, StubAssemblyName);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method gets, caching it if necessary, the reflected method
        /// information for the stub type "Execute" method.
        /// </summary>
        /// <param name="type">
        /// The stub type from which to obtain the "Execute" method.
        /// </param>
        /// <returns>
        /// The reflected method information for the stub "Execute" method, or
        /// null if it could not be obtained.
        /// </returns>
        private static MethodInfo GetStubExecuteMethodInfo(
            Type type /* in */
            )
        {
            bool locked = false;

            try
            {
                HardTryLock(ref locked); /* TRANSACTIONAL */

                if (locked)
                {
                    if ((StubExecuteMethodInfo == null) &&
                        (type != null))
                    {
                        StubExecuteMethodInfo = type.GetMethod(
                            "Execute", ObjectOps.GetBindingFlags(
                            MetaBindingFlags.PublicInstanceMethod,
                            true));
                    }

                    return StubExecuteMethodInfo;
                }
                else
                {
                    TraceOps.LockTrace(
                        "GetStubExecuteMethodInfo",
                        typeof(GlobalState).Name, true,
                        TracePriority.LockError,
                        MaybeWhoHasLock());
                }
            }
            finally
            {
                ExitLock(ref locked); /* TRANSACTIONAL */
            }

            return null;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method invokes the stub assembly, if present, in the specified
        /// application domain.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter context to use, or null for none.
        /// </param>
        /// <param name="clientData">
        /// The optional client data to pass to the stub, or null for none.
        /// </param>
        /// <param name="arguments">
        /// The optional arguments to pass to the stub, or null for none.
        /// </param>
        /// <param name="appDomain">
        /// The application domain to search, or null to use the current one.
        /// </param>
        /// <param name="result">
        /// Upon return, receives the result produced by the stub.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise, an appropriate
        /// error code.
        /// </returns>
        public static ReturnCode ExecuteStubAssemblyInAppDomain(
            Interpreter interpreter, /* in: OPTIONAL */
            IClientData clientData,  /* in: OPTIONAL */
            ArgumentList arguments,  /* in: OPTIONAL */
            AppDomain appDomain,     /* in: OPTIONAL */
            ref Result result        /* out */
            )
        {
            bool allowCreation; /* NOT USED */

            return IsStubAssemblyInAppDomain(
                interpreter, clientData, arguments, appDomain,
                false, out allowCreation, ref result);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method determines whether the stub assembly is present in the
        /// specified application domain, optionally invoking it.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter context to use, or null for none.
        /// </param>
        /// <param name="clientData">
        /// The optional client data to pass to the stub, or null for none.
        /// </param>
        /// <param name="arguments">
        /// The optional arguments to pass to the stub, or null for none.
        /// </param>
        /// <param name="appDomain">
        /// The application domain to search, or null to use the current one.
        /// </param>
        /// <param name="noInvoke">
        /// Non-zero to only check for the presence of the stub type, without
        /// invoking it.
        /// </param>
        /// <param name="allowCreation">
        /// Upon return, indicates whether the stub explicitly allowed
        /// interpreter creation to proceed.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> if the stub type is present;
        /// <see cref="ReturnCode.Continue" /> if it is not present.
        /// </returns>
        public static ReturnCode IsStubAssemblyInAppDomain(
            Interpreter interpreter, /* in: OPTIONAL */
            IClientData clientData,  /* in: OPTIONAL */
            ArgumentList arguments,  /* in: OPTIONAL */
            AppDomain appDomain,     /* in: OPTIONAL */
            bool noInvoke,           /* in */
            out bool allowCreation   /* out */
            )
        {
            Result result = null;

            return IsStubAssemblyInAppDomain(
                interpreter, clientData, arguments, appDomain,
                noInvoke, out allowCreation, ref result);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method determines whether the stub assembly is present in the
        /// specified application domain, optionally invoking it and capturing
        /// its result.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter context to use, or null for none.
        /// </param>
        /// <param name="clientData">
        /// The optional client data to pass to the stub, or null for none.
        /// </param>
        /// <param name="arguments">
        /// The optional arguments to pass to the stub, or null for none.
        /// </param>
        /// <param name="appDomain">
        /// The application domain to search, or null to use the current one.
        /// </param>
        /// <param name="noInvoke">
        /// Non-zero to only check for the presence of the stub type, without
        /// invoking it.
        /// </param>
        /// <param name="allowCreation">
        /// Upon return, indicates whether the stub explicitly allowed
        /// interpreter creation to proceed.
        /// </param>
        /// <param name="result">
        /// Upon return, receives the result produced by the stub, when it is
        /// invoked.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> if the stub type is present;
        /// <see cref="ReturnCode.Continue" /> if it is not present, or if the
        /// stub explicitly allowed interpreter creation to proceed.
        /// </returns>
        private static ReturnCode IsStubAssemblyInAppDomain(
            Interpreter interpreter, /* in: OPTIONAL */
            IClientData clientData,  /* in: OPTIONAL */
            ArgumentList arguments,  /* in: OPTIONAL */
            AppDomain appDomain,     /* in: OPTIONAL */
            bool noInvoke,           /* in */
            out bool allowCreation,  /* out */
            ref Result result        /* in, out */
            )
        {
            allowCreation = false;

            AppDomain localAppDomain = (appDomain != null) ?
                appDomain : AppDomainOps.GetCurrent();

            ValueFlags valueFlags = ValueFlags.SkipTypeGetType;

            valueFlags |= Value.GetTypeValueFlags(false, false, false);

            CultureInfo cultureInfo = null;

            if (interpreter != null)
                cultureInfo = interpreter.InternalCultureInfo;

            //
            // HACK: Do not pass the interpreter instance to this type
            //       lookup call because it will repeatedly attempt to
            //       acquire the interpreter lock, waiting a bit each
            //       time.  This can cause other threads to timeout if
            //       they are in the midst of creating an interpreter.
            //
            Type type = null;

            if ((Value.GetAnyType(null, StubAssemblyTypeName,
                    null, localAppDomain, valueFlags, cultureInfo,
                    ref type) == ReturnCode.Ok) &&
                (type != null) && IsStubAssemblyType(type))
            {
                if (noInvoke)
                    return ReturnCode.Ok; /* NOTE: Stub type present. */

                IExecute stub = null;

                try
                {
                    MethodInfo methodInfo = GetStubExecuteMethodInfo(type);

                    if (methodInfo == null)
                        return ReturnCode.Ok; /* NOTE: Stub type present. */

#if !NET_STANDARD_20
                    stub = localAppDomain.CreateInstanceAndUnwrap(
                        StubAssemblyName, StubAssemblyTypeName) as IExecute;
#else
                    stub = Activator.CreateInstance(type) as IExecute;
#endif

                    if (stub == null)
                        return ReturnCode.Ok; /* NOTE: Stub type present. */

                    /*
                     * Eagle._Components.Public.Delegates.ExecuteCallback
                     */

                    Result localResult = result;

                    object[] args = {
                        interpreter, /* Eagle._Components.Public.Interpreter */
                        clientData,  /* Eagle._Interfaces.Public.IClientData */
                        arguments,   /* Eagle._Containers.Public.ArgumentList */
                        localResult  /* Eagle._Components.Public.Result& */
                    };

                    int length = args.Length;

                    ReturnCode code = (ReturnCode)methodInfo.Invoke(
                        stub, args); /* throw */

                    localResult = args[length - 1] as Result;
                    result = localResult;

                    TracePriority priority = (code == ReturnCode.Ok) ?
                        TracePriority.SecurityDebug3 :
                        TracePriority.SecurityError;

                    TraceOps.DebugTrace(String.Format(
                        "IsStubAssemblyInAppDomain: " +
                        "assembly = {0}, interpreter = {1}, " +
                        "arguments = {2}, code = {3}, result = {4}",
                        FormatOps.AssemblyLocation(type, true),
                        FormatOps.InterpreterNoThrow(interpreter),
                        FormatOps.WrapOrNull(arguments), code,
                        FormatOps.WrapOrNull(localResult)),
                        typeof(GlobalState).Name, priority);

                    if ((code == ReturnCode.Ok) &&
                        SharedStringOps.SystemEquals(
                            localResult, String.Format(StubOkResultFormat,
                            GetCurrentThreadId())))
                    {
                        //
                        // HACK: If we get to this point, a properly signed
                        //       stub assembly is loaded in this AppDomain
                        //       and it has explicitly allowed interpreter
                        //       creation to proceed.
                        //
                        TraceOps.DebugTrace(String.Format(
                            "IsStubAssemblyInAppDomain: CREATION " +
                            "WAS ALLOWED via assembly {0} using " +
                            "interpreter {1}",
                            FormatOps.AssemblyLocation(type, true),
                            FormatOps.InterpreterNoThrow(interpreter)),
                            typeof(GlobalState).Name,
                            TracePriority.SecurityDebug);

                        allowCreation = true;

                        //
                        // NOTE: Fake stub type not present.
                        //
                        return ReturnCode.Continue;
                    }
                }
                catch (Exception e)
                {
                    TraceOps.DebugTrace(
                        e, typeof(GlobalState).Name,
                        TracePriority.SecurityError);
                }
                finally
                {
                    ObjectOps.DisposeOrTrace<IExecute>(
                        null, ref stub);

                    stub = null;
                }

                return ReturnCode.Ok; /* NOTE: Stub type present. */
            }

            //
            // NOTE: Stub type not present.
            //
            return ReturnCode.Continue;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method determines whether the stub assembly is loaded as a
        /// module within the specified process.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter context to use when comparing file names, or null
        /// for none.
        /// </param>
        /// <param name="process">
        /// The process to search, or null to use the current process.
        /// </param>
        /// <returns>
        /// True if the stub assembly is loaded within the process; otherwise,
        /// false.
        /// </returns>
        public static bool IsStubAssemblyInProcess(
            Interpreter interpreter, /* in: OPTIONAL */
            Process process          /* in: OPTIONAL */
            )
        {
            Process localProcess = (process != null) ?
                process : ProcessOps.GetCurrent();

            try
            {
                IEnumerable<ProcessModule> modules = ProcessOps.GetModules(
                    localProcess); /* throw */

                if (modules != null)
                {
                    string fileName = GetStubAssemblyFileName();
                    string fileNameOnly = GetStubAssemblyFileNameOnly();

                    foreach (ProcessModule module in modules)
                    {
                        if (module == null)
                            continue;

                        string moduleFileName = module.FileName;

                        string moduleFileNameOnly = Path.GetFileName(
                            moduleFileName);

                        if (!PathOps.IsEqualFileName(
                                moduleFileNameOnly, fileNameOnly))
                        {
                            continue;
                        }

                        if (PathOps.IsSameFile(
                                interpreter, moduleFileName, fileName))
                        {
                            return true;
                        }
                    }
                }
            }
            catch (Exception e)
            {
                TraceOps.DebugTrace(
                    e, typeof(GlobalState).Name,
                    TracePriority.SecurityError);
            }

            return false;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method determines whether the stub assembly is present in the
        /// current application domain or loaded within the current process.
        /// </summary>
        /// <returns>
        /// True if the stub assembly is present in either location; otherwise,
        /// false.
        /// </returns>
        public static bool IsStubAssemblyAnywhere()
        {
            bool allowCreation;

            if (IsStubAssemblyInAppDomain(
                    null, null, null, null, true,
                    out allowCreation) == ReturnCode.Ok)
            {
                return true;
            }

            if (IsStubAssemblyInProcess(null, null))
                return true;

            return false;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method attempts to cache the stub assembly bytes, emitting a
        /// trace message upon failure.
        /// </summary>
        public static void TryToCacheStubAssemblyOrTrace()
        {
            ReturnCode code;
            Result error = null;

            code = TryToCacheStubAssembly(ref error);

            if (code != ReturnCode.Ok)
            {
                TraceOps.DebugTrace(String.Format(
                    "TryToCacheStubAssemblyOrTrace: {0}",
                    FormatOps.WrapOrNull(error)),
                    typeof(GlobalState).Name,
                    TracePriority.SecurityError);
            }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method attempts to read the stub assembly file and cache its
        /// bytes for later use.
        /// </summary>
        /// <param name="error">
        /// Upon failure, receives information about the error that was
        /// encountered.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise, an appropriate
        /// error code.
        /// </returns>
        public static ReturnCode TryToCacheStubAssembly(
            ref Result error /* out */
            )
        {
            string fileName = GetStubAssemblyFileName();

            if (String.IsNullOrEmpty(fileName))
            {
                error = "stub assembly file name is invalid";
                return ReturnCode.Error;
            }

            if (!File.Exists(fileName))
            {
                error = "stub assembly file name does not exist";
                return ReturnCode.Error;
            }

            bool locked = false;

            try
            {
                HardTryLock(ref locked); /* TRANSACTIONAL */

                if (locked)
                {
                    byte[] bytes; /* REUSED */
                    int length; /* REUSED */

                    bytes = stubAssemblyBytes;

                    if (bytes != null)
                    {
                        length = bytes.Length;

                        if (length >= minimumStubAssemblyFileSize)
                            return ReturnCode.Ok;
                    }

                    bytes = File.ReadAllBytes(fileName); /* throw */

                    if (bytes == null)
                    {
                        error = "could not read stub assembly file";
                        return ReturnCode.Error;
                    }

                    length = bytes.Length;

                    if (length < minimumStubAssemblyFileSize)
                    {
                        error = "stub assembly file is too small";
                        return ReturnCode.Error;
                    }

                    stubAssemblyBytes = bytes;
                    return ReturnCode.Ok;
                }
                else
                {
                    TraceOps.LockTrace(
                        "TryToCacheStubAssembly",
                        typeof(GlobalState).Name, true,
                        TracePriority.LockError,
                        MaybeWhoHasLock());
                }
            }
            catch (Exception e)
            {
                error = e;
            }
            finally
            {
                ExitLock(ref locked); /* TRANSACTIONAL */
            }

            return ReturnCode.Error;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method attempts to write the cached stub assembly bytes to the
        /// specified file.
        /// </summary>
        /// <param name="fileName">
        /// The file name to which the cached stub assembly bytes should be
        /// written.
        /// </param>
        /// <param name="clearCache">
        /// Non-zero to clear the cached stub assembly bytes after they have
        /// been written.
        /// </param>
        /// <param name="error">
        /// Upon failure, receives information about the error that was
        /// encountered.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise, an appropriate
        /// error code.
        /// </returns>
        private static ReturnCode TryToWriteCachedStubAssembly(
            string fileName, /* in */
            bool clearCache, /* in */
            ref Result error /* out */
            )
        {
            bool locked = false;

            try
            {
                HardTryLock(ref locked); /* TRANSACTIONAL */

                if (locked)
                {
                    byte[] bytes = stubAssemblyBytes;

                    if (bytes == null)
                    {
                        error = "missing cached stub assembly bytes";
                        return ReturnCode.Error;
                    }

                    int length = bytes.Length;

                    if (length < minimumStubAssemblyFileSize)
                    {
                        error = "not enough stub assembly bytes";
                        return ReturnCode.Error;
                    }

                    File.WriteAllBytes(fileName, bytes); /* throw */

                    if (clearCache)
                        stubAssemblyBytes = null;

                    return ReturnCode.Ok;
                }
                else
                {
                    TraceOps.LockTrace(
                        "TryToWriteStubAssembly",
                        typeof(GlobalState).Name, true,
                        TracePriority.LockError,
                        MaybeWhoHasLock());
                }
            }
            catch (Exception e)
            {
                error = e;
            }
            finally
            {
                ExitLock(ref locked); /* TRANSACTIONAL */
            }

            return ReturnCode.Error;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method attempts to verify and load the stub assembly,
        /// rewriting it from the cached bytes first if the file is missing.
        /// </summary>
        /// <param name="clientData">
        /// The optional client data used when verifying the assembly, or null
        /// for none.
        /// </param>
        /// <param name="useDefault">
        /// Non-zero to load the stub assembly into the default application
        /// domain, when supported.
        /// </param>
        /// <param name="error">
        /// Upon failure, receives information about the error that was
        /// encountered.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise, an appropriate
        /// error code.
        /// </returns>
        public static ReturnCode TryToLoadStubAssembly(
            IClientData clientData, /* in: NOT USED */
            bool useDefault,        /* in */
            ref Result error        /* out */
            )
        {
            //
            // HACK: If the stub assembly file is not present for
            //       some reason, e.g. it was deleted (?), try to
            //       write it from the cached bytes, if possible.
            //       This is being done as a convenience, not for
            //       security.  If it is missing and there are no
            //       cached bytes, this will fail, which is fine,
            //       as loading would fail (just below) anyhow.
            //
            string fileName = GetStubAssemblyFileName();

            if (!File.Exists(fileName) &&
                (TryToWriteCachedStubAssembly(
                    fileName, true, ref error) != ReturnCode.Ok))
            {
                return ReturnCode.Error;
            }

            byte[] publicKeyToken = GetAssemblyPublicKeyToken();

            if (AssemblyOps.VerifyFromFile(
                    fileName, publicKeyToken, clientData,
                    ref error) != ReturnCode.Ok)
            {
                return ReturnCode.Error;
            }

#if !NET_STANDARD_20 && REMOTING && NATIVE && WINDOWS
            IExecute stub = null;
#endif

            try
            {
#if !NET_STANDARD_20 && REMOTING && NATIVE && WINDOWS
                if (useDefault && !AppDomainOps.IsCurrentDefault() &&
                    AppDomainOps.CanGetDefault())
                {
                    _AppDomain appDomain =
                        AppDomainOps.GetDefault() as _AppDomain;

                    if (appDomain != null)
                    {
                        ObjectHandle handle = appDomain.CreateInstanceFrom(
                            fileName, StubAssemblyTypeName);

                        if (handle == null)
                        {
                            error = "could not create stub object handle";
                            return ReturnCode.Error;
                        }

                        stub = handle.Unwrap() as IExecute;

                        if (stub == null)
                        {
                            error = "could not unwrap stub object handle";
                            return ReturnCode.Error;
                        }

                        Result result = null;

                        if (stub.Execute(null, clientData, null,
                                ref result) != ReturnCode.Error)
                        {
                            error = "stub returned unexpected code";
                            return ReturnCode.Error;
                        }

                        if (!SharedStringOps.SystemEquals(
                                result, StubErrorResult))
                        {
                            error = "stub returned unexpected result";
                            return ReturnCode.Error;
                        }
                    }
                    else
                    {
                        error = "could not get default application domain";
                        return ReturnCode.Error;
                    }
                }
                else
#endif
                {
                    Assembly.LoadFrom(fileName); /* throw */
                }

                return ReturnCode.Ok;
            }
            catch (Exception e)
            {
                TraceOps.DebugTrace(
                    e, typeof(GlobalState).Name,
                    TracePriority.SecurityError);

                error = "stub assembly file name could not be loaded";
                return ReturnCode.Error;
            }
#if !NET_STANDARD_20 && REMOTING && NATIVE && WINDOWS
            finally
            {
                ObjectOps.DisposeOrTrace<IExecute>(
                    null, ref stub);

                stub = null;
            }
#endif
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Entry Assembly Variable Access Methods
        /// <summary>
        /// This method refreshes the cached entry assembly information, using
        /// the specified assembly or the detected entry assembly.
        /// </summary>
        /// <param name="assembly">
        /// The assembly to use as the entry assembly, or null to detect it
        /// automatically.
        /// </param>
        /// <returns>
        /// Always returns true.
        /// </returns>
        public static bool RefreshEntryAssembly(
            Assembly assembly /* in: OPTIONAL */
            )
        {
            entryAssembly = (assembly != null) ?
                assembly : FindEntryAssembly();

            entryAssemblyName = (entryAssembly != null) ?
                entryAssembly.GetName() : null;

#if DEAD_CODE
            entryAssemblyTitle = SharedAttributeOps.GetAssemblyTitle(
                entryAssembly);
#endif

            entryAssemblyLocation = (entryAssembly != null) ?
                entryAssembly.Location : null;

            entryAssemblyVersion = (entryAssemblyName != null) ?
                entryAssemblyName.Version : null;

            /* IGNORED */
            InitializeEntryAssemblyPath(true);

            return true;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method locates the entry assembly, falling back to the
        /// executing assembly when no entry assembly is available.
        /// </summary>
        /// <returns>
        /// The entry assembly, or the executing assembly when no entry
        /// assembly is available. This method never returns null.
        /// </returns>
        private static Assembly FindEntryAssembly() /* CANNOT RETURN NULL */
        {
            Assembly assembly = Assembly.GetEntryAssembly(); /* NULL? */

            if (assembly != null)
            {
                TraceOps.DebugTrace(String.Format(
                    "FindEntryAssembly: using entry assembly {0}",
                    FormatOps.WrapOrNull(assembly)),
                    typeof(GlobalState).Name, TracePriority.StartupDebug);

                return assembly;
            }

            assembly = Assembly.GetExecutingAssembly();

            TraceOps.DebugTrace(String.Format(
                "FindEntryAssembly: using executing assembly {0}",
                FormatOps.WrapOrNull(assembly)),
                typeof(GlobalState).Name, TracePriority.StartupDebug);

            return assembly; /* NOT NULL */
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method determines whether the specified assembly is the cached
        /// entry assembly.
        /// </summary>
        /// <param name="assembly">
        /// The assembly to compare against the cached entry assembly.
        /// </param>
        /// <returns>
        /// True if the specified assembly is the entry assembly; otherwise,
        /// false.
        /// </returns>
        public static bool IsEntryAssembly(
            Assembly assembly
            ) /* THREAD-SAFE */
        {
            return Object.ReferenceEquals(assembly, entryAssembly);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method gets the cached entry assembly.
        /// </summary>
        /// <returns>
        /// The cached entry assembly.
        /// </returns>
        public static Assembly GetEntryAssembly() /* THREAD-SAFE */
        {
            return entryAssembly;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method gets the cached name of the entry assembly.
        /// </summary>
        /// <returns>
        /// The cached entry assembly name.
        /// </returns>
        public static AssemblyName GetEntryAssemblyName() /* THREAD-SAFE */
        {
            return entryAssemblyName;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method determines whether the specified assembly name matches
        /// the cached entry assembly name.
        /// </summary>
        /// <param name="assemblyName">
        /// The assembly name to compare against the cached entry assembly
        /// name.
        /// </param>
        /// <returns>
        /// True if the specified name matches the entry assembly name;
        /// otherwise, false.
        /// </returns>
        public static bool IsEntryAssemblyName( /* THREAD-SAFE */
            string assemblyName
            )
        {
            return SharedStringOps.Equals(
                assemblyName, (entryAssemblyName != null) ?
                entryAssemblyName.ToString() : null,
                assemblyNameComparisonType);
        }

        ///////////////////////////////////////////////////////////////////////

        #region Dead Code
#if DEAD_CODE
        /// <summary>
        /// This method gets the cached title of the entry assembly.
        /// </summary>
        /// <returns>
        /// The cached entry assembly title.
        /// </returns>
        private static string GetEntryAssemblyTitle() /* THREAD-SAFE */
        {
            return entryAssemblyTitle;
        }
#endif
        #endregion

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method gets the cached file system location of the entry
        /// assembly.
        /// </summary>
        /// <returns>
        /// The cached entry assembly location.
        /// </returns>
        public static string GetEntryAssemblyLocation() /* THREAD-SAFE */
        {
            return entryAssemblyLocation;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method gets the cached version of the entry assembly.
        /// </summary>
        /// <returns>
        /// The cached entry assembly version.
        /// </returns>
        public static Version GetEntryAssemblyVersion() /* THREAD-SAFE */
        {
            return entryAssemblyVersion;
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Executing Assembly Variable Access Methods
        /// <summary>
        /// This method determines whether the specified assembly is this
        /// (the Eagle core library) assembly.
        /// </summary>
        /// <param name="assembly">
        /// The assembly to compare against this assembly.
        /// </param>
        /// <returns>
        /// True if the specified assembly is this assembly; otherwise, false.
        /// </returns>
        public static bool IsAssembly(
            Assembly assembly
            ) /* THREAD-SAFE */
        {
            return Object.ReferenceEquals(assembly, thisAssembly);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method gets this (the Eagle core library) assembly.
        /// </summary>
        /// <returns>
        /// This assembly.
        /// </returns>
        public static Assembly GetAssembly() /* THREAD-SAFE */
        {
            return thisAssembly;
        }

        ///////////////////////////////////////////////////////////////////////

#if CAS_POLICY
        /// <summary>
        /// This method gets the cached security evidence for this assembly.
        /// </summary>
        /// <returns>
        /// The cached security evidence for this assembly.
        /// </returns>
        public static Evidence GetAssemblyEvidence() /* THREAD-SAFE */
        {
            return thisAssemblyEvidence;
        }
#endif

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method gets the cached name of this assembly.
        /// </summary>
        /// <returns>
        /// The cached name of this assembly.
        /// </returns>
        public static AssemblyName GetAssemblyName() /* THREAD-SAFE */
        {
            return thisAssemblyName;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method determines whether the specified assembly name matches
        /// the cached name of this assembly.
        /// </summary>
        /// <param name="assemblyName">
        /// The assembly name to compare against the cached name of this
        /// assembly.
        /// </param>
        /// <returns>
        /// True if the specified name matches the name of this assembly;
        /// otherwise, false.
        /// </returns>
        public static bool IsAssemblyName( /* THREAD-SAFE */
            string assemblyName
            )
        {
            return SharedStringOps.Equals(
                assemblyName, (thisAssemblyName != null) ?
                thisAssemblyName.ToString() : null,
                assemblyNameComparisonType);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method gets the cached title of this assembly.
        /// </summary>
        /// <returns>
        /// The cached title of this assembly.
        /// </returns>
        public static string GetAssemblyTitle() /* THREAD-SAFE */
        {
            return thisAssemblyTitle;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method gets the cached file system location of this assembly.
        /// </summary>
        /// <returns>
        /// The cached file system location of this assembly.
        /// </returns>
        public static string GetAssemblyLocation() /* THREAD-SAFE */
        {
            return thisAssemblyLocation;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method determines whether the specified location matches the
        /// cached file system location of this assembly.
        /// </summary>
        /// <param name="location">
        /// The location to compare against the cached location of this
        /// assembly.
        /// </param>
        /// <returns>
        /// True if the specified location matches the location of this
        /// assembly; otherwise, false.
        /// </returns>
        public static bool IsAssemblyLocation( /* THREAD-SAFE */
            string location
            )
        {
            return SharedStringOps.Equals(
                location, thisAssemblyLocation, PathOps.ComparisonType);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method gets the cached date and time associated with this
        /// assembly.
        /// </summary>
        /// <returns>
        /// The cached date and time associated with this assembly.
        /// </returns>
        public static DateTime GetAssemblyDateTime() /* THREAD-SAFE */
        {
            return thisAssemblyDateTime;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method gets the cached simple name of this assembly.
        /// </summary>
        /// <returns>
        /// The cached simple name of this assembly.
        /// </returns>
        public static string GetAssemblySimpleName() /* THREAD-SAFE */
        {
            return thisAssemblySimpleName;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method gets the cached full name of this assembly.
        /// </summary>
        /// <returns>
        /// The cached full name of this assembly.
        /// </returns>
        public static string GetAssemblyFullName() /* THREAD-SAFE */
        {
            return thisAssemblyFullName;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method gets the cached version of this assembly.
        /// </summary>
        /// <returns>
        /// The cached version of this assembly.
        /// </returns>
        public static Version GetAssemblyVersion() /* THREAD-SAFE */
        {
            return thisAssemblyVersion;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method gets the major and minor components of the cached
        /// version of this assembly.
        /// </summary>
        /// <returns>
        /// A version containing only the major and minor components of the
        /// cached version of this assembly.
        /// </returns>
        public static Version GetTwoPartAssemblyVersion() /* THREAD-SAFE */
        {
            return GetTwoPartVersion(thisAssemblyVersion);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method gets the cached version of this assembly, formatted as
        /// a string.
        /// </summary>
        /// <returns>
        /// The cached version of this assembly as a string, or null if no
        /// version is available.
        /// </returns>
        public static string GetAssemblyVersionString() /* THREAD-SAFE */
        {
            return (thisAssemblyVersion != null) ?
                thisAssemblyVersion.ToString() : null;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method gets the version string used for update checks. When
        /// this assembly has the default major and minor version, only the
        /// build and revision components are used; otherwise, the full version
        /// string is used.
        /// </summary>
        /// <returns>
        /// The update version string, or null if no version is available.
        /// </returns>
        public static string GetAssemblyUpdateVersion() /* THREAD-SAFE */
        {
            if (thisAssemblyVersion == null)
                return null;

            if ((thisAssemblyVersion.Major == DefaultMajorVersion) &&
                (thisAssemblyVersion.Minor == DefaultMinorVersion))
            {
                //
                // NOTE: This has a default major and minor version, use
                //       the build and revision only.
                //
                return String.Format(
                    UpdateVersionFormat, thisAssemblyVersion.Build,
                    thisAssemblyVersion.Revision);
            }
            else
            {
                //
                // NOTE: This has a non-default major or minor version,
                //       use the full version string.
                //
                return thisAssemblyVersion.ToString();
            }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method gets the cached culture information for this assembly.
        /// </summary>
        /// <returns>
        /// The cached culture information for this assembly.
        /// </returns>
        public static CultureInfo GetAssemblyCultureInfo() /* THREAD-SAFE */
        {
            return thisAssemblyCultureInfo;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method gets the cached public key token for this assembly.
        /// </summary>
        /// <returns>
        /// The cached public key token for this assembly.
        /// </returns>
        public static byte[] GetAssemblyPublicKeyToken() /* THREAD-SAFE */
        {
            return thisAssemblyPublicKeyToken;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method gets the cached primary URI associated with this
        /// assembly.
        /// </summary>
        /// <returns>
        /// The cached primary URI associated with this assembly.
        /// </returns>
        public static Uri GetAssemblyUri() /* THREAD-SAFE */
        {
            return thisAssemblyUri;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method gets the cached base URI used for update operations
        /// associated with this assembly.
        /// </summary>
        /// <returns>
        /// The cached update base URI associated with this assembly.
        /// </returns>
        public static Uri GetAssemblyUpdateBaseUri() /* THREAD-SAFE */
        {
            return thisAssemblyUpdateBaseUri;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method gets the cached base URI used for download operations
        /// associated with this assembly.
        /// </summary>
        /// <returns>
        /// The cached download base URI associated with this assembly.
        /// </returns>
        public static Uri GetAssemblyDownloadBaseUri() /* THREAD-SAFE */
        {
            return thisAssemblyDownloadBaseUri;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method gets the cached base URI used for script operations
        /// associated with this assembly.
        /// </summary>
        /// <returns>
        /// The cached script base URI associated with this assembly.
        /// </returns>
        public static Uri GetAssemblyScriptBaseUri() /* THREAD-SAFE */
        {
            return thisAssemblyScriptBaseUri;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method gets the cached base URI used for auxiliary operations
        /// associated with this assembly.
        /// </summary>
        /// <returns>
        /// The cached auxiliary base URI associated with this assembly.
        /// </returns>
        public static Uri GetAssemblyAuxiliaryBaseUri() /* THREAD-SAFE */
        {
            return thisAssemblyAuxiliaryBaseUri;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method gets the cached namespace URI associated with this
        /// assembly.
        /// </summary>
        /// <returns>
        /// The cached namespace URI associated with this assembly.
        /// </returns>
        public static Uri GetAssemblyNamespaceUri() /* THREAD-SAFE */
        {
            return thisAssemblyNamespaceUri;
        }

        ///////////////////////////////////////////////////////////////////////

        #region Dead Code
#if DEAD_CODE
        /// <summary>
        /// This method gets the cached plugin flags associated with this
        /// assembly.
        /// </summary>
        /// <returns>
        /// The cached plugin flags associated with this assembly, or null if
        /// the static lock could not be acquired.
        /// </returns>
        public static PluginFlags? GetAssemblyPluginFlags() /* THREAD-SAFE */
        {
            bool locked = false;

            try
            {
                HardTryLock(ref locked); /* TRANSACTIONAL */

                if (locked)
                {
                    return thisAssemblyPluginFlags;
                }
                else
                {
                    TraceOps.LockTrace(
                        "GetAssemblyPluginFlags",
                        typeof(GlobalState).Name, true,
                        TracePriority.LockError,
                        MaybeWhoHasLock());
                }
            }
            finally
            {
                ExitLock(ref locked); /* TRANSACTIONAL */
            }

            return null;
        }
#endif

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method sets the cached plugin flags associated with this
        /// assembly.
        /// </summary>
        /// <param name="pluginFlags">
        /// The plugin flags to associate with this assembly.
        /// </param>
        /// <returns>
        /// True if the plugin flags were set; otherwise, false.
        /// </returns>
        private static bool SetAssemblyPluginFlags(
            PluginFlags pluginFlags /* in */
            )
        {
            bool locked = false;

            try
            {
                HardTryLock(ref locked); /* TRANSACTIONAL */

                if (locked)
                {
                    thisAssemblyPluginFlags = pluginFlags;
                    return true;
                }
                else
                {
                    TraceOps.LockTrace(
                        "SetAssemblyPluginFlags",
                        typeof(GlobalState).Name, true,
                        TracePriority.LockError,
                        MaybeWhoHasLock());
                }
            }
            finally
            {
                ExitLock(ref locked); /* TRANSACTIONAL */
            }

            return false;
        }
        #endregion
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Assembly Path Variable Access Methods
        /// <summary>
        /// This method gets the fully qualified path to the directory containing
        /// this assembly.  This method is thread-safe.
        /// </summary>
        /// <returns>
        /// The assembly directory path, or null if it could not be obtained.
        /// </returns>
        public static string GetAssemblyPath() /* THREAD-SAFE */
        {
            bool locked = false;

            try
            {
                PathHardTryLock(ref locked); /* TRANSACTIONAL */

                if (locked)
                {
                    return thisAssemblyPath;
                }
                else
                {
                    TraceOps.LockTrace(
                        "GetAssemblyPath",
                        typeof(GlobalState).Name, true,
                        TracePriority.LockError,
                        MaybeWhoHasLock());
                }
            }
            finally
            {
                ExitLock(ref locked); /* TRANSACTIONAL */
            }

            return null;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method determines whether the path to the directory containing
        /// this assembly has been set.  This method is thread-safe.
        /// </summary>
        /// <returns>
        /// True if the assembly path has been set to a non-empty value;
        /// otherwise, false.
        /// </returns>
        private static bool HaveAssemblyPath() /* THREAD-SAFE */
        {
            bool locked = false;

            try
            {
                PathHardTryLock(ref locked); /* TRANSACTIONAL */

                if (locked)
                {
                    return !String.IsNullOrEmpty(thisAssemblyPath);
                }
                else
                {
                    TraceOps.LockTrace(
                        "HaveAssemblyPath",
                        typeof(GlobalState).Name, true,
                        TracePriority.LockError,
                        MaybeWhoHasLock());
                }
            }
            finally
            {
                ExitLock(ref locked); /* TRANSACTIONAL */
            }

            return false;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method optionally initializes and then returns the path to the
        /// directory containing this assembly.  This method is thread-safe.
        /// </summary>
        /// <param name="initialize">
        /// Non-zero to initialize the assembly path before returning it;
        /// otherwise, the current value is returned.
        /// </param>
        /// <returns>
        /// The assembly directory path, or null if it could not be obtained.
        /// </returns>
        public static string InitializeOrGetAssemblyPath(
            bool initialize
            ) /* THREAD-SAFE */
        {
            return InitializeOrGetAssemblyPath(initialize, false);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method optionally initializes and then returns the path to the
        /// directory containing this assembly.  This method is thread-safe.
        /// </summary>
        /// <param name="initialize">
        /// Non-zero to initialize the assembly path before returning it;
        /// otherwise, the current value is returned.
        /// </param>
        /// <param name="force">
        /// Non-zero to force re-initialization of the assembly path even if it
        /// has already been set.
        /// </param>
        /// <returns>
        /// The assembly directory path, or null if it could not be obtained.
        /// </returns>
        private static string InitializeOrGetAssemblyPath(
            bool initialize,
            bool force
            ) /* THREAD-SAFE */
        {
            return initialize ?
                InitializeAssemblyPath(force) : GetAssemblyPath();
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method initializes the path to the directory containing this
        /// assembly, without acquiring the associated lock.  The path lock must
        /// already be held by the caller.
        /// </summary>
        /// <param name="force">
        /// Non-zero to re-initialize the assembly path even if it has already
        /// been set.
        /// </param>
        /// <returns>
        /// The assembly directory path.
        /// </returns>
        private static string InitializeAssemblyPathNoLock(
            bool force
            ) /* THREAD-SAFE */
        {
            if (force || (thisAssemblyPath == null))
            {
                thisAssemblyPath = AssemblyOps.GetPath(
                    null, thisAssembly);
            }

            return thisAssemblyPath;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method initializes and returns the path to the directory
        /// containing this assembly.  This method is thread-safe.
        /// </summary>
        /// <param name="force">
        /// Non-zero to re-initialize the assembly path even if it has already
        /// been set.
        /// </param>
        /// <returns>
        /// The assembly directory path, or null if it could not be obtained.
        /// </returns>
        private static string InitializeAssemblyPath(
            bool force
            ) /* THREAD-SAFE */
        {
            bool locked = false;

            try
            {
                PathHardTryLock(ref locked); /* TRANSACTIONAL */

                if (locked)
                {
                    return InitializeAssemblyPathNoLock(force);
                }
                else
                {
                    TraceOps.LockTrace(
                        "InitializeAssemblyPath",
                        typeof(GlobalState).Name, true,
                        TracePriority.LockError,
                        MaybeWhoHasLock());
                }
            }
            finally
            {
                ExitLock(ref locked); /* TRANSACTIONAL */
            }

            return null;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method initializes (if necessary) and then returns the path to
        /// the directory containing this assembly.  This method is thread-safe.
        /// </summary>
        /// <returns>
        /// The assembly directory path, or null if it could not be obtained.
        /// </returns>
        private static string AlwaysGetAssemblyPath() /* THREAD-SAFE */
        {
            return InitializeOrGetAssemblyPath(true);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method gets the path to the directory containing the entry
        /// assembly for the current application.  This method is thread-safe.
        /// </summary>
        /// <returns>
        /// The entry assembly directory path, or null if it could not be
        /// obtained.
        /// </returns>
        private static string GetEntryAssemblyPath() /* THREAD-SAFE */
        {
            bool locked = false;

            try
            {
                PathHardTryLock(ref locked); /* TRANSACTIONAL */

                if (locked)
                {
                    return entryAssemblyPath;
                }
                else
                {
                    TraceOps.LockTrace(
                        "GetEntryAssemblyPath",
                        typeof(GlobalState).Name, true,
                        TracePriority.LockError,
                        MaybeWhoHasLock());
                }
            }
            finally
            {
                ExitLock(ref locked); /* TRANSACTIONAL */
            }

            return null;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method optionally initializes and then returns the path to the
        /// directory containing the entry assembly.  This method is thread-safe.
        /// </summary>
        /// <param name="initialize">
        /// Non-zero to initialize the entry assembly path before returning it;
        /// otherwise, the current value is returned.
        /// </param>
        /// <returns>
        /// The entry assembly directory path, or null if it could not be
        /// obtained.
        /// </returns>
        public static string InitializeOrGetEntryAssemblyPath(
            bool initialize
            ) /* THREAD-SAFE */
        {
            return InitializeOrGetEntryAssemblyPath(initialize, false);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method optionally initializes and then returns the path to the
        /// directory containing the entry assembly.  This method is thread-safe.
        /// </summary>
        /// <param name="initialize">
        /// Non-zero to initialize the entry assembly path before returning it;
        /// otherwise, the current value is returned.
        /// </param>
        /// <param name="force">
        /// Non-zero to force re-initialization of the entry assembly path even
        /// if it has already been set.
        /// </param>
        /// <returns>
        /// The entry assembly directory path, or null if it could not be
        /// obtained.
        /// </returns>
        private static string InitializeOrGetEntryAssemblyPath(
            bool initialize,
            bool force
            ) /* THREAD-SAFE */
        {
            return initialize ?
                InitializeEntryAssemblyPath(force) : GetEntryAssemblyPath();
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method initializes the path to the directory containing the
        /// entry assembly, without acquiring the associated lock.  The path lock
        /// must already be held by the caller.
        /// </summary>
        /// <param name="force">
        /// Non-zero to re-initialize the entry assembly path even if it has
        /// already been set.
        /// </param>
        /// <returns>
        /// The entry assembly directory path.
        /// </returns>
        private static string InitializeEntryAssemblyPathNoLock(
            bool force
            ) /* THREAD-SAFE */
        {
            if (force || (entryAssemblyPath == null))
            {
                entryAssemblyPath = AssemblyOps.GetPath(
                    null, entryAssembly);
            }

            return entryAssemblyPath;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method initializes and returns the path to the directory
        /// containing the entry assembly.  This method is thread-safe.
        /// </summary>
        /// <param name="force">
        /// Non-zero to re-initialize the entry assembly path even if it has
        /// already been set.
        /// </param>
        /// <returns>
        /// The entry assembly directory path, or null if it could not be
        /// obtained.
        /// </returns>
        private static string InitializeEntryAssemblyPath(
            bool force
            ) /* THREAD-SAFE */
        {
            bool locked = false;

            try
            {
                PathHardTryLock(ref locked); /* TRANSACTIONAL */

                if (locked)
                {
                    return InitializeEntryAssemblyPathNoLock(force);
                }
                else
                {
                    TraceOps.LockTrace(
                        "InitializeEntryAssemblyPath",
                        typeof(GlobalState).Name, true,
                        TracePriority.LockError,
                        MaybeWhoHasLock());
                }
            }
            finally
            {
                ExitLock(ref locked); /* TRANSACTIONAL */
            }

            return null;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method initializes (if necessary) and then returns the path to
        /// the directory containing the entry assembly.  This method is
        /// thread-safe.
        /// </summary>
        /// <returns>
        /// The entry assembly directory path, or null if it could not be
        /// obtained.
        /// </returns>
        private static string AlwaysGetEntryAssemblyPath() /* THREAD-SAFE */
        {
            return InitializeOrGetEntryAssemblyPath(true);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method gets the path to the directory containing the entry
        /// assembly, falling back to the path of this assembly when the entry
        /// assembly path is not available.  This method is thread-safe.
        /// </summary>
        /// <returns>
        /// An assembly directory path, or null if neither could be obtained.
        /// </returns>
        public static string GetAnyEntryAssemblyPath() /* THREAD-SAFE */
        {
            string path = AlwaysGetEntryAssemblyPath();

            if (path != null)
                return path;

            return AlwaysGetAssemblyPath();
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Binary Executable Variable Access Methods
        /// <summary>
        /// This method optionally initializes and then returns the binary base
        /// path used by the library.  This method is thread-safe.
        /// </summary>
        /// <param name="initialize">
        /// Non-zero to initialize the binary path before returning it;
        /// otherwise, the current value is returned.
        /// </param>
        /// <returns>
        /// The binary base path, or null if it could not be obtained.
        /// </returns>
        public static string InitializeOrGetBinaryPath(
            bool initialize
            ) /* THREAD-SAFE */
        {
            return InitializeOrGetBinaryPath(initialize, false);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method optionally initializes and then returns the binary base
        /// path used by the library.  This method is thread-safe.
        /// </summary>
        /// <param name="initialize">
        /// Non-zero to initialize the binary path before returning it;
        /// otherwise, the current value is returned.
        /// </param>
        /// <param name="force">
        /// Non-zero to force re-initialization of the binary path even if it has
        /// already been set.
        /// </param>
        /// <returns>
        /// The binary base path, or null if it could not be obtained.
        /// </returns>
        private static string InitializeOrGetBinaryPath(
            bool initialize,
            bool force
            ) /* THREAD-SAFE */
        {
            return initialize ?
                InitializeBinaryPath(force) : GetBinaryPath();
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method initializes the binary base path used by the library,
        /// without acquiring the associated lock.  The path lock must already be
        /// held by the caller.
        /// </summary>
        /// <param name="force">
        /// Non-zero to re-initialize the binary path even if it has already been
        /// set.
        /// </param>
        /// <returns>
        /// The binary base path.
        /// </returns>
        private static string InitializeBinaryPathNoLock(
            bool force
            ) /* THREAD-SAFE */
        {
            if (force || (sharedBinaryPath == null))
                sharedBinaryPath = PathOps.GetBinaryPath(true);

            return sharedBinaryPath;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method initializes and returns the binary base path used by the
        /// library.  This method is thread-safe.
        /// </summary>
        /// <param name="force">
        /// Non-zero to re-initialize the binary path even if it has already been
        /// set.
        /// </param>
        /// <returns>
        /// The binary base path, or null if it could not be obtained.
        /// </returns>
        private static string InitializeBinaryPath(
            bool force
            ) /* THREAD-SAFE */
        {
            bool locked = false;

            try
            {
                PathHardTryLock(ref locked); /* TRANSACTIONAL */

                if (locked)
                {
                    return InitializeBinaryPathNoLock(force);
                }
                else
                {
                    TraceOps.LockTrace(
                        "InitializeBinaryPath",
                        typeof(GlobalState).Name, true,
                        TracePriority.LockError,
                        MaybeWhoHasLock());
                }
            }
            finally
            {
                ExitLock(ref locked); /* TRANSACTIONAL */
            }

            return null;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method attempts to retrieve the binary base path used by the
        /// library.  This method is thread-safe.
        /// </summary>
        /// <param name="binaryPath">
        /// Upon success, this contains the binary base path.  Upon failure, this
        /// is unchanged.
        /// </param>
        /// <returns>
        /// True if the binary base path was successfully retrieved; otherwise,
        /// false.
        /// </returns>
        private static bool TryGetBinaryPath(
            ref string binaryPath
            ) /* THREAD-SAFE */
        {
            bool locked = false;

            try
            {
                PathHardTryLock(ref locked); /* TRANSACTIONAL */

                if (locked)
                {
                    if (String.IsNullOrEmpty(sharedBinaryPath))
                        return false;

                    binaryPath = sharedBinaryPath;
                    return true;
                }
                else
                {
                    TraceOps.LockTrace(
                        "TryGetBinaryPath",
                        typeof(GlobalState).Name, true,
                        TracePriority.LockError,
                        MaybeWhoHasLock());
                }
            }
            finally
            {
                ExitLock(ref locked); /* TRANSACTIONAL */
            }

            return false;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method gets the binary base path used by the library.  This
        /// method is thread-safe.
        /// </summary>
        /// <returns>
        /// The binary base path, or null if it has not been set.
        /// </returns>
        private static string GetBinaryPath() /* THREAD-SAFE */
        {
            bool locked = false;

            try
            {
                PathHardTryLock(ref locked); /* TRANSACTIONAL */

                if (locked)
                {
                    return sharedBinaryPath;
                }
                else
                {
                    TraceOps.LockTrace(
                        "GetBinaryPath",
                        typeof(GlobalState).Name, true,
                        TracePriority.LockError,
                        MaybeWhoHasLock());
                }
            }
            finally
            {
                ExitLock(ref locked); /* TRANSACTIONAL */
            }

            return null;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method conditionally sets the binary base path used by the
        /// library.  This method is thread-safe.
        /// </summary>
        /// <param name="binaryPath">
        /// The new binary base path.
        /// </param>
        /// <param name="force">
        /// Non-zero to overwrite an existing binary base path.
        /// </param>
        /// <returns>
        /// True if the binary base path was set; otherwise, false.
        /// </returns>
        public static bool MaybeSetBinaryPath(
            string binaryPath,
            bool force
            ) /* THREAD-SAFE */
        {
            bool locked = false;

            try
            {
                PathSoftTryLock(ref locked); /* TRANSACTIONAL */

                if (locked)
                {
                    if (!force && (sharedBinaryPath != null))
                        return false;

                    sharedBinaryPath = binaryPath;
                    return true;
                }
                else
                {
                    TraceOps.LockTrace(
                        "MaybeSetBinaryPath",
                        typeof(GlobalState).Name, true,
                        TracePriority.LockError,
                        MaybeWhoHasLock());
                }
            }
            finally
            {
                ExitLock(ref locked); /* TRANSACTIONAL */
            }

            return false;
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Resource Management Variable Access Methods
        /// <summary>
        /// This method gets the base name used when accessing managed resources.
        /// This method is thread-safe.
        /// </summary>
        /// <returns>
        /// The resource base name.
        /// </returns>
        public static string GetResourceBaseName() /* THREAD-SAFE */
        {
            return resourceBaseName;
        }
        #endregion
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Package Global Variable Access Methods
#if DEBUGGER
        /// <summary>
        /// This method gets the name used for the script debugger.  This method
        /// is thread-safe.
        /// </summary>
        /// <returns>
        /// The debugger name.
        /// </returns>
        public static string GetDebuggerName() /* THREAD-SAFE */
        {
            return debuggerName;
        }
#endif

        ///////////////////////////////////////////////////////////////////////

        /* THREAD-SAFE */
        /// <summary>
        /// This method gets the configured package name, returning the default
        /// package name when none has been set.  This method is thread-safe.
        /// </summary>
        /// <returns>
        /// The package name; this value is never null.
        /// </returns>
        public static string GetPackageName() /* CANNOT RETURN NULL */
        {
            return (packageName != null) ?
                packageName : DefaultPackageName;
        }

        ///////////////////////////////////////////////////////////////////////

        /* THREAD-SAFE */
        /// <summary>
        /// This method gets the configured case-insensitive package name,
        /// returning the default when none has been set.  This method is
        /// thread-safe.
        /// </summary>
        /// <returns>
        /// The case-insensitive package name; this value is never null.
        /// </returns>
        public static string GetPackageNameNoCase() /* CANNOT RETURN NULL */
        {
            return (packageNameNoCase != null) ?
                packageNameNoCase : DefaultPackageNameNoCase;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method gets the package name associated with the specified
        /// package type.  This method is thread-safe.
        /// </summary>
        /// <param name="packageType">
        /// The package type whose name is requested.
        /// </param>
        /// <param name="default">
        /// The package name to return when the specified package type is not
        /// recognized; this value may be null.
        /// </param>
        /// <returns>
        /// The package name for the specified package type, or the supplied
        /// default value; this value may be null.
        /// </returns>
        public static string GetPackageTypeName( /* MAY RETURN NULL */
            PackageType packageType, /* in */
            string @default          /* OPTIONAL: May be null. */
            ) /* THREAD-SAFE */
        {
            switch (packageType)
            {
                case PackageType.Loader:
                    return LoaderPackageName;
                case PackageType.Library:
                    return LibraryPackageName;
                case PackageType.Test:
                    return TestPackageName;
                case PackageType.Kit:
                    return KitPackageName;
                case PackageType.Default:
                    return DefaultPackageName;
                default:
                    return @default;
            }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method gets the package name for the specified package type.
        /// This method is thread-safe.
        /// </summary>
        /// <param name="packageType">
        /// The package type whose name is requested.
        /// </param>
        /// <param name="noCase">
        /// Non-zero to return the package name in lower case.
        /// </param>
        /// <returns>
        /// The package name; this value is never null.
        /// </returns>
        public static string GetPackageName( /* CANNOT RETURN NULL */
            PackageType packageType,
            bool noCase
            ) /* THREAD-SAFE */
        {
            return GetPackageName(packageType, null, null, noCase);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method gets the package name for the specified package type,
        /// optionally surrounding it with a prefix and suffix.  This method is
        /// thread-safe.
        /// </summary>
        /// <param name="packageType">
        /// The package type whose name is requested.
        /// </param>
        /// <param name="prefix">
        /// The optional prefix to prepend to the package name; this value may be
        /// null.
        /// </param>
        /// <param name="suffix">
        /// The optional suffix to append to the package name; this value may be
        /// null.
        /// </param>
        /// <param name="noCase">
        /// Non-zero to return the package name in lower case.
        /// </param>
        /// <returns>
        /// The package name; this value is never null.
        /// </returns>
        public static string GetPackageName( /* CANNOT RETURN NULL */
            PackageType packageType,
            string prefix,
            string suffix,
            bool noCase
            ) /* THREAD-SAFE */
        {
            //
            // NOTE: We do not want to return the assembly name here because
            //       that is not guaranteed to be the same as what we consider
            //       the "package name".
            //
            string result = GetAssemblyTitle();

            if (String.IsNullOrEmpty(result))
                result = GetPackageTypeName(packageType, DefaultPackageName);

            if (noCase && !String.IsNullOrEmpty(result))
                result = result.ToLowerInvariant();

            if (!String.IsNullOrEmpty(prefix) || !String.IsNullOrEmpty(suffix))
            {
                result = String.Format(
                    PackageNameFormat, prefix, result, suffix);
            }

            return result;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method gets the package version.  This method is thread-safe.
        /// </summary>
        /// <param name="shortOnly">
        /// Non-zero to return only the two-part (major and minor) version; zero
        /// to return the full version; null to use the configured default.  This
        /// value may be null.
        /// </param>
        /// <returns>
        /// The package version.
        /// </returns>
        public static Version GetPackageVersion(
            bool? shortOnly /* in: OPTIONAL, COMPAT: Eagle beta. */
            ) /* THREAD-SAFE */
        {
            if (shortOnly != null)
            {
                return (bool)shortOnly ?
                    GetShortPackageVersion() : GetLongPackageVersion();
            }

            return useLongPackageVersion ?
                GetLongPackageVersion() : GetShortPackageVersion();
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method gets the two-part (major and minor) package version.
        /// This method is thread-safe.
        /// </summary>
        /// <returns>
        /// The two-part package version.
        /// </returns>
        private static Version GetShortPackageVersion() /* THREAD-SAFE */
        {
            //
            // NOTE: Package versions do not typically include the build
            //       and revision numbers; therefore, be sure they are
            //       omitted in our return value.
            //
            return GetTwoPartVersion(GetLongPackageVersion());
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method gets the full package version, returning the default
        /// version when none has been set.  This method is thread-safe.
        /// </summary>
        /// <returns>
        /// The full package version.
        /// </returns>
        private static Version GetLongPackageVersion() /* THREAD-SAFE */
        {
            return (packageVersion != null) ?
                packageVersion : DefaultVersion;
        }

        ///////////////////////////////////////////////////////////////////////

#if NATIVE && TCL
        /// <summary>
        /// This method gets the path used as the Tcl package name.  This method
        /// is thread-safe.
        /// </summary>
        /// <returns>
        /// The Tcl package name path, or null if it has not been set.
        /// </returns>
        public static string GetTclPackageNamePath() /* THREAD-SAFE */
        {
            bool locked = false;

            try
            {
                PathSoftTryLock(ref locked); /* TRANSACTIONAL */

                if (locked)
                {
                    return tclPackageNamePath;
                }
                else
                {
                    TraceOps.LockTrace(
                        "GetTclPackageNamePath",
                        typeof(GlobalState).Name, true,
                        TracePriority.LockError,
                        MaybeWhoHasLock());
                }
            }
            finally
            {
                ExitLock(ref locked); /* TRANSACTIONAL */
            }

            return null;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method gets the root path used as the Tcl package name.  This
        /// method is thread-safe.
        /// </summary>
        /// <returns>
        /// The Tcl package name root path, or null if it has not been set.
        /// </returns>
        public static string GetTclPackageNameRootPath() /* THREAD-SAFE */
        {
            bool locked = false;

            try
            {
                PathSoftTryLock(ref locked); /* TRANSACTIONAL */

                if (locked)
                {
                    return tclPackageNameRootPath;
                }
                else
                {
                    TraceOps.LockTrace(
                        "GetTclPackageNameRootPath",
                        typeof(GlobalState).Name, true,
                        TracePriority.LockError,
                        MaybeWhoHasLock());
                }
            }
            finally
            {
                ExitLock(ref locked); /* TRANSACTIONAL */
            }

            return null;
        }
#endif
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Raw Binary Base Path Management Methods
        /// <summary>
        /// This method gets the raw binary base path for this assembly.  This
        /// method is thread-safe.
        /// </summary>
        /// <returns>
        /// The raw binary base path.
        /// </returns>
        public static string GetRawBinaryBasePath() /* THREAD-SAFE */
        {
            return GetRawBinaryBasePath(thisAssembly);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method gets the raw binary base path for this assembly using the
        /// specified binary path.  This method is thread-safe.
        /// </summary>
        /// <param name="binaryPath">
        /// The binary path to use when computing the base path.
        /// </param>
        /// <returns>
        /// The raw binary base path.
        /// </returns>
        private static string GetRawBinaryBasePath(
            string binaryPath
            ) /* THREAD-SAFE */
        {
            return GetRawBinaryBasePath(thisAssembly, binaryPath);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method gets the raw binary base path for the specified assembly.
        /// This method is thread-safe.
        /// </summary>
        /// <param name="assembly">
        /// The assembly used when computing the base path; this value may be
        /// null.
        /// </param>
        /// <returns>
        /// The raw binary base path.
        /// </returns>
        private static string GetRawBinaryBasePath(
            Assembly assembly /* OPTIONAL: May be null. */
            ) /* THREAD-SAFE */
        {
            return GetRawBasePath(assembly, InitializeOrGetBinaryPath(false));
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method gets the raw binary base path for the specified assembly
        /// using the specified binary path.  This method is thread-safe.
        /// </summary>
        /// <param name="assembly">
        /// The assembly used when computing the base path; this value may be
        /// null.
        /// </param>
        /// <param name="binaryPath">
        /// The binary path to use when computing the base path.
        /// </param>
        /// <returns>
        /// The raw binary base path.
        /// </returns>
        private static string GetRawBinaryBasePath(
            Assembly assembly, /* OPTIONAL: May be null. */
            string binaryPath
            ) /* THREAD-SAFE */
        {
            return GetRawBasePath(assembly, binaryPath);
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Raw Base Path Management Methods
        /// <summary>
        /// This method gets the raw base path for this assembly.  This method is
        /// thread-safe.
        /// </summary>
        /// <returns>
        /// The raw base path.
        /// </returns>
        public static string GetRawBasePath() /* THREAD-SAFE */
        {
            return GetRawBasePath(InitializeOrGetAssemblyPath(false));
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method gets the raw base path for this assembly using the
        /// specified path.  This method is thread-safe.
        /// </summary>
        /// <param name="path">
        /// The path to use when computing the base path.
        /// </param>
        /// <returns>
        /// The raw base path.
        /// </returns>
        private static string GetRawBasePath(
            string path
            ) /* THREAD-SAFE */
        {
            return GetRawBasePath(thisAssembly, path);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method gets the raw base path for the specified assembly using
        /// the specified path.  This method is thread-safe.
        /// </summary>
        /// <param name="assembly">
        /// The assembly used when computing the base path; this value may be
        /// null.
        /// </param>
        /// <param name="path">
        /// The path to use when computing the base path.
        /// </param>
        /// <returns>
        /// The raw base path.
        /// </returns>
        private static string GetRawBasePath(
            Assembly assembly, /* OPTIONAL: May be null. */
            string path
            ) /* THREAD-SAFE */
        {
            return PathOps.GetBasePath(assembly, path);
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Base Path Global Variable Management Methods
        /// <summary>
        /// This method gets the base path used by the library.  This method is
        /// thread-safe.
        /// </summary>
        /// <returns>
        /// The base path, or null if it could not be determined.
        /// </returns>
        public static string GetBasePath() /* THREAD-SAFE */
        {
            return GetBasePath(thisAssembly);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method gets the base path used by the library for the specified
        /// assembly.  This method is thread-safe.
        /// </summary>
        /// <param name="assembly">
        /// The assembly used when computing the base path; this value may be
        /// null.
        /// </param>
        /// <returns>
        /// The base path, or null if it could not be determined.
        /// </returns>
        private static string GetBasePath(
            Assembly assembly /* OPTIONAL: May be null. */
            ) /* THREAD-SAFE */
        {
            string result = null;
            bool locked = false;

            try
            {
                PathHardTryLock(ref locked); /* TRANSACTIONAL */

                if (locked)
                {
                    //
                    // NOTE: Allow manual override of the base path via the
                    //       SetBasePath method.
                    //
                    result = sharedBasePath;

                    //
                    // NOTE: Was the shared base path set to something that
                    //       looks valid?
                    //
                    if (String.IsNullOrEmpty(result))
                    {
                        //
                        // NOTE: Allow the "EAGLE_BASE" environment variable
                        //       to override the base path.
                        //
                        result = GetEnvironmentVariable(EnvVars.EagleBase);
                    }

                    //
                    // NOTE: Was the "EAGLE_BASE" environment variable set to
                    //       something that looks valid?
                    //
                    if (String.IsNullOrEmpty(result))
                    {
                        //
                        // NOTE: Check if the assembly specified by the caller,
                        //       if any, is present in the GAC.
                        //
                        string binaryPath; /* REUSED */

                        if ((assembly != null) && assembly.GlobalAssemblyCache)
                        {
#if !NET_STANDARD_20
                            //
                            // NOTE: The specified assembly has been GAC'd.  We
                            //       need to use the registry to find where we
                            //       were actually installed to.
                            //
                            result = SetupOps.GetPath(packageVersion);
#endif

                            //
                            // NOTE: If we failed to get the path from the setup
                            //       registry hive (perhaps setup was not run?)
                            //       then we resort to using the current assembly
                            //       probing path for the application domain, if
                            //       possible.
                            //
                            if (String.IsNullOrEmpty(result))
                            {
                                binaryPath = null;

                                if (TryGetBinaryPath(ref binaryPath))
                                {
                                    result = GetRawBinaryBasePath(
                                        assembly, binaryPath);
                                }
                            }
                        }
                        else
                        {
                            //
                            // NOTE: Return the base directory that this assembly
                            //       was loaded from (i.e. without the "bin"), if
                            //       possible.
                            //
                            binaryPath = null;

                            if (TryGetBinaryPath(ref binaryPath))
                            {
                                result = GetRawBinaryBasePath(
                                    assembly, binaryPath);
                            }
                        }
                    }
                }
                else
                {
                    TraceOps.LockTrace(
                        "GetBasePath",
                        typeof(GlobalState).Name, true,
                        TracePriority.LockError,
                        MaybeWhoHasLock());
                }
            }
            finally
            {
                ExitLock(ref locked); /* TRANSACTIONAL */
            }

            return result;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method sets the base path used by the library.  This method is
        /// intended for external use only and is thread-safe.
        /// </summary>
        /// <param name="basePath">
        /// The new base path.
        /// </param>
        /// <param name="refresh">
        /// Non-zero to refresh any paths derived from the base path.
        /// </param>
        public static void SetBasePath( /* EXTERNAL USE ONLY */
            string basePath,
            bool refresh
            ) /* THREAD-SAFE */
        {
            TraceOps.DebugTrace(String.Format(
                "SetBasePath: entered, basePath = {0}, refresh = {1}",
                FormatOps.WrapOrNull(basePath), refresh),
                typeof(GlobalState).Name, TracePriority.StartupDebug);

            bool locked = false;

            try
            {
                PathSoftTryLock(ref locked); /* TRANSACTIONAL */

                if (locked)
                {
                    sharedBasePath = basePath;

                    //
                    // BUGFIX: Be sure to propagate the changes down to
                    //         where they are actually useful.
                    //
                    if (refresh)
                        RefreshBasePath();
                }
                else
                {
                    TraceOps.LockTrace(
                        "SetBasePath",
                        typeof(GlobalState).Name, true,
                        TracePriority.LockError,
                        MaybeWhoHasLock());
                }
            }
            finally
            {
                ExitLock(ref locked); /* TRANSACTIONAL */
            }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method refreshes any paths that are derived from the base path.
        /// This method is thread-safe.
        /// </summary>
        private static void RefreshBasePath() /* THREAD-SAFE */
        {
            RefreshLibraryPath();

            ///////////////////////////////////////////////////////////////////

            TraceOps.DebugTrace("RefreshBasePath: complete",
                typeof(GlobalState).Name, TracePriority.StartupDebug);
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Externals Path Global Variable Management Methods
        /// <summary>
        /// This method gets the externals path used by the library.  This method
        /// is thread-safe.
        /// </summary>
        /// <returns>
        /// The externals path, or null if it could not be determined.
        /// </returns>
        public static string GetExternalsPath() /* THREAD-SAFE */
        {
            return GetExternalsPath(thisAssembly);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method gets the externals path used by the library for the
        /// specified assembly.  This method is thread-safe.
        /// </summary>
        /// <param name="assembly">
        /// The assembly used when computing the externals path; this value may
        /// be null.
        /// </param>
        /// <returns>
        /// The externals path, or null if it could not be determined.
        /// </returns>
        private static string GetExternalsPath(
            Assembly assembly /* OPTIONAL: May be null. */
            ) /* THREAD-SAFE */
        {
            string result = null;
            bool locked = false;

            try
            {
                PathSoftTryLock(ref locked); /* TRANSACTIONAL */

                if (locked)
                {
                    //
                    // NOTE: Allow manual override of the base path via the
                    //       SetExternalsPath method.
                    //
                    result = sharedExternalsPath;

                    //
                    // NOTE: Was the shared base path set to something that
                    //       looks valid?
                    //
                    if (String.IsNullOrEmpty(result))
                    {
                        //
                        // NOTE: Allow the "EAGLE_EXTERNALS" environment
                        //       variable to override the externals path.
                        //
                        result = GetEnvironmentVariable(
                            EnvVars.EagleExternals);
                    }

                    //
                    // NOTE: Was the "EAGLE_EXTERNALS" environment variable
                    //       set to something that looks valid?
                    //
                    if (String.IsNullOrEmpty(result))
                    {
                        string basePath = GetBasePath(assembly);

                        if (!String.IsNullOrEmpty(basePath))
                        {
                            result = PathOps.CombinePath(
                                null, basePath, _Path.Externals);
                        }
                    }
                }
                else
                {
                    TraceOps.LockTrace(
                        "GetExternalsPath",
                        typeof(GlobalState).Name, true,
                        TracePriority.LockError,
                        MaybeWhoHasLock());
                }
            }
            finally
            {
                ExitLock(ref locked); /* TRANSACTIONAL */
            }

            return result;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method sets the externals path used by the library.  This method
        /// is intended for external use only and is thread-safe.
        /// </summary>
        /// <param name="externalsPath">
        /// The new externals path.
        /// </param>
        /// <param name="refresh">
        /// Non-zero to refresh any paths derived from the externals path.
        /// </param>
        public static void SetExternalsPath( /* EXTERNAL USE ONLY */
            string externalsPath,
            bool refresh
            ) /* THREAD-SAFE */
        {
            TraceOps.DebugTrace(String.Format(
                "SetExternalsPath: entered, externalsPath = {0}, refresh = {1}",
                FormatOps.WrapOrNull(externalsPath), refresh),
                typeof(GlobalState).Name, TracePriority.StartupDebug);

            bool locked = false;

            try
            {
                PathSoftTryLock(ref locked); /* TRANSACTIONAL */

                if (locked)
                {
                    sharedExternalsPath = externalsPath;

                    //
                    // NOTE: Be sure to propagate the changes down to where
                    //       they are actually useful.
                    //
                    if (refresh)
                        RefreshExternalsPath();
                }
                else
                {
                    TraceOps.LockTrace(
                        "SetExternalsPath",
                        typeof(GlobalState).Name, true,
                        TracePriority.LockError,
                        MaybeWhoHasLock());
                }
            }
            finally
            {
                ExitLock(ref locked); /* TRANSACTIONAL */
            }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method refreshes any paths that are derived from the externals
        /// path.  This method is thread-safe.
        /// </summary>
        private static void RefreshExternalsPath() /* THREAD-SAFE */
        {
            //
            // TODO: Currently, this method does nothing.  Eventually, we may
            //       need to notify internal or external components of this
            //       path change.
            //
            TraceOps.DebugTrace("RefreshExternalsPath: complete",
                typeof(GlobalState).Name, TracePriority.StartupDebug);
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Interpreter Library Path / Auto-Path Support Methods
        /// <summary>
        /// This method fetches the library path and auto-path list associated
        /// with the specified interpreter.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter whose paths should be fetched.  This value may be
        /// null.
        /// </param>
        /// <param name="libraryPath">
        /// Upon success, receives the library path of the specified
        /// interpreter.
        /// </param>
        /// <param name="autoPathList">
        /// Upon success, receives the auto-path list of the specified
        /// interpreter.
        /// </param>
        /// <returns>
        /// True if the paths were fetched successfully; otherwise, false.
        /// </returns>
        private static bool FetchInterpreterPaths(
            Interpreter interpreter,
            ref string libraryPath,
            ref StringList autoPathList
            )
        {
            InitializeFlags initializeFlags = InitializeFlags.None;

            return FetchInterpreterPathsAndFlags(
                interpreter, ref libraryPath, ref autoPathList,
                ref initializeFlags);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method fetches the library path, auto-path list, and
        /// initialization flags associated with the specified interpreter,
        /// using the interpreter lock to ensure thread-safe access.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter whose paths and flags should be fetched.  This
        /// value may be null.
        /// </param>
        /// <param name="libraryPath">
        /// Upon success, receives the library path of the specified
        /// interpreter.
        /// </param>
        /// <param name="autoPathList">
        /// Upon success, receives the auto-path list of the specified
        /// interpreter.
        /// </param>
        /// <param name="initializeFlags">
        /// Upon success, receives the initialization flags of the specified
        /// interpreter.
        /// </param>
        /// <returns>
        /// True if the paths and flags were fetched successfully; otherwise,
        /// false.
        /// </returns>
        private static bool FetchInterpreterPathsAndFlags(
            Interpreter interpreter,
            ref string libraryPath,
            ref StringList autoPathList,
            ref InitializeFlags initializeFlags
            )
        {
            bool result = false;

            if (interpreter != null)
            {
                bool locked = false;

                try
                {
                    interpreter.InternalHardTryLock(
                        ref locked); /* TRANSACTIONAL */

                    if (locked)
                    {
                        libraryPath = interpreter.LibraryPath; /* throw */
                        autoPathList = interpreter.AutoPathList; /* throw */
                        initializeFlags = interpreter.InitializeFlags; /* throw */

                        result = true;
                    }
                    else
                    {
                        TraceOps.LockTrace(
                            "FetchInterpreterPathsAndFlags",
                            typeof(GlobalState).Name, false,
                            TracePriority.LockError,
                            interpreter.MaybeWhoHasLock());
                    }
                }
                catch (Exception e)
                {
                    TraceOps.DebugTrace(
                        e, typeof(GlobalState).Name,
                        TracePriority.StartupError);
                }
                finally
                {
                    interpreter.InternalExitLock(
                        ref locked); /* TRANSACTIONAL */
                }
            }

            return result;
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Interpreter Disposal Support Methods
        /// <summary>
        /// This method determines whether the specified interpreter is in a
        /// state suitable for being disposed, optionally canceling any pending
        /// evaluations and checking for global busy status.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter to be examined.  This value may be null.
        /// </param>
        /// <param name="noCancel">
        /// Non-zero to skip canceling any pending evaluations in the
        /// interpreter.
        /// </param>
        /// <param name="noBusy">
        /// Non-zero to skip checking whether the interpreter is globally busy.
        /// </param>
        /// <returns>
        /// True if the interpreter should be disposed; otherwise, false.
        /// </returns>
        private static bool ShouldDisposeInterpreter(
            Interpreter interpreter, /* in */
            bool noCancel,           /* in */
            bool noBusy              /* in */
            )
        {
            bool result = false;
            Result error = null;

            try
            {
                if (interpreter == null)
                {
                    error = "invalid interpreter";
                    goto done;
                }

                if (interpreter.Disposed)
                {
                    error = "interpreter is already disposed";
                    goto done;
                }

                if (!noCancel)
                {
                    CancelFlags cancelFlags =
                        RuntimeOps.GetCancelEvaluateFlags(
                            true, false, true, false, true, true);

                    if (interpreter.InternalCancelAnyEvaluateNoContext(
                            "interpreter is about to be disposed",
                            cancelFlags, ref error) != ReturnCode.Ok)
                    {
                        goto done;
                    }
                }

                if (!noBusy && interpreter.InternalIsGlobalBusy)
                {
                    error = "interpreter is globally busy";
                    goto done;
                }

                result = true;

            done:

                return result;
            }
            finally
            {
                if (!result || (error != null))
                {
                    TraceOps.DebugTrace(String.Format(
                        "ShouldDisposeInterpreter: interpreter = {0}, " +
                        "result = {1}, error = {2}",
                        FormatOps.InterpreterNoThrow(interpreter),
                        result, FormatOps.WrapOrNull(error)),
                        typeof(GlobalState).Name, result ?
                            TracePriority.CleanupDebug3 :
                            TracePriority.CleanupError);
                }
            }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method disposes of all tracked interpreters whose string
        /// representation matches the specified pattern.
        /// </summary>
        /// <param name="mode">
        /// The matching mode used to compare each interpreter against the
        /// specified pattern.
        /// </param>
        /// <param name="pattern">
        /// The pattern used to select which interpreters should be disposed.
        /// This value may be null to select all interpreters.
        /// </param>
        /// <param name="cancelFlags">
        /// The flags used to control whether pending evaluations are canceled
        /// and whether global busy status is honored.
        /// </param>
        /// <returns>
        /// The number of interpreters that were disposed.
        /// </returns>
        public static int DisposeInterpreters(
            MatchMode mode,         /* in */
            string pattern,         /* in */
            CancelFlags cancelFlags /* in */
            ) /* THREAD-SAFE */
        {
            int count = 0;
            IEnumerable<Interpreter> interpreters = GetInterpreters();

            if (interpreters == null)
                return count;

            bool noCancel = FlagOps.HasFlags(
                cancelFlags, CancelFlags.NoCancel, true);

            bool noBusy = FlagOps.HasFlags(
                cancelFlags, CancelFlags.NoBusy, true);

            foreach (Interpreter interpreter in interpreters)
            {
                if ((interpreter == null) || interpreter.Disposed)
                    continue;

                string text = interpreter.InternalToString();

                if ((pattern != null) && !StringOps.Match(
                        interpreter, mode, text, pattern, false))
                {
                    continue;
                }

                bool locked = false;

                try
                {
                    interpreter.InternalHardTryLock(
                        ref locked); /* TRANSACTIONAL */

                    if (locked)
                    {
                        if (!ShouldDisposeInterpreter(
                                interpreter, noCancel, noBusy))
                        {
                            continue;
                        }

                        Interpreter localInterpreter = interpreter;

                        ObjectOps.DisposeOrTrace<Interpreter>(
                            interpreter, ref localInterpreter);

                        localInterpreter = null;

                        if (interpreter.Disposed) /* REDUNDANT? */
                            count++;
                    }
                }
                finally
                {
                    interpreter.InternalExitLock(
                        ref locked); /* TRANSACTIONAL */
                }
            }

            return count;
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Library Path Global Variable Management Methods
        //
        // WARNING: *DEADLOCK* This requires the interpreter lock.
        //
        /// <summary>
        /// This method gets the library path for the specified interpreter,
        /// optionally refreshing the auto-path list beforehand.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter whose library path is being queried.  This value
        /// may be null.
        /// </param>
        /// <param name="refresh">
        /// Non-zero to refresh the auto-path list prior to computing the
        /// library path.
        /// </param>
        /// <param name="resetShared">
        /// Non-zero to reset the shared auto-path list when refreshing.
        /// </param>
        /// <returns>
        /// The library path, or the base path if no suitable library path is
        /// found.
        /// </returns>
        public static string GetLibraryPath(
            Interpreter interpreter, /* OPTIONAL: May be null. */
            bool refresh,
            bool resetShared
            ) /* THREAD-SAFE */
        {
            string libraryPath = null;
            StringList autoPathList = null;
            InitializeFlags initializeFlags = InitializeFlags.None;

            /* IGNORED */
            FetchInterpreterPathsAndFlags(
                interpreter, ref libraryPath, ref autoPathList,
                ref initializeFlags);

            return GetLibraryPath(
                interpreter, libraryPath, autoPathList, initializeFlags,
                refresh, resetShared);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method gets the library path for the specified interpreter
        /// using the supplied library path, auto-path list, and initialization
        /// flags, optionally refreshing the auto-path list beforehand.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter whose library path is being queried.  This value
        /// may be null.
        /// </param>
        /// <param name="libraryPath">
        /// The library path associated with the interpreter.
        /// </param>
        /// <param name="autoPathList">
        /// The auto-path list associated with the interpreter.
        /// </param>
        /// <param name="initializeFlags">
        /// The initialization flags used to control auto-path display and
        /// strictness.
        /// </param>
        /// <param name="refresh">
        /// Non-zero to refresh the auto-path list prior to computing the
        /// library path.
        /// </param>
        /// <param name="resetShared">
        /// Non-zero to reset the shared auto-path list when refreshing.
        /// </param>
        /// <returns>
        /// The library path, or the base path if no suitable library path is
        /// found.
        /// </returns>
        private static string GetLibraryPath(
            Interpreter interpreter, /* OPTIONAL: May be null. */
            string libraryPath,
            StringList autoPathList,
            InitializeFlags initializeFlags,
            bool refresh,
            bool resetShared
            ) /* THREAD-SAFE */
        {
            bool showAutoPath = FlagOps.HasFlags(
                initializeFlags, InitializeFlags.ShowAutoPath, true);

            bool strictAutoPath = FlagOps.HasFlags(
                initializeFlags, InitializeFlags.StrictAutoPath, true);

            if (refresh)
                RefreshAutoPathList(resetShared, showAutoPath);

            AutoPathDictionary autoPaths = null;

            GetInterpreterAutoPathList(
                interpreter, libraryPath, autoPathList, true,
                showAutoPath, strictAutoPath, ref autoPaths);

            GetSharedAutoPathList(
                interpreter, true, showAutoPath, strictAutoPath,
                ref autoPaths);

            if ((autoPaths != null) && (autoPaths.Count > 0))
            {
                string path = autoPaths.GetNthKeyOrNull(0, false);

                if (!String.IsNullOrEmpty(path))
                    return path;
            }

            return GetBasePath();
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method gets the library path relative to the specified
        /// assembly, honoring the supplied path flags to control how the path
        /// is resolved.
        /// </summary>
        /// <param name="assembly">
        /// The assembly used as the basis for computing the library path.
        /// This value may be null.
        /// </param>
        /// <param name="pathFlags">
        /// The flags used to control how the library path is computed,
        /// including whether to use the shared, root, or binary-relative path.
        /// </param>
        /// <returns>
        /// The library path, or null if no suitable library path is found.
        /// </returns>
        private static string GetLibraryPath(
            Assembly assembly, /* OPTIONAL: May be null. */
            PathFlags pathFlags
            ) /* THREAD-SAFE */
        {
            bool locked = false;

            try
            {
                PathHardTryLock(ref locked); /* TRANSACTIONAL */

                if (locked)
                {
                    bool noShared = FlagOps.HasFlags(
                        pathFlags, PathFlags.NoShared, true);

                    bool root = FlagOps.HasFlags(
                        pathFlags, PathFlags.Root, true);

                    bool noBinary = FlagOps.HasFlags(
                        pathFlags, PathFlags.NoBinary, true);

                    bool verbatim = FlagOps.HasFlags(
                        pathFlags, PathFlags.Verbatim, true);

                    bool noFullPath = FlagOps.HasFlags(
                        pathFlags, PathFlags.NoFullPath, true);

                    bool libExists = FlagOps.HasFlags(
                        pathFlags, PathFlags.LibExists, true);

                    bool noInitialize = FlagOps.HasFlags(
                        pathFlags, PathFlags.NoInitialize, true);

                    bool forceInitialize = FlagOps.HasFlags(
                        pathFlags, PathFlags.ForceInitialize, true);

                    if (!noShared && !String.IsNullOrEmpty(sharedLibraryPath))
                    {
                        //
                        // NOTE: Allow manual override of the library path.
                        //
                        if (!libExists || Directory.Exists(sharedLibraryPath))
                            return sharedLibraryPath;
                    }
                    else if (root)
                    {
                        //
                        // NOTE: We want the root library directory.  This
                        //       path allows us to run from the build
                        //       directory (e.g. "bin\Debug\bin") and still
                        //       refer to directories that are not in the
                        //       build directory (i.e. they are a peer of
                        //       the outer "bin" directory).
                        //
                        string path = PathOps.CombinePath(null,
                            GetBasePath(assembly), TclVars.Path.Lib);

                        if (!noFullPath)
                            path = Path.GetFullPath(path);

                        if (!libExists || Directory.Exists(path))
                            return path;
                    }
                    else if (noBinary)
                    {
                        string assemblyPath = AlwaysGetAssemblyPath();

                        //
                        // HACK: When running on .NET Core, package index
                        //       files may end up in "net*" sub-directories
                        //       (e.g. "netstandard2.0") below their actual
                        //       package output path.  In order to fix this
                        //       discrepancy, remove that final portion of
                        //       the path.
                        //
                        if (!verbatim)
                        {
                            /* IGNORED */
                            PathOps.MaybePreMutatePath(ref assemblyPath);
                        }

                        //
                        // NOTE: We want the non-root (or peer) assembly
                        //       library directory.  When running in a
                        //       non-build environment, this will typically
                        //       be the same as the root library directory.
                        //       This assumes that the parent directory of
                        //       the Eagle assembly contains a directory
                        //       named "lib".
                        //
                        string path = PathOps.CombinePath(null,
                            Path.GetDirectoryName(assemblyPath),
                            TclVars.Path.Lib);

                        if (!noFullPath)
                            path = Path.GetFullPath(path);

                        if (!libExists || Directory.Exists(path))
                            return path;
                    }
                    else
                    {
                        //
                        // HACK: We (generally) know that the binary path
                        //       must be initialized at this point because
                        //       this method is called during interpreter
                        //       creation.
                        //
                        string binaryPath = InitializeOrGetBinaryPath(
                            !noInitialize, forceInitialize);

                        //
                        // HACK: When running on .NET Core, package index
                        //       files may end up in "net*" sub-directories
                        //       (e.g. "netstandard2.0") below their actual
                        //       package output path.  In order to fix this
                        //       discrepancy, remove that final portion of
                        //       the path.
                        //
                        if (!verbatim)
                        {
                            /* IGNORED */
                            PathOps.MaybePreMutatePath(ref binaryPath);
                        }

                        //
                        // NOTE: We want the non-root (or peer) binary
                        //       library directory.  When running in a
                        //       non-build environment, this will
                        //       typically be the same as the root
                        //       library directory.
                        //
                        // BUGBUG: This basically assumes that the
                        //         directory containing the application
                        //         binary is parallel to the Eagle library
                        //         directory.  This will not work if Eagle
                        //         is running from "/usr/local/bin/Eagle"
                        //         and the library directory is
                        //         "/usr/local/lib/Eagle/" (OpenBSD).  In
                        //         order for that layout to work, we would
                        //         have to go up one more level.
                        //
                        string path = PathOps.CombinePath(null,
                            Path.GetDirectoryName(binaryPath),
                            TclVars.Path.Lib);

                        if (!noFullPath)
                            path = Path.GetFullPath(path);

                        if (!libExists || Directory.Exists(path))
                            return path;
                    }
                }
                else
                {
                    TraceOps.LockTrace(
                        "GetLibraryPath",
                        typeof(GlobalState).Name, true,
                        TracePriority.LockError,
                        MaybeWhoHasLock());
                }
            }
            finally
            {
                ExitLock(ref locked); /* TRANSACTIONAL */
            }

            return null;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method sets the shared library path, optionally refreshing the
        /// dependent package paths so that the change takes effect.
        /// </summary>
        /// <param name="libraryPath">
        /// The new shared library path.  This value may be null.
        /// </param>
        /// <param name="refresh">
        /// Non-zero to refresh the library path so that the change is
        /// propagated to the dependent package paths.
        /// </param>
        public static void SetLibraryPath( /* EXTERNAL USE ONLY */
            string libraryPath,
            bool refresh
            ) /* THREAD-SAFE */
        {
            TraceOps.DebugTrace(String.Format(
                "SetLibraryPath: entered, libraryPath = {0}, refresh = {1}",
                FormatOps.WrapOrNull(libraryPath), refresh),
                typeof(GlobalState).Name, TracePriority.StartupDebug);

            bool locked = false;

            try
            {
                PathSoftTryLock(ref locked); /* TRANSACTIONAL */

                if (locked)
                {
                    sharedLibraryPath = libraryPath;

                    //
                    // BUGFIX: Be sure to propagate the changes down to
                    //         where they are actually useful.
                    //
                    if (refresh)
                        RefreshLibraryPath();
                }
                else
                {
                    TraceOps.LockTrace(
                        "SetLibraryPath",
                        typeof(GlobalState).Name, true,
                        TracePriority.LockError,
                        MaybeWhoHasLock());
                }
            }
            finally
            {
                ExitLock(ref locked); /* TRANSACTIONAL */
            }
        }

        ///////////////////////////////////////////////////////////////////////

#if UNIX
        /// <summary>
        /// This method gets the Unix-specific library path, honoring the
        /// supplied path flags to control whether the shared, local, or
        /// system library directory is returned.
        /// </summary>
        /// <param name="pathFlags">
        /// The flags used to control how the Unix library path is computed.
        /// </param>
        /// <returns>
        /// The Unix library path, or null if no suitable library path is
        /// found.
        /// </returns>
        private static string GetUnixLibraryPath(
            PathFlags pathFlags
            ) /* THREAD-SAFE */
        {
            bool locked = false;

            try
            {
                PathHardTryLock(ref locked); /* TRANSACTIONAL */

                if (locked)
                {
                    bool noShared = FlagOps.HasFlags(
                        pathFlags, PathFlags.NoShared, true);

                    bool local = FlagOps.HasFlags(
                        pathFlags, PathFlags.Local, true);

                    bool libExists = FlagOps.HasFlags(
                        pathFlags, PathFlags.LibExists, true);

                    if (!noShared && !String.IsNullOrEmpty(sharedLibraryPath))
                    {
                        //
                        // NOTE: Allow manual override of the library path.
                        //
                        if (!libExists || Directory.Exists(sharedLibraryPath))
                            return sharedLibraryPath;
                    }
                    else if (local)
                    {
                        //
                        // NOTE: We want the directory where local libraries
                        //       are installed.
                        //
                        string path = TclVars.Path.UserLocalLib;

                        if (!libExists || Directory.Exists(path))
                            return path;
                    }
                    else
                    {
                        //
                        // NOTE: We want the directory where libraries are
                        //       installed.
                        //
                        string path = TclVars.Path.UserLib;

                        if (!libExists || Directory.Exists(path))
                            return path;
                    }
                }
                else
                {
                    TraceOps.LockTrace(
                        "GetUnixLibraryPath",
                        typeof(GlobalState).Name, true,
                        TracePriority.LockError,
                        MaybeWhoHasLock());
                }
            }
            finally
            {
                ExitLock(ref locked); /* TRANSACTIONAL */
            }

            return null;
        }
#endif

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method refreshes the package paths and resets the shared
        /// auto-path list so that they are recomputed using the current
        /// library path.
        /// </summary>
        private static void RefreshLibraryPath() /* THREAD-SAFE */
        {
            bool locked = false;

            try
            {
                PathHardTryLock(ref locked); /* TRANSACTIONAL */

                if (locked)
                {
                    RefreshPackagePathsNoLock(true);

                    ///////////////////////////////////////////////////////////

                    //
                    // BUGFIX: Reset the shared auto-path so that it will be
                    //         initialized again [using our new paths] on the
                    //         next call to the GetAutoPathList method.
                    //
                    ResetSharedAutoPathList();

                    ///////////////////////////////////////////////////////////

                    TraceOps.DebugTrace("RefreshLibraryPath: complete",
                        typeof(GlobalState).Name, TracePriority.StartupDebug);
                }
                else
                {
                    TraceOps.LockTrace(
                        "RefreshLibraryPath",
                        typeof(GlobalState).Name, true,
                        TracePriority.LockError,
                        MaybeWhoHasLock());
                }
            }
            finally
            {
                ExitLock(ref locked); /* TRANSACTIONAL */
            }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Package Path Data Support Methods
        /// <summary>
        /// This method refreshes the cached assembly package paths without
        /// acquiring the path lock.
        /// </summary>
        /// <param name="force">
        /// Non-zero to recompute the cached paths even when they have already
        /// been computed.
        /// </param>
        private static void RefreshAssemblyPackagePathsNoLock(
            bool force
            )
        {
            if (force || (assemblyPackageNamePath == null))
            {
                assemblyPackageNamePath = GetAssemblyPackagePath(
                    packageName, packageVersion);
            }

            if (force || (assemblyPackageRootPath == null))
            {
                assemblyPackageRootPath = GetAssemblyPackagePath(
                    null, null);
            }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method refreshes the cached raw binary base and raw base
        /// package paths without acquiring the path lock.
        /// </summary>
        /// <param name="force">
        /// Non-zero to recompute the cached paths even when they have already
        /// been computed.
        /// </param>
        private static void RefreshRawPackagePathsNoLock(
            bool force
            )
        {
            if (force || (rawBinaryBasePackageNamePath == null))
            {
                rawBinaryBasePackageNamePath = GetRawBinaryBasePackagePath(
                    packageName, packageVersion);
            }

            if (force || (rawBinaryBasePackageRootPath == null))
            {
                rawBinaryBasePackageRootPath = GetRawBinaryBasePackagePath(
                    null, null);
            }

            ///////////////////////////////////////////////////////////////////

            if (force || (rawBasePackageNamePath == null))
            {
                rawBasePackageNamePath = GetRawBasePackagePath(
                    packageName, packageVersion);
            }

            if (force || (rawBasePackageRootPath == null))
            {
                rawBasePackageRootPath = GetRawBasePackagePath(
                    null, null);
            }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method refreshes the cached package paths, including the peer
        /// binary, peer assembly, root, name-based, Unix, and Tcl package
        /// paths, without acquiring the path lock.
        /// </summary>
        /// <param name="force">
        /// Non-zero to recompute the cached paths even when they have already
        /// been computed.
        /// </param>
        private static void RefreshPackagePathsNoLock(
            bool force
            )
        {
            if (force || (packagePeerBinaryPath == null))
            {
                packagePeerBinaryPath = GetPackagePath(
                    thisAssembly, null, null, PathFlags.None);
            }

            if (force || (packagePeerAssemblyPath == null))
            {
                packagePeerAssemblyPath = GetPackagePath(
                    thisAssembly, null, null, PathFlags.NoBinary);
            }

            if (force || (packageRootPath == null))
            {
                packageRootPath = GetPackagePath(
                    thisAssembly, null, null, PathFlags.Root);
            }

            ///////////////////////////////////////////////////////////////////

            if (force || (packageNameBinaryPath == null))
            {
                packageNameBinaryPath = GetPackagePath(
                    thisAssembly, packageName, packageVersion,
                    PathFlags.None);
            }

            if (force || (packageNameAssemblyPath == null))
            {
                packageNameAssemblyPath = GetPackagePath(
                    thisAssembly, packageName, packageVersion,
                    PathFlags.NoBinary);
            }

            if (force || (packageNameRootPath == null))
            {
                packageNameRootPath = GetPackagePath(
                    thisAssembly, packageName, packageVersion,
                    PathFlags.Root);
            }

            ///////////////////////////////////////////////////////////////////

#if UNIX
            if (force || (unixPackageNameLocalPath == null))
            {
                unixPackageNameLocalPath = GetUnixPackagePath(
                    unixPackageName, unixPackageVersion,
                    PathFlags.Local);
            }

            if (force || (unixPackageNamePath == null))
            {
                unixPackageNamePath = GetUnixPackagePath(
                    unixPackageName, unixPackageVersion,
                    PathFlags.None);
            }
#endif

            ///////////////////////////////////////////////////////////////////

#if NATIVE && TCL
            if (force || (tclPackageNamePath == null))
            {
                tclPackageNamePath = GetPackagePath(
                    thisAssembly, TclVars.Package.Name, null,
                    PathFlags.None);
            }

            if (force || (tclPackageNameRootPath == null))
            {
                tclPackageNameRootPath = GetPackagePath(
                    thisAssembly, TclVars.Package.Name, null,
                    PathFlags.Root);
            }
#endif
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method initializes and/or refreshes the binary, assembly, and
        /// package paths used by the global state, under the protection of the
        /// path lock.
        /// </summary>
        /// <param name="initialize">
        /// Non-zero to initialize the binary, assembly, and entry assembly
        /// paths.
        /// </param>
        /// <param name="refresh">
        /// Non-zero to refresh the cached assembly, raw, and package paths.
        /// </param>
        /// <param name="force">
        /// Non-zero to force the paths to be recomputed even when they have
        /// already been computed.
        /// </param>
        /// <returns>
        /// True if the paths were set up successfully; otherwise, false.
        /// </returns>
        public static bool SetupPaths(
            bool initialize,
            bool refresh,
            bool force
            )
        {
            bool locked = false;

            try
            {
                PathHardTryLock(ref locked); /* TRANSACTIONAL */

                if (locked)
                {
                    if (initialize)
                    {
                        /* IGNORED */
                        InitializeBinaryPathNoLock(force);

                        /* IGNORED */
                        InitializeAssemblyPathNoLock(force);

                        /* IGNORED */
                        InitializeEntryAssemblyPathNoLock(force);
                    }

                    if (refresh)
                    {
                        /* NO RESULT */
                        RefreshAssemblyPackagePathsNoLock(force);

                        /* NO RESULT */
                        RefreshRawPackagePathsNoLock(force);

                        /* NO RESULT */
                        RefreshPackagePathsNoLock(force);
                    }

                    return true;
                }
                else
                {
                    TraceOps.LockTrace(
                        "SetupPaths",
                        typeof(GlobalState).Name, true,
                        TracePriority.LockError,
                        MaybeWhoHasLock());
                }
            }
            finally
            {
                ExitLock(ref locked); /* TRANSACTIONAL */
            }

            return false;
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Package Path Support Methods
        #region Library Package Path Support Methods
        /// <summary>
        /// This method gets the package path name for the specified package
        /// type, appending the major and minor version components when a
        /// version is supplied.
        /// </summary>
        /// <param name="packageType">
        /// The package type whose path name is being queried.
        /// </param>
        /// <param name="version">
        /// The version whose major and minor components are appended to the
        /// result.  This value may be null.
        /// </param>
        /// <param name="default">
        /// The default name to use when no name is associated with the
        /// specified package type.  This value may be null.
        /// </param>
        /// <returns>
        /// The package path name, or null if none is available.
        /// </returns>
        public static string GetPackagePath( /* MAY RETURN NULL */
            PackageType packageType, /* in */
            Version version,         /* OPTIONAL: May be null. */
            string @default          /* OPTIONAL: May be null. */
            ) /* THREAD-SAFE */
        {
            string result = GetPackageTypeName(packageType, @default);

            if (!String.IsNullOrEmpty(result) && (version != null))
                result += FormatOps.MajorMinor(version);

            return result;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method gets the package path relative to the specified
        /// assembly, optionally appending the package name and version
        /// components.
        /// </summary>
        /// <param name="assembly">
        /// The assembly used as the basis for computing the library path.
        /// This value may be null.
        /// </param>
        /// <param name="name">
        /// The package name to append to the library path.  This value may be
        /// null.
        /// </param>
        /// <param name="version">
        /// The version whose major and minor components are appended to the
        /// result.  This value may be null.
        /// </param>
        /// <param name="pathFlags">
        /// The flags used to control how the underlying library path is
        /// computed.
        /// </param>
        /// <returns>
        /// The package path, or null if no suitable library path is found.
        /// </returns>
        public static string GetPackagePath(
            Assembly assembly,  /* OPTIONAL: May be null. */
            string name,        /* OPTIONAL: May be null. */
            Version version,    /* OPTIONAL: May be null. */
            PathFlags pathFlags
            ) /* THREAD-SAFE */
        {
            string result = GetLibraryPath(assembly, pathFlags);

            if (!FlagOps.HasFlags(
                    pathFlags, PathFlags.Absolute, true) ||
                !String.IsNullOrEmpty(result))
            {
                if (!String.IsNullOrEmpty(name))
                {
                    result = PathOps.CombinePath(
                            null, result, name);

                    if (version != null)
                        result += FormatOps.MajorMinor(version);
                }
            }

            return result;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method gets the package file name, consisting of the package
        /// name followed by the package version.
        /// </summary>
        /// <returns>
        /// The package file name, or null if the package name is not
        /// available.
        /// </returns>
        public static string GetPackageFileNameOnly()
        {
            string name = GetPackageName(); /* "Eagle" */

            if (String.IsNullOrEmpty(name))
                return null;

            StringBuilder builder = StringBuilderFactory.Create();

            builder.Append(name);

            Version version = GetPackageVersion(null);

            if (version != null)
                builder.Append(version);

            return StringBuilderCache.GetStringAndRelease(ref builder);
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Unix Package Path Support Methods
#if UNIX
        /// <summary>
        /// This method gets the Unix-specific package path, optionally
        /// appending the package name and version components.
        /// </summary>
        /// <param name="name">
        /// The package name to append to the Unix library path.  This value
        /// may be null.
        /// </param>
        /// <param name="version">
        /// The version whose major and minor components are appended to the
        /// result.  This value may be null.
        /// </param>
        /// <param name="pathFlags">
        /// The flags used to control how the underlying Unix library path is
        /// computed.
        /// </param>
        /// <returns>
        /// The Unix package path, or null if no suitable library path is
        /// found.
        /// </returns>
        private static string GetUnixPackagePath(
            string name,
            Version version,
            PathFlags pathFlags
            ) /* THREAD-SAFE */
        {
            string result = GetUnixLibraryPath(pathFlags);

            if (!FlagOps.HasFlags(pathFlags, PathFlags.Absolute, true) ||
                !String.IsNullOrEmpty(result))
            {
                if (!String.IsNullOrEmpty(name))
                {
                    result = PathOps.CombinePath(null, result, name);

                    if (version != null)
                        result += FormatOps.MajorMinor(version);
                }
            }

            return result;
        }
#endif
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Assembly Package Path Support Methods
        /// <summary>
        /// This method gets the assembly-relative package path, optionally
        /// appending the package name and version components.
        /// </summary>
        /// <param name="name">
        /// The package name to append to the assembly library path.  This
        /// value may be null.
        /// </param>
        /// <param name="version">
        /// The version whose major and minor components are appended to the
        /// result.  This value may be null.
        /// </param>
        /// <returns>
        /// The assembly package path, or null if the assembly path is not
        /// available.
        /// </returns>
        private static string GetAssemblyPackagePath(
            string name,
            Version version
            ) /* THREAD-SAFE */
        {
            string result = null;

            if (HaveAssemblyPath()) /* NOTE: Needed by GetAssemblyPath(). */
            {
                string basePath = AlwaysGetAssemblyPath();

                if (!String.IsNullOrEmpty(basePath))
                {
                    result = PathOps.CombinePath(
                        null, basePath, TclVars.Path.Lib);

                    if (!String.IsNullOrEmpty(name))
                    {
                        result = PathOps.CombinePath(null, result, name);

                        if (version != null)
                            result += FormatOps.MajorMinor(version);
                    }
                }
            }

            return result;
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Raw Binary Base Package Path Support Methods
        /// <summary>
        /// This method gets the package path relative to the raw binary base
        /// path, optionally appending the package name and version components.
        /// </summary>
        /// <param name="name">
        /// The package name to append to the raw binary base library path.
        /// This value may be null.
        /// </param>
        /// <param name="version">
        /// The version whose major and minor components are appended to the
        /// result.  This value may be null.
        /// </param>
        /// <returns>
        /// The raw binary base package path, or null if the binary path is
        /// not available.
        /// </returns>
        private static string GetRawBinaryBasePackagePath(
            string name,
            Version version
            ) /* THREAD-SAFE */
        {
            string result = null;
            string binaryPath = null;

            if (TryGetBinaryPath(ref binaryPath))
            {
                string basePath = GetRawBinaryBasePath(binaryPath);

                if (!String.IsNullOrEmpty(basePath))
                {
                    result = PathOps.CombinePath(
                        null, basePath, TclVars.Path.Lib);

                    if (!String.IsNullOrEmpty(name))
                    {
                        result = PathOps.CombinePath(null, result, name);

                        if (version != null)
                            result += FormatOps.MajorMinor(version);
                    }
                }
            }

            return result;
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Raw Base Package Path Support Methods
        /// <summary>
        /// This method gets the package path relative to the raw base path,
        /// optionally appending the package name and version components.
        /// </summary>
        /// <param name="name">
        /// The package name to append to the raw base library path.  This
        /// value may be null.
        /// </param>
        /// <param name="version">
        /// The version whose major and minor components are appended to the
        /// result.  This value may be null.
        /// </param>
        /// <returns>
        /// The raw base package path, or null if the assembly path is not
        /// available.
        /// </returns>
        private static string GetRawBasePackagePath(
            string name,
            Version version
            ) /* THREAD-SAFE */
        {
            string result = null;

            if (HaveAssemblyPath()) /* NOTE: Needed by GetRawBasePath(). */
            {
                string basePath = GetRawBasePath();

                if (!String.IsNullOrEmpty(basePath))
                {
                    result = PathOps.CombinePath(
                        null, basePath, TclVars.Path.Lib);

                    if (!String.IsNullOrEmpty(name))
                    {
                        result = PathOps.CombinePath(null, result, name);

                        if (version != null)
                            result += FormatOps.MajorMinor(version);
                    }
                }
            }

            return result;
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Package Path Global Variable Management Methods
        /// <summary>
        /// This method gets the cached assembly package root path, under the
        /// protection of the path lock.
        /// </summary>
        /// <returns>
        /// The assembly package root path, or null if it is not available or
        /// the path lock could not be acquired.
        /// </returns>
        public static string GetAssemblyPackageRootPath() /* THREAD-SAFE */
        {
            bool locked = false;

            try
            {
                PathHardTryLock(ref locked); /* TRANSACTIONAL */

                if (locked)
                {
                    return assemblyPackageRootPath;
                }
                else
                {
                    TraceOps.LockTrace(
                        "GetAssemblyPackageRootPath",
                        typeof(GlobalState).Name, true,
                        TracePriority.LockError,
                        MaybeWhoHasLock());
                }
            }
            finally
            {
                ExitLock(ref locked); /* TRANSACTIONAL */
            }

            return null;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method gets the cached package peer binary path, under the
        /// protection of the path lock.
        /// </summary>
        /// <returns>
        /// The package peer binary path, or null if it is not available or
        /// the path lock could not be acquired.
        /// </returns>
        public static string GetPackagePeerBinaryPath() /* THREAD-SAFE */
        {
            bool locked = false;

            try
            {
                PathHardTryLock(ref locked); /* TRANSACTIONAL */

                if (locked)
                {
                    return packagePeerBinaryPath;
                }
                else
                {
                    TraceOps.LockTrace(
                        "GetPackagePeerBinaryPath",
                        typeof(GlobalState).Name, true,
                        TracePriority.LockError,
                        MaybeWhoHasLock());
                }
            }
            finally
            {
                ExitLock(ref locked); /* TRANSACTIONAL */
            }

            return null;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method gets the cached package peer assembly path, under the
        /// protection of the path lock.
        /// </summary>
        /// <returns>
        /// The package peer assembly path, or null if it is not available or
        /// the path lock could not be acquired.
        /// </returns>
        public static string GetPackagePeerAssemblyPath() /* THREAD-SAFE */
        {
            bool locked = false;

            try
            {
                PathHardTryLock(ref locked); /* TRANSACTIONAL */

                if (locked)
                {
                    return packagePeerAssemblyPath;
                }
                else
                {
                    TraceOps.LockTrace(
                        "GetPackagePeerAssemblyPath",
                        typeof(GlobalState).Name, true,
                        TracePriority.LockError,
                        MaybeWhoHasLock());
                }
            }
            finally
            {
                ExitLock(ref locked); /* TRANSACTIONAL */
            }

            return null;
        }
        #endregion
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Auto-Path Support Methods
        /// <summary>
        /// This method determines whether the path associated with the
        /// specified client data should be added to the auto-path dictionary.
        /// </summary>
        /// <param name="autoPaths">
        /// The dictionary of existing auto-path entries to check against for
        /// duplicate paths.
        /// </param>
        /// <param name="clientData">
        /// The client data containing the candidate path and its associated
        /// name.
        /// </param>
        /// <param name="strictAutoPath">
        /// Non-zero to require that the candidate path refer to an existing
        /// directory.
        /// </param>
        /// <param name="value">
        /// Upon success, receives the path that should be added; otherwise,
        /// receives null.
        /// </param>
        /// <returns>
        /// True if the path should be added to the auto-path dictionary;
        /// otherwise, false.
        /// </returns>
        private static bool ShouldAddToAutoPathList(
            AutoPathDictionary autoPaths, /* in */
            PathClientData clientData,    /* in */
            bool strictAutoPath,          /* in */
            out string value              /* out */
            )
        {
            value = null;

            if (autoPaths == null)
                return false;

            if (clientData == null)
                return false;

            string path = clientData.Path;

            if (String.IsNullOrEmpty(path))
                return false;

            if (autoPaths.ContainsKey(path))
                return false;

            if (strictAutoPath && !Directory.Exists(path))
                return false;

            string name = clientData.Name;

            if (!String.IsNullOrEmpty(name))
            {
                string envVarName = String.Format("No_{0}", name);

                if (CommonOps.Environment.DoesVariableExist(envVarName))
                    return false;
            }

            value = path;
            return true;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method conditionally adds the path associated with the
        /// specified client data to the auto-path dictionary, if it qualifies
        /// for inclusion.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter context, used for diagnostic tracing. This
        /// parameter may be null.
        /// </param>
        /// <param name="autoPaths">
        /// The dictionary of auto-path entries to add to.
        /// </param>
        /// <param name="clientData">
        /// The client data containing the candidate path and its associated
        /// name.
        /// </param>
        /// <param name="showAutoPath">
        /// Non-zero to emit diagnostic tracing about the added path.
        /// </param>
        /// <param name="strictAutoPath">
        /// Non-zero to require that the candidate path refer to an existing
        /// directory.
        /// </param>
        private static void MaybeAddToAutoPathList(
            Interpreter interpreter,      /* in: OPTIONAL */
            AutoPathDictionary autoPaths, /* in */
            PathClientData clientData,    /* in */
            bool showAutoPath,            /* in */
            bool strictAutoPath           /* in */
            )
        {
            string value;

            if (ShouldAddToAutoPathList(
                    autoPaths, clientData, strictAutoPath, out value))
            {
                autoPaths.Add(value, clientData);

                if (showAutoPath && (clientData != null))
                {
                    string name = clientData.Name;

                    TraceOps.DebugWriteTo(
                        interpreter, String.Format(
                        "MaybeAddToAutoPathList: " +
                        "name = {0}, value = {1}",
                        FormatOps.WrapOrNull(name),
                        FormatOps.WrapOrNull(value)),
                        true);
                }
            }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method builds the auto-path dictionary for the specified
        /// interpreter, including its associated paths.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter whose paths are to be considered. This parameter
        /// may be null.
        /// </param>
        /// <param name="libraryPath">
        /// The script library path to consider. This parameter may be null.
        /// </param>
        /// <param name="autoPathList">
        /// The list of auto-path entries to consider. This parameter may be
        /// null.
        /// </param>
        /// <param name="libraryOnly">
        /// Non-zero to consider only paths that may contain the core script
        /// library.
        /// </param>
        /// <param name="showAutoPath">
        /// Non-zero to emit diagnostic tracing about the added paths.
        /// </param>
        /// <param name="strictAutoPath">
        /// Non-zero to require that candidate paths refer to existing
        /// directories.
        /// </param>
        /// <param name="autoPaths">
        /// Upon return, contains the populated auto-path dictionary, created
        /// if necessary.
        /// </param>
        private static void GetInterpreterAutoPathList(
            Interpreter interpreter,         /* in: OPTIONAL */
            string libraryPath,              /* in: OPTIONAL */
            StringList autoPathList,         /* in: OPTIONAL */
            bool libraryOnly,                /* in */
            bool showAutoPath,               /* in */
            bool strictAutoPath,             /* in */
            ref AutoPathDictionary autoPaths /* in, out */
            ) /* THREAD-SAFE */
        {
            //
            // HACK: First, make sure various extra paths are initialized;
            //       however, do not forcibly reset them.
            //
            SetupPaths(true, true, false);

            ///////////////////////////////////////////////////////////////////

            PathClientDataDictionary paths = null;
            bool locked = false;

            try
            {
                PathHardTryLock(ref locked); /* TRANSACTIONAL */

                if (locked)
                {
                    int sequence = 0;

                    PopulatePathsNoLock(
                        interpreter, true, libraryOnly, false,
                        ref sequence, ref paths);
                }
                else
                {
                    TraceOps.LockTrace(
                        "GetInterpreterAutoPathList",
                        typeof(GlobalState).Name, true,
                        TracePriority.LockError,
                        MaybeWhoHasLock());
                }
            }
            finally
            {
                ExitLock(ref locked); /* TRANSACTIONAL */
            }

            ///////////////////////////////////////////////////////////////////

            if (paths == null)
                return;

            IEnumerable<PathClientDataPair> pairs =
                paths.GetPairsInOrder(false);

            if (pairs == null)
                return;

            ///////////////////////////////////////////////////////////////////

            if (autoPaths == null)
                autoPaths = new AutoPathDictionary();

            foreach (PathClientDataPair pair in pairs)
            {
                MaybeAddToAutoPathList(
                    interpreter, autoPaths, pair.Value,
                    showAutoPath, strictAutoPath);
            }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method builds the auto-path dictionary using the paths that
        /// are shared across all interpreters.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter context, used for diagnostic tracing. This
        /// parameter may be null.
        /// </param>
        /// <param name="libraryOnly">
        /// Non-zero to consider only paths that may contain the core script
        /// library.
        /// </param>
        /// <param name="showAutoPath">
        /// Non-zero to emit diagnostic tracing about the added paths.
        /// </param>
        /// <param name="strictAutoPath">
        /// Non-zero to require that candidate paths refer to existing
        /// directories.
        /// </param>
        /// <param name="autoPaths">
        /// Upon return, contains the populated auto-path dictionary, created
        /// if necessary.
        /// </param>
        private static void GetSharedAutoPathList(
            Interpreter interpreter,         /* in: OPTIONAL */
            bool libraryOnly,                /* in */
            bool showAutoPath,               /* in */
            bool strictAutoPath,             /* in */
            ref AutoPathDictionary autoPaths /* in, out */
            ) /* THREAD-SAFE */
        {
            //
            // HACK: First, make sure various extra paths are initialized;
            //       however, do not forcibly reset them.
            //
            SetupPaths(true, true, false);

            ///////////////////////////////////////////////////////////////////

            PathClientDataDictionary paths = null;
            bool locked = false;

            try
            {
                PathHardTryLock(ref locked); /* TRANSACTIONAL */

                if (locked)
                {
                    int sequence = 0;

                    PopulatePathsNoLock(
                        null, false, libraryOnly, false,
                        ref sequence, ref paths);
                }
                else
                {
                    TraceOps.LockTrace(
                        "GetSharedAutoPathList",
                        typeof(GlobalState).Name, true,
                        TracePriority.LockError,
                        MaybeWhoHasLock());
                }
            }
            finally
            {
                ExitLock(ref locked); /* TRANSACTIONAL */
            }

            ///////////////////////////////////////////////////////////////////

            if (paths == null)
                return;

            IEnumerable<PathClientDataPair> pairs =
                paths.GetPairsInOrder(false);

            if (pairs == null)
                return;

            ///////////////////////////////////////////////////////////////////

            if (autoPaths == null)
                autoPaths = new AutoPathDictionary();

            foreach (PathClientDataPair pair in pairs)
            {
                MaybeAddToAutoPathList(
                    interpreter, autoPaths, pair.Value,
                    showAutoPath, strictAutoPath);
            }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method resets the shared auto-path list to null so that it
        /// will be reinitialized on the next request.
        /// </summary>
        private static void ResetSharedAutoPathList()
        {
            bool locked = false;

            try
            {
                PathHardTryLock(ref locked); /* TRANSACTIONAL */

                if (locked)
                {
                    if (sharedAutoPathList == null)
                        return;

                    sharedAutoPathList = null;

                    TraceOps.DebugTrace("ResetSharedAutoPathList: complete",
                        typeof(GlobalState).Name, TracePriority.StartupDebug);
                }
                else
                {
                    TraceOps.LockTrace(
                        "ResetSharedAutoPathList",
                        typeof(GlobalState).Name, true,
                        TracePriority.LockError,
                        MaybeWhoHasLock());
                }
            }
            finally
            {
                ExitLock(ref locked); /* TRANSACTIONAL */
            }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Auto-Path Global Variable Management Methods
        //
        // WARNING: Assumes the static lock is already held.
        //
        // WARNING: The ordering of the paths in this method is somewhat
        //          bad and counter-intuitive; in the future, it may be
        //          changed.
        //
        /// <summary>
        /// This method populates the specified dictionary with the configured
        /// paths, without acquiring the associated lock.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter whose paths are to be included. This parameter may
        /// be null.
        /// </param>
        /// <param name="interpreterOnly">
        /// Non-zero to include only paths that are specific to the specified
        /// interpreter.
        /// </param>
        /// <param name="libraryOnly">
        /// Non-zero to include only paths that may contain the core script
        /// library.
        /// </param>
        /// <param name="all">
        /// Non-zero to include all paths, including the non-auto-path and
        /// diagnostic entries.
        /// </param>
        /// <param name="sequence">
        /// A counter used to assign relative ordering to each added path
        /// entry; updated as entries are added.
        /// </param>
        /// <param name="paths">
        /// The dictionary to populate, created if necessary; updated to
        /// contain the configured paths.
        /// </param>
        private static void PopulatePathsNoLock(
            Interpreter interpreter,           /* in: OPTIONAL */
            bool interpreterOnly,              /* in */
            bool libraryOnly,                  /* in */
            bool all,                          /* in */
            ref int sequence,                  /* in, out */
            ref PathClientDataDictionary paths /* in, out */
            )
        {
            string group; /* REUSED */
            string assemblyPath; /* REUSED */
            int count; /* REUSED */

            if (paths == null)
                paths = new PathClientDataDictionary();

            ///////////////////////////////////////////////////////////////////

            #region Auto-Path
            if (interpreter != null)
            {
                string interpreterLibraryPath = null;
                StringList interpreterAutoPathList = null;

                ///////////////////////////////////////////////////////////////

                FetchInterpreterPaths(interpreter,
                    ref interpreterLibraryPath, ref interpreterAutoPathList);

                ///////////////////////////////////////////////////////////////

                group = "interpreter";

                ///////////////////////////////////////////////////////////////

                paths.Add(new PathClientData(
                    ++sequence,
                    null,
                    "interpreterLibraryPath",
                    group,
                    "interpreter library path",
                    interpreterLibraryPath
                ), all);

                ///////////////////////////////////////////////////////////////

                //
                // NOTE: In "library only" mode, only consider paths which
                //       can contain the core script library.
                //
                if (libraryOnly)
                    return;

                ///////////////////////////////////////////////////////////////

                if (interpreterAutoPathList != null)
                {
                    count = interpreterAutoPathList.Count;

                    for (int index = 0; index < count; index++)
                    {
                        paths.Add(new PathClientData(
                            ++sequence, index,
                            "interpreterAutoPathList",
                            group,
                            "interpreter auto-path list",
                            interpreterAutoPathList[index]
                        ), all);
                    }
                }
                else
                {
                    paths.Add(new PathClientData(
                        ++sequence,
                        null,
                        "interpreterAutoPathList",
                        group,
                        "interpreter auto-path list",
                        null
                    ), all);
                }
            }

            ///////////////////////////////////////////////////////////////////

            //
            // NOTE: In "interpreter only" mode, only consider paths which
            //       are specific to the specified interpreter.
            //
            if (interpreterOnly)
                return;

            ///////////////////////////////////////////////////////////////////

            group = "auto-path";

            ///////////////////////////////////////////////////////////////////

            paths.Add(new PathClientData(
                ++sequence,
                null,
                "libraryPath",
                group,
                "library path",
                libraryPath
            ), all);

            ///////////////////////////////////////////////////////////////////

            //
            // NOTE: In "library only" mode, only consider paths which
            //       can contain the core script library.
            //
            if (libraryOnly)
                return;

            ///////////////////////////////////////////////////////////////////

            assemblyPath = InitializeOrGetAssemblyPath(false);

            paths.Add(new PathClientData(
                ++sequence,
                null,
                "thisAssemblyPath",
                group,
                "this assembly path",
                assemblyPath
            ), all);

            ///////////////////////////////////////////////////////////////////

            paths.Add(new PathClientData(
                ++sequence,
                null,
                "tclLibraryPath",
                group,
                "Tcl library path",
                tclLibraryPath
            ), all);

            ///////////////////////////////////////////////////////////////////

            if (autoPathList != null)
            {
                count = autoPathList.Count;

                for (int index = 0; index < count; index++)
                {
                    paths.Add(new PathClientData(
                        ++sequence,
                        index,
                        "autoPathList",
                        group,
                        "auto-path list",
                        autoPathList[index]
                    ), all);
                }
            }
            else
            {
                paths.Add(new PathClientData(
                    ++sequence,
                    null,
                    "autoPathList",
                    group,
                    "auto-path list",
                    null
                ), all);
            }

            ///////////////////////////////////////////////////////////////////

            if (tclAutoPathList != null)
            {
                count = tclAutoPathList.Count;

                for (int index = 0; index < count; index++)
                {
                    paths.Add(new PathClientData(
                        ++sequence,
                        index,
                        "tclAutoPathList",
                        group,
                        "Tcl auto-path list",
                        tclAutoPathList[index]
                    ), all);
                }
            }
            else
            {
                paths.Add(new PathClientData(
                    ++sequence,
                    null,
                    "tclAutoPathList",
                    group,
                    "Tcl auto-path list",
                    null
                ), all);
            }

            ///////////////////////////////////////////////////////////////////

#if UNIX
            paths.Add(new PathClientData(
                ++sequence,
                null,
                "unixPackageNameLocalPath",
                group,
                "Unix package name local path",
                unixPackageNameLocalPath
            ), all);

            ///////////////////////////////////////////////////////////////////

            paths.Add(new PathClientData(
                ++sequence,
                null,
                "unixPackageNamePath",
                group,
                "Unix package name path",
                unixPackageNamePath
            ), all);
#endif

            ///////////////////////////////////////////////////////////////////

            paths.Add(new PathClientData(
                ++sequence,
                null,
                "packageNameBinaryPath",
                group,
                "package name binary path",
                packageNameBinaryPath
            ), all);

            ///////////////////////////////////////////////////////////////////

            paths.Add(new PathClientData(
                ++sequence,
                null,
                "packageNameAssemblyPath",
                group,
                "package name assembly path",
                packageNameAssemblyPath
            ), all);

            ///////////////////////////////////////////////////////////////////

            paths.Add(new PathClientData(
                ++sequence,
                null,
                "packageNameRootPath",
                group,
                "package name root path",
                packageNameRootPath
            ), all);

            ///////////////////////////////////////////////////////////////////

            paths.Add(new PathClientData(
                ++sequence,
                null,
                "packagePeerBinaryPath",
                group,
                "package peer binary path",
                packagePeerBinaryPath
            ), all);

            ///////////////////////////////////////////////////////////////////

            paths.Add(new PathClientData(
                ++sequence,
                null,
                "packagePeerAssemblyPath",
                group,
                "package peer assembly path",
                packagePeerAssemblyPath
            ), all);

            ///////////////////////////////////////////////////////////////////

            paths.Add(new PathClientData(
                ++sequence,
                null,
                "packageRootPath",
                group,
                "package root path",
                packageRootPath
            ), all);

            ///////////////////////////////////////////////////////////////////

            paths.Add(new PathClientData(
                ++sequence,
                null,
                "assemblyPackageNamePath",
                group,
                "assembly package name path",
                assemblyPackageNamePath
            ), all);

            ///////////////////////////////////////////////////////////////////

            paths.Add(new PathClientData(
                ++sequence,
                null,
                "assemblyPackageRootPath",
                group,
                "assembly package root path",
                assemblyPackageRootPath
            ), all);

            ///////////////////////////////////////////////////////////////////

            paths.Add(new PathClientData(
                ++sequence,
                null,
                "rawBinaryBasePackageNamePath",
                group,
                "raw binary base package name path",
                rawBinaryBasePackageNamePath
            ), all);

            ///////////////////////////////////////////////////////////////////

            paths.Add(new PathClientData(
                ++sequence,
                null,
                "rawBinaryBasePackageRootPath",
                group,
                "raw binary base package root path",
                rawBinaryBasePackageRootPath
            ), all);

            ///////////////////////////////////////////////////////////////////

            paths.Add(new PathClientData(
                ++sequence,
                null,
                "rawBasePackageNamePath",
                group,
                "raw base package name path",
                rawBasePackageNamePath
            ), all);

            ///////////////////////////////////////////////////////////////////

            paths.Add(new PathClientData(
                ++sequence,
                null,
                "rawBasePackageRootPath",
                group,
                "raw base package root path",
                rawBasePackageRootPath
            ), all);
            #endregion

            ///////////////////////////////////////////////////////////////////

            if (!all)
                return;

            ///////////////////////////////////////////////////////////////////

            #region Non-Auto-Path (Other)
            group = "other";

            ///////////////////////////////////////////////////////////////////

            paths.Add(new PathClientData(
                ++sequence,
                null,
                "sharedBinaryPath",
                group,
                "shared binary path",
                sharedBinaryPath
            ), all);

            ///////////////////////////////////////////////////////////////////

            assemblyPath = InitializeOrGetEntryAssemblyPath(false);

            paths.Add(new PathClientData(
                ++sequence,
                null,
                "entryAssemblyPath",
                group,
                "entry assembly path",
                assemblyPath
            ), all);

            ///////////////////////////////////////////////////////////////////

            paths.Add(new PathClientData(
                ++sequence,
                null,
                "sharedBasePath",
                group,
                "shared base path",
                sharedBasePath
            ), all);

            ///////////////////////////////////////////////////////////////////

            paths.Add(new PathClientData(
                ++sequence,
                null,
                "sharedLibraryPath",
                group,
                "shared library path",
                sharedLibraryPath
            ), all);

            ///////////////////////////////////////////////////////////////////

#if NATIVE && TCL
            paths.Add(new PathClientData(
                ++sequence,
                null,
                "tclPackageNamePath",
                group,
                "Tcl package name path",
                tclPackageNamePath
            ), all);

            ///////////////////////////////////////////////////////////////////

            paths.Add(new PathClientData(
                ++sequence,
                null,
                "tclPackageNameRootPath",
                group,
                "Tcl package name root path",
                tclPackageNameRootPath
            ), all);
#endif

            ///////////////////////////////////////////////////////////////////

            if (sharedAutoPathList != null)
            {
                count = sharedAutoPathList.Count;

                for (int index = 0; index < count; index++)
                {
                    paths.Add(new PathClientData(
                        ++sequence,
                        index,
                        "sharedAutoPathList",
                        group,
                        "shared auto-path list",
                        sharedAutoPathList[index]
                    ), all);
                }
            }
            else
            {
                paths.Add(new PathClientData(
                    ++sequence,
                    null,
                    "sharedAutoPathList",
                    group,
                    "shared auto-path list",
                    null
                ), all);
            }
            #endregion

            ///////////////////////////////////////////////////////////////////

            #region Non-Auto-Path (Diagnostic)
            string name; /* REUSED */
            string path; /* REUSED */

            ///////////////////////////////////////////////////////////////////

            group = "diagnostic";

            ///////////////////////////////////////////////////////////////////

            name = "GetBasePath()";

            try
            {
                path = GetBasePath();

                paths.Add(new PathClientData(
                    ++sequence,
                    null,
                    name,
                    group,
                    null,
                    path
                ), all);
            }
            catch (Exception e)
            {
                paths.Add(new PathClientData(
                    ++sequence, null, name, group, e
                ), all);
            }

            ///////////////////////////////////////////////////////////////////

            name = "GetRawBinaryBasePath()";

            try
            {
                path = GetRawBinaryBasePath();

                paths.Add(new PathClientData(
                    ++sequence,
                    null,
                    name,
                    group,
                    null,
                    path
                ), all);
            }
            catch (Exception e)
            {
                paths.Add(new PathClientData(
                    ++sequence, null, name, group, e
                ), all);
            }

            ///////////////////////////////////////////////////////////////////

            name = "GetRawBasePath()";

            try
            {
                path = GetRawBasePath();

                paths.Add(new PathClientData(
                    ++sequence,
                    null,
                    name,
                    group,
                    null,
                    path
                ), all);
            }
            catch (Exception e)
            {
                paths.Add(new PathClientData(
                    ++sequence, null, name, group, e
                ), all);
            }

            ///////////////////////////////////////////////////////////////////

            name = "GetExternalsPath()";

            try
            {
                path = GetExternalsPath();

                paths.Add(new PathClientData(
                    ++sequence,
                    null,
                    name,
                    group,
                    null,
                    path
                ), all);
            }
            catch (Exception e)
            {
                paths.Add(new PathClientData(
                    ++sequence, null, name, group, e
                ), all);
            }

            ///////////////////////////////////////////////////////////////////

            name = "Path.GetDirectoryName(GetBinaryPath())";

            try
            {
                path = Path.GetDirectoryName(
                    InitializeOrGetBinaryPath(false));

                paths.Add(new PathClientData(
                    ++sequence,
                    null,
                    name,
                    group,
                    null,
                    path
                ), all);
            }
            catch (Exception e)
            {
                paths.Add(new PathClientData(
                    ++sequence, null, name, group, e
                ), all);
            }

            ///////////////////////////////////////////////////////////////////

            name = "Path.GetFullPath(Path.GetDirectoryName(GetBinaryPath()))";

            try
            {
                path = Path.GetDirectoryName(
                    InitializeOrGetBinaryPath(false));

                paths.Add(new PathClientData(
                    ++sequence,
                    null,
                    name,
                    group,
                    null,
                    (path != null) ? Path.GetFullPath(path) : null
                ), all);
            }
            catch (Exception e)
            {
                paths.Add(new PathClientData(
                    ++sequence, null, name, group, e
                ), all);
            }

            ///////////////////////////////////////////////////////////////////

            name = "Path.GetFullPath(GetBasePath())";

            try
            {
                path = GetBasePath();

                paths.Add(new PathClientData(
                    ++sequence,
                    null,
                    name,
                    group,
                    null,
                    (path != null) ? Path.GetFullPath(path) : null
                ), all);
            }
            catch (Exception e)
            {
                paths.Add(new PathClientData(
                    ++sequence, null, name, group, e
                ), all);
            }

            ///////////////////////////////////////////////////////////////////

            name = "Path.GetFullPath(GetRawBinaryBasePath())";

            try
            {
                path = GetRawBinaryBasePath();

                paths.Add(new PathClientData(
                    ++sequence,
                    null,
                    name,
                    group,
                    null,
                    (path != null) ? Path.GetFullPath(path) : null
                ), all);
            }
            catch (Exception e)
            {
                paths.Add(new PathClientData(
                    ++sequence, null, name, group, e
                ), all);
            }

            ///////////////////////////////////////////////////////////////////

            name = "Path.GetFullPath(GetRawBasePath())";

            try
            {
                path = GetRawBasePath();

                paths.Add(new PathClientData(
                    ++sequence,
                    null,
                    name,
                    group,
                    null,
                    (path != null) ? Path.GetFullPath(path) : null
                ), all);
            }
            catch (Exception e)
            {
                paths.Add(new PathClientData(
                    ++sequence, null, name, group, e
                ), all);
            }

            ///////////////////////////////////////////////////////////////////

            name = "AssemblyOps.GetCurrentPath(GetAssembly())";

            try
            {
                path = AssemblyOps.GetCurrentPath(GetAssembly());

                paths.Add(new PathClientData(
                    ++sequence,
                    null,
                    name,
                    group,
                    null,
                    path
                ), all);
            }
            catch (Exception e)
            {
                paths.Add(new PathClientData(
                    ++sequence, null, name, group, e
                ), all);
            }

            ///////////////////////////////////////////////////////////////////

            name = "AssemblyOps.GetOriginalPath(GetAssembly())";

            try
            {
                path = AssemblyOps.GetOriginalPath(GetAssembly());

                paths.Add(new PathClientData(
                    ++sequence,
                    null,
                    name,
                    group,
                    null,
                    path
                ), all);
            }
            catch (Exception e)
            {
                paths.Add(new PathClientData(
                    ++sequence, null, name, group, e
                ), all);
            }

            ///////////////////////////////////////////////////////////////////

            name = "AssemblyOps.GetCurrentPath(GetEntryAssembly())";

            try
            {
                path = AssemblyOps.GetCurrentPath(GetEntryAssembly());

                paths.Add(new PathClientData(
                    ++sequence,
                    null,
                    name,
                    group,
                    null,
                    path
                ), all);
            }
            catch (Exception e)
            {
                paths.Add(new PathClientData(
                    ++sequence, null, name, group, e
                ), all);
            }

            ///////////////////////////////////////////////////////////////////

            name = "AssemblyOps.GetOriginalPath(GetEntryAssembly())";

            try
            {
                path = AssemblyOps.GetOriginalPath(GetEntryAssembly());

                paths.Add(new PathClientData(
                    ++sequence,
                    null,
                    name,
                    group,
                    null,
                    path
                ), all);
            }
            catch (Exception e)
            {
                paths.Add(new PathClientData(
                    ++sequence, null, name, group, e
                ), all);
            }
            #endregion
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method populates the specified dictionary with the configured
        /// paths, acquiring the associated lock.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter whose paths are to be included. This parameter may
        /// be null.
        /// </param>
        /// <param name="interpreterOnly">
        /// Non-zero to include only paths that are specific to the specified
        /// interpreter.
        /// </param>
        /// <param name="libraryOnly">
        /// Non-zero to include only paths that may contain the core script
        /// library.
        /// </param>
        /// <param name="all">
        /// Non-zero to include all paths, including the non-auto-path and
        /// diagnostic entries.
        /// </param>
        /// <param name="sequence">
        /// A counter used to assign relative ordering to each added path
        /// entry; updated as entries are added.
        /// </param>
        /// <param name="paths">
        /// The dictionary to populate, created if necessary; updated to
        /// contain the configured paths.
        /// </param>
        public static void PopulatePaths(
            Interpreter interpreter,           /* in: OPTIONAL */
            bool interpreterOnly,              /* in */
            bool libraryOnly,                  /* in */
            bool all,                          /* in */
            ref int sequence,                  /* in, out */
            ref PathClientDataDictionary paths /* in, out */
            )
        {
            bool locked = false;

            try
            {
                PathMetaTryLock(ref locked); /* TRANSACTIONAL */

                if (locked)
                {
                    PopulatePathsNoLock(
                        interpreter, interpreterOnly, libraryOnly, all,
                        ref sequence, ref paths);
                }
                else
                {
                    TraceOps.LockTrace(
                        "PopulatePaths",
                        typeof(GlobalState).Name, true,
                        TracePriority.LockError,
                        MaybeWhoHasLock());
                }
            }
            finally
            {
                ExitLock(ref locked); /* TRANSACTIONAL */
            }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method filters the specified dictionary of paths in place,
        /// optionally removing non-existent and/or duplicate entries.
        /// </summary>
        /// <param name="existingOnly">
        /// Non-zero to remove paths that do not refer to an existing
        /// directory.
        /// </param>
        /// <param name="uniqueOnly">
        /// Non-zero to remove duplicate paths, keying the resulting entries
        /// by their path.
        /// </param>
        /// <param name="paths">
        /// The dictionary of paths to filter; upon return, contains only the
        /// entries that pass the filter.
        /// </param>
        public static void FilterPaths(
            bool existingOnly,                 /* in */
            bool uniqueOnly,                   /* in */
            ref PathClientDataDictionary paths /* in, out */
            )
        {
            if (paths == null)
                return;

            IEnumerable<PathClientDataPair> pairs =
                paths.GetPairsInOrder(false);

            if (pairs == null)
                return;

            PathClientDataDictionary localPaths =
                new PathClientDataDictionary();

            foreach (PathClientDataPair pair in pairs)
            {
                PathClientData clientData = pair.Value;

                if (clientData == null)
                    continue;

                string path = clientData.Path;

                if (path == null)
                    continue;

                if (existingOnly && !Directory.Exists(path))
                    continue;

                localPaths[uniqueOnly ? path : pair.Key] = clientData;
            }

            paths = localPaths;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method temporarily prepends the specified path to the
        /// auto-path environment variable and refreshes the auto-path list,
        /// saving the previous value for later restoration.
        /// </summary>
        /// <param name="path">
        /// The path to prepend to the auto-path. This parameter may be null.
        /// </param>
        /// <param name="verbose">
        /// Non-zero to enable verbose diagnostic output during the refresh.
        /// </param>
        /// <param name="savedLibPath">
        /// Upon return, receives the previous value of the auto-path
        /// environment variable, for later restoration.
        /// </param>
        public static void BeginWithAutoPath( /* EXTERNAL USE ONLY */
            string path,            /* in */
            bool verbose,           /* in */
            ref string savedLibPath /* out */
            )
        {
            savedLibPath = GetEnvironmentVariable(EnvVars.EagleLibPath);

            StringList list = null;

            if (savedLibPath != null)
                list = StringList.FromString(savedLibPath);

            if (list == null)
                list = new StringList();

            if (path != null)
                list.Insert(0, path);

            /* IGNORED */
            SetEnvironmentVariable(EnvVars.EagleLibPath, list.ToString());

            /* NO RESULT */
            RefreshAutoPathList(true, verbose);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method restores the previously saved auto-path environment
        /// variable value and refreshes the auto-path list.
        /// </summary>
        /// <param name="verbose">
        /// Non-zero to enable verbose diagnostic output during the refresh.
        /// </param>
        /// <param name="savedLibPath">
        /// The previously saved auto-path environment variable value to
        /// restore; reset to null upon return.
        /// </param>
        public static void EndWithAutoPath( /* EXTERNAL USE ONLY */
            bool verbose,           /* in */
            ref string savedLibPath /* in, out */
            )
        {
            /* IGNORED */
            SetEnvironmentVariable(EnvVars.EagleLibPath, savedLibPath);

            savedLibPath = null;

            /* NO RESULT */
            RefreshAutoPathList(true, verbose);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method re-queries all auto-path related environment variables,
        /// also resetting the shared auto-path list.
        /// </summary>
        /// <param name="verbose">
        /// Non-zero to enable verbose diagnostic output.
        /// </param>
        public static void RefreshAutoPathList(
            bool verbose
            ) /* THREAD-SAFE */
        {
            RefreshAutoPathList(true, verbose);
        }

        ///////////////////////////////////////////////////////////////////////

        //
        // WARNING: Re-query all auto-path related environment variables now.
        //
        /// <summary>
        /// This method re-queries all auto-path related environment variables.
        /// </summary>
        /// <param name="resetShared">
        /// Non-zero to also reset the shared auto-path list so that it is
        /// reinitialized on the next request.
        /// </param>
        /// <param name="verbose">
        /// Non-zero to enable verbose diagnostic output.
        /// </param>
        private static void RefreshAutoPathList(
            bool resetShared,
            bool verbose
            ) /* THREAD-SAFE */
        {
            bool locked = false;

            try
            {
                PathHardTryLock(ref locked); /* TRANSACTIONAL */

                if (locked)
                {
                    //
                    // WARNING: This is the only place within the core library
                    //          that this variable is set to a non-null value;
                    //          i.e. by default, under normal operation, it is
                    //          always null.
                    //
                    libraryPath = GlobalConfiguration.GetValue(
                        EnvVars.EagleLibrary, GlobalConfiguration.GetFlags(
                        ConfigurationFlags.GlobalStateNoPrefix |
                        ConfigurationFlags.NativePathValue, verbose));

                    //
                    // WARNING: This is the only place within the core library
                    //          that this variable is set to a non-null value;
                    //          i.e. by default, under normal operation, it is
                    //          always null.
                    //
                    autoPathList = StringList.FromString(
                        GlobalConfiguration.GetValue(EnvVars.EagleLibPath,
                        GlobalConfiguration.GetFlags(
                            ConfigurationFlags.GlobalStateNoPrefix |
                            ConfigurationFlags.NativePathListValue, verbose)));

                    ///////////////////////////////////////////////////////////

                    //
                    // WARNING: This is the only place within the core library
                    //          that this variable is set to a non-null value;
                    //          i.e. by default, under normal operation, it is
                    //          always null.
                    //
                    tclLibraryPath = GlobalConfiguration.GetValue(
                        EnvVars.TclLibrary, GlobalConfiguration.GetFlags(
                        ConfigurationFlags.GlobalState |
                        ConfigurationFlags.NativePathValue, verbose));

                    //
                    // WARNING: This is the only place within the core library
                    //          that this variable is set to a non-null value;
                    //          i.e. by default, under normal operation, it is
                    //          always null.
                    //
                    tclAutoPathList = StringList.FromString(
                        GlobalConfiguration.GetValue(EnvVars.TclLibPath,
                        GlobalConfiguration.GetFlags(
                            ConfigurationFlags.GlobalState |
                            ConfigurationFlags.NativePathListValue, verbose)));

                    ///////////////////////////////////////////////////////////

                    if (resetShared)
                    {
                        //
                        // BUGFIX: Reset the shared auto-path so that it will
                        //         be initialized again [using our new paths]
                        //         on the next call to the GetAutoPathList
                        //         method.
                        //
                        ResetSharedAutoPathList();
                    }

                    ///////////////////////////////////////////////////////////

                    TraceOps.DebugTrace("RefreshAutoPathList: complete",
                        typeof(GlobalState).Name, TracePriority.StartupDebug);
                }
                else
                {
                    TraceOps.LockTrace(
                        "RefreshAutoPathList",
                        typeof(GlobalState).Name, true,
                        TracePriority.LockError,
                        MaybeWhoHasLock());
                }
            }
            finally
            {
                ExitLock(ref locked); /* TRANSACTIONAL */
            }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method returns the combined auto-path list for the specified
        /// interpreter, including the shared auto-path list.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter whose paths are to be included. This parameter may
        /// be null.
        /// </param>
        /// <param name="refresh">
        /// Non-zero to force the shared auto-path list to be reinitialized.
        /// </param>
        /// <returns>
        /// The combined auto-path list. This method cannot return null.
        /// </returns>
        public static StringList GetAutoPathList( /* CANNOT RETURN NULL */
            Interpreter interpreter, /* OPTIONAL: May be null. */
            bool refresh
            ) /* THREAD-SAFE */
        {
            string libraryPath = null;
            StringList autoPathList = null;
            InitializeFlags initializeFlags = InitializeFlags.None;

            /* IGNORED */
            FetchInterpreterPathsAndFlags(
                interpreter, ref libraryPath, ref autoPathList,
                ref initializeFlags);

            return GetAutoPathList(
                interpreter, libraryPath, autoPathList, initializeFlags,
                refresh);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method returns the combined auto-path list for the specified
        /// interpreter and paths, including the shared auto-path list.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter whose paths are to be included. This parameter may
        /// be null.
        /// </param>
        /// <param name="libraryPath">
        /// The script library path to consider. This parameter may be null.
        /// </param>
        /// <param name="autoPathList">
        /// The list of auto-path entries to consider. This parameter may be
        /// null.
        /// </param>
        /// <param name="initializeFlags">
        /// Flags that control how the auto-path list is built, including the
        /// diagnostic and strictness behavior.
        /// </param>
        /// <param name="refresh">
        /// Non-zero to force the shared auto-path list to be reinitialized.
        /// </param>
        /// <returns>
        /// The combined auto-path list. This method cannot return null.
        /// </returns>
        public static StringList GetAutoPathList( /* CANNOT RETURN NULL */
            Interpreter interpreter, /* OPTIONAL: May be null. */
            string libraryPath,
            StringList autoPathList,
            InitializeFlags initializeFlags,
            bool refresh
            ) /* THREAD-SAFE */
        {
            bool showAutoPath = FlagOps.HasFlags(
                initializeFlags, InitializeFlags.ShowAutoPath, true);

            bool strictAutoPath = FlagOps.HasFlags(
                initializeFlags, InitializeFlags.StrictAutoPath, true);

            if (showAutoPath)
            {
                TraceOps.DebugWriteTo(interpreter, String.Format(
                    "GetAutoPathList: entered, interpreter = {0}, " +
                    "initializeFlags = {1}, refresh = {2}",
                    FormatOps.InterpreterNoThrow(interpreter),
                    FormatOps.WrapOrNull(initializeFlags), refresh),
                    true);
            }

            AutoPathDictionary autoPaths = null;

            GetInterpreterAutoPathList(
                interpreter, libraryPath, autoPathList, false,
                showAutoPath, strictAutoPath, ref autoPaths);

            bool locked = false;

            try
            {
                PathHardTryLock(ref locked); /* TRANSACTIONAL */

                if (locked)
                {
                    if (refresh || (sharedAutoPathList == null))
                    {
                        if (showAutoPath)
                        {
                            TraceOps.DebugWriteTo(interpreter, String.Format(
                                "GetAutoPathList: Shared auto-path list {0}",
                                (sharedAutoPathList != null) ?
                                    "was initialized" : "was not initialized"),
                                true);
                        }

                        RefreshAutoPathList(false, showAutoPath);

                        AutoPathDictionary sharedAutoPaths = null;

                        GetSharedAutoPathList(
                            interpreter, false, showAutoPath, strictAutoPath,
                            ref sharedAutoPaths);

                        sharedAutoPathList = (sharedAutoPaths != null) ?
                            sharedAutoPaths.GetKeysInOrder(false) : new StringList();

                        if (showAutoPath)
                        {
                            TraceOps.DebugWriteTo(interpreter, String.Format(
                                "GetAutoPathList: Shared auto-path list initialized to: {0}",
                                FormatOps.WrapOrNull(sharedAutoPathList)),
                                true);
                        }
                    }
                    else
                    {
                        if (showAutoPath)
                        {
                            TraceOps.DebugWriteTo(interpreter,
                                "GetAutoPathList: Shared auto-path list already initialized",
                                true);
                        }
                    }

                    if (showAutoPath)
                    {
                        TraceOps.DebugWriteTo(interpreter, String.Format(
                            "GetAutoPathList: exited, interpreter = {0}, " +
                            "initializeFlags = {1}, refresh = {2}",
                            FormatOps.InterpreterNoThrow(interpreter),
                            FormatOps.WrapOrNull(initializeFlags), refresh),
                            true);
                    }

                    //
                    // NOTE: Merge in shared path list into the overall list.
                    //
                    if (autoPaths == null)
                        autoPaths = new AutoPathDictionary();

                    autoPaths.Add(sharedAutoPathList, true);

                    //
                    // NOTE: Create a simple string list based on the path
                    //       list and return it.
                    //
                    return autoPaths.GetKeysInOrder(false);
                }
                else
                {
                    TraceOps.LockTrace(
                        "GetAutoPathList",
                        typeof(GlobalState).Name, true,
                        TracePriority.LockError,
                        MaybeWhoHasLock());
                }
            }
            finally
            {
                ExitLock(ref locked); /* TRANSACTIONAL */
            }

            return null;
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Path Debugging Support Methods
        //
        // WARNING: *DEADLOCK* This requires the interpreter lock.
        //
        /// <summary>
        /// This method gathers the various paths used by the interpreter and adds
        /// them, together with their associated client data, to the supplied
        /// dictionary.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter context to use, if any.
        /// </param>
        /// <param name="all">
        /// Non-zero to gather all available paths; otherwise, only a subset of the
        /// paths is gathered.
        /// </param>
        /// <param name="paths">
        /// Upon return, receives the gathered paths and their associated client
        /// data.  This dictionary may be created if it is initially null.
        /// </param>
        public static void GetPaths(
            Interpreter interpreter,           /* in */
            bool all,                          /* in */
            ref PathClientDataDictionary paths /* in, out */
            ) /* THREAD-SAFE */
        {
            //
            // HACK: First, make sure various extra paths are initialized;
            //       however, do not forcibly reset them.
            //
            SetupPaths(true, true, false);

            ///////////////////////////////////////////////////////////////////

            bool locked = false;

            try
            {
                PathMetaTryLock(ref locked); /* TRANSACTIONAL */

                if (locked)
                {
                    int sequence = 0;

                    PopulatePathsNoLock(
                        interpreter, false, false, all,
                        ref sequence, ref paths);
                }
                else
                {
                    TraceOps.LockTrace(
                        "GetPaths",
                        typeof(GlobalState).Name, true,
                        TracePriority.LockError,
                        MaybeWhoHasLock());
                }
            }
            finally
            {
                ExitLock(ref locked); /* TRANSACTIONAL */
            }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method gathers the various paths used by the interpreter and
        /// displays them, optionally filtering them based on the specified flags.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter context to use, if any.
        /// </param>
        /// <param name="flags">
        /// The flags used to control which paths are gathered and how they are
        /// filtered prior to being displayed.
        /// </param>
        public static void DisplayPaths(
            Interpreter interpreter, /* in */
            DebugPathFlags flags     /* in */
            ) /* THREAD-SAFE */
        {
            PathClientDataDictionary paths = null;

            GetPaths(
                interpreter, FlagOps.HasFlags(flags,
                DebugPathFlags.GetAll, true), ref paths);

            if (FlagOps.HasFlags(
                    flags, DebugPathFlags.UseFilter, true))
            {
                FilterPaths(
                    FlagOps.HasFlags(flags,
                        DebugPathFlags.ExistingOnly, true),
                    FlagOps.HasFlags(flags,
                        DebugPathFlags.UniqueOnly, true),
                    ref paths);
            }

            if (paths == null)
                return;

            IEnumerable<PathClientDataPair> pairs =
                paths.GetPairsInOrder(false);

            if (pairs == null)
                return;

            foreach (PathClientDataPair pair in pairs)
            {
                PathClientData clientData = pair.Value;

                string description;
                string path;

                if (clientData != null)
                {
                    description = clientData.Description;
                    path = clientData.Path;
                }
                else
                {
                    description = pair.Key;
                    path = null;
                }

                /* EXEMPT */
                DebugOps.WriteTo(
                    interpreter, String.Format("{0} = {1}",
                    FormatOps.WrapOrNull(description),
                    FormatOps.WrapOrNull(path)), true);
            }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Global Trusted Hashes Support Methods
        /// <summary>
        /// This method creates and returns a copy of the global list of trusted
        /// hashes, optionally clearing the original list.
        /// </summary>
        /// <param name="clear">
        /// Non-zero to clear the global list of trusted hashes after copying it.
        /// </param>
        /// <returns>
        /// A copy of the global list of trusted hashes, or null if there are no
        /// trusted hashes or the required lock could not be acquired.
        /// </returns>
        public static StringList CopyTrustedHashes(
            bool clear /* in */
            )
        {
            StringList result = null;
            bool locked = false;

            try
            {
                HardTryLock(ref locked); /* TRANSACTIONAL */

                if (locked)
                {
                    if (trustedHashes != null)
                    {
                        result = new StringList(trustedHashes);

                        if (clear)
                            trustedHashes.Clear();
                    }
                }
                else
                {
                    TraceOps.LockTrace(
                        "CopyTrustedHashes",
                        typeof(GlobalState).Name, true,
                        TracePriority.LockError,
                        MaybeWhoHasLock());
                }
            }
            finally
            {
                ExitLock(ref locked); /* TRANSACTIONAL */
            }

            return result;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method copies the trusted hashes associated with the specified
        /// interpreter into the global list of trusted hashes, optionally clearing
        /// the global list beforehand.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter context to use, if any.
        /// </param>
        /// <param name="clear">
        /// Non-zero to clear the global list of trusted hashes before adding the
        /// copied hashes.
        /// </param>
        /// <param name="error">
        /// Upon failure, receives an error message that describes why the copy
        /// could not be performed.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise, an error code.
        /// </returns>
        public static ReturnCode CopyTrustedHashes(
            Interpreter interpreter, /* in */
            bool clear,              /* in */
            ref Result error         /* out */
            )
        {
            return AddTrustedHashes(RuntimeOps.CombineOrCopyTrustedHashes(
                interpreter, false, true, clear), clear, ref error);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method adds the specified hashes to the global list of trusted
        /// hashes, optionally clearing the global list beforehand.
        /// </summary>
        /// <param name="hashes">
        /// The hashes to add to the global list of trusted hashes.  This value may
        /// be null.
        /// </param>
        /// <param name="clear">
        /// Non-zero to clear the global list of trusted hashes before adding the
        /// specified hashes.
        /// </param>
        /// <param name="error">
        /// Upon failure, receives an error message that describes why the hashes
        /// could not be added.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise, an error code.
        /// </returns>
        public static ReturnCode AddTrustedHashes(
            IEnumerable<string> hashes, /* in: OPTIONAL */
            bool clear,                 /* in */
            ref Result error            /* out */
            )
        {
            bool locked = false;

            try
            {
                HardTryLock(ref locked); /* TRANSACTIONAL */

                if (locked)
                {
                    if (hashes != null)
                    {
                        if (trustedHashes == null)
                            trustedHashes = new StringList();

                        if (clear)
                            trustedHashes.Clear();

                        trustedHashes.AddRange(hashes);
                    }
                    else if (clear)
                    {
                        trustedHashes.Clear();
                        trustedHashes = null;
                    }

                    return ReturnCode.Ok;
                }
                else
                {
                    error = "unable to acquire static lock";

                    TraceOps.LockTrace(
                        "AddTrustedHashes",
                        typeof(GlobalState).Name, true,
                        TracePriority.LockError,
                        MaybeWhoHasLock());

                    return ReturnCode.Error;
                }
            }
            finally
            {
                ExitLock(ref locked); /* TRANSACTIONAL */
            }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Embedding Support Methods
        /// <summary>
        /// This method records that the specified directory was considered using
        /// the specified detection flags, adding it to the supplied dictionary of
        /// tracked paths.
        /// </summary>
        /// <param name="detectFlags">
        /// The detection flags that were in effect when the directory was
        /// considered; these are used to form the dictionary key.
        /// </param>
        /// <param name="directory">
        /// The directory that was considered.
        /// </param>
        /// <param name="paths">
        /// Upon return, receives the tracked directory.  This dictionary may be
        /// created if it is initially null.
        /// </param>
        private static void TrackPackageDirectory(
            DetectFlags detectFlags,       /* in */
            string directory,              /* in */
            ref StringListDictionary paths /* in, out */
            )
        {
            if (paths == null)
                paths = new StringListDictionary();

            string key = detectFlags.ToString();
            StringList value;

            if (paths.TryGetValue(key, out value))
            {
                if (value != null)
                    value.Add(directory);
                else
                    paths[key] = new StringList(directory);
            }
            else
            {
                paths.Add(key, new StringList(directory));
            }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method checks whether the specified directory contains a script
        /// library for the specified package, tracking the directory that was
        /// considered.
        /// </summary>
        /// <param name="packageName">
        /// The name of the package to check for, if any.
        /// </param>
        /// <param name="packageVersion">
        /// The version of the package to check for, if any.
        /// </param>
        /// <param name="fileNameOnly">
        /// The file name, without any directory information, that must exist within
        /// the candidate directory, if any.
        /// </param>
        /// <param name="detectFlags">
        /// The detection flags that are in effect; these are used when tracking the
        /// directory that was considered.
        /// </param>
        /// <param name="path">
        /// Upon entry, the candidate directory to check.  Upon success, receives
        /// the resolved package directory.
        /// </param>
        /// <param name="paths">
        /// Upon return, receives the directory that was considered.  This
        /// dictionary may be created if it is initially null.
        /// </param>
        /// <returns>
        /// True if a suitable package directory was found; otherwise, false.
        /// </returns>
        private static bool CheckPackageDirectory(
            string packageName,            /* in: OPTIONAL */
            Version packageVersion,        /* in: OPTIONAL */
            string fileNameOnly,           /* in: OPTIONAL */
            DetectFlags detectFlags,       /* in */
            ref string path,               /* in, out */
            ref StringListDictionary paths /* in, out */
            )
        {
            string directory = path;

            TrackPackageDirectory(
                detectFlags, directory, ref paths);

            if (String.IsNullOrEmpty(directory))
                return false;

            if (!Directory.Exists(directory))
                return false;

            if (!PathOps.IsEqualFileName(Path.GetFileName(
                    directory), TclVars.Path.Lib))
            {
                directory = PathOps.CombinePath(
                    null, directory, TclVars.Path.Lib);

                if (String.IsNullOrEmpty(directory))
                    return false;

                if (!Directory.Exists(directory))
                    return false;
            }

            if (!String.IsNullOrEmpty(packageName) &&
                (packageVersion != null))
            {
                directory = PathOps.CombinePath(
                    null, directory, FormatOps.PackageDirectory(
                    packageName, packageVersion, false));

                if (String.IsNullOrEmpty(directory))
                    return false;

                if (!Directory.Exists(directory))
                    return false;
            }

            if (!String.IsNullOrEmpty(fileNameOnly))
            {
                if (PathOps.HasDirectory(fileNameOnly))
                    return false;

                string fileName = PathOps.CombinePath(
                    null, directory, fileNameOnly);

                if (!File.Exists(fileName))
                    return false;
            }

            path = directory;
            return true;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method attempts to detect a package script library directory using
        /// the indicated starting point, tracking each directory that is
        /// considered.
        /// </summary>
        /// <param name="assembly">
        /// The assembly whose location is used as the starting point for the
        /// search.
        /// </param>
        /// <param name="clientData">
        /// The client data for this operation.  This parameter is not used.
        /// </param>
        /// <param name="packageName">
        /// The name of the package to check for, if any.
        /// </param>
        /// <param name="packageVersion">
        /// The version of the package to check for, if any.
        /// </param>
        /// <param name="fileNameOnly">
        /// The file name, without any directory information, that must exist within
        /// the candidate directory, if any.
        /// </param>
        /// <param name="path">
        /// Upon success, receives the detected package directory.
        /// </param>
        /// <param name="paths">
        /// Upon return, receives the directories that were considered.  This
        /// dictionary may be created if it is initially null.
        /// </param>
        /// <returns>
        /// True if a suitable package directory was found; otherwise, false.
        /// </returns>
        private static bool DetectPackageFileViaAssembly(
            Assembly assembly,             /* in */
            IClientData clientData,        /* in: NOT USED */
            string packageName,            /* in: OPTIONAL */
            Version packageVersion,        /* in: OPTIONAL */
            string fileNameOnly,           /* in: OPTIONAL */
            ref string path,               /* out */
            ref StringListDictionary paths /* in, out */
            )
        {
            string assemblyPath = GetPackagePath(
                assembly, null, null, PathFlags.Root);

            if (CheckPackageDirectory(
                    packageName, packageVersion, fileNameOnly,
                    DetectFlags.Assembly, ref assemblyPath,
                    ref paths))
            {
                path = assemblyPath;
                return true;
            }
            else
            {
                return false;
            }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method attempts to detect a package script library directory using
        /// the indicated starting point, tracking each directory that is
        /// considered.
        /// </summary>
        /// <param name="variable">
        /// The name of the environment variable whose value is used as the starting
        /// point for the search.
        /// </param>
        /// <param name="clientData">
        /// The client data for this operation.  This parameter is not used.
        /// </param>
        /// <param name="packageName">
        /// The name of the package to check for, if any.
        /// </param>
        /// <param name="packageVersion">
        /// The version of the package to check for, if any.
        /// </param>
        /// <param name="fileNameOnly">
        /// The file name, without any directory information, that must exist within
        /// the candidate directory, if any.
        /// </param>
        /// <param name="path">
        /// Upon success, receives the detected package directory.
        /// </param>
        /// <param name="paths">
        /// Upon return, receives the directories that were considered.  This
        /// dictionary may be created if it is initially null.
        /// </param>
        /// <returns>
        /// True if a suitable package directory was found; otherwise, false.
        /// </returns>
        private static bool DetectPackageFileViaEnvironment(
            string variable,               /* in */
            IClientData clientData,        /* in: NOT USED */
            string packageName,            /* in: OPTIONAL */
            Version packageVersion,        /* in: OPTIONAL */
            string fileNameOnly,           /* in: OPTIONAL */
            ref string path,               /* out */
            ref StringListDictionary paths /* in, out */
            )
        {
            string variablePath = GetEnvironmentVariable(variable);

            if (CheckPackageDirectory(
                    packageName, packageVersion, fileNameOnly,
                    DetectFlags.Environment, ref variablePath,
                    ref paths))
            {
                path = variablePath;
                return true;
            }
            else
            {
                return false;
            }
        }

        ///////////////////////////////////////////////////////////////////////

#if !NET_STANDARD_20
        /// <summary>
        /// This method attempts to detect a package script library directory using
        /// the indicated starting point, tracking each directory that is
        /// considered.
        /// </summary>
        /// <param name="version">
        /// The version whose setup information is used as the starting point for the
        /// search.
        /// </param>
        /// <param name="clientData">
        /// The client data for this operation.  This parameter is not used.
        /// </param>
        /// <param name="packageName">
        /// The name of the package to check for, if any.
        /// </param>
        /// <param name="packageVersion">
        /// The version of the package to check for, if any.
        /// </param>
        /// <param name="fileNameOnly">
        /// The file name, without any directory information, that must exist within
        /// the candidate directory, if any.
        /// </param>
        /// <param name="path">
        /// Upon success, receives the detected package directory.
        /// </param>
        /// <param name="paths">
        /// Upon return, receives the directories that were considered.  This
        /// dictionary may be created if it is initially null.
        /// </param>
        /// <returns>
        /// True if a suitable package directory was found; otherwise, false.
        /// </returns>
        private static bool DetectPackageFileViaSetup(
            Version version,               /* in */
            IClientData clientData,        /* in: NOT USED */
            string packageName,            /* in: OPTIONAL */
            Version packageVersion,        /* in: OPTIONAL */
            string fileNameOnly,           /* in: OPTIONAL */
            ref string path,               /* out */
            ref StringListDictionary paths /* in, out */
            )
        {
            string setupPath = SetupOps.GetPath(version);

            if (CheckPackageDirectory(
                    packageName, packageVersion, fileNameOnly,
                    DetectFlags.Setup, ref setupPath,
                    ref paths))
            {
                path = setupPath;
                return true;
            }
            else
            {
                return false;
            }
        }
#endif

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method attempts to detect and set the script library path using the
        /// location of the specified assembly.
        /// </summary>
        /// <param name="assembly">
        /// The assembly whose location is used as the starting point for the
        /// search, if any.
        /// </param>
        /// <param name="clientData">
        /// The client data for this operation, if any.
        /// </param>
        /// <param name="detectFlags">
        /// The detection flags used to control how the script library path is
        /// detected.
        /// </param>
        /// <returns>
        /// True if a suitable script library path was detected; otherwise, false.
        /// </returns>
        public static bool DetectLibraryPath( /* EXTERNAL USE ONLY */
            Assembly assembly,      /* in: OPTIONAL */
            IClientData clientData, /* in: OPTIONAL */
            DetectFlags detectFlags /* in */
            ) /* THREAD-SAFE */
        {
            return DetectLibraryPath(
                null, assembly, clientData, detectFlags);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method attempts to detect and set the script library path using the
        /// specified assembly name and assembly.
        /// </summary>
        /// <param name="assemblyName">
        /// The assembly name whose version may be used during detection, if any.
        /// </param>
        /// <param name="assembly">
        /// The assembly whose location is used as the starting point for the
        /// search, if any.
        /// </param>
        /// <param name="clientData">
        /// The client data for this operation, if any.
        /// </param>
        /// <param name="detectFlags">
        /// The detection flags used to control how the script library path is
        /// detected.
        /// </param>
        /// <returns>
        /// True if a suitable script library path was detected; otherwise, false.
        /// </returns>
        public static bool DetectLibraryPath( /* EXTERNAL USE ONLY */
            AssemblyName assemblyName, /* in: OPTIONAL */
            Assembly assembly,         /* in: OPTIONAL */
            IClientData clientData,    /* in: OPTIONAL */
            DetectFlags detectFlags    /* in */
            ) /* THREAD-SAFE */
        {
            TraceOps.DebugTrace(String.Format(
                "DetectLibraryPath: entered, " +
                "assemblyName = {0}, assembly = {1}, " +
                "clientData = {2}, detectFlags = {3}",
                FormatOps.AssemblyName(assemblyName, 0, false, true),
                FormatOps.AssemblyName(assembly, 0, false, true),
                FormatOps.WrapOrNull(clientData),
                FormatOps.WrapOrNull(detectFlags)),
                typeof(GlobalState).Name,
                TracePriority.StartupDebug);

            StringListDictionary paths = null;
            bool locked = false;

            try
            {
                PathMetaTryLock(ref locked); /* TRANSACTIONAL */

                if (locked)
                {
#if !NET_STANDARD_20
                    //
                    // NOTE: Attempt to obtain the versions of the assembly
                    //       and the assembly name specified by the caller.
                    //       If these values cannot be obtained, they will
                    //       not be used.
                    //
                    Version assemblyNameVersion = AssemblyOps.GetVersion(
                        assemblyName); /* "1.0.2222.33333" */

                    Version assemblyVersion = AssemblyOps.GetVersion(
                        assembly); /* "1.0.4444.55555" */
#endif

                    ///////////////////////////////////////////////////////////

                    //
                    // HACK: This section is the only portion of this method
                    //       that is "hard-coded" to deal with how the Eagle
                    //       core library package works, e.g. it will end up
                    //       causing the "lib/Eagle1.0/init.eagle" relative
                    //       library file name to be used when checking each
                    //       candidate core script library directory.
                    //
                    #region Eagle Core Library [Package] Specific Section
                    //
                    // NOTE: Fetch the configured script library package
                    //       name and version for the core library.
                    //
                    string packageName = GetPackageName(); /* "Eagle" */
                    Version packageVersion = GetPackageVersion(null); /* "1.0" */

                    //
                    // NOTE: What is the name of the file we are looking
                    //       for?
                    //
                    string fileNameOnly = PathOps.ScriptFileNameOnly(
                        FileName.Initialization); /* "init.eagle" */
                    #endregion

                    ///////////////////////////////////////////////////////////

                    //
                    // NOTE: Attempt to find a suitable library path.
                    //
                    string path = null;

                    if ((!FlagOps.HasFlags(
                            detectFlags, DetectFlags.Assembly, true) ||
                         !DetectPackageFileViaAssembly(
                            assembly, clientData, packageName,
                            packageVersion, fileNameOnly, ref path,
                            ref paths)) &&
                        (!FlagOps.HasFlags(
                            detectFlags, DetectFlags.Environment |
                            DetectFlags.BaseDirectory, true) ||
                         !DetectPackageFileViaEnvironment(
                            EnvVars.EagleBase, clientData, packageName,
                            packageVersion, fileNameOnly, ref path,
                            ref paths)) &&
                        (!FlagOps.HasFlags(
                            detectFlags, DetectFlags.Environment |
                            DetectFlags.Directory, true) ||
                         !DetectPackageFileViaEnvironment(
                            EnvVars.Eagle, clientData, packageName,
                            packageVersion, fileNameOnly, ref path,
                            ref paths)) &&
#if !NET_STANDARD_20
                        ((assemblyNameVersion == null) ||
                         !FlagOps.HasFlags(
                            detectFlags, DetectFlags.Setup |
                            DetectFlags.AssemblyNameVersion, true) ||
                         !DetectPackageFileViaSetup(
                            assemblyNameVersion, clientData, packageName,
                            packageVersion, fileNameOnly, ref path,
                            ref paths)) &&
                        ((assemblyVersion == null) ||
                         !FlagOps.HasFlags(
                            detectFlags, DetectFlags.Setup |
                            DetectFlags.AssemblyVersion, true) ||
                         !DetectPackageFileViaSetup(
                            assemblyVersion, clientData, packageName,
                            packageVersion, fileNameOnly, ref path,
                            ref paths)) &&
                        (!FlagOps.HasFlags(
                            detectFlags, DetectFlags.Setup |
                            DetectFlags.PackageVersion, true) ||
                         !DetectPackageFileViaSetup(
                            packageVersion, clientData, packageName,
                            packageVersion, fileNameOnly, ref path,
                            ref paths)) &&
                        (!FlagOps.HasFlags(
                            detectFlags, DetectFlags.Setup |
                            DetectFlags.NoVersion, true) ||
                         !DetectPackageFileViaSetup(
                            null, clientData, packageName,
                            packageVersion, fileNameOnly, ref path,
                            ref paths))
#else
                        true /* HACK: .NET Standard 2.0 stub. */
#endif
                        )
                    {
                        //
                        // NOTE: Do nothing.
                        //
                    }
                    else
                    {
                        if (FlagOps.HasFlags(
                                detectFlags, DetectFlags.DetectOnly, true))
                        {
                            clientData = ClientData.WrapOrReplace(
                                clientData, path);
                        }
                        else
                        {
                            SetLibraryPath(path, true);
                        }

                        TraceOps.DebugTrace(String.Format(
                            "DetectLibraryPath: exited (success), " +
                            "assemblyName = {0}, assembly = {1}, " +
                            "clientData = {2}, detectFlags = {3}, " +
                            "path = {4}, paths = {5}, result = {6}",
                            FormatOps.AssemblyName(assemblyName, 0, false, true),
                            FormatOps.AssemblyName(assembly, 0, false, true),
                            FormatOps.WrapOrNull(clientData),
                            FormatOps.WrapOrNull(detectFlags),
                            FormatOps.WrapOrNull(path),
                            FormatOps.WrapOrNull(paths), true),
                            typeof(GlobalState).Name,
                            TracePriority.StartupDebug);

                        return true;
                    }
                }
                else
                {
                    TraceOps.LockTrace(
                        "DetectLibraryPath",
                        typeof(GlobalState).Name, true,
                        TracePriority.LockError,
                        MaybeWhoHasLock());
                }
            }
            finally
            {
                ExitLock(ref locked); /* TRANSACTIONAL */
            }

            if (FlagOps.HasFlags(
                    detectFlags, DetectFlags.Verbose, true))
            {
                clientData = ClientData.WrapOrReplace(
                    clientData, paths);
            }

            TraceOps.DebugTrace(String.Format(
                "DetectLibraryPath: exited (failure), " +
                "assemblyName = {0}, assembly = {1}, " +
                "clientData = {2}, detectFlags = {3}, " +
                "paths = {4}, result = {5}",
                FormatOps.AssemblyName(assemblyName, 0, false, true),
                FormatOps.AssemblyName(assembly, 0, false, true),
                FormatOps.WrapOrNull(clientData),
                FormatOps.WrapOrNull(detectFlags),
                FormatOps.WrapOrNull(paths), false),
                typeof(GlobalState).Name,
                TracePriority.StartupError);

            return false;
        }
        #endregion
    }
}
