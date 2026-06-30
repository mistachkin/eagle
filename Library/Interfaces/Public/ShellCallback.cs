/*
 * ShellCallback.cs --
 *
 * Copyright (c) 2007-2012 by Joe Mistachkin.  All rights reserved.
 *
 * See the file "license.terms" for information on usage and redistribution of
 * this file, and for a DISCLAIMER OF ALL WARRANTIES.
 *
 * RCS: @(#) $Id: $
 */

using System.Collections.Generic;
using System.Text;
using Eagle._Attributes;
using Eagle._Components.Public;

namespace Eagle._Interfaces.Public
{
    /// <summary>
    /// This interface is implemented by entities that customize how the
    /// interactive shell processes its command-line arguments and evaluates
    /// scripts.  Its methods provide hooks that are invoked during shell
    /// startup and during script evaluation.
    /// </summary>
    [ObjectId("e8744274-1efb-47c0-83c5-41e50eff3cf8")]
    public interface IShellCallback
    {
        /// <summary>
        /// This method is called by the shell to preview a single
        /// command-line argument before it is otherwise processed.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter context the shell is running in.  This parameter
        /// should not be null.
        /// </param>
        /// <param name="interactiveHost">
        /// The interactive host associated with the interpreter, if any.
        /// This parameter may be null.
        /// </param>
        /// <param name="clientData">
        /// The extra, entity-specific data to use for this callback, if any.
        /// This parameter may be null.
        /// </param>
        /// <param name="whatIf">
        /// If true, the argument processing should be simulated only, without
        /// performing the underlying actions.
        /// </param>
        /// <param name="index">
        /// The index of the argument being previewed.  Upon return, this may
        /// be modified to change which argument is processed next.
        /// </param>
        /// <param name="arg">
        /// The argument being previewed.  Upon return, this may be modified
        /// to change the argument that is processed.
        /// </param>
        /// <param name="argv">
        /// The list of command-line arguments being processed.  Upon return,
        /// this may be modified.
        /// </param>
        /// <param name="result">
        /// Upon success, this may contain a result value.  Upon failure, this
        /// must contain an appropriate error message.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise, a non-Ok
        /// value with details placed in the <paramref name="result" />
        /// parameter.
        /// </returns>
        ReturnCode PreviewArgument(
            Interpreter interpreter, // TODO: Change to use IInterpreter type.
            IInteractiveHost interactiveHost,
            IClientData clientData,
            bool whatIf,
            ref int index,
            ref string arg,
            ref IList<string> argv,
            ref Result result
        );

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method is called by the shell when it encounters a
        /// command-line argument that it cannot otherwise handle.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter context the shell is running in.  This parameter
        /// should not be null.
        /// </param>
        /// <param name="interactiveHost">
        /// The interactive host associated with the interpreter, if any.
        /// This parameter may be null.
        /// </param>
        /// <param name="clientData">
        /// The extra, entity-specific data to use for this callback, if any.
        /// This parameter may be null.
        /// </param>
        /// <param name="switchCount">
        /// The number of command-line switches processed so far.
        /// </param>
        /// <param name="arg">
        /// The unknown argument that was encountered.  This parameter may be
        /// null.
        /// </param>
        /// <param name="whatIf">
        /// If true, the argument processing should be simulated only, without
        /// performing the underlying actions.
        /// </param>
        /// <param name="argv">
        /// The list of command-line arguments being processed.  Upon return,
        /// this may be modified.
        /// </param>
        /// <param name="result">
        /// Upon success, this may contain a result value.  Upon failure, this
        /// must contain an appropriate error message.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise, a non-Ok
        /// value with details placed in the <paramref name="result" />
        /// parameter.
        /// </returns>
        ReturnCode UnknownArgument(
            Interpreter interpreter, // TODO: Change to use IInterpreter type.
            IInteractiveHost interactiveHost,
            IClientData clientData,
            int switchCount,
            string arg,
            bool whatIf,
            ref IList<string> argv,
            ref Result result
        );

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method is called by the shell to evaluate a script.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter context the shell is running in.  This parameter
        /// should not be null.
        /// </param>
        /// <param name="text">
        /// The text of the script to be evaluated.  This parameter should not
        /// be null.
        /// </param>
        /// <param name="result">
        /// Upon success, this may contain the result of evaluating the
        /// script.  Upon failure, this must contain an appropriate error
        /// message.
        /// </param>
        /// <param name="errorLine">
        /// Upon failure, this may contain the line number where the error
        /// occurred.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise, a non-Ok
        /// value with details placed in the <paramref name="result" />
        /// parameter.
        /// </returns>
        ReturnCode EvaluateScript(
            Interpreter interpreter, // TODO: Change to use IInterpreter type.
            string text,
            ref Result result,
            ref int errorLine
        );

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method is called by the shell to evaluate a script contained
        /// in a file.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter context the shell is running in.  This parameter
        /// should not be null.
        /// </param>
        /// <param name="fileName">
        /// The name of the file containing the script to be evaluated.  This
        /// parameter should not be null.
        /// </param>
        /// <param name="result">
        /// Upon success, this may contain the result of evaluating the
        /// script.  Upon failure, this must contain an appropriate error
        /// message.
        /// </param>
        /// <param name="errorLine">
        /// Upon failure, this may contain the line number where the error
        /// occurred.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise, a non-Ok
        /// value with details placed in the <paramref name="result" />
        /// parameter.
        /// </returns>
        ReturnCode EvaluateFile(
            Interpreter interpreter, // TODO: Change to use IInterpreter type.
            string fileName,
            ref Result result,
            ref int errorLine
        );

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method is called by the shell to evaluate a script contained
        /// in a file, using the specified character encoding.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter context the shell is running in.  This parameter
        /// should not be null.
        /// </param>
        /// <param name="encoding">
        /// The character encoding to use when reading the file, if any.  This
        /// parameter may be null.
        /// </param>
        /// <param name="fileName">
        /// The name of the file containing the script to be evaluated.  This
        /// parameter should not be null.
        /// </param>
        /// <param name="result">
        /// Upon success, this may contain the result of evaluating the
        /// script.  Upon failure, this must contain an appropriate error
        /// message.
        /// </param>
        /// <param name="errorLine">
        /// Upon failure, this may contain the line number where the error
        /// occurred.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise, a non-Ok
        /// value with details placed in the <paramref name="result" />
        /// parameter.
        /// </returns>
        ReturnCode EvaluateEncodedFile(
            Interpreter interpreter, // TODO: Change to use IInterpreter type.
            Encoding encoding,
            string fileName,
            ref Result result,
            ref int errorLine
        );
    }
}
