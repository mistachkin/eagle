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
    /// <summary>
    /// This class carries the data necessary to create and identify a host,
    /// including its name, group, description, client data, associated type
    /// and interpreter, resource manager, profile, and creation flags.  It
    /// implements <see cref="IHostData" />.
    /// </summary>
#if SERIALIZATION
    [Serializable()]
#endif
    [ObjectId("3574f21a-8d6e-40cf-aac8-41a027b3b12f")]
    public class HostData : IHostData
    {
        /// <summary>
        /// Constructs host data from the fully specified set of identity,
        /// interpreter, resource, profile, and creation-flag parameters.
        /// </summary>
        /// <param name="name">
        /// The name of the host.
        /// </param>
        /// <param name="group">
        /// The group of the host.
        /// </param>
        /// <param name="description">
        /// The description of the host.
        /// </param>
        /// <param name="clientData">
        /// The client data associated with the host, if any.  This parameter
        /// may be null.
        /// </param>
        /// <param name="typeName">
        /// The name of the type used to create the host.
        /// </param>
        /// <param name="interpreter">
        /// The interpreter associated with the host, if any.  This parameter
        /// may be null.
        /// </param>
        /// <param name="resourceManager">
        /// The resource manager used by the host, if any.  This parameter may
        /// be null.
        /// </param>
        /// <param name="profile">
        /// The profile name used by the host, if any.  This parameter may be
        /// null.
        /// </param>
        /// <param name="hostCreateFlags">
        /// The flags that control how the host is created.
        /// </param>
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
        /// <summary>
        /// The name of the host.
        /// </summary>
        private string name;
        /// <summary>
        /// Gets or sets the name of the host.
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
        /// The kind of identifier represented by this host data.
        /// </summary>
        private IdentifierKind kind;
        /// <summary>
        /// Gets or sets the kind of identifier represented by this host data.
        /// </summary>
        public virtual IdentifierKind Kind
        {
            get { return kind; }
            set { kind = value; }
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// The unique identifier of this host data.
        /// </summary>
        private Guid id;
        /// <summary>
        /// Gets or sets the unique identifier of this host data.
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
        /// The client data associated with the host, if any.
        /// </summary>
        private IClientData clientData;
        /// <summary>
        /// Gets or sets the client data associated with the host, if any.
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
        /// The group of the host.
        /// </summary>
        private string group;
        /// <summary>
        /// Gets or sets the group of the host.
        /// </summary>
        public virtual string Group
        {
            get { return group; }
            set { group = value; }
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// The description of the host.
        /// </summary>
        private string description;
        /// <summary>
        /// Gets or sets the description of the host.
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
        /// The interpreter associated with the host, if any.
        /// </summary>
#if SERIALIZATION && !ISOLATED_INTERPRETERS && !ISOLATED_PLUGINS
        [NonSerialized()]
#endif
        private Interpreter interpreter;
        /// <summary>
        /// Gets or sets the interpreter associated with the host, if any.
        /// </summary>
        public virtual Interpreter Interpreter
        {
            get { return interpreter; }
            set { interpreter = value; }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////////////////////////////

        #region ITypeAndName Members
        /// <summary>
        /// The name of the type used to create the host.
        /// </summary>
        private string typeName;
        /// <summary>
        /// Gets or sets the name of the type used to create the host.
        /// </summary>
        public virtual string TypeName
        {
            get { return typeName; }
            set { typeName = value; }
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// The type used to create the host.
        /// </summary>
        private Type type;
        /// <summary>
        /// Gets or sets the type used to create the host.
        /// </summary>
        public virtual Type Type
        {
            get { return type; }
            set { type = value; }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////////////////////////////

        #region IHostData Members
        /// <summary>
        /// The resource manager used by the host, if any.
        /// </summary>
#if SERIALIZATION
        [NonSerialized()]
#endif
        private ResourceManager resourceManager;
        /// <summary>
        /// Gets or sets the resource manager used by the host, if any.
        /// </summary>
        public virtual ResourceManager ResourceManager
        {
            get { return resourceManager; }
            set { resourceManager = value; }
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// The profile name used by the host, if any.
        /// </summary>
        private string profile;
        /// <summary>
        /// Gets or sets the profile name used by the host, if any.
        /// </summary>
        public virtual string Profile
        {
            get { return profile; }
            set { profile = value; }
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// The flags that control how the host is created.
        /// </summary>
        private HostCreateFlags hostCreateFlags;
        /// <summary>
        /// Gets or sets the flags that control how the host is created.
        /// </summary>
        public virtual HostCreateFlags HostCreateFlags
        {
            get { return hostCreateFlags; }
            set { hostCreateFlags = value; }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////////////////////////////

        #region System.Object Overrides
        /// <summary>
        /// This method returns a string representation of this host data.
        /// </summary>
        /// <returns>
        /// The name of the host, or an empty string if it has no name.
        /// </returns>
        public override string ToString()
        {
            return (name != null) ? name : String.Empty;
        }
        #endregion
    }
}
