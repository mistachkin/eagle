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
    /// <summary>
    /// This class stores the metadata describing a sub-command, including its
    /// name, group, description, associated type and command, flags, and the
    /// token used to identify it.  It implements <see cref="ISubCommandData" />.
    /// </summary>
#if SERIALIZATION
    [Serializable()]
#endif
    [ObjectId("e0e51ae4-7ab7-4a27-925f-58cde429317a")]
    public class SubCommandData : ISubCommandData
    {
        /// <summary>
        /// Constructs a new instance of this class using the specified sub-command
        /// metadata.
        /// </summary>
        /// <param name="name">
        /// The name of this sub-command.  This parameter may be null.
        /// </param>
        /// <param name="group">
        /// The group of this sub-command.  This parameter may be null.
        /// </param>
        /// <param name="description">
        /// The description of this sub-command.  This parameter may be null.
        /// </param>
        /// <param name="clientData">
        /// The client data associated with this sub-command.  This parameter may
        /// be null.
        /// </param>
        /// <param name="typeName">
        /// The name of the type that implements this sub-command.  This parameter
        /// may be null.
        /// </param>
        /// <param name="type">
        /// The type that implements this sub-command.  This parameter may be null.
        /// </param>
        /// <param name="nameIndex">
        /// The index of the argument that contains the name of this sub-command.
        /// </param>
        /// <param name="commandFlags">
        /// The flags for the command associated with this sub-command.
        /// </param>
        /// <param name="subCommandFlags">
        /// The flags for this sub-command.
        /// </param>
        /// <param name="command">
        /// The command associated with this sub-command.  This parameter may be
        /// null.
        /// </param>
        /// <param name="token">
        /// The token used to identify this sub-command.
        /// </param>
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
        /// <summary>
        /// Stores the name of this sub-command.
        /// </summary>
        private string name;
        /// <summary>
        /// Gets or sets the name of this sub-command.
        /// </summary>
        public virtual string Name
        {
            get { return name; }
            set { name = value; }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region IIdentifierBase Members
        /// <summary>
        /// Stores the identifier kind of this sub-command.
        /// </summary>
        private IdentifierKind kind;
        /// <summary>
        /// Gets or sets the identifier kind of this sub-command.
        /// </summary>
        public virtual IdentifierKind Kind
        {
            get { return kind; }
            set { kind = value; }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Stores the globally unique identifier of this sub-command.
        /// </summary>
        private Guid id;
        /// <summary>
        /// Gets or sets the globally unique identifier of this sub-command.
        /// </summary>
        public virtual Guid Id
        {
            get { return id; }
            set { id = value; }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region IGetClientData / ISetClientData Members
        /// <summary>
        /// Stores the client data associated with this sub-command.
        /// </summary>
        private IClientData clientData;
        /// <summary>
        /// Gets or sets the client data associated with this sub-command.
        /// </summary>
        public virtual IClientData ClientData
        {
            get { return clientData; }
            set { clientData = value; }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region IIdentifier Members
        /// <summary>
        /// Stores the group of this sub-command.
        /// </summary>
        private string group;
        /// <summary>
        /// Gets or sets the group of this sub-command.
        /// </summary>
        public virtual string Group
        {
            get { return group; }
            set { group = value; }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Stores the description of this sub-command.
        /// </summary>
        private string description;
        /// <summary>
        /// Gets or sets the description of this sub-command.
        /// </summary>
        public virtual string Description
        {
            get { return description; }
            set { description = value; }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region ITypeAndName Members
        /// <summary>
        /// Stores the name of the type that implements this sub-command.
        /// </summary>
        private string typeName;
        /// <summary>
        /// Gets or sets the name of the type that implements this sub-command.
        /// </summary>
        public virtual string TypeName
        {
            get { return typeName; }
            set { typeName = value; }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Stores the type that implements this sub-command.
        /// </summary>
        private Type type;
        /// <summary>
        /// Gets or sets the type that implements this sub-command.
        /// </summary>
        public virtual Type Type
        {
            get { return type; }
            set { type = value; }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region ICommandBaseData Members
        /// <summary>
        /// Stores the flags for the command associated with this sub-command.
        /// </summary>
        private CommandFlags commandFlags;
        /// <summary>
        /// Gets or sets the flags for the command associated with this
        /// sub-command.
        /// </summary>
        public virtual CommandFlags CommandFlags
        {
            get { return commandFlags; }
            set { commandFlags = value; }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region IHaveCommand Members
        /// <summary>
        /// Stores the command associated with this sub-command.
        /// </summary>
        private ICommand command;
        /// <summary>
        /// Gets or sets the command associated with this sub-command.
        /// </summary>
        public virtual ICommand Command
        {
            get { return command; }
            set { command = value; }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region ISubCommandData Members
        /// <summary>
        /// Stores the index of the argument that contains the name of this
        /// sub-command.
        /// </summary>
        private int nameIndex;
        /// <summary>
        /// Gets or sets the index of the argument that contains the name of this
        /// sub-command.
        /// </summary>
        public virtual int NameIndex
        {
            get { return nameIndex; }
            set { nameIndex = value; }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Stores the flags for this sub-command.
        /// </summary>
        private SubCommandFlags subCommandFlags;
        /// <summary>
        /// Gets or sets the flags for this sub-command.
        /// </summary>
        public virtual SubCommandFlags Flags
        {
            get { return subCommandFlags; }
            set { subCommandFlags = value; }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region IWrapperData Members
        /// <summary>
        /// Stores the token used to identify this sub-command.
        /// </summary>
        private long token;
        /// <summary>
        /// Gets or sets the token used to identify this sub-command.
        /// </summary>
        public virtual long Token
        {
            get { return token; }
            set { token = value; }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region System.Object Overrides
        /// <summary>
        /// This method produces a string describing this sub-command using its
        /// name only.
        /// </summary>
        /// <returns>
        /// A string containing the name of this sub-command (or an empty string
        /// when it has no name).
        /// </returns>
        public override string ToString()
        {
            return (name != null) ? name : String.Empty;
        }
        #endregion
    }
}
