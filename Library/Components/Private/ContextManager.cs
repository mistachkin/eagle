/*
 * ContextManager.cs --
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
using Eagle._Containers.Private;
using Eagle._Containers.Public;
using Eagle._Interfaces.Private;
using Eagle._Interfaces.Public;

namespace Eagle._Components.Private
{
    [ObjectId("53f5f2e0-ea9c-46a8-b584-779ef217beb5")]
    internal sealed class ContextManager :
            IContextManager, IHaveInterpreter, IDisposable
    {
        #region Private Static Data
        private static object syncRoot = new object();

        ///////////////////////////////////////////////////////////////////////

        private static LocalDataStoreSlot engineSlot;
        private static LocalDataStoreSlot interactiveSlot;
        private static LocalDataStoreSlot testSlot;
        private static LocalDataStoreSlot variableSlot;
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Private Data
        private IEngineContext previousEngineContext;
        private IInteractiveContext previousInteractiveContext;
        private ITestContext previousTestContext;
        private IVariableContext previousVariableContext;
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Public Static Methods
        public static void Initialize()
        {
            //
            // NOTE: These MUST to be done prior to evaluating any scripts or
            //       call frame handling (and a bunch of other stuff) will not
            //       work properly.
            //
            lock (syncRoot)
            {
                if (engineSlot == null)
                    engineSlot = Thread.AllocateDataSlot();

                if (interactiveSlot == null)
                    interactiveSlot = Thread.AllocateDataSlot();

                if (testSlot == null)
                    testSlot = Thread.AllocateDataSlot();

                if (variableSlot == null)
                    variableSlot = Thread.AllocateDataSlot();
            }
        }

        ///////////////////////////////////////////////////////////////////////

        //
        // NOTE: This method is called via the interpreter disposal pipeline.
        //
        public static int Purge(
            Interpreter interpreter,
            bool nonPrimary,
            bool global
            )
        {
            return PrivatePurgeEngineContexts(
                    interpreter, nonPrimary, global) +
                PrivatePurgeInteractiveContexts(
                    interpreter, nonPrimary, global) +
                PrivatePurgeTestContexts(
                    interpreter, nonPrimary, global) +
                PrivatePurgeVariableContexts(
                    interpreter, nonPrimary, global);
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Public Constructors
        public ContextManager(
            Interpreter interpreter
            )
        {
            this.interpreter = interpreter;
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Private Static Methods
        private static bool FreeVariableContext(
            IVariableContext variableContext,
            bool global
            )
        {
            //
            // HACK: *SPECIAL CASE* We cannot dispose the global call frame
            //       unless we are [also] disposing of the interpreter itself;
            //       therefore, use the special Free method here instead of
            //       the Dispose method.  The Free method is guaranteed to do
            //       the right thing with regard to the global call frame
            //       (assuming the "global" parameter is correct).
            //
            if (variableContext == null)
                return false;

            //
            // HACK: *SPECIAL CASE* If we free this variable context and its
            //       global state, there is no need to finalize it.
            //
            variableContext.Free(global);

            if (global)
                GC.SuppressFinalize(variableContext);

            variableContext = null;

            return true;
        }

        ///////////////////////////////////////////////////////////////////////

        private static int PrivatePurgeEngineContexts(
            Interpreter interpreter, /* NOT USED */
            bool nonPrimary,
            bool global /* NOT USED */
            )
        {
            try
            {
                LocalDataStoreSlot localSlot;

                lock (syncRoot)
                {
                    localSlot = engineSlot;
                }

                if (localSlot == null)
                    return 0;

                InterpreterEngineContextDictionary contexts =
                    Thread.GetData(localSlot)
                    as InterpreterEngineContextDictionary; /* throw */

                if (contexts == null)
                    return 0;

                IEnumerable<IInterpreter> localInterpreters =
                    GlobalState.FilterInterpretersToPurge(
                        contexts.Keys, nonPrimary);

                if (localInterpreters == null)
                    return 0;

                int count = 0;

                foreach (IInterpreter localInterpreter in localInterpreters)
                {
                    //
                    // NOTE: There should not be any null values in
                    //       the list of interpreters; if there are
                    //       any, just skip over them.
                    //
                    if (localInterpreter == null)
                        continue;

                    //
                    // NOTE: Does the requested interpreter have an
                    //       entry in the dictionary?  It should,
                    //       that is where we got it from originally.
                    //
                    IEngineContext context;

                    if (contexts.RemoveAndReturn(
                            localInterpreter, out context))
                    {
                        count++;
                    }

                    /* IGNORED */
                    ContextOps.DisposeThread(context);

                    //
                    // NOTE: Release local context reference now.
                    //
                    context = null;

                    //
                    // NOTE: If there are no more contexts present,
                    //       free the collection and the thread data
                    //       slot.
                    //
                    if (contexts.Count == 0)
                    {
                        //
                        // NOTE: Release local reference now.
                        //
                        contexts = null;

                        //
                        // NOTE: Clear it in the per-thread data.
                        //
                        Thread.SetData(localSlot, contexts);

                        //
                        // NOTE: There is nothing more to do.
                        //
                        break;
                    }
                }

                //
                // NOTE: Even if the loop above was skipped completely,
                //       clear out the per-thread data if necessary.
                //
                if ((contexts != null) && (contexts.Count == 0))
                {
                    //
                    // NOTE: Release local reference now.
                    //
                    contexts = null;

                    //
                    // NOTE: Clear it in the per-thread data.
                    //
                    Thread.SetData(localSlot, contexts);
                }

                TraceOps.DebugTrace(String.Format(
                    "PrivatePurgeEngineContexts: nonPrimary = {0}, " +
                    "count = {1}", nonPrimary, count),
                    typeof(ContextManager).Name, TracePriority.EngineDebug);

                return count;
            }
            catch
            {
                // do nothing.
            }

            return 0;
        }

        ///////////////////////////////////////////////////////////////////////

        private static int PrivatePurgeInteractiveContexts(
            Interpreter interpreter, /* NOT USED */
            bool nonPrimary,
            bool global /* NOT USED */
            )
        {
            try
            {
                LocalDataStoreSlot localSlot;

                lock (syncRoot)
                {
                    localSlot = interactiveSlot;
                }

                if (localSlot == null)
                    return 0;

                InterpreterInteractiveContextDictionary contexts =
                    Thread.GetData(localSlot) as
                    InterpreterInteractiveContextDictionary; /* throw */

                if (contexts == null)
                    return 0;

                IEnumerable<IInterpreter> localInterpreters =
                    GlobalState.FilterInterpretersToPurge(
                        contexts.Keys, nonPrimary);

                if (localInterpreters == null)
                    return 0;

                int count = 0;

                foreach (IInterpreter localInterpreter in localInterpreters)
                {
                    //
                    // NOTE: There should not be any null values in
                    //       the list of interpreters; if there are
                    //       any, just skip over them.
                    //
                    if (localInterpreter == null)
                        continue;

                    //
                    // NOTE: Does the requested interpreter have an
                    //       entry in the dictionary?  It should,
                    //       that is where we got it from originally.
                    //
                    IInteractiveContext context;

                    if (contexts.RemoveAndReturn(
                            localInterpreter, out context))
                    {
                        count++;
                    }

                    /* IGNORED */
                    ContextOps.DisposeThread(context);

                    //
                    // NOTE: Release local context reference now.
                    //
                    context = null;

                    //
                    // NOTE: If there are no more contexts present,
                    //       free the collection and the thread data
                    //       slot.
                    //
                    if (contexts.Count == 0)
                    {
                        //
                        // NOTE: Release local reference now.
                        //
                        contexts = null;

                        //
                        // NOTE: Clear it in the per-thread data.
                        //
                        Thread.SetData(localSlot, contexts);

                        //
                        // NOTE: There is nothing more to do.
                        //
                        break;
                    }
                }

                //
                // NOTE: Even if the loop above was skipped completely,
                //       clear out the per-thread data if necessary.
                //
                if ((contexts != null) && (contexts.Count == 0))
                {
                    //
                    // NOTE: Release local reference now.
                    //
                    contexts = null;

                    //
                    // NOTE: Clear it in the per-thread data.
                    //
                    Thread.SetData(localSlot, contexts);
                }

                TraceOps.DebugTrace(String.Format(
                    "PrivatePurgeInteractiveContexts: nonPrimary = {0}, " +
                    "count = {1}", nonPrimary, count),
                    typeof(ContextManager).Name, TracePriority.EngineDebug);

                return count;
            }
            catch
            {
                // do nothing.
            }

            return 0;
        }

        ///////////////////////////////////////////////////////////////////////

        private static int PrivatePurgeTestContexts(
            Interpreter interpreter, /* NOT USED */
            bool nonPrimary,
            bool global /* NOT USED */
            )
        {
            try
            {
                LocalDataStoreSlot localSlot;

                lock (syncRoot)
                {
                    localSlot = testSlot;
                }

                if (localSlot == null)
                    return 0;

                InterpreterTestContextDictionary contexts =
                    Thread.GetData(localSlot) as
                    InterpreterTestContextDictionary; /* throw */

                if (contexts == null)
                    return 0;

                IEnumerable<IInterpreter> localInterpreters =
                    GlobalState.FilterInterpretersToPurge(
                        contexts.Keys, nonPrimary);

                if (localInterpreters == null)
                    return 0;

                int count = 0;

                foreach (IInterpreter localInterpreter in localInterpreters)
                {
                    //
                    // NOTE: There should not be any null values in
                    //       the list of interpreters; if there are
                    //       any, just skip over them.
                    //
                    if (localInterpreter == null)
                        continue;

                    //
                    // NOTE: Does the requested interpreter have an
                    //       entry in the dictionary?  It should,
                    //       that is where we got it from originally.
                    //
                    ITestContext context;

                    if (contexts.RemoveAndReturn(
                            localInterpreter, out context))
                    {
                        count++;
                    }

                    /* IGNORED */
                    ContextOps.DisposeThread(context);

                    //
                    // NOTE: Release local context reference now.
                    //
                    context = null;

                    //
                    // NOTE: If there are no more contexts present,
                    //       free the collection and the thread data
                    //       slot.
                    //
                    if (contexts.Count == 0)
                    {
                        //
                        // NOTE: Release local reference now.
                        //
                        contexts = null;

                        //
                        // NOTE: Clear it in the per-thread data.
                        //
                        Thread.SetData(localSlot, contexts);

                        //
                        // NOTE: There is nothing more to do.
                        //
                        break;
                    }
                }

                //
                // NOTE: Even if the loop above was skipped completely,
                //       clear out the per-thread data if necessary.
                //
                if ((contexts != null) && (contexts.Count == 0))
                {
                    //
                    // NOTE: Release local reference now.
                    //
                    contexts = null;

                    //
                    // NOTE: Clear it in the per-thread data.
                    //
                    Thread.SetData(localSlot, contexts);
                }

                TraceOps.DebugTrace(String.Format(
                    "PrivatePurgeTestContexts: nonPrimary = {0}, " +
                    "count = {1}", nonPrimary, count),
                    typeof(ContextManager).Name, TracePriority.EngineDebug);

                return count;
            }
            catch
            {
                // do nothing.
            }

            return 0;
        }

        ///////////////////////////////////////////////////////////////////////

        private static int PrivatePurgeVariableContexts(
            Interpreter interpreter,
            bool nonPrimary,
            bool global
            )
        {
            try
            {
                LocalDataStoreSlot localSlot;

                lock (syncRoot)
                {
                    localSlot = variableSlot;
                }

                if (localSlot == null)
                    return 0;

                InterpreterVariableContextDictionary contexts =
                    Thread.GetData(localSlot) as
                    InterpreterVariableContextDictionary; /* throw */

                if (contexts == null)
                    return 0;

                IEnumerable<IInterpreter> localInterpreters =
                    GlobalState.FilterInterpretersToPurge(
                        contexts.Keys, nonPrimary);

                if (localInterpreters == null)
                    return 0;

                int count = 0;

                foreach (IInterpreter localInterpreter in localInterpreters)
                {
                    //
                    // NOTE: There should not be any null values in
                    //       the list of interpreters; if there are
                    //       any, just skip over them.
                    //
                    if (localInterpreter == null)
                        continue;

                    //
                    // NOTE: Does the requested interpreter have an
                    //       entry in the dictionary?  It should,
                    //       that is where we got it from originally.
                    //
                    IVariableContext context;

                    if (contexts.RemoveAndReturn(
                            localInterpreter, out context))
                    {
                        count++;
                    }

                    /* IGNORED */
                    FreeVariableContext(
                        context, global && Object.ReferenceEquals(
                        localInterpreter, interpreter));

                    //
                    // NOTE: If there are no more contexts present,
                    //       free the collection and the thread data
                    //       slot.
                    //
                    if (contexts.Count == 0)
                    {
                        //
                        // NOTE: Release local reference now.
                        //
                        contexts = null;

                        //
                        // NOTE: Clear it in the per-thread data.
                        //
                        Thread.SetData(localSlot, contexts);

                        //
                        // NOTE: There is nothing more to do.
                        //
                        break;
                    }
                }

                //
                // NOTE: Even if the loop above was skipped completely,
                //       clear out the per-thread data if necessary.
                //
                if ((contexts != null) && (contexts.Count == 0))
                {
                    //
                    // NOTE: Release local reference now.
                    //
                    contexts = null;

                    //
                    // NOTE: Clear it in the per-thread data.
                    //
                    Thread.SetData(localSlot, contexts);
                }

                TraceOps.DebugTrace(String.Format(
                    "PrivatePurgeVariableContexts: nonPrimary = {0}, " +
                    "count = {1}", nonPrimary, count),
                    typeof(ContextManager).Name, TracePriority.EngineDebug);

                return count;
            }
            catch
            {
                // do nothing.
            }

            return 0;
        }

        ///////////////////////////////////////////////////////////////////////

        private static int PrivateGetEngineContextCount()
        {
            try
            {
                LocalDataStoreSlot localSlot;

                lock (syncRoot)
                {
                    localSlot = engineSlot;
                }

                if (localSlot == null)
                    return 0;

                InterpreterEngineContextDictionary contexts =
                    Thread.GetData(localSlot) as
                    InterpreterEngineContextDictionary; /* throw */

                if (contexts != null)
                    return contexts.Count;
            }
            catch
            {
                // do nothing.
            }

            return 0;
        }

        ///////////////////////////////////////////////////////////////////////

        private static int PrivateGetInteractiveContextCount()
        {
            try
            {
                LocalDataStoreSlot localSlot;

                lock (syncRoot)
                {
                    localSlot = interactiveSlot;
                }

                if (localSlot == null)
                    return 0;

                InterpreterInteractiveContextDictionary contexts =
                    Thread.GetData(localSlot) as
                    InterpreterInteractiveContextDictionary; /* throw */

                if (contexts != null)
                    return contexts.Count;
            }
            catch
            {
                // do nothing.
            }

            return 0;
        }

        ///////////////////////////////////////////////////////////////////////

        private static int PrivateGetTestContextCount()
        {
            try
            {
                LocalDataStoreSlot localSlot;

                lock (syncRoot)
                {
                    localSlot = testSlot;
                }

                if (localSlot == null)
                    return 0;

                InterpreterTestContextDictionary contexts =
                    Thread.GetData(localSlot) as
                    InterpreterTestContextDictionary; /* throw */

                if (contexts != null)
                    return contexts.Count;
            }
            catch
            {
                // do nothing.
            }

            return 0;
        }

        ///////////////////////////////////////////////////////////////////////

        private static int PrivateGetVariableContextCount()
        {
            try
            {
                LocalDataStoreSlot localSlot;

                lock (syncRoot)
                {
                    localSlot = variableSlot;
                }

                if (localSlot == null)
                    return 0;

                InterpreterVariableContextDictionary contexts =
                    Thread.GetData(localSlot) as
                    InterpreterVariableContextDictionary; /* throw */

                if (contexts != null)
                    return contexts.Count;
            }
            catch
            {
                // do nothing.
            }

            return 0;
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Private Methods
        private bool PrivateReleaseEngineContext(
            bool global
            )
        {
            Result error = null;

            return PrivateReleaseEngineContext(global, ref error);
        }

        ///////////////////////////////////////////////////////////////////////

        private bool PrivateReleaseEngineContext(
            bool global, /* NOT USED */
            ref Result error
            )
        {
            try
            {
                if (interpreter == null)
                {
                    error = "invalid interpreter";
                    return false;
                }

                LocalDataStoreSlot localSlot;

                lock (syncRoot)
                {
                    localSlot = engineSlot;
                }

                if (localSlot == null)
                {
                    error = "engine context slot is invalid";
                    return false;
                }

                //
                // NOTE: Try to obtain the per-interpreter dictionary
                //       of contexts for this thread.
                //
                InterpreterEngineContextDictionary contexts =
                    Thread.GetData(localSlot) as
                    InterpreterEngineContextDictionary; /* throw */

                if (contexts != null)
                {
                    //
                    // NOTE: Does the requested interpreter have an
                    //       entry in the dictionary?  If so, remove
                    //       it and then dispose it.
                    //
                    IEngineContext context;

                    /* IGNORED */
                    contexts.RemoveAndReturn(interpreter, out context);

                    /* IGNORED */
                    ContextOps.DisposeThread(context);

                    //
                    // NOTE: Release local context reference now.
                    //
                    context = null;

                    //
                    // NOTE: If there are no more contexts present,
                    //       free the collection and the thread data
                    //       slot.
                    //
                    if (contexts.Count == 0)
                    {
                        //
                        // NOTE: Release local reference now.
                        //
                        contexts = null;

                        //
                        // NOTE: Clear it in the per-thread data.
                        //
                        Thread.SetData(localSlot, contexts);
                    }
                }

                //
                // NOTE: Invalidate the cached context for this
                //       interpreter.
                //
                previousEngineContext = null;

                return true;
            }
            catch (Exception e)
            {
                error = e;
            }

            return false;
        }

        ///////////////////////////////////////////////////////////////////////

        private IEngineContext PrivateGetEngineContext(
            bool create
            )
        {
            Result error = null;

            return PrivateGetEngineContext(create, ref error);
        }

        ///////////////////////////////////////////////////////////////////////

        private IEngineContext PrivateGetEngineContext(
            bool create,
            ref Result error
            )
        {
            try
            {
                if (interpreter == null)
                {
                    error = "invalid interpreter";
                    return null;
                }

                IEngineContext result = previousEngineContext;

                if (ContextOps.CanUseThread(result))
                    return result;

                LocalDataStoreSlot localSlot;

                lock (syncRoot)
                {
                    localSlot = engineSlot;
                }

                if (localSlot == null)
                {
                    error = "engine context slot is invalid";
                    return null;
                }

                //
                // NOTE: Try to obtain the per-interpreter dictionary
                //       of engine contexts for this thread.
                //
                InterpreterEngineContextDictionary contexts =
                    Thread.GetData(localSlot) as
                    InterpreterEngineContextDictionary; /* throw */

                //
                // NOTE: Has the dictionary been created yet and/or can
                //       we create it now?
                //
                if (create && (contexts == null))
                {
                    //
                    // NOTE: Nope, create it now.
                    //
                    contexts = new InterpreterEngineContextDictionary();

                    //
                    // NOTE: Store it in the per-thread data.
                    //
                    Thread.SetData(localSlot, contexts); /* throw */
                }

                //
                // NOTE: Is the dictionary available now (if not,
                //       we have been forbidden by the caller from
                //       automatically creating it).
                //
                if (contexts != null)
                {
                    //
                    // NOTE: Does the requested interpreter have an
                    //       entry in the dictionary?  If so, grab
                    //       and return it.
                    //
                    if (!contexts.TryGetValue(
                            interpreter, out result))
                    {
                        //
                        // NOTE: Now, create one and add it to the
                        //       dictionary of engine contexts
                        //       (which is stored via the per-thread
                        //       data slot).
                        //
                        result = new EngineContext(interpreter,
                            ContextOps.GetCurrentThreadId());

                        contexts.Add(interpreter, result);
                    }

                    //
                    // NOTE: Save the resulting context for next
                    //       time.
                    //
                    previousEngineContext = result;

                    return result;
                }
                else
                {
                    error = "engine contexts not available";
                }
            }
            catch (Exception e)
            {
                error = e;
            }

            return null;
        }

        ///////////////////////////////////////////////////////////////////////

        private bool PrivateReleaseInteractiveContext(
            bool global
            )
        {
            Result error = null;

            return PrivateReleaseInteractiveContext(global, ref error);
        }

        ///////////////////////////////////////////////////////////////////////

        private bool PrivateReleaseInteractiveContext(
            bool global, /* NOT USED */
            ref Result error
            )
        {
            try
            {
                if (interpreter == null)
                {
                    error = "invalid interpreter";
                    return false;
                }

                LocalDataStoreSlot localSlot;

                lock (syncRoot)
                {
                    localSlot = interactiveSlot;
                }

                if (localSlot == null)
                {
                    error = "interactive context slot is invalid";
                    return false;
                }

                //
                // NOTE: Try to obtain the per-interpreter dictionary
                //       of contexts for this thread.
                //
                InterpreterInteractiveContextDictionary contexts =
                    Thread.GetData(localSlot) as
                    InterpreterInteractiveContextDictionary; /* throw */

                if (contexts != null)
                {
                    //
                    // NOTE: Does the requested interpreter have an
                    //       entry in the dictionary?  If so, remove
                    //       it and then dispose it.
                    //
                    IInteractiveContext context;

                    /* IGNORED */
                    contexts.RemoveAndReturn(interpreter, out context);

                    /* IGNORED */
                    ContextOps.DisposeThread(context);

                    //
                    // NOTE: Release local context reference now.
                    //
                    context = null;

                    //
                    // NOTE: If there are no more contexts present,
                    //       free the collection and the thread data
                    //       slot.
                    //
                    if (contexts.Count == 0)
                    {
                        //
                        // NOTE: Release local reference now.
                        //
                        contexts = null;

                        //
                        // NOTE: Clear it in the per-thread data.
                        //
                        Thread.SetData(localSlot, contexts);
                    }
                }

                //
                // NOTE: Invalidate the cached context for this
                //       interpreter.
                //
                previousInteractiveContext = null;

                return true;
            }
            catch (Exception e)
            {
                error = e;
            }

            return false;
        }

        ///////////////////////////////////////////////////////////////////////

        private IInteractiveContext PrivateGetInteractiveContext(
            bool create
            )
        {
            Result error = null;

            return PrivateGetInteractiveContext(create, ref error);
        }

        ///////////////////////////////////////////////////////////////////////

        private IInteractiveContext PrivateGetInteractiveContext(
            bool create,
            ref Result error
            )
        {
            try
            {
                if (interpreter == null)
                {
                    error = "invalid interpreter";
                    return null;
                }

                IInteractiveContext result = previousInteractiveContext;

                if (ContextOps.CanUseThread(result))
                    return result;

                LocalDataStoreSlot localSlot;

                lock (syncRoot)
                {
                    localSlot = interactiveSlot;
                }

                if (localSlot == null)
                {
                    error = "interactive context slot is invalid";
                    return null;
                }

                //
                // NOTE: Try to obtain the per-interpreter dictionary
                //       of interactive contexts for this thread.
                //
                InterpreterInteractiveContextDictionary contexts =
                    Thread.GetData(localSlot) as
                    InterpreterInteractiveContextDictionary; /* throw */

                //
                // NOTE: Has the dictionary been created yet and/or can
                //       we create it now?
                //
                if (create && (contexts == null))
                {
                    //
                    // NOTE: Nope, create it now.
                    //
                    contexts = new InterpreterInteractiveContextDictionary();

                    //
                    // NOTE: Store it in the per-thread data.
                    //
                    Thread.SetData(localSlot, contexts); /* throw */
                }

                //
                // NOTE: Is the dictionary available now (if not,
                //       we have been forbidden by the caller from
                //       automatically creating it).
                //
                if (contexts != null)
                {
                    //
                    // NOTE: Does the requested interpreter have an
                    //       entry in the dictionary?  If so, grab
                    //       and return it.
                    //
                    if (!contexts.TryGetValue(
                            interpreter, out result))
                    {
                        //
                        // NOTE: Now, create one and add it to the
                        //       dictionary of interactive contexts
                        //       (which is stored via the per-thread
                        //       data slot).
                        //
#if SHELL
                        result = new InteractiveContext(interpreter,
                            ContextOps.GetCurrentThreadId(),
                            interpreter.InternalInteractiveLoopSemaphore);
#else
                        result = new InteractiveContext(interpreter,
                            ContextOps.GetCurrentThreadId());
#endif

                        contexts.Add(interpreter, result);
                    }

                    //
                    // NOTE: Save the resulting context for next
                    //       time.
                    //
                    previousInteractiveContext = result;

                    return result;
                }
                else
                {
                    error = "interactive contexts not available";
                }
            }
            catch (Exception e)
            {
                error = e;
            }

            return null;
        }

        ///////////////////////////////////////////////////////////////////////

        private bool PrivateReleaseTestContext(
            bool global
            )
        {
            Result error = null;

            return PrivateReleaseTestContext(global, ref error);
        }

        ///////////////////////////////////////////////////////////////////////

        private bool PrivateReleaseTestContext(
            bool global, /* NOT USED */
            ref Result error
            )
        {
            try
            {
                if (interpreter == null)
                {
                    error = "invalid interpreter";
                    return false;
                }

                LocalDataStoreSlot localSlot;

                lock (syncRoot)
                {
                    localSlot = testSlot;
                }

                if (localSlot == null)
                {
                    error = "test context slot is invalid";
                    return false;
                }

                //
                // NOTE: Try to obtain the per-interpreter dictionary
                //       of contexts for this thread.
                //
                InterpreterTestContextDictionary contexts =
                    Thread.GetData(localSlot) as
                    InterpreterTestContextDictionary; /* throw */

                if (contexts != null)
                {
                    //
                    // NOTE: Does the requested interpreter have an
                    //       entry in the dictionary?  If so, remove
                    //       it and then dispose it.
                    //
                    ITestContext context;

                    /* IGNORED */
                    contexts.RemoveAndReturn(interpreter, out context);

                    /* IGNORED */
                    ContextOps.DisposeThread(context);

                    //
                    // NOTE: Release local context reference now.
                    //
                    context = null;

                    //
                    // NOTE: If there are no more contexts present,
                    //       free the collection and the thread data
                    //       slot.
                    //
                    if (contexts.Count == 0)
                    {
                        //
                        // NOTE: Release local reference now.
                        //
                        contexts = null;

                        //
                        // NOTE: Clear it in the per-thread data.
                        //
                        Thread.SetData(localSlot, contexts);
                    }
                }

                //
                // NOTE: Invalidate the cached context for this
                //       interpreter.
                //
                previousTestContext = null;

                return true;
            }
            catch (Exception e)
            {
                error = e;
            }

            return false;
        }

        ///////////////////////////////////////////////////////////////////////

        private ITestContext PrivateGetTestContext(
            bool create
            )
        {
            Result error = null;

            return PrivateGetTestContext(create, ref error);
        }

        ///////////////////////////////////////////////////////////////////////

        private ITestContext PrivateGetTestContext(
            bool create,
            ref Result error
            )
        {
            try
            {
                if (interpreter == null)
                {
                    error = "invalid interpreter";
                    return null;
                }

                ITestContext result = previousTestContext;

                if (ContextOps.CanUseThread(result))
                    return result;

                LocalDataStoreSlot localSlot;

                lock (syncRoot)
                {
                    localSlot = testSlot;
                }

                if (localSlot == null)
                {
                    error = "test context slot is invalid";
                    return null;
                }

                //
                // NOTE: Try to obtain the per-interpreter dictionary of
                //       test contexts for this thread.
                //
                InterpreterTestContextDictionary contexts =
                    Thread.GetData(localSlot) as
                    InterpreterTestContextDictionary; /* throw */

                //
                // NOTE: Has the dictionary been created yet and/or can
                //       we create it now?
                //
                if (create && (contexts == null))
                {
                    //
                    // NOTE: Nope, create it now.
                    //
                    contexts = new InterpreterTestContextDictionary();

                    //
                    // NOTE: Store it in the per-thread data.
                    //
                    Thread.SetData(localSlot, contexts); /* throw */
                }

                //
                // NOTE: Is the dictionary available now (if not,
                //       we have been forbidden by the caller from
                //       automatically creating it).
                //
                if (contexts != null)
                {
                    //
                    // NOTE: Does the requested interpreter have an
                    //       entry in the dictionary?  If so, grab
                    //       and return it.
                    //
                    if (!contexts.TryGetValue(
                            interpreter, out result))
                    {
                        //
                        // NOTE: Now, create one and add it to the
                        //       dictionary of test contexts (which
                        //       is stored via the per-thread data
                        //       slot).
                        //
                        result = new TestContext(interpreter,
                            ContextOps.GetCurrentThreadId());

                        contexts.Add(interpreter, result);
                    }

                    //
                    // NOTE: Save the resulting context for next
                    //       time.
                    //
                    previousTestContext = result;

                    return result;
                }
                else
                {
                    error = "test contexts not available";
                }
            }
            catch (Exception e)
            {
                error = e;
            }

            return null;
        }

        ///////////////////////////////////////////////////////////////////////

        private bool PrivateReleaseVariableContext(
            bool global
            )
        {
            Result error = null;

            return PrivateReleaseVariableContext(global, ref error);
        }

        ///////////////////////////////////////////////////////////////////////

        private bool PrivateReleaseVariableContext(
            bool global,
            ref Result error
            )
        {
            try
            {
                if (interpreter == null)
                {
                    error = "invalid interpreter";
                    return false;
                }

                LocalDataStoreSlot localSlot;

                lock (syncRoot)
                {
                    localSlot = variableSlot;
                }

                if (localSlot == null)
                {
                    error = "variable context slot is invalid";
                    return false;
                }

                //
                // NOTE: Try to obtain the per-interpreter dictionary
                //       of contexts for this thread.
                //
                InterpreterVariableContextDictionary contexts =
                    Thread.GetData(localSlot) as
                    InterpreterVariableContextDictionary; /* throw */

                if (contexts != null)
                {
                    //
                    // NOTE: Does the requested interpreter have an
                    //       entry in the dictionary?  If so, remove
                    //       it and then dispose it.
                    //
                    IVariableContext context;

                    /* IGNORED */
                    contexts.RemoveAndReturn(interpreter, out context);

                    /* IGNORED */
                    FreeVariableContext(context, global);

                    //
                    // NOTE: If there are no more contexts present,
                    //       free the collection and the thread data
                    //       slot.
                    //
                    if (contexts.Count == 0)
                    {
                        //
                        // NOTE: Release local reference now.
                        //
                        contexts = null;

                        //
                        // NOTE: Clear it in the per-thread data.
                        //
                        Thread.SetData(localSlot, contexts);
                    }
                }

                //
                // NOTE: Invalidate the cached context for this
                //       interpreter.
                //
                previousVariableContext = null;

                return true;
            }
            catch (Exception e)
            {
                error = e;
            }

            return false;
        }

        ///////////////////////////////////////////////////////////////////////

        private IVariableContext PrivateGetVariableContext(
            bool create
            )
        {
            Result error = null;

            return PrivateGetVariableContext(create, ref error);
        }

        ///////////////////////////////////////////////////////////////////////

        private IVariableContext PrivateGetVariableContext(
            bool create,
            ref Result error
            )
        {
            CallStack callStack = null;
            bool success = false;

            try
            {
                if (interpreter == null)
                {
                    error = "invalid interpreter";
                    return null;
                }

                IVariableContext result = previousVariableContext;

                if (ContextOps.CanUseThread(result))
                    return result;

                LocalDataStoreSlot localSlot;

                lock (syncRoot)
                {
                    localSlot = variableSlot;
                }

                if (localSlot == null)
                {
                    error = "variable context slot is invalid";
                    return null;
                }

                //
                // NOTE: Try to obtain the per-interpreter dictionary
                //       of variable contexts for this thread.
                //
                InterpreterVariableContextDictionary contexts =
                    Thread.GetData(localSlot) as
                    InterpreterVariableContextDictionary; /* throw */

                //
                // NOTE: Has the dictionary been created yet and/or can
                //       we create it now?
                //
                if (create && (contexts == null))
                {
                    //
                    // NOTE: Nope, create it now.
                    //
                    contexts = new InterpreterVariableContextDictionary();

                    //
                    // NOTE: Store it in the per-thread data.
                    //
                    Thread.SetData(localSlot, contexts); /* throw */
                }

                //
                // NOTE: Is the dictionary available now (if not,
                //       we have been forbidden by the caller from
                //       automatically creating it).
                //
                if (contexts != null)
                {
                    //
                    // NOTE: Does the requested interpreter have an
                    //       entry in the dictionary?  If so, grab
                    //       and return it.
                    //
                    if (!contexts.TryGetValue(
                            interpreter, out result))
                    {
                        //
                        // NOTE: Create a new call stack for the
                        //       interpreter.
                        //
                        callStack = new CallStack(
                            interpreter.RecursionLimit, false);

                        //
                        // NOTE: If necessary, create a new global
                        //       call frame for the interpreter;
                        //       all threads share the same global
                        //       call frame for a given interpreter.
                        //
                        lock (interpreter.InternalSyncRoot) /* TRANSACTIONAL */
                        {
                            //
                            // NOTE: Does the interpreter already
                            //       have a global call frame?  If
                            //       not, make sure a new one is
                            //       created now.  This does *NOT*
                            //       actually associate or save the
                            //       call stack with the (possibly)
                            //       newly created global frame, it
                            //       just grabs the count from it.
                            //
                            /* IGNORED */
                            interpreter.CreateGlobalFrame(callStack);

                            //
                            // NOTE: Now, create one and add it to
                            //       the dictionary of variable
                            //       contexts (which is stored via
                            //       the per-thread data slot).
                            //
                            result = new VariableContext(interpreter,
                                ContextOps.GetCurrentThreadId(),
                                callStack, interpreter.InternalGlobalFrame,
                                null, null, null, null, null);

                            contexts.Add(interpreter, result);

                            //
                            // BUGFIX: Only once we make it to *this*
                            //         point can we guarantee that the
                            //         CallStack instance been safely
                            //         created and stored; otherwise,
                            //         it must be disposed via the
                            //         finally block.
                            //
                            success = true;
                        }

                        //
                        // NOTE: This call frame is never popped.
                        //
                        interpreter.PushGlobalCallFrame(false);
                    }

                    //
                    // NOTE: Save the resulting context for next
                    //       time.
                    //
                    previousVariableContext = result;

                    return result;
                }
                else
                {
                    error = "variable contexts not available";
                }
            }
            catch (Exception e)
            {
                error = e;
            }
            finally
            {
                //
                // BUGFIX: Dispose of the created call stack if it was
                //         not successfully stored somewhere persistent.
                //
                if (!success && (callStack != null))
                {
                    callStack.Dispose();
                    callStack = null;
                }
            }

            return null;
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region IGetInterpreter / ISetInterpreter Members
        private Interpreter interpreter;
        public Interpreter Interpreter
        {
            get { CheckDisposed(); return interpreter; }
            set { CheckDisposed(); interpreter = value; }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region IContextManager Members
        //
        // NOTE: This method is called via the interpreter disposal pipeline.
        //
        public int GetInterpreterContextCount()
        {
            // CheckDisposed(); /* EXEMPT */

            return Math.Max(
                Math.Max(PrivateGetEngineContextCount(),
                PrivateGetInteractiveContextCount()),
                Math.Max(PrivateGetTestContextCount(),
                PrivateGetVariableContextCount()));
        }

        ///////////////////////////////////////////////////////////////////////

        public bool ReleaseEngineContext(
            bool global
            )
        {
            CheckDisposed();

            return PrivateReleaseEngineContext(global);
        }

        ///////////////////////////////////////////////////////////////////////

        public bool ReleaseEngineContext(
            bool global,
            ref Result error
            )
        {
            CheckDisposed();

            return PrivateReleaseEngineContext(global, ref error);
        }

        ///////////////////////////////////////////////////////////////////////

        public IEngineContext GetEngineContext(
            bool create
            )
        {
            CheckDisposed();

            return PrivateGetEngineContext(create);
        }

        ///////////////////////////////////////////////////////////////////////

        public IEngineContext GetEngineContext(
            bool create,
            ref Result error
            )
        {
            CheckDisposed();

            return PrivateGetEngineContext(create, ref error);
        }

        ///////////////////////////////////////////////////////////////////////

        public int GetEngineContextCount()
        {
            CheckDisposed();

            return PrivateGetEngineContextCount();
        }

        ///////////////////////////////////////////////////////////////////////

        public int PurgeEngineContexts(
            Interpreter interpreter,
            bool nonPrimary,
            bool global
            )
        {
            CheckDisposed();

            return PrivatePurgeEngineContexts(
                interpreter, nonPrimary, global);
        }

        ///////////////////////////////////////////////////////////////////////

        public bool ReleaseInteractiveContext(
            bool global
            )
        {
            CheckDisposed();

            return PrivateReleaseInteractiveContext(global);
        }

        ///////////////////////////////////////////////////////////////////////

        public bool ReleaseInteractiveContext(
            bool global,
            ref Result error
            )
        {
            CheckDisposed();

            return PrivateReleaseInteractiveContext(global, ref error);
        }

        ///////////////////////////////////////////////////////////////////////

        public IInteractiveContext GetInteractiveContext(
            bool create
            )
        {
            CheckDisposed();

            return PrivateGetInteractiveContext(create);
        }

        ///////////////////////////////////////////////////////////////////////

        public IInteractiveContext GetInteractiveContext(
            bool create,
            ref Result error
            )
        {
            CheckDisposed();

            return PrivateGetInteractiveContext(create, ref error);
        }

        ///////////////////////////////////////////////////////////////////////

        public int GetInteractiveContextCount()
        {
            CheckDisposed();

            return PrivateGetInteractiveContextCount();
        }

        ///////////////////////////////////////////////////////////////////////

        public int PurgeInteractiveContexts(
            Interpreter interpreter,
            bool nonPrimary,
            bool global
            )
        {
            CheckDisposed();

            return PrivatePurgeInteractiveContexts(
                interpreter, nonPrimary, global);
        }

        ///////////////////////////////////////////////////////////////////////

        public bool ReleaseTestContext(
            bool global
            )
        {
            CheckDisposed();

            return PrivateReleaseTestContext(global);
        }

        ///////////////////////////////////////////////////////////////////////

        public bool ReleaseTestContext(
            bool global,
            ref Result error
            )
        {
            CheckDisposed();

            return PrivateReleaseTestContext(global, ref error);
        }

        ///////////////////////////////////////////////////////////////////////

        public ITestContext GetTestContext(
            bool create
            )
        {
            CheckDisposed();

            return PrivateGetTestContext(create);
        }

        ///////////////////////////////////////////////////////////////////////

        public ITestContext GetTestContext(
            bool create,
            ref Result error
            )
        {
            CheckDisposed();

            return PrivateGetTestContext(create, ref error);
        }

        ///////////////////////////////////////////////////////////////////////

        public int GetTestContextCount()
        {
            CheckDisposed();

            return PrivateGetTestContextCount();
        }

        ///////////////////////////////////////////////////////////////////////

        public int PurgeTestContexts(
            Interpreter interpreter,
            bool nonPrimary,
            bool global
            )
        {
            CheckDisposed();

            return PrivatePurgeTestContexts(
                interpreter, nonPrimary, global);
        }

        ///////////////////////////////////////////////////////////////////////

        public bool ReleaseVariableContext(
            bool global
            )
        {
            CheckDisposed();

            return PrivateReleaseVariableContext(global);
        }

        ///////////////////////////////////////////////////////////////////////

        public bool ReleaseVariableContext(
            bool global,
            ref Result error
            )
        {
            CheckDisposed();

            return PrivateReleaseVariableContext(global, ref error);
        }

        ///////////////////////////////////////////////////////////////////////

        public IVariableContext GetVariableContext(
            bool create
            )
        {
            CheckDisposed();

            return PrivateGetVariableContext(create);
        }

        ///////////////////////////////////////////////////////////////////////

        public IVariableContext GetVariableContext(
            bool create,
            ref Result error
            )
        {
            CheckDisposed();

            return PrivateGetVariableContext(create, ref error);
        }

        ///////////////////////////////////////////////////////////////////////

        public int GetVariableContextCount()
        {
            CheckDisposed();

            return PrivateGetVariableContextCount();
        }

        ///////////////////////////////////////////////////////////////////////

        public int PurgeVariableContexts(
            Interpreter interpreter,
            bool nonPrimary,
            bool global
            )
        {
            CheckDisposed();

            return PrivatePurgeVariableContexts(
                interpreter, nonPrimary, global);
        }

        ///////////////////////////////////////////////////////////////////////

        //
        // NOTE: This method is called via the interpreter disposal pipeline
        //       as well as during the disposal of this class.
        //
        public void Free(
            bool global
            )
        {
            // CheckDisposed(); /* EXEMPT */

            ///////////////////////////////////////////////////////////////////

            PrivateReleaseEngineContext(global);
            PrivateReleaseInteractiveContext(global);
            PrivateReleaseTestContext(global);
            PrivateReleaseVariableContext(global);

            ///////////////////////////////////////////////////////////////////

            if (global && (interpreter != null))
                interpreter = null; /* NOT OWNED, DO NOT DISPOSE. */
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region IDisposable "Pattern" Members
        private bool disposed;
        private void CheckDisposed() /* throw */
        {
#if THROW_ON_DISPOSED
            if (disposed && Engine.IsThrowOnDisposed(interpreter, null))
                throw new InterpreterDisposedException(typeof(ContextManager));
#endif
        }

        ///////////////////////////////////////////////////////////////////////

        private /* protected virtual */ void Dispose(
            bool disposing
            )
        {
            if (!disposed)
            {
                if (disposing)
                {
                    ////////////////////////////////////
                    // dispose managed resources here...
                    ////////////////////////////////////

                    //
                    // NOTE: Skip freeing any thread-local storage used by
                    //       this object if the entire application domain
                    //       is being finalized.  This is necessary because
                    //       the thread-local storage may have already been
                    //       freed in that case.
                    //
                    if (!AppDomainOps.IsStoppingSoon())
                        Free(true);
                }

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
        ~ContextManager()
        {
            Dispose(false);
        }
        #endregion
    }
}
