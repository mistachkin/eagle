/*
 * EngineFlagOps.cs --
 *
 * Copyright (c) 2007-2012 by Joe Mistachkin.  All rights reserved.
 *
 * See the file "license.terms" for information on usage and redistribution of
 * this file, and for a DISCLAIMER OF ALL WARRANTIES.
 *
 * RCS: @(#) $Id: $
 */

#if NET_45 || NET_451 || NET_452 || NET_46 || NET_461 || NET_462 || NET_47 || NET_471 || NET_472 || NET_48 || NET_481 || NET_STANDARD_20
using System.Runtime.CompilerServices;
#endif

using Eagle._Attributes;
using Eagle._Components.Public;

namespace Eagle._Components.Private
{
    /// <summary>
    /// This class provides static helper methods for testing whether one or
    /// more flags are present within an <see cref="EngineFlags" /> value used
    /// by the script engine.
    /// </summary>
    [ObjectId("edec12ec-7c3b-4028-b66c-b3d8f7eaedc6")]
    internal static class EngineFlagOps
    {
        /// <summary>
        /// This method determines whether the specified engine flags include
        /// a set of flags to check for.
        /// </summary>
        /// <param name="flags">
        /// The engine flags to examine.
        /// </param>
        /// <param name="hasFlags">
        /// The engine flags to check for within <paramref name="flags" />.
        /// </param>
        /// <param name="all">
        /// Non-zero to require that all of the flags in
        /// <paramref name="hasFlags" /> be present; otherwise, the presence of
        /// any one of them is sufficient.
        /// </param>
        /// <returns>
        /// True if the required flags are present in
        /// <paramref name="flags" />; otherwise, false.
        /// </returns>
#if NET_45 || NET_451 || NET_452 || NET_46 || NET_461 || NET_462 || NET_47 || NET_471 || NET_472 || NET_48 || NET_481 || NET_STANDARD_20
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
#endif
        private static bool HasFlags(
            EngineFlags flags,
            EngineFlags hasFlags,
            bool all
            )
        {
            if (all)
                return ((flags & hasFlags) == hasFlags);
            else
                return ((flags & hasFlags) != EngineFlags.None);
        }

        ///////////////////////////////////////////////////////////////////////

#if CALLBACK_QUEUE
        /// <summary>
        /// This method determines whether the specified engine flags include
        /// the <see cref="EngineFlags.NoCallbackQueue" /> flag.
        /// </summary>
        /// <param name="flags">
        /// The engine flags to examine.
        /// </param>
        /// <returns>
        /// True if the <see cref="EngineFlags.NoCallbackQueue" /> flag is set
        /// in <paramref name="flags" />; otherwise, false.
        /// </returns>
#if NET_45 || NET_451 || NET_452 || NET_46 || NET_461 || NET_462 || NET_47 || NET_471 || NET_472 || NET_48 || NET_481 || NET_STANDARD_20
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
#endif
        public static bool HasNoCallbackQueue(
            EngineFlags flags
            )
        {
            return HasFlags(flags, EngineFlags.NoCallbackQueue, true);
        }
#endif

