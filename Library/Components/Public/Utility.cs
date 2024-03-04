/*
 * Utility.cs --
 *
 * Copyright (c) 2007-2012 by Joe Mistachkin.  All rights reserved.
 *
 * See the file "license.terms" for information on usage and redistribution of
 * this file, and for a DISCLAIMER OF ALL WARRANTIES.
 *
 * RCS: @(#) $Id: $
 */

///////////////////////////////////////////////////////////////////////////////
// *WARNING* *WARNING* *WARNING* *WARNING* *WARNING* *WARNING* *WARNING*
//
// Please do not use this class from within the Eagle core library itself.
// Instead, the various internal methods used by this class should be called
// directly.  This class is intended only for use by third-party plugins and
// applications.
//
// *WARNING* *WARNING* *WARNING* *WARNING* *WARNING* *WARNING* *WARNING*
///////////////////////////////////////////////////////////////////////////////

using System;
using System.Collections;
using System.Collections.Generic;

#if NETWORK
using System.Collections.Specialized;
#endif

#if DATA
using System.Data;
#endif

using System.Diagnostics;
using System.Globalization;
using System.IO;

#if NETWORK
using System.Net.Sockets;
#endif

using System.Reflection;
using System.Resources;
using System.Runtime.CompilerServices;

#if !NET_STANDARD_20
using System.Security.AccessControl;
#endif

using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading;

#if XML
using System.Xml;
#endif

#if XML && SERIALIZATION
using System.Xml.Serialization;
#endif

#if WINFORMS
using System.Windows.Forms;
#endif

using Eagle._Attributes;
using Eagle._Components.Private;
using Eagle._Components.Public.Delegates;
using Eagle._Constants;
using Eagle._Containers.Public;
using Eagle._Interfaces.Public;
using _RuntimeOps = Eagle._Components.Private.RuntimeOps;
using SharedAttributeOps = Eagle._Components.Shared.AttributeOps;
using SharedStringOps = Eagle._Components.Shared.StringOps;
using _StringDictionary = Eagle._Containers.Public.StringDictionary;

using ActiveInterpreterPair = Eagle._Interfaces.Public.IAnyPair<
    Eagle._Components.Public.Interpreter, Eagle._Interfaces.Public.IClientData>;

using AssemblyFilePluginNames = System.Collections.Generic.Dictionary<
    string, Eagle._Containers.Public.StringList>;

namespace Eagle._Components.Public
{
    [ObjectId("702cb2b3-5e60-4f90-b5af-df09c236ef51")]
    public static class Utility /* FOR EXTERNAL USE ONLY */
    {
        #region External Use Only Helper Methods
        public static string GetManagedExecutableName()
        {
            return PathOps.GetManagedExecutableName();
        }

        ///////////////////////////////////////////////////////////////////////

        [Obsolete()]
        public static void TryGlobalLock( /* Trust me, you don't need this. */
            int timeout,
            ref bool locked
            )
        {
            GlobalState.TryLock(timeout, ref locked);
        }

        ///////////////////////////////////////////////////////////////////////

        [Obsolete()]
        public static void ExitGlobalLock( /* You don't need this either. */
            ref bool locked
            )
        {
            GlobalState.ExitLock(ref locked);
        }

        ///////////////////////////////////////////////////////////////////////

        public static void PushActiveInterpreter(
            Interpreter interpreter
            )
        {
            GlobalState.PushActiveInterpreter(interpreter);
        }

        ///////////////////////////////////////////////////////////////////////

        public static void PushActiveInterpreter(
            Interpreter interpreter,
            IClientData clientData
            )
        {
            GlobalState.PushActiveInterpreter(interpreter, clientData);
        }

        ///////////////////////////////////////////////////////////////////////

        public static void MaybePushActiveLogClientData(
            Interpreter interpreter,
            IClientData clientData,
            ref int pushed
            )
        {
            GlobalState.MaybePushActiveLogClientData(
                interpreter, clientData, ref pushed);
        }

        ///////////////////////////////////////////////////////////////////////

        public static ActiveInterpreterPair MaybePopActiveLogClientData(
            ref int pushed
            ) /* THREAD-SAFE */
        {
            return GlobalState.MaybePopActiveLogClientData(null, ref pushed);
        }

        ///////////////////////////////////////////////////////////////////////

        public static ActiveInterpreterPair PeekActiveInterpreter()
        {
            return GlobalState.PeekActiveInterpreter();
        }

        ///////////////////////////////////////////////////////////////////////

        public static void PopActiveInterpreter()
        {
            GlobalState.PopActiveInterpreter();
        }

        ///////////////////////////////////////////////////////////////////////

        public static bool IsSameType(
            Type type1,
            Type type2
            )
        {
            return MarshalOps.IsSameType(type1, type2);
        }

        ///////////////////////////////////////////////////////////////////////

        public static string MemberNameToEntityName(
            string name,
            bool noCase
            )
        {
            return ScriptOps.MemberNameToEntityName(name, noCase);
        }

        ///////////////////////////////////////////////////////////////////////

        public static void ExtractWords(
            long value,
            ref long highWord,
            ref long highMidWord,
            ref long lowMidWord,
            ref long lowWord
            )
        {
            ConversionOps.UnmakeLong(
                value, ref highWord, ref highMidWord, ref lowMidWord,
                ref lowWord);
        }

        ///////////////////////////////////////////////////////////////////////

        public static void ExtractWords(
            long value,
            ref int highWord,
            ref int lowWord
            )
        {
            ConversionOps.ToInts(
                value, ref highWord, ref lowWord);
        }

        ///////////////////////////////////////////////////////////////////////

        public static EventWaitHandle CreateNamedEvent(
            bool initialState,
            EventResetMode mode,
            string name,
            out bool createdNew
            )
        {
            return ThreadOps.CreateEvent(
                initialState, mode, name, out createdNew);
        }

        ///////////////////////////////////////////////////////////////////////

#if !NET_STANDARD_20
        public static EventWaitHandle CreateNamedEvent(
            bool initialState,
            EventResetMode mode,
            string name,
            out bool createdNew,
            EventWaitHandleSecurity eventSecurity
            )
        {
            return ThreadOps.CreateEvent(
                initialState, mode, name, out createdNew,
                eventSecurity);
        }
#endif

        ///////////////////////////////////////////////////////////////////////

        public static EventWaitHandle OpenNamedEvent(
            string name
            )
        {
            return ThreadOps.OpenEvent(name);
        }

        ///////////////////////////////////////////////////////////////////////

        public static void CloseNamedEvent(
            ref EventWaitHandle @event
            )
        {
            ThreadOps.CloseEvent(ref @event);
        }

        ///////////////////////////////////////////////////////////////////////

        public static FieldInfo GetFieldInfo(
            Type type,
            string name,
            BindingFlags bindingFlags,
            ref Result error
            )
        {
            return SettingsOps.GetFieldInfo(
                type, name, bindingFlags, ref error);
        }

        ///////////////////////////////////////////////////////////////////////

        public static PropertyInfo GetPropertyInfo(
            Type type,
            string name,
            BindingFlags bindingFlags,
            bool canRead,
            bool canWrite,
            ref Result error
            )
        {
            return SettingsOps.GetPropertyInfo(
                type, name, bindingFlags, canRead, canWrite, ref error);
        }

        ///////////////////////////////////////////////////////////////////////

        public static ReturnCode GetFieldValue(
            Type type,
            string name,
            BindingFlags bindingFlags,
            object @object,
            ref object value,
            ref Result error
            )
        {
            return SettingsOps.GetFieldValue(
                type, name, bindingFlags, @object, ref value, ref error);
        }

        ///////////////////////////////////////////////////////////////////////

        public static ReturnCode SetFieldValue(
            Type type,
            string name,
            BindingFlags bindingFlags,
            object @object,
            object value,
            ref Result error
            )
        {
            return SettingsOps.SetFieldValue(
                type, name, bindingFlags, @object, value, ref error);
        }

        ///////////////////////////////////////////////////////////////////////

        public static ReturnCode GetPropertyValue(
            Type type,
            string name,
            BindingFlags bindingFlags,
            object @object,
            ref object value,
            ref Result error
            )
        {
            return SettingsOps.GetPropertyValue(
                type, name, bindingFlags, @object, ref value, ref error);
        }

        ///////////////////////////////////////////////////////////////////////

        public static ReturnCode SetPropertyValue(
            Type type,
            string name,
            BindingFlags bindingFlags,
            object @object,
            object value,
            ref Result error
            )
        {
            return SettingsOps.SetPropertyValue(
                type, name, bindingFlags, @object, value, ref error);
        }

        ///////////////////////////////////////////////////////////////////////

        public static long GetPerformanceCount()
        {
            return PerformanceOps.GetCount();
        }

        ///////////////////////////////////////////////////////////////////////

        public static double GetPerformanceMicroseconds(
            long startCount,
            long stopCount
            )
        {
            return PerformanceOps.GetMicrosecondsFromCount(
                startCount, stopCount);
        }

        ///////////////////////////////////////////////////////////////////////

        public static int GetHashCode(
            object value
            )
        {
            return _RuntimeOps.GetHashCode(value);
        }

        ///////////////////////////////////////////////////////////////////////

#if NETWORK
        public static Socket GetSocket(
            NetworkStream stream
            )
        {
            return SocketOps.GetSocket(stream);
        }

        ///////////////////////////////////////////////////////////////////////

        public static bool IsSocketCleanedUp(
            Socket socket,
            bool @default
            )
        {
            return SocketOps.IsCleanedUp(socket, @default);
        }

        ///////////////////////////////////////////////////////////////////////

        public static bool IsTcpListenerActive(
            TcpListener listener,
            bool @default
            )
        {
            return SocketOps.IsListenerActive(listener, @default);
        }

        ///////////////////////////////////////////////////////////////////////

        public static ReturnCode DownloadAndExtractZipFile(
            Interpreter interpreter,
            IClientData clientData,
            string extractDirectory,
            string resourceName,
            bool? useFallback,
            ref Result error
            )
        {
            return ScriptOps.DownloadAndExtractZipFile(
                interpreter, clientData, extractDirectory,
                resourceName, useFallback, ref error);
        }

        ///////////////////////////////////////////////////////////////////////

        public static ReturnCode ExtractZipFileToDirectory(
            Interpreter interpreter,
            IClientData clientData,
            string downloadDirectory,
            string downloadFileName,
            string extractDirectory,
            EventFlags? eventFlags,
            bool? useFallback,
            ref Result error
            )
        {
            return ScriptOps.ExtractZipFileToDirectory(
                interpreter, clientData, downloadDirectory,
                downloadFileName, extractDirectory, eventFlags,
                useFallback, ref error);
        }

        ///////////////////////////////////////////////////////////////////////

#if NETWORK
        public static bool InOfflineMode()
        {
            return WebOps.InOfflineMode();
        }

        ///////////////////////////////////////////////////////////////////////

        public static void SetOfflineMode(
            bool offline
            )
        {
            WebOps.SetOfflineMode(offline);
        }
#endif

        ///////////////////////////////////////////////////////////////////////

#if TEST
        public static ReturnCode SetWebSecurityProtocol(
            bool obsolete,
            ref Result error
            )
        {
            return WebOps.SetSecurityProtocol(obsolete, ref error);
        }
#endif
#endif

        ///////////////////////////////////////////////////////////////////////

#if SHELL
        public static void StartupShellMain() /* System.CrossAppDomainDelegate */
        {
            ShellOps.StartupShellMain();
        }

        ///////////////////////////////////////////////////////////////////////

        public static void StartupInteractiveLoop() /* System.CrossAppDomainDelegate */
        {
            ShellOps.StartupInteractiveLoop();
        }
#endif

        ///////////////////////////////////////////////////////////////////////

#if NATIVE && WINDOWS
        public static object GetDefaultAppDomain()
        {
            return AppDomainOps.GetDefault();
        }
#endif

        ///////////////////////////////////////////////////////////////////////

        public static string GetAssemblyLocation()
        {
            return GlobalState.GetAssemblyLocation();
        }

        ///////////////////////////////////////////////////////////////////////

        public static bool IsFileStrongNameVerified(
            string fileName
            )
        {
            return _RuntimeOps.IsStrongNameVerified(fileName, true);
        }

        ///////////////////////////////////////////////////////////////////////

        public static bool IsFileTrusted(
            Interpreter interpreter,
            string fileName
            )
        {
            return _RuntimeOps.IsFileTrusted(
                interpreter, null, fileName, IntPtr.Zero);
        }

        ///////////////////////////////////////////////////////////////////////

        public static bool IsInterpreterCreationDisabled(
            ref Result error
            )
        {
            return Interpreter.IsCreationDisabled(false, false, ref error);
        }

        ///////////////////////////////////////////////////////////////////////

        public static void EnableInterpreterCreation(
            DisableFlags flags
            )
        {
            Interpreter.EnableCreation(flags);
        }

        ///////////////////////////////////////////////////////////////////////

        public static void DisableInterpreterCreation(
            DisableFlags flags
            )
        {
            Interpreter.DisableCreation(flags);
        }

        ///////////////////////////////////////////////////////////////////////

        public static void EnableStubAssembly(
            DisableFlags flags
            )
        {
            Interpreter.EnableStubAssembly(flags);
        }

        ///////////////////////////////////////////////////////////////////////

        public static void DisableStubAssembly(
            DisableFlags flags
            )
        {
            Interpreter.DisableStubAssembly(flags);
        }

        ///////////////////////////////////////////////////////////////////////

#if NATIVE && (WINDOWS || UNIX)
        public static bool TryGetPathSerialNumber(
            string path,
            PathFlags flags,
            ref string serialNumber,
            ref Result error
            )
        {
            return PathOps.TryGetSerialNumber(
                path, flags, ref serialNumber, ref error);
        }
#endif

        ///////////////////////////////////////////////////////////////////////

        public static string CombinePath(
            bool? unix,
            IList list
            )
        {
            return PathOps.CombinePath(unix, list);
        }

        ///////////////////////////////////////////////////////////////////////

        public static string TranslatePath(
            string path,
            PathTranslationType translationType
            )
        {
            return PathOps.TranslatePath(path, translationType);
        }

        ///////////////////////////////////////////////////////////////////////

        public static string GetTempPath(
            Interpreter interpreter
            )
        {
            return PathOps.GetTempPath(interpreter);
        }

        ///////////////////////////////////////////////////////////////////////

        public static bool IsUnderPath(
            Interpreter interpreter,
            string path1,
            string path2
            )
        {
            return PathOps.IsUnderPath(interpreter, path1, path2);
        }

        ///////////////////////////////////////////////////////////////////////

        public static bool? GetLocalNames(
            bool perUser,
            bool? forceBuiltIn,
            out string userName,
            out string machineName,
            out string domainName
            )
        {
            return PathOps.GetLocalNames(
                perUser, forceBuiltIn, out userName, out machineName,
                out domainName);
        }

        ///////////////////////////////////////////////////////////////////////

        public static string GetUniquePath(
            Interpreter interpreter,
            string directory,
            string prefix,
            string suffix,
            ref Result error
            ) /* DEADLOCK-ON-DISPOSE */
        {
            return PathOps.GetUniquePath(
                interpreter, directory, prefix, suffix, ref error);
        }

        ///////////////////////////////////////////////////////////////////////

        public static bool[] GetGreatestMaxKeySizeAndLeastMinBlockSize(
            SymmetricAlgorithm algorithm,
            ref int keySize,
            ref int blockSize
            )
        {
            return _RuntimeOps.GetGreatestMaxKeySizeAndLeastMinBlockSize(
                algorithm, ref keySize, ref blockSize);
        }

        ///////////////////////////////////////////////////////////////////////

        public static ReturnCode GetFramework(
            Assembly assembly,
            Guid? id,
            FrameworkFlags flags,
            ref Result result
            )
        {
            return _RuntimeOps.GetFramework(assembly, id, flags, ref result);
        }

        ///////////////////////////////////////////////////////////////////////

        //
        // WARNING: The returned DateTime value may be virtualized (i.e. it may
        //          not reflect the actual current date and time).
        //
        public static DateTime GetNow()
        {
            return TimeOps.GetNow();
        }

        ///////////////////////////////////////////////////////////////////////

        //
        // WARNING: The returned DateTime value may be virtualized (i.e. it may
        //          not reflect the actual current date and time).
        //
        public static DateTime GetUtcNow()
        {
            return TimeOps.GetUtcNow();
        }

        ///////////////////////////////////////////////////////////////////////

        public static long GetUtcNowTicks()
        {
            return TimeOps.GetUtcNowTicks();
        }

        ///////////////////////////////////////////////////////////////////////

#if XML
        public static bool LooksLikeXmlDocument(
            string text
            )
        {
            return XmlOps.LooksLikeDocument(text);
        }
#endif

        ///////////////////////////////////////////////////////////////////////

        public static ulong GetRandomNumber()
        {
            return _RuntimeOps.GetRandomNumber();
        }

        ///////////////////////////////////////////////////////////////////////

