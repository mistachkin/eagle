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
    [ObjectId("b3669ac8-f2a9-4d69-b203-ba8a61995ee4")]
    public interface INotifyManager
    {
        ///////////////////////////////////////////////////////////////////////
        // NOTIFICATIONS
        ///////////////////////////////////////////////////////////////////////

        NotifyType GlobalNotifyTypes { get; set; }
        NotifyFlags GlobalNotifyFlags { get; set; }
        bool GlobalNotify { get; set; }

        NotifyType NotifyTypes { get; set; }
        NotifyFlags NotifyFlags { get; set; }

        ReturnCode FireNotification(
            IScriptEventArgs eventArgs,
            IClientData clientData,
            ArgumentList arguments,
            ref Result result
            );
    }
}
