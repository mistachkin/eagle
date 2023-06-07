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
#if SERIALIZATION
    [Serializable()]
#endif
    [ObjectId("cd6330c8-889b-4a60-a16a-fda58ddc7fb8")]
    internal class OperatorData : IOperatorData
    {
        #region Public Constructors
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

        #region IOperatorData Members
        private Lexeme lexeme;
        public virtual Lexeme Lexeme
        {
            get { return lexeme; }
            set { lexeme = value; }
        }

        ///////////////////////////////////////////////////////////////////////

        private int operands;
        public virtual int Operands
        {
            get { return operands; }
            set { operands = value; }
        }

        ///////////////////////////////////////////////////////////////////////

        private TypeList types;
        public virtual TypeList Types
        {
            get { return types; }
            set { types = value; }
        }

        ///////////////////////////////////////////////////////////////////////

        private OperatorFlags flags;
        public virtual OperatorFlags Flags
        {
            get { return flags; }
            set { flags = value; }
        }

        ///////////////////////////////////////////////////////////////////////

        private StringComparison comparisonType;
        public virtual StringComparison ComparisonType
        {
            get { return comparisonType; }
            set { comparisonType = value; }
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
