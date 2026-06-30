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

#if NET_40
using System.Numerics;
#endif

using System.Threading;
using Eagle._Attributes;
using Eagle._Components.Public;
using Eagle._Constants;

namespace Eagle._Components.Private
{
    /// <summary>
    /// This class provides static helper methods for performing low-level
    /// mathematical operations used throughout the Eagle core, including
    /// power-of-two checks, integer exponentiation, bit shifting and rotation,
    /// logarithms, FNV hashing, and approximate floating-point comparison.
    /// </summary>
    [ObjectId("4d43cec5-9a8c-4b0e-b47d-002c28de623f")]
    internal static class MathOps
    {
        #region Private Constants
        /// <summary>
        /// One half of the minimum value representable by a 32-bit signed integer,
        /// used to determine whether a value can be doubled without overflow.
        /// </summary>
        private const int HalfInt32MinValue = int.MinValue / 2;
        /// <summary>
        /// One half of the maximum value representable by a 32-bit signed integer,
        /// used to determine whether a value can be doubled without overflow.
        /// </summary>
        private const int HalfInt32MaxValue = int.MaxValue / 2;

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// The offset basis used to seed the 32-bit FNV-1 hash.
        /// </summary>
        private const uint FnvOffsetBasis32 = 2166136261;
        /// <summary>
        /// The prime multiplier used by the 32-bit FNV-1 hash.
        /// </summary>
        private const uint FnvPrime32 = 16777619;

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// The offset basis used to seed the 64-bit FNV-1 hash.
        /// </summary>
        private const ulong FnvOffsetBasis64 = 14695981039346656037;
        /// <summary>
        /// The prime multiplier used by the 64-bit FNV-1 hash.
        /// </summary>
        private const ulong FnvPrime64 = 1099511628211;

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// The number of bits a double-precision value must be shifted right to
        /// isolate its biased exponent.
        /// </summary>
        private const int DoubleExponentShift = 52;
        /// <summary>
        /// The number of bits occupied by the exponent of a double-precision value.
        /// </summary>
        private const int DoubleExponentBits = 11;
        /// <summary>
        /// The bit mask used to extract the biased exponent of a double-precision
        /// value.
        /// </summary>
        private const long DoubleExponentMask = 0x7FF;

        ///////////////////////////////////////////////////////////////////////////////////////////////

        //
        // HACK: These are purposely not read-only.
        //
        /// <summary>
        /// The default tolerance used when comparing two double-precision values
        /// for approximate equality.
        /// </summary>
        private static double DoubleEpsilon = 0.00001;
        /// <summary>
        /// The default tolerance used when comparing two decimal values for
        /// approximate equality.
        /// </summary>
        private static decimal DecimalEpsilon = 0.00001m;
        /// <summary>
        /// The value of pi represented as a decimal, used when decimal precision is
        /// requested.
        /// </summary>
        private static decimal DecimalPi = 3.1415926535897932384626433833m;
        /// <summary>
        /// When non-zero, the value of pi is returned as a decimal; otherwise, it
        /// is returned as a double.  This exists for compatibility with the Eagle
        /// beta.
        /// </summary>
        private static bool UseDecimalForPi = false; // COMPAT: Eagle beta.

        ///////////////////////////////////////////////////////////////////////////////////////////////

        //
        // HACK: These are purposely not read-only.
        //
        /// <summary>
        /// Controls how often the active interpreter is checked for readiness while
        /// computing an integer power; a negative value disables the check.
        /// </summary>
        private static int readyPowCount = 10000;
        /// <summary>
        /// The yield behavior applied between readiness checks while computing an
        /// integer power.
        /// </summary>
        private static int readyPowYield = (int)YieldType.Default;
        /// <summary>
        /// The largest exponent permitted when computing an integer power; a value
        /// greater than this is rejected.  This exists for compatibility with Tcl.
        /// </summary>
        private static int maximumExponent = 0xFFFFFFF; /* COMPAT: Tcl */

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// A lookup table containing each successive power of two that fits within
        /// a 64-bit unsigned integer, indexed by exponent.
        /// </summary>
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

