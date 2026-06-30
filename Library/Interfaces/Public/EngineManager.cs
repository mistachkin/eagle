/*
 * EngineManager.cs --
 *
 * Copyright (c) 2007-2012 by Joe Mistachkin.  All rights reserved.
 *
 * See the file "license.terms" for information on usage and redistribution of
 * this file, and for a DISCLAIMER OF ALL WARRANTIES.
 *
 * RCS: @(#) $Id: $
 */

using System.IO;
using System.Text;
using Eagle._Attributes;
using Eagle._Components.Public;
using Eagle._Components.Public.Delegates;
using Eagle._Containers.Public;

namespace Eagle._Interfaces.Public
{
    /// <summary>
    /// This interface defines the script engine management contract for an
    /// Eagle interpreter. It provides the methods used to evaluate scripts,
    /// files, streams, and expressions, to perform substitution processing,
    /// to invoke commands, and to query or control script cancellation. Both
    /// synchronous and asynchronous variants are provided, along with
    /// trusted-evaluation overloads. See <c>core_language.md</c> for the
    /// script evaluation model.
    /// </summary>
    [ObjectId("6d41d61d-1173-4034-b5c3-3e929e5c7f24")]
    public interface IEngineManager
    {
        ///////////////////////////////////////////////////////////////////////
        // SCRIPT CANCELLATION
        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Checks whether script evaluation has been canceled for the current
        /// thread.
        /// </summary>
        /// <param name="cancelFlags">
        /// The <see cref="CancelFlags" /> used to control the cancellation
        /// behavior.
        /// </param>
        /// <param name="result">
        /// Upon return, this indicates whether cancellation has been requested,
        /// or contains an appropriate error message upon failure.
        /// </param>
        /// <returns>
        /// Returns <see cref="ReturnCode.Ok" /> on success; otherwise, a non-Ok
        /// value (such as <see cref="ReturnCode.Error" />) with details placed in
        /// the <paramref name="result" /> parameter.
        /// </returns>
        ReturnCode IsCanceled(
            CancelFlags cancelFlags,
            ref Result result
            );

#if THREADING
        /// <summary>
        /// Checks whether script evaluation has been canceled for the specified
        /// engine context.
        /// </summary>
        /// <param name="engineContext">
        /// The opaque engine context to operate on, if any. This parameter may be
        /// null.
        /// </param>
        /// <param name="cancelFlags">
        /// The <see cref="CancelFlags" /> used to control the cancellation
        /// behavior.
        /// </param>
        /// <param name="result">
        /// Upon return, this indicates whether cancellation has been requested,
        /// or contains an appropriate error message upon failure.
        /// </param>
        /// <returns>
        /// Returns <see cref="ReturnCode.Ok" /> on success; otherwise, a non-Ok
        /// value (such as <see cref="ReturnCode.Error" />) with details placed in
        /// the <paramref name="result" /> parameter.
        /// </returns>
        ReturnCode IsCanceled(
            object engineContext,
            CancelFlags cancelFlags,
            ref Result result
            );
#endif

        /// <summary>
        /// Requests that the script currently being evaluated on the current
        /// thread be canceled.
        /// </summary>
        /// <param name="result">
        /// The result value to associate with the canceled evaluation, if any.
        /// This parameter may be null.
        /// </param>
        /// <param name="cancelFlags">
        /// The <see cref="CancelFlags" /> used to control the cancellation
        /// behavior.
        /// </param>
        /// <param name="error">
        /// Upon failure, this must contain an appropriate error message.
        /// </param>
        /// <returns>
        /// Returns <see cref="ReturnCode.Ok" /> on success; otherwise, a non-Ok
        /// value (such as <see cref="ReturnCode.Error" />) with details placed in
        /// the <paramref name="error" /> parameter.
        /// </returns>
        ReturnCode CancelEvaluate(
            Result result,
            CancelFlags cancelFlags,
            ref Result error
            );

        /// <summary>
        /// Requests that any script currently being evaluated be canceled.
        /// </summary>
        /// <param name="result">
        /// The result value to associate with the canceled evaluation, if any.
        /// This parameter may be null.
        /// </param>
        /// <param name="cancelFlags">
        /// The <see cref="CancelFlags" /> used to control the cancellation
        /// behavior.
        /// </param>
        /// <param name="error">
        /// Upon failure, this must contain an appropriate error message.
        /// </param>
        /// <returns>
        /// Returns <see cref="ReturnCode.Ok" /> on success; otherwise, a non-Ok
        /// value (such as <see cref="ReturnCode.Error" />) with details placed in
        /// the <paramref name="error" /> parameter.
        /// </returns>
        ReturnCode CancelAnyEvaluate(
            Result result,
            CancelFlags cancelFlags,
            ref Result error
            );

#if THREADING
        /// <summary>
        /// Requests that any script currently being evaluated within the
        /// specified engine context be canceled.
        /// </summary>
        /// <param name="engineContext">
        /// The opaque engine context to operate on, if any. This parameter may be
        /// null.
        /// </param>
        /// <param name="result">
        /// The result value to associate with the canceled evaluation, if any.
        /// This parameter may be null.
        /// </param>
        /// <param name="cancelFlags">
        /// The <see cref="CancelFlags" /> used to control the cancellation
        /// behavior.
        /// </param>
        /// <param name="error">
        /// Upon failure, this must contain an appropriate error message.
        /// </param>
        /// <returns>
        /// Returns <see cref="ReturnCode.Ok" /> on success; otherwise, a non-Ok
        /// value (such as <see cref="ReturnCode.Error" />) with details placed in
        /// the <paramref name="error" /> parameter.
        /// </returns>
        ReturnCode CancelAnyEvaluate(
            object engineContext,
            Result result,
            CancelFlags cancelFlags,
            ref Result error
            );
#endif

        /// <summary>
        /// Resets the script cancellation state for the current thread.
        /// </summary>
        /// <param name="cancelFlags">
        /// The <see cref="CancelFlags" /> used to control the cancellation
        /// behavior.
        /// </param>
        /// <param name="error">
        /// Upon failure, this must contain an appropriate error message.
        /// </param>
        /// <returns>
        /// Returns <see cref="ReturnCode.Ok" /> on success; otherwise, a non-Ok
        /// value (such as <see cref="ReturnCode.Error" />) with details placed in
        /// the <paramref name="error" /> parameter.
        /// </returns>
        ReturnCode ResetCancel(
            CancelFlags cancelFlags,
            ref Result error
            );

#if THREADING
        /// <summary>
        /// Resets the script cancellation state for the specified engine context.
        /// </summary>
        /// <param name="engineContext">
        /// The opaque engine context to operate on, if any. This parameter may be
        /// null.
        /// </param>
        /// <param name="cancelFlags">
        /// The <see cref="CancelFlags" /> used to control the cancellation
        /// behavior.
        /// </param>
        /// <param name="error">
        /// Upon failure, this must contain an appropriate error message.
        /// </param>
        /// <returns>
        /// Returns <see cref="ReturnCode.Ok" /> on success; otherwise, a non-Ok
        /// value (such as <see cref="ReturnCode.Error" />) with details placed in
        /// the <paramref name="error" /> parameter.
        /// </returns>
        ReturnCode ResetCancel(
            object engineContext,
            CancelFlags cancelFlags,
            ref Result error
            );
#endif

        ///////////////////////////////////////////////////////////////////////
        // SCRIPT EVALUATION
        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Evaluates a script contained in the specified string.
        /// </summary>
        /// <param name="text">
        /// The script text to be evaluated. This parameter should not be null.
        /// </param>
        /// <param name="result">
        /// Upon success, this may contain the result value produced by the
        /// operation; upon failure, it must contain an appropriate error message.
        /// </param>
        /// <returns>
        /// Returns <see cref="ReturnCode.Ok" /> on success; otherwise, a non-Ok
        /// value (such as <see cref="ReturnCode.Error" />) with details placed in
        /// the <paramref name="result" /> parameter.
        /// </returns>
        ReturnCode EvaluateScript(
            string text,
            ref Result result
            );

        /// <summary>
        /// Evaluates a script contained in the specified string, reporting the
        /// line number of any error.
        /// </summary>
        /// <param name="text">
        /// The script text to be evaluated. This parameter should not be null.
        /// </param>
        /// <param name="result">
        /// Upon success, this may contain the result value produced by the
        /// operation; upon failure, it must contain an appropriate error message.
        /// </param>
        /// <param name="errorLine">
        /// Upon return, this may contain the one-based line number associated
        /// with any error that was encountered.
        /// </param>
        /// <returns>
        /// Returns <see cref="ReturnCode.Ok" /> on success; otherwise, a non-Ok
        /// value (such as <see cref="ReturnCode.Error" />) with details placed in
        /// the <paramref name="result" /> parameter.
        /// </returns>
        ReturnCode EvaluateScript(
            string text,
            ref Result result,
            ref int errorLine
            );

        /// <summary>
        /// Evaluates a script contained in the specified string, using the
        /// specified engine flags.
        /// </summary>
        /// <param name="text">
        /// The script text to be evaluated. This parameter should not be null.
        /// </param>
        /// <param name="engineFlags">
        /// The <see cref="EngineFlags" /> used to control how evaluation is
        /// performed.
        /// </param>
        /// <param name="result">
        /// Upon success, this may contain the result value produced by the
        /// operation; upon failure, it must contain an appropriate error message.
        /// </param>
        /// <param name="errorLine">
        /// Upon return, this may contain the one-based line number associated
        /// with any error that was encountered.
        /// </param>
        /// <returns>
        /// Returns <see cref="ReturnCode.Ok" /> on success; otherwise, a non-Ok
        /// value (such as <see cref="ReturnCode.Error" />) with details placed in
        /// the <paramref name="result" /> parameter.
        /// </returns>
        ReturnCode EvaluateScript(
            string text,
            EngineFlags engineFlags,
            ref Result result,
            ref int errorLine
            );

