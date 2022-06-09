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
    [ObjectId("79e784cf-30a0-4653-839c-070da0030096")]
    public interface ITraceInfo
    {
        ITrace Trace { get; set; }
        BreakpointType BreakpointType { get; set; }
        ICallFrame Frame { get; set; }
        IVariable Variable { get; set; }
        string Name { get; set; }
        string Index { get; set; }
        VariableFlags Flags { get; set; }
        object OldValue { get; set; }
        object NewValue { get; set; }
        ElementDictionary OldValues { get; set; }
        ElementDictionary NewValues { get; set; }
        StringList List { get; set; }
        bool Cancel { get; set; }
        bool PostProcess { get; set; }
        ReturnCode ReturnCode { get; set; }

        ITraceInfo Copy();

        ITraceInfo Update(ITraceInfo traceInfo);

        ITraceInfo Update(
            ITrace trace, BreakpointType breakpointType, ICallFrame frame,
            IVariable variable, string name, string index, VariableFlags flags,
            object oldValue, object newValue, ElementDictionary oldValues,
            ElementDictionary newValues, StringList list, bool cancel,
            bool postProcess, ReturnCode returnCode);

        StringPairList ToStringPairList();
    }
}
