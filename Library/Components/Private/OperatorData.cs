/*
 * OperatorData.cs --
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
using Eagle._Components.Public;
using Eagle._Containers.Public;
using Eagle._Interfaces.Private;
using Eagle._Interfaces.Public;

namespace Eagle._Components.Private
{
    /// <summary>
    /// This class stores the metadata that describes a single expression
    /// operator, including its name, group, description, client data, backing
    /// type, lexeme, operand count, supported operand types, flags, string
    /// comparison type, owner plugin, and token.  It implements
    /// <see cref="IOperatorData" /> and is used to identify and configure an
    /// operator within an Eagle interpreter.
    /// </summary>
#if SERIALIZATION
    [Serializable()]
#endif
    [ObjectId("cd6330c8-889b-4a60-a16a-fda58ddc7fb8")]
    internal class OperatorData : IOperatorData
    {
        #region Public Constructors
        /// <summary>
        /// Constructs an instance of this class using an automatically assigned
        /// object identifier, delegating to the more general constructor and
        /// then resolving the object identifier from this type's attributes.
        /// </summary>
        /// <param name="name">
        /// The name of the operator.
        /// </param>
        /// <param name="group">
        /// The group that the operator belongs to.
        /// </param>
        /// <param name="description">
        /// The human-readable description of the operator.
        /// </param>
        /// <param name="clientData">
        /// The client data associated with the operator.  This parameter may be
        /// null.
        /// </param>
        /// <param name="typeName">
        /// The name of the type that implements the operator.
        /// </param>
        /// <param name="type">
        /// The type that implements the operator.
        /// </param>
        /// <param name="lexeme">
        /// The lexeme used by the expression parser to recognize the operator.
        /// </param>
        /// <param name="operands">
        /// The number of operands that the operator requires.
        /// </param>
        /// <param name="types">
        /// The list of operand types supported by the operator.  This parameter
        /// may be null.
        /// </param>
        /// <param name="flags">
        /// The flags controlling the behavior of the operator.
        /// </param>
        /// <param name="comparisonType">
        /// The string comparison type used by the operator, when applicable.
        /// </param>
        /// <param name="plugin">
        /// The plugin that owns the operator.  This parameter may be null.
        /// </param>
        /// <param name="token">
        /// The token associated with the operator.
        /// </param>
        public OperatorData(
            string name,
            string group,
            string description,
            IClientData clientData,
            string typeName,
            Type type,
            Lexeme lexeme,
            int operands,
            TypeList types,
            OperatorFlags flags,
            StringComparison comparisonType,
            IPlugin plugin,
            long token
            )
            : this(Guid.Empty, name, group, description,
                   clientData, typeName, type, lexeme,
                   operands, types, flags, comparisonType,
                   plugin, token)
        {
            this.id = AttributeOps.GetObjectId(this);
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Private Constructors
        /// <summary>
        /// Constructs an instance of this class using the fully specified set of
        /// identity, type, and configuration parameters.  This is the most
        /// general constructor; the other constructor delegates to it.
        /// </summary>
        /// <param name="id">
        /// The unique object identifier to assign to the operator.
        /// </param>
        /// <param name="name">
        /// The name of the operator.
        /// </param>
        /// <param name="group">
        /// The group that the operator belongs to.
        /// </param>
        /// <param name="description">
        /// The human-readable description of the operator.
        /// </param>
        /// <param name="clientData">
        /// The client data associated with the operator.  This parameter may be
        /// null.
        /// </param>
        /// <param name="typeName">
        /// The name of the type that implements the operator.
        /// </param>
        /// <param name="type">
        /// The type that implements the operator.
        /// </param>
        /// <param name="lexeme">
        /// The lexeme used by the expression parser to recognize the operator.
        /// </param>
        /// <param name="operands">
        /// The number of operands that the operator requires.
        /// </param>
        /// <param name="types">
        /// The list of operand types supported by the operator.  This parameter
        /// may be null.
        /// </param>
        /// <param name="flags">
        /// The flags controlling the behavior of the operator.
        /// </param>
        /// <param name="comparisonType">
        /// The string comparison type used by the operator, when applicable.
        /// </param>
        /// <param name="plugin">
        /// The plugin that owns the operator.  This parameter may be null.
        /// </param>
        /// <param name="token">
        /// The token associated with the operator.
        /// </param>
        internal OperatorData(
            Guid id,
            string name,
            string group,
            string description,
            IClientData clientData,
            string typeName,
            Type type,
            Lexeme lexeme,
            int operands,
            TypeList types,
            OperatorFlags flags,
            StringComparison comparisonType,
            IPlugin plugin,
            long token
            )
        {
            this.kind = IdentifierKind.OperatorData;
            this.id = id;
            this.name = name;
            this.group = group;
            this.description = description;
            this.clientData = clientData;
            this.typeName = typeName;
            this.type = type;
            this.lexeme = lexeme;
            this.operands = operands;
            this.types = types;
            this.flags = flags;
            this.comparisonType = comparisonType;
            this.plugin = plugin;
            this.token = token;
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region IIdentifierName Members
        /// <summary>
        /// The name of the operator.
        /// </summary>
        private string name;
        /// <summary>
        /// Gets or sets the name of the operator.
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
        /// The kind of identifier represented by this object.
        /// </summary>
        private IdentifierKind kind;
        /// <summary>
        /// Gets or sets the kind of identifier represented by this object.
        /// </summary>
        public virtual IdentifierKind Kind
        {
            get { return kind; }
            set { kind = value; }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// The unique object identifier for the operator.
        /// </summary>
        private Guid id;
        /// <summary>
        /// Gets or sets the unique object identifier for the operator.
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
        /// The client data associated with the operator.
        /// </summary>
        private IClientData clientData;
        /// <summary>
        /// Gets or sets the client data associated with the operator.
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
        /// The group that the operator belongs to.
        /// </summary>
        private string group;
        /// <summary>
        /// Gets or sets the group that the operator belongs to.
        /// </summary>
        public virtual string Group
        {
            get { return group; }
            set { group = value; }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// The human-readable description of the operator.
        /// </summary>
        private string description;
        /// <summary>
        /// Gets or sets the human-readable description of the operator.
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
        /// The plugin that owns the operator.
        /// </summary>
        private IPlugin plugin;
        /// <summary>
        /// Gets or sets the plugin that owns the operator.
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
        /// The name of the type that implements the operator.
        /// </summary>
        private string typeName;
        /// <summary>
        /// Gets or sets the name of the type that implements the operator.
        /// </summary>
        public virtual string TypeName
        {
            get { return typeName; }
            set { typeName = value; }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// The type that implements the operator.
        /// </summary>
        private Type type;
        /// <summary>
        /// Gets or sets the type that implements the operator.
        /// </summary>
        public virtual Type Type
        {
            get { return type; }
            set { type = value; }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region IOperatorData Members
        /// <summary>
        /// The lexeme used by the expression parser to recognize the operator.
        /// </summary>
        private Lexeme lexeme;
        /// <summary>
        /// Gets or sets the lexeme used by the expression parser to recognize
        /// the operator.
        /// </summary>
        public virtual Lexeme Lexeme
        {
            get { return lexeme; }
            set { lexeme = value; }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// The number of operands that the operator requires.
        /// </summary>
        private int operands;
        /// <summary>
        /// Gets or sets the number of operands that the operator requires.
        /// </summary>
        public virtual int Operands
        {
            get { return operands; }
            set { operands = value; }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// The list of operand types supported by the operator.
        /// </summary>
        private TypeList types;
        /// <summary>
        /// Gets or sets the list of operand types supported by the operator.
        /// </summary>
        public virtual TypeList Types
        {
            get { return types; }
            set { types = value; }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// The flags controlling the behavior of the operator.
        /// </summary>
        private OperatorFlags flags;
        /// <summary>
        /// Gets or sets the flags controlling the behavior of the operator.
        /// </summary>
        public virtual OperatorFlags Flags
        {
            get { return flags; }
            set { flags = value; }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// The string comparison type used by the operator, when applicable.
        /// </summary>
        private StringComparison comparisonType;
        /// <summary>
        /// Gets or sets the string comparison type used by the operator, when
        /// applicable.
        /// </summary>
        public virtual StringComparison ComparisonType
        {
            get { return comparisonType; }
            set { comparisonType = value; }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region IWrapperData Members
        /// <summary>
        /// The token associated with the operator.
        /// </summary>
        private long token;
        /// <summary>
        /// Gets or sets the token associated with the operator.
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
        /// This method returns a string representation of the operator.
        /// </summary>
        /// <returns>
        /// The name of the operator, or an empty string if it has no name.
        /// </returns>
        public override string ToString()
        {
            return (name != null) ? name : String.Empty;
        }
        #endregion
    }
}
