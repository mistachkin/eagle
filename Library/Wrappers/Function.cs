/*
 * Function.cs --
 *
 * Copyright (c) 2007-2012 by Joe Mistachkin.  All rights reserved.
 *
 * See the file "license.terms" for information on usage and redistribution of
 * this file, and for a DISCLAIMER OF ALL WARRANTIES.
 *
 * RCS: @(#) $Id: $
 */

using System;
using Eagle._Attributes;
using Eagle._Components.Public;
using Eagle._Containers.Public;
using Eagle._Interfaces.Public;

namespace Eagle._Wrappers
{
    [ObjectId("3b367479-8554-46ee-9d62-6c366e153ee4")]
    internal sealed class Function : Default, IFunction
    {
        #region Public Constructors
        public Function(
            long token,
            IFunction function
            )
            : base(token)
        {
            this.function = function;
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Private Data
        internal IFunction function;
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region IIdentifierName Members
        public string Name
        {
            get { return (function != null) ? function.Name : null; }
            set { if (function != null) { function.Name = value; } }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region IIdentifierBase Members
        public IdentifierKind Kind
        {
            get { return (function != null) ? function.Kind : IdentifierKind.None; }
            set { if (function != null) { function.Kind = value; } }
        }

        ///////////////////////////////////////////////////////////////////////

        public Guid Id
        {
            get { return (function != null) ? function.Id : Guid.Empty; }
            set { if (function != null) { function.Id = value; } }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region IIdentifier Members
        public string Group
        {
            get { return (function != null) ? function.Group : null; }
            set { if (function != null) { function.Group = value; } }
        }

        ///////////////////////////////////////////////////////////////////////

        public string Description
        {
            get { return (function != null) ? function.Description : null; }
            set { if (function != null) { function.Description = value; } }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region IGetClientData / ISetClientData Members
        public IClientData ClientData
        {
            get { return (function != null) ? function.ClientData : null; }
            set { if (function != null) { function.ClientData = value; } }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region IState Members
        public bool Initialized
        {
            get { return (function != null) ? function.Initialized : false; }
            set { if (function != null) { function.Initialized = value; } }
        }

        ///////////////////////////////////////////////////////////////////////

        public ReturnCode Initialize(
            Interpreter interpreter,
            IClientData clientData,
            ref Result result
            )
        {
            if (function != null)
                return function.Initialize(interpreter, clientData, ref result);
            else
                return ReturnCode.Error;
        }

        ///////////////////////////////////////////////////////////////////////

        public ReturnCode Terminate(
            Interpreter interpreter,
            IClientData clientData,
            ref Result result
            )
        {
            if (function != null)
                return function.Terminate(interpreter, clientData, ref result);
            else
                return ReturnCode.Error;
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region IHavePlugin Members
        public IPlugin Plugin
        {
            get { return (function != null) ? function.Plugin : null; }
            set { if (function != null) { function.Plugin = value; } }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region ITypeAndName Members
        public string TypeName
        {
            get { return (function != null) ? function.TypeName : null; }
            set { if (function != null) { function.TypeName = value; } }
        }

        ///////////////////////////////////////////////////////////////////////

        public Type Type
        {
            get { return (function != null) ? function.Type : null; }
            set { if (function != null) { function.Type = value; } }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region IFunctionData Members
        public int Arguments
        {
            get { return (function != null) ? function.Arguments : 0; }
            set { if (function != null) { function.Arguments = value; } }
        }

        ///////////////////////////////////////////////////////////////////////

        public TypeList Types
        {
            get { return (function != null) ? function.Types : null; }
            set { if (function != null) { function.Types = value; } }
        }

        ///////////////////////////////////////////////////////////////////////

        public FunctionFlags Flags
        {
            get { return (function != null) ? function.Flags : FunctionFlags.None; }
            set { if (function != null) { function.Flags = value; } }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region IExecuteArgument Members
        public ReturnCode Execute(
            Interpreter interpreter,
            IClientData clientData,
            ArgumentList arguments,
            ref Argument value,
            ref Result error
            )
        {
            if (function != null)
                return function.Execute(
                    interpreter, clientData, arguments, ref value, ref error);
            else
                return ReturnCode.Error;
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region IUsageData Members
        public bool ResetUsage(
            UsageType type,
            ref long value
            )
        {
            return (function != null) ?
                function.ResetUsage(type, ref value) : false;
        }

        ///////////////////////////////////////////////////////////////////////

        public bool GetUsage(
            UsageType type,
            ref long value
            )
        {
            return (function != null) ?
                function.GetUsage(type, ref value) : false;
        }

        ///////////////////////////////////////////////////////////////////////

        public bool SetUsage(
            UsageType type,
            ref long value
            )
        {
            return (function != null) ?
                function.SetUsage(type, ref value) : false;
        }

        ///////////////////////////////////////////////////////////////////////

        public bool AddUsage(
            UsageType type,
            ref long value
            )
        {
            return (function != null) ?
                function.AddUsage(type, ref value) : false;
        }

        ///////////////////////////////////////////////////////////////////////

        public bool CountUsage(
            ref long count
            )
        {
            return (function != null) ?
                function.CountUsage(ref count) : false;
        }

        ///////////////////////////////////////////////////////////////////////

        public bool ProfileUsage(
            ref long microseconds
            )
        {
            return (function != null) ?
                function.ProfileUsage(ref microseconds) : false;
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
            get { return function; }
        }
        #endregion
    }
}
