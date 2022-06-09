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
    [ObjectId("6a34a41f-689c-41a8-b00c-70c43e6c3167")]
    public class Core : Default
    {
        #region Public Constructors
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
