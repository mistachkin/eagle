/*
 * Object.cs --
 *
 * Copyright (c) 2007-2012 by Joe Mistachkin.  All rights reserved.
 *
 * See the file "license.terms" for information on usage and redistribution of
 * this file, and for a DISCLAIMER OF ALL WARRANTIES.
 *
 * RCS: @(#) $Id: $
 */

using System.Collections.Generic;
using Eagle._Attributes;
using Eagle._Components.Private;
using Eagle._Components.Public;
using Eagle._Constants;
using Eagle._Containers.Public;
using Eagle._Interfaces.Public;

namespace Eagle._Plugins
{
    [ObjectId("55febaed-b731-4d2c-9176-4144a7550011")]
    [PluginFlags(
        PluginFlags.System | PluginFlags.Notify |
        PluginFlags.Static | PluginFlags.NoCommands |
        PluginFlags.NoFunctions | PluginFlags.NoPolicies |
        PluginFlags.NoTraces)]
    [NotifyTypes(NotifyType.CallFrame)]
    [NotifyFlags(NotifyFlags.Popped | NotifyFlags.Deleted)]
    internal sealed class Object : Notify
    {
        #region Public Constructors
        public Object(
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
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Private Methods
        private void ResetToken(
            Interpreter interpreter
            )
        {
            //
            // HACK: Cleanup the object plugin token in the interpreter
            //       state because this is the only place where we can
            //       be 100% sure it will get done.
            //
            if (interpreter == null)
                return;

            interpreter.InternalObjectPluginToken = 0;
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

        #region INotify Members
        public override ReturnCode Notify(
            Interpreter interpreter,
            IScriptEventArgs eventArgs,
            IClientData clientData,
            ArgumentList arguments,
            ref Result result
            )
        {
            if (eventArgs == null)
                return ReturnCode.Ok;

            if (!FlagOps.HasFlags(
                    eventArgs.NotifyTypes, NotifyType.CallFrame, false))
            {
                return ReturnCode.Ok;
            }

            NotifyFlags notifyFlags = eventArgs.NotifyFlags;

            if (!FlagOps.HasFlags(notifyFlags,
                    NotifyFlags.Popped | NotifyFlags.Deleted, false))
            {
                return ReturnCode.Ok;
            }

            IClientData eventClientData = eventArgs.ClientData;

            if (eventClientData == null)
                return ReturnCode.Ok;

            ICallFrame newFrame = eventClientData.Data as ICallFrame;

            if (newFrame == null)
                return ReturnCode.Ok;

            //
            // NOTE: Make sure the variables in this frame actually BELONG
            //       to this frame.  Also, we do not handle the global call
            //       frame.
            //
            if (!FlagOps.HasFlags(notifyFlags, NotifyFlags.Force, true) &&
                !CallFrameOps.IsNonGlobalVariable(newFrame))
            {
                return ReturnCode.Ok;
            }

            //
            // NOTE: If this is a [scope] created call frame, we do NOT want
            //       to change any reference counts unless the call frame is
            //       being deleted, not simply popped.
            //
            if (!FlagOps.HasFlags(notifyFlags, NotifyFlags.Deleted, true) &&
                CallFrameOps.IsScope(newFrame))
            {
                return ReturnCode.Ok;
            }

            //
            // NOTE: Grab the variables for this call frame.  If there are
            //       none, we are done.
            //
            VariableDictionary variables = newFrame.Variables;

            if (variables == null)
                return ReturnCode.Ok;

            //
            // NOTE: Process each variable in the call frame to adjust all
            //       all the reference counts.  After this point, we need
            //       the interpreter context for the event.
            //
            Interpreter eventInterpreter = eventArgs.Interpreter;

            if (eventInterpreter == null)
                return ReturnCode.Ok;

            foreach (KeyValuePair<string, IVariable> pair in variables)
            {
                //
                // NOTE: Grab the variable and make sure the variable it is
                //       valid.
                //
                IVariable variable = pair.Value;

                if (variable == null)
                    continue;

                //
                // NOTE: For unset operations, ObjectTraceCallback uses only
                //       the "traceInfo.Variable" and "traceInfo.oldValue"
                //       members of the ITraceInfo object instance.  If the
                //       number of trace and/or watch levels exceeds one,
                //       force creation of a new TraceInfo object here;
                //       otherwise, we may interfere with the setting of an
                //       unrelated variable value.
                //
                ITraceInfo traceInfo = ScriptOps.NewTraceInfo(
                    interpreter, null, BreakpointType.BeforeVariableUnset,
                    newFrame, variable, pair.Key, null, VariableFlags.None,
                    variable.Value, null, null, null, null,
                    interpreter.NeedNewTraceInfo(VariableFlags.None),
                    false, !EntityOps.IsNoPostProcess(variable),
                    ReturnCode.Ok);

                //
                // HACK: Manually invoke the Interpreter.ObjectTraceCallback
                //       static (trace callback) method, in order to handle
                //       contained object reference(s), if any.  After this
                //       method returns, the entire call frame will be going
                //       away, along with any object references contained
                //       within it.
                //
                ReturnCode code = Interpreter.ObjectTraceCallback(
                    traceInfo.BreakpointType, eventInterpreter, traceInfo,
                    ref result);

                if (code != ReturnCode.Ok)
                    return code;
            }

            return ReturnCode.Ok;
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
