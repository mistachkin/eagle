/*
 * ConversionOps.cs --
 *
 * Copyright (c) 2007-2012 by Joe Mistachkin.  All rights reserved.
 *
 * See the file "license.terms" for information on usage and redistribution of
 * this file, and for a DISCLAIMER OF ALL WARRANTIES.
 *
 * RCS: @(#) $Id: $
 */

using System;
using System.Collections.Generic;
using System.Globalization;

#if NET_40
using System.Numerics;
#endif

using System.Reflection;
using System.Threading;
using Eagle._Attributes;
using Eagle._Components.Public;
using Eagle._Components.Public.Delegates;
using Eagle._Constants;
using Eagle._Containers.Private;
using Eagle._Containers.Public;
using Eagle._Interfaces.Public;
using _Public = Eagle._Components.Public;

#if NET_STANDARD_21
using Index = Eagle._Constants.Index;
#endif

namespace Eagle._Components.Private
{
    /// <summary>
    /// This class provides a collection of static helper methods used to
    /// convert between the various primitive value types (and a handful of
    /// related framework types) supported by the library.  It also contains
    /// the nested helper types and callback tables used to support dynamic
    /// (string-based) value conversions performed during marshalling.
    /// </summary>
    [ObjectId("d93666f3-561b-4257-aaf7-8fd5a5436de9")]
    internal static class ConversionOps
    {
        #region Private Constants
        /// <summary>
        /// The number of bits contained in a byte value.
        /// </summary>
        public static readonly int ByteBits = ToInt(MathOps.Log2(byte.MaxValue)) + 1;

        /// <summary>
        /// The number of bits contained in a short (or ushort) value.
        /// </summary>
        private static readonly int ShortBits = ToInt(MathOps.Log2(ushort.MaxValue)) + 1;

        /// <summary>
        /// The number of bits contained in an int (or uint) value.
        /// </summary>
        public static readonly int IntBits = ToInt(MathOps.Log2(uint.MaxValue)) + 1;

        /// <summary>
        /// The number of bits contained in a long (or ulong) value.
        /// </summary>
        public static readonly int LongBits = ToInt(MathOps.Log2(ulong.MaxValue)) + 1;

        ///////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// The number of bits contained in two byte values.
        /// </summary>
        private static readonly int TwoByteBits = ByteBits * 2;

        /// <summary>
        /// The number of bits contained in four byte values.
        /// </summary>
        private static readonly int FourByteBits = ByteBits * 4;

        /// <summary>
        /// The number of bits contained in six byte values.
        /// </summary>
        private static readonly int SixByteBits = ByteBits * 6;

        ///////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// The bit mask used to select the high-order byte of a character
        /// value.
        /// </summary>
        private const char CharHighByte = (char)0xFF00;

        /// <summary>
        /// The bit mask used to select the low-order byte of a character
        /// value.
        /// </summary>
        private const char CharLowByte = (char)byte.MaxValue;

        ///////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// The bit mask used to select the high-order 16 bits of an integer
        /// value.
        /// </summary>
        private const int IntHighShort = unchecked((int)0xFFFF0000);

        /// <summary>
        /// The bit mask used to select the low-order 16 bits of an integer
        /// value.
        /// </summary>
        private const int IntLowShort = (int)ushort.MaxValue;

        ///////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// The bit mask used to select the highest-order 16 bits of a long
        /// integer value.
        /// </summary>
        private const long LongHighShort = unchecked((long)0xFFFF000000000000);

        /// <summary>
        /// The bit mask used to select the upper-middle 16 bits of a long
        /// integer value.
        /// </summary>
        private const long LongHighMidShort = 0xFFFF00000000;

        /// <summary>
        /// The bit mask used to select the lower-middle 16 bits of a long
        /// integer value.
        /// </summary>
        private const long LongLowMidShort = 0xFFFF0000;

        /// <summary>
        /// The bit mask used to select the lowest-order 16 bits of a long
        /// integer value.
        /// </summary>
        private const long LongLowShort = 0xFFFF;

        ///////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// The bit mask used to select the high-order 32 bits of a long
        /// integer value.
        /// </summary>
        private const long LongHighInt = unchecked((long)0xFFFFFFFF00000000);

        /// <summary>
        /// The bit mask used to select the low-order 32 bits of a long integer
        /// value.
        /// </summary>
        private const long LongLowInt = (long)uint.MaxValue;

        ///////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// The string used to represent a request to enable something.
        /// </summary>
        private const string EnableString = "enable";

        /// <summary>
        /// The string used to represent a request to disable something.
        /// </summary>
        private const string DisableString = "disable";

        /// <summary>
        /// The string used to represent the enabled state.
        /// </summary>
        private const string EnabledString = "enabled";

        /// <summary>
        /// The string used to represent the disabled state.
        /// </summary>
        private const string DisabledString = "disabled";
        #endregion

        ///////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method determines whether the specified type is a delegate
        /// type.
        /// </summary>
        /// <param name="type">
        /// The type to check.
        /// </param>
        /// <param name="strict">
        /// When true, only the <see cref="Delegate" /> type itself is
        /// considered a match; when false, any type derived from
        /// <see cref="Delegate" /> is also considered a match.
        /// </param>
        /// <returns>
        /// True if the type is considered a delegate type; otherwise, false.
        /// </returns>
        public static bool IsDelegateType(Type type, bool strict)
        {
            if (type != null)
            {
                Type delegateType = typeof(Delegate);

                if (type == delegateType)
                    return true;

                if (strict)
                    return false;

                if (type.IsSubclassOf(delegateType))
                    return true;
            }

            return false;
        }

        ///////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method determines whether the specified type is one of the
        /// built-in delegate types directly supported by the library.
        /// </summary>
        /// <param name="type">
        /// The type to check.
        /// </param>
        /// <param name="useGenericCallback">
        /// When true, the <see cref="GenericCallback" /> delegate type is
        /// included among the supported built-in delegate types.
        /// </param>
        /// <param name="useDynamicCallback">
        /// When true, the <see cref="DynamicInvokeCallback" /> delegate type is
        /// included among the supported built-in delegate types.
        /// </param>
        /// <returns>
        /// True if the type is one of the supported built-in delegate types;
        /// otherwise, false.
        /// </returns>
        private static bool IsBuiltInDelegateType(
            Type type,
            bool useGenericCallback,
            bool useDynamicCallback
            )
        {
            if (type == null)
                return false;

            if (LooksLikeAsyncCallback(type))
                return true;

            if (LooksLikeEventHandler(type))
                return true;

            if (LooksLikeThreadStart(type))
                return true;

            if (LooksLikeParameterizedThreadStart(type))
                return true;

            if (LooksLikeWaitCallback(type))
                return true;

            if (useGenericCallback && LooksLikeGenericCallback(type))
                return true;

            if (useDynamicCallback && LooksLikeDynamicInvokeCallback(type))
                return true;

            return false;
        }

        ///////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method determines whether the specified type is a delegate
        /// type that can be created via the command callback mechanism.
        /// </summary>
        /// <param name="type">
        /// The type to check.
        /// </param>
        /// <param name="useDelegateCallback">
        /// When true, the <see cref="Delegate" /> type itself is considered a
        /// supported delegate type.
        /// </param>
        /// <param name="useGenericCallback">
        /// When true, the <see cref="GenericCallback" /> delegate type is
        /// considered a supported built-in delegate type.
        /// </param>
        /// <param name="useDynamicCallback">
        /// When true, the <see cref="DynamicInvokeCallback" /> delegate type is
        /// considered a supported built-in delegate type.
        /// </param>
        /// <param name="isDelegate">
        /// Upon return, this will be true if the specified type is the
        /// <see cref="Delegate" /> type itself.
        /// </param>
        /// <returns>
        /// True if the type is a supported delegate type; otherwise, false.
        /// </returns>
        private static bool IsSupportedDelegateType(
            Type type,
            bool useDelegateCallback,
            bool useGenericCallback,
            bool useDynamicCallback,
            out bool isDelegate
            )
        {
            //
            // NOTE: Determine if the specified type is the System.Delegate type
            //       itself (this requires some special handling).
            //
            isDelegate = IsDelegate(type);

            //
            // NOTE: If this looks like a supported delegate type, we can use our
            //       CommandCallback class; otherwise, this is currently an error.
            //
            // TODO: Eventually, we will support converting to arbitrary delegate
            //       types; however, this conversion currently exists primarily to
            //       facilitate integration with WinForms and Xaml (WPF).
            //
            // DONE: The above *TODO* is now complete (as of Beta 34).
            //
            if (useDelegateCallback && isDelegate)
                return true;

            if (IsBuiltInDelegateType(
                    type, useGenericCallback, useDynamicCallback))
            {
                return true;
            }

            return false;
        }

        ///////////////////////////////////////////////////////////////////////////////////////

        #region DelegateMethods Class
        /// <summary>
        /// This class provides a set of no-op instance methods whose signatures
        /// match the various supported delegate types.  Instances of those
        /// delegate types are created from these methods solely so that their
        /// method signatures (i.e. <see cref="MethodInfo" />) can be used to
        /// probe whether an arbitrary delegate type is signature-compatible.
        /// </summary>
        [ObjectId("a88d72f9-b067-44ec-9457-b6e8000cf378")]
        private sealed class DelegateMethods
        {
            #region Public Constructors
            /// <summary>
            /// Constructs an instance of the <see cref="DelegateMethods" />
            /// class.
            /// </summary>
            public DelegateMethods()
            {
                // do nothing.
            }
            #endregion

            ///////////////////////////////////////////////////////////////////////////////////

            #region Public "Delegate" Methods
            /// <summary>
            /// This method does nothing.  Its signature matches the
            /// <see cref="AsyncCallback" /> delegate type.
            /// </summary>
            /// <param name="ar">
            /// The asynchronous operation status; it is not used.
            /// </param>
            /* System.AsyncCallback */
            public void NullAsyncCallback(
                IAsyncResult ar
                )
            {
                // do nothing.
            }

            ///////////////////////////////////////////////////////////////////////////////////

            /// <summary>
            /// This method does nothing.  Its signature matches the
            /// <see cref="EventHandler" /> delegate type.
            /// </summary>
            /// <param name="sender">
            /// The source of the event; it is not used.
            /// </param>
            /// <param name="e">
            /// The event data; it is not used.
            /// </param>
            /* System.EventHandler */
            public void NullEventHandler(
                object sender,
                EventArgs e
                )
            {
                // do nothing.
            }

            ///////////////////////////////////////////////////////////////////////////////////

            /// <summary>
            /// This method does nothing.  Its signature matches the
            /// <see cref="ThreadStart" /> delegate type.
            /// </summary>
            /* System.Threading.ThreadStart */
            public void NullThreadStart()
            {
                // do nothing.
            }

            ///////////////////////////////////////////////////////////////////////////////////

            /// <summary>
            /// This method does nothing.  Its signature matches the
            /// <see cref="ParameterizedThreadStart" /> delegate type.
            /// </summary>
            /// <param name="obj">
            /// The data passed to the thread; it is not used.
            /// </param>
            /* System.Threading.ParameterizedThreadStart */
            public void NullParameterizedThreadStart(
                object obj
                )
            {
                // do nothing.
            }

            ///////////////////////////////////////////////////////////////////////////////////

            /// <summary>
            /// This method does nothing.  Its signature matches the
            /// <see cref="WaitCallback" /> delegate type.
            /// </summary>
            /// <param name="state">
            /// The data passed to the callback; it is not used.
            /// </param>
            /* System.Threading.WaitCallback */
            public void NullWaitCallback(
                object state
                )
            {
                // do nothing.
            }

            ///////////////////////////////////////////////////////////////////////////////////

            /// <summary>
            /// This method does nothing.  Its signature matches the
            /// <see cref="GenericCallback" /> delegate type.
            /// </summary>
            /* Eagle._Components.Public.Delegates.GenericCallback */
            public void NullGenericCallback()
            {
                // do nothing.
            }

            ///////////////////////////////////////////////////////////////////////////////////

            /// <summary>
            /// This method does nothing and always returns null.  Its signature
            /// matches the <see cref="DynamicInvokeCallback" /> delegate type.
            /// </summary>
            /// <param name="args">
            /// The arguments passed to the callback; they are not used.
            /// </param>
            /// <returns>
            /// Always returns null.
            /// </returns>
            /* System.Delegate.DynamicInvoke */
            public object NullDynamicInvokeCallback(
                params object[] args
                )
            {
                return null;
            }
            #endregion
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////////////////////

        #region Delegate Type Checking Methods
        /// <summary>
        /// This method determines whether the specified type is the
        /// <see cref="Delegate" /> type itself.
        /// </summary>
        /// <param name="type">
        /// The type to check.
        /// </param>
        /// <returns>
        /// True if the type is the <see cref="Delegate" /> type itself;
        /// otherwise, false.
        /// </returns>
        public static bool IsDelegate(Type type)
        {
            //
            // NOTE: Must simply be the actual "System.Delegate" type.
            //
            return IsDelegateType(type, true);
        }

        ///////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method determines whether the specified type is exactly the
        /// <see cref="AsyncCallback" /> delegate type.
        /// </summary>
        /// <param name="type">
        /// The type to check.
        /// </param>
        /// <returns>
        /// True if the type is the <see cref="AsyncCallback" /> delegate type;
        /// otherwise, false.
        /// </returns>
        private static bool IsAsyncCallback(Type type)
        {
            return type == typeof(AsyncCallback);
        }

        ///////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method determines whether the specified type is the
        /// <see cref="AsyncCallback" /> delegate type or another delegate type
        /// with a compatible method signature.
        /// </summary>
        /// <param name="type">
        /// The type to check.
        /// </param>
        /// <returns>
        /// True if the type is signature-compatible with the
        /// <see cref="AsyncCallback" /> delegate type; otherwise, false.
        /// </returns>
        public static bool LooksLikeAsyncCallback(Type type)
        {
            if (IsAsyncCallback(type))
                return true;

            if (IsDelegateType(type, false))
            {
                try
                {
                    //
                    // NOTE: We need an instance of the delegate so that we can
                    //       get the method signature (i.e. MethodInfo) for it.
                    //
                    DelegateMethods delegateMethods = new DelegateMethods();

                    AsyncCallback asyncCallback = new AsyncCallback(
                        delegateMethods.NullAsyncCallback);

                    //
                    // NOTE: Attempt to create delegate with a compatible method
                    //       signature.
                    //
                    Delegate @delegate = Delegate.CreateDelegate(type, null,
                        asyncCallback.Method, false);

                    if (@delegate != null)
                        return true;
                }
                catch
                {
                    // do nothing.
                }
            }

            return false;
        }

        ///////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method determines whether the specified type is exactly the
        /// <see cref="EventHandler" /> delegate type.
        /// </summary>
        /// <param name="type">
        /// The type to check.
        /// </param>
        /// <returns>
        /// True if the type is the <see cref="EventHandler" /> delegate type;
        /// otherwise, false.
        /// </returns>
        private static bool IsEventHandler(Type type)
        {
            return type == typeof(EventHandler);
        }

        ///////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method determines whether the specified type is the
        /// <see cref="EventHandler" /> delegate type or another delegate type
        /// with a compatible method signature.
        /// </summary>
        /// <param name="type">
        /// The type to check.
        /// </param>
        /// <returns>
        /// True if the type is signature-compatible with the
        /// <see cref="EventHandler" /> delegate type; otherwise, false.
        /// </returns>
        public static bool LooksLikeEventHandler(Type type)
        {
            if (IsEventHandler(type))
                return true;

            if (IsDelegateType(type, false))
            {
                try
                {
                    //
                    // NOTE: We need an instance of the delegate so that we can
                    //       get the method signature (i.e. MethodInfo) for it.
                    //
                    DelegateMethods delegateMethods = new DelegateMethods();

                    EventHandler eventHandler = new EventHandler(
                        delegateMethods.NullEventHandler);

                    //
                    // NOTE: Attempt to create delegate with a compatible method
                    //       signature.
                    //
                    Delegate @delegate = Delegate.CreateDelegate(type, null,
                        eventHandler.Method, false);

                    if (@delegate != null)
                        return true;
                }
                catch
                {
                    // do nothing.
                }
            }

            return false;
        }

        ///////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method determines whether the specified type is exactly the
        /// <see cref="ThreadStart" /> delegate type.
        /// </summary>
        /// <param name="type">
        /// The type to check.
        /// </param>
        /// <returns>
        /// True if the type is the <see cref="ThreadStart" /> delegate type;
        /// otherwise, false.
        /// </returns>
        public static bool IsThreadStart(Type type)
        {
            return type == typeof(ThreadStart);
        }

        ///////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method determines whether the specified type is the
        /// <see cref="ThreadStart" /> delegate type or another delegate type
        /// with a compatible method signature.
        /// </summary>
        /// <param name="type">
        /// The type to check.
        /// </param>
        /// <returns>
        /// True if the type is signature-compatible with the
        /// <see cref="ThreadStart" /> delegate type; otherwise, false.
        /// </returns>
        public static bool LooksLikeThreadStart(Type type)
        {
            if (IsThreadStart(type))
                return true;

            if (IsDelegateType(type, false))
            {
                try
                {
                    //
                    // NOTE: We need an instance of the delegate so that we can
                    //       get the method signature (i.e. MethodInfo) for it.
                    //
                    DelegateMethods delegateMethods = new DelegateMethods();

                    ThreadStart threadStart = new ThreadStart(
                        delegateMethods.NullThreadStart);

                    //
                    // NOTE: Attempt to create delegate with a compatible method
                    //       signature.
                    //
                    Delegate @delegate = Delegate.CreateDelegate(type, null,
                        threadStart.Method, false);

                    if (@delegate != null)
                        return true;
                }
                catch
                {
                    // do nothing.
                }
            }

            return false;
        }

        ///////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method determines whether the specified type is exactly the
        /// <see cref="ParameterizedThreadStart" /> delegate type.
        /// </summary>
        /// <param name="type">
        /// The type to check.
        /// </param>
        /// <returns>
        /// True if the type is the <see cref="ParameterizedThreadStart" />
        /// delegate type; otherwise, false.
        /// </returns>
        public static bool IsParameterizedThreadStart(Type type)
        {
            return type == typeof(ParameterizedThreadStart);
        }

        ///////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method determines whether the specified type is the
        /// <see cref="ParameterizedThreadStart" /> delegate type or another
        /// delegate type with a compatible method signature.
        /// </summary>
        /// <param name="type">
        /// The type to check.
        /// </param>
        /// <returns>
        /// True if the type is signature-compatible with the
        /// <see cref="ParameterizedThreadStart" /> delegate type; otherwise,
        /// false.
        /// </returns>
        public static bool LooksLikeParameterizedThreadStart(Type type)
        {
            if (IsParameterizedThreadStart(type))
                return true;

            if (IsDelegateType(type, false))
            {
                try
                {
                    //
                    // NOTE: We need an instance of the delegate so that we can
                    //       get the method signature (i.e. MethodInfo) for it.
                    //
                    DelegateMethods delegateMethods = new DelegateMethods();

                    ParameterizedThreadStart parameterizedThreadStart =
                        new ParameterizedThreadStart(
                            delegateMethods.NullParameterizedThreadStart);

                    //
                    // NOTE: Attempt to create delegate with a compatible method
                    //       signature.
                    //
                    Delegate @delegate = Delegate.CreateDelegate(type, null,
                        parameterizedThreadStart.Method, false);

                    if (@delegate != null)
                        return true;
                }
                catch
                {
                    // do nothing.
                }
            }

            return false;
        }

        ///////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method determines whether the specified type is exactly the
        /// <see cref="WaitCallback" /> delegate type.
        /// </summary>
        /// <param name="type">
        /// The type to check.
        /// </param>
        /// <returns>
        /// True if the type is the <see cref="WaitCallback" /> delegate type;
        /// otherwise, false.
        /// </returns>
        public static bool IsWaitCallback(Type type)
        {
            return type == typeof(WaitCallback);
        }

