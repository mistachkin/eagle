/*
 * WinTrustOps.cs --
 *
 * Copyright (c) 2007-2012 by Joe Mistachkin.  All rights reserved.
 *
 * See the file "license.terms" for information on usage and redistribution of
 * this file, and for a DISCLAIMER OF ALL WARRANTIES.
 *
 * RCS: @(#) $Id: $
 */

#if !NATIVE
#error "This file cannot be compiled or used properly with native code disabled."
#endif

using System;
using System.Runtime.InteropServices;
using System.Security;

#if !NET_40
using System.Security.Permissions;
#endif

using Eagle._Attributes;
using Eagle._Components.Public;
using Eagle._Constants;
using Eagle._Containers.Public;

namespace Eagle._Components.Private
{
    /// <summary>
    /// This class provides Authenticode trust verification for files using the
    /// native Windows WinTrust API (specifically WinVerifyTrust) via P/Invoke.
    /// </summary>
#if NET_40
    [SecurityCritical()]
#else
    [SecurityPermission(SecurityAction.LinkDemand, UnmanagedCode = true)]
#endif
    [ObjectId("53a5ad6d-23a7-4172-97be-d90e68000859")]
    internal static class WinTrustOps
    {
        #region Private Constants
#if WINDOWS
        /// <summary>
        /// The native invalid handle value (negative one), used as the window handle
        /// when no interactive user interface is permitted.
        /// </summary>
        private static readonly IntPtr INVALID_HANDLE_VALUE = new IntPtr(-1);

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// The name of the embedded resource containing the default trust values used
        /// when overriding the WinTrust verification parameters.
        /// </summary>
        private const string TrustValuesResourceName = "DefaultTrustValues.txt";
#endif
        #endregion

        ///////////////////////////////////////////////////////////////////////
        // Required Native APIs used via P/Invoke
        ///////////////////////////////////////////////////////////////////////

        #region Private Unsafe Native Methods Class
        /// <summary>
        /// This class contains the private constants, structures, and native method
        /// declarations used to invoke the Windows WinTrust API via P/Invoke.
        /// </summary>
        [SuppressUnmanagedCodeSecurity()]
        [ObjectId("e8dc85ea-ceba-47db-9ed0-a66ba3f0f916")]
        private static class UnsafeNativeMethods
        {
            //
            // NOTE: Currently, this constant is only used with the WinTrust
            //       API; however, it is still needed even when the WINDOWS
            //       compile-time option is disabled.
            //
            /// <summary>
            /// The native success status code (zero) used to indicate that a trust
            /// verification operation completed without error.
            /// </summary>
            internal const uint ERROR_SUCCESS = 0;

            ///////////////////////////////////////////////////////////////////

#if WINDOWS
            #region WinTrust API
            #region Constants
            /// <summary>
            /// The action identifier that selects the generic Authenticode verification
            /// policy provider for use with the WinTrust API.
            /// </summary>
            internal static readonly Guid WINTRUST_ACTION_GENERIC_VERIFY_V2 =
                new Guid("00aac56b-cd44-11d0-8cc2-00c04fc295ee");

            ///////////////////////////////////////////////////////////////////

            /// <summary>
            /// The WinTrust user interface choice that permits all user interface elements
            /// to be displayed.
            /// </summary>
            internal const uint WTD_UI_ALL =
                (uint)TrustValues.WTD_UI_ALL;

            /// <summary>
            /// The WinTrust user interface choice that suppresses all user interface
            /// elements.
            /// </summary>
            internal const uint WTD_UI_NONE =
                (uint)TrustValues.WTD_UI_NONE;

            ///////////////////////////////////////////////////////////////////

            /// <summary>
            /// The WinTrust revocation flag that disables certificate revocation checking.
            /// </summary>
            internal const uint WTD_REVOKE_NONE =
                (uint)TrustValues.WTD_REVOKE_NONE;

            /// <summary>
            /// The WinTrust revocation flag that enables revocation checking for the entire
            /// certificate chain.
            /// </summary>
            internal const uint WTD_REVOKE_WHOLECHAIN =
                (uint)TrustValues.WTD_REVOKE_WHOLECHAIN;

            ///////////////////////////////////////////////////////////////////

            /// <summary>
            /// The WinTrust union choice indicating that a file is the subject of the
            /// verification.
            /// </summary>
            internal const uint WTD_CHOICE_FILE = 1;

