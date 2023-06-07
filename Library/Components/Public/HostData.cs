/*
 * HostData.cs --
 *
 * Copyright (c) 2007-2012 by Joe Mistachkin.  All rights reserved.
 *
 * See the file "license.terms" for information on usage and redistribution of
 * this file, and for a DISCLAIMER OF ALL WARRANTIES.
 *
 * RCS: @(#) $Id: $
 */

using System;
using System.Resources;
using Eagle._Attributes;
using Eagle._Components.Private;
using Eagle._Interfaces.Public;

namespace Eagle._Components.Public
{
#if SERIALIZATION
    [Serializable()]
#endif
    [ObjectId("3574f21a-8d6e-40cf-aac8-41a027b3b12f")]
    public class HostData : IHostData
    {
        public HostData(
            string name,
            string group,
            string description,
            IClientData clientData,
            string typeName,
            Interpreter interpreter,
            ResourceManager resourceManager,
            string profile,
            HostCreateFlags hostCreateFlags
            )
        {
            this.kind = IdentifierKind.HostData;
            this.id = AttributeOps.GetObjectId(this);
            this.name = name;
            this.group = group;
            this.description = description;
            this.clientData = clientData;
            this.typeName = typeName;
            this.interpreter = interpreter;
            this.resourceManager = resourceManager;
            this.profile = profile;
            this.hostCreateFlags = hostCreateFlags;
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

        #region IGetInterpreter / ISetInterpreter Members
#if SERIALIZATION && !ISOLATED_INTERPRETERS && !ISOLATED_PLUGINS
        [NonSerialized()]
#endif
        private Interpreter interpreter;
        public virtual Interpreter Interpreter
        {
            get { return interpreter; }
            set { interpreter = value; }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////////////////////////////

        #region ITypeAndName Members
        private string typeName;
        public virtual string TypeName
        {
            get { return typeName; }
            set { typeName = value; }
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        private Type type;
        public virtual Type Type
        {
            get { return type; }
            set { type = value; }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////////////////////////////

        #region IHostData Members
#if SERIALIZATION
        [NonSerialized()]
#endif
        private ResourceManager resourceManager;
        public virtual ResourceManager ResourceManager
        {
            get { return resourceManager; }
            set { resourceManager = value; }
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        private string profile;
        public virtual string Profile
        {
            get { return profile; }
            set { profile = value; }
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        private HostCreateFlags hostCreateFlags;
        public virtual HostCreateFlags HostCreateFlags
        {
            get { return hostCreateFlags; }
            set { hostCreateFlags = value; }
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
