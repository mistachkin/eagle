/*
 * Event.cs --
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
using Eagle._Components.Public.Delegates;
using Eagle._Containers.Public;

namespace Eagle._Interfaces.Public
{
    [ObjectId("9c55849d-5347-4f5d-8c56-80d306d4f688")]
    public interface IEvent : ISynchronize, IIdentifier, IGetInterpreter
    {
        Delegate Delegate { get; }
        EventType Type { get; }
        EventFlags Flags { get; }
        EventPriority Priority { get; }
        DateTime DateTime { get; }
        EventCallback Callback { get; }
        long? ThreadId { get; }

        ///////////////////////////////////////////////////////////////////////

        bool GetResult(
            bool wait,
            ref ReturnCode returnCode,
            ref Result result,
            ref int errorLine,
            ref Result error
        );

        ///////////////////////////////////////////////////////////////////////

        bool GetResult(
            int timeout,
            bool wait,
            ref ReturnCode returnCode,
            ref Result result,
            ref int errorLine,
            ref Result error
        );

        ///////////////////////////////////////////////////////////////////////

        bool ResetResult(
            bool signal,
            ref Result error
        );

        ///////////////////////////////////////////////////////////////////////

        bool ResetResult(
            int timeout,
            bool signal,
            ref Result error
        );

        ///////////////////////////////////////////////////////////////////////

        bool SetResult(
            bool signal,
            ReturnCode returnCode,
            Result result,
            int errorLine,
            ref Result error
        );

        ///////////////////////////////////////////////////////////////////////

        bool SetResult(
            int timeout,
            bool signal,
            ReturnCode returnCode,
            Result result,
            int errorLine,
            ref Result error
        );

        ///////////////////////////////////////////////////////////////////////

        StringPairList ToList();
    }
}
