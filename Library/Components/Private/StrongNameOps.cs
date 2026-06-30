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

#if !NET_40 || MONO_BUILD
using System.Security.Permissions;
#endif

using Eagle._Attributes;
using Eagle._Components.Public;
using Eagle._Constants;

#if NET_40 && !MONO_BUILD
using System.Runtime.CompilerServices;
using IClrStrongName = Eagle._Components.Private.StrongNameOps.UnsafeNativeMethods.IClrStrongName;
#endif

namespace Eagle._Components.Private
{
    /// <summary>
    /// This class provides helper methods used to verify the Authenticode-style
    /// strong name signature of a managed assembly file via the appropriate
    /// native CLR runtime API for the executing version of the framework.
    /// </summary>
#if NET_40 && !MONO_BUILD
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
        /// <summary>
        /// This class contains the private native APIs, P/Invoke signatures, and
        /// COM interop declarations used to perform strong name signature
        /// verification through the underlying CLR runtime.
        /// </summary>
        [SuppressUnmanagedCodeSecurity()]
        [ObjectId("c02693d7-a96b-42e5-983e-b98c3d973771")]
        internal static class UnsafeNativeMethods
        {
#if WINDOWS
            #region Private Methods
            /// <summary>
            /// This method verifies the strong name signature of the specified
            /// assembly file using the legacy CLR v2 native runtime API.
            /// </summary>
            /// <param name="filePath">
            /// The fully qualified path to the assembly file whose strong name
            /// signature should be verified.
            /// </param>
            /// <param name="forceVerification">
            /// Non-zero to force the signature verification even when it would
            /// otherwise be skipped (e.g. due to a registry verification skip
            /// entry).
            /// </param>
            /// <param name="wasVerified">
            /// Upon return, this is set to non-zero if the verification was
            /// actually performed; otherwise, it is set to zero.
            /// </param>
            /// <returns>
            /// Non-zero if the strong name signature is valid; otherwise, zero.
            /// </returns>
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

#if NET_40 && !MONO_BUILD
            #region Private Constants
            /// <summary>
            /// The class identifier (CLSID) of the CLR strong name COM object
            /// used to obtain an <see cref="IClrStrongName" /> instance from the
            /// runtime.
            /// </summary>
            internal static readonly Guid CLSID_CLRStrongName = new Guid(
                "b79b0acd-f5cd-409b-b5a5-a16244610b92");

            /// <summary>
            /// The interface identifier (IID) of the
            /// <see cref="IClrStrongName" /> COM interface.
            /// </summary>
            internal static readonly Guid IID_CLRStrongName =
                typeof(IClrStrongName).GUID;
            #endregion

            ///////////////////////////////////////////////////////////////////

