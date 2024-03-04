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
    [ObjectId("bd3bc9f3-5ee1-4d82-b877-68350a5b84c3")]
    public class TraceData : ITraceData
    {
        #region Public Constructors
        public TraceData(
            string name,
            string group,
            string description,
            IClientData clientData,
            string typeName,
            Type type,
            string methodName,
            BindingFlags bindingFlags,
            MethodFlags methodFlags,
            TraceFlags traceFlags,
            IPlugin plugin,
            long token
            )
        {
            this.kind = IdentifierKind.TraceData;
            this.id = AttributeOps.GetObjectId(this);
            this.name = name;
            this.group = group;
            this.description = description;
            this.clientData = clientData;
            this.typeName = typeName;
            this.type = type;
            this.methodName = methodName;
            this.bindingFlags = bindingFlags;
            this.methodFlags = methodFlags;
            this.traceFlags = traceFlags;
            this.plugin = plugin;
            this.token = token;
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region IIdentifierName Members
        private string name;
        public virtual string Name
        {
            get { return name; }
            set { name = value; }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region IIdentifierBase Members
        private IdentifierKind kind;
        public virtual IdentifierKind Kind
        {
            get { return kind; }
            set { kind = value; }
        }

        ///////////////////////////////////////////////////////////////////////

        private Guid id;
        public virtual Guid Id
        {
            get { return id; }
            set { id = value; }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region IGetClientData / ISetClientData Members
        private IClientData clientData;
        public virtual IClientData ClientData
        {
            get { return clientData; }
            set { clientData = value; }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region IIdentifier Members
        private string group;
        public virtual string Group
        {
            get { return group; }
            set { group = value; }
        }

        ///////////////////////////////////////////////////////////////////////

        private string description;
        public virtual string Description
        {
            get { return description; }
            set { description = value; }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region IHavePlugin Members
        private IPlugin plugin;
        public virtual IPlugin Plugin
        {
            get { return plugin; }
            set { plugin = value; }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region ITypeAndName Members
        private string typeName;
        public virtual string TypeName
        {
            get { return typeName; }
            set { typeName = value; }
        }

        ///////////////////////////////////////////////////////////////////////

        private Type type;
        public virtual Type Type
        {
            get { return type; }
            set { type = value; }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region ITraceData Members
        private string methodName;
        public virtual string MethodName
        {
            get { return methodName; }
            set { methodName = value; }
        }

        ///////////////////////////////////////////////////////////////////////

        private BindingFlags bindingFlags;
        public virtual BindingFlags BindingFlags
        {
            get { return bindingFlags; }
            set { bindingFlags = value; }
        }

        ///////////////////////////////////////////////////////////////////////

        private MethodFlags methodFlags;
        public virtual MethodFlags MethodFlags
        {
            get { return methodFlags; }
            set { methodFlags = value; }
        }

        ///////////////////////////////////////////////////////////////////////

        private TraceFlags traceFlags;
        public virtual TraceFlags TraceFlags
        {
            get { return traceFlags; }
            set { traceFlags = value; }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

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
