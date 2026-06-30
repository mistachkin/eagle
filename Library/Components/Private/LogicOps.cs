/*
 * LogicOps.cs --
 *
 * Copyright (c) 2007-2012 by Joe Mistachkin.  All rights reserved.
 *
 * See the file "license.terms" for information on usage and redistribution of
 * this file, and for a DISCLAIMER OF ALL WARRANTIES.
 *
 * RCS: @(#) $Id: $
 */

using System;

#if NET_40
using System.Numerics;
#endif

using System.Threading;
using Eagle._Attributes;

namespace Eagle._Components.Private
{
    /// <summary>
    /// This class provides various low-level logical, comparison, and
    /// bitwise helper operations used internally by the expression engine
    /// and related components.
    /// </summary>
    [ObjectId("876cc31c-cba3-4242-9123-15cd4e8131ca")]
    internal static class LogicOps
    {
        /// <summary>
        /// This method compares two 32-bit integer values and returns an
        /// indication of their relative ordering.
        /// </summary>
        /// <param name="X">
        /// The first value to compare.
        /// </param>
        /// <param name="Y">
        /// The second value to compare.
        /// </param>
        /// <returns>
        /// A negative value if <paramref name="X" /> is less than
        /// <paramref name="Y" />, a positive value if it is greater, or zero
        /// if they are equal.
        /// </returns>
        public static int Compare(int X, int Y)
        {
            //
            // BUGFIX: Do NOT use Math.Sign(X - Y) here: the subtraction can
            //         overflow (e.g. a large positive minus a large negative),
            //         wrapping to the wrong sign and producing an incorrect
            //         comparison (mis-sorting "lsort -integer", etc.).  Use a
            //         direct ordered comparison, which cannot overflow.
            //
            return (X < Y) ? -1 : ((X > Y) ? 1 : 0);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method compares two 64-bit integer values and returns an
        /// indication of their relative ordering.
        /// </summary>
        /// <param name="X">
        /// The first value to compare.
        /// </param>
        /// <param name="Y">
        /// The second value to compare.
        /// </param>
        /// <returns>
        /// A negative value if <paramref name="X" /> is less than
        /// <paramref name="Y" />, a positive value if it is greater, or zero
        /// if they are equal.
        /// </returns>
        public static int Compare(long X, long Y)
        {
            //
            // BUGFIX: Do NOT use Math.Sign(X - Y) here: the subtraction can
            //         overflow (e.g. a large positive minus a large negative),
            //         wrapping to the wrong sign and producing an incorrect
            //         comparison (mis-sorting "lsort -integer", etc.).  Use a
            //         direct ordered comparison, which cannot overflow.
            //
            return (X < Y) ? -1 : ((X > Y) ? 1 : 0);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method compares two double-precision floating-point values
        /// and returns an indication of their relative ordering.
        /// </summary>
        /// <param name="X">
        /// The first value to compare.
        /// </param>
        /// <param name="Y">
        /// The second value to compare.
        /// </param>
        /// <returns>
        /// A negative value if <paramref name="X" /> is less than
        /// <paramref name="Y" />, a positive value if it is greater, or zero
        /// if they are equal.
        /// </returns>
        public static int Compare(double X, double Y)
        {
            return Math.Sign(X - Y);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method exchanges the values of two 64-bit integer variables.
        /// </summary>
        /// <param name="X">
        /// The first variable to swap; upon return, holds the previous value
        /// of <paramref name="Y" />.
        /// </param>
        /// <param name="Y">
        /// The second variable to swap; upon return, holds the previous value
        /// of <paramref name="X" />.
        /// </param>
        public static void Swap(ref long X, ref long Y)
        {
            Y = Interlocked.Exchange(ref X, Y);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        #region Dead Code
#if DEAD_CODE
        /// <summary>
        /// This method exchanges the values of two 32-bit integer variables.
        /// </summary>
        /// <param name="X">
        /// The first variable to swap; upon return, holds the previous value
        /// of <paramref name="Y" />.
        /// </param>
        /// <param name="Y">
        /// The second variable to swap; upon return, holds the previous value
        /// of <paramref name="X" />.
        /// </param>
        private static void Swap(ref int X, ref int Y)
        {
            Y = Interlocked.Exchange(ref X, Y);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method exchanges the values of two unsigned 32-bit integer
        /// variables using an exclusive-or swap; it is not thread-safe.
        /// </summary>
        /// <param name="X">
        /// The first variable to swap; upon return, holds the previous value
        /// of <paramref name="Y" />.
        /// </param>
        /// <param name="Y">
        /// The second variable to swap; upon return, holds the previous value
        /// of <paramref name="X" />.
        /// </param>
        private static void Swap(ref uint X, ref uint Y) /* NOT THREAD-SAFE */
        {
            X = X ^ Y;
            Y = X ^ Y;
            X = X ^ Y;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method exchanges the values of two unsigned 64-bit integer
        /// variables using an exclusive-or swap; it is not thread-safe.
        /// </summary>
        /// <param name="X">
        /// The first variable to swap; upon return, holds the previous value
        /// of <paramref name="Y" />.
        /// </param>
        /// <param name="Y">
        /// The second variable to swap; upon return, holds the previous value
        /// of <paramref name="X" />.
        /// </param>
        private static void Swap(ref ulong X, ref ulong Y) /* NOT THREAD-SAFE */
        {
            X = X ^ Y;
            Y = X ^ Y;
            X = X ^ Y;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method exchanges the values of two single-precision
        /// floating-point variables.
        /// </summary>
        /// <param name="X">
        /// The first variable to swap; upon return, holds the previous value
        /// of <paramref name="Y" />.
        /// </param>
        /// <param name="Y">
        /// The second variable to swap; upon return, holds the previous value
        /// of <paramref name="X" />.
        /// </param>
        private static void Swap(ref float X, ref float Y)
        {
            Y = Interlocked.Exchange(ref X, Y);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method exchanges the values of two double-precision
        /// floating-point variables.
        /// </summary>
        /// <param name="X">
        /// The first variable to swap; upon return, holds the previous value
        /// of <paramref name="Y" />.
        /// </param>
        /// <param name="Y">
        /// The second variable to swap; upon return, holds the previous value
        /// of <paramref name="X" />.
        /// </param>
        private static void Swap(ref double X, ref double Y)
        {
            Y = Interlocked.Exchange(ref X, Y);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method exchanges the values of two native pointer variables.
        /// </summary>
        /// <param name="X">
        /// The first variable to swap; upon return, holds the previous value
        /// of <paramref name="Y" />.
        /// </param>
        /// <param name="Y">
        /// The second variable to swap; upon return, holds the previous value
        /// of <paramref name="X" />.
        /// </param>
        private static void Swap(ref IntPtr X, ref IntPtr Y)
        {
            Y = Interlocked.Exchange(ref X, Y);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method exchanges the values of two object reference variables.
        /// </summary>
        /// <param name="X">
        /// The first variable to swap; upon return, holds the previous value
        /// of <paramref name="Y" />.
        /// </param>
        /// <param name="Y">
        /// The second variable to swap; upon return, holds the previous value
        /// of <paramref name="X" />.
        /// </param>
        private static void Swap(ref object X, ref object Y)
        {
            Y = Interlocked.Exchange(ref X, Y);
        }
#endif
        #endregion

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method computes the logical negation of a Boolean value.
        /// </summary>
        /// <param name="X">
        /// The value to negate.
        /// </param>
        /// <returns>
        /// True if <paramref name="X" /> is false; otherwise, false.
        /// </returns>
        public static bool Not(bool X)
        {
            return !X;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method computes the logical conjunction of two Boolean values.
        /// </summary>
        /// <param name="X">
        /// The first value.
        /// </param>
        /// <param name="Y">
        /// The second value.
        /// </param>
        /// <returns>
        /// True if both <paramref name="X" /> and <paramref name="Y" /> are
        /// true; otherwise, false.
        /// </returns>
        public static bool And(bool X, bool Y)
        {
            return X && Y;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method computes the logical disjunction of two Boolean values.
        /// </summary>
        /// <param name="X">
        /// The first value.
        /// </param>
        /// <param name="Y">
        /// The second value.
        /// </param>
        /// <returns>
        /// True if either <paramref name="X" /> or <paramref name="Y" /> is
        /// true; otherwise, false.
        /// </returns>
        public static bool Or(bool X, bool Y)
        {
            return X || Y;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method computes the logical exclusive-or of two Boolean
        /// values.
        /// </summary>
        /// <param name="X">
        /// The first value.
        /// </param>
        /// <param name="Y">
        /// The second value.
        /// </param>
        /// <returns>
        /// True if exactly one of <paramref name="X" /> and
        /// <paramref name="Y" /> is true; otherwise, false.
        /// </returns>
        public static bool Xor(bool X, bool Y)
        {
            return (X || Y) && !(X && Y);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method computes the logical implication of two Boolean values.
        /// </summary>
        /// <param name="X">
        /// The antecedent value.
        /// </param>
        /// <param name="Y">
        /// The consequent value.
        /// </param>
        /// <returns>
        /// True if <paramref name="X" /> is false or <paramref name="Y" /> is
        /// true (i.e. <paramref name="X" /> implies <paramref name="Y" />);
        /// otherwise, false.
        /// </returns>
        public static bool Imp(bool X, bool Y)
        {
            return !X || Y;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method computes the bitwise logical implication of two 8-bit
        /// values.
        /// </summary>
        /// <param name="X">
        /// The antecedent value.
        /// </param>
        /// <param name="Y">
        /// The consequent value.
        /// </param>
        /// <returns>
        /// The bitwise implication of <paramref name="X" /> and
        /// <paramref name="Y" />.
        /// </returns>
        public static byte Imp(byte X, byte Y)
        {
            return (byte)(~X | Y);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method computes the bitwise logical implication of two 32-bit
        /// integer values.
        /// </summary>
        /// <param name="X">
        /// The antecedent value.
        /// </param>
        /// <param name="Y">
        /// The consequent value.
        /// </param>
        /// <returns>
        /// The bitwise implication of <paramref name="X" /> and
        /// <paramref name="Y" />.
        /// </returns>
        public static int Imp(int X, int Y)
        {
            return ~X | Y;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method computes the bitwise logical implication of two 64-bit
        /// integer values.
        /// </summary>
        /// <param name="X">
        /// The antecedent value.
        /// </param>
        /// <param name="Y">
        /// The consequent value.
        /// </param>
        /// <returns>
        /// The bitwise implication of <paramref name="X" /> and
        /// <paramref name="Y" />.
        /// </returns>
        public static long Imp(long X, long Y)
        {
            return ~X | Y;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

#if NET_40
        /// <summary>
        /// This method computes the bitwise logical implication of two
        /// arbitrary-precision integer values.
        /// </summary>
        /// <param name="X">
        /// The antecedent value.
        /// </param>
        /// <param name="Y">
        /// The consequent value.
        /// </param>
        /// <returns>
        /// The bitwise implication of <paramref name="X" /> and
        /// <paramref name="Y" />.
        /// </returns>
        public static BigInteger Imp(BigInteger X, BigInteger Y)
        {
            return ~X | Y;
        }
#endif

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method computes the logical equivalence of two Boolean values.
        /// </summary>
        /// <param name="X">
        /// The first value.
        /// </param>
        /// <param name="Y">
        /// The second value.
        /// </param>
        /// <returns>
        /// True if <paramref name="X" /> and <paramref name="Y" /> have the
        /// same Boolean value; otherwise, false.
        /// </returns>
        public static bool Eqv(bool X, bool Y)
        {
            return (X && Y) || (!X && !Y);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method computes the bitwise logical equivalence of two 8-bit
        /// values.
        /// </summary>
        /// <param name="X">
        /// The first value.
        /// </param>
        /// <param name="Y">
        /// The second value.
        /// </param>
        /// <returns>
        /// The bitwise equivalence of <paramref name="X" /> and
        /// <paramref name="Y" />.
        /// </returns>
        public static byte Eqv(byte X, byte Y)
        {
            return (byte)~(X ^ Y);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method computes the bitwise logical equivalence of two 32-bit
        /// integer values.
        /// </summary>
        /// <param name="X">
        /// The first value.
        /// </param>
        /// <param name="Y">
        /// The second value.
        /// </param>
        /// <returns>
        /// The bitwise equivalence of <paramref name="X" /> and
        /// <paramref name="Y" />.
        /// </returns>
        public static int Eqv(int X, int Y)
        {
            return ~(X ^ Y);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method computes the bitwise logical equivalence of two 64-bit
        /// integer values.
        /// </summary>
        /// <param name="X">
        /// The first value.
        /// </param>
        /// <param name="Y">
        /// The second value.
        /// </param>
        /// <returns>
        /// The bitwise equivalence of <paramref name="X" /> and
        /// <paramref name="Y" />.
        /// </returns>
        public static long Eqv(long X, long Y)
        {
            return ~(X ^ Y);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

#if NET_40
        /// <summary>
        /// This method computes the bitwise logical equivalence of two
        /// arbitrary-precision integer values.
        /// </summary>
        /// <param name="X">
        /// The first value.
        /// </param>
        /// <param name="Y">
        /// The second value.
        /// </param>
        /// <returns>
        /// The bitwise equivalence of <paramref name="X" /> and
        /// <paramref name="Y" />.
        /// </returns>
        public static BigInteger Eqv(BigInteger X, BigInteger Y)
        {
            return ~(X ^ Y);
        }
#endif

        ///////////////////////////////////////////////////////////////////////////////////////////////

        #region Dead Code
#if DEAD_CODE
        /// <summary>
        /// This method returns the first of its two operands, ignoring the
        /// second.
        /// </summary>
        /// <param name="X">
        /// The value to return.
        /// </param>
        /// <param name="Y">
        /// The value to ignore.
        /// </param>
        /// <returns>
        /// The value of <paramref name="X" />.
        /// </returns>
        private static object X(object X, object Y)
        {
            return X;
        }
#endif
        #endregion

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method returns the second of its two operands, ignoring the
        /// first.
        /// </summary>
        /// <param name="X">
        /// The value to ignore.
        /// </param>
        /// <param name="Y">
        /// The value to return.
        /// </param>
        /// <returns>
        /// The value of <paramref name="Y" />.
        /// </returns>
        public static object Y(object X, object Y)
        {
            return Y;
        }
    }
}
