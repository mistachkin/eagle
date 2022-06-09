/*
 * Program.cs --
 *
 * Copyright (c) 2007-2012 by Joe Mistachkin.  All rights reserved.
 *
 * See the file "license.terms" for information on usage and redistribution of
 * this file, and for a DISCLAIMER OF ALL WARRANTIES.
 *
 * RCS: @(#) $Id: $
 */

using System;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Security.AccessControl;
using System.Threading;
using System.Windows.Forms;
using Eagle._Components.Private;
using Eagle._Forms;

namespace Eagle._Tool
{
    [Guid("3fa71d89-21c4-49bd-b1ef-3f13371cf3e6")]
    internal static class Program
    {
        #region Private Data (Read-Only)
        private static readonly Assembly assembly = 
            Assembly.GetExecutingAssembly();

        ///////////////////////////////////////////////////////////////////////

        private static readonly string TraceCategory = typeof(Program).Name;
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Private Data (Read-Write)
        //
        // NOTE: This is used to synchronize access to the saved configuration
        //       instance field (below).
        //
        private static readonly object syncRoot = new object();

        ///////////////////////////////////////////////////////////////////////

        //
        // NOTE: The saved configuration instance.  This is for use only by the
        //       ApplicationExit method (i.e. after being initially set by the
        //       Main method).
        //
        private static Configuration savedConfiguration;
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Error Handling Methods
        public static void Fail(
            Configuration configuration,
            Assembly assembly,
            string message,
            string category
            )
        {
            TraceOps.ShowMessage(
                configuration, assembly, message, category,
                MessageBoxButtons.OK, MessageBoxIcon.Error);

            Environment.Exit(1);
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Application Exit Event Handler
        private static void ApplicationExit(
            object sender,
            EventArgs e
            )
        {
            Configuration configuration;

            lock (syncRoot)
            {
                configuration = savedConfiguration;
            }

            TraceOps.Trace(configuration, "Exiting.", TraceCategory);

            string error = null;

            if (!FileOps.DeleteInUse(configuration, ref error))
                Fail(configuration, assembly, error, TraceCategory);
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Application Entry Point Method
        [STAThread] /* WinForms */
        private static int Main(
            string[] args
            )
        {
            Configuration configuration = null;
            string error = null;

            if (Configuration.TryCreate(
                    assembly, ref configuration, ref error) &&
                Configuration.FromFile(
                    null, true, ref configuration, ref error) &&
                Configuration.FromArgs(
                    args, true, ref configuration, ref error) &&
                Configuration.Process(
                    args, configuration, false, ref error))
            {
#if DEBUG
                TraceOps.Trace(configuration,
                    "WARNING: This is a DEBUG build and is intended only " +
                    "for development and in-house testing.", TraceCategory);
#endif

#if MONO
                TraceOps.Trace(configuration,
                    "WARNING: This is a Mono build, some features will be " +
                    "unavailable, including some features used to enhance " +
                    "security.", TraceCategory);
#endif

                if (!configuration.IsValid)
                {
                    error = "Invalid configuration.";
                    goto error;
                }

                if (!configuration.IsSigned)
                {
                    error = "Self-check failed, cannot continue.";
                    goto error;
                }

                try
                {
                    using (Mutex.OpenExisting(configuration.MutexName,
                            MutexRights.Synchronize))
                    {
                        error = "Detected that an instance of Eagle is " +
                            "running, please close it and try again.";
                    }
                }
                catch (WaitHandleCannotBeOpenedException)
                {
                    try
                    {
                        lock (syncRoot)
                        {
                            savedConfiguration = configuration;
                        }

                        Application.ApplicationExit += ApplicationExit;
                        Application.EnableVisualStyles();
                        Application.SetCompatibleTextRenderingDefault(false);

                        using (UpdateForm form = UpdateForm.Create(
                                configuration, ref error))
                        {
                            if (form != null)
                            {
                                Application.Run(form);
                                return 0; /* SUCCESS */
                            }
                        }
                    }
                    catch (Exception e)
                    {
                        TraceOps.Trace(configuration, e, TraceCategory);

                        error = "User interface failed.";
                    }
                }
                catch (Exception e)
                {
                    TraceOps.Trace(configuration, e, TraceCategory);

                    error = "Running instance detection failed.";
                }
            }

        error:
            Fail(configuration, assembly, error, TraceCategory);
            return 1; /* NOT REACHED */
        }
        #endregion
    }
}
