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
    /// <summary>
    /// This class provides managed (non-WinTrust) support for verifying the
    /// trust status of a file on platforms, such as .NET Core, where the
    /// native WinTrust API is not available.  It attempts to verify an
    /// Authenticode signature using managed cryptography and, failing that,
    /// falls back to matching the file against a set of trusted file hashes.
    /// </summary>
    [ObjectId("51860eb6-c91c-484b-a41a-8663909108f6")]
    internal static partial class WinTrustDotNet
    {
        #region Private Constants
        //
        // NOTE: Currently, this constant is only used with the WinTrust
        //       API; however, it is still needed even when the WINDOWS
        //       compile-time option is disabled.
        //
        /// <summary>
        /// The Windows success status code (zero), used to indicate that a
        /// file is trusted.
        /// </summary>
        private const uint ERROR_SUCCESS = 0;
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Private Methods
        /// <summary>
        /// This method adds an error to the specified list of errors, creating
        /// the list first if necessary.  If the supplied error is null, nothing
        /// is added.
        /// </summary>
        /// <param name="errors">
        /// The list of errors to add to.  If this is null and an error is
        /// added, a new list is created and stored here.
        /// </param>
        /// <param name="error">
        /// The error to add.  If this is null, this method does nothing.
        /// </param>
        /// <returns>
        /// True if the error was added; otherwise, false.
        /// </returns>
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

        /// <summary>
        /// This method attempts to verify that the specified file is trusted by
        /// matching its hash against a set of trusted file hashes.  This is only
        /// supported when the use of trusted hashes is enabled and either the
        /// runtime is .NET Core or the use of trusted hashes is being forced.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter context to use, if any.  This parameter may be null.
        /// </param>
        /// <param name="trustedHashes">
        /// The list of trusted file hashes to match against, if any.  This
        /// parameter may be null.
        /// </param>
        /// <param name="fileName">
        /// The name of the file whose hash is to be checked.
        /// </param>
        /// <param name="errors">
        /// The list of errors to add to upon failure.  If this is null and an
        /// error is added, a new list is created and stored here.
        /// </param>
        /// <returns>
        /// True if the file hash is trusted; otherwise, false.
        /// </returns>
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

        /// <summary>
        /// This method attempts to verify the Authenticode signature of the
        /// specified portable executable (PE) file using managed cryptography.
        /// This is only supported when the netstandard 2.0 cryptographic APIs
        /// are available; on other platforms it always fails.
        /// </summary>
        /// <param name="fileName">
        /// The name of the portable executable file whose signature is to be
        /// verified.
        /// </param>
        /// <param name="revocation">
        /// Non-zero to perform online certificate revocation checking as part
        /// of the verification.
        /// </param>
        /// <param name="errors">
        /// The list of errors to add to upon failure.  If this is null and an
        /// error is added, a new list is created and stored here.
        /// </param>
        /// <returns>
        /// True if the file signature was successfully verified as valid;
        /// otherwise, false.
        /// </returns>
        private static bool TryVerifyPeFileSignature(
            string fileName,      /* in */
            bool revocation,      /* in */
            ref ResultList errors /* in, out */
            )
        {
#if NET_STANDARD_20
            try
            {
                VerificationOptions options = null;

                if (revocation)
                {
                    options = new VerificationOptions();
                    options.RevocationMode = X509RevocationMode.Online;
                }

                VerificationResult result = VerifyPeFileSignature(
                    fileName, options); /* throw */

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
        /// <summary>
        /// This method determines whether the specified file is trusted,
        /// returning the result as a simple boolean value.  It delegates to the
        /// overload that returns detailed status and error information.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter context to use, if any.  This parameter may be null.
        /// </param>
        /// <param name="trustedHashes">
        /// The list of trusted file hashes to match against, if any.  This
        /// parameter may be null.
        /// </param>
        /// <param name="fileName">
        /// The name of the file whose trust status is to be checked.
        /// </param>
        /// <param name="fileHandle">
        /// An open handle to the file, if any.
        /// </param>
        /// <param name="userInterface">
        /// Non-zero if a user interface may be displayed during verification.
        /// </param>
        /// <param name="userPrompt">
        /// Non-zero if the user may be prompted during verification.
        /// </param>
        /// <param name="revocation">
        /// Non-zero to perform online certificate revocation checking as part
        /// of the verification.
        /// </param>
        /// <param name="install">
        /// Non-zero if the file is being checked as part of an install
        /// operation.
        /// </param>
        /// <returns>
        /// True if the file is trusted; otherwise, false.
        /// </returns>
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
        /// <summary>
        /// This method determines whether the specified file is trusted by
        /// first attempting to verify its Authenticode signature and then, if
        /// that fails, attempting to match its hash against a set of trusted
        /// file hashes.  It also emits a diagnostic trace describing the result.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter context to use, if any.  This parameter may be null.
        /// </param>
        /// <param name="trustedHashes">
        /// The list of trusted file hashes to match against, if any.  This
        /// parameter may be null.
        /// </param>
        /// <param name="fileName">
        /// The name of the file whose trust status is to be checked.
        /// </param>
        /// <param name="fileHandle">
        /// An open handle to the file, if any.  This parameter is not used.
        /// </param>
        /// <param name="userInterface">
        /// Non-zero if a user interface may be displayed during verification.
        /// This parameter is not used.
        /// </param>
        /// <param name="userPrompt">
        /// Non-zero if the user may be prompted during verification.  This
        /// parameter is not used.
        /// </param>
        /// <param name="revocation">
        /// Non-zero to perform online certificate revocation checking as part
        /// of the verification.
        /// </param>
        /// <param name="install">
        /// Non-zero if the file is being checked as part of an install
        /// operation.  This parameter is not used.
        /// </param>
        /// <param name="returnValue">
        /// Upon success, this is set to <see cref="ERROR_SUCCESS" /> to indicate
        /// that the file is trusted; otherwise, it is left with a non-success
        /// value.
        /// </param>
        /// <param name="error">
        /// Upon failure, this contains the collected error information.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> if the file is trusted; otherwise,
        /// <see cref="ReturnCode.Error" />.
        /// </returns>
        private static ReturnCode IsFileTrusted(
            Interpreter interpreter,  /* in */
            StringList trustedHashes, /* in */
            string fileName,          /* in */
            IntPtr fileHandle,        /* in: NOT USED */
            bool userInterface,       /* in: NOT USED */
            bool userPrompt,          /* in: NOT USED */
            bool revocation,          /* in */
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

            if (TryVerifyPeFileSignature(
                    fileName, revocation, ref errors))
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