        /// <summary>
        /// Evaluates a script contained in the specified string, associating it
        /// with the specified file name for error reporting.
        /// </summary>
        /// <param name="fileName">
        /// The name of the file to be processed. This parameter should not be
        /// null.
        /// </param>
        /// <param name="text">
        /// The script text to be evaluated. This parameter should not be null.
        /// </param>
        /// <param name="result">
        /// Upon success, this may contain the result value produced by the
        /// operation; upon failure, it must contain an appropriate error message.
        /// </param>
        /// <returns>
        /// Returns <see cref="ReturnCode.Ok" /> on success; otherwise, a non-Ok
        /// value (such as <see cref="ReturnCode.Error" />) with details placed in
        /// the <paramref name="result" /> parameter.
        /// </returns>
        ReturnCode EvaluateScript(
            string fileName,
            string text,
            ref Result result
            );

        /// <summary>
        /// Evaluates a script contained in the specified string, associating it
        /// with the specified file name for error reporting, and reporting the
        /// line number of any error.
        /// </summary>
        /// <param name="fileName">
        /// The name of the file to be processed. This parameter should not be
        /// null.
        /// </param>
        /// <param name="text">
        /// The script text to be evaluated. This parameter should not be null.
        /// </param>
        /// <param name="result">
        /// Upon success, this may contain the result value produced by the
        /// operation; upon failure, it must contain an appropriate error message.
        /// </param>
        /// <param name="errorLine">
        /// Upon return, this may contain the one-based line number associated
        /// with any error that was encountered.
        /// </param>
        /// <returns>
        /// Returns <see cref="ReturnCode.Ok" /> on success; otherwise, a non-Ok
        /// value (such as <see cref="ReturnCode.Error" />) with details placed in
        /// the <paramref name="result" /> parameter.
        /// </returns>
        ReturnCode EvaluateScript(
            string fileName,
            string text,
            ref Result result,
            ref int errorLine
            );

        /// <summary>
        /// Evaluates a script contained in the specified string within the global
        /// scope.
        /// </summary>
        /// <param name="text">
        /// The script text to be evaluated. This parameter should not be null.
        /// </param>
        /// <param name="result">
        /// Upon success, this may contain the result value produced by the
        /// operation; upon failure, it must contain an appropriate error message.
        /// </param>
        /// <returns>
        /// Returns <see cref="ReturnCode.Ok" /> on success; otherwise, a non-Ok
        /// value (such as <see cref="ReturnCode.Error" />) with details placed in
        /// the <paramref name="result" /> parameter.
        /// </returns>
        ReturnCode EvaluateGlobalScript(
            string text,
            ref Result result
            );

        /// <summary>
        /// Evaluates a script contained in the specified string within the global
        /// scope, reporting the line number of any error.
        /// </summary>
        /// <param name="text">
        /// The script text to be evaluated. This parameter should not be null.
        /// </param>
        /// <param name="result">
        /// Upon success, this may contain the result value produced by the
        /// operation; upon failure, it must contain an appropriate error message.
        /// </param>
        /// <param name="errorLine">
        /// Upon return, this may contain the one-based line number associated
        /// with any error that was encountered.
        /// </param>
        /// <returns>
        /// Returns <see cref="ReturnCode.Ok" /> on success; otherwise, a non-Ok
        /// value (such as <see cref="ReturnCode.Error" />) with details placed in
        /// the <paramref name="result" /> parameter.
        /// </returns>
        ReturnCode EvaluateGlobalScript(
            string text,
            ref Result result,
            ref int errorLine
            );

        /// <summary>
        /// Evaluates the specified script object.
        /// </summary>
        /// <param name="script">
        /// The <see cref="IScript" /> to be evaluated. This parameter should not
        /// be null.
        /// </param>
        /// <param name="result">
        /// Upon success, this may contain the result value produced by the
        /// operation; upon failure, it must contain an appropriate error message.
        /// </param>
        /// <returns>
        /// Returns <see cref="ReturnCode.Ok" /> on success; otherwise, a non-Ok
        /// value (such as <see cref="ReturnCode.Error" />) with details placed in
        /// the <paramref name="result" /> parameter.
        /// </returns>
        ReturnCode EvaluateScript(
            IScript script,
            ref Result result
            );

        /// <summary>
        /// Evaluates the specified script object, reporting the line number of
        /// any error.
        /// </summary>
        /// <param name="script">
        /// The <see cref="IScript" /> to be evaluated. This parameter should not
        /// be null.
        /// </param>
        /// <param name="result">
        /// Upon success, this may contain the result value produced by the
        /// operation; upon failure, it must contain an appropriate error message.
        /// </param>
        /// <param name="errorLine">
        /// Upon return, this may contain the one-based line number associated
        /// with any error that was encountered.
        /// </param>
        /// <returns>
        /// Returns <see cref="ReturnCode.Ok" /> on success; otherwise, a non-Ok
        /// value (such as <see cref="ReturnCode.Error" />) with details placed in
        /// the <paramref name="result" /> parameter.
        /// </returns>
        ReturnCode EvaluateScript(
            IScript script,
            ref Result result,
            ref int errorLine
            );

        /// <summary>
        /// Evaluates a script contained in the specified string using the
        /// specified scope call frame.
        /// </summary>
        /// <param name="text">
        /// The script text to be evaluated. This parameter should not be null.
        /// </param>
        /// <param name="frame">
        /// Upon entry, the scope call frame to use, if any; upon return, the call
        /// frame that was actually used. This parameter may be null.
        /// </param>
        /// <param name="result">
        /// Upon success, this may contain the result value produced by the
        /// operation; upon failure, it must contain an appropriate error message.
        /// </param>
        /// <param name="errorLine">
        /// Upon return, this may contain the one-based line number associated
        /// with any error that was encountered.
        /// </param>
        /// <returns>
        /// Returns <see cref="ReturnCode.Ok" /> on success; otherwise, a non-Ok
        /// value (such as <see cref="ReturnCode.Error" />) with details placed in
        /// the <paramref name="result" /> parameter.
        /// </returns>
        ReturnCode EvaluateScriptWithScopeFrame(
            string text,
            ref ICallFrame frame,
            ref Result result,
            ref int errorLine
            );

        /// <summary>
        /// Evaluates the script contained in the specified file.
        /// </summary>
        /// <param name="fileName">
        /// The name of the file to be processed. This parameter should not be
        /// null.
        /// </param>
        /// <param name="result">
        /// Upon success, this may contain the result value produced by the
        /// operation; upon failure, it must contain an appropriate error message.
        /// </param>
        /// <returns>
        /// Returns <see cref="ReturnCode.Ok" /> on success; otherwise, a non-Ok
        /// value (such as <see cref="ReturnCode.Error" />) with details placed in
        /// the <paramref name="result" /> parameter.
        /// </returns>
        ReturnCode EvaluateFile(
            string fileName,
            ref Result result
            );

        /// <summary>
        /// Evaluates the script contained in the specified file, reporting the
        /// line number of any error.
        /// </summary>
        /// <param name="fileName">
        /// The name of the file to be processed. This parameter should not be
        /// null.
        /// </param>
        /// <param name="result">
        /// Upon success, this may contain the result value produced by the
        /// operation; upon failure, it must contain an appropriate error message.
        /// </param>
        /// <param name="errorLine">
        /// Upon return, this may contain the one-based line number associated
        /// with any error that was encountered.
        /// </param>
        /// <returns>
        /// Returns <see cref="ReturnCode.Ok" /> on success; otherwise, a non-Ok
        /// value (such as <see cref="ReturnCode.Error" />) with details placed in
        /// the <paramref name="result" /> parameter.
        /// </returns>
        ReturnCode EvaluateFile(
            string fileName,
            ref Result result,
            ref int errorLine
            );

        /// <summary>
        /// Evaluates the script contained in the specified file, using the
        /// specified character encoding.
        /// </summary>
        /// <param name="encoding">
        /// The <see cref="Encoding" /> to use when reading the file. This
        /// parameter may be null.
        /// </param>
        /// <param name="fileName">
        /// The name of the file to be processed. This parameter should not be
        /// null.
        /// </param>
        /// <param name="result">
        /// Upon success, this may contain the result value produced by the
        /// operation; upon failure, it must contain an appropriate error message.
        /// </param>
        /// <returns>
        /// Returns <see cref="ReturnCode.Ok" /> on success; otherwise, a non-Ok
        /// value (such as <see cref="ReturnCode.Error" />) with details placed in
        /// the <paramref name="result" /> parameter.
        /// </returns>
        ReturnCode EvaluateFile(
            Encoding encoding,
            string fileName,
            ref Result result
            );

        /// <summary>
        /// Evaluates the script contained in the specified file, using the
        /// specified character encoding, and reporting the line number of any
        /// error.
        /// </summary>
        /// <param name="encoding">
        /// The <see cref="Encoding" /> to use when reading the file. This
        /// parameter may be null.
        /// </param>
        /// <param name="fileName">
        /// The name of the file to be processed. This parameter should not be
        /// null.
        /// </param>
        /// <param name="result">
        /// Upon success, this may contain the result value produced by the
        /// operation; upon failure, it must contain an appropriate error message.
        /// </param>
        /// <param name="errorLine">
        /// Upon return, this may contain the one-based line number associated
        /// with any error that was encountered.
        /// </param>
        /// <returns>
        /// Returns <see cref="ReturnCode.Ok" /> on success; otherwise, a non-Ok
        /// value (such as <see cref="ReturnCode.Error" />) with details placed in
        /// the <paramref name="result" /> parameter.
        /// </returns>
        ReturnCode EvaluateFile(
            Encoding encoding,
            string fileName,
            ref Result result,
            ref int errorLine
            );

