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
        private static readonly NotifyType NotifyTypes =
            NotifyType.Interpreter | NotifyType.Script;

        ///////////////////////////////////////////////////////////////////////

        private static readonly NotifyFlags NotifyFlags =
            NotifyFlags.Completed | NotifyFlags.Canceled | NotifyFlags.Exit;
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Private Data
        private _Forms.TestForm form = null;
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Public Constructors
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
        public override ReturnCode About(
            Interpreter interpreter,
            ref Result result
            )
        {
            result = Utility.FormatPluginAbout(this, true);
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
