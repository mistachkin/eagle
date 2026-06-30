/*
 * ErrorManager.cs --
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
using Eagle._Containers.Public;

namespace Eagle._Interfaces.Public
{
    /// <summary>
    /// This interface is implemented by entities that manage the script error
    /// handling state of an interpreter, providing access to the current error
    /// line and operations to reset and copy the associated error information.
    /// </summary>
    [ObjectId("169da22e-94a6-4252-a7c1-92db09365dd0")]
    public interface IErrorManager
    {
        ///////////////////////////////////////////////////////////////////////
        // SCRIPT ERROR HANDLING
        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Gets or sets the script line number where the most recent error
        /// occurred.
        /// </summary>
        int ErrorLine { get; set; }

        // string ErrorCode { get; set; } // TODO: Maybe?
        // string ErrorInfo { get; set; } // TODO: Maybe?

        /// <summary>
        /// Resets the error information maintained by the interpreter.
        /// </summary>
        /// <param name="flags">
        /// The <see cref="VariableFlags" /> that control how the underlying
        /// error variables are accessed.
        /// </param>
        /// <param name="all">
        /// Non-zero to reset all error information; otherwise, only the
        /// primary error information is reset.
        /// </param>
        /// <param name="strict">
        /// Non-zero to treat missing or inaccessible error variables as a
        /// failure; otherwise, such conditions are ignored.
        /// </param>
        /// <param name="errors">
        /// Upon failure, this will receive the list of errors encountered
        /// while resetting the error information.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise, a non-Ok value
        /// with details placed in the <paramref name="errors" /> parameter.
        /// </returns>
        ReturnCode ResetErrorInformation(VariableFlags flags, bool all,
            bool strict, ref ResultList errors);

        /// <summary>
        /// Copies the current error information from the interpreter into the
        /// specified result.
        /// </summary>
        /// <param name="flags">
        /// The <see cref="VariableFlags" /> that control how the underlying
        /// error variables are accessed.
        /// </param>
        /// <param name="result">
        /// Upon success, this will receive the copied error information; upon
        /// failure, this will contain an appropriate error message.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise, a non-Ok value
        /// with details placed in the <paramref name="result" /> parameter.
        /// </returns>
        ReturnCode CopyErrorInformation(VariableFlags flags,
            ref Result result);

        /// <summary>
        /// Copies the current error information from the interpreter into the
        /// specified result.
        /// </summary>
        /// <param name="flags">
        /// The <see cref="VariableFlags" /> that control how the underlying
        /// error variables are accessed.
        /// </param>
        /// <param name="strict">
        /// Non-zero to treat missing or inaccessible error variables as a
        /// failure; otherwise, such conditions are ignored.
        /// </param>
        /// <param name="result">
        /// Upon success, this will receive the copied error information; upon
        /// failure, this will contain an appropriate error message.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise, a non-Ok value
        /// with details placed in the <paramref name="result" /> parameter.
        /// </returns>
        ReturnCode CopyErrorInformation(VariableFlags flags, bool strict,
            ref Result result);

        /// <summary>
        /// Copies the current error code and error information from the
        /// interpreter into the specified results.
        /// </summary>
        /// <param name="flags">
        /// The <see cref="VariableFlags" /> that control how the underlying
        /// error variables are accessed.
        /// </param>
        /// <param name="strict">
        /// Non-zero to treat missing or inaccessible error variables as a
        /// failure; otherwise, such conditions are ignored.
        /// </param>
        /// <param name="errorCode">
        /// Upon success, this will receive the copied error code (i.e. the
        /// value of the global <c>errorCode</c> variable).
        /// </param>
        /// <param name="errorInfo">
        /// Upon success, this will receive the copied error information (i.e.
        /// the value of the global <c>errorInfo</c> variable).
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise, a non-Ok value
        /// with details placed in the result parameters.
        /// </returns>
        ReturnCode CopyErrorInformation(VariableFlags flags, bool strict,
            ref Result errorCode, ref Result errorInfo);

        /// <summary>
        /// Copies the current error code and error information from the
        /// interpreter into the specified results.
        /// </summary>
        /// <param name="flags">
        /// The <see cref="VariableFlags" /> that control how the underlying
        /// error variables are accessed.
        /// </param>
        /// <param name="strict">
        /// Non-zero to treat missing or inaccessible error variables as a
        /// failure; otherwise, such conditions are ignored.
        /// </param>
        /// <param name="errorCode">
        /// Upon success, this will receive the copied error code (i.e. the
        /// value of the global <c>errorCode</c> variable).
        /// </param>
        /// <param name="errorInfo">
        /// Upon success, this will receive the copied error information (i.e.
        /// the value of the global <c>errorInfo</c> variable).
        /// </param>
        /// <param name="errors">
        /// Upon failure, this will receive the list of errors encountered
        /// while copying the error information.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise, a non-Ok value
        /// with details placed in the result parameters.
        /// </returns>
        ReturnCode CopyErrorInformation(VariableFlags flags, bool strict,
            ref Result errorCode, ref Result errorInfo, ref ResultList errors);
    }
}
