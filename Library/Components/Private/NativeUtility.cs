/*
 * NativeUtility.cs --
 *
 * Copyright (c) 2007-2012 by Joe Mistachkin.  All rights reserved.
 *
 * See the file "license.terms" for information on usage and redistribution of
 * this file, and for a DISCLAIMER OF ALL WARRANTIES.
 *
 * RCS: @(#) $Id: $
 */

using System;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Threading;
using Eagle._Attributes;
using Eagle._Components.Private.Delegates;
using Eagle._Components.Public;
using Eagle._Constants;
using Eagle._Containers.Private;
using Eagle._Containers.Public;
using Eagle._Interfaces.Public;
using SharedStringOps = Eagle._Components.Shared.StringOps;

#if NET_STANDARD_21
using Index = Eagle._Constants.Index;
#endif

namespace Eagle._Components.Private
{
    [ObjectId("4e7b9ec6-8474-49ec-9f5f-59c3f21e7046")]
    internal static class NativeUtility
    {
        #region Private Constants
        private const string optionDebug = " DEBUG";
        private const string optionRelease = " RELEASE";
        private const string optionSizeOfWcharT = " SIZE_OF_WCHAR_T=2";
        private const string optionUse32BitSizeT = " USE_32BIT_SIZE_T=1";
        private const string optionUseSysStringLen = " USE_SYSSTRINGLEN=1";
        private const string optionUseHeapApi = " USE_HEAPAPI=1";

        ///////////////////////////////////////////////////////////////////////

        //
        // HACK: This is purposely not read-only.
        //
        private static StringComparison optionComparisonType =
            SharedStringOps.SystemComparisonType;

        ///////////////////////////////////////////////////////////////////////

#if WINDOWS
        //
        // HACK: This is purposely not read-only.
        //
        private static long compactEveryCount = 1000000;

        ///////////////////////////////////////////////////////////////////////

        //
        // HACK: These are purposely not read-only.
        //
        private static UIntPtr heapInitialSize = new UIntPtr(33554432); /* 32MB */
        private static UIntPtr heapMaximumSize = new UIntPtr(0);
#endif
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Private Data
        private static readonly object syncRoot = new object();

        ///////////////////////////////////////////////////////////////////////

        private static IntPtr nativeModule = IntPtr.Zero;
        private static string nativeFileName = null;
        private static TypeDelegateDictionary nativeDelegates;
        private static TypeBoolDictionary nativeOptional;

        ///////////////////////////////////////////////////////////////////////

#if WINDOWS
        private static IntPtr nativeHeap = IntPtr.Zero;
        private static bool? nativeUseHeapApi = null;
#endif

        ///////////////////////////////////////////////////////////////////////

        private static Eagle_GetVersion nativeGetVersion;
        private static Eagle_FreeVersion nativeFreeVersion;
        private static Eagle_AllocateMemory nativeAllocateMemory;
        private static Eagle_FreeMemory nativeFreeMemory;
        private static Eagle_FreeElements nativeFreeElements;
        private static Eagle_SplitList nativeSplitList;
        private static Eagle_JoinList nativeJoinList;
        private static Eagle_SetMemoryHeap nativeSetMemoryHeap;

        ///////////////////////////////////////////////////////////////////////

        private static long splitCount;
        private static long joinCount;

        ///////////////////////////////////////////////////////////////////////

#if WINDOWS
        private static long maybeCompactCount;
        private static long compactCount;
#endif

        ///////////////////////////////////////////////////////////////////////

        private static bool strictPath = false;

        ///////////////////////////////////////////////////////////////////////

        private static bool locked = false;
        private static bool disabled = false; /* INFORMATIONAL */
        private static bool? isAvailable = null;
        private static string version = null;

        ///////////////////////////////////////////////////////////////////////

        private static FieldInfo itemsFieldInfo = null;
        private static bool noReflection = false;

        ///////////////////////////////////////////////////////////////////////

        //
        // NOTE: Permit native utility library to be loaded on operating
        //       systems other than Windows?
        //
        private static bool forceNonWindows = false;

        ///////////////////////////////////////////////////////////////////////

#if MONO || MONO_HACKS
        //
        // HACK: *MONO* Just in case Mono eventually fixes the crash issue,
        //       allow this static field to be preset to bypass the runtime
        //       check.
        //
        private static bool forceMono = false;
#endif
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Private Methods
        private static bool IsUsable(
            string version,
            bool debug,
            out bool useHeapApi
            )
        {
            useHeapApi = false;

            if (version == null)
            {
                TraceOps.DebugTrace(
                    "IsUsable: invalid version string",
                    typeof(NativeUtility).Name,
                    TracePriority.NativeError);

                return false;
            }

            if (debug)
            {
                if (version.IndexOf(optionDebug,
                        optionComparisonType) == Index.Invalid)
                {
                    TraceOps.DebugTrace(String.Format(
                        "IsUsable: missing option {0}",
                        FormatOps.WrapOrNull(optionDebug)),
                        typeof(NativeUtility).Name,
                        TracePriority.NativeError);

                    return false;
                }
            }
            else
            {
                if (version.IndexOf(optionRelease,
                        optionComparisonType) == Index.Invalid)
                {
                    TraceOps.DebugTrace(String.Format(
                        "IsUsable: missing option {0}",
                        FormatOps.WrapOrNull(optionRelease)),
                        typeof(NativeUtility).Name,
                        TracePriority.NativeError);

                    return false;
                }
            }

            if (version.IndexOf(optionSizeOfWcharT,
                    optionComparisonType) == Index.Invalid)
            {
                TraceOps.DebugTrace(String.Format(
                    "IsUsable: missing option {0}",
                    FormatOps.WrapOrNull(optionSizeOfWcharT)),
                    typeof(NativeUtility).Name,
                    TracePriority.NativeError);

                return false;
            }

            if (version.IndexOf(optionUse32BitSizeT,
                    optionComparisonType) == Index.Invalid)
            {
                TraceOps.DebugTrace(String.Format(
                    "IsUsable: missing option {0}",
                    FormatOps.WrapOrNull(optionUse32BitSizeT)),
                    typeof(NativeUtility).Name,
                    TracePriority.NativeError);

                return false;
            }

#if NATIVE_UTILITY_BSTR
            if (version.IndexOf(optionUseSysStringLen,
                    optionComparisonType) == Index.Invalid)
            {
                TraceOps.DebugTrace(String.Format(
                    "IsUsable: missing option {0}",
                    FormatOps.WrapOrNull(optionUseSysStringLen)),
                    typeof(NativeUtility).Name,
                    TracePriority.NativeError);

                return false;
            }
#else
            if (version.IndexOf(optionUseSysStringLen,
                    optionComparisonType) != Index.Invalid)
            {
                TraceOps.DebugTrace(String.Format(
                    "IsUsable: mismatched option {0}",
                    FormatOps.WrapOrNull(optionUseSysStringLen)),
                    typeof(NativeUtility).Name,
                    TracePriority.NativeError);

                return false;
            }
#endif

            if (version.IndexOf(optionUseHeapApi,
                    optionComparisonType) != Index.Invalid)
            {
                TraceOps.DebugTrace(String.Format(
                    "IsUsable: found option {0}",
                    FormatOps.WrapOrNull(optionUseHeapApi)),
                    typeof(NativeUtility).Name,
                    TracePriority.NativeDebug);

                useHeapApi = true;
            }
            else
            {
                TraceOps.DebugTrace(String.Format(
                    "IsUsable: missing option {0}",
                    FormatOps.WrapOrNull(optionUseHeapApi)),
                    typeof(NativeUtility).Name,
                    TracePriority.NativeWarning);
            }

            return true;
        }

