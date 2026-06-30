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
    /// <summary>
    /// This class holds the metadata describing a mathematical function exposed
    /// to an interpreter, including its identity, the managed type that
    /// implements it, the number and types of its arguments, its flags, and the
    /// token used to identify it.
    /// </summary>
#if SERIALIZATION
    [Serializable()]
#endif
    [ObjectId("330dfb6a-9286-4d88-a321-c975aa327bef")]
    public class FunctionData : IFunctionData, IWrapperData
    {
        #region Public Constructors
        /// <summary>
        /// Constructs a function data instance from the specified identity,
        /// type, argument, flag, plugin, and token parameters.  A fresh object
        /// identifier is generated for the new instance.
        /// </summary>
        /// <param name="name">
        /// The name of this function.
        /// </param>
        /// <param name="group">
        /// The group of this function, if any.  This parameter may be null.
        /// </param>
        /// <param name="description">
        /// The description of this function, if any.  This parameter may be
        /// null.
        /// </param>
        /// <param name="clientData">
        /// The client data to associate with this function, if any.  This
        /// parameter may be null.
        /// </param>
        /// <param name="typeName">
        /// The name of the managed type that implements this function.
        /// </param>
        /// <param name="type">
        /// The managed type that implements this function.
        /// </param>
        /// <param name="arguments">
        /// The number of arguments accepted by this function.
        /// </param>
        /// <param name="types">
        /// The list of argument types accepted by this function, if any.  This
        /// parameter may be null.
        /// </param>
        /// <param name="flags">
        /// The flags controlling the behavior of this function.
        /// </param>
        /// <param name="plugin">
        /// The plugin that provides this function, if any.  This parameter may
        /// be null.
        /// </param>
        /// <param name="token">
        /// The token used to identify this function within the interpreter.
        /// </param>
        public FunctionData(
            string name,
            string group,
            string description,
            IClientData clientData,
            string typeName,
            Type type,
            int arguments,
            TypeList types,
            FunctionFlags flags,
            IPlugin plugin,
            long token
            )
            : this(Guid.Empty, name, group, description,
                   clientData, typeName, type, arguments,
                   types, flags, plugin, token)
        {
            this.id = AttributeOps.GetObjectId(this);
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Private Constructors
        /// <summary>
        /// Constructs a function data instance from the specified identity,
        /// type, argument, flag, plugin, and token parameters.  This is the
        /// most general constructor; the public constructor delegates to it.
        /// </summary>
        /// <param name="id">
        /// The globally unique identifier of this function.
        /// </param>
        /// <param name="name">
        /// The name of this function.
        /// </param>
        /// <param name="group">
        /// The group of this function, if any.  This parameter may be null.
        /// </param>
        /// <param name="description">
        /// The description of this function, if any.  This parameter may be
        /// null.
        /// </param>
        /// <param name="clientData">
        /// The client data to associate with this function, if any.  This
        /// parameter may be null.
        /// </param>
        /// <param name="typeName">
        /// The name of the managed type that implements this function.
        /// </param>
        /// <param name="type">
        /// The managed type that implements this function.
        /// </param>
        /// <param name="arguments">
        /// The number of arguments accepted by this function.
        /// </param>
        /// <param name="types">
        /// The list of argument types accepted by this function, if any.  This
        /// parameter may be null.
        /// </param>
        /// <param name="flags">
        /// The flags controlling the behavior of this function.
        /// </param>
        /// <param name="plugin">
        /// The plugin that provides this function, if any.  This parameter may
        /// be null.
        /// </param>
        /// <param name="token">
        /// The token used to identify this function within the interpreter.
        /// </param>
        internal FunctionData(
            Guid id,
            string name,
            string group,
            string description,
            IClientData clientData,
            string typeName,
            Type type,
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
            this.type = type;
            this.arguments = arguments;
            this.types = types;
            this.flags = flags;
            this.plugin = plugin;
            this.token = token;
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region IIdentifierName Members
        /// <summary>
        /// Stores the name of this function data.
        /// </summary>
        private string name;
        /// <summary>
        /// Gets or sets the name of this function data.
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
        /// Stores the identifier kind of this function data.
        /// </summary>
        private IdentifierKind kind;
        /// <summary>
        /// Gets or sets the identifier kind of this function data.
        /// </summary>
        public virtual IdentifierKind Kind
        {
            get { return kind; }
            set { kind = value; }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Stores the globally unique identifier of this function data.
        /// </summary>
        private Guid id;
        /// <summary>
        /// Gets or sets the globally unique identifier of this function data.
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
        /// Stores the client data associated with this function data.
        /// </summary>
        private IClientData clientData;
        /// <summary>
        /// Gets or sets the client data associated with this function data.
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
        /// Stores the group of this function data.
        /// </summary>
        private string group;
        /// <summary>
        /// Gets or sets the group of this function data.
        /// </summary>
        public virtual string Group
        {
            get { return group; }
            set { group = value; }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Stores the description of this function data.
        /// </summary>
        private string description;
        /// <summary>
        /// Gets or sets the description of this function data.
        /// </summary>
        public virtual string Description
        {
            get { return description; }
            set { description = value; }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region IHavePlugin Members
        /// <summary>
        /// Stores the plugin that provides this function data.
        /// </summary>
        private IPlugin plugin;
        /// <summary>
        /// Gets or sets the plugin that provides this function data.
        /// </summary>
        public virtual IPlugin Plugin
        {
            get { return plugin; }
            set { plugin = value; }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region ITypeAndName Members
        /// <summary>
        /// Stores the name of the managed type that implements this function.
        /// </summary>
        private string typeName;
        /// <summary>
        /// Gets or sets the name of the managed type that implements this
        /// function.
        /// </summary>
        public virtual string TypeName
        {
            get { return typeName; }
            set { typeName = value; }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Stores the managed type that implements this function.
        /// </summary>
        private Type type;
        /// <summary>
        /// Gets or sets the managed type that implements this function.
        /// </summary>
        public virtual Type Type
        {
            get { return type; }
            set { type = value; }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region IFunctionData Members
        /// <summary>
        /// Stores the number of arguments accepted by this function.
        /// </summary>
        private int arguments;
        /// <summary>
        /// Gets or sets the number of arguments accepted by this function.
        /// </summary>
        public virtual int Arguments
        {
            get { return arguments; }
            set { arguments = value; }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Stores the list of argument types accepted by this function.
        /// </summary>
        private TypeList types;
        /// <summary>
        /// Gets or sets the list of argument types accepted by this function.
        /// </summary>
        public virtual TypeList Types
        {
            get { return types; }
            set { types = value; }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Stores the flags controlling the behavior of this function.
        /// </summary>
        private FunctionFlags flags;
        /// <summary>
        /// Gets or sets the flags controlling the behavior of this function.
        /// </summary>
        public virtual FunctionFlags Flags
        {
            get { return flags; }
            set { flags = value; }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region IWrapperData Members
        /// <summary>
        /// Stores the token used to identify this function data within the
        /// interpreter.
        /// </summary>
        private long token;
        /// <summary>
        /// Gets or sets the token used to identify this function data within
        /// the interpreter.
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
        /// This method produces a string representation of this function data
        /// using its name only.
        /// </summary>
        /// <returns>
        /// The name of this function data, or an empty string when it has no
        /// name.
        /// </returns>
        public override string ToString()
        {
            return (name != null) ? name : String.Empty;
        }
        #endregion
    }
}
