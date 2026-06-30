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
    /// <summary>
    /// This class provides the central set of helper methods used to measure
    /// elapsed time and convert between the various units of time (e.g.
    /// seconds, milliseconds, microseconds, ticks, and raw performance
    /// counts) used by the timing and benchmarking infrastructure (e.g. the
    /// <c>[time]</c> command).  On Windows, the high-resolution performance
    /// counter may be used via P/Invoke; on other platforms, a simulated
    /// microsecond-based count derived from the system clock is used instead.
    /// </summary>
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
        /// <summary>
        /// This class contains the native Win32 APIs, used via P/Invoke, that
        /// are required to access the high-resolution performance counter on
        /// the Windows operating system.
        /// </summary>
        [SuppressUnmanagedCodeSecurity()]
        [ObjectId("53eaca2a-6ad1-4373-b541-8decca686521")]
        private static class UnsafeNativeMethods
        {
#if WINDOWS
            /// <summary>
            /// This method wraps the native Win32 QueryPerformanceCounter API,
            /// which retrieves the current value of the high-resolution
            /// performance counter.
            /// </summary>
            /// <param name="count">
            /// Upon success, receives the current value of the high-resolution
            /// performance counter.
            /// </param>
            /// <returns>
            /// True if the counter value was successfully retrieved; otherwise,
            /// false.
            /// </returns>
            [DllImport(DllName.Kernel32,
                CallingConvention = CallingConvention.Winapi,
                SetLastError = true)]
            [return: MarshalAs(UnmanagedType.Bool)]
            internal static extern bool QueryPerformanceCounter(
                out long count
            );

            ///////////////////////////////////////////////////////////////////

            /// <summary>
            /// This method wraps the native Win32 QueryPerformanceFrequency
            /// API, which retrieves the frequency (in counts per second) of the
            /// high-resolution performance counter.
            /// </summary>
            /// <param name="frequency">
            /// Upon success, receives the frequency, in counts per second, of
            /// the high-resolution performance counter.
            /// </param>
            /// <returns>
            /// True if the frequency was successfully retrieved; otherwise,
            /// false.
            /// </returns>
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
        /// <summary>
        /// The number of milliseconds in one second.
        /// </summary>
        private const long MillisecondsPerSecond = 1000;

        /// <summary>
        /// The number of microseconds in one millisecond.
        /// </summary>
        private const long MicrosecondsPerMillisecond = 1000;

        /// <summary>
        /// The number of microseconds in one second.
        /// </summary>
        private const long MicrosecondsPerSecond = 1000000;

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// The number of clock ticks (i.e. units of one hundred nanoseconds)
        /// in one microsecond.
        /// </summary>
        private const long TicksPerMicrosecond =
            TimeSpan.TicksPerMillisecond / 1000;
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Private Data
#if NATIVE && WINDOWS
        /// <summary>
        /// When non-zero, the Windows-specific native methods (i.e. the
        /// high-resolution performance counter) may be used; setting this value
        /// to zero disables them.
        /// </summary>
        //
        // HACK: Setting this value to zero will avoid using the Windows
        //       specific native methods.
        //
        private static int UseWindows = 1;
#endif

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// The cached number of performance counts per second.  This value is
        /// cached once per application domain; on Windows it is the value
        /// returned from the QueryPerformanceFrequency Win32 API, and on other
        /// platforms it is the number of microseconds per second.
        /// </summary>
        //
        // HACK: This value is cached once per AppDomain.  It is the value
        //       returned from the QueryPerformanceFrequency Win32 API.
        //
        private static long CountsPerSecond = 0; /* CACHE */

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// The epoch, in clock ticks, used when calculating the "simulated"
        /// tick count (e.g. on non-Windows platforms).  This value is set once
        /// per application domain.
        /// </summary>
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
        /// <summary>
        /// This method determines whether the Windows-specific native methods
        /// (i.e. the high-resolution performance counter) should potentially be
        /// used, based on the current value of the <c>UseWindows</c> field.
        /// </summary>
        /// <returns>
        /// True if the Windows-specific native methods may be used; otherwise,
        /// false.
        /// </returns>
        private static bool ShouldMaybeUseWindows()
        {
            return Interlocked.CompareExchange(
                ref UseWindows, 0, 0) > 0;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method retrieves the current value of the Windows
        /// high-resolution performance counter via the native
        /// QueryPerformanceCounter Win32 API.
        /// </summary>
        /// <returns>
        /// The current value of the high-resolution performance counter, or
        /// zero if it could not be retrieved.
        /// </returns>
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

        /// <summary>
        /// This method retrieves the frequency, in counts per second, of the
        /// Windows high-resolution performance counter via the native
        /// QueryPerformanceFrequency Win32 API.
        /// </summary>
        /// <returns>
        /// The frequency, in counts per second, of the high-resolution
        /// performance counter, or zero if it could not be retrieved.
        /// </returns>
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

        /// <summary>
        /// This method determines whether the specified value is valid for a
        /// quantity that represents a given duration (e.g. seconds,
        /// milliseconds, or microseconds, etc).  Negative and/or null values
        /// are never valid.
        /// </summary>
        /// <param name="value">
        /// The duration value (e.g. seconds, milliseconds, microseconds, etc)
        /// to be validated.
        /// </param>
        /// <param name="integer">
        /// When true, the value must also fit within a signed 32-bit integer.
        /// </param>
        /// <returns>
        /// True if the specified value is valid; otherwise, false.
        /// </returns>
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

        /// <summary>
        /// This method gets the number of performance counts per second,
        /// optionally refreshing the cached value.  On Windows the value comes
        /// from the high-resolution performance counter; on other platforms it
        /// is the number of microseconds per second.
        /// </summary>
        /// <param name="refresh">
        /// When true, the cached value is ignored and recalculated; otherwise,
        /// any previously cached value is returned.
        /// </param>
        /// <returns>
        /// The number of performance counts per second.
        /// </returns>
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

        /// <summary>
        /// This method gets the number of performance counts per millisecond.
        /// </summary>
        /// <returns>
        /// The number of performance counts per millisecond.
        /// </returns>
        private static double GetCountsPerMillisecond()
        {
            return (double)GetCountsPerSecond(false) /
                (double)MillisecondsPerSecond;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method gets the number of performance counts per microsecond.
        /// </summary>
        /// <returns>
        /// The number of performance counts per microsecond.
        /// </returns>
        private static double GetCountsPerMicrosecond()
        {
            return (double)GetCountsPerSecond(false) /
                (double)MicrosecondsPerSecond;
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Public Methods
        /// <summary>
        /// This method converts the specified number of milliseconds into
        /// microseconds, clamping the result to the optional minimum and
        /// maximum bounds.  The result is coerced to a signed 32-bit integer
        /// and may therefore be lossy.
        /// </summary>
        /// <param name="milliseconds">
        /// The number of milliseconds to convert into microseconds.
        /// </param>
        /// <param name="minimumMicroseconds">
        /// The optional minimum number of microseconds; when valid, the result
        /// will not be less than this value.
        /// </param>
        /// <param name="maximumMicroseconds">
        /// The optional maximum number of microseconds; when valid, the result
        /// will not be greater than this value.
        /// </param>
        /// <returns>
        /// The number of microseconds, clamped to the specified bounds.
        /// </returns>
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

        /// <summary>
        /// This method calculates the number of microseconds that elapsed
        /// between the specified starting and stopping performance counts,
        /// averaged over the specified number of iterations.
        /// </summary>
        /// <param name="startCount">
        /// The performance count captured at the start of the measured
        /// interval.
        /// </param>
        /// <param name="stopCount">
        /// The performance count captured at the end of the measured interval.
        /// </param>
        /// <param name="iterations">
        /// The number of iterations to average the elapsed time over; values of
        /// one or less are treated as a single iteration.
        /// </param>
        /// <param name="obfuscate">
        /// When true, the resulting number of microseconds is obfuscated to
        /// reduce its precision.
        /// </param>
        /// <returns>
        /// The number of microseconds, per iteration, that elapsed during the
        /// measured interval.
        /// </returns>
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

        /// <summary>
        /// This method calculates the number of microseconds that elapsed
        /// between the specified starting and stopping performance counts.
        /// </summary>
        /// <param name="startCount">
        /// The performance count captured at the start of the measured
        /// interval.
        /// </param>
        /// <param name="stopCount">
        /// The performance count captured at the end of the measured interval.
        /// </param>
        /// <returns>
        /// The number of microseconds that elapsed during the measured
        /// interval.
        /// </returns>
        public static double GetMicrosecondsFromCount(
            long startCount,
            long stopCount
            )
        {
            return GetMicrosecondsFromCount(
                (stopCount - startCount), 1, false);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method calculates the number of microseconds represented by the
        /// specified raw performance count, averaged over the specified number
        /// of iterations.
        /// </summary>
        /// <param name="count">
        /// The raw performance count (i.e. the difference between a stopping and
        /// a starting count) to convert into microseconds.
        /// </param>
        /// <param name="iterations">
        /// The number of iterations to average the elapsed time over; values of
        /// one or less are treated as a single iteration.
        /// </param>
        /// <param name="obfuscate">
        /// When true, the resulting number of microseconds is obfuscated to
        /// reduce its precision.
        /// </param>
        /// <returns>
        /// The number of microseconds, per iteration, represented by the
        /// specified count.
        /// </returns>
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

        /// <summary>
        /// This method obfuscates the specified number of microseconds by
        /// reducing its precision to whole milliseconds (expressed in
        /// microseconds).
        /// </summary>
        /// <param name="microseconds">
        /// The number of microseconds to obfuscate.
        /// </param>
        /// <returns>
        /// The obfuscated number of microseconds, truncated to whole
        /// milliseconds.
        /// </returns>
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

        /// <summary>
        /// This method converts the specified number of microseconds into whole
        /// milliseconds.
        /// </summary>
        /// <param name="microseconds">
        /// The number of microseconds to convert into milliseconds.
        /// </param>
        /// <returns>
        /// The number of whole milliseconds.
        /// </returns>
        public static long GetMillisecondsFromMicroseconds(
            long microseconds
            )
        {
            return microseconds / MicrosecondsPerMillisecond;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method calculates the number of milliseconds that elapsed
        /// between the specified starting and stopping performance counts,
        /// averaged over the specified number of iterations.
        /// </summary>
        /// <param name="startCount">
        /// The performance count captured at the start of the measured
        /// interval.
        /// </param>
        /// <param name="stopCount">
        /// The performance count captured at the end of the measured interval.
        /// </param>
        /// <param name="iterations">
        /// The number of iterations to average the elapsed time over; values of
        /// one or less are treated as a single iteration.
        /// </param>
        /// <returns>
        /// The number of milliseconds, per iteration, that elapsed during the
        /// measured interval.
        /// </returns>
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

        /// <summary>
        /// This method gets the number of performance counts per second, using
        /// any previously cached value.
        /// </summary>
        /// <returns>
        /// The number of performance counts per second.
        /// </returns>
        public static long GetCountsPerSecond()
        {
            return GetCountsPerSecond(false);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method determines the effective number of iterations to report
        /// for a timed operation, based on the requested and actually completed
        /// iteration counts and the return code of the operation.  This logic
        /// differs from Tcl in its handling of negative requested iteration
        /// counts.
        /// </summary>
        /// <param name="requestedIterations">
        /// The number of iterations originally requested by the caller.  Values
        /// less than negative one return their absolute value, negative one
        /// returns the number of iterations actually completed, and zero
        /// returns one.
        /// </param>
        /// <param name="actualIterations">
        /// The number of iterations that were actually completed.
        /// </param>
        /// <param name="returnCode">
        /// The <see cref="ReturnCode" /> produced by the timed operation.
        /// </param>
        /// <param name="breakOk">
        /// When true, a <see cref="ReturnCode.Break" /> result causes the
        /// requested number of iterations to be returned.
        /// </param>
        /// <returns>
        /// The effective number of iterations to report.
        /// </returns>
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

        /// <summary>
        /// This method gets the number of milliseconds elapsed since the system
        /// started, using the most precise tick count available for the current
        /// target framework.
        /// </summary>
        /// <returns>
        /// The system tick count, in milliseconds.
        /// </returns>
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

        /// <summary>
        /// This method converts the specified number of milliseconds into
        /// microseconds.
        /// </summary>
        /// <param name="milliseconds">
        /// The number of milliseconds to convert into microseconds.
        /// </param>
        /// <returns>
        /// The number of microseconds.
        /// </returns>
        public static long GetMicrosecondsFromMilliseconds(
            long milliseconds
            )
        {
            return milliseconds * MicrosecondsPerMillisecond;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method converts the specified number of clock ticks (i.e. units
        /// of one hundred nanoseconds) into whole microseconds.
        /// </summary>
        /// <param name="ticks">
        /// The number of clock ticks to convert into microseconds.
        /// </param>
        /// <returns>
        /// The number of whole microseconds.
        /// </returns>
        private static long GetMicrosecondsFromTicks(
            long ticks
            )
        {
            return ticks / TicksPerMicrosecond;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method initializes the epoch, in clock ticks, used as the basis
        /// for calculating elapsed microseconds.  The epoch is established only
        /// once per application domain.
        /// </summary>
        public static void Initialize()
        {
            Interlocked.CompareExchange(
                ref EpochTicks, TimeOps.GetUtcNowTicks(), 0);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method gets the number of microseconds that have elapsed since
        /// the epoch was initialized, based on the current value of the system
        /// clock.  This avoids relying on the Environment.TickCount property,
        /// which overflows after roughly 24.9 days.
        /// </summary>
        /// <returns>
        /// The number of microseconds elapsed since the epoch, or zero if the
        /// epoch has not been initialized.
        /// </returns>
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

        /// <summary>
        /// This method gets the current performance count.  On Windows the
        /// high-resolution performance counter may be used; on other platforms
        /// the result is the current number of elapsed microseconds.
        /// </summary>
        /// <returns>
        /// The current performance count.
        /// </returns>
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

        /// <summary>
        /// This method calculates the number of microseconds that elapsed
        /// between the specified starting and stopping performance counts.
        /// </summary>
        /// <param name="startCount">
        /// The performance count captured at the start of the measured
        /// interval.
        /// </param>
        /// <param name="stopCount">
        /// The performance count captured at the end of the measured interval.
        /// </param>
        /// <returns>
        /// The number of microseconds that elapsed during the measured
        /// interval.
        /// </returns>
        private static double ElapsedMicroseconds(
            long startCount,
            long stopCount
            )
        {
            return GetMicrosecondsFromCount(
                startCount, stopCount, 1, false); /* EXEMPT */
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method determines whether at least the specified wait interval,
        /// in microseconds, has elapsed since the specified starting count,
        /// taking an allowed slop interval into account.  The current
        /// performance count is captured as part of this determination.
        /// </summary>
        /// <param name="startCount">
        /// The performance count captured at the start of the measured
        /// interval.
        /// </param>
        /// <param name="stopCount">
        /// Upon return, receives the current performance count captured by this
        /// method.
        /// </param>
        /// <param name="waitMicroseconds">
        /// The number of microseconds that must elapse before this method
        /// reports the interval as having elapsed.
        /// </param>
        /// <param name="slopMicroseconds">
        /// An additional number of microseconds, treated as already elapsed,
        /// allowing the wait interval to be satisfied slightly early.
        /// </param>
        /// <returns>
        /// True if the wait interval has elapsed (or time appeared to move
        /// backward); otherwise, false.
        /// </returns>
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
