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
    /// <summary>
    /// This class provides a concrete, mutable container for the metadata that
    /// describes an Eagle command, including its name, group, description,
    /// client data, type name and type, flags, owning plugin, and token.  It
    /// implements <see cref="ICommandData" /> and is typically used to carry
    /// command metadata when registering or constructing commands.
    /// </summary>
#if SERIALIZATION
    [Serializable()]
#endif
    [ObjectId("242630be-5333-42d7-95e2-15145bba9a65")]
    public class CommandData : ICommandData
    {
        #region Public Constructors
        /// <summary>
        /// Constructs an instance of this class using the specified command
        /// metadata.  The object identifier is obtained from the
        /// <see cref="ObjectIdAttribute" /> applied to this instance.
        /// </summary>
        /// <param name="name">
        /// The name of the command.  This parameter may be null.
        /// </param>
        /// <param name="group">
        /// The group of the command.  This parameter may be null.
        /// </param>
        /// <param name="description">
        /// The description of the command.  This parameter may be null.
        /// </param>
        /// <param name="clientData">
        /// The client data associated with the command.  This parameter may be
        /// null.
        /// </param>
        /// <param name="typeName">
        /// The type name of the command.  This parameter may be null.
        /// </param>
        /// <param name="flags">
        /// The flags associated with the command.
        /// </param>
        /// <param name="plugin">
        /// The plugin that owns the command.  This parameter may be null.
        /// </param>
        /// <param name="token">
        /// The token associated with the command.
        /// </param>
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

        /// <summary>
        /// Constructs an instance of this class, copying the command metadata
        /// from the specified command data.
        /// </summary>
        /// <param name="commandData">
        /// The command data to copy the metadata from.  If this parameter is
        /// null, the new instance is left in its default state.
        /// </param>
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
        /// <summary>
        /// Constructs an instance of this class using the fully specified set
        /// of command metadata.  This is the most general constructor; the
        /// public constructor overloads delegate to it.
        /// </summary>
        /// <param name="id">
        /// The globally unique identifier of the command.
        /// </param>
        /// <param name="name">
        /// The name of the command.  This parameter may be null.
        /// </param>
        /// <param name="group">
        /// The group of the command.  This parameter may be null.
        /// </param>
        /// <param name="description">
        /// The description of the command.  This parameter may be null.
        /// </param>
        /// <param name="clientData">
        /// The client data associated with the command.  This parameter may be
        /// null.
        /// </param>
        /// <param name="typeName">
        /// The type name of the command.  This parameter may be null.
        /// </param>
        /// <param name="type">
        /// The type of the command.  This parameter may be null.
        /// </param>
        /// <param name="flags">
        /// The flags associated with the command.
        /// </param>
        /// <param name="plugin">
        /// The plugin that owns the command.  This parameter may be null.
        /// </param>
        /// <param name="token">
        /// The token associated with the command.
        /// </param>
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
        /// <summary>
        /// Stores the name of the command.
        /// </summary>
        private string name;
        /// <summary>
        /// Gets or sets the name of the command.
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
        /// Stores the identifier kind of the command.
        /// </summary>
        private IdentifierKind kind;
        /// <summary>
        /// Gets or sets the identifier kind of the command.
        /// </summary>
        public virtual IdentifierKind Kind
        {
            get { return kind; }
            set { kind = value; }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Stores the globally unique identifier of the command.
        /// </summary>
        private Guid id;
        /// <summary>
        /// Gets or sets the globally unique identifier of the command.
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
        /// Stores the client data associated with the command.
        /// </summary>
        private IClientData clientData;
        /// <summary>
        /// Gets or sets the client data associated with the command.
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
        /// Stores the group of the command.
        /// </summary>
        private string group;
        /// <summary>
        /// Gets or sets the group of the command.
        /// </summary>
        public virtual string Group
        {
            get { return group; }
            set { group = value; }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Stores the description of the command.
        /// </summary>
        private string description;
        /// <summary>
        /// Gets or sets the description of the command.
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
        /// Stores the type name of the command.
        /// </summary>
        private string typeName;
        /// <summary>
        /// Gets or sets the type name of the command.
        /// </summary>
        public virtual string TypeName
        {
            get { return typeName; }
            set { typeName = value; }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Stores the type of the command.
        /// </summary>
        private Type type;
        /// <summary>
        /// Gets or sets the type of the command.
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
        /// Gets or sets the flags associated with the command.
        /// </summary>
        public virtual CommandFlags CommandFlags
        {
            get { return flags; }
            set { flags = value; }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region IHavePlugin Members
        /// <summary>
        /// Stores the plugin that owns the command.
        /// </summary>
        private IPlugin plugin;
        /// <summary>
        /// Gets or sets the plugin that owns the command.
        /// </summary>
        public virtual IPlugin Plugin
        {
            get { return plugin; }
            set { plugin = value; }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region ICommandData Members
        /// <summary>
        /// Stores the flags associated with the command.
        /// </summary>
        private CommandFlags flags;
        /// <summary>
        /// Gets or sets the flags associated with the command.
        /// </summary>
        public virtual CommandFlags Flags
        {
            get { return flags; }
            set { flags = value; }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region IWrapperData Members
        /// <summary>
        /// Stores the token associated with the command.
        /// </summary>
        private long token;
        /// <summary>
        /// Gets or sets the token associated with the command.
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
        /// This method returns a string representation of the command.
        /// </summary>
        /// <returns>
        /// The name of the command, or an empty string if the name is null.
        /// </returns>
        public override string ToString()
        {
            return (name != null) ? name : String.Empty;
        }
        #endregion
    }
}