        /// <summary>
        /// Evaluates the script contained in the specified file, using the
        /// specified character encoding and engine flags.
        /// </summary>
        /// <param name="encoding">
        /// The <see cref="Encoding" /> to use when reading the file. This
        /// parameter may be null.
        /// </param>
        /// <param name="fileName">
        /// The name of the file to be processed. This parameter should not be
        /// null.
        /// </param>
        /// <param name="engineFlags">
        /// The <see cref="EngineFlags" /> used to control how evaluation is
        /// performed.
        /// </param>
        /// <param name="result">
        /// Upon success, this may contain the result value produced by the
        /// operation; upon failure, it must contain an appropriate error message.
        /// </param>
        /// <param name="errorLine">
        /// Upon return, this may contain the one-based line number associated
        /// with any error that was encountered.
        /// </param>
        /// <returns>
        /// Returns <see cref="ReturnCode.Ok" /> on success; otherwise, a non-Ok
        /// value (such as <see cref="ReturnCode.Error" />) with details placed in
        /// the <paramref name="result" /> parameter.
        /// </returns>
        ReturnCode EvaluateFile(
            Encoding encoding,
            string fileName,
            EngineFlags engineFlags,
            ref Result result,
            ref int errorLine
            );

#if DATA
        /// <summary>
        /// Evaluates the script bundle contained in the specified file.
        /// </summary>
        /// <param name="fileName">
        /// The name of the file to be processed. This parameter should not be
        /// null.
        /// </param>
        /// <param name="password">
        /// The password used to decrypt the bundle file, if any. This parameter
        /// may be null.
        /// </param>
        /// <param name="bundleFlags">
        /// The <see cref="BundleFlags" /> used to control how the bundle file is
        /// processed.
        /// </param>
        /// <param name="clientData">
        /// Upon return, this may contain extra data associated with the evaluated
        /// bundle. This parameter may be null.
        /// </param>
        /// <param name="result">
        /// Upon success, this may contain the result value produced by the
        /// operation; upon failure, it must contain an appropriate error message.
        /// </param>
        /// <returns>
        /// Returns <see cref="ReturnCode.Ok" /> on success; otherwise, a non-Ok
        /// value (such as <see cref="ReturnCode.Error" />) with details placed in
        /// the <paramref name="result" /> parameter.
        /// </returns>
        ReturnCode EvaluateBundleFile(
            string fileName,
            byte[] password,
            BundleFlags bundleFlags,
            ref IClientData clientData,
            ref Result result
            );

        /// <summary>
        /// Evaluates the script bundle contained in the specified file, reporting
        /// the line number of any error.
        /// </summary>
        /// <param name="fileName">
        /// The name of the file to be processed. This parameter should not be
        /// null.
        /// </param>
        /// <param name="password">
        /// The password used to decrypt the bundle file, if any. This parameter
        /// may be null.
        /// </param>
        /// <param name="bundleFlags">
        /// The <see cref="BundleFlags" /> used to control how the bundle file is
        /// processed.
        /// </param>
        /// <param name="clientData">
        /// Upon return, this may contain extra data associated with the evaluated
        /// bundle. This parameter may be null.
        /// </param>
        /// <param name="result">
        /// Upon success, this may contain the result value produced by the
        /// operation; upon failure, it must contain an appropriate error message.
        /// </param>
        /// <param name="errorLine">
        /// Upon return, this may contain the one-based line number associated
        /// with any error that was encountered.
        /// </param>
        /// <returns>
        /// Returns <see cref="ReturnCode.Ok" /> on success; otherwise, a non-Ok
        /// value (such as <see cref="ReturnCode.Error" />) with details placed in
        /// the <paramref name="result" /> parameter.
        /// </returns>
        ReturnCode EvaluateBundleFile(
            string fileName,
            byte[] password,
            BundleFlags bundleFlags,
            ref IClientData clientData,
            ref Result result,
            ref int errorLine
            );

        /// <summary>
        /// Evaluates the script bundle contained in the specified file, using the
        /// specified script flags.
        /// </summary>
        /// <param name="fileName">
        /// The name of the file to be processed. This parameter should not be
        /// null.
        /// </param>
        /// <param name="password">
        /// The password used to decrypt the bundle file, if any. This parameter
        /// may be null.
        /// </param>
        /// <param name="haveScriptFlags">
        /// The <see cref="IHaveScriptFlags" /> used to control how the bundle
        /// file is processed. This parameter should not be null.
        /// </param>
        /// <param name="clientData">
        /// Upon return, this may contain extra data associated with the evaluated
        /// bundle. This parameter may be null.
        /// </param>
        /// <param name="result">
        /// Upon success, this may contain the result value produced by the
        /// operation; upon failure, it must contain an appropriate error message.
        /// </param>
        /// <param name="errorLine">
        /// Upon return, this may contain the one-based line number associated
        /// with any error that was encountered.
        /// </param>
        /// <returns>
        /// Returns <see cref="ReturnCode.Ok" /> on success; otherwise, a non-Ok
        /// value (such as <see cref="ReturnCode.Error" />) with details placed in
        /// the <paramref name="result" /> parameter.
        /// </returns>
        ReturnCode EvaluateBundleFile(
            string fileName,
            byte[] password,
            IHaveScriptFlags haveScriptFlags,
            ref IClientData clientData,
            ref Result result,
            ref int errorLine
            );
#endif

        /// <summary>
        /// Evaluates the script contained in the specified file within the global
        /// scope.
        /// </summary>
        /// <param name="fileName">
        /// The name of the file to be processed. This parameter should not be
        /// null.
        /// </param>
        /// <param name="result">
        /// Upon success, this may contain the result value produced by the
        /// operation; upon failure, it must contain an appropriate error message.
        /// </param>
        /// <returns>
        /// Returns <see cref="ReturnCode.Ok" /> on success; otherwise, a non-Ok
        /// value (such as <see cref="ReturnCode.Error" />) with details placed in
        /// the <paramref name="result" /> parameter.
        /// </returns>
        ReturnCode EvaluateGlobalFile(
            string fileName,
            ref Result result
            );

        /// <summary>
        /// Evaluates the script contained in the specified file within the global
        /// scope, reporting the line number of any error.
        /// </summary>
        /// <param name="fileName">
        /// The name of the file to be processed. This parameter should not be
        /// null.
        /// </param>
        /// <param name="result">
        /// Upon success, this may contain the result value produced by the
        /// operation; upon failure, it must contain an appropriate error message.
        /// </param>
        /// <param name="errorLine">
        /// Upon return, this may contain the one-based line number associated
        /// with any error that was encountered.
        /// </param>
        /// <returns>
        /// Returns <see cref="ReturnCode.Ok" /> on success; otherwise, a non-Ok
        /// value (such as <see cref="ReturnCode.Error" />) with details placed in
        /// the <paramref name="result" /> parameter.
        /// </returns>
        ReturnCode EvaluateGlobalFile(
            string fileName,
            ref Result result,
            ref int errorLine
            );

        /// <summary>
        /// Evaluates the script contained in the specified file within the global
        /// scope, using the specified character encoding.
        /// </summary>
        /// <param name="encoding">
        /// The <see cref="Encoding" /> to use when reading the file. This
        /// parameter may be null.
        /// </param>
        /// <param name="fileName">
        /// The name of the file to be processed. This parameter should not be
        /// null.
        /// </param>
        /// <param name="result">
        /// Upon success, this may contain the result value produced by the
        /// operation; upon failure, it must contain an appropriate error message.
        /// </param>
        /// <returns>
        /// Returns <see cref="ReturnCode.Ok" /> on success; otherwise, a non-Ok
        /// value (such as <see cref="ReturnCode.Error" />) with details placed in
        /// the <paramref name="result" /> parameter.
        /// </returns>
        ReturnCode EvaluateGlobalFile(
            Encoding encoding,
            string fileName,
            ref Result result
            );

        /// <summary>
        /// Evaluates the script contained in the specified file within the global
        /// scope, using the specified character encoding, and reporting the line
        /// number of any error.
        /// </summary>
        /// <param name="encoding">
        /// The <see cref="Encoding" /> to use when reading the file. This
        /// parameter may be null.
        /// </param>
        /// <param name="fileName">
        /// The name of the file to be processed. This parameter should not be
        /// null.
        /// </param>
        /// <param name="result">
        /// Upon success, this may contain the result value produced by the
        /// operation; upon failure, it must contain an appropriate error message.
        /// </param>
        /// <param name="errorLine">
        /// Upon return, this may contain the one-based line number associated
        /// with any error that was encountered.
        /// </param>
        /// <returns>
        /// Returns <see cref="ReturnCode.Ok" /> on success; otherwise, a non-Ok
        /// value (such as <see cref="ReturnCode.Error" />) with details placed in
        /// the <paramref name="result" /> parameter.
        /// </returns>
        ReturnCode EvaluateGlobalFile(
            Encoding encoding,
            string fileName,
            ref Result result,
            ref int errorLine
            );

