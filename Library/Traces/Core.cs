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
using Eagle._Interfaces.Public;

namespace Eagle._Traces
{
    [ObjectId("2ffd8707-da8b-42fe-82aa-2446e1746e31")]
    public class Core : Default
    {
        #region Public Constructors
        public Core(
            ITraceData traceData
            )
            : base(traceData)
        {
            // do nothing.
        }
        #endregion

        ////////////////////////////////////////////////////////////////////////

        #region IExecuteTrace Members
        public override ReturnCode Execute(
            BreakpointType breakpointType,
            Interpreter interpreter,
            ITraceInfo traceInfo,
            ref Result result
            )
        {
            TraceCallback callback = this.Callback;

            if (callback != null)
                return callback(breakpointType, interpreter, traceInfo, ref result);
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
                                    typeof(TraceCallback), null, methodInfo,
                                    false) as TraceCallback;

                                return ReturnCode.Ok;
                            }
                            else
                            {
                                error = "cannot get method from trace type";
                            }
                        }
                        else
                        {
                            error = "cannot get trace type from plugin assembly";
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
