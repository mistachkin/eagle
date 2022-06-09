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

#if APPDOMAINS || ISOLATED_INTERPRETERS || ISOLATED_PLUGINS
using System.Threading;
#endif

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
        [SuppressUnmanagedCodeSecurity()]
        [ObjectId("5857d617-b3fa-4ce6-89f8-3961ad6aa50c")]
        internal static class UnsafeNativeMethods
        {
            #region COM Identifiers for CLR Hosting
            internal const string CLSID_CorRuntimeHost_String =
                "cb2f6723-ab3a-11d2-9c40-00c04fa30a3e"; /* mscoree.h */

            internal const string IID_ICorRuntimeHost_String =
                "cb2f6722-ab3a-11d2-9c40-00c04fa30a3e"; /* mscoree.h */
            #endregion

            ///////////////////////////////////////////////////////////////////

            #region CLR Hosting Enumerations
            [ObjectId("a1cd61e4-857d-4ee1-b3b7-216d4d5de3f6")]
            internal enum STARTUP_FLAGS /* mscoree.h */
            {
                STARTUP_NONE = 0x0,

                STARTUP_CONCURRENT_GC = 0x1,
                STARTUP_LOADER_OPTIMIZATION_MASK = 0x3 << 1,
                STARTUP_LOADER_OPTIMIZATION_SINGLE_DOMAIN = 0x1 << 1,
                STARTUP_LOADER_OPTIMIZATION_MULTI_DOMAIN = 0x2 << 1,
                STARTUP_LOADER_OPTIMIZATION_MULTI_DOMAIN_HOST = 0x3 << 1,

                STARTUP_LOADER_SAFEMODE = 0x10,
                STARTUP_LOADER_SETPREFERENCE = 0x100,

                STARTUP_SERVER_GC = 0x1000,
                STARTUP_HOARD_GC_VM = 0x2000,

                STARTUP_SINGLE_VERSION_HOSTING_INTERFACE = 0x4000,
                STARTUP_LEGACY_IMPERSONATION = 0x10000,
                STARTUP_DISABLE_COMMITTHREADSTACK = 0x20000,
                STARTUP_ALWAYSFLOW_IMPERSONATION = 0x40000,
                STARTUP_TRIM_GC_COMMIT = 0x80000,

                STARTUP_ETW = 0x100000,
                STARTUP_ARM = 0x400000
            }
            #endregion

            ///////////////////////////////////////////////////////////////////

            #region CLR Static Functions
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
            [ComImport]
            [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
            [Guid("cb2f6722-ab3a-11d2-9c40-00c04fa30a3e")]
            [ComConversionLoss]
            [ObjectId("b67eb5f1-e800-4f24-a4a0-afe1c74ad882")]
            internal interface ICorRuntimeHost /* mscoree.h */
            {
                void Void00();
                void Void01();
                void Void02();
                void Void03();
                void Void04();
                void Void05();
                void Void06();
                void Void07();
                void Void08();
                void Void09();

                ///////////////////////////////////////////////////////////////

                [return: MarshalAs(UnmanagedType.U4)]
                [MethodImpl(MethodImplOptions.InternalCall,
                    MethodCodeType = MethodCodeType.Runtime)]
                [PreserveSig]
                int GetDefaultDomain(out _AppDomain appDomain);

                ///////////////////////////////////////////////////////////////

                void Void10();
                void Void11();
                void Void12();
                void Void13();
                void Void14();
                void Void15();
                void Void16();
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
        private static readonly int InvalidId = -1;

        ///////////////////////////////////////////////////////////////////////

        private const string UnloadDataName = "_EAGLE_PENDING_UNLOAD";

        ///////////////////////////////////////////////////////////////////////

#if REMOTING
        private const string domainIdFieldName = "_domainID";
#endif

        ///////////////////////////////////////////////////////////////////////

#if CAS_POLICY && NET_40
        private const string isLegacyCasPolicyEnabledPropertyName =
            "IsLegacyCasPolicyEnabled";
#endif

        ///////////////////////////////////////////////////////////////////////

#if APPDOMAINS || ISOLATED_INTERPRETERS || ISOLATED_PLUGINS
        //
        // HACK: This is purposely not read-only.
        //
        private static int UnloadRetryLimit = 3; // TODO: Good default?

        ///////////////////////////////////////////////////////////////////////

        //
        // HACK: This is purposely not read-only.
        //
        private static bool UnloadStrict = false; // TODO: Good default?
#endif

        ///////////////////////////////////////////////////////////////////////

#if !NET_STANDARD_20 && NATIVE && WINDOWS
        //
        // HACK: These are purposely not read-only.
        //
        private static Guid CLSID_CorRuntimeHost = new Guid(
            UnsafeNativeMethods.CLSID_CorRuntimeHost_String);

        private static Guid IID_ICorRuntimeHost = new Guid(
            UnsafeNativeMethods.IID_ICorRuntimeHost_String);
#endif
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Private Data
#if REMOTING
        private static readonly object syncRoot = new object();

        ///////////////////////////////////////////////////////////////////////

        private static FieldInfo domainIdFieldInfo = null;
#endif

        ///////////////////////////////////////////////////////////////////////

#if CAS_POLICY && NET_40
        private static PropertyInfo isLegacyCasPolicyEnabledPropertyInfo = null;
#endif
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region AppDomain / Remoting Support Methods
#if NATIVE && WINDOWS
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

        public static bool IsPrimary()
        {
            return IsPrimary(AppDomain.CurrentDomain);
        }

        ///////////////////////////////////////////////////////////////////////

        private static bool IsPrimary(
            AppDomain appDomain
            ) /* GLOBAL */
        {
            if (appDomain == null)
                return false;

            return IsSame(appDomain, GlobalState.GetAppDomain());
        }

        ///////////////////////////////////////////////////////////////////////

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

        public static bool IsTransparentProxy(
            IWrapper wrapper
            )
        {
            if (wrapper == null)
                return false;

            return IsTransparentProxy(wrapper.Object);
        }

        ///////////////////////////////////////////////////////////////////////

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

        private static Type FindType(
            string typeName
            )
        {
            return FindType(typeName, AppDomain.CurrentDomain);
        }

        ///////////////////////////////////////////////////////////////////////

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

        public static Type MaybeGetType(
            object value
            )
        {
            return MaybeGetType(value, null, typeof(object));
        }

        ///////////////////////////////////////////////////////////////////////

        public static Type MaybeGetTypeOrNull(
            object value
            )
        {
            return MaybeGetType(value, null, null);
        }

        ///////////////////////////////////////////////////////////////////////

        public static Type MaybeGetTypeOrObject(
            object value
            )
        {
            return MaybeGetType(value, typeof(object), typeof(object));
        }

        ///////////////////////////////////////////////////////////////////////

        public static Type MaybeGetType(
            object value,
            Type defaultType
            )
        {
            return MaybeGetType(value, defaultType, defaultType);
        }

        ///////////////////////////////////////////////////////////////////////

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
            }
            catch
            {
                // do nothing.
            }
            finally
            {
                interpreter.ExitLock(ref locked); /* TRANSACTIONAL */
            }

            return null;
        }

        ///////////////////////////////////////////////////////////////////////

        public static AppDomain GetCurrent()
        {
            return AppDomain.CurrentDomain;
        }

        ///////////////////////////////////////////////////////////////////////

        public static int GetCurrentId()
        {
            return GetId(AppDomain.CurrentDomain);
        }

        ///////////////////////////////////////////////////////////////////////

        public static int GetPrimaryId() /* GLOBAL */
        {
            return GetId(GlobalState.GetAppDomain());
        }

        ///////////////////////////////////////////////////////////////////////

        public static string GetIdString(
            AppDomain appDomain
            )
        {
            if (appDomain == null)
                return null;

            int id = appDomain.Id;

            if (id == InvalidId)
                return null;

            return id.ToString();
        }

        ///////////////////////////////////////////////////////////////////////

        public static int GetId(
            AppDomain appDomain
            )
        {
            if (appDomain == null)
                return InvalidId;

            return appDomain.Id;
        }

        ///////////////////////////////////////////////////////////////////////

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

        public static bool IsCross(
            IPluginData pluginData
            )
        {
#if ISOLATED_PLUGINS
            if (IsIsolated(pluginData))
                return true;
#endif

            return IsCrossNoIsolated(pluginData);
        }

        ///////////////////////////////////////////////////////////////////////

        //
        // BUGBUG: Should this just use the IsTransparentProxy method instead?
        //
        public static bool IsCrossNoIsolated(
            IPluginData pluginData
            )
        {
            AppDomain pluginAppDomain = (pluginData != null) ?
                pluginData.AppDomain : null;

            AppDomain currentAppDomain = GetCurrent();

            if (!IsSame(pluginAppDomain, currentAppDomain))
                return true;

            if (!IsSameId(pluginData, currentAppDomain))
                return true;

            return false;
        }

        ///////////////////////////////////////////////////////////////////////

        public static bool IsCross(
            Interpreter interpreter,
            IPluginData pluginData
            )
        {
#if ISOLATED_PLUGINS
            if (IsIsolated(pluginData))
                return true;
#endif

            return IsCrossNoIsolated(interpreter, pluginData);
        }

        ///////////////////////////////////////////////////////////////////////

        //
        // BUGBUG: Should this just use the IsTransparentProxy method instead?
        //
        public static bool IsCrossNoIsolated(
            Interpreter interpreter,
            IPluginData pluginData
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
                if (!interpreter.IsOrphanInterpreter() &&
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

            AppDomain pluginAppDomain = (pluginData != null) ?
                pluginData.AppDomain : null;

            if (!IsSame(interpreterAppDomain, pluginAppDomain))
                return true;

            if (!IsSameId(interpreter, pluginData))
                return true;

            return false;
        }

        ///////////////////////////////////////////////////////////////////////

        public static bool IsPrimaryDefault() /* GLOBAL */
        {
            return IsDefault(GlobalState.GetAppDomain());
        }

        ///////////////////////////////////////////////////////////////////////

        public static bool IsCurrentDefault()
        {
            return IsDefault(AppDomain.CurrentDomain);
        }

        ///////////////////////////////////////////////////////////////////////

        public static bool IsDefault(
            AppDomain appDomain
            )
        {
            return ((appDomain != null) && appDomain.IsDefaultAppDomain());
        }

        ///////////////////////////////////////////////////////////////////////

        public static bool IsCurrent(
            AppDomain appDomain
            )
        {
            return IsSame(appDomain, AppDomain.CurrentDomain);
        }

        ///////////////////////////////////////////////////////////////////////

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

        public static bool IsSame(
            Interpreter interpreter
            )
        {
            return IsSame(interpreter, AppDomain.CurrentDomain);
        }

        ///////////////////////////////////////////////////////////////////////

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

        public static bool IsSame(
            IPluginData pluginData1,
            IPluginData pluginData2
            )
        {
            return IsSameId(pluginData1, pluginData2);
        }

        ///////////////////////////////////////////////////////////////////////

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

#if CAS_POLICY && NET_40
        public static bool IsLegacyCasPolicyEnabled()
        {
            return IsLegacyCasPolicyEnabled(AppDomain.CurrentDomain);
        }

        ///////////////////////////////////////////////////////////////////////

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

        private static bool IsFinalizing(
            AppDomain appDomain
            )
        {
            if (appDomain == null)
                return false;

            return appDomain.IsFinalizingForUnload();
        }

        ///////////////////////////////////////////////////////////////////////

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
        public static bool MaybeClearIsolatedHost(
            Interpreter interpreter
            )
        {
            return MaybeSetIsolatedHost(interpreter, null, true);
        }

        ///////////////////////////////////////////////////////////////////////

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

        public static bool IsIsolated(
            IPluginData pluginData
            )
        {
            return (pluginData != null) &&
                FlagOps.HasFlags(pluginData.Flags, PluginFlags.Isolated, true);
        }

        ///////////////////////////////////////////////////////////////////////

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
                        "unsupported type code for enumerated type \"{0}\"",
                        type);

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
#if SERIALIZATION
        [Serializable()]
#endif
        [ObjectId("b762237d-6008-4b8a-8376-483d0664d464")]
        private sealed class TransferHelper
        {
            #region Private Constants
            private static BindingFlags bindingFlags =
                ObjectOps.GetBindingFlags(
                    MetaBindingFlags.TransferHelper, true);
            #endregion

            ///////////////////////////////////////////////////////////////////

            #region Private Data
            private Type type;
            private StringList includeNames;
            private StringList excludeNames;
            private bool failOnError;

            ///////////////////////////////////////////////////////////////////

            private ObjectDictionary properties;
            private ObjectDictionary fields;
            #endregion

            ///////////////////////////////////////////////////////////////////

            #region Public Constructors
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

                            if (FlagOps.HasFlags(fieldInfo.Attributes,
                                    FieldAttributes.InitOnly, true))
                            {
                                continue;
                            }

                            fieldInfo.SetValue(
                                null, pair.Value); /* throw */
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

        public static ReturnCode TransferStaticInformation(
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
#if SERIALIZATION
        [Serializable()]
#endif
        [ObjectId("40a88aa7-e9c9-477d-bd55-9ae6f99c8607")]
        private sealed class PostCreateHelper
        {
            #region Private Data
            private Assembly entryAssembly;
            #endregion

            ///////////////////////////////////////////////////////////////////

            #region Private Constructors
            private PostCreateHelper(
                Assembly entryAssembly
                )
            {
                this.entryAssembly = entryAssembly;
            }
            #endregion

            ///////////////////////////////////////////////////////////////////

            #region Static "Factory" Methods
            public static PostCreateHelper Create(
                Assembly entryAssembly
                )
            {
                return new PostCreateHelper(entryAssembly);
            }
            #endregion

            ///////////////////////////////////////////////////////////////////

            #region Public Methods
            public void RefreshEntryAssembly()
            {
                GlobalState.RefreshEntryAssembly(entryAssembly);
            }
            #endregion
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

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

        private static string GetAssemblyPath()
        {
            return GlobalState.GetAssemblyPath();
        }

        ///////////////////////////////////////////////////////////////////////

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
            }
            else
            {
                list.Add(FormatOps.DisplayNull);
            }
        }

        ///////////////////////////////////////////////////////////////////////

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
                                    postCreateHelper.RefreshEntryAssembly);
                            }
                            catch
                            {
                                if (!optionalEntryAssembly)
                                    throw;
                            }
                        }

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

        private static string FormatTime(
            long startCount
            )
        {
            return FormatOps.Performance(PerformanceOps.GetMicroseconds(
                startCount, PerformanceOps.GetCount(), 1, false));
        }

        ///////////////////////////////////////////////////////////////////////

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
                            AppDomain.Unload(appDomain); /* throw */

                            TraceOps.DebugTrace(String.Format(
                                "Unload: unloaded application domain " +
                                "{0} with retry count {1} in {2}",
                                appDomainName, count, FormatTime(
                                startCount)), typeof(AppDomainOps).Name,
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
