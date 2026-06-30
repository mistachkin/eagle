/*
 * Policy.cs --
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
using Eagle._Components.Public;
using Eagle._Components.Public.Delegates;
using Eagle._Interfaces.Public;

namespace Eagle._Wrappers
{
    /// <summary>
    /// This class wraps an <see cref="IPolicy" /> instance, forwarding all
    /// member access to the wrapped target while gracefully handling the case
    /// where no target has been set (by returning default values or an error
    /// result).
    /// </summary>
    [ObjectId("f23e646a-0766-4bd5-b770-4a307df76792")]
    internal sealed class Policy : Core, IPolicy
    {
        #region Public Constructors
        /// <summary>
        /// Constructs an instance of this class with no wrapped policy target.
        /// </summary>
        public Policy()
            : base()
        {
            // do nothing.
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Private Data
        /// <summary>
        /// The wrapped policy instance that all members forward to.
        /// </summary>
        internal IPolicy policy;
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region IIdentifierName Members
        /// <summary>
        /// Gets or sets the name of the wrapped policy.
        /// </summary>
        public string Name
        {
            get { return (policy != null) ? policy.Name : null; }
            set { if (policy != null) { policy.Name = value; } }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region IIdentifierBase Members
        /// <summary>
        /// Gets or sets the identifier kind of the wrapped policy.
        /// </summary>
        public IdentifierKind Kind
        {
            get { return (policy != null) ? policy.Kind : IdentifierKind.None; }
            set { if (policy != null) { policy.Kind = value; } }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Gets or sets the unique identifier of the wrapped policy.
        /// </summary>
        public Guid Id
        {
            get { return (policy != null) ? policy.Id : Guid.Empty; }
            set { if (policy != null) { policy.Id = value; } }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region IGetClientData / ISetClientData Members
        /// <summary>
        /// Gets or sets the client data associated with the wrapped policy.
        /// </summary>
        public IClientData ClientData
        {
            get { return (policy != null) ? policy.ClientData : null; }
            set { if (policy != null) { policy.ClientData = value; } }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region IIdentifier Members
        /// <summary>
        /// Gets or sets the group of the wrapped policy.
        /// </summary>
        public string Group
        {
            get { return (policy != null) ? policy.Group : null; }
            set { if (policy != null) { policy.Group = value; } }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Gets or sets the description of the wrapped policy.
        /// </summary>
        public string Description
        {
            get { return (policy != null) ? policy.Description : null; }
            set { if (policy != null) { policy.Description = value; } }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region IDynamicExecuteCallback Members
        /// <summary>
        /// Gets or sets the execute callback of the wrapped policy.
        /// </summary>
        public ExecuteCallback Callback
        {
            get { return (policy != null) ? policy.Callback : null; }
            set { if (policy != null) { policy.Callback = value; } }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region IHavePlugin Members
        /// <summary>
        /// Gets or sets the plugin that owns the wrapped policy.
        /// </summary>
        public IPlugin Plugin
        {
            get { return (policy != null) ? policy.Plugin : null; }
            set { if (policy != null) { policy.Plugin = value; } }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region ITypeAndName Members
        /// <summary>
        /// Gets or sets the type name of the wrapped policy.
        /// </summary>
        public string TypeName
        {
            get { return (policy != null) ? policy.TypeName : null; }
            set { if (policy != null) { policy.TypeName = value; } }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Gets or sets the type of the wrapped policy.
        /// </summary>
        public Type Type
        {
            get { return (policy != null) ? policy.Type : null; }
            set { if (policy != null) { policy.Type = value; } }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region IPolicyData Members
        /// <summary>
        /// Gets or sets the method name of the wrapped policy.
        /// </summary>
        public string MethodName
        {
            get { return (policy != null) ? policy.MethodName : null; }
            set { if (policy != null) { policy.MethodName = value; } }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Gets or sets the reflection binding flags used to locate the policy
        /// method of the wrapped policy.
        /// </summary>
        public BindingFlags BindingFlags
        {
            get { return (policy != null) ? policy.BindingFlags : BindingFlags.Default; }
            set { if (policy != null) { policy.BindingFlags = value; } }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Gets or sets the method flags of the wrapped policy.
        /// </summary>
        public MethodFlags MethodFlags
        {
            get { return (policy != null) ? policy.MethodFlags : MethodFlags.None; }
            set { if (policy != null) { policy.MethodFlags = value; } }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Gets or sets the policy flags of the wrapped policy.
        /// </summary>
        public PolicyFlags PolicyFlags
        {
            get { return (policy != null) ? policy.PolicyFlags : PolicyFlags.None; }
            set { if (policy != null) { policy.PolicyFlags = value; } }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region ISetup Members
        /// <summary>
        /// This method performs any setup required by the wrapped policy.
        /// </summary>
        /// <param name="error">
        /// Upon failure, receives an appropriate error message.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise, an error code.
        /// </returns>
        public ReturnCode Setup(
            ref Result error
            )
        {
            if (policy == null)
            {
                error = "invalid wrapper target";
                return ReturnCode.Error;
            }

            return policy.Setup(ref error);
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region IWrapper Members
        /// <summary>
        /// Gets a value indicating whether the wrapped object is disposable;
        /// always false for this wrapper.
        /// </summary>
        public override bool IsDisposable
        {
            get { return false; }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Gets or sets the wrapped object.  The value must implement both
        /// <see cref="IPolicy" /> and <see cref="IExecute" />; otherwise,
        /// setting it throws.
        /// </summary>
        public override object Object
        {
            get { return policy; }
            set
            {
                policy = (IPolicy)value; /* throw */
                execute = (IExecute)value; /* throw */
            }
        }
        #endregion
    }
}