        /// <summary>
        /// Evaluates the script contained in the specified file using the
        /// specified scope call frame.
        /// </summary>
        /// <param name="encoding">
        /// The <see cref="Encoding" /> to use when reading the file. This
        /// parameter may be null.
        /// </param>
        /// <param name="fileName">
        /// The name of the file to be processed. This parameter should not be
        /// null.
        /// </param>
        /// <param name="frame">
        /// Upon entry, the scope call frame to use, if any; upon return, the call
        /// frame that was actually used. This parameter may be null.
        /// </param>
        /// <param name="result">
        /// Upon success, this may contain the result value produced by the
        /// operation; upon failure, it must contain an appropriate error message.
        /// </param>
        /// <param name="errorLine">
        /// Upon return, this may contain the one-based line number associated
        /// with any error that was encountered.
        /// </param>
        /// <returns>
        /// Returns <see cref="ReturnCode.Ok" /> on success; otherwise, a non-Ok
        /// value (such as <see cref="ReturnCode.Error" />) with details placed in
        /// the <paramref name="result" /> parameter.
        /// </returns>
        ReturnCode EvaluateFileWithScopeFrame(
            Encoding encoding,
            string fileName,
            ref ICallFrame frame,
            ref Result result,
            ref int errorLine
            );

        /// <summary>
        /// Evaluates the script read from the specified text reader.
        /// </summary>
        /// <param name="name">
        /// The name to associate with the stream, typically used for error
        /// reporting. This parameter may be null.
        /// </param>
        /// <param name="textReader">
        /// The <see cref="TextReader" /> to read the script or text from. This
        /// parameter should not be null.
        /// </param>
        /// <param name="result">
        /// Upon success, this may contain the result value produced by the
        /// operation; upon failure, it must contain an appropriate error message.
        /// </param>
        /// <returns>
        /// Returns <see cref="ReturnCode.Ok" /> on success; otherwise, a non-Ok
        /// value (such as <see cref="ReturnCode.Error" />) with details placed in
        /// the <paramref name="result" /> parameter.
        /// </returns>
        ReturnCode EvaluateStream(
            string name,
            TextReader textReader,
            ref Result result
            );

        /// <summary>
        /// Evaluates the script read from the specified text reader, reporting
        /// the line number of any error.
        /// </summary>
        /// <param name="name">
        /// The name to associate with the stream, typically used for error
        /// reporting. This parameter may be null.
        /// </param>
        /// <param name="textReader">
        /// The <see cref="TextReader" /> to read the script or text from. This
        /// parameter should not be null.
        /// </param>
        /// <param name="result">
        /// Upon success, this may contain the result value produced by the
        /// operation; upon failure, it must contain an appropriate error message.
        /// </param>
        /// <param name="errorLine">
        /// Upon return, this may contain the one-based line number associated
        /// with any error that was encountered.
        /// </param>
        /// <returns>
        /// Returns <see cref="ReturnCode.Ok" /> on success; otherwise, a non-Ok
        /// value (such as <see cref="ReturnCode.Error" />) with details placed in
        /// the <paramref name="result" /> parameter.
        /// </returns>
        ReturnCode EvaluateStream(
            string name,
            TextReader textReader,
            ref Result result,
            ref int errorLine
            );

        /// <summary>
        /// Evaluates a portion of the script read from the specified text reader.
        /// </summary>
        /// <param name="name">
        /// The name to associate with the stream, typically used for error
        /// reporting. This parameter may be null.
        /// </param>
        /// <param name="textReader">
        /// The <see cref="TextReader" /> to read the script or text from. This
        /// parameter should not be null.
        /// </param>
        /// <param name="startIndex">
        /// The character offset within the stream where processing should begin.
        /// </param>
        /// <param name="characters">
        /// The number of characters to read from the stream, or a negative value
        /// to read until the end of the stream.
        /// </param>
        /// <param name="result">
        /// Upon success, this may contain the result value produced by the
        /// operation; upon failure, it must contain an appropriate error message.
        /// </param>
        /// <param name="errorLine">
        /// Upon return, this may contain the one-based line number associated
        /// with any error that was encountered.
        /// </param>
        /// <returns>
        /// Returns <see cref="ReturnCode.Ok" /> on success; otherwise, a non-Ok
        /// value (such as <see cref="ReturnCode.Error" />) with details placed in
        /// the <paramref name="result" /> parameter.
        /// </returns>
        ReturnCode EvaluateStream(
            string name,
            TextReader textReader,
            int startIndex,
            int characters,
            ref Result result,
            ref int errorLine
            );

        /// <summary>
        /// Evaluates a portion of the script read from the specified text reader,
        /// using the specified engine flags.
        /// </summary>
        /// <param name="name">
        /// The name to associate with the stream, typically used for error
        /// reporting. This parameter may be null.
        /// </param>
        /// <param name="textReader">
        /// The <see cref="TextReader" /> to read the script or text from. This
        /// parameter should not be null.
        /// </param>
        /// <param name="startIndex">
        /// The character offset within the stream where processing should begin.
        /// </param>
        /// <param name="characters">
        /// The number of characters to read from the stream, or a negative value
        /// to read until the end of the stream.
        /// </param>
        /// <param name="engineFlags">
        /// The <see cref="EngineFlags" /> used to control how evaluation is
        /// performed.
        /// </param>
        /// <param name="result">
        /// Upon success, this may contain the result value produced by the
        /// operation; upon failure, it must contain an appropriate error message.
        /// </param>
        /// <param name="errorLine">
        /// Upon return, this may contain the one-based line number associated
        /// with any error that was encountered.
        /// </param>
        /// <returns>
        /// Returns <see cref="ReturnCode.Ok" /> on success; otherwise, a non-Ok
        /// value (such as <see cref="ReturnCode.Error" />) with details placed in
        /// the <paramref name="result" /> parameter.
        /// </returns>
        ReturnCode EvaluateStream(
            string name,
            TextReader textReader,
            int startIndex,
            int characters,
            EngineFlags engineFlags,
            ref Result result,
            ref int errorLine
            );

        /// <summary>
        /// Evaluates a portion of the script read from the specified text reader
        /// within the global scope.
        /// </summary>
        /// <param name="name">
        /// The name to associate with the stream, typically used for error
        /// reporting. This parameter may be null.
        /// </param>
        /// <param name="textReader">
        /// The <see cref="TextReader" /> to read the script or text from. This
        /// parameter should not be null.
        /// </param>
        /// <param name="startIndex">
        /// The character offset within the stream where processing should begin.
        /// </param>
        /// <param name="characters">
        /// The number of characters to read from the stream, or a negative value
        /// to read until the end of the stream.
        /// </param>
        /// <param name="result">
        /// Upon success, this may contain the result value produced by the
        /// operation; upon failure, it must contain an appropriate error message.
        /// </param>
        /// <param name="errorLine">
        /// Upon return, this may contain the one-based line number associated
        /// with any error that was encountered.
        /// </param>
        /// <returns>
        /// Returns <see cref="ReturnCode.Ok" /> on success; otherwise, a non-Ok
        /// value (such as <see cref="ReturnCode.Error" />) with details placed in
        /// the <paramref name="result" /> parameter.
        /// </returns>
        ReturnCode EvaluateGlobalStream(
            string name,
            TextReader textReader,
            int startIndex,
            int characters,
            ref Result result,
            ref int errorLine
            );

        /// <summary>
        /// Evaluates a portion of the script read from the specified text reader
        /// using the specified scope call frame.
        /// </summary>
        /// <param name="name">
        /// The name to associate with the stream, typically used for error
        /// reporting. This parameter may be null.
        /// </param>
        /// <param name="textReader">
        /// The <see cref="TextReader" /> to read the script or text from. This
        /// parameter should not be null.
        /// </param>
        /// <param name="startIndex">
        /// The character offset within the stream where processing should begin.
        /// </param>
        /// <param name="characters">
        /// The number of characters to read from the stream, or a negative value
        /// to read until the end of the stream.
        /// </param>
        /// <param name="frame">
        /// Upon entry, the scope call frame to use, if any; upon return, the call
        /// frame that was actually used. This parameter may be null.
        /// </param>
        /// <param name="result">
        /// Upon success, this may contain the result value produced by the
        /// operation; upon failure, it must contain an appropriate error message.
        /// </param>
        /// <param name="errorLine">
        /// Upon return, this may contain the one-based line number associated
        /// with any error that was encountered.
        /// </param>
        /// <returns>
        /// Returns <see cref="ReturnCode.Ok" /> on success; otherwise, a non-Ok
        /// value (such as <see cref="ReturnCode.Error" />) with details placed in
        /// the <paramref name="result" /> parameter.
        /// </returns>
        ReturnCode EvaluateStreamWithScopeFrame(
            string name,
            TextReader textReader,
            int startIndex,
            int characters,
            ref ICallFrame frame,
            ref Result result,
            ref int errorLine
            );

        ///////////////////////////////////////////////////////////////////////
        // TRUSTED SCRIPT EVALUATION
        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Evaluates a script contained in the specified string with elevated
        /// trust.
        /// </summary>
        /// <param name="text">
        /// The script text to be evaluated. This parameter should not be null.
        /// </param>
        /// <param name="trustFlags">
        /// The <see cref="TrustFlags" /> used to control the trust behavior
        /// during evaluation.
        /// </param>
        /// <param name="result">
        /// Upon success, this may contain the result value produced by the
        /// operation; upon failure, it must contain an appropriate error message.
        /// </param>
        /// <returns>
        /// Returns <see cref="ReturnCode.Ok" /> on success; otherwise, a non-Ok
        /// value (such as <see cref="ReturnCode.Error" />) with details placed in
        /// the <paramref name="result" /> parameter.
        /// </returns>
        ReturnCode EvaluateTrustedScript(
            string text,
            TrustFlags trustFlags,
            ref Result result
            );

