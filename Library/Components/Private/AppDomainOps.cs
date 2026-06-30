/*
 * AppDomainOps.cs --
 *
 * Copyright (c) 2007-2012 by Joe Mistachkin.  All rights reserved.
 *
 * See the file "license.terms" for information on usage and redistribution of
 * this file, and for a DISCLAIMER OF ALL WARRANTIES.
 *
 * RCS: @(#) $Id: $
 */

using System;

#if APPDOMAINS || ISOLATED_PLUGINS
using System.Collections.Generic;
#endif

#if APPDOMAINS || ISOLATED_PLUGINS || REMOTING
using System.Reflection;
#endif

#if NATIVE && WINDOWS
using System.Runtime.CompilerServices;
#endif

#if REMOTING
using System.Runtime.Remoting;
using System.Runtime.Remoting.Proxies;
#endif

#if NATIVE && WINDOWS
using System.Runtime.InteropServices;
using System.Security;

#if !NET_40
using System.Security.Permissions;
#endif
#endif

#if CAS_POLICY
using System.Security.Policy;
#endif

using System.Threading;
using Eagle._Attributes;
using Eagle._Components.Public;
using Eagle._Components.Public.Delegates;

#if NATIVE && WINDOWS
using Eagle._Constants;
#endif

#if APPDOMAINS || ISOLATED_PLUGINS
using Eagle._Containers.Public;
#endif

using Eagle._Interfaces.Public;
using SharedStringOps = Eagle._Components.Shared.StringOps;

#if !NET_STANDARD_20 && NATIVE && WINDOWS
using ICorRuntimeHost = Eagle._Components.Private.AppDomainOps.UnsafeNativeMethods.ICorRuntimeHost;
using STARTUP_FLAGS = Eagle._Components.Private.AppDomainOps.UnsafeNativeMethods.STARTUP_FLAGS;
#endif

namespace Eagle._Components.Private
{
    /// <summary>
    /// This class provides the central set of helper methods used to query,
    /// create, configure, and unload application domains, as well as to detect
    /// and reason about cross-application-domain (remoting) boundaries used by
    /// isolated interpreters and isolated plugins.
    /// </summary>
#if NATIVE && WINDOWS
#if NET_40
    [SecurityCritical()]
#else
    [SecurityPermission(SecurityAction.LinkDemand, UnmanagedCode = true)]
#endif
#endif
    [ObjectId("592a1298-b491-48ca-b66d-0a5ef3c7a1ae")]
    internal static class AppDomainOps
    {
        ///////////////////////////////////////////////////////////////////////
        // Required Native APIs used via P/Invoke
        ///////////////////////////////////////////////////////////////////////

#if !NET_STANDARD_20 && NATIVE && WINDOWS
        #region Unsafe Native Methods Class
        /// <summary>
        /// This class contains the native (P/Invoke) declarations, COM
        /// interface definitions, and related constants needed to host the CLR
        /// and obtain the default application domain on the full .NET Framework
        /// running on Windows.
        /// </summary>
        [SuppressUnmanagedCodeSecurity()]
        [ObjectId("5857d617-b3fa-4ce6-89f8-3961ad6aa50c")]
        internal static class UnsafeNativeMethods
        {
            #region COM Identifiers for CLR Hosting
            /// <summary>
            /// The string form of the COM class identifier (CLSID) for the CLR
            /// runtime host object, as defined in mscoree.h.
            /// </summary>
            internal const string CLSID_CorRuntimeHost_String =
                "cb2f6723-ab3a-11d2-9c40-00c04fa30a3e"; /* mscoree.h */

            /// <summary>
            /// The string form of the COM interface identifier (IID) for the
            /// <see cref="ICorRuntimeHost" /> interface, as defined in
            /// mscoree.h.
            /// </summary>
            internal const string IID_ICorRuntimeHost_String =
                "cb2f6722-ab3a-11d2-9c40-00c04fa30a3e"; /* mscoree.h */
            #endregion

            ///////////////////////////////////////////////////////////////////

            #region CLR Hosting Enumerations
            /// <summary>
            /// This enumeration contains the CLR startup flags, as defined in
            /// mscoree.h, used when binding to and initializing the CLR runtime
            /// host.
            /// </summary>
            [ObjectId("a1cd61e4-857d-4ee1-b3b7-216d4d5de3f6")]
            internal enum STARTUP_FLAGS /* mscoree.h */
            {
                /// <summary>
                /// No startup flags are set.
                /// </summary>
                STARTUP_NONE = 0x0,

                /// <summary>
                /// Enable concurrent (background) garbage collection.
                /// </summary>
                STARTUP_CONCURRENT_GC = 0x1,

                /// <summary>
                /// The bit mask covering the loader optimization flags.
                /// </summary>
                STARTUP_LOADER_OPTIMIZATION_MASK = 0x3 << 1,

                /// <summary>
                /// Optimize assembly loading for a single application domain.
                /// </summary>
                STARTUP_LOADER_OPTIMIZATION_SINGLE_DOMAIN = 0x1 << 1,

                /// <summary>
                /// Optimize assembly loading for multiple application domains.
                /// </summary>
                STARTUP_LOADER_OPTIMIZATION_MULTI_DOMAIN = 0x2 << 1,

                /// <summary>
                /// Optimize assembly loading for multiple application domains
                /// that share host (GAC) assemblies.
                /// </summary>
                STARTUP_LOADER_OPTIMIZATION_MULTI_DOMAIN_HOST = 0x3 << 1,

                /// <summary>
                /// Run the loader in safe mode.
                /// </summary>
                STARTUP_LOADER_SAFEMODE = 0x10,

                /// <summary>
                /// Honor the loader version preference.
                /// </summary>
                STARTUP_LOADER_SETPREFERENCE = 0x100,

                /// <summary>
                /// Use the server (rather than workstation) garbage collector.
                /// </summary>
                STARTUP_SERVER_GC = 0x1000,

                /// <summary>
                /// Allow the garbage collector to hoard virtual memory.
                /// </summary>
                STARTUP_HOARD_GC_VM = 0x2000,

                /// <summary>
                /// Use a single-version hosting interface.
                /// </summary>
                STARTUP_SINGLE_VERSION_HOSTING_INTERFACE = 0x4000,

                /// <summary>
                /// Use legacy impersonation behavior.
                /// </summary>
                STARTUP_LEGACY_IMPERSONATION = 0x10000,

                /// <summary>
                /// Disable committing the entire thread stack at thread
                /// creation.
                /// </summary>
                STARTUP_DISABLE_COMMITTHREADSTACK = 0x20000,

                /// <summary>
                /// Always flow the impersonation context across asynchronous
                /// points.
                /// </summary>
                STARTUP_ALWAYSFLOW_IMPERSONATION = 0x40000,

                /// <summary>
                /// Trim the committed memory of the garbage collector.
                /// </summary>
                STARTUP_TRIM_GC_COMMIT = 0x80000,

                /// <summary>
                /// Enable Event Tracing for Windows (ETW).
                /// </summary>
                STARTUP_ETW = 0x100000,

                /// <summary>
                /// Indicate that the process is running on the ARM
                /// architecture.
                /// </summary>
                STARTUP_ARM = 0x400000
            }
            #endregion

            ///////////////////////////////////////////////////////////////////

            #region CLR Static Functions
            /// <summary>
            /// This method binds to a specific version of the CLR runtime and
            /// returns an interface pointer to the requested runtime host
            /// object.  It corresponds to the native CorBindToRuntimeEx
            /// function in mscoree.h.
            /// </summary>
            /// <param name="version">
            /// The version of the CLR to bind to (e.g. the three-part version
            /// prefixed with "v").
            /// </param>
            /// <param name="buildFlavor">
            /// The build flavor (e.g. workstation or server) to bind to; this
            /// is optional and may be null.
            /// </param>
            /// <param name="startupFlags">
            /// The <see cref="STARTUP_FLAGS" /> controlling how the CLR is
            /// started.
            /// </param>
            /// <param name="clsId">
            /// The COM class identifier of the runtime host object to create.
            /// </param>
            /// <param name="iId">
            /// The COM interface identifier of the interface to return.
            /// </param>
            /// <param name="pUnknown">
            /// Upon success, this receives the IUnknown interface pointer for
            /// the created runtime host object.
            /// </param>
            /// <returns>
            /// An HRESULT value indicating success or failure of the bind
            /// operation.
            /// </returns>
            [DllImport(DllName.MsCorEe,
                CallingConvention = CallingConvention.StdCall,
                CharSet = CharSet.Unicode, BestFitMapping = false,
                ThrowOnUnmappableChar = true)]
            internal static extern int CorBindToRuntimeEx( /* mscoree.h */
                string version,             /* in */
                string buildFlavor,         /* in: OPTIONAL */
                STARTUP_FLAGS startupFlags, /* in */
                ref Guid clsId,             /* in */
                ref Guid iId,               /* in */
                ref IntPtr pUnknown         /* out */
            );
            #endregion

            ///////////////////////////////////////////////////////////////////

            #region CLR Hosting Interfaces
            /// <summary>
            /// This interface is a partial managed declaration of the native
            /// ICorRuntimeHost COM interface (mscoree.h).  Only the slots
            /// needed to obtain the default application domain are declared;
            /// the other virtual table slots are represented by placeholder
            /// methods so that the layout matches the native interface.
            /// </summary>
            [ComImport]
            [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
            [Guid("cb2f6722-ab3a-11d2-9c40-00c04fa30a3e")]
            [ComConversionLoss]
            [ObjectId("b67eb5f1-e800-4f24-a4a0-afe1c74ad882")]
            internal interface ICorRuntimeHost /* mscoree.h */
            {
                /// <summary>
                /// Placeholder for an unused virtual table slot.
                /// </summary>
                void Void00();

                /// <summary>
                /// Placeholder for an unused virtual table slot.
                /// </summary>
                void Void01();

                /// <summary>
                /// Placeholder for an unused virtual table slot.
                /// </summary>
                void Void02();

                /// <summary>
                /// Placeholder for an unused virtual table slot.
                /// </summary>
                void Void03();

                /// <summary>
                /// Placeholder for an unused virtual table slot.
                /// </summary>
                void Void04();

                /// <summary>
                /// Placeholder for an unused virtual table slot.
                /// </summary>
                void Void05();

                /// <summary>
                /// Placeholder for an unused virtual table slot.
                /// </summary>
                void Void06();

                /// <summary>
                /// Placeholder for an unused virtual table slot.
                /// </summary>
                void Void07();

                /// <summary>
                /// Placeholder for an unused virtual table slot.
                /// </summary>
                void Void08();

                /// <summary>
                /// Placeholder for an unused virtual table slot.
                /// </summary>
                void Void09();

                ///////////////////////////////////////////////////////////////

                /// <summary>
                /// This method obtains the default application domain hosted by
                /// the CLR runtime host.
                /// </summary>
                /// <param name="appDomain">
                /// Upon success, this receives the default application domain.
                /// </param>
                /// <returns>
                /// An HRESULT value indicating success or failure.
                /// </returns>
                [return: MarshalAs(UnmanagedType.U4)]
                [MethodImpl(MethodImplOptions.InternalCall,
                    MethodCodeType = MethodCodeType.Runtime)]
                [PreserveSig]
                int GetDefaultDomain(out _AppDomain appDomain);

                ///////////////////////////////////////////////////////////////

                /// <summary>
                /// Placeholder for an unused virtual table slot.
                /// </summary>
                void Void10();

                /// <summary>
                /// Placeholder for an unused virtual table slot.
                /// </summary>
                void Void11();

                /// <summary>
                /// Placeholder for an unused virtual table slot.
                /// </summary>
                void Void12();

                /// <summary>
                /// Placeholder for an unused virtual table slot.
                /// </summary>
                void Void13();

                /// <summary>
                /// Placeholder for an unused virtual table slot.
                /// </summary>
                void Void14();

                /// <summary>
                /// Placeholder for an unused virtual table slot.
                /// </summary>
                void Void15();

                /// <summary>
                /// Placeholder for an unused virtual table slot.
                /// </summary>
                void Void16();

                /// <summary>
                /// Placeholder for an unused virtual table slot.
                /// </summary>
                void Void17();
            }
            #endregion
        }
        #endregion
#endif

        ///////////////////////////////////////////////////////////////////////

        #region Private Constants
        //
        // NOTE: Normally, zero would be used here; however, Mono appears
        //       to use zero for the default application domain; therefore,
        //       we must use a negative value here.
        //
        /// <summary>
        /// The sentinel value used to represent an invalid or unknown
        /// application domain identifier.
        /// </summary>
        private static readonly int InvalidId = -1;

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// The name of the application domain data slot used to flag that an
        /// application domain unload is pending.
        /// </summary>
        private const string UnloadDataName = "_EAGLE_PENDING_UNLOAD";

        ///////////////////////////////////////////////////////////////////////

#if REMOTING
        /// <summary>
        /// The name of the private field on the RealProxy type that holds the
        /// application domain identifier, queried via reflection.
        /// </summary>
        private const string domainIdFieldName = "_domainID";
#endif

        ///////////////////////////////////////////////////////////////////////

#if CAS_POLICY && NET_40
        /// <summary>
        /// The name of the IsLegacyCasPolicyEnabled property on the AppDomain
        /// type, queried via reflection.
        /// </summary>
        private const string isLegacyCasPolicyEnabledPropertyName =
            "IsLegacyCasPolicyEnabled";
#endif

        ///////////////////////////////////////////////////////////////////////

#if APPDOMAINS || ISOLATED_INTERPRETERS || ISOLATED_PLUGINS
        //
        // HACK: This is purposely not read-only.
        //
        /// <summary>
        /// The maximum number of times to retry unloading an application
        /// domain before giving up.
        /// </summary>
        private static int UnloadRetryLimit = 3; // TODO: Good default?

        ///////////////////////////////////////////////////////////////////////

        //
        // HACK: This is purposely not read-only.
        //
        /// <summary>
        /// When non-zero, an application domain that appears to be already
        /// unloaded is treated as an error instead of being tolerated.
        /// </summary>
        private static bool UnloadStrict = false; // TODO: Good default?
#endif

        ///////////////////////////////////////////////////////////////////////

#if !NET_STANDARD_20 && NATIVE && WINDOWS
        //
        // HACK: These are purposely not read-only.
        //
        /// <summary>
        /// The COM class identifier (CLSID) for the CLR runtime host object,
        /// built from <see cref="UnsafeNativeMethods.CLSID_CorRuntimeHost_String" />.
        /// </summary>
        private static Guid CLSID_CorRuntimeHost = new Guid(
            UnsafeNativeMethods.CLSID_CorRuntimeHost_String);

