/*
 * CommandOptions.cs --
 *
 * Copyright (c) 2007-2012 by Joe Mistachkin.  All rights reserved.
 *
 * See the file "license.terms" for information on usage and redistribution of
 * this file, and for a DISCLAIMER OF ALL WARRANTIES.
 *
 * RCS: @(#) $Id: $
 */

using System;
using System.Globalization;
using System.IO;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;
using Eagle._Attributes;
using Eagle._Components.Public;
using Eagle._Constants;
using Eagle._Containers.Public;
using Eagle._Interfaces.Public;

#if NET_STANDARD_21
using Index = Eagle._Constants.Index;
#endif

namespace Eagle._Components.Private
{
    [ObjectId("affc5b7f-3ff3-4297-a3b6-0e8969e02766")]
    internal static class CommandOptions
    {
        #region Command Option "Factory" Methods
        #region [after] Command Options
        //
        // NOTE: This is for the [after idle] sub-command.
        //
        private static OptionDictionary GetAfterIdleOptions()
        {
            return new OptionDictionary(
                new IOption[] {
                new Option(null, OptionFlags.MustHaveWideIntegerValue |
                    OptionFlags.Unsafe, Index.Invalid, Index.Invalid,
                    "-thread", null),
                new Option(typeof(EventPriority),
                    OptionFlags.MustHaveEnumValue |
                    OptionFlags.Unsafe, Index.Invalid,
                    Index.Invalid, "-priority",
                    new Variant(EventPriority.Idle)),
                new Option(typeof(EventFlags),
                    OptionFlags.MustHaveEnumValue |
                    OptionFlags.Unsafe, Index.Invalid,
                    Index.Invalid, "-flags",
                    new Variant(EventFlags.None)),
                Option.CreateEndOfOptions()
            });
        }

        ///////////////////////////////////////////////////////////////////////

