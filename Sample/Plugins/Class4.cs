/*
 * Class4.cs --
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
using Eagle._Attributes;
using Eagle._Components.Public;
using Eagle._Containers.Public;
using Eagle._Interfaces.Public;
using _Plugins = Eagle._Plugins;

namespace Sample
{
    /// <summary>
    /// Declare a "custom plugin" class that inherits notify functionality and
    /// implements the appropriate interface(s).
    /// </summary>
    //
    // FIXME: Always change this GUID.
    //
    [ObjectId("bacdcc28-eeb6-4c30-8293-e3646884b199")]
    [PluginFlags(
        PluginFlags.User | PluginFlags.Notify |
        PluginFlags.NoCommands | PluginFlags.NoPolicies)]
    [NotifyTypes(NotifyType.Script)]
    [NotifyFlags(NotifyFlags.Completed)]
    internal sealed class Class4 : _Plugins.Notify
    {
        #region Public Constructor (Required)
        /// <summary>
        /// Constructs an instance of this custom plugin class.
        /// </summary>
        /// <param name="pluginData">
        /// An instance of the plugin data class containing the properties
        /// used to initialize the new instance of this custom plugin class.
        /// Typically, the only thing done with this parameter is to pass it
        /// along to the base class constructor.
        /// </param>
        public Class4(
            IPluginData pluginData /* in */
            )
            : base(pluginData)
        {
            //
            // NOTE: Typically, nothing much is done here.  Any non-trivial
            //       initialization should be done in IState.Initialize and
            //       cleaned up in IState.Terminate.
            //
            this.Flags |= Utility.GetPluginFlags(GetType().BaseType) |
                Utility.GetPluginFlags(this); /* HIGHLY RECOMMENDED */
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region INotify Members (Required)
        /// <summary>
        /// Receives notifications when an event occurs that the plugin has
        /// declared it wants to be notified about.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter context we are executing in.
        /// </param>
        /// <param name="eventArgs">
        /// Contains data related to the event.  The exact data depends on the
        /// type of event being processed.
        /// </param>
        /// <param name="clientData">
        /// The extra data supplied for this event, if any.
        /// </param>
        /// <param name="result">
        /// Upon success, this may contain an informational message.
        /// Upon failure, this must contain an appropriate error message.
        /// </param>
        /// <returns>
        /// ReturnCode.Ok on success, ReturnCode.Error on failure.
        /// </returns>
        public override ReturnCode Notify(
            Interpreter interpreter,    /* in */
            IScriptEventArgs eventArgs, /* in */
            IClientData clientData,     /* in */
            ArgumentList arguments,     /* in */
            ref Result result           /* out */
            )
        {
            ReturnCode code = ReturnCode.Ok;

            //
            // NOTE: If there is no data associated with this event, just
            //       return.  We do not know how to handle these types of
            //       events and nothing else should be done.  The best advice
            //       when implementing this interface is "when in doubt, just
            //       do nothing".
            //
            if (eventArgs == null)
                return code;

            //
            // NOTE: Make sure that notification matches the types and flags
            //       that we care about.  In theory, this should be handled
            //       by the core library before we get called; however, it
            //       cannot really hurt to double check.
            //
            if (!Utility.HasFlags(
                    eventArgs.NotifyTypes, NotifyType.Script, true) ||
                !Utility.HasFlags(
                    eventArgs.NotifyFlags, NotifyFlags.Completed, true))
            {
                return code;
            }

#if false
            //
            // NOTE: This is the interpreter involved in the event, which may
            //       be different from the interpreter context we are executing
            //       in.  This example does not make use of this interpreter;
            //       therefore, this code block is commented out.
            //
            /* NOT USED */
            Interpreter eventInterpreter = eventArgs.Interpreter;

            if (eventInterpreter == null)
                return code;
#endif

            //
            // NOTE: Grab the extra data associated with this "event" now.  The
            //       exact contents will vary depending on the event type being
            //       serviced.  The source code associated with the event type
            //       in question should be consulted to determine the necessary
            //       type conversion(s).
            //
            IClientData eventClientData = eventArgs.ClientData;

            if (eventClientData == null)
                return code;

            //
            // NOTE: In this case, the data associated with the event is an
            //       "object list".  If the data does not conform to that type,
            //       bail out now.
            //
            IList<object> list = eventClientData.Data as IList<object>;

            if (list == null)
                return code;

            //
            // NOTE: Attempt to fetch the text of the script that was just
            //       completed.
            //
            string text;

            try
            {
                //
                // NOTE: The third element should contain the full text of the
                //       completed script.
                //
                text = list[2] as string;

                if (text != null)
                {
                    //
                    // NOTE: The third and fourth elements should contain the
                    //       offset and number of characters for the completed
                    //       script, respectively.
                    //
                    text = text.Substring((int)list[3], (int)list[4]);
                }
            }
            catch
            {
                //
                // NOTE: Somehow, the data does not conform to expectations for
                //       this event type.  Gracefully ignore it.
                //
                text = null;
            }

            //
            // NOTE: To display the text of the completed script, both the
            //       interpreter and the text itself is required.
            //
            if ((interpreter != null) && (text != null))
            {
                //
                // NOTE: Grab the host from the interpreter context we are
                //       executing in.
                //
                IInteractiveHost interactiveHost = interpreter.Host;

                if (interactiveHost != null)
                {
                    //
                    // NOTE: Emit a message to the interpreter host that
                    //       includes the full text of the completed script.
                    //
                    interactiveHost.WriteLine(String.Format(
                        "{0}: script completed{1}{2}", GetType().FullName,
                        Environment.NewLine, text));
                }
            }

            return code;
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region IPlugin Members (Optional)
        /// <summary>
        /// Returns information about the loaded plugin.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter context we are executing in.
        /// </param>
        /// <param name="result">
        /// Upon success, this must contain an informational message.
        /// Upon failure, this must contain an appropriate error message.
        /// </param>
        /// <returns>
        /// ReturnCode.Ok on success, ReturnCode.Error on failure.
        /// </returns>
        public override ReturnCode About(
            Interpreter interpreter, /* in */
            ref Result result        /* out */
            )
        {
            result = Utility.FormatPluginAbout(this, true);
            return ReturnCode.Ok;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Returns the compilation options used when compiling the loaded
        /// plugin as a list of strings.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter context we are executing in.
        /// </param>
        /// <param name="result">
        /// Upon success, this must contain a list of strings consisting of the
        /// compilation options used when compiling the loaded plugin.
        /// Upon failure, this must contain an appropriate error message.
        /// </param>
        /// <returns>
        /// ReturnCode.Ok on success, ReturnCode.Error on failure.
        /// </returns>
        public override ReturnCode Options(
            Interpreter interpreter, /* in */
            ref Result result        /* out */
            )
        {
            result = new StringList(DefineConstants.OptionList, false);
            return ReturnCode.Ok;
        }
        #endregion
    }
}
