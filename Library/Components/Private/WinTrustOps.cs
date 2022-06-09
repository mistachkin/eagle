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

namespace Eagle._Components.Private
{
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
        private static readonly IntPtr INVALID_HANDLE_VALUE = new IntPtr(-1);
#endif
        #endregion

        ///////////////////////////////////////////////////////////////////////
        // Required Native APIs used via P/Invoke
        ///////////////////////////////////////////////////////////////////////

        #region Private Unsafe Native Methods Class
        [SuppressUnmanagedCodeSecurity()]
        [ObjectId("e8dc85ea-ceba-47db-9ed0-a66ba3f0f916")]
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
            [ObjectId("5c7cdbf0-c943-49b7-87fe-471813fa88d5")]
            internal struct WINTRUST_FILE_INFO
            {
                public /* DWORD */ uint cbStruct;
                public /* LPCWSTR */ string pcwszFilePath;
                public /* HANDLE */ IntPtr hFile;
                public /* LPGUID */ IntPtr pgKnownSubject;
            }

            ///////////////////////////////////////////////////////////////////

            [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
            [ObjectId("7cb4009e-d4c5-403c-a246-16751dfacb6b")]
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
        private static Guid GetActionId()
        {
            return UnsafeNativeMethods.WINTRUST_ACTION_GENERIC_VERIFY_V2;
        }
#endif

        ///////////////////////////////////////////////////////////////////////

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
            if (String.IsNullOrEmpty(fileName))
            {
                error = "invalid file name";
                goto done;
            }

#if WINDOWS
            if (!PlatformOps.IsWindowsOperatingSystem())
            {
                error = "not supported on this operating system";
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
                            WindowOps.GetInteractiveHandle() :
                            INVALID_HANDLE_VALUE;

                        Guid actionId = GetActionId();

                        returnValue = UnsafeNativeMethods.WinVerifyTrust(
                            hWnd, actionId, ref winTrustData);

                        return ReturnCode.Ok;
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
                error = e;
            }
#else
            error = "not implemented";
#endif

        done:

            TraceOps.DebugTrace(String.Format(
                "IsFileTrusted: file {0} trust failure, " +
                "userInterface = {1}, revocation = {2}, " +
                "install = {3}, returnValue = {4}, error = {5}",
                FormatOps.WrapOrNull(fileName),
                userInterface, revocation, install,
                returnValue, FormatOps.WrapOrNull(error)),
                typeof(WinTrustOps).Name,
                TracePriority.SecurityError);

            return ReturnCode.Error;
        }
        #endregion
    }
}
