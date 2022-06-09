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

#if NET_45 || NET_451 || NET_452 || NET_46 || NET_461 || NET_462 || NET_47 || NET_471 || NET_472 || NET_48 || NET_STANDARD_20
using System.Runtime.CompilerServices;
#endif

using Eagle._Attributes;
using Eagle._Components.Public;

namespace Eagle._Components.Private
{
    [ObjectId("edec12ec-7c3b-4028-b66c-b3d8f7eaedc6")]
    internal static class EngineFlagOps
    {
#if NET_45 || NET_451 || NET_452 || NET_46 || NET_461 || NET_462 || NET_47 || NET_471 || NET_472 || NET_48 || NET_STANDARD_20
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
#if NET_45 || NET_451 || NET_452 || NET_46 || NET_461 || NET_462 || NET_47 || NET_471 || NET_472 || NET_48 || NET_STANDARD_20
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
#if NET_45 || NET_451 || NET_452 || NET_46 || NET_461 || NET_462 || NET_47 || NET_471 || NET_472 || NET_48 || NET_STANDARD_20
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
#endif
        public static bool HasNoBreakpoint(
            EngineFlags flags
            )
        {
            return HasFlags(flags, EngineFlags.NoBreakpoint, true);
        }

        ///////////////////////////////////////////////////////////////////////

#if NET_45 || NET_451 || NET_452 || NET_46 || NET_461 || NET_462 || NET_47 || NET_471 || NET_472 || NET_48 || NET_STANDARD_20
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
#if NET_45 || NET_451 || NET_452 || NET_46 || NET_461 || NET_462 || NET_47 || NET_471 || NET_472 || NET_48 || NET_STANDARD_20
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

#if NET_45 || NET_451 || NET_452 || NET_46 || NET_461 || NET_462 || NET_47 || NET_471 || NET_472 || NET_48 || NET_STANDARD_20
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
#if NET_45 || NET_451 || NET_452 || NET_46 || NET_461 || NET_462 || NET_47 || NET_471 || NET_472 || NET_48 || NET_STANDARD_20
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
#if NET_45 || NET_451 || NET_452 || NET_46 || NET_461 || NET_462 || NET_47 || NET_471 || NET_472 || NET_48 || NET_STANDARD_20
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
#if NET_45 || NET_451 || NET_452 || NET_46 || NET_461 || NET_462 || NET_47 || NET_471 || NET_472 || NET_48 || NET_STANDARD_20
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

#if NET_45 || NET_451 || NET_452 || NET_46 || NET_461 || NET_462 || NET_47 || NET_471 || NET_472 || NET_48 || NET_STANDARD_20
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
#endif
        public static bool HasNoHost(
            EngineFlags flags
            )
        {
            return HasFlags(flags, EngineFlags.NoHost, true);
        }

        ///////////////////////////////////////////////////////////////////////

#if NET_45 || NET_451 || NET_452 || NET_46 || NET_461 || NET_462 || NET_47 || NET_471 || NET_472 || NET_48 || NET_STANDARD_20
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
#endif
        public static bool HasNoCancel(
            EngineFlags flags
            )
        {
            return HasFlags(flags, EngineFlags.NoCancel, true);
        }

        ///////////////////////////////////////////////////////////////////////

#if NET_45 || NET_451 || NET_452 || NET_46 || NET_461 || NET_462 || NET_47 || NET_471 || NET_472 || NET_48 || NET_STANDARD_20
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
#endif
        public static bool HasNoReady(
            EngineFlags flags
            )
        {
            return HasFlags(flags, EngineFlags.NoReady, true);
        }

        ///////////////////////////////////////////////////////////////////////

#if NET_45 || NET_451 || NET_452 || NET_46 || NET_461 || NET_462 || NET_47 || NET_471 || NET_472 || NET_48 || NET_STANDARD_20
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
#endif
        public static bool HasCheckStack(
            EngineFlags flags
            )
        {
            return HasFlags(flags, EngineFlags.CheckStack, true);
        }

        ///////////////////////////////////////////////////////////////////////

#if NET_45 || NET_451 || NET_452 || NET_46 || NET_461 || NET_462 || NET_47 || NET_471 || NET_472 || NET_48 || NET_STANDARD_20
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
#endif
        public static bool HasForceStack(
            EngineFlags flags
            )
        {
            return HasFlags(flags, EngineFlags.ForceStack, true);
        }

