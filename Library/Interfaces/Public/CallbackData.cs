/*
 * CallbackData.cs --
 *
 * Copyright (c) 2007-2012 by Joe Mistachkin.  All rights reserved.
 *
 * See the file "license.terms" for information on usage and redistribution of
 * this file, and for a DISCLAIMER OF ALL WARRANTIES.
 *
 * RCS: @(#) $Id: $
 */

using System;
using System.Threading;
using Eagle._Attributes;
using Eagle._Components.Public;
using Eagle._Containers.Public;

namespace Eagle._Interfaces.Public
{
    [ObjectId("8a93df11-d442-4a53-b079-f63e7b7f854e")]
    public interface ICallbackData : IIdentifier, IHaveObjectFlags
    {
        MarshalFlags MarshalFlags { get; }
        CallbackFlags CallbackFlags { get; set; }
        ByRefArgumentFlags ByRefArgumentFlags { get; }
        StringList Arguments { get; }
        Delegate Delegate { get; }

        Type OriginalDelegateType { get; }
        Type ModifiedDelegateType { get; }
        StringList ParameterNames { get; }
        Type ReturnType { get; }
        TypeList ParameterTypes { get; }

        AsyncCallback AsyncCallback { get; }
        EventHandler EventHandler { get; }
        ThreadStart ThreadStart { get; }
        ParameterizedThreadStart ParameterizedThreadStart { get; }
    }
}
