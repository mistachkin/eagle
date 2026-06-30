/*
 * Core.cs --
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
using Eagle._Containers.Public;
using Eagle._Interfaces.Public;

namespace Eagle._Policies
{
    /// <summary>
    /// This class implements the core policy used by the Eagle library to make
    /// security-related decisions about whether a given operation is permitted.
    /// It derives from <see cref="Default" /> and dispatches policy checks to
    /// an <see cref="ExecuteCallback" /> delegate, which is bound at setup time
    /// to a method discovered via reflection on the policy's owning plugin.
    /// </summary>
    [ObjectId("6a34a41f-689c-41a8-b00c-70c43e6c3167")]
    public class Core : Default
    {
        #region Public Constructors
        /// <summary>
        /// Constructs an instance of the core policy using the specified policy
        /// metadata.
        /// </summary>
        /// <param name="policyData">
        /// The data used to create and identify this policy, such as its type
        /// name, method name, and flags.  This parameter may be null.
        /// </param>
        public Core(
            IPolicyData policyData
            )
            : base(policyData)
        {
            // do nothing.
        }
        #endregion

        ////////////////////////////////////////////////////////////////////////

        #region IExecute Members
        /// <summary>
        /// Evaluates this policy by invoking its configured callback with the
        /// supplied arguments.
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
        /// policy callback.  Upon failure, this may contain an appropriate
        /// error message.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise, a non-Ok value
        /// (e.g. <see cref="ReturnCode.Error" /> when no callback has been
        /// configured).
        /// </returns>
        public override ReturnCode Execute(
            Interpreter interpreter,
            IClientData clientData,
            ArgumentList arguments,
            ref Result result
            )
        {
            ExecuteCallback callback = this.Callback;

            if (callback != null)
                return callback(interpreter, clientData, arguments, ref result);
            else
                return ReturnCode.Error;
        }
        #endregion

        ////////////////////////////////////////////////////////////////////////

        #region ISetup Members
        /// <summary>
        /// Prepares this policy for use by locating its target method via
        /// reflection on the owning plugin's assembly and binding it as the
        /// policy callback.
        /// </summary>
        /// <param name="error">
        /// Upon failure, this will contain an appropriate error message.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise, a non-Ok value
        /// with details placed in the <paramref name="error" /> parameter.
        /// </returns>
        public override ReturnCode Setup(
            ref Result error
            )
        {
            try
            {
                IPluginData pluginData = this.Plugin;

                if (pluginData != null)
                {
                    Assembly assembly = pluginData.Assembly;

                    if (assembly != null)
                    {
                        Type type = assembly.GetType(
                            this.TypeName, true, false); /* throw */

                        if (type != null)
                        {
                            MethodInfo methodInfo = type.GetMethod(
                                this.MethodName, this.BindingFlags); /* throw */

                            if (methodInfo != null)
                            {
                                this.Callback = Delegate.CreateDelegate(
                                    typeof(ExecuteCallback), null, methodInfo,
                                    false) as ExecuteCallback;

                                return ReturnCode.Ok;
                            }
                            else
                            {
                                error = "cannot get method from policy type";
                            }
                        }
                        else
                        {
                            error = "cannot get policy type from plugin assembly";
                        }
                    }
                    else
                    {
                        error = "plugin data has invalid assembly";
                    }
                }
                else
                {
                    error = "invalid plugin data";
                }
            }
            catch (Exception e)
            {
                error = e;
            }

            return ReturnCode.Error;
        }
        #endregion
    }
}
