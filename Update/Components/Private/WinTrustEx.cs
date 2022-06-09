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
        private static readonly IntPtr INVALID_HANDLE_VALUE = new IntPtr(-1);
#endif
        #endregion

        ///////////////////////////////////////////////////////////////////////
        // Required Native APIs used via P/Invoke
        ///////////////////////////////////////////////////////////////////////

        #region Private Unsafe Native Methods Class
#if !DEBUG
        [SuppressUnmanagedCodeSecurity()]
        [Guid("8bc0abad-d1e3-4788-9e92-3f4e80f8df92")]
        private static class UnsafeNativeMethods
        {
            //
            // NOTE: Currently, this constant is only used with the WinTrust
            //       API; however, it is still needed even when the WINDOWS
            //       compile-time option is disabled.
            //
            internal const uint ERROR_SUCCESS = 0;

            ///////////////////////////////////////////////////////////////////

#if WINDOWS
            #region WinTrust API
            #region Constants
            internal static readonly Guid WINTRUST_ACTION_GENERIC_VERIFY_V2 =
                new Guid("00aac56b-cd44-11d0-8cc2-00c04fc295ee");

            internal const uint WTD_UI_ALL = 1;
            internal const uint WTD_UI_NONE = 2;

            internal const uint WTD_REVOKE_NONE = 0x0;
            internal const uint WTD_REVOKE_WHOLECHAIN = 0x1;

            internal const uint WTD_CHOICE_FILE = 1;

            internal const uint WTD_UICONTEXT_EXECUTE = 0;
            internal const uint WTD_UICONTEXT_INSTALL = 1;

            internal const uint WTD_STATEACTION_IGNORE = 0x0;

            internal const uint WTD_SAFER_FLAG = 0x100;
            #endregion

            ///////////////////////////////////////////////////////////////////

            #region Structures
            [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
            [Guid("44180a13-a903-4700-8230-f171856562ac")]
            internal struct WINTRUST_FILE_INFO
            {
                public /* DWORD */ uint cbStruct;
                public /* LPCWSTR */ string pcwszFilePath;
                public /* HANDLE */ IntPtr hFile;
                public /* LPGUID */ IntPtr pgKnownSubject;
            }

            ///////////////////////////////////////////////////////////////////

            [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
            [Guid("7c80a4b2-d998-45f3-a4b7-99e7bcb58eaa")]
            internal struct WINTRUST_DATA
            {
                public /* DWORD */ uint cbStruct;
                public /* LPVOID */ IntPtr pPolicyCallbackData;
                public /* LPVOID */ IntPtr pSIPClientData;
                public /* DWORD */ uint dwUIChoice;
                public /* DWORD */ uint fdwRevocationChecks;
                public /* DWORD */ uint dwUnionChoice;
                public /* PWINTRUST_FILE_INFO */ IntPtr pFile;
                public /* DWORD */ uint dwStateAction;
                public /* HANDLE */ IntPtr hWVTStateData;
                public /* LPWSTR */ string pwszURLReference;
                public /* DWORD */ uint dwProvFlags;
                public /* DWORD */ uint dwUIContext;
            }
            #endregion

            ///////////////////////////////////////////////////////////////////

            #region Functions
            private const string WinTrust = "wintrust.dll";

            ///////////////////////////////////////////////////////////////////

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
        private static Guid GetActionId()
        {
            return UnsafeNativeMethods.WINTRUST_ACTION_GENERIC_VERIFY_V2;
        }

        ///////////////////////////////////////////////////////////////////////

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
