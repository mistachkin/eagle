/*
 * ResolveData.cs --
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
    /// This class holds the identifying and configuration data associated with
    /// an Eagle resolver.  It implements <see cref="IResolveData" /> and
    /// carries the resolver's name, group, description, client data, associated
    /// interpreter, resolver flags, and wrapper token.
    /// </summary>
#if SERIALIZATION
    [Serializable()]
#endif
    [ObjectId("8c1a11ac-d826-4dcb-aef3-bfd184b1bfb7")]
    public class ResolveData : IResolveData
    {
        /// <summary>
        /// Constructs resolver data from the specified identity, configuration,
        /// and context values.
        /// </summary>
        /// <param name="name">
        /// The name of the resolver.  This parameter may be null.
        /// </param>
        /// <param name="group">
        /// The group of the resolver.  This parameter may be null.
        /// </param>
        /// <param name="description">
        /// The description of the resolver.  This parameter may be null.
        /// </param>
        /// <param name="clientData">
        /// The client data to associate with the resolver, if any.  This
        /// parameter may be null.
        /// </param>
        /// <param name="interpreter">
        /// The interpreter that the resolver is associated with.  This parameter
        /// may be null.
        /// </param>
        /// <param name="flags">
        /// The flags controlling the behavior of the resolver.
        /// </param>
        /// <param name="token">
        /// The wrapper token associated with the resolver.
        /// </param>
        public ResolveData(
            string name,
            string group,
            string description,
            IClientData clientData,
            Interpreter interpreter,
            ResolveFlags flags,
            long token
            )
        {
            this.kind = IdentifierKind.ResolveData;
            this.id = AttributeOps.GetObjectId(this);
            this.name = name;
            this.group = group;
            this.description = description;
            this.clientData = clientData;
            this.interpreter = interpreter;
            this.flags = flags;
            this.token = token;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        #region IIdentifierName Members
        /// <summary>
        /// The name of the resolver.
        /// </summary>
        private string name;
        /// <summary>
        /// Gets or sets the name of the resolver.
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
        /// The kind of identifier represented by this object.
        /// </summary>
        private IdentifierKind kind;
        /// <summary>
        /// Gets or sets the kind of identifier represented by this object.
        /// </summary>
        public virtual IdentifierKind Kind
        {
            get { return kind; }
            set { kind = value; }
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// The unique identifier of this object.
        /// </summary>
        private Guid id;
        /// <summary>
        /// Gets or sets the unique identifier of this object.
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
        /// The client data associated with the resolver.
        /// </summary>
        private IClientData clientData;
        /// <summary>
        /// Gets or sets the client data associated with the resolver.
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
        /// The group of the resolver.
        /// </summary>
        private string group;
        /// <summary>
        /// Gets or sets the group of the resolver.
        /// </summary>
        public virtual string Group
        {
            get { return group; }
            set { group = value; }
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// The description of the resolver.
        /// </summary>
        private string description;
        /// <summary>
        /// Gets or sets the description of the resolver.
        /// </summary>
        public virtual string Description
        {
            get { return description; }
            set { description = value; }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////////////////////////////

        #region IGetInterpreter / ISetInterpreter Members
        /// <summary>
        /// The interpreter that the resolver is associated with.
        /// </summary>
#if SERIALIZATION && !ISOLATED_INTERPRETERS && !ISOLATED_PLUGINS
        [NonSerialized()]
#endif
        private Interpreter interpreter;
        /// <summary>
        /// Gets or sets the interpreter that the resolver is associated with.
        /// </summary>
        public virtual Interpreter Interpreter
        {
            get { return interpreter; }
            set { interpreter = value; }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////////////////////////////

        #region IWrapperData Members
        /// <summary>
        /// The wrapper token associated with the resolver.
        /// </summary>
        private long token;
        /// <summary>
        /// Gets or sets the wrapper token associated with the resolver.
        /// </summary>
        public virtual long Token
        {
            get { return token; }
            set { token = value; }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////////////////////////////

        #region IResolveData Members
        /// <summary>
        /// The flags controlling the behavior of the resolver.
        /// </summary>
        private ResolveFlags flags;
        /// <summary>
        /// Gets or sets the flags controlling the behavior of the resolver.
        /// </summary>
        public virtual ResolveFlags Flags
        {
            get { return flags; }
            set { flags = value; }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////////////////////////////

        #region System.Object Overrides
        /// <summary>
        /// This method returns the string representation of this object.
        /// </summary>
        /// <returns>
        /// The name of the resolver, or an empty string if it has no name.
        /// </returns>
        public override string ToString()
        {
            return (name != null) ? name : String.Empty;
        }
        #endregion
    }
}
