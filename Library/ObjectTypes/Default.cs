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
    [ObjectId("03395a90-9970-478a-99fa-6fa2f486c158")]
    public class Default : IObjectType
    {
        #region Public Constructors
        public Default(
            IObjectTypeData objectTypeData
            )
        {
            kind = IdentifierKind.ObjectType;
            id = AttributeOps.GetObjectId(this);
            group = AttributeOps.GetObjectGroups(this);

            if (objectTypeData != null)
            {
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

        #region IObjectTypeData Members
        private Type type;
        public virtual Type Type
        {
            get { return type; }
            set { type = value; }
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

        #region IObjectType Members
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
