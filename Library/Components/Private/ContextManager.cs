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
    /// <summary>
    /// This class manages the per-interpreter, per-thread engine, interactive, test, and variable contexts used by the Eagle engine, creating them on demand and releasing them during disposal.
    /// </summary>
    [ObjectId("53f5f2e0-ea9c-46a8-b584-779ef217beb5")]
    internal sealed class ContextManager :
            IContextManager, IHaveInterpreter, IDisposable
    {
        #region Private Static Data
        /// <summary>
        /// The object used to synchronize access to the thread-local storage slots.
        /// </summary>
        private static object syncRoot = new object();

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// The thread-local storage slot holding the per-interpreter engine contexts.
        /// </summary>
        private static LocalDataStoreSlot engineSlot;
        /// <summary>
        /// The thread-local storage slot holding the per-interpreter interactive contexts.
        /// </summary>
        private static LocalDataStoreSlot interactiveSlot;
        /// <summary>
        /// The thread-local storage slot holding the per-interpreter test contexts.
        /// </summary>
        private static LocalDataStoreSlot testSlot;
        /// <summary>
        /// The thread-local storage slot holding the per-interpreter variable contexts.
        /// </summary>
        private static LocalDataStoreSlot variableSlot;
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Private Data
        /// <summary>
        /// The most recently used engine context, cached to avoid repeated lookups.
        /// </summary>
        private IEngineContext previousEngineContext;
        /// <summary>
        /// The most recently used interactive context, cached to avoid repeated lookups.
        /// </summary>
        private IInteractiveContext previousInteractiveContext;
        /// <summary>
        /// The most recently used test context, cached to avoid repeated lookups.
        /// </summary>
        private ITestContext previousTestContext;
        /// <summary>
        /// The most recently used variable context, cached to avoid repeated lookups.
        /// </summary>
        private IVariableContext previousVariableContext;
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Public Static Methods
        /// <summary>
        /// This method allocates the thread-local storage slots used to hold the per-interpreter contexts; it must be called prior to evaluating any scripts or performing any call frame handling.
        /// </summary>
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
        /// <summary>
        /// This method purges the engine, interactive, test, and variable contexts associated with the specified interpreter from all threads; it is called via the interpreter disposal pipeline.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter whose contexts should be purged.
        /// </param>
        /// <param name="nonPrimary">
        /// Non-zero to also purge contexts belonging to non-primary interpreters.
        /// </param>
        /// <param name="global">
        /// Non-zero if the interpreter itself is also being disposed.
        /// </param>
        /// <returns>
        /// The total number of contexts that were purged.
        /// </returns>
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
        /// <summary>
        /// Constructs a context manager associated with the specified interpreter.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter that owns this context manager.
        /// </param>
        public ContextManager(
            Interpreter interpreter
            )
        {
            this.interpreter = interpreter;
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Private Static Methods
        /// <summary>
        /// This method frees the specified variable context, using its special Free method so that the global call frame is handled correctly.
        /// </summary>
        /// <param name="variableContext">
        /// The variable context to free.  This parameter may be null.
        /// </param>
        /// <param name="global">
        /// Non-zero if the interpreter that owns the global call frame is also being disposed.
        /// </param>
        /// <returns>
        /// True if the variable context was freed; otherwise, false.
        /// </returns>
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

        /// <summary>
        /// This method purges the engine contexts for the matching interpreters from the engine context slot on the current thread.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter whose contexts should be purged.  This parameter is not used.
        /// </param>
        /// <param name="nonPrimary">
        /// Non-zero to also purge contexts belonging to non-primary interpreters.
        /// </param>
        /// <param name="global">
        /// Non-zero if the interpreter itself is also being disposed.  This parameter is not used.
        /// </param>
        /// <returns>
        /// The number of engine contexts that were purged.
        /// </returns>
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

        /// <summary>
        /// This method purges the interactive contexts for the matching interpreters from the interactive context slot on the current thread.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter whose contexts should be purged.  This parameter is not used.
        /// </param>
        /// <param name="nonPrimary">
        /// Non-zero to also purge contexts belonging to non-primary interpreters.
        /// </param>
        /// <param name="global">
        /// Non-zero if the interpreter itself is also being disposed.  This parameter is not used.
        /// </param>
        /// <returns>
        /// The number of interactive contexts that were purged.
        /// </returns>
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

        /// <summary>
        /// This method purges the test contexts for the matching interpreters from the test context slot on the current thread.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter whose contexts should be purged.  This parameter is not used.
        /// </param>
        /// <param name="nonPrimary">
        /// Non-zero to also purge contexts belonging to non-primary interpreters.
        /// </param>
        /// <param name="global">
        /// Non-zero if the interpreter itself is also being disposed.  This parameter is not used.
        /// </param>
        /// <returns>
        /// The number of test contexts that were purged.
        /// </returns>
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

        /// <summary>
        /// This method purges the variable contexts for the matching interpreters from the variable context slot on the current thread.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter whose contexts should be purged.
        /// </param>
        /// <param name="nonPrimary">
        /// Non-zero to also purge contexts belonging to non-primary interpreters.
        /// </param>
        /// <param name="global">
        /// Non-zero if the interpreter that owns the global call frame is also being disposed.
        /// </param>
        /// <returns>
        /// The number of variable contexts that were purged.
        /// </returns>
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

        /// <summary>
        /// This method returns the number of engine contexts present in the engine context slot on the current thread.
        /// </summary>
        /// <returns>
        /// The number of engine contexts; zero if none are present.
        /// </returns>
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

        /// <summary>
        /// This method returns the number of interactive contexts present in the interactive context slot on the current thread.
        /// </summary>
        /// <returns>
        /// The number of interactive contexts; zero if none are present.
        /// </returns>
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

        /// <summary>
        /// This method returns the number of test contexts present in the test context slot on the current thread.
        /// </summary>
        /// <returns>
        /// The number of test contexts; zero if none are present.
        /// </returns>
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

        /// <summary>
        /// This method returns the number of variable contexts present in the variable context slot on the current thread.
        /// </summary>
        /// <returns>
        /// The number of variable contexts; zero if none are present.
        /// </returns>
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
        /// <summary>
        /// This method releases the engine context for the current interpreter on the current thread.
        /// </summary>
        /// <param name="global">
        /// Non-zero if the interpreter itself is also being disposed.
        /// </param>
        /// <returns>
        /// True if the engine context was released; otherwise, false.
        /// </returns>
        private bool PrivateReleaseEngineContext(
            bool global
            )
        {
            Result error = null;

            return PrivateReleaseEngineContext(global, ref error);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method releases the engine context for the current interpreter on the current thread.
        /// </summary>
        /// <param name="global">
        /// Non-zero if the interpreter itself is also being disposed.  This parameter is not used.
        /// </param>
        /// <param name="error">
        /// Upon failure, receives an error message describing why the engine context could not be released.
        /// </param>
        /// <returns>
        /// True if the engine context was released; otherwise, false.
        /// </returns>
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

        /// <summary>
        /// This method returns the engine context for the current interpreter on the current thread, optionally creating it.
        /// </summary>
        /// <param name="create">
        /// Non-zero to create the engine context if it does not already exist.
        /// </param>
        /// <returns>
        /// The engine context, or null if it is not available.
        /// </returns>
        private IEngineContext PrivateGetEngineContext(
            bool create
            )
        {
            Result error = null;

            return PrivateGetEngineContext(create, ref error);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method returns the engine context for the current interpreter on the current thread, optionally creating it.
        /// </summary>
        /// <param name="create">
        /// Non-zero to create the engine context if it does not already exist.
        /// </param>
        /// <param name="error">
        /// Upon failure, receives an error message describing why the engine context could not be returned.
        /// </param>
        /// <returns>
        /// The engine context, or null if it is not available.
        /// </returns>
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

        /// <summary>
        /// This method releases the interactive context for the current interpreter on the current thread.
        /// </summary>
        /// <param name="global">
        /// Non-zero if the interpreter itself is also being disposed.
        /// </param>
        /// <returns>
        /// True if the interactive context was released; otherwise, false.
        /// </returns>
        private bool PrivateReleaseInteractiveContext(
            bool global
            )
        {
            Result error = null;

            return PrivateReleaseInteractiveContext(global, ref error);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method releases the interactive context for the current interpreter on the current thread.
        /// </summary>
        /// <param name="global">
        /// Non-zero if the interpreter itself is also being disposed.  This parameter is not used.
        /// </param>
        /// <param name="error">
        /// Upon failure, receives an error message describing why the interactive context could not be released.
        /// </param>
        /// <returns>
        /// True if the interactive context was released; otherwise, false.
        /// </returns>
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

        /// <summary>
        /// This method returns the interactive context for the current interpreter on the current thread, optionally creating it.
        /// </summary>
        /// <param name="create">
        /// Non-zero to create the interactive context if it does not already exist.
        /// </param>
        /// <returns>
        /// The interactive context, or null if it is not available.
        /// </returns>
        private IInteractiveContext PrivateGetInteractiveContext(
            bool create
            )
        {
            Result error = null;

            return PrivateGetInteractiveContext(create, ref error);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method returns the interactive context for the current interpreter on the current thread, optionally creating it.
        /// </summary>
        /// <param name="create">
        /// Non-zero to create the interactive context if it does not already exist.
        /// </param>
        /// <param name="error">
        /// Upon failure, receives an error message describing why the interactive context could not be returned.
        /// </param>
        /// <returns>
        /// The interactive context, or null if it is not available.
        /// </returns>
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

        /// <summary>
        /// This method releases the test context for the current interpreter on the current thread.
        /// </summary>
        /// <param name="global">
        /// Non-zero if the interpreter itself is also being disposed.
        /// </param>
        /// <returns>
        /// True if the test context was released; otherwise, false.
        /// </returns>
        private bool PrivateReleaseTestContext(
            bool global
            )
        {
            Result error = null;

            return PrivateReleaseTestContext(global, ref error);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method releases the test context for the current interpreter on the current thread.
        /// </summary>
        /// <param name="global">
        /// Non-zero if the interpreter itself is also being disposed.  This parameter is not used.
        /// </param>
        /// <param name="error">
        /// Upon failure, receives an error message describing why the test context could not be released.
        /// </param>
        /// <returns>
        /// True if the test context was released; otherwise, false.
        /// </returns>
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

        /// <summary>
        /// This method returns the test context for the current interpreter on the current thread, optionally creating it.
        /// </summary>
        /// <param name="create">
        /// Non-zero to create the test context if it does not already exist.
        /// </param>
        /// <returns>
        /// The test context, or null if it is not available.
        /// </returns>
        private ITestContext PrivateGetTestContext(
            bool create
            )
        {
            Result error = null;

            return PrivateGetTestContext(create, ref error);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method returns the test context for the current interpreter on the current thread, optionally creating it.
        /// </summary>
        /// <param name="create">
        /// Non-zero to create the test context if it does not already exist.
        /// </param>
        /// <param name="error">
        /// Upon failure, receives an error message describing why the test context could not be returned.
        /// </param>
        /// <returns>
        /// The test context, or null if it is not available.
        /// </returns>
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

        /// <summary>
        /// This method releases the variable context for the current interpreter on the current thread.
        /// </summary>
        /// <param name="global">
        /// Non-zero if the interpreter itself is also being disposed.
        /// </param>
        /// <returns>
        /// True if the variable context was released; otherwise, false.
        /// </returns>
        private bool PrivateReleaseVariableContext(
            bool global
            )
        {
            Result error = null;

            return PrivateReleaseVariableContext(global, ref error);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method releases the variable context for the current interpreter on the current thread.
        /// </summary>
        /// <param name="global">
        /// Non-zero if the interpreter that owns the global call frame is also being disposed.
        /// </param>
        /// <param name="error">
        /// Upon failure, receives an error message describing why the variable context could not be released.
        /// </param>
        /// <returns>
        /// True if the variable context was released; otherwise, false.
        /// </returns>
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

        /// <summary>
        /// This method returns the variable context for the current interpreter on the current thread, optionally creating it.
        /// </summary>
        /// <param name="create">
        /// Non-zero to create the variable context if it does not already exist.
        /// </param>
        /// <returns>
        /// The variable context, or null if it is not available.
        /// </returns>
        private IVariableContext PrivateGetVariableContext(
            bool create
            )
        {
            Result error = null;

            return PrivateGetVariableContext(create, ref error);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method returns the variable context for the current interpreter on the current thread, optionally creating it.
        /// </summary>
        /// <param name="create">
        /// Non-zero to create the variable context if it does not already exist.
        /// </param>
        /// <param name="error">
        /// Upon failure, receives an error message describing why the variable context could not be returned.
        /// </param>
        /// <returns>
        /// The variable context, or null if it is not available.
        /// </returns>
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
        /// <summary>
        /// The interpreter associated with this context manager.
        /// </summary>
        private Interpreter interpreter;
        /// <summary>
        /// Gets or sets the interpreter associated with this context manager.
        /// </summary>
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
        /// <summary>
        /// This method returns the largest number of contexts of any single kind (engine, interactive, test, or variable) present on the current thread; it is called via the interpreter disposal pipeline.
        /// </summary>
        /// <returns>
        /// The largest per-kind context count on the current thread.
        /// </returns>
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

        /// <summary>
        /// This method releases the engine context for the current interpreter on the current thread.
        /// </summary>
        /// <param name="global">
        /// Non-zero if the interpreter itself is also being disposed.
        /// </param>
        /// <returns>
        /// True if the engine context was released; otherwise, false.
        /// </returns>
        public bool ReleaseEngineContext(
            bool global
            )
        {
            CheckDisposed();

            return PrivateReleaseEngineContext(global);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method releases the engine context for the current interpreter on the current thread.
        /// </summary>
        /// <param name="global">
        /// Non-zero if the interpreter itself is also being disposed.
        /// </param>
        /// <param name="error">
        /// Upon failure, receives an error message describing why the engine context could not be released.
        /// </param>
        /// <returns>
        /// True if the engine context was released; otherwise, false.
        /// </returns>
        public bool ReleaseEngineContext(
            bool global,
            ref Result error
            )
        {
            CheckDisposed();

            return PrivateReleaseEngineContext(global, ref error);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method returns the engine context for the current interpreter on the current thread, optionally creating it.
        /// </summary>
        /// <param name="create">
        /// Non-zero to create the engine context if it does not already exist.
        /// </param>
        /// <returns>
        /// The engine context, or null if it is not available.
        /// </returns>
        public IEngineContext GetEngineContext(
            bool create
            )
        {
            CheckDisposed();

            return PrivateGetEngineContext(create);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method returns the engine context for the current interpreter on the current thread, optionally creating it.
        /// </summary>
        /// <param name="create">
        /// Non-zero to create the engine context if it does not already exist.
        /// </param>
        /// <param name="error">
        /// Upon failure, receives an error message describing why the engine context could not be returned.
        /// </param>
        /// <returns>
        /// The engine context, or null if it is not available.
        /// </returns>
        public IEngineContext GetEngineContext(
            bool create,
            ref Result error
            )
        {
            CheckDisposed();

            return PrivateGetEngineContext(create, ref error);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method returns the number of engine contexts present on the current thread.
        /// </summary>
        /// <returns>
        /// The number of engine contexts; zero if none are present.
        /// </returns>
        public int GetEngineContextCount()
        {
            CheckDisposed();

            return PrivateGetEngineContextCount();
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method purges the engine contexts associated with the specified interpreter from all threads.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter whose contexts should be purged.
        /// </param>
        /// <param name="nonPrimary">
        /// Non-zero to also purge contexts belonging to non-primary interpreters.
        /// </param>
        /// <param name="global">
        /// Non-zero if the interpreter itself is also being disposed.
        /// </param>
        /// <returns>
        /// The number of engine contexts that were purged.
        /// </returns>
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

        /// <summary>
        /// This method releases the interactive context for the current interpreter on the current thread.
        /// </summary>
        /// <param name="global">
        /// Non-zero if the interpreter itself is also being disposed.
        /// </param>
        /// <returns>
        /// True if the interactive context was released; otherwise, false.
        /// </returns>
        public bool ReleaseInteractiveContext(
            bool global
            )
        {
            CheckDisposed();

            return PrivateReleaseInteractiveContext(global);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method releases the interactive context for the current interpreter on the current thread.
        /// </summary>
        /// <param name="global">
        /// Non-zero if the interpreter itself is also being disposed.
        /// </param>
        /// <param name="error">
        /// Upon failure, receives an error message describing why the interactive context could not be released.
        /// </param>
        /// <returns>
        /// True if the interactive context was released; otherwise, false.
        /// </returns>
        public bool ReleaseInteractiveContext(
            bool global,
            ref Result error
            )
        {
            CheckDisposed();

            return PrivateReleaseInteractiveContext(global, ref error);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method returns the interactive context for the current interpreter on the current thread, optionally creating it.
        /// </summary>
        /// <param name="create">
        /// Non-zero to create the interactive context if it does not already exist.
        /// </param>
        /// <returns>
        /// The interactive context, or null if it is not available.
        /// </returns>
        public IInteractiveContext GetInteractiveContext(
            bool create
            )
        {
            CheckDisposed();

            return PrivateGetInteractiveContext(create);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method returns the interactive context for the current interpreter on the current thread, optionally creating it.
        /// </summary>
        /// <param name="create">
        /// Non-zero to create the interactive context if it does not already exist.
        /// </param>
        /// <param name="error">
        /// Upon failure, receives an error message describing why the interactive context could not be returned.
        /// </param>
        /// <returns>
        /// The interactive context, or null if it is not available.
        /// </returns>
        public IInteractiveContext GetInteractiveContext(
            bool create,
            ref Result error
            )
        {
            CheckDisposed();

            return PrivateGetInteractiveContext(create, ref error);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method returns the number of interactive contexts present on the current thread.
        /// </summary>
        /// <returns>
        /// The number of interactive contexts; zero if none are present.
        /// </returns>
        public int GetInteractiveContextCount()
        {
            CheckDisposed();

            return PrivateGetInteractiveContextCount();
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method purges the interactive contexts associated with the specified interpreter from all threads.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter whose contexts should be purged.
        /// </param>
        /// <param name="nonPrimary">
        /// Non-zero to also purge contexts belonging to non-primary interpreters.
        /// </param>
        /// <param name="global">
        /// Non-zero if the interpreter itself is also being disposed.
        /// </param>
        /// <returns>
        /// The number of interactive contexts that were purged.
        /// </returns>
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

        /// <summary>
        /// This method releases the test context for the current interpreter on the current thread.
        /// </summary>
        /// <param name="global">
        /// Non-zero if the interpreter itself is also being disposed.
        /// </param>
        /// <returns>
        /// True if the test context was released; otherwise, false.
        /// </returns>
        public bool ReleaseTestContext(
            bool global
            )
        {
            CheckDisposed();

            return PrivateReleaseTestContext(global);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method releases the test context for the current interpreter on the current thread.
        /// </summary>
        /// <param name="global">
        /// Non-zero if the interpreter itself is also being disposed.
        /// </param>
        /// <param name="error">
        /// Upon failure, receives an error message describing why the test context could not be released.
        /// </param>
        /// <returns>
        /// True if the test context was released; otherwise, false.
        /// </returns>
        public bool ReleaseTestContext(
            bool global,
            ref Result error
            )
        {
            CheckDisposed();

            return PrivateReleaseTestContext(global, ref error);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method returns the test context for the current interpreter on the current thread, optionally creating it.
        /// </summary>
        /// <param name="create">
        /// Non-zero to create the test context if it does not already exist.
        /// </param>
        /// <returns>
        /// The test context, or null if it is not available.
        /// </returns>
        public ITestContext GetTestContext(
            bool create
            )
        {
            CheckDisposed();

            return PrivateGetTestContext(create);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method returns the test context for the current interpreter on the current thread, optionally creating it.
        /// </summary>
        /// <param name="create">
        /// Non-zero to create the test context if it does not already exist.
        /// </param>
        /// <param name="error">
        /// Upon failure, receives an error message describing why the test context could not be returned.
        /// </param>
        /// <returns>
        /// The test context, or null if it is not available.
        /// </returns>
        public ITestContext GetTestContext(
            bool create,
            ref Result error
            )
        {
            CheckDisposed();

            return PrivateGetTestContext(create, ref error);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method returns the number of test contexts present on the current thread.
        /// </summary>
        /// <returns>
        /// The number of test contexts; zero if none are present.
        /// </returns>
        public int GetTestContextCount()
        {
            CheckDisposed();

            return PrivateGetTestContextCount();
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method purges the test contexts associated with the specified interpreter from all threads.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter whose contexts should be purged.
        /// </param>
        /// <param name="nonPrimary">
        /// Non-zero to also purge contexts belonging to non-primary interpreters.
        /// </param>
        /// <param name="global">
        /// Non-zero if the interpreter itself is also being disposed.
        /// </param>
        /// <returns>
        /// The number of test contexts that were purged.
        /// </returns>
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

        /// <summary>
        /// This method releases the variable context for the current interpreter on the current thread.
        /// </summary>
        /// <param name="global">
        /// Non-zero if the interpreter itself is also being disposed.
        /// </param>
        /// <returns>
        /// True if the variable context was released; otherwise, false.
        /// </returns>
        public bool ReleaseVariableContext(
            bool global
            )
        {
            CheckDisposed();

            return PrivateReleaseVariableContext(global);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method releases the variable context for the current interpreter on the current thread.
        /// </summary>
        /// <param name="global">
        /// Non-zero if the interpreter itself is also being disposed.
        /// </param>
        /// <param name="error">
        /// Upon failure, receives an error message describing why the variable context could not be released.
        /// </param>
        /// <returns>
        /// True if the variable context was released; otherwise, false.
        /// </returns>
        public bool ReleaseVariableContext(
            bool global,
            ref Result error
            )
        {
            CheckDisposed();

            return PrivateReleaseVariableContext(global, ref error);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method returns the variable context for the current interpreter on the current thread, optionally creating it.
        /// </summary>
        /// <param name="create">
        /// Non-zero to create the variable context if it does not already exist.
        /// </param>
        /// <returns>
        /// The variable context, or null if it is not available.
        /// </returns>
        public IVariableContext GetVariableContext(
            bool create
            )
        {
            CheckDisposed();

            return PrivateGetVariableContext(create);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method returns the variable context for the current interpreter on the current thread, optionally creating it.
        /// </summary>
        /// <param name="create">
        /// Non-zero to create the variable context if it does not already exist.
        /// </param>
        /// <param name="error">
        /// Upon failure, receives an error message describing why the variable context could not be returned.
        /// </param>
        /// <returns>
        /// The variable context, or null if it is not available.
        /// </returns>
        public IVariableContext GetVariableContext(
            bool create,
            ref Result error
            )
        {
            CheckDisposed();

            return PrivateGetVariableContext(create, ref error);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method returns the number of variable contexts present on the current thread.
        /// </summary>
        /// <returns>
        /// The number of variable contexts; zero if none are present.
        /// </returns>
        public int GetVariableContextCount()
        {
            CheckDisposed();

            return PrivateGetVariableContextCount();
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method purges the variable contexts associated with the specified interpreter from all threads.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter whose contexts should be purged.
        /// </param>
        /// <param name="nonPrimary">
        /// Non-zero to also purge contexts belonging to non-primary interpreters.
        /// </param>
        /// <param name="global">
        /// Non-zero if the interpreter itself is also being disposed.
        /// </param>
        /// <returns>
        /// The number of variable contexts that were purged.
        /// </returns>
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
        /// <summary>
        /// This method releases the engine, interactive, test, and variable contexts for the current interpreter and, when disposing globally, clears the interpreter reference; it is called via the interpreter disposal pipeline and during disposal of this class.
        /// </summary>
        /// <param name="global">
        /// Non-zero if the interpreter itself is also being disposed.
        /// </param>
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
        /// <summary>
        /// Non-zero if this context manager has been disposed.
        /// </summary>
        private bool disposed;
        /// <summary>
        /// This method throws an exception if this context manager has been disposed and the interpreter is configured to throw on disposed access.
        /// </summary>
        private void CheckDisposed() /* throw */
        {
#if THROW_ON_DISPOSED
            if (disposed && Engine.IsThrowOnDisposed(interpreter, null))
                throw new InterpreterDisposedException(typeof(ContextManager));
#endif
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method releases the resources used by this context manager.
        /// </summary>
        /// <param name="disposing">
        /// Non-zero if this method is being called from the Dispose method rather than from the finalizer.
        /// </param>
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
        /// <summary>
        /// This method releases all resources used by this context manager.
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
        /// Finalizes this context manager, releasing any resources that were not explicitly disposed.
        /// </summary>
        ~ContextManager()
        {
            Dispose(false);
        }
        #endregion
    }
}
