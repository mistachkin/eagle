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
using System.Threading;
using Eagle._Attributes;

namespace Eagle._Components.Private
{
    [ObjectId("876cc31c-cba3-4242-9123-15cd4e8131ca")]
    internal static class LogicOps
    {
        public static int Compare(int X, int Y)
        {
            return Math.Sign(X - Y);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public static int Compare(long X, long Y)
        {
            return Math.Sign(X - Y);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public static int Compare(double X, double Y)
        {
            return Math.Sign(X - Y);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public static void Swap(ref long X, ref long Y)
        {
            Y = Interlocked.Exchange(ref X, Y);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        #region Dead Code
#if DEAD_CODE
        private static void Swap(ref int X, ref int Y)
        {
            Y = Interlocked.Exchange(ref X, Y);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        private static void Swap(ref uint X, ref uint Y) /* NOT THREAD-SAFE */
        {
            X = X ^ Y;
            Y = X ^ Y;
            X = X ^ Y;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        private static void Swap(ref ulong X, ref ulong Y) /* NOT THREAD-SAFE */
        {
            X = X ^ Y;
            Y = X ^ Y;
            X = X ^ Y;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        private static void Swap(ref float X, ref float Y)
        {
            Y = Interlocked.Exchange(ref X, Y);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        private static void Swap(ref double X, ref double Y)
        {
            Y = Interlocked.Exchange(ref X, Y);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        private static void Swap(ref IntPtr X, ref IntPtr Y)
        {
            Y = Interlocked.Exchange(ref X, Y);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        private static void Swap(ref object X, ref object Y)
        {
            Y = Interlocked.Exchange(ref X, Y);
        }
#endif
        #endregion

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public static bool Not(bool X)
        {
            return !X;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public static bool And(bool X, bool Y)
        {
            return X && Y;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public static bool Or(bool X, bool Y)
        {
            return X || Y;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public static bool Xor(bool X, bool Y)
        {
            return (X || Y) && !(X && Y);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public static bool Imp(bool X, bool Y)
        {
            return !X || Y;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public static int Imp(int X, int Y)
        {
            return ~X | Y;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public static long Imp(long X, long Y)
        {
            return ~X | Y;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public static bool Eqv(bool X, bool Y)
        {
            return (X && Y) || (!X && !Y);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public static int Eqv(int X, int Y)
        {
            return ~(X ^ Y);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public static long Eqv(long X, long Y)
        {
            return ~(X ^ Y);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        #region Dead Code
#if DEAD_CODE
        private static object X(object X, object Y)
        {
            return X;
        }
#endif
        #endregion

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public static object Y(object X, object Y)
        {
            return Y;
        }
    }
}
