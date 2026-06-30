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
    /// <summary>
    /// This class implements the system plugin responsible for managing the
    /// reference counts of opaque object handles held by interpreter
    /// variables.  When a call frame is popped or deleted, it adjusts the
    /// reference counts of any objects referenced by that frame's variables so
    /// that they can be cleaned up correctly.
    /// </summary>
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
        /// <summary>
        /// Constructs an instance of the object plugin, merging the plugin
        /// flags declared via attributes on this type and its base type.
        /// </summary>
        /// <param name="pluginData">
        /// The data used to create and identify this plugin, such as its name
        /// and flags.  This parameter may be null.
        /// </param>
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
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Private Methods
        /// <summary>
        /// This method clears the object plugin token stored in the
        /// interpreter state, ensuring it is reset when this plugin is
        /// terminated.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter context whose object plugin token is reset.  This
        /// parameter may be null.
        /// </param>
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
        /// <summary>
        /// This method is called when the plugin is being terminated within
        /// the specified interpreter.  It resets the object plugin token before
        /// delegating to the base implementation.
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

        #region INotify Members
        /// <summary>
        /// This method is called by the engine to deliver a notification to
        /// this plugin.  It responds to call frame popped or deleted events by
        /// adjusting the reference counts of any objects referenced by the
        /// affected call frame's variables, ensuring those objects can be
        /// cleaned up as the call frame goes away.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter context this plugin is associated with.  This
        /// parameter may be null.
        /// </param>
        /// <param name="eventArgs">
        /// The event arguments describing the notification, including its
        /// types, flags, and associated data.  This parameter may be null.
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
        /// Upon success, this may contain an informational result.  Upon
        /// failure, this must contain an appropriate error message.
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
