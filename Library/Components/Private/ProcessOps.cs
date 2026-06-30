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
using ComEnv = Eagle._Components.Private.CommonOps.Environment;
using SBF = Eagle._Components.Private.StringBuilderFactory;
using SBC = Eagle._Components.Private.StringBuilderCache;

#if NET_STANDARD_21
using Index = Eagle._Constants.Index;
#endif

namespace Eagle._Components.Private
{
    /// <summary>
    /// This class provides the central set of helper methods used to create,
    /// start, monitor, capture output from, and terminate external (child)
    /// processes on behalf of the Eagle core library.  It also maintains the
    /// per-process state needed to capture, log, and dispatch the standard
    /// output and standard error data produced by those processes, and it
    /// implements the console reference counting mechanism that is shared (via
    /// environment variables) between parent and child processes.
    /// </summary>
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
        /// <summary>
        /// The composite format string used to construct the fully qualified
        /// environment variable name for a console reference count.  The first
        /// argument is the name prefix and the second argument is the decimal
        /// string representation of the integer process identifier.
        /// </summary>
        private static readonly string ReferenceCountEnvVarFormat = "{0}{1}";

        ///////////////////////////////////////////////////////////////////////

        //
        // HACK: This is purposely not read-only.
        //
        /// <summary>
        /// The default name prefix used when constructing the console reference
        /// count environment variable name and no explicit prefix is supplied.
        /// </summary>
        private static string ReferenceCountDefaultPrefix = "ReferenceCount";

        ///////////////////////////////////////////////////////////////////////

        //
        // HACK: This is purposely not read-only.
        //
        /// <summary>
        /// The initial capacity to use, if any, when creating the string
        /// builders used to capture standard output and standard error data.
        /// A null value indicates that the default capacity should be used.
        /// </summary>
        private static int? StringBuilderCapacity = null;

        ///////////////////////////////////////////////////////////////////////

        //
        // HACK: This is purposely not read-only.
        //
        /// <summary>
        /// When non-zero, the infinite (no timeout) wait-for-exit code path is
        /// disabled, forcing the use of a finite timeout even when the caller
        /// requested waiting forever for a process to exit.
        /// </summary>
        private static bool DoNotWaitForever = false;
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Private Data
        /// <summary>
        /// The object used to synchronize access to the per-process output, log
        /// path, capture, and handler dictionaries maintained by this class.
        /// </summary>
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
        /// <summary>
        /// Controls the level of validation performed on the data received via
        /// the process output and error events.  Zero disables checking; higher
        /// values progressively check for null/empty, spaces-only, and
        /// out-of-range character values.
        /// </summary>
        private static int DataReceivedCheckLevel = 0;

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Maps each tracked process to the log file path where its captured
        /// standard output data should be appended.
        /// </summary>
        private static ProcessDictionary<string> outputLogPaths = null;

        /// <summary>
        /// Maps each tracked process to the log file path where its captured
        /// standard error data should be appended.
        /// </summary>
        private static ProcessDictionary<string> errorLogPaths = null;

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Maps each tracked process to the string builder used to accumulate
        /// its captured standard output data in memory.
        /// </summary>
        private static ProcessStringBuilderDictionary outputCaptures = null;

        /// <summary>
        /// Maps each tracked process to the string builder used to accumulate
        /// its captured standard error data in memory.
        /// </summary>
        private static ProcessStringBuilderDictionary errorCaptures = null;

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Maps each tracked process to the optional caller-supplied event
        /// handler that should be invoked for its standard output data.
        /// </summary>
        private static ProcessDataReceivedEventHandlerDictionary outputHandlers = null;

        /// <summary>
        /// Maps each tracked process to the optional caller-supplied event
        /// handler that should be invoked for its standard error data.
        /// </summary>
        private static ProcessDataReceivedEventHandlerDictionary errorHandlers = null;
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Public Introspection Support Methods
        /// <summary>
        /// This method appends a section of diagnostic name/value pairs that
        /// describe the current per-process output, log path, capture, and
        /// handler state to the specified list.
        /// </summary>
        /// <param name="list">
        /// The list to which the diagnostic information should be appended.  If
        /// this is null, this method does nothing.
        /// </param>
        /// <param name="detailFlags">
        /// The flags that control the level of detail to include in the
        /// resulting diagnostic information.
        /// </param>
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

                if (empty || ((outputLogPaths != null) &&
                    (outputLogPaths.Count > 0)))
                {
                    localList.Add("OutputLogPaths",
                        (outputLogPaths != null) ?
                            outputLogPaths.Count.ToString() :
                            FormatOps.DisplayNull);
                }

                if (empty || ((errorLogPaths != null) &&
                    (errorLogPaths.Count > 0)))
                {
                    localList.Add("ErrorLogPaths",
                        (errorLogPaths != null) ?
                            errorLogPaths.Count.ToString() :
                            FormatOps.DisplayNull);
                }

                if (empty || ((outputCaptures != null) &&
                    (outputCaptures.Count > 0)))
                {
                    localList.Add("OutputCaptures",
                        (outputCaptures != null) ?
                            outputCaptures.Count.ToString() :
                            FormatOps.DisplayNull);
                }