        public static ReturnCode GetRandomBytes(
            Interpreter interpreter,
            ref byte[] bytes,
            ref Result error
            ) /* DEADLOCK-ON-DISPOSE */
        {
            return _RuntimeOps.GetRandomBytes(
                interpreter, ref bytes, ref error);
        }

        ///////////////////////////////////////////////////////////////////////

        public static ReturnCode SelectRandomArrayValue(
            Interpreter interpreter,
            Array array,
            ref object value,
            ref Result error
            ) /* DEADLOCK-ON-DISPOSE */
        {
            return ArrayOps.SelectRandomValue(
                interpreter, array, ref value, ref error);
        }

        ///////////////////////////////////////////////////////////////////////

        public static ReturnCode RemoveBlanksAndComments(
            bool trimAll,
            ref string value,
            ref Result error
            )
        {
            return StringOps.RemoveBlanksAndComments(
                trimAll, ref value, ref error);
        }

        ///////////////////////////////////////////////////////////////////////

        public static ReturnCode ExtractDataFromComments(
            ref string value,
            ref Result error
            )
        {
            return StringOps.ExtractDataFromComments(ref value, ref error);
        }

        ///////////////////////////////////////////////////////////////////////

        public static ReturnCode GetScriptPath(
            Interpreter interpreter,
            bool directoryOnly,
            ref string path,
            ref Result error
            ) /* DEADLOCK-ON-DISPOSE */
        {
            return ScriptOps.GetScriptPath(
                interpreter, directoryOnly, ref path, ref error);
        }

        ///////////////////////////////////////////////////////////////////////

        public static string SearchForPath(
            Interpreter interpreter,
            string path,
            FileSearchFlags fileSearchFlags
            ) /* DEADLOCK-ON-DISPOSE */
        {
            return PathOps.Search(interpreter, path, fileSearchFlags);
        }

        ///////////////////////////////////////////////////////////////////////

#if SHELL && INTERACTIVE_COMMANDS
        public static StringList GetInteractiveCommandNames(
            Interpreter interpreter,
            string pattern,
            bool noCase
            ) /* DEADLOCK-ON-DISPOSE */
        {
            return HelpOps.GetInteractiveCommandNames(
                interpreter, pattern, noCase);
        }

        ///////////////////////////////////////////////////////////////////////

        public static StringPair GetInteractiveCommandHelpItem(
            Interpreter interpreter,
            string name
            ) /* DEADLOCK-ON-DISPOSE */
        {
            return HelpOps.GetInteractiveCommandHelpItem(interpreter, name);
        }
#endif

        ///////////////////////////////////////////////////////////////////////

#if SHELL
        public static Thread CreateShellMainThread(
            IEnumerable<string> args,
            bool start
            )
        {
            return ShellOps.CreateShellMainThread(args, start);
        }

        ///////////////////////////////////////////////////////////////////////

        public static Thread CreateInteractiveLoopThread(
            Interpreter interpreter,
            IInteractiveLoopData loopData,
            bool start,
            ref Result error
            ) /* DEADLOCK-ON-DISPOSE */
        {
            return ShellOps.CreateInteractiveLoopThread(
                interpreter, loopData, start, ref error);
        }

        ///////////////////////////////////////////////////////////////////////

        public static ReturnCode StopInteractiveLoopThread(
            Thread thread,
            Interpreter interpreter,
            bool force,
            ref Result error
            ) /* DEADLOCK-ON-DISPOSE */
        {
            return ShellOps.StopInteractiveLoopThread(
                thread, interpreter, force, ref error);
        }
#endif

        ///////////////////////////////////////////////////////////////////////

        public static string BuildCommandLine(
            IEnumerable<string> args,
            bool quoteAll
            )
        {
            return _RuntimeOps.BuildCommandLine(args, quoteAll);
        }

        ///////////////////////////////////////////////////////////////////////

        public static string PopFirstArgument(
            ref IList<string> args
            )
        {
            return GenericOps<string>.PopFirstArgument(ref args);
        }

        ///////////////////////////////////////////////////////////////////////

        public static string PopLastArgument(
            ref IList<string> args
            )
        {
            return GenericOps<string>.PopLastArgument(ref args);
        }

        ///////////////////////////////////////////////////////////////////////

        public static bool MatchSwitch(
            string text,
            string @switch
            )
        {
            return StringOps.MatchSwitch(text, @switch);
        }

        ///////////////////////////////////////////////////////////////////////

        public static bool HasFlags(
            PackageIfNeededFlags flags,
            PackageIfNeededFlags hasFlags,
            bool all
            )
        {
            return FlagOps.HasFlags(flags, hasFlags, all);
        }

        ///////////////////////////////////////////////////////////////////////

        public static bool HasFlags(
            RuleSetType flags,
            RuleSetType hasFlags,
            bool all
            )
        {
            return FlagOps.HasFlags(flags, hasFlags, all);
        }

        ///////////////////////////////////////////////////////////////////////

        public static bool HasFlags(
            BreakpointType flags,
            BreakpointType hasFlags,
            bool all
            )
        {
            return FlagOps.HasFlags(flags, hasFlags, all);
        }

        ///////////////////////////////////////////////////////////////////////

        public static bool HasFlags(
            ProcedureFlags flags,
            ProcedureFlags hasFlags,
            bool all
            )
        {
            return FlagOps.HasFlags(flags, hasFlags, all);
        }

        ///////////////////////////////////////////////////////////////////////

        public static bool HasFlags(
            CommandFlags flags,
            CommandFlags hasFlags,
            bool all
            )
        {
            return FlagOps.HasFlags(flags, hasFlags, all);
        }

        ///////////////////////////////////////////////////////////////////////

        public static bool HasFlags(
            CreateFlags flags,
            CreateFlags hasFlags,
            bool all
            )
        {
            return FlagOps.HasFlags(flags, hasFlags, all);
        }

        ///////////////////////////////////////////////////////////////////////

        public static bool HasFlags(
            CreateStateFlags flags,
            CreateStateFlags hasFlags,
            bool all
            )
        {
            return FlagOps.HasFlags(flags, hasFlags, all);
        }

        ///////////////////////////////////////////////////////////////////////

        public static bool HasFlags(
            ExecutionPolicy flags,
            ExecutionPolicy hasFlags,
            bool all
            )
        {
            return FlagOps.HasFlags(flags, hasFlags, all);
        }

        ///////////////////////////////////////////////////////////////////////

        public static bool HasFlags(
            ExecutionPolicy? flags,
            ExecutionPolicy hasFlags,
            bool all
            )
        {
            return FlagOps.HasFlags(flags, hasFlags, all);
        }

        ///////////////////////////////////////////////////////////////////////

        public static bool HasFlags(
            HostFlags flags,
            HostFlags hasFlags,
            bool all
            )
        {
            return FlagOps.HasFlags(flags, hasFlags, all);
        }

        ///////////////////////////////////////////////////////////////////////

#if NOTIFY || NOTIFY_OBJECT
        public static bool HasFlags(
            NotifyFlags flags,
            NotifyFlags hasFlags,
            bool all
            )
        {
            return FlagOps.HasFlags(flags, hasFlags, all);
        }

        ///////////////////////////////////////////////////////////////////////

        public static bool HasFlags(
            NotifyType flags,
            NotifyType hasFlags,
            bool all
            )
        {
            return FlagOps.HasFlags(flags, hasFlags, all);
        }
#endif

        ///////////////////////////////////////////////////////////////////////

        public static bool HasFlags(
            OperatorFlags flags,
            OperatorFlags hasFlags,
            bool all
            )
        {
            return FlagOps.HasFlags(flags, hasFlags, all);
        }

        ///////////////////////////////////////////////////////////////////////

        public static bool HasFlags(
            PathFlags flags,
            PathFlags hasFlags,
            bool all
            )
        {
            return FlagOps.HasFlags(flags, hasFlags, all);
        }

        ///////////////////////////////////////////////////////////////////////

        public static bool HasFlags(
            PluginFlags flags,
            PluginFlags hasFlags,
            bool all
            )
        {
            return FlagOps.HasFlags(flags, hasFlags, all);
        }

        ///////////////////////////////////////////////////////////////////////

        public static bool HasFlags(
            PolicyFlags flags,
            PolicyFlags hasFlags,
            bool all
            )
        {
            return FlagOps.HasFlags(flags, hasFlags, all);
        }

        ///////////////////////////////////////////////////////////////////////

        public static bool HasFlags(
            ScriptFlags flags,
            ScriptFlags hasFlags,
            bool all
            )
        {
            return FlagOps.HasFlags(flags, hasFlags, all);
        }

        ///////////////////////////////////////////////////////////////////////

        public static bool HasFlags(
            SecretDataFlags flags,
            SecretDataFlags hasFlags,
            bool all
            )
        {
            return FlagOps.HasFlags(flags, hasFlags, all);
        }

        ///////////////////////////////////////////////////////////////////////

        public static bool HasFlags(
            SecretDataFlags? flags,
            SecretDataFlags hasFlags,
            bool all
            )
        {
            return FlagOps.HasFlags(flags, hasFlags, all);
        }

        ///////////////////////////////////////////////////////////////////////

        public static bool HasFlags(
            UriFlags flags,
            UriFlags hasFlags,
            bool all
            )
        {
            return FlagOps.HasFlags(flags, hasFlags, all);
        }

        ///////////////////////////////////////////////////////////////////////

#if NETWORK
        public static bool HasFlags(
            WebFlags flags,
            WebFlags hasFlags,
            bool all
            )
        {
            return FlagOps.HasFlags(flags, hasFlags, all);
        }
#endif

        ///////////////////////////////////////////////////////////////////////

        public static byte[] HashString(
            Interpreter interpreter,
            string value,
            EncodingType encodingType,
            ref Result error
            )
        {
            return HashOps.Compute(interpreter,
                HashOps.GetAlgorithmName(EncodingType.Text),
                value, StringOps.GetEncoding(encodingType),
                false, ref error);
        }

        ///////////////////////////////////////////////////////////////////////

        public static byte[] HashString(
            Interpreter interpreter,
            string hashAlgorithmName,
            string value,
            EncodingType encodingType,
            ref Result error
            )
        {
            return HashOps.Compute(
                interpreter, hashAlgorithmName, value,
                StringOps.GetEncoding(encodingType), false,
                ref error);
        }

        ///////////////////////////////////////////////////////////////////////

        public static byte[] HashString(
            string hashAlgorithmName,
            Encoding encoding,
            string text,
            ref Result error
            )
        {
            return HashOps.HashString(
                hashAlgorithmName, encoding, text, ref error);
        }

        ///////////////////////////////////////////////////////////////////////

        public static byte[] HashBytes(
            string hashAlgorithmName,
            byte[] bytes,
            ref Result error
            )
        {
            return HashOps.HashBytes(
                hashAlgorithmName, bytes, ref error);
        }

        ///////////////////////////////////////////////////////////////////////

        public static byte[] HashFile(
            string hashAlgorithmName,
            string fileName,
            Encoding encoding,
            ref Result error
            )
        {
            return _RuntimeOps.HashFile(
                hashAlgorithmName, fileName, encoding, ref error);
        }

        ///////////////////////////////////////////////////////////////////////

        public static byte[] HashStringAndOrBytes(
            string value1,
            byte[] value2,
            EncodingType encodingType,
            ref Result error
            )
        {
            return HashOps.Compute(HashOps.GetAlgorithmName(
                EncodingType.Text), value1, value2, StringOps.GetEncoding(
                encodingType), ref error);
        }

        ///////////////////////////////////////////////////////////////////////

        public static byte[] HashScriptFile(
            Interpreter interpreter,
            string fileName,
            bool noRemote,
            ref Result error
            ) /* DEADLOCK-ON-DISPOSE */
        {
            return _RuntimeOps.HashScriptFile(
                interpreter, fileName, noRemote, ref error);
        }

        ///////////////////////////////////////////////////////////////////////

        public static ReturnCode SetupTraceListeners(
            TraceListenerType listenerType,
            IClientData clientData,
            bool trace,
            bool debug,
            bool console,
            bool verbose,
            bool typeOnly,
            ref TraceListener listener,
            ref Result error
            )
        {
            return DebugOps.SetupTraceListeners(
                listenerType, clientData, trace,
                debug, console, verbose, typeOnly,
                ref listener, ref error);
        }

        ///////////////////////////////////////////////////////////////////////

        public static Encoding GetEncoding(
            string name,
            ref Result error
            )
        {
            return StringOps.GetEncoding(name, ref error);
        }

        ///////////////////////////////////////////////////////////////////////

#if TEST
        public static ReturnCode SetupTraceLogFile(
            string name,
            string fileName,
            Encoding encoding,
            LogFlags? flags,
            bool trace,
            bool debug,
            bool console,
            bool verbose,
            bool typeOnly,
            ref TraceListener listener,
            ref Result error
            )
        {
            return DebugOps.SetupTraceLogFile(
                name, fileName, encoding, flags, trace, debug, console,
                verbose, typeOnly, ref listener, ref error);
        }
#endif

        ///////////////////////////////////////////////////////////////////////

        public static bool ShouldTraceToHost(
            Interpreter interpreter
            ) /* SAFE-ON-DISPOSE */
        {
            return DebugOps.SafeGetTraceToHost(interpreter, false);
        }

        ///////////////////////////////////////////////////////////////////////

        public static string WrapHandle(
            Interpreter interpreter,
            object value
            ) /* DEADLOCK-ON-DISPOSE */
        {
            return HandleOps.Wrap(interpreter, value);
        }

        ///////////////////////////////////////////////////////////////////////

        public static object Identity(
            object arg
            )
        {
            return HandleOps.Identity(arg);
        }

        ///////////////////////////////////////////////////////////////////////

        public static Type TypeIdentity(
            Type arg
            )
        {
            return HandleOps.TypeIdentity(arg);
        }

        ///////////////////////////////////////////////////////////////////////

        public static AssemblyName GetAssemblyName(
            string assemblyName,
            ref Result error
            )
        {
            return AssemblyOps.GetName(assemblyName, ref error);
        }

        ///////////////////////////////////////////////////////////////////////

        public static Assembly FindAssemblyInAppDomain(
            Interpreter interpreter,
            AppDomain appDomain,
            MatchMode mode,
            string pattern,
            bool noCase,
            int? startIndex,
            ref Result error
            )
        {
            return AssemblyOps.FindInAppDomain(
                interpreter, appDomain, mode, pattern, noCase,
                startIndex, ref error);
        }

        ///////////////////////////////////////////////////////////////////////

        public static Assembly FindAssemblyInAppDomain(
            AppDomain appDomain,
            string name,
            Version version,
            byte[] publicKeyToken,
            ref Result error
            )
        {
            return AssemblyOps.FindInAppDomain(
                appDomain, name, version, publicKeyToken, ref error);
        }

        ///////////////////////////////////////////////////////////////////////

        public static Assembly FindAssemblyInAppDomain(
            Interpreter interpreter,
            IClientData clientData,
            AppDomain appDomain,
            string path,
            int? startIndex,
            ref Result error
            )
        {
            return AssemblyOps.FindInAppDomain(
                interpreter, clientData, appDomain, path,
                startIndex, ref error);
        }

        ///////////////////////////////////////////////////////////////////////

        public static bool IsSameAssemblyName(
            AssemblyName assemblyName1,
            AssemblyName assemblyName2
            )
        {
            return AssemblyOps.IsSameAssemblyName(
                assemblyName1, assemblyName2);
        }

        ///////////////////////////////////////////////////////////////////////

        public static InterpreterDictionary GetInterpreters()
        {
            return GlobalState.CloneInterpreterPairs();
        }

        ///////////////////////////////////////////////////////////////////////

        public static int DisposeInterpreters(
            MatchMode mode,
            string pattern,
            CancelFlags cancelFlags
            )
        {
            return GlobalState.DisposeInterpreters(
                mode, pattern, cancelFlags);
        }

        ///////////////////////////////////////////////////////////////////////

        public static AssemblyName GetPackageAssemblyName()
        {
            return GlobalState.GetAssemblyName();
        }

        ///////////////////////////////////////////////////////////////////////

        public static string GetPackageName(
            PackageType packageType,
            bool noCase
            )
        {
            return GlobalState.GetPackageName(packageType, noCase);
        }

        ///////////////////////////////////////////////////////////////////////

        public static string GetPackageName(
            PackageType packageType,
            string prefix,
            string suffix,
            bool noCase
            )
        {
            return GlobalState.GetPackageName(
                packageType, prefix, suffix, noCase);
        }

        ///////////////////////////////////////////////////////////////////////

        public static Version GetPackageVersion()
        {
            return GlobalState.GetPackageVersion();
        }

        ///////////////////////////////////////////////////////////////////////

        //
        // NOTE: The "mappings" dictionary passed here must contain mappings
        //       between (unqualified) assembly file names (e.g. "Harpy.dll",
        //       "Badge.dll", etc) and their contained (plugin) type names,
        //       e.g. "Licensing.Core", "Security.Core", "Badge.Enterprise",
        //       etc.
        //
        public static ReturnCode CreateAndEvaluatePackageIfNeededScripts(
            Interpreter interpreter,
            AssemblyFilePluginNames mappings,
            string path,
            Version version,
            byte[] publicKeyToken,
            CultureInfo cultureInfo,
            PackageIfNeededFlags flags,
            ref Result result
            )
        {
            return PackageOps.CreateAndEvaluateIfNeededScripts(
                interpreter, mappings, path, version, publicKeyToken,
                cultureInfo, flags, ref result);
        }

