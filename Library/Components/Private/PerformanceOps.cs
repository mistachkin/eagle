/*
 * PerformanceOps.cs --
 *
 * Copyright (c) 2007-2012 by Joe Mistachkin.  All rights reserved.
 *
 * See the file "license.terms" for information on usage and redistribution of
 * this file, and for a DISCLAIMER OF ALL WARRANTIES.
 *
 * RCS: @(#) $Id: $
 */

using System;
using System.Runtime.InteropServices;

#if NATIVE
using System.Security;
#endif

#if !NET_40
using System.Security.Permissions;
#endif

using System.Threading;
using Eagle._Attributes;
using Eagle._Components.Public;
using Eagle._Constants;

namespace Eagle._Components.Private
{
#if NATIVE
#if NET_40
    [SecurityCritical()]
#else
    [SecurityPermission(SecurityAction.LinkDemand, UnmanagedCode = true)]
#endif
#endif
    [ObjectId("4e5f0e33-37e0-4d99-a39f-6d0a87ebc487")]
    internal static class PerformanceOps
    {
#if NATIVE
        ///////////////////////////////////////////////////////////////////////
        // Required Native APIs used via P/Invoke
        ///////////////////////////////////////////////////////////////////////

        #region Private Unsafe Native Methods Class
        [SuppressUnmanagedCodeSecurity()]
        [ObjectId("53eaca2a-6ad1-4373-b541-8decca686521")]
        private static class UnsafeNativeMethods
        {
#if WINDOWS
            [DllImport(DllName.Kernel32,
                CallingConvention = CallingConvention.Winapi,
                SetLastError = true)]
            [return: MarshalAs(UnmanagedType.Bool)]
            internal static extern bool QueryPerformanceCounter(
                out long count
            );

            ///////////////////////////////////////////////////////////////////

            [DllImport(DllName.Kernel32,
                CallingConvention = CallingConvention.Winapi,
                SetLastError = true)]
            [return: MarshalAs(UnmanagedType.Bool)]
            internal static extern bool QueryPerformanceFrequency(
                out long frequency
            );
#endif
        }
        #endregion
#endif

        ///////////////////////////////////////////////////////////////////////

        #region Private Constants
        private const long MillisecondsPerSecond = 1000;
        private const long MicrosecondsPerMillisecond = 1000;
        private const long MicrosecondsPerSecond = 1000000;

        ///////////////////////////////////////////////////////////////////////

        private const long TicksPerMicrosecond =
            TimeSpan.TicksPerMillisecond / 1000;
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Private Data
#if NATIVE && WINDOWS
        //
        // HACK: Setting this value to zero will avoid using the Windows
        //       specific native methods.
        //
        private static int UseWindows = 1;
#endif

        ///////////////////////////////////////////////////////////////////////

        //
        // HACK: This value is cached once per AppDomain.  It is the value
        //       returned from the QueryPerformanceFrequency Win32 API.
        //
        private static long CountsPerSecond = 0; /* CACHE */

        ///////////////////////////////////////////////////////////////////////

        //
        // HACK: This is set once per AppDomain.  It is the epoch used when
        //       calculating the "simulated" tick count, e.g. on non-Windows
        //       platforms.
        //
        private static long EpochTicks = 0;
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Private Methods
#if NATIVE && WINDOWS
        private static bool ShouldMaybeUseWindows()
        {
            return Interlocked.CompareExchange(
                ref UseWindows, 0, 0) > 0;
        }

        ///////////////////////////////////////////////////////////////////////

        private static long WindowsGetCount()
        {
            try
            {
                long count;

                if (UnsafeNativeMethods.QueryPerformanceCounter(
                        out count))
                {
                    return count;
                }
            }
            catch (Exception e)
            {
                TraceOps.DebugTrace(
                    e, typeof(PerformanceOps).Name,
                    TracePriority.PerformanceError);
            }

            return 0;
        }

