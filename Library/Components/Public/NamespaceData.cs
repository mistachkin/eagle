/*
 * NamespaceData.cs --
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
#if SERIALIZATION
    [Serializable()]
#endif
    [ObjectId("f93ed198-0ce3-4568-a66d-4191d6b20c85")]
    public class NamespaceData : INamespaceData
    {
        #region Public Constructors
        public NamespaceData(
            string name,
            IClientData clientData,
            Interpreter interpreter,
            INamespace parent,
            IResolve resolve,
            ICallFrame variableFrame,
            string unknown
            )
        {
            this.kind = IdentifierKind.NamespaceData;
            this.id = AttributeOps.GetObjectId(this);
            this.name = NamespaceOps.TrimAll(name);
            this.clientData = clientData;
            this.interpreter = interpreter;
            this.parent = parent;
            this.resolve = resolve;
            this.variableFrame = variableFrame;
            this.unknown = unknown;
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

        #region IGetClientData / ISetClientData Members
        private IClientData clientData;
        public virtual IClientData ClientData
        {
            get { return clientData; }
            set { clientData = value; }
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

        ///////////////////////////////////////////////////////////////////////

        #region INamespaceData Members
        private INamespace parent;
        public virtual INamespace Parent
        {
            get { return parent; }
            set { parent = value; }
        }

        ///////////////////////////////////////////////////////////////////////

        private IResolve resolve;
        public virtual IResolve Resolve
        {
            get { return resolve; }
            set { resolve = value; }
        }

        ///////////////////////////////////////////////////////////////////////

        private ICallFrame variableFrame;
        public virtual ICallFrame VariableFrame
        {
            get { return variableFrame; }
            set { variableFrame = value; }
        }

        ///////////////////////////////////////////////////////////////////////

        private string unknown;
        public virtual string Unknown
        {
            get { return unknown; }
            set { unknown = value; }
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
