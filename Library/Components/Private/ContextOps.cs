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
    /// <summary>
    /// This class provides helper methods for working with per-thread context
    /// objects, including determining the current context thread identifier,
    /// checking whether a thread context may be used by the calling thread,
    /// and disposing a thread context.
    /// </summary>
    [ObjectId("c8a9c871-b414-44e9-a5b6-a4612e2bb9ac")]
    internal static class ContextOps
    {
        /// <summary>
        /// This method gets the thread identifier associated with the current
        /// thread context.
        /// </summary>
        /// <returns>
        /// The thread identifier of the current context thread.
        /// </returns>
        public static long GetCurrentThreadId()
        {
            return GlobalState.GetCurrentContextThreadId();
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method determines whether the specified thread context may be
        /// used by the calling thread.  A thread context may be used only when
        /// it is non-null, has not been disposed, and is owned by the current
        /// thread.
        /// </summary>
        /// <param name="threadContext">
        /// The thread context to check.  This parameter may be null.
        /// </param>
        /// <returns>
        /// True if the specified thread context may be used by the calling
        /// thread; otherwise, false.
        /// </returns>
        public static bool CanUseThread(
            IThreadContext threadContext
            )
        {
            if ((threadContext == null) || threadContext.Disposed)
                return false;

            return (threadContext.ThreadId == GetCurrentThreadId());
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method disposes the specified thread context if it implements
        /// <see cref="IDisposable" />.
        /// </summary>
        /// <param name="threadContext">
        /// The thread context to dispose.  This parameter may be null.
        /// </param>
        /// <returns>
        /// True if the specified thread context was non-null and supported
        /// disposal (and was disposed); otherwise, false.
        /// </returns>
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