        /// <summary>
        /// Evaluates a script contained in the specified string with elevated
        /// trust, associating it with the specified file name for error
        /// reporting.
        /// </summary>
        /// <param name="fileName">
        /// The name of the file to be processed. This parameter should not be
        /// null.
        /// </param>
        /// <param name="text">
        /// The script text to be evaluated. This parameter should not be null.
        /// </param>
        /// <param name="trustFlags">
        /// The <see cref="TrustFlags" /> used to control the trust behavior
        /// during evaluation.
        /// </param>
        /// <param name="result">
        /// Upon success, this may contain the result value produced by the
        /// operation; upon failure, it must contain an appropriate error message.
        /// </param>
        /// <returns>
        /// Returns <see cref="ReturnCode.Ok" /> on success; otherwise, a non-Ok
        /// value (such as <see cref="ReturnCode.Error" />) with details placed in
        /// the <paramref name="result" /> parameter.
        /// </returns>
        ReturnCode EvaluateTrustedScript(
            string fileName,
            string text,
            TrustFlags trustFlags,
            ref Result result
            );

        /// <summary>
        /// Evaluates a script contained in the specified string with elevated
        /// trust, reporting the line number of any error.
        /// </summary>
        /// <param name="text">
        /// The script text to be evaluated. This parameter should not be null.
        /// </param>
        /// <param name="trustFlags">
        /// The <see cref="TrustFlags" /> used to control the trust behavior
        /// during evaluation.
        /// </param>
        /// <param name="result">
        /// Upon success, this may contain the result value produced by the
        /// operation; upon failure, it must contain an appropriate error message.
        /// </param>
        /// <param name="errorLine">
        /// Upon return, this may contain the one-based line number associated
        /// with any error that was encountered.
        /// </param>
        /// <returns>
        /// Returns <see cref="ReturnCode.Ok" /> on success; otherwise, a non-Ok
        /// value (such as <see cref="ReturnCode.Error" />) with details placed in
        /// the <paramref name="result" /> parameter.
        /// </returns>
        ReturnCode EvaluateTrustedScript(
            string text,
            TrustFlags trustFlags,
            ref Result result,
            ref int errorLine
            );

        /// <summary>
        /// Evaluates a script contained in the specified string with elevated
        /// trust, associating it with the specified file name for error
        /// reporting, and reporting the line number of any error.
        /// </summary>
        /// <param name="fileName">
        /// The name of the file to be processed. This parameter should not be
        /// null.
        /// </param>
        /// <param name="text">
        /// The script text to be evaluated. This parameter should not be null.
        /// </param>
        /// <param name="trustFlags">
        /// The <see cref="TrustFlags" /> used to control the trust behavior
        /// during evaluation.
        /// </param>
        /// <param name="result">
        /// Upon success, this may contain the result value produced by the
        /// operation; upon failure, it must contain an appropriate error message.
        /// </param>
        /// <param name="errorLine">
        /// Upon return, this may contain the one-based line number associated
        /// with any error that was encountered.
        /// </param>
        /// <returns>
        /// Returns <see cref="ReturnCode.Ok" /> on success; otherwise, a non-Ok
        /// value (such as <see cref="ReturnCode.Error" />) with details placed in
        /// the <paramref name="result" /> parameter.
        /// </returns>
        ReturnCode EvaluateTrustedScript(
            string fileName,
            string text,
            TrustFlags trustFlags,
            ref Result result,
            ref int errorLine
            );

        /// <summary>
        /// Evaluates the script contained in the specified file with elevated
        /// trust.
        /// </summary>
        /// <param name="encoding">
        /// The <see cref="Encoding" /> to use when reading the file. This
        /// parameter may be null.
        /// </param>
        /// <param name="fileName">
        /// The name of the file to be processed. This parameter should not be
        /// null.
        /// </param>
        /// <param name="trustFlags">
        /// The <see cref="TrustFlags" /> used to control the trust behavior
        /// during evaluation.
        /// </param>
        /// <param name="result">
        /// Upon success, this may contain the result value produced by the
        /// operation; upon failure, it must contain an appropriate error message.
        /// </param>
        /// <returns>
        /// Returns <see cref="ReturnCode.Ok" /> on success; otherwise, a non-Ok
        /// value (such as <see cref="ReturnCode.Error" />) with details placed in
        /// the <paramref name="result" /> parameter.
        /// </returns>
        ReturnCode EvaluateTrustedFile(
            Encoding encoding,
            string fileName,
            TrustFlags trustFlags,
            ref Result result
            );

        /// <summary>
        /// Evaluates the script contained in the specified file with elevated
        /// trust, reporting the line number of any error.
        /// </summary>
        /// <param name="encoding">
        /// The <see cref="Encoding" /> to use when reading the file. This
        /// parameter may be null.
        /// </param>
        /// <param name="fileName">
        /// The name of the file to be processed. This parameter should not be
        /// null.
        /// </param>
        /// <param name="trustFlags">
        /// The <see cref="TrustFlags" /> used to control the trust behavior
        /// during evaluation.
        /// </param>
        /// <param name="result">
        /// Upon success, this may contain the result value produced by the
        /// operation; upon failure, it must contain an appropriate error message.
        /// </param>
        /// <param name="errorLine">
        /// Upon return, this may contain the one-based line number associated
        /// with any error that was encountered.
        /// </param>
        /// <returns>
        /// Returns <see cref="ReturnCode.Ok" /> on success; otherwise, a non-Ok
        /// value (such as <see cref="ReturnCode.Error" />) with details placed in
        /// the <paramref name="result" /> parameter.
        /// </returns>
        ReturnCode EvaluateTrustedFile(
            Encoding encoding,
            string fileName,
            TrustFlags trustFlags,
            ref Result result,
            ref int errorLine
            );

        /// <summary>
        /// Evaluates the script read from the specified text reader with elevated
        /// trust.
        /// </summary>
        /// <param name="name">
        /// The name to associate with the stream, typically used for error
        /// reporting. This parameter may be null.
        /// </param>
        /// <param name="textReader">
        /// The <see cref="TextReader" /> to read the script or text from. This
        /// parameter should not be null.
        /// </param>
        /// <param name="trustFlags">
        /// The <see cref="TrustFlags" /> used to control the trust behavior
        /// during evaluation.
        /// </param>
        /// <param name="result">
        /// Upon success, this may contain the result value produced by the
        /// operation; upon failure, it must contain an appropriate error message.
        /// </param>
        /// <returns>
        /// Returns <see cref="ReturnCode.Ok" /> on success; otherwise, a non-Ok
        /// value (such as <see cref="ReturnCode.Error" />) with details placed in
        /// the <paramref name="result" /> parameter.
        /// </returns>
        ReturnCode EvaluateTrustedStream(
            string name,
            TextReader textReader,
            TrustFlags trustFlags,
            ref Result result
            );

        /// <summary>
        /// Evaluates the script read from the specified text reader with elevated
        /// trust, reporting the line number of any error.
        /// </summary>
        /// <param name="name">
        /// The name to associate with the stream, typically used for error
        /// reporting. This parameter may be null.
        /// </param>
        /// <param name="textReader">
        /// The <see cref="TextReader" /> to read the script or text from. This
        /// parameter should not be null.
        /// </param>
        /// <param name="trustFlags">
        /// The <see cref="TrustFlags" /> used to control the trust behavior
        /// during evaluation.
        /// </param>
        /// <param name="result">
        /// Upon success, this may contain the result value produced by the
        /// operation; upon failure, it must contain an appropriate error message.
        /// </param>
        /// <param name="errorLine">
        /// Upon return, this may contain the one-based line number associated
        /// with any error that was encountered.
        /// </param>
        /// <returns>
        /// Returns <see cref="ReturnCode.Ok" /> on success; otherwise, a non-Ok
        /// value (such as <see cref="ReturnCode.Error" />) with details placed in
        /// the <paramref name="result" /> parameter.
        /// </returns>
        ReturnCode EvaluateTrustedStream(
            string name,
            TextReader textReader,
            TrustFlags trustFlags,
            ref Result result,
            ref int errorLine
            );

        /// <summary>
        /// Evaluates a portion of the script read from the specified text reader
        /// with elevated trust.
        /// </summary>
        /// <param name="name">
        /// The name to associate with the stream, typically used for error
        /// reporting. This parameter may be null.
        /// </param>
        /// <param name="textReader">
        /// The <see cref="TextReader" /> to read the script or text from. This
        /// parameter should not be null.
        /// </param>
        /// <param name="startIndex">
        /// The character offset within the stream where processing should begin.
        /// </param>
        /// <param name="characters">
        /// The number of characters to read from the stream, or a negative value
        /// to read until the end of the stream.
        /// </param>
        /// <param name="trustFlags">
        /// The <see cref="TrustFlags" /> used to control the trust behavior
        /// during evaluation.
        /// </param>
        /// <param name="result">
        /// Upon success, this may contain the result value produced by the
        /// operation; upon failure, it must contain an appropriate error message.
        /// </param>
        /// <returns>
        /// Returns <see cref="ReturnCode.Ok" /> on success; otherwise, a non-Ok
        /// value (such as <see cref="ReturnCode.Error" />) with details placed in
        /// the <paramref name="result" /> parameter.
        /// </returns>
        ReturnCode EvaluateTrustedStream(
            string name,
            TextReader textReader,
            int startIndex,
            int characters,
            TrustFlags trustFlags,
            ref Result result
            );

