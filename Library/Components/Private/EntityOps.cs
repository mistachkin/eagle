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
    [ObjectId("c0e69f4b-fe35-44aa-8798-3080a85e6614")]
    internal static class EntityOps
    {
        #region Callback Checking Methods
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
        public static bool HasBreakpoint(
            ICommand command
            )
        {
            return (command != null) ?
                FlagOps.HasFlags(command.Flags,
                    CommandFlags.Breakpoint, true) : false;
        }

        ///////////////////////////////////////////////////////////////////////

        public static bool IsDisabled(
            ICommand command
            )
        {
            return (command != null) ?
                FlagOps.HasFlags(command.Flags,
                    CommandFlags.Disabled, true) : false;
        }

        ///////////////////////////////////////////////////////////////////////

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

        public static bool IsSafe(
            ICommand command
            )
        {
            return (command != null) ?
                IsSafe(command.Flags) : false;
        }

        ///////////////////////////////////////////////////////////////////////

        public static bool IsHidden(
            ICommand command
            )
        {
            return (command != null) ?
                FlagOps.HasFlags(command.Flags,
                    CommandFlags.Hidden, true) : false;
        }

        ///////////////////////////////////////////////////////////////////////

        public static bool IsNoToken(
            ICommand command
            )
        {
            return (command != null) ?
                FlagOps.HasFlags(command.Flags,
                    CommandFlags.NoToken, true) : false;
        }

        ///////////////////////////////////////////////////////////////////////

        public static bool IsNoRename(
            ICommand command
            )
        {
            return (command != null) ?
                FlagOps.HasFlags(command.Flags,
                    CommandFlags.NoRename, true) : false;
        }

        ///////////////////////////////////////////////////////////////////////

        public static bool IsNoRemove(
            ICommand command
            )
        {
            return (command != null) ?
                FlagOps.HasFlags(command.Flags,
                    CommandFlags.NoRemove, true) : false;
        }

        ///////////////////////////////////////////////////////////////////////

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
        public static bool HasBreakpoint(
            IFunction function
            )
        {
            return (function != null) ?
                FlagOps.HasFlags(function.Flags,
                    FunctionFlags.Breakpoint, true) : false;
        }

        ///////////////////////////////////////////////////////////////////////

        public static bool IsNoToken(
            IFunction function
            )
        {
            return (function != null) ?
                FlagOps.HasFlags(function.Flags,
                    FunctionFlags.NoToken, true) : false;
        }

        ///////////////////////////////////////////////////////////////////////

        public static bool IsReadOnly(
            IFunction function
            )
        {
            return (function != null) ?
                FlagOps.HasFlags(function.Flags,
                    FunctionFlags.ReadOnly, true) : false;
        }

        ///////////////////////////////////////////////////////////////////////

        public static bool IsDisabled(
            IFunction function
            )
        {
            return (function != null) ?
                FlagOps.HasFlags(function.Flags,
                    FunctionFlags.Disabled, true) : false;
        }

        ///////////////////////////////////////////////////////////////////////

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

        public static bool IsDisabled(
            IPolicy policy
            )
        {
            return (policy != null) ?
                FlagOps.HasFlags(policy.PolicyFlags,
                    PolicyFlags.Disabled, true) : false;
        }

        ///////////////////////////////////////////////////////////////////////

        public static bool IsNoToken(
            IPolicy policy
            )
        {
            return (policy != null) ?
                FlagOps.HasFlags(policy.PolicyFlags,
                    PolicyFlags.NoToken, true) : false;
        }

        ///////////////////////////////////////////////////////////////////////

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
        public static bool IsDisabled(
            ITrace trace
            )
        {
            return (trace != null) ?
                FlagOps.HasFlags(trace.TraceFlags,
                    TraceFlags.Disabled, true) : false;
        }

        ///////////////////////////////////////////////////////////////////////

        public static bool IsNoToken(
            ITrace trace
            )
        {
            return (trace != null) ?
                FlagOps.HasFlags(trace.TraceFlags,
                    TraceFlags.NoToken, true) : false;
        }

        ///////////////////////////////////////////////////////////////////////

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

        public static bool HasBreakpoint(
            IProcedure procedure
            )
        {
            return (procedure != null) ?
                FlagOps.HasFlags(procedure.Flags,
                    ProcedureFlags.Breakpoint, true) : false;
        }

        ///////////////////////////////////////////////////////////////////////

        public static bool IsAtomic(
            IProcedure procedure
            )
        {
            return (procedure != null) ?
                FlagOps.HasFlags(procedure.Flags,
                    ProcedureFlags.Atomic, true) : false;
        }

        ///////////////////////////////////////////////////////////////////////

#if ARGUMENT_CACHE || PARSE_CACHE
        public static bool IsNonCaching(
            IProcedure procedure
            )
        {
            return (procedure != null) ?
                FlagOps.HasFlags(procedure.Flags,
                    ProcedureFlags.NonCaching, true) : false;
        }
#endif

        ///////////////////////////////////////////////////////////////////////

        public static bool IsDisabled(
            IProcedure procedure
            )
        {
            return (procedure != null) ?
                FlagOps.HasFlags(procedure.Flags,
                    ProcedureFlags.Disabled, true) : false;
        }

        ///////////////////////////////////////////////////////////////////////

        public static bool IsHidden(
            IProcedure procedure
            )
        {
            return (procedure != null) ?
                FlagOps.HasFlags(procedure.Flags,
                    ProcedureFlags.Hidden, true) : false;
        }

        ///////////////////////////////////////////////////////////////////////

        public static bool IsPositionalArguments(
            IProcedure procedure
            )
        {
            return (procedure != null) ?
                FlagOps.HasFlags(procedure.Flags,
                    ProcedureFlags.PositionalArguments, true) : false;
        }

        ///////////////////////////////////////////////////////////////////////

        public static bool IsNamedArguments(
            IProcedure procedure
            )
        {
            return (procedure != null) ?
                FlagOps.HasFlags(procedure.Flags,
                    ProcedureFlags.NamedArguments, true) : false;
        }

        ///////////////////////////////////////////////////////////////////////

        public static bool IsReadOnly(
            IProcedure procedure
            )
        {
            return (procedure != null) ?
                FlagOps.HasFlags(procedure.Flags,
                    ProcedureFlags.ReadOnly, true) : false;
        }

        ///////////////////////////////////////////////////////////////////////

        public static bool IsNoReplace(
            IProcedure procedure
            )
        {
            return (procedure != null) ?
                FlagOps.HasFlags(procedure.Flags,
                    ProcedureFlags.NoReplace, true) : false;
        }

        ///////////////////////////////////////////////////////////////////////

        public static bool IsNoRename(
            IProcedure procedure
            )
        {
            return (procedure != null) ?
                FlagOps.HasFlags(procedure.Flags,
                    ProcedureFlags.NoRename, true) : false;
        }

        ///////////////////////////////////////////////////////////////////////

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
        public static bool HasBreakpoint(
            IOperator @operator
            )
        {
            return (@operator != null) ?
                FlagOps.HasFlags(@operator.Flags,
                    OperatorFlags.Breakpoint, true) : false;
        }

        ///////////////////////////////////////////////////////////////////////

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
        public static bool IsDisabled(
            ISubCommand subCommand
            )
        {
            return (subCommand != null) ?
                FlagOps.HasFlags(subCommand.CommandFlags,
                    CommandFlags.Disabled, true) : false;
        }

        ///////////////////////////////////////////////////////////////////////

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

        public static Array GetSystemArray(
            IVariable variable
            )
        {
            if (variable == null)
                return null;

            return variable.Value as Array;
        }

        ///////////////////////////////////////////////////////////////////////

        public static VariableFlags GetWatchpointFlags(
            VariableFlags flags
            )
        {
            return flags & VariableFlags.WatchpointMask;
        }

        ///////////////////////////////////////////////////////////////////////

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
        public static IVariable FollowLinks(
            IVariable variable,
            VariableFlags flags
            )
        {
            Result error = null;

            return FollowLinks(variable, flags, ref error);
        }

        ///////////////////////////////////////////////////////////////////////

        public static IVariable FollowLinks(
            IVariable variable,
            VariableFlags flags,
            ref Result error
            )
        {
            return FollowLinks(variable, flags, Count.Invalid, ref error);
        }

        ///////////////////////////////////////////////////////////////////////

        public static IVariable FollowLinks(
            IVariable variable,
            VariableFlags flags,
            int limit,
            ref Result error
            )
        {
            if (variable == null)
            {
                error = "invalid variable";
                return null;
            }

            if (FlagOps.HasFlags(flags, VariableFlags.NoFollowLink, true))
                return variable;

            bool noUsable = false;

            if (FlagOps.HasFlags(flags, VariableFlags.NoUsable, true))
                noUsable = true;

            int count = 0;
            IVariable linkVariable = variable.Link;

            while (linkVariable != null)
            {
                if ((limit > 0) && (count++ >= limit))
                    break;

                Result linkError = null;

                if (!noUsable && !linkVariable.IsUsable(ref linkError))
                {
                    error = String.Format(
                        "can't follow from {0} to {1}: {2}",
                        FormatOps.ErrorVariableName(variable.Name),
                        FormatOps.ErrorVariableName(linkVariable.Name),
                        FormatOps.DisplayString(linkError));

                    return null;
                }

                variable = linkVariable;
                linkVariable = linkVariable.Link;
            }

            return variable; /* NOTE: Cannot be null at this point. */
        }

        ///////////////////////////////////////////////////////////////////////

        public static bool HasTraces(
            IVariable variable
            )
        {
            return (variable != null) && variable.HasTraces();
        }

        ///////////////////////////////////////////////////////////////////////

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

        public static bool IsUndefined2(
            IVariable variable
            )
        {
            if (variable == null)
                return false;

            return variable.HasFlags(VariableFlags.Undefined, true);
        }

        ///////////////////////////////////////////////////////////////////////

        public static bool IsWriteOnly(
            IVariable variable
            )
        {
            if (variable == null)
                return false;

            return variable.HasFlags(VariableFlags.WriteOnly, true);
        }

        ///////////////////////////////////////////////////////////////////////

        public static bool IsVirtualOrSystem(
            IVariable variable
            )
        {
            if (variable == null)
                return false;

            return variable.HasFlags(VariableFlags.VirtualOrSystemMask, false);
        }

        ///////////////////////////////////////////////////////////////////////

        public static bool IsArray2(
            IVariable variable
            )
        {
            if (variable == null)
                return false;

            return variable.HasFlags(VariableFlags.Array, true);
        }

        ///////////////////////////////////////////////////////////////////////

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

        public static bool IsVirtual(
            IVariable variable
            )
        {
            if (variable == null)
                return false;

            return variable.HasFlags(VariableFlags.Virtual, true);
        }

        ///////////////////////////////////////////////////////////////////////

        public static bool IsBreakOnGet(
            IVariable variable
            )
        {
            if (variable == null)
                return false;

            return variable.HasFlags(VariableFlags.BreakOnGet, true);
        }

        ///////////////////////////////////////////////////////////////////////

        public static bool IsBreakOnSet(
            IVariable variable
            )
        {
            if (variable == null)
                return false;

            return variable.HasFlags(VariableFlags.BreakOnSet, true);
        }

        ///////////////////////////////////////////////////////////////////////

        public static bool IsBreakOnUnset(
            IVariable variable
            )
        {
            if (variable == null)
                return false;

            return variable.HasFlags(VariableFlags.BreakOnUnset, true);
        }

        ///////////////////////////////////////////////////////////////////////

        public static bool IsDirty(
            IVariable variable
            )
        {
            if (variable == null)
                return false;

            return variable.HasFlags(VariableFlags.Dirty, true);
        }

        ///////////////////////////////////////////////////////////////////////

        public static bool IsEvaluate(
            IVariable variable
            )
        {
            if (variable == null)
                return false;

            return variable.HasFlags(VariableFlags.Evaluate, true);
        }

        ///////////////////////////////////////////////////////////////////////

        public static bool IsLink(
            IVariable variable
            )
        {
            if (variable == null)
                return false;

            return variable.HasFlags(VariableFlags.Link, true);
        }

        ///////////////////////////////////////////////////////////////////////

        public static bool IsNoTrace(
            IVariable variable
            )
        {
            if (variable == null)
                return false;

            return variable.HasFlags(VariableFlags.NoTrace, true);
        }

        ///////////////////////////////////////////////////////////////////////

        public static bool IsNoWatchpoint(
            IVariable variable
            )
        {
            if (variable == null)
                return false;

            return variable.HasFlags(VariableFlags.NoWatchpoint, true);
        }

        ///////////////////////////////////////////////////////////////////////

        public static bool IsNoPostProcess(
            IVariable variable
            )
        {
            if (variable == null)
                return false;

            return variable.HasFlags(VariableFlags.NoPostProcess, true);
        }

        ///////////////////////////////////////////////////////////////////////

        public static bool IsNoNotify(
            IVariable variable
            )
        {
            if (variable == null)
                return false;

            return variable.HasFlags(VariableFlags.NoNotify, true);
        }

        ///////////////////////////////////////////////////////////////////////

        public static bool IsReadOnly(
            IVariable variable
            )
        {
            if (variable == null)
                return false;

            return variable.HasFlags(VariableFlags.ReadOnly, true);
        }

        ///////////////////////////////////////////////////////////////////////

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

        public static bool IsSubstitute(
            IVariable variable
            )
        {
            if (variable == null)
                return false;

            return variable.HasFlags(VariableFlags.Substitute, true);
        }

        ///////////////////////////////////////////////////////////////////////

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

        public static bool IsSystem(
            IVariable variable
            )
        {
            if (variable == null)
                return false;

            return variable.HasFlags(VariableFlags.System, true);
        }

        ///////////////////////////////////////////////////////////////////////

        public static bool IsMutable(
            IVariable variable
            )
        {
            if (variable == null)
                return false;

            return variable.HasFlags(VariableFlags.Mutable, true);
        }

        ///////////////////////////////////////////////////////////////////////

        public static bool IsInvariant(
            IVariable variable
            )
        {
            if (variable == null)
                return false;

            return variable.HasFlags(VariableFlags.Invariant, true);
        }

        ///////////////////////////////////////////////////////////////////////

        public static bool IsWait(
            IVariable variable
            )
        {
            if (variable == null)
                return false;

            return variable.HasFlags(VariableFlags.Wait, true);
        }

        ///////////////////////////////////////////////////////////////////////

        public static bool IsWait(
            IVariable variable,
            string index
            )
        {
            return CheckElementFlags(
                variable, index, VariableFlags.Wait, true);
        }

        ///////////////////////////////////////////////////////////////////////

        public static bool IsUndefined(
            IVariable variable
            )
        {
            //
            // HACK: Also check if the call frame is undefined.  Technically,
            //       this is now always required and so we do this here rather
            //       than propogate this check all throughout the code.
            //
            if (variable == null)
                return false;

            if (CallFrameOps.IsDisposedOrUndefined(variable.Frame))
                return true;

            return variable.HasFlags(VariableFlags.Undefined, true);
        }

        ///////////////////////////////////////////////////////////////////////

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
        public static bool IsNowClean(
            VariableFlags oldFlags,
            VariableFlags newFlags
            )
        {
            return FlagOps.HasFlags(oldFlags, VariableFlags.Dirty, true) &&
                !FlagOps.HasFlags(newFlags, VariableFlags.Dirty, true);
        }

        ///////////////////////////////////////////////////////////////////////

        public static bool IsNowDirty(
            VariableFlags oldFlags,
            VariableFlags newFlags
            )
        {
            return !FlagOps.HasFlags(oldFlags, VariableFlags.Dirty, true) &&
                FlagOps.HasFlags(newFlags, VariableFlags.Dirty, true);
        }

        ///////////////////////////////////////////////////////////////////////

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

        public static bool ResetCallFrame(
            Interpreter interpreter,
            IVariable variable,
            ICallFrame frame
            )
        {
            if ((interpreter != null) && (variable != null))
            {
                variable.Frame = frame;

                /* IGNORED */
                interpreter.MaybeSetQualifiedName(variable);

                return true;
            }

            return false;
        }

        ///////////////////////////////////////////////////////////////////////

        #region Array Element Mutator Methods
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

        public static long GetToken(
            IWrapperData wrapper
            )
        {
            if (wrapper != null)
                return wrapper.Token;

            return 0;
        }

        ///////////////////////////////////////////////////////////////////////

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

        public static void SetToken(
            IWrapperData wrapper,
            long token
            )
        {
            if (wrapper != null)
                wrapper.Token = token;
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Identifier Support Methods
        public static Guid GetId(
            IIdentifierBase identifierBase
            )
        {
            if (identifierBase != null)
                return identifierBase.Id;

            return Guid.Empty;
        }

        ///////////////////////////////////////////////////////////////////////

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

        public static string GetName(
            IIdentifierName identifierName
            )
        {
            if (identifierName != null)
                return identifierName.Name;

            return null;
        }

        ///////////////////////////////////////////////////////////////////////

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

        public static string GetNameNoThrow(
            object @object
            )
        {
            return GetNameNoThrow(@object, true);
        }

        ///////////////////////////////////////////////////////////////////////

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

        public static string GetNameNoThrow(
            Process process
            )
        {
            if (process != null)
            {
                try
                {
                    return process.ToString();
                }
                catch
                {
                    // do nothing.
                }
            }

            return null;
        }

        ///////////////////////////////////////////////////////////////////////

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
        public static bool IsViaSource(
            IScriptLocation location
            )
        {
            return ((location != null) && location.ViaSource);
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Interpreter Support Methods
        public static bool IsUsable(
            Interpreter interpreter
            )
        {
            return ((interpreter != null) &&
                !Interpreter.IsDeletedOrDisposed(interpreter, false));
        }

        ///////////////////////////////////////////////////////////////////////

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
