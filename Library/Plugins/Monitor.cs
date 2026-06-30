/*
 * Monitor.cs --
 *
 * Copyright (c) 2007-2012 by Joe Mistachkin.  All rights reserved.
 *
 * See the file "license.terms" for information on usage and redistribution of
 * this file, and for a DISCLAIMER OF ALL WARRANTIES.
 *
 * RCS: @(#) $Id: $
 */

using System;
using System.Collections.Generic;
using System.Globalization;
using Eagle._Attributes;
using Eagle._Components.Private;
using Eagle._Components.Public;
using Eagle._Constants;
using Eagle._Containers.Public;
using Eagle._Interfaces.Public;

namespace Eagle._Plugins
{
    /// <summary>
    /// This class implements a diagnostic plugin that monitors engine
    /// execution.  When notified that the engine has executed something, it
    /// formats the associated arguments and result and writes them to the
    /// tracing subsystem (or directly via the trace listeners in "direct"
    /// mode).  Its formatting and behavior are controlled by a set of instance
    /// fields that may be queried or modified at run-time.
    /// </summary>
    [ObjectId("a276dbd3-6a72-46f5-9c79-4cc42bb34819")]
    [PluginFlags(
        PluginFlags.System | PluginFlags.Notify |
        PluginFlags.Static | PluginFlags.NoCommands |
        PluginFlags.NoFunctions | PluginFlags.NoPolicies |
        PluginFlags.NoTraces)]
    [NotifyTypes(NotifyType.Engine)]
    [NotifyFlags(NotifyFlags.Executed)]
    internal sealed class Monitor : Trace
    {
        #region Private Constants
        //
        // HACK: These are purposely not marked as read-only.
        //
        /// <summary>
        /// The default format string used in "normal" (non-direct) mode.
        /// </summary>
        private static string DefaultNormalFormat = "Notify: {0} ==> {1}";

        /// <summary>
        /// The default format string used in "direct" mode.
        /// </summary>
        private static string DefaultDirectFormat = "{0} ==> {1}";

        ///////////////////////////////////////////////////////////////////////

        //
        // HACK: This is purposely not marked as read-only.
        //
        /// <summary>
        /// The default trace category used for emitted messages.
        /// </summary>
        private static string DefaultCategory = typeof(Monitor).FullName;

        ///////////////////////////////////////////////////////////////////////

        //
        // HACK: This is purposely not marked as read-only.
        //
        /// <summary>
        /// The default trace priority used for emitted messages.
        /// </summary>
        private static TracePriority DefaultPriority =
            TracePriority.EngineDebug;

        ///////////////////////////////////////////////////////////////////////

        //
        // HACK: These are purposely not marked as read-only.
        //
        /// <summary>
        /// The default value indicating whether monitoring is disabled.
        /// </summary>
        private static bool DefaultDisabled = true; // TODO: Good default?

        /// <summary>
        /// The default value indicating whether "direct" mode is used.
        /// </summary>
        private static bool DefaultDirect = true; // TODO: Good default?

        ///////////////////////////////////////////////////////////////////////

        //
        // HACK: These are purposely not marked as read-only.
        //
        /// <summary>
        /// The default value indicating whether formatted text is normalized.
        /// </summary>
        private static bool DefaultNormalize = true; // TODO: Good default?

        /// <summary>
        /// The default value indicating whether formatted text is truncated
        /// with an ellipsis when it is too long.
        /// </summary>
        private static bool DefaultEllipsis = true; // TODO: Good default?

        /// <summary>
        /// The default value indicating whether formatted text is quoted.
        /// </summary>
        private static bool DefaultQuote = false; // TODO: Good default?

        /// <summary>
        /// The default value indicating whether formatted text is made
        /// suitable for display.
        /// </summary>
        private static bool DefaultDisplay = true; // TODO: Good default?
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Private Data
        /// <summary>
        /// The format string used to emit messages in "normal" mode.
        /// </summary>
        private string normalFormat;

        /// <summary>
        /// The format string used to emit messages in "direct" mode.
        /// </summary>
        private string directFormat;

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// The trace category used for messages emitted in "normal" mode.
        /// </summary>
        private string normalCategory;

        /// <summary>
        /// The trace category used for messages emitted in "direct" mode.
        /// </summary>
        private string directCategory;

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// The trace priority used for messages emitted in "normal" mode.
        /// </summary>
        private TracePriority normalPriority;

        /// <summary>
        /// The trace priority used for messages emitted in "direct" mode.
        /// </summary>
#if MONO_BUILD
#pragma warning disable 414
#endif
        private TracePriority directPriority;
#if MONO_BUILD
#pragma warning restore 414
#endif

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// When true, monitoring is disabled and no messages are emitted.
        /// </summary>
        private bool disabled;

