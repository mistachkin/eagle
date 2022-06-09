/*
 * ContextOps.cs --
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
using Eagle._Interfaces.Private;

namespace Eagle._Components.Private
{
    [ObjectId("c8a9c871-b414-44e9-a5b6-a4612e2bb9ac")]
    internal static class ContextOps
    {
        public static long GetCurrentThreadId()
        {
            return GlobalState.GetCurrentContextThreadId();
        }

        ///////////////////////////////////////////////////////////////////////

        public static bool CanUseThread(
            IThreadContext threadContext
            )
        {
            if ((threadContext == null) || threadContext.Disposed)
                return false;

            return (threadContext.ThreadId == GetCurrentThreadId());
        }

        ///////////////////////////////////////////////////////////////////////

        public static bool DisposeThread(
            IThreadContext threadContext
            )
        {
            if (threadContext == null)
                return false;

            IDisposable disposable = threadContext as IDisposable;

            if (disposable == null)
                return false;

            disposable.Dispose();
            disposable = null;

            return true;
        }
    }
}
