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
    /// <summary>
    /// This class is the base class for plugins that wish to receive
    /// notifications from the Eagle engine.  It manages registering the
    /// notification types and flags required by the plugin with the
    /// interpreter when the plugin is initialized, and removing them again
    /// when the plugin is terminated.
    /// </summary>
    [ObjectId("61e23183-72cf-4fa4-a2e5-412541a5df63")]
    public class Notify : Default
    {
        #region Public Constructors
        /// <summary>
        /// Constructs an instance of this plugin, merging the plugin flags
        /// declared via attributes on this type and its base type.
        /// </summary>
        /// <param name="pluginData">
        /// The data used to create and identify this plugin, such as its name
        /// and flags.  This parameter may be null.
        /// </param>
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
        /// <summary>
        /// The notification types that this plugin added to the interpreter
        /// during initialization and must remove during termination.
        /// </summary>
        private NotifyType savedNotifyTypes = NotifyType.None;

        /// <summary>
        /// The notification flags that this plugin added to the interpreter
        /// during initialization and must remove during termination.
        /// </summary>
        private NotifyFlags savedNotifyFlags = NotifyFlags.None;
        #endregion

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method is called when the plugin is being initialized within
        /// the specified interpreter.  It adds any notification types and
        /// flags required by this plugin that are not already present on the
        /// interpreter, remembering what it added for later removal.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter context this plugin is being initialized in.  This
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

        /// <summary>
        /// This method is called when the plugin is being terminated within
        /// the specified interpreter.  It removes the notification flags and
        /// types that this plugin previously added during initialization.
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
        /// <summary>
        /// The cached notification types handled by this plugin; this is
        /// computed once on demand and reused thereafter.
        /// </summary>
        private NotifyType notifyTypes = NotifyType.Invalid;

        /// <summary>
        /// The cached notification flags handled by this plugin; this is
        /// computed once on demand and reused thereafter.
        /// </summary>
        private NotifyFlags notifyFlags = NotifyFlags.Invalid;
        #endregion

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method returns the notification types handled by this plugin,
        /// computing them from the attributes on this type and its base type
        /// the first time it is called and caching the result for performance.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter context, which is not used by this implementation.
        /// This parameter may be null.
        /// </param>
        /// <returns>
        /// The notification types handled by this plugin.
        /// </returns>
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

        /// <summary>
        /// This method returns the notification flags handled by this plugin,
        /// computing them from the attributes on this type and its base type
        /// the first time it is called and caching the result for performance.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter context, which is not used by this implementation.
        /// This parameter may be null.
        /// </param>
        /// <returns>
        /// The notification flags handled by this plugin.
        /// </returns>
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
