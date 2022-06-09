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
using Eagle._Components.Public;
using Eagle._Containers.Public;

namespace Eagle._Interfaces.Public
{
    [ObjectId("41636597-8a41-4601-9328-ff2df87d8f71")]
    public interface INotify
    {
        //
        // TODO: Change these to use the IInterpreter type.
        //
        [Throw(true)]
        NotifyType GetTypes(Interpreter interpreter);

        [Throw(true)]
        NotifyFlags GetFlags(Interpreter interpreter);

        [Throw(true)]
        ReturnCode Notify(Interpreter interpreter, IScriptEventArgs eventArgs,
            IClientData clientData, ArgumentList arguments, ref Result result);
    }
}
