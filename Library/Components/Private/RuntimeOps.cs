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
using System.Threading;
using Eagle._Attributes;
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

using DelegatePair = System.Collections.Generic.KeyValuePair<
    System.Delegate, Eagle._Components.Public.MethodFlags>;

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
    [ObjectId("52155f4f-322b-4389-aacd-166fe334d164")]
    internal static class RuntimeOps
    {
        #region Synchronization Objects
        private static readonly object syncRoot = new object();
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Private Constants
        #region Property Value Defaults
        private static readonly bool ThrowOnFeatureNotSupported = true;
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Encoding Constants
        //
        // WARNING: Do not change this as it must be a pass-through one-byte
        //          per character encoding.
        //
        private static readonly Encoding RawEncoding = OneByteEncoding.OneByte;
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Resource Handling
        private const string InvalidInterpreterResourceManager =
            "invalid interpreter resource manager";

        private const string InvalidPluginResourceManager =
            "invalid plugin resource manager";

        private const string InvalidAssemblyResourceManager =
            "invalid assembly resource manager";

        ///////////////////////////////////////////////////////////////////////

        private static readonly string SymbolsFormat = "{0}_Symbols";
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Native Pointer Handling
        private static readonly IntPtr INVALID_HANDLE_VALUE = new IntPtr(-1);
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
        private static int checkedNoNativeStack = 0;

        ///////////////////////////////////////////////////////////////////////

        //
        // NOTE: When this is non-zero, all native stack checking will be
        //       disabled.
        //
        // HACK: This is purposely not read-only.
        //
        private static int noNativeStack = 0;

        ///////////////////////////////////////////////////////////////////////

        private static LocalDataStoreSlot stackPtrSlot; /* ThreadSpecificData */
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
        private static int NoStackLevels = 100;
        private static int NoStackParserLevels = 100;
        private static int NoStackExpressionLevels = 100;
#endif
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Interpreter Locking
#if DEBUG
        //
        // HACK: This is not read-only.
        //
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
        private static bool forceMono = false;
#endif
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Random Number Support
        //
        // NOTE: Cached instance of cryptographic random number generator.
        //
        private static RandomNumberGenerator randomNumberGenerator;
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Loader Exception Handling
        //
        // HACK: When this is non-zero, any loader related exceptions that
        //       are encountered by this class will be reported in detail.
        //
        private static bool VerboseExceptions = true;
        #endregion
        #endregion

        ///////////////////////////////////////////////////////////////////////

#if NATIVE
        #region Static Constructor
        static RuntimeOps()
        {
            MaybeInitialize();
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region AppDomain Initialization
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

        private static bool IsNativeStackEnabled()
        {
            return Interlocked.CompareExchange(ref noNativeStack, 0, 0) <= 0;
        }

        ///////////////////////////////////////////////////////////////////////

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
        public static int GetHashCode(object value)
        {
            return RuntimeHelpers.GetHashCode(value);
        }

        ///////////////////////////////////////////////////////////////////////

        public static int GetHashCode(IObject @object)
        {
            if (@object == null)
                return HashCode.Invalid;

            return GetHashCode(@object.Value);
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Process Support Methods
        public static ReturnCode Exit(
            Interpreter interpreter, /* in: OPTIONAL */
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
            if ((interpreter != null) && interpreter.CanExit(exitCode,
                    force, fail, message, ref error) != ReturnCode.Ok)
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
                Thread.CurrentThread.IsThreadPoolThread)
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

        public static void RefreshNativeStackPointers(
            bool initialize
            )
        {
            UIntPtr innerStackPtr = UIntPtr.Zero;
            UIntPtr outerStackPtr = UIntPtr.Zero;

            RefreshNativeStackPointers(
                initialize, ref innerStackPtr, ref outerStackPtr);

            TraceOps.DebugTrace(String.Format(
                "RefreshNativeStackPointers: initialize = {0}, " +
                "innerStackPtr = {1}, outerStackPtr = {2}", initialize,
                innerStackPtr, outerStackPtr), typeof(RuntimeOps).Name,
                TracePriority.ThreadDebug2);
        }

        ///////////////////////////////////////////////////////////////////////

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

            //
            // NOTE: Emit a diagnostic message with updated native stack
            //       information.
            //
            TraceOps.DebugTrace(String.Format(
                "CreateOrUpdateStackSize: updated {0}", stackSize),
                typeof(RuntimeOps).Name, TracePriority.ThreadDebug2);

            //
            // NOTE: Return the created (or updated) stack size object
            //       to the caller.
            //
            return stackSize;
        }

        ///////////////////////////////////////////////////////////////////////

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

        public static ReturnCode CheckForStackSpace(
            Interpreter interpreter
            ) /* THREAD-SAFE */
        {
            return CheckForStackSpace(
                interpreter, Engine.GetExtraStackSpace());
        }

        ///////////////////////////////////////////////////////////////////////

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

                if ((bestIndex == Index.Invalid) || (minSize < bestMinSize))
                {
                    bestIndex = index;
                    bestMinSize = minSize;
                }
            }

            return (bestIndex != Index.Invalid) ?
                allKeySizes[bestIndex] : null;
        }

        ///////////////////////////////////////////////////////////////////////

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

                if ((bestIndex == Index.Invalid) || (maxSize > bestMaxSize))
                {
                    bestIndex = index;
                    bestMaxSize = maxSize;
                }
            }

            return (bestIndex != Index.Invalid) ?
                allKeySizes[bestIndex] : null;
        }

        ///////////////////////////////////////////////////////////////////////

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

        public static bool ShouldCheckStrongNameVerified()
        {
            return !CommonOps.Environment.DoesVariableExist(EnvVars.NoVerified);
        }

        ///////////////////////////////////////////////////////////////////////

        public static bool IsStrongNameVerified(
            byte[] bytes,
            bool force
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
                fileName = PathOps.GetTempFileName(); /* throw */

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

        public static bool IsStrongNameVerified(
            string fileName,
            bool force
            )
        {
            Version runtimeVersion = CommonOps.Runtime.GetRuntimeVersion();

            ///////////////////////////////////////////////////////////////////

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
                        "SUCCESS using CoreCLR {1}.",
                        FormatOps.WrapOrNull(fileName),
                        FormatOps.VMajorMinorOrNull(runtimeVersion)),
                        typeof(RuntimeOps).Name,
                        TracePriority.SecurityDebug2);

                    return true;
                }
                else
                {
                    TraceOps.DebugTrace(String.Format(
                        "IsStrongNameVerified: file {0} " +
                        "FAILURE using CoreCLR {1}.",
                        FormatOps.WrapOrNull(fileName),
                        FormatOps.VMajorMinorOrNull(runtimeVersion)),
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
                        "SUCCESS using Mono {1}.",
                        FormatOps.WrapOrNull(fileName),
                        FormatOps.VMajorMinorOrNull(runtimeVersion)),
                        typeof(RuntimeOps).Name,
                        TracePriority.SecurityDebug2);

                    return true;
                }
                else
                {
                    TraceOps.DebugTrace(String.Format(
                        "IsStrongNameVerified: file {0} " +
                        "FAILURE using Mono {1}.",
                        FormatOps.WrapOrNull(fileName),
                        FormatOps.VMajorMinorOrNull(runtimeVersion)),
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
                    "SUCCESS using CLRv{1} ({2}).",
                    FormatOps.WrapOrNull(fileName), clrVersion,
                    FormatOps.VMajorMinorOrNull(runtimeVersion)),
                    typeof(RuntimeOps).Name,
                    TracePriority.SecurityDebug2);

                return true;
            }
            else
            {
                TraceOps.DebugTrace(String.Format(
                    "IsStrongNameVerified: file {0} " +
                    "FAILURE using CLRv{1} ({2}).",
                    FormatOps.WrapOrNull(fileName), clrVersion,
                    FormatOps.VMajorMinorOrNull(runtimeVersion)),
                    typeof(RuntimeOps).Name,
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

            return !CommonOps.Environment.DoesVariableExist(EnvVars.NoUpdates);
        }

        ///////////////////////////////////////////////////////////////////////

        public static bool ShouldCheckFileTrusted()
        {
            return !CommonOps.Environment.DoesVariableExist(EnvVars.NoTrusted);
        }

        ///////////////////////////////////////////////////////////////////////

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
        public static bool ShouldTrustNativeLibrary(
            string fileName
            )
        {
            //
            // NOTE: If the primary assembly is not "trusted", allow any
            //       native library to load.
            //
            // NOTE: For the purposes of this ShouldCheckCoreTrusted() call, the
            //       "Eagle Native Utility Library" (Spilornis) *IS* considered
            //       to be part of the "Eagle Core Library".
            //
            if (!ShouldCheckCoreFileTrusted() ||
                !IsFileTrusted(GlobalState.GetAssemblyLocation(), IntPtr.Zero))
            {
                return true;
            }

            //
            // NOTE: Otherwise, if the native library is "trusted", allow
            //       it to load.
            //
            if (!ShouldCheckFileTrusted() ||
                IsFileTrusted(fileName, IntPtr.Zero))
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

        public static bool IsFileTrusted(
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
                fileName = PathOps.GetTempFileName(); /* throw */

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

                    return IsFileTrusted(fileName, stream.Handle);
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

        public static bool IsFileTrusted(
            string fileName,
            IntPtr fileHandle
            )
        {
            return IsFileTrusted(
                fileName, fileHandle, false, false, true, false);
        }

        ///////////////////////////////////////////////////////////////////////

        private static bool IsFileTrusted(
            string fileName,
            IntPtr fileHandle,
            bool userInterface,
            bool userPrompt,
            bool revocation,
            bool install
            )
        {
            Version runtimeVersion = CommonOps.Runtime.GetRuntimeVersion();

            ///////////////////////////////////////////////////////////////////

            #region .NET Core Support
#if NET_STANDARD_20
            if (CommonOps.Runtime.IsDotNetCore())
            {
#if NATIVE
                if (PlatformOps.IsWindowsOperatingSystem())
                    goto native;
#endif

                if (WinTrustDotNet.IsFileTrusted(
                        fileName, fileHandle, userInterface, userPrompt,
                        revocation, install))
                {
                    TraceOps.DebugTrace(String.Format(
                        "IsFileTrusted: file {0} SUCCESS using CoreCLR {1}.",
                        FormatOps.WrapOrNull(fileName),
                        FormatOps.VMajorMinorOrNull(runtimeVersion)),
                        typeof(RuntimeOps).Name,
                        TracePriority.SecurityDebug2);

                    return true;
                }
                else
                {
                    TraceOps.DebugTrace(String.Format(
                        "IsFileTrusted: file {0} FAILURE using CoreCLR {1}.",
                        FormatOps.WrapOrNull(fileName),
                        FormatOps.VMajorMinorOrNull(runtimeVersion)),
                        typeof(RuntimeOps).Name,
                        TracePriority.SecurityError);

                    return false;
                }
            }
#endif
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
                        "IsFileTrusted: file {0} SUCCESS using Mono {1}.",
                        FormatOps.WrapOrNull(fileName),
                        FormatOps.VMajorMinorOrNull(runtimeVersion)),
                        typeof(RuntimeOps).Name,
                        TracePriority.SecurityDebug2);

                    return true;
                }
                else
                {
                    TraceOps.DebugTrace(String.Format(
                        "IsFileTrusted: file {0} FAILURE using Mono {1}.",
                        FormatOps.WrapOrNull(fileName),
                        FormatOps.VMajorMinorOrNull(runtimeVersion)),
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
#if NET_STANDARD_20 || (MONO && MONO_BUILD)
        native:
#endif

            if (WinTrustOps.IsFileTrusted(
                    fileName, fileHandle, userInterface, userPrompt,
                    revocation, install))
            {
                TraceOps.DebugTrace(String.Format(
                    "IsFileTrusted: file {0} SUCCESS using CLR {1}.",
                    FormatOps.WrapOrNull(fileName),
                    FormatOps.VMajorMinorOrNull(runtimeVersion)),
                    typeof(RuntimeOps).Name,
                    TracePriority.SecurityDebug2);

                return true;
            }
            else
            {
                TraceOps.DebugTrace(String.Format(
                    "IsFileTrusted: file {0} FAILURE using CLR {1}.",
                    FormatOps.WrapOrNull(fileName),
                    FormatOps.VMajorMinorOrNull(runtimeVersion)),
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
            return false;
#endif
        }

        ///////////////////////////////////////////////////////////////////////

        public static string GetVendor(
            bool noCache
            )
        {
            return GetCertificateSubject(GlobalState.GetAssemblyLocation(),
                null, ShouldCheckCoreFileTrusted(), true, noCache);
        }

        ///////////////////////////////////////////////////////////////////////

        public static string GetCertificateSubject(
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
                    if ((certificate2 != null) &&
                        IsFileTrusted(fileName, IntPtr.Zero))
                    {
                        StringBuilder result = StringOps.NewStringBuilder();

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
                        return result.ToString();
                    }
                }
            }

            return null;
        }

        ///////////////////////////////////////////////////////////////////////

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
                    simpleName = Path.GetFileNameWithoutExtension(fileName);
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

        public static string PluginFlagsToPrefix(
            PluginFlags flags
            )
        {
            StringBuilder result = StringOps.NewStringBuilder();

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

            return result.ToString();
        }

        ///////////////////////////////////////////////////////////////////////

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
        private static void AppendCommandLineArgument(
            StringBuilder builder,
            string arg,
            bool quoteAll
            )
        {
            if ((builder == null) || (arg == null))
                return;

            char[] special = {
                Characters.Space, Characters.QuotationMark,
                Characters.Backslash
            };

            if (builder.Length > 0)
                builder.Append(Characters.Space);

            bool wrap = quoteAll ||
                (arg.IndexOfAny(special) != Index.Invalid);

            if (wrap)
                builder.Append(Characters.QuotationMark);

            int length = arg.Length;

            for (int index = 0; index < length; index++)
            {
                if (arg[index] == Characters.QuotationMark)
                {
                    builder.Append(Characters.Backslash);
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
                builder.Append(Characters.QuotationMark);
        }

        ///////////////////////////////////////////////////////////////////////

        public static string BuildCommandLine(
            IEnumerable<string> args,
            bool quoteAll
            )
        {
            if (args == null)
                return null;

            StringBuilder builder = StringOps.NewStringBuilder();

            foreach (string arg in args)
                AppendCommandLineArgument(builder, arg, quoteAll);

            return builder.ToString();
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Alias Support Methods
        public static ReturnCode GetInterpreterAliasArguments(
            string interpreterName,
            ObjectOptionType objectOptionType, /* NOT USED */
            ref ArgumentList arguments,
            ref Result error /* NOT USED */
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
        public static ReturnCode GetLibraryAliasArguments(
            string delegateName,
            ObjectOptionType objectOptionType, /* NOT USED */
            ref ArgumentList arguments,
            ref Result error /* NOT USED */
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

        private static string GetObjectAliasSubCommand(
            ObjectOptionType objectOptionType
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

        public static ReturnCode GetObjectAliasArguments(
            string objectName,
            ObjectOptionType objectOptionType,
            ref ArgumentList arguments,
            ref Result error
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
        public static ReturnCode GetTclAliasArguments(
            string interpName,
            ObjectOptionType objectOptionType, /* NOT USED */
            ref ArgumentList arguments,
            ref Result error /* NOT USED */
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
        public static IAlias NewAlias(
            string name,
            CommandFlags flags,
            AliasFlags aliasFlags,
            IClientData clientData,
            string nameToken,
            Interpreter sourceInterpreter,
            Interpreter targetInterpreter,
            INamespace sourceNamespace,
            INamespace targetNamespace,
            IExecute target,
            ArgumentList arguments,
            OptionDictionary options,
            int startIndex
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

        public static string GetAnyString(
            Interpreter interpreter,
            IPlugin plugin,
            ResourceManager resourceManager,
            string name,
            CultureInfo cultureInfo,
            ref Result error
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

        public static ReturnCode GetPublicKeyToken(
            string value,
            CultureInfo cultureInfo,
            ref byte[] publicKeyToken,
            ref Result error
            )
        {
            ulong ulongValue = 0;

            if (Value.GetUnsignedWideInteger2(
                    value, ValueFlags.AnyWideInteger | ValueFlags.Unsigned,
                    cultureInfo, ref ulongValue, ref error) != ReturnCode.Ok)
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

            IFileSystemHost fileSystemHost = interpreter.Host; /* throw */

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

            if (fileSystemHost.GetData(
                    resourceName, DataFlags.Plugin, ref scriptFlags,
                    ref localClientData, ref localResult) != ReturnCode.Ok)
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
            fileSystemHost.GetData(
                String.Format(SymbolsFormat, resourceName), DataFlags.Plugin,
                ref scriptFlags, ref localClientData, ref localResult);

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

            foreach (KeyValuePair<string, _Wrappers.Plugin> pair in plugins)
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
        private static bool IsGenuine()
        {
            return ArrayOps.Equals(
                License.Hash, HashOps.HashString(null, (string)null,
                StringOps.ForceCarriageReturns(License.Summary +
                License.Text)));
        }

        ///////////////////////////////////////////////////////////////////////

        private static string GetGenuine()
        {
            return IsGenuine() ? Vars.Version.GenuineValue : null;
        }

        ///////////////////////////////////////////////////////////////////////

        public static string GetFileTrusted(
            string fileName
            )
        {
            if (GetCertificateSubject(
                    fileName, null, ShouldCheckFileTrusted(), true,
                    true) != null)
            {
                return Vars.Version.TrustedValue;
            }

            return null;
        }

        ///////////////////////////////////////////////////////////////////////

        public static bool IsOfficial()
        {
#if OFFICIAL
            return true;
#else
            return false;
#endif
        }

        ///////////////////////////////////////////////////////////////////////

        public static bool IsStable()
        {
#if STABLE
            return true;
#else
            return false;
#endif
        }

        ///////////////////////////////////////////////////////////////////////

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

                bool thisAssembly; /* NOT USED */
                string typeName; /* NOT USED */
                string methodName;

                DebugOps.GetMethodName(
                    1, null, false, true, null, out thisAssembly,
                    out typeName, out methodName);

                return String.Format("{1}{0}", String.Format(
                    format, version, suffix), methodName);
            }
        }

        ///////////////////////////////////////////////////////////////////////

        private static string GetAssemblyTextOrSuffix()
        {
            return GetAssemblyTextOrSuffix(GlobalState.GetAssembly());
        }

        ///////////////////////////////////////////////////////////////////////

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

        private static string GetTclVersionString()
        {
            return String.Format("{0} {1}", TclVars.Package.Name,
                TclVars.Package.PatchLevelValue);
        }

        ///////////////////////////////////////////////////////////////////////

        private static void AddCoreVersionInformation(
            Assembly assembly,  /* in */
            string fileName,    /* in */
            bool safe,          /* in */
            ref StringList list /* in, out */
            )
        {
            if (list == null)
                list = new StringList();

            list.Add(GlobalState.GetPackageName());
            list.Add(GlobalState.GetAssemblyVersionString());
            list.Add(GetFileTrusted(fileName));
            list.Add(GetGenuine());

            if (safe)
            {
                list.Add((string)null);
                list.Add((string)null);
                list.Add((string)null);
                list.Add((string)null);
                list.Add((string)null);
            }
            else
            {
                list.Add(Vars.Version.OfficialValue);
                list.Add(Vars.Version.StableValue);
                list.Add(SharedAttributeOps.GetAssemblyTag(assembly));
                list.Add(SharedAttributeOps.GetAssemblyRelease(assembly));
                list.Add(GetAssemblyTextOrSuffix(assembly));
            }

            list.Add(AttributeOps.GetAssemblyConfiguration(assembly));
            list.Add(GetTclVersionString());

            if (safe)
            {
                list.Add((string)null);
                list.Add((string)null);
                list.Add((string)null);
                list.Add((string)null);
                list.Add((string)null);
                list.Add((string)null);
            }
            else
            {
                list.Add(FormatOps.Iso8601DateTime(
                    SharedAttributeOps.GetAssemblyDateTime(assembly), true));

                list.Add(
                    SharedAttributeOps.GetAssemblySourceId(assembly));

                list.Add(
                    SharedAttributeOps.GetAssemblySourceTimeStamp(assembly));

                list.Add(CommonOps.Runtime.GetRuntimeNameAndVersion());
                list.Add(PlatformOps.GetOperatingSystemName());
                list.Add(PlatformOps.GetMachineName());
            }
        }

        ///////////////////////////////////////////////////////////////////////

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

            foreach (KeyValuePair<string, _Wrappers.Plugin> pair in plugins)
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

        public static ReturnCode GetVersion(
            Interpreter interpreter,   /* in */
            VersionFlags versionFlags, /* in */
            ref Result result          /* out */
            )
        {
            StringList list = null;

            if (FlagOps.HasFlags(versionFlags, VersionFlags.Core, true))
            {
                AddCoreVersionInformation(
                    GlobalState.GetAssembly(),
                    GlobalState.GetAssemblyLocation(),
                    (interpreter != null) ?
                        interpreter.InternalIsSafe() : false,
                    ref list);
            }

            if (FlagOps.HasFlags(versionFlags, VersionFlags.Plugins, true))
                MaybeAddAllPluginStatus(interpreter, ref list);

            result = list;
            return ReturnCode.Ok;
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Processor Information Methods
        public static string GetProcessorArchitecture()
        {
            string processorArchitecture;

            if (PlatformOps.IsWindowsOperatingSystem())
            {
                processorArchitecture = CommonOps.Environment.GetVariable(
                    EnvVars.ProcessorArchitecture);
            }
            else
            {
                //
                // HACK: Technically, this may not be 100% accurate.
                //
                processorArchitecture = PlatformOps.GetMachineName();
            }

            //
            // HACK: Check for an "impossible" situation.  If the pointer size
            //       is 32-bits, the processor architecture cannot be "AMD64".
            //       In that case, we are almost certainly hitting a bug in the
            //       operating system and/or Visual Studio that causes the
            //       PROCESSOR_ARCHITECTURE environment variable to contain the
            //       wrong value in some circumstances.  There are several
            //       reports of this issue from users on StackOverflow.
            //
            if ((IntPtr.Size == sizeof(int)) &&
                SharedStringOps.SystemNoCaseEquals(processorArchitecture, "AMD64"))
            {
                //
                // NOTE: When tracing is enabled, save the originally detected
                //       processor architecture before changing it.
                //
                string savedProcessorArchitecture = processorArchitecture;

                //
                // NOTE: We know that operating systems that return "AMD64" as
                //       the processor architecture are actually a superset of
                //       the "x86" processor architecture; therefore, return
                //       "x86" when the pointer size is 32-bits.
                //
                processorArchitecture = "x86";

                //
                // NOTE: Show that we hit a fairly unusual situation (i.e. the
                //       "wrong" processor architecture was detected).
                //
                TraceOps.DebugTrace(String.Format(
                    "Detected {0}-bit process pointer size with processor " +
                    "architecture {1}, using processor architecture " +
                    "{2} instead...", PlatformOps.GetProcessBits(),
                    FormatOps.WrapOrNull(savedProcessorArchitecture),
                    FormatOps.WrapOrNull(processorArchitecture)),
                    typeof(RuntimeOps).Name, TracePriority.StartupDebug);
            }

            return processorArchitecture;
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Class Factory Methods
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

        public static IProcedure NewProcedure(
            Interpreter interpreter,
            string name,
            string group,
            string description,
            ProcedureFlags flags,
            ArgumentList arguments,
            ArgumentDictionary namedArguments,
            string body,
            IScriptLocation location,
            IClientData clientData,
            ref Result error
            )
        {
            IProcedureData procedureData = new ProcedureData(
                name, group, description, flags, arguments,
                namedArguments, body, location, clientData, 0);

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

        #region Resolver Support Methods
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

        public static bool AreNamespacesEnabled(
            CreateFlags createFlags
            )
        {
            return FlagOps.HasFlags(
                createFlags, CreateFlags.UseNamespaces, true);
        }

        ///////////////////////////////////////////////////////////////////////

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
                    null, null, null, ClientData.Empty, interpreter, 0),
                    frame, @namespace);
            }
            else
            {
                return new _Resolvers.Core(new ResolveData(
                    null, null, null, ClientData.Empty, interpreter, 0));
            }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Reflection Support Methods
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

        public static PluginFlags GetSkipCheckPluginFlags()
        {
            PluginFlags result = PluginFlags.None;

            if (!ShouldCheckStrongNameVerified())
                result |= PluginFlags.SkipVerified;

            if (!ShouldCheckFileTrusted())
                result |= PluginFlags.SkipTrusted;

            return result;
        }

        ///////////////////////////////////////////////////////////////////////

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
                    interpreter, ref result) != ReturnCode.Ok)
            {
                error = result;
                return false;
            }
#endif

            return true;
        }

        ///////////////////////////////////////////////////////////////////////

        public static PluginFlags GetAssemblyPluginFlags(
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
                            IsStrongNameVerified(assemblyBytes, true))
                        {
                            result |= PluginFlags.Verified;
                        }
                    }
                    else
                    {
                        result |= PluginFlags.SkipVerified;
                    }
                }
            }
#endif

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
                        if (IsFileTrusted(assemblyBytes))
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

        public static PluginFlags GetAssemblyPluginFlags(
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
            }
#endif

            ///////////////////////////////////////////////////////////////////

#if !NET_STANDARD_20
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
                    if (IsFileTrusted(assembly.Location, IntPtr.Zero))
                    {
                        result |= PluginFlags.Trusted;
                    }
                }
                else
                {
                    result |= PluginFlags.SkipTrusted;
                }
            }
#endif

            ///////////////////////////////////////////////////////////////////

            return result;
        }

        ///////////////////////////////////////////////////////////////////////

#if APPDOMAINS || ISOLATED_INTERPRETERS || ISOLATED_PLUGINS
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

        private static IPluginData PreviewPluginData(
            Interpreter interpreter, /* in */
            string fileName,         /* in */
            string typeName,         /* in */
            PluginFlags pluginFlags, /* in */
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
                    fileName, null, pluginFlags, pluginLoaderFlags,
                    ref @delegate, ref error);

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

        private static IPluginData PreviewPluginData(
            Interpreter interpreter, /* in */
            byte[] assemblyBytes,    /* in */
            string typeName,         /* in */
            PluginFlags pluginFlags, /* in */
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
                    assemblyBytes, null, pluginFlags, pluginLoaderFlags,
                    ref @delegate, ref error);

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

        public static ReturnCode PreviewPluginFlagsAndUpdateUri(
            Interpreter interpreter,     /* in */
            string fileName,             /* in */
            string typeName,             /* in */
            ref PluginFlags pluginFlags, /* in, out */
            ref IPluginData pluginData,  /* out */
            ref Uri updateUri,           /* out */
            ref Result error             /* out */
            )
        {
            pluginData = PreviewPluginData(
                interpreter, fileName, typeName, pluginFlags,
                ref error);

            if (pluginData == null)
                return ReturnCode.Error;

            pluginFlags = pluginData.Flags;
            updateUri = pluginData.UpdateUri;

            return ReturnCode.Ok;
        }

        ///////////////////////////////////////////////////////////////////////

        public static ReturnCode PreviewPluginFlagsAndUpdateUri(
            Interpreter interpreter,     /* in */
            byte[] assemblyBytes,        /* in */
            string typeName,             /* in */
            ref PluginFlags pluginFlags, /* in, out */
            ref IPluginData pluginData,  /* out */
            ref Uri updateUri,           /* out */
            ref Result error             /* out */
            )
        {
            pluginData = PreviewPluginData(
                interpreter, assemblyBytes, typeName, pluginFlags,
                ref error);

            if (pluginData == null)
                return ReturnCode.Error;

            pluginFlags = pluginData.Flags;
            updateUri = pluginData.UpdateUri;

            return ReturnCode.Ok;
        }

        ///////////////////////////////////////////////////////////////////////

        public static ReturnCode PreviewPluginResources(
            Interpreter interpreter,                /* in */
            string fileName,                        /* in */
            StringList patterns,                    /* in */
            PluginFlags pluginFlags,                /* in */
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
                    fileName, patterns, pluginFlags, pluginLoaderFlags,
                    ref @delegate, ref error);

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

        public static ReturnCode PreviewPluginResources(
            Interpreter interpreter,                /* in */
            byte[] assemblyBytes,                   /* in */
            StringList patterns,                    /* in */
            PluginFlags pluginFlags,                /* in */
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
                    assemblyBytes, patterns, pluginFlags, pluginLoaderFlags,
                    ref @delegate, ref error);

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

        private static string GetDelegateName(
            DelegateDictionary delegates,
            MethodInfo methodInfo,
            ParameterInfo[] parameterInfo
            )
        {
            if (methodInfo == null)
                return null;

            string methodName = methodInfo.Name;
            StringBuilder builder = StringOps.NewStringBuilder();

            builder.Append(methodName);

            if ((parameterInfo != null) &&
                (delegates != null) &&
                delegates.ContainsKey(methodName))
            {
                builder.AppendFormat(
                    "{0}{1}", Characters.Underscore,
                    parameterInfo.Length);
            }

            return builder.ToString();
        }

        ///////////////////////////////////////////////////////////////////////

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

        ///////////////////////////////////////////////////////////////////////

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

        private static bool IsReallyNonCommand(
            Type type
            )
        {
            if ((type == typeof(_Commands.Default)) ||
                (type == typeof(_Commands._Delegate)) ||
                (type == typeof(_Commands.SubDelegate)) ||
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

        public static bool IsReallyNonCommandName(
            string typeName
            )
        {
            if (SharedStringOps.SystemEquals(
                    typeName, typeof(_Commands.Default).FullName) ||
                SharedStringOps.SystemEquals(
                    typeName, typeof(_Commands._Delegate).FullName) ||
                SharedStringOps.SystemEquals(
                    typeName, typeof(_Commands.SubDelegate).FullName) ||
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

        private static ReturnCode PopulateBuiltInCommands(
            Interpreter interpreter,
            IRuleSet ruleSet,
            IPlugin plugin,
            CommandFlags? commandFlags,
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

                string name = names[index];

                if (name == null)
                    name = ScriptOps.TypeNameToEntityName(type);

                if ((ruleSet != null) && !ruleSet.ApplyRules(
                        interpreter, IdentifierKind.Command, name))
                {
                    continue;
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
                    localCommandFlags, plugin, 0));
            }

            return ReturnCode.Ok;
        }

        ///////////////////////////////////////////////////////////////////////

        private static ReturnCode PopulatePluginTypes(
            IPlugin plugin,
            PluginFlags pluginFlags,
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

            bool verbose = FlagOps.HasFlags(
                pluginFlags, PluginFlags.Verbose, true);

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

        private static ReturnCode PopulatePluginCommands(
            Interpreter interpreter,
            IRuleSet ruleSet,
            IPlugin plugin,
            TypeList types,
            PluginFlags pluginFlags,
            CommandFlags? commandFlags,
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

            bool verbose = FlagOps.HasFlags(
                pluginFlags, PluginFlags.Verbose, true);

            TypeList localTypes = null;

            if (types != null)
            {
                localTypes = types;
            }
            else if (PopulatePluginTypes(
                    plugin, pluginFlags, ref localTypes,
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

                string name = AttributeOps.GetObjectName(type);

                if (name == null)
                    name = ScriptOps.TypeNameToEntityName(type);

                if ((ruleSet != null) && !ruleSet.ApplyRules(
                        interpreter, IdentifierKind.Command, name))
                {
                    continue;
                }

                commands.Add(new CommandData(
                    name, null, null, null, type.FullName,
                    localCommandFlags, plugin, 0));
            }

            return ReturnCode.Ok;
        }

        ///////////////////////////////////////////////////////////////////////

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

                if ((ruleSet != null) && !ruleSet.ApplyRules(
                        interpreter, IdentifierKind.Policy, name))
                {
                    continue;
                }

                policies.Add(new PolicyData(
                    name, null, null, null, type.FullName,
                    methodInfo.Name, bindingFlags, pair.Value,
                    PolicyFlags.None, plugin, 0));
            }

            return ReturnCode.Ok;
        }

        ///////////////////////////////////////////////////////////////////////

        public static ReturnCode PopulatePluginEntities(
            Interpreter interpreter,
            IPlugin plugin,
            TypeList types,
            IRuleSet ruleSet,
            PluginFlags pluginFlags,
            CommandFlags? commandFlags,
            bool useBuiltIn,
            bool noCommands,
            bool noPolicies,
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
                    plugin, pluginFlags,
                    ref localTypes, ref error);

                if (code != ReturnCode.Ok)
                    return code;
            }

            if (!noCommands)
            {
                if (useBuiltIn)
                {
                    code = PopulateBuiltInCommands(
                        interpreter, ruleSet, plugin,
                        commandFlags, ref error);

                    if (code != ReturnCode.Ok)
                        return code;
                }
                else
                {
                    code = PopulatePluginCommands(
                        interpreter, ruleSet, plugin,
                        localTypes, pluginFlags,
                        commandFlags, ref error);

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
        public static ReturnCode GetBuiltInOperators(
            IPlugin plugin,
            StringComparison comparisonType,
            PluginFlags pluginFlags,
            bool createStandard,
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

                if (createStandard && !FlagOps.HasFlags(
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
                    name, group, null, null, typeName,
                    lexemes[index], (int)operands[index],
                    operandTypes, localOperatorFlags,
                    comparisonType, plugin, 0));
            }

            return ReturnCode.Ok;
        }

        ///////////////////////////////////////////////////////////////////////

        public static ReturnCode GetPluginOperators(
            IPlugin plugin,
            TypeList types,
            StringComparison comparisonType,
            PluginFlags pluginFlags,
            bool createStandard,
            ref List<IOperatorData> operators,
            ref Result error
            )
        {
            bool verbose = FlagOps.HasFlags(
                pluginFlags, PluginFlags.Verbose, true);

            TypeList localTypes = null;

            if (types != null)
            {
                localTypes = types;
            }
            else if (PopulatePluginTypes(
                    plugin, pluginFlags, ref localTypes,
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

                if (createStandard && !FlagOps.HasFlags(
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
                    name, null, null, null, typeName, lexeme, operands,
                    operandTypes, operatorFlags, comparisonType, plugin,
                    0));
            }

            return ReturnCode.Ok;
        }

        ///////////////////////////////////////////////////////////////////////

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

            Type type = Type.GetType(typeName, false, true);

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
        public static ReturnCode GetBuiltInFunctions(
            IPlugin plugin,
            PluginFlags pluginFlags,
            bool createStandard,
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
                        typeName, typeof(_Functions.Core).FullName))
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

                if (createStandard && !FlagOps.HasFlags(
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
                    name, group, null, null, typeName,
                    (int)arguments[index], argumentTypes,
                    localFunctionFlags, plugin, 0));
            }

            return ReturnCode.Ok;
        }

        ///////////////////////////////////////////////////////////////////////

        public static ReturnCode GetPluginFunctions(
            IPlugin plugin,
            TypeList types,
            PluginFlags pluginFlags,
            bool createStandard,
            ref List<IFunctionData> functions,
            ref Result error
            )
        {
            bool verbose = FlagOps.HasFlags(
                pluginFlags, PluginFlags.Verbose, true);

            TypeList localTypes = null;

            if (types != null)
            {
                localTypes = types;
            }
            else if (PopulatePluginTypes(
                    plugin, pluginFlags, ref localTypes,
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
                        typeName, typeof(_Functions.Core).FullName))
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

                if (createStandard && !FlagOps.HasFlags(
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
                    name, null, null, null, typeName, arguments,
                    argumentTypes, functionFlags, plugin, 0));
            }

            return ReturnCode.Ok;
        }

        ///////////////////////////////////////////////////////////////////////

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

            Type type = Type.GetType(typeName, false, true);

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
        private static void InitializeRandomness() /* throw */
        {
            lock (syncRoot) /* TRANSACTIONAL */
            {
                if (randomNumberGenerator != null)
                    return;

                randomNumberGenerator = RNGCryptoServiceProvider.Create();
            }
        }

        ///////////////////////////////////////////////////////////////////////

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

        public static void GetRandomBytes( /* throw */
            ref byte[] bytes /* in, out */
            )
        {
            /* NO RESULT */
            InitializeRandomness(); /* throw */

            lock (syncRoot) /* TRANSACTIONAL */
            {
                /* NO RESULT */
                GetRandomBytes(
                    randomNumberGenerator, null, ref bytes); /* throw */
            }
        }

        ///////////////////////////////////////////////////////////////////////

        public static void GetRandomBytes( /* throw */
            RandomNumberGenerator randomNumberGenerator, /* in: may be NULL. */
            Random random,                               /* in: may be NULL. */
            ref byte[] bytes                             /* in, out */
            )
        {
            bool gotBytes = false;

            if (randomNumberGenerator != null)
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

        public static ulong GetRandomNumber() /* throw */
        {
            /* NO RESULT */
            InitializeRandomness(); /* throw */

            lock (syncRoot) /* TRANSACTIONAL */
            {
                return GetRandomNumber(randomNumberGenerator, null); /* throw */
            }
        }

        ///////////////////////////////////////////////////////////////////////

        public static ulong GetRandomNumber( /* throw */
            RandomNumberGenerator randomNumberGenerator, /* in: may be NULL. */
            Random random                                /* in: may be NULL. */
            )
        {
            byte[] bytes = new byte[sizeof(ulong)];

            /* NO RESULT */
            GetRandomBytes(
                randomNumberGenerator, random, ref bytes); /* throw */

            return BitConverter.ToUInt64(bytes, 0);
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Interpreter Locking Support Methods
        //
        // TODO: Make this method configurable via some runtime mechanism?
        //
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
        public static bool IsValidHandle(
            IntPtr handle
            )
        {
            return ((handle != IntPtr.Zero) &&
                    (handle != INVALID_HANDLE_VALUE));
        }

        ///////////////////////////////////////////////////////////////////////

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
#if EMIT && NATIVE && LIBRARY
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

            _Wrappers._Module wrapper = module as _Wrappers._Module;

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
                // HACK: Cannot use the wrapper methods here because they
                //       catch ThreadInterruptedException, et al.
                //
                Thread.Sleep(timeout); /* throw */

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
                    TracePriority.ThreadError);
            }
            catch (ThreadInterruptedException e)
            {
                TraceOps.DebugTrace(
                    e, typeof(RuntimeOps).Name,
                    TracePriority.ThreadError);
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
                cacheCounts.SetCacheCounts(null, false);

            return true;
        }

        ///////////////////////////////////////////////////////////////////////

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
    }
}
