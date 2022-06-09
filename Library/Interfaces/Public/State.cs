/*
 * State.cs --
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
    [ObjectId("3105de58-a7c0-4cf0-bb57-36872afe8942")]
    public interface IState
    {
        bool Initialized { get; set; }

        //
        // TODO: Change these to use the IInterpreter type.
        //
        [Throw(true)]
        ReturnCode Initialize(Interpreter interpreter, IClientData clientData, ref Result result);

        [Throw(true)]
        ReturnCode Terminate(Interpreter interpreter, IClientData clientData, ref Result result);
    }
}