        ///////////////////////////////////////////////////////////////////////

#if NET_45 || NET_451 || NET_452 || NET_46 || NET_461 || NET_462 || NET_47 || NET_471 || NET_472 || NET_48 || NET_STANDARD_20
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
#endif
        public static bool HasForcePoolStack(
            EngineFlags flags
            )
        {
            return HasFlags(flags, EngineFlags.ForcePoolStack, true);
        }

        ///////////////////////////////////////////////////////////////////////

#if NET_45 || NET_451 || NET_452 || NET_46 || NET_461 || NET_462 || NET_47 || NET_471 || NET_472 || NET_48 || NET_STANDARD_20
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
#endif
        public static bool HasNoEvent(
            EngineFlags flags
            )
        {
            return HasFlags(flags, EngineFlags.NoEvent, true);
        }

        ///////////////////////////////////////////////////////////////////////

#if NET_45 || NET_451 || NET_452 || NET_46 || NET_461 || NET_462 || NET_47 || NET_471 || NET_472 || NET_48 || NET_STANDARD_20
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
#endif
        public static bool HasEvaluateGlobal(
            EngineFlags flags
            )
        {
            return HasFlags(flags, EngineFlags.EvaluateGlobal, true);
        }

        ///////////////////////////////////////////////////////////////////////

#if NET_45 || NET_451 || NET_452 || NET_46 || NET_461 || NET_462 || NET_47 || NET_471 || NET_472 || NET_48 || NET_STANDARD_20
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
#endif
        public static bool HasResetReturnCode(
            EngineFlags flags
            )
        {
            return HasFlags(flags, EngineFlags.ResetReturnCode, true);
        }

        ///////////////////////////////////////////////////////////////////////

#if NET_45 || NET_451 || NET_452 || NET_46 || NET_461 || NET_462 || NET_47 || NET_471 || NET_472 || NET_48 || NET_STANDARD_20
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
#endif
        public static bool HasResetCancel(
            EngineFlags flags
            )
        {
            return HasFlags(flags, EngineFlags.ResetCancel, true);
        }

        ///////////////////////////////////////////////////////////////////////

#if NET_45 || NET_451 || NET_452 || NET_46 || NET_461 || NET_462 || NET_47 || NET_471 || NET_472 || NET_48 || NET_STANDARD_20
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
#endif
        public static bool HasErrorAlreadyLogged(
            EngineFlags flags
            )
        {
            return HasFlags(flags, EngineFlags.ErrorAlreadyLogged, true);
        }

        ///////////////////////////////////////////////////////////////////////

#if NET_45 || NET_451 || NET_452 || NET_46 || NET_461 || NET_462 || NET_47 || NET_471 || NET_472 || NET_48 || NET_STANDARD_20
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
#endif
        public static bool HasErrorInProgress(
            EngineFlags flags
            )
        {
            return HasFlags(flags, EngineFlags.ErrorInProgress, true);
        }

        ///////////////////////////////////////////////////////////////////////

#if NET_45 || NET_451 || NET_452 || NET_46 || NET_461 || NET_462 || NET_47 || NET_471 || NET_472 || NET_48 || NET_STANDARD_20
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
#endif
        public static bool HasErrorCodeSet(
            EngineFlags flags
            )
        {
            return HasFlags(flags, EngineFlags.ErrorCodeSet, true);
        }

        ///////////////////////////////////////////////////////////////////////

#if NET_45 || NET_451 || NET_452 || NET_46 || NET_461 || NET_462 || NET_47 || NET_471 || NET_472 || NET_48 || NET_STANDARD_20
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
#endif
        public static bool HasNoEvaluate(
            EngineFlags flags
            )
        {
            return HasFlags(flags, EngineFlags.NoEvaluate, true);
        }

        ///////////////////////////////////////////////////////////////////////

#if NET_45 || NET_451 || NET_452 || NET_46 || NET_461 || NET_462 || NET_47 || NET_471 || NET_472 || NET_48 || NET_STANDARD_20
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
#endif
        public static bool HasNoRemote(
            EngineFlags flags
            )
        {
            return HasFlags(flags, EngineFlags.NoRemote, true);
        }

        ///////////////////////////////////////////////////////////////////////

#if NET_45 || NET_451 || NET_452 || NET_46 || NET_461 || NET_462 || NET_47 || NET_471 || NET_472 || NET_48 || NET_STANDARD_20
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
#endif
        public static bool HasExactMatch(
            EngineFlags flags
            ) /* USED BY CORE RESOLVER */
        {
            return HasFlags(flags, EngineFlags.ExactMatch, true);
        }