        //
        // NOTE: This is for the [after <milliseconds>] sub-command.
        //
        private static OptionDictionary GetAfterInfoOptions()
        {
            return new OptionDictionary(
                new IOption[] {
                new Option(null, OptionFlags.MustHaveWideIntegerValue |
                    OptionFlags.Unsafe, Index.Invalid, Index.Invalid,
                    "-thread", null),
                new Option(typeof(EventPriority),
                    OptionFlags.MustHaveEnumValue |
                    OptionFlags.Unsafe, Index.Invalid,
                    Index.Invalid, "-priority",
                    new Variant(EventPriority.After)),
                new Option(typeof(EventFlags),
                    OptionFlags.MustHaveEnumValue |
                    OptionFlags.Unsafe, Index.Invalid,
                    Index.Invalid, "-flags",
                    new Variant(EventFlags.None)),
                Option.CreateEndOfOptions()
            });
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region [array] Command Options
        //
        // NOTE: This is for the [array copy] sub-command.
        //
        private static OptionDictionary GetArrayCopyOptions()
        {
            return new OptionDictionary(
                new IOption[] {
                new Option(null, OptionFlags.None, Index.Invalid,
                    Index.Invalid, "-deep", null),
                new Option(null, OptionFlags.None, Index.Invalid,
                    Index.Invalid, "-nosignal", null),
                Option.CreateEndOfOptions()
            });
        }

        ///////////////////////////////////////////////////////////////////////

        //
        // NOTE: This is for the [array random] sub-command.
        //
        private static OptionDictionary GetArrayRandomOptions()
        {
            return new OptionDictionary(
                new IOption[] {
                new Option(null, OptionFlags.None, Index.Invalid,
                    Index.Invalid, "-strict", null),
                new Option(null, OptionFlags.None, Index.Invalid,
                    Index.Invalid, "-pair", null),
                new Option(null, OptionFlags.None, Index.Invalid,
                    Index.Invalid, "-valueonly", null),
                new Option(null, OptionFlags.None, Index.Invalid,
                    Index.Invalid, "-matchname", null),
                new Option(null, OptionFlags.None, Index.Invalid,
                    Index.Invalid, "-matchvalue", null),
                Option.CreateEndOfOptions()
            });
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region [base64] Command Options
        //
        // NOTE: This is for the [base64 decode] sub-command.
        //
        private static OptionDictionary GetBase64DecodeOptions()
        {
            return new OptionDictionary(
                new IOption[] {
                new Option(null, OptionFlags.MustHaveEncodingValue,
                    Index.Invalid, Index.Invalid, "-encoding",
                    null),
                Option.CreateEndOfOptions()
            });
        }

        ///////////////////////////////////////////////////////////////////////

        //
        // NOTE: This is for the [base64 encode] sub-command.
        //
        private static OptionDictionary GetBase64EncodeOptions()
        {
            return new OptionDictionary(
                new IOption[] {
                new Option(null, OptionFlags.MustHaveEncodingValue,
                    Index.Invalid, Index.Invalid, "-encoding",
                    null),
                Option.CreateEndOfOptions()
            });
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region [clock] Command Options
        //
        // NOTE: This is for the [clock days] sub-command.
        //
        private static OptionDictionary GetClockDaysOptions()
        {
            return new OptionDictionary(
                new IOption[] {
                new Option(null, OptionFlags.MustHaveValue,
                    Index.Invalid, Index.Invalid, "-format", null),
                new Option(null, OptionFlags.MustHaveDateTimeValue,
                    Index.Invalid, Index.Invalid, "-epoch", null),
                new Option(null, OptionFlags.MustHaveBooleanValue,
                    Index.Invalid, Index.Invalid, "-gmt", null),
                Option.CreateEndOfOptions()
            });
        }

        ///////////////////////////////////////////////////////////////////////

        //
        // NOTE: This is for the [clock clicks] sub-command.
        //
        private static OptionDictionary GetClockClicksOptions()
        {
            return new OptionDictionary(
                new IOption[] {
                new Option(null, OptionFlags.None, Index.Invalid,
                    Index.Invalid, "-milliseconds", null),
                new Option(null, OptionFlags.None, Index.Invalid,
                    Index.Invalid, "-microseconds", null)
            });
        }

        ///////////////////////////////////////////////////////////////////////

        //
        // NOTE: This is for the [clock duration] sub-command.
        //
        private static OptionDictionary GetClockDurationOptions()
        {
            return new OptionDictionary(
                new IOption[] {
                new Option(typeof(DurationFlags),
                    OptionFlags.MustHaveEnumValue, Index.Invalid,
                    Index.Invalid, "-flags",
                    new Variant(DurationFlags.Default)),
                Option.CreateEndOfOptions()
            });
        }

        ///////////////////////////////////////////////////////////////////////

        //
        // NOTE: This is for the [clock filetime] sub-command.
        //
        private static OptionDictionary GetClockFileTimeOptions()
        {
            return new OptionDictionary(
                new IOption[] {
                new Option(null, OptionFlags.MustHaveValue,
                    Index.Invalid, Index.Invalid, "-format", null),
                new Option(null, OptionFlags.MustHaveDateTimeValue,
                    Index.Invalid, Index.Invalid, "-epoch", null),
                new Option(null, OptionFlags.MustHaveBooleanValue,
                    Index.Invalid, Index.Invalid, "-gmt", null)
            });
        }

        ///////////////////////////////////////////////////////////////////////

        //
        // NOTE: This is for the [clock format] sub-command.
        //
        private static OptionDictionary GetClockFormatOptions()
        {
            return new OptionDictionary(
                new IOption[] {
                new Option(null, OptionFlags.MustHaveValue,
                    Index.Invalid, Index.Invalid, "-format", null),
                new Option(typeof(DateTimeKind),
                    OptionFlags.MustHaveEnumValue, Index.Invalid,
                    Index.Invalid, "-kind", null),
                new Option(null, OptionFlags.None, Index.Invalid,
                    Index.Invalid, "-ticks", null),
                new Option(null, OptionFlags.MustHaveDateTimeValue,
                    Index.Invalid, Index.Invalid, "-epoch", null),
                new Option(null, OptionFlags.MustHaveBooleanValue,
                    Index.Invalid, Index.Invalid, "-gmt", null),
                new Option(null, OptionFlags.None, Index.Invalid,
                    Index.Invalid, "-iso", null),
                new Option(null, OptionFlags.None, Index.Invalid,
                    Index.Invalid, "-full", null),
                new Option(null, OptionFlags.None, Index.Invalid,
                    Index.Invalid, "-isotimezone", null)
            });
        }

        ///////////////////////////////////////////////////////////////////////

        //
        // NOTE: This is for the [clock now] sub-command.
        //
        private static OptionDictionary GetClockNowOptions()
        {
            return new OptionDictionary(
                new IOption[] {
                new Option(null, OptionFlags.MustHaveBooleanValue,
                    Index.Invalid, Index.Invalid, "-gmt", null)
            });
        }

        ///////////////////////////////////////////////////////////////////////

        //
        // NOTE: This is for the [clock scan] sub-command.
        //
        private static OptionDictionary GetClockScanOptions()
        {
            return new OptionDictionary(
                new IOption[] {
                new Option(null, OptionFlags.MustHaveValue,
                    Index.Invalid, Index.Invalid, "-format", null),
                new Option(null, OptionFlags.MustHaveWideIntegerValue,
                    Index.Invalid, Index.Invalid, "-base", null),
                new Option(null, OptionFlags.MustHaveDateTimeValue,
                    Index.Invalid, Index.Invalid, "-epoch", null),
                new Option(null, OptionFlags.MustHaveBooleanValue,
                    Index.Invalid, Index.Invalid, "-gmt", null)
            });
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region [debug] Command Options
#if DEBUGGER
        private static OptionDictionary GetDebugBreakOptions()
        {
            return new OptionDictionary(
                new IOption[] {
                new Option(null,
                    OptionFlags.MustHaveInterpreterValue,
                    Index.Invalid, Index.Invalid, "-interpreter",
                    null),
                new Option(null, OptionFlags.None, Index.Invalid,
                    Index.Invalid, "-ignoreenabled", null),
                new Option(null, OptionFlags.None, Index.Invalid,
                    Index.Invalid, "-complain", null),
                new Option(null, OptionFlags.None, Index.Invalid,
                    Index.Invalid, "-nocomplain", null),
                new Option(null, OptionFlags.None, Index.Invalid,
                    Index.Invalid, "-noerror", null),
                Option.CreateEndOfOptions()
            });
        }

        ///////////////////////////////////////////////////////////////////////

        private static OptionDictionary GetDebugEmergencyOptions()
        {
            return new OptionDictionary(
                new IOption[] {
                new Option(null,
                    OptionFlags.MustHaveInterpreterValue,
                    Index.Invalid, Index.Invalid, "-interpreter",
                    null),
                new Option(null, OptionFlags.None, Index.Invalid,
                    Index.Invalid, "-ignoreenabled", null),
                new Option(null, OptionFlags.None, Index.Invalid,
                    Index.Invalid, "-nocomplain", null),
                new Option(null, OptionFlags.None, Index.Invalid,
                    Index.Invalid, "-noerror", null),
                Option.CreateEndOfOptions()
            });
        }

        ///////////////////////////////////////////////////////////////////////

#if TEST
        private static OptionDictionary GetDebugHookOptions()
        {
            return new OptionDictionary(
                new IOption[] {
                new Option(typeof(TestHookType),
                    OptionFlags.MustHaveEnumValue, Index.Invalid,
                    Index.Invalid, "-type",
                    new Variant(TestHookType.Default)),
                new Option(null, OptionFlags.MustHaveBooleanValue,
                    Index.Invalid, Index.Invalid, "-unset", null),
                Option.CreateEndOfOptions()
            });
        }
#endif

        ///////////////////////////////////////////////////////////////////////

        private static OptionDictionary GetDebugIqueueOptions()
        {
            return new OptionDictionary(
                new IOption[] {
                new Option(null, OptionFlags.None, Index.Invalid,
                    Index.Invalid, "-dump", null),
                new Option(null, OptionFlags.None, Index.Invalid,
                    Index.Invalid, "-clear", null),
                Option.CreateEndOfOptions()
            });
        }

        ///////////////////////////////////////////////////////////////////////

        private static OptionDictionary GetDebugLogOptions()
        {
            return new OptionDictionary(
                new IOption[] {
                new Option(null, OptionFlags.MustHaveIntegerValue,
                    Index.Invalid, Index.Invalid, "-level", null),
                new Option(null, OptionFlags.MustHaveValue,
                    Index.Invalid, Index.Invalid, "-category", null),
                Option.CreateEndOfOptions()
            });
        }

        ///////////////////////////////////////////////////////////////////////

        private static OptionDictionary GetDebugSecureEvalOptions()
        {
            return new OptionDictionary(
                new IOption[] {
                new Option(null, OptionFlags.MustHaveIntegerValue,
                    Index.Invalid, Index.Invalid, "-timeout", null),
                new Option(null, OptionFlags.MustHaveBooleanValue,
                    Index.Invalid, Index.Invalid, "-nocancel", null),
                new Option(null, OptionFlags.MustHaveBooleanValue,
                    Index.Invalid, Index.Invalid, "-globalcancel",
                    null),
                new Option(null, OptionFlags.MustHaveBooleanValue,
                    Index.Invalid, Index.Invalid, "-stoponerror",
                    null),
                new Option(null, OptionFlags.MustHaveBooleanValue,
                    Index.Invalid, Index.Invalid, "-file", null),
                new Option(null, OptionFlags.MustHaveBooleanValue,
                    Index.Invalid, Index.Invalid, "-trusted", null),
                new Option(null, OptionFlags.MustHaveBooleanValue,
                    Index.Invalid, Index.Invalid, "-events", null),
#if ISOLATED_PLUGINS
                new Option(null, OptionFlags.MustHaveBooleanValue,
                    Index.Invalid, Index.Invalid,
                    "-noisolatedplugins", null),
#else
                new Option(null, OptionFlags.MustHaveBooleanValue |
                    OptionFlags.Unsupported, Index.Invalid,
                    Index.Invalid, "-noisolatedplugins", null),
#endif
                Option.CreateEndOfOptions()
            });
        }

        ///////////////////////////////////////////////////////////////////////

        private static OptionDictionary GetDebugSetOptions()
        {
            return new OptionDictionary(
                new IOption[] {
                new Option(null, OptionFlags.MustHaveIntegerValue,
                    Index.Invalid, Index.Invalid, "-reference",
                    null),
                new Option(null, OptionFlags.MustHaveBooleanValue,
                    Index.Invalid, Index.Invalid, "-convert", null),
                Option.CreateEndOfOptions()
            });
        }

        ///////////////////////////////////////////////////////////////////////

#if SHELL
        private static OptionDictionary GetDebugShellOptions()
        {
            return new OptionDictionary(
                new IOption[] {
                new Option(null,
                    OptionFlags.MustHaveInterpreterValue,
                    Index.Invalid, Index.Invalid, "-interpreter",
                    null),
                new Option(null, OptionFlags.MustHaveBooleanValue,
                    Index.Invalid, Index.Invalid, "-initialize",
                    null),
                new Option(null, OptionFlags.MustHaveBooleanValue,
                    Index.Invalid, Index.Invalid, "-loop", null),
                new Option(null, OptionFlags.MustHaveBooleanValue,
                    Index.Invalid, Index.Invalid, "-asynchronous",
                    null),
                Option.CreateEndOfOptions()
            });
        }
#endif

        ///////////////////////////////////////////////////////////////////////

        private static OptionDictionary GetDebugSubstOptions()
        {
            return new OptionDictionary(
                new IOption[] {
                new Option(null, OptionFlags.None, Index.Invalid,
                    Index.Invalid, "-nobackslashes", null),
                new Option(null, OptionFlags.None, Index.Invalid,
                    Index.Invalid, "-nocommands", null),
                new Option(null, OptionFlags.None, Index.Invalid,
                    Index.Invalid, "-novariables", null),
                Option.CreateEndOfOptions()
            });
        }

        ///////////////////////////////////////////////////////////////////////

        private static OptionDictionary GetDebugTraceOptions()
        {
            return new OptionDictionary(
                new IOption[] {
                new Option(null, OptionFlags.MustHaveBooleanValue,
                    Index.Invalid, Index.Invalid, "-noresult", null),
                new Option(null, OptionFlags.MustHaveBooleanValue,
                    Index.Invalid, Index.Invalid, "-default", null),
                new Option(null, OptionFlags.MustHaveBooleanValue,
                    Index.Invalid, Index.Invalid, "-console", null),
                new Option(null, OptionFlags.MustHaveBooleanValue,
                    Index.Invalid, Index.Invalid, "-native", null),
                new Option(null, OptionFlags.MustHaveBooleanValue,
                    Index.Invalid, Index.Invalid, "-statusform",
                    null),
                new Option(null, OptionFlags.MustHaveBooleanValue,
                    Index.Invalid, Index.Invalid, "-debug", null),
                new Option(null, OptionFlags.MustHaveBooleanValue,
                    Index.Invalid, Index.Invalid, "-raw", null),
                new Option(null, OptionFlags.MustHaveBooleanValue,
                    Index.Invalid, Index.Invalid, "-log", null),
                new Option(null, OptionFlags.MustHaveBooleanValue,
                    Index.Invalid, Index.Invalid, "-resetsystem",
                    null),
                new Option(null, OptionFlags.MustHaveBooleanValue,
                    Index.Invalid, Index.Invalid, "-resetlisteners",
                    null),
                new Option(null, OptionFlags.MustHaveBooleanValue,
                    Index.Invalid, Index.Invalid, "-forceenabled",
                    null),
                new Option(null, OptionFlags.MustHaveBooleanValue,
                    Index.Invalid, Index.Invalid,
                    "-overrideenvironment", null),
                new Option(null, OptionFlags.MustHaveListValue,
                    Index.Invalid, Index.Invalid,
                    "-enabledcategories", null),
                new Option(null, OptionFlags.MustHaveListValue,
                    Index.Invalid, Index.Invalid,
                    "-disabledcategories", null),
                new Option(null, OptionFlags.MustHaveListValue,
                    Index.Invalid, Index.Invalid,
                    "-penaltycategories", null),
                new Option(null, OptionFlags.MustHaveListValue,
                    Index.Invalid, Index.Invalid,
                    "-bonuscategories", null),
                new Option(typeof(TraceStateType),
                    OptionFlags.MustHaveEnumValue, Index.Invalid,
                    Index.Invalid, "-statetypes",
                    new Variant(TraceStateType.TraceCommand)),
                new Option(typeof(TracePriority),
                    OptionFlags.MustHaveEnumValue, Index.Invalid,
                    Index.Invalid, "-priority",
                    new Variant(TraceOps.GetTracePriority())),
                new Option(typeof(TracePriority),
                    OptionFlags.MustHaveEnumValue, Index.Invalid,
                    Index.Invalid, "-priorities",
                    new Variant(TraceOps.GetTracePriorities())),
                new Option(null, OptionFlags.MustHaveValue,
                    Index.Invalid, Index.Invalid, "-category", null),
#if TEST
                new Option(null, OptionFlags.MustHaveValue,
                    Index.Invalid, Index.Invalid, "-logname", null),
                new Option(null, OptionFlags.MustHaveValue,
                    Index.Invalid, Index.Invalid, "-logfilename",
                    null),
                new Option(typeof(LogFlags),
                    OptionFlags.MustHaveEnumValue, Index.Invalid,
                    Index.Invalid, "-logflags",
                    new Variant(LogFlags.Default)),
#else
                new Option(null, OptionFlags.MustHaveValue |
                    OptionFlags.Unsupported, Index.Invalid,
                    Index.Invalid, "-logname", null),
                new Option(null, OptionFlags.MustHaveValue |
                    OptionFlags.Unsupported, Index.Invalid,
                    Index.Invalid, "-logfilename", null),
                new Option(typeof(LogFlags),
                    OptionFlags.MustHaveEnumValue |
                    OptionFlags.Unsupported, Index.Invalid,
                    Index.Invalid, "-logflags",
                    new Variant(LogFlags.Default)),
#endif
                Option.CreateEndOfOptions()
            });
        }

        ///////////////////////////////////////////////////////////////////////

        private static OptionDictionary GetDebugVariableOptions()
        {
            return new OptionDictionary(
                new IOption[] {
                new Option(null, OptionFlags.None, Index.Invalid,
                    Index.Invalid, "-searches", null),
                new Option(null, OptionFlags.None, Index.Invalid,
                    Index.Invalid, "-elements", null),
                new Option(null, OptionFlags.None, Index.Invalid,
                    Index.Invalid, "-links", null),
                new Option(null, OptionFlags.None, Index.Invalid,
                    Index.Invalid, "-empty", null),
                Option.CreateEndOfOptions()
            });
        }
#endif
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region [debugger] Command Options
        //
        // NOTE: This is for the debugger "dsubst" command
        //       (from InteractiveOps).
        //
        private static OptionDictionary GetDebuggerDsubstOptions()
        {
            return new OptionDictionary(
                new IOption[] {
                new Option(null, OptionFlags.None, Index.Invalid,
                    Index.Invalid, "-nobackslashes", null),
                new Option(null, OptionFlags.None, Index.Invalid,
                    Index.Invalid, "-nocommands", null),
                new Option(null, OptionFlags.None, Index.Invalid,
                    Index.Invalid, "-novariables", null),
                Option.CreateEndOfOptions()
            });
        }

        ///////////////////////////////////////////////////////////////////////

        //
        // NOTE: This is for the debugger "overr" command
        //       (from InteractiveOps).
        //
        private static OptionDictionary GetDebuggerOverrOptions()
        {
            return new OptionDictionary(
                new IOption[] {
                new Option(null, OptionFlags.MustHaveReturnCodeValue,
                    Index.Invalid, Index.Invalid, "-code", null),
                new Option(null, OptionFlags.MustHaveValue,
                    Index.Invalid, Index.Invalid, "-result", null),
                Option.CreateEndOfOptions()
            });
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region [exit] Command Options
        //
        // NOTE: This is for the [exit] command.
        //
        private static OptionDictionary GetExitOptions()
        {
            return new OptionDictionary(
                new IOption[] {
                new Option(null, OptionFlags.MustHaveValue |
                    OptionFlags.Unsafe, Index.Invalid,
                    Index.Invalid, "-message", null),
                new Option(
                    null, OptionFlags.Unsafe, Index.Invalid,
                     Index.Invalid, "-force", null),
                new Option(
                    null, OptionFlags.Unsafe, Index.Invalid,
                     Index.Invalid, "-fail", null),
                new Option(
                    null, OptionFlags.Unsafe, Index.Invalid,
                     Index.Invalid, "-nodispose", null),
                new Option(
                    null, OptionFlags.Unsafe, Index.Invalid,
                     Index.Invalid, "-nocomplain", null),
                new Option(null, OptionFlags.Unsafe,
                    Index.Invalid, Index.Invalid, "-current",
                    null),
                Option.CreateEndOfOptions()
            });
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region [fconfigure] Command Options
        //
        // NOTE: This is for the [fconfigure] command (set mode).
        //
        private static OptionDictionary GetFconfigureSetOptions()
        {
            return new OptionDictionary(
                new IOption[] {
                new Option(null, OptionFlags.MustHaveBooleanValue,
                    Index.Invalid, Index.Invalid, "-blocking", null),
                new Option(null, OptionFlags.MustHaveBooleanValue,
                    Index.Invalid, Index.Invalid, "-buffer", null),
                new Option(null, OptionFlags.MustHaveEncodingValue,
                    Index.Invalid, Index.Invalid, "-encoding", null),
                new Option(null, OptionFlags.MustHaveListValue,
                    Index.Invalid, Index.Invalid, "-translation",
                    null)
            });
        }

        ///////////////////////////////////////////////////////////////////////

        //
        // NOTE: This is for the [fconfigure] command (query mode).
        //
        private static OptionDictionary GetFconfigureQueryOptions()
        {
            return new OptionDictionary(
                new IOption[] {
                new Option(null, OptionFlags.MustHaveBooleanValue,
                    Index.Invalid, Index.Invalid, "-blocking", null),
                new Option(null, OptionFlags.None, Index.Invalid,
                    Index.Invalid, "-encoding", null),
                new Option(null, OptionFlags.None, Index.Invalid,
                    Index.Invalid, "-translation", null)
            });
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region [fcopy] Command Options
        //
        // NOTE: This is for the [fcopy] command.
        //
        private static OptionDictionary GetFcopyOptions(
            Interpreter interpreter /* in */
            )
        {
            return new OptionDictionary(
                new IOption[] {
                new Option(null, OptionFlags.MustHaveIntegerValue,
                    Index.Invalid, Index.Invalid, "-size", null),
                new Option(null, OptionFlags.MustHaveValue |
                    OptionFlags.Unsupported, Index.Invalid,
                    Index.Invalid, "-command", null),
                new Option(typeof(EventFlags),
                    OptionFlags.MustHaveEnumValue, Index.Invalid,
                    Index.Invalid, "-eventflags",
                    new Variant((interpreter != null) ?
                        interpreter.EngineEventFlags :
                        EventFlags.None)),
                Option.CreateEndOfOptions()
            });
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region [file] Command Options
        private static OptionDictionary GetFileCleanupOptions()
        {
            return new OptionDictionary(
                new IOption[] {
                new Option(typeof(PathType),
                    OptionFlags.MustHaveEnumValue, Index.Invalid,
                    Index.Invalid, "-type",
                    new Variant(PathType.Cleanup)),
                new Option(null, OptionFlags.MustHaveValue,
                    Index.Invalid, Index.Invalid, "-pattern", null),
                new Option(null, OptionFlags.None, Index.Invalid,
                    Index.Invalid, "-nocase", null),
                new Option(null, OptionFlags.None, Index.Invalid,
                    Index.Invalid, "-recursive", null),
                new Option(null, OptionFlags.None, Index.Invalid,
                    Index.Invalid, "-force", null),
                new Option(null, OptionFlags.None, Index.Invalid,
                    Index.Invalid, "-nocomplain", null),
                new Option(null, OptionFlags.None, Index.Invalid,
                    Index.Invalid, "-now", null),
                Option.CreateEndOfOptions()
            });
        }

        ///////////////////////////////////////////////////////////////////////

        private static OptionDictionary GetFileCopyOptions()
        {
            return new OptionDictionary(
                new IOption[] {
                new Option(null, OptionFlags.None, Index.Invalid,
                    Index.Invalid, "-force", null),
                Option.CreateEndOfOptions()
            });
        }

        ///////////////////////////////////////////////////////////////////////

        private static OptionDictionary GetFileDeleteOptions()
        {
            return new OptionDictionary(
                new IOption[] {
                new Option(null, OptionFlags.None, Index.Invalid,
                    Index.Invalid, "-recursive", null),
                new Option(null, OptionFlags.None, Index.Invalid,
                    Index.Invalid, "-force", null),
                new Option(null, OptionFlags.None, Index.Invalid,
                    Index.Invalid, "-nocomplain", null),
                Option.CreateEndOfOptions()
            });
        }

        ///////////////////////////////////////////////////////////////////////

        private static OptionDictionary GetFileGlobOptions()
        {
            return new OptionDictionary(
                new IOption[] {
                new Option(null, OptionFlags.None, Index.Invalid,
                    Index.Invalid, "-nocomplain", null),
                new Option(null, OptionFlags.None, Index.Invalid,
                    Index.Invalid, "-noresolve", null),
                new Option(null, OptionFlags.None, Index.Invalid,
                    Index.Invalid, "-novalidate", null),
                new Option(null, OptionFlags.MustHaveMatchModeValue,
                    Index.Invalid, Index.Invalid, "-match",
                    new Variant(StringOps.DefaultMatchMode)),
                new Option(null, OptionFlags.None, Index.Invalid,
                    Index.Invalid, "-nocase", null),
                new Option(null, OptionFlags.MustHaveValue,
                    Index.Invalid, Index.Invalid, "-directory",
                    null),
                new Option(null, OptionFlags.MustHaveValue,
                    Index.Invalid, Index.Invalid, "-searchpattern",
                    null),
                Option.CreateEndOfOptions()
            });
        }

        ///////////////////////////////////////////////////////////////////////

        private static OptionDictionary GetFileInformationOptions()
        {
            return new OptionDictionary(
                new IOption[] {
                new Option(null, OptionFlags.MustHaveBooleanValue,
                    Index.Invalid, Index.Invalid, "-directory",
                    null),
                new Option(null, OptionFlags.MustHaveBooleanValue,
                    Index.Invalid, Index.Invalid, "-reparse", null),
                Option.CreateEndOfOptions()
            });
        }

        ///////////////////////////////////////////////////////////////////////

        private static OptionDictionary GetFileNormalizeOptions()
        {
            return new OptionDictionary(
                new IOption[] {
                new Option(null, OptionFlags.MustHaveBooleanValue,
                    Index.Invalid, Index.Invalid, "-legacy", null),
                Option.CreateEndOfOptions()
            });
        }

        ///////////////////////////////////////////////////////////////////////

        private static OptionDictionary GetFileObjectIdOptions()
        {
            return new OptionDictionary(
                new IOption[] {
                new Option(null, OptionFlags.MustHaveBooleanValue,
                    Index.Invalid, Index.Invalid, "-directory",
                    null),
                new Option(null, OptionFlags.MustHaveBooleanValue,
                    Index.Invalid, Index.Invalid, "-create", null),
                Option.CreateEndOfOptions()
            });
        }

        ///////////////////////////////////////////////////////////////////////

        private static OptionDictionary GetFileRenameOptions()
        {
            return new OptionDictionary(
                new IOption[] {
                new Option(null, OptionFlags.None, Index.Invalid,
                    Index.Invalid, "-force", null),
                Option.CreateEndOfOptions()
            });
        }

        ///////////////////////////////////////////////////////////////////////

#if !NET_STANDARD_20 && !MONO
        private static OptionDictionary GetFileSddlOptions()
        {
            return new OptionDictionary(
                new IOption[] {
                new Option(typeof(SddlFlags),
                    OptionFlags.MustHaveEnumValue, Index.Invalid,
                    Index.Invalid, "-flags",
                    new Variant(SddlFlags.Default)),
                Option.CreateEndOfOptions()
            });
        }
#endif

        ///////////////////////////////////////////////////////////////////////

        private static OptionDictionary GetFileUnderOptions()
        {
            return new OptionDictionary(
                new IOption[] {
                new Option(typeof(MatchMode),
                    OptionFlags.MustHaveEnumValue, Index.Invalid,
                    Index.Invalid, "-mode",
                    new Variant(MatchMode.None)),
                new Option(typeof(SearchOption),
                    OptionFlags.MustHaveEnumValue, Index.Invalid,
                    Index.Invalid, "-searchoption",
                    new Variant(SearchOption.AllDirectories)),
                new Option(typeof(PathType),
                    OptionFlags.MustHaveEnumValue, Index.Invalid,
                    Index.Invalid, "-pathtype",
                    new Variant(PathType.Under)),
                new Option(null, OptionFlags.None, Index.Invalid,
                    Index.Invalid, "-contains", null),
                new Option(null, OptionFlags.None, Index.Invalid,
                    Index.Invalid, "-failonerror", null),
                Option.CreateEndOfOptions()
            });
        }

        ///////////////////////////////////////////////////////////////////////

        private static OptionDictionary GetFileVersionOptions()
        {
            return new OptionDictionary(
                new IOption[] {
                new Option(null, OptionFlags.None, Index.Invalid,
                    Index.Invalid, "-full", null),
                new Option(null, OptionFlags.None, Index.Invalid,
                    Index.Invalid, "-fixed", null),
                Option.CreateEndOfOptions()
            });
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region [gets] Command Options
        //
        // NOTE: This is for the [gets] command.
        //
        private static OptionDictionary GetGetsOptions()
        {
            return new OptionDictionary(
                new IOption[] {
                new Option(null, OptionFlags.MustHaveEncodingValue |
                    OptionFlags.Unsafe, Index.Invalid, Index.Invalid,
                    "-encoding", null),
                new Option(null, OptionFlags.Unsafe, Index.Invalid,
                    Index.Invalid, "-usecount", null),
                new Option(null, OptionFlags.None, Index.Invalid,
                    Index.Invalid, "-noblock", null),
                new Option(null, OptionFlags.None |
                    OptionFlags.MustHaveBooleanValue, Index.Invalid,
                    Index.Invalid, "-keepeol", null),
                new Option(null, OptionFlags.None |
                    OptionFlags.MustHaveIntegerValue, Index.Invalid,
                    Index.Invalid, "-count", null),
                Option.CreateEndOfOptions()
            });
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region [glob] Command Options
        //
        // NOTE: This is for the [glob] command.
        //
        private static OptionDictionary GetGlobOptions()
        {
            return new OptionDictionary(
                new IOption[] {
                new Option(null, OptionFlags.MustHaveValue,
                    Index.Invalid, Index.Invalid, "-path", null),
                new Option(null, OptionFlags.MustHaveValue,
                    Index.Invalid, Index.Invalid, "-directory", null),
                new Option(null, OptionFlags.MustHaveListValue,
                    Index.Invalid, Index.Invalid, "-types", null),
                new Option(null, OptionFlags.None, Index.Invalid,
                    Index.Invalid, "-join", null),
                new Option(null, OptionFlags.None, Index.Invalid,
                    Index.Invalid, "-tails", null),
                new Option(null, OptionFlags.None, Index.Invalid,
                    Index.Invalid, "-nocomplain", null),
                new Option(null, OptionFlags.None, Index.Invalid,
                    Index.Invalid, "-noerror", null),
                Option.CreateEndOfOptions()
            });
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region [hash] Command Options
        //
        // NOTE: This is for the [hash keyed] sub-command.
        //
        private static OptionDictionary GetHashKeyedOptions()
        {
            return new OptionDictionary(
                new IOption[] {
                new Option(null, OptionFlags.None, Index.Invalid,
                    Index.Invalid, "-object", null),
                new Option(null, OptionFlags.None, Index.Invalid,
                    Index.Invalid, "-raw", null),
                new Option(null, OptionFlags.Unsafe, Index.Invalid,
                    Index.Invalid, "-filename", null),
                new Option(null, OptionFlags.MustHaveEncodingValue,
                    Index.Invalid, Index.Invalid, "-encoding",
                    null),
                Option.CreateEndOfOptions()
            });
        }

        ///////////////////////////////////////////////////////////////////////

        //
        // NOTE: This is for the [hash mac] sub-command.
        //
        private static OptionDictionary GetHashMacOptions()
        {
            return new OptionDictionary(
                new IOption[] {
                new Option(null, OptionFlags.None, Index.Invalid,
                    Index.Invalid, "-object", null),
                new Option(null, OptionFlags.None, Index.Invalid,
                    Index.Invalid, "-raw", null),
                new Option(null, OptionFlags.Unsafe, Index.Invalid,
                    Index.Invalid, "-filename", null),
                new Option(null, OptionFlags.MustHaveEncodingValue,
                    Index.Invalid, Index.Invalid, "-encoding",
                    null),
                Option.CreateEndOfOptions()
            });
        }

        ///////////////////////////////////////////////////////////////////////

        //
        // NOTE: This is for the [hash normal] sub-command.
        //
        private static OptionDictionary GetHashNormalOptions()
        {
            return new OptionDictionary(
                new IOption[] {
                new Option(null, OptionFlags.None, Index.Invalid,
                    Index.Invalid, "-object", null),
                new Option(null, OptionFlags.None, Index.Invalid,
                    Index.Invalid, "-raw", null),
                new Option(null, OptionFlags.Unsafe, Index.Invalid,
                    Index.Invalid, "-filename", null),
                new Option(null, OptionFlags.MustHaveEncodingValue,
                    Index.Invalid, Index.Invalid, "-encoding",
                    null),
                Option.CreateEndOfOptions()
            });
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region [host] Command Options
        private static OptionDictionary GetHostBeepOptions()
        {
            return new OptionDictionary(
                new IOption[] {
                new Option(null, OptionFlags.MustHaveIntegerValue,
                    Index.Invalid, Index.Invalid, "-frequency",
                    null),
                new Option(null, OptionFlags.MustHaveIntegerValue,
                    Index.Invalid, Index.Invalid, "-duration",
                    null),
                Option.CreateEndOfOptions()
            });
        }

        ///////////////////////////////////////////////////////////////////////

        private static OptionDictionary GetHostColorOptions()
        {
            return new OptionDictionary(
                new IOption[] {
                new Option(typeof(ConsoleColor),
                    OptionFlags.MustHaveEnumValue, Index.Invalid,
                    Index.Invalid, "-bg", null),
                new Option(typeof(ConsoleColor),
                    OptionFlags.MustHaveEnumValue, Index.Invalid,
                    Index.Invalid, "-background", null),
                new Option(typeof(ConsoleColor),
                    OptionFlags.MustHaveEnumValue, Index.Invalid,
                    Index.Invalid, "-fg", null),
                new Option(typeof(ConsoleColor),
                    OptionFlags.MustHaveEnumValue, Index.Invalid,
                    Index.Invalid, "-foreground", null),
                Option.CreateEndOfOptions()
            });
        }

        ///////////////////////////////////////////////////////////////////////

#if CONSOLE && NATIVE && WINDOWS
        private static OptionDictionary GetHostFontOptions()
        {
            return new OptionDictionary(
                new IOption[] {
                new Option(null, OptionFlags.MustHaveValue,
                    Index.Invalid, Index.Invalid, "-facename",
                    null),
                new Option(null,
                    OptionFlags.MustHaveNarrowIntegerValue,
                    Index.Invalid, Index.Invalid, "-fontsize",
                    null),
                new Option(null, OptionFlags.MustHaveBooleanValue,
                    Index.Invalid, Index.Invalid, "-save", null),
                new Option(null, OptionFlags.MustHaveBooleanValue,
                    Index.Invalid, Index.Invalid, "-restore", null),
                Option.CreateEndOfOptions()
            });
        }
#endif

        ///////////////////////////////////////////////////////////////////////

        private static OptionDictionary GetHostNamedColorOptions()
        {
            return new OptionDictionary(
                new IOption[] {
                new Option(null, OptionFlags.MustHaveValue,
                    Index.Invalid, Index.Invalid, "-theme", null),
                new Option(null, OptionFlags.MustHaveValue,
                    Index.Invalid, Index.Invalid, "-name", null),
                new Option(typeof(ConsoleColor),
                    OptionFlags.MustHaveEnumValue, Index.Invalid,
                    Index.Invalid, "-bg", null),
                new Option(typeof(ConsoleColor),
                    OptionFlags.MustHaveEnumValue, Index.Invalid,
                    Index.Invalid, "-background", null),
                new Option(typeof(ConsoleColor),
                    OptionFlags.MustHaveEnumValue, Index.Invalid,
                    Index.Invalid, "-fg", null),
                new Option(typeof(ConsoleColor),
                    OptionFlags.MustHaveEnumValue, Index.Invalid,
                    Index.Invalid, "-foreground", null),
                Option.CreateEndOfOptions()
            });
        }

        ///////////////////////////////////////////////////////////////////////

        private static OptionDictionary GetHostPositionOptions()
        {
            return new OptionDictionary(
                new IOption[] {
                new Option(null, OptionFlags.MustHaveIntegerValue,
                    Index.Invalid, Index.Invalid, "-x", null),
                new Option(null, OptionFlags.MustHaveIntegerValue,
                    Index.Invalid, Index.Invalid, "-relx", null),
                new Option(null, OptionFlags.MustHaveIntegerValue,
                    Index.Invalid, Index.Invalid, "-y", null),
                new Option(null, OptionFlags.MustHaveIntegerValue,
                    Index.Invalid, Index.Invalid, "-rely", null),
                Option.CreateEndOfOptions()
            });
        }

        ///////////////////////////////////////////////////////////////////////

        private static OptionDictionary GetHostResetOptions()
        {
            return new OptionDictionary(
                new IOption[] {
                new Option(typeof(HostSizeType),
                    OptionFlags.MustHaveEnumValue, Index.Invalid,
                    Index.Invalid, "-sizetype", null),
                new Option(null, OptionFlags.None, Index.Invalid,
                    Index.Invalid, "-all", null),
                new Option(null, OptionFlags.None, Index.Invalid,
                    Index.Invalid, "-channels", null),
                new Option(null, OptionFlags.None, Index.Invalid,
                    Index.Invalid, "-flags", null),
                new Option(null, OptionFlags.None, Index.Invalid,
                    Index.Invalid, "-history", null),
                new Option(null, OptionFlags.None, Index.Invalid,
                    Index.Invalid, "-interface", null),
                new Option(null, OptionFlags.None, Index.Invalid,
                    Index.Invalid, "-input", null),
                new Option(null, OptionFlags.None, Index.Invalid,
                    Index.Invalid, "-output", null),
                new Option(null, OptionFlags.None, Index.Invalid,
                    Index.Invalid, "-error", null),
                new Option(null, OptionFlags.None, Index.Invalid,
                    Index.Invalid, "-size", null),
                new Option(null, OptionFlags.None, Index.Invalid,
                    Index.Invalid, "-position", null),
                new Option(null, OptionFlags.None, Index.Invalid,
                    Index.Invalid, "-colors", null),
                Option.CreateEndOfOptions()
            });
        }

        ///////////////////////////////////////////////////////////////////////

        private static OptionDictionary GetHostSizeOptions()
        {
            return new OptionDictionary(
                new IOption[] {
                new Option(typeof(HostSizeType),
                    OptionFlags.MustHaveEnumValue, Index.Invalid,
                    Index.Invalid, "-sizetype", null),
                new Option(null, OptionFlags.None, Index.Invalid,
                    Index.Invalid, "-norestore", null),
                new Option(null, OptionFlags.MustHaveIntegerValue,
                    Index.Invalid, Index.Invalid, "-width", null),
                new Option(null, OptionFlags.MustHaveIntegerValue,
                    Index.Invalid, Index.Invalid, "-relwidth",
                    null),
                new Option(null, OptionFlags.MustHaveIntegerValue,
                    Index.Invalid, Index.Invalid, "-height", null),
                new Option(null, OptionFlags.MustHaveIntegerValue,
                    Index.Invalid, Index.Invalid, "-relheight",
                    null),
                Option.CreateEndOfOptions()
            });
        }

        ///////////////////////////////////////////////////////////////////////

        private static OptionDictionary GetHostWriteBoxOptions()
        {
            return new OptionDictionary(
                new IOption[] {
                new Option(null, OptionFlags.MustHaveValue,
                    Index.Invalid, Index.Invalid, "-theme", null),
                new Option(null, OptionFlags.MustHaveValue,
                    Index.Invalid, Index.Invalid, "-name", null),
                new Option(null, OptionFlags.MustHaveIntegerValue,
                    Index.Invalid, Index.Invalid, "-x", null),
                new Option(null, OptionFlags.MustHaveIntegerValue,
                    Index.Invalid, Index.Invalid, "-relx", null),
                new Option(null, OptionFlags.MustHaveIntegerValue,
                    Index.Invalid, Index.Invalid, "-y", null),
                new Option(null, OptionFlags.MustHaveIntegerValue,
                    Index.Invalid, Index.Invalid, "-rely", null),
                new Option(typeof(ConsoleColor),
                    OptionFlags.MustHaveEnumValue, Index.Invalid,
                    Index.Invalid, "-bg", null),
                new Option(typeof(ConsoleColor),
                    OptionFlags.MustHaveEnumValue, Index.Invalid,
                    Index.Invalid, "-background", null),
                new Option(typeof(ConsoleColor),
                    OptionFlags.MustHaveEnumValue, Index.Invalid,
                    Index.Invalid, "-fg", null),
                new Option(typeof(ConsoleColor),
                    OptionFlags.MustHaveEnumValue, Index.Invalid,
                    Index.Invalid, "-foreground", null),
                new Option(typeof(ConsoleColor),
                    OptionFlags.MustHaveEnumValue, Index.Invalid,
                    Index.Invalid, "-boxbg", null),
                new Option(typeof(ConsoleColor),
                    OptionFlags.MustHaveEnumValue, Index.Invalid,
                    Index.Invalid, "-boxbackground", null),
                new Option(typeof(ConsoleColor),
                    OptionFlags.MustHaveEnumValue, Index.Invalid,
                    Index.Invalid, "-boxfg", null),
                new Option(typeof(ConsoleColor),
                    OptionFlags.MustHaveEnumValue, Index.Invalid,
                    Index.Invalid, "-boxforeground", null),
                new Option(null, OptionFlags.None, Index.Invalid,
                    Index.Invalid, "-nohandle", null),
                new Option(null, OptionFlags.None, Index.Invalid,
                    Index.Invalid, "-multiple", null),
                new Option(null, OptionFlags.None, Index.Invalid,
                    Index.Invalid, "-noposition", null),
                new Option(null, OptionFlags.None, Index.Invalid,
                    Index.Invalid, "-noboxcolors", null),
                new Option(null, OptionFlags.None, Index.Invalid,
                    Index.Invalid, "-nocolors", null),
                new Option(null, OptionFlags.None, Index.Invalid,
                    Index.Invalid, "-pairs", null),
                new Option(null, OptionFlags.None, Index.Invalid,
                    Index.Invalid, "-newline", null),
                new Option(null, OptionFlags.None, Index.Invalid,
                    Index.Invalid, "-separator", null),
                new Option(null, OptionFlags.None, Index.Invalid,
                    Index.Invalid, "-norestore", null),
                Option.CreateEndOfOptions()
            });
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region [info] Command Options
        private static OptionDictionary GetInfoCommandsOptions()
        {
            return new OptionDictionary(
                new IOption[] {
                new Option(null, OptionFlags.MustHaveInterpreterValue |
                    OptionFlags.Unsafe, Index.Invalid, Index.Invalid,
                    "-interpreter", null),
                new Option(typeof(SdkType),
                    OptionFlags.MustHaveEnumValue, Index.Invalid,
                    Index.Invalid, "-sdk",
                    new Variant(SdkType.Default)),
                new Option(null, OptionFlags.None, Index.Invalid,
                    Index.Invalid, "-breakpoint", null),
                new Option(null, OptionFlags.None, Index.Invalid,
                    Index.Invalid, "-core", null),
                new Option(null, OptionFlags.None, Index.Invalid,
                    Index.Invalid, "-library", null),
                new Option(null, OptionFlags.None, Index.Invalid,
                    Index.Invalid, "-nocore", null),
                new Option(null, OptionFlags.None, Index.Invalid,
                    Index.Invalid, "-nolibrary", null),
                new Option(null, OptionFlags.None, Index.Invalid,
                    Index.Invalid, "-interactive", null),
                new Option(null, OptionFlags.None, Index.Invalid,
                    Index.Invalid, "-nocommands", null),
                new Option(null, OptionFlags.None, Index.Invalid,
                    Index.Invalid, "-noprocedures", null),
                new Option(null, OptionFlags.None, Index.Invalid,
                    Index.Invalid, "-noexecutes", null),
                new Option(null, OptionFlags.None, Index.Invalid,
                    Index.Invalid, "-noaliases", null),
                new Option(null, OptionFlags.None, Index.Invalid,
                    Index.Invalid, "-safe", null),
                new Option(null, OptionFlags.None, Index.Invalid,
                    Index.Invalid, "-unsafe", null),
                new Option(null, OptionFlags.None, Index.Invalid,
                    Index.Invalid, "-standard", null),
                new Option(null, OptionFlags.None, Index.Invalid,
                    Index.Invalid, "-nonstandard", null),
                new Option(null, OptionFlags.None, Index.Invalid,
                    Index.Invalid, "-hidden", null),
                new Option(null, OptionFlags.None, Index.Invalid,
                    Index.Invalid, "-hiddenonly", null),
                new Option(null, OptionFlags.None, Index.Invalid,
                    Index.Invalid, "-strict", null),
                Option.CreateEndOfOptions()
            });
        }

        ///////////////////////////////////////////////////////////////////////

        private static OptionDictionary GetInfoFunctionsOptions()
        {
            return new OptionDictionary(
                new IOption[] {
                new Option(null, OptionFlags.MustHaveInterpreterValue |
                    OptionFlags.Unsafe, Index.Invalid, Index.Invalid,
                    "-interpreter", null),
                new Option(null, OptionFlags.None, Index.Invalid,
                    Index.Invalid, "-safe", null),
                new Option(null, OptionFlags.None, Index.Invalid,
                    Index.Invalid, "-unsafe", null),
                new Option(null, OptionFlags.None, Index.Invalid,
                    Index.Invalid, "-standard", null),
                new Option(null, OptionFlags.None, Index.Invalid,
                    Index.Invalid, "-nonstandard", null),
                new Option(null, OptionFlags.None, Index.Invalid,
                    Index.Invalid, "-hidden", null),
                Option.CreateEndOfOptions()
            });
        }

        ///////////////////////////////////////////////////////////////////////

        private static OptionDictionary GetInfoLoadedOptions()
        {
            return new OptionDictionary(
                new IOption[] {
                new Option(null, OptionFlags.None, Index.Invalid,
                    Index.Invalid, "-nocore", null),
                Option.CreateEndOfOptions()
            });
        }

        ///////////////////////////////////////////////////////////////////////

        private static OptionDictionary GetInfoOperatorsOptions()
        {
            return new OptionDictionary(
                new IOption[] {
                new Option(null, OptionFlags.MustHaveInterpreterValue |
                    OptionFlags.Unsafe, Index.Invalid, Index.Invalid,
                    "-interpreter", null),
                new Option(null, OptionFlags.None, Index.Invalid,
                    Index.Invalid, "-standard", null),
                new Option(null, OptionFlags.None, Index.Invalid,
                    Index.Invalid, "-nonstandard", null),
                new Option(null, OptionFlags.None, Index.Invalid,
                    Index.Invalid, "-hidden", null),
                Option.CreateEndOfOptions()
            });
        }

        ///////////////////////////////////////////////////////////////////////

        private static OptionDictionary GetInfoSubCommandsOptions()
        {
            return new OptionDictionary(
                new IOption[] {
                new Option(null, OptionFlags.Unsafe |
                    OptionFlags.MustHaveBooleanValue, Index.Invalid,
                    Index.Invalid, "-hidden", null),
                Option.CreateEndOfOptions()
            });
        }

        ///////////////////////////////////////////////////////////////////////

        private static OptionDictionary GetInfoVarsOptions()
        {
            return new OptionDictionary(
                new IOption[] {
                new Option(null, OptionFlags.MustHaveInterpreterValue |
                    OptionFlags.Unsafe, Index.Invalid, Index.Invalid,
                    "-interpreter", null),
                Option.CreateEndOfOptions()
            });
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region [interp] Command Options
        //
        // NOTE: This is for the [interp addcommands] sub-command.
        //
        private static OptionDictionary GetInterpAddCommandsOptions()
        {
            return new OptionDictionary(
                new IOption[] {
                new Option(typeof(CreateFlags),
                    OptionFlags.MustHaveEnumValue, Index.Invalid,
                    Index.Invalid, "-createflags", null),
                new Option(typeof(InterpreterFlags),
                    OptionFlags.MustHaveEnumValue, Index.Invalid,
                    Index.Invalid, "-interpreterflags", null),
                new Option(null,
                    OptionFlags.MustHaveRuleSetValue |
                    OptionFlags.CouldBePath |
                    OptionFlags.Unsafe, Index.Invalid,
                    Index.Invalid, "-ruleset", null),
                new Option(null, OptionFlags.Unsafe, Index.Invalid,
                    Index.Invalid, "-safetyoverride", null),
                new Option(null, OptionFlags.Unsafe, Index.Invalid,
                    Index.Invalid, "-repopulate", null),
                Option.CreateEndOfOptions()
            });
        }

        ///////////////////////////////////////////////////////////////////////

        //
        // NOTE: This is for the [interp cancel] sub-command.
        //
        private static OptionDictionary GetInterpCancelOptions()
        {
            return new OptionDictionary(
                new IOption[] {
                new Option(null, OptionFlags.None, Index.Invalid,
                    Index.Invalid, "-global", null),
                new Option(null, OptionFlags.None, Index.Invalid,
                    Index.Invalid, "-nolocal", null),
                new Option(null, OptionFlags.None, Index.Invalid,
                    Index.Invalid, "-unwind", null),
                Option.CreateEndOfOptions()
            });
        }

        ///////////////////////////////////////////////////////////////////////

        //
        // NOTE: This is for the [interp create] sub-command.
        //
        private static OptionDictionary GetInterpCreateOptions()
        {
            return new OptionDictionary(
                new IOption[] {
                new Option(typeof(CreationFlagTypes),
                    OptionFlags.MustHaveEnumValue, Index.Invalid,
                    Index.Invalid, "-creationflagtypes", null),
                new Option(typeof(PeerType),
                    OptionFlags.MustHaveEnumValue, Index.Invalid,
                    Index.Invalid, "-peer",
                    new Variant(PeerType.Default)),
                new Option(null,
                    OptionFlags.MustHaveRuleSetValue |
                    OptionFlags.CouldBePath |
                    OptionFlags.Unsafe, Index.Invalid,
                    Index.Invalid, "-ruleset", null),
                new Option(null, OptionFlags.None, Index.Invalid,
                    Index.Invalid, "-namespaces", null),
                new Option(null, OptionFlags.None, Index.Invalid,
                    Index.Invalid, "-nocommands", null),
                new Option(null, OptionFlags.None, Index.Invalid,
                    Index.Invalid, "-nofunctions", null),
                new Option(null, OptionFlags.None, Index.Invalid,
                    Index.Invalid, "-nonamespaces", null),
                new Option(null, OptionFlags.None, Index.Invalid,
                    Index.Invalid, "-novariables", null),
                new Option(null, OptionFlags.None, Index.Invalid,
                    Index.Invalid, "-noloader", null),
                new Option(null, OptionFlags.None, Index.Invalid,
                    Index.Invalid, "-noinitialize", null),
                new Option(null, OptionFlags.None, Index.Invalid,
                    Index.Invalid, "-alias", null),
                new Option(null, OptionFlags.None, Index.Invalid,
                    Index.Invalid, "-safe", null),
#if DEBUG
                new Option(typeof(SdkType),
                    OptionFlags.MustHaveEnumValue, Index.Invalid,
                    Index.Invalid, "-sdk", null),
#endif
                new Option(null, OptionFlags.None, Index.Invalid,
                    Index.Invalid, "-nohidden", null),
                new Option(null, OptionFlags.None, Index.Invalid,
                    Index.Invalid, "-standard", null),
                new Option(null, OptionFlags.None, Index.Invalid,
                    Index.Invalid, "-unsafeinitialize", null),
#if APPDOMAINS && ISOLATED_INTERPRETERS
                new Option(null, OptionFlags.None, Index.Invalid,
                    Index.Invalid, "-isolated", null),
#endif
#if DEBUGGER
                new Option(null, OptionFlags.None, Index.Invalid,
                    Index.Invalid, "-debug", null),
#endif
#if TEST_PLUGIN || DEBUG
                new Option(null, OptionFlags.None, Index.Invalid,
                    Index.Invalid, "-test", null),
#endif
#if NOTIFY && NOTIFY_ARGUMENTS
                new Option(null, OptionFlags.None, Index.Invalid,
                    Index.Invalid, "-monitor", null),
#endif
#if APPDOMAINS || ISOLATED_INTERPRETERS || ISOLATED_PLUGINS
                new Option(null, OptionFlags.None, Index.Invalid,
                    Index.Invalid, "-probing", null),
                new Option(null, OptionFlags.None, Index.Invalid,
                    Index.Invalid, "-noprobing", null),
#endif
                new Option(null, OptionFlags.None, Index.Invalid,
                    Index.Invalid, "-security", null),
                new Option(null, OptionFlags.None, Index.Invalid,
                    Index.Invalid, "-nosecurity", null),
                new Option(null, OptionFlags.None, Index.Invalid,
                    Index.Invalid, "-nocorepolicies", null),
                new Option(null, OptionFlags.None, Index.Invalid,
                    Index.Invalid, "-nopluginpolicies", null),
                Option.CreateEndOfOptions()
            });
        }

        ///////////////////////////////////////////////////////////////////////

        //
        // NOTE: This is for the [interp invokehidden] sub-command.
        //
        private static OptionDictionary GetInterpInvokeHiddenOptions()
        {
            return new OptionDictionary(
                new IOption[] {
                new Option(null, OptionFlags.None, Index.Invalid,
                    Index.Invalid, "-global", null),
                new Option(null,
                    OptionFlags.MustHaveAbsoluteNamespaceValue,
                    Index.Invalid, Index.Invalid, "-namespace",
                    null),
                Option.CreateEndOfOptions()
            });
        }

        ///////////////////////////////////////////////////////////////////////

        //
        // NOTE: This is for the [interp policy] sub-command.
        //
        private static OptionDictionary GetInterpPolicyOptions()
        {
            return new OptionDictionary(
                new IOption[] {
                new Option(null, OptionFlags.MustHaveTypeValue,
                    Index.Invalid, Index.Invalid, "-type", null),
                new Option(null,
                    OptionFlags.MustHaveWideIntegerValue,
                    Index.Invalid, Index.Invalid, "-token", null),
                new Option(typeof(PolicyFlags),
                    OptionFlags.MustHaveEnumValue, Index.Invalid,
                    Index.Invalid, "-flags",
                    new Variant(PolicyFlags.Script)),
                Option.CreateEndOfOptions()
            });
        }

        ///////////////////////////////////////////////////////////////////////

        //
        // NOTE: This is for the [interp queue] sub-command.
        //
        private static OptionDictionary GetInterpQueueOptions()
        {
            return new OptionDictionary(
                new IOption[] {
                new Option(null, OptionFlags.MustHaveDateTimeValue,
                    Index.Invalid, Index.Invalid, "-when", null),
                Option.CreateEndOfOptions()
            });
        }

        ///////////////////////////////////////////////////////////////////////

        //
        // NOTE: This is for the [interp readorgetscriptfile]
        //       sub-command.
        //
        private static OptionDictionary GetInterpReadOrGetScriptFileOptions(
            ScriptFlags? scriptFlags, /* in */
            EngineFlags? engineFlags  /* in */
            )
        {
            return new OptionDictionary(
                new IOption[] {
                new Option(null, OptionFlags.MustHaveEncodingValue,
                    Index.Invalid, Index.Invalid, "-encoding", null),
                new Option(null, OptionFlags.MustHaveValue,
                    Index.Invalid, Index.Invalid, "-variable", null),
                new Option(null, OptionFlags.MustHaveBooleanValue,
                    Index.Invalid, Index.Invalid, "-package", null),
                new Option(typeof(ScriptFlags),
                    OptionFlags.MustHaveEnumValue, Index.Invalid,
                    Index.Invalid, "-scriptflags",
                    (scriptFlags != null) ?
                        new Variant((ScriptFlags)scriptFlags) :
                        null),
                new Option(typeof(EngineFlags),
                    OptionFlags.MustHaveEnumValue, Index.Invalid,
                    Index.Invalid, "-engineflags",
                    (engineFlags != null) ?
                        new Variant((EngineFlags)engineFlags) :
                        null),
                Option.CreateEndOfOptions()
            });
        }

        ///////////////////////////////////////////////////////////////////////

        //
        // NOTE: This is for the [interp rename] sub-command.
        //
        private static OptionDictionary GetInterpRenameOptions()
        {
            return new OptionDictionary(
                new IOption[] {
                new Option(null, OptionFlags.None, Index.Invalid,
                    Index.Invalid, "-nodelete", null),
                new Option(null, OptionFlags.None, Index.Invalid,
                    Index.Invalid, "-all", null),
                new Option(null, OptionFlags.Unsafe, Index.Invalid,
                    Index.Invalid, "-hidden", null),
                new Option(null, OptionFlags.Unsafe, Index.Invalid,
                    Index.Invalid, "-hiddenonly", null),
                new Option(typeof(IdentifierKind),
                    OptionFlags.MustHaveEnumValue, Index.Invalid,
                    Index.Invalid, "-kind",
                    new Variant(IdentifierKind.None)),
                new Option(null, OptionFlags.MustHaveValue,
                    Index.Invalid, Index.Invalid, "-newnamevar",
                    null),
                Option.CreateEndOfOptions()
            });
        }

        ///////////////////////////////////////////////////////////////////////

        //
        // NOTE: This is for the [interp resetcancel] sub-command.
        //
        private static OptionDictionary GetInterpResetCancelOptions()
        {
            return new OptionDictionary(
                new IOption[] {
                new Option(null, OptionFlags.None, Index.Invalid,
                    Index.Invalid, "-global", null),
                new Option(null, OptionFlags.None, Index.Invalid,
                    Index.Invalid, "-nolocal", null),
                new Option(null, OptionFlags.None, Index.Invalid,
                    Index.Invalid, "-force", null),
                Option.CreateEndOfOptions()
            });
        }

        ///////////////////////////////////////////////////////////////////////

        //
        // NOTE: This is for the [interp service] sub-command.
        //
        private static OptionDictionary GetInterpServiceOptions(
            EventFlags? eventFlags /* in */
            )
        {
            return new OptionDictionary(
                new IOption[] {
                new Option(null, OptionFlags.None, Index.Invalid,
                    Index.Invalid, "-dedicated", null),
                new Option(null, OptionFlags.None, Index.Invalid,
                    Index.Invalid, "-nocancel", null),
                new Option(null, OptionFlags.None, Index.Invalid,
                    Index.Invalid, "-noglobalcancel", null),
                new Option(null, OptionFlags.None, Index.Invalid,
                    Index.Invalid, "-erroronempty", null),
                new Option(null, OptionFlags.None, Index.Invalid,
                    Index.Invalid, "-userinterface", null),
                new Option(null, OptionFlags.None, Index.Invalid,
                    Index.Invalid, "-nocomplain", null),
                new Option(null,
                    OptionFlags.MustHaveWideIntegerValue,
                    Index.Invalid, Index.Invalid, "-thread", null),
                new Option(null,
                    OptionFlags.MustHaveIntegerValue,
                    Index.Invalid, Index.Invalid, "-limit", null),
                new Option(typeof(EventFlags),
                    OptionFlags.MustHaveEnumValue, Index.Invalid,
                    Index.Invalid, "-eventflags",
                    new Variant(eventFlags)),
                new Option(typeof(EventPriority),
                    OptionFlags.MustHaveEnumValue, Index.Invalid,
                    Index.Invalid, "-priority",
                    new Variant(EventPriority.Service)),
                Option.CreateEndOfOptions()
            });
        }

        ///////////////////////////////////////////////////////////////////////

        //
        // NOTE: This is for the [interp source] sub-command.
        //
        private static OptionDictionary GetInterpSourceOptions()
        {
            return new OptionDictionary(
                new IOption[] {
                Option.CreateEndOfOptions()
            });
        }

        ///////////////////////////////////////////////////////////////////////

        //
        // NOTE: This is for the [interp stub] sub-command.
        //
        private static OptionDictionary GetInterpStubOptions()
        {
            return new OptionDictionary(
                new IOption[] {
                new Option(null, OptionFlags.None, Index.Invalid,
                    Index.Invalid, "-ensemble", null),
                new Option(null, OptionFlags.None, Index.Invalid,
                    Index.Invalid, "-external", null),
                Option.CreateEndOfOptions()
            });
        }

        ///////////////////////////////////////////////////////////////////////

        //
        // NOTE: This is for the [interp subcommand] sub-command.
        //
        private static OptionDictionary GetInterpSubCommandOptions()
        {
            return new OptionDictionary(
                new IOption[] {
                new Option(typeof(SubCommandFlags),
                    OptionFlags.MustHaveEnumValue, Index.Invalid,
                    Index.Invalid, "-flags",
                    new Variant(SubCommandFlags.Default)),
                Option.CreateEndOfOptions()
            });
        }

        ///////////////////////////////////////////////////////////////////////

        //
        // NOTE: This is for the [interp subst] sub-command.
        //
        private static OptionDictionary GetInterpSubstOptions()
        {
            return new OptionDictionary(
                new IOption[] {
                new Option(null, OptionFlags.None, Index.Invalid,
                    Index.Invalid, "-nobackslashes", null),
                new Option(null, OptionFlags.None, Index.Invalid,
                    Index.Invalid, "-nocommands", null),
                new Option(null, OptionFlags.None, Index.Invalid,
                    Index.Invalid, "-novariables", null),
                Option.CreateEndOfOptions()
            });
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region [kill] Command Options
        //
        // NOTE: This is for the [kill] command.
        //
        private static OptionDictionary GetKillOptions()
        {
            return new OptionDictionary(
                new IOption[] {
                new Option(null, OptionFlags.None, Index.Invalid,
                    Index.Invalid, "-all", null),
                new Option(null, OptionFlags.None, Index.Invalid,
                    Index.Invalid, "-force", null),
                new Option(null, OptionFlags.NoCase, Index.Invalid,
                    Index.Invalid, "-whatIf", null),
                new Option(null, OptionFlags.None, Index.Invalid,
                    Index.Invalid, "-verbose", null),
                Option.CreateEndOfOptions()
            });
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region [library] Command Options
        //
        // NOTE: This is for the [library declare] sub-command.
        //
        private static OptionDictionary GetLibraryDeclareOptions()
        {
            return new OptionDictionary(
                new IOption[] {
                new Option(null, OptionFlags.None, Index.Invalid,
                    Index.Invalid, "-alias", null),
                new Option(null, OptionFlags.MustHaveValue,
                    Index.Invalid, Index.Invalid, "-module", null),
                new Option(null, OptionFlags.MustHaveValue,
                    Index.Invalid, Index.Invalid, "-functionname",
                    null),
                new Option(null,
                    OptionFlags.MustHaveWideIntegerValue,
                    Index.Invalid, Index.Invalid, "-address", null),
                new Option(null, OptionFlags.MustHaveTypeValue,
                    Index.Invalid, Index.Invalid, "-returntype",
                    null),
                new Option(null, OptionFlags.MustHaveTypeListValue,
                    Index.Invalid, Index.Invalid, "-parametertypes",
                    null),
                new Option(typeof(CallingConvention),
                    OptionFlags.MustHaveEnumValue, Index.Invalid,
                    Index.Invalid, "-callingconvention", null),
                new Option(null, OptionFlags.MustHaveValue,
                    Index.Invalid, Index.Invalid, "-assemblyname",
                    null),
                new Option(null, OptionFlags.MustHaveValue,
                    Index.Invalid, Index.Invalid, "-modulename",
                    null),
                new Option(null, OptionFlags.MustHaveValue,
                    Index.Invalid, Index.Invalid, "-typename", null),
                new Option(null, OptionFlags.MustHaveBooleanValue,
                    Index.Invalid, Index.Invalid, "-bestfitmapping",
                    null),
                new Option(typeof(CharSet),
                    OptionFlags.MustHaveEnumValue, Index.Invalid,
                    Index.Invalid, "-charset", null),
                new Option(null, OptionFlags.MustHaveBooleanValue,
                    Index.Invalid, Index.Invalid, "-setlasterror",
                    null),
                new Option(null, OptionFlags.MustHaveBooleanValue,
                    Index.Invalid, Index.Invalid,
                    "-throwonunmappablechar", null),
                new Option(null, OptionFlags.MustHaveValue,
                    Index.Invalid, Index.Invalid, "-delegatename",
                    null),
                Option.CreateEndOfOptions()
            });
        }

        ///////////////////////////////////////////////////////////////////////

        //
        // NOTE: This is for the [library load] sub-command.
        //
        private static OptionDictionary GetLibraryLoadOptions()
        {
            return new OptionDictionary(
                new IOption[] {
                new Option(null, OptionFlags.MustHaveValue,
                    Index.Invalid, Index.Invalid, "-modulename",
                    null),
                new Option(null, OptionFlags.None, Index.Invalid,
                    Index.Invalid, "-locked", null),
                new Option(null, OptionFlags.None, Index.Invalid,
                    Index.Invalid, "-maybetrustedonly", null),
                new Option(null, OptionFlags.None, Index.Invalid,
                    Index.Invalid, "-trustedonly", null),
                new Option(typeof(ModuleFlags),
                    OptionFlags.MustHaveEnumValue, Index.Invalid,
                    Index.Invalid, "-flags",
                    new Variant(ModuleFlags.None)),
                Option.CreateEndOfOptions()
            });
        }

        ///////////////////////////////////////////////////////////////////////

        //
        // NOTE: This is for the [library resolve] sub-command.
        //
        private static OptionDictionary GetLibraryResolveOptions()
        {
            return new OptionDictionary(
                new IOption[] {
                new Option(null, OptionFlags.MustHaveValue,
                    Index.Invalid, Index.Invalid, "-module", null),
                new Option(null, OptionFlags.MustHaveValue,
                    Index.Invalid, Index.Invalid, "-functionname",
                    null),
                Option.CreateEndOfOptions()
            });
        }

        ///////////////////////////////////////////////////////////////////////

        //
        // NOTE: This is for the [library unresolve] sub-command.
        //
        private static OptionDictionary GetLibraryUnresolveOptions()
        {
            return new OptionDictionary(
                new IOption[] {
                Option.CreateEndOfOptions()
            });
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region [load] Command Options
        //
        // NOTE: This is for the [load] command.
        //
        private static OptionDictionary GetLoadOptions()
        {
            return new OptionDictionary(
                new IOption[] {
                new Option(null, OptionFlags.MustHaveRuleSetValue |
                    OptionFlags.CouldBePath | OptionFlags.Unsafe,
                    Index.Invalid, Index.Invalid, "-ruleset", null),
                new Option(null, OptionFlags.Unsafe, Index.Invalid,
                    Index.Invalid, "-needclientdata", null),
                new Option(null, OptionFlags.Unsafe, Index.Invalid,
                    Index.Invalid, "-anythread", null),
                new Option(null, OptionFlags.Unsafe, Index.Invalid,
                    Index.Invalid, "-nocommands", null),
                new Option(null, OptionFlags.Unsafe, Index.Invalid,
                    Index.Invalid, "-nofunctions", null),
                new Option(null, OptionFlags.Unsafe, Index.Invalid,
                    Index.Invalid, "-nopolicies", null),
                new Option(null, OptionFlags.Unsafe, Index.Invalid,
                    Index.Invalid, "-notraces", null),
                new Option(null, OptionFlags.Unsafe, Index.Invalid,
                    Index.Invalid, "-noprovide", null),
                new Option(null, OptionFlags.Unsafe, Index.Invalid,
                    Index.Invalid, "-noresources", null),
                new Option(null, OptionFlags.Unsafe, Index.Invalid,
                    Index.Invalid, "-verifiedonly", null),
                //
                // HACK: The "-maybeverifiedonly" option is allowed
                //       in "safe" interpreters due to its lack of a
                //       value, its relative harmlessness, and because
                //       the core library binary plugin loader uses it,
                //       e.g. for HotKey, et al.
                //
                new Option(null, OptionFlags.None, Index.Invalid,
                    Index.Invalid, "-maybeverifiedonly", null),
                new Option(null, OptionFlags.Unsafe, Index.Invalid,
                    Index.Invalid, "-trustedonly", null),
                //
                // HACK: The "-maybetrustedonly" option is allowed
                //       in "safe" interpreters due to its lack of a
                //       value, its relative harmlessness, and because
                //       the core library binary plugin loader uses it,
                //       e.g. for HotKey, et al.
                //
                new Option(null, OptionFlags.None, Index.Invalid,
                    Index.Invalid, "-maybetrustedonly", null),
                new Option(null, OptionFlags.Unsafe |
                    OptionFlags.MustHaveValue, Index.Invalid,
                    Index.Invalid, "-publickeytoken", null),
#if ISOLATED_PLUGINS
                new Option(null, OptionFlags.Unsafe, Index.Invalid,
                    Index.Invalid, "-isolated", null),
                new Option(null, OptionFlags.Unsafe, Index.Invalid,
                    Index.Invalid, "-noisolated", null),
                new Option(null, OptionFlags.Unsafe, Index.Invalid,
                    Index.Invalid, "-preview", null),
                new Option(null, OptionFlags.Unsafe, Index.Invalid,
                    Index.Invalid, "-nopreview", null),
#else
                new Option(null, OptionFlags.Unsafe |
                    OptionFlags.Unsupported, Index.Invalid,
                    Index.Invalid, "-isolated", null),
                new Option(null, OptionFlags.Unsafe |
                    OptionFlags.Unsupported, Index.Invalid,
                    Index.Invalid, "-noisolated", null),
                new Option(null, OptionFlags.Unsafe |
                    OptionFlags.Unsupported, Index.Invalid,
                    Index.Invalid, "-preview", null),
                new Option(null, OptionFlags.Unsafe |
                    OptionFlags.Unsupported, Index.Invalid,
                    Index.Invalid, "-nopreview", null),
#endif
#if ISOLATED_PLUGINS && SHELL
                new Option(null, OptionFlags.Unsafe, Index.Invalid,
                    Index.Invalid, "-update", null),
                new Option(null, OptionFlags.Unsafe, Index.Invalid,
                    Index.Invalid, "-noupdate", null),
#else
                new Option(null, OptionFlags.Unsafe |
                    OptionFlags.Unsupported, Index.Invalid,
                    Index.Invalid, "-update", null),
                new Option(null, OptionFlags.Unsafe |
                    OptionFlags.Unsupported, Index.Invalid,
                    Index.Invalid, "-noupdate", null),
#endif
                new Option(null, OptionFlags.MustHaveObjectValue,
                    Index.Invalid, Index.Invalid, "-clientdata",
                    null),
                new Option(null, OptionFlags.MustHaveObjectValue,
                    Index.Invalid, Index.Invalid, "-data", null),
                new Option(null, OptionFlags.None, Index.Invalid,
                    Index.Invalid, "-viaresource", null),
                Option.CreateEndOfOptions()
            });
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region [lsearch] Command Options
        //
        // NOTE: This is for the [lsearch] command.
        //
        private static OptionDictionary GetLsearchOptions()
        {
            return new OptionDictionary(
                new IOption[] {
                new Option(null, OptionFlags.None, 1,
                    Index.Invalid, "-ascii", null),
                new Option(null, OptionFlags.None, 1,
                    Index.Invalid, "-dictionary", null),
                new Option(null, OptionFlags.None, 1,
                    Index.Invalid, "-integer", null),
                new Option(null, OptionFlags.None, 1,
                    Index.Invalid, "-real", null),
                new Option(null, OptionFlags.None, 2,
                    Index.Invalid, "-decreasing", null),
                new Option(null, OptionFlags.None, 2,
                    Index.Invalid, "-increasing", null),
                new Option(null, OptionFlags.None, 3,
                    Index.Invalid, "-exact", null),
                new Option(null, OptionFlags.None, 3,
                    Index.Invalid, "-substring", null),
                new Option(null, OptionFlags.None, 3,
                    Index.Invalid, "-glob", null),
                new Option(null, OptionFlags.None, 3,
                    Index.Invalid, "-regexp", null),
                new Option(null, OptionFlags.None, 3,
                    Index.Invalid, "-sorted", null),
                new Option(null, OptionFlags.None, Index.Invalid,
                    Index.Invalid, "-variable", null),
                new Option(null, OptionFlags.None, Index.Invalid,
                    Index.Invalid, "-inverse", null),
                new Option(null, OptionFlags.None, Index.Invalid,
                    Index.Invalid, "-subindices", null),
                new Option(null, OptionFlags.None, Index.Invalid,
                    Index.Invalid, "-all", null),
                new Option(null, OptionFlags.None, Index.Invalid,
                    Index.Invalid, "-inline", null),
                new Option(null, OptionFlags.None, Index.Invalid,
                    Index.Invalid, "-nocase", null),
                new Option(null, OptionFlags.None, Index.Invalid,
                    Index.Invalid, "-not", null),
                new Option(null, OptionFlags.MustHaveValue,
                    Index.Invalid, Index.Invalid, "-start", null),
                new Option(null, OptionFlags.MustHaveValue,
                    Index.Invalid, Index.Invalid, "-index", null),
                Option.CreateEndOfOptions()
            });
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region [lsort] Command Options
        //
        // NOTE: This is for the [lsort] command.
        //
        private static OptionDictionary GetLsortOptions()
        {
            return new OptionDictionary(
                new IOption[] {
                new Option(null, OptionFlags.None, 1,
                    Index.Invalid, "-ascii", null),
                new Option(null, OptionFlags.None, 1,
                    Index.Invalid, "-dictionary", null),
                new Option(null, OptionFlags.None, 1,
                    Index.Invalid, "-integer", null),
                new Option(null, OptionFlags.None, 1,
                    Index.Invalid, "-random", null),
                new Option(null, OptionFlags.None, 1,
                    Index.Invalid, "-real", null),
                new Option(null, OptionFlags.None, 2,
                    Index.Invalid, "-increasing", null),
                new Option(null, OptionFlags.None, 2,
                    Index.Invalid, "-decreasing", null),
                new Option(null, OptionFlags.None, Index.Invalid,
                    Index.Invalid, "-nocase", null),
                new Option(null, OptionFlags.None, Index.Invalid,
                    Index.Invalid, "-unique", null),
                new Option(null, OptionFlags.MustHaveValue,
                    Index.Invalid, Index.Invalid, "-command", null),
                new Option(null, OptionFlags.MustHaveValue,
                    Index.Invalid, Index.Invalid, "-index", null),
                Option.CreateEndOfOptions()
            });
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region [namespace] Command Options
        //
        // NOTE: This is for the [namespace1 export] sub-command.
        //
        private static OptionDictionary GetNamespace1ExportOptions()
        {
            return new OptionDictionary(
                new IOption[] {
                new Option(null, OptionFlags.None, Index.Invalid,
                    Index.Invalid, "-clear", null),
                Option.CreateEndOfOptions()
            });
        }

        ///////////////////////////////////////////////////////////////////////

        //
        // NOTE: This is for the [namespace1 import] sub-command.
        //
        private static OptionDictionary GetNamespace1ImportOptions()
        {
            return new OptionDictionary(
                new IOption[] {
                new Option(null, OptionFlags.None, Index.Invalid,
                    Index.Invalid, "-force", null),
                Option.CreateEndOfOptions()
            });
        }

        ///////////////////////////////////////////////////////////////////////

        //
        // NOTE: This is for the [namespace1 which] sub-command.
        //
        private static OptionDictionary GetNamespace1WhichOptions()
        {
            return new OptionDictionary(
                new IOption[] {
                new Option(null, OptionFlags.None, Index.Invalid,
                    Index.Invalid, "-command", null),
                new Option(null, OptionFlags.None, Index.Invalid,
                    Index.Invalid, "-variable", null),
                Option.CreateEndOfOptions()
            });
        }

        ///////////////////////////////////////////////////////////////////////

        //
        // NOTE: This is for the [namespace2 export] sub-command.
        //
        private static OptionDictionary GetNamespace2ExportOptions()
        {
            return new OptionDictionary(
                new IOption[] {
                new Option(null, OptionFlags.None, Index.Invalid,
                    Index.Invalid, "-clear", null),
                Option.CreateEndOfOptions()
            });
        }

        ///////////////////////////////////////////////////////////////////////

        //
        // NOTE: This is for the [namespace2 import] sub-command.
        //
        private static OptionDictionary GetNamespace2ImportOptions()
        {
            return new OptionDictionary(
                new IOption[] {
                new Option(null, OptionFlags.None, Index.Invalid,
                    Index.Invalid, "-force", null),
                Option.CreateEndOfOptions()
            });
        }

        ///////////////////////////////////////////////////////////////////////

        //
        // NOTE: This is for the [namespace2 which] sub-command.
        //
        private static OptionDictionary GetNamespace2WhichOptions()
        {
            return new OptionDictionary(
                new IOption[] {
                new Option(null, OptionFlags.None, Index.Invalid,
                    Index.Invalid, "-command", null),
                new Option(null, OptionFlags.None, Index.Invalid,
                    Index.Invalid, "-variable", null),
                Option.CreateEndOfOptions()
            });
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region [object] Command Options
        //
        // NOTE: This is for the [object verifyall] sub-command.
        //
        private static OptionDictionary GetObjectVerifyAllOptions()
        {
            return new OptionDictionary(
                new IOption[] {
                new Option(typeof(VerifyFlags),
                    OptionFlags.MustHaveEnumValue, Index.Invalid,
                    Index.Invalid, "-verifyflags",
                    new Variant(VerifyFlags.Default))
            }, ObjectOps.GetObjectOptions(ObjectOptionType.Certificate));
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region [open] Command Options
        //
        // NOTE: This is for the [open] command.
        //
        private static OptionDictionary GetOpenOptions()
        {
            return new OptionDictionary(
                new IOption[] {
#if CONSOLE
                new Option(null, OptionFlags.None, 1,
                    Index.Invalid, "-stdin", null),
                new Option(null, OptionFlags.None, 1,
                    Index.Invalid, "-stdout", null),
                new Option(null, OptionFlags.None, 1,
                    Index.Invalid, "-stderr", null),
#else
                new Option(null, OptionFlags.Unsupported, 1,
                    Index.Invalid, "-stdin", null),
                new Option(null, OptionFlags.Unsupported, 1,
                    Index.Invalid, "-stdout", null),
                new Option(null, OptionFlags.Unsupported, 1,
                    Index.Invalid, "-stderr", null),
#endif
                new Option(null, OptionFlags.MustHaveValue,
                    Index.Invalid, Index.Invalid, "-channelid",
                    null),
                new Option(null, OptionFlags.MustHaveIntegerValue,
                    Index.Invalid, Index.Invalid, "-buffersize",
                    null),
                new Option(null, OptionFlags.None, Index.Invalid,
                    Index.Invalid, "-nullencoding", null),
                new Option(null, OptionFlags.None, Index.Invalid,
                    Index.Invalid, "-autoflush", null),
                new Option(null, OptionFlags.None, Index.Invalid,
                    Index.Invalid, "-rawendofstream", null),
                new Option(typeof(HostStreamFlags),
                    OptionFlags.MustHaveEnumValue, Index.Invalid,
                    Index.Invalid, "-streamflags",
                    new Variant(HostStreamFlags.Default)),
                new Option(typeof(FileOptions),
                    OptionFlags.MustHaveEnumValue, Index.Invalid,
                    Index.Invalid, "-options",
                    new Variant(FileOptions.None)),
                new Option(typeof(FileShare),
                    OptionFlags.MustHaveEnumValue, Index.Invalid,
                    Index.Invalid, "-share",
                    new Variant(FileShare.Read))
            });
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region [package] Command Options
        private static OptionDictionary GetPackageAbsentOptions()
        {
            return new OptionDictionary(
                new IOption[] {
                new Option(null, OptionFlags.None, Index.Invalid,
                    Index.Invalid, "-exact", null),
                Option.CreateEndOfOptions()
            });
        }

        ///////////////////////////////////////////////////////////////////////

        private static OptionDictionary GetPackageAliasOptions()
        {
            return new OptionDictionary(
                new IOption[] {
                new Option(null, OptionFlags.None, Index.Invalid,
                    Index.Invalid, "-overwrite", null),
                new Option(null, OptionFlags.None, Index.Invalid,
                    Index.Invalid, "-disabled", null),
                new Option(null, OptionFlags.None, Index.Invalid,
                    Index.Invalid, "-exact", null),
                Option.CreateEndOfOptions()
            });
        }

        ///////////////////////////////////////////////////////////////////////

        private static OptionDictionary GetPackagePresentOptions()
        {
            return new OptionDictionary(
                new IOption[] {
                new Option(null, OptionFlags.None, Index.Invalid,
                    Index.Invalid, "-exact", null),
                Option.CreateEndOfOptions()
            });
        }

        ///////////////////////////////////////////////////////////////////////

        private static OptionDictionary GetPackageRequireOptions()
        {
            return new OptionDictionary(
                new IOption[] {
                new Option(null, OptionFlags.None, Index.Invalid,
                    Index.Invalid, "-exact", null),
                new Option(null, OptionFlags.MustHaveBooleanValue,
                    Index.Invalid, Index.Invalid, "-autoscan",
                    null),
                Option.CreateEndOfOptions()
            });
        }

        ///////////////////////////////////////////////////////////////////////

        private static OptionDictionary GetPackageScanPreOptionsMethod()
        {
            return new OptionDictionary(
                new IOption[] {
                new Option(null, OptionFlags.None, Index.Invalid,
                    Index.Invalid, "-interpreter", null),
                Option.CreateEndOfOptions()
            });
        }

        ///////////////////////////////////////////////////////////////////////

        private static OptionDictionary GetPackageScanOptions(
            PackageIndexFlags? packageIndexFlags /* in */
            )
        {
            return new OptionDictionary(
                new IOption[] {
                new Option(null, OptionFlags.Ignored, Index.Invalid,
                    Index.Invalid, "-interpreter", null),
                new Option(typeof(PackageIndexFlags),
                    OptionFlags.MustHaveEnumValue, Index.Invalid,
                    Index.Invalid, "-flags",
                    new Variant(packageIndexFlags)),
                new Option(null, OptionFlags.None, Index.Invalid,
                    Index.Invalid, "-reset", null),
                new Option(null, OptionFlags.None, Index.Invalid,
                    Index.Invalid, "-autopath", null),
                new Option(null, OptionFlags.NoCase, Index.Invalid,
                    Index.Invalid, "-whatIf", null),
                new Option(null, OptionFlags.None, Index.Invalid,
                    Index.Invalid, "-preferfilesystem", null),
                new Option(null, OptionFlags.None, Index.Invalid,
                    Index.Invalid, "-preferhost", null),
                new Option(null, OptionFlags.None, Index.Invalid,
                    Index.Invalid, "-host", null),
                new Option(null, OptionFlags.None, Index.Invalid,
                    Index.Invalid, "-nohost", null),
                new Option(null, OptionFlags.None, Index.Invalid,
                    Index.Invalid, "-bundle", null),
                new Option(null, OptionFlags.None, Index.Invalid,
                    Index.Invalid, "-nobundle", null),
#if APPDOMAINS || ISOLATED_INTERPRETERS || ISOLATED_PLUGINS
                new Option(null, OptionFlags.None, Index.Invalid,
                    Index.Invalid, "-plugin", null),
                new Option(null, OptionFlags.None, Index.Invalid,
                    Index.Invalid, "-noplugin", null),
#else
                new Option(null, OptionFlags.Unsupported,
                    Index.Invalid, Index.Invalid, "-plugin", null),
                new Option(null, OptionFlags.Unsupported,
                    Index.Invalid, Index.Invalid, "-noplugin", null),
#endif
                new Option(null, OptionFlags.None, Index.Invalid,
                    Index.Invalid, "-temporary", null),
                new Option(null, OptionFlags.None, Index.Invalid,
                    Index.Invalid, "-primary", null),
                new Option(null, OptionFlags.None, Index.Invalid,
                    Index.Invalid, "-noprimary", null),
                new Option(null, OptionFlags.None, Index.Invalid,
                    Index.Invalid, "-tagged", null),
                new Option(null, OptionFlags.None, Index.Invalid,
                    Index.Invalid, "-notagged", null),
                new Option(null, OptionFlags.None, Index.Invalid,
                    Index.Invalid, "-normal", null),
                new Option(null, OptionFlags.None, Index.Invalid,
                    Index.Invalid, "-nonormal", null),
                new Option(null, OptionFlags.None, Index.Invalid,
                    Index.Invalid, "-dump", null),
                new Option(null, OptionFlags.None, Index.Invalid,
                    Index.Invalid, "-nodump", null),
                new Option(null, OptionFlags.None, Index.Invalid,
                    Index.Invalid, "-recursive", null),
                new Option(null, OptionFlags.None, Index.Invalid,
                    Index.Invalid, "-refresh", null),
                new Option(null, OptionFlags.None, Index.Invalid,
                    Index.Invalid, "-resolve", null),
                new Option(null, OptionFlags.None, Index.Invalid,
                    Index.Invalid, "-trace", null),
                new Option(null, OptionFlags.None, Index.Invalid,
                    Index.Invalid, "-verbose", null),
                new Option(null, OptionFlags.None, Index.Invalid,
                    Index.Invalid, "-notrusted", null),
                new Option(null, OptionFlags.None, Index.Invalid,
                    Index.Invalid, "-noverified", null),
                new Option(null, OptionFlags.None, Index.Invalid,
                    Index.Invalid, "-nocomplain", null),
                new Option(null, OptionFlags.None, Index.Invalid,
                    Index.Invalid, "-fileerror", null),
                Option.CreateEndOfOptions()
            });
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region [parse] Command Options
        //
        // NOTE: This is for the [parse command] sub-command.
        //
        private static OptionDictionary GetParseCommandOptions(
            Interpreter interpreter /* in */
            )
        {
            EngineFlags engineFlags = (interpreter != null) ?
                interpreter.EngineFlags : EngineFlags.None;

            SubstitutionFlags substitutionFlags = (interpreter != null) ?
                interpreter.SubstitutionFlags : SubstitutionFlags.None;

            return new OptionDictionary(
                new IOption[] {
                new Option(typeof(EngineFlags),
                    OptionFlags.MustHaveEnumValue, Index.Invalid,
                    Index.Invalid, "-engineflags",
                    new Variant(engineFlags)),
                new Option(typeof(SubstitutionFlags),
                    OptionFlags.MustHaveEnumValue, Index.Invalid,
                    Index.Invalid, "-substitutionflags",
                    new Variant(substitutionFlags)),
                new Option(null,
                    OptionFlags.MustHaveIntegerValue,
                    Index.Invalid, Index.Invalid, "-startindex",
                    null),
                new Option(null,
                    OptionFlags.MustHaveIntegerValue,
                    Index.Invalid, Index.Invalid, "-characters",
                    null),
                new Option(null,
                    OptionFlags.MustHaveBooleanValue,
                    Index.Invalid, Index.Invalid, "-nested", null),
                new Option(null,
                    OptionFlags.MustHaveBooleanValue,
                    Index.Invalid, Index.Invalid, "-noready", null),
                Option.CreateEndOfOptions()
            });
        }

        ///////////////////////////////////////////////////////////////////////

        //
        // NOTE: This is for the [parse expression] sub-command.
        //
        private static OptionDictionary GetParseExpressionOptions(
            Interpreter interpreter /* in */
            )
        {
            EngineFlags engineFlags = (interpreter != null) ?
                interpreter.EngineFlags : EngineFlags.None;

            SubstitutionFlags substitutionFlags = (interpreter != null) ?
                interpreter.SubstitutionFlags : SubstitutionFlags.None;

            return new OptionDictionary(
                new IOption[] {
                new Option(typeof(EngineFlags),
                    OptionFlags.MustHaveEnumValue, Index.Invalid,
                    Index.Invalid, "-engineflags",
                    new Variant(engineFlags)),
                new Option(typeof(SubstitutionFlags),
                    OptionFlags.MustHaveEnumValue, Index.Invalid,
                    Index.Invalid, "-substitutionflags",
                    new Variant(substitutionFlags)),
                new Option(null,
                    OptionFlags.MustHaveIntegerValue,
                    Index.Invalid, Index.Invalid, "-startindex",
                    null),
                new Option(null,
                    OptionFlags.MustHaveIntegerValue,
                    Index.Invalid, Index.Invalid, "-characters",
                    null),
                new Option(null,
                    OptionFlags.MustHaveBooleanValue,
                    Index.Invalid, Index.Invalid, "-noready", null),
                Option.CreateEndOfOptions()
            });
        }

        ///////////////////////////////////////////////////////////////////////

        //
        // NOTE: This is for the [parse options] sub-command.
        //
        private static OptionDictionary GetParseOptionsOptions()
        {
            return new OptionDictionary(
                new IOption[] {
                new Option(typeof(OptionBehaviorFlags),
                    OptionFlags.MustHaveEnumValue, Index.Invalid,
                    Index.Invalid, "-flags",
                    new Variant(OptionBehaviorFlags.Default)),
                new Option(null, OptionFlags.MustHaveValue,
                    Index.Invalid, Index.Invalid, "-optionsvar",
                    null),
                new Option(null, OptionFlags.None, Index.Invalid,
                    Index.Invalid, "-indexes", null),
                new Option(null, OptionFlags.None, Index.Invalid,
                    Index.Invalid, "-allowinteger", null),
                new Option(null, OptionFlags.None, Index.Invalid,
                    Index.Invalid, "-strict", null),
                new Option(null, OptionFlags.None, Index.Invalid,
                    Index.Invalid, "-verbose", null),
                new Option(null, OptionFlags.None, Index.Invalid,
                    Index.Invalid, "-nocase", null),
                new Option(null, OptionFlags.None, Index.Invalid,
                    Index.Invalid, "-novalue", null),
                new Option(null, OptionFlags.None, Index.Invalid,
                    Index.Invalid, "-noset", null),
                new Option(null, OptionFlags.None, Index.Invalid,
                    Index.Invalid, "-noready", null),
                new Option(null, OptionFlags.None, Index.Invalid,
                    Index.Invalid, "-simple", null),
                Option.CreateEndOfOptions()
            });
        }

        ///////////////////////////////////////////////////////////////////////

        //
        // NOTE: This is for the [parse script] sub-command.
        //
        private static OptionDictionary GetParseScriptOptions(
            Interpreter interpreter /* in */
            )
        {
            EngineFlags engineFlags = (interpreter != null) ?
                interpreter.EngineFlags : EngineFlags.None;

            SubstitutionFlags substitutionFlags = (interpreter != null) ?
                interpreter.SubstitutionFlags : SubstitutionFlags.None;

            return new OptionDictionary(
                new IOption[] {
                new Option(typeof(EngineFlags),
                    OptionFlags.MustHaveEnumValue, Index.Invalid,
                    Index.Invalid, "-engineflags",
                    new Variant(engineFlags)),
                new Option(typeof(SubstitutionFlags),
                    OptionFlags.MustHaveEnumValue, Index.Invalid,
                    Index.Invalid, "-substitutionflags",
                    new Variant(substitutionFlags)),
                new Option(null, OptionFlags.MustHaveValue,
                    Index.Invalid, Index.Invalid, "-filename",
                    null),
                new Option(null,
                    OptionFlags.MustHaveIntegerValue,
                    Index.Invalid, Index.Invalid, "-currentline",
                    null),
                new Option(null,
                    OptionFlags.MustHaveIntegerValue,
                    Index.Invalid, Index.Invalid, "-startindex",
                    null),
                new Option(null,
                    OptionFlags.MustHaveIntegerValue,
                    Index.Invalid, Index.Invalid, "-characters",
                    null),
                new Option(null,
                    OptionFlags.MustHaveBooleanValue,
                    Index.Invalid, Index.Invalid, "-nested", null),
                new Option(null,
                    OptionFlags.MustHaveBooleanValue,
                    Index.Invalid, Index.Invalid, "-syntax", null),
                new Option(null,
                    OptionFlags.MustHaveBooleanValue,
                    Index.Invalid, Index.Invalid, "-strict", null),
                new Option(null,
                    OptionFlags.MustHaveBooleanValue,
                    Index.Invalid, Index.Invalid, "-roundtrip",
                    null),
                new Option(null,
                    OptionFlags.MustHaveBooleanValue,
                    Index.Invalid, Index.Invalid, "-noready", null),
                Option.CreateEndOfOptions()
            });
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region [puts] Command Options
        //
        // NOTE: This is for the [puts] command.
        //
        private static OptionDictionary GetPutsOptions()
        {
            return new OptionDictionary(
                new IOption[] {
                new Option(null, OptionFlags.MustHaveEncodingValue |
                    OptionFlags.Unsafe, Index.Invalid, Index.Invalid,
                    "-encoding", null),
                new Option(null, OptionFlags.Unsafe, Index.Invalid,
                    Index.Invalid, "-usecount", null),
                new Option(null, OptionFlags.Unsafe, Index.Invalid,
                    Index.Invalid, "-useobject", null),
                new Option(null, OptionFlags.None, Index.Invalid,
                    Index.Invalid, "-nonewline", null)
            });
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region [regexp] Command Options
        //
        // NOTE: This is for the [regexp] command.
        //
        private static OptionDictionary GetRegexpOptions()
        {
            return new OptionDictionary(
                new IOption[] {
                new Option(null, OptionFlags.Unsupported,
                    Index.Invalid, Index.Invalid, "-about", null),
                new Option(typeof(RegexOptions),
                    OptionFlags.MustHaveEnumValue, Index.Invalid,
                    Index.Invalid, "-options",
                    new Variant(StringOps.DefaultRegExSyntaxOptions)),
                new Option(null, OptionFlags.None, Index.Invalid,
                    Index.Invalid, "-all", null),
                new Option(null, OptionFlags.None, Index.Invalid,
                    Index.Invalid, "-debug", null),
                new Option(null, OptionFlags.None, Index.Invalid,
                    Index.Invalid, "-ecma", null),
                new Option(null, OptionFlags.None, Index.Invalid,
                    Index.Invalid, "-compiled", null),
                new Option(null, OptionFlags.None, Index.Invalid,
                    Index.Invalid, "-explicit", null),
                new Option(null, OptionFlags.None, Index.Invalid,
                    Index.Invalid, "-reverse", null),
                new Option(null, OptionFlags.None, Index.Invalid,
                    Index.Invalid, "-expanded", null),
                new Option(null, OptionFlags.None, Index.Invalid,
                    Index.Invalid, "-indexes", null),
                new Option(null, OptionFlags.None, Index.Invalid,
                    Index.Invalid, "-indices", null),
                new Option(null, OptionFlags.None, Index.Invalid,
                    Index.Invalid, "-global", null),
                new Option(null, OptionFlags.None, Index.Invalid,
                    Index.Invalid, "-inline", null),
                new Option(null, OptionFlags.MustHaveIntegerValue,
                    Index.Invalid, Index.Invalid, "-skip", null),
                new Option(null, OptionFlags.MustHaveIntegerValue,
                    Index.Invalid, Index.Invalid, "-limit", null),
                new Option(null, OptionFlags.None, Index.Invalid,
                    Index.Invalid, "-line", null),
                new Option(null, OptionFlags.None, Index.Invalid,
                    Index.Invalid, "-lineanchor", null),
                new Option(null, OptionFlags.None, Index.Invalid,
                    Index.Invalid, "-linestop", null),
                new Option(null, OptionFlags.None, Index.Invalid,
                    Index.Invalid, "-nocase", null),
                new Option(null, OptionFlags.None, Index.Invalid,
                    Index.Invalid, "-noempty", null),
                new Option(null, OptionFlags.None, Index.Invalid,
                    Index.Invalid, "-noculture", null),
                new Option(null, OptionFlags.MustHaveValue,
                    Index.Invalid, Index.Invalid, "-start", null),
                new Option(null, OptionFlags.MustHaveIntegerValue,
                    Index.Invalid, Index.Invalid, "-length", null),
                Option.CreateEndOfOptions()
            });
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region [regsub] Command Options
        //
        // NOTE: This is for the [regsub] command.
        //
        private static OptionDictionary GetRegsubOptions()
        {
            return new OptionDictionary(
                new IOption[] {
                new Option(typeof(RegexOptions),
                    OptionFlags.MustHaveEnumValue, Index.Invalid,
                    Index.Invalid, "-options",
                    new Variant(StringOps.DefaultRegExSyntaxOptions)),
                new Option(null, OptionFlags.None, Index.Invalid,
                    Index.Invalid, "-all", null),
                new Option(null, OptionFlags.MustHaveIntegerValue,
                    Index.Invalid, Index.Invalid, "-count", null),
                new Option(null, OptionFlags.None, Index.Invalid,
                    Index.Invalid, "-ecma", null),
                new Option(null, OptionFlags.None, Index.Invalid,
                    Index.Invalid, "-compiled", null),
                new Option(null, OptionFlags.None, Index.Invalid,
                    Index.Invalid, "-explicit", null),
                new Option(null, OptionFlags.None, Index.Invalid,
                    Index.Invalid, "-quote", null),
                new Option(null, OptionFlags.None, Index.Invalid,
                    Index.Invalid, "-nostrict", null),
                new Option(null, OptionFlags.None, Index.Invalid,
                    Index.Invalid, "-reverse", null),
                new Option(null, OptionFlags.MustHaveValue,
                    Index.Invalid, Index.Invalid, "-eval", null),
                new Option(null, OptionFlags.None, Index.Invalid,
                    Index.Invalid, "-command", null),
                new Option(null, OptionFlags.None, Index.Invalid,
                    Index.Invalid, "-literal", null),
                new Option(null, OptionFlags.None, Index.Invalid,
                    Index.Invalid, "-verbatim", null),
                new Option(null, OptionFlags.None, Index.Invalid,
                    Index.Invalid, "-extra", null),
                new Option(null, OptionFlags.None, Index.Invalid,
                    Index.Invalid, "-expanded", null),
                new Option(null, OptionFlags.None, Index.Invalid,
                    Index.Invalid, "-line", null),
                new Option(null, OptionFlags.None, Index.Invalid,
                    Index.Invalid, "-lineanchor", null),
                new Option(null, OptionFlags.None, Index.Invalid,
                    Index.Invalid, "-linestop", null),
                new Option(null, OptionFlags.None, Index.Invalid,
                    Index.Invalid, "-nocase", null),
                new Option(null, OptionFlags.None, Index.Invalid,
                    Index.Invalid, "-noculture", null),
                new Option(null, OptionFlags.MustHaveValue,
                    Index.Invalid, Index.Invalid, "-start", null),
                Option.CreateEndOfOptions()
            });
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region [rename] Command Options
        //
        // NOTE: This is for the [rename] command.
        //
        private static OptionDictionary GetRenameOptions()
        {
            return new OptionDictionary(
                new IOption[] {
                new Option(null, OptionFlags.None, Index.Invalid,
                    Index.Invalid, "-nodelete", null),
                new Option(null, OptionFlags.Unsafe, Index.Invalid,
                    Index.Invalid, "-hidden", null),
                new Option(null, OptionFlags.Unsafe, Index.Invalid,
                    Index.Invalid, "-hiddenonly", null),
                new Option(typeof(IdentifierKind),
                    OptionFlags.MustHaveEnumValue |
                    OptionFlags.Unsafe, Index.Invalid,
                    Index.Invalid, "-kind",
                    new Variant(IdentifierKind.None)),
                new Option(null, OptionFlags.MustHaveValue,
                    Index.Invalid, Index.Invalid, "-newnamevar",
                    null),
                Option.CreateEndOfOptions()
            });
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region [return] Command Options
        //
        // NOTE: This is for the [return] command.
        //
        private static OptionDictionary GetReturnOptions()
        {
            return new OptionDictionary(
                new IOption[] {
                new Option(null, OptionFlags.MustHaveReturnCodeValue,
                    Index.Invalid, Index.Invalid, "-code", null),
                new Option(null, OptionFlags.MustHaveValue,
                    Index.Invalid, Index.Invalid, "-errorinfo", null),
                new Option(null, OptionFlags.MustHaveValue,
                    Index.Invalid, Index.Invalid, "-errorcode", null),
                Option.CreateEndOfOptions()
            });
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region [scope] Command Options
        //
        // NOTE: This is for the [scope close] sub-command.
        //
        private static OptionDictionary GetScopeCloseOptions()
        {
            return new OptionDictionary(
                new IOption[] {
                new Option(null, OptionFlags.None, Index.Invalid,
                    Index.Invalid, "-all", null),
                Option.CreateEndOfOptions()
            });
        }

        ///////////////////////////////////////////////////////////////////////

        //
        // NOTE: This is for the [scope create] sub-command.
        //
        private static OptionDictionary GetScopeCreateOptions()
        {
            return new OptionDictionary(
                new IOption[] {
                new Option(null, OptionFlags.None, Index.Invalid,
                    Index.Invalid, "-args", null),
                new Option(null, OptionFlags.None, Index.Invalid,
                    Index.Invalid, "-clone", null),
                new Option(null, OptionFlags.Unsafe, Index.Invalid,
                    Index.Invalid, "-byref", null),
                new Option(null, OptionFlags.None, Index.Invalid,
                    Index.Invalid, "-global", null),
                new Option(null, OptionFlags.None, Index.Invalid,
                    Index.Invalid, "-open", null),
                new Option(null, OptionFlags.None, Index.Invalid,
                    Index.Invalid, "-procedure", null),
                new Option(null, OptionFlags.None, Index.Invalid,
                    Index.Invalid, "-shared", null),
                new Option(null, OptionFlags.None, Index.Invalid,
                    Index.Invalid, "-strict", null),
                new Option(null, OptionFlags.None, Index.Invalid,
                    Index.Invalid, "-fast", null),
                Option.CreateEndOfOptions()
            });
        }

        ///////////////////////////////////////////////////////////////////////

        //
        // NOTE: This is for the [scope eval] sub-command.
        //
        private static OptionDictionary GetScopeEvalOptions(
            Interpreter interpreter /* in */
            )
        {
            EventWaitFlags eventWaitFlags = (interpreter != null) ?
                interpreter.EventWaitFlags : EventWaitFlags.None;

            return new OptionDictionary(
                new IOption[] {
                new Option(typeof(EventWaitFlags),
                    OptionFlags.MustHaveEnumValue | OptionFlags.Unsafe,
                    Index.Invalid, Index.Invalid, "-eventwaitflags",
                    new Variant(eventWaitFlags)),
                new Option(null, OptionFlags.MustHaveBooleanValue,
                    Index.Invalid, Index.Invalid, "-lock", null),
                new Option(null, OptionFlags.MustHaveIntegerValue |
                    OptionFlags.Unsafe, Index.Invalid, Index.Invalid,
                    "-timeout", null),
                Option.CreateEndOfOptions()
            });
        }

        ///////////////////////////////////////////////////////////////////////

        //
        // NOTE: This is for the [scope global] sub-command.
        //
        private static OptionDictionary GetScopeGlobalOptions()
        {
            return new OptionDictionary(
                new IOption[] {
                new Option(null, OptionFlags.None, Index.Invalid,
                    Index.Invalid, "-unset", null),
                new Option(null, OptionFlags.None, Index.Invalid,
                    Index.Invalid, "-force", null),
                Option.CreateEndOfOptions()
            });
        }

        ///////////////////////////////////////////////////////////////////////

        //
        // NOTE: This is for the [scope lock] sub-command.
        //
        private static OptionDictionary GetScopeLockOptions()
        {
            return new OptionDictionary(
                new IOption[] {
                new Option(null, OptionFlags.None, Index.Invalid,
                    Index.Invalid, "-nocomplain", null),
                Option.CreateEndOfOptions()
            });
        }

        ///////////////////////////////////////////////////////////////////////

        //
        // NOTE: This is for the [scope open] sub-command.
        //
        private static OptionDictionary GetScopeOpenOptions()
        {
            return new OptionDictionary(
                new IOption[] {
                new Option(null, OptionFlags.None, Index.Invalid,
                    Index.Invalid, "-procedure", null),
                new Option(null, OptionFlags.None, Index.Invalid,
                    Index.Invalid, "-shared", null),
                new Option(null, OptionFlags.None, Index.Invalid,
                    Index.Invalid, "-args", null),
                Option.CreateEndOfOptions()
            });
        }

        ///////////////////////////////////////////////////////////////////////

        //
        // NOTE: This is for the [scope unlock] sub-command.
        //
        private static OptionDictionary GetScopeUnlockOptions()
        {
            return new OptionDictionary(
                new IOption[] {
                new Option(null, OptionFlags.None, Index.Invalid,
                    Index.Invalid, "-nocomplain", null),
                Option.CreateEndOfOptions()
            });
        }

        ///////////////////////////////////////////////////////////////////////

        //
        // NOTE: This is for the [scope update] sub-command.
        //
        private static OptionDictionary GetScopeUpdateOptions()
        {
            return new OptionDictionary(
                new IOption[] {
                new Option(null, OptionFlags.None, Index.Invalid,
                    Index.Invalid, "-global", null),
                Option.CreateEndOfOptions()
            });
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region [socket] Command Options
        //
        // NOTE: This is for the [socket] command.
        //
        private static OptionDictionary GetSocketOptions()
        {
            return new OptionDictionary(
                new IOption[] {
                new Option(typeof(TimeoutType),
                    OptionFlags.MustHaveEnumValue, Index.Invalid,
                    Index.Invalid, "-timeouttype", null),
                new Option(typeof(AddressFamily),
                    OptionFlags.MustHaveEnumValue, Index.Invalid,
                    Index.Invalid, "-addressfamily", null),
                new Option(null, OptionFlags.MustHaveBooleanValue |
                    OptionFlags.Nullable, Index.Invalid,
                    Index.Invalid, "-keepalive", null),
                new Option(null, OptionFlags.MustHaveValue,
                    Index.Invalid, Index.Invalid, "-server", null),
                new Option(null, OptionFlags.MustHaveIntegerValue,
                    Index.Invalid, Index.Invalid, "-buffer", null),
                new Option(null, OptionFlags.MustHaveIntegerValue,
                    Index.Invalid, Index.Invalid, "-timeout", null),
                new Option(null, OptionFlags.MustHaveIntegerValue,
                    Index.Invalid, Index.Invalid, "-sendtimeout",
                    null),
                new Option(null, OptionFlags.MustHaveIntegerValue,
                    Index.Invalid, Index.Invalid,
                    "-receivetimeout", null),
                new Option(null, OptionFlags.MustHaveIntegerValue,
                    Index.Invalid, Index.Invalid,
                    "-availabletimeout", null),
                new Option(null, OptionFlags.MustHaveIntegerValue,
                    Index.Invalid, Index.Invalid, "-readtimeout",
                    null),
                new Option(null, OptionFlags.MustHaveIntegerValue,
                    Index.Invalid, Index.Invalid, "-writetimeout",
                    null),
                new Option(null, OptionFlags.MustHaveValue,
                    Index.Invalid, Index.Invalid, "-myaddr", null),
                new Option(null, OptionFlags.MustHaveValue,
                    Index.Invalid, Index.Invalid, "-myport", null),
                new Option(null, OptionFlags.Unsupported,
                    Index.Invalid, Index.Invalid, "-async", null),
                new Option(null, OptionFlags.MustHaveValue,
                    Index.Invalid, Index.Invalid, "-channelid",
                    null),
                new Option(null, OptionFlags.None, Index.Invalid,
                    Index.Invalid, "-nodelay", null),
                new Option(null, OptionFlags.None, Index.Invalid,
                    Index.Invalid, "-nobuffer", null),
                new Option(null, OptionFlags.None, Index.Invalid,
                    Index.Invalid, "-noexclusive", null),
                new Option(null, OptionFlags.None, Index.Invalid,
                    Index.Invalid, "-trace", null),
                Option.CreateEndOfOptions()
            });
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region [source] Command Options
        //
        // NOTE: This is for the [source] command.
        //
        private static OptionDictionary GetSourceOptions()
        {
            return new OptionDictionary(
                new IOption[] {
                new Option(null, OptionFlags.MustHaveEncodingValue,
                    Index.Invalid, Index.Invalid, "-encoding", null),
                new Option(null, OptionFlags.MustHaveBooleanValue,
                    Index.Invalid, Index.Invalid, "-withinfo", null),
                new Option(null, OptionFlags.MustHaveBooleanValue,
                    Index.Invalid, Index.Invalid, "-time", null),
                new Option(null, OptionFlags.MustHaveByteArrayValue,
                    Index.Invalid, Index.Invalid, "-password", null),
                new Option(null, OptionFlags.MustHaveBooleanValue,
                    Index.Invalid, Index.Invalid, "-library", null),
#if DATA
                new Option(null, OptionFlags.MustHaveBooleanValue |
                    OptionFlags.Nullable, Index.Invalid,
                    Index.Invalid, "-bundle", null),
                new Option(typeof(BundleFlags),
                    OptionFlags.MustHaveEnumValue, Index.Invalid,
                    Index.Invalid, "-bundleflags",
                    new Variant(BundleFlags.Default)),
#else
                new Option(null, OptionFlags.MustHaveBooleanValue |
                    OptionFlags.Nullable | OptionFlags.Unsupported,
                    Index.Invalid, Index.Invalid, "-bundle", null),
                new Option(null, OptionFlags.MustHaveEnumValue |
                    OptionFlags.Unsupported, Index.Invalid,
                    Index.Invalid, "-bundleflags", null),
#endif
                Option.CreateEndOfOptions()
            });
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region [split] Command Options
        //
        // NOTE: This is for the [split] command.
        //
        private static OptionDictionary GetSplitOptions()
        {
            return new OptionDictionary(
                new IOption[] {
                new Option(null, OptionFlags.None, Index.Invalid,
                    Index.Invalid, "-string", null),
                Option.CreateEndOfOptions()
            });
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region [sql] Command Options
        //
        // NOTE: This is for the [sql open] sub-command
        //       (pre-options phase).
        //
        private static OptionDictionary GetSqlOpenPreOptionsMethod()
        {
            return new OptionDictionary(
                new IOption[] {
                new Option(null, OptionFlags.None, Index.Invalid,
                    Index.Invalid, "-stricttype", null),
                new Option(null, OptionFlags.None, Index.Invalid,
                    Index.Invalid, "-verbose", null),
                new Option(null, OptionFlags.None, Index.Invalid,
                    Index.Invalid, "-nocase", null),
                Option.CreateEndOfOptions()
            });
        }

        ///////////////////////////////////////////////////////////////////////

        //
        // NOTE: This is for the [sql open] sub-command.
        //
        private static OptionDictionary GetSqlOpenOptions(
            ValueFlags? valueFlags /* in */
            )
        {
            return new OptionDictionary(
                new IOption[] {
                new Option(typeof(DbConnectionType),
                    OptionFlags.MustHaveEnumValue, Index.Invalid,
                    Index.Invalid, "-type", null),
                new Option(typeof(DbConnectionType),
                    OptionFlags.MustHaveEnumValue, Index.Invalid,
                    Index.Invalid, "-type1", null),
                new Option(typeof(DbConnectionType),
                    OptionFlags.MustHaveEnumValue, Index.Invalid,
                    Index.Invalid, "-type2", null),
                new Option(null, OptionFlags.MustHaveValue,
                    Index.Invalid, Index.Invalid, "-variable", null),
                new Option(null, OptionFlags.MustHaveValue,
                    Index.Invalid, Index.Invalid,
                    "-assemblyfilename", null),
                new Option(null, OptionFlags.MustHaveValue,
                    Index.Invalid, Index.Invalid, "-typename", null),
                new Option(null, OptionFlags.MustHaveValue,
                    Index.Invalid, Index.Invalid, "-typefullname",
                    null),
                new Option(typeof(ValueFlags),
                    OptionFlags.MustHaveEnumValue, Index.Invalid,
                    Index.Invalid, "-valueflags",
                    new Variant(valueFlags)),
                new Option(null, OptionFlags.None, Index.Invalid,
                    Index.Invalid, "-trustedonly", null),
                new Option(null, OptionFlags.None, Index.Invalid,
                    Index.Invalid, "-maybetrustedonly", null),
                new Option(null, OptionFlags.MustHaveValue,
                    Index.Invalid, Index.Invalid,
                    "-publickeytoken1", null),
                new Option(null, OptionFlags.MustHaveValue,
                    Index.Invalid, Index.Invalid,
                    "-publickeytoken2", null),
                new Option(null, OptionFlags.Ignored, Index.Invalid,
                    Index.Invalid, "-stricttype", null),
                new Option(null, OptionFlags.Ignored, Index.Invalid,
                    Index.Invalid, "-verbose", null),
                new Option(null, OptionFlags.Ignored, Index.Invalid,
                    Index.Invalid, "-nocase", null),
                Option.CreateEndOfOptions()
            });
        }

        ///////////////////////////////////////////////////////////////////////

#if DATA
        //
        // NOTE: This is for the [sql transaction] sub-command.
        //
        private static OptionDictionary GetSqlTransactionOptions()
        {
            return new OptionDictionary(
                new IOption[] {
                new Option(typeof(System.Data.IsolationLevel),
                    OptionFlags.MustHaveEnumValue, Index.Invalid,
                    Index.Invalid, "-isolation", null),
                new Option(null, OptionFlags.MustHaveValue,
                    Index.Invalid, Index.Invalid, "-variable", null),
                Option.CreateEndOfOptions()
            });
        }
#endif
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region [string] Command Options
        private static OptionDictionary GetStringEqualOptions()
        {
            return new OptionDictionary(
                new IOption[] {
#if (NET_20_SP2 || NET_40 || NET_STANDARD_20) && !MONO_LEGACY
                new Option(null,
                    OptionFlags.MustHaveCultureInfoValue,
                    Index.Invalid, Index.Invalid, "-culture", null),
                new Option(typeof(CompareOptions),
                    OptionFlags.MustHaveEnumValue, Index.Invalid,
                    Index.Invalid, "-options",
                    new Variant(CompareOptions.None)),
#else
                new Option(null,
                    OptionFlags.MustHaveCultureInfoValue |
                    OptionFlags.Unsupported, Index.Invalid,
                    Index.Invalid, "-culture", null),
                new Option(typeof(CompareOptions),
                    OptionFlags.MustHaveEnumValue |
                    OptionFlags.Unsupported, Index.Invalid,
                    Index.Invalid, "-options",
                    new Variant(CompareOptions.None)),
#endif
                new Option(null, OptionFlags.None, Index.Invalid,
                    Index.Invalid, "-nocase", null),
                new Option(typeof(StringComparison),
                    OptionFlags.MustHaveEnumValue, Index.Invalid,
                    Index.Invalid, "-comparison", null),
                new Option(null, OptionFlags.MustHaveIntegerValue,
                    Index.Invalid, Index.Invalid, "-length", null),
                Option.CreateEndOfOptions()
            });
        }

        ///////////////////////////////////////////////////////////////////////

        private static OptionDictionary GetStringEndsOptions()
        {
            return new OptionDictionary(
                new IOption[] {
#if (NET_20_SP2 || NET_40 || NET_STANDARD_20) && !MONO_LEGACY
                new Option(null,
                    OptionFlags.MustHaveCultureInfoValue,
                    Index.Invalid, Index.Invalid, "-culture", null),
#else
                new Option(null,
                    OptionFlags.MustHaveCultureInfoValue |
                    OptionFlags.Unsupported, Index.Invalid,
                    Index.Invalid, "-culture", null),
#endif
                new Option(null, OptionFlags.None, Index.Invalid,
                    Index.Invalid, "-nocase", null),
                new Option(typeof(StringComparison),
                    OptionFlags.MustHaveEnumValue, Index.Invalid,
                    Index.Invalid, "-comparison", null),
                Option.CreateEndOfOptions()
            });
        }

        ///////////////////////////////////////////////////////////////////////

        private static OptionDictionary GetStringFirstOptions()
        {
            return new OptionDictionary(
                new IOption[] {
                new Option(null, OptionFlags.None, Index.Invalid,
                    Index.Invalid, "-nocase", null),
                new Option(typeof(StringComparison),
                    OptionFlags.MustHaveEnumValue, Index.Invalid,
                    Index.Invalid, "-comparison", null),
                Option.CreateEndOfOptions()
            });
        }

        ///////////////////////////////////////////////////////////////////////

        private static OptionDictionary GetStringFormatOptions(
            Interpreter interpreter /* in */
            )
        {
            return new OptionDictionary(
                new IOption[] {
                new Option(null, OptionFlags.MustHaveValue,
                    Index.Invalid, Index.Invalid, "-valueformat",
                    null),
                new Option(typeof(DateTimeKind),
                    OptionFlags.MustHaveEnumValue, Index.Invalid,
                    Index.Invalid, "-datetimekind",
                    new Variant((interpreter != null) ?
                        interpreter.DateTimeKind :
                        DateTimeKind.Unspecified)),
                new Option(typeof(DateTimeStyles),
                    OptionFlags.MustHaveEnumValue, Index.Invalid,
                    Index.Invalid, "-datetimestyles",
                    new Variant(
                        ObjectOps.GetDefaultDateTimeStyles())),
                new Option(null,
                    OptionFlags.MustHaveCultureInfoValue,
                    Index.Invalid, Index.Invalid, "-culture", null),
                new Option(null, OptionFlags.None, Index.Invalid,
                    Index.Invalid, "-verbatim", null),
                new Option(typeof(ValueFlags),
                    OptionFlags.MustHaveEnumValue, Index.Invalid,
                    Index.Invalid, "-valueflags",
                    new Variant(ValueFlags.AnyNonCharacter)),
                Option.CreateEndOfOptions()
            });
        }

        ///////////////////////////////////////////////////////////////////////

        private static OptionDictionary GetStringIsOptions(
            bool? not /* in */
            )
        {
            return new OptionDictionary(
                new IOption[] {
                new Option(null, OptionFlags.None, Index.Invalid,
                    Index.Invalid, "-strict", null),
                new Option(null, OptionFlags.None, Index.Invalid,
                    Index.Invalid, "-nocomplain", null),
                new Option(null, OptionFlags.MustHaveBooleanValue,
                    Index.Invalid, Index.Invalid, "-not",
                    new Variant(not)),
                new Option(null, OptionFlags.MustHaveBooleanValue,
                    Index.Invalid, Index.Invalid, "-any", null),
                new Option(null, OptionFlags.MustHaveBooleanValue,
                    Index.Invalid, Index.Invalid, "-via", null),
                new Option(null, OptionFlags.MustHaveIntegerValue,
                    Index.Invalid, Index.Invalid, "-count", null),
                new Option(null, OptionFlags.MustHaveValue,
                    Index.Invalid, Index.Invalid, "-good", null),
                new Option(null, OptionFlags.MustHaveValue,
                    Index.Invalid, Index.Invalid, "-bad", null),
                new Option(null, OptionFlags.MustHaveValue,
                    Index.Invalid, Index.Invalid, "-failindex",
                    null),
                Option.CreateEndOfOptions()
            });
        }

        ///////////////////////////////////////////////////////////////////////

        private static OptionDictionary GetStringLastOptions()
        {
            return new OptionDictionary(
                new IOption[] {
                new Option(null, OptionFlags.None, Index.Invalid,
                    Index.Invalid, "-nocase", null),
                new Option(typeof(StringComparison),
                    OptionFlags.MustHaveEnumValue, Index.Invalid,
                    Index.Invalid, "-comparison", null),
                Option.CreateEndOfOptions()
            });
        }

        ///////////////////////////////////////////////////////////////////////

        private static OptionDictionary GetStringMapOptions()
        {
            return new OptionDictionary(
                new IOption[] {
                new Option(null, OptionFlags.None, Index.Invalid,
                    Index.Invalid, "-multipass", null),
                new Option(null, OptionFlags.None, Index.Invalid,
                    Index.Invalid, "-regexp", null),
                new Option(null, OptionFlags.None, Index.Invalid,
                    Index.Invalid, "-subspec", null),
                new Option(null, OptionFlags.None, Index.Invalid,
                    Index.Invalid, "-eval", null),
                new Option(null, OptionFlags.MustHaveIntegerValue,
                    Index.Invalid, Index.Invalid, "-maximum", null),
                new Option(null, OptionFlags.MustHaveValue,
                    Index.Invalid, Index.Invalid, "-countvar",
                    null),
                new Option(typeof(StringComparison),
                    OptionFlags.MustHaveEnumValue, Index.Invalid,
                    Index.Invalid, "-comparison", null),
                new Option(typeof(RegexOptions),
                    OptionFlags.MustHaveEnumValue, Index.Invalid,
                    Index.Invalid, "-regexpoptions",
                    new Variant(StringOps.DefaultRegExOptions)),
                new Option(null, OptionFlags.None, Index.Invalid,
                    Index.Invalid, "-nocase", null),
                Option.CreateEndOfOptions()
            });
        }

        ///////////////////////////////////////////////////////////////////////

        private static OptionDictionary GetStringMatchOptions()
        {
            return new OptionDictionary(
                new IOption[] {
                new Option(null, OptionFlags.MustHaveMatchModeValue,
                    Index.Invalid, Index.Invalid, "-mode",
                    new Variant(StringOps.DefaultMatchMode)),
                new Option(null, OptionFlags.None, Index.Invalid,
                    Index.Invalid, "-nocase", null),
                Option.CreateEndOfOptions()
            });
        }

        ///////////////////////////////////////////////////////////////////////

        private static OptionDictionary GetStringStartsOptions()
        {
            return new OptionDictionary(
                new IOption[] {
#if (NET_20_SP2 || NET_40 || NET_STANDARD_20) && !MONO_LEGACY
                new Option(null,
                    OptionFlags.MustHaveCultureInfoValue,
                    Index.Invalid, Index.Invalid, "-culture", null),
#else
                new Option(null,
                    OptionFlags.MustHaveCultureInfoValue |
                    OptionFlags.Unsupported, Index.Invalid,
                    Index.Invalid, "-culture", null),
#endif
                new Option(null, OptionFlags.None, Index.Invalid,
                    Index.Invalid, "-nocase", null),
                new Option(typeof(StringComparison),
                    OptionFlags.MustHaveEnumValue, Index.Invalid,
                    Index.Invalid, "-comparison", null),
                Option.CreateEndOfOptions()
            });
        }

        ///////////////////////////////////////////////////////////////////////

        private static OptionDictionary GetStringToUpperOptions()
        {
            return new OptionDictionary(
                new IOption[] {
#if (NET_20_SP2 || NET_40 || NET_STANDARD_20) && !MONO_LEGACY
                new Option(null,
                    OptionFlags.MustHaveCultureInfoValue,
                    Index.Invalid, Index.Invalid, "-culture", null),
#else
                new Option(null,
                    OptionFlags.MustHaveCultureInfoValue |
                    OptionFlags.Unsupported, Index.Invalid,
                    Index.Invalid, "-culture", null),
#endif
                Option.CreateEndOfOptions()
            });
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region [subst] Command Options
        //
        // NOTE: This is for the [subst] command.
        //
        private static OptionDictionary GetSubstOptions()
        {
            return new OptionDictionary(
                new IOption[] {
                new Option(null, OptionFlags.None, Index.Invalid,
                    Index.Invalid, "-nobackslashes", null),
                new Option(null, OptionFlags.None, Index.Invalid,
                    Index.Invalid, "-nocommands", null),
                new Option(null, OptionFlags.None, Index.Invalid,
                    Index.Invalid, "-novariables", null) /*,
                Option.CreateEndOfOptions() */
            });
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region [switch] Command Options
        //
        // NOTE: This is for the [switch] command.
        //
        private static OptionDictionary GetSwitchOptions()
        {
            return new OptionDictionary(
                new IOption[] {
                new Option(null, OptionFlags.None, 1,
                    Index.Invalid, "-exact", null),
                new Option(null, OptionFlags.None, 3,
                    Index.Invalid, "-integer", null),
                new Option(null, OptionFlags.None, 3,
                    Index.Invalid, "-substring", null),
                new Option(null, OptionFlags.None, 1,
                    Index.Invalid, "-glob", null),
                new Option(null, OptionFlags.None, 1,
                    Index.Invalid, "-regexp", null),
                new Option(null, OptionFlags.None, 2,
                    Index.Invalid, "-subst", null),
                new Option(null, OptionFlags.None, Index.Invalid,
                    Index.Invalid, "-nocase", null),
                Option.CreateEndOfOptions()
            });
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region [test2] Command Options
        //
        // NOTE: This is for the [test2] command.
        //
        private static OptionDictionary GetTest2Options()
        {
            return new OptionDictionary(
                new IOption[] {
                //
                // NOTE: These are the options that work like
                //       those in "tcltest".
                //
                new Option(null, OptionFlags.MustHaveValue,
                    Index.Invalid, Index.Invalid, "-constraints",
                    null),
                new Option(null, OptionFlags.MustHaveValue,
                    Index.Invalid, Index.Invalid, "-setup", null),
                new Option(null, OptionFlags.MustHaveValue,
                    Index.Invalid, Index.Invalid, "-body", null),
                new Option(null, OptionFlags.MustHaveValue,
                    Index.Invalid, Index.Invalid, "-cleanup", null),
                new Option(null, OptionFlags.MustHaveValue,
                    Index.Invalid, Index.Invalid, "-result", null),
                new Option(null, OptionFlags.MustHaveValue,
                    Index.Invalid, Index.Invalid, "-output", null),
                new Option(null, OptionFlags.NoCase |
                    OptionFlags.MustHaveValue, Index.Invalid,
                    Index.Invalid, "-errorOutput", null),
                new Option(null, OptionFlags.NoCase |
                    OptionFlags.MustHaveReturnCodeListValue,
                    Index.Invalid, Index.Invalid, "-returnCodes",
                    null),
                new Option(null, OptionFlags.NoCase |
                    OptionFlags.MustHaveReturnCodeListValue,
                    Index.Invalid, Index.Invalid,
                    "-execReturnCodes", null),
                new Option(typeof(ExitCode), OptionFlags.NoCase |
                    OptionFlags.MustHaveEnumValue, Index.Invalid,
                    Index.Invalid, "-exitCode", null),
                new Option(typeof(ExitCode), OptionFlags.NoCase |
                    OptionFlags.MustHaveEnumValue, Index.Invalid,
                    Index.Invalid, "-execExitCode", null),
                new Option(null, OptionFlags.MustHaveMatchModeValue,
                    Index.Invalid, Index.Invalid, "-match",
                    new Variant(
                        StringOps.DefaultResultMatchMode)),
                //
                // NOTE: These are the Eagle specific options.
                //
                new Option(null, OptionFlags.MustHaveBooleanValue |
                    OptionFlags.Unsafe, Index.Invalid,
                    Index.Invalid, "-debug", null),
                new Option(null, OptionFlags.MustHaveBooleanValue |
                    OptionFlags.Unsafe, Index.Invalid,
                    Index.Invalid, "-trace", null),
#if TEST
                new Option(null, OptionFlags.MustHaveBooleanValue |
                    OptionFlags.Unsafe, Index.Invalid,
                    Index.Invalid, "-captureTrace", null),
#else
                new Option(null, OptionFlags.MustHaveBooleanValue |
                    OptionFlags.Unsafe | OptionFlags.Unsupported,
                    Index.Invalid, Index.Invalid, "-captureTrace",
                    null),
#endif
                new Option(null, OptionFlags.MustHaveBooleanValue,
                    Index.Invalid, Index.Invalid, "-time", null),
                new Option(null, OptionFlags.MustHaveBooleanValue,
                    Index.Invalid, Index.Invalid, "-once", null),
                new Option(null, OptionFlags.MustHaveValue |
                    OptionFlags.Unsafe, Index.Invalid,
                    Index.Invalid, "-text", null),
                new Option(null, OptionFlags.MustHaveListValue |
                    OptionFlags.Unsafe, Index.Invalid,
                    Index.Invalid, "-argv", null),
                new Option(null, OptionFlags.MustHaveIntegerValue |
                    OptionFlags.Unsafe, Index.Invalid,
                    Index.Invalid, "-timeout", null),
                //
                // NOTE: These are the "NoCase" options.
                //
                new Option(null, OptionFlags.NoCase |
                    OptionFlags.MustHaveRuleSetValue |
                    OptionFlags.CouldBePath | OptionFlags.Unsafe,
                    Index.Invalid, Index.Invalid, "-ruleSet", null),
                new Option(null, OptionFlags.NoCase |
                    OptionFlags.MustHaveBooleanValue, Index.Invalid,
                    Index.Invalid, "-noCase", null),
                new Option(null, OptionFlags.NoCase |
                    OptionFlags.MustHaveBooleanValue, Index.Invalid,
                    Index.Invalid, "-visibleSpace", null),
                new Option(typeof(RegexOptions), OptionFlags.NoCase |
                    OptionFlags.MustHaveEnumValue |
                    OptionFlags.Unsafe, Index.Invalid,
                    Index.Invalid, "-regExOptions",
                    new Variant(TestOps.RegExOptions)),
                new Option(null, OptionFlags.NoCase |
                    OptionFlags.MustHaveValue, Index.Invalid,
                    Index.Invalid, "-constraintExpression", null),
                new Option(null, OptionFlags.NoCase |
                    OptionFlags.MustHaveIntegerValue |
                    OptionFlags.Unsafe, Index.Invalid,
                    Index.Invalid, "-repeatCount", null),
                new Option(null, OptionFlags.NoCase |
                    OptionFlags.MustHaveBooleanValue |
                    OptionFlags.Unsafe, Index.Invalid,
                    Index.Invalid, "-noCleanup", null),
                new Option(null, OptionFlags.NoCase |
                    OptionFlags.MustHaveBooleanValue |
                    OptionFlags.Unsafe, Index.Invalid,
                    Index.Invalid, "-noCancel", null),
                new Option(null, OptionFlags.NoCase |
                    OptionFlags.MustHaveBooleanValue |
                    OptionFlags.Unsafe, Index.Invalid,
                    Index.Invalid, "-globalCancel", null),
                new Option(null, OptionFlags.NoCase |
                    OptionFlags.MustHaveBooleanValue |
                    OptionFlags.Unsafe, Index.Invalid,
                    Index.Invalid, "-noData", null),
                new Option(null, OptionFlags.NoCase |
                    OptionFlags.MustHaveBooleanValue, Index.Invalid,
                    Index.Invalid, "-noEvent", null),
                new Option(null, OptionFlags.NoCase |
                    OptionFlags.MustHaveBooleanValue, Index.Invalid,
                    Index.Invalid, "-noExit", null),
                new Option(null, OptionFlags.NoCase |
                    OptionFlags.MustHaveBooleanValue |
                    OptionFlags.Unsafe, Index.Invalid,
                    Index.Invalid, "-noHalt", null),
                new Option(null, OptionFlags.NoCase |
                    OptionFlags.MustHaveBooleanValue |
                    OptionFlags.Unsafe, Index.Invalid,
                    Index.Invalid, "-noProcessId", null),
                new Option(null, OptionFlags.NoCase |
                    OptionFlags.MustHaveBooleanValue |
                    OptionFlags.Unsafe, Index.Invalid,
                    Index.Invalid, "-noStatistics", null),
                new Option(null, OptionFlags.NoCase |
                    OptionFlags.MustHaveBooleanValue |
                    OptionFlags.Unsafe, Index.Invalid,
                    Index.Invalid, "-noSecurity", null),
                new Option(null, OptionFlags.NoCase |
                    OptionFlags.MustHaveBooleanValue |
                    OptionFlags.Unsafe, Index.Invalid,
                    Index.Invalid, "-noTrack", null),
                new Option(null, OptionFlags.MustHaveMatchModeValue,
                    Index.Invalid, Index.Invalid, "-ignoreMatch",
                    new Variant(
                        StringOps.DefaultResultMatchMode)),
                new Option(null, OptionFlags.NoCase |
                    OptionFlags.MustHaveListValue, Index.Invalid,
                    Index.Invalid, "-ignorePatterns", null),
                new Option(null, OptionFlags.NoCase |
                    OptionFlags.MustHaveValue | OptionFlags.Unsafe,
                    Index.Invalid, Index.Invalid, "-libraryPath",
                    null),
#if ISOLATED_INTERPRETERS
                new Option(null, OptionFlags.NoCase |
                    OptionFlags.MustHaveValue | OptionFlags.Unsafe,
                    Index.Invalid, Index.Invalid, "-packagePath",
                    null),
                new Option(null, OptionFlags.NoCase |
                    OptionFlags.MustHaveValue | OptionFlags.Unsafe,
                    Index.Invalid, Index.Invalid, "-baseDirectory",
                    null),
                new Option(null, OptionFlags.NoCase |
                    OptionFlags.MustHaveBooleanValue |
                    OptionFlags.Unsafe, Index.Invalid,
                    Index.Invalid, "-useBasePath", null),
                new Option(null, OptionFlags.NoCase |
                    OptionFlags.MustHaveBooleanValue |
                    OptionFlags.Unsafe, Index.Invalid,
                    Index.Invalid, "-verifyCoreAssembly", null),
                new Option(null, OptionFlags.NoCase |
                    OptionFlags.MustHaveBooleanValue |
                    OptionFlags.Unsafe, Index.Invalid,
                    Index.Invalid, "-useEntryAssembly", null),
                new Option(null, OptionFlags.NoCase |
                    OptionFlags.MustHaveBooleanValue |
                    OptionFlags.Unsafe, Index.Invalid,
                    Index.Invalid, "-optionalEntryAssembly", null),
#else
                new Option(null, OptionFlags.NoCase |
                    OptionFlags.MustHaveValue | OptionFlags.Unsafe |
                    OptionFlags.Unsupported, Index.Invalid,
                    Index.Invalid, "-packagePath", null),
                new Option(null, OptionFlags.NoCase |
                    OptionFlags.MustHaveValue | OptionFlags.Unsafe |
                    OptionFlags.Unsupported, Index.Invalid,
                    Index.Invalid, "-baseDirectory", null),
                new Option(null, OptionFlags.NoCase |
                    OptionFlags.MustHaveBooleanValue |
                    OptionFlags.Unsafe | OptionFlags.Unsupported,
                    Index.Invalid, Index.Invalid, "-useBasePath",
                    null),
                new Option(null, OptionFlags.NoCase |
                    OptionFlags.MustHaveBooleanValue |
                    OptionFlags.Unsafe | OptionFlags.Unsupported,
                    Index.Invalid, Index.Invalid,
                    "-verifyCoreAssembly", null),
                new Option(null, OptionFlags.NoCase |
                    OptionFlags.MustHaveBooleanValue |
                    OptionFlags.Unsafe | OptionFlags.Unsupported,
                    Index.Invalid, Index.Invalid,
                    "-useEntryAssembly", null),
                new Option(null, OptionFlags.NoCase |
                    OptionFlags.MustHaveBooleanValue |
                    OptionFlags.Unsafe | OptionFlags.Unsupported,
                    Index.Invalid, Index.Invalid,
                    "-optionalEntryAssembly", null),
#endif
                new Option(null, OptionFlags.NoCase |
                    OptionFlags.MustHaveListValue |
                    OptionFlags.Unsafe, Index.Invalid,
                    Index.Invalid, "-autoPath", null),
                new Option(
                    typeof(Eagle._Components.Public.IsolationLevel),
                    OptionFlags.NoCase |
                    OptionFlags.MustHaveEnumValue |
                    OptionFlags.Unsafe, Index.Invalid,
                    Index.Invalid, "-isolationLevel",
                    new Variant(
                        Eagle._Components.Public.IsolationLevel.Default)),
                new Option(typeof(IsolationDetail),
                    OptionFlags.NoCase |
                    OptionFlags.MustHaveEnumValue |
                    OptionFlags.Unsafe, Index.Invalid,
                    Index.Invalid, "-isolationPassDetail",
                    new Variant(IsolationDetail.Default)),
                new Option(typeof(IsolationDetail),
                    OptionFlags.NoCase |
                    OptionFlags.MustHaveEnumValue |
                    OptionFlags.Unsafe, Index.Invalid,
                    Index.Invalid, "-isolationFailDetail",
                    new Variant(IsolationDetail.Default)),
                new Option(typeof(TestPathType), OptionFlags.NoCase |
                    OptionFlags.MustHaveEnumValue |
                    OptionFlags.Unsafe, Index.Invalid,
                    Index.Invalid, "-isolationPathType",
                    new Variant(TestPathType.Default)),
                new Option(null, OptionFlags.NoCase |
                    OptionFlags.MustHaveBooleanValue |
                    OptionFlags.Unsafe, Index.Invalid,
                    Index.Invalid, "-isolationUnicode", null),
                new Option(null, OptionFlags.NoCase |
                    OptionFlags.MustHaveValue | OptionFlags.Unsafe,
                    Index.Invalid, Index.Invalid,
                    "-isolationTemplate", null),
                new Option(null, OptionFlags.NoCase |
                    OptionFlags.MustHaveListValue |
                    OptionFlags.Unsafe, Index.Invalid,
                    Index.Invalid, "-isolationOtherArguments",
                    null),
                new Option(null, OptionFlags.NoCase |
                    OptionFlags.MustHaveListValue |
                    OptionFlags.Unsafe, Index.Invalid,
                    Index.Invalid, "-isolationLastArguments",
                    null),
                new Option(null, OptionFlags.NoCase |
                    OptionFlags.MustHaveValue | OptionFlags.Unsafe,
                    Index.Invalid, Index.Invalid,
                    "-isolationFileName", null),
                new Option(null, OptionFlags.NoCase |
                    OptionFlags.MustHaveValue | OptionFlags.Unsafe,
                    Index.Invalid, Index.Invalid,
                    "-isolationLogFile", null),
                new Option(null, OptionFlags.MustHaveBooleanValue |
                    OptionFlags.Unsafe, Index.Invalid,
                    Index.Invalid, "-noChangeReturnCode", null),
                new Option(null, OptionFlags.MustHaveBooleanValue |
                    OptionFlags.Unsafe, Index.Invalid,
                    Index.Invalid, "-stopOnHookError", null),
                Option.CreateEndOfOptions()
            });
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

#if NATIVE && TCL
        #region [tcl] Command Options
        //
        // NOTE: This is for the [tcl cancel] sub-command.
        //
        private static OptionDictionary GetTclCancelOptions()
        {
            return new OptionDictionary(
                new IOption[] {
                new Option(null, OptionFlags.None, Index.Invalid,
                    Index.Invalid, "-unwind", null),
                Option.CreateEndOfOptions()
            });
        }

        ///////////////////////////////////////////////////////////////////////

        //
        // NOTE: This is for the [tcl create] sub-command.
        //
        private static OptionDictionary GetTclCreateOptions()
        {
            return new OptionDictionary(
                new IOption[] {
                new Option(null, OptionFlags.None, Index.Invalid,
                    Index.Invalid, "-alias", null),
                new Option(null, OptionFlags.None, Index.Invalid,
                    Index.Invalid, "-noinitialize", null),
                new Option(null, OptionFlags.None, Index.Invalid,
                    Index.Invalid, "-memory", null),
                new Option(null, OptionFlags.None, Index.Invalid,
                    Index.Invalid, "-safe", null),
                new Option(null, OptionFlags.None, Index.Invalid,
                    Index.Invalid, "-nobridge", null),
                new Option(null, OptionFlags.None, Index.Invalid,
                    Index.Invalid, "-noforcedelete", null),
                new Option(null, OptionFlags.None, Index.Invalid,
                    Index.Invalid, "-nocomplain", null),
                Option.CreateEndOfOptions()
            });
        }

        ///////////////////////////////////////////////////////////////////////

        //
        // NOTE: This is for the [tcl evaluate] sub-command.
        //       Options are handled by ObjectOps.GetEvaluateOptions.
        //

        ///////////////////////////////////////////////////////////////////////

        //
        // NOTE: This is for the [tcl expr] sub-command.
        //
        private static OptionDictionary GetTclExprOptions()
        {
            return new OptionDictionary(
                new IOption[] {
                new Option(null, OptionFlags.None, Index.Invalid,
                    Index.Invalid, "-time", null),
                new Option(null, OptionFlags.MustHaveBooleanValue,
                    Index.Invalid, Index.Invalid, "-exceptions",
                    null),
                Option.CreateEndOfOptions()
            });
        }

        ///////////////////////////////////////////////////////////////////////

        //
        // NOTE: This is for the [tcl find] (and [tcl available])
        //       sub-command.
        //
        private static OptionDictionary GetTclFindOptions(
            Interpreter interpreter /* in */
            )
        {
            FindFlags findFlags = (interpreter != null) ?
                interpreter.TclFindFlags : FindFlags.None;

            return new OptionDictionary(
                new IOption[] {
                new Option(typeof(FindFlags),
                    OptionFlags.MustHaveEnumValue, Index.Invalid,
                    Index.Invalid, "-flags",
                    new Variant(findFlags)),
                new Option(null, OptionFlags.None, Index.Invalid,
                    Index.Invalid, "-robustify", null),
                new Option(null, OptionFlags.None, Index.Invalid,
                    Index.Invalid, "-architecture", null),
                new Option(null, OptionFlags.None, Index.Invalid,
                    Index.Invalid, "-trustedonly", null),
                new Option(null, OptionFlags.None, Index.Invalid,
                    Index.Invalid, "-maybetrustedonly", null),
                new Option(null, OptionFlags.None, Index.Invalid,
                    Index.Invalid, "-verbose", null),
                new Option(null, OptionFlags.MustHaveValue,
                    Index.Invalid, Index.Invalid, "-eval", null),
                new Option(null, OptionFlags.None, Index.Invalid,
                    Index.Invalid, "-full", null),
                new Option(null,
                    OptionFlags.MustHaveVersionValue,
                    Index.Invalid, Index.Invalid,
                    "-minimumversion", null),
                new Option(null,
                    OptionFlags.MustHaveVersionValue,
                    Index.Invalid, Index.Invalid,
                    "-maximumversion", null),
                new Option(null,
                    OptionFlags.MustHaveVersionValue,
                    Index.Invalid, Index.Invalid,
                    "-unknownversion", null),
                new Option(null, OptionFlags.MustHaveValue,
                    Index.Invalid, Index.Invalid, "-errorsvar",
                    null),
                Option.CreateEndOfOptions()
            });
        }

        ///////////////////////////////////////////////////////////////////////

        //
        // NOTE: This is for the [tcl command create] (a.k.a.
        //       [tcl interp create]) sub-command.
        //
        private static OptionDictionary GetTclInterpCreateOptions()
        {
            return new OptionDictionary(
                new IOption[] {
                new Option(null, OptionFlags.None, Index.Invalid,
                    Index.Invalid, "-noforcedelete", null),
                new Option(null, OptionFlags.None, Index.Invalid,
                    Index.Invalid, "-nocomplain", null),
                Option.CreateEndOfOptions()
            });
        }

        ///////////////////////////////////////////////////////////////////////

        //
        // NOTE: This is for the [tcl load] sub-command.
        //
        private static OptionDictionary GetTclLoadOptions(
            Interpreter interpreter /* in */
            )
        {
            FindFlags findFlags = (interpreter != null) ?
                interpreter.TclFindFlags : FindFlags.None;

            LoadFlags loadFlags = (interpreter != null) ?
                interpreter.TclLoadFlags : LoadFlags.None;

            return new OptionDictionary(
                new IOption[] {
                new Option(typeof(FindFlags),
                    OptionFlags.MustHaveEnumValue, Index.Invalid,
                    Index.Invalid, "-findflags",
                    new Variant(findFlags)),
                new Option(typeof(LoadFlags),
                    OptionFlags.MustHaveEnumValue, Index.Invalid,
                    Index.Invalid, "-loadflags",
                    new Variant(loadFlags)),
                new Option(null, OptionFlags.None, Index.Invalid,
                    Index.Invalid, "-robustify", null),
                new Option(null, OptionFlags.None, Index.Invalid,
                    Index.Invalid, "-trustedonly", null),
                new Option(null, OptionFlags.None, Index.Invalid,
                    Index.Invalid, "-maybetrustedonly", null),
                new Option(null, OptionFlags.MustHaveValue,
                    Index.Invalid, Index.Invalid, "-eval", null),
                new Option(null, OptionFlags.None, Index.Invalid,
                    Index.Invalid, "-bridge", null),
                new Option(null, OptionFlags.None, Index.Invalid,
                    Index.Invalid, "-noforcedelete", null),
                new Option(null, OptionFlags.None, Index.Invalid,
                    Index.Invalid, "-nocomplain", null),
                new Option(null,
                    OptionFlags.MustHaveVersionValue,
                    Index.Invalid, Index.Invalid,
                    "-minimumversion", null),
                new Option(null,
                    OptionFlags.MustHaveVersionValue,
                    Index.Invalid, Index.Invalid,
                    "-maximumversion", null),
                new Option(null,
                    OptionFlags.MustHaveVersionValue,
                    Index.Invalid, Index.Invalid,
                    "-unknownversion", null),
                Option.CreateEndOfOptions()
            });
        }

        ///////////////////////////////////////////////////////////////////////

        //
        // NOTE: This is for the [tcl queue] sub-command.
        //
        private static OptionDictionary GetTclQueueOptions()
        {
            return new OptionDictionary(
                new IOption[] {
                new Option(typeof(EventType),
                    OptionFlags.MustHaveEnumValue, Index.Invalid,
                    Index.Invalid, "-eventtype",
                    new Variant(EventType.Evaluate)),
                new Option(typeof(EventFlags),
                    OptionFlags.MustHaveEnumValue, Index.Invalid,
                    Index.Invalid, "-eventflags",
                    new Variant(EventFlags.None)),
                new Option(null, OptionFlags.MustHaveBooleanValue,
                    Index.Invalid, Index.Invalid, "-exceptions",
                    null),
                new Option(null, OptionFlags.MustHaveBooleanValue,
                    Index.Invalid, Index.Invalid, "-synchronous",
                    null),
                new Option(null,
                    OptionFlags.MustHaveObjectValue,
                    Index.Invalid, Index.Invalid, "-data", null),
                Option.CreateEndOfOptions()
            });
        }

        ///////////////////////////////////////////////////////////////////////

        //
        // NOTE: This is for the [tcl recordandeval] sub-command.
        //
        private static OptionDictionary GetTclRecordAndEvalOptions()
        {
            return new OptionDictionary(
                new IOption[] {
                new Option(null, OptionFlags.None, Index.Invalid,
                    Index.Invalid, "-time", null),
                new Option(null, OptionFlags.MustHaveBooleanValue,
                    Index.Invalid, Index.Invalid, "-exceptions",
                    null),
                Option.CreateEndOfOptions()
            });
        }

        ///////////////////////////////////////////////////////////////////////

        //
        // NOTE: This is for the [tcl resetcancel] sub-command.
        //
        private static OptionDictionary GetTclResetCancelOptions()
        {
            return new OptionDictionary(
                new IOption[] {
                new Option(null, OptionFlags.None, Index.Invalid,
                    Index.Invalid, "-children", null),
                new Option(null, OptionFlags.None, Index.Invalid,
                    Index.Invalid, "-force", null),
                Option.CreateEndOfOptions()
            });
        }

        ///////////////////////////////////////////////////////////////////////

        //
        // NOTE: This is for the [tcl select] sub-command.
        //
        private static OptionDictionary GetTclSelectOptions(
            Interpreter interpreter /* in */
            )
        {
            FindFlags findFlags = (interpreter != null) ?
                interpreter.TclFindFlags : FindFlags.None;

            return new OptionDictionary(
                new IOption[] {
                new Option(typeof(FindFlags),
                    OptionFlags.MustHaveEnumValue, Index.Invalid,
                    Index.Invalid, "-flags",
                    new Variant(findFlags)),
                new Option(null, OptionFlags.None, Index.Invalid,
                    Index.Invalid, "-robustify", null),
                new Option(null, OptionFlags.None, Index.Invalid,
                    Index.Invalid, "-architecture", null),
                new Option(null, OptionFlags.None, Index.Invalid,
                    Index.Invalid, "-trustedonly", null),
                new Option(null, OptionFlags.None, Index.Invalid,
                    Index.Invalid, "-maybetrustedonly", null),
                new Option(null, OptionFlags.None, Index.Invalid,
                    Index.Invalid, "-verbose", null),
                new Option(null, OptionFlags.MustHaveValue,
                    Index.Invalid, Index.Invalid, "-eval", null),
                new Option(null,
                    OptionFlags.MustHaveVersionValue,
                    Index.Invalid, Index.Invalid,
                    "-minimumversion", null),
                new Option(null,
                    OptionFlags.MustHaveVersionValue,
                    Index.Invalid, Index.Invalid,
                    "-maximumversion", null),
                new Option(null,
                    OptionFlags.MustHaveVersionValue,
                    Index.Invalid, Index.Invalid,
                    "-unknownversion", null),
                new Option(null, OptionFlags.MustHaveValue,
                    Index.Invalid, Index.Invalid, "-errorsvar",
                    null),
                new Option(null, OptionFlags.None, Index.Invalid,
                    Index.Invalid, "-allerrors", null),
                Option.CreateEndOfOptions()
            });
        }

        ///////////////////////////////////////////////////////////////////////

        //
        // NOTE: This is for the [tcl source] sub-command.
        //
        private static OptionDictionary GetTclSourceOptions()
        {
            return new OptionDictionary(
                new IOption[] {
                new Option(null, OptionFlags.None, Index.Invalid,
                    Index.Invalid, "-time", null),
                new Option(null, OptionFlags.MustHaveBooleanValue,
                    Index.Invalid, Index.Invalid, "-exceptions",
                    null),
                Option.CreateEndOfOptions()
            });
        }

        ///////////////////////////////////////////////////////////////////////

        //
        // NOTE: This is for the [tcl subst] sub-command.
        //
        private static OptionDictionary GetTclSubstOptions()
        {
            return new OptionDictionary(
                new IOption[] {
                new Option(null, OptionFlags.None, Index.Invalid,
                    Index.Invalid, "-nobackslashes", null),
                new Option(null, OptionFlags.None, Index.Invalid,
                    Index.Invalid, "-nocommands", null),
                new Option(null, OptionFlags.None, Index.Invalid,
                    Index.Invalid, "-novariables", null),
                new Option(null, OptionFlags.None, Index.Invalid,
                    Index.Invalid, "-time", null),
                new Option(null, OptionFlags.MustHaveBooleanValue,
                    Index.Invalid, Index.Invalid, "-exceptions",
                    null),
                Option.CreateEndOfOptions()
            });
        }

        ///////////////////////////////////////////////////////////////////////

        //
        // NOTE: This is for the [tcl update] sub-command.
        //
        private static OptionDictionary GetTclUpdateOptions()
        {
            return new OptionDictionary(
                new IOption[] {
                new Option(null,
                    OptionFlags.MustHaveIntegerValue,
                    Index.Invalid, Index.Invalid, "-timeout", null),
                new Option(null, OptionFlags.None, Index.Invalid,
                    Index.Invalid, "-wait", null),
                new Option(null, OptionFlags.None, Index.Invalid,
                    Index.Invalid, "-all", null),
                new Option(null, OptionFlags.None, Index.Invalid,
                    Index.Invalid, "-nocomplain", null),
                Option.CreateEndOfOptions()
            });
        }

        ///////////////////////////////////////////////////////////////////////

        //
        // NOTE: This is for the [tcl versionrange] sub-command.
        //
        private static OptionDictionary GetTclVersionRangeOptions(
            Interpreter interpreter /* in */
            )
        {
            FindFlags findFlags = (interpreter != null) ?
                interpreter.TclFindFlags : FindFlags.None;

            return new OptionDictionary(
                new IOption[] {
                new Option(typeof(FindFlags),
                    OptionFlags.MustHaveEnumValue, Index.Invalid,
                    Index.Invalid, "-flags",
                    new Variant(findFlags)),
                new Option(null, OptionFlags.None, Index.Invalid,
                    Index.Invalid, "-robustify", null),
                new Option(null, OptionFlags.None, Index.Invalid,
                    Index.Invalid, "-trustedonly", null),
                new Option(null, OptionFlags.None, Index.Invalid,
                    Index.Invalid, "-maybetrustedonly", null),
                new Option(null,
                    OptionFlags.MustHaveVersionValue,
                    Index.Invalid, Index.Invalid,
                    "-minimumversion", null),
                new Option(null,
                    OptionFlags.MustHaveVersionValue,
                    Index.Invalid, Index.Invalid,
                    "-maximumversion", null),
                new Option(null,
                    OptionFlags.MustHaveIntegerValue,
                    Index.Invalid, Index.Invalid,
                    "-majorincrement", null),
                new Option(null,
                    OptionFlags.MustHaveIntegerValue,
                    Index.Invalid, Index.Invalid,
                    "-minorincrement", null),
                new Option(null,
                    OptionFlags.MustHaveIntegerValue,
                    Index.Invalid, Index.Invalid,
                    "-intermediateminimum", null),
                new Option(null,
                    OptionFlags.MustHaveIntegerValue,
                    Index.Invalid, Index.Invalid,
                    "-intermediatemaximum", null),
                Option.CreateEndOfOptions()
            });
        }
        #endregion
#endif

        ///////////////////////////////////////////////////////////////////////

        #region [test] Command Options (Default.cs)
#if TEST
        //
        // NOTE: This is for the TestCreateWithRulesCommandCallback
        //       in Default.cs.
        //
        private static OptionDictionary GetTestCreateWithRulesOptions()
        {
            return new OptionDictionary(
                new IOption[] {
                new Option(null,
                    OptionFlags.MustHaveUnsignedWideIntegerValue |
                    OptionFlags.Unsafe, Index.Invalid,
                    Index.Invalid, "-token", null),
                new Option(null, OptionFlags.MustHaveBooleanValue |
                    OptionFlags.Unsafe, Index.Invalid,
                    Index.Invalid, "-namespaces", null),
                new Option(null, OptionFlags.MustHaveBooleanValue |
                    OptionFlags.Unsafe, Index.Invalid,
                    Index.Invalid, "-safe", null),
                new Option(null, OptionFlags.MustHaveBooleanValue |
                    OptionFlags.Unsafe, Index.Invalid,
                    Index.Invalid, "-isolated", null),
                new Option(null, OptionFlags.MustHaveBooleanValue |
                    OptionFlags.Unsafe, Index.Invalid,
                    Index.Invalid, "-security", null),
                Option.CreateEndOfOptions()
            });
        }
#endif
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region [time] Command Options
        //
        // NOTE: This is for the [time] command.
        //
        private static OptionDictionary GetTimeOptions()
        {
            return new OptionDictionary(
                new IOption[] {
                new Option(null, OptionFlags.Unsafe |
                    OptionFlags.MustHaveIntegerValue, Index.Invalid,
                    Index.Invalid, "-timeout", null),
                new Option(null, OptionFlags.MustHaveBooleanValue,
                    Index.Invalid, Index.Invalid, "-statistics",
                    null),
                new Option(null, OptionFlags.Unsafe |
                    OptionFlags.NoCase |
                    OptionFlags.MustHaveBooleanValue, Index.Invalid,
                    Index.Invalid, "-breakOk", null),
                new Option(null, OptionFlags.Unsafe |
                    OptionFlags.NoCase |
                    OptionFlags.MustHaveBooleanValue, Index.Invalid,
                    Index.Invalid, "-errorOk", null),
                new Option(null, OptionFlags.Unsafe |
                    OptionFlags.NoCase |
                    OptionFlags.MustHaveBooleanValue, Index.Invalid,
                    Index.Invalid, "-noCancel", null),
                new Option(null, OptionFlags.Unsafe |
                    OptionFlags.NoCase |
                    OptionFlags.MustHaveBooleanValue, Index.Invalid,
                    Index.Invalid, "-globalCancel", null),
                new Option(null, OptionFlags.Unsafe |
                    OptionFlags.NoCase |
                    OptionFlags.MustHaveBooleanValue, Index.Invalid,
                    Index.Invalid, "-noHalt", null),
                new Option(null, OptionFlags.Unsafe |
                    OptionFlags.NoCase |
                    OptionFlags.MustHaveBooleanValue, Index.Invalid,
                    Index.Invalid, "-noEvent", null),
                new Option(null, OptionFlags.Unsafe |
                    OptionFlags.NoCase |
                    OptionFlags.MustHaveBooleanValue, Index.Invalid,
                    Index.Invalid, "-noExit", null),
                Option.CreateEndOfOptions()
            });
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region [unload] Command Options
        //
        // NOTE: This is for the [unload] command.
        //
        private static OptionDictionary GetUnloadOptions()
        {
            return new OptionDictionary(
                new IOption[] {
                new Option(null, OptionFlags.MustHaveObjectValue,
                    Index.Invalid, Index.Invalid, "-clientdata",
                    null),
                new Option(null, OptionFlags.MustHaveObjectValue,
                    Index.Invalid, Index.Invalid, "-data", null),
                new Option(null, OptionFlags.None, Index.Invalid,
                    Index.Invalid, "-nocase", null),
                new Option(null, OptionFlags.None, Index.Invalid,
                    Index.Invalid, "-keeplibrary", null),
                new Option(null, OptionFlags.None, Index.Invalid,
                    Index.Invalid, "-nocomplain", null),
                new Option(null, OptionFlags.MustHaveMatchModeValue,
                    Index.Invalid, Index.Invalid, "-match",
                    new Variant(StringOps.DefaultUnloadMatchMode)),
                Option.CreateEndOfOptions()
            });
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region [unset] Command Options
        //
        // NOTE: This is for the [unset] command.
        //
        private static OptionDictionary GetUnsetOptions()
        {
            return new OptionDictionary(
                new IOption[] {
                new Option(null, OptionFlags.None, Index.Invalid,
                    Index.Invalid, "-nocomplain", null),
                new Option(null, OptionFlags.Unsafe, Index.Invalid,
                    Index.Invalid, "-unlinkonly", null),
                new Option(null, OptionFlags.Unsafe, Index.Invalid,
                    Index.Invalid, "-remove", null),
                new Option(null, OptionFlags.Unsafe, Index.Invalid,
                    Index.Invalid, "-notrace", null),
                new Option(null, OptionFlags.Unsafe, Index.Invalid,
                    Index.Invalid, "-purge", null),
#if !MONO && NATIVE && WINDOWS
                new Option(null, OptionFlags.Unsafe, Index.Invalid,
                    Index.Invalid, "-zerostring", null),
                new Option(null, OptionFlags.Unsafe, Index.Invalid,
                    Index.Invalid, "-maybezerostring", null),
#else
                new Option(null, OptionFlags.Unsafe |
                    OptionFlags.Unsupported, Index.Invalid,
                    Index.Invalid, "-zerostring", null),
                new Option(null, OptionFlags.Unsafe |
                    OptionFlags.Ignored, Index.Invalid,
                    Index.Invalid, "-maybezerostring", null),
#endif
                Option.CreateEndOfOptions()
            });
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region [uri] Command Options
        //
        // NOTE: This is for the [uri compare] sub-command.
        //
        private static OptionDictionary GetUriCompareOptions()
        {
            return new OptionDictionary(
                new IOption[] {
                new Option(typeof(UriKind),
                    OptionFlags.MustHaveEnumValue, Index.Invalid,
                    Index.Invalid, "-kind", null),
                new Option(typeof(UriComponents),
                    OptionFlags.MustHaveEnumValue, Index.Invalid,
                    Index.Invalid, "-components",
                    new Variant(UriComponents.AbsoluteUri)),
                new Option(typeof(UriFormat),
                    OptionFlags.MustHaveEnumValue, Index.Invalid,
                    Index.Invalid, "-format", null),
                new Option(typeof(StringComparison),
                    OptionFlags.MustHaveEnumValue, Index.Invalid,
                    Index.Invalid, "-comparison", null),
                new Option(null, OptionFlags.None, Index.Invalid,
                    Index.Invalid, "-nocase", null),
                Option.CreateEndOfOptions()
            });
        }

        ///////////////////////////////////////////////////////////////////////

        //
        // NOTE: This is for the [uri create] sub-command.
        //
        private static OptionDictionary GetUriCreateOptions()
        {
            return new OptionDictionary(
                new IOption[] {
                new Option(null, OptionFlags.MustHaveValue,
                    Index.Invalid, Index.Invalid, "-username", null),
                new Option(null, OptionFlags.MustHaveValue,
                    Index.Invalid, Index.Invalid, "-password", null),
                new Option(null, OptionFlags.MustHaveIntegerValue,
                    Index.Invalid, Index.Invalid, "-port", null),
                new Option(null, OptionFlags.MustHaveValue,
                    Index.Invalid, Index.Invalid, "-path", null),
                new Option(null, OptionFlags.MustHaveValue,
                    Index.Invalid, Index.Invalid, "-query", null),
                new Option(null, OptionFlags.MustHaveValue,
                    Index.Invalid, Index.Invalid, "-fragment", null),
                Option.CreateEndOfOptions()
            });
        }

        ///////////////////////////////////////////////////////////////////////

#if NETWORK
        //
        // NOTE: This is for the [uri get] sub-command.
        //
        private static OptionDictionary GetUriGetOptions()
        {
            return new OptionDictionary(
                new IOption[] {
                new Option(typeof(TimeoutType),
                    OptionFlags.Unsafe |
                    OptionFlags.MustHaveEnumValue, Index.Invalid,
                    Index.Invalid, "-timeouttype", null),
                new Option(null, OptionFlags.Unsafe |
                    OptionFlags.MustHaveIntegerValue, Index.Invalid,
                    Index.Invalid, "-retries", null),
                new Option(null, OptionFlags.Unsafe |
                    OptionFlags.MustHaveIntegerValue, Index.Invalid,
                    Index.Invalid, "-timeout", null),
                new Option(null, OptionFlags.Unsafe |
                    OptionFlags.MustHaveListValue, Index.Invalid,
                    Index.Invalid, "-callback", null),
                new Option(typeof(CallbackFlags),
                    OptionFlags.Unsafe |
                    OptionFlags.MustHaveEnumValue, Index.Invalid,
                    Index.Invalid, "-callbackflags",
                    new Variant(CallbackFlags.Default)),
                new Option(null, OptionFlags.Unsafe, Index.Invalid,
                    Index.Invalid, "-inline", null),
                new Option(null, OptionFlags.Unsafe, Index.Invalid,
                    Index.Invalid, "-noinline", null),
                new Option(null, OptionFlags.Unsafe, Index.Invalid,
                    Index.Invalid, "-trusted", null),
#if TEST
                new Option(null, OptionFlags.Unsafe, Index.Invalid,
                    Index.Invalid, "-yesprotocol", null),
                new Option(null, OptionFlags.Unsafe, Index.Invalid,
                    Index.Invalid, "-noprotocol", null),
                new Option(null, OptionFlags.Unsafe, Index.Invalid,
                    Index.Invalid, "-obsolete", null),
#else
                new Option(null, OptionFlags.Unsafe |
                    OptionFlags.Unsupported, Index.Invalid,
                    Index.Invalid, "-yesprotocol", null),
                new Option(null, OptionFlags.Unsafe |
                    OptionFlags.Unsupported, Index.Invalid,
                    Index.Invalid, "-noprotocol", null),
                new Option(null, OptionFlags.Unsafe |
                    OptionFlags.Unsupported, Index.Invalid,
                    Index.Invalid, "-obsolete", null),
#endif
                new Option(typeof(EncodingType),
                    OptionFlags.Unsafe |
                    OptionFlags.MustHaveEnumValue, Index.Invalid,
                    Index.Invalid, "-encodingtype", null),
                new Option(null, OptionFlags.Unsafe |
                    OptionFlags.MustHaveEncodingValue, Index.Invalid,
                    Index.Invalid, "-encoding", null),
                new Option(null, OptionFlags.Unsafe |
                    OptionFlags.MustHaveObjectValue, Index.Invalid,
                    Index.Invalid, "-webclientdata", null),
                Option.CreateEndOfOptions()
            });
        }

        ///////////////////////////////////////////////////////////////////////

        //
        // NOTE: This is for the [uri post] sub-command.
        //
        private static OptionDictionary GetUriPostOptions()
        {
            return new OptionDictionary(
                new IOption[] {
                new Option(typeof(TimeoutType),
                    OptionFlags.Unsafe |
                    OptionFlags.MustHaveEnumValue, Index.Invalid,
                    Index.Invalid, "-timeouttype", null),
                new Option(null, OptionFlags.Unsafe |
                    OptionFlags.MustHaveIntegerValue, Index.Invalid,
                    Index.Invalid, "-retries", null),
                new Option(null, OptionFlags.Unsafe |
                    OptionFlags.MustHaveIntegerValue, Index.Invalid,
                    Index.Invalid, "-timeout", null),
                new Option(null, OptionFlags.Unsafe |
                    OptionFlags.MustHaveValue, Index.Invalid,
                    Index.Invalid, "-method", null),
                new Option(null, OptionFlags.MustHaveListValue,
                    Index.Invalid, Index.Invalid, "-data", null),
                new Option(null, OptionFlags.Unsafe |
                    OptionFlags.MustHaveListValue, Index.Invalid,
                    Index.Invalid, "-callback", null),
                new Option(typeof(CallbackFlags),
                    OptionFlags.Unsafe |
                    OptionFlags.MustHaveEnumValue, Index.Invalid,
                    Index.Invalid, "-callbackflags",
                    new Variant(CallbackFlags.Default)),
                new Option(null, OptionFlags.Unsafe, Index.Invalid,
                    Index.Invalid, "-inline", null),
                new Option(null, OptionFlags.Unsafe, Index.Invalid,
                    Index.Invalid, "-noinline", null),
                new Option(null, OptionFlags.Unsafe, Index.Invalid,
                    Index.Invalid, "-raw", null),
                new Option(null, OptionFlags.Unsafe, Index.Invalid,
                    Index.Invalid, "-trusted", null),
#if TEST
                new Option(null, OptionFlags.Unsafe, Index.Invalid,
                    Index.Invalid, "-yesprotocol", null),
                new Option(null, OptionFlags.Unsafe, Index.Invalid,
                    Index.Invalid, "-noprotocol", null),
                new Option(null, OptionFlags.Unsafe, Index.Invalid,
                    Index.Invalid, "-obsolete", null),
#else
                new Option(null, OptionFlags.Unsafe |
                    OptionFlags.Unsupported, Index.Invalid,
                    Index.Invalid, "-yesprotocol", null),
                new Option(null, OptionFlags.Unsafe |
                    OptionFlags.Unsupported, Index.Invalid,
                    Index.Invalid, "-noprotocol", null),
                new Option(null, OptionFlags.Unsafe |
                    OptionFlags.Unsupported, Index.Invalid,
                    Index.Invalid, "-obsolete", null),
#endif
                new Option(typeof(EncodingType),
                    OptionFlags.Unsafe |
                    OptionFlags.MustHaveEnumValue, Index.Invalid,
                    Index.Invalid, "-encodingtype", null),
                new Option(null, OptionFlags.Unsafe |
                    OptionFlags.MustHaveEncodingValue, Index.Invalid,
                    Index.Invalid, "-encoding", null),
                new Option(null, OptionFlags.Unsafe |
                    OptionFlags.MustHaveObjectValue, Index.Invalid,
                    Index.Invalid, "-webclientdata", null),
                Option.CreateEndOfOptions()
            });
        }
#endif
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region [vwait] Command Options
        //
        // NOTE: This is for the [vwait] command.
        //
        private static OptionDictionary GetVwaitOptions(
            Interpreter interpreter /* in */
            )
        {
            EventWaitFlags eventWaitFlags = (interpreter != null) ?
                interpreter.EventWaitFlags : EventWaitFlags.None;

            VariableFlags variableFlags = (interpreter != null) ?
                interpreter.EventVariableFlags : VariableFlags.None;

            return new OptionDictionary(
                new IOption[] {
                new Option(null,
                    OptionFlags.MustHaveObjectValue |
                    OptionFlags.Unsafe, Index.Invalid,
                    Index.Invalid, "-handle", null),
                new Option(typeof(EventWaitFlags),
                    OptionFlags.MustHaveEnumValue |
                    OptionFlags.Unsafe, Index.Invalid,
                    Index.Invalid, "-eventwaitflags",
                    new Variant(eventWaitFlags)),
                new Option(typeof(VariableFlags),
                    OptionFlags.MustHaveEnumValue |
                    OptionFlags.Unsafe, Index.Invalid,
                    Index.Invalid, "-variableflags",
                    new Variant(variableFlags)),
                new Option(null,
                    OptionFlags.MustHaveWideIntegerValue |
                    OptionFlags.Unsafe, Index.Invalid,
                    Index.Invalid, "-thread", null),
                new Option(null,
                    OptionFlags.MustHaveIntegerValue |
                    OptionFlags.Unsafe, Index.Invalid,
                    Index.Invalid, "-limit", null),
                new Option(null,
                    OptionFlags.MustHaveIntegerValue |
                    OptionFlags.Unsafe, Index.Invalid,
                    Index.Invalid, "-timeout", null),
                new Option(null, OptionFlags.Unsafe, Index.Invalid,
                    Index.Invalid, "-clear", null),
                new Option(null, OptionFlags.Unsafe, Index.Invalid,
                    Index.Invalid, "-force", null),
                new Option(null, OptionFlags.Unsafe, Index.Invalid,
                    Index.Invalid, "-nocomplain", null),
                new Option(null, OptionFlags.Unsafe, Index.Invalid,
                    Index.Invalid, "-leaveresult", null),
                new Option(null, OptionFlags.Unsafe |
                    OptionFlags.Restricted, Index.Invalid,
                    Index.Invalid, "-resetcancel", null),
                new Option(null, OptionFlags.MustHaveValue |
                    OptionFlags.Unsafe, Index.Invalid,
                    Index.Invalid, "-locked", null),
                Option.CreateEndOfOptions()
            });
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region [xml] Command Options
        //
        // NOTE: This is for the [xml foreach] sub-command.
        //
        private static OptionDictionary GetXmlForEachOptions()
        {
            return new OptionDictionary(
                new IOption[] {
                new Option(null, OptionFlags.None, Index.Invalid,
                    Index.Invalid, "-file", null),
                new Option(null,
                    OptionFlags.MustHaveDictionaryValue,
                    Index.Invalid, Index.Invalid, "-namespaces",
                    null),
                new Option(null, OptionFlags.MustHaveListValue,
                    Index.Invalid, Index.Invalid, "-xpaths", null)
            }, ObjectOps.GetObjectOptions(ObjectOptionType.FixupReturnValue));
        }
        #endregion
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Command Option Dispatch Methods
        public static OptionDictionary GetCommandOptions(
            ArgumentList arguments /* in */
            )
        {
            if (arguments == null)
                return null;

            StringBuilder builder = StringBuilderFactory.Create();

            foreach (Argument argument in arguments)
            {
                if (argument == null)
                    continue;

                if (builder.Length > 0)
                    builder.Append(Characters.Underscore);

                builder.Append(argument);
            }

            object enumValue = EnumOps.TryParse(typeof(CommandOptionType),
                StringBuilderCache.GetStringAndRelease(ref builder), false,
                true);

            if (!(enumValue is CommandOptionType))
                return null;

            return GetCommandOptions((CommandOptionType)enumValue);
        }

        ///////////////////////////////////////////////////////////////////////

        public static OptionDictionary GetCommandOptions(
            CommandOptionType commandOptionType /* in */
            )
        {
            return GetCommandOptions(
                commandOptionType, null, null, null, null, null, null, null);
        }

        ///////////////////////////////////////////////////////////////////////

        public static OptionDictionary GetCommandOptions(
            CommandOptionType commandOptionType, /* in */
            Interpreter interpreter              /* in */
            )
        {
            return GetCommandOptions(
                commandOptionType, interpreter, null, null, null, null, null,
                null);
        }

        ///////////////////////////////////////////////////////////////////////

        public static OptionDictionary GetCommandOptions(
            CommandOptionType commandOptionType,  /* in */
            Interpreter interpreter,              /* in */
            EngineFlags? engineFlags,             /* in */
            ScriptFlags? scriptFlags,             /* in */
            ValueFlags? valueFlags,               /* in */
            PackageIndexFlags? packageIndexFlags, /* in */
            EventFlags? eventFlags,               /* in */
            bool? not                             /* in */
            )
        {
            switch (commandOptionType)
            {
                case CommandOptionType.None:
                case CommandOptionType.Invalid:
                    return null;
                case CommandOptionType.After_Idle:
                    return GetAfterIdleOptions();
                case CommandOptionType.After_Info:
                    return GetAfterInfoOptions();
                case CommandOptionType.Array_Copy:
                    return GetArrayCopyOptions();
                case CommandOptionType.Array_Random:
                    return GetArrayRandomOptions();
                case CommandOptionType.Base64_Decode:
                    return GetBase64DecodeOptions();
                case CommandOptionType.Base64_Encode:
                    return GetBase64EncodeOptions();
#if CALLBACK_QUEUE
                case CommandOptionType.Callback_Dequeue:
                    return ObjectOps.GetObjectOptions(
                        ObjectOptionType.Dequeue);
#endif
                case CommandOptionType.Clock_Days:
                    return GetClockDaysOptions();
                case CommandOptionType.Clock_Clicks:
                    return GetClockClicksOptions();
                case CommandOptionType.Clock_Duration:
                    return GetClockDurationOptions();
                case CommandOptionType.Clock_FileTime:
                    return GetClockFileTimeOptions();
                case CommandOptionType.Clock_Format:
                    return GetClockFormatOptions();
                case CommandOptionType.Clock_Now:
                    return GetClockNowOptions();
                case CommandOptionType.Clock_Scan:
                    return GetClockScanOptions();
#if DEBUGGER
                case CommandOptionType.Debug_Break:
                    return GetDebugBreakOptions();
                case CommandOptionType.Debug_Emergency:
                    return GetDebugEmergencyOptions();
#if TEST
                case CommandOptionType.Debug_Hook:
                    return GetDebugHookOptions();
#endif
                case CommandOptionType.Debug_Iqueue:
                    return GetDebugIqueueOptions();
                case CommandOptionType.Debug_Log:
                    return GetDebugLogOptions();
                case CommandOptionType.Debug_SecureEval:
                    return GetDebugSecureEvalOptions();
                case CommandOptionType.Debug_Set:
                    return GetDebugSetOptions();
#if SHELL
                case CommandOptionType.Debug_Shell:
                    return GetDebugShellOptions();
#endif
                case CommandOptionType.Debug_Subst:
                    return GetDebugSubstOptions();
                case CommandOptionType.Debug_Trace:
                    return GetDebugTraceOptions();
                case CommandOptionType.Debug_Variable:
                    return GetDebugVariableOptions();
#endif
#if PREVIOUS_RESULT
                case CommandOptionType.Debug_Exception:
                    return ObjectOps.GetObjectOptions(
                        ObjectOptionType.Exception);
#endif
                case CommandOptionType.Debugger_Dsubst:
                    return GetDebuggerDsubstOptions();
                case CommandOptionType.Debugger_Overr:
                    return GetDebuggerOverrOptions();
                case CommandOptionType.Exec:
                    return ObjectOps.GetObjectOptions(
                        ObjectOptionType.Exec);
                case CommandOptionType.Exit:
                    return GetExitOptions();
                case CommandOptionType.Fconfigure_Set:
                    return GetFconfigureSetOptions();
                case CommandOptionType.Fconfigure_Query:
                    return GetFconfigureQueryOptions();
                case CommandOptionType.Fcopy:
                    return GetFcopyOptions(interpreter);
                case CommandOptionType.File_Cleanup:
                    return GetFileCleanupOptions();
                case CommandOptionType.File_Copy:
                    return GetFileCopyOptions();
                case CommandOptionType.File_Delete:
                    return GetFileDeleteOptions();
                case CommandOptionType.File_Glob:
                    return GetFileGlobOptions();
                case CommandOptionType.File_Information:
                    return GetFileInformationOptions();
                case CommandOptionType.File_Normalize:
                    return GetFileNormalizeOptions();
                case CommandOptionType.File_ObjectId:
                    return GetFileObjectIdOptions();
                case CommandOptionType.File_Rename:
                    return GetFileRenameOptions();
#if !NET_STANDARD_20 && !MONO
                case CommandOptionType.File_Sddl:
                    return GetFileSddlOptions();
#endif
                case CommandOptionType.File_Under:
                    return GetFileUnderOptions();
                case CommandOptionType.File_Version:
                    return GetFileVersionOptions();
                case CommandOptionType.Gets:
                    return GetGetsOptions();
                case CommandOptionType.Glob:
                    return GetGlobOptions();
                case CommandOptionType.Hash_Keyed:
                    return GetHashKeyedOptions();
                case CommandOptionType.Hash_Mac:
                    return GetHashMacOptions();
                case CommandOptionType.Hash_Normal:
                    return GetHashNormalOptions();
                case CommandOptionType.Host_Beep:
                    return GetHostBeepOptions();
                case CommandOptionType.Host_Color:
                    return GetHostColorOptions();
#if CONSOLE && NATIVE && WINDOWS
                case CommandOptionType.Host_Font:
                    return GetHostFontOptions();
#endif
                case CommandOptionType.Host_NamedColor:
                    return GetHostNamedColorOptions();
                case CommandOptionType.Host_Position:
                    return GetHostPositionOptions();
                case CommandOptionType.Host_Reset:
                    return GetHostResetOptions();
                case CommandOptionType.Host_Size:
                    return GetHostSizeOptions();
                case CommandOptionType.Host_WriteBox:
                    return GetHostWriteBoxOptions();
                case CommandOptionType.Info_Commands:
                    return GetInfoCommandsOptions();
                case CommandOptionType.Info_Functions:
                    return GetInfoFunctionsOptions();
                case CommandOptionType.Info_Loaded:
                    return GetInfoLoadedOptions();
                case CommandOptionType.Info_Operators:
                    return GetInfoOperatorsOptions();
                case CommandOptionType.Info_SubCommands:
                    return GetInfoSubCommandsOptions();
                case CommandOptionType.Info_Vars:
                    return GetInfoVarsOptions();
                case CommandOptionType.Interp_AddCommands:
                    return GetInterpAddCommandsOptions();
                case CommandOptionType.Interp_Cancel:
                    return GetInterpCancelOptions();
                case CommandOptionType.Interp_Create:
                    return GetInterpCreateOptions();
                case CommandOptionType.Interp_InvokeHidden:
                    return GetInterpInvokeHiddenOptions();
                case CommandOptionType.Interp_Policy:
                    return GetInterpPolicyOptions();
                case CommandOptionType.Interp_Queue:
                    return GetInterpQueueOptions();
                case CommandOptionType.Interp_ReadOrGetScriptFile:
                    return GetInterpReadOrGetScriptFileOptions(
                        scriptFlags, engineFlags);
                case CommandOptionType.Interp_Rename:
                    return GetInterpRenameOptions();
                case CommandOptionType.Interp_ResetCancel:
                    return GetInterpResetCancelOptions();
                case CommandOptionType.Interp_Service:
                    return GetInterpServiceOptions(eventFlags);
                case CommandOptionType.Interp_Source:
                    return GetInterpSourceOptions();
                case CommandOptionType.Interp_Stub:
                    return GetInterpStubOptions();
                case CommandOptionType.Interp_SubCommand:
                    return GetInterpSubCommandOptions();
                case CommandOptionType.Interp_Subst:
                    return GetInterpSubstOptions();
                case CommandOptionType.Kill:
                    return GetKillOptions();
                case CommandOptionType.Library_Call:
                    return ObjectOps.GetObjectOptions(
                        ObjectOptionType.Call);
                case CommandOptionType.Library_Declare:
                    return GetLibraryDeclareOptions();
                case CommandOptionType.Library_Load:
                    return GetLibraryLoadOptions();
                case CommandOptionType.Library_Resolve:
                    return GetLibraryResolveOptions();
                case CommandOptionType.Library_Unresolve:
                    return GetLibraryUnresolveOptions();
                case CommandOptionType.Load:
                    return GetLoadOptions();
                case CommandOptionType.Lsearch:
                    return GetLsearchOptions();
                case CommandOptionType.Lsort:
                    return GetLsortOptions();
                case CommandOptionType.Namespace1_Export:
                    return GetNamespace1ExportOptions();
                case CommandOptionType.Namespace1_Import:
                    return GetNamespace1ImportOptions();
                case CommandOptionType.Namespace1_Which:
                    return GetNamespace1WhichOptions();
                case CommandOptionType.Namespace2_Export:
                    return GetNamespace2ExportOptions();
                case CommandOptionType.Namespace2_Import:
                    return GetNamespace2ImportOptions();
                case CommandOptionType.Namespace2_Which:
                    return GetNamespace2WhichOptions();
                case CommandOptionType.Object_Alias:
                    return ObjectOps.GetObjectOptions(
                        ObjectOptionType.Alias);
                case CommandOptionType.Object_Callback:
                    return ObjectOps.GetObjectOptions(
                        ObjectOptionType.Callback);
                case CommandOptionType.Object_Certificate:
                    return ObjectOps.GetObjectOptions(
                        ObjectOptionType.Certificate);
                case CommandOptionType.Object_Cleanup:
                    return ObjectOps.GetObjectOptions(
                        ObjectOptionType.Cleanup);
                case CommandOptionType.Object_Create:
                    return ObjectOps.GetObjectOptions(
                        ObjectOptionType.Create);
                case CommandOptionType.Object_Declare:
                    return ObjectOps.GetObjectOptions(
                        ObjectOptionType.Declare);
                case CommandOptionType.Object_Dispose:
                    return ObjectOps.GetObjectOptions(
                        ObjectOptionType.Dispose);
                case CommandOptionType.Object_FixupReturnValue:
                    return ObjectOps.GetObjectOptions(
                        ObjectOptionType.FixupReturnValue);
                case CommandOptionType.Object_ForEach:
                    return ObjectOps.GetObjectOptions(
                        ObjectOptionType.ForEach);
                case CommandOptionType.Object_Get:
                    return ObjectOps.GetObjectOptions(
                        ObjectOptionType.Get);
                case CommandOptionType.Object_Import:
                    return ObjectOps.GetObjectOptions(
                        ObjectOptionType.Import);
                case CommandOptionType.Object_Invoke:
                    return ObjectOps.GetObjectOptions(
                        ObjectOptionType.Invoke);
                case CommandOptionType.Object_InvokeAll:
                    return ObjectOps.GetObjectOptions(
                        ObjectOptionType.InvokeAll);
                case CommandOptionType.Object_InvokeOnly:
                    return ObjectOps.GetObjectOptions(
                        ObjectOptionType.InvokeOnly);
                case CommandOptionType.Object_InvokeRaw:
                    return ObjectOps.GetObjectOptions(
                        ObjectOptionType.InvokeRaw);
                case CommandOptionType.Object_InvokeRawOnly:
                    return ObjectOps.GetObjectOptions(
                        ObjectOptionType.InvokeRawOnly);
                case CommandOptionType.Object_IsDisposed:
                    return ObjectOps.GetObjectOptions(
                        ObjectOptionType.IsDisposed);
                case CommandOptionType.Object_IsNull:
                    return ObjectOps.GetObjectOptions(
                        ObjectOptionType.IsNull);
                case CommandOptionType.Object_IsOfType:
                    return ObjectOps.GetObjectOptions(
                        ObjectOptionType.IsOfType);
                case CommandOptionType.Object_Load:
                    return ObjectOps.GetObjectOptions(
                        ObjectOptionType.Load);
                case CommandOptionType.Object_Members:
                    return ObjectOps.GetObjectOptions(
                        ObjectOptionType.Members);
                case CommandOptionType.Object_Search:
                    return ObjectOps.GetObjectOptions(
                        ObjectOptionType.Search);
                case CommandOptionType.Object_SimpleCallback:
                    return ObjectOps.GetObjectOptions(
                        ObjectOptionType.SimpleCallback);
                case CommandOptionType.Object_Type:
                    return ObjectOps.GetObjectOptions(
                        ObjectOptionType.Type);
                case CommandOptionType.Object_UnaliasNamespace:
                    return ObjectOps.GetObjectOptions(
                        ObjectOptionType.UnaliasNamespace);
                case CommandOptionType.Object_Undeclare:
                    return ObjectOps.GetObjectOptions(
                        ObjectOptionType.Undeclare);
                case CommandOptionType.Object_Unimport:
                    return ObjectOps.GetObjectOptions(
                        ObjectOptionType.Unimport);
                case CommandOptionType.Object_VerifyAll:
                    return GetObjectVerifyAllOptions();
                case CommandOptionType.Open:
                    return GetOpenOptions();
                case CommandOptionType.Package_Absent:
                    return GetPackageAbsentOptions();
                case CommandOptionType.Package_Alias:
                    return GetPackageAliasOptions();
                case CommandOptionType.Package_Present:
                    return GetPackagePresentOptions();
                case CommandOptionType.Package_Require:
                    return GetPackageRequireOptions();
                case CommandOptionType.Package_ScanPreOptions:
                    return GetPackageScanPreOptionsMethod();
                case CommandOptionType.Package_Scan:
                    return GetPackageScanOptions(packageIndexFlags);
                case CommandOptionType.Parse_Command:
                    return GetParseCommandOptions(interpreter);
                case CommandOptionType.Parse_Expression:
                    return GetParseExpressionOptions(interpreter);
                case CommandOptionType.Parse_Options:
                    return GetParseOptionsOptions();
                case CommandOptionType.Parse_Script:
                    return GetParseScriptOptions(interpreter);
                case CommandOptionType.Puts:
                    return GetPutsOptions();
                case CommandOptionType.Read:
                    return ObjectOps.GetObjectOptions(
                        ObjectOptionType.Read);
                case CommandOptionType.Read_Only:
                    return ObjectOps.GetObjectOptions(
                        ObjectOptionType.ReadOnly);
                case CommandOptionType.Regexp:
                    return GetRegexpOptions();
                case CommandOptionType.Regsub:
                    return GetRegsubOptions();
                case CommandOptionType.Rename:
                    return GetRenameOptions();
                case CommandOptionType.Return:
                    return GetReturnOptions();
                case CommandOptionType.Scope_Close:
                    return GetScopeCloseOptions();
                case CommandOptionType.Scope_Create:
                    return GetScopeCreateOptions();
                case CommandOptionType.Scope_Eval:
                    return GetScopeEvalOptions(interpreter);
                case CommandOptionType.Scope_Global:
                    return GetScopeGlobalOptions();
                case CommandOptionType.Scope_Lock:
                    return GetScopeLockOptions();
                case CommandOptionType.Scope_Open:
                    return GetScopeOpenOptions();
                case CommandOptionType.Scope_Unlock:
                    return GetScopeUnlockOptions();
                case CommandOptionType.Scope_Update:
                    return GetScopeUpdateOptions();
                case CommandOptionType.Socket:
                    return GetSocketOptions();
                case CommandOptionType.Source:
                    return GetSourceOptions();
                case CommandOptionType.Split:
                    return GetSplitOptions();
                case CommandOptionType.Sql_OpenPreOptions:
#if DATA
                    return GetSqlOpenPreOptionsMethod();
                case CommandOptionType.Sql_Open:
                    return GetSqlOpenOptions(valueFlags);
                case CommandOptionType.Sql_Execute:
                    return ObjectOps.GetObjectOptions(
                        ObjectOptionType.SqlExecute);
                case CommandOptionType.Sql_Transaction:
                    return GetSqlTransactionOptions();
#endif
                case CommandOptionType.String_Equal:
                    return GetStringEqualOptions();
                case CommandOptionType.String_Ends:
                    return GetStringEndsOptions();
                case CommandOptionType.String_First:
                    return GetStringFirstOptions();
                case CommandOptionType.String_Format:
                    return GetStringFormatOptions(interpreter);
                case CommandOptionType.String_Is:
                    return GetStringIsOptions(not);
                case CommandOptionType.String_Last:
                    return GetStringLastOptions();
                case CommandOptionType.String_Map:
                    return GetStringMapOptions();
                case CommandOptionType.String_Match:
                    return GetStringMatchOptions();
                case CommandOptionType.String_Starts:
                    return GetStringStartsOptions();
                case CommandOptionType.String_ToUpper:
                    return GetStringToUpperOptions();
                case CommandOptionType.Subst:
                    return GetSubstOptions();
                case CommandOptionType.Switch:
                    return GetSwitchOptions();
#if NATIVE && TCL
                case CommandOptionType.Tcl_Cancel:
                    return GetTclCancelOptions();
                case CommandOptionType.Tcl_Create:
                    return GetTclCreateOptions();
                case CommandOptionType.Tcl_Evaluate:
                    return ObjectOps.GetObjectOptions(
                        ObjectOptionType.Evaluate);
                case CommandOptionType.Tcl_Expr:
                    return GetTclExprOptions();
                case CommandOptionType.Tcl_Find:
                    return GetTclFindOptions(interpreter);
                case CommandOptionType.Tcl_InterpCreate:
                    return GetTclInterpCreateOptions();
                case CommandOptionType.Tcl_Load:
                    return GetTclLoadOptions(interpreter);
                case CommandOptionType.Tcl_Queue:
                    return GetTclQueueOptions();
                case CommandOptionType.Tcl_RecordAndEval:
                    return GetTclRecordAndEvalOptions();
                case CommandOptionType.Tcl_ResetCancel:
                    return GetTclResetCancelOptions();
                case CommandOptionType.Tcl_Select:
                    return GetTclSelectOptions(interpreter);
                case CommandOptionType.Tcl_Source:
                    return GetTclSourceOptions();
                case CommandOptionType.Tcl_Subst:
                    return GetTclSubstOptions();
                case CommandOptionType.Tcl_Update:
                    return GetTclUpdateOptions();
                case CommandOptionType.Tcl_VersionRange:
                    return GetTclVersionRangeOptions(interpreter);
#endif
                case CommandOptionType.Test2:
                    return GetTest2Options();
#if TEST
                case CommandOptionType.Test_CreateWithRules:
                    return GetTestCreateWithRulesOptions();
#endif
                case CommandOptionType.Time:
                    return GetTimeOptions();
                case CommandOptionType.Unload:
                    return GetUnloadOptions();
                case CommandOptionType.Unset:
                    return GetUnsetOptions();
                case CommandOptionType.Uri_Compare:
                    return GetUriCompareOptions();
                case CommandOptionType.Uri_Create:
                    return GetUriCreateOptions();
#if NETWORK
                case CommandOptionType.Uri_Get:
                    return GetUriGetOptions();
                case CommandOptionType.Uri_Post:
                    return GetUriPostOptions();
#endif
                case CommandOptionType.Vwait:
                    return GetVwaitOptions(interpreter);
                case CommandOptionType.Xml_ForEach:
                    return GetXmlForEachOptions();
#if XML && SERIALIZATION
                case CommandOptionType.Xml_Deserialize:
                    return ObjectOps.GetObjectOptions(
                        ObjectOptionType.Deserialize);
                case CommandOptionType.Xml_Serialize:
                    return ObjectOps.GetObjectOptions(
                        ObjectOptionType.Serialize);
#endif
                default:
                    return null;
            }
        }
        #endregion
    }
}
