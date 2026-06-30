/*
 * InteractiveLoopData.cs --
 *
 * Copyright (c) 2007-2012 by Joe Mistachkin.  All rights reserved.
 *
 * See the file "license.terms" for information on usage and redistribution of
 * this file, and for a DISCLAIMER OF ALL WARRANTIES.
 *
 * RCS: @(#) $Id: $
 */

using System.Collections.Generic;
using Eagle._Attributes;
using Eagle._Components.Public;
using Eagle._Containers.Public;

namespace Eagle._Interfaces.Public
{
    /// <summary>
    /// This interface defines the data used to drive a single invocation of
    /// the Eagle interactive loop.  It carries the inputs that describe why
    /// the loop was entered (e.g. the breakpoint that triggered it), the
    /// various flags that govern engine, substitution, event, and expression
    /// behavior while the loop runs, and the outputs (such as the return code
    /// and whether the loop should exit) that report the loop outcome.
    /// </summary>
    [ObjectId("af9b6a9a-2260-4185-9ab5-fa72eb6f593c")]
    public interface IInteractiveLoopData : IIdentifier, IHaveClientData
    {
        /// <summary>
        /// Gets or sets a value indicating whether the interactive loop is
        /// being entered for debugging purposes.
        /// </summary>
        bool Debug { get; set; }

        /// <summary>
        /// Gets or sets the arguments associated with the interactive loop.
        /// </summary>
        IEnumerable<string> Args { get; set; }

        /// <summary>
        /// Gets or sets the <see cref="ReturnCode" /> associated with the
        /// interactive loop.
        /// </summary>
        ReturnCode Code { get; set; }

        /// <summary>
        /// Gets or sets the <see cref="BreakpointType" /> that caused the
        /// interactive loop to be entered.
        /// </summary>
        BreakpointType BreakpointType { get; set; }

        /// <summary>
        /// Gets or sets the name of the breakpoint that caused the interactive
        /// loop to be entered.
        /// </summary>
        string BreakpointName { get; set; }

        /// <summary>
        /// Gets or sets the script token associated with the active
        /// breakpoint, if any.
        /// </summary>
        IToken Token { get; set; }

        /// <summary>
        /// Gets or sets the trace information associated with the active
        /// breakpoint, if any.
        /// </summary>
        ITraceInfo TraceInfo { get; set; }

        /// <summary>
        /// Gets or sets the <see cref="EngineFlags" /> in effect for the
        /// interactive loop.
        /// </summary>
        EngineFlags EngineFlags { get; set; }

        /// <summary>
        /// Gets or sets the <see cref="SubstitutionFlags" /> in effect for the
        /// interactive loop.
        /// </summary>
        SubstitutionFlags SubstitutionFlags { get; set; }

        /// <summary>
        /// Gets or sets the <see cref="EventFlags" /> in effect for the
        /// interactive loop.
        /// </summary>
        EventFlags EventFlags { get; set; }

        /// <summary>
        /// Gets or sets the <see cref="ExpressionFlags" /> in effect for the
        /// interactive loop.
        /// </summary>
        ExpressionFlags ExpressionFlags { get; set; }

        /// <summary>
        /// Gets or sets the <see cref="HeaderFlags" /> that control what
        /// header information is displayed by the interactive loop.
        /// </summary>
        HeaderFlags HeaderFlags { get; set; }

        /// <summary>
        /// Gets or sets the <see cref="DetailFlags" /> that control how much
        /// detail is displayed by the interactive loop.
        /// </summary>
        DetailFlags DetailFlags { get; set; }

        /// <summary>
        /// Gets or sets the list of arguments for the command currently being
        /// processed by the interactive loop.
        /// </summary>
        ArgumentList Arguments { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the interactive loop should
        /// exit.
        /// </summary>
        bool Exit { get; set; }

        /// <summary>
        /// Sets the value indicating that the interactive loop should exit.
        /// </summary>
        void SetExit();

        /// <summary>
        /// Sets the <see cref="ReturnCode" /> associated with the interactive
        /// loop.
        /// </summary>
        /// <param name="code">
        /// The return code to associate with the interactive loop.
        /// </param>
        void SetCode(ReturnCode code);
    }
}
