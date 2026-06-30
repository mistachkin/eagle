/*
 * RuntimeOps.cs --
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
using System.Runtime.CompilerServices;

#if NATIVE && (NATIVE_UTILITY || TCL)
using System.Runtime.InteropServices;
#endif

#if NATIVE
using System.Security;
#endif

using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;

#if CAS_POLICY
using System.Security.Policy;
#endif

#if !NATIVE && !NET_STANDARD_20
using System.Security.Principal;
#endif

using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using Eagle._Attributes;
using Eagle._Components.Private.Delegates;
using Eagle._Components.Public;
using Eagle._Components.Public.Delegates;
using Eagle._Components.Shared;
using Eagle._Constants;
using Eagle._Containers.Private;
using Eagle._Containers.Public;
using Eagle._Encodings;
using Eagle._Interfaces.Private;
using Eagle._Interfaces.Public;
using SharedAttributeOps = Eagle._Components.Shared.AttributeOps;
using SharedStringOps = Eagle._Components.Shared.StringOps;

using IndexRange = Eagle._Components.Public.Pair<ulong>;

using IndexRangeList = System.Collections.Generic.List<
    Eagle._Components.Public.Pair<ulong>>;

using IndexDictionary = System.Collections.Generic.Dictionary<
    ulong, bool>;

using PluginPair = System.Collections.Generic.KeyValuePair<
    string, Eagle._Wrappers.Plugin>;

using DelegatePair = System.Collections.Generic.KeyValuePair<
    System.Delegate, Eagle._Components.Public.MethodFlags>;

#if EMIT && NATIVE && LIBRARY
using ModuleWrapper = Eagle._Wrappers._Module;
#endif

using FieldInfoDictionary = System.Collections.Generic.Dictionary<
    string, Eagle._Interfaces.Public.IAnyPair<
        System.Reflection.FieldInfo, object>>;

#if APPDOMAINS || ISOLATED_INTERPRETERS || ISOLATED_PLUGINS
using PluginResourceDictionary = System.Collections.Generic.Dictionary<string, byte[]>;
#endif

#if NET_STANDARD_21
using HashCode = Eagle._Constants.HashCode;
using Index = Eagle._Constants.Index;
#endif

namespace Eagle._Components.Private
{
    /// <summary>
    /// This class provides a collection of miscellaneous, low-level runtime
    /// operations used throughout the Eagle core.  These helpers cover such
    /// areas as native stack checking, hashing and trust/signature
    /// verification, resource and version handling, the population of
    /// plugins, commands, operators, and functions, and the generation of
    /// random data.  It is an internal, static utility class and is not
    /// intended for direct use by application code.
    /// </summary>
    [ObjectId("52155f4f-322b-4389-aacd-166fe334d164")]
    internal static class RuntimeOps
    {
        #region Synchronization Objects
        /// <summary>
        /// The object used to synchronize access to the static state of this
        /// class.
        /// </summary>
        private static readonly object syncRoot = new object();
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Private Constants
        #region Property Value Defaults
        /// <summary>
        /// When non-zero, an exception is thrown when a requested feature is
        /// not supported by the current runtime.
        /// </summary>
        private static readonly bool ThrowOnFeatureNotSupported = true;
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Encoding Constants
        //
        // WARNING: Do not change this as it must be a pass-through one-byte
        //          per character encoding.
        //
        /// <summary>
        /// The encoding used to read and write raw bytes; this must be a
        /// pass-through, one-byte-per-character encoding.
        /// </summary>
        private static readonly Encoding RawEncoding = OneByteEncoding.OneByte;
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Resource Handling
        //
        // NOTE: These five strings must be compile time constants because
        //       they are used before the cultureInfo and resourceManager
        //       objects are available to resolve runtime string resources.
        //
        /// <summary>
        /// The error message format used when a value cannot be interpreted as
        /// a culture name or identifier.
        /// </summary>
        private const string CultureInfoError =
            "could not interpret {0} as a culture name or identifier";

        /// <summary>
        /// The error message used when a culture is invalid.
        /// </summary>
        private const string InvalidCultureInfoError =
            "invalid culture";

        /// <summary>
        /// The error message used when a base resource name is invalid.
        /// </summary>
        private const string InvalidBaseResourceName =
            "invalid base resource name";

        /// <summary>
        /// The error message used when a resource assembly is invalid.
        /// </summary>
        private const string InvalidResourceAssembly =
            "invalid resource assembly";

        /// <summary>
        /// The error message format used when a resource manager cannot be
        /// created.
        /// </summary>
        private const string ResourceManagerError =
            "could not create resource manager {0}";

        /// <summary>
        /// The error message used when an interpreter resource manager is
        /// invalid.
        /// </summary>
        private const string InvalidInterpreterResourceManager =
            "invalid interpreter resource manager";

        /// <summary>
        /// The error message used when a plugin resource manager is invalid.
        /// </summary>
        private const string InvalidPluginResourceManager =
            "invalid plugin resource manager";

        /// <summary>
        /// The error message used when an assembly resource manager is
        /// invalid.
        /// </summary>
        private const string InvalidAssemblyResourceManager =
            "invalid assembly resource manager";

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// The format string used to construct the name of the symbols
        /// resource associated with a given base resource name.
        /// </summary>
        private static readonly string SymbolsFormat = "{0}_Symbols";
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Native Pointer Handling
        /// <summary>
        /// The native pointer value used to represent an invalid handle.
        /// </summary>
        private static readonly IntPtr INVALID_HANDLE_VALUE = new IntPtr(-1);
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Compile Options Constants
        /// <summary>
        /// The name of the compile-time option that indicates threading
        /// support is enabled.
        /// </summary>
        private const string ThreadingDefineName = "THREADING";

        /// <summary>
        /// The name of the compile-time option that indicates native code
        /// support is enabled.
        /// </summary>
        private const string NativeDefineName = "NATIVE";
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Integer Range List Constants
        /// <summary>
        /// The regular expression used to validate and parse a string
        /// containing a comma-separated list of integer ranges.
        /// </summary>
        private static readonly Regex indexRangesRegEx = RegExOps.Create(
            "^(?:[ ]*\\d+(?:[ ]*-[ ]*\\d+)?" +
            "(?:[ ]*,[ ]*\\d+(?:[ ]*-[ ]*\\d+)?)*)?[ ]*$",
            RegexOptions.Singleline | RegexOptions.CultureInvariant);
        #endregion
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Private Data
        #region Native Stack Checking
#if NATIVE
        //
        // NOTE: When this is non-zero, the environment variable that is
        //       used to disable native stack checking has already been
        //       checked.
        //
        // HACK: This is purposely not read-only.
        //
        /// <summary>
        /// When non-zero, the environment variable used to disable native
        /// stack checking has already been checked.
        /// </summary>
        private static int checkedNoNativeStack = 0;

        ///////////////////////////////////////////////////////////////////////

        //
        // NOTE: When this is non-zero, all native stack checking will be
        //       disabled.
        //
        // HACK: This is purposely not read-only.
        //
        /// <summary>
        /// When non-zero, all native stack checking will be disabled.
        /// </summary>
        private static int noNativeStack = 0;

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// The thread-specific data slot used to store the base pointer of the
        /// native stack for the current thread.
        /// </summary>
        private static LocalDataStoreSlot stackPtrSlot; /* ThreadSpecificData */

        /// <summary>
        /// The thread-specific data slot used to store the size of the native
        /// stack for the current thread.
        /// </summary>
        private static LocalDataStoreSlot stackSizeSlot; /* ThreadSpecificData */

        //
        // NOTE: The number of nesting levels before we start checking
        //       native stack space.
        //
        // TODO: We really need to adjust these numbers dynamically
        //       depending on the maximum stack size of the thread.
        //
        // HACK: These are no longer read-only.
        //
        /// <summary>
        /// The number of nesting levels permitted before native stack space is
        /// checked during general evaluation.
        /// </summary>
        private static int NoStackLevels = 100;

        /// <summary>
        /// The number of nesting levels permitted before native stack space is
        /// checked during parsing.
        /// </summary>
        private static int NoStackParserLevels = 100;

        /// <summary>
        /// The number of nesting levels permitted before native stack space is
        /// checked during expression evaluation.
        /// </summary>
        private static int NoStackExpressionLevels = 100;
#endif
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Interpreter Locking
#if DEBUG
        //
        // HACK: This is purposely not read-only.
        //
        /// <summary>
        /// When non-zero, interpreters are checked for disposal when the exit
        /// lock is taken.
        /// </summary>
        private static bool CheckDisposedOnExitLock = false;
#endif
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Update Checking
#if MONO || MONO_HACKS
        //
        // HACK: *MONO* Just in case Mono eventually fixes the crash issue,
        //       allow this static field to be preset to bypass the runtime
        //       check.
        //
        /// <summary>
        /// When non-zero, the runtime check that would otherwise bypass a
        /// Mono-specific code path is skipped, forcing Mono behavior.
        /// </summary>
        private static bool forceMono = false;
#endif
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Random Number Support
        //
        // NOTE: Cached instance of cryptographic random number generator.
        //
        /// <summary>
        /// The cached instance of the cryptographic random number generator
        /// used to produce random data.
        /// </summary>
        private static RandomNumberGenerator randomNumberGenerator;
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Loader Exception Handling
        //
        // HACK: When this is non-zero, any loader related exceptions that
        //       are encountered by this class will be reported in detail.
        //
        /// <summary>
        /// When non-zero, any loader-related exceptions encountered by this
        /// class will be reported in detail.
        /// </summary>
        private static bool VerboseExceptions = true;
        #endregion
        #endregion

        ///////////////////////////////////////////////////////////////////////

#if NATIVE
        #region Static Constructor
        /// <summary>
        /// Initializes static state for the <see cref="RuntimeOps" /> class.
        /// This constructor performs any one-time initialization required
        /// before the native stack checking subsystem can be used.
        /// </summary>
        static RuntimeOps()
        {
            MaybeInitialize();
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region AppDomain Initialization
        /// <summary>
        /// This method performs one-time initialization of the native stack
        /// checking state, disabling native stack checking if the associated
        /// environment variable is present.  It has no effect after the first
        /// call.
        /// </summary>
        public static void MaybeInitialize()
        {
            if (Interlocked.CompareExchange(
                    ref checkedNoNativeStack, 1, 0) == 0)
            {
                if (CommonOps.Environment.DoesVariableExist(
                        EnvVars.NoNativeStack))
                {
                    /* IGNORED */
                    EnableNativeStack(false);
                }
            }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method determines whether native stack checking is currently
        /// enabled.
        /// </summary>
        /// <returns>
        /// True if native stack checking is enabled; otherwise, false.
        /// </returns>
        private static bool IsNativeStackEnabled()
        {
            return Interlocked.CompareExchange(ref noNativeStack, 0, 0) <= 0;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method enables or disables native stack checking, maintaining a
        /// nesting count so that paired enable and disable calls balance.
        /// </summary>
        /// <param name="enable">
        /// Non-zero to enable native stack checking; zero to disable it.
        /// </param>
        /// <returns>
        /// True if native stack checking is enabled after this call; otherwise,
        /// false.
        /// </returns>
        private static bool EnableNativeStack(
            bool enable
            )
        {
            if (enable)
                return Interlocked.Decrement(ref noNativeStack) <= 0;
            else
                return Interlocked.Increment(ref noNativeStack) > 0;
        }
        #endregion
#endif

        ///////////////////////////////////////////////////////////////////////

        #region Object Support Methods
        /// <summary>
        /// This method computes the runtime hash code for the specified object,
        /// based on its identity rather than any overridden hash code.
        /// </summary>
        /// <param name="value">
        /// The object for which to compute the runtime hash code.
        /// </param>
        /// <returns>
        /// The identity-based hash code for the specified object.
        /// </returns>
        public static int GetHashCode(object value)
        {
            return RuntimeHelpers.GetHashCode(value);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method computes the runtime hash code for the value wrapped by
        /// the specified opaque object, based on its identity.
        /// </summary>
        /// <param name="object">
        /// The opaque object whose wrapped value is used to compute the runtime
        /// hash code.  This value may be null.
        /// </param>
        /// <returns>
        /// The identity-based hash code for the wrapped value, or
        /// <see cref="HashCode.Invalid" /> if the specified object is null.
        /// </returns>
        public static int GetHashCode(IObject @object)
        {
            if (@object == null)
                return HashCode.Invalid;

            return GetHashCode(@object.Value);
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Process Support Methods
        /// <summary>
        /// This method causes the current process to exit, optionally giving
        /// the interpreter host an opportunity to prevent or customize the
        /// shutdown.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter associated with the exit request, if any.  This
        /// value may be null.
        /// </param>
        /// <param name="clientData">
        /// Optional, opaque, caller-specific data associated with the exit
        /// request.  This value may be null.
        /// </param>
        /// <param name="arguments">
        /// The list of arguments associated with the exit request, if any.
        /// This value may be null.
        /// </param>
        /// <param name="message">
        /// An optional message describing the reason for the exit.  This value
        /// may be null.
        /// </param>
        /// <param name="exitCode">
        /// The exit code to report to the operating system upon exit.
        /// </param>
        /// <param name="force">
        /// Non-zero to force the process to exit even if the interpreter host
        /// would otherwise prevent it.
        /// </param>
        /// <param name="fail">
        /// Non-zero to terminate the process abruptly instead of performing a
        /// graceful exit.
        /// </param>
        /// <param name="noDispose">
        /// Non-zero to skip disposing of the interpreter prior to exiting.
        /// </param>
        /// <param name="noComplain">
        /// Non-zero to suppress error reporting that would otherwise occur
        /// during the exit.
        /// </param>
        /// <param name="error">
        /// Upon failure, this contains an appropriate error message.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise, an appropriate
        /// error code.
        /// </returns>
        public static ReturnCode Exit(
            Interpreter interpreter, /* in: OPTIONAL */
            IClientData clientData,  /* in: OPTIONAL */
            ArgumentList arguments,  /* in: OPTIONAL */
            string message,          /* in: OPTIONAL */
            ExitCode exitCode,       /* in */
            bool force,              /* in */
            bool fail,               /* in */
            bool noDispose,          /* in */
            bool noComplain,         /* in */
            ref Result error         /* out */
            )
        {
            //
            // NOTE: Give the interpreter host, if any, an opportunity to
            //       prevent the interpreter from exiting.  This might be
            //       necessary if the application is doing something that
            //       cannot be gracefully interrupted.
            //
            if ((interpreter != null) && (interpreter.CanExit(exitCode,
                    force, fail, message, ref error) != ReturnCode.Ok))
            {
                return ReturnCode.Error;
            }

            //
            // NOTE: Exit the application (either by marking the current
            //       interpreter as "exited" or by physically exiting the
            //       containing process).
            //
            TraceOps.DebugTrace(String.Format(
                "Exit: {0}, interpreter = {1}, message = {2}", force &&
                fail ? "forcibly failing" : force ? "forcibly exiting" :
                "exiting", FormatOps.InterpreterNoThrow(interpreter),
                FormatOps.WrapOrNull(message)), typeof(RuntimeOps).Name,
                TracePriority.Command);

            ///////////////////////////////////////////////////////////////////

#if DEBUGGER
            //
            // HACK: If there is an active interpreter and its NoExit state
            //       flag has been set, i.e. via command line, etc, attempt
            //       to break into the (already available?) script debugger
            //       now.  If there is an error, block the [exit] request.
            //       This implies that one of the following statements must
            //       always be true in order to successfully exit:
            //
            //       1. The NoExit state flag cannot be set and/or must be
            //          unset before this method is called.
            //
            //       2. Script debugger must be available, functional, and
            //          the interactive loop must return success.
            //
            //       This is considered to be a "fail-safe" mechanism from
            //       the perspective of scripts and should not be changed.
            //
            if ((interpreter != null) && interpreter.HasNoExit())
            {
                IDebugger debugger = null;
                HeaderFlags headerFlags = HeaderFlags.None;

                if (!Engine.CheckDebugger(
                        interpreter, interpreter.HasIgnoreEnabled(),
                        ref debugger, ref headerFlags, ref error))
                {
                    return ReturnCode.Error;
                }

                string breakpointName = typeof(_Commands.Exit).FullName;

                if (interpreter.DebuggerBreak(
                        debugger, new InteractiveLoopData(ReturnCode.Ok,
                        BreakpointType.InterceptExit, breakpointName,
                        headerFlags | HeaderFlags.Breakpoint, clientData,
                        arguments), ref error) != ReturnCode.Ok)
                {
                    return ReturnCode.Error;
                }
            }
#endif

            ///////////////////////////////////////////////////////////////////

            if (!force)
            {
                //
                // NOTE: When not forcibly exiting the current process,
                //       the interpreter will be marked as "exited" and
                //       unavailable to scripts.  This can be undone at
                //       a later time by unsetting the "exited" flag.
                //
                if (interpreter != null)
                {
                    interpreter.ExitCodeNoThrow = exitCode;
                    interpreter.ExitNoThrow = true;

                    return ReturnCode.Ok;
                }
                else
                {
                    error = "invalid interpreter";
                    return ReturnCode.Error;
                }
            }

#if SHELL
            //
            // HACK: Forbid ANY attempt to exit the process itself when
            //       the interpreter is operating in kiosk mode.  This
            //       does NOT prevent the interpreter itself from being
            //       marked as exited (see above).
            //
            if ((interpreter != null) && interpreter.IsKioskLock())
            {
                error = "cannot forcibly exit when a kiosk";
                return ReturnCode.Error;
            }
#endif

#if !MONO
            if (fail)
            {
                if (CommonOps.Runtime.IsMono())
                {
                    if (message != null)
                    {
                        DebugOps.Complain(
                            interpreter, ReturnCode.Error, message);
                    }
                }
                else
                {
                    try
                    {
                        //
                        // NOTE: Using this method to exit a script is
                        //       NOT recommended unless you are trying
                        //       to prevent damaging another part of
                        //       the system.
                        //
                        // MONO: This is (apparently?) not supported by
                        //       the Mono runtime.
                        //
                        Environment.FailFast(message);

                        /* NOT REACHED */
                        error = "failed to fail-fast process";
                    }
                    catch (Exception e)
                    {
                        error = e;
                    }

                    return ReturnCode.Error;
                }
            }
#endif

            //
            // BUGFIX: Try to dispose the containing interpreter now.  In
            //         general, commands should not do this; however, the
            //         script engine will detect this condition and halt
            //         the script in progress.  Also, we must do this to
            //         prevent it from being disposed on a (semi-random)
            //         GC thread (i.e. just in case it is hosting native
            //         resources that have thread affinity).
            //
            // TODO: Should this really be skipped when operating in the
            //       fail-fast mode?  Especially, since this point would
            //       ONLY be reached if that handling somehow failed AND
            //       did NOT throw an exception (OR was skipped due to
            //       running on Mono) above.
            //
            if (!fail && !noDispose)
            {
                try
                {
                    if (interpreter != null)
                    {
                        interpreter.Dispose(); /* throw */
                        interpreter = null;
                    }
                }
                catch (Exception e)
                {
                    if (!noComplain)
                    {
                        DebugOps.Complain(
                            interpreter, ReturnCode.Error, e);
                    }
                }
            }

            try
            {
                //
                // NOTE: Using this method to exit a script is generally
                //       NOT recommended unless it is a standalone script
                //       running in the Eagle Shell (i.e. not hosted in a
                //       larger application).
                //
                Environment.Exit((int)exitCode);

                /* NOT REACHED */
                error = "failed to exit process";
            }
            catch (Exception e)
            {
                error = e;
            }

            return ReturnCode.Error;
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Native Stack Checking Support Methods
        /// <summary>
        /// This method retrieves the native stack size information that is
        /// currently cached for the calling thread.
        /// </summary>
        /// <param name="used">
        /// Upon success, this contains the approximate amount of native stack
        /// space used by the calling thread.
        /// </param>
        /// <param name="allocated">
        /// Upon success, this contains the approximate amount of native stack
        /// space allocated for the calling thread.
        /// </param>
        /// <param name="extra">
        /// Upon success, this contains the amount of extra native stack space
        /// required by the calling thread.
        /// </param>
        /// <param name="margin">
        /// Upon success, this contains the safety margin (overhead) of native
        /// stack space reserved for use by the runtime.
        /// </param>
        /// <param name="maximum">
        /// Upon success, this contains the maximum amount of native stack
        /// space available to the calling thread.
        /// </param>
        /// <param name="reserve">
        /// Upon success, this contains the amount of native stack space
        /// reserved according to the executable (PE) file for this process.
        /// </param>
        /// <param name="commit">
        /// Upon success, this contains the amount of native stack space
        /// committed according to the executable (PE) file for this process.
        /// </param>
        /// <param name="error">
        /// Upon failure, this contains an error message that describes why the
        /// native stack size information could not be retrieved.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise, an appropriate
        /// error code.
        /// </returns>
        public static ReturnCode GetStackSize(
            ref UIntPtr used,
            ref UIntPtr allocated,
            ref UIntPtr extra,
            ref UIntPtr margin,
            ref UIntPtr maximum,
            ref UIntPtr reserve,
            ref UIntPtr commit,
            ref Result error
            ) /* ENTRY-POINT, THREAD-SAFE */
        {
#if NATIVE
            lock (syncRoot) /* TRANSACTIONAL */
            {
                if (stackSizeSlot != null)
                {
                    try
                    {
                        /* THREAD-SAFE, per-thread data */
                        NativeStack.StackSize stackSize = Thread.GetData(
                            stackSizeSlot) as NativeStack.StackSize; /* throw */

                        if (stackSize != null)
                        {
                            used = stackSize.used;
                            allocated = stackSize.allocated;
                            extra = stackSize.extra;
                            margin = stackSize.margin;
                            maximum = stackSize.maximum;
                            reserve = stackSize.reserve;
                            commit = stackSize.commit;

                            return ReturnCode.Ok;
                        }
                        else
                        {
                            error = "thread stack size is invalid";
                        }
                    }
                    catch (Exception e)
                    {
                        error = e;
                    }
                }
                else
                {
                    error = "thread stack size slot is invalid";
                }
            }
#else
            error = "not implemented";
#endif

            return ReturnCode.Error;
        }

        ///////////////////////////////////////////////////////////////////////

#if NATIVE
        /// <summary>
        /// This method initializes the per-thread data slots used by the
        /// native stack checking subsystem, if they have not already been
        /// allocated.  It must be called prior to evaluating scripts in order
        /// for runtime stack checking to function properly.
        /// </summary>
        public static void MaybeInitializeStackChecking()
        {
            #region Native Stack Checking Thread Local Storage
            lock (syncRoot) /* TRANSACTIONAL */
            {
                //
                // NOTE: These MUST to be done prior to evaluating scripts
                //       or runtime stack checking will not work properly
                //       (which can potentially cause scripts that use deep
                //       recursion to cause a .NET exception to be thrown
                //       from the script engine itself because the script
                //       engine depends upon runtime stack checking working
                //       properly).
                //
                int count = 0;

                if (stackPtrSlot == null)
                {
                    stackPtrSlot = Thread.AllocateDataSlot();
                    count++;
                }

                if (stackSizeSlot == null)
                {
                    stackSizeSlot = Thread.AllocateDataSlot();
                    count++;
                }

                if (count > 0)
                {
                    TraceOps.DebugTrace(String.Format(
                        "MaybeInitializeStackChecking: count = {0}", count),
                        typeof(RuntimeOps).Name, TracePriority.ThreadDebug2);
                }
            }
            #endregion
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method disposes of the cached native stack pointer and stack
        /// size information for the calling thread, clearing the associated
        /// per-thread data slots.  The data is automatically re-created later
        /// if it is still required.
        /// </summary>
        public static void MaybeFinalizeStackChecking()
        {
            #region Native Stack Checking Thread Local Storage
            lock (syncRoot) /* TRANSACTIONAL */
            {
                //
                // NOTE: Dispose the cached stack pointer and size information
                //       for this thread.  It is "mostly harmless" to do this
                //       even if is still required by another interpreter in
                //       this thread because it will automatically re-created
                //       in that case.  The alternative is to never dispose of
                //       this data.
                //
                int count = 0;

                if (stackPtrSlot != null)
                {
                    try
                    {
                        object stackPtrData = Thread.GetData(
                            stackPtrSlot); /* throw */

                        if (stackPtrData != null)
                        {
                            //
                            // NOTE: Remove our local reference to the data.
                            //
                            stackPtrData = null;

                            //
                            // NOTE: Clear out the data value for this thread.
                            //
                            Thread.SetData(
                                stackPtrSlot, stackPtrData); /* throw */

                            count++;
                        }
                    }
                    catch (Exception e)
                    {
                        TraceOps.DebugTrace(
                            e, typeof(RuntimeOps).Name,
                            TracePriority.ThreadError);
                    }
                }

                ///////////////////////////////////////////////////////////////

                if (stackSizeSlot != null)
                {
                    try
                    {
                        object stackSizeData = Thread.GetData(
                            stackSizeSlot); /* throw */

                        if (stackSizeData != null)
                        {
                            //
                            // NOTE: Remove our local reference to the data.
                            //
                            stackSizeData = null;

                            //
                            // NOTE: Clear out the data value for this thread.
                            //
                            Thread.SetData(
                                stackSizeSlot, stackSizeData); /* throw */

                            count++;
                        }
                    }
                    catch (Exception e)
                    {
                        TraceOps.DebugTrace(
                            e, typeof(RuntimeOps).Name,
                            TracePriority.ThreadError);
                    }
                }

                ///////////////////////////////////////////////////////////////

                if (count > 0)
                {
                    TraceOps.DebugTrace(String.Format(
                        "MaybeFinalizeStackChecking: count = {0}", count),
                        typeof(RuntimeOps).Name, TracePriority.ThreadDebug2);
                }
            }
            #endregion
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method calculates the approximate amount of native stack space
        /// that has been used between two stack pointers, automatically
        /// detecting the direction in which the stack is growing.
        /// </summary>
        /// <param name="outerStackPtr">
        /// The native stack pointer captured nearer to the start (outermost
        /// level) of script execution.
        /// </param>
        /// <param name="innerStackPtr">
        /// The native stack pointer captured nearer to the current (innermost
        /// level) of script execution.
        /// </param>
        /// <returns>
        /// The approximate amount of native stack space used between the two
        /// specified stack pointers.
        /// </returns>
        private static UIntPtr CalculateUsedStackSpace(
            UIntPtr outerStackPtr,
            UIntPtr innerStackPtr
            )
        {
            //
            // NOTE: Attempt to automatically detect which way the stack
            //       is growing and then calculate the approximate amount
            //       of space that has been used so far.
            //
            if (outerStackPtr.ToUInt64() > innerStackPtr.ToUInt64())
            {
                return new UIntPtr(
                    outerStackPtr.ToUInt64() - innerStackPtr.ToUInt64());
            }
            else
            {
                return new UIntPtr(
                    innerStackPtr.ToUInt64() - outerStackPtr.ToUInt64());
            }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method calculates the approximate amount of native stack space
        /// that is needed in order to safely continue script execution,
        /// combining the used space, the requested extra space, and the safety
        /// margin.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter whose configured extra stack space should be
        /// included in the calculation, or null if there is no associated
        /// interpreter.
        /// </param>
        /// <param name="extraSpace">
        /// The additional amount of native stack space requested by the
        /// caller.
        /// </param>
        /// <param name="usedSpace">
        /// The approximate amount of native stack space that has already been
        /// used.
        /// </param>
        /// <param name="stackMargin">
        /// The safety margin (overhead) of native stack space that should be
        /// kept in reserve.
        /// </param>
        /// <returns>
        /// The approximate total amount of native stack space needed.
        /// </returns>
        private static UIntPtr CalculateNeededStackSpace(
            Interpreter interpreter,
            ulong extraSpace,
            UIntPtr usedSpace,
            UIntPtr stackMargin
            )
        {
            ulong interpreterExtraSpace = (interpreter != null) ?
                interpreter.InternalExtraStackSpace : 0;

            return new UIntPtr(
                interpreterExtraSpace + extraSpace +
                usedSpace.ToUInt64() + stackMargin.ToUInt64());
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method determines whether the native stack space should be
        /// checked, based on the specified ready flags and the various nesting
        /// levels that have been reached thus far.
        /// </summary>
        /// <param name="flags">
        /// The ready flags that control whether and how native stack checking
        /// is performed.
        /// </param>
        /// <param name="levels">
        /// The current number of script execution levels.
        /// </param>
        /// <param name="maximumLevels">
        /// The maximum number of script execution levels reached thus far.
        /// </param>
        /// <param name="parserLevels">
        /// The current number of script parser levels.
        /// </param>
        /// <param name="maximumParserLevels">
        /// The maximum number of script parser levels reached thus far.
        /// </param>
        /// <param name="expressionLevels">
        /// The current number of expression evaluation levels.
        /// </param>
        /// <param name="maximumExpressionLevels">
        /// The maximum number of expression evaluation levels reached thus
        /// far.
        /// </param>
        /// <returns>
        /// True if the native stack space should be checked; otherwise, false.
        /// </returns>
        public static bool ShouldCheckForStackSpace(
            ReadyFlags flags,
            int levels,
            int maximumLevels,
            int parserLevels,
            int maximumParserLevels,
            int expressionLevels,
            int maximumExpressionLevels
            )
        {
            //
            // NOTE: If native stack checking was not requested -OR- has
            //       been explicitly disabled, just skip it.
            //
            if (FlagOps.HasFlags(flags, ReadyFlags.NoStack, true) ||
                !FlagOps.HasFlags(flags, ReadyFlags.CheckStack, true))
            {
                return false;
            }

            //
            // NOTE: If this is a thread-pool thread, skip checking its
            //       stack if that was not requested -OR- it has been
            //       explicitly disabled.
            //
            if ((FlagOps.HasFlags(flags, ReadyFlags.NoPoolStack, true) ||
                !FlagOps.HasFlags(flags, ReadyFlags.ForcePoolStack, true)) &&
                ThreadOps.IsCurrentPool())
            {
                return false;
            }

            //
            // NOTE: Otherwise, if native stack checking is being forced,
            //       just do it.
            //
            if (FlagOps.HasFlags(flags, ReadyFlags.ForceStack, true))
                return true;

            //
            // NOTE: Are we supposed to check (or ignore?) the maximum
            //       levels reached thus far?
            //
            bool checkLevels = FlagOps.HasFlags(
                flags, ReadyFlags.CheckLevels, true);

            //
            // NOTE: Otherwise, if we have exceeded the number of script
            //       execution levels that require no native stack check,
            //       do it.
            //
            if ((levels > NoStackLevels) &&
                (!checkLevels || (levels >= maximumLevels)))
            {
                return true;
            }

            //
            // NOTE: Otherwise, if we have exceeded the number of script
            //       parser levels that require no native stack check,
            //       do it.
            //
            if ((parserLevels > NoStackParserLevels) &&
                (!checkLevels || (parserLevels >= maximumParserLevels)))
            {
                return true;
            }

            //
            // NOTE: Otherwise, if we have exceeded the number of script
            //       expression levels that require no native stack check,
            //       do it.
            //
            if ((expressionLevels > NoStackExpressionLevels) &&
                (!checkLevels || (expressionLevels >= maximumExpressionLevels)))
            {
                return true;
            }

            //
            // NOTE: Otherwise, skip it.
            //
            return false;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method refreshes the cached native stack pointers for the
        /// calling thread, optionally initializing the native stack checking
        /// subsystem beforehand.
        /// </summary>
        /// <param name="initialize">
        /// Non-zero to initialize the native stack checking data slots prior
        /// to refreshing the stack pointers.
        /// </param>
        public static void RefreshNativeStackPointers(
            bool initialize
            )
        {
            UIntPtr innerStackPtr = UIntPtr.Zero;
            UIntPtr outerStackPtr = UIntPtr.Zero;

            RefreshNativeStackPointers(
                initialize, ref innerStackPtr, ref outerStackPtr);

#if false
            TraceOps.DebugTrace(String.Format(
                "RefreshNativeStackPointers: initialize = {0}, " +
                "innerStackPtr = {1}, outerStackPtr = {2}", initialize,
                innerStackPtr, outerStackPtr), typeof(RuntimeOps).Name,
                TracePriority.ThreadDebug5);
#endif
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method refreshes the cached native stack pointers for the
        /// calling thread, optionally initializing the native stack checking
        /// subsystem beforehand, and returns the resulting inner and outer
        /// stack pointers to the caller.
        /// </summary>
        /// <param name="initialize">
        /// Non-zero to initialize the native stack checking data slots prior
        /// to refreshing the stack pointers.
        /// </param>
        /// <param name="innerStackPtr">
        /// Upon return, this contains the current (innermost) native stack
        /// pointer for the calling thread.
        /// </param>
        /// <param name="outerStackPtr">
        /// Upon return, this contains the saved (outermost) native stack
        /// pointer for the calling thread.
        /// </param>
        private static void RefreshNativeStackPointers(
            bool initialize,
            ref UIntPtr innerStackPtr,
            ref UIntPtr outerStackPtr
            )
        {
            lock (syncRoot) /* TRANSACTIONAL */
            {
                //
                // NOTE: If requested by the caller, initialize the stack
                //       slots prior to doing anything else.
                //
                if (initialize)
                    MaybeInitializeStackChecking();

                ///////////////////////////////////////////////////////////////

                //
                // NOTE: Make sure we have our AppDomain-wide thread data
                //       slot.
                //
                if (stackPtrSlot == null)
                    return;

                //
                // NOTE: If the native stack checking subsystem is disabled,
                //       just do nothing.
                //
                if (!IsNativeStackEnabled())
                    return;

                //
                // NOTE: Get the current native stack pointer (so that we
                //       know approximately where in the stack we currently
                //       are).
                //
                innerStackPtr = NativeStack.GetNativeStackPointer();

                //
                // NOTE: Get previously saved outer native stack pointer,
                //       if any.
                //
                /* THREAD-SAFE, per-thread data */
                object stackPtrData = Thread.GetData(stackPtrSlot); /* throw */

                //
                // NOTE: If we got a valid saved outer stack pointer value
                //       from the thread data slot, it should be a UIntPtr;
                //       otherwise, set it to zero (first time through) so
                //       that the current inner stack pointer will be saved
                //       into it for later use.
                //
                outerStackPtr = (stackPtrData is UIntPtr) ?
                    (UIntPtr)stackPtrData : UIntPtr.Zero;

                //
                // NOTE: If it was not previously saved, save it now.
                //
                if (outerStackPtr == UIntPtr.Zero)
                {
                    //
                    // NOTE: This must be the first time through, set the
                    //       outer stack pointer value equal to the current
                    //       stack pointer value and then save it for later
                    //       use.
                    //
                    outerStackPtr = innerStackPtr;

                    /* THREAD-SAFE, per-thread data */
                    Thread.SetData(stackPtrSlot, outerStackPtr); /* throw */
                }
            }
        }

        ///////////////////////////////////////////////////////////////////////

        //
        // NOTE: This method assumes the associated lock is held.
        //
        /// <summary>
        /// This method creates or updates the cached native stack size
        /// information for the calling thread, refreshing the used, allocated,
        /// maximum, and margin values as appropriate.  The associated lock
        /// must already be held by the caller.
        /// </summary>
        /// <param name="extraSpace">
        /// The amount of extra native stack space requested by the caller.
        /// </param>
        /// <param name="usedSpace">
        /// The approximate amount of native stack space that has already been
        /// used by the calling thread.
        /// </param>
        /// <returns>
        /// The created or updated native stack size object for the calling
        /// thread, which may be null.
        /// </returns>
        private static NativeStack.StackSize CreateOrUpdateStackSize(
            ulong extraSpace,
            UIntPtr usedSpace
            )
        {
            //
            // NOTE: Get the stack size object for this thread.  If it is
            //       invalid or has not been created yet, we will create
            //       or reset it now.
            //
            /* THREAD-SAFE, per-thread data */
            NativeStack.StackSize stackSize = Thread.GetData(
                stackSizeSlot) as NativeStack.StackSize; /* throw */

            //
            // NOTE: If it was not previously saved, save it now.
            //
            if (stackSize == null)
            {
                stackSize = new NativeStack.StackSize();

                /* THREAD-SAFE, per-thread data */
                Thread.SetData(stackSizeSlot, stackSize); /* throw */

                //
                // NOTE: Emit a diagnostic message with the new native
                //       stack information.  Initially, this may have
                //       a bunch of zero values (or not?).
                //
                TraceOps.DebugTrace(String.Format(
                    "CreateOrUpdateStackSize: created {0}", stackSize),
                    typeof(RuntimeOps).Name, TracePriority.ThreadDebug2);
            }

            //
            // NOTE: Update stack size object for this thread with the
            //       requested amount of extra space.
            //
            stackSize.extra = new UIntPtr(extraSpace);

            //
            // NOTE: First, update the stack size object for this thread
            //       with the amount of used space.
            //
            stackSize.used = usedSpace;

            //
            // NOTE: If the native stack checking subsystem is disabled,
            //       just return the cached stack size data (even if it
            //       happens to be null).
            //
            if (!IsNativeStackEnabled())
                return stackSize;

            //
            // NOTE: Next, update the stack size object for this thread
            //       with the amount of space allocated (because this
            //       number grows automatically within the actual stack
            //       limits, it is useless for the actual stack check
            //       and is only used for informational purposes).
            //
            stackSize.allocated = NativeStack.GetNativeStackAllocated();

            //
            // NOTE: Get the current amount of stack reserved for this
            //       thread from its Thread Environment Block (TEB).
            //       Since it is highly unlikely that this number will
            //       change during the lifetime of the thread, we cache
            //       it.
            //
            UIntPtr maximum = UIntPtr.Zero;

            if (stackSize.maximum == UIntPtr.Zero)
            {
                maximum = NativeStack.GetNativeStackMaximum();
                stackSize.maximum = maximum;
            }

            //
            // NOTE: Calculate the approximate safety margin (overhead)
            //       imposed by the CLR runtime.  This is estimated and
            //       may need to be updated for later versions of the
            //       CLR.  Since this number is currently constant for
            //       the lifetime of the AppDomain, we calculate it once
            //       and then cache it.
            //
            if (stackSize.margin == UIntPtr.Zero)
            {
                //
                // NOTE: If necessary, query the maximum stack size.  If
                //       already set, use the existing value.
                //
                if (maximum == UIntPtr.Zero)
                    maximum = NativeStack.GetNativeStackMaximum();

                //
                // NOTE: Grab minimum stack size required for the default
                //       safety margin to actually be used.
                //
                UIntPtr minimum = NativeStack.GetNativeStackMinimum();

                //
                // NOTE: If the maximum stack size exceeds the specified
                //       minimum, use default safety margin; otherwise,
                //       use half the maximum stack size (rounded down).
                //
                if (maximum.ToUInt64() >= minimum.ToUInt64())
                    stackSize.margin = NativeStack.GetNativeStackMargin();
                else
                    stackSize.margin = new UIntPtr(maximum.ToUInt64() / 2);
            }

#if false
            //
            // NOTE: Emit a diagnostic message with updated native stack
            //       information.
            //
            TraceOps.DebugTrace(String.Format(
                "CreateOrUpdateStackSize: updated {0}", stackSize),
                typeof(RuntimeOps).Name, TracePriority.ThreadDebug5);
#endif

            //
            // NOTE: Return the created (or updated) stack size object
            //       to the caller.
            //
            return stackSize;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method populates the stack reserve and commit values of the
        /// specified stack size object from the executable (PE) file that
        /// started this process, if they have not already been set.
        /// </summary>
        /// <param name="stackSize">
        /// The native stack size object whose reserve and commit values should
        /// be set, which may be null.
        /// </param>
        private static void MaybeSetStackReserveAndCommit(
            NativeStack.StackSize stackSize
            )
        {
            if (stackSize != null)
            {
                if ((stackSize.reserve == UIntPtr.Zero) ||
                    (stackSize.commit == UIntPtr.Zero))
                {
                    FileOps.CopyPeFileStackReserveAndCommit(stackSize);

                    TraceOps.DebugTrace(String.Format(
                        "MaybeSetStackReserveAndCommit: reserve = {0}, " +
                        "commit = {1}", stackSize.reserve, stackSize.commit),
                        typeof(RuntimeOps).Name, TracePriority.ThreadDebug2);
                }
            }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method attempts to determine the maximum amount of native
        /// stack space available, preferring the maximum value from the stack
        /// size object and falling back on the stack reserve from the
        /// executable (PE) file for this process.
        /// </summary>
        /// <param name="stackSize">
        /// The native stack size object from which to obtain the maximum stack
        /// space, which may be null.
        /// </param>
        /// <param name="maximumSpace">
        /// Upon success, this contains the maximum amount of native stack
        /// space available.
        /// </param>
        /// <returns>
        /// True if the maximum native stack space was determined; otherwise,
        /// false.
        /// </returns>
        private static bool TryGetMaximumStackSpace(
            NativeStack.StackSize stackSize,
            ref UIntPtr maximumSpace
            )
        {
            if (stackSize != null)
            {
                //
                // NOTE: Start out with the maximum value from the stack size
                //       object.  This should be the typical case on Windows,
                //       because (most versions of) it supports the necessary
                //       native stack size checking APIs.
                //
                UIntPtr localMaximumSpace = stackSize.maximum;

                if (localMaximumSpace != UIntPtr.Zero)
                {
                    maximumSpace = localMaximumSpace;
                    return true;
                }

                //
                // NOTE: Failing that, fallback on the stack reserve from the
                //       executable (PE) file that started this process.  Do
                //       not bother with the commit as it is useless for this
                //       purpose.
                //
                MaybeSetStackReserveAndCommit(stackSize);

                localMaximumSpace = stackSize.reserve;

                if (localMaximumSpace != UIntPtr.Zero)
                {
                    maximumSpace = localMaximumSpace;
                    return true;
                }
            }

            return false;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method checks the available native stack space for the
        /// specified interpreter, but only when the number of parser levels
        /// exceeds the threshold that requires no native stack check.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter for which the native stack space should be checked,
        /// which may be null.
        /// </param>
        /// <param name="parserLevels">
        /// The current number of script parser levels.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> if sufficient native stack space is
        /// available (or the check was skipped); otherwise, an appropriate
        /// error code.
        /// </returns>
        public static ReturnCode MaybeCheckForParserStackSpace(
            Interpreter interpreter,
            int parserLevels
            ) /* THREAD-SAFE */
        {
            if (parserLevels > NoStackParserLevels)
                return CheckForStackSpace(interpreter);

            return ReturnCode.Ok;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method checks whether sufficient native stack space is
        /// available for the specified interpreter, using the default amount
        /// of extra stack space.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter for which the native stack space should be checked,
        /// which may be null.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> if sufficient native stack space is
        /// available; otherwise, an appropriate error code.
        /// </returns>
        public static ReturnCode CheckForStackSpace(
            Interpreter interpreter
            ) /* THREAD-SAFE */
        {
            return CheckForStackSpace(
                interpreter, Engine.GetExtraStackSpace());
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method checks whether sufficient native stack space is
        /// available for the specified interpreter, using the specified amount
        /// of extra stack space.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter for which the native stack space should be checked,
        /// which may be null.
        /// </param>
        /// <param name="extraSpace">
        /// The additional amount of native stack space that should be
        /// available beyond what is currently in use.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> if sufficient native stack space is
        /// available; otherwise, an appropriate error code.
        /// </returns>
        private static ReturnCode CheckForStackSpace(
            Interpreter interpreter,
            ulong extraSpace
            ) /* THREAD-SAFE */
        {
            try
            {
                lock (syncRoot) /* TRANSACTIONAL */
                {
                    //
                    // NOTE: Make sure we have our AppDomain-wide thread data
                    //       slots.  We do not and cannot actually allocate
                    //       or create them here.
                    //
                    if ((stackPtrSlot == null) || (stackSizeSlot == null))
                    {
                        //
                        // NOTE: Our AppDomain-wide data slots were either not
                        //       allocated or have been freed prematurely?
                        //       Just assume that runtime stack checking was
                        //       purposely disabled and enough stack space is
                        //       available.
                        //
#if (DEBUG || FORCE_TRACE) && VERBOSE
                        TraceOps.DebugTrace(
                            "CheckForStackSpace: thread storage slots " +
                            "not available", typeof(RuntimeOps).Name,
                            TracePriority.ThreadError);
#endif

                        return ReturnCode.Ok;
                    }

                    //
                    // NOTE: Attempt to get the current (inner) native stack
                    //       pointer and the (previously saved) outer native
                    //       stack pointer.
                    //
                    UIntPtr innerStackPtr = UIntPtr.Zero;
                    UIntPtr outerStackPtr = UIntPtr.Zero;

                    RefreshNativeStackPointers(
                        false, ref innerStackPtr, ref outerStackPtr);

                    //
                    // NOTE: Make sure we have valid values for the outer and
                    //       inner native stack pointers.
                    //
                    if (outerStackPtr == UIntPtr.Zero)
                    {
                        //
                        // NOTE: Runtime native stack checking appears to be
                        //       unavailable, just assume that enough stack
                        //       space is available.
                        //
#if (DEBUG || FORCE_TRACE) && VERBOSE
                        TraceOps.DebugTrace(
                            "CheckForStackSpace: outer stack pointer " +
                            "not available", typeof(RuntimeOps).Name,
                            TracePriority.ThreadError);
#endif

                        return ReturnCode.Ok;
                    }

                    if (innerStackPtr == UIntPtr.Zero)
                    {
                        //
                        // NOTE: Runtime native stack checking appears to be
                        //       unavailable, just assume that enough stack
                        //       space is available.
                        //
#if (DEBUG || FORCE_TRACE) && VERBOSE
                        TraceOps.DebugTrace(
                            "CheckForStackSpace: inner stack pointer " +
                            "not available", typeof(RuntimeOps).Name,
                            TracePriority.ThreadError);
#endif

                        return ReturnCode.Ok;
                    }

                    //
                    // NOTE: Calculate approximately how much native stack
                    //       space has been used.
                    //
                    UIntPtr usedSpace = CalculateUsedStackSpace(outerStackPtr,
                        innerStackPtr);

                    //
                    // NOTE: Create and/or update the native stack size for
                    //       this thread.  If the resulting native stack size
                    //       is null, for whatever reason, we cannot continue.
                    //
                    NativeStack.StackSize stackSize = CreateOrUpdateStackSize(
                        extraSpace, usedSpace);

                    if (stackSize == null)
                    {
                        //
                        // NOTE: If we made it this far and still do not have
                        //       a valid native stack size, just assume that
                        //       enough stack space is available.
                        //
#if (DEBUG || FORCE_TRACE) && VERBOSE
                        TraceOps.DebugTrace(
                            "CheckForStackSpace: stack size not available",
                            typeof(RuntimeOps).Name, TracePriority.ThreadError);
#endif

                        return ReturnCode.Ok;
                    }

                    //
                    // NOTE: Obtain the maximum stack size for this thread.
                    //
                    UIntPtr maximumSpace = UIntPtr.Zero;

                    if (!TryGetMaximumStackSpace(stackSize, ref maximumSpace))
                    {
                        //
                        // NOTE: If we made it this far and still do not have
                        //       a valid maximum native stack size, just assume
                        //       that enough stack space is available.
                        //
#if (DEBUG || FORCE_TRACE) && VERBOSE
                        TraceOps.DebugTrace(
                            "CheckForStackSpace: maximum space not available",
                            typeof(RuntimeOps).Name, TracePriority.ThreadError);
#endif

                        return ReturnCode.Ok;
                    }

                    //
                    // NOTE: Calculate the amount of space used with the safety
                    //       margin taken into account.
                    //
                    UIntPtr neededSpace = CalculateNeededStackSpace(
                        interpreter, extraSpace, usedSpace, stackSize.margin);

                    //
                    // NOTE: Are we "out of stack space" taking the requested
                    //       extra space and our internal safety margin into
                    //       account?
                    //
                    // BUGBUG: Also, it seems that some pool threads have a
                    //         miserably low stack size (less than our internal
                    //         safety margin); therefore, evaluating scripts on
                    //         pool threads is not officially supported.
                    //
                    if (neededSpace.ToUInt64() <= maximumSpace.ToUInt64())
                    {
                        //
                        // NOTE: Normal case, enough native stack space appears
                        //       to be available.
                        //
                        return ReturnCode.Ok;
                    }
                    else
                    {
                        //
                        // NOTE: Try to "fill in" some accurate stack reserve
                        //       and commit numbers, now, if needed.
                        //
                        MaybeSetStackReserveAndCommit(stackSize);

                        //
                        // NOTE: We hit a "soft" stack-overflow error.  This
                        //       error is guaranteed by the script engine to
                        //       be non-fatal to the process, the application
                        //       domain, and the script engine itself, and is
                        //       always fully recoverable.
                        //
                        TraceOps.DebugTrace(String.Format(
                            "CheckForStackSpace: stack overflow, needed " +
                            "space {0} is greater than maximum space {1} for " +
                            "interpreter {2}: {3}", neededSpace, maximumSpace,
                            FormatOps.InterpreterNoThrow(interpreter),
                            stackSize), typeof(RuntimeOps).Name,
                            TracePriority.EngineError);

                        TraceOps.DebugTrace(String.Format(
                            "CheckForStackSpace: innerStackPtr = {0}, " +
                            "outerStackPtr = {1}", innerStackPtr, outerStackPtr),
                            typeof(RuntimeOps).Name, TracePriority.NativeDebug);

                        return ReturnCode.Error;
                    }
                }
            }
            catch (StackOverflowException)
            {
                //
                // NOTE: We hit a "hard" stack-overflow (exception) during the
                //       stack checking code?  Generally, this error should be
                //       non-fatal to the process, the application domain, and
                //       the script engine, and should be fully "recoverable";
                //       however, this is not guaranteed by the script engine
                //       as we are relying on the CLR stack unwinding semantics
                //       to function properly.
                //
                try
                {
                    //
                    // NOTE: We really want to report this condition to anybody
                    //       who might be listening; however, it is somewhat
                    //       dangerous to do so.  Therefore, wrap the necessary
                    //       method call in a try/catch block just in case we
                    //       re-trigger another stack overflow.
                    //
                    TraceOps.DebugTrace(
                        "CheckForStackSpace: stack overflow exception",
                        typeof(RuntimeOps).Name, TracePriority.EngineError);
                }
                catch (StackOverflowException)
                {
                    // do nothing.
                }

                return ReturnCode.Error;
            }
            catch (SecurityException)
            {
                //
                // NOTE: We may not be allowed to execute any native code;
                //       therefore, just assume that we always have enough
                //       stack space in that case.
                //
#if (DEBUG || FORCE_TRACE) && VERBOSE
                TraceOps.DebugTrace(
                    "CheckForStackSpace: security exception",
                    typeof(RuntimeOps).Name, TracePriority.EngineError);
#endif

                return ReturnCode.Ok;
            }
        }
#endif
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Hash Algorithm Support Methods
        /// <summary>
        /// This method computes the hash of the string value of an argument
        /// using the specified hash algorithm.
        /// </summary>
        /// <param name="hashAlgorithmName">
        /// The name of the hash algorithm to use, or null to use the default
        /// hash algorithm.
        /// </param>
        /// <param name="argument">
        /// The argument whose string value should be hashed.  This value may
        /// be null.
        /// </param>
        /// <param name="encoding">
        /// The encoding used to convert the string value into bytes prior to
        /// hashing, or null to use the raw encoding.
        /// </param>
        /// <param name="error">
        /// Upon failure, this parameter will contain an appropriate error
        /// message.
        /// </param>
        /// <returns>
        /// The computed hash as an array of bytes, or null upon failure.
        /// </returns>
        public static byte[] HashArgument(
            string hashAlgorithmName,
            Argument argument,
            Encoding encoding,
            ref Result error
            )
        {
            return HashString(hashAlgorithmName,
                (argument != null) ? argument.String : null,
                encoding, ref error);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method computes the hash of the text of a script using the
        /// specified hash algorithm.
        /// </summary>
        /// <param name="hashAlgorithmName">
        /// The name of the hash algorithm to use, or null to use the default
        /// hash algorithm.
        /// </param>
        /// <param name="script">
        /// The script whose text should be hashed.  This value may be null.
        /// </param>
        /// <param name="encoding">
        /// The encoding used to convert the script text into bytes prior to
        /// hashing, or null to use the raw encoding.
        /// </param>
        /// <param name="error">
        /// Upon failure, this parameter will contain an appropriate error
        /// message.
        /// </param>
        /// <returns>
        /// The computed hash as an array of bytes, or null upon failure.
        /// </returns>
        public static byte[] HashScript(
            string hashAlgorithmName,
            IScript script,
            Encoding encoding,
            ref Result error
            )
        {
            try
            {
                ByteList bytes = new ByteList();

                if (script != null)
                {
                    string value = script.Text;

                    if (value != null)
                    {
                        if (encoding != null)
                            bytes.AddRange(encoding.GetBytes(value));
                        else
                            bytes.AddRange(RawEncoding.GetBytes(value));
                    }
                }

                return HashOps.HashBytes(
                    hashAlgorithmName, bytes.ToArray(), ref error);
            }
            catch (Exception e)
            {
                error = e;
            }

            return null;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method computes the hash of the contents of a file using the
        /// specified hash algorithm.  Remote URI file names are not supported.
        /// </summary>
        /// <param name="hashAlgorithmName">
        /// The name of the hash algorithm to use, or null to use the default
        /// hash algorithm.
        /// </param>
        /// <param name="fileName">
        /// The name of the file whose contents should be hashed.
        /// </param>
        /// <param name="encoding">
        /// The encoding used to read the file and convert its contents into
        /// bytes prior to hashing, or null to read the raw bytes of the file.
        /// </param>
        /// <param name="error">
        /// Upon failure, this parameter will contain an appropriate error
        /// message.
        /// </param>
        /// <returns>
        /// The computed hash as an array of bytes, or null upon failure.
        /// </returns>
        public static byte[] HashFile(
            string hashAlgorithmName,
            string fileName,
            Encoding encoding,
            ref Result error
            )
        {
            if (String.IsNullOrEmpty(fileName))
            {
                error = "invalid file name";
                return null;
            }

            if (PathOps.IsRemoteUri(fileName))
            {
                error = "remote uri not supported";
                return null;
            }

            if (!File.Exists(fileName))
            {
                error = String.Format(
                    "couldn't read file {0}: " +
                    "no such file or directory",
                    FormatOps.WrapOrNull(fileName));

                return null;
            }

            try
            {
                ByteList bytes = new ByteList();

                if (encoding != null)
                {
                    bytes.AddRange(encoding.GetBytes(
                        File.ReadAllText(fileName, encoding)));
                }
                else
                {
                    bytes.AddRange(File.ReadAllBytes(fileName));
                }

                return HashOps.HashBytes(
                    hashAlgorithmName, bytes.ToArray(), ref error);
            }
            catch (Exception e)
            {
                error = e;
            }

            return null;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method reads (or otherwise obtains) the contents of a script
        /// file and computes the hash of its original text using the default
        /// hash algorithm.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter context used to read the script file and to obtain
        /// the flags that control how it is read.
        /// </param>
        /// <param name="fileName">
        /// The name of the script file whose original text should be hashed.
        /// </param>
        /// <param name="noRemote">
        /// Non-zero to prevent the script file from being read from a remote
        /// location.
        /// </param>
        /// <param name="error">
        /// Upon failure, this parameter will contain an appropriate error
        /// message.
        /// </param>
        /// <returns>
        /// The computed hash as an array of bytes, or null upon failure.
        /// </returns>
        public static byte[] HashScriptFile(
            Interpreter interpreter,
            string fileName,
            bool noRemote,
            ref Result error
            )
        {
            try
            {
                if (interpreter == null)
                {
                    error = "invalid interpreter";
                    return null;
                }

                Encoding encoding = Engine.GetEncoding(
                    fileName, EncodingType.Script, null);

                if (encoding == null)
                {
                    error = "script encoding not available";
                    return null;
                }

                ScriptFlags scriptFlags;
                EngineFlags engineFlags;
                SubstitutionFlags substitutionFlags;
                EventFlags eventFlags;
                ExpressionFlags expressionFlags;

                lock (interpreter.InternalSyncRoot) /* TRANSACTIONAL */
                {
                    scriptFlags = ScriptOps.GetFlags(
                        interpreter, interpreter.ScriptFlags, true,
                        false);

                    engineFlags = interpreter.EngineFlags;
                    substitutionFlags = interpreter.SubstitutionFlags;
                    eventFlags = interpreter.EngineEventFlags;
                    expressionFlags = interpreter.ExpressionFlags;
                }

                scriptFlags |= ScriptFlags.NoPolicy;
                engineFlags |= EngineFlags.NoPolicy;

                if (noRemote)
                    engineFlags |= EngineFlags.NoRemote;

                string originalText = null;
                string text = null; /* NOT USED */

                if (Engine.ReadOrGetScriptFile(
                        interpreter, encoding, ref scriptFlags,
                        ref fileName, ref engineFlags,
                        ref substitutionFlags, ref eventFlags,
                        ref expressionFlags, ref originalText,
                        ref text, ref error) != ReturnCode.Ok)
                {
                    return null;
                }

                return HashString(
                    null, originalText, encoding, ref error);
            }
            catch (Exception e)
            {
                error = e;
            }

            return null;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method computes the hash of a string value using the specified
        /// hash algorithm.
        /// </summary>
        /// <param name="hashAlgorithmName">
        /// The name of the hash algorithm to use, or null to use the default
        /// hash algorithm.
        /// </param>
        /// <param name="value">
        /// The string value to be hashed.  This value may be null.
        /// </param>
        /// <param name="encoding">
        /// The encoding used to convert the string value into bytes prior to
        /// hashing, or null to use the raw encoding.
        /// </param>
        /// <param name="error">
        /// Upon failure, this parameter will contain an appropriate error
        /// message.
        /// </param>
        /// <returns>
        /// The computed hash as an array of bytes, or null upon failure.
        /// </returns>
        public static byte[] HashString(
            string hashAlgorithmName,
            string value,
            Encoding encoding,
            ref Result error
            )
        {
            try
            {
                ByteList bytes = new ByteList();

                if (value != null)
                {
                    if (encoding != null)
                        bytes.AddRange(encoding.GetBytes(value));
                    else
                        bytes.AddRange(RawEncoding.GetBytes(value));
                }

                return HashOps.HashBytes(
                    hashAlgorithmName, bytes.ToArray(), ref error);
            }
            catch (Exception e)
            {
                error = e;
            }

            return null;
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Security Support Methods
        /// <summary>
        /// This method finds the <see cref="KeySizes" /> instance that has the
        /// smallest minimum size from an array of candidates.
        /// </summary>
        /// <param name="allKeySizes">
        /// The array of <see cref="KeySizes" /> instances to examine.  This
        /// value may be null and individual elements may be null.
        /// </param>
        /// <returns>
        /// The <see cref="KeySizes" /> instance with the smallest minimum size,
        /// or null if there are no suitable candidates.
        /// </returns>
        private static KeySizes GetLeastMinSize(
            KeySizes[] allKeySizes /* in */
            )
        {
            if (allKeySizes == null)
                return null;

            int bestIndex = Index.Invalid;
            int bestMinSize = _Size.Invalid;

            for (int index = 0; index < allKeySizes.Length; index++)
            {
                KeySizes keySizes = allKeySizes[index];

                if (keySizes == null)
                    continue;

                int minSize = keySizes.MinSize;

                if ((bestIndex == Index.Invalid) ||
                    (minSize < bestMinSize))
                {
                    bestIndex = index;
                    bestMinSize = minSize;
                }
            }

            return (bestIndex != Index.Invalid) ?
                allKeySizes[bestIndex] : null;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method finds the <see cref="KeySizes" /> instance that has the
        /// largest maximum size from an array of candidates.
        /// </summary>
        /// <param name="allKeySizes">
        /// The array of <see cref="KeySizes" /> instances to examine.  This
        /// value may be null and individual elements may be null.
        /// </param>
        /// <returns>
        /// The <see cref="KeySizes" /> instance with the largest maximum size,
        /// or null if there are no suitable candidates.
        /// </returns>
        private static KeySizes GetGreatestMaxSize(
            KeySizes[] allKeySizes /* in */
            )
        {
            if (allKeySizes == null)
                return null;

            int bestIndex = Index.Invalid;
            int bestMaxSize = _Size.Invalid;

            for (int index = 0; index < allKeySizes.Length; index++)
            {
                KeySizes keySizes = allKeySizes[index];

                if (keySizes == null)
                    continue;

                int maxSize = keySizes.MaxSize;

                if ((bestIndex == Index.Invalid) ||
                    (maxSize > bestMaxSize))
                {
                    bestIndex = index;
                    bestMaxSize = maxSize;
                }
            }

            return (bestIndex != Index.Invalid) ?
                allKeySizes[bestIndex] : null;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method determines the greatest legal key size and the least
        /// legal block size supported by the specified symmetric algorithm.
        /// </summary>
        /// <param name="algorithm">
        /// The symmetric algorithm to query.  This value may be null.
        /// </param>
        /// <param name="keySize">
        /// Upon success, this parameter will contain the greatest legal key
        /// size, in bits, supported by the algorithm.
        /// </param>
        /// <param name="blockSize">
        /// Upon success, this parameter will contain the least legal block
        /// size, in bits, supported by the algorithm.
        /// </param>
        /// <returns>
        /// An array of two boolean values; the first element is true if the
        /// key size was determined and the second element is true if the block
        /// size was determined.
        /// </returns>
        public static bool[] GetGreatestMaxKeySizeAndLeastMinBlockSize(
            SymmetricAlgorithm algorithm, /* in */
            ref int keySize,              /* in, out */
            ref int blockSize             /* in, out */
            )
        {
            bool[] found = { false, false };

            if (algorithm != null)
            {
                KeySizes keySizes; /* REUSED */

                keySizes = GetGreatestMaxSize(algorithm.LegalKeySizes);

                if (keySizes != null)
                {
                    keySize = keySizes.MaxSize;
                    found[0] = true;
                }

                keySizes = GetLeastMinSize(algorithm.LegalBlockSizes);

                if (keySizes != null)
                {
                    blockSize = keySizes.MinSize;
                    found[1] = true;
                }
            }

            return found;
        }

        ///////////////////////////////////////////////////////////////////////

#if !NATIVE && !NET_STANDARD_20
        /// <summary>
        /// This method determines whether the current process is running with
        /// administrative privileges.
        /// </summary>
        /// <param name="administrator">
        /// Upon success, this parameter will be true if the current process is
        /// running with administrative privileges; otherwise, it will be false.
        /// </param>
        /// <param name="error">
        /// Upon failure, this parameter will contain an appropriate error
        /// message.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success;
        /// <see cref="ReturnCode.Error" /> on failure.
        /// </returns>
        private static ReturnCode IsAdministrator(
            ref bool administrator,
            ref Result error
            )
        {
            try
            {
                //
                // BUGBUG: This does not work properly on Mono due to their
                //         lack of support for checking the elevation status of
                //         the current process (i.e. it returns true even when
                //         running without elevation).
                //
                WindowsIdentity identity = WindowsIdentity.GetCurrent();

                administrator = (identity != null)
                    ? new WindowsPrincipal(identity).IsInRole(
                        WindowsBuiltInRole.Administrator) :
                    false;

                return ReturnCode.Ok;
            }
            catch (Exception e)
            {
                error = e;
            }

            return ReturnCode.Error;
        }
#endif

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method determines whether the current process is running with
        /// administrative privileges.
        /// </summary>
        /// <returns>
        /// True if the current process is running with administrative
        /// privileges; otherwise, false.
        /// </returns>
        public static bool IsAdministrator()
        {
#if NATIVE
            //
            // BUGBUG: This fails when running on Mono for Windows due to the
            //         bug that prevents native functions from being called by
            //         ordinal (e.g. "#680").
            //         https://bugzilla.novell.com/show_bug.cgi?id=636966
            //
            return SecurityOps.IsAdministrator();
#elif !NET_STANDARD_20
            //
            // BUGBUG: This does not work properly on Mono due to their lack of
            //         support for checking the elevation status of the current
            //         process (i.e. it returns true even when running without
            //         elevation).
            //
            bool administrator = false;
            Result error = null;

            return (IsAdministrator(ref administrator,
                ref error) == ReturnCode.Ok) && administrator;
#else
            return false;
#endif
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method determines whether strong name verification checks
        /// should be performed, based on the presence of an environment
        /// variable.
        /// </summary>
        /// <returns>
        /// True if strong name verification checks should be performed;
        /// otherwise, false.
        /// </returns>
        public static bool ShouldCheckStrongNameVerified()
        {
            return !CommonOps.Environment.DoesVariableExist(
                EnvVars.NoVerified);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method determines whether the strong name signature of an
        /// assembly, provided as an array of bytes, is verified.  The bytes are
        /// written to a temporary file, which is held open while the native CLR
        /// API verifies the strong name signature, and then deleted.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter context used to obtain the temporary file name.
        /// This value is optional and may be null.
        /// </param>
        /// <param name="bytes">
        /// The bytes of the assembly whose strong name signature should be
        /// verified.
        /// </param>
        /// <param name="force">
        /// Non-zero to force the strong name signature to be verified even if
        /// verification has been previously disabled for the assembly.
        /// </param>
        /// <returns>
        /// True if the strong name signature is verified; otherwise, false.
        /// </returns>
        public static bool IsStrongNameVerified(
            Interpreter interpreter, /* in: OPTIONAL */
            byte[] bytes,            /* in */
            bool force               /* in */
            )
        {
            //
            // NOTE: *SECURITY* Failure, if no bytes were supplied we cannot
            //       verify them.
            //
            if ((bytes == null) || (bytes.Length == 0))
                return false;

            string fileName = null;

            try
            {
                fileName = PathOps.GetTempFileName( /* throw */
                    interpreter, "esnv_"); /* Eagle Strong Name Verification */

                //
                // NOTE: *SECURITY* Failure, if we cannot obtain a temporary
                //       file name we cannot verify the assembly file.
                //
                if (String.IsNullOrEmpty(fileName))
                    return false;

                //
                // NOTE: This code requires a bit of explanation.  First,
                //       we write all the file bytes to the temporary file.
                //       Next, we [re-]open that same temporary file for
                //       reading only and hold it open while calling into
                //       the native CLR API to verify the strong name
                //       signature on it.  Furthermore, the bytes of the
                //       open temporary file are read back into a new byte
                //       array and are then compared with the previously
                //       written byte array.  If there is any discrepancy,
                //       this method returns false without calling the
                //       native CLR API to check the strong name signature.
                //
                File.WriteAllBytes(fileName, bytes); /* throw */

                using (FileStream stream = new FileStream(
                        fileName, FileMode.Open, FileAccess.Read,
                        FileShare.Read)) /* throw */ /* EXEMPT */
                {
                    //
                    // NOTE: Depending on the size of the file, this could
                    //       potentially run out of memory.
                    //
                    byte[] newBytes = new byte[bytes.Length]; /* throw */
                    stream.Read(newBytes, 0, newBytes.Length); /* throw */

                    //
                    // NOTE: *SECURITY* Failure, if the underlying bytes of
                    //       the file have changed since we wrote them then
                    //       it cannot be verified.
                    //
                    if (!ArrayOps.Equals(newBytes, bytes))
                        return false;

                    //
                    // NOTE: Ok, the newly read bytes match those we wrote
                    //       out and we are holding the underlying file open,
                    //       preventing it from being changed via any other
                    //       thread or process; therefore, perform the strong
                    //       name verification via the native CLR API now.
                    //
                    return IsStrongNameVerified(fileName, force);
                }
            }
            catch (Exception e)
            {
                TraceOps.DebugTrace(
                    e, typeof(RuntimeOps).Name,
                    TracePriority.SecurityError);

                //
                // NOTE: *SECURITY* Failure, assume not verified.
                //
                return false;
            }
            finally
            {
                try
                {
                    //
                    // NOTE: If we created a temporary file, always delete it
                    //       prior to returning from this method.
                    //
                    if (fileName != null)
                        File.Delete(fileName); /* throw */
                }
                catch (Exception e)
                {
                    TraceOps.DebugTrace(
                        e, typeof(RuntimeOps).Name,
                        TracePriority.SecurityError);
                }
            }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method determines whether the strong name signature of the
        /// assembly file with the specified name is verified.
        /// </summary>
        /// <param name="fileName">
        /// The name of the assembly file whose strong name signature should be
        /// verified.
        /// </param>
        /// <param name="force">
        /// Non-zero to force the strong name signature to be verified even if
        /// verification has been previously disabled for the assembly.
        /// </param>
        /// <returns>
        /// True if the strong name signature is verified; otherwise, false.
        /// </returns>
        public static bool IsStrongNameVerified(
            string fileName,
            bool force
            )
        {
            #region .NET Core Support
#if NET_STANDARD_20
            if (CommonOps.Runtime.IsDotNetCore())
            {
                bool returnValue = false;
                bool verified = false;
                Result error = null; /* NOT USED */

                if ((StrongNameDotNet.IsStrongNameVerifiedDotNet(
                        fileName, force, ref returnValue, ref verified,
                        ref error) == ReturnCode.Ok) &&
                    returnValue && verified)
                {
                    TraceOps.DebugTrace(String.Format(
                        "IsStrongNameVerified: file {0} " +
                        "SUCCESS using {1} (1).",
                        FormatOps.WrapOrNull(fileName),
                        CommonOps.Runtime.GetRuntimeNameAndVMajorMinor()),
                        typeof(RuntimeOps).Name,
                        TracePriority.SecurityDebug4);

                    return true;
                }
                else
                {
                    TraceOps.DebugTrace(String.Format(
                        "IsStrongNameVerified: file {0} " +
                        "FAILURE using {1} (1).",
                        FormatOps.WrapOrNull(fileName),
                        CommonOps.Runtime.GetRuntimeNameAndVMajorMinor()),
                        typeof(RuntimeOps).Name,
                        TracePriority.SecurityError);

                    return false;
                }
            }
#endif
            #endregion

            ///////////////////////////////////////////////////////////////////

            #region Mono Support
#if MONO
            if (CommonOps.Runtime.IsMono())
            {
                bool returnValue = false;
                bool verified = false;
                Result error = null; /* NOT USED */

                if ((StrongNameMono.IsStrongNameVerifiedMono(
                        fileName, force, ref returnValue, ref verified,
                        ref error) == ReturnCode.Ok) &&
                    returnValue && verified)
                {
                    TraceOps.DebugTrace(String.Format(
                        "IsStrongNameVerified: file {0} " +
                        "SUCCESS using {1} (2).",
                        FormatOps.WrapOrNull(fileName),
                        CommonOps.Runtime.GetRuntimeNameAndVMajorMinor()),
                        typeof(RuntimeOps).Name,
                        TracePriority.SecurityDebug4);

                    return true;
                }
                else
                {
                    TraceOps.DebugTrace(String.Format(
                        "IsStrongNameVerified: file {0} " +
                        "FAILURE using {1} (2).",
                        FormatOps.WrapOrNull(fileName),
                        CommonOps.Runtime.GetRuntimeNameAndVMajorMinor()),
                        typeof(RuntimeOps).Name,
                        TracePriority.SecurityError);

                    return false;
                }
            }
#endif
            #endregion

            ///////////////////////////////////////////////////////////////////

            #region .NET Framework Support
#if NATIVE
            int clrVersion = 0;

            if (StrongNameOps.IsStrongNameVerifiedClr(
                    fileName, force, ref clrVersion))
            {
                TraceOps.DebugTrace(String.Format(
                    "IsStrongNameVerified: file {0} " +
                    "SUCCESS using {1} (CLRv{2}) (3).",
                    FormatOps.WrapOrNull(fileName),
                    CommonOps.Runtime.GetRuntimeNameAndVMajorMinor(false),
                    clrVersion), typeof(RuntimeOps).Name,
                    TracePriority.SecurityDebug4);

                return true;
            }
            else
            {
                TraceOps.DebugTrace(String.Format(
                    "IsStrongNameVerified: file {0} " +
                    "FAILURE using {1} (CLRv{2}) (3).",
                    FormatOps.WrapOrNull(fileName),
                    CommonOps.Runtime.GetRuntimeNameAndVMajorMinor(false),
                    clrVersion), typeof(RuntimeOps).Name,
                    TracePriority.SecurityError);

                return false;
            }
#endif
            #endregion

            ///////////////////////////////////////////////////////////////////

#if !NATIVE
            //
            // FIXME: Find some (other) pure-managed way to do this?
            //
            return false;
#endif
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method determines whether the application should check for
        /// updates, based on the presence of an environment variable.  Checking
        /// for updates is disabled by default when running on Mono.
        /// </summary>
        /// <returns>
        /// True if the application should check for updates; otherwise, false.
        /// </returns>
        public static bool ShouldCheckForUpdates()
        {
#if MONO || MONO_HACKS
            //
            // HACK: *MONO* When running on Mono, attempting to check for
            //       updates may crash, for reasons that are unclear; so,
            //       in that case, checking for updates will be disabled
            //       by default.
            //
            if (!forceMono && CommonOps.Runtime.IsMono())
            {
                TraceOps.DebugTrace(
                    "ShouldCheckForUpdates: detected Mono runtime, " +
                    "forced disabled", typeof(RuntimeOps).Name,
                    TracePriority.PlatformDebug);

                return false;
            }
#endif

            return !CommonOps.Environment.DoesVariableExist(
                EnvVars.NoUpdates);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method determines whether trusted hashes should be used, based
        /// on the presence of an environment variable.
        /// </summary>
        /// <returns>
        /// True if trusted hashes should be used; otherwise, false.
        /// </returns>
        public static bool ShouldUseTrustedHashes()
        {
            return !CommonOps.Environment.DoesVariableExist(
                EnvVars.NoTrustedHashes);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method determines whether the use of trusted hashes should be
        /// forced, based on the presence of an environment variable.
        /// </summary>
        /// <returns>
        /// True if the use of trusted hashes should be forced; otherwise,
        /// false.
        /// </returns>
        public static bool ShouldForceTrustedHashes()
        {
            return CommonOps.Environment.DoesVariableExist(
                EnvVars.ForceTrustedHashes);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method determines whether files should be checked for trust,
        /// based on the presence of an environment variable.
        /// </summary>
        /// <returns>
        /// True if files should be checked for trust; otherwise, false.
        /// </returns>
        public static bool ShouldCheckFileTrusted()
        {
            return !CommonOps.Environment.DoesVariableExist(
                EnvVars.NoTrusted);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method determines whether files belonging to the Eagle Core
        /// Library should be checked for trust.
        /// </summary>
        /// <returns>
        /// True if core library files should be checked for trust; otherwise,
        /// false.
        /// </returns>
        public static bool ShouldCheckCoreFileTrusted()
        {
            if (!ShouldCheckFileTrusted())
                return false;

#if !NET_STANDARD_20
            if (!SetupOps.ShouldCheckCoreTrusted())
                return false;
#endif

            return true;
        }

        ///////////////////////////////////////////////////////////////////////

#if NATIVE_UTILITY
        //
        // WARNING: For now, this method should be called *ONLY* from within
        //          the NativeUtility class in order to verify that the Eagle
        //          Native Utility Library (Spilornis) is trusted.
        //
        /// <summary>
        /// This method determines whether the Eagle Native Utility Library
        /// (Spilornis), provided as a file name, should be trusted and allowed
        /// to load.  For the purposes of this check, the native utility library
        /// is considered to be part of the Eagle Core Library.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter context used when checking whether the relevant
        /// files are trusted.
        /// </param>
        /// <param name="fileName">
        /// The name of the native library file whose trust should be checked.
        /// </param>
        /// <returns>
        /// True if the native library should be trusted and allowed to load;
        /// otherwise, false.
        /// </returns>
        public static bool ShouldTrustNativeLibrary(
            Interpreter interpreter,
            string fileName
            )
        {
            //
            // NOTE: If the primary assembly is not "trusted", allow any
            //       native library to load.
            //
            // NOTE: For the purposes of this ShouldCheckCoreTrusted() call,
            //       the "Eagle Native Utility Library" (Spilornis) *IS*
            //       considered to be part of the "Eagle Core Library".
            //
            if (!ShouldCheckCoreFileTrusted() || !IsFileTrusted(
                    interpreter, null, GlobalState.GetAssemblyLocation(),
                    IntPtr.Zero))
            {
                return true;
            }

            //
            // NOTE: Otherwise, if the native library is "trusted", allow
            //       it to load.
            //
            if (!ShouldCheckFileTrusted() ||
                IsFileTrusted(interpreter, null, fileName, IntPtr.Zero))
            {
                return true;
            }

            //
            // NOTE: Otherwise, do not allow the native library to load.
            //
            return false;
        }
#endif

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method determines whether the trust of the specified block of
        /// bytes can be verified.  The bytes are written to a temporary file,
        /// which is checked for trust and then deleted prior to returning.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter context to use, if any.  This parameter may be null.
        /// </param>
        /// <param name="trustedHashes">
        /// The optional list of pre-approved file hashes that should be treated
        /// as trusted.  This parameter may be null.
        /// </param>
        /// <param name="bytes">
        /// The block of bytes to check for trust.  This parameter may not be
        /// null or empty.
        /// </param>
        /// <returns>
        /// True if the trust of the bytes could be verified; otherwise, false.
        /// </returns>
        public static bool IsFileTrusted(
            Interpreter interpreter,
            StringList trustedHashes,
            byte[] bytes
            )
        {
            //
            // NOTE: *SECURITY* Failure, if no bytes were supplied we cannot
            //       check trust on them.
            //
            if ((bytes == null) || (bytes.Length == 0))
                return false;

            string fileName = null;

            try
            {
                fileName = PathOps.GetTempFileName( /* throw */
                    interpreter, "etfc_"); /* Eagle Trusted File Checking */

                //
                // NOTE: *SECURITY* Failure, if we cannot obtain a temporary
                //       file name we cannot check trust on the file.
                //
                if (String.IsNullOrEmpty(fileName))
                    return false;

                using (FileStream stream = new FileStream(
                        fileName, FileMode.Create, FileAccess.ReadWrite,
                        FileShare.None)) /* throw */ /* EXEMPT */
                {
                    stream.Write(bytes, 0, bytes.Length); /* throw */

                    return IsFileTrusted(
                        interpreter, trustedHashes, fileName,
                        stream.Handle);
                }
            }
            catch (Exception e)
            {
                TraceOps.DebugTrace(
                    e, typeof(RuntimeOps).Name,
                    TracePriority.SecurityError);

                //
                // NOTE: *SECURITY* Failure, assume not trusted.
                //
                return false;
            }
            finally
            {
                try
                {
                    //
                    // NOTE: If we created a temporary file, always delete it
                    //       prior to returning from this method.
                    //
                    if (fileName != null)
                        File.Delete(fileName); /* throw */
                }
                catch (Exception e)
                {
                    TraceOps.DebugTrace(
                        e, typeof(RuntimeOps).Name,
                        TracePriority.SecurityError);
                }
            }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method determines whether the trust of the specified file can
        /// be verified, using the default trust checking options.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter context to use, if any.  This parameter may be null.
        /// </param>
        /// <param name="trustedHashes">
        /// The optional list of pre-approved file hashes that should be treated
        /// as trusted.  This parameter may be null.
        /// </param>
        /// <param name="fileName">
        /// The name of the file to check for trust.
        /// </param>
        /// <param name="fileHandle">
        /// The native handle of the open file to check for trust, if available;
        /// otherwise, <see cref="IntPtr.Zero" />.
        /// </param>
        /// <returns>
        /// True if the trust of the file could be verified; otherwise, false.
        /// </returns>
        public static bool IsFileTrusted(
            Interpreter interpreter,
            StringList trustedHashes,
            string fileName,
            IntPtr fileHandle
            )
        {
            return IsFileTrusted(
                interpreter, trustedHashes, fileName, fileHandle, false,
                false, true, false);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method determines whether the trust of the specified file can
        /// be verified, dispatching to the appropriate platform-specific or
        /// managed trust checking implementation based on the runtime in use.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter context to use, if any.  This parameter may be null.
        /// </param>
        /// <param name="trustedHashes">
        /// The optional list of pre-approved file hashes that should be treated
        /// as trusted.  This parameter may be null.
        /// </param>
        /// <param name="fileName">
        /// The name of the file to check for trust.
        /// </param>
        /// <param name="fileHandle">
        /// The native handle of the open file to check for trust, if available;
        /// otherwise, <see cref="IntPtr.Zero" />.
        /// </param>
        /// <param name="userInterface">
        /// Non-zero to permit a user interface to be displayed during trust
        /// checking.
        /// </param>
        /// <param name="userPrompt">
        /// Non-zero to permit the user to be prompted during trust checking.
        /// </param>
        /// <param name="revocation">
        /// Non-zero to enable certificate revocation checking during trust
        /// checking.
        /// </param>
        /// <param name="install">
        /// Non-zero if the trust check is being performed in the context of an
        /// installation.
        /// </param>
        /// <returns>
        /// True if the trust of the file could be verified; otherwise, false.
        /// </returns>
        private static bool IsFileTrusted(
            Interpreter interpreter,
            StringList trustedHashes,
            string fileName,
            IntPtr fileHandle,
            bool userInterface,
            bool userPrompt,
            bool revocation,
            bool install
            )
        {
            #region .NET Core Support
            bool forceTrustedHashes = ShouldForceTrustedHashes();

#if !NATIVE
            bool treatAsDotNetCore = false;

        nonNative:
#endif

            if (
                forceTrustedHashes ||
#if !NATIVE
                treatAsDotNetCore ||
#endif
                CommonOps.Runtime.IsDotNetCore())
            {
#if NATIVE
                if (!forceTrustedHashes &&
                    PlatformOps.IsWindowsOperatingSystem())
                {
                    goto native;
                }
#endif

                if (WinTrustDotNet.IsFileTrusted(
                        interpreter, trustedHashes, fileName,
                        fileHandle, userInterface, userPrompt,
                        revocation, install))
                {
                    TraceOps.DebugTrace(String.Format(
                        "IsFileTrusted: file {0} SUCCESS using {1} (1).",
                        FormatOps.WrapOrNull(fileName),
                        CommonOps.Runtime.GetRuntimeNameAndVMajorMinor()),
                        typeof(RuntimeOps).Name,
                        TracePriority.SecurityDebug4);

                    return true;
                }
                else
                {
                    TraceOps.DebugTrace(String.Format(
                        "IsFileTrusted: file {0} FAILURE using {1} (1).",
                        FormatOps.WrapOrNull(fileName),
                        CommonOps.Runtime.GetRuntimeNameAndVMajorMinor()),
                        typeof(RuntimeOps).Name,
                        TracePriority.SecurityError);

                    return false;
                }
            }
            #endregion

            ///////////////////////////////////////////////////////////////////

            #region Mono Support
#if MONO && MONO_BUILD
            if (CommonOps.Runtime.IsMono())
            {
#if NATIVE
                if (PlatformOps.IsWindowsOperatingSystem())
                    goto native;
#endif

                if (WinTrustMono.IsFileTrusted(
                        fileName, fileHandle, userInterface, userPrompt,
                        revocation, install))
                {
                    TraceOps.DebugTrace(String.Format(
                        "IsFileTrusted: file {0} SUCCESS using {1} (2).",
                        FormatOps.WrapOrNull(fileName),
                        CommonOps.Runtime.GetRuntimeNameAndVMajorMinor()),
                        typeof(RuntimeOps).Name,
                        TracePriority.SecurityDebug4);

                    return true;
                }
                else
                {
                    TraceOps.DebugTrace(String.Format(
                        "IsFileTrusted: file {0} FAILURE using {1} (2).",
                        FormatOps.WrapOrNull(fileName),
                        CommonOps.Runtime.GetRuntimeNameAndVMajorMinor()),
                        typeof(RuntimeOps).Name,
                        TracePriority.SecurityError);

                    return false;
                }
            }
#endif
            #endregion

            ///////////////////////////////////////////////////////////////////

            #region .NET Framework Support
#if NATIVE
        native:

            if (WinTrustOps.IsFileTrusted(
                    fileName, fileHandle, userInterface, userPrompt,
                    revocation, install))
            {
                TraceOps.DebugTrace(String.Format(
                    "IsFileTrusted: file {0} SUCCESS using {1} (3).",
                    FormatOps.WrapOrNull(fileName),
                    CommonOps.Runtime.GetRuntimeNameAndVMajorMinor()),
                    typeof(RuntimeOps).Name,
                    TracePriority.SecurityDebug4);

                return true;
            }
            else
            {
                TraceOps.DebugTrace(String.Format(
                    "IsFileTrusted: file {0} FAILURE using {1} (3).",
                    FormatOps.WrapOrNull(fileName),
                    CommonOps.Runtime.GetRuntimeNameAndVMajorMinor()),
                    typeof(RuntimeOps).Name,
                    TracePriority.SecurityError);

                return false;
            }
#endif
            #endregion

            ///////////////////////////////////////////////////////////////////

#if !NATIVE
            //
            // FIXME: Find some pure-managed way to do this?
            //
            // NOTE: Maybe use AuthenticodeSignatureInformation class
            //       if we took a dependency on the .NET Framework 3.5
            //       or higher?
            //
            treatAsDotNetCore = true;
            goto nonNative;
#endif
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method gets the vendor name associated with this assembly, as
        /// derived from the subject of its trusted code-signing certificate.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter context to use, if any.  This parameter may be null.
        /// </param>
        /// <param name="noCache">
        /// Non-zero to bypass any cached certificate information and re-read it
        /// from the underlying assembly file.
        /// </param>
        /// <returns>
        /// The vendor name, or null if it could not be determined.
        /// </returns>
        public static string GetVendor(
            Interpreter interpreter,
            bool noCache
            )
        {
            return GetCertificateSubject(
                interpreter, GlobalState.GetAssemblyLocation(), null,
                ShouldCheckCoreFileTrusted(), true, noCache);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method gets the simple-name subject of the code-signing
        /// certificate for the specified file, optionally requiring that the
        /// file be trusted.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter context to use, if any.  This parameter may be null.
        /// </param>
        /// <param name="fileName">
        /// The name of the file whose certificate subject is wanted.  This
        /// parameter may be null.
        /// </param>
        /// <param name="prefix">
        /// The optional prefix string to prepend to the returned subject.  This
        /// parameter may be null.
        /// </param>
        /// <param name="trusted">
        /// Non-zero to require that the file be trusted before its certificate
        /// subject is returned.
        /// </param>
        /// <param name="noParenthesis">
        /// Non-zero to strip any trailing parenthesized portion from the
        /// certificate simple-name.
        /// </param>
        /// <param name="noCache">
        /// Non-zero to bypass any cached certificate information and re-read it
        /// from the file.
        /// </param>
        /// <returns>
        /// The certificate subject simple-name, or null if it could not be
        /// determined.
        /// </returns>
        public static string GetCertificateSubject(
            Interpreter interpreter,
            string fileName,
            string prefix,
            bool trusted,
            bool noParenthesis,
            bool noCache
            )
        {
            if (trusted && (fileName != null))
            {
                X509Certificate2 certificate2 = null;

                if (CertificateOps.GetCertificate2(
                        fileName, noCache, ref certificate2) == ReturnCode.Ok)
                {
                    if ((certificate2 != null) && IsFileTrusted(
                            interpreter, null, fileName, IntPtr.Zero))
                    {
                        StringBuilder result = StringBuilderFactory.Create();

                        if (!String.IsNullOrEmpty(prefix))
                            result.Append(prefix);

                        string simpleName = certificate2.GetNameInfo(
                            X509NameType.SimpleName, false);

                        if (noParenthesis && (simpleName != null))
                        {
                            int index = simpleName.IndexOf(
                                Characters.OpenParenthesis);

                            if (index != Index.Invalid)
                            {
                                simpleName = simpleName.Substring(
                                    0, index).Trim();
                            }
                        }

                        result.Append(simpleName);

                        return StringBuilderCache.GetStringAndRelease(
                            ref result);
                    }
                }
            }

            return null;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method produces a list of name/value pairs describing the
        /// specified certificate.
        /// </summary>
        /// <param name="certificate">
        /// The certificate to describe.  This parameter may be null.
        /// </param>
        /// <param name="verbose">
        /// Non-zero to include additional details (e.g. issuer, serial number,
        /// hash, validity dates, and key algorithm) in the resulting list.
        /// </param>
        /// <returns>
        /// A list of name/value pairs describing the certificate, or null if
        /// <paramref name="certificate" /> is null.
        /// </returns>
        public static StringList CertificateToList(
            X509Certificate certificate,
            bool verbose
            )
        {
            StringList list = null;

            if (certificate != null)
            {
                list = new StringList();
                list.Add("subject", certificate.Subject);

                if (verbose)
                {
                    list.Add("issuer", certificate.Issuer);

                    list.Add("serialNumber",
                        certificate.GetSerialNumberString());

                    list.Add("hash",
                        certificate.GetCertHashString());

                    list.Add("effectiveDate",
                        certificate.GetEffectiveDateString());

                    list.Add("expirationDate",
                        certificate.GetExpirationDateString());

                    list.Add("algorithm",
                        certificate.GetKeyAlgorithm());

                    list.Add("algorithmParameters",
                        certificate.GetKeyAlgorithmParametersString());
                }
            }

            return list;
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Plugin Support Methods
        /// <summary>
        /// This method gets the package name to use for the specified plugin.
        /// </summary>
        /// <param name="pluginData">
        /// The plugin data to query.  This parameter may be null.
        /// </param>
        /// <param name="simple">
        /// Non-zero to construct the package name from the plugin simple-name
        /// and type name; otherwise, the full type name is used.
        /// </param>
        /// <returns>
        /// The plugin package name, or null if it could not be determined.
        /// </returns>
        public static string GetPluginPackageName(
            IPluginData pluginData,
            bool simple
            )
        {
            if (pluginData == null)
                return null;

            Type type = pluginData.GetType();

            if (type == null)
                return null; /* HACK: Impossible. */

            if (simple)
            {
                string simpleName = RuntimeOps.GetPluginSimpleName(
                    pluginData);

                string typeName = type.Name;

                if (!String.IsNullOrEmpty(simpleName) &&
                    !String.IsNullOrEmpty(typeName))
                {
                    return String.Format(
                        "{0}{1}{2}", simpleName, Type.Delimiter,
                        typeName);
                }
            }

            return type.FullName;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method gets the simple-name for the specified plugin, derived
        /// from its file name when available, or from its assembly name
        /// otherwise.
        /// </summary>
        /// <param name="pluginData">
        /// The plugin data to query.  This parameter may be null.
        /// </param>
        /// <returns>
        /// The plugin simple-name, or null if it could not be determined.
        /// </returns>
        public static string GetPluginSimpleName(
            IPluginData pluginData
            )
        {
            string simpleName = null;

            if (pluginData != null)
            {
                string fileName = pluginData.FileName;

                if (fileName != null)
                {
                    try
                    {
                        simpleName = Path.GetFileNameWithoutExtension(
                            fileName); /* throw */
                    }
                    catch (Exception e)
                    {
                        TraceOps.DebugTrace(
                            e, typeof(RuntimeOps).Name,
                            TracePriority.FileSystemError);
                    }
                }
                else
                {
                    AssemblyName assemblyName = pluginData.AssemblyName;

                    if (assemblyName != null)
                        simpleName = assemblyName.Name;
                }
            }

            return simpleName;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method builds a short, bracketed prefix string of single-letter
        /// codes representing the notable flags set on a plugin.
        /// </summary>
        /// <param name="flags">
        /// The plugin flags to translate into a prefix string.
        /// </param>
        /// <returns>
        /// The bracketed prefix string, or an empty string if none of the
        /// notable flags are set.
        /// </returns>
        public static string PluginFlagsToPrefix(
            PluginFlags flags
            )
        {
            StringBuilder result = StringBuilderFactory.Create();

            if (FlagOps.HasFlags(flags, PluginFlags.System, true))
                result.Append(Characters.S);

            if (FlagOps.HasFlags(flags, PluginFlags.Host, true))
                result.Append(Characters.H);

            if (FlagOps.HasFlags(flags, PluginFlags.Debugger, true))
                result.Append(Characters.D);

            if (FlagOps.HasFlags(flags, PluginFlags.Commercial, true) ||
                FlagOps.HasFlags(flags, PluginFlags.Proprietary, true))
            {
                result.Append(Characters.N); /* NOTE: Non-free. */
            }

            if (FlagOps.HasFlags(flags, PluginFlags.Licensed, true))
                result.Append(Characters.L);

#if ISOLATED_PLUGINS
            if (FlagOps.HasFlags(flags, PluginFlags.Isolated, true))
                result.Append(Characters.I);
#endif

            if (FlagOps.HasFlags(flags, PluginFlags.StrongName, true) &&
                FlagOps.HasFlags(flags, PluginFlags.Verified, true))
            {
                result.Append(Characters.V);
            }

            if (FlagOps.HasFlags(flags, PluginFlags.Authenticode, true) &&
                FlagOps.HasFlags(flags, PluginFlags.Trusted, true))
            {
                result.Append(Characters.T);
            }

            if (FlagOps.HasFlags(flags, PluginFlags.Primary, true))
                result.Append(Characters.P);

            if (FlagOps.HasFlags(flags, PluginFlags.UserInterface, true))
                result.Append(Characters.U);

            //
            // NOTE: Did the plugin have any special flags?
            //
            if (result.Length > 0)
            {
                result.Insert(0, Characters.OpenBracket);
                result.Append(Characters.CloseBracket);
                result.Append(Characters.Space);
            }

            return StringBuilderCache.GetStringAndRelease(ref result);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method determines whether the specified plugin satisfies the
        /// constraints expressed by the specified lookup flags (e.g. that it
        /// provides commands, functions, policies, or traces).
        /// </summary>
        /// <param name="pluginData">
        /// The plugin data to check.  This parameter may be null.
        /// </param>
        /// <param name="lookupFlags">
        /// The lookup flags expressing the constraints the plugin must satisfy.
        /// </param>
        /// <returns>
        /// True if the plugin satisfies all of the specified constraints;
        /// otherwise, false.
        /// </returns>
        public static bool CheckPluginVersusLookupFlags(
            IPluginData pluginData,
            LookupFlags lookupFlags
            )
        {
            if (pluginData == null)
                return false;

            if (FlagOps.HasFlags(
                    lookupFlags, LookupFlags.WithCommands, true))
            {
                LongList commandTokens = pluginData.CommandTokens;

                if ((commandTokens == null) ||
                    (commandTokens.Count == 0))
                {
                    return false;
                }
            }

            if (FlagOps.HasFlags(
                    lookupFlags, LookupFlags.WithFunctions, true))
            {
                LongList functionTokens = pluginData.FunctionTokens;

                if ((functionTokens == null) ||
                    (functionTokens.Count == 0))
                {
                    return false;
                }
            }

            if (FlagOps.HasFlags(
                    lookupFlags, LookupFlags.WithPolicies, true))
            {
                LongList policyTokens = pluginData.PolicyTokens;

                if ((policyTokens == null) ||
                    (policyTokens.Count == 0))
                {
                    return false;
                }
            }

            if (FlagOps.HasFlags(
                    lookupFlags, LookupFlags.WithTraces, true))
            {
                LongList traceTokens = pluginData.TraceTokens;

                if ((traceTokens == null) ||
                    (traceTokens.Count == 0))
                {
                    return false;
                }
            }

            return true;
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Command Line Support Methods
        /// <summary>
        /// This method parses a single index range of the form
        /// <c>&lt;start&gt;[-&lt;stop&gt;]</c>, where both indexes must be
        /// non-negative and, when a count is supplied, less than that count.
        /// </summary>
        /// <param name="value">
        /// The string value containing the index range to parse.
        /// </param>
        /// <param name="count">
        /// The number of available elements, used to bounds-check the parsed
        /// indexes; a negative value disables this check.
        /// </param>
        /// <param name="cultureInfo">
        /// The culture to use when parsing the integer index values.  This
        /// parameter may be null.
        /// </param>
        /// <param name="range">
        /// Upon success, this contains the parsed index range.
        /// </param>
        /// <param name="error">
        /// Upon failure, this contains an appropriate error message.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise, a non-Ok value
        /// with details placed in <paramref name="error" />.
        /// </returns>
        private static ReturnCode ParseIndexRange(
            string value,            /* in */
            int count,               /* in */
            CultureInfo cultureInfo, /* in */
            ref IndexRange range,    /* out */
            ref Result error         /* out */
            )
        {
            string trimValue;

            if (StringOps.IsLogicallyEmpty(value, out trimValue))
            {
                error = "empty index range is not allowed";
                return ReturnCode.Error;
            }

            ulong startIndex;
            ulong stopIndex;
            int minusIndex = trimValue.IndexOf(Characters.MinusSign);

            if (minusIndex != Index.Invalid)
            {
                startIndex = 0;

                if (Value.GetUnsignedWideInteger2(
                        trimValue.Substring(0, minusIndex).Trim(),
                        ValueFlags.AnyWideInteger, cultureInfo,
                        ref startIndex, ref error) != ReturnCode.Ok)
                {
                    return ReturnCode.Error;
                }

                if ((count >= 0) && (startIndex >= (ulong)count))
                {
                    error = String.Format(
                        "start index {0} must be less than count {1}",
                        startIndex, count);

                    return ReturnCode.Error;
                }

                stopIndex = 0;

                if (Value.GetUnsignedWideInteger2(
                        trimValue.Substring(minusIndex + 1).Trim(),
                        ValueFlags.AnyWideInteger, cultureInfo,
                        ref stopIndex, ref error) != ReturnCode.Ok)
                {
                    return ReturnCode.Error;
                }

                if ((count >= 0) && (stopIndex >= (ulong)count))
                {
                    error = String.Format(
                        "stop index {0} must be less than count {1}",
                        stopIndex, count);

                    return ReturnCode.Error;
                }

                if (startIndex > stopIndex)
                {
                    error = String.Format(
                        "start index {0} cannot exceed stop index {1}",
                        startIndex, stopIndex);

                    return ReturnCode.Error;
                }
            }
            else
            {
                startIndex = 0;

                if (Value.GetUnsignedWideInteger2(
                        trimValue, ValueFlags.AnyWideInteger,
                        cultureInfo, ref startIndex,
                        ref error) != ReturnCode.Ok)
                {
                    return ReturnCode.Error;
                }

                if ((count >= 0) && (startIndex >= (ulong)count))
                {
                    error = String.Format(
                        "index {0} must be less than count {1}",
                        startIndex, count);

                    return ReturnCode.Error;
                }

                stopIndex = startIndex;
            }

            range = new IndexRange(startIndex, stopIndex);
            return ReturnCode.Ok;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method parses a list of index ranges from the specified string
        /// value, appending the results to the specified list.
        /// </summary>
        /// <param name="value">
        /// The string value containing the comma-separated list of index ranges
        /// to parse.
        /// </param>
        /// <param name="count">
        /// The number of available elements, used to bounds-check the parsed
        /// indexes; a negative value disables this check.
        /// </param>
        /// <param name="cultureInfo">
        /// The culture to use when parsing the integer index values.  This
        /// parameter may be null.
        /// </param>
        /// <param name="ranges">
        /// On input, an optional existing list to append to; on output, this
        /// contains the parsed index ranges.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise, a non-Ok value.
        /// </returns>
        public static ReturnCode ParseIndexRanges(
            string value,             /* in */
            int count,                /* in */
            CultureInfo cultureInfo,  /* in */
            ref IndexRangeList ranges /* in, out */
            )
        {
            Result error = null;

            return ParseIndexRanges(
                value, count, cultureInfo, ref ranges, ref error);
        }

        ///////////////////////////////////////////////////////////////////////

        //
        // NOTE: This method is used to parse a list of "index ranges",
        //       which must be of the following form:
        //
        //       <start0>[-<end0>] ... [,<startN>[-<endN>]]
        //
        //       Where <start*> and <end*> must be non-negative integer
        //       values less than the specified count.
        //
        //       The minus sign must be used when specifying an ending
        //       index for the range; otherwise, it will be the same as
        //       the starting index.  The comma must be used in between
        //       each range.  If there is only one range, no comma is
        //       required.
        //
        //       Spaces are the only legal whitespace and they will be
        //       ignored.
        //
        /// <summary>
        /// This method parses a list of index ranges of the form
        /// <c>&lt;start0&gt;[-&lt;end0&gt;] ... [,&lt;startN&gt;[-&lt;endN&gt;]]</c>
        /// from the specified string value, appending the results to the
        /// specified list.
        /// </summary>
        /// <param name="value">
        /// The string value containing the comma-separated list of index ranges
        /// to parse.  This parameter may not be null.
        /// </param>
        /// <param name="count">
        /// The number of available elements, used to bounds-check the parsed
        /// indexes; a negative value disables this check.
        /// </param>
        /// <param name="cultureInfo">
        /// The culture to use when parsing the integer index values.  This
        /// parameter may be null.
        /// </param>
        /// <param name="ranges">
        /// On input, an optional existing list to append to; on output, this
        /// contains the parsed index ranges.
        /// </param>
        /// <param name="error">
        /// Upon failure, this contains an appropriate error message.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise, a non-Ok value
        /// with details placed in <paramref name="error" />.
        /// </returns>
        private static ReturnCode ParseIndexRanges(
            string value,              /* in */
            int count,                 /* in */
            CultureInfo cultureInfo,   /* in */
            ref IndexRangeList ranges, /* in, out */
            ref Result error           /* out */
            )
        {
            if (value == null)
            {
                error = "ranges cannot be null";
                return ReturnCode.Error;
            }

            Regex regEx = indexRangesRegEx;

            if ((regEx == null) || !regEx.IsMatch(value))
            {
                error = "index ranges syntax error";
                return ReturnCode.Error;
            }

            string[] parts = value.Split(Characters.Comma);

            if (parts == null) /* IMPOSSIBLE (?) */
            {
                error = "could not split index ranges";
                return ReturnCode.Error;
            }

            IndexRangeList localRanges = new IndexRangeList();
            int length = parts.Length;

            for (int index = 0; index < length; index++)
            {
                string part = parts[index];

                if (part == null)
                    continue;

                IndexRange localRange = null;

                if (ParseIndexRange(part,
                        count, cultureInfo, ref localRange,
                        ref error) != ReturnCode.Ok)
                {
                    return ReturnCode.Error;
                }

                if (localRange != null)
                    localRanges.Add(localRange);
            }

            if (ranges != null)
                ranges.AddRange(localRanges);
            else
                ranges = localRanges;

            return ReturnCode.Ok;
        }

        ///////////////////////////////////////////////////////////////////////

        #region Dead Code
#if DEAD_CODE
        /// <summary>
        /// This method invokes the specified callback once for each unique
        /// index covered by the specified list of index ranges, stopping early
        /// when the callback requests cancellation.
        /// </summary>
        /// <param name="count">
        /// The expected number of indexes, used to size the internal tracking
        /// of already-visited indexes.
        /// </param>
        /// <param name="ranges">
        /// The list of index ranges to process.  This parameter may not be
        /// null.
        /// </param>
        /// <param name="callback">
        /// The callback to invoke for each unique index.  This parameter may
        /// not be null.
        /// </param>
        /// <param name="clientData">
        /// The optional client data to pass to the callback; this value may be
        /// null.
        /// </param>
        /// <param name="error">
        /// Upon failure, receives information about the error that was
        /// encountered.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> if all indexes were processed (or the
        /// callback requested a successful early stop); otherwise, an
        /// appropriate error code.
        /// </returns>
        private static ReturnCode ProcessIndexRanges(
            int count,                   /* in */
            IndexRangeList ranges,       /* in */
            IndexRangeCallback callback, /* in */
            IClientData clientData,      /* in: OPTIONAL */
            ref Result error             /* out */
            )
        {
            if (ranges == null)
            {
                error = "ranges cannot be null";
                return ReturnCode.Error;
            }

            if (callback == null)
            {
                error = "invalid index range callback";
                return ReturnCode.Error;
            }

            IndexDictionary marks = new IndexDictionary(count);

            foreach (IndexRange range in ranges)
            {
                if (range == null) /* IMPOSSIBLE (?) */
                    continue;

                ulong startIndex = range.X;
                ulong stopIndex = range.Y;

                if (startIndex > stopIndex) /* IMPOSSIBLE (?) */
                {
                    error = String.Format(
                        "start index {0} cannot exceed stop index {1}",
                        startIndex, stopIndex);

                    return ReturnCode.Error;
                }

                if (stopIndex == ulong.MaxValue)
                {
                    error = String.Format(
                        "stop index {0} cannot exceed {1}",
                        stopIndex, ulong.MaxValue - 1);

                    return ReturnCode.Error;
                }

                for (ulong index = startIndex; index <= stopIndex; index++)
                {
                    bool mark;

                    if (marks.TryGetValue(index, out mark) && mark)
                        continue;

                    try
                    {
                        bool? cancel = callback(range, index, clientData);

                        if (cancel != null)
                        {
                            if ((bool)cancel)
                            {
                                return ReturnCode.Ok;
                            }
                            else
                            {
                                error = String.Format(
                                    "canceled at index range {0} and index {1}",
                                    range, index);

                                return ReturnCode.Error;
                            }
                        }
                    }
                    catch (Exception e)
                    {
                        error = e;
                        return ReturnCode.Error;
                    }

                    marks[index] = true;
                }
            }

            return ReturnCode.Ok;
        }
#endif
        #endregion

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method optionally evaluates a configured script command in order
        /// to escape (or otherwise transform) a portion of a command-line
        /// argument value.  When no command is configured, no evaluation is
        /// performed and the default handling is requested by the caller.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter context used to evaluate the configured command, if
        /// any.  This value cannot be null when a command is configured.
        /// </param>
        /// <param name="command">
        /// The script command, as a list of words, to be evaluated.  When this
        /// value is null, no evaluation is performed.
        /// </param>
        /// <param name="value">
        /// The argument value containing the substring being processed.
        /// </param>
        /// <param name="startIndex">
        /// The index of the first character of the substring being processed.
        /// </param>
        /// <param name="stopIndex">
        /// The index of the last character of the substring being processed.
        /// </param>
        /// <param name="mode">
        /// The escaping mode flags that govern how the substring should be
        /// processed.
        /// </param>
        /// <param name="result">
        /// Upon success, this contains the result of evaluating the configured
        /// command.  Upon failure, this contains an error message.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> upon success,
        /// <see cref="ReturnCode.Continue" /> when no command is configured, or
        /// another <see cref="ReturnCode" /> value to indicate failure or to
        /// alter the default handling.
        /// </returns>
        private static ReturnCode MaybeEscapeSubString(
            Interpreter interpreter, /* in */
            StringList command,      /* in */
            string value,            /* in */
            int startIndex,          /* in */
            int stopIndex,           /* in */
            EscapeMode mode,         /* in */
            ref Result result        /* out */
            )
        {
            if (command != null)
            {
                if (interpreter == null)
                {
                    result = "invalid interpreter";
                    return ReturnCode.Error;
                }

                StringList localCommand = new StringList(command);

                localCommand.Add(value);
                localCommand.Add(startIndex.ToString());
                localCommand.Add(stopIndex.ToString());
                localCommand.Add(mode.ToString());

                return interpreter.EvaluateScript(
                    localCommand.ToString(), ref result);
            }

            return ReturnCode.Continue;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method builds a single command-line string from the specified
        /// list of arguments, optionally restricting processing to the subset of
        /// arguments identified by a range specification.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter context used when evaluating any configured escaping
        /// command, if any.
        /// </param>
        /// <param name="args">
        /// The list of arguments to be included in the resulting command line.
        /// </param>
        /// <param name="rangeValue">
        /// The index range specification identifying which arguments should be
        /// quoted and escaped; arguments outside of these ranges are appended
        /// verbatim.
        /// </param>
        /// <param name="cultureInfo">
        /// The culture used when parsing the index range specification.
        /// </param>
        /// <param name="command">
        /// The optional script command, as a list of words, used to escape
        /// portions of the arguments being processed.
        /// </param>
        /// <param name="quoteAll">
        /// Non-zero to force every processed argument to be quoted.
        /// </param>
        /// <param name="forProcessor">
        /// Non-zero if the resulting command line is destined for a command
        /// processor (e.g. <c>cmd.exe</c>) and therefore requires additional
        /// escaping.
        /// </param>
        /// <param name="noComplain">
        /// Non-zero to suppress errors raised while evaluating any configured
        /// escaping command.
        /// </param>
        /// <param name="done">
        /// Upon return, this is set to non-zero if processing was stopped early
        /// before all arguments were appended.
        /// </param>
        /// <param name="error">
        /// Upon failure, this contains an error message or list of error
        /// messages.
        /// </param>
        /// <returns>
        /// The constructed command-line string, or null if an error was
        /// encountered.
        /// </returns>
        public static string BuildCommandLine(
            Interpreter interpreter, /* in */
            IList<string> args,      /* in */
            string rangeValue,       /* in */
            CultureInfo cultureInfo, /* in */
            StringList command,      /* in */
            bool quoteAll,           /* in */
            bool forProcessor,       /* in */
            bool noComplain,         /* in */
            ref bool done,           /* in, out */
            ref Result error         /* out */
            )
        {
            if (args == null)
            {
                error = "invalid argument list";
                return null;
            }

            StringBuilder builder = StringBuilderFactory.Create();

            ReturnCode code;
            ResultList errors = null;

            code = AppendCommandLine(
                interpreter, args, rangeValue, cultureInfo,
                command, quoteAll, forProcessor, noComplain,
                ref builder, ref done, ref errors);

            if (errors != null)
                error = errors;

            if (code != ReturnCode.Ok)
                return null;

            return StringBuilderCache.GetStringAndRelease(ref builder);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method populates a dictionary that marks each argument index
        /// falling within any of the specified index ranges.
        /// </summary>
        /// <param name="ranges">
        /// The list of index ranges identifying the argument indexes to be
        /// marked.  This value cannot be null.
        /// </param>
        /// <param name="marks">
        /// The dictionary that, upon return, contains an entry for each index
        /// covered by the specified ranges.  This value cannot be null.
        /// </param>
        /// <param name="error">
        /// Upon failure, this contains an error message.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise, an appropriate
        /// error code.
        /// </returns>
        private static ReturnCode MarkIndexRanges(
            IndexRangeList ranges, /* in */
            IndexDictionary marks, /* in */
            ref Result error       /* out */
            )
        {
            if (ranges == null)
            {
                error = "ranges cannot be null";
                return ReturnCode.Error;
            }

            if (marks == null)
            {
                error = "marks cannot be null";
                return ReturnCode.Error;
            }

            foreach (IndexRange range in ranges)
            {
                if (range == null) /* IMPOSSIBLE (?) */
                    continue;

                ulong startIndex = range.X;
                ulong stopIndex = range.Y;

                if (startIndex > stopIndex) /* IMPOSSIBLE (?) */
                {
                    error = String.Format(
                        "start index {0} cannot exceed stop index {1}",
                        startIndex, stopIndex);

                    return ReturnCode.Error;
                }

                if (stopIndex == ulong.MaxValue)
                {
                    error = String.Format(
                        "stop index {0} cannot exceed {1}",
                        stopIndex, ulong.MaxValue - 1);

                    return ReturnCode.Error;
                }

                for (ulong index = startIndex; index <= stopIndex; index++)
                {
                    bool indexVisited;

                    if (marks.TryGetValue(
                            index, out indexVisited) && indexVisited)
                    {
                        continue;
                    }

                    marks[index] = true;
                }
            }

            return ReturnCode.Ok;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method appends the specified list of arguments to a string
        /// builder, forming a command line.  Arguments identified by the index
        /// range specification are quoted and escaped, while all other arguments
        /// are appended verbatim.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter context used when evaluating any configured escaping
        /// command, if any.
        /// </param>
        /// <param name="args">
        /// The list of arguments to be appended to the command line.
        /// </param>
        /// <param name="rangeValue">
        /// The index range specification identifying which arguments should be
        /// quoted and escaped.
        /// </param>
        /// <param name="cultureInfo">
        /// The culture used when parsing the index range specification.
        /// </param>
        /// <param name="command">
        /// The optional script command, as a list of words, used to escape
        /// portions of the arguments being processed.
        /// </param>
        /// <param name="quoteAll">
        /// Non-zero to force every processed argument to be quoted.
        /// </param>
        /// <param name="forProcessor">
        /// Non-zero if the resulting command line is destined for a command
        /// processor and therefore requires additional escaping.
        /// </param>
        /// <param name="noComplain">
        /// Non-zero to suppress errors raised while evaluating any configured
        /// escaping command.
        /// </param>
        /// <param name="builder">
        /// The string builder to which the command line is appended.  If this
        /// value is null, a new string builder is created.
        /// </param>
        /// <param name="done">
        /// Upon return, this is set to non-zero if processing was stopped early
        /// before all arguments were appended.
        /// </param>
        /// <param name="errors">
        /// Upon failure, this contains the list of error messages encountered.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise, an appropriate
        /// error code.
        /// </returns>
        private static ReturnCode AppendCommandLine(
            Interpreter interpreter,   /* in */
            IList<string> args,        /* in */
            string rangeValue,         /* in */
            CultureInfo cultureInfo,   /* in */
            StringList command,        /* in */
            bool quoteAll,             /* in */
            bool forProcessor,         /* in */
            bool noComplain,           /* in */
            ref StringBuilder builder, /* in, out */
            ref bool done,             /* in, out */
            ref ResultList errors      /* in, out */
            )
        {
            if (args == null)
            {
                if (errors == null)
                    errors = new ResultList();

                errors.Add("invalid argument list");
                return ReturnCode.Error;
            }

            Result error; /* REUSED */
            int count = args.Count;
            IndexRangeList ranges = null;

            error = null;

            if (ParseIndexRanges(
                    rangeValue, count, cultureInfo, ref ranges,
                    ref error) != ReturnCode.Ok)
            {
                if (error != null)
                {
                    if (errors == null)
                        errors = new ResultList();

                    errors.Add("invalid argument list");
                }

                return ReturnCode.Error;
            }

            IndexDictionary marks = new IndexDictionary(count);

            error = null;

            if (MarkIndexRanges(
                    ranges, marks, ref error) != ReturnCode.Ok)
            {
                return ReturnCode.Error;
            }

            if (builder == null)
                builder = StringBuilderFactory.Create();

            for (int index = 0; index < count; index++)
            {
                string arg = args[(int)index];
                bool mark;

                if (marks.TryGetValue((ulong)index, out mark) && mark)
                {
                    /* NO RESULT */
                    AppendCommandLineArgument(
                        interpreter, builder, command, arg, quoteAll,
                        forProcessor, noComplain, ref done, ref errors);

                    if (done)
                        break;
                }
                else
                {
                    if (builder.Length > 0)
                        builder.Append(Characters.Space);

                    builder.Append(arg);
                }
            }

            return ReturnCode.Ok;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method appends a single argument to a string builder, applying
        /// the quoting and escaping rules necessary to round-trip the argument
        /// through a command-line parser, optionally consulting a configured
        /// script command to escape individual characters.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter context used when evaluating any configured escaping
        /// command, if any.
        /// </param>
        /// <param name="builder">
        /// The string builder to which the escaped argument is appended.  This
        /// value cannot be null.
        /// </param>
        /// <param name="command">
        /// The optional script command, as a list of words, used to escape
        /// portions of the argument being processed.
        /// </param>
        /// <param name="arg">
        /// The argument value to be appended.  This value cannot be null.
        /// </param>
        /// <param name="quoteAll">
        /// Non-zero to force the argument to be quoted even when it contains no
        /// special characters.
        /// </param>
        /// <param name="forProcessor">
        /// Non-zero if the resulting command line is destined for a command
        /// processor and therefore requires additional escaping.
        /// </param>
        /// <param name="noComplain">
        /// Non-zero to suppress errors raised while evaluating any configured
        /// escaping command.
        /// </param>
        /// <param name="done">
        /// Upon return, this is set to non-zero if processing should be stopped
        /// early and no further arguments should be appended.
        /// </param>
        /// <param name="errors">
        /// Upon failure, this contains the list of error messages encountered.
        /// </param>
        private static void AppendCommandLineArgument(
            Interpreter interpreter, /* in */
            StringBuilder builder,   /* in, out */
            StringList command,      /* in */
            string arg,              /* in */
            bool quoteAll,           /* in */
            bool forProcessor,       /* in */
            bool noComplain,         /* in */
            ref bool done,           /* in, out */
            ref ResultList errors    /* in, out */
            )
        {
            if (builder == null) /* IMPOSSIBLE (?) */
            {
                if (errors == null)
                    errors = new ResultList();

                errors.Add("invalid string builder");

                done = true; /* HACK: Cannot continue. */

                return;
            }

            if (arg == null) /* IMPOSSIBLE (?) */
            {
                if (errors == null)
                    errors = new ResultList();

                errors.Add("invalid command argument");

                done = true; /* HACK: Cannot continue. */

                return;
            }

            char[] specials = {
                Characters.Space, Characters.QuotationMark,
                Characters.Backslash
            };

            if (builder.Length > 0)
                builder.Append(Characters.Space);

            bool wrap = quoteAll ||
                (arg.IndexOfAny(specials) != Index.Invalid);

            EscapeMode escapeMode = EscapeMode.Default;

            if (quoteAll)
                escapeMode |= EscapeMode.QuoteAll;

            if (forProcessor)
                escapeMode |= EscapeMode.ForProcessor;

            if (noComplain)
                escapeMode |= EscapeMode.NoComplain;

            Result result; /* REUSED */

            if (wrap)
            {
                result = null;

                switch (MaybeEscapeSubString(
                        interpreter, command, arg, Index.Invalid,
                        Index.Invalid, escapeMode | EscapeMode.Start,
                        ref result))
                {
                    case ReturnCode.Ok:
                        {
                            //
                            // NOTE: Character (which may have
                            //       been "escaped") should be
                            //       added via that result and
                            //       all default handling will
                            //       be skipped.
                            //
                            if (result != null)
                                builder.Append(result);

                            break;
                        }
                    case ReturnCode.Error: /* Warning (?) */
                        {
                            //
                            // NOTE: Character is forbidden (?),
                            //       maybe record an error and
                            //       then continue.
                            //
                            if (result != null)
                            {
                                if (errors == null)
                                    errors = new ResultList();

                                errors.Add(result);
                            }

                            break;
                        }
                    case ReturnCode.Return:
                        {
                            //
                            // NOTE: Character was considered to
                            //       stop all further processing
                            //       and return immediately.
                            //
                            if (result != null)
                            {
                                if (errors == null)
                                    errors = new ResultList();

                                errors.Add(result);
                            }

                            done = true;

                            return;
                        }
                    case ReturnCode.Break:
                        {
                            //
                            // NOTE: Character was considered to
                            //       stop all further processing
                            //       and finalize the argument.
                            //
                            if (result != null)
                                builder.Append(result);

                            goto case ReturnCode.Continue;
                        }
                    case ReturnCode.Continue:
                    default:
                        {
                            //
                            // NOTE: Character should be treated
                            //       as normal, do nothing extra
                            //       and then continue argument
                            //       processing as normal.
                            //
                            builder.Append(Characters.QuotationMark);
                            break;
                        }
                }
            }

            int length = arg.Length;

            for (int index = 0; index < length; index++)
            {
                result = null;

                switch (MaybeEscapeSubString(
                        interpreter, command, arg, index,
                        index, escapeMode | EscapeMode.Middle,
                        ref result))
                {
                    case ReturnCode.Ok:
                        {
                            //
                            // NOTE: Character (which may have
                            //       been "escaped") should be
                            //       added via that result and
                            //       all default handling will
                            //       be skipped.
                            //
                            if (result != null)
                                builder.Append(result);

                            continue;
                        }
                    case ReturnCode.Error: /* Warning (?) */
                        {
                            //
                            // NOTE: Character is forbidden (?),
                            //       maybe record an error and
                            //       then continue.
                            //
                            if (result != null)
                            {
                                if (errors == null)
                                    errors = new ResultList();

                                errors.Add(result);
                            }

                            continue;
                        }
                    case ReturnCode.Return:
                        {
                            //
                            // NOTE: Character was considered to
                            //       stop all further processing
                            //       and return immediately.
                            //
                            if (result != null)
                            {
                                if (errors == null)
                                    errors = new ResultList();

                                errors.Add(result);
                            }

                            done = true;

                            return;
                        }
                    case ReturnCode.Break:
                        {
                            //
                            // NOTE: Character was considered to
                            //       stop all further processing
                            //       and finalize the argument.
                            //
                            if (result != null)
                                builder.Append(result);

                            goto case ReturnCode.Continue;
                        }
                    case ReturnCode.Continue:
                    default:
                        {
                            //
                            // NOTE: Character should be treated
                            //       as normal, do nothing extra
                            //       and then continue argument
                            //       processing as normal.
                            //
                            break;
                        }
                }

                if (forProcessor && (arg[index] == Characters.Caret))
                {
                    builder.Append(Characters.Caret, 2);
                }
                else if (arg[index] == Characters.QuotationMark)
                {
                    if (forProcessor)
                    {
                        builder.Append(Characters.Backslash);
                        builder.Append(Characters.Caret);
                    }
                    else
                    {
                        builder.Append(Characters.Backslash);
                    }

                    builder.Append(Characters.QuotationMark);
                }
                else if (arg[index] == Characters.Backslash)
                {
                    int count = 0;

                    while ((index < length) &&
                        (arg[index] == Characters.Backslash))
                    {
                        count++; index++;
                    }

                    if (index < length)
                    {
                        if (arg[index] == Characters.QuotationMark)
                        {
                            builder.Append(
                                Characters.Backslash, (count * 2) + 1);

                            if (forProcessor)
                                builder.Append(Characters.Caret);

                            builder.Append(Characters.QuotationMark);
                        }
                        else
                        {
                            builder.Append(Characters.Backslash, count);
                            builder.Append(arg[index]);
                        }
                    }
                    else
                    {
                        if (forProcessor)
                            builder.Append(Characters.Backslash, count);
                        else
                            builder.Append(Characters.Backslash, count * 2);

                        break;
                    }
                }
                else
                {
                    builder.Append(arg[index]);
                }
            }

            if (wrap)
            {
                result = null;

                switch (MaybeEscapeSubString(
                        interpreter, command, arg, Index.Invalid,
                        Index.Invalid, escapeMode | EscapeMode.End,
                        ref result))
                {
                    case ReturnCode.Ok:
                        {
                            //
                            // NOTE: Character (which may have
                            //       been "escaped") should be
                            //       added via that result and
                            //       all default handling will
                            //       be skipped.
                            //
                            if (result != null)
                                builder.Append(result);

                            break;
                        }
                    case ReturnCode.Error: /* Warning (?) */
                        {
                            //
                            // NOTE: Character is forbidden (?),
                            //       maybe record an error and
                            //       then continue.
                            //
                            if (result != null)
                            {
                                if (errors == null)
                                    errors = new ResultList();

                                errors.Add(result);
                            }

                            break;
                        }
                    case ReturnCode.Return:
                        {
                            //
                            // NOTE: Character was considered to
                            //       stop all further processing
                            //       and return immediately.
                            //
                            if (result != null)
                            {
                                if (errors == null)
                                    errors = new ResultList();

                                errors.Add(result);
                            }

                            done = true;

                            return;
                        }
                    case ReturnCode.Break:
                        {
                            //
                            // NOTE: Character was considered to
                            //       stop all further processing
                            //       and finalize the argument.
                            //
                            if (result != null)
                                builder.Append(result);

                            goto case ReturnCode.Continue;
                        }
                    case ReturnCode.Continue:
                    default:
                        {
                            //
                            // NOTE: Character should be treated
                            //       as normal, do nothing extra
                            //       and then continue argument
                            //       processing as normal.
                            //
                            builder.Append(Characters.QuotationMark);
                            break;
                        }
                }
            }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method builds a single command-line string from the specified
        /// sequence of arguments, quoting and escaping each argument as
        /// necessary.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter context used when evaluating any configured escaping
        /// command, if any.
        /// </param>
        /// <param name="args">
        /// The sequence of arguments to be included in the resulting command
        /// line.
        /// </param>
        /// <param name="command">
        /// The optional script command, as a list of words, used to escape
        /// portions of the arguments being processed.
        /// </param>
        /// <param name="quoteAll">
        /// Non-zero to force every argument to be quoted.
        /// </param>
        /// <param name="forProcessor">
        /// Non-zero if the resulting command line is destined for a command
        /// processor and therefore requires additional escaping.
        /// </param>
        /// <param name="noComplain">
        /// Non-zero to suppress errors raised while evaluating any configured
        /// escaping command.
        /// </param>
        /// <param name="done">
        /// Upon return, this is set to non-zero if processing was stopped early
        /// before all arguments were appended.
        /// </param>
        /// <param name="error">
        /// Upon failure, this contains an error message or list of error
        /// messages.
        /// </param>
        /// <returns>
        /// The constructed command-line string.
        /// </returns>
        public static string BuildCommandLine(
            Interpreter interpreter,  /* in */
            IEnumerable<string> args, /* in */
            StringList command,       /* in */
            bool quoteAll,            /* in */
            bool forProcessor,        /* in */
            bool noComplain,          /* in */
            ref bool done,            /* in, out */
            ref Result error          /* out */
            )
        {
            if (args == null)
            {
                error = "invalid argument list";
                return null;
            }

            StringBuilder builder = StringBuilderFactory.Create();
            ResultList errors = null;

            foreach (string arg in args)
            {
                /* NO RESULT */
                AppendCommandLineArgument(
                    interpreter, builder, command, arg, quoteAll,
                    forProcessor, noComplain, ref done, ref errors);

                if (done)
                    break;
            }

            if (errors != null)
                error = errors;

            return StringBuilderCache.GetStringAndRelease(ref builder);
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Alias Support Methods
        /// <summary>
        /// This method builds the list of arguments used to create an alias that
        /// forwards to the nested interpreter evaluation command.
        /// </summary>
        /// <param name="interpreterName">
        /// The name of the target interpreter, if any, to be appended to the
        /// resulting argument list.
        /// </param>
        /// <param name="objectOptionType">
        /// The object option type associated with the alias.  This parameter is
        /// not used by this method.
        /// </param>
        /// <param name="arguments">
        /// Upon return, this contains the constructed argument list.
        /// </param>
        /// <param name="error">
        /// Upon failure, this contains an error message.  This parameter is not
        /// used by this method.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise, an appropriate
        /// error code.
        /// </returns>
        public static ReturnCode GetInterpreterAliasArguments(
            string interpreterName,            /* in */
            ObjectOptionType objectOptionType, /* in: NOT USED */
            ref ArgumentList arguments,        /* out */
            ref Result error                   /* out: NOT USED */
            )
        {
            arguments = new ArgumentList((IEnumerable<string>)new string[] {
                ScriptOps.TypeNameToEntityName(typeof(_Commands.Interp)),
                "eval"
            });

            if (interpreterName != null)
                arguments.Add(interpreterName);

            return ReturnCode.Ok;
        }

        ///////////////////////////////////////////////////////////////////////

#if EMIT && NATIVE && LIBRARY
        /// <summary>
        /// This method builds the list of arguments used to create an alias that
        /// forwards to the native library delegate invocation command.
        /// </summary>
        /// <param name="delegateName">
        /// The name of the target delegate, if any, to be appended to the
        /// resulting argument list.
        /// </param>
        /// <param name="objectOptionType">
        /// The object option type associated with the alias.  This parameter is
        /// not used by this method.
        /// </param>
        /// <param name="arguments">
        /// Upon return, this contains the constructed argument list.
        /// </param>
        /// <param name="error">
        /// Upon failure, this contains an error message.  This parameter is not
        /// used by this method.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise, an appropriate
        /// error code.
        /// </returns>
        public static ReturnCode GetLibraryAliasArguments(
            string delegateName,               /* in */
            ObjectOptionType objectOptionType, /* in: NOT USED */
            ref ArgumentList arguments,        /* out */
            ref Result error                   /* out: NOT USED */
            )
        {
            arguments = new ArgumentList((IEnumerable<string>)new string[] {
                ScriptOps.TypeNameToEntityName(typeof(_Commands.Library)),
                "call"
            });

            if (delegateName != null)
                arguments.Add(delegateName);

            return ReturnCode.Ok;
        }
#endif

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method determines the sub-command name corresponding to the
        /// object invocation option encoded within the specified object option
        /// type.
        /// </summary>
        /// <param name="objectOptionType">
        /// The object option type whose object invocation sub-command is to be
        /// determined.
        /// </param>
        /// <returns>
        /// The name of the corresponding sub-command, or null if the object
        /// option type does not specify a supported invocation option.
        /// </returns>
        private static string GetObjectAliasSubCommand(
            ObjectOptionType objectOptionType /* in */
            )
        {
            ObjectOptionType maskedObjectOptionType =
                objectOptionType & ObjectOptionType.ObjectInvokeOptionMask;

            switch (maskedObjectOptionType)
            {
                case ObjectOptionType.Invoke:
                    return "invoke";
                case ObjectOptionType.InvokeRaw:
                    return "invokeraw";
                case ObjectOptionType.InvokeAll:
                    return "invokeall";
            }

            return null;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method builds the list of arguments used to create an alias that
        /// forwards to the appropriate object invocation command, as determined
        /// by the specified object option type.
        /// </summary>
        /// <param name="objectName">
        /// The name of the target object, if any, to be appended to the
        /// resulting argument list.
        /// </param>
        /// <param name="objectOptionType">
        /// The object option type used to select the object invocation
        /// sub-command.
        /// </param>
        /// <param name="arguments">
        /// Upon return, this contains the constructed argument list.
        /// </param>
        /// <param name="error">
        /// Upon failure, this contains an error message.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise, an appropriate
        /// error code.
        /// </returns>
        public static ReturnCode GetObjectAliasArguments(
            string objectName,                 /* in */
            ObjectOptionType objectOptionType, /* in */
            ref ArgumentList arguments,        /* out */
            ref Result error                   /* out */
            )
        {
            string subCommand = GetObjectAliasSubCommand(objectOptionType);

            if (subCommand == null)
            {
                error = "invalid sub-command";
                return ReturnCode.Error;
            }

            arguments = new ArgumentList((IEnumerable<string>)new string[] {
                ScriptOps.TypeNameToEntityName(typeof(_Commands.Object)),
                subCommand
            });

            if (objectName != null)
                arguments.Add(objectName);

            return ReturnCode.Ok;
        }

        ///////////////////////////////////////////////////////////////////////

#if NATIVE && TCL
        /// <summary>
        /// This method builds the list of arguments used to create an alias that
        /// forwards to the native Tcl interpreter evaluation command.
        /// </summary>
        /// <param name="interpName">
        /// The name of the target Tcl interpreter, if any, to be appended to the
        /// resulting argument list.
        /// </param>
        /// <param name="objectOptionType">
        /// The object option type associated with the alias.  This parameter is
        /// not used by this method.
        /// </param>
        /// <param name="arguments">
        /// Upon return, this contains the constructed argument list.
        /// </param>
        /// <param name="error">
        /// Upon failure, this contains an error message.  This parameter is not
        /// used by this method.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise, an appropriate
        /// error code.
        /// </returns>
        public static ReturnCode GetTclAliasArguments(
            string interpName,                 /* in */
            ObjectOptionType objectOptionType, /* in: NOT USED */
            ref ArgumentList arguments,        /* out */
            ref Result error                   /* out: NOT USED */
            )
        {
            arguments = new ArgumentList((IEnumerable<string>)new string[] {
                ScriptOps.TypeNameToEntityName(typeof(_Commands.Tcl)),
                "eval"
            });

            if (interpName != null)
                arguments.Add(interpName);

            return ReturnCode.Ok;
        }
#endif

        ///////////////////////////////////////////////////////////////////////

        //
        // NOTE: This method is static; however, in the future that may no
        //       longer be true.  In that case, it will need to move back to
        //       the Interpreter class.
        //
        /// <summary>
        /// This method creates a new alias command that, when executed in the
        /// source interpreter, forwards to the specified target along with the
        /// configured arguments and options.
        /// </summary>
        /// <param name="name">
        /// The name of the alias command being created.
        /// </param>
        /// <param name="flags">
        /// The command flags to associate with the new alias command.
        /// </param>
        /// <param name="aliasFlags">
        /// The alias-specific flags to associate with the new alias.
        /// </param>
        /// <param name="clientData">
        /// The client-specific data to associate with the new alias command, if
        /// any.
        /// </param>
        /// <param name="nameToken">
        /// The name token used to identify the alias within the target
        /// interpreter.
        /// </param>
        /// <param name="sourceInterpreter">
        /// The interpreter in which the alias command is defined.
        /// </param>
        /// <param name="targetInterpreter">
        /// The interpreter in which the alias target is executed.
        /// </param>
        /// <param name="sourceNamespace">
        /// The namespace, if any, associated with the alias in the source
        /// interpreter.
        /// </param>
        /// <param name="targetNamespace">
        /// The namespace, if any, associated with the alias target in the target
        /// interpreter.
        /// </param>
        /// <param name="target">
        /// The execution target to which the alias forwards.
        /// </param>
        /// <param name="arguments">
        /// The list of arguments to be prepended when the alias forwards to its
        /// target, if any.
        /// </param>
        /// <param name="options">
        /// The options to associate with the alias, if any.
        /// </param>
        /// <param name="startIndex">
        /// The index of the first argument, supplied by the caller of the alias,
        /// that should be forwarded to the target.
        /// </param>
        /// <returns>
        /// The newly created alias.
        /// </returns>
        public static IAlias NewAlias(
            string name,                   /* in */
            CommandFlags flags,            /* in */
            AliasFlags aliasFlags,         /* in */
            IClientData clientData,        /* in */
            string nameToken,              /* in */
            Interpreter sourceInterpreter, /* in */
            Interpreter targetInterpreter, /* in */
            INamespace sourceNamespace,    /* in */
            INamespace targetNamespace,    /* in */
            IExecute target,               /* in */
            ArgumentList arguments,        /* in */
            OptionDictionary options,      /* in */
            int startIndex                 /* in */
            )
        {
            //
            // HACK: We do not necessarily know (and do not simply want to
            //       "guess") the plugin associated with the target of the
            //       command; therefore, we use a null value for the plugin
            //       argument here.
            //
            return new _Commands.Alias(
                new CommandData(name, null, null, clientData,
                    typeof(_Commands.Alias).FullName, flags,
                    /* plugin */ null, 0),
                new AliasData(nameToken, sourceInterpreter,
                    targetInterpreter, sourceNamespace, targetNamespace,
                    target, arguments, options, aliasFlags, startIndex, 0));
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Notification Support Methods
        /// <summary>
        /// This method creates a new script event arguments object suitable for
        /// use when raising a notification.
        /// </summary>
        /// <param name="type">
        /// The type of notification being raised.
        /// </param>
        /// <param name="flags">
        /// The flags associated with the notification being raised.
        /// </param>
        /// <param name="interpreter">
        /// The interpreter context associated with the notification, if any.
        /// </param>
        /// <param name="clientData">
        /// The client-specific data associated with the notification, if any.
        /// </param>
        /// <param name="arguments">
        /// The list of arguments associated with the notification, if any.
        /// </param>
        /// <param name="result">
        /// The result associated with the notification, if any.
        /// </param>
        /// <param name="exception">
        /// The script exception associated with the notification, if any.
        /// </param>
        /// <param name="interruptType">
        /// The type of interrupt associated with the notification, if any.
        /// </param>
        /// <returns>
        /// The newly created script event arguments object.
        /// </returns>
        public static IScriptEventArgs GetEventArgs(
            NotifyType type,
            NotifyFlags flags,
            Interpreter interpreter,
            IClientData clientData,
            ArgumentList arguments,
            Result result,
            ScriptException exception,
            InterruptType interruptType
            )
        {
            return new ScriptEventArgs(
                GlobalState.NextId(interpreter), type, flags, interpreter,
                clientData, arguments, result, exception, interruptType, null,
                null);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method creates a new script event arguments object suitable for
        /// use when raising a notification about an interpreter interrupt.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter context associated with the interrupt, if any.
        /// </param>
        /// <param name="interruptType">
        /// The type of interrupt that has occurred.
        /// </param>
        /// <param name="clientData">
        /// The client-specific data associated with the interrupt, if any.
        /// </param>
        /// <returns>
        /// The newly created script event arguments object.
        /// </returns>
        public static IScriptEventArgs GetInterruptEventArgs(
            Interpreter interpreter,
            InterruptType interruptType,
            IClientData clientData
            )
        {
            NotifyType notifyType = NotifyType.Script;

            if (interruptType == InterruptType.Deleted)
                notifyType = NotifyType.Interpreter;
#if DEBUGGER
            else if (interruptType == InterruptType.Halted)
                notifyType = NotifyType.Debugger;
#endif

            return GetEventArgs(
                notifyType, NotifyFlags.Interrupted, interpreter, clientData,
                null, null, null, interruptType);
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Channel Support Methods
        /// <summary>
        /// This method returns the effective channel name for the specified
        /// channel type, falling back to the appropriate standard channel name
        /// when an explicit name is not provided.
        /// </summary>
        /// <param name="name">
        /// The explicit channel name to use, if any.
        /// </param>
        /// <param name="channelType">
        /// The type of channel for which a name is being determined.
        /// </param>
        /// <returns>
        /// The effective channel name, which may be the provided name or one of
        /// the standard channel names.
        /// </returns>
        public static string ChannelTypeToName(
            string name,
            ChannelType channelType
            )
        {
            if (FlagOps.HasFlags(channelType, ChannelType.Input, true))
                return (name != null) ? name : StandardChannel.Input;

            if (FlagOps.HasFlags(channelType, ChannelType.Output, true))
                return (name != null) ? name : StandardChannel.Output;

            if (FlagOps.HasFlags(channelType, ChannelType.Error, true))
                return (name != null) ? name : StandardChannel.Error;

            return name;
        }

        ///////////////////////////////////////////////////////////////////////

        #region Dead Code
#if DEAD_CODE
        /// <summary>
        /// This method determines whether the specified name refers to one of
        /// the standard channels (input, output, or error).
        /// </summary>
        /// <param name="name">
        /// The channel name to check; this value may be null.
        /// </param>
        /// <returns>
        /// True if the name refers to a standard channel; otherwise, false.
        /// </returns>
        private static bool IsStandardChannelName( /* NOT USED */
            string name
            )
        {
            if (SharedStringOps.SystemEquals(
                    name, StandardChannel.Input))
            {
                return true;
            }

            if (SharedStringOps.SystemEquals(
                    name, StandardChannel.Output))
            {
                return true;
            }

            if (SharedStringOps.SystemEquals(
                    name, StandardChannel.Error))
            {
                return true;
            }

            return false;
        }
#endif
        #endregion
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Resource Support Methods
        /// <summary>
        /// This method attempts to open a manifest resource stream with the
        /// specified name from the specified assembly.
        /// </summary>
        /// <param name="assembly">
        /// The assembly from which the manifest resource stream should be
        /// opened.
        /// </param>
        /// <param name="name">
        /// The name of the manifest resource stream to open.
        /// </param>
        /// <param name="error">
        /// Upon failure, this contains an error message that describes the
        /// problem encountered.
        /// </param>
        /// <returns>
        /// The opened stream, or null if the stream could not be opened.
        /// </returns>
        public static Stream GetStream(
            Assembly assembly,
            string name,
            ref Result error
            )
        {
            if (assembly != null)
            {
                try
                {
                    return assembly.GetManifestResourceStream(name);
                }
                catch (Exception e)
                {
                    error = e;
                }
            }
            else
            {
                error = InvalidAssemblyResourceManager;
            }

            return null;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method searches all assemblies currently loaded into the
        /// application domain for one that contains a manifest resource stream
        /// with the specified name.
        /// </summary>
        /// <param name="name">
        /// The name of the manifest resource stream to search for.
        /// </param>
        /// <param name="verbose">
        /// Non-zero if errors encountered while searching individual assemblies
        /// should be accumulated into the error list.
        /// </param>
        /// <param name="errors">
        /// Upon failure, this contains the list of error messages that describe
        /// the problems encountered.
        /// </param>
        /// <returns>
        /// The assembly that contains the named manifest resource stream, or
        /// null if no such assembly was found.
        /// </returns>
        private static Assembly FindStream(
            string name,
            bool verbose,
            ref ResultList errors
            )
        {
            AppDomain appDomain = AppDomainOps.GetCurrent();

            if (appDomain == null)
            {
                if (errors == null)
                    errors = new ResultList();

                errors.Add("invalid application domain");
                return null;
            }

            Assembly[] assemblies = appDomain.GetAssemblies();

            if (assemblies == null)
            {
                if (errors == null)
                    errors = new ResultList();

                errors.Add("invalid assemblies");
                return null;
            }

            foreach (Assembly assembly in assemblies)
            {
                Result error = null;

                if (GetStream(assembly, name, ref error) != null)
                    return assembly;

                if (verbose && (error != null))
                {
                    if (errors == null)
                        errors = new ResultList();

                    errors.Add(error);
                }
            }

            return null;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method searches for a manifest resource stream with the
        /// specified name, first within the specified assembly and then within
        /// all assemblies currently loaded into the application domain.
        /// </summary>
        /// <param name="assembly">
        /// The assembly to search first, if any.
        /// </param>
        /// <param name="name">
        /// The name of the manifest resource stream to search for.
        /// </param>
        /// <param name="verbose">
        /// Non-zero if errors encountered while searching individual assemblies
        /// should be accumulated into the error message.
        /// </param>
        /// <param name="error">
        /// Upon failure, this contains an error message that describes the
        /// problem encountered.
        /// </param>
        /// <returns>
        /// The assembly that contains the named manifest resource stream, or
        /// null if no such assembly was found.
        /// </returns>
        public static Assembly FindStream(
            Assembly assembly,
            string name,
            bool verbose,
            ref Result error
            )
        {
            ResultList errors = null;
            Result localError; /* REUSED */

            if (assembly != null)
            {
                localError = null;

                if (GetStream(assembly,
                        name, ref localError) != null)
                {
                    return assembly;
                }

                if (verbose && (localError != null))
                {
                    if (errors == null)
                        errors = new ResultList();

                    errors.Add(localError);
                }
            }
            else if (verbose)
            {
                if (errors == null)
                    errors = new ResultList();

                errors.Add("no specific assembly");
            }

            Assembly localAssembly = FindStream(
                name, verbose, ref errors);

            if (localAssembly != null)
                return localAssembly;

            if (errors == null)
                errors = new ResultList();

            errors.Insert(0, String.Format(
                "resource {0} not found in application domain",
                FormatOps.WrapOrNull(name)));

            if (errors != null) /* REDUNDANT */
                error = errors;

            return null;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method attempts to open a stream for the resource with the
        /// specified name using the specified resource manager and culture.
        /// </summary>
        /// <param name="resourceManager">
        /// The resource manager from which the stream should be opened.
        /// </param>
        /// <param name="name">
        /// The name of the resource to open.
        /// </param>
        /// <param name="cultureInfo">
        /// The culture for which the resource should be opened, if any.
        /// </param>
        /// <param name="error">
        /// Upon failure, this contains an error message that describes the
        /// problem encountered.
        /// </param>
        /// <returns>
        /// The opened stream, or null if the stream could not be opened.
        /// </returns>
        public static Stream GetStream(
            ResourceManager resourceManager,
            string name,
            CultureInfo cultureInfo,
            ref Result error
            )
        {
            if (resourceManager != null)
            {
                try
                {
                    return resourceManager.GetStream(name, cultureInfo);
                }
                catch (Exception e)
                {
                    error = e;
                }
            }
            else
            {
                error = InvalidPluginResourceManager;
            }

            return null;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method attempts to retrieve the string value of the resource
        /// with the specified name using the specified resource manager and
        /// culture.
        /// </summary>
        /// <param name="resourceManager">
        /// The resource manager from which the string value should be
        /// retrieved.
        /// </param>
        /// <param name="name">
        /// The name of the resource to retrieve.
        /// </param>
        /// <param name="cultureInfo">
        /// The culture for which the resource should be retrieved, if any.
        /// </param>
        /// <param name="error">
        /// Upon failure, this contains an error message that describes the
        /// problem encountered.
        /// </param>
        /// <returns>
        /// The string value of the resource, or null if it could not be
        /// retrieved.
        /// </returns>
        public static string GetString(
            ResourceManager resourceManager,
            string name,
            CultureInfo cultureInfo,
            ref Result error
            )
        {
            if (resourceManager != null)
            {
                try
                {
                    return resourceManager.GetString(name, cultureInfo);
                }
                catch (Exception e)
                {
                    error = e;
                }
            }
            else
            {
                error = InvalidPluginResourceManager;
            }

            return null;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method attempts to retrieve the string value of the resource
        /// with the specified name, first using the specified resource manager
        /// and then using the manifest resources of the specified plugin
        /// assembly.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter context associated with the request, if any.
        /// </param>
        /// <param name="plugin">
        /// The plugin whose assembly manifest resources should be queried.
        /// </param>
        /// <param name="resourceManager">
        /// The resource manager from which the string value should first be
        /// retrieved.
        /// </param>
        /// <param name="name">
        /// The name of the resource to retrieve.
        /// </param>
        /// <param name="cultureInfo">
        /// The culture for which the resource should be retrieved, if any.
        /// </param>
        /// <param name="error">
        /// Upon failure, this contains the list of error messages that describe
        /// the problems encountered.
        /// </param>
        /// <returns>
        /// The string value of the resource, or null if it could not be
        /// retrieved.
        /// </returns>
        public static string GetAnyString(
            Interpreter interpreter,         /* in: NOT USED */
            IPlugin plugin,                  /* in */
            ResourceManager resourceManager, /* in */
            string name,                     /* in */
            CultureInfo cultureInfo,         /* in: OPTIONAL */
            ref Result error                 /* out */
            )
        {
            string value; /* REUSED */
            Result localError; /* REUSED */
            ResultList errors = null;

            localError = null;

            value = GetString(
                resourceManager, name, cultureInfo, ref localError);

            if (value != null)
                return value;

            if (localError != null)
            {
                if (errors == null)
                    errors = new ResultList();

                errors.Add(localError);
            }

            Assembly assembly = null;

            try
            {
                assembly = plugin.Assembly; /* throw */
            }
            catch (Exception e)
            {
                if (errors == null)
                    errors = new ResultList();

                errors.Add(e);
            }

            if (assembly != null)
            {
                localError = null;

                value = AssemblyOps.GetResourceStreamData(
                    assembly, name, null, false, ref localError) as string;

                if (value != null)
                    return value;

                if (localError != null)
                {
                    if (errors == null)
                        errors = new ResultList();

                    errors.Add(localError);
                }
            }

            error = errors;
            return null;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method retrieves the names of all resources available via the
        /// specified resource manager and the resource manager associated with
        /// the specified plugin.
        /// </summary>
        /// <param name="pluginData">
        /// The plugin whose associated resource manager should be queried, if
        /// any.
        /// </param>
        /// <param name="resourceManager">
        /// The resource manager that should be queried, if any.
        /// </param>
        /// <param name="cultureInfo">
        /// The culture for which the resource names should be retrieved, if any.
        /// </param>
        /// <param name="list">
        /// Upon success, the retrieved resource names are added to this list,
        /// which is created if necessary.
        /// </param>
        /// <param name="error">
        /// Upon failure, this contains an error message that describes the
        /// problem encountered.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success;
        /// <see cref="ReturnCode.Error" /> on failure.
        /// </returns>
        public static ReturnCode GetResourceNames(
            IPluginData pluginData,          /* in */
            ResourceManager resourceManager, /* in */
            CultureInfo cultureInfo,         /* in */
            ref StringList list,             /* in, out */
            ref Result error                 /* out */
            )
        {
            ResourceManager pluginResourceManager = null;

            if ((pluginData != null) && !FlagOps.HasFlags(
                    pluginData.Flags, PluginFlags.NoResources, true))
            {
                pluginResourceManager = pluginData.ResourceManager;
            }

            StringList localList = null;

            foreach (ResourceManager localResourceManager in
                new ResourceManager[] {
                    resourceManager, pluginResourceManager
                })
            {
                if (localResourceManager == null)
                    continue;

                if (ResourceOps.GetNames(
                        localResourceManager, cultureInfo, true,
                        ref localList, ref error) != ReturnCode.Ok)
                {
                    return ReturnCode.Error;
                }
            }

            if (localList != null)
            {
                if (list == null)
                    list = new StringList();

                list.AddRange(localList);
            }

            return ReturnCode.Ok;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method attempts to retrieve the string value of the resource
        /// with the specified name, using the resource manager associated with
        /// the specified plugin when available and otherwise using the
        /// specified resource manager.
        /// </summary>
        /// <param name="pluginData">
        /// The plugin whose associated resource manager should be preferred, if
        /// any.
        /// </param>
        /// <param name="resourceManager">
        /// The resource manager to use when the plugin resource manager is not
        /// available.
        /// </param>
        /// <param name="name">
        /// The name of the resource to retrieve.
        /// </param>
        /// <param name="cultureInfo">
        /// The culture for which the resource should be retrieved, if any.
        /// </param>
        /// <param name="error">
        /// Upon failure, this contains an error message that describes the
        /// problem encountered.
        /// </param>
        /// <returns>
        /// The string value of the resource, or null if it could not be
        /// retrieved.
        /// </returns>
        public static string GetString(
            IPluginData pluginData,
            ResourceManager resourceManager,
            string name,
            CultureInfo cultureInfo,
            ref Result error
            )
        {
            if ((pluginData != null) && !FlagOps.HasFlags(
                    pluginData.Flags, PluginFlags.NoResources, true))
            {
                ResourceManager pluginResourceManager = pluginData.ResourceManager;

                if (pluginResourceManager != null)
                {
                    try
                    {
                        return pluginResourceManager.GetString(name, cultureInfo);
                    }
                    catch (Exception e)
                    {
                        error = e;
                    }
                }
                else
                {
                    error = InvalidPluginResourceManager;
                }
            }
            else
            {
                if (resourceManager != null)
                {
                    try
                    {
                        return resourceManager.GetString(name, cultureInfo);
                    }
                    catch (Exception e)
                    {
                        error = e;
                    }
                }
                else
                {
                    error = InvalidInterpreterResourceManager;
                }
            }

            return null;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method creates a new resource manager for the assembly
        /// identified by the specified assembly name, loading the assembly if
        /// necessary.
        /// </summary>
        /// <param name="assemblyName">
        /// The name of the assembly for which a resource manager should be
        /// created.
        /// </param>
        /// <returns>
        /// The newly created resource manager, or null if it could not be
        /// created.
        /// </returns>
        public static ResourceManager NewResourceManager(
            AssemblyName assemblyName
            )
        {
            if (assemblyName != null)
            {
                try
                {
                    return NewResourceManager(assemblyName,
                        Assembly.Load(assemblyName));
                }
                catch (Exception e)
                {
                    DebugOps.Complain(ReturnCode.Error, e);
                }
            }

            return null;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method creates a new resource manager for the specified
        /// assembly.
        /// </summary>
        /// <param name="assembly">
        /// The assembly for which a resource manager should be created.
        /// </param>
        /// <returns>
        /// The newly created resource manager, or null if it could not be
        /// created.
        /// </returns>
        public static ResourceManager NewResourceManager(
            Assembly assembly
            )
        {
            if (assembly != null)
            {
                try
                {
                    return NewResourceManager(assembly.GetName(), assembly);
                }
                catch (Exception e)
                {
                    DebugOps.Complain(ReturnCode.Error, e);
                }

            }

            return null;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method creates a new resource manager using the base name from
        /// the specified assembly name and the specified assembly.
        /// </summary>
        /// <param name="assemblyName">
        /// The assembly name whose base name should be used by the resource
        /// manager.
        /// </param>
        /// <param name="assembly">
        /// The assembly from which the resources should be loaded.
        /// </param>
        /// <returns>
        /// The newly created resource manager, or null if it could not be
        /// created.
        /// </returns>
        public static ResourceManager NewResourceManager(
            AssemblyName assemblyName,
            Assembly assembly
            )
        {
            if ((assemblyName != null) && (assembly != null))
            {
                try
                {
                    return new ResourceManager(assemblyName.Name, assembly);
                }
                catch (Exception e)
                {
                    DebugOps.Complain(ReturnCode.Error, e);
                }
            }

            return null;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method converts the specified string representation of an
        /// unsigned wide integer into the corresponding public key token byte
        /// array.
        /// </summary>
        /// <param name="value">
        /// The string representation of the public key token to convert.
        /// </param>
        /// <param name="cultureInfo">
        /// The culture to use when parsing the value, if any.
        /// </param>
        /// <param name="publicKeyToken">
        /// Upon success, this contains the public key token bytes.
        /// </param>
        /// <param name="error">
        /// Upon failure, this contains an error message that describes the
        /// problem encountered.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success;
        /// <see cref="ReturnCode.Error" /> on failure.
        /// </returns>
        public static ReturnCode GetPublicKeyToken(
            string value,
            CultureInfo cultureInfo,
            ref byte[] publicKeyToken,
            ref Result error
            )
        {
            ulong ulongValue = 0;

            if (Value.GetUnsignedWideInteger2(
                    value, ValueFlags.AnyWideInteger |
                    ValueFlags.Unsigned, cultureInfo,
                    ref ulongValue, ref error) != ReturnCode.Ok)
            {
                return ReturnCode.Error;
            }

            byte[] bytes = BitConverter.GetBytes(ulongValue);

            if (BitConverter.IsLittleEndian)
                Array.Reverse(bytes);

            publicKeyToken = bytes;
            return ReturnCode.Ok;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method checks whether the assembly contained in the specified
        /// file has the specified public key token.
        /// </summary>
        /// <param name="fileName">
        /// The file name of the assembly whose public key token should be
        /// checked.
        /// </param>
        /// <param name="publicKeyToken">
        /// The public key token that the assembly is expected to have.
        /// </param>
        /// <returns>
        /// True if the assembly has the specified public key token; otherwise,
        /// false.
        /// </returns>
        public static bool CheckPublicKeyToken(
            string fileName,
            byte[] publicKeyToken
            )
        {
            Result error = null;

            return CheckPublicKeyToken(
                fileName, publicKeyToken, ref error);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method checks whether the assembly contained in the specified
        /// file has the specified public key token, returning detailed error
        /// information on failure.
        /// </summary>
        /// <param name="fileName">
        /// The file name of the assembly whose public key token should be
        /// checked.
        /// </param>
        /// <param name="publicKeyToken">
        /// The public key token that the assembly is expected to have.
        /// </param>
        /// <param name="error">
        /// Upon failure, this contains an error message that describes the
        /// problem encountered.
        /// </param>
        /// <returns>
        /// True if the assembly has the specified public key token; otherwise,
        /// false.
        /// </returns>
        public static bool CheckPublicKeyToken(
            string fileName,
            byte[] publicKeyToken,
            ref Result error
            )
        {
            if (String.IsNullOrEmpty(fileName))
            {
                error = "invalid assembly file name";
                return false;
            }

            if (publicKeyToken == null)
            {
                error = "invalid public key token";
                return false;
            }

            try
            {
                AssemblyName assemblyName = AssemblyName.GetAssemblyName(
                    fileName); /* throw */

                if (assemblyName != null)
                {
                    byte[] assemblyNamePublicKeyToken =
                        assemblyName.GetPublicKeyToken();

                    if ((assemblyNamePublicKeyToken != null) &&
                        ArrayOps.Equals(
                            publicKeyToken, assemblyNamePublicKeyToken))
                    {
                        return true;
                    }
                    else
                    {
                        error = String.Format(
                            "public key token mismatch: {0} versus {1}",
                            FormatOps.PublicKeyToken(publicKeyToken),
                            FormatOps.PublicKeyToken(
                                assemblyNamePublicKeyToken));
                    }
                }
                else
                {
                    error = "invalid assembly name";
                }
            }
            catch (Exception e)
            {
                error = e;
            }

            return false;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method loads a plugin from the assembly bytes obtained from the
        /// specified resource via the interpreter host.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter context into which the plugin should be loaded.
        /// </param>
        /// <param name="ruleSet">
        /// The rule set used to constrain the loading of the plugin, if any.
        /// </param>
        /// <param name="resourceName">
        /// The name of the resource from which the assembly bytes should be
        /// obtained.
        /// </param>
        /// <param name="evidence">
        /// The evidence used when loading the plugin assembly, if any.
        /// </param>
        /// <param name="typeName">
        /// The name of the plugin type to load, if any.
        /// </param>
        /// <param name="clientData">
        /// The client-specific data to associate with the plugin, if any.
        /// </param>
        /// <param name="flags">
        /// The flags used to control how the plugin is loaded.
        /// </param>
        /// <param name="plugin">
        /// Upon success, this contains the loaded plugin.
        /// </param>
        /// <param name="result">
        /// Upon success, this contains the result of loading the plugin; upon
        /// failure, this contains an error message that describes the problem
        /// encountered.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success;
        /// <see cref="ReturnCode.Error" /> on failure.
        /// </returns>
        public static ReturnCode LoadPlugin(
            Interpreter interpreter, /* in */
            IRuleSet ruleSet,        /* in */
            string resourceName,     /* in */
#if CAS_POLICY
            Evidence evidence,       /* in */
#endif
            string typeName,         /* in */
            IClientData clientData,  /* in */
            PluginFlags flags,       /* in */
            ref IPlugin plugin,      /* out */
            ref Result result        /* out */
            )
        {
            if (interpreter == null)
            {
                result = "invalid interpreter";
                return ReturnCode.Error;
            }

            IFileSystemHost fileSystemHost = interpreter.InternalHost;

            if (fileSystemHost == null)
            {
                result = "interpreter host not available";
                return ReturnCode.Error;
            }

            ScriptFlags scriptFlags; /* REUSED */
            IClientData localClientData; /* REUSED */
            Result localResult; /* REUSED */

            ///////////////////////////////////////////////////////////////////
            //
            // NOTE: The assembly bytes are always required.
            //
            scriptFlags = ScriptFlags.PluginBinaryOnly;
            localClientData = null;
            localResult = null;

            if (fileSystemHost.GetData( /* EXEMPT */
                    resourceName, HostOps.CombineDataFlags(
                        interpreter, resourceName, scriptFlags,
                        DataFlags.Plugin),
                    ref scriptFlags, ref localClientData,
                    ref localResult) != ReturnCode.Ok)
            {
                result = localResult;
                return ReturnCode.Error;
            }

            if (localResult == null)
            {
                result = "invalid assembly bytes";
                return ReturnCode.Error;
            }

            byte[] assemblyBytes = localResult.Value as byte[];

            ///////////////////////////////////////////////////////////////////
            //
            // NOTE: The symbol bytes are always optional.
            //
            scriptFlags = ScriptFlags.PluginBinaryOnly;
            localClientData = null;
            localResult = null;

            /* IGNORED */
            fileSystemHost.GetData( /* EXEMPT */
                String.Format(SymbolsFormat, resourceName),
                HostOps.CombineDataFlags(
                    interpreter, resourceName, scriptFlags,
                    DataFlags.Plugin),
                ref scriptFlags, ref localClientData,
                ref localResult);

            byte[] symbolBytes = (localResult != null) ?
                localResult.Value as byte[] : null;

            ///////////////////////////////////////////////////////////////////

            return interpreter.LoadPlugin(
                ruleSet, assemblyBytes, symbolBytes,
#if CAS_POLICY
                evidence,
#endif
                typeName, clientData, flags, ref plugin, ref result);
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Stream Support Methods
        /// <summary>
        /// This method attempts to open a stream for the specified path by
        /// querying the manifest resources of the entry and/or executing
        /// assemblies, as indicated by the specified host stream flags.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter context to use when resolving the path, if any.
        /// </param>
        /// <param name="path">
        /// The path of the stream to open.
        /// </param>
        /// <param name="hostStreamFlags">
        /// Flags that control which assemblies are queried and how the stream is
        /// located.  Upon return, the flags indicating how the stream was found
        /// are updated.
        /// </param>
        /// <param name="fullPath">
        /// Upon success, this contains the fully resolved path of the opened
        /// stream.
        /// </param>
        /// <param name="stream">
        /// Upon success, this contains the opened stream.
        /// </param>
        /// <param name="error">
        /// Upon failure, this contains an error message that describes the
        /// problem encountered.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success;
        /// <see cref="ReturnCode.Error" /> on failure.
        /// </returns>
        private static ReturnCode NewStreamFromAssembly(
            Interpreter interpreter,
            string path,
            ref HostStreamFlags hostStreamFlags,
            ref string fullPath,
            ref Stream stream,
            ref Result error
            )
        {
            if (String.IsNullOrEmpty(path))
            {
                error = "unrecognized path";
                return ReturnCode.Error;
            }

            Dictionary<HostStreamFlags, Assembly> assemblies =
                new Dictionary<HostStreamFlags, Assembly>();

            if (FlagOps.HasFlags(hostStreamFlags,
                    HostStreamFlags.EntryAssembly, true))
            {
                assemblies.Add(HostStreamFlags.EntryAssembly,
                    GlobalState.GetEntryAssembly());
            }

            if (FlagOps.HasFlags(hostStreamFlags,
                    HostStreamFlags.ExecutingAssembly, true))
            {
                assemblies.Add(HostStreamFlags.ExecutingAssembly,
                    GlobalState.GetAssembly());
            }

            hostStreamFlags &= ~HostStreamFlags.AssemblyMask;

            bool resolve = FlagOps.HasFlags(
                hostStreamFlags, HostStreamFlags.ResolveFullPath, true);

            foreach (KeyValuePair<HostStreamFlags, Assembly> pair
                    in assemblies)
            {
                Assembly assembly = pair.Value;

                if (assembly == null)
                    continue;

                string localFullPath = resolve ?
                    PathOps.ResolveFullPath(interpreter, path) : path;

                Stream localStream = AssemblyOps.GetResourceStream(
                    assembly, localFullPath);

                if (localStream != null)
                {
                    hostStreamFlags |= pair.Key;

                    if (FlagOps.HasFlags(hostStreamFlags,
                            HostStreamFlags.AssemblyQualified, true))
                    {
                        fullPath = String.Format(
                            "{0}{1}{2}", assembly.Location,
                            PathOps.GetFirstDirectorySeparator(localFullPath),
                            PathOps.MakeRelativePath(localFullPath, true));
                    }
                    else
                    {
                        fullPath = localFullPath;
                    }

                    stream = localStream;

                    return ReturnCode.Ok;
                }
            }

            error = String.Format(
                "stream {0} not available via specified assemblies",
                FormatOps.WrapOrNull(path));

            return ReturnCode.Error;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method attempts to open a stream for the specified path by
        /// querying the plugins currently loaded into the specified
        /// interpreter.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter context whose loaded plugins should be queried.
        /// </param>
        /// <param name="path">
        /// The path of the stream to open.
        /// </param>
        /// <param name="hostStreamFlags">
        /// Flags that control how the stream is located and opened.  Upon
        /// return, the flags indicating how the stream was found are updated.
        /// </param>
        /// <param name="fullPath">
        /// Upon success, this contains the fully resolved path of the opened
        /// stream.
        /// </param>
        /// <param name="stream">
        /// Upon success, this contains the opened stream.
        /// </param>
        /// <param name="error">
        /// Upon failure, this contains an error message that describes the
        /// problem encountered.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success;
        /// <see cref="ReturnCode.Error" /> on failure.
        /// </returns>
        public static ReturnCode NewStreamFromPlugins(
            Interpreter interpreter,
            string path,
            ref HostStreamFlags hostStreamFlags,
            ref string fullPath,
            ref Stream stream,
            ref Result error
            )
        {
            if (interpreter == null)
            {
                error = "invalid interpreter";
                return ReturnCode.Error;
            }

            if (String.IsNullOrEmpty(path))
            {
                error = "unrecognized path";
                return ReturnCode.Error;
            }

            PluginWrapperDictionary plugins = interpreter.CopyPlugins();

            if (plugins == null)
            {
                error = "plugins not available";
                return ReturnCode.Error;
            }

            hostStreamFlags &= ~HostStreamFlags.FoundViaPlugin;

            bool resolve = FlagOps.HasFlags(
                hostStreamFlags, HostStreamFlags.ResolveFullPath, true);

            foreach (PluginPair pair in plugins)
            {
                IPlugin plugin = pair.Value;

                if (plugin == null)
                    continue;

                string localFullPath = resolve ?
                    PathOps.ResolveFullPath(interpreter, path) : path;

                Stream localStream = plugin.GetStream(interpreter,
                    localFullPath, interpreter.InternalCultureInfo,
                    ref error);

                if (localStream != null)
                {
                    hostStreamFlags |= HostStreamFlags.FoundViaPlugin;
                    fullPath = localFullPath;
                    stream = localStream;

                    return ReturnCode.Ok;
                }
            }

            error = String.Format(
                "stream {0} not available via loaded plugins",
                FormatOps.WrapOrNull(path));

            return ReturnCode.Error;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method attempts to open a stream for the specified path using
        /// the default flags, share mode, buffer size, and options.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter context to use when resolving the path and locating
        /// the stream.
        /// </param>
        /// <param name="path">
        /// The path of the stream to open.
        /// </param>
        /// <param name="mode">
        /// The <see cref="FileMode" /> value that controls how the stream is
        /// opened or created.
        /// </param>
        /// <param name="access">
        /// The <see cref="FileAccess" /> value that controls the access
        /// permitted on the stream.
        /// </param>
        /// <param name="stream">
        /// Upon success, this contains the opened stream.
        /// </param>
        /// <param name="error">
        /// Upon failure, this contains an error message that describes the
        /// problem encountered.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success;
        /// <see cref="ReturnCode.Error" /> on failure.
        /// </returns>
        public static ReturnCode NewStream(
            Interpreter interpreter,
            string path,
            FileMode mode,
            FileAccess access,
            ref Stream stream,
            ref Result error
            )
        {
            HostStreamFlags hostStreamFlags = HostStreamFlags.None;
            string fullPath = null;

            return NewStream(
                interpreter, path, mode, access, FileShare.Read,
                ChannelOps.DefaultBufferSize, FileOptions.None,
                ref hostStreamFlags, ref fullPath, ref stream,
                ref error);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method attempts to open a stream for the specified path using
        /// the specified flags and the default share mode, buffer size, and
        /// options.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter context to use when resolving the path and locating
        /// the stream.
        /// </param>
        /// <param name="path">
        /// The path of the stream to open.
        /// </param>
        /// <param name="mode">
        /// The <see cref="FileMode" /> value that controls how the stream is
        /// opened or created.
        /// </param>
        /// <param name="access">
        /// The <see cref="FileAccess" /> value that controls the access
        /// permitted on the stream.
        /// </param>
        /// <param name="hostStreamFlags">
        /// Flags that control how the stream is located and opened.  Upon
        /// return, the flags indicating how the stream was found are updated.
        /// </param>
        /// <param name="fullPath">
        /// Upon success, this contains the fully resolved path of the opened
        /// stream.
        /// </param>
        /// <param name="stream">
        /// Upon success, this contains the opened stream.
        /// </param>
        /// <param name="error">
        /// Upon failure, this contains an error message that describes the
        /// problem encountered.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success;
        /// <see cref="ReturnCode.Error" /> on failure.
        /// </returns>
        public static ReturnCode NewStream(
            Interpreter interpreter,
            string path,
            FileMode mode,
            FileAccess access,
            ref HostStreamFlags hostStreamFlags,
            ref string fullPath,
            ref Stream stream,
            ref Result error
            )
        {
            return NewStream(
                interpreter, path, mode, access, FileShare.Read,
                ChannelOps.DefaultBufferSize, FileOptions.None,
                ref hostStreamFlags, ref fullPath, ref stream,
                ref error);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method attempts to open a stream for the specified path,
        /// optionally searching loaded plugins and assemblies in addition to
        /// the file system, according to the specified flags.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter context to use when resolving the path and locating
        /// the stream.  This value may be null.
        /// </param>
        /// <param name="path">
        /// The path of the stream to open.
        /// </param>
        /// <param name="mode">
        /// The <see cref="FileMode" /> value that controls how the stream is
        /// opened or created.
        /// </param>
        /// <param name="access">
        /// The <see cref="FileAccess" /> value that controls the access
        /// permitted on the stream.
        /// </param>
        /// <param name="share">
        /// The <see cref="FileShare" /> value that controls the access other
        /// streams may have to the same file.
        /// </param>
        /// <param name="bufferSize">
        /// The size, in bytes, of the buffer to use for the stream.
        /// </param>
        /// <param name="options">
        /// The <see cref="FileOptions" /> value that specifies additional
        /// options for creating the stream.
        /// </param>
        /// <param name="hostStreamFlags">
        /// Flags that control how the stream is located and opened, including
        /// whether plugins, assemblies, and the file system are searched and
        /// in what order.  Upon return, the flags indicating how the stream
        /// was found are updated.
        /// </param>
        /// <param name="fullPath">
        /// Upon success, this contains the fully resolved path of the opened
        /// stream.
        /// </param>
        /// <param name="stream">
        /// Upon success, this contains the opened stream.
        /// </param>
        /// <param name="error">
        /// Upon failure, this contains an error message that describes the
        /// problem encountered.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success;
        /// <see cref="ReturnCode.Error" /> on failure.
        /// </returns>
        public static ReturnCode NewStream(
            Interpreter interpreter,             /* in, OPTIONAL: May be null. */
            string path,                         /* in */
            FileMode mode,                       /* in */
            FileAccess access,                   /* in */
            FileShare share,                     /* in */
            int bufferSize,                      /* in */
            FileOptions options,                 /* in */
            ref HostStreamFlags hostStreamFlags, /* in, out */
            ref string fullPath,                 /* out */
            ref Stream stream,                   /* out */
            ref Result error                     /* out */
            )
        {
            hostStreamFlags &= ~HostStreamFlags.FoundMask;

            if (String.IsNullOrEmpty(path))
            {
                error = "unrecognized path";
                return ReturnCode.Error;
            }

            if (PathOps.IsRemoteUri(path))
            {
                error = String.Format(
                    "cannot open stream for remote uri {0}",
                    FormatOps.WrapOrNull(path));

                return ReturnCode.Error;
            }

            ReturnCode code;
            Result localError = null;
            ResultList errors = null;

            ///////////////////////////////////////////////////////////////////

            bool usePlugins = FlagOps.HasFlags(
                hostStreamFlags, HostStreamFlags.LoadedPlugins, true);

            bool useAssembly = FlagOps.HasFlags(
                hostStreamFlags, HostStreamFlags.AssemblyMask, false);

            bool resolve = FlagOps.HasFlags(
                hostStreamFlags, HostStreamFlags.ResolveFullPath, true);

            bool preferFileSystem = FlagOps.HasFlags(
                hostStreamFlags, HostStreamFlags.PreferFileSystem, true);

            bool skipFileSystem = FlagOps.HasFlags(
                hostStreamFlags, HostStreamFlags.SkipFileSystem, true);

            ///////////////////////////////////////////////////////////////////

            if (usePlugins && !preferFileSystem)
            {
                code = NewStreamFromPlugins(
                    interpreter, path, ref hostStreamFlags, ref fullPath,
                    ref stream, ref localError);

                if (code == ReturnCode.Ok)
                {
                    hostStreamFlags |= HostStreamFlags.FoundViaPlugin;
                    return ReturnCode.Ok;
                }
                else if (localError != null)
                {
                    if (errors == null)
                        errors = new ResultList();

                    errors.Add(localError);
                }
            }

            ///////////////////////////////////////////////////////////////////

            if (useAssembly && !preferFileSystem)
            {
                code = NewStreamFromAssembly(
                    interpreter, path, ref hostStreamFlags, ref fullPath,
                    ref stream, ref localError);

                if (code == ReturnCode.Ok)
                {
                    hostStreamFlags |= HostStreamFlags.FoundViaAssembly;
                    return ReturnCode.Ok;
                }
                else if (localError != null)
                {
                    if (errors == null)
                        errors = new ResultList();

                    errors.Add(localError);
                }
            }

            ///////////////////////////////////////////////////////////////////

            if (!skipFileSystem)
            {
                string localFullPath = resolve ?
                    PathOps.ResolveFullPath(interpreter, path) : path;

                if (!String.IsNullOrEmpty(localFullPath))
                {
                    try
                    {
                        stream = new FileStream(
                            localFullPath, mode, access, share,
                            bufferSize, options); /* throw */ /* EXEMPT */

                        hostStreamFlags |= HostStreamFlags.FoundViaFileSystem;
                        fullPath = localFullPath;

                        return ReturnCode.Ok;
                    }
                    catch (Exception e)
                    {
                        if (errors == null)
                            errors = new ResultList();

                        errors.Add(e);
                    }
                }
                else
                {
                    if (errors == null)
                        errors = new ResultList();

                    errors.Add(String.Format(
                        "could not resolve local path {0}",
                        FormatOps.WrapOrNull(path)));
                }
            }

            ///////////////////////////////////////////////////////////////////

            if (usePlugins && preferFileSystem)
            {
                code = NewStreamFromPlugins(
                    interpreter, path, ref hostStreamFlags, ref fullPath,
                    ref stream, ref localError);

                if (code == ReturnCode.Ok)
                {
                    hostStreamFlags |= HostStreamFlags.FoundViaPlugin;
                    return ReturnCode.Ok;
                }
                else if (localError != null)
                {
                    if (errors == null)
                        errors = new ResultList();

                    errors.Add(localError);
                }
            }

            ///////////////////////////////////////////////////////////////////

            if (useAssembly && preferFileSystem)
            {
                code = NewStreamFromAssembly(
                    interpreter, path, ref hostStreamFlags, ref fullPath,
                    ref stream, ref localError);

                if (code == ReturnCode.Ok)
                {
                    hostStreamFlags |= HostStreamFlags.FoundViaAssembly;
                    return ReturnCode.Ok;
                }
                else if (localError != null)
                {
                    if (errors == null)
                        errors = new ResultList();

                    errors.Add(localError);
                }
            }

            ///////////////////////////////////////////////////////////////////

            if (!usePlugins && !useAssembly && skipFileSystem)
            {
                if (errors == null)
                    errors = new ResultList();

                errors.Add(String.Format(
                    "cannot open stream for {0}, no search performed",
                    FormatOps.WrapOrNull(path)));
            }

            ///////////////////////////////////////////////////////////////////

            error = errors;
            return ReturnCode.Error;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method reads the entire contents of the specified stream as an
        /// array of bytes.
        /// </summary>
        /// <param name="stream">
        /// The stream to read from.
        /// </param>
        /// <param name="bytes">
        /// Upon success, this contains the bytes read from the stream.
        /// </param>
        /// <param name="error">
        /// Upon failure, this contains an error message that describes the
        /// problem encountered.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success;
        /// <see cref="ReturnCode.Error" /> on failure.
        /// </returns>
        public static ReturnCode ReadStream(
            Stream stream,
            ref byte[] bytes,
            ref Result error
            )
        {
            try
            {
                using (BinaryReader binaryReader = new BinaryReader(stream))
                {
                    int length = (int)stream.Length; /* throw */
                    bytes = binaryReader.ReadBytes(length); /* throw */
                }

                return ReturnCode.Ok;
            }
            catch (Exception e)
            {
                error = e;
            }

            return ReturnCode.Error;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method reads the entire contents of the specified stream as a
        /// string, using the default encoding detection of the underlying
        /// stream reader.
        /// </summary>
        /// <param name="stream">
        /// The stream to read from.
        /// </param>
        /// <param name="text">
        /// Upon success, this contains the text read from the stream.
        /// </param>
        /// <param name="error">
        /// Upon failure, this contains an error message that describes the
        /// problem encountered.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success;
        /// <see cref="ReturnCode.Error" /> on failure.
        /// </returns>
        public static ReturnCode ReadStream(
            Stream stream,
            ref string text,
            ref Result error
            )
        {
            try
            {
                using (StreamReader streamReader = new StreamReader(stream))
                {
                    text = streamReader.ReadToEnd(); /* throw */
                }

                return ReturnCode.Ok;
            }
            catch (Exception e)
            {
                error = e;
            }

            return ReturnCode.Error;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method reads the entire contents of the specified stream as a
        /// string, using the specified character encoding.
        /// </summary>
        /// <param name="stream">
        /// The stream to read from.
        /// </param>
        /// <param name="encoding">
        /// The character encoding to use when reading the stream.
        /// </param>
        /// <param name="text">
        /// Upon success, this contains the text read from the stream.
        /// </param>
        /// <param name="error">
        /// Upon failure, this contains an error message that describes the
        /// problem encountered.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success;
        /// <see cref="ReturnCode.Error" /> on failure.
        /// </returns>
        public static ReturnCode ReadStream(
            Stream stream,
            Encoding encoding,
            ref string text,
            ref Result error
            )
        {
            try
            {
                using (StreamReader streamReader = new StreamReader(
                        stream, encoding))
                {
                    text = streamReader.ReadToEnd(); /* throw */
                }

                return ReturnCode.Ok;
            }
            catch (Exception e)
            {
                error = e;
            }

            return ReturnCode.Error;
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Version Information Methods
        /// <summary>
        /// This method determines whether the embedded source license appears
        /// to be genuine by comparing its stored hash against a freshly
        /// computed hash of its summary and text.
        /// </summary>
        /// <returns>
        /// True if the source license is genuine; otherwise, false.
        /// </returns>
        private static bool IsGenuine()
        {
            return ArrayOps.Equals(
                SourceLicense.Hash, HashOps.HashString(null, (string)null,
                StringOps.ForceCarriageReturns(SourceLicense.Summary +
                SourceLicense.Text)));
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method returns the value used to indicate that the runtime is
        /// genuine, if and only if the embedded source license is genuine.
        /// </summary>
        /// <returns>
        /// The genuine indicator value if the source license is genuine;
        /// otherwise, null.
        /// </returns>
        private static string GetGenuine()
        {
            return IsGenuine() ? Vars.Version.GenuineValue : null;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method returns the value used to indicate that the specified
        /// file is trusted, if and only if it carries a valid certificate
        /// subject.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter context to use when checking the certificate.
        /// </param>
        /// <param name="fileName">
        /// The name of the file whose trust status should be checked.
        /// </param>
        /// <returns>
        /// The trusted indicator value if the file is trusted; otherwise,
        /// null.
        /// </returns>
        public static string GetFileTrusted(
            Interpreter interpreter,
            string fileName
            )
        {
            if (GetCertificateSubject(
                    interpreter, fileName, null, ShouldCheckFileTrusted(),
                    true, true) != null)
            {
                return Vars.Version.TrustedValue;
            }

            return null;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method determines whether this build is an official build, as
        /// determined by the <c>OFFICIAL</c> compile-time option.
        /// </summary>
        /// <returns>
        /// True if this is an official build; otherwise, false.
        /// </returns>
        public static bool IsOfficial()
        {
#if OFFICIAL
            return true;
#else
            return false;
#endif
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method determines whether this build is an official binary
        /// build, as determined by the <c>OFFICIAL_BINARY</c> compile-time
        /// option.
        /// </summary>
        /// <returns>
        /// True if this is an official binary build; otherwise, false.
        /// </returns>
        public static bool IsOfficialBinary()
        {
#if OFFICIAL_BINARY
            return true;
#else
            return false;
#endif
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method determines whether this build is a stable build, as
        /// determined by the <c>STABLE</c> compile-time option.
        /// </summary>
        /// <returns>
        /// True if this is a stable build; otherwise, false.
        /// </returns>
        public static bool IsStable()
        {
#if STABLE
            return true;
#else
            return false;
#endif
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method builds the path and query string portion of the URI
        /// used to check for updates, based on the specified version, stability
        /// preference, and suffix.
        /// </summary>
        /// <param name="version">
        /// The version string to include in the update path and query, or null
        /// if no version should be included.
        /// </param>
        /// <param name="stable">
        /// Non-null to select the stable or unstable update format based on its
        /// value; null to use the default update format and include the calling
        /// method name.
        /// </param>
        /// <param name="suffix">
        /// The suffix string to include in the update path and query, or null
        /// if no suffix should be included.
        /// </param>
        /// <returns>
        /// The formatted path and query string to use when checking for
        /// updates.
        /// </returns>
        [MethodImpl(MethodImplOptions.NoInlining)]
        public static string GetUpdatePathAndQuery(
            string version,
            bool? stable,
            string suffix
            )
        {
            string format;

            if (stable != null)
            {
                format = (bool)stable ?
                    Vars.Platform.UpdateStablePathAndQueryFormat :
                    Vars.Platform.UpdateUnstablePathAndQueryFormat;

                if ((version != null) || (suffix != null))
                    return String.Format(format, version, suffix);
                else
                    return format;
            }
            else
            {
                format = Vars.Platform.UpdatePathAndQueryFormat;

                bool isThisAssembly; /* NOT USED */
                string typeName; /* NOT USED */
                string methodName;

                DebugOps.GetMethodName(
                    1, null, false, true, null, false,
                    out isThisAssembly, out typeName,
                    out methodName);

                return String.Format("{1}{0}", String.Format(
                    format, version, suffix), methodName);
            }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method returns the descriptive text or base suffix associated
        /// with the assembly containing the Eagle core library.
        /// </summary>
        /// <returns>
        /// The assembly text if available; otherwise, the base suffix of the
        /// assembly.
        /// </returns>
        private static string GetAssemblyTextOrSuffix()
        {
            return GetAssemblyTextOrSuffix(GlobalState.GetAssembly());
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method returns the descriptive text or base suffix associated
        /// with the specified assembly.
        /// </summary>
        /// <param name="assembly">
        /// The assembly whose text or base suffix should be returned.
        /// </param>
        /// <returns>
        /// The assembly text if available; otherwise, the base suffix of the
        /// assembly.
        /// </returns>
        public static string GetAssemblyTextOrSuffix( /* e.g. "NetFx20", etc */
            Assembly assembly
            )
        {
            string result = SharedAttributeOps.GetAssemblyText(assembly);

            if (result == null)
                result = PathOps.GetBaseSuffix(assembly);

            return result;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method formats the specified value, appending the assembly text
        /// or suffix (preceded by an underscore) when one is available.
        /// </summary>
        /// <param name="value">
        /// The composite format string into which the underscore separator and
        /// lower-cased assembly text or suffix are substituted.
        /// </param>
        /// <returns>
        /// The formatted value; the unchanged value if it is null or empty; or
        /// null if an exception is encountered during formatting.
        /// </returns>
        public static string MaybeAppendTextOrSuffix(
            string value
            )
        {
            if (String.IsNullOrEmpty(value))
                return value;

            string text = GetAssemblyTextOrSuffix();

            try
            {
                if (String.IsNullOrEmpty(text))
                {
                    return String.Format(
                        value, null, null); /* throw */
                }

                text = text.ToLowerInvariant();

                return String.Format(
                    value, Characters.Underscore,
                    text); /* throw */
            }
            catch (Exception e)
            {
                TraceOps.DebugTrace(
                    e, typeof(RuntimeOps).Name,
                    TracePriority.StringError);

                return null;
            }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method returns a string that describes the Tcl package name and
        /// patch level supported by this runtime.
        /// </summary>
        /// <returns>
        /// A string containing the Tcl package name and patch level.
        /// </returns>
        private static string GetTclVersionString()
        {
            return String.Format("{0} {1}", TclVars.Package.Name,
                TclVars.Package.PatchLevelValue);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method appends the version information elements describing the
        /// Eagle core library to the specified list, optionally redacting
        /// sensitive elements when running in safe mode.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter context to use when determining trust and other
        /// version information.  This value may be null.
        /// </param>
        /// <param name="assembly">
        /// The assembly whose version information should be added.
        /// </param>
        /// <param name="fileName">
        /// The name of the file whose trust status should be included.
        /// </param>
        /// <param name="safe">
        /// Non-zero to redact potentially sensitive version information,
        /// suitable for use within a safe interpreter.
        /// </param>
        /// <param name="allowNull">
        /// Non-zero to add null elements for information that is not available;
        /// otherwise, such elements are omitted.
        /// </param>
        /// <param name="list">
        /// The list to which the version information elements are added.  If
        /// null, a new list is created.
        /// </param>
        private static void AddCoreVersionInformation(
            Interpreter interpreter, /* in: OPTIONAL */
            Assembly assembly,       /* in */
            string fileName,         /* in */
            bool safe,               /* in */
            bool allowNull,          /* in */
            ref StringList list      /* in, out */
            )
        {
            if (list == null)
                list = new StringList();

            list.MaybeAdd(
                GlobalState.GetPackageName(), allowNull);

            list.MaybeAdd(
                GlobalState.GetAssemblyVersionString(), allowNull);

            list.MaybeAdd(
                GetFileTrusted(interpreter, fileName), allowNull);

            list.MaybeAdd(GetGenuine(), allowNull);

            ///////////////////////////////////////////////////////////////////

            if (safe)
            {
                if (allowNull)
                {
                    list.Add((string)null);
                    list.Add((string)null);
                    list.Add((string)null);
                    list.Add((string)null);
                    list.Add((string)null);
                }
            }
            else
            {
                list.MaybeAdd(Vars.Version.OfficialValue, allowNull);
                list.MaybeAdd(Vars.Version.StableValue, allowNull);

                list.MaybeAdd(
                    SharedAttributeOps.GetAssemblyTag(assembly),
                    allowNull);

                list.MaybeAdd(
                    SharedAttributeOps.GetAssemblyRelease(assembly),
                    allowNull);

                list.MaybeAdd(
                    GetAssemblyTextOrSuffix(assembly), allowNull);
            }

            ///////////////////////////////////////////////////////////////////

            list.MaybeAdd(
                AttributeOps.GetAssemblyConfiguration(assembly),
                allowNull);

            list.MaybeAdd(GetTclVersionString(), allowNull);

            ///////////////////////////////////////////////////////////////////

            if (safe)
            {
                if (allowNull)
                {
                    list.Add((string)null);
                    list.Add((string)null);
                    list.Add((string)null);
                    list.Add((string)null);
                    list.Add((string)null);
                    list.Add((string)null);
                }
            }
            else
            {
                list.MaybeAdd(FormatOps.Iso8601DateTime(
                    SharedAttributeOps.GetAssemblyDateTime(assembly),
                    true), allowNull);

                list.MaybeAdd(
                    SharedAttributeOps.GetAssemblySourceId(assembly),
                    allowNull);

                list.MaybeAdd(
                    SharedAttributeOps.GetAssemblySourceTimeStamp(
                    assembly), allowNull);

                list.MaybeAdd(
                    CommonOps.Runtime.GetRuntimeNameAndVersion(),
                    allowNull);

                list.MaybeAdd(
                    PlatformOps.GetOperatingSystemName(), allowNull);

                list.MaybeAdd(PlatformOps.GetMachineName(), allowNull);
            }

            ///////////////////////////////////////////////////////////////////

#if ENTERPRISE_LOCKDOWN || MAYBE_ENTERPRISE_LOCKDOWN
            if (Interpreter.IsEnterpriseLockdownEnabled())
                list.MaybeAdd("ENTERPRISE_LOCKDOWN", allowNull);
#endif
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method queries the status of the specified plugin and, when
        /// successful, appends the resulting status information to the specified
        /// list.  If the plugin is null or its status cannot be obtained, no
        /// changes are made to the list.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter context to use when querying the plugin status.
        /// </param>
        /// <param name="plugin">
        /// The plugin whose status should be queried and added.
        /// </param>
        /// <param name="list">
        /// The list to which the plugin status information is added.
        /// </param>
        private static void MaybeAddPluginStatus(
            Interpreter interpreter, /* in */
            IPlugin plugin,          /* in */
            ref StringList list      /* in, out */
            )
        {
            if (plugin == null)
                return;

            ReturnCode statusCode;
            Result statusResult = null;

            statusCode = plugin.Status(interpreter, ref statusResult);

            if (statusCode != ReturnCode.Ok)
            {
                TraceOps.DebugTrace(String.Format(
                    "MaybeAddPluginStatus: plugin = {0}, " +
                    "statusCode = {1}, statusResult = {2}",
                    FormatOps.WrapOrNull(plugin), statusCode,
                    FormatOps.WrapOrNull(statusResult)),
                    typeof(RuntimeOps).Name,
                    TracePriority.PluginError);

                return;
            }

            string statusName = EntityOps.GetSimpleAssemblyNameNoThrow(
                plugin);

            if (String.IsNullOrEmpty(statusName))
                return;

            string statusString = statusResult;

            if (String.IsNullOrEmpty(statusString))
                return;

            if (list == null)
                list = new StringList();

            list.Add(String.Format("{0}: {1}", statusName, statusString));
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method appends status information for all plugins loaded into
        /// the specified interpreter to the specified list.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter whose loaded plugins should be queried for status
        /// information.
        /// </param>
        /// <param name="list">
        /// Upon return, the list to which the combined plugin status
        /// information is appended.  If necessary, a new list is created.
        /// </param>
        private static void MaybeAddAllPluginStatus(
            Interpreter interpreter, /* in */
            ref StringList list      /* in, out */
            )
        {
            if (interpreter == null)
                return;

            PluginWrapperDictionary plugins = interpreter.CopyPlugins();

            if (plugins == null)
                return;

            StringList subList = null;

            foreach (PluginPair pair in plugins)
            {
                IPlugin plugin = pair.Value;

                if (plugin == null)
                    continue;

                MaybeAddPluginStatus(interpreter, plugin, ref subList);
            }

            if (subList != null)
            {
                if (list == null)
                    list = new StringList();

                list.Add("with plugin status");
                list.Add(subList.ToString());
            }
        }

        ///////////////////////////////////////////////////////////////////////

        //
        // WARNING: For use by the Utility class only.
        //
        /// <summary>
        /// This method determines whether this build of the runtime was
        /// compiled with threading support enabled.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter context, if any.  This parameter is not used.
        /// </param>
        /// <returns>
        /// True if threading support is available; otherwise, false.
        /// </returns>
        public static bool HaveThreading(
            Interpreter interpreter /* in: NOT USED */
            )
        {
            return HaveDefineConstant(ThreadingDefineName);
        }

        ///////////////////////////////////////////////////////////////////////

        //
        // WARNING: For use by the Utility class only.
        //
        /// <summary>
        /// This method determines whether this build of the runtime was
        /// compiled with native code support enabled.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter context, if any.  This parameter is not used.
        /// </param>
        /// <returns>
        /// True if native code support is available; otherwise, false.
        /// </returns>
        public static bool HaveNative(
            Interpreter interpreter /* in: NOT USED */
            )
        {
            return HaveDefineConstant(NativeDefineName);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method determines whether the specified define constant was
        /// present when this build of the runtime was compiled.
        /// </summary>
        /// <param name="name">
        /// The name of the define constant to check for.
        /// </param>
        /// <returns>
        /// True if the specified define constant is present; otherwise, false.
        /// </returns>
        public static bool HaveDefineConstant(
            string name /* in */
            )
        {
            if (String.IsNullOrEmpty(name))
                return false;

            StringList options = DefineConstants.OptionList;

            if (options == null)
                return false;

            return options.Contains(
                name, StringComparison.OrdinalIgnoreCase);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method builds a string describing the version of the runtime,
        /// subject to the specified flags.
        /// </summary>
        /// <param name="versionFlags">
        /// The flags used to control which pieces of version information are
        /// included in the resulting string.
        /// </param>
        /// <returns>
        /// The version string, or null if it could not be built.
        /// </returns>
        public static string GetVersion(
            VersionFlags versionFlags /* in */
            )
        {
            Result result = null;

            if (GetVersion(
                    null, versionFlags, ref result) == ReturnCode.Ok)
            {
                return result;
            }

            return null;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method builds a string describing the version of the runtime,
        /// subject to the specified flags, using the specified interpreter for
        /// context.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter to use for context.  This parameter is optional and
        /// may be null.
        /// </param>
        /// <param name="versionFlags">
        /// The flags used to control which pieces of version information are
        /// included in the resulting string.
        /// </param>
        /// <param name="result">
        /// Upon success, this contains the version string.  Upon failure, this
        /// contains an appropriate error message.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise, an appropriate
        /// error code.
        /// </returns>
        public static ReturnCode GetVersion(
            Interpreter interpreter,   /* in: OPTIONAL */
            VersionFlags versionFlags, /* in */
            ref Result result          /* out */
            )
        {
            StringList list = null;

            if (FlagOps.HasFlags(versionFlags, VersionFlags.Vendor, true))
            {
                string vendor = GetVendor(interpreter, false);

                if (vendor != null)
                {
                    if (list == null)
                        list = new StringList();

                    list.Add(vendor);
                }
            }

            if (FlagOps.HasFlags(versionFlags, VersionFlags.Core, true))
            {
                AddCoreVersionInformation(interpreter,
                    GlobalState.GetAssembly(),
                    GlobalState.GetAssemblyLocation(),
                    (interpreter != null) ?
                        interpreter.InternalIsSafe() : false,
                    FlagOps.HasFlags(
                        versionFlags, VersionFlags.AllowNull, true),
                    ref list);
            }

            if (FlagOps.HasFlags(versionFlags, VersionFlags.Plugins, true))
                MaybeAddAllPluginStatus(interpreter, ref list);

            result = list;
            return ReturnCode.Ok;
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Class Factory Methods
        /// <summary>
        /// This method parses a list of formal argument specifiers into a list
        /// of argument name and default value pairs.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter to use when splitting the argument specifiers.
        /// </param>
        /// <param name="list1">
        /// The list of formal argument specifiers to parse.  Each element may
        /// contain an argument name and an optional default value.
        /// </param>
        /// <param name="list2">
        /// Upon return, the list to which the parsed argument name and default
        /// value pairs are added.  If necessary, a new list is created.
        /// </param>
        /// <param name="error">
        /// Upon failure, this contains an appropriate error message.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise, an appropriate
        /// error code.
        /// </returns>
        public static ReturnCode GetFormalArgumentNamesAndDefaults(
            Interpreter interpreter,  /* in */
            StringList list1,         /* in */
            ref StringPairList list2, /* in, out */
            ref Result error          /* out */
            )
        {
            if (list1 == null)
            {
                error = "invalid formal argument list";
                return ReturnCode.Error;
            }

            if (list2 == null)
                list2 = new StringPairList();

            int count1 = list1.Count;

            for (int index1 = 0; index1 < count1; index1++)
            {
                StringList list3 = null;

                if (ParserOps<string>.SplitList(
                        interpreter, list1[index1], 0,
                        Length.Invalid, true, ref list3,
                        ref error) != ReturnCode.Ok)
                {
                    return ReturnCode.Error;
                }

                int count3 = list3.Count;

                if (count3 > 2)
                {
                    error = String.Format(
                        "too many fields in argument specifier {0}",
                        FormatOps.WrapOrNull(list1[index1]));

                    return ReturnCode.Error;
                }

                if (count3 == 0)
                {
                    error = "argument without name";
                    return ReturnCode.Error;
                }

                string argumentName = list3[0];

                if (String.IsNullOrEmpty(argumentName))
                {
                    error = "argument with null / empty name";
                    return ReturnCode.Error;
                }

                if (!Parser.IsSimpleScalarVariableName(
                        argumentName, String.Format(
                            Interpreter.ArgumentNotSimpleError,
                            FormatOps.WrapOrNull(argumentName)),
                        String.Format(
                            Interpreter.ArgumentNotScalarError,
                            FormatOps.WrapOrNull(argumentName)),
                        ref error))
                {
                    return ReturnCode.Error;
                }

                string argumentDefault;

                if (count3 >= 2)
                    argumentDefault = list3[1];
                else
                    argumentDefault = null;

                list2.Add(new StringPair(
                    argumentName, argumentDefault));
            }

            return ReturnCode.Ok;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method builds the formal argument list and the named argument
        /// dictionary for a procedure from a list of argument name and default
        /// value pairs.
        /// </summary>
        /// <param name="procedureName">
        /// The name of the procedure, used when formatting error messages.
        /// </param>
        /// <param name="list2">
        /// The list of argument name and default value pairs from which the
        /// formal and named arguments are built.
        /// </param>
        /// <param name="formalArguments">
        /// Upon success, this contains the resulting list of formal arguments.
        /// </param>
        /// <param name="namedArguments">
        /// Upon success, this contains the resulting dictionary of named
        /// arguments, keyed by argument name.
        /// </param>
        /// <param name="error">
        /// Upon failure, this contains an appropriate error message.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise, an appropriate
        /// error code.
        /// </returns>
        public static ReturnCode GetFormalAndNamedArguments(
            string procedureName,                  /* in */
            StringPairList list2,                  /* in */
            ref ArgumentList formalArguments,      /* out */
            ref ArgumentDictionary namedArguments, /* out */
            ref Result error                       /* out */
            )
        {
            if (list2 == null)
            {
                error = "invalid argument name / default list";
                return ReturnCode.Error;
            }

            formalArguments = new ArgumentList(list2,
                ArgumentFlags.NameOnly | ArgumentFlags.WithName);

            namedArguments = new ArgumentDictionary();

            foreach (Argument argument in formalArguments)
            {
                if (argument == null)
                    continue;

                string argumentName = argument.Name;

                if (argumentName == null)
                    continue;

                if (namedArguments.ContainsKey(argumentName))
                {
                    error = String.Format(
                        "procedure {0} duplicate argument named {1}",
                        FormatOps.WrapOrNull(procedureName),
                        FormatOps.WrapOrNull(argumentName));

                    return ReturnCode.Error;
                }

                namedArguments.Add(argumentName, argument);
            }

            return ReturnCode.Ok;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method creates a new core procedure instance of the appropriate
        /// type based on the flags contained in the specified procedure data.
        /// </summary>
        /// <param name="procedureData">
        /// The data describing the procedure to create, including its flags.
        /// </param>
        /// <param name="error">
        /// Upon failure, this contains an appropriate error message.
        /// </param>
        /// <returns>
        /// The newly created procedure, or null if it could not be created.
        /// </returns>
        public static IProcedure NewCoreProcedure(
            IProcedureData procedureData,
            ref Result error
            )
        {
            if (procedureData == null)
            {
                error = "invalid procedure data";
                return null;
            }

            ProcedureFlags flags = procedureData.Flags;

            if (FlagOps.HasFlags(
                    flags, ProcedureFlags.PositionalArguments, true))
            {
                return new _Procedures.PositionalArguments(
                    procedureData);
            }

            if (FlagOps.HasFlags(
                    flags, ProcedureFlags.NamedArguments, true))
            {
                return new _Procedures.NamedArguments(
                    procedureData);
            }

            error = String.Format(
                "don't know how to create procedure {0} of type {1}",
                FormatOps.DisplayName(procedureData.Name),
                FormatOps.WrapOrNull(flags));

            return null;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method creates a new procedure instance, using the procedure
        /// creation callback associated with the specified interpreter, if any,
        /// and falling back to the core procedure factory otherwise.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter that the procedure will belong to.  This parameter
        /// is optional and may be null.
        /// </param>
        /// <param name="name">
        /// The name of the procedure to create.
        /// </param>
        /// <param name="group">
        /// The group that the procedure belongs to, if any.
        /// </param>
        /// <param name="description">
        /// The description of the procedure, if any.
        /// </param>
        /// <param name="flags">
        /// The flags used to configure the procedure.
        /// </param>
        /// <param name="arguments">
        /// The formal arguments of the procedure.
        /// </param>
        /// <param name="namedArguments">
        /// The named arguments of the procedure, if any.
        /// </param>
        /// <param name="overwriteArguments">
        /// The arguments that should be overwritten when the procedure is
        /// invoked, if any.
        /// </param>
        /// <param name="cleanArguments">
        /// The arguments that should be cleaned up when the procedure is
        /// invoked, if any.
        /// </param>
        /// <param name="body">
        /// The script body of the procedure.
        /// </param>
        /// <param name="location">
        /// The script location associated with the procedure, if any.
        /// </param>
        /// <param name="clientData">
        /// The extra data to associate with the procedure, if any.
        /// </param>
        /// <param name="error">
        /// Upon failure, this contains an appropriate error message.
        /// </param>
        /// <returns>
        /// The newly created procedure, or null if it could not be created.
        /// </returns>
        public static IProcedure NewProcedure(
            Interpreter interpreter,
            string name,
            string group,
            string description,
            ProcedureFlags flags,
            ArgumentList arguments,
            ArgumentDictionary namedArguments,
            ArgumentList overwriteArguments,
            ArgumentList cleanArguments,
            string body,
            IScriptLocation location,
            IClientData clientData,
            ref Result error
            )
        {
            IProcedureData procedureData = new ProcedureData(
                name, group, description, flags, arguments,
                namedArguments, overwriteArguments, cleanArguments,
                body, location, clientData, 0);

            NewProcedureCallback callback = null;

            if (interpreter != null)
                callback = interpreter.NewProcedureCallback;

        retry:

            if (callback != null)
            {
                try
                {
                    IProcedure procedure = callback(
                        interpreter, procedureData,
                        ref error); /* throw */

                    if (procedure != null)
                        return procedure;

                    //
                    // HACK: If the callback returns
                    //       null, stop using it for
                    //       this procedure.
                    //
                    callback = null;
                    goto retry;
                }
                catch (Exception e)
                {
                    //
                    // HACK: If the callback throws
                    //       an exception, fail the
                    //       procedure creation.
                    //
                    error = e;
                    return null;
                }
            }
            else
            {
                return NewCoreProcedure(
                    procedureData, ref error);
            }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Culture Support Methods
        /// <summary>
        /// This method resolves the specified culture name or identifier into a
        /// <see cref="CultureInfo" /> instance.
        /// </summary>
        /// <param name="culture">
        /// The culture name or integer identifier to resolve.  An empty string
        /// selects the current culture and null selects the default culture.
        /// </param>
        /// <param name="specific">
        /// Non-zero to resolve the culture as a specific culture; zero to
        /// resolve it as either a neutral or specific culture.
        /// </param>
        /// <returns>
        /// The resolved <see cref="CultureInfo" />, or null if the culture
        /// could not be resolved.
        /// </returns>
        public static CultureInfo GetCultureInfo(
            string culture, /* in */
            bool specific   /* in */
            )
        {
            Result error = null;

            return GetCultureInfo(culture, specific, ref error);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method resolves the specified culture name or identifier into a
        /// <see cref="CultureInfo" /> instance, returning an error message on
        /// failure.
        /// </summary>
        /// <param name="culture">
        /// The culture name or integer identifier to resolve.  An empty string
        /// selects the current culture and null selects the default culture.
        /// </param>
        /// <param name="specific">
        /// Non-zero to resolve the culture as a specific culture; zero to
        /// resolve it as either a neutral or specific culture.
        /// </param>
        /// <param name="error">
        /// Upon failure, this contains an appropriate error message.
        /// </param>
        /// <returns>
        /// The resolved <see cref="CultureInfo" />, or null if the culture
        /// could not be resolved.
        /// </returns>
        public static CultureInfo GetCultureInfo(
            string culture,  /* in */
            bool specific,   /* in */
            ref Result error /* out */
            )
        {
            try
            {
                //
                // NOTE: Empty string is valid here, it can be used to
                //       select the current culture.
                //
                if (culture == null)
                    return Value.GetDefaultCulture();

                //
                // NOTE: Attempt to set the culture based on using the
                //       parameter "culture" as a name (either neutral
                //       or specific).  Empty string is valid here and
                //       selects the invariant culture.
                //
                return specific ?
                    CultureInfo.CreateSpecificCulture(culture) :
                    CultureInfo.GetCultureInfo(culture);
            }
#if NET_40
            catch (CultureNotFoundException)
#else
            catch
#endif
            {
                //
                // NOTE: It is not a valid culture name, try to convert
                //       the "culture" parameter to integer identifier.
                //
                int cultureId = 0;

                if (Value.GetInteger2(culture, ValueFlags.AnyInteger,
                        null /* culture not yet set! */, ref cultureId,
                        ref error) == ReturnCode.Ok)
                {
                    try
                    {
                        return CultureInfo.GetCultureInfo(cultureId);
                    }
                    catch (Exception e)
                    {
                        //
                        // NOTE: It parsed as a valid integer; however,
                        //       that caused an exception.  Figure out
                        //       if the caught exception should be used
                        //       verbatim -OR- converted to a simpler
                        //       error message.
                        //
                        if ((e is ArgumentOutOfRangeException)
#if NET_40
                            || (e is CultureNotFoundException)
#endif
                            )
                        {
                            error = FormatOps.ErrorWithException(
                                String.Format(CultureInfoError,
                                FormatOps.WrapOrNull(culture)), e);
                        }
                        else
                        {
                            error = e;
                        }
                    }
                }

                return null;
            }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method creates a <see cref="ResourceManager" /> for the runtime
        /// resources associated with the specified culture.
        /// </summary>
        /// <param name="cultureInfo">
        /// The culture for which the resource manager is being created.
        /// </param>
        /// <param name="error">
        /// Upon failure, this contains an appropriate error message.
        /// </param>
        /// <returns>
        /// The newly created <see cref="ResourceManager" />, or null if it
        /// could not be created.
        /// </returns>
        public static ResourceManager GetResourceManager(
            CultureInfo cultureInfo, /* in */
            ref Result error         /* out */
            )
        {
            if (cultureInfo == null)
            {
                error = InvalidCultureInfoError;
                return null;
            }

            string resourceBaseName = GlobalState.GetResourceBaseName();

            if (resourceBaseName == null)
            {
                error = InvalidBaseResourceName;
                return null;
            }

            Assembly assembly = GlobalState.GetAssembly();

            if (resourceBaseName == null)
            {
                error = InvalidResourceAssembly;
                return null;
            }

            try
            {
                //
                // FIXME: PRI 4: Now that this resource management code
                //        is in place and working properly, we need to
                //        migrate all the error messages and other
                //        static strings to be managed resources.  The
                //        original intention was to do this right from
                //        the start; however, time constraints prevented
                //        that vision from becoming a reality.
                //
                return new ResourceManager(resourceBaseName, assembly);
            }
            catch (Exception e)
            {
                error = FormatOps.ErrorWithException(
                    String.Format(ResourceManagerError,
                    FormatOps.WrapOrNull(resourceBaseName)), e);

                return null;
            }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Resolver Support Methods
        /// <summary>
        /// This method determines whether hidden commands should be resolved,
        /// based on the specified engine flags.
        /// </summary>
        /// <param name="engineFlags">
        /// The engine flags that govern command resolution.
        /// </param>
        /// <param name="match">
        /// Non-zero if the resolution is being performed as part of a matching
        /// operation; otherwise, zero.
        /// </param>
        /// <returns>
        /// True if hidden commands should be resolved; otherwise, false.
        /// </returns>
        public static bool ShouldResolveHidden(
            EngineFlags engineFlags,
            bool match
            )
        {
            return EngineFlagOps.HasToExecute(engineFlags) &&
                !EngineFlagOps.HasUseHidden(engineFlags) &&
                (match ? EngineFlagOps.HasMatchHidden(engineFlags) :
                    EngineFlagOps.HasGetHidden(engineFlags));
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method determines whether namespace support is enabled, based on
        /// the specified creation flags.
        /// </summary>
        /// <param name="createFlags">
        /// The creation flags that govern interpreter behavior.
        /// </param>
        /// <returns>
        /// True if namespace support is enabled; otherwise, false.
        /// </returns>
        public static bool AreNamespacesEnabled(
            CreateFlags createFlags
            )
        {
            return FlagOps.HasFlags(
                createFlags, CreateFlags.UseNamespaces, true);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method creates a new command resolver of the appropriate type,
        /// based on whether namespace support is enabled.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter that the resolver will belong to.
        /// </param>
        /// <param name="frame">
        /// The call frame associated with the resolver, if any.
        /// </param>
        /// <param name="namespace">
        /// The namespace associated with the resolver, if any.
        /// </param>
        /// <param name="createFlags">
        /// The creation flags used to determine whether namespace support is
        /// enabled.
        /// </param>
        /// <returns>
        /// The newly created command resolver.
        /// </returns>
        public static IResolve NewResolver(
            Interpreter interpreter,
            ICallFrame frame,
            INamespace @namespace,
            CreateFlags createFlags
            )
        {
            if (AreNamespacesEnabled(createFlags))
            {
                return new _Resolvers.Namespace(new ResolveData(
                    null, null, null, ClientData.Empty, interpreter,
                    ResolveFlags.None, 0), frame, @namespace);
            }
            else
            {
                return new _Resolvers.Core(new ResolveData(
                    null, null, null, ClientData.Empty, interpreter,
                    ResolveFlags.None, 0));
            }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Reflection Support Methods
        /// <summary>
        /// This method computes a <see cref="TracePriority" /> value by parsing
        /// the specified new value, optionally combining it with the current
        /// value of the specified field.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter to use when parsing the trace priority flags.
        /// </param>
        /// <param name="fieldInfo">
        /// The field whose current value is used as the basis for parsing the
        /// new value, if any.
        /// </param>
        /// <param name="object">
        /// The object instance from which to read the field value, or null for
        /// a static field.
        /// </param>
        /// <param name="newValue">
        /// The string representation of the trace priority flags to parse.
        /// </param>
        /// <param name="cultureInfo">
        /// The culture to use when parsing the trace priority flags.
        /// </param>
        /// <param name="value">
        /// Upon success, this contains the resulting trace priority value.
        /// </param>
        /// <param name="error">
        /// Upon failure, this contains an appropriate error message.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise, an appropriate
        /// error code.
        /// </returns>
        public static ReturnCode GetTracePriorityValue(
            Interpreter interpreter,
            FieldInfo fieldInfo,
            object @object,
            string newValue,
            CultureInfo cultureInfo,
            ref TracePriority value,
            ref Result error
            )
        {
            string oldValue = null;

            if (fieldInfo != null)
            {
                try
                {
                    oldValue = StringOps.GetStringFromObject(
                        fieldInfo.GetValue(@object)); /* throw */
                }
                catch (Exception e)
                {
                    error = e;
                    return ReturnCode.Error;
                }
            }

            object enumValue = EnumOps.TryParseFlags(
                interpreter, typeof(TracePriority), oldValue,
                newValue, cultureInfo, true, true, true,
                ref error);

            if (enumValue is TracePriority)
            {
                value = (TracePriority)enumValue;
                return ReturnCode.Ok;
            }

            return ReturnCode.Error;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method determines whether the specified argument list consists
        /// of exactly the specified request name and no other arguments.
        /// </summary>
        /// <param name="arguments">
        /// The argument list to examine.
        /// </param>
        /// <param name="name">
        /// The request name to match against the first argument.
        /// </param>
        /// <returns>
        /// True if the argument list contains only the specified request name;
        /// otherwise, false.
        /// </returns>
        public static bool MatchFieldNameOnly(
            ArgumentList arguments,
            string name
            )
        {
            int count;

            if (!MatchRequestName(arguments, name, out count))
                return false;

            return (count == 1);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method determines whether the first element of the specified
        /// argument list matches the specified request name.
        /// </summary>
        /// <param name="arguments">
        /// The argument list to examine.
        /// </param>
        /// <param name="name">
        /// The request name to match against the first argument.
        /// </param>
        /// <param name="count">
        /// Upon return, this contains the number of arguments in the list, or
        /// an invalid count if the list is null.
        /// </param>
        /// <returns>
        /// True if the first argument matches the specified request name;
        /// otherwise, false.
        /// </returns>
        public static bool MatchRequestName(
            ArgumentList arguments,
            string name,
            out int count
            )
        {
            if (arguments == null)
            {
                count = Count.Invalid;
                return false;
            }

            count = arguments.Count;

            if (count < 1)
                return false;

            return SharedStringOps.SystemEquals(arguments[0], name);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method gets or sets the value of a named field, based on the
        /// specified arguments, if the field is present in the specified
        /// dictionary of supported fields.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter to use when converting the field value.
        /// </param>
        /// <param name="fields">
        /// The dictionary of supported fields, keyed by name.
        /// </param>
        /// <param name="object">
        /// The object instance on which to get or set the field value, or null
        /// for a static field.
        /// </param>
        /// <param name="arguments">
        /// The arguments specifying the field name and, optionally, the new
        /// value to set.
        /// </param>
        /// <param name="cultureInfo">
        /// The culture to use when converting the field value.
        /// </param>
        /// <param name="result">
        /// Upon success, this contains the field value that was read or set.
        /// </param>
        /// <param name="done">
        /// Upon return, non-zero if the field request was handled; otherwise,
        /// zero, indicating that the request was not supported.
        /// </param>
        /// <param name="error">
        /// Upon failure, this contains an appropriate error message.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise, an appropriate
        /// error code.
        /// </returns>
        public static ReturnCode MaybeGetOrSetFieldValue(
            Interpreter interpreter,
            FieldInfoDictionary fields,
            object @object,
            ArgumentList arguments,
            CultureInfo cultureInfo,
            ref object result,
            out bool done,
            ref Result error
            )
        {
            done = false;

            if (arguments == null)
                return ReturnCode.Ok; /* UNSUPPORTED REQUEST */

            int count = arguments.Count;

            if ((count < 1) || (count > 2))
                return ReturnCode.Ok; /* UNSUPPORTED REQUEST */

            string fieldName = arguments[0];

            if (fieldName == null)
                return ReturnCode.Ok; /* UNSUPPORTED REQUEST */

            IAnyPair<FieldInfo, object> anyPair;

            if ((fields == null) ||
                !fields.TryGetValue(fieldName, out anyPair))
            {
                return ReturnCode.Ok; /* UNSUPPORTED REQUEST */
            }

            if (anyPair == null)
                return ReturnCode.Ok; /* UNSUPPORTED REQUEST */

            FieldInfo fieldInfo = anyPair.X;

            if (fieldInfo == null)
                return ReturnCode.Ok; /* UNSUPPORTED REQUEST */

            Type fieldType = fieldInfo.FieldType;

            if ((fieldType != typeof(string)) &&
                (fieldType != typeof(TracePriority)) &&
                (fieldType != typeof(bool)))
            {
                return ReturnCode.Ok; /* UNSUPPORTED REQUEST */
            }

            if (count >= 2)
            {
                object fieldValue = anyPair.Y;
                string stringValue = arguments[1];

                if (fieldType == typeof(string))
                {
                    try
                    {
                        if (stringValue != null)
                        {
                            fieldInfo.SetValue(
                                @object, stringValue); /* throw */
                        }
                        else if (fieldValue is string)
                        {
                            fieldInfo.SetValue(
                                @object, fieldValue); /* throw */
                        }
                        else /* stringValue == null */
                        {
                            fieldInfo.SetValue(
                                @object, stringValue); /* throw */
                        }
                    }
                    catch (Exception e)
                    {
                        error = e;
                        return ReturnCode.Error;
                    }
                }
                else if (fieldType == typeof(TracePriority))
                {
                    if (stringValue != null)
                    {
                        TracePriority priority = TracePriority.None;

                        if (GetTracePriorityValue(
                                interpreter, fieldInfo, @object,
                                stringValue, cultureInfo,
                                ref priority, ref error) == ReturnCode.Ok)
                        {
                            try
                            {
                                fieldInfo.SetValue(
                                    @object, priority); /* throw */
                            }
                            catch (Exception e)
                            {
                                error = e;
                                return ReturnCode.Error;
                            }
                        }
                        else
                        {
                            return ReturnCode.Error;
                        }
                    }
                    else if (fieldValue is TracePriority)
                    {
                        try
                        {
                            fieldInfo.SetValue(
                                @object, fieldValue); /* throw */
                        }
                        catch (Exception e)
                        {
                            error = e;
                            return ReturnCode.Error;
                        }
                    }
                    else
                    {
                        error = String.Format(
                            "expected {0} value for {1}",
                            MarshalOps.GetErrorTypeName(
                                typeof(TracePriority)),
                            FormatOps.WrapOrNull(fieldName));

                        return ReturnCode.Error;
                    }
                }
                else if (fieldType == typeof(bool))
                {
                    if (stringValue != null)
                    {
                        bool boolValue = false;

                        if (Value.GetBoolean2(
                                stringValue, ValueFlags.AnyBoolean,
                                cultureInfo, ref boolValue,
                                ref error) == ReturnCode.Ok)
                        {
                            try
                            {
                                fieldInfo.SetValue(
                                    @object, boolValue); /* throw */
                            }
                            catch (Exception e)
                            {
                                error = e;
                                return ReturnCode.Error;
                            }
                        }
                        else
                        {
                            return ReturnCode.Error;
                        }
                    }
                    else if (fieldValue is bool)
                    {
                        try
                        {
                            fieldInfo.SetValue(
                                @object, fieldValue); /* throw */
                        }
                        catch (Exception e)
                        {
                            error = e;
                            return ReturnCode.Error;
                        }
                    }
                    else
                    {
                        error = String.Format(
                            "expected boolean value for {0}",
                            FormatOps.WrapOrNull(fieldName));

                        return ReturnCode.Error;
                    }
                }
                else
                {
                    //
                    // NOTE: It should not be possible to hit this point
                    //       as the field type must be string or boolean
                    //       based on the preliminary check above.
                    //
                    error = String.Format(
                        "unsupported type {0}, must be {1}, {2}, or {3}",
                        MarshalOps.GetErrorTypeName(fieldType),
                        MarshalOps.GetErrorTypeName(typeof(string)),
                        MarshalOps.GetErrorTypeName(typeof(TracePriority)),
                        MarshalOps.GetErrorTypeName(typeof(bool)));

                    return ReturnCode.Error;
                }
            }

            try
            {
                result = fieldInfo.GetValue(@object); /* throw */
                done = true;

                return ReturnCode.Ok;
            }
            catch (Exception e)
            {
                error = e;
                return ReturnCode.Error;
            }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method selects, from a list of types, the one whose full name
        /// most closely resembles the specified text.
        /// </summary>
        /// <param name="types">
        /// The list of types to consider.  This parameter may be null.
        /// </param>
        /// <param name="text">
        /// The text to compare each type name against.
        /// </param>
        /// <param name="comparisonType">
        /// The <see cref="StringComparison" /> value used when comparing type
        /// names.
        /// </param>
        /// <returns>
        /// The type with the most similar name, or null if there are no types
        /// or none is sufficiently similar.
        /// </returns>
        public static Type GetTypeWithMostSimilarName(
            TypeList types,
            string text,
            StringComparison comparisonType
            )
        {
            if (types == null)
                return null;

            Type typeWithMostSimilarName = null;
            int mostSimilarNameResult = 0;

            foreach (Type type in types)
            {
                if (type == null)
                    continue;

                int similarNameResult = MarshalOps.CompareSimilarTypeNames(
                    type.FullName, text, comparisonType);

                if (typeWithMostSimilarName == null)
                {
                    mostSimilarNameResult = similarNameResult;
                    typeWithMostSimilarName = type;
                    continue;
                }

                if ((mostSimilarNameResult == 0) ||
                    (similarNameResult > mostSimilarNameResult))
                {
                    mostSimilarNameResult = similarNameResult;
                    typeWithMostSimilarName = type;
                }
            }

            if (mostSimilarNameResult > 0)
                return typeWithMostSimilarName;

            return null;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method selects, from a list of types, the one that exposes the
        /// greatest number of members matching the specified binding flags.
        /// </summary>
        /// <param name="types">
        /// The list of types to consider.  This parameter may be null.
        /// </param>
        /// <param name="bindingFlags">
        /// The <see cref="BindingFlags" /> used to enumerate the members of
        /// each type.  When this is <see cref="BindingFlags.Default" />, the
        /// default member enumeration is used.
        /// </param>
        /// <returns>
        /// The type with the most members, or null if there are no types to
        /// consider.
        /// </returns>
        public static Type GetTypeWithMostMembers(
            TypeList types,
            BindingFlags bindingFlags
            )
        {
            if (types == null)
                return null;

            Type typeWithMostMembers = null;
            MemberInfo[] mostMemberInfos = null;

            foreach (Type type in types)
            {
                if (type == null)
                    continue;

                MemberInfo[] memberInfos;

                if (bindingFlags != BindingFlags.Default)
                    memberInfos = type.GetMembers(bindingFlags);
                else
                    memberInfos = type.GetMembers();

                if (memberInfos == null)
                    continue;

                if (typeWithMostMembers == null)
                {
                    mostMemberInfos = memberInfos;
                    typeWithMostMembers = type;
                    continue;
                }

                if ((mostMemberInfos == null) ||
                    (memberInfos.Length > mostMemberInfos.Length))
                {
                    mostMemberInfos = memberInfos;
                    typeWithMostMembers = type;
                }
            }

            return typeWithMostMembers;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method determines whether a class type implements a particular
        /// interface type.
        /// </summary>
        /// <param name="type">
        /// The class type to check.  This parameter may be null.
        /// </param>
        /// <param name="matchType">
        /// The interface type to check for.  This parameter may be null.
        /// </param>
        /// <returns>
        /// True if <paramref name="type" /> is a class that implements the
        /// interface <paramref name="matchType" />; otherwise, false.
        /// </returns>
        public static bool DoesClassTypeSupportInterface(
            Type type,
            Type matchType
            )
        {
            if ((type == null) || !type.IsClass)
                return false;

            if ((matchType == null) || !matchType.IsInterface)
                return false;

            //
            // HACK: Yes, this is horrible.  There must be a cleaner way of
            //       checking if a given type implements a given interface.
            //
            return (type.GetInterface(matchType.FullName) != null);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method determines whether a class type is equal to, or
        /// optionally a sub-class of, another type.
        /// </summary>
        /// <param name="type">
        /// The class type to check.  This parameter may be null.
        /// </param>
        /// <param name="matchType">
        /// The type to compare against.  This parameter may be null and must
        /// not be an interface type.
        /// </param>
        /// <param name="subClass">
        /// Non-zero if <paramref name="type" /> being a sub-class of
        /// <paramref name="matchType" /> should be considered a match.
        /// </param>
        /// <returns>
        /// True if <paramref name="type" /> is a class that is equal to (or,
        /// when permitted, a sub-class of) <paramref name="matchType" />;
        /// otherwise, false.
        /// </returns>
        public static bool IsClassTypeEqualOrSubClass(
            Type type,
            Type matchType,
            bool subClass
            )
        {
            if ((type == null) || !type.IsClass)
                return false;

            if ((matchType == null) || matchType.IsInterface)
                return false;

            if (type.Equals(matchType))
                return true;

            if (subClass && type.IsSubclassOf(matchType))
                return true;

            return false;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method determines whether a type matches another type, handling
        /// both interface implementation and class equality or inheritance.
        /// </summary>
        /// <param name="type">
        /// The type to check.  This parameter may be null.
        /// </param>
        /// <param name="matchType">
        /// The type to match against.  This parameter may be null.  When it is
        /// an interface, the check is whether <paramref name="type" />
        /// implements it; otherwise, the check is for equality or, optionally,
        /// inheritance.
        /// </param>
        /// <param name="subClass">
        /// Non-zero if <paramref name="type" /> being a sub-class of
        /// <paramref name="matchType" /> should be considered a match.
        /// </param>
        /// <returns>
        /// True if the types match; otherwise, false.  When both
        /// <paramref name="type" /> and <paramref name="matchType" /> are null,
        /// this is considered a match.
        /// </returns>
        public static bool DoesClassTypeMatch(
            Type type,
            Type matchType,
            bool subClass
            )
        {
            if ((type != null) && (matchType != null))
            {
                //
                // NOTE: Are we matching against an interface type?
                //
                if (matchType.IsInterface)
                {
                    //
                    // NOTE: Does the class implement the interface?
                    //
                    if (DoesClassTypeSupportInterface(type, matchType))
                        return true;
                }
                else
                {
                    //
                    // NOTE: Are the types equal; otherwise, [optionally]
                    //       is the type a sub-class of the type to match
                    //       against?
                    //
                    if (IsClassTypeEqualOrSubClass(
                            type, matchType, subClass))
                    {
                        return true;
                    }
                }
            }
            else if ((type == null) && (matchType == null))
            {
                //
                // NOTE: If both are null we consider that to be a match.
                //
                return true;
            }

            return false;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method builds the set of plugin flags indicating which security
        /// checks (StrongName verification and file trust) should be skipped,
        /// based on the current configuration.
        /// </summary>
        /// <returns>
        /// The combined <see cref="PluginFlags" /> value describing the checks
        /// to skip.
        /// </returns>
        private static PluginFlags GetSkipCheckPluginFlags()
        {
            PluginFlags result = PluginFlags.None;

            if (!ShouldCheckStrongNameVerified())
                result |= PluginFlags.SkipVerified;

            if (!ShouldCheckFileTrusted())
                result |= PluginFlags.SkipTrusted;

            return result;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method determines whether the assembly associated with the
        /// specified plugin data is licensed for use.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter context to use, if any.  This parameter may be null.
        /// </param>
        /// <param name="pluginData">
        /// The plugin data whose assembly should be checked.  This parameter
        /// may be null.
        /// </param>
        /// <param name="error">
        /// Upon failure, receives information about the error that was
        /// encountered.
        /// </param>
        /// <returns>
        /// True if the plugin assembly is licensed; otherwise, false.
        /// </returns>
        public static bool IsLicensed(
            Interpreter interpreter,
            IPluginData pluginData,
            ref Result error
            )
        {
            if (pluginData == null)
            {
                error = "invalid plugin data";
                return false;
            }

            if (AppDomainOps.IsCross(interpreter, pluginData))
            {
                error = "unsupported when plugin is isolated";
                return false;
            }

            try
            {
                bool noTrace = false; /* NOT USED */

                return IsLicensed(
                    interpreter, pluginData.Assembly, /* throw */
                    ref noTrace, ref error);
            }
            catch (Exception e)
            {
                error = e;
                return false;
            }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method determines whether the specified assembly is licensed
        /// for use, emitting a diagnostic trace when it is not (unless tracing
        /// is suppressed internally).
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter context to use, if any.  This parameter may be null.
        /// </param>
        /// <param name="assembly">
        /// The assembly to check.  This parameter may be null.
        /// </param>
        /// <returns>
        /// True if the assembly is licensed; otherwise, false.
        /// </returns>
        private static bool IsLicensed(
            Interpreter interpreter,
            Assembly assembly
            )
        {
            bool noTrace = false;
            Result error = null;

            if (IsLicensed(
                    interpreter, assembly, ref noTrace,
                    ref error))
            {
                return true;
            }

            if (!noTrace)
            {
                //
                // HACK: This is not really an error,
                //       per se.
                //
                TraceOps.DebugTrace(String.Format(
                    "IsLicensed: error = {0}",
                    FormatOps.WrapOrNull(error)),
                    typeof(RuntimeOps).Name,
                    TracePriority.SecurityError);
            }

            return false;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method determines whether the specified assembly is licensed
        /// for use, performing the underlying genuineness and security
        /// certificate checks.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter context to use, if any.  This parameter may be null.
        /// </param>
        /// <param name="assembly">
        /// The assembly to check.  This parameter may be null.
        /// </param>
        /// <param name="noTrace">
        /// Upon return, non-zero indicates that the failure does not warrant a
        /// diagnostic trace by the caller.
        /// </param>
        /// <param name="error">
        /// Upon failure, receives information about the error that was
        /// encountered.
        /// </param>
        /// <returns>
        /// True if the assembly is licensed; otherwise, false.
        /// </returns>
        public static bool IsLicensed(
            Interpreter interpreter,
            Assembly assembly,
            ref bool noTrace,
            ref Result error
            )
        {
            if (!GlobalState.IsAssembly(assembly))
            {
                noTrace = true;
                error = "wrong plugin assembly";

                return false;
            }

            if (!IsGenuine())
            {
                error = "plugin is not genuine";
                return false;
            }

#if !DEBUG
            if (interpreter == null)
            {
                error = "invalid interpreter";
                return false;
            }

            Result result = null;

            if (ScriptOps.CheckSecurityCertificate(
                    interpreter, false, ref result) != ReturnCode.Ok)
            {
                error = result;
                return false;
            }
#endif

            return true;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method computes the plugin flags describing the security
        /// characteristics (e.g. StrongName, verification, Authenticode, and
        /// trust) of an assembly given its raw bytes, skipping any checks
        /// indicated by the current configuration.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter context to use, if any.  This parameter may be null.
        /// </param>
        /// <param name="trustedHashes">
        /// The list of trusted hashes used when evaluating file trust.  This
        /// parameter may be null.
        /// </param>
        /// <param name="assembly">
        /// The assembly to evaluate.  This parameter may be null.
        /// </param>
        /// <param name="assemblyBytes">
        /// The raw bytes of the assembly to evaluate.  This parameter may be
        /// null.
        /// </param>
        /// <returns>
        /// The combined <see cref="PluginFlags" /> value describing the
        /// assembly.
        /// </returns>
        public static PluginFlags GetAssemblyPluginFlags(
            Interpreter interpreter,
            StringList trustedHashes,
            Assembly assembly,
            byte[] assemblyBytes
            )
        {
            return GetAssemblyPluginFlags(
                interpreter, trustedHashes, assembly, assemblyBytes,
                GetSkipCheckPluginFlags());
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method computes the plugin flags describing the security
        /// characteristics (e.g. StrongName, verification, Authenticode, and
        /// trust) of an assembly given its raw bytes, honoring the specified
        /// flags that indicate which checks should be skipped.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter context to use, if any.  This parameter may be null.
        /// </param>
        /// <param name="trustedHashes">
        /// The list of trusted hashes used when evaluating file trust.  This
        /// parameter may be null.
        /// </param>
        /// <param name="assembly">
        /// The assembly to evaluate.  This parameter may be null.
        /// </param>
        /// <param name="assemblyBytes">
        /// The raw bytes of the assembly to evaluate.  This parameter may be
        /// null.
        /// </param>
        /// <param name="pluginFlags">
        /// The plugin flags indicating which security checks should be skipped.
        /// </param>
        /// <returns>
        /// The combined <see cref="PluginFlags" /> value describing the
        /// assembly.
        /// </returns>
        private static PluginFlags GetAssemblyPluginFlags(
            Interpreter interpreter,
            StringList trustedHashes,
            Assembly assembly,
            byte[] assemblyBytes,
            PluginFlags pluginFlags
            )
        {
            PluginFlags result = PluginFlags.None;

            ///////////////////////////////////////////////////////////////////

#if CAS_POLICY
            if (assembly != null)
            {
                //
                // NOTE: Check if the plugin has a StrongName signature.
                //
                StrongName strongName = null;

                if ((AssemblyOps.GetStrongName(assembly,
                        ref strongName) == ReturnCode.Ok) &&
                    (strongName != null))
                {
                    result |= PluginFlags.StrongName;
                }
            }
#endif

            ///////////////////////////////////////////////////////////////////

            //
            // NOTE: Skip checking the StrongName signature?
            //
            if (!FlagOps.HasFlags(
                    pluginFlags, PluginFlags.SkipVerified, true))
            {
                //
                // NOTE: See if the StrongName signature has really
                //       been verified by the CLR itself [via the CLR
                //       native API StrongNameSignatureVerificationEx].
                //
                if ((assemblyBytes != null) &&
                    IsStrongNameVerified(interpreter, assemblyBytes, true))
                {
                    result |= PluginFlags.Verified;
                }
            }
            else
            {
                result |= PluginFlags.SkipVerified;
            }

            ///////////////////////////////////////////////////////////////////

            if (assemblyBytes != null)
            {
                //
                // NOTE: Check if the plugin has an Authenticode signature.
                //
                X509Certificate certificate = null;

                if ((AssemblyOps.GetCertificate(
                        assemblyBytes, ref certificate) == ReturnCode.Ok) &&
                    (certificate != null))
                {
                    result |= PluginFlags.Authenticode;

                    //
                    // NOTE: Skip checking the Authenticode signature?
                    //
                    if (!FlagOps.HasFlags(
                            pluginFlags, PluginFlags.SkipTrusted, true))
                    {
                        //
                        // NOTE: See if the Authenticode signature and
                        //       certificate are trusted by the operating
                        //       system [via the Win32 native API
                        //       WinVerifyTrust].
                        //
                        if (IsFileTrusted(
                                interpreter, trustedHashes, assemblyBytes))
                        {
                            result |= PluginFlags.Trusted;
                        }
                    }
                    else
                    {
                        result |= PluginFlags.SkipTrusted;
                    }
                }
            }

            return result;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method computes the plugin flags describing the security
        /// characteristics (e.g. StrongName, verification, Authenticode, and
        /// trust) of an assembly given the assembly itself, skipping any checks
        /// indicated by the current configuration.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter context to use, if any.  This parameter may be null.
        /// </param>
        /// <param name="trustedHashes">
        /// The list of trusted hashes used when evaluating file trust.  This
        /// parameter may be null.
        /// </param>
        /// <param name="assembly">
        /// The assembly to evaluate.  This parameter may be null.
        /// </param>
        /// <returns>
        /// The combined <see cref="PluginFlags" /> value describing the
        /// assembly.
        /// </returns>
        public static PluginFlags GetAssemblyPluginFlags(
            Interpreter interpreter,
            StringList trustedHashes,
            Assembly assembly
            )
        {
            return GetAssemblyPluginFlags(interpreter,
                trustedHashes, assembly, GetSkipCheckPluginFlags());
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method computes the plugin flags describing the security
        /// characteristics (e.g. StrongName, verification, Authenticode, and
        /// trust) of an assembly given the assembly itself, honoring the
        /// specified flags that indicate which checks should be skipped.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter context to use, if any.  This parameter may be null.
        /// </param>
        /// <param name="trustedHashes">
        /// The list of trusted hashes used when evaluating file trust.  This
        /// parameter may be null.
        /// </param>
        /// <param name="assembly">
        /// The assembly to evaluate.  This parameter may be null.
        /// </param>
        /// <param name="pluginFlags">
        /// The plugin flags indicating which security checks should be skipped.
        /// </param>
        /// <returns>
        /// The combined <see cref="PluginFlags" /> value describing the
        /// assembly, or <see cref="PluginFlags.None" /> if
        /// <paramref name="assembly" /> is null.
        /// </returns>
        private static PluginFlags GetAssemblyPluginFlags(
            Interpreter interpreter,
            StringList trustedHashes,
            Assembly assembly,
            PluginFlags pluginFlags
            )
        {
            if (assembly == null)
                return PluginFlags.None;

            PluginFlags result = PluginFlags.None;

            ///////////////////////////////////////////////////////////////////

#if DEBUG
            if (IsLicensed(null, assembly))
                result |= PluginFlags.Licensed;
#endif

            ///////////////////////////////////////////////////////////////////

#if CAS_POLICY
            //
            // NOTE: Check if the plugin has a StrongName signature.
            //
            StrongName strongName = null;

            if ((AssemblyOps.GetStrongName(assembly,
                    ref strongName) == ReturnCode.Ok) &&
                (strongName != null))
            {
                result |= PluginFlags.StrongName;
            }
#endif

            ///////////////////////////////////////////////////////////////////

            //
            // NOTE: Skip checking the StrongName signature?
            //
            if (!FlagOps.HasFlags(
                    pluginFlags, PluginFlags.SkipVerified, true))
            {
                //
                // NOTE: See if the StrongName signature has really
                //       been verified by the CLR itself [via the CLR
                //       native API StrongNameSignatureVerificationEx].
                //
                if (IsStrongNameVerified(assembly.Location, true))
                {
                    result |= PluginFlags.Verified;
                }
            }
            else
            {
                result |= PluginFlags.SkipVerified;
            }

            ///////////////////////////////////////////////////////////////////

            //
            // NOTE: Check if the plugin has an Authenticode signature.
            //
            X509Certificate certificate = null;

            if ((AssemblyOps.GetCertificate(
                    assembly, ref certificate) == ReturnCode.Ok) &&
                (certificate != null))
            {
                result |= PluginFlags.Authenticode;

                //
                // NOTE: Skip checking the Authenticode signature?
                //
                if (!FlagOps.HasFlags(
                        pluginFlags, PluginFlags.SkipTrusted, true))
                {
                    //
                    // NOTE: See if the Authenticode signature and
                    //       certificate are trusted by the operating
                    //       system [via the Win32 native API
                    //       WinVerifyTrust].
                    //
                    if (IsFileTrusted(
                            interpreter, trustedHashes, assembly.Location,
                            IntPtr.Zero))
                    {
                        result |= PluginFlags.Trusted;
                    }
                }
                else
                {
                    result |= PluginFlags.SkipTrusted;
                }
            }

            ///////////////////////////////////////////////////////////////////

            return result;
        }

        ///////////////////////////////////////////////////////////////////////

        //
        // HACK: Yes, this method uses the TryLock pattern and then calls
        //       the CopyTrustedHashes interpreter method, which has its
        //       own internal locking; this is done to prevent a possible
        //       deadlock.
        //
        /// <summary>
        /// This method copies the trusted hashes from the specified
        /// interpreter, using a lock to coordinate access to interpreter state.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter whose trusted hashes should be copied.  This
        /// parameter may be null.
        /// </param>
        /// <param name="clear">
        /// Non-zero to clear the trusted hashes from the interpreter after they
        /// have been copied.
        /// </param>
        /// <returns>
        /// A new list containing the copied trusted hashes, or null if there
        /// is no interpreter or the interpreter lock could not be obtained.
        /// </returns>
        private static StringList CopyTrustedHashes(
            Interpreter interpreter, /* in */
            bool clear               /* in */
            )
        {
            StringList result = null;

            if (interpreter != null)
            {
                bool locked = false;

                try
                {
                    interpreter.InternalHardTryLock(
                        ref locked); /* TRANSACTIONAL */

                    if (locked)
                    {
                        result = interpreter.CopyTrustedHashes(
                            clear);
                    }
                    else
                    {
                        TraceOps.LockTrace(
                            "CopyTrustedHashes",
                            typeof(RuntimeOps).Name, false,
                            TracePriority.LockError,
                            interpreter.MaybeWhoHasLock());
                    }
                }
                finally
                {
                    interpreter.InternalExitLock(
                        ref locked); /* TRANSACTIONAL */
                }
            }

            return result;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method combines the trusted hashes from the specified
        /// interpreter and the global state into a single list, copying from
        /// either or both sources as requested.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter whose trusted hashes should be included.  This
        /// parameter is optional and may be null.
        /// </param>
        /// <param name="noInterpreter">
        /// Non-zero to exclude the trusted hashes from the interpreter.
        /// </param>
        /// <param name="noGlobal">
        /// Non-zero to exclude the trusted hashes from the global state.
        /// </param>
        /// <param name="clear">
        /// Non-zero to clear the trusted hashes from each source after they
        /// have been copied.
        /// </param>
        /// <returns>
        /// A new list containing the combined trusted hashes, or null if no
        /// trusted hashes were available from either source.
        /// </returns>
        public static StringList CombineOrCopyTrustedHashes(
            Interpreter interpreter, /* in: OPTIONAL */
            bool noInterpreter,      /* in */
            bool noGlobal,           /* in */
            bool clear               /* in */
            )
        {
            StringList trustedHashes1 = noInterpreter ?
                null : CopyTrustedHashes(interpreter, clear);

            StringList trustedHashes2 = noGlobal ?
                null : GlobalState.CopyTrustedHashes(clear);

            if ((trustedHashes1 != null) || (trustedHashes2 != null))
            {
                StringList trustedHashes3 = new StringList();

                if (trustedHashes1 != null)
                    trustedHashes3.AddRange(trustedHashes1);

                if (trustedHashes2 != null)
                    trustedHashes3.AddRange(trustedHashes2);

                return trustedHashes3;
            }

            return null;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method determines whether the specified file is a managed
        /// assembly by inspecting its PE file headers for a CLR header.
        /// </summary>
        /// <param name="fileName">
        /// The name of the file to inspect.  This parameter may be null or
        /// empty.
        /// </param>
        /// <returns>
        /// True if the file appears to be a managed assembly; otherwise, false.
        /// </returns>
        public static bool IsManagedAssembly(
            string fileName /* in */
            )
        {
            if (String.IsNullOrEmpty(fileName))
                return false;

            ushort magic = FileOps.IMAGE_NT_OPTIONAL_BAD_MAGIC;
            uint clrHeader = 0;

            if (!FileOps.GetPeFileMagic(
                    fileName, ref magic, ref clrHeader))
            {
                return false;
            }

            if (magic == FileOps.IMAGE_NT_OPTIONAL_BAD_MAGIC)
                return false;

            if (clrHeader == 0)
                return false;

            return true;
        }

        ///////////////////////////////////////////////////////////////////////

#if APPDOMAINS || ISOLATED_INTERPRETERS || ISOLATED_PLUGINS
        /// <summary>
        /// This method previews the plugin data for a plugin contained in the
        /// specified file by loading it into a temporary, isolated application
        /// domain so that its metadata can be inspected without committing to
        /// loading it.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter context to use.  This parameter may be null.
        /// </param>
        /// <param name="fileName">
        /// The name of the file containing the plugin to preview.  This
        /// parameter may be null.
        /// </param>
        /// <param name="typeName">
        /// The name of the plugin type to preview, if known.  This parameter
        /// may be null.
        /// </param>
        /// <param name="pluginFlags">
        /// The plugin flags controlling how the plugin is loaded and previewed.
        /// </param>
        /// <param name="verbose">
        /// Non-zero to enable verbose diagnostic output during the preview.
        /// </param>
        /// <param name="error">
        /// Upon failure, receives information about the error that was
        /// encountered.
        /// </param>
        /// <returns>
        /// The previewed plugin data, or null if the plugin data could not be
        /// obtained.
        /// </returns>
        private static IPluginData PreviewPluginData(
            Interpreter interpreter, /* in */
            string fileName,         /* in */
            string typeName,         /* in */
            PluginFlags pluginFlags, /* in */
            bool verbose,            /* in */
            ref Result error         /* out */
            )
        {
            string friendlyName = null;
            AppDomain appDomain = null;

            try
            {
                friendlyName = AppDomainOps.GetFriendlyName(
                    "preview", fileName, typeName, ref error);

                if (friendlyName == null)
                    return null;

                string packagePath = null;

                if (fileName != null)
                    packagePath = Path.GetDirectoryName(fileName);

                if (AppDomainOps.Create(
                        interpreter, friendlyName, packagePath, true,
#if ISOLATED_PLUGINS
                        FlagOps.HasFlags(pluginFlags,
                            PluginFlags.VerifyCoreAssembly, true),
                        !FlagOps.HasFlags(pluginFlags,
                            PluginFlags.NoUseEntryAssembly, true),
                        FlagOps.HasFlags(pluginFlags,
                            PluginFlags.OptionalEntryAssembly, true),
#else
                        false, false, false,
#endif
                        ref appDomain, ref error) != ReturnCode.Ok)
                {
                    return null;
                }

                PluginLoaderFlags pluginLoaderFlags =
                    PluginLoaderFlags.Preview;

#if !NET_STANDARD_20
                CrossAppDomainDelegate @delegate = null;
#else
                GenericCallback @delegate = null;
#endif

                object helper = Interpreter.GetReflectionHelper(
                    CombineOrCopyTrustedHashes(
                        interpreter, false, false, false),
                    fileName, null, pluginLoaderFlags,
                    verbose, ref @delegate, ref error);

                if (helper == null)
                    return null;

                AppDomainOps.DoCallBack(appDomain, @delegate);

                return Interpreter.ExtractPluginData(helper, ref error);
            }
            catch (Exception e)
            {
                error = e;
            }
            finally
            {
                AppDomainOps.UnloadOrComplain(
                    interpreter, friendlyName, appDomain, null);
            }

            return null;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method previews the plugin metadata for an in-memory assembly
        /// by loading it into an isolated application domain, extracting its
        /// plugin data, and then unloading that application domain.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter context to use when creating the temporary
        /// application domain and reflection helper.
        /// </param>
        /// <param name="assemblyBytes">
        /// The raw bytes of the assembly to be previewed.
        /// </param>
        /// <param name="typeName">
        /// The name of the plugin type to look for, or null to use the
        /// primary plugin type.
        /// </param>
        /// <param name="pluginFlags">
        /// The plugin flags that control how the assembly is loaded and
        /// verified.
        /// </param>
        /// <param name="verbose">
        /// Non-zero to include additional diagnostic information when an
        /// error is encountered.
        /// </param>
        /// <param name="error">
        /// Upon failure, this contains an error message that describes why
        /// the plugin data could not be previewed.
        /// </param>
        /// <returns>
        /// The extracted plugin data, or null if it could not be obtained.
        /// </returns>
        private static IPluginData PreviewPluginData(
            Interpreter interpreter, /* in */
            byte[] assemblyBytes,    /* in */
            string typeName,         /* in */
            PluginFlags pluginFlags, /* in */
            bool verbose,            /* in */
            ref Result error         /* out */
            )
        {
            string friendlyName = null;
            AppDomain appDomain = null;

            try
            {
                friendlyName = AppDomainOps.GetFriendlyName(
                    "preview", assemblyBytes, typeName, ref error);

                if (friendlyName == null)
                    return null;

                if (AppDomainOps.Create(
                        interpreter, friendlyName, null, true,
#if ISOLATED_PLUGINS
                        FlagOps.HasFlags(pluginFlags,
                            PluginFlags.VerifyCoreAssembly, true),
                        !FlagOps.HasFlags(pluginFlags,
                            PluginFlags.NoUseEntryAssembly, true),
                        FlagOps.HasFlags(pluginFlags,
                            PluginFlags.OptionalEntryAssembly, true),
#else
                        false, false, false,
#endif
                        ref appDomain, ref error) != ReturnCode.Ok)
                {
                    return null;
                }

                PluginLoaderFlags pluginLoaderFlags =
                    PluginLoaderFlags.Preview;

#if !NET_STANDARD_20
                CrossAppDomainDelegate @delegate = null;
#else
                GenericCallback @delegate = null;
#endif

                object helper = Interpreter.GetReflectionHelper(
                    CombineOrCopyTrustedHashes(
                        interpreter, false, false, false),
                    assemblyBytes, null, pluginLoaderFlags,
                    verbose, ref @delegate, ref error);

                if (helper == null)
                    return null;

                AppDomainOps.DoCallBack(appDomain, @delegate);

                return Interpreter.ExtractPluginData(helper, ref error);
            }
            catch (Exception e)
            {
                error = e;
            }
            finally
            {
                AppDomainOps.UnloadOrComplain(
                    interpreter, friendlyName, appDomain, null);
            }

            return null;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method previews the plugin flags and update URI for a plugin
        /// assembly that is identified by its file name.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter context to use when previewing the plugin data.
        /// </param>
        /// <param name="fileName">
        /// The file name of the assembly to be previewed.
        /// </param>
        /// <param name="typeName">
        /// The name of the plugin type to look for, or null to use the
        /// primary plugin type.
        /// </param>
        /// <param name="verbose">
        /// Non-zero to include additional diagnostic information when an
        /// error is encountered.
        /// </param>
        /// <param name="pluginFlags">
        /// Upon entry, the plugin flags that control how the assembly is
        /// loaded and verified.  Upon success, this contains the plugin
        /// flags reported by the previewed plugin.
        /// </param>
        /// <param name="pluginData">
        /// Upon success, this contains the plugin data that was extracted
        /// from the previewed assembly.
        /// </param>
        /// <param name="updateUri">
        /// Upon success, this contains the update URI reported by the
        /// previewed plugin.
        /// </param>
        /// <param name="error">
        /// Upon failure, this contains an error message that describes why
        /// the plugin flags and update URI could not be previewed.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise, an
        /// appropriate error code.
        /// </returns>
        public static ReturnCode PreviewPluginFlagsAndUpdateUri(
            Interpreter interpreter,     /* in */
            string fileName,             /* in */
            string typeName,             /* in */
            bool verbose,                /* in */
            ref PluginFlags pluginFlags, /* in, out */
            ref IPluginData pluginData,  /* out */
            ref Uri updateUri,           /* out */
            ref Result error             /* out */
            )
        {
            pluginData = PreviewPluginData(
                interpreter, fileName, typeName, pluginFlags,
                verbose, ref error);

            if (pluginData == null)
                return ReturnCode.Error;

            pluginFlags = pluginData.Flags;
            updateUri = pluginData.UpdateUri;

            return ReturnCode.Ok;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method previews the plugin flags and update URI for a plugin
        /// assembly that is provided as an in-memory array of bytes.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter context to use when previewing the plugin data.
        /// </param>
        /// <param name="assemblyBytes">
        /// The raw bytes of the assembly to be previewed.
        /// </param>
        /// <param name="typeName">
        /// The name of the plugin type to look for, or null to use the
        /// primary plugin type.
        /// </param>
        /// <param name="verbose">
        /// Non-zero to include additional diagnostic information when an
        /// error is encountered.
        /// </param>
        /// <param name="pluginFlags">
        /// Upon entry, the plugin flags that control how the assembly is
        /// loaded and verified.  Upon success, this contains the plugin
        /// flags reported by the previewed plugin.
        /// </param>
        /// <param name="pluginData">
        /// Upon success, this contains the plugin data that was extracted
        /// from the previewed assembly.
        /// </param>
        /// <param name="updateUri">
        /// Upon success, this contains the update URI reported by the
        /// previewed plugin.
        /// </param>
        /// <param name="error">
        /// Upon failure, this contains an error message that describes why
        /// the plugin flags and update URI could not be previewed.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise, an
        /// appropriate error code.
        /// </returns>
        public static ReturnCode PreviewPluginFlagsAndUpdateUri(
            Interpreter interpreter,     /* in */
            byte[] assemblyBytes,        /* in */
            string typeName,             /* in */
            bool verbose,                /* in */
            ref PluginFlags pluginFlags, /* in, out */
            ref IPluginData pluginData,  /* out */
            ref Uri updateUri,           /* out */
            ref Result error             /* out */
            )
        {
            pluginData = PreviewPluginData(
                interpreter, assemblyBytes, typeName, pluginFlags,
                verbose, ref error);

            if (pluginData == null)
                return ReturnCode.Error;

            pluginFlags = pluginData.Flags;
            updateUri = pluginData.UpdateUri;

            return ReturnCode.Ok;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method previews the resources contained within a plugin
        /// assembly that is identified by its file name by loading it into
        /// an isolated application domain, extracting its resource data, and
        /// then unloading that application domain.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter context to use when creating the temporary
        /// application domain and reflection helper.
        /// </param>
        /// <param name="fileName">
        /// The file name of the assembly to be previewed.
        /// </param>
        /// <param name="patterns">
        /// The list of patterns used to match the resource names to be
        /// returned, or null to match all resources.
        /// </param>
        /// <param name="pluginFlags">
        /// The plugin flags that control how the assembly is loaded and
        /// verified.
        /// </param>
        /// <param name="verbose">
        /// Non-zero to include additional diagnostic information when an
        /// error is encountered.
        /// </param>
        /// <param name="resources">
        /// Upon success, this contains the dictionary of resources that were
        /// extracted from the previewed assembly.
        /// </param>
        /// <param name="error">
        /// Upon failure, this contains an error message that describes why
        /// the plugin resources could not be previewed.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise, an
        /// appropriate error code.
        /// </returns>
        public static ReturnCode PreviewPluginResources(
            Interpreter interpreter,                /* in */
            string fileName,                        /* in */
            StringList patterns,                    /* in */
            PluginFlags pluginFlags,                /* in */
            bool verbose,                           /* in */
            ref PluginResourceDictionary resources, /* out */
            ref Result error                        /* out */
            )
        {
            string friendlyName = null;
            AppDomain appDomain = null;

            try
            {
                friendlyName = AppDomainOps.GetFriendlyName(
                    "preview", fileName, "resources", ref error);

                if (friendlyName == null)
                    return ReturnCode.Error;

                string packagePath = null;

                if (fileName != null)
                    packagePath = Path.GetDirectoryName(fileName);

                if (AppDomainOps.Create(
                        interpreter, friendlyName, packagePath, true,
#if ISOLATED_PLUGINS
                        FlagOps.HasFlags(pluginFlags,
                            PluginFlags.VerifyCoreAssembly, true),
                        !FlagOps.HasFlags(pluginFlags,
                            PluginFlags.NoUseEntryAssembly, true),
                        FlagOps.HasFlags(pluginFlags,
                            PluginFlags.OptionalEntryAssembly, true),
#else
                        false, false, false,
#endif
                        ref appDomain, ref error) != ReturnCode.Ok)
                {
                    return ReturnCode.Error;
                }

                PluginLoaderFlags pluginLoaderFlags =
                    PluginLoaderFlags.ResourcesOnly;

#if !NET_STANDARD_20
                CrossAppDomainDelegate @delegate = null;
#else
                GenericCallback @delegate = null;
#endif

                object helper = Interpreter.GetReflectionHelper(
                    CombineOrCopyTrustedHashes(
                        interpreter, false, false, false),
                    fileName, patterns, pluginLoaderFlags,
                    verbose, ref @delegate, ref error);

                if (helper == null)
                    return ReturnCode.Error;

                AppDomainOps.DoCallBack(appDomain, @delegate);

                return Interpreter.ExtractResourceData(
                    helper, ref resources, ref error);
            }
            catch (Exception e)
            {
                error = e;
            }
            finally
            {
                AppDomainOps.UnloadOrComplain(
                    interpreter, friendlyName, appDomain, null);
            }

            return ReturnCode.Error;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method previews the resources contained within a plugin
        /// assembly that is provided as an in-memory array of bytes by
        /// loading it into an isolated application domain, extracting its
        /// resource data, and then unloading that application domain.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter context to use when creating the temporary
        /// application domain and reflection helper.
        /// </param>
        /// <param name="assemblyBytes">
        /// The raw bytes of the assembly to be previewed.
        /// </param>
        /// <param name="patterns">
        /// The list of patterns used to match the resource names to be
        /// returned, or null to match all resources.
        /// </param>
        /// <param name="pluginFlags">
        /// The plugin flags that control how the assembly is loaded and
        /// verified.
        /// </param>
        /// <param name="verbose">
        /// Non-zero to include additional diagnostic information when an
        /// error is encountered.
        /// </param>
        /// <param name="resources">
        /// Upon success, this contains the dictionary of resources that were
        /// extracted from the previewed assembly.
        /// </param>
        /// <param name="error">
        /// Upon failure, this contains an error message that describes why
        /// the plugin resources could not be previewed.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise, an
        /// appropriate error code.
        /// </returns>
        public static ReturnCode PreviewPluginResources(
            Interpreter interpreter,                /* in */
            byte[] assemblyBytes,                   /* in */
            StringList patterns,                    /* in */
            PluginFlags pluginFlags,                /* in */
            bool verbose,                           /* in */
            ref PluginResourceDictionary resources, /* out */
            ref Result error                        /* out */
            )
        {
            string friendlyName = null;
            AppDomain appDomain = null;

            try
            {
                friendlyName = AppDomainOps.GetFriendlyName(
                    "preview", assemblyBytes, "resources", ref error);

                if (friendlyName == null)
                    return ReturnCode.Error;

                if (AppDomainOps.Create(
                        interpreter, friendlyName, null, true,
#if ISOLATED_PLUGINS
                        FlagOps.HasFlags(pluginFlags,
                            PluginFlags.VerifyCoreAssembly, true),
                        !FlagOps.HasFlags(pluginFlags,
                            PluginFlags.NoUseEntryAssembly, true),
                        FlagOps.HasFlags(pluginFlags,
                            PluginFlags.OptionalEntryAssembly, true),
#else
                        false, false, false,
#endif
                        ref appDomain, ref error) != ReturnCode.Ok)
                {
                    return ReturnCode.Error;
                }

                PluginLoaderFlags pluginLoaderFlags =
                    PluginLoaderFlags.ResourcesOnly;

#if !NET_STANDARD_20
                CrossAppDomainDelegate @delegate = null;
#else
                GenericCallback @delegate = null;
#endif

                object helper = Interpreter.GetReflectionHelper(
                    CombineOrCopyTrustedHashes(
                        interpreter, false, false, false),
                    assemblyBytes, patterns, pluginLoaderFlags,
                    verbose, ref @delegate, ref error);

                if (helper == null)
                    return ReturnCode.Error;

                AppDomainOps.DoCallBack(appDomain, @delegate);

                return Interpreter.ExtractResourceData(
                    helper, ref resources, ref error);
            }
            catch (Exception e)
            {
                error = e;
            }
            finally
            {
                AppDomainOps.UnloadOrComplain(
                    interpreter, friendlyName, appDomain, null);
            }

            return ReturnCode.Error;
        }
#endif

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method walks the chain of inner exceptions for the specified
        /// exception, adding each one to the specified list of errors.
        /// </summary>
        /// <param name="exception">
        /// The exception whose inner exceptions should be collected.  If this
        /// is null, no action is taken.
        /// </param>
        /// <param name="errors">
        /// Upon entry, the list of errors to add to, which may be null.  If
        /// it is null and there are inner exceptions to add, a new list is
        /// created.  Upon return, this contains any collected inner
        /// exceptions.
        /// </param>
        /// <returns>
        /// True if the specified exception was non-null; otherwise, false.
        /// </returns>
        private static bool MaybeGrabInnerExceptions(
            Exception exception,  /* in */
            ref ResultList errors /* in, out */
            )
        {
            if (exception == null)
                return false;

            int innerCount = 0;
            Exception innerException = exception.InnerException;

            while (innerException != null)
            {
                if (errors == null)
                    errors = new ResultList();

                errors.Add(String.Format(
                    "inner exception #{0}", innerCount + 1));

                errors.Add(innerException);

                innerException = innerException.InnerException;
                innerCount++;
            }

            return true;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method collects the loader exceptions associated with the
        /// specified exception when it is a
        /// <see cref="ReflectionTypeLoadException" />, adding each one and its
        /// inner exceptions to the specified list of errors.
        /// </summary>
        /// <param name="exception">
        /// The exception whose loader exceptions should be collected.  If this
        /// is null or is not a <see cref="ReflectionTypeLoadException" />, no
        /// action is taken.
        /// </param>
        /// <param name="errors">
        /// Upon entry, the list of errors to add to, which may be null.  If
        /// it is null and there are loader exceptions to add, a new list is
        /// created.  Upon return, this contains any collected loader
        /// exceptions.
        /// </param>
        /// <returns>
        /// True if loader exceptions were available to be collected;
        /// otherwise, false.
        /// </returns>
        private static bool MaybeGrabLoaderExceptions(
            Exception exception,  /* in */
            ref ResultList errors /* in, out */
            )
        {
            if (exception == null)
                return false;

            ReflectionTypeLoadException localException =
                exception as ReflectionTypeLoadException;

            if (localException == null)
                return false;

            Exception[] loaderExceptions = localException.LoaderExceptions;

            if (loaderExceptions == null)
                return false;

            int loaderCount = 0;

            foreach (Exception loaderException in loaderExceptions)
            {
                if (loaderException == null)
                    continue;

                if (errors == null)
                    errors = new ResultList();

                errors.Add(String.Format(
                    "loader exception #{0}", loaderCount + 1));

                errors.Add(loaderException);

                /* IGNORED */
                MaybeGrabInnerExceptions(loaderException, ref errors);

                loaderCount++;
            }

            return true;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method collects the specified exception, and optionally its
        /// inner and loader exceptions, adding each one to the specified list
        /// of errors.
        /// </summary>
        /// <param name="exception">
        /// The exception to collect.  If this is null, no action is taken.
        /// </param>
        /// <param name="verbose">
        /// Non-zero to also collect the inner and loader exceptions
        /// associated with the specified exception.
        /// </param>
        /// <param name="errors">
        /// Upon entry, the list of errors to add to, which may be null.  If
        /// it is null, a new list is created.  Upon return, this contains
        /// the collected exceptions.
        /// </param>
        /// <returns>
        /// True if the specified exception was non-null; otherwise, false.
        /// </returns>
        public static bool MaybeGrabExceptions(
            Exception exception,  /* in */
            bool verbose,         /* in */
            ref ResultList errors /* in, out */
            )
        {
            if (exception == null)
                return false;

            if (errors == null)
                errors = new ResultList();

            int outerCount = errors.Count;

            errors.Add(String.Format(
                "outer exception #{0}", outerCount + 1));

            errors.Add(exception);

            if (verbose)
            {
                /* IGNORED */
                MaybeGrabInnerExceptions(exception, ref errors);

                /* IGNORED */
                MaybeGrabLoaderExceptions(exception, ref errors);
            }

            return true;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method collects the specified exception, and optionally its
        /// inner and loader exceptions, and reports them via the diagnostic
        /// tracing subsystem.
        /// </summary>
        /// <param name="exception">
        /// The exception to collect and report.  If this is null, no
        /// exception is traced; however, any collected errors are still
        /// reported.
        /// </param>
        /// <param name="verbose">
        /// Non-zero to also collect the inner and loader exceptions
        /// associated with the specified exception.
        /// </param>
        /// <returns>
        /// True if at least one exception or error was reported; otherwise,
        /// false.
        /// </returns>
        public static bool MaybeGrabAndReportExceptions(
            Exception exception, /* in */
            bool verbose         /* in */
            )
        {
            int reportCount = 0;

            if (exception != null)
            {
                TraceOps.DebugTrace(
                    exception, typeof(RuntimeOps).Name,
                    TracePriority.InternalError3);

                reportCount++;
            }

            ResultList errors = null;

            /* IGNORED */
            MaybeGrabExceptions(exception, verbose, ref errors);

            if ((reportCount > 0) || (errors != null))
            {
                int newReportCount = reportCount;

                if (errors != null)
                    newReportCount++;

                TraceOps.DebugTrace(String.Format(
                    "MaybeGrabAndReportExceptions: verbose = {0}, " +
                    "count = {1}, errors = {2}", verbose, newReportCount,
                    FormatOps.WrapOrNull(errors)), typeof(RuntimeOps).Name,
                    TracePriority.InternalError2);

                reportCount = newReportCount;
            }

            return (reportCount > 0);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method gets the list of types defined by the core library
        /// assembly.
        /// </summary>
        /// <param name="verbose">
        /// Non-zero to include additional diagnostic information when an
        /// error is encountered.
        /// </param>
        /// <param name="types">
        /// Upon entry, the list of types to add to, which may be null.  If
        /// it is null, a new list is created.  Upon success, this contains
        /// the types defined by the core library assembly.
        /// </param>
        /// <param name="errors">
        /// Upon entry, the list of errors to add to, which may be null.
        /// Upon failure, this contains one or more error messages that
        /// describe why the types could not be obtained.
        /// </param>
        /// <returns>
        /// True if the types were obtained successfully; otherwise, false.
        /// </returns>
        public static bool GetTypes(
            bool verbose,         /* in */
            ref TypeList types,   /* in, out */
            ref ResultList errors /* in, out */
            )
        {
            return GetTypes(
                GlobalState.GetAssembly(), verbose, ref types, ref errors);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method gets the list of types defined by the specified
        /// assembly.
        /// </summary>
        /// <param name="assembly">
        /// The assembly whose types should be obtained.  If this is null, an
        /// error is reported.
        /// </param>
        /// <param name="verbose">
        /// Non-zero to include additional diagnostic information when an
        /// error is encountered.
        /// </param>
        /// <param name="types">
        /// Upon entry, the list of types to add to, which may be null.  If
        /// it is null, a new list is created.  Upon success, this contains
        /// the types defined by the specified assembly.
        /// </param>
        /// <param name="errors">
        /// Upon entry, the list of errors to add to, which may be null.
        /// Upon failure, this contains one or more error messages that
        /// describe why the types could not be obtained.
        /// </param>
        /// <returns>
        /// True if the types were obtained successfully; otherwise, false.
        /// </returns>
        private static bool GetTypes(
            Assembly assembly,    /* in */
            bool verbose,         /* in */
            ref TypeList types,   /* in, out */
            ref ResultList errors /* in, out */
            )
        {
            try
            {
                if (assembly == null)
                {
                    if (errors == null)
                        errors = new ResultList();

                    errors.Add("invalid assembly");
                    return false;
                }

                Type[] localTypes = assembly.GetTypes(); /* throw */

                if (localTypes == null)
                {
                    if (errors == null)
                        errors = new ResultList();

                    errors.Add("invalid types");
                    return false;
                }

                int length = localTypes.Length;

                if (length == 0)
                {
                    if (errors == null)
                        errors = new ResultList();

                    errors.Add("no types");
                    return false;
                }

                if (types == null)
                    types = new TypeList(length);

                types.AddRange(localTypes);
                return true;
            }
            catch (Exception e)
            {
                /* IGNORED */
                MaybeGrabExceptions(
                    e, verbose || VerboseExceptions, ref errors);
            }

            return false;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method searches the specified assembly for the type name of
        /// its primary plugin, which is the plugin type marked with the
        /// <see cref="PluginFlags.Primary" /> flag.
        /// </summary>
        /// <param name="assembly">
        /// The assembly to search for the primary plugin.  If this is null,
        /// an error is reported.
        /// </param>
        /// <param name="verbose">
        /// Non-zero to include additional diagnostic information when an
        /// error is encountered.
        /// </param>
        /// <param name="typeName">
        /// Upon success, this contains the full name of the primary plugin
        /// type found in the specified assembly.
        /// </param>
        /// <param name="error">
        /// Upon failure, this contains an error message that describes why
        /// the primary plugin could not be found.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise, an
        /// appropriate error code.
        /// </returns>
        public static ReturnCode FindPrimaryPlugin(
            Assembly assembly,
            bool verbose,
            ref string typeName,
            ref Result error
            )
        {
            if (assembly == null)
            {
                error = "invalid assembly";
                return ReturnCode.Error;
            }

            TypeList types = null;
            ResultList errors = null;

            if (!GetTypes(
                    assembly, verbose, ref types, ref errors))
            {
                error = errors;
                return ReturnCode.Error;
            }

            TypeList matchingTypes = null;

            if (!GetMatchingClassTypes(
                    types, typeof(IPlugin), typeof(IWrapper),
                    true, verbose, ref matchingTypes, ref errors))
            {
                errors.Insert(0,
                    "no plugins found in assembly");

                error = errors;
                return ReturnCode.Error;
            }

            typeName = null;

            foreach (Type type in matchingTypes)
            {
                if (type == null)
                    continue;

                //
                // NOTE: Is the plugin named "Default"?  If so, we need to
                //       skip over it because it is used as the base class
                //       for other plugins.
                //
                if (SharedStringOps.SystemEquals(
                        type.FullName, typeof(_Plugins.Default).FullName))
                {
                    continue;
                }

                PluginFlags flags;

                if (assembly.ReflectionOnly)
                    flags = AttributeOps.GetReflectionOnlyPluginFlags(type);
                else
                    flags = AttributeOps.GetPluginFlags(type);

                if (FlagOps.HasFlags(flags, PluginFlags.Primary, true))
                {
                    typeName = type.FullName;
                    return ReturnCode.Ok;
                }
            }

            error = "no primary plugin found in assembly";
            return ReturnCode.Error;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method determines whether the object identifier associated
        /// with the specified type matches the specified identifier.
        /// </summary>
        /// <param name="type">
        /// The type whose object identifier should be compared.  If this is
        /// null, no match is possible.
        /// </param>
        /// <param name="matchId">
        /// The object identifier to compare against the object identifier of
        /// the specified type.
        /// </param>
        /// <returns>
        /// True if the specified type has a defined object identifier that
        /// matches the specified identifier; otherwise, false.
        /// </returns>
        public static bool DoesTypeMatchId(
            Type type,
            Guid matchId
            )
        {
            if (type != null)
            {
                Guid id;
                bool defined = false;

                id = AttributeOps.GetObjectId(type, ref defined);

                if (defined && matchId.Equals(id))
                    return true;
            }

            return false;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method searches the specified assembly for the type whose
        /// object identifier matches the specified identifier.
        /// </summary>
        /// <param name="assembly">
        /// The assembly to search for the matching type.
        /// </param>
        /// <param name="id">
        /// The object identifier to match.  If this is
        /// <see cref="Guid.Empty" />, the <see cref="Type" /> type itself is
        /// returned.
        /// </param>
        /// <param name="nonPublic">
        /// Non-zero to also consider non-public types when searching;
        /// otherwise, only public types are considered.
        /// </param>
        /// <param name="verbose">
        /// Non-zero to include additional diagnostic information when an
        /// error is encountered.
        /// </param>
        /// <param name="type">
        /// Upon success, this contains the type whose object identifier
        /// matches the specified identifier.
        /// </param>
        /// <param name="errors">
        /// Upon entry, the list of errors to add to, which may be null.
        /// Upon failure, this contains one or more error messages that
        /// describe why the matching type could not be found.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise, an
        /// appropriate error code.
        /// </returns>
        private static ReturnCode FindTypeById(
            Assembly assembly,
            Guid id,
            bool nonPublic,
            bool verbose,
            ref Type type,
            ref ResultList errors
            )
        {
            try
            {
                if (id.Equals(Guid.Empty))
                {
                    type = typeof(Type); /* META */
                    return ReturnCode.Ok;
                }

                TypeList types = null;

                if (!GetTypes(
                        assembly, verbose, ref types, ref errors))
                {
                    return ReturnCode.Error;
                }

                foreach (Type localType in types)
                {
                    if (localType == null)
                        continue;

                    if (!nonPublic && !localType.IsPublic)
                        continue;

                    if (DoesTypeMatchId(localType, id))
                    {
                        type = localType;
                        return ReturnCode.Ok;
                    }
                }

                if (errors == null)
                    errors = new ResultList();

                errors.Add(String.Format(
                    "missing {0} type matching Id {1}", nonPublic ?
                    "any" : "public", FormatOps.WrapOrNull(id)));

                return ReturnCode.Error;
            }
            catch (Exception e)
            {
                /* IGNORED */
                MaybeGrabExceptions(
                    e, verbose || VerboseExceptions, ref errors);
            }

            return ReturnCode.Error;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method gets framework version information for the specified
        /// assembly and/or type, according to the specified framework flags.
        /// </summary>
        /// <param name="assembly">
        /// The assembly to query for framework information.  If this is null,
        /// an error is reported.
        /// </param>
        /// <param name="id">
        /// The optional object identifier of the type within the assembly to
        /// query, or null to query the assembly itself.
        /// </param>
        /// <param name="flags">
        /// The framework flags that control which sources of framework
        /// information are consulted and how the result is formatted.
        /// </param>
        /// <param name="result">
        /// Upon success, this contains the framework information; upon
        /// failure, this contains an error message that describes why the
        /// framework information could not be obtained.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise, an
        /// appropriate error code.
        /// </returns>
        public static ReturnCode GetFramework(
            Assembly assembly,
            Guid? id,
            FrameworkFlags flags,
            ref Result result
            )
        {
            if (assembly == null)
            {
                result = "invalid assembly";
                return ReturnCode.Error;
            }

            ResultList errors = null;

            bool builtIn = FlagOps.HasFlags(
                flags, FrameworkFlags.BuiltIn, true);

            bool external = FlagOps.HasFlags(
                flags, FrameworkFlags.External, true);

            bool verbose = FlagOps.HasFlags(
                flags, FrameworkFlags.Verbose, true);

            if (Object.ReferenceEquals(
                    assembly, GlobalState.GetAssembly()))
            {
                int errorCount = 0;

                if (!builtIn)
                {
                    if (verbose)
                    {
                        if (errors == null)
                            errors = new ResultList();

                        errors.Add(String.Format(
                            "flag {0} required for core library assembly",
                            FormatOps.WrapOrNull(FrameworkFlags.BuiltIn)));
                    }

                    errorCount++;
                }

                if (external)
                {
                    if (verbose)
                    {
                        if (errors == null)
                            errors = new ResultList();

                        errors.Add(String.Format(
                            "flag {0} forbidden for core library assembly",
                            FormatOps.WrapOrNull(FrameworkFlags.External)));
                    }

                    errorCount++;
                }

                if (errorCount > 0)
                    goto done;
            }
            else
            {
                int errorCount = 0;

                if (builtIn)
                {
                    if (verbose)
                    {
                        if (errors == null)
                            errors = new ResultList();

                        errors.Add(String.Format(
                            "flag {0} forbidden for external assembly",
                            FormatOps.WrapOrNull(FrameworkFlags.BuiltIn)));
                    }

                    errorCount++;
                }

                if (!external)
                {
                    if (verbose)
                    {
                        if (errors == null)
                            errors = new ResultList();

                        errors.Add(String.Format(
                            "flag {0} required for external assembly",
                            FormatOps.WrapOrNull(FrameworkFlags.External)));
                    }

                    errorCount++;
                }

                if (errorCount > 0)
                    goto done;
            }

            bool nonPublic = FlagOps.HasFlags(
                flags, FrameworkFlags.NonPublic, true);

            bool instance = FlagOps.HasFlags(
                flags, FrameworkFlags.Instance, true);

            bool @static = FlagOps.HasFlags(
                flags, FrameworkFlags.Static, true);

            if (id != null)
            {
                Type type = null;

                if (FindTypeById(
                        assembly, (Guid)id, nonPublic, verbose, ref type,
                        ref errors) == ReturnCode.Ok)
                {
                    if (instance)
                    {
                        if (type != typeof(Type))
                        {
                            try
                            {
                                result = String.Empty; /* CANNOT BE NULL */

                                result.Value = Activator.CreateInstance(
                                    type, nonPublic); /* throw */

                                return ReturnCode.Ok;
                            }
                            catch (Exception e)
                            {
                                if (verbose)
                                {
                                    if (errors == null)
                                        errors = new ResultList();

                                    errors.Add(e);
                                }
                            }
                        }
                        else
                        {
                            if (verbose)
                            {
                                if (errors == null)
                                    errors = new ResultList();

                                errors.Add(String.Format(
                                    "cannot create an instance of {0}",
                                    FormatOps.TypeName(typeof(Type))));
                            }
                        }
                    }
                    else if (@static)
                    {
                        result = String.Empty; /* CANNOT BE NULL */
                        result.Value = type;

                        return ReturnCode.Ok;
                    }
                }
            }
#if TEST
            else
            {
                bool test = FlagOps.HasFlags(
                    flags, FrameworkFlags.Test, true);

                if (test)
                {
                    if (instance)
                    {
                        result = String.Empty; /* CANNOT BE NULL */
                        result.Value = new _Tests.Default();

                        return ReturnCode.Ok;
                    }
                    else if (@static)
                    {
                        result = String.Empty; /* CANNOT BE NULL */
                        result.Value = typeof(_Tests.Default);

                        return ReturnCode.Ok;
                    }
                }
            }
#endif

        done:

            if (errors == null)
                errors = new ResultList();

            errors.Insert(0, String.Format(
                "framework with Id {0} and flags {1} within assembly " +
                "{2} not available", FormatOps.WrapOrNull(id),
                FormatOps.WrapOrNull(flags), FormatOps.WrapOrNull(
                assembly.GetName())));

            result = errors;
            return ReturnCode.Error;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method filters a collection of types, selecting those class or
        /// value types that match a required type and do not match an excluded
        /// type.
        /// </summary>
        /// <param name="types">
        /// The collection of types to examine.  If this parameter is null, the
        /// method fails.
        /// </param>
        /// <param name="matchType">
        /// The type that a candidate type must match in order to be selected.
        /// This parameter may be null, in which case no positive matching is
        /// performed.
        /// </param>
        /// <param name="nonMatchType">
        /// The type that a candidate type must not match in order to be
        /// selected.  This parameter may be null, in which case no negative
        /// matching is performed.
        /// </param>
        /// <param name="subClass">
        /// Non-zero to also consider sub-classes when matching against
        /// <paramref name="matchType" /> and <paramref name="nonMatchType" />.
        /// </param>
        /// <param name="verbose">
        /// Non-zero to include verbose exception information in the
        /// <paramref name="errors" /> collection.
        /// </param>
        /// <param name="matchingTypes">
        /// Upon success, this receives the list of matching types.  If this
        /// parameter is null, a new list is created.
        /// </param>
        /// <param name="errors">
        /// Upon failure, this receives one or more error messages.  If this
        /// parameter is null, a new list is created when an error is recorded.
        /// </param>
        /// <returns>
        /// True if the types were examined successfully; otherwise, false.
        /// </returns>
        private static bool GetMatchingClassTypes(
            IEnumerable<Type> types,
            Type matchType,    // must match this type
            Type nonMatchType, // must not match this type
            bool subClass,     // check sub-classes also (for not match)
            bool verbose,
            ref TypeList matchingTypes,
            ref ResultList errors
            )
        {
            try
            {
                if (types == null)
                {
                    if (errors == null)
                        errors = new ResultList();

                    errors.Add("invalid types");

                    return false;
                }

                if (matchingTypes == null)
                    matchingTypes = new TypeList();

                foreach (Type type in types)
                {
                    if (type == null)
                        continue;

                    if (!type.IsClass && !type.IsValueType)
                        continue;

                    if ((matchType != null) &&
                        !DoesClassTypeMatch(type, matchType, subClass))
                    {
                        continue;
                    }

                    if ((nonMatchType != null) &&
                        DoesClassTypeMatch(type, nonMatchType, subClass))
                    {
                        continue;
                    }

                    matchingTypes.Add(type);
                }

                return true;
            }
            catch (Exception e)
            {
                /* IGNORED */
                MaybeGrabExceptions(
                    e, verbose || VerboseExceptions, ref errors);
            }

            return false;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method scans a collection of types for methods that can be
        /// converted into delegates of an expected type and whose associated
        /// method flags satisfy the specified inclusion and exclusion criteria.
        /// </summary>
        /// <param name="types">
        /// The collection of types whose methods are examined.
        /// </param>
        /// <param name="matchType">
        /// The delegate type that each candidate method must be convertible to.
        /// </param>
        /// <param name="hasFlags">
        /// The method flags that a candidate method must have in order to be
        /// included.
        /// </param>
        /// <param name="notHasFlags">
        /// The method flags that a candidate method must not have in order to
        /// be included.
        /// </param>
        /// <param name="hasAll">
        /// Non-zero to require all of the <paramref name="hasFlags" /> to be
        /// present; zero to require any of them.
        /// </param>
        /// <param name="notHasAll">
        /// Non-zero to require all of the <paramref name="notHasFlags" /> to be
        /// present before excluding a method; zero to exclude when any of them
        /// are present.
        /// </param>
        /// <param name="verbose">
        /// Non-zero to include verbose exception information in the
        /// <paramref name="errors" /> collection.
        /// </param>
        /// <param name="delegates">
        /// Upon success, this receives the created delegates mapped to their
        /// associated method flags.  If this parameter is null, a new
        /// dictionary is created.
        /// </param>
        /// <param name="errors">
        /// Upon failure, this receives one or more error messages.  If this
        /// parameter is null, a new list is created when an error is recorded.
        /// </param>
        /// <returns>
        /// True if the types were examined successfully; otherwise, false.
        /// </returns>
        private static bool GetMatchingDelegates(
            IEnumerable<Type> types,
            Type matchType,          // the delegate type we are expecting
            MethodFlags hasFlags,    // must match flag(s)
            MethodFlags notHasFlags, // must not match flag(s)
            bool hasAll,
            bool notHasAll,
            bool verbose,
            ref Dictionary<Delegate, MethodFlags> delegates,
            ref ResultList errors
            )
        {
            try
            {
                BindingFlags bindingFlags = ObjectOps.GetBindingFlags(
                    MetaBindingFlags.Delegate, true);

                if (delegates == null)
                    delegates = new Dictionary<Delegate, MethodFlags>();

                foreach (Type type in types)
                {
                    if (type == null)
                        continue;

                    if (!type.IsClass && !type.IsValueType)
                        continue;

                    MethodInfo[] methodInfo = type.GetMethods(
                        bindingFlags);

                    foreach (MethodInfo thisMethodInfo in methodInfo)
                    {
                        if (thisMethodInfo == null)
                            continue;

                        MethodFlags methodFlags =
                            AttributeOps.GetMethodFlags(thisMethodInfo);

                        if (!FlagOps.HasFlags(
                                methodFlags, hasFlags, hasAll) ||
                            FlagOps.HasFlags(
                                methodFlags, notHasFlags, notHasAll))
                        {
                            continue;
                        }

                        Delegate @delegate = Delegate.CreateDelegate(
                            matchType, null, thisMethodInfo, false);

                        if (@delegate != null)
                        {
                            delegates[@delegate] = methodFlags;
                        }
                        else
                        {
                            //
                            // NOTE: This is not strictly an "error";
                            //       however, report it to the caller
                            //       anyhow.
                            //
                            if (errors == null)
                                errors = new ResultList();

                            errors.Add(String.Format(
                                "could not convert method {0} " +
                                "to a delegate of type {1}",
                                FormatOps.WrapOrNull(
                                    FormatOps.MethodFullName(
                                        thisMethodInfo.DeclaringType,
                                        thisMethodInfo.Name)),
                                FormatOps.WrapOrNull(
                                    matchType.FullName)));
                        }
                    }
                }

                return true;
            }
            catch (Exception e)
            {
                /* IGNORED */
                MaybeGrabExceptions(
                    e, verbose || VerboseExceptions, ref errors);
            }

            return false;
        }

        ///////////////////////////////////////////////////////////////////////

#if EMIT
        /// <summary>
        /// This method collects the methods of a type that are eligible to be
        /// turned into delegates, honoring the visibility and binding criteria
        /// expressed by the specified delegate flags.
        /// </summary>
        /// <param name="type">
        /// The type whose methods are collected.
        /// </param>
        /// <param name="delegateFlags">
        /// The flags controlling which methods are collected (e.g. public,
        /// non-public, instance, and/or static) and how failures are reported.
        /// </param>
        /// <param name="methodInfoList">
        /// Upon success, this receives the collected methods.  If this
        /// parameter is null, a new list is created; otherwise, the collected
        /// methods are appended.
        /// </param>
        /// <param name="errors">
        /// Upon failure, this receives one or more error messages.  If this
        /// parameter is null, a new list is created when an error is recorded.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise, an appropriate
        /// error code.
        /// </returns>
        public static ReturnCode GetDelegateMethods(
            Type type,
            DelegateFlags delegateFlags,
            ref MethodInfoList methodInfoList,
            ref ResultList errors
            )
        {
            try
            {
                MethodInfoList localMethodInfoList = null;

                if (FlagOps.HasFlags(
                        delegateFlags, DelegateFlags.Public, true))
                {
                    if (FlagOps.HasFlags(
                            delegateFlags, DelegateFlags.Instance, true))
                    {
                        if (localMethodInfoList == null)
                            localMethodInfoList = new MethodInfoList();

                        localMethodInfoList.AddRange(type.GetMethods(
                            ObjectOps.GetBindingFlags(
                                MetaBindingFlags.PublicInstanceMethod,
                                true))); /* throw */
                    }

                    if (FlagOps.HasFlags(
                            delegateFlags, DelegateFlags.Static, true))
                    {
                        if (localMethodInfoList == null)
                            localMethodInfoList = new MethodInfoList();

                        localMethodInfoList.AddRange(type.GetMethods(
                            ObjectOps.GetBindingFlags(
                                MetaBindingFlags.PublicStaticMethod,
                                true))); /* throw */
                    }
                }

                if (FlagOps.HasFlags(
                        delegateFlags, DelegateFlags.NonPublic, true))
                {
                    if (FlagOps.HasFlags(
                            delegateFlags, DelegateFlags.Instance, true))
                    {
                        if (localMethodInfoList == null)
                            localMethodInfoList = new MethodInfoList();

                        localMethodInfoList.AddRange(type.GetMethods(
                            ObjectOps.GetBindingFlags(
                                MetaBindingFlags.PrivateInstanceMethod,
                                true))); /* throw */
                    }

                    if (FlagOps.HasFlags(
                            delegateFlags, DelegateFlags.Static, true))
                    {
                        if (localMethodInfoList == null)
                            localMethodInfoList = new MethodInfoList();

                        localMethodInfoList.AddRange(type.GetMethods(
                            ObjectOps.GetBindingFlags(
                                MetaBindingFlags.PrivateStaticMethod,
                                true))); /* throw */
                    }
                }

                if (localMethodInfoList == null)
                {
                    if (FlagOps.HasFlags(
                            delegateFlags, DelegateFlags.FailOnNone, true))
                    {
                        if (errors == null)
                            errors = new ResultList();

                        errors.Add("no matching methods found");

                        return ReturnCode.Error;
                    }

                    return ReturnCode.Ok;
                }

                if (methodInfoList != null)
                    methodInfoList.AddRange(localMethodInfoList);
                else
                    methodInfoList = localMethodInfoList;

                return ReturnCode.Ok;
            }
            catch (Exception e)
            {
                /* IGNORED */
                MaybeGrabExceptions(
                    e, FlagOps.HasFlags(delegateFlags,
                    DelegateFlags.Verbose, true) || VerboseExceptions,
                    ref errors);
            }

            return ReturnCode.Error;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method determines whether a method cannot be used as the basis
        /// for a delegate because it has open generic parameters or is not CLS
        /// compliant.
        /// </summary>
        /// <param name="methodInfo">
        /// The method to examine.  This parameter may be null.
        /// </param>
        /// <returns>
        /// True if the method is unsupported; otherwise, false.
        /// </returns>
        private static bool IsUnsupportedMethod(
            MethodInfo methodInfo
            )
        {
            if (methodInfo != null)
            {
                if (methodInfo.ContainsGenericParameters)
                    return true;

                bool? clsCompliant = AttributeOps.GetClsCompliant(
                    methodInfo);

                if ((clsCompliant != null) && !(bool)clsCompliant)
                    return true;
            }

            return false;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method determines whether any of the specified parameters have
        /// a type that cannot be supported, such as a parameter array or a
        /// pointer type.
        /// </summary>
        /// <param name="parameterInfos">
        /// The array of parameters to examine.  This parameter may be null.
        /// </param>
        /// <returns>
        /// True if any parameter has an unsupported type; otherwise, false.
        /// </returns>
        private static bool HasUnsupportedParameterType(
            ParameterInfo[] parameterInfos
            )
        {
            if (parameterInfos != null)
            {
                foreach (ParameterInfo parameterInfo in parameterInfos)
                {
                    if (parameterInfo == null)
                        continue;

                    if (parameterInfo.IsDefined(
                            typeof(ParamArrayAttribute), false))
                    {
                        return true;
                    }

                    Type type = parameterInfo.ParameterType;

                    if ((type != null) && type.IsPointer)
                        return true;
                }
            }

            return false;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method computes the name to use for a delegate created from the
        /// specified method, appending the parameter count when the method name
        /// is overloaded within the existing delegate collection.
        /// </summary>
        /// <param name="delegates">
        /// The existing collection of delegates, used to detect overloaded
        /// method names.  This parameter may be null.
        /// </param>
        /// <param name="methodInfo">
        /// The method for which a delegate name is being computed.  If this
        /// parameter is null, a null name is returned.
        /// </param>
        /// <param name="parameterInfo">
        /// The parameters of the method, used to disambiguate overloaded
        /// names.  This parameter may be null.
        /// </param>
        /// <returns>
        /// The computed delegate name, or null if <paramref name="methodInfo" />
        /// is null.
        /// </returns>
        private static string GetDelegateName(
            DelegateDictionary delegates,
            MethodInfo methodInfo,
            ParameterInfo[] parameterInfo
            )
        {
            if (methodInfo == null)
                return null;

            string methodName = methodInfo.Name;
            StringBuilder builder = StringBuilderFactory.Create();

            builder.Append(methodName);

            if ((parameterInfo != null) &&
                (delegates != null) &&
                delegates.ContainsKey(methodName))
            {
                builder.AppendFormat(
                    "{0}{1}", Characters.Underscore,
                    parameterInfo.Length);
            }

            return StringBuilderCache.GetStringAndRelease(ref builder);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method computes the name to use for a delegate created from the
        /// specified method, delegating to the supplied callback when one is
        /// provided and falling back to the default naming scheme otherwise.
        /// </summary>
        /// <param name="nameCallback">
        /// The optional callback used to compute the delegate name.  This
        /// parameter may be null, in which case the default naming scheme is
        /// used.
        /// </param>
        /// <param name="delegates">
        /// The existing collection of delegates, used to detect overloaded
        /// method names.  This parameter may be null.
        /// </param>
        /// <param name="methodInfo">
        /// The method for which a delegate name is being computed.
        /// </param>
        /// <param name="parameterInfo">
        /// The parameters of the method, used to disambiguate overloaded
        /// names.  This parameter may be null.
        /// </param>
        /// <param name="clientData">
        /// The optional caller-specific data passed to the callback.  This
        /// parameter may be null.
        /// </param>
        /// <returns>
        /// The computed delegate name, or null if the method is null.
        /// </returns>
        private static string GetDelegateName(
            NewDelegateNameCallback nameCallback,
            DelegateDictionary delegates,
            MethodInfo methodInfo,
            ParameterInfo[] parameterInfo,
            IClientData clientData
            )
        {
            if (nameCallback != null)
            {
                try
                {
                    return nameCallback(
                        delegates, methodInfo,
                        clientData); /* throw */
                }
                catch (Exception e)
                {
                    TraceOps.DebugTrace(
                        e, typeof(RuntimeOps).Name,
                        TracePriority.MarshalError);
                }
            }

            return GetDelegateName(
                delegates, methodInfo, parameterInfo);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method creates delegates for the eligible methods in the
        /// specified list, binding each to the supplied target object and
        /// adding the resulting delegates to the delegate collection.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter context used when constructing managed delegate
        /// types.  This parameter may be null.
        /// </param>
        /// <param name="type">
        /// The type that declares the methods being bound.  If this parameter
        /// is null, the method fails.
        /// </param>
        /// <param name="object">
        /// The target object to bind instance methods to, or null for static
        /// methods.
        /// </param>
        /// <param name="methodInfoList">
        /// The list of methods to convert into delegates.  If this parameter
        /// is null, the method fails.
        /// </param>
        /// <param name="nameCallback">
        /// The optional callback used to compute each delegate name.  This
        /// parameter may be null.
        /// </param>
        /// <param name="clientData">
        /// The optional caller-specific data passed to the name callback.  This
        /// parameter may be null.
        /// </param>
        /// <param name="delegateFlags">
        /// The flags controlling delegate creation, including how duplicates
        /// are handled and how failures are reported.
        /// </param>
        /// <param name="delegates">
        /// Upon success, this receives the created delegates keyed by name.  If
        /// this parameter is null, a new dictionary is created.
        /// </param>
        /// <param name="errors">
        /// Upon failure, this receives one or more error messages.  If this
        /// parameter is null, a new list is created when an error is recorded.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> if no errors were encountered;
        /// otherwise, an appropriate error code.
        /// </returns>
        public static ReturnCode CreateDelegates(
            Interpreter interpreter,
            Type type,
            object @object,
            MethodInfoList methodInfoList,
            NewDelegateNameCallback nameCallback,
            IClientData clientData,
            DelegateFlags delegateFlags,
            ref DelegateDictionary delegates,
            ref ResultList errors
            )
        {
            if (type == null)
            {
                if (errors == null)
                    errors = new ResultList();

                errors.Add("invalid type");
                return ReturnCode.Error;
            }

            if (methodInfoList == null)
            {
                if (errors == null)
                    errors = new ResultList();

                errors.Add("invalid method list");
                return ReturnCode.Error;
            }

            int errorCount = 0;

            foreach (MethodInfo methodInfo in methodInfoList)
            {
                if ((methodInfo == null) ||
                    IsUnsupportedMethod(methodInfo))
                {
                    continue;
                }

                try
                {
                    ParameterInfo[] parameterInfo =
                        methodInfo.GetParameters();

                    if ((parameterInfo == null) ||
                        HasUnsupportedParameterType(parameterInfo))
                    {
                        continue;
                    }

                    string delegateName = GetDelegateName(
                        nameCallback, delegates, methodInfo,
                        parameterInfo, clientData);

                    if (delegateName == null)
                        continue;

                    if ((delegates != null) &&
                        delegates.ContainsKey(delegateName))
                    {
                        if (!FlagOps.HasFlags(delegateFlags,
                                DelegateFlags.AllowDuplicate, true))
                        {
                            if (errors == null)
                                errors = new ResultList();

                            errors.Add(String.Format(
                                "delegate {0} already exists",
                                FormatOps.WrapOrNull(delegateName)));

                            if (!FlagOps.HasFlags(delegateFlags,
                                    DelegateFlags.NoComplain, true))
                            {
                                errorCount++;
                            }

                            continue;
                        }

                        if (!FlagOps.HasFlags(delegateFlags,
                                DelegateFlags.OverwriteExisting, true))
                        {
                            continue;
                        }
                    }

                    TypeList parameterTypes = null;
                    Result parameterError = null;

                    if (MarshalOps.GetTypeListFromParameterInfo(
                            parameterInfo, false, ref parameterTypes,
                            ref parameterError) != ReturnCode.Ok)
                    {
                        if (FlagOps.HasFlags(delegateFlags,
                                DelegateFlags.Verbose, true))
                        {
                            if (errors == null)
                                errors = new ResultList();

                            errors.Add(parameterError);
                        }

                        if (!FlagOps.HasFlags(delegateFlags,
                                DelegateFlags.NoComplain, true))
                        {
                            errorCount++;
                        }

                        continue;
                    }

                    Type delegateType = null;
                    Result delegateError = null;

                    if (DelegateOps.CreateManagedDelegateType(
                            interpreter, null, null, null, null,
                            methodInfo.ReturnType, parameterTypes,
                            ref delegateType,
                            ref delegateError) != ReturnCode.Ok)
                    {
                        if (FlagOps.HasFlags(delegateFlags,
                                DelegateFlags.Verbose, true))
                        {
                            if (errors == null)
                                errors = new ResultList();

                            errors.Add(delegateError);
                        }

                        if (!FlagOps.HasFlags(delegateFlags,
                                DelegateFlags.NoComplain, true))
                        {
                            errorCount++;
                        }

                        continue;
                    }

                    Delegate @delegate = Delegate.CreateDelegate(
                        delegateType, @object, methodInfo, true); /* throw */

                    if (@delegate == null)
                    {
                        if (FlagOps.HasFlags(delegateFlags,
                                DelegateFlags.Verbose, true))
                        {
                            if (errors == null)
                                errors = new ResultList();

                            errors.Add(String.Format(
                                "could not create delegate " +
                                "from type {0} and method {1}",
                                FormatOps.TypeName(type),
                                FormatOps.MemberName(methodInfo)));
                        }

                        if (!FlagOps.HasFlags(delegateFlags,
                                DelegateFlags.NoComplain, true))
                        {
                            errorCount++;
                        }

                        continue;
                    }

                    if (delegates == null)
                        delegates = new DelegateDictionary();

                    delegates[delegateName] = @delegate;
                }
                catch (Exception e)
                {
                    if (FlagOps.HasFlags(delegateFlags,
                            DelegateFlags.Verbose, true))
                    {
                        if (errors == null)
                            errors = new ResultList();

                        errors.Add(e);
                    }

                    if (!FlagOps.HasFlags(delegateFlags,
                            DelegateFlags.NoComplain, true))
                    {
                        errorCount++;
                    }
                }
            }

            return (errorCount > 0) ? ReturnCode.Error : ReturnCode.Ok;
        }
#endif

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method searches the commands of a plugin for the command data
        /// whose type name matches the full name of the specified type.
        /// </summary>
        /// <param name="pluginData">
        /// The plugin data whose commands are searched.  If this parameter is
        /// null, a null reference is returned.
        /// </param>
        /// <param name="type">
        /// The type whose full name is used to match the command data.
        /// </param>
        /// <returns>
        /// The matching <see cref="ICommandData" />, or null if no match is
        /// found.
        /// </returns>
        public static ICommandData FindCommandData(
            IPluginData pluginData,
            Type type
            )
        {
            if (pluginData == null)
                return null;

            CommandDataList commands = pluginData.Commands;

            if (commands == null)
                return null;

            foreach (ICommandData commandData in commands)
            {
                if (commandData == null)
                    continue;

                if (SharedStringOps.SystemEquals(
                        commandData.TypeName, type.FullName))
                {
                    return commandData;
                }
            }

            return null;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method determines whether the specified type represents one of
        /// the special internal command implementations that are not treated as
        /// ordinary commands.
        /// </summary>
        /// <param name="type">
        /// The type to examine.
        /// </param>
        /// <returns>
        /// True if the type is one of the special internal command types;
        /// otherwise, false.
        /// </returns>
        private static bool IsReallyNonCommand(
            Type type
            )
        {
            if ((type == typeof(_Commands.Default)) ||
                (type == typeof(_Commands._Delegate)) ||
#if EMIT
                (type == typeof(_Commands.SubDelegate)) ||
                (type == typeof(_Commands.Automatic)) ||
#endif
                (type == typeof(_Commands.Ensemble)) ||
                (type == typeof(_Commands.Core)) ||
                (type == typeof(_Commands.Stub)) ||
                (type == typeof(_Commands.Alias)))
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method determines whether the specified type name matches the
        /// full name of one of the special internal command implementations
        /// that are not treated as ordinary commands.
        /// </summary>
        /// <param name="typeName">
        /// The type name to examine.
        /// </param>
        /// <returns>
        /// True if the type name matches one of the special internal command
        /// types; otherwise, false.
        /// </returns>
        public static bool IsReallyNonCommandName(
            string typeName
            )
        {
            if (SharedStringOps.SystemEquals(
                    typeName, typeof(_Commands.Default).FullName) ||
                SharedStringOps.SystemEquals(
                    typeName, typeof(_Commands._Delegate).FullName) ||
#if EMIT
                SharedStringOps.SystemEquals(
                    typeName, typeof(_Commands.SubDelegate).FullName) ||
                SharedStringOps.SystemEquals(
                    typeName, typeof(_Commands.Automatic).FullName) ||
#endif
                SharedStringOps.SystemEquals(
                    typeName, typeof(_Commands.Ensemble).FullName) ||
                SharedStringOps.SystemEquals(
                    typeName, typeof(_Commands.Core).FullName) ||
                SharedStringOps.SystemEquals(
                    typeName, typeof(_Commands.Stub).FullName) ||
                SharedStringOps.SystemEquals(
                    typeName, typeof(_Commands.Alias).FullName))
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method populates the command data list of the specified plugin
        /// with the built-in commands, optionally filtered by command flags and
        /// constrained by a rule set.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter context for which the built-in commands are being
        /// populated.  This parameter may be null.
        /// </param>
        /// <param name="ruleSet">
        /// The optional rule set used to further constrain which built-in
        /// commands are included.  This parameter may be null.
        /// </param>
        /// <param name="plugin">
        /// The plugin whose command data list is populated.  If this parameter
        /// is null, the method fails.
        /// </param>
        /// <param name="commandFlags">
        /// The optional command flags that a built-in command must have in
        /// order to be included.  This parameter may be null.
        /// </param>
        /// <param name="notCommandFlags">
        /// The optional command flags that a built-in command must not have in
        /// order to be included.  This parameter may be null.
        /// </param>
        /// <param name="error">
        /// Upon failure, this contains an appropriate error message.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise, an appropriate
        /// error code.
        /// </returns>
        private static ReturnCode PopulateBuiltInCommands(
            Interpreter interpreter,
            IRuleSet ruleSet,
            IPlugin plugin,
            CommandFlags? commandFlags,
            CommandFlags? notCommandFlags,
            ref Result error
            )
        {
            if (plugin == null)
            {
                error = "invalid plugin";
                return ReturnCode.Error;
            }

            CommandDataList commands = plugin.Commands;

            if (commands == null)
            {
                error = "invalid command data list";
                return ReturnCode.Error;
            }

            int length = 0;
            Guid?[] ids = null;
            Type[] types = null;
            CommandFlags[] flags = null;
            string[] names = null;
            string[] groups = null;

            if (!BuiltIns.GetCommands(
                    ref length, ref types, ref ids, ref flags,
                    ref names, ref groups))
            {
                error = "invalid built-in command set";
                return ReturnCode.Error;
            }

            for (int index = 0; index < length; index++)
            {
                Type type = types[index];

                if (type == null)
                    continue;

                if (IsReallyNonCommand(type))
                    continue;

                CommandFlags localCommandFlags = flags[index];

                if (FlagOps.HasFlags(localCommandFlags,
                        CommandFlags.NoPopulate, true))
                {
                    continue;
                }

                if ((commandFlags != null) && !FlagOps.HasFlags(
                        localCommandFlags, (CommandFlags)commandFlags,
                        false))
                {
                    continue;
                }

                if ((notCommandFlags != null) && FlagOps.HasFlags(
                        localCommandFlags, (CommandFlags)notCommandFlags,
                        false))
                {
                    continue;
                }

                string name = names[index];

                if (name == null)
                    name = ScriptOps.TypeNameToEntityName(type);

                if (ruleSet != null)
                {
                    if (!ruleSet.ApplyRules(
                            interpreter, IdentifierKind.Command,
                            MatchMode.IncludeRuleSetMask, name))
                    {
                        continue;
                    }

                    if (ruleSet.ApplyRules(
                            interpreter, IdentifierKind.Command,
                            MatchMode.ExcludeRuleSetMask, name))
                    {
                        continue;
                    }

                    if (FlagOps.HasFlags(
                            localCommandFlags, CommandFlags.Safe,
                            true))
                    {
                        if (ruleSet.ApplyRules(
                                interpreter, IdentifierKind.Command,
                                MatchMode.HideRuleSetMask, name))
                        {
                            localCommandFlags &= ~CommandFlags.Safe;
                            localCommandFlags |= CommandFlags.Unsafe;
                        }
                    }
                    else
                    {
                        if (ruleSet.ApplyRules(
                                interpreter, IdentifierKind.Command,
                                MatchMode.ShowRuleSetMask, name))
                        {
                            localCommandFlags &= ~CommandFlags.Unsafe;
                            localCommandFlags |= CommandFlags.Safe;
                        }
                    }
                }

                Guid? id = ids[index];

                if (id == null)
                    id = AttributeOps.GetObjectId(type);

                if (id == null)
                    id = Guid.Empty;

                string group = groups[index];

                if (group == null)
                    group = AttributeOps.GetObjectGroups(type);

                commands.Add(new CommandData((Guid)id,
                    name, group, null, null, type.FullName,
                    type, localCommandFlags, plugin, 0));
            }

            return ReturnCode.Ok;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method discovers all the types provided by the assembly that
        /// is associated with the specified plugin.
        /// </summary>
        /// <param name="plugin">
        /// The plugin whose assembly types are to be discovered.
        /// </param>
        /// <param name="verbose">
        /// Non-zero to produce more detailed error information.
        /// </param>
        /// <param name="types">
        /// Upon success, receives the list of discovered types.
        /// </param>
        /// <param name="error">
        /// Upon failure, receives information about the error that was
        /// encountered.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise, an error code.
        /// </returns>
        private static ReturnCode PopulatePluginTypes(
            IPlugin plugin,
            bool verbose,
            ref TypeList types,
            ref Result error
            )
        {
            if (plugin == null)
            {
                error = "invalid plugin";
                return ReturnCode.Error;
            }

            Assembly assembly = plugin.Assembly;

            if (assembly == null)
            {
                error = "plugin has invalid assembly";
                return ReturnCode.Error;
            }

            ResultList errors = null;

            if (!GetTypes(
                    assembly, verbose, ref types, ref errors))
            {
                error = errors;
                return ReturnCode.Error;
            }

            return ReturnCode.Ok;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method discovers the command types provided by the specified
        /// plugin and adds the corresponding command metadata to that plugin.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter context to use when applying any rule set, if
        /// any.
        /// </param>
        /// <param name="ruleSet">
        /// The rule set used to filter (and optionally hide or show) the
        /// discovered commands; may be null.
        /// </param>
        /// <param name="plugin">
        /// The plugin to receive the discovered command metadata.
        /// </param>
        /// <param name="types">
        /// The list of candidate types to consider; if null, the types are
        /// queried from the specified plugin.
        /// </param>
        /// <param name="commandFlags">
        /// If not null, only commands with all of these flags set are
        /// included.
        /// </param>
        /// <param name="notCommandFlags">
        /// If not null, commands with any of these flags set are excluded.
        /// </param>
        /// <param name="verbose">
        /// Non-zero to produce more detailed error information.
        /// </param>
        /// <param name="error">
        /// Upon failure, receives information about the error that was
        /// encountered.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise, an error code.
        /// </returns>
        private static ReturnCode PopulatePluginCommands(
            Interpreter interpreter,
            IRuleSet ruleSet,
            IPlugin plugin,
            TypeList types,
            CommandFlags? commandFlags,
            CommandFlags? notCommandFlags,
            bool verbose,
            ref Result error
            )
        {
            if (plugin == null)
            {
                error = "invalid plugin";
                return ReturnCode.Error;
            }

            CommandDataList commands = plugin.Commands;

            if (commands == null)
            {
                error = "plugin has invalid command data list";
                return ReturnCode.Error;
            }

            TypeList localTypes = null;

            if (types != null)
            {
                localTypes = types;
            }
            else if (PopulatePluginTypes(
                    plugin, verbose, ref localTypes,
                    ref error) != ReturnCode.Ok)
            {
                return ReturnCode.Error;
            }

            TypeList matchingTypes = null;
            ResultList errors = null;

            if (!GetMatchingClassTypes(
                    localTypes, typeof(ICommand), typeof(IWrapper),
                    true, verbose, ref matchingTypes, ref errors))
            {
                errors.Insert(0,
                    "could not get matching command types");

                error = errors;
                return ReturnCode.Error;
            }

            foreach (Type type in matchingTypes)
            {
                if (type == null)
                    continue;

                if (IsReallyNonCommand(type))
                    continue;

                CommandFlags localCommandFlags =
                    AttributeOps.GetCommandFlags(type);

                if (FlagOps.HasFlags(localCommandFlags,
                        CommandFlags.NoPopulate, true))
                {
                    continue;
                }

                if ((commandFlags != null) && !FlagOps.HasFlags(
                        localCommandFlags, (CommandFlags)commandFlags,
                        false))
                {
                    continue;
                }

                if ((notCommandFlags != null) && FlagOps.HasFlags(
                        localCommandFlags, (CommandFlags)notCommandFlags,
                        false))
                {
                    continue;
                }

                string name = AttributeOps.GetObjectName(type);

                if (name == null)
                    name = ScriptOps.TypeNameToEntityName(type);

                if (ruleSet != null)
                {
                    if (!ruleSet.ApplyRules(
                            interpreter, IdentifierKind.Command,
                            MatchMode.IncludeRuleSetMask, name))
                    {
                        continue;
                    }

                    if (ruleSet.ApplyRules(
                            interpreter, IdentifierKind.Command,
                            MatchMode.ExcludeRuleSetMask, name))
                    {
                        continue;
                    }

                    if (FlagOps.HasFlags(
                            localCommandFlags, CommandFlags.Safe,
                            true))
                    {
                        if (ruleSet.ApplyRules(
                                interpreter, IdentifierKind.Command,
                                MatchMode.HideRuleSetMask, name))
                        {
                            localCommandFlags &= ~CommandFlags.Safe;
                            localCommandFlags |= CommandFlags.Unsafe;
                        }
                    }
                    else
                    {
                        if (ruleSet.ApplyRules(
                                interpreter, IdentifierKind.Command,
                                MatchMode.ShowRuleSetMask, name))
                        {
                            localCommandFlags &= ~CommandFlags.Unsafe;
                            localCommandFlags |= CommandFlags.Safe;
                        }
                    }
                }

                commands.Add(new CommandData(
                    name, null, null, null, type.FullName,
                    localCommandFlags, plugin, 0));
            }

            return ReturnCode.Ok;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method discovers the policy delegates provided by the
        /// specified types and adds the corresponding policy metadata to the
        /// specified plugin.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter context to use when applying any rule set, if
        /// any.
        /// </param>
        /// <param name="ruleSet">
        /// The rule set used to filter the discovered policies; may be null.
        /// </param>
        /// <param name="plugin">
        /// The plugin to receive the discovered policy metadata.
        /// </param>
        /// <param name="types">
        /// The types to examine for matching policy delegates.
        /// </param>
        /// <param name="error">
        /// Upon failure, receives information about the error that was
        /// encountered.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise, an error code.
        /// </returns>
        private static ReturnCode PopulatePluginPolicies(
            Interpreter interpreter,
            IRuleSet ruleSet,
            IPlugin plugin,
            IEnumerable<Type> types,
            ref Result error
            )
        {
            if (types == null)
            {
                error = "invalid types";
                return ReturnCode.Error;
            }

            if (plugin == null)
            {
                error = "invalid plugin";
                return ReturnCode.Error;
            }

            PolicyDataList policies = plugin.Policies;

            if (policies == null)
            {
                error = "plugin has invalid policy data list";
                return ReturnCode.Error;
            }

            Dictionary<Delegate, MethodFlags> delegates = null;
            ResultList errors = null;

            if (!GetMatchingDelegates(types,
                    typeof(ExecuteCallback), MethodFlags.PolicyMask,
                    MethodFlags.NoAdd, false, false, false,
                    ref delegates, ref errors))
            {
                errors.Insert(0,
                    "could not get matching policy delegates");

                error = errors;
                return ReturnCode.Error;
            }

            BindingFlags bindingFlags = ObjectOps.GetBindingFlags(
                MetaBindingFlags.Delegate, true);

            foreach (DelegatePair pair in delegates)
            {
                Delegate @delegate = pair.Key;

                if (@delegate == null)
                    continue;

                MethodInfo methodInfo = @delegate.Method;

                if (methodInfo == null)
                    continue;

                Type type = methodInfo.DeclaringType;

                if (type == null)
                    continue;

                string name = FormatOps.MethodFullName(
                    type, methodInfo.Name);

                if (ruleSet != null)
                {
                    if (!ruleSet.ApplyRules(
                            interpreter, IdentifierKind.Policy,
                            MatchMode.IncludeRuleSetMask, name))
                    {
                        continue;
                    }

                    if (ruleSet.ApplyRules(
                            interpreter, IdentifierKind.Policy,
                            MatchMode.ExcludeRuleSetMask, name))
                    {
                        continue;
                    }
                }

                //
                // HACK: Cannot use "type" here because it will
                //       break plugin isolation.
                //
                policies.Add(new PolicyData(
                    name, null, null, null, type.FullName,
                    null, methodInfo.Name, bindingFlags,
                    pair.Value, PolicyFlags.None, plugin, 0));
            }

            return ReturnCode.Ok;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method populates the command and policy metadata for the
        /// specified plugin, optionally using the built-in command data.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter context to use when applying any rule set, if
        /// any.
        /// </param>
        /// <param name="plugin">
        /// The plugin to receive the discovered command and policy metadata.
        /// </param>
        /// <param name="types">
        /// The list of candidate types to consider; if null, the types are
        /// queried from the specified plugin when necessary.
        /// </param>
        /// <param name="ruleSet">
        /// The rule set used to filter the discovered entities; may be null.
        /// </param>
        /// <param name="commandFlags">
        /// If not null, only commands with all of these flags set are
        /// included.
        /// </param>
        /// <param name="notCommandFlags">
        /// If not null, commands with any of these flags set are excluded.
        /// </param>
        /// <param name="useBuiltIn">
        /// Non-zero to populate commands from the built-in command data
        /// instead of discovering them from the plugin.
        /// </param>
        /// <param name="noCommands">
        /// Non-zero to skip populating command metadata.
        /// </param>
        /// <param name="noPolicies">
        /// Non-zero to skip populating policy metadata.
        /// </param>
        /// <param name="verbose">
        /// Non-zero to produce more detailed error information.
        /// </param>
        /// <param name="error">
        /// Upon failure, receives information about the error that was
        /// encountered.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise, an error code.
        /// </returns>
        public static ReturnCode PopulatePluginEntities(
            Interpreter interpreter,
            IPlugin plugin,
            TypeList types,
            IRuleSet ruleSet,
            CommandFlags? commandFlags,
            CommandFlags? notCommandFlags,
            bool useBuiltIn,
            bool noCommands,
            bool noPolicies,
            bool verbose,
            ref Result error
            )
        {
            ReturnCode code;
            TypeList localTypes = null;

            if (types != null)
            {
                localTypes = types;
            }
            else if ((useBuiltIn || noCommands) && noPolicies)
            {
                //
                // NOTE: Either we are using the built-in
                //       command data -OR- no commands are
                //       required.  Also, no policies can
                //       be required.
                //
            }
            else
            {
                code = PopulatePluginTypes(
                    plugin, verbose, ref localTypes,
                    ref error);

                if (code != ReturnCode.Ok)
                    return code;
            }

            if (!noCommands)
            {
                if (useBuiltIn)
                {
                    code = PopulateBuiltInCommands(
                        interpreter, ruleSet, plugin,
                        commandFlags, notCommandFlags,
                        ref error);

                    if (code != ReturnCode.Ok)
                        return code;
                }
                else
                {
                    code = PopulatePluginCommands(
                        interpreter, ruleSet, plugin,
                        localTypes, commandFlags,
                        notCommandFlags, verbose,
                        ref error);

                    if (code != ReturnCode.Ok)
                        return code;
                }
            }

            if (!noPolicies)
            {
                code = PopulatePluginPolicies(
                    interpreter, ruleSet, plugin,
                    localTypes, ref error);

                if (code != ReturnCode.Ok)
                    return code;
            }

            return ReturnCode.Ok;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method throws a <see cref="ScriptException" /> indicating that
        /// the specified feature is not supported by the specified plugin,
        /// subject to the active interpreter (or global) configuration.
        /// </summary>
        /// <param name="pluginData">
        /// The plugin that does not support the feature.
        /// </param>
        /// <param name="name">
        /// The name of the feature that is not supported.
        /// </param>
        public static void ThrowFeatureNotSupported( /* EXTERNAL USE ONLY */
            IPluginData pluginData,
            string name
            )
        {
            Interpreter interpreter = Interpreter.GetActive();

            bool shouldThrow = (interpreter != null) ?
                interpreter.ThrowOnFeatureNotSupported :
                ThrowOnFeatureNotSupported;

            if (shouldThrow)
            {
                throw new ScriptException(String.Format(
                    "feature {0} not supported by plugin {1}",
                    FormatOps.WrapOrNull(name),
                    FormatOps.WrapOrNull(pluginData)));
            }
        }

        ///////////////////////////////////////////////////////////////////////

        #region Expression Operator Support Methods
        /// <summary>
        /// This method builds the list of metadata for the built-in
        /// expression operators.
        /// </summary>
        /// <param name="plugin">
        /// The plugin to associate with the discovered operator metadata; may
        /// be null.
        /// </param>
        /// <param name="comparisonType">
        /// The string comparison type to associate with the discovered
        /// operator metadata.
        /// </param>
        /// <param name="standardOnly">
        /// Non-zero to include only those operators flagged as standard;
        /// otherwise, all matching operators are included.
        /// </param>
        /// <param name="operators">
        /// Upon success, receives the list of discovered operator metadata;
        /// if null, a new list is created.
        /// </param>
        /// <param name="error">
        /// Upon failure, receives information about the error that was
        /// encountered.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise, an error code.
        /// </returns>
        public static ReturnCode GetBuiltInOperators(
            IPlugin plugin,
            StringComparison comparisonType,
            bool standardOnly,
            ref List<IOperatorData> operators,
            ref Result error
            )
        {
            if (operators == null)
                operators = new List<IOperatorData>();

            int length = 0;
            Type[] types = null;
            Guid?[] ids = null;
            OperatorFlags[] flags = null;
            Lexeme[] lexemes = null;
            Arity[] operands = null;
            string[] names = null;
            string[] groups = null;
            TypeListFlags[] typeListFlags = null;

            if (!BuiltIns.GetOperators(
                    ref length, ref types, ref ids, ref flags,
                    ref lexemes, ref operands, ref names,
                    ref groups, ref typeListFlags))
            {
                error = "invalid built-in operator set";
                return ReturnCode.Error;
            }

            for (int index = 0; index < length; index++)
            {
                Type type = types[index];

                if (type == null)
                    continue;

                string typeName = type.FullName;

                if (SharedStringOps.SystemEquals(
                        typeName, typeof(_Operators.Default).FullName) ||
                    SharedStringOps.SystemEquals(
                        typeName, typeof(_Operators.Core).FullName))
                {
                    continue;
                }

                OperatorFlags localOperatorFlags = flags[index];

                if (FlagOps.HasFlags(
                        localOperatorFlags, OperatorFlags.NoPopulate,
                        true))
                {
                    continue;
                }

                if (standardOnly && !FlagOps.HasFlags(
                        localOperatorFlags, OperatorFlags.Standard,
                        true))
                {
                    continue;
                }

                TypeList operandTypes = null;

                Value.GetTypes(
                    typeListFlags[index], ref operandTypes);

                Guid? id = ids[index];

                if (id == null)
                    id = AttributeOps.GetObjectId(type);

                if (id == null)
                    id = Guid.Empty;

                string name = names[index];

                if (name == null)
                    name = ScriptOps.TypeNameToEntityName(type);

                string group = groups[index];

                if (group == null)
                    group = AttributeOps.GetObjectGroups(type);

                operators.Add(new OperatorData((Guid)id,
                    name, group, null, null, typeName, type,
                    lexemes[index], (int)operands[index],
                    operandTypes, localOperatorFlags,
                    comparisonType, plugin, 0));
            }

            return ReturnCode.Ok;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method discovers the operator types provided by the specified
        /// plugin and builds the corresponding list of operator metadata.
        /// </summary>
        /// <param name="plugin">
        /// The plugin whose operator types are to be discovered.
        /// </param>
        /// <param name="types">
        /// The list of candidate types to consider; if null, the types are
        /// queried from the specified plugin.
        /// </param>
        /// <param name="comparisonType">
        /// The string comparison type to associate with the discovered
        /// operator metadata.
        /// </param>
        /// <param name="standardOnly">
        /// Non-zero to include only those operators flagged as standard;
        /// otherwise, all matching operators are included.
        /// </param>
        /// <param name="verbose">
        /// Non-zero to produce more detailed error information.
        /// </param>
        /// <param name="operators">
        /// Upon success, receives the list of discovered operator metadata;
        /// if null, a new list is created.
        /// </param>
        /// <param name="error">
        /// Upon failure, receives information about the error that was
        /// encountered.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise, an error code.
        /// </returns>
        public static ReturnCode GetPluginOperators(
            IPlugin plugin,
            TypeList types,
            StringComparison comparisonType,
            bool standardOnly,
            bool verbose,
            ref List<IOperatorData> operators,
            ref Result error
            )
        {
            TypeList localTypes = null;

            if (types != null)
            {
                localTypes = types;
            }
            else if (PopulatePluginTypes(
                    plugin, verbose, ref localTypes,
                    ref error) != ReturnCode.Ok)
            {
                return ReturnCode.Error;
            }

            TypeList matchingTypes = null;
            ResultList errors = null;

            if (!GetMatchingClassTypes(
                    localTypes, typeof(IOperator), typeof(IWrapper),
                    true, verbose, ref matchingTypes, ref errors))
            {
                errors.Insert(0,
                    "could not get matching operator types");

                error = errors;
                return ReturnCode.Error;
            }

            if (operators == null)
                operators = new List<IOperatorData>();

            foreach (Type type in matchingTypes)
            {
                if (type == null)
                    continue;

                string typeName = type.FullName;

                if (SharedStringOps.SystemEquals(
                        typeName, typeof(_Operators.Default).FullName) ||
                    SharedStringOps.SystemEquals(
                        typeName, typeof(_Operators.Core).FullName))
                {
                    continue;
                }

                OperatorFlags operatorFlags =
                    AttributeOps.GetOperatorFlags(type);

                if (FlagOps.HasFlags(
                        operatorFlags, OperatorFlags.NoPopulate, true))
                {
                    continue;
                }

                if (standardOnly && !FlagOps.HasFlags(
                        operatorFlags, OperatorFlags.Standard, true))
                {
                    continue;
                }

                Lexeme lexeme = AttributeOps.GetLexeme(type);
                int operands = AttributeOps.GetOperands(type);

                TypeList operandTypes = null;

                Value.GetTypes(
                    AttributeOps.GetTypeListFlags(type), ref operandTypes);

                string name = AttributeOps.GetObjectName(type);

                if (name == null)
                    name = ScriptOps.TypeNameToEntityName(type);

                operators.Add(new OperatorData(
                    name, null, null, null, typeName, type,
                    lexeme, operands, operandTypes, operatorFlags,
                    comparisonType, plugin, 0));
            }

            return ReturnCode.Ok;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method creates an operator instance from the specified
        /// operator metadata.
        /// </summary>
        /// <param name="operatorData">
        /// The metadata describing the operator to create.
        /// </param>
        /// <param name="operator">
        /// Upon success, receives the newly created operator instance.
        /// </param>
        /// <param name="error">
        /// Upon failure, receives information about the error that was
        /// encountered.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise, an error code.
        /// </returns>
        public static ReturnCode CreateOperator(
            IOperatorData operatorData,
            ref IOperator @operator,
            ref Result error
            )
        {
            if (operatorData == null)
            {
                error = "invalid operator data";
                return ReturnCode.Error;
            }

            string typeName = operatorData.TypeName;

            if (String.IsNullOrEmpty(typeName))
            {
                error = "invalid type name";
                return ReturnCode.Error;
            }

            Type type = operatorData.Type;

            if (type == null)
                type = Type.GetType(typeName, false, true);

            if (type == null)
            {
                error = String.Format(
                    "operator {0} not found",
                    FormatOps.OperatorTypeName(typeName, true));

                return ReturnCode.Error;
            }

            try
            {
                @operator = (IOperator)Activator.CreateInstance(
                    type, new object[] { operatorData });

                return ReturnCode.Ok;
            }
            catch (Exception e)
            {
                error = e;
            }

            return ReturnCode.Error;
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Expression Function Support Methods
        /// <summary>
        /// This method builds the list of metadata for the built-in
        /// expression functions.
        /// </summary>
        /// <param name="plugin">
        /// The plugin to associate with the discovered function metadata; may
        /// be null.
        /// </param>
        /// <param name="standardOnly">
        /// Non-zero to include only those functions flagged as standard;
        /// otherwise, all matching functions are included.
        /// </param>
        /// <param name="functions">
        /// Upon success, receives the list of discovered function metadata;
        /// if null, a new list is created.
        /// </param>
        /// <param name="error">
        /// Upon failure, receives information about the error that was
        /// encountered.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise, an error code.
        /// </returns>
        public static ReturnCode GetBuiltInFunctions(
            IPlugin plugin,
            bool standardOnly,
            ref List<IFunctionData> functions,
            ref Result error
            )
        {
            if (functions == null)
                functions = new List<IFunctionData>();

            int length = 0;
            Type[] types = null;
            Guid?[] ids = null;
            FunctionFlags[] flags = null;
            Arity[] arguments = null;
            string[] names = null;
            string[] groups = null;
            TypeListFlags[] typeListFlags = null;

            if (!BuiltIns.GetFunctions(
                    ref length, ref types, ref ids, ref flags,
                    ref arguments, ref names, ref groups,
                    ref typeListFlags))
            {
                error = "invalid built-in function set";
                return ReturnCode.Error;
            }

            for (int index = 0; index < length; index++)
            {
                Type type = types[index];

                if (type == null)
                    continue;

                string typeName = type.FullName;

                if (SharedStringOps.SystemEquals(
                        typeName, typeof(_Functions.Default).FullName) ||
                    SharedStringOps.SystemEquals(
                        typeName, typeof(_Functions.Core).FullName) ||
                    SharedStringOps.SystemEquals(
                        typeName, typeof(_Functions.Arguments).FullName))
                {
                    continue;
                }

                FunctionFlags localFunctionFlags = flags[index];

                if (FlagOps.HasFlags(
                        localFunctionFlags, FunctionFlags.NoPopulate,
                        true))
                {
                    continue;
                }

                if (standardOnly && !FlagOps.HasFlags(
                        localFunctionFlags, FunctionFlags.Standard,
                        true))
                {
                    continue;
                }

                TypeList argumentTypes = null;

                Value.GetTypes(
                    typeListFlags[index], ref argumentTypes);

                Guid? id = ids[index];

                if (id == null)
                    id = AttributeOps.GetObjectId(type);

                if (id == null)
                    id = Guid.Empty;

                string name = names[index];

                if (name == null)
                    name = ScriptOps.TypeNameToEntityName(type);

                string group = groups[index];

                if (group == null)
                    group = AttributeOps.GetObjectGroups(type);

                functions.Add(new FunctionData((Guid)id,
                    name, group, null, null, typeName, type,
                    (int)arguments[index], argumentTypes,
                    localFunctionFlags, plugin, 0));
            }

            return ReturnCode.Ok;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method discovers the function types provided by the specified
        /// plugin and builds the corresponding list of function metadata.
        /// </summary>
        /// <param name="plugin">
        /// The plugin whose function types are to be discovered.
        /// </param>
        /// <param name="types">
        /// The list of candidate types to consider; if null, the types are
        /// queried from the specified plugin.
        /// </param>
        /// <param name="standardOnly">
        /// Non-zero to include only those functions flagged as standard;
        /// otherwise, all matching functions are included.
        /// </param>
        /// <param name="verbose">
        /// Non-zero to produce more detailed error information.
        /// </param>
        /// <param name="functions">
        /// Upon success, receives the list of discovered function metadata;
        /// if null, a new list is created.
        /// </param>
        /// <param name="error">
        /// Upon failure, receives information about the error that was
        /// encountered.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise, an error code.
        /// </returns>
        public static ReturnCode GetPluginFunctions(
            IPlugin plugin,
            TypeList types,
            bool standardOnly,
            bool verbose,
            ref List<IFunctionData> functions,
            ref Result error
            )
        {
            TypeList localTypes = null;

            if (types != null)
            {
                localTypes = types;
            }
            else if (PopulatePluginTypes(
                    plugin, verbose, ref localTypes,
                    ref error) != ReturnCode.Ok)
            {
                return ReturnCode.Error;
            }

            TypeList matchingTypes = null;
            ResultList errors = null;

            if (!GetMatchingClassTypes(
                    localTypes, typeof(IFunction), typeof(IWrapper),
                    true, verbose, ref matchingTypes, ref errors))
            {
                errors.Insert(0,
                    "could not get matching function types");

                error = errors;
                return ReturnCode.Error;
            }

            if (functions == null)
                functions = new List<IFunctionData>();

            foreach (Type type in matchingTypes)
            {
                if (type == null)
                    continue;

                string typeName = type.FullName;

                if (SharedStringOps.SystemEquals(
                        typeName, typeof(_Functions.Default).FullName) ||
                    SharedStringOps.SystemEquals(
                        typeName, typeof(_Functions.Core).FullName) ||
                    SharedStringOps.SystemEquals(
                        typeName, typeof(_Functions.Arguments).FullName))
                {
                    continue;
                }

                FunctionFlags functionFlags =
                    AttributeOps.GetFunctionFlags(type);

                if (FlagOps.HasFlags(
                        functionFlags, FunctionFlags.NoPopulate, true))
                {
                    continue;
                }

                if (standardOnly && !FlagOps.HasFlags(
                        functionFlags, FunctionFlags.Standard, true))
                {
                    continue;
                }

                int arguments = AttributeOps.GetArguments(type);

                TypeList argumentTypes = null;

                Value.GetTypes(
                    AttributeOps.GetTypeListFlags(type),
                    ref argumentTypes);

                string name = AttributeOps.GetObjectName(type);

                if (name == null)
                    name = ScriptOps.TypeNameToEntityName(type);

                functions.Add(new FunctionData(
                    name, null, null, null, typeName, type,
                    arguments, argumentTypes, functionFlags,
                    plugin, 0));
            }

            return ReturnCode.Ok;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method creates a function instance from the specified function
        /// metadata.
        /// </summary>
        /// <param name="functionData">
        /// The metadata describing the function to be created, including its
        /// type information.
        /// </param>
        /// <param name="function">
        /// Upon success, receives the newly created function instance.
        /// </param>
        /// <param name="error">
        /// Upon failure, receives information about the error that was
        /// encountered.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise, an error code.
        /// </returns>
        public static ReturnCode CreateFunction(
            IFunctionData functionData,
            ref IFunction function,
            ref Result error
            )
        {
            if (functionData == null)
            {
                error = "invalid function data";
                return ReturnCode.Error;
            }

            string typeName = functionData.TypeName;

            if (String.IsNullOrEmpty(typeName))
            {
                error = "invalid type name";
                return ReturnCode.Error;
            }

            Type type = functionData.Type;

            if (type == null)
                type = Type.GetType(typeName, false, true);

            if (type == null)
            {
                error = String.Format(
                    "function {0} not found",
                    FormatOps.FunctionTypeName(typeName, true));

                return ReturnCode.Error;
            }

            try
            {
                function = (IFunction)Activator.CreateInstance(
                    type, new object[] { functionData });

                return ReturnCode.Ok;
            }
            catch (Exception e)
            {
                error = e;
            }

            return ReturnCode.Error;
        }
        #endregion
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Native Delegate Support Methods
#if NATIVE && (NATIVE_UTILITY || TCL)
        /// <summary>
        /// This method clears the specified collections of native delegates
        /// and their associated optional flags.
        /// </summary>
        /// <param name="delegates">
        /// The collection of native delegates to clear; if null, it is ignored.
        /// </param>
        /// <param name="optional">
        /// The collection of optional flags to clear; if null, it is ignored.
        /// </param>
        public static void UnsetNativeDelegates(
            TypeDelegateDictionary delegates,
            TypeBoolDictionary optional
            )
        {
            if (delegates != null)
                delegates.Clear();

            if (optional != null)
                optional.Clear();
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method populates the specified collection of native delegates
        /// from a collection of native function addresses.
        /// </summary>
        /// <param name="description">
        /// A short description of the native delegates being populated, used
        /// when formatting error messages.
        /// </param>
        /// <param name="addresses">
        /// The collection that maps each delegate type to its native function
        /// address.
        /// </param>
        /// <param name="delegates">
        /// The collection of delegate types to populate; upon success, each
        /// entry is set to the delegate created for its native function.
        /// </param>
        /// <param name="optional">
        /// The collection indicating which delegate types are optional; an
        /// optional delegate with no available address is set to null instead
        /// of causing a failure.  This value may be null.
        /// </param>
        /// <param name="error">
        /// Upon failure, receives information about the error that was
        /// encountered.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise, an error code.
        /// </returns>
        public static ReturnCode SetNativeDelegates(
            string description,
            TypeIntPtrDictionary addresses,
            TypeDelegateDictionary delegates,
            TypeBoolDictionary optional,
            ref Result error
            )
        {
            if (addresses == null)
            {
                error = "addresses are invalid";
                return ReturnCode.Error;
            }

            if (delegates == null)
            {
                error = "delegates are invalid";
                return ReturnCode.Error;
            }

            try
            {
                TypeList types = new TypeList(delegates.Keys);

                foreach (Type type in types)
                {
                    if (type == null)
                        continue;

                    IntPtr address;

                    if (addresses.TryGetValue(type, out address) &&
                        (address != IntPtr.Zero))
                    {
                        delegates[type] = Marshal.GetDelegateForFunctionPointer(
                            address, type); /* throw */
                    }
                    else
                    {
                        bool value;

                        if ((optional != null) &&
                            optional.TryGetValue(type, out value) && value)
                        {
                            //
                            // NOTE: This is allowed, an optional function was
                            //       not found.
                            //
                            delegates[type] = null;
                        }
                        else
                        {
                            error = String.Format(
                                "cannot locate required {0} function " +
                                "{1}, address not available", description,
                                FormatOps.WrapOrNull(type.Name));

                            return ReturnCode.Error;
                        }
                    }
                }

                return ReturnCode.Ok;
            }
            catch (Exception e)
            {
                error = e;
            }

            return ReturnCode.Error;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method populates the specified collection of native delegates
        /// by resolving each delegate type to a native function exported by the
        /// specified loaded module.
        /// </summary>
        /// <param name="description">
        /// A short description of the native delegates being populated, used
        /// when formatting error messages.
        /// </param>
        /// <param name="module">
        /// The native handle of the loaded module from which the native
        /// functions are resolved.
        /// </param>
        /// <param name="delegates">
        /// The collection of delegate types to populate; upon success, each
        /// entry is set to the delegate created for its native function.
        /// </param>
        /// <param name="optional">
        /// The collection indicating which delegate types are optional; an
        /// optional delegate that cannot be resolved is set to null instead of
        /// causing a failure.  This value may be null.
        /// </param>
        /// <param name="error">
        /// Upon failure, receives information about the error that was
        /// encountered.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise, an error code.
        /// </returns>
        public static ReturnCode SetNativeDelegates(
            string description,
            IntPtr module,
            TypeDelegateDictionary delegates,
            TypeBoolDictionary optional,
            ref Result error
            )
        {
            if (module == IntPtr.Zero)
            {
                error = "module is invalid";
                return ReturnCode.Error;
            }

            if (delegates == null)
            {
                error = "delegates are invalid";
                return ReturnCode.Error;
            }

            try
            {
                TypeList types = new TypeList(delegates.Keys);

                foreach (Type type in types)
                {
                    if (type == null)
                        continue;

                    int lastError;

                    IntPtr address = NativeOps.GetProcAddress(
                        module, type.Name, out lastError); /* throw */

                    if (address == IntPtr.Zero)
                    {
                        string objectName = AttributeOps.GetObjectName(type);

                        if (objectName != null)
                        {
                            address = NativeOps.GetProcAddress(
                                module, objectName, out lastError); /* throw */
                        }
                    }

                    if (address != IntPtr.Zero)
                    {
                        delegates[type] = Marshal.GetDelegateForFunctionPointer(
                            address, type); /* throw */
                    }
                    else
                    {
                        bool value;

                        if ((optional != null) &&
                            optional.TryGetValue(type, out value) && value)
                        {
                            //
                            // NOTE: This is allowed, an optional function was
                            //       not found.
                            //
                            delegates[type] = null;
                        }
                        else
                        {
                            //
                            // NOTE: Failure, a required function was not found.
                            //
                            error = String.Format(
                                "cannot locate required {1} function " +
                                "{2}, GetProcAddress({3}, {2}) failed " +
                                "with error {0}: {4}", lastError, description,
                                FormatOps.WrapOrNull(type.Name), module,
                                NativeOps.GetDynamicLoadingError(lastError));

                            return ReturnCode.Error;
                        }
                    }
                }

                return ReturnCode.Ok;
            }
            catch (Exception e)
            {
                error = e;
            }

            return ReturnCode.Error;
        }
#endif
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Random Number Support Methods
        /// <summary>
        /// This method ensures that the shared random number generator has been
        /// created, optionally forcing it to be recreated.
        /// </summary>
        /// <param name="force">
        /// Non-zero to force a new random number generator to be created even if
        /// one already exists.
        /// </param>
        /// <returns>
        /// The shared random number generator instance.
        /// </returns>
        private static RandomNumberGenerator InitializeRandomness(
            bool force
            )
        {
            lock (syncRoot) /* TRANSACTIONAL */
            {
                if (force || (randomNumberGenerator == null))
                    randomNumberGenerator = RNGCryptoServiceProvider.Create();

                return randomNumberGenerator;
            }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method returns the shared random number generator, creating it
        /// first if necessary.
        /// </summary>
        /// <returns>
        /// The shared random number generator instance.
        /// </returns>
        public static RandomNumberGenerator GetRandomness()
        {
            return InitializeRandomness(false);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method disposes of and clears the shared random number
        /// generator, releasing any associated resources.
        /// </summary>
        /// <returns>
        /// The number of internal operations performed while clearing the
        /// cached state.
        /// </returns>
        public static int ClearCache()
        {
            lock (syncRoot) /* TRANSACTIONAL */
            {
                int result = 0;

                if (randomNumberGenerator != null)
                {
                    if (ObjectOps.TryDisposeOrComplain<RandomNumberGenerator>(
                            null, ref randomNumberGenerator) == ReturnCode.Ok)
                    {
                        result++;
                    }

                    randomNumberGenerator = null;
                    result++;
                }

                return result;
            }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method fills the specified byte array with random data obtained
        /// from the shared random number generator.
        /// </summary>
        /// <param name="bytes">
        /// The byte array to fill with random data; its length determines the
        /// number of random bytes produced.
        /// </param>
        public static void GetRandomBytes( /* throw */
            ref byte[] bytes /* in, out */
            )
        {
            /* NO RESULT */
            InitializeRandomness(false); /* throw */

            lock (syncRoot) /* TRANSACTIONAL */
            {
                /* NO RESULT */
                GetRandomBytes(null,
                    randomNumberGenerator, null, ref bytes); /* throw */
            }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method fills the specified byte array with random data obtained
        /// from the first available entropy source among those provided, falling
        /// back to the global entropy source when none is supplied.
        /// </summary>
        /// <param name="provideEntropy">
        /// The entropy provider to use; this value may be null.
        /// </param>
        /// <param name="randomNumberGenerator">
        /// The random number generator to use when no entropy provider is
        /// supplied; this value may be null.
        /// </param>
        /// <param name="random">
        /// The pseudo-random number generator to use when neither an entropy
        /// provider nor a random number generator is supplied; this value may be
        /// null.
        /// </param>
        /// <param name="bytes">
        /// The byte array to fill with random data; its length determines the
        /// number of random bytes produced.
        /// </param>
        public static void GetRandomBytes( /* throw */
            IProvideEntropy provideEntropy,              /* in: may be NULL. */
            RandomNumberGenerator randomNumberGenerator, /* in: may be NULL. */
            Random random,                               /* in: may be NULL. */
            ref byte[] bytes                             /* in, out */
            )
        {
            bool gotBytes = false;

            if (provideEntropy != null)
            {
                /* NO RESULT */
                provideEntropy.GetBytes(ref bytes);

                gotBytes = true;
            }
            else if (randomNumberGenerator != null)
            {
                /* NO RESULT */
                randomNumberGenerator.GetBytes(bytes);

                gotBytes = true;
            }
            else if (random != null)
            {
                /* NO RESULT */
                random.NextBytes(bytes);

                gotBytes = true;
            }

            if (!gotBytes && !GlobalState.GetRandomBytes(ref bytes))
                throw new ScriptException("could not obtain entropy");
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method fills the specified byte array with random data, using
        /// the specified interpreter as the entropy source when one is provided.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter whose entropy source is used; if null, the shared
        /// random number generator is used instead.  This value is optional.
        /// </param>
        /// <param name="bytes">
        /// Upon success, receives the random data; its length determines the
        /// number of random bytes produced.
        /// </param>
        /// <param name="error">
        /// Upon failure, receives information about the error that was
        /// encountered.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise, an error code.
        /// </returns>
        public static ReturnCode GetRandomBytes(
            Interpreter interpreter, /* in: OPTIONAL */
            ref byte[] bytes,        /* out */
            ref Result error         /* out */
            )
        {
            if (interpreter != null)
            {
                try
                {
                    /* NO RESULT */
                    interpreter.GetRandomBytes(ref bytes); /* throw */

                    return ReturnCode.Ok;
                }
                catch (Exception e)
                {
                    error = e;
                }
            }
            else
            {
                try
                {
                    /* NO RESULT */
                    GetRandomBytes(ref bytes); /* throw */

                    return ReturnCode.Ok;
                }
                catch (Exception e)
                {
                    error = e;
                }
            }

            return ReturnCode.Error;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method returns a signed random number obtained from the shared
        /// random number generator.
        /// </summary>
        /// <returns>
        /// A randomly generated signed integer value.
        /// </returns>
        public static long GetSignedRandomNumber() /* throw */
        {
            /* NO RESULT */
            InitializeRandomness(false); /* throw */

            lock (syncRoot) /* TRANSACTIONAL */
            {
                return GetSignedRandomNumber(
                    null, randomNumberGenerator, null); /* throw */
            }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method returns a signed random number obtained from the first
        /// available entropy source among those provided.
        /// </summary>
        /// <param name="provideEntropy">
        /// The entropy provider to use; this value may be null.
        /// </param>
        /// <param name="randomNumberGenerator">
        /// The random number generator to use when no entropy provider is
        /// supplied; this value may be null.
        /// </param>
        /// <param name="random">
        /// The pseudo-random number generator to use when neither an entropy
        /// provider nor a random number generator is supplied; this value may be
        /// null.
        /// </param>
        /// <returns>
        /// A randomly generated signed integer value.
        /// </returns>
        private static long GetSignedRandomNumber( /* throw */
            IProvideEntropy provideEntropy,              /* in: may be NULL. */
            RandomNumberGenerator randomNumberGenerator, /* in: may be NULL. */
            Random random                                /* in: may be NULL. */
            )
        {
            return ConversionOps.ToLong(GetRandomNumber(
                provideEntropy, randomNumberGenerator, random));
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method returns an unsigned random number obtained from the
        /// shared random number generator.
        /// </summary>
        /// <returns>
        /// A randomly generated unsigned integer value.
        /// </returns>
        public static ulong GetRandomNumber() /* throw */
        {
            /* NO RESULT */
            InitializeRandomness(false); /* throw */

            lock (syncRoot) /* TRANSACTIONAL */
            {
                return GetRandomNumber(
                    null, randomNumberGenerator, null); /* throw */
            }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method returns an unsigned random number obtained from the first
        /// available entropy source among those provided.
        /// </summary>
        /// <param name="provideEntropy">
        /// The entropy provider to use; this value may be null.
        /// </param>
        /// <param name="randomNumberGenerator">
        /// The random number generator to use when no entropy provider is
        /// supplied; this value may be null.
        /// </param>
        /// <param name="random">
        /// The pseudo-random number generator to use when neither an entropy
        /// provider nor a random number generator is supplied; this value may be
        /// null.
        /// </param>
        /// <returns>
        /// A randomly generated unsigned integer value.
        /// </returns>
        public static ulong GetRandomNumber( /* throw */
            IProvideEntropy provideEntropy,              /* in: may be NULL. */
            RandomNumberGenerator randomNumberGenerator, /* in: may be NULL. */
            Random random                                /* in: may be NULL. */
            )
        {
            byte[] bytes = new byte[sizeof(ulong)];

            /* NO RESULT */
            GetRandomBytes(
                provideEntropy, randomNumberGenerator, random,
                ref bytes); /* throw */

            return BitConverter.ToUInt64(bytes, 0);
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Interpreter Locking Support Methods
        //
        // TODO: Make this method configurable via some runtime mechanism?
        //
        /// <summary>
        /// This method determines whether the disposed state of the parent
        /// object should be checked prior to exiting a lock via the
        /// <see cref="ISynchronize.ExitLock" /> method.
        /// </summary>
        /// <param name="locked">
        /// Non-zero if the lock is actually held; otherwise, zero.
        /// </param>
        /// <returns>
        /// True if the disposed state should be checked; otherwise, false.
        /// </returns>
        public static bool ShouldCheckDisposedOnExitLock(
            bool locked
            )
        {
#if DEBUG
            //
            // NOTE: When compiled in the "Debug" build configuration, check
            //       if the parent object instance is disposed prior to exiting
            //       the lock via the ISynchronize.ExitLock method if the lock
            //       is not actually held -OR- if the "CheckDisposedOnExitLock"
            //       variable is non-zero.
            //
            if (CheckDisposedOnExitLock)
                return true;

            return !locked;
#else
            //
            // NOTE: When compiled in the "Release" build configuration, check
            //       if the parent object instance is disposed prior to exiting
            //       the lock via the ISynchronize.ExitLock method only if the
            //       lock is not actually held.
            //
            return !locked;
#endif
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Native Pointer Support Methods
        /// <summary>
        /// This method determines whether the specified native handle appears
        /// to be valid (i.e. neither a null handle nor the well-known invalid
        /// handle value).
        /// </summary>
        /// <param name="handle">
        /// The native handle value to check.
        /// </param>
        /// <returns>
        /// True if the handle appears to be valid; otherwise, false.
        /// </returns>
        public static bool IsValidHandle(
            IntPtr handle
            )
        {
            return ((handle != IntPtr.Zero) &&
                    (handle != INVALID_HANDLE_VALUE));
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method determines whether the specified native handle appears
        /// to be valid (i.e. neither a null handle nor the well-known invalid
        /// handle value), also reporting whether an invalid handle was the
        /// well-known invalid handle value.
        /// </summary>
        /// <param name="handle">
        /// The native handle value to check.
        /// </param>
        /// <param name="invalid">
        /// Upon return, this is set to true if the handle is the well-known
        /// invalid handle value, or false if it is a null handle.  This value
        /// is only meaningful when this method returns false.
        /// </param>
        /// <returns>
        /// True if the handle appears to be valid; otherwise, false.
        /// </returns>
        public static bool IsValidHandle(
            IntPtr handle,
            ref bool invalid
            )
        {
            if (handle == IntPtr.Zero)
            {
                invalid = false;
                return false;
            }

            if (handle == INVALID_HANDLE_VALUE)
            {
                invalid = true;
                return false;
            }

            return true;
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Native Module Support Methods
        /// <summary>
        /// This method returns the file extension typically used for native
        /// shared libraries on the current operating system.
        /// </summary>
        /// <returns>
        /// The file extension appropriate for native shared libraries on the
        /// current operating system, including the leading period.
        /// </returns>
        public static string GetSharedLibraryExtension()
        {
            if (PlatformOps.IsWindowsOperatingSystem())
                return FileExtension.Library;
            else if (PlatformOps.IsMacintoshOperatingSystem())
                return FileExtension.DynamicLibrary;
            else
                return FileExtension.SharedObject;
        }

        ///////////////////////////////////////////////////////////////////////

#if EMIT && NATIVE && LIBRARY
        /// <summary>
        /// This method attempts to unload the native shared library associated
        /// with the specified native module.
        /// </summary>
        /// <param name="module">
        /// The module whose underlying native shared library should be
        /// unloaded.  This module must be a native module.
        /// </param>
        /// <param name="loaded">
        /// Upon return, this contains the updated reference count for the
        /// native shared library after the unload attempt.
        /// </param>
        /// <param name="error">
        /// Upon failure, this will contain an appropriate error message.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success;
        /// <see cref="ReturnCode.Error" /> on failure.
        /// </returns>
        public static ReturnCode UnloadNativeModule(
            IModule module,
            ref int loaded,
            ref Result error
            )
        {
            if (module == null)
            {
                error = "invalid module";
                return ReturnCode.Error;
            }

            ModuleWrapper wrapper = module as ModuleWrapper;

            if (wrapper != null)
                module = wrapper.Object as IModule;

            NativeModule nativeModule = module as NativeModule;

            if (nativeModule == null)
            {
                error = "module is not native";
                return ReturnCode.Error;
            }

            return nativeModule.UnloadNoThrow(ref loaded, ref error);
        }
#endif
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Script Cancellation Support Methods
        /// <summary>
        /// This method builds the set of cancel flags used to cancel script
        /// evaluation based on the specified options.
        /// </summary>
        /// <param name="global">
        /// Non-zero to set the global cancellation state; otherwise, the local
        /// cancellation state is set.
        /// </param>
        /// <param name="interactive">
        /// Non-zero if the cancellation is being performed on behalf of an
        /// interactive user.
        /// </param>
        /// <param name="unwind">
        /// Non-zero to unwind the active call stack as part of the
        /// cancellation.
        /// </param>
        /// <param name="strict">
        /// Non-zero to stop on error during the cancellation.
        /// </param>
        /// <param name="noLock">
        /// Non-zero to skip acquiring the associated lock during the
        /// cancellation.
        /// </param>
        /// <param name="interrupt">
        /// Non-zero to use thread interruption as part of the cancellation.
        /// </param>
        /// <returns>
        /// The set of cancel flags corresponding to the specified options.
        /// </returns>
        public static CancelFlags GetCancelEvaluateFlags(
            bool global,
            bool interactive,
            bool unwind,
            bool strict,
            bool noLock,
            bool interrupt
            )
        {
            CancelFlags cancelFlags = CancelFlags.Default;

            if (global)
                cancelFlags |= CancelFlags.SetGlobalState;
            else
                cancelFlags |= CancelFlags.SetLocalState;

            if (interactive)
                cancelFlags |= CancelFlags.ForInteractive;

            if (unwind)
                cancelFlags |= CancelFlags.Unwind;

            if (strict)
                cancelFlags |= CancelFlags.StopOnError;

            if (noLock)
                cancelFlags |= CancelFlags.NoLock;

            if (interrupt)
            {
                cancelFlags |= CancelFlags.UseThreadInterrupt;

#if SHELL
                if (interactive)
                    cancelFlags |= CancelFlags.UseInteractiveThread;
#endif
            }

            return cancelFlags;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method queues a background thread that will cancel script
        /// evaluation in the specified interpreter after the specified timeout
        /// has elapsed.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter whose script evaluation should be canceled when the
        /// timeout elapses.
        /// </param>
        /// <param name="cancelFlags">
        /// The optional set of cancel flags to use when canceling script
        /// evaluation, or null to use a reasonable set of default flags.
        /// </param>
        /// <param name="timeout">
        /// The timeout, in milliseconds, to wait before canceling script
        /// evaluation.  This value must be greater than or equal to zero.
        /// </param>
        /// <param name="thread">
        /// Upon success, this contains the thread that was created and started
        /// to handle the script timeout.
        /// </param>
        /// <param name="error">
        /// Upon failure, this will contain an appropriate error message.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success;
        /// <see cref="ReturnCode.Error" /> on failure.
        /// </returns>
        public static ReturnCode QueueScriptTimeout(
            Interpreter interpreter,
            CancelFlags? cancelFlags,
            int timeout,
            ref Thread thread,
            ref Result error
            )
        {
            if (interpreter == null)
            {
                error = "invalid interpreter";
                return ReturnCode.Error;
            }

            if (timeout < 0)
            {
                error = "invalid timeout in milliseconds";
                return ReturnCode.Error;
            }

            try
            {
                ThreadOps.CreateAndOrStart(
                    interpreter, null, ScriptTimeoutThreadStart,
                    interpreter.CreateScriptTimeoutClientData(
                        null, TimeoutFlags.Default, cancelFlags,
                        timeout), false, 0, false, true, true,
                    ref thread);

                return ReturnCode.Ok;
            }
            catch (Exception e)
            {
                error = e;
                return ReturnCode.Error;
            }
        }

        ///////////////////////////////////////////////////////////////////////

        /* System.Threading.ParameterizedThreadStart */
        /// <summary>
        /// This method is the thread start routine used to implement script
        /// timeouts.  It waits for the configured timeout to elapse and then
        /// cancels script evaluation in the associated interpreter if it is
        /// still busy.
        /// </summary>
        /// <param name="state">
        /// The thread start state, which is expected to be a
        /// <see cref="ScriptTimeoutClientData" /> instance describing the
        /// interpreter, timeout, and cancel flags to use.
        /// </param>
        private static void ScriptTimeoutThreadStart(
            object state
            )
        {
            try
            {
                ScriptTimeoutClientData clientData =
                    state as ScriptTimeoutClientData;

                if (clientData == null)
                    return;

                Interpreter interpreter = clientData.Interpreter;

                if (interpreter == null)
                    return;

#if THREADING
                IEngineContext engineContext = clientData.EngineContext;
#endif

                int timeout = clientData.Timeout;

                if (timeout < 0)
                    return;

                CancelFlags cancelFlags;

                if (clientData.CancelFlags != null)
                {
                    cancelFlags = (CancelFlags)clientData.CancelFlags;
                }
                else
                {
                    //
                    // HACK: Use a reasonable set of default cancel flags.
                    //
                    cancelFlags = CancelFlags.ScriptTimeout;
                }

                //
                // HACK: Cannot use the (complex) wrapper methods here
                //       because they catch ThreadInterruptedException,
                //       et al.
                //
                HostOps.ThreadSleep(timeout); /* throw */

                //
                // HACK: If the specified interpreter does not actually
                //       appear busy at the moment, do not actually
                //       initiate script cancellation -UNLESS- the NoBusy
                //       flag was specified.
                //
                if (!FlagOps.HasFlags(
                        cancelFlags, CancelFlags.NoBusy, true) &&
                    !interpreter.InternalIsGlobalBusy)
                {
                    return;
                }

                Result result;

                if (FlagOps.HasFlags(
                        cancelFlags, CancelFlags.Unwind, true))
                {
                    result = Result.Copy(
                        Engine.EvalUnwoundTimeoutError,
                        ResultFlags.CopyValue);
                }
                else
                {
                    result = Result.Copy(
                        Engine.EvalCanceledTimeoutError,
                        ResultFlags.CopyValue);
                }

                /* IGNORED */
                interpreter.InternalCancelEvaluate(
#if THREADING
                    engineContext,
#endif
                    result, cancelFlags);
            }
            catch (ThreadAbortException e)
            {
                Thread.ResetAbort();

                TraceOps.DebugTrace(
                    e, typeof(RuntimeOps).Name,
                    TracePriority.ThreadError2);
            }
            catch (ThreadInterruptedException e)
            {
                TraceOps.DebugTrace(
                    e, typeof(RuntimeOps).Name,
                    TracePriority.ThreadError2);
            }
            catch (Exception e)
            {
                TraceOps.DebugTrace(
                    e, typeof(RuntimeOps).Name,
                    TracePriority.ThreadError);
            }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Cache Support Methods
#if CACHE_STATISTICS
        /// <summary>
        /// This method saves the cache counts from the specified cache counts
        /// object into the supplied dictionary, optionally clearing them from
        /// the cache counts object afterward.
        /// </summary>
        /// <param name="flags">
        /// The cache flags used as the key under which the saved cache counts
        /// are stored.
        /// </param>
        /// <param name="cacheCounts">
        /// The object whose cache counts should be saved.
        /// </param>
        /// <param name="move">
        /// Non-zero to clear the cache counts from the
        /// <paramref name="cacheCounts" /> object after saving them.
        /// </param>
        /// <param name="savedCacheCounts">
        /// Upon return, this dictionary contains the saved cache counts keyed
        /// by the specified cache flags.  If it is null, a new dictionary will
        /// be created.
        /// </param>
        /// <returns>
        /// True if the cache counts were saved; otherwise, false.
        /// </returns>
        public static bool MaybeSaveCacheCounts(
            CacheFlags flags,                                   /* in */
            ICacheCounts cacheCounts,                           /* in */
            bool move,                                          /* in */
            ref Dictionary<CacheFlags, long[]> savedCacheCounts /* in, out */
            )
        {
            if (cacheCounts == null)
                return false;

            long[] counts = cacheCounts.GetCacheCounts();

            if (counts == null)
                return false;

            if (savedCacheCounts == null)
                savedCacheCounts = new Dictionary<CacheFlags, long[]>();

            savedCacheCounts[flags] = counts;

            if (move)
            {
                /* IGNORED */
                cacheCounts.SetCacheCounts(null, false);
            }

            return true;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method restores previously saved cache counts from the supplied
        /// dictionary into the specified cache counts object, optionally merging
        /// them and optionally removing them from the dictionary afterward.
        /// </summary>
        /// <param name="flags">
        /// The cache flags used as the key under which the saved cache counts
        /// were stored.
        /// </param>
        /// <param name="cacheCounts">
        /// The object whose cache counts should be restored.
        /// </param>
        /// <param name="merge">
        /// Non-zero to merge the saved cache counts with the existing cache
        /// counts; otherwise, the existing cache counts are replaced.
        /// </param>
        /// <param name="move">
        /// Non-zero to remove the saved cache counts from the
        /// <paramref name="savedCacheCounts" /> dictionary after restoring
        /// them.
        /// </param>
        /// <param name="savedCacheCounts">
        /// The dictionary containing the previously saved cache counts keyed by
        /// the specified cache flags.
        /// </param>
        /// <returns>
        /// True if the cache counts were restored; otherwise, false.
        /// </returns>
        public static bool MaybeRestoreCacheCounts(
            CacheFlags flags,                                   /* in */
            ICacheCounts cacheCounts,                           /* in, out */
            bool merge,                                         /* in */
            bool move,                                          /* in */
            ref Dictionary<CacheFlags, long[]> savedCacheCounts /* in, out */
            )
        {
            if ((cacheCounts == null) || (savedCacheCounts == null))
                return false;

            long[] counts;

            if (!savedCacheCounts.TryGetValue(flags, out counts))
                return false;

            if (counts == null)
                return false;

            /* IGNORED */
            cacheCounts.SetCacheCounts(counts, merge);

            if (move)
            {
                if (savedCacheCounts.Remove(flags) &&
                    (savedCacheCounts.Count == 0))
                {
                    savedCacheCounts.Clear();
                    savedCacheCounts = null;
                }
            }

            return true;
        }
#endif
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Trusted Update Support Methods
#if NETWORK
        /// <summary>
        /// This method queries the current trusted and exclusive status of the
        /// software update certificate, recording any errors and optionally
        /// adding a human-readable status message to the result list.
        /// </summary>
        /// <param name="needResult">
        /// Non-zero to add a human-readable status message describing the
        /// trusted and exclusive status to the result list.
        /// </param>
        /// <param name="wasTrusted">
        /// Upon return, this contains the current trusted status of the
        /// software update certificate, or null if it is unknown.
        /// </param>
        /// <param name="wasExclusive">
        /// Upon return, this contains the current exclusive mode status of the
        /// software update certificate, or null if it is unknown.
        /// </param>
        /// <param name="errorCount">
        /// Upon return, this is incremented by the number of errors that were
        /// encountered while querying the status.
        /// </param>
        /// <param name="results">
        /// Upon return, this contains any status or error messages that were
        /// produced.  If it is null, a new result list will be created as
        /// needed.
        /// </param>
        private static void RefreshTrustedUpdateStatus(
            bool needResult,        /* in */
            out bool? wasTrusted,   /* out */
            out bool? wasExclusive, /* out */
            ref int errorCount,     /* in, out */
            ref ResultList results  /* in, out */
            )
        {
            wasTrusted = UpdateOps.IsTrusted();

            if (wasTrusted == null)
            {
                if (results == null)
                    results = new ResultList();

                results.Add(
                    "software update certificate trusted status is unknown");

                errorCount++;
            }

            wasExclusive = UpdateOps.IsExclusive();

            if (wasExclusive == null)
            {
                if (results == null)
                    results = new ResultList();

                results.Add(
                    "software update certificate exclusive mode is unknown");

                errorCount++;
            }

            if (needResult && (wasTrusted != null) && (wasExclusive != null))
            {
                if (results == null)
                    results = new ResultList();

                results.Add(String.Format(
                    "software update certificate is {0}{1}",
                    (bool)wasTrusted ? "trusted" : "untrusted",
                    (bool)wasExclusive ? " exclusively" : String.Empty));
            }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method queries and optionally updates the trusted and exclusive
        /// status of the software update certificate.
        /// </summary>
        /// <param name="trusted">
        /// The desired trusted status of the software update certificate, or
        /// null to leave the trusted status unchanged.
        /// </param>
        /// <param name="exclusive">
        /// The desired exclusive mode status of the software update
        /// certificate, or null to leave the exclusive mode status unchanged.
        /// </param>
        /// <param name="results">
        /// Upon return, this contains any status or error messages that were
        /// produced.  If it is null, a new result list will be created as
        /// needed.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success;
        /// <see cref="ReturnCode.Error" /> on failure.
        /// </returns>
        public static ReturnCode GetOrSetTrustedUpdateStatus(
            bool? trusted,         /* in */
            bool? exclusive,       /* in */
            ref ResultList results /* in, out */
            )
        {
            int errorCount; /* REUSED */
            bool? wasTrusted; /* REUSED */
            bool? wasExclusive; /* REUSED */

            errorCount = 0;

            RefreshTrustedUpdateStatus(
                false, out wasTrusted, out wasExclusive, ref errorCount,
                ref results);

            if (errorCount > 0)
                return ReturnCode.Error;

            ///////////////////////////////////////////////////////////////////

            Result error; /* REUSED */

            ///////////////////////////////////////////////////////////////////

            if (trusted != null)
            {
                if ((bool)trusted != wasTrusted)
                {
                    error = null;

                    if (UpdateOps.SetTrusted(
                            (bool)trusted, ref error) != ReturnCode.Ok)
                    {
                        if (results == null)
                            results = new ResultList();

                        results.Add(error);
                        errorCount++;
                    }
                }
                else
                {
                    if (results == null)
                        results = new ResultList();

                    results.Add(String.Format(
                        "software update certificate is already {0}{1}",
                        (bool)wasTrusted ? "TRUSTED" : "UNTRUSTED",
                        (bool)wasExclusive ? " exclusively" : String.Empty));

                    errorCount++;
                }
            }

            ///////////////////////////////////////////////////////////////////

            if (exclusive != null)
            {
                if ((bool)exclusive != wasExclusive)
                {
                    error = null;

                    if (UpdateOps.SetExclusive(
                            (bool)exclusive, ref error) != ReturnCode.Ok)
                    {
                        if (results == null)
                            results = new ResultList();

                        results.Add(error);
                        errorCount++;
                    }
                }
                else
                {
                    if (results == null)
                        results = new ResultList();

                    results.Add(String.Format(
                        "software update certificate is already {0}{1}",
                        (bool)wasTrusted ? "trusted" : "untrusted",
                        (bool)wasExclusive ? " EXCLUSIVELY" : String.Empty));

                    errorCount++;
                }
            }

            ///////////////////////////////////////////////////////////////////

            if (errorCount > 0)
                return ReturnCode.Error;

            ///////////////////////////////////////////////////////////////////

            RefreshTrustedUpdateStatus(
                true, out wasTrusted, out wasExclusive, ref errorCount,
                ref results);

            if (errorCount > 0)
                return ReturnCode.Error;
            else
                return ReturnCode.Ok;
        }
#endif
        #endregion
    }
}
