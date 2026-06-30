/*
 * UsageData.cs --
 *
 * Copyright (c) 2007-2012 by Joe Mistachkin.  All rights reserved.
 *
 * See the file "license.terms" for information on usage and redistribution of
 * this file, and for a DISCLAIMER OF ALL WARRANTIES.
 *
 * RCS: @(#) $Id: $
 */

using Eagle._Attributes;
using Eagle._Components.Public;

namespace Eagle._Interfaces.Public
{
    /// <summary>
    /// This interface is implemented by entities that track usage statistics,
    /// such as how many times they have been invoked and the total amount of
    /// time spent during those invocations.
    /// </summary>
    [ObjectId("217ff750-08b8-49c1-a172-dd5c36a80a8f")]
    public interface IUsageData
    {
        //
        // WARNING: *EXPERIMENTAL* This interface may still change radically.
        //
        /// <summary>
        /// Resets the usage statistic of the specified type to its default
        /// value.
        /// </summary>
        /// <param name="type">
        /// The type of usage statistic to reset.
        /// </param>
        /// <param name="value">
        /// Upon success, receives the previous value of the usage statistic,
        /// prior to it being reset.
        /// </param>
        /// <returns>
        /// True if the usage statistic was reset; otherwise, false.
        /// </returns>
        bool ResetUsage(UsageType type, ref long value);
        /// <summary>
        /// Gets the current value of the usage statistic of the specified
        /// type.
        /// </summary>
        /// <param name="type">
        /// The type of usage statistic to query.
        /// </param>
        /// <param name="value">
        /// Upon success, receives the current value of the usage statistic.
        /// </param>
        /// <returns>
        /// True if the usage statistic was queried; otherwise, false.
        /// </returns>
        bool GetUsage(UsageType type, ref long value);
        /// <summary>
        /// Sets the value of the usage statistic of the specified type.
        /// </summary>
        /// <param name="type">
        /// The type of usage statistic to set.
        /// </param>
        /// <param name="value">
        /// Upon entry, the new value for the usage statistic.  Upon success,
        /// receives the previous value of the usage statistic.
        /// </param>
        /// <returns>
        /// True if the usage statistic was set; otherwise, false.
        /// </returns>
        bool SetUsage(UsageType type, ref long value);
        /// <summary>
        /// Adds the specified amount to the usage statistic of the specified
        /// type.
        /// </summary>
        /// <param name="type">
        /// The type of usage statistic to modify.
        /// </param>
        /// <param name="value">
        /// Upon entry, the amount to add to the usage statistic.  Upon
        /// success, receives the new value of the usage statistic.
        /// </param>
        /// <returns>
        /// True if the usage statistic was modified; otherwise, false.
        /// </returns>
        bool AddUsage(UsageType type, ref long value);

        //
        // WARNING: These methods are only intended for use by the core script
        //          engine itself (i.e. the "Engine" class).
        //
        /// <summary>
        /// Adds the specified amount to the invocation count usage statistic.
        /// </summary>
        /// <param name="count">
        /// Upon entry, the amount to add to the invocation count.  Upon
        /// success, receives the new invocation count.
        /// </param>
        /// <returns>
        /// True if the invocation count was modified; otherwise, false.
        /// </returns>
        bool CountUsage(ref long count);
        /// <summary>
        /// Adds the specified number of microseconds to the elapsed time usage
        /// statistic.
        /// </summary>
        /// <param name="microseconds">
        /// Upon entry, the number of microseconds to add to the elapsed time.
        /// Upon success, receives the new elapsed time, in microseconds.
        /// </param>
        /// <returns>
        /// True if the elapsed time was modified; otherwise, false.
        /// </returns>
        bool ProfileUsage(ref long microseconds);
    }
}
