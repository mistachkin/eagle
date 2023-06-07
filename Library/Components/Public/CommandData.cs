/*
 * CommandData.cs --
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
using Eagle._Interfaces.Public;

namespace Eagle._Components.Public
{
#if SERIALIZATION
    [Serializable()]
#endif
    [ObjectId("242630be-5333-42d7-95e2-15145bba9a65")]
    public class CommandData : ICommandData
    {
        #region Public Constructors
        public CommandData(
            string name,
            string group,
            string description,
            IClientData clientData,
            string typeName,
            CommandFlags flags,
            IPlugin plugin,
            long token
            )
            : this(Guid.Empty, name, group, description,
                   clientData, typeName, null, flags,
                   plugin, token)
        {
            this.id = AttributeOps.GetObjectId(this);
        }

        ///////////////////////////////////////////////////////////////////////

        public CommandData(
            ICommandData commandData
            )
            : this(Guid.Empty, null, null, null,
                   null, null, null, CommandFlags.None,
                   null, 0)
        {
            if (commandData != null)
            {
                this.kind = commandData.Kind;
                this.id = commandData.Id;
                this.name = commandData.Name;
                this.group = commandData.Group;
                this.description = commandData.Description;
                this.clientData = commandData.ClientData;
                this.typeName = commandData.TypeName;
                this.type = commandData.Type;
                this.flags = commandData.Flags;
                this.plugin = commandData.Plugin;
                this.token = commandData.Token;
            }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Private Constructors
        internal CommandData(
            Guid id,
            string name,
            string group,
            string description,
            IClientData clientData,
            string typeName,
            Type type,
            CommandFlags flags,
            IPlugin plugin,
            long token
            )
        {
            this.kind = IdentifierKind.CommandData;
            this.id = id;
            this.name = name;
            this.group = group;
            this.description = description;
            this.clientData = clientData;
            this.typeName = typeName;
            this.type = type;
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

        #region ICommandBaseData Members
        public virtual CommandFlags CommandFlags
        {
            get { return flags; }
            set { flags = value; }
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

        #region ICommandData Members
        private CommandFlags flags;
        public virtual CommandFlags Flags
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
