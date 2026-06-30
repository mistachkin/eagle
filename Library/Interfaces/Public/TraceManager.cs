/*
 * TraceManager.cs --
 *
 * Copyright (c) 2007-2012 by Joe Mistachkin.  All rights reserved.
 *
 * See the file "license.terms" for information on usage and redistribution of
 * this file, and for a DISCLAIMER OF ALL WARRANTIES.
 *
 * RCS: @(#) $Id: $
 */

using Eagle._Attributes;
using Eagle._Components.Public;

namespace Eagle._Interfaces.Public
{
    /// <summary>
    /// This interface exposes the static trace operations state so that it
    /// can be queried and adjusted, including from another application
    /// domain.  It provides access to the active and configured trace
    /// priorities, trace status management, and trace limit adjustments.
    /// </summary>
    //
    // WARNING: This interface exists only to facilitate accessing the
    //          static TraceOps state from another application domain.
    //
    [ObjectId("8ef725d2-2803-4b7c-9625-1abd65a3fb7e")]
    public interface ITraceManager
    {
        /// <summary>
        /// Gets the current trace priority.
        /// </summary>
        /// <returns>
        /// The current trace priority.
        /// </returns>
        TracePriority GetTracePriority();

        /// <summary>
        /// Adjusts a trace priority by the specified amount.
        /// </summary>
        /// <param name="priority">
        /// The trace priority to adjust, in place.
        /// </param>
        /// <param name="adjustment">
        /// The amount by which to adjust the trace priority.
        /// </param>
        void AdjustTracePriority(
            ref TracePriority priority,
            int adjustment
        );

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Gets the set of currently enabled trace priorities.
        /// </summary>
        /// <returns>
        /// The set of currently enabled trace priorities.
        /// </returns>
        TracePriority GetTracePriorities();

        /// <summary>
        /// Sets the set of enabled trace priorities.
        /// </summary>
        /// <param name="priorities">
        /// The set of trace priorities to enable.
        /// </param>
        void SetTracePriorities(
            TracePriority priorities
        );

        /// <summary>
        /// Enables or disables one or more trace priorities.
        /// </summary>
        /// <param name="priority">
        /// The trace priority, or priorities, to adjust.
        /// </param>
        /// <param name="enabled">
        /// Non-zero to enable the specified priorities; zero to disable them.
        /// </param>
        void AdjustTracePriorities(
            TracePriority priority,
            bool enabled
        );

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Resets the trace status to its default state.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter context to use, if any.  This parameter may be
        /// null.
        /// </param>
        /// <param name="overrideEnvironment">
        /// Non-zero to ignore any trace settings present in the process
        /// environment.
        /// </param>
        void ResetTraceStatus(
            Interpreter interpreter,
            bool overrideEnvironment
        );

        /// <summary>
        /// Forces tracing to be enabled or disabled, returning the previous
        /// trace state.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter context to use, if any.  This parameter may be
        /// null.
        /// </param>
        /// <param name="stateType">
        /// The trace state type to modify.
        /// </param>
        /// <param name="enabled">
        /// Non-zero to enable tracing; zero to disable it.
        /// </param>
        /// <returns>
        /// The trace state in effect prior to this call.
        /// </returns>
        TraceStateType ForceTraceEnabledOrDisabled(
            Interpreter interpreter,
            TraceStateType stateType,
            bool enabled
        );

        /// <summary>
        /// Processes the specified trace client data.
        /// </summary>
        /// <param name="traceClientData">
        /// The trace client data to process.  This parameter should not be
        /// null.
        /// </param>
        /// <param name="result">
        /// Upon success, this may contain the result value produced while
        /// processing the trace client data.  Upon failure, this must contain
        /// an appropriate error message.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise, a non-Ok value
        /// with details placed in the <paramref name="result" /> parameter.
        /// </returns>
        ReturnCode ProcessTraceClientData(
            TraceClientData traceClientData,
            ref Result result
        );

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Adjusts the configured trace limits, optionally enabling or
        /// disabling them.
        /// </summary>
        /// <param name="enable">
        /// Non-zero to enable the trace limits; zero to disable them; null to
        /// leave their enabled state unchanged.
        /// </param>
        /// <returns>
        /// True if the trace limits were adjusted; otherwise, false.
        /// </returns>
        bool MaybeAdjustTraceLimits(
            bool? enable
        );
    }
}
