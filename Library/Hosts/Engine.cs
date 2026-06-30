/*
 * Engine.cs --
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
using Eagle._Components.Private;
using Eagle._Components.Public;
using Eagle._Interfaces.Public;

using _Engine = Eagle._Components.Public.Engine;

namespace Eagle._Hosts
{
    /// <summary>
    /// This class is an abstract host base class that extends the default host
    /// with threading-related capabilities.  It adds support for creating
    /// threads, queuing work items, sleeping, and yielding by delegating to the
    /// Eagle engine, and it advertises the corresponding host flags.  It sits
    /// between <see cref="Default" /> and the more specialized host classes in
    /// the host class hierarchy.
    /// </summary>
    [ObjectId("a188f92b-33a8-4784-9c4c-a11ebc6f1fd5")]
    public abstract class Engine : Default, IDisposable
    {
        #region Protected Constructors
        /// <summary>
        /// Constructs an instance of this host class.
        /// </summary>
        /// <param name="hostData">
        /// The host data used to initialize this host, if any.  This parameter
        /// may be null.
        /// </param>
        protected Engine(
            IHostData hostData
            )
            : base(hostData)
        {
            // do nothing.
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Host Flags Support
        /// <summary>
        /// This method invalidates the cached host flags so that they will be
        /// recomputed the next time they are requested.
        /// </summary>
        private void PrivateResetHostFlagsOnly()
        {
            hostFlags = HostFlags.Invalid;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method invalidates the cached host flags and then resets the
        /// base host flags.
        /// </summary>
        /// <returns>
        /// True if the base host flags were reset; otherwise, false.
        /// </returns>
        private bool PrivateResetHostFlags()
        {
            PrivateResetHostFlagsOnly();
            return base.ResetHostFlags();
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method computes and caches the host flags for this host, if
        /// they have not already been computed.  It adds the thread, work item,
        /// sleep, and yield flags to those provided by the base host.
        /// </summary>
        /// <returns>
        /// The host flags for this host.
        /// </returns>
        protected override HostFlags MaybeInitializeHostFlags()
        {
            if (hostFlags == HostFlags.Invalid)
            {
                //
                // NOTE: We support the "CreateThread", "QueueWorkItem",
                //       "Sleep", and "Yield" methods.
                //
                hostFlags = HostFlags.Thread | HostFlags.WorkItem |
                            HostFlags.Sleep | HostFlags.Yield |
                            base.MaybeInitializeHostFlags();
            }

            return hostFlags;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method records whether an exception was encountered while
        /// reading from the host and invalidates the cached host flags.
        /// </summary>
        /// <param name="exception">
        /// Non-zero if an exception was encountered while reading from the
        /// host; otherwise, zero.
        /// </param>
        protected override void SetReadException(
            bool exception
            )
        {
            base.SetReadException(exception);
            PrivateResetHostFlagsOnly();
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method records whether an exception was encountered while
        /// writing to the host and invalidates the cached host flags.
        /// </summary>
        /// <param name="exception">
        /// Non-zero if an exception was encountered while writing to the
        /// host; otherwise, zero.
        /// </param>
        protected override void SetWriteException(
            bool exception
            )
        {
            base.SetWriteException(exception);
            PrivateResetHostFlagsOnly();
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region IInteractiveHost Members
        /// <summary>
        /// The cached host flags for this host, or
        /// <see cref="HostFlags.Invalid" /> when they have not yet been
        /// computed.
        /// </summary>
        private HostFlags hostFlags = HostFlags.Invalid;
        /// <summary>
        /// This method gets the host flags for this host, computing and caching
        /// them on first use.
        /// </summary>
        /// <returns>
        /// The host flags for this host.
        /// </returns>
        public override HostFlags GetHostFlags()
        {
            CheckDisposed();

            return MaybeInitializeHostFlags();
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region IThreadHost Members
        /// <summary>
        /// This method creates a new thread that will run the specified
        /// parameterless start routine.
        /// </summary>
        /// <param name="start">
        /// The start routine to be executed by the new thread.
        /// </param>
        /// <param name="maxStackSize">
        /// The maximum stack size, in bytes, to be used by the new thread, or
        /// zero to use the default stack size.
        /// </param>
        /// <param name="userInterface">
        /// Non-zero if the new thread should use single-threaded apartment
        /// state (i.e. it may host user-interface components); otherwise, zero.
        /// </param>
        /// <param name="isBackground">
        /// Non-zero if the new thread should be a background thread; otherwise,
        /// zero.
        /// </param>
        /// <param name="useActiveStack">
        /// Non-zero if the new thread should share the active call stack of the
        /// creating thread; otherwise, zero.
        /// </param>
        /// <param name="thread">
        /// Upon success, this is set to the newly created thread.
        /// </param>
        /// <param name="error">
        /// Upon failure, this contains an appropriate error message.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise,
        /// <see cref="ReturnCode.Error" /> with details placed in
        /// <paramref name="error" />.
        /// </returns>
        public override ReturnCode CreateThread(
            ThreadStart start,
            int maxStackSize,
            bool userInterface,
            bool isBackground,
            bool useActiveStack,
            ref Thread thread,
            ref Result error
            )
        {
            CheckDisposed();

            try
            {
                thread = _Engine.CreateThread(
                    start, maxStackSize, userInterface, isBackground,
                    useActiveStack);

                return ReturnCode.Ok;
            }
            catch (Exception e)
            {
                error = e;
            }

            return ReturnCode.Error;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method creates a new thread that will run the specified
        /// parameterized start routine.
        /// </summary>
        /// <param name="start">
        /// The parameterized start routine to be executed by the new thread.
        /// </param>
        /// <param name="maxStackSize">
        /// The maximum stack size, in bytes, to be used by the new thread, or
        /// zero to use the default stack size.
        /// </param>
        /// <param name="userInterface">
        /// Non-zero if the new thread should use single-threaded apartment
        /// state (i.e. it may host user-interface components); otherwise, zero.
        /// </param>
        /// <param name="isBackground">
        /// Non-zero if the new thread should be a background thread; otherwise,
        /// zero.
        /// </param>
        /// <param name="useActiveStack">
        /// Non-zero if the new thread should share the active call stack of the
        /// creating thread; otherwise, zero.
        /// </param>
        /// <param name="thread">
        /// Upon success, this is set to the newly created thread.
        /// </param>
        /// <param name="error">
        /// Upon failure, this contains an appropriate error message.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise,
        /// <see cref="ReturnCode.Error" /> with details placed in
        /// <paramref name="error" />.
        /// </returns>
        public override ReturnCode CreateThread(
            ParameterizedThreadStart start,
            int maxStackSize,
            bool userInterface,
            bool isBackground,
            bool useActiveStack,
            ref Thread thread,
            ref Result error
            )
        {
            CheckDisposed();

            try
            {
                thread = _Engine.CreateThread(
                    start, maxStackSize, userInterface, isBackground,
                    useActiveStack);

                return ReturnCode.Ok;
            }
            catch (Exception e)
            {
                error = e;
            }

            return ReturnCode.Error;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method queues a parameterless callback for asynchronous
        /// execution on a thread pool thread.
        /// </summary>
        /// <param name="callback">
        /// The callback to be executed asynchronously.
        /// </param>
        /// <param name="flags">
        /// The flags that control how the work item is queued.
        /// </param>
        /// <param name="error">
        /// Upon failure, this contains an appropriate error message.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise,
        /// <see cref="ReturnCode.Error" /> with details placed in
        /// <paramref name="error" />.
        /// </returns>
        public override ReturnCode QueueWorkItem(
            ThreadStart callback,
            QueueFlags flags,
            ref Result error
            )
        {
            CheckDisposed();

            try
            {
                if (_Engine.QueueWorkItem(callback, flags))
                    return ReturnCode.Ok;
                else
                    error = "could not queue work item";
            }
            catch (Exception e)
            {
                error = e;
            }

            return ReturnCode.Error;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method queues a callback, together with its state object, for
        /// asynchronous execution on a thread pool thread.
        /// </summary>
        /// <param name="callback">
        /// The callback to be executed asynchronously.
        /// </param>
        /// <param name="state">
        /// The state object to be passed to the callback when it is executed.
        /// This parameter may be null.
        /// </param>
        /// <param name="flags">
        /// The flags that control how the work item is queued.
        /// </param>
        /// <param name="error">
        /// Upon failure, this contains an appropriate error message.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise,
        /// <see cref="ReturnCode.Error" /> with details placed in
        /// <paramref name="error" />.
        /// </returns>
        public override ReturnCode QueueWorkItem(
            WaitCallback callback,
            object state,
            QueueFlags flags,
            ref Result error
            )
        {
            CheckDisposed();

            try
            {
                if (_Engine.QueueWorkItem(callback, state, flags))
                    return ReturnCode.Ok;
                else
                    error = "could not queue work item";
            }
            catch (Exception e)
            {
                error = e;
            }

            return ReturnCode.Error;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method blocks the current thread for the specified number of
        /// milliseconds.  Any exception encountered while sleeping is traced
        /// and suppressed.
        /// </summary>
        /// <param name="milliseconds">
        /// The number of milliseconds for which the current thread should
        /// sleep.
        /// </param>
        /// <returns>
        /// True if the thread slept successfully; otherwise, false.
        /// </returns>
        public override bool Sleep(
            int milliseconds
            )
        {
            CheckDisposed();

            try
            {
                HostOps.ThreadSleep(milliseconds); /* throw */
                return true;
            }
            catch (ThreadAbortException e)
            {
                Thread.ResetAbort();

                TraceOps.DebugTrace(
                    e, typeof(Engine).Name,
                    TracePriority.ThreadError2);
            }
            catch (ThreadInterruptedException e)
            {
                TraceOps.DebugTrace(
                    e, typeof(Engine).Name,
                    TracePriority.ThreadError2);
            }
            catch (Exception e)
            {
                TraceOps.DebugTrace(
                    e, typeof(Engine).Name,
                    TracePriority.ThreadError);
            }

            return false;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method yields the remainder of the current thread's time slice
        /// to another ready thread.  Any exception encountered while yielding
        /// is traced and suppressed.
        /// </summary>
        /// <returns>
        /// True if the thread yielded successfully; otherwise, false.
        /// </returns>
        public override bool Yield()
        {
            CheckDisposed();

            try
            {
                HostOps.ThreadYield(); /* throw */
                return true;
            }
#if !NET_40
            catch (ThreadAbortException e)
            {
                Thread.ResetAbort();

                TraceOps.DebugTrace(
                    e, typeof(Engine).Name,
                    TracePriority.ThreadError2);
            }
            catch (ThreadInterruptedException e)
            {
                TraceOps.DebugTrace(
                    e, typeof(Engine).Name,
                    TracePriority.ThreadError2);
            }
#endif
            catch (Exception e)
            {
                TraceOps.DebugTrace(
                    e, typeof(Engine).Name,
                    TracePriority.ThreadError);
            }

            return false;
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region IHost Members
        /// <summary>
        /// This method resets this host's configuration flags to their default
        /// values.
        /// </summary>
        /// <returns>
        /// True if the flags were reset; otherwise, false.
        /// </returns>
        public override bool ResetHostFlags()
        {
            CheckDisposed();

            return PrivateResetHostFlags();
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method resets this host to its initial state, including
        /// resetting its host flags.
        /// </summary>
        /// <param name="error">
        /// Upon failure, this contains an appropriate error message.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise,
        /// <see cref="ReturnCode.Error" /> with details placed in
        /// <paramref name="error" />.
        /// </returns>
        public override ReturnCode Reset(
            ref Result error
            )
        {
            CheckDisposed();

            if (base.Reset(ref error) == ReturnCode.Ok)
            {
                if (!PrivateResetHostFlags()) /* NON-VIRTUAL */
                {
                    error = "failed to reset flags";
                    return ReturnCode.Error;
                }

                return ReturnCode.Ok;
            }

            return ReturnCode.Error;
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region IMaybeDisposed Members
        /// <summary>
        /// Gets a value indicating whether this host has been disposed.
        /// </summary>
        public override bool Disposed
        {
            get { return disposed; }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region IDisposable "Pattern" Members
        /// <summary>
        /// Stores a value indicating whether this host has been disposed.
        /// </summary>
        private bool disposed;
        /// <summary>
        /// This method throws an exception if this host has already been
        /// disposed.  It is called at the start of most members to guard
        /// against use after disposal.
        /// </summary>
        /// <exception cref="InterpreterDisposedException">
        /// Thrown when this host has been disposed and the engine is configured
        /// to throw on use of a disposed object.
        /// </exception>
        private void CheckDisposed() /* throw */
        {
#if THROW_ON_DISPOSED
            if (disposed && _Engine.IsThrowOnDisposed(null, false))
                throw new InterpreterDisposedException(typeof(Engine));
#endif
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method releases the resources held by this host.  It implements
        /// the standard dispose pattern.
        /// </summary>
        /// <param name="disposing">
        /// Non-zero if this method is being called from the
        /// <see cref="IDisposable.Dispose" /> method (i.e.
        /// deterministically); zero if it is being called from the finalizer.
        /// When non-zero, managed resources are released.
        /// </param>
        protected override void Dispose(bool disposing)
        {
            try
            {
                if (!disposed)
                {
                    //if (disposing)
                    //{
                    //    ////////////////////////////////////
                    //    // dispose managed resources here...
                    //    ////////////////////////////////////
                    //}

                    //////////////////////////////////////
                    // release unmanaged resources here...
                    //////////////////////////////////////
                }
            }
            finally
            {
                base.Dispose(disposing);

                disposed = true;
            }
        }
        #endregion
    }
}
