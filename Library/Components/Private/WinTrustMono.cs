/*
 * WinTrustMono.cs --
 *
 * Copyright (c) 2007-2012 by Joe Mistachkin.  All rights reserved.
 *
 * See the file "license.terms" for information on usage and redistribution of
 * this file, and for a DISCLAIMER OF ALL WARRANTIES.
 *
 * RCS: @(#) $Id: $
 */

using System;
using Mono.Security.Authenticode;
using Eagle._Attributes;
using Eagle._Components.Public;

namespace Eagle._Components.Private
{
    [ObjectId("9634a278-b167-433d-9d2f-223dd31cf622")]
    internal static class WinTrustMono
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
            ReturnCode code;

            if (String.IsNullOrEmpty(fileName))
            {
                error = "invalid file name";
                code = ReturnCode.Error;

                goto done;
            }

            if (!CommonOps.Runtime.IsMono())
            {
                error = "not supported on this platform";
                code = ReturnCode.Error;

                goto done;
            }

            try
            {
                AuthenticodeDeformatter deformatter =
                    new AuthenticodeDeformatter(fileName); /* throw */

                try
                {
                    if (deformatter.IsTrusted())
                    {
                        code = ReturnCode.Ok;
                    }
                    else
                    {
                        error = "file is not trusted";
                        code = ReturnCode.Error;
                    }
                }
                finally
                {
                    returnValue = deformatter.Reason;
                }
            }
            catch (Exception e)
            {
                error = e;
                code = ReturnCode.Error;
            }

        done:

            bool success = (code == ReturnCode.Ok) &&
                (returnValue == ERROR_SUCCESS);

            TracePriority priority = success ?
                TracePriority.SecurityDebug2 : TracePriority.SecurityError;

            TraceOps.DebugTrace(String.Format(
                "IsFileTrusted: file {0} check {1}, " +
                "userInterface = {2}, revocation = {3}, " +
                "install = {4}, returnValue = {5}, error = {6}",
                FormatOps.WrapOrNull(fileName),
                success ? "success" : "failure",
                userInterface, revocation, install,
                returnValue, FormatOps.WrapOrNull(error)),
                typeof(WinTrustMono).Name,
                TracePriority.SecurityError);

            return code;
        }
        #endregion
    }
}
