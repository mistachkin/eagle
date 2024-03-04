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
    [ObjectId("e8491fec-2fd3-455e-92fd-cf2a84c75e8a")]
    internal static class GlobalState
    {
        #region Private Read-Only Data (Logical Constants)
        private static readonly object syncRoot = new object();

        ///////////////////////////////////////////////////////////////////////

        #region Application Domain Data
        private static readonly AppDomain appDomain = AppDomain.CurrentDomain;

        ///////////////////////////////////////////////////////////////////////

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
        private static readonly int appDomainId = (appDomain != null) ?
            appDomain.Id : -1;

        ///////////////////////////////////////////////////////////////////////

        private static readonly bool isDefaultAppDomain =
            (appDomain != null) ? appDomain.IsDefaultAppDomain() : false;
#endif
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Default Package Name & Version Data
        private static readonly string DefaultPackageName = "Eagle";
        private static readonly string DefaultPackageNameNoCase = "eagle";

        ///////////////////////////////////////////////////////////////////////

        //
        // TODO: This version information should be changed if the major or
        //       minor version of the assembly changes.
        //
        private static readonly int DefaultMajorVersion = 1;
        private static readonly int DefaultMinorVersion = 0;

        ///////////////////////////////////////////////////////////////////////

        private static readonly Version DefaultVersion = GetTwoPartVersion(
            DefaultMajorVersion, DefaultMinorVersion);
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Package Name Data
        //
        // NOTE: This package contains the Eagle [core] script library.  Its
        //       primary contents are script files that are required during
        //       initialization of an interpreter (e.g. the "embed.eagle",
        //       "init.eagle", and "vendor.eagle" files, etc).
        //
        private static readonly string LibraryPackageName = DefaultPackageName;

        //
        // NOTE: This package contains the Eagle test [suite infrastructure].
        //       Its primary contents are script files that are required when
        //       running the Eagle test suite (e.g. the "constraints.eagle",
        //       "prologue.eagle", and "epilogue.eagle" files).  They are also
        //       designed to be used by third-party test suites.
        //
        private static readonly string TestPackageName = "Test";

        //
        // NOTE: This package may contain a set of built-in packages included
        //       with the Eagle [core] library, e.g. Harpy, et al.
        //
        private static readonly string KitPackageName = "Kit";
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Package Name & Version Formatting
        private static readonly string UpdateVersionFormat = "{0}.{1}";

        ///////////////////////////////////////////////////////////////////////

        private static readonly string PackageNameFormat = "{0}{1}{2}";
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Stub Assembly Data
        private const string StubAssemblyName = "Eagle.Eye";

        ///////////////////////////////////////////////////////////////////////

        private const string StubAssemblyTypeName =
            "Eagle._Components.Private.Stub";

        ///////////////////////////////////////////////////////////////////////

        private static MethodInfo StubExecuteMethodInfo = null;

        ///////////////////////////////////////////////////////////////////////

        private const string StubOkResult = "ok";
        private const string StubErrorResult = "invalid interpreter";
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Entry & Executing Assembly Data
        private static StringComparison assemblyNameComparisonType =
            StringComparison.OrdinalIgnoreCase;

        ///////////////////////////////////////////////////////////////////////

        #region Executing Assembly Data
        private static readonly Assembly thisAssembly =
            Assembly.GetExecutingAssembly();

#if CAS_POLICY
        private static readonly Evidence thisAssemblyEvidence =
            (thisAssembly != null) ? thisAssembly.Evidence : null;
#endif

        private static readonly AssemblyName thisAssemblyName =
            (thisAssembly != null) ? thisAssembly.GetName() : null;

        private static readonly string thisAssemblyTitle =
            SharedAttributeOps.GetAssemblyTitle(thisAssembly);

        private static readonly string thisAssemblyLocation =
            (thisAssembly != null) ? thisAssembly.Location : null;

        private static readonly DateTime thisAssemblyDateTime =
            (thisAssembly != null) ? SharedAttributeOps.GetAssemblyDateTime(
                thisAssembly) : DateTime.MinValue; /* MUST BE AFTER LOCATION */

        private static readonly string thisAssemblySimpleName =
            (thisAssemblyName != null) ? thisAssemblyName.Name : null;

        private static readonly string thisAssemblyFullName =
            (thisAssemblyName != null) ? thisAssemblyName.FullName : null;

        private static readonly Version thisAssemblyVersion =
            (thisAssemblyName != null) ? thisAssemblyName.Version : null;

        private static readonly CultureInfo thisAssemblyCultureInfo =
            (thisAssemblyName != null) ? thisAssemblyName.CultureInfo : null;

        private static readonly byte[] thisAssemblyPublicKeyToken =
            (thisAssemblyName != null) ? thisAssemblyName.GetPublicKeyToken() : null;

        ///////////////////////////////////////////////////////////////////////

        private static string thisAssemblyPath = null;

        private static readonly Uri thisAssemblyUri =
            SharedAttributeOps.GetAssemblyUri(thisAssembly);

        ///////////////////////////////////////////////////////////////////////

        //
        // TODO: Change this if the update URI needs to be something
        //       other than the one embedded in the assembly.  In
        //       addition, the AttributeOps.GetAssemblyUpdateBaseUri
        //       method would most likely need to be changed as well.
        //
        private static readonly Uri thisAssemblyUpdateBaseUri =
            SharedAttributeOps.GetAssemblyUpdateBaseUri(thisAssembly);

        //
        // TODO: Change this if the download URI needs to be something
        //       other than the one embedded in the assembly.  In
        //       addition, the AttributeOps.GetAssemblyDownloadBaseUri
        //       method would most likely need to be changed as well.
        //
        private static readonly Uri thisAssemblyDownloadBaseUri =
            SharedAttributeOps.GetAssemblyDownloadBaseUri(thisAssembly);

        //
        // TODO: Change this if the script URI needs to be something
        //       other than the one embedded in the assembly.  In
        //       addition, the AttributeOps.GetAssemblyScriptBaseUri
        //       method would most likely need to be changed as well.
        //
        private static readonly Uri thisAssemblyScriptBaseUri =
            SharedAttributeOps.GetAssemblyScriptBaseUri(thisAssembly);

        //
        // TODO: Change this if the auxiliary URI needs to be something
        //       other than the one embedded in the assembly.  In
        //       addition, the AttributeOps.GetAssemblyAuxiliaryBaseUri
        //       method would most likely need to be changed as well.
        //
        private static readonly Uri thisAssemblyAuxiliaryBaseUri =
            SharedAttributeOps.GetAssemblyAuxiliaryBaseUri(thisAssembly);

        //
        // TODO: Change this if the XSD schema URI changes.
        //
        private static readonly Uri thisAssemblyNamespaceUri =
            (thisAssemblyUri != null) ?
                new Uri(thisAssemblyUri, "2009/schema") : null;

        //
        // NOTE: These are the (cached) plugin flags for the core library
        //       assembly.
        //
        private static PluginFlags? thisAssemblyPluginFlags = null;

        //
        // NOTE: The number of times the assembly plugin flags callback has
        //       been invoked for this AppDomain.
        //
        private static int thisAssemblyPluginFlagsCount = 0;

        ///////////////////////////////////////////////////////////////////////

        #region Package Data
        //
        // NOTE: This is the base package name (e.g. "Eagle").
        //
        private static readonly string packageName = GetPackageName(
            PackageType.Library, false);

        ///////////////////////////////////////////////////////////////////////

        //
        // NOTE: This is the base package name (e.g. "Eagle") in lower-case.
        //
        private static readonly string packageNameNoCase =
            GetPackageName(PackageType.Library, true);

        ///////////////////////////////////////////////////////////////////////

        //
        // HACK: The package version *IS* the assembly version.
        //
        private static readonly Version packageVersion =
            thisAssemblyVersion;

        ///////////////////////////////////////////////////////////////////////

#if UNIX
        //
        // NOTE: This is the base package name (e.g. "Eagle") in lower-case
        //       for use on Unix.
        //
        private static readonly string unixPackageName = packageNameNoCase;

        //
        // HACK: The Unix package version *IS* the assembly version.
        //
        private static readonly Version unixPackageVersion =
            thisAssemblyVersion;
#endif

        ///////////////////////////////////////////////////////////////////////

#if DEBUGGER
        private static readonly string debuggerName = String.Format(
            "{0} {1}", packageName, typeof(Debugger).Name).Trim();
#endif
        #endregion
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Entry Assembly Data
        private static Assembly entryAssembly = null;
        private static AssemblyName entryAssemblyName = null;

#if DEAD_CODE
        private static string entryAssemblyTitle = null;
#endif

        private static string entryAssemblyLocation = null;
        private static Version entryAssemblyVersion = null;
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
        private static readonly string resourceBaseName =
            thisAssemblySimpleName;
        #endregion
        #endregion
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Private Read-Only Primary Thread Data (Logical Constants)
        private static long primaryThreadId;
        private static long primaryManagedThreadId;
        private static long primaryNativeThreadId;

        ///////////////////////////////////////////////////////////////////////

        private static readonly Thread primaryThread = SetupPrimaryThread();
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Private Read-Write Data
        #region Diagnostic Data
        //
        // HACK: Which thread currently holds the static lock?
        //
        private static long lockThreadId = 0;

        ///////////////////////////////////////////////////////////////////////

        //
        // HACK: This is purposely not read-only.
        //
        private static bool defaultNoComplain = !Build.Debug;

        ///////////////////////////////////////////////////////////////////////

#if POLICY_TRACE
        //
        // NOTE: When this is non-zero, policy trace diagnostics will always
        //       be written by the engine, regardless of the per-interpreter
        //       settings.
        //
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
        private static Random random = new Random(); /* EXEMPT */

        ///////////////////////////////////////////////////////////////////////

        #region Randomized Initial Integer Identifiers
#if RANDOMIZE_ID
        private static long nextId = Math.Abs((random != null) ?
            random.Next() : 0);

#if !SHARED_ID_POOL
        private static long nextTokenId = Math.Abs((random != null) ?
            random.Next() : 0);
#endif

#if !SHARED_ID_POOL
        private static long nextComplaintId = Math.Abs((random != null) ?
            random.Next() : 0);
#endif

#if !SHARED_ID_POOL
        private static long nextInterpreterId = Math.Abs((random != null) ?
            random.Next() : 0);
#endif

#if !SHARED_ID_POOL
        private static long nextScriptThreadId = Math.Abs((random != null) ?
            random.Next() : 0);
#endif

#if !SHARED_ID_POOL
        private static long nextEntryId = Math.Abs((random != null) ?
            random.Next() : 0);
#endif

#if !SHARED_ID_POOL
        private static long nextRuleSetId = Math.Abs((random != null) ?
            random.Next() : 0);
#endif
#endif
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Non-Randomized Initial Integer Identifiers
#if !RANDOMIZE_ID
        private static long nextId = 0;

#if !SHARED_ID_POOL
        private static long nextTokenId = 0;
#endif

#if !SHARED_ID_POOL
        private static long nextComplaintId = 0;
#endif

#if !SHARED_ID_POOL
        private static long nextInterpreterId = 0;
#endif

#if !SHARED_ID_POOL
        private static long nextScriptThreadId = 0;
#endif

#if !SHARED_ID_POOL
        private static long nextEntryId = 0;
#endif

#if !SHARED_ID_POOL
        private static long nextRuleSetId = 0;
#endif
#endif
        #endregion
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Threading Data
#if NATIVE && (WINDOWS || UNIX)
#if MONO || MONO_HACKS || NET_STANDARD_20
        private static int threadInitialized;
#endif

        ///////////////////////////////////////////////////////////////////////

        private static bool pinvokeThreadId;
#endif

        ///////////////////////////////////////////////////////////////////////

#if NATIVE_THREAD_ID
        //
        // WARNING: This value should not be changed while any interpreter
        //          objects are active; otherwise, the wrong context state
        //          may be used.
        //
        private static int useNativeThreadIdForContexts = 0;
#endif
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region "First" / "Active" / "All" Interpreter Tracking
        private static Interpreter firstInterpreter = null;

        ///////////////////////////////////////////////////////////////////////

        [ThreadStatic()] /* ThreadSpecificData */
        private static InterpreterStackList activeInterpreters;

        private static readonly InterpreterDictionary allInterpreters =
            new InterpreterDictionary();

        private static readonly InterpreterDictionaryCache allInterpretersCache =
            new InterpreterDictionaryCache();

        private static readonly TokenInterpreterDictionary tokenInterpreters =
            new TokenInterpreterDictionary();
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region "All" Thread Tracking
        private static readonly EngineThreadList allEngineThreads =
            new EngineThreadList();
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Path Override Data
        #region Environment Variable Path Overrides
        private static string libraryPath = null;
        private static StringList autoPathList = null;
        private static string tclLibraryPath = null;
        private static StringList tclAutoPathList = null;
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Shared Binary / Base / Library Path Overrides
        //
        // NOTE: This is no longer read-only; also, it has been renamed
        //       from "binaryPath" to "sharedBinaryPath".
        //
        private static string sharedBinaryPath = null;

        ///////////////////////////////////////////////////////////////////////

        //
        // NOTE: Shared override for the base path.
        //
        private static string sharedBasePath = null;

        ///////////////////////////////////////////////////////////////////////

        //
        // NOTE: Shared override for the library path.
        //
        private static string sharedLibraryPath = null;
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Shared Externals Path Overrides
        //
        // NOTE: Shared override for the externals path.
        //
        private static string sharedExternalsPath = null;
        #endregion
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Package Path Data
        private static string assemblyPackageNamePath = null;
        private static string assemblyPackageRootPath = null;

        ///////////////////////////////////////////////////////////////////////

        private static string rawBinaryBasePackageNamePath = null;
        private static string rawBinaryBasePackageRootPath = null;

        ///////////////////////////////////////////////////////////////////////

        private static string rawBasePackageNamePath = null;
        private static string rawBasePackageRootPath = null;

        ///////////////////////////////////////////////////////////////////////

        private static string packagePeerBinaryPath = null;
        private static string packagePeerAssemblyPath = null;
        private static string packageRootPath = null;

        ///////////////////////////////////////////////////////////////////////

        private static string packageNameBinaryPath = null;
        private static string packageNameAssemblyPath = null;
        private static string packageNameRootPath = null;

        ///////////////////////////////////////////////////////////////////////

#if UNIX
        private static string unixPackageNameLocalPath = null;
        private static string unixPackageNamePath = null;
#endif

        ///////////////////////////////////////////////////////////////////////

#if NATIVE && TCL
        private static string tclPackageNamePath = null;
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
        private static StringList sharedAutoPathList = null;
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Trusted Hashes List
        //
        // NOTE: The global list of trusted hashes.  Used for all interpreters
        //       in this application domain.  Set via the Utility class, by an
        //       external caller.
        //
        private static StringList trustedHashes = null;
        #endregion
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Threading Cooperative Locking Diagnostic Methods
        private static long MaybeWhoHasLock()
        {
            return Interlocked.CompareExchange(ref lockThreadId, 0, 0);
        }

        ///////////////////////////////////////////////////////////////////////

        private static void MaybeSomebodyHasLock(
            bool locked /* in */
            )
        {
            if (locked)
            {
                /* IGNORED */
                Interlocked.CompareExchange(
                    ref lockThreadId, GetCurrentThreadId(), 0);
            }
        }

        ///////////////////////////////////////////////////////////////////////

        private static void MaybeNobodyHasLock(
            bool locked /* in */
            )
        {
            if (locked)
            {
                /* IGNORED */
                Interlocked.CompareExchange(
                    ref lockThreadId, 0, GetCurrentThreadId());
            }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Threading Cooperative Locking Methods
        #region Dead Code
#if DEAD_CODE
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

        #region Special Timeout Locking Methods
        private static void SoftTryLock(
            ref bool locked
            )
        {
            TryLock(ThreadOps.GetTimeout(
                null, null, TimeoutType.SoftLock), ref locked);
        }

        ///////////////////////////////////////////////////////////////////////

        private static void FirmTryLock(
            ref bool locked
            )
        {
            TryLock(ThreadOps.GetTimeout(
                null, null, TimeoutType.FirmLock), ref locked);
        }

        ///////////////////////////////////////////////////////////////////////

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
        public static ReturnCode TryLockForHealth(
            ref bool locked,
            ref ResultList errors
            )
        {
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
        private static void PathMetaTryLock(
            ref bool locked
            )
        {
            SoftTryLock(ref locked);
        }

        ///////////////////////////////////////////////////////////////////////

        private static void PathSoftTryLock(
            ref bool locked
            )
        {
            SoftTryLock(ref locked);
        }

        ///////////////////////////////////////////////////////////////////////

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
            if ((id < 0) || (id > uint.MaxValue))
            {
                //
                // HACK: This method may not be able to use the DebugOps
                //       methods because they may call into us (e.g. the
                //       Complain method).
                //
                if (!noComplain)
                {
                    DebugOps.Complain(ReturnCode.Error, String.Format(
                        "original identifier is negative or greater than {0}",
                        uint.MaxValue));
                }

                return id;
            }

            return ConversionOps.MakeLong(
                appDomainId /* NO-LOCK, READ-ONLY */, id);
        }
#endif

        ///////////////////////////////////////////////////////////////////////

        public static long NextId() /* THREAD-SAFE */
        {
            return NextId(defaultNoComplain);
        }

        ///////////////////////////////////////////////////////////////////////

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

        public static long NextId(
            Interpreter interpreter
            ) /* THREAD-SAFE */
        {
            return NextId(interpreter, defaultNoComplain);
        }

        ///////////////////////////////////////////////////////////////////////

        private static long NextId(
            Interpreter interpreter,
            bool noComplain
            ) /* THREAD-SAFE */
        {
            return (interpreter != null) ?
                interpreter.NextId() : NextId(noComplain);
        }

        ///////////////////////////////////////////////////////////////////////

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
        public static long NextComplaintId() /* THREAD-SAFE */
        {
            return NextComplaintId(true); // HACK: Hard-coded, do not change.
        }

        ///////////////////////////////////////////////////////////////////////

        //
        // WARNING: This is used by the DebugOps.Complain subsystem.
        //
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

        public static long NextInterpreterId() /* THREAD-SAFE */
        {
            return NextInterpreterId(defaultNoComplain);
        }

        ///////////////////////////////////////////////////////////////////////

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

        public static long NextScriptThreadId() /* THREAD-SAFE */
        {
            return NextScriptThreadId(defaultNoComplain);
        }

        ///////////////////////////////////////////////////////////////////////

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

        public static long NextEntryId() /* THREAD-SAFE */
        {
            return NextEntryId(defaultNoComplain);
        }

        ///////////////////////////////////////////////////////////////////////

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

        public static long NextEventId(
            Interpreter interpreter
            ) /* THREAD-SAFE */
        {
            return NextEventId(interpreter, defaultNoComplain);
        }

        ///////////////////////////////////////////////////////////////////////

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

        public static long NextRuleSetId() /* THREAD-SAFE */
        {
            return NextRuleSetId(defaultNoComplain);
        }

        ///////////////////////////////////////////////////////////////////////

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

        public static long NextTypeId(
            Interpreter interpreter
            ) /* THREAD-SAFE */
        {
            return NextTypeId(interpreter, defaultNoComplain);
        }

        ///////////////////////////////////////////////////////////////////////

        private static long NextTypeId(
            Interpreter interpreter,
            bool noComplain
            ) /* THREAD-SAFE */
        {
            //
            // NOTE: Type names must be totally unique within the application
            //       domain; therefore, this must be global.
            //
            return NextId(noComplain);
        }

        ///////////////////////////////////////////////////////////////////////

        #region Dead Code
#if DEAD_CODE
        public static long NextThreadId(
            Interpreter interpreter
            ) /* THREAD-SAFE */
        {
            return NextThreadId(interpreter, defaultNoComplain);
        }
#endif
        #endregion

        ///////////////////////////////////////////////////////////////////////

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
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Threading Global Variable Access Methods
        public static long GetCurrentThreadId() /* THREAD-SAFE */
        {
#if NATIVE_THREAD_ID
            return GetCurrentNativeThreadId();
#else
            return GetCurrentManagedThreadId();
#endif
        }

        ///////////////////////////////////////////////////////////////////////

        public static long GetCurrentSystemThreadId() /* THREAD-SAFE */
        {
#if NATIVE_THREAD_ID
            return GetCurrentNativeThreadId();
#else
            return GetCurrentManagedThreadId();
#endif
        }

        ///////////////////////////////////////////////////////////////////////

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

        public static long GetCurrentManagedThreadId() /* THREAD-SAFE */
        {
            Thread thread = Thread.CurrentThread;

            if (thread == null)
                return 0;

            return thread.ManagedThreadId;
        }

        ///////////////////////////////////////////////////////////////////////

#if NATIVE && (WINDOWS || UNIX)
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

            TraceOps.DebugTrace(threadId, String.Format(
                "SetupPrimaryThread: pinvokeThreadId feature {0}.",
                pinvokeThreadId ? "enabled" : "disabled"),
                typeof(GlobalState).Name, TracePriority.StartupDebug);
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

            SetupPrimaryThreadIds(true); /* LEGACY */

            return thread;
        }

        ///////////////////////////////////////////////////////////////////////

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

        private static Thread GetPrimaryThread() /* THREAD-SAFE */
        {
            return primaryThread; /* READ-ONLY */
        }

        ///////////////////////////////////////////////////////////////////////

        public static long GetPrimaryThreadId() /* THREAD-SAFE */
        {
            return Interlocked.CompareExchange(
                ref primaryThreadId, 0, 0);
        }

        ///////////////////////////////////////////////////////////////////////

        public static long GetPrimaryManagedThreadId() /* THREAD-SAFE */
        {
            return Interlocked.CompareExchange(
                ref primaryManagedThreadId, 0, 0);
        }

        ///////////////////////////////////////////////////////////////////////

        public static long GetPrimaryNativeThreadId() /* THREAD-SAFE */
        {
            return Interlocked.CompareExchange(
                ref primaryNativeThreadId, 0, 0);
        }

        ///////////////////////////////////////////////////////////////////////

        public static bool IsPrimaryThread() /* THREAD-SAFE */
        {
            return IsPrimaryThread(GetCurrentThreadId());
        }

        ///////////////////////////////////////////////////////////////////////

        public static bool IsPrimaryManagedThread() /* THREAD-SAFE */
        {
            return IsPrimaryManagedThread(GetCurrentManagedThreadId());
        }

        ///////////////////////////////////////////////////////////////////////

        public static bool IsPrimaryNativeThread() /* THREAD-SAFE */
        {
            return IsPrimaryNativeThread(GetCurrentNativeThreadId());
        }

        ///////////////////////////////////////////////////////////////////////

        private static bool IsPrimaryThread(
            long threadId
            ) /* THREAD-SAFE */
        {
            return (threadId == GetPrimaryThreadId());
        }

        ///////////////////////////////////////////////////////////////////////

        private static bool IsPrimaryManagedThread(
            long threadId
            ) /* THREAD-SAFE */
        {
            return (threadId == GetPrimaryManagedThreadId());
        }

        ///////////////////////////////////////////////////////////////////////

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

        public static Interpreter GetFirstInterpreter() /* THREAD-SAFE */
        {
            return GetFirstInterpreter(null);
        }

        ///////////////////////////////////////////////////////////////////////

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
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region "First" / "All" Interpreter Tracking Methods
        //
        // WARNING: This method is used during interpreter creation.
        //
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
        public static bool IsActiveInterpreter(
            Interpreter interpreter
            ) /* THREAD-SAFE */
        {
            if (activeInterpreters == null)
                return false;

            return activeInterpreters.ContainsInterpreter(interpreter);
        }

        ///////////////////////////////////////////////////////////////////////

        /* THREAD-SAFE */
        public static Interpreter GetActiveInterpreterOnly()
        {
            ActiveInterpreterPair anyPair = GetActiveInterpreter();
            return (anyPair != null) ? anyPair.X : null;
        }

        ///////////////////////////////////////////////////////////////////////

        /* THREAD-SAFE */
        private static ActiveInterpreterPair GetActiveInterpreter()
        {
            return GetActiveInterpreter(null);
        }

        ///////////////////////////////////////////////////////////////////////

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

        /* THREAD-SAFE */
        private static InterpreterStackList GetActiveInterpreters()
        {
            return (activeInterpreters != null) ?
                activeInterpreters.DeepCopy() : null;
        }

        ///////////////////////////////////////////////////////////////////////

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

        public static void PushActiveInterpreter(
            Interpreter interpreter
            ) /* THREAD-SAFE */
        {
            PushActiveInterpreter(interpreter, null);
        }

        ///////////////////////////////////////////////////////////////////////

        public static void PushActiveInterpreter(
            Interpreter interpreter,
            IClientData clientData
            ) /* THREAD-SAFE */
        {
            int pushed = 0;

            PushActiveInterpreter(interpreter, clientData, ref pushed);
        }

        ///////////////////////////////////////////////////////////////////////

        private static void PushActiveInterpreter(
            Interpreter interpreter,
            IClientData clientData,
            ref int pushed
            ) /* THREAD-SAFE */
        {
            if (activeInterpreters == null)
                activeInterpreters = new InterpreterStackList();

            activeInterpreters.Push(new AnyPair<Interpreter, IClientData>(
                interpreter, clientData));

            /* IGNORED */
            Interlocked.Increment(ref pushed);

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

        /* THREAD-SAFE */
        public static ActiveInterpreterPair PeekActiveInterpreter()
        {
            int pushed = 1; // required, or no peek.

            return MaybePeekActiveInterpreter(null, ref pushed);
        }

        ///////////////////////////////////////////////////////////////////////

        public static ActiveInterpreterPair MaybePeekActiveInterpreter(
            Interpreter interpreter
            ) /* THREAD-SAFE */
        {
            int pushed = 1; // required, or no peek.

            return MaybePopActiveInterpreter(interpreter, ref pushed);
        }

        ///////////////////////////////////////////////////////////////////////

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

        /* THREAD-SAFE */
        public static ActiveInterpreterPair PopActiveInterpreter()
        {
            int pushed = 1; // required, or no pop.

            return MaybePopActiveInterpreter(null, ref pushed);
        }

        ///////////////////////////////////////////////////////////////////////

        public static ActiveInterpreterPair MaybePopActiveInterpreter(
            Interpreter interpreter
            ) /* THREAD-SAFE */
        {
            int pushed = 1; // required, or no pop.

            return MaybePopActiveInterpreter(interpreter, ref pushed);
        }

        ///////////////////////////////////////////////////////////////////////

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

        public static Version GetTwoPartVersion( /* MAY RETURN NULL */
            Version version
            )
        {
            if (version == null)
                return null;

            return GetTwoPartVersion(version.Major, version.Minor);
        }

        ///////////////////////////////////////////////////////////////////////

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
        //
        // NOTE: This method assumes the global lock is held.
        //
        /* ASYNCHRONOUS */
        private static bool ShouldTryGrabAssemblyPluginFlags(
            PluginDataTriplet anyTriplet /* in */
            )
        {
            return (anyTriplet != null) && !anyTriplet.Z &&
                (thisAssemblyPluginFlags != null);
        }

        ///////////////////////////////////////////////////////////////////////

        //
        // NOTE: This method assumes the global lock is held.
        //
        /* SYNCHRONOUS / ASYNCHRONOUS */
        private static bool TryGrabAssemblyPluginFlags(
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

        //
        // NOTE: This method assumes the global lock is held.
        //
        /* ASYNCHRONOUS */
        private static void RefreshAssemblyPluginFlags(
            StringList hashes /* in: OPTIONAL */
            )
        {
            thisAssemblyPluginFlags = RuntimeOps.GetAssemblyPluginFlags(
                null, hashes, thisAssembly);

            TraceOps.DebugTrace(String.Format(
                "RefreshAssemblyPluginFlags: hashes = {0}, pluginFlags = {1}",
                FormatOps.WrapOrNull(hashes), FormatOps.WrapOrNull(
                thisAssemblyPluginFlags)), typeof(GlobalState).Name,
                TracePriority.StartupDebug2);
        }

        ///////////////////////////////////////////////////////////////////////

        //
        // NOTE: This method assumes the global lock is held.
        //
        /* ASYNCHRONOUS */
        private static bool RefreshAndTryGrabAssemblyPluginFlags(
            StringList hashes,          /* in: OPTIONAL */
            out PluginFlags pluginFlags /* out */
            )
        {
            RefreshAssemblyPluginFlags(hashes);

            return TryGrabAssemblyPluginFlags(out pluginFlags);
        }

        ///////////////////////////////////////////////////////////////////////

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
                bool locked = false;

                try
                {
                    HardTryLock(ref locked); /* TRANSACTIONAL */

                    if (locked)
                    {
                        PluginFlags pluginFlags;

                        if (!ShouldTryGrabAssemblyPluginFlags(anyTriplet) ||
                            !TryGrabAssemblyPluginFlags(out pluginFlags))
                        {
                            /* IGNORED */
                            RefreshAndTryGrabAssemblyPluginFlags(
                                anyTriplet.X, out pluginFlags);
                        }

                        if (anyTriplet != null)
                        {
                            IPluginData pluginData = anyTriplet.Y;

                            if (pluginData != null)
                                pluginData.Flags |= pluginFlags;
                        }
                    }
                    else
                    {
                        TraceOps.LockTrace(
                            "AssemblyPluginFlagsCallback",
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

                        if (TryGrabAssemblyPluginFlags(out pluginFlags))
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
                        interpreter, false),
                    pluginData, refresh),
                ThreadOps.GetQueueFlags(false));
            #endregion
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Environment Variable Wrapper Methods
        private static string GetEnvironmentVariable(
            string variable
            ) /* THREAD-SAFE */
        {
            return CommonOps.Environment.GetVariable(variable);
        }

        ///////////////////////////////////////////////////////////////////////

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
        public static AppDomain GetAppDomain() /* THREAD-SAFE */
        {
            return appDomain;
        }

        ///////////////////////////////////////////////////////////////////////

        public static string GetAppDomainBaseDirectory() /* THREAD-SAFE */
        {
            return appDomainBaseDirectory;
        }

        ///////////////////////////////////////////////////////////////////////

#if APPDOMAINS || ISOLATED_INTERPRETERS || ISOLATED_PLUGINS
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
        private static string GetStubAssemblyPath()
        {
            string path = CommonOps.Environment.GetVariable(
                EnvVars.StubPath);

            if (path != null)
                return path;

            return AlwaysGetAssemblyPath();
        }

        ///////////////////////////////////////////////////////////////////////

        private static string GetStubAssemblyFileNameOnly()
        {
            return String.Format("{0}{1}",
                StubAssemblyName, FileExtension.Library);
        }

        ///////////////////////////////////////////////////////////////////////

        private static string GetStubAssemblyFileName()
        {
            return Path.Combine(
                GetStubAssemblyPath(), GetStubAssemblyFileNameOnly());
        }

        ///////////////////////////////////////////////////////////////////////

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
                        SharedStringOps.SystemEquals(localResult, StubOkResult))
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
                    ObjectOps.DisposeOrComplain<IExecute>(
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

        public static ReturnCode TryToLoadStubAssembly(
            IClientData clientData, /* in: NOT USED */
            bool useDefault,        /* in */
            ref Result error        /* out */
            )
        {
            string fileName = GetStubAssemblyFileName();
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
                ObjectOps.DisposeOrComplain<IExecute>(
                    null, ref stub);

                stub = null;
            }
#endif
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Entry Assembly Variable Access Methods
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

        public static bool IsEntryAssembly(
            Assembly assembly
            ) /* THREAD-SAFE */
        {
            return Object.ReferenceEquals(assembly, entryAssembly);
        }

        ///////////////////////////////////////////////////////////////////////

        public static Assembly GetEntryAssembly() /* THREAD-SAFE */
        {
            return entryAssembly;
        }

        ///////////////////////////////////////////////////////////////////////

        public static AssemblyName GetEntryAssemblyName() /* THREAD-SAFE */
        {
            return entryAssemblyName;
        }

        ///////////////////////////////////////////////////////////////////////

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
        private static string GetEntryAssemblyTitle() /* THREAD-SAFE */
        {
            return entryAssemblyTitle;
        }
#endif
        #endregion

        ///////////////////////////////////////////////////////////////////////

        public static string GetEntryAssemblyLocation() /* THREAD-SAFE */
        {
            return entryAssemblyLocation;
        }

        ///////////////////////////////////////////////////////////////////////

        public static Version GetEntryAssemblyVersion() /* THREAD-SAFE */
        {
            return entryAssemblyVersion;
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Executing Assembly Variable Access Methods
        public static bool IsAssembly(
            Assembly assembly
            ) /* THREAD-SAFE */
        {
            return Object.ReferenceEquals(assembly, thisAssembly);
        }

        ///////////////////////////////////////////////////////////////////////

        public static Assembly GetAssembly() /* THREAD-SAFE */
        {
            return thisAssembly;
        }

        ///////////////////////////////////////////////////////////////////////

#if CAS_POLICY
        public static Evidence GetAssemblyEvidence() /* THREAD-SAFE */
        {
            return thisAssemblyEvidence;
        }
#endif

        ///////////////////////////////////////////////////////////////////////

        public static AssemblyName GetAssemblyName() /* THREAD-SAFE */
        {
            return thisAssemblyName;
        }

        ///////////////////////////////////////////////////////////////////////

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

        public static string GetAssemblyTitle() /* THREAD-SAFE */
        {
            return thisAssemblyTitle;
        }

        ///////////////////////////////////////////////////////////////////////

        public static string GetAssemblyLocation() /* THREAD-SAFE */
        {
            return thisAssemblyLocation;
        }

        ///////////////////////////////////////////////////////////////////////

        public static bool IsAssemblyLocation( /* THREAD-SAFE */
            string location
            )
        {
            return SharedStringOps.Equals(
                location, thisAssemblyLocation, PathOps.ComparisonType);
        }

        ///////////////////////////////////////////////////////////////////////

        public static DateTime GetAssemblyDateTime() /* THREAD-SAFE */
        {
            return thisAssemblyDateTime;
        }

        ///////////////////////////////////////////////////////////////////////

        public static string GetAssemblySimpleName() /* THREAD-SAFE */
        {
            return thisAssemblySimpleName;
        }

        ///////////////////////////////////////////////////////////////////////

        public static string GetAssemblyFullName() /* THREAD-SAFE */
        {
            return thisAssemblyFullName;
        }

        ///////////////////////////////////////////////////////////////////////

        public static Version GetAssemblyVersion() /* THREAD-SAFE */
        {
            return thisAssemblyVersion;
        }

        ///////////////////////////////////////////////////////////////////////

        public static Version GetTwoPartAssemblyVersion() /* THREAD-SAFE */
        {
            return GetTwoPartVersion(thisAssemblyVersion);
        }

        ///////////////////////////////////////////////////////////////////////

        public static string GetAssemblyVersionString() /* THREAD-SAFE */
        {
            return (thisAssemblyVersion != null) ?
                thisAssemblyVersion.ToString() : null;
        }

        ///////////////////////////////////////////////////////////////////////

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

        public static CultureInfo GetAssemblyCultureInfo() /* THREAD-SAFE */
        {
            return thisAssemblyCultureInfo;
        }

        ///////////////////////////////////////////////////////////////////////

        public static byte[] GetAssemblyPublicKeyToken() /* THREAD-SAFE */
        {
            return thisAssemblyPublicKeyToken;
        }

        ///////////////////////////////////////////////////////////////////////

        public static Uri GetAssemblyUri() /* THREAD-SAFE */
        {
            return thisAssemblyUri;
        }

        ///////////////////////////////////////////////////////////////////////

        public static Uri GetAssemblyUpdateBaseUri() /* THREAD-SAFE */
        {
            return thisAssemblyUpdateBaseUri;
        }

        ///////////////////////////////////////////////////////////////////////

        public static Uri GetAssemblyDownloadBaseUri() /* THREAD-SAFE */
        {
            return thisAssemblyDownloadBaseUri;
        }

        ///////////////////////////////////////////////////////////////////////

        public static Uri GetAssemblyScriptBaseUri() /* THREAD-SAFE */
        {
            return thisAssemblyScriptBaseUri;
        }

        ///////////////////////////////////////////////////////////////////////

        public static Uri GetAssemblyAuxiliaryBaseUri() /* THREAD-SAFE */
        {
            return thisAssemblyAuxiliaryBaseUri;
        }

        ///////////////////////////////////////////////////////////////////////

        public static Uri GetAssemblyNamespaceUri() /* THREAD-SAFE */
        {
            return thisAssemblyNamespaceUri;
        }

        ///////////////////////////////////////////////////////////////////////

        #region Dead Code
#if DEAD_CODE
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
        #endregion
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Assembly Path Variable Access Methods
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

        public static string InitializeOrGetAssemblyPath(
            bool initialize
            ) /* THREAD-SAFE */
        {
            return InitializeOrGetAssemblyPath(initialize, false);
        }

        ///////////////////////////////////////////////////////////////////////

        private static string InitializeOrGetAssemblyPath(
            bool initialize,
            bool force
            ) /* THREAD-SAFE */
        {
            return initialize ?
                InitializeAssemblyPath(force) : GetAssemblyPath();
        }

        ///////////////////////////////////////////////////////////////////////

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

        private static string AlwaysGetAssemblyPath() /* THREAD-SAFE */
        {
            return InitializeOrGetAssemblyPath(true);
        }

        ///////////////////////////////////////////////////////////////////////

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

        public static string InitializeOrGetEntryAssemblyPath(
            bool initialize
            ) /* THREAD-SAFE */
        {
            return InitializeOrGetEntryAssemblyPath(initialize, false);
        }

        ///////////////////////////////////////////////////////////////////////

        private static string InitializeOrGetEntryAssemblyPath(
            bool initialize,
            bool force
            ) /* THREAD-SAFE */
        {
            return initialize ?
                InitializeEntryAssemblyPath(force) : GetEntryAssemblyPath();
        }

        ///////////////////////////////////////////////////////////////////////

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

        private static string AlwaysGetEntryAssemblyPath() /* THREAD-SAFE */
        {
            return InitializeOrGetEntryAssemblyPath(true);
        }

        ///////////////////////////////////////////////////////////////////////

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
        public static string InitializeOrGetBinaryPath(
            bool initialize
            ) /* THREAD-SAFE */
        {
            return InitializeOrGetBinaryPath(initialize, false);
        }

        ///////////////////////////////////////////////////////////////////////

        private static string InitializeOrGetBinaryPath(
            bool initialize,
            bool force
            ) /* THREAD-SAFE */
        {
            return initialize ?
                InitializeBinaryPath(force) : GetBinaryPath();
        }

        ///////////////////////////////////////////////////////////////////////

        private static string InitializeBinaryPathNoLock(
            bool force
            ) /* THREAD-SAFE */
        {
            if (force || (sharedBinaryPath == null))
                sharedBinaryPath = PathOps.GetBinaryPath(true);

            return sharedBinaryPath;
        }

        ///////////////////////////////////////////////////////////////////////

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
        public static string GetResourceBaseName() /* THREAD-SAFE */
        {
            return resourceBaseName;
        }
        #endregion
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Package Global Variable Access Methods
#if DEBUGGER
        public static string GetDebuggerName() /* THREAD-SAFE */
        {
            return debuggerName;
        }
#endif

        ///////////////////////////////////////////////////////////////////////

        /* THREAD-SAFE */
        public static string GetPackageName() /* CANNOT RETURN NULL */
        {
            return (packageName != null) ?
                packageName : DefaultPackageName;
        }

        ///////////////////////////////////////////////////////////////////////

        /* THREAD-SAFE */
        public static string GetPackageNameNoCase() /* CANNOT RETURN NULL */
        {
            return (packageNameNoCase != null) ?
                packageNameNoCase : DefaultPackageNameNoCase;
        }

        ///////////////////////////////////////////////////////////////////////

        public static string GetPackageTypeName( /* MAY RETURN NULL */
            PackageType packageType, /* in */
            string @default          /* OPTIONAL: May be null. */
            ) /* THREAD-SAFE */
        {
            switch (packageType)
            {
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

        public static string GetPackageName( /* CANNOT RETURN NULL */
            PackageType packageType,
            bool noCase
            ) /* THREAD-SAFE */
        {
            return GetPackageName(packageType, null, null, noCase);
        }

        ///////////////////////////////////////////////////////////////////////

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

        public static Version GetPackageVersion() /* THREAD-SAFE */
        {
            //
            // NOTE: Package versions do not typically include the build
            //       and revision numbers; therefore, be sure they are
            //       omitted in our return value.
            //
            return (packageVersion != null) ?
                GetTwoPartVersion(packageVersion) :
                GetTwoPartVersion(DefaultVersion);
        }

        ///////////////////////////////////////////////////////////////////////

#if NATIVE && TCL
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
        public static string GetRawBinaryBasePath() /* THREAD-SAFE */
        {
            return GetRawBinaryBasePath(thisAssembly);
        }

        ///////////////////////////////////////////////////////////////////////

        private static string GetRawBinaryBasePath(
            string binaryPath
            ) /* THREAD-SAFE */
        {
            return GetRawBinaryBasePath(thisAssembly, binaryPath);
        }

        ///////////////////////////////////////////////////////////////////////

        private static string GetRawBinaryBasePath(
            Assembly assembly /* OPTIONAL: May be null. */
            ) /* THREAD-SAFE */
        {
            return GetRawBasePath(assembly, InitializeOrGetBinaryPath(false));
        }

        ///////////////////////////////////////////////////////////////////////

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
        public static string GetRawBasePath() /* THREAD-SAFE */
        {
            return GetRawBasePath(InitializeOrGetAssemblyPath(false));
        }

        ///////////////////////////////////////////////////////////////////////

        private static string GetRawBasePath(
            string path
            ) /* THREAD-SAFE */
        {
            return GetRawBasePath(thisAssembly, path);
        }

        ///////////////////////////////////////////////////////////////////////

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
        public static string GetBasePath() /* THREAD-SAFE */
        {
            return GetBasePath(thisAssembly);
        }

        ///////////////////////////////////////////////////////////////////////

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
        public static string GetExternalsPath() /* THREAD-SAFE */
        {
            return GetExternalsPath(thisAssembly);
        }

        ///////////////////////////////////////////////////////////////////////

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
                        TraceOps.DebugTrace(String.Format(
                            "FetchInterpreterPathsAndFlags: " +
                            "unable to acquire interpreter {0} lock",
                            FormatOps.InterpreterNoThrow(interpreter)),
                            typeof(GlobalState).Name,
                            TracePriority.LockError);
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

        public static string GetPackagePath(
            Assembly assembly,  /* OPTIONAL: May be null. */
            string name,        /* OPTIONAL: May be null. */
            Version version,    /* OPTIONAL: May be null. */
            PathFlags pathFlags
            ) /* THREAD-SAFE */
        {
            string result = GetLibraryPath(assembly, pathFlags);

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
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Unix Package Path Support Methods
#if UNIX
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
        #endregion
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Auto-Path Support Methods
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
                    autoPaths.Add(sharedAutoPathList, true);

                    //
                    // NOTE: Create a simple string list beased on the path
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
        public static StringList CopyTrustedHashes()
        {
            bool locked = false;

            try
            {
                HardTryLock(ref locked); /* TRANSACTIONAL */

                if (locked)
                {
                    if (trustedHashes != null)
                        return new StringList(trustedHashes);
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

            return null;
        }

        ///////////////////////////////////////////////////////////////////////

        public static ReturnCode CopyTrustedHashes(
            Interpreter interpreter, /* in */
            bool clear,              /* in */
            ref Result error         /* out */
            )
        {
            return AddTrustedHashes(RuntimeOps.CombineOrCopyTrustedHashes(
                interpreter, true), clear, ref error);
        }

        ///////////////////////////////////////////////////////////////////////

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
                    Version packageVersion = GetPackageVersion(); /* "1.0" */

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