        ///////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method determines whether the specified type is the
        /// <see cref="WaitCallback" /> delegate type or another delegate type
        /// with a compatible method signature.
        /// </summary>
        /// <param name="type">
        /// The type to check.
        /// </param>
        /// <returns>
        /// True if the type is signature-compatible with the
        /// <see cref="WaitCallback" /> delegate type; otherwise, false.
        /// </returns>
        public static bool LooksLikeWaitCallback(Type type)
        {
            if (IsWaitCallback(type))
                return true;

            if (IsDelegateType(type, false))
            {
                try
                {
                    //
                    // NOTE: We need an instance of the delegate so that we can
                    //       get the method signature (i.e. MethodInfo) for it.
                    //
                    DelegateMethods delegateMethods = new DelegateMethods();

                    WaitCallback waitCallback = new WaitCallback(
                        delegateMethods.NullWaitCallback);

                    //
                    // NOTE: Attempt to create delegate with a compatible method
                    //       signature.
                    //
                    Delegate @delegate = Delegate.CreateDelegate(type, null,
                        waitCallback.Method, false);

                    if (@delegate != null)
                        return true;
                }
                catch
                {
                    // do nothing.
                }
            }

            return false;
        }

        ///////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method determines whether the specified type is exactly the
        /// <see cref="GenericCallback" /> delegate type.
        /// </summary>
        /// <param name="type">
        /// The type to check.
        /// </param>
        /// <returns>
        /// True if the type is the <see cref="GenericCallback" /> delegate
        /// type; otherwise, false.
        /// </returns>
        private static bool IsGenericCallback(Type type)
        {
            return type == typeof(GenericCallback);
        }

        ///////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method determines whether the specified type is the
        /// <see cref="GenericCallback" /> delegate type or another delegate
        /// type with a compatible method signature.
        /// </summary>
        /// <param name="type">
        /// The type to check.
        /// </param>
        /// <returns>
        /// True if the type is signature-compatible with the
        /// <see cref="GenericCallback" /> delegate type; otherwise, false.
        /// </returns>
        public static bool LooksLikeGenericCallback(Type type)
        {
            if (IsGenericCallback(type))
                return true;

            if (IsDelegateType(type, false))
            {
                try
                {
                    //
                    // NOTE: We need an instance of the delegate so that we can
                    //       get the method signature (i.e. MethodInfo) for it.
                    //
                    DelegateMethods delegateMethods = new DelegateMethods();

                    GenericCallback genericCallback = new GenericCallback(
                        delegateMethods.NullGenericCallback);

                    //
                    // NOTE: Attempt to create delegate with a compatible method
                    //       signature.
                    //
                    Delegate @delegate = Delegate.CreateDelegate(type, null,
                        genericCallback.Method, false);

                    if (@delegate != null)
                        return true;
                }
                catch
                {
                    // do nothing.
                }
            }

            return false;
        }

        ///////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method determines whether the specified type is exactly the
        /// <see cref="DynamicInvokeCallback" /> delegate type.
        /// </summary>
        /// <param name="type">
        /// The type to check.
        /// </param>
        /// <returns>
        /// True if the type is the <see cref="DynamicInvokeCallback" /> delegate
        /// type; otherwise, false.
        /// </returns>
        private static bool IsDynamicInvokeCallback(Type type)
        {
            return type == typeof(DynamicInvokeCallback);
        }

