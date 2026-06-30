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
using System.Reflection;
using Eagle._Attributes;
using Eagle._Components.Private;
using Eagle._Components.Public;
using Eagle._Components.Public.Delegates;
using Eagle._Containers.Public;
using Eagle._Interfaces.Public;

namespace Eagle._Policies
{
    /// <summary>
    /// This class provides the default implementation of the
    /// <see cref="IPolicy" /> interface and serves as the base class for the
    /// Eagle policies.  It stores the common identification and configuration
    /// data for a policy (including the type and method that implement its
    /// decision logic) and provides default implementations of policy execution
    /// and setup that always succeed without taking any action.
    /// </summary>
    [ObjectId("45dd5294-ff31-47a2-b450-61d34c4184fb")]
    public class Default :
#if ISOLATED_INTERPRETERS || ISOLATED_PLUGINS
        ScriptMarshalByRefObject,
#endif
        IPolicy
    {
        #region Public Constructors
        /// <summary>
        /// Constructs an instance of the default policy, optionally
        /// initializing it from the specified policy metadata.
        /// </summary>
        /// <param name="policyData">
        /// The data used to create and identify this policy, such as its type
        /// name, method name, binding flags, and policy flags.  This parameter
        /// may be null.
        /// </param>
        public Default(
            IPolicyData policyData
            )
        {
            kind = IdentifierKind.Policy;

            if ((policyData == null) ||
                !FlagOps.HasFlags(policyData.PolicyFlags,
                    PolicyFlags.NoAttributes, true))
            {
                id = AttributeOps.GetObjectId(this);
                group = AttributeOps.GetObjectGroups(this);
            }

            if (policyData != null)
            {
                id = policyData.Id;

                EntityOps.MaybeSetupId(this);

                EntityOps.MaybeSetGroup(
                    this, policyData.Group);

                name = policyData.Name;
                description = policyData.Description;
                typeName = policyData.TypeName;
                methodName = policyData.MethodName;
                bindingFlags = policyData.BindingFlags;
                methodFlags = policyData.MethodFlags;
                policyFlags = policyData.PolicyFlags;
                token = policyData.Token;
                plugin = policyData.Plugin;
                clientData = policyData.ClientData;
            }

            callback = null;
        }
        #endregion

        ////////////////////////////////////////////////////////////////////////

        #region System.Object Overrides
        /// <summary>
        /// Returns a string representation of this policy, consisting of its
        /// type name and, when available, its name.
        /// </summary>
        /// <returns>
        /// A string that represents this policy.
        /// </returns>
        public override string ToString()
        {
            return (name != null) ?
                StringList.MakeList(FormatOps.RawTypeName(GetType()), name) :
                base.ToString();
        }
        #endregion

        ////////////////////////////////////////////////////////////////////////

        #region IIdentifierName Members
        /// <summary>
        /// The name of this policy.
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

        ////////////////////////////////////////////////////////////////////////

        #region IIdentifierBase Members
        /// <summary>
        /// The kind of identifier represented by this policy.
        /// </summary>
        private IdentifierKind kind;
        /// <summary>
        /// Gets or sets the kind of identifier represented by this policy.
        /// </summary>
        public virtual IdentifierKind Kind
        {
            get { return kind; }
            set { kind = value; }
        }

        ////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// The unique identifier (GUID) of this policy.
        /// </summary>
        private Guid id;
        /// <summary>
        /// Gets or sets the unique identifier (GUID) of this policy.
        /// </summary>
        public virtual Guid Id
        {
            get { return id; }
            set { id = value; }
        }
        #endregion

        ////////////////////////////////////////////////////////////////////////

        #region IGetClientData / ISetClientData Members
        /// <summary>
        /// The extra data associated with this policy, if any.
        /// </summary>
        private IClientData clientData;
        /// <summary>
        /// Gets or sets the extra data associated with this policy, if any.
        /// </summary>
        public virtual IClientData ClientData
        {
            get { return clientData; }
            set { clientData = value; }
        }
        #endregion

        ////////////////////////////////////////////////////////////////////////

        #region IIdentifier Members
        /// <summary>
        /// The object group that this policy belongs to, if any.
        /// </summary>
        private string group;
        /// <summary>
        /// Gets or sets the object group that this policy belongs to, if any.
        /// </summary>
        public virtual string Group
        {
            get { return group; }
            set { group = value; }
        }

        ////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// The human-readable description of this policy, if any.
        /// </summary>
        private string description;
        /// <summary>
        /// Gets or sets the human-readable description of this policy, if any.
        /// </summary>
        public virtual string Description
        {
            get { return description; }
            set { description = value; }
        }
        #endregion

        ////////////////////////////////////////////////////////////////////////

        #region IHavePlugin Members
        /// <summary>
        /// The plugin that owns this policy, if any.
        /// </summary>
        private IPlugin plugin;
        /// <summary>
        /// Gets or sets the plugin that owns this policy, if any.
        /// </summary>
        public virtual IPlugin Plugin
        {
            get { return plugin; }
            set { plugin = value; }
        }
        #endregion

        ////////////////////////////////////////////////////////////////////////

        #region ITypeAndName Members
        /// <summary>
        /// The name of the type that implements this policy.
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

        ////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// The type that implements this policy.
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

        ////////////////////////////////////////////////////////////////////////

        #region IPolicyData Members
        /// <summary>
        /// The name of the method that implements this policy.
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

        ////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// The reflection binding flags used to locate the policy method.
        /// </summary>
        private BindingFlags bindingFlags;
        /// <summary>
        /// Gets or sets the reflection binding flags used to locate the policy
        /// method.
        /// </summary>
        public virtual BindingFlags BindingFlags
        {
            get { return bindingFlags; }
            set { bindingFlags = value; }
        }

        ////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// The method flags associated with the policy method.
        /// </summary>
        private MethodFlags methodFlags;
        /// <summary>
        /// Gets or sets the method flags associated with the policy method.
        /// </summary>
        public virtual MethodFlags MethodFlags
        {
            get { return methodFlags; }
            set { methodFlags = value; }
        }

        ////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// The flags that control the behavior of this policy.
        /// </summary>
        private PolicyFlags policyFlags;
        /// <summary>
        /// Gets or sets the flags that control the behavior of this policy.
        /// </summary>
        public virtual PolicyFlags PolicyFlags
        {
            get { return policyFlags; }
            set { policyFlags = value; }
        }
        #endregion

        ////////////////////////////////////////////////////////////////////////

        #region IWrapperData Members
        /// <summary>
        /// The token that identifies this policy within the interpreter.
        /// </summary>
        private long token;
        /// <summary>
        /// Gets or sets the token that identifies this policy within the
        /// interpreter.
        /// </summary>
        public virtual long Token
        {
            get { return token; }
            set { token = value; }
        }
        #endregion

        ////////////////////////////////////////////////////////////////////////

        #region IDynamicExecuteCallback Members
        /// <summary>
        /// The delegate invoked to evaluate this policy.
        /// </summary>
        private ExecuteCallback callback;
        /// <summary>
        /// Gets or sets the delegate invoked to evaluate this policy.
        /// </summary>
        public virtual ExecuteCallback Callback
        {
            get { return callback; }
            set { callback = value; }
        }
        #endregion

        ////////////////////////////////////////////////////////////////////////

        #region IExecute Members
        /// <summary>
        /// Evaluates this policy.  This default implementation takes no action
        /// and always succeeds.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter context this policy is executing in.  This parameter
        /// may be null.
        /// </param>
        /// <param name="clientData">
        /// The extra, policy-specific data supplied when the policy was
        /// created, if any.  This parameter may be null.
        /// </param>
        /// <param name="arguments">
        /// The list of arguments describing the operation being checked.  This
        /// parameter may be null.
        /// </param>
        /// <param name="result">
        /// Upon success, this may contain the result value produced by the
        /// policy.  Upon failure, this may contain an appropriate error
        /// message.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> in all cases.
        /// </returns>
        public virtual ReturnCode Execute(
            Interpreter interpreter,
            IClientData clientData,
            ArgumentList arguments,
            ref Result result
            )
        {
            return ReturnCode.Ok;
        }
        #endregion

        ////////////////////////////////////////////////////////////////////////

        #region ISetup Members
        /// <summary>
        /// Prepares this policy for use.  This default implementation takes no
        /// action and always succeeds.
        /// </summary>
        /// <param name="error">
        /// Upon failure, this will contain an appropriate error message.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> in all cases.
        /// </returns>
        public virtual ReturnCode Setup(
            ref Result error
            )
        {
            return ReturnCode.Ok;
        }
        #endregion
    }
}