        ///////////////////////////////////////////////////////////////////////

        private static string GetNativeLibraryFileName(
            Interpreter interpreter, /* NOT USED */
            ref Result error
            )
        {
            //
            // HACK: For now, the native utility library is supported only
            //       on Windows.
            //
            if (!forceNonWindows &&
                !PlatformOps.IsWindowsOperatingSystem())
            {
                error = "not supported on this operating system";
                return null;
            }

            string path = CommonOps.Environment.GetVariable(
                EnvVars.UtilityPath);

            if (!String.IsNullOrEmpty(path))
            {
                if (File.Exists(path))
                    return path;

                if (Directory.Exists(path))
                {
                    string fileName = PathOps.CombinePath(
                        null, path, DllName.Utility);

                    if (File.Exists(fileName))
                        return fileName;

                    //
                    // TODO: Is this strictly necessary here?  It is known
                    //       at this point that this file does not exist.
                    //       Setting the path here only controls the result
                    //       returned in non-strict mode (below).
                    //
                    path = fileName;
                }

                //
                // NOTE: If the environment variable was set and the utility
                //       library could not be found, force an invalid result
                //       to be returned.  This ends up skipping the standard
                //       automatic utility library detection logic.
                //
                lock (syncRoot)
                {
                    return strictPath ? null : path;
                }
            }

            //
            // NOTE: The initial basis for the native utility library path is
            //       the path of the assembly being executed.
            //
            string basePath = GlobalState.GetAssemblyPath();

            //
            // NOTE: Initially, the candidate base path has not been mutated.
            //       This flag ensures it will only be mutated a maximum of
            //       one time.
            //
            bool wasMutated = false;

            //
            // HACK: If the processor architecture ends up being "AMD64", we
            //       want it to be "x64" instead, to match the platform name
            //       used by the native utility library project itself.
            //
            string processorName = PlatformOps.GetAlternateProcessorName(
                RuntimeOps.GetProcessorArchitecture(), true, false);

        retry:

            if (processorName != null)
            {
                path = PathOps.CombinePath(
                    null, basePath, processorName, DllName.Utility);

                if (File.Exists(path))
                    return path;
            }

            path = PathOps.CombinePath(null, basePath, DllName.Utility);

            if (File.Exists(path))
                return path;

            //
            // HACK: If the path can be successfully mutated (e.g. in order
            //       to remove superfluous portions) then try again with the
            //       mutated path.  This mutation is only performed once, if
            //       applicable.
            //
            if (!wasMutated && PathOps.MaybePreMutatePath(ref basePath))
            {
                wasMutated = true;
                goto retry;
            }

            lock (syncRoot)
            {
                return strictPath ? null : path;
            }
        }

        ///////////////////////////////////////////////////////////////////////

#if !NATIVE_UTILITY_BSTR
        private static int[] ToLengthArray(
            StringList list
            )
        {
            if (list == null)
                return null;

            int count = list.Count;
            int[] result = new int[count];

            for (int index = 0; index < count; index++)
            {
                string element = list[index];

                if (element == null)
                    continue;

                result[index] = element.Length;
            }

            return result;
        }
#endif

        ///////////////////////////////////////////////////////////////////////

        private static bool MaybeEnableReflection( /* NOT USED */
            bool? enable
            )
        {
            lock (syncRoot) /* TRANSACTIONAL */
            {
                //
                // NOTE: Always clear the cached FieldInfo.
                //
                itemsFieldInfo = null;

                //
                // NOTE: Does the caller want to force use of
                //       the FieldInfo to be enabled/disabled?
                //
                if (enable != null)
                {
                    //
                    // NOTE: Invert the parameter value passed
                    //       by the caller to set our private
                    //       "disable" flag.
                    //
                    noReflection = !(bool)enable;
                }

                //
                // NOTE: Return the existing (or new) value of
                //       our private "disable" flag.
                //
                return noReflection;
            }
        }

        ///////////////////////////////////////////////////////////////////////

        private static string[] ToStringArray(
            StringList list
            )
        {
            lock (syncRoot) /* TRANSACTIONAL */
            {
                if (noReflection)
                {
                    if (list == null)
                        return null;

                    return list.ToArray();
                }
                else
                {
                    return ArrayOps.GetArray<string>(
                        list, true, ref itemsFieldInfo);
                }
            }
        }

        ///////////////////////////////////////////////////////////////////////