        /// <summary>
        /// Evaluates a portion of the script read from the specified text reader
        /// with elevated trust, reporting the line number of any error.
        /// </summary>
        /// <param name="name">
        /// The name to associate with the stream, typically used for error
        /// reporting. This parameter may be null.
        /// </param>
        /// <param name="textReader">
        /// The <see cref="TextReader" /> to read the script or text from. This
        /// parameter should not be null.
        /// </param>
        /// <param name="startIndex">
        /// The character offset within the stream where processing should begin.
        /// </param>
        /// <param name="characters">
        /// The number of characters to read from the stream, or a negative value
        /// to read until the end of the stream.
        /// </param>
        /// <param name="trustFlags">
        /// The <see cref="TrustFlags" /> used to control the trust behavior
        /// during evaluation.
        /// </param>
        /// <param name="result">
        /// Upon success, this may contain the result value produced by the
        /// operation; upon failure, it must contain an appropriate error message.
        /// </param>
        /// <param name="errorLine">
        /// Upon return, this may contain the one-based line number associated
        /// with any error that was encountered.
        /// </param>
        /// <returns>
        /// Returns <see cref="ReturnCode.Ok" /> on success; otherwise, a non-Ok
        /// value (such as <see cref="ReturnCode.Error" />) with details placed in
        /// the <paramref name="result" /> parameter.
        /// </returns>
        ReturnCode EvaluateTrustedStream(
            string name,
            TextReader textReader,
            int startIndex,
            int characters,
            TrustFlags trustFlags,
            ref Result result,
            ref int errorLine
            );

        ///////////////////////////////////////////////////////////////////////
        // EXPRESSION EVALUATION
        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Evaluates the specified expression.
        /// </summary>
        /// <param name="text">
        /// The script text to be evaluated. This parameter should not be null.
        /// </param>
        /// <param name="result">
        /// Upon success, this may contain the result value produced by the
        /// operation; upon failure, it must contain an appropriate error message.
        /// </param>
        /// <returns>
        /// Returns <see cref="ReturnCode.Ok" /> on success; otherwise, a non-Ok
        /// value (such as <see cref="ReturnCode.Error" />) with details placed in
        /// the <paramref name="result" /> parameter.
        /// </returns>
        ReturnCode EvaluateExpression(
            string text,
            ref Result result
            );

        /// <summary>
        /// Evaluates the specified expression, using the specified initial error
        /// information.
        /// </summary>
        /// <param name="text">
        /// The script text to be evaluated. This parameter should not be null.
        /// </param>
        /// <param name="errorInfo">
        /// The initial error information to use when an error is encountered.
        /// This parameter may be null.
        /// </param>
        /// <param name="result">
        /// Upon success, this may contain the result value produced by the
        /// operation; upon failure, it must contain an appropriate error message.
        /// </param>
        /// <returns>
        /// Returns <see cref="ReturnCode.Ok" /> on success; otherwise, a non-Ok
        /// value (such as <see cref="ReturnCode.Error" />) with details placed in
        /// the <paramref name="result" /> parameter.
        /// </returns>
        ReturnCode EvaluateExpressionWithErrorInfo(
            string text,
            string errorInfo,
            ref Result result
            );

        ///////////////////////////////////////////////////////////////////////
        // SUBSTITUTION PROCESSING
        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Performs substitution processing on the specified string.
        /// </summary>
        /// <param name="text">
        /// The text to be processed for substitution. This parameter should not
        /// be null.
        /// </param>
        /// <param name="result">
        /// Upon success, this may contain the result value produced by the
        /// operation; upon failure, it must contain an appropriate error message.
        /// </param>
        /// <returns>
        /// Returns <see cref="ReturnCode.Ok" /> on success; otherwise, a non-Ok
        /// value (such as <see cref="ReturnCode.Error" />) with details placed in
        /// the <paramref name="result" /> parameter.
        /// </returns>
        ReturnCode SubstituteString(
            string text,
            ref Result result
            );

        /// <summary>
        /// Performs substitution processing on the specified string, using the
        /// specified substitution flags.
        /// </summary>
        /// <param name="text">
        /// The text to be processed for substitution. This parameter should not
        /// be null.
        /// </param>
        /// <param name="substitutionFlags">
        /// The <see cref="SubstitutionFlags" /> used to control which kinds of
        /// substitution are performed.
        /// </param>
        /// <param name="result">
        /// Upon success, this may contain the result value produced by the
        /// operation; upon failure, it must contain an appropriate error message.
        /// </param>
        /// <returns>
        /// Returns <see cref="ReturnCode.Ok" /> on success; otherwise, a non-Ok
        /// value (such as <see cref="ReturnCode.Error" />) with details placed in
        /// the <paramref name="result" /> parameter.
        /// </returns>
        ReturnCode SubstituteString(
            string text,
            SubstitutionFlags substitutionFlags,
            ref Result result
            );

        /// <summary>
        /// Performs substitution processing on the specified string within the
        /// global scope.
        /// </summary>
        /// <param name="text">
        /// The text to be processed for substitution. This parameter should not
        /// be null.
        /// </param>
        /// <param name="result">
        /// Upon success, this may contain the result value produced by the
        /// operation; upon failure, it must contain an appropriate error message.
        /// </param>
        /// <returns>
        /// Returns <see cref="ReturnCode.Ok" /> on success; otherwise, a non-Ok
        /// value (such as <see cref="ReturnCode.Error" />) with details placed in
        /// the <paramref name="result" /> parameter.
        /// </returns>
        ReturnCode SubstituteGlobalString(
            string text,
            ref Result result
            );

        /// <summary>
        /// Performs substitution processing on the specified string within the
        /// global scope, using the specified substitution flags.
        /// </summary>
        /// <param name="text">
        /// The text to be processed for substitution. This parameter should not
        /// be null.
        /// </param>
        /// <param name="substitutionFlags">
        /// The <see cref="SubstitutionFlags" /> used to control which kinds of
        /// substitution are performed.
        /// </param>
        /// <param name="result">
        /// Upon success, this may contain the result value produced by the
        /// operation; upon failure, it must contain an appropriate error message.
        /// </param>
        /// <returns>
        /// Returns <see cref="ReturnCode.Ok" /> on success; otherwise, a non-Ok
        /// value (such as <see cref="ReturnCode.Error" />) with details placed in
        /// the <paramref name="result" /> parameter.
        /// </returns>
        ReturnCode SubstituteGlobalString(
            string text,
            SubstitutionFlags substitutionFlags,
            ref Result result
            );

        /// <summary>
        /// Performs substitution processing on the contents of the specified
        /// file.
        /// </summary>
        /// <param name="fileName">
        /// The name of the file to be processed. This parameter should not be
        /// null.
        /// </param>
        /// <param name="result">
        /// Upon success, this may contain the result value produced by the
        /// operation; upon failure, it must contain an appropriate error message.
        /// </param>
        /// <returns>
        /// Returns <see cref="ReturnCode.Ok" /> on success; otherwise, a non-Ok
        /// value (such as <see cref="ReturnCode.Error" />) with details placed in
        /// the <paramref name="result" /> parameter.
        /// </returns>
        ReturnCode SubstituteFile(
            string fileName,
            ref Result result
            );

        /// <summary>
        /// Performs substitution processing on the contents of the specified
        /// file, using the specified substitution flags.
        /// </summary>
        /// <param name="fileName">
        /// The name of the file to be processed. This parameter should not be
        /// null.
        /// </param>
        /// <param name="substitutionFlags">
        /// The <see cref="SubstitutionFlags" /> used to control which kinds of
        /// substitution are performed.
        /// </param>
        /// <param name="result">
        /// Upon success, this may contain the result value produced by the
        /// operation; upon failure, it must contain an appropriate error message.
        /// </param>
        /// <returns>
        /// Returns <see cref="ReturnCode.Ok" /> on success; otherwise, a non-Ok
        /// value (such as <see cref="ReturnCode.Error" />) with details placed in
        /// the <paramref name="result" /> parameter.
        /// </returns>
        ReturnCode SubstituteFile(
            string fileName,
            SubstitutionFlags substitutionFlags,
            ref Result result
            );

        /// <summary>
        /// Performs substitution processing on the contents of the specified file
        /// within the global scope.
        /// </summary>
        /// <param name="fileName">
        /// The name of the file to be processed. This parameter should not be
        /// null.
        /// </param>
        /// <param name="result">
        /// Upon success, this may contain the result value produced by the
        /// operation; upon failure, it must contain an appropriate error message.
        /// </param>
        /// <returns>
        /// Returns <see cref="ReturnCode.Ok" /> on success; otherwise, a non-Ok
        /// value (such as <see cref="ReturnCode.Error" />) with details placed in
        /// the <paramref name="result" /> parameter.
        /// </returns>
        ReturnCode SubstituteGlobalFile(
            string fileName,
            ref Result result
            );

        /// <summary>
        /// Performs substitution processing on the contents of the specified file
        /// within the global scope, using the specified substitution flags.
        /// </summary>
        /// <param name="fileName">
        /// The name of the file to be processed. This parameter should not be
        /// null.
        /// </param>
        /// <param name="substitutionFlags">
        /// The <see cref="SubstitutionFlags" /> used to control which kinds of
        /// substitution are performed.
        /// </param>
        /// <param name="result">
        /// Upon success, this may contain the result value produced by the
        /// operation; upon failure, it must contain an appropriate error message.
        /// </param>
        /// <returns>
        /// Returns <see cref="ReturnCode.Ok" /> on success; otherwise, a non-Ok
        /// value (such as <see cref="ReturnCode.Error" />) with details placed in
        /// the <paramref name="result" /> parameter.
        /// </returns>
        ReturnCode SubstituteGlobalFile(
            string fileName,
            SubstitutionFlags substitutionFlags,
            ref Result result
            );