        /// <summary>
        /// The COM interface identifier (IID) for the
        /// <see cref="UnsafeNativeMethods.ICorRuntimeHost" /> interface, built
        /// from <see cref="UnsafeNativeMethods.IID_ICorRuntimeHost_String" />.
        /// </summary>
        private static Guid IID_ICorRuntimeHost = new Guid(
            UnsafeNativeMethods.IID_ICorRuntimeHost_String);
#endif

        ///////////////////////////////////////////////////////////////////////

        //
        // HACK: This is purposely not read-only.
        //
        /// <summary>
        /// The name of the application domain data slot used to save and
        /// restore the log file name across application domains.
        /// </summary>
        private static string SavedLogFileNameData = "SavedLogFileName";
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Private Data
#if APPDOMAINS || ISOLATED_INTERPRETERS || ISOLATED_PLUGINS
        //
        // HACK: How many application domains has this class been responsible
        //       for creating -OR- unloading?
        //
        /// <summary>
        /// The running total of application domains that this class has been
        /// responsible for creating.
        /// </summary>
        private static long createCount;

        /// <summary>
        /// The running total of application domains that this class has been
        /// responsible for unloading.
        /// </summary>
        private static long unloadCount;
#endif

        ///////////////////////////////////////////////////////////////////////

#if REMOTING
        /// <summary>
        /// The object used to synchronize access to the cached reflection
        /// members within this class.
        /// </summary>
        private static readonly object syncRoot = new object();

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// The cached reflection information for the private application domain
        /// identifier field of the RealProxy type.
        /// </summary>
        private static FieldInfo domainIdFieldInfo = null;
#endif

