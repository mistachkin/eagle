/*
 * FlagOps.cs --
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
using Eagle._Attributes;

#if NATIVE && TCL
using Eagle._Components.Private.Tcl;
#endif

using Eagle._Components.Public;

namespace Eagle._Components.Private
{
    [ObjectId("c3397500-b84c-4b5e-a0cf-ea4dd6042d6b")]
    internal static class FlagOps
    {
        public static bool HasFlags(
            ulong flags,
            ulong hasFlags,
            bool all
            )
        {
            if (all)
                return ((flags & hasFlags) == hasFlags);
            else
                return ((flags & hasFlags) != 0);
        }

        ///////////////////////////////////////////////////////////////////////

        public static bool HasFlags(
            AliasFlags flags,
            AliasFlags hasFlags,
            bool all
            )
        {
            if (all)
                return ((flags & hasFlags) == hasFlags);
            else
                return ((flags & hasFlags) != AliasFlags.None);
        }

        ///////////////////////////////////////////////////////////////////////

        public static bool HasFlags(
            ArgumentFlags flags,
            ArgumentFlags hasFlags,
            bool all
            )
        {
            if (all)
                return ((flags & hasFlags) == hasFlags);
            else
                return ((flags & hasFlags) != ArgumentFlags.None);
        }

        ///////////////////////////////////////////////////////////////////////

        public static bool HasFlags(
            Base26FormattingOption flags,
            Base26FormattingOption hasFlags,
            bool all
            )
        {
            if (all)
                return ((flags & hasFlags) == hasFlags);
            else
                return ((flags & hasFlags) != Base26FormattingOption.None);
        }

        ///////////////////////////////////////////////////////////////////////

#if TEST
        public static bool HasFlags(
            BufferedTraceFlags flags,
            BufferedTraceFlags hasFlags,
            bool all
            )
        {
            if (all)
                return ((flags & hasFlags) == hasFlags);
            else
                return ((flags & hasFlags) != BufferedTraceFlags.None);
        }
#endif

        ///////////////////////////////////////////////////////////////////////

        public static bool HasFlags(
            ByRefArgumentFlags flags,
            ByRefArgumentFlags hasFlags,
            bool all
            )
        {
            if (all)
                return ((flags & hasFlags) == hasFlags);
            else
                return ((flags & hasFlags) != ByRefArgumentFlags.None);
        }

        ///////////////////////////////////////////////////////////////////////

        public static bool HasFlags(
            BindingFlags flags,
            BindingFlags hasFlags,
            bool all
            )
        {
            if (all)
                return ((flags & hasFlags) == hasFlags);
            else
                return ((flags & hasFlags) != BindingFlags.Default);
        }

        ///////////////////////////////////////////////////////////////////////

        public static bool HasFlags(
            BreakpointType flags,
            BreakpointType hasFlags,
            bool all
            )
        {
            if (all)
                return ((flags & hasFlags) == hasFlags);
            else
                return ((flags & hasFlags) != BreakpointType.None);
        }

        ///////////////////////////////////////////////////////////////////////

#if ARGUMENT_CACHE || LIST_CACHE || PARSE_CACHE || TYPE_CACHE || COM_TYPE_CACHE
        public static bool HasFlags(
            CacheInformationFlags flags,
            CacheInformationFlags hasFlags,
            bool all
            )
        {
            if (all)
                return ((flags & hasFlags) == hasFlags);
            else
                return ((flags & hasFlags) != CacheInformationFlags.None);
        }
#endif

        ///////////////////////////////////////////////////////////////////////

#if ARGUMENT_CACHE || LIST_CACHE || PARSE_CACHE || EXECUTE_CACHE || TYPE_CACHE || COM_TYPE_CACHE
        public static bool HasFlags(
            CacheFlags flags,
            CacheFlags hasFlags,
            bool all
            )
        {
            if (all)
                return ((flags & hasFlags) == hasFlags);
            else
                return ((flags & hasFlags) != CacheFlags.None);
        }
#endif

        ///////////////////////////////////////////////////////////////////////

        public static bool HasFlags(
            CallFrameFlags flags,
            CallFrameFlags hasFlags,
            bool all
            )
        {
            if (all)
                return ((flags & hasFlags) == hasFlags);
            else
                return ((flags & hasFlags) != CallFrameFlags.None);
        }

        ///////////////////////////////////////////////////////////////////////

        public static bool HasFlags(
            CallbackFlags flags,
            CallbackFlags hasFlags,
            bool all
            )
        {
            if (all)
                return ((flags & hasFlags) == hasFlags);
            else
                return ((flags & hasFlags) != CallbackFlags.None);
        }

        ///////////////////////////////////////////////////////////////////////

        public static bool HasFlags(
            CancelFlags flags,
            CancelFlags hasFlags,
            bool all
            )
        {
            if (all)
                return ((flags & hasFlags) == hasFlags);
            else
                return ((flags & hasFlags) != CancelFlags.None);
        }

        ///////////////////////////////////////////////////////////////////////

        public static bool HasFlags(
            ChannelType flags,
            ChannelType hasFlags,
            bool all
            )
        {
            if (all)
                return ((flags & hasFlags) == hasFlags);
            else
                return ((flags & hasFlags) != ChannelType.None);
        }

        ///////////////////////////////////////////////////////////////////////

        public static bool HasFlags(
            CloneFlags flags,
            CloneFlags hasFlags,
            bool all
            )
        {
            if (all)
                return ((flags & hasFlags) == hasFlags);
            else
                return ((flags & hasFlags) != CloneFlags.None);
        }

        ///////////////////////////////////////////////////////////////////////

        public static bool HasFlags(
            CommandFlags flags,
            CommandFlags hasFlags,
            bool all
            )
        {
            if (all)
                return ((flags & hasFlags) == hasFlags);
            else
                return ((flags & hasFlags) != CommandFlags.None);
        }

        ///////////////////////////////////////////////////////////////////////

        public static bool HasFlags(
            ConfigurationFlags flags,
            ConfigurationFlags hasFlags,
            bool all
            )
        {
            if (all)
                return ((flags & hasFlags) == hasFlags);
            else
                return ((flags & hasFlags) != ConfigurationFlags.None);
        }

        ///////////////////////////////////////////////////////////////////////

#if CONSOLE
        public static bool HasFlags(
            ConsoleModifiers flags,
            ConsoleModifiers hasFlags,
            bool all
            )
        {
            if (all)
                return ((flags & hasFlags) == hasFlags);
            else
                return ((flags & hasFlags) != (ConsoleModifiers)0);
        }
#endif

        ///////////////////////////////////////////////////////////////////////

        public static bool HasFlags(
            CreateFlags flags,
            CreateFlags hasFlags,
            bool all
            )
        {
            if (all)
                return ((flags & hasFlags) == hasFlags);
            else
                return ((flags & hasFlags) != CreateFlags.None);
        }

        ///////////////////////////////////////////////////////////////////////

        public static bool HasFlags(
            CreationFlagTypes flags,
            CreationFlagTypes hasFlags,
            bool all
            )
        {
            if (all)
                return ((flags & hasFlags) == hasFlags);
            else
                return ((flags & hasFlags) != CreationFlagTypes.None);
        }

        ///////////////////////////////////////////////////////////////////////

        public static bool HasFlags(
            CreateStateFlags flags,
            CreateStateFlags hasFlags,
            bool all
            )
        {
            if (all)
                return ((flags & hasFlags) == hasFlags);
            else
                return ((flags & hasFlags) != CreateStateFlags.None);
        }

        ///////////////////////////////////////////////////////////////////////

        public static bool HasFlags(
            DataFlags flags,
            DataFlags hasFlags,
            bool all
            )
        {
            if (all)
                return ((flags & hasFlags) == hasFlags);
            else
                return ((flags & hasFlags) != DataFlags.None);
        }

        ///////////////////////////////////////////////////////////////////////

#if DATA
        public static bool HasFlags(
            DbVariableFlags flags,
            DbVariableFlags hasFlags,
            bool all
            )
        {
            if (all)
                return ((flags & hasFlags) == hasFlags);
            else
                return ((flags & hasFlags) != DbVariableFlags.None);
        }
#endif

        ///////////////////////////////////////////////////////////////////////

        public static bool HasFlags(
            DebugEmergencyLevel flags,
            DebugEmergencyLevel hasFlags,
            bool all
            )
        {
            if (all)
                return ((flags & hasFlags) == hasFlags);
            else
                return ((flags & hasFlags) != DebugEmergencyLevel.None);
        }

        ///////////////////////////////////////////////////////////////////////

        public static bool HasFlags(
            DebugPathFlags flags,
            DebugPathFlags hasFlags,
            bool all
            )
        {
            if (all)
                return ((flags & hasFlags) == hasFlags);
            else
                return ((flags & hasFlags) != DebugPathFlags.None);
        }

        ///////////////////////////////////////////////////////////////////////

        public static bool HasFlags(
            DetailFlags flags,
            DetailFlags hasFlags,
            bool all
            )
        {
            if (all)
                return ((flags & hasFlags) == hasFlags);
            else
                return ((flags & hasFlags) != DetailFlags.None);
        }

        ///////////////////////////////////////////////////////////////////////

        public static bool HasFlags(
            DelegateFlags flags,
            DelegateFlags hasFlags,
            bool all
            )
        {
            if (all)
                return ((flags & hasFlags) == hasFlags);
            else
                return ((flags & hasFlags) != DelegateFlags.None);
        }

        ///////////////////////////////////////////////////////////////////////

        public static bool HasFlags(
            DetectFlags flags,
            DetectFlags hasFlags,
            bool all
            )
        {
            if (all)
                return ((flags & hasFlags) == hasFlags);
            else
                return ((flags & hasFlags) != DetectFlags.None);
        }

        ///////////////////////////////////////////////////////////////////////

        public static bool HasFlags(
            DisableFlags flags,
            DisableFlags hasFlags,
            bool all
            )
        {
            if (all)
                return ((flags & hasFlags) == hasFlags);
            else
                return ((flags & hasFlags) != DisableFlags.None);
        }

        ///////////////////////////////////////////////////////////////////////

        public static bool HasFlags(
            DisposalPhase flags,
            DisposalPhase hasFlags,
            bool all
            )
        {
            if (all)
                return ((flags & hasFlags) == hasFlags);
            else
                return ((flags & hasFlags) != DisposalPhase.None);
        }

        ///////////////////////////////////////////////////////////////////////

        public static bool HasFlags(
            EventFlags flags,
            EventFlags hasFlags,
            bool all
            )
        {
            if (all)
                return ((flags & hasFlags) == hasFlags);
            else
                return ((flags & hasFlags) != EventFlags.None);
        }

        ///////////////////////////////////////////////////////////////////////

        public static bool HasFlags(
            EventWaitFlags flags,
            EventWaitFlags hasFlags,
            bool all
            )
        {
            if (all)
                return ((flags & hasFlags) == hasFlags);
            else
                return ((flags & hasFlags) != EventWaitFlags.None);
        }

        ///////////////////////////////////////////////////////////////////////

        public static bool HasFlags(
            ExecutionPolicy flags,
            ExecutionPolicy hasFlags,
            bool all
            )
        {
            if (all)
                return ((flags & hasFlags) == hasFlags);
            else
                return ((flags & hasFlags) != ExecutionPolicy.None);
        }

        ///////////////////////////////////////////////////////////////////////

        public static bool HasFlags(
            ExecutionPolicy? flags,
            ExecutionPolicy hasFlags,
            bool all
            )
        {
            ExecutionPolicy localFlags = (flags != null) ?
                (ExecutionPolicy)flags : ExecutionPolicy.None;

            return FlagOps.HasFlags(localFlags, hasFlags, all);
        }

        ///////////////////////////////////////////////////////////////////////

#if APPDOMAINS
        public static bool HasFlags(
            FieldAttributes flags,
            FieldAttributes hasFlags,
            bool all
            )
        {
            if (all)
                return ((flags & hasFlags) == hasFlags);
            else
                return ((flags & hasFlags) != (FieldAttributes)0);
        }
#endif

        ///////////////////////////////////////////////////////////////////////

        public static bool HasFlags(
            FileAttributes flags,
            FileAttributes hasFlags,
            bool all
            )
        {
            if (all)
                return ((flags & hasFlags) == hasFlags);
            else
                return ((flags & hasFlags) != (FileAttributes)0);
        }

        ///////////////////////////////////////////////////////////////////////

#if NATIVE && WINDOWS
        public static bool HasFlags(
            FileFlagsAndAttributes flags,
            FileFlagsAndAttributes hasFlags,
            bool all
            )
        {
            if (all)
                return ((flags & hasFlags) == hasFlags);
            else
                return ((flags & hasFlags) != FileFlagsAndAttributes.FILE_ATTRIBUTE_NONE);
        }
#endif

        ///////////////////////////////////////////////////////////////////////

        public static bool HasFlags(
            FilePermission flags,
            FilePermission hasFlags,
            bool all
            )
        {
            if (all)
                return ((flags & hasFlags) == hasFlags);
            else
                return ((flags & hasFlags) != FilePermission.None);
        }

        ///////////////////////////////////////////////////////////////////////

        public static bool HasFlags(
            FileSearchFlags flags,
            FileSearchFlags hasFlags,
            bool all
            )
        {
            if (all)
                return ((flags & hasFlags) == hasFlags);
            else
                return ((flags & hasFlags) != FileSearchFlags.None);
        }

        ///////////////////////////////////////////////////////////////////////

#if NATIVE && TCL
        public static bool HasFlags(
            FindFlags flags,
            FindFlags hasFlags,
            bool all
            )
        {
            if (all)
                return ((flags & hasFlags) == hasFlags);
            else
                return ((flags & hasFlags) != FindFlags.None);
        }
#endif

        ///////////////////////////////////////////////////////////////////////

        public static bool HasFlags(
            FrameworkFlags flags,
            FrameworkFlags hasFlags,
            bool all
            )
        {
            if (all)
                return ((flags & hasFlags) == hasFlags);
            else
                return ((flags & hasFlags) != FrameworkFlags.None);
        }

        ///////////////////////////////////////////////////////////////////////

        public static bool HasFlags(
            FunctionFlags flags,
            FunctionFlags hasFlags,
            bool all
            )
        {
            if (all)
                return ((flags & hasFlags) == hasFlags);
            else
                return ((flags & hasFlags) != FunctionFlags.None);
        }

        ///////////////////////////////////////////////////////////////////////

        public static bool HasFlags(
            GarbageFlags flags,
            GarbageFlags hasFlags,
            bool all
            )
        {
            if (all)
                return ((flags & hasFlags) == hasFlags);
            else
                return ((flags & hasFlags) != GarbageFlags.None);
        }

        ///////////////////////////////////////////////////////////////////////

        public static bool HasFlags(
            HeaderFlags flags,
            HeaderFlags hasFlags,
            bool all
            )
        {
            if (all)
                return ((flags & hasFlags) == hasFlags);
            else
                return ((flags & hasFlags) != HeaderFlags.None);
        }

        ///////////////////////////////////////////////////////////////////////

#if HISTORY
        public static bool HasFlags(
            HistoryFlags flags,
            HistoryFlags hasFlags,
            bool all
            )
        {
            if (all)
                return ((flags & hasFlags) == hasFlags);
            else
                return ((flags & hasFlags) != HistoryFlags.None);
        }
#endif

        ///////////////////////////////////////////////////////////////////////

        public static bool HasFlags(
            HomeFlags flags,
            HomeFlags hasFlags,
            bool all
            )
        {
            if (all)
                return ((flags & hasFlags) == hasFlags);
            else
                return ((flags & hasFlags) != HomeFlags.None);
        }

        ///////////////////////////////////////////////////////////////////////

        public static bool HasFlags(
            HostFlags flags,
            HostFlags hasFlags,
            bool all
            )
        {
            if (all)
                return ((flags & hasFlags) == hasFlags);
            else
                return ((flags & hasFlags) != HostFlags.None);
        }

        ///////////////////////////////////////////////////////////////////////

        public static bool HasFlags(
            HostCreateFlags flags,
            HostCreateFlags hasFlags,
            bool all
            )
        {
            if (all)
                return ((flags & hasFlags) == hasFlags);
            else
                return ((flags & hasFlags) != HostCreateFlags.None);
        }

        ///////////////////////////////////////////////////////////////////////

        public static bool HasFlags(
            HostStreamFlags flags,
            HostStreamFlags hasFlags,
            bool all
            )
        {
            if (all)
                return ((flags & hasFlags) == hasFlags);
            else
                return ((flags & hasFlags) != HostStreamFlags.None);
        }

        ///////////////////////////////////////////////////////////////////////

        public static bool HasFlags(
            HostTestFlags flags,
            HostTestFlags hasFlags,
            bool all
            )
        {
            if (all)
                return ((flags & hasFlags) == hasFlags);
            else
                return ((flags & hasFlags) != HostTestFlags.None);
        }

        ///////////////////////////////////////////////////////////////////////

        public static bool HasFlags(
            IdentifierKind flags,
            IdentifierKind hasFlags,
            bool all
            )
        {
            if (all)
                return ((flags & hasFlags) == hasFlags);
            else
                return ((flags & hasFlags) != IdentifierKind.None);
        }

        ///////////////////////////////////////////////////////////////////////

        public static bool HasFlags(
            InfoPathType flags,
            InfoPathType hasFlags,
            bool all
            )
        {
            if (all)
                return ((flags & hasFlags) == hasFlags);
            else
                return ((flags & hasFlags) != InfoPathType.None);
        }

        ///////////////////////////////////////////////////////////////////////

        public static bool HasFlags(
            InitializeFlags flags,
            InitializeFlags hasFlags,
            bool all
            )
        {
            if (all)
                return ((flags & hasFlags) == hasFlags);
            else
                return ((flags & hasFlags) != InitializeFlags.None);
        }

        ///////////////////////////////////////////////////////////////////////

#if SHELL
        public static bool HasFlags(
            InteractiveLoopFlags flags,
            InteractiveLoopFlags hasFlags,
            bool all
            )
        {
            if (all)
                return ((flags & hasFlags) == hasFlags);
            else
                return ((flags & hasFlags) != InteractiveLoopFlags.None);
        }
#endif

        ///////////////////////////////////////////////////////////////////////

        public static bool HasFlags(
            InterpreterFlags flags,
            InterpreterFlags hasFlags,
            bool all
            )
        {
            if (all)
                return ((flags & hasFlags) == hasFlags);
            else
                return ((flags & hasFlags) != InterpreterFlags.None);
        }

        ///////////////////////////////////////////////////////////////////////

        public static bool HasFlags(
            InterpreterStateFlags flags,
            InterpreterStateFlags hasFlags,
            bool all
            )
        {
            if (all)
                return ((flags & hasFlags) == hasFlags);
            else
                return ((flags & hasFlags) != InterpreterStateFlags.None);
        }

        ///////////////////////////////////////////////////////////////////////

        public static bool HasFlags(
            InterpreterTestFlags flags,
            InterpreterTestFlags hasFlags,
            bool all
            )
        {
            if (all)
                return ((flags & hasFlags) == hasFlags);
            else
                return ((flags & hasFlags) != InterpreterTestFlags.None);
        }

        ///////////////////////////////////////////////////////////////////////

        public static bool HasFlags(
            InterpreterType flags,
            InterpreterType hasFlags,
            bool all
            )
        {
            if (all)
                return ((flags & hasFlags) == hasFlags);
            else
                return ((flags & hasFlags) != InterpreterType.None);
        }

        ///////////////////////////////////////////////////////////////////////

        public static bool HasFlags(
            IsolationDetail flags,
            IsolationDetail hasFlags,
            bool all
            )
        {
            if (all)
                return ((flags & hasFlags) == hasFlags);
            else
                return ((flags & hasFlags) != IsolationDetail.None);
        }

        ///////////////////////////////////////////////////////////////////////

#if SHELL
        public static bool HasFlags(
            KioskFlags flags,
            KioskFlags hasFlags,
            bool all
            )
        {
            if (all)
                return ((flags & hasFlags) == hasFlags);
            else
                return ((flags & hasFlags) != KioskFlags.None);
        }
#endif

        ///////////////////////////////////////////////////////////////////////

        public static bool HasFlags(
            LevelFlags flags,
            LevelFlags hasFlags,
            bool all
            )
        {
            if (all)
                return ((flags & hasFlags) == hasFlags);
            else
                return ((flags & hasFlags) != LevelFlags.None);
        }

        ///////////////////////////////////////////////////////////////////////

        public static bool HasFlags(
            ListElementFlags flags,
            ListElementFlags hasFlags,
            bool all
            )
        {
            if (all)
                return ((flags & hasFlags) == hasFlags);
            else
                return ((flags & hasFlags) != ListElementFlags.None);
        }

        ///////////////////////////////////////////////////////////////////////

#if NATIVE && TCL
        public static bool HasFlags(
            LoadFlags flags,
            LoadFlags hasFlags,
            bool all
            )
        {
            if (all)
                return ((flags & hasFlags) == hasFlags);
            else
                return ((flags & hasFlags) != LoadFlags.None);
        }
#endif

        ///////////////////////////////////////////////////////////////////////

        public static bool HasFlags(
            LogFlags flags,
            LogFlags hasFlags,
            bool all
            )
        {
            if (all)
                return ((flags & hasFlags) == hasFlags);
            else
                return ((flags & hasFlags) != LogFlags.None);
        }

        ///////////////////////////////////////////////////////////////////////

        public static bool HasFlags(
            LogFlags? flags,
            LogFlags hasFlags,
            bool all
            )
        {
            LogFlags localFlags = (flags != null) ?
                (LogFlags)flags : LogFlags.None;

            return FlagOps.HasFlags(localFlags, hasFlags, all);
        }

        ///////////////////////////////////////////////////////////////////////

        public static bool HasFlags(
            LookupFlags flags,
            LookupFlags hasFlags,
            bool all
            )
        {
            if (all)
                return ((flags & hasFlags) == hasFlags);
            else
                return ((flags & hasFlags) != LookupFlags.None);
        }

        ///////////////////////////////////////////////////////////////////////

        public static bool HasFlags(
            MakeFlags flags,
            MakeFlags hasFlags,
            bool all
            )
        {
            if (all)
                return ((flags & hasFlags) == hasFlags);
            else
                return ((flags & hasFlags) != MakeFlags.None);
        }

        ///////////////////////////////////////////////////////////////////////

        public static bool HasFlags(
            MapOpenAccess access,
            MapOpenAccess flags,
            bool all
            )
        {
            if (all)
                return ((access & flags) == flags);
            else
                return ((access & flags) != MapOpenAccess.Default);
        }

        ///////////////////////////////////////////////////////////////////////

        public static bool HasFlags(
            MarshalFlags flags,
            MarshalFlags hasFlags,
            bool all
            )
        {
            if (all)
                return ((flags & hasFlags) == hasFlags);
            else
                return ((flags & hasFlags) != MarshalFlags.None);
        }

        ///////////////////////////////////////////////////////////////////////

        public static bool HasFlags(
            MatchMode flags,
            MatchMode hasFlags,
            bool all
            )
        {
            if (all)
                return ((flags & hasFlags) == hasFlags);
            else
                return ((flags & hasFlags) != MatchMode.None);
        }

        ///////////////////////////////////////////////////////////////////////

        public static bool HasFlags(
            MethodAttributes flags,
            MethodAttributes hasFlags,
            bool all
            )
        {
            if (all)
                return ((flags & hasFlags) == hasFlags);
            else
                return ((flags & hasFlags) != (MethodAttributes)0);
        }

        ///////////////////////////////////////////////////////////////////////

        public static bool HasFlags(
            MemberTypes flags,
            MemberTypes hasFlags,
            bool all
            )
        {
            if (all)
                return ((flags & hasFlags) == hasFlags);
            else
                return ((flags & hasFlags) != (MemberTypes)0);
        }

        ///////////////////////////////////////////////////////////////////////

        public static bool HasFlags(
            MethodFlags flags,
            MethodFlags hasFlags,
            bool all
            )
        {
            if (all)
                return ((flags & hasFlags) == hasFlags);
            else
                return ((flags & hasFlags) != MethodFlags.None);
        }

        ///////////////////////////////////////////////////////////////////////

#if EMIT && NATIVE && LIBRARY
        public static bool HasFlags(
            ModuleFlags flags,
            ModuleFlags hasFlags,
            bool all
            )
        {
            if (all)
                return ((flags & hasFlags) == hasFlags);
            else
                return ((flags & hasFlags) != ModuleFlags.None);
        }
#endif

        ///////////////////////////////////////////////////////////////////////

        public static bool HasFlags(
            NamespaceFlags flags,
            NamespaceFlags hasFlags,
            bool all
            )
        {
            if (all)
                return ((flags & hasFlags) == hasFlags);
            else
                return ((flags & hasFlags) != NamespaceFlags.None);
        }

        ///////////////////////////////////////////////////////////////////////

#if NOTIFY || NOTIFY_OBJECT
        public static bool HasFlags(
            NotifyFlags flags,
            NotifyFlags hasFlags,
            bool all
            )
        {
            if (all)
                return ((flags & hasFlags) == hasFlags);
            else
                return ((flags & hasFlags) != NotifyFlags.None);
        }

        ///////////////////////////////////////////////////////////////////////

        public static bool HasFlags(
            NotifyType flags,
            NotifyType hasFlags,
            bool all
            )
        {
            if (all)
                return ((flags & hasFlags) == hasFlags);
            else
                return ((flags & hasFlags) != NotifyType.None);
        }
#endif

        ///////////////////////////////////////////////////////////////////////

        public static bool HasFlags(
            ObjectFlags flags,
            ObjectFlags hasFlags,
            bool all
            )
        {
            if (all)
                return ((flags & hasFlags) == hasFlags);
            else
                return ((flags & hasFlags) != ObjectFlags.None);
        }

        ///////////////////////////////////////////////////////////////////////

        public static bool HasFlags(
            ObjectNamespace flags,
            ObjectNamespace hasFlags,
            bool all
            )
        {
            if (all)
                return ((flags & hasFlags) == hasFlags);
            else
                return ((flags & hasFlags) != ObjectNamespace.None);
        }

        ///////////////////////////////////////////////////////////////////////

        public static bool HasFlags(
            ObjectOptionType flags,
            ObjectOptionType hasFlags,
            bool all
            )
        {
            if (all)
                return ((flags & hasFlags) == hasFlags);
            else
                return ((flags & hasFlags) != ObjectOptionType.None);
        }

        ///////////////////////////////////////////////////////////////////////

        public static bool HasFlags(
            OperatorFlags flags,
            OperatorFlags hasFlags,
            bool all
            )
        {
            if (all)
                return ((flags & hasFlags) == hasFlags);
            else
                return ((flags & hasFlags) != OperatorFlags.None);
        }

        ///////////////////////////////////////////////////////////////////////

        public static bool HasFlags(
            OptionFlags flags,
            OptionFlags hasFlags,
            bool all
            )
        {
            if (all)
                return ((flags & hasFlags) == hasFlags);
            else
                return ((flags & hasFlags) != OptionFlags.None);
        }

        ///////////////////////////////////////////////////////////////////////

        public static bool HasFlags(
            OptionBehaviorFlags flags,
            OptionBehaviorFlags hasFlags,
            bool all
            )
        {
            if (all)
                return ((flags & hasFlags) == hasFlags);
            else
                return ((flags & hasFlags) != OptionBehaviorFlags.None);
        }

        ///////////////////////////////////////////////////////////////////////

        public static bool HasFlags(
            OptionOriginFlags flags,
            OptionOriginFlags hasFlags,
            bool all
            )
        {
            if (all)
                return ((flags & hasFlags) == hasFlags);
            else
                return ((flags & hasFlags) != OptionOriginFlags.None);
        }

        ///////////////////////////////////////////////////////////////////////

        public static bool HasFlags(
            OutputStyle flags,
            OutputStyle hasFlags,
            bool all
            )
        {
            if (all)
                return ((flags & hasFlags) == hasFlags);
            else
                return ((flags & hasFlags) != OutputStyle.None);
        }

        ///////////////////////////////////////////////////////////////////////

        public static bool HasFlags(
            PackageFlags flags,
            PackageFlags hasFlags,
            bool all
            )
        {
            if (all)
                return ((flags & hasFlags) == hasFlags);
            else
                return ((flags & hasFlags) != PackageFlags.None);
        }

        ///////////////////////////////////////////////////////////////////////

        public static bool HasFlags(
            PackageIfNeededFlags flags,
            PackageIfNeededFlags hasFlags,
            bool all
            )
        {
            if (all)
                return ((flags & hasFlags) == hasFlags);
            else
                return ((flags & hasFlags) != PackageIfNeededFlags.None);
        }

        ///////////////////////////////////////////////////////////////////////

        public static bool HasFlags(
            PackageIndexFlags flags,
            PackageIndexFlags hasFlags,
            bool all
            )
        {
            if (all)
                return ((flags & hasFlags) == hasFlags);
            else
                return ((flags & hasFlags) != PackageIndexFlags.None);
        }

        ///////////////////////////////////////////////////////////////////////

        public static bool HasFlags(
            PackageType flags,
            PackageType hasFlags,
            bool all
            )
        {
            if (all)
                return ((flags & hasFlags) == hasFlags);
            else
                return ((flags & hasFlags) != PackageType.None);
        }

        ///////////////////////////////////////////////////////////////////////

        public static bool HasFlags(
            PathFlags flags,
            PathFlags hasFlags,
            bool all
            )
        {
            if (all)
                return ((flags & hasFlags) == hasFlags);
            else
                return ((flags & hasFlags) != PathFlags.None);
        }

        ///////////////////////////////////////////////////////////////////////

        public static bool HasFlags(
            PathType flags,
            PathType hasFlags,
            bool all
            )
        {
            if (all)
                return ((flags & hasFlags) == hasFlags);
            else
                return ((flags & hasFlags) != PathType.None);
        }

        ///////////////////////////////////////////////////////////////////////

#if TEST
        public static bool HasFlags(
            PkgInstallType flags,
            PkgInstallType hasFlags,
            bool all
            )
        {
            if (all)
                return ((flags & hasFlags) == hasFlags);
            else
                return ((flags & hasFlags) != PkgInstallType.None);
        }
#endif

        ///////////////////////////////////////////////////////////////////////

        public static bool HasFlags(
            PluginFlags flags,
            PluginFlags hasFlags,
            bool all
            )
        {
            if (all)
                return ((flags & hasFlags) == hasFlags);
            else
                return ((flags & hasFlags) != PluginFlags.None);
        }

        ///////////////////////////////////////////////////////////////////////

        public static bool HasFlags(
            PluginLoaderFlags flags,
            PluginLoaderFlags hasFlags,
            bool all
            )
        {
            if (all)
                return ((flags & hasFlags) == hasFlags);
            else
                return ((flags & hasFlags) != PluginLoaderFlags.None);
        }

        ///////////////////////////////////////////////////////////////////////

        public static bool HasFlags(
            PolicyDecisionType flags,
            PolicyDecisionType hasFlags,
            bool all
            )
        {
            if (all)
                return ((flags & hasFlags) == hasFlags);
            else
                return ((flags & hasFlags) != PolicyDecisionType.None);
        }

        ///////////////////////////////////////////////////////////////////////

        public static bool HasFlags(
            PolicyFlags flags,
            PolicyFlags hasFlags,
            bool all
            )
        {
            if (all)
                return ((flags & hasFlags) == hasFlags);
            else
                return ((flags & hasFlags) != PolicyFlags.None);
        }

        ///////////////////////////////////////////////////////////////////////

        public static bool HasFlags(
            ProcedureFlags flags,
            ProcedureFlags hasFlags,
            bool all
            )
        {
            if (all)
                return ((flags & hasFlags) == hasFlags);
            else
                return ((flags & hasFlags) != ProcedureFlags.None);
        }

        ///////////////////////////////////////////////////////////////////////

        public static bool HasFlags(
            PromptFlags flags,
            PromptFlags hasFlags,
            bool all
            )
        {
            if (all)
                return ((flags & hasFlags) == hasFlags);
            else
                return ((flags & hasFlags) != PromptFlags.None);
        }

        ///////////////////////////////////////////////////////////////////////

        public static bool HasFlags(
            QueueFlags flags,
            QueueFlags hasFlags,
            bool all
            )
        {
            if (all)
                return ((flags & hasFlags) == hasFlags);
            else
                return ((flags & hasFlags) != QueueFlags.None);
        }

        ///////////////////////////////////////////////////////////////////////

        public static bool HasFlags(
            ReadyFlags flags,
            ReadyFlags hasFlags,
            bool all
            )
        {
            if (all)
                return ((flags & hasFlags) == hasFlags);
            else
                return ((flags & hasFlags) != ReadyFlags.None);
        }

        ///////////////////////////////////////////////////////////////////////

        public static bool HasFlags(
            ReorderFlags flags,
            ReorderFlags hasFlags,
            bool all
            )
        {
            if (all)
                return ((flags & hasFlags) == hasFlags);
            else
                return ((flags & hasFlags) != ReorderFlags.None);
        }

        ///////////////////////////////////////////////////////////////////////

        public static bool HasFlags(
            ResolveFlags flags,
            ResolveFlags hasFlags,
            bool all
            )
        {
            if (all)
                return ((flags & hasFlags) == hasFlags);
            else
                return ((flags & hasFlags) != ResolveFlags.None);
        }

        ///////////////////////////////////////////////////////////////////////

        public static bool HasFlags(
            ResultFlags flags,
            ResultFlags hasFlags,
            bool all
            )
        {
            if (all)
                return ((flags & hasFlags) == hasFlags);
            else
                return ((flags & hasFlags) != ResultFlags.None);
        }

        ///////////////////////////////////////////////////////////////////////

        public static bool HasFlags(
            RuleSetType flags,
            RuleSetType hasFlags,
            bool all
            )
        {
            if (all)
                return ((flags & hasFlags) == hasFlags);
            else
                return ((flags & hasFlags) != RuleSetType.None);
        }

        ///////////////////////////////////////////////////////////////////////

        public static bool HasFlags(
            ScriptFlags flags,
            ScriptFlags hasFlags,
            bool all
            )
        {
            if (all)
                return ((flags & hasFlags) == hasFlags);
            else
                return ((flags & hasFlags) != ScriptFlags.None);
        }

        ///////////////////////////////////////////////////////////////////////

        public static bool HasFlags(
            ScriptBlockFlags flags,
            ScriptBlockFlags hasFlags,
            bool all
            )
        {
            if (all)
                return ((flags & hasFlags) == hasFlags);
            else
                return ((flags & hasFlags) != ScriptBlockFlags.None);
        }

        ///////////////////////////////////////////////////////////////////////

        public static bool HasFlags(
            ScriptDataFlags flags,
            ScriptDataFlags hasFlags,
            bool all
            )
        {
            if (all)
                return ((flags & hasFlags) == hasFlags);
            else
                return ((flags & hasFlags) != ScriptDataFlags.None);
        }

        ///////////////////////////////////////////////////////////////////////

        public static bool HasFlags(
            ScriptSecurityFlags flags,
            ScriptSecurityFlags hasFlags,
            bool all
            )
        {
            if (all)
                return ((flags & hasFlags) == hasFlags);
            else
                return ((flags & hasFlags) != ScriptSecurityFlags.None);
        }

        ///////////////////////////////////////////////////////////////////////

#if !NET_STANDARD_20 && !MONO
        public static bool HasFlags(
            SddlFlags flags,
            SddlFlags hasFlags,
            bool all
            )
        {
            if (all)
                return ((flags & hasFlags) == hasFlags);
            else
                return ((flags & hasFlags) != SddlFlags.None);
        }
#endif

        ///////////////////////////////////////////////////////////////////////

        public static bool HasFlags(
            SdkType flags,
            SdkType hasFlags,
            bool all
            )
        {
            if (all)
                return ((flags & hasFlags) == hasFlags);
            else
                return ((flags & hasFlags) != SdkType.None);
        }

        ///////////////////////////////////////////////////////////////////////

        public static bool HasFlags(
            SecretDataFlags flags,
            SecretDataFlags hasFlags,
            bool all
            )
        {
            if (all)
                return ((flags & hasFlags) == hasFlags);
            else
                return ((flags & hasFlags) != SecretDataFlags.None);
        }

        ///////////////////////////////////////////////////////////////////////

        public static bool HasFlags(
            SecretDataFlags? flags,
            SecretDataFlags hasFlags,
            bool all
            )
        {
            SecretDataFlags localFlags = (flags != null) ?
                (SecretDataFlags)flags : SecretDataFlags.None;

            return FlagOps.HasFlags(localFlags, hasFlags, all);
        }

        ///////////////////////////////////////////////////////////////////////

        public static bool HasFlags(
            SettingFlags flags,
            SettingFlags hasFlags,
            bool all
            )
        {
            if (all)
                return ((flags & hasFlags) == hasFlags);
            else
                return ((flags & hasFlags) != SettingFlags.None);
        }

        ///////////////////////////////////////////////////////////////////////

        public static bool HasFlags(
            ShutdownFlags flags,
            ShutdownFlags hasFlags,
            bool all
            )
        {
            if (all)
                return ((flags & hasFlags) == hasFlags);
            else
                return ((flags & hasFlags) != ShutdownFlags.None);
        }

        ///////////////////////////////////////////////////////////////////////

#if NATIVE && WINDOWS
        public static bool HasFlags(
            SimulatedKeyFlags flags,
            SimulatedKeyFlags hasFlags,
            bool all
            )
        {
            if (all)
                return ((flags & hasFlags) == hasFlags);
            else
                return ((flags & hasFlags) != SimulatedKeyFlags.None);
        }
#endif

        ///////////////////////////////////////////////////////////////////////

        public static bool HasFlags(
            SnippetFlags flags,
            SnippetFlags hasFlags,
            bool all
            )
        {
            if (all)
                return ((flags & hasFlags) == hasFlags);
            else
                return ((flags & hasFlags) != SnippetFlags.None);
        }

        ///////////////////////////////////////////////////////////////////////

        public static bool HasFlags(
            StreamDirection flags,
            StreamDirection hasFlags,
            bool all
            )
        {
            if (all)
                return ((flags & hasFlags) == hasFlags);
            else
                return ((flags & hasFlags) != StreamDirection.None);
        }

        ///////////////////////////////////////////////////////////////////////

        public static bool HasFlags(
            StreamFlags flags,
            StreamFlags hasFlags,
            bool all
            )
        {
            if (all)
                return ((flags & hasFlags) == hasFlags);
            else
                return ((flags & hasFlags) != StreamFlags.None);
        }

        ///////////////////////////////////////////////////////////////////////

        public static bool HasFlags(
            SubCommandFlags flags,
            SubCommandFlags hasFlags,
            bool all
            )
        {
            if (all)
                return ((flags & hasFlags) == hasFlags);
            else
                return ((flags & hasFlags) != SubCommandFlags.None);
        }

        ///////////////////////////////////////////////////////////////////////

#if NATIVE && TCL
        public static bool HasFlags(
            Tcl_VarFlags flags,
            Tcl_VarFlags hasFlags,
            bool all
            )
        {
            if (all)
                return ((flags & hasFlags) == hasFlags);
            else
                return ((flags & hasFlags) != Tcl_VarFlags.TCL_VAR_NONE);
        }
#endif

        ///////////////////////////////////////////////////////////////////////

        public static bool HasFlags(
            TestOutputType flags,
            TestOutputType hasFlags,
            bool all
            )
        {
            if (all)
                return ((flags & hasFlags) == hasFlags);
            else
                return ((flags & hasFlags) != TestOutputType.None);
        }

        ///////////////////////////////////////////////////////////////////////

#if TEST
        public static bool HasFlags(
            TestResolveFlags flags,
            TestResolveFlags hasFlags,
            bool all
            )
        {
            if (all)
                return ((flags & hasFlags) == hasFlags);
            else
                return ((flags & hasFlags) != TestResolveFlags.None);
        }
#endif

        ///////////////////////////////////////////////////////////////////////

        public static bool HasFlags(
            ThreadFlags flags,
            ThreadFlags hasFlags,
            bool all
            )
        {
            if (all)
                return ((flags & hasFlags) == hasFlags);
            else
                return ((flags & hasFlags) != ThreadFlags.None);
        }

        ///////////////////////////////////////////////////////////////////////

        public static bool HasFlags(
            TimeoutFlags flags,
            TimeoutFlags hasFlags,
            bool all
            )
        {
            if (all)
                return ((flags & hasFlags) == hasFlags);
            else
                return ((flags & hasFlags) != TimeoutFlags.None);
        }

        ///////////////////////////////////////////////////////////////////////

        public static bool HasFlags(
            TimeoutType flags,
            TimeoutType hasFlags,
            bool all
            )
        {
            if (all)
                return ((flags & hasFlags) == hasFlags);
            else
                return ((flags & hasFlags) != TimeoutType.None);
        }

        ///////////////////////////////////////////////////////////////////////

        public static bool HasFlags(
            TokenFlags flags,
            TokenFlags hasFlags,
            bool all
            )
        {
            if (all)
                return ((flags & hasFlags) == hasFlags);
            else
                return ((flags & hasFlags) != TokenFlags.None);
        }

        ///////////////////////////////////////////////////////////////////////

        public static bool HasFlags(
            ToStringFlags flags,
            ToStringFlags hasFlags,
            bool all
            )
        {
            if (all)
                return ((flags & hasFlags) == hasFlags);
            else
                return ((flags & hasFlags) != ToStringFlags.None);
        }

        ///////////////////////////////////////////////////////////////////////

        public static bool HasFlags(
            TraceCategoryType flags,
            TraceCategoryType hasFlags,
            bool all
            )
        {
            if (all)
                return ((flags & hasFlags) == hasFlags);
            else
                return ((flags & hasFlags) != TraceCategoryType.None);
        }

        ///////////////////////////////////////////////////////////////////////

        public static bool HasFlags(
            TraceFlags flags,
            TraceFlags hasFlags,
            bool all
            )
        {
            if (all)
                return ((flags & hasFlags) == hasFlags);
            else
                return ((flags & hasFlags) != TraceFlags.None);
        }

        ///////////////////////////////////////////////////////////////////////

        public static bool HasFlags(
            TracePriority flags,
            TracePriority hasFlags,
            bool all
            )
        {
            if (all)
                return ((flags & hasFlags) == hasFlags);
            else
                return ((flags & hasFlags) != TracePriority.None);
        }

        ///////////////////////////////////////////////////////////////////////

        public static bool HasFlags(
            TraceStateType flags,
            TraceStateType hasFlags,
            bool all
            )
        {
            if (all)
                return ((flags & hasFlags) == hasFlags);
            else
                return ((flags & hasFlags) != TraceStateType.None);
        }

        ///////////////////////////////////////////////////////////////////////

        public static bool HasFlags(
            TrustFlags flags,
            TrustFlags hasFlags,
            bool all
            )
        {
            if (all)
                return ((flags & hasFlags) == hasFlags);
            else
                return ((flags & hasFlags) != TrustFlags.None);
        }

        ///////////////////////////////////////////////////////////////////////

        public static bool HasFlags(
            TypeListFlags flags,
            TypeListFlags hasFlags,
            bool all
            )
        {
            if (all)
                return ((flags & hasFlags) == hasFlags);
            else
                return ((flags & hasFlags) != TypeListFlags.None);
        }

        ///////////////////////////////////////////////////////////////////////

#if NATIVE && TCL
        public static bool HasFlags(
            UnloadFlags flags,
            UnloadFlags hasFlags,
            bool all
            )
        {
            if (all)
                return ((flags & hasFlags) == hasFlags);
            else
                return ((flags & hasFlags) != UnloadFlags.None);
        }
#endif

        ///////////////////////////////////////////////////////////////////////

        public static bool HasFlags(
            UpdateFlags flags,
            UpdateFlags hasFlags,
            bool all
            )
        {
            if (all)
                return ((flags & hasFlags) == hasFlags);
            else
                return ((flags & hasFlags) != UpdateFlags.None);
        }

        ///////////////////////////////////////////////////////////////////////

        public static bool HasFlags(
            UriComponents flags,
            UriComponents hasFlags,
            bool all
            )
        {
            if (all)
                return ((flags & hasFlags) == hasFlags);
            else
                return ((flags & hasFlags) != (UriComponents)0);
        }

        ///////////////////////////////////////////////////////////////////////

        public static bool HasFlags(
            UriFlags flags,
            UriFlags hasFlags,
            bool all
            )
        {
            if (all)
                return ((flags & hasFlags) == hasFlags);
            else
                return ((flags & hasFlags) != UriFlags.None);
        }

        ///////////////////////////////////////////////////////////////////////

        public static bool HasFlags(
            ValueFlags flags,
            ValueFlags hasFlags,
            bool all
            )
        {
            if (all)
                return ((flags & hasFlags) == hasFlags);
            else
                return ((flags & hasFlags) != ValueFlags.None);
        }

        ///////////////////////////////////////////////////////////////////////

        public static bool HasFlags(
            VariableFlags flags,
            VariableFlags hasFlags,
            bool all
            )
        {
            if (all)
                return ((flags & hasFlags) == hasFlags);
            else
                return ((flags & hasFlags) != VariableFlags.None);
        }

        ///////////////////////////////////////////////////////////////////////

        public static bool HasFlags(
            VerifyFlags flags,
            VerifyFlags hasFlags,
            bool all
            )
        {
            if (all)
                return ((flags & hasFlags) == hasFlags);
            else
                return ((flags & hasFlags) != VerifyFlags.None);
        }

        ///////////////////////////////////////////////////////////////////////

        public static bool HasFlags(
            VersionFlags flags,
            VersionFlags hasFlags,
            bool all
            )
        {
            if (all)
                return ((flags & hasFlags) == hasFlags);
            else
                return ((flags & hasFlags) != VersionFlags.None);
        }

        ///////////////////////////////////////////////////////////////////////

        public static bool HasFlags(
            WatchdogOperation flags,
            WatchdogOperation hasFlags,
            bool all
            )
        {
            if (all)
                return ((flags & hasFlags) == hasFlags);
            else
                return ((flags & hasFlags) != WatchdogOperation.None);
        }

        ///////////////////////////////////////////////////////////////////////

#if NETWORK
        public static bool HasFlags(
            WebFlags flags,
            WebFlags hasFlags,
            bool all
            )
        {
            if (all)
                return ((flags & hasFlags) == hasFlags);
            else
                return ((flags & hasFlags) != WebFlags.None);
        }
#endif

        ///////////////////////////////////////////////////////////////////////

        public static bool HasFlags(
            WhiteSpaceFlags flags,
            WhiteSpaceFlags hasFlags,
            bool all
            )
        {
            if (all)
                return ((flags & hasFlags) == hasFlags);
            else
                return ((flags & hasFlags) != WhiteSpaceFlags.None);
        }

        ///////////////////////////////////////////////////////////////////////

#if XML
        public static bool HasFlags(
            XmlErrorTypes flags,
            XmlErrorTypes hasFlags,
            bool all
            )
        {
            if (all)
                return ((flags & hasFlags) == hasFlags);
            else
                return ((flags & hasFlags) != XmlErrorTypes.None);
        }
#endif
    }
}