        ///////////////////////////////////////////////////////////////////////

        public static string GetPackageScanCommand(
            Interpreter interpreter,
            string commandName,
            IEnumerable<string> paths,
            ref Result error
            )
        {
            return PackageOps.GetScanCommand(
                interpreter, commandName, paths, ref error);
        }

        ///////////////////////////////////////////////////////////////////////

        public static int VersionCompare(
            Version version1,
            Version version2
            )
        {
            return PackageOps.VersionCompare(version1, version2);
        }

        ///////////////////////////////////////////////////////////////////////

        public static string GetPackagePath(
            Assembly assembly,
            string name,
            Version version,
            PathFlags pathFlags
            )
        {
            return GlobalState.GetPackagePath(
                assembly, name, version, pathFlags);
        }

        ///////////////////////////////////////////////////////////////////////

#if POLICY_TRACE
        public static void SetPolicyTrace(
            bool enable
            )
        {
            GlobalState.PolicyTrace = enable;
        }
#endif

        ///////////////////////////////////////////////////////////////////////

        public static ReturnCode CopyTrustedHashes(
            Interpreter interpreter,
            bool clear,
            ref Result error
            )
        {
            return GlobalState.CopyTrustedHashes(interpreter, clear, ref error);
        }

        ///////////////////////////////////////////////////////////////////////

        public static void CopyTrustedHashes(
            Interpreter sourceInterpreter,
            Interpreter targetInterpreter
            )
        {
            PolicyOps.CopyTrustedHashes(sourceInterpreter, targetInterpreter);
        }

        ///////////////////////////////////////////////////////////////////////

        public static ReturnCode AddTrustedHashes(
            IEnumerable<string> hashes,
            bool clear,
            ref Result error
            )
        {
            return GlobalState.AddTrustedHashes(hashes, clear, ref error);
        }

        ///////////////////////////////////////////////////////////////////////

        public static void SetBasePath(
            string basePath,
            bool refresh
            )
        {
            GlobalState.SetBasePath(basePath, refresh);
        }

        ///////////////////////////////////////////////////////////////////////

        public static void SetLibraryPath(
            string libraryPath,
            bool refresh
            )
        {
            GlobalState.SetLibraryPath(libraryPath, refresh);
        }

        ///////////////////////////////////////////////////////////////////////

        public static string FormatTracePriority(
            TracePriority priority,
            bool baseOnly,
            bool shortName
            )
        {
            return FormatOps.TracePriority(priority, baseOnly, shortName);
        }

        ///////////////////////////////////////////////////////////////////////

        public static string GetFullPlatformName()
        {
            return FormatOps.FullPlatformName(GlobalState.GetAssembly());
        }

        ///////////////////////////////////////////////////////////////////////

        public static string ScriptTypeToFileName(
            string type,
            PackageType packageType,
            bool fileNameOnly,
            bool strict
            )
        {
            return FormatOps.ScriptTypeToFileName(
                type, packageType, fileNameOnly, strict);
        }

        ///////////////////////////////////////////////////////////////////////

        public static bool SetVariableUndefined(
            IVariable variable,
            bool undefined
            )
        {
            return EntityOps.SetUndefined(variable, undefined);
        }

        ///////////////////////////////////////////////////////////////////////

        public static bool SetVariableDirty(
            IVariable variable,
            bool dirty
            )
        {
            return EntityOps.SetDirty(variable, dirty);
        }

        ///////////////////////////////////////////////////////////////////////

        public static bool SignalVariableDirty(
            IVariable variable,
            string index
            )
        {
            return EntityOps.SignalDirty(variable, index);
        }

        ///////////////////////////////////////////////////////////////////////

        public static string FormatBreakpoint(
            BreakpointType breakpointType
            )
        {
            return FormatOps.Breakpoint(breakpointType);
        }

        ///////////////////////////////////////////////////////////////////////

        public static string FormatComplaint(
            long id,
            ReturnCode code,
            Result result,
            string stackTrace
            )
        {
            return FormatOps.Complaint(id, code, result, stackTrace);
        }

        ///////////////////////////////////////////////////////////////////////

        public static string FormatErrorVariableName(
            string varName,
            string varIndex
            )
        {
            return FormatOps.ErrorVariableName(varName, varIndex);
        }

        ///////////////////////////////////////////////////////////////////////

        public static string GetBasePath()
        {
            return GlobalState.GetBasePath();
        }

        ///////////////////////////////////////////////////////////////////////

        public static string GetExternalsPath()
        {
            return GlobalState.GetExternalsPath();
        }

        ///////////////////////////////////////////////////////////////////////

        public static string GetBinaryPath()
        {
            //
            // HACK: We do not know if the external caller already has an
            //       interpreter; therefore, make sure the binary path is
            //       initialized here.
            //
            return GlobalState.InitializeOrGetBinaryPath(true);
        }

        ///////////////////////////////////////////////////////////////////////

        public static bool MaybeSetBinaryPath(
            string binaryPath,
            bool force
            )
        {
            return GlobalState.MaybeSetBinaryPath(binaryPath, force);
        }

        ///////////////////////////////////////////////////////////////////////

        public static string GetDocumentDirectory()
        {
            return PathOps.GetDocumentDirectory(false);
        }

        ///////////////////////////////////////////////////////////////////////

        public static string GetExecutableName()
        {
            return PathOps.GetExecutableName();
        }

        ///////////////////////////////////////////////////////////////////////

        public static string GetAssemblyPublicKeyToken(
            AssemblyName assemblyName
            )
        {
            return AssemblyOps.GetPublicKeyToken(assemblyName);
        }

        ///////////////////////////////////////////////////////////////////////

        public static ReturnCode GetAssemblyCertificate2(
            string fileName,
            bool noCache,
            ref X509Certificate2 certificate2,
            ref Result error
            )
        {
            return CertificateOps.GetCertificate2(
                fileName, noCache, ref certificate2, ref error);
        }

        ///////////////////////////////////////////////////////////////////////

        public static ReturnCode GetAssemblyCertificate2(
            Assembly assembly,
            bool strict,
            ref X509Certificate2 certificate2,
            ref Result error
            )
        {
            return AssemblyOps.GetCertificate2(
                assembly, strict, ref certificate2, ref error);
        }

        ///////////////////////////////////////////////////////////////////////

        public static Guid GetObjectId(
            MemberInfo memberInfo
            )
        {
            return AttributeOps.GetObjectId(memberInfo);
        }

        ///////////////////////////////////////////////////////////////////////

        public static Guid GetObjectId(
            object @object
            )
        {
            return AttributeOps.GetObjectId(@object);
        }

        ///////////////////////////////////////////////////////////////////////

        public static CommandFlags GetCommandFlags(
            MemberInfo memberInfo
            )
        {
            return AttributeOps.GetCommandFlags(memberInfo);
        }

        ///////////////////////////////////////////////////////////////////////

        public static CommandFlags GetCommandFlags(
            object @object
            )
        {
            return AttributeOps.GetCommandFlags(@object);
        }

        ///////////////////////////////////////////////////////////////////////

        public static FunctionFlags GetFunctionFlags(
            MemberInfo memberInfo
            )
        {
            return AttributeOps.GetFunctionFlags(memberInfo);
        }

        ///////////////////////////////////////////////////////////////////////

        public static FunctionFlags GetFunctionFlags(
            object @object
            )
        {
            return AttributeOps.GetFunctionFlags(@object);
        }

        ///////////////////////////////////////////////////////////////////////

        public static ProcessorArchitecture GetProcessorArchitecture()
        {
            return PlatformOps.GetProcessorArchitecture();
        }

        ///////////////////////////////////////////////////////////////////////

        public static IPluginData CreatePluginData(
            AppDomain appDomain,
            Assembly assembly,
            AssemblyName assemblyName,
            DateTime? dateTime,
            string fileName,
            string typeName,
            Uri uri,
            Uri updateUri,
            IClientData clientData,
            PluginFlags flags
            )
        {
            return Interpreter.CreatePluginData(
                appDomain, assembly, assemblyName, dateTime,
                fileName, typeName, uri, updateUri, clientData,
                flags);
        }

        ///////////////////////////////////////////////////////////////////////

        public static string GetPackageRelativeFileName(
            string fileName,
            bool keepLib,
            bool verbatim,
            ref Result error
            )
        {
            return PathOps.GetPackageRelativeFileName(
                fileName, keepLib, verbatim, ref error);
        }

        ///////////////////////////////////////////////////////////////////////

        public static string GetPluginRelativeFileName(
            IPlugin plugin,
            IClientData clientData,
            string fileName
            )
        {
            return PathOps.GetPluginRelativeFileName(
                plugin, clientData, fileName);
        }

        ///////////////////////////////////////////////////////////////////////

        public static PluginFlags GetPluginFlags(
            MemberInfo memberInfo
            )
        {
            return AttributeOps.GetPluginFlags(memberInfo);
        }

        ///////////////////////////////////////////////////////////////////////

        public static PluginFlags GetPluginFlags(
            object @object
            )
        {
            return AttributeOps.GetPluginFlags(@object);
        }

        ///////////////////////////////////////////////////////////////////////

        public static NotifyFlags GetNotifyFlags(
            MemberInfo memberInfo
            )
        {
            return AttributeOps.GetNotifyFlags(memberInfo);
        }

        ///////////////////////////////////////////////////////////////////////

        public static NotifyType GetNotifyTypes(
            MemberInfo memberInfo
            )
        {
            return AttributeOps.GetNotifyTypes(memberInfo);
        }

        ///////////////////////////////////////////////////////////////////////

        public static DateTime GetAssemblyDateTime(
            Assembly assembly
            )
        {
            return SharedAttributeOps.GetAssemblyDateTime(assembly);
        }

        ///////////////////////////////////////////////////////////////////////

        public static string GetAssemblyConfiguration(
            Assembly assembly
            )
        {
            return AttributeOps.GetAssemblyConfiguration(assembly);
        }

        ///////////////////////////////////////////////////////////////////////

        public static string GetAssemblyTag(
            Assembly assembly
            )
        {
            return SharedAttributeOps.GetAssemblyTag(assembly);
        }

        ///////////////////////////////////////////////////////////////////////

        public static string GetAssemblyText(
            Assembly assembly
            )
        {
            return SharedAttributeOps.GetAssemblyText(assembly);
        }

        ///////////////////////////////////////////////////////////////////////

        public static string GetAssemblyTextOrSuffix(
            Assembly assembly
            )
        {
            return _RuntimeOps.GetAssemblyTextOrSuffix(assembly);
        }

        ///////////////////////////////////////////////////////////////////////

        public static string GetAssemblyTitle(
            Assembly assembly
            )
        {
            return SharedAttributeOps.GetAssemblyTitle(assembly);
        }

        ///////////////////////////////////////////////////////////////////////

        public static string GetAssemblyDescription(
            Assembly assembly
            )
        {
            return AttributeOps.GetAssemblyDescription(assembly);
        }

        ///////////////////////////////////////////////////////////////////////

        public static Uri GetAssemblyUri(
            Assembly assembly
            )
        {
            return SharedAttributeOps.GetAssemblyUri(assembly);
        }

        ///////////////////////////////////////////////////////////////////////

        public static Uri GetAssemblyUri(
            Assembly assembly,
            string name
            )
        {
            return SharedAttributeOps.GetAssemblyUri(assembly, name);
        }

        ///////////////////////////////////////////////////////////////////////

        public static Version GetAssemblyVersion(
            Assembly assembly
            )
        {
            return AssemblyOps.GetVersion(assembly);
        }

        ///////////////////////////////////////////////////////////////////////

        public static Version GetEagleVersion()
        {
            return GlobalState.GetAssemblyVersion();
        }

        ///////////////////////////////////////////////////////////////////////

        public static string GetAssemblySourceId(
            Assembly assembly
            )
        {
            return SharedAttributeOps.GetAssemblySourceId(assembly);
        }

        ///////////////////////////////////////////////////////////////////////

        public static string GetAssemblySourceTimeStamp(
            Assembly assembly
            )
        {
            return SharedAttributeOps.GetAssemblySourceTimeStamp(assembly);
        }

        ///////////////////////////////////////////////////////////////////////

        public static bool HaveEagleThreading(
            Interpreter interpreter
            )
        {
            return _RuntimeOps.HaveThreading(interpreter);
        }

        ///////////////////////////////////////////////////////////////////////

        public static bool HaveEagleDefineConstant(
            string name
            )
        {
            return _RuntimeOps.HaveDefineConstant(name);
        }

        ///////////////////////////////////////////////////////////////////////

        public static string GetEagleSourceId()
        {
            return SharedAttributeOps.GetAssemblySourceId(
                GlobalState.GetAssembly());
        }

        ///////////////////////////////////////////////////////////////////////

        public static StringList GetEagleDefineConstants()
        {
            return Eagle._Constants.DefineConstants.OptionList;
        }

        ///////////////////////////////////////////////////////////////////////

        public static bool DoesTokenInterpreterExist(
            ulong? token
            )
        {
            if (token == null)
                return false;

            ulong localToken = (ulong)token;
            Interpreter interpreter;
            Result error = null;

            interpreter = GlobalState.GetTokenInterpreter(
                localToken, ref error);

            if (interpreter == null)
                return false;

            return interpreter.MatchToken(localToken);
        }

        ///////////////////////////////////////////////////////////////////////

        public static bool DoesEnvironmentVariableExist(
            string variable
            )
        {
            return CommonOps.Environment.DoesVariableExist(variable);
        }

        ///////////////////////////////////////////////////////////////////////

        public static bool DoesEnvironmentVariableExist(
            string variable,
            ref string value
            )
        {
            return CommonOps.Environment.DoesVariableExist(
                variable, ref value);
        }

        ///////////////////////////////////////////////////////////////////////

        public static bool DoesEnvironmentVariableExistOnce(
            string variable,
            ref string value
            )
        {
            return CommonOps.Environment.DoesVariableExistOnce(
                variable, ref value);
        }

        ///////////////////////////////////////////////////////////////////////

        public static string GetEnvironmentVariable(
            string variable
            )
        {
            return CommonOps.Environment.GetVariable(variable);
        }

        ///////////////////////////////////////////////////////////////////////

        public static string GetEnvironmentVariable(
            string variable,
            bool prefixed,
            bool expand
            )
        {
            ConfigurationFlags flags = ConfigurationFlags.Utility;

            if (prefixed)
                flags |= ConfigurationFlags.Prefixed;

            if (expand)
                flags |= ConfigurationFlags.Expand;

            return GlobalConfiguration.GetValue(variable, flags);
        }

        ///////////////////////////////////////////////////////////////////////

        public static string GetAndUnsetEnvironmentVariable(
            string variable
            )
        {
            return CommonOps.Environment.GetAndUnsetVariable(variable);
        }

        ///////////////////////////////////////////////////////////////////////

        public static bool SetEnvironmentVariable(
            string variable,
            string value
            )
        {
            return CommonOps.Environment.SetVariable(variable, value);
        }

        ///////////////////////////////////////////////////////////////////////

        public static void SetEnvironmentVariable(
            string variable,
            string value,
            bool prefixed
            )
        {
            ConfigurationFlags flags = ConfigurationFlags.Utility;

            if (prefixed)
                flags |= ConfigurationFlags.Prefixed;

            GlobalConfiguration.SetValue(variable, value, flags);
        }

        ///////////////////////////////////////////////////////////////////////

        public static bool UnsetEnvironmentVariable(
            string variable
            )
        {
            return CommonOps.Environment.UnsetVariable(variable);
        }

        ///////////////////////////////////////////////////////////////////////

        public static string ChangeEnvironmentVariable(
            string variable,
            string value
            )
        {
            return CommonOps.Environment.ChangeVariable(variable, value);
        }

        ///////////////////////////////////////////////////////////////////////

        public static string MaybeChangeEnvironmentVariable(
            string variable,
            string oldValue,
            string newValue
            )
        {
            return CommonOps.Environment.MaybeChangeVariable(
                variable, oldValue, newValue);
        }

        ///////////////////////////////////////////////////////////////////////

        public static void UnsetEnvironmentVariable(
            string variable,
            bool prefixed
            )
        {
            ConfigurationFlags flags = ConfigurationFlags.Utility;

            if (prefixed)
                flags |= ConfigurationFlags.Prefixed;

            GlobalConfiguration.UnsetValue(variable, flags);
        }

        ///////////////////////////////////////////////////////////////////////

        public static string ExpandEnvironmentVariables(
            string name
            )
        {
            return CommonOps.Environment.ExpandVariables(name);
        }

        ///////////////////////////////////////////////////////////////////////

        public static bool SaveEnvironmentVariables(
            IEnumerable<string> names,
            ref IClientData clientData
            )
        {
            return CommonOps.Environment.SaveVariables(
                names, ref clientData);
        }

        ///////////////////////////////////////////////////////////////////////

