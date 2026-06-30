/*
 * ObjectTypeData.cs --
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
    /// This class holds the data that describes a registered object type within
    /// an Eagle interpreter, including its identity, the underlying managed
    /// type, and its token.  It implements <see cref="IObjectTypeData" />.
    /// </summary>
    [ObjectId("4bbb8bcd-079c-4796-94d2-4f8cc77b5cdb")]
    public class ObjectTypeData : IObjectTypeData
    {
        /// <summary>
        /// Constructs an object type data instance from the fully specified set
        /// of identity, type, and token parameters.
        /// </summary>
        /// <param name="name">
        /// The name of this object type.  This parameter may be null.
        /// </param>
        /// <param name="group">
        /// The group of this object type.  This parameter may be null.
        /// </param>
        /// <param name="description">
        /// The description of this object type.  This parameter may be null.
        /// </param>
        /// <param name="clientData">
        /// The client data associated with this object type, if any.  This
        /// parameter may be null.
        /// </param>
        /// <param name="type">
        /// The underlying managed type represented by this object type.  This
        /// parameter may be null.
        /// </param>
        /// <param name="token">
        /// The token used to identify this object type within the interpreter.
        /// </param>
        public ObjectTypeData(
            string name,
            string group,
            string description,
            IClientData clientData,
            Type type,
            long token
            )
        {
            this.kind = IdentifierKind.ObjectTypeData;
            this.id = AttributeOps.GetObjectId(this);
            this.name = name;
            this.group = group;
            this.description = description;
            this.clientData = clientData;
            this.type = type;
            this.token = token;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        #region IIdentifierName Members
        /// <summary>
        /// Stores the name of this object type.
        /// </summary>
        private string name;
        /// <summary>
        /// Gets or sets the name of this object type.
        /// </summary>
        public virtual string Name
        {
            get { return name; }
            set { name = value; }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////////////////////////////

        #region IIdentifierBase Members
        /// <summary>
        /// Stores the identifier kind of this object type.
        /// </summary>
        private IdentifierKind kind;
        /// <summary>
        /// Gets or sets the identifier kind of this object type.
        /// </summary>
        public virtual IdentifierKind Kind
        {
            get { return kind; }
            set { kind = value; }
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Stores the globally unique identifier of this object type.
        /// </summary>
        private Guid id;
        /// <summary>
        /// Gets or sets the globally unique identifier of this object type.
        /// </summary>
        public virtual Guid Id
        {
            get { return id; }
            set { id = value; }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////////////////////////////

        #region IGetClientData / ISetClientData Members
        /// <summary>
        /// Stores the client data associated with this object type.
        /// </summary>
        private IClientData clientData;
        /// <summary>
        /// Gets or sets the client data associated with this object type.
        /// </summary>
        public virtual IClientData ClientData
        {
            get { return clientData; }
            set { clientData = value; }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////////////////////////////

        #region IIdentifier Members
        /// <summary>
        /// Stores the group of this object type.
        /// </summary>
        private string group;
        /// <summary>
        /// Gets or sets the group of this object type.
        /// </summary>
        public virtual string Group
        {
            get { return group; }
            set { group = value; }
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Stores the description of this object type.
        /// </summary>
        private string description;
        /// <summary>
        /// Gets or sets the description of this object type.
        /// </summary>
        public virtual string Description
        {
            get { return description; }
            set { description = value; }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////////////////////////////

        #region IObjectTypeData Members
        /// <summary>
        /// Stores the underlying managed type represented by this object type.
        /// </summary>
        private Type type;
        /// <summary>
        /// Gets or sets the underlying managed type represented by this object
        /// type.
        /// </summary>
        public virtual Type Type
        {
            get { return type; }
            set { type = value; }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////////////////////////////

        #region IWrapperData Members
        /// <summary>
        /// Stores the token used to identify this object type within the
        /// interpreter.
        /// </summary>
        private long token;
        /// <summary>
        /// Gets or sets the token used to identify this object type within the
        /// interpreter.
        /// </summary>
        public virtual long Token
        {
            get { return token; }
            set { token = value; }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////////////////////////////

        #region System.Object Overrides
        /// <summary>
        /// This method returns the name of this object type, or an empty string
        /// when it has no name.
        /// </summary>
        /// <returns>
        /// The name of this object type, or an empty string when it has no
        /// name.
        /// </returns>
        public override string ToString()
        {
            return (name != null) ? name : String.Empty;
        }
        #endregion
    }
}

