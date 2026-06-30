/*
 * Trace.cs --
 *
 * Copyright (c) 2007-2012 by Joe Mistachkin.  All rights reserved.
 *
 * See the file "license.terms" for information on usage and redistribution of
 * this file, and for a DISCLAIMER OF ALL WARRANTIES.
 *
 * RCS: @(#) $Id: $
 */

using System;
using System.Reflection;
using Eagle._Attributes;
using Eagle._Components.Public;
using Eagle._Components.Public.Delegates;
using Eagle._Interfaces.Public;

namespace Eagle._Wrappers
{
    /// <summary>
    /// This class wraps an <see cref="ITrace" /> instance, forwarding all
    /// member access to the wrapped target while gracefully handling the case
    /// where no target has been set (by returning default values or an error
    /// result).
    /// </summary>
    [ObjectId("fe0b13aa-48c3-4992-8b1e-eb2a80ce33cb")]
    internal sealed class Trace : Default, ITrace
    {
        #region Public Constructors
        /// <summary>
        /// Constructs an instance of this class with no wrapped trace target.
        /// </summary>
        public Trace()
            : base()
        {
            // do nothing.
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Private Data
        /// <summary>
        /// The wrapped trace instance that all members forward to.
        /// </summary>
        internal ITrace trace;
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region IIdentifierName Members
        /// <summary>
        /// Gets or sets the name of the wrapped trace.
        /// </summary>
        public string Name
        {
            get { return (trace != null) ? trace.Name : null; }
            set { if (trace != null) { trace.Name = value; } }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region IIdentifierBase Members
        /// <summary>
        /// Gets or sets the identifier kind of the wrapped trace.
        /// </summary>
        public IdentifierKind Kind
        {
            get { return (trace != null) ? trace.Kind : IdentifierKind.None; }
            set { if (trace != null) { trace.Kind = value; } }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Gets or sets the unique identifier of the wrapped trace.
        /// </summary>
        public Guid Id
        {
            get { return (trace != null) ? trace.Id : Guid.Empty; }
            set { if (trace != null) { trace.Id = value; } }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region IGetClientData / ISetClientData Members
        /// <summary>
        /// Gets or sets the client data associated with the wrapped trace.
        /// </summary>
        public IClientData ClientData
        {
            get { return (trace != null) ? trace.ClientData : null; }
            set { if (trace != null) { trace.ClientData = value; } }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region IIdentifier Members
        /// <summary>
        /// Gets or sets the group of the wrapped trace.
        /// </summary>
        public string Group
        {
            get { return (trace != null) ? trace.Group : null; }
            set { if (trace != null) { trace.Group = value; } }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Gets or sets the description of the wrapped trace.
        /// </summary>
        public string Description
        {
            get { return (trace != null) ? trace.Description : null; }
            set { if (trace != null) { trace.Description = value; } }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region IDynamicExecuteTrace Members
        /// <summary>
        /// Gets or sets the trace callback of the wrapped trace.
        /// </summary>
        public TraceCallback Callback
        {
            get { return (trace != null) ? trace.Callback : null; }
            set { if (trace != null) { trace.Callback = value; } }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region IExecuteTrace Members
        /// <summary>
        /// This method executes the wrapped trace for a variable operation.
        /// </summary>
        /// <param name="breakpointType">
        /// The breakpoint type describing the operation being traced.
        /// </param>
        /// <param name="interpreter">
        /// The interpreter context in which the trace is being executed.
        /// </param>
        /// <param name="traceInfo">
        /// The information describing the trace operation.
        /// </param>
        /// <param name="result">
        /// Upon success, receives any result produced by the wrapped trace.
        /// Upon failure, receives an appropriate error message.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise, an error code.
        /// </returns>
        public ReturnCode Execute(
            BreakpointType breakpointType,
            Interpreter interpreter,
            ITraceInfo traceInfo,
            ref Result result
            )
        {
            if (trace == null)
            {
                result = "invalid wrapper target";
                return ReturnCode.Error;
            }

            return trace.Execute(
                breakpointType, interpreter, traceInfo, ref result);
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region IHavePlugin Members
        /// <summary>
        /// Gets or sets the plugin that owns the wrapped trace.
        /// </summary>
        public IPlugin Plugin
        {
            get { return (trace != null) ? trace.Plugin : null; }
            set { if (trace != null) { trace.Plugin = value; } }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region ITypeAndName Members
        /// <summary>
        /// Gets or sets the type name of the wrapped trace.
        /// </summary>
        public string TypeName
        {
            get { return (trace != null) ? trace.TypeName : null; }
            set { if (trace != null) { trace.TypeName = value; } }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Gets or sets the type of the wrapped trace.
        /// </summary>
        public Type Type
        {
            get { return (trace != null) ? trace.Type : null; }
            set { if (trace != null) { trace.Type = value; } }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region ITraceData Members
        /// <summary>
        /// Gets or sets the method name of the wrapped trace.
        /// </summary>
        public string MethodName
        {
            get { return (trace != null) ? trace.MethodName : null; }
            set { if (trace != null) { trace.MethodName = value; } }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Gets or sets the reflection binding flags used to locate the trace
        /// method of the wrapped trace.
        /// </summary>
        public BindingFlags BindingFlags
        {
            get { return (trace != null) ? trace.BindingFlags : BindingFlags.Default; }
            set { if (trace != null) { trace.BindingFlags = value; } }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Gets or sets the method flags of the wrapped trace.
        /// </summary>
        public MethodFlags MethodFlags
        {
            get { return (trace != null) ? trace.MethodFlags : MethodFlags.None; }
            set { if (trace != null) { trace.MethodFlags = value; } }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Gets or sets the trace flags of the wrapped trace.
        /// </summary>
        public TraceFlags TraceFlags
        {
            get { return (trace != null) ? trace.TraceFlags : TraceFlags.None; }
            set { if (trace != null) { trace.TraceFlags = value; } }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region ISetup Members
        /// <summary>
        /// This method performs any setup required by the wrapped trace.
        /// </summary>
        /// <param name="error">
        /// Upon failure, receives an appropriate error message.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise, an error code.
        /// </returns>
        public ReturnCode Setup(
            ref Result error
            )
        {
            if (trace == null)
            {
                error = "invalid wrapper target";
                return ReturnCode.Error;
            }

            return trace.Setup(ref error);
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region IWrapper Members
        /// <summary>
        /// Gets a value indicating whether the wrapped object is disposable;
        /// always false for this wrapper.
        /// </summary>
        public override bool IsDisposable
        {
            get { return false; }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Gets or sets the wrapped object.  The value must be an
        /// <see cref="ITrace" />; otherwise, setting it throws.
        /// </summary>
        public override object Object
        {
            get { return trace; }
            set { trace = (ITrace)value; } /* throw */
        }
        #endregion
    }
}