        private static void InitializeNativeDelegates(
            bool clear
            )
        {
            lock (syncRoot) /* TRANSACTIONAL */
            {
                if (nativeDelegates == null)
                    nativeDelegates = new TypeDelegateDictionary();
                else if (clear)
                    nativeDelegates.Clear();

                nativeDelegates.Add(typeof(Eagle_GetVersion), null);
                nativeDelegates.Add(typeof(Eagle_FreeVersion), null);
                nativeDelegates.Add(typeof(Eagle_AllocateMemory), null);
                nativeDelegates.Add(typeof(Eagle_FreeMemory), null);
                nativeDelegates.Add(typeof(Eagle_FreeElements), null);
                nativeDelegates.Add(typeof(Eagle_SplitList), null);
                nativeDelegates.Add(typeof(Eagle_JoinList), null);
                nativeDelegates.Add(typeof(Eagle_SetMemoryHeap), null);

                if (nativeOptional == null)
                    nativeOptional = new TypeBoolDictionary();
                else if (clear)
                    nativeOptional.Clear();

                nativeOptional.Add(typeof(Eagle_SetMemoryHeap), true);
            }
        }

        ///////////////////////////////////////////////////////////////////////

        private static void UnsetNativeDelegates()
        {
            lock (syncRoot) /* TRANSACTIONAL */
            {
                nativeGetVersion = null;
                nativeFreeVersion = null;
                nativeAllocateMemory = null;
                nativeFreeMemory = null;
                nativeFreeElements = null;
                nativeSplitList = null;
                nativeJoinList = null;
                nativeSetMemoryHeap = null;

                /* NO RESULT */
                RuntimeOps.UnsetNativeDelegates(
                    nativeDelegates, nativeOptional);
            }
        }

        ///////////////////////////////////////////////////////////////////////

        private static bool SetNativeDelegates(
            ref Result error
            )
        {
            lock (syncRoot) /* TRANSACTIONAL */
            {
                if ((RuntimeOps.SetNativeDelegates(
                        "utility API", nativeModule, nativeDelegates,
                        nativeOptional, ref error) == ReturnCode.Ok) &&
                    (nativeDelegates != null))
                {
                    try
                    {
                        nativeGetVersion = (Eagle_GetVersion)
                            nativeDelegates[typeof(Eagle_GetVersion)];

                        nativeFreeVersion = (Eagle_FreeVersion)
                            nativeDelegates[typeof(Eagle_FreeVersion)];

                        nativeAllocateMemory = (Eagle_AllocateMemory)
                            nativeDelegates[typeof(Eagle_AllocateMemory)];

                        nativeFreeMemory = (Eagle_FreeMemory)
                            nativeDelegates[typeof(Eagle_FreeMemory)];

                        nativeFreeElements = (Eagle_FreeElements)
                            nativeDelegates[typeof(Eagle_FreeElements)];

                        nativeSplitList = (Eagle_SplitList)
                            nativeDelegates[typeof(Eagle_SplitList)];

                        nativeJoinList = (Eagle_JoinList)
                            nativeDelegates[typeof(Eagle_JoinList)];

                        nativeSetMemoryHeap = (Eagle_SetMemoryHeap)
                            nativeDelegates[typeof(Eagle_SetMemoryHeap)];

                        return true;
                    }
                    catch (Exception e)
                    {
                        error = e;
                    }
                }
            }

            return false;
        }

        ///////////////////////////////////////////////////////////////////////

#if WINDOWS
        private static void AddExitedEventHandler()
        {
            if (!GlobalConfiguration.DoesValueExist(
                    "No_NativeUtility_Exited",
                    ConfigurationFlags.NativeUtility))
            {
                AppDomain appDomain = AppDomainOps.GetCurrent();

                if (appDomain != null)
                {
                    if (!AppDomainOps.IsDefault(appDomain))
                    {
                        appDomain.DomainUnload -= NativeUtility_Exited;
                        appDomain.DomainUnload += NativeUtility_Exited;
                    }
                    else
                    {
                        appDomain.ProcessExit -= NativeUtility_Exited;
                        appDomain.ProcessExit += NativeUtility_Exited;
                    }
                }
            }
        }

        ///////////////////////////////////////////////////////////////////////

        private static void RemoveExitedEventHandler()
        {
            AppDomain appDomain = AppDomainOps.GetCurrent();

            if (appDomain != null)
            {
                if (!AppDomainOps.IsDefault(appDomain))
                    appDomain.DomainUnload -= NativeUtility_Exited;
                else
                    appDomain.ProcessExit -= NativeUtility_Exited;
            }
        }

        ///////////////////////////////////////////////////////////////////////

        private static void NativeUtility_Exited(
            object sender,
            EventArgs e
            )
        {
            /* IGNORED */
            MaybeFinalizeNativeHeap();

            /* IGNORED */
            UnloadNativeLibrary(null);
        }

        ///////////////////////////////////////////////////////////////////////

        private static bool InitializeNativeHeap(
            ref Result error
            )
        {
            lock (syncRoot) /* TRANSACTIONAL */
            {
                if (!PlatformOps.IsWindowsOperatingSystem())
                    return true;

                if (nativeHeap != IntPtr.Zero)
                    return true;

                IntPtr newHeap = NativeOps.UnsafeNativeMethods.HeapCreate(
                    NativeOps.UnsafeNativeMethods.HEAP_NONE, heapInitialSize,
                    heapMaximumSize);

                if (newHeap != IntPtr.Zero)
                {
                    IntPtr setHeap = newHeap; /* NOT USED */

                    if (SetMemoryHeap(
                            ref setHeap, ref error) == ReturnCode.Ok)
                    {
                        nativeHeap = newHeap;
                        return true;
                    }
                }
                else
                {
                    int lastError = Marshal.GetLastWin32Error();

                    error = String.Format(
                        "HeapCreate() failed with error {0}: {1}",
                        lastError, NativeOps.GetErrorMessage(lastError));
                }

                return false;
            }
        }

        ///////////////////////////////////////////////////////////////////////

        private static bool CompactNativeHeap(
            ref Result error
            )
        {
            lock (syncRoot) /* TRANSACTIONAL */
            {
                if (!PlatformOps.IsWindowsOperatingSystem())
                    return true;

                if (nativeHeap == IntPtr.Zero)
                    return true;

                UIntPtr size = NativeOps.UnsafeNativeMethods.HeapCompact(
                    nativeHeap, NativeOps.UnsafeNativeMethods.HEAP_NONE);

                Interlocked.Increment(ref compactCount);

                if (size != UIntPtr.Zero)
                {
                    TraceOps.DebugTrace(String.Format(
                        "CompactNativeHeap: largest free block: {0} bytes",
                        size), typeof(NativeUtility).Name,
                        TracePriority.NativeDebug);

                    return true;
                }
                else
                {
                    int lastError = Marshal.GetLastWin32Error();

                    error = String.Format(
                        "HeapCompact() failed with error {0}: {1}",
                        lastError, NativeOps.GetErrorMessage(lastError));
                }

                return false;
            }
        }