        /// <summary>
        /// Performs substitution processing on a portion of the text read from
        /// the specified text reader.
        /// </summary>
        /// <param name="name">
        /// The name to associate with the stream, typically used for error
        /// reporting. This parameter may be null.
        /// </param>
        /// <param name="textReader">
        /// The <see cref="TextReader" /> to read the script or text from. This
        /// parameter should not be null.
        /// </param>
        /// <param name="startIndex">
        /// The character offset within the stream where processing should begin.
        /// </param>
        /// <param name="characters">
        /// The number of characters to read from the stream, or a negative value
        /// to read until the end of the stream.
        /// </param>
        /// <param name="result">
        /// Upon success, this may contain the result value produced by the
        /// operation; upon failure, it must contain an appropriate error message.
        /// </param>
        /// <returns>
        /// Returns <see cref="ReturnCode.Ok" /> on success; otherwise, a non-Ok
        /// value (such as <see cref="ReturnCode.Error" />) with details placed in
        /// the <paramref name="result" /> parameter.
        /// </returns>
        ReturnCode SubstituteStream(
            string name,
            TextReader textReader,
            int startIndex,
            int characters,
            ref Result result
            );

        /// <summary>
        /// Performs substitution processing on a portion of the text read from
        /// the specified text reader, using the specified substitution flags.
        /// </summary>
        /// <param name="name">
        /// The name to associate with the stream, typically used for error
        /// reporting. This parameter may be null.
        /// </param>
        /// <param name="textReader">
        /// The <see cref="TextReader" /> to read the script or text from. This
        /// parameter should not be null.
        /// </param>
        /// <param name="startIndex">
        /// The character offset within the stream where processing should begin.
        /// </param>
        /// <param name="characters">
        /// The number of characters to read from the stream, or a negative value
        /// to read until the end of the stream.
        /// </param>
        /// <param name="substitutionFlags">
        /// The <see cref="SubstitutionFlags" /> used to control which kinds of
        /// substitution are performed.
        /// </param>
        /// <param name="result">
        /// Upon success, this may contain the result value produced by the
        /// operation; upon failure, it must contain an appropriate error message.
        /// </param>
        /// <returns>
        /// Returns <see cref="ReturnCode.Ok" /> on success; otherwise, a non-Ok
        /// value (such as <see cref="ReturnCode.Error" />) with details placed in
        /// the <paramref name="result" /> parameter.
        /// </returns>
        ReturnCode SubstituteStream(
            string name,
            TextReader textReader,
            int startIndex,
            int characters,
            SubstitutionFlags substitutionFlags,
            ref Result result
            );

        /// <summary>
        /// Performs substitution processing on a portion of the text read from
        /// the specified text reader within the global scope.
        /// </summary>
        /// <param name="name">
        /// The name to associate with the stream, typically used for error
        /// reporting. This parameter may be null.
        /// </param>
        /// <param name="textReader">
        /// The <see cref="TextReader" /> to read the script or text from. This
        /// parameter should not be null.
        /// </param>
        /// <param name="startIndex">
        /// The character offset within the stream where processing should begin.
        /// </param>
        /// <param name="characters">
        /// The number of characters to read from the stream, or a negative value
        /// to read until the end of the stream.
        /// </param>
        /// <param name="result">
        /// Upon success, this may contain the result value produced by the
        /// operation; upon failure, it must contain an appropriate error message.
        /// </param>
        /// <returns>
        /// Returns <see cref="ReturnCode.Ok" /> on success; otherwise, a non-Ok
        /// value (such as <see cref="ReturnCode.Error" />) with details placed in
        /// the <paramref name="result" /> parameter.
        /// </returns>
        ReturnCode SubstituteGlobalStream(
            string name,
            TextReader textReader,
            int startIndex,
            int characters,
            ref Result result
            );

        /// <summary>
        /// Performs substitution processing on a portion of the text read from
        /// the specified text reader within the global scope, using the specified
        /// substitution flags.
        /// </summary>
        /// <param name="name">
        /// The name to associate with the stream, typically used for error
        /// reporting. This parameter may be null.
        /// </param>
        /// <param name="textReader">
        /// The <see cref="TextReader" /> to read the script or text from. This
        /// parameter should not be null.
        /// </param>
        /// <param name="startIndex">
        /// The character offset within the stream where processing should begin.
        /// </param>
        /// <param name="characters">
        /// The number of characters to read from the stream, or a negative value
        /// to read until the end of the stream.
        /// </param>
        /// <param name="substitutionFlags">
        /// The <see cref="SubstitutionFlags" /> used to control which kinds of
        /// substitution are performed.
        /// </param>
        /// <param name="result">
        /// Upon success, this may contain the result value produced by the
        /// operation; upon failure, it must contain an appropriate error message.
        /// </param>
        /// <returns>
        /// Returns <see cref="ReturnCode.Ok" /> on success; otherwise, a non-Ok
        /// value (such as <see cref="ReturnCode.Error" />) with details placed in
        /// the <paramref name="result" /> parameter.
        /// </returns>
        ReturnCode SubstituteGlobalStream(
            string name,
            TextReader textReader,
            int startIndex,
            int characters,
            SubstitutionFlags substitutionFlags,
            ref Result result
            );

        ///////////////////////////////////////////////////////////////////////
        // ASYNCHRONOUS SCRIPT EVALUATION
        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Asynchronously evaluates a script contained in the specified string.
        /// </summary>
        /// <param name="text">
        /// The script text to be evaluated. This parameter should not be null.
        /// </param>
        /// <param name="callback">
        /// The <see cref="AsynchronousCallback" /> to invoke when the
        /// asynchronous operation completes. This parameter may be null.
        /// </param>
        /// <param name="clientData">
        /// The extra data to pass to the callback, if any. This parameter may be
        /// null.
        /// </param>
        /// <param name="error">
        /// Upon failure, this must contain an appropriate error message.
        /// </param>
        /// <returns>
        /// Returns <see cref="ReturnCode.Ok" /> if the asynchronous operation was
        /// successfully started; otherwise, a non-Ok value (such as
        /// <see cref="ReturnCode.Error" />) with details placed in the
        /// <paramref name="error" /> parameter.
        /// </returns>
        ReturnCode EvaluateScript(
            string text,
            AsynchronousCallback callback,
            IClientData clientData,
            ref Result error
            );

        /// <summary>
        /// Asynchronously evaluates a script contained in the specified string
        /// within the global scope.
        /// </summary>
        /// <param name="text">
        /// The script text to be evaluated. This parameter should not be null.
        /// </param>
        /// <param name="callback">
        /// The <see cref="AsynchronousCallback" /> to invoke when the
        /// asynchronous operation completes. This parameter may be null.
        /// </param>
        /// <param name="clientData">
        /// The extra data to pass to the callback, if any. This parameter may be
        /// null.
        /// </param>
        /// <param name="error">
        /// Upon failure, this must contain an appropriate error message.
        /// </param>
        /// <returns>
        /// Returns <see cref="ReturnCode.Ok" /> if the asynchronous operation was
        /// successfully started; otherwise, a non-Ok value (such as
        /// <see cref="ReturnCode.Error" />) with details placed in the
        /// <paramref name="error" /> parameter.
        /// </returns>
        ReturnCode EvaluateGlobalScript(
            string text,
            AsynchronousCallback callback,
            IClientData clientData,
            ref Result error
            );

        /// <summary>
        /// Asynchronously evaluates the script contained in the specified file.
        /// </summary>
        /// <param name="fileName">
        /// The name of the file to be processed. This parameter should not be
        /// null.
        /// </param>
        /// <param name="callback">
        /// The <see cref="AsynchronousCallback" /> to invoke when the
        /// asynchronous operation completes. This parameter may be null.
        /// </param>
        /// <param name="clientData">
        /// The extra data to pass to the callback, if any. This parameter may be
        /// null.
        /// </param>
        /// <param name="error">
        /// Upon failure, this must contain an appropriate error message.
        /// </param>
        /// <returns>
        /// Returns <see cref="ReturnCode.Ok" /> if the asynchronous operation was
        /// successfully started; otherwise, a non-Ok value (such as
        /// <see cref="ReturnCode.Error" />) with details placed in the
        /// <paramref name="error" /> parameter.
        /// </returns>
        ReturnCode EvaluateFile(
            string fileName,
            AsynchronousCallback callback,
            IClientData clientData,
            ref Result error
            );

        /// <summary>
        /// Asynchronously evaluates the script contained in the specified file
        /// within the global scope.
        /// </summary>
        /// <param name="fileName">
        /// The name of the file to be processed. This parameter should not be
        /// null.
        /// </param>
        /// <param name="callback">
        /// The <see cref="AsynchronousCallback" /> to invoke when the
        /// asynchronous operation completes. This parameter may be null.
        /// </param>
        /// <param name="clientData">
        /// The extra data to pass to the callback, if any. This parameter may be
        /// null.
        /// </param>
        /// <param name="error">
        /// Upon failure, this must contain an appropriate error message.
        /// </param>
        /// <returns>
        /// Returns <see cref="ReturnCode.Ok" /> if the asynchronous operation was
        /// successfully started; otherwise, a non-Ok value (such as
        /// <see cref="ReturnCode.Error" />) with details placed in the
        /// <paramref name="error" /> parameter.
        /// </returns>
        ReturnCode EvaluateGlobalFile(
            string fileName,
            AsynchronousCallback callback,
            IClientData clientData,
            ref Result error
            );

        ///////////////////////////////////////////////////////////////////////
        // ASYNCHRONOUS SUBSTITUTION PROCESSING
        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Asynchronously performs substitution processing on the specified
        /// string.
        /// </summary>
        /// <param name="text">
        /// The text to be processed for substitution. This parameter should not
        /// be null.
        /// </param>
        /// <param name="callback">
        /// The <see cref="AsynchronousCallback" /> to invoke when the
        /// asynchronous operation completes. This parameter may be null.
        /// </param>
        /// <param name="clientData">
        /// The extra data to pass to the callback, if any. This parameter may be
        /// null.
        /// </param>
        /// <param name="error">
        /// Upon failure, this must contain an appropriate error message.
        /// </param>
        /// <returns>
        /// Returns <see cref="ReturnCode.Ok" /> if the asynchronous operation was
        /// successfully started; otherwise, a non-Ok value (such as
        /// <see cref="ReturnCode.Error" />) with details placed in the
        /// <paramref name="error" /> parameter.
        /// </returns>
        ReturnCode SubstituteString(
            string text,
            AsynchronousCallback callback,
            IClientData clientData,
            ref Result error
            );

