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
        private static string DefaultNormalFormat = "Notify: {0} ==> {1}";
        private static string DefaultDirectFormat = "{0} ==> {1}";

        ///////////////////////////////////////////////////////////////////////

        //
        // HACK: This is purposely not marked as read-only.
        //
        private static string DefaultCategory = typeof(Monitor).FullName;

        ///////////////////////////////////////////////////////////////////////

        //
        // HACK: This is purposely not marked as read-only.
        //
        private static TracePriority DefaultPriority =
            TracePriority.EngineDebug;

        ///////////////////////////////////////////////////////////////////////

        //
        // HACK: These are purposely not marked as read-only.
        //
        private static bool DefaultDisabled = true; // TODO: Good default?
        private static bool DefaultDirect = true; // TODO: Good default?

        ///////////////////////////////////////////////////////////////////////

        //
        // HACK: These are purposely not marked as read-only.
        //
        private static bool DefaultNormalize = true; // TODO: Good default?
        private static bool DefaultEllipsis = true; // TODO: Good default?
        private static bool DefaultQuote = false; // TODO: Good default?
        private static bool DefaultDisplay = true; // TODO: Good default?
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Private Data
        private string normalFormat;
        private string directFormat;

        ///////////////////////////////////////////////////////////////////////

        private string normalCategory;
        private string directCategory;

        ///////////////////////////////////////////////////////////////////////

        private TracePriority normalPriority;

#if MONO_BUILD
#pragma warning disable 414
#endif
        private TracePriority directPriority;
#if MONO_BUILD
#pragma warning restore 414
#endif

        ///////////////////////////////////////////////////////////////////////

        private bool disabled;
        private bool direct;

        ///////////////////////////////////////////////////////////////////////

        private bool normalizeArguments;
        private bool normalizeResult;
        private bool ellipsisArguments;
        private bool ellipsisResult;
        private bool quoteArguments;
        private bool quoteResult;
        private bool displayArguments;
        private bool displayResult;
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Public Constructors
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
        protected override PackageFlags GetPackageFlags()
        {
            //
            // NOTE: We know the package is a core package because this is
            //       the core library and this class is sealed.
            //
            return PackageFlags.Core | base.GetPackageFlags();
        }

        ///////////////////////////////////////////////////////////////////////

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
        public override ReturnCode About(
            Interpreter interpreter,
            ref Result result
            )
        {
            result = FormatOps.PluginAbout(this, false, null);
            return ReturnCode.Ok;
        }

        ///////////////////////////////////////////////////////////////////////

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
