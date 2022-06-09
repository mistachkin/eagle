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
    [ObjectId("aca94477-b342-4572-84e2-dd048f215e23")]
    internal static class ProfileOps
    {
        #region Profiling Support Methods
        public static void Start(
            ref long startCount,    /* out */
            ref double microseconds /* in, out */
            )
        {
            microseconds = 0;

            startCount = PerformanceOps.GetCount();
        }

        ///////////////////////////////////////////////////////////////////////

        public static void Stop(
            long startCount,        /* in */
            ref double microseconds /* in, out */
            )
        {
            Stop(startCount, false /* EXEMPT */, ref microseconds);
        }

        ///////////////////////////////////////////////////////////////////////

        public static void Stop(
            long startCount,        /* in */
            bool obfuscate,         /* in */
            ref double microseconds /* in, out */
            )
        {
            long stopCount = PerformanceOps.GetCount();

            microseconds += PerformanceOps.GetMicroseconds(
                startCount, stopCount, 1, obfuscate);
        }
        #endregion
    }
}
