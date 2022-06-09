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
    [Guid("84055eb4-6bac-4471-880a-6f9da34c5a5b")]
    internal static class TraceOps
    {
        #region Private Constants
        private const string Iso8601DateTimeOutputFormat =
            "yyyy.MM.ddTHH:mm:ss.fffffff";
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Private Data
        private static readonly object syncRoot = new object();
        private static long nextId;

        ///////////////////////////////////////////////////////////////////////

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
        public static long NextId()
        {
            return Interlocked.Increment(ref nextId);
        }

        ///////////////////////////////////////////////////////////////////////

        public static string TimeStamp(DateTime dateTime)
        {
            return dateTime.ToString(Iso8601DateTimeOutputFormat);
        }

        ///////////////////////////////////////////////////////////////////////

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
