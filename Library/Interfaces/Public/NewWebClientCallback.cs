/*
 * NewWebClientCallback.cs --
 *
 * Copyright (c) 2007-2012 by Joe Mistachkin.  All rights reserved.
 *
 * See the file "license.terms" for information on usage and redistribution of
 * this file, and for a DISCLAIMER OF ALL WARRANTIES.
 *
 * RCS: @(#) $Id: $
 */

using System.Net;
using Eagle._Attributes;
using Eagle._Components.Public;

namespace Eagle._Interfaces.Public
{
    [ObjectId("2d265417-d31f-45e3-b755-b52fdb180830")]
    public interface INewWebClientCallback
    {
        WebClient NewWebClient(
            Interpreter interpreter,
            string argument,
            IClientData clientData,
            ref Result error
        );
    }
}
