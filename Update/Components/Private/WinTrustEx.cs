/*
 * WinTrustEx.cs --
 *
 * Copyright (c) 2007-2012 by Joe Mistachkin.  All rights reserved.
 *
 * See the file "license.terms" for information on usage and redistribution of
 * this file, and for a DISCLAIMER OF ALL WARRANTIES.
 *
 * RCS: @(#) $Id: $
 */

#if !NATIVE || !WINDOWS
#error "This file cannot be compiled or used properly with native Windows code disabled."
#endif

using System;
using System.Runtime.InteropServices;

#if !DEBUG || NET_40 || NET_STANDARD_20
using System.Security;
#endif

#if !NET_40
using System.Security.Permissions;
#endif

namespace Eagle._Components.Private
{
    /// <summary>
    /// This class provides Authenticode file trust verification support via the
    /// native Windows WinTrust API.
    /// </summary>
#if NET_40 || NET_STANDARD_20
    [SecurityCritical()]
#else
    [SecurityPermission(SecurityAction.LinkDemand, UnmanagedCode = true)]
#endif
    [Guid("536028ed-7aa2-4341-b349-c11a00a50b03")]
    internal static class WinTrustEx
    {
        #region Private Constants
#if !DEBUG
        /// <summary>
        /// The native invalid handle value, used as the parent window handle to
        /// suppress any user interface during trust verification.
        /// </summary>
        private static readonly IntPtr INVALID_HANDLE_VALUE = new IntPtr(-1);
#endif
        #endregion

        ///////////////////////////////////////////////////////////////////////
        // Required Native APIs used via P/Invoke
        ///////////////////////////////////////////////////////////////////////

        #region Private Unsafe Native Methods Class
#if !DEBUG
        /// <summary>
        /// This class contains the native constants, structures, and P/Invoke
        /// declarations used to call the Windows WinTrust API.
        /// </summary>
        [SuppressUnmanagedCodeSecurity()]
        [Guid("8bc0abad-d1e3-4788-9e92-3f4e80f8df92")]
        private static class UnsafeNativeMethods
        {
            //
            // NOTE: Currently, this constant is only used with the WinTrust
            //       API; however, it is still needed even when the WINDOWS
            //       compile-time option is disabled.
            //
            /// <summary>
            /// The native success status code returned when an operation
            /// completes successfully.
            /// </summary>
            internal const uint ERROR_SUCCESS = 0;

            ///////////////////////////////////////////////////////////////////

#if WINDOWS
            #region WinTrust API
            #region Constants
            /// <summary>
            /// The action identifier that selects the generic Authenticode
            /// verification policy provider.
            /// </summary>
            internal static readonly Guid WINTRUST_ACTION_GENERIC_VERIFY_V2 =
                new Guid("00aac56b-cd44-11d0-8cc2-00c04fc295ee");

            /// <summary>
            /// Requests that all user interface elements be displayed during
            /// verification.
            /// </summary>
            internal const uint WTD_UI_ALL = 1;

            /// <summary>
            /// Requests that no user interface be displayed during
            /// verification.
            /// </summary>
            internal const uint WTD_UI_NONE = 2;

            /// <summary>
            /// Specifies that no revocation checking should be performed.
            /// </summary>
            internal const uint WTD_REVOKE_NONE = 0x0;

            /// <summary>
            /// Specifies that the entire certificate chain should be checked for
            /// revocation.
            /// </summary>
            internal const uint WTD_REVOKE_WHOLECHAIN = 0x1;

            /// <summary>
            /// Specifies that the subject of the verification is a file.
            /// </summary>
            internal const uint WTD_CHOICE_FILE = 1;

            /// <summary>
            /// Specifies the user interface context for verifying an executable
            /// being run.
            /// </summary>
            internal const uint WTD_UICONTEXT_EXECUTE = 0;

            /// <summary>
            /// Specifies the user interface context for verifying an executable
            /// being installed.
            /// </summary>
            internal const uint WTD_UICONTEXT_INSTALL = 1;

            /// <summary>
            /// Specifies that no persistent state action should be taken for the
            /// verification.
            /// </summary>
            internal const uint WTD_STATEACTION_IGNORE = 0x0;

            /// <summary>
            /// Specifies that the verification should use the Software
            /// Restriction Policies (SAFER) flag.
            /// </summary>
            internal const uint WTD_SAFER_FLAG = 0x100;
            #endregion

            ///////////////////////////////////////////////////////////////////