                if (empty || ((errorCaptures != null) &&
                    (errorCaptures.Count > 0)))
                {
                    localList.Add("ErrorCaptures",
                        (errorCaptures != null) ?
                            errorCaptures.Count.ToString() :
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

        #region Process-Wide Variable Support Methods
        /// <summary>
        /// This method constructs the fully qualified environment variable name
        /// used to refer to the console reference count value associated with
        /// the specified process.
        /// </summary>
        /// <param name="prefix">
        /// The name prefix to use.  If this is null, the default prefix is used
        /// instead.
        /// </param>
        /// <param name="processId">
        /// The integer identifier of the process the reference count belongs
        /// to.
        /// </param>
        /// <returns>
        /// The constructed environment variable name.
        /// </returns>
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

        /// <summary>
        /// This method determines the console reference count environment
        /// variable name and value to use, preferring an existing variable for
        /// the parent process, then the current process, and finally falling
        /// back to the variable name for the current process when none exists.
        /// </summary>
        /// <param name="prefix">
        /// The name prefix to use.  If this is null, the default prefix is used
        /// instead.
        /// </param>
        /// <param name="variable">
        /// Upon return, this contains the selected environment variable name.
        /// </param>
        /// <param name="value">
        /// Upon return, this contains the value of the selected environment
        /// variable, or null if no existing variable was found.
        /// </param>
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

                if (ComEnv.DoesVariableExist(
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

        /// <summary>
        /// This method attempts to parse the specified string into a console
        /// reference count value.  A null string is treated as a reference
        /// count of zero.
        /// </summary>
        /// <param name="value">
        /// The string to parse, or null to use a reference count of zero.
        /// </param>
        /// <param name="cultureInfo">
        /// The culture to use when parsing the integer value.
        /// </param>
        /// <param name="referenceCount">
        /// Upon return, this contains the parsed reference count value.
        /// </param>
        /// <param name="error">
        /// Upon failure, this contains an appropriate error message.
        /// </param>
        /// <returns>
        /// True if the value was parsed successfully (or was null); otherwise,
        /// false.
        /// </returns>
        private static bool TryGetReferenceCount(
            string value,            /* in */
            CultureInfo cultureInfo, /* in */
            out long referenceCount, /* out */
            ref Result error         /* out */
            )
        {
            referenceCount = 0;

            if ((value == null) || (Value.GetWideInteger2(
                    value, ValueFlags.AnyInteger, cultureInfo,
                    ref referenceCount, ref error) == ReturnCode.Ok))
            {
                return true;
            }

            return false;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method attempts to parse the specified string into a list of
        /// strings using Tcl-style list parsing.  A null string is treated as
        /// the absence of a list.
        /// </summary>
        /// <param name="value">
        /// The string to parse, or null to indicate no list.
        /// </param>
        /// <param name="list">
        /// Upon return, this contains the parsed list, or null if the input
        /// value was null.
        /// </param>
        /// <param name="error">
        /// Upon failure, this contains an appropriate error message.
        /// </param>
        /// <returns>
        /// True if the value was parsed successfully (or was null); otherwise,
        /// false.
        /// </returns>
        private static bool TryGetStringList(
            string value,        /* in */
            out StringList list, /* out */
            ref Result error     /* out */
            )
        {
            list = null;

            if ((value == null) || (ParserOps<string>.SplitList(
                    null, value, 0, Length.Invalid, false,
                    ref list, ref error) == ReturnCode.Ok))
            {
                return true;
            }

            return false;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method reads the console reference count from its environment
        /// variable and, if requested, increments or decrements it, persisting
        /// the updated value (or removing the variable when the count drops to
        /// zero or below).
        /// </summary>
        /// <param name="prefix">
        /// The name prefix to use.  If this is null, the default prefix is used
        /// instead.
        /// </param>
        /// <param name="cultureInfo">
        /// The culture to use when parsing the integer value.
        /// </param>
        /// <param name="increment">
        /// Non-zero to increment the reference count, zero to decrement it, or
        /// null to leave it unchanged.
        /// </param>
        /// <param name="referenceCount">
        /// Upon return, this contains the resulting reference count value.
        /// </param>
        /// <param name="error">
        /// Upon failure, this contains an appropriate error message.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise,
        /// <see cref="ReturnCode.Error" />.
        /// </returns>
        public static ReturnCode CheckAndMaybeModifyReferenceCount(
            string prefix,           /* in: OPTIONAL */
            CultureInfo cultureInfo, /* in */
            bool? increment,         /* in: OPTIONAL */
            out long referenceCount, /* out */
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

                long localReferenceCount;

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
                        if (!ComEnv.SetVariable(variable,
                                localReferenceCount.ToString()))
                        {
                            error = "could not set environment variable";
                            return ReturnCode.Error;
                        }
                    }
                    else
                    {
                        if (!ComEnv.UnsetVariable(variable))
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

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method reads a list-valued environment variable and, if
        /// requested, clears it and/or appends an element to it, persisting the
        /// updated list (or removing the variable when the resulting list is
        /// empty).
        /// </summary>
        /// <param name="prefix">
        /// The name prefix to use.  If this is null, the default prefix is used
        /// instead.
        /// </param>
        /// <param name="element">
        /// The element to append to the list, or null to append nothing.
        /// </param>
        /// <param name="clear">
        /// Non-zero to clear the existing list prior to appending.
        /// </param>
        /// <param name="list">
        /// Upon return, this contains the resulting list.
        /// </param>
        /// <param name="error">
        /// Upon failure, this contains an appropriate error message.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise,
        /// <see cref="ReturnCode.Error" />.
        /// </returns>
        public static ReturnCode CheckAndMaybeAppendElement(
            string prefix,       /* in: OPTIONAL */
            string element,      /* in: OPTIONAL */
            bool clear,          /* in */
            out StringList list, /* out */
            ref Result error     /* out */
            )
        {
            list = null;

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

                StringList localList;

                if (!TryGetStringList(
                        value, out localList, ref error))
                {
                    return ReturnCode.Error;
                }

                if (clear || (element != null))
                {
                    if (clear && (localList != null))
                        localList.Clear();

                    if (element != null)
                    {
                        if (localList == null)
                            localList = new StringList();

                        localList.Add(element);
                    }

                    if ((localList != null) &&
                        (localList.Count > 0))
                    {
                        if (!ComEnv.SetVariable(
                                variable, localList.ToString()))
                        {
                            error = "could not set environment variable";
                            return ReturnCode.Error;
                        }
                    }
                    else
                    {
                        if (!ComEnv.UnsetVariable(variable))
                        {
                            error = "could not unset environment variable";
                            return ReturnCode.Error;
                        }
                    }
                }

                list = localList;
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
        /// <summary>
        /// This method initializes the per-process output and error log path
        /// dictionaries, creating them only if they do not already exist.
        /// </summary>
        private static void InitializeOutputAndErrorLogPaths()
        {
            lock (syncRoot) /* TRANSACTIONAL */
            {
                if (outputLogPaths == null)
                    outputLogPaths = new ProcessDictionary<string>();

                if (errorLogPaths == null)
                    errorLogPaths = new ProcessDictionary<string>();
            }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method initializes the per-process output and error capture
        /// dictionaries, creating them only if they do not already exist.
        /// </summary>
        private static void InitializeOutputAndErrorCaptures()
        {
            lock (syncRoot) /* TRANSACTIONAL */
            {
                if (outputCaptures == null)
                    outputCaptures = new ProcessStringBuilderDictionary();

                if (errorCaptures == null)
                    errorCaptures = new ProcessStringBuilderDictionary();
            }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method initializes the per-process output and error handler
        /// dictionaries, creating them only if they do not already exist.
        /// </summary>
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

        /// <summary>
        /// This method initializes the per-process state needed to capture
        /// output and error data when output capture is enabled, optionally
        /// including the built-in handler dictionaries.
        /// </summary>
        /// <param name="captureOutput">
        /// Non-zero if output and error data will be captured; if this is zero,
        /// this method does nothing.
        /// </param>
        /// <param name="overrideCapture">
        /// Non-zero if caller-supplied handlers replace the built-in ones, in
        /// which case the handler dictionaries are not initialized.
        /// </param>
        private static void InitializeOutputsAndErrors(
            bool captureOutput,  /* in */
            bool overrideCapture /* in */
            )
        {
            if (captureOutput)
            {
                InitializeOutputAndErrorLogPaths();
                InitializeOutputAndErrorCaptures();

                if (!overrideCapture)
                    InitializeOutputAndErrorHandlers();
            }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method clears and releases the per-process output and error log
        /// path dictionaries.
        /// </summary>
        /// <returns>
        /// The total number of log path entries that were removed.
        /// </returns>
        private static int ClearOutputAndErrorLogPaths()
        {
            lock (syncRoot) /* TRANSACTIONAL */
            {
                int result = 0;

                if (outputLogPaths != null)
                {
                    result += outputLogPaths.Count;

                    outputLogPaths.Clear();
                    outputLogPaths = null;
                }

                if (errorLogPaths != null)
                {
                    result += errorLogPaths.Count;

                    errorLogPaths.Clear();
                    errorLogPaths = null;
                }

                return result;
            }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method clears and releases the per-process output and error
        /// capture dictionaries.
        /// </summary>
        /// <returns>
        /// The total number of capture entries that were removed.
        /// </returns>
        private static int ClearOutputAndErrorCaptures()
        {
            lock (syncRoot) /* TRANSACTIONAL */
            {
                int result = 0;

                if (outputCaptures != null)
                {
                    result += outputCaptures.Count;

                    outputCaptures.Clear();
                    outputCaptures = null;
                }

                if (errorCaptures != null)
                {
                    result += errorCaptures.Count;

                    errorCaptures.Clear();
                    errorCaptures = null;
                }

                return result;
            }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method clears and releases the per-process output and error
        /// handler dictionaries.
        /// </summary>
        /// <returns>
        /// The total number of handler entries that were removed.
        /// </returns>
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

        /// <summary>
        /// This method clears and releases all of the per-process log path,
        /// capture, and handler state maintained by this class.
        /// </summary>
        /// <returns>
        /// The total number of entries that were removed across all of the
        /// per-process dictionaries.
        /// </returns>
        public static int Cleanup()
        {
            lock (syncRoot) /* TRANSACTIONAL */
            {
                int result = 0;

                result += ClearOutputAndErrorLogPaths();
                result += ClearOutputAndErrorCaptures();
                result += ClearOutputAndErrorHandlers();

                return result;
            }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method validates received output or error data according to the
        /// current data-received check level, emitting a diagnostic trace if the
        /// data appears suspect.  No checking is performed when the check level
        /// is zero or below.
        /// </summary>
        /// <param name="methodName">
        /// The name of the calling method, used in any diagnostic trace
        /// message.
        /// </param>
        /// <param name="data">
        /// The received data to validate.
        /// </param>
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

        /// <summary>
        /// This method formats the important fields of the specified process
        /// start information into a single Tcl-style dictionary line suitable
        /// for appending to a log file.
        /// </summary>
        /// <param name="prefix">
        /// An optional prefix string to include at the start of the formatted
        /// line, or null for none.
        /// </param>
        /// <param name="logTag">
        /// An optional tag string to include in the formatted line, or null for
        /// none.
        /// </param>
        /// <param name="startInfo">
        /// The process start information to format.
        /// </param>
        /// <returns>
        /// The formatted log line.
        /// </returns>
        private static string FormatStartInfoForLogPaths(
            string prefix,             /* in: OPTIONAL */
            string logTag,             /* in: OPTIONAL */
            ProcessStartInfo startInfo /* in */
            )
        {
            StringBuilder builder = SBF.Create();

            builder.AppendLine();
            builder.AppendLine();
            builder.Append("==== ");

            if (!String.IsNullOrEmpty(prefix))
                builder.AppendFormat("{0}: ", prefix);

            builder.Append(new StringList("logTag", logTag,
                "workingDirectory", startInfo.WorkingDirectory,
                "fileName", startInfo.FileName, "arguments",
                startInfo.Arguments));

            builder.Append(" ====");
            builder.AppendLine();
            builder.AppendLine();

            return SBC.GetStringAndRelease(ref builder);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method formats the specified process start information and, if
        /// any log paths are set, appends it to them as a single log line.
        /// </summary>
        /// <param name="outputLogPath">
        /// The output log file path to append to, or null for none.
        /// </param>
        /// <param name="errorLogPath">
        /// The error log file path to append to, or null for none.
        /// </param>
        /// <param name="prefix">
        /// An optional prefix string to include in the formatted line, or null
        /// for none.
        /// </param>
        /// <param name="logTag">
        /// An optional tag string to include in the formatted line, or null for
        /// none.
        /// </param>
        /// <param name="startInfo">
        /// The process start information to format and append.
        /// </param>
        /// <returns>
        /// True if the formatted line was appended to at least one log path;
        /// otherwise, false.
        /// </returns>
        private static bool MaybeAppendStartInfoToLogPaths(
            string outputLogPath,      /* in: OPTIONAL */
            string errorLogPath,       /* in: OPTIONAL */
            string prefix,             /* in: OPTIONAL */
            string logTag,             /* in: OPTIONAL */
            ProcessStartInfo startInfo /* in */
            )
        {
            //
            // NOTE: If one (or both) of the log paths are set, log all
            //       the important process information to it, as a line
            //       that is a properly formatted Tcl-style dictionary,
            //       i.e. as a Tcl-style list of name/value pairs.
            //
            int count = 0;

            if ((startInfo != null) &&
                ((outputLogPath != null) || (errorLogPath != null)))
            {
                string logData = FormatStartInfoForLogPaths(
                    prefix, logTag, startInfo);

                if ((outputLogPath != null) &&
                    AppendDataToLogPath(outputLogPath, logData))
                {
                    count++;
                }

                if ((errorLogPath != null) &&
                    AppendDataToLogPath(errorLogPath, logData))
                {
                    count++;
                }
            }

            return (count > 0);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method appends the specified data, as a single line, to the
        /// specified log file path.  Any exception is caught and traced rather
        /// than propagated.
        /// </summary>
        /// <param name="path">
        /// The log file path to append to.
        /// </param>
        /// <param name="data">
        /// The data to append, or null to append nothing.
        /// </param>
        /// <returns>
        /// True if the data was appended successfully; otherwise, false.
        /// </returns>
        private static bool AppendDataToLogPath(
            string path, /* in */
            string data  /* in */
            )
        {
            try
            {
                if (data != null)
                {
#if NET_40
                    /* NO RESULT */
                    File.AppendAllLines(
                        path, new string[] { data });
#else
                    StringBuilder builder = SBF.Create(
                        data.Length + 2 /* NewLine */);

                    builder.AppendLine(data);

                    /* NO RESULT */
                    File.AppendAllText(
                        path, SBC.GetStringAndRelease(
                        ref builder));
#endif

                    return true;
                }
            }
            catch (Exception e)
            {
                TraceOps.DebugTrace(
                    e, typeof(ProcessOps).Name,
                    TracePriority.ProcessError2);
            }

            return false;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method appends the specified data to the standard output log
        /// file associated with the specified process, if any.
        /// </summary>
        /// <param name="process">
        /// The process whose associated output log file should be appended to.
        /// </param>
        /// <param name="data">
        /// The data to append.
        /// </param>
        /// <returns>
        /// True if the data was appended successfully; otherwise, false.
        /// </returns>
        private static bool AppendOutputDataToLogPath(
            Process process, /* in */
            string data      /* in */
            )
        {
            lock (syncRoot) /* TRANSACTIONAL */
            {
                string path;

                if ((outputLogPaths != null) &&
                    outputLogPaths.TryGetValue(process, out path))
                {
                    return AppendDataToLogPath(path, data);
                }
                else
                {
                    return false;
                }
            }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method appends the specified data to the standard error log
        /// file associated with the specified process, if any.
        /// </summary>
        /// <param name="process">
        /// The process whose associated error log file should be appended to.
        /// </param>
        /// <param name="data">
        /// The data to append.
        /// </param>
        /// <returns>
        /// True if the data was appended successfully; otherwise, false.
        /// </returns>
        private static bool AppendErrorDataToLogPath(
            Process process, /* in */
            string data      /* in */
            )
        {
            lock (syncRoot) /* TRANSACTIONAL */
            {
                string path;

                if ((errorLogPaths != null) &&
                    errorLogPaths.TryGetValue(process, out path))
                {
                    return AppendDataToLogPath(path, data);
                }
                else
                {
                    return false;
                }
            }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method appends the specified data to the in-memory standard
        /// output capture associated with the specified process, if any.
        /// </summary>
        /// <param name="process">
        /// The process whose associated output capture should be appended to.
        /// </param>
        /// <param name="data">
        /// The data to append.
        /// </param>
        /// <returns>
        /// True if the data was appended successfully; otherwise, false.
        /// </returns>
        private static bool AppendOutputDataToCapture(
            Process process, /* in */
            string data      /* in */
            )
        {
            lock (syncRoot) /* TRANSACTIONAL */
            {
                if (outputCaptures == null)
                    return false;

                return outputCaptures.AppendData(process, data);
            }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method appends the specified data to the in-memory standard
        /// error capture associated with the specified process, if any.
        /// </summary>
        /// <param name="process">
        /// The process whose associated error capture should be appended to.
        /// </param>
        /// <param name="data">
        /// The data to append.
        /// </param>
        /// <returns>
        /// True if the data was appended successfully; otherwise, false.
        /// </returns>
        private static bool AppendErrorDataToCapture(
            Process process, /* in */
            string data      /* in */
            )
        {
            lock (syncRoot) /* TRANSACTIONAL */
            {
                if (errorCaptures == null)
                    return false;

                return errorCaptures.AppendData(process, data);
            }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method retrieves the caller-supplied standard output event
        /// handler associated with the specified process, if any.
        /// </summary>
        /// <param name="process">
        /// The process whose associated output handler should be retrieved.
        /// </param>
        /// <returns>
        /// The associated output handler, or null if there is none.
        /// </returns>
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

        /// <summary>
        /// This method retrieves the caller-supplied standard error event
        /// handler associated with the specified process, if any.
        /// </summary>
        /// <param name="process">
        /// The process whose associated error handler should be retrieved.
        /// </param>
        /// <returns>
        /// The associated error handler, or null if there is none.
        /// </returns>
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

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method invokes the specified data-received event handler, if it
        /// is not null, catching and tracing any exception it throws.
        /// </summary>
        /// <param name="handler">
        /// The event handler to invoke, or null to invoke nothing.
        /// </param>
        /// <param name="sender">
        /// The sender object to pass to the event handler.
        /// </param>
        /// <param name="e">
        /// The event arguments to pass to the event handler.
        /// </param>
        /// <returns>
        /// True if the handler was invoked successfully; otherwise, false.
        /// </returns>
        private static bool MaybeInvokeDataReceivedEventHandler(
            DataReceivedEventHandler handler, /* in */
            object sender,                    /* in */
            DataReceivedEventArgs e           /* in */
            )
        {
            if (handler != null)
            {
                try
                {
                    handler(sender, e); /* throw */
                    return true;
                }
                catch (Exception ex)
                {
                    TraceOps.DebugTrace(
                        ex, typeof(ProcessOps).Name,
                        TracePriority.ProcessError2);
                }
            }

            return false;
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Captured Output & Handler Support Methods
        /// <summary>
        /// This method registers the per-process log path, capture, and handler
        /// state required to capture output and error data, prior to the
        /// process being started.  Upon failure, any partially registered state
        /// is removed.
        /// </summary>
        /// <param name="startInfo">
        /// The process start information that indicates which streams are being
        /// redirected.
        /// </param>
        /// <param name="process">
        /// The process for which capture state should be registered.
        /// </param>
        /// <param name="outputLogPath">
        /// The standard output log file path to associate with the process, or
        /// null for none.
        /// </param>
        /// <param name="errorLogPath">
        /// The standard error log file path to associate with the process, or
        /// null for none.
        /// </param>
        /// <param name="outputHandler">
        /// The caller-supplied standard output handler to associate with the
        /// process, or null for none.
        /// </param>
        /// <param name="errorHandler">
        /// The caller-supplied standard error handler to associate with the
        /// process, or null for none.
        /// </param>
        /// <param name="capacity">
        /// The initial capacity to use for the capture string builders, or null
        /// to use the default.
        /// </param>
        /// <param name="overrideCapture">
        /// Non-zero if the caller-supplied handlers replace the built-in ones,
        /// in which case the handlers are not registered here.
        /// </param>
        /// <param name="error">
        /// Upon failure, this contains an appropriate error message.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise,
        /// <see cref="ReturnCode.Error" />.
        /// </returns>
        private static ReturnCode PreSetupForCapture(
            ProcessStartInfo startInfo,             /* in */
            Process process,                        /* in */
            string outputLogPath,                   /* in */
            string errorLogPath,                    /* in */
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
                        if (success && (outputLogPaths != null) &&
                            (outputLogPath != null))
                        {
                            /* NO RESULT */
                            outputLogPaths.Add(process, outputLogPath);
                        }

                        if (success && (outputCaptures != null) &&
                            !outputCaptures.NewData(process, capacity))
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
                        if (success && (errorLogPaths != null) &&
                            (errorLogPath != null))
                        {
                            /* NO RESULT */
                            errorLogPaths.Add(process, errorLogPath);
                        }

                        if (success && (errorCaptures != null) &&
                            !errorCaptures.NewData(process, capacity))
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

                        if (errorCaptures != null)
                        {
                            /* IGNORED */
                            errorCaptures.RemoveData(process);
                        }

                        if (errorLogPaths != null)
                        {
                            /* IGNORED */
                            errorLogPaths.Remove(process);
                        }

                        if (outputHandlers != null)
                        {
                            /* IGNORED */
                            outputHandlers.Remove(process);
                        }

                        if (outputCaptures != null)
                        {
                            /* IGNORED */
                            outputCaptures.RemoveData(process);
                        }

                        if (outputLogPaths != null)
                        {
                            /* IGNORED */
                            outputLogPaths.Remove(process);
                        }
                    }
                }
            }

            return ReturnCode.Error;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method begins asynchronous capture of the redirected output and
        /// error streams and, if requested, writes the supplied input to the
        /// standard input stream, after the process has been started.
        /// </summary>
        /// <param name="startInfo">
        /// The process start information that indicates which streams are being
        /// redirected.
        /// </param>
        /// <param name="process">
        /// The started process for which capture should begin.
        /// </param>
        /// <param name="input">
        /// The input string to write to the standard input stream, or null for
        /// none.
        /// </param>
        /// <param name="inputObject">
        /// The opaque object that should receive the standard input stream
        /// writer, or null for none.
        /// </param>
        /// <param name="standardInput">
        /// Upon return, this contains the standard input stream writer when
        /// input redirection is in use; otherwise, it is left unchanged.
        /// </param>
        /// <param name="error">
        /// Upon failure, this contains an appropriate error message.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise,
        /// <see cref="ReturnCode.Error" />.
        /// </returns>
        private static ReturnCode PostSetupForCapture(
            ProcessStartInfo startInfo,     /* in */
            Process process,                /* in */
            string input,                   /* in */
            IObject inputObject,            /* in, out */
            ref StreamWriter standardInput, /* out */
            ref Result error                /* out */
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
                        standardInput = process.StandardInput;

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

        /// <summary>
        /// This method cancels asynchronous reads and retrieves the captured
        /// standard output and standard error data for the specified process,
        /// optionally stripping any trailing newline.
        /// </summary>
        /// <param name="startInfo">
        /// The process start information that indicates which streams were
        /// redirected.
        /// </param>
        /// <param name="process">
        /// The process whose captured data should be retrieved.
        /// </param>
        /// <param name="useShellExecute">
        /// Non-zero if the process was started via the shell, in which case no
        /// captured data is available.
        /// </param>
        /// <param name="keepNewLine">
        /// Non-zero to keep the final trailing newline; zero to strip it
        /// (COMPAT: Tcl).
        /// </param>
        /// <param name="result">
        /// Upon return, this contains the captured standard output data, or
        /// null when none is available.
        /// </param>
        /// <param name="error">
        /// Upon return, this contains the captured standard error data, or null
        /// when none is available; upon failure, it contains an appropriate
        /// error message.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise,
        /// <see cref="ReturnCode.Error" />.
        /// </returns>
        private static ReturnCode GetCaptureData(
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
                        try
                        {
                            process.CancelOutputRead(); /* throw */
                        }
                        catch (Exception e)
                        {
                            TraceOps.DebugTrace(
                                e, typeof(ProcessOps).Name,
                                TracePriority.CleanupError);
                        }

                        lock (syncRoot) /* TRANSACTIONAL */
                        {
                            if (outputCaptures != null)
                            {
                                localOutput = outputCaptures.GetData(
                                    process);
                            }
                        }
                    }

                    string localError = null;

                    if (startInfo.RedirectStandardError)
                    {
                        try
                        {
                            process.CancelErrorRead(); /* throw */
                        }
                        catch (Exception e)
                        {
                            TraceOps.DebugTrace(
                                e, typeof(ProcessOps).Name,
                                TracePriority.CleanupError);
                        }

                        lock (syncRoot) /* TRANSACTIONAL */
                        {
                            if (errorCaptures != null)
                            {
                                localError = errorCaptures.GetData(
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

        /// <summary>
        /// This method removes all of the per-process log path, capture, and
        /// handler state associated with the specified process, for the streams
        /// that were redirected.
        /// </summary>
        /// <param name="startInfo">
        /// The process start information that indicates which streams were
        /// redirected.
        /// </param>
        /// <param name="process">
        /// The process whose capture state should be removed.
        /// </param>
        /// <param name="error">
        /// Upon failure, this contains an appropriate error message.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise,
        /// <see cref="ReturnCode.Error" />.
        /// </returns>
        private static ReturnCode TerminateForCapture(
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
                        if (outputLogPaths != null)
                        {
                            /* IGNORED */
                            outputLogPaths.Remove(process);
                        }

                        if (outputCaptures != null)
                        {
                            /* IGNORED */
                            outputCaptures.RemoveData(process);
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
                        if (errorLogPaths != null)
                        {
                            /* IGNORED */
                            errorLogPaths.Remove(process);
                        }

                        if (errorCaptures != null)
                        {
                            /* IGNORED */
                            errorCaptures.RemoveData(process);
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
        /// <summary>
        /// This method determines whether the specified process is the same as
        /// the current (this) process, comparing by reference and by process
        /// identifier.
        /// </summary>
        /// <param name="process">
        /// The process to compare against the current process.
        /// </param>
        /// <returns>
        /// True if the specified process is the current process; otherwise,
        /// false.
        /// </returns>
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

        /// <summary>
        /// This method retrieves the process object for the current (this)
        /// process.
        /// </summary>
        /// <returns>
        /// The process object representing the current process.
        /// </returns>
        public static Process GetCurrent()
        {
            return Process.GetCurrentProcess();
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method retrieves the native operating system handle for the
        /// current (this) process.
        /// </summary>
        /// <returns>
        /// The native handle for the current process, or
        /// <see cref="IntPtr.Zero" /> if it cannot be obtained.
        /// </returns>
        public static IntPtr GetHandle()
        {
            Process process = GetCurrent();

            if (process == null)
                return IntPtr.Zero;

            return process.Handle;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method retrieves the integer identifier of the current (this)
        /// process.
        /// </summary>
        /// <returns>
        /// The identifier of the current process, or zero if it cannot be
        /// obtained.
        /// </returns>
        public static long GetId()
        {
            Process process = GetCurrent();

            if (process == null)
                return 0;

            return process.Id;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method retrieves the integer identifier of the parent of the
        /// current (this) process, when native support is available.
        /// </summary>
        /// <returns>
        /// The identifier of the parent process, or zero if it cannot be
        /// obtained.
        /// </returns>
        public static long GetParentId()
        {
#if NATIVE
            return NativeOps.GetParentProcessId().ToInt64();
#else
            return 0;
#endif
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method retrieves the main module file name of the current
        /// (this) process.
        /// </summary>
        /// <returns>
        /// The main module file name of the current process, or null if it
        /// cannot be obtained.
        /// </returns>
        public static string GetFileName()
        {
            Process process = GetCurrent();

            if (process == null)
                return null;

            return GetFileName(process.Id);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method retrieves the main module file name of the process with
        /// the specified identifier.
        /// </summary>
        /// <param name="id">
        /// The integer identifier of the process whose file name is wanted.
        /// </param>
        /// <returns>
        /// The main module file name of the specified process, or null if it
        /// cannot be obtained.
        /// </returns>
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

        /// <summary>
        /// This method retrieves the loaded modules for the specified process.
        /// </summary>
        /// <param name="process">
        /// The process whose loaded modules are wanted.
        /// </param>
        /// <returns>
        /// An enumerable of the loaded modules, or null if it cannot be
        /// obtained.
        /// </returns>
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

        /// <summary>
        /// This method retrieves the process name and main module file name for
        /// the specified process, tracing any exception rather than propagating
        /// it.
        /// </summary>
        /// <param name="process">
        /// The process whose name and file name are wanted.
        /// </param>
        /// <param name="name">
        /// Upon return, this contains the process name, if it could be
        /// obtained.
        /// </param>
        /// <param name="fileName">
        /// Upon return, this contains the main module file name, if it could be
        /// obtained.
        /// </param>
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

        /// <summary>
        /// This method determines whether the specified process is the same as
        /// the current (this) process.
        /// </summary>
        /// <param name="process">
        /// The process to compare against the current process.
        /// </param>
        /// <param name="result">
        /// Upon success, this is non-zero if the processes are the same;
        /// otherwise, zero.
        /// </param>
        /// <param name="error">
        /// Upon failure, this contains an appropriate error message.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise,
        /// <see cref="ReturnCode.Error" />.
        /// </returns>
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

        /// <summary>
        /// This method determines whether two processes are the same, comparing
        /// by reference and by process identifier.  Two null processes are
        /// considered the same.
        /// </summary>
        /// <param name="process1">
        /// The first process to compare.
        /// </param>
        /// <param name="process2">
        /// The second process to compare.
        /// </param>
        /// <param name="result">
        /// Upon success, this is non-zero if the processes are the same;
        /// otherwise, zero.
        /// </param>
        /// <param name="error">
        /// Upon failure, this contains an appropriate error message.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise,
        /// <see cref="ReturnCode.Error" />.
        /// </returns>
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
        /// <summary>
        /// This method attempts to safely retrieve the integer identifier of
        /// the specified process, tracing any failure rather than propagating
        /// it.
        /// </summary>
        /// <param name="process">
        /// The process whose identifier is wanted.
        /// </param>
        /// <param name="id">
        /// Upon success, this contains the process identifier.
        /// </param>
        /// <returns>
        /// True if the identifier was obtained; otherwise, false.
        /// </returns>
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

        /// <summary>
        /// This method attempts to safely retrieve the integer identifier of
        /// the specified process.  Accessing the identifier can throw if the
        /// process was not actually started by this library.
        /// </summary>
        /// <param name="process">
        /// The process whose identifier is wanted.
        /// </param>
        /// <param name="id">
        /// Upon success, this contains the process identifier.
        /// </param>
        /// <param name="error">
        /// Upon failure, this contains an appropriate error message.
        /// </param>
        /// <returns>
        /// True if the identifier was obtained; otherwise, false.
        /// </returns>
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

        /// <summary>
        /// This method safely retrieves the integer identifier of the specified
        /// process and, unless suppressed, records it as the previous process
        /// identifier for the specified interpreter.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter whose previous process identifier should be set, or
        /// null for none.
        /// </param>
        /// <param name="process">
        /// The process whose identifier is wanted.
        /// </param>
        /// <param name="noPreviousProcessId">
        /// Non-zero to skip setting the previous process identifier on the
        /// interpreter.
        /// </param>
        /// <param name="id">
        /// Upon success, this contains the process identifier.
        /// </param>
        /// <returns>
        /// True if the identifier was obtained; otherwise, false.
        /// </returns>
        private static bool TryGetIdAndPassToInterpreter(
            Interpreter interpreter,  /* in: OPTIONAL */
            Process process,          /* in */
            bool noPreviousProcessId, /* in */
            ref long id               /* out */
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
                if (!noPreviousProcessId)
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

        /// <summary>
        /// This method creates and populates a process start information object
        /// from the supplied parameters, including logon credentials, file name
        /// and arguments, working directory, encodings, and the input/output
        /// redirection settings implied by the other options.
        /// </summary>
        /// <param name="domainName">
        /// The domain name for the logon, if any.
        /// </param>
        /// <param name="userName">
        /// The user name for the logon, if any.
        /// </param>
        /// <param name="password">
        /// The password for the logon, if any.
        /// </param>
        /// <param name="fileName">
        /// The executable file for the new process.
        /// </param>
        /// <param name="arguments">
        /// The command line arguments for the new process, if any.
        /// </param>
        /// <param name="workingDirectory">
        /// The working directory for the new process, if any.
        /// </param>
        /// <param name="input">
        /// The simulated input for the new process, if any.
        /// </param>
        /// <param name="inputObject">
        /// The opaque object handle where the standard input stream should be
        /// stored.
        /// </param>
        /// <param name="outputLogPath">
        /// The optional log path where captured output data should be appended.
        /// </param>
        /// <param name="errorLogPath">
        /// The optional log path where captured error data should be appended.
        /// </param>
        /// <param name="windowStyle">
        /// The window style (normal, minimized, etc.) for the new process.
        /// </param>
        /// <param name="useShellExecute">
        /// Non-zero to use ShellExecute instead of CreateProcess.
        /// </param>
        /// <param name="captureOutput">
        /// Non-zero to enable redirection so output and error data can be
        /// captured later.
        /// </param>
        /// <param name="useUnicode">
        /// Non-zero if captured output from the process will be Unicode.
        /// </param>
        /// <param name="ignoreStdErr">
        /// Non-zero to not capture output written to standard error (COMPAT:
        /// Tcl).
        /// </param>
        /// <param name="background">
        /// Non-zero to prevent waiting on the child process to exit.
        /// </param>
        /// <param name="error">
        /// Upon failure, this contains an appropriate error message.
        /// </param>
        /// <returns>
        /// The populated process start information object.
        /// </returns>
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
            string outputLogPath,           /* in: Optional log path where
                                             *     captured output data should
                                             *     be appended. */
            string errorLogPath,            /* in: Optional log path where
                                             *     captured error data should
                                             *     be appended. */
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
            startInfo.RedirectStandardInput =
                (!useShellExecute && (background ||
                (input != null) || (inputObject != null)));

            if (captureOutput)
            {
                startInfo.RedirectStandardOutput =
                    (!useShellExecute &&
                    (!background || (outputLogPath != null)));

                startInfo.RedirectStandardError =
                    (!ignoreStdErr && !useShellExecute &&
                    (!background || (errorLogPath != null)));
            }

            return startInfo;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method creates a process object from the specified start
        /// information and wires up the output and error data-received events,
        /// using either the caller-supplied handlers or the built-in ones.
        /// This does not actually start the process.
        /// </summary>
        /// <param name="startInfo">
        /// The process start information to assign to the new process object.
        /// </param>
        /// <param name="outputHandler">
        /// The caller-supplied standard output handler, if any.
        /// </param>
        /// <param name="errorHandler">
        /// The caller-supplied standard error handler, if any.
        /// </param>
        /// <param name="overrideCapture">
        /// Non-zero to use the caller-supplied handlers in place of the
        /// built-in ones; otherwise, the built-in handlers are used.
        /// </param>
        /// <param name="error">
        /// Upon failure, this contains an appropriate error message.
        /// </param>
        /// <returns>
        /// The created process object, or null upon failure.
        /// </returns>
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

        /// <summary>
        /// This method determines the working directory to use for a new
        /// process, resolving and normalizing the supplied directory or falling
        /// back to the current directory of this process.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter context to use when resolving the directory.
        /// </param>
        /// <param name="directory">
        /// The requested working directory, or null to use the current
        /// directory.
        /// </param>
        /// <param name="error">
        /// Upon failure, this contains an appropriate error message.
        /// </param>
        /// <returns>
        /// The resolved working directory, or null upon failure.
        /// </returns>
        private static string GetWorkingDirectory(
            Interpreter interpreter, /* in */
            string directory,        /* in */
            ref Result error         /* out */
            )
        {
            //
            // NOTE: If they supplied a working directory, normalize
            //       it and then use it; otherwise, use the current
            //       directory for this process.
            //
            string workingDirectory = null;

            if (directory != null)
            {
                workingDirectory = PathOps.ResolveFullPath(
                    interpreter, directory, ref error);
            }
            else
            {
                workingDirectory = PathOps.GetCurrentDirectory();

                if (workingDirectory == null)
                    error = "invalid current directory";
            }

            return workingDirectory;
        }

        ///////////////////////////////////////////////////////////////////////

        //
        // NOTE: This method is NEVER allowed to return any infinite timeout
        //       value, i.e. it cannot return a value that is negative -AND-
        //       it cannot return null, because some (potential) callers use
        //       that value to indicate "wait forever".
        //
        /// <summary>
        /// This method determines the finite timeout, in milliseconds, to use
        /// when waiting for a process to exit, preferring the supplied timeout,
        /// then the interpreter minimum sleep time, then the default exit
        /// timeout, and finally a fixed minimum.  This method never returns a
        /// negative or infinite value.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter context to consult for timeout values, if any.
        /// </param>
        /// <param name="timeout">
        /// The requested timeout, in milliseconds, or null to compute one from
        /// the other sources.
        /// </param>
        /// <returns>
        /// The non-negative timeout value, in milliseconds, to use.
        /// </returns>
        private static int GetWaitForExitTimeout(
            Interpreter interpreter, /* in: OPTIONAL */
            int? timeout             /* in */
            )
        {
            int localTimeout; /* REUSED */

            if (timeout != null)
            {
                localTimeout = (int)timeout;

                if (localTimeout >= 0)
                    return localTimeout;
            }

            if (interpreter != null)
            {
                localTimeout = interpreter.GetMinimumSleepTime(
                    SleepType.Process);

                if (localTimeout >= 0)
                    return localTimeout;
            }

            localTimeout = ThreadOps.GetDefaultTimeout(
                interpreter, TimeoutType.Exit);

            if (localTimeout >= 0)
                return localTimeout;

            return EventManager.MinimumSleepTime;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method safely determines whether the specified process has
        /// exited, optionally waiting for it, discarding the exited indicator.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter context to use while waiting, if any.
        /// </param>
        /// <param name="process">
        /// The process to check.
        /// </param>
        /// <param name="timeout">
        /// The timeout, in milliseconds, to use when waiting, or null for the
        /// default.
        /// </param>
        /// <param name="waitForExit">
        /// Non-zero to wait for the process to exit; zero to merely check its
        /// current state.
        /// </param>
        /// <returns>
        /// True if the exited state was determined successfully; otherwise,
        /// false.
        /// </returns>
        private static bool SafeHasExited(
            Interpreter interpreter, /* in */
            Process process,         /* in */
            int? timeout,            /* in */
            bool waitForExit         /* in */
            )
        {
            bool hasExited; /* NOT USED */

            return SafeHasExited(
                interpreter, process, timeout, waitForExit,
                out hasExited);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method safely determines whether the specified process has
        /// exited, optionally waiting for it, catching and tracing any
        /// exception rather than propagating it.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter context to use while waiting, if any.
        /// </param>
        /// <param name="process">
        /// The process to check.
        /// </param>
        /// <param name="timeout">
        /// The timeout, in milliseconds, to use when waiting, or null for the
        /// default.
        /// </param>
        /// <param name="waitForExit">
        /// Non-zero to wait for the process to exit; zero to merely check its
        /// current state.
        /// </param>
        /// <param name="hasExited">
        /// Upon return, this is non-zero if the process has exited; otherwise,
        /// zero.
        /// </param>
        /// <returns>
        /// True if the exited state was determined successfully; otherwise,
        /// false.
        /// </returns>
        private static bool SafeHasExited(
            Interpreter interpreter, /* in */
            Process process,         /* in */
            int? timeout,            /* in */
            bool waitForExit,        /* in */
            out bool hasExited       /* out */
            )
        {
            hasExited = false;

            try
            {
                if (process != null)
                {
                    if (waitForExit)
                    {
                        return WaitForExit(
                            interpreter, process, timeout, false,
                            ref hasExited); /* throw */
                    }
                    else
                    {
                        hasExited = process.HasExited; /* throw */

                        TraceOps.DebugTrace(String.Format(
                            "SafeHasExited: {0} ==> EXITED (?) {1}",
                            EntityOps.GetNameOrIdNoThrow(process),
                            hasExited), typeof(ProcessOps).Name,
                            TracePriority.ProcessDebug);

                        return true;
                    }
                }
            }
            catch (Exception e)
            {
                TraceOps.DebugTrace(
                    e, typeof(ProcessOps).Name,
                    TracePriority.ProcessError2);
            }

            return false;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method is the thread-pool work item callback used to process
        /// events while waiting on a background process to exit and to clean up
        /// its in-memory capture state afterward.  The state argument carries
        /// the wait information.
        /// </summary>
        /// <param name="state">
        /// The <see cref="ProcessWaitInfo" /> describing the process being
        /// waited on.
        /// </param>
        private static void ProcessWaitCallback(
            object state /* in */
            ) /* System.Threading.WaitCallback */
        {
            ProcessWaitInfo processWaitInfo = state as ProcessWaitInfo;

            if (processWaitInfo == null)
                return;

            bool waitForExit = true; /* TODO: Why not? */
            Result error = null;

            if (ProcessEvents(
                    processWaitInfo.Interpreter,
                    processWaitInfo.StartInfo,
                    processWaitInfo.Process,
                    processWaitInfo.OutputLogPath,
                    processWaitInfo.ErrorLogPath,
                    processWaitInfo.LogTag,
                    processWaitInfo.Timeout,
                    processWaitInfo.EventFlags,
                    processWaitInfo.UserInterface,
                    processWaitInfo.NoSleep,
                    processWaitInfo.KillOnError,
                    processWaitInfo.Background,
                    ref waitForExit, /* NOT USED */
                    ref error) != ReturnCode.Ok)
            {
                TraceOps.DebugTrace(String.Format(
                    "ProcessWaitCallback: error = {0}",
                    FormatOps.WrapOrNull(error)),
                    typeof(ProcessOps).Name,
                    TracePriority.ProcessError2);
            }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method processes pending interpreter (and optionally window)
        /// events on the calling thread while waiting for the specified process
        /// to exit, sleeping between iterations and optionally killing the
        /// process on error.  When the process is in the background, any
        /// in-memory capture state is cleaned up before returning.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter context whose events should be processed, if any.
        /// </param>
        /// <param name="startInfo">
        /// The process start information for the process being waited on.
        /// </param>
        /// <param name="process">
        /// The process being waited on.
        /// </param>
        /// <param name="outputLogPath">
        /// The standard output log file path associated with the process, or
        /// null for none.
        /// </param>
        /// <param name="errorLogPath">
        /// The standard error log file path associated with the process, or
        /// null for none.
        /// </param>
        /// <param name="logTag">
        /// The optional tag string to include in the log file(s), if any.
        /// </param>
        /// <param name="timeout">
        /// The timeout, in milliseconds, to use when waiting between event
        /// processing, or null for the default.
        /// </param>
        /// <param name="eventFlags">
        /// The event flags to use while processing events.
        /// </param>
        /// <param name="userInterface">
        /// Non-zero if pending window messages should also be processed on this
        /// thread.
        /// </param>
        /// <param name="noSleep">
        /// Non-zero to avoid sleeping between event processing iterations.
        /// </param>
        /// <param name="killOnError">
        /// Non-zero to kill the process when an interpreter error is
        /// encountered.
        /// </param>
        /// <param name="background">
        /// Non-zero if the process is a background process, in which case its
        /// capture state is cleaned up before returning.
        /// </param>
        /// <param name="waitForExit">
        /// Upon return, this is set to zero when it is known for certain that
        /// the process has exited, allowing the caller to avoid waiting again.
        /// </param>
        /// <param name="error">
        /// Upon failure, this contains an appropriate error message.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise,
        /// <see cref="ReturnCode.Error" />.
        /// </returns>
        private static ReturnCode ProcessEvents(
            Interpreter interpreter,    /* in */
            ProcessStartInfo startInfo, /* in */
            Process process,            /* in */
            string outputLogPath,       /* in */
            string errorLogPath,        /* in */
            string logTag,              /* in */
            int? timeout,               /* in */
            EventFlags eventFlags,      /* in */
            bool userInterface,         /* in */
            bool noSleep,               /* in */
            bool killOnError,           /* in */
            bool background,            /* in */
            ref bool waitForExit,       /* out */
            ref Result error            /* out */
            )
        {
            if (process == null)
            {
                error = "invalid process";
                return ReturnCode.Error;
            }

            EngineFlags engineFlags;
            SubstitutionFlags substitutionFlags;
            ExpressionFlags expressionFlags;
            Result localError = null;

            if (!Engine.TryQueryAllFlags(
                    interpreter, Engine.BlockingFlagsForProcess,
                    out engineFlags, out substitutionFlags,
                    out expressionFlags, ref localError))
            {
                if (FlagOps.HasFlags(
                        eventFlags, EventFlags.FailSafe, true))
                {
                    Engine.InitializeAllFlags(
                        out engineFlags, out substitutionFlags,
                        out expressionFlags);
                }
                else
                {
                    error = localError;
                    return ReturnCode.Error;
                }
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
                // NOTE: Keep going until child process has exited.  Since
                //       merely checking the HasExited property can cause
                //       an exception to be thrown, use the "safe" helper
                //       method.
                //
                // BUGFIX: Even if the process exits immediately, we really
                //         should process events at least once prior to our
                //         loop exiting, if possible.
                //
                int checkEventsCount = 0;
                bool hasExited; /* REUSED */

                while (((checkEventsCount == 0) && (interpreter != null)) ||
                    (SafeHasExited(
                        interpreter, process, null, false, out hasExited) &&
                    !hasExited))
                {
                    //
                    // NOTE: We need a local result because we do not want
                    //       to change the overall result based on random
                    //       asynchronous events that are processed while
                    //       waiting for a variable to become "signaled".
                    //       However, we will change the overall result if
                    //       a halting error is encountered, e.g. script
                    //       has been canceled, interpreter not ready, etc.
                    //       Process any pending events that may be queued
                    //       to this thread -AND- that satisfy the current
                    //       event flags for the interpreter involved.
                    //
                    Result localResult; /* REUSED */

                    if (interpreter != null)
                    {
                        localResult = null;

                        if (Engine.CheckEvents(interpreter,
                                engineFlags, substitutionFlags,
                                eventFlags, expressionFlags,
                                ref localResult) != ReturnCode.Ok)
                        {
                            if (killOnError)
                                KillProcess(process, false, verbose);

                            error = localResult;
                            return ReturnCode.Error;
                        }

                        //
                        // NOTE: Zero or more engine events have (again?)
                        //       been successfully processed, increment
                        //       the (loop invariant) counter associated
                        //       with that.
                        //
                        checkEventsCount++;
                    }

#if WINFORMS
                    //
                    // NOTE: If requested, process any pending window(s)
                    //       messages on this thread as well.
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
                    hasExited = false; /* REDUNDANT? */

                    if (WaitForExit(
                            interpreter, process, timeout, false,
                            ref hasExited) && hasExited) /* throw */
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
                                    KillProcess(process, false, verbose);

                                error = localResult;
                                return ReturnCode.Error;
                            }
                        }
                        catch (Exception e)
                        {
                            if (killOnError)
                                KillProcess(process, false, verbose);

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
                    KillProcess(process, false, verbose);

                error = e;
            }
            finally
            {
                if (background &&
                    ((outputLogPath != null) || (errorLogPath != null)))
                {
                    //
                    // HACK: Attempt to make sure the target process
                    //       has (actually) exited by this point, so
                    //       the outputs captured into the log files
                    //       are complete.
                    //
                    /* IGNORED */
                    SafeHasExited(interpreter, process, null, true);

                    /* IGNORED */
                    MaybeAppendStartInfoToLogPaths(outputLogPath,
                        errorLogPath, "END", logTag, startInfo);

                    ReturnCode terminateCode;
                    Result terminateError = null;

                    terminateCode = TerminateForCapture(
                        startInfo, process, ref terminateError);

                    if (terminateCode != ReturnCode.Ok)
                    {
                        DebugOps.Complain(
                            interpreter, terminateCode, terminateError);
                    }
                }
            }

            return ReturnCode.Error;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method forcibly terminates the specified process, tracing any
        /// exception rather than propagating it.  When operating in what-if
        /// mode, the process is not actually terminated.
        /// </summary>
        /// <param name="process">
        /// The process to terminate.
        /// </param>
        /// <param name="whatIf">
        /// Non-zero to only trace what would be done, without actually killing
        /// the process.
        /// </param>
        /// <param name="verbose">
        /// Non-zero to emit a diagnostic trace describing the terminated
        /// process.
        /// </param>
        private static void KillProcess(
            Process process, /* in */
            bool whatIf,     /* in */
            bool verbose     /* in */
            )
        {
            if (process == null)
                return;

            try
            {
                if (whatIf)
                {
                    TraceOps.DebugTrace(String.Format(
                        "KillProcess: KILL {0}",
                        FormatOps.ProcessName(process, true)),
                        typeof(ProcessOps).Name,
                        TracePriority.ProcessDebug2);
                }
                else
                {
                    process.Kill(); /* throw */
                }

                if (verbose)
                {
                    TraceOps.DebugTrace(String.Format(
                        "KillProcess: {0}",
                        FormatOps.ProcessName(process, true)),
                        typeof(ProcessOps).Name,
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

        #region Public ExecuteProcess Helper Methods
        /// <summary>
        /// This method resolves the input, input object, and callback handler
        /// options used when executing a process, reading variable values and
        /// extracting the appropriate delegates from the supplied callbacks.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter context to use, if any.
        /// </param>
        /// <param name="options">
        /// The option dictionary to use when fixing up the input object, if
        /// any.
        /// </param>
        /// <param name="startCallback">
        /// The callback to use as the process start handler, if any.
        /// </param>
        /// <param name="stdOutCallback">
        /// The callback to use as the standard output handler, if any.
        /// </param>
        /// <param name="stdErrCallback">
        /// The callback to use as the standard error handler, if any.
        /// </param>
        /// <param name="stdInVarName">
        /// The name of the variable containing the input string, if any.
        /// </param>
        /// <param name="stdInObjectVarName">
        /// The name of the variable that should receive the input object
        /// handle, if any.
        /// </param>
        /// <param name="objectFlags">
        /// The object flags to use when creating the input object.
        /// </param>
        /// <param name="captureInput">
        /// Non-zero to resolve the input and input object options.
        /// </param>
        /// <param name="captureOutput">
        /// Non-zero to resolve the standard output and standard error handler
        /// options.
        /// </param>
        /// <param name="input">
        /// Upon success, this contains the resolved input string, if any.
        /// </param>
        /// <param name="inputObject">
        /// Upon success, this contains the resolved input object, if any.
        /// </param>
        /// <param name="outputHandler">
        /// Upon success, this contains the resolved standard output handler, if
        /// any.
        /// </param>
        /// <param name="errorHandler">
        /// Upon success, this contains the resolved standard error handler, if
        /// any.
        /// </param>
        /// <param name="startHandler">
        /// Upon success, this contains the resolved process start handler, if
        /// any.
        /// </param>
        /// <param name="error">
        /// Upon failure, this contains an appropriate error message.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise,
        /// <see cref="ReturnCode.Error" />.
        /// </returns>
        public static ReturnCode HandleCaptureOptions(
            Interpreter interpreter,                    /* in: OPTIONAL */
            OptionDictionary options,                   /* in: OPTIONAL */
            ICallback startCallback,                    /* in: OPTIONAL */
            ICallback stdOutCallback,                   /* in: OPTIONAL */
            ICallback stdErrCallback,                   /* in: OPTIONAL */
            string stdInVarName,                        /* in: OPTIONAL */
            string stdInObjectVarName,                  /* in: OPTIONAL */
            ObjectFlags objectFlags,                    /* in */
            bool captureInput,                          /* in */
            bool captureOutput,                         /* in */
            ref Result input,                           /* out */
            ref IObject inputObject,                    /* out */
            ref DataReceivedEventHandler outputHandler, /* out */
            ref DataReceivedEventHandler errorHandler,  /* out */
            ref EventHandler startHandler,              /* out */
            ref Result error                            /* out */
            )
        {
            if (captureInput)
            {
                if (stdInVarName != null)
                {
                    if (interpreter == null)
                    {
                        error = "invalid interpreter";
                        return ReturnCode.Error;
                    }

                    if (interpreter.GetVariableValue(
                            VariableFlags.None, stdInVarName,
                            ref input, ref error) != ReturnCode.Ok)
                    {
                        return ReturnCode.Error;
                    }
                }

                if (stdInObjectVarName != null)
                {
                    if (interpreter == null)
                    {
                        error = "invalid interpreter";
                        return ReturnCode.Error;
                    }

                    Result result = null;

                    if (MarshalOps.FixupReturnValue(
                            interpreter, null, objectFlags, options,
                            null, ObjectOptionType.Default, null,
                            new object(), true, true, false,
                            ref result) != ReturnCode.Ok)
                    {
                        error = result;
                        return ReturnCode.Error;
                    }

                    string inputObjectName = result;

                    if (interpreter.GetObject(
                            inputObjectName, LookupFlags.Default,
                            ref inputObject, ref error) != ReturnCode.Ok)
                    {
                        return ReturnCode.Error;
                    }

                    if (interpreter.SetVariableValue(
                            VariableFlags.None, stdInObjectVarName,
                            inputObjectName, ref error) != ReturnCode.Ok)
                    {
                        return ReturnCode.Error;
                    }
                }
            }

            if (captureOutput)
            {
                if (stdOutCallback != null)
                {
                    outputHandler = stdOutCallback.Delegate as DataReceivedEventHandler;

                    if (outputHandler == null)
                    {
                        error = "option \"-stdoutcallback\" value has invalid callback";
                        return ReturnCode.Error;
                    }
                }

                if (stdErrCallback != null)
                {
                    errorHandler = stdErrCallback.Delegate as DataReceivedEventHandler;

                    if (errorHandler == null)
                    {
                        error = "option \"-stderrcallback\" value has invalid callback";
                        return ReturnCode.Error;
                    }
                }
            }

            if (startCallback != null)
            {
                startHandler = startCallback.Delegate as EventHandler;

                if (startHandler == null)
                {
                    error = "option \"-startcallback\" value has invalid callback";
                    return ReturnCode.Error;
                }
            }

            return ReturnCode.Ok;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method evaluates an optional pre-processing script, passing it
        /// the executable file name, directory, and arguments, allowing the
        /// script to rewrite the arguments or to indicate that execution should
        /// be skipped.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter context used to evaluate the pre-processing script.
        /// </param>
        /// <param name="list">
        /// The pre-processing script command list to evaluate, or null to do
        /// nothing.  The file name, directory, and arguments are appended to it.
        /// </param>
        /// <param name="execFileName">
        /// The executable file name to pass to the script, if any.
        /// </param>
        /// <param name="directory">
        /// The working directory to pass to the script, if any.
        /// </param>
        /// <param name="execArguments">
        /// On input, the command line arguments to pass to the script; upon a
        /// return result, this is updated with the rewritten arguments.
        /// </param>
        /// <param name="done">
        /// Upon return, this is non-zero if the script indicated (via a continue
        /// result) that execution should be skipped.
        /// </param>
        /// <param name="error">
        /// Upon failure, this contains an appropriate error message.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise,
        /// <see cref="ReturnCode.Error" />.
        /// </returns>
        public static ReturnCode PreProcessArguments(
            Interpreter interpreter,  /* in */
            StringList list,          /* in, out: OPTIONAL */
            string execFileName,      /* in: OPTIONAL */
            string directory,         /* in: OPTIONAL */
            ref string execArguments, /* in, out */
            ref bool done,            /* out */
            ref Result error          /* out */
            )
        {
            if (interpreter == null)
            {
                error = "invalid interpreter";
                return ReturnCode.Error;
            }

            if (list == null)
                return ReturnCode.Ok;

            list.Add(execFileName);
            list.Add(directory);
            list.Add(execArguments);

            ReturnCode code;
            Result result = null;

            code = interpreter.EvaluateScript(
                list.ToString(), ref result);

            if (code == ReturnCode.Error)
            {
                error = result;
            }
            else if (code == ReturnCode.Return)
            {
                execArguments = result;
                code = ReturnCode.Ok;
            }
            else if (code == ReturnCode.Continue)
            {
                done = true;
                code = ReturnCode.Ok;
            }

            return code;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method stores the results of a process execution into the
        /// caller-specified variables, including the process identifier, exit
        /// code, and captured standard output and error, optionally trimming
        /// whitespace and removing carriage returns, and setting the error code
        /// when the exit code does not match an expected success value.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter context whose variables should be set, if any.
        /// </param>
        /// <param name="processIdVarName">
        /// The name of the variable to receive the process identifier, if any.
        /// </param>
        /// <param name="exitCodeVarName">
        /// The name of the variable to receive the process exit code, if any.
        /// </param>
        /// <param name="stdOutVarName">
        /// The name of the variable to receive the captured standard output, if
        /// any.
        /// </param>
        /// <param name="stdErrVarName">
        /// The name of the variable to receive the captured standard error, if
        /// any.
        /// </param>
        /// <param name="processId">
        /// The identifier of the process that was executed; may be zero if no
        /// process was started.
        /// </param>
        /// <param name="exitCode">
        /// The exit code returned by the process.
        /// </param>
        /// <param name="successExitCode">
        /// The exit code that indicates success, or null if no such check
        /// should be performed.
        /// </param>
        /// <param name="attempted">
        /// Non-zero if execution of the process was actually attempted.
        /// </param>
        /// <param name="useShellExecute">
        /// Non-zero if the process was started via the shell, in which case no
        /// output is available.
        /// </param>
        /// <param name="background">
        /// Non-zero if the process was started in the background.
        /// </param>
        /// <param name="captureExitCode">
        /// Non-zero if the exit code should be stored.
        /// </param>
        /// <param name="captureOutput">
        /// Non-zero if the captured output should be stored.
        /// </param>
        /// <param name="setAll">
        /// Non-zero to store the output variables even when the overall return
        /// code indicates failure.
        /// </param>
        /// <param name="trimAll">
        /// Non-zero to trim surrounding whitespace from the captured output.
        /// </param>
        /// <param name="carriageReturns">
        /// Non-zero to retain carriage returns in the captured output; zero to
        /// remove them.
        /// </param>
        /// <param name="code">
        /// On input, the current return code; this may be updated to
        /// <see cref="ReturnCode.Error" /> when an abnormal exit code is
        /// detected.
        /// </param>
        /// <param name="result">
        /// On input, the captured standard output; upon failure, this is set to
        /// the error output.
        /// </param>
        /// <param name="error">
        /// On input, the captured standard error; it may be updated with an
        /// abnormal-exit error message.
        /// </param>
        /// <param name="setErrors">
        /// This accumulates any errors encountered while setting the result
        /// variables.
        /// </param>
        public static void HandleCaptureResults(
            Interpreter interpreter,   /* in: OPTIONAL */
            string processIdVarName,   /* in: OPTIONAL */
            string exitCodeVarName,    /* in: OPTIONAL */
            string stdOutVarName,      /* in: OPTIONAL */
            string stdErrVarName,      /* in: OPTIONAL */
            long processId,            /* in */
            ExitCode exitCode,         /* in */
            ExitCode? successExitCode, /* in: OPTIONAL */
            bool attempted,            /* in */
            bool useShellExecute,      /* in */
            bool background,           /* in */
            bool captureExitCode,      /* in */
            bool captureOutput,        /* in */
            bool setAll,               /* in */
            bool trimAll,              /* in */
            bool carriageReturns,      /* in */
            ref ReturnCode code,       /* in, out */
            ref Result result,         /* in, out */
            ref Result error,          /* in, out */
            ref ResultList setErrors   /* in, out */
            )
        {
            ReturnCode setCode; /* REUSED */
            Result setError; /* REUSED */

            ///////////////////////////////////////////////////////////////////

            //
            // NOTE: Even upon failure -OR- even if the actual
            //       process execution was not even attempted,
            //       always set the variable to contain process
            //       identifier, if applicable.  Its value may
            //       be zero to indicate that no process was
            //       started.
            //
            if ((interpreter != null) && (processIdVarName != null))
            {
                setError = null;

                setCode = interpreter.SetVariableValue(
                    VariableFlags.NoReady, processIdVarName,
                    processId.ToString(), null, ref setError);

                if ((setCode != ReturnCode.Ok) &&
                    (setError != null))
                {
                    if (setErrors == null)
                        setErrors = new ResultList();

                    setErrors.Add(setError);
                }
            }

            ///////////////////////////////////////////////////////////////////

            if (attempted)
            {
                //
                // NOTE: Remove carriage returns from output (leaving
                //       only line feeds as line separators)?
                //
                if (!carriageReturns)
                {
                    if (!String.IsNullOrEmpty(result))
                    {
                        result = result.Replace(
                            Characters.CarriageReturnString,
                            String.Empty);
                    }

                    if (!String.IsNullOrEmpty(error))
                    {
                        error = error.Replace(
                            Characters.CarriageReturnString,
                            String.Empty);
                    }
                }

                ///////////////////////////////////////////////////////////////

                //
                // NOTE: Remove all surrounding whitespace from the
                //       output?
                //
                if (trimAll)
                {
                    if (!String.IsNullOrEmpty(result))
                        result = result.Trim();

                    if (!String.IsNullOrEmpty(error))
                        error = error.Trim();
                }

                ///////////////////////////////////////////////////////////////

                if ((interpreter != null) &&
                    ((code == ReturnCode.Ok) || setAll))
                {
                    //
                    // NOTE: Now, "result" contains any StdOut output
                    //       and "error" contains any StdErr output.
                    //
                    if (!background &&
                        captureExitCode && (exitCodeVarName != null))
                    {
                        setError = null;

                        setCode = interpreter.SetVariableValue(
                            VariableFlags.None, exitCodeVarName,
                            exitCode.ToString(), null, ref setError);

                        if ((setCode != ReturnCode.Ok) &&
                            (setError != null))
                        {
                            if (setErrors == null)
                                setErrors = new ResultList();

                            setErrors.Add(setError);
                        }
                    }

                    ///////////////////////////////////////////////////////////

                    if (!useShellExecute && !background &&
                        captureOutput && (stdOutVarName != null))
                    {
                        setError = null;

                        setCode = interpreter.SetVariableValue(
                            VariableFlags.None, stdOutVarName,
                            result, null, ref setError);

                        if ((setCode != ReturnCode.Ok) &&
                            (setError != null))
                        {
                            if (setErrors == null)
                                setErrors = new ResultList();

                            setErrors.Add(setError);
                        }
                    }

                    ///////////////////////////////////////////////////////////

                    if (!useShellExecute && !background &&
                        captureOutput && (stdErrVarName != null))
                    {
                        setError = null;

                        setCode = interpreter.SetVariableValue(
                            VariableFlags.None, stdErrVarName,
                            error, null, ref setError);

                        if ((setCode != ReturnCode.Ok) &&
                            (setError != null))
                        {
                            if (setErrors == null)
                                setErrors = new ResultList();

                            setErrors.Add(setError);
                        }
                    }

                    ///////////////////////////////////////////////////////////

                    //
                    // NOTE: If the caller specified a success exit code,
                    //       make sure that is the same as the exit code
                    //       we actually received from the process.
                    //
                    if (!background && captureExitCode &&
                        (successExitCode != null) &&
                        (exitCode != successExitCode))
                    {
                        setError = null;

                        setCode = interpreter.SetVariableValue(
                            Engine.ErrorCodeVariableFlags,
                            TclVars.Core.ErrorCode, StringList.MakeList(
                                TclVars.Core.ChildStatus, processId,
                                exitCode),
                            null, ref setError);

                        if ((setCode != ReturnCode.Ok) &&
                            (setError != null))
                        {
                            if (setErrors == null)
                                setErrors = new ResultList();

                            setErrors.Add(setError);
                        }

                        if (code == ReturnCode.Ok)
                        {
                            if (setCode == ReturnCode.Ok)
                                Engine.SetErrorCodeSet(interpreter, true);

                            error = "child process exited abnormally";
                            code = ReturnCode.Error;
                        }
                    }
                }
            }

            ///////////////////////////////////////////////////////////////////

            if (code != ReturnCode.Ok)
                result = error;
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Private KillProcess Helper Methods
        /// <summary>
        /// This method terminates the specified process, either forcibly or by
        /// requesting that its main window close, refusing to kill the current
        /// process unless explicitly permitted.
        /// </summary>
        /// <param name="process">
        /// The process to terminate.
        /// </param>
        /// <param name="self">
        /// Non-zero to allow terminating the current (this) process.
        /// </param>
        /// <param name="force">
        /// Non-zero to forcibly kill the process; zero to request that its main
        /// window close.
        /// </param>
        /// <param name="whatIf">
        /// Non-zero to only trace what would be done, without actually killing
        /// the process.
        /// </param>
        /// <param name="verbose">
        /// Non-zero to emit a diagnostic trace describing the terminated
        /// process.
        /// </param>
        /// <param name="results">
        /// This accumulates a description of each process that was successfully
        /// killed or closed.
        /// </param>
        /// <param name="errors">
        /// This accumulates any errors encountered while terminating the
        /// process.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise,
        /// <see cref="ReturnCode.Error" />.
        /// </returns>
        private static ReturnCode KillProcess(
            Process process,        /* in */
            bool self,              /* in */
            bool force,             /* in */
            bool whatIf,            /* in */
            bool verbose,           /* in */
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
                    if (whatIf)
                    {
                        TraceOps.DebugTrace(String.Format(
                            "KillProcess: KILL {0}",
                            FormatOps.ProcessName(process, true)),
                            typeof(ProcessOps).Name,
                            TracePriority.ProcessDebug2);
                    }
                    else
                    {
                        process.Kill(); /* throw */
                    }

                    if (verbose)
                    {
                        TraceOps.DebugTrace(String.Format(
                            "KillProcess: {0}",
                            FormatOps.ProcessName(process, true)),
                            typeof(ProcessOps).Name,
                            TracePriority.ProcessDebug);
                    }

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

        /// <summary>
        /// This method opens the process with the specified identifier and then
        /// terminates it.
        /// </summary>
        /// <param name="id">
        /// The identifier of the process to terminate.
        /// </param>
        /// <param name="self">
        /// Non-zero to allow terminating the current (this) process.
        /// </param>
        /// <param name="force">
        /// Non-zero to forcibly kill the process; zero to request that its main
        /// window close.
        /// </param>
        /// <param name="whatIf">
        /// Non-zero to only trace what would be done, without actually killing
        /// the process.
        /// </param>
        /// <param name="verbose">
        /// Non-zero to emit a diagnostic trace describing the terminated
        /// process.
        /// </param>
        /// <param name="results">
        /// This accumulates a description of each process that was successfully
        /// killed or closed.
        /// </param>
        /// <param name="errors">
        /// This accumulates any errors encountered while terminating the
        /// process.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise,
        /// <see cref="ReturnCode.Error" />.
        /// </returns>
        private static ReturnCode KillProcess(
            int id,                 /* in */
            bool self,              /* in */
            bool force,             /* in */
            bool whatIf,            /* in */
            bool verbose,           /* in */
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
                process, self, force, whatIf, verbose, ref results,
                ref errors);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method enumerates all running processes and terminates those
        /// whose name or file name matches the specified pattern, stopping
        /// after the first match unless terminating all matches.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter context to use when performing the pattern match.
        /// </param>
        /// <param name="mode">
        /// The string matching mode to use.
        /// </param>
        /// <param name="pattern">
        /// The pattern to match against process names and file names.
        /// </param>
        /// <param name="noCase">
        /// Non-zero to perform a case-insensitive match.
        /// </param>
        /// <param name="all">
        /// Non-zero to terminate all matching processes; zero to terminate only
        /// the first match.
        /// </param>
        /// <param name="self">
        /// Non-zero to allow terminating the current (this) process.
        /// </param>
        /// <param name="force">
        /// Non-zero to forcibly kill the processes; zero to request that their
        /// main windows close.
        /// </param>
        /// <param name="whatIf">
        /// Non-zero to only trace what would be done, without actually killing
        /// any process.
        /// </param>
        /// <param name="verbose">
        /// Non-zero to emit a diagnostic trace describing each terminated
        /// process.
        /// </param>
        /// <param name="results">
        /// This accumulates a description of each process that was successfully
        /// killed or closed.
        /// </param>
        /// <param name="errors">
        /// This accumulates any errors encountered while terminating the
        /// processes.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise,
        /// <see cref="ReturnCode.Error" />.
        /// </returns>
        private static ReturnCode KillProcess(
            Interpreter interpreter, /* in */
            MatchMode mode,          /* in */
            string pattern,          /* in */
            bool noCase,             /* in */
            bool all,                /* in */
            bool self,               /* in */
            bool force,              /* in */
            bool whatIf,             /* in */
            bool verbose,            /* in */
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
                    process, self, force, whatIf, verbose,
                    ref results, ref errors);

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

        #region Private WaitForExit Helper Methods
        /// <summary>
        /// This method waits for the specified process to exit, either for a
        /// finite timeout or, when permitted and not otherwise disabled,
        /// indefinitely.  When waiting forever is not allowed, a finite timeout
        /// is computed and the wait is retried.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter context to consult for timeout values, if any.
        /// </param>
        /// <param name="process">
        /// The process to wait on.
        /// </param>
        /// <param name="timeout">
        /// The timeout, in milliseconds, to wait, or null to wait without an
        /// explicit timeout.
        /// </param>
        /// <param name="canWaitForever">
        /// Non-zero to permit waiting indefinitely when no timeout is supplied.
        /// </param>
        /// <param name="hasExited">
        /// Upon return, this is non-zero if the process has exited; otherwise,
        /// zero.
        /// </param>
        /// <returns>
        /// True if the wait completed (whether or not the process had exited);
        /// otherwise, false.
        /// </returns>
        private static bool WaitForExit(
            Interpreter interpreter, /* in: OPTIONAL */
            Process process,         /* in */
            int? timeout,            /* in: OPTIONAL */
            bool canWaitForever,     /* in */
            ref bool hasExited       /* out */
            )
        {
            if (process == null)
                return false;

        retry:

            int localTimeout;

            if (timeout != null)
            {
                localTimeout = GetWaitForExitTimeout(
                    interpreter, timeout);

                if (process.WaitForExit(
                        localTimeout)) /* throw */
                {
                    hasExited = true;

                    TraceOps.DebugTrace(String.Format(
                        "WaitForExit: EXITED {0}, timeout {1}",
                        EntityOps.GetNameOrIdNoThrow(process),
                        localTimeout), typeof(ProcessOps).Name,
                        TracePriority.ProcessDebug2);
                }
                else
                {
                    TraceOps.DebugTrace(String.Format(
                        "WaitForExit: RUNNING {0}, timeout {1}",
                        EntityOps.GetNameOrIdNoThrow(process),
                        localTimeout), typeof(ProcessOps).Name,
                        TracePriority.ProcessDebug);
                }
            }
            else
            {
                if (!canWaitForever || DoNotWaitForever) /* RARE */
                {
                    localTimeout = GetWaitForExitTimeout(
                        interpreter, timeout);

                    TraceOps.DebugTrace(String.Format(
                        "WaitForExit: RETRYING {0}, timeout {1}",
                        EntityOps.GetNameOrIdNoThrow(process),
                        localTimeout), typeof(ProcessOps).Name,
                        TracePriority.ProcessDebug2);

                    timeout = localTimeout;
                    goto retry;
                }
                else
                {
                    TraceOps.DebugTrace(String.Format(
                        "WaitForExit: WAITING {0}, no timeout",
                        EntityOps.GetNameOrIdNoThrow(process)),
                        typeof(ProcessOps).Name,
                        TracePriority.ProcessDebug2);

                    process.WaitForExit(); /* throw */
                    hasExited = true;

                    TraceOps.DebugTrace(String.Format(
                        "WaitForExit: EXITED {0}, no timeout",
                        EntityOps.GetNameOrIdNoThrow(process)),
                        typeof(ProcessOps).Name,
                        TracePriority.ProcessDebug2);
                }
            }

            return true;
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Public ExecuteProcess Methods
        /// <summary>
        /// This method is the primary entry point for executing an external
        /// (child) process.  It validates and normalizes the file name and
        /// working directory, creates and starts the process, optionally
        /// redirects input and captures output, waits for the process to exit
        /// (unless run in the background) while optionally processing events,
        /// and returns the exit code and captured output to the caller.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter context to use, if any.
        /// </param>
        /// <param name="domainName">
        /// The domain name for the logon, if any.
        /// </param>
        /// <param name="userName">
        /// The user name for the logon, if any.
        /// </param>
        /// <param name="password">
        /// The password for the logon, if any.
        /// </param>
        /// <param name="fileName">
        /// The executable file for the new process.
        /// </param>
        /// <param name="arguments">
        /// The command line arguments for the new process, if any.
        /// </param>
        /// <param name="directory">
        /// The working directory for the new process, if any.
        /// </param>
        /// <param name="input">
        /// The simulated string input for the new process, if any.
        /// </param>
        /// <param name="inputObject">
        /// The opaque object handle where the standard input stream should be
        /// stored.
        /// </param>
        /// <param name="startHandler">
        /// The event handler to be called right after the process is started.
        /// </param>
        /// <param name="outputLogPath">
        /// The optional log path where captured output data should be appended.
        /// </param>
        /// <param name="errorLogPath">
        /// The optional log path where captured error data should be appended.
        /// </param>
        /// <param name="outputHandler">
        /// The raw event handler for output data coming from the new process,
        /// if any.
        /// </param>
        /// <param name="errorHandler">
        /// The raw event handler for error data coming from the new process, if
        /// any.
        /// </param>
        /// <param name="logTag">
        /// The optional tag string to include in the log file(s), if any.
        /// </param>
        /// <param name="windowStyle">
        /// The window style (normal, minimized, etc.) for the new process.
        /// </param>
        /// <param name="eventFlags">
        /// The event flags to use while waiting for the new process to exit.
        /// </param>
        /// <param name="timeout">
        /// The number of milliseconds to wait for the process to exit between
        /// processing events.
        /// </param>
        /// <param name="useShellExecute">
        /// Non-zero to use ShellExecute instead of CreateProcess.
        /// </param>
        /// <param name="captureExitCode">
        /// Non-zero to populate the exit code parameter.
        /// </param>
        /// <param name="captureOutput">
        /// Non-zero to populate the result and error parameters.
        /// </param>
        /// <param name="useUnicode">
        /// Non-zero if captured output from the process will be Unicode.
        /// </param>
        /// <param name="ignoreStdErr">
        /// Non-zero to not capture output written to standard error (COMPAT:
        /// Tcl).
        /// </param>
        /// <param name="overrideCapture">
        /// Non-zero to use the supplied events to replace the built-in ones
        /// (instead of wrapping them).
        /// </param>
        /// <param name="userInterface">
        /// Non-zero if this thread needs to process window messages before any
        /// sleep operation.
        /// </param>
        /// <param name="noSleep">
        /// Non-zero to avoid sleeping while waiting for the process to exit.
        /// </param>
        /// <param name="killOnError">
        /// Non-zero to kill the process on an interpreter error (e.g. disposed,
        /// exited, etc).
        /// </param>
        /// <param name="keepNewLine">
        /// Zero to remove the final carriage-return/line-feed pair from the
        /// output.
        /// </param>
        /// <param name="background">
        /// Non-zero to prevent waiting on the child process to exit.
        /// </param>
        /// <param name="events">
        /// Non-zero to process events while waiting (non-background only).
        /// </param>
        /// <param name="noPreviousProcessId">
        /// Non-zero to not set and/or reset the previous process identifier of
        /// the interpreter.
        /// </param>
        /// <param name="trace">
        /// Non-zero to emit a trace with the final command line.
        /// </param>
        /// <param name="id">
        /// Upon returning, this contains the identifier of the started process,
        /// if any.
        /// </param>
        /// <param name="exitCode">
        /// Upon success, this contains the exit code from the child process.
        /// </param>
        /// <param name="result">
        /// Upon success, this contains the output captured from standard
        /// output.
        /// </param>
        /// <param name="error">
        /// Upon success, this contains the output captured from standard error;
        /// otherwise, it contains error information.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise,
        /// <see cref="ReturnCode.Error" />.
        /// </returns>
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
            string outputLogPath,           /* in: Optional log path where
                                             *     captured output data should
                                             *     be appended. */
            string errorLogPath,            /* in: Optional log path where
                                             *     captured error data should
                                             *     be appended. */
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
            string logTag,                  /* in: The optional "tag" string to
                                             *     include in the log file(s),
                                             *     if any. */
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
            bool noPreviousProcessId,       /* in: Do NOT set and/or reset the
                                             *     PreviousProcessId of the
                                             *     interpreter. */
            bool trace,                     /* in: Non-zero to emit a trace with
                                             *     the final command line. */
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
            InitializeOutputsAndErrors(captureOutput, overrideCapture);

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
                workingDirectory, input, inputObject, outputLogPath,
                errorLogPath, windowStyle, useShellExecute,
                captureOutput, useUnicode, ignoreStdErr, background,
                ref localError);

            if (startInfo == null)
            {
                error = localError;
                return ReturnCode.Error;
            }

            if (trace)
            {
                IDebugHost debugHost = (interpreter != null) ?
                    interpreter.InternalHost as IDebugHost : null;

                DebugOps.WriteWithoutFail(debugHost,
                    String.Format("ExecuteProcess: {0} {1}",
                    FormatOps.WrapOrNull(startInfo.FileName),
                    FormatOps.WrapOrNull(startInfo.Arguments)),
                    true, true);
            }

            /* IGNORED */
            MaybeAppendStartInfoToLogPaths(outputLogPath,
                errorLogPath, "START", logTag, startInfo);

            //
            // NOTE: Attempt to create child process OBJECT.  This does
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

                if (PreSetupForCapture(
                        startInfo, process, outputLogPath, errorLogPath,
                        outputHandler, errorHandler, StringBuilderCapacity,
                        overrideCapture, ref localError) != ReturnCode.Ok)
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
                StreamWriter standardInput = null;

                localError = null;

                if (PostSetupForCapture(
                        startInfo, process, input,
                        inputObject, ref standardInput,
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
                // NOTE: At this point, we must close the standard input
                //       pipe for the child process as it may be waiting
                //       for this to be closed for it to exit.
                //
                if (standardInput != null)
                {
                    standardInput.Close();
                    standardInput = null;
                }

                //
                // NOTE: Give caller the Id for newly started process.
                //       This value could be zero if a process was not
                //       actually started -OR- an existing browser was
                //       reused (etc) -OR- the process was started in
                //       the background.
                //
                id = 0;

                if (!TryGetIdAndPassToInterpreter(
                        interpreter, process, noPreviousProcessId,
                        ref id) || background)
                {
                    //
                    // NOTE: If one or both of the log paths are in use,
                    //       process events on a thread-pool thread and
                    //       then cleanup the in-memory log path state.
                    //
                    if (background && ((outputLogPath != null) ||
                        (errorLogPath != null)))
                    {
                        ThreadOps.QueueUserWorkItem(
                            new WaitCallback(ProcessWaitCallback),
                            new ProcessWaitInfo(
                                interpreter, startInfo, process,
                                outputLogPath, errorLogPath, logTag,
                                timeout, eventFlags, userInterface,
                                noSleep, killOnError, background
                            ), true);
                    }

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
                                interpreter, startInfo, process,
                                outputLogPath, errorLogPath, logTag,
                                timeout, eventFlags, userInterface,
                                noSleep, killOnError, background,
                                ref waitForExit,
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
                    //       process has *actually* exited (apparently,
                    //       the HasExited property cannot always be
                    //       trusted).  Also, if the caller did not
                    //       choose to process events while waiting,
                    //       this should keep us synchronized anyhow.
                    //
                    bool didWaitForExit = false;

                    if (waitForExit)
                    {
                        /* IGNORED */
                        WaitForExit(
                            interpreter, process, timeout, true,
                            ref didWaitForExit); /* throw */
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
                            /* IGNORED */
                            WaitForExit(
                                interpreter, process, null, true,
                                ref didWaitForExit); /* throw */
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
                            /* IGNORED */
                            WaitForExit(
                                interpreter, process, null, true,
                                ref didWaitForExit); /* throw */
                        }

                        return GetCaptureData(
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
                if (!background ||
                    ((outputLogPath == null) && (errorLogPath == null)))
                {
                    /* IGNORED */
                    MaybeAppendStartInfoToLogPaths(outputLogPath,
                        errorLogPath, "END", logTag, startInfo);

                    ReturnCode terminateCode;
                    Result terminateError = null;

                    terminateCode = TerminateForCapture(
                        startInfo, process, ref terminateError);

                    if (terminateCode != ReturnCode.Ok)
                    {
                        DebugOps.Complain(
                            interpreter, terminateCode, terminateError);
                    }
                }
            }

            return ReturnCode.Error;
        }

        ///////////////////////////////////////////////////////////////////////

        //
        // NOTE: For use by the [test2] command only.
        //
        /// <summary>
        /// This method is a convenience overload that executes an external
        /// process with output capture enabled, for use by the [test2] command
        /// only.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter context to use, if any.
        /// </param>
        /// <param name="fileName">
        /// The executable file for the new process.
        /// </param>
        /// <param name="arguments">
        /// The command line arguments for the new process, if any.
        /// </param>
        /// <param name="directory">
        /// The working directory for the new process, if any.
        /// </param>
        /// <param name="eventFlags">
        /// The event flags to use while waiting for the new process to exit.
        /// </param>
        /// <param name="useUnicode">
        /// Non-zero if captured output from the process will be Unicode.
        /// </param>
        /// <param name="id">
        /// Upon returning, this contains the identifier of the started process,
        /// if any.
        /// </param>
        /// <param name="exitCode">
        /// Upon success, this contains the exit code from the child process.
        /// </param>
        /// <param name="result">
        /// Upon success, this contains the output captured from standard
        /// output.
        /// </param>
        /// <param name="error">
        /// Upon success, this contains the output captured from standard error;
        /// otherwise, it contains error information.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise,
        /// <see cref="ReturnCode.Error" />.
        /// </returns>
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
            return ExecuteProcess(
                interpreter, null, null, null, fileName, arguments,
                directory, null, null, null, null, null, null, null,
                null, ProcessWindowStyle.Normal, eventFlags, null,
                false, true, true, useUnicode, false, false, false,
                false, false, true, false, true, false, false,
                ref id, ref exitCode, ref result, ref error);
        }

        ///////////////////////////////////////////////////////////////////////

        //
        // NOTE: For use by the PlatformOps.GetInstalledUpdates
        //       and ScriptOps.ExtractToDirectory methods only.
        //
        /// <summary>
        /// This method is a convenience overload that executes an external
        /// process with exit code and output capture enabled, for use by the
        /// PlatformOps.GetInstalledUpdates and ScriptOps.ExtractToDirectory
        /// methods only.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter context to use, if any.
        /// </param>
        /// <param name="fileName">
        /// The executable file for the new process.
        /// </param>
        /// <param name="arguments">
        /// The command line arguments for the new process, if any.
        /// </param>
        /// <param name="eventFlags">
        /// The event flags to use while waiting for the new process to exit.
        /// </param>
        /// <param name="exitCode">
        /// Upon success, this contains the exit code from the child process.
        /// </param>
        /// <param name="result">
        /// Upon success, this contains the output captured from standard
        /// output.
        /// </param>
        /// <param name="error">
        /// Upon success, this contains the output captured from standard error;
        /// otherwise, it contains error information.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise,
        /// <see cref="ReturnCode.Error" />.
        /// </returns>
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

            return ExecuteProcess(
                interpreter, null, null, null, fileName, arguments,
                null, null, null, null, null, null, null, null, null,
                ProcessWindowStyle.Normal, eventFlags, null, false,
                true, true, false, false, false, false, false, true,
                false, false, true, false, false, ref id, ref exitCode,
                ref result, ref error);
        }

        ///////////////////////////////////////////////////////////////////////

#if SHELL && INTERACTIVE_COMMANDS
        //
        // NOTE: For use by the interactive "#website" command only.
        //
        /// <summary>
        /// This method is a convenience overload that executes an external
        /// process via the shell, without capturing output, for use by the
        /// interactive "#website" command only.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter context to use, if any.
        /// </param>
        /// <param name="fileName">
        /// The executable file (or document) for the new process.
        /// </param>
        /// <param name="arguments">
        /// The command line arguments for the new process, if any.
        /// </param>
        /// <param name="directory">
        /// The working directory for the new process, if any.
        /// </param>
        /// <param name="eventFlags">
        /// The event flags to use while waiting for the new process to exit.
        /// </param>
        /// <param name="error">
        /// Upon failure, this contains an appropriate error message.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise,
        /// <see cref="ReturnCode.Error" />.
        /// </returns>
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

            return ExecuteProcess(
                interpreter, null, null, null, fileName, arguments,
                directory, null, null, null, null, null, null, null,
                null, ProcessWindowStyle.Normal, eventFlags, null,
                true, false, false, false, false, false, false,
                false, false, false, false, true, false, false,
                ref id, ref exitCode, ref result, ref error);
        }

        ///////////////////////////////////////////////////////////////////////

        //
        // NOTE: For use by the interactive "#cmd" command only.
        //
        /// <summary>
        /// This method is a convenience overload that executes an external
        /// process without capturing output, for use by the interactive "#cmd"
        /// command only.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter context to use, if any.
        /// </param>
        /// <param name="fileName">
        /// The executable file for the new process.
        /// </param>
        /// <param name="arguments">
        /// The command line arguments for the new process, if any.
        /// </param>
        /// <param name="directory">
        /// The working directory for the new process, if any.
        /// </param>
        /// <param name="eventFlags">
        /// The event flags to use while waiting for the new process to exit.
        /// </param>
        /// <param name="error">
        /// Upon failure, this contains an appropriate error message.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise,
        /// <see cref="ReturnCode.Error" />.
        /// </returns>
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

            return ExecuteProcess(
                interpreter, null, null, null, fileName, arguments,
                directory, null, null, null, null, null, null, null,
                null, ProcessWindowStyle.Normal, eventFlags, null,
                false, false, false, false, false, false, false,
                false, false, false, false, true, false, false,
                ref id, ref exitCode, ref result, ref error);
        }

        ///////////////////////////////////////////////////////////////////////

        //
        // NOTE: For use by the interactive "#tclshrc" command only.
        //
        /// <summary>
        /// This method is a convenience overload that executes an external
        /// process without capturing output, optionally in the background, for
        /// use by the interactive "#tclshrc" command only.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter context to use, if any.
        /// </param>
        /// <param name="fileName">
        /// The executable file for the new process.
        /// </param>
        /// <param name="arguments">
        /// The command line arguments for the new process, if any.
        /// </param>
        /// <param name="directory">
        /// The working directory for the new process, if any.
        /// </param>
        /// <param name="eventFlags">
        /// The event flags to use while waiting for the new process to exit.
        /// </param>
        /// <param name="background">
        /// Non-zero to prevent waiting on the child process to exit.
        /// </param>
        /// <param name="error">
        /// Upon failure, this contains an appropriate error message.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise,
        /// <see cref="ReturnCode.Error" />.
        /// </returns>
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

            return ExecuteProcess(
                interpreter, null, null, null, fileName, arguments,
                directory, null, null, null, null, null, null, null,
                null, ProcessWindowStyle.Normal, eventFlags, null,
                false, false, false, false, false, false, false,
                false, false, false, background, true, false, false,
                ref id, ref exitCode, ref result, ref error);
        }
#endif
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Public KillProcess Methods
        /// <summary>
        /// This method terminates one or more processes identified either by a
        /// numeric process identifier or by a name/file-name pattern.  When a
        /// numeric identifier is supplied, the all option is not permitted.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter context to use when performing the pattern match.
        /// </param>
        /// <param name="idOrPattern">
        /// The numeric process identifier or the pattern to match against
        /// process names and file names.
        /// </param>
        /// <param name="cultureInfo">
        /// The culture to use when parsing a numeric process identifier.
        /// </param>
        /// <param name="all">
        /// Non-zero to terminate all matching processes; this cannot be used
        /// with a numeric process identifier.
        /// </param>
        /// <param name="self">
        /// Non-zero to allow terminating the current (this) process.
        /// </param>
        /// <param name="force">
        /// Non-zero to forcibly kill the processes; zero to request that their
        /// main windows close.
        /// </param>
        /// <param name="whatIf">
        /// Non-zero to only trace what would be done, without actually killing
        /// any process.
        /// </param>
        /// <param name="verbose">
        /// Non-zero to emit a diagnostic trace describing each terminated
        /// process.
        /// </param>
        /// <param name="result">
        /// Upon success, this contains a description of the terminated
        /// processes; upon failure, it contains the combined results and error
        /// information.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise,
        /// <see cref="ReturnCode.Error" />.
        /// </returns>
        public static ReturnCode KillProcess(
            Interpreter interpreter, /* in */
            string idOrPattern,      /* in */
            CultureInfo cultureInfo, /* in */
            bool all,                /* in */
            bool self,               /* in */
            bool force,              /* in */
            bool whatIf,             /* in */
            bool verbose,            /* in */
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

                if (KillProcess(
                        id, self, force, whatIf, verbose, ref results,
                        ref errors) == ReturnCode.Ok)
                {
                    result = results;
                    return ReturnCode.Ok;
                }
            }
            else
            {
                if (KillProcess(
                        interpreter, MatchMode.Glob, idOrPattern,
                        PathOps.NoCase, all, self, force, whatIf,
                        verbose, ref results, ref errors) == ReturnCode.Ok)
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
        /// <summary>
        /// This method validates received output or error data according to the
        /// specified check level, progressively checking for null/empty,
        /// spaces-only, and out-of-range character values.
        /// </summary>
        /// <param name="data">
        /// The received data to validate.
        /// </param>
        /// <param name="level">
        /// The check level that controls how much validation is performed.
        /// </param>
        /// <param name="error">
        /// Upon failure, this describes why the data was considered suspect.
        /// </param>
        /// <returns>
        /// True if the data passed all applicable checks; otherwise, false.
        /// </returns>
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
        /// <summary>
        /// This method is the built-in handler for standard output data
        /// received from a process.  It validates the data, appends it to the
        /// in-memory capture and log file, and invokes any caller-supplied
        /// output handler, tracing when the data cannot be captured anywhere.
        /// </summary>
        /// <param name="sender">
        /// The process that produced the output data.
        /// </param>
        /// <param name="e">
        /// The event arguments containing the received output data.
        /// </param>
        private static void OutputDataReceived(
            object sender,          /* in */
            DataReceivedEventArgs e /* in */
            )
        {
            string methodName = "OutputDataReceived";
            Process process = sender as Process;

            if ((process == null) || (e == null))
            {
                TraceOps.DebugTrace(String.Format(
                    "{0}: missing process = {1} -OR- e = {2}",
                    methodName, FormatOps.NullOrNotNull(process),
                    FormatOps.NullOrNotNull(e)),
                    typeof(ProcessOps).Name,
                    TracePriority.ProcessError2);

                return;
            }

            string data = e.Data;

            MaybeCheckDataReceived(methodName, data);

            bool[] success = { false, false, false };

            success[0] = AppendOutputDataToCapture(process, data);
            success[1] = AppendOutputDataToLogPath(process, data);

            success[2] = MaybeInvokeDataReceivedEventHandler(
                GetOutputHandler(process), sender, e);

            if (!success[0] && !success[1] && !success[2])
            {
                TraceOps.DebugTrace(String.Format(
                    "{0}: cannot capture for {1} " +
                    "({2}, {3}, {4}): dropping {5}",
                    methodName, FormatOps.WrapOrNull(
                        EntityOps.GetNameOrIdNoThrow(process)),
                    success[0], success[1], success[2],
                    FormatOps.DisplayStringLength(data)),
                    typeof(ProcessOps).Name,
                    TracePriority.ProcessError2);
            }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method is the built-in handler for standard error data received
        /// from a process.  It validates the data, appends it to the in-memory
        /// capture and log file, and invokes any caller-supplied error handler,
        /// tracing when the data cannot be captured anywhere.
        /// </summary>
        /// <param name="sender">
        /// The process that produced the error data.
        /// </param>
        /// <param name="e">
        /// The event arguments containing the received error data.
        /// </param>
        private static void ErrorDataReceived(
            object sender,          /* in */
            DataReceivedEventArgs e /* in */
            )
        {
            string methodName = "ErrorDataReceived";
            Process process = sender as Process;

            if ((process == null) || (e == null))
            {
                TraceOps.DebugTrace(String.Format(
                    "{0}: missing process = {1} -OR- e = {2}",
                    methodName, FormatOps.NullOrNotNull(process),
                    FormatOps.NullOrNotNull(e)),
                    typeof(ProcessOps).Name,
                    TracePriority.ProcessError2);

                return;
            }

            string data = e.Data;

            MaybeCheckDataReceived(methodName, data);

            bool[] success = { false, false, false };

            success[0] = AppendErrorDataToCapture(process, data);
            success[1] = AppendErrorDataToLogPath(process, data);

            success[2] = MaybeInvokeDataReceivedEventHandler(
                GetErrorHandler(process), sender, e);

            if (!success[0] && !success[1] && !success[2])
            {
                TraceOps.DebugTrace(String.Format(
                    "{0}: cannot capture for {1} " +
                    "({2}, {3}, {4}): dropping {5}",
                    methodName, FormatOps.WrapOrNull(
                        EntityOps.GetNameOrIdNoThrow(process)),
                    success[0], success[1], success[2],
                    FormatOps.DisplayStringLength(data)),
                    typeof(ProcessOps).Name,
                    TracePriority.ProcessError2);
            }
        }
        #endregion
    }
}
