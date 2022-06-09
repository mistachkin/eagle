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
    [ObjectId("fe0b13aa-48c3-4992-8b1e-eb2a80ce33cb")]
    internal sealed class Trace : Default, ITrace
    {
        #region Public Constructors
        public Trace(
            long token,
            ITrace trace
            )
            : base(token)
        {
            this.trace = trace;
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Private Data
        internal ITrace trace;
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region IIdentifierName Members
        public string Name
        {
            get { return (trace != null) ? trace.Name : null; }
            set { if (trace != null) { trace.Name = value; } }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region IIdentifierBase Members
        public IdentifierKind Kind
        {
            get { return (trace != null) ? trace.Kind : IdentifierKind.None; }
            set { if (trace != null) { trace.Kind = value; } }
        }

        ///////////////////////////////////////////////////////////////////////

        public Guid Id
        {
            get { return (trace != null) ? trace.Id : Guid.Empty; }
            set { if (trace != null) { trace.Id = value; } }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region IIdentifier Members
        public string Group
        {
            get { return (trace != null) ? trace.Group : null; }
            set { if (trace != null) { trace.Group = value; } }
        }

        ///////////////////////////////////////////////////////////////////////

        public string Description
        {
            get { return (trace != null) ? trace.Description : null; }
            set { if (trace != null) { trace.Description = value; } }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region IGetClientData / ISetClientData Members
        public IClientData ClientData
        {
            get { return (trace != null) ? trace.ClientData : null; }
            set { if (trace != null) { trace.ClientData = value; } }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region IDynamicExecuteTrace Members
        public TraceCallback Callback
        {
            get { return (trace != null) ? trace.Callback : null; }
            set { if (trace != null) { trace.Callback = value; } }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region IExecuteTrace Members
        public ReturnCode Execute(
            BreakpointType breakpointType,
            Interpreter interpreter,
            ITraceInfo traceInfo,
            ref Result result
            )
        {
            if (trace != null)
                return trace.Execute(
                    breakpointType, interpreter, traceInfo, ref result);
            else
                return ReturnCode.Error;
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region IHavePlugin Members
        public IPlugin Plugin
        {
            get { return (trace != null) ? trace.Plugin : null; }
            set { if (trace != null) { trace.Plugin = value; } }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region ITraceData Members
        public string TypeName
        {
            get { return (trace != null) ? trace.TypeName : null; }
            set { if (trace != null) { trace.TypeName = value; } }
        }

        ///////////////////////////////////////////////////////////////////////

        public string MethodName
        {
            get { return (trace != null) ? trace.MethodName : null; }
            set { if (trace != null) { trace.MethodName = value; } }
        }

        ///////////////////////////////////////////////////////////////////////

        public BindingFlags BindingFlags
        {
            get { return (trace != null) ? trace.BindingFlags : BindingFlags.Default; }
            set { if (trace != null) { trace.BindingFlags = value; } }
        }

        ///////////////////////////////////////////////////////////////////////

        public MethodFlags MethodFlags
        {
            get { return (trace != null) ? trace.MethodFlags : MethodFlags.None; }
            set { if (trace != null) { trace.MethodFlags = value; } }
        }

        ///////////////////////////////////////////////////////////////////////

        public TraceFlags TraceFlags
        {
            get { return (trace != null) ? trace.TraceFlags : TraceFlags.None; }
            set { if (trace != null) { trace.TraceFlags = value; } }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region ISetup Members
        public ReturnCode Setup(
            ref Result error
            )
        {
            if (trace != null)
                return trace.Setup(ref error);
            else
                return ReturnCode.Error;
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region IWrapper Members
        public override bool IsDisposable
        {
            get { return false; }
        }

        ///////////////////////////////////////////////////////////////////////

        public override object Object
        {
            get { return trace; }
        }
        #endregion
    }
}
