/*
 * Lambda.cs --
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
using Eagle._Components.Public.Delegates;
using Eagle._Containers.Public;
using Eagle._Interfaces.Private;
using Eagle._Interfaces.Public;

namespace Eagle._Wrappers
{
    [ObjectId("fad094ee-bd3a-4e0a-ae90-165bd7a14b26")]
    internal sealed class Lambda : Default, ILambda
    {
        #region Public Constructors
        public Lambda(
            long token,
            ILambda lambda
            )
            : base(token)
        {
            this.lambda = lambda;
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Private Data
        internal ILambda lambda;
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region IIdentifierName Members
        public string Name
        {
            get { return (lambda != null) ? lambda.Name : null; }
            set { if (lambda != null) { lambda.Name = value; } }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region IIdentifierBase Members
        public IdentifierKind Kind
        {
            get { return (lambda != null) ? lambda.Kind : IdentifierKind.None; }
            set { if (lambda != null) { lambda.Kind = value; } }
        }

        ///////////////////////////////////////////////////////////////////////

        public Guid Id
        {
            get { return (lambda != null) ? lambda.Id : Guid.Empty; }
            set { if (lambda != null) { lambda.Id = value; } }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region IIdentifier Members
        public string Group
        {
            get { return (lambda != null) ? lambda.Group : null; }
            set { if (lambda != null) { lambda.Group = value; } }
        }

        ///////////////////////////////////////////////////////////////////////

        public string Description
        {
            get { return (lambda != null) ? lambda.Description : null; }
            set { if (lambda != null) { lambda.Description = value; } }
        }

        ///////////////////////////////////////////////////////////////////////

        public IClientData ClientData
        {
            get { return (lambda != null) ? lambda.ClientData : null; }
            set { if (lambda != null) { lambda.ClientData = value; } }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region IUsageData Members
        public bool ResetUsage(
            UsageType type,
            ref long value
            )
        {
            return (lambda != null) ? lambda.ResetUsage(type, ref value) : false;
        }

        ///////////////////////////////////////////////////////////////////////

        public bool GetUsage(
            UsageType type,
            ref long value
            )
        {
            return (lambda != null) ? lambda.GetUsage(type, ref value) : false;
        }

        ///////////////////////////////////////////////////////////////////////

        public bool SetUsage(
            UsageType type,
            ref long value
            )
        {
            return (lambda != null) ? lambda.SetUsage(type, ref value) : false;
        }

        ///////////////////////////////////////////////////////////////////////

        public bool AddUsage(
            UsageType type,
            ref long value
            )
        {
            return (lambda != null) ?
                lambda.AddUsage(type, ref value) : false;
        }

        ///////////////////////////////////////////////////////////////////////

        public bool CountUsage(
            ref long count
            )
        {
            return (lambda != null) ?
                lambda.CountUsage(ref count) : false;
        }

        ///////////////////////////////////////////////////////////////////////

        public bool ProfileUsage(
            ref long microseconds
            )
        {
            return (lambda != null) ?
                lambda.ProfileUsage(ref microseconds) : false;
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region ILevels Members
        public int Levels
        {
            get { return (lambda != null) ? lambda.Levels : 0; }
        }

        ///////////////////////////////////////////////////////////////////////

        public int EnterLevel()
        {
            return (lambda != null) ? lambda.EnterLevel() : 0;
        }

        ///////////////////////////////////////////////////////////////////////

        public int ExitLevel()
        {
            return (lambda != null) ? lambda.ExitLevel() : 0;
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region IProcedureData Members
        public ProcedureFlags Flags
        {
            get { return (lambda != null) ? lambda.Flags : ProcedureFlags.None; }
            set { if (lambda != null) { lambda.Flags = value; } }
        }

        ///////////////////////////////////////////////////////////////////////

        public ArgumentList Arguments
        {
            get { return (lambda != null) ? lambda.Arguments : null; }
            set { if (lambda != null) { lambda.Arguments = value; } }
        }

        ///////////////////////////////////////////////////////////////////////

        public ArgumentDictionary NamedArguments
        {
            get { return (lambda != null) ? lambda.NamedArguments : null; }
            set { if (lambda != null) { lambda.NamedArguments = value; } }
        }

        ///////////////////////////////////////////////////////////////////////

        public string Body
        {
            get { return (lambda != null) ? lambda.Body : null; }
            set { if (lambda != null) { lambda.Body = value; } }
        }

        ///////////////////////////////////////////////////////////////////////

        public IScriptLocation Location
        {
            get { return (lambda != null) ? lambda.Location : null; }
            set { if (lambda != null) { lambda.Location = value; } }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region IDynamicExecuteCallback Members
        public ExecuteCallback Callback
        {
            get { return (lambda != null) ? lambda.Callback : null; }
            set { if (lambda != null) { lambda.Callback = value; } }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region IExecute Members
        public ReturnCode Execute(
            Interpreter interpreter,
            IClientData clientData,
            ArgumentList arguments,
            ref Result result
            )
        {
            if (lambda != null)
                return lambda.Execute(
                    interpreter, clientData, arguments, ref result);
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
            get { return lambda; }
        }
        #endregion
    }
}
