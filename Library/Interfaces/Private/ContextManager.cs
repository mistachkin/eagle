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

using Eagle._Attributes;
using Eagle._Components.Public;

namespace Eagle._Interfaces.Private
{
    //
    // NOTE: This interface is currently private; however, it may be "promoted"
    //       to public at some point.
    //
    /// <summary>
    /// This interface defines methods used to create, retrieve, release, and
    /// purge the various per-thread context objects (engine, interactive, test,
    /// and variable) that an interpreter maintains.
    /// </summary>
    [ObjectId("740b2349-fc1c-45f7-9549-9e96f20e8221")]
    internal interface IContextManager
    {
        /// <summary>
        /// This method returns the number of interpreter contexts currently
        /// maintained.
        /// </summary>
        /// <returns>
        /// The number of interpreter contexts.
        /// </returns>
        int GetInterpreterContextCount();

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method releases the engine context.
        /// </summary>
        /// <param name="global">
        /// Non-zero to release the global engine context; zero to release the
        /// per-thread engine context.
        /// </param>
        /// <returns>
        /// True if the context was released; otherwise, false.
        /// </returns>
        bool ReleaseEngineContext(bool global);

        /// <summary>
        /// This method releases the engine context, reporting any error.
        /// </summary>
        /// <param name="global">
        /// Non-zero to release the global engine context; zero to release the
        /// per-thread engine context.
        /// </param>
        /// <param name="error">
        /// Upon failure, this contains an appropriate error message.
        /// </param>
        /// <returns>
        /// True if the context was released; otherwise, false.
        /// </returns>
        bool ReleaseEngineContext(bool global, ref Result error);

        /// <summary>
        /// This method returns the engine context for the current thread.
        /// </summary>
        /// <param name="create">
        /// Non-zero to create the engine context if it does not already exist.
        /// </param>
        /// <returns>
        /// The engine context, or null if none is available and one was not
        /// created.
        /// </returns>
        IEngineContext GetEngineContext(bool create);

        /// <summary>
        /// This method returns the engine context for the current thread,
        /// reporting any error.
        /// </summary>
        /// <param name="create">
        /// Non-zero to create the engine context if it does not already exist.
        /// </param>
        /// <param name="error">
        /// Upon failure, this contains an appropriate error message.
        /// </param>
        /// <returns>
        /// The engine context, or null if none is available and one was not
        /// created.
        /// </returns>
        IEngineContext GetEngineContext(bool create, ref Result error);

        /// <summary>
        /// This method returns the number of engine contexts currently
        /// maintained.
        /// </summary>
        /// <returns>
        /// The number of engine contexts.
        /// </returns>
        int GetEngineContextCount();

        /// <summary>
        /// This method purges engine contexts that are no longer needed.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter context.  This parameter should not be null.
        /// </param>
        /// <param name="nonPrimary">
        /// Non-zero to purge contexts belonging to non-primary threads.
        /// </param>
        /// <param name="global">
        /// Non-zero to also purge the global engine context.
        /// </param>
        /// <returns>
        /// The number of engine contexts that were purged.
        /// </returns>
        int PurgeEngineContexts(
            Interpreter interpreter, bool nonPrimary, bool global);

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method releases the interactive context.
        /// </summary>
        /// <param name="global">
        /// Non-zero to release the global interactive context; zero to release
        /// the per-thread interactive context.
        /// </param>
        /// <returns>
        /// True if the context was released; otherwise, false.
        /// </returns>
        bool ReleaseInteractiveContext(bool global);

        /// <summary>
        /// This method releases the interactive context, reporting any error.
        /// </summary>
        /// <param name="global">
        /// Non-zero to release the global interactive context; zero to release
        /// the per-thread interactive context.
        /// </param>
        /// <param name="error">
        /// Upon failure, this contains an appropriate error message.
        /// </param>
        /// <returns>
        /// True if the context was released; otherwise, false.
        /// </returns>
        bool ReleaseInteractiveContext(bool global, ref Result error);

        /// <summary>
        /// This method returns the interactive context for the current thread.
        /// </summary>
        /// <param name="create">
        /// Non-zero to create the interactive context if it does not already
        /// exist.
        /// </param>
        /// <returns>
        /// The interactive context, or null if none is available and one was
        /// not created.
        /// </returns>
        IInteractiveContext GetInteractiveContext(bool create);

        /// <summary>
        /// This method returns the interactive context for the current thread,
        /// reporting any error.
        /// </summary>
        /// <param name="create">
        /// Non-zero to create the interactive context if it does not already
        /// exist.
        /// </param>
        /// <param name="error">
        /// Upon failure, this contains an appropriate error message.
        /// </param>
        /// <returns>
        /// The interactive context, or null if none is available and one was
        /// not created.
        /// </returns>
        IInteractiveContext GetInteractiveContext(bool create, ref Result error);

        /// <summary>
        /// This method returns the number of interactive contexts currently
        /// maintained.
        /// </summary>
        /// <returns>
        /// The number of interactive contexts.
        /// </returns>
        int GetInteractiveContextCount();

