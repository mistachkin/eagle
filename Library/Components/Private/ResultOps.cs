/*
 * ResultOps.cs --
 *
 * Copyright (c) 2007-2012 by Joe Mistachkin.  All rights reserved.
 *
 * See the file "license.terms" for information on usage and redistribution of
 * this file, and for a DISCLAIMER OF ALL WARRANTIES.
 *
 * RCS: @(#) $Id: $
 */

using System;
using System.Threading;
using Eagle._Attributes;
using Eagle._Components.Public;
using Eagle._Containers.Public;
using Eagle._Interfaces.Public;

namespace Eagle._Components.Private
{
    /// <summary>
    /// This class provides static helper methods for working with operation
    /// results and return codes, including success and exit code translation,
    /// formatting of results for display, and support for synchronized
    /// (cross-thread) result delivery.
    /// </summary>
    [ObjectId("dd2bb49e-1140-4461-bbb1-5c0febdf95c8")]
    internal static class ResultOps
    {
        #region Private Constants
        #region Formatting
        /// <summary>
        /// The format string used when there is nothing to format, yielding an
        /// empty string.
        /// </summary>
        private static readonly string emptyFormat = String.Empty;

        /// <summary>
        /// The format string used to format a return code only.
        /// </summary>
        private const string codeOnlyFormat = "{0}{1}";
        /// <summary>
        /// The format string used to format a result value only.
        /// </summary>
        private const string resultOnlyFormat = "{0}{2}";
        /// <summary>
        /// The format string used to format a return code together with a
        /// result value.
        /// </summary>
        private const string codeAndResultFormat = "{0}{1}: {2}";
        /// <summary>
        /// The format string used to format a return code together with an
        /// error line number.
        /// </summary>
        private const string codeAndErrorLineFormat = "{0}{1}, line {3}";
        /// <summary>
        /// The format string used to format a return code, error line number,
        /// and result value.
        /// </summary>
        private const string codeResultAndErrorLineFormat = "{0}{1}, line {3}: {2}";
        #endregion
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Return / Exit Code Handling Methods
        /// <summary>
        /// Determines whether the specified return code is
        /// <see cref="ReturnCode.Ok" /> or <see cref="ReturnCode.Return" />.
        /// </summary>
        /// <param name="code">
        /// The return code to check.
        /// </param>
        /// <returns>
        /// True if the return code is <see cref="ReturnCode.Ok" /> or
        /// <see cref="ReturnCode.Return" />; otherwise, false.
        /// </returns>
        public static bool IsOkOrReturn(
            ReturnCode code
            )
        {
            return ((code == ReturnCode.Ok) || (code == ReturnCode.Return));
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Determines whether the specified return code represents success,
        /// taking into account whether exceptions are being treated as
        /// failures.
        /// </summary>
        /// <param name="code">
        /// The return code to check.
        /// </param>
        /// <param name="exceptions">
        /// Non-zero to treat any return code other than
        /// <see cref="ReturnCode.Error" /> (and not flagged as a custom error)
        /// as success; zero to treat only <see cref="ReturnCode.Ok" /> (or a
        /// custom-ok code) as success.
        /// </param>
        /// <returns>
        /// True if the return code represents success; otherwise, false.
        /// </returns>
        public static bool IsSuccess(
            ReturnCode code,
            bool exceptions
            )
        {
            if (exceptions)
            {
                return ((code != ReturnCode.Error) &&
                        ((code & ReturnCode.CustomError) == 0));
            }
            else
            {
                return ((code == ReturnCode.Ok) ||
                        ((code & ReturnCode.CustomOk) != 0));
            }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Builds a custom success return code from the specified value by
        /// combining it with the <see cref="ReturnCode.CustomOk" /> flag.
        /// </summary>
        /// <param name="value">
        /// The custom value to combine with the success flag.
        /// </param>
        /// <returns>
        /// The custom success return code.
        /// </returns>
        public static ReturnCode CustomOkCode(uint value)
        {
            //
            // NOTE: These are always considered as "success codes".
            //
            return (ReturnCode.CustomOk | (ReturnCode)value);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Builds a custom failure return code from the specified value by
        /// combining it with the <see cref="ReturnCode.CustomError" /> flag.
        /// </summary>
        /// <param name="value">
        /// The custom value to combine with the failure flag.
        /// </param>
        /// <returns>
        /// The custom failure return code.
        /// </returns>
        public static ReturnCode CustomErrorCode(uint value)
        {
            //
            // NOTE: These are always considered as "failure codes".
            //
            return (ReturnCode.CustomError | (ReturnCode)value);
        }

        ///////////////////////////////////////////////////////////////////////

        //
        // HACK: Avoid "ExitCode" enumeration / property name collision.
        //
        /// <summary>
        /// Gets the exit code that represents successful completion.
        /// </summary>
        /// <returns>
        /// The <see cref="ExitCode.Success" /> exit code.
        /// </returns>
        public static ExitCode SuccessExitCode()
        {
            return ExitCode.Success;
        }

        ///////////////////////////////////////////////////////////////////////

        //
        // HACK: Avoid "ExitCode" enumeration / property name collision.
        //
        /// <summary>
        /// Gets the exit code that represents failure.
        /// </summary>
        /// <returns>
        /// The <see cref="ExitCode.Failure" /> exit code.
        /// </returns>
        public static ExitCode FailureExitCode()
        {
            return ExitCode.Failure;
        }

        ///////////////////////////////////////////////////////////////////////

        //
        // HACK: Avoid "ExitCode" enumeration / property name collision.
        //
        /// <summary>
        /// Gets the exit code that represents termination due to an exception.
        /// </summary>
        /// <returns>
        /// The <see cref="ExitCode.Exception" /> exit code.
        /// </returns>
        public static ExitCode ExceptionExitCode()
        {
            return ExitCode.Exception;
        }

        ///////////////////////////////////////////////////////////////////////

        //
        // HACK: Avoid "ExitCode" enumeration / property name collision.
        //
        /// <summary>
        /// Gets the exit code that represents an unknown result.
        /// </summary>
        /// <returns>
        /// The <see cref="ExitCode.Unknown" /> exit code.
        /// </returns>
        public static ExitCode UnknownExitCode()
        {
            return ExitCode.Unknown;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Translates the specified exit code into the corresponding return
        /// code.
        /// </summary>
        /// <param name="exitCode">
        /// The exit code to translate.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> if the exit code represents success;
        /// otherwise, <see cref="ReturnCode.Error" />.
        /// </returns>
        public static ReturnCode ExitCodeToReturnCode(
            ExitCode exitCode
            )
        {
            return (exitCode == SuccessExitCode()) ?
                ReturnCode.Ok : ReturnCode.Error;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Translates the specified return code into the corresponding exit
        /// code.
        /// </summary>
        /// <param name="code">
        /// The return code to translate.
        /// </param>
        /// <param name="exceptions">
        /// Non-zero to use exception-aware success semantics when determining
        /// whether the return code represents success.
        /// </param>
        /// <returns>
        /// The success exit code if the return code represents success;
        /// otherwise, the failure exit code.
        /// </returns>
        public static ExitCode ReturnCodeToExitCode(
            ReturnCode code,
            bool exceptions
            )
        {
            return IsSuccess(code, exceptions) ?
                SuccessExitCode() : FailureExitCode();
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Result Handling Methods
        #region Synchronized Methods
        /// <summary>
        /// Creates a result that can be used to deliver an operation result
        /// across threads, backed by a newly created event wait handle stored
        /// in its client data.
        /// </summary>
        /// <param name="name">
        /// The name to assign to the underlying event wait handle.  This
        /// parameter may be null.
        /// </param>
        /// <returns>
        /// The newly created synchronized result.
        /// </returns>
        public static Result CreateSynchronized(
            string name
            )
        {
            Result synchronizedResult = String.Empty; /* FORCE VALID */

            synchronizedResult.ClientData =
                new ClientData(ThreadOps.CreateEvent(name));

            return synchronizedResult;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Cleans up a synchronized result by closing the underlying event
        /// wait handle stored in its client data, if any.
        /// </summary>
        /// <param name="synchronizedResult">
        /// The synchronized result to clean up.  This parameter may be null.
        /// </param>
        public static void CleanupSynchronized(
            Result synchronizedResult
            )
        {
            if (synchronizedResult != null)
            {
                lock (synchronizedResult)
                {
                    //
                    // NOTE: Grab the client data (the event wait handle).
                    //
                    IClientData clientData = synchronizedResult.ClientData;

                    if (clientData != null)
                    {
                        EventWaitHandle @event =
                            clientData.Data as EventWaitHandle;

                        if (@event != null)
                            ThreadOps.CloseEvent(ref @event);
                    }
                }
            }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Waits, indefinitely, for the event wait handle associated with the
        /// specified synchronized result to be signaled.
        /// </summary>
        /// <param name="synchronizedResult">
        /// The synchronized result whose event wait handle should be waited on.
        /// This parameter may be null.
        /// </param>
        /// <returns>
        /// True if the event was signaled; otherwise, false.
        /// </returns>
        public static bool WaitSynchronized(
            Result synchronizedResult
            )
        {
            EventWaitHandle @event = null;

            if (synchronizedResult != null)
            {
                lock (synchronizedResult)
                {
                    //
                    // NOTE: Grab the client data (the event wait handle).
                    //
                    IClientData clientData = synchronizedResult.ClientData;

                    if (clientData != null)
                        @event = clientData.Data as EventWaitHandle;
                }
            }

            if (@event != null)
                return ThreadOps.WaitEvent(@event);

            return false;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Waits, up to the specified timeout, for the event wait handle
        /// associated with the specified synchronized result to be signaled.
        /// </summary>
        /// <param name="synchronizedResult">
        /// The synchronized result whose event wait handle should be waited on.
        /// This parameter may be null.
        /// </param>
        /// <param name="timeout">
        /// The maximum amount of time, in milliseconds, to wait.
        /// </param>
        /// <returns>
        /// True if the event was signaled before the timeout elapsed;
        /// otherwise, false.
        /// </returns>
        public static bool WaitSynchronized(
            Result synchronizedResult,
            int timeout
            )
        {
            EventWaitHandle @event = null;

            if (synchronizedResult != null)
            {
                lock (synchronizedResult)
                {
                    //
                    // NOTE: Grab the client data (the event wait handle).
                    //
                    IClientData clientData = synchronizedResult.ClientData;

                    if (clientData != null)
                        @event = clientData.Data as EventWaitHandle;
                }
            }

            if (@event != null)
                return ThreadOps.WaitEvent(@event, timeout);

            return false;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Stores the supplied return code and result into the specified
        /// synchronized result and signals its event wait handle, if any, to
        /// notify a waiting thread that the result is available.
        /// </summary>
        /// <param name="synchronizedResult">
        /// The synchronized result to update and signal.  This parameter may be
        /// null.
        /// </param>
        /// <param name="code">
        /// The return code to store.
        /// </param>
        /// <param name="result">
        /// The result value to store.  This parameter may be null.
        /// </param>
        public static void SetSynchronized(
            Result synchronizedResult,
            ReturnCode code,
            Result result
            )
        {
            //
            // NOTE: Does the caller want to be notified about the result?
            //
            if (synchronizedResult != null)
            {
                lock (synchronizedResult)
                {
                    //
                    // NOTE: Grab the original client data (the event wait
                    //       handle).
                    //
                    IClientData clientData = synchronizedResult.ClientData;

                    //
                    // NOTE: Set the new client data (the result pair).
                    //
                    synchronizedResult.ClientData = new ClientData(
                        new AnyPair<ReturnCode, Result>(code, result));

                    //
                    // NOTE: If the original client data is valid and can be
                    //       cast to an event wait handle, signal it now.
                    //
                    if (clientData != null)
                    {
                        EventWaitHandle @event =
                            clientData.Data as EventWaitHandle;

                        if (@event != null)
                        {
                            /* IGNORED */
                            ThreadOps.SetEvent(@event);
                        }
                    }
                }
            }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Retrieves the return code and result value previously stored into
        /// the specified synchronized result.
        /// </summary>
        /// <param name="synchronizedResult">
        /// The synchronized result to read from.  This parameter may be null.
        /// </param>
        /// <param name="code">
        /// Upon success, set to the stored return code.
        /// </param>
        /// <param name="result">
        /// Upon success, set to the stored result value.
        /// </param>
        /// <param name="error">
        /// Upon failure, set to an error message describing the problem.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise,
        /// <see cref="ReturnCode.Error" />.
        /// </returns>
        public static ReturnCode GetSynchronized(
            Result synchronizedResult,
            ref ReturnCode code,
            ref Result result,
            ref Result error
            )
        {
            if (synchronizedResult != null)
            {
                lock (synchronizedResult)
                {
                    IClientData clientData = synchronizedResult.ClientData;

                    if (clientData != null)
                    {
                        IAnyPair<ReturnCode, Result> anyPair =
                            clientData.Data as IAnyPair<ReturnCode, Result>;

                        if (anyPair != null)
                        {
                            result = anyPair.Y;
                            code = anyPair.X;

                            return ReturnCode.Ok;
                        }
                        else
                        {
                            error = "synchronized result clientData is not a pair";
                        }
                    }
                    else
                    {
                        error = "invalid synchronized result clientData";
                    }
                }
            }
            else
            {
                error = "invalid synchronized result";
            }

            return ReturnCode.Error;
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Formatting Methods
        /// <summary>
        /// Formats the specified return code and result value into a single
        /// string.
        /// </summary>
        /// <param name="code">
        /// The return code to format.  This parameter may be null.
        /// </param>
        /// <param name="result">
        /// The result value to format.  This parameter may be null.
        /// </param>
        /// <returns>
        /// The formatted string.
        /// </returns>
        public static string Format(
            ReturnCode? code,
            Result result
            )
        {
            return Format(code, result, 0);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Formats the specified return code and result value into a single
        /// string, optionally preparing it for display.
        /// </summary>
        /// <param name="code">
        /// The return code to format.  This parameter may be null.
        /// </param>
        /// <param name="result">
        /// The result value to format.  This parameter may be null.
        /// </param>
        /// <param name="display">
        /// Non-zero to further process the formatted string for display
        /// purposes.
        /// </param>
        /// <returns>
        /// The formatted string.
        /// </returns>
        public static string Format(
            ReturnCode? code,
            Result result,
            bool display
            )
        {
            return Format(code, result, 0, false, display);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Formats the specified return code, result value, and error line
        /// number into a single string.
        /// </summary>
        /// <param name="code">
        /// The return code to format.  This parameter may be null.
        /// </param>
        /// <param name="result">
        /// The result value to format.  This parameter may be null.
        /// </param>
        /// <param name="errorLine">
        /// The error line number to format, or zero if none.
        /// </param>
        /// <returns>
        /// The formatted string.
        /// </returns>
        public static string Format(
            ReturnCode? code,
            Result result,
            int errorLine
            )
        {
            return Format(code, result, errorLine, false, false);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Formats the specified return code, result value, and error line
        /// number into a single string, optionally using exception-aware
        /// success semantics and preparing the result for display.
        /// </summary>
        /// <param name="code">
        /// The return code to format.  This parameter may be null.
        /// </param>
        /// <param name="result">
        /// The result value to format.  This parameter may be null.
        /// </param>
        /// <param name="errorLine">
        /// The error line number to format, or zero if none.
        /// </param>
        /// <param name="exceptions">
        /// Non-zero to use exception-aware success semantics when deciding how
        /// the return code should be formatted.
        /// </param>
        /// <param name="display">
        /// Non-zero to further process the formatted string for display
        /// purposes.
        /// </param>
        /// <returns>
        /// The formatted string.
        /// </returns>
        public static string Format(
            ReturnCode? code,
            Result result,
            int errorLine,
            bool exceptions,
            bool display
            )
        {
            return Format(
                null, code, result, errorLine, exceptions, display);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Formats the specified prefix, return code, result value, and error
        /// line number into a single string, optionally using exception-aware
        /// success semantics and preparing the result for display.  This is the
        /// core implementation to which the other overloads delegate.
        /// </summary>
        /// <param name="prefix">
        /// The prefix to prepend to the formatted string.  This parameter may
        /// be null.
        /// </param>
        /// <param name="code">
        /// The return code to format.  This parameter may be null.
        /// </param>
        /// <param name="result">
        /// The result value to format.  This parameter may be null.
        /// </param>
        /// <param name="errorLine">
        /// The error line number to format, or zero if none.
        /// </param>
        /// <param name="exceptions">
        /// Non-zero to use exception-aware success semantics when deciding how
        /// the return code should be formatted.
        /// </param>
        /// <param name="display">
        /// Non-zero to further process the formatted string for display
        /// purposes.
        /// </param>
        /// <returns>
        /// The formatted string.
        /// </returns>
        public static string Format(
            string prefix,
            ReturnCode? code,
            Result result,
            int errorLine,
            bool exceptions,
            bool display
            )
        {
            bool haveResult = !StringOps.IsLogicallyEmpty(result);
            bool haveErrorLine = (errorLine != 0);
            string format;

            if ((code == null) ||
                IsSuccess((ReturnCode)code, exceptions))
            {
                if (haveResult)
                    format = resultOnlyFormat;
                else
                    format = emptyFormat;
            }
            else
            {
                if (haveResult)
                {
                    if (haveErrorLine)
                        format = codeResultAndErrorLineFormat;
                    else
                        format = codeAndResultFormat;
                }
                else
                {
                    if (haveErrorLine)
                        format = codeAndErrorLineFormat;
                    else
                        format = codeOnlyFormat;
                }
            }

            string formatted = String.Format(
                format, prefix, FormatOps.MaybeNull(code),
                result, errorLine);

            if (display)
            {
                formatted = FormatOps.DisplayResult(
                    formatted, false, false);
            }

            return formatted;
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region List Methods
        /// <summary>
        /// Combines the specified results into a single result list, skipping
        /// any that are null.
        /// </summary>
        /// <param name="results">
        /// The array of results to combine.  This parameter may be null.
        /// </param>
        /// <returns>
        /// A result containing the non-null results, or null if the supplied
        /// array was null.
        /// </returns>
        public static Result MaybeCombine(
            params Result[] results
            )
        {
            if (results == null)
                return null;

            ResultList localResults = new ResultList();

            foreach (Result result in results)
            {
                if (result == null)
                    continue;

                localResults.Add(result);
            }

            return localResults;
        }
        #endregion
        #endregion
    }
}
