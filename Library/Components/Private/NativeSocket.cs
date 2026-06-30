/*
 * NativeSocket.cs --
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
using System.Net;
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
    /// <summary>
    /// This class provides a platform abstraction over the native sockets
    /// service-resolution APIs, allowing a service port number to be looked up
    /// by its service name and protocol on both Windows and Unix operating
    /// systems via P/Invoke.
    /// </summary>
#if NET_40
    [SecurityCritical()]
#else
    [SecurityPermission(SecurityAction.LinkDemand, UnmanagedCode = true)]
#endif
    [ObjectId("d257cf77-77d6-485b-af5b-aff5dbd8bfb6")]
    internal static class NativeSocket
    {
        ///////////////////////////////////////////////////////////////////////
        // Required Native APIs used via P/Invoke
        ///////////////////////////////////////////////////////////////////////

        #region Unsafe Native Methods Class
        /// <summary>
        /// This class contains the unsafe native method declarations and
        /// associated structures used to resolve service port numbers via
        /// P/Invoke on the supported operating systems.
        /// </summary>
        [SuppressUnmanagedCodeSecurity()]
        [ObjectId("498919ba-d33d-4de7-a702-079c78b6c073")]
        private static class UnsafeNativeMethods
        {
#if WINDOWS
            #region Windows Sockets Structures
            /// <summary>
            /// This structure mirrors the native Windows Sockets <c>servent</c>
            /// structure returned by the <c>getservbyname</c> function,
            /// describing a network service entry.
            /// </summary>
            [StructLayout(LayoutKind.Sequential)]
            [ObjectId("9384bd87-2ff1-45e4-b09d-ee41e2d133bb")]
            internal struct windows_servent
            {
                /// <summary>
                /// The official name of the service.
                /// </summary>
                public string s_name;    // (char *)
                /// <summary>
                /// A pointer to a null-terminated array of alternate names for
                /// the service.
                /// </summary>
                public IntPtr s_aliases; // (char **)
                /// <summary>
                /// The port number at which the service may be contacted, in
                /// network byte order.
                /// </summary>
                public short s_port;     // (short)
                /// <summary>
                /// The name of the protocol to use when contacting the service.
                /// </summary>
                public string s_proto;   // (char *)
            }
            #endregion

            ///////////////////////////////////////////////////////////////////

            #region Windows Sockets Methods
            /// <summary>
            /// This method wraps the native Windows Sockets <c>getservbyname</c>
            /// function, which retrieves service information corresponding to a
            /// service name and protocol.
            /// </summary>
            /// <param name="name">
            /// The name of the service to look up.
            /// </param>
            /// <param name="protocol">
            /// The name of the protocol associated with the service, or null to
            /// match any protocol.
            /// </param>
            /// <returns>
            /// A pointer to a <see cref="windows_servent" /> structure on
            /// success; otherwise, <see cref="IntPtr.Zero" />.
            /// </returns>
            [DllImport(DllName.Ws2_32, EntryPoint = "getservbyname",
                CallingConvention = CallingConvention.Winapi,
                SetLastError = true, CharSet = CharSet.Ansi,
                BestFitMapping = false, ThrowOnUnmappableChar = true)]
            internal static extern IntPtr windows_getservbyname(
                string name, string protocol
            );
            #endregion
#endif

            ///////////////////////////////////////////////////////////////////

#if UNIX
            #region Unix Sockets Structures
            /// <summary>
            /// This structure mirrors the native Unix sockets <c>servent</c>
            /// structure returned by the <c>getservbyname</c> function,
            /// describing a network service entry.
            /// </summary>
            [StructLayout(LayoutKind.Sequential)]
            [ObjectId("7dddccf1-1cde-48d1-8b9e-d4856d9e4947")]
            internal struct unix_servent
            {
                /// <summary>
                /// The official name of the service.
                /// </summary>
                public string s_name;    // (char *)
                /// <summary>
                /// A pointer to a null-terminated array of alternate names for
                /// the service.
                /// </summary>
                public IntPtr s_aliases; // (char **)
                /// <summary>
                /// The port number at which the service may be contacted, in
                /// network byte order.
                /// </summary>
                public int s_port;       // (int)
                /// <summary>
                /// The name of the protocol to use when contacting the service.
                /// </summary>
                public string s_proto;   // (char *)
            }
            #endregion

            ///////////////////////////////////////////////////////////////////

            #region Unix Sockets Methods
            //
            // NOTE: According to the POSIX standard and the Linux man page,
            //       this function does NOT provide any error codes via the
            //       errno variable, etc.
            //
            /// <summary>
            /// This method wraps the native Unix sockets <c>getservbyname</c>
            /// function, which retrieves service information corresponding to a
            /// service name and protocol.
            /// </summary>
            /// <param name="name">
            /// The name of the service to look up.
            /// </param>
            /// <param name="protocol">
            /// The name of the protocol associated with the service, or null to
            /// match any protocol.
            /// </param>
            /// <returns>
            /// A pointer to a <see cref="unix_servent" /> structure on success;
            /// otherwise, <see cref="IntPtr.Zero" />.
            /// </returns>
            [DllImport(DllName.LibC, EntryPoint = "getservbyname",
                CallingConvention = CallingConvention.Cdecl,
                CharSet = CharSet.Ansi, BestFitMapping = false,
                ThrowOnUnmappableChar = true)]
            internal static extern IntPtr unix_getservbyname(
                string name, string protocol
            );
            #endregion
#endif
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Private Windows Specific Methods (DO NOT CALL)
#if WINDOWS
        /// <summary>
        /// This method looks up the port number of a network service by its name
        /// and protocol using the native Windows Sockets APIs.
        /// </summary>
        /// <param name="name">
        /// The name of the service to look up.
        /// </param>
        /// <param name="protocol">
        /// The name of the protocol associated with the service, or null to
        /// match any protocol.
        /// </param>
        /// <param name="error">
        /// Upon failure, receives an error message describing why the lookup
        /// could not be completed.
        /// </param>
        /// <returns>
        /// The port number associated with the service, in host byte order, or
        /// null if it could not be determined.
        /// </returns>
        private static int? WindowsGetPortNumberByNameAndProtocol(
            string name,
            string protocol,
            ref Result error
            )
        {
            try
            {
                IntPtr ptr = UnsafeNativeMethods.windows_getservbyname(
                    name, protocol);

                if (ptr != IntPtr.Zero)
                {
                    UnsafeNativeMethods.windows_servent servent;

                    servent = (UnsafeNativeMethods.windows_servent)
                        Marshal.PtrToStructure(ptr,
                            typeof(UnsafeNativeMethods.windows_servent));

                    return IPAddress.NetworkToHostOrder(
                        servent.s_port);
                }
                else
                {
                    string errorString = NativeOps.MaybeGetErrorMessage();

                    if (errorString != null)
                        error = errorString;
                }
            }
            catch (Exception e)
            {
                error = e;
            }

            return null;
        }
#endif
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Private Unix Specific Methods (DO NOT CALL)
#if UNIX
        /// <summary>
        /// This method looks up the port number of a network service by its name
        /// and protocol using the native Unix sockets APIs.
        /// </summary>
        /// <param name="name">
        /// The name of the service to look up.
        /// </param>
        /// <param name="protocol">
        /// The name of the protocol associated with the service, or null to
        /// match any protocol.
        /// </param>
        /// <param name="error">
        /// Upon failure, receives an error message describing why the lookup
        /// could not be completed.
        /// </param>
        /// <returns>
        /// The port number associated with the service, in host byte order, or
        /// null if it could not be determined.
        /// </returns>
        private static int? UnixGetPortNumberByNameAndProtocol(
            string name,
            string protocol,
            ref Result error
            )
        {
            try
            {
                IntPtr ptr = UnsafeNativeMethods.unix_getservbyname(
                    name, protocol);

                if (ptr != IntPtr.Zero)
                {
                    UnsafeNativeMethods.unix_servent servent;

                    servent = (UnsafeNativeMethods.unix_servent)
                        Marshal.PtrToStructure(ptr,
                            typeof(UnsafeNativeMethods.unix_servent));

                    return IPAddress.NetworkToHostOrder(
                        (short)servent.s_port);
                }
                else
                {
                    string errorString = NativeOps.MaybeGetErrorMessage();

                    if (errorString != null)
                        error = errorString;
                }
            }
            catch (Exception e)
            {
                error = e;
            }

            return null;
        }
#endif
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Public Platform Abstraction Methods
        /// <summary>
        /// This method looks up the port number of a network service by its name
        /// and protocol, dispatching to the appropriate native implementation
        /// for the current operating system.
        /// </summary>
        /// <param name="name">
        /// The name of the service to look up.
        /// </param>
        /// <param name="protocol">
        /// The name of the protocol associated with the service, or null to
        /// match any protocol.
        /// </param>
        /// <param name="error">
        /// Upon failure, receives an error message describing why the lookup
        /// could not be completed.
        /// </param>
        /// <returns>
        /// The port number associated with the service, in host byte order, or
        /// null if it could not be determined or the current operating system is
        /// not supported.
        /// </returns>
        public static int? GetPortNumberByNameAndProtocol(
            string name,
            string protocol,
            ref Result error
            )
        {
#if WINDOWS
            if (PlatformOps.IsWindowsOperatingSystem())
            {
                return WindowsGetPortNumberByNameAndProtocol(
                    name, protocol, ref error);
            }
#endif

#if UNIX
            if (PlatformOps.IsUnixOperatingSystem())
            {
                return UnixGetPortNumberByNameAndProtocol(
                    name, protocol, ref error);
            }
#endif

            error = "not supported on this operating system";
            return null;
        }
        #endregion
    }
}
