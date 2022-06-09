/*
 * PolicyData.cs --
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
using Eagle._Interfaces.Public;

namespace Eagle._Components.Public
{
#if SERIALIZATION
    [Serializable()]
#endif
    [ObjectId("1f22a392-8ec7-4000-86b4-dc34a769b796")]
    public class PolicyData : IPolicyData
    {
        #region Public Constructors
        public PolicyData(
            string name,
            string group,
            string description,
            IClientData clientData,
            string typeName,
            string methodName,
            BindingFlags bindingFlags,
            MethodFlags methodFlags,
            PolicyFlags policyFlags,
            IPlugin plugin,
            long token
            )
        {
            this.kind = IdentifierKind.PolicyData;
            this.id = AttributeOps.GetObjectId(this);
            this.name = name;
            this.group = group;
            this.description = description;
            this.clientData = clientData;
            this.typeName = typeName;
            this.methodName = methodName;
            this.bindingFlags = bindingFlags;
            this.methodFlags = methodFlags;
            this.policyFlags = policyFlags;
            this.plugin = plugin;
            this.token = token;
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////////////////////////////

        #region IIdentifierName Members
        private string name;
        public virtual string Name
        {
            get { return name; }
            set { name = value; }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////////////////////////////

        #region IIdentifierBase Members
        private IdentifierKind kind;
        public virtual IdentifierKind Kind
        {
            get { return kind; }
            set { kind = value; }
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        private Guid id;
        public virtual Guid Id
        {
            get { return id; }
            set { id = value; }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////////////////////////////

        #region IIdentifier Members
        private string group;
        public virtual string Group
        {
            get { return group; }
            set { group = value; }
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        private string description;
        public virtual string Description
        {
            get { return description; }
            set { description = value; }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////////////////////////////

        #region IGetClientData / ISetClientData Members
        private IClientData clientData;
        public virtual IClientData ClientData
        {
            get { return clientData; }
            set { clientData = value; }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////////////////////////////

        #region IHavePlugin Members
        private IPlugin plugin;
        public virtual IPlugin Plugin
        {
            get { return plugin; }
            set { plugin = value; }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////////////////////////////

        #region IPolicyData Members
        private string typeName;
        public virtual string TypeName
        {
            get { return typeName; }
            set { typeName = value; }
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        private string methodName;
        public virtual string MethodName
        {
            get { return methodName; }
            set { methodName = value; }
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        private BindingFlags bindingFlags;
        public virtual BindingFlags BindingFlags
        {
            get { return bindingFlags; }
            set { bindingFlags = value; }
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        private MethodFlags methodFlags;
        public virtual MethodFlags MethodFlags
        {
            get { return methodFlags; }
            set { methodFlags = value; }
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        private PolicyFlags policyFlags;
        public virtual PolicyFlags PolicyFlags
        {
            get { return policyFlags; }
            set { policyFlags = value; }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////////////////////////////

        #region IWrapperData Members
        private long token;
        public virtual long Token
        {
            get { return token; }
            set { token = value; }
        }
        #endregion
    }
}
