/*
 * TraceOps.cs --
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
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows.Forms;
using Eagle._Components.Private.Delegates;
using Eagle._Components.Shared;

namespace Eagle._Components.Private
{
    /// <summary>
    /// This class provides helper methods for emitting diagnostic trace output
    /// and for displaying message boxes to the user during the update process.
    /// </summary>
    [Guid("84055eb4-6bac-4471-880a-6f9da34c5a5b")]
    internal static class TraceOps
    {
        #region Private Constants
        /// <summary>
        /// The format string used to render a date and time value as an ISO-8601
        /// style timestamp.
        /// </summary>
        private const string Iso8601DateTimeOutputFormat =
            "yyyy.MM.ddTHH:mm:ss.fffffff";
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Private Data
        /// <summary>
        /// The object used to synchronize access to the shared state of this
        /// class.
        /// </summary>
        private static readonly object syncRoot = new object();
        /// <summary>
        /// The most recently issued trace identifier, used as a counter by
        /// <see cref="NextId" />.
        /// </summary>
        private static long nextId;

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// The set of method names that should be skipped when searching the
        /// call stack for the originating method name.
        /// </summary>
        private static readonly IDictionary<string, bool> skipNames =
            DictionaryFromPairs<string, bool>(new AnyPair<string, bool>[] {
                new AnyPair<string, bool>("QueueStatus", false),
                new AnyPair<string, bool>("QueueTrace", false),
                new AnyPair<string, bool>("Trace", false),
                new AnyPair<string, bool>("TraceAndUpdateStatus", false),
                // new AnyPair<string, bool>("UpdateStatusUri", false)
            }, true);
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Miscellaneous Support Methods
        /// <summary>
        /// This method builds a dictionary from a sequence of key/value pairs.
        /// </summary>
        /// <typeparam name="T1">
        /// The type of the keys in the resulting dictionary.
        /// </typeparam>
        /// <typeparam name="T2">
        /// The type of the values in the resulting dictionary.
        /// </typeparam>
        /// <param name="enumerable">
        /// The sequence of key/value pairs to add to the dictionary.  This
        /// parameter may be null.
        /// </param>
        /// <param name="unique">
        /// Non-zero to require that every key is unique (adding each pair and
        /// throwing on a duplicate); zero to allow later pairs to overwrite
        /// earlier ones.
        /// </param>
        /// <returns>
        /// The newly created dictionary, or null if <paramref name="enumerable" />
        /// is null.
        /// </returns>
        private static IDictionary<T1, T2> DictionaryFromPairs<T1, T2>(
            IEnumerable<AnyPair<T1, T2>> enumerable,
            bool unique
            )
        {
            if (enumerable == null)
                return null;

            IDictionary<T1, T2> result = new Dictionary<T1, T2>();

            foreach (AnyPair<T1, T2> anyPair in enumerable)
            {
                if (unique)
                    result.Add(anyPair.X, anyPair.Y);
                else
                    result[anyPair.X] = anyPair.Y;
            }

            return result;
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Interactive Support Methods
        /// <summary>
        /// This method traces a message and, when appropriate, displays it to
        /// the user in a message box.
        /// </summary>
        /// <param name="configuration">
        /// The configuration that controls tracing and prompting behavior.  This
        /// parameter may be null.
        /// </param>
        /// <param name="assembly">
        /// The assembly used to derive the message box title.  This parameter
        /// may be null.
        /// </param>
        /// <param name="message">
        /// The message text to trace and optionally display.
        /// </param>
        /// <param name="category">
        /// The trace category associated with the message.
        /// </param>
        /// <param name="buttons">
        /// The set of buttons to display in the message box.
        /// </param>
        /// <param name="icon">
        /// The icon to display in the message box.
        /// </param>
        /// <returns>
        /// The dialog result indicating the user's choice, or the default
        /// result when the message box is not displayed.
        /// </returns>
        public static DialogResult ShowMessage(
            Configuration configuration,
            Assembly assembly,
            string message,
            string category,
            MessageBoxButtons buttons,
            MessageBoxIcon icon
            )
        {
            return ShowMessage(
                configuration, assembly, message, category, buttons, icon,
                null);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method traces a message and, when appropriate, displays it to
        /// the user in a message box, using the specified default dialog result.
        /// </summary>
        /// <param name="configuration">
        /// The configuration that controls tracing and prompting behavior.  This
        /// parameter may be null.
        /// </param>
        /// <param name="assembly">
        /// The assembly used to derive the message box title.  This parameter
        /// may be null.
        /// </param>
        /// <param name="message">
        /// The message text to trace and optionally display.
        /// </param>
        /// <param name="category">
        /// The trace category associated with the message.
        /// </param>
        /// <param name="buttons">
        /// The set of buttons to display in the message box.
        /// </param>
        /// <param name="icon">
        /// The icon to display in the message box.
        /// </param>
        /// <param name="dialogResult">
        /// The default dialog result to return when the message box is not
        /// displayed.  This parameter may be null, in which case a result of OK
        /// is assumed.
        /// </param>
        /// <returns>
        /// The dialog result indicating the user's choice, or the default
        /// result when the message box is not displayed.
        /// </returns>
        public static DialogResult ShowMessage(
            Configuration configuration,
            Assembly assembly,
            string message,
            string category,
            MessageBoxButtons buttons,
            MessageBoxIcon icon,
            DialogResult? dialogResult
            )
        {
            DialogResult result = (dialogResult != null) ?
                (DialogResult)dialogResult : DialogResult.OK;

            Trace(configuration, message, category);

            if (Configuration.IsPromptOk(configuration, icon, true))
            {
                string title = AttributeOps.GetAssemblyTitle(assembly);

                if (title == null)
                    title = Application.ProductName;

                result = MessageBox.Show(message, title, buttons, icon);

                Trace(configuration, String.Format(
                    "User choice of \"{0}\".", result), category);

                return result;
            }

            Trace(configuration, String.Format(
                "Default choice of \"{0}\".", result), category);

            return result;
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Tracing Support Methods
        /// <summary>
        /// This method gets the current date and time, in Coordinated Universal
        /// Time (UTC).
        /// </summary>
        /// <returns>
        /// The current UTC date and time.
        /// </returns>
        public static DateTime GetNow()
        {
            return DateTime.UtcNow;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method atomically increments and returns the next trace
        /// identifier.
        /// </summary>
        /// <returns>
        /// The next trace identifier.
        /// </returns>
        public static long NextId()
        {
            return Interlocked.Increment(ref nextId);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method formats the specified date and time value as an ISO-8601
        /// style timestamp string.
        /// </summary>
        /// <param name="dateTime">
        /// The date and time value to format.
        /// </param>
        /// <returns>
        /// The formatted timestamp string.
        /// </returns>
        public static string TimeStamp(DateTime dateTime)
        {
            return dateTime.ToString(Iso8601DateTimeOutputFormat);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method searches a call stack for the first eligible (non-trace)
        /// stack frame and returns its formatted type and method name.
        /// </summary>
        /// <param name="stackTrace">
        /// The stack trace to search.  This parameter may be null, in which case
        /// the current execution stack is captured and used.
        /// </param>
        /// <param name="level">
        /// The number of stack frames, from the top, to skip before beginning
        /// the search.
        /// </param>
        /// <returns>
        /// The formatted type and method name of the originating method, or null
        /// if one could not be determined.
        /// </returns>
        [MethodImpl(MethodImplOptions.NoInlining)]
        private static string GetMethodName(
            StackTrace stackTrace,
            int level
            )
        {
            try
            {
                //
                // NOTE: If a valid stack trace was not supplied by the
                //       caller, create one now based on the current
                //       execution stack.
                //
                if (stackTrace == null)
                {
                    //
                    // NOTE: Grab the current execution stack.
                    //
                    stackTrace = new StackTrace();

                    //
                    // NOTE: Always skip this call frame when we capture
                    //       the stack trace.
                    //
                    level++;
                }

                //
                // NOTE: Search for the first "non-trace" stack frame
                //       (i.e. one which is not just another "Trace"
                //       method).
                //
                int count = stackTrace.FrameCount;

                for (int index = level; index < count; index++)
                {
                    //
                    // NOTE: Get the stack frame for this index.
                    //
                    StackFrame stackFrame = stackTrace.GetFrame(index);

                    if (stackFrame == null)
                        continue;

                    //
                    // NOTE: Get the method for this stack frame.
                    //
                    MethodBase methodBase = stackFrame.GetMethod();

                    if (methodBase == null)
                        continue;

                    //
                    // NOTE: Get the type for the method.
                    //
                    Type type = methodBase.DeclaringType;

                    if (type == null)
                        continue;

                    //
                    // NOTE: Get the name of the method.
                    //
                    string name = methodBase.Name;

                    if (name == null)
                        continue;

                    //
                    // NOTE: Do we need to skip this method (based on the
                    //       raw method name)?
                    //
                    lock (syncRoot)
                    {
                        bool value;

                        if ((skipNames == null) ||
                            !skipNames.TryGetValue(name, out value) || value)
                        {
                            //
                            // NOTE: Found an eligible stack frame, return
                            //       the properly formatted result.
                            //
                            return String.Format(
                                "{0}{1}{2}", type.Name, Type.Delimiter, name);
                        }
                    }
                }
            }
            catch
            {
                // do nothing.
            }

            return null;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method writes a message directly to the diagnostic trace
        /// listeners and flushes them.  It is the default trace callback.
        /// </summary>
        /// <param name="message">
        /// The message text to write.
        /// </param>
        /// <param name="category">
        /// The trace category associated with the message.
        /// </param>
        public static void TraceCore(
            string message,
            string category
            )
        {
            lock (syncRoot)
            {
                System.Diagnostics.Trace.WriteLine(message, category);
                System.Diagnostics.Trace.Flush();
            }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method traces the string representation of an exception, along
        /// with the stack trace captured from the exception.
        /// </summary>
        /// <param name="configuration">
        /// The configuration that controls tracing behavior.  This parameter may
        /// be null.
        /// </param>
        /// <param name="exception">
        /// The exception to trace.  This parameter may be null, in which case
        /// nothing is traced.
        /// </param>
        /// <param name="category">
        /// The trace category associated with the message.
        /// </param>
        /// <returns>
        /// The traced message, or null if <paramref name="exception" /> is null.
        /// </returns>
        [MethodImpl(MethodImplOptions.NoInlining)]
        public static string Trace(
            Configuration configuration,
            Exception exception,
            string category
            )
        {
            if (exception != null)
                return Trace(configuration,
                    new StackTrace(exception, true), 0,
                    exception.ToString(), category);

            return null;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method traces a message, prefixing it with the name of the
        /// calling method.
        /// </summary>
        /// <param name="configuration">
        /// The configuration that controls tracing behavior.  This parameter may
        /// be null.
        /// </param>
        /// <param name="message">
        /// The message text to trace.
        /// </param>
        /// <param name="category">
        /// The trace category associated with the message.
        /// </param>
        /// <returns>
        /// The traced message.
        /// </returns>
        [MethodImpl(MethodImplOptions.NoInlining)]
        public static string Trace(
            Configuration configuration,
            string message,
            string category
            )
        {
            return Trace(configuration, null, 1, message, category);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method traces a message, prefixing it with the name of the
        /// originating method derived from the supplied or captured stack trace,
        /// using the configured trace callback when available.
        /// </summary>
        /// <param name="configuration">
        /// The configuration that controls tracing behavior, including the trace
        /// callback.  This parameter may be null.
        /// </param>
        /// <param name="stackTrace">
        /// The stack trace used to determine the originating method name.  This
        /// parameter may be null, in which case the current execution stack is
        /// captured and used.
        /// </param>
        /// <param name="level">
        /// The number of stack frames, from the top, to skip when determining
        /// the originating method name.
        /// </param>
        /// <param name="message">
        /// The message text to trace.
        /// </param>
        /// <param name="category">
        /// The trace category associated with the message.
        /// </param>
        /// <returns>
        /// The traced message.
        /// </returns>
        [MethodImpl(MethodImplOptions.NoInlining)]
        private static string Trace(
            Configuration configuration,
            StackTrace stackTrace,
            int level,
            string message,
            string category
            )
        {
            //
            // NOTE: Always skip this call frame if the stack trace is going
            //       to be captured by GetMethodName.
            //
            if (stackTrace == null)
                level++;

            TraceCallback traceCallback =
                (configuration != null) ? configuration.TraceCallback : null;

            if (traceCallback == null)
                traceCallback = TraceCore;

            traceCallback(String.Format("{0}: {1}",
                GetMethodName(stackTrace, level), message), category);

            return message;
        }
        #endregion
    }
}
