/*
 * ProcessOps.cs --
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
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Security;
using System.Text;
using System.Threading;
using Eagle._Attributes;
using Eagle._Components.Public;
using Eagle._Constants;
using Eagle._Containers.Private;
using Eagle._Containers.Public;
using Eagle._Interfaces.Public;

#if NET_STANDARD_21
using Index = Eagle._Constants.Index;
#endif

namespace Eagle._Components.Private
{
    [ObjectId("41b56439-03b9-4a8d-932a-aca836c99823")]
    internal static class ProcessOps
    {
        #region Private Constants
        //
        // NOTE: There are two values in this format string.  The first
        //       value is the name prefix part of the final environment
        //       variable.  The second value is a (decimal radix) string
        //       representation of the integer identifier for the current
        //       (or parent) process.  Used together, they form the fully
        //       qualified environment variable name used to refer to the
        //       associated reference count value within the environment
        //       for the current (and child) process(es).
        //
        private static readonly string ReferenceCountEnvVarFormat = "{0}{1}";

        ///////////////////////////////////////////////////////////////////////

        //
        // HACK: This is purposely not read-only.
        //
        private static string ReferenceCountDefaultPrefix = "ReferenceCount";

        ///////////////////////////////////////////////////////////////////////

        //
        // HACK: This is purposely not read-only.
        //
        private static int? StringBuilderCapacity = null;
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Private Data
        private static readonly object syncRoot = new object();

        ///////////////////////////////////////////////////////////////////////

        //
        // HACK: This is purposely not read-only.
        //
        // NOTE: This field controls (the level of) checking performed
        //       on the data received via the "OutputDataReceived" and
        //       "ErrorDataReceived" process events.  The following
        //       values are supported:
        //
        //       0: No checking is performed.  This is the default.
        //
        //       1. The strings are checked to make sure they are
        //          not null or empty.
        //
        //       2: The strings are checked to see if they contain
        //          only spaces.
        //
        //       3: The strings are checked to see if they contain
        //          character values above the visible ASCII range
        //          of 0x7E.
        //
        //       4: The strings are checked to see if they contain
        //          character values below the visible ASCII range
        //          of 0x20.
        //
        // WARNING: Any value other than zero here is potentially
        //          very expensive in terms of compute time.  You
        //          have been warned.
        //
        private static int DataReceivedCheckLevel = 0;

        ///////////////////////////////////////////////////////////////////////

        private static ProcessStringBuilderDictionary standardOutputs = null;
        private static ProcessStringBuilderDictionary standardErrors = null;

        ///////////////////////////////////////////////////////////////////////

        private static ProcessDataReceivedEventHandlerDictionary outputHandlers = null;
        private static ProcessDataReceivedEventHandlerDictionary errorHandlers = null;
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Public Introspection Support Methods
        public static void AddInfo(
            StringPairList list,    /* in */
            DetailFlags detailFlags /* in */
            )
        {
            if (list == null)
                return;

            lock (syncRoot) /* TRANSACTIONAL */
            {
                bool empty = HostOps.HasEmptyContent(detailFlags);
                StringPairList localList = new StringPairList();

                if (empty || ((standardOutputs != null) &&
                    (standardOutputs.Count > 0)))
                {
                    localList.Add("StandardOutputs",
                        (standardOutputs != null) ?
                            standardOutputs.Count.ToString() :
                            FormatOps.DisplayNull);
                }

                if (empty || ((standardErrors != null) &&
                    (standardErrors.Count > 0)))
                {
                    localList.Add("StandardErrors",
                        (standardErrors != null) ?
                            standardErrors.Count.ToString() :
                            FormatOps.DisplayNull);
                }

                if (empty || ((outputHandlers != null) &&
                    (outputHandlers.Count > 0)))
                {
                    localList.Add("OutputHandlers",
                        (outputHandlers != null) ?
                            outputHandlers.Count.ToString() :
                            FormatOps.DisplayNull);
                }

                if (empty || ((errorHandlers != null) &&
                    (errorHandlers.Count > 0)))
                {
                    localList.Add("ErrorHandlers",
                        (errorHandlers != null) ?
                            errorHandlers.Count.ToString() :
                            FormatOps.DisplayNull);
                }

                if (localList.Count > 0)
                {
                    list.Add((IPair<string>)null);
                    list.Add("Process Information");
                    list.Add((IPair<string>)null);
                    list.Add(localList);
                }
            }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Process Reference Count Support Methods
        public static string GetEnvironmentVariable(
            string prefix, /* in: OPTIONAL */
            long processId /* in */
            )
        {
            if (prefix == null)
                prefix = ReferenceCountDefaultPrefix;

            return String.Format(
                ReferenceCountEnvVarFormat, prefix, processId);
        }

        ///////////////////////////////////////////////////////////////////////

        private static void GetEnvironmentVariableAndValue(
            string prefix,       /* in: OPTIONAL */
            out string variable, /* out */
            out string value     /* out */
            )
        {
            long parentProcessId = GetParentId();
            long currentProcessId = GetId();

            foreach (long processId in new long[] {
                    parentProcessId, currentProcessId })
            {
                if (processId == 0)
                    continue;

                string localVariable = GetEnvironmentVariable(
                    prefix, processId);

                if (String.IsNullOrEmpty(localVariable))
                    continue;

                string localValue = null;

                if (CommonOps.Environment.DoesVariableExist(
                        localVariable, ref localValue))
                {
                    variable = localVariable;
                    value = localValue;

                    return;
                }
            }

            //
            // NOTE: Always fallback to the console reference count
            //       environment variable for the current process.
            //
            //       This is the common case as there is not normally
            //       a parent process that is also using the console
            //       reference counting mechanism (which is specific
            //       to the Eagle core library).
            //
            variable = GetEnvironmentVariable(prefix, currentProcessId);
            value = null;
        }

        ///////////////////////////////////////////////////////////////////////

        private static bool TryGetReferenceCount(
            string value,            /* in */
            CultureInfo cultureInfo, /* in */
            out int referenceCount,  /* out */
            ref Result error         /* out */
            )
        {
            referenceCount = 0;

            if ((value == null) || Value.GetInteger2(
                    value, ValueFlags.AnyInteger, cultureInfo,
                    ref referenceCount, ref error) == ReturnCode.Ok)
            {
                return true;
            }

            return false;
        }

        ///////////////////////////////////////////////////////////////////////

        public static ReturnCode CheckAndMaybeModifyReferenceCount(
            string prefix,           /* in: OPTIONAL */
            CultureInfo cultureInfo, /* in */
            bool? increment,         /* in: OPTIONAL */
            out int referenceCount,  /* out */
            ref Result error         /* out */
            )
        {
            referenceCount = 0;

            try
            {
                string variable;
                string value;

                GetEnvironmentVariableAndValue(
                    prefix, out variable, out value);

                if (String.IsNullOrEmpty(variable))
                {
                    error = "invalid environment variable name";
                    return ReturnCode.Error;
                }

                int localReferenceCount;

                if (!TryGetReferenceCount(
                        value, cultureInfo, out localReferenceCount,
                        ref error))
                {
                    return ReturnCode.Error;
                }

                if (increment != null)
                {
                    if ((bool)increment)
                        localReferenceCount++;
                    else
                        localReferenceCount--;

                    if (localReferenceCount > 0)
                    {
                        if (!CommonOps.Environment.SetVariable(
                                variable, localReferenceCount.ToString()))
                        {
                            error = "could not set environment variable";
                            return ReturnCode.Error;
                        }
                    }
                    else
                    {
                        if (!CommonOps.Environment.UnsetVariable(variable))
                        {
                            error = "could not unset environment variable";
                            return ReturnCode.Error;
                        }
                    }
                }

                referenceCount = localReferenceCount;
                return ReturnCode.Ok;
            }
            catch (Exception e)
            {
                error = e;
            }

            return ReturnCode.Error;
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Captured Output & Handler Lifecycle Methods
        private static void InitializeStandardOutputsAndErrors()
        {
            lock (syncRoot) /* TRANSACTIONAL */
            {
                if (standardOutputs == null)
                    standardOutputs = new ProcessStringBuilderDictionary();

                if (standardErrors == null)
                    standardErrors = new ProcessStringBuilderDictionary();
            }
        }

        ///////////////////////////////////////////////////////////////////////

        private static void InitializeOutputAndErrorHandlers()
        {
            lock (syncRoot) /* TRANSACTIONAL */
            {
                if (outputHandlers == null)
                    outputHandlers = new ProcessDataReceivedEventHandlerDictionary();

                if (errorHandlers == null)
                    errorHandlers = new ProcessDataReceivedEventHandlerDictionary();
            }
        }

        ///////////////////////////////////////////////////////////////////////

        private static int ClearStandardOutputsAndErrors()
        {
            lock (syncRoot) /* TRANSACTIONAL */
            {
                int result = 0;

                if (standardOutputs != null)
                {
                    result += standardOutputs.Count;

                    standardOutputs.Clear();
                    standardOutputs = null;
                }

                if (standardErrors != null)
                {
                    result += standardErrors.Count;

                    standardErrors.Clear();
                    standardErrors = null;
                }

                return result;
            }
        }

        ///////////////////////////////////////////////////////////////////////

        private static int ClearOutputAndErrorHandlers()
        {
            lock (syncRoot) /* TRANSACTIONAL */
            {
                int result = 0;

                if (outputHandlers != null)
                {
                    result += outputHandlers.Count;

                    outputHandlers.Clear();
                    outputHandlers = null;
                }

                if (errorHandlers != null)
                {
                    result += errorHandlers.Count;

                    errorHandlers.Clear();
                    errorHandlers = null;
                }

                return result;
            }
        }

        ///////////////////////////////////////////////////////////////////////

        public static int Cleanup()
        {
            lock (syncRoot) /* TRANSACTIONAL */
            {
                int result = 0;

                result += ClearStandardOutputsAndErrors();
                result += ClearOutputAndErrorHandlers();

                return result;
            }
        }

        ///////////////////////////////////////////////////////////////////////

        private static void MaybeCheckDataReceived(
            string methodName, /* in */
            string data        /* in */
            )
        {
            int level = Interlocked.CompareExchange(
                ref DataReceivedCheckLevel, 0, 0);

            if (level <= 0)
                return;

            Result error = null;

            if (!CheckDataReceived(data, level, ref error))
            {
                TraceOps.DebugTrace(String.Format(
                    "{0}: possibly bad data received: {1}", methodName,
                    FormatOps.WrapOrNull(error)), typeof(ProcessOps).Name,
                    TracePriority.ProcessError2);
            }
        }

        ///////////////////////////////////////////////////////////////////////

        private static DataReceivedEventHandler GetOutputHandler(
            Process process /* in */
            )
        {
            lock (syncRoot) /* TRANSACTIONAL */
            {
                DataReceivedEventHandler handler;

                if ((outputHandlers != null) &&
                    outputHandlers.TryGetValue(process, out handler))
                {
                    return handler;
                }
                else
                {
                    return null;
                }
            }
        }

        ///////////////////////////////////////////////////////////////////////

        private static DataReceivedEventHandler GetErrorHandler(
            Process process /* in */
            )
        {
            lock (syncRoot) /* TRANSACTIONAL */
            {
                DataReceivedEventHandler handler;

                if ((errorHandlers != null) &&
                    errorHandlers.TryGetValue(process, out handler))
                {
                    return handler;
                }
                else
                {
                    return null;
                }
            }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Captured Output & Handler Support Methods
        private static ReturnCode PreSetupCapturedOutput(
            ProcessStartInfo startInfo,             /* in */
            Process process,                        /* in */
            DataReceivedEventHandler outputHandler, /* in */
            DataReceivedEventHandler errorHandler,  /* in */
            int? capacity,                          /* in */
            bool overrideCapture,                   /* in */
            ref Result error                        /* out */
            )
        {
            if (startInfo == null)
            {
                error = "invalid process information";
                return ReturnCode.Error;
            }

            if (process == null)
            {
                error = "invalid process";
                return ReturnCode.Error;
            }

            bool success = true;

            try
            {
                //
                // NOTE: If necessary, setup the process normal output
                //       buffer.
                //
                if (startInfo.RedirectStandardOutput)
                {
                    lock (syncRoot) /* TRANSACTIONAL */
                    {
                        if ((standardOutputs != null) &&
                            !standardOutputs.NewData(process, capacity))
                        {
                            success = false;
                        }

                        if (success && (outputHandlers != null) &&
                            !overrideCapture && (outputHandler != null))
                        {
                            /* NO RESULT */
                            outputHandlers.Add(process, outputHandler);
                        }
                    }
                }

                //
                // NOTE: If necessary, setup the process error output
                //       buffer.
                //
                if (startInfo.RedirectStandardError)
                {
                    lock (syncRoot) /* TRANSACTIONAL */
                    {
                        if ((standardErrors != null) &&
                            !standardErrors.NewData(process, capacity))
                        {
                            success = false;
                        }

                        if (success && (errorHandlers != null) &&
                            !overrideCapture && (errorHandler != null))
                        {
                            /* NO RESULT */
                            errorHandlers.Add(process, errorHandler);
                        }
                    }
                }

                if (success)
                    return ReturnCode.Ok;
                else
                    error = "could not enable capture for process";
            }
            catch (Exception e)
            {
                error = e;
            }
            finally
            {
                if (!success)
                {
                    lock (syncRoot) /* TRANSACTIONAL */
                    {
                        if (errorHandlers != null)
                        {
                            /* IGNORED */
                            errorHandlers.Remove(process);
                        }

                        if (standardErrors != null)
                        {
                            /* IGNORED */
                            standardErrors.RemoveData(process);
                        }

                        if (outputHandlers != null)
                        {
                            /* IGNORED */
                            outputHandlers.Remove(process);
                        }

                        if (standardOutputs != null)
                        {
                            /* IGNORED */
                            standardOutputs.RemoveData(process);
                        }
                    }
                }
            }

            return ReturnCode.Error;
        }

        ///////////////////////////////////////////////////////////////////////

        private static ReturnCode PostSetupCapturedOutput(
            ProcessStartInfo startInfo, /* in */
            Process process,            /* in */
            string input,               /* in */
            IObject inputObject,        /* in, out */
            ref Result error            /* out */
            )
        {
            if (startInfo == null)
            {
                error = "invalid process information";
                return ReturnCode.Error;
            }

            if (process == null)
            {
                error = "invalid process";
                return ReturnCode.Error;
            }

            try
            {
                //
                // NOTE: If necessary, start capturing normal output from
                //       the process asynchronously.
                //
                if (startInfo.RedirectStandardOutput)
                    process.BeginOutputReadLine(); /* throw */

                //
                // NOTE: If necessary, start capturing error output from
                //       the process asynchronously.
                //
                if (startInfo.RedirectStandardError)
                    process.BeginErrorReadLine(); /* throw */

                //
                // NOTE: If requested and possible, write provided input
                //       string to the standard input stream for the
                //       started process.
                //
                if (startInfo.RedirectStandardInput)
                {
                    if ((input != null) || (inputObject != null))
                    {
                        StreamWriter standardInput = process.StandardInput;

                        if (input != null)
                        {
                            if (standardInput != null)
                            {
                                standardInput.Write(input); /* throw */
                                standardInput.Flush(); /* throw */
                            }
                        }
                        else if (inputObject != null)
                        {
                            inputObject.Value = standardInput;
                        }
                    }
                }

                return ReturnCode.Ok;
            }
            catch (Exception e)
            {
                error = e;
            }

            return ReturnCode.Error;
        }

        ///////////////////////////////////////////////////////////////////////

        private static ReturnCode GetCapturedOutput(
            ProcessStartInfo startInfo, /* in */
            Process process,            /* in */
            bool useShellExecute,       /* in */
            bool keepNewLine,           /* in */
            ref Result result,          /* out */
            ref Result error            /* out */
            )
        {
            //
            // NOTE: If we used the shell, output from the child process is
            //       not available; otherwise, save it for later use by the
            //       caller.
            //
            if (useShellExecute)
            {
                result = null;
                error = null;

                return ReturnCode.Ok;
            }
            else
            {
                if (startInfo == null)
                {
                    error = "invalid process information";
                    return ReturnCode.Error;
                }

                if (process == null)
                {
                    error = "invalid process";
                    return ReturnCode.Error;
                }

                try
                {
                    string localOutput = null;

                    if (startInfo.RedirectStandardOutput)
                    {
                        lock (syncRoot) /* TRANSACTIONAL */
                        {
                            if (standardOutputs != null)
                            {
                                localOutput = standardOutputs.GetData(
                                    process);
                            }
                        }
                    }

                    string localError = null;

                    if (startInfo.RedirectStandardError)
                    {
                        lock (syncRoot) /* TRANSACTIONAL */
                        {
                            if (standardErrors != null)
                            {
                                localError = standardErrors.GetData(
                                    process);
                            }
                        }
                    }

                    //
                    // NOTE: Remove final (trailing) newline sequence,
                    //       if any?
                    //
                    // COMPAT: Tcl.
                    //
                    if (!keepNewLine)
                    {
                        StringOps.StripNewLine(ref localOutput);
                        StringOps.StripNewLine(ref localError);
                    }

                    result = localOutput;
                    error = localError;

                    return ReturnCode.Ok;
                }
                catch (Exception e)
                {
                    error = e;
                }
            }

            return ReturnCode.Error;
        }

        ///////////////////////////////////////////////////////////////////////

        private static ReturnCode TerminateCapturedOutput(
            ProcessStartInfo startInfo, /* in */
            Process process,            /* in */
            ref Result error            /* out */
            )
        {
            if (startInfo == null)
            {
                error = "invalid process information";
                return ReturnCode.Error;
            }

            if (process == null)
            {
                error = "invalid process";
                return ReturnCode.Error;
            }

            try
            {
                if (startInfo.RedirectStandardOutput)
                {
                    lock (syncRoot) /* TRANSACTIONAL */
                    {
                        if (standardOutputs != null)
                        {
                            /* IGNORED */
                            standardOutputs.RemoveData(process);
                        }

                        if (outputHandlers != null)
                        {
                            /* IGNORED */
                            outputHandlers.Remove(process);
                        }
                    }
                }

                if (startInfo.RedirectStandardError)
                {
                    lock (syncRoot) /* TRANSACTIONAL */
                    {
                        if (standardErrors != null)
                        {
                            /* IGNORED */
                            standardErrors.RemoveData(process);
                        }

                        if (errorHandlers != null)
                        {
                            /* IGNORED */
                            errorHandlers.Remove(process);
                        }
                    }
                }

                return ReturnCode.Ok;
            }
            catch (Exception e)
            {
                error = e;
            }

            return ReturnCode.Error;
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Process Information Support Methods
#if SHELL
        public static bool IsCurrent(
            Process process
            )
        {
            if (process == null)
                return false;

            Process currentProcess = GetCurrent();

            if (currentProcess == null)
                return false;

            if (Object.ReferenceEquals(process, currentProcess))
                return true;

            long currentProcessId = currentProcess.Id;

            if (process.Id == currentProcessId)
                return true;

            return false;
        }
#endif

        ///////////////////////////////////////////////////////////////////////

        public static Process GetCurrent()
        {
            return Process.GetCurrentProcess();
        }

        ///////////////////////////////////////////////////////////////////////

        public static IntPtr GetHandle()
        {
            Process process = GetCurrent();

            if (process == null)
                return IntPtr.Zero;

            return process.Handle;
        }

        ///////////////////////////////////////////////////////////////////////

        public static long GetId()
        {
            Process process = GetCurrent();

            if (process == null)
                return 0;

            return process.Id;
        }

        ///////////////////////////////////////////////////////////////////////

        public static long GetParentId()
        {
#if NATIVE
            return NativeOps.GetParentProcessId().ToInt64();
#else
            return 0;
#endif
        }

        ///////////////////////////////////////////////////////////////////////

        public static string GetFileName()
        {
            Process process = GetCurrent();

            if (process == null)
                return null;

            return GetFileName(process.Id);
        }

        ///////////////////////////////////////////////////////////////////////

        public static string GetFileName(
            long id /* in */
            )
        {
            Process process = null;

            try
            {
                process = Process.GetProcessById(
                    (int)id); /* throw */
            }
            catch (Exception e)
            {
                TraceOps.DebugTrace(
                    e, typeof(ProcessOps).Name,
                    TracePriority.PlatformError);
            }

            return PathOps.GetProcessMainModuleFileName(
                process, false);
        }

        ///////////////////////////////////////////////////////////////////////

        public static IEnumerable<ProcessModule> GetModules(
            Process process /* in */
            )
        {
            if (process == null)
                return null;

            ProcessModuleCollection modules = process.Modules;

            if (modules == null)
                return null;

            IList<ProcessModule> result = new List<ProcessModule>();

            foreach (ProcessModule module in modules)
            {
                if (module == null)
                    continue;

                result.Add(module);
            }

            return result;
        }

        ///////////////////////////////////////////////////////////////////////

        private static void GetNames(
            Process process,    /* in */
            ref string name,    /* out */
            ref string fileName /* out */
            )
        {
            if (process == null)
                return;

            try
            {
                name = process.ProcessName; /* throw */
            }
            catch (Exception e)
            {
                TraceOps.DebugTrace(
                    e, typeof(ProcessOps).Name,
                    TracePriority.PlatformError);
            }

            fileName = PathOps.GetProcessMainModuleFileName(
                process, false);
        }

        ///////////////////////////////////////////////////////////////////////

        private static ReturnCode IsSame(
            Process process, /* in */
            ref bool result, /* out */
            ref Result error /* out */
            )
        {
            return IsSame(
                process, GetCurrent(), ref result, ref error);
        }

        ///////////////////////////////////////////////////////////////////////

        private static ReturnCode IsSame(
            Process process1, /* in */
            Process process2, /* in */
            ref bool result,  /* out */
            ref Result error  /* out */
            )
        {
            if ((process1 != null) && (process2 != null))
            {
                if (Object.ReferenceEquals(process1, process2))
                {
                    result = true;
                    return ReturnCode.Ok;
                }

                Result localError; /* REUSED */
                long id1 = 0;

                localError = null;

                if (!TryGetId(process1, ref id1, ref localError))
                {
                    error = localError;
                    return ReturnCode.Error;
                }

                long id2 = 0;

                localError = null;

                if (!TryGetId(process2, ref id2, ref localError))
                {
                    error = localError;
                    return ReturnCode.Error;
                }

                result = (id1 == id2);
                return ReturnCode.Ok;
            }

            result = ((process1 == null) && (process2 == null));
            return ReturnCode.Ok;
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Private ExecuteProcess Helper Methods
        private static bool TryGetId(
            Process process, /* in */
            ref long id      /* out */
            )
        {
            Result error = null;

            if (TryGetId(process, ref id, ref error))
            {
                return true;
            }
            else
            {
                TraceOps.DebugTrace(String.Format(
                    "TryGetId: error = {0}", FormatOps.WrapOrNull(error)),
                    typeof(ProcessOps).Name, TracePriority.PlatformDebug2);

                return false;
            }
        }

        ///////////////////////////////////////////////////////////////////////

        private static bool TryGetId(
            Process process, /* in */
            ref long id,     /* out */
            ref Result error /* out */
            )
        {
            if (process != null)
            {
                try
                {
                    //
                    // NOTE: Did we actually start the process
                    //       or was it already running?  If the
                    //       process was already running, this
                    //       will throw an exception.
                    //
                    id = process.Id; /* throw */

                    return true;
                }
                catch (Exception e)
                {
                    error = e;
                }
            }

            return false;
        }

        ///////////////////////////////////////////////////////////////////////

        private static bool TryGetIdAndPassToInterpreter(
            Interpreter interpreter, /* in: OPTIONAL */
            Process process,         /* in */
            ref long id              /* out */
            )
        {
            //
            // NOTE: We never access the Id property of the process
            //       directly here because it can throw an exception.
            //       Instead, a helper method is used to "safely" get
            //       it and place the value in the caller's variable.
            //
            if (TryGetId(process, ref id))
            {
                //
                // NOTE: Set Id of the last process that was executed
                //       for the interpreter unless we do not have an
                //       interpreter or we did not actually start a
                //       new process.  In either case, just skip it.
                //       This (per-thread) Id will be available for
                //       any (script) events that get processed while
                //       waiting for the process to exit.
                //
                Interpreter.SetPreviousProcessId(interpreter, id);

                //
                // NOTE: The process Id was obtained and it should be
                //       non-zero.
                //
                return true;
            }

            return false;
        }

        ///////////////////////////////////////////////////////////////////////

        private static ProcessStartInfo CreateStartInfo(
            string domainName,              /* in: Domain name for the logon,
                                             *     if any. */
            string userName,                /* in: User name for the logon, if
                                             *     any. */
            SecureString password,          /* in: Password for the logon, if
                                             *     any. */
            string fileName,                /* in: Executable file for the new
                                             *     process. */
            string arguments,               /* in: Command line arguments for
                                             *     the new process, if any. */
            string workingDirectory,        /* in: Working directory for the
                                             *     new process, if any. */
            string input,                   /* in: Simulated input for the new
                                             *     process, if any. */
            IObject inputObject,            /* in: Opaque object handle where
                                             *     standard input stream should
                                             *     be stored. */
            ProcessWindowStyle windowStyle, /* in: Normal, minimized, etc. */
            bool useShellExecute,           /* in: Use ShellExecute instead of
                                             *     CreateProcess? */
            bool captureOutput,             /* in: Populate the result and
                                             *     error parameters with
                                             *     captured output and error
                                             *     information? */
            bool useUnicode,                /* in: Captured output from process
                                             *     will be Unicode? */
            bool ignoreStdErr,              /* in: True to not capture output
                                             *     to StdErr (COMPAT: Tcl). */
            bool background,                /* in: Prevent waiting on the child
                                             *     process to exit. */
            ref Result error                /* out: Error output -OR- message,
                                             *      if any. */
            )
        {
            //
            // NOTE: Create object to place child process creation parameters
            //       into and start populating it.
            //
            ProcessStartInfo startInfo = new ProcessStartInfo();

            //
            // NOTE: If requested (and applicable), set the domain name, user
            //       name, and password.  This will not be done when starting
            //       the process via the shell.
            //
            if (!useShellExecute)
            {
                if (domainName != null)
                    startInfo.Domain = domainName;

                if (userName != null)
                    startInfo.UserName = userName;

                if (password != null)
                    startInfo.Password = password;
            }

            //
            // NOTE: Set the file name and working directory.  At this point,
            //       these values should be normalized and reasonably well
            //       verified.
            //
            startInfo.FileName = fileName;
            startInfo.WorkingDirectory = workingDirectory;

            //
            // NOTE: If requested, reset the encodings for the standard output
            //       and error streams to Unicode (i.e. UTF-16).
            //
            if (useUnicode)
            {
                startInfo.StandardOutputEncoding = Encoding.Unicode;
                startInfo.StandardErrorEncoding = Encoding.Unicode;
            }

            //
            // NOTE: If they supplied command line arguments, use them.
            //
            if (!String.IsNullOrEmpty(arguments))
                startInfo.Arguments = arguments;

            //
            // NOTE: Do they want to execute the new process via the shell?  If
            //       so, that will prevent them from using some other features,
            //       like capturing the output from the child process.  Set the
            //       window style as well.
            //
            startInfo.UseShellExecute = useShellExecute;
            startInfo.WindowStyle = windowStyle;

            //
            // NOTE: Setup the necessary input/output redirection based on the
            //       other options they specified.
            //
            //       Do not want a background process using our standard input
            //       (not applicable when executing via the shell).
            //
            //       Output is only captured if we plan on using it later.  In
            //       that case, capture both the standard output and standard
            //       error channels from the child process for non-background
            //       processes.
            //
            startInfo.RedirectStandardInput = (!useShellExecute &&
                (background || (input != null) || (inputObject != null)));

            if (captureOutput)
            {
                startInfo.RedirectStandardOutput =
                    (!useShellExecute && !background);

                startInfo.RedirectStandardError =
                    (!ignoreStdErr && !useShellExecute && !background);
            }

            return startInfo;
        }

        ///////////////////////////////////////////////////////////////////////

        private static Process CreateProcess(
            ProcessStartInfo startInfo,             /* in */
            DataReceivedEventHandler outputHandler, /* in */
            DataReceivedEventHandler errorHandler,  /* in */
            bool overrideCapture,                   /* in */
            ref Result error                        /* out */
            )
        {
            //
            // NOTE: Make sure that the process start information has
            //       been specified by the caller.
            //
            if (startInfo == null)
            {
                error = "invalid process information";
                return null;
            }

            //
            // NOTE: Create a child process OBJECT instance.  This does
            //       not actually start the process.
            //
            Process process = new Process();

            //
            // NOTE: Set the child process creation parameters to the
            //       ones we created previously.
            //
            process.StartInfo = startInfo;

            //
            // NOTE: If requested, setup asynchronous output capture
            //       events for the newly created process.
            //
            if (startInfo.RedirectStandardOutput)
            {
                if (overrideCapture && (outputHandler != null))
                {
                    process.OutputDataReceived += outputHandler;
                }
                else
                {
                    process.OutputDataReceived +=
                        new DataReceivedEventHandler(OutputDataReceived);
                }
            }

            if (startInfo.RedirectStandardError)
            {
                if (overrideCapture && (errorHandler != null))
                {
                    process.ErrorDataReceived += errorHandler;
                }
                else
                {
                    process.ErrorDataReceived +=
                        new DataReceivedEventHandler(ErrorDataReceived);
                }
            }

            return process;
        }

        ///////////////////////////////////////////////////////////////////////

        private static string GetWorkingDirectory(
            Interpreter interpreter, /* in */
            string directory,        /* in */
            ref Result error         /* out */
            )
        {
            try
            {
                //
                // NOTE: If they supplied a working directory, normalize
                //       it and then use it; otherwise, use the current
                //       directory for this process.
                //
                return (directory != null) ?
                    PathOps.ResolveFullPath(interpreter, directory) :
                    Directory.GetCurrentDirectory();
            }
            catch (Exception e)
            {
                error = e;
            }

            return null;
        }

        ///////////////////////////////////////////////////////////////////////

        private static int GetWaitForExitTimeout(
            Interpreter interpreter, /* in */
            int? timeout             /* in */
            )
        {
            if (timeout != null)
                return (int)timeout;

            if (interpreter == null)
                return EventManager.MinimumSleepTime;

            return interpreter.GetMinimumSleepTime(SleepType.Process);
        }

        ///////////////////////////////////////////////////////////////////////

        private static ReturnCode ProcessEvents(
            Interpreter interpreter, /* in */
            Process process,         /* in */
            int? timeout,            /* in */
            EventFlags eventFlags,   /* in */
            bool userInterface,      /* in */
            bool noSleep,            /* in */
            bool killOnError,        /* in */
            ref bool waitForExit,    /* out */
            ref Result error         /* out */
            )
        {
            if (process == null)
            {
                error = "invalid process";
                return ReturnCode.Error;
            }

            //
            // NOTE: If the "Debug" event flag has been set, be verbose
            //       about killing processes in response to failures.
            //
            bool verbose = FlagOps.HasFlags(
                eventFlags, EventFlags.Debug, true);

            try
            {
                //
                // NOTE: Keep going until the child process has exited.
                //
                while (!process.HasExited) /* throw */
                {
                    //
                    // NOTE: We need a local result because we do not want
                    //       to change the overall result based on random
                    //       asynchronous events that are processed while
                    //       waiting for a variable to become "signaled".
                    //       However, we will change the overall result if
                    //       a halting error is encountered, e.g. script
                    //       has been canceled, interpreter not ready, etc.
                    //       Process any pending events that may be queued.
                    //
                    Result localResult; /* REUSED */

                    if (interpreter != null)
                    {
                        localResult = null;

                        if (Engine.CheckEvents(
                                interpreter, eventFlags,
                                ref localResult) != ReturnCode.Ok)
                        {
                            if (killOnError)
                                KillProcess(process, verbose);

                            error = localResult;
                            return ReturnCode.Error;
                        }
                    }

#if WINFORMS
                    //
                    // NOTE: If requested, process pending window messages
                    //       on this thread as well.
                    //
                    if (userInterface)
                    {
                        localResult = null;

                        if (WindowOps.ProcessEvents(interpreter,
                                ref localResult) != ReturnCode.Ok)
                        {
                            error = localResult;
                            return ReturnCode.Error;
                        }
                    }
#endif

                    //
                    // NOTE: Prevent this loop from needlessly spinning
                    //       while waiting for the child process to exit.
                    //
                    int localTimeout = GetWaitForExitTimeout(interpreter,
                        timeout);

                    if ((localTimeout > 0) &&
                        process.WaitForExit(localTimeout)) /* throw */
                    {
                        //
                        // NOTE: The child process has now exited, bail
                        //       out of loop now.  Since we now know for
                        //       sure that the process has exited, reset
                        //       the wait-for-exit flag to false so the
                        //       caller can avoid calling it again.
                        //
                        waitForExit = false;
                        break;
                    }
                    else if (!noSleep)
                    {
                        //
                        // NOTE: We always try to yield to other running
                        //       threads while the child process is still
                        //       running.  This (also) gives them a small
                        //       (but important) opportunity to cancel
                        //       waiting on the child process and then
                        //       optionally terminate it.
                        //
                        try
                        {
                            localResult = null;

                            if (!EventOps.Sleep(
                                    interpreter, SleepType.Process,
                                    true, ref localResult))
                            {
                                //
                                // BUGFIX: If we reach this point, it
                                //         was likely due to some other
                                //         thread interrupting our sleep.
                                //         This should be treated as an
                                //         error worthy of causing this
                                //         method to fail after killing
                                //         the child process.
                                //
                                if (killOnError)
                                    KillProcess(process, verbose);

                                error = localResult;
                                return ReturnCode.Error;
                            }
                        }
                        catch (Exception e)
                        {
                            if (killOnError)
                                KillProcess(process, verbose);

                            error = e;
                            return ReturnCode.Error;
                        }
                    }

                    //
                    // NOTE: Force the "cached" state of the process to be
                    //       refreshed so that the HasExited property has
                    //       a better chance of actually being accurate.
                    //       Generally, this should not throw an exception.
                    //       If it does, this method is considered to have
                    //       failed.
                    //
                    process.Refresh(); /* throw */
                }

                return ReturnCode.Ok;
            }
            catch (Exception e)
            {
                if (killOnError)
                    KillProcess(process, verbose);

                error = e;
            }

            return ReturnCode.Error;
        }

        ///////////////////////////////////////////////////////////////////////

        private static void KillProcess(
            Process process, /* in */
            bool verbose
            )
        {
            if (process == null)
                return;

            try
            {
                process.Kill(); /* throw */

                if (verbose)
                {
                    TraceOps.DebugTrace(String.Format(
                        "KillProcess: {0}", FormatOps.ProcessName(
                        process, true)), typeof(ProcessOps).Name,
                        TracePriority.ProcessDebug);
                }
            }
            catch (Exception e)
            {
                TraceOps.DebugTrace(
                    e, typeof(ProcessOps).Name,
                    TracePriority.PlatformError);
            }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Private KillProcess Helper Methods
        private static ReturnCode KillProcess(
            Process process,        /* in */
            bool self,              /* in */
            bool force,             /* in */
            ref ResultList results, /* in, out */
            ref ResultList errors   /* in, out */
            )
        {
            if (process == null)
            {
                if (errors == null)
                    errors = new ResultList();

                errors.Add("invalid process");
                return ReturnCode.Error;
            }

            if (!self)
            {
                ReturnCode sameCode;
                bool sameResult = false;
                Result sameError = null;

                sameCode = IsSame(
                    process, ref sameResult, ref sameError);

                if (sameCode != ReturnCode.Ok)
                {
                    if (errors == null)
                        errors = new ResultList();

                    if (sameError != null)
                        errors.Add(sameError);
                    else
                        errors.Add("cannot verify process");

                    return sameCode;
                }

                if (sameResult)
                {
                    if (errors == null)
                        errors = new ResultList();

                    if (sameError != null)
                        errors.Add(sameError);
                    else
                        errors.Add("cannot kill self");

                    return ReturnCode.Error;
                }
            }

            if (force)
            {
                try
                {
                    //
                    // NOTE: Attempt to forcibly terminate process
                    //       now.
                    //
                    process.Kill(); /* throw */

                    //
                    // NOTE: If we get here, it should be dead now.
                    //
                    if (results == null)
                        results = new ResultList();

                    results.Add(StringList.MakeList("killed",
                        FormatOps.ProcessName(process, false)));

                    return ReturnCode.Ok;
                }
                catch (Exception e)
                {
                    TraceOps.DebugTrace(
                        e, typeof(ProcessOps).Name,
                        TracePriority.PlatformError);

                    if (errors == null)
                        errors = new ResultList();

                    errors.Add(String.Format(
                        "could not kill process {0}",
                        FormatOps.ProcessName(process, true)));
                }
            }
            else
            {
                try
                {
                    if (process.CloseMainWindow()) /* throw */
                    {
                        //
                        // NOTE: Here, we report that it was closed;
                        //       however, this may not actually be
                        //       the case if the application cancels
                        //       the close (which we have no nice
                        //       way of detecting).
                        //
                        if (results == null)
                            results = new ResultList();

                        results.Add(StringList.MakeList("closed",
                            FormatOps.ProcessName(process, false)));

                        return ReturnCode.Ok;
                    }
                    else
                    {
                        if (errors == null)
                            errors = new ResultList();

                        errors.Add(String.Format(
                            "failed request to close process {0}",
                            FormatOps.ProcessName(process, true)));
                    }
                }
                catch (Exception e)
                {
                    TraceOps.DebugTrace(
                        e, typeof(ProcessOps).Name,
                        TracePriority.PlatformError);

                    if (errors == null)
                        errors = new ResultList();

                    errors.Add(String.Format(
                        "could not close process {0}",
                        FormatOps.ProcessName(process, true)));
                }
            }

            return ReturnCode.Error;
        }

        ///////////////////////////////////////////////////////////////////////

        private static ReturnCode KillProcess(
            int id,                 /* in */
            bool self,              /* in */
            bool force,             /* in */
            ref ResultList results, /* in, out */
            ref ResultList errors   /* in, out */
            )
        {
            Process process = null;

            try
            {
                process = Process.GetProcessById(id); /* throw */
            }
            catch (Exception e)
            {
                TraceOps.DebugTrace(
                    e, typeof(ProcessOps).Name,
                    TracePriority.PlatformError);
            }

            if (process == null)
            {
                if (errors == null)
                    errors = new ResultList();

                errors.Add(String.Format(
                    "could not open process {0}", id));

                return ReturnCode.Error;
            }

            return KillProcess(
                process, self, force, ref results, ref errors);
        }

        ///////////////////////////////////////////////////////////////////////

        private static ReturnCode KillProcess(
            Interpreter interpreter, /* in */
            MatchMode mode,          /* in */
            string pattern,          /* in */
            bool noCase,             /* in */
            bool all,                /* in */
            bool self,               /* in */
            bool force,              /* in */
            ref ResultList results,  /* in, out */
            ref ResultList errors    /* in, out */
            )
        {
            Process[] processes = null;

            try
            {
                processes = Process.GetProcesses(); /* throw */
            }
            catch (Exception e)
            {
                TraceOps.DebugTrace(
                    e, typeof(ProcessOps).Name,
                    TracePriority.PlatformError);
            }

            if (processes == null)
            {
                if (errors == null)
                    errors = new ResultList();

                errors.Add("could not get process list");

                return ReturnCode.Error;
            }

            int[] counts = { 0, 0 };

            foreach (Process process in processes)
            {
                if (process == null)
                    continue;

                string name = null;
                string fileName = null;

                GetNames(process, ref name, ref fileName);

                bool match = false;

                if ((name != null) && StringOps.Match(
                        interpreter, mode, name, pattern, noCase))
                {
                    match = true;
                }
                else if (fileName != null)
                {
                    if (StringOps.Match(
                            interpreter, mode, fileName, pattern,
                            noCase))
                    {
                        match = true;
                    }
                    else if ((fileName.Length > 0) &&
                        StringOps.Match(
                            interpreter, mode, Path.GetFileName(
                            fileName), pattern, noCase))
                    {
                        match = true;
                    }
                }

                if (!match)
                    continue;

                ReturnCode code = KillProcess(
                    process, self, force, ref results,
                    ref errors);

                if (!all)
                    return code;

                counts[(code == ReturnCode.Ok) ? 0 : 1]++;
            }

            if (counts[1] == 0) /* NOTE: No errors? */
            {
                if (counts[0] > 0) /* NOTE: Did something? */
                {
                    return ReturnCode.Ok;
                }
                else
                {
                    if (errors == null)
                        errors = new ResultList();

                    errors.Add(String.Format(
                        "no such process {0}", FormatOps.WrapOrNull(
                        pattern)));
                }
            }

            return ReturnCode.Error;
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Public ExecuteProcess Methods
        public static ReturnCode ExecuteProcess(
            Interpreter interpreter,        /* in: Interpreter context to use,
                                             *     if any. */
            string domainName,              /* in: Domain name for the logon,
                                             *     if any. */
            string userName,                /* in: User name for the logon, if
                                             *     any. */
            SecureString password,          /* in: Password for the logon, if
                                             *     any. */
            string fileName,                /* in: Executable file for the new
                                             *     process. */
            string arguments,               /* in: Command line arguments for
                                             *     the new process, if any */
            string directory,               /* in: Working directory for the
                                             *     new process, if any. */
            string input,                   /* in: Simulated string input for
                                             *     the new process, if any. */
            IObject inputObject,            /* in: Opaque object handle where
                                             *     standard input stream should
                                             *     be stored. */
            EventHandler startHandler,      /* in: Event handler to be called
                                             *     right after the process is
                                             *     started. */
            DataReceivedEventHandler outputHandler, /* in: Raw event handler
                                                     *     for output data
                                                     *     coming from the
                                                     *     new process, if
                                                     *     any. */
            DataReceivedEventHandler errorHandler,  /* in: Raw event handler
                                                     *     for error data
                                                     *     coming from the
                                                     *     new process, if
                                                     *     any. */
            ProcessWindowStyle windowStyle, /* in: Normal, minimized, etc. */
            EventFlags eventFlags,          /* in: Event flags to use while
                                             *     waiting for new process to
                                             *     exit. */
            int? timeout,                   /* in: Number of milliseconds to
                                             *     wait for process to exit
                                             *     between processing events.
                                             */
            bool useShellExecute,           /* in: Use ShellExecute instead of
                                             *     CreateProcess? */
            bool captureExitCode,           /* in: Populate the exitCode
                                             *     parameter? */
            bool captureOutput,             /* in: Populate the result and
                                             *     error parameters? */
            bool useUnicode,                /* in: Captured output from process
                                             *     will be Unicode? */
            bool ignoreStdErr,              /* in: True to not capture output
                                             *     to StdErr (COMPAT: Tcl). */
            bool overrideCapture,           /* in: True to use supplied events
                                             *     to replace the built-in ones
                                             *     (i.e. instead of wrapping). */
            bool userInterface,             /* in: This thread needs to process
                                             *     window messages before any
                                             *     sleep operation. */
            bool noSleep,                   /* in: True to avoid sleeping while
                                             *     waiting for the process to
                                             *     exit. */
            bool killOnError,               /* in: True to kill process on
                                             *     interpreter error (e.g.
                                             *     disposed, exited, etc). */
            bool keepNewLine,               /* in: False to remove final cr/lf
                                             *     pair from output. */
            bool background,                /* in: Prevent waiting on child
                                             *     process to exit. */
            bool events,                    /* in: Process events while waiting
                                             *     (non-background only). */
            ref long id,                    /* out: Upon returning, the Id of
                                             *      started process, if any. */
            ref ExitCode exitCode,          /* out: Upon success, ExitCode from
                                             *      child process. */
            ref Result result,              /* out: Upon success, output from
                                             *      StdOut. */
            ref Result error                /* out: Upon success, output from
                                             *      StdErr; otherwise, error
                                             *      information. */
            )
        {
            //
            // NOTE: If capturing output, before doing anything, initialize
            //       the static data used (directly and indirectly) by this
            //       method.
            //
            if (captureOutput)
            {
                InitializeStandardOutputsAndErrors();

                if (!overrideCapture)
                    InitializeOutputAndErrorHandlers();
            }

            //
            // NOTE: The file name cannot be null or an empty string.  If it
            //       is, fail now.
            //
            if (String.IsNullOrEmpty(fileName))
            {
                //
                // NOTE: Yes, we know that the file name is null or an empty
                //       string; however, this is (still) the right error.
                //
                error = String.Format(
                    "couldn't execute {0}: no such file or directory",
                    FormatOps.WrapOrNull(fileName));

                return ReturnCode.Error;
            }

            //
            // NOTE: Check if the executable file name is really a remote
            //       URI.  If not, the file name will be made absolute.
            //
            bool remoteUri = false;

            fileName = PathOps.SubstituteOrResolvePath(
                interpreter, fileName, true, ref remoteUri);

            //
            // NOTE: The file name may have changed.  Make sure it is still
            //       not null or an empty string.
            //
            if (String.IsNullOrEmpty(fileName))
            {
                //
                // NOTE: Really, the file name is just plain invalid at this
                //       point (it could not be normalized for some reason);
                //       however, the difference is really academic.
                //
                error = String.Format(
                    "couldn't execute {0}: no such file or directory",
                    FormatOps.WrapOrNull(fileName)); /* COMPAT: Tcl. */

                return ReturnCode.Error;
            }

            //
            // NOTE: If this is an absolute [local] path, verify the file
            //       exists; otherwise, it could be anything, including
            //       shell commands (i.e. "things we cannot verify").
            //
            if (!remoteUri &&
                Path.IsPathRooted(fileName) && !File.Exists(fileName))
            {
                error = String.Format(
                    "couldn't execute {0}: no such file or directory",
                    FormatOps.WrapOrNull(fileName));

                return ReturnCode.Error;
            }

            //
            // NOTE: If they supplied a working directory for the child
            //       process, normalize it and then use it; otherwise, use
            //       the current directory for this process.
            //
            string workingDirectory;
            Result localError; /* REUSED */

            localError = null;

            workingDirectory = GetWorkingDirectory(
                interpreter, directory, ref localError);

            //
            // NOTE: At this point, there must be a valid working directory
            //       to continue.
            //
            if (workingDirectory == null)
            {
                if (localError != null)
                {
                    error = localError;
                }
                else
                {
                    error = String.Format(
                        "invalid working directory {0}",
                        FormatOps.WrapOrNull(directory));
                }

                return ReturnCode.Error;
            }

            //
            // NOTE: Create object to place the child process creation
            //       parameters into and populate it.
            //
            ProcessStartInfo startInfo;

            localError = null;

            startInfo = CreateStartInfo(
                domainName, userName, password, fileName, arguments,
                workingDirectory, input, inputObject, windowStyle,
                useShellExecute, captureOutput, useUnicode, ignoreStdErr,
                background, ref localError);

            if (startInfo == null)
            {
                error = localError;
                return ReturnCode.Error;
            }

            //
            // NOTE: Attempt to create a child process OBJECT.  This does
            //       not actually start the process.
            //
            Process process;

            localError = null;

            process = CreateProcess(
                startInfo, outputHandler, errorHandler, overrideCapture,
                ref localError);

            if (process == null)
            {
                error = localError;
                return ReturnCode.Error;
            }

            try
            {
                //
                // NOTE: If necessary, setup the process output buffers
                //       now.
                //
                // BUGFIX: Part #1.  Setting up process output buffers
                //         MUST be done prior to actually starting the
                //         process because the process event handlers
                //         cannot do anything without them.
                //
                localError = null;

                if (PreSetupCapturedOutput(startInfo,
                        process, outputHandler, errorHandler,
                        StringBuilderCapacity, overrideCapture,
                        ref localError) != ReturnCode.Ok)
                {
                    error = localError;
                    return ReturnCode.Error;
                }

                //
                // NOTE: Start the process.  We may or may not wait for
                //       it to complete before returning (see below).
                //
                process.Start(); /* throw */

                //
                // NOTE: If necessary, try to redirect the input and/or
                //       capture the output from the started process.
                //
                // BUGFIX: Part #2.  Since methods BeginOutputReadLine,
                //         BeginErrorReadLine, and StandardInput cannot
                //         be accessed prior to actually starting the
                //         process, do that now.
                //
                localError = null;

                if (PostSetupCapturedOutput(
                        startInfo, process, input, inputObject,
                        ref localError) != ReturnCode.Ok)
                {
                    error = localError;
                    return ReturnCode.Error;
                }

                //
                // NOTE: If the caller supplied an event handler to be
                //       invoked upon process start, do that now (i.e.
                //       after input/output redirection has been fully
                //       setup).
                //
                if (startHandler != null)
                    startHandler(process, new EventArgs());

                //
                // NOTE: Give caller the Id for newly started process.
                //       This value could be zero if a process was not
                //       actually started -OR- an existing browser was
                //       reused (etc) -OR- the process was started in
                //       the background.
                //
                id = 0;

                if (!TryGetIdAndPassToInterpreter(
                        interpreter, process, ref id) || background)
                {
                    //
                    // NOTE: For background child processes, we do not
                    //       wait and we return the PID of the child
                    //       process.  The value may be zero if we did
                    //       not actually start a process.
                    //
                    result = id;
                    error = null;

                    return ReturnCode.Ok;
                }
                else
                {
                    //
                    // NOTE: Wait for child process to exit and record
                    //       the results.
                    //
                    bool waitForExit = true;

                    if (events)
                    {
                        localError = null;

                        if (ProcessEvents(
                                interpreter, process, timeout,
                                eventFlags, userInterface, noSleep,
                                killOnError, ref waitForExit,
                                ref localError) != ReturnCode.Ok)
                        {
                            error = localError;
                            return ReturnCode.Error;
                        }
                    }

                    //
                    // NOTE: If necessary, block until we are sure that
                    //       we have received all pending output from
                    //       the process and just to make sure that the
                    //       process has actually exited (apparently,
                    //       the HasExited property cannot always be
                    //       trusted).  Also, if the caller did not
                    //       choose to process events while waiting,
                    //       this should keep us in sync.
                    //
                    bool didWaitForExit = false;

                    if (waitForExit)
                    {
                        if (timeout != null)
                        {
                            process.WaitForExit(
                                (int)timeout); /* throw */
                        }
                        else
                        {
                            process.WaitForExit(); /* throw */
                            didWaitForExit = true;
                        }
                    }

                    //
                    // NOTE: Save exit code for later use?  We do NOT
                    //       try to interpret the meaning of it here.
                    //
                    if (captureExitCode)
                    {
                        //
                        // BUGFIX: Part #3.  To get its exit code, we
                        //         MUST always wait (synchronously)
                        //         for the process to exit.  This MAY
                        //         have already been done (above).
                        //
                        if (!didWaitForExit)
                        {
                            process.WaitForExit(); /* throw */
                            didWaitForExit = true;
                        }

                        exitCode = (ExitCode)process.ExitCode; /* throw */
                    }

                    //
                    // NOTE: Only populate the caller's variables if
                    //       we are requested to do so.
                    //
                    if (captureOutput)
                    {
                        //
                        // BUGFIX: Part #4.  When capturing output, we
                        //         MUST always wait (synchronously) for
                        //         the process to exit prior to getting
                        //         the captured output; otherwise, some
                        //         of it may be missed.  This MAY have
                        //         already been done (above).  From the
                        //         MSDN documentation, this must be done
                        //         using the method overload that does
                        //         not have a timeout value.
                        //
                        if (!didWaitForExit)
                        {
                            process.WaitForExit(); /* throw */
                            didWaitForExit = true;
                        }

                        return GetCapturedOutput(
                            startInfo, process, useShellExecute,
                            keepNewLine, ref result, ref error);
                    }
                    else
                    {
                        result = null;
                        error = null;

                        return ReturnCode.Ok;
                    }
                }
            }
            catch (Exception e)
            {
                error = e;
            }
            finally
            {
                ReturnCode terminateCode;
                Result terminateError = null;

                terminateCode = TerminateCapturedOutput(
                    startInfo, process, ref terminateError);

                if (terminateCode != ReturnCode.Ok)
                {
                    DebugOps.Complain(
                        interpreter, terminateCode, terminateError);
                }
            }

            return ReturnCode.Error;
        }

        ///////////////////////////////////////////////////////////////////////

        //
        // NOTE: For use by the [test2] command only.
        //
        public static ReturnCode ExecuteProcess(
            Interpreter interpreter, /* in */
            string fileName,         /* in */
            string arguments,        /* in */
            string directory,        /* in */
            EventFlags eventFlags,   /* in */
            bool useUnicode,         /* in */
            ref long id,             /* out */
            ref ExitCode exitCode,   /* out */
            ref Result result,       /* out */
            ref Result error         /* out */
            )
        {
            return ExecuteProcess(interpreter, null,
                null, null, fileName, arguments, directory, null,
                null, null, null, null, ProcessWindowStyle.Normal,
                eventFlags, null, false, true, true, useUnicode,
                false, false, false, false, false, true, false,
                true, ref id, ref exitCode, ref result, ref error);
        }

        ///////////////////////////////////////////////////////////////////////

        //
        // NOTE: For use by the PlatformOps.GetInstalledUpdates
        //       and ScriptOps.ExtractToDirectory methods only.
        //
        public static ReturnCode ExecuteProcess(
            Interpreter interpreter, /* in */
            string fileName,         /* in */
            string arguments,        /* in */
            EventFlags eventFlags,   /* in */
            ref ExitCode exitCode,   /* out */
            ref Result result,       /* out */
            ref Result error         /* out */
            )
        {
            long id = 0;

            return ExecuteProcess(interpreter, null,
                null, null, fileName, arguments, null, null, null, null,
                null, null, ProcessWindowStyle.Normal, eventFlags, null,
                false, true, true, false, false, false, false, false,
                true, false, false, true, ref id, ref exitCode, ref result,
                ref error);
        }

        ///////////////////////////////////////////////////////////////////////

#if SHELL && INTERACTIVE_COMMANDS
        //
        // NOTE: For use by the interactive "#website" command only.
        //
        public static ReturnCode ShellExecuteProcess(
            Interpreter interpreter, /* in */
            string fileName,         /* in */
            string arguments,        /* in */
            string directory,        /* in */
            EventFlags eventFlags,   /* in */
            ref Result error         /* out */
            )
        {
            long id = 0;
            ExitCode exitCode = ResultOps.SuccessExitCode();
            Result result = null;

            return ExecuteProcess(interpreter, null,
                null, null, fileName, arguments, directory, null, null,
                null, null, null, ProcessWindowStyle.Normal, eventFlags,
                null, true, false, false, false, false, false, false,
                false, false, false, false, true, ref id, ref exitCode,
                ref result, ref error);
        }

        ///////////////////////////////////////////////////////////////////////

        //
        // NOTE: For use by the interactive "#cmd" command only.
        //
        public static ReturnCode ExecuteProcess(
            Interpreter interpreter, /* in */
            string fileName,         /* in */
            string arguments,        /* in */
            string directory,        /* in */
            EventFlags eventFlags,   /* in */
            ref Result error         /* out */
            )
        {
            long id = 0;
            ExitCode exitCode = ResultOps.SuccessExitCode();
            Result result = null;

            return ExecuteProcess(interpreter, null,
                null, null, fileName, arguments, directory, null, null,
                null, null, null, ProcessWindowStyle.Normal, eventFlags,
                null, false, false, false, false, false, false, false,
                false, false, false, false, true, ref id, ref exitCode,
                ref result, ref error);
        }

        ///////////////////////////////////////////////////////////////////////

        //
        // NOTE: For use by the interactive "#tclshrc" command only.
        //
        public static ReturnCode ExecuteProcess(
            Interpreter interpreter, /* in */
            string fileName,         /* in */
            string arguments,        /* in */
            string directory,        /* in */
            EventFlags eventFlags,   /* in */
            bool background,         /* in */
            ref Result error         /* out */
            )
        {
            long id = 0;
            ExitCode exitCode = ResultOps.SuccessExitCode();
            Result result = null;

            return ExecuteProcess(interpreter, null,
                null, null, fileName, arguments, directory, null, null,
                null, null, null, ProcessWindowStyle.Normal, eventFlags,
                null, false, false, false, false, false, false, false,
                false, false, false, background, true, ref id, ref exitCode,
                ref result, ref error);
        }
#endif
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Public KillProcess Methods
        public static ReturnCode KillProcess(
            Interpreter interpreter, /* in */
            string idOrPattern,      /* in */
            CultureInfo cultureInfo, /* in */
            bool all,                /* in */
            bool self,               /* in */
            bool force,              /* in */
            ref Result result        /* out */
            )
        {
            int id = 0;
            ResultList results = null;
            ResultList errors = null;

            if (Value.GetInteger2(
                    idOrPattern, ValueFlags.AnyInteger, cultureInfo,
                    ref id) == ReturnCode.Ok)
            {
                if (all)
                {
                    result = "option \"-all\" cannot be used with a pid";
                    return ReturnCode.Error;
                }

                if (KillProcess(id, self,
                        force, ref results, ref errors) == ReturnCode.Ok)
                {
                    result = results;
                    return ReturnCode.Ok;
                }
            }
            else
            {
                if (KillProcess(
                        interpreter, MatchMode.Glob, idOrPattern,
                        PathOps.NoCase, all, self, force, ref results,
                        ref errors) == ReturnCode.Ok)
                {
                    result = results;
                    return ReturnCode.Ok;
                }
            }

            result = ResultList.Combine(results, errors);
            return ReturnCode.Error;
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Private Data Helper Methods
        private static bool CheckDataReceived(
            string data,     /* in */
            int level,       /* in */
            ref Result error /* out */
            )
        {
            if (data == null)
            {
                error = "invalid data";
                return false;
            }

            int length = data.Length;

            if (length == 0)
            {
                error = "empty data";
                return false;
            }

            if (level < 2)
                return true;

            string trimData = data.Trim();

            if (trimData == null) /* IMPOSSIBLE? */
            {
                error = "invalid trim data";
                return false;
            }

            int trimLength = trimData.Length;

            if (trimLength == 0)
            {
                error = "spaces only data";
                return false;
            }

            if (level < 3)
                return true;

            int badIndex = Index.Invalid;
            int badUpper = 0;
            int badLower = 0;

            for (int index = 0; index < length; index++)
            {
                char character = data[index];

                if (character > Characters.Tilde) // U+007E
                {
                    if (badIndex == Index.Invalid)
                        badIndex = index;

                    badUpper++;
                }
                else if ((level > 3) &&
                    (character < Characters.Space)) // U+0020
                {
                    if (badIndex == Index.Invalid)
                        badIndex = index;

                    badLower++;
                }
            }

            if (badIndex != Index.Invalid)
            {
                error = String.Format(
                    "out-of-bounds characters starting at index {0}: " +
                    "{1} are too high, {2} are too low", badIndex,
                    badUpper, badLower);

                return false;
            }
            else
            {
                return true;
            }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Private Event Handlers
        private static void OutputDataReceived(
            object sender,          /* in */
            DataReceivedEventArgs e /* in */
            )
        {
            Process process = sender as Process;

            if ((process == null) || (e == null))
            {
                TraceOps.DebugTrace(String.Format(
                    "OutputDataReceived: missing process {0} -OR- e = {1}",
                    process != null ? "<notNull>" : FormatOps.DisplayNull,
                    e != null ? "<notNull>" : FormatOps.DisplayNull),
                    typeof(ProcessOps).Name, TracePriority.ProcessError);

                return;
            }

            string data = e.Data;

            MaybeCheckDataReceived("OutputDataReceived", data);

            bool success;

            lock (syncRoot) /* TRANSACTIONAL */
            {
                success = (standardOutputs != null) ?
                    standardOutputs.AppendData(process, data) : false;
            }

            if (!success)
            {
                TraceOps.DebugTrace(String.Format(
                    "OutputDataReceived: missing builder? {0}: dropping {1}",
                    EntityOps.GetNameNoThrow(process),
                    FormatOps.DisplayStringLength(data)),
                    typeof(ProcessOps).Name,
                    TracePriority.ProcessError);
            }

            DataReceivedEventHandler handler = GetOutputHandler(process);

            if (handler != null)
            {
                try
                {
                    handler(sender, e); /* throw */
                }
                catch (Exception ex)
                {
                    TraceOps.DebugTrace(
                        ex, typeof(ProcessOps).Name,
                        TracePriority.ProcessError);
                }
            }
        }

        ///////////////////////////////////////////////////////////////////////

        private static void ErrorDataReceived(
            object sender,          /* in */
            DataReceivedEventArgs e /* in */
            )
        {
            Process process = sender as Process;

            if ((process == null) || (e == null))
            {
                TraceOps.DebugTrace(String.Format(
                    "ErrorDataReceived: missing process {0} -OR- e = {1}",
                    process != null ? "<notNull>" : FormatOps.DisplayNull,
                    e != null ? "<notNull>" : FormatOps.DisplayNull),
                    typeof(ProcessOps).Name, TracePriority.ProcessError);

                return;
            }

            string data = e.Data;

            MaybeCheckDataReceived("ErrorDataReceived", data);

            bool success;

            lock (syncRoot) /* TRANSACTIONAL */
            {
                success = (standardErrors != null) ?
                    standardErrors.AppendData(process, data) : false;
            }

            if (!success)
            {
                TraceOps.DebugTrace(String.Format(
                    "ErrorDataReceived: missing builder? {0}: dropping {1}",
                    EntityOps.GetNameNoThrow(process),
                    FormatOps.DisplayStringLength(data)),
                    typeof(ProcessOps).Name,
                    TracePriority.ProcessError);
            }

            DataReceivedEventHandler handler = GetErrorHandler(process);

            if (handler != null)
            {
                try
                {
                    handler(sender, e); /* throw */
                }
                catch (Exception ex)
                {
                    TraceOps.DebugTrace(
                        ex, typeof(ProcessOps).Name,
                        TracePriority.ProcessError);
                }
            }
        }
        #endregion
    }
}