        public static bool SetEnvironmentVariables(
            IEnumerable<string> names,
            IClientData clientData
            )
        {
            return CommonOps.Environment.SetVariables(
                names, clientData);
        }

        ///////////////////////////////////////////////////////////////////////

        public static bool RestoreEnvironmentVariables(
            IEnumerable<string> names,
            IClientData clientData
            )
        {
            return CommonOps.Environment.RestoreVariables(
                names, clientData);
        }

        ///////////////////////////////////////////////////////////////////////

        public static string GetStringFromObject(
            object @object
            )
        {
            return StringOps.GetStringFromObject(@object);
        }

        ///////////////////////////////////////////////////////////////////////

        public static Argument GetArgumentFromObject(
            object @object
            )
        {
            return StringOps.GetArgumentFromObject(@object);
        }

        ///////////////////////////////////////////////////////////////////////

        public static Result GetResultFromObject(
            object @object
            )
        {
            return StringOps.GetResultFromObject(@object);
        }

        ///////////////////////////////////////////////////////////////////////

        public static TraceListenerType GetTraceListenerType(
            bool? console
            )
        {
            return DebugOps.GetTraceListenerType(console);
        }

        ///////////////////////////////////////////////////////////////////////

        public static TraceListener NewTraceListener(
            TraceListenerType listenerType,
            IClientData clienData,
            ref Result error
            )
        {
            return DebugOps.NewTraceListener(
                listenerType, clienData, ref error);
        }

        ///////////////////////////////////////////////////////////////////////

        public static ReturnCode AddTraceListener(
            TraceListener listener,
            bool debug
            )
        {
            return DebugOps.AddTraceListener(listener, debug);
        }

        ///////////////////////////////////////////////////////////////////////

        public static ReturnCode RemoveTraceListener(
            TraceListener listener,
            bool debug
            )
        {
            return DebugOps.RemoveTraceListener(listener, debug);
        }

        ///////////////////////////////////////////////////////////////////////

        public static IProcedure NewCoreProcedure(
            IProcedureData procedureData,
            ref Result error
            )
        {
            return _RuntimeOps.NewCoreProcedure(procedureData, ref error);
        }

        ///////////////////////////////////////////////////////////////////////

        public static bool IsWebUri(
            Uri uri,
            ref UriFlags flags,
            ref Result error
            )
        {
            return PathOps.IsWebUri(uri, ref flags, ref error);
        }

        ///////////////////////////////////////////////////////////////////////

        public static bool IsWebUri(
            Uri uri,
            ref UriFlags flags,
            ref string host,
            ref Result error
            )
        {
            return PathOps.IsWebUri(uri, ref flags, ref host, ref error);
        }

        ///////////////////////////////////////////////////////////////////////

        public static bool IsRemoteUri(
            string value
            )
        {
            return PathOps.IsRemoteUri(value);
        }

        ///////////////////////////////////////////////////////////////////////

        public static bool IsRemoteUri(
            string value,
            ref Uri uri
            )
        {
            return PathOps.IsRemoteUri(value, ref uri);
        }

        ///////////////////////////////////////////////////////////////////////

        public static object TryGetEnum(
            Type enumType,
            object value,
            ref Result error
            )
        {
            return EnumOps.TryGet(enumType, value, ref error);
        }

        ///////////////////////////////////////////////////////////////////////

        public static object TryParseEnum(
            Type enumType,
            string value,
            bool allowInteger,
            bool noCase
            )
        {
            return EnumOps.TryParse(enumType, value, allowInteger, noCase);
        }

        ///////////////////////////////////////////////////////////////////////

        public static object TryParseEnum(
            Type enumType,
            string value,
            bool allowInteger,
            bool ignoreLeading,
            bool errorOnNotFound,
            bool noCase,
            ref Result error
            )
        {
            return EnumOps.TryParse(
                enumType, value, allowInteger, ignoreLeading, errorOnNotFound,
                noCase, ref error);
        }

        ///////////////////////////////////////////////////////////////////////

        public static object TryParseEnum(
            Type enumType,
            string value,
            bool allowInteger,
            bool noCase,
            ref Result error
            )
        {
            return EnumOps.TryParse(
                enumType, value, allowInteger, noCase, ref error);
        }

        ///////////////////////////////////////////////////////////////////////

        public static object TryParseFlagsEnum(
            Interpreter interpreter,
            Type enumType,
            string oldValue,
            string newValue,
            CultureInfo cultureInfo,
            bool allowInteger,
            bool errorOnNop,
            bool noCase,
            ref Result error
            ) /* DEADLOCK-ON-DISPOSE */
        {
            return EnumOps.TryParseFlags(
                interpreter, enumType, oldValue, newValue, cultureInfo,
                allowInteger, errorOnNop, noCase, ref error);
        }

        ///////////////////////////////////////////////////////////////////////

        public static object TryParseFlagsEnum(
            Interpreter interpreter,
            Type enumType,
            string oldValue,
            string newValue,
            string maskValues,
            string maskOperators,
            CultureInfo cultureInfo,
            bool allowInteger,
            bool errorOnNop,
            bool errorOnMask,
            bool noCase,
            ref Result error
            ) /* DEADLOCK-ON-DISPOSE */
        {
            return EnumOps.TryParseFlags(
                interpreter, enumType, oldValue, newValue, maskValues,
                maskOperators, cultureInfo, allowInteger, errorOnNop,
                errorOnMask, noCase, ref error);
        }

        ///////////////////////////////////////////////////////////////////////

        public static ReturnCode SetParameterValuesFromTablesEnum(
            ObjectDictionary[] tables,
            ulong[] parameterValues,
            CultureInfo cultureInfo,
            bool errorOnBadValue,
            ref Result error
            )
        {
            return EnumOps.SetParameterValuesFromTables(
                tables, parameterValues, cultureInfo, errorOnBadValue,
                ref error);
        }

        ///////////////////////////////////////////////////////////////////////

        public static ReturnCode FillTablesEnum(
            Type enumType,
            ref ObjectDictionary[] tables,
            ref Result error
            )
        {
            return EnumOps.FillTables(enumType, ref tables, ref error);
        }

        ///////////////////////////////////////////////////////////////////////

        public static ReturnCode TryParseTablesEnum(
            Interpreter interpreter,
            Type enumType,
            string value,
            CultureInfo cultureInfo,
            bool noCase,
            bool errorOnEmptyList,
            bool errorOnNotFound,
            ref ObjectDictionary[] tables,
            ref Result error
            ) /* DEADLOCK-ON-DISPOSE */
        {
            return EnumOps.TryParseTables(
                interpreter, enumType, value, cultureInfo,
                noCase, errorOnEmptyList, errorOnNotFound,
                ref tables, ref error);
        }

        ///////////////////////////////////////////////////////////////////////

        public static ReturnCode GetEnumNamesAndValues(
            Type enumType,
            ref StringList enumNames,
            ref UlongList enumValues,
            ref Result error
            )
        {
            return EnumOps.GetNamesAndValues(
                enumType, ref enumNames, ref enumValues, ref error);
        }

        ///////////////////////////////////////////////////////////////////////

        public static StringList ToFlagsEnumList(
            Enum enumValue,
            StringList enumNames,
            UlongList enumValues,
            bool skipEnumType,
            bool skipNameless,
            bool keepZeros,
            bool uniqueValues,
            ref Result error
            )
        {
            return FormatOps.FlagsEnumV2(
                enumValue, enumNames, enumValues, skipEnumType,
                skipNameless, keepZeros, uniqueValues, ref error);
        }

        ///////////////////////////////////////////////////////////////////////

        public static bool IsFlagsEnum(Type enumType)
        {
            return EnumOps.IsFlags(enumType);
        }

        ///////////////////////////////////////////////////////////////////////

        public static string ListToEnglish(
            IList<string> list,
            string separator,
            string prefix,
            string suffix,
            string valuePrefix,
            string valueSuffix
            )
        {
            return GenericOps<string>.ListToEnglish(
                list, separator, prefix, suffix, valuePrefix, valueSuffix);
        }

        ///////////////////////////////////////////////////////////////////////

        public static string ListToEnglish(
            IList<Uri> list,
            string separator,
            string prefix,
            string suffix,
            string valuePrefix,
            string valueSuffix
            )
        {
            return GenericOps<Uri>.ListToEnglish(
                list, separator, prefix, suffix, valuePrefix, valueSuffix);
        }

        ///////////////////////////////////////////////////////////////////////

        public static int GetTotalLength<T>(
            IList<T> list,
            string format,
            int startIndex,
            int minimum
            )
        {
            return ListOps.GetTotalLength<T>(
                list, format, startIndex, minimum);
        }

        ///////////////////////////////////////////////////////////////////////

        public static bool IEnumerableEquals<T>(
            IEnumerable<T> collection1,
            IEnumerable<T> collection2,
            CompareCallback<T> callback
            )
        {
            return ListOps.IEnumerableEquals<T>(
                collection1, collection2, callback);
        }

        ///////////////////////////////////////////////////////////////////////

        public static int IEnumerableHashCode<T>(
            IEnumerable<T> collection,
            GetHashCodeCallback<T> callback
            )
        {
            return ListOps.IEnumerableHashCode<T>(collection, callback);
        }

        ///////////////////////////////////////////////////////////////////////

        public static ReturnCode TryDisposeObjectOrTrace<T>(
            ref T @object
            )
        {
            return ObjectOps.TryDisposeOrTrace<T>(ref @object);
        }

        ///////////////////////////////////////////////////////////////////////

        public static ReturnCode TryDisposeObjectOrComplain<T>(
            Interpreter interpreter,
            ref T @object
            )
        {
            return ObjectOps.TryDisposeOrComplain<T>(
                interpreter, ref @object);
        }

        ///////////////////////////////////////////////////////////////////////

        public static ReturnCode TryDisposeObject<T>(
            ref T @object,
            ref Result error
            )
        {
            return ObjectOps.TryDispose<T>(ref @object, ref error);
        }

        ///////////////////////////////////////////////////////////////////////

        public static string MakeStringId(
            string prefix,
            long id
            )
        {
            return FormatOps.Id(prefix, null, id);
        }

        ///////////////////////////////////////////////////////////////////////

        public static string MakeRelativePath(
            string path,
            bool separator
            )
        {
            return PathOps.MakeRelativePath(path, separator);
        }

        ///////////////////////////////////////////////////////////////////////

        public static ReturnCode ParseHexavigesimal(
            string text,
            ref long value,
            ref Result error
            )
        {
            if (!String.IsNullOrEmpty(text) &&
                (Parser.ParseHexavigesimal(text, 0, text.Length,
                    ref value) == text.Length))
            {
                return ReturnCode.Ok;
            }

            error = String.Format(
                "expected hexavigesimal wide integer but got \"{0}\"",
                text);

            return ReturnCode.Error;
        }

        ///////////////////////////////////////////////////////////////////////

        public static string FormatHexavigesimal(
            ulong value,
            byte width
            )
        {
            return FormatOps.Hexavigesimal(value, width);
        }

        ///////////////////////////////////////////////////////////////////////

        public static bool IsBase26(
            string text
            )
        {
            return StringOps.IsBase26(text);
        }

        ///////////////////////////////////////////////////////////////////////

        public static byte[] FromBase26String(
            string value
            )
        {
            return StringOps.FromBase26String(value);
        }

        ///////////////////////////////////////////////////////////////////////

        public static string ToBase26String(
            byte[] array,
            Base26FormattingOption options
            )
        {
            return StringOps.ToBase26String(array, options);
        }

        ///////////////////////////////////////////////////////////////////////

        public static bool IsBase64(
            string text
            )
        {
            return StringOps.IsBase64(text);
        }

        ///////////////////////////////////////////////////////////////////////

        public static string FormatDelegateMethodName(
            Delegate @delegate,
            bool assembly,
            bool display
            )
        {
            return FormatOps.DelegateMethodName(@delegate, assembly, display);
        }

        ///////////////////////////////////////////////////////////////////////

        public static string FormatPackageDirectory(
            string name,
            Version version,
            bool full
            )
        {
            return FormatOps.PackageDirectory(name, version, full);
        }

        ///////////////////////////////////////////////////////////////////////

        public static string FormatPluginName(
            string assemblyName,
            string typeName
            )
        {
            return FormatOps.PluginName(assemblyName, typeName);
        }

        ///////////////////////////////////////////////////////////////////////

        public static string FormatPluginAbout(
            IPluginData pluginData,
            bool full
            )
        {
            return FormatOps.PluginAbout(pluginData, full, null);
        }

        ///////////////////////////////////////////////////////////////////////

        public static string FormatPluginAbout(
            IPluginData pluginData,
            bool full,
            string extra
            )
        {
            return FormatOps.PluginAbout(pluginData, full, extra);
        }

        ///////////////////////////////////////////////////////////////////////

        public static string FormatId(
            string prefix,
            string name,
            long id
            )
        {
            return FormatOps.Id(prefix, name, id);
        }

        ///////////////////////////////////////////////////////////////////////

        public static string FormatResult(
            ReturnCode code,
            Result result
            )
        {
            return ResultOps.Format(code, result);
        }

        ///////////////////////////////////////////////////////////////////////

        public static string FormatResult(
            ReturnCode code,
            Result result,
            int errorLine
            )
        {
            return ResultOps.Format(code, result, errorLine);
        }

        ///////////////////////////////////////////////////////////////////////

        public static bool IsSuccess(
            ReturnCode code,
            bool exceptions
            )
        {
            return ResultOps.IsSuccess(code, exceptions);
        }

        ///////////////////////////////////////////////////////////////////////

        public static ReturnCode CustomOkCode(
            uint value
            )
        {
            return ResultOps.CustomOkCode(value);
        }

        ///////////////////////////////////////////////////////////////////////

        public static ReturnCode CustomErrorCode(
            uint value
            )
        {
            return ResultOps.CustomErrorCode(value);
        }

        ///////////////////////////////////////////////////////////////////////

        public static ExitCode SuccessExitCode()
        {
            return ResultOps.SuccessExitCode();
        }

        ///////////////////////////////////////////////////////////////////////

        public static ExitCode FailureExitCode()
        {
            return ResultOps.FailureExitCode();
        }

        ///////////////////////////////////////////////////////////////////////

        public static ExitCode ExceptionExitCode()
        {
            return ResultOps.ExceptionExitCode();
        }

        ///////////////////////////////////////////////////////////////////////

        public static ReturnCode ExitCodeToReturnCode(
            ExitCode exitCode
            )
        {
            return ResultOps.ExitCodeToReturnCode(exitCode);
        }

        ///////////////////////////////////////////////////////////////////////

        public static ExitCode ReturnCodeToExitCode(
            ReturnCode code
            )
        {
            return ResultOps.ReturnCodeToExitCode(code, false);
        }

        ///////////////////////////////////////////////////////////////////////

        public static ExitCode ReturnCodeToExitCode(
            ReturnCode code,
            bool exceptions
            )
        {
            return ResultOps.ReturnCodeToExitCode(code, exceptions);
        }

        ///////////////////////////////////////////////////////////////////////

        public static uint FlipEndian(
            uint X
            )
        {
            return ConversionOps.FlipEndian(X);
        }

        ///////////////////////////////////////////////////////////////////////

        public static ulong FlipEndian(
            ulong X
            )
        {
            return ConversionOps.FlipEndian(X);
        }

        ///////////////////////////////////////////////////////////////////////

        public static ReturnCode GetBytesFromString(
            string value,
            CultureInfo cultureInfo,
            ref byte[] bytes,
            ref Result error
            )
        {
            return StringOps.GetBytesFromString(
                value, cultureInfo, ref bytes, ref error);
        }

        ///////////////////////////////////////////////////////////////////////

        public static ReturnCode GetBytes(
            Encoding encoding,
            string value,
            EncodingType type,
            bool errorOnNull,
            ref byte[] bytes,
            ref Result error
            )
        {
            return StringOps.GetBytes(
                encoding, value, type, errorOnNull, ref bytes,
                ref error);
        }

        ///////////////////////////////////////////////////////////////////////

        public static StringList GetUniqueElements(
            StringList list
            )
        {
            return ListOps.GetUniqueElements(list);
        }

        ///////////////////////////////////////////////////////////////////////

        public static StringList GetUniqueElements(
            StringList list,
            UniqueStringCallback<string> callback
            )
        {
            return ListOps.GetUniqueElements(list, callback);
        }

        ///////////////////////////////////////////////////////////////////////

        public static string ToHexadecimalString(
            byte[] array
            )
        {
            return ArrayOps.ToHexadecimalString(array);
        }

        ///////////////////////////////////////////////////////////////////////

#if XML && SERIALIZATION
        public static ReturnCode Serialize(
            object @object,
            Type type,
            XmlWriter writer,
            XmlSerializerNamespaces serializerNamespaces,
            ref Result error
            )
        {
            return XmlOps.Serialize(
                @object, type, writer, serializerNamespaces, ref error);
        }

        ///////////////////////////////////////////////////////////////////////

        public static ReturnCode Deserialize(
            Type type,
            XmlReader reader,
            ref object @object,
            ref Result error
            )
        {
            return XmlOps.Deserialize(type, reader, ref @object, ref error);
        }
#endif

