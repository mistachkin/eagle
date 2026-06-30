/*
 * ProfileOps.cs --
 *
 * Copyright (c) 2007-2012 by Joe Mistachkin.  All rights reserved.
 *
 * See the file "license.terms" for information on usage and redistribution of
 * this file, and for a DISCLAIMER OF ALL WARRANTIES.
 *
 * RCS: @(#) $Id: $
 */

using Eagle._Attributes;

namespace Eagle._Components.Private
{
    /// <summary>
    /// This class provides static helper methods used to profile the elapsed
    /// time of arbitrary sections of code, built on top of the performance
    /// counter facilities.
    /// </summary>
    [ObjectId("aca94477-b342-4572-84e2-dd048f215e23")]
    internal static class ProfileOps
    {
        #region Profiling Support Methods
        /// <summary>
        /// This method begins a profiling measurement, resetting the elapsed
        /// time and recording the starting performance counter value.
        /// </summary>
        /// <param name="startCount">
        /// Upon return, receives the starting performance counter value.
        /// </param>
        /// <param name="microseconds">
        /// Upon return, is reset to zero in preparation for accumulating the
        /// elapsed time.
        /// </param>
        public static void Start(
            ref long startCount,    /* out */
            ref double microseconds /* in, out */
            )
        {
            microseconds = 0;

            startCount = PerformanceOps.GetCount();
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method ends a profiling measurement, accumulating the elapsed
        /// time since the specified starting performance counter value.
        /// </summary>
        /// <param name="startCount">
        /// The starting performance counter value previously obtained from the
        /// <see cref="Start" /> method.
        /// </param>
        /// <param name="microseconds">
        /// Upon return, is increased by the elapsed time, in microseconds.
        /// </param>
        public static void Stop(
            long startCount,        /* in */
            ref double microseconds /* in, out */
            )
        {
            Stop(startCount, false /* EXEMPT */, ref microseconds);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method ends a profiling measurement, accumulating the elapsed
        /// time since the specified starting performance counter value, with the
        /// option to obfuscate the resulting value.
        /// </summary>
        /// <param name="startCount">
        /// The starting performance counter value previously obtained from the
        /// <see cref="Start" /> method.
        /// </param>
        /// <param name="obfuscate">
        /// Non-zero to obfuscate the resulting elapsed time before it is
        /// accumulated.
        /// </param>
        /// <param name="microseconds">
        /// Upon return, is increased by the elapsed time, in microseconds.
        /// </param>
        public static void Stop(
            long startCount,        /* in */
            bool obfuscate,         /* in */
            ref double microseconds /* in, out */
            )
        {
            long stopCount = PerformanceOps.GetCount();

            microseconds += PerformanceOps.GetMicrosecondsFromCount(
                startCount, stopCount, 1, obfuscate);
        }
        #endregion
    }
}
