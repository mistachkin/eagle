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
    [ObjectId("dd2bb49e-1140-4461-bbb1-5c0febdf95c8")]
    internal static class ResultOps
    {
        #region Private Constants
        #region Formatting
        private static readonly string emptyFormat = String.Empty;

        private const string codeOnlyFormat = "{0}{1}";
        private const string resultOnlyFormat = "{0}{2}";
        private const string codeAndResultFormat = "{0}{1}: {2}";
        private const string codeAndErrorLineFormat = "{0}{1}, line {3}";
        private const string codeResultAndErrorLineFormat = "{0}{1}, line {3}: {2}";
        #endregion
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Return / Exit Code Handling Methods
        public static bool IsOkOrReturn(
            ReturnCode code
            )
        {
            return ((code == ReturnCode.Ok) || (code == ReturnCode.Return));
        }

        ///////////////////////////////////////////////////////////////////////

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

        public static ReturnCode CustomOkCode(uint value)
        {
            //
            // NOTE: These are always considered as "success codes".
            //
            return (ReturnCode.CustomOk | (ReturnCode)value);
        }

        ///////////////////////////////////////////////////////////////////////

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
        public static ExitCode SuccessExitCode()
        {
            return ExitCode.Success;
        }

        ///////////////////////////////////////////////////////////////////////

        //
        // HACK: Avoid "ExitCode" enumeration / property name collision.
        //
        public static ExitCode FailureExitCode()
        {
            return ExitCode.Failure;
        }

        ///////////////////////////////////////////////////////////////////////

        //
        // HACK: Avoid "ExitCode" enumeration / property name collision.
        //
        public static ExitCode ExceptionExitCode()
        {
            return ExitCode.Exception;
        }

        ///////////////////////////////////////////////////////////////////////

        //
        // HACK: Avoid "ExitCode" enumeration / property name collision.
        //
        public static ExitCode UnknownExitCode()
        {
            return ExitCode.Unknown;
        }

        ///////////////////////////////////////////////////////////////////////

        public static ReturnCode ExitCodeToReturnCode(
            ExitCode exitCode
            )
        {
            return (exitCode == SuccessExitCode()) ?
                ReturnCode.Ok : ReturnCode.Error;
        }

        ///////////////////////////////////////////////////////////////////////

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
        public static string Format(
            ReturnCode code,
            Result result
            )
        {
            return Format(code, result, 0);
        }

        ///////////////////////////////////////////////////////////////////////

        public static string Format(
            ReturnCode code,
            Result result,
            int errorLine
            )
        {
            return Format(code, result, errorLine, false, false);
        }

        ///////////////////////////////////////////////////////////////////////

        public static string Format(
            ReturnCode code,
            Result result,
            int errorLine,
            bool exceptions,
            bool display
            )
        {
            return Format(null, code, result, errorLine, exceptions, display);
        }

        ///////////////////////////////////////////////////////////////////////

        public static string Format(
            string prefix,
            ReturnCode code,
            Result result,
            int errorLine,
            bool exceptions,
            bool display
            )
        {
            bool haveResult = !StringOps.IsLogicallyEmpty(result);
            bool haveErrorLine = (errorLine != 0);
            string format;

            if (IsSuccess(code, exceptions))
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
                format, prefix, code, result, errorLine);

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
