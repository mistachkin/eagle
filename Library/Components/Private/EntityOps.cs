/*
 * EntityOps.cs --
 *
 * Copyright (c) 2007-2012 by Joe Mistachkin.  All rights reserved.
 *
 * See the file "license.terms" for information on usage and redistribution of
 * this file, and for a DISCLAIMER OF ALL WARRANTIES.
 *
 * RCS: @(#) $Id: $
 */

using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Text;
using System.Threading;
using Eagle._Attributes;
using Eagle._Components.Public;
using Eagle._Constants;
using Eagle._Containers.Public;
using Eagle._Interfaces.Private;
using Eagle._Interfaces.Public;

namespace Eagle._Components.Private
{
    /// <summary>
    /// This class provides a collection of static helper methods for querying
    /// and manipulating the flags, names, tokens, identifiers, and other
    /// metadata of the various entity types (e.g. commands, functions,
    /// procedures, operators, variables, plugins, packages, wrappers, etc.)
    /// used throughout the Eagle core library.
    /// </summary>
    [ObjectId("c0e69f4b-fe35-44aa-8798-3080a85e6614")]
    internal static class EntityOps
    {
        #region Callback Checking Methods
        /// <summary>
        /// This method determines whether the specified callback is marked as read-only.
        /// </summary>
        /// <param name="callback">
        /// The callback to check. This value may be null.
        /// </param>
        /// <returns>
        /// True if the callback is non-null and has the read-only flag set; otherwise, false.
        /// </returns>
        public static bool IsReadOnly(
            ICallback callback
            )
        {
            return (callback != null) ?
                FlagOps.HasFlags(callback.CallbackFlags,
                    CallbackFlags.ReadOnly, true) : false;
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Object Support Methods
        /// <summary>
        /// This method attempts to query the object flags for the specified object, catching and ignoring any exception that may be thrown.
        /// </summary>
        /// <param name="object">
        /// The object whose flags will be queried. This value may be null.
        /// </param>
        /// <returns>
        /// The object flags for the specified object, or <see cref="ObjectFlags.None" /> if they cannot be queried.
        /// </returns>
        public static ObjectFlags GetFlagsNoThrow(
            IObject @object
            )
        {
            if (@object != null)
            {
                try
                {
                    return @object.ObjectFlags; /* throw */
                }
                catch
                {
                    // do nothing.
                }
            }

            return ObjectFlags.None;
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Event Support Methods
        /// <summary>
        /// This method attempts to query the flags for the specified event, catching and ignoring any exception that may be thrown.
        /// </summary>
        /// <param name="event">
        /// The event whose flags will be queried. This value may be null.
        /// </param>
        /// <returns>
        /// The flags for the specified event, or <see cref="EventFlags.None" /> if they cannot be queried.
        /// </returns>
        public static EventFlags GetFlagsNoThrow(
            IEvent @event
            )
        {
            if (@event != null)
            {
                try
                {
                    return @event.Flags; /* throw */
                }
                catch
                {
                    // do nothing.
                }
            }

            return EventFlags.None;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method attempts to convert the specified event to a list of name/value pairs, catching and ignoring any exception that may be thrown.
        /// </summary>
        /// <param name="event">
        /// The event to convert. This value may be null.
        /// </param>
        /// <returns>
        /// The list of name/value pairs for the specified event, or null if it cannot be produced.
        /// </returns>
        public static StringPairList ToListNoThrow(
            IEvent @event
            )
        {
            if (@event != null)
            {
                try
                {
                    return @event.ToList(); /* throw */
                }
                catch
                {
                    // do nothing.
                }
            }

            return null;
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Plugin Support Methods
        /// <summary>
        /// This method attempts to query the flags for the specified plugin, catching and ignoring any exception that may be thrown.
        /// </summary>
        /// <param name="pluginData">
        /// The plugin data whose flags will be queried. This value may be null.
        /// </param>
        /// <returns>
        /// The flags for the specified plugin, or <see cref="PluginFlags.None" /> if they cannot be queried.
        /// </returns>
        public static PluginFlags GetFlagsNoThrow(
            IPluginData pluginData
            )
        {
            if (pluginData != null)
            {
                try
                {
                    return pluginData.Flags; /* throw */
                }
                catch
                {
                    // do nothing.
                }
            }

            return PluginFlags.None;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method attempts to query the application domain for the specified plugin, catching and ignoring any exception that may be thrown.
        /// </summary>
        /// <param name="pluginData">
        /// The plugin data to query. This value may be null.
        /// </param>
        /// <returns>
        /// The application domain associated with the specified plugin, or null if it cannot be queried.
        /// </returns>
        public static AppDomain GetAppDomainNoThrow(
            IPluginData pluginData
            )
        {
            if (pluginData != null)
            {
                try
                {
                    return pluginData.AppDomain; /* throw */
                }
                catch
                {
                    // do nothing.
                }
            }

            return null;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method attempts to query the simple (short) assembly name for the specified plugin, catching and ignoring any exception that may be thrown.
        /// </summary>
        /// <param name="pluginData">
        /// The plugin data to query. This value may be null.
        /// </param>
        /// <returns>
        /// The simple assembly name for the specified plugin, or null if it cannot be queried.
        /// </returns>
        public static string GetSimpleAssemblyNameNoThrow(
            IPluginData pluginData
            )
        {
            if (pluginData != null)
            {
                try
                {
                    AssemblyName assemblyName = pluginData.AssemblyName; /* throw */

                    if (assemblyName != null)
                        return assemblyName.Name;
                }
                catch
                {
                    // do nothing.
                }
            }

            return null;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method sets or clears the licensed flag on the specified plugin.
        /// </summary>
        /// <param name="pluginData">
        /// The plugin data to modify. This value may be null.
        /// </param>
        /// <param name="licensed">
        /// Non-zero to set the licensed flag; zero to clear it.
        /// </param>
        /// <returns>
        /// True if the plugin was non-null and its flags were modified; otherwise, false.
        /// </returns>
        public static bool SetLicensed(
            IPluginData pluginData,
            bool licensed
            )
        {
            if (pluginData != null)
            {
                if (licensed)
                    pluginData.Flags |= PluginFlags.Licensed;
                else
                    pluginData.Flags &= ~PluginFlags.Licensed;

                return true;
            }
            else
            {
                return false;
            }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Package Support Methods
        /// <summary>
        /// This method attempts to query the flags for the specified package, catching and ignoring any exception that may be thrown.
        /// </summary>
        /// <param name="packageData">
        /// The package data whose flags will be queried. This value may be null.
        /// </param>
        /// <returns>
        /// The flags for the specified package, or <see cref="PackageFlags.None" /> if they cannot be queried.
        /// </returns>
        public static PackageFlags GetFlagsNoThrow(
            IPackageData packageData
            )
        {
            if (packageData != null)
            {
                try
                {
                    return packageData.Flags; /* throw */
                }
                catch
                {
                    // do nothing.
                }
            }

            return PackageFlags.None;
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Command Support Methods
        /// <summary>
        /// This method attempts to query the flags for the specified command, catching and ignoring any exception that may be thrown.
        /// </summary>
        /// <param name="command">
        /// The command whose flags will be queried. This value may be null.
        /// </param>
        /// <returns>
        /// The flags for the specified command, or <see cref="CommandFlags.None" /> if they cannot be queried.
        /// </returns>
        public static CommandFlags GetFlagsNoThrow(
            ICommand command
            )
        {
            if (command != null)
            {
                try
                {
                    return command.Flags; /* throw */
                }
                catch
                {
                    // do nothing.
                }
            }

            return CommandFlags.None;
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Command Checking Methods
        /// <summary>
        /// This method determines whether the specified command has its breakpoint flag set.
        /// </summary>
        /// <param name="command">
        /// The command to check. This value may be null.
        /// </param>
        /// <returns>
        /// True if the command is non-null and has the breakpoint flag set; otherwise, false.
        /// </returns>
        public static bool HasBreakpoint(
            ICommand command
            )
        {
            return (command != null) ?
                FlagOps.HasFlags(command.Flags,
                    CommandFlags.Breakpoint, true) : false;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method determines whether the specified command is disabled.
        /// </summary>
        /// <param name="command">
        /// The command to check. This value may be null.
        /// </param>
        /// <returns>
        /// True if the command is non-null and has the disabled flag set; otherwise, false.
        /// </returns>
        public static bool IsDisabled(
            ICommand command
            )
        {
            return (command != null) ?
                FlagOps.HasFlags(command.Flags,
                    CommandFlags.Disabled, true) : false;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method determines whether the specified command flags indicate a command that is safe for use by a safe interpreter.
        /// </summary>
        /// <param name="commandFlags">
        /// The command flags to check.
        /// </param>
        /// <returns>
        /// True if the safe flag is set and the unsafe flag is not set; otherwise, false.
        /// </returns>
        public static bool IsSafe(
            CommandFlags commandFlags
            )
        {
            if (!FlagOps.HasFlags(commandFlags, CommandFlags.Safe, true))
                return false;

            if (FlagOps.HasFlags(commandFlags, CommandFlags.Unsafe, true))
                return false;

            return true;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method determines whether the specified command is safe for use by a safe interpreter.
        /// </summary>
        /// <param name="command">
        /// The command to check. This value may be null.
        /// </param>
        /// <returns>
        /// True if the command is non-null and its flags indicate it is safe; otherwise, false.
        /// </returns>
        public static bool IsSafe(
            ICommand command
            )
        {
            return (command != null) ?
                IsSafe(command.Flags) : false;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method determines whether the specified command is hidden.
        /// </summary>
        /// <param name="command">
        /// The command to check. This value may be null.
        /// </param>
        /// <returns>
        /// True if the command is non-null and has the hidden flag set; otherwise, false.
        /// </returns>
        public static bool IsHidden(
            ICommand command
            )
        {
            return (command != null) ?
                FlagOps.HasFlags(command.Flags,
                    CommandFlags.Hidden, true) : false;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method determines whether the specified command has its no-token flag set.
        /// </summary>
        /// <param name="command">
        /// The command to check. This value may be null.
        /// </param>
        /// <returns>
        /// True if the command is non-null and has the no-token flag set; otherwise, false.
        /// </returns>
        public static bool IsNoToken(
            ICommand command
            )
        {
            return (command != null) ?
                FlagOps.HasFlags(command.Flags,
                    CommandFlags.NoToken, true) : false;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method determines whether the specified command has its no-rename flag set.
        /// </summary>
        /// <param name="command">
        /// The command to check. This value may be null.
        /// </param>
        /// <returns>
        /// True if the command is non-null and has the no-rename flag set; otherwise, false.
        /// </returns>
        public static bool IsNoRename(
            ICommand command
            )
        {
            return (command != null) ?
                FlagOps.HasFlags(command.Flags,
                    CommandFlags.NoRename, true) : false;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method determines whether the specified command has its no-remove flag set.
        /// </summary>
        /// <param name="command">
        /// The command to check. This value may be null.
        /// </param>
        /// <returns>
        /// True if the command is non-null and has the no-remove flag set; otherwise, false.
        /// </returns>
        public static bool IsNoRemove(
            ICommand command
            )
        {
            return (command != null) ?
                FlagOps.HasFlags(command.Flags,
                    CommandFlags.NoRemove, true) : false;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method determines whether the specified command has its read-only flag set.
        /// </summary>
        /// <param name="command">
        /// The command to check. This value may be null.
        /// </param>
        /// <returns>
        /// True if the command is non-null and has the read-only flag set; otherwise, false.
        /// </returns>
        public static bool IsReadOnly(
            ICommand command
            )
        {
            return (command != null) ?
                FlagOps.HasFlags(command.Flags,
                    CommandFlags.ReadOnly, true) : false;
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Command Mutator Methods
        /// <summary>
        /// This method sets or clears the breakpoint flag on the specified command.
        /// </summary>
        /// <param name="command">
        /// The command to modify. This value may be null.
        /// </param>
        /// <param name="breakpoint">
        /// Non-zero to set the breakpoint flag; zero to clear it.
        /// </param>
        /// <returns>
        /// True if the command was non-null and its flags were modified; otherwise, false.
        /// </returns>
        public static bool SetBreakpoint(
            ICommand command,
            bool breakpoint
            )
        {
            if (command != null)
            {
                if (breakpoint)
                    command.Flags |= CommandFlags.Breakpoint;
                else
                    command.Flags &= ~CommandFlags.Breakpoint;

                return true;
            }
            else
            {
                return false;
            }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method sets or clears the read-only flag on the specified command.
        /// </summary>
        /// <param name="command">
        /// The command to modify. This value may be null.
        /// </param>
        /// <param name="readOnly">
        /// Non-zero to set the read-only flag; zero to clear it.
        /// </param>
        /// <returns>
        /// True if the command was non-null and its flags were modified; otherwise, false.
        /// </returns>
        public static bool SetReadOnly(
            ICommand command,
            bool readOnly
            )
        {
            if (command != null)
            {
                if (readOnly)
                    command.Flags |= CommandFlags.ReadOnly;
                else
                    command.Flags &= ~CommandFlags.ReadOnly;

                return true;
            }
            else
            {
                return false;
            }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Function Checking Methods
        /// <summary>
        /// This method determines whether the specified function has its breakpoint flag set.
        /// </summary>
        /// <param name="function">
        /// The function to check. This value may be null.
        /// </param>
        /// <returns>
        /// True if the function is non-null and has the breakpoint flag set; otherwise, false.
        /// </returns>
        public static bool HasBreakpoint(
            IFunction function
            )
        {
            return (function != null) ?
                FlagOps.HasFlags(function.Flags,
                    FunctionFlags.Breakpoint, true) : false;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method determines whether the specified function has its no-rename flag set.
        /// </summary>
        /// <param name="function">
        /// The function to check. This value may be null.
        /// </param>
        /// <returns>
        /// True if the function is non-null and has the no-rename flag set; otherwise, false.
        /// </returns>
        public static bool IsNoRename(
            IFunction function
            )
        {
            return (function != null) ?
                FlagOps.HasFlags(function.Flags,
                    FunctionFlags.NoRename, true) : false;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method determines whether the specified function has its no-token flag set.
        /// </summary>
        /// <param name="function">
        /// The function to check. This value may be null.
        /// </param>
        /// <returns>
        /// True if the function is non-null and has the no-token flag set; otherwise, false.
        /// </returns>
        public static bool IsNoToken(
            IFunction function
            )
        {
            return (function != null) ?
                FlagOps.HasFlags(function.Flags,
                    FunctionFlags.NoToken, true) : false;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method determines whether the specified function has its read-only flag set.
        /// </summary>
        /// <param name="function">
        /// The function to check. This value may be null.
        /// </param>
        /// <returns>
        /// True if the function is non-null and has the read-only flag set; otherwise, false.
        /// </returns>
        public static bool IsReadOnly(
            IFunction function
            )
        {
            return (function != null) ?
                FlagOps.HasFlags(function.Flags,
                    FunctionFlags.ReadOnly, true) : false;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method determines whether the specified function is disabled.
        /// </summary>
        /// <param name="function">
        /// The function to check. This value may be null.
        /// </param>
        /// <returns>
        /// True if the function is non-null and has the disabled flag set; otherwise, false.
        /// </returns>
        public static bool IsDisabled(
            IFunction function
            )
        {
            return (function != null) ?
                FlagOps.HasFlags(function.Flags,
                    FunctionFlags.Disabled, true) : false;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method determines whether the specified function is safe for use by a safe interpreter.
        /// </summary>
        /// <param name="function">
        /// The function to check. This value may be null.
        /// </param>
        /// <returns>
        /// True if the function is non-null, has the safe flag set, and does not have the unsafe flag set; otherwise, false.
        /// </returns>
        public static bool IsSafe(
            IFunction function
            )
        {
            if (function != null)
            {
                FunctionFlags flags = function.Flags;

                if (FlagOps.HasFlags(flags, FunctionFlags.Safe, true) &&
                    !FlagOps.HasFlags(flags, FunctionFlags.Unsafe, true))
                {
                    return true;
                }
            }

            return false;
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Function Mutator Methods
        /// <summary>
        /// This method sets or clears the breakpoint flag on the specified function.
        /// </summary>
        /// <param name="function">
        /// The function to modify. This value may be null.
        /// </param>
        /// <param name="breakpoint">
        /// Non-zero to set the breakpoint flag; zero to clear it.
        /// </param>
        /// <returns>
        /// True if the function was non-null and its flags were modified; otherwise, false.
        /// </returns>
        public static bool SetBreakpoint(
            IFunction function,
            bool breakpoint
            )
        {
            if (function != null)
            {
                if (breakpoint)
                    function.Flags |= FunctionFlags.Breakpoint;
                else
                    function.Flags &= ~FunctionFlags.Breakpoint;

                return true;
            }
            else
            {
                return false;
            }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Policy Checking Methods
        /// <summary>
        /// This method determines whether the specified policy has the specified method flags set.
        /// </summary>
        /// <param name="policy">
        /// The policy to check. This value may be null.
        /// </param>
        /// <param name="methodFlags">
        /// The method flags to look for.
        /// </param>
        /// <param name="all">
        /// Non-zero to require that all of the specified flags are set; zero to require only one of them.
        /// </param>
        /// <returns>
        /// True if the policy is non-null and has the specified method flags set; otherwise, false.
        /// </returns>
        public static bool HasFlags(
            IPolicy policy,
            MethodFlags methodFlags,
            bool all
            )
        {
            return (policy != null) ?
                FlagOps.HasFlags(policy.MethodFlags,
                    methodFlags, all) : false;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method determines whether the specified policy is disabled.
        /// </summary>
        /// <param name="policy">
        /// The policy to check. This value may be null.
        /// </param>
        /// <returns>
        /// True if the policy is non-null and has the disabled flag set; otherwise, false.
        /// </returns>
        public static bool IsDisabled(
            IPolicy policy
            )
        {
            return (policy != null) ?
                FlagOps.HasFlags(policy.PolicyFlags,
                    PolicyFlags.Disabled, true) : false;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method determines whether the specified policy has its no-token flag set.
        /// </summary>
        /// <param name="policy">
        /// The policy to check. This value may be null.
        /// </param>
        /// <returns>
        /// True if the policy is non-null and has the no-token flag set; otherwise, false.
        /// </returns>
        public static bool IsNoToken(
            IPolicy policy
            )
        {
            return (policy != null) ?
                FlagOps.HasFlags(policy.PolicyFlags,
                    PolicyFlags.NoToken, true) : false;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method determines whether the specified policy has its read-only flag set.
        /// </summary>
        /// <param name="policy">
        /// The policy to check. This value may be null.
        /// </param>
        /// <returns>
        /// True if the policy is non-null and has the read-only flag set; otherwise, false.
        /// </returns>
        public static bool IsReadOnly(
            IPolicy policy
            )
        {
            return (policy != null) ?
                FlagOps.HasFlags(policy.PolicyFlags,
                    PolicyFlags.ReadOnly, true) : false;
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Trace Checking Methods
        /// <summary>
        /// This method determines whether the specified trace is disabled.
        /// </summary>
        /// <param name="trace">
        /// The trace to check. This value may be null.
        /// </param>
        /// <returns>
        /// True if the trace is non-null and has the disabled flag set; otherwise, false.
        /// </returns>
        public static bool IsDisabled(
            ITrace trace
            )
        {
            return (trace != null) ?
                FlagOps.HasFlags(trace.TraceFlags,
                    TraceFlags.Disabled, true) : false;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method determines whether the specified trace has its no-token flag set.
        /// </summary>
        /// <param name="trace">
        /// The trace to check. This value may be null.
        /// </param>
        /// <returns>
        /// True if the trace is non-null and has the no-token flag set; otherwise, false.
        /// </returns>
        public static bool IsNoToken(
            ITrace trace
            )
        {
            return (trace != null) ?
                FlagOps.HasFlags(trace.TraceFlags,
                    TraceFlags.NoToken, true) : false;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method determines whether the specified trace has its read-only flag set.
        /// </summary>
        /// <param name="trace">
        /// The trace to check. This value may be null.
        /// </param>
        /// <returns>
        /// True if the trace is non-null and has the read-only flag set; otherwise, false.
        /// </returns>
        public static bool IsReadOnly(
            ITrace trace
            )
        {
            return (trace != null) ?
                FlagOps.HasFlags(trace.TraceFlags,
                    TraceFlags.ReadOnly, true) : false;
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Procedure Checking Methods
        /// <summary>
        /// This method determines whether the specified identifier kind represents a procedure or lambda.
        /// </summary>
        /// <param name="kind">
        /// The identifier kind to check.
        /// </param>
        /// <returns>
        /// True if the identifier kind represents a procedure or lambda; otherwise, false.
        /// </returns>
        public static bool IsProcedure(
            IdentifierKind kind
            )
        {
            switch (kind)
            {
                case IdentifierKind.ProcedureData:
                case IdentifierKind.Procedure:
                case IdentifierKind.HiddenProcedure:
                case IdentifierKind.LambdaData:
                case IdentifierKind.Lambda:
                    {
                        return true;
                    }
                default:
                    {
                        return false;
                    }
            }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method determines whether the specified procedure has its breakpoint flag set.
        /// </summary>
        /// <param name="procedure">
        /// The procedure to check. This value may be null.
        /// </param>
        /// <returns>
        /// True if the procedure is non-null and has the breakpoint flag set; otherwise, false.
        /// </returns>
        public static bool HasBreakpoint(
            IProcedure procedure
            )
        {
            return (procedure != null) ?
                FlagOps.HasFlags(procedure.Flags,
                    ProcedureFlags.Breakpoint, true) : false;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method determines whether the specified procedure is disabled.
        /// </summary>
        /// <param name="procedure">
        /// The procedure to check. This value may be null.
        /// </param>
        /// <returns>
        /// True if the procedure is non-null and has the disabled flag set; otherwise, false.
        /// </returns>
        public static bool IsDisabled(
            IProcedure procedure
            )
        {
            return (procedure != null) ?
                FlagOps.HasFlags(procedure.Flags,
                    ProcedureFlags.Disabled, true) : false;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method determines whether the specified procedure is hidden.
        /// </summary>
        /// <param name="procedure">
        /// The procedure to check. This value may be null.
        /// </param>
        /// <returns>
        /// True if the procedure is non-null and has the hidden flag set; otherwise, false.
        /// </returns>
        public static bool IsHidden(
            IProcedure procedure
            )
        {
            return (procedure != null) ?
                FlagOps.HasFlags(procedure.Flags,
                    ProcedureFlags.Hidden, true) : false;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method determines whether the specified procedure has its positional-arguments flag set.
        /// </summary>
        /// <param name="procedure">
        /// The procedure to check. This value may be null.
        /// </param>
        /// <returns>
        /// True if the procedure is non-null and has the positional-arguments flag set; otherwise, false.
        /// </returns>
        public static bool IsPositionalArguments(
            IProcedure procedure
            )
        {
            return (procedure != null) ?
                FlagOps.HasFlags(procedure.Flags,
                    ProcedureFlags.PositionalArguments, true) : false;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method determines whether the specified procedure has its named-arguments flag set.
        /// </summary>
        /// <param name="procedure">
        /// The procedure to check. This value may be null.
        /// </param>
        /// <returns>
        /// True if the procedure is non-null and has the named-arguments flag set; otherwise, false.
        /// </returns>
        public static bool IsNamedArguments(
            IProcedure procedure
            )
        {
            return (procedure != null) ?
                FlagOps.HasFlags(procedure.Flags,
                    ProcedureFlags.NamedArguments, true) : false;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method determines whether the specified procedure has its read-only flag set.
        /// </summary>
        /// <param name="procedure">
        /// The procedure to check. This value may be null.
        /// </param>
        /// <returns>
        /// True if the procedure is non-null and has the read-only flag set; otherwise, false.
        /// </returns>
        public static bool IsReadOnly(
            IProcedure procedure
            )
        {
            return (procedure != null) ?
                FlagOps.HasFlags(procedure.Flags,
                    ProcedureFlags.ReadOnly, true) : false;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method determines whether the specified procedure has its no-replace flag set.
        /// </summary>
        /// <param name="procedure">
        /// The procedure to check. This value may be null.
        /// </param>
        /// <returns>
        /// True if the procedure is non-null and has the no-replace flag set; otherwise, false.
        /// </returns>
        public static bool IsNoReplace(
            IProcedure procedure
            )
        {
            return (procedure != null) ?
                FlagOps.HasFlags(procedure.Flags,
                    ProcedureFlags.NoReplace, true) : false;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method determines whether the specified procedure has its no-rename flag set.
        /// </summary>
        /// <param name="procedure">
        /// The procedure to check. This value may be null.
        /// </param>
        /// <returns>
        /// True if the procedure is non-null and has the no-rename flag set; otherwise, false.
        /// </returns>
        public static bool IsNoRename(
            IProcedure procedure
            )
        {
            return (procedure != null) ?
                FlagOps.HasFlags(procedure.Flags,
                    ProcedureFlags.NoRename, true) : false;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method determines whether the specified procedure has its no-remove flag set.
        /// </summary>
        /// <param name="procedure">
        /// The procedure to check. This value may be null.
        /// </param>
        /// <returns>
        /// True if the procedure is non-null and has the no-remove flag set; otherwise, false.
        /// </returns>
        public static bool IsNoRemove(
            IProcedure procedure
            )
        {
            return (procedure != null) ?
                FlagOps.HasFlags(procedure.Flags,
                    ProcedureFlags.NoRemove, true) : false;
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Procedure Mutator Methods
        /// <summary>
        /// This method sets or clears the breakpoint flag on the specified procedure.
        /// </summary>
        /// <param name="procedure">
        /// The procedure to modify. This value may be null.
        /// </param>
        /// <param name="breakpoint">
        /// Non-zero to set the breakpoint flag; zero to clear it.
        /// </param>
        /// <returns>
        /// True if the procedure was non-null and its flags were modified; otherwise, false.
        /// </returns>
        public static bool SetBreakpoint(
            IProcedure procedure,
            bool breakpoint
            )
        {
            if (procedure != null)
            {
                if (breakpoint)
                    procedure.Flags |= ProcedureFlags.Breakpoint;
                else
                    procedure.Flags &= ~ProcedureFlags.Breakpoint;

                return true;
            }
            else
            {
                return false;
            }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method sets or clears the read-only flag on the specified procedure.
        /// </summary>
        /// <param name="procedure">
        /// The procedure to modify. This value may be null.
        /// </param>
        /// <param name="readOnly">
        /// Non-zero to set the read-only flag; zero to clear it.
        /// </param>
        /// <returns>
        /// True if the procedure was non-null and its flags were modified; otherwise, false.
        /// </returns>
        public static bool SetReadOnly(
            IProcedure procedure,
            bool readOnly
            )
        {
            if (procedure != null)
            {
                if (readOnly)
                    procedure.Flags |= ProcedureFlags.ReadOnly;
                else
                    procedure.Flags &= ~ProcedureFlags.ReadOnly;

                return true;
            }
            else
            {
                return false;
            }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Operator Checking Methods
        /// <summary>
        /// This method determines whether the specified operator has its breakpoint flag set.
        /// </summary>
        /// <param name="operator">
        /// The operator to check. This value may be null.
        /// </param>
        /// <returns>
        /// True if the operator is non-null and has the breakpoint flag set; otherwise, false.
        /// </returns>
        public static bool HasBreakpoint(
            IOperator @operator
            )
        {
            return (@operator != null) ?
                FlagOps.HasFlags(@operator.Flags,
                    OperatorFlags.Breakpoint, true) : false;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method determines whether the specified operator is disabled.
        /// </summary>
        /// <param name="operator">
        /// The operator to check. This value may be null.
        /// </param>
        /// <returns>
        /// True if the operator is non-null and has the disabled flag set; otherwise, false.
        /// </returns>
        public static bool IsDisabled(
            IOperator @operator
            )
        {
            return (@operator != null) ?
                FlagOps.HasFlags(@operator.Flags,
                    OperatorFlags.Disabled, true) : false;
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Operator Mutator Methods
        /// <summary>
        /// This method sets or clears the breakpoint flag on the specified operator.
        /// </summary>
        /// <param name="operator">
        /// The operator to modify. This value may be null.
        /// </param>
        /// <param name="breakpoint">
        /// Non-zero to set the breakpoint flag; zero to clear it.
        /// </param>
        /// <returns>
        /// True if the operator was non-null and its flags were modified; otherwise, false.
        /// </returns>
        public static bool SetBreakpoint(
            IOperator @operator,
            bool breakpoint
            )
        {
            if (@operator != null)
            {
                if (breakpoint)
                    @operator.Flags |= OperatorFlags.Breakpoint;
                else
                    @operator.Flags &= ~OperatorFlags.Breakpoint;

                return true;
            }
            else
            {
                return false;
            }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Sub-Command Checking Methods
        /// <summary>
        /// This method determines whether the specified sub-command is disabled.
        /// </summary>
        /// <param name="subCommand">
        /// The sub-command to check. This value may be null.
        /// </param>
        /// <returns>
        /// True if the sub-command is non-null and has the disabled flag set; otherwise, false.
        /// </returns>
        public static bool IsDisabled(
            ISubCommand subCommand
            )
        {
            return (subCommand != null) ?
                FlagOps.HasFlags(subCommand.CommandFlags,
                    CommandFlags.Disabled, true) : false;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method determines whether the specified sub-command is safe for use by a safe interpreter.
        /// </summary>
        /// <param name="subCommand">
        /// The sub-command to check. This value may be null.
        /// </param>
        /// <returns>
        /// True if the sub-command is non-null, has the safe flag set, and does not have the unsafe flag set; otherwise, false.
        /// </returns>
        public static bool IsSafe(
            ISubCommand subCommand
            )
        {
            if (subCommand != null)
            {
                SubCommandFlags flags = subCommand.Flags;

                if (FlagOps.HasFlags(flags, SubCommandFlags.Safe, true) &&
                    !FlagOps.HasFlags(flags, SubCommandFlags.Unsafe, true))
                {
                    return true;
                }
            }

            return false;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method determines whether the specified sub-command is hidden.
        /// </summary>
        /// <param name="subCommand">
        /// The sub-command to check. This value may be null.
        /// </param>
        /// <returns>
        /// True if the sub-command is non-null and has the hidden flag set; otherwise, false.
        /// </returns>
        public static bool IsHidden(
            ISubCommand subCommand
            )
        {
            return (subCommand != null) ?
                FlagOps.HasFlags(subCommand.CommandFlags,
                    CommandFlags.Hidden, true) : false;
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Variable Support Methods
        /// <summary>
        /// This method strips any leading namespace qualifiers from the specified variable name, adjusting the specified variable flags to force use of the global call frame when the name was fully qualified.
        /// </summary>
        /// <param name="varName">
        /// The variable name to examine. Upon return, this contains the variable name with any leading namespace qualifiers removed.
        /// </param>
        /// <param name="flags">
        /// The variable flags to examine. Upon return, this may have the global-only flag added when the variable name was absolute.
        /// </param>
        public static void GetFlags(
            ref string varName,      /* in, out */
            ref VariableFlags flags  /* in, out */
            )
        {
            bool absolute = false;

            varName = NamespaceOps.TrimLeading(varName, ref absolute);

            if (absolute)
            {
                //
                // NOTE: Set the caller's flags to force them to use the
                //       global call frame for this variable from now on.
                //
                flags |= VariableFlags.GlobalOnly;
            }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method gets the existing value for the specified array element, optionally forcing it to its string representation.
        /// </summary>
        /// <param name="flags">
        /// The variable flags that may control how the value is returned.
        /// </param>
        /// <param name="arrayValue">
        /// The array element dictionary to query. This value may be null.
        /// </param>
        /// <param name="index">
        /// The array element index (key) to look up.
        /// </param>
        /// <param name="default">
        /// The value to return when the element is not present.
        /// </param>
        /// <returns>
        /// The existing element value, or the value of <paramref name="default" /> when the element is not present.
        /// </returns>
        public static object GetOldValue(
            VariableFlags flags,
            ElementDictionary arrayValue,
            string index,
            object @default
            )
        {
            if (arrayValue != null)
            {
                object value;

                if (arrayValue.TryGetValue(index, out value) &&
                    (value != null))
                {
                    if (FlagOps.HasFlags(flags,
                            VariableFlags.ForceToString, true))
                    {
                        return StringOps.GetStringFromObject(value);
                    }

                    return value;
                }
            }

            return @default;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method gets the existing scalar value, optionally forcing it to its string representation.
        /// </summary>
        /// <param name="flags">
        /// The variable flags that may control how the value is returned.
        /// </param>
        /// <param name="value">
        /// The existing value. This value may be null.
        /// </param>
        /// <param name="index">
        /// The array element index, if any. This value may be null.
        /// </param>
        /// <param name="default">
        /// The value to return when no value is present.
        /// </param>
        /// <returns>
        /// The existing value, or the value of <paramref name="default" /> when no value is present.
        /// </returns>
        private static object GetOldValue(
            VariableFlags flags,
            object value,
            string index,
            object @default
            )
        {
            if (value != null)
            {
                if (FlagOps.HasFlags(flags,
                        VariableFlags.ForceToString, true))
                {
                    return StringOps.GetStringFromObject(value);
                }

                return value;
            }

            return @default;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method gets the existing value for the specified variable or one of its array elements.
        /// </summary>
        /// <param name="flags">
        /// The variable flags that may control how the value is returned.
        /// </param>
        /// <param name="variable">
        /// The variable to query. This value may be null.
        /// </param>
        /// <param name="index">
        /// The array element index to look up, or null to query the scalar value.
        /// </param>
        /// <param name="default">
        /// The value to return when no value is present.
        /// </param>
        /// <returns>
        /// The existing value, or the value of <paramref name="default" /> when no value is present.
        /// </returns>
        public static object GetOldValue(
            VariableFlags flags,
            IVariable variable,
            string index,
            object @default
            )
        {
            if (variable != null)
            {
                if (index != null)
                {
                    return GetOldValue(
                        flags, variable.ArrayValue, index, @default);
                }
                else
                {
                    return GetOldValue(
                        flags, variable.Value, index, @default);
                }
            }

            return @default;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method computes the new value for a variable, honoring any append-value or append-element semantics indicated by the specified variable flags.
        /// </summary>
        /// <param name="flags">
        /// The variable flags that control how the new value is combined with the old value.
        /// </param>
        /// <param name="oldValue">
        /// The existing value of the variable. This value may be null.
        /// </param>
        /// <param name="newValue">
        /// The new value to set or append.
        /// </param>
        /// <returns>
        /// The resulting value to store into the variable.
        /// </returns>
        public static object GetNewValue(
            VariableFlags flags,
            object oldValue,
            object newValue
            )
        {
            if (FlagOps.HasFlags(flags, VariableFlags.AppendValue, true))
            {
                StringBuilder value = oldValue as StringBuilder;

                if (value == null)
                    //
                    // BUGBUG: Would discard any non-string "internal rep"
                    //         the old variable value may have had.
                    //
                    value = StringBuilderFactory.CreateNoCache(oldValue as string); /* EXEMPT */

                //
                // TODO: Why doesn't this use GetStringFromObject?
                //
                return value.Append(newValue);
            }
            else if (FlagOps.HasFlags(flags, VariableFlags.AppendElement, true))
            {
                StringList value = oldValue as StringList;

                if (value == null)
                {
                    if (oldValue != null)
                        //
                        // BUGBUG: Would discard any non-string "internal rep"
                        //         the old variable value may have had.
                        //
                        value = new StringList(oldValue as string);
                    else
                        value = new StringList();
                }

                value.Add(StringOps.GetStringFromObject(newValue));

                return value;
            }
            else
            {
                return newValue;
            }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method gets the underlying system array associated with the specified variable, if any.
        /// </summary>
        /// <param name="variable">
        /// The variable to query. This value may be null.
        /// </param>
        /// <returns>
        /// The system array value of the variable, or null if there is none.
        /// </returns>
        public static Array GetSystemArray(
            IVariable variable
            )
        {
            if (variable == null)
                return null;

            return variable.Value as Array;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method extracts the watchpoint-related flags from the specified variable flags.
        /// </summary>
        /// <param name="flags">
        /// The variable flags to mask.
        /// </param>
        /// <returns>
        /// The watchpoint-related flags contained within the specified flags.
        /// </returns>
        public static VariableFlags GetWatchpointFlags(
            VariableFlags flags
            )
        {
            return flags & VariableFlags.WatchpointMask;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method replaces the watchpoint-related flags within the specified variable flags with those from another set of flags.
        /// </summary>
        /// <param name="flags">
        /// The original variable flags.
        /// </param>
        /// <param name="newFlags">
        /// The variable flags supplying the new watchpoint-related flags.
        /// </param>
        /// <returns>
        /// The original flags with their watchpoint-related flags replaced.
        /// </returns>
        public static VariableFlags SetWatchpointFlags(
            VariableFlags flags,
            VariableFlags newFlags
            )
        {
            VariableFlags result = flags;

            result &= ~VariableFlags.WatchpointMask; /* remove old flags */
            result |= GetWatchpointFlags(newFlags);  /* add new flags */

            return result;
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Variable Checking Methods
        /// <summary>
        /// This method follows any chain of variable links starting from the specified variable, returning the final target variable.
        /// </summary>
        /// <param name="variable">
        /// The variable at which to begin following links. This value may be null.
        /// </param>
        /// <param name="flags">
        /// The variable flags that control link following.
        /// </param>
        /// <returns>
        /// The final target variable, or null if the links cannot be followed.
        /// </returns>
        public static IVariable FollowLinks(
            IVariable variable, /* in */
            VariableFlags flags /* in */
            )
        {
            Result error = null;

            return FollowLinks(variable, flags, ref error);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method follows any chain of variable links starting from the specified variable, returning the final target variable.
        /// </summary>
        /// <param name="variable">
        /// The variable at which to begin following links. This value may be null.
        /// </param>
        /// <param name="flags">
        /// The variable flags that control link following.
        /// </param>
        /// <param name="error">
        /// Upon failure, this will contain an appropriate error message.
        /// </param>
        /// <returns>
        /// The final target variable, or null if the links cannot be followed.
        /// </returns>
        public static IVariable FollowLinks(
            IVariable variable,  /* in */
            VariableFlags flags, /* in */
            ref Result error     /* out */
            )
        {
            string linkIndex = null;

            return FollowLinks(
                variable, flags, Count.Invalid, ref linkIndex,
                ref error);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method follows any chain of variable links starting from the specified variable, up to the specified limit, returning the final target variable.
        /// </summary>
        /// <param name="variable">
        /// The variable at which to begin following links. This value may be null.
        /// </param>
        /// <param name="flags">
        /// The variable flags that control link following.
        /// </param>
        /// <param name="limit">
        /// The maximum number of links to follow, or a value less than one for no limit.
        /// </param>
        /// <param name="linkIndex">
        /// Upon return, this will contain the array element index associated with the link, if any.
        /// </param>
        /// <param name="error">
        /// Upon failure, this will contain an appropriate error message.
        /// </param>
        /// <returns>
        /// The final target variable, or null if the links cannot be followed.
        /// </returns>
        public static IVariable FollowLinks(
            IVariable variable,   /* in */
            VariableFlags flags,  /* in */
            int limit,            /* in */
            ref string linkIndex, /* in, out */
            ref Result error      /* out */
            )
        {
            if (variable == null)
            {
                error = "invalid variable";
                return null;
            }

            if (FlagOps.HasFlags(
                    flags, VariableFlags.NoFollowLink, true))
            {
                return variable;
            }

            bool noUsable = false;

            if (FlagOps.HasFlags(
                    flags, VariableFlags.NoUsable, true))
            {
                noUsable = true;
            }

            int count = 0;
            IVariable linkVariable = variable.Link;
            string localLinkIndex = variable.LinkIndex;

            while (linkVariable != null)
            {
                if ((limit > 0) && (count++ >= limit))
                    break;

                Result linkError = null;

                if (!noUsable &&
                    !linkVariable.IsUsable(ref linkError))
                {
                    error = String.Format(
                        "can't follow from {0} to {1}: {2}",
                        FormatOps.ErrorVariableName(variable.Name),
                        FormatOps.ErrorVariableName(linkVariable.Name),
                        FormatOps.DisplayString(linkError));

                    return null;
                }

                variable = linkVariable;

                //
                // BUGBUG: Why does this conditional never
                //         get hit?
                //
                // if ((localLinkIndex == null) &&
                //     EntityOps.IsLink(variable))
                // {
                //     localLinkIndex = variable.LinkIndex;
                // }

                linkVariable = linkVariable.Link;
            }

            linkIndex = localLinkIndex;

            return variable; /* NOTE: Cannot be null at this point. */
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method determines whether the specified variable has any traces.
        /// </summary>
        /// <param name="variable">
        /// The variable to check. This value may be null.
        /// </param>
        /// <returns>
        /// True if the variable is non-null and has one or more traces; otherwise, false.
        /// </returns>
        public static bool HasTraces(
            IVariable variable
            )
        {
            return (variable != null) && variable.HasTraces();
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method determines whether the specified variable refers, via its link chain, to a variable within a usable call frame.
        /// </summary>
        /// <param name="variable">
        /// The variable to check. This value may be null.
        /// </param>
        /// <param name="force">
        /// Non-zero to follow links even when the variable is not itself a link.
        /// </param>
        /// <param name="error">
        /// Upon failure, this will contain an appropriate error message.
        /// </param>
        /// <returns>
        /// True if the variable refers to a variable within a valid, non-disposed call frame; otherwise, false.
        /// </returns>
        public static bool HasValidLink(
            IVariable variable,
            bool force,
            ref Result error
            )
        {
            bool result = false;

            if (variable != null)
            {
                if (force || IsLink(variable))
                {
                    variable = FollowLinks(
                        variable, VariableFlags.None, ref error);

                    if (variable != null)
                    {
                        ICallFrame frame = variable.Frame;

                        if ((frame != null) &&
                            !CallFrameOps.IsDisposedOrUndefined(frame))
                        {
                            result = true;
                        }
                    }
                }
            }

            return result;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method determines whether the specified trace information refers to an array element or array operation.
        /// </summary>
        /// <param name="traceInfo">
        /// The trace information to check. This value may be null.
        /// </param>
        /// <returns>
        /// True if the trace information refers to an array; otherwise, false.
        /// </returns>
        public static bool IsArray(
            ITraceInfo traceInfo
            )
        {
            if (traceInfo == null)
                return false;

            if (traceInfo.Index != null)
                return true;

            if (FlagOps.HasFlags(
                    traceInfo.Flags, VariableFlags.Array, true))
            {
                return true;
            }

            return false;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method determines whether the specified variable is an array that has element data.
        /// </summary>
        /// <param name="variable">
        /// The variable to check. This value may be null.
        /// </param>
        /// <returns>
        /// True if the variable is an array with a non-null element dictionary; otherwise, false.
        /// </returns>
        public static bool IsArray(
            IVariable variable
            )
        {
            if (variable == null)
                return false;

            ElementDictionary arrayValue = null;

            if (!IsArray(variable, ref arrayValue))
                return false;

            return (arrayValue != null);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method determines whether the specified variable is an array and, if so, obtains its element data.
        /// </summary>
        /// <param name="variable">
        /// The variable to check. This value may be null.
        /// </param>
        /// <param name="arrayValue">
        /// Upon success, this will contain the array element dictionary for the variable.
        /// </param>
        /// <returns>
        /// True if the variable is an array with a non-null element dictionary; otherwise, false.
        /// </returns>
        public static bool IsArray(
            IVariable variable,
            ref ElementDictionary arrayValue
            )
        {
            if ((variable != null) && IsArray2(variable))
            {
                arrayValue = variable.ArrayValue;
                return (arrayValue != null);
            }

            return false;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method determines whether the specified variable has its write-only flag set.
        /// </summary>
        /// <param name="variable">
        /// The variable to check. This value may be null.
        /// </param>
        /// <returns>
        /// True if the variable is non-null and has the write-only flag set; otherwise, false.
        /// </returns>
        public static bool IsWriteOnly(
            IVariable variable
            )
        {
            if (variable == null)
                return false;

            return variable.HasFlags(VariableFlags.WriteOnly, true);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method determines whether the specified variable has any of its virtual or system flags set.
        /// </summary>
        /// <param name="variable">
        /// The variable to check. This value may be null.
        /// </param>
        /// <returns>
        /// True if the variable is non-null and has a virtual or system flag set; otherwise, false.
        /// </returns>
        public static bool IsVirtualOrSystem(
            IVariable variable
            )
        {
            if (variable == null)
                return false;

            return variable.HasFlags(VariableFlags.VirtualOrSystemMask, false);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method determines whether the specified variable has its array flag set.
        /// </summary>
        /// <param name="variable">
        /// The variable to check. This value may be null.
        /// </param>
        /// <returns>
        /// True if the variable is non-null and has the array flag set; otherwise, false.
        /// </returns>
        public static bool IsArray2(
            IVariable variable
            )
        {
            if (variable == null)
                return false;

            return variable.HasFlags(VariableFlags.Array, true);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method determines whether the specified variable currently has no value, also reporting whether the variable is an array.
        /// </summary>
        /// <param name="variable">
        /// The variable to check. This value may be null.
        /// </param>
        /// <param name="isArray">
        /// Upon return, this indicates whether the variable is an array, or null when the variable is null.
        /// </param>
        /// <returns>
        /// True if the variable has no value; otherwise, false.
        /// </returns>
        public static bool HasNoValue(
            IVariable variable,
            ref bool? isArray
            )
        {
            if (variable == null)
            {
                isArray = null;
                return false;
            }

            ElementDictionary arrayValue = null;

            if (IsArray(variable, ref arrayValue))
            {
                isArray = true;

                if ((arrayValue == null) ||
                    (arrayValue.Count == 0))
                {
                    return true;
                }
            }
            else
            {
                isArray = false;

                if (variable.Value == null)
                    return true;
            }

            return false;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method determines whether the specified variable has its virtual flag set.
        /// </summary>
        /// <param name="variable">
        /// The variable to check. This value may be null.
        /// </param>
        /// <returns>
        /// True if the variable is non-null and has the virtual flag set; otherwise, false.
        /// </returns>
        public static bool IsVirtual(
            IVariable variable
            )
        {
            if (variable == null)
                return false;

            return variable.HasFlags(VariableFlags.Virtual, true);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method determines whether the specified variable has its break-on-get flag set.
        /// </summary>
        /// <param name="variable">
        /// The variable to check. This value may be null.
        /// </param>
        /// <returns>
        /// True if the variable is non-null and has the break-on-get flag set; otherwise, false.
        /// </returns>
        public static bool IsBreakOnGet(
            IVariable variable
            )
        {
            if (variable == null)
                return false;

            return variable.HasFlags(VariableFlags.BreakOnGet, true);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method determines whether the specified variable has its break-on-set flag set.
        /// </summary>
        /// <param name="variable">
        /// The variable to check. This value may be null.
        /// </param>
        /// <returns>
        /// True if the variable is non-null and has the break-on-set flag set; otherwise, false.
        /// </returns>
        public static bool IsBreakOnSet(
            IVariable variable
            )
        {
            if (variable == null)
                return false;

            return variable.HasFlags(VariableFlags.BreakOnSet, true);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method determines whether the specified variable has its break-on-unset flag set.
        /// </summary>
        /// <param name="variable">
        /// The variable to check. This value may be null.
        /// </param>
        /// <returns>
        /// True if the variable is non-null and has the break-on-unset flag set; otherwise, false.
        /// </returns>
        public static bool IsBreakOnUnset(
            IVariable variable
            )
        {
            if (variable == null)
                return false;

            return variable.HasFlags(VariableFlags.BreakOnUnset, true);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method determines whether the specified variable has its dirty flag set.
        /// </summary>
        /// <param name="variable">
        /// The variable to check. This value may be null.
        /// </param>
        /// <returns>
        /// True if the variable is non-null and has the dirty flag set; otherwise, false.
        /// </returns>
        public static bool IsDirty(
            IVariable variable
            )
        {
            if (variable == null)
                return false;

            return variable.HasFlags(VariableFlags.Dirty, true);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method determines whether the specified variable has its evaluate flag set.
        /// </summary>
        /// <param name="variable">
        /// The variable to check. This value may be null.
        /// </param>
        /// <returns>
        /// True if the variable is non-null and has the evaluate flag set; otherwise, false.
        /// </returns>
        public static bool IsEvaluate(
            IVariable variable
            )
        {
            if (variable == null)
                return false;

            return variable.HasFlags(VariableFlags.Evaluate, true);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method determines whether the specified variable has its link flag set.
        /// </summary>
        /// <param name="variable">
        /// The variable to check. This value may be null.
        /// </param>
        /// <returns>
        /// True if the variable is non-null and has the link flag set; otherwise, false.
        /// </returns>
        public static bool IsLink(
            IVariable variable
            )
        {
            if (variable == null)
                return false;

            return variable.HasFlags(VariableFlags.Link, true);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method determines whether the specified variable has its no-trace flag set.
        /// </summary>
        /// <param name="variable">
        /// The variable to check. This value may be null.
        /// </param>
        /// <returns>
        /// True if the variable is non-null and has the no-trace flag set; otherwise, false.
        /// </returns>
        public static bool IsNoTrace(
            IVariable variable
            )
        {
            if (variable == null)
                return false;

            return variable.HasFlags(VariableFlags.NoTrace, true);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method determines whether the specified variable has its no-watchpoint flag set.
        /// </summary>
        /// <param name="variable">
        /// The variable to check. This value may be null.
        /// </param>
        /// <returns>
        /// True if the variable is non-null and has the no-watchpoint flag set; otherwise, false.
        /// </returns>
        public static bool IsNoWatchpoint(
            IVariable variable
            )
        {
            if (variable == null)
                return false;

            return variable.HasFlags(VariableFlags.NoWatchpoint, true);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method determines whether the specified variable has its no-post-process flag set.
        /// </summary>
        /// <param name="variable">
        /// The variable to check. This value may be null.
        /// </param>
        /// <returns>
        /// True if the variable is non-null and has the no-post-process flag set; otherwise, false.
        /// </returns>
        public static bool IsNoPostProcess(
            IVariable variable
            )
        {
            if (variable == null)
                return false;

            return variable.HasFlags(VariableFlags.NoPostProcess, true);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method determines whether the specified variable has its no-notify flag set.
        /// </summary>
        /// <param name="variable">
        /// The variable to check. This value may be null.
        /// </param>
        /// <returns>
        /// True if the variable is non-null and has the no-notify flag set; otherwise, false.
        /// </returns>
        public static bool IsNoNotify(
            IVariable variable
            )
        {
            if (variable == null)
                return false;

            return variable.HasFlags(VariableFlags.NoNotify, true);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method determines whether the specified variable has its read-only flag set.
        /// </summary>
        /// <param name="variable">
        /// The variable to check. This value may be null.
        /// </param>
        /// <returns>
        /// True if the variable is non-null and has the read-only flag set; otherwise, false.
        /// </returns>
        public static bool IsReadOnly(
            IVariable variable
            )
        {
            if (variable == null)
                return false;

            return variable.HasFlags(VariableFlags.ReadOnly, true);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method determines whether the specified variable has its read-only or invariant flag set.
        /// </summary>
        /// <param name="variable">
        /// The variable to check. This value may be null.
        /// </param>
        /// <returns>
        /// True if the variable is non-null and has the read-only or invariant flag set; otherwise, false.
        /// </returns>
        public static bool IsReadOnlyOrInvariant(
            IVariable variable
            )
        {
            if (variable == null)
                return false;

            return variable.HasFlags(
                VariableFlags.ReadOnly | VariableFlags.Invariant, false);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method determines whether the specified variable has its substitute flag set.
        /// </summary>
        /// <param name="variable">
        /// The variable to check. This value may be null.
        /// </param>
        /// <returns>
        /// True if the variable is non-null and has the substitute flag set; otherwise, false.
        /// </returns>
        public static bool IsSubstitute(
            IVariable variable
            )
        {
            if (variable == null)
                return false;

            return variable.HasFlags(VariableFlags.Substitute, true);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method determines whether the specified variable has its substitute or evaluate flag set.
        /// </summary>
        /// <param name="variable">
        /// The variable to check. This value may be null.
        /// </param>
        /// <returns>
        /// True if the variable is non-null and has the substitute or evaluate flag set; otherwise, false.
        /// </returns>
        public static bool IsSubstituteOrEvaluate(
            IVariable variable
            )
        {
            if (variable == null)
                return false;

            return variable.HasFlags(VariableFlags.Substitute |
                VariableFlags.Evaluate, false);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method determines whether the specified variable has its system flag set.
        /// </summary>
        /// <param name="variable">
        /// The variable to check. This value may be null.
        /// </param>
        /// <returns>
        /// True if the variable is non-null and has the system flag set; otherwise, false.
        /// </returns>
        public static bool IsSystem(
            IVariable variable
            )
        {
            if (variable == null)
                return false;

            return variable.HasFlags(VariableFlags.System, true);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method determines whether the specified variable has its mutable flag set.
        /// </summary>
        /// <param name="variable">
        /// The variable to check. This value may be null.
        /// </param>
        /// <returns>
        /// True if the variable is non-null and has the mutable flag set; otherwise, false.
        /// </returns>
        public static bool IsMutable(
            IVariable variable
            )
        {
            if (variable == null)
                return false;

            return variable.HasFlags(VariableFlags.Mutable, true);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method determines whether the specified variable has its invariant flag set.
        /// </summary>
        /// <param name="variable">
        /// The variable to check. This value may be null.
        /// </param>
        /// <returns>
        /// True if the variable is non-null and has the invariant flag set; otherwise, false.
        /// </returns>
        public static bool IsInvariant(
            IVariable variable
            )
        {
            if (variable == null)
                return false;

            return variable.HasFlags(VariableFlags.Invariant, true);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method determines whether the specified variable has its wait flag set.
        /// </summary>
        /// <param name="variable">
        /// The variable to check. This value may be null.
        /// </param>
        /// <returns>
        /// True if the variable is non-null and has the wait flag set; otherwise, false.
        /// </returns>
        public static bool IsWait(
            IVariable variable
            )
        {
            if (variable == null)
                return false;

            return variable.HasFlags(VariableFlags.Wait, true);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method determines whether the specified variable or one of its array elements has its wait flag set.
        /// </summary>
        /// <param name="variable">
        /// The variable to check. This value may be null.
        /// </param>
        /// <param name="index">
        /// The array element index to check, or null to check the variable itself.
        /// </param>
        /// <returns>
        /// True if the wait flag is set on the specified variable or array element; otherwise, false.
        /// </returns>
        public static bool IsWait(
            IVariable variable,
            string index
            )
        {
            return CheckElementFlags(
                variable, index, VariableFlags.Wait, true);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method determines whether the specified variable is undefined, also treating it as undefined when its call frame has been disposed or is undefined.
        /// </summary>
        /// <param name="variable">
        /// The variable to check. This value may be null.
        /// </param>
        /// <returns>
        /// True if the variable is undefined; otherwise, false.
        /// </returns>
        public static bool IsUndefined(
            IVariable variable
            )
        {
            //
            // HACK: Also check if the call frame is undefined.  Technically,
            //       this is now always required and so we do this here rather
            //       than propagate this check all throughout the code.
            //
            if (variable == null)
                return false;

            if (CallFrameOps.IsDisposedOrUndefined(variable.Frame))
                return true;

            return variable.HasFlags(VariableFlags.Undefined, true);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method determines whether the specified variable is undefined, building an appropriate error message when it is.
        /// </summary>
        /// <param name="variable">
        /// The variable to check. This value may be null.
        /// </param>
        /// <param name="operation">
        /// The name of the operation being attempted, used when building the error message.
        /// </param>
        /// <param name="name">
        /// The variable name used when building the error message.
        /// </param>
        /// <param name="index">
        /// The array element index used when building the error message, if any.
        /// </param>
        /// <param name="error">
        /// Upon the variable being undefined, this will contain an appropriate error message.
        /// </param>
        /// <returns>
        /// True if the variable is undefined; otherwise, false.
        /// </returns>
        public static bool IsUndefined(
            IVariable variable,
            string operation,
            string name,
            string index,
            ref Result error
            )
        {
            bool result = IsUndefined(variable);

            if (result && !String.IsNullOrEmpty(operation) && (name != null))
            {
                error = String.Format("can't {0} {1}: no such variable",
                    operation, FormatOps.ErrorVariableName(name, index));
            }

            return result;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method determines whether the specified variable is usable.
        /// </summary>
        /// <param name="variable">
        /// The variable to check. This value may be null.
        /// </param>
        /// <param name="error">
        /// Upon failure, this will contain an appropriate error message.
        /// </param>
        /// <returns>
        /// True if the variable is non-null and usable; otherwise, false.
        /// </returns>
        public static bool IsUsable(
            IVariable variable,
            ref Result error
            )
        {
            if (variable == null)
            {
                error = "invalid variable";
                return false;
            }

            return variable.IsUsable(ref error);
        }

        ///////////////////////////////////////////////////////////////////////

        #region Variable Dirty Flag Methods
        /// <summary>
        /// This method determines whether a transition from the old variable flags to the new variable flags represents the variable becoming clean.
        /// </summary>
        /// <param name="oldFlags">
        /// The previous variable flags.
        /// </param>
        /// <param name="newFlags">
        /// The new variable flags.
        /// </param>
        /// <returns>
        /// True if the variable was dirty and is now clean; otherwise, false.
        /// </returns>
        public static bool IsNowClean(
            VariableFlags oldFlags,
            VariableFlags newFlags
            )
        {
            return FlagOps.HasFlags(oldFlags, VariableFlags.Dirty, true) &&
                !FlagOps.HasFlags(newFlags, VariableFlags.Dirty, true);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method determines whether a transition from the old variable flags to the new variable flags represents the variable becoming dirty.
        /// </summary>
        /// <param name="oldFlags">
        /// The previous variable flags.
        /// </param>
        /// <param name="newFlags">
        /// The new variable flags.
        /// </param>
        /// <returns>
        /// True if the variable was clean and is now dirty; otherwise, false.
        /// </returns>
        public static bool IsNowDirty(
            VariableFlags oldFlags,
            VariableFlags newFlags
            )
        {
            return !FlagOps.HasFlags(oldFlags, VariableFlags.Dirty, true) &&
                FlagOps.HasFlags(newFlags, VariableFlags.Dirty, true);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method updates the specified wait event in response to a change in variable flags, signaling or resetting it as appropriate.
        /// </summary>
        /// <param name="variableEvent">
        /// The event used to signal waiters on the variable.
        /// </param>
        /// <param name="oldFlags">
        /// The previous variable flags.
        /// </param>
        /// <param name="newFlags">
        /// The new variable flags.
        /// </param>
        /// <returns>
        /// True if the event was signaled or reset; otherwise, false.
        /// </returns>
        public static bool OnFlagsChanged(
            EventWaitHandle variableEvent,
            VariableFlags oldFlags,
            VariableFlags newFlags
            )
        {
            //
            // NOTE: If the wait flag is not set, we do not
            //       care about the flags changing.
            //
            if (FlagOps.HasFlags(newFlags, VariableFlags.Wait, true))
            {
                //
                // NOTE: If the variable is now clean [and
                //       it was dirty before], reset the
                //       event.
                //
                if (IsNowClean(oldFlags, newFlags))
                    return ThreadOps.ResetEvent(variableEvent);
                //
                // NOTE: Otherwise, if the variable is now
                //       dirty [and it was clean before],
                //       clear the wait flag and set the
                //       event.
                //
                else if (IsNowDirty(oldFlags, newFlags))
                    return ThreadOps.SetEvent(variableEvent);
            }

            return false;
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Array Element Checking Methods
        /// <summary>
        /// This method determines whether the specified variable, or one of its array elements, has the specified flags set.
        /// </summary>
        /// <param name="variable">
        /// The variable to check. This value may be null.
        /// </param>
        /// <param name="index">
        /// The array element index to check, or null to check the variable itself.
        /// </param>
        /// <param name="hasFlags">
        /// The variable flags to look for.
        /// </param>
        /// <param name="all">
        /// Non-zero to require that all of the specified flags are set; zero to require only one of them.
        /// </param>
        /// <returns>
        /// True if the specified flags are set on the variable or array element; otherwise, false.
        /// </returns>
        private static bool CheckElementFlags(
            IVariable variable,
            string index,
            VariableFlags hasFlags,
            bool all
            )
        {
            if (variable == null)
                return false;

            if (index != null)
            {
                ElementDictionary arrayValue = variable.ArrayValue;

                if (arrayValue == null)
                    return false;

                return arrayValue.HasFlags(
                    index, VariableFlags.None, hasFlags, all);
            }
            else
            {
                return variable.HasFlags(hasFlags, all);
            }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method determines whether the specified array element is present within the specified variable.
        /// </summary>
        /// <param name="variable">
        /// The variable to check. This value may be null.
        /// </param>
        /// <param name="index">
        /// The array element index to check.
        /// </param>
        /// <returns>
        /// True if the variable is a defined array containing the specified element; otherwise, false.
        /// </returns>
        public static bool IsPresent(
            IVariable variable,
            string index
            )
        {
            if ((variable == null) || (index == null))
                return false;

            if (IsUndefined(variable))
                return false;

            ElementDictionary arrayValue = variable.ArrayValue;

            if (arrayValue == null)
                return false;

            return arrayValue.ContainsKey(index);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method determines whether the specified variable, or one of its array elements, is dirty (i.e. has changed).
        /// </summary>
        /// <param name="variable">
        /// The variable to check. This value may be null.
        /// </param>
        /// <param name="index">
        /// The array element index to check, or null to check all elements of the array.
        /// </param>
        /// <param name="wasUndefined">
        /// Non-zero if the variable was undefined prior to any wait taking place.
        /// </param>
        /// <returns>
        /// True if the specified variable or array element is dirty; otherwise, false.
        /// </returns>
        public static bool IsDirty(
            IVariable variable,
            string index,
            bool wasUndefined
            )
        {
            if (variable == null)
                return false;

            ElementDictionary arrayValue = variable.ArrayValue;

            if (arrayValue == null)
                return false;

            if (index != null)
            {
                if (arrayValue.HasFlags(
                        index, VariableFlags.None, VariableFlags.Dirty,
                        true))
                {
                    return true;
                }

                //
                // BUGFIX: If the variable itself is now undefined, it was
                //         almost certainly [unset] during the [vwait] for
                //         the element; therefore, consider the element as
                //         "changed" now in that case.
                //
                // BUGFIX: *UPDATE* Unless the variable was undefined prior
                //         to any [vwait] taking place (this time).
                //
                return !wasUndefined && IsUndefined(variable);
            }
            else
            {
                foreach (KeyValuePair<string, object> pair in arrayValue)
                {
                    if (arrayValue.HasFlags(
                            pair.Key, VariableFlags.None,
                            VariableFlags.Dirty, true))
                    {
                        return true;
                    }
                }

                return IsDirty(variable);
            }
        }
        #endregion
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Variable Signaling Methods
        /// <summary>
        /// This method marks the specified variable as clean and waiting.
        /// </summary>
        /// <param name="variable">
        /// The variable to modify. This value may be null.
        /// </param>
        /// <returns>
        /// True if both the wait and dirty flags were updated; otherwise, false.
        /// </returns>
        public static bool SignalClean(
            IVariable variable /* in */
            )
        {
            bool result = true;

            if (!SetWait(variable, true))
                result = false;

            if (!SetDirty(variable, false))
                result = false;

            return result;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method marks the specified array element as clean and waiting.
        /// </summary>
        /// <param name="variable">
        /// The variable to modify. This value may be null.
        /// </param>
        /// <param name="index">
        /// The array element index to mark, or null for the variable itself.
        /// </param>
        /// <returns>
        /// True if both the wait and dirty element flags were updated; otherwise, false.
        /// </returns>
        public static bool SignalClean(
            IVariable variable, /* in */
            string index        /* in, optional */
            )
        {
            bool result = true;

            if (!SetElementWait(variable, index, true))
                result = false;

            if (!SetElementDirty(variable, index, false))
                result = false;

            return result;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method marks the specified variable, and optionally one of its array elements, as dirty.
        /// </summary>
        /// <param name="variable">
        /// The variable to modify. This value may be null.
        /// </param>
        /// <param name="index">
        /// The array element index to mark, or null to mark only the variable itself.
        /// </param>
        /// <returns>
        /// True if the variable was non-null and its flags were updated; otherwise, false.
        /// </returns>
        public static bool SignalDirty(
            IVariable variable, /* in */
            string index        /* in, optional */
            )
        {
            if (variable == null)
                return false;

            bool result = true;
            ElementDictionary arrayValue = variable.ArrayValue;

            if (arrayValue != null)
            {
                //
                // TODO: To support waiting (and being notified) on array
                //       elements that have never been waited on nor flagged
                //       as dirty before, the value of the "initialFlags"
                //       parameter to the "ChangeElementFlags" method would
                //       need to be "VariableFlags.Wait"; however, this will
                //       have a negative impact on array element performance
                //       and is not necessary to obtain compliance with the
                //       semantics of the native Tcl [vwait] command.
                //
                if (!ChangeElementFlags(
                        variable, index, VariableFlags.None,
                        VariableFlags.Dirty, (index != null),
                        true))
                {
                    result = false;
                }
            }

            variable.SetFlags(VariableFlags.Dirty, true);
            return result;
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Variable Mutator Methods
        /// <summary>
        /// This method sets or clears the array flag on the specified variable.
        /// </summary>
        /// <param name="variable">
        /// The variable to modify. This value may be null.
        /// </param>
        /// <param name="array">
        /// Non-zero to set the array flag; zero to clear it.
        /// </param>
        /// <returns>
        /// True if the variable was non-null and its flags were modified; otherwise, false.
        /// </returns>
        public static bool SetArray(
            IVariable variable,
            bool array
            )
        {
            if (variable != null)
            {
                variable.SetFlags(VariableFlags.Array, array);
                return true;
            }
            else
            {
                return false;
            }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method sets or clears the dirty flag on the specified variable.
        /// </summary>
        /// <param name="variable">
        /// The variable to modify. This value may be null.
        /// </param>
        /// <param name="dirty">
        /// Non-zero to set the dirty flag; zero to clear it.
        /// </param>
        /// <returns>
        /// True if the variable was non-null and its flags were modified; otherwise, false.
        /// </returns>
        public static bool SetDirty(
            IVariable variable,
            bool dirty
            )
        {
            if (variable != null)
            {
                variable.SetFlags(VariableFlags.Dirty, dirty);
                return true;
            }
            else
            {
                return false;
            }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method sets or clears the link flag on the specified variable.
        /// </summary>
        /// <param name="variable">
        /// The variable to modify. This value may be null.
        /// </param>
        /// <param name="link">
        /// Non-zero to set the link flag; zero to clear it.
        /// </param>
        /// <returns>
        /// True if the variable was non-null and its flags were modified; otherwise, false.
        /// </returns>
        public static bool SetLink(
            IVariable variable,
            bool link
            )
        {
            if (variable != null)
            {
                variable.SetFlags(VariableFlags.Link, link);
                return true;
            }
            else
            {
                return false;
            }
        }

        ///////////////////////////////////////////////////////////////////////

        #region Dead Code
#if DEAD_CODE
        /// <summary>
        /// This method sets or clears the no-trace flag on the specified
        /// variable.
        /// </summary>
        /// <param name="variable">
        /// The variable whose no-trace flag is to be modified.
        /// </param>
        /// <param name="noTrace">
        /// When non-zero, the no-trace flag is set; otherwise, it is cleared.
        /// </param>
        /// <returns>
        /// True if the flag was modified; otherwise, false.
        /// </returns>
        public static bool SetNoTrace(
            IVariable variable,
            bool noTrace
            )
        {
            if (variable != null)
            {
                variable.SetFlags(VariableFlags.NoTrace, noTrace);
                return true;
            }
            else
            {
                return false;
            }
        }
#endif
        #endregion

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method sets or clears the read-only flag on the specified variable.
        /// </summary>
        /// <param name="variable">
        /// The variable to modify. This value may be null.
        /// </param>
        /// <param name="readOnly">
        /// Non-zero to set the read-only flag; zero to clear it.
        /// </param>
        /// <returns>
        /// True if the variable was non-null and its flags were modified; otherwise, false.
        /// </returns>
        public static bool SetReadOnly(
            IVariable variable,
            bool readOnly
            )
        {
            if (variable != null)
            {
                variable.SetFlags(VariableFlags.ReadOnly, readOnly);
                return true;
            }
            else
            {
                return false;
            }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method marks the specified variable as undefined or defined.
        /// </summary>
        /// <param name="variable">
        /// The variable to modify. This value may be null.
        /// </param>
        /// <param name="undefined">
        /// Non-zero to mark the variable as undefined; zero to mark it as defined.
        /// </param>
        /// <returns>
        /// True if the variable was non-null and was modified; otherwise, false.
        /// </returns>
        public static bool SetUndefined(
            IVariable variable,
            bool undefined
            )
        {
            if (variable != null)
            {
                variable.MakeUndefined(undefined);
                return true;
            }
            else
            {
                return false;
            }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method sets or clears the wait flag on the specified variable.
        /// </summary>
        /// <param name="variable">
        /// The variable to modify. This value may be null.
        /// </param>
        /// <param name="wait">
        /// Non-zero to set the wait flag; zero to clear it.
        /// </param>
        /// <returns>
        /// True if the variable was non-null and its flags were modified; otherwise, false.
        /// </returns>
        public static bool SetWait(
            IVariable variable,
            bool wait
            )
        {
            if (variable != null)
            {
                variable.SetFlags(VariableFlags.Wait, wait);
                return true;
            }
            else
            {
                return false;
            }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method marks the specified variable as global or non-global.
        /// </summary>
        /// <param name="variable">
        /// The variable to modify. This value may be null.
        /// </param>
        /// <param name="global">
        /// Non-zero to mark the variable as global; zero otherwise.
        /// </param>
        /// <returns>
        /// True if the variable was non-null and was modified; otherwise, false.
        /// </returns>
        public static bool SetGlobal( /* TODO: Rename?  MakeGlobal? */
            IVariable variable,
            bool global
            )
        {
            if (variable != null)
            {
                variable.MakeGlobal(global);
                return true;
            }
            else
            {
                return false;
            }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method marks the specified variable as local or non-local.
        /// </summary>
        /// <param name="variable">
        /// The variable to modify. This value may be null.
        /// </param>
        /// <param name="local">
        /// Non-zero to mark the variable as local; zero otherwise.
        /// </param>
        /// <returns>
        /// True if the variable was non-null and was modified; otherwise, false.
        /// </returns>
        public static bool SetLocal( /* TODO: Rename?  MakeLocal? */
            IVariable variable,
            bool local
            )
        {
            if (variable != null)
            {
                variable.MakeLocal(local);
                return true;
            }
            else
            {
                return false;
            }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method resets the call frame and qualified name associated with the specified variable.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter context. This value may be null.
        /// </param>
        /// <param name="variable">
        /// The variable to modify. This value may be null.
        /// </param>
        /// <param name="frame">
        /// The new call frame to associate with the variable.
        /// </param>
        /// <returns>
        /// True if the interpreter and variable were both non-null and the variable was modified; otherwise, false.
        /// </returns>
        public static bool ResetCallFrame(
            Interpreter interpreter,
            IVariable variable,
            ICallFrame frame
            )
        {
            if ((interpreter != null) && (variable != null))
            {
                variable.Frame = frame;
                variable.QualifiedName = null;

                /* IGNORED */
                interpreter.MaybeSetQualifiedName(variable);

                return true;
            }

            return false;
        }

        ///////////////////////////////////////////////////////////////////////

        #region Array Element Mutator Methods
        /// <summary>
        /// This method adds or removes the specified flags on one array element, or on all elements of the array.
        /// </summary>
        /// <param name="variable">
        /// The variable to modify. This value may be null.
        /// </param>
        /// <param name="index">
        /// The array element index to change, or null to change all elements.
        /// </param>
        /// <param name="initialFlags">
        /// The initial flags to use when an element is being created.
        /// </param>
        /// <param name="changeFlags">
        /// The flags to add or remove.
        /// </param>
        /// <param name="create">
        /// Non-zero to create the element if it does not exist.
        /// </param>
        /// <param name="add">
        /// Non-zero to add the flags; zero to remove them.
        /// </param>
        /// <returns>
        /// True if the flags were changed for the specified element(s); otherwise, false.
        /// </returns>
        private static bool ChangeElementFlags(
            IVariable variable,
            string index,
            VariableFlags initialFlags,
            VariableFlags changeFlags,
            bool create,
            bool add
            )
        {
            if (variable == null)
                return false;

            ElementDictionary arrayValue = variable.ArrayValue;

            if (arrayValue == null)
                return false;

            bool notify = true;

            if (index != null)
            {
                return arrayValue.ChangeFlags(
                    index, initialFlags, changeFlags, create, add,
                    ref notify);
            }
            else
            {
                bool result = true;

                foreach (KeyValuePair<string, object> pair in arrayValue)
                {
                    if (!arrayValue.ChangeFlags(
                            pair.Key, initialFlags, changeFlags,
                            create, add, ref notify))
                    {
                        result = false;
                    }
                }

                return result;
            }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method sets or clears the dirty flag on the specified array element.
        /// </summary>
        /// <param name="variable">
        /// The variable to modify. This value may be null.
        /// </param>
        /// <param name="index">
        /// The array element index to modify, or null for all elements.
        /// </param>
        /// <param name="dirty">
        /// Non-zero to set the dirty flag; zero to clear it.
        /// </param>
        /// <returns>
        /// True if the flag was changed; otherwise, false.
        /// </returns>
        public static bool SetElementDirty(
            IVariable variable,
            string index,
            bool dirty
            )
        {
            return ChangeElementFlags(
                variable, index, VariableFlags.None,
                VariableFlags.Dirty, false, dirty);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method sets or clears the wait flag on the specified array element.
        /// </summary>
        /// <param name="variable">
        /// The variable to modify. This value may be null.
        /// </param>
        /// <param name="index">
        /// The array element index to modify, or null for all elements.
        /// </param>
        /// <param name="wait">
        /// Non-zero to set the wait flag; zero to clear it.
        /// </param>
        /// <returns>
        /// True if the flag was changed; otherwise, false.
        /// </returns>
        public static bool SetElementWait(
            IVariable variable,
            string index,
            bool wait
            )
        {
            return ChangeElementFlags(
                variable, index, VariableFlags.None,
                VariableFlags.Wait, true, wait);
        }
        #endregion
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Wrapper Support Methods
        /// <summary>
        /// This method returns the existing token for the specified wrapper if available, catching and ignoring any exception that may be thrown; otherwise, it allocates and returns the next available token identifier.
        /// </summary>
        /// <param name="wrapper">
        /// The wrapper whose token will be used, if available. This value may be null.
        /// </param>
        /// <returns>
        /// The existing token for the wrapper, or a newly allocated token identifier.
        /// </returns>
        public static long NextTokenIdNoThrow(
            IWrapperData wrapper
            )
        {
            //
            // HACK: Use the existing token for the entity if available.
            //       This should not cause any issues because the tokens
            //       are shared by all interpreters within the AppDomain
            //       (i.e. there should not be duplicate values).  Also,
            //       if USE_APPDOMAIN_FOR_ID is defined, there will not
            //       be duplicate values within the entire process.
            //
            if (wrapper != null)
            {
                try
                {
                    long token = wrapper.Token; /* throw */

                    if (token != 0)
                        return token;
                }
                catch
                {
                    // do nothing.
                }
            }

            return GlobalState.NextTokenId();
        }

        ///////////////////////////////////////////////////////////////////////

        //
        // WARNING: For use by the Interpreter.AddResolver method only.
        //
        /// <summary>
        /// This method returns the existing token for the specified wrapper if available, catching and ignoring any exception that may be thrown; otherwise, it returns the specified default token identifier.
        /// </summary>
        /// <param name="wrapper">
        /// The wrapper whose token will be used, if available. This value may be null.
        /// </param>
        /// <param name="default">
        /// The token identifier to return when the wrapper has no existing token.
        /// </param>
        /// <returns>
        /// The existing token for the wrapper, or the value of <paramref name="default" />.
        /// </returns>
        public static long NextTokenIdNoThrow(
            IWrapperData wrapper,
            long @default
            )
        {
            //
            // HACK: Use the existing token for the entity if available.
            //       This should not cause any issues because the tokens
            //       are shared by all interpreters within the AppDomain
            //       (i.e. there should not be duplicate values).  Also,
            //       if USE_APPDOMAIN_FOR_ID is defined, there will not
            //       be duplicate values within the entire process.
            //
            if (wrapper != null)
            {
                try
                {
                    long token = wrapper.Token; /* throw */

                    if (token != 0)
                        return token;
                }
                catch
                {
                    // do nothing.
                }
            }

            return @default;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method gets the token for the specified wrapper.
        /// </summary>
        /// <param name="wrapper">
        /// The wrapper whose token will be returned. This value may be null.
        /// </param>
        /// <returns>
        /// The token for the wrapper, or zero if the wrapper is null.
        /// </returns>
        public static long GetToken(
            IWrapperData wrapper
            )
        {
            if (wrapper != null)
                return wrapper.Token;

            return 0;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method gets the token for the specified wrapper, catching and ignoring any exception that may be thrown.
        /// </summary>
        /// <param name="wrapper">
        /// The wrapper whose token will be returned. This value may be null.
        /// </param>
        /// <returns>
        /// The token for the wrapper, or zero if it cannot be queried.
        /// </returns>
        public static long GetTokenNoThrow(
            IWrapperData wrapper
            )
        {
            if (wrapper != null)
            {
                try
                {
                    return wrapper.Token; /* throw */
                }
                catch
                {
                    // do nothing.
                }
            }

            return 0;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method sets the token for the specified wrapper.
        /// </summary>
        /// <param name="wrapper">
        /// The wrapper to modify. This value may be null.
        /// </param>
        /// <param name="token">
        /// The token value to set.
        /// </param>
        public static void SetToken(
            IWrapperData wrapper,
            long token
            )
        {
            if (wrapper != null)
                wrapper.Token = token;
        }

        ///////////////////////////////////////////////////////////////////////

        //
        // HACK: It should be noted that the wrapperData parameter to
        //       this method should not actually be a "wrapper" class;
        //       instead, it should be of the class type to be wrapped
        //       (e.g. _Wrappers.Command, etc).  All of those wrapper
        //       classes (also) implement the IWrapperData interface.
        //
        /// <summary>
        /// This method attempts to create a new wrapper of the specified type, associating it with the specified token and wrapped object, catching and ignoring any exception that may be thrown.
        /// </summary>
        /// <typeparam name="T">
        /// The type of wrapper to create.
        /// </typeparam>
        /// <param name="token">
        /// The token to assign to the new wrapper.
        /// </param>
        /// <param name="wrapperData">
        /// The object to be wrapped by the new wrapper.
        /// </param>
        /// <returns>
        /// The newly created wrapper, or null if it cannot be created.
        /// </returns>
        public static IWrapper MaybeNewWrapperWith<T>(
            long token,              /* in */
            IWrapperData wrapperData /* in */
            ) where T : IWrapper, new()
        {
            try
            {
                IWrapper wrapper = new T(); /* throw (?) */

                wrapper.Token = token; /* NOTE: Via IWrapperData. */
                wrapper.Object = wrapperData; /* NOTE: Via IWrapper. */

                return wrapper;
            }
            catch (Exception e)
            {
                TraceOps.DebugTrace(
                    e, typeof(EntityOps).Name,
                    TracePriority.EntityError);

                return null;
            }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Identifier Support Methods
        /// <summary>
        /// This method gets the identifier for the specified entity.
        /// </summary>
        /// <param name="identifierBase">
        /// The entity to query. This value may be null.
        /// </param>
        /// <returns>
        /// The identifier for the entity, or <see cref="Guid.Empty" /> if the entity is null.
        /// </returns>
        public static Guid GetId(
            IIdentifierBase identifierBase
            )
        {
            if (identifierBase != null)
                return identifierBase.Id;

            return Guid.Empty;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method assigns the object identifier attribute value to the identifier of the specified entity, if it has not already been set.
        /// </summary>
        /// <param name="identifierBase">
        /// The entity to modify. This value may be null.
        /// </param>
        /// <returns>
        /// True if the identifier was assigned; otherwise, false.
        /// </returns>
        public static bool MaybeSetupId(
            IIdentifierBase identifierBase
            )
        {
            //
            // NOTE: Attempt to assign the the ObjectId of the entity to the
            //       Id property, if necessary.
            //
            if ((identifierBase != null) &&
                identifierBase.Id.Equals(Guid.Empty))
            {
                identifierBase.Id = AttributeOps.GetObjectId(identifierBase);
                return true;
            }

            return false;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method gets the name of the specified entity.
        /// </summary>
        /// <param name="identifierName">
        /// The entity to query. This value may be null.
        /// </param>
        /// <returns>
        /// The name of the entity, or null if the entity is null.
        /// </returns>
        public static string GetName(
            IIdentifierName identifierName
            )
        {
            if (identifierName != null)
                return identifierName.Name;

            return null;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method gets the lexeme for the specified operator, catching and ignoring any exception that may be thrown.
        /// </summary>
        /// <param name="operatorData">
        /// The operator data to query. This value may be null.
        /// </param>
        /// <returns>
        /// The lexeme for the operator, or <see cref="Lexeme.Unknown" /> if it cannot be queried.
        /// </returns>
        public static Lexeme GetLexemeNoThrow(
            IOperatorData operatorData
            )
        {
            if (operatorData != null)
            {
                try
                {
                    return operatorData.Lexeme; /* throw */
                }
                catch
                {
                    // do nothing.
                }
            }

            return Lexeme.Unknown;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method gets the name of the specified entity, catching and ignoring any exception that may be thrown.
        /// </summary>
        /// <param name="identifierName">
        /// The entity to query. This value may be null.
        /// </param>
        /// <returns>
        /// The name of the entity, or null if it cannot be queried.
        /// </returns>
        public static string GetNameNoThrow(
            IIdentifierName identifierName
            )
        {
            if (identifierName != null)
            {
                if (!ObjectOps.IsDisposed(identifierName))
                {
                    try
                    {
                        return identifierName.Name; /* throw */
                    }
                    catch
                    {
                        // do nothing.
                    }
                }
            }

            return null;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method gets a name for the specified text writer, catching and ignoring any exception that may be thrown.
        /// </summary>
        /// <param name="textWriter">
        /// The text writer to query. This value may be null.
        /// </param>
        /// <returns>
        /// A name (string representation) for the text writer, or null if it cannot be queried.
        /// </returns>
        public static string GetNameNoThrow(
            TextWriter textWriter
            )
        {
            if (textWriter != null)
            {
                try
                {
                    return textWriter.ToString(); /* throw */
                }
                catch
                {
                    // do nothing.
                }
            }

            return null;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method gets a name for the specified object, catching and ignoring any exception that may be thrown.
        /// </summary>
        /// <param name="object">
        /// The object to query. This value may be null.
        /// </param>
        /// <returns>
        /// A name for the object, or null if it cannot be queried.
        /// </returns>
        public static string GetNameNoThrow(
            object @object
            )
        {
            return GetNameNoThrow(@object, true);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method gets a name for the specified object, optionally falling back to its string representation, catching and ignoring any exception that may be thrown.
        /// </summary>
        /// <param name="object">
        /// The object to query. This value may be null.
        /// </param>
        /// <param name="toString">
        /// Non-zero to fall back to the string representation of the object when it does not have a name.
        /// </param>
        /// <returns>
        /// A name for the object, or null if it cannot be queried.
        /// </returns>
        private static string GetNameNoThrow(
            object @object,
            bool toString
            )
        {
            if (@object != null)
            {
                IIdentifierName identifierName = @object as IIdentifierName;

                if (identifierName != null)
                    return GetNameNoThrow(identifierName);

                if (toString)
                {
                    try
                    {
                        return @object.ToString(); /* throw */
                    }
                    catch
                    {
                        // do nothing.
                    }
                }
            }

            return null;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method gets the friendly name of the specified application domain, catching and ignoring any exception that may be thrown.
        /// </summary>
        /// <param name="appDomain">
        /// The application domain to query. This value may be null.
        /// </param>
        /// <returns>
        /// The friendly name of the application domain, or null if it cannot be queried.
        /// </returns>
        public static string GetNameNoThrow(
            AppDomain appDomain
            )
        {
            if (appDomain != null)
            {
                try
                {
                    return appDomain.FriendlyName; /* throw */
                }
                catch
                {
                    // do nothing.
                }
            }

            return null;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method gets the web name of the specified encoding, catching and ignoring any exception that may be thrown.
        /// </summary>
        /// <param name="encoding">
        /// The encoding to query. This value may be null.
        /// </param>
        /// <returns>
        /// The web name of the encoding, or null if it cannot be queried.
        /// </returns>
        public static string GetNameNoThrow(
            Encoding encoding
            )
        {
            if (encoding != null)
            {
                try
                {
                    return encoding.WebName; /* throw */
                }
                catch
                {
                    // do nothing.
                }
            }

            return null;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method gets a name or identifier for the specified process, catching and ignoring any exception that may be thrown.
        /// </summary>
        /// <param name="process">
        /// The process to query. This value may be null.
        /// </param>
        /// <returns>
        /// The file name or identifier of the process, or null if neither can be queried.
        /// </returns>
        public static object GetNameOrIdNoThrow(
            Process process
            )
        {
            if (process != null)
            {
                try
                {
                    ProcessStartInfo startInfo = process.StartInfo;

                    if (startInfo != null)
                        return startInfo.FileName;
                }
                catch
                {
                    // do nothing.
                }

                try
                {
                    return process.Id;
                }
                catch
                {
                    // do nothing.
                }
            }

            return null;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method gets the names of the items within the specified collection, catching and ignoring any exception that may be thrown.
        /// </summary>
        /// <param name="collection">
        /// The collection to enumerate. This value may be null.
        /// </param>
        /// <returns>
        /// The list of item names, or null if it cannot be produced.
        /// </returns>
        public static IEnumerable<string> GetNamesNoThrow(
            IEnumerable collection
            )
        {
            if (collection != null)
            {
                try
                {
                    StringList result = new StringList();

                    foreach (object item in collection)
                    {
                        if (item == null)
                            continue;

                        string name = GetNameNoThrow(
                            item, false);

                        if (name == null)
                            continue;

                        result.Add(name);
                    }

                    return result;
                }
                catch
                {
                    // do nothing.
                }
            }

            return null;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method gets the names of the values (or keys) within the specified dictionary, catching and ignoring any exception that may be thrown.
        /// </summary>
        /// <param name="dictionary">
        /// The dictionary to enumerate. This value may be null.
        /// </param>
        /// <returns>
        /// The list of names, or null if it cannot be produced.
        /// </returns>
        public static IEnumerable<string> GetNamesNoThrow(
            IDictionary dictionary
            )
        {
            if (dictionary != null)
            {
                try
                {
                    StringList result = new StringList();

                    foreach (DictionaryEntry entry in dictionary)
                    {
                        string name; /* REUSED */
                        object value = entry.Value;

                        if (value == null)
                            continue;

                        name = GetNameNoThrow(value, false);

                        if (name != null)
                        {
                            result.Add(name);
                            continue;
                        }

                        object key = entry.Key;

                        if (key == null)
                            continue;

                        name = GetNameNoThrow(key, true);

                        if (name != null)
                        {
                            result.Add(name);
                            continue;
                        }
                    }

                    return result;
                }
                catch
                {
                    // do nothing.
                }
            }

            return null;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method builds a list of name/value pairs describing the various flags supported by the specified object, based on the interfaces it implements, catching and ignoring any exception that may be thrown.
        /// </summary>
        /// <param name="object">
        /// The object to describe. This value may be null.
        /// </param>
        /// <returns>
        /// A list of flag names and values for the object, or null if none apply.
        /// </returns>
        public static StringList GetFlagsNoThrow(
            object @object /* in */
            )
        {
            StringList result = null;

            try
            {
                IAliasData aliasData = @object as IAliasData;

                if (aliasData != null)
                {
                    if (result == null)
                        result = new StringList();

                    result.Add("AliasFlags");
                    result.Add(aliasData.AliasFlags.ToString());
                }

                ICallbackData callbackData = @object as ICallbackData;

                if (callbackData != null)
                {
                    if (result == null)
                        result = new StringList();

                    result.Add("MarshalFlags");
                    result.Add(callbackData.MarshalFlags.ToString());

                    result.Add("CallbackFlags");
                    // result.Add(callbackData.CallbackFlags.ToString()); /* CS0176 */
                    result.Add(String.Format("{0}", callbackData.CallbackFlags));

                    result.Add("ByRefArgumentFlags");
                    // result.Add(callbackData.ByRefArgumentFlags.ToString()); /* CS0176 */
                    result.Add(String.Format("{0}", callbackData.ByRefArgumentFlags));
                }

                ICallFrame frame = @object as ICallFrame;

                if (frame != null)
                {
                    if (result == null)
                        result = new StringList();

                    result.Add("CallFrameFlags");
                    result.Add(frame.Flags.ToString());
                }

                IChannel channel = @object as IChannel;

                if (channel != null)
                {
                    if (result == null)
                        result = new StringList();

                    result.Add("CanRead");
                    result.Add(channel.CanRead.ToString());
                    result.Add("CanSeek");
                    result.Add(channel.CanSeek.ToString());
                    result.Add("CanWrite");
                    result.Add(channel.CanWrite.ToString());
                    result.Add("HitEndOfStream");
                    result.Add(channel.HitEndOfStream.ToString());
                    result.Add("EndOfStream");

                    try
                    {
                        result.Add(channel.EndOfStream.ToString());
                    }
                    catch (Exception e)
                    {
                        result.Add(e.GetType().ToString());
                    }

                    result.Add("AnyEndOfStream");
                    result.Add(channel.AnyEndOfStream.ToString());
                    result.Add("OneEndOfStream");
                    result.Add(channel.OneEndOfStream.ToString());
                    result.Add("Length");

                    try
                    {
                        result.Add(channel.Length.ToString());
                    }
                    catch (Exception e)
                    {
                        result.Add(e.GetType().ToString());
                    }

                    result.Add("Position");

                    try
                    {
                        result.Add(channel.Position.ToString());
                    }
                    catch (Exception e)
                    {
                        result.Add(e.GetType().ToString());
                    }
                }

                ICommandBaseData commandBaseData = @object as ICommandBaseData;

                if (commandBaseData != null)
                {
                    if (result == null)
                        result = new StringList();

                    result.Add("BaseCommandFlags");
                    result.Add(commandBaseData.CommandFlags.ToString());
                }

                ICommandData commandData = @object as ICommandData;

                if (commandData != null)
                {
                    if (result == null)
                        result = new StringList();

                    result.Add("CommandFlags");
                    result.Add(commandData.CommandFlags.ToString());
                }

#if EMIT && NATIVE && LIBRARY
                IDelegate @delegate = @object as IDelegate;

                if (@delegate != null)
                {
                    if (result == null)
                        result = new StringList();

                    result.Add("FunctionName");
                    result.Add(@delegate.FunctionName);
                }
#endif

                IDelegateData delegateData = @object as IDelegateData;

                if (delegateData != null)
                {
                    if (result == null)
                        result = new StringList();

                    result.Add("DelegateFlags");
                    result.Add(delegateData.ToString());
                }

                IEvent @event = @object as IEvent;

                if (@event != null)
                {
                    if (result == null)
                        result = new StringList();

                    result.Add("EventType");
                    result.Add(@event.Type.ToString());
                    result.Add("EventFlags");
                    result.Add(@event.Flags.ToString());
                    result.Add("EventPriority");
                    result.Add(@event.Priority.ToString());
                }

                IFunctionData functionData = @object as IFunctionData;

                if (functionData != null)
                {
                    if (result == null)
                        result = new StringList();

                    result.Add("FunctionFlags");
                    result.Add(functionData.Flags.ToString());
                }

                IHaveObjectFlags haveObjectFlags = @object as IHaveObjectFlags;

                if (haveObjectFlags != null)
                {
                    if (result == null)
                        result = new StringList();

                    result.Add("ObjectFlags");
                    result.Add(haveObjectFlags.ObjectFlags.ToString());
                }

                IHost host = @object as IHost;

                if (host != null)
                {
                    if (result == null)
                        result = new StringList();

                    result.Add("HostCreateFlags");
                    result.Add(host.HostCreateFlags.ToString());
                    result.Add("HostFlags");
                    result.Add(host.GetHostFlags().ToString());
                    result.Add("TestFlags");
                    result.Add(host.GetTestFlags().ToString());
                    result.Add("HeaderFlags");
                    result.Add(host.GetHeaderFlags().ToString());
                    result.Add("DetailFlags");
                    result.Add(host.GetDetailFlags().ToString());
                }

                IHostData hostData = @object as IHostData;

                if (hostData != null)
                {
                    if (result == null)
                        result = new StringList();

                    result.Add("HostDataCreateFlags");
                    result.Add(hostData.HostCreateFlags.ToString());
                }

                IInteractiveLoopData loopData = @object as IInteractiveLoopData;

                if (loopData != null)
                {
                    if (result == null)
                        result = new StringList();

                    result.Add("ReturnCode");
                    result.Add(loopData.Code.ToString());
                    result.Add("BreakpointType");
                    result.Add(loopData.BreakpointType.ToString());
                    result.Add("EngineFlags");
                    result.Add(loopData.EngineFlags.ToString());
                    result.Add("SubstitutionFlags");
                    result.Add(loopData.SubstitutionFlags.ToString());
                    result.Add("EventFlags");
                    result.Add(loopData.EventFlags.ToString());
                    result.Add("ExpressionFlags");
                    result.Add(loopData.ExpressionFlags.ToString());
                    result.Add("HeaderFlags");
                    result.Add(loopData.HeaderFlags.ToString());
                    result.Add("DetailFlags");
                    result.Add(loopData.DetailFlags.ToString());
                    result.Add("Exit");
                    result.Add(loopData.Exit.ToString());
                }

#if EMIT && NATIVE && LIBRARY
                IModule module = @object as IModule;

                if (module != null)
                {
                    if (result == null)
                        result = new StringList();

                    result.Add("ModuleFlags");
                    result.Add(module.Flags.ToString());
                }
#endif

                IOption option = @object as IOption;

                if (option != null)
                {
                    if (result == null)
                        result = new StringList();

                    result.Add("OptionFlags");
                    result.Add(option.Flags.ToString());
                }

                IOperatorData operatorData = @object as IOperatorData;

                if (operatorData != null)
                {
                    if (result == null)
                        result = new StringList();

                    result.Add("OperatorFlags");
                    result.Add(operatorData.Flags.ToString());
                }

                IPackageData packageData = @object as IPackageData;

                if (packageData != null)
                {
                    if (result == null)
                        result = new StringList();

                    result.Add("PackageFlags");
                    result.Add(packageData.Flags.ToString());
                    result.Add("Loaded");
                    result.Add((packageData.Loaded != null).ToString());
                }

                IPluginData pluginData = @object as IPluginData;

                if (pluginData != null)
                {
                    if (result == null)
                        result = new StringList();

                    result.Add("PluginFlags");
                    result.Add(pluginData.Flags.ToString());
                }

                IProcedureData procedureData = @object as IProcedureData;

                if (procedureData != null)
                {
                    if (result == null)
                        result = new StringList();

                    result.Add("ProcedureFlags");
                    result.Add(procedureData.Flags.ToString());
                }

                IResolveData resolveData = @object as IResolveData;

                if (resolveData != null)
                {
                    if (result == null)
                        result = new StringList();

                    result.Add("ResolveFlags");
                    result.Add(resolveData.Flags.ToString());
                }

#if SHELL
                IShellCallbackData shellCallbackData = @object as IShellCallbackData;

                if (shellCallbackData != null)
                {
                    if (result == null)
                        result = new StringList();

                    result.Add("WhatIf");
                    result.Add(shellCallbackData.WhatIf.ToString());
                    result.Add("StopOnUnknown");
                    result.Add(shellCallbackData.StopOnUnknown.ToString());
                }
#endif

                ISnippetData snippetData = @object as ISnippetData;

                if (snippetData != null)
                {
                    if (result == null)
                        result = new StringList();

                    result.Add("SnippetFlags");
                    result.Add(snippetData.SnippetFlags.ToString());
                }

                ISubCommandData subCommandData = @object as ISubCommandData;

                if (subCommandData != null)
                {
                    if (result == null)
                        result = new StringList();

                    result.Add("SubCommandFlags");
                    result.Add(subCommandData.Flags.ToString());
                }

                ITraceData traceData = @object as ITraceData;

                if (traceData != null)
                {
                    if (result == null)
                        result = new StringList();

                    result.Add("BindingFlags");
                    result.Add(traceData.BindingFlags.ToString());
                    result.Add("MethodFlags");
                    result.Add(traceData.MethodFlags.ToString());
                    result.Add("TraceFlags");
                    result.Add(traceData.TraceFlags.ToString());
                }

#if SHELL
                IUpdateData updateData = @object as IUpdateData;

                if (updateData != null)
                {
                    if (result == null)
                        result = new StringList();

                    result.Add("ActionType");
                    result.Add(updateData.ActionType.ToString());
                    result.Add("ReleaseType");
                    result.Add(updateData.ReleaseType.ToString());
                    result.Add("UpdateType");
                    result.Add(updateData.UpdateType.ToString());
                }
#endif

                IVariable variable = @object as IVariable;

                if (variable != null)
                {
                    if (result == null)
                        result = new StringList();

                    result.Add("VariableFlags");
                    result.Add(variable.Flags.ToString());
                }
            }
            catch
            {
                // do nothing.
            }

            return result;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method sets the group of the specified entity, unless either the entity or the group is null.
        /// </summary>
        /// <param name="identifier">
        /// The entity to modify. This value may be null.
        /// </param>
        /// <param name="group">
        /// The group to set. This value may be null.
        /// </param>
        public static void MaybeSetGroup(
            IIdentifier identifier,
            string group
            )
        {
            if ((identifier == null) || (group == null))
                return;

            identifier.Group = group;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method gets the plugin associated with the specified entity, catching and ignoring any exception that may be thrown.
        /// </summary>
        /// <param name="identifier">
        /// The entity to query. This value may be null.
        /// </param>
        /// <returns>
        /// The plugin associated with the entity, or null if there is none or it cannot be queried.
        /// </returns>
        public static IPlugin GetPluginNoThrow(
            IIdentifier identifier
            )
        {
            if (identifier != null)
            {
                IHavePlugin havePlugin = identifier as IHavePlugin;

                if (havePlugin != null)
                {
                    try
                    {
                        return havePlugin.Plugin;
                    }
                    catch
                    {
                        // do nothing.
                    }
                }
            }

            return null;
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Script Location Support Methods
        /// <summary>
        /// This method determines whether the specified script location was produced via the [source] command.
        /// </summary>
        /// <param name="location">
        /// The script location to check. This value may be null.
        /// </param>
        /// <returns>
        /// True if the location is non-null and was produced via the source command; otherwise, false.
        /// </returns>
        public static bool IsViaSource(
            IScriptLocation location
            )
        {
            return ((location != null) && location.ViaSource);
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Interpreter Support Methods
        /// <summary>
        /// This method determines whether the specified interpreter is usable (i.e. neither deleted nor disposed).
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter to check. This value may be null.
        /// </param>
        /// <returns>
        /// True if the interpreter is non-null and usable; otherwise, false.
        /// </returns>
        public static bool IsUsable(
            Interpreter interpreter
            )
        {
            return ((interpreter != null) &&
                !Interpreter.IsDeletedOrDisposed(interpreter, false));
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method follows the chain of parent interpreters starting from the specified interpreter.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter at which to begin. This value may be null.
        /// </param>
        /// <param name="usable">
        /// Non-zero to stop as soon as a usable interpreter is found.
        /// </param>
        /// <returns>
        /// The final parent interpreter in the chain, or the first usable one found when requested.
        /// </returns>
        public static Interpreter FollowParent(
            Interpreter interpreter,
            bool usable
            )
        {
            while (interpreter != null)
            {
                Interpreter parentInterpreter = null;

                try
                {
                    parentInterpreter = interpreter.ParentInterpreter;
                }
                catch (InterpreterDisposedException)
                {
                    // do nothing.
                }

                if (parentInterpreter == null)
                    break;

                interpreter = parentInterpreter;

                if (usable && IsUsable(interpreter))
                    return interpreter;
            }

            return interpreter;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method follows the chain of test target interpreters starting from the specified interpreter.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter at which to begin. This value may be null.
        /// </param>
        /// <param name="usable">
        /// Non-zero to stop as soon as a usable interpreter is found.
        /// </param>
        /// <returns>
        /// The final test target interpreter in the chain, or the first usable one found when requested.
        /// </returns>
        public static Interpreter FollowTest(
            Interpreter interpreter,
            bool usable
            )
        {
            while (interpreter != null)
            {
                //
                // NOTE: This method requires access to the current test
                //       context; therefore, the interpreter *CANNOT* be
                //       disposed.
                //
                if (interpreter.Disposed)
                    break;

                Interpreter testTargetInterpreter = null;

                try
                {
                    testTargetInterpreter = interpreter.TestTargetInterpreter;
                }
                catch (InterpreterDisposedException)
                {
                    // do nothing.
                }

                if (testTargetInterpreter == null)
                    break;

                interpreter = testTargetInterpreter;

                if (usable && IsUsable(interpreter))
                    return interpreter;
            }

            return interpreter;
        }
        #endregion
    }
}
