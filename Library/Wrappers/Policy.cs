/*
 * Policy.cs --
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
using Eagle._Containers.Public;
using Eagle._Interfaces.Public;

namespace Eagle._Wrappers
{
    [ObjectId("f23e646a-0766-4bd5-b770-4a307df76792")]
    internal sealed class Policy : Default, IPolicy
    {
        #region Public Constructors
        public Policy(
            long token,
            IPolicy policy
            )
            : base(token)
        {
            this.policy = policy;
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Private Data
        internal IPolicy policy;
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region IIdentifierName Members
        public string Name
        {
            get { return (policy != null) ? policy.Name : null; }
            set { if (policy != null) { policy.Name = value; } }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region IIdentifierBase Members
        public IdentifierKind Kind
        {
            get { return (policy != null) ? policy.Kind : IdentifierKind.None; }
            set { if (policy != null) { policy.Kind = value; } }
        }

        ///////////////////////////////////////////////////////////////////////

        public Guid Id
        {
            get { return (policy != null) ? policy.Id : Guid.Empty; }
            set { if (policy != null) { policy.Id = value; } }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region IIdentifier Members
        public string Group
        {
            get { return (policy != null) ? policy.Group : null; }
            set { if (policy != null) { policy.Group = value; } }
        }

        ///////////////////////////////////////////////////////////////////////

        public string Description
        {
            get { return (policy != null) ? policy.Description : null; }
            set { if (policy != null) { policy.Description = value; } }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region IGetClientData / ISetClientData Members
        public IClientData ClientData
        {
            get { return (policy != null) ? policy.ClientData : null; }
            set { if (policy != null) { policy.ClientData = value; } }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region IDynamicExecuteCallback Members
        public ExecuteCallback Callback
        {
            get { return (policy != null) ? policy.Callback : null; }
            set { if (policy != null) { policy.Callback = value; } }
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
            if (policy != null)
                return policy.Execute(
                    interpreter, clientData, arguments, ref result);
            else
                return ReturnCode.Error;
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region IHavePlugin Members
        public IPlugin Plugin
        {
            get { return (policy != null) ? policy.Plugin : null; }
            set { if (policy != null) { policy.Plugin = value; } }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region ITypeAndName Members
        public string TypeName
        {
            get { return (policy != null) ? policy.TypeName : null; }
            set { if (policy != null) { policy.TypeName = value; } }
        }

        ///////////////////////////////////////////////////////////////////////

        public Type Type
        {
            get { return (policy != null) ? policy.Type : null; }
            set { if (policy != null) { policy.Type = value; } }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region IPolicyData Members
        public string MethodName
        {
            get { return (policy != null) ? policy.MethodName : null; }
            set { if (policy != null) { policy.MethodName = value; } }
        }

        ///////////////////////////////////////////////////////////////////////

        public BindingFlags BindingFlags
        {
            get { return (policy != null) ? policy.BindingFlags : BindingFlags.Default; }
            set { if (policy != null) { policy.BindingFlags = value; } }
        }

        ///////////////////////////////////////////////////////////////////////

        public MethodFlags MethodFlags
        {
            get { return (policy != null) ? policy.MethodFlags : MethodFlags.None; }
            set { if (policy != null) { policy.MethodFlags = value; } }
        }

        ///////////////////////////////////////////////////////////////////////

        public PolicyFlags PolicyFlags
        {
            get { return (policy != null) ? policy.PolicyFlags : PolicyFlags.None; }
            set { if (policy != null) { policy.PolicyFlags = value; } }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region ISetup Members
        public ReturnCode Setup(
            ref Result error
            )
        {
            if (policy != null)
                return policy.Setup(ref error);
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
            get { return policy; }
        }
        #endregion
    }
}