            #region Private Interfaces
            /// <summary>
            /// This interface exposes the subset of the native CLR strong name
            /// COM interface needed to verify the strong name signature of an
            /// assembly file.  Only the verification method is used; the other
            /// members exist solely to preserve the correct virtual method table
            /// layout.
            /// </summary>
            [ComImport]
            [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
            [Guid("9fd93ccf-3280-4391-b3a9-96e1cde77c8d")]
            [ComConversionLoss]
            [ObjectId("deeb4a0d-dc26-44a4-9311-17da84b5b2a5")]
            internal interface IClrStrongName
            {
                /// <summary>
                /// This method is an unused placeholder that preserves the
                /// virtual method table layout of the native interface.
                /// </summary>
                void Void00();
                /// <summary>
                /// This method is an unused placeholder that preserves the
                /// virtual method table layout of the native interface.
                /// </summary>
                void Void01();
                /// <summary>
                /// This method is an unused placeholder that preserves the
                /// virtual method table layout of the native interface.
                /// </summary>
                void Void02();
                /// <summary>
                /// This method is an unused placeholder that preserves the
                /// virtual method table layout of the native interface.
                /// </summary>
                void Void03();
                /// <summary>
                /// This method is an unused placeholder that preserves the
                /// virtual method table layout of the native interface.
                /// </summary>
                void Void04();
                /// <summary>
                /// This method is an unused placeholder that preserves the
                /// virtual method table layout of the native interface.
                /// </summary>
                void Void05();
                /// <summary>
                /// This method is an unused placeholder that preserves the
                /// virtual method table layout of the native interface.
                /// </summary>
                void Void06();
                /// <summary>
                /// This method is an unused placeholder that preserves the
                /// virtual method table layout of the native interface.
                /// </summary>
                void Void07();
                /// <summary>
                /// This method is an unused placeholder that preserves the
                /// virtual method table layout of the native interface.
                /// </summary>
                void Void08();
                /// <summary>
                /// This method is an unused placeholder that preserves the
                /// virtual method table layout of the native interface.
                /// </summary>
                void Void09();
                /// <summary>
                /// This method is an unused placeholder that preserves the
                /// virtual method table layout of the native interface.
                /// </summary>
                void Void10();
                /// <summary>
                /// This method is an unused placeholder that preserves the
                /// virtual method table layout of the native interface.
                /// </summary>
                void Void11();
                /// <summary>
                /// This method is an unused placeholder that preserves the
                /// virtual method table layout of the native interface.
                /// </summary>
                void Void12();
                /// <summary>
                /// This method is an unused placeholder that preserves the
                /// virtual method table layout of the native interface.
                /// </summary>
                void Void13();
                /// <summary>
                /// This method is an unused placeholder that preserves the
                /// virtual method table layout of the native interface.
                /// </summary>
                void Void14();
                /// <summary>
                /// This method is an unused placeholder that preserves the
                /// virtual method table layout of the native interface.
                /// </summary>
                void Void15();
                /// <summary>
                /// This method is an unused placeholder that preserves the
                /// virtual method table layout of the native interface.
                /// </summary>
                void Void16();
                /// <summary>
                /// This method is an unused placeholder that preserves the
                /// virtual method table layout of the native interface.
                /// </summary>
                void Void17();
                /// <summary>
                /// This method is an unused placeholder that preserves the
                /// virtual method table layout of the native interface.
                /// </summary>
                void Void18();
                /// <summary>
                /// This method is an unused placeholder that preserves the
                /// virtual method table layout of the native interface.
                /// </summary>
                void Void19();

                ///////////////////////////////////////////////////////////////

                /// <summary>
                /// This method verifies the strong name signature of the
                /// specified assembly file using the CLR strong name COM
                /// interface.
                /// </summary>
                /// <param name="filePath">
                /// The fully qualified path to the assembly file whose strong
                /// name signature should be verified.
                /// </param>
                /// <param name="forceVerification">
                /// Non-zero to force the signature verification even when it
                /// would otherwise be skipped (e.g. due to a registry
                /// verification skip entry).
                /// </param>
                /// <param name="wasVerified">
                /// Upon return, this is set to non-zero if the verification was
                /// actually performed; otherwise, it is set to zero.
                /// </param>
                /// <returns>
                /// An HRESULT indicating the outcome; a successful HRESULT means
                /// the strong name signature is valid.
                /// </returns>
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

                /// <summary>
                /// This method is an unused placeholder that preserves the
                /// virtual method table layout of the native interface.
                /// </summary>
                void Void20();
                /// <summary>
                /// This method is an unused placeholder that preserves the
                /// virtual method table layout of the native interface.
                /// </summary>
                void Void21();
                /// <summary>
                /// This method is an unused placeholder that preserves the
                /// virtual method table layout of the native interface.
                /// </summary>
                void Void22();
                /// <summary>
                /// This method is an unused placeholder that preserves the
                /// virtual method table layout of the native interface.
                /// </summary>
                void Void23();
            }
            #endregion
#endif
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Public Methods
        /// <summary>
        /// This method verifies the strong name signature of the specified
        /// assembly file, automatically selecting the appropriate native CLR
        /// runtime API based on the version of the framework in use.
        /// </summary>
        /// <param name="fileName">
        /// The fully qualified path to the assembly file whose strong name
        /// signature should be verified.
        /// </param>
        /// <param name="force">
        /// Non-zero to force the signature verification even when it would
        /// otherwise be skipped (e.g. due to a registry verification skip
        /// entry).
        /// </param>
        /// <param name="clrVersion">
        /// Upon return, this is set to the major version number of the CLR whose
        /// native API was used to attempt the verification.
        /// </param>
        /// <returns>
        /// True if the strong name signature was successfully verified;
        /// otherwise, false.
        /// </returns>
        public static bool IsStrongNameVerifiedClr(
            string fileName,   /* in */
            bool force,        /* in */
            ref int clrVersion /* out */
            )
        {
            bool returnValue; /* REUSED */
            bool verified; /* REUSED */
            Result error; /* REUSED */

#if NET_40 && !MONO_BUILD
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
#endif
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
        /// <summary>
        /// This method verifies the strong name signature of the specified
        /// assembly file using the legacy CLR v2 native runtime API.
        /// </summary>
        /// <param name="fileName">
        /// The fully qualified path to the assembly file whose strong name
        /// signature should be verified.
        /// </param>
        /// <param name="force">
        /// Non-zero to force the signature verification even when it would
        /// otherwise be skipped (e.g. due to a registry verification skip
        /// entry).
        /// </param>
        /// <param name="returnValue">
        /// Upon return, this is set to non-zero if the native API reported the
        /// strong name signature as valid; otherwise, it is set to zero.
        /// </param>
        /// <param name="verified">
        /// Upon return, this is set to non-zero if the signature verification
        /// was actually performed; otherwise, it is set to zero.
        /// </param>
        /// <param name="error">
        /// Upon failure, this is set to an error message or exception describing
        /// why the verification could not be performed.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success;
        /// <see cref="ReturnCode.Error" /> on failure.
        /// </returns>
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

            if (CommonOps.Runtime.IsMono() ||
                CommonOps.Runtime.IsDotNetCore())
            {
                error = "not supported on this platform";
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

        /// <summary>
        /// This method verifies the strong name signature of the specified
        /// assembly file using the CLR v4 strong name COM interface.
        /// </summary>
        /// <param name="fileName">
        /// The fully qualified path to the assembly file whose strong name
        /// signature should be verified.
        /// </param>
        /// <param name="force">
        /// Non-zero to force the signature verification even when it would
        /// otherwise be skipped (e.g. due to a registry verification skip
        /// entry).
        /// </param>
        /// <param name="returnValue">
        /// Upon return, this is set to non-zero if the native API reported the
        /// strong name signature as valid; otherwise, it is set to zero.
        /// </param>
        /// <param name="verified">
        /// Upon return, this is set to non-zero if the signature verification
        /// was actually performed; otherwise, it is set to zero.
        /// </param>
        /// <param name="error">
        /// Upon failure, this is set to an error message or exception describing
        /// why the verification could not be performed.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success;
        /// <see cref="ReturnCode.Error" /> on failure.
        /// </returns>
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

#if NET_40 && !MONO_BUILD
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
