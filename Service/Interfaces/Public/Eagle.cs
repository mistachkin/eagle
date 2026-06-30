/*
 * Eagle.cs --
 *
 * Copyright (c) 2007-2012 by Joe Mistachkin.  All rights reserved.
 *
 * See the file "license.terms" for information on usage and redistribution of
 * this file, and for a DISCLAIMER OF ALL WARRANTIES.
 *
 * RCS: @(#) $Id: $
 */

using System.Collections.ObjectModel;
using System.Web.Services;
using Eagle._Attributes;
using Eagle._Components.Public;

namespace Eagle._Services
{
    /// <summary>
    /// This interface defines the web service contract exposed by the Eagle
    /// service.  It provides methods for evaluating expressions, scripts, and
    /// files, for performing command substitution, and for inspecting and
    /// formatting the results of those operations.
    /// </summary>
    [WebServiceBinding("IEagle")]
    [ObjectId("3c5bcb95-95b8-4402-9713-f0a2497a40be")]
    public interface IEagle
    {
        /// <summary>
        /// This method evaluates the specified text as an Eagle expression.
        /// </summary>
        /// <param name="text">
        /// The text of the expression to evaluate.
        /// </param>
        /// <returns>
        /// The result of evaluating the expression, including its return code
        /// and value (or error).
        /// </returns>
        [WebMethod()]
        MethodResult EvaluateExpression(string text);

        /// <summary>
        /// This method evaluates the specified text as an Eagle expression,
        /// using the specified command-line arguments.
        /// </summary>
        /// <param name="text">
        /// The text of the expression to evaluate.
        /// </param>
        /// <param name="args">
        /// The command-line arguments to make available during evaluation.
        /// </param>
        /// <returns>
        /// The result of evaluating the expression, including its return code
        /// and value (or error).
        /// </returns>
        [WebMethod()]
        MethodResult EvaluateExpressionWithArgs(string text,
            Collection<string> args);

        /// <summary>
        /// This method evaluates the specified text as an Eagle script.
        /// </summary>
        /// <param name="text">
        /// The text of the script to evaluate.
        /// </param>
        /// <returns>
        /// The result of evaluating the script, including its return code and
        /// value (or error).
        /// </returns>
        [WebMethod()]
        MethodResult EvaluateScript(string text);

        /// <summary>
        /// This method evaluates the specified text as an Eagle script, using
        /// the specified command-line arguments.
        /// </summary>
        /// <param name="text">
        /// The text of the script to evaluate.
        /// </param>
        /// <param name="args">
        /// The command-line arguments to make available during evaluation.
        /// </param>
        /// <returns>
        /// The result of evaluating the script, including its return code and
        /// value (or error).
        /// </returns>
        [WebMethod()]
        MethodResult EvaluateScriptWithArgs(string text,
            Collection<string> args);

        /// <summary>
        /// This method evaluates the contents of the specified file as an
        /// Eagle script.
        /// </summary>
        /// <param name="fileName">
        /// The name of the file containing the script to evaluate.
        /// </param>
        /// <returns>
        /// The result of evaluating the file, including its return code and
        /// value (or error).
        /// </returns>
        [WebMethod()]
        MethodResult EvaluateFile(string fileName);

        /// <summary>
        /// This method evaluates the contents of the specified file as an
        /// Eagle script, using the specified command-line arguments.
        /// </summary>
        /// <param name="fileName">
        /// The name of the file containing the script to evaluate.
        /// </param>
        /// <param name="args">
        /// The command-line arguments to make available during evaluation.
        /// </param>
        /// <returns>
        /// The result of evaluating the file, including its return code and
        /// value (or error).
        /// </returns>
        [WebMethod()]
        MethodResult EvaluateFileWithArgs(string fileName,
            Collection<string> args);

        /// <summary>
        /// This method performs Eagle command substitution on the specified
        /// text.
        /// </summary>
        /// <param name="text">
        /// The text on which to perform command substitution.
        /// </param>
        /// <returns>
        /// The result of performing substitution, including its return code
        /// and value (or error).
        /// </returns>
        [WebMethod()]
        MethodResult SubstituteString(string text);

        /// <summary>
        /// This method performs Eagle command substitution on the specified
        /// text, using the specified command-line arguments.
        /// </summary>
        /// <param name="text">
        /// The text on which to perform command substitution.
        /// </param>
        /// <param name="args">
        /// The command-line arguments to make available during substitution.
        /// </param>
        /// <returns>
        /// The result of performing substitution, including its return code
        /// and value (or error).
        /// </returns>
        [WebMethod()]
        MethodResult SubstituteStringWithArgs(string text,
            Collection<string> args);

        /// <summary>
        /// This method performs Eagle command substitution on the contents of
        /// the specified file.
        /// </summary>
        /// <param name="fileName">
        /// The name of the file on which to perform command substitution.
        /// </param>
        /// <returns>
        /// The result of performing substitution, including its return code
        /// and value (or error).
        /// </returns>
        [WebMethod()]
        MethodResult SubstituteFile(string fileName);

        /// <summary>
        /// This method performs Eagle command substitution on the contents of
        /// the specified file, using the specified command-line arguments.
        /// </summary>
        /// <param name="fileName">
        /// The name of the file on which to perform command substitution.
        /// </param>
        /// <param name="args">
        /// The command-line arguments to make available during substitution.
        /// </param>
        /// <returns>
        /// The result of performing substitution, including its return code
        /// and value (or error).
        /// </returns>
        [WebMethod()]
        MethodResult SubstituteFileWithArgs(string fileName,
            Collection<string> args);

        /// <summary>
        /// This method determines whether the specified return code
        /// represents a successful outcome.
        /// </summary>
        /// <param name="code">
        /// The return code to evaluate.
        /// </param>
        /// <param name="exceptions">
        /// Non-zero if the control-flow return codes (e.g.
        /// <see cref="ReturnCode.Break" />,
        /// <see cref="ReturnCode.Continue" />, and
        /// <see cref="ReturnCode.Return" />) should also be considered
        /// successful.
        /// </param>
        /// <returns>
        /// True if the return code represents success; otherwise, false.
        /// </returns>
        [WebMethod()]
        bool IsSuccess(ReturnCode code, bool exceptions);

        /// <summary>
        /// This method formats a return code, result value, and error line
        /// into a single human-readable string.
        /// </summary>
        /// <param name="code">
        /// The return code to format.
        /// </param>
        /// <param name="result">
        /// The result value (or error message) to format.
        /// </param>
        /// <param name="errorLine">
        /// The line number where an error occurred, or zero if not
        /// applicable.
        /// </param>
        /// <returns>
        /// The formatted string representation of the result.
        /// </returns>
        [WebMethod()]
        string FormatResult(ReturnCode code, string result, int errorLine);

        /// <summary>
        /// This method formats the specified method result into a single
        /// human-readable string.
        /// </summary>
        /// <param name="result">
        /// The method result to format.
        /// </param>
        /// <returns>
        /// The formatted string representation of the method result.
        /// </returns>
        [WebMethod()]
        string FormatMethodResult(MethodResult result);
    }
}