        ///////////////////////////////////////////////////////////////////////

#if DEBUGGER
        /// <summary>
        /// This method determines whether the specified engine flags include
        /// the <see cref="EngineFlags.NoBreakpoint" /> flag.
        /// </summary>
        /// <param name="flags">
        /// The engine flags to examine.
        /// </param>
        /// <returns>
        /// True if the <see cref="EngineFlags.NoBreakpoint" /> flag is set in
        /// <paramref name="flags" />; otherwise, false.
        /// </returns>
#if NET_45 || NET_451 || NET_452 || NET_46 || NET_461 || NET_462 || NET_47 || NET_471 || NET_472 || NET_48 || NET_481 || NET_STANDARD_20
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
#endif
        public static bool HasNoBreakpoint(
            EngineFlags flags
            )
        {
            return HasFlags(flags, EngineFlags.NoBreakpoint, true);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method determines whether the specified engine flags include
        /// the <see cref="EngineFlags.NoWatchpoint" /> flag.
        /// </summary>
        /// <param name="flags">
        /// The engine flags to examine.
        /// </param>
        /// <returns>
        /// True if the <see cref="EngineFlags.NoWatchpoint" /> flag is set in
        /// <paramref name="flags" />; otherwise, false.
        /// </returns>
#if NET_45 || NET_451 || NET_452 || NET_46 || NET_461 || NET_462 || NET_47 || NET_471 || NET_472 || NET_48 || NET_481 || NET_STANDARD_20
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
#endif
        public static bool HasNoWatchpoint(
            EngineFlags flags
            )
        {
            return HasFlags(flags, EngineFlags.NoWatchpoint, true);
        }
#endif

        ///////////////////////////////////////////////////////////////////////

#if DEBUGGER && DEBUGGER_ARGUMENTS
        /// <summary>
        /// This method determines whether the specified engine flags include
        /// the <see cref="EngineFlags.NoDebuggerArguments" /> flag.
        /// </summary>
        /// <param name="flags">
        /// The engine flags to examine.
        /// </param>
        /// <returns>
        /// True if the <see cref="EngineFlags.NoDebuggerArguments" /> flag is
        /// set in <paramref name="flags" />; otherwise, false.
        /// </returns>
#if NET_45 || NET_451 || NET_452 || NET_46 || NET_461 || NET_462 || NET_47 || NET_471 || NET_472 || NET_48 || NET_481 || NET_STANDARD_20
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
#endif
        public static bool HasNoDebuggerArguments(
            EngineFlags flags
            )
        {
            return HasFlags(flags, EngineFlags.NoDebuggerArguments, true);
        }
#endif

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method determines whether the specified engine flags include
        /// the <see cref="EngineFlags.NoGlobalCancel" /> flag.
        /// </summary>
        /// <param name="flags">
        /// The engine flags to examine.
        /// </param>
        /// <returns>
        /// True if the <see cref="EngineFlags.NoGlobalCancel" /> flag is set in
        /// <paramref name="flags" />; otherwise, false.
        /// </returns>
#if NET_45 || NET_451 || NET_452 || NET_46 || NET_461 || NET_462 || NET_47 || NET_471 || NET_472 || NET_48 || NET_481 || NET_STANDARD_20
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
#endif
        public static bool HasNoGlobalCancel(
            EngineFlags flags
            )
        {
            return HasFlags(flags, EngineFlags.NoGlobalCancel, true);
        }

        ///////////////////////////////////////////////////////////////////////

#if HISTORY
        /// <summary>
        /// This method determines whether the specified engine flags include
        /// the <see cref="EngineFlags.NoHistory" /> flag.
        /// </summary>
        /// <param name="flags">
        /// The engine flags to examine.
        /// </param>
        /// <returns>
        /// True if the <see cref="EngineFlags.NoHistory" /> flag is set in
        /// <paramref name="flags" />; otherwise, false.
        /// </returns>
#if NET_45 || NET_451 || NET_452 || NET_46 || NET_461 || NET_462 || NET_47 || NET_471 || NET_472 || NET_48 || NET_481 || NET_STANDARD_20
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
#endif
        public static bool HasNoHistory(
            EngineFlags flags
            )
        {
            return HasFlags(flags, EngineFlags.NoHistory, true);
        }
#endif

        ///////////////////////////////////////////////////////////////////////

#if NOTIFY || NOTIFY_OBJECT
        /// <summary>
        /// This method determines whether the specified engine flags include
        /// the <see cref="EngineFlags.NoNotify" /> flag.
        /// </summary>
        /// <param name="flags">
        /// The engine flags to examine.
        /// </param>
        /// <returns>
        /// True if the <see cref="EngineFlags.NoNotify" /> flag is set in
        /// <paramref name="flags" />; otherwise, false.
        /// </returns>
#if NET_45 || NET_451 || NET_452 || NET_46 || NET_461 || NET_462 || NET_47 || NET_471 || NET_472 || NET_48 || NET_481 || NET_STANDARD_20
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
#endif
        public static bool HasNoNotify(
            EngineFlags flags
            )
        {
            return HasFlags(flags, EngineFlags.NoNotify, true);
        }
#endif

        ///////////////////////////////////////////////////////////////////////

#if XML
        /// <summary>
        /// This method determines whether the specified engine flags include
        /// the <see cref="EngineFlags.NoXml" /> flag.
        /// </summary>
        /// <param name="flags">
        /// The engine flags to examine.
        /// </param>
        /// <returns>
        /// True if the <see cref="EngineFlags.NoXml" /> flag is set in
        /// <paramref name="flags" />; otherwise, false.
        /// </returns>
#if NET_45 || NET_451 || NET_452 || NET_46 || NET_461 || NET_462 || NET_47 || NET_471 || NET_472 || NET_48 || NET_481 || NET_STANDARD_20
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
#endif
        public static bool HasNoXml(
            EngineFlags flags
            )
        {
            return HasFlags(flags, EngineFlags.NoXml, true);
        }
#endif

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method determines whether the specified engine flags include
        /// the <see cref="EngineFlags.NoHost" /> flag.
        /// </summary>
        /// <param name="flags">
        /// The engine flags to examine.
        /// </param>
        /// <returns>
        /// True if the <see cref="EngineFlags.NoHost" /> flag is set in
        /// <paramref name="flags" />; otherwise, false.
        /// </returns>
#if NET_45 || NET_451 || NET_452 || NET_46 || NET_461 || NET_462 || NET_47 || NET_471 || NET_472 || NET_48 || NET_481 || NET_STANDARD_20
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
#endif
        public static bool HasNoHost(
            EngineFlags flags
            )
        {
            return HasFlags(flags, EngineFlags.NoHost, true);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method determines whether the specified engine flags include
        /// the <see cref="EngineFlags.NoCancel" /> flag.
        /// </summary>
        /// <param name="flags">
        /// The engine flags to examine.
        /// </param>
        /// <returns>
        /// True if the <see cref="EngineFlags.NoCancel" /> flag is set in
        /// <paramref name="flags" />; otherwise, false.
        /// </returns>
#if NET_45 || NET_451 || NET_452 || NET_46 || NET_461 || NET_462 || NET_47 || NET_471 || NET_472 || NET_48 || NET_481 || NET_STANDARD_20
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
#endif
        public static bool HasNoCancel(
            EngineFlags flags
            )
        {
            return HasFlags(flags, EngineFlags.NoCancel, true);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method determines whether the specified engine flags include
        /// the <see cref="EngineFlags.NoReady" /> flag.
        /// </summary>
        /// <param name="flags">
        /// The engine flags to examine.
        /// </param>
        /// <returns>
        /// True if the <see cref="EngineFlags.NoReady" /> flag is set in
        /// <paramref name="flags" />; otherwise, false.
        /// </returns>
#if NET_45 || NET_451 || NET_452 || NET_46 || NET_461 || NET_462 || NET_47 || NET_471 || NET_472 || NET_48 || NET_481 || NET_STANDARD_20
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
#endif
        public static bool HasNoReady(
            EngineFlags flags
            )
        {
            return HasFlags(flags, EngineFlags.NoReady, true);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method determines whether the specified engine flags include
        /// the <see cref="EngineFlags.CheckStack" /> flag.
        /// </summary>
        /// <param name="flags">
        /// The engine flags to examine.
        /// </param>
        /// <returns>
        /// True if the <see cref="EngineFlags.CheckStack" /> flag is set in
        /// <paramref name="flags" />; otherwise, false.
        /// </returns>
#if NET_45 || NET_451 || NET_452 || NET_46 || NET_461 || NET_462 || NET_47 || NET_471 || NET_472 || NET_48 || NET_481 || NET_STANDARD_20
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
#endif
        public static bool HasCheckStack(
            EngineFlags flags
            )
        {
            return HasFlags(flags, EngineFlags.CheckStack, true);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method determines whether the specified engine flags include
        /// the <see cref="EngineFlags.ForceStack" /> flag.
        /// </summary>
        /// <param name="flags">
        /// The engine flags to examine.
        /// </param>
        /// <returns>
        /// True if the <see cref="EngineFlags.ForceStack" /> flag is set in
        /// <paramref name="flags" />; otherwise, false.
        /// </returns>
#if NET_45 || NET_451 || NET_452 || NET_46 || NET_461 || NET_462 || NET_47 || NET_471 || NET_472 || NET_48 || NET_481 || NET_STANDARD_20
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
#endif
        public static bool HasForceStack(
            EngineFlags flags
            )
        {
            return HasFlags(flags, EngineFlags.ForceStack, true);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method determines whether the specified engine flags include
        /// the <see cref="EngineFlags.ForcePoolStack" /> flag.
        /// </summary>
        /// <param name="flags">
        /// The engine flags to examine.
        /// </param>
        /// <returns>
        /// True if the <see cref="EngineFlags.ForcePoolStack" /> flag is set in
        /// <paramref name="flags" />; otherwise, false.
        /// </returns>
#if NET_45 || NET_451 || NET_452 || NET_46 || NET_461 || NET_462 || NET_47 || NET_471 || NET_472 || NET_48 || NET_481 || NET_STANDARD_20
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
#endif
        public static bool HasForcePoolStack(
            EngineFlags flags
            )
        {
            return HasFlags(flags, EngineFlags.ForcePoolStack, true);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method determines whether the specified engine flags include
        /// the <see cref="EngineFlags.NoEvent" /> flag.
        /// </summary>
        /// <param name="flags">
        /// The engine flags to examine.
        /// </param>
        /// <returns>
        /// True if the <see cref="EngineFlags.NoEvent" /> flag is set in
        /// <paramref name="flags" />; otherwise, false.
        /// </returns>
#if NET_45 || NET_451 || NET_452 || NET_46 || NET_461 || NET_462 || NET_47 || NET_471 || NET_472 || NET_48 || NET_481 || NET_STANDARD_20
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
#endif
        public static bool HasNoEvent(
            EngineFlags flags
            )
        {
            return HasFlags(flags, EngineFlags.NoEvent, true);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method determines whether the specified engine flags include
        /// the <see cref="EngineFlags.EvaluateGlobal" /> flag.
        /// </summary>
        /// <param name="flags">
        /// The engine flags to examine.
        /// </param>
        /// <returns>
        /// True if the <see cref="EngineFlags.EvaluateGlobal" /> flag is set in
        /// <paramref name="flags" />; otherwise, false.
        /// </returns>
#if NET_45 || NET_451 || NET_452 || NET_46 || NET_461 || NET_462 || NET_47 || NET_471 || NET_472 || NET_48 || NET_481 || NET_STANDARD_20
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
#endif
        public static bool HasEvaluateGlobal(
            EngineFlags flags
            )
        {
            return HasFlags(flags, EngineFlags.EvaluateGlobal, true);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method determines whether the specified engine flags include
        /// the <see cref="EngineFlags.ResetReturnCode" /> flag.
        /// </summary>
        /// <param name="flags">
        /// The engine flags to examine.
        /// </param>
        /// <returns>
        /// True if the <see cref="EngineFlags.ResetReturnCode" /> flag is set
        /// in <paramref name="flags" />; otherwise, false.
        /// </returns>
#if NET_45 || NET_451 || NET_452 || NET_46 || NET_461 || NET_462 || NET_47 || NET_471 || NET_472 || NET_48 || NET_481 || NET_STANDARD_20
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
#endif
        public static bool HasResetReturnCode(
            EngineFlags flags
            )
        {
            return HasFlags(flags, EngineFlags.ResetReturnCode, true);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method determines whether the specified engine flags include
        /// the <see cref="EngineFlags.ResetCancel" /> flag.
        /// </summary>
        /// <param name="flags">
        /// The engine flags to examine.
        /// </param>
        /// <returns>
        /// True if the <see cref="EngineFlags.ResetCancel" /> flag is set in
        /// <paramref name="flags" />; otherwise, false.
        /// </returns>
#if NET_45 || NET_451 || NET_452 || NET_46 || NET_461 || NET_462 || NET_47 || NET_471 || NET_472 || NET_48 || NET_481 || NET_STANDARD_20
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
#endif
        public static bool HasResetCancel(
            EngineFlags flags
            )
        {
            return HasFlags(flags, EngineFlags.ResetCancel, true);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method determines whether the specified engine flags include
        /// the <see cref="EngineFlags.ErrorAlreadyLogged" /> flag.
        /// </summary>
        /// <param name="flags">
        /// The engine flags to examine.
        /// </param>
        /// <returns>
        /// True if the <see cref="EngineFlags.ErrorAlreadyLogged" /> flag is
        /// set in <paramref name="flags" />; otherwise, false.
        /// </returns>
#if NET_45 || NET_451 || NET_452 || NET_46 || NET_461 || NET_462 || NET_47 || NET_471 || NET_472 || NET_48 || NET_481 || NET_STANDARD_20
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
#endif
        public static bool HasErrorAlreadyLogged(
            EngineFlags flags
            )
        {
            return HasFlags(flags, EngineFlags.ErrorAlreadyLogged, true);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method determines whether the specified engine flags include
        /// the <see cref="EngineFlags.ErrorInProgress" /> flag.
        /// </summary>
        /// <param name="flags">
        /// The engine flags to examine.
        /// </param>
        /// <returns>
        /// True if the <see cref="EngineFlags.ErrorInProgress" /> flag is set
        /// in <paramref name="flags" />; otherwise, false.
        /// </returns>
#if NET_45 || NET_451 || NET_452 || NET_46 || NET_461 || NET_462 || NET_47 || NET_471 || NET_472 || NET_48 || NET_481 || NET_STANDARD_20
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
#endif
        public static bool HasErrorInProgress(
            EngineFlags flags
            )
        {
            return HasFlags(flags, EngineFlags.ErrorInProgress, true);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method determines whether the specified engine flags include
        /// the <see cref="EngineFlags.ErrorCodeSet" /> flag.
        /// </summary>
        /// <param name="flags">
        /// The engine flags to examine.
        /// </param>
        /// <returns>
        /// True if the <see cref="EngineFlags.ErrorCodeSet" /> flag is set in
        /// <paramref name="flags" />; otherwise, false.
        /// </returns>
#if NET_45 || NET_451 || NET_452 || NET_46 || NET_461 || NET_462 || NET_47 || NET_471 || NET_472 || NET_48 || NET_481 || NET_STANDARD_20
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
#endif
        public static bool HasErrorCodeSet(
            EngineFlags flags
            )
        {
            return HasFlags(flags, EngineFlags.ErrorCodeSet, true);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method determines whether the specified engine flags include
        /// the <see cref="EngineFlags.NoEvaluate" /> flag.
        /// </summary>
        /// <param name="flags">
        /// The engine flags to examine.
        /// </param>
        /// <returns>
        /// True if the <see cref="EngineFlags.NoEvaluate" /> flag is set in
        /// <paramref name="flags" />; otherwise, false.
        /// </returns>
#if NET_45 || NET_451 || NET_452 || NET_46 || NET_461 || NET_462 || NET_47 || NET_471 || NET_472 || NET_48 || NET_481 || NET_STANDARD_20
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
#endif
        public static bool HasNoEvaluate(
            EngineFlags flags
            )
        {
            return HasFlags(flags, EngineFlags.NoEvaluate, true);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method determines whether the specified engine flags include
        /// the <see cref="EngineFlags.NoRemote" /> flag.
        /// </summary>
        /// <param name="flags">
        /// The engine flags to examine.
        /// </param>
        /// <returns>
        /// True if the <see cref="EngineFlags.NoRemote" /> flag is set in
        /// <paramref name="flags" />; otherwise, false.
        /// </returns>
#if NET_45 || NET_451 || NET_452 || NET_46 || NET_461 || NET_462 || NET_47 || NET_471 || NET_472 || NET_48 || NET_481 || NET_STANDARD_20
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
#endif
        public static bool HasNoRemote(
            EngineFlags flags
            )
        {
            return HasFlags(flags, EngineFlags.NoRemote, true);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method determines whether the specified engine flags include
        /// the <see cref="EngineFlags.ExactMatch" /> flag.
        /// </summary>
        /// <param name="flags">
        /// The engine flags to examine.
        /// </param>
        /// <returns>
        /// True if the <see cref="EngineFlags.ExactMatch" /> flag is set in
        /// <paramref name="flags" />; otherwise, false.
        /// </returns>
#if NET_45 || NET_451 || NET_452 || NET_46 || NET_461 || NET_462 || NET_47 || NET_471 || NET_472 || NET_48 || NET_481 || NET_STANDARD_20
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
#endif
        public static bool HasExactMatch(
            EngineFlags flags
            ) /* USED BY CORE RESOLVER */
        {
            return HasFlags(flags, EngineFlags.ExactMatch, true);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method determines whether the specified engine flags include
        /// the <see cref="EngineFlags.NoUnknown" /> flag.
        /// </summary>
        /// <param name="flags">
        /// The engine flags to examine.
        /// </param>
        /// <returns>
        /// True if the <see cref="EngineFlags.NoUnknown" /> flag is set in
        /// <paramref name="flags" />; otherwise, false.
        /// </returns>
#if NET_45 || NET_451 || NET_452 || NET_46 || NET_461 || NET_462 || NET_47 || NET_471 || NET_472 || NET_48 || NET_481 || NET_STANDARD_20
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
#endif
        public static bool HasNoUnknown(
            EngineFlags flags
            )
        {
            return HasFlags(flags, EngineFlags.NoUnknown, true);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method determines whether the specified engine flags include
        /// the <see cref="EngineFlags.NoResetResult" /> flag.
        /// </summary>
        /// <param name="flags">
        /// The engine flags to examine.
        /// </param>
        /// <returns>
        /// True if the <see cref="EngineFlags.NoResetResult" /> flag is set in
        /// <paramref name="flags" />; otherwise, false.
        /// </returns>
#if NET_45 || NET_451 || NET_452 || NET_46 || NET_461 || NET_462 || NET_47 || NET_471 || NET_472 || NET_48 || NET_481 || NET_STANDARD_20
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
#endif
        public static bool HasNoResetResult(
            EngineFlags flags
            )
        {
            return HasFlags(flags, EngineFlags.NoResetResult, true);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method determines whether the specified engine flags include
        /// the <see cref="EngineFlags.NoResetError" /> flag.
        /// </summary>
        /// <param name="flags">
        /// The engine flags to examine.
        /// </param>
        /// <returns>
        /// True if the <see cref="EngineFlags.NoResetError" /> flag is set in
        /// <paramref name="flags" />; otherwise, false.
        /// </returns>
#if NET_45 || NET_451 || NET_452 || NET_46 || NET_461 || NET_462 || NET_47 || NET_471 || NET_472 || NET_48 || NET_481 || NET_STANDARD_20
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
#endif
        public static bool HasNoResetError(
            EngineFlags flags
            )
        {
            return HasFlags(flags, EngineFlags.NoResetError, true);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method determines whether the specified engine flags include
        /// the <see cref="EngineFlags.NoSafeFunction" /> flag.
        /// </summary>
        /// <param name="flags">
        /// The engine flags to examine.
        /// </param>
        /// <returns>
        /// True if the <see cref="EngineFlags.NoSafeFunction" /> flag is set in
        /// <paramref name="flags" />; otherwise, false.
        /// </returns>
#if NET_45 || NET_451 || NET_452 || NET_46 || NET_461 || NET_462 || NET_47 || NET_471 || NET_472 || NET_48 || NET_481 || NET_STANDARD_20
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
#endif
        public static bool HasNoSafeFunction(
            EngineFlags flags
            )
        {
            return HasFlags(flags, EngineFlags.NoSafeFunction, true);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method determines whether the specified engine flags include
        /// the <see cref="EngineFlags.NoSubstitute" /> flag.
        /// </summary>
        /// <param name="flags">
        /// The engine flags to examine.
        /// </param>
        /// <returns>
        /// True if the <see cref="EngineFlags.NoSubstitute" /> flag is set in
        /// <paramref name="flags" />; otherwise, false.
        /// </returns>
#if NET_45 || NET_451 || NET_452 || NET_46 || NET_461 || NET_462 || NET_47 || NET_471 || NET_472 || NET_48 || NET_481 || NET_STANDARD_20
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
#endif
        public static bool HasNoSubstitute(
            EngineFlags flags
            )
        {
            return HasFlags(flags, EngineFlags.NoSubstitute, true);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method determines whether the specified engine flags include
        /// the <see cref="EngineFlags.BracketTerminator" /> flag.
        /// </summary>
        /// <param name="flags">
        /// The engine flags to examine.
        /// </param>
        /// <returns>
        /// True if the <see cref="EngineFlags.BracketTerminator" /> flag is set
        /// in <paramref name="flags" />; otherwise, false.
        /// </returns>
#if NET_45 || NET_451 || NET_452 || NET_46 || NET_461 || NET_462 || NET_47 || NET_471 || NET_472 || NET_48 || NET_481 || NET_STANDARD_20
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
#endif
        public static bool HasBracketTerminator(
            EngineFlags flags
            )
        {
            return HasFlags(flags, EngineFlags.BracketTerminator, true);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method determines whether the specified engine flags include
        /// the <see cref="EngineFlags.UseIExecutes" /> flag.
        /// </summary>
        /// <param name="flags">
        /// The engine flags to examine.
        /// </param>
        /// <returns>
        /// True if the <see cref="EngineFlags.UseIExecutes" /> flag is set in
        /// <paramref name="flags" />; otherwise, false.
        /// </returns>
#if NET_45 || NET_451 || NET_452 || NET_46 || NET_461 || NET_462 || NET_47 || NET_471 || NET_472 || NET_48 || NET_481 || NET_STANDARD_20
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
#endif
        public static bool HasUseIExecutes(
            EngineFlags flags
            )
        {
            return HasFlags(flags, EngineFlags.UseIExecutes, true);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method determines whether the specified engine flags include
        /// the <see cref="EngineFlags.UseCommands" /> flag.
        /// </summary>
        /// <param name="flags">
        /// The engine flags to examine.
        /// </param>
        /// <returns>
        /// True if the <see cref="EngineFlags.UseCommands" /> flag is set in
        /// <paramref name="flags" />; otherwise, false.
        /// </returns>
#if NET_45 || NET_451 || NET_452 || NET_46 || NET_461 || NET_462 || NET_47 || NET_471 || NET_472 || NET_48 || NET_481 || NET_STANDARD_20
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
#endif
        public static bool HasUseCommands(
            EngineFlags flags
            )
        {
            return HasFlags(flags, EngineFlags.UseCommands, true);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method determines whether the specified engine flags include
        /// the <see cref="EngineFlags.UseProcedures" /> flag.
        /// </summary>
        /// <param name="flags">
        /// The engine flags to examine.
        /// </param>
        /// <returns>
        /// True if the <see cref="EngineFlags.UseProcedures" /> flag is set in
        /// <paramref name="flags" />; otherwise, false.
        /// </returns>
#if NET_45 || NET_451 || NET_452 || NET_46 || NET_461 || NET_462 || NET_47 || NET_471 || NET_472 || NET_48 || NET_481 || NET_STANDARD_20
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
#endif
        public static bool HasUseProcedures(
            EngineFlags flags
            )
        {
            return HasFlags(flags, EngineFlags.UseProcedures, true);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method determines whether the specified engine flags include
        /// the <see cref="EngineFlags.ForceSoftEof" /> flag.
        /// </summary>
        /// <param name="flags">
        /// The engine flags to examine.
        /// </param>
        /// <returns>
        /// True if the <see cref="EngineFlags.ForceSoftEof" /> flag is set in
        /// <paramref name="flags" />; otherwise, false.
        /// </returns>
#if NET_45 || NET_451 || NET_452 || NET_46 || NET_461 || NET_462 || NET_47 || NET_471 || NET_472 || NET_48 || NET_481 || NET_STANDARD_20
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
#endif
        public static bool HasForceSoftEof(
            EngineFlags flags
            )
        {
            return HasFlags(flags, EngineFlags.ForceSoftEof, true);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method determines whether the specified engine flags include
        /// the <see cref="EngineFlags.SeekSoftEof" /> flag.
        /// </summary>
        /// <param name="flags">
        /// The engine flags to examine.
        /// </param>
        /// <returns>
        /// True if the <see cref="EngineFlags.SeekSoftEof" /> flag is set in
        /// <paramref name="flags" />; otherwise, false.
        /// </returns>
#if NET_45 || NET_451 || NET_452 || NET_46 || NET_461 || NET_462 || NET_47 || NET_471 || NET_472 || NET_48 || NET_481 || NET_STANDARD_20
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
#endif
        public static bool HasSeekSoftEof(
            EngineFlags flags
            )
        {
            return HasFlags(flags, EngineFlags.SeekSoftEof, true);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method determines whether the specified engine flags include
        /// the <see cref="EngineFlags.PostScriptBytes" /> flag.
        /// </summary>
        /// <param name="flags">
        /// The engine flags to examine.
        /// </param>
        /// <returns>
        /// True if the <see cref="EngineFlags.PostScriptBytes" /> flag is set
        /// in <paramref name="flags" />; otherwise, false.
        /// </returns>
#if NET_45 || NET_451 || NET_452 || NET_46 || NET_461 || NET_462 || NET_47 || NET_471 || NET_472 || NET_48 || NET_481 || NET_STANDARD_20
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
#endif
        public static bool HasPostScriptBytes(
            EngineFlags flags
            )
        {
            return HasFlags(flags, EngineFlags.PostScriptBytes, true);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method determines whether the specified engine flags include
        /// the <see cref="EngineFlags.NoPolicy" /> flag.
        /// </summary>
        /// <param name="flags">
        /// The engine flags to examine.
        /// </param>
        /// <returns>
        /// True if the <see cref="EngineFlags.NoPolicy" /> flag is set in
        /// <paramref name="flags" />; otherwise, false.
        /// </returns>
#if NET_45 || NET_451 || NET_452 || NET_46 || NET_461 || NET_462 || NET_47 || NET_471 || NET_472 || NET_48 || NET_481 || NET_STANDARD_20
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
#endif
        public static bool HasNoPolicy(
            EngineFlags flags
            )
        {
            return HasFlags(flags, EngineFlags.NoPolicy, true);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method determines whether the specified engine flags include
        /// the <see cref="EngineFlags.GetHidden" /> flag.
        /// </summary>
        /// <param name="flags">
        /// The engine flags to examine.
        /// </param>
        /// <returns>
        /// True if the <see cref="EngineFlags.GetHidden" /> flag is set in
        /// <paramref name="flags" />; otherwise, false.
        /// </returns>
#if NET_45 || NET_451 || NET_452 || NET_46 || NET_461 || NET_462 || NET_47 || NET_471 || NET_472 || NET_48 || NET_481 || NET_STANDARD_20
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
#endif
        public static bool HasGetHidden(
            EngineFlags flags
            )
        {
            return HasFlags(flags, EngineFlags.GetHidden, true);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method determines whether the specified engine flags include
        /// the <see cref="EngineFlags.MatchHidden" /> flag.
        /// </summary>
        /// <param name="flags">
        /// The engine flags to examine.
        /// </param>
        /// <returns>
        /// True if the <see cref="EngineFlags.MatchHidden" /> flag is set in
        /// <paramref name="flags" />; otherwise, false.
        /// </returns>
#if NET_45 || NET_451 || NET_452 || NET_46 || NET_461 || NET_462 || NET_47 || NET_471 || NET_472 || NET_48 || NET_481 || NET_STANDARD_20
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
#endif
        public static bool HasMatchHidden(
            EngineFlags flags
            )
        {
            return HasFlags(flags, EngineFlags.MatchHidden, true);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method determines whether the specified engine flags include
        /// the <see cref="EngineFlags.IgnoreHidden" /> flag.
        /// </summary>
        /// <param name="flags">
        /// The engine flags to examine.
        /// </param>
        /// <returns>
        /// True if the <see cref="EngineFlags.IgnoreHidden" /> flag is set in
        /// <paramref name="flags" />; otherwise, false.
        /// </returns>
#if NET_45 || NET_451 || NET_452 || NET_46 || NET_461 || NET_462 || NET_47 || NET_471 || NET_472 || NET_48 || NET_481 || NET_STANDARD_20
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
#endif
        public static bool HasIgnoreHidden(
            EngineFlags flags
            )
        {
            return HasFlags(flags, EngineFlags.IgnoreHidden, true);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method determines whether the specified engine flags include
        /// the <see cref="EngineFlags.ToExecute" /> flag.
        /// </summary>
        /// <param name="flags">
        /// The engine flags to examine.
        /// </param>
        /// <returns>
        /// True if the <see cref="EngineFlags.ToExecute" /> flag is set in
        /// <paramref name="flags" />; otherwise, false.
        /// </returns>
#if NET_45 || NET_451 || NET_452 || NET_46 || NET_461 || NET_462 || NET_47 || NET_471 || NET_472 || NET_48 || NET_481 || NET_STANDARD_20
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
#endif
        public static bool HasToExecute(
            EngineFlags flags
            )
        {
            return HasFlags(flags, EngineFlags.ToExecute, true);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method determines whether the specified engine flags include
        /// the <see cref="EngineFlags.UseHidden" /> flag.
        /// </summary>
        /// <param name="flags">
        /// The engine flags to examine.
        /// </param>
        /// <returns>
        /// True if the <see cref="EngineFlags.UseHidden" /> flag is set in
        /// <paramref name="flags" />; otherwise, false.
        /// </returns>
#if NET_45 || NET_451 || NET_452 || NET_46 || NET_461 || NET_462 || NET_47 || NET_471 || NET_472 || NET_48 || NET_481 || NET_STANDARD_20
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
#endif
        public static bool HasUseHidden(
            EngineFlags flags
            )
        {
            return HasFlags(flags, EngineFlags.UseHidden, true);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method determines whether the specified engine flags include
        /// the <see cref="EngineFlags.InvokeHidden" /> flag.
        /// </summary>
        /// <param name="flags">
        /// The engine flags to examine.
        /// </param>
        /// <returns>
        /// True if the <see cref="EngineFlags.InvokeHidden" /> flag is set in
        /// <paramref name="flags" />; otherwise, false.
        /// </returns>
#if NET_45 || NET_451 || NET_452 || NET_46 || NET_461 || NET_462 || NET_47 || NET_471 || NET_472 || NET_48 || NET_481 || NET_STANDARD_20
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
#endif
        public static bool HasInvokeHidden(
            EngineFlags flags
            )
        {
            return HasFlags(flags, EngineFlags.InvokeHidden, true);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method determines whether the specified engine flags include
        /// the <see cref="EngineFlags.GlobalOnly" /> flag.
        /// </summary>
        /// <param name="flags">
        /// The engine flags to examine.
        /// </param>
        /// <returns>
        /// True if the <see cref="EngineFlags.GlobalOnly" /> flag is set in
        /// <paramref name="flags" />; otherwise, false.
        /// </returns>
#if NET_45 || NET_451 || NET_452 || NET_46 || NET_461 || NET_462 || NET_47 || NET_471 || NET_472 || NET_48 || NET_481 || NET_STANDARD_20
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
#endif
        public static bool HasGlobalOnly(
            EngineFlags flags
            )
        {
            return HasFlags(flags, EngineFlags.GlobalOnly, true);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method determines whether the specified engine flags include
        /// the <see cref="EngineFlags.UseInterpreter" /> flag.
        /// </summary>
        /// <param name="flags">
        /// The engine flags to examine.
        /// </param>
        /// <returns>
        /// True if the <see cref="EngineFlags.UseInterpreter" /> flag is set in
        /// <paramref name="flags" />; otherwise, false.
        /// </returns>
#if NET_45 || NET_451 || NET_452 || NET_46 || NET_461 || NET_462 || NET_47 || NET_471 || NET_472 || NET_48 || NET_481 || NET_STANDARD_20
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
#endif
        public static bool HasUseInterpreter(
            EngineFlags flags
            )
        {
            return HasFlags(flags, EngineFlags.UseInterpreter, true);
        }

        ///////////////////////////////////////////////////////////////////////

#if NET_45 || NET_451 || NET_452 || NET_46 || NET_461 || NET_462 || NET_47 || NET_471 || NET_472 || NET_48 || NET_481 || NET_STANDARD_20
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
#endif
        public static bool HasExternalScript(
            EngineFlags flags
            )
        {
            return HasFlags(flags, EngineFlags.ExternalScript, true);
        }

        ///////////////////////////////////////////////////////////////////////

#if NET_45 || NET_451 || NET_452 || NET_46 || NET_461 || NET_462 || NET_47 || NET_471 || NET_472 || NET_48 || NET_481 || NET_STANDARD_20
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
#endif
        public static bool HasExtraCallFrame(
            EngineFlags flags
            )
        {
            return HasFlags(flags, EngineFlags.ExtraCallFrame, true);
        }

        ///////////////////////////////////////////////////////////////////////

#if TEST
        /// <summary>
        /// This method determines whether the specified engine flags include
        /// the <see cref="EngineFlags.SetSecurityProtocol" /> flag.
        /// </summary>
        /// <param name="flags">
        /// The engine flags to examine.
        /// </param>
        /// <returns>
        /// True if the <see cref="EngineFlags.SetSecurityProtocol" /> flag is
        /// set in <paramref name="flags" />; otherwise, false.
        /// </returns>
#if NET_45 || NET_451 || NET_452 || NET_46 || NET_461 || NET_462 || NET_47 || NET_471 || NET_472 || NET_48 || NET_481 || NET_STANDARD_20
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
#endif
        public static bool HasSetSecurityProtocol(
            EngineFlags flags
            )
        {
            return HasFlags(flags, EngineFlags.SetSecurityProtocol, true);
        }
#endif

        ///////////////////////////////////////////////////////////////////////

#if NET_45 || NET_451 || NET_452 || NET_46 || NET_461 || NET_462 || NET_47 || NET_471 || NET_472 || NET_48 || NET_481 || NET_STANDARD_20
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
#endif
        public static bool HasIgnoreRootedFileName(
            EngineFlags flags
            )
        {
            return HasFlags(flags, EngineFlags.IgnoreRootedFileName, true);
        }

        ///////////////////////////////////////////////////////////////////////

#if NET_45 || NET_451 || NET_452 || NET_46 || NET_461 || NET_462 || NET_47 || NET_471 || NET_472 || NET_48 || NET_481 || NET_STANDARD_20
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
#endif
        public static bool HasNoFileNameOnly(
            EngineFlags flags
            )
        {
            return HasFlags(flags, EngineFlags.NoFileNameOnly, true);
        }

        ///////////////////////////////////////////////////////////////////////

#if NET_45 || NET_451 || NET_452 || NET_46 || NET_461 || NET_462 || NET_47 || NET_471 || NET_472 || NET_48 || NET_481 || NET_STANDARD_20
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
#endif
        public static bool HasNoRawName(
            EngineFlags flags
            )
        {
            return HasFlags(flags, EngineFlags.NoRawName, true);
        }

        ///////////////////////////////////////////////////////////////////////

#if NET_45 || NET_451 || NET_452 || NET_46 || NET_461 || NET_462 || NET_47 || NET_471 || NET_472 || NET_48 || NET_481 || NET_STANDARD_20
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
#endif
        public static bool HasAllErrors(
            EngineFlags flags
            )
        {
            return HasFlags(flags, EngineFlags.AllErrors, true);
        }

        ///////////////////////////////////////////////////////////////////////

#if NET_45 || NET_451 || NET_452 || NET_46 || NET_461 || NET_462 || NET_47 || NET_471 || NET_472 || NET_48 || NET_481 || NET_STANDARD_20
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
#endif
        public static bool HasNoDefaultError(
            EngineFlags flags
            )
        {
            return HasFlags(flags, EngineFlags.NoDefaultError, true);
        }

        ///////////////////////////////////////////////////////////////////////

#if NET_45 || NET_451 || NET_452 || NET_46 || NET_461 || NET_462 || NET_47 || NET_471 || NET_472 || NET_48 || NET_481 || NET_STANDARD_20
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
#endif
        public static bool HasNoCache(
            EngineFlags flags
            )
        {
            return HasFlags(flags, EngineFlags.NoCache, true);
        }

        ///////////////////////////////////////////////////////////////////////

#if PARSE_CACHE
        /// <summary>
        /// This method determines whether the specified engine flags include
        /// the <see cref="EngineFlags.NoCacheParseState" /> flag.
        /// </summary>
        /// <param name="flags">
        /// The engine flags to examine.
        /// </param>
        /// <returns>
        /// True if the <see cref="EngineFlags.NoCacheParseState" /> flag is set
        /// in <paramref name="flags" />; otherwise, false.
        /// </returns>
#if NET_45 || NET_451 || NET_452 || NET_46 || NET_461 || NET_462 || NET_47 || NET_471 || NET_472 || NET_48 || NET_481 || NET_STANDARD_20
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
#endif
        public static bool HasNoCacheParseState(
            EngineFlags flags
            )
        {
            return HasFlags(flags, EngineFlags.NoCacheParseState, true);
        }
#endif

        ///////////////////////////////////////////////////////////////////////

#if ARGUMENT_CACHE
        /// <summary>
        /// This method determines whether the specified engine flags include
        /// the <see cref="EngineFlags.NoCacheArgument" /> flag.
        /// </summary>
        /// <param name="flags">
        /// The engine flags to examine.
        /// </param>
        /// <returns>
        /// True if the <see cref="EngineFlags.NoCacheArgument" /> flag is set
        /// in <paramref name="flags" />; otherwise, false.
        /// </returns>
#if NET_45 || NET_451 || NET_452 || NET_46 || NET_461 || NET_462 || NET_47 || NET_471 || NET_472 || NET_48 || NET_481 || NET_STANDARD_20
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
#endif
        public static bool HasNoCacheArgument(
            EngineFlags flags
            )
        {
            return HasFlags(flags, EngineFlags.NoCacheArgument, true);
        }
#endif

        ///////////////////////////////////////////////////////////////////////

#if NET_45 || NET_451 || NET_452 || NET_46 || NET_461 || NET_462 || NET_47 || NET_471 || NET_472 || NET_48 || NET_481 || NET_STANDARD_20
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
#endif
        public static bool HasNoUsageData(
            EngineFlags flags
            )
        {
            return HasFlags(flags, EngineFlags.NoUsageData, true);
        }

        ///////////////////////////////////////////////////////////////////////

#if NET_45 || NET_451 || NET_452 || NET_46 || NET_461 || NET_462 || NET_47 || NET_471 || NET_472 || NET_48 || NET_481 || NET_STANDARD_20
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
#endif
        public static bool HasNoNullArgument(
            EngineFlags flags
            )
        {
            return HasFlags(flags, EngineFlags.NoNullArgument, true);
        }

        ///////////////////////////////////////////////////////////////////////

#if NET_45 || NET_451 || NET_452 || NET_46 || NET_461 || NET_462 || NET_47 || NET_471 || NET_472 || NET_48 || NET_481 || NET_STANDARD_20
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
#endif
        public static bool HasNoResetAbort(
            EngineFlags flags
            )
        {
            return HasFlags(flags, EngineFlags.NoResetAbort, true);
        }

        ///////////////////////////////////////////////////////////////////////

#if PREVIOUS_RESULT
        /// <summary>
        /// This method determines whether the specified engine flags include
        /// the <see cref="EngineFlags.NoPreviousResult" /> flag.
        /// </summary>
        /// <param name="flags">
        /// The engine flags to examine.
        /// </param>
        /// <returns>
        /// True if the <see cref="EngineFlags.NoPreviousResult" /> flag is set
        /// in <paramref name="flags" />; otherwise, false.
        /// </returns>
#if NET_45 || NET_451 || NET_452 || NET_46 || NET_461 || NET_462 || NET_47 || NET_471 || NET_472 || NET_48 || NET_481 || NET_STANDARD_20
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
#endif
        public static bool HasNoPreviousResult(
            EngineFlags flags
            )
        {
            return HasFlags(flags, EngineFlags.NoPreviousResult, true);
        }
#endif
    }
}