        ///////////////////////////////////////////////////////////////////////

#if XML
        public static ReturnCode Validate(
            Assembly assembly,
            string resourceName,
            XmlDocument document,
            ref Result error
            )
        {
            return XmlOps.Validate(
                assembly, resourceName, document, ref error);
        }
#endif

        ///////////////////////////////////////////////////////////////////////

        //
        // WARNING: *EXPERIMENTAL* This API may change until the core
        //          marshaller subsystem is completed.
        //
        public static ObjectOptionType GetOptionType(
            bool raw,
            bool all
            )
        {
            return ObjectOps.GetOptionType(raw, all);
        }

        ///////////////////////////////////////////////////////////////////////

        //
        // WARNING: *EXPERIMENTAL* This API may change until the core
        //          marshaller subsystem is completed.
        //
        public static OptionDictionary GetInvokeOptions(
            ObjectOptionType objectOptionType
            )
        {
            return ObjectOps.GetInvokeOptions(objectOptionType);
        }

        ///////////////////////////////////////////////////////////////////////

        public static Assembly LoadAssemblyFromStream(
            Stream stream,
            ref Result error
            )
        {
            return AssemblyOps.LoadFromStream(stream, ref error);
        }

        ///////////////////////////////////////////////////////////////////////

        public static ReturnCode VerifyAssemblyFromFile(
            string fileName,
            byte[] publicKeyToken,
            IClientData clientData,
            ref Result error
            )
        {
            return AssemblyOps.VerifyFromFile(
                fileName, publicKeyToken, clientData, ref error);
        }

        ///////////////////////////////////////////////////////////////////////

        public static Interpreter CreateInterpreterForSettings(
            Interpreter interpreter,
            IClientData clientData,
            ScriptDataFlags flags,
            ref Result result
            ) /* DEADLOCK-ON-DISPOSE */
        {
            return ScriptOps.CreateInterpreterForSettings(
                interpreter, clientData, flags, ref result);
        }

        ///////////////////////////////////////////////////////////////////////

        public static void ClearInterpreterForSettings()
        {
            ScriptOps.ClearInterpreterCache();
        }

        ///////////////////////////////////////////////////////////////////////

        public static bool IsScriptFileForSettingsPending(
            Interpreter interpreter
            )
        {
            return ScriptOps.IsFileForSettingsPending(interpreter);
        }

        ///////////////////////////////////////////////////////////////////////

        public static ReturnCode LoadSettingsViaScriptFile(
            Interpreter interpreter,
            IClientData pushClientData,
            IClientData callbackClientData,
            string fileName,
            ref ScriptDataFlags flags,
            ref _StringDictionary settings,
            ref Result error
            ) /* DEADLOCK-ON-DISPOSE */
        {
            return ScriptOps.LoadSettingsViaFile(
                interpreter, pushClientData, callbackClientData,
                fileName, ref flags, ref settings, ref error);
        }

        ///////////////////////////////////////////////////////////////////////

        public static ReturnCode CreateTemporaryScriptFile(
            string text,
            Encoding encoding,
            ref string fileName,
            ref Result error
            )
        {
            return ScriptOps.CreateTemporaryFile(
                text, encoding, ref fileName, ref error);
        }

        ///////////////////////////////////////////////////////////////////////

        public static ReturnCode TryGetValueOfType(
            Interpreter interpreter,
            IClientData clientData,
            Type type,
            string text,
            ValueFlags? valueFlags,
            string dateTimeFormat,
            DateTimeKind dateTimeKind,
            DateTimeStyles dateTimeStyles,
            ref object value,
            ref Result error
            )
        {
            return MarshalOps.TryGetValueOfType(
                interpreter, clientData, type, text, valueFlags,
                dateTimeFormat, dateTimeKind, dateTimeStyles,
                ref value, ref error);
        }

        ///////////////////////////////////////////////////////////////////////

        //
        // WARNING: *EXPERIMENTAL* This API may change until the core
        //          marshaller subsystem is completed.
        //
        public static void ProcessReflectionOptions(
            OptionDictionary options,
            ObjectOptionType objectOptionType,
            MemberTypes? defaultMemberTypes,
            BindingFlags? defaultBindingFlags,
            ValueFlags? defaultObjectValueFlags,
            ValueFlags? defaultMemberValueFlags,
            out MemberTypes memberTypes,
            out BindingFlags bindingFlags,
            out ValueFlags objectValueFlags,
            out ValueFlags memberValueFlags
            )
        {
            ObjectOps.ProcessReflectionOptions(
                options, objectOptionType, defaultMemberTypes,
                defaultBindingFlags, defaultObjectValueFlags,
                defaultMemberValueFlags, out memberTypes,
                out bindingFlags, out objectValueFlags,
                out memberValueFlags);
        }

        ///////////////////////////////////////////////////////////////////////

        //
        // WARNING: *EXPERIMENTAL* This API may change until the core
        //          marshaller subsystem is completed.
        //
        public static void ProcessFixupReturnValueOptions(
            OptionDictionary options,
            ObjectFlags? defaultObjectFlags,
            out ObjectFlags objectFlags,
            out string objectName,
            out string interpName,
            out bool alias,
            out bool aliasRaw,
            out bool aliasAll,
            out bool aliasReference
            )
        {
            ObjectOps.ProcessFixupReturnValueOptions(
                options, defaultObjectFlags, out objectFlags,
                out objectName, out interpName, out alias,
                out aliasRaw, out aliasAll, out aliasReference);
        }

        ///////////////////////////////////////////////////////////////////////

        //
        // WARNING: *EXPERIMENTAL* This API may change until the core
        //          marshaller subsystem is completed.
        //
        public static void ProcessFixupReturnValueOptions(
            OptionDictionary options,
            ObjectFlags? defaultObjectFlags,
            out Type returnType,
            out ObjectFlags objectFlags,
            out string objectName,
            out string interpName,
            out bool create,
            out bool dispose,
            out bool alias,
            out bool aliasRaw,
            out bool aliasAll,
            out bool aliasReference,
            out bool toString
            )
        {
            ObjectOps.ProcessFixupReturnValueOptions(
                options, defaultObjectFlags, out returnType,
                out objectFlags, out objectName, out interpName,
                out create, out dispose, out alias, out aliasRaw,
                out aliasAll, out aliasReference, out toString);
        }

        ///////////////////////////////////////////////////////////////////////

        //
        // WARNING: *EXPERIMENTAL* This API may change until the core
        //          marshaller subsystem is completed.
        //
        public static void ProcessFixupReturnValueOptions(
            OptionDictionary options,
            ObjectFlags? defaultObjectFlags,
            ObjectFlags? defaultByRefObjectFlags,
            out Type returnType,
            out ObjectFlags objectFlags,
            out ObjectFlags byRefObjectFlags,
            out string objectName,
            out string interpName,
            out bool create,
            out bool dispose,
            out bool alias,
            out bool aliasRaw,
            out bool aliasAll,
            out bool aliasReference,
            out bool toString
            )
        {
            ObjectOps.ProcessFixupReturnValueOptions(
                options, defaultObjectFlags, defaultByRefObjectFlags,
                out returnType, out objectFlags, out byRefObjectFlags,
                out objectName, out interpName, out create, out dispose,
                out alias, out aliasRaw, out aliasAll, out aliasReference,
                out toString);
        }

        ///////////////////////////////////////////////////////////////////////

        //
        // WARNING: *EXPERIMENTAL* This API may change until the core
        //          marshaller subsystem is completed.
        //
        public static OptionDictionary GetFixupReturnValueOptions()
        {
            return ObjectOps.GetFixupReturnValueOptions();
        }

        ///////////////////////////////////////////////////////////////////////

        //
        // WARNING: *EXPERIMENTAL* This API may change until the core
        //          marshaller subsystem is completed.
        //
        [Obsolete()]
        public static ReturnCode FixupReturnValue(
            Interpreter interpreter,
            Type type,
            ObjectFlags flags,
            OptionDictionary options,
            ObjectOptionType objectOptionType,
            string objectName,
            object value,
            bool alias,
            bool aliasReference,
            ref Result result
            ) /* DEADLOCK-ON-DISPOSE */
        {
            return MarshalOps.FixupReturnValue(
                interpreter, type, flags, null, options,
                objectOptionType, objectName, value, true,
                alias, aliasReference, ref result);
        }

        ///////////////////////////////////////////////////////////////////////

        //
        // WARNING: *EXPERIMENTAL* This API may change until the core
        //          marshaller subsystem is completed.
        //
        public static ReturnCode FixupReturnValue(
            Interpreter interpreter,
            Type type,
            ObjectFlags flags,
            OptionDictionary currentOptions,
            OptionDictionary aliasOptions,
            ObjectOptionType objectOptionType,
            string objectName,
            object value,
            bool alias,
            bool aliasReference,
            ref Result result
            ) /* DEADLOCK-ON-DISPOSE */
        {
            return MarshalOps.FixupReturnValue(
                interpreter, type, flags, currentOptions, aliasOptions,
                objectOptionType, objectName, value, true,
                alias, aliasReference, ref result);
        }

        ///////////////////////////////////////////////////////////////////////

        //
        // WARNING: *EXPERIMENTAL* This API may change until the core
        //          marshaller subsystem is completed.
        //
        [Obsolete()]
        public static ReturnCode FixupReturnValue(
            Interpreter interpreter,
            IBinder binder,
            CultureInfo cultureInfo,
            Type type,
            ObjectFlags flags,
            OptionDictionary options,
            ObjectOptionType objectOptionType,
            string objectName,
            string interpName,
            object value,
            bool create,
            bool dispose,
            bool alias,
            bool aliasReference,
            bool toString,
            ref Result result
            ) /* DEADLOCK-ON-DISPOSE */
        {
            return MarshalOps.FixupReturnValue(
                interpreter, binder, cultureInfo, type, flags,
                null, options, objectOptionType,
                objectName, interpName, value, create, dispose,
                alias, aliasReference, toString, ref result);
        }

        ///////////////////////////////////////////////////////////////////////

        //
        // WARNING: *EXPERIMENTAL* This API may change until the core
        //          marshaller subsystem is completed.
        //
        public static ReturnCode FixupReturnValue(
            Interpreter interpreter,
            IBinder binder,
            CultureInfo cultureInfo,
            Type type,
            ObjectFlags flags,
            OptionDictionary currentOptions,
            OptionDictionary aliasOptions,
            ObjectOptionType objectOptionType,
            string objectName,
            string interpName,
            object value,
            bool create,
            bool dispose,
            bool alias,
            bool aliasReference,
            bool toString,
            ref Result result
            ) /* DEADLOCK-ON-DISPOSE */
        {
            return MarshalOps.FixupReturnValue(
                interpreter, binder, cultureInfo, type, flags,
                currentOptions, aliasOptions, objectOptionType,
                objectName, interpName, value, create, dispose,
                alias, aliasReference, toString, ref result);
        }

        ///////////////////////////////////////////////////////////////////////

        public static void ThrowFeatureNotSupported(
            IPluginData pluginData,
            string name
            )
        {
            _RuntimeOps.ThrowFeatureNotSupported(pluginData, name);
        }

        ///////////////////////////////////////////////////////////////////////

        public static ReturnCode SubCommandPolicy(
            PolicyFlags flags,
            Type commandType,
            long commandToken,
            _StringDictionary subCommandNames,
            bool allowed,
            Interpreter interpreter,
            IClientData clientData,
            ArgumentList arguments,
            ref Result result
            ) /* DEADLOCK-ON-DISPOSE */
        {
            return PolicyOps.CheckViaSubCommand(
                flags, commandType, commandToken, subCommandNames,
                allowed, interpreter, clientData, arguments,
                ref result);
        }

        ///////////////////////////////////////////////////////////////////////

        public static ReturnCode DirectoryPolicy(
            PolicyFlags flags,
            Type commandType,
            long commandToken,
            string fileName,
            PathDictionary<object> directories,
            bool allowed,
            Interpreter interpreter,
            IClientData clientData,
            ArgumentList arguments,
            ref Result result
            ) /* DEADLOCK-ON-DISPOSE */
        {
            return PolicyOps.CheckViaDirectory(
                flags, commandType, commandToken, fileName,
                directories, allowed, interpreter, clientData,
                arguments, ref result);
        }

        ///////////////////////////////////////////////////////////////////////

        public static ReturnCode UriPolicy(
            PolicyFlags flags,
            Type commandType,
            long commandToken,
            Uri uri,
            UriDictionary<object> uris,
            bool allowed,
            Interpreter interpreter,
            IClientData clientData,
            ArgumentList arguments,
            ref Result result
            ) /* DEADLOCK-ON-DISPOSE */
        {
            return PolicyOps.CheckViaUri(
                flags, commandType, commandToken, uri, uris,
                allowed, interpreter, clientData, arguments,
                ref result);
        }

        ///////////////////////////////////////////////////////////////////////

        public static ReturnCode CallbackPolicy(
            PolicyFlags flags,
            Type commandType,
            long commandToken,
            ICallback callback,
            Interpreter interpreter,
            IClientData clientData,
            ArgumentList arguments,
            ref Result result
            ) /* DEADLOCK-ON-DISPOSE */
        {
            return PolicyOps.CheckViaCallback(
                flags, commandType, commandToken, callback,
                interpreter, clientData, arguments, ref result);
        }

        ///////////////////////////////////////////////////////////////////////

        public static ReturnCode ScriptPolicy(
            PolicyFlags flags,
            Type commandType,
            long commandToken,
            Interpreter policyInterpreter,
            string text,
            Interpreter interpreter,
            IClientData clientData,
            ArgumentList arguments,
            ref Result result
            ) /* DEADLOCK-ON-DISPOSE */
        {
            return PolicyOps.CheckViaScript(
                flags, commandType, commandToken, policyInterpreter,
                text, interpreter, clientData, arguments, ref result);
        }

        ///////////////////////////////////////////////////////////////////////

        public static ReturnCode TypePolicy(
            PolicyFlags flags,
            Type commandType,
            long commandToken,
            Type objectType,
            TypeList types,
            bool allowed,
            Interpreter interpreter,
            IClientData clientData,
            ArgumentList arguments,
            ref Result result
            ) /* DEADLOCK-ON-DISPOSE */
        {
            return PolicyOps.CheckViaType(
                flags, commandType, commandToken, objectType,
                types, allowed, interpreter, clientData,
                arguments, ref result);
        }

        ///////////////////////////////////////////////////////////////////////

        public static bool IsSameFile(
            StringList paths,
            string path2
            )
        {
            return IsSameFile(null, paths, path2);
        }

        ///////////////////////////////////////////////////////////////////////

        public static bool IsSameFile(
            Interpreter interpreter,
            StringList paths,
            string path2
            ) /* DEADLOCK-ON-DISPOSE */
        {
            foreach (string path1 in paths)
                if (IsSameFile(interpreter, path1, path2))
                    return true;

            return false;
        }

        ///////////////////////////////////////////////////////////////////////

        public static bool IsSameFile(
            string path1,
            string path2
            )
        {
            return IsSameFile(null, path1, path2);
        }

        ///////////////////////////////////////////////////////////////////////

        public static bool IsSameFile(
            Interpreter interpreter,
            string path1,
            string path2
            ) /* DEADLOCK-ON-DISPOSE */
        {
            return PathOps.IsSameFile(interpreter, path1, path2);
        }

        ///////////////////////////////////////////////////////////////////////

        public static string RobustNormalizePath(
            Interpreter interpreter,
            string path
            )
        {
            return PathOps.RobustNormalizePath(interpreter, path);
        }

        ///////////////////////////////////////////////////////////////////////

        public static string NormalizePath(
            string path
            )
        {
            return NormalizePath(null, path);
        }

        ///////////////////////////////////////////////////////////////////////

        public static string NormalizePath(
            Interpreter interpreter,
            string path
            ) /* DEADLOCK-ON-DISPOSE */
        {
            return PathOps.ResolvePath(interpreter, path);
        }

        ///////////////////////////////////////////////////////////////////////

        public static char GetFirstDirectorySeparator(
            string path
            )
        {
            return PathOps.GetFirstDirectorySeparator(path);
        }

        ///////////////////////////////////////////////////////////////////////

        public static bool IsAdministrator()
        {
            return _RuntimeOps.IsAdministrator();
        }

        ///////////////////////////////////////////////////////////////////////

        public static ReturnCode CheckDefineConstants(
            StringList defines,
            ref Result error
            )
        {
            return CommonOps.Runtime.CheckDefineConstants(defines, ref error);
        }

        ///////////////////////////////////////////////////////////////////////

        public static bool IsFramework20()
        {
            return CommonOps.Runtime.IsFramework20();
        }

        ///////////////////////////////////////////////////////////////////////

        public static bool IsFramework40()
        {
            return CommonOps.Runtime.IsFramework40();
        }

        ///////////////////////////////////////////////////////////////////////

        public static bool IsMono()
        {
            return CommonOps.Runtime.IsMono();
        }

        ///////////////////////////////////////////////////////////////////////

