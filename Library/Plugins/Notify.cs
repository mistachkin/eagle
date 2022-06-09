/*
 * Notify.cs --
 *
 * Copyright (c) 2007-2012 by Joe Mistachkin.  All rights reserved.
 *
 * See the file "license.terms" for information on usage and redistribution of
 * this file, and for a DISCLAIMER OF ALL WARRANTIES.
 *
 * RCS: @(#) $Id: $
 */

using Eagle._Attributes;
using Eagle._Components.Private;
using Eagle._Components.Public;
using Eagle._Interfaces.Public;

namespace Eagle._Plugins
{
    [ObjectId("61e23183-72cf-4fa4-a2e5-412541a5df63")]
    public class Notify : Default
    {
        #region Public Constructors
        public Notify(
            IPluginData pluginData
            )
            : base(pluginData)
        {
            this.Flags |= AttributeOps.GetPluginFlags(GetType().BaseType) |
                AttributeOps.GetPluginFlags(this);
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region IState Members
        #region Private Data
        private NotifyType savedNotifyTypes = NotifyType.None;
        private NotifyFlags savedNotifyFlags = NotifyFlags.None;
        #endregion

        ///////////////////////////////////////////////////////////////////////

        public override ReturnCode Initialize(
            Interpreter interpreter,
            IClientData clientData,
            ref Result result
            )
        {
            if (interpreter != null)
            {
                NotifyType notifyTypes = GetTypes(interpreter);

                if (!FlagOps.HasFlags(
                        interpreter.NotifyTypes, notifyTypes, true))
                {
                    //
                    // NOTE: Add the notify types that we need to the
                    //       interpreter.
                    //
                    interpreter.GlobalNotifyTypes |= notifyTypes;
                    savedNotifyTypes = notifyTypes;
                }

                ///////////////////////////////////////////////////////////////

                NotifyFlags notifyFlags = GetFlags(interpreter);

                if (!FlagOps.HasFlags(
                        interpreter.NotifyFlags, notifyFlags, true))
                {
                    //
                    // NOTE: Add the notify flags that we need to the
                    //       interpreter.
                    //
                    interpreter.GlobalNotifyFlags |= notifyFlags;
                    savedNotifyFlags = notifyFlags;
                }
            }

            ///////////////////////////////////////////////////////////////////

            return base.Initialize(interpreter, clientData, ref result);
        }

        ///////////////////////////////////////////////////////////////////////

        public override ReturnCode Terminate(
            Interpreter interpreter,
            IClientData clientData,
            ref Result result
            )
        {
            if (interpreter != null)
            {
                if (savedNotifyFlags != NotifyFlags.None)
                {
                    //
                    // NOTE: Remove the notify flags that we added to the
                    //       interpreter earlier.
                    //
                    interpreter.GlobalNotifyFlags &= ~savedNotifyFlags;
                    savedNotifyFlags = NotifyFlags.None;
                }

                ///////////////////////////////////////////////////////////////

                if (savedNotifyTypes != NotifyType.None)
                {
                    //
                    // NOTE: Remove the notify types that we added to the
                    //       interpreter earlier.
                    //
                    interpreter.GlobalNotifyTypes &= ~savedNotifyTypes;
                    savedNotifyTypes = NotifyType.None;
                }
            }

            ///////////////////////////////////////////////////////////////////

            return base.Terminate(interpreter, clientData, ref result);
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region INotify Members
        #region Private Data
        private NotifyType notifyTypes = NotifyType.Invalid;
        private NotifyFlags notifyFlags = NotifyFlags.Invalid;
        #endregion

        ///////////////////////////////////////////////////////////////////////

        public override NotifyType GetTypes(
            Interpreter interpreter /* NOT USED */
            )
        {
            //
            // NOTE: *WARNING* This code is in critical path for all
            //       notifications and should be as fast as possible;
            //       therefore, we only compute the NotifyType for the
            //       plugin once.  Classes that inherit from this one
            //       are free to change this behavior; however, if
            //       anything costly is done here it may severely
            //       negatively impact performance in some scenarios.
            //
            if (notifyTypes == NotifyType.Invalid)
            {
                notifyTypes = AttributeOps.GetNotifyTypes(
                    GetType().BaseType) | AttributeOps.GetNotifyTypes(
                    this);
            }

            return notifyTypes;
        }

        ///////////////////////////////////////////////////////////////////////

        public override NotifyFlags GetFlags(
            Interpreter interpreter /* NOT USED */
            )
        {
            //
            // NOTE: *WARNING* This code is in critical path for all
            //       notifications and should be as fast as possible;
            //       therefore, we only compute the NotifyFlags for
            //       the plugin once.  Classes that inherit from this
            //       one are free to change this behavior; however, if
            //       anything costly is done here it may severely
            //       negatively impact performance in some scenarios.
            //
            if (notifyFlags == NotifyFlags.Invalid)
            {
                notifyFlags = AttributeOps.GetNotifyFlags(
                    GetType().BaseType) | AttributeOps.GetNotifyFlags(
                    this);
            }

            return notifyFlags;
        }
        #endregion
    }
}