        ///////////////////////////////////////////////////////////////////////

        private static bool FinalizeNativeHeap(
            ref Result error
            )
        {
            lock (syncRoot) /* TRANSACTIONAL */
            {
                if (!PlatformOps.IsWindowsOperatingSystem())
                    return true;

                if (nativeHeap == IntPtr.Zero)
                    return true;

                IntPtr setHeap = IntPtr.Zero; /* NOT USED */

                if (SetMemoryHeap(
                        ref setHeap, ref error) == ReturnCode.Ok)
                {
                    if (NativeOps.UnsafeNativeMethods.HeapDestroy(
                            nativeHeap))
                    {
                        nativeHeap = IntPtr.Zero;
                        return true;
                    }
                    else
                    {
                        int lastError = Marshal.GetLastWin32Error();

                        error = String.Format(
                            "HeapDestroy() failed with error {0}: {1}",
                            lastError, NativeOps.GetErrorMessage(lastError));
                    }
                }

                return false;
            }
        }

        ///////////////////////////////////////////////////////////////////////

        private static bool MaybeInitializeNativeHeap()
        {
            lock (syncRoot) /* TRANSACTIONAL */
            {
                if ((nativeUseHeapApi == null) || !(bool)nativeUseHeapApi)
                    return true;

                /* NO RESULT */
                AddExitedEventHandler();

                Result error = null;

                if (!InitializeNativeHeap(ref error))
                {
                    TraceOps.DebugTrace(String.Format(
                        "MaybeInitializeNativeHeap: native heap error: {0}",
                        FormatOps.WrapOrNull(error)),
                        typeof(NativeUtility).Name,
                        TracePriority.NativeError);

                    return false;
                }

                return true;
            }
        }

        ///////////////////////////////////////////////////////////////////////

        private static bool MaybeCompactNativeHeap()
        {
            lock (syncRoot) /* TRANSACTIONAL */
            {
                if ((nativeUseHeapApi == null) || !(bool)nativeUseHeapApi)
                    return true;

                if ((Interlocked.Increment(
                        ref maybeCompactCount) % compactEveryCount) != 0)
                {
                    return true;
                }

                Result error = null;

                if (!CompactNativeHeap(ref error))
                {
                    TraceOps.DebugTrace(String.Format(
                        "MaybeCompactNativeHeap: native heap error: {0}",
                        FormatOps.WrapOrNull(error)),
                        typeof(NativeUtility).Name,
                        TracePriority.NativeError);

                    return false;
                }

                return true;
            }
        }

        ///////////////////////////////////////////////////////////////////////

        private static bool MaybeFinalizeNativeHeap()
        {
            lock (syncRoot) /* TRANSACTIONAL */
            {
                if ((nativeUseHeapApi == null) || !(bool)nativeUseHeapApi)
                    return true;

                Result error = null;

                if (!FinalizeNativeHeap(ref error))
                {
                    TraceOps.DebugTrace(String.Format(
                        "MaybeFinalizeNativeHeap: native heap error: {0}",
                        FormatOps.WrapOrNull(error)),
                        typeof(NativeUtility).Name,
                        TracePriority.NativeError);

                    return false;
                }

                /* NO RESULT */
                RemoveExitedEventHandler();

                return true;
            }
        }
#endif

        ///////////////////////////////////////////////////////////////////////

        private static bool LoadNativeLibrary(
            Interpreter interpreter
            )
        {
            lock (syncRoot) /* TRANSACTIONAL */
            {
                if (nativeModule != IntPtr.Zero)
                    return true;

                try
                {
                    Result error; /* REUSED */
                    string fileName;

                    error = null;

                    fileName = GetNativeLibraryFileName(
                        interpreter, ref error);

                    if (!String.IsNullOrEmpty(fileName))
                    {
                        TraceOps.DebugTrace(String.Format(
                            "LoadNativeLibrary: using file name {0}",
                            FormatOps.WrapOrNull(fileName)),
                            typeof(NativeUtility).Name,
                            TracePriority.NativeDebug2);
                    }
                    else
                    {
                        if (error == null)
                        {
                            error = String.Format(
                                "file name {0} is invalid",
                                FormatOps.WrapOrNull(fileName));
                        }

                        TraceOps.DebugTrace(String.Format(
                            "LoadNativeLibrary: {0}",
                            FormatOps.WrapOrNull(error)),
                            typeof(NativeUtility).Name,
                            TracePriority.NativeError);

                        return false;
                    }

                    //
                    // NOTE: Check if the native library file name actually
                    //       exists.  If not, do nothing and return failure
                    //       after tracing the issue.
                    //
                    if (!File.Exists(fileName))
                    {
                        TraceOps.DebugTrace(String.Format(
                            "LoadNativeLibrary: file name {0} does not exist",
                            FormatOps.WrapOrNull(fileName)),
                            typeof(NativeUtility).Name,
                            TracePriority.NativeError2);

                        return false;
                    }

                    //
                    // BUGFIX: Stop loading "untrusted" native libraries
                    //         when running with a "trusted" core library.
                    //
                    if (!RuntimeOps.ShouldTrustNativeLibrary(
                            interpreter, fileName))
                    {
                        TraceOps.DebugTrace(String.Format(
                            "LoadNativeLibrary: file name {0} is untrusted",
                            FormatOps.WrapOrNull(fileName)),
                            typeof(NativeUtility).Name,
                            TracePriority.NativeError);

                        return false;
                    }

                    int lastError;

                    nativeModule = NativeOps.LoadLibrary(
                        fileName, out lastError); /* throw */

                    if (nativeModule != IntPtr.Zero)
                    {
                        /* NO RESULT */
                        InitializeNativeDelegates(true);

                        error = null;

                        if (SetNativeDelegates(ref error))
                        {
                            nativeFileName = fileName;

                            TraceOps.DebugTrace(String.Format(
                                "LoadNativeLibrary: file name {0} " +
                                "successfully loaded",
                                FormatOps.WrapOrNull(fileName)),
                                typeof(NativeUtility).Name,
                                TracePriority.NativeDebug);

                            return true;
                        }
                        else
                        {
                            TraceOps.DebugTrace(String.Format(
                                "LoadNativeLibrary: file name {0} delegate " +
                                "setup error: {1}",
                                FormatOps.WrapOrNull(fileName),
                                FormatOps.WrapOrNull(error)),
                                typeof(NativeUtility).Name,
                                TracePriority.NativeError);

                            /* IGNORED */
                            UnloadNativeLibrary(interpreter);
                        }
                    }
                    else
                    {
                        error = NativeOps.GetDynamicLoadingError(
                            lastError);

                        if (error != null)
                            error = error.Trim();

                        TraceOps.DebugTrace(String.Format(
                            "LoadLibrary({1}) failed with error {0}: {2}",
                            lastError, FormatOps.WrapOrNull(fileName),
                            FormatOps.WrapOrNull(error)),
                            typeof(NativeUtility).Name,
                            TracePriority.NativeError);
                    }
                }
                catch (Exception e)
                {
                    TraceOps.DebugTrace(
                        e, typeof(NativeUtility).Name,
                        TracePriority.NativeError);
                }

                return false;
            }
        }

