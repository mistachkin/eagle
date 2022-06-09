/*
 * StatusManager.cs --
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

namespace Eagle._Interfaces.Public
{
    [ObjectId("f3eb371a-5949-4615-a9fb-b262503a539b")]
    public interface IStatusManager
    {
        ReturnCode CheckStatus(
            IClientData clientData,
            int? timeout,
            ref Result error
        );

        ///////////////////////////////////////////////////////////////////////

        ReturnCode StartStatus(
            IClientData clientData,
            int? timeout,
            ref Result error
        );

        ///////////////////////////////////////////////////////////////////////

        ReturnCode StopStatus(
            IClientData clientData,
            int? timeout,
            ref Result error
        );

        ///////////////////////////////////////////////////////////////////////

        ReturnCode ClearStatus(
            IClientData clientData,
            int? timeout,
            ref Result error
        );

        ///////////////////////////////////////////////////////////////////////

        ReturnCode ReportStatus(
            IClientData clientData,
            string text,
            int? timeout,
            ref Result error
        );
    }
}
