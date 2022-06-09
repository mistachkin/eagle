/*
 * StrongNameOps.cs --
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

#if NET_40 && !MONO
using System.Runtime.CompilerServices;
using IClrStrongName = Eagle._Components.Private.StrongNameOps.UnsafeNativeMethods.IClrStrongName;
#endif

namespace Eagle._Components.Private
{
#if NET_40
    [SecurityCritical()]
#else
    [SecurityPermission(SecurityAction.LinkDemand, UnmanagedCode = true)]
#endif
    [ObjectId("fc99d220-d288-43c5-b065-d7d8ac39303e")]
    internal static class StrongNameOps
    {
        ///////////////////////////////////////////////////////////////////////
        // Required Native APIs used via P/Invoke
        ///////////////////////////////////////////////////////////////////////

        #region Private Unsafe Native Methods Class
        [SuppressUnmanagedCodeSecurity()]
        [ObjectId("c02693d7-a96b-42e5-983e-b98c3d973771")]
        internal static class UnsafeNativeMethods
        {
#if WINDOWS && !MONO
            #region Private Methods
            [DllImport(DllName.MsCorEe,
                CallingConvention = CallingConvention.StdCall,
                CharSet = CharSet.Unicode)]
            [return: MarshalAs(UnmanagedType.U1)]
            internal static extern bool StrongNameSignatureVerificationEx(
                [MarshalAs(UnmanagedType.LPWStr)] string filePath,
                [MarshalAs(UnmanagedType.U1)] bool forceVerification,
                [MarshalAs(UnmanagedType.U1)] ref bool wasVerified
            );
            #endregion
#endif

            ///////////////////////////////////////////////////////////////////

#if NET_40 && !MONO
            #region Private Constants
            internal static readonly Guid CLSID_CLRStrongName = new Guid(
                "b79b0acd-f5cd-409b-b5a5-a16244610b92");

            internal static readonly Guid IID_CLRStrongName =
                typeof(IClrStrongName).GUID;
            #endregion

            ///////////////////////////////////////////////////////////////////

            #region Private Interfaces
            [ComImport]
            [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
            [Guid("9fd93ccf-3280-4391-b3a9-96e1cde77c8d")]
            [ComConversionLoss]
            [ObjectId("deeb4a0d-dc26-44a4-9311-17da84b5b2a5")]
            internal interface IClrStrongName
            {
                void Void00();
                void Void01();
                void Void02();
                void Void03();
                void Void04();
                void Void05();
                void Void06();
                void Void07();
                void Void08();
                void Void09();
                void Void10();
                void Void11();
                void Void12();
                void Void13();
                void Void14();
                void Void15();
                void Void16();
                void Void17();
                void Void18();
                void Void19();

                ///////////////////////////////////////////////////////////////

                [return: MarshalAs(UnmanagedType.U4)]
#if !NET_STANDARD_20
                [MethodImpl(MethodImplOptions.InternalCall,
                    MethodCodeType = MethodCodeType.Runtime)]
#endif
                [PreserveSig]
                int StrongNameSignatureVerificationEx(
                    [In, MarshalAs(UnmanagedType.LPWStr)] string filePath,
                    [In, MarshalAs(UnmanagedType.I1)] bool forceVerification,
                    [MarshalAs(UnmanagedType.I1)] out bool wasVerified
                );

                ///////////////////////////////////////////////////////////////

                void Void20();
                void Void21();
                void Void22();
                void Void23();
            }
            #endregion
#endif
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Public Methods
        public static bool IsStrongNameVerifiedClr(
            string fileName,   /* in */
            bool force,        /* in */
            ref int clrVersion /* out */
            )
        {
            bool returnValue; /* REUSED */
            bool verified; /* REUSED */
            Result error; /* REUSED */

            if (CommonOps.Runtime.IsFramework40())
            {
                clrVersion = 4;
                returnValue = false;
                verified = false;
                error = null; /* NOT USED */

                if ((IsStrongNameVerifiedClrV4(fileName,
                        force, ref returnValue, ref verified,
                        ref error) == ReturnCode.Ok) &&
                    returnValue && verified)
                {
                    return true;
                }
                else
                {
                    return false;
                }
            }
            else
            {
                clrVersion = 2;
                returnValue = false;
                verified = false;
                error = null; /* NOT USED */

                if ((IsStrongNameVerifiedClrV2(fileName,
                        force, ref returnValue, ref verified,
                        ref error) == ReturnCode.Ok) &&
                    returnValue && verified)
                {
                    return true;
                }
                else
                {
                    return false;
                }
            }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Private Methods
        private static ReturnCode IsStrongNameVerifiedClrV2(
            string fileName,
            bool force,
            ref bool returnValue,
            ref bool verified,
            ref Result error
            )
        {
            if (String.IsNullOrEmpty(fileName))
            {
                error = "invalid file name";
                goto done;
            }

#if WINDOWS && !MONO
            if (!PlatformOps.IsWindowsOperatingSystem())
            {
                error = "not supported on this operating system";
                goto done;
            }

            try
            {
                returnValue =
                    UnsafeNativeMethods.StrongNameSignatureVerificationEx(
                        fileName, force, ref verified);

                return ReturnCode.Ok;
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
                "IsStrongNameVerifiedClrV2: file {0} verification failure, " +
                "force = {1}, returnValue = {2}, verified = {3}, error = {4}",
                FormatOps.WrapOrNull(fileName), force, returnValue, verified,
                FormatOps.WrapOrNull(error)), typeof(StrongNameOps).Name,
                TracePriority.SecurityError);

            return ReturnCode.Error;
        }

        ///////////////////////////////////////////////////////////////////////

        private static ReturnCode IsStrongNameVerifiedClrV4(
            string fileName,
            bool force,
            ref bool returnValue,
            ref bool verified,
            ref Result error
            )
        {
            if (String.IsNullOrEmpty(fileName))
            {
                error = "invalid file name";
                goto done;
            }

            if (CommonOps.Runtime.IsMono() ||
                CommonOps.Runtime.IsDotNetCore())
            {
                error = "not supported on this platform";
                goto done;
            }

#if NET_40 && !MONO
            try
            {
                Guid clsId = UnsafeNativeMethods.CLSID_CLRStrongName;
                Guid iId = UnsafeNativeMethods.IID_CLRStrongName;

                IClrStrongName clrStrongName =
                    RuntimeEnvironment.GetRuntimeInterfaceAsObject(
                        clsId, iId) as IClrStrongName;

                if (clrStrongName != null)
                {
                    int hResult =
                        clrStrongName.StrongNameSignatureVerificationEx(
                            fileName, force, out verified);

                    returnValue = MarshalOps.ComSucceeded(hResult);

                    if (!returnValue)
                    {
                        TraceOps.DebugTrace(String.Format(
                            "IsStrongNameVerifiedClrV4: " +
                            "file {0} hResult {1} failure",
                            FormatOps.WrapOrNull(fileName),
                            hResult), typeof(StrongNameOps).Name,
                            TracePriority.SecurityError);
                    }

                    return ReturnCode.Ok;
                }
                else
                {
                    error = "unable to get IClrStrongName object";
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
                "IsStrongNameVerifiedClrV4: file {0} verification failure, " +
                "force = {1}, returnValue = {2}, verified = {3}, error = {4}",
                FormatOps.WrapOrNull(fileName), force, returnValue, verified,
                FormatOps.WrapOrNull(error)), typeof(StrongNameOps).Name,
                TracePriority.SecurityError);

            return ReturnCode.Error;
        }
        #endregion
    }
}
