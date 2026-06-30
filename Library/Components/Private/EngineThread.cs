/*
 * EngineThread.cs --
 *
 * Copyright (c) 2007-2012 by Joe Mistachkin.  All rights reserved.
 *
 * See the file "license.terms" for information on usage and redistribution of
 * this file, and for a DISCLAIMER OF ALL WARRANTIES.
 *
 * RCS: @(#) $Id: $
 */

using System;
using System.Collections.Generic;
using System.Threading;
using Eagle._Attributes;
using Eagle._Components.Public;
using Eagle._Containers.Public;
using Eagle._Interfaces.Public;

using EngineThreadDictionary = System.Collections.Generic.Dictionary<
    Eagle._Components.Private.EngineThread, Eagle._Components.Public.Interpreter>;

namespace Eagle._Components.Private
{
    /// <summary>
    /// This class represents a single engine thread used by an Eagle
    /// interpreter -- a managed thread together with the interpreter it is
    /// associated with and the start delegate it should run.  It wraps a
    /// framework <see cref="Thread" /> (which it does not own), tracks the
    /// association between engine threads and interpreters, sets up and tears
    /// down the per-thread state surrounding the start delegate, and optionally
    /// pushes and pops the active interpreter on the active interpreter stack.
    /// It implements <see cref="IGetInterpreter" /> and is disposable.
    /// </summary>
    [ObjectId("e1a3509f-1b6b-4940-8cfc-7d21c2d81c93")]
    internal sealed class EngineThread : IGetInterpreter, IDisposable
    {
        #region Private Static Data
        //
        // NOTE: This static field is used to synchronize access to the list
        //       of engine threads (below).
        //
        /// <summary>
        /// Used to synchronize access to the list of engine threads (the
        /// <see cref="engineThreads" /> dictionary below).
        /// </summary>
        private static readonly object syncRoot = new object();

        ///////////////////////////////////////////////////////////////////////

        //
        // NOTE: This static field is used to keep track of the associations
        //       between engine threads and interpreters.  Any engine thread
        //       may have at most one valid interpreter associated with it.
        //
        /// <summary>
        /// Tracks the associations between engine threads and interpreters.
        /// Any engine thread may have at most one valid interpreter associated
        /// with it.
        /// </summary>
        private static EngineThreadDictionary engineThreads;

        ///////////////////////////////////////////////////////////////////////

        //
        // NOTE: These static fields are used to keep track of how many of
        //       these objects have ever been created and how many are now
        //       active.
        //
        /// <summary>
        /// The total number of these objects that have ever been created.
        /// </summary>
        private static int createCount;

        /// <summary>
        /// The number of these objects that are currently active.
        /// </summary>
        private static int activeCount;
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Private Data
        //
        // NOTE: This is the parameterless start delegate for this thread.  It
        //       is only used when handling the ThreadStart delegate.
        //
        /// <summary>
        /// The parameterless start delegate for this thread.  It is only used
        /// when handling the <c>ThreadStart</c> delegate.
        /// </summary>
        private ThreadStart threadStart;

        ///////////////////////////////////////////////////////////////////////

        //
        // NOTE: This is the parameterized start delegate for this thread.  It
        //       is used when handling the ParameterizedThreadStart delegate
        //       and/or the ThreadStart delegate if the parameterless start
        //       delegate is not available.
        //
        /// <summary>
        /// The parameterized start delegate for this thread.  It is used when
        /// handling the <c>ParameterizedThreadStart</c> delegate and/or the
        /// <c>ThreadStart</c> delegate if the parameterless start delegate is
        /// not available.
        /// </summary>
        private ParameterizedThreadStart parameterizedThreadStart;

        ///////////////////////////////////////////////////////////////////////

        //
        // NOTE: This is the framework thread associated with this thread.  It
        //       is NOT owned by this object and will not be disposed.
        //
#if MONO_BUILD
#pragma warning disable 414
#endif
        /// <summary>
        /// The framework thread associated with this thread.  It is not owned
        /// by this object and will not be disposed.
        /// </summary>
        private Thread thread;
#if MONO_BUILD
#pragma warning restore 414
#endif

        ///////////////////////////////////////////////////////////////////////