            #region Structures
            /// <summary>
            /// This structure provides information about a file to be verified
            /// by the WinTrust API.
            /// </summary>
            [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
            [Guid("44180a13-a903-4700-8230-f171856562ac")]
            internal struct WINTRUST_FILE_INFO
            {
                /// <summary>
                /// The size, in bytes, of this structure.
                /// </summary>
                public /* DWORD */ uint cbStruct;

                /// <summary>
                /// The full path and file name of the file to be verified.
                /// </summary>
                public /* LPCWSTR */ string pcwszFilePath;

                /// <summary>
                /// An optional open handle to the file to be verified.
                /// </summary>
                public /* HANDLE */ IntPtr hFile;

                /// <summary>
                /// An optional pointer to the known subject GUID for the file.
                /// </summary>
                public /* LPGUID */ IntPtr pgKnownSubject;
            }

            ///////////////////////////////////////////////////////////////////

            /// <summary>
            /// This structure provides the data passed to the WinTrust API to
            /// control and direct a trust verification operation.
            /// </summary>
            [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
            [Guid("7c80a4b2-d998-45f3-a4b7-99e7bcb58eaa")]
            internal struct WINTRUST_DATA
            {
                /// <summary>
                /// The size, in bytes, of this structure.
                /// </summary>
                public /* DWORD */ uint cbStruct;

                /// <summary>
                /// An optional pointer to policy-specific callback data.
                /// </summary>
                public /* LPVOID */ IntPtr pPolicyCallbackData;

                /// <summary>
                /// An optional pointer to Subject Interface Package (SIP)
                /// client data.
                /// </summary>
                public /* LPVOID */ IntPtr pSIPClientData;

                /// <summary>
                /// The user interface choice for the verification operation.
                /// </summary>
                public /* DWORD */ uint dwUIChoice;

                /// <summary>
                /// The revocation checking options for the verification
                /// operation.
                /// </summary>
                public /* DWORD */ uint fdwRevocationChecks;

                /// <summary>
                /// The kind of subject described by the union member; here, a
                /// file.
                /// </summary>
                public /* DWORD */ uint dwUnionChoice;

                /// <summary>
                /// A pointer to the <see cref="WINTRUST_FILE_INFO" /> structure
                /// describing the file being verified.
                /// </summary>
                public /* PWINTRUST_FILE_INFO */ IntPtr pFile;

                /// <summary>
                /// The persistent state action for the verification operation.
                /// </summary>
                public /* DWORD */ uint dwStateAction;

                /// <summary>
                /// A handle used to communicate state between calls; not used
                /// here.
                /// </summary>
                public /* HANDLE */ IntPtr hWVTStateData;

                /// <summary>
                /// An optional URL reference for the verification operation.
                /// </summary>
                public /* LPWSTR */ string pwszURLReference;

                /// <summary>
                /// The provider-specific flags for the verification operation.
                /// </summary>
                public /* DWORD */ uint dwProvFlags;

                /// <summary>
                /// The user interface context for the verification operation.
                /// </summary>
                public /* DWORD */ uint dwUIContext;
            }
            #endregion

            ///////////////////////////////////////////////////////////////////

            #region Functions
            /// <summary>
            /// The name of the native library that exports the WinTrust API.
            /// </summary>
            private const string WinTrust = "wintrust.dll";

            ///////////////////////////////////////////////////////////////////

            /// <summary>
            /// This method performs a trust verification action on the
            /// specified object using the WinTrust API.
            /// </summary>
            /// <param name="hWnd">
            /// A handle to the parent window for any user interface, or the
            /// invalid handle value to suppress the user interface.
            /// </param>
            /// <param name="actionId">
            /// The action identifier that selects the verification policy
            /// provider to be used.
            /// </param>
            /// <param name="pData">
            /// The <see cref="WINTRUST_DATA" /> structure that describes the
            /// object to be verified and how it should be verified.
            /// </param>
            /// <returns>
            /// Zero if the object is trusted; otherwise, a non-zero status code
            /// describing the failure.
            /// </returns>
            [DllImport(WinTrust,
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
#endif
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Public Methods
        /// <summary>
        /// This method determines whether the specified file is trusted (i.e.
        /// has a valid Authenticode signature) using the WinTrust API.  In
        /// DEBUG builds, the verification is skipped and the file is treated as
        /// trusted.
        /// </summary>
        /// <param name="configuration">
        /// The configuration used for diagnostic tracing, if any.
        /// </param>
        /// <param name="fileName">
        /// The name of the file to be verified.
        /// </param>
        /// <param name="fileHandle">
        /// An optional open handle to the file to be verified.
        /// </param>
        /// <param name="userInterface">
        /// Non-zero to permit the display of a user interface during
        /// verification.
        /// </param>
        /// <param name="userPrompt">
        /// Non-zero to permit prompting the user during verification.
        /// </param>
        /// <param name="revocation">
        /// Non-zero to perform revocation checking on the entire certificate
        /// chain.
        /// </param>
        /// <param name="install">
        /// Non-zero if the file is being installed; zero if it is being run.
        /// </param>
        /// <param name="error">
        /// Upon failure, receives an error message describing why the file is
        /// not trusted.
        /// </param>
        /// <returns>
        /// True if the file is trusted; otherwise, false.
        /// </returns>
        public static bool IsFileTrusted(
            Configuration configuration,
            string fileName,
            IntPtr fileHandle,
            bool userInterface,
            bool userPrompt,
            bool revocation,
            bool install,
            ref string error
            )
        {
#if !DEBUG
            /* !SUCCESS */
            int returnValue = (int)UnsafeNativeMethods.ERROR_SUCCESS + 1;
            string localError = null;

            if ((IsFileTrusted(
                    fileName, fileHandle, userInterface,
                    userPrompt, revocation,
                    install, ref returnValue,
                    ref localError)) &&
                (returnValue == UnsafeNativeMethods.ERROR_SUCCESS))
            {
                return true;
            }
            else
            {
                if (localError != null)
                {
                    error = localError;
                }
                else if (returnValue != UnsafeNativeMethods.ERROR_SUCCESS)
                {
                    error = String.Format(
                        "WinVerifyTrust() failed with error 0x{0:X}.",
                        returnValue);
                }

                return false;
            }
#else
            //
            // NOTE: Emit a log entry so that the user knows for sure
            //       that we did NOT actually verify the file trust.
            //
            TraceOps.Trace(configuration, String.Format(
                "File \"{0}\" certificate unchecked: " +
                "WinVerifyTrust use is disabled.",
                fileName), typeof(WinTrustEx).Name);

            //
            // NOTE: In-development version, fake it.  We can do this
            //       because DEBUG builds are never officially released.
            //
            return true;
#endif
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Private Methods
#if !DEBUG
        /// <summary>
        /// This method returns the action identifier that selects the generic
        /// Authenticode verification policy provider.
        /// </summary>
        /// <returns>
        /// The action identifier used for generic Authenticode verification.
        /// </returns>
        private static Guid GetActionId()
        {
            return UnsafeNativeMethods.WINTRUST_ACTION_GENERIC_VERIFY_V2;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method verifies the trust of the specified file by populating
        /// the native WinTrust structures and calling the WinTrust API.
        /// </summary>
        /// <param name="fileName">
        /// The name of the file to be verified.
        /// </param>
        /// <param name="fileHandle">
        /// An optional open handle to the file to be verified.
        /// </param>
        /// <param name="userInterface">
        /// Non-zero to permit the display of a user interface during
        /// verification.
        /// </param>
        /// <param name="userPrompt">
        /// Non-zero to permit prompting the user during verification.
        /// </param>
        /// <param name="revocation">
        /// Non-zero to perform revocation checking on the entire certificate
        /// chain.
        /// </param>
        /// <param name="install">
        /// Non-zero if the file is being installed; zero if it is being run.
        /// </param>
        /// <param name="returnValue">
        /// Upon return, receives the status code produced by the WinTrust API.
        /// </param>
        /// <param name="error">
        /// Upon failure, receives an error message describing the failure.
        /// </param>
        /// <returns>
        /// True if the WinTrust API was called successfully (regardless of the
        /// trust result, which is reported via <paramref name="returnValue" />);
        /// otherwise, false.
        /// </returns>
        private static bool IsFileTrusted(
            string fileName,
            IntPtr fileHandle,
            bool userInterface,
            bool userPrompt,
            bool revocation,
            bool install,
            ref int returnValue,
            ref string error
            )
        {
            if (String.IsNullOrEmpty(fileName))
            {
                error = "invalid file name";
                return false;
            }

#if WINDOWS
            if (!VersionOps.IsWindowsOperatingSystem())
            {
                error = "not supported on this operating system";
                return false;
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

                        winTrustData.dwUIChoice = userInterface && userPrompt ?
                            UnsafeNativeMethods.WTD_UI_ALL :
                            UnsafeNativeMethods.WTD_UI_NONE;

                        winTrustData.fdwRevocationChecks = revocation ?
                            UnsafeNativeMethods.WTD_REVOKE_WHOLECHAIN :
                            UnsafeNativeMethods.WTD_REVOKE_NONE;

                        winTrustData.dwUnionChoice =
                            UnsafeNativeMethods.WTD_CHOICE_FILE;

                        winTrustData.pFile = pFile;

                        winTrustData.dwStateAction =
                            UnsafeNativeMethods.WTD_STATEACTION_IGNORE;

                        winTrustData.hWVTStateData = IntPtr.Zero;
                        winTrustData.pwszURLReference = null;

                        winTrustData.dwProvFlags =
                            UnsafeNativeMethods.WTD_SAFER_FLAG;

                        winTrustData.dwUIContext = install ?
                            UnsafeNativeMethods.WTD_UICONTEXT_INSTALL :
                            UnsafeNativeMethods.WTD_UICONTEXT_EXECUTE;

                        IntPtr hWnd = userInterface ?
                            IntPtr.Zero : INVALID_HANDLE_VALUE;

                        Guid actionId = GetActionId();

                        returnValue = UnsafeNativeMethods.WinVerifyTrust(
                            hWnd, actionId, ref winTrustData);

                        return true;
                    }
                    else
                    {
                        error = "out of memory";
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
                error = e.ToString();
            }
#else
            error = "not implemented";
#endif

            return false;
        }
#endif
        #endregion
    }
}
