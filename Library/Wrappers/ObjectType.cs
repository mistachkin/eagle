/*
 * ObjectType.cs --
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
using Eagle._Interfaces.Public;

namespace Eagle._Wrappers
{
    /// <summary>
    /// This class implements a wrapper around an <see cref="IObjectType" />
    /// object, forwarding the object type interface to the wrapped instance.
    /// It is used so an object type can participate in the interpreter as an
    /// identifiable, token-bearing entity.
    /// </summary>
    [ObjectId("c9d4c0ab-a50f-4610-9d5c-cd48c5c55c4d")]
    internal sealed class ObjectType : Default, IObjectType
    {
        #region Public Constructors
        /// <summary>
        /// Constructs an instance of this wrapper class.
        /// </summary>
        public ObjectType()
            : base()
        {
            // do nothing.
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Private Data
        /// <summary>
        /// The wrapped <see cref="IObjectType" /> object, or null if none has
        /// been set.
        /// </summary>
        internal IObjectType objectType;
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region IIdentifierName Members
        /// <summary>
        /// Gets or sets the name of the wrapped object type.
        /// </summary>
        public string Name
        {
            get { return (objectType != null) ? objectType.Name : null; }
            set { if (objectType != null) { objectType.Name = value; } }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region IIdentifierBase Members
        /// <summary>
        /// Gets or sets the identifier kind of the wrapped object type.
        /// </summary>
        public IdentifierKind Kind
        {
            get { return (objectType != null) ? objectType.Kind : IdentifierKind.None; }
            set { if (objectType != null) { objectType.Kind = value; } }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Gets or sets the unique identifier of the wrapped object type.
        /// </summary>
        public Guid Id
        {
            get { return (objectType != null) ? objectType.Id : Guid.Empty; }
            set { if (objectType != null) { objectType.Id = value; } }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region IGetClientData / ISetClientData Members
        /// <summary>
        /// Gets or sets the client data associated with the wrapped object
        /// type.
        /// </summary>
        public IClientData ClientData
        {
            get { return (objectType != null) ? objectType.ClientData : null; }
            set { if (objectType != null) { objectType.ClientData = value; } }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region IIdentifier Members
        /// <summary>
        /// Gets or sets the group of the wrapped object type.
        /// </summary>
        public string Group
        {
            get { return (objectType != null) ? objectType.Group : null; }
            set { if (objectType != null) { objectType.Group = value; } }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Gets or sets the description of the wrapped object type.
        /// </summary>
        public string Description
        {
            get { return (objectType != null) ? objectType.Description : null; }
            set { if (objectType != null) { objectType.Description = value; } }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region IObjectTypeData Members
        /// <summary>
        /// Gets or sets the type of the wrapped object type.
        /// </summary>
        public Type Type
        {
            get { return (objectType != null) ? objectType.Type : null; }
            set { if (objectType != null) { objectType.Type = value; } }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region IObjectType Members
        /// <summary>
        /// This method forwards the set-from-any operation to the wrapped
        /// object type.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter context this object type is operating in.  This
        /// parameter should not be null.
        /// </param>
        /// <param name="text">
        /// The string representation involved in the operation.
        /// </param>
        /// <param name="value">
        /// Upon success, this is set to the resulting native value.
        /// </param>
        /// <param name="error">
        /// Upon failure, this contains an appropriate error message.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise,
        /// <see cref="ReturnCode.Error" />, including when there is no wrapped
        /// object.
        /// </returns>
        public ReturnCode SetFromAny(
            Interpreter interpreter,
            string text,
            ref IntPtr value,
            ref Result error
            )
        {
            if (objectType == null)
            {
                error = "invalid wrapper target";
                return ReturnCode.Error;
            }

            return objectType.SetFromAny(
                interpreter, text, ref value, ref error);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method forwards the update-string operation to the wrapped
        /// object type.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter context this object type is operating in.  This
        /// parameter should not be null.
        /// </param>
        /// <param name="text">
        /// Upon success, this is set to the resulting string representation.
        /// </param>
        /// <param name="value">
        /// The native value involved in the operation.
        /// </param>
        /// <param name="error">
        /// Upon failure, this contains an appropriate error message.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise,
        /// <see cref="ReturnCode.Error" />, including when there is no wrapped
        /// object.
        /// </returns>
        public ReturnCode UpdateString(
            Interpreter interpreter,
            ref string text,
            IntPtr value,
            ref Result error
            )
        {
            if (objectType == null)
            {
                error = "invalid wrapper target";
                return ReturnCode.Error;
            }

            return objectType.UpdateString(
                interpreter, ref text, value, ref error);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method forwards the duplicate operation to the wrapped object
        /// type.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter context this object type is operating in.  This
        /// parameter should not be null.
        /// </param>
        /// <param name="oldValue">
        /// The existing native value to duplicate.
        /// </param>
        /// <param name="newValue">
        /// Upon success, this is set to the duplicated native value.
        /// </param>
        /// <param name="error">
        /// Upon failure, this contains an appropriate error message.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise,
        /// <see cref="ReturnCode.Error" />, including when there is no wrapped
        /// object.
        /// </returns>
        public ReturnCode Duplicate(
            Interpreter interpreter,
            IntPtr oldValue,
            ref IntPtr newValue,
            ref Result error
            )
        {
            if (objectType == null)
            {
                error = "invalid wrapper target";
                return ReturnCode.Error;
            }

            return objectType.Duplicate(
                interpreter, oldValue, ref newValue, ref error);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method forwards the shimmer operation to the wrapped object
        /// type.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter context this object type is operating in.  This
        /// parameter should not be null.
        /// </param>
        /// <param name="text">
        /// The string representation involved in the operation.
        /// </param>
        /// <param name="value">
        /// Upon success, this is set to the resulting native value.
        /// </param>
        /// <param name="error">
        /// Upon failure, this contains an appropriate error message.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise,
        /// <see cref="ReturnCode.Error" />, including when there is no wrapped
        /// object.
        /// </returns>
        public ReturnCode Shimmer(
            Interpreter interpreter,
            string text,
            ref IntPtr value,
            ref Result error
            )
        {
            if (objectType == null)
            {
                error = "invalid wrapper target";
                return ReturnCode.Error;
            }

            return objectType.Shimmer(
                interpreter, text, ref value, ref error);
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region IWrapper Members
        /// <summary>
        /// Gets a value indicating whether the object wrapped by this instance
        /// represents a resource that requires disposal.
        /// </summary>
        public override bool IsDisposable
        {
            get { return false; }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Gets or sets the underlying <see cref="IObjectType" /> object
        /// wrapped by this instance.
        /// </summary>
        public override object Object
        {
            get { return objectType; }
            set { objectType = (IObjectType)value; } /* throw */
        }
        #endregion
    }
}
