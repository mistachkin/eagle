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
using System.Collections.Generic;
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

        ///////////////////////////////////////////////////////////////////////

        private const string TrustValuesResourceName = "DefaultTrustValues.txt";
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

            ///////////////////////////////////////////////////////////////////

            internal const uint WTD_UI_ALL =
                (uint)TrustValues.WTD_UI_ALL;

            internal const uint WTD_UI_NONE =
                (uint)TrustValues.WTD_UI_NONE;

            ///////////////////////////////////////////////////////////////////

            internal const uint WTD_REVOKE_NONE =
                (uint)TrustValues.WTD_REVOKE_NONE;

            internal const uint WTD_REVOKE_WHOLECHAIN =
                (uint)TrustValues.WTD_REVOKE_WHOLECHAIN;

            ///////////////////////////////////////////////////////////////////

            internal const uint WTD_CHOICE_FILE = 1;

            ///////////////////////////////////////////////////////////////////

            internal const uint WTD_STATEACTION_IGNORE = 0x0;

            ///////////////////////////////////////////////////////////////////

            internal const uint WTD_SAFER_FLAG =
                (uint)TrustValues.WTD_SAFER_FLAG;

            internal const uint WTD_CACHE_ONLY_URL_RETRIEVAL =
                (uint)TrustValues.WTD_CACHE_ONLY_URL_RETRIEVAL;

            internal const uint WTD_DEFAULT =
                WTD_SAFER_FLAG | WTD_CACHE_ONLY_URL_RETRIEVAL;

            ///////////////////////////////////////////////////////////////////

            internal const uint WTD_UICONTEXT_EXECUTE =
                (uint)TrustValues.WTD_UICONTEXT_EXECUTE;

            internal const uint WTD_UICONTEXT_INSTALL =
                (uint)TrustValues.WTD_UICONTEXT_INSTALL;
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

        ///////////////////////////////////////////////////////////////////////

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