        ///////////////////////////////////////////////////////////////////////

#if NET_45 || NET_451 || NET_452 || NET_46 || NET_461 || NET_462 || NET_47 || NET_471 || NET_472 || NET_48 || NET_STANDARD_20
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
#endif
        public static bool HasNoUnknown(
            EngineFlags flags
            )
        {
            return HasFlags(flags, EngineFlags.NoUnknown, true);
        }

        ///////////////////////////////////////////////////////////////////////

#if NET_45 || NET_451 || NET_452 || NET_46 || NET_461 || NET_462 || NET_47 || NET_471 || NET_472 || NET_48 || NET_STANDARD_20
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
#endif
        public static bool HasNoResetResult(
            EngineFlags flags
            )
        {
            return HasFlags(flags, EngineFlags.NoResetResult, true);
        }

        ///////////////////////////////////////////////////////////////////////

#if NET_45 || NET_451 || NET_452 || NET_46 || NET_461 || NET_462 || NET_47 || NET_471 || NET_472 || NET_48 || NET_STANDARD_20
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
#endif
        public static bool HasNoResetError(
            EngineFlags flags
            )
        {
            return HasFlags(flags, EngineFlags.NoResetError, true);
        }

        ///////////////////////////////////////////////////////////////////////

#if NET_45 || NET_451 || NET_452 || NET_46 || NET_461 || NET_462 || NET_47 || NET_471 || NET_472 || NET_48 || NET_STANDARD_20
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
#endif
        public static bool HasNoSafeFunction(
            EngineFlags flags
            )
        {
            return HasFlags(flags, EngineFlags.NoSafeFunction, true);
        }

        ///////////////////////////////////////////////////////////////////////

#if NET_45 || NET_451 || NET_452 || NET_46 || NET_461 || NET_462 || NET_47 || NET_471 || NET_472 || NET_48 || NET_STANDARD_20
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
#endif
        public static bool HasNoSubstitute(
            EngineFlags flags
            )
        {
            return HasFlags(flags, EngineFlags.NoSubstitute, true);
        }

        ///////////////////////////////////////////////////////////////////////

#if NET_45 || NET_451 || NET_452 || NET_46 || NET_461 || NET_462 || NET_47 || NET_471 || NET_472 || NET_48 || NET_STANDARD_20
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
#endif
        public static bool HasBracketTerminator(
            EngineFlags flags
            )
        {
            return HasFlags(flags, EngineFlags.BracketTerminator, true);
        }

        ///////////////////////////////////////////////////////////////////////

#if NET_45 || NET_451 || NET_452 || NET_46 || NET_461 || NET_462 || NET_47 || NET_471 || NET_472 || NET_48 || NET_STANDARD_20
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
#endif
        public static bool HasUseIExecutes(
            EngineFlags flags
            )
        {
            return HasFlags(flags, EngineFlags.UseIExecutes, true);
        }

        ///////////////////////////////////////////////////////////////////////

#if NET_45 || NET_451 || NET_452 || NET_46 || NET_461 || NET_462 || NET_47 || NET_471 || NET_472 || NET_48 || NET_STANDARD_20
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
#endif
        public static bool HasUseCommands(
            EngineFlags flags
            )
        {
            return HasFlags(flags, EngineFlags.UseCommands, true);
        }

        ///////////////////////////////////////////////////////////////////////

#if NET_45 || NET_451 || NET_452 || NET_46 || NET_461 || NET_462 || NET_47 || NET_471 || NET_472 || NET_48 || NET_STANDARD_20
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
#endif
        public static bool HasUseProcedures(
            EngineFlags flags
            )
        {
            return HasFlags(flags, EngineFlags.UseProcedures, true);
        }

        ///////////////////////////////////////////////////////////////////////

#if NET_45 || NET_451 || NET_452 || NET_46 || NET_461 || NET_462 || NET_47 || NET_471 || NET_472 || NET_48 || NET_STANDARD_20
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
#endif
        public static bool HasForceSoftEof(
            EngineFlags flags
            )
        {
            return HasFlags(flags, EngineFlags.ForceSoftEof, true);
        }

        ///////////////////////////////////////////////////////////////////////

#if NET_45 || NET_451 || NET_452 || NET_46 || NET_461 || NET_462 || NET_47 || NET_471 || NET_472 || NET_48 || NET_STANDARD_20
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
#endif
        public static bool HasSeekSoftEof(
            EngineFlags flags
            )
        {
            return HasFlags(flags, EngineFlags.SeekSoftEof, true);
        }

