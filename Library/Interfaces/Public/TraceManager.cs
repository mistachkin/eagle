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
    //
    // WARNING: This interface exists only to facilitate accessing the
    //          static TraceOps state from another application domain.
    //
    [ObjectId("8ef725d2-2803-4b7c-9625-1abd65a3fb7e")]
    public interface ITraceManager
    {
        TracePriority GetTracePriority();

        void AdjustTracePriority(
            ref TracePriority priority,
            int adjustment
        );

        ///////////////////////////////////////////////////////////////////////

        TracePriority GetTracePriorities();

        void SetTracePriorities(
            TracePriority priorities
        );

        void AdjustTracePriorities(
            TracePriority priority,
            bool enabled
        );

        ///////////////////////////////////////////////////////////////////////

        void ResetTraceStatus(
            Interpreter interpreter,
            bool overrideEnvironment
        );

        TraceStateType ForceTraceEnabledOrDisabled(
            Interpreter interpreter,
            TraceStateType stateType,
            bool enabled
        );

        ReturnCode ProcessTraceClientData(
            TraceClientData traceClientData,
            ref Result result
        );

        ///////////////////////////////////////////////////////////////////////

        bool MaybeAdjustTraceLimits(
            bool? enable
        );
    }
}
