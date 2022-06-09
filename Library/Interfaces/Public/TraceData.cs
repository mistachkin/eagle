/*
 * TraceData.cs --
 *
 * Copyright (c) 2007-2012 by Joe Mistachkin.  All rights reserved.
 *
 * See the file "license.terms" for information on usage and redistribution of
 * this file, and for a DISCLAIMER OF ALL WARRANTIES.
 *
 * RCS: @(#) $Id: $
 */

using System.Reflection;
using Eagle._Attributes;
using Eagle._Components.Public;

namespace Eagle._Interfaces.Public
{
    [ObjectId("b35ce743-e9b6-4fe3-85b9-91a67657ad62")]
    public interface ITraceData : IIdentifier, IHavePlugin, IWrapperData
    {
        //
        // NOTE: The fully qualified type name for this trace (not including
        //       the assembly name).
        //
        string TypeName { get; set; }

        //
        // NOTE: The name of the trace method.
        //
        string MethodName { get; set; }

        //
        // NOTE: The binding flags for the trace method.
        //
        BindingFlags BindingFlags { get; set; }

        //
        // NOTE: The flags for the trace method.
        //
        MethodFlags MethodFlags { get; set; }

        //
        // NOTE: The flags for the trace.
        //
        TraceFlags TraceFlags { get; set; }
    }
}
