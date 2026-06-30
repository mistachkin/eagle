/*
 * TestForm.cs --
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
using Eagle._Containers.Public;
using Eagle._Interfaces.Public;

namespace Eagle._Plugins
{
    /// <summary>
    /// This class implements a test plugin that bridges the Eagle script
    /// engine notification system to a Windows Forms test application,
    /// forwarding script completion, cancellation, and exit events to the
    /// associated form.
    /// </summary>
    [ObjectId("29368752-20e2-4236-828a-4b680d55539d")]
    [PluginFlags(
        PluginFlags.Primary | PluginFlags.Host |
        PluginFlags.Command | PluginFlags.Notify |
        PluginFlags.Static | PluginFlags.Test |
        PluginFlags.UserInterface)]
    [NotifyTypes(NotifyType.Interpreter | NotifyType.Script)]
    [NotifyFlags(
        NotifyFlags.Completed | NotifyFlags.Canceled |
        NotifyFlags.Exit)]
    internal sealed class TestForm : Notify
    {
        #region Private Constants
        /// <summary>
        /// The set of notification types that this plugin is interested in
        /// handling.
        /// </summary>
        private static readonly NotifyType NotifyTypes =
            NotifyType.Interpreter | NotifyType.Script;

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// The set of notification flags that this plugin is interested in
        /// handling.
        /// </summary>
        private static readonly NotifyFlags NotifyFlags =
            NotifyFlags.Completed | NotifyFlags.Canceled | NotifyFlags.Exit;
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Private Data
        /// <summary>
        /// The test form to which script engine notifications are forwarded.
        /// </summary>
        private _Forms.TestForm form = null;
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Public Constructors
        /// <summary>
        /// Constructs an instance of this plugin.
        /// </summary>
        /// <param name="form">
        /// The test form to which script engine notifications should be
        /// forwarded.
        /// </param>
        /// <param name="pluginData">
        /// The plugin data used to initialize the base class.
        /// </param>
        public TestForm(
            _Forms.TestForm form,
            IPluginData pluginData
            )
            : base(pluginData)
        {
            this.Flags |= Utility.GetPluginFlags(GetType().BaseType) |
                Utility.GetPluginFlags(this);

            this.form = form;
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region IPlugin Members
        /// <summary>
        /// This method returns descriptive information about this plugin.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter context this plugin is executing in.
        /// </param>
        /// <param name="result">
        /// Upon success, this will contain the descriptive information about
        /// this plugin.  Upon failure, this will contain an appropriate error
        /// message.
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
            result = Utility.FormatPluginAbout(this, true);
            return ReturnCode.Ok;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method returns the list of options supported by this plugin.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter context this plugin is executing in.
        /// </param>
        /// <param name="result">
        /// Upon success, this will contain the list of supported options.
        /// Upon failure, this will contain an appropriate error message.
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

        ///////////////////////////////////////////////////////////////////////

        #region INotify Members
        /// <summary>
        /// This method is called by the script engine to deliver a
        /// notification to this plugin.  When the notification matches the
        /// types and flags of interest, it forwards the corresponding script
        /// completion, cancellation, or exit event to the associated test
        /// form.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter context this plugin is executing in.
        /// </param>
        /// <param name="eventArgs">
        /// The event arguments describing the notification being delivered.
        /// </param>
        /// <param name="clientData">
        /// The extra, plugin-specific data supplied when the plugin was
        /// created, if any.
        /// </param>
        /// <param name="arguments">
        /// The list of arguments associated with the notification, if any.
        /// </param>
        /// <param name="result">
        /// Upon success, this may contain a result value.  Upon failure, this
        /// will contain an appropriate error message.
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
            ReturnCode code = ReturnCode.Ok;

            if (form != null)
            {
                if (eventArgs != null)
                {
                    if (Utility.HasFlags(eventArgs.NotifyTypes, NotifyTypes, false) &&
                        Utility.HasFlags(eventArgs.NotifyFlags, NotifyFlags, false))
                    {
                        Interpreter eventInterpreter = eventArgs.Interpreter;

                        if ((eventInterpreter != null) &&
                            (eventArgs.ClientData != null) &&
                            (eventArgs.ClientData.Data != null))
                        {
                            if (Utility.HasFlags(eventArgs.NotifyTypes, NotifyType.Interpreter, true) &&
                                Utility.HasFlags(eventArgs.NotifyFlags, NotifyFlags.Exit, true))
                            {
                                bool? exit;

                                try
                                {
                                    exit = (bool)eventArgs.ClientData.Data;
                                }
                                catch
                                {
                                    exit = null;
                                }

                                //
                                // NOTE: If necessary, exit the application.
                                //
                                if ((exit != null) && ((bool)exit))
                                    form.AsyncDispose();
                            }
                            else if (Utility.HasFlags(eventArgs.NotifyTypes, NotifyType.Script, true) &&
                                Utility.HasFlags(eventArgs.NotifyFlags, NotifyFlags.Completed, true))
                            {
                                if (!interpreter.Exit)
                                {
                                    IList<object> list = eventArgs.ClientData.Data as IList<object>;

                                    if (list != null)
                                    {
                                        string text;

                                        try
                                        {
                                            text = list[2] as string;

                                            if (text != null)
                                                text = text.Substring(
                                                    (int)list[3], (int)list[4]);
                                        }
                                        catch
                                        {
                                            text = null;
                                        }

                                        if (text != null)
                                            form.AsyncScriptCompleted(text, result);
                                    }
                                }
                            }
                            else if (Utility.HasFlags(eventArgs.NotifyTypes, NotifyType.Script, true) &&
                                Utility.HasFlags(eventArgs.NotifyFlags, NotifyFlags.Canceled, true) &&
                                !Utility.HasFlags(eventArgs.NotifyFlags, NotifyFlags.Reset, true))
                            {
                                if (!interpreter.Exit)
                                {
                                    //
                                    // NOTE: The script engine does not currently provide any
                                    //       meaningful context information here; however, it
                                    //       should not be necessary because the test script is
                                    //       the only script that is evaluated by this application
                                    //       that can be canceled without resulting in the
                                    //       application exiting.
                                    //
                                    form.AsyncScriptCanceled();
                                }
                            }
                        }
                    }
                }
            }

            return code;
        }
        #endregion
    }
}
