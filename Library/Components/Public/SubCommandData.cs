/*
 * SubCommandData.cs --
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
    [ObjectId("e0e51ae4-7ab7-4a27-925f-58cde429317a")]
    public class SubCommandData : ISubCommandData
    {
        public SubCommandData(
            string name,
            string group,
            string description,
            IClientData clientData,
            string typeName,
            Type type,
            int nameIndex,
            CommandFlags commandFlags,
            SubCommandFlags subCommandFlags,
            ICommand command,
            long token
            )
        {
            this.kind = IdentifierKind.SubCommandData;
            this.id = AttributeOps.GetObjectId(this);
            this.name = name;
            this.group = group;
            this.description = description;
            this.clientData = clientData;
            this.typeName = typeName;
            this.type = type;
            this.nameIndex = nameIndex;
            this.commandFlags = commandFlags;
            this.subCommandFlags = subCommandFlags;
            this.command = command;
            this.token = token;
        }

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
        private CommandFlags commandFlags;
        public virtual CommandFlags CommandFlags
        {
            get { return commandFlags; }
            set { commandFlags = value; }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region IHaveCommand Members
        private ICommand command;
        public virtual ICommand Command
        {
            get { return command; }
            set { command = value; }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region ISubCommandData Members
        private int nameIndex;
        public virtual int NameIndex
        {
            get { return nameIndex; }
            set { nameIndex = value; }
        }

        ///////////////////////////////////////////////////////////////////////

        private SubCommandFlags subCommandFlags;
        public virtual SubCommandFlags Flags
        {
            get { return subCommandFlags; }
            set { subCommandFlags = value; }
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
