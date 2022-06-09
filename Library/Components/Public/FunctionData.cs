/*
 * FunctionData.cs --
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
using Eagle._Components.Private;
using Eagle._Containers.Public;
using Eagle._Interfaces.Public;

namespace Eagle._Components.Public
{
#if SERIALIZATION
    [Serializable()]
#endif
    [ObjectId("330dfb6a-9286-4d88-a321-c975aa327bef")]
    public class FunctionData : IFunctionData, IWrapperData
    {
        #region Public Constructors
        public FunctionData(
            string name,
            string group,
            string description,
            IClientData clientData,
            string typeName,
            int arguments,
            TypeList types,
            FunctionFlags flags,
            IPlugin plugin,
            long token
            )
            : this(Guid.Empty, name, group, description,
                   clientData, typeName, arguments, types,
                   flags, plugin, token)
        {
            this.id = AttributeOps.GetObjectId(this);
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Private Constructors
        internal FunctionData(
            Guid id,
            string name,
            string group,
            string description,
            IClientData clientData,
            string typeName,
            int arguments,
            TypeList types,
            FunctionFlags flags,
            IPlugin plugin,
            long token
            )
        {
            this.kind = IdentifierKind.FunctionData;
            this.id = id;
            this.name = name;
            this.group = group;
            this.description = description;
            this.clientData = clientData;
            this.typeName = typeName;
            this.arguments = arguments;
            this.types = types;
            this.flags = flags;
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

        #region IGetClientData / ISetClientData Members
        private IClientData clientData;
        public virtual IClientData ClientData
        {
            get { return clientData; }
            set { clientData = value; }
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

        #region IFunctionData Members
        private string typeName;
        public virtual string TypeName
        {
            get { return typeName; }
            set { typeName = value; }
        }

        ///////////////////////////////////////////////////////////////////////

        private int arguments;
        public virtual int Arguments
        {
            get { return arguments; }
            set { arguments = value; }
        }

        ///////////////////////////////////////////////////////////////////////

        private TypeList types;
        public virtual TypeList Types
        {
            get { return types; }
            set { types = value; }
        }

        ///////////////////////////////////////////////////////////////////////

        private FunctionFlags flags;
        public virtual FunctionFlags Flags
        {
            get { return flags; }
            set { flags = value; }
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

        ///////////////////////////////////////////////////////////////////////

        #region System.Object Overrides
        public override string ToString()
        {
            return (name != null) ? name : String.Empty;
        }
        #endregion
    }
}
