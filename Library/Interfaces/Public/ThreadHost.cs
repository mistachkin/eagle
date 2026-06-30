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
    /// <summary>
    /// This interface extends the interactive host
    /// (<see cref="IInteractiveHost" />) with thread-related services,
    /// allowing the host to create threads, queue work items, and yield or
    /// suspend the current thread on behalf of the interpreter.
    /// </summary>
    [ObjectId("22fdbff0-f93d-4d29-8bd9-1d2707ef3d15")]
    public interface IThreadHost : IInteractiveHost
    {
        /// <summary>
        /// Creates a new thread that uses a parameterless start delegate.
        /// </summary>
        /// <param name="start">
        /// The delegate that represents the entry point for the new thread.
        /// </param>
        /// <param name="maxStackSize">
        /// The maximum stack size, in bytes, to use for the new thread, or
        /// zero to use the default.
        /// </param>
        /// <param name="userInterface">
        /// Non-zero if the new thread will host a user interface and should be
        /// configured for single-threaded apartment use.
        /// </param>
        /// <param name="isBackground">
        /// Non-zero if the new thread should be created as a background thread.
        /// </param>
        /// <param name="useActiveStack">
        /// Non-zero if the new thread should inherit the active call stack from
        /// the creating thread.
        /// </param>
        /// <param name="thread">
        /// Upon success, this will contain the newly created thread.
        /// </param>
        /// <param name="error">
        /// Upon failure, this may contain an appropriate error message.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise, a non-Ok value
        /// with details placed in the <paramref name="error" /> parameter.
        /// </returns>
        ReturnCode CreateThread(ThreadStart start, int maxStackSize,
            bool userInterface, bool isBackground, bool useActiveStack,
            ref Thread thread, ref Result error);

        /// <summary>
        /// Creates a new thread that uses a parameterized start delegate.
        /// </summary>
        /// <param name="start">
        /// The delegate that represents the entry point for the new thread and
        /// accepts a single object argument.
        /// </param>
        /// <param name="maxStackSize">
        /// The maximum stack size, in bytes, to use for the new thread, or
        /// zero to use the default.
        /// </param>
        /// <param name="userInterface">
        /// Non-zero if the new thread will host a user interface and should be
        /// configured for single-threaded apartment use.
        /// </param>
        /// <param name="isBackground">
        /// Non-zero if the new thread should be created as a background thread.
        /// </param>
        /// <param name="useActiveStack">
        /// Non-zero if the new thread should inherit the active call stack from
        /// the creating thread.
        /// </param>
        /// <param name="thread">
        /// Upon success, this will contain the newly created thread.
        /// </param>
        /// <param name="error">
        /// Upon failure, this may contain an appropriate error message.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise, a non-Ok value
        /// with details placed in the <paramref name="error" /> parameter.
        /// </returns>
        ReturnCode CreateThread(ParameterizedThreadStart start,
            int maxStackSize, bool userInterface, bool isBackground,
            bool useActiveStack, ref Thread thread, ref Result error);

        /// <summary>
        /// Queues a parameterless callback for execution on a thread pool
        /// thread.
        /// </summary>
        /// <param name="callback">
        /// The delegate to invoke on a thread pool thread.
        /// </param>
        /// <param name="flags">
        /// The flags used to control how the work item is queued.
        /// </param>
        /// <param name="error">
        /// Upon failure, this may contain an appropriate error message.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise, a non-Ok value
        /// with details placed in the <paramref name="error" /> parameter.
        /// </returns>
        ReturnCode QueueWorkItem(
            ThreadStart callback, QueueFlags flags, ref Result error);

        /// <summary>
        /// Queues a callback that accepts a state object for execution on a
        /// thread pool thread.
        /// </summary>
        /// <param name="callback">
        /// The delegate to invoke on a thread pool thread.
        /// </param>
        /// <param name="state">
        /// The state object to pass to the callback.  This parameter may be
        /// null.
        /// </param>
        /// <param name="flags">
        /// The flags used to control how the work item is queued.
        /// </param>
        /// <param name="error">
        /// Upon failure, this may contain an appropriate error message.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise, a non-Ok value
        /// with details placed in the <paramref name="error" /> parameter.
        /// </returns>
        ReturnCode QueueWorkItem(
            WaitCallback callback, object state, QueueFlags flags,
            ref Result error);

        /// <summary>
        /// Suspends the current thread for the specified amount of time.
        /// </summary>
        /// <param name="milliseconds">
        /// The amount of time to suspend the current thread, in milliseconds.
        /// </param>
        /// <returns>
        /// True if the thread was successfully suspended; otherwise, false.
        /// </returns>
        bool Sleep(int milliseconds);
        /// <summary>
        /// Causes the current thread to yield execution to another thread that
        /// is ready to run on the current processor.
        /// </summary>
        /// <returns>
        /// True if the operating system switched execution to another thread;
        /// otherwise, false.
        /// </returns>
        bool Yield();
    }
}
