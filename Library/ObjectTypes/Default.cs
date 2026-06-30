/*
 * Default.cs --
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
using Eagle._Components.Public;
using Eagle._Interfaces.Public;

namespace Eagle._ObjectTypes
{
    /// <summary>
    /// This class provides the default implementation of the
    /// <see cref="IObjectType" /> interface, which represents a registered
    /// object type known to the Eagle engine.  An object type associates a
    /// name with a managed <see cref="System.Type" /> and supplies the hooks
    /// used to convert, update, duplicate, and shimmer the internal
    /// representation of values of that type.  The conversion methods in this
    /// base class are no-op placeholders that derived classes override.
    /// </summary>
    [ObjectId("03395a90-9970-478a-99fa-6fa2f486c158")]
    public class Default : IObjectType
    {
        #region Public Constructors
        /// <summary>
        /// Constructs an instance of the default object type, optionally
        /// initializing it from the supplied object type data.
        /// </summary>
        /// <param name="objectTypeData">
        /// The data used to create and identify this object type, such as its
        /// name and associated managed type.  This parameter may be null, in
        /// which case the object type is left with default property values.
        /// </param>
        public Default(
            IObjectTypeData objectTypeData
            )
        {
            kind = IdentifierKind.ObjectType;
            id = AttributeOps.GetObjectId(this);
            group = AttributeOps.GetObjectGroups(this);

            if (objectTypeData != null)
            {
                id = objectTypeData.Id;

                EntityOps.MaybeSetupId(this);

                EntityOps.MaybeSetGroup(
                    this, objectTypeData.Group);

                name = objectTypeData.Name;
                description = objectTypeData.Description;
                clientData = objectTypeData.ClientData;
                type = objectTypeData.Type;
                token = objectTypeData.Token;
            }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region IIdentifierName Members
        /// <summary>
        /// The name of this object type.
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

        ///////////////////////////////////////////////////////////////////////

        #region IIdentifierBase Members
        /// <summary>
        /// The kind of identifier represented by this object type.
        /// </summary>
        private IdentifierKind kind;
        /// <summary>
        /// Gets or sets the kind of identifier represented by this object
        /// type.
        /// </summary>
        public virtual IdentifierKind Kind
        {
            get { return kind; }
            set { kind = value; }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// The unique identifier for this object type.
        /// </summary>
        private Guid id;
        /// <summary>
        /// Gets or sets the unique identifier for this object type.
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
        /// The extra, caller-specific data associated with this object type,
        /// if any.
        /// </summary>
        private IClientData clientData;
        /// <summary>
        /// Gets or sets the extra, caller-specific data associated with this
        /// object type.
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
        /// The name of the group this object type belongs to, if any.
        /// </summary>
        private string group;
        /// <summary>
        /// Gets or sets the name of the group this object type belongs to.
        /// </summary>
        public virtual string Group
        {
            get { return group; }
            set { group = value; }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// The human-readable description of this object type, if any.
        /// </summary>
        private string description;
        /// <summary>
        /// Gets or sets the human-readable description of this object type.
        /// </summary>
        public virtual string Description
        {
            get { return description; }
            set { description = value; }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region IObjectTypeData Members
        /// <summary>
        /// The managed type associated with this object type.
        /// </summary>
        private Type type;
        /// <summary>
        /// Gets or sets the managed type associated with this object type.
        /// </summary>
        public virtual Type Type
        {
            get { return type; }
            set { type = value; }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region IWrapperData Members
        /// <summary>
        /// The interpreter token that uniquely identifies this object type
        /// within its containing collection.
        /// </summary>
        private long token;
        /// <summary>
        /// Gets or sets the interpreter token that uniquely identifies this
        /// object type within its containing collection.
        /// </summary>
        public virtual long Token
        {
            get { return token; }
            set { token = value; }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region IObjectType Members
        /// <summary>
        /// Builds the internal representation of this object type from the
        /// supplied string value.  This default implementation performs no
        /// work and always succeeds; derived classes override it to perform
        /// the conversion.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter context for this operation.  This parameter should
        /// not be null.
        /// </param>
        /// <param name="text">
        /// The string value to convert into the internal representation.
        /// </param>
        /// <param name="value">
        /// Upon success, receives a pointer to the newly built internal
        /// representation.
        /// </param>
        /// <param name="error">
        /// Upon failure, receives an appropriate error message.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise, a non-Ok value
        /// with details placed in the <paramref name="error" /> parameter.
        /// </returns>
        public virtual ReturnCode SetFromAny(
            Interpreter interpreter,
            string text,
            ref IntPtr value,
            ref Result error
            )
        {
            return ReturnCode.Ok;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Regenerates the string representation of this object type from its
        /// internal representation.  This default implementation performs no
        /// work and always succeeds; derived classes override it to perform
        /// the conversion.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter context for this operation.  This parameter should
        /// not be null.
        /// </param>
        /// <param name="text">
        /// Upon success, receives the regenerated string representation.
        /// </param>
        /// <param name="value">
        /// A pointer to the internal representation to convert into a string.
        /// </param>
        /// <param name="error">
        /// Upon failure, receives an appropriate error message.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise, a non-Ok value
        /// with details placed in the <paramref name="error" /> parameter.
        /// </returns>
        public virtual ReturnCode UpdateString(
            Interpreter interpreter,
            ref string text,
            IntPtr value,
            ref Result error
            )
        {
            return ReturnCode.Ok;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Creates a copy of the internal representation of this object type.
        /// This default implementation performs no work and always succeeds;
        /// derived classes override it to perform the duplication.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter context for this operation.  This parameter should
        /// not be null.
        /// </param>
        /// <param name="oldValue">
        /// A pointer to the existing internal representation to copy.
        /// </param>
        /// <param name="newValue">
        /// Upon success, receives a pointer to the newly created copy of the
        /// internal representation.
        /// </param>
        /// <param name="error">
        /// Upon failure, receives an appropriate error message.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise, a non-Ok value
        /// with details placed in the <paramref name="error" /> parameter.
        /// </returns>
        public virtual ReturnCode Duplicate(
            Interpreter interpreter,
            IntPtr oldValue,
            ref IntPtr newValue,
            ref Result error
            )
        {
            return ReturnCode.Ok;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Converts a value to this object type, replacing its internal
        /// representation in place (i.e. "shimmering" it).  This default
        /// implementation performs no work and always succeeds; derived
        /// classes override it to perform the conversion.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter context for this operation.  This parameter should
        /// not be null.
        /// </param>
        /// <param name="text">
        /// The string value associated with the value being shimmered.
        /// </param>
        /// <param name="value">
        /// On input, a pointer to the existing internal representation; upon
        /// success, receives a pointer to the new internal representation for
        /// this object type.
        /// </param>
        /// <param name="error">
        /// Upon failure, receives an appropriate error message.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise, a non-Ok value
        /// with details placed in the <paramref name="error" /> parameter.
        /// </returns>
        public virtual ReturnCode Shimmer(
            Interpreter interpreter,
            string text,
            ref IntPtr value,
            ref Result error
            )
        {
            return ReturnCode.Ok;
        }
        #endregion
    }
}