        ///////////////////////////////////////////////////////////////////////

        private static long WindowsGetCountsPerSecond()
        {
            try
            {
                long frequency;

                if (UnsafeNativeMethods.QueryPerformanceFrequency(
                        out frequency))
                {
                    return frequency;
                }
            }
            catch (Exception e)
            {
                TraceOps.DebugTrace(
                    e, typeof(PerformanceOps).Name,
                    TracePriority.PerformanceError);
            }

            return 0;
        }
#endif

        ///////////////////////////////////////////////////////////////////////

        //
        // NOTE: This method determines if the specified value is valid for
        //       a quantity that represents a given duration (e.g. seconds,
        //       milliseconds, or microseconds, etc).  Negative and/or null
        //       values are never valid.  When the "integer" flag is true,
        //       the value must also fit within a signed 32-bit integer.
        //
        private static bool IsValidValue(
            long? value, /* NOTE: Seconds, milliseconds, microseconds, etc. */
            bool integer /* NOTE: When true, value must fit into an Int32. */
            )
        {
            if (value == null)
                return false;

            long localValue = (long)value;

            if (localValue < 0) /* NOTE: Negative duration? */
                return false;

            if (integer && (localValue > int.MaxValue))
                return false;

            return true;
        }

        ///////////////////////////////////////////////////////////////////////

        private static long GetCountsPerSecond(
            bool refresh
            )
        {
            long counts = 0;

            if (!refresh)
            {
                counts = Interlocked.CompareExchange(
                    ref CountsPerSecond, 0, 0);

                if (counts != 0)
                    return counts;
            }

            try
            {
#if NATIVE && WINDOWS
                if (ShouldMaybeUseWindows() &&
                    PlatformOps.IsWindowsOperatingSystem())
                {
                    counts = WindowsGetCountsPerSecond();
                }
                else
#endif
                {
                    //
                    // HACK: On non-Windows, the GetCount() method
                    //       always returns microseconds.
                    //
                    counts = MicrosecondsPerSecond;
                }
            }
            finally
            {
                /* IGNORED */
                Interlocked.CompareExchange(
                    ref CountsPerSecond, counts, 0);
            }

            return counts;
        }

        ///////////////////////////////////////////////////////////////////////

        private static double GetCountsPerMillisecond()
        {
            return (double)GetCountsPerSecond(false) /
                (double)MillisecondsPerSecond;
        }

        ///////////////////////////////////////////////////////////////////////

