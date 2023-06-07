/*
 * MathOps.cs --
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
using Eagle._Attributes;
using Eagle._Components.Public;
using Eagle._Constants;

namespace Eagle._Components.Private
{
    [ObjectId("4d43cec5-9a8c-4b0e-b47d-002c28de623f")]
    internal static class MathOps
    {
        #region Private Constants
        private const int HalfInt32MinValue = int.MinValue / 2;
        private const int HalfInt32MaxValue = int.MaxValue / 2;

        ///////////////////////////////////////////////////////////////////////////////////////////////

        private const uint FnvOffsetBasis32 = 2166136261;
        private const uint FnvPrime32 = 16777619;

        ///////////////////////////////////////////////////////////////////////////////////////////////

        private const ulong FnvOffsetBasis64 = 14695981039346656037;
        private const ulong FnvPrime64 = 1099511628211;

        ///////////////////////////////////////////////////////////////////////////////////////////////

        private const int DoubleExponentShift = 52;
        private const int DoubleExponentBits = 11;
        private const long DoubleExponentMask = 0x7FF;

        ///////////////////////////////////////////////////////////////////////////////////////////////

        //
        // HACK: These are purposely not read-only.
        //
        private static double DoubleEpsilon = 0.00001;
        private static decimal DecimalEpsilon = 0.00001m;

        ///////////////////////////////////////////////////////////////////////////////////////////////

        private static readonly ulong[] PowersOfTwo = {
            /*  0 */ 1,
            /*  1 */ 2,
            /*  2 */ 4,
            /*  3 */ 8,
            /*  4 */ 16,
            /*  5 */ 32,
            /*  6 */ 64,
            /*  7 */ 128,
            /*  8 */ 256,
            /*  9 */ 512,
            /* 10 */ 1024,
            /* 11 */ 2048,
            /* 12 */ 4096,
            /* 13 */ 8192,
            /* 14 */ 16384,
            /* 15 */ 32768,
            /* 16 */ 65536,
            /* 17 */ 131072,
            /* 18 */ 262144,
            /* 19 */ 524288,
            /* 20 */ 1048576,
            /* 21 */ 2097152,
            /* 22 */ 4194304,
            /* 23 */ 8388608,
            /* 24 */ 16777216,
            /* 25 */ 33554432,
            /* 26 */ 67108864,
            /* 27 */ 134217728,
            /* 28 */ 268435456,
            /* 29 */ 536870912,
            /* 30 */ 1073741824,
            /* 31 */ 2147483648,
            /* 32 */ 4294967296,
            /* 33 */ 8589934592,
            /* 34 */ 17179869184,
            /* 35 */ 34359738368,
            /* 36 */ 68719476736,
            /* 37 */ 137438953472,
            /* 38 */ 274877906944,
            /* 39 */ 549755813888,
            /* 40 */ 1099511627776,
            /* 41 */ 2199023255552,
            /* 42 */ 4398046511104,
            /* 43 */ 8796093022208,
            /* 44 */ 17592186044416,
            /* 45 */ 35184372088832,
            /* 46 */ 70368744177664,
            /* 47 */ 140737488355328,
            /* 48 */ 281474976710656,
            /* 49 */ 562949953421312,
            /* 50 */ 1125899906842624,
            /* 51 */ 2251799813685248,
            /* 52 */ 4503599627370496,
            /* 53 */ 9007199254740992,
            /* 54 */ 18014398509481984,
            /* 55 */ 36028797018963968,
            /* 56 */ 72057594037927936,
            /* 57 */ 144115188075855872,
            /* 58 */ 288230376151711744,
            /* 59 */ 576460752303423488,
            /* 60 */ 1152921504606846976,
            /* 61 */ 2305843009213693952,
            /* 62 */ 4611686018427387904,
            /* 63 */ 9223372036854775808
        };
        #endregion

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public static ulong? Pow2(int X)
        {
            if (PowersOfTwo == null)
                return null;

            int length = PowersOfTwo.Length;

            if ((X < 0) || (X >= length))
                return null;

            return PowersOfTwo[X];
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public static bool WithinMagnitudes(
            long X,       /* in */
            long Y,       /* in */
            int? minimum, /* in: OPTIONAL */
            int? maximum  /* in: OPTIONAL */
            )
        {
            int logX = Log10(X);
            int logY = Log10(Y);

            int difference = Math.Abs(logX - logY);

            if ((minimum != null) && (difference < (int)minimum))
                return false;

            if ((maximum != null) && (difference > (int)maximum))
                return false;

            return true;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public static bool AboutEquals(
            double X,
            double Y
            )
        {
            return AboutEquals(X, Y, DoubleEpsilon);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public static bool AboutEquals(
            double X,
            double Y,
            double epsilon
            )
        {
            if (double.IsNaN(X) || double.IsNaN(Y))
                return false;

            if (double.IsNegativeInfinity(X) || double.IsNegativeInfinity(Y))
                return double.IsNegativeInfinity(X) && double.IsNegativeInfinity(Y);

            if (double.IsPositiveInfinity(X) || double.IsPositiveInfinity(Y))
                return double.IsPositiveInfinity(X) && double.IsPositiveInfinity(Y);

            return Math.Abs(X - Y) < epsilon;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public static bool AboutEquals(
            decimal X,
            decimal Y
            )
        {
            return AboutEquals(X, Y, DecimalEpsilon);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public static bool AboutEquals(
            decimal X,
            decimal Y,
            decimal epsilon
            )
        {
            return Math.Abs(X - Y) < epsilon;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public static FloatingPointClass Classify(
            double value
            )
        {
            long bits = BitConverter.DoubleToInt64Bits(value);

            long exponent = (
                (bits >> DoubleExponentShift) & DoubleExponentMask
            );

            if (exponent == 0)
            {
                if ((bits << 1) != 0) /* discard sign bit */
                    return FloatingPointClass.SubNormal;
                else
                    return FloatingPointClass.Zero;
            }

            if (exponent == DoubleExponentMask)
            {
                if ((bits << (DoubleExponentBits + 1)) != 0)
                    return FloatingPointClass.NaN;
                else
                    return FloatingPointClass.Infinite;
            }

            return FloatingPointClass.Normal;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public static bool CanDouble(
            int value
            )
        {
            if ((value >= 0) && (value <= HalfInt32MaxValue))
                return true;

            if ((value < 0) && (value >= HalfInt32MinValue))
                return true;

            return false;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public static uint HashFnv1UInt(
            byte[] bytes,
            bool alternate
            )
        {
            if (bytes == null)
                return 0;

            int length = bytes.Length;
            uint result = FnvOffsetBasis32;

            if (length > 0)
            {
                if (alternate)
                {
                    for (int index = 0; index < length; index++)
                    {
                        result ^= bytes[index];
                        result = unchecked(result * FnvPrime32);
                    }
                }
                else
                {
                    for (int index = 0; index < length; index++)
                    {
                        result = unchecked(result * FnvPrime32);
                        result ^= bytes[index];
                    }
                }
            }

            return result;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public static ulong HashFnv1ULong(
            byte[] bytes,
            bool alternate
            )
        {
            if (bytes == null)
                return 0;

            int length = bytes.Length;
            ulong result = FnvOffsetBasis64;

            if (length > 0)
            {
                if (alternate)
                {
                    for (int index = 0; index < length; index++)
                    {
                        result ^= bytes[index];
                        result = unchecked(result * FnvPrime64);
                    }
                }
                else
                {
                    for (int index = 0; index < length; index++)
                    {
                        result = unchecked(result * FnvPrime64);
                        result ^= bytes[index];
                    }
                }
            }

            return result;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public static int LeftShift(int X, int Y)
        {
            //
            // NOTE: It seems that for non-wide integers, Tcl 8.4 treats
            //       all negative shift values as though they were the
            //       corresponding positive value (COMPAT: Tcl 8.4).
            //
            return (Y < ConversionOps.IntBits) ? X << Y : 0;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public static int RightShift(int X, int Y)
        {
            //
            // NOTE: It seems that for non-wide integers, Tcl 8.4 treats
            //       all negative shift values as though they were the
            //       corresponding positive value (COMPAT: Tcl 8.4).
            //
            return (Y < ConversionOps.IntBits) ? X >> Y : 0;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public static long LeftShift(long X, int Y)
        {
            //
            // NOTE: It seems that for wide integers, Tcl 8.4 returns zero
            //       for all negative shift values (COMPAT: Tcl 8.4).
            //
            return (Y >= 0) && (Y < ConversionOps.LongBits) ? X << Y : 0;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public static long RightShift(long X, int Y)
        {
            //
            // NOTE: It seems that for wide integers, Tcl 8.4 returns zero
            //       for all negative shift values (COMPAT: Tcl 8.4).
            //
            return (Y >= 0) && (Y < ConversionOps.LongBits) ? X >> Y : 0;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public static int LeftRotate(int X, int Y)
        {
            //
            // NOTE: Per MSDN, C# masks the high bits for us.
            //
            return ((X << Y) | (X >> (ConversionOps.IntBits - Y)));
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public static int RightRotate(int X, int Y)
        {
            //
            // NOTE: Per MSDN, C# masks the high bits for us.
            //
            return ((X >> Y) | (X << (ConversionOps.IntBits - Y)));
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public static long LeftRotate(long X, int Y)
        {
            //
            // NOTE: Per MSDN, C# masks the high bits for us.
            //
            return ((X << Y) | (X >> (ConversionOps.LongBits - Y)));
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public static long RightRotate(long X, int Y)
        {
            //
            // NOTE: Per MSDN, C# masks the high bits for us.
            //
            return ((X >> Y) | (X << (ConversionOps.LongBits - Y)));
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public static int Pow(int X, int Y)
        {
            int result;

            if (X == 1)
            {
                //
                // 1. One raised to any power is one.
                //
                result = 1;
            }
            else if (Y == 0)
            {
                //
                // 1. Any number raised to the power of zero is one (typically,
                //    includes zero, especially for integers).
                //
                result = 1;
            }
            else if (Y == 1)
            {
                //
                // 1. Any number raised to the power of one is itself.
                //
                result = X;
            }
            else if (Y < 0)
            {
                if (X == -1)
                {
                    //
                    // 1. Negative one raised to negative odd powers is negative one.
                    // 2. Negative one raised to negative even powers is one.
                    //
                    if ((Y & 1) != 0) // odd exponent?
                        result = -1;
                    else
                        result = 1;
                }
                else if (X == 0)
                {
                    //
                    // 1. Zero raised to negative powers is the same as attempting to
                    //    divide by zero.
                    //
                    throw new DivideByZeroException();
                }
                else
                {
                    //
                    // 1. Non-zero integers raised to negative powers is zero.
                    //
                    result = 0;
                }
            }
            else
            {
                //
                // 1. Zero raised to any positive non-zero power is itself.
                // 2. One raised to any positive non-zero power is itself.
                //
                result = X;

                //
                // 1. General case of using repeated integer multiplication.  This may
                //    raise an overflow exception.
                //
                while ((result != 0) && (result != 1) && (--Y > 0))
                    result *= X;
            }

            return result;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public static long Pow(long X, long Y)
        {
            long result;

            if (X == 1)
            {
                //
                // 1. One raised to any power is one.
                //
                result = 1;
            }
            else if (Y == 0)
            {
                //
                // 1. Any number raised to the power of zero is one (typically,
                //    includes zero, especially for integers).
                //
                result = 1;
            }
            else if (Y == 1)
            {
                //
                // 1. Any number raised to the power of one is itself.
                //
                result = X;
            }
            else if (Y < 0)
            {
                if (X == -1)
                {
                    //
                    // 1. Negative one raised to negative odd powers is negative one.
                    // 2. Negative one raised to negative even powers is one.
                    //
                    if ((Y & 1) != 0) // odd exponent?
                        result = -1;
                    else
                        result = 1;
                }
                else if (X == 0)
                {
                    //
                    // 1. Zero raised to negative powers is the same as attempting to
                    //    divide by zero.
                    //
                    throw new DivideByZeroException();
                }
                else
                {
                    //
                    // 1. Non-zero integers raised to negative powers is zero.
                    //
                    result = 0;
                }
            }
            else
            {
                //
                // 1. Zero raised to any positive non-zero power is itself.
                // 2. One raised to any positive non-zero power is itself.
                //
                result = X;

                //
                // 1. General case of using repeated integer multiplication.  This may
                //    raise an overflow exception.
                //
                while ((result != 0) && (result != 1) && (--Y > 0))
                    result *= X;
            }

            return result;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public static int Log2(int X)
        {
            int N = X;
            int result = 0;

            while (N > 1)
            {
                N >>= 1;
                result++;
            }

            return result;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public static uint Log2(uint X)
        {
            uint N = X;
            uint result = 0;

            while (N > 1)
            {
                N >>= 1;
                result++;
            }

            return result;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public static long Log2(long X)
        {
            long N = X;
            long result = 0;

            while (N > 1)
            {
                N >>= 1;
                result++;
            }

            return result;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public static ulong Log2(ulong X)
        {
            ulong N = X;
            ulong result = 0;

            while (N > 1)
            {
                N >>= 1;
                result++;
            }

            return result;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public static int Log10(int X)
        {
            return (int)Math.Truncate(Math.Log10(X));
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        private static int Log10(long X)
        {
            //
            // HACK: Convert to string and use the length to help
            //       determine the log10() of the integer value.
            //
            string value = X.ToString(
                CultureInfo.InvariantCulture).Trim().TrimStart(
                Characters.MinusSign);

            int length;

            if (StringOps.IsNullOrEmpty(value, out length))
                return Count.Invalid;

            return length - 1;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public static int? Max(
            IEnumerable<int?> collection
            )
        {
            int? maximum = null;

            if (collection != null)
            {
                foreach (int? value in collection)
                {
                    if (value == null)
                        continue;

                    if ((maximum == null) ||
                        ((int)value > (int)maximum))
                    {
                        maximum = value;
                    }
                }
            }

            return maximum;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public static int? Max(
            params int[] args
            )
        {
            int? maximum = null;

            foreach (int value in args)
            {
                if ((maximum == null) ||
                    (value > (int)maximum))
                {
                    maximum = value;
                }
            }

            return maximum;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        #region Dead Code
#if DEAD_CODE
        public static int? Min(
            IEnumerable<int?> collection
            )
        {
            int? minimum = null;

            if (collection != null)
            {
                foreach (int? value in collection)
                {
                    if (value == null)
                        continue;

                    if ((minimum == null) ||
                        ((int)value < (int)minimum))
                    {
                        minimum = value;
                    }
                }
            }

            return minimum;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public static int? Min(
            params int[] args
            )
        {
            int? minimum = null;

            foreach (int value in args)
            {
                if ((minimum == null) ||
                    (value < (int)minimum))
                {
                    minimum = value;
                }
            }

            return minimum;
        }
#endif
        #endregion

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public static bool IsEvenlyDivisible(
            double? dividend,
            long divisor
            )
        {
            if (dividend == null)
                return false;

            return Classify(Math.IEEERemainder((double)dividend,
                divisor)) == FloatingPointClass.Zero;
        }
    }
}
