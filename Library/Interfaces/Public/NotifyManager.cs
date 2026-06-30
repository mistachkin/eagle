/*
 * NotifyManager.cs --
 *
 * Copyright (c) 2007-2012 by Joe Mistachkin.  All rights reserved.
 *
 * See the file "license.terms" for information on usage and redistribution of
 * this file, and for a DISCLAIMER OF ALL WARRANTIES.
 *
 * RCS: @(#) $Id: $
 */

using Eagle._Attributes;
using Eagle._Components.Public;
using Eagle._Containers.Public;

namespace Eagle._Interfaces.Public
{
    /// <summary>
    /// This interface is implemented by entities that manage the firing and
    /// routing of script notifications within an Eagle interpreter.  It
    /// exposes the global and per-entity notification type and flag filters
    /// along with the entry point used to fire a notification to interested
    /// subscribers.
    /// </summary>
    [ObjectId("b3669ac8-f2a9-4d69-b203-ba8a61995ee4")]
    public interface INotifyManager
    {
        ///////////////////////////////////////////////////////////////////////
        // NOTIFICATIONS
        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Gets or sets the notification types that are enabled globally,
        /// across all interpreters.
        /// </summary>
        NotifyType GlobalNotifyTypes { get; set; }
        /// <summary>
        /// Gets or sets the notification flags that apply globally, across all
        /// interpreters.
        /// </summary>
        NotifyFlags GlobalNotifyFlags { get; set; }
        /// <summary>
        /// Gets or sets a value indicating whether global notifications are
        /// enabled.
        /// </summary>
        bool GlobalNotify { get; set; }

        /// <summary>
        /// Gets or sets the notification types that are enabled for this
        /// entity.
        /// </summary>
        NotifyType NotifyTypes { get; set; }
        /// <summary>
        /// Gets or sets the notification flags that apply to this entity.
        /// </summary>
        NotifyFlags NotifyFlags { get; set; }

        /// <summary>
        /// Fires a notification, dispatching it to any interested subscribers
        /// for processing.
        /// </summary>
        /// <param name="eventArgs">
        /// The event arguments describing the notification being fired.  This
        /// parameter should not be null.
        /// </param>
        /// <param name="clientData">
        /// The extra, entity-specific data associated with this notification,
        /// if any.  This parameter may be null.
        /// </param>
        /// <param name="arguments">
        /// The list of arguments associated with this notification, if any.
        /// </param>
        /// <param name="result">
        /// Upon success, this may contain a result value produced while
        /// processing the notification.  Upon failure, this must contain an
        /// appropriate error message.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise, a non-Ok value
        /// with details placed in the <paramref name="result" /> parameter.
        /// </returns>
        ReturnCode FireNotification(
            IScriptEventArgs eventArgs,
            IClientData clientData,
            ArgumentList arguments,
            ref Result result
            );
    }
}