        /// <summary>
        /// This method purges interactive contexts that are no longer needed.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter context.  This parameter should not be null.
        /// </param>
        /// <param name="nonPrimary">
        /// Non-zero to purge contexts belonging to non-primary threads.
        /// </param>
        /// <param name="global">
        /// Non-zero to also purge the global interactive context.
        /// </param>
        /// <returns>
        /// The number of interactive contexts that were purged.
        /// </returns>
        int PurgeInteractiveContexts(
            Interpreter interpreter, bool nonPrimary, bool global);

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method releases the test context.
        /// </summary>
        /// <param name="global">
        /// Non-zero to release the global test context; zero to release the
        /// per-thread test context.
        /// </param>
        /// <returns>
        /// True if the context was released; otherwise, false.
        /// </returns>
        bool ReleaseTestContext(bool global);

        /// <summary>
        /// This method releases the test context, reporting any error.
        /// </summary>
        /// <param name="global">
        /// Non-zero to release the global test context; zero to release the
        /// per-thread test context.
        /// </param>
        /// <param name="error">
        /// Upon failure, this contains an appropriate error message.
        /// </param>
        /// <returns>
        /// True if the context was released; otherwise, false.
        /// </returns>
        bool ReleaseTestContext(bool global, ref Result error);

        /// <summary>
        /// This method returns the test context for the current thread.
        /// </summary>
        /// <param name="create">
        /// Non-zero to create the test context if it does not already exist.
        /// </param>
        /// <returns>
        /// The test context, or null if none is available and one was not
        /// created.
        /// </returns>
        ITestContext GetTestContext(bool create);

        /// <summary>
        /// This method returns the test context for the current thread,
        /// reporting any error.
        /// </summary>
        /// <param name="create">
        /// Non-zero to create the test context if it does not already exist.
        /// </param>
        /// <param name="error">
        /// Upon failure, this contains an appropriate error message.
        /// </param>
        /// <returns>
        /// The test context, or null if none is available and one was not
        /// created.
        /// </returns>
        ITestContext GetTestContext(bool create, ref Result error);

        /// <summary>
        /// This method returns the number of test contexts currently
        /// maintained.
        /// </summary>
        /// <returns>
        /// The number of test contexts.
        /// </returns>
        int GetTestContextCount();

        /// <summary>
        /// This method purges test contexts that are no longer needed.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter context.  This parameter should not be null.
        /// </param>
        /// <param name="nonPrimary">
        /// Non-zero to purge contexts belonging to non-primary threads.
        /// </param>
        /// <param name="global">
        /// Non-zero to also purge the global test context.
        /// </param>
        /// <returns>
        /// The number of test contexts that were purged.
        /// </returns>
        int PurgeTestContexts(
            Interpreter interpreter, bool nonPrimary, bool global);

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method releases the variable context.
        /// </summary>
        /// <param name="global">
        /// Non-zero to release the global variable context; zero to release the
        /// per-thread variable context.
        /// </param>
        /// <returns>
        /// True if the context was released; otherwise, false.
        /// </returns>
        bool ReleaseVariableContext(bool global);

        /// <summary>
        /// This method releases the variable context, reporting any error.
        /// </summary>
        /// <param name="global">
        /// Non-zero to release the global variable context; zero to release the
        /// per-thread variable context.
        /// </param>
        /// <param name="error">
        /// Upon failure, this contains an appropriate error message.
        /// </param>
        /// <returns>
        /// True if the context was released; otherwise, false.
        /// </returns>
        bool ReleaseVariableContext(bool global, ref Result error);

        /// <summary>
        /// This method returns the variable context for the current thread.
        /// </summary>
        /// <param name="create">
        /// Non-zero to create the variable context if it does not already
        /// exist.
        /// </param>
        /// <returns>
        /// The variable context, or null if none is available and one was not
        /// created.
        /// </returns>
        IVariableContext GetVariableContext(bool create);

        /// <summary>
        /// This method returns the variable context for the current thread,
        /// reporting any error.
        /// </summary>
        /// <param name="create">
        /// Non-zero to create the variable context if it does not already
        /// exist.
        /// </param>
        /// <param name="error">
        /// Upon failure, this contains an appropriate error message.
        /// </param>
        /// <returns>
        /// The variable context, or null if none is available and one was not
        /// created.
        /// </returns>
        IVariableContext GetVariableContext(bool create, ref Result error);

        /// <summary>
        /// This method returns the number of variable contexts currently
        /// maintained.
        /// </summary>
        /// <returns>
        /// The number of variable contexts.
        /// </returns>
        int GetVariableContextCount();

        /// <summary>
        /// This method purges variable contexts that are no longer needed.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter context.  This parameter should not be null.
        /// </param>
        /// <param name="nonPrimary">
        /// Non-zero to purge contexts belonging to non-primary threads.
        /// </param>
        /// <param name="global">
        /// Non-zero to also purge the global variable context.
        /// </param>
        /// <returns>
        /// The number of variable contexts that were purged.
        /// </returns>
        int PurgeVariableContexts(
            Interpreter interpreter, bool nonPrimary, bool global);

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method releases all contexts managed by this instance.
        /// </summary>
        /// <param name="global">
        /// Non-zero to also release global contexts.
        /// </param>
        void Free(bool global);
    }
}
