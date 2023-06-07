/*
 * EventCallback.cs --
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
    [ObjectId("b7e3be5e-440c-4486-9b18-0a9c45c6a4d3")]
    public interface IEventCallback
    {
        ReturnCode Event(
            Interpreter interpreter, // TODO: Change to use the IInterpreter type.
            IClientData clientData,
            ref Result result
        );
    }
}
