/*
 * WebTransferCallback.cs --
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
    [ObjectId("c381f330-a5db-4130-855c-ad33bd99dc5d")]
    public interface IWebTransferCallback
    {
        ReturnCode WebTransfer(
            Interpreter interpreter,
            WebFlags flags,
            IClientData clientData,
            ref Result error
        );
    }
}
