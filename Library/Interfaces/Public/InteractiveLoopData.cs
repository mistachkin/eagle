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
    [ObjectId("af9b6a9a-2260-4185-9ab5-fa72eb6f593c")]
    public interface IInteractiveLoopData : IIdentifier, IHaveClientData
    {
        bool Debug { get; set; }
        IEnumerable<string> Args { get; set; }
        ReturnCode Code { get; set; }
        BreakpointType BreakpointType { get; set; }
        string BreakpointName { get; set; }
        IToken Token { get; set; }
        ITraceInfo TraceInfo { get; set; }
        EngineFlags EngineFlags { get; set; }
        SubstitutionFlags SubstitutionFlags { get; set; }
        EventFlags EventFlags { get; set; }
        ExpressionFlags ExpressionFlags { get; set; }
        HeaderFlags HeaderFlags { get; set; }
        ArgumentList Arguments { get; set; }
        bool Exit { get; set; }
    }
}
