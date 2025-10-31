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

#if NET_STANDARD_20
using System.Security.Cryptography.Pkcs;
using System.Security.Cryptography.X509Certificates;
#endif

using Eagle._Attributes;
using Eagle._Components.Public;
using Eagle._Containers.Public;

namespace Eagle._Components.Private
{
    [ObjectId("51860eb6-c91c-484b-a41a-8663909108f6")]
    internal static partial class WinTrustDotNet
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

        #region Private Methods
        private static bool MaybeAddError(
            ref ResultList errors, /* in, out */
            Result error           /* in */
            )
        {
            if (error == null)
                return false;

            if (errors == null)
                errors = new ResultList();

            errors.Add(error);
            return true;
        }

        ///////////////////////////////////////////////////////////////////////

        private static bool MaybeMatchTrustedFileHash(
            Interpreter interpreter,  /* in: OPTIONAL */
            StringList trustedHashes, /* in: OPTIONAL */
            string fileName,          /* in */
            ref ResultList errors     /* in, out */
            )
        {
            if (!RuntimeOps.ShouldForceTrustedHashes() &&
                !CommonOps.Runtime.IsDotNetCore())
            {
                MaybeAddError(ref errors,
                    "not supported on this platform");

                return false;
            }

            if (!RuntimeOps.ShouldUseTrustedHashes())
            {
                MaybeAddError(ref errors,
                    "trusted hashes are disabled");

                return false;
            }

            Result localError = null;

            if (!PolicyOps.IsTrustedFile(
                    interpreter, trustedHashes, fileName,
                    ref localError))
            {
                MaybeAddError(ref errors, String.Format(
                    "file hash not trusted: {0}", localError));

                return false;
            }

            return true;
        }

        ///////////////////////////////////////////////////////////////////////

        private static bool TryVerifyPeFileSignature(
            string fileName,      /* in */
            ref ResultList errors /* in, out */
            )
        {
#if NET_STANDARD_20
            try
            {
                VerificationResult result = VerifyPeFileSignature(
                    fileName); /* throw */

                if ((result != null) && result.AllValid)
                    return true;

                MaybeAddError(ref errors, String.Format(
                    "signature verification failed: {0}", result));
            }
            catch (Exception e)
            {
                MaybeAddError(ref errors, e);
            }
#else
            MaybeAddError(ref errors,
                "not supported on this platform");
#endif

            return false;
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Public Methods
        public static bool IsFileTrusted(
            Interpreter interpreter,  /* in */
            StringList trustedHashes, /* in */
            string fileName,          /* in */
            IntPtr fileHandle,        /* in */
            bool userInterface,       /* in */
            bool userPrompt,          /* in */
            bool revocation,          /* in */
            bool install              /* in */
            )
        {
            /* !SUCCESS */
            int returnValue = (int)ERROR_SUCCESS + 1;
            Result error = null;

            if ((IsFileTrusted(
                    interpreter, trustedHashes, fileName,
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
        //
        // BUGBUG: There was no way to verify the trust status of
        //         an executable file signed with an Authenticode
        //         certificate when running on the .NET Core 2.x
        //         (or 3.x) runtimes, unless we also happen to be
        //         running on Windows.  This method should not be
        //         called when running on Windows or when running
        //         on the .NET Framework.
        //
        private static ReturnCode IsFileTrusted(
            Interpreter interpreter,  /* in */
            StringList trustedHashes, /* in */
            string fileName,          /* in */
            IntPtr fileHandle,        /* in: NOT USED */
            bool userInterface,       /* in: NOT USED */
            bool userPrompt,          /* in: NOT USED */
            bool revocation,          /* in: NOT USED */
            bool install,             /* in: NOT USED */
            ref int returnValue,      /* out */
            ref Result error          /* out */
            )
        {
            ReturnCode code = ReturnCode.Error;
            ResultList errors = null;

            if (String.IsNullOrEmpty(fileName))
            {
                MaybeAddError(ref errors,
                    "invalid file name");

                goto done;
            }

            if (TryVerifyPeFileSignature(fileName, ref errors))
            {
                returnValue = (int)ERROR_SUCCESS;
                code = ReturnCode.Ok;

                goto done;
            }

            if (MaybeMatchTrustedFileHash(
                    interpreter, trustedHashes, fileName, ref errors))
            {
                returnValue = (int)ERROR_SUCCESS;
                code = ReturnCode.Ok;

                goto done;
            }

        done:

            if (errors != null)
                error = errors;

            bool success = (code == ReturnCode.Ok) &&
                (returnValue == ERROR_SUCCESS);

            TracePriority priority = success ?
                TracePriority.SecurityDebug2 : TracePriority.SecurityError;

            TraceOps.DebugTrace(String.Format(
                "IsFileTrusted: file {0} check {1}, " +
                "interpreter = {2}, trustedHashes = {3}, " +
                "userInterface = {4}, revocation = {5}, " +
                "install = {6}, returnValue = {7}, " +
                "error = {8}", FormatOps.WrapOrNull(fileName),
                success ? "success" : "failure",
                FormatOps.InterpreterNoThrow(interpreter),
                FormatOps.WrapOrNull(trustedHashes),
                userInterface, revocation, install,
                returnValue, FormatOps.WrapOrNull(error)),
                typeof(WinTrustDotNet).Name, priority);

            return code;
        }
        #endregion
    }
}