            ///////////////////////////////////////////////////////////////////

            /// <summary>
            /// The WinTrust state action indicating that no verification state is retained
            /// across calls.
            /// </summary>
            internal const uint WTD_STATEACTION_IGNORE = 0x0;

            ///////////////////////////////////////////////////////////////////

            /// <summary>
            /// The WinTrust provider flag that enables the trust evaluation behavior used
            /// by Software Restriction Policies.
            /// </summary>
            internal const uint WTD_SAFER_FLAG =
                (uint)TrustValues.WTD_SAFER_FLAG;

            /// <summary>
            /// The WinTrust provider flag that restricts revocation retrieval to the local
            /// cache, preventing network access.
            /// </summary>
            internal const uint WTD_CACHE_ONLY_URL_RETRIEVAL =
                (uint)TrustValues.WTD_CACHE_ONLY_URL_RETRIEVAL;

            /// <summary>
            /// The default combination of WinTrust provider flags used during
            /// verification.
            /// </summary>
            internal const uint WTD_DEFAULT =
                WTD_SAFER_FLAG | WTD_CACHE_ONLY_URL_RETRIEVAL;

            ///////////////////////////////////////////////////////////////////

            /// <summary>
            /// The WinTrust user interface context indicating the file is being verified
            /// prior to execution.
            /// </summary>
            internal const uint WTD_UICONTEXT_EXECUTE =
                (uint)TrustValues.WTD_UICONTEXT_EXECUTE;

            /// <summary>
            /// The WinTrust user interface context indicating the file is being verified
            /// prior to installation.
            /// </summary>
            internal const uint WTD_UICONTEXT_INSTALL =
                (uint)TrustValues.WTD_UICONTEXT_INSTALL;
            #endregion

            ///////////////////////////////////////////////////////////////////

