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
    /// <summary>
    /// This interface describes the metadata for a trace callback, including
    /// the reflected method that implements the trace and the various flags
    /// that govern how and when it is invoked.  It composes the trace
    /// identity (<see cref="IIdentifier" />), the owning plugin
    /// (<see cref="IHavePlugin" />), wrapper bookkeeping
    /// (<see cref="IWrapperData" />), and the target type and name
    /// (<see cref="ITypeAndName" />).
    /// </summary>
    [ObjectId("b35ce743-e9b6-4fe3-85b9-91a67657ad62")]
    public interface ITraceData : IIdentifier, IHavePlugin, IWrapperData, ITypeAndName
    {
        /// <summary>
        /// Gets or sets the name of the trace method.
        /// </summary>
        //
        // NOTE: The name of the trace method.
        //
        string MethodName { get; set; }

        /// <summary>
        /// Gets or sets the binding flags used to locate the trace method.
        /// </summary>
        //
        // NOTE: The binding flags for the trace method.
        //
        BindingFlags BindingFlags { get; set; }

        /// <summary>
        /// Gets or sets the method flags for the trace method.
        /// </summary>
        //
        // NOTE: The flags for the trace method.
        //
        MethodFlags MethodFlags { get; set; }

        /// <summary>
        /// Gets or sets the flags for the trace.
        /// </summary>
        //
        // NOTE: The flags for the trace.
        //
        TraceFlags TraceFlags { get; set; }
    }
}