        ///////////////////////////////////////////////////////////////////////

#if NET_45 || NET_451 || NET_452 || NET_46 || NET_461 || NET_462 || NET_47 || NET_471 || NET_472 || NET_48 || NET_STANDARD_20
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
#endif
        public static bool HasPostScriptBytes(
            EngineFlags flags
            )
        {
            return HasFlags(flags, EngineFlags.PostScriptBytes, true);
        }

        ///////////////////////////////////////////////////////////////////////

#if NET_45 || NET_451 || NET_452 || NET_46 || NET_461 || NET_462 || NET_47 || NET_471 || NET_472 || NET_48 || NET_STANDARD_20
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
#endif
        public static bool HasNoPolicy(
            EngineFlags flags
            )
        {
            return HasFlags(flags, EngineFlags.NoPolicy, true);
        }

        ///////////////////////////////////////////////////////////////////////

#if NET_45 || NET_451 || NET_452 || NET_46 || NET_461 || NET_462 || NET_47 || NET_471 || NET_472 || NET_48 || NET_STANDARD_20
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
#endif
        public static bool HasGetHidden(
            EngineFlags flags
            )
        {
            return HasFlags(flags, EngineFlags.GetHidden, true);
        }

        ///////////////////////////////////////////////////////////////////////

#if NET_45 || NET_451 || NET_452 || NET_46 || NET_461 || NET_462 || NET_47 || NET_471 || NET_472 || NET_48 || NET_STANDARD_20
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
#endif
        public static bool HasMatchHidden(
            EngineFlags flags
            )
        {
            return HasFlags(flags, EngineFlags.MatchHidden, true);
        }

        ///////////////////////////////////////////////////////////////////////

#if NET_45 || NET_451 || NET_452 || NET_46 || NET_461 || NET_462 || NET_47 || NET_471 || NET_472 || NET_48 || NET_STANDARD_20
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
#endif
        public static bool HasIgnoreHidden(
            EngineFlags flags
            )
        {
            return HasFlags(flags, EngineFlags.IgnoreHidden, true);
        }

        ///////////////////////////////////////////////////////////////////////

#if NET_45 || NET_451 || NET_452 || NET_46 || NET_461 || NET_462 || NET_47 || NET_471 || NET_472 || NET_48 || NET_STANDARD_20
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
#endif
        public static bool HasToExecute(
            EngineFlags flags
            )
        {
            return HasFlags(flags, EngineFlags.ToExecute, true);
        }

        ///////////////////////////////////////////////////////////////////////

#if NET_45 || NET_451 || NET_452 || NET_46 || NET_461 || NET_462 || NET_47 || NET_471 || NET_472 || NET_48 || NET_STANDARD_20
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
#endif
        public static bool HasUseHidden(
            EngineFlags flags
            )
        {
            return HasFlags(flags, EngineFlags.UseHidden, true);
        }

        ///////////////////////////////////////////////////////////////////////

#if NET_45 || NET_451 || NET_452 || NET_46 || NET_461 || NET_462 || NET_47 || NET_471 || NET_472 || NET_48 || NET_STANDARD_20
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
#endif
        public static bool HasInvokeHidden(
            EngineFlags flags
            )
        {
            return HasFlags(flags, EngineFlags.InvokeHidden, true);
        }

        ///////////////////////////////////////////////////////////////////////

#if NET_45 || NET_451 || NET_452 || NET_46 || NET_461 || NET_462 || NET_47 || NET_471 || NET_472 || NET_48 || NET_STANDARD_20
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
#endif
        public static bool HasGlobalOnly(
            EngineFlags flags
            )
        {
            return HasFlags(flags, EngineFlags.GlobalOnly, true);
        }

        ///////////////////////////////////////////////////////////////////////

#if NET_45 || NET_451 || NET_452 || NET_46 || NET_461 || NET_462 || NET_47 || NET_471 || NET_472 || NET_48 || NET_STANDARD_20
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
#endif
        public static bool HasUseInterpreter(
            EngineFlags flags
            )
        {
            return HasFlags(flags, EngineFlags.UseInterpreter, true);
        }

        ///////////////////////////////////////////////////////////////////////

#if NET_45 || NET_451 || NET_452 || NET_46 || NET_461 || NET_462 || NET_47 || NET_471 || NET_472 || NET_48 || NET_STANDARD_20
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
#endif
        public static bool HasExternalScript(
            EngineFlags flags
            )
        {
            return HasFlags(flags, EngineFlags.ExternalScript, true);
        }

