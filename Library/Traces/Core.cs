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
    /// <summary>
    /// This class implements the core variable trace used by the Eagle
    /// engine.  It derives from <see cref="Default" /> and provides a working
    /// implementation of trace execution that dispatches to a delegate
    /// callback, along with the setup logic needed to resolve that callback
    /// from a plugin assembly via reflection.
    /// </summary>
    [ObjectId("2ffd8707-da8b-42fe-82aa-2446e1746e31")]
    public class Core : Default
    {
        #region Public Constructors
        /// <summary>
        /// Constructs an instance of the core variable trace.
        /// </summary>
        /// <param name="traceData">
        /// The data used to create and identify this trace, such as its name
        /// and the plugin type and method that supply its callback.  This
        /// parameter may be null.
        /// </param>
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
        /// <summary>
        /// Executes this trace by invoking its configured delegate callback.
        /// </summary>
        /// <param name="breakpointType">
        /// The kind of variable operation (e.g. read, write, or unset) that
        /// triggered this trace.
        /// </param>
        /// <param name="interpreter">
        /// The interpreter context this trace is executing in.  This parameter
        /// should not be null.
        /// </param>
        /// <param name="traceInfo">
        /// The details of the variable operation that triggered this trace.
        /// </param>
        /// <param name="result">
        /// Upon success, this may contain the result value produced by the
        /// trace callback.  Upon failure, this must contain an appropriate
        /// error message.
        /// </param>
        /// <returns>
        /// The <see cref="ReturnCode" /> produced by the trace callback; if no
        /// callback is configured, <see cref="ReturnCode.Error" /> is
        /// returned.
        /// </returns>
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
        /// <summary>
        /// Prepares this trace for use by resolving its delegate callback from
        /// the configured plugin assembly, type, and method via reflection.
        /// </summary>
        /// <param name="error">
        /// Upon failure, receives an appropriate error message describing why
        /// the callback could not be resolved.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise,
        /// <see cref="ReturnCode.Error" /> with details placed in the
        /// <paramref name="error" /> parameter.
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