        /// <summary>
        /// Asynchronously performs substitution processing on the specified
        /// string, using the specified substitution flags.
        /// </summary>
        /// <param name="text">
        /// The text to be processed for substitution. This parameter should not
        /// be null.
        /// </param>
        /// <param name="substitutionFlags">
        /// The <see cref="SubstitutionFlags" /> used to control which kinds of
        /// substitution are performed.
        /// </param>
        /// <param name="callback">
        /// The <see cref="AsynchronousCallback" /> to invoke when the
        /// asynchronous operation completes. This parameter may be null.
        /// </param>
        /// <param name="clientData">
        /// The extra data to pass to the callback, if any. This parameter may be
        /// null.
        /// </param>
        /// <param name="error">
        /// Upon failure, this must contain an appropriate error message.
        /// </param>
        /// <returns>
        /// Returns <see cref="ReturnCode.Ok" /> if the asynchronous operation was
        /// successfully started; otherwise, a non-Ok value (such as
        /// <see cref="ReturnCode.Error" />) with details placed in the
        /// <paramref name="error" /> parameter.
        /// </returns>
        ReturnCode SubstituteString(
            string text,
            SubstitutionFlags substitutionFlags,
            AsynchronousCallback callback,
            IClientData clientData,
            ref Result error);

        /// <summary>
        /// Asynchronously performs substitution processing on the specified
        /// string within the global scope.
        /// </summary>
        /// <param name="text">
        /// The text to be processed for substitution. This parameter should not
        /// be null.
        /// </param>
        /// <param name="callback">
        /// The <see cref="AsynchronousCallback" /> to invoke when the
        /// asynchronous operation completes. This parameter may be null.
        /// </param>
        /// <param name="clientData">
        /// The extra data to pass to the callback, if any. This parameter may be
        /// null.
        /// </param>
        /// <param name="error">
        /// Upon failure, this must contain an appropriate error message.
        /// </param>
        /// <returns>
        /// Returns <see cref="ReturnCode.Ok" /> if the asynchronous operation was
        /// successfully started; otherwise, a non-Ok value (such as
        /// <see cref="ReturnCode.Error" />) with details placed in the
        /// <paramref name="error" /> parameter.
        /// </returns>
        ReturnCode SubstituteGlobalString(
            string text,
            AsynchronousCallback callback,
            IClientData clientData,
            ref Result error
            );

        /// <summary>
        /// Asynchronously performs substitution processing on the specified
        /// string within the global scope, using the specified substitution
        /// flags.
        /// </summary>
        /// <param name="text">
        /// The text to be processed for substitution. This parameter should not
        /// be null.
        /// </param>
        /// <param name="substitutionFlags">
        /// The <see cref="SubstitutionFlags" /> used to control which kinds of
        /// substitution are performed.
        /// </param>
        /// <param name="callback">
        /// The <see cref="AsynchronousCallback" /> to invoke when the
        /// asynchronous operation completes. This parameter may be null.
        /// </param>
        /// <param name="clientData">
        /// The extra data to pass to the callback, if any. This parameter may be
        /// null.
        /// </param>
        /// <param name="error">
        /// Upon failure, this must contain an appropriate error message.
        /// </param>
        /// <returns>
        /// Returns <see cref="ReturnCode.Ok" /> if the asynchronous operation was
        /// successfully started; otherwise, a non-Ok value (such as
        /// <see cref="ReturnCode.Error" />) with details placed in the
        /// <paramref name="error" /> parameter.
        /// </returns>
        ReturnCode SubstituteGlobalString(
            string text,
            SubstitutionFlags substitutionFlags,
            AsynchronousCallback callback,
            IClientData clientData,
            ref Result error);

        /// <summary>
        /// Asynchronously performs substitution processing on the contents of the
        /// specified file.
        /// </summary>
        /// <param name="fileName">
        /// The name of the file to be processed. This parameter should not be
        /// null.
        /// </param>
        /// <param name="callback">
        /// The <see cref="AsynchronousCallback" /> to invoke when the
        /// asynchronous operation completes. This parameter may be null.
        /// </param>
        /// <param name="clientData">
        /// The extra data to pass to the callback, if any. This parameter may be
        /// null.
        /// </param>
        /// <param name="error">
        /// Upon failure, this must contain an appropriate error message.
        /// </param>
        /// <returns>
        /// Returns <see cref="ReturnCode.Ok" /> if the asynchronous operation was
        /// successfully started; otherwise, a non-Ok value (such as
        /// <see cref="ReturnCode.Error" />) with details placed in the
        /// <paramref name="error" /> parameter.
        /// </returns>
        ReturnCode SubstituteFile(
            string fileName,
            AsynchronousCallback callback,
            IClientData clientData,
            ref Result error
            );

        /// <summary>
        /// Asynchronously performs substitution processing on the contents of the
        /// specified file, using the specified substitution flags.
        /// </summary>
        /// <param name="fileName">
        /// The name of the file to be processed. This parameter should not be
        /// null.
        /// </param>
        /// <param name="substitutionFlags">
        /// The <see cref="SubstitutionFlags" /> used to control which kinds of
        /// substitution are performed.
        /// </param>
        /// <param name="callback">
        /// The <see cref="AsynchronousCallback" /> to invoke when the
        /// asynchronous operation completes. This parameter may be null.
        /// </param>
        /// <param name="clientData">
        /// The extra data to pass to the callback, if any. This parameter may be
        /// null.
        /// </param>
        /// <param name="error">
        /// Upon failure, this must contain an appropriate error message.
        /// </param>
        /// <returns>
        /// Returns <see cref="ReturnCode.Ok" /> if the asynchronous operation was
        /// successfully started; otherwise, a non-Ok value (such as
        /// <see cref="ReturnCode.Error" />) with details placed in the
        /// <paramref name="error" /> parameter.
        /// </returns>
        ReturnCode SubstituteFile(
            string fileName,
            SubstitutionFlags substitutionFlags,
            AsynchronousCallback callback,
            IClientData clientData,
            ref Result error
            );

        /// <summary>
        /// Asynchronously performs substitution processing on the contents of the
        /// specified file within the global scope.
        /// </summary>
        /// <param name="fileName">
        /// The name of the file to be processed. This parameter should not be
        /// null.
        /// </param>
        /// <param name="callback">
        /// The <see cref="AsynchronousCallback" /> to invoke when the
        /// asynchronous operation completes. This parameter may be null.
        /// </param>
        /// <param name="clientData">
        /// The extra data to pass to the callback, if any. This parameter may be
        /// null.
        /// </param>
        /// <param name="error">
        /// Upon failure, this must contain an appropriate error message.
        /// </param>
        /// <returns>
        /// Returns <see cref="ReturnCode.Ok" /> if the asynchronous operation was
        /// successfully started; otherwise, a non-Ok value (such as
        /// <see cref="ReturnCode.Error" />) with details placed in the
        /// <paramref name="error" /> parameter.
        /// </returns>
        ReturnCode SubstituteGlobalFile(
            string fileName,
            AsynchronousCallback callback,
            IClientData clientData,
            ref Result error
            );

        /// <summary>
        /// Asynchronously performs substitution processing on the contents of the
        /// specified file within the global scope, using the specified
        /// substitution flags.
        /// </summary>
        /// <param name="fileName">
        /// The name of the file to be processed. This parameter should not be
        /// null.
        /// </param>
        /// <param name="substitutionFlags">
        /// The <see cref="SubstitutionFlags" /> used to control which kinds of
        /// substitution are performed.
        /// </param>
        /// <param name="callback">
        /// The <see cref="AsynchronousCallback" /> to invoke when the
        /// asynchronous operation completes. This parameter may be null.
        /// </param>
        /// <param name="clientData">
        /// The extra data to pass to the callback, if any. This parameter may be
        /// null.
        /// </param>
        /// <param name="error">
        /// Upon failure, this must contain an appropriate error message.
        /// </param>
        /// <returns>
        /// Returns <see cref="ReturnCode.Ok" /> if the asynchronous operation was
        /// successfully started; otherwise, a non-Ok value (such as
        /// <see cref="ReturnCode.Error" />) with details placed in the
        /// <paramref name="error" /> parameter.
        /// </returns>
        ReturnCode SubstituteGlobalFile(
            string fileName,
            SubstitutionFlags substitutionFlags,
            AsynchronousCallback callback,
            IClientData clientData,
            ref Result error
            );

        ///////////////////////////////////////////////////////////////////////
        // COMMAND EXECUTION
        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Invokes the named command with the specified arguments.
        /// </summary>
        /// <param name="name">
        /// The name of the command to invoke. This parameter should not be null.
        /// </param>
        /// <param name="clientData">
        /// The extra data to pass to the callback, if any. This parameter may be
        /// null.
        /// </param>
        /// <param name="arguments">
        /// The list of arguments to pass to the command. This parameter should
        /// not be null.
        /// </param>
        /// <param name="result">
        /// Upon success, this may contain the result value produced by the
        /// operation; upon failure, it must contain an appropriate error message.
        /// </param>
        /// <returns>
        /// Returns <see cref="ReturnCode.Ok" /> on success; otherwise, a non-Ok
        /// value (such as <see cref="ReturnCode.Error" />) with details placed in
        /// the <paramref name="result" /> parameter.
        /// </returns>
        ReturnCode Invoke(
            string name,
            IClientData clientData,
            ArgumentList arguments,
            ref Result result
            );
    }
}