        public static bool IsDotNetCore()
        {
            return CommonOps.Runtime.IsDotNetCore();
        }

        ///////////////////////////////////////////////////////////////////////

        public static bool IsInteractive()
        {
            return WindowOps.IsInteractive();
        }

        ///////////////////////////////////////////////////////////////////////

        public static bool CleanupDirectory(
            string directory,
            IEnumerable<string> patterns,
            bool recursive
            )
        {
            return FileOps.CleanupDirectory(directory, patterns, recursive);
        }

        ///////////////////////////////////////////////////////////////////////

        public static bool ValidatePath(
            string path,
            bool asDirectory,
            bool? rooted,
            bool? exists
            )
        {
            return asDirectory ?
                PathOps.ValidatePathAsDirectory(path, rooted, exists) :
                PathOps.ValidatePathAsFile(path, rooted, exists);
        }

        ///////////////////////////////////////////////////////////////////////

#if !NET_STANDARD_20 && !MONO
        public static ReturnCode VerifyPath(
            string path,
            FilePermission permissions,
            ref Result error
            )
        {
            return FileOps.VerifyPath(path, permissions, ref error);
        }
#endif

        ///////////////////////////////////////////////////////////////////////

        public static int ComparePathParts(
            string part1,
            string part2
            )
        {
            return PathOps.CompareParts(part1, part2);
        }

        ///////////////////////////////////////////////////////////////////////

        public static int CompareFileNames(
            string path1,
            string path2
            )
        {
            return PathOps.CompareFileNames(path1, path2);
        }

        ///////////////////////////////////////////////////////////////////////

        public static PathType GetPathType(
            string path
            )
        {
            return PathOps.GetPathType(path);
        }

        ///////////////////////////////////////////////////////////////////////

        public static StringComparison GetPathComparisonType()
        {
            return PathOps.GetComparisonType();
        }

        ///////////////////////////////////////////////////////////////////////

        public static StringComparison GetSystemComparisonType(
            bool noCase
            )
        {
            return SharedStringOps.GetSystemComparisonType(noCase);
        }

        ///////////////////////////////////////////////////////////////////////

        public static bool StringEquals(
            string left,
            string right,
            StringComparison comparisonType
            )
        {
            return SharedStringOps.Equals(left, right, comparisonType);
        }

        ///////////////////////////////////////////////////////////////////////

        public static bool SystemStringEquals(
            string left,
            string right
            )
        {
            return SharedStringOps.SystemEquals(left, right);
        }

        ///////////////////////////////////////////////////////////////////////

        public static bool SystemStringEquals(
            string left,
            string right,
            bool noCase
            )
        {
            return SharedStringOps.Equals(left, right,
                SharedStringOps.GetSystemComparisonType(noCase));
        }

        ///////////////////////////////////////////////////////////////////////

        public static string NormalizeLineEndings(
            string text
            )
        {
            return StringOps.NormalizeLineEndings(text);
        }

        ///////////////////////////////////////////////////////////////////////

        public static long DefaultAttributeFlagsKey()
        {
            return AttributeFlags.DefaultKey;
        }

        ///////////////////////////////////////////////////////////////////////

        public static IDictionary<long, string> ParseAttributeFlags(
            string text,
            bool complex,
            bool space,
            bool sort,
            ref Result error
            )
        {
            return AttributeFlags.Parse(
                text, complex, space, sort, ref error);
        }

        ///////////////////////////////////////////////////////////////////////

        public static string FormatAttributeFlags(
            IDictionary<long, string> flags,
            bool legacy,
            bool compact,
            bool space,
            bool sort,
            ref Result error
            )
        {
            return AttributeFlags.Format(
                flags, legacy, compact, space, sort, ref error);
        }

        ///////////////////////////////////////////////////////////////////////

        public static bool VerifyAttributeFlags(
            string text,
            bool complex,
            bool space,
            ref Result error
            )
        {
            return AttributeFlags.Verify(text, complex, space, ref error);
        }

        ///////////////////////////////////////////////////////////////////////

        public static bool HaveAttributeFlags(
            IDictionary<long, string> flags,
            long key,
            string haveFlags,
            bool all,
            bool strict
            )
        {
            return AttributeFlags.Have(flags, key, haveFlags, all, strict);
        }

        ///////////////////////////////////////////////////////////////////////

        public static IDictionary<long, string> ChangeAttributeFlags(
            IDictionary<long, string> flags,
            long key,
            string changeFlags,
            bool sort,
            ref Result error
            )
        {
            return AttributeFlags.Change(
                flags, key, changeFlags, sort, ref error);
        }

        ///////////////////////////////////////////////////////////////////////

        public static Result BadValue(
            string adjective,
            string type,
            string value,
            IEnumerable<string> values,
            string prefix,
            string suffix
            )
        {
            return ScriptOps.BadValue(
                adjective, type, value, values, prefix, suffix);
        }

        ///////////////////////////////////////////////////////////////////////

        public static Result BadSubCommand(
            Interpreter interpreter, /* NOT USED */
            string adjective,
            string type,
            string subCommand,
            IEnsemble ensemble,
            string prefix,
            string suffix
            )
        {
            return ScriptOps.BadSubCommand(
                interpreter, adjective, type, subCommand, ensemble,
                prefix, suffix);
        }

        ///////////////////////////////////////////////////////////////////////

        public static Result WrongNumberOfArguments(
            IIdentifierName identifierName,
            int count,
            ArgumentList arguments,
            string suffix
            )
        {
            return ScriptOps.WrongNumberOfArguments(
                identifierName, count, arguments, suffix);
        }

        ///////////////////////////////////////////////////////////////////////

        public static Result BadSubCommand(
            Interpreter interpreter, /* NOT USED */
            string adjective,
            string type,
            string subCommand,
            EnsembleDictionary subCommands,
            string prefix,
            string suffix
            )
        {
            return ScriptOps.BadSubCommand(
                interpreter, adjective, type, subCommand, subCommands,
                prefix, suffix);
        }

        ///////////////////////////////////////////////////////////////////////

        public static ReturnCode SubCommandFromEnsemble(
            Interpreter interpreter,
            EnsembleDictionary subCommands,
            string type,
            bool strict,
            bool noCase,
            ref string name,
            ref Result error
            ) /* DEADLOCK-ON-DISPOSE */
        {
            return ScriptOps.SubCommandFromEnsemble(
                interpreter, subCommands, type, strict, noCase, ref name,
                ref error);
        }

        ///////////////////////////////////////////////////////////////////////

        public static ReturnCode TryExecuteSubCommandFromEnsemble(
            Interpreter interpreter,
            IEnsemble ensemble,
            IClientData clientData,
            ArgumentList arguments,
            bool strict,
            bool noCase,
            ref string name,
            ref bool tried,
            ref Result result
            ) /* DEADLOCK-ON-DISPOSE */
        {
            return ScriptOps.TryExecuteSubCommandFromEnsemble(
                interpreter, ensemble, clientData, arguments, strict,
                noCase, ref name, ref tried, ref result);
        }

        ///////////////////////////////////////////////////////////////////////

        public static ReturnCode PopulatePluginEntities(
            Interpreter interpreter,
            IPlugin plugin,
            TypeList types,
            IRuleSet ruleSet,
            CommandFlags? commandFlags,
            bool noCommands,
            bool noPolicies,
            bool verbose,
            ref Result error
            )
        {
            return _RuntimeOps.PopulatePluginEntities(
                interpreter, plugin, types, ruleSet, commandFlags,
                false, noCommands, noPolicies, verbose, ref error);
        }

        ///////////////////////////////////////////////////////////////////////

        public static ReturnCode ExtractPolicyContextAndCommand(
            Interpreter interpreter,
            IClientData clientData,
            Type commandType,
            long commandToken,
            ref IPolicyContext policyContext,
            ref bool match,
            ref Result error
            ) /* DEADLOCK-ON-DISPOSE */
        {
            return PolicyOps.ExtractContextAndCommand(
                interpreter, clientData, commandType, commandToken,
                ref policyContext, ref match, ref error);
        }

        ///////////////////////////////////////////////////////////////////////

        public static ReturnCode ExtractPolicyContextAndPlugin(
            Interpreter interpreter,
            IClientData clientData,
            ref IPolicyContext policyContext,
            ref IPlugin plugin,
            ref Result error
            ) /* DEADLOCK-ON-DISPOSE */
        {
            return PolicyOps.ExtractContextAndPlugin(
                interpreter, clientData, ref policyContext, ref plugin,
                ref error);
        }

        ///////////////////////////////////////////////////////////////////////

        public static ReturnCode ExtractPolicyContextAndScript(
            Interpreter interpreter,
            IClientData clientData,
            ref IPolicyContext policyContext,
            ref Encoding encoding,
            ref IScript script,
            ref Result error
            ) /* DEADLOCK-ON-DISPOSE */
        {
            return PolicyOps.ExtractContextAndScript(
                interpreter, clientData, ref policyContext, ref encoding,
                ref script, ref error);
        }

        ///////////////////////////////////////////////////////////////////////

        public static ReturnCode ExtractPolicyContextAndFileName(
            Interpreter interpreter,
            IClientData clientData,
            ref IPolicyContext policyContext,
            ref string fileName,
            ref Result error
            ) /* DEADLOCK-ON-DISPOSE */
        {
            return PolicyOps.ExtractContextAndFileName(
                interpreter, clientData, ref policyContext, ref fileName,
                ref error);
        }

        ///////////////////////////////////////////////////////////////////////

        public static ReturnCode ExtractPolicyContextAndText(
            Interpreter interpreter,
            IClientData clientData,
            ref IPolicyContext policyContext,
            ref string text,
            ref Result error
            ) /* DEADLOCK-ON-DISPOSE */
        {
            return PolicyOps.ExtractContextAndText(
                interpreter, clientData, ref policyContext, ref text,
                ref error);
        }

        ///////////////////////////////////////////////////////////////////////

        public static ReturnCode ExtractPolicyContextAndTextAndBytes(
            Interpreter interpreter,
            IClientData clientData,
            ref IPolicyContext policyContext,
            ref Encoding encoding,
            ref string text,
            ref byte[] hashValue,
            ref ByteList bytes,
            ref Result error
            ) /* DEADLOCK-ON-DISPOSE */
        {
            return PolicyOps.ExtractContextAndTextAndBytes(
                interpreter, clientData, ref policyContext, ref encoding,
                ref text, ref hashValue, ref bytes, ref error);
        }

        ///////////////////////////////////////////////////////////////////////

        public static bool DetectLibraryPath(
            Assembly assembly,
            IClientData clientData,
            DetectFlags detectFlags
            )
        {
            return GlobalState.DetectLibraryPath(
                assembly, clientData, detectFlags);
        }

        ///////////////////////////////////////////////////////////////////////

        public static bool DetectLibraryPath(
            AssemblyName assemblyName,
            Assembly assembly,
            IClientData clientData,
            DetectFlags detectFlags
            )
        {
            return GlobalState.DetectLibraryPath(
                assemblyName, assembly, clientData, detectFlags);
        }

        ///////////////////////////////////////////////////////////////////////

        public static Result CreateSynchronizedResult(
            string name
            )
        {
            return ResultOps.CreateSynchronized(name);
        }

        ///////////////////////////////////////////////////////////////////////

        public static void CleanupSynchronizedResult(
            Result synchronizedResult
            )
        {
            ResultOps.CleanupSynchronized(synchronizedResult);
        }

        ///////////////////////////////////////////////////////////////////////

        public static ReturnCode Wait(
            Interpreter interpreter,
            EventWaitHandle @event,
            long? waitMicroseconds,
            long? readyMicroseconds,
            bool timeout,
            bool noWindows,
            bool noCancel,
            bool noGlobalCancel,
            ref Result error
            ) /* SAFE-ON-DISPOSE */
        {
            return EventOps.Wait(
                interpreter, @event, waitMicroseconds, readyMicroseconds,
                timeout, noWindows, noCancel, noGlobalCancel, ref error);
        }

        ///////////////////////////////////////////////////////////////////////

        public static bool WaitSynchronizedResult(
            Result synchronizedResult
            )
        {
            return ResultOps.WaitSynchronized(synchronizedResult);
        }

        ///////////////////////////////////////////////////////////////////////

        public static bool WaitSynchronizedResult(
            Result synchronizedResult,
            int timeout
            )
        {
            return ResultOps.WaitSynchronized(
                synchronizedResult, timeout);
        }

        ///////////////////////////////////////////////////////////////////////

        public static void SetSynchronizedResult(
            Result synchronizedResult,
            ReturnCode code,
            Result result
            )
        {
            ResultOps.SetSynchronized(synchronizedResult, code, result);
        }

        ///////////////////////////////////////////////////////////////////////

        public static ReturnCode GetSynchronizedResult(
            Result synchronizedResult,
            ref ReturnCode code,
            ref Result result,
            ref Result error
            )
        {
            return ResultOps.GetSynchronized(
                synchronizedResult, ref code, ref result, ref error);
        }

        ///////////////////////////////////////////////////////////////////////

#if NETWORK
        public static bool IsSoftwareUpdateExclusive()
        {
            return UpdateOps.IsExclusive();
        }

        ///////////////////////////////////////////////////////////////////////

        public static ReturnCode SetSoftwareUpdateExclusive(
            bool exclusive,
            ref Result error
            )
        {
            return UpdateOps.SetExclusive(exclusive, ref error);
        }

        ///////////////////////////////////////////////////////////////////////

        public static bool IsSoftwareUpdateTrusted()
        {
            return UpdateOps.IsTrusted();
        }

        ///////////////////////////////////////////////////////////////////////

        public static ReturnCode SetSoftwareUpdateTrusted(
            bool trusted,
            ref Result error
            )
        {
            return UpdateOps.SetTrusted(trusted, ref error);
        }
#endif

        ///////////////////////////////////////////////////////////////////////

        public static bool AppDomainIsStoppingSoon()
        {
            return AppDomainOps.IsStoppingSoon();
        }

        ///////////////////////////////////////////////////////////////////////

        public static bool IsDefaultAppDomain()
        {
            return AppDomainOps.IsCurrentDefault();
        }

        ///////////////////////////////////////////////////////////////////////

        public static bool IsCrossAppDomain(
            IPluginData pluginData
            )
        {
            return AppDomainOps.IsCross(
                pluginData);
        }

        ///////////////////////////////////////////////////////////////////////

        public static bool IsCrossAppDomain(
            IPluginData pluginData,
            bool? resultOnNull
            )
        {
            return AppDomainOps.IsCross(
                pluginData, resultOnNull);
        }

        ///////////////////////////////////////////////////////////////////////

        public static bool IsCrossAppDomainNoIsolated(
            IPluginData pluginData
            )
        {
            return AppDomainOps.IsCrossNoIsolated(
                pluginData);
        }

        ///////////////////////////////////////////////////////////////////////

        public static bool IsCrossAppDomainNoIsolated(
            IPluginData pluginData,
            bool? resultOnNull
            )
        {
            return AppDomainOps.IsCrossNoIsolated(
                pluginData, resultOnNull);
        }

        ///////////////////////////////////////////////////////////////////////

        public static bool IsCrossAppDomain(
            Interpreter interpreter,
            IPluginData pluginData
            ) /* DEADLOCK-ON-DISPOSE */
        {
            return AppDomainOps.IsCross(
                interpreter, pluginData);
        }

        ///////////////////////////////////////////////////////////////////////

        public static bool IsCrossAppDomain(
            Interpreter interpreter,
            IPluginData pluginData,
            bool? resultOnNull
            ) /* DEADLOCK-ON-DISPOSE */
        {
            return AppDomainOps.IsCross(
                interpreter, pluginData, resultOnNull);
        }

        ///////////////////////////////////////////////////////////////////////

        public static bool IsCrossAppDomainNoIsolated(
            Interpreter interpreter,
            IPluginData pluginData
            ) /* DEADLOCK-ON-DISPOSE */
        {
            return AppDomainOps.IsCrossNoIsolated(
                interpreter, pluginData);
        }

        ///////////////////////////////////////////////////////////////////////

        public static bool IsCrossAppDomainNoIsolated(
            Interpreter interpreter,
            IPluginData pluginData,
            bool? resultOnNull
            ) /* DEADLOCK-ON-DISPOSE */
        {
            return AppDomainOps.IsCrossNoIsolated(
                interpreter, pluginData, resultOnNull);
        }

        ///////////////////////////////////////////////////////////////////////

        public static bool IsSameAppDomain(
            Interpreter interpreter
            ) /* DEADLOCK-ON-DISPOSE */
        {
            return AppDomainOps.IsSame(interpreter);
        }

        ///////////////////////////////////////////////////////////////////////

        public static bool IsSameAppDomain(
            IPluginData pluginData1,
            IPluginData pluginData2
            )
        {
            return AppDomainOps.IsSame(pluginData1, pluginData2);
        }

        ///////////////////////////////////////////////////////////////////////

        public static string GetCurrentPath(
            Assembly assembly
            )
        {
            return AssemblyOps.GetCurrentPath(assembly);
        }

        ///////////////////////////////////////////////////////////////////////