        /// <summary>
        /// This method determines whether the specified value is an exact power of
        /// two.
        /// </summary>
        /// <param name="value">
        /// The value to test.
        /// </param>
        /// <returns>
        /// True if the value is a power of two; otherwise, false.
        /// </returns>
        public static bool IsPowerOfTwo(
            ulong value /* in */
            )
        {
            ulong[] values = PowersOfTwo;

            if (values == null)
                return false;

            int length = values.Length;

            for (int index = 0; index < length; index++) /* O(64) */
                if (values[index] == value)
                    return true;

            return false;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method returns two raised to the specified power, using a
        /// precomputed lookup table.
        /// </summary>
        /// <param name="X">
        /// The exponent, which must be within the bounds of the lookup table.
        /// </param>
        /// <returns>
        /// Two raised to the specified power, or null when the exponent is negative
        /// or too large to represent.
        /// </returns>
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

        /// <summary>
        /// This method determines whether the specified double-precision value is
        /// not zero.
        /// </summary>
        /// <param name="X">
        /// The value to test.
        /// </param>
        /// <returns>
        /// True if the value is not zero; otherwise, false.
        /// </returns>
        public static bool NotZero(double X)
        {
            return ((X < 0.0) || (X > 0.0));
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method determines whether the base-ten magnitudes of the two
        /// specified values differ by an amount within the optional minimum and
        /// maximum bounds.
        /// </summary>
        /// <param name="X">
        /// The first value to compare.
        /// </param>
        /// <param name="Y">
        /// The second value to compare.
        /// </param>
        /// <param name="minimum">
        /// The minimum allowed difference in magnitude, or null for no minimum.
        /// </param>
        /// <param name="maximum">
        /// The maximum allowed difference in magnitude, or null for no maximum.
        /// </param>
        /// <returns>
        /// True if the difference in magnitude falls within the specified bounds;
        /// otherwise, false.
        /// </returns>
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

        /// <summary>
        /// This method determines whether two double-precision values are
        /// approximately equal, using the default tolerance.
        /// </summary>
        /// <param name="X">
        /// The first value to compare.
        /// </param>
        /// <param name="Y">
        /// The second value to compare.
        /// </param>
        /// <returns>
        /// True if the two values are approximately equal; otherwise, false.
        /// </returns>
        public static bool AboutEquals(
            double X,
            double Y
            )
        {
            return AboutEquals(X, Y, DoubleEpsilon);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method determines whether two double-precision values are
        /// approximately equal, within the specified tolerance, with special
        /// handling for not-a-number and infinite values.
        /// </summary>
        /// <param name="X">
        /// The first value to compare.
        /// </param>
        /// <param name="Y">
        /// The second value to compare.
        /// </param>
        /// <param name="epsilon">
        /// The maximum permitted difference for the values to be considered equal.
        /// </param>
        /// <returns>
        /// True if the two values are approximately equal; otherwise, false.
        /// </returns>
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

        /// <summary>
        /// This method returns the value of pi, as either a decimal or a double
        /// depending on the current configuration.
        /// </summary>
        /// <returns>
        /// The value of pi.
        /// </returns>
        public static Argument Pi()
        {
            if (UseDecimalForPi)
                return DecimalPi;
            else
                return Math.PI;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method determines whether two decimal values are approximately
        /// equal, using the default tolerance.
        /// </summary>
        /// <param name="X">
        /// The first value to compare.
        /// </param>
        /// <param name="Y">
        /// The second value to compare.
        /// </param>
        /// <returns>
        /// True if the two values are approximately equal; otherwise, false.
        /// </returns>
        public static bool AboutEquals(
            decimal X,
            decimal Y
            )
        {
            return AboutEquals(X, Y, DecimalEpsilon);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method determines whether two decimal values are approximately
        /// equal, within the specified tolerance.
        /// </summary>
        /// <param name="X">
        /// The first value to compare.
        /// </param>
        /// <param name="Y">
        /// The second value to compare.
        /// </param>
        /// <param name="epsilon">
        /// The maximum permitted difference for the values to be considered equal.
        /// </param>
        /// <returns>
        /// True if the two values are approximately equal; otherwise, false.
        /// </returns>
        public static bool AboutEquals(
            decimal X,
            decimal Y,
            decimal epsilon
            )
        {
            return Math.Abs(X - Y) < epsilon;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method classifies the specified double-precision value as zero,
        /// subnormal, normal, infinite, or not-a-number, based on its exponent and
        /// mantissa bits.
        /// </summary>
        /// <param name="value">
        /// The value to classify.
        /// </param>
        /// <returns>
        /// A <see cref="FloatingPointClass" /> value describing the specified
        /// value.
        /// </returns>
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

        /// <summary>
        /// This method determines whether the specified value can be doubled
        /// without overflowing a 32-bit signed integer.
        /// </summary>
        /// <param name="value">
        /// The value to test.
        /// </param>
        /// <returns>
        /// True if the value can be doubled without overflow; otherwise, false.
        /// </returns>
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

        /// <summary>
        /// This method computes a 32-bit FNV-1 hash over the specified bytes.
        /// </summary>
        /// <param name="bytes">
        /// The bytes to hash.  This parameter may be null.
        /// </param>
        /// <param name="alternate">
        /// Non-zero to use the alternate ordering in which each byte is mixed in
        /// before, rather than after, multiplication by the prime.
        /// </param>
        /// <returns>
        /// The 32-bit FNV-1 hash of the specified bytes, or zero when no bytes are
        /// supplied.
        /// </returns>
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

        /// <summary>
        /// This method computes a 64-bit FNV-1 hash over the specified bytes.
        /// </summary>
        /// <param name="bytes">
        /// The bytes to hash.  This parameter may be null.
        /// </param>
        /// <param name="alternate">
        /// Non-zero to use the alternate ordering in which each byte is mixed in
        /// before, rather than after, multiplication by the prime.
        /// </param>
        /// <returns>
        /// The 64-bit FNV-1 hash of the specified bytes, or zero when no bytes are
        /// supplied.
        /// </returns>
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

        /// <summary>
        /// This method shifts a 32-bit integer left by the specified number of
        /// bits, returning zero for a negative shift count or one that meets or
        /// exceeds the operand width, for compatibility with Tcl.
        /// </summary>
        /// <param name="X">
        /// The value to shift.
        /// </param>
        /// <param name="Y">
        /// The number of bits to shift by.
        /// </param>
        /// <returns>
        /// The shifted value, or zero when the shift count is out of range.
        /// </returns>
        public static int LeftShift(int X, int Y)
        {
            //
            // NOTE: A shift count that meets or exceeds the operand width
            //       yields zero (every bit is shifted out).  A negative count
            //       is masked to its low bits by the native C# shift (e.g.
            //       "X << -3" becomes "X << 29"); this matches Tcl 8.4 and the
            //       Eagle rotate operators, which likewise treat the count as
            //       modulo the operand width (COMPAT: Tcl 8.4).
            //
            return (Y < ConversionOps.IntBits) ? X << Y : 0;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method shifts a 32-bit integer right by the specified number of
        /// bits, returning zero for a negative shift count or one that meets or
        /// exceeds the operand width, for compatibility with Tcl.
        /// </summary>
        /// <param name="X">
        /// The value to shift.
        /// </param>
        /// <param name="Y">
        /// The number of bits to shift by.
        /// </param>
        /// <returns>
        /// The shifted value, or zero when the shift count is out of range.
        /// </returns>
        public static int RightShift(int X, int Y)
        {
            //
            // NOTE: A shift count that meets or exceeds the operand width
            //       yields zero (every bit is shifted out).  A negative count
            //       is masked to its low bits by the native C# shift (e.g.
            //       "X >> -3" becomes "X >> 29"); this matches Tcl 8.4 and the
            //       Eagle rotate operators, which likewise treat the count as
            //       modulo the operand width (COMPAT: Tcl 8.4).
            //
            return (Y < ConversionOps.IntBits) ? X >> Y : 0;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method shifts a 64-bit integer left by the specified number of
        /// bits, returning zero for a negative shift count or one that meets or
        /// exceeds the operand width, for compatibility with Tcl.
        /// </summary>
        /// <param name="X">
        /// The value to shift.
        /// </param>
        /// <param name="Y">
        /// The number of bits to shift by.
        /// </param>
        /// <returns>
        /// The shifted value, or zero when the shift count is out of range.
        /// </returns>
        public static long LeftShift(long X, int Y)
        {
            //
            // NOTE: A shift count that meets or exceeds the operand width
            //       yields zero; a negative count is masked to its low 6 bits
            //       by the native C# shift (e.g. "X << -3" becomes "X << 61"),
            //       matching Tcl 8.4 (which masks the wide shift count) and the
            //       Eagle rotate operators (COMPAT: Tcl 8.4).
            //
            return (Y < ConversionOps.LongBits) ? X << Y : 0;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

#if NET_40
        /// <summary>
        /// This method shifts an arbitrary-precision integer left by the specified
        /// number of bits.
        /// </summary>
        /// <param name="X">
        /// The value to shift.
        /// </param>
        /// <param name="Y">
        /// The number of bits to shift by.
        /// </param>
        /// <returns>
        /// The shifted value.
        /// </returns>
        public static BigInteger LeftShift(BigInteger X, int Y)
        {
            //
            // NOTE: It seems that for wide integers, Tcl 8.4 returns zero
            //       for all negative shift values (COMPAT: Tcl 8.4).
            //
            return X << Y;
        }
#endif

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method shifts a 64-bit integer right by the specified number of
        /// bits, returning zero for a negative shift count or one that meets or
        /// exceeds the operand width, for compatibility with Tcl.
        /// </summary>
        /// <param name="X">
        /// The value to shift.
        /// </param>
        /// <param name="Y">
        /// The number of bits to shift by.
        /// </param>
        /// <returns>
        /// The shifted value, or zero when the shift count is out of range.
        /// </returns>
        public static long RightShift(long X, int Y)
        {
            //
            // NOTE: A shift count that meets or exceeds the operand width
            //       yields zero; a negative count is masked to its low 6 bits
            //       by the native C# shift (e.g. "X >> -3" becomes "X >> 61"),
            //       matching Tcl 8.4 (which masks the wide shift count) and the
            //       Eagle rotate operators (COMPAT: Tcl 8.4).
            //
            return (Y < ConversionOps.LongBits) ? X >> Y : 0;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

#if NET_40
        /// <summary>
        /// This method shifts an arbitrary-precision integer right by the specified
        /// number of bits.
        /// </summary>
        /// <param name="X">
        /// The value to shift.
        /// </param>
        /// <param name="Y">
        /// The number of bits to shift by.
        /// </param>
        /// <returns>
        /// The shifted value.
        /// </returns>
        public static BigInteger RightShift(BigInteger X, int Y)
        {
            //
            // NOTE: It seems that for wide integers, Tcl 8.4 returns zero
            //       for all negative shift values (COMPAT: Tcl 8.4).
            //
            return X >> Y;
        }
#endif

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method rotates the bits of a 32-bit integer left by the specified
        /// number of positions.
        /// </summary>
        /// <param name="X">
        /// The value to rotate.
        /// </param>
        /// <param name="Y">
        /// The number of positions to rotate by.
        /// </param>
        /// <returns>
        /// The rotated value.
        /// </returns>
        public static int LeftRotate(int X, int Y)
        {
            //
            // NOTE: Per MSDN, C# masks the high bits for us.
            //
            return ((X << Y) | (X >> (ConversionOps.IntBits - Y)));
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method rotates the bits of a 32-bit integer right by the specified
        /// number of positions.
        /// </summary>
        /// <param name="X">
        /// The value to rotate.
        /// </param>
        /// <param name="Y">
        /// The number of positions to rotate by.
        /// </param>
        /// <returns>
        /// The rotated value.
        /// </returns>
        public static int RightRotate(int X, int Y)
        {
            //
            // NOTE: Per MSDN, C# masks the high bits for us.
            //
            return ((X >> Y) | (X << (ConversionOps.IntBits - Y)));
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method rotates the bits of a 64-bit integer left by the specified
        /// number of positions.
        /// </summary>
        /// <param name="X">
        /// The value to rotate.
        /// </param>
        /// <param name="Y">
        /// The number of positions to rotate by.
        /// </param>
        /// <returns>
        /// The rotated value.
        /// </returns>
        public static long LeftRotate(long X, int Y)
        {
            //
            // NOTE: Per MSDN, C# masks the high bits for us.
            //
            return ((X << Y) | (X >> (ConversionOps.LongBits - Y)));
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method rotates the bits of a 64-bit integer right by the specified
        /// number of positions.
        /// </summary>
        /// <param name="X">
        /// The value to rotate.
        /// </param>
        /// <param name="Y">
        /// The number of positions to rotate by.
        /// </param>
        /// <returns>
        /// The rotated value.
        /// </returns>
        public static long RightRotate(long X, int Y)
        {
            //
            // NOTE: Per MSDN, C# masks the high bits for us.
            //
            return ((X >> Y) | (X << (ConversionOps.LongBits - Y)));
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

#if NET_40
        /// <summary>
        /// This method rotates the bits of an arbitrary-precision integer left by
        /// the specified number of positions, within the specified bit width.
        /// </summary>
        /// <param name="X">
        /// The value to rotate.
        /// </param>
        /// <param name="Y">
        /// The number of positions to rotate by.
        /// </param>
        /// <param name="bits">
        /// The width, in bits, over which the rotation is performed.
        /// </param>
        /// <returns>
        /// The rotated value.
        /// </returns>
        public static BigInteger LeftRotate(BigInteger X, int Y, int bits)
        {
            //
            // NOTE: Per MSDN, C# masks the high bits for us.
            //
            return ((X << Y) | (X >> (bits - Y)));
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method rotates the bits of an arbitrary-precision integer right by
        /// the specified number of positions, within the specified bit width.
        /// </summary>
        /// <param name="X">
        /// The value to rotate.
        /// </param>
        /// <param name="Y">
        /// The number of positions to rotate by.
        /// </param>
        /// <param name="bits">
        /// The width, in bits, over which the rotation is performed.
        /// </param>
        /// <returns>
        /// The rotated value.
        /// </returns>
        public static BigInteger RightRotate(BigInteger X, int Y, int bits)
        {
            //
            // NOTE: Per MSDN, C# masks the high bits for us.
            //
            return ((X >> Y) | (X << (bits - Y)));
        }
#endif

        ///////////////////////////////////////////////////////////////////////////////////////////////

        //
        // HACK: Check if the current script has been canceled;
        //       this method is only for use by the Pow method
        //       overloads, below.
        //
        /// <summary>
        /// This method determines whether the active interpreter, if any, is no
        /// longer ready (e.g. the script being evaluated has been canceled).  It is
        /// intended only for use by the integer power methods.
        /// </summary>
        /// <param name="error">
        /// Upon failure, this contains an appropriate error message describing why
        /// the interpreter is not ready.
        /// </param>
        /// <returns>
        /// True if the active interpreter is no longer ready; otherwise, false.
        /// </returns>
        private static bool IsNotReady(
            ref Result error /* out */
            )
        {
            Interpreter interpreter = Interpreter.GetActive();

            if (interpreter == null)
                return false;

            Result result = null;

            if (Interpreter.Ready(
                    interpreter, ref result) == ReturnCode.Ok)
            {
                return false;
            }

            error = result;
            return true;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method raises a 32-bit integer to the specified power using
        /// repeated multiplication, handling the various special cases and
        /// periodically checking that the active interpreter remains ready.
        /// </summary>
        /// <param name="X">
        /// The base value.
        /// </param>
        /// <param name="Y">
        /// The exponent.
        /// </param>
        /// <returns>
        /// The base raised to the specified power.
        /// </returns>
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
            else if (X == -1)
            {
                //
                // NOTE: Negative one raised to any odd power is negative one;
                //       to any even power it is one.  This MUST be handled
                //       before the general loop below: that loop would
                //       otherwise compute an incorrect result for odd
                //       exponents (the former "result != 1" early-exit
                //       truncated the -1/1 oscillation, e.g. "(-1)**3"
                //       yielded 1), and a large exponent would also spin
                //       pointlessly.
                //
                result = ((Y & 1) != 0) ? -1 : 1;
            }
            else
            {
                //
                // BUGFIX: Do not allow an exponent that is greater than we
                //         can (reasonably) calculate using a 32-bit signed
                //         integer.
                //
                if ((maximumExponent > 0) && (Y > maximumExponent))
                    throw new ScriptException("integer exponent too large");

                //
                // 1. Zero raised to any positive non-zero power is itself.
                // 2. One raised to any positive non-zero power is itself.
                //
                result = X;

                //
                // 1. General case of using repeated integer multiplication.
                //    This may raise an overflow exception.
                //
                int readyCount = Interlocked.CompareExchange(
                    ref readyPowCount, 0, 0);

                int readyYield = Interlocked.CompareExchange(
                    ref readyPowYield, 0, 0);

                int count = 0;

                while ((result != 0) && (--Y > 0))
                {
                    //
                    // BUGFIX: Do not simply spin in this loop, which could
                    //         take a while; instead, make sure the active
                    //         interpreter, if any, is still "ready", e.g.
                    //         the script being evaluated has not yet been
                    //         canceled.
                    //
                    if (readyCount >= 0)
                    {
                        if ((readyCount == 0) || ((count++ % readyCount) == 0))
                        {
                            Result error = null;

                            if (IsNotReady(ref error))
                                throw new ScriptException(error);

                            HostOps.MaybeThreadYieldAndOrSleep(readyYield);
                        }
                    }

                    result *= X;
                }
            }

            return result;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method raises a 64-bit integer to the specified power using
        /// repeated multiplication, handling the various special cases and
        /// periodically checking that the active interpreter remains ready.
        /// </summary>
        /// <param name="X">
        /// The base value.
        /// </param>
        /// <param name="Y">
        /// The exponent.
        /// </param>
        /// <returns>
        /// The base raised to the specified power.
        /// </returns>
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
            else if (X == -1)
            {
                //
                // NOTE: Negative one raised to any odd power is negative one;
                //       to any even power it is one.  This MUST be handled
                //       before the general loop below: that loop would
                //       otherwise compute an incorrect result for odd
                //       exponents (the former "result != 1" early-exit
                //       truncated the -1/1 oscillation, e.g. "(-1)**3"
                //       yielded 1), and a large exponent would also spin
                //       pointlessly.
                //
                result = ((Y & 1) != 0) ? -1 : 1;
            }
            else
            {
                //
                // BUGFIX: Do not allow an exponent that is greater than we
                //         can (reasonably) calculate using a 64-bit signed
                //         integer.
                //
                if ((maximumExponent > 0) && (Y > maximumExponent))
                    throw new ScriptException("integer exponent too large");

                //
                // 1. Zero raised to any positive non-zero power is itself.
                // 2. One raised to any positive non-zero power is itself.
                //
                result = X;

                //
                // 1. General case of using repeated integer multiplication.
                //    This may raise an overflow exception.
                //
                int readyCount = Interlocked.CompareExchange(
                    ref readyPowCount, 0, 0);

                int readyYield = Interlocked.CompareExchange(
                    ref readyPowYield, 0, 0);

                int count = 0;

                while ((result != 0) && (--Y > 0))
                {
                    //
                    // BUGFIX: Do not simply spin in this loop, which could
                    //         take a while; instead, make sure the active
                    //         interpreter, if any, is still "ready", e.g.
                    //         the script being evaluated has not yet been
                    //         canceled.
                    //
                    if (readyCount >= 0)
                    {
                        if ((readyCount == 0) || ((count++ % readyCount) == 0))
                        {
                            Result error = null;

                            if (IsNotReady(ref error))
                                throw new ScriptException(error);

                            HostOps.MaybeThreadYieldAndOrSleep(readyYield);
                        }
                    }

                    result *= X;
                }
            }

            return result;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method computes the floor of the base-two logarithm of the
        /// specified value.
        /// </summary>
        /// <param name="X">
        /// The value whose base-two logarithm is computed.
        /// </param>
        /// <returns>
        /// The floor of the base-two logarithm of the specified value.
        /// </returns>
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

        /// <summary>
        /// This method computes the floor of the base-two logarithm of the
        /// specified value.
        /// </summary>
        /// <param name="X">
        /// The value whose base-two logarithm is computed.
        /// </param>
        /// <returns>
        /// The floor of the base-two logarithm of the specified value.
        /// </returns>
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

        /// <summary>
        /// This method computes the floor of the base-two logarithm of the
        /// specified value.
        /// </summary>
        /// <param name="X">
        /// The value whose base-two logarithm is computed.
        /// </param>
        /// <returns>
        /// The floor of the base-two logarithm of the specified value.
        /// </returns>
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

        /// <summary>
        /// This method computes the floor of the base-two logarithm of the
        /// specified value.
        /// </summary>
        /// <param name="X">
        /// The value whose base-two logarithm is computed.
        /// </param>
        /// <returns>
        /// The floor of the base-two logarithm of the specified value.
        /// </returns>
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

#if NET_40
        /// <summary>
        /// This method computes the base-two logarithm of the specified
        /// arbitrary-precision integer.
        /// </summary>
        /// <param name="X">
        /// The value whose base-two logarithm is computed.
        /// </param>
        /// <returns>
        /// The base-two logarithm of the specified value.
        /// </returns>
        public static double Log2(BigInteger X)
        {
            return BigInteger.Log(X, 2);
        }
#endif

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method computes the floor of the base-ten logarithm of the
        /// specified value.
        /// </summary>
        /// <param name="X">
        /// The value whose base-ten logarithm is computed.
        /// </param>
        /// <returns>
        /// The floor of the base-ten logarithm of the specified value.
        /// </returns>
        public static int Log10(int X)
        {
            return (int)Math.Truncate(Math.Log10(X));
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method computes the floor of the base-ten logarithm of the
        /// specified value, using the length of its string representation.
        /// </summary>
        /// <param name="X">
        /// The value whose base-ten logarithm is computed.
        /// </param>
        /// <returns>
        /// The floor of the base-ten logarithm of the specified value, or an
        /// invalid count when the value has no string representation.
        /// </returns>
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

        /// <summary>
        /// This method returns the largest non-null value in the specified
        /// collection.
        /// </summary>
        /// <param name="collection">
        /// The collection of values to examine.  This parameter may be null.
        /// </param>
        /// <returns>
        /// The largest non-null value in the collection, or null when the
        /// collection is null or contains no non-null values.
        /// </returns>
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

        /// <summary>
        /// This method returns the largest of the specified values.
        /// </summary>
        /// <param name="args">
        /// The values to examine.
        /// </param>
        /// <returns>
        /// The largest of the specified values, or null when no values are
        /// supplied.
        /// </returns>
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
        /// <summary>
        /// This method returns the smallest value contained in the specified
        /// collection, ignoring any null values.
        /// </summary>
        /// <param name="collection">
        /// The collection of values to examine.
        /// </param>
        /// <returns>
        /// The smallest value in the collection, or null if the collection is
        /// null or contains no non-null values.
        /// </returns>
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

        /// <summary>
        /// This method returns the smallest of the specified values.
        /// </summary>
        /// <param name="args">
        /// The values to examine.
        /// </param>
        /// <returns>
        /// The smallest of the specified values, or null if no values are
        /// specified.
        /// </returns>
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

        /// <summary>
        /// This method determines whether the specified dividend is evenly
        /// divisible by the specified divisor.
        /// </summary>
        /// <param name="dividend">
        /// The dividend to test.  This parameter may be null.
        /// </param>
        /// <param name="divisor">
        /// The divisor to test against.
        /// </param>
        /// <returns>
        /// True if the dividend is non-null and evenly divisible by the divisor;
        /// otherwise, false.
        /// </returns>
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
