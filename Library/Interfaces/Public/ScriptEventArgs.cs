/*
 * ScriptEventArgs.cs --
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
    [ObjectId("acd98820-f016-440a-a087-4a268cba1310")]
    public interface IScriptEventArgs : IGetInterpreter, IGetClientData
    {
        long Id { get; }
        NotifyType NotifyTypes { get; }
        NotifyFlags NotifyFlags { get; }
        ArgumentList Arguments { get; }
        Result Result { get; }
        ScriptException Exception { get; }
        InterruptType InterruptType { get; }
        string ResourceName { get; }
        object[] MessageArgs { get; }
    }
}