        private static double GetCountsPerMicrosecond()
        {
            return (double)GetCountsPerSecond(false) /
                (double)MicrosecondsPerSecond;
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Public Methods
        public static int GetMicrosecondsFromMilliseconds(
            int milliseconds,
            int? minimumMicroseconds,
            int? maximumMicroseconds
            ) /* LOSSY */
        {
            long result = GetMicrosecondsFromMilliseconds(milliseconds);

            if (IsValidValue(minimumMicroseconds, true))
            {
                int localMinimumMicroseconds = (int)minimumMicroseconds;

                if (!IsValidValue(result, true) ||
                    (result < localMinimumMicroseconds))
                {
                    result = localMinimumMicroseconds;
                }
            }

            if (IsValidValue(maximumMicroseconds, true))
            {
                int localMaximumMicroseconds = (int)maximumMicroseconds;

                if (!IsValidValue(result, true) ||
                    (result > localMaximumMicroseconds))
                {
                    result = localMaximumMicroseconds;
                }
            }

            if (!IsValidValue(result, true))
                result = 0;

            return ConversionOps.ToInt(result);
        }

        ///////////////////////////////////////////////////////////////////////

        public static double GetMicrosecondsFromCount(
            long startCount,
            long stopCount,
            long iterations,
            bool obfuscate
            )
        {
            return GetMicrosecondsFromCount(
                (stopCount - startCount), iterations, obfuscate);
        }

        ///////////////////////////////////////////////////////////////////////

        public static double GetMicrosecondsFromCount(
            long startCount,
            long stopCount
            )
        {
            return GetMicrosecondsFromCount(
                (stopCount - startCount), 1, false);
        }

        ///////////////////////////////////////////////////////////////////////

        public static double GetMicrosecondsFromCount(
            long count,
            long iterations,
            bool obfuscate
            )
        {
            double result;

            if (iterations <= 1)
            {
                result = count / GetCountsPerMicrosecond();
            }
            else
            {
                result = (count / (double)iterations) /
                    GetCountsPerMicrosecond();
            }

            if (obfuscate)
                result = ObfuscateMicroseconds(result);

            return result;
        }

        ///////////////////////////////////////////////////////////////////////

        public static double ObfuscateMicroseconds(
            double microseconds
            )
        {
            //
            // TODO: Do something more robust here?
            //
            double result = microseconds;

            result = Math.Truncate(result);
            result = result / MicrosecondsPerMillisecond;
            result = Math.Truncate(result);
            result = result * MicrosecondsPerMillisecond;

            return result;
        }

        ///////////////////////////////////////////////////////////////////////

        public static long GetMillisecondsFromMicroseconds(
            long microseconds
            )
        {
            return microseconds / MicrosecondsPerMillisecond;
        }

        ///////////////////////////////////////////////////////////////////////

        public static double GetMillisecondsFromCount(
            long startCount,
            long stopCount,
            long iterations
            )
        {
            double result;

            if (iterations <= 1)
            {
                result = (stopCount - startCount) /
                    GetCountsPerMillisecond();
            }
            else
            {
                result = ((stopCount - startCount) /
                    (double)iterations) / GetCountsPerMillisecond();
            }

            return result;
        }

        ///////////////////////////////////////////////////////////////////////

        public static long GetCountsPerSecond()
        {
            return GetCountsPerSecond(false);
        }

        ///////////////////////////////////////////////////////////////////////

        public static long GetIterations(
            long requestedIterations,
            long actualIterations,
            ReturnCode returnCode,
            bool breakOk
            )
        {
            //
            // NOTE: If the number of iterations requested is less than
            //       negative one, return the absolute value of it.
            //       Effectively, this allows the caller to use a specific
            //       overall divisor, even though only one actual iteration
            //       will take place.  This differs from what Tcl does (i.e.
            //       it treats all negative numbers the same as zero).
            //
            if (requestedIterations < Count.Invalid)
                return Math.Abs(requestedIterations);

            //
            // NOTE: If the number of iterations requested is negative one
            //       (i.e. "run forever until error"), return the number of
            //       iterations actually completed instead.  This differs
            //       from what Tcl does (i.e. it treats all negative numbers
            //       the same as zero).
            //
            if (requestedIterations == Count.Invalid)
                return actualIterations;

            //
            // NOTE: If the number of iterations requested is exactly zero,
            //       just return one.  This is used to measure the overhead
            //       associated with the [time] command infrastructure
            //       (COMPAT: Tcl).
            //
            if (requestedIterations == 0)
                return 1;

            //
            // NOTE: If the return code was Ok, the requested number of
            //       iterations should match the number of iterations
            //       actually completed.
            //
            if (returnCode == ReturnCode.Ok)
                return requestedIterations;

            //
            // NOTE: If the return code was Break, the requested number of
            //       iterations should only be used if the caller requests
            //       it.
            //
            if ((returnCode == ReturnCode.Break) && breakOk)
                return requestedIterations;

            //
            // NOTE: Otherwise, return the number of iterations actually
            //       completed.
            //
            return actualIterations;
        }

        ///////////////////////////////////////////////////////////////////////

        public static long GetTickCount()
        {
#if NET_STANDARD_20 && NET_STANDARD_21 && NET_CORE_30
            //
            // NOTE: This property is available in .NET Core 3.0; however,
            //       it is not included in the .NET Standard 2.1.  To use
            //       this, apparently the TargetFramework for the project
            //       must be (manually) set to "netcoreapp3.0" instead of
            //       "netstandard2.1".
            //
            return Environment.TickCount64;
#else
            return Environment.TickCount;
#endif
        }

        ///////////////////////////////////////////////////////////////////////

        public static long GetMicrosecondsFromMilliseconds(
            long milliseconds
            )
        {
            return milliseconds * MicrosecondsPerMillisecond;
        }

        ///////////////////////////////////////////////////////////////////////

        private static long GetMicrosecondsFromTicks(
            long ticks
            )
        {
            return ticks / TicksPerMicrosecond;
        }

        ///////////////////////////////////////////////////////////////////////

        public static void Initialize()
        {
            Interlocked.CompareExchange(
                ref EpochTicks, TimeOps.GetUtcNowTicks(), 0);
        }

        ///////////////////////////////////////////////////////////////////////

        public static long GetMicroseconds()
        {
            //
            // BUGFIX: Stop relying on the Environment.TickCount property
            //         here as that will overflow after (only) 24.9 days.
            //         Instead, use DateTime.Ticks property values as the
            //         basis.
            //
            long epoch = Interlocked.CompareExchange(ref EpochTicks, 0, 0);

            if (epoch > 0)
            {
                long now = TimeOps.GetUtcNowTicks();
                long ticks = now - epoch;

                if (ticks >= 0)
                {
                    return GetMicrosecondsFromTicks(ticks);
                }
                else
                {
                    TraceOps.DebugTrace(String.Format(
                        "GetMicroseconds: overflowed ticks {0} " +
                        "(epoch {1}, now {2})", ticks, epoch,
                        now), typeof(PerformanceOps).Name,
                        TracePriority.PerformanceError2);
                }

                //
                // NOTE: If the calculated ticks value (somehow) ends up
                //       negative, reset the epoch value to (right) now.
                //       This should, at least, prevent the same thing
                //       from happening again next time.
                //
                /* IGNORED */
                Interlocked.CompareExchange(ref EpochTicks, now, epoch);
            }
            else
            {
                TraceOps.DebugTrace(String.Format(
                    "GetMicroseconds: invalid epoch {0}",
                    epoch), typeof(PerformanceOps).Name,
                    TracePriority.PerformanceError2);
            }

            //
            // NOTE: If the epoch value is not initialized, we can only
            //       return zero.
            //
            return 0;
        }

        ///////////////////////////////////////////////////////////////////////

        public static long GetCount()
        {
#if NATIVE && WINDOWS
            if (ShouldMaybeUseWindows() &&
                PlatformOps.IsWindowsOperatingSystem())
            {
                return WindowsGetCount();
            }
            else
#endif
            {
                //
                // BUGFIX: Result must be in microseconds because the
                //         various callers of this method assume that
                //         when they calculate elapsed time.
                //
                return GetMicroseconds();
            }
        }

        ///////////////////////////////////////////////////////////////////////

        private static double ElapsedMicroseconds(
            long startCount,
            long stopCount
            )
        {
            return GetMicrosecondsFromCount(
                startCount, stopCount, 1, false); /* EXEMPT */
        }

        ///////////////////////////////////////////////////////////////////////

        public static bool HasElapsed(
            long startCount,
            ref long stopCount,
            long waitMicroseconds,
            long slopMicroseconds
            )
        {
            stopCount = GetCount();

            if (stopCount < startCount)
            {
                TraceOps.DebugTrace(String.Format(
                    "HasElapsed: went backward in time? {0} versus {1}",
                    stopCount, startCount), typeof(PerformanceOps).Name,
                    TracePriority.PerformanceError2);

                return true;
            }

            return ElapsedMicroseconds(startCount, stopCount) +
                (double)slopMicroseconds >= (double)waitMicroseconds;
        }
        #endregion
    }
}