            #region Structures
            /// <summary>
            /// This structure provides information about a file whose trust is being
            /// verified, corresponding to the native WINTRUST_FILE_INFO structure.
            /// </summary>
            [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
            [ObjectId("5c7cdbf0-c943-49b7-87fe-471813fa88d5")]
            internal struct WINTRUST_FILE_INFO
            {
                /// <summary>
                /// The size, in bytes, of this structure.
                /// </summary>
                public /* DWORD */ uint cbStruct;
                /// <summary>
                /// The full path of the file whose trust is being verified.
                /// </summary>
                public /* LPCWSTR */ string pcwszFilePath;
                /// <summary>
                /// An optional open handle to the file whose trust is being verified.
                /// </summary>
                public /* HANDLE */ IntPtr hFile;
                /// <summary>
                /// An optional pointer to the known subject interface identifier for the file.
                /// </summary>
                public /* LPGUID */ IntPtr pgKnownSubject;
            }

            ///////////////////////////////////////////////////////////////////

            /// <summary>
            /// This structure provides the data used by the WinTrust API to verify the
            /// trust of a subject, corresponding to the native WINTRUST_DATA structure.
            /// </summary>
            [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
            [ObjectId("7cb4009e-d4c5-403c-a246-16751dfacb6b")]
            internal struct WINTRUST_DATA
            {
                /// <summary>
                /// The size, in bytes, of this structure.
                /// </summary>
                public /* DWORD */ uint cbStruct;
                /// <summary>
                /// An optional pointer to data passed to the policy provider callback.
                /// </summary>
                public /* LPVOID */ IntPtr pPolicyCallbackData;
                /// <summary>
                /// An optional pointer to data passed to the subject interface package.
                /// </summary>
                public /* LPVOID */ IntPtr pSIPClientData;
                /// <summary>
                /// The value controlling which user interface elements may be displayed during
                /// verification.
                /// </summary>
                public /* DWORD */ uint dwUIChoice;
                /// <summary>
                /// The value controlling how certificate revocation checking is performed.
                /// </summary>
                public /* DWORD */ uint fdwRevocationChecks;
                /// <summary>
                /// The value indicating which kind of subject is being verified.
                /// </summary>
                public /* DWORD */ uint dwUnionChoice;
                /// <summary>
                /// A pointer to the WINTRUST_FILE_INFO structure describing the file subject.
                /// </summary>
                public /* PWINTRUST_FILE_INFO */ IntPtr pFile;
                /// <summary>
                /// The value controlling how verification state is retained across calls.
                /// </summary>
                public /* DWORD */ uint dwStateAction;
                /// <summary>
                /// A handle to the verification state data maintained between calls.
                /// </summary>
                public /* HANDLE */ IntPtr hWVTStateData;
                /// <summary>
                /// An optional URL reference associated with the subject being verified.
                /// </summary>
                public /* LPWSTR */ string pwszURLReference;
                /// <summary>
                /// The flags controlling the behavior of the trust provider.
                /// </summary>
                public /* DWORD */ uint dwProvFlags;
                /// <summary>
                /// The value indicating the context in which the verification is being
                /// performed.
                /// </summary>
                public /* DWORD */ uint dwUIContext;
            }
            #endregion

            ///////////////////////////////////////////////////////////////////

            #region Functions
            /// <summary>
            /// This method invokes the native WinTrust API to perform trust verification
            /// for the specified subject.
            /// </summary>
            /// <param name="hWnd">
            /// A handle to the window to use as the parent for any user interface, or the
            /// invalid handle value to suppress user interface.
            /// </param>
            /// <param name="actionId">
            /// The action identifier selecting the policy provider to use.
            /// </param>
            /// <param name="pData">
            /// The trust data describing the subject to verify and the verification
            /// options.
            /// </param>
            /// <returns>
            /// The native status code produced by the verification; zero indicates the
            /// subject is trusted.
            /// </returns>
            [DllImport(DllName.WinTrust,
                CallingConvention = CallingConvention.Winapi,
                CharSet = CharSet.Unicode, SetLastError = true)]
            internal static extern int WinVerifyTrust(
                IntPtr hWnd,
                [MarshalAs(UnmanagedType.LPStruct)] Guid actionId,
                ref WINTRUST_DATA pData
            );
            #endregion
            #endregion
#endif
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Public Methods
        /// <summary>
        /// This method determines whether the specified file is trusted, returning a
        /// simple Boolean result.
        /// </summary>
        /// <param name="fileName">
        /// The name of the file to verify.
        /// </param>
        /// <param name="fileHandle">
        /// An optional open handle to the file to verify.
        /// </param>
        /// <param name="userInterface">
        /// Non-zero if user interface elements may be displayed during verification.
        /// </param>
        /// <param name="userPrompt">
        /// Non-zero if the user may be prompted during verification.
        /// </param>
        /// <param name="revocation">
        /// Non-zero if certificate revocation checking should be performed.
        /// </param>
        /// <param name="install">
        /// Non-zero if the file is being verified in an installation context.
        /// </param>
        /// <returns>
        /// True if the file is trusted; otherwise, false.
        /// </returns>
        public static bool IsFileTrusted(
            string fileName,
            IntPtr fileHandle,
            bool userInterface,
            bool userPrompt,
            bool revocation,
            bool install
            )
        {
            /* !SUCCESS */
            int returnValue = (int)UnsafeNativeMethods.ERROR_SUCCESS + 1;
            Result error = null;

            if ((IsFileTrusted(fileName,
                    fileHandle, userInterface, userPrompt,
                    revocation, install, ref returnValue,
                    ref error) == ReturnCode.Ok) &&
                (returnValue == UnsafeNativeMethods.ERROR_SUCCESS))
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
#if WINDOWS
        /// <summary>
        /// This method returns the action identifier used to select the generic
        /// Authenticode verification policy provider.
        /// </summary>
        /// <returns>
        /// The action identifier for generic Authenticode verification.
        /// </returns>
        private static Guid GetActionId()
        {
            return UnsafeNativeMethods.WINTRUST_ACTION_GENERIC_VERIFY_V2;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method populates the supplied array with the WinTrust parameter values
        /// that correspond to the specified verification options.
        /// </summary>
        /// <param name="userInterface">
        /// Non-zero if user interface elements may be displayed during verification.
        /// </param>
        /// <param name="userPrompt">
        /// Non-zero if the user may be prompted during verification.
        /// </param>
        /// <param name="revocation">
        /// Non-zero if certificate revocation checking should be performed.
        /// </param>
        /// <param name="install">
        /// Non-zero if the file is being verified in an installation context.
        /// </param>
        /// <param name="parameters">
        /// The array to populate with the resulting WinTrust parameter values.
        /// </param>
        private static void InitializeParameters(
            bool userInterface,
            bool userPrompt,
            bool revocation,
            bool install,
            uint[] parameters
            )
        {
            if (userInterface && userPrompt)
                parameters[0] = UnsafeNativeMethods.WTD_UI_ALL;
            else
                parameters[0] = UnsafeNativeMethods.WTD_UI_NONE;

            if (revocation)
                parameters[1] = UnsafeNativeMethods.WTD_REVOKE_WHOLECHAIN;
            else
                parameters[1] = UnsafeNativeMethods.WTD_REVOKE_NONE;

            parameters[2] = UnsafeNativeMethods.WTD_DEFAULT;

            if (install)
                parameters[3] = UnsafeNativeMethods.WTD_UICONTEXT_INSTALL;
            else
                parameters[3] = UnsafeNativeMethods.WTD_UICONTEXT_EXECUTE;
        }

        ///////////////////////////////////////////////////////////////////////

#if !ENTERPRISE_LOCKDOWN
        /// <summary>
        /// This method builds the parsed enumeration value tables representing the
        /// WinTrust parameters derived from the default trust values resource and the
        /// specified verification options.
        /// </summary>
        /// <param name="userInterface">
        /// Non-zero if user interface elements may be displayed during verification.
        /// </param>
        /// <param name="userPrompt">
        /// Non-zero if the user may be prompted during verification.
        /// </param>
        /// <param name="revocation">
        /// Non-zero if certificate revocation checking should be performed.
        /// </param>
        /// <param name="install">
        /// Non-zero if the file is being verified in an installation context.
        /// </param>
        /// <param name="tables">
        /// Upon success, receives the array of parsed enumeration value tables.
        /// </param>
        /// <param name="error">
        /// Upon failure, receives information about the error that occurred.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise,
        /// <see cref="ReturnCode.Error" />.
        /// </returns>
        private static ReturnCode InitializeTables(
            bool userInterface,
            bool userPrompt,
            bool revocation,
            bool install,
            ref ObjectDictionary[] tables,
            ref Result error
            )
        {
            StringList defaultValues = AssemblyOps.GetResourceStreamList(
                GlobalState.GetAssembly(), TrustValuesResourceName, null,
                8, Count.Invalid, true, ref error);

            if (defaultValues == null)
                return ReturnCode.Error;

            StringList values = new StringList();

            if (userInterface && userPrompt)
                values.Add(defaultValues[0]); /* WTD_UI_ALL */
            else
                values.Add(defaultValues[1]); /* WTD_UI_NONE */

            if (revocation)
                values.Add(defaultValues[2]); /* WTD_REVOKE_WHOLECHAIN */
            else
                values.Add(defaultValues[3]); /* WTD_REVOKE_NONE */

            values.Add(defaultValues[4]); /* WTD_SAFER_FLAG */
            values.Add(defaultValues[5]); /* WTD_CACHE_ONLY_URL_RETRIEVAL */

            if (install)
                values.Add(defaultValues[6]); /* WTD_UICONTEXT_INSTALL */
            else
                values.Add(defaultValues[7]); /* WTD_UICONTEXT_EXECUTE */

            if (EnumOps.TryParseTables(null,
                    typeof(TrustValues), values.ToString(), null, true,
                    true, true, ref tables, ref error) != ReturnCode.Ok)
            {
                return ReturnCode.Error;
            }

            return ReturnCode.Ok;
        }
#endif
#endif

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method determines whether the specified file is trusted, resolving the
        /// WinTrust verification parameters (including any configured overrides) before
        /// delegating to the lower-level verification method.
        /// </summary>
        /// <param name="fileName">
        /// The name of the file to verify.
        /// </param>
        /// <param name="fileHandle">
        /// An optional open handle to the file to verify.
        /// </param>
        /// <param name="userInterface">
        /// Non-zero if user interface elements may be displayed during verification.
        /// </param>
        /// <param name="userPrompt">
        /// Non-zero if the user may be prompted during verification.
        /// </param>
        /// <param name="revocation">
        /// Non-zero if certificate revocation checking should be performed.
        /// </param>
        /// <param name="install">
        /// Non-zero if the file is being verified in an installation context.
        /// </param>
        /// <param name="returnValue">
        /// Upon return, receives the native status code produced by the verification;
        /// zero indicates success.
        /// </param>
        /// <param name="error">
        /// Upon failure, receives information about the error that occurred.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise,
        /// <see cref="ReturnCode.Error" />.
        /// </returns>
        private static ReturnCode IsFileTrusted(
            string fileName,
            IntPtr fileHandle,
            bool userInterface,
            bool userPrompt,
            bool revocation,
            bool install,
            ref int returnValue,
            ref Result error
            )
        {
            int parameterLength = (int)TrustValues.PARAMETER_COUNT;
            uint[] parameters = new uint[parameterLength];

#if WINDOWS
            /* NO RESULT */
            InitializeParameters(
                userInterface, userPrompt, revocation, install,
                parameters);

#if !ENTERPRISE_LOCKDOWN
            //
            // TODO: Should this possible be allowed even when built
            //       with the enterprise lockdown option?
            //
            string value = GlobalConfiguration.GetValue(
                EnvVars.TrustFlags, ConfigurationFlags.WinTrustOps);

            if (value != null)
            {
                ObjectDictionary[] tables = null;

                if (InitializeTables(
                        userInterface, userPrompt, revocation, install,
                        ref tables, ref error) != ReturnCode.Ok)
                {
                    TraceOps.DebugTrace(String.Format(
                        "IsFileTrusted: file {0} override failure " +
                        "(InitializeTables), value = {1}, error = {2}",
                        FormatOps.WrapOrNull(fileName),
                        FormatOps.WrapOrNull(value),
                        FormatOps.WrapOrNull(error)),
                        typeof(WinTrustOps).Name,
                        TracePriority.SecurityError);

                    return ReturnCode.Error;
                }

                if (EnumOps.TryParseTables(null,
                        typeof(TrustValues), value, null, true, true,
                        true, ref tables, ref error) != ReturnCode.Ok)
                {
                    TraceOps.DebugTrace(String.Format(
                        "IsFileTrusted: file {0} override failure " +
                        "(TryParseTables), value = {1}, error = {2}",
                        FormatOps.WrapOrNull(fileName),
                        FormatOps.WrapOrNull(value),
                        FormatOps.WrapOrNull(error)),
                        typeof(WinTrustOps).Name,
                        TracePriority.SecurityError);

                    return ReturnCode.Error;
                }

                ulong[] ulongValues = new ulong[parameterLength];

                if (EnumOps.SetParameterValuesFromTables(
                        tables, ulongValues, null, true,
                        ref error) != ReturnCode.Ok)
                {
                    TraceOps.DebugTrace(String.Format(
                        "IsFileTrusted: file {0} override failure " +
                        "(SetParameterValuesFromTables), value = {1}, error = {2}",
                        FormatOps.WrapOrNull(fileName),
                        FormatOps.WrapOrNull(value),
                        FormatOps.WrapOrNull(error)),
                        typeof(WinTrustOps).Name,
                        TracePriority.SecurityError);

                    return ReturnCode.Error;
                }

                ConversionOps.Copy(ref parameters, ulongValues);
            }
#endif
#endif

            return IsFileTrusted(
                fileName, fileHandle, parameters[0], parameters[1],
                parameters[2], parameters[3], userInterface,
                ref returnValue, ref error);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method determines whether the specified file is trusted by invoking the
        /// native WinTrust API with the supplied verification parameters.
        /// </summary>
        /// <param name="fileName">
        /// The name of the file to verify.
        /// </param>
        /// <param name="fileHandle">
        /// An optional open handle to the file to verify.
        /// </param>
        /// <param name="uiChoice">
        /// The WinTrust user interface choice controlling which user interface elements
        /// may be displayed.
        /// </param>
        /// <param name="revocationChecks">
        /// The WinTrust value controlling how certificate revocation checking is
        /// performed.
        /// </param>
        /// <param name="providerFlags">
        /// The WinTrust provider flags controlling the behavior of the trust provider.
        /// </param>
        /// <param name="uiContext">
        /// The WinTrust value indicating the context in which the verification is being
        /// performed.
        /// </param>
        /// <param name="userInterface">
        /// Non-zero if user interface elements may be displayed during verification.
        /// </param>
        /// <param name="returnValue">
        /// Upon return, receives the native status code produced by the verification;
        /// zero indicates success.
        /// </param>
        /// <param name="error">
        /// Upon failure, receives information about the error that occurred.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise,
        /// <see cref="ReturnCode.Error" />.
        /// </returns>
        private static ReturnCode IsFileTrusted(
            string fileName,
            IntPtr fileHandle,
            uint uiChoice,
            uint revocationChecks,
            uint providerFlags,
            uint uiContext,
            bool userInterface,
            ref int returnValue,
            ref Result error
            )
        {
            ReturnCode code;

            if (String.IsNullOrEmpty(fileName))
            {
                error = "invalid file name";
                code = ReturnCode.Error;

                goto done;
            }

#if WINDOWS
            if (!PlatformOps.IsWindowsOperatingSystem())
            {
                error = "not supported on this operating system";
                code = ReturnCode.Error;

                goto done;
            }

            try
            {
                UnsafeNativeMethods.WINTRUST_FILE_INFO file =
                    new UnsafeNativeMethods.WINTRUST_FILE_INFO();

                file.cbStruct = (uint)Marshal.SizeOf(
                    typeof(UnsafeNativeMethods.WINTRUST_FILE_INFO));

                file.pcwszFilePath = fileName;
                file.hFile = fileHandle;
                file.pgKnownSubject = IntPtr.Zero;

                IntPtr pFile = IntPtr.Zero;

                try
                {
                    pFile = Marshal.AllocCoTaskMem((int)file.cbStruct);

                    if (pFile != IntPtr.Zero)
                    {
                        Marshal.StructureToPtr(file, pFile, false);

                        UnsafeNativeMethods.WINTRUST_DATA winTrustData =
                            new UnsafeNativeMethods.WINTRUST_DATA();

                        winTrustData.cbStruct = (uint)Marshal.SizeOf(
                            typeof(UnsafeNativeMethods.WINTRUST_DATA));

                        winTrustData.pPolicyCallbackData = IntPtr.Zero;
                        winTrustData.pSIPClientData = IntPtr.Zero;

                        winTrustData.dwUIChoice = uiChoice;
                        winTrustData.fdwRevocationChecks = revocationChecks;

                        winTrustData.dwUnionChoice =
                            UnsafeNativeMethods.WTD_CHOICE_FILE;

                        winTrustData.pFile = pFile;

                        winTrustData.dwStateAction =
                            UnsafeNativeMethods.WTD_STATEACTION_IGNORE;

                        winTrustData.hWVTStateData = IntPtr.Zero;
                        winTrustData.pwszURLReference = null;

                        winTrustData.dwProvFlags = providerFlags;
                        winTrustData.dwUIContext = uiContext;

                        IntPtr hWnd = userInterface ?
                            WindowOps.GetInteractiveHandle() :
                            INVALID_HANDLE_VALUE;

                        Guid actionId = GetActionId();

                        returnValue = UnsafeNativeMethods.WinVerifyTrust(
                            hWnd, actionId, ref winTrustData);

                        code = ReturnCode.Ok;
                    }
                    else
                    {
                        error = "out of memory";
                        code = ReturnCode.Error;
                    }
                }
                finally
                {
                    if (pFile != IntPtr.Zero)
                    {
                        Marshal.FreeCoTaskMem(pFile);
                        pFile = IntPtr.Zero;
                    }
                }
            }
            catch (Exception e)
            {
                error = e;
                code = ReturnCode.Error;
            }
#else
            error = "not implemented";
            code = ReturnCode.Error;
#endif

        done:

            bool success = (code == ReturnCode.Ok) &&
                (returnValue == UnsafeNativeMethods.ERROR_SUCCESS);

            TracePriority priority = success ?
                TracePriority.SecurityDebug2 : TracePriority.SecurityError;

            TraceOps.DebugTrace(String.Format(
                "IsFileTrusted: file {0} check {1}, " +
                "uiChoice = {2}, revocationChecks = {3}, " +
                "providerFlags = {4}, uiContext = {5}, " +
                "userInterface = {6}, returnValue = {7}, " +
                "error = {8}", FormatOps.WrapOrNull(fileName),
                success ? "success" : "failure",
                uiChoice, revocationChecks, providerFlags,
                uiContext, userInterface, returnValue,
                FormatOps.WrapOrNull(error)),
                typeof(WinTrustOps).Name, priority);

            return code;
        }
        #endregion
    }
}
