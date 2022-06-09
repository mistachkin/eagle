/*
 * ProcedureData.cs --
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
#if SERIALIZATION
    [Serializable()]
#endif
    [ObjectId("84de62ac-7cea-432a-8f10-b5267cab6122")]
    public class ProcedureData : IProcedureData
    {
        public ProcedureData(
            string name,
            string group,
            string description,
            ProcedureFlags flags,
            ArgumentList arguments,
            ArgumentDictionary namedArguments,
            string body,
            IScriptLocation location,
            IClientData clientData,
            long token
            )
        {
            this.kind = IdentifierKind.ProcedureData;
            this.id = AttributeOps.GetObjectId(this);
            this.name = name;
            this.group = group;
            this.description = description;
            this.flags = flags;
            this.clientData = clientData;
            this.arguments = arguments;
            this.namedArguments = namedArguments;
            this.body = body;
            this.location = location;
            this.token = token;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        #region IIdentifierName Members
        private string name;
        public virtual string Name
        {
            get { return name; }
            set { name = value; }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////////////////////////////

        #region IIdentifierBase Members
        private IdentifierKind kind;
        public virtual IdentifierKind Kind
        {
            get { return kind; }
            set { kind = value; }
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        private Guid id;
        public virtual Guid Id
        {
            get { return id; }
            set { id = value; }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////////////////////////////

        #region IIdentifier Members
        private string group;
        public virtual string Group
        {
            get { return group; }
            set { group = value; }
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        private string description;
        public virtual string Description
        {
            get { return description; }
            set { description = value; }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////////////////////////////

        #region IGetClientData / ISetClientData Members
        private IClientData clientData;
        public virtual IClientData ClientData
        {
            get { return clientData; }
            set { clientData = value; }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////////////////////////////

        #region IProcedureData Members
        private ProcedureFlags flags;
        public virtual ProcedureFlags Flags
        {
            get { return flags; }
            set { flags = value; }
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        private ArgumentList arguments;
        public virtual ArgumentList Arguments
        {
            get { return arguments; }
            set { arguments = value; }
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        private ArgumentDictionary namedArguments;
        public virtual ArgumentDictionary NamedArguments
        {
            get { return namedArguments; }
            set { namedArguments = value; }
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        private string body;
        public virtual string Body
        {
            get { return body; }
            set { body = value; }
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        private IScriptLocation location;
        public virtual IScriptLocation Location
        {
            get { return location; }
            set { location = value; }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////////////////////////////

        #region IWrapperData Members
        private long token;
        public virtual long Token
        {
            get { return token; }
            set { token = value; }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////////////////////////////

        #region System.Object Overrides
        public override string ToString()
        {
            return (name != null) ? name : String.Empty;
        }
        #endregion
    }
}