        ///////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method determines whether the specified type is the
        /// <see cref="DynamicInvokeCallback" /> delegate type or another
        /// delegate type with a compatible method signature.
        /// </summary>
        /// <param name="type">
        /// The type to check.
        /// </param>
        /// <returns>
        /// True if the type is signature-compatible with the
        /// <see cref="DynamicInvokeCallback" /> delegate type; otherwise,
        /// false.
        /// </returns>
        public static bool LooksLikeDynamicInvokeCallback(Type type)
        {
            if (IsDynamicInvokeCallback(type))
                return true;

            if (IsDelegateType(type, false))
            {
                try
                {
                    //
                    // NOTE: We need an instance of the delegate so that we can
                    //       get the method signature (i.e. MethodInfo) for it.
                    //
                    DelegateMethods delegateMethods = new DelegateMethods();

                    DynamicInvokeCallback dynamicInvokeCallback =
                        new DynamicInvokeCallback(
                            delegateMethods.NullDynamicInvokeCallback);

                    //
                    // NOTE: Attempt to create delegate with a compatible method
                    //       signature.
                    //
                    Delegate @delegate = Delegate.CreateDelegate(type, null,
                        dynamicInvokeCallback.Method, false);

                    if (@delegate != null)
                        return true;
                }
                catch
                {
                    // do nothing.
                }
            }

            return false;
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method combines the low-order 32 bits of two long integer
        /// values into a single long integer value.
        /// </summary>
        /// <param name="X">
        /// The value to use for the high-order 32 bits of the result.
        /// </param>
        /// <param name="Y">
        /// The value to use for the low-order 32 bits of the result.
        /// </param>
        /// <returns>
        /// The combined long integer value.
        /// </returns>
        public static long MakeLong(long X, long Y) /* LOSSY */
        {
            return ((X & LongLowInt) << IntBits) | (Y & LongLowInt);
        }

        ///////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method splits a long integer value into its high-order and
        /// low-order 32-bit halves.
        /// </summary>
        /// <param name="Z">
        /// The long integer value to split.
        /// </param>
        /// <param name="X">
        /// Upon return, this will contain the high-order 32 bits of the value.
        /// </param>
        /// <param name="Y">
        /// Upon return, this will contain the low-order 32 bits of the value.
        /// </param>
        public static void UnmakeLong(
            long Z,
            out long X,
            out long Y
            ) /* SAFE */
        {
            X = (((Z >> IntBits) & LongLowInt) & uint.MaxValue);
            Y = ((Z & LongLowInt) & uint.MaxValue);
        }

        ///////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method splits a long integer value into its four constituent
        /// 16-bit parts.
        /// </summary>
        /// <param name="V">
        /// The long integer value to split.
        /// </param>
        /// <param name="W">
        /// Upon return, this will contain the highest-order 16 bits of the
        /// value.
        /// </param>
        /// <param name="X">
        /// Upon return, this will contain the upper-middle 16 bits of the
        /// value.
        /// </param>
        /// <param name="Y">
        /// Upon return, this will contain the lower-middle 16 bits of the
        /// value.
        /// </param>
        /// <param name="Z">
        /// Upon return, this will contain the lowest-order 16 bits of the
        /// value.
        /// </param>
        public static void UnmakeLong(
            long V,
            ref long W,
            ref long X,
            ref long Y,
            ref long Z
            ) /* SAFE */
        {
            W = unchecked((long)(((ulong)V & (ulong)LongHighShort) >> SixByteBits));
            X = unchecked((V & LongHighMidShort) >> FourByteBits);
            Y = unchecked((V & LongLowMidShort) >> TwoByteBits);
            Z = unchecked(V & LongLowShort);
        }

        ///////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method reverses the byte order (endianness) of the specified
        /// integer value.
        /// </summary>
        /// <param name="X">
        /// The value whose byte order will be reversed.
        /// </param>
        /// <returns>
        /// The value with its bytes in reversed order.
        /// </returns>
        public static int FlipEndian(int X) /* SAFE */
        {
            byte[] bytes = BitConverter.GetBytes(X);

            Array.Reverse(bytes);

            return BitConverter.ToInt32(bytes, 0);
        }

        ///////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method reverses the byte order (endianness) of the specified
        /// unsigned integer value.
        /// </summary>
        /// <param name="X">
        /// The value whose byte order will be reversed.
        /// </param>
        /// <returns>
        /// The value with its bytes in reversed order.
        /// </returns>
        public static uint FlipEndian(uint X) /* SAFE */
        {
            byte[] bytes = BitConverter.GetBytes(X);

            Array.Reverse(bytes);

            return BitConverter.ToUInt32(bytes, 0);
        }

        ///////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method reverses the byte order (endianness) of the specified
        /// long integer value.
        /// </summary>
        /// <param name="X">
        /// The value whose byte order will be reversed.
        /// </param>
        /// <returns>
        /// The value with its bytes in reversed order.
        /// </returns>
        public static long FlipEndian(long X) /* SAFE */
        {
            byte[] bytes = BitConverter.GetBytes(X);

            Array.Reverse(bytes);

            return BitConverter.ToInt64(bytes, 0);
        }

        ///////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method reverses the byte order (endianness) of the specified
        /// unsigned long integer value.
        /// </summary>
        /// <param name="X">
        /// The value whose byte order will be reversed.
        /// </param>
        /// <returns>
        /// The value with its bytes in reversed order.
        /// </returns>
        public static ulong FlipEndian(ulong X) /* SAFE */
        {
            byte[] bytes = BitConverter.GetBytes(X);

            Array.Reverse(bytes);

            return BitConverter.ToUInt64(bytes, 0);
        }

        ///////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method computes the arithmetic negation of the specified
        /// unsigned integer value, wrapping around on overflow.
        /// </summary>
        /// <param name="X">
        /// The value to negate.
        /// </param>
        /// <returns>
        /// The negated value.
        /// </returns>
        public static uint Negate(uint X) /* SAFE */
        {
            return unchecked((uint)(-(int)X));
        }

        ///////////////////////////////////////////////////////////////////////////////////////

        #region Dead Code
#if DEAD_CODE
        /// <summary>
        /// This method computes the arithmetic negation of the specified
        /// unsigned byte value.
        /// </summary>
        /// <param name="X">
        /// The value to negate.
        /// </param>
        /// <returns>
        /// The resulting unsigned byte value.
        /// </returns>
        private static byte Negate(byte X) /* SAFE */
        {
            return unchecked((byte)(-(sbyte)X));
        }

        ///////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method computes the arithmetic negation of the specified
        /// unsigned short value.
        /// </summary>
        /// <param name="X">
        /// The value to negate.
        /// </param>
        /// <returns>
        /// The resulting unsigned short value.
        /// </returns>
        private static ushort Negate(ushort X) /* SAFE */
        {
            return unchecked((ushort)(-(short)X));
        }

        ///////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method computes the arithmetic negation of the specified
        /// unsigned long integer value.
        /// </summary>
        /// <param name="X">
        /// The value to negate.
        /// </param>
        /// <returns>
        /// The resulting unsigned long integer value.
        /// </returns>
        private static ulong Negate(ulong X) /* SAFE */
        {
            return unchecked((ulong)(-(long)X));
        }
#endif
        #endregion

        ///////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method creates a copy of the specified array of bytes with its
        /// elements in reversed order.
        /// </summary>
        /// <param name="bytes">
        /// The array of bytes to reverse.  This may be null.
        /// </param>
        /// <returns>
        /// A new array containing the bytes in reversed order, or null if the
        /// specified array was null.
        /// </returns>
        public static byte[] Reverse(
            byte[] bytes /* in */
            )
        {
            if (bytes != null)
            {
                byte[] reverseBytes;
                int length = bytes.Length;

                reverseBytes = new byte[length];

                Array.Copy(bytes, reverseBytes, length);
                Array.Reverse(reverseBytes);

                return reverseBytes;
            }
            else
            {
                return bytes;
            }
        }

        ///////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method converts the specified tri-state boolean value to a
        /// native boolean value.
        /// </summary>
        /// <param name="X">
        /// The value to convert.
        /// </param>
        /// <returns>
        /// True if the value is not <see cref="_Public.Boolean.False" />;
        /// otherwise, false.
        /// </returns>
        public static bool ToBool(_Public.Boolean X) /* SAFE */
        {
            return (X != _Public.Boolean.False) ? true : false;
        }

        ///////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method converts the specified integer value to a boolean value.
        /// </summary>
        /// <param name="X">
        /// The value to convert.
        /// </param>
        /// <returns>
        /// True if the value is non-zero; otherwise, false.
        /// </returns>
        public static bool ToBool(int X) /* LOSSY */
        {
            return X != 0 ? true : false;
        }

        ///////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method converts the specified unsigned integer value to a
        /// boolean value.
        /// </summary>
        /// <param name="X">
        /// The value to convert.
        /// </param>
        /// <returns>
        /// True if the value is non-zero; otherwise, false.
        /// </returns>
        public static bool ToBool(uint X) /* LOSSY */
        {
            return X != 0 ? true : false;
        }

        ///////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method converts the specified long integer value to a boolean
        /// value.
        /// </summary>
        /// <param name="X">
        /// The value to convert.
        /// </param>
        /// <returns>
        /// True if the value is non-zero; otherwise, false.
        /// </returns>
        public static bool ToBool(long X) /* LOSSY */
        {
            return X != 0 ? true : false;
        }

        ///////////////////////////////////////////////////////////////////////////////////////

#if NET_40
        /// <summary>
        /// This method converts the specified arbitrary-precision integer value
        /// to a boolean value.
        /// </summary>
        /// <param name="X">
        /// The value to convert.
        /// </param>
        /// <returns>
        /// True if the value is non-zero; otherwise, false.
        /// </returns>
        public static bool ToBool(BigInteger X) /* LOSSY */
        {
            return X != 0 ? true : false;
        }
#endif

        ///////////////////////////////////////////////////////////////////////////////////////

        #region Dead Code
#if DEAD_CODE
        /// <summary>
        /// This method converts the specified return code value to a boolean
        /// value.
        /// </summary>
        /// <param name="X">
        /// The value to convert.
        /// </param>
        /// <returns>
        /// True if the value is <see cref="ReturnCode.Ok" />; otherwise, false.
        /// </returns>
        private static bool ToBool(ReturnCode X) /* LOSSY */
        {
            return X == ReturnCode.Ok ? true : false;
        }
#endif
        #endregion

        ///////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method converts the specified date/time value to a boolean
        /// value.
        /// </summary>
        /// <param name="X">
        /// The value to convert.
        /// </param>
        /// <returns>
        /// True if the value has a non-zero tick count; otherwise, false.
        /// </returns>
        public static bool ToBool(DateTime X) /* LOSSY */
        {
            return X.Ticks != 0 ? true : false;
        }

        ///////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method converts the specified decimal value to a boolean value.
        /// </summary>
        /// <param name="X">
        /// The value to convert.
        /// </param>
        /// <returns>
        /// True if the value is non-zero; otherwise, false.
        /// </returns>
        public static bool ToBool(decimal X) /* LOSSY */
        {
            return X != 0 ? true : false;
        }

        ///////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method converts the specified double value to a boolean value.
        /// </summary>
        /// <param name="X">
        /// The value to convert.
        /// </param>
        /// <returns>
        /// True if the value is non-zero; otherwise, false.
        /// </returns>
        public static bool ToBool(double X) /* LOSSY */
        {
            return X != 0 ? true : false;
        }

        ///////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method converts the specified object to a boolean value based
        /// on its underlying runtime type.
        /// </summary>
        /// <param name="X">
        /// The object to convert.  It must be one of the supported primitive,
        /// enumerated, or floating-point types.
        /// </param>
        /// <returns>
        /// True if the underlying value is considered non-zero (or true);
        /// otherwise, false.
        /// </returns>
        public static bool ToBool(object X) /* LOSSY */
        {
            if (X is bool)
                return (bool)X;

            if (X is sbyte)
                return (sbyte)X != 0 ? true : false;

            if (X is byte)
                return (byte)X != 0 ? true : false;

            if (X is short)
                return (short)X != 0 ? true : false;

            if (X is ushort)
                return (ushort)X != 0 ? true : false;

            if (X is char)
                return (char)X != 0 ? true : false;

            if (X is int)
                return (int)X != 0 ? true : false;

            if (X is uint)
                return (uint)X != 0 ? true : false;

            if (X is long)
                return (long)X != 0 ? true : false;

            if (X is ulong)
                return (ulong)X != 0 ? true : false;

#if NET_40
            if (X is BigInteger)
                return (BigInteger)X != 0 ? true : false;
#endif

            if (X is Enum)
                return EnumOps.ToLong((Enum)X) != 0 ? true : false;

            if (X is decimal)
                return (decimal)X != Decimal.Zero ? true : false;

            if (X is float)
                return (float)X != 0.0f ? true : false;

            if (X is double)
                return (double)X != 0.0 ? true : false;

            throw new ScriptException(String.Format(
                "conversion to \"{0}\" failed, unsupported type \"{1}\"",
                typeof(bool), (X != null) ? X.GetType() : typeof(object)));
        }

        ///////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method reinterprets the specified byte value as a signed byte
        /// value.
        /// </summary>
        /// <param name="X">
        /// The value to convert.
        /// </param>
        /// <returns>
        /// The resulting signed byte value.
        /// </returns>
        public static sbyte ToSByte(byte X) /* SAFE */
        {
            return unchecked((sbyte)X);
        }

        ///////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method converts the specified integer value to a signed byte
        /// value by retaining only its low-order byte.
        /// </summary>
        /// <param name="X">
        /// The value to convert.
        /// </param>
        /// <returns>
        /// The resulting signed byte value.
        /// </returns>
        public static sbyte ToSByte(int X) /* LOSSY */
        {
            return unchecked((sbyte)(X & byte.MaxValue));
        }

        ///////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method converts the specified long integer value to a signed
        /// byte value by retaining only its low-order byte.
        /// </summary>
        /// <param name="X">
        /// The value to convert.
        /// </param>
        /// <returns>
        /// The resulting signed byte value.
        /// </returns>
        public static sbyte ToSByte(long X) /* LOSSY */
        {
            return unchecked((sbyte)(X & byte.MaxValue));
        }

        ///////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method converts the specified character value to a byte value
        /// by retaining only its low-order byte.
        /// </summary>
        /// <param name="X">
        /// The value to convert.
        /// </param>
        /// <returns>
        /// The resulting byte value.
        /// </returns>
        public static byte ToByte(char X) /* LOSSY */
        {
            return ToLowByte(X);
        }

        ///////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method extracts the low-order byte of the specified character
        /// value.
        /// </summary>
        /// <param name="X">
        /// The value whose low-order byte will be extracted.
        /// </param>
        /// <returns>
        /// The low-order byte of the value.
        /// </returns>
        public static byte ToLowByte(char X) /* LOSSY */
        {
            return (byte)(X & CharLowByte);
        }

        ///////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method extracts the high-order byte of the specified character
        /// value.
        /// </summary>
        /// <param name="X">
        /// The value whose high-order byte will be extracted.
        /// </param>
        /// <returns>
        /// The high-order byte of the value.
        /// </returns>
        public static byte ToHighByte(char X) /* LOSSY */
        {
            return (byte)((X & CharHighByte) >> ByteBits);
        }

        ///////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method reinterprets the specified signed byte value as a byte
        /// value.
        /// </summary>
        /// <param name="X">
        /// The value to convert.
        /// </param>
        /// <returns>
        /// The resulting byte value.
        /// </returns>
        public static byte ToByte(sbyte X) /* SAFE */
        {
            return unchecked((byte)X);
        }

        ///////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method converts the specified integer value to a byte value by
        /// retaining only its low-order byte.
        /// </summary>
        /// <param name="X">
        /// The value to convert.
        /// </param>
        /// <returns>
        /// The resulting byte value.
        /// </returns>
        public static byte ToByte(int X) /* LOSSY */
        {
            return (byte)(X & byte.MaxValue);
        }

        ///////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method converts the specified long integer value to a byte
        /// value by retaining only its low-order byte.
        /// </summary>
        /// <param name="X">
        /// The value to convert.
        /// </param>
        /// <returns>
        /// The resulting byte value.
        /// </returns>
        public static byte ToByte(long X) /* LOSSY */
        {
            return (byte)(X & byte.MaxValue);
        }

        ///////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method converts the specified byte value to a character value.
        /// </summary>
        /// <param name="X">
        /// The value to convert.
        /// </param>
        /// <returns>
        /// The resulting character value.
        /// </returns>
        public static char ToChar(byte X) /* SAFE */
        {
            return (char)X;
        }

        ///////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method combines the specified low-order and high-order byte
        /// values into a single character value (using little-endian byte
        /// order).
        /// </summary>
        /// <param name="X">
        /// The value to use for the low-order byte of the result.
        /// </param>
        /// <param name="Y">
        /// The value to use for the high-order byte of the result.
        /// </param>
        /// <returns>
        /// The resulting character value.
        /// </returns>
        public static char ToChar(byte X, byte Y) /* SAFE, LITTLE-ENDIAN */
        {
            return (char)(X | (Y << ByteBits));
        }

        ///////////////////////////////////////////////////////////////////////////////////////

        #region Dead Code
#if DEAD_CODE
        /// <summary>
        /// This method converts the specified unsigned short value to a
        /// character value.
        /// </summary>
        /// <param name="X">
        /// The value to convert.
        /// </param>
        /// <returns>
        /// The resulting character value.
        /// </returns>
        public static char ToChar(ushort X) /* SAFE */
        {
            return (char)(X & char.MaxValue);
        }
#endif
        #endregion

        ///////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method converts the specified integer value to a character
        /// value by retaining only its low-order 16 bits.
        /// </summary>
        /// <param name="X">
        /// The value to convert.
        /// </param>
        /// <returns>
        /// The resulting character value.
        /// </returns>
        public static char ToChar(int X) /* LOSSY */
        {
            return (char)(X & char.MaxValue);
        }

        ///////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method converts the specified long integer value to a character
        /// value by retaining only its low-order 16 bits.
        /// </summary>
        /// <param name="X">
        /// The value to convert.
        /// </param>
        /// <returns>
        /// The resulting character value.
        /// </returns>
        public static char ToChar(long X) /* LOSSY */
        {
            return (char)(X & char.MaxValue);
        }

        ///////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method splits the specified integer value into two character
        /// values, honoring the byte order of the current platform.
        /// </summary>
        /// <param name="X">
        /// The value to split.
        /// </param>
        /// <param name="Y">
        /// Upon return, this will contain the first character value.
        /// </param>
        /// <param name="Z">
        /// Upon return, this will contain the second character value.
        /// </param>
        private static void ToChars(int X, ref char? Y, ref char? Z) /* SAFE */
        {
            if (BitConverter.IsLittleEndian)
            {
                Y = (char)(X & IntLowShort);
                Z = (char)((X & IntHighShort) >> ShortBits);
            }
            else
            {
                Y = (char)((X & IntHighShort) >> ShortBits);
                Z = (char)(X & IntLowShort);
            }
        }

        ///////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method splits the specified long integer value into two
        /// character values, honoring the byte order of the current platform.
        /// </summary>
        /// <param name="X">
        /// The value to split.
        /// </param>
        /// <param name="Y">
        /// Upon return, this will contain the first character value.
        /// </param>
        /// <param name="Z">
        /// Upon return, this will contain the second character value.
        /// </param>
        /* NOTE: For use by the Parser.ParseBackslash method only. */
        public static void ToChars(long X, ref char? Y, ref char? Z) /* LOSSY */
        {
            ToChars(ToInt(X), ref Y, ref Z);
        }

        ///////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method converts the specified long integer value to a short
        /// value by retaining only its low-order 16 bits.
        /// </summary>
        /// <param name="X">
        /// The value to convert.
        /// </param>
        /// <returns>
        /// The resulting short value.
        /// </returns>
        public static short ToShort(long X) /* LOSSY */
        {
            return unchecked((short)(X & ushort.MaxValue));
        }

        ///////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method reinterprets the specified unsigned short value as a
        /// short value.
        /// </summary>
        /// <param name="X">
        /// The value to convert.
        /// </param>
        /// <returns>
        /// The resulting short value.
        /// </returns>
        private static short ToShort(ushort X) /* LOSSY */
        {
            return unchecked((short)X);
        }

        ///////////////////////////////////////////////////////////////////////////////////////

        #region Dead Code
#if DEAD_CODE
        /// <summary>
        /// This method reinterprets the specified short value as an unsigned
        /// short value.
        /// </summary>
        /// <param name="X">
        /// The value to convert.
        /// </param>
        /// <returns>
        /// The resulting unsigned short value.
        /// </returns>
        private static ushort ToUShort(short X) /* SAFE */
        {
            return unchecked((ushort)X);
        }
#endif
        #endregion

        ///////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method converts the specified long integer value to an unsigned
        /// short value by retaining only its low-order 16 bits.
        /// </summary>
        /// <param name="X">
        /// The value to convert.
        /// </param>
        /// <returns>
        /// The resulting unsigned short value.
        /// </returns>
        public static ushort ToUShort(long X) /* LOSSY */
        {
            return unchecked((ushort)(X & ushort.MaxValue));
        }

        ///////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method converts the specified boolean value to an integer
        /// value.
        /// </summary>
        /// <param name="X">
        /// The value to convert.
        /// </param>
        /// <returns>
        /// One if the value is true; otherwise, zero.
        /// </returns>
        public static int ToInt(bool X) /* SAFE */
        {
            return X ? 1 : 0;
        }

        ///////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method converts the specified character value to an integer
        /// value.
        /// </summary>
        /// <param name="X">
        /// The value to convert.
        /// </param>
        /// <returns>
        /// The resulting integer value.
        /// </returns>
        public static int ToInt(char X) /* SAFE */
        {
            return X;
        }

        ///////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method converts the specified <see cref="ReturnCode" /> value to
        /// an integer value.
        /// </summary>
        /// <param name="X">
        /// The value to convert.
        /// </param>
        /// <returns>
        /// The resulting integer value.
        /// </returns>
        public static int ToInt(ReturnCode X) /* SAFE */
        {
            return (int)X;
        }

        ///////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method reinterprets the specified unsigned integer value as an
        /// integer value.
        /// </summary>
        /// <param name="X">
        /// The value to convert.
        /// </param>
        /// <returns>
        /// The resulting integer value.
        /// </returns>
        public static int ToInt(uint X) /* SAFE */
        {
            return unchecked((int)X);
        }

        ///////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method converts the specified long integer value to an integer
        /// value by retaining only its low-order 32 bits.
        /// </summary>
        /// <param name="X">
        /// The value to convert.
        /// </param>
        /// <returns>
        /// The resulting integer value.
        /// </returns>
        public static int ToInt(long X) /* LOSSY */
        {
            return (int)(X & uint.MaxValue);
        }

        ///////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method converts the specified unsigned long integer value to an
        /// integer value by retaining only its low-order 32 bits.
        /// </summary>
        /// <param name="X">
        /// The value to convert.
        /// </param>
        /// <returns>
        /// The resulting integer value.
        /// </returns>
        public static int ToInt(ulong X) /* LOSSY */
        {
            return (int)(X & uint.MaxValue);
        }

        ///////////////////////////////////////////////////////////////////////////////////////

#if NET_40
        /// <summary>
        /// This method converts the specified arbitrary-precision integer value
        /// to an integer value.
        /// </summary>
        /// <param name="X">
        /// The value to convert.
        /// </param>
        /// <returns>
        /// The resulting integer value.
        /// </returns>
        public static int ToInt(BigInteger X) /* SAFE */
        {
            return (int)X;
        }
#endif

        ///////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method converts the specified pointer-sized integer value to an
        /// integer value by retaining only its low-order 32 bits.
        /// </summary>
        /// <param name="X">
        /// The value to convert.
        /// </param>
        /// <returns>
        /// The resulting integer value.
        /// </returns>
        public static int ToInt(IntPtr X) /* LOSSY */
        {
            return ToInt(X.ToInt64());
        }

        ///////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method converts the specified unsigned pointer-sized integer
        /// value to an integer value by retaining only its low-order 32 bits.
        /// </summary>
        /// <param name="X">
        /// The value to convert.
        /// </param>
        /// <returns>
        /// The resulting integer value.
        /// </returns>
        public static int ToInt(UIntPtr X) /* LOSSY */
        {
            return ToInt(X.ToUInt64());
        }

        ///////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method converts the specified date/time value to an integer
        /// value by retaining only the low-order 32 bits of its tick count.
        /// </summary>
        /// <param name="X">
        /// The value to convert.
        /// </param>
        /// <returns>
        /// The resulting integer value.
        /// </returns>
        public static int ToInt(DateTime X) /* LOSSY */
        {
            return (int)(X.Ticks & uint.MaxValue);
        }

        ///////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method splits the specified long integer value into its
        /// low-order and high-order 32-bit halves.
        /// </summary>
        /// <param name="X">
        /// The value to split.
        /// </param>
        /// <param name="Y">
        /// Upon return, this will contain the low-order 32 bits of the value.
        /// </param>
        /// <param name="Z">
        /// Upon return, this will contain the high-order 32 bits of the value.
        /// </param>
        public static void ToInts(long X, ref int Y, ref int Z) /* SAFE */
        {
            Y = (int)(X & LongLowInt);
            Z = (int)((X & LongHighInt) >> IntBits);
        }

        ///////////////////////////////////////////////////////////////////////////////////////

#if NATIVE && WINDOWS
        /// <summary>
        /// This method copies the elements of the specified array of unsigned
        /// long integer values into an array of unsigned integer values,
        /// converting each element (and allocating the destination array when
        /// necessary).
        /// </summary>
        /// <param name="destination">
        /// The destination array of unsigned integer values.  When null, a new
        /// array large enough to hold the source elements will be allocated.
        /// </param>
        /// <param name="source">
        /// The source array of unsigned long integer values.  This may be null,
        /// in which case nothing is done.
        /// </param>
        public static void Copy(
            ref uint[] destination,
            ulong[] source
            ) /* LOSSY */
        {
            if (source == null)
                return;

            int sourceLength = source.Length;
            int destinationLength;

            if (destination != null)
            {
                destinationLength = destination.Length;
            }
            else
            {
                destinationLength = sourceLength;
                destination = new uint[destinationLength];
            }

            int length = Math.Min(
                sourceLength, destinationLength);

            for (int index = 0; index < length; index++)
                destination[index] = ToUInt(source[index]);
        }
#endif

        ///////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method converts the specified object to an unsigned integer
        /// value using its <see cref="IConvertible" /> implementation, if any.
        /// </summary>
        /// <param name="X">
        /// The object to convert.  This may be null.
        /// </param>
        /// <param name="cultureInfo">
        /// The culture-specific formatting information to use when performing
        /// the conversion.
        /// </param>
        /// <returns>
        /// The resulting unsigned integer value, or null if the object was null
        /// or did not implement <see cref="IConvertible" />.
        /// </returns>
        public static uint? ToUInt(
            object X,
            CultureInfo cultureInfo
            ) /* SAFE */
        {
            if (X == null)
                return null;

            IConvertible convertible = X as IConvertible;

            if (convertible == null)
                return null;

            return convertible.ToUInt32(cultureInfo);
        }

        ///////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method reinterprets the specified integer value as an unsigned
        /// integer value.
        /// </summary>
        /// <param name="X">
        /// The value to convert.
        /// </param>
        /// <returns>
        /// The resulting unsigned integer value.
        /// </returns>
        public static uint ToUInt(int X) /* SAFE */
        {
            return unchecked((uint)X);
        }

        ///////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method converts the specified long integer value to an unsigned
        /// integer value by retaining only its low-order 32 bits.
        /// </summary>
        /// <param name="X">
        /// The value to convert.
        /// </param>
        /// <returns>
        /// The resulting unsigned integer value.
        /// </returns>
        public static uint ToUInt(long X) /* LOSSY */
        {
            return (uint)(X & uint.MaxValue);
        }

        ///////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method converts the specified unsigned long integer value to an
        /// unsigned integer value by retaining only its low-order 32 bits.
        /// </summary>
        /// <param name="X">
        /// The value to convert.
        /// </param>
        /// <returns>
        /// The resulting unsigned integer value.
        /// </returns>
        public static uint ToUInt(ulong X) /* LOSSY */
        {
            return (uint)(X & uint.MaxValue);
        }

        ///////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method converts the specified pointer-sized integer value to an
        /// unsigned integer value by retaining only its low-order 32 bits.
        /// </summary>
        /// <param name="X">
        /// The value to convert.
        /// </param>
        /// <returns>
        /// The resulting unsigned integer value.
        /// </returns>
        public static uint ToUInt(IntPtr X) /* LOSSY */
        {
            return ToUInt(X.ToInt64());
        }

        ///////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method converts the specified unsigned pointer-sized integer
        /// value to an unsigned integer value by retaining only its low-order
        /// 32 bits.
        /// </summary>
        /// <param name="X">
        /// The value to convert.
        /// </param>
        /// <returns>
        /// The resulting unsigned integer value.
        /// </returns>
        public static uint ToUInt(UIntPtr X) /* LOSSY */
        {
            return ToUInt(X.ToUInt64());
        }

        ///////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method converts the specified boolean value to a long integer
        /// value.
        /// </summary>
        /// <param name="X">
        /// The value to convert.
        /// </param>
        /// <returns>
        /// One if the value is true; otherwise, zero.
        /// </returns>
        public static long ToLong(bool X) /* SAFE */
        {
            return X ? 1 : 0;
        }

        ///////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method converts the specified integer value to a long integer
        /// value.
        /// </summary>
        /// <param name="X">
        /// The value to convert.
        /// </param>
        /// <returns>
        /// The resulting long integer value.
        /// </returns>
        public static long ToLong(int X) /* SAFE */
        {
            return X;
        }

        ///////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method converts the specified date/time value to a long integer
        /// value containing its tick count.
        /// </summary>
        /// <param name="X">
        /// The value to convert.
        /// </param>
        /// <returns>
        /// The tick count of the value.
        /// </returns>
        public static long ToLong(DateTime X) /* SAFE */
        {
            return X.Ticks;
        }

        ///////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method reinterprets the specified unsigned long integer value as
        /// a long integer value.
        /// </summary>
        /// <param name="X">
        /// The value to convert.
        /// </param>
        /// <returns>
        /// The resulting long integer value.
        /// </returns>
        public static long ToLong(ulong X) /* SAFE */
        {
            return unchecked((long)X);
        }

        ///////////////////////////////////////////////////////////////////////////////////////

#if NET_40
        /// <summary>
        /// This method converts the specified arbitrary-precision integer value
        /// to a long integer value.
        /// </summary>
        /// <param name="X">
        /// The value to convert.
        /// </param>
        /// <returns>
        /// The resulting long integer value.
        /// </returns>
        public static long ToLong(BigInteger X) /* SAFE */
        {
            return (long)X;
        }
#endif

        ///////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method converts the specified double value to a long integer
        /// value by truncating its fractional part.
        /// </summary>
        /// <param name="X">
        /// The value to convert.
        /// </param>
        /// <returns>
        /// The resulting long integer value.
        /// </returns>
        public static long ToLong(double X) /* LOSSY */
        {
            return unchecked((long)X);
        }

        ///////////////////////////////////////////////////////////////////////////////////////

        #region Dead Code
#if DEAD_CODE
        /// <summary>
        /// This method reinterprets the bits of the specified double-precision
        /// floating-point value as a long integer value.
        /// </summary>
        /// <param name="X">
        /// The value to convert.
        /// </param>
        /// <returns>
        /// The resulting long integer value.
        /// </returns>
        private static long ToLongBits(double X) /* SAFE */
        {
            return BitConverter.DoubleToInt64Bits(X);
        }

        ///////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method converts the specified unsigned integer value to a
        /// pointer-sized integer value.
        /// </summary>
        /// <param name="X">
        /// The value to convert.
        /// </param>
        /// <returns>
        /// The resulting pointer-sized integer value.
        /// </returns>
        private static IntPtr ToIntPtr(uint X) /* SAFE */
        {
            return new IntPtr(unchecked((int)X));
        }

        ///////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method converts the specified unsigned long integer value to a
        /// pointer-sized integer value.
        /// </summary>
        /// <param name="X">
        /// The value to convert.
        /// </param>
        /// <returns>
        /// The resulting pointer-sized integer value.
        /// </returns>
        private static IntPtr ToIntPtr(ulong X) /* SAFE */
        {
            return new IntPtr(unchecked((long)X));
        }
#endif
        #endregion

        ///////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method reinterprets the specified unsigned pointer-sized integer
        /// value as a (signed) pointer-sized integer value.
        /// </summary>
        /// <param name="X">
        /// The value to convert.
        /// </param>
        /// <returns>
        /// The resulting pointer-sized integer value.
        /// </returns>
        public static IntPtr ToIntPtr(UIntPtr X) /* SAFE */
        {
            // NOTE: Easy way.
            // unsafe { return new IntPtr(X.ToPointer()); }

            // NOTE: Hard way.
            return new IntPtr(unchecked((long)X.ToUInt64()));
        }

        ///////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method converts the specified object to an unsigned long integer
        /// value using its <see cref="IConvertible" /> implementation, if any.
        /// </summary>
        /// <param name="X">
        /// The object to convert.  This may be null.
        /// </param>
        /// <param name="cultureInfo">
        /// The culture-specific formatting information to use when performing
        /// the conversion.
        /// </param>
        /// <returns>
        /// The resulting unsigned long integer value, or null if the object was
        /// null or did not implement <see cref="IConvertible" />.
        /// </returns>
        public static ulong? ToULong(
            object X,
            CultureInfo cultureInfo
            ) /* SAFE */
        {
            if (X == null)
                return null;

            IConvertible convertible = X as IConvertible;

            if (convertible == null)
                return null;

            return convertible.ToUInt64(cultureInfo);
        }

        ///////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method converts the specified boolean value to an unsigned long
        /// integer value.
        /// </summary>
        /// <param name="X">
        /// The value to convert.
        /// </param>
        /// <returns>
        /// One if the value is true; otherwise, zero.
        /// </returns>
        public static ulong ToULong(bool X) /* SAFE */
        {
            return X ? (ulong)1 : 0;
        }

        ///////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method converts the specified signed byte value to an unsigned
        /// long integer value, preserving its bit pattern.
        /// </summary>
        /// <param name="X">
        /// The value to convert.
        /// </param>
        /// <returns>
        /// The resulting unsigned long integer value.
        /// </returns>
        public static ulong ToULong(sbyte X) /* SAFE */
        {
            return unchecked((ulong)(byte)X);
        }

        ///////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method converts the specified short value to an unsigned long
        /// integer value, preserving its bit pattern.
        /// </summary>
        /// <param name="X">
        /// The value to convert.
        /// </param>
        /// <returns>
        /// The resulting unsigned long integer value.
        /// </returns>
        public static ulong ToULong(short X) /* SAFE */
        {
            return unchecked((ulong)(ushort)X);
        }

        ///////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method converts the specified integer value to an unsigned long
        /// integer value, preserving its bit pattern.
        /// </summary>
        /// <param name="X">
        /// The value to convert.
        /// </param>
        /// <returns>
        /// The resulting unsigned long integer value.
        /// </returns>
        public static ulong ToULong(int X) /* SAFE */
        {
            return unchecked((ulong)(uint)X);
        }

        ///////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method reinterprets the specified long integer value as an
        /// unsigned long integer value.
        /// </summary>
        /// <param name="X">
        /// The value to convert.
        /// </param>
        /// <returns>
        /// The resulting unsigned long integer value.
        /// </returns>
        public static ulong ToULong(long X) /* SAFE */
        {
            return unchecked((ulong)X);
        }

        ///////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method combines two unsigned integer values into a single
        /// unsigned long integer value.
        /// </summary>
        /// <param name="X">
        /// The value to use for the low-order 32 bits of the result.
        /// </param>
        /// <param name="Y">
        /// The value to use for the high-order 32 bits of the result.
        /// </param>
        /// <returns>
        /// The combined unsigned long integer value.
        /// </returns>
        public static ulong ToULong(uint X, uint Y) /* SAFE */
        {
            return (ulong)(X | ((ulong)Y << IntBits));
        }

        ///////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method converts the specified integer value to an unsigned
        /// pointer-sized integer value, preserving its bit pattern.
        /// </summary>
        /// <param name="X">
        /// The value to convert.
        /// </param>
        /// <returns>
        /// The resulting unsigned pointer-sized integer value.
        /// </returns>
        public static UIntPtr ToUIntPtr(int X) /* SAFE */
        {
            return new UIntPtr(unchecked((uint)X));
        }

        ///////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method converts the specified long integer value to an unsigned
        /// pointer-sized integer value, preserving its bit pattern.
        /// </summary>
        /// <param name="X">
        /// The value to convert.
        /// </param>
        /// <returns>
        /// The resulting unsigned pointer-sized integer value.
        /// </returns>
        public static UIntPtr ToUIntPtr(long X) /* SAFE */
        {
            return new UIntPtr(unchecked((ulong)X));
        }

        ///////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method reinterprets the specified (signed) pointer-sized integer
        /// value as an unsigned pointer-sized integer value.
        /// </summary>
        /// <param name="X">
        /// The value to convert.
        /// </param>
        /// <returns>
        /// The resulting unsigned pointer-sized integer value.
        /// </returns>
        public static UIntPtr ToUIntPtr(IntPtr X) /* SAFE */
        {
            // NOTE: Easy way.
            // unsafe { return new UIntPtr(X.ToPointer()); }

            // NOTE: Hard way.
            return new UIntPtr(unchecked((ulong)X.ToInt64()));
        }

        ///////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method converts the specified boolean value to a decimal value.
        /// </summary>
        /// <param name="X">
        /// The value to convert.
        /// </param>
        /// <returns>
        /// One if the value is true; otherwise, zero.
        /// </returns>
        public static decimal ToDecimal(bool X) /* SAFE */
        {
            return X ? 1 : 0;
        }

        ///////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method converts the specified integer value to a decimal value.
        /// </summary>
        /// <param name="X">
        /// The value to convert.
        /// </param>
        /// <returns>
        /// The resulting decimal value.
        /// </returns>
        public static decimal ToDecimal(int X) /* SAFE */
        {
            return X;
        }

        ///////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method converts the specified long integer value to a decimal
        /// value.
        /// </summary>
        /// <param name="X">
        /// The value to convert.
        /// </param>
        /// <returns>
        /// The resulting decimal value.
        /// </returns>
        public static decimal ToDecimal(long X) /* SAFE */
        {
            return X;
        }

        ///////////////////////////////////////////////////////////////////////////////////////

#if NET_40
        /// <summary>
        /// This method converts the specified arbitrary-precision integer value
        /// to a decimal value.
        /// </summary>
        /// <param name="X">
        /// The value to convert.
        /// </param>
        /// <returns>
        /// The resulting decimal value.
        /// </returns>
        public static decimal ToDecimal(BigInteger X) /* SAFE */
        {
            return (decimal)X;
        }
#endif

        ///////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method converts the specified date/time value to a decimal value
        /// containing its tick count.
        /// </summary>
        /// <param name="X">
        /// The value to convert.
        /// </param>
        /// <returns>
        /// The tick count of the value.
        /// </returns>
        public static decimal ToDecimal(DateTime X) /* SAFE */
        {
            return X.Ticks;
        }

        ///////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method converts the specified boolean value to a double value.
        /// </summary>
        /// <param name="X">
        /// The value to convert.
        /// </param>
        /// <returns>
        /// One if the value is true; otherwise, zero.
        /// </returns>
        public static double ToDouble(bool X) /* SAFE */
        {
            return X ? 1 : 0;
        }

        ///////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method converts the specified integer value to a double value.
        /// </summary>
        /// <param name="X">
        /// The value to convert.
        /// </param>
        /// <returns>
        /// The resulting double value.
        /// </returns>
        public static double ToDouble(int X) /* SAFE */
        {
            return X;
        }

        ///////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method converts the specified long integer value to a double
        /// value.
        /// </summary>
        /// <param name="X">
        /// The value to convert.
        /// </param>
        /// <returns>
        /// The resulting double value.
        /// </returns>
        public static double ToDouble(long X) /* SAFE */
        {
            return X;
        }

        ///////////////////////////////////////////////////////////////////////////////////////

#if NET_40
        /// <summary>
        /// This method converts the specified arbitrary-precision integer value
        /// to a double value.
        /// </summary>
        /// <param name="X">
        /// The value to convert.
        /// </param>
        /// <returns>
        /// The resulting double value.
        /// </returns>
        public static double ToDouble(BigInteger X) /* SAFE */
        {
            return (double)X;
        }
#endif

        ///////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method converts the specified date/time value to a double value
        /// containing its tick count.
        /// </summary>
        /// <param name="X">
        /// The value to convert.
        /// </param>
        /// <returns>
        /// The tick count of the value, as a double.
        /// </returns>
        public static double ToDouble(DateTime X) /* LOSSY */
        {
            return X.Ticks;
        }

        ///////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method converts the specified boolean value to a date/time value
        /// using the default date/time kind.
        /// </summary>
        /// <param name="X">
        /// The value to convert.
        /// </param>
        /// <returns>
        /// The resulting date/time value.
        /// </returns>
        public static DateTime ToDateTime(bool X) /* SAFE */
        {
            return ToDateTime(X, ObjectOps.GetDefaultDateTimeKind());
        }

        ///////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method converts the specified boolean value to a date/time value
        /// with the specified date/time kind.
        /// </summary>
        /// <param name="X">
        /// The value to convert.
        /// </param>
        /// <param name="kind">
        /// The date/time kind to associate with the resulting value.
        /// </param>
        /// <returns>
        /// The resulting date/time value.
        /// </returns>
        public static DateTime ToDateTime(bool X, DateTimeKind kind) /* SAFE */
        {
            return DateTime.SpecifyKind(new DateTime(X ? 1 : 0), kind);
        }

        ///////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method converts the specified integer value to a date/time value
        /// using the default date/time kind.
        /// </summary>
        /// <param name="X">
        /// The value to convert.
        /// </param>
        /// <returns>
        /// The resulting date/time value.
        /// </returns>
        public static DateTime ToDateTime(int X) /* SAFE */
        {
            return ToDateTime(X, ObjectOps.GetDefaultDateTimeKind());
        }

        ///////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method converts the specified integer value to a date/time value
        /// with the specified date/time kind.
        /// </summary>
        /// <param name="X">
        /// The value to convert.
        /// </param>
        /// <param name="kind">
        /// The date/time kind to associate with the resulting value.
        /// </param>
        /// <returns>
        /// The resulting date/time value.
        /// </returns>
        public static DateTime ToDateTime(int X, DateTimeKind kind) /* SAFE */
        {
            return DateTime.SpecifyKind(new DateTime(X), kind);
        }

        ///////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method converts the specified long integer value to a date/time
        /// value using the default date/time kind.
        /// </summary>
        /// <param name="X">
        /// The value to convert.
        /// </param>
        /// <returns>
        /// The resulting date/time value.
        /// </returns>
        public static DateTime ToDateTime(long X) /* LOSSY */
        {
            return ToDateTime(X, ObjectOps.GetDefaultDateTimeKind());
        }

        ///////////////////////////////////////////////////////////////////////////////////////

#if NET_40
        /// <summary>
        /// This method converts the specified arbitrary-precision integer value
        /// to a date/time value using the default date/time kind.
        /// </summary>
        /// <param name="X">
        /// The value to convert.
        /// </param>
        /// <returns>
        /// The resulting date/time value.
        /// </returns>
        public static DateTime ToDateTime(BigInteger X) /* SAFE */
        {
            return ToDateTime(X, ObjectOps.GetDefaultDateTimeKind());
        }

        ///////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method converts the specified arbitrary-precision integer value
        /// to a date/time value with the specified date/time kind.
        /// </summary>
        /// <param name="X">
        /// The value to convert.
        /// </param>
        /// <param name="kind">
        /// The date/time kind to associate with the resulting value.
        /// </param>
        /// <returns>
        /// The resulting date/time value.
        /// </returns>
        public static DateTime ToDateTime(BigInteger X, DateTimeKind kind) /* SAFE */
        {
            return ToDateTime((long)X, kind);
        }
#endif

        ///////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method converts the specified long integer value to a date/time
        /// value with the specified date/time kind.
        /// </summary>
        /// <param name="X">
        /// The value to convert.
        /// </param>
        /// <param name="kind">
        /// The date/time kind to associate with the resulting value.
        /// </param>
        /// <returns>
        /// The resulting date/time value.
        /// </returns>
        public static DateTime ToDateTime(long X, DateTimeKind kind) /* LOSSY */
        {
            //
            // NOTE: Limited to 0x2BCA2875F4373FFF ticks (not even close to
            //       the full range of long integers).
            //
            return DateTime.SpecifyKind(new DateTime(X), kind);
        }

        ///////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method converts the specified double value to a date/time value
        /// using the default date/time kind.
        /// </summary>
        /// <param name="X">
        /// The value to convert.
        /// </param>
        /// <returns>
        /// The resulting date/time value.
        /// </returns>
        public static DateTime ToDateTime(double X) /* LOSSY */
        {
            return ToDateTime(X, ObjectOps.GetDefaultDateTimeKind());
        }

        ///////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method converts the specified double value to a date/time value
        /// with the specified date/time kind, treating its raw bits as a tick
        /// count.
        /// </summary>
        /// <param name="X">
        /// The value to convert.
        /// </param>
        /// <param name="kind">
        /// The date/time kind to associate with the resulting value.
        /// </param>
        /// <returns>
        /// The resulting date/time value.
        /// </returns>
        public static DateTime ToDateTime(double X, DateTimeKind kind) /* LOSSY */
        {
            //
            // NOTE: Limited to 0x2BCA2875F4373FFF ticks (not even close to
            //       the full range of long integers).
            //
            return DateTime.SpecifyKind(new DateTime(BitConverter.DoubleToInt64Bits(X)), kind);
        }

        ///////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method converts the specified boolean value to a time span
        /// value.
        /// </summary>
        /// <param name="X">
        /// The value to convert.
        /// </param>
        /// <returns>
        /// The resulting time span value.
        /// </returns>
        public static TimeSpan ToTimeSpan(bool X) /* SAFE */
        {
            return new TimeSpan(X ? 1 : 0);
        }

        ///////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method converts the specified integer value to a time span value
        /// containing that number of ticks.
        /// </summary>
        /// <param name="X">
        /// The value to convert.
        /// </param>
        /// <returns>
        /// The resulting time span value.
        /// </returns>
        public static TimeSpan ToTimeSpan(int X) /* SAFE */
        {
            return new TimeSpan(X);
        }

        ///////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method converts the specified long integer value to a time span
        /// value containing that number of ticks.
        /// </summary>
        /// <param name="X">
        /// The value to convert.
        /// </param>
        /// <returns>
        /// The resulting time span value.
        /// </returns>
        public static TimeSpan ToTimeSpan(long X) /* SAFE */
        {
            return new TimeSpan(X);
        }

        ///////////////////////////////////////////////////////////////////////////////////////

#if NET_40
        /// <summary>
        /// This method converts the specified arbitrary-precision integer value
        /// to a time span value containing that number of ticks.
        /// </summary>
        /// <param name="X">
        /// The value to convert.
        /// </param>
        /// <returns>
        /// The resulting time span value.
        /// </returns>
        public static TimeSpan ToTimeSpan(BigInteger X) /* SAFE */
        {
            return new TimeSpan((long)X);
        }
#endif

        ///////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method converts the specified double value to a time span value,
        /// treating its raw bits as a tick count.
        /// </summary>
        /// <param name="X">
        /// The value to convert.
        /// </param>
        /// <returns>
        /// The resulting time span value.
        /// </returns>
        public static TimeSpan ToTimeSpan(double X) /* LOSSY (?) */
        {
            return new TimeSpan(BitConverter.DoubleToInt64Bits(X));
        }

        ///////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method converts the specified boolean value to its
        /// enable/disable string representation.
        /// </summary>
        /// <param name="X">
        /// The value to convert.
        /// </param>
        /// <returns>
        /// The string "enable" if the value is true; otherwise, the string
        /// "disable".
        /// </returns>
        public static string ToEnable(bool X)
        {
            return X ? EnableString : DisableString;
        }

        ///////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method converts the specified boolean value to its
        /// enabled/disabled string representation.
        /// </summary>
        /// <param name="X">
        /// The value to convert.
        /// </param>
        /// <returns>
        /// The string "enabled" if the value is true; otherwise, the string
        /// "disabled".
        /// </returns>
        public static string ToEnabled(bool X)
        {
            return X ? EnabledString : DisabledString;
        }

        ///////////////////////////////////////////////////////////////////////////////////////

        #region Dynamic Conversion Class
        /// <summary>
        /// This class provides the dynamic, string-based value conversion
        /// support used during marshalling.  It holds the lookup tables that map
        /// target types to their associated change-type and to-string callback
        /// methods, along with the conversion methods themselves.
        /// </summary>
        [ObjectId("8264f2fc-42c9-4892-b152-a6368115c4a7")]
        internal static class Dynamic
        {
            //
            // NOTE: What dynamic ChangeType conversions do we support?
            //
            /// <summary>
            /// The table mapping target types to the callback methods used to
            /// convert a string value into an instance of that type.
            /// </summary>
            public static readonly TypeChangeTypeCallbackDictionary ChangeTypes =
                ChangeType.PopulateCallbackTable();

            //
            // NOTE: What dynamic ToString type conversions do we support?
            //
            /// <summary>
            /// The table mapping source types to the callback methods used to
            /// convert an instance of that type into its string representation.
            /// </summary>
            public static readonly TypeToStringCallbackDictionary ToStringTypes =
                _ToString.PopulateCallbackTable();

            ///////////////////////////////////////////////////////////////////////////////////

            /// <summary>
            /// This method determines whether the specified type is a nullable
            /// type whose underlying value type matches the specified value
            /// type.
            /// </summary>
            /// <param name="type">
            /// The type to check.
            /// </param>
            /// <param name="valueType">
            /// The expected underlying value type of the nullable type.
            /// </param>
            /// <returns>
            /// True if the type is a nullable type with the specified underlying
            /// value type; otherwise, false.
            /// </returns>
            private static bool IsNullableType(
                Type type,
                Type valueType
                )
            {
                Type localValueType = null;

                if (MarshalOps.IsNullableType(type, true, ref localValueType) &&
                    Object.ReferenceEquals(localValueType, valueType))
                {
                    return true;
                }

                return false;
            }

            ///////////////////////////////////////////////////////////////////////////////////

            #region ToString Callback Class
            /// <summary>
            /// This class provides the callback methods used to convert
            /// supported value types into their string representations, honoring
            /// any interpreter-specific formatting settings.
            /// </summary>
            [ObjectId("ceeb671d-b4b4-4607-b96e-da5867b011d2")]
            internal static class _ToString
            {
                /// <summary>
                /// This method builds and returns the table mapping source types
                /// to their associated to-string callback methods.
                /// </summary>
                /// <returns>
                /// The newly built to-string callback table.
                /// </returns>
                public static TypeToStringCallbackDictionary PopulateCallbackTable()
                {
                    TypeToStringCallbackDictionary result =
                        new TypeToStringCallbackDictionary();

                    //
                    // NOTE: These conversion methods are used to enforce use of the
                    //       configured DateTimeFormat property for the interpreter,
                    //       if any.
                    //
                    result.Add(typeof(DateTime), FromDateTime);
                    result.Add(typeof(DateTime).MakeByRefType(), FromDateTime);
                    result.Add(typeof(DateTime?), FromDateTime);

                    //
                    // NOTE: Finally, return the fully built table to the caller.
                    //
                    return result;
                }

                ///////////////////////////////////////////////////////////////////////////////

                /// <summary>
                /// This method converts a date/time value into its string
                /// representation, honoring the date/time format configured for
                /// the interpreter, if any.
                /// </summary>
                /// <param name="interpreter">
                /// The interpreter whose configured date/time options will be
                /// used.
                /// </param>
                /// <param name="type">
                /// The type of the value being converted.
                /// </param>
                /// <param name="value">
                /// The date/time value to convert.  This may be null when the
                /// type is a nullable date/time type.
                /// </param>
                /// <param name="options">
                /// The options that may further refine the conversion (e.g. the
                /// date/time format).
                /// </param>
                /// <param name="cultureInfo">
                /// The culture-specific formatting information to use when
                /// formatting the value.
                /// </param>
                /// <param name="clientData">
                /// The caller-specific data; it is not used.
                /// </param>
                /// <param name="marshalFlags">
                /// The flags that control marshalling; they are not used.
                /// </param>
                /// <param name="text">
                /// Upon success, this will contain the string representation of
                /// the value.
                /// </param>
                /// <param name="error">
                /// Upon failure, this will contain information about the error.
                /// </param>
                /// <returns>
                /// <see cref="ReturnCode.Ok" /> on success;
                /// <see cref="ReturnCode.Error" /> on failure.
                /// </returns>
                public static ReturnCode FromDateTime(
                    Interpreter interpreter,
                    Type type,
                    object value,
                    OptionDictionary options,
                    CultureInfo cultureInfo,
                    IClientData clientData, /* NOT USED */
                    ref MarshalFlags marshalFlags, /* NOT USED */
                    ref string text,
                    ref Result error
                    )
                {
                    if ((value == null) && IsNullableType(type, typeof(DateTime)))
                    {
                        text = null;
                        return ReturnCode.Ok;
                    }
                    else if (value is DateTime)
                    {
                        //
                        // NOTE: There is no need for the DateTimeKind or
                        //       DateTimeStyles option values here because
                        //       they are only used when creating DateTime
                        //       values from a string.
                        //
                        string dateTimeFormat;

                        ObjectOps.ProcessDateTimeOptions(
                            interpreter, options, null, out dateTimeFormat);

                        DateTime dateTime = (DateTime)value;

                        if (cultureInfo != null) /* REDUNDANT? */
                        {
                            text = (dateTimeFormat != null) ?
                                dateTime.ToString(dateTimeFormat, cultureInfo) :
                                dateTime.ToString(cultureInfo);
                        }
                        else
                        {
                            text = (dateTimeFormat != null) ?
                                dateTime.ToString(dateTimeFormat) :
                                dateTime.ToString();
                        }

                        return ReturnCode.Ok;
                    }
                    else
                    {
                        error = "type mismatch, need DateTime";
                    }

                    return ReturnCode.Error;
                }
            }
            #endregion

            ///////////////////////////////////////////////////////////////////////////////////

            #region ChangeType Callback Class
            /// <summary>
            /// This class provides the callback methods used to convert a string
            /// value into an instance of one of the supported target types, as
            /// well as the table that maps each supported target type to its
            /// conversion callback.
            /// </summary>
            [ObjectId("96b36848-67b3-4c66-bccf-39f3726f57d0")]
            internal static class ChangeType
            {
                /// <summary>
                /// This method builds and returns the table mapping target types
                /// (including reference, nullable, and array variants) to their
                /// associated change-type callback methods.
                /// </summary>
                /// <returns>
                /// The newly built change-type callback table.
                /// </returns>
                public static TypeChangeTypeCallbackDictionary PopulateCallbackTable()
                {
                    Type runtimeType = MarshalOps.GetRuntimeType();

                    TypeChangeTypeCallbackDictionary result =
                        new TypeChangeTypeCallbackDictionary();

                    //
                    // NOTE: Add the special handlers for translating an opaque
                    //       object handle (string) into a real object.
                    //
                    result.Add(typeof(object), ToObject);
                    result.Add(typeof(object).MakeByRefType(), ToObject);

                    //
                    // NOTE: Add the special handlers for translating an opaque
                    //       interpreter handle (string) into a real interpreter.
                    //
                    result.Add(typeof(Interpreter), ToInterpreter);
                    result.Add(typeof(Interpreter).MakeByRefType(), ToInterpreter);

                    //
                    // NOTE: First, add the simple value types we support.
                    //
                    result.Add(typeof(bool), ToBoolean);
                    result.Add(typeof(sbyte), ToSignedByte);
                    result.Add(typeof(byte), ToByte);
                    result.Add(typeof(short), ToNarrowInteger);
                    result.Add(typeof(ushort), ToUnsignedNarrowInteger);
                    result.Add(typeof(char), ToCharacter);
                    result.Add(typeof(int), ToInteger);
                    result.Add(typeof(uint), ToUnsignedInteger);
                    result.Add(typeof(long), ToWideInteger);
                    result.Add(typeof(ulong), ToUnsignedWideInteger);
                    result.Add(typeof(decimal), ToDecimal);
                    result.Add(typeof(float), ToSingle);
                    result.Add(typeof(double), ToDouble);

                    //
                    // NOTE: Next, add any simple array types we support.
                    //
                    result.Add(typeof(char).MakeArrayType(), ToCharacterArray);

                    //
                    // NOTE: Next, add their corresponding reference types.
                    //
                    result.Add(typeof(bool).MakeByRefType(), ToBoolean);
                    result.Add(typeof(sbyte).MakeByRefType(), ToSignedByte);
                    result.Add(typeof(byte).MakeByRefType(), ToByte);
                    result.Add(typeof(short).MakeByRefType(), ToNarrowInteger);
                    result.Add(typeof(ushort).MakeByRefType(), ToUnsignedNarrowInteger);
                    result.Add(typeof(char).MakeByRefType(), ToCharacter);
                    result.Add(typeof(int).MakeByRefType(), ToInteger);
                    result.Add(typeof(uint).MakeByRefType(), ToUnsignedInteger);
                    result.Add(typeof(long).MakeByRefType(), ToWideInteger);
                    result.Add(typeof(ulong).MakeByRefType(), ToUnsignedWideInteger);
                    result.Add(typeof(decimal).MakeByRefType(), ToDecimal);
                    result.Add(typeof(float).MakeByRefType(), ToSingle);
                    result.Add(typeof(double).MakeByRefType(), ToDouble);

                    //
                    // NOTE: Next, add any simple array reference types we support.
                    //
                    result.Add(typeof(char).MakeArrayType().MakeByRefType(), ToCharacterArray);

                    //
                    // NOTE: Next, add their corresponding nullable types.
                    //
                    result.Add(typeof(bool?), ToBoolean);
                    result.Add(typeof(sbyte?), ToSignedByte);
                    result.Add(typeof(byte?), ToByte);
                    result.Add(typeof(short?), ToNarrowInteger);
                    result.Add(typeof(ushort?), ToUnsignedNarrowInteger);
                    result.Add(typeof(char?), ToCharacter);
                    result.Add(typeof(int?), ToInteger);
                    result.Add(typeof(uint?), ToUnsignedInteger);
                    result.Add(typeof(long?), ToWideInteger);
                    result.Add(typeof(ulong?), ToUnsignedWideInteger);
                    result.Add(typeof(decimal?), ToDecimal);
                    result.Add(typeof(float?), ToSingle);
                    result.Add(typeof(double?), ToDouble);

                    //
                    // NOTE: Next, add their corresponding reference types.
                    //
                    result.Add(typeof(bool?).MakeByRefType(), ToBoolean);
                    result.Add(typeof(sbyte?).MakeByRefType(), ToSignedByte);
                    result.Add(typeof(byte?).MakeByRefType(), ToByte);
                    result.Add(typeof(short?).MakeByRefType(), ToNarrowInteger);
                    result.Add(typeof(ushort?).MakeByRefType(), ToUnsignedNarrowInteger);
                    result.Add(typeof(char?).MakeByRefType(), ToCharacter);
                    result.Add(typeof(int?).MakeByRefType(), ToInteger);
                    result.Add(typeof(uint?).MakeByRefType(), ToUnsignedInteger);
                    result.Add(typeof(long?).MakeByRefType(), ToWideInteger);
                    result.Add(typeof(ulong?).MakeByRefType(), ToUnsignedWideInteger);
                    result.Add(typeof(decimal?).MakeByRefType(), ToDecimal);
                    result.Add(typeof(float?).MakeByRefType(), ToSingle);
                    result.Add(typeof(double?).MakeByRefType(), ToDouble);

                    //
                    // NOTE: Next, add the "special" value types we support.
                    //
                    result.Add(typeof(ValueType), ToPrimitive);
                    result.Add(typeof(Enum), ToEnumeration);
                    result.Add(typeof(Guid), ToGuid);
                    result.Add(typeof(DateTime), ToDateTime);
                    result.Add(typeof(TimeSpan), ToTimeSpan);
                    result.Add(typeof(StringList), ToStringList);
                    result.Add(typeof(Delegate), ToCommandCallback);
                    result.Add(typeof(Type), ToType);

                    if (runtimeType != null)
                        result.Add(runtimeType, ToType);

                    result.Add(typeof(Uri), ToUri);
                    result.Add(typeof(Version), ToVersion);
                    result.Add(typeof(INumber), ToNumber);
                    result.Add(typeof(IVariant), ToVariant);

                    //
                    // NOTE: Next, add their corresponding reference types.
                    //
                    result.Add(typeof(ValueType).MakeByRefType(), ToPrimitive);
                    result.Add(typeof(Enum).MakeByRefType(), ToEnumeration);
                    result.Add(typeof(DateTime).MakeByRefType(), ToDateTime);
                    result.Add(typeof(TimeSpan).MakeByRefType(), ToTimeSpan);
                    result.Add(typeof(Guid).MakeByRefType(), ToGuid);
                    result.Add(typeof(StringList).MakeByRefType(), ToStringList);
                    result.Add(typeof(Delegate).MakeByRefType(), ToCommandCallback);
                    result.Add(typeof(Type).MakeByRefType(), ToType);

                    if (runtimeType != null)
                        result.Add(runtimeType.MakeByRefType(), ToType);

                    result.Add(typeof(Uri).MakeByRefType(), ToUri);
                    result.Add(typeof(Version).MakeByRefType(), ToVersion);
                    result.Add(typeof(INumber).MakeByRefType(), ToNumber);
                    result.Add(typeof(IVariant).MakeByRefType(), ToVariant);

                    //
                    // NOTE: Next, add their corresponding nullable types.
                    //
                    result.Add(typeof(DateTime?), ToDateTime);
                    result.Add(typeof(TimeSpan?), ToTimeSpan);
                    result.Add(typeof(Guid?), ToGuid);

                    //
                    // NOTE: Next, add their corresponding reference types.
                    //
                    result.Add(typeof(DateTime?).MakeByRefType(), ToDateTime);
                    result.Add(typeof(TimeSpan?).MakeByRefType(), ToTimeSpan);
                    result.Add(typeof(Guid?).MakeByRefType(), ToGuid);

                    //
                    // NOTE: Next, add the special string list type conversions
                    //       that we know about.
                    //
                    result.Add(typeof(List<string>), ToStringList);
                    result.Add(typeof(List<string>).MakeByRefType(), ToStringList);
                    result.Add(typeof(IList<string>), ToStringList);
                    result.Add(typeof(IList<string>).MakeByRefType(), ToStringList);
                    result.Add(typeof(ICollection<string>), ToStringList);
                    result.Add(typeof(ICollection<string>).MakeByRefType(), ToStringList);
                    result.Add(typeof(IEnumerable<string>), ToStringList);
                    result.Add(typeof(IEnumerable<string>).MakeByRefType(), ToStringList);
                    result.Add(typeof(IStringList), ToStringList);
                    result.Add(typeof(IStringList).MakeByRefType(), ToStringList);

                    //
                    // NOTE: Finally, return the fully built table to the caller.
                    //
                    return result;
                }

                ///////////////////////////////////////////////////////////////////////////////

                /// <summary>
                /// This method converts the specified string value into a
                /// boolean value.
                /// </summary>
                /// <param name="interpreter">
                /// The interpreter that provides context for the conversion.
                /// </param>
                /// <param name="type">
                /// The target type of the conversion.
                /// </param>
                /// <param name="text">
                /// The string value to convert.
                /// </param>
                /// <param name="options">
                /// The options that may further refine the conversion.
                /// </param>
                /// <param name="cultureInfo">
                /// The culture-specific formatting information to use when
                /// performing the conversion.
                /// </param>
                /// <param name="clientData">
                /// The caller-specific data.
                /// </param>
                /// <param name="marshalFlags">
                /// The flags that control marshalling.
                /// </param>
                /// <param name="value">
                /// Upon success, this will contain the converted value.
                /// </param>
                /// <param name="error">
                /// Upon failure, this will contain information about the error.
                /// </param>
                /// <returns>
                /// <see cref="ReturnCode.Ok" /> on success;
                /// <see cref="ReturnCode.Error" /> on failure.
                /// </returns>
                private static ReturnCode ToBoolean(
                    Interpreter interpreter, /* NOT USED */
                    Type type,
                    string text,
                    OptionDictionary options, /* NOT USED */
                    CultureInfo cultureInfo,
                    IClientData clientData, /* NOT USED */
                    ref MarshalFlags marshalFlags, /* NOT USED */
                    ref object value, /* bool, System.Boolean */
                    ref Result error
                    )
                {
                    if ((text == null) && IsNullableType(type, typeof(bool)))
                    {
                        value = null;
                        return ReturnCode.Ok;
                    }
                    else
                    {
                        bool boolValue = false;

                        if (Value.GetBoolean2(
                               text, ValueFlags.AnyBoolean, cultureInfo,
                               ref boolValue, ref error) == ReturnCode.Ok)
                        {
                            value = boolValue;
                            return ReturnCode.Ok;
                        }
                    }

                    return ReturnCode.Error;
                }

                ///////////////////////////////////////////////////////////////////////////////

                /// <summary>
                /// This method converts the specified string value into a signed
                /// byte value.
                /// </summary>
                /// <param name="interpreter">
                /// The interpreter that provides context for the conversion.
                /// </param>
                /// <param name="type">
                /// The target type of the conversion.
                /// </param>
                /// <param name="text">
                /// The string value to convert.
                /// </param>
                /// <param name="options">
                /// The options that may further refine the conversion.
                /// </param>
                /// <param name="cultureInfo">
                /// The culture-specific formatting information to use when
                /// performing the conversion.
                /// </param>
                /// <param name="clientData">
                /// The caller-specific data.
                /// </param>
                /// <param name="marshalFlags">
                /// The flags that control marshalling.
                /// </param>
                /// <param name="value">
                /// Upon success, this will contain the converted value.
                /// </param>
                /// <param name="error">
                /// Upon failure, this will contain information about the error.
                /// </param>
                /// <returns>
                /// <see cref="ReturnCode.Ok" /> on success;
                /// <see cref="ReturnCode.Error" /> on failure.
                /// </returns>
                private static ReturnCode ToSignedByte(
                    Interpreter interpreter, /* NOT USED */
                    Type type,
                    string text,
                    OptionDictionary options, /* NOT USED */
                    CultureInfo cultureInfo,
                    IClientData clientData, /* NOT USED */
                    ref MarshalFlags marshalFlags,
                    ref object value, /* sbyte, System.SByte */
                    ref Result error
                    )
                {
                    if ((text == null) && IsNullableType(type, typeof(sbyte)))
                    {
                        value = null;
                        return ReturnCode.Ok;
                    }
                    else
                    {
                        ValueFlags valueFlags =
                            ValueFlags.AnyByte | ValueFlags.Signed;

                        if (FlagOps.HasFlags(marshalFlags,
                                MarshalFlags.WidenToUnsigned, true))
                        {
                            valueFlags |= ValueFlags.WidenToUnsigned;
                        }

                        sbyte sbyteValue = 0;

                        if (Value.GetSignedByte2(
                                text, valueFlags, cultureInfo, ref sbyteValue,
                                ref error) == ReturnCode.Ok)
                        {
                            value = sbyteValue;
                            return ReturnCode.Ok;
                        }

                        return ReturnCode.Error;
                    }
                }

                ///////////////////////////////////////////////////////////////////////////////

                /// <summary>
                /// This method converts the specified string value into a byte
                /// value.
                /// </summary>
                /// <param name="interpreter">
                /// The interpreter that provides context for the conversion.
                /// </param>
                /// <param name="type">
                /// The target type of the conversion.
                /// </param>
                /// <param name="text">
                /// The string value to convert.
                /// </param>
                /// <param name="options">
                /// The options that may further refine the conversion.
                /// </param>
                /// <param name="cultureInfo">
                /// The culture-specific formatting information to use when
                /// performing the conversion.
                /// </param>
                /// <param name="clientData">
                /// The caller-specific data.
                /// </param>
                /// <param name="marshalFlags">
                /// The flags that control marshalling.
                /// </param>
                /// <param name="value">
                /// Upon success, this will contain the converted value.
                /// </param>
                /// <param name="error">
                /// Upon failure, this will contain information about the error.
                /// </param>
                /// <returns>
                /// <see cref="ReturnCode.Ok" /> on success;
                /// <see cref="ReturnCode.Error" /> on failure.
                /// </returns>
                private static ReturnCode ToByte(
                    Interpreter interpreter, /* NOT USED */
                    Type type,
                    string text,
                    OptionDictionary options, /* NOT USED */
                    CultureInfo cultureInfo,
                    IClientData clientData, /* NOT USED */
                    ref MarshalFlags marshalFlags, /* NOT USED */
                    ref object value, /* byte, System.Byte */
                    ref Result error
                    )
                {
                    if ((text == null) && IsNullableType(type, typeof(byte)))
                    {
                        value = null;
                        return ReturnCode.Ok;
                    }
                    else
                    {
                        byte byteValue = 0;

                        if (Value.GetByte2(
                                text, ValueFlags.AnyByte, cultureInfo,
                                ref byteValue, ref error) == ReturnCode.Ok)
                        {
                            value = byteValue;
                            return ReturnCode.Ok;
                        }

                        return ReturnCode.Error;
                    }
                }

                ///////////////////////////////////////////////////////////////////////////////

                /// <summary>
                /// This method converts the specified string value into a narrow
                /// (16-bit) integer value.
                /// </summary>
                /// <param name="interpreter">
                /// The interpreter that provides context for the conversion.
                /// </param>
                /// <param name="type">
                /// The target type of the conversion.
                /// </param>
                /// <param name="text">
                /// The string value to convert.
                /// </param>
                /// <param name="options">
                /// The options that may further refine the conversion.
                /// </param>
                /// <param name="cultureInfo">
                /// The culture-specific formatting information to use when
                /// performing the conversion.
                /// </param>
                /// <param name="clientData">
                /// The caller-specific data.
                /// </param>
                /// <param name="marshalFlags">
                /// The flags that control marshalling.
                /// </param>
                /// <param name="value">
                /// Upon success, this will contain the converted value.
                /// </param>
                /// <param name="error">
                /// Upon failure, this will contain information about the error.
                /// </param>
                /// <returns>
                /// <see cref="ReturnCode.Ok" /> on success;
                /// <see cref="ReturnCode.Error" /> on failure.
                /// </returns>
                private static ReturnCode ToNarrowInteger(
                    Interpreter interpreter, /* NOT USED */
                    Type type,
                    string text,
                    OptionDictionary options, /* NOT USED */
                    CultureInfo cultureInfo,
                    IClientData clientData, /* NOT USED */
                    ref MarshalFlags marshalFlags,
                    ref object value, /* short, System.Int16 */
                    ref Result error
                    )
                {
                    if ((text == null) && IsNullableType(type, typeof(short)))
                    {
                        value = null;
                        return ReturnCode.Ok;
                    }
                    else
                    {
                        ValueFlags valueFlags = ValueFlags.AnyNarrowInteger;

                        if (FlagOps.HasFlags(marshalFlags,
                                MarshalFlags.WidenToUnsigned, true))
                        {
                            valueFlags |= ValueFlags.WidenToUnsigned;
                        }

                        short shortValue = 0;

                        if (Value.GetNarrowInteger2(
                                text, valueFlags, cultureInfo, ref shortValue,
                                ref error) == ReturnCode.Ok)
                        {
                            value = shortValue;
                            return ReturnCode.Ok;
                        }

                        return ReturnCode.Error;
                    }
                }

                ///////////////////////////////////////////////////////////////////////////////

                /// <summary>
                /// This method converts the specified string value into an
                /// unsigned narrow (16-bit) integer value.
                /// </summary>
                /// <param name="interpreter">
                /// The interpreter that provides context for the conversion.
                /// </param>
                /// <param name="type">
                /// The target type of the conversion.
                /// </param>
                /// <param name="text">
                /// The string value to convert.
                /// </param>
                /// <param name="options">
                /// The options that may further refine the conversion.
                /// </param>
                /// <param name="cultureInfo">
                /// The culture-specific formatting information to use when
                /// performing the conversion.
                /// </param>
                /// <param name="clientData">
                /// The caller-specific data.
                /// </param>
                /// <param name="marshalFlags">
                /// The flags that control marshalling.
                /// </param>
                /// <param name="value">
                /// Upon success, this will contain the converted value.
                /// </param>
                /// <param name="error">
                /// Upon failure, this will contain information about the error.
                /// </param>
                /// <returns>
                /// <see cref="ReturnCode.Ok" /> on success;
                /// <see cref="ReturnCode.Error" /> on failure.
                /// </returns>
                private static ReturnCode ToUnsignedNarrowInteger(
                    Interpreter interpreter, /* NOT USED */
                    Type type,
                    string text,
                    OptionDictionary options, /* NOT USED */
                    CultureInfo cultureInfo,
                    IClientData clientData, /* NOT USED */
                    ref MarshalFlags marshalFlags, /* NOT USED */
                    ref object value, /* ushort, System.UInt16 */
                    ref Result error
                    )
                {
                    if ((text == null) && IsNullableType(type, typeof(ushort)))
                    {
                        value = null;
                        return ReturnCode.Ok;
                    }
                    else
                    {
                        ushort ushortValue = 0;

                        if (Value.GetUnsignedNarrowInteger2(
                                text, ValueFlags.AnyNarrowInteger | ValueFlags.Unsigned,
                                cultureInfo, ref ushortValue, ref error) == ReturnCode.Ok)
                        {
                            value = ushortValue;
                            return ReturnCode.Ok;
                        }

                        return ReturnCode.Error;
                    }
                }

                ///////////////////////////////////////////////////////////////////////////////

                /// <summary>
                /// This method converts the specified string value into a
                /// character value.
                /// </summary>
                /// <param name="interpreter">
                /// The interpreter that provides context for the conversion.
                /// </param>
                /// <param name="type">
                /// The target type of the conversion.
                /// </param>
                /// <param name="text">
                /// The string value to convert.
                /// </param>
                /// <param name="options">
                /// The options that may further refine the conversion.
                /// </param>
                /// <param name="cultureInfo">
                /// The culture-specific formatting information to use when
                /// performing the conversion.
                /// </param>
                /// <param name="clientData">
                /// The caller-specific data.
                /// </param>
                /// <param name="marshalFlags">
                /// The flags that control marshalling.
                /// </param>
                /// <param name="value">
                /// Upon success, this will contain the converted value.
                /// </param>
                /// <param name="error">
                /// Upon failure, this will contain information about the error.
                /// </param>
                /// <returns>
                /// <see cref="ReturnCode.Ok" /> on success;
                /// <see cref="ReturnCode.Error" /> on failure.
                /// </returns>
                private static ReturnCode ToCharacter(
                    Interpreter interpreter, /* NOT USED */
                    Type type,
                    string text,
                    OptionDictionary options, /* NOT USED */
                    CultureInfo cultureInfo,
                    IClientData clientData, /* NOT USED */
                    ref MarshalFlags marshalFlags, /* NOT USED */
                    ref object value, /* char, System.Char */
                    ref Result error
                    )
                {
                    if ((text == null) && IsNullableType(type, typeof(char)))
                    {
                        value = null;
                        return ReturnCode.Ok;
                    }
                    else
                    {
                        char charValue = Characters.Null;

                        if (Value.GetCharacter2(
                                text, ValueFlags.AnyCharacter, cultureInfo,
                                ref charValue, ref error) == ReturnCode.Ok)
                        {
                            value = charValue;
                            return ReturnCode.Ok;
                        }

                        return ReturnCode.Error;
                    }
                }

                ///////////////////////////////////////////////////////////////////////////////

                /// <summary>
                /// This method converts the specified string value into a 32-bit
                /// integer value.
                /// </summary>
                /// <param name="interpreter">
                /// The interpreter that provides context for the conversion.
                /// </param>
                /// <param name="type">
                /// The target type of the conversion.
                /// </param>
                /// <param name="text">
                /// The string value to convert.
                /// </param>
                /// <param name="options">
                /// The options that may further refine the conversion.
                /// </param>
                /// <param name="cultureInfo">
                /// The culture-specific formatting information to use when
                /// performing the conversion.
                /// </param>
                /// <param name="clientData">
                /// The caller-specific data.
                /// </param>
                /// <param name="marshalFlags">
                /// The flags that control marshalling.
                /// </param>
                /// <param name="value">
                /// Upon success, this will contain the converted value.
                /// </param>
                /// <param name="error">
                /// Upon failure, this will contain information about the error.
                /// </param>
                /// <returns>
                /// <see cref="ReturnCode.Ok" /> on success;
                /// <see cref="ReturnCode.Error" /> on failure.
                /// </returns>
                private static ReturnCode ToInteger(
                    Interpreter interpreter, /* NOT USED */
                    Type type, /* NOT USED */
                    string text,
                    OptionDictionary options, /* NOT USED */
                    CultureInfo cultureInfo,
                    IClientData clientData, /* NOT USED */
                    ref MarshalFlags marshalFlags,
                    ref object value, /* int, System.Int32 */
                    ref Result error
                    )
                {
                    if ((text == null) && IsNullableType(type, typeof(int)))
                    {
                        value = null;
                        return ReturnCode.Ok;
                    }
                    else
                    {
                        ValueFlags valueFlags = ValueFlags.AnyInteger;

                        if (FlagOps.HasFlags(marshalFlags,
                                MarshalFlags.WidenToUnsigned, true))
                        {
                            valueFlags |= ValueFlags.WidenToUnsigned;
                        }

                        int intValue = 0;

                        if (Value.GetInteger2(
                                text, valueFlags, cultureInfo, ref intValue,
                                ref error) == ReturnCode.Ok)
                        {
                            value = intValue;
                            return ReturnCode.Ok;
                        }

                        return ReturnCode.Error;
                    }
                }

                ///////////////////////////////////////////////////////////////////////////////

                /// <summary>
                /// This method converts the specified string value into an
                /// unsigned 32-bit integer value.
                /// </summary>
                /// <param name="interpreter">
                /// The interpreter that provides context for the conversion.
                /// </param>
                /// <param name="type">
                /// The target type of the conversion.
                /// </param>
                /// <param name="text">
                /// The string value to convert.
                /// </param>
                /// <param name="options">
                /// The options that may further refine the conversion.
                /// </param>
                /// <param name="cultureInfo">
                /// The culture-specific formatting information to use when
                /// performing the conversion.
                /// </param>
                /// <param name="clientData">
                /// The caller-specific data.
                /// </param>
                /// <param name="marshalFlags">
                /// The flags that control marshalling.
                /// </param>
                /// <param name="value">
                /// Upon success, this will contain the converted value.
                /// </param>
                /// <param name="error">
                /// Upon failure, this will contain information about the error.
                /// </param>
                /// <returns>
                /// <see cref="ReturnCode.Ok" /> on success;
                /// <see cref="ReturnCode.Error" /> on failure.
                /// </returns>
                private static ReturnCode ToUnsignedInteger(
                    Interpreter interpreter, /* NOT USED */
                    Type type, /* NOT USED */
                    string text,
                    OptionDictionary options, /* NOT USED */
                    CultureInfo cultureInfo,
                    IClientData clientData, /* NOT USED */
                    ref MarshalFlags marshalFlags, /* NOT USED */
                    ref object value, /* uint, System.UInt32 */
                    ref Result error
                    )
                {
                    if ((text == null) && IsNullableType(type, typeof(uint)))
                    {
                        value = null;
                        return ReturnCode.Ok;
                    }
                    else
                    {
                        uint uintValue = 0;

                        if (Value.GetUnsignedInteger2(
                                text, ValueFlags.AnyInteger | ValueFlags.Unsigned,
                                cultureInfo, ref uintValue, ref error) == ReturnCode.Ok)
                        {
                            value = uintValue;
                            return ReturnCode.Ok;
                        }

                        return ReturnCode.Error;
                    }
                }

                ///////////////////////////////////////////////////////////////////////////////

                /// <summary>
                /// This method converts the specified string value into a wide
                /// (64-bit) integer value.
                /// </summary>
                /// <param name="interpreter">
                /// The interpreter that provides context for the conversion.
                /// </param>
                /// <param name="type">
                /// The target type of the conversion.
                /// </param>
                /// <param name="text">
                /// The string value to convert.
                /// </param>
                /// <param name="options">
                /// The options that may further refine the conversion.
                /// </param>
                /// <param name="cultureInfo">
                /// The culture-specific formatting information to use when
                /// performing the conversion.
                /// </param>
                /// <param name="clientData">
                /// The caller-specific data.
                /// </param>
                /// <param name="marshalFlags">
                /// The flags that control marshalling.
                /// </param>
                /// <param name="value">
                /// Upon success, this will contain the converted value.
                /// </param>
                /// <param name="error">
                /// Upon failure, this will contain information about the error.
                /// </param>
                /// <returns>
                /// <see cref="ReturnCode.Ok" /> on success;
                /// <see cref="ReturnCode.Error" /> on failure.
                /// </returns>
                private static ReturnCode ToWideInteger(
                    Interpreter interpreter, /* NOT USED */
                    Type type,
                    string text,
                    OptionDictionary options, /* NOT USED */
                    CultureInfo cultureInfo,
                    IClientData clientData, /* NOT USED */
                    ref MarshalFlags marshalFlags,
                    ref object value, /* long, System.Int64 */
                    ref Result error
                    )
                {
                    if ((text == null) && IsNullableType(type, typeof(long)))
                    {
                        value = null;
                        return ReturnCode.Ok;
                    }
                    else
                    {
                        ValueFlags valueFlags = ValueFlags.AnyWideInteger;

                        if (FlagOps.HasFlags(marshalFlags,
                                MarshalFlags.WidenToUnsigned, true))
                        {
                            valueFlags |= ValueFlags.WidenToUnsigned;
                        }

                        long longValue = 0;

                        if (Value.GetWideInteger2(
                                text, valueFlags, cultureInfo, ref longValue,
                                ref error) == ReturnCode.Ok)
                        {
                            value = longValue;
                            return ReturnCode.Ok;
                        }

                        return ReturnCode.Error;
                    }
                }

                ///////////////////////////////////////////////////////////////////////////////

                /// <summary>
                /// This method converts the specified string value into an
                /// unsigned wide (64-bit) integer value.
                /// </summary>
                /// <param name="interpreter">
                /// The interpreter that provides context for the conversion.
                /// </param>
                /// <param name="type">
                /// The target type of the conversion.
                /// </param>
                /// <param name="text">
                /// The string value to convert.
                /// </param>
                /// <param name="options">
                /// The options that may further refine the conversion.
                /// </param>
                /// <param name="cultureInfo">
                /// The culture-specific formatting information to use when
                /// performing the conversion.
                /// </param>
                /// <param name="clientData">
                /// The caller-specific data.
                /// </param>
                /// <param name="marshalFlags">
                /// The flags that control marshalling.
                /// </param>
                /// <param name="value">
                /// Upon success, this will contain the converted value.
                /// </param>
                /// <param name="error">
                /// Upon failure, this will contain information about the error.
                /// </param>
                /// <returns>
                /// <see cref="ReturnCode.Ok" /> on success;
                /// <see cref="ReturnCode.Error" /> on failure.
                /// </returns>
                private static ReturnCode ToUnsignedWideInteger(
                    Interpreter interpreter, /* NOT USED */
                    Type type,
                    string text,
                    OptionDictionary options, /* NOT USED */
                    CultureInfo cultureInfo,
                    IClientData clientData, /* NOT USED */
                    ref MarshalFlags marshalFlags, /* NOT USED */
                    ref object value, /* ulong, System.UInt64 */
                    ref Result error
                    )
                {
                    if ((text == null) && IsNullableType(type, typeof(ulong)))
                    {
                        value = null;
                        return ReturnCode.Ok;
                    }
                    else
                    {
                        ulong ulongValue = 0;

                        if (Value.GetUnsignedWideInteger2(
                                text, ValueFlags.AnyWideInteger | ValueFlags.Unsigned,
                                cultureInfo, ref ulongValue, ref error) == ReturnCode.Ok)
                        {
                            value = ulongValue;
                            return ReturnCode.Ok;
                        }

                        return ReturnCode.Error;
                    }
                }

                ///////////////////////////////////////////////////////////////////////////////

                /// <summary>
                /// This method converts the specified string value into a number
                /// value (or its wrapper, depending on the marshal flags).
                /// </summary>
                /// <param name="interpreter">
                /// The interpreter that provides context for the conversion.
                /// </param>
                /// <param name="type">
                /// The target type of the conversion.
                /// </param>
                /// <param name="text">
                /// The string value to convert.
                /// </param>
                /// <param name="options">
                /// The options that may further refine the conversion.
                /// </param>
                /// <param name="cultureInfo">
                /// The culture-specific formatting information to use when
                /// performing the conversion.
                /// </param>
                /// <param name="clientData">
                /// The caller-specific data.
                /// </param>
                /// <param name="marshalFlags">
                /// The flags that control marshalling.
                /// </param>
                /// <param name="value">
                /// Upon success, this will contain the converted value.
                /// </param>
                /// <param name="error">
                /// Upon failure, this will contain information about the error.
                /// </param>
                /// <returns>
                /// <see cref="ReturnCode.Ok" /> on success;
                /// <see cref="ReturnCode.Error" /> on failure.
                /// </returns>
                private static ReturnCode ToNumber(
                    Interpreter interpreter, /* NOT USED */
                    Type type, /* NOT USED */
                    string text,
                    OptionDictionary options, /* NOT USED */
                    CultureInfo cultureInfo,
                    IClientData clientData, /* NOT USED */
                    ref MarshalFlags marshalFlags, /* NOT USED */
                    ref object value, /* NOTE: Not Eagle._Interfaces.Public.INumber. */
                    ref Result error
                    )
                {
                    INumber numberValue = null;

                    if (Value.GetNumber(
                            text, ValueFlags.AnyNumberAnyRadix, cultureInfo,
                            ref numberValue, ref error) == ReturnCode.Ok)
                    {
                        if (FlagOps.HasFlags(
                                marshalFlags, MarshalFlags.KeepWrapper, true))
                        {
                            value = numberValue;
                            return ReturnCode.Ok;
                        }
                        else
                        {
                            if (numberValue != null)
                            {
                                value = numberValue.Value;
                                return ReturnCode.Ok;
                            }
                            else
                            {
                                error = String.Format(
                                    "invalid {0} instance from GetNumber",
                                    typeof(INumber));
                            }
                        }
                    }

                    return ReturnCode.Error;
                }

                ///////////////////////////////////////////////////////////////////////////////

                /// <summary>
                /// This method converts the specified string value into a variant
                /// value (or its wrapper, depending on the marshal flags).
                /// </summary>
                /// <param name="interpreter">
                /// The interpreter that provides context for the conversion.
                /// </param>
                /// <param name="type">
                /// The target type of the conversion.
                /// </param>
                /// <param name="text">
                /// The string value to convert.
                /// </param>
                /// <param name="options">
                /// The options that may further refine the conversion.
                /// </param>
                /// <param name="cultureInfo">
                /// The culture-specific formatting information to use when
                /// performing the conversion.
                /// </param>
                /// <param name="clientData">
                /// The caller-specific data.
                /// </param>
                /// <param name="marshalFlags">
                /// The flags that control marshalling.
                /// </param>
                /// <param name="value">
                /// Upon success, this will contain the converted value.
                /// </param>
                /// <param name="error">
                /// Upon failure, this will contain information about the error.
                /// </param>
                /// <returns>
                /// <see cref="ReturnCode.Ok" /> on success;
                /// <see cref="ReturnCode.Error" /> on failure.
                /// </returns>
                private static ReturnCode ToVariant(
                    Interpreter interpreter, /* NOT USED */
                    Type type, /* NOT USED */
                    string text,
                    OptionDictionary options, /* NOT USED */
                    CultureInfo cultureInfo,
                    IClientData clientData, /* NOT USED */
                    ref MarshalFlags marshalFlags,
                    ref object value, /* NOTE: Not Eagle._Interfaces.Public.IVariant. */
                    ref Result error
                    )
                {
                    IVariant variantValue = null;

                    if (Value.GetVariant(
                            interpreter, text, ValueFlags.AnyVariant,
                            cultureInfo, ref variantValue,
                            ref error) == ReturnCode.Ok)
                    {
                        if (FlagOps.HasFlags(
                                marshalFlags, MarshalFlags.KeepWrapper, true))
                        {
                            value = variantValue;
                            return ReturnCode.Ok;
                        }
                        else
                        {
                            if (variantValue != null)
                            {
                                value = variantValue.Value;
                                return ReturnCode.Ok;
                            }
                            else
                            {
                                error = String.Format(
                                    "invalid {0} instance from GetVariant",
                                    typeof(IVariant));
                            }
                        }
                    }

                    return ReturnCode.Error;
                }

                ///////////////////////////////////////////////////////////////////////////////

                /// <summary>
                /// This method converts the specified string value into an
                /// enumerated value of the specified enumeration type.
                /// </summary>
                /// <param name="interpreter">
                /// The interpreter that provides context for the conversion.
                /// </param>
                /// <param name="type">
                /// The target type of the conversion.
                /// </param>
                /// <param name="text">
                /// The string value to convert.
                /// </param>
                /// <param name="options">
                /// The options that may further refine the conversion.
                /// </param>
                /// <param name="cultureInfo">
                /// The culture-specific formatting information to use when
                /// performing the conversion.
                /// </param>
                /// <param name="clientData">
                /// The caller-specific data.
                /// </param>
                /// <param name="marshalFlags">
                /// The flags that control marshalling.
                /// </param>
                /// <param name="value">
                /// Upon success, this will contain the converted value.
                /// </param>
                /// <param name="error">
                /// Upon failure, this will contain information about the error.
                /// </param>
                /// <returns>
                /// <see cref="ReturnCode.Ok" /> on success;
                /// <see cref="ReturnCode.Error" /> on failure.
                /// </returns>
                private static ReturnCode ToEnumeration(
                    Interpreter interpreter,
                    Type type,
                    string text,
                    OptionDictionary options, /* NOT USED */
                    CultureInfo cultureInfo, /* NOT USED */
                    IClientData clientData, /* NOT USED */
                    ref MarshalFlags marshalFlags, /* NOT USED */
                    ref object value,
                    ref Result error
                    )
                {
                    if (type != null)
                    {
                        Type elementType = null;

                        if (MarshalOps.IsEnumType(type, true, true, ref elementType))
                        {
                            object enumValue;

                            if (EnumOps.IsFlags(elementType))
                            {
                                enumValue = EnumOps.TryParseFlags(
                                    interpreter, elementType, null, text,
                                    cultureInfo, true, true, true, ref error);
                            }
                            else
                            {
                                enumValue = EnumOps.TryParse(
                                    elementType, text, true, true, ref error);
                            }

                            //
                            // NOTE: Did we succeed in getting a value of the proper
                            //       enumerated type?  If so, set the value for the
                            //       caller and return success.  If not, the error
                            //       message has already been set and we will return
                            //       failure at the end of the method.
                            //
                            if (enumValue != null)
                            {
                                value = enumValue;
                                return ReturnCode.Ok;
                            }
                        }
                        else
                        {
                            error = String.Format(
                                "type {0} is not an enumeration",
                                FormatOps.TypeName(type));
                        }
                    }
                    else
                    {
                        error = "invalid type";
                    }

                    return ReturnCode.Error;
                }

                ///////////////////////////////////////////////////////////////////////////////

                #region Dead Code
#if DEAD_CODE
                /// <summary>
                /// This method converts the specified string value into a return code
                /// value.
                /// </summary>
                /// <param name="interpreter">
                /// The interpreter that provides context for the conversion.
                /// </param>
                /// <param name="type">
                /// The target type of the conversion.
                /// </param>
                /// <param name="text">
                /// The string value to convert.
                /// </param>
                /// <param name="options">
                /// The options that may further refine the conversion.
                /// </param>
                /// <param name="cultureInfo">
                /// The culture-specific formatting information to use when
                /// performing the conversion.
                /// </param>
                /// <param name="clientData">
                /// The caller-specific data.
                /// </param>
                /// <param name="marshalFlags">
                /// The flags that control marshalling.
                /// </param>
                /// <param name="value">
                /// Upon success, this will contain the converted value.
                /// </param>
                /// <param name="error">
                /// Upon failure, this will contain information about the error.
                /// </param>
                /// <returns>
                /// <see cref="ReturnCode.Ok" /> on success;
                /// <see cref="ReturnCode.Error" /> on failure.
                /// </returns>
                private static ReturnCode ToReturnCode(
                    Interpreter interpreter, /* NOT USED */
                    Type type,
                    string text,
                    OptionDictionary options, /* NOT USED */
                    CultureInfo cultureInfo,
                    IClientData clientData, /* NOT USED */
                    ref MarshalFlags marshalFlags, /* NOT USED */
                    ref object value,
                    ref Result error
                    )
                {
                    if ((text == null) && IsNullableType(type, typeof(ReturnCode)))
                    {
                        value = null;
                        return ReturnCode.Ok;
                    }
                    else
                    {
                        ReturnCode returnCode = ReturnCode.Ok;

                        if (Value.GetReturnCode2(
                                text, ValueFlags.AnyReturnCode, cultureInfo,
                                ref returnCode, ref error) == ReturnCode.Ok)
                        {
                            value = returnCode;
                            return ReturnCode.Ok;
                        }

                        return ReturnCode.Error;
                    }
                }
#endif
                #endregion

                ///////////////////////////////////////////////////////////////////////////////

                /// <summary>
                /// This method converts the specified string value into a
                /// date/time value, honoring any configured date/time options.
                /// </summary>
                /// <param name="interpreter">
                /// The interpreter that provides context for the conversion.
                /// </param>
                /// <param name="type">
                /// The target type of the conversion.
                /// </param>
                /// <param name="text">
                /// The string value to convert.
                /// </param>
                /// <param name="options">
                /// The options that may further refine the conversion.
                /// </param>
                /// <param name="cultureInfo">
                /// The culture-specific formatting information to use when
                /// performing the conversion.
                /// </param>
                /// <param name="clientData">
                /// The caller-specific data.
                /// </param>
                /// <param name="marshalFlags">
                /// The flags that control marshalling.
                /// </param>
                /// <param name="value">
                /// Upon success, this will contain the converted value.
                /// </param>
                /// <param name="error">
                /// Upon failure, this will contain information about the error.
                /// </param>
                /// <returns>
                /// <see cref="ReturnCode.Ok" /> on success;
                /// <see cref="ReturnCode.Error" /> on failure.
                /// </returns>
                private static ReturnCode ToDateTime(
                    Interpreter interpreter,
                    Type type,
                    string text,
                    OptionDictionary options,
                    CultureInfo cultureInfo,
                    IClientData clientData, /* NOT USED */
                    ref MarshalFlags marshalFlags, /* NOT USED */
                    ref object value, /* System.DateTime */
                    ref Result error
                    )
                {
                    if ((text == null) && IsNullableType(type, typeof(DateTime)))
                    {
                        value = null;
                        return ReturnCode.Ok;
                    }
                    else
                    {
                        DateTimeKind dateTimeKind;
                        DateTimeStyles dateTimeStyles;
                        string dateTimeFormat;

                        ObjectOps.ProcessDateTimeOptions(
                            interpreter, options, null, null, null, out dateTimeKind,
                            out dateTimeStyles, out dateTimeFormat);

                        DateTime dateTime = DateTime.MinValue;

                        if (Value.GetDateTime2(
                                text, dateTimeFormat, ValueFlags.AnyDateTime,
                                dateTimeKind, dateTimeStyles, cultureInfo,
                                ref dateTime, ref error) == ReturnCode.Ok)
                        {
                            value = dateTime;
                            return ReturnCode.Ok;
                        }

                        return ReturnCode.Error;
                    }
                }

                ///////////////////////////////////////////////////////////////////////////////

                /// <summary>
                /// This method converts the specified string value into a time
                /// span value.
                /// </summary>
                /// <param name="interpreter">
                /// The interpreter that provides context for the conversion.
                /// </param>
                /// <param name="type">
                /// The target type of the conversion.
                /// </param>
                /// <param name="text">
                /// The string value to convert.
                /// </param>
                /// <param name="options">
                /// The options that may further refine the conversion.
                /// </param>
                /// <param name="cultureInfo">
                /// The culture-specific formatting information to use when
                /// performing the conversion.
                /// </param>
                /// <param name="clientData">
                /// The caller-specific data.
                /// </param>
                /// <param name="marshalFlags">
                /// The flags that control marshalling.
                /// </param>
                /// <param name="value">
                /// Upon success, this will contain the converted value.
                /// </param>
                /// <param name="error">
                /// Upon failure, this will contain information about the error.
                /// </param>
                /// <returns>
                /// <see cref="ReturnCode.Ok" /> on success;
                /// <see cref="ReturnCode.Error" /> on failure.
                /// </returns>
                private static ReturnCode ToTimeSpan(
                    Interpreter interpreter, /* NOT USED */
                    Type type,
                    string text,
                    OptionDictionary options, /* NOT USED */
                    CultureInfo cultureInfo,
                    IClientData clientData, /* NOT USED */
                    ref MarshalFlags marshalFlags, /* NOT USED */
                    ref object value, /* System.TimeSpan */
                    ref Result error
                    )
                {
                    if ((text == null) && IsNullableType(type, typeof(TimeSpan)))
                    {
                        value = null;
                        return ReturnCode.Ok;
                    }
                    else
                    {
                        TimeSpan timeSpan = TimeSpan.Zero;

                        if (Value.GetTimeSpan2(
                                text, ValueFlags.AnyTimeSpan, cultureInfo,
                                ref timeSpan, ref error) == ReturnCode.Ok)
                        {
                            value = timeSpan;
                            return ReturnCode.Ok;
                        }

                        return ReturnCode.Error;
                    }
                }

                ///////////////////////////////////////////////////////////////////////////////

                /// <summary>
                /// This method converts the specified string value into a
                /// globally unique identifier (GUID) value.
                /// </summary>
                /// <param name="interpreter">
                /// The interpreter that provides context for the conversion.
                /// </param>
                /// <param name="type">
                /// The target type of the conversion.
                /// </param>
                /// <param name="text">
                /// The string value to convert.
                /// </param>
                /// <param name="options">
                /// The options that may further refine the conversion.
                /// </param>
                /// <param name="cultureInfo">
                /// The culture-specific formatting information to use when
                /// performing the conversion.
                /// </param>
                /// <param name="clientData">
                /// The caller-specific data.
                /// </param>
                /// <param name="marshalFlags">
                /// The flags that control marshalling.
                /// </param>
                /// <param name="value">
                /// Upon success, this will contain the converted value.
                /// </param>
                /// <param name="error">
                /// Upon failure, this will contain information about the error.
                /// </param>
                /// <returns>
                /// <see cref="ReturnCode.Ok" /> on success;
                /// <see cref="ReturnCode.Error" /> on failure.
                /// </returns>
                private static ReturnCode ToGuid(
                    Interpreter interpreter, /* NOT USED */
                    Type type, /* NOT USED */
                    string text,
                    OptionDictionary options, /* NOT USED */
                    CultureInfo cultureInfo,
                    IClientData clientData, /* NOT USED */
                    ref MarshalFlags marshalFlags, /* NOT USED */
                    ref object value, /* System.Guid */
                    ref Result error
                    )
                {
                    if ((text == null) && IsNullableType(type, typeof(Guid)))
                    {
                        value = null;
                        return ReturnCode.Ok;
                    }
                    else
                    {
                        Guid guid = Guid.Empty;

                        if (Value.GetGuid(
                                text, cultureInfo, ref guid,
                                ref error) == ReturnCode.Ok)
                        {
                            value = guid;
                            return ReturnCode.Ok;
                        }

                        return ReturnCode.Error;
                    }
                }

                ///////////////////////////////////////////////////////////////////////////////

                /// <summary>
                /// This method converts the specified string value into a decimal
                /// value.
                /// </summary>
                /// <param name="interpreter">
                /// The interpreter that provides context for the conversion.
                /// </param>
                /// <param name="type">
                /// The target type of the conversion.
                /// </param>
                /// <param name="text">
                /// The string value to convert.
                /// </param>
                /// <param name="options">
                /// The options that may further refine the conversion.
                /// </param>
                /// <param name="cultureInfo">
                /// The culture-specific formatting information to use when
                /// performing the conversion.
                /// </param>
                /// <param name="clientData">
                /// The caller-specific data.
                /// </param>
                /// <param name="marshalFlags">
                /// The flags that control marshalling.
                /// </param>
                /// <param name="value">
                /// Upon success, this will contain the converted value.
                /// </param>
                /// <param name="error">
                /// Upon failure, this will contain information about the error.
                /// </param>
                /// <returns>
                /// <see cref="ReturnCode.Ok" /> on success;
                /// <see cref="ReturnCode.Error" /> on failure.
                /// </returns>
                private static ReturnCode ToDecimal(
                    Interpreter interpreter, /* NOT USED */
                    Type type,
                    string text,
                    OptionDictionary options, /* NOT USED */
                    CultureInfo cultureInfo,
                    IClientData clientData, /* NOT USED */
                    ref MarshalFlags marshalFlags, /* NOT USED */
                    ref object value, /* decimal, System.Decimal */
                    ref Result error
                    )
                {
                    if ((text == null) && IsNullableType(type, typeof(decimal)))
                    {
                        value = null;
                        return ReturnCode.Ok;
                    }
                    else
                    {
                        decimal decimalValue = Decimal.Zero;

                        if (Value.GetDecimal(
                                text, ValueFlags.AnyDecimal, cultureInfo,
                                ref decimalValue, ref error) == ReturnCode.Ok)
                        {
                            value = decimalValue;
                            return ReturnCode.Ok;
                        }

                        return ReturnCode.Error;
                    }
                }

                ///////////////////////////////////////////////////////////////////////////////

                /// <summary>
                /// This method converts the specified string value into a single
                /// precision floating-point value.
                /// </summary>
                /// <param name="interpreter">
                /// The interpreter that provides context for the conversion.
                /// </param>
                /// <param name="type">
                /// The target type of the conversion.
                /// </param>
                /// <param name="text">
                /// The string value to convert.
                /// </param>
                /// <param name="options">
                /// The options that may further refine the conversion.
                /// </param>
                /// <param name="cultureInfo">
                /// The culture-specific formatting information to use when
                /// performing the conversion.
                /// </param>
                /// <param name="clientData">
                /// The caller-specific data.
                /// </param>
                /// <param name="marshalFlags">
                /// The flags that control marshalling.
                /// </param>
                /// <param name="value">
                /// Upon success, this will contain the converted value.
                /// </param>
                /// <param name="error">
                /// Upon failure, this will contain information about the error.
                /// </param>
                /// <returns>
                /// <see cref="ReturnCode.Ok" /> on success;
                /// <see cref="ReturnCode.Error" /> on failure.
                /// </returns>
                private static ReturnCode ToSingle(
                    Interpreter interpreter, /* NOT USED */
                    Type type,
                    string text,
                    OptionDictionary options, /* NOT USED */
                    CultureInfo cultureInfo,
                    IClientData clientData, /* NOT USED */
                    ref MarshalFlags marshalFlags, /* NOT USED */
                    ref object value,
                    ref Result error
                    )
                {
                    if ((text == null) && IsNullableType(type, typeof(float)))
                    {
                        value = null;
                        return ReturnCode.Ok;
                    }
                    else
                    {
                        float floatValue = 0.0f;

                        if (Value.GetSingle(
                                text, cultureInfo, ref floatValue,
                                ref error) == ReturnCode.Ok)
                        {
                            value = floatValue;
                            return ReturnCode.Ok;
                        }

                        return ReturnCode.Error;
                    }
                }

                ///////////////////////////////////////////////////////////////////////////////

                /// <summary>
                /// This method converts the specified string value into a double
                /// precision floating-point value.
                /// </summary>
                /// <param name="interpreter">
                /// The interpreter that provides context for the conversion.
                /// </param>
                /// <param name="type">
                /// The target type of the conversion.
                /// </param>
                /// <param name="text">
                /// The string value to convert.
                /// </param>
                /// <param name="options">
                /// The options that may further refine the conversion.
                /// </param>
                /// <param name="cultureInfo">
                /// The culture-specific formatting information to use when
                /// performing the conversion.
                /// </param>
                /// <param name="clientData">
                /// The caller-specific data.
                /// </param>
                /// <param name="marshalFlags">
                /// The flags that control marshalling.
                /// </param>
                /// <param name="value">
                /// Upon success, this will contain the converted value.
                /// </param>
                /// <param name="error">
                /// Upon failure, this will contain information about the error.
                /// </param>
                /// <returns>
                /// <see cref="ReturnCode.Ok" /> on success;
                /// <see cref="ReturnCode.Error" /> on failure.
                /// </returns>
                private static ReturnCode ToDouble(
                    Interpreter interpreter, /* NOT USED */
                    Type type,
                    string text,
                    OptionDictionary options, /* NOT USED */
                    CultureInfo cultureInfo,
                    IClientData clientData, /* NOT USED */
                    ref MarshalFlags marshalFlags, /* NOT USED */
                    ref object value,
                    ref Result error
                    )
                {
                    if ((text == null) && IsNullableType(type, typeof(double)))
                    {
                        value = null;
                        return ReturnCode.Ok;
                    }
                    else
                    {
                        double doubleValue = 0.0;

                        if (Value.GetDouble(
                                text, cultureInfo, ref doubleValue,
                                ref error) == ReturnCode.Ok)
                        {
                            value = doubleValue;
                            return ReturnCode.Ok;
                        }

                        return ReturnCode.Error;
                    }
                }

                ///////////////////////////////////////////////////////////////////////////////

                /// <summary>
                /// This method converts the specified string value into a
                /// <see cref="Type" /> value.
                /// </summary>
                /// <param name="interpreter">
                /// The interpreter that provides context for the conversion.
                /// </param>
                /// <param name="type">
                /// The target type of the conversion.
                /// </param>
                /// <param name="text">
                /// The string value to convert.
                /// </param>
                /// <param name="options">
                /// The options that may further refine the conversion.
                /// </param>
                /// <param name="cultureInfo">
                /// The culture-specific formatting information to use when
                /// performing the conversion.
                /// </param>
                /// <param name="clientData">
                /// The caller-specific data.
                /// </param>
                /// <param name="marshalFlags">
                /// The flags that control marshalling.
                /// </param>
                /// <param name="value">
                /// Upon success, this will contain the converted value.
                /// </param>
                /// <param name="error">
                /// Upon failure, this will contain information about the error.
                /// </param>
                /// <returns>
                /// <see cref="ReturnCode.Ok" /> on success;
                /// <see cref="ReturnCode.Error" /> on failure.
                /// </returns>
                private static ReturnCode ToType(
                    Interpreter interpreter,
                    Type type, /* NOT USED */
                    string text,
                    OptionDictionary options, /* NOT USED */
                    CultureInfo cultureInfo,
                    IClientData clientData, /* NOT USED */
                    ref MarshalFlags marshalFlags, /* NOT USED */
                    ref object value,
                    ref Result error
                    )
                {
                    Type typeValue = null;
                    ResultList errors = null;

                    if (Value.GetAnyType(interpreter, text, null,
                            (interpreter != null) ? interpreter.GetAppDomain() : null,
                            Value.GetTypeValueFlags(false, false, false), cultureInfo,
                            ref typeValue, ref errors) == ReturnCode.Ok)
                    {
                        value = typeValue;
                        return ReturnCode.Ok;
                    }
                    else
                    {
                        error = errors;
                    }

                    return ReturnCode.Error;
                }

                ///////////////////////////////////////////////////////////////////////////////

                /// <summary>
                /// This method converts the specified string value into an
                /// absolute <see cref="Uri" /> value.
                /// </summary>
                /// <param name="interpreter">
                /// The interpreter that provides context for the conversion.
                /// </param>
                /// <param name="type">
                /// The target type of the conversion.
                /// </param>
                /// <param name="text">
                /// The string value to convert.
                /// </param>
                /// <param name="options">
                /// The options that may further refine the conversion.
                /// </param>
                /// <param name="cultureInfo">
                /// The culture-specific formatting information to use when
                /// performing the conversion.
                /// </param>
                /// <param name="clientData">
                /// The caller-specific data.
                /// </param>
                /// <param name="marshalFlags">
                /// The flags that control marshalling.
                /// </param>
                /// <param name="value">
                /// Upon success, this will contain the converted value.
                /// </param>
                /// <param name="error">
                /// Upon failure, this will contain information about the error.
                /// </param>
                /// <returns>
                /// <see cref="ReturnCode.Ok" /> on success;
                /// <see cref="ReturnCode.Error" /> on failure.
                /// </returns>
                private static ReturnCode ToUri(
                    Interpreter interpreter, /* NOT USED */
                    Type type, /* NOT USED */
                    string text,
                    OptionDictionary options, /* NOT USED */
                    CultureInfo cultureInfo,
                    IClientData clientData, /* NOT USED */
                    ref MarshalFlags marshalFlags, /* NOT USED */
                    ref object value,
                    ref Result error
                    )
                {
                    Uri uri = null;

                    if (Value.GetUri(
                            text, UriKind.Absolute, cultureInfo,
                            ref uri, ref error) == ReturnCode.Ok)
                    {
                        value = uri;
                        return ReturnCode.Ok;
                    }

                    return ReturnCode.Error;
                }

                ///////////////////////////////////////////////////////////////////////////////

                /// <summary>
                /// This method converts the specified string value into a
                /// <see cref="Version" /> value.
                /// </summary>
                /// <param name="interpreter">
                /// The interpreter that provides context for the conversion.
                /// </param>
                /// <param name="type">
                /// The target type of the conversion.
                /// </param>
                /// <param name="text">
                /// The string value to convert.
                /// </param>
                /// <param name="options">
                /// The options that may further refine the conversion.
                /// </param>
                /// <param name="cultureInfo">
                /// The culture-specific formatting information to use when
                /// performing the conversion.
                /// </param>
                /// <param name="clientData">
                /// The caller-specific data.
                /// </param>
                /// <param name="marshalFlags">
                /// The flags that control marshalling.
                /// </param>
                /// <param name="value">
                /// Upon success, this will contain the converted value.
                /// </param>
                /// <param name="error">
                /// Upon failure, this will contain information about the error.
                /// </param>
                /// <returns>
                /// <see cref="ReturnCode.Ok" /> on success;
                /// <see cref="ReturnCode.Error" /> on failure.
                /// </returns>
                private static ReturnCode ToVersion(
                    Interpreter interpreter, /* NOT USED */
                    Type type, /* NOT USED */
                    string text,
                    OptionDictionary options, /* NOT USED */
                    CultureInfo cultureInfo,
                    IClientData clientData, /* NOT USED */
                    ref MarshalFlags marshalFlags, /* NOT USED */
                    ref object value,
                    ref Result error
                    )
                {
                    Version version = null;

                    if (Value.GetVersion(
                            text, cultureInfo, ref version,
                            ref error) == ReturnCode.Ok)
                    {
                        value = version;
                        return ReturnCode.Ok;
                    }

                    return ReturnCode.Error;
                }

                ///////////////////////////////////////////////////////////////////////////////

                /// <summary>
                /// This method converts the specified string value into a
                /// primitive value, then changes it to the requested target type
                /// using its <see cref="IConvertible" /> implementation when
                /// necessary.
                /// </summary>
                /// <param name="interpreter">
                /// The interpreter that provides context for the conversion.
                /// </param>
                /// <param name="type">
                /// The target type of the conversion.
                /// </param>
                /// <param name="text">
                /// The string value to convert.
                /// </param>
                /// <param name="options">
                /// The options that may further refine the conversion.
                /// </param>
                /// <param name="cultureInfo">
                /// The culture-specific formatting information to use when
                /// performing the conversion.
                /// </param>
                /// <param name="clientData">
                /// The caller-specific data.
                /// </param>
                /// <param name="marshalFlags">
                /// The flags that control marshalling.
                /// </param>
                /// <param name="value">
                /// Upon success, this will contain the converted value.
                /// </param>
                /// <param name="error">
                /// Upon failure, this will contain information about the error.
                /// </param>
                /// <returns>
                /// <see cref="ReturnCode.Ok" /> on success;
                /// <see cref="ReturnCode.Error" /> on failure.
                /// </returns>
                private static ReturnCode ToPrimitive(
                    Interpreter interpreter,
                    Type type,
                    string text,
                    OptionDictionary options,
                    CultureInfo cultureInfo,
                    IClientData clientData, /* NOT USED */
                    ref MarshalFlags marshalFlags, /* NOT USED */
                    ref object value,
                    ref Result error
                    )
                {
                    DateTimeKind dateTimeKind;
                    DateTimeStyles dateTimeStyles;
                    string dateTimeFormat;

                    ObjectOps.ProcessDateTimeOptions(
                        interpreter, options, null, null, null, out dateTimeKind,
                        out dateTimeStyles, out dateTimeFormat);

                    object localValue = null;

                    if (Value.GetValue(
                            text, dateTimeFormat, ValueFlags.AnyStrict,
                            dateTimeKind, dateTimeStyles, cultureInfo,
                            ref localValue, ref error) == ReturnCode.Ok)
                    {
                        try
                        {
                            if ((type != typeof(ValueType)) &&
                                (type != typeof(ValueType).MakeByRefType()))
                            {
                                if (localValue is IConvertible)
                                {
                                    value = Convert.ChangeType(
                                        localValue, type); /* throw */
                                }
                                else
                                {
                                    error = String.Format(
                                        "cannot convert from type {0} to type {1}",
                                        FormatOps.TypeName(localValue),
                                        FormatOps.TypeName(typeof(IConvertible)));
                                }
                            }
                            else
                            {
                                value = localValue;
                            }

                            return ReturnCode.Ok;
                        }
                        catch (Exception e)
                        {
                            error = e;
                        }
                    }

                    return ReturnCode.Error;
                }

                ///////////////////////////////////////////////////////////////////////////////

                /// <summary>
                /// This method converts the specified opaque interpreter handle
                /// string into an <see cref="Interpreter" /> instance.
                /// </summary>
                /// <param name="interpreter">
                /// The interpreter that provides context for the conversion.
                /// </param>
                /// <param name="type">
                /// The target type of the conversion.
                /// </param>
                /// <param name="text">
                /// The string value to convert.
                /// </param>
                /// <param name="options">
                /// The options that may further refine the conversion.
                /// </param>
                /// <param name="cultureInfo">
                /// The culture-specific formatting information to use when
                /// performing the conversion.
                /// </param>
                /// <param name="clientData">
                /// The caller-specific data.
                /// </param>
                /// <param name="marshalFlags">
                /// The flags that control marshalling.
                /// </param>
                /// <param name="value">
                /// Upon success, this will contain the converted value.
                /// </param>
                /// <param name="error">
                /// Upon failure, this will contain information about the error.
                /// </param>
                /// <returns>
                /// <see cref="ReturnCode.Ok" /> on success;
                /// <see cref="ReturnCode.Error" /> on failure.
                /// </returns>
                public static ReturnCode ToInterpreter(
                    Interpreter interpreter,
                    Type type,
                    string text,
                    OptionDictionary options, /* NOT USED */
                    CultureInfo cultureInfo, /* NOT USED */
                    IClientData clientData, /* NOT USED */
                    ref MarshalFlags marshalFlags, /* NOT USED */
                    ref object value,
                    ref Result error
                    )
                {
                    Interpreter otherInterpreter = null;

                    if (Value.GetInterpreter(
                            interpreter, text, InterpreterType.Default,
                            ref otherInterpreter, ref error) == ReturnCode.Ok)
                    {
                        value = otherInterpreter;
                        return ReturnCode.Ok;
                    }

                    return ReturnCode.Error;
                }

                ///////////////////////////////////////////////////////////////////////////////

                /// <summary>
                /// This method converts the specified opaque object handle string
                /// into the underlying object instance, verifying that it is
                /// assignable to (and, when applicable, trusted for) the
                /// requested target type.
                /// </summary>
                /// <param name="interpreter">
                /// The interpreter that provides context for the conversion.
                /// </param>
                /// <param name="type">
                /// The target type of the conversion.
                /// </param>
                /// <param name="text">
                /// The string value to convert.
                /// </param>
                /// <param name="options">
                /// The options that may further refine the conversion.
                /// </param>
                /// <param name="cultureInfo">
                /// The culture-specific formatting information to use when
                /// performing the conversion.
                /// </param>
                /// <param name="clientData">
                /// The caller-specific data.
                /// </param>
                /// <param name="marshalFlags">
                /// The flags that control marshalling.
                /// </param>
                /// <param name="value">
                /// Upon success, this will contain the converted value.
                /// </param>
                /// <param name="error">
                /// Upon failure, this will contain information about the error.
                /// </param>
                /// <returns>
                /// <see cref="ReturnCode.Ok" /> on success;
                /// <see cref="ReturnCode.Error" /> on failure.
                /// </returns>
                public static ReturnCode ToObject(
                    Interpreter interpreter,
                    Type type,
                    string text,
                    OptionDictionary options, /* NOT USED */
                    CultureInfo cultureInfo, /* NOT USED */
                    IClientData clientData, /* NOT USED */
                    ref MarshalFlags marshalFlags,
                    ref object value,
                    ref Result error
                    )
                {
                    if (type != null)
                    {
                        Type objectType = null;
                        ObjectFlags objectFlags = ObjectFlags.None;
                        object @object = null;

                        if (Value.GetObject(
                                interpreter, text, LookupFlags.Default,
                                ref objectType, ref objectFlags,
                                ref @object, ref error) == ReturnCode.Ok)
                        {
                            if ((interpreter == null) || !interpreter.InternalIsSafe() ||
                                PolicyOps.IsTrustedObject(
                                    interpreter, text, objectFlags, @object, ref error))
                            {
                                //
                                // NOTE: Get the type of the underlying
                                //       object instance.  If the object
                                //       instance is invalid here then
                                //       so is the type.
                                //
                                if ((objectType == null) &&
                                    MarshalOps.ShouldUseObjectGetType(@object, marshalFlags))
                                {
                                    objectType = (@object != null) ? @object.GetType() : null;
                                }

                                if ((@object == null) ||
                                    MarshalOps.IsAssignableFrom(type, objectType, marshalFlags))
                                {
                                    value = @object;
                                    return ReturnCode.Ok;
                                }
                                else
                                {
                                    error = String.Format(
                                        "object of type {0} is not assignable " +
                                        "from object \"{1}\" of type {2}",
                                        FormatOps.TypeName(type), text,
                                        FormatOps.TypeName(objectType));
                                }
                            }
                        }
                    }
                    else
                    {
                        error = "invalid type";
                    }

                    return ReturnCode.Error;
                }

                ///////////////////////////////////////////////////////////////////////////////

                /// <summary>
                /// This method converts the specified string value into an array
                /// of its constituent characters.
                /// </summary>
                /// <param name="interpreter">
                /// The interpreter that provides context for the conversion.
                /// </param>
                /// <param name="type">
                /// The target type of the conversion.
                /// </param>
                /// <param name="text">
                /// The string value to convert.
                /// </param>
                /// <param name="options">
                /// The options that may further refine the conversion.
                /// </param>
                /// <param name="cultureInfo">
                /// The culture-specific formatting information to use when
                /// performing the conversion.
                /// </param>
                /// <param name="clientData">
                /// The caller-specific data.
                /// </param>
                /// <param name="marshalFlags">
                /// The flags that control marshalling.
                /// </param>
                /// <param name="value">
                /// Upon success, this will contain the converted value.
                /// </param>
                /// <param name="error">
                /// Upon failure, this will contain information about the error.
                /// </param>
                /// <returns>
                /// Always returns <see cref="ReturnCode.Ok" />.
                /// </returns>
                private static ReturnCode ToCharacterArray(
                    Interpreter interpreter, /* NOT USED */
                    Type type, /* NOT USED */
                    string text,
                    OptionDictionary options, /* NOT USED */
                    CultureInfo cultureInfo, /* NOT USED */
                    IClientData clientData, /* NOT USED */
                    ref MarshalFlags marshalFlags, /* NOT USED */
                    ref object value,
                    ref Result error /* NOT USED */
                    )
                {
                    value = (text != null) ? text.ToCharArray() : null;
                    return ReturnCode.Ok;
                }

                ///////////////////////////////////////////////////////////////////////////////

                /// <summary>
                /// This method converts the specified string value, interpreted
                /// as a Tcl list, into a <see cref="StringList" /> instance.
                /// </summary>
                /// <param name="interpreter">
                /// The interpreter that provides context for the conversion.
                /// </param>
                /// <param name="type">
                /// The target type of the conversion.
                /// </param>
                /// <param name="text">
                /// The string value to convert.
                /// </param>
                /// <param name="options">
                /// The options that may further refine the conversion.
                /// </param>
                /// <param name="cultureInfo">
                /// The culture-specific formatting information to use when
                /// performing the conversion.
                /// </param>
                /// <param name="clientData">
                /// The caller-specific data.
                /// </param>
                /// <param name="marshalFlags">
                /// The flags that control marshalling.
                /// </param>
                /// <param name="value">
                /// Upon success, this will contain the converted value.
                /// </param>
                /// <param name="error">
                /// Upon failure, this will contain information about the error.
                /// </param>
                /// <returns>
                /// <see cref="ReturnCode.Ok" /> on success;
                /// <see cref="ReturnCode.Error" /> on failure.
                /// </returns>
                public static ReturnCode ToStringList(
                    Interpreter interpreter,
                    Type type, /* NOT USED */
                    string text,
                    OptionDictionary options, /* NOT USED */
                    CultureInfo cultureInfo, /* NOT USED */
                    IClientData clientData, /* NOT USED */
                    ref MarshalFlags marshalFlags, /* NOT USED */
                    ref object value,
                    ref Result error
                    )
                {
                    StringList list = null;

                    //
                    // TODO: *PERF* We cannot have this call to SplitList perform any
                    //       caching because we do not know exactly what the resulting
                    //       list will be used for.
                    //
                    if (ParserOps<string>.SplitList(
                            interpreter, text, 0, Length.Invalid, false,
                            ref list, ref error) == ReturnCode.Ok)
                    {
                        value = list;
                        return ReturnCode.Ok;
                    }

                    return ReturnCode.Error;
                }

                ///////////////////////////////////////////////////////////////////////////////

                /// <summary>
                /// This method converts the specified string value, interpreted
                /// as a command (optionally preceded by callback options), into a
                /// delegate of the requested type that invokes that command.
                /// </summary>
                /// <param name="interpreter">
                /// The interpreter that provides context for the conversion.
                /// </param>
                /// <param name="type">
                /// The target type of the conversion.
                /// </param>
                /// <param name="text">
                /// The string value to convert.
                /// </param>
                /// <param name="options">
                /// The options that may further refine the conversion.
                /// </param>
                /// <param name="cultureInfo">
                /// The culture-specific formatting information to use when
                /// performing the conversion.
                /// </param>
                /// <param name="clientData">
                /// The caller-specific data.
                /// </param>
                /// <param name="marshalFlags">
                /// The flags that control marshalling.
                /// </param>
                /// <param name="value">
                /// Upon success, this will contain the converted value.
                /// </param>
                /// <param name="error">
                /// Upon failure, this will contain information about the error.
                /// </param>
                /// <returns>
                /// <see cref="ReturnCode.Ok" /> on success;
                /// <see cref="ReturnCode.Error" /> on failure.
                /// </returns>
                public static ReturnCode ToCommandCallback(
                    Interpreter interpreter,
                    Type type,
                    string text,
                    OptionDictionary options, /* NOT USED */
                    CultureInfo cultureInfo,
                    IClientData clientData, /* NOT USED */
                    ref MarshalFlags marshalFlags,
                    ref object value,
                    ref Result error
                    )
                {
                    if (type != null)
                    {
                        if (IsDelegateType(type, false))
                        {
                            //
                            // NOTE: Extract some marshal flags that we need further
                            //       down (just below) in this method.
                            //
                            bool useDelegateCallback = !FlagOps.HasFlags(
                                marshalFlags, MarshalFlags.NoDelegateCallback, true);

                            bool useGenericCallback = !FlagOps.HasFlags(
                                marshalFlags, MarshalFlags.NoGenericCallback, true);

                            bool useDynamicCallback = FlagOps.HasFlags(
                                marshalFlags, MarshalFlags.DynamicCallback, true);

                            bool useSimpleCallback = FlagOps.HasFlags(
                                marshalFlags, MarshalFlags.SimpleCallback, true);

                            //
                            // NOTE: Attempt to figure out if the target type is one
                            //       of the supported Delegate-derived types that we
                            //       support.
                            //
                            // HACK: Check for the new "DynamicCallback" marshal flag.
                            //       When set, allow the command callback to use any
                            //       delegate type.  This may fail (late-bound) when
                            //       the runtime tries to invoke it; however, this
                            //       feature is opt-in so it's not overly critical.
                            //
                            // HACK: Always pass "true" for the useDynamicCallback
                            //       parameter to the IsSupportedDelegateType method
                            //       here.  This allows a DynamicInvokeCallback type
                            //       compatible delegate type to be matched with it
                            //       (i.e. the delegate type would return an object
                            //       and accept zero or more object parameters).
                            //
                            bool isDelegate;

                            if (IsSupportedDelegateType(
                                    type, useDelegateCallback, useGenericCallback,
                                    true, out isDelegate) || useDynamicCallback ||
                                useSimpleCallback)
                            {
                                //
                                // NOTE: Any command callback *MUST* be specified as a
                                //       valid list.  Also, as of Beta 34, the command
                                //       itself *MAY* be preceded by the options that
                                //       are returned from GetCallbackOptions.
                                //
                                StringList list = null;

                                if (ParserOps<string>.SplitList(
                                        interpreter, text, 0, Length.Invalid, true,
                                        ref list, ref error) == ReturnCode.Ok)
                                {
                                    //
                                    // NOTE: If this flag is set, do not attempt to
                                    //       parse -OR- process any options.
                                    //
                                    bool noCallbackOptions = FlagOps.HasFlags(
                                        marshalFlags, MarshalFlags.NoCallbackOptions,
                                        true);

                                    //
                                    // NOTE: If this flag is set, ignore any options
                                    //       that may be set (will still be parsed).
                                    //
                                    bool ignoreCallbackOptions = FlagOps.HasFlags(
                                        marshalFlags, MarshalFlags.IgnoreCallbackOptions,
                                        true);

                                    OptionDictionary localOptions = null;
                                    ArgumentList arguments = null;

                                    if (!noCallbackOptions)
                                    {
                                        localOptions = CommandOptions.GetCommandOptions(
                                            useSimpleCallback ?
                                                CommandOptionType.Object_SimpleCallback :
                                                CommandOptionType.Object_Callback);
                                    }

                                    if (!noCallbackOptions)
                                        arguments = new ArgumentList(list);

                                    int argumentIndex = Index.Invalid;

                                    if (noCallbackOptions || (interpreter == null) ||
                                        (interpreter.GetOptions(localOptions, arguments,
                                            0, 0, Index.Invalid, false, ref argumentIndex,
                                            ref error) == ReturnCode.Ok))
                                    {
                                        if (noCallbackOptions || (interpreter == null) ||
                                            (argumentIndex != Index.Invalid))
                                        {
                                            StringList newList = (argumentIndex != Index.Invalid) ?
                                                new StringList((IList<string>)list, argumentIndex) :
                                                list;

                                            //
                                            // NOTE: If we succeed, make sure the callback has
                                            //       updated marshal flags preventing future
                                            //       (superfluous) errors.
                                            //
                                            MarshalFlags defaultMarshalFlags = marshalFlags |
                                                MarshalFlags.SkipChangeType | (isDelegate ?
                                                MarshalFlags.SkipReferenceTypeCheck :
                                                MarshalFlags.None);

                                            MarshalFlags newMarshalFlags;
                                            bool throwOnBindFailure;

                                            if (useSimpleCallback)
                                            {
                                                BindingFlags defaultBindingFlags =
                                                    ObjectOps.GetBindingFlags(
                                                        MetaBindingFlags.ObjectDefault, true);

                                                BindingFlags bindingFlags;

                                                if (noCallbackOptions || ignoreCallbackOptions)
                                                {
                                                    newMarshalFlags = defaultMarshalFlags;
                                                    bindingFlags = defaultBindingFlags;
                                                }
                                                else
                                                {
                                                    ObjectOps.ProcessSimpleCallbackOptions(
                                                        interpreter, localOptions, defaultBindingFlags,
                                                        defaultMarshalFlags, out bindingFlags,
                                                        out newMarshalFlags);
                                                }

                                                //
                                                // NOTE: If this flag is set, delegate binding errors
                                                //       will cause an exception to be thrown (and
                                                //       later caught by this method).
                                                //
                                                throwOnBindFailure = FlagOps.HasFlags(
                                                    newMarshalFlags, MarshalFlags.ThrowOnBindFailure,
                                                    true);

                                                if (!FlagOps.HasFlags(newMarshalFlags,
                                                        MarshalFlags.SimpleCallbackErrorMask, false))
                                                {
                                                    if (FlagOps.HasFlags(newMarshalFlags,
                                                            MarshalFlags.SimpleCallbackWarningMask, false))
                                                    {
                                                        TraceOps.DebugTrace(String.Format(
                                                            "ToCommandCallback: superfluous marshal flags " +
                                                            "{0} for simple callback {1} of type {2}",
                                                            FormatOps.WrapOrNull(newMarshalFlags &
                                                                MarshalFlags.SimpleCallbackWarningMask),
                                                            FormatOps.WrapOrNull(text),
                                                            FormatOps.TypeName(type)),
                                                            typeof(ConversionOps).Name,
                                                            TracePriority.MarshalWarning);
                                                    }

                                                    Delegate @delegate = null;

                                                    if (MarshalOps.LookupSimpleCallback(
                                                            interpreter, type, newList, cultureInfo,
                                                            bindingFlags, throwOnBindFailure,
                                                            ref @delegate, ref error) == ReturnCode.Ok)
                                                    {
                                                        value = @delegate;
                                                        return ReturnCode.Ok;
                                                    }
                                                }
                                                else
                                                {
                                                    error = String.Format(
                                                        "bad marshal flags {0} for simple callback {1} of type {2}",
                                                        FormatOps.WrapOrNull(newMarshalFlags &
                                                            MarshalFlags.SimpleCallbackErrorMask),
                                                        FormatOps.WrapOrNull(text), FormatOps.TypeName(type));
                                                }
                                            }
                                            else
                                            {
                                                Type returnType;
                                                TypeList parameterTypes;
                                                MarshalFlagsList parameterMarshalFlags;
                                                ObjectFlags objectFlags;
                                                ByRefArgumentFlags byRefArgumentFlags;
                                                CallbackFlags callbackFlags;

                                                if (noCallbackOptions || ignoreCallbackOptions)
                                                {
                                                    returnType = null;
                                                    parameterTypes = null;
                                                    parameterMarshalFlags = null;
                                                    newMarshalFlags = defaultMarshalFlags;
                                                    byRefArgumentFlags = ByRefArgumentFlags.None;
                                                    objectFlags = ObjectFlags.Callback;
                                                    callbackFlags = CallbackFlags.Default;
                                                }
                                                else
                                                {
                                                    ObjectOps.ProcessCallbackOptions(
                                                        interpreter, localOptions,
                                                        defaultMarshalFlags, ObjectFlags.Callback,
                                                        null, null, out returnType,
                                                        out parameterTypes, out parameterMarshalFlags,
                                                        out newMarshalFlags, out objectFlags,
                                                        out byRefArgumentFlags, out callbackFlags);
                                                }

                                                //
                                                // NOTE: If this flag is set, delegate binding errors
                                                //       will cause an exception to be thrown (and
                                                //       later caught by this method).
                                                //
                                                throwOnBindFailure = FlagOps.HasFlags(
                                                    newMarshalFlags, MarshalFlags.ThrowOnBindFailure,
                                                    true);

                                                //
                                                // NOTE: Create a command callback object to handle
                                                //       the incoming callbacks.
                                                //
                                                ICallback callback = CommandCallback.Create(
                                                    newMarshalFlags, callbackFlags, objectFlags,
                                                    byRefArgumentFlags, interpreter, ClientData.Empty,
                                                    list.ToString(), newList, ref error);

                                                if (callback != null)
                                                {
                                                    try
                                                    {
                                                        Delegate @delegate = callback.GetDelegate(
                                                            type, returnType, parameterTypes,
                                                            parameterMarshalFlags, marshalFlags,
                                                            ref error); /* throw */

                                                        if (@delegate != null)
                                                        {
                                                            object newValue;

                                                            if (FlagOps.HasFlags(newMarshalFlags,
                                                                    MarshalFlags.ReturnICallback,
                                                                    true))
                                                            {
                                                                newMarshalFlags &=
                                                                    ~MarshalFlags.ReturnICallback;

                                                                newValue = callback;
                                                            }
                                                            else
                                                            {
                                                                newValue = @delegate;
                                                            }

                                                            marshalFlags = newMarshalFlags;
                                                            value = newValue;

                                                            return ReturnCode.Ok;
                                                        }
                                                    }
                                                    catch (Exception e)
                                                    {
                                                        error = e;
                                                    }
                                                }
                                            }
                                        }
                                        else
                                        {
                                            error = "wrong # args: should be \"?options? arg ?arg ...?\"";
                                        }
                                    }
                                }
                            }
                            else
                            {
                                error = ScriptOps.BadValue(
                                    "unsupported", "delegate type",
                                    FormatOps.RawTypeName(type), new string[] {
                                        useDelegateCallback ?
                                            typeof(Delegate).FullName : null,
                                        typeof(AsyncCallback).FullName,
                                        typeof(EventHandler).FullName,
                                        typeof(ThreadStart).FullName,
                                        typeof(ParameterizedThreadStart).FullName,
                                        useGenericCallback ?
                                            typeof(GenericCallback).FullName : null,
                                        useDynamicCallback ?
                                            typeof(DynamicInvokeCallback).FullName : null
                                    }, null, null);
                            }
                        }
                        else
                        {
                            error = String.Format(
                                "type {0} is not a delegate",
                                FormatOps.TypeName(type));
                        }
                    }
                    else
                    {
                        error = "invalid type";
                    }

                    return ReturnCode.Error;
                }
            }
            #endregion
        }
        #endregion
    }
}
