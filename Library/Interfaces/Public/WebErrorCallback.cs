/*
 * WebErrorCallback.cs --
 *
 * Copyright (c) 2007-2012 by Joe Mistachkin.  All rights reserved.
 *
 * See the file "license.terms" for information on usage and redistribution of
 * this file, and for a DISCLAIMER OF ALL WARRANTIES.
 *
 * RCS: @(#) $Id: $
 */

using System;
using Eagle._Attributes;
using Eagle._Components.Public;
using Eagle._Containers.Public;

namespace Eagle._Interfaces.Public
{
    [ObjectId("44c7aa4e-42eb-4ed8-ad78-335bfb7bcec4")]
    public interface IWebErrorCallback
    {
        ReturnCode WebError(
            Interpreter interpreter,
            IClientData clientData,
            Uri uri,
            WebFlags webFlags,
            int retries,
            int? timeout,
            int? maximumRetries,
            ref object result,
            ref ResultList errors
        );
    }
}