        public static string GetOriginalLocalPath(
            Assembly assembly
            )
        {
            return AssemblyOps.GetOriginalLocalPath(assembly);
        }

        ///////////////////////////////////////////////////////////////////////

        public static string GetOriginalLocalPath(
            AssemblyName assemblyName
            )
        {
            return AssemblyOps.GetOriginalLocalPath(assemblyName);
        }

        ///////////////////////////////////////////////////////////////////////

        public static long GetCurrentAppDomainId()
        {
            return AppDomainOps.GetCurrentId();
        }

        ///////////////////////////////////////////////////////////////////////

        public static long GetCurrentProcessId()
        {
            return ProcessOps.GetId();
        }

        ///////////////////////////////////////////////////////////////////////

        public static string GetCurrentProcessFileName()
        {
            return ProcessOps.GetFileName();
        }

        ///////////////////////////////////////////////////////////////////////

        public static long GetCurrentThreadId()
        {
            return GlobalState.GetCurrentThreadId(); /* EXEMPT */
        }

        ///////////////////////////////////////////////////////////////////////

        public static long GetParentProcessId()
        {
            return ProcessOps.GetParentId();
        }

        ///////////////////////////////////////////////////////////////////////

        public static ReturnCode InvokeDelegate(
            Interpreter interpreter,
            Delegate @delegate,
            DelegateFlags delegateFlags,
            ArgumentList arguments,
            int nameCount,
            ref Result result
            ) /* DEADLOCK-ON-DISPOSE */
        {
            return ObjectOps.InvokeDelegate(
                interpreter, @delegate, delegateFlags, arguments,
                nameCount, ref result);
        }

        ///////////////////////////////////////////////////////////////////////

#if CONSOLE
        public static void WritePromptViaConsole(
            string value
            )
        {
            ConsoleOps.WritePrompt(value);
        }
#endif

        ///////////////////////////////////////////////////////////////////////

#if SHELL
        public static bool? WriteOptions(
            IInteractiveHost interactiveHost, /* in */
            IEnumerable<string> options,      /* in */
            int perLine,                      /* in */
            bool newLine                      /* in */
            )
        {
            return HelpOps.WriteOptions(
                interactiveHost, options, perLine, newLine);
        }
#endif

        ///////////////////////////////////////////////////////////////////////

        public static ReturnCode WriteViaIExecute(
            Interpreter interpreter,
            string commandName, /* NOTE: Almost always null, for [puts]. */
            string channelId,   /* NOTE: Almost always null, for "stdout". */
            string value,
            ref Result result
            ) /* DEADLOCK-ON-DISPOSE */
        {
            return ScriptOps.WriteViaIExecute(
                interpreter, commandName, channelId, value, ref result);
        }

        ///////////////////////////////////////////////////////////////////////

        public static string FormatTraceDateTime(
            DateTime value,
            bool interactive
            )
        {
            return FormatOps.TraceDateTime(value, interactive);
        }

        ///////////////////////////////////////////////////////////////////////

        public static string FormatScriptForLog(
            bool normalize,
            bool ellipsis,
            object value
            )
        {
            return FormatOps.ScriptForLog(normalize, ellipsis, value);
        }

        ///////////////////////////////////////////////////////////////////////

        [Obsolete()]
        public static string FormatTraceException(
            Exception exception
            )
        {
            return FormatOps.TraceException(exception);
        }

        ///////////////////////////////////////////////////////////////////////

        public static string FormatPluginName(
            IPluginData pluginData,
            bool wrap
            )
        {
            return FormatOps.PluginName(pluginData, wrap);
        }

        ///////////////////////////////////////////////////////////////////////

        public static string FormatDefineConstants(
            IEnumerable<string> collection
            )
        {
            return FormatOps.DefineConstants(collection);
        }

        ///////////////////////////////////////////////////////////////////////

        public static string FormatCommandLogEntry(
            Interpreter interpreter,
            IPluginData pluginData,
            IClientData clientData,
            ArgumentList arguments,
            ReturnCode? returnCode,
            Result result,
            int indentSpaces,
            bool allowNewLines,
            ref long entryId
            )
        {
            return FormatOps.CommandLogEntry(
                interpreter, pluginData, clientData, arguments,
                returnCode, result, indentSpaces, allowNewLines,
                ref entryId);
        }

        ///////////////////////////////////////////////////////////////////////

        public static string FormatDisplayString(
            string value
            )
        {
            return FormatOps.DisplayString(value);
        }

        ///////////////////////////////////////////////////////////////////////

        public static object FormatMaybeNull(
            object value
            )
        {
            return FormatOps.MaybeNull(value);
        }

        ///////////////////////////////////////////////////////////////////////

        public static string FormatTypeAndWrapOrNull(
            object value
            )
        {
            return FormatOps.TypeAndWrapOrNull(value);
        }

        ///////////////////////////////////////////////////////////////////////

        public static string FormatWrapOrNull(
            IEnumerable<string> value
            )
        {
            return FormatOps.WrapOrNull(value);
        }

        ///////////////////////////////////////////////////////////////////////

        public static string FormatWrapOrNull(
            object value
            )
        {
            return FormatOps.WrapOrNull(value);
        }

        ///////////////////////////////////////////////////////////////////////

        public static string FormatWrapOrNull(
            bool normalize,
            bool ellipsis,
            IEnumerable<string> value
            )
        {
            return FormatOps.WrapOrNull(normalize, ellipsis, value);
        }

        ///////////////////////////////////////////////////////////////////////

        public static string FormatWrapOrNull(
            bool normalize,
            bool ellipsis,
            object value
            )
        {
            return FormatOps.WrapOrNull(normalize, ellipsis, value);
        }

        ///////////////////////////////////////////////////////////////////////

        public static string FormatWrapOrNull(
            bool normalize,
            bool ellipsis,
            bool display,
            IEnumerable<string> value
            )
        {
            return FormatOps.WrapOrNull(normalize, ellipsis, display, value);
        }

        ///////////////////////////////////////////////////////////////////////

        public static string FormatWrapOrNull(
            bool normalize,
            bool ellipsis,
            bool display,
            object value
            )
        {
            return FormatOps.WrapOrNull(normalize, ellipsis, display, value);
        }

        ///////////////////////////////////////////////////////////////////////

        public static bool ArrayEquals(
            byte[] array1,
            byte[] array2
            )
        {
            return ArrayOps.Equals(array1, array2);
        }

        ///////////////////////////////////////////////////////////////////////

        public static bool ArrayEquals(
            byte[] array1,
            byte[] array2,
            int length
            )
        {
            return ArrayOps.Equals(array1, array2, length);
        }

        ///////////////////////////////////////////////////////////////////////

        public static int ArrayIndexOf(
            byte[] array1,
            byte[] array2
            )
        {
            return ArrayOps.IndexOf(array1, array2);
        }

        ///////////////////////////////////////////////////////////////////////

        public static bool MaybeAdjustTraceLimits(
            bool? enable
            )
        {
            return TraceLimits.MaybeAdjustEnabled(enable);
        }

        ///////////////////////////////////////////////////////////////////////

        public static TracePriority GetTracePriority()
        {
            return TraceOps.GetTracePriority();
        }

        ///////////////////////////////////////////////////////////////////////

        public static ReturnCode ProcessTraceClientData(
            TraceClientData traceClientData,
            ref Result result
            )
        {
            return TraceOps.ProcessClientData(traceClientData, ref result);
        }

        ///////////////////////////////////////////////////////////////////////

        [Obsolete()] /* NOTE: Lack of priority. */
        [MethodImpl(MethodImplOptions.NoInlining)]
        public static void DebugTrace(
            string message,
            string category
            )
        {
            TraceOps.DebugTraceAlways(message, category,
                TraceOps.GetTracePriority() | TracePriority.External);
        }

        ///////////////////////////////////////////////////////////////////////

        [Obsolete()] /* NOTE: Lack of priority. */
        [MethodImpl(MethodImplOptions.NoInlining)]
        public static void DebugTrace(
            long threadId,
            string message,
            string category
            )
        {
            TraceOps.DebugTraceAlways(threadId, message, category,
                TraceOps.GetTracePriority() | TracePriority.External);
        }

        ///////////////////////////////////////////////////////////////////////

        [MethodImpl(MethodImplOptions.NoInlining)]
        public static void DebugTrace(
            Interpreter interpreter,
            string message,
            string category,
            TracePriority priority,
            int skipFrames
            )
        {
            TraceOps.DebugTraceAlways(interpreter, message, category,
                priority | TracePriority.External, skipFrames);
        }

        ///////////////////////////////////////////////////////////////////////

        [MethodImpl(MethodImplOptions.NoInlining)]
        public static void DebugTrace(
            string message,
            string category,
            TracePriority priority
            )
        {
            TraceOps.DebugTraceAlways(message, category,
                priority | TracePriority.External);
        }

        ///////////////////////////////////////////////////////////////////////

        [MethodImpl(MethodImplOptions.NoInlining)]
        public static void DebugTrace(
            Exception exception,
            string category,
            TracePriority priority
            )
        {
            TraceOps.DebugTraceAlways(exception, category,
                priority | TracePriority.External);
        }

        ///////////////////////////////////////////////////////////////////////

        [MethodImpl(MethodImplOptions.NoInlining)]
        public static void DebugTrace(
            Interpreter interpreter,
            Exception exception,
            string category,
            TracePriority priority,
            int skipFrames
            )
        {
            TraceOps.DebugTraceAlways(interpreter, exception, category,
                priority | TracePriority.External, skipFrames);
        }

        ///////////////////////////////////////////////////////////////////////

        [MethodImpl(MethodImplOptions.NoInlining)]
        public static void DebugTrace(
            long threadId,
            string message,
            string category,
            TracePriority priority
            )
        {
            TraceOps.DebugTraceAlways(threadId, message, category,
                priority | TracePriority.External);
        }

        ///////////////////////////////////////////////////////////////////////

        [MethodImpl(MethodImplOptions.NoInlining)]
        public static void Complain(
            Interpreter interpreter,
            ReturnCode code,
            Result result
            ) /* SAFE-ON-DISPOSE */
        {
            DebugOps.Complain(interpreter, code, result);
        }

        ///////////////////////////////////////////////////////////////////////

        public static void WriteWithoutFail(
            IDebugHost debugHost,
            string value,
            bool viaHost
            )
        {
            DebugOps.WriteWithoutFail(debugHost, value, true, viaHost);
        }

        ///////////////////////////////////////////////////////////////////////

        public static long NextId()
        {
            return GlobalState.NextId();
        }

        ///////////////////////////////////////////////////////////////////////

#if NATIVE
        public static string MaybeGetErrorMessage()
        {
            return NativeOps.MaybeGetErrorMessage();
        }

        ///////////////////////////////////////////////////////////////////////

        public static string GetErrorMessage()
        {
            return NativeOps.GetErrorMessage();
        }

        ///////////////////////////////////////////////////////////////////////

        public static string GetErrorMessage(
            int error
            )
        {
            return NativeOps.GetErrorMessage(error);
        }
#endif

        ///////////////////////////////////////////////////////////////////////

        public static Stream GetStream(
            Assembly assembly,
            string name,
            ref Result error
            )
        {
            return _RuntimeOps.GetStream(assembly, name, ref error);
        }

        ///////////////////////////////////////////////////////////////////////

        public static Assembly FindStream(
            Assembly assembly,
            string name,
            bool verbose,
            ref Result error
            )
        {
            return _RuntimeOps.FindStream(
                assembly, name, verbose, ref error);
        }

        ///////////////////////////////////////////////////////////////////////

        public static Stream GetStream(
            ResourceManager resourceManager,
            string name,
            CultureInfo cultureInfo,
            ref Result error
            )
        {
            return _RuntimeOps.GetStream(
                resourceManager, name, cultureInfo, ref error);
        }

        ///////////////////////////////////////////////////////////////////////

        public static string GetString(
            ResourceManager resourceManager,
            string name,
            CultureInfo cultureInfo,
            ref Result error
            )
        {
            return _RuntimeOps.GetString(
                resourceManager, name, cultureInfo, ref error);
        }

        ///////////////////////////////////////////////////////////////////////

        public static string GetAnyString(
            Interpreter interpreter,
            IPlugin plugin,
            ResourceManager resourceManager,
            string name,
            CultureInfo cultureInfo,
            ref Result error
            )
        {
            return _RuntimeOps.GetAnyString(
                interpreter, plugin, resourceManager, name, cultureInfo,
                ref error);
        }

        ///////////////////////////////////////////////////////////////////////

        public static ReturnCode GetResourceNames(
            IPluginData pluginData,
            ResourceManager resourceManager,
            CultureInfo cultureInfo,
            ref StringList list,
            ref Result error
            )
        {
            return _RuntimeOps.GetResourceNames(
                pluginData, resourceManager, cultureInfo, ref list, ref error);
        }

        ///////////////////////////////////////////////////////////////////////

        public static string GetTclPatchLevel()
        {
            return TclVars.Package.PatchLevelValue;
        }

        ///////////////////////////////////////////////////////////////////////

        public static string GetTclVersion()
        {
            return TclVars.Package.VersionValue;
        }

        ///////////////////////////////////////////////////////////////////////

        public static bool IsTransparentProxy(
            object proxy
            )
        {
            return AppDomainOps.IsTransparentProxy(proxy);
        }

        ///////////////////////////////////////////////////////////////////////

#if ISOLATED_PLUGINS
        [Obsolete()]
        public static bool IsPluginIsolated(
            IPluginData pluginData
            )
        {
            return AppDomainOps.IsIsolated(pluginData);
        }

        ///////////////////////////////////////////////////////////////////////

        public static ReturnCode FixupOptions(
            IPluginData pluginData,
            OptionDictionary options,
            bool strict,
            ref Result error
            )
        {
            return AppDomainOps.FixupOptions(
                pluginData, options, strict, ref error);
        }
#endif

        ///////////////////////////////////////////////////////////////////////

        public static void BeginWithAutoPath(
            string path,
            bool verbose,
            ref string savedLibPath
            )
        {
            GlobalState.BeginWithAutoPath(path, verbose, ref savedLibPath);
        }

        ///////////////////////////////////////////////////////////////////////

        public static void EndWithAutoPath(
            bool verbose,
            ref string savedLibPath
            )
        {
            GlobalState.EndWithAutoPath(verbose, ref savedLibPath);
        }

        ///////////////////////////////////////////////////////////////////////

        public static bool RefreshEntryAssembly(
            Assembly assembly
            )
        {
            return GlobalState.RefreshEntryAssembly(assembly);
        }

        ///////////////////////////////////////////////////////////////////////

        //
        // HACK: This method needs to be public because external applications
        //       and plugins may set the environment variables we care about;
        //       however, there is no other way to notify this library about
        //       those changes (other than this method, that is).
        //
        public static void RefreshAutoPathList(
            bool verbose
            )
        {
            GlobalState.RefreshAutoPathList(verbose);
        }

        ///////////////////////////////////////////////////////////////////////

        public static Uri TryCombineUris(
            Uri baseUri,
            string relativeUri,
            Encoding encoding,
            UriComponents components,
            UriFormat format,
            UriFlags flags,
            ref Result error
            )
        {
            return PathOps.TryCombineUris(
                baseUri, relativeUri, encoding, components, format,
                flags, ref error);
        }

        ///////////////////////////////////////////////////////////////////////

#if NETWORK
        public static ReturnCode DownloadData(
            Interpreter interpreter,
            IClientData clientData,
            Uri uri,
            int? timeout,
            bool trusted,
            ref byte[] bytes,
            ref Result error
            ) /* DEADLOCK-ON-DISPOSE */
        {
            return WebOps.DownloadData(
                interpreter, clientData, uri, timeout, trusted, ref bytes,
                ref error);
        }

        ///////////////////////////////////////////////////////////////////////

        public static ReturnCode DownloadFile(
            Interpreter interpreter,
            IClientData clientData,
            Uri uri,
            string fileName,
            int? timeout,
            bool trusted,
            ref Result error
            ) /* DEADLOCK-ON-DISPOSE */
        {
            return WebOps.DownloadFile(
                interpreter, clientData, uri, fileName, timeout, trusted,
                ref error);
        }

        ///////////////////////////////////////////////////////////////////////

        public static ReturnCode UploadData(
            Interpreter interpreter,
            IClientData clientData,
            Uri uri,
            string method,
            byte[] rawData,
            int? timeout,
            bool trusted,
            ref byte[] bytes,
            ref Result error
            )
        {
            return WebOps.UploadData(
                interpreter, clientData, uri, method, rawData, timeout,
                trusted, ref bytes, ref error);
        }

        ///////////////////////////////////////////////////////////////////////

        public static ReturnCode UploadValues(
            Interpreter interpreter,
            IClientData clientData,
            Uri uri,
            string method,
            NameValueCollection collection,
            int? timeout,
            bool trusted,
            ref byte[] bytes,
            ref Result error
            ) /* DEADLOCK-ON-DISPOSE */
        {
            return WebOps.UploadValues(
                interpreter, clientData, uri, method, collection, timeout,
                trusted, ref bytes, ref error);
        }
#endif

        ///////////////////////////////////////////////////////////////////////

