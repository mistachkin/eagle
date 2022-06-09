/*
 * ThreadHost.cs --
 *
 * Copyright (c) 2007-2012 by Joe Mistachkin.  All rights reserved.
 *
 * See the file "license.terms" for information on usage and redistribution of
 * this file, and for a DISCLAIMER OF ALL WARRANTIES.
 *
 * RCS: @(#) $Id: $
 */

using System.Threading;
using Eagle._Attributes;
using Eagle._Components.Public;

namespace Eagle._Interfaces.Public
{
    [ObjectId("22fdbff0-f93d-4d29-8bd9-1d2707ef3d15")]
    public interface IThreadHost : IInteractiveHost
    {
        ReturnCode CreateThread(ThreadStart start, int maxStackSize,
            bool userInterface, bool isBackground, bool useActiveStack,
            ref Thread thread, ref Result error);

        ReturnCode CreateThread(ParameterizedThreadStart start,
            int maxStackSize, bool userInterface, bool isBackground,
            bool useActiveStack, ref Thread thread, ref Result error);

        ReturnCode QueueWorkItem(WaitCallback callback, object state,
            ref Result error);

        bool Sleep(int milliseconds);
        bool Yield();
    }
}
