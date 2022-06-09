/*
 * WinTrustDotNet.cs --
 *
 * Copyright (c) 2007-2012 by Joe Mistachkin.  All rights reserved.
 *
 * See the file "license.terms" for information on usage and redistribution of
 * this file, and for a DISCLAIMER OF ALL WARRANTIES.
 *
 * RCS: @(#) $Id: $
 */

using System;
using Eagle._Attributes;
using Eagle._Components.Public;

namespace Eagle._Components.Private
{
    [ObjectId("51860eb6-c91c-484b-a41a-8663909108f6")]
    internal static class WinTrustDotNet
    {
        #region Private Constants
        //
        // NOTE: Currently, this constant is only used with the WinTrust
        //       API; however, it is still needed even when the WINDOWS
        //       compile-time option is disabled.
        //
        private const uint ERROR_SUCCESS = 0;
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Public Methods
        public static bool IsFileTrusted(
            string fileName,    /* in */
            IntPtr fileHandle,  /* in */
            bool userInterface, /* in */
            bool userPrompt,    /* in */
            bool revocation,    /* in */
            bool install        /* in */
            )
        {
            /* !SUCCESS */
            int returnValue = (int)ERROR_SUCCESS + 1;
            Result error = null;

            if ((IsFileTrusted(fileName,
                    fileHandle, userInterface, userPrompt,
                    revocation, install, ref returnValue,
                    ref error) == ReturnCode.Ok) &&
                (returnValue == ERROR_SUCCESS))
            {
                return true;
            }
            else
            {
                return false;
            }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Private Methods
        private static ReturnCode IsFileTrusted(
            string fileName,     /* in */
            IntPtr fileHandle,   /* in: NOT USED */
            bool userInterface,  /* in: NOT USED */
            bool userPrompt,     /* in: NOT USED */
            bool revocation,     /* in: NOT USED */
            bool install,        /* in: NOT USED */
            ref int returnValue, /* out */
            ref Result error     /* out */
            )
        {
            if (String.IsNullOrEmpty(fileName))
            {
                error = "invalid file name";
                goto done;
            }

            if (!CommonOps.Runtime.IsDotNetCore())
            {
                error = "not supported on this platform";
                goto done;
            }

            //
            // BUGBUG: There is no way to verify the trust status of
            //         an executable file signed with an Authenticode
            //         certificate when running on the .NET Core 2.x
            //         (or 3.x) runtimes, unless we also happen to be
            //         running on Windows.  This method should not be
            //         called when running on Windows.
            //
            error = "no implementation available on this platform";

        done:

            TraceOps.DebugTrace(String.Format(
                "IsFileTrusted: file {0} trust failure, " +
                "userInterface = {1}, revocation = {2}, " +
                "install = {3}, returnValue = {4}, error = {5}",
                FormatOps.WrapOrNull(fileName),
                userInterface, revocation, install,
                returnValue, FormatOps.WrapOrNull(error)),
                typeof(WinTrustDotNet).Name,
                TracePriority.SecurityError);

            return ReturnCode.Error;
        }
        #endregion
    }
}