        public static ICallback CreateCommandCallback(
            MarshalFlags marshalFlags,
            CallbackFlags callbackFlags,
            ObjectFlags objectFlags,
            ByRefArgumentFlags byRefArgumentFlags,
            Interpreter interpreter,
            IClientData clientData,
            string name,
            StringList arguments,
            ref Result error
            ) /* DEADLOCK-ON-DISPOSE */
        {
            return CommandCallback.Create(
                marshalFlags, callbackFlags | CallbackFlags.External,
                objectFlags, byRefArgumentFlags, interpreter,
                clientData, name, arguments, ref error);
        }

        ///////////////////////////////////////////////////////////////////////

#if DATA
        public static ReturnCode CreateDbConnection(
            Interpreter interpreter,
            IDbConnectionParameters dbConnectionParameters,
            ref IDbConnection connection,
            ref Result error
            ) /* DEADLOCK-ON-DISPOSE */
        {
            if (dbConnectionParameters == null)
            {
                error = "invalid database connection parameters";
                return ReturnCode.Error;
            }

            return DataOps.CreateDbConnection(interpreter,
                dbConnectionParameters.DbConnectionType,
                dbConnectionParameters.ConnectionString,
                dbConnectionParameters.AssemblyFileName,
                dbConnectionParameters.TypeFullName,
                dbConnectionParameters.TypeName,
                dbConnectionParameters.Type,
                dbConnectionParameters.ValueFlags,
                ref connection, ref error);
        }
#endif

        ///////////////////////////////////////////////////////////////////////

#if NATIVE && WINDOWS
        public static bool IsDebuggerPresent()
        {
            try
            {
                if (PlatformOps.IsWindowsOperatingSystem())
                    return NativeOps.SafeNativeMethods.IsDebuggerPresent();
            }
            catch (Exception e)
            {
                TraceOps.DebugTrace(
                    e, typeof(Utility).Name, TracePriority.External |
                    TracePriority.NativeError);
            }

            return false;
        }
#endif

        ///////////////////////////////////////////////////////////////////////

        public static HostCreateFlags GetHostCreateFlags(
            HostCreateFlags hostCreateFlags,
            bool useAttach,
            bool noColor,
            bool noTitle,
            bool noIcon,
            bool noProfile,
            bool noCancel
            )
        {
            return HostOps.GetCreateFlags(
                hostCreateFlags, useAttach, noColor, noTitle, noIcon,
                noProfile, noCancel);
        }

        ///////////////////////////////////////////////////////////////////////

        public static ReturnCode CopyAndWrapHost(
            Interpreter interpreter,
            Type type,
            ref IHost host,
            ref Result error
            ) /* DEADLOCK-ON-DISPOSE */
        {
            return HostOps.CopyAndWrap(
                interpreter, type, ref host, ref error);
        }

        ///////////////////////////////////////////////////////////////////////

        public static ReturnCode UnwrapAndDisposeHost(
            Interpreter interpreter,
            ref Result error
            ) /* DEADLOCK-ON-DISPOSE */
        {
            return HostOps.UnwrapAndDispose(interpreter, ref error);
        }

        ///////////////////////////////////////////////////////////////////////

        public static string GetOperatingSystemNameAndVersion()
        {
            return PlatformOps.GetOperatingSystemNameAndVersion();
        }

        ///////////////////////////////////////////////////////////////////////

        public static string GetRuntimeNameAndVersion()
        {
            return CommonOps.Runtime.GetRuntimeNameAndVersion();
        }

        ///////////////////////////////////////////////////////////////////////

        public static string GetVersion()
        {
            return _RuntimeOps.GetVersion(VersionFlags.Setup);
        }

        ///////////////////////////////////////////////////////////////////////

        public static bool IsUnixOperatingSystem()
        {
            return PlatformOps.IsUnixOperatingSystem();
        }

        ///////////////////////////////////////////////////////////////////////

        public static bool IsWindowsOperatingSystem()
        {
            return PlatformOps.IsWindowsOperatingSystem();
        }

        ///////////////////////////////////////////////////////////////////////

#if WINFORMS
        public static ReturnCode GetControlHandle( /* hWnd */
            Control control,
            ref IntPtr handle,
            ref Result error
            )
        {
            return FormOps.GetHandle(control, ref handle, ref error);
        }

        ///////////////////////////////////////////////////////////////////////

        public static ReturnCode GetMenuHandle( /* hMenu */
            Menu menu,
            ref IntPtr handle,
            ref Result error
            )
        {
            return FormOps.GetHandle(menu, ref handle, ref error);
        }
#endif

        ///////////////////////////////////////////////////////////////////////

#if NATIVE && WINDOWS
        public static ReturnCode ZeroMemory(
            IntPtr pMemory,
            uint size,
            ref Result error
            )
        {
            return NativeOps.ZeroMemory(pMemory, size, ref error);
        }
#endif

        ///////////////////////////////////////////////////////////////////////

#if ARGUMENT_CACHE
        public static int GetHashCode(
            byte[] array
            )
        {
            return ArrayOps.GetHashCode(array);
        }
#endif

        ///////////////////////////////////////////////////////////////////////

        public static int GetThreadTimeout(
            Interpreter interpreter,
            int? timeout,
            TimeoutType timeoutType
            ) /* SAFE-ON-DISPOSE */
        {
            return ThreadOps.GetTimeout(
                interpreter, timeout, timeoutType | TimeoutType.External);
        }

        ///////////////////////////////////////////////////////////////////////

        public static void CreateAndOrStartThread(
            Interpreter interpreter,
            string name,
            ParameterizedThreadStart start,
            object parameter,
            bool useThreadPool,
            int maxStackSize,
            bool userInterface,
            bool isBackground,
            bool useActiveStack,
            ref Thread thread
            ) /* SAFE-ON-DISPOSE */
        {
            ThreadOps.CreateAndOrStart(
                interpreter, name, start, parameter, useThreadPool,
                maxStackSize, userInterface, isBackground, useActiveStack,
                ref thread);
        }

        ///////////////////////////////////////////////////////////////////////

        public static void MaybeShutdownThread(
            Interpreter interpreter,
            int? timeout,
            ShutdownFlags flags,
            ref Thread thread
            ) /* SAFE-ON-DISPOSE */
        {
            ThreadOps.MaybeShutdown(
                interpreter, timeout, flags | ShutdownFlags.External,
                ref thread);
        }

        ///////////////////////////////////////////////////////////////////////

        public static bool QueueUserWorkItem(
            WaitCallback callBack
            )
        {
            return ThreadOps.QueueUserWorkItem(callBack, false);
        }

        ///////////////////////////////////////////////////////////////////////

        public static bool QueueUserWorkItem(
            WaitCallback callBack,
            QueueFlags flags
            )
        {
            return ThreadOps.QueueUserWorkItem(
                callBack, FlagOps.HasFlags(flags,
                QueueFlags.WaitForStart, true));
        }

        ///////////////////////////////////////////////////////////////////////

        public static bool QueueUserWorkItem(
            WaitCallback callBack,
            object state
            )
        {
            return ThreadOps.QueueUserWorkItem(callBack, state, false);
        }

        ///////////////////////////////////////////////////////////////////////

        public static bool QueueUserWorkItem(
            WaitCallback callBack,
            object state,
            QueueFlags flags
            )
        {
            return ThreadOps.QueueUserWorkItem(
                callBack, state, FlagOps.HasFlags(flags,
                QueueFlags.WaitForStart, true));
        }

        ///////////////////////////////////////////////////////////////////////

        public static HashAlgorithm CreateHashAlgorithm(
            string hashAlgorithmName,
            ref Result error
            )
        {
            return HashOps.CreateAlgorithm(hashAlgorithmName, ref error);
        }

        ///////////////////////////////////////////////////////////////////////

        public static string GetFactoryTypeName(
            string namespaceName,
            string typeName,
            Assembly assembly
            )
        {
            return FormatOps.GetQualifiedTypeFullName(
                namespaceName, typeName, assembly);
        }

        ///////////////////////////////////////////////////////////////////////

        public static Type LookupFactoryType(
            string typeName,
            bool allowFallback
            )
        {
            return FactoryOps.LookupType(typeName, allowFallback);
        }

        ///////////////////////////////////////////////////////////////////////

        public static object CreateViaFactory(
            Type type,
            ref Result error
            )
        {
            return FactoryOps.Create(type, ref error);
        }

        ///////////////////////////////////////////////////////////////////////

        public static string FormatTypeNameOrFullName(
            object @object
            )
        {
            return FormatOps.TypeNameOrFullName(@object);
        }

        ///////////////////////////////////////////////////////////////////////

        public static void ResetTraceStatus(
            Interpreter interpreter,
            bool overrideEnvironment
            )
        {
            TraceOps.ResetStatus(interpreter, overrideEnvironment);
        }

        ///////////////////////////////////////////////////////////////////////

        public static TraceStateType ForceTraceEnabledOrDisabled(
            Interpreter interpreter,
            TraceStateType stateType,
            bool enabled
            )
        {
            return TraceOps.ForceEnabledOrDisabled(
                interpreter, stateType, enabled);
        }

        ///////////////////////////////////////////////////////////////////////

        public static void AdjustTracePriority(
            ref TracePriority priority,
            int adjustment
            )
        {
            TraceOps.ExternalAdjustTracePriority(ref priority, adjustment);
        }

        ///////////////////////////////////////////////////////////////////////

        public static TracePriority GetTracePriorities()
        {
            return TraceOps.GetTracePriorities();
        }

        ///////////////////////////////////////////////////////////////////////

        public static void SetTracePriorities(
            TracePriority priorities
            )
        {
            TraceOps.SetTracePriorities(priorities);
        }

        ///////////////////////////////////////////////////////////////////////

        public static void AdjustTracePriorities(
            TracePriority priority,
            bool enabled
            )
        {
            TraceOps.AdjustTracePriorities(priority, enabled);
        }

        ///////////////////////////////////////////////////////////////////////

        public static bool GetObjectDefaultDispose()
        {
            return ObjectOps.GetDefaultDispose();
        }

        ///////////////////////////////////////////////////////////////////////

        public static bool GetObjectDefaultSynchronous()
        {
            return ObjectOps.GetDefaultSynchronous();
        }

        ///////////////////////////////////////////////////////////////////////

        public static ReturnCode DisposeOrComplain<T>(
            Interpreter interpreter,
            ref T @object
            )
        {
            return ObjectOps.DisposeOrComplain<T>(interpreter, ref @object);
        }

        ///////////////////////////////////////////////////////////////////////

        //
        // HACK: On .NET Core, calling AppDomain.GetAssemblies via reflection
        //       does not appear to work.  Calling this method via reflection
        //       does work.
        //
        public static IEnumerable<Assembly> GetAssemblies()
        {
            AppDomain appDomain = AppDomainOps.GetCurrent();

            if (appDomain == null)
                return null;

            return appDomain.GetAssemblies();
        }

        ///////////////////////////////////////////////////////////////////////

        public static string GetAppSetting(
            string name
            )
        {
            return ConfigurationOps.GetAppSetting(name);
        }

        ///////////////////////////////////////////////////////////////////////

        public static object GetResourceStreamData(
            Assembly assembly,
            string name,
            bool raw,
            ref Result error
            )
        {
            return AssemblyOps.GetResourceStreamData(
                assembly, name, null, raw, ref error);
        }

        ///////////////////////////////////////////////////////////////////////

        public static object GetResourceStreamData(
            Assembly assembly,
            string name,
            Encoding encoding,
            bool raw,
            ref Result error
            )
        {
            return AssemblyOps.GetResourceStreamData(
                assembly, name, encoding, raw, ref error);
        }

        ///////////////////////////////////////////////////////////////////////

        public static string NullObjectName()
        {
            return _Object.Null;
        }

        ///////////////////////////////////////////////////////////////////////

        public static Enum GetInstanceFlags(
            object @object,
            string memberName,
            bool noCase,
            ref Result error
            )
        {
            return EnumOps.GetFlags(
                @object, memberName, noCase, ref error);
        }

        ///////////////////////////////////////////////////////////////////////

        public static ReturnCode CheckAndMaybeModifyProcessReferenceCount(
            string prefix,
            CultureInfo cultureInfo,
            bool? increment,
            out int referenceCount,
            ref Result error
            )
        {
            return ProcessOps.CheckAndMaybeModifyReferenceCount(
                prefix, cultureInfo, increment, out referenceCount,
                ref error);
        }

        ///////////////////////////////////////////////////////////////////////

#if NATIVE && (WINDOWS || UNIX || UNSAFE)
        public static ReturnCode ChangeNativeCallback(
            NativeCallbackType callbackType,
            ref Delegate @delegate,
            ref Result error
            )
        {
            return NativeStack.ChangeCallback(
                callbackType, ref @delegate, ref error);
        }
#endif

        ///////////////////////////////////////////////////////////////////////

#if WEB
        public static void QueryFromDictionary(
            _StringDictionary dictionary,
            Encoding encoding,
            ref StringBuilder builder
            )
        {
            PathOps.QueryFromDictionary(
                dictionary, encoding, ref builder);
        }
#endif

        ///////////////////////////////////////////////////////////////////////

#if DATA
        public static string FixupDataValue(
            object value,
            IFormatValue formatValue
            ) /* throw */
        {
            return MarshalOps.FixupDataValue(
                value, formatValue.CultureInfo,
                formatValue.DateTimeBehavior,
                formatValue.DateTimeKind,
                formatValue.DateTimeFormat,
                formatValue.NumberFormat,
                formatValue.NullValue,
                formatValue.DbNullValue,
                formatValue.ErrorValue);
        }

        ///////////////////////////////////////////////////////////////////////

        public static ReturnCode DataReaderToList(
            Interpreter interpreter,
            IDataReader reader,
            IFormatDataValue formatDataValue,
            ref StringList list,
            ref int count,
            ref Result error
            )
        {
            if (formatDataValue == null)
            {
                error = "invalid data value format parameters";
                return ReturnCode.Error;
            }

            return DataOps.DataReaderToList(
                interpreter, reader,
                formatDataValue.CultureInfo,
                formatDataValue.DateTimeBehavior,
                formatDataValue.DateTimeKind,
                formatDataValue.DateTimeFormat,
                formatDataValue.NumberFormat,
                formatDataValue.NullValue,
                formatDataValue.DbNullValue,
                formatDataValue.ErrorValue,
                formatDataValue.Limit,
                formatDataValue.Nested,
                formatDataValue.Clear,
                formatDataValue.AllowNull,
                formatDataValue.Pairs,
                formatDataValue.Names,
                formatDataValue.NoFixup,
                ref list, ref count, ref error);
        }

        ///////////////////////////////////////////////////////////////////////

        public static ReturnCode DataReaderToArray(
            Interpreter interpreter,
            IDataReader reader,
            string varName,
            IFormatDataValue formatDataValue,
            ref int count,
            ref Result error
            )
        {
            if (formatDataValue == null)
            {
                error = "invalid data value format parameters";
                return ReturnCode.Error;
            }

            return DataOps.DataReaderToArray(
                interpreter, reader, varName,
                formatDataValue.CultureInfo,
                formatDataValue.DateTimeBehavior,
                formatDataValue.DateTimeKind,
                formatDataValue.DateTimeFormat,
                formatDataValue.NumberFormat,
                formatDataValue.NullValue,
                formatDataValue.DbNullValue,
                formatDataValue.ErrorValue,
                formatDataValue.Limit,
                formatDataValue.Clear,
                formatDataValue.AllowNull,
                formatDataValue.Pairs,
                formatDataValue.Names,
                formatDataValue.NoFixup,
                ref count, ref error);
        }
#endif

        ///////////////////////////////////////////////////////////////////////

#if NATIVE && WINDOWS
        public static ReturnCode ConsoleKeyboardString(
            CheckStringCallback stringCallback,
            IClientData clientData,
            string value,
            int milliseconds,
            SimulatedKeyFlags flags,
            ref Result error
            )
        {
            return NativeConsole.SimulateKeyboardString(
                stringCallback, clientData, value, milliseconds,
                flags, ref error);
        }
#endif

        ///////////////////////////////////////////////////////////////////////

        public static string MakeCommandName(
            string name
            )
        {
            return ScriptOps.MakeCommandName(name);
        }

        ///////////////////////////////////////////////////////////////////////

        public static bool GetPeFileDateTime(
            string fileName,
            ref DateTime dateTime
            )
        {
            return FileOps.GetPeFileDateTime(fileName, ref dateTime);
        }

        ///////////////////////////////////////////////////////////////////////

        #region .NET Core Wrapper Methods
        //
        // HACK: These wrapper methods are primarily for use by the test
        //       suite due to a bug in the .NET Core runtime, see:
        //
        //       https://github.com/dotnet/coreclr/issues/15662
        //
        public static Type GetType(
            string typeName
            )
        {
            return Type.GetType(typeName);
        }

        ///////////////////////////////////////////////////////////////////////

        public static Type GetType(
            string typeName,
            bool throwOnError,
            bool ignoreCase
            )
        {
            return Type.GetType(typeName, throwOnError, ignoreCase);
        }
        #endregion
        #endregion
    }
}