        ///////////////////////////////////////////////////////////////////////

        private static bool UnloadNativeLibrary(
            Interpreter interpreter /* NOT USED */
            )
        {
            lock (syncRoot) /* TRANSACTIONAL */
            {
                if (nativeModule == IntPtr.Zero)
                    return true;

                try
                {
#if WINDOWS
                    if (!MaybeFinalizeNativeHeap())
                        return false;
#endif

                    /* NO RESULT */
                    UnsetNativeDelegates();

                    int lastError;

                    if (NativeOps.FreeLibrary(
                            nativeModule, out lastError)) /* throw */
                    {
                        nativeModule = IntPtr.Zero;
                        nativeFileName = null;

                        TraceOps.DebugTrace(
                            "UnloadNativeLibrary: successfully unloaded",
                            typeof(NativeUtility).Name,
                            TracePriority.NativeDebug3);

                        return true;
                    }
                    else
                    {
                        Result error = NativeOps.GetDynamicLoadingError(
                            lastError);

                        if (error != null)
                            error = error.Trim();

                        TraceOps.DebugTrace(String.Format(
                            "FreeLibrary(0x{1:X}) " +
                            "failed with error {0}: {2}",
                            lastError, nativeModule,
                            FormatOps.WrapOrNull(error)),
                            typeof(NativeUtility).Name,
                            TracePriority.NativeError);
                    }
                }
                catch (Exception e)
                {
                    TraceOps.DebugTrace(
                        e, typeof(NativeUtility).Name,
                        TracePriority.NativeError);
                }

                return false;
            }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Public Methods
        #region Introspection Support Methods
        //
        // BUGFIX: *DEADLOCK* Prevent deadlocks here by using the TryLock
        //         pattern.
        //
        // NOTE: Used by the _Hosts.Default.WriteEngineInfo method.
        //
        public static void AddInfo(
            StringPairList list,
            DetailFlags detailFlags
            )
        {
            if (list == null)
                return;

            bool locked = false;

            try
            {
                TryLock(ref locked); /* TRANSACTIONAL */

                if (locked)
                {
                    bool empty = HostOps.HasEmptyContent(detailFlags);
                    StringPairList localList = new StringPairList();

                    if (empty || forceNonWindows)
                        localList.Add("ForceNonWindows", forceNonWindows.ToString());

#if MONO || MONO_HACKS
                    if (empty || forceMono)
                        localList.Add("ForceMono", forceMono.ToString());
#endif

                    if (empty || (isAvailable != null))
                        localList.Add("IsAvailable", (isAvailable != null) ?
                            isAvailable.ToString() : FormatOps.DisplayNull);

                    if (empty || locked)
                        localList.Add("Locked", locked.ToString());

                    if (empty || disabled)
                        localList.Add("Disabled", disabled.ToString());

                    if (empty || strictPath)
                        localList.Add("StrictPath", strictPath.ToString());

                    if (empty || noReflection)
                        localList.Add("NoReflection", noReflection.ToString());

#if WINDOWS
                    if (empty || (nativeUseHeapApi != null))
                        localList.Add("NativeUseHeapApi", (nativeUseHeapApi != null) ?
                            nativeUseHeapApi.ToString() : FormatOps.DisplayNull);
#endif

                    if (empty || (nativeModule != IntPtr.Zero))
                        localList.Add("NativeModule", nativeModule.ToString());

                    if (empty || (nativeFileName != null))
                        localList.Add("NativeFileName", (nativeFileName != null) ?
                            nativeFileName : FormatOps.DisplayNull);

#if WINDOWS
                    if (empty || (nativeHeap != IntPtr.Zero))
                        localList.Add("NativeHeap", nativeHeap.ToString());

                    if (empty || (heapInitialSize != UIntPtr.Zero))
                        localList.Add("HeapInitialSize", heapInitialSize.ToString());

                    if (empty || (heapMaximumSize != UIntPtr.Zero))
                        localList.Add("HeapMaximumSize", heapMaximumSize.ToString());
#endif

                    if (empty || ((nativeDelegates != null) && (nativeDelegates.Count > 0)))
                        localList.Add("NativeDelegates", (nativeDelegates != null) ?
                            nativeDelegates.Count.ToString() : FormatOps.DisplayNull);

                    if (empty || ((nativeOptional != null) && (nativeOptional.Count > 0)))
                        localList.Add("NativeOptional", (nativeOptional != null) ?
                            nativeOptional.Count.ToString() : FormatOps.DisplayNull);

                    if (empty || (nativeGetVersion != null))
                        localList.Add("NativeGetVersion", (nativeGetVersion != null) ?
                            nativeGetVersion.ToString() : FormatOps.DisplayNull);

                    if (empty || (nativeFreeVersion != null))
                        localList.Add("NativeFreeVersion", (nativeFreeVersion != null) ?
                            nativeFreeVersion.ToString() : FormatOps.DisplayNull);

                    if (empty || (nativeAllocateMemory != null))
                        localList.Add("NativeAllocateMemory", (nativeAllocateMemory != null) ?
                            nativeAllocateMemory.ToString() : FormatOps.DisplayNull);

                    if (empty || (nativeFreeMemory != null))
                        localList.Add("NativeFreeMemory", (nativeFreeMemory != null) ?
                            nativeFreeMemory.ToString() : FormatOps.DisplayNull);

                    if (empty || (nativeFreeElements != null))
                        localList.Add("NativeFreeElements", (nativeFreeElements != null) ?
                            nativeFreeElements.ToString() : FormatOps.DisplayNull);

                    if (empty || (nativeSplitList != null))
                        localList.Add("NativeSplitList", (nativeSplitList != null) ?
                            nativeSplitList.ToString() : FormatOps.DisplayNull);

                    if (empty || (nativeJoinList != null))
                        localList.Add("NativeJoinList", (nativeJoinList != null) ?
                            nativeJoinList.ToString() : FormatOps.DisplayNull);

                    if (empty || (nativeSetMemoryHeap != null))
                        localList.Add("NativeSetMemoryHeap", (nativeSetMemoryHeap != null) ?
                            nativeSetMemoryHeap.ToString() : FormatOps.DisplayNull);

                    if (empty || (version != null))
                        localList.Add("Version", (version != null) ?
                            version : FormatOps.DisplayNull);

                    string localVersion = GetVersion();

                    if (empty || (localVersion != null))
                        localList.Add("GetVersion", (localVersion != null) ?
                            localVersion : FormatOps.DisplayNull);

                    if (empty || (itemsFieldInfo != null))
                        localList.Add("ItemsFieldInfo", (itemsFieldInfo != null) ?
                            itemsFieldInfo.ToString() : FormatOps.DisplayNull);

                    long localSplitCount = Interlocked.CompareExchange(
                        ref splitCount, 0, 0);

                    if (empty || (localSplitCount > 0))
                        localList.Add("SplitCount", localSplitCount.ToString());

                    long localJoinCount = Interlocked.CompareExchange(
                        ref joinCount, 0, 0);

                    if (empty || (localJoinCount > 0))
                        localList.Add("JoinCount", localJoinCount.ToString());

#if WINDOWS
                    long localCompactCount = Interlocked.CompareExchange(
                        ref compactCount, 0, 0);

                    if (empty || (localCompactCount > 0))
                        localList.Add("CompactCount", localCompactCount.ToString());

                    long localMaybeCompactCount = Interlocked.CompareExchange(
                        ref maybeCompactCount, 0, 0);

                    if (empty || (localMaybeCompactCount > 0))
                        localList.Add("MaybeCompactCount", localMaybeCompactCount.ToString());
#endif

                    if (localList.Count > 0)
                    {
                        list.Add((IPair<string>)null);
                        list.Add("Native Utility");
                        list.Add((IPair<string>)null);
                        list.Add(localList);
                    }
                }
                else
                {
                    StringPairList localList = new StringPairList();

                    localList.Add(FormatOps.DisplayBusy);

                    if (localList.Count > 0)
                    {
                        list.Add((IPair<string>)null);
                        list.Add("Native Utility");
                        list.Add((IPair<string>)null);
                        list.Add(localList);
                    }
                }
            }
            finally
            {
                ExitLock(ref locked); /* TRANSACTIONAL */
            }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        public static void TryLock(
            ref bool locked
            )
        {
            if (syncRoot == null)
                return;

            locked = Monitor.TryEnter(syncRoot);
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
                Monitor.Exit(syncRoot);
                locked = false;
            }
        }

        ///////////////////////////////////////////////////////////////////////

        private static string GetVersion()
        {
            bool locked = false;

            try
            {
                TryLock(ref locked); /* TRANSACTIONAL */

                if (locked)
                {
                    if ((nativeFreeVersion != null) &&
                        (nativeGetVersion != null))
                    {
                        IntPtr pVersion = IntPtr.Zero;

                        try
                        {
                            pVersion = nativeGetVersion();

                            if (pVersion != IntPtr.Zero)
                                return Marshal.PtrToStringUni(pVersion);
                        }
                        catch (Exception e)
                        {
                            TraceOps.DebugTrace(
                                e, typeof(NativeUtility).Name,
                                TracePriority.NativeError);
                        }
                        finally
                        {
                            if (pVersion != IntPtr.Zero)
                            {
                                nativeFreeVersion(pVersion);
                                pVersion = IntPtr.Zero;
                            }
                        }
                    }
                }

                return null;
            }
            finally
            {
                ExitLock(ref locked); /* TRANSACTIONAL */
            }
        }

        ///////////////////////////////////////////////////////////////////////

        private static bool IsDisabled(
            Interpreter interpreter
            )
        {
            Interpreter localInterpreter = interpreter;

            if (localInterpreter == null)
                localInterpreter = Interpreter.GetActive();

            if ((localInterpreter != null) && FlagOps.HasFlags(
                    localInterpreter.CreateFlagsNoLock,
                    CreateFlags.NoNativeUtility, true))
            {
                return true;
            }

            if (GlobalConfiguration.DoesValueExist(
                    EnvVars.NoNativeUtility, GlobalConfiguration.GetFlags(
                    ConfigurationFlags.NativeUtility, Interpreter.IsVerbose(
                    localInterpreter))))
            {
                return true;
            }

            return false;
        }

        ///////////////////////////////////////////////////////////////////////

        public static bool IsAvailable(
            Interpreter interpreter /* OPTIONAL */
            )
        {
            lock (syncRoot) /* TRANSACTIONAL */
            {
                try
                {
                    if (isAvailable == null)
                    {
                        //
                        // NOTE: If loading the native utility library has
                        //       been temporarily locked out, return false to
                        //       indicate that it is temporarily unavailable.
                        //       Do nothing else.  That way, it may become
                        //       available later after being unlocked.
                        //
                        if (locked)
                            return false;

                        //
                        // NOTE: If loading the native utility library has
                        //       been prohibited, mark it as "permanently"
                        //       unavailable and return now.
                        //
                        if (IsDisabled(interpreter))
                        {
                            disabled = true; /* INFORMATIONAL */
                            return (bool)(isAvailable = false);
                        }

                        ///////////////////////////////////////////////////////

#if MONO || MONO_HACKS
                        //
                        // HACK: *MONO* When running on Mono, attempting to
                        //       use the native utility library crashes, for
                        //       reasons that are unclear.
                        //
                        if (!forceMono && CommonOps.Runtime.IsMono())
                        {
                            TraceOps.DebugTrace(
                                "IsAvailable: detected Mono runtime, forced " +
                                "unavailable", typeof(NativeUtility).Name,
                                TracePriority.NativeDebug);

                            return (bool)(isAvailable = false);
                        }
#endif

                        ///////////////////////////////////////////////////////

                        //
                        // NOTE: If loading the native utility library fails,
                        //       mark it as "permanently" unavailable.  This
                        //       must be done; otherwise, we will try to load
                        //       it everytime a list needs to be joined or
                        //       split, potentially slowing things down rather
                        //       significantly.
                        //
                        if (!LoadNativeLibrary(interpreter))
                            return (bool)(isAvailable = false);

                        ///////////////////////////////////////////////////////

                        if ((nativeFreeVersion != null) &&
                            (nativeGetVersion != null))
                        {
                            IntPtr pVersion = IntPtr.Zero;

                            try
                            {
                                pVersion = nativeGetVersion();

                                if (pVersion != IntPtr.Zero)
                                {
                                    version = Marshal.PtrToStringUni(
                                        pVersion);

                                    bool useHeapApi;

                                    if (IsUsable(
                                            version, Build.Debug,
                                            out useHeapApi))
                                    {
#if WINDOWS
                                        //
                                        // HACK: Unless somebody (?) already
                                        //       set the "UseHeapApi" flag,
                                        //       do that now.
                                        //
                                        if (nativeUseHeapApi == null)
                                            nativeUseHeapApi = useHeapApi;

                                        //
                                        // NOTE: If applicable, enable usage
                                        //       of the native Win32 API for
                                        //       heap management.
                                        //
                                        if (MaybeInitializeNativeHeap())
#endif
                                        {
                                            ParserOpsData.EnableNative(true);
                                            isAvailable = true;
                                        }
#if WINDOWS
                                        else
                                        {
                                            version = null;
                                            isAvailable = false;
                                        }
#endif
                                    }
                                    else
                                    {
                                        version = null;
                                        isAvailable = false;
                                    }
                                }
                                else
                                {
                                    version = null;
                                    isAvailable = false;
                                }
                            }
                            catch
                            {
                                //
                                // NOTE: Prevent an exception during the native
                                //       function call from causing this check
                                //       to be repeated [forever] in the future.
                                //
                                version = null;
                                isAvailable = false;

                                //
                                // NOTE: Next, re-throw the exception (i.e. to
                                //       be caught by the outer catch block).
                                //
                                throw;
                            }
                            finally
                            {
                                if (pVersion != IntPtr.Zero)
                                {
                                    nativeFreeVersion(pVersion);
                                    pVersion = IntPtr.Zero;
                                }
                            }
                        }
                        else
                        {
                            TraceOps.DebugTrace(String.Format(
                                "IsAvailable: one or more required " +
                                "functions are unavailable: {0} or {1}",
                                typeof(Eagle_FreeVersion).Name,
                                typeof(Eagle_GetVersion).Name),
                                typeof(NativeUtility).Name,
                                TracePriority.NativeError);

                            version = null;
                            isAvailable = false;
                        }
                    }

                    return (bool)isAvailable;
                }
                catch (Exception e)
                {
                    TraceOps.DebugTrace(
                        e, typeof(NativeUtility).Name,
                        TracePriority.NativeError);
                }
            }

            return false;
        }

        ///////////////////////////////////////////////////////////////////////

        private static bool ResetAvailable( /* NOT USED */
            Interpreter interpreter,
            bool? available,
            bool unload,
            bool unlock
            )
        {
            try
            {
                lock (syncRoot) /* TRANSACTIONAL */
                {
                    if (unload && !UnloadNativeLibrary(interpreter))
                        return false;

                    if (unlock)
                        locked = false;

                    disabled = false; /* INFORMATIONAL */
                    isAvailable = available;
                    version = null;

                    return true;
                }
            }
            catch (Exception e)
            {
                TraceOps.DebugTrace(
                    e, typeof(NativeUtility).Name,
                    TracePriority.NativeError);
            }

            return false;
        }

        ///////////////////////////////////////////////////////////////////////

        //
        // BUGFIX: *DEADLOCK* Prevent deadlocks here by using the TryLock
        //         pattern.
        //
        public static string GetVersion(
            Interpreter interpreter
            )
        {
            bool locked = false;

            try
            {
                TryLock(ref locked); /* TRANSACTIONAL */

                if (locked)
                {
                    if (IsAvailable(interpreter))
                        return version;
                    else if (disabled)
                        return "disabled";
                    else
                        return "unavailable";
                }
                else
                {
                    return "locked";
                }
            }
            finally
            {
                ExitLock(ref locked); /* TRANSACTIONAL */
            }
        }

        ///////////////////////////////////////////////////////////////////////

        public static ReturnCode SplitList(
            string text,
            ref StringList list,
            ref Result error
            )
        {
            if (text == null)
            {
                error = "invalid text";
                return ReturnCode.Error;
            }

            lock (syncRoot) /* TRANSACTIONAL */
            {
                if ((nativeFreeMemory != null) &&
                    (nativeFreeElements != null) &&
                    (nativeSplitList != null))
                {
                    int elementCount = 0;
                    IntPtr pElementLengths = IntPtr.Zero;
                    IntPtr ppElements = IntPtr.Zero;
                    IntPtr pError = IntPtr.Zero;

                    try
                    {
                        ReturnCode code = nativeSplitList(
                            text.Length, text, ref elementCount,
                            ref pElementLengths, ref ppElements,
                            ref pError);

                        Interlocked.Increment(ref splitCount);

                        if (code != ReturnCode.Ok)
                        {
                            error = Marshal.PtrToStringUni(pError);
                            return code;
                        }

                        if (elementCount < 0)
                        {
                            error = String.Format(
                                "bad number of elements in list: {0}",
                                elementCount);

                            return ReturnCode.Error;
                        }

                        if (list != null)
                            list.Capacity += elementCount;
                        else
                            list = new StringList(elementCount);

                        for (int index = 0; index < elementCount; index++)
                        {
                            int elementOffset = index * IntPtr.Size;

                            if (elementOffset < 0)
                            {
                                error = String.Format(
                                    "bad list element {0} offset: {1}",
                                    index, elementOffset);

                                return ReturnCode.Error;
                            }

                            IntPtr pElement = Marshal.ReadIntPtr(
                                ppElements, elementOffset);

                            if (pElement == IntPtr.Zero)
                            {
                                list.Add(String.Empty);
                                continue;
                            }

                            int lengthOffset = index * sizeof(int);

                            if (lengthOffset < 0)
                            {
                                error = String.Format(
                                    "bad list element length {0} offset: {1}",
                                    index, lengthOffset);

                                return ReturnCode.Error;
                            }

                            int elementLength = Marshal.ReadInt32(
                                pElementLengths, lengthOffset);

                            if (elementLength < 0)
                            {
                                error = String.Format(
                                    "bad number of characters in list element: {0}",
                                    elementLength);

                                return ReturnCode.Error;
                            }

                            list.Add(Marshal.PtrToStringUni(pElement,
                                elementLength));
                        }

                        return ReturnCode.Ok;
                    }
                    catch (Exception e)
                    {
                        error = e;
                    }
                    finally
                    {
                        #region Free Error String
                        if (pError != IntPtr.Zero)
                        {
                            nativeFreeMemory(pError);
                            pError = IntPtr.Zero;
                        }
                        #endregion

                        ///////////////////////////////////////////////////////

                        #region Free Element Array
                        if (ppElements != IntPtr.Zero)
                        {
                            nativeFreeElements(elementCount, ppElements);
                            ppElements = IntPtr.Zero;
                            elementCount = 0;
                        }
                        #endregion

                        ///////////////////////////////////////////////////////

                        #region Free Element Lengths Array
                        if (pElementLengths != IntPtr.Zero)
                        {
                            nativeFreeMemory(pElementLengths);
                            pElementLengths = IntPtr.Zero;
                        }
                        #endregion

                        ///////////////////////////////////////////////////////

                        #region Maybe Compact Native Heap
#if WINDOWS
                        /* IGNORED */
                        MaybeCompactNativeHeap();
#endif
                        #endregion
                    }
                }
                else
                {
                    error = String.Format(
                        "one or more required functions are unavailable: " +
                        "{0}, {1}, or {2}", typeof(Eagle_FreeMemory).Name,
                        typeof(Eagle_FreeElements).Name,
                        typeof(Eagle_SplitList).Name);
                }
            }

            return ReturnCode.Error;
        }

        ///////////////////////////////////////////////////////////////////////

        public static ReturnCode JoinList(
            StringList list,
            ref string text,
            ref Result error
            )
        {
            if (list == null)
            {
                error = "invalid list";
                return ReturnCode.Error;
            }

            lock (syncRoot) /* TRANSACTIONAL */
            {
                if ((nativeFreeMemory != null) && (nativeJoinList != null))
                {
                    IntPtr pText = IntPtr.Zero;
                    IntPtr pError = IntPtr.Zero;

                    try
                    {
                        int length = 0;

#if NATIVE_UTILITY_BSTR
                        ReturnCode code = nativeJoinList(
                            list.Count, null, ToStringArray(list),
                            ref length, ref pText, ref pError);
#else
                        ReturnCode code = nativeJoinList(
                            list.Count, ToLengthArray(list),
                            ToStringArray(list), ref length,
                            ref pText, ref pError);
#endif

                        Interlocked.Increment(ref joinCount);

                        if (code != ReturnCode.Ok)
                        {
                            error = Marshal.PtrToStringUni(pError);
                            return code;
                        }

                        if (length < 0)
                        {
                            error = String.Format(
                                "bad number of characters in string: {0}",
                                length);

                            return ReturnCode.Error;
                        }

                        text = Marshal.PtrToStringUni(pText, length);
                        return ReturnCode.Ok;
                    }
                    catch (Exception e)
                    {
                        error = e;
                    }
                    finally
                    {
                        #region Free Error String
                        if (pError != IntPtr.Zero)
                        {
                            nativeFreeMemory(pError);
                            pError = IntPtr.Zero;
                        }
                        #endregion

                        ///////////////////////////////////////////////////////

                        #region Free Text String
                        if (pText != IntPtr.Zero)
                        {
                            nativeFreeMemory(pText);
                            pText = IntPtr.Zero;
                        }
                        #endregion

                        ///////////////////////////////////////////////////////

                        #region Maybe Compact Native Heap
#if WINDOWS
                        /* IGNORED */
                        MaybeCompactNativeHeap();
#endif
                        #endregion
                    }
                }
                else
                {
                    error = String.Format(
                        "one or more required functions are unavailable: " +
                        "{0} or {1}", typeof(Eagle_FreeMemory).Name,
                        typeof(Eagle_JoinList).Name);
                }
            }

            return ReturnCode.Error;
        }

        ///////////////////////////////////////////////////////////////////////

        private static ReturnCode SetMemoryHeap(
            ref IntPtr newHeap,
            ref Result error
            )
        {
            lock (syncRoot) /* TRANSACTIONAL */
            {
                if (nativeSetMemoryHeap != null)
                {
                    try
                    {
                        IntPtr oldHeap = nativeSetMemoryHeap(newHeap);

                        TraceOps.DebugTrace(String.Format(
                            "SetMemoryHeap: changed from 0x{0:X} to 0x{1:X}",
                            oldHeap.ToInt64(), newHeap.ToInt64()),
                            typeof(NativeUtility).Name,
                            TracePriority.NativeDebug);

                        return ReturnCode.Ok;
                    }
                    catch (Exception e)
                    {
                        error = e;
                    }
                }
                else
                {
                    error = String.Format(
                        "one or more required functions are unavailable: " +
                        "{0}", typeof(Eagle_SetMemoryHeap).Name);
                }
            }

            return ReturnCode.Error;
        }
        #endregion
    }
}
