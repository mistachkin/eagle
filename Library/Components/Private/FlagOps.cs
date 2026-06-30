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
    /// <summary>
    /// This class provides static helper methods for determining whether a
    /// particular set of bit flags is present within an enumerated flags
    /// value.  It contains one overload for each flags enumeration used
    /// throughout the Eagle core library.
    /// </summary>
    [ObjectId("c3397500-b84c-4b5e-a0cf-ea4dd6042d6b")]
    internal static class FlagOps
    {
        /// <summary>
        /// This method determines whether the specified <c>ulong</c> value
        /// contains a particular set of flags.
        /// </summary>
        /// <param name="flags">
        /// The flags value to be examined.
        /// </param>
        /// <param name="hasFlags">
        /// The flags to look for within the <paramref name="flags" /> value.
        /// </param>
        /// <param name="all">
        /// Non-zero if every flag in <paramref name="hasFlags" /> must be
        /// present; otherwise, only one of them needs to be present.
        /// </param>
        /// <returns>
        /// True if the requested flags are present; otherwise, false.
        /// </returns>
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

        /// <summary>
        /// This method determines whether the specified <see cref="AliasFlags" /> value
        /// contains a particular set of flags.
        /// </summary>
        /// <param name="flags">
        /// The flags value to be examined.
        /// </param>
        /// <param name="hasFlags">
        /// The flags to look for within the <paramref name="flags" /> value.
        /// </param>
        /// <param name="all">
        /// Non-zero if every flag in <paramref name="hasFlags" /> must be
        /// present; otherwise, only one of them needs to be present.
        /// </param>
        /// <returns>
        /// True if the requested flags are present; otherwise, false.
        /// </returns>
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

        /// <summary>
        /// This method determines whether the specified <see cref="ArgumentFlags" /> value
        /// contains a particular set of flags.
        /// </summary>
        /// <param name="flags">
        /// The flags value to be examined.
        /// </param>
        /// <param name="hasFlags">
        /// The flags to look for within the <paramref name="flags" /> value.
        /// </param>
        /// <param name="all">
        /// Non-zero if every flag in <paramref name="hasFlags" /> must be
        /// present; otherwise, only one of them needs to be present.
        /// </param>
        /// <returns>
        /// True if the requested flags are present; otherwise, false.
        /// </returns>
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

        /// <summary>
        /// This method determines whether the specified <see cref="AutomationFlags" /> value
        /// contains a particular set of flags.
        /// </summary>
        /// <param name="flags">
        /// The flags value to be examined.
        /// </param>
        /// <param name="hasFlags">
        /// The flags to look for within the <paramref name="flags" /> value.
        /// </param>
        /// <param name="all">
        /// Non-zero if every flag in <paramref name="hasFlags" /> must be
        /// present; otherwise, only one of them needs to be present.
        /// </param>
        /// <returns>
        /// True if the requested flags are present; otherwise, false.
        /// </returns>
        public static bool HasFlags(
            AutomationFlags flags,
            AutomationFlags hasFlags,
            bool all
            )
        {
            if (all)
                return ((flags & hasFlags) == hasFlags);
            else
                return ((flags & hasFlags) != AutomationFlags.None);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method determines whether the specified <see cref="Base26FormattingOption" /> value
        /// contains a particular set of flags.
        /// </summary>
        /// <param name="flags">
        /// The flags value to be examined.
        /// </param>
        /// <param name="hasFlags">
        /// The flags to look for within the <paramref name="flags" /> value.
        /// </param>
        /// <param name="all">
        /// Non-zero if every flag in <paramref name="hasFlags" /> must be
        /// present; otherwise, only one of them needs to be present.
        /// </param>
        /// <returns>
        /// True if the requested flags are present; otherwise, false.
        /// </returns>
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
        /// <summary>
        /// This method determines whether the specified <see cref="BufferedTraceFlags" /> value
        /// contains a particular set of flags.
        /// </summary>
        /// <param name="flags">
        /// The flags value to be examined.
        /// </param>
        /// <param name="hasFlags">
        /// The flags to look for within the <paramref name="flags" /> value.
        /// </param>
        /// <param name="all">
        /// Non-zero if every flag in <paramref name="hasFlags" /> must be
        /// present; otherwise, only one of them needs to be present.
        /// </param>
        /// <returns>
        /// True if the requested flags are present; otherwise, false.
        /// </returns>
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

#if DATA
        /// <summary>
        /// This method determines whether the specified <see cref="BundleFlags" /> value
        /// contains a particular set of flags.
        /// </summary>
        /// <param name="flags">
        /// The flags value to be examined.
        /// </param>
        /// <param name="hasFlags">
        /// The flags to look for within the <paramref name="flags" /> value.
        /// </param>
        /// <param name="all">
        /// Non-zero if every flag in <paramref name="hasFlags" /> must be
        /// present; otherwise, only one of them needs to be present.
        /// </param>
        /// <returns>
        /// True if the requested flags are present; otherwise, false.
        /// </returns>
        public static bool HasFlags(
            BundleFlags flags,
            BundleFlags hasFlags,
            bool all
            )
        {
            if (all)
                return ((flags & hasFlags) == hasFlags);
            else
                return ((flags & hasFlags) != BundleFlags.None);
        }
#endif

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method determines whether the specified <see cref="ByRefArgumentFlags" /> value
        /// contains a particular set of flags.
        /// </summary>
        /// <param name="flags">
        /// The flags value to be examined.
        /// </param>
        /// <param name="hasFlags">
        /// The flags to look for within the <paramref name="flags" /> value.
        /// </param>
        /// <param name="all">
        /// Non-zero if every flag in <paramref name="hasFlags" /> must be
        /// present; otherwise, only one of them needs to be present.
        /// </param>
        /// <returns>
        /// True if the requested flags are present; otherwise, false.
        /// </returns>
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

        /// <summary>
        /// This method determines whether the specified <see cref="BindingFlags" /> value
        /// contains a particular set of flags.
        /// </summary>
        /// <param name="flags">
        /// The flags value to be examined.
        /// </param>
        /// <param name="hasFlags">
        /// The flags to look for within the <paramref name="flags" /> value.
        /// </param>
        /// <param name="all">
        /// Non-zero if every flag in <paramref name="hasFlags" /> must be
        /// present; otherwise, only one of them needs to be present.
        /// </param>
        /// <returns>
        /// True if the requested flags are present; otherwise, false.
        /// </returns>
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

        /// <summary>
        /// This method determines whether the specified <see cref="BreakpointType" /> value
        /// contains a particular set of flags.
        /// </summary>
        /// <param name="flags">
        /// The flags value to be examined.
        /// </param>
        /// <param name="hasFlags">
        /// The flags to look for within the <paramref name="flags" /> value.
        /// </param>
        /// <param name="all">
        /// Non-zero if every flag in <paramref name="hasFlags" /> must be
        /// present; otherwise, only one of them needs to be present.
        /// </param>
        /// <returns>
        /// True if the requested flags are present; otherwise, false.
        /// </returns>
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
        /// <summary>
        /// This method determines whether the specified <see cref="CacheInformationFlags" /> value
        /// contains a particular set of flags.
        /// </summary>
        /// <param name="flags">
        /// The flags value to be examined.
        /// </param>
        /// <param name="hasFlags">
        /// The flags to look for within the <paramref name="flags" /> value.
        /// </param>
        /// <param name="all">
        /// Non-zero if every flag in <paramref name="hasFlags" /> must be
        /// present; otherwise, only one of them needs to be present.
        /// </param>
        /// <returns>
        /// True if the requested flags are present; otherwise, false.
        /// </returns>
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
        /// <summary>
        /// This method determines whether the specified <see cref="CacheFlags" /> value
        /// contains a particular set of flags.
        /// </summary>
        /// <param name="flags">
        /// The flags value to be examined.
        /// </param>
        /// <param name="hasFlags">
        /// The flags to look for within the <paramref name="flags" /> value.
        /// </param>
        /// <param name="all">
        /// Non-zero if every flag in <paramref name="hasFlags" /> must be
        /// present; otherwise, only one of them needs to be present.
        /// </param>
        /// <returns>
        /// True if the requested flags are present; otherwise, false.
        /// </returns>
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

        /// <summary>
        /// This method determines whether the specified <see cref="CallFrameFlags" /> value
        /// contains a particular set of flags.
        /// </summary>
        /// <param name="flags">
        /// The flags value to be examined.
        /// </param>
        /// <param name="hasFlags">
        /// The flags to look for within the <paramref name="flags" /> value.
        /// </param>
        /// <param name="all">
        /// Non-zero if every flag in <paramref name="hasFlags" /> must be
        /// present; otherwise, only one of them needs to be present.
        /// </param>
        /// <returns>
        /// True if the requested flags are present; otherwise, false.
        /// </returns>
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

        /// <summary>
        /// This method determines whether the specified <see cref="CallbackFlags" /> value
        /// contains a particular set of flags.
        /// </summary>
        /// <param name="flags">
        /// The flags value to be examined.
        /// </param>
        /// <param name="hasFlags">
        /// The flags to look for within the <paramref name="flags" /> value.
        /// </param>
        /// <param name="all">
        /// Non-zero if every flag in <paramref name="hasFlags" /> must be
        /// present; otherwise, only one of them needs to be present.
        /// </param>
        /// <returns>
        /// True if the requested flags are present; otherwise, false.
        /// </returns>
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

        /// <summary>
        /// This method determines whether the specified <see cref="CancelFlags" /> value
        /// contains a particular set of flags.
        /// </summary>
        /// <param name="flags">
        /// The flags value to be examined.
        /// </param>
        /// <param name="hasFlags">
        /// The flags to look for within the <paramref name="flags" /> value.
        /// </param>
        /// <param name="all">
        /// Non-zero if every flag in <paramref name="hasFlags" /> must be
        /// present; otherwise, only one of them needs to be present.
        /// </param>
        /// <returns>
        /// True if the requested flags are present; otherwise, false.
        /// </returns>
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

        /// <summary>
        /// This method determines whether the specified <see cref="ChannelType" /> value
        /// contains a particular set of flags.
        /// </summary>
        /// <param name="flags">
        /// The flags value to be examined.
        /// </param>
        /// <param name="hasFlags">
        /// The flags to look for within the <paramref name="flags" /> value.
        /// </param>
        /// <param name="all">
        /// Non-zero if every flag in <paramref name="hasFlags" /> must be
        /// present; otherwise, only one of them needs to be present.
        /// </param>
        /// <returns>
        /// True if the requested flags are present; otherwise, false.
        /// </returns>
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

#if THREADING
        /// <summary>
        /// This method determines whether the specified <see cref="CheckStatus" /> value
        /// contains a particular set of flags.
        /// </summary>
        /// <param name="flags">
        /// The flags value to be examined.
        /// </param>
        /// <param name="hasFlags">
        /// The flags to look for within the <paramref name="flags" /> value.
        /// </param>
        /// <param name="all">
        /// Non-zero if every flag in <paramref name="hasFlags" /> must be
        /// present; otherwise, only one of them needs to be present.
        /// </param>
        /// <returns>
        /// True if the requested flags are present; otherwise, false.
        /// </returns>
        public static bool HasFlags(
            CheckStatus flags,
            CheckStatus hasFlags,
            bool all
            )
        {
            if (all)
                return ((flags & hasFlags) == hasFlags);
            else
                return ((flags & hasFlags) != CheckStatus.None);
        }
#endif

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method determines whether the specified <see cref="CloneFlags" /> value
        /// contains a particular set of flags.
        /// </summary>
        /// <param name="flags">
        /// The flags value to be examined.
        /// </param>
        /// <param name="hasFlags">
        /// The flags to look for within the <paramref name="flags" /> value.
        /// </param>
        /// <param name="all">
        /// Non-zero if every flag in <paramref name="hasFlags" /> must be
        /// present; otherwise, only one of them needs to be present.
        /// </param>
        /// <returns>
        /// True if the requested flags are present; otherwise, false.
        /// </returns>
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

        /// <summary>
        /// This method determines whether the specified <see cref="CommandFlags" /> value
        /// contains a particular set of flags.
        /// </summary>
        /// <param name="flags">
        /// The flags value to be examined.
        /// </param>
        /// <param name="hasFlags">
        /// The flags to look for within the <paramref name="flags" /> value.
        /// </param>
        /// <param name="all">
        /// Non-zero if every flag in <paramref name="hasFlags" /> must be
        /// present; otherwise, only one of them needs to be present.
        /// </param>
        /// <returns>
        /// True if the requested flags are present; otherwise, false.
        /// </returns>
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

        /// <summary>
        /// This method determines whether the specified <see cref="CommandCountType" /> value
        /// contains a particular set of flags.
        /// </summary>
        /// <param name="flags">
        /// The flags value to be examined.
        /// </param>
        /// <param name="hasFlags">
        /// The flags to look for within the <paramref name="flags" /> value.
        /// </param>
        /// <param name="all">
        /// Non-zero if every flag in <paramref name="hasFlags" /> must be
        /// present; otherwise, only one of them needs to be present.
        /// </param>
        /// <returns>
        /// True if the requested flags are present; otherwise, false.
        /// </returns>
        public static bool HasFlags(
            CommandCountType flags,
            CommandCountType hasFlags,
            bool all
            )
        {
            if (all)
                return ((flags & hasFlags) == hasFlags);
            else
                return ((flags & hasFlags) != CommandCountType.None);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method determines whether the specified <see cref="ConfigurationFlags" /> value
        /// contains a particular set of flags.
        /// </summary>
        /// <param name="flags">
        /// The flags value to be examined.
        /// </param>
        /// <param name="hasFlags">
        /// The flags to look for within the <paramref name="flags" /> value.
        /// </param>
        /// <param name="all">
        /// Non-zero if every flag in <paramref name="hasFlags" /> must be
        /// present; otherwise, only one of them needs to be present.
        /// </param>
        /// <returns>
        /// True if the requested flags are present; otherwise, false.
        /// </returns>
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
        /// <summary>
        /// This method determines whether the specified <see cref="ConsoleModifiers" /> value
        /// contains a particular set of flags.
        /// </summary>
        /// <param name="flags">
        /// The flags value to be examined.
        /// </param>
        /// <param name="hasFlags">
        /// The flags to look for within the <paramref name="flags" /> value.
        /// </param>
        /// <param name="all">
        /// Non-zero if every flag in <paramref name="hasFlags" /> must be
        /// present; otherwise, only one of them needs to be present.
        /// </param>
        /// <returns>
        /// True if the requested flags are present; otherwise, false.
        /// </returns>
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

#if NETWORK
        /// <summary>
        /// This method determines whether the specified <see cref="ContextIdType" /> value
        /// contains a particular set of flags.
        /// </summary>
        /// <param name="flags">
        /// The flags value to be examined.
        /// </param>
        /// <param name="hasFlags">
        /// The flags to look for within the <paramref name="flags" /> value.
        /// </param>
        /// <param name="all">
        /// Non-zero if every flag in <paramref name="hasFlags" /> must be
        /// present; otherwise, only one of them needs to be present.
        /// </param>
        /// <returns>
        /// True if the requested flags are present; otherwise, false.
        /// </returns>
        public static bool HasFlags(
            ContextIdType flags,
            ContextIdType hasFlags,
            bool all
            )
        {
            if (all)
                return ((flags & hasFlags) == hasFlags);
            else
                return ((flags & hasFlags) != ContextIdType.None);
        }
#endif

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method determines whether the specified <see cref="CreateFlags" /> value
        /// contains a particular set of flags.
        /// </summary>
        /// <param name="flags">
        /// The flags value to be examined.
        /// </param>
        /// <param name="hasFlags">
        /// The flags to look for within the <paramref name="flags" /> value.
        /// </param>
        /// <param name="all">
        /// Non-zero if every flag in <paramref name="hasFlags" /> must be
        /// present; otherwise, only one of them needs to be present.
        /// </param>
        /// <returns>
        /// True if the requested flags are present; otherwise, false.
        /// </returns>
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

        /// <summary>
        /// This method determines whether the specified <see cref="CreationFlagTypes" /> value
        /// contains a particular set of flags.
        /// </summary>
        /// <param name="flags">
        /// The flags value to be examined.
        /// </param>
        /// <param name="hasFlags">
        /// The flags to look for within the <paramref name="flags" /> value.
        /// </param>
        /// <param name="all">
        /// Non-zero if every flag in <paramref name="hasFlags" /> must be
        /// present; otherwise, only one of them needs to be present.
        /// </param>
        /// <returns>
        /// True if the requested flags are present; otherwise, false.
        /// </returns>
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

        /// <summary>
        /// This method determines whether the specified <see cref="CreateStateFlags" /> value
        /// contains a particular set of flags.
        /// </summary>
        /// <param name="flags">
        /// The flags value to be examined.
        /// </param>
        /// <param name="hasFlags">
        /// The flags to look for within the <paramref name="flags" /> value.
        /// </param>
        /// <param name="all">
        /// Non-zero if every flag in <paramref name="hasFlags" /> must be
        /// present; otherwise, only one of them needs to be present.
        /// </param>
        /// <returns>
        /// True if the requested flags are present; otherwise, false.
        /// </returns>
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

        /// <summary>
        /// This method determines whether the specified <see cref="DataFlags" /> value
        /// contains a particular set of flags.
        /// </summary>
        /// <param name="flags">
        /// The flags value to be examined.
        /// </param>
        /// <param name="hasFlags">
        /// The flags to look for within the <paramref name="flags" /> value.
        /// </param>
        /// <param name="all">
        /// Non-zero if every flag in <paramref name="hasFlags" /> must be
        /// present; otherwise, only one of them needs to be present.
        /// </param>
        /// <returns>
        /// True if the requested flags are present; otherwise, false.
        /// </returns>
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
        /// <summary>
        /// This method determines whether the specified <see cref="DbVariableFlags" /> value
        /// contains a particular set of flags.
        /// </summary>
        /// <param name="flags">
        /// The flags value to be examined.
        /// </param>
        /// <param name="hasFlags">
        /// The flags to look for within the <paramref name="flags" /> value.
        /// </param>
        /// <param name="all">
        /// Non-zero if every flag in <paramref name="hasFlags" /> must be
        /// present; otherwise, only one of them needs to be present.
        /// </param>
        /// <returns>
        /// True if the requested flags are present; otherwise, false.
        /// </returns>
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

        /// <summary>
        /// This method determines whether the specified <see cref="DebugEmergencyLevel" /> value
        /// contains a particular set of flags.
        /// </summary>
        /// <param name="flags">
        /// The flags value to be examined.
        /// </param>
        /// <param name="hasFlags">
        /// The flags to look for within the <paramref name="flags" /> value.
        /// </param>
        /// <param name="all">
        /// Non-zero if every flag in <paramref name="hasFlags" /> must be
        /// present; otherwise, only one of them needs to be present.
        /// </param>
        /// <returns>
        /// True if the requested flags are present; otherwise, false.
        /// </returns>
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

        /// <summary>
        /// This method determines whether the specified <see cref="DebugPathFlags" /> value
        /// contains a particular set of flags.
        /// </summary>
        /// <param name="flags">
        /// The flags value to be examined.
        /// </param>
        /// <param name="hasFlags">
        /// The flags to look for within the <paramref name="flags" /> value.
        /// </param>
        /// <param name="all">
        /// Non-zero if every flag in <paramref name="hasFlags" /> must be
        /// present; otherwise, only one of them needs to be present.
        /// </param>
        /// <returns>
        /// True if the requested flags are present; otherwise, false.
        /// </returns>
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

        /// <summary>
        /// This method determines whether the specified <see cref="DebugPriority" /> value
        /// contains a particular set of flags.
        /// </summary>
        /// <param name="flags">
        /// The flags value to be examined.
        /// </param>
        /// <param name="hasFlags">
        /// The flags to look for within the <paramref name="flags" /> value.
        /// </param>
        /// <param name="all">
        /// Non-zero if every flag in <paramref name="hasFlags" /> must be
        /// present; otherwise, only one of them needs to be present.
        /// </param>
        /// <returns>
        /// True if the requested flags are present; otherwise, false.
        /// </returns>
        public static bool HasFlags(
            DebugPriority flags,
            DebugPriority hasFlags,
            bool all
            )
        {
            if (all)
                return ((flags & hasFlags) == hasFlags);
            else
                return ((flags & hasFlags) != DebugPriority.None);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method determines whether the specified <see cref="DetailFlags" /> value
        /// contains a particular set of flags.
        /// </summary>
        /// <param name="flags">
        /// The flags value to be examined.
        /// </param>
        /// <param name="hasFlags">
        /// The flags to look for within the <paramref name="flags" /> value.
        /// </param>
        /// <param name="all">
        /// Non-zero if every flag in <paramref name="hasFlags" /> must be
        /// present; otherwise, only one of them needs to be present.
        /// </param>
        /// <returns>
        /// True if the requested flags are present; otherwise, false.
        /// </returns>
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

        /// <summary>
        /// This method determines whether the specified <see cref="DelegateFlags" /> value
        /// contains a particular set of flags.
        /// </summary>
        /// <param name="flags">
        /// The flags value to be examined.
        /// </param>
        /// <param name="hasFlags">
        /// The flags to look for within the <paramref name="flags" /> value.
        /// </param>
        /// <param name="all">
        /// Non-zero if every flag in <paramref name="hasFlags" /> must be
        /// present; otherwise, only one of them needs to be present.
        /// </param>
        /// <returns>
        /// True if the requested flags are present; otherwise, false.
        /// </returns>
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

        /// <summary>
        /// This method determines whether the specified <see cref="DetectFlags" /> value
        /// contains a particular set of flags.
        /// </summary>
        /// <param name="flags">
        /// The flags value to be examined.
        /// </param>
        /// <param name="hasFlags">
        /// The flags to look for within the <paramref name="flags" /> value.
        /// </param>
        /// <param name="all">
        /// Non-zero if every flag in <paramref name="hasFlags" /> must be
        /// present; otherwise, only one of them needs to be present.
        /// </param>
        /// <returns>
        /// True if the requested flags are present; otherwise, false.
        /// </returns>
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

        /// <summary>
        /// This method determines whether the specified <see cref="DisableFlags" /> value
        /// contains a particular set of flags.
        /// </summary>
        /// <param name="flags">
        /// The flags value to be examined.
        /// </param>
        /// <param name="hasFlags">
        /// The flags to look for within the <paramref name="flags" /> value.
        /// </param>
        /// <param name="all">
        /// Non-zero if every flag in <paramref name="hasFlags" /> must be
        /// present; otherwise, only one of them needs to be present.
        /// </param>
        /// <returns>
        /// True if the requested flags are present; otherwise, false.
        /// </returns>
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

        /// <summary>
        /// This method determines whether the specified <see cref="DisposalPhase" /> value
        /// contains a particular set of flags.
        /// </summary>
        /// <param name="flags">
        /// The flags value to be examined.
        /// </param>
        /// <param name="hasFlags">
        /// The flags to look for within the <paramref name="flags" /> value.
        /// </param>
        /// <param name="all">
        /// Non-zero if every flag in <paramref name="hasFlags" /> must be
        /// present; otherwise, only one of them needs to be present.
        /// </param>
        /// <returns>
        /// True if the requested flags are present; otherwise, false.
        /// </returns>
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

        /// <summary>
        /// This method determines whether the specified <see cref="DurationFlags" /> value
        /// contains a particular set of flags.
        /// </summary>
        /// <param name="flags">
        /// The flags value to be examined.
        /// </param>
        /// <param name="hasFlags">
        /// The flags to look for within the <paramref name="flags" /> value.
        /// </param>
        /// <param name="all">
        /// Non-zero if every flag in <paramref name="hasFlags" /> must be
        /// present; otherwise, only one of them needs to be present.
        /// </param>
        /// <returns>
        /// True if the requested flags are present; otherwise, false.
        /// </returns>
        public static bool HasFlags(
            DurationFlags flags,
            DurationFlags hasFlags,
            bool all
            )
        {
            if (all)
                return ((flags & hasFlags) == hasFlags);
            else
                return ((flags & hasFlags) != DurationFlags.None);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method determines whether the specified <see cref="EventFlags" /> value
        /// contains a particular set of flags.
        /// </summary>
        /// <param name="flags">
        /// The flags value to be examined.
        /// </param>
        /// <param name="hasFlags">
        /// The flags to look for within the <paramref name="flags" /> value.
        /// </param>
        /// <param name="all">
        /// Non-zero if every flag in <paramref name="hasFlags" /> must be
        /// present; otherwise, only one of them needs to be present.
        /// </param>
        /// <returns>
        /// True if the requested flags are present; otherwise, false.
        /// </returns>
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

        /// <summary>
        /// This method determines whether the specified <see cref="EventWaitFlags" /> value
        /// contains a particular set of flags.
        /// </summary>
        /// <param name="flags">
        /// The flags value to be examined.
        /// </param>
        /// <param name="hasFlags">
        /// The flags to look for within the <paramref name="flags" /> value.
        /// </param>
        /// <param name="all">
        /// Non-zero if every flag in <paramref name="hasFlags" /> must be
        /// present; otherwise, only one of them needs to be present.
        /// </param>
        /// <returns>
        /// True if the requested flags are present; otherwise, false.
        /// </returns>
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

        /// <summary>
        /// This method determines whether the specified <see cref="ExecutionPolicy" /> value
        /// contains a particular set of flags.
        /// </summary>
        /// <param name="flags">
        /// The flags value to be examined.
        /// </param>
        /// <param name="hasFlags">
        /// The flags to look for within the <paramref name="flags" /> value.
        /// </param>
        /// <param name="all">
        /// Non-zero if every flag in <paramref name="hasFlags" /> must be
        /// present; otherwise, only one of them needs to be present.
        /// </param>
        /// <returns>
        /// True if the requested flags are present; otherwise, false.
        /// </returns>
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

        /// <summary>
        /// This method determines whether the specified <see cref="ExecutionPolicy" /> value
        /// contains a particular set of flags, treating a null value as
        /// <see cref="ExecutionPolicy.None" />.
        /// </summary>
        /// <param name="flags">
        /// The flags value to be examined.  This parameter may be null.
        /// </param>
        /// <param name="hasFlags">
        /// The flags to look for within the <paramref name="flags" /> value.
        /// </param>
        /// <param name="all">
        /// Non-zero if every flag in <paramref name="hasFlags" /> must be
        /// present; otherwise, only one of them needs to be present.
        /// </param>
        /// <returns>
        /// True if the requested flags are present; otherwise, false.
        /// </returns>
        public static bool HasFlags(
            ExecutionPolicy? flags,
            ExecutionPolicy hasFlags,
            bool all
            )
        {
            ExecutionPolicy localFlags = (flags != null) ?
                (ExecutionPolicy)flags : ExecutionPolicy.None;

            return HasFlags(localFlags, hasFlags, all);
        }

        ///////////////////////////////////////////////////////////////////////

#if APPDOMAINS
        /// <summary>
        /// This method determines whether the specified <see cref="FieldAttributes" /> value
        /// contains a particular set of flags.
        /// </summary>
        /// <param name="flags">
        /// The flags value to be examined.
        /// </param>
        /// <param name="hasFlags">
        /// The flags to look for within the <paramref name="flags" /> value.
        /// </param>
        /// <param name="all">
        /// Non-zero if every flag in <paramref name="hasFlags" /> must be
        /// present; otherwise, only one of them needs to be present.
        /// </param>
        /// <returns>
        /// True if the requested flags are present; otherwise, false.
        /// </returns>
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

        /// <summary>
        /// This method determines whether the specified <see cref="FileAttributes" /> value
        /// contains a particular set of flags.
        /// </summary>
        /// <param name="flags">
        /// The flags value to be examined.
        /// </param>
        /// <param name="hasFlags">
        /// The flags to look for within the <paramref name="flags" /> value.
        /// </param>
        /// <param name="all">
        /// Non-zero if every flag in <paramref name="hasFlags" /> must be
        /// present; otherwise, only one of them needs to be present.
        /// </param>
        /// <returns>
        /// True if the requested flags are present; otherwise, false.
        /// </returns>
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
        /// <summary>
        /// This method determines whether the specified <see cref="FileFlagsAndAttributes" /> value
        /// contains a particular set of flags.
        /// </summary>
        /// <param name="flags">
        /// The flags value to be examined.
        /// </param>
        /// <param name="hasFlags">
        /// The flags to look for within the <paramref name="flags" /> value.
        /// </param>
        /// <param name="all">
        /// Non-zero if every flag in <paramref name="hasFlags" /> must be
        /// present; otherwise, only one of them needs to be present.
        /// </param>
        /// <returns>
        /// True if the requested flags are present; otherwise, false.
        /// </returns>
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

        /// <summary>
        /// This method determines whether the specified <see cref="FilePermission" /> value
        /// contains a particular set of flags.
        /// </summary>
        /// <param name="flags">
        /// The flags value to be examined.
        /// </param>
        /// <param name="hasFlags">
        /// The flags to look for within the <paramref name="flags" /> value.
        /// </param>
        /// <param name="all">
        /// Non-zero if every flag in <paramref name="hasFlags" /> must be
        /// present; otherwise, only one of them needs to be present.
        /// </param>
        /// <returns>
        /// True if the requested flags are present; otherwise, false.
        /// </returns>
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

        /// <summary>
        /// This method determines whether the specified <see cref="FileSearchFlags" /> value
        /// contains a particular set of flags.
        /// </summary>
        /// <param name="flags">
        /// The flags value to be examined.
        /// </param>
        /// <param name="hasFlags">
        /// The flags to look for within the <paramref name="flags" /> value.
        /// </param>
        /// <param name="all">
        /// Non-zero if every flag in <paramref name="hasFlags" /> must be
        /// present; otherwise, only one of them needs to be present.
        /// </param>
        /// <returns>
        /// True if the requested flags are present; otherwise, false.
        /// </returns>
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
        /// <summary>
        /// This method determines whether the specified <see cref="FindFlags" /> value
        /// contains a particular set of flags.
        /// </summary>
        /// <param name="flags">
        /// The flags value to be examined.
        /// </param>
        /// <param name="hasFlags">
        /// The flags to look for within the <paramref name="flags" /> value.
        /// </param>
        /// <param name="all">
        /// Non-zero if every flag in <paramref name="hasFlags" /> must be
        /// present; otherwise, only one of them needs to be present.
        /// </param>
        /// <returns>
        /// True if the requested flags are present; otherwise, false.
        /// </returns>
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

#if SHELL && INTERACTIVE_COMMANDS
        /// <summary>
        /// This method determines whether the specified <see cref="TextFlags" /> value
        /// contains a particular set of flags.
        /// </summary>
        /// <param name="flags">
        /// The flags value to be examined.
        /// </param>
        /// <param name="hasFlags">
        /// The flags to look for within the <paramref name="flags" /> value.
        /// </param>
        /// <param name="all">
        /// Non-zero if every flag in <paramref name="hasFlags" /> must be
        /// present; otherwise, only one of them needs to be present.
        /// </param>
        /// <returns>
        /// True if the requested flags are present; otherwise, false.
        /// </returns>
        public static bool HasFlags(
            TextFlags flags,
            TextFlags hasFlags,
            bool all
            )
        {
            if (all)
                return ((flags & hasFlags) == hasFlags);
            else
                return ((flags & hasFlags) != TextFlags.None);
        }
#endif

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method determines whether the specified <see cref="FrameworkFlags" /> value
        /// contains a particular set of flags.
        /// </summary>
        /// <param name="flags">
        /// The flags value to be examined.
        /// </param>
        /// <param name="hasFlags">
        /// The flags to look for within the <paramref name="flags" /> value.
        /// </param>
        /// <param name="all">
        /// Non-zero if every flag in <paramref name="hasFlags" /> must be
        /// present; otherwise, only one of them needs to be present.
        /// </param>
        /// <returns>
        /// True if the requested flags are present; otherwise, false.
        /// </returns>
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

        /// <summary>
        /// This method determines whether the specified <see cref="FunctionFlags" /> value
        /// contains a particular set of flags.
        /// </summary>
        /// <param name="flags">
        /// The flags value to be examined.
        /// </param>
        /// <param name="hasFlags">
        /// The flags to look for within the <paramref name="flags" /> value.
        /// </param>
        /// <param name="all">
        /// Non-zero if every flag in <paramref name="hasFlags" /> must be
        /// present; otherwise, only one of them needs to be present.
        /// </param>
        /// <returns>
        /// True if the requested flags are present; otherwise, false.
        /// </returns>
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

        /// <summary>
        /// This method determines whether the specified <see cref="GarbageFlags" /> value
        /// contains a particular set of flags.
        /// </summary>
        /// <param name="flags">
        /// The flags value to be examined.
        /// </param>
        /// <param name="hasFlags">
        /// The flags to look for within the <paramref name="flags" /> value.
        /// </param>
        /// <param name="all">
        /// Non-zero if every flag in <paramref name="hasFlags" /> must be
        /// present; otherwise, only one of them needs to be present.
        /// </param>
        /// <returns>
        /// True if the requested flags are present; otherwise, false.
        /// </returns>
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

        /// <summary>
        /// This method determines whether the specified <see cref="HeaderFlags" /> value
        /// contains a particular set of flags.
        /// </summary>
        /// <param name="flags">
        /// The flags value to be examined.
        /// </param>
        /// <param name="hasFlags">
        /// The flags to look for within the <paramref name="flags" /> value.
        /// </param>
        /// <param name="all">
        /// Non-zero if every flag in <paramref name="hasFlags" /> must be
        /// present; otherwise, only one of them needs to be present.
        /// </param>
        /// <returns>
        /// True if the requested flags are present; otherwise, false.
        /// </returns>
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
        /// <summary>
        /// This method determines whether the specified <see cref="HistoryFlags" /> value
        /// contains a particular set of flags.
        /// </summary>
        /// <param name="flags">
        /// The flags value to be examined.
        /// </param>
        /// <param name="hasFlags">
        /// The flags to look for within the <paramref name="flags" /> value.
        /// </param>
        /// <param name="all">
        /// Non-zero if every flag in <paramref name="hasFlags" /> must be
        /// present; otherwise, only one of them needs to be present.
        /// </param>
        /// <returns>
        /// True if the requested flags are present; otherwise, false.
        /// </returns>
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

        /// <summary>
        /// This method determines whether the specified <see cref="HomeFlags" /> value
        /// contains a particular set of flags.
        /// </summary>
        /// <param name="flags">
        /// The flags value to be examined.
        /// </param>
        /// <param name="hasFlags">
        /// The flags to look for within the <paramref name="flags" /> value.
        /// </param>
        /// <param name="all">
        /// Non-zero if every flag in <paramref name="hasFlags" /> must be
        /// present; otherwise, only one of them needs to be present.
        /// </param>
        /// <returns>
        /// True if the requested flags are present; otherwise, false.
        /// </returns>
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

        /// <summary>
        /// This method determines whether the specified <see cref="HostFlags" /> value
        /// contains a particular set of flags.
        /// </summary>
        /// <param name="flags">
        /// The flags value to be examined.
        /// </param>
        /// <param name="hasFlags">
        /// The flags to look for within the <paramref name="flags" /> value.
        /// </param>
        /// <param name="all">
        /// Non-zero if every flag in <paramref name="hasFlags" /> must be
        /// present; otherwise, only one of them needs to be present.
        /// </param>
        /// <returns>
        /// True if the requested flags are present; otherwise, false.
        /// </returns>
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

        /// <summary>
        /// This method determines whether the specified <see cref="HostCreateFlags" /> value
        /// contains a particular set of flags.
        /// </summary>
        /// <param name="flags">
        /// The flags value to be examined.
        /// </param>
        /// <param name="hasFlags">
        /// The flags to look for within the <paramref name="flags" /> value.
        /// </param>
        /// <param name="all">
        /// Non-zero if every flag in <paramref name="hasFlags" /> must be
        /// present; otherwise, only one of them needs to be present.
        /// </param>
        /// <returns>
        /// True if the requested flags are present; otherwise, false.
        /// </returns>
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

        /// <summary>
        /// This method determines whether the specified <see cref="HostStreamFlags" /> value
        /// contains a particular set of flags.
        /// </summary>
        /// <param name="flags">
        /// The flags value to be examined.
        /// </param>
        /// <param name="hasFlags">
        /// The flags to look for within the <paramref name="flags" /> value.
        /// </param>
        /// <param name="all">
        /// Non-zero if every flag in <paramref name="hasFlags" /> must be
        /// present; otherwise, only one of them needs to be present.
        /// </param>
        /// <returns>
        /// True if the requested flags are present; otherwise, false.
        /// </returns>
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

        /// <summary>
        /// This method determines whether the specified <see cref="HostTestFlags" /> value
        /// contains a particular set of flags.
        /// </summary>
        /// <param name="flags">
        /// The flags value to be examined.
        /// </param>
        /// <param name="hasFlags">
        /// The flags to look for within the <paramref name="flags" /> value.
        /// </param>
        /// <param name="all">
        /// Non-zero if every flag in <paramref name="hasFlags" /> must be
        /// present; otherwise, only one of them needs to be present.
        /// </param>
        /// <returns>
        /// True if the requested flags are present; otherwise, false.
        /// </returns>
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

        /// <summary>
        /// This method determines whether the specified <see cref="IdentifierKind" /> value
        /// contains a particular set of flags.
        /// </summary>
        /// <param name="flags">
        /// The flags value to be examined.
        /// </param>
        /// <param name="hasFlags">
        /// The flags to look for within the <paramref name="flags" /> value.
        /// </param>
        /// <param name="all">
        /// Non-zero if every flag in <paramref name="hasFlags" /> must be
        /// present; otherwise, only one of them needs to be present.
        /// </param>
        /// <returns>
        /// True if the requested flags are present; otherwise, false.
        /// </returns>
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

        /// <summary>
        /// This method determines whether the specified <see cref="InfoPathType" /> value
        /// contains a particular set of flags.
        /// </summary>
        /// <param name="flags">
        /// The flags value to be examined.
        /// </param>
        /// <param name="hasFlags">
        /// The flags to look for within the <paramref name="flags" /> value.
        /// </param>
        /// <param name="all">
        /// Non-zero if every flag in <paramref name="hasFlags" /> must be
        /// present; otherwise, only one of them needs to be present.
        /// </param>
        /// <returns>
        /// True if the requested flags are present; otherwise, false.
        /// </returns>
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

        /// <summary>
        /// This method determines whether the specified <see cref="InitializeFlags" /> value
        /// contains a particular set of flags.
        /// </summary>
        /// <param name="flags">
        /// The flags value to be examined.
        /// </param>
        /// <param name="hasFlags">
        /// The flags to look for within the <paramref name="flags" /> value.
        /// </param>
        /// <param name="all">
        /// Non-zero if every flag in <paramref name="hasFlags" /> must be
        /// present; otherwise, only one of them needs to be present.
        /// </param>
        /// <returns>
        /// True if the requested flags are present; otherwise, false.
        /// </returns>
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
        /// <summary>
        /// This method determines whether the specified <see cref="InteractiveLoopFlags" /> value
        /// contains a particular set of flags.
        /// </summary>
        /// <param name="flags">
        /// The flags value to be examined.
        /// </param>
        /// <param name="hasFlags">
        /// The flags to look for within the <paramref name="flags" /> value.
        /// </param>
        /// <param name="all">
        /// Non-zero if every flag in <paramref name="hasFlags" /> must be
        /// present; otherwise, only one of them needs to be present.
        /// </param>
        /// <returns>
        /// True if the requested flags are present; otherwise, false.
        /// </returns>
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

        /// <summary>
        /// This method determines whether the specified <see cref="InterpreterFlags" /> value
        /// contains a particular set of flags.
        /// </summary>
        /// <param name="flags">
        /// The flags value to be examined.
        /// </param>
        /// <param name="hasFlags">
        /// The flags to look for within the <paramref name="flags" /> value.
        /// </param>
        /// <param name="all">
        /// Non-zero if every flag in <paramref name="hasFlags" /> must be
        /// present; otherwise, only one of them needs to be present.
        /// </param>
        /// <returns>
        /// True if the requested flags are present; otherwise, false.
        /// </returns>
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

        /// <summary>
        /// This method determines whether the specified <see cref="InterpreterStateFlags" /> value
        /// contains a particular set of flags.
        /// </summary>
        /// <param name="flags">
        /// The flags value to be examined.
        /// </param>
        /// <param name="hasFlags">
        /// The flags to look for within the <paramref name="flags" /> value.
        /// </param>
        /// <param name="all">
        /// Non-zero if every flag in <paramref name="hasFlags" /> must be
        /// present; otherwise, only one of them needs to be present.
        /// </param>
        /// <returns>
        /// True if the requested flags are present; otherwise, false.
        /// </returns>
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

        /// <summary>
        /// This method determines whether the specified <see cref="InterpreterTestFlags" /> value
        /// contains a particular set of flags.
        /// </summary>
        /// <param name="flags">
        /// The flags value to be examined.
        /// </param>
        /// <param name="hasFlags">
        /// The flags to look for within the <paramref name="flags" /> value.
        /// </param>
        /// <param name="all">
        /// Non-zero if every flag in <paramref name="hasFlags" /> must be
        /// present; otherwise, only one of them needs to be present.
        /// </param>
        /// <returns>
        /// True if the requested flags are present; otherwise, false.
        /// </returns>
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

        /// <summary>
        /// This method determines whether the specified <see cref="InterpreterType" /> value
        /// contains a particular set of flags.
        /// </summary>
        /// <param name="flags">
        /// The flags value to be examined.
        /// </param>
        /// <param name="hasFlags">
        /// The flags to look for within the <paramref name="flags" /> value.
        /// </param>
        /// <param name="all">
        /// Non-zero if every flag in <paramref name="hasFlags" /> must be
        /// present; otherwise, only one of them needs to be present.
        /// </param>
        /// <returns>
        /// True if the requested flags are present; otherwise, false.
        /// </returns>
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

#if NETWORK
        /// <summary>
        /// This method determines whether the specified <see cref="IpFlags" /> value
        /// contains a particular set of flags.
        /// </summary>
        /// <param name="flags">
        /// The flags value to be examined.
        /// </param>
        /// <param name="hasFlags">
        /// The flags to look for within the <paramref name="flags" /> value.
        /// </param>
        /// <param name="all">
        /// Non-zero if every flag in <paramref name="hasFlags" /> must be
        /// present; otherwise, only one of them needs to be present.
        /// </param>
        /// <returns>
        /// True if the requested flags are present; otherwise, false.
        /// </returns>
        public static bool HasFlags(
            IpFlags flags,
            IpFlags hasFlags,
            bool all
            )
        {
            if (all)
                return ((flags & hasFlags) == hasFlags);
            else
                return ((flags & hasFlags) != IpFlags.None);
        }
#endif

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method determines whether the specified <see cref="IsolationDetail" /> value
        /// contains a particular set of flags.
        /// </summary>
        /// <param name="flags">
        /// The flags value to be examined.
        /// </param>
        /// <param name="hasFlags">
        /// The flags to look for within the <paramref name="flags" /> value.
        /// </param>
        /// <param name="all">
        /// Non-zero if every flag in <paramref name="hasFlags" /> must be
        /// present; otherwise, only one of them needs to be present.
        /// </param>
        /// <returns>
        /// True if the requested flags are present; otherwise, false.
        /// </returns>
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

        /// <summary>
        /// This method determines whether the specified <see cref="IsolationLevel" /> value
        /// contains a particular set of flags.
        /// </summary>
        /// <param name="flags">
        /// The flags value to be examined.
        /// </param>
        /// <param name="hasFlags">
        /// The flags to look for within the <paramref name="flags" /> value.
        /// </param>
        /// <param name="all">
        /// Non-zero if every flag in <paramref name="hasFlags" /> must be
        /// present; otherwise, only one of them needs to be present.
        /// </param>
        /// <returns>
        /// True if the requested flags are present; otherwise, false.
        /// </returns>
        public static bool HasFlags(
            IsolationLevel flags,
            IsolationLevel hasFlags,
            bool all
            )
        {
            if (all)
                return ((flags & hasFlags) == hasFlags);
            else
                return ((flags & hasFlags) != IsolationLevel.None);
        }

        ///////////////////////////////////////////////////////////////////////

#if SHELL
        /// <summary>
        /// This method determines whether the specified <see cref="KioskFlags" /> value
        /// contains a particular set of flags.
        /// </summary>
        /// <param name="flags">
        /// The flags value to be examined.
        /// </param>
        /// <param name="hasFlags">
        /// The flags to look for within the <paramref name="flags" /> value.
        /// </param>
        /// <param name="all">
        /// Non-zero if every flag in <paramref name="hasFlags" /> must be
        /// present; otherwise, only one of them needs to be present.
        /// </param>
        /// <returns>
        /// True if the requested flags are present; otherwise, false.
        /// </returns>
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

        /// <summary>
        /// This method determines whether the specified <see cref="LevelFlags" /> value
        /// contains a particular set of flags.
        /// </summary>
        /// <param name="flags">
        /// The flags value to be examined.
        /// </param>
        /// <param name="hasFlags">
        /// The flags to look for within the <paramref name="flags" /> value.
        /// </param>
        /// <param name="all">
        /// Non-zero if every flag in <paramref name="hasFlags" /> must be
        /// present; otherwise, only one of them needs to be present.
        /// </param>
        /// <returns>
        /// True if the requested flags are present; otherwise, false.
        /// </returns>
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

        /// <summary>
        /// This method determines whether the specified <see cref="ListElementFlags" /> value
        /// contains a particular set of flags.
        /// </summary>
        /// <param name="flags">
        /// The flags value to be examined.
        /// </param>
        /// <param name="hasFlags">
        /// The flags to look for within the <paramref name="flags" /> value.
        /// </param>
        /// <param name="all">
        /// Non-zero if every flag in <paramref name="hasFlags" /> must be
        /// present; otherwise, only one of them needs to be present.
        /// </param>
        /// <returns>
        /// True if the requested flags are present; otherwise, false.
        /// </returns>
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
        /// <summary>
        /// This method determines whether the specified <see cref="LoadFlags" /> value
        /// contains a particular set of flags.
        /// </summary>
        /// <param name="flags">
        /// The flags value to be examined.
        /// </param>
        /// <param name="hasFlags">
        /// The flags to look for within the <paramref name="flags" /> value.
        /// </param>
        /// <param name="all">
        /// Non-zero if every flag in <paramref name="hasFlags" /> must be
        /// present; otherwise, only one of them needs to be present.
        /// </param>
        /// <returns>
        /// True if the requested flags are present; otherwise, false.
        /// </returns>
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

        /// <summary>
        /// This method determines whether the specified <see cref="LogFlags" /> value
        /// contains a particular set of flags.
        /// </summary>
        /// <param name="flags">
        /// The flags value to be examined.
        /// </param>
        /// <param name="hasFlags">
        /// The flags to look for within the <paramref name="flags" /> value.
        /// </param>
        /// <param name="all">
        /// Non-zero if every flag in <paramref name="hasFlags" /> must be
        /// present; otherwise, only one of them needs to be present.
        /// </param>
        /// <returns>
        /// True if the requested flags are present; otherwise, false.
        /// </returns>
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

        /// <summary>
        /// This method determines whether the specified <see cref="LogFlags" /> value
        /// contains a particular set of flags, treating a null value as
        /// <see cref="LogFlags.None" />.
        /// </summary>
        /// <param name="flags">
        /// The flags value to be examined.  This parameter may be null.
        /// </param>
        /// <param name="hasFlags">
        /// The flags to look for within the <paramref name="flags" /> value.
        /// </param>
        /// <param name="all">
        /// Non-zero if every flag in <paramref name="hasFlags" /> must be
        /// present; otherwise, only one of them needs to be present.
        /// </param>
        /// <returns>
        /// True if the requested flags are present; otherwise, false.
        /// </returns>
        public static bool HasFlags(
            LogFlags? flags,
            LogFlags hasFlags,
            bool all
            )
        {
            LogFlags localFlags = (flags != null) ?
                (LogFlags)flags : LogFlags.None;

            return HasFlags(localFlags, hasFlags, all);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method determines whether the specified <see cref="LookupFlags" /> value
        /// contains a particular set of flags.
        /// </summary>
        /// <param name="flags">
        /// The flags value to be examined.
        /// </param>
        /// <param name="hasFlags">
        /// The flags to look for within the <paramref name="flags" /> value.
        /// </param>
        /// <param name="all">
        /// Non-zero if every flag in <paramref name="hasFlags" /> must be
        /// present; otherwise, only one of them needs to be present.
        /// </param>
        /// <returns>
        /// True if the requested flags are present; otherwise, false.
        /// </returns>
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

        /// <summary>
        /// This method determines whether the specified <see cref="MakeFlags" /> value
        /// contains a particular set of flags.
        /// </summary>
        /// <param name="flags">
        /// The flags value to be examined.
        /// </param>
        /// <param name="hasFlags">
        /// The flags to look for within the <paramref name="flags" /> value.
        /// </param>
        /// <param name="all">
        /// Non-zero if every flag in <paramref name="hasFlags" /> must be
        /// present; otherwise, only one of them needs to be present.
        /// </param>
        /// <returns>
        /// True if the requested flags are present; otherwise, false.
        /// </returns>
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

        /// <summary>
        /// This method determines whether the specified <see cref="MapOpenAccess" /> value
        /// contains a particular set of flags.
        /// </summary>
        /// <param name="access">
        /// The flags value to be examined.
        /// </param>
        /// <param name="flags">
        /// The flags to look for within the <paramref name="access" /> value.
        /// </param>
        /// <param name="all">
        /// Non-zero if every flag in <paramref name="flags" /> must be
        /// present; otherwise, only one of them needs to be present.
        /// </param>
        /// <returns>
        /// True if the requested flags are present; otherwise, false.
        /// </returns>
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

        /// <summary>
        /// This method determines whether the specified <see cref="MarshalFlags" /> value
        /// contains a particular set of flags.
        /// </summary>
        /// <param name="flags">
        /// The flags value to be examined.
        /// </param>
        /// <param name="hasFlags">
        /// The flags to look for within the <paramref name="flags" /> value.
        /// </param>
        /// <param name="all">
        /// Non-zero if every flag in <paramref name="hasFlags" /> must be
        /// present; otherwise, only one of them needs to be present.
        /// </param>
        /// <returns>
        /// True if the requested flags are present; otherwise, false.
        /// </returns>
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

        /// <summary>
        /// This method determines whether the specified <see cref="MatchMode" /> value
        /// contains a particular set of flags.
        /// </summary>
        /// <param name="flags">
        /// The flags value to be examined.
        /// </param>
        /// <param name="hasFlags">
        /// The flags to look for within the <paramref name="flags" /> value.
        /// </param>
        /// <param name="all">
        /// Non-zero if every flag in <paramref name="hasFlags" /> must be
        /// present; otherwise, only one of them needs to be present.
        /// </param>
        /// <returns>
        /// True if the requested flags are present; otherwise, false.
        /// </returns>
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

        /// <summary>
        /// This method determines whether the specified <see cref="MethodAttributes" /> value
        /// contains a particular set of flags.
        /// </summary>
        /// <param name="flags">
        /// The flags value to be examined.
        /// </param>
        /// <param name="hasFlags">
        /// The flags to look for within the <paramref name="flags" /> value.
        /// </param>
        /// <param name="all">
        /// Non-zero if every flag in <paramref name="hasFlags" /> must be
        /// present; otherwise, only one of them needs to be present.
        /// </param>
        /// <returns>
        /// True if the requested flags are present; otherwise, false.
        /// </returns>
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

        /// <summary>
        /// This method determines whether the specified <see cref="MemberTypes" /> value
        /// contains a particular set of flags.
        /// </summary>
        /// <param name="flags">
        /// The flags value to be examined.
        /// </param>
        /// <param name="hasFlags">
        /// The flags to look for within the <paramref name="flags" /> value.
        /// </param>
        /// <param name="all">
        /// Non-zero if every flag in <paramref name="hasFlags" /> must be
        /// present; otherwise, only one of them needs to be present.
        /// </param>
        /// <returns>
        /// True if the requested flags are present; otherwise, false.
        /// </returns>
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

        /// <summary>
        /// This method determines whether the specified <see cref="MethodFlags" /> value
        /// contains a particular set of flags.
        /// </summary>
        /// <param name="flags">
        /// The flags value to be examined.
        /// </param>
        /// <param name="hasFlags">
        /// The flags to look for within the <paramref name="flags" /> value.
        /// </param>
        /// <param name="all">
        /// Non-zero if every flag in <paramref name="hasFlags" /> must be
        /// present; otherwise, only one of them needs to be present.
        /// </param>
        /// <returns>
        /// True if the requested flags are present; otherwise, false.
        /// </returns>
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
        /// <summary>
        /// This method determines whether the specified <see cref="ModuleFlags" /> value
        /// contains a particular set of flags.
        /// </summary>
        /// <param name="flags">
        /// The flags value to be examined.
        /// </param>
        /// <param name="hasFlags">
        /// The flags to look for within the <paramref name="flags" /> value.
        /// </param>
        /// <param name="all">
        /// Non-zero if every flag in <paramref name="hasFlags" /> must be
        /// present; otherwise, only one of them needs to be present.
        /// </param>
        /// <returns>
        /// True if the requested flags are present; otherwise, false.
        /// </returns>
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

        /// <summary>
        /// This method determines whether the specified <see cref="NamespaceFlags" /> value
        /// contains a particular set of flags.
        /// </summary>
        /// <param name="flags">
        /// The flags value to be examined.
        /// </param>
        /// <param name="hasFlags">
        /// The flags to look for within the <paramref name="flags" /> value.
        /// </param>
        /// <param name="all">
        /// Non-zero if every flag in <paramref name="hasFlags" /> must be
        /// present; otherwise, only one of them needs to be present.
        /// </param>
        /// <returns>
        /// True if the requested flags are present; otherwise, false.
        /// </returns>
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
        /// <summary>
        /// This method determines whether the specified <see cref="NotifyFlags" /> value
        /// contains a particular set of flags.
        /// </summary>
        /// <param name="flags">
        /// The flags value to be examined.
        /// </param>
        /// <param name="hasFlags">
        /// The flags to look for within the <paramref name="flags" /> value.
        /// </param>
        /// <param name="all">
        /// Non-zero if every flag in <paramref name="hasFlags" /> must be
        /// present; otherwise, only one of them needs to be present.
        /// </param>
        /// <returns>
        /// True if the requested flags are present; otherwise, false.
        /// </returns>
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

        /// <summary>
        /// This method determines whether the specified <see cref="NotifyType" /> value
        /// contains a particular set of flags.
        /// </summary>
        /// <param name="flags">
        /// The flags value to be examined.
        /// </param>
        /// <param name="hasFlags">
        /// The flags to look for within the <paramref name="flags" /> value.
        /// </param>
        /// <param name="all">
        /// Non-zero if every flag in <paramref name="hasFlags" /> must be
        /// present; otherwise, only one of them needs to be present.
        /// </param>
        /// <returns>
        /// True if the requested flags are present; otherwise, false.
        /// </returns>
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

        /// <summary>
        /// This method determines whether the specified <see cref="ObjectFlags" /> value
        /// contains a particular set of flags.
        /// </summary>
        /// <param name="flags">
        /// The flags value to be examined.
        /// </param>
        /// <param name="hasFlags">
        /// The flags to look for within the <paramref name="flags" /> value.
        /// </param>
        /// <param name="all">
        /// Non-zero if every flag in <paramref name="hasFlags" /> must be
        /// present; otherwise, only one of them needs to be present.
        /// </param>
        /// <returns>
        /// True if the requested flags are present; otherwise, false.
        /// </returns>
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

        /// <summary>
        /// This method determines whether the specified <see cref="ObjectNamespace" /> value
        /// contains a particular set of flags.
        /// </summary>
        /// <param name="flags">
        /// The flags value to be examined.
        /// </param>
        /// <param name="hasFlags">
        /// The flags to look for within the <paramref name="flags" /> value.
        /// </param>
        /// <param name="all">
        /// Non-zero if every flag in <paramref name="hasFlags" /> must be
        /// present; otherwise, only one of them needs to be present.
        /// </param>
        /// <returns>
        /// True if the requested flags are present; otherwise, false.
        /// </returns>
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

        /// <summary>
        /// This method determines whether the specified <see cref="ObjectOptionType" /> value
        /// contains a particular set of flags.
        /// </summary>
        /// <param name="flags">
        /// The flags value to be examined.
        /// </param>
        /// <param name="hasFlags">
        /// The flags to look for within the <paramref name="flags" /> value.
        /// </param>
        /// <param name="all">
        /// Non-zero if every flag in <paramref name="hasFlags" /> must be
        /// present; otherwise, only one of them needs to be present.
        /// </param>
        /// <returns>
        /// True if the requested flags are present; otherwise, false.
        /// </returns>
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

        /// <summary>
        /// This method determines whether the specified <see cref="OperatorFlags" /> value
        /// contains a particular set of flags.
        /// </summary>
        /// <param name="flags">
        /// The flags value to be examined.
        /// </param>
        /// <param name="hasFlags">
        /// The flags to look for within the <paramref name="flags" /> value.
        /// </param>
        /// <param name="all">
        /// Non-zero if every flag in <paramref name="hasFlags" /> must be
        /// present; otherwise, only one of them needs to be present.
        /// </param>
        /// <returns>
        /// True if the requested flags are present; otherwise, false.
        /// </returns>
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

        /// <summary>
        /// This method determines whether the specified <see cref="OptionCategory" /> value
        /// contains a particular set of flags.
        /// </summary>
        /// <param name="flags">
        /// The flags value to be examined.
        /// </param>
        /// <param name="hasFlags">
        /// The flags to look for within the <paramref name="flags" /> value.
        /// </param>
        /// <param name="all">
        /// Non-zero if every flag in <paramref name="hasFlags" /> must be
        /// present; otherwise, only one of them needs to be present.
        /// </param>
        /// <returns>
        /// True if the requested flags are present; otherwise, false.
        /// </returns>
        public static bool HasFlags(
            OptionCategory flags,
            OptionCategory hasFlags,
            bool all
            )
        {
            if (all)
                return ((flags & hasFlags) == hasFlags);
            else
                return ((flags & hasFlags) != OptionCategory.None);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method determines whether the specified <see cref="OptionFlags" /> value
        /// contains a particular set of flags.
        /// </summary>
        /// <param name="flags">
        /// The flags value to be examined.
        /// </param>
        /// <param name="hasFlags">
        /// The flags to look for within the <paramref name="flags" /> value.
        /// </param>
        /// <param name="all">
        /// Non-zero if every flag in <paramref name="hasFlags" /> must be
        /// present; otherwise, only one of them needs to be present.
        /// </param>
        /// <returns>
        /// True if the requested flags are present; otherwise, false.
        /// </returns>
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

        /// <summary>
        /// This method determines whether the specified <see cref="OptionBehaviorFlags" /> value
        /// contains a particular set of flags.
        /// </summary>
        /// <param name="flags">
        /// The flags value to be examined.
        /// </param>
        /// <param name="hasFlags">
        /// The flags to look for within the <paramref name="flags" /> value.
        /// </param>
        /// <param name="all">
        /// Non-zero if every flag in <paramref name="hasFlags" /> must be
        /// present; otherwise, only one of them needs to be present.
        /// </param>
        /// <returns>
        /// True if the requested flags are present; otherwise, false.
        /// </returns>
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

        /// <summary>
        /// This method determines whether the specified <see cref="OptionOriginFlags" /> value
        /// contains a particular set of flags.
        /// </summary>
        /// <param name="flags">
        /// The flags value to be examined.
        /// </param>
        /// <param name="hasFlags">
        /// The flags to look for within the <paramref name="flags" /> value.
        /// </param>
        /// <param name="all">
        /// Non-zero if every flag in <paramref name="hasFlags" /> must be
        /// present; otherwise, only one of them needs to be present.
        /// </param>
        /// <returns>
        /// True if the requested flags are present; otherwise, false.
        /// </returns>
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

        /// <summary>
        /// This method determines whether the specified <see cref="OutputStyle" /> value
        /// contains a particular set of flags.
        /// </summary>
        /// <param name="flags">
        /// The flags value to be examined.
        /// </param>
        /// <param name="hasFlags">
        /// The flags to look for within the <paramref name="flags" /> value.
        /// </param>
        /// <param name="all">
        /// Non-zero if every flag in <paramref name="hasFlags" /> must be
        /// present; otherwise, only one of them needs to be present.
        /// </param>
        /// <returns>
        /// True if the requested flags are present; otherwise, false.
        /// </returns>
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

        /// <summary>
        /// This method determines whether the specified <see cref="PackageFlags" /> value
        /// contains a particular set of flags.
        /// </summary>
        /// <param name="flags">
        /// The flags value to be examined.
        /// </param>
        /// <param name="hasFlags">
        /// The flags to look for within the <paramref name="flags" /> value.
        /// </param>
        /// <param name="all">
        /// Non-zero if every flag in <paramref name="hasFlags" /> must be
        /// present; otherwise, only one of them needs to be present.
        /// </param>
        /// <returns>
        /// True if the requested flags are present; otherwise, false.
        /// </returns>
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

        /// <summary>
        /// This method determines whether the specified <see cref="PackageFlags" /> value
        /// contains a particular set of flags, treating a null value as
        /// <see cref="PackageFlags.None" />.
        /// </summary>
        /// <param name="flags">
        /// The flags value to be examined.  This parameter may be null.
        /// </param>
        /// <param name="hasFlags">
        /// The flags to look for within the <paramref name="flags" /> value.
        /// </param>
        /// <param name="all">
        /// Non-zero if every flag in <paramref name="hasFlags" /> must be
        /// present; otherwise, only one of them needs to be present.
        /// </param>
        /// <returns>
        /// True if the requested flags are present; otherwise, false.
        /// </returns>
        public static bool HasFlags(
            PackageFlags? flags,
            PackageFlags hasFlags,
            bool all
            )
        {
            PackageFlags localFlags = (flags != null) ?
                (PackageFlags)flags : PackageFlags.None;

            return HasFlags(localFlags, hasFlags, all);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method determines whether the specified <see cref="PackageIfNeededFlags" /> value
        /// contains a particular set of flags.
        /// </summary>
        /// <param name="flags">
        /// The flags value to be examined.
        /// </param>
        /// <param name="hasFlags">
        /// The flags to look for within the <paramref name="flags" /> value.
        /// </param>
        /// <param name="all">
        /// Non-zero if every flag in <paramref name="hasFlags" /> must be
        /// present; otherwise, only one of them needs to be present.
        /// </param>
        /// <returns>
        /// True if the requested flags are present; otherwise, false.
        /// </returns>
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

        /// <summary>
        /// This method determines whether the specified <see cref="PackageIndexFlags" /> value
        /// contains a particular set of flags.
        /// </summary>
        /// <param name="flags">
        /// The flags value to be examined.
        /// </param>
        /// <param name="hasFlags">
        /// The flags to look for within the <paramref name="flags" /> value.
        /// </param>
        /// <param name="all">
        /// Non-zero if every flag in <paramref name="hasFlags" /> must be
        /// present; otherwise, only one of them needs to be present.
        /// </param>
        /// <returns>
        /// True if the requested flags are present; otherwise, false.
        /// </returns>
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

        /// <summary>
        /// This method determines whether the specified <see cref="PackageType" /> value
        /// contains a particular set of flags.
        /// </summary>
        /// <param name="flags">
        /// The flags value to be examined.
        /// </param>
        /// <param name="hasFlags">
        /// The flags to look for within the <paramref name="flags" /> value.
        /// </param>
        /// <param name="all">
        /// Non-zero if every flag in <paramref name="hasFlags" /> must be
        /// present; otherwise, only one of them needs to be present.
        /// </param>
        /// <returns>
        /// True if the requested flags are present; otherwise, false.
        /// </returns>
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

        /// <summary>
        /// This method determines whether the specified <see cref="PathFlags" /> value
        /// contains a particular set of flags.
        /// </summary>
        /// <param name="flags">
        /// The flags value to be examined.
        /// </param>
        /// <param name="hasFlags">
        /// The flags to look for within the <paramref name="flags" /> value.
        /// </param>
        /// <param name="all">
        /// Non-zero if every flag in <paramref name="hasFlags" /> must be
        /// present; otherwise, only one of them needs to be present.
        /// </param>
        /// <returns>
        /// True if the requested flags are present; otherwise, false.
        /// </returns>
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

        /// <summary>
        /// This method determines whether the specified <see cref="PathType" /> value
        /// contains a particular set of flags.
        /// </summary>
        /// <param name="flags">
        /// The flags value to be examined.
        /// </param>
        /// <param name="hasFlags">
        /// The flags to look for within the <paramref name="flags" /> value.
        /// </param>
        /// <param name="all">
        /// Non-zero if every flag in <paramref name="hasFlags" /> must be
        /// present; otherwise, only one of them needs to be present.
        /// </param>
        /// <returns>
        /// True if the requested flags are present; otherwise, false.
        /// </returns>
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

        /// <summary>
        /// This method determines whether the specified <see cref="PeerType" /> value
        /// contains a particular set of flags.
        /// </summary>
        /// <param name="flags">
        /// The flags value to be examined.
        /// </param>
        /// <param name="hasFlags">
        /// The flags to look for within the <paramref name="flags" /> value.
        /// </param>
        /// <param name="all">
        /// Non-zero if every flag in <paramref name="hasFlags" /> must be
        /// present; otherwise, only one of them needs to be present.
        /// </param>
        /// <returns>
        /// True if the requested flags are present; otherwise, false.
        /// </returns>
        public static bool HasFlags(
            PeerType flags,
            PeerType hasFlags,
            bool all
            )
        {
            if (all)
                return ((flags & hasFlags) == hasFlags);
            else
                return ((flags & hasFlags) != PeerType.None);
        }

        ///////////////////////////////////////////////////////////////////////

#if TEST
        /// <summary>
        /// This method determines whether the specified <see cref="PkgInstallType" /> value
        /// contains a particular set of flags.
        /// </summary>
        /// <param name="flags">
        /// The flags value to be examined.
        /// </param>
        /// <param name="hasFlags">
        /// The flags to look for within the <paramref name="flags" /> value.
        /// </param>
        /// <param name="all">
        /// Non-zero if every flag in <paramref name="hasFlags" /> must be
        /// present; otherwise, only one of them needs to be present.
        /// </param>
        /// <returns>
        /// True if the requested flags are present; otherwise, false.
        /// </returns>
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

        /// <summary>
        /// This method determines whether the specified <see cref="PluginFlags" /> value
        /// contains a particular set of flags.
        /// </summary>
        /// <param name="flags">
        /// The flags value to be examined.
        /// </param>
        /// <param name="hasFlags">
        /// The flags to look for within the <paramref name="flags" /> value.
        /// </param>
        /// <param name="all">
        /// Non-zero if every flag in <paramref name="hasFlags" /> must be
        /// present; otherwise, only one of them needs to be present.
        /// </param>
        /// <returns>
        /// True if the requested flags are present; otherwise, false.
        /// </returns>
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

        /// <summary>
        /// This method determines whether the specified <see cref="PluginLoaderFlags" /> value
        /// contains a particular set of flags.
        /// </summary>
        /// <param name="flags">
        /// The flags value to be examined.
        /// </param>
        /// <param name="hasFlags">
        /// The flags to look for within the <paramref name="flags" /> value.
        /// </param>
        /// <param name="all">
        /// Non-zero if every flag in <paramref name="hasFlags" /> must be
        /// present; otherwise, only one of them needs to be present.
        /// </param>
        /// <returns>
        /// True if the requested flags are present; otherwise, false.
        /// </returns>
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

        /// <summary>
        /// This method determines whether the specified <see cref="PolicyDecisionType" /> value
        /// contains a particular set of flags.
        /// </summary>
        /// <param name="flags">
        /// The flags value to be examined.
        /// </param>
        /// <param name="hasFlags">
        /// The flags to look for within the <paramref name="flags" /> value.
        /// </param>
        /// <param name="all">
        /// Non-zero if every flag in <paramref name="hasFlags" /> must be
        /// present; otherwise, only one of them needs to be present.
        /// </param>
        /// <returns>
        /// True if the requested flags are present; otherwise, false.
        /// </returns>
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

        /// <summary>
        /// This method determines whether the specified <see cref="PolicyFlags" /> value
        /// contains a particular set of flags.
        /// </summary>
        /// <param name="flags">
        /// The flags value to be examined.
        /// </param>
        /// <param name="hasFlags">
        /// The flags to look for within the <paramref name="flags" /> value.
        /// </param>
        /// <param name="all">
        /// Non-zero if every flag in <paramref name="hasFlags" /> must be
        /// present; otherwise, only one of them needs to be present.
        /// </param>
        /// <returns>
        /// True if the requested flags are present; otherwise, false.
        /// </returns>
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

        /// <summary>
        /// This method determines whether the specified <see cref="ProcedureFlags" /> value
        /// contains a particular set of flags.
        /// </summary>
        /// <param name="flags">
        /// The flags value to be examined.
        /// </param>
        /// <param name="hasFlags">
        /// The flags to look for within the <paramref name="flags" /> value.
        /// </param>
        /// <param name="all">
        /// Non-zero if every flag in <paramref name="hasFlags" /> must be
        /// present; otherwise, only one of them needs to be present.
        /// </param>
        /// <returns>
        /// True if the requested flags are present; otherwise, false.
        /// </returns>
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

        /// <summary>
        /// This method determines whether the specified <see cref="PromptFlags" /> value
        /// contains a particular set of flags.
        /// </summary>
        /// <param name="flags">
        /// The flags value to be examined.
        /// </param>
        /// <param name="hasFlags">
        /// The flags to look for within the <paramref name="flags" /> value.
        /// </param>
        /// <param name="all">
        /// Non-zero if every flag in <paramref name="hasFlags" /> must be
        /// present; otherwise, only one of them needs to be present.
        /// </param>
        /// <returns>
        /// True if the requested flags are present; otherwise, false.
        /// </returns>
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

        /// <summary>
        /// This method determines whether the specified <see cref="QueueFlags" /> value
        /// contains a particular set of flags.
        /// </summary>
        /// <param name="flags">
        /// The flags value to be examined.
        /// </param>
        /// <param name="hasFlags">
        /// The flags to look for within the <paramref name="flags" /> value.
        /// </param>
        /// <param name="all">
        /// Non-zero if every flag in <paramref name="hasFlags" /> must be
        /// present; otherwise, only one of them needs to be present.
        /// </param>
        /// <returns>
        /// True if the requested flags are present; otherwise, false.
        /// </returns>
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

        /// <summary>
        /// This method determines whether the specified <see cref="ReadyFlags" /> value
        /// contains a particular set of flags.
        /// </summary>
        /// <param name="flags">
        /// The flags value to be examined.
        /// </param>
        /// <param name="hasFlags">
        /// The flags to look for within the <paramref name="flags" /> value.
        /// </param>
        /// <param name="all">
        /// Non-zero if every flag in <paramref name="hasFlags" /> must be
        /// present; otherwise, only one of them needs to be present.
        /// </param>
        /// <returns>
        /// True if the requested flags are present; otherwise, false.
        /// </returns>
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

        /// <summary>
        /// This method determines whether the specified <see cref="ReorderFlags" /> value
        /// contains a particular set of flags.
        /// </summary>
        /// <param name="flags">
        /// The flags value to be examined.
        /// </param>
        /// <param name="hasFlags">
        /// The flags to look for within the <paramref name="flags" /> value.
        /// </param>
        /// <param name="all">
        /// Non-zero if every flag in <paramref name="hasFlags" /> must be
        /// present; otherwise, only one of them needs to be present.
        /// </param>
        /// <returns>
        /// True if the requested flags are present; otherwise, false.
        /// </returns>
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

        /// <summary>
        /// This method determines whether the specified <see cref="ResolveFlags" /> value
        /// contains a particular set of flags.
        /// </summary>
        /// <param name="flags">
        /// The flags value to be examined.
        /// </param>
        /// <param name="hasFlags">
        /// The flags to look for within the <paramref name="flags" /> value.
        /// </param>
        /// <param name="all">
        /// Non-zero if every flag in <paramref name="hasFlags" /> must be
        /// present; otherwise, only one of them needs to be present.
        /// </param>
        /// <returns>
        /// True if the requested flags are present; otherwise, false.
        /// </returns>
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

        /// <summary>
        /// This method determines whether the specified <see cref="ResultFlags" /> value
        /// contains a particular set of flags.
        /// </summary>
        /// <param name="flags">
        /// The flags value to be examined.
        /// </param>
        /// <param name="hasFlags">
        /// The flags to look for within the <paramref name="flags" /> value.
        /// </param>
        /// <param name="all">
        /// Non-zero if every flag in <paramref name="hasFlags" /> must be
        /// present; otherwise, only one of them needs to be present.
        /// </param>
        /// <returns>
        /// True if the requested flags are present; otherwise, false.
        /// </returns>
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

        /// <summary>
        /// This method determines whether the specified <see cref="RuleSetType" /> value
        /// contains a particular set of flags.
        /// </summary>
        /// <param name="flags">
        /// The flags value to be examined.
        /// </param>
        /// <param name="hasFlags">
        /// The flags to look for within the <paramref name="flags" /> value.
        /// </param>
        /// <param name="all">
        /// Non-zero if every flag in <paramref name="hasFlags" /> must be
        /// present; otherwise, only one of them needs to be present.
        /// </param>
        /// <returns>
        /// True if the requested flags are present; otherwise, false.
        /// </returns>
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

        /// <summary>
        /// This method determines whether the specified <see cref="ScriptFlags" /> value
        /// contains a particular set of flags.
        /// </summary>
        /// <param name="flags">
        /// The flags value to be examined.
        /// </param>
        /// <param name="hasFlags">
        /// The flags to look for within the <paramref name="flags" /> value.
        /// </param>
        /// <param name="all">
        /// Non-zero if every flag in <paramref name="hasFlags" /> must be
        /// present; otherwise, only one of them needs to be present.
        /// </param>
        /// <returns>
        /// True if the requested flags are present; otherwise, false.
        /// </returns>
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

        /// <summary>
        /// This method determines whether the specified <see cref="ScriptBlockFlags" /> value
        /// contains a particular set of flags.
        /// </summary>
        /// <param name="flags">
        /// The flags value to be examined.
        /// </param>
        /// <param name="hasFlags">
        /// The flags to look for within the <paramref name="flags" /> value.
        /// </param>
        /// <param name="all">
        /// Non-zero if every flag in <paramref name="hasFlags" /> must be
        /// present; otherwise, only one of them needs to be present.
        /// </param>
        /// <returns>
        /// True if the requested flags are present; otherwise, false.
        /// </returns>
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

        /// <summary>
        /// This method determines whether the specified <see cref="ScriptDataFlags" /> value
        /// contains a particular set of flags.
        /// </summary>
        /// <param name="flags">
        /// The flags value to be examined.
        /// </param>
        /// <param name="hasFlags">
        /// The flags to look for within the <paramref name="flags" /> value.
        /// </param>
        /// <param name="all">
        /// Non-zero if every flag in <paramref name="hasFlags" /> must be
        /// present; otherwise, only one of them needs to be present.
        /// </param>
        /// <returns>
        /// True if the requested flags are present; otherwise, false.
        /// </returns>
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

        /// <summary>
        /// This method determines whether the specified <see cref="ScriptSecurityFlags" /> value
        /// contains a particular set of flags.
        /// </summary>
        /// <param name="flags">
        /// The flags value to be examined.
        /// </param>
        /// <param name="hasFlags">
        /// The flags to look for within the <paramref name="flags" /> value.
        /// </param>
        /// <param name="all">
        /// Non-zero if every flag in <paramref name="hasFlags" /> must be
        /// present; otherwise, only one of them needs to be present.
        /// </param>
        /// <returns>
        /// True if the requested flags are present; otherwise, false.
        /// </returns>
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
        /// <summary>
        /// This method determines whether the specified <see cref="SddlFlags" /> value
        /// contains a particular set of flags.
        /// </summary>
        /// <param name="flags">
        /// The flags value to be examined.
        /// </param>
        /// <param name="hasFlags">
        /// The flags to look for within the <paramref name="flags" /> value.
        /// </param>
        /// <param name="all">
        /// Non-zero if every flag in <paramref name="hasFlags" /> must be
        /// present; otherwise, only one of them needs to be present.
        /// </param>
        /// <returns>
        /// True if the requested flags are present; otherwise, false.
        /// </returns>
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

        /// <summary>
        /// This method determines whether the specified <see cref="SdkType" /> value
        /// contains a particular set of flags.
        /// </summary>
        /// <param name="flags">
        /// The flags value to be examined.
        /// </param>
        /// <param name="hasFlags">
        /// The flags to look for within the <paramref name="flags" /> value.
        /// </param>
        /// <param name="all">
        /// Non-zero if every flag in <paramref name="hasFlags" /> must be
        /// present; otherwise, only one of them needs to be present.
        /// </param>
        /// <returns>
        /// True if the requested flags are present; otherwise, false.
        /// </returns>
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

        /// <summary>
        /// This method determines whether the specified <see cref="SecretDataFlags" /> value
        /// contains a particular set of flags.
        /// </summary>
        /// <param name="flags">
        /// The flags value to be examined.
        /// </param>
        /// <param name="hasFlags">
        /// The flags to look for within the <paramref name="flags" /> value.
        /// </param>
        /// <param name="all">
        /// Non-zero if every flag in <paramref name="hasFlags" /> must be
        /// present; otherwise, only one of them needs to be present.
        /// </param>
        /// <returns>
        /// True if the requested flags are present; otherwise, false.
        /// </returns>
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

        /// <summary>
        /// This method determines whether the specified <see cref="SecretDataFlags" /> value
        /// contains a particular set of flags, treating a null value as
        /// <see cref="SecretDataFlags.None" />.
        /// </summary>
        /// <param name="flags">
        /// The flags value to be examined.  This parameter may be null.
        /// </param>
        /// <param name="hasFlags">
        /// The flags to look for within the <paramref name="flags" /> value.
        /// </param>
        /// <param name="all">
        /// Non-zero if every flag in <paramref name="hasFlags" /> must be
        /// present; otherwise, only one of them needs to be present.
        /// </param>
        /// <returns>
        /// True if the requested flags are present; otherwise, false.
        /// </returns>
        public static bool HasFlags(
            SecretDataFlags? flags,
            SecretDataFlags hasFlags,
            bool all
            )
        {
            SecretDataFlags localFlags = (flags != null) ?
                (SecretDataFlags)flags : SecretDataFlags.None;

            return HasFlags(localFlags, hasFlags, all);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method determines whether the specified <see cref="SecurityLevel" /> value
        /// contains a particular set of flags.
        /// </summary>
        /// <param name="flags">
        /// The flags value to be examined.
        /// </param>
        /// <param name="hasFlags">
        /// The flags to look for within the <paramref name="flags" /> value.
        /// </param>
        /// <param name="all">
        /// Non-zero if every flag in <paramref name="hasFlags" /> must be
        /// present; otherwise, only one of them needs to be present.
        /// </param>
        /// <returns>
        /// True if the requested flags are present; otherwise, false.
        /// </returns>
        public static bool HasFlags(
            SecurityLevel flags,
            SecurityLevel hasFlags,
            bool all
            )
        {
            if (all)
                return ((flags & hasFlags) == hasFlags);
            else
                return ((flags & hasFlags) != SecurityLevel.None);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method determines whether the specified <see cref="SettingFlags" /> value
        /// contains a particular set of flags.
        /// </summary>
        /// <param name="flags">
        /// The flags value to be examined.
        /// </param>
        /// <param name="hasFlags">
        /// The flags to look for within the <paramref name="flags" /> value.
        /// </param>
        /// <param name="all">
        /// Non-zero if every flag in <paramref name="hasFlags" /> must be
        /// present; otherwise, only one of them needs to be present.
        /// </param>
        /// <returns>
        /// True if the requested flags are present; otherwise, false.
        /// </returns>
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

        /// <summary>
        /// This method determines whether the specified <see cref="ShutdownFlags" /> value
        /// contains a particular set of flags.
        /// </summary>
        /// <param name="flags">
        /// The flags value to be examined.
        /// </param>
        /// <param name="hasFlags">
        /// The flags to look for within the <paramref name="flags" /> value.
        /// </param>
        /// <param name="all">
        /// Non-zero if every flag in <paramref name="hasFlags" /> must be
        /// present; otherwise, only one of them needs to be present.
        /// </param>
        /// <returns>
        /// True if the requested flags are present; otherwise, false.
        /// </returns>
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
        /// <summary>
        /// This method determines whether the specified <see cref="SimulatedKeyFlags" /> value
        /// contains a particular set of flags.
        /// </summary>
        /// <param name="flags">
        /// The flags value to be examined.
        /// </param>
        /// <param name="hasFlags">
        /// The flags to look for within the <paramref name="flags" /> value.
        /// </param>
        /// <param name="all">
        /// Non-zero if every flag in <paramref name="hasFlags" /> must be
        /// present; otherwise, only one of them needs to be present.
        /// </param>
        /// <returns>
        /// True if the requested flags are present; otherwise, false.
        /// </returns>
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

        /// <summary>
        /// This method determines whether the specified <see cref="SnippetFlags" /> value
        /// contains a particular set of flags.
        /// </summary>
        /// <param name="flags">
        /// The flags value to be examined.
        /// </param>
        /// <param name="hasFlags">
        /// The flags to look for within the <paramref name="flags" /> value.
        /// </param>
        /// <param name="all">
        /// Non-zero if every flag in <paramref name="hasFlags" /> must be
        /// present; otherwise, only one of them needs to be present.
        /// </param>
        /// <returns>
        /// True if the requested flags are present; otherwise, false.
        /// </returns>
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

        /// <summary>
        /// This method determines whether the specified <see cref="StreamDirection" /> value
        /// contains a particular set of flags.
        /// </summary>
        /// <param name="flags">
        /// The flags value to be examined.
        /// </param>
        /// <param name="hasFlags">
        /// The flags to look for within the <paramref name="flags" /> value.
        /// </param>
        /// <param name="all">
        /// Non-zero if every flag in <paramref name="hasFlags" /> must be
        /// present; otherwise, only one of them needs to be present.
        /// </param>
        /// <returns>
        /// True if the requested flags are present; otherwise, false.
        /// </returns>
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

        /// <summary>
        /// This method determines whether the specified <see cref="StreamFlags" /> value
        /// contains a particular set of flags.
        /// </summary>
        /// <param name="flags">
        /// The flags value to be examined.
        /// </param>
        /// <param name="hasFlags">
        /// The flags to look for within the <paramref name="flags" /> value.
        /// </param>
        /// <param name="all">
        /// Non-zero if every flag in <paramref name="hasFlags" /> must be
        /// present; otherwise, only one of them needs to be present.
        /// </param>
        /// <returns>
        /// True if the requested flags are present; otherwise, false.
        /// </returns>
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

        /// <summary>
        /// This method determines whether the specified <see cref="SubCommandFlags" /> value
        /// contains a particular set of flags.
        /// </summary>
        /// <param name="flags">
        /// The flags value to be examined.
        /// </param>
        /// <param name="hasFlags">
        /// The flags to look for within the <paramref name="flags" /> value.
        /// </param>
        /// <param name="all">
        /// Non-zero if every flag in <paramref name="hasFlags" /> must be
        /// present; otherwise, only one of them needs to be present.
        /// </param>
        /// <returns>
        /// True if the requested flags are present; otherwise, false.
        /// </returns>
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

        /// <summary>
        /// This method determines whether the specified <see cref="SwapFlags" /> value
        /// contains a particular set of flags.
        /// </summary>
        /// <param name="flags">
        /// The flags value to be examined.
        /// </param>
        /// <param name="hasFlags">
        /// The flags to look for within the <paramref name="flags" /> value.
        /// </param>
        /// <param name="all">
        /// Non-zero if every flag in <paramref name="hasFlags" /> must be
        /// present; otherwise, only one of them needs to be present.
        /// </param>
        /// <returns>
        /// True if the requested flags are present; otherwise, false.
        /// </returns>
        public static bool HasFlags(
            SwapFlags flags,
            SwapFlags hasFlags,
            bool all
            )
        {
            if (all)
                return ((flags & hasFlags) == hasFlags);
            else
                return ((flags & hasFlags) != SwapFlags.None);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method determines whether the specified <see cref="SyntaxDataFlags" /> value
        /// contains a particular set of flags.
        /// </summary>
        /// <param name="flags">
        /// The flags value to be examined.
        /// </param>
        /// <param name="hasFlags">
        /// The flags to look for within the <paramref name="flags" /> value.
        /// </param>
        /// <param name="all">
        /// Non-zero if every flag in <paramref name="hasFlags" /> must be
        /// present; otherwise, only one of them needs to be present.
        /// </param>
        /// <returns>
        /// True if the requested flags are present; otherwise, false.
        /// </returns>
        public static bool HasFlags(
            SyntaxDataFlags flags,
            SyntaxDataFlags hasFlags,
            bool all
            )
        {
            if (all)
                return ((flags & hasFlags) == hasFlags);
            else
                return ((flags & hasFlags) != SyntaxDataFlags.None);
        }

        ///////////////////////////////////////////////////////////////////////

#if NATIVE && TCL
        /// <summary>
        /// This method determines whether the specified <see cref="Tcl_VarFlags" /> value
        /// contains a particular set of flags.
        /// </summary>
        /// <param name="flags">
        /// The flags value to be examined.
        /// </param>
        /// <param name="hasFlags">
        /// The flags to look for within the <paramref name="flags" /> value.
        /// </param>
        /// <param name="all">
        /// Non-zero if every flag in <paramref name="hasFlags" /> must be
        /// present; otherwise, only one of them needs to be present.
        /// </param>
        /// <returns>
        /// True if the requested flags are present; otherwise, false.
        /// </returns>
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

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method determines whether the specified <see cref="TclCreateFlags" /> value
        /// contains a particular set of flags.
        /// </summary>
        /// <param name="flags">
        /// The flags value to be examined.
        /// </param>
        /// <param name="hasFlags">
        /// The flags to look for within the <paramref name="flags" /> value.
        /// </param>
        /// <param name="all">
        /// Non-zero if every flag in <paramref name="hasFlags" /> must be
        /// present; otherwise, only one of them needs to be present.
        /// </param>
        /// <returns>
        /// True if the requested flags are present; otherwise, false.
        /// </returns>
        public static bool HasFlags(
            TclCreateFlags flags,
            TclCreateFlags hasFlags,
            bool all
            )
        {
            if (all)
                return ((flags & hasFlags) == hasFlags);
            else
                return ((flags & hasFlags) != TclCreateFlags.None);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method determines whether the specified <see cref="TclCommandFlags" /> value
        /// contains a particular set of flags.
        /// </summary>
        /// <param name="flags">
        /// The flags value to be examined.
        /// </param>
        /// <param name="hasFlags">
        /// The flags to look for within the <paramref name="flags" /> value.
        /// </param>
        /// <param name="all">
        /// Non-zero if every flag in <paramref name="hasFlags" /> must be
        /// present; otherwise, only one of them needs to be present.
        /// </param>
        /// <returns>
        /// True if the requested flags are present; otherwise, false.
        /// </returns>
        public static bool HasFlags(
            TclCommandFlags flags,
            TclCommandFlags hasFlags,
            bool all
            )
        {
            if (all)
                return ((flags & hasFlags) == hasFlags);
            else
                return ((flags & hasFlags) != TclCommandFlags.None);
        }

        ///////////////////////////////////////////////////////////////////////

#if TCL_THREADS
        /// <summary>
        /// This method determines whether the specified <see cref="TclThreadFlags" /> value
        /// contains a particular set of flags.
        /// </summary>
        /// <param name="flags">
        /// The flags value to be examined.
        /// </param>
        /// <param name="hasFlags">
        /// The flags to look for within the <paramref name="flags" /> value.
        /// </param>
        /// <param name="all">
        /// Non-zero if every flag in <paramref name="hasFlags" /> must be
        /// present; otherwise, only one of them needs to be present.
        /// </param>
        /// <returns>
        /// True if the requested flags are present; otherwise, false.
        /// </returns>
        public static bool HasFlags(
            TclThreadFlags flags,
            TclThreadFlags hasFlags,
            bool all
            )
        {
            if (all)
                return ((flags & hasFlags) == hasFlags);
            else
                return ((flags & hasFlags) != TclThreadFlags.None);
        }
#endif
#endif

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method determines whether the specified <see cref="TestHookType" /> value
        /// contains a particular set of flags.
        /// </summary>
        /// <param name="flags">
        /// The flags value to be examined.
        /// </param>
        /// <param name="hasFlags">
        /// The flags to look for within the <paramref name="flags" /> value.
        /// </param>
        /// <param name="all">
        /// Non-zero if every flag in <paramref name="hasFlags" /> must be
        /// present; otherwise, only one of them needs to be present.
        /// </param>
        /// <returns>
        /// True if the requested flags are present; otherwise, false.
        /// </returns>
        public static bool HasFlags(
            TestHookType flags,
            TestHookType hasFlags,
            bool all
            )
        {
            if (all)
                return ((flags & hasFlags) == hasFlags);
            else
                return ((flags & hasFlags) != TestHookType.None);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method determines whether the specified <see cref="TestOutputType" /> value
        /// contains a particular set of flags.
        /// </summary>
        /// <param name="flags">
        /// The flags value to be examined.
        /// </param>
        /// <param name="hasFlags">
        /// The flags to look for within the <paramref name="flags" /> value.
        /// </param>
        /// <param name="all">
        /// Non-zero if every flag in <paramref name="hasFlags" /> must be
        /// present; otherwise, only one of them needs to be present.
        /// </param>
        /// <returns>
        /// True if the requested flags are present; otherwise, false.
        /// </returns>
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
        /// <summary>
        /// This method determines whether the specified <see cref="TestResolveFlags" /> value
        /// contains a particular set of flags.
        /// </summary>
        /// <param name="flags">
        /// The flags value to be examined.
        /// </param>
        /// <param name="hasFlags">
        /// The flags to look for within the <paramref name="flags" /> value.
        /// </param>
        /// <param name="all">
        /// Non-zero if every flag in <paramref name="hasFlags" /> must be
        /// present; otherwise, only one of them needs to be present.
        /// </param>
        /// <returns>
        /// True if the requested flags are present; otherwise, false.
        /// </returns>
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

        /// <summary>
        /// This method determines whether the specified <see cref="ThreadFlags" /> value
        /// contains a particular set of flags.
        /// </summary>
        /// <param name="flags">
        /// The flags value to be examined.
        /// </param>
        /// <param name="hasFlags">
        /// The flags to look for within the <paramref name="flags" /> value.
        /// </param>
        /// <param name="all">
        /// Non-zero if every flag in <paramref name="hasFlags" /> must be
        /// present; otherwise, only one of them needs to be present.
        /// </param>
        /// <returns>
        /// True if the requested flags are present; otherwise, false.
        /// </returns>
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

        /// <summary>
        /// This method determines whether the specified <see cref="TimeoutFlags" /> value
        /// contains a particular set of flags.
        /// </summary>
        /// <param name="flags">
        /// The flags value to be examined.
        /// </param>
        /// <param name="hasFlags">
        /// The flags to look for within the <paramref name="flags" /> value.
        /// </param>
        /// <param name="all">
        /// Non-zero if every flag in <paramref name="hasFlags" /> must be
        /// present; otherwise, only one of them needs to be present.
        /// </param>
        /// <returns>
        /// True if the requested flags are present; otherwise, false.
        /// </returns>
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

        /// <summary>
        /// This method determines whether the specified <see cref="TimeoutType" /> value
        /// contains a particular set of flags.
        /// </summary>
        /// <param name="flags">
        /// The flags value to be examined.
        /// </param>
        /// <param name="hasFlags">
        /// The flags to look for within the <paramref name="flags" /> value.
        /// </param>
        /// <param name="all">
        /// Non-zero if every flag in <paramref name="hasFlags" /> must be
        /// present; otherwise, only one of them needs to be present.
        /// </param>
        /// <returns>
        /// True if the requested flags are present; otherwise, false.
        /// </returns>
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

        /// <summary>
        /// This method determines whether the specified <see cref="TokenFlags" /> value
        /// contains a particular set of flags.
        /// </summary>
        /// <param name="flags">
        /// The flags value to be examined.
        /// </param>
        /// <param name="hasFlags">
        /// The flags to look for within the <paramref name="flags" /> value.
        /// </param>
        /// <param name="all">
        /// Non-zero if every flag in <paramref name="hasFlags" /> must be
        /// present; otherwise, only one of them needs to be present.
        /// </param>
        /// <returns>
        /// True if the requested flags are present; otherwise, false.
        /// </returns>
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

        /// <summary>
        /// This method determines whether the specified <see cref="ToStringFlags" /> value
        /// contains a particular set of flags.
        /// </summary>
        /// <param name="flags">
        /// The flags value to be examined.
        /// </param>
        /// <param name="hasFlags">
        /// The flags to look for within the <paramref name="flags" /> value.
        /// </param>
        /// <param name="all">
        /// Non-zero if every flag in <paramref name="hasFlags" /> must be
        /// present; otherwise, only one of them needs to be present.
        /// </param>
        /// <returns>
        /// True if the requested flags are present; otherwise, false.
        /// </returns>
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

        /// <summary>
        /// This method determines whether the specified <see cref="TraceCategoryType" /> value
        /// contains a particular set of flags.
        /// </summary>
        /// <param name="flags">
        /// The flags value to be examined.
        /// </param>
        /// <param name="hasFlags">
        /// The flags to look for within the <paramref name="flags" /> value.
        /// </param>
        /// <param name="all">
        /// Non-zero if every flag in <paramref name="hasFlags" /> must be
        /// present; otherwise, only one of them needs to be present.
        /// </param>
        /// <returns>
        /// True if the requested flags are present; otherwise, false.
        /// </returns>
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

        /// <summary>
        /// This method determines whether the specified <see cref="TraceFlags" /> value
        /// contains a particular set of flags.
        /// </summary>
        /// <param name="flags">
        /// The flags value to be examined.
        /// </param>
        /// <param name="hasFlags">
        /// The flags to look for within the <paramref name="flags" /> value.
        /// </param>
        /// <param name="all">
        /// Non-zero if every flag in <paramref name="hasFlags" /> must be
        /// present; otherwise, only one of them needs to be present.
        /// </param>
        /// <returns>
        /// True if the requested flags are present; otherwise, false.
        /// </returns>
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

        /// <summary>
        /// This method determines whether the specified <see cref="TracePriority" /> value
        /// contains a particular set of flags.
        /// </summary>
        /// <param name="flags">
        /// The flags value to be examined.
        /// </param>
        /// <param name="hasFlags">
        /// The flags to look for within the <paramref name="flags" /> value.
        /// </param>
        /// <param name="all">
        /// Non-zero if every flag in <paramref name="hasFlags" /> must be
        /// present; otherwise, only one of them needs to be present.
        /// </param>
        /// <returns>
        /// True if the requested flags are present; otherwise, false.
        /// </returns>
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

        /// <summary>
        /// This method determines whether the specified <see cref="TraceStateType" /> value
        /// contains a particular set of flags.
        /// </summary>
        /// <param name="flags">
        /// The flags value to be examined.
        /// </param>
        /// <param name="hasFlags">
        /// The flags to look for within the <paramref name="flags" /> value.
        /// </param>
        /// <param name="all">
        /// Non-zero if every flag in <paramref name="hasFlags" /> must be
        /// present; otherwise, only one of them needs to be present.
        /// </param>
        /// <returns>
        /// True if the requested flags are present; otherwise, false.
        /// </returns>
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

        /// <summary>
        /// This method determines whether the specified <see cref="TrustFlags" /> value
        /// contains a particular set of flags.
        /// </summary>
        /// <param name="flags">
        /// The flags value to be examined.
        /// </param>
        /// <param name="hasFlags">
        /// The flags to look for within the <paramref name="flags" /> value.
        /// </param>
        /// <param name="all">
        /// Non-zero if every flag in <paramref name="hasFlags" /> must be
        /// present; otherwise, only one of them needs to be present.
        /// </param>
        /// <returns>
        /// True if the requested flags are present; otherwise, false.
        /// </returns>
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

        /// <summary>
        /// This method determines whether the specified <see cref="TypeListFlags" /> value
        /// contains a particular set of flags.
        /// </summary>
        /// <param name="flags">
        /// The flags value to be examined.
        /// </param>
        /// <param name="hasFlags">
        /// The flags to look for within the <paramref name="flags" /> value.
        /// </param>
        /// <param name="all">
        /// Non-zero if every flag in <paramref name="hasFlags" /> must be
        /// present; otherwise, only one of them needs to be present.
        /// </param>
        /// <returns>
        /// True if the requested flags are present; otherwise, false.
        /// </returns>
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
        /// <summary>
        /// This method determines whether the specified <see cref="UnloadFlags" /> value
        /// contains a particular set of flags.
        /// </summary>
        /// <param name="flags">
        /// The flags value to be examined.
        /// </param>
        /// <param name="hasFlags">
        /// The flags to look for within the <paramref name="flags" /> value.
        /// </param>
        /// <param name="all">
        /// Non-zero if every flag in <paramref name="hasFlags" /> must be
        /// present; otherwise, only one of them needs to be present.
        /// </param>
        /// <returns>
        /// True if the requested flags are present; otherwise, false.
        /// </returns>
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

        /// <summary>
        /// This method determines whether the specified <see cref="UpdateFlags" /> value
        /// contains a particular set of flags.
        /// </summary>
        /// <param name="flags">
        /// The flags value to be examined.
        /// </param>
        /// <param name="hasFlags">
        /// The flags to look for within the <paramref name="flags" /> value.
        /// </param>
        /// <param name="all">
        /// Non-zero if every flag in <paramref name="hasFlags" /> must be
        /// present; otherwise, only one of them needs to be present.
        /// </param>
        /// <returns>
        /// True if the requested flags are present; otherwise, false.
        /// </returns>
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

        /// <summary>
        /// This method determines whether the specified <see cref="UriComponents" /> value
        /// contains a particular set of flags.
        /// </summary>
        /// <param name="flags">
        /// The flags value to be examined.
        /// </param>
        /// <param name="hasFlags">
        /// The flags to look for within the <paramref name="flags" /> value.
        /// </param>
        /// <param name="all">
        /// Non-zero if every flag in <paramref name="hasFlags" /> must be
        /// present; otherwise, only one of them needs to be present.
        /// </param>
        /// <returns>
        /// True if the requested flags are present; otherwise, false.
        /// </returns>
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

        /// <summary>
        /// This method determines whether the specified <see cref="UriFlags" /> value
        /// contains a particular set of flags.
        /// </summary>
        /// <param name="flags">
        /// The flags value to be examined.
        /// </param>
        /// <param name="hasFlags">
        /// The flags to look for within the <paramref name="flags" /> value.
        /// </param>
        /// <param name="all">
        /// Non-zero if every flag in <paramref name="hasFlags" /> must be
        /// present; otherwise, only one of them needs to be present.
        /// </param>
        /// <returns>
        /// True if the requested flags are present; otherwise, false.
        /// </returns>
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

        /// <summary>
        /// This method determines whether the specified <see cref="ValueFlags" /> value
        /// contains a particular set of flags.
        /// </summary>
        /// <param name="flags">
        /// The flags value to be examined.
        /// </param>
        /// <param name="hasFlags">
        /// The flags to look for within the <paramref name="flags" /> value.
        /// </param>
        /// <param name="all">
        /// Non-zero if every flag in <paramref name="hasFlags" /> must be
        /// present; otherwise, only one of them needs to be present.
        /// </param>
        /// <returns>
        /// True if the requested flags are present; otherwise, false.
        /// </returns>
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

        /// <summary>
        /// This method determines whether the specified <see cref="VariableFlags" /> value
        /// contains a particular set of flags.
        /// </summary>
        /// <param name="flags">
        /// The flags value to be examined.
        /// </param>
        /// <param name="hasFlags">
        /// The flags to look for within the <paramref name="flags" /> value.
        /// </param>
        /// <param name="all">
        /// Non-zero if every flag in <paramref name="hasFlags" /> must be
        /// present; otherwise, only one of them needs to be present.
        /// </param>
        /// <returns>
        /// True if the requested flags are present; otherwise, false.
        /// </returns>
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

        /// <summary>
        /// This method determines whether the specified <see cref="VerifyFlags" /> value
        /// contains a particular set of flags.
        /// </summary>
        /// <param name="flags">
        /// The flags value to be examined.
        /// </param>
        /// <param name="hasFlags">
        /// The flags to look for within the <paramref name="flags" /> value.
        /// </param>
        /// <param name="all">
        /// Non-zero if every flag in <paramref name="hasFlags" /> must be
        /// present; otherwise, only one of them needs to be present.
        /// </param>
        /// <returns>
        /// True if the requested flags are present; otherwise, false.
        /// </returns>
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

        /// <summary>
        /// This method determines whether the specified <see cref="VersionFlags" /> value
        /// contains a particular set of flags.
        /// </summary>
        /// <param name="flags">
        /// The flags value to be examined.
        /// </param>
        /// <param name="hasFlags">
        /// The flags to look for within the <paramref name="flags" /> value.
        /// </param>
        /// <param name="all">
        /// Non-zero if every flag in <paramref name="hasFlags" /> must be
        /// present; otherwise, only one of them needs to be present.
        /// </param>
        /// <returns>
        /// True if the requested flags are present; otherwise, false.
        /// </returns>
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

        /// <summary>
        /// This method determines whether the specified <see cref="WatchdogOperation" /> value
        /// contains a particular set of flags.
        /// </summary>
        /// <param name="flags">
        /// The flags value to be examined.
        /// </param>
        /// <param name="hasFlags">
        /// The flags to look for within the <paramref name="flags" /> value.
        /// </param>
        /// <param name="all">
        /// Non-zero if every flag in <paramref name="hasFlags" /> must be
        /// present; otherwise, only one of them needs to be present.
        /// </param>
        /// <returns>
        /// True if the requested flags are present; otherwise, false.
        /// </returns>
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
        /// <summary>
        /// This method determines whether the specified <see cref="WebFlags" /> value
        /// contains a particular set of flags.
        /// </summary>
        /// <param name="flags">
        /// The flags value to be examined.
        /// </param>
        /// <param name="hasFlags">
        /// The flags to look for within the <paramref name="flags" /> value.
        /// </param>
        /// <param name="all">
        /// Non-zero if every flag in <paramref name="hasFlags" /> must be
        /// present; otherwise, only one of them needs to be present.
        /// </param>
        /// <returns>
        /// True if the requested flags are present; otherwise, false.
        /// </returns>
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

        /// <summary>
        /// This method determines whether the specified <see cref="WhiteSpaceFlags" /> value
        /// contains a particular set of flags.
        /// </summary>
        /// <param name="flags">
        /// The flags value to be examined.
        /// </param>
        /// <param name="hasFlags">
        /// The flags to look for within the <paramref name="flags" /> value.
        /// </param>
        /// <param name="all">
        /// Non-zero if every flag in <paramref name="hasFlags" /> must be
        /// present; otherwise, only one of them needs to be present.
        /// </param>
        /// <returns>
        /// True if the requested flags are present; otherwise, false.
        /// </returns>
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
        /// <summary>
        /// This method determines whether the specified <see cref="XmlErrorTypes" /> value
        /// contains a particular set of flags.
        /// </summary>
        /// <param name="flags">
        /// The flags value to be examined.
        /// </param>
        /// <param name="hasFlags">
        /// The flags to look for within the <paramref name="flags" /> value.
        /// </param>
        /// <param name="all">
        /// Non-zero if every flag in <paramref name="hasFlags" /> must be
        /// present; otherwise, only one of them needs to be present.
        /// </param>
        /// <returns>
        /// True if the requested flags are present; otherwise, false.
        /// </returns>
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
