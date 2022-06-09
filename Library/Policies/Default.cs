/*
 * Default.cs --
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
using Eagle._Components.Private;
using Eagle._Components.Public;
using Eagle._Components.Public.Delegates;
using Eagle._Containers.Public;
using Eagle._Interfaces.Public;

namespace Eagle._Policies
{
    [ObjectId("45dd5294-ff31-47a2-b450-61d34c4184fb")]
    public class Default :
#if ISOLATED_INTERPRETERS || ISOLATED_PLUGINS
        ScriptMarshalByRefObject,
#endif
        IPolicy
    {
        #region Public Constructors
        public Default(
            IPolicyData policyData
            )
        {
            kind = IdentifierKind.Policy;

            //
            // VIRTUAL: Id of the deepest derived class.
            //
            id = AttributeOps.GetObjectId(this);

            //
            // VIRTUAL: Group of the deepest derived class.
            //
            group = AttributeOps.GetObjectGroups(this);

            //
            // NOTE: Is the supplied policy data valid?
            //
            if (policyData != null)
            {
                EntityOps.MaybeSetGroup(
                    this, policyData.Group);

                name = policyData.Name;
                description = policyData.Description;
                typeName = policyData.TypeName;
                methodName = policyData.MethodName;
                bindingFlags = policyData.BindingFlags;
                methodFlags = policyData.MethodFlags;
                policyFlags = policyData.PolicyFlags;
                token = policyData.Token;
                plugin = policyData.Plugin;
                clientData = policyData.ClientData;
            }

            callback = null;
        }
        #endregion

        ////////////////////////////////////////////////////////////////////////

        #region System.Object Overrides
        public override string ToString()
        {
            return (name != null) ?
                StringList.MakeList(FormatOps.RawTypeName(GetType()), name) :
                base.ToString();
        }
        #endregion

        ////////////////////////////////////////////////////////////////////////

        #region IIdentifierName Members
        private string name;
        public virtual string Name
        {
            get { return name; }
            set { name = value; }
        }
        #endregion

        ////////////////////////////////////////////////////////////////////////

        #region IIdentifierBase Members
        private IdentifierKind kind;
        public virtual IdentifierKind Kind
        {
            get { return kind; }
            set { kind = value; }
        }

        ////////////////////////////////////////////////////////////////////////

        private Guid id;
        public virtual Guid Id
        {
            get { return id; }
            set { id = value; }
        }
        #endregion

        ////////////////////////////////////////////////////////////////////////

        #region IIdentifier Members
        private string group;
        public virtual string Group
        {
            get { return group; }
            set { group = value; }
        }

        ////////////////////////////////////////////////////////////////////////

        private string description;
        public virtual string Description
        {
            get { return description; }
            set { description = value; }
        }
        #endregion

        ////////////////////////////////////////////////////////////////////////

        #region IGetClientData / ISetClientData Members
        private IClientData clientData;
        public virtual IClientData ClientData
        {
            get { return clientData; }
            set { clientData = value; }
        }
        #endregion

        ////////////////////////////////////////////////////////////////////////

        #region IHavePlugin Members
        private IPlugin plugin;
        public virtual IPlugin Plugin
        {
            get { return plugin; }
            set { plugin = value; }
        }
        #endregion

        ////////////////////////////////////////////////////////////////////////

        #region IPolicyData Members
        private string typeName;
        public virtual string TypeName
        {
            get { return typeName; }
            set { typeName = value; }
        }

        ////////////////////////////////////////////////////////////////////////

        private string methodName;
        public virtual string MethodName
        {
            get { return methodName; }
            set { methodName = value; }
        }

        ////////////////////////////////////////////////////////////////////////

        private BindingFlags bindingFlags;
        public virtual BindingFlags BindingFlags
        {
            get { return bindingFlags; }
            set { bindingFlags = value; }
        }

        ////////////////////////////////////////////////////////////////////////

        private MethodFlags methodFlags;
        public virtual MethodFlags MethodFlags
        {
            get { return methodFlags; }
            set { methodFlags = value; }
        }

        ////////////////////////////////////////////////////////////////////////

        private PolicyFlags policyFlags;
        public virtual PolicyFlags PolicyFlags
        {
            get { return policyFlags; }
            set { policyFlags = value; }
        }
        #endregion

        ////////////////////////////////////////////////////////////////////////

        #region IWrapperData Members
        private long token;
        public virtual long Token
        {
            get { return token; }
            set { token = value; }
        }
        #endregion

        ////////////////////////////////////////////////////////////////////////

        #region IDynamicExecuteCallback Members
        private ExecuteCallback callback;
        public virtual ExecuteCallback Callback
        {
            get { return callback; }
            set { callback = value; }
        }
        #endregion

        ////////////////////////////////////////////////////////////////////////

        #region IExecute Members
        public virtual ReturnCode Execute(
            Interpreter interpreter,
            IClientData clientData,
            ArgumentList arguments,
            ref Result result
            )
        {
            return ReturnCode.Ok;
        }
        #endregion

        ////////////////////////////////////////////////////////////////////////

        #region ISetup Members
        public virtual ReturnCode Setup(
            ref Result error
            )
        {
            return ReturnCode.Ok;
        }
        #endregion
    }
}
