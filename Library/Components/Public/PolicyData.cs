/*
 * PolicyData.cs --
 *
 * Copyright (c) 2007-2012 by Joe Mistachkin.  All rights reserved.
 *
 * See the file "license.terms" for information on usage and redistribution of
 * this file, and for a DISCLAIMER OF ALL WARRANTIES.
 *
 * RCS: @(#) $Id: $
 */

using System;
using System.Reflection;
using Eagle._Attributes;
using Eagle._Components.Private;
using Eagle._Interfaces.Public;

namespace Eagle._Components.Public
{
    /// <summary>
    /// This class represents the metadata describing a policy that may be
    /// consulted by an interpreter, including its identity, the plugin that
    /// owns it, the type and method that implement it, the binding and method
    /// flags used to locate and invoke it, and the policy flags that control
    /// when it applies.
    /// </summary>
#if SERIALIZATION
    [Serializable()]
#endif
    [ObjectId("1f22a392-8ec7-4000-86b4-dc34a769b796")]
    public class PolicyData : IPolicyData
    {
        #region Public Constructors
        /// <summary>
        /// Constructs a policy data instance from the fully specified set of
        /// identity, plugin, type, method, flag, and token parameters.
        /// </summary>
        /// <param name="name">
        /// The name of this policy.  This parameter may be null.
        /// </param>
        /// <param name="group">
        /// The group of this policy.  This parameter may be null.
        /// </param>
        /// <param name="description">
        /// The description of this policy.  This parameter may be null.
        /// </param>
        /// <param name="clientData">
        /// The client data associated with this policy, if any.  This parameter
        /// may be null.
        /// </param>
        /// <param name="typeName">
        /// The name of the type that implements this policy.  This parameter may
        /// be null.
        /// </param>
        /// <param name="type">
        /// The type that implements this policy.  This parameter may be null.
        /// </param>
        /// <param name="methodName">
        /// The name of the method that implements this policy.  This parameter
        /// may be null.
        /// </param>
        /// <param name="bindingFlags">
        /// The binding flags used to locate the method that implements this
        /// policy.
        /// </param>
        /// <param name="methodFlags">
        /// The method flags associated with the method that implements this
        /// policy.
        /// </param>
        /// <param name="policyFlags">
        /// The flags controlling when and how this policy applies.
        /// </param>
        /// <param name="plugin">
        /// The plugin that owns this policy, if any.  This parameter may be
        /// null.
        /// </param>
        /// <param name="token">
        /// The token used to identify this policy within the interpreter.
        /// </param>
        public PolicyData(
            string name,
            string group,
            string description,
            IClientData clientData,
            string typeName,
            Type type,
            string methodName,
            BindingFlags bindingFlags,
            MethodFlags methodFlags,
            PolicyFlags policyFlags,
            IPlugin plugin,
            long token
            )
        {
            this.kind = IdentifierKind.PolicyData;
            this.id = AttributeOps.GetObjectId(this);
            this.name = name;
            this.group = group;
            this.description = description;
            this.clientData = clientData;
            this.typeName = typeName;
            this.type = type;
            this.methodName = methodName;
            this.bindingFlags = bindingFlags;
            this.methodFlags = methodFlags;
            this.policyFlags = policyFlags;
            this.plugin = plugin;
            this.token = token;
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////////////////////////////

        #region IIdentifierName Members
        /// <summary>
        /// Stores the name of this policy.
        /// </summary>
        private string name;
        /// <summary>
        /// Gets or sets the name of this policy.
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
        /// Stores the identifier kind of this policy.
        /// </summary>
        private IdentifierKind kind;
        /// <summary>
        /// Gets or sets the identifier kind of this policy.
        /// </summary>
        public virtual IdentifierKind Kind
        {
            get { return kind; }
            set { kind = value; }
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Stores the globally unique identifier of this policy.
        /// </summary>
        private Guid id;
        /// <summary>
        /// Gets or sets the globally unique identifier of this policy.
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
        /// Stores the client data associated with this policy.
        /// </summary>
        private IClientData clientData;
        /// <summary>
        /// Gets or sets the client data associated with this policy.
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
        /// Stores the group of this policy.
        /// </summary>
        private string group;
        /// <summary>
        /// Gets or sets the group of this policy.
        /// </summary>
        public virtual string Group
        {
            get { return group; }
            set { group = value; }
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Stores the description of this policy.
        /// </summary>
        private string description;
        /// <summary>
        /// Gets or sets the description of this policy.
        /// </summary>
        public virtual string Description
        {
            get { return description; }
            set { description = value; }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////////////////////////////

        #region IHavePlugin Members
        /// <summary>
        /// Stores the plugin that owns this policy.
        /// </summary>
        private IPlugin plugin;
        /// <summary>
        /// Gets or sets the plugin that owns this policy.
        /// </summary>
        public virtual IPlugin Plugin
        {
            get { return plugin; }
            set { plugin = value; }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////////////////////////////

        #region ITypeAndName Members
        /// <summary>
        /// Stores the name of the type that implements this policy.
        /// </summary>
        private string typeName;
        /// <summary>
        /// Gets or sets the name of the type that implements this policy.
        /// </summary>
        public virtual string TypeName
        {
            get { return typeName; }
            set { typeName = value; }
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Stores the type that implements this policy.
        /// </summary>
        private Type type;
        /// <summary>
        /// Gets or sets the type that implements this policy.
        /// </summary>
        public virtual Type Type
        {
            get { return type; }
            set { type = value; }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////////////////////////////

        #region IPolicyData Members
        /// <summary>
        /// Stores the name of the method that implements this policy.
        /// </summary>
        private string methodName;
        /// <summary>
        /// Gets or sets the name of the method that implements this policy.
        /// </summary>
        public virtual string MethodName
        {
            get { return methodName; }
            set { methodName = value; }
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Stores the binding flags used to locate the method that implements
        /// this policy.
        /// </summary>
        private BindingFlags bindingFlags;
        /// <summary>
        /// Gets or sets the binding flags used to locate the method that
        /// implements this policy.
        /// </summary>
        public virtual BindingFlags BindingFlags
        {
            get { return bindingFlags; }
            set { bindingFlags = value; }
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Stores the method flags associated with the method that implements
        /// this policy.
        /// </summary>
        private MethodFlags methodFlags;
        /// <summary>
        /// Gets or sets the method flags associated with the method that
        /// implements this policy.
        /// </summary>
        public virtual MethodFlags MethodFlags
        {
            get { return methodFlags; }
            set { methodFlags = value; }
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Stores the flags controlling when and how this policy applies.
        /// </summary>
        private PolicyFlags policyFlags;
        /// <summary>
        /// Gets or sets the flags controlling when and how this policy applies.
        /// </summary>
        public virtual PolicyFlags PolicyFlags
        {
            get { return policyFlags; }
            set { policyFlags = value; }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////////////////////////////

        #region IWrapperData Members
        /// <summary>
        /// Stores the token used to identify this policy within the
        /// interpreter.
        /// </summary>
        private long token;
        /// <summary>
        /// Gets or sets the token used to identify this policy within the
        /// interpreter.
        /// </summary>
        public virtual long Token
        {
            get { return token; }
            set { token = value; }
        }
        #endregion
    }
}
