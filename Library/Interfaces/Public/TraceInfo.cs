/*
 * TraceInfo.cs --
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
using Eagle._Containers.Public;

namespace Eagle._Interfaces.Public
{
    /// <summary>
    /// This interface represents the contextual information passed to a
    /// variable trace callback when a traced variable is read, written, or
    /// unset.  It carries the trace being processed, the breakpoint context,
    /// the affected call frame and variable, the variable name and array
    /// element index, the old and new values, and flags that allow the
    /// callback to influence the outcome of the traced operation.
    /// </summary>
    [ObjectId("79e784cf-30a0-4653-839c-070da0030096")]
    public interface ITraceInfo
    {
        /// <summary>
        /// Gets or sets the trace associated with this information.
        /// </summary>
        ITrace Trace { get; set; }
        /// <summary>
        /// Gets or sets the breakpoint type that triggered this trace.
        /// </summary>
        BreakpointType BreakpointType { get; set; }
        /// <summary>
        /// Gets or sets the call frame associated with the traced variable.
        /// </summary>
        ICallFrame Frame { get; set; }
        /// <summary>
        /// Gets or sets the variable being traced.
        /// </summary>
        IVariable Variable { get; set; }
        /// <summary>
        /// Gets or sets the name of the variable being traced.
        /// </summary>
        string Name { get; set; }
        /// <summary>
        /// Gets or sets the array element index being traced, if any.
        /// </summary>
        string Index { get; set; }
        /// <summary>
        /// Gets or sets the variable flags associated with this trace.
        /// </summary>
        VariableFlags Flags { get; set; }
        /// <summary>
        /// Gets or sets the previous value of the variable, if any.
        /// </summary>
        object OldValue { get; set; }
        /// <summary>
        /// Gets or sets the new value of the variable, if any.
        /// </summary>
        object NewValue { get; set; }
        /// <summary>
        /// Gets or sets the previous array element values, if any.
        /// </summary>
        ElementDictionary OldValues { get; set; }
        /// <summary>
        /// Gets or sets the new array element values, if any.
        /// </summary>
        ElementDictionary NewValues { get; set; }
        /// <summary>
        /// Gets or sets the list of values associated with this trace, if any.
        /// </summary>
        StringList List { get; set; }
        /// <summary>
        /// Gets or sets a value indicating whether the traced operation should
        /// be canceled.
        /// </summary>
        bool Cancel { get; set; }
        /// <summary>
        /// Gets or sets a value indicating whether this trace is being invoked
        /// during post-processing of the traced operation.
        /// </summary>
        bool PostProcess { get; set; }
        /// <summary>
        /// Gets or sets the return code produced by the trace callback.
        /// </summary>
        ReturnCode ReturnCode { get; set; }

        /// <summary>
        /// Creates a deep copy of this trace information.
        /// </summary>
        /// <returns>
        /// The newly created copy of this trace information.
        /// </returns>
        ITraceInfo Copy();

        /// <summary>
        /// Updates this trace information using the values from another
        /// instance.
        /// </summary>
        /// <param name="traceInfo">
        /// The trace information to copy values from.  This parameter should
        /// not be null.
        /// </param>
        /// <returns>
        /// The updated trace information.
        /// </returns>
        ITraceInfo Update(ITraceInfo traceInfo);

        /// <summary>
        /// Updates this trace information using the supplied values.
        /// </summary>
        /// <param name="trace">
        /// The trace to associate with this information.
        /// </param>
        /// <param name="breakpointType">
        /// The breakpoint type that triggered this trace.
        /// </param>
        /// <param name="frame">
        /// The call frame associated with the traced variable.
        /// </param>
        /// <param name="variable">
        /// The variable being traced.
        /// </param>
        /// <param name="name">
        /// The name of the variable being traced.
        /// </param>
        /// <param name="index">
        /// The array element index being traced, if any.
        /// </param>
        /// <param name="flags">
        /// The variable flags associated with this trace.
        /// </param>
        /// <param name="oldValue">
        /// The previous value of the variable, if any.
        /// </param>
        /// <param name="newValue">
        /// The new value of the variable, if any.
        /// </param>
        /// <param name="oldValues">
        /// The previous array element values, if any.
        /// </param>
        /// <param name="newValues">
        /// The new array element values, if any.
        /// </param>
        /// <param name="list">
        /// The list of values associated with this trace, if any.
        /// </param>
        /// <param name="cancel">
        /// Non-zero if the traced operation should be canceled.
        /// </param>
        /// <param name="postProcess">
        /// Non-zero if this trace is being invoked during post-processing of
        /// the traced operation.
        /// </param>
        /// <param name="returnCode">
        /// The return code to associate with the trace callback.
        /// </param>
        /// <returns>
        /// The updated trace information.
        /// </returns>
        ITraceInfo Update(
            ITrace trace, BreakpointType breakpointType, ICallFrame frame,
            IVariable variable, string name, string index, VariableFlags flags,
            object oldValue, object newValue, ElementDictionary oldValues,
            ElementDictionary newValues, StringList list, bool cancel,
            bool postProcess, ReturnCode returnCode);

        /// <summary>
        /// Converts this trace information into a list of name/value pairs.
        /// </summary>
        /// <returns>
        /// The list of name/value pairs representing this trace information.
        /// </returns>
        StringPairList ToStringPairList();
    }
}
