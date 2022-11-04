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
    [ObjectId("e1a3509f-1b6b-4940-8cfc-7d21c2d81c93")]
    internal sealed class EngineThread : IGetInterpreter, IDisposable
    {
        #region Private Static Data
        //
        // NOTE: This static field is used to synchronize access to the list
        //       of engine threads (below).
        //
        private static readonly object syncRoot = new object();

        ///////////////////////////////////////////////////////////////////////

        //
        // NOTE: This static field is used to keep track of the associations
        //       between engine threads and interpreters.  Any engine thread
        //       may have at most one valid interpreter associated with it.
        //
        private static EngineThreadDictionary engineThreads;

        ///////////////////////////////////////////////////////////////////////

        //
        // NOTE: These static fields are used to keep track of how many of
        //       these objects have ever been created and how many are now
        //       active.
        //
        private static int createCount;
        private static int activeCount;
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Private Data
        //
        // NOTE: This is the parameterless start delegate for this thread.  It
        //       is only used when handling the ThreadStart delegate.
        //
        private ThreadStart threadStart;

        ///////////////////////////////////////////////////////////////////////

        //
        // NOTE: This is the parameterized start delegate for this thread.  It
        //       is used when handling the ParameterizedThreadStart delegate
        //       and/or the ThreadStart delegate if the parameterless start
        //       delegate is not available.
        //
        private ParameterizedThreadStart parameterizedThreadStart;

        ///////////////////////////////////////////////////////////////////////

        //
        // NOTE: This is the framework thread associated with this thread.  It
        //       is NOT owned by this object and will not be disposed.
        //
#if MONO_BUILD
#pragma warning disable 414
#endif
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
        private bool useActiveStack;
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Private Constructors
        private EngineThread()
        {
            /* IGNORED */
            GlobalState.AddThread(this);
        }

        ///////////////////////////////////////////////////////////////////////

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
        private Interpreter interpreter;
        public Interpreter Interpreter
        {
            get { CheckDisposed(); return interpreter; }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Public Properties
        private long id;
        public long Id
        {
            get { CheckDisposed(); return id; }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Public Methods
        public Thread GetThread()
        {
            CheckDisposed();

            return thread;
        }

        ///////////////////////////////////////////////////////////////////////

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
        private static bool CheckId(
            EngineThread engineThread,
            string prefix
            )
        {
            long currentId = 0;

            return CheckId(engineThread, prefix, ref currentId);
        }

        ///////////////////////////////////////////////////////////////////////

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
        private bool disposed;
        private void CheckDisposed() /* throw */
        {
#if THROW_ON_DISPOSED
            if (disposed && Engine.IsThrowOnDisposed(interpreter, null))
                throw new ObjectDisposedException(typeof(EngineThread).Name);
#endif
        }

        ///////////////////////////////////////////////////////////////////////

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
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Destructor
        ~EngineThread()
        {
            Dispose(false);
        }
        #endregion
    }
}
