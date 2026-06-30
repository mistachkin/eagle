/*
 * ScriptThread.cs --
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
using Eagle._Components.Public.Delegates;

namespace Eagle._Interfaces.Public
{
    /// <summary>
    /// This interface represents a managed thread that owns an Eagle
    /// interpreter and evaluates scripts on that thread.  It exposes the
    /// owned thread and interpreter, the configuration governing how the
    /// thread and interpreter are created, properties describing the current
    /// thread state, and methods to start and stop the thread, add CLR
    /// objects, wait for and signal events, queue and send scripts for
    /// evaluation, cancel running scripts, and perform cleanup.  It extends
    /// <see cref="IGetInterpreter" />.
    /// </summary>
    [ObjectId("a93e417d-5ad1-413e-bd26-feb58d04a0f0")]
    public interface IScriptThread : IGetInterpreter
    {
        ///////////////////////////////////////////////////////////////////////
        // OWNED RESOURCES PROPERTIES
        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Gets the managed thread owned by this script thread.
        /// </summary>
        Thread Thread { get; }

        ///////////////////////////////////////////////////////////////////////
        // OBJECT IDENTITY & AFFINITY PROPERTIES
        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Gets the unique identifier for this script thread.
        /// </summary>
        long Id { get; }
        /// <summary>
        /// Gets the name of this script thread.
        /// </summary>
        string Name { get; }
        /// <summary>
        /// Gets the number of active script threads in the current application
        /// domain.
        /// </summary>
        int ActiveCount { get; } // NOTE: How many in this AppDomain?

        ///////////////////////////////////////////////////////////////////////
        // THREAD CREATION & SETUP PROPERTIES
        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Gets the flags used to control the creation and behavior of this
        /// script thread.
        /// </summary>
        ThreadFlags ThreadFlags { get; }
        /// <summary>
        /// Gets the maximum stack size, in bytes, used when creating the
        /// underlying thread.
        /// </summary>
        int MaxStackSize { get; }
        /// <summary>
        /// Gets the default timeout, in milliseconds, used for wait operations on
        /// this script thread.
        /// </summary>
        int Timeout { get; }

        ///////////////////////////////////////////////////////////////////////
        // THREAD CREATION PROPERTIES
        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Gets a value indicating whether the underlying thread is configured
        /// for user-interface use (i.e. single-threaded apartment).
        /// </summary>
        bool UserInterface { get; }
        /// <summary>
        /// Gets a value indicating whether the underlying thread is a background
        /// thread.
        /// </summary>
        bool IsBackground { get; }

        ///////////////////////////////////////////////////////////////////////
        // INTERPRETER CREATION & SETUP PROPERTIES
        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Gets the command-line arguments used when creating the interpreter
        /// owned by this script thread, if any.
        /// </summary>
        IEnumerable<string> Args { get; }
        /// <summary>
        /// Gets the host used by the interpreter owned by this script thread, if
        /// any.
        /// </summary>
        IHost Host { get; }
        /// <summary>
        /// Gets the flags used to create the interpreter owned by this script
        /// thread.
        /// </summary>
        CreateFlags CreateFlags { get; }
        /// <summary>
        /// Gets the flags used to create the host for the interpreter owned by
        /// this script thread.
        /// </summary>
        HostCreateFlags HostCreateFlags { get; }
        /// <summary>
        /// Gets the flags used to initialize the interpreter owned by this script
        /// thread.
        /// </summary>
        InitializeFlags InitializeFlags { get; }
        /// <summary>
        /// Gets the flags used when locating and loading scripts for the
        /// interpreter owned by this script thread.
        /// </summary>
        ScriptFlags ScriptFlags { get; }
        /// <summary>
        /// Gets the flags applied to the interpreter owned by this script
        /// thread.
        /// </summary>
        InterpreterFlags InterpreterFlags { get; }

        ///////////////////////////////////////////////////////////////////////
        // INTERPRETER HANDLING PROPERTIES
        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Gets a value indicating whether the interpreter owned by this script
        /// thread is made available to scripts evaluated on it.
        /// </summary>
        bool UseSelf { get; }
        /// <summary>
        /// Gets a value indicating whether the active call stack of the owning
        /// interpreter is used when evaluating scripts on this script thread.
        /// </summary>
        bool UseActiveStack { get; }

        ///////////////////////////////////////////////////////////////////////
        // ERROR HANDLING PROPERTIES
        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Gets a value indicating whether error reporting for this script thread
        /// is suppressed.
        /// </summary>
        bool Quiet { get; }
        /// <summary>
        /// Gets a value indicating whether background error reporting is disabled
        /// for this script thread.
        /// </summary>
        bool NoBackgroundError { get; }

        ///////////////////////////////////////////////////////////////////////
        // EVENT HANDLING PROPERTIES
        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Gets the script associated with the event-handling loop of this script
        /// thread, if any.
        /// </summary>
        IScript Script { get; }
        /// <summary>
        /// Gets the name of the variable used to signal the event-handling loop
        /// of this script thread, if any.
        /// </summary>
        string VarName { get; }
        /// <summary>
        /// Gets the flags used to control how this script thread waits for
        /// events.
        /// </summary>
        EventWaitFlags EventWaitFlags { get; }
        /// <summary>
        /// Gets the variable flags used when accessing the signal variable for
        /// this script thread.
        /// </summary>
        VariableFlags EventVariableFlags { get; }
        /// <summary>
        /// Gets a value indicating whether complaints raised while handling
        /// events on this script thread are suppressed.
        /// </summary>
        bool NoComplain { get; }

        ///////////////////////////////////////////////////////////////////////
        // DIAGNOSTIC READ-WRITE PROPERTIES
        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Gets or sets a value indicating whether verbose diagnostic output is
        /// enabled for this script thread.
        /// </summary>
        bool Verbose { get; set; }
        /// <summary>
        /// Gets or sets a value indicating whether debug mode is enabled for this
        /// script thread.
        /// </summary>
        bool Debug { get; set; }
        /// <summary>
        /// Gets or sets the return code produced by the most recent script
        /// evaluated on this script thread.
        /// </summary>
        ReturnCode ReturnCode { get; set; }
        /// <summary>
        /// Gets or sets the result produced by the most recent script evaluated
        /// on this script thread.
        /// </summary>
        Result Result { get; set; }

        ///////////////////////////////////////////////////////////////////////
        // THREAD STATE PROPERTIES
        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Gets a value indicating whether the underlying thread is currently
        /// alive.
        /// </summary>
        bool IsAlive { get; }
        /// <summary>
        /// Gets a value indicating whether this script thread is currently busy
        /// processing work.
        /// </summary>
        bool IsBusy { get; }
        /// <summary>
        /// Gets a value indicating whether this script thread has been
        /// disposed.
        /// </summary>
        bool IsDisposed { get; }

        ///////////////////////////////////////////////////////////////////////
        // INTERPRETER DISPOSAL & PURGING PROPERTIES
        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Gets a value indicating whether the interpreter owned by this script
        /// thread is returned to an interpreter pool when it is disposed.
        /// </summary>
        bool UsePool { get; }
        /// <summary>
        /// Gets a value indicating whether the global call frame of the owning
        /// interpreter is purged during cleanup.
        /// </summary>
        bool PurgeGlobal { get; }
        /// <summary>
        /// Gets a value indicating whether aborting the underlying thread is
        /// disabled during cleanup.
        /// </summary>
        bool NoAbort { get; }

        ///////////////////////////////////////////////////////////////////////
        // THREAD STATE METHODS
        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Starts the underlying thread for this script thread.
        /// </summary>
        /// <returns>
        /// True if the thread was started successfully; otherwise,
        /// false.
        /// </returns>
        bool Start();

        /// <summary>
        /// Stops this script thread, waiting for it to finish its current
        /// work.
        /// </summary>
        /// <returns>
        /// True if the thread was stopped successfully; otherwise,
        /// false.
        /// </returns>
        bool Stop();
        /// <summary>
        /// Stops this script thread, optionally forcing it to stop.
        /// </summary>
        /// <param name="force">
        /// True to forcibly stop the thread; otherwise, false.
        /// </param>
        /// <returns>
        /// True if the thread was stopped successfully; otherwise,
        /// false.
        /// </returns>
        bool Stop(bool force);

        ///////////////////////////////////////////////////////////////////////
        // CLR OBJECT INTEGRATION METHODS
        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Adds the specified CLR object to the interpreter owned by this script
        /// thread.
        /// </summary>
        /// <param name="value">
        /// The object to add.  This parameter may be null.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise, a non-Ok value.
        /// </returns>
        ReturnCode AddObject(object value);
        /// <summary>
        /// Adds the specified CLR object to the interpreter owned by this script
        /// thread.
        /// </summary>
        /// <param name="value">
        /// The object to add.  This parameter may be null.
        /// </param>
        /// <param name="result">
        /// Upon success, this receives the name assigned to the added
        /// object; upon failure, this receives an appropriate error message.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise, a non-Ok
        /// value with details placed in the <paramref name="result" /> parameter.
        /// </returns>
        ReturnCode AddObject(object value, ref Result result);
        /// <summary>
        /// Adds the specified CLR object to the interpreter owned by this script
        /// thread, optionally creating a command alias for it.
        /// </summary>
        /// <param name="value">
        /// The object to add.  This parameter may be null.
        /// </param>
        /// <param name="alias">
        /// True to create a command alias for the added object;
        /// otherwise, false.
        /// </param>
        /// <param name="result">
        /// Upon success, this receives the name assigned to the added
        /// object; upon failure, this receives an appropriate error message.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise, a non-Ok
        /// value with details placed in the <paramref name="result" /> parameter.
        /// </returns>
        ReturnCode AddObject(object value, bool alias, ref Result result);

        /// <summary>
        /// Adds the specified CLR object to the interpreter owned by this script
        /// thread, using the specified object options.
        /// </summary>
        /// <param name="objectOptionType">
        /// The kind of object options to use when adding the
        /// object.
        /// </param>
        /// <param name="value">
        /// The object to add.  This parameter may be null.
        /// </param>
        /// <param name="alias">
        /// True to create a command alias for the added object;
        /// otherwise, false.
        /// </param>
        /// <param name="result">
        /// Upon success, this receives the name assigned to the added
        /// object; upon failure, this receives an appropriate error message.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise, a non-Ok
        /// value with details placed in the <paramref name="result" /> parameter.
        /// </returns>
        ReturnCode AddObject(ObjectOptionType objectOptionType,
            object value, bool alias, ref Result result);

        /// <summary>
        /// Adds the specified CLR object to the interpreter owned by this script
        /// thread, using the specified object options.
        /// </summary>
        /// <param name="objectOptionType">
        /// The kind of object options to use when adding the
        /// object.
        /// </param>
        /// <param name="value">
        /// The object to add.  This parameter may be null.
        /// </param>
        /// <param name="alias">
        /// True to create a command alias for the added object;
        /// otherwise, false.
        /// </param>
        /// <param name="aliasReference">
        /// True to add an opaque object handle reference for the
        /// created alias; otherwise, false.
        /// </param>
        /// <param name="result">
        /// Upon success, this receives the name assigned to the added
        /// object; upon failure, this receives an appropriate error message.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise, a non-Ok
        /// value with details placed in the <paramref name="result" /> parameter.
        /// </returns>
        ReturnCode AddObject(ObjectOptionType objectOptionType, object value,
            bool alias, bool aliasReference, ref Result result);

        /// <summary>
        /// Adds the specified CLR object to the interpreter owned by this script
        /// thread, using the specified object options and flags.
        /// </summary>
        /// <param name="objectOptionType">
        /// The kind of object options to use when adding the
        /// object.
        /// </param>
        /// <param name="objectFlags">
        /// The flags to associate with the added object.
        /// </param>
        /// <param name="value">
        /// The object to add.  This parameter may be null.
        /// </param>
        /// <param name="alias">
        /// True to create a command alias for the added object;
        /// otherwise, false.
        /// </param>
        /// <param name="aliasReference">
        /// True to add an opaque object handle reference for the
        /// created alias; otherwise, false.
        /// </param>
        /// <param name="result">
        /// Upon success, this receives the name assigned to the added
        /// object; upon failure, this receives an appropriate error message.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise, a non-Ok
        /// value with details placed in the <paramref name="result" /> parameter.
        /// </returns>
        ReturnCode AddObject(ObjectOptionType objectOptionType,
            ObjectFlags objectFlags, object value, bool alias,
            bool aliasReference, ref Result result);

        /// <summary>
        /// Adds the specified CLR object to the interpreter owned by this script
        /// thread, using the specified name, object options, and flags.
        /// </summary>
        /// <param name="objectOptionType">
        /// The kind of object options to use when adding the
        /// object.
        /// </param>
        /// <param name="objectName">
        /// The name to assign to the added object, or null to
        /// generate one automatically.
        /// </param>
        /// <param name="objectFlags">
        /// The flags to associate with the added object.
        /// </param>
        /// <param name="value">
        /// The object to add.  This parameter may be null.
        /// </param>
        /// <param name="alias">
        /// True to create a command alias for the added object;
        /// otherwise, false.
        /// </param>
        /// <param name="aliasReference">
        /// True to add an opaque object handle reference for the
        /// created alias; otherwise, false.
        /// </param>
        /// <param name="result">
        /// Upon success, this receives the name assigned to the added
        /// object; upon failure, this receives an appropriate error message.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise, a non-Ok
        /// value with details placed in the <paramref name="result" /> parameter.
        /// </returns>
        ReturnCode AddObject(ObjectOptionType objectOptionType,
            string objectName, ObjectFlags objectFlags, object value,
            bool alias, bool aliasReference, ref Result result);

        ///////////////////////////////////////////////////////////////////////
        // SYNCHRONOUS WAIT METHODS
        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Waits for this script thread to start.
        /// </summary>
        /// <returns>
        /// True if the thread started within the wait period; otherwise,
        /// false.
        /// </returns>
        bool WaitForStart();
        /// <summary>
        /// Waits for this script thread to start, up to the specified timeout.
        /// </summary>
        /// <param name="timeout">
        /// The maximum time to wait, in milliseconds.
        /// </param>
        /// <returns>
        /// True if the thread started within the wait period; otherwise,
        /// false.
        /// </returns>
        bool WaitForStart(int timeout);
        /// <summary>
        /// Waits for this script thread to start, up to the specified timeout.
        /// </summary>
        /// <param name="timeout">
        /// The maximum time to wait, in milliseconds.
        /// </param>
        /// <param name="strict">
        /// True to treat an inability to wait as failure; otherwise,
        /// false.
        /// </param>
        /// <returns>
        /// True if the thread started within the wait period; otherwise,
        /// false.
        /// </returns>
        bool WaitForStart(int timeout, bool strict);

        /// <summary>
        /// Waits for this script thread to end.
        /// </summary>
        /// <returns>
        /// True if the thread ended within the wait period; otherwise,
        /// false.
        /// </returns>
        bool WaitForEnd();
        /// <summary>
        /// Waits for this script thread to end, up to the specified timeout.
        /// </summary>
        /// <param name="timeout">
        /// The maximum time to wait, in milliseconds.
        /// </param>
        /// <returns>
        /// True if the thread ended within the wait period; otherwise,
        /// false.
        /// </returns>
        bool WaitForEnd(int timeout);
        /// <summary>
        /// Waits for this script thread to end, up to the specified timeout.
        /// </summary>
        /// <param name="timeout">
        /// The maximum time to wait, in milliseconds.
        /// </param>
        /// <param name="strict">
        /// True to treat an inability to wait as failure; otherwise,
        /// false.
        /// </param>
        /// <returns>
        /// True if the thread ended within the wait period; otherwise,
        /// false.
        /// </returns>
        bool WaitForEnd(int timeout, bool strict);

        /// <summary>
        /// Waits for the event queue of this script thread to become empty.
        /// </summary>
        /// <returns>
        /// True if the queue became empty within the wait period;
        /// otherwise, false.
        /// </returns>
        bool WaitForEmpty();
        /// <summary>
        /// Waits for the event queue of this script thread to become empty, up
        /// to the specified timeout.
        /// </summary>
        /// <param name="timeout">
        /// The maximum time to wait, in milliseconds.
        /// </param>
        /// <returns>
        /// True if the queue became empty within the wait period;
        /// otherwise, false.
        /// </returns>
        bool WaitForEmpty(int timeout);
        /// <summary>
        /// Waits for the event queue of this script thread to become empty, up
        /// to the specified timeout.
        /// </summary>
        /// <param name="timeout">
        /// The maximum time to wait, in milliseconds.
        /// </param>
        /// <param name="strict">
        /// True to treat an inability to wait as failure; otherwise,
        /// false.
        /// </param>
        /// <returns>
        /// True if the queue became empty within the wait period;
        /// otherwise, false.
        /// </returns>
        bool WaitForEmpty(int timeout, bool strict);
        /// <summary>
        /// Waits for the event queue of this script thread to become empty, up
        /// to the specified timeout, optionally also waiting for the thread to
        /// become idle.
        /// </summary>
        /// <param name="timeout">
        /// The maximum time to wait, in milliseconds.
        /// </param>
        /// <param name="idle">
        /// True to also wait for the thread to become idle; otherwise,
        /// false.
        /// </param>
        /// <param name="strict">
        /// True to treat an inability to wait as failure; otherwise,
        /// false.
        /// </param>
        /// <returns>
        /// True if the queue became empty within the wait period;
        /// otherwise, false.
        /// </returns>
        bool WaitForEmpty(int timeout, bool idle, bool strict);

        /// <summary>
        /// Waits for an event to be processed by this script thread.
        /// </summary>
        /// <returns>
        /// True if an event was processed within the wait period;
        /// otherwise, false.
        /// </returns>
        bool WaitForEvent();
        /// <summary>
        /// Waits for an event to be processed by this script thread, up to the
        /// specified timeout.
        /// </summary>
        /// <param name="timeout">
        /// The maximum time to wait, in milliseconds.
        /// </param>
        /// <returns>
        /// True if an event was processed within the wait period;
        /// otherwise, false.
        /// </returns>
        bool WaitForEvent(int timeout);
        /// <summary>
        /// Waits for an event to be processed by this script thread, up to the
        /// specified timeout.
        /// </summary>
        /// <param name="timeout">
        /// The maximum time to wait, in milliseconds.
        /// </param>
        /// <param name="strict">
        /// True to treat an inability to wait as failure; otherwise,
        /// false.
        /// </param>
        /// <returns>
        /// True if an event was processed within the wait period;
        /// otherwise, false.
        /// </returns>
        bool WaitForEvent(int timeout, bool strict);
        /// <summary>
        /// Waits for an event to be processed by this script thread, up to the
        /// specified timeout, optionally also waiting for the thread to become
        /// idle.
        /// </summary>
        /// <param name="timeout">
        /// The maximum time to wait, in milliseconds.
        /// </param>
        /// <param name="idle">
        /// True to also wait for the thread to become idle; otherwise,
        /// false.
        /// </param>
        /// <param name="strict">
        /// True to treat an inability to wait as failure; otherwise,
        /// false.
        /// </param>
        /// <returns>
        /// True if an event was processed within the wait period;
        /// otherwise, false.
        /// </returns>
        bool WaitForEvent(int timeout, bool idle, bool strict);

        ///////////////////////////////////////////////////////////////////////
        // ASYNCHRONOUS QUEUEING METHODS (VIA EVENT MANAGER)
        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Queues the specified callback for asynchronous execution by the event
        /// manager of this script thread.
        /// </summary>
        /// <param name="callback">
        /// The callback to queue.  This parameter should not be null.
        /// </param>
        /// <param name="clientData">
        /// The extra data to pass to the callback, if any.  This
        /// parameter may be null.
        /// </param>
        /// <returns>
        /// True if the callback was queued successfully; otherwise,
        /// false.
        /// </returns>
        bool Queue(EventCallback callback, IClientData clientData);
        /// <summary>
        /// Queues the specified callback for asynchronous execution by the event
        /// manager of this script thread at the specified time.
        /// </summary>
        /// <param name="dateTime">
        /// The time at which the callback should be executed.
        /// </param>
        /// <param name="callback">
        /// The callback to queue.  This parameter should not be null.
        /// </param>
        /// <param name="clientData">
        /// The extra data to pass to the callback, if any.  This
        /// parameter may be null.
        /// </param>
        /// <returns>
        /// True if the callback was queued successfully; otherwise,
        /// false.
        /// </returns>
        bool Queue(DateTime dateTime, EventCallback callback,
            IClientData clientData);

        ///////////////////////////////////////////////////////////////////////
        // ASYNCHRONOUS EVALUATION METHODS (VIA ENGINE)
        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Queues the specified script for asynchronous evaluation by the
        /// engine.
        /// </summary>
        /// <param name="text">
        /// The script to evaluate.  This parameter should not be null.
        /// </param>
        /// <returns>
        /// True if the script was queued successfully; otherwise,
        /// false.
        /// </returns>
        bool Queue(string text);
        /// <summary>
        /// Queues the specified script for asynchronous evaluation by the engine
        /// at the specified time.
        /// </summary>
        /// <param name="dateTime">
        /// The time at which the script should be evaluated.
        /// </param>
        /// <param name="text">
        /// The script to evaluate.  This parameter should not be null.
        /// </param>
        /// <returns>
        /// True if the script was queued successfully; otherwise,
        /// false.
        /// </returns>
        bool Queue(DateTime dateTime, string text);
        /// <summary>
        /// Queues the specified script for asynchronous evaluation by the engine,
        /// invoking the specified callback with the result.
        /// </summary>
        /// <param name="text">
        /// The script to evaluate.  This parameter should not be null.
        /// </param>
        /// <param name="callback">
        /// The callback to invoke with the result of the evaluation, if
        /// any.  This parameter may be null.
        /// </param>
        /// <param name="clientData">
        /// The extra data to pass to the callback, if any.  This
        /// parameter may be null.
        /// </param>
        /// <returns>
        /// True if the script was queued successfully; otherwise,
        /// false.
        /// </returns>
        bool Queue(string text, AsynchronousCallback callback,
            IClientData clientData);

        ///////////////////////////////////////////////////////////////////////
        // SYNCHRONOUS EVALUATION METHODS (VIA EVENT MANAGER AND/OR ENGINE)
        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Evaluates the specified script synchronously on this script thread and
        /// returns its result.
        /// </summary>
        /// <param name="text">
        /// The script to evaluate.  This parameter should not be null.
        /// </param>
        /// <param name="result">
        /// Upon success, this receives the result of the evaluation;
        /// upon failure, this receives an appropriate error message.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise, a non-Ok
        /// value with details placed in the <paramref name="result" /> parameter.
        /// </returns>
        ReturnCode Send(string text, ref Result result);
        /// <summary>
        /// Evaluates the specified script synchronously on this script thread and
        /// returns its result.
        /// </summary>
        /// <param name="text">
        /// The script to evaluate.  This parameter should not be null.
        /// </param>
        /// <param name="useEngine">
        /// True to evaluate the script directly via the engine; false
        /// to dispatch it via the event manager.
        /// </param>
        /// <param name="result">
        /// Upon success, this receives the result of the evaluation;
        /// upon failure, this receives an appropriate error message.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise, a non-Ok
        /// value with details placed in the <paramref name="result" /> parameter.
        /// </returns>
        ReturnCode Send(string text, bool useEngine, ref Result result);
        /// <summary>
        /// Evaluates the specified script synchronously on this script thread and
        /// returns its result, up to the specified timeout.
        /// </summary>
        /// <param name="text">
        /// The script to evaluate.  This parameter should not be null.
        /// </param>
        /// <param name="timeout">
        /// The maximum time to wait for the evaluation to complete, in
        /// milliseconds.
        /// </param>
        /// <param name="useEngine">
        /// True to evaluate the script directly via the engine; false
        /// to dispatch it via the event manager.
        /// </param>
        /// <param name="result">
        /// Upon success, this receives the result of the evaluation;
        /// upon failure, this receives an appropriate error message.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise, a non-Ok
        /// value with details placed in the <paramref name="result" /> parameter.
        /// </returns>
        ReturnCode Send(string text, int timeout, bool useEngine,
            ref Result result);

        ///////////////////////////////////////////////////////////////////////
        // ASYNCHRONOUS SIGNALING METHODS
        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Signals the event-handling loop of this script thread by setting its
        /// signal variable to the specified value.
        /// </summary>
        /// <param name="value">
        /// The value to assign to the signal variable.  This parameter
        /// may be null.
        /// </param>
        /// <returns>
        /// True if the signal was delivered successfully; otherwise,
        /// false.
        /// </returns>
        bool Signal(string value);
        /// <summary>
        /// Wakes up the event-handling loop of this script thread.
        /// </summary>
        /// <returns>
        /// True if the thread was woken successfully; otherwise,
        /// false.
        /// </returns>
        bool WakeUp();

        ///////////////////////////////////////////////////////////////////////
        // ASYNCHRONOUS SCRIPT CANCELLATION METHODS
        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Requests cancellation of any script currently being evaluated on this
        /// script thread.
        /// </summary>
        /// <param name="cancelFlags">
        /// The flags used to control how cancellation is
        /// performed.
        /// </param>
        /// <returns>
        /// True if cancellation was requested successfully; otherwise,
        /// false.
        /// </returns>
        bool Cancel(CancelFlags cancelFlags);
        /// <summary>
        /// Requests cancellation of any script currently being evaluated on this
        /// script thread.
        /// </summary>
        /// <param name="cancelFlags">
        /// The flags used to control how cancellation is
        /// performed.
        /// </param>
        /// <param name="error">
        /// Upon failure, receives an appropriate error message.
        /// </param>
        /// <returns>
        /// True if cancellation was requested successfully; otherwise,
        /// false.
        /// </returns>
        bool Cancel(CancelFlags cancelFlags, ref Result error);

        /// <summary>
        /// Resets any pending cancellation for scripts evaluated on this script
        /// thread.
        /// </summary>
        /// <param name="cancelFlags">
        /// The flags used to control how the cancellation state is
        /// reset.
        /// </param>
        /// <returns>
        /// True if the cancellation state was reset successfully;
        /// otherwise, false.
        /// </returns>
        bool ResetCancel(CancelFlags cancelFlags);
        /// <summary>
        /// Resets any pending cancellation for scripts evaluated on this script
        /// thread.
        /// </summary>
        /// <param name="cancelFlags">
        /// The flags used to control how the cancellation state is
        /// reset.
        /// </param>
        /// <param name="error">
        /// Upon failure, receives an appropriate error message.
        /// </param>
        /// <returns>
        /// True if the cancellation state was reset successfully;
        /// otherwise, false.
        /// </returns>
        bool ResetCancel(CancelFlags cancelFlags, ref Result error);

        ///////////////////////////////////////////////////////////////////////
        // CLEANUP METHODS (NON-PRIMARY THREAD CONTEXTS)
        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Performs cleanup for this script thread when running in a non-primary
        /// thread context.
        /// </summary>
        /// <returns>
        /// True if cleanup completed successfully; otherwise, false.
        /// </returns>
        bool Cleanup();
    }
}