        ///////////////////////////////////////////////////////////////////////

#if CAS_POLICY && NET_40
        /// <summary>
        /// The cached reflection information for the IsLegacyCasPolicyEnabled
        /// property of the AppDomain type.
        /// </summary>
        private static PropertyInfo isLegacyCasPolicyEnabledPropertyInfo = null;
#endif
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region AppDomain / Remoting Support Methods
        /// <summary>
        /// This method retrieves the saved log file name from the data slot of
        /// the current application domain.
        /// </summary>
        /// <returns>
        /// The saved log file name, or null if none is set or it could not be
        /// retrieved.
        /// </returns>
        public static string GetSavedLogFileName()
        {
            AppDomain appDomain = GetCurrent();

            if (appDomain != null)
            {
                try
                {
                    return appDomain.GetData(
                        SavedLogFileNameData) as string;
                }
                catch (Exception e)
                {
                    TraceOps.DebugTrace(
                        e, typeof(AppDomainOps).Name,
                        TracePriority.RemotingError);
                }
            }

            return null;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method stores the saved log file name in the data slot of the
        /// current application domain.
        /// </summary>
        /// <param name="fileName">
        /// The log file name to save; this is optional and may be null.
        /// </param>
        /// <returns>
        /// True if the value was stored successfully; otherwise, false.
        /// </returns>
        public static bool SetSavedLogFileName(
            string fileName /* in: OPTIONAL */
            )
        {
            AppDomain appDomain = GetCurrent();

            if (appDomain != null)
            {
                try
                {
                    appDomain.SetData(
                        SavedLogFileNameData, fileName);

                    return true;
                }
                catch (Exception e)
                {
                    TraceOps.DebugTrace(
                        e, typeof(AppDomainOps).Name,
                        TracePriority.RemotingError);
                }
            }

            return false;
        }

        ///////////////////////////////////////////////////////////////////////

#if NATIVE && WINDOWS
        /// <summary>
        /// This method determines whether the default application domain can be
        /// obtained on the current platform and runtime.  This is only
        /// supported on the full .NET Framework running on Windows.
        /// </summary>
        /// <returns>
        /// True if the default application domain can be obtained; otherwise,
        /// false.
        /// </returns>
        public static bool CanGetDefault()
        {
            if (!PlatformOps.IsWindowsOperatingSystem())
                return false;

            if (CommonOps.Runtime.IsMono())
                return false;

            if (CommonOps.Runtime.IsDotNetCore())
                return false;

            return true;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method obtains the default application domain by hosting the
        /// CLR runtime via native COM interfaces.  On platforms or runtimes
        /// where this is not supported, it falls back to returning the current
        /// application domain.
        /// </summary>
        /// <returns>
        /// The default application domain, or null if it could not be
        /// obtained.
        /// </returns>
        public static object GetDefault()
        {
#if !NET_STANDARD_20
            //
            // NOTE: This method will only work correctly when running on
            //       the full .NET Framework on Windows.
            //
            if (!CanGetDefault())
                return null;

            //
            // NOTE: Use the three-part version prefixed with "v" for the
            //       version parameter to CorBindToRuntimeEx, which will
            //       correspond to running .NET Framework version.
            //
            string runtimeVersion = CommonOps.Runtime.GetNativeVersion();

            if (String.IsNullOrEmpty(runtimeVersion))
                return null;

            IntPtr pUnknown = IntPtr.Zero;

            try
            {
                int hResult; /* REUSED */

                hResult = UnsafeNativeMethods.CorBindToRuntimeEx(
                    runtimeVersion, null, STARTUP_FLAGS.STARTUP_NONE,
                    ref CLSID_CorRuntimeHost, ref IID_ICorRuntimeHost,
                    ref pUnknown);

                if (MarshalOps.ComSucceeded(hResult))
                {
                    ICorRuntimeHost runtimeHost =
                        Marshal.GetObjectForIUnknown(
                            pUnknown) as ICorRuntimeHost;

                    if (runtimeHost != null)
                    {
                        _AppDomain appDomain;

                        hResult = runtimeHost.GetDefaultDomain(
                            out appDomain);

                        if (MarshalOps.ComSucceeded(hResult))
                            return appDomain;
                    }
                }

                return null;
            }
            finally
            {
                if (pUnknown != IntPtr.Zero)
                {
                    Marshal.Release(pUnknown);
                    pUnknown = IntPtr.Zero;
                }
            }
#else
            return GetCurrent();
#endif
        }
#endif

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method determines whether it is appropriate to complain about
        /// errors in the current context.  Complaints are suppressed when the
        /// application domain or process is shutting down, or when running in a
        /// non-default application domain.
        /// </summary>
        /// <returns>
        /// True if complaining about errors is appropriate; otherwise, false.
        /// </returns>
        public static bool ShouldComplain()
        {
            //
            // NOTE: There is not much point in complaining about errors
            //       when this entire AppDomain (or process) is shutting
            //       down.
            //
            if (IsStoppingSoon())
                return false;

            //
            // TODO: *HACK* Maybe come up with a better semantic here?
            //       Assume that callers of this method prefer not to
            //       complain when running in a non-default AppDomain.
            //
            if (!IsCurrentDefault())
                return false;

            //
            // NOTE: Otherwise, feel free to complain.
            //
            return true;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method determines whether the current application domain is the
        /// primary (global) application domain.
        /// </summary>
        /// <returns>
        /// True if the current application domain is the primary one;
        /// otherwise, false.
        /// </returns>
        public static bool IsPrimary()
        {
            return IsPrimary(AppDomain.CurrentDomain);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method determines whether the specified application domain is
        /// the primary (global) application domain.
        /// </summary>
        /// <param name="appDomain">
        /// The application domain to check.
        /// </param>
        /// <returns>
        /// True if the specified application domain is the primary one;
        /// otherwise, false.
        /// </returns>
        private static bool IsPrimary(
            AppDomain appDomain
            ) /* GLOBAL */
        {
            if (appDomain == null)
                return false;

            return IsSame(appDomain, GlobalState.GetAppDomain());
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method determines whether the specified object is a transparent
        /// proxy (i.e. a proxy to an object in another application domain).
        /// </summary>
        /// <param name="proxy">
        /// The object to check.
        /// </param>
        /// <returns>
        /// True if the object is a transparent proxy; otherwise, false.
        /// </returns>
        public static bool IsTransparentProxy(
            object proxy
            )
        {
#if REMOTING
            return RemotingServices.IsTransparentProxy(proxy);
#else
            return false;
#endif
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method determines whether the object wrapped by the specified
        /// wrapper is a transparent proxy.
        /// </summary>
        /// <param name="wrapper">
        /// The wrapper whose contained object should be checked.
        /// </param>
        /// <returns>
        /// True if the wrapped object is a transparent proxy; otherwise, false.
        /// </returns>
        public static bool IsTransparentProxy(
            IWrapper wrapper
            )
        {
            if (wrapper == null)
                return false;

            return IsTransparentProxy(wrapper.Object);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method determines whether two objects have the same transparent
        /// proxy status (i.e. both are proxies or both are not).  When remoting
        /// support is not available, the supplied default value is returned.
        /// </summary>
        /// <param name="proxy1">
        /// The first object to check.
        /// </param>
        /// <param name="proxy2">
        /// The second object to check.
        /// </param>
        /// <param name="default">
        /// The value to return when remoting support is not available.
        /// </param>
        /// <returns>
        /// True if both objects have the same transparent proxy status;
        /// otherwise, false.
        /// </returns>
        public static bool MatchIsTransparentProxy(
            object proxy1,
            object proxy2,
            bool @default
            )
        {
#if REMOTING
            return RemotingServices.IsTransparentProxy(proxy1)
                == RemotingServices.IsTransparentProxy(proxy2);
#else
            return @default;
#endif
        }

        ///////////////////////////////////////////////////////////////////////

#if REMOTING
        /// <summary>
        /// This method obtains the remoting type information for the specified
        /// object, if it is a remoted (marshal-by-reference) object.
        /// </summary>
        /// <param name="value">
        /// The object whose remoting type information should be obtained.
        /// </param>
        /// <returns>
        /// The remoting type information for the object, or null if it is not
        /// available.
        /// </returns>
        private static IRemotingTypeInfo GetRemotingTypeInfo(
            object value
            )
        {
            if (value != null)
            {
                MarshalByRefObject marshalByRefObject =
                    value as MarshalByRefObject;

                if (marshalByRefObject != null)
                {
                    ObjRef objRef = RemotingServices.GetObjRefForProxy(
                        marshalByRefObject);

                    if (objRef != null)
                        return objRef.TypeInfo;
                }
            }

            return null;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method attempts to locate a type by name within the current
        /// application domain.
        /// </summary>
        /// <param name="typeName">
        /// The fully qualified name of the type to locate.
        /// </param>
        /// <returns>
        /// The located type, or null if it could not be found.
        /// </returns>
        private static Type FindType(
            string typeName
            )
        {
            return FindType(typeName, AppDomain.CurrentDomain);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method attempts to locate a type by name, searching the
        /// assemblies loaded into the specified application domain.
        /// </summary>
        /// <param name="typeName">
        /// The fully qualified name of the type to locate.
        /// </param>
        /// <param name="appDomain">
        /// The application domain whose loaded assemblies should be searched.
        /// </param>
        /// <returns>
        /// The located type, or null if it could not be found.
        /// </returns>
        private static Type FindType(
            string typeName,
            AppDomain appDomain
            )
        {
            try
            {
                if (String.IsNullOrEmpty(typeName))
                    return null;

                Type type = Type.GetType(typeName);

                if (type != null)
                    return type;

                if (appDomain == null)
                    return null;

                Assembly[] assemblies = appDomain.GetAssemblies();

                if (assemblies == null)
                    return null;

                foreach (Assembly assembly in assemblies)
                {
                    if (assembly == null)
                        continue;

                    type = assembly.GetType(typeName);

                    if (type != null)
                        return type;
                }
            }
            catch (Exception e)
            {
                TraceOps.DebugTrace(
                    e, typeof(AppDomainOps).Name,
                    TracePriority.RemotingError);
            }

            return null;
        }
#endif

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method determines whether the type of the specified remoted
        /// object is present (i.e. loadable) in the current application domain.
        /// </summary>
        /// <param name="value">
        /// The object whose type presence should be checked.
        /// </param>
        /// <returns>
        /// True if the type of the object is present in the current application
        /// domain; otherwise, false.
        /// </returns>
        public static bool IsTypePresent(
            object value
            )
        {
#if REMOTING
            IRemotingTypeInfo remotingTypeInfo = GetRemotingTypeInfo(
                value);

            if (remotingTypeInfo != null)
            {
                string typeName = MarshalOps.GetTypeNameWithoutAssembly(
                    remotingTypeInfo.TypeName);

                if (String.IsNullOrEmpty(typeName))
                    return false;

                if (FindType(typeName) != null)
                    return true;
            }
#endif

            return false;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method attempts to obtain the type name of the specified object
        /// when it is a transparent proxy.
        /// </summary>
        /// <param name="value">
        /// The object whose type name should be obtained.
        /// </param>
        /// <param name="typeName">
        /// Upon return, this contains the type name of the object when it is a
        /// transparent proxy; otherwise, null.
        /// </param>
        /// <returns>
        /// True if the object is a transparent proxy (and the type name was
        /// produced); otherwise, false.
        /// </returns>
        public static bool MaybeGetTypeName(
            object value,
            out string typeName
            )
        {
            typeName = null;

#if REMOTING
            if ((value != null) &&
                RemotingServices.IsTransparentProxy(value))
            {
                IRemotingTypeInfo remotingTypeInfo = GetRemotingTypeInfo(
                    value);

                if (remotingTypeInfo != null)
                    typeName = remotingTypeInfo.TypeName;

                return true;
            }
#endif

            return false;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method attempts to obtain the type of the specified object,
        /// complaining if an exception is thrown during the attempt.
        /// </summary>
        /// <param name="value">
        /// The object whose type should be obtained.
        /// </param>
        /// <returns>
        /// The type of the object, or null if it could not be determined.
        /// </returns>
        public static Type MaybeGetTypeOrComplain(
            object value
            )
        {
            try
            {
                return MaybeGetType(value); /* throw */
            }
            catch (Exception e)
            {
                DebugOps.Complain(ReturnCode.Error, e);
                return null;
            }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method attempts to obtain the type of the specified object,
        /// returning the object type for transparent proxies whose underlying
        /// type cannot be resolved, and null for a null value.
        /// </summary>
        /// <param name="value">
        /// The object whose type should be obtained.
        /// </param>
        /// <returns>
        /// The type of the object, or null when the object is null.
        /// </returns>
        public static Type MaybeGetType(
            object value
            )
        {
            return MaybeGetType(value, null, typeof(object));
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method attempts to obtain the type of the specified object,
        /// returning null both for a null value and for transparent proxies
        /// whose underlying type cannot be resolved.
        /// </summary>
        /// <param name="value">
        /// The object whose type should be obtained.
        /// </param>
        /// <returns>
        /// The type of the object, or null if it could not be determined.
        /// </returns>
        public static Type MaybeGetTypeOrNull(
            object value
            )
        {
            return MaybeGetType(value, null, null);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method attempts to obtain the type of the specified object,
        /// returning the object type both for a null value and for transparent
        /// proxies whose underlying type cannot be resolved.
        /// </summary>
        /// <param name="value">
        /// The object whose type should be obtained.
        /// </param>
        /// <returns>
        /// The type of the object, or the object type as a fallback.
        /// </returns>
        public static Type MaybeGetTypeOrObject(
            object value
            )
        {
            return MaybeGetType(value, typeof(object), typeof(object));
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method attempts to obtain the type of the specified object,
        /// returning the supplied default type both for a null value and for
        /// transparent proxies whose underlying type cannot be resolved.
        /// </summary>
        /// <param name="value">
        /// The object whose type should be obtained.
        /// </param>
        /// <param name="defaultType">
        /// The type to return when the object is null or its type cannot be
        /// resolved.
        /// </param>
        /// <returns>
        /// The type of the object, or the supplied default type as a fallback.
        /// </returns>
        public static Type MaybeGetType(
            object value,
            Type defaultType
            )
        {
            return MaybeGetType(value, defaultType, defaultType);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method attempts to obtain the type of the specified object.
        /// For a null value the null type is returned, and for a transparent
        /// proxy whose underlying type cannot be resolved the proxy type is
        /// returned.
        /// </summary>
        /// <param name="value">
        /// The object whose type should be obtained.
        /// </param>
        /// <param name="nullType">
        /// The type to return when the object is null.
        /// </param>
        /// <param name="proxyType">
        /// The type to return when the object is a transparent proxy whose
        /// underlying type cannot be resolved.
        /// </param>
        /// <returns>
        /// The type of the object, or one of the supplied fallback types.
        /// </returns>
        private static Type MaybeGetType(
            object value,
            Type nullType,
            Type proxyType
            )
        {
            if (value == null)
                return nullType;

#if REMOTING
            if (RemotingServices.IsTransparentProxy(value))
            {
                IRemotingTypeInfo remotingTypeInfo = GetRemotingTypeInfo(
                    value);

                if (remotingTypeInfo != null)
                {
                    string typeName = MarshalOps.GetTypeNameWithoutAssembly(
                        remotingTypeInfo.TypeName);

                    Type type = FindType(typeName);

                    if (type != null)
                        return type;
                }

                return proxyType;
            }
#endif

            return value.GetType();
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method obtains the application domain associated with the
        /// specified interpreter, locking the interpreter for the duration of
        /// the query.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter whose application domain should be obtained.
        /// </param>
        /// <returns>
        /// The application domain associated with the interpreter, or null if
        /// it could not be obtained.
        /// </returns>
        private static AppDomain GetFrom(
            Interpreter interpreter
            )
        {
            if (interpreter == null)
                return null;

            bool locked = false;

            try
            {
                interpreter.InternalHardTryLock(
                    ref locked); /* TRANSACTIONAL */

                if (locked)
                {
                    if (!interpreter.Disposed)
                        return interpreter.GetAppDomain();
                }
                else
                {
                    TraceOps.LockTrace(
                        "GetFrom",
                        typeof(AppDomainOps).Name, false,
                        TracePriority.LockError,
                        interpreter.MaybeWhoHasLock());
                }
            }
            catch
            {
                // do nothing.
            }
            finally
            {
                interpreter.InternalExitLock(
                    ref locked); /* TRANSACTIONAL */
            }

            return null;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method obtains the application domain associated with the
        /// specified plugin data.
        /// </summary>
        /// <param name="pluginData">
        /// The plugin data whose application domain should be obtained.
        /// </param>
        /// <returns>
        /// The application domain associated with the plugin data, or null if
        /// it is not available.
        /// </returns>
        private static AppDomain GetFrom(
            IPluginData pluginData
            )
        {
            if (pluginData == null)
                return null;

            return pluginData.AppDomain;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method obtains the current application domain.
        /// </summary>
        /// <returns>
        /// The current application domain.
        /// </returns>
        public static AppDomain GetCurrent()
        {
            return AppDomain.CurrentDomain;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method obtains the identifier of the current application
        /// domain.
        /// </summary>
        /// <returns>
        /// The identifier of the current application domain.
        /// </returns>
        public static int GetCurrentId()
        {
            return GetId(AppDomain.CurrentDomain);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method obtains the identifier of the primary (global)
        /// application domain.
        /// </summary>
        /// <returns>
        /// The identifier of the primary application domain.
        /// </returns>
        public static int GetPrimaryId() /* GLOBAL */
        {
            return GetId(GlobalState.GetAppDomain());
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method obtains the identifier of the current application domain
        /// as a string.
        /// </summary>
        /// <param name="display">
        /// When non-zero, a display-friendly placeholder is returned for a null
        /// or invalid identifier instead of null.
        /// </param>
        /// <returns>
        /// The string form of the current application domain identifier.
        /// </returns>
        public static string GetIdString(
            bool display
            )
        {
            return GetIdString(AppDomain.CurrentDomain, display);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method obtains the identifier of the specified application
        /// domain as a string.
        /// </summary>
        /// <param name="appDomain">
        /// The application domain whose identifier should be obtained.
        /// </param>
        /// <param name="display">
        /// When non-zero, a display-friendly placeholder is returned for a null
        /// or invalid identifier instead of null.
        /// </param>
        /// <returns>
        /// The string form of the application domain identifier.
        /// </returns>
        public static string GetIdString(
            AppDomain appDomain,
            bool display
            )
        {
            if (appDomain == null)
                return display ? FormatOps.DisplayNull : null;

            int id = appDomain.Id;

            if (id == InvalidId)
                return display ? FormatOps.DisplayInvalid : null;

            return id.ToString();
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method formats an application domain identifier string into a
        /// display form prefixed with "AppDomain:".
        /// </summary>
        /// <param name="idString">
        /// The application domain identifier string to format.
        /// </param>
        /// <returns>
        /// The formatted application domain string.
        /// </returns>
        public static string FormatAppDomain(
            string idString
            )
        {
            return String.Format("AppDomain:{0}", idString);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method formats the identifier of the specified application
        /// domain into a string, accounting for whether the application domain
        /// has been disposed and whether a display form is requested.
        /// </summary>
        /// <param name="appDomain">
        /// The application domain whose identifier should be formatted.
        /// </param>
        /// <param name="disposed">
        /// Non-zero if the application domain has been disposed.
        /// </param>
        /// <param name="display">
        /// When non-zero, a display-friendly form is produced.
        /// </param>
        /// <returns>
        /// The formatted application domain identifier string.
        /// </returns>
        public static string FormatIdString(
            AppDomain appDomain,
            bool disposed,
            bool display
            )
        {
            string idString = disposed ?
                (display ? FormatOps.DisplayDisposed : null) :
                GetIdString(appDomain, display);

            return display ? FormatAppDomain(idString) : idString;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method obtains the identifier of the application domain
        /// associated with the specified interpreter.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter whose application domain identifier should be
        /// obtained.
        /// </param>
        /// <returns>
        /// The application domain identifier, or the invalid identifier if it
        /// could not be obtained.
        /// </returns>
        public static int GetId(
            Interpreter interpreter
            )
        {
            return GetId(GetFrom(interpreter));
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method obtains the identifier of the specified application
        /// domain.
        /// </summary>
        /// <param name="appDomain">
        /// The application domain whose identifier should be obtained.
        /// </param>
        /// <returns>
        /// The application domain identifier, or the invalid identifier if the
        /// application domain is null.
        /// </returns>
        private static int GetId(
            AppDomain appDomain
            )
        {
            if (appDomain == null)
                return InvalidId;

            return appDomain.Id;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method obtains the identifier of the specified application
        /// domain, or null if the application domain is null.
        /// </summary>
        /// <param name="appDomain">
        /// The application domain whose identifier should be obtained.
        /// </param>
        /// <returns>
        /// The application domain identifier, or null if the application domain
        /// is null.
        /// </returns>
        private static int? GetIdOrNull(
            AppDomain appDomain
            )
        {
            if (appDomain == null)
                return null;

            return appDomain.Id;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method obtains the application domain identifier associated
        /// with the specified object when it is a remoting proxy, querying the
        /// private identifier field via reflection.
        /// </summary>
        /// <param name="object">
        /// The object whose application domain identifier should be obtained.
        /// </param>
        /// <returns>
        /// The application domain identifier of the proxy, or the invalid
        /// identifier when it cannot be determined.
        /// </returns>
        private static int GetId(
            object @object
            )
        {
#if REMOTING
            if (CommonOps.Runtime.IsMono())
                return InvalidId;

            try
            {
                FieldInfo fieldInfo = null;

                lock (syncRoot) /* TRANSACTIONAL */
                {
                    if (domainIdFieldInfo == null)
                    {
                        domainIdFieldInfo = typeof(RealProxy).GetField(
                            domainIdFieldName, ObjectOps.GetBindingFlags(
                            MetaBindingFlags.DomainId, true));
                    }

                    fieldInfo = domainIdFieldInfo;
                }

                if (fieldInfo != null)
                {
                    RealProxy realProxy = RemotingServices.GetRealProxy(
                        @object);

                    if (realProxy != null)
                        return (int)fieldInfo.GetValue(realProxy);
                }
            }
            catch
            {
                // do nothing.
            }
#endif

            return InvalidId;
        }

        ///////////////////////////////////////////////////////////////////////

        //
        // BUGBUG: Should this just use the IsTransparentProxy method instead?
        //
        /// <summary>
        /// This method determines whether two interpreters reside in different
        /// application domains (i.e. whether calls between them would cross an
        /// application domain boundary).
        /// </summary>
        /// <param name="interpreter1">
        /// The first interpreter to compare.
        /// </param>
        /// <param name="interpreter2">
        /// The second interpreter to compare.
        /// </param>
        /// <returns>
        /// True if the two interpreters are in different application domains;
        /// otherwise, false.
        /// </returns>
        public static bool IsCross(
            Interpreter interpreter1,
            Interpreter interpreter2
            )
        {
            AppDomain interpreterAppDomain1 = (interpreter1 != null) ?
                GetFrom(interpreter1) : null;

            AppDomain interpreterAppDomain2 = (interpreter2 != null) ?
                GetFrom(interpreter2) : null;

            if (!IsSame(interpreterAppDomain1, interpreterAppDomain2))
                return true;

            if (!IsSameId(interpreter1, interpreter2))
                return true;

            return false;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method determines whether the specified plugin resides in a
        /// different application domain than the current one (i.e. whether
        /// calls to it would cross an application domain boundary).
        /// </summary>
        /// <param name="pluginData">
        /// The plugin data to check.
        /// </param>
        /// <returns>
        /// True if the plugin is in a different application domain (or is
        /// isolated); otherwise, false.
        /// </returns>
        public static bool IsCross(
            IPluginData pluginData
            )
        {
            return IsCross(pluginData, (bool?)null);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method determines whether the specified plugin resides in a
        /// different application domain than the current one (i.e. whether
        /// calls to it would cross an application domain boundary), with an
        /// explicit result to use when an application domain is null.
        /// </summary>
        /// <param name="pluginData">
        /// The plugin data to check.
        /// </param>
        /// <param name="resultOnNull">
        /// The value to return when either application domain involved is null;
        /// when null, the comparison proceeds normally.
        /// </param>
        /// <returns>
        /// True if the plugin is in a different application domain (or is
        /// isolated); otherwise, false.
        /// </returns>
        public static bool IsCross(
            IPluginData pluginData,
            bool? resultOnNull
            )
        {
#if ISOLATED_PLUGINS
            if (IsIsolated(pluginData))
                return true;
#endif

            return IsCrossNoIsolated(pluginData, resultOnNull);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method determines whether the specified plugin resides in a
        /// different application domain than the current one, without treating
        /// an isolated plugin as automatically cross-domain.
        /// </summary>
        /// <param name="pluginData">
        /// The plugin data to check.
        /// </param>
        /// <returns>
        /// True if the plugin is in a different application domain; otherwise,
        /// false.
        /// </returns>
        public static bool IsCrossNoIsolated(
            IPluginData pluginData
            )
        {
            return IsCrossNoIsolated(pluginData, null);
        }

        ///////////////////////////////////////////////////////////////////////

        //
        // BUGBUG: Should this method just use the IsTransparentProxy
        //         method instead?
        //
        /// <summary>
        /// This method determines whether the specified plugin resides in a
        /// different application domain than the current one, without treating
        /// an isolated plugin as automatically cross-domain, with an explicit
        /// result to use when an application domain is null.
        /// </summary>
        /// <param name="pluginData">
        /// The plugin data to check.
        /// </param>
        /// <param name="resultOnNull">
        /// The value to return when either application domain involved is null;
        /// when null, the comparison proceeds normally.
        /// </param>
        /// <returns>
        /// True if the plugin is in a different application domain; otherwise,
        /// false.
        /// </returns>
        public static bool IsCrossNoIsolated(
            IPluginData pluginData,
            bool? resultOnNull
            )
        {
            AppDomain currentAppDomain = GetCurrent();

            //
            // BUGBUG: If the plugin data is null here, this method
            //         should probably not return true; however, as
            //         currently written, it will return true.
            //
            AppDomain pluginAppDomain = GetFrom(pluginData);

            if (resultOnNull != null)
            {
                if ((currentAppDomain == null) ||
                    (pluginAppDomain == null))
                {
                    return (bool)resultOnNull;
                }
            }

            if (!IsSame(pluginAppDomain, currentAppDomain))
                return true;

            if (!IsSameId(pluginData, currentAppDomain))
                return true;

            return false;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method determines whether the specified interpreter and plugin
        /// reside in different application domains (i.e. whether calls between
        /// them would cross an application domain boundary).
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter to compare.
        /// </param>
        /// <param name="pluginData">
        /// The plugin data to compare.
        /// </param>
        /// <returns>
        /// True if the interpreter and plugin are in different application
        /// domains (or the plugin is isolated); otherwise, false.
        /// </returns>
        public static bool IsCross(
            Interpreter interpreter,
            IPluginData pluginData
            )
        {
            return IsCross(interpreter, pluginData, null);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method determines whether the specified interpreter and plugin
        /// reside in different application domains, with an explicit result to
        /// use when an application domain is null.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter to compare.
        /// </param>
        /// <param name="pluginData">
        /// The plugin data to compare.
        /// </param>
        /// <param name="resultOnNull">
        /// The value to return when either application domain involved is null;
        /// when null, the comparison proceeds normally.
        /// </param>
        /// <returns>
        /// True if the interpreter and plugin are in different application
        /// domains (or the plugin is isolated); otherwise, false.
        /// </returns>
        public static bool IsCross(
            Interpreter interpreter,
            IPluginData pluginData,
            bool? resultOnNull
            )
        {
#if ISOLATED_PLUGINS
            if (IsIsolated(pluginData))
                return true;
#endif

            return IsCrossNoIsolated(interpreter, pluginData, resultOnNull);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method determines whether the specified interpreter and plugin
        /// reside in different application domains, without treating an isolated
        /// plugin as automatically cross-domain.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter to compare.
        /// </param>
        /// <param name="pluginData">
        /// The plugin data to compare.
        /// </param>
        /// <returns>
        /// True if the interpreter and plugin are in different application
        /// domains; otherwise, false.
        /// </returns>
        public static bool IsCrossNoIsolated(
            Interpreter interpreter,
            IPluginData pluginData
            )
        {
            return IsCrossNoIsolated(interpreter, pluginData, null);
        }

        ///////////////////////////////////////////////////////////////////////

        //
        // BUGBUG: Should this method just use the IsTransparentProxy
        //         method instead?
        //
        /// <summary>
        /// This method determines whether the specified interpreter and plugin
        /// reside in different application domains, without treating an isolated
        /// plugin as automatically cross-domain, with an explicit result to use
        /// when an application domain is null.  A non-orphan interpreter running
        /// in a non-default application domain is always treated as
        /// cross-domain.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter to compare.
        /// </param>
        /// <param name="pluginData">
        /// The plugin data to compare.
        /// </param>
        /// <param name="resultOnNull">
        /// The value to return when either application domain involved is null;
        /// when null, the comparison proceeds normally.
        /// </param>
        /// <returns>
        /// True if the interpreter and plugin are in different application
        /// domains; otherwise, false.
        /// </returns>
        public static bool IsCrossNoIsolated(
            Interpreter interpreter,
            IPluginData pluginData,
            bool? resultOnNull
            )
        {
            AppDomain interpreterAppDomain;

            if (interpreter != null)
            {
                //
                // NOTE: If the interpreter is not a parent interpreter
                //       and it is running in a non-default application
                //       domain, it MUST be considered as cross-domain.
                //       The parent interpreter may call [interp eval]
                //       on it, and that result could be of a type from
                //       an assembly that has not been (and cannot be)
                //       loaded into the parent interpreter application
                //       domain.
                //
                if (!interpreter.InternalIsOrphanInterpreter() &&
                    !IsCurrentDefault())
                {
                    return true;
                }

                interpreterAppDomain = GetFrom(interpreter);
            }
            else
            {
                interpreterAppDomain = null;
            }

            //
            // BUGBUG: If the plugin data is null here, this method
            //         should probably not return true; however, as
            //         currently written, it will return true, -IF-
            //         the interpreter is not null.
            //
            AppDomain pluginAppDomain = GetFrom(pluginData);

            if (resultOnNull != null)
            {
                if ((interpreterAppDomain == null) ||
                    (pluginAppDomain == null))
                {
                    return (bool)resultOnNull;
                }
            }

            if (!IsSame(interpreterAppDomain, pluginAppDomain))
                return true;

            if (!IsSameId(interpreter, pluginData))
                return true;

            return false;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method determines whether the primary (global) application
        /// domain is the default application domain.
        /// </summary>
        /// <returns>
        /// True if the primary application domain is the default one;
        /// otherwise, false.
        /// </returns>
        public static bool IsPrimaryDefault() /* GLOBAL */
        {
            return IsDefault(GlobalState.GetAppDomain());
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method determines whether the current application domain is the
        /// default application domain.
        /// </summary>
        /// <returns>
        /// True if the current application domain is the default one;
        /// otherwise, false.
        /// </returns>
        public static bool IsCurrentDefault()
        {
            return IsDefault(AppDomain.CurrentDomain);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method determines whether the specified application domain is
        /// the default application domain.
        /// </summary>
        /// <param name="appDomain">
        /// The application domain to check.
        /// </param>
        /// <returns>
        /// True if the specified application domain is the default one;
        /// otherwise, false.
        /// </returns>
        public static bool IsDefault(
            AppDomain appDomain
            )
        {
            return ((appDomain != null) && appDomain.IsDefaultAppDomain());
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method determines whether the specified application domain is
        /// the current application domain.
        /// </summary>
        /// <param name="appDomain">
        /// The application domain to check.
        /// </param>
        /// <returns>
        /// True if the specified application domain is the current one;
        /// otherwise, false.
        /// </returns>
        public static bool IsCurrent(
            AppDomain appDomain
            )
        {
            return IsSame(appDomain, AppDomain.CurrentDomain);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method determines whether the application domain associated
        /// with the specified interpreter is the current application domain.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter whose application domain should be checked.
        /// </param>
        /// <returns>
        /// True if the interpreter application domain is the current one;
        /// otherwise, false.
        /// </returns>
        public static bool IsCurrent(
            Interpreter interpreter
            )
        {
            if (interpreter == null)
                return false;

            return IsSame(
                interpreter.GetAppDomain(), AppDomain.CurrentDomain);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method determines whether two application domains are the same,
        /// comparing them by identifier.  Two null application domains are
        /// considered the same.
        /// </summary>
        /// <param name="appDomain1">
        /// The first application domain to compare.
        /// </param>
        /// <param name="appDomain2">
        /// The second application domain to compare.
        /// </param>
        /// <returns>
        /// True if the two application domains are the same; otherwise, false.
        /// </returns>
        public static bool IsSame(
            AppDomain appDomain1,
            AppDomain appDomain2
            )
        {
            if ((appDomain1 == null) && (appDomain2 == null))
                return true;
            else if ((appDomain1 == null) || (appDomain2 == null))
                return false;
            else
                return appDomain1.Id == appDomain2.Id;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method determines whether the application domain associated
        /// with the specified interpreter is the same as the current
        /// application domain.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter whose application domain should be compared.
        /// </param>
        /// <returns>
        /// True if the interpreter application domain is the same as the
        /// current one; otherwise, false.
        /// </returns>
        public static bool IsSame(
            Interpreter interpreter
            )
        {
            return IsSame(interpreter, AppDomain.CurrentDomain);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method determines whether the application domain associated
        /// with the specified interpreter is the same as the specified
        /// application domain.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter whose application domain should be compared.
        /// </param>
        /// <param name="appDomain">
        /// The application domain to compare against.
        /// </param>
        /// <returns>
        /// True if the interpreter application domain is the same as the
        /// specified one; otherwise, false.
        /// </returns>
        public static bool IsSame(
            Interpreter interpreter,
            AppDomain appDomain
            )
        {
            AppDomain localAppDomain = GetFrom(interpreter);

            if (!IsSame(localAppDomain, appDomain))
                return false;

            return true;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method determines whether two plugins reside in the same
        /// application domain, comparing them by application domain identifier.
        /// </summary>
        /// <param name="pluginData1">
        /// The first plugin data to compare.
        /// </param>
        /// <param name="pluginData2">
        /// The second plugin data to compare.
        /// </param>
        /// <returns>
        /// True if the two plugins are in the same application domain;
        /// otherwise, false.
        /// </returns>
        public static bool IsSame(
            IPluginData pluginData1,
            IPluginData pluginData2
            )
        {
            return IsSameId(pluginData1, pluginData2);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method determines whether the application domain identifier of
        /// the specified object (when it is a proxy) matches that of the
        /// specified application domain.  A non-proxy object is considered to
        /// match the current application domain.
        /// </summary>
        /// <param name="object">
        /// The object whose application domain identifier should be compared.
        /// </param>
        /// <param name="appDomain">
        /// The application domain to compare against.
        /// </param>
        /// <returns>
        /// True if the application domain identifiers match; otherwise, false.
        /// </returns>
        public static bool IsSameId(
            object @object,
            AppDomain appDomain
            )
        {
            //
            // NOTE: Grab the application domain ID for the object, if
            //       it is a proxy; otherwise, this will be "invalid",
            //       which is fine.
            //
            int id = GetId(@object);

            //
            // NOTE: If the object is NOT a proxy and the application
            //       domain is the current one, then the application
            //       domain IDs are considered to be "matching".
            //
            if ((id == InvalidId) && IsCurrent(appDomain))
                return true;

            //
            // NOTE: Otherwise, the application domain may be invalid
            //       -OR- not the current one -OR- the plugin may be
            //       a proxy.  Fallback to default handling.
            //
            return (id == GetId(appDomain));
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method determines whether two objects share the same
        /// application domain identifier (as obtained when they are proxies).
        /// </summary>
        /// <param name="object1">
        /// The first object to compare.
        /// </param>
        /// <param name="object2">
        /// The second object to compare.
        /// </param>
        /// <returns>
        /// True if the application domain identifiers of the objects match;
        /// otherwise, false.
        /// </returns>
        public static bool IsSameId(
            object object1,
            object object2
            )
        {
            //
            // NOTE: Grab the application domain IDs for the objects, if
            //       they are proxies; otherwise, they will be "invalid",
            //       which is fine.
            //
            return (GetId(object1) == GetId(object2));
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method determines whether the current application domain or the
        /// process as a whole is shutting down soon (e.g. a shutdown has
        /// started, an unload is pending, or finalization is in progress).
        /// </summary>
        /// <returns>
        /// True if the application domain or process is stopping soon;
        /// otherwise, false.
        /// </returns>
        public static bool IsStoppingSoon()
        {
#if NATIVE_PACKAGE
            if (CommonOps.Environment.DoesVariableExist(
                    EnvVars.EagleClrStopping))
            {
                return true;
            }
#endif

            if (Environment.HasShutdownStarted)
                return true;

            AppDomain appDomain = AppDomain.CurrentDomain;

            if (IsPendingUnload(appDomain))
                return true;

            if (IsFinalizing(appDomain))
                return true;

            return false;
        }

        ///////////////////////////////////////////////////////////////////////

#if APPDOMAINS || ISOLATED_INTERPRETERS || ISOLATED_PLUGINS
        /// <summary>
        /// This method obtains the running totals of application domains
        /// created and unloaded, either limited to this class instance or
        /// drawn from the process-wide reference counts.
        /// </summary>
        /// <param name="localOnly">
        /// Non-zero to report only the counts tracked by this class; zero to
        /// report the process-wide counts.
        /// </param>
        /// <param name="createCount">
        /// Upon return, this contains the total number of application domains
        /// created.
        /// </param>
        /// <param name="unloadCount">
        /// Upon return, this contains the total number of application domains
        /// unloaded.
        /// </param>
        public static void GetCounts(
            bool localOnly,
            ref long createCount,
            ref long unloadCount
            )
        {
            if (localOnly)
            {
                createCount = Interlocked.CompareExchange(
                    ref AppDomainOps.createCount, 0, 0);

                unloadCount = Interlocked.CompareExchange(
                    ref AppDomainOps.unloadCount, 0, 0);
            }
            else
            {
                Result error; /* REUSED */
                long count;

                error = null;

                if (ProcessOps.CheckAndMaybeModifyReferenceCount(
                        EnvVars.EagleLibraryAppDomainCreateCount, null,
                        null, out count, ref error) == ReturnCode.Ok)
                {
                    createCount = count;
                }
                else
                {
                    DebugOps.Complain(null, ReturnCode.Error, error);
                }

                error = null;

                if (ProcessOps.CheckAndMaybeModifyReferenceCount(
                        EnvVars.EagleLibraryAppDomainUnloadCount, null,
                        null, out count, ref error) == ReturnCode.Ok)
                {
                    unloadCount = count;
                }
                else
                {
                    DebugOps.Complain(null, ReturnCode.Error, error);
                }
            }
        }

        ///////////////////////////////////////////////////////////////////////

        //
        // WARNING: This method is for use by the test suite AppDomain
        //          leak checking code only.  Please do not use it for
        //          anything else.
        //
        /// <summary>
        /// This method obtains the process-wide lists of created and unloaded
        /// application domain identifiers.  It is intended for use by the test
        /// suite application domain leak checking code only.
        /// </summary>
        /// <param name="createList">
        /// Upon return, this contains the list of created application domain
        /// identifiers.
        /// </param>
        /// <param name="unloadList">
        /// Upon return, this contains the list of unloaded application domain
        /// identifiers.
        /// </param>
        private static void GetLists(
            ref StringList createList,
            ref StringList unloadList
            )
        {
            Result error; /* REUSED */
            StringList localList; /* REUSED */

            error = null;

            if (ProcessOps.CheckAndMaybeAppendElement(
                    EnvVars.EagleLibraryAppDomainCreateList, null,
                    false, out localList, ref error) == ReturnCode.Ok)
            {
                createList = localList;
            }
            else
            {
                DebugOps.Complain(null, ReturnCode.Error, error);
            }

            error = null;

            if (ProcessOps.CheckAndMaybeAppendElement(
                    EnvVars.EagleLibraryAppDomainUnloadList, null,
                    false, out localList, ref error) == ReturnCode.Ok)
            {
                unloadList = localList;
            }
            else
            {
                DebugOps.Complain(null, ReturnCode.Error, error);
            }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method records that another application domain has been
        /// created, updating both the process-wide tracking and the local
        /// counter.
        /// </summary>
        /// <param name="id">
        /// The identifier of the created application domain, or null to skip
        /// recording.
        /// </param>
        /// <returns>
        /// The updated local count of created application domains.
        /// </returns>
        private static long AnotherOneCreated(
            int? id
            )
        {
            if (id == null)
                return 0;

            Result error; /* REUSED */
            StringList list;

            error = null;

            if (ProcessOps.CheckAndMaybeAppendElement(
                    EnvVars.EagleLibraryAppDomainCreateList,
                    id.ToString(), false, out list,
                    ref error) != ReturnCode.Ok)
            {
                DebugOps.Complain(null, ReturnCode.Error, error);
            }

            long count;

            error = null;

            if (ProcessOps.CheckAndMaybeModifyReferenceCount(
                    EnvVars.EagleLibraryAppDomainCreateCount, null,
                    true, out count, ref error) != ReturnCode.Ok)
            {
                DebugOps.Complain(null, ReturnCode.Error, error);
            }

            return Interlocked.Increment(ref createCount);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method records that another application domain has been
        /// unloaded, updating both the process-wide tracking and the local
        /// counter.
        /// </summary>
        /// <param name="id">
        /// The identifier of the unloaded application domain, or null to skip
        /// recording.
        /// </param>
        /// <returns>
        /// The updated local count of unloaded application domains.
        /// </returns>
        private static long AnotherOneUnloaded(
            int? id
            )
        {
            if (id == null)
                return 0;

            Result error; /* REUSED */
            StringList list;

            error = null;

            if (ProcessOps.CheckAndMaybeAppendElement(
                    EnvVars.EagleLibraryAppDomainUnloadList,
                    id.ToString(), false, out list,
                    ref error) != ReturnCode.Ok)
            {
                DebugOps.Complain(null, ReturnCode.Error, error);
            }

            long count;

            error = null;

            if (ProcessOps.CheckAndMaybeModifyReferenceCount(
                    EnvVars.EagleLibraryAppDomainUnloadCount, null,
                    true, out count, ref error) != ReturnCode.Ok)
            {
                DebugOps.Complain(null, ReturnCode.Error, error);
            }

            return Interlocked.Increment(ref unloadCount);
        }
#endif

        ///////////////////////////////////////////////////////////////////////

#if CAS_POLICY && NET_40
        /// <summary>
        /// This method determines whether legacy Code Access Security (CAS)
        /// policy is enabled for the current application domain.
        /// </summary>
        /// <returns>
        /// True if legacy CAS policy is enabled; otherwise, false.
        /// </returns>
        public static bool IsLegacyCasPolicyEnabled()
        {
            return IsLegacyCasPolicyEnabled(AppDomain.CurrentDomain);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method determines whether legacy Code Access Security (CAS)
        /// policy is enabled for the specified application domain, querying the
        /// relevant property via reflection.
        /// </summary>
        /// <param name="appDomain">
        /// The application domain to check.
        /// </param>
        /// <returns>
        /// True if legacy CAS policy is enabled; otherwise, false.
        /// </returns>
        private static bool IsLegacyCasPolicyEnabled(
            AppDomain appDomain
            )
        {
            if (CommonOps.Runtime.IsMono() ||
                CommonOps.Runtime.IsDotNetCore())
            {
                return false;
            }

            try
            {
                PropertyInfo propertyInfo = null;

                lock (syncRoot) /* TRANSACTIONAL */
                {
                    if (isLegacyCasPolicyEnabledPropertyInfo == null)
                    {
                        isLegacyCasPolicyEnabledPropertyInfo =
                            typeof(AppDomain).GetProperty(
                                isLegacyCasPolicyEnabledPropertyName,
                                ObjectOps.GetBindingFlags(
                                    MetaBindingFlags.IsLegacyCasPolicyEnabled,
                                    true));
                    }

                    propertyInfo = isLegacyCasPolicyEnabledPropertyInfo;
                }

                if (propertyInfo != null)
                    return (bool)propertyInfo.GetValue(appDomain, null);
            }
            catch
            {
                // do nothing.
            }

            return false;
        }
#endif

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method determines whether an unload is pending for the
        /// specified application domain, as indicated by its data slot.
        /// </summary>
        /// <param name="appDomain">
        /// The application domain to check.
        /// </param>
        /// <returns>
        /// True if an unload is pending for the application domain; otherwise,
        /// false.
        /// </returns>
        private static bool IsPendingUnload(
            AppDomain appDomain
            )
        {
            if (appDomain == null)
                return false;

            try
            {
                return appDomain.GetData(UnloadDataName) != null;
            }
            catch (Exception e)
            {
                TraceOps.DebugTrace(
                    e, typeof(AppDomainOps).Name,
                    TracePriority.RemotingError);

                return false;
            }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method marks the specified application domain as having an
        /// unload pending, by setting its data slot.
        /// </summary>
        /// <param name="appDomain">
        /// The application domain to mark.
        /// </param>
        /// <returns>
        /// True if the application domain was marked successfully; otherwise,
        /// false.
        /// </returns>
        private static bool MarkPendingUnload(
            AppDomain appDomain
            )
        {
            if (appDomain == null)
                return false;

            try
            {
                appDomain.SetData(UnloadDataName, 1.ToString());
                return true;
            }
            catch (Exception e)
            {
                TraceOps.DebugTrace(
                    e, typeof(AppDomainOps).Name,
                    TracePriority.RemotingError);

                return false;
            }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method determines whether the specified application domain is
        /// currently being finalized for unload.
        /// </summary>
        /// <param name="appDomain">
        /// The application domain to check.
        /// </param>
        /// <returns>
        /// True if the application domain is finalizing for unload; otherwise,
        /// false.
        /// </returns>
        private static bool IsFinalizing(
            AppDomain appDomain
            )
        {
            if (appDomain == null)
                return false;

            return appDomain.IsFinalizingForUnload();
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method executes the specified delegate inside the specified
        /// application domain.  On runtimes that do not support cross-domain
        /// callbacks, the application domain must be the current one and the
        /// delegate is simply invoked directly.
        /// </summary>
        /// <param name="appDomain">
        /// The application domain in which to execute the delegate.
        /// </param>
        /// <param name="delegate">
        /// The delegate to execute.
        /// </param>
        public static void DoCallBack(
            AppDomain appDomain,
#if !NET_STANDARD_20
            CrossAppDomainDelegate @delegate
#else
            GenericCallback @delegate
#endif
            )
        {
            if (appDomain == null)
                throw new ArgumentNullException("appDomain");

            if (@delegate == null)
                throw new ArgumentNullException("delegate");

#if !NET_STANDARD_20
            appDomain.DoCallBack(@delegate);
#else
            if (!IsCurrent(appDomain))
            {
                throw new InvalidOperationException(
                    "application domain mismatch");
            }

            @delegate();
#endif
        }

        ///////////////////////////////////////////////////////////////////////

#if ISOLATED_PLUGINS
        /// <summary>
        /// This method clears the isolated host stored on the specified
        /// interpreter, when the interpreter is running in the same application
        /// domain as its parent.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter whose isolated host should be cleared.
        /// </param>
        /// <returns>
        /// True if the isolated host was cleared; otherwise, false.
        /// </returns>
        public static bool MaybeClearIsolatedHost(
            Interpreter interpreter
            )
        {
            return MaybeSetIsolatedHost(interpreter, null, true);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method stores the specified host as the isolated host on the
        /// specified interpreter, when the interpreter is running in the same
        /// application domain as its parent.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter on which to store the isolated host.
        /// </param>
        /// <param name="host">
        /// The host to store, or null to clear it.
        /// </param>
        /// <param name="overwrite">
        /// Non-zero to overwrite any existing isolated host; zero to set it
        /// only when one is not already present.
        /// </param>
        /// <returns>
        /// True if the isolated host was stored; otherwise, false.
        /// </returns>
        public static bool MaybeSetIsolatedHost(
            Interpreter interpreter,
            IHost host,
            bool overwrite
            )
        {
            //
            // NOTE: If this host is running in the same application domain
            //       as the parent interpreter, store this instance in the
            //       "isolatedHost" field of the interpreter for later use.
            //
            if ((interpreter != null) && !IsIsolated(interpreter))
            {
                if (overwrite || (interpreter.IsolatedHost == null))
                {
                    interpreter.IsolatedHost = host;
                    return true;
                }
            }

            return false;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method determines whether the specified interpreter is isolated
        /// (i.e. running in a different application domain than the current
        /// one).
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter to check.
        /// </param>
        /// <returns>
        /// True if the interpreter is isolated; otherwise, false.
        /// </returns>
        public static bool IsIsolated(
            Interpreter interpreter
            )
        {
            try
            {
                //
                // TODO: Can this really throw?
                //
                if (!IsSame(interpreter)) /* throw */
                    return true;
            }
            catch (Exception e)
            {
                TraceOps.DebugTrace(
                    e, typeof(AppDomainOps).Name,
                    TracePriority.HostError);
            }

            return false;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method determines whether the specified plugin is isolated
        /// (i.e. loaded in isolated mode), based on its flags.
        /// </summary>
        /// <param name="pluginData">
        /// The plugin data to check.
        /// </param>
        /// <returns>
        /// True if the plugin is isolated; otherwise, false.
        /// </returns>
        public static bool IsIsolated(
            IPluginData pluginData
            )
        {
            return (pluginData != null) && FlagOps.HasFlags(
                pluginData.Flags, PluginFlags.Isolated, true);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method maps an integral type code to the option flags that
        /// require an option value of the corresponding integral type.
        /// </summary>
        /// <param name="typeCode">
        /// The integral type code to map.
        /// </param>
        /// <param name="strict">
        /// Non-zero to return no flags for an unsupported type code; zero to
        /// fall back to the unsigned wide integer flag.
        /// </param>
        /// <returns>
        /// The option flags corresponding to the type code.
        /// </returns>
        private static OptionFlags GetEnumOptionFlags(
            TypeCode typeCode,
            bool strict
            )
        {
            switch (typeCode)
            {
                case TypeCode.Boolean:
                    {
                        return OptionFlags.MustBeBoolean;
                    }
                case TypeCode.SByte:
                    {
                        return OptionFlags.MustBeSignedByte;
                    }
                case TypeCode.Byte:
                    {
                        return OptionFlags.MustBeByte;
                    }
                case TypeCode.Int16:
                    {
                        return OptionFlags.MustBeNarrowInteger;
                    }
                case TypeCode.UInt16:
                    {
                        return OptionFlags.MustBeUnsignedNarrowInteger;
                    }
                case TypeCode.Int32:
                    {
                        return OptionFlags.MustBeInteger;
                    }
                case TypeCode.UInt32:
                    {
                        return OptionFlags.MustBeUnsignedInteger;
                    }
                case TypeCode.Int64:
                    {
                        return OptionFlags.MustBeWideInteger;
                    }
                case TypeCode.UInt64:
                    {
                        return OptionFlags.MustBeUnsignedWideInteger;
                    }
                default:
                    {
                        return strict ? OptionFlags.None :
                            OptionFlags.MustBeUnsignedWideInteger;
                    }
            }
        }

        ///////////////////////////////////////////////////////////////////////

        //
        // HACK: This is a horrible hack to workaround the issue with not being
        //       able to use plugin enumerated types from the "primary"
        //       application domain when the plugin has been loaded in isolated
        //       mode.
        //
        /// <summary>
        /// This method fixes up the enumerated-type options that refer to types
        /// defined within an isolated plugin assembly, converting them so that
        /// they can be used from the primary application domain.  For flags
        /// enumerations a placeholder type is substituted; for ordinary
        /// enumerations the option is converted to its integral type.
        /// </summary>
        /// <param name="pluginData">
        /// The plugin data whose options should be fixed up.
        /// </param>
        /// <param name="options">
        /// The option dictionary to fix up.
        /// </param>
        /// <param name="strict">
        /// Non-zero to treat missing plugin data or options as an error; zero
        /// to treat them as a successful no-op.
        /// </param>
        /// <param name="error">
        /// Upon failure, this contains an error message describing the problem.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise, an appropriate
        /// error code.
        /// </returns>
        public static ReturnCode FixupOptions(
            IPluginData pluginData,
            OptionDictionary options,
            bool strict,
            ref Result error
            )
        {
            if (pluginData == null)
            {
                if (strict)
                {
                    error = "invalid plugin data";
                    return ReturnCode.Error;
                }

                return ReturnCode.Ok;
            }

            if (options == null)
            {
                if (strict)
                {
                    error = "invalid options";
                    return ReturnCode.Error;
                }

                return ReturnCode.Ok;
            }

            if (!IsIsolated(pluginData))
                return ReturnCode.Ok;

            Assembly assembly = pluginData.Assembly;

            foreach (KeyValuePair<string, IOption> pair in options)
            {
                IOption option = pair.Value;

                if (option == null)
                    continue;

                //
                // HACK: Skip options that do not have enumerated types.
                //       For now, these are the only options we really have to
                //       worry about because they are the only ones that can
                //       directly refer to user-defined types [of any kind].
                //
                if (!option.HasFlags(OptionFlags.MustBeEnum, true))
                    continue;

                //
                // NOTE: Grab the enumerated (?) type and figure out if it
                //       came from the plugin assembly.  If not, ignore it and
                //       continue.
                //
                Type type = option.Type;

                if ((type == null) || !type.IsEnum ||
                    !Object.ReferenceEquals(type.Assembly, assembly))
                {
                    continue;
                }

                //
                // NOTE: Get the current value of the option.
                //
                object oldValue = option.InnerValue;
                TypeCode typeCode = TypeCode.Empty;

                //
                // NOTE: Attempt to get the new value for the integral type for
                //       the enumeration value of this option, if any.  We must
                //       do this even if the original value is null because we
                //       must have the type code to properly reset the option
                //       flags.
                //
                object newValue = EnumOps.ConvertToTypeCodeValue(type,
                    (oldValue != null) ? oldValue : 0, null, ref typeCode,
                    ref error);

                if (newValue == null)
                    return ReturnCode.Error;

                //
                // NOTE: Get the option flags required for the integral type.
                //
                OptionFlags flags = GetEnumOptionFlags(typeCode, strict);

                if (flags == OptionFlags.None)
                {
                    error = String.Format(
                        "unsupported type code for enumerated type {0}",
                        FormatOps.WrapOrNull(type));

                    return ReturnCode.Error;
                }

                //
                // NOTE: Special handling for "flags" enumerations here.
                //
                if (EnumOps.IsFlags(type))
                {
                    //
                    // HACK: Substitute our placeholder flags enumerated type.
                    //       It does not know about the textual values provided
                    //       by the actual enumerated type; however, at least
                    //       they can use the custom flags enumeration handling
                    //       (i.e. the "+" and "-" operators, etc).
                    //
                    option.Type = typeof(StubFlagsEnum);
                }
                else
                {
                    //
                    // NOTE: Remove the MustBeEnum flag for this option and add
                    //       the flag(s) needed for its integral type.
                    //
                    option.Flags &= ~OptionFlags.MustBeEnum;
                    option.Flags |= flags;

                    //
                    // NOTE: Clear the type for the option.  The type property
                    //       is only meaningful for enumeration-based options
                    //       and we are converting this option to use some kind
                    //       of integral type.
                    //
                    option.Type = null;
                }

                //
                // NOTE: If necessary, set the new [default] value for this
                //       option to the one we converted to an integral type
                //       value above.  If the old (original) value was null, we
                //       just discard the new value which will be zero anyhow.
                //
                option.Value = (oldValue != null) ? new Variant(newValue) : null;
            }

            return ReturnCode.Ok;
        }
#endif

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method obtains an existing application domain for the specified
        /// interpreter or creates a new isolated one, depending on whether
        /// isolation is requested.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter for which the application domain is obtained or
        /// created.
        /// </param>
        /// <param name="friendlyName">
        /// The friendly name to use for a newly created application domain.
        /// </param>
        /// <param name="baseDirectory">
        /// The base directory to use for a newly created application domain.
        /// </param>
        /// <param name="packagePath">
        /// The package path to include in the private binary path of a newly
        /// created application domain.
        /// </param>
        /// <param name="evidence">
        /// The Code Access Security evidence to associate with a newly created
        /// application domain.
        /// </param>
        /// <param name="clientData">
        /// The client data to associate with the operation.
        /// </param>
        /// <param name="isolated">
        /// Non-zero to create a new isolated application domain; zero to reuse
        /// the application domain configured for the interpreter.
        /// </param>
        /// <param name="useBasePath">
        /// Non-zero to use the base path when configuring the new application
        /// domain.
        /// </param>
        /// <param name="verifyCoreAssembly">
        /// Non-zero to verify that the core library assembly resides under the
        /// chosen base directory.
        /// </param>
        /// <param name="useEntryAssembly">
        /// Non-zero to refresh and use the entry assembly within the new
        /// application domain.
        /// </param>
        /// <param name="optionalEntryAssembly">
        /// Non-zero to treat failures involving the entry assembly as
        /// non-fatal.
        /// </param>
        /// <param name="appDomain">
        /// Upon success, this contains the obtained or created application
        /// domain.
        /// </param>
        /// <param name="error">
        /// Upon failure, this contains an error message describing the problem.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise, an appropriate
        /// error code.
        /// </returns>
        public static ReturnCode GetOrCreate(
            Interpreter interpreter,
            string friendlyName,
            string baseDirectory,
            string packagePath,
#if CAS_POLICY
            Evidence evidence,
#endif
            IClientData clientData,
            bool isolated,
            bool useBasePath,
            bool verifyCoreAssembly,
            bool useEntryAssembly,
            bool optionalEntryAssembly,
            ref AppDomain appDomain,
            ref Result error
            )
        {
            //
            // NOTE: Use an isolated application domain or the current one?
            //
            if (isolated)
            {
#if ISOLATED_PLUGINS
                //
                // BUGBUG: This feature does not currently work due to
                //         cross-domain marshalling issues.
                //
                return Create(
                    interpreter, friendlyName, baseDirectory, packagePath,
#if CAS_POLICY
                    evidence,
#endif
                    clientData, useBasePath, verifyCoreAssembly,
                    useEntryAssembly, optionalEntryAssembly,
                    ref appDomain, ref error);
#else
                error = "not implemented";
#endif
            }
            else if (interpreter != null)
            {
                //
                // NOTE: Get the application domain configured for this
                //       interpreter.
                //
                appDomain = GetFrom(interpreter);

                if (appDomain != null)
                {
                    return ReturnCode.Ok;
                }
                else
                {
                    error = "invalid application domain";
                }
            }
            else
            {
                error = "invalid interpreter";
            }

            return ReturnCode.Error;
        }

        ///////////////////////////////////////////////////////////////////////

#if ISOLATED_INTERPRETERS
        /// <summary>
        /// This method creates a new isolated application domain for use by the
        /// test suite (isolated interpreters).
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter for which the application domain is created.
        /// </param>
        /// <param name="friendlyName">
        /// The friendly name to use for the new application domain.
        /// </param>
        /// <param name="baseDirectory">
        /// The base directory to use for the new application domain.
        /// </param>
        /// <param name="packagePath">
        /// The package path to include in the private binary path of the new
        /// application domain.
        /// </param>
        /// <param name="clientData">
        /// The client data to associate with the operation.
        /// </param>
        /// <param name="useBasePath">
        /// Non-zero to use the base path when configuring the new application
        /// domain.
        /// </param>
        /// <param name="verifyCoreAssembly">
        /// Non-zero to verify that the core library assembly resides under the
        /// chosen base directory.
        /// </param>
        /// <param name="useEntryAssembly">
        /// Non-zero to refresh and use the entry assembly within the new
        /// application domain.
        /// </param>
        /// <param name="optionalEntryAssembly">
        /// Non-zero to treat failures involving the entry assembly as
        /// non-fatal.
        /// </param>
        /// <param name="appDomain">
        /// Upon success, this contains the created application domain.
        /// </param>
        /// <param name="error">
        /// Upon failure, this contains an error message describing the problem.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise, an appropriate
        /// error code.
        /// </returns>
        public static ReturnCode CreateForTest(
            Interpreter interpreter,
            string friendlyName,
            string baseDirectory,
            string packagePath,
            IClientData clientData,
            bool useBasePath,
            bool verifyCoreAssembly,
            bool useEntryAssembly,
            bool optionalEntryAssembly,
            ref AppDomain appDomain,
            ref Result error
            )
        {
            return Create(
                interpreter, friendlyName, baseDirectory, packagePath,
#if CAS_POLICY
                null,
#endif
                clientData, useBasePath, verifyCoreAssembly,
                useEntryAssembly, optionalEntryAssembly, ref appDomain,
                ref error);
        }
#endif

        ///////////////////////////////////////////////////////////////////////

#if APPDOMAINS
        #region TransferHelper Class (Serializable)
        /// <summary>
        /// This class is a serializable helper used to capture the static
        /// property and field values of a type in one application domain and
        /// reapply them within another application domain via a cross-domain
        /// callback.
        /// </summary>
#if SERIALIZATION
        [Serializable()]
#endif
        [ObjectId("b762237d-6008-4b8a-8376-483d0664d464")]
        private sealed class TransferHelper
        {
            #region Private Constants
            /// <summary>
            /// The binding flags used when reflecting over the static
            /// properties and fields to be transferred.
            /// </summary>
            private static BindingFlags bindingFlags =
                ObjectOps.GetBindingFlags(
                    MetaBindingFlags.TransferHelper, true);
            #endregion

            ///////////////////////////////////////////////////////////////////

            #region Private Data
            /// <summary>
            /// The type whose static property and field values are being
            /// transferred.
            /// </summary>
            private Type type;

            /// <summary>
            /// The optional glob patterns used to select which member names to
            /// include in the transfer.
            /// </summary>
            private StringList includeNames;

            /// <summary>
            /// The optional glob patterns used to select which member names to
            /// exclude from the transfer.
            /// </summary>
            private StringList excludeNames;

            /// <summary>
            /// When non-zero, exceptions encountered during the transfer are
            /// rethrown instead of being ignored.
            /// </summary>
            private bool failOnError;

            ///////////////////////////////////////////////////////////////////

            /// <summary>
            /// The captured static property values, keyed by property name.
            /// </summary>
            private ObjectDictionary properties;

            /// <summary>
            /// The captured static field values, keyed by field name.
            /// </summary>
            private ObjectDictionary fields;
            #endregion

            ///////////////////////////////////////////////////////////////////

            #region Public Constructors
            /// <summary>
            /// Constructs an instance of this class.
            /// </summary>
            /// <param name="type">
            /// The type whose static property and field values are to be
            /// transferred.
            /// </param>
            /// <param name="includeNames">
            /// The optional glob patterns selecting which member names to
            /// include.
            /// </param>
            /// <param name="excludeNames">
            /// The optional glob patterns selecting which member names to
            /// exclude.
            /// </param>
            /// <param name="failOnError">
            /// Non-zero to rethrow exceptions encountered during the transfer.
            /// </param>
            public TransferHelper(
                Type type,
                StringList includeNames,
                StringList excludeNames,
                bool failOnError
                )
            {
                this.type = type;
                this.includeNames = includeNames;
                this.excludeNames = excludeNames;
                this.failOnError = failOnError;
            }
            #endregion

            ///////////////////////////////////////////////////////////////////

            #region Private Methods
            /// <summary>
            /// This method determines whether the specified member name should
            /// be transferred, based on the configured include and exclude
            /// glob patterns.
            /// </summary>
            /// <param name="name">
            /// The member name to test.
            /// </param>
            /// <returns>
            /// True if the member name should be transferred; otherwise, false.
            /// </returns>
            private bool Match(
                string name
                )
            {
                if (String.IsNullOrEmpty(name))
                    return false;

                if (includeNames != null)
                {
                    bool match = false;

                    if (StringOps.MatchAnyOrAll(
                            null, MatchMode.Glob, name, includeNames,
                            false, false, ref match) != ReturnCode.Ok)
                    {
                        return false;
                    }

                    if (!match)
                        return false;
                }

                if (excludeNames != null)
                {
                    bool match = false;

                    if (StringOps.MatchAnyOrAll(
                            null, MatchMode.Glob, name, excludeNames,
                            false, false, ref match) != ReturnCode.Ok)
                    {
                        return false;
                    }

                    if (match)
                        return false;
                }

                return true;
            }
            #endregion

            ///////////////////////////////////////////////////////////////////

            #region Public Methods
            /// <summary>
            /// This method captures the current values of the matching static
            /// properties and fields of the configured type into this helper
            /// instance.
            /// </summary>
            public void Save()
            {
                if (type == null)
                    return;

                PropertyInfo[] propertyInfos = null;

                try
                {
                    propertyInfos = type.GetProperties(
                        bindingFlags); /* throw */
                }
                catch
                {
                    if (failOnError)
                        throw;
                }

                if (propertyInfos != null)
                {
                    int length = propertyInfos.Length;

                    for (int index = 0; index < length; index++)
                    {
                        try
                        {
                            PropertyInfo propertyInfo = propertyInfos[index];

                            if (propertyInfo == null)
                                continue;

                            if (!propertyInfo.CanRead)
                                continue;

                            string name = propertyInfo.Name;

                            if (Match(name))
                            {
                                object value = propertyInfo.GetValue(
                                    null, null); /* throw */

                                if (properties == null)
                                    properties = new ObjectDictionary();

                                properties[name] = value;
                            }
                        }
                        catch
                        {
                            if (failOnError)
                                throw;
                        }
                    }
                }

                FieldInfo[] fieldInfos = null;

                try
                {
                    fieldInfos = type.GetFields(
                        bindingFlags); /* throw */
                }
                catch
                {
                    if (failOnError)
                        throw;
                }

                if (fieldInfos != null)
                {
                    int length = fieldInfos.Length;

                    for (int index = 0; index < length; index++)
                    {
                        try
                        {
                            FieldInfo fieldInfo = fieldInfos[index];

                            if (fieldInfo == null)
                                continue;

                            string name = fieldInfo.Name;

                            if (Match(name))
                            {
                                object value = fieldInfo.GetValue(
                                    null); /* throw */

                                if (fields == null)
                                    fields = new ObjectDictionary();

                                fields[name] = value;
                            }
                        }
                        catch
                        {
                            if (failOnError)
                                throw;
                        }
                    }
                }
            }

            ///////////////////////////////////////////////////////////////////

            /// <summary>
            /// This method applies the previously captured static property and
            /// field values to the configured type in the current application
            /// domain.  Literal and read-only fields are skipped.
            /// </summary>
            public void Load()
            {
                if (type == null)
                    return;

                if (properties != null)
                {
                    foreach (KeyValuePair<string, object> pair in properties)
                    {
                        try
                        {
                            PropertyInfo propertyInfo = type.GetProperty(
                                pair.Key, bindingFlags); /* throw */

                            if (propertyInfo == null)
                                continue;

                            if (!propertyInfo.CanWrite)
                                continue;

                            propertyInfo.SetValue(
                                null, pair.Value, null); /* throw */
                        }
                        catch
                        {
                            if (failOnError)
                                throw;
                        }
                    }
                }

                if (fields != null)
                {
                    foreach (KeyValuePair<string, object> pair in fields)
                    {
                        try
                        {
                            FieldInfo fieldInfo = type.GetField(
                                pair.Key, bindingFlags); /* throw */

                            if (fieldInfo == null)
                                continue;

                            FieldAttributes attributes = fieldInfo.Attributes;

                            if (FlagOps.HasFlags(attributes,
                                    FieldAttributes.Literal, true) ||
                                FlagOps.HasFlags(attributes,
                                    FieldAttributes.InitOnly, true))
                            {
                                continue;
                            }

                            fieldInfo.SetValue(null, pair.Value); /* throw */
                        }
                        catch
                        {
                            if (failOnError)
                                throw;
                        }
                    }
                }
            }
            #endregion
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method transfers the matching static property and field values
        /// of the specified type from the current application domain into the
        /// specified target application domain via a cross-domain callback.
        /// </summary>
        /// <param name="appDomain">
        /// The target application domain to receive the transferred values.
        /// </param>
        /// <param name="type">
        /// The type whose static property and field values are to be
        /// transferred.
        /// </param>
        /// <param name="includeNames">
        /// The optional glob patterns selecting which member names to include.
        /// </param>
        /// <param name="excludeNames">
        /// The optional glob patterns selecting which member names to exclude.
        /// </param>
        /// <param name="failOnError">
        /// Non-zero to rethrow exceptions encountered during the transfer.
        /// </param>
        /// <param name="error">
        /// Upon failure, this contains an error message describing the problem.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise, an appropriate
        /// error code.
        /// </returns>
        private static ReturnCode TransferStaticInformation(
            AppDomain appDomain,
            Type type,
            StringList includeNames,
            StringList excludeNames,
            bool failOnError,
            ref Result error
            )
        {
            try
            {
                TransferHelper transferHelper = new TransferHelper(
                    type, includeNames, excludeNames, failOnError);

                transferHelper.Save(); /* throw */
                appDomain.DoCallBack(transferHelper.Load); /* throw */

                return ReturnCode.Ok;
            }
            catch (Exception e)
            {
                error = e;
                return ReturnCode.Error;
            }
        }
#endif

        ///////////////////////////////////////////////////////////////////////

#if APPDOMAINS || ISOLATED_INTERPRETERS || ISOLATED_PLUGINS
        #region PostCreateHelper Class (Serializable)
        /// <summary>
        /// This class is a serializable helper used to perform post-creation
        /// initialization within a newly created application domain via
        /// cross-domain callbacks, such as refreshing the entry assembly and
        /// performing static initialization.
        /// </summary>
#if SERIALIZATION
        [Serializable()]
#endif
        [ObjectId("40a88aa7-e9c9-477d-bd55-9ae6f99c8607")]
        private sealed class PostCreateHelper
        {
            #region Private Data
            /// <summary>
            /// The entry assembly to be refreshed within the target application
            /// domain.
            /// </summary>
            private Assembly entryAssembly;
            #endregion

            ///////////////////////////////////////////////////////////////////

            #region Private Constructors
            /// <summary>
            /// Constructs an instance of this class.
            /// </summary>
            /// <param name="entryAssembly">
            /// The entry assembly to be refreshed within the target application
            /// domain.
            /// </param>
            private PostCreateHelper(
                Assembly entryAssembly
                )
            {
                this.entryAssembly = entryAssembly;
            }
            #endregion

            ///////////////////////////////////////////////////////////////////

            #region Static "Factory" Methods
            /// <summary>
            /// This method creates a new instance of this class for the
            /// specified entry assembly.
            /// </summary>
            /// <param name="entryAssembly">
            /// The entry assembly to be refreshed within the target application
            /// domain.
            /// </param>
            /// <returns>
            /// The newly created helper instance.
            /// </returns>
            public static PostCreateHelper Create(
                Assembly entryAssembly
                )
            {
                return new PostCreateHelper(entryAssembly);
            }
            #endregion

            ///////////////////////////////////////////////////////////////////

            #region Public Methods
            /// <summary>
            /// This method refreshes the global entry assembly using the entry
            /// assembly captured by this helper.  It is intended to be invoked
            /// within the target application domain.
            /// </summary>
            public void RefreshEntryAssembly()
            {
                GlobalState.RefreshEntryAssembly(entryAssembly);
            }

            ///////////////////////////////////////////////////////////////////

            /// <summary>
            /// This method performs interpreter static initialization, if it
            /// has not already been performed.  It is intended to be invoked
            /// within the target application domain.
            /// </summary>
            public void MaybeStaticInitialize()
            {
                Interpreter.MaybeStaticInitialize();
            }
            #endregion
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method builds a friendly name for an application domain from a
        /// prefix and two values.  Each value may be a string used directly or
        /// a byte array that is hashed to produce a name component.
        /// </summary>
        /// <param name="prefix">
        /// The prefix to use for the friendly name.
        /// </param>
        /// <param name="value1">
        /// The first value, either a string or a byte array to be hashed.
        /// </param>
        /// <param name="value2">
        /// The second value, either a string or a byte array to be hashed.
        /// </param>
        /// <param name="error">
        /// Upon failure, this contains an error message describing the problem.
        /// </param>
        /// <returns>
        /// The constructed friendly name, or null if it could not be produced.
        /// </returns>
        public static string GetFriendlyName(
            string prefix,
            object value1,
            object value2,
            ref Result error
            )
        {
            string name1 = null;

            if (value1 is string)
            {
                name1 = (string)value1;
            }
            else if (value1 is byte[])
            {
                byte[] hashValue1 = HashOps.HashBytes(
                    null, (byte[])value1, ref error);

                if (hashValue1 == null)
                    return null;

                name1 = FormatOps.Hash(hashValue1);
            }

            string name2 = null;

            if (value2 is string)
            {
                name2 = (string)value2;
            }
            else if (value2 is byte[])
            {
                byte[] hashValue2 = HashOps.HashBytes(
                    null, (byte[])value2, ref error);

                if (hashValue2 == null)
                    return null;

                name2 = FormatOps.Hash(hashValue2);
            }

            return String.Format("{0} {1} {2}", prefix, name1, name2).Trim();
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method determines a usable base path for a new application
        /// domain, such that the path contains the core library assembly (and,
        /// optionally, the package path) somewhere underneath it.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter providing context for path resolution; this is
        /// optional and may be null.
        /// </param>
        /// <param name="packagePath">
        /// The package path that must also reside under the chosen base path,
        /// or null if there is none.
        /// </param>
        /// <param name="error">
        /// Upon failure, this contains an error message describing the problem.
        /// </param>
        /// <returns>
        /// The usable base path, or null if one could not be determined.
        /// </returns>
        private static string GetBasePath(
            Interpreter interpreter, /* OPTIONAL */
            string packagePath,
            ref Result error
            )
        {
            //
            // NOTE: Fetch the raw base directory for the currently executing
            //       application binary.  It is now possible to override the
            //       value used here via the environment.
            //
            string path0 = AssemblyOps.GetAnchorPath();

            if (path0 == null)
                path0 = GlobalState.GetRawBinaryBasePath();

            //
            // NOTE: First, try to use the effective path to the core library
            //       assembly.  This is used to verify that this candidate
            //       application domain base path contains the core library
            //       assembly somewhere underneath it.
            //
            string path1 = GetAssemblyPath();

            if (PathOps.IsUnderPath(interpreter, path1, path0))
            {
                if ((packagePath == null) ||
                    PathOps.IsUnderPath(interpreter, packagePath, path0))
                {
                    return path0;
                }
            }

            //
            // NOTE: Second, try to use the raw base path for the assembly.
            //       This is used to verify that this candidate application
            //       domain base path contains the core library assembly
            //       somewhere underneath it.
            //
            string path2 = GlobalState.GetRawBasePath();

            if (PathOps.IsUnderPath(interpreter, path1, path2))
            {
                if ((packagePath == null) ||
                    PathOps.IsUnderPath(interpreter, packagePath, path2))
                {
                    return path2;
                }
            }

            //
            // NOTE: At this point, we have failed to figure out a base path
            //       for the application domain to be created that actually
            //       contains the core library assembly.
            //
            error = String.Format(
                "cannot determine usable base path for the new application " +
                "domain for interpreter {0}, with the raw binary base path " +
                "{1}, assembly path {2}, and raw base path {3} for package " +
                "path {4}", FormatOps.InterpreterNoThrow(interpreter),
                FormatOps.DisplayPath(path0), FormatOps.DisplayPath(path1),
                FormatOps.DisplayPath(path2), FormatOps.DisplayPath(packagePath));

            return null;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method obtains the path to the core library assembly.
        /// </summary>
        /// <returns>
        /// The path to the core library assembly.
        /// </returns>
        private static string GetAssemblyPath()
        {
            return GlobalState.GetAssemblyPath();
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method appends descriptive information about an application
        /// domain setup, along with the current create and unload counts, to
        /// the specified list.
        /// </summary>
        /// <param name="appDomainSetup">
        /// The application domain setup whose information should be added, or
        /// null.
        /// </param>
        /// <param name="list">
        /// The list to which the information should be appended.
        /// </param>
        /// <param name="detailFlags">
        /// The detail flags controlling how much information is included.
        /// </param>
        private static void AddInfo(
            AppDomainSetup appDomainSetup,
            StringPairList list,
            DetailFlags detailFlags
            )
        {
            if (list == null)
                return;

            if (appDomainSetup != null)
            {
                bool empty = HostOps.HasEmptyContent(detailFlags);
                long count; /* REUSED */

                if (empty || (appDomainSetup.ApplicationName != null))
                {
                    list.Add("ApplicationName",
                        appDomainSetup.ApplicationName);
                }

                if (empty || (appDomainSetup.ApplicationBase != null))
                {
                    list.Add("ApplicationBase",
                        appDomainSetup.ApplicationBase);
                }

                if (empty || (appDomainSetup.PrivateBinPath != null))
                {
                    list.Add("PrivateBinPath",
                        appDomainSetup.PrivateBinPath);
                }

                count = Interlocked.CompareExchange(ref createCount, 0, 0);

                if (empty || (count > 0))
                    list.Add("CreateCount", count.ToString());

                count = Interlocked.CompareExchange(ref unloadCount, 0, 0);

                if (empty || (count > 0))
                    list.Add("UnloadCount", count.ToString());
            }
            else
            {
                list.Add(FormatOps.DisplayNull);
            }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method emits a diagnostic trace describing an application
        /// domain setup and the parameters used to create it.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter for which the application domain is being created.
        /// </param>
        /// <param name="friendlyName">
        /// The friendly name of the application domain being created.
        /// </param>
        /// <param name="baseDirectory">
        /// The base directory used for the application domain.
        /// </param>
        /// <param name="packagePath">
        /// The package path used for the application domain.
        /// </param>
        /// <param name="useBasePath">
        /// Non-zero if the base path is being used to configure the application
        /// domain.
        /// </param>
        /// <param name="verifyCoreAssembly">
        /// Non-zero if the core library assembly is being verified.
        /// </param>
        /// <param name="appDomainSetup">
        /// The application domain setup to describe.
        /// </param>
        private static void DumpSetup(
            Interpreter interpreter,
            string friendlyName,
            string baseDirectory,
            string packagePath,
            bool useBasePath,
            bool verifyCoreAssembly,
            AppDomainSetup appDomainSetup
            )
        {
            StringPairList list = new StringPairList();

            AddInfo(appDomainSetup, list, DetailFlags.DebugTrace);

            TraceOps.DebugTrace(String.Format(
                "DumpSetup: interpreter = {0}, friendlyName = {1}, " +
                "baseDirectory = {2}, packagePath = {3}, " +
                "useBasePath = {4}, verifyCoreAssembly = {5}, " +
                "appDomainSetup = {6}",
                FormatOps.InterpreterNoThrow(interpreter),
                FormatOps.WrapOrNull(friendlyName),
                FormatOps.WrapOrNull(baseDirectory),
                FormatOps.WrapOrNull(packagePath),
                useBasePath, verifyCoreAssembly, list),
                typeof(AppDomainOps).Name,
                TracePriority.RemotingDebug3);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method selects the application base directory to use for an
        /// application domain setup, preferring the base path, then the package
        /// path, and finally the assembly path.
        /// </summary>
        /// <param name="basePath">
        /// The base path to use when the base path is preferred.
        /// </param>
        /// <param name="packagePath">
        /// The package path to use when the base path is not preferred.
        /// </param>
        /// <param name="assemblyPath">
        /// The assembly path to use as a final fallback.
        /// </param>
        /// <param name="useBasePath">
        /// Non-zero to use the base path; zero to use the package or assembly
        /// path.
        /// </param>
        /// <returns>
        /// The selected application base directory.
        /// </returns>
        private static string GetSetupApplicationBase(
            string basePath,
            string packagePath,
            string assemblyPath,
            bool useBasePath
            )
        {
            if (useBasePath)
                return basePath;

            if (packagePath != null)
                return packagePath;

            return assemblyPath;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method computes the portion of a path relative to a base path,
        /// when the path is known to reside under that base path.
        /// </summary>
        /// <param name="basePath">
        /// The base path to make the path relative to.
        /// </param>
        /// <param name="path">
        /// The path to make relative.
        /// </param>
        /// <param name="underBasePath">
        /// Non-zero if the path is known to reside under the base path.
        /// </param>
        /// <returns>
        /// The relative path, or null if it could not be computed.
        /// </returns>
        private static string MakeRelativePath(
            string basePath,
            string path,
            bool underBasePath
            )
        {
            if (underBasePath && (basePath != null) && (path != null))
            {
                int baseLength = basePath.Length;

                if (path.Length >= baseLength)
                    return PathOps.MaybeTrim(path.Remove(0, baseLength));
            }

            return null;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method constructs and configures an application domain setup
        /// for a new application domain, computing the application base and
        /// private binary path so that both the core library assembly and the
        /// package can be located.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter providing context; this is optional and may be null.
        /// </param>
        /// <param name="friendlyName">
        /// The friendly name of the application domain being configured.
        /// </param>
        /// <param name="baseDirectory">
        /// The base directory to use, or null to derive one.
        /// </param>
        /// <param name="packagePath">
        /// The package path to include in the private binary path, or null.
        /// </param>
        /// <param name="useBasePath">
        /// Non-zero to use and verify a base path; zero to skip base path
        /// handling.
        /// </param>
        /// <param name="verifyCoreAssembly">
        /// Non-zero to verify that the core library assembly resides under the
        /// chosen base directory.
        /// </param>
        /// <param name="error">
        /// Upon failure, this contains an error message describing the problem.
        /// </param>
        /// <returns>
        /// The configured application domain setup, or null on failure.
        /// </returns>
        private static AppDomainSetup CreateSetup(
            Interpreter interpreter, /* OPTIONAL */
            string friendlyName,
            string baseDirectory,
            string packagePath,
            bool useBasePath,
            bool verifyCoreAssembly,
            ref Result error
            )
        {
            if (verifyCoreAssembly)
            {
                if (!GlobalState.VerifyAppDomainBaseDirectory(
                        interpreter, friendlyName, ref error))
                {
                    return null;
                }
            }

            string basePath = baseDirectory;

            if (useBasePath && (basePath == null) &&
                (interpreter != null))
            {
                basePath = interpreter.PluginBaseDirectory;
            }

            Result localError = null;

            if (useBasePath && (basePath == null))
            {
                basePath = GetBasePath(
                    interpreter, packagePath, ref localError);
            }

            if (useBasePath && (basePath == null))
            {
                if (localError != null)
                    error = localError;
                else
                    error = "invalid base path";

                return null;
            }

            bool packageUnderBasePath = (packagePath != null) ?
                PathOps.IsUnderPath(interpreter, packagePath, basePath) :
                false;

            //
            // NOTE: Verify package path is usable or superfluous.
            //
            if (useBasePath && (packagePath != null) &&
                !packageUnderBasePath)
            {
                error = "package path is not under base path";
                return null;
            }

            string assemblyPath = GetAssemblyPath();

            bool assemblyUnderBasePath = (assemblyPath != null) ?
                PathOps.IsUnderPath(interpreter, assemblyPath, basePath) :
                false;

            //
            // NOTE: Verify assembly path is usable or superfluous.
            //
            if (useBasePath && (assemblyPath != null) &&
                !assemblyUnderBasePath)
            {
                error = "assembly path is not under base path";
                return null;
            }

            AppDomainSetup appDomainSetup = new AppDomainSetup();

            //
            // NOTE: *SECURITY* Per the MSDN documentation, this should
            //       be disallowed for improved security.
            //
            appDomainSetup.DisallowCodeDownload = true;

            //
            // NOTE: Use base directory of the core library assembly as
            //       the base directory for the new isolated application
            //       domain.
            //
            appDomainSetup.ApplicationBase = GetSetupApplicationBase(
                basePath, packagePath, assemblyPath, useBasePath);

            //
            // NOTE: If we are using the base path of the core library
            //       assembly, then we need to modify the private binary
            //       path so it includes both the directory containing
            //       that core library assembly itself and the directory
            //       containing the package; otherwise, skip this step.
            //
            if (useBasePath)
            {
                //
                // TODO: May need to add more options here.
                //
                string relativeAssemblyPath = MakeRelativePath(
                    basePath, assemblyPath, assemblyUnderBasePath);

                string privateBinPath = relativeAssemblyPath;

                string relativePackagePath = MakeRelativePath(
                    basePath, packagePath, packageUnderBasePath);

                if (!String.IsNullOrEmpty(relativePackagePath) &&
                    !SharedStringOps.SystemEquals(
                        relativeAssemblyPath, relativePackagePath))
                {
                    if (!String.IsNullOrEmpty(privateBinPath))
                        privateBinPath += Characters.SemiColon;

                    privateBinPath += relativePackagePath;
                }

                appDomainSetup.PrivateBinPath = privateBinPath;
            }

            return appDomainSetup;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method creates a new application domain using a default base
        /// directory and no Code Access Security evidence or client data.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter for which the application domain is created.
        /// </param>
        /// <param name="friendlyName">
        /// The friendly name to use for the new application domain.
        /// </param>
        /// <param name="packagePath">
        /// The package path to include in the private binary path of the new
        /// application domain.
        /// </param>
        /// <param name="useBasePath">
        /// Non-zero to use the base path when configuring the new application
        /// domain.
        /// </param>
        /// <param name="verifyCoreAssembly">
        /// Non-zero to verify that the core library assembly resides under the
        /// chosen base directory.
        /// </param>
        /// <param name="useEntryAssembly">
        /// Non-zero to refresh and use the entry assembly within the new
        /// application domain.
        /// </param>
        /// <param name="optionalEntryAssembly">
        /// Non-zero to treat failures involving the entry assembly as
        /// non-fatal.
        /// </param>
        /// <param name="appDomain">
        /// Upon success, this contains the created application domain.
        /// </param>
        /// <param name="error">
        /// Upon failure, this contains an error message describing the problem.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise, an appropriate
        /// error code.
        /// </returns>
        public static ReturnCode Create(
            Interpreter interpreter,
            string friendlyName,
            string packagePath,
            bool useBasePath,
            bool verifyCoreAssembly,
            bool useEntryAssembly,
            bool optionalEntryAssembly,
            ref AppDomain appDomain,
            ref Result error
            )
        {
            return Create(
                interpreter, friendlyName, null, packagePath,
#if CAS_POLICY
                null,
#endif
                null, useBasePath, verifyCoreAssembly,
                useEntryAssembly, optionalEntryAssembly,
                ref appDomain, ref error);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method creates and configures a new application domain, records
        /// the create count, and optionally performs entry assembly handling
        /// inside the new application domain.  On failure, any partially
        /// created application domain is unloaded.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter for which the application domain is created.
        /// </param>
        /// <param name="friendlyName">
        /// The friendly name to use for the new application domain; an empty
        /// name is allowed but a null name is not.
        /// </param>
        /// <param name="baseDirectory">
        /// The base directory to use for the new application domain.
        /// </param>
        /// <param name="packagePath">
        /// The package path to include in the private binary path of the new
        /// application domain.
        /// </param>
        /// <param name="evidence">
        /// The Code Access Security evidence to associate with the new
        /// application domain.
        /// </param>
        /// <param name="clientData">
        /// The client data to associate with the operation.
        /// </param>
        /// <param name="useBasePath">
        /// Non-zero to use the base path when configuring the new application
        /// domain.
        /// </param>
        /// <param name="verifyCoreAssembly">
        /// Non-zero to verify that the core library assembly resides under the
        /// chosen base directory.
        /// </param>
        /// <param name="useEntryAssembly">
        /// Non-zero to refresh and use the entry assembly within the new
        /// application domain.
        /// </param>
        /// <param name="optionalEntryAssembly">
        /// Non-zero to treat failures involving the entry assembly as
        /// non-fatal.
        /// </param>
        /// <param name="appDomain">
        /// Upon success, this contains the created application domain.
        /// </param>
        /// <param name="error">
        /// Upon failure, this contains an error message describing the problem.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise, an appropriate
        /// error code.
        /// </returns>
        public static ReturnCode Create(
            Interpreter interpreter,
            string friendlyName,
            string baseDirectory,
            string packagePath,
#if CAS_POLICY
            Evidence evidence,
#endif
            IClientData clientData, /* NOT USED */
            bool useBasePath,
            bool verifyCoreAssembly,
            bool useEntryAssembly,
            bool optionalEntryAssembly,
            ref AppDomain appDomain,
            ref Result error
            )
        {
            //
            // NOTE: *WARNING* Empty application domain names are allowed,
            //       please do not change this to "!String.IsNullOrEmpty".
            //
            if (friendlyName != null)
            {
                bool success = false;
                AppDomain localAppDomain = null;

                try
                {
                    AppDomainSetup appDomainSetup = CreateSetup(
                        interpreter, friendlyName, baseDirectory,
                        packagePath, useBasePath, verifyCoreAssembly,
                        ref error);

                    if (appDomainSetup != null)
                    {
                        DumpSetup(
                            interpreter, friendlyName, baseDirectory,
                            packagePath, useBasePath, verifyCoreAssembly,
                            appDomainSetup);

                        localAppDomain = AppDomain.CreateDomain(
                            friendlyName,
#if CAS_POLICY
                            evidence,
#else
                            null,
#endif
                            appDomainSetup);

                        int? id = AppDomainOps.GetIdOrNull(
                            localAppDomain);

                        /* IGNORED */
                        AnotherOneCreated(id);

                        if (useEntryAssembly)
                        {
                            Assembly entryAssembly =
                                GlobalState.GetEntryAssembly();

                            TraceOps.DebugTrace(String.Format(
                                "Create: entryAssembly = {0}",
                                FormatOps.DisplayAssemblyName(
                                    entryAssembly)),
                                typeof(AppDomainOps).Name,
                                TracePriority.RemotingDebug2);

                            try
                            {
                                PostCreateHelper postCreateHelper =
                                    PostCreateHelper.Create(entryAssembly);

                                DoCallBack(localAppDomain,
                                    postCreateHelper.MaybeStaticInitialize);

                                DoCallBack(localAppDomain,
                                    postCreateHelper.RefreshEntryAssembly);
                            }
                            catch
                            {
                                if (!optionalEntryAssembly)
                                    throw;
                            }
                        }

                        TraceOps.DebugTrace(String.Format(
                            "Create: created application domain " +
                            "{0}, total created now {1}, total " +
                            "unloaded now {2}",
                            FormatOps.MaybeNull(id),
                            Interlocked.CompareExchange(
                                ref createCount, 0, 0),
                            Interlocked.CompareExchange(
                                ref unloadCount, 0, 0)),
                            typeof(AppDomainOps).Name,
                            TracePriority.RemotingDebug);

                        appDomain = localAppDomain;
                        success = true;

                        return ReturnCode.Ok;
                    }
                }
                catch (Exception e)
                {
                    error = e;
                }
                finally
                {
                    if (!success && (localAppDomain != null))
                    {
                        UnloadOrComplain(
                            interpreter, friendlyName, localAppDomain,
                            clientData);
                    }
                }
            }
            else
            {
                error = "invalid friendly name";
            }

            return ReturnCode.Error;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method creates and configures a new application domain and,
        /// optionally, copies static configuration (such as the trace
        /// operations configuration) into the new application domain.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter for which the application domain is created.
        /// </param>
        /// <param name="friendlyName">
        /// The friendly name to use for the new application domain.
        /// </param>
        /// <param name="baseDirectory">
        /// The base directory to use for the new application domain.
        /// </param>
        /// <param name="packagePath">
        /// The package path to include in the private binary path of the new
        /// application domain.
        /// </param>
        /// <param name="evidence">
        /// The Code Access Security evidence to associate with the new
        /// application domain.
        /// </param>
        /// <param name="clientData">
        /// The client data to associate with the operation.
        /// </param>
        /// <param name="useBasePath">
        /// Non-zero to use the base path when configuring the new application
        /// domain.
        /// </param>
        /// <param name="verifyCoreAssembly">
        /// Non-zero to verify that the core library assembly resides under the
        /// chosen base directory.
        /// </param>
        /// <param name="useEntryAssembly">
        /// Non-zero to refresh and use the entry assembly within the new
        /// application domain.
        /// </param>
        /// <param name="optionalEntryAssembly">
        /// Non-zero to treat failures involving the entry assembly as
        /// non-fatal.
        /// </param>
        /// <param name="copyConfiguration">
        /// Non-zero to copy static configuration into the new application
        /// domain after it is created.
        /// </param>
        /// <param name="appDomain">
        /// Upon success, this contains the created application domain.
        /// </param>
        /// <param name="error">
        /// Upon failure, this contains an error message describing the problem.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise, an appropriate
        /// error code.
        /// </returns>
        public static ReturnCode Create(
            Interpreter interpreter,
            string friendlyName,
            string baseDirectory,
            string packagePath,
#if CAS_POLICY
            Evidence evidence,
#endif
            IClientData clientData, /* NOT USED */
            bool useBasePath,
            bool verifyCoreAssembly,
            bool useEntryAssembly,
            bool optionalEntryAssembly,
            bool copyConfiguration,
            ref AppDomain appDomain,
            ref Result error
            )
        {
            if (Create(interpreter,
                    friendlyName, baseDirectory, packagePath,
#if CAS_POLICY
                    evidence,
#endif
                    clientData, useBasePath, verifyCoreAssembly,
                    useEntryAssembly, optionalEntryAssembly,
                    ref appDomain, ref error) != ReturnCode.Ok)
            {
                return ReturnCode.Error;
            }

            Result localError = null;

            if (copyConfiguration && TransferStaticInformation(
                    appDomain, typeof(TraceOps), null, null,
                    false, ref localError) != ReturnCode.Ok)
            {
                TraceOps.DebugTrace(String.Format(
                    "Create: failed to copy static " +
                    "TraceOps configuration: {0}",
                    FormatOps.WrapOrNull(localError)),
                    typeof(AppDomainOps).Name,
                    TracePriority.MarshalError);
            }

            return ReturnCode.Ok;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method formats the elapsed time, in microseconds, since the
        /// specified performance counter value into a display string.
        /// </summary>
        /// <param name="startCount">
        /// The performance counter value captured at the start of the elapsed
        /// interval.
        /// </param>
        /// <returns>
        /// The formatted elapsed time string.
        /// </returns>
        private static string FormatTime(
            long startCount
            )
        {
            return FormatOps.PerformanceMicroseconds(
                PerformanceOps.GetMicrosecondsFromCount(startCount,
                    PerformanceOps.GetCount(), 1, false));
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method unloads the specified application domain, retrying up to
        /// the configured retry limit and tolerating cases where the
        /// application domain is already unloaded (unless strict unloading is
        /// enabled).
        /// </summary>
        /// <param name="friendlyName">
        /// The friendly name of the application domain, used for diagnostics;
        /// this may be null.
        /// </param>
        /// <param name="appDomain">
        /// The application domain to unload.
        /// </param>
        /// <param name="clientData">
        /// The client data to associate with the operation.
        /// </param>
        /// <param name="error">
        /// Upon failure, this contains an error message describing the problem.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise, an appropriate
        /// error code.
        /// </returns>
        public static ReturnCode Unload(
            string friendlyName,
            AppDomain appDomain,
            IClientData clientData, /* NOT USED */
            ref Result error
            )
        {
            if (appDomain != null)
            {
                string appDomainName = (friendlyName != null) ?
                    FormatOps.WrapOrNull(friendlyName) :
                    FormatOps.DisplayAppDomain(appDomain);

                long startCount = PerformanceOps.GetCount();
                int retry = 0;

            retryUnload:

                try
                {
                    int count = Interlocked.Increment(ref retry);

                    try
                    {
                        if (MarkPendingUnload(appDomain))
                        {
                            int? id = appDomain.Id; /* NOT-NULL */

                            AppDomain.Unload(appDomain); /* throw */

                            /* IGNORED */
                            AnotherOneUnloaded(id);

                            TraceOps.DebugTrace(String.Format(
                                "Unload: unloaded application " +
                                "domain {0} ({1}) with retry " +
                                "count {2} in {3}, total " +
                                "locally created now {4}, " +
                                "total locally unloaded now {5}",
                                appDomainName,
                                FormatOps.MaybeNull(id),
                                count, FormatTime(startCount),
                                Interlocked.CompareExchange(
                                    ref createCount, 0, 0),
                                Interlocked.CompareExchange(
                                    ref unloadCount, 0, 0)),
                                typeof(AppDomainOps).Name,
                                TracePriority.RemotingDebug);

                            return ReturnCode.Ok;
                        }
                        else
                        {
                            error = "could not mark application domain";
                            return ReturnCode.Error;
                        }
                    }
                    catch (CannotUnloadAppDomainException e)
                    {
                        bool tryAgain = (count < UnloadRetryLimit);

                        TraceOps.DebugTrace(String.Format(
                            "Unload: failed to unload application " +
                            "domain {0} with retry count {1} in {2}, " +
                            "{3}...", appDomainName, count, FormatTime(
                            startCount), tryAgain ? "trying again" :
                            "done trying"), typeof(AppDomainOps).Name,
                            TracePriority.RemotingError);

                        if (tryAgain)
                        {
                            ObjectOps.CollectGarbage(
                                GarbageFlags.ForUnload);

                            goto retryUnload;
                        }

                        error = e;
                    }
                    catch (RemotingException e) /* HACK: Mono. */
                    {
                        TraceOps.DebugTrace(String.Format(
                            "Unload: application domain {0} is " +
                            "already unloaded via remoting?",
                            appDomainName),
                            typeof(AppDomainOps).Name,
                            TracePriority.RemotingError);

                        if (UnloadStrict)
                            error = e; // COMPAT: Eagle (legacy).
                        else
                            return ReturnCode.Ok;
                    }
                    catch (AppDomainUnloadedException e)
                    {
                        TraceOps.DebugTrace(String.Format(
                            "Unload: application domain {0} is " +
                            "already unloaded?", appDomainName),
                            typeof(AppDomainOps).Name,
                            TracePriority.RemotingError);

                        if (UnloadStrict)
                            error = e; // COMPAT: Eagle (legacy).
                        else
                            return ReturnCode.Ok;
                    }
                }
                catch (Exception e)
                {
                    error = e;
                }
            }
            else
            {
                error = "invalid application domain";
            }

            return ReturnCode.Error;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method unloads the specified application domain, complaining if
        /// the unload operation fails.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter used for complaint context.
        /// </param>
        /// <param name="friendlyName">
        /// The friendly name of the application domain, used for diagnostics;
        /// this may be null.
        /// </param>
        /// <param name="appDomain">
        /// The application domain to unload.
        /// </param>
        /// <param name="clientData">
        /// The client data to associate with the operation.
        /// </param>
        public static void UnloadOrComplain(
            Interpreter interpreter,
            string friendlyName,
            AppDomain appDomain,
            IClientData clientData /* NOT USED */
            )
        {
            ReturnCode unloadCode;
            Result unloadError = null;

            unloadCode = Unload(
                friendlyName, appDomain, clientData, ref unloadError);

            if (unloadCode != ReturnCode.Ok)
                DebugOps.Complain(interpreter, unloadCode, unloadError);
        }
#endif
        #endregion
    }
}
