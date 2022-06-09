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
    [ObjectId("c9d4c0ab-a50f-4610-9d5c-cd48c5c55c4d")]
    internal sealed class ObjectType : Default, IObjectType
    {
        #region Public Constructors
        public ObjectType(
            long token,
            IObjectType objectType
            )
            : base(token)
        {
            this.objectType = objectType;
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Private Data
        internal IObjectType objectType;
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region IIdentifierName Members
        public string Name
        {
            get { return (objectType != null) ? objectType.Name : null; }
            set { if (objectType != null) { objectType.Name = value; } }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region IIdentifierBase Members
        public IdentifierKind Kind
        {
            get { return (objectType != null) ? objectType.Kind : IdentifierKind.None; }
            set { if (objectType != null) { objectType.Kind = value; } }
        }

        ///////////////////////////////////////////////////////////////////////

        public Guid Id
        {
            get { return (objectType != null) ? objectType.Id : Guid.Empty; }
            set { if (objectType != null) { objectType.Id = value; } }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region IIdentifier Members
        public string Group
        {
            get { return (objectType != null) ? objectType.Group : null; }
            set { if (objectType != null) { objectType.Group = value; } }
        }

        ///////////////////////////////////////////////////////////////////////

        public string Description
        {
            get { return (objectType != null) ? objectType.Description : null; }
            set { if (objectType != null) { objectType.Description = value; } }
        }

        ///////////////////////////////////////////////////////////////////////

        public IClientData ClientData
        {
            get { return (objectType != null) ? objectType.ClientData : null; }
            set { if (objectType != null) { objectType.ClientData = value; } }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region IObjectTypeData Members
        public Type Type
        {
            get { return (objectType != null) ? objectType.Type : null; }
            set { if (objectType != null) { objectType.Type = value; } }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region IObjectType Members
        public ReturnCode SetFromAny(
            Interpreter interpreter,
            string text,
            ref IntPtr value,
            ref Result error
            )
        {
            if (objectType != null)
                return objectType.SetFromAny(
                    interpreter, text, ref value, ref error);
            else
                return ReturnCode.Error;
        }

        ///////////////////////////////////////////////////////////////////////

        public ReturnCode UpdateString(
            Interpreter interpreter,
            ref string text,
            IntPtr value,
            ref Result error
            )
        {
            if (objectType != null)
                return objectType.UpdateString(
                    interpreter, ref text, value, ref error);
            else
                return ReturnCode.Error;
        }

        ///////////////////////////////////////////////////////////////////////

        public ReturnCode Duplicate(
            Interpreter interpreter,
            IntPtr oldValue,
            ref IntPtr newValue,
            ref Result error
            )
        {
            if (objectType != null)
                return objectType.Duplicate(
                    interpreter, oldValue, ref newValue, ref error);
            else
                return ReturnCode.Error;
        }

        ///////////////////////////////////////////////////////////////////////

        public ReturnCode Shimmer(
            Interpreter interpreter,
            string text,
            ref IntPtr value,
            ref Result error
            )
        {
            if (objectType != null)
                return objectType.Shimmer(
                    interpreter, text, ref value, ref error);
            else
                return ReturnCode.Error;
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region IWrapper Members
        public override bool IsDisposable
        {
            get { return false; }
        }

        ///////////////////////////////////////////////////////////////////////

        public override object Object
        {
            get { return objectType; }
        }
        #endregion
    }
}
