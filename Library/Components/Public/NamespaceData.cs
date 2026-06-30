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
    /// <summary>
    /// This class holds the data that describes an Eagle namespace, including
    /// its identity, the interpreter that owns it, its parent namespace, its
    /// associated resolver, its variable call frame, and its unknown command
    /// handler.  It implements <see cref="INamespaceData" />.
    /// </summary>
#if SERIALIZATION
    [Serializable()]
#endif
    [ObjectId("f93ed198-0ce3-4568-a66d-4191d6b20c85")]
    public class NamespaceData : INamespaceData
    {
        #region Public Constructors
        /// <summary>
        /// Constructs a namespace data instance from the fully specified set of
        /// identity, interpreter, hierarchy, resolver, and frame parameters.
        /// </summary>
        /// <param name="name">
        /// The name of this namespace.  This parameter may be null.
        /// </param>
        /// <param name="clientData">
        /// The client data associated with this namespace, if any.  This
        /// parameter may be null.
        /// </param>
        /// <param name="interpreter">
        /// The interpreter that owns this namespace.  This parameter may be
        /// null.
        /// </param>
        /// <param name="parent">
        /// The parent of this namespace, or null if this is the global
        /// namespace.
        /// </param>
        /// <param name="resolve">
        /// The resolver associated with this namespace.  This parameter may be
        /// null.
        /// </param>
        /// <param name="variableFrame">
        /// The call frame that holds the variables for this namespace.  This
        /// parameter may be null.
        /// </param>
        /// <param name="unknown">
        /// The name of the unknown command handler for this namespace.  This
        /// parameter may be null.
        /// </param>
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
        /// <summary>
        /// Stores the name of this namespace.
        /// </summary>
        private string name;
        /// <summary>
        /// Gets or sets the name of this namespace.
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
        /// Stores the identifier kind of this namespace.
        /// </summary>
        private IdentifierKind kind;
        /// <summary>
        /// Gets or sets the identifier kind of this namespace.
        /// </summary>
        public virtual IdentifierKind Kind
        {
            get { return kind; }
            set { kind = value; }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Stores the globally unique identifier of this namespace.
        /// </summary>
        private Guid id;
        /// <summary>
        /// Gets or sets the globally unique identifier of this namespace.
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
        /// Stores the client data associated with this namespace.
        /// </summary>
        private IClientData clientData;
        /// <summary>
        /// Gets or sets the client data associated with this namespace.
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
        /// Stores the group of this namespace.
        /// </summary>
        private string group;
        /// <summary>
        /// Gets or sets the group of this namespace.
        /// </summary>
        public virtual string Group
        {
            get { return group; }
            set { group = value; }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Stores the description of this namespace.
        /// </summary>
        private string description;
        /// <summary>
        /// Gets or sets the description of this namespace.
        /// </summary>
        public virtual string Description
        {
            get { return description; }
            set { description = value; }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region IGetInterpreter / ISetInterpreter Members
        /// <summary>
        /// Stores the interpreter that owns this namespace.
        /// </summary>
#if SERIALIZATION && !ISOLATED_INTERPRETERS && !ISOLATED_PLUGINS
        [NonSerialized()]
#endif
        private Interpreter interpreter;
        /// <summary>
        /// Gets or sets the interpreter that owns this namespace.
        /// </summary>
        public virtual Interpreter Interpreter
        {
            get { return interpreter; }
            set { interpreter = value; }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region INamespaceData Members
        /// <summary>
        /// Stores the parent of this namespace, or null if this is the global
        /// namespace.
        /// </summary>
        private INamespace parent;
        /// <summary>
        /// Gets or sets the parent of this namespace, or null if this is the
        /// global namespace.
        /// </summary>
        public virtual INamespace Parent
        {
            get { return parent; }
            set { parent = value; }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Stores the resolver associated with this namespace.
        /// </summary>
        private IResolve resolve;
        /// <summary>
        /// Gets or sets the resolver associated with this namespace.
        /// </summary>
        public virtual IResolve Resolve
        {
            get { return resolve; }
            set { resolve = value; }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Stores the call frame that holds the variables for this namespace.
        /// </summary>
        private ICallFrame variableFrame;
        /// <summary>
        /// Gets or sets the call frame that holds the variables for this
        /// namespace.
        /// </summary>
        public virtual ICallFrame VariableFrame
        {
            get { return variableFrame; }
            set { variableFrame = value; }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Stores the name of the unknown command handler for this namespace.
        /// </summary>
        private string unknown;
        /// <summary>
        /// Gets or sets the name of the unknown command handler for this
        /// namespace.
        /// </summary>
        public virtual string Unknown
        {
            get { return unknown; }
            set { unknown = value; }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region System.Object Overrides
        /// <summary>
        /// This method returns the name of this namespace, or an empty string
        /// when it has no name.
        /// </summary>
        /// <returns>
        /// The name of this namespace, or an empty string when it has no name.
        /// </returns>
        public override string ToString()
        {
            return (name != null) ? name : String.Empty;
        }
        #endregion
    }
}
