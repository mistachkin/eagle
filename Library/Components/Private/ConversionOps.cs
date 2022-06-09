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
    [ObjectId("d93666f3-561b-4257-aaf7-8fd5a5436de9")]
    internal static class ConversionOps
    {
        #region Private Constants
        public static readonly int ByteBits = ToInt(MathOps.Log2(byte.MaxValue)) + 1;
        private static readonly int ShortBits = ToInt(MathOps.Log2(ushort.MaxValue)) + 1;
        public static readonly int IntBits = ToInt(MathOps.Log2(uint.MaxValue)) + 1;
        public static readonly int LongBits = ToInt(MathOps.Log2(ulong.MaxValue)) + 1;

        ///////////////////////////////////////////////////////////////////////////////////////

        private static readonly int TwoByteBits = ByteBits * 2;
        private static readonly int FourByteBits = ByteBits * 4;
        private static readonly int SixByteBits = ByteBits * 6;

        ///////////////////////////////////////////////////////////////////////////////////////

        private const char CharHighByte = (char)0xFF00;
        private const char CharLowByte = (char)byte.MaxValue;

        ///////////////////////////////////////////////////////////////////////////////////////

        private const int IntHighShort = unchecked((int)0xFFFF0000);
        private const int IntLowShort = (int)ushort.MaxValue;

        ///////////////////////////////////////////////////////////////////////////////////////

        private const long LongHighShort = unchecked((long)0xFFFF000000000000);
        private const long LongHighMidShort = 0xFFFF00000000;
        private const long LongLowMidShort = 0xFFFF0000;
        private const long LongLowShort = 0xFFFF;

        ///////////////////////////////////////////////////////////////////////////////////////

        private const long LongHighInt = unchecked((long)0xFFFFFFFF00000000);
        private const long LongLowInt = (long)uint.MaxValue;

        ///////////////////////////////////////////////////////////////////////////////////////

        private const string EnableString = "enable";
        private const string DisableString = "disable";

        private const string EnabledString = "enabled";
        private const string DisabledString = "disabled";
        #endregion

        ///////////////////////////////////////////////////////////////////////////////////////

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

            if (useGenericCallback && LooksLikeGenericCallback(type))
                return true;

            if (useDynamicCallback && LooksLikeDynamicInvokeCallback(type))
                return true;

            return false;
        }

        ///////////////////////////////////////////////////////////////////////////////

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
        [ObjectId("a88d72f9-b067-44ec-9457-b6e8000cf378")]
        private sealed class DelegateMethods
        {
            #region Public Constructors
            public DelegateMethods()
            {
                // do nothing.
            }
            #endregion

            ///////////////////////////////////////////////////////////////////////////////////

            #region Public "Delegate" Methods
            /* System.AsyncCallback */
            public void NullAsyncCallback(
                IAsyncResult ar
                )
            {
                // do nothing.
            }

            ///////////////////////////////////////////////////////////////////////////////////

            /* System.EventHandler */
            public void NullEventHandler(
                object sender,
                EventArgs e
                )
            {
                // do nothing.
            }

            ///////////////////////////////////////////////////////////////////////////////////

            /* System.Threading.ThreadStart */
            public void NullThreadStart()
            {
                // do nothing.
            }

            ///////////////////////////////////////////////////////////////////////////////////

            /* System.Threading.ParameterizedThreadStart */
            public void NullParameterizedThreadStart(
                object obj
                )
            {
                // do nothing.
            }

            ///////////////////////////////////////////////////////////////////////////////////

            /* Eagle._Components.Public.Delegates.GenericCallback */
            public void NullGenericCallback()
            {
                // do nothing.
            }

            ///////////////////////////////////////////////////////////////////////////////////

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
        public static bool IsDelegate(Type type)
        {
            //
            // NOTE: Must simply be the actual "System.Delegate" type.
            //
            return IsDelegateType(type, true);
        }

        ///////////////////////////////////////////////////////////////////////////////////////

        private static bool IsAsyncCallback(Type type)
        {
            return type == typeof(AsyncCallback);
        }

        ///////////////////////////////////////////////////////////////////////////////////////

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

        private static bool IsEventHandler(Type type)
        {
            return type == typeof(EventHandler);
        }

        ///////////////////////////////////////////////////////////////////////////////////////

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

        public static bool IsThreadStart(Type type)
        {
            return type == typeof(ThreadStart);
        }

        ///////////////////////////////////////////////////////////////////////////////////////

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

        public static bool IsParameterizedThreadStart(Type type)
        {
            return type == typeof(ParameterizedThreadStart);
        }

        ///////////////////////////////////////////////////////////////////////////////////////

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

        private static bool IsGenericCallback(Type type)
        {
            return type == typeof(GenericCallback);
        }

        ///////////////////////////////////////////////////////////////////////////////////////

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

        private static bool IsDynamicInvokeCallback(Type type)
        {
            return type == typeof(DynamicInvokeCallback);
        }

        ///////////////////////////////////////////////////////////////////////////////////////

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

        public static long MakeLong(long X, long Y) /* LOSSY */
        {
            return ((X & LongLowInt) << IntBits) | (Y & LongLowInt);
        }

        ///////////////////////////////////////////////////////////////////////////////////////

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

        public static int FlipEndian(int X) /* SAFE */
        {
            byte[] bytes = BitConverter.GetBytes(X);

            Array.Reverse(bytes);

            return BitConverter.ToInt32(bytes, 0);
        }

        ///////////////////////////////////////////////////////////////////////////////////////

        public static uint FlipEndian(uint X) /* SAFE */
        {
            byte[] bytes = BitConverter.GetBytes(X);

            Array.Reverse(bytes);

            return BitConverter.ToUInt32(bytes, 0);
        }

        ///////////////////////////////////////////////////////////////////////////////////////

        public static long FlipEndian(long X) /* SAFE */
        {
            byte[] bytes = BitConverter.GetBytes(X);

            Array.Reverse(bytes);

            return BitConverter.ToInt64(bytes, 0);
        }

        ///////////////////////////////////////////////////////////////////////////////////////

        public static ulong FlipEndian(ulong X) /* SAFE */
        {
            byte[] bytes = BitConverter.GetBytes(X);

            Array.Reverse(bytes);

            return BitConverter.ToUInt64(bytes, 0);
        }

        ///////////////////////////////////////////////////////////////////////////////////////

        public static uint Negate(uint X) /* SAFE */
        {
            return unchecked((uint)(-(int)X));
        }

        ///////////////////////////////////////////////////////////////////////////////////////

        #region Dead Code
#if DEAD_CODE
        private static byte Negate(byte X) /* SAFE */
        {
            return unchecked((byte)(-(sbyte)X));
        }

        ///////////////////////////////////////////////////////////////////////////////////////

        private static ushort Negate(ushort X) /* SAFE */
        {
            return unchecked((ushort)(-(short)X));
        }

        ///////////////////////////////////////////////////////////////////////////////////////

        private static ulong Negate(ulong X) /* SAFE */
        {
            return unchecked((ulong)(-(long)X));
        }
#endif
        #endregion

        ///////////////////////////////////////////////////////////////////////////////////////

        public static bool ToBool(_Public.Boolean X) /* SAFE */
        {
            return X != _Public.Boolean.False ? true : false;
        }

        ///////////////////////////////////////////////////////////////////////////////////////

        public static bool ToBool(int X) /* LOSSY */
        {
            return X != 0 ? true : false;
        }

        ///////////////////////////////////////////////////////////////////////////////////////

        public static bool ToBool(uint X) /* LOSSY */
        {
            return X != 0 ? true : false;
        }

        ///////////////////////////////////////////////////////////////////////////////////////

        public static bool ToBool(long X) /* LOSSY */
        {
            return X != 0 ? true : false;
        }

        ///////////////////////////////////////////////////////////////////////////////////////

        #region Dead Code
#if DEAD_CODE
        private static bool ToBool(ReturnCode X) /* LOSSY */
        {
            return X == ReturnCode.Ok ? true : false;
        }
#endif
        #endregion

        ///////////////////////////////////////////////////////////////////////////////////////

        public static bool ToBool(DateTime X) /* LOSSY */
        {
            return X.Ticks != 0 ? true : false;
        }

        ///////////////////////////////////////////////////////////////////////////////////////

        public static bool ToBool(decimal X) /* LOSSY */
        {
            return X != 0 ? true : false;
        }

        ///////////////////////////////////////////////////////////////////////////////////////

        public static bool ToBool(double X) /* LOSSY */
        {
            return X != 0 ? true : false;
        }

        ///////////////////////////////////////////////////////////////////////////////////////

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

        public static sbyte ToSByte(byte X) /* SAFE */
        {
            return unchecked((sbyte)X);
        }

        ///////////////////////////////////////////////////////////////////////////////////////

        public static sbyte ToSByte(int X) /* LOSSY */
        {
            return unchecked((sbyte)(X & byte.MaxValue));
        }

        ///////////////////////////////////////////////////////////////////////////////////////

        public static sbyte ToSByte(long X) /* LOSSY */
        {
            return unchecked((sbyte)(X & byte.MaxValue));
        }

        ///////////////////////////////////////////////////////////////////////////////////////

        public static byte ToByte(char X) /* LOSSY */
        {
            return ToLowByte(X);
        }

        ///////////////////////////////////////////////////////////////////////////////////////

        public static byte ToLowByte(char X) /* LOSSY */
        {
            return (byte)(X & CharLowByte);
        }

        ///////////////////////////////////////////////////////////////////////////////////////

        public static byte ToHighByte(char X) /* LOSSY */
        {
            return (byte)((X & CharHighByte) >> ByteBits);
        }

        ///////////////////////////////////////////////////////////////////////////////////////

        public static byte ToByte(sbyte X) /* SAFE */
        {
            return unchecked((byte)X);
        }

        ///////////////////////////////////////////////////////////////////////////////////////

        public static byte ToByte(int X) /* LOSSY */
        {
            return (byte)(X & byte.MaxValue);
        }

        ///////////////////////////////////////////////////////////////////////////////////////

        public static byte ToByte(long X) /* LOSSY */
        {
            return (byte)(X & byte.MaxValue);
        }

        ///////////////////////////////////////////////////////////////////////////////////////

        public static char ToChar(byte X) /* SAFE */
        {
            return (char)X;
        }

        ///////////////////////////////////////////////////////////////////////////////////////

        public static char ToChar(byte X, byte Y) /* SAFE, LITTLE-ENDIAN */
        {
            return (char)(X | (Y << ByteBits));
        }

        ///////////////////////////////////////////////////////////////////////////////////////

        public static char ToChar(int X) /* LOSSY */
        {
            return (char)(X & char.MaxValue);
        }

        ///////////////////////////////////////////////////////////////////////////////////////

        public static char ToChar(long X) /* LOSSY */
        {
            return (char)(X & char.MaxValue);
        }

        ///////////////////////////////////////////////////////////////////////////////////////

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

        /* NOTE: For use by the Parser.ParseBackslash method only. */
        public static void ToChars(long X, ref char? Y, ref char? Z) /* LOSSY */
        {
            ToChars(ToInt(X), ref Y, ref Z);
        }

        ///////////////////////////////////////////////////////////////////////////////////////

        public static short ToShort(long X) /* LOSSY */
        {
            return unchecked((short)(X & ushort.MaxValue));
        }

        ///////////////////////////////////////////////////////////////////////////////////////

        private static short ToShort(ushort X) /* LOSSY */
        {
            return unchecked((short)X);
        }

        ///////////////////////////////////////////////////////////////////////////////////////

        #region Dead Code
#if DEAD_CODE
        private static ushort ToUShort(short X) /* SAFE */
        {
            return unchecked((ushort)X);
        }
#endif
        #endregion

        ///////////////////////////////////////////////////////////////////////////////////////

        public static ushort ToUShort(long X) /* LOSSY */
        {
            return unchecked((ushort)(X & ushort.MaxValue));
        }

        ///////////////////////////////////////////////////////////////////////////////////////

        public static int ToInt(bool X) /* SAFE */
        {
            return X ? 1 : 0;
        }

        ///////////////////////////////////////////////////////////////////////////////////////

        public static int ToInt(char X) /* SAFE */
        {
            return X;
        }

        ///////////////////////////////////////////////////////////////////////////////////////

        public static int ToInt(ReturnCode X) /* SAFE */
        {
            return (int)X;
        }

        ///////////////////////////////////////////////////////////////////////////////////////

        public static int ToInt(uint X) /* SAFE */
        {
            return unchecked((int)X);
        }

        ///////////////////////////////////////////////////////////////////////////////////////

        public static int ToInt(long X) /* LOSSY */
        {
            return (int)(X & uint.MaxValue);
        }

        ///////////////////////////////////////////////////////////////////////////////////////

        public static int ToInt(ulong X) /* LOSSY */
        {
            return (int)(X & uint.MaxValue);
        }

        ///////////////////////////////////////////////////////////////////////////////////////

        public static int ToInt(IntPtr X) /* LOSSY */
        {
            return ToInt(X.ToInt64());
        }

        ///////////////////////////////////////////////////////////////////////////////////////

        public static int ToInt(UIntPtr X) /* LOSSY */
        {
            return ToInt(X.ToUInt64());
        }

        ///////////////////////////////////////////////////////////////////////////////////////

        public static int ToInt(DateTime X) /* LOSSY */
        {
            return (int)(X.Ticks & uint.MaxValue);
        }

        ///////////////////////////////////////////////////////////////////////////////////////

        public static void ToInts(long X, ref int Y, ref int Z) /* SAFE */
        {
            Y = (int)(X & LongLowInt);
            Z = (int)((X & LongHighInt) >> IntBits);
        }

        ///////////////////////////////////////////////////////////////////////////////////////

        public static uint ToUInt(int X) /* SAFE */
        {
            return unchecked((uint)X);
        }

        ///////////////////////////////////////////////////////////////////////////////////////

        public static uint ToUInt(long X) /* LOSSY */
        {
            return (uint)(X & uint.MaxValue);
        }

        ///////////////////////////////////////////////////////////////////////////////////////

        public static uint ToUInt(ulong X) /* LOSSY */
        {
            return (uint)(X & uint.MaxValue);
        }

        ///////////////////////////////////////////////////////////////////////////////////////

        public static uint ToUInt(IntPtr X) /* LOSSY */
        {
            return ToUInt(X.ToInt64());
        }

        ///////////////////////////////////////////////////////////////////////////////////////

        public static uint ToUInt(UIntPtr X) /* LOSSY */
        {
            return ToUInt(X.ToUInt64());
        }

        ///////////////////////////////////////////////////////////////////////////////////////

        public static long ToLong(bool X) /* SAFE */
        {
            return X ? 1 : 0;
        }

        ///////////////////////////////////////////////////////////////////////////////////////

        public static long ToLong(int X) /* SAFE */
        {
            return X;
        }

        ///////////////////////////////////////////////////////////////////////////////////////

        public static long ToLong(DateTime X) /* SAFE */
        {
            return X.Ticks;
        }

        ///////////////////////////////////////////////////////////////////////////////////////

        public static long ToLong(ulong X) /* SAFE */
        {
            return unchecked((long)X);
        }

        ///////////////////////////////////////////////////////////////////////////////////////

        public static long ToLong(double X) /* LOSSY */
        {
            return unchecked((long)X);
        }

        ///////////////////////////////////////////////////////////////////////////////////////

        #region Dead Code
#if DEAD_CODE
        private static long ToLongBits(double X) /* SAFE */
        {
            return BitConverter.DoubleToInt64Bits(X);
        }

        ///////////////////////////////////////////////////////////////////////////////////////

        private static IntPtr ToIntPtr(uint X) /* SAFE */
        {
            return new IntPtr(unchecked((int)X));
        }

        ///////////////////////////////////////////////////////////////////////////////////////

        private static IntPtr ToIntPtr(ulong X) /* SAFE */
        {
            return new IntPtr(unchecked((long)X));
        }
#endif
        #endregion

        ///////////////////////////////////////////////////////////////////////////////////////

        public static IntPtr ToIntPtr(UIntPtr X) /* SAFE */
        {
            // NOTE: Easy way.
            // unsafe { return new IntPtr(X.ToPointer()); }

            // NOTE: Hard way.
            return new IntPtr(unchecked((long)X.ToUInt64()));
        }

        ///////////////////////////////////////////////////////////////////////////////////////

        public static ulong ToULong(bool X) /* SAFE */
        {
            return X ? (ulong)1 : 0;
        }

        ///////////////////////////////////////////////////////////////////////////////////////

        public static ulong ToULong(sbyte X) /* SAFE */
        {
            return unchecked((ulong)(byte)X);
        }

        ///////////////////////////////////////////////////////////////////////////////////////

        public static ulong ToULong(short X) /* SAFE */
        {
            return unchecked((ulong)(ushort)X);
        }

        ///////////////////////////////////////////////////////////////////////////////////////

        public static ulong ToULong(int X) /* SAFE */
        {
            return unchecked((ulong)(uint)X);
        }

        ///////////////////////////////////////////////////////////////////////////////////////

        public static ulong ToULong(long X) /* SAFE */
        {
            return unchecked((ulong)X);
        }

        ///////////////////////////////////////////////////////////////////////////////////////

        public static ulong ToULong(uint X, uint Y) /* SAFE */
        {
            return (ulong)(X | ((ulong)Y << IntBits));
        }

        ///////////////////////////////////////////////////////////////////////////////////////

        public static UIntPtr ToUIntPtr(int X) /* SAFE */
        {
            return new UIntPtr(unchecked((uint)X));
        }

        ///////////////////////////////////////////////////////////////////////////////////////

        public static UIntPtr ToUIntPtr(long X) /* SAFE */
        {
            return new UIntPtr(unchecked((ulong)X));
        }

        ///////////////////////////////////////////////////////////////////////////////////////

        public static UIntPtr ToUIntPtr(IntPtr X) /* SAFE */
        {
            // NOTE: Easy way.
            // unsafe { return new UIntPtr(X.ToPointer()); }

            // NOTE: Hard way.
            return new UIntPtr(unchecked((ulong)X.ToInt64()));
        }

        ///////////////////////////////////////////////////////////////////////////////////////

        public static decimal ToDecimal(bool X) /* SAFE */
        {
            return X ? 1 : 0;
        }

        ///////////////////////////////////////////////////////////////////////////////////////

        public static decimal ToDecimal(int X) /* SAFE */
        {
            return X;
        }

        ///////////////////////////////////////////////////////////////////////////////////////

        public static decimal ToDecimal(long X) /* SAFE */
        {
            return X;
        }

        ///////////////////////////////////////////////////////////////////////////////////////

        public static decimal ToDecimal(DateTime X) /* SAFE */
        {
            return X.Ticks;
        }

        ///////////////////////////////////////////////////////////////////////////////////////

        public static double ToDouble(bool X) /* SAFE */
        {
            return X ? 1 : 0;
        }

        ///////////////////////////////////////////////////////////////////////////////////////

        public static double ToDouble(int X) /* SAFE */
        {
            return X;
        }

        ///////////////////////////////////////////////////////////////////////////////////////

        public static double ToDouble(long X) /* SAFE */
        {
            return X;
        }

        ///////////////////////////////////////////////////////////////////////////////////////

        public static double ToDouble(DateTime X) /* LOSSY */
        {
            return X.Ticks;
        }

        ///////////////////////////////////////////////////////////////////////////////////////

        public static DateTime ToDateTime(bool X) /* SAFE */
        {
            return ToDateTime(X, ObjectOps.GetDefaultDateTimeKind());
        }

        ///////////////////////////////////////////////////////////////////////////////////////

        public static DateTime ToDateTime(bool X, DateTimeKind kind) /* SAFE */
        {
            return DateTime.SpecifyKind(new DateTime(X ? 1 : 0), kind);
        }

        ///////////////////////////////////////////////////////////////////////////////////////

        public static DateTime ToDateTime(int X) /* SAFE */
        {
            return ToDateTime(X, ObjectOps.GetDefaultDateTimeKind());
        }

        ///////////////////////////////////////////////////////////////////////////////////////

        public static DateTime ToDateTime(int X, DateTimeKind kind) /* SAFE */
        {
            return DateTime.SpecifyKind(new DateTime(X), kind);
        }

        ///////////////////////////////////////////////////////////////////////////////////////

        public static DateTime ToDateTime(long X) /* LOSSY */
        {
            return ToDateTime(X, ObjectOps.GetDefaultDateTimeKind());
        }

        ///////////////////////////////////////////////////////////////////////////////////////

        public static DateTime ToDateTime(long X, DateTimeKind kind) /* LOSSY */
        {
            //
            // NOTE: Limited to 0x2BCA2875F4373FFF ticks (not even close to
            //       the full range of long integers).
            //
            return DateTime.SpecifyKind(new DateTime(X), kind);
        }

        ///////////////////////////////////////////////////////////////////////////////////////

        public static DateTime ToDateTime(double X) /* LOSSY */
        {
            return ToDateTime(X, ObjectOps.GetDefaultDateTimeKind());
        }

        ///////////////////////////////////////////////////////////////////////////////////////

        public static DateTime ToDateTime(double X, DateTimeKind kind) /* LOSSY */
        {
            //
            // NOTE: Limited to 0x2BCA2875F4373FFF ticks (not even close to
            //       the full range of long integers).
            //
            return DateTime.SpecifyKind(new DateTime(BitConverter.DoubleToInt64Bits(X)), kind);
        }

        ///////////////////////////////////////////////////////////////////////////////////////

        public static TimeSpan ToTimeSpan(bool X) /* SAFE */
        {
            return new TimeSpan(X ? 1 : 0);
        }

        ///////////////////////////////////////////////////////////////////////////////////////

        public static TimeSpan ToTimeSpan(int X) /* SAFE */
        {
            return new TimeSpan(X);
        }

        ///////////////////////////////////////////////////////////////////////////////////////

        public static TimeSpan ToTimeSpan(long X) /* SAFE */
        {
            return new TimeSpan(X);
        }

        ///////////////////////////////////////////////////////////////////////////////////////

        public static TimeSpan ToTimeSpan(double X) /* LOSSY (?) */
        {
            return new TimeSpan(BitConverter.DoubleToInt64Bits(X));
        }

        ///////////////////////////////////////////////////////////////////////////////////////

        public static string ToEnable(bool X)
        {
            return X ? EnableString : DisableString;
        }

        ///////////////////////////////////////////////////////////////////////////////////////

        public static string ToEnabled(bool X)
        {
            return X ? EnabledString : DisabledString;
        }

        ///////////////////////////////////////////////////////////////////////////////////////

        #region Dynamic Conversion Class
        [ObjectId("8264f2fc-42c9-4892-b152-a6368115c4a7")]
        internal static class Dynamic
        {
            //
            // NOTE: What dynamic ChangeType conversions do we support?
            //
            public static readonly TypeChangeTypeCallbackDictionary ChangeTypes =
                ChangeType.PopulateCallbackTable();

            //
            // NOTE: What dynamic ToString type conversions do we support?
            //
            public static readonly TypeToStringCallbackDictionary ToStringTypes =
                _ToString.PopulateCallbackTable();

            ///////////////////////////////////////////////////////////////////////////////////

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
            [ObjectId("ceeb671d-b4b4-4607-b96e-da5867b011d2")]
            internal static class _ToString
            {
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

                private static ReturnCode FromDateTime(
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
            [ObjectId("96b36848-67b3-4c66-bccf-39f3726f57d0")]
            internal static class ChangeType
            {
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
                    result.Add(typeof(Number), ToNumber);
                    result.Add(typeof(Variant), ToVariant);

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
                    result.Add(typeof(Number).MakeByRefType(), ToNumber);
                    result.Add(typeof(Variant).MakeByRefType(), ToVariant);

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

                private static ReturnCode ToNumber(
                    Interpreter interpreter, /* NOT USED */
                    Type type, /* NOT USED */
                    string text,
                    OptionDictionary options, /* NOT USED */
                    CultureInfo cultureInfo,
                    IClientData clientData, /* NOT USED */
                    ref MarshalFlags marshalFlags, /* NOT USED */
                    ref object value, /* Eagle._Components.Public.Number */
                    ref Result error
                    )
                {
                    Number numberValue = null;

                    if (Value.GetNumber(
                            text, ValueFlags.AnyNumberAnyRadix, cultureInfo,
                            ref numberValue, ref error) == ReturnCode.Ok)
                    {
                        value = numberValue;
                        return ReturnCode.Ok;
                    }

                    return ReturnCode.Error;
                }

                ///////////////////////////////////////////////////////////////////////////////

                private static ReturnCode ToVariant(
                    Interpreter interpreter, /* NOT USED */
                    Type type, /* NOT USED */
                    string text,
                    OptionDictionary options, /* NOT USED */
                    CultureInfo cultureInfo,
                    IClientData clientData, /* NOT USED */
                    ref MarshalFlags marshalFlags, /* NOT USED */
                    ref object value, /* Eagle._Components.Public.Variant */
                    ref Result error
                    )
                {
                    Variant variantValue = null;

                    if (Value.GetVariant(
                            interpreter, text, ValueFlags.AnyVariant,
                            cultureInfo, ref variantValue,
                            ref error) == ReturnCode.Ok)
                    {
                        value = variantValue;
                        return ReturnCode.Ok;
                    }

                    return ReturnCode.Error;
                }

                ///////////////////////////////////////////////////////////////////////////////

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
                                        localOptions = useSimpleCallback ?
                                            ObjectOps.GetSimpleCallbackOptions() :
                                            ObjectOps.GetCallbackOptions();
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
                                                            parameterMarshalFlags, throwOnBindFailure,
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