        ///////////////////////////////////////////////////////////////////////

#if NET_45 || NET_451 || NET_452 || NET_46 || NET_461 || NET_462 || NET_47 || NET_471 || NET_472 || NET_48 || NET_STANDARD_20
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
#if NET_45 || NET_451 || NET_452 || NET_46 || NET_461 || NET_462 || NET_47 || NET_471 || NET_472 || NET_48 || NET_STANDARD_20
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

#if NET_45 || NET_451 || NET_452 || NET_46 || NET_461 || NET_462 || NET_47 || NET_471 || NET_472 || NET_48 || NET_STANDARD_20
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
#endif
        public static bool HasIgnoreRootedFileName(
            EngineFlags flags
            )
        {
            return HasFlags(flags, EngineFlags.IgnoreRootedFileName, true);
        }

        ///////////////////////////////////////////////////////////////////////

#if NET_45 || NET_451 || NET_452 || NET_46 || NET_461 || NET_462 || NET_47 || NET_471 || NET_472 || NET_48 || NET_STANDARD_20
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
#endif
        public static bool HasNoFileNameOnly(
            EngineFlags flags
            )
        {
            return HasFlags(flags, EngineFlags.NoFileNameOnly, true);
        }

        ///////////////////////////////////////////////////////////////////////

#if NET_45 || NET_451 || NET_452 || NET_46 || NET_461 || NET_462 || NET_47 || NET_471 || NET_472 || NET_48 || NET_STANDARD_20
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
#endif
        public static bool HasNoRawName(
            EngineFlags flags
            )
        {
            return HasFlags(flags, EngineFlags.NoRawName, true);
        }

        ///////////////////////////////////////////////////////////////////////

#if NET_45 || NET_451 || NET_452 || NET_46 || NET_461 || NET_462 || NET_47 || NET_471 || NET_472 || NET_48 || NET_STANDARD_20
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
#endif
        public static bool HasAllErrors(
            EngineFlags flags
            )
        {
            return HasFlags(flags, EngineFlags.AllErrors, true);
        }

        ///////////////////////////////////////////////////////////////////////

#if NET_45 || NET_451 || NET_452 || NET_46 || NET_461 || NET_462 || NET_47 || NET_471 || NET_472 || NET_48 || NET_STANDARD_20
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
#endif
        public static bool HasNoDefaultError(
            EngineFlags flags
            )
        {
            return HasFlags(flags, EngineFlags.NoDefaultError, true);
        }

        ///////////////////////////////////////////////////////////////////////

#if NET_45 || NET_451 || NET_452 || NET_46 || NET_461 || NET_462 || NET_47 || NET_471 || NET_472 || NET_48 || NET_STANDARD_20
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
#if NET_45 || NET_451 || NET_452 || NET_46 || NET_461 || NET_462 || NET_47 || NET_471 || NET_472 || NET_48 || NET_STANDARD_20
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
#if NET_45 || NET_451 || NET_452 || NET_46 || NET_461 || NET_462 || NET_47 || NET_471 || NET_472 || NET_48 || NET_STANDARD_20
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

#if NET_45 || NET_451 || NET_452 || NET_46 || NET_461 || NET_462 || NET_47 || NET_471 || NET_472 || NET_48 || NET_STANDARD_20
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
#endif
        public static bool HasNoUsageData(
            EngineFlags flags
            )
        {
            return HasFlags(flags, EngineFlags.NoUsageData, true);
        }

        ///////////////////////////////////////////////////////////////////////

#if NET_45 || NET_451 || NET_452 || NET_46 || NET_461 || NET_462 || NET_47 || NET_471 || NET_472 || NET_48 || NET_STANDARD_20
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
#endif
        public static bool HasNoNullArgument(
            EngineFlags flags
            )
        {
            return HasFlags(flags, EngineFlags.NoNullArgument, true);
        }

        ///////////////////////////////////////////////////////////////////////

#if NET_45 || NET_451 || NET_452 || NET_46 || NET_461 || NET_462 || NET_47 || NET_471 || NET_472 || NET_48 || NET_STANDARD_20
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
#if NET_45 || NET_451 || NET_452 || NET_46 || NET_461 || NET_462 || NET_47 || NET_471 || NET_472 || NET_48 || NET_STANDARD_20
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