        //
        // NOTE: When this is non-zero, the active interpreter stack will be
        //       used upon entry/exit from the thread start routine, i.e. the
        //       associated interpreter will be pushed/popped.
        //
        /// <summary>
        /// When non-zero, the active interpreter stack will be used upon entry
        /// to and exit from the thread start routine; that is, the associated
        /// interpreter will be pushed and popped.
        /// </summary>
        private bool useActiveStack;
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Private Constructors
        /// <summary>
        /// Constructs an engine thread and adds it to the global thread
        /// tracking list.
        /// </summary>
        private EngineThread()
        {
            /* IGNORED */
            GlobalState.AddThread(this);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Constructs an engine thread associated with the specified
        /// interpreter.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter to associate with this engine thread.  This
        /// parameter may be null.
        /// </param>
        /// <param name="useActiveStack">
        /// Non-zero to push and pop the associated interpreter on the active
        /// interpreter stack around the thread start routine.
        /// </param>
        private EngineThread(
            Interpreter interpreter,
            bool useActiveStack
            )
            : this()
        {
            this.interpreter = interpreter;
            this.useActiveStack = useActiveStack;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Constructs an engine thread associated with the specified
        /// interpreter that runs the specified parameterless start delegate.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter to associate with this engine thread.  This
        /// parameter may be null.
        /// </param>
        /// <param name="start">
        /// The parameterless start delegate for this thread.
        /// </param>
        /// <param name="useActiveStack">
        /// Non-zero to push and pop the associated interpreter on the active
        /// interpreter stack around the thread start routine.
        /// </param>
        private EngineThread(
            Interpreter interpreter,
            ThreadStart start,
            bool useActiveStack
            )
            : this(interpreter, useActiveStack)
        {
            this.threadStart = start;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Constructs an engine thread associated with the specified
        /// interpreter that runs the specified parameterized start delegate.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter to associate with this engine thread.  This
        /// parameter may be null.
        /// </param>
        /// <param name="start">
        /// The parameterized start delegate for this thread.
        /// </param>
        /// <param name="useActiveStack">
        /// Non-zero to push and pop the associated interpreter on the active
        /// interpreter stack around the thread start routine.
        /// </param>
        private EngineThread(
            Interpreter interpreter,
            ParameterizedThreadStart start,
            bool useActiveStack
            )
            : this(interpreter, useActiveStack)
        {
            this.parameterizedThreadStart = start;
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Introspection Support Methods
        //
        // NOTE: Used by the _Hosts.Default.BuildEngineInfoList method.
        //
        /// <summary>
        /// This method appends diagnostic information about the engine threads
        /// subsystem (such as the create and active counts) to the specified
        /// list.  It is used by the <c>_Hosts.Default.BuildEngineInfoList</c>
        /// method.
        /// </summary>
        /// <param name="list">
        /// The list to append the engine thread information to.  If this
        /// parameter is null, this method does nothing.
        /// </param>
        /// <param name="detailFlags">
        /// The flags used to control the level of detail included in the
        /// information.
        /// </param>
        public static void AddInfo(
            StringPairList list,    /* in, out */
            DetailFlags detailFlags /* in */
            )
        {
            if (list == null)
                return;

            lock (syncRoot) /* TRANSACTIONAL */
            {
                bool empty = HostOps.HasEmptyContent(detailFlags);
                StringPairList localList = new StringPairList();

                if (empty || (createCount != 0))
                    localList.Add("CreateCount", createCount.ToString());

                if (empty || (activeCount != 0))
                    localList.Add("ActiveCount", activeCount.ToString());

                if (localList.Count > 0)
                {
                    list.Add((IPair<string>)null);
                    list.Add("Engine Threads");
                    list.Add((IPair<string>)null);
                    list.Add(localList);
                }
            }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Static "Factory" Methods
        /// <summary>
        /// This method creates a new engine thread associated with the
        /// specified interpreter that runs the specified parameterless start
        /// delegate.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter to associate with the new engine thread.  This
        /// parameter may be null.
        /// </param>
        /// <param name="start">
        /// The parameterless start delegate for the new thread.
        /// </param>
        /// <param name="useActiveStack">
        /// Non-zero to push and pop the associated interpreter on the active
        /// interpreter stack around the thread start routine.
        /// </param>
        /// <returns>
        /// The newly created engine thread.
        /// </returns>
        public static EngineThread Create(
            Interpreter interpreter,
            ThreadStart start,
            bool useActiveStack
            )
        {
            Interlocked.Increment(ref createCount);
            return new EngineThread(interpreter, start, useActiveStack);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method creates a new engine thread associated with the
        /// specified interpreter that runs the specified parameterized start
        /// delegate.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter to associate with the new engine thread.  This
        /// parameter may be null.
        /// </param>
        /// <param name="start">
        /// The parameterized start delegate for the new thread.
        /// </param>
        /// <param name="useActiveStack">
        /// Non-zero to push and pop the associated interpreter on the active
        /// interpreter stack around the thread start routine.
        /// </param>
        /// <returns>
        /// The newly created engine thread.
        /// </returns>
        public static EngineThread Create(
            Interpreter interpreter,
            ParameterizedThreadStart start,
            bool useActiveStack
            )
        {
            Interlocked.Increment(ref createCount);
            return new EngineThread(interpreter, start, useActiveStack);
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region IGetInterpreter Members
        //
        // NOTE: This is the primary interpreter associated with this thread,
        //       which is set only during its creation.  It is NOT owned by
        //       this object and will not be disposed.
        //
        /// <summary>
        /// The primary interpreter associated with this thread, which is set
        /// only during its creation.  It is not owned by this object and will
        /// not be disposed.
        /// </summary>
        private Interpreter interpreter;
        /// <summary>
        /// Gets the interpreter associated with this engine thread.
        /// </summary>
        public Interpreter Interpreter
        {
            get { CheckDisposed(); return interpreter; }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Public Properties
        /// <summary>
        /// The integer identifier of the system thread associated with this
        /// engine thread.
        /// </summary>
        private long id;
        /// <summary>
        /// Gets the integer identifier of the system thread associated with
        /// this engine thread.
        /// </summary>
        public long Id
        {
            get { CheckDisposed(); return id; }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Public Methods
        /// <summary>
        /// This method gets the framework thread associated with this engine
        /// thread.
        /// </summary>
        /// <returns>
        /// The framework thread associated with this engine thread, or null if
        /// none has been set.
        /// </returns>
        public Thread GetThread()
        {
            CheckDisposed();

            return thread;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method sets the framework thread associated with this engine
        /// thread.
        /// </summary>
        /// <param name="thread">
        /// The framework thread to associate with this engine thread.
        /// </param>
        public void SetThread(
            Thread thread
            )
        {
            CheckDisposed();

            this.thread = thread;
        }

        ///////////////////////////////////////////////////////////////////////

        //
        // HACK: For use by the Interpreter.ClearReferences method -AND- the
        //       test suite only.  See test "interp-1.12" for example usage.
        //
        /// <summary>
        /// This method removes the association between the specified
        /// interpreter and any engine threads that reference it, clearing their
        /// interpreter fields.  It is intended for use by the
        /// <c>Interpreter.ClearReferences</c> method and the test suite only.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter whose engine thread associations should be removed.
        /// </param>
        /// <returns>
        /// The number of engine thread associations that were removed.
        /// </returns>
        public static int CleanupInterpreter(
            Interpreter interpreter
            )
        {
            int count = 0;

            lock (syncRoot) /* TRANSACTIONAL */
            {
                if (engineThreads != null)
                {
                    IEnumerable<EngineThread> keys = new List<EngineThread>(
                        engineThreads.Keys);

                    foreach (EngineThread key in keys)
                    {
                        if (key == null)
                            continue;

                        Interpreter value;

                        if (!engineThreads.TryGetValue(key, out value))
                            continue;

                        if (!Object.ReferenceEquals(value, interpreter))
                            continue;

                        key.interpreter = null; /* FIELD */

                        if (engineThreads.Remove(key))
                            count++;
                    }
                }
            }

            return count;
        }

        ///////////////////////////////////////////////////////////////////////

        #region Public "Delegate" Methods
        /* System.Threading.ThreadStart */
        /// <summary>
        /// This method serves as the <c>System.Threading.ThreadStart</c>
        /// delegate for this engine thread.  It sets up the thread identifier,
        /// associates the interpreter, optionally pushes the active
        /// interpreter, invokes the configured start delegate, and then
        /// performs the corresponding cleanup.
        /// </summary>
        public void ThreadStart()
        {
            CheckDisposed();

            Interlocked.Increment(ref activeCount);

            try
            {
                try
                {
                    try
                    {
                        /* IGNORED */
                        SetupId(this);

                        try
                        {
                            /* IGNORED */
                            AssociateInterpreter(this);

                            try
                            {
#if NATIVE
                                RuntimeOps.RefreshNativeStackPointers(true);
#endif

                                if (threadStart != null)
                                {
                                    if (useActiveStack)
                                    {
                                        GlobalState.PushActiveInterpreter(
                                            interpreter);
                                    }

                                    try
                                    {
                                        threadStart();
                                    }
                                    finally
                                    {
                                        if (useActiveStack)
                                        {
                                            /* IGNORED */
                                            GlobalState.PopActiveInterpreter();
                                        }
                                    }
                                }
                                else if (parameterizedThreadStart != null)
                                {
                                    if (useActiveStack)
                                    {
                                        GlobalState.PushActiveInterpreter(
                                            interpreter);
                                    }

                                    try
                                    {
                                        parameterizedThreadStart(null);
                                    }
                                    finally
                                    {
                                        if (useActiveStack)
                                        {
                                            /* IGNORED */
                                            GlobalState.PopActiveInterpreter();
                                        }
                                    }
                                }
                                else
                                {
                                    TraceOps.DebugTrace(
                                        "ThreadStart: no delegates available",
                                        typeof(EngineThread).Name,
                                        TracePriority.ThreadError);
                                }
                            }
                            catch (ThreadAbortException e)
                            {
                                Thread.ResetAbort();

                                TraceOps.DebugTrace(
                                    e, typeof(EngineThread).Name,
                                    TracePriority.ThreadError2);
                            }
                            catch (ThreadInterruptedException e)
                            {
                                TraceOps.DebugTrace(
                                    e, typeof(EngineThread).Name,
                                    TracePriority.ThreadError2);
                            }
                            catch (Exception e)
                            {
                                TraceOps.DebugTrace(
                                    e, typeof(EngineThread).Name,
                                    TracePriority.ThreadError);
                            }
                            finally
                            {
                                /* IGNORED */
                                DisassociateInterpreter(this);
                            }
                        }
                        finally
                        {
                            /* IGNORED */
                            UnsetupId(this);
                        }
                    }
                    finally
                    {
                        ThreadVariable.CleanupForThread(interpreter,
                            ThreadVariable.GetThreadId());
                    }
                }
                finally
                {
                    MaybeDisposeThread(ref interpreter);
                }
            }
            finally
            {
                Interlocked.Decrement(ref activeCount);
            }
        }

        ///////////////////////////////////////////////////////////////////////

        /* System.Threading.ParameterizedThreadStart */
        /// <summary>
        /// This method serves as the
        /// <c>System.Threading.ParameterizedThreadStart</c> delegate for this
        /// engine thread.  It sets up the thread identifier, associates the
        /// interpreter, optionally pushes the active interpreter, invokes the
        /// configured parameterized start delegate, and then performs the
        /// corresponding cleanup.
        /// </summary>
        /// <param name="obj">
        /// The opaque data to pass to the parameterized start delegate.  This
        /// parameter may be null.
        /// </param>
        public void ParameterizedThreadStart(
            object obj
            )
        {
            CheckDisposed();

            Interlocked.Increment(ref activeCount);

            try
            {
                try
                {
                    /* IGNORED */
                    SetupId(this);

                    try
                    {
                        /* IGNORED */
                        AssociateInterpreter(this);

                        try
                        {
#if NATIVE
                            RuntimeOps.RefreshNativeStackPointers(true);
#endif

                            if (parameterizedThreadStart != null)
                            {
                                if (useActiveStack)
                                {
                                    GlobalState.PushActiveInterpreter(
                                        interpreter);
                                }

                                try
                                {
                                    parameterizedThreadStart(obj);
                                }
                                finally
                                {
                                    if (useActiveStack)
                                    {
                                        /* IGNORED */
                                        GlobalState.PopActiveInterpreter();
                                    }
                                }
                            }
                            else
                            {
                                TraceOps.DebugTrace(
                                    "ParameterizedThreadStart: no delegate available",
                                    typeof(EngineThread).Name,
                                    TracePriority.ThreadError);
                            }
                        }
                        catch (ThreadAbortException e)
                        {
                            Thread.ResetAbort();

                            TraceOps.DebugTrace(
                                e, typeof(EngineThread).Name,
                                TracePriority.ThreadError2);
                        }
                        catch (ThreadInterruptedException e)
                        {
                            TraceOps.DebugTrace(
                                e, typeof(EngineThread).Name,
                                TracePriority.ThreadError2);
                        }
                        catch (Exception e)
                        {
                            TraceOps.DebugTrace(
                                e, typeof(EngineThread).Name,
                                TracePriority.ThreadError);
                        }
                        finally
                        {
                            /* IGNORED */
                            DisassociateInterpreter(this);
                        }
                    }
                    finally
                    {
                        /* IGNORED */
                        UnsetupId(this);
                    }
                }
                finally
                {
                    MaybeDisposeThread(ref interpreter);
                }
            }
            finally
            {
                Interlocked.Decrement(ref activeCount);
            }
        }
        #endregion
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Private Static Methods
        /// <summary>
        /// This method checks whether the specified engine thread does not yet
        /// have an integer identifier assigned to it, emitting a trace message
        /// if it has one that does not match the current system thread.
        /// </summary>
        /// <param name="engineThread">
        /// The engine thread to check.  If this parameter is null, false is
        /// returned.
        /// </param>
        /// <param name="prefix">
        /// The prefix to include in any emitted trace message.
        /// </param>
        /// <returns>
        /// True if the engine thread has no identifier assigned yet; otherwise,
        /// false.
        /// </returns>
        private static bool CheckId(
            EngineThread engineThread,
            string prefix
            )
        {
            long currentId = 0;

            return CheckId(engineThread, prefix, ref currentId);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method checks whether the specified engine thread does not yet
        /// have an integer identifier assigned to it, emitting a trace message
        /// if it has one that does not match the current system thread, and
        /// reports the current system thread identifier.
        /// </summary>
        /// <param name="engineThread">
        /// The engine thread to check.  If this parameter is null, false is
        /// returned.
        /// </param>
        /// <param name="prefix">
        /// The prefix to include in any emitted trace message.
        /// </param>
        /// <param name="currentId">
        /// Upon success, receives the current system thread identifier.
        /// </param>
        /// <returns>
        /// True if the engine thread has no identifier assigned yet; otherwise,
        /// false.
        /// </returns>
        private static bool CheckId(
            EngineThread engineThread,
            string prefix,
            ref long currentId
            )
        {
            //
            // NOTE: Make sure that a valid integer identifier is
            //       assigned this thread.  This may be a managed
            //       or native thread identifier, depending on the
            //       selected build options.
            //
            if (engineThread == null)
                return false;

            long instanceId = engineThread.id;

            if (instanceId != 0)
            {
                currentId = GlobalState.GetCurrentSystemThreadId();

                if (instanceId != currentId)
                {
                    TraceOps.DebugTrace(String.Format(
                        "{0}: instance {1} system thread Id {2} does " +
                        "not match current system thread Id {3}",
                        prefix, FormatOps.WrapHashCode(engineThread),
                        instanceId, currentId), typeof(EngineThread).Name,
                        TracePriority.ThreadError);
                }

                return false;
            }

            return true;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method assigns the current system thread identifier to the
        /// specified engine thread.
        /// </summary>
        /// <param name="engineThread">
        /// The engine thread to assign the identifier to.  If this parameter is
        /// null, false is returned.
        /// </param>
        /// <returns>
        /// True if the identifier was assigned; otherwise, false.
        /// </returns>
        private static bool SetupId(
            EngineThread engineThread
            )
        {
            if (engineThread == null)
                return false;

            long currentId = 0;

            if (!CheckId(engineThread, "SetupId", ref currentId))
                return false;

            engineThread.id = currentId;
            return true;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method clears the integer identifier previously assigned to the
        /// specified engine thread.
        /// </summary>
        /// <param name="engineThread">
        /// The engine thread to clear the identifier from.  If this parameter
        /// is null, false is returned.
        /// </param>
        /// <returns>
        /// True if the identifier was cleared; otherwise, false.
        /// </returns>
        private static bool UnsetupId(
            EngineThread engineThread
            )
        {
            if (engineThread == null)
                return false;

            if (!CheckId(engineThread, "UnsetupId"))
                return false;

            engineThread.id = 0;
            return true;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method adds an association between the specified engine thread
        /// and its interpreter to the global engine thread tracking list.
        /// </summary>
        /// <param name="engineThread">
        /// The engine thread to associate with its interpreter.  If this
        /// parameter is null, false is returned.
        /// </param>
        /// <returns>
        /// True if the association was added; otherwise, false.
        /// </returns>
        private static bool AssociateInterpreter(
            EngineThread engineThread
            )
        {
            if (engineThread == null)
                return false;

            Interpreter newInterpreter = engineThread.Interpreter;

            if (newInterpreter == null)
                return false;

            lock (syncRoot) /* TRANSACTIONAL */
            {
                if (engineThreads == null)
                    engineThreads = new EngineThreadDictionary();

                Interpreter oldInterpreter;

                if (engineThreads.TryGetValue(
                        engineThread, out oldInterpreter))
                {
                    return false;
                }
                else
                {
                    engineThreads.Add(engineThread, newInterpreter);
                    return true;
                }
            }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method removes the association between the specified engine
        /// thread and its interpreter from the global engine thread tracking
        /// list.
        /// </summary>
        /// <param name="engineThread">
        /// The engine thread to disassociate from its interpreter.  If this
        /// parameter is null, false is returned.
        /// </param>
        /// <returns>
        /// True if the association was removed; otherwise, false.
        /// </returns>
        private static bool DisassociateInterpreter(
            EngineThread engineThread
            )
        {
            if (engineThread == null)
                return false;

            Interpreter newInterpreter = engineThread.Interpreter;

            if (newInterpreter == null)
                return false;

            lock (syncRoot) /* TRANSACTIONAL */
            {
                if (engineThreads == null)
                    return false;

                Interpreter oldInterpreter;

                if (!engineThreads.TryGetValue(
                        engineThread, out oldInterpreter))
                {
                    return false;
                }

                if (!Object.ReferenceEquals(oldInterpreter, newInterpreter))
                    return false;

                return engineThreads.Remove(engineThread);
            }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method conditionally disposes any per-thread state owned by the
        /// specified interpreter and then clears the reference to it.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter whose per-thread state should be disposed.  Upon
        /// return, this parameter is set to null.
        /// </param>
        private static void MaybeDisposeThread(
            ref Interpreter interpreter /* in, out */
            )
        {
            if (interpreter != null)
            {
                /* IGNORED */
                interpreter.MaybeDisposeThread();
                interpreter = null;
            }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region System.Object Overrides
        /// <summary>
        /// This method returns a string representation of this engine thread,
        /// including its identifier, associated interpreter, and thread.
        /// </summary>
        /// <returns>
        /// A string representation of this engine thread.
        /// </returns>
        public override string ToString()
        {
            CheckDisposed();

            return StringList.MakeList("id", id, "interpreter",
                FormatOps.InterpreterNoThrow(interpreter),
                "thread", FormatOps.ThreadIdNoThrow(thread));
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region IDisposable "Pattern" Members
        /// <summary>
        /// Non-zero if this object has been disposed.
        /// </summary>
        private bool disposed;
        /// <summary>
        /// This method throws an <see cref="ObjectDisposedException" /> if this
        /// object has been disposed and the interpreter is configured to throw
        /// on disposed objects.
        /// </summary>
        private void CheckDisposed() /* throw */
        {
#if THROW_ON_DISPOSED
            if (disposed && Engine.IsThrowOnDisposed(interpreter, null))
                throw new ObjectDisposedException(typeof(EngineThread).Name);
#endif
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method releases the resources used by this object.
        /// </summary>
        /// <param name="disposing">
        /// Non-zero if this method is being called from the
        /// <see cref="Dispose()" /> method; zero if it is being called from the
        /// finalizer.
        /// </param>
        private /* protected virtual */ void Dispose(
            bool disposing
            )
        {
            TraceOps.DebugTrace(String.Format(
                "Dispose: called, disposing = {0}, disposed = {1}",
                disposing, disposed), typeof(EngineThread).Name,
                TracePriority.CleanupDebug);

            if (!disposed)
            {
                if (disposing)
                {
                    ////////////////////////////////////
                    // dispose managed resources here...
                    ////////////////////////////////////

                    interpreter = null; /* NOT OWNED, DO NOT DISPOSE. */
                    threadStart = null;
                    parameterizedThreadStart = null;
                    thread = null; /* NOT OWNED, DO NOT DISPOSE. */
                }

                //
                // NOTE: Make sure the thread is removed
                //       from the global tracking list.
                //
                /* IGNORED */
                GlobalState.RemoveThread(this);

                //////////////////////////////////////
                // release unmanaged resources here...
                //////////////////////////////////////

                disposed = true;
            }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region IDisposable Members
        /// <summary>
        /// This method releases all resources used by this object.
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Destructor
        /// <summary>
        /// Finalizes this object, releasing any unmanaged resources.
        /// </summary>
        ~EngineThread()
        {
            Dispose(false);
        }
        #endregion
    }
}
