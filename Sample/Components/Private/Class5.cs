/*
 * Class5.cs --
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
using System.Windows.Forms;
using Eagle._Attributes;
using Eagle._Components.Public;
using TclSample.Forms;

namespace TclSample
{
    /// <summary>
    /// This class contains the main entry point for this assembly.  It is
    /// used to demonstrate how to use the Tcl form class, along with several
    /// related components, to display a graphical Tcl "shell-like" user
    /// interface.
    /// </summary>
    //
    // FIXME: Always change this GUID.
    //
    [ObjectId("1d0c8b12-70f9-4037-9c95-fc38de52e52a")]
    internal static class Class5
    {
        #region Assembly Entry Point Methods
        /// <summary>
        /// This is the main entry point for this assembly.
        /// </summary>
        /// <param name="args">
        /// The command line arguments received from the calling assembly.
        /// </param>
        /// <returns>
        /// Zero for success, non-zero on error.
        /// </returns>
        [STAThread()] /* WinForms */
        private static int Main(
            string[] args /* in */
            )
        {
            return (int)Test(args);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Creates the initial Tcl form, shows it modally, and then waits for
        /// all Tcl forms to exit.
        /// </summary>
        /// <param name="args">
        /// The command line arguments received from the calling assembly.
        /// </param>
        /// <returns>
        /// Zero for success, non-zero on error.
        /// </returns>
        private static ExitCode Test(
            IEnumerable<string> args /* in */
            )
        {
            //
            // NOTE: This will be the exit code for the process.
            //
            ExitCode exitCode = Utility.SuccessExitCode();

            //
            // NOTE: Enable visual styles (i.e. "theming") on operating systems
            //       that support it.
            //
            Application.EnableVisualStyles();

            //
            // NOTE: Use GDI+ for text rendering on new controls that support
            //       it.
            //
            Application.SetCompatibleTextRenderingDefault(false);

            //
            // NOTE: Create and initialize the application user interface.
            //       This method can fail and will return null in that case.
            //
            using (Form form = TclForm.Create(args))
            {
                //
                // NOTE: Make sure the creation was successful.
                //
                if (form != null)
                {
                    //
                    // NOTE: Show the primary user interface form for the
                    //       application.
                    //
                    Application.Run(form);
                }
                else
                {
                    //
                    // NOTE: Complain about not being able to create the user
                    //       interface.
                    //
                    Utility.Complain(null, ReturnCode.Error,
                        "could not create user interface");

                    //
                    // NOTE: Failed to create the form?
                    //
                    exitCode = Utility.FailureExitCode();
                }
            }

            return exitCode;
        }
        #endregion
    }
}