        /// <summary>
        /// When true, messages are emitted in "direct" mode, bypassing the
        /// normal tracing subsystem.
        /// </summary>
        private bool direct;

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// When true, the arguments text is normalized before being formatted.
        /// </summary>
        private bool normalizeArguments;

        /// <summary>
        /// When true, the result text is normalized before being formatted.
        /// </summary>
        private bool normalizeResult;

        /// <summary>
        /// When true, the arguments text is truncated with an ellipsis when it
        /// is too long.
        /// </summary>
        private bool ellipsisArguments;

        /// <summary>
        /// When true, the result text is truncated with an ellipsis when it is
        /// too long.
        /// </summary>
        private bool ellipsisResult;

        /// <summary>
        /// When true, the arguments text is quoted before being formatted.
        /// </summary>
        private bool quoteArguments;

        /// <summary>
        /// When true, the result text is quoted before being formatted.
        /// </summary>
        private bool quoteResult;

        /// <summary>
        /// When true, the arguments text is made suitable for display.
        /// </summary>
        private bool displayArguments;

        /// <summary>
        /// When true, the result text is made suitable for display.
        /// </summary>
        private bool displayResult;
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Public Constructors
        /// <summary>
        /// Constructs an instance of the monitor plugin, merging the plugin
        /// flags declared via attributes on this type and its base type.
        /// </summary>
        /// <param name="pluginData">
        /// The data used to create and identify this plugin, such as its name
        /// and flags.  This parameter may be null.
        /// </param>
        public Monitor(
            IPluginData pluginData
            )
            : base(pluginData)
        {
            this.Flags |= AttributeOps.GetPluginFlags(GetType().BaseType) |
                AttributeOps.GetPluginFlags(this);
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Protected Methods
        /// <summary>
        /// This method returns the package flags associated with this plugin.
        /// Since this is the core library and this class is sealed, the
        /// resulting flags always include <see cref="PackageFlags.Core" />.
        /// </summary>
        /// <returns>
        /// The package flags for this plugin.
        /// </returns>
        protected override PackageFlags GetPackageFlags()
        {
            //
            // NOTE: We know the package is a core package because this is
            //       the core library and this class is sealed.
            //
            return PackageFlags.Core | base.GetPackageFlags();
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method returns the names of the instance fields of this class
        /// that may be read or written via the request-handling mechanism.
        /// </summary>
        /// <returns>
        /// The array of field names.
        /// </returns>
        protected override string[] GetRequestFieldNames()
        {
            //
            // NOTE: This is the list of instance fields of this class that
            //       may be read or written via the IExecuteRequest.Execute
            //       method.  Currently, all field in this list must have a
            //       type of string or boolean.
            //
            return new string[] {
                "normalFormat",       // string, may not be null
                "directFormat",       // string, may not be null
                "normalCategory",     // string, may be null
                "directCategory",     // string, may be null
                "normalPriority",     // TracePriority
                "directPriority",     // TracePriority
                "disabled",           // boolean
                "direct",             // boolean
                "normalizeArguments", // boolean
                "normalizeResult",    // boolean
                "ellipsisArguments",  // boolean
                "ellipsisResult",     // boolean
                "quoteArguments",     // boolean
                "quoteResult",        // boolean
                "displayArguments",   // boolean
                "displayResult"       // boolean
            };
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method returns the default values for the request-handling
        /// fields, in the same order as the names returned by
        /// <see cref="GetRequestFieldNames" />.
        /// </summary>
        /// <returns>
        /// The array of default field values.
        /// </returns>
        protected override object[] GetRequestFieldValues()
        {
            //
            // NOTE: Since the String.Format method does *NOT* permit the
            //       format parameter to be null, fallback to the default
            //       format strings for those cases.
            //
            // NOTE: Since the DebugOps.TraceWriteLine method permits the
            //       category parameter to be null (or any other string),
            //       there is no need to force a non-null default here.
            //
            return new object[] {
                DefaultNormalFormat, // normalFormat
                DefaultDirectFormat, // directFormat
                null,                // normalCategory
                null,                // directCategory
                null,                // normalPriority
                null,                // directPriority
                null,                // disabled
                null,                // direct
                null,                // normalizeArguments
                null,                // normalizeResult
                null,                // ellipsisArguments
                null,                // ellipsisResult
                null,                // quoteArguments
                null,                // quoteResult
                null,                // displayArguments
                null                 // displayResult
            };
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method resets all of the request-handling fields of this
        /// plugin to their default values.
        /// </summary>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success.
        /// </returns>
        protected override ReturnCode UseDefaultRequestFieldValues()
        {
            normalFormat = DefaultNormalFormat;
            directFormat = DefaultDirectFormat;

            ///////////////////////////////////////////////////////////////////

            normalCategory = DefaultCategory;
            directCategory = DefaultCategory;

            ///////////////////////////////////////////////////////////////////

            normalPriority = DefaultPriority;
            directPriority = DefaultPriority;

            ///////////////////////////////////////////////////////////////////

            disabled = DefaultDisabled;
            direct = DefaultDirect;

            ///////////////////////////////////////////////////////////////////

            normalizeArguments = DefaultNormalize;
            normalizeResult = DefaultNormalize;
            ellipsisArguments = DefaultEllipsis;
            ellipsisResult = DefaultEllipsis;
            quoteArguments = DefaultQuote;
            quoteResult = DefaultQuote;
            displayArguments = DefaultDisplay;
            displayResult = DefaultDisplay;

            ///////////////////////////////////////////////////////////////////

            return ReturnCode.Ok;
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Private Methods
        /// <summary>
        /// This method clears the monitor plugin token stored in the
        /// interpreter state, ensuring it is reset when this plugin is
        /// terminated.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter context whose monitor plugin token is reset.  This
        /// parameter may be null.
        /// </param>
        private void ResetToken(
            Interpreter interpreter
            )
        {
            //
            // HACK: Cleanup the monitor plugin token in the interpreter
            //       state because this is the only place where we can
            //       be 100% sure it will get done.
            //
            if (interpreter == null)
                return;

            interpreter.InternalMonitorPluginToken = 0;
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region IState Members
        /// <summary>
        /// This method is called when the plugin is being terminated within
        /// the specified interpreter.  It resets the monitor plugin token
        /// before delegating to the base implementation.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter context this plugin is being terminated in.  This
        /// parameter may be null.
        /// </param>
        /// <param name="clientData">
        /// The extra, plugin-specific data supplied for this operation, if
        /// any.  This parameter may be null.
        /// </param>
        /// <param name="result">
        /// Upon success, this may contain an informational result.  Upon
        /// failure, this must contain an appropriate error message.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise, a non-Ok value
        /// with details placed in the <paramref name="result" /> parameter.
        /// </returns>
        public override ReturnCode Terminate(
            Interpreter interpreter,
            IClientData clientData,
            ref Result result
            )
        {
            ResetToken(interpreter);

            return base.Terminate(interpreter, clientData, ref result);
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region IExecuteRequest Members
        /// <summary>
        /// This method handles a request directed at this plugin.  It supports
        /// resetting the request-handling fields to their defaults and getting
        /// or setting individual field values; any request it does not
        /// recognize is delegated to the base implementation.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter context this request is being performed in.  This
        /// parameter may be null.
        /// </param>
        /// <param name="clientData">
        /// The extra, plugin-specific data supplied for this request, if any.
        /// This parameter may be null.
        /// </param>
        /// <param name="request">
        /// The request to handle.  This parameter may be null.
        /// </param>
        /// <param name="response">
        /// Upon success, this contains the response produced for the request,
        /// if any.
        /// </param>
        /// <param name="error">
        /// Upon failure, this contains an appropriate error message.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise, a non-Ok value
        /// with details placed in the <paramref name="error" /> parameter.
        /// </returns>
        public override ReturnCode Execute(
            Interpreter interpreter,
            IClientData clientData,
            object request,
            ref object response,
            ref Result error
            )
        {
            //
            // NOTE: This method is not supposed to raise an error under
            //       normal conditions when faced with an unrecognized
            //       request.  It simply does nothing and lets the base
            //       plugin handle it.
            //
            if (request is string[])
            {
                string[] stringRequest = (string[])request;

                ArgumentList arguments = new ArgumentList(
                    (IEnumerable<string>)stringRequest);

                if (RuntimeOps.MatchFieldNameOnly(
                        arguments, "useDefaultSettings"))
                {
                    response = UseDefaultRequestFieldValues();
                    return ReturnCode.Ok;
                }

                CultureInfo cultureInfo = null;

                if (interpreter != null)
                    cultureInfo = interpreter.InternalCultureInfo;

                bool done;

                if (RuntimeOps.MaybeGetOrSetFieldValue(
                        interpreter, GetRequestFields(), this,
                        arguments, cultureInfo, ref response,
                        out done, ref error) != ReturnCode.Ok)
                {
                    return ReturnCode.Error;
                }

                if (done)
                    return ReturnCode.Ok;
            }

            //
            // NOTE: If this point is reached the request was not handled.
            //       Call our base plugin and let it attempt to handle the
            //       request.
            //
            return base.Execute(
                interpreter, clientData, request, ref response, ref error);
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region INotify Members
        /// <summary>
        /// This method is called by the engine to deliver a notification to
        /// this plugin.  When monitoring is enabled and the notification
        /// indicates that the engine has executed something, it formats the
        /// associated arguments and result and emits them via the tracing
        /// subsystem (or directly, in "direct" mode).
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter context this plugin is associated with.  This
        /// parameter may be null.
        /// </param>
        /// <param name="eventArgs">
        /// The event arguments describing the notification, including its
        /// types, flags, arguments, and result.  This parameter may be null.
        /// </param>
        /// <param name="clientData">
        /// The extra, plugin-specific data supplied for this notification, if
        /// any.  This parameter may be null.
        /// </param>
        /// <param name="arguments">
        /// The arguments associated with the notification, if any.  This
        /// parameter may be null.
        /// </param>
        /// <param name="result">
        /// Upon failure, this contains an appropriate error message.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise, a non-Ok value
        /// with details placed in the <paramref name="result" /> parameter.
        /// </returns>
        public override ReturnCode Notify(
            Interpreter interpreter,
            IScriptEventArgs eventArgs,
            IClientData clientData,
            ArgumentList arguments,
            ref Result result
            )
        {
            //
            // NOTE: If we are disabled -OR- there are no event arguments -OR-
            //       this event does not match the kind we are interested in
            //       then just return "success" now.
            //
            if (disabled || (eventArgs == null) ||
                !FlagOps.HasFlags(
                    eventArgs.NotifyTypes, NotifyType.Engine, false) ||
                !FlagOps.HasFlags(
                    eventArgs.NotifyFlags, NotifyFlags.Executed, false))
            {
                return ReturnCode.Ok;
            }

            //
            // NOTE: In "direct" mode, skip [almost] all the tracing ceremony
            //       and just call into Trace.WriteLine().  Otherwise, use the
            //       TraceOps class and all its special handling.  Either way,
            //       figure out the String.Format() arguments ahead of time,
            //       based on our current "normalize" and "ellipsis" settings.
            //
            try
            {
                string arg0 = FormatOps.WrapTraceOrNull(
                    normalizeArguments, ellipsisArguments, quoteArguments,
                    displayArguments, eventArgs.Arguments);

                string arg1 = FormatOps.WrapTraceOrNull(
                    normalizeResult, ellipsisResult, quoteResult,
                    displayResult, eventArgs.Result);

                if (direct)
                {
                    //
                    // NOTE: This is just an extremely thin wrapper around
                    //       the Trace.WriteLine method.  This prevents any
                    //       trace priority or category checking.  Also, it
                    //       ignores the enabled/disabled state of the core
                    //       library tracing subsystem.
                    //
                    DebugOps.TraceWriteLine(String.Format( /* EXEMPT */
                        directFormat, arg0, arg1), directCategory);
                }
                else
                {
                    //
                    // NOTE: Use the (normal) tracing subsystem used by the
                    //       core library.
                    //
                    TraceOps.DebugTrace(String.Format(
                        normalFormat, arg0, arg1), normalCategory,
                        normalPriority);
                }

                return ReturnCode.Ok;
            }
            catch (Exception e)
            {
                TraceOps.DebugTrace(
                    e, typeof(Monitor).Name,
                    TracePriority.EngineError);

                result = e;
                return ReturnCode.Error;
            }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region IPlugin Members
        /// <summary>
        /// This method returns descriptive "about" information for this
        /// plugin, such as its name, version, and copyright.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter context this operation is being performed in.  This
        /// parameter may be null.
        /// </param>
        /// <param name="result">
        /// Upon success, this contains the formatted "about" information.  Upon
        /// failure, this contains an appropriate error message.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise, a non-Ok value
        /// with details placed in the <paramref name="result" /> parameter.
        /// </returns>
        public override ReturnCode About(
            Interpreter interpreter,
            ref Result result
            )
        {
            result = FormatOps.PluginAbout(this, false, null);
            return ReturnCode.Ok;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method returns the list of compile-time options that this
        /// plugin (i.e. the core library) was built with.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter context this operation is being performed in.  This
        /// parameter may be null.
        /// </param>
        /// <param name="result">
        /// Upon success, this contains the list of options.  Upon failure,
        /// this contains an appropriate error message.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise, a non-Ok value
        /// with details placed in the <paramref name="result" /> parameter.
        /// </returns>
        public override ReturnCode Options(
            Interpreter interpreter,
            ref Result result
            )
        {
            result = new StringList(DefineConstants.OptionList, false);
            return ReturnCode.Ok;
        }
        #endregion
    }
}
