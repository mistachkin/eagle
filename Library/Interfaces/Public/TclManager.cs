/*
 * TclManager.cs --
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
using Eagle._Attributes;
using Eagle._Components.Private.Tcl;
using Eagle._Components.Public;

#if TCL_WRAPPER
using Eagle._Interfaces.Private.Tcl;
#endif

namespace Eagle._Interfaces.Public
{
    /// <summary>
    /// This interface is implemented by entities that manage an embedded,
    /// natively loaded Tcl library on behalf of an Eagle interpreter.  It
    /// provides the ability to find, load, and unload the native Tcl library,
    /// to query its state and patch level, and to evaluate Tcl expressions,
    /// scripts, and files, to perform Tcl string substitution, and to get,
    /// set, and unset Tcl variables, optionally targeting a specific master
    /// Tcl interpreter by name.  These members are only available when the
    /// library is compiled with native Tcl integration support.
    /// </summary>
    [ObjectId("e3c85670-a38f-4f0a-b99e-e6c39c51359b")]
    public interface ITclManager
    {
        ///////////////////////////////////////////////////////////////////////
        // TCL SYNCHRONIZATION SUPPORT
        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Gets the object used to synchronize access to the native Tcl
        /// integration subsystem.  Most callers should not need to use this.
        /// </summary>
        //
        // NOTE: You should not need to use this.
        //
        object TclSyncRoot { get; }

        ///////////////////////////////////////////////////////////////////////
        // TCL READ-ONLY SUPPORT
        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Gets or sets a value indicating whether the native Tcl integration
        /// subsystem is in read-only mode.  When this property is true,
        /// operations that would modify Tcl state are disallowed.
        /// </summary>
        //
        // NOTE: For dealing with read-only mode.
        //
        bool TclReadOnly { get; set; }

        ///////////////////////////////////////////////////////////////////////
        // TCL LOADER SUPPORT
        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Determines whether the native Tcl library is currently loaded and
        /// available for use.
        /// </summary>
        /// <param name="error">
        /// Upon failure, this may contain an appropriate error message.
        /// </param>
        /// <returns>
        /// True if the native Tcl library is loaded and available; otherwise,
        /// false.
        /// </returns>
        //
        // NOTE: Is Tcl loaded and available?
        //
        bool HasTcl(ref Result error);

        /// <summary>
        /// Determines whether the current thread is the correct thread for using
        /// the native Tcl library.
        /// </summary>
        /// <returns>
        /// True if the current thread may be used to access the native Tcl
        /// library; otherwise, false.
        /// </returns>
        //
        // NOTE: Is the current thread the correct one for Tcl usage?
        //
        bool IsTclThread();

        /// <summary>
        /// Queries the patch level of the native Tcl library that was previously
        /// loaded.
        /// </summary>
        /// <param name="patchLevel">
        /// Upon success, this is set to the patch level of the native Tcl library
        /// that was previously loaded.
        /// </param>
        /// <param name="error">
        /// Upon failure, this may contain an appropriate error message.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise, a non-Ok value
        /// with details placed in the <paramref name="error" /> parameter.
        /// </returns>
        //
        // NOTE: Returns the patch level of Tcl that was previously loaded.
        //
        ReturnCode GetTclPatchLevel(
            ref Version patchLevel,
            ref Result error
            );

        /// <summary>
        /// Attempts to find and load the native Tcl library now.
        /// </summary>
        /// <param name="findFlags">
        /// The flags used to control how the native Tcl library is located.
        /// </param>
        /// <param name="loadFlags">
        /// The flags used to control how the native Tcl library is loaded.
        /// </param>
        /// <param name="paths">
        /// The list of candidate paths to search for the native Tcl library, if
        /// any.  This parameter may be null.
        /// </param>
        /// <param name="text">
        /// The optional string used to help locate the native Tcl library, if
        /// any.  This parameter may be null.
        /// </param>
        /// <param name="minimumRequired">
        /// The minimum version of the native Tcl library that is acceptable, if
        /// any.  This parameter may be null.
        /// </param>
        /// <param name="maximumRequired">
        /// The maximum version of the native Tcl library that is acceptable, if
        /// any.  This parameter may be null.
        /// </param>
        /// <param name="unknown">
        /// The version to assume for the native Tcl library when its actual
        /// version cannot be determined, if any.  This parameter may be null.
        /// </param>
        /// <param name="clientData">
        /// The extra data to associate with the loaded native Tcl library, if
        /// any.  This parameter may be null.
        /// </param>
        /// <param name="result">
        /// Upon success, this may contain a result value associated with the load
        /// operation.  Upon failure, this must contain an appropriate error
        /// message.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise, a non-Ok value
        /// with details placed in the <paramref name="result" /> parameter.
        /// </returns>
        //
        // NOTE: Attempt to find and load the native Tcl library now.
        //
        ReturnCode LoadTcl(
            FindFlags findFlags,
            LoadFlags loadFlags,
            IEnumerable<string> paths,
            string text,
            Version minimumRequired,
            Version maximumRequired,
            Version unknown,
            IClientData clientData,
            ref Result result
            );

        /// <summary>
        /// Attempts to unload the native Tcl library now.
        /// </summary>
        /// <param name="unloadFlags">
        /// The flags used to control how the native Tcl library is unloaded.
        /// </param>
        /// <param name="error">
        /// Upon failure, this may contain an appropriate error message.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise, a non-Ok value
        /// with details placed in the <paramref name="error" /> parameter.
        /// </returns>
        //
        // NOTE: Attempt to unload the native Tcl library now.
        //
        ReturnCode UnloadTcl(
            UnloadFlags unloadFlags,
            ref Result error
            );

        ///////////////////////////////////////////////////////////////////////

#if TCL_WRAPPER
        /// <summary>
        /// Gets or sets the object that provides low-level access to the native
        /// Tcl application programming interface.  Most callers should not need
        /// to use this.
        /// </summary>
        //
        // NOTE: You should not need to use this either.
        //
        ITclApi TclApi { get; set; }
#endif

        ///////////////////////////////////////////////////////////////////////
        // TCL INTERPRETER SUPPORT
        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Determines whether the cancellation of script evaluation has been
        /// requested for the specified master Tcl interpreter.
        /// </summary>
        /// <param name="name">
        /// The name of the master Tcl interpreter to query, if any.  This
        /// parameter may be null to use the default master Tcl interpreter.
        /// </param>
        /// <param name="error">
        /// Upon failure, this may contain an appropriate error message.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> if the script evaluation of the specified
        /// master Tcl interpreter has been canceled; otherwise, a non-Ok value
        /// with details placed in the <paramref name="error" /> parameter.
        /// </returns>
        ReturnCode IsTclInterpreterCanceled(
            string name,
            ref Result error
            );

        /// <summary>
        /// Determines whether the cancellation of script evaluation has been
        /// requested for the specified master Tcl interpreter.
        /// </summary>
        /// <param name="name">
        /// The name of the master Tcl interpreter to query, if any.  This
        /// parameter may be null to use the default master Tcl interpreter.
        /// </param>
        /// <param name="flags">
        /// The flags used to control how the cancellation state of the master Tcl
        /// interpreter is queried.
        /// </param>
        /// <param name="error">
        /// Upon failure, this may contain an appropriate error message.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> if the script evaluation of the specified
        /// master Tcl interpreter has been canceled; otherwise, a non-Ok value
        /// with details placed in the <paramref name="error" /> parameter.
        /// </returns>
        ReturnCode IsTclInterpreterCanceled(
            string name,
            Tcl_CanceledFlags flags,
            ref Result error
            );

        /// <summary>
        /// Determines whether the specified master Tcl interpreter is ready to
        /// evaluate a script.
        /// </summary>
        /// <param name="name">
        /// The name of the master Tcl interpreter to query, if any.  This
        /// parameter may be null to use the default master Tcl interpreter.
        /// </param>
        /// <param name="deleted">
        /// True if a deleted master Tcl interpreter should be considered ready;
        /// otherwise, false.
        /// </param>
        /// <param name="error">
        /// Upon failure, this may contain an appropriate error message.
        /// </param>
        /// <returns>
        /// True if the specified master Tcl interpreter is ready; otherwise,
        /// false.
        /// </returns>
        bool IsTclInterpreterReady(
            string name,
            bool deleted,
            ref Result error
            );

        /// <summary>
        /// Determines whether the specified master Tcl interpreter is currently
        /// active (i.e. evaluating a script).
        /// </summary>
        /// <param name="name">
        /// The name of the master Tcl interpreter to query, if any.  This
        /// parameter may be null to use the default master Tcl interpreter.
        /// </param>
        /// <returns>
        /// True if the specified master Tcl interpreter is active; otherwise,
        /// false.
        /// </returns>
        bool IsTclInterpreterActive(string name);
        /// <summary>
        /// Queries the line number where an error last occurred during script
        /// evaluation in the specified master Tcl interpreter.
        /// </summary>
        /// <param name="name">
        /// The name of the master Tcl interpreter to query, if any.  This
        /// parameter may be null to use the default master Tcl interpreter.
        /// </param>
        /// <returns>
        /// The line number where an error last occurred, or zero if no error line
        /// is available.
        /// </returns>
        int GetTclInterpreterErrorLine(string name);

#if TCL_THREADS
        ///////////////////////////////////////////////////////////////////////
        // TCL THREAD EVENT SUPPORT
        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Queues an event for processing on the thread associated with the
        /// specified Tcl interpreter.
        /// </summary>
        /// <param name="threadName">
        /// The name of the Tcl thread to queue the event for, if any.  This
        /// parameter may be null to use the default Tcl thread.
        /// </param>
        /// <param name="type">
        /// The type of event to queue.
        /// </param>
        /// <param name="flags">
        /// The flags used to control how the event is processed.
        /// </param>
        /// <param name="data">
        /// The extra data to associate with the event, if any.  This parameter
        /// may be null.
        /// </param>
        /// <param name="synchronous">
        /// True to process the event synchronously, waiting for it to complete
        /// before returning; otherwise, the event is queued for asynchronous
        /// processing.
        /// </param>
        /// <param name="result">
        /// Upon success, this may contain the result value produced by the event.
        /// Upon failure, this must contain an appropriate error message.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise, a non-Ok value
        /// with details placed in the <paramref name="result" /> parameter.
        /// </returns>
        ReturnCode QueueTclThreadEvent(
            string threadName,
            EventType type,
            EventFlags flags,
            object data,
            bool synchronous,
            ref Result result
            );

        /// <summary>
        /// Queues an event for processing on the thread associated with the
        /// specified Tcl interpreter.
        /// </summary>
        /// <param name="threadName">
        /// The name of the Tcl thread to queue the event for, if any.  This
        /// parameter may be null to use the default Tcl thread.
        /// </param>
        /// <param name="type">
        /// The type of event to queue.
        /// </param>
        /// <param name="flags">
        /// The flags used to control how the event is processed.
        /// </param>
        /// <param name="data">
        /// The extra data to associate with the event, if any.  This parameter
        /// may be null.
        /// </param>
        /// <param name="synchronous">
        /// True to process the event synchronously, waiting for it to complete
        /// before returning; otherwise, the event is queued for asynchronous
        /// processing.
        /// </param>
        /// <param name="result">
        /// Upon success, this may contain the result value produced by the event.
        /// Upon failure, this must contain an appropriate error message.
        /// </param>
        /// <param name="errorLine">
        /// Upon failure, this is set to the line number where an error occurred
        /// while processing the event, if applicable.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise, a non-Ok value
        /// with details placed in the <paramref name="result" /> parameter.
        /// </returns>
        ReturnCode QueueTclThreadEvent(
            string threadName,
            EventType type,
            EventFlags flags,
            object data,
            bool synchronous,
            ref Result result,
            ref int errorLine
            );
#endif

        ///////////////////////////////////////////////////////////////////////
        // TCL SCRIPT CANCELLATION SUPPORT
        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Requests cancellation of the script currently being evaluated in the
        /// specified master Tcl interpreter.
        /// </summary>
        /// <param name="interpName">
        /// The name of the master Tcl interpreter whose script evaluation is to
        /// be canceled, if any.  This parameter may be null to use the default
        /// master Tcl interpreter.
        /// </param>
        /// <param name="result">
        /// The result value to associate with the cancellation, if any.  This
        /// parameter may be null.
        /// </param>
        /// <param name="error">
        /// Upon failure, this may contain an appropriate error message.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise, a non-Ok value
        /// with details placed in the <paramref name="error" /> parameter.
        /// </returns>
        ReturnCode CancelTclEvaluate(
            string interpName,
            Result result,
            ref Result error
            );

        /// <summary>
        /// Requests cancellation of the script currently being evaluated in the
        /// specified master Tcl interpreter.
        /// </summary>
        /// <param name="interpName">
        /// The name of the master Tcl interpreter whose script evaluation is to
        /// be canceled, if any.  This parameter may be null to use the default
        /// master Tcl interpreter.
        /// </param>
        /// <param name="result">
        /// The result value to associate with the cancellation, if any.  This
        /// parameter may be null.
        /// </param>
        /// <param name="flags">
        /// The flags used to control how the script evaluation is canceled.
        /// </param>
        /// <param name="error">
        /// Upon failure, this may contain an appropriate error message.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise, a non-Ok value
        /// with details placed in the <paramref name="error" /> parameter.
        /// </returns>
        ReturnCode CancelTclEvaluate(
            string interpName,
            Result result,
            Tcl_EvalFlags flags,
            ref Result error
            );

        /// <summary>
        /// Requests cancellation of the script currently being evaluated in the
        /// specified master Tcl interpreter.
        /// </summary>
        /// <param name="interpName">
        /// The name of the master Tcl interpreter whose script evaluation is to
        /// be canceled, if any.  This parameter may be null to use the default
        /// master Tcl interpreter.
        /// </param>
        /// <param name="result">
        /// The result value to associate with the cancellation, if any.  This
        /// parameter may be null.
        /// </param>
        /// <param name="flags">
        /// The flags used to control how the script evaluation is canceled.
        /// </param>
        /// <param name="clientData">
        /// Upon return, this may contain the extra data that was associated with
        /// the canceled script evaluation, if any.
        /// </param>
        /// <param name="error">
        /// Upon failure, this may contain an appropriate error message.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise, a non-Ok value
        /// with details placed in the <paramref name="error" /> parameter.
        /// </returns>
        ReturnCode CancelTclEvaluate(
            string interpName,
            Result result,
            Tcl_EvalFlags flags,
            ref IClientData clientData,
            ref Result error
            );

        ///////////////////////////////////////////////////////////////////////
        // TCL EXPRESSION SUPPORT
        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Evaluates a Tcl expression using the specified master Tcl
        /// interpreter.
        /// </summary>
        /// <param name="interpName">
        /// The name of the master Tcl interpreter to use, if any.  This
        /// parameter may be null to use the default master Tcl interpreter.
        /// </param>
        /// <param name="text">
        /// The text of the Tcl expression to evaluate.
        /// </param>
        /// <param name="result">
        /// Upon success, this contains the value of the evaluated
        /// expression.  Upon failure, this contains an appropriate error
        /// message.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise, a non-Ok value
        /// with details placed in the <paramref name="result" /> parameter.
        /// </returns>
        ReturnCode EvaluateTclExpression(
            string interpName,
            string text,
            ref Result result
            );

        /// <summary>
        /// Evaluates a Tcl expression using the specified master Tcl
        /// interpreter.
        /// </summary>
        /// <param name="interpName">
        /// The name of the master Tcl interpreter to use, if any.  This
        /// parameter may be null to use the default master Tcl interpreter.
        /// </param>
        /// <param name="text">
        /// The text of the Tcl expression to evaluate.
        /// </param>
        /// <param name="exceptions">
        /// True if a Tcl return code other than Ok should be treated as an
        /// error and cause this method to fail; otherwise, false.
        /// </param>
        /// <param name="result">
        /// Upon success, this contains the value of the evaluated
        /// expression.  Upon failure, this contains an appropriate error
        /// message.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise, a non-Ok value
        /// with details placed in the <paramref name="result" /> parameter.
        /// </returns>
        ReturnCode EvaluateTclExpression(
            string interpName,
            string text,
            bool exceptions,
            ref Result result
            );

        /// <summary>
        /// Evaluates a Tcl expression using the specified master Tcl
        /// interpreter.
        /// </summary>
        /// <param name="interpName">
        /// The name of the master Tcl interpreter to use, if any.  This
        /// parameter may be null to use the default master Tcl interpreter.
        /// </param>
        /// <param name="text">
        /// The text of the Tcl expression to evaluate.
        /// </param>
        /// <param name="clientData">
        /// Upon return, this may contain the extra data associated with the
        /// evaluated expression, if any.
        /// </param>
        /// <param name="result">
        /// Upon success, this contains the value of the evaluated
        /// expression.  Upon failure, this contains an appropriate error
        /// message.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise, a non-Ok value
        /// with details placed in the <paramref name="result" /> parameter.
        /// </returns>
        ReturnCode EvaluateTclExpression(
            string interpName,
            string text,
            ref IClientData clientData,
            ref Result result
            );

        /// <summary>
        /// Evaluates a Tcl expression using the specified master Tcl
        /// interpreter.
        /// </summary>
        /// <param name="interpName">
        /// The name of the master Tcl interpreter to use, if any.  This
        /// parameter may be null to use the default master Tcl interpreter.
        /// </param>
        /// <param name="text">
        /// The text of the Tcl expression to evaluate.
        /// </param>
        /// <param name="exceptions">
        /// True if a Tcl return code other than Ok should be treated as an
        /// error and cause this method to fail; otherwise, false.
        /// </param>
        /// <param name="clientData">
        /// Upon return, this may contain the extra data associated with the
        /// evaluated expression, if any.
        /// </param>
        /// <param name="result">
        /// Upon success, this contains the value of the evaluated
        /// expression.  Upon failure, this contains an appropriate error
        /// message.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise, a non-Ok value
        /// with details placed in the <paramref name="result" /> parameter.
        /// </returns>
        ReturnCode EvaluateTclExpression(
            string interpName,
            string text,
            bool exceptions,
            ref IClientData clientData,
            ref Result result
            );

        ///////////////////////////////////////////////////////////////////////
        // TCL SCRIPT EVALUATION SUPPORT
        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Gets or sets a value indicating whether a Tcl return code other than
        /// Ok should be treated as an error by default for operations that do not
        /// explicitly specify this behavior.
        /// </summary>
        bool TclExceptions { get; set; }

        /// <summary>
        /// Evaluates a Tcl script using the specified master Tcl
        /// interpreter.
        /// </summary>
        /// <param name="interpName">
        /// The name of the master Tcl interpreter to use, if any.  This
        /// parameter may be null to use the default master Tcl interpreter.
        /// </param>
        /// <param name="text">
        /// The text of the Tcl script to evaluate.
        /// </param>
        /// <param name="result">
        /// Upon success, this contains the result of the evaluated script.
        /// Upon failure, this contains an appropriate error message.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise, a non-Ok value
        /// with details placed in the <paramref name="result" /> parameter.
        /// </returns>
        ReturnCode EvaluateTclScript(
            string interpName,
            string text,
            ref Result result
            );

        /// <summary>
        /// Evaluates a Tcl script using the specified master Tcl
        /// interpreter.
        /// </summary>
        /// <param name="interpName">
        /// The name of the master Tcl interpreter to use, if any.  This
        /// parameter may be null to use the default master Tcl interpreter.
        /// </param>
        /// <param name="text">
        /// The text of the Tcl script to evaluate.
        /// </param>
        /// <param name="exceptions">
        /// True if a Tcl return code other than Ok should be treated as an
        /// error and cause this method to fail; otherwise, false.
        /// </param>
        /// <param name="result">
        /// Upon success, this contains the result of the evaluated script.
        /// Upon failure, this contains an appropriate error message.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise, a non-Ok value
        /// with details placed in the <paramref name="result" /> parameter.
        /// </returns>
        ReturnCode EvaluateTclScript(
            string interpName,
            string text,
            bool exceptions,
            ref Result result
            );

        /// <summary>
        /// Evaluates a Tcl script using the specified master Tcl
        /// interpreter.
        /// </summary>
        /// <param name="interpName">
        /// The name of the master Tcl interpreter to use, if any.  This
        /// parameter may be null to use the default master Tcl interpreter.
        /// </param>
        /// <param name="text">
        /// The text of the Tcl script to evaluate.
        /// </param>
        /// <param name="clientData">
        /// Upon return, this may contain the extra data associated with
        /// the evaluated script, if any.
        /// </param>
        /// <param name="result">
        /// Upon success, this contains the result of the evaluated script.
        /// Upon failure, this contains an appropriate error message.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise, a non-Ok value
        /// with details placed in the <paramref name="result" /> parameter.
        /// </returns>
        ReturnCode EvaluateTclScript(
            string interpName,
            string text,
            ref IClientData clientData,
            ref Result result
            );

        /// <summary>
        /// Evaluates a Tcl script using the specified master Tcl
        /// interpreter.
        /// </summary>
        /// <param name="interpName">
        /// The name of the master Tcl interpreter to use, if any.  This
        /// parameter may be null to use the default master Tcl interpreter.
        /// </param>
        /// <param name="text">
        /// The text of the Tcl script to evaluate.
        /// </param>
        /// <param name="exceptions">
        /// True if a Tcl return code other than Ok should be treated as an
        /// error and cause this method to fail; otherwise, false.
        /// </param>
        /// <param name="clientData">
        /// Upon return, this may contain the extra data associated with
        /// the evaluated script, if any.
        /// </param>
        /// <param name="result">
        /// Upon success, this contains the result of the evaluated script.
        /// Upon failure, this contains an appropriate error message.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise, a non-Ok value
        /// with details placed in the <paramref name="result" /> parameter.
        /// </returns>
        ReturnCode EvaluateTclScript(
            string interpName,
            string text,
            bool exceptions,
            ref IClientData clientData,
            ref Result result
            );

        /// <summary>
        /// Evaluates a Tcl script using the specified master Tcl
        /// interpreter.
        /// </summary>
        /// <param name="interpName">
        /// The name of the master Tcl interpreter to use, if any.  This
        /// parameter may be null to use the default master Tcl interpreter.
        /// </param>
        /// <param name="text">
        /// The text of the Tcl script to evaluate.
        /// </param>
        /// <param name="flags">
        /// The flags used to control how the Tcl script is evaluated.
        /// </param>
        /// <param name="result">
        /// Upon success, this contains the result of the evaluated script.
        /// Upon failure, this contains an appropriate error message.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise, a non-Ok value
        /// with details placed in the <paramref name="result" /> parameter.
        /// </returns>
        ReturnCode EvaluateTclScript(
            string interpName,
            string text,
            Tcl_EvalFlags flags,
            ref Result result
            );

        /// <summary>
        /// Evaluates a Tcl script using the specified master Tcl
        /// interpreter.
        /// </summary>
        /// <param name="interpName">
        /// The name of the master Tcl interpreter to use, if any.  This
        /// parameter may be null to use the default master Tcl interpreter.
        /// </param>
        /// <param name="text">
        /// The text of the Tcl script to evaluate.
        /// </param>
        /// <param name="flags">
        /// The flags used to control how the Tcl script is evaluated.
        /// </param>
        /// <param name="exceptions">
        /// True if a Tcl return code other than Ok should be treated as an
        /// error and cause this method to fail; otherwise, false.
        /// </param>
        /// <param name="result">
        /// Upon success, this contains the result of the evaluated script.
        /// Upon failure, this contains an appropriate error message.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise, a non-Ok value
        /// with details placed in the <paramref name="result" /> parameter.
        /// </returns>
        ReturnCode EvaluateTclScript(
            string interpName,
            string text,
            Tcl_EvalFlags flags,
            bool exceptions,
            ref Result result
            );

        /// <summary>
        /// Evaluates a Tcl script using the specified master Tcl
        /// interpreter.
        /// </summary>
        /// <param name="interpName">
        /// The name of the master Tcl interpreter to use, if any.  This
        /// parameter may be null to use the default master Tcl interpreter.
        /// </param>
        /// <param name="text">
        /// The text of the Tcl script to evaluate.
        /// </param>
        /// <param name="flags">
        /// The flags used to control how the Tcl script is evaluated.
        /// </param>
        /// <param name="clientData">
        /// Upon return, this may contain the extra data associated with
        /// the evaluated script, if any.
        /// </param>
        /// <param name="result">
        /// Upon success, this contains the result of the evaluated script.
        /// Upon failure, this contains an appropriate error message.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise, a non-Ok value
        /// with details placed in the <paramref name="result" /> parameter.
        /// </returns>
        ReturnCode EvaluateTclScript(
            string interpName,
            string text,
            Tcl_EvalFlags flags,
            ref IClientData clientData,
            ref Result result
            );

        /// <summary>
        /// Evaluates a Tcl script using the specified master Tcl
        /// interpreter.
        /// </summary>
        /// <param name="interpName">
        /// The name of the master Tcl interpreter to use, if any.  This
        /// parameter may be null to use the default master Tcl interpreter.
        /// </param>
        /// <param name="text">
        /// The text of the Tcl script to evaluate.
        /// </param>
        /// <param name="flags">
        /// The flags used to control how the Tcl script is evaluated.
        /// </param>
        /// <param name="exceptions">
        /// True if a Tcl return code other than Ok should be treated as an
        /// error and cause this method to fail; otherwise, false.
        /// </param>
        /// <param name="clientData">
        /// Upon return, this may contain the extra data associated with
        /// the evaluated script, if any.
        /// </param>
        /// <param name="result">
        /// Upon success, this contains the result of the evaluated script.
        /// Upon failure, this contains an appropriate error message.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise, a non-Ok value
        /// with details placed in the <paramref name="result" /> parameter.
        /// </returns>
        ReturnCode EvaluateTclScript(
            string interpName,
            string text,
            Tcl_EvalFlags flags,
            bool exceptions,
            ref IClientData clientData,
            ref Result result
            );

        ///////////////////////////////////////////////////////////////////////
        // TCL FILE EVALUATION SUPPORT
        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Evaluates the contents of a file as a Tcl script using the
        /// specified master Tcl interpreter.
        /// </summary>
        /// <param name="interpName">
        /// The name of the master Tcl interpreter to use, if any.  This
        /// parameter may be null to use the default master Tcl interpreter.
        /// </param>
        /// <param name="fileName">
        /// The name of the file containing the Tcl script to evaluate.
        /// </param>
        /// <param name="result">
        /// Upon success, this contains the result of the evaluated script.
        /// Upon failure, this contains an appropriate error message.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise, a non-Ok value
        /// with details placed in the <paramref name="result" /> parameter.
        /// </returns>
        ReturnCode EvaluateTclFile(
            string interpName,
            string fileName,
            ref Result result
            );

        /// <summary>
        /// Evaluates the contents of a file as a Tcl script using the
        /// specified master Tcl interpreter.
        /// </summary>
        /// <param name="interpName">
        /// The name of the master Tcl interpreter to use, if any.  This
        /// parameter may be null to use the default master Tcl interpreter.
        /// </param>
        /// <param name="fileName">
        /// The name of the file containing the Tcl script to evaluate.
        /// </param>
        /// <param name="exceptions">
        /// True if a Tcl return code other than Ok should be treated as an
        /// error and cause this method to fail; otherwise, false.
        /// </param>
        /// <param name="result">
        /// Upon success, this contains the result of the evaluated script.
        /// Upon failure, this contains an appropriate error message.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise, a non-Ok value
        /// with details placed in the <paramref name="result" /> parameter.
        /// </returns>
        ReturnCode EvaluateTclFile(
            string interpName,
            string fileName,
            bool exceptions,
            ref Result result
            );

        /// <summary>
        /// Evaluates the contents of a file as a Tcl script using the
        /// specified master Tcl interpreter.
        /// </summary>
        /// <param name="interpName">
        /// The name of the master Tcl interpreter to use, if any.  This
        /// parameter may be null to use the default master Tcl interpreter.
        /// </param>
        /// <param name="fileName">
        /// The name of the file containing the Tcl script to evaluate.
        /// </param>
        /// <param name="clientData">
        /// Upon return, this may contain the extra data associated with
        /// the evaluated file, if any.
        /// </param>
        /// <param name="result">
        /// Upon success, this contains the result of the evaluated script.
        /// Upon failure, this contains an appropriate error message.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise, a non-Ok value
        /// with details placed in the <paramref name="result" /> parameter.
        /// </returns>
        ReturnCode EvaluateTclFile(
            string interpName,
            string fileName,
            ref IClientData clientData,
            ref Result result
            );

        /// <summary>
        /// Evaluates the contents of a file as a Tcl script using the
        /// specified master Tcl interpreter.
        /// </summary>
        /// <param name="interpName">
        /// The name of the master Tcl interpreter to use, if any.  This
        /// parameter may be null to use the default master Tcl interpreter.
        /// </param>
        /// <param name="fileName">
        /// The name of the file containing the Tcl script to evaluate.
        /// </param>
        /// <param name="exceptions">
        /// True if a Tcl return code other than Ok should be treated as an
        /// error and cause this method to fail; otherwise, false.
        /// </param>
        /// <param name="clientData">
        /// Upon return, this may contain the extra data associated with
        /// the evaluated file, if any.
        /// </param>
        /// <param name="result">
        /// Upon success, this contains the result of the evaluated script.
        /// Upon failure, this contains an appropriate error message.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise, a non-Ok value
        /// with details placed in the <paramref name="result" /> parameter.
        /// </returns>
        ReturnCode EvaluateTclFile(
            string interpName,
            string fileName,
            bool exceptions,
            ref IClientData clientData,
            ref Result result
            );

        ///////////////////////////////////////////////////////////////////////
        // TCL INTERACTIVE SCRIPT EVALUATION SUPPORT
        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Records the specified Tcl script in the history of the specified
        /// master Tcl interpreter and then evaluates it.
        /// </summary>
        /// <param name="interpName">
        /// The name of the master Tcl interpreter to use, if any.  This
        /// parameter may be null to use the default master Tcl interpreter.
        /// </param>
        /// <param name="text">
        /// The text of the Tcl script to evaluate.
        /// </param>
        /// <param name="result">
        /// Upon success, this contains the result of the evaluated script.
        /// Upon failure, this contains an appropriate error message.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise, a non-Ok value
        /// with details placed in the <paramref name="result" /> parameter.
        /// </returns>
        ReturnCode RecordAndEvaluateTclScript(
            string interpName,
            string text,
            ref Result result
            );

        /// <summary>
        /// Records the specified Tcl script in the history of the specified
        /// master Tcl interpreter and then evaluates it.
        /// </summary>
        /// <param name="interpName">
        /// The name of the master Tcl interpreter to use, if any.  This
        /// parameter may be null to use the default master Tcl interpreter.
        /// </param>
        /// <param name="text">
        /// The text of the Tcl script to evaluate.
        /// </param>
        /// <param name="exceptions">
        /// True if a Tcl return code other than Ok should be treated as an
        /// error and cause this method to fail; otherwise, false.
        /// </param>
        /// <param name="result">
        /// Upon success, this contains the result of the evaluated script.
        /// Upon failure, this contains an appropriate error message.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise, a non-Ok value
        /// with details placed in the <paramref name="result" /> parameter.
        /// </returns>
        ReturnCode RecordAndEvaluateTclScript(
            string interpName,
            string text,
            bool exceptions,
            ref Result result
            );

        /// <summary>
        /// Records the specified Tcl script in the history of the specified
        /// master Tcl interpreter and then evaluates it.
        /// </summary>
        /// <param name="interpName">
        /// The name of the master Tcl interpreter to use, if any.  This
        /// parameter may be null to use the default master Tcl interpreter.
        /// </param>
        /// <param name="text">
        /// The text of the Tcl script to evaluate.
        /// </param>
        /// <param name="clientData">
        /// Upon return, this may contain the extra data associated with
        /// the evaluated script, if any.
        /// </param>
        /// <param name="result">
        /// Upon success, this contains the result of the evaluated script.
        /// Upon failure, this contains an appropriate error message.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise, a non-Ok value
        /// with details placed in the <paramref name="result" /> parameter.
        /// </returns>
        ReturnCode RecordAndEvaluateTclScript(
            string interpName,
            string text,
            ref IClientData clientData,
            ref Result result
            );

        /// <summary>
        /// Records the specified Tcl script in the history of the specified
        /// master Tcl interpreter and then evaluates it.
        /// </summary>
        /// <param name="interpName">
        /// The name of the master Tcl interpreter to use, if any.  This
        /// parameter may be null to use the default master Tcl interpreter.
        /// </param>
        /// <param name="text">
        /// The text of the Tcl script to evaluate.
        /// </param>
        /// <param name="exceptions">
        /// True if a Tcl return code other than Ok should be treated as an
        /// error and cause this method to fail; otherwise, false.
        /// </param>
        /// <param name="clientData">
        /// Upon return, this may contain the extra data associated with
        /// the evaluated script, if any.
        /// </param>
        /// <param name="result">
        /// Upon success, this contains the result of the evaluated script.
        /// Upon failure, this contains an appropriate error message.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise, a non-Ok value
        /// with details placed in the <paramref name="result" /> parameter.
        /// </returns>
        ReturnCode RecordAndEvaluateTclScript(
            string interpName,
            string text,
            bool exceptions,
            ref IClientData clientData,
            ref Result result
            );

        /// <summary>
        /// Records the specified Tcl script in the history of the specified
        /// master Tcl interpreter and then evaluates it.
        /// </summary>
        /// <param name="interpName">
        /// The name of the master Tcl interpreter to use, if any.  This
        /// parameter may be null to use the default master Tcl interpreter.
        /// </param>
        /// <param name="text">
        /// The text of the Tcl script to evaluate.
        /// </param>
        /// <param name="flags">
        /// The flags used to control how the Tcl script is evaluated.
        /// </param>
        /// <param name="result">
        /// Upon success, this contains the result of the evaluated script.
        /// Upon failure, this contains an appropriate error message.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise, a non-Ok value
        /// with details placed in the <paramref name="result" /> parameter.
        /// </returns>
        ReturnCode RecordAndEvaluateTclScript(
            string interpName,
            string text,
            Tcl_EvalFlags flags,
            ref Result result
            );

        /// <summary>
        /// Records the specified Tcl script in the history of the specified
        /// master Tcl interpreter and then evaluates it.
        /// </summary>
        /// <param name="interpName">
        /// The name of the master Tcl interpreter to use, if any.  This
        /// parameter may be null to use the default master Tcl interpreter.
        /// </param>
        /// <param name="text">
        /// The text of the Tcl script to evaluate.
        /// </param>
        /// <param name="flags">
        /// The flags used to control how the Tcl script is evaluated.
        /// </param>
        /// <param name="exceptions">
        /// True if a Tcl return code other than Ok should be treated as an
        /// error and cause this method to fail; otherwise, false.
        /// </param>
        /// <param name="result">
        /// Upon success, this contains the result of the evaluated script.
        /// Upon failure, this contains an appropriate error message.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise, a non-Ok value
        /// with details placed in the <paramref name="result" /> parameter.
        /// </returns>
        ReturnCode RecordAndEvaluateTclScript(
            string interpName,
            string text,
            Tcl_EvalFlags flags,
            bool exceptions,
            ref Result result
            );

        /// <summary>
        /// Records the specified Tcl script in the history of the specified
        /// master Tcl interpreter and then evaluates it.
        /// </summary>
        /// <param name="interpName">
        /// The name of the master Tcl interpreter to use, if any.  This
        /// parameter may be null to use the default master Tcl interpreter.
        /// </param>
        /// <param name="text">
        /// The text of the Tcl script to evaluate.
        /// </param>
        /// <param name="flags">
        /// The flags used to control how the Tcl script is evaluated.
        /// </param>
        /// <param name="clientData">
        /// Upon return, this may contain the extra data associated with
        /// the evaluated script, if any.
        /// </param>
        /// <param name="result">
        /// Upon success, this contains the result of the evaluated script.
        /// Upon failure, this contains an appropriate error message.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise, a non-Ok value
        /// with details placed in the <paramref name="result" /> parameter.
        /// </returns>
        ReturnCode RecordAndEvaluateTclScript(
            string interpName,
            string text,
            Tcl_EvalFlags flags,
            ref IClientData clientData,
            ref Result result
            );

        /// <summary>
        /// Records the specified Tcl script in the history of the specified
        /// master Tcl interpreter and then evaluates it.
        /// </summary>
        /// <param name="interpName">
        /// The name of the master Tcl interpreter to use, if any.  This
        /// parameter may be null to use the default master Tcl interpreter.
        /// </param>
        /// <param name="text">
        /// The text of the Tcl script to evaluate.
        /// </param>
        /// <param name="flags">
        /// The flags used to control how the Tcl script is evaluated.
        /// </param>
        /// <param name="exceptions">
        /// True if a Tcl return code other than Ok should be treated as an
        /// error and cause this method to fail; otherwise, false.
        /// </param>
        /// <param name="clientData">
        /// Upon return, this may contain the extra data associated with
        /// the evaluated script, if any.
        /// </param>
        /// <param name="result">
        /// Upon success, this contains the result of the evaluated script.
        /// Upon failure, this contains an appropriate error message.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise, a non-Ok value
        /// with details placed in the <paramref name="result" /> parameter.
        /// </returns>
        ReturnCode RecordAndEvaluateTclScript(
            string interpName,
            string text,
            Tcl_EvalFlags flags,
            bool exceptions,
            ref IClientData clientData,
            ref Result result
            );

        ///////////////////////////////////////////////////////////////////////
        // TCL STRING SUBSTITUTION SUPPORT
        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Performs Tcl substitution on the specified string using the
        /// specified master Tcl interpreter.
        /// </summary>
        /// <param name="interpName">
        /// The name of the master Tcl interpreter to use, if any.  This
        /// parameter may be null to use the default master Tcl interpreter.
        /// </param>
        /// <param name="text">
        /// The text of the string to perform Tcl substitution on.
        /// </param>
        /// <param name="result">
        /// Upon success, this contains the result of the Tcl substitution.
        /// Upon failure, this contains an appropriate error message.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise, a non-Ok value
        /// with details placed in the <paramref name="result" /> parameter.
        /// </returns>
        ReturnCode SubstituteTclString(
            string interpName,
            string text,
            ref Result result
            );

        /// <summary>
        /// Performs Tcl substitution on the specified string using the
        /// specified master Tcl interpreter.
        /// </summary>
        /// <param name="interpName">
        /// The name of the master Tcl interpreter to use, if any.  This
        /// parameter may be null to use the default master Tcl interpreter.
        /// </param>
        /// <param name="text">
        /// The text of the string to perform Tcl substitution on.
        /// </param>
        /// <param name="exceptions">
        /// True if a Tcl return code other than Ok should be treated as an
        /// error and cause this method to fail; otherwise, false.
        /// </param>
        /// <param name="result">
        /// Upon success, this contains the result of the Tcl substitution.
        /// Upon failure, this contains an appropriate error message.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise, a non-Ok value
        /// with details placed in the <paramref name="result" /> parameter.
        /// </returns>
        ReturnCode SubstituteTclString(
            string interpName,
            string text,
            bool exceptions,
            ref Result result
            );

        /// <summary>
        /// Performs Tcl substitution on the specified string using the
        /// specified master Tcl interpreter.
        /// </summary>
        /// <param name="interpName">
        /// The name of the master Tcl interpreter to use, if any.  This
        /// parameter may be null to use the default master Tcl interpreter.
        /// </param>
        /// <param name="text">
        /// The text of the string to perform Tcl substitution on.
        /// </param>
        /// <param name="clientData">
        /// Upon return, this may contain the extra data associated with
        /// the Tcl substitution, if any.
        /// </param>
        /// <param name="result">
        /// Upon success, this contains the result of the Tcl substitution.
        /// Upon failure, this contains an appropriate error message.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise, a non-Ok value
        /// with details placed in the <paramref name="result" /> parameter.
        /// </returns>
        ReturnCode SubstituteTclString(
            string interpName,
            string text,
            ref IClientData clientData,
            ref Result result
            );

        /// <summary>
        /// Performs Tcl substitution on the specified string using the
        /// specified master Tcl interpreter.
        /// </summary>
        /// <param name="interpName">
        /// The name of the master Tcl interpreter to use, if any.  This
        /// parameter may be null to use the default master Tcl interpreter.
        /// </param>
        /// <param name="text">
        /// The text of the string to perform Tcl substitution on.
        /// </param>
        /// <param name="exceptions">
        /// True if a Tcl return code other than Ok should be treated as an
        /// error and cause this method to fail; otherwise, false.
        /// </param>
        /// <param name="clientData">
        /// Upon return, this may contain the extra data associated with
        /// the Tcl substitution, if any.
        /// </param>
        /// <param name="result">
        /// Upon success, this contains the result of the Tcl substitution.
        /// Upon failure, this contains an appropriate error message.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise, a non-Ok value
        /// with details placed in the <paramref name="result" /> parameter.
        /// </returns>
        ReturnCode SubstituteTclString(
            string interpName,
            string text,
            bool exceptions,
            ref IClientData clientData,
            ref Result result
            );

        /// <summary>
        /// Performs Tcl substitution on the specified string using the
        /// specified master Tcl interpreter.
        /// </summary>
        /// <param name="interpName">
        /// The name of the master Tcl interpreter to use, if any.  This
        /// parameter may be null to use the default master Tcl interpreter.
        /// </param>
        /// <param name="text">
        /// The text of the string to perform Tcl substitution on.
        /// </param>
        /// <param name="flags">
        /// The flags used to control how the Tcl substitution is performed.
        /// </param>
        /// <param name="result">
        /// Upon success, this contains the result of the Tcl substitution.
        /// Upon failure, this contains an appropriate error message.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise, a non-Ok value
        /// with details placed in the <paramref name="result" /> parameter.
        /// </returns>
        ReturnCode SubstituteTclString(
            string interpName,
            string text,
            Tcl_SubstFlags flags,
            ref Result result
            );

        /// <summary>
        /// Performs Tcl substitution on the specified string using the
        /// specified master Tcl interpreter.
        /// </summary>
        /// <param name="interpName">
        /// The name of the master Tcl interpreter to use, if any.  This
        /// parameter may be null to use the default master Tcl interpreter.
        /// </param>
        /// <param name="text">
        /// The text of the string to perform Tcl substitution on.
        /// </param>
        /// <param name="flags">
        /// The flags used to control how the Tcl substitution is performed.
        /// </param>
        /// <param name="exceptions">
        /// True if a Tcl return code other than Ok should be treated as an
        /// error and cause this method to fail; otherwise, false.
        /// </param>
        /// <param name="result">
        /// Upon success, this contains the result of the Tcl substitution.
        /// Upon failure, this contains an appropriate error message.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise, a non-Ok value
        /// with details placed in the <paramref name="result" /> parameter.
        /// </returns>
        ReturnCode SubstituteTclString(
            string interpName,
            string text,
            Tcl_SubstFlags flags,
            bool exceptions,
            ref Result result
            );

        /// <summary>
        /// Performs Tcl substitution on the specified string using the
        /// specified master Tcl interpreter.
        /// </summary>
        /// <param name="interpName">
        /// The name of the master Tcl interpreter to use, if any.  This
        /// parameter may be null to use the default master Tcl interpreter.
        /// </param>
        /// <param name="text">
        /// The text of the string to perform Tcl substitution on.
        /// </param>
        /// <param name="flags">
        /// The flags used to control how the Tcl substitution is performed.
        /// </param>
        /// <param name="clientData">
        /// Upon return, this may contain the extra data associated with
        /// the Tcl substitution, if any.
        /// </param>
        /// <param name="result">
        /// Upon success, this contains the result of the Tcl substitution.
        /// Upon failure, this contains an appropriate error message.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise, a non-Ok value
        /// with details placed in the <paramref name="result" /> parameter.
        /// </returns>
        ReturnCode SubstituteTclString(
            string interpName,
            string text,
            Tcl_SubstFlags flags,
            ref IClientData clientData,
            ref Result result
            );

        /// <summary>
        /// Performs Tcl substitution on the specified string using the
        /// specified master Tcl interpreter.
        /// </summary>
        /// <param name="interpName">
        /// The name of the master Tcl interpreter to use, if any.  This
        /// parameter may be null to use the default master Tcl interpreter.
        /// </param>
        /// <param name="text">
        /// The text of the string to perform Tcl substitution on.
        /// </param>
        /// <param name="flags">
        /// The flags used to control how the Tcl substitution is performed.
        /// </param>
        /// <param name="exceptions">
        /// True if a Tcl return code other than Ok should be treated as an
        /// error and cause this method to fail; otherwise, false.
        /// </param>
        /// <param name="clientData">
        /// Upon return, this may contain the extra data associated with
        /// the Tcl substitution, if any.
        /// </param>
        /// <param name="result">
        /// Upon success, this contains the result of the Tcl substitution.
        /// Upon failure, this contains an appropriate error message.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise, a non-Ok value
        /// with details placed in the <paramref name="result" /> parameter.
        /// </returns>
        ReturnCode SubstituteTclString(
            string interpName,
            string text,
            Tcl_SubstFlags flags,
            bool exceptions,
            ref IClientData clientData,
            ref Result result
            );

        ///////////////////////////////////////////////////////////////////////
        // TCL VARIABLE SUPPORT
        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Gets the value of a Tcl variable in the specified master Tcl
        /// interpreter.
        /// </summary>
        /// <param name="interpName">
        /// The name of the master Tcl interpreter to use, if any.  This
        /// parameter may be null to use the default master Tcl interpreter.
        /// </param>
        /// <param name="flags">
        /// The flags used to control how the Tcl variable is accessed.
        /// </param>
        /// <param name="name">
        /// The name of the Tcl variable to get the value of.
        /// </param>
        /// <param name="value">
        /// Upon success, this is set to the value of the specified Tcl
        /// variable.
        /// </param>
        /// <param name="error">
        /// Upon failure, this may contain an appropriate error message.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise, a non-Ok value
        /// with details placed in the <paramref name="error" /> parameter.
        /// </returns>
        ReturnCode GetTclVariableValue(
            string interpName,
            Tcl_VarFlags flags,
            string name,
            ref Result value,
            ref Result error
            );

        /// <summary>
        /// Sets the value of a Tcl variable in the specified master Tcl
        /// interpreter.
        /// </summary>
        /// <param name="interpName">
        /// The name of the master Tcl interpreter to use, if any.  This
        /// parameter may be null to use the default master Tcl interpreter.
        /// </param>
        /// <param name="flags">
        /// The flags used to control how the Tcl variable is accessed.
        /// </param>
        /// <param name="name">
        /// The name of the Tcl variable to set the value of.
        /// </param>
        /// <param name="value">
        /// The value to set the specified Tcl variable to.
        /// </param>
        /// <param name="error">
        /// Upon failure, this may contain an appropriate error message.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise, a non-Ok value
        /// with details placed in the <paramref name="error" /> parameter.
        /// </returns>
        ReturnCode SetTclVariableValue(
            string interpName,
            Tcl_VarFlags flags,
            string name,
            ref Result value,
            ref Result error
            );

        /// <summary>
        /// Unsets a Tcl variable in the specified master Tcl interpreter.
        /// </summary>
        /// <param name="interpName">
        /// The name of the master Tcl interpreter to use, if any.  This
        /// parameter may be null to use the default master Tcl interpreter.
        /// </param>
        /// <param name="flags">
        /// The flags used to control how the Tcl variable is accessed.
        /// </param>
        /// <param name="name">
        /// The name of the Tcl variable to unset.
        /// </param>
        /// <param name="error">
        /// Upon failure, this may contain an appropriate error message.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise, a non-Ok value
        /// with details placed in the <paramref name="error" /> parameter.
        /// </returns>
        ReturnCode UnsetTclVariableValue(
            string interpName,
            Tcl_VarFlags flags,
            string name,
            ref Result error
            );
    }
}
