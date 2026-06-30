/*
 * ClockData.cs --
 *
 * Copyright (c) 2007-2012 by Joe Mistachkin.  All rights reserved.
 *
 * See the file "license.terms" for information on usage and redistribution of
 * this file, and for a DISCLAIMER OF ALL WARRANTIES.
 *
 * RCS: @(#) $Id: $
 */

using System;
using Eagle._Attributes;

namespace Eagle._Interfaces.Public
{
    /// <summary>
    /// This interface holds the contextual data used by the Eagle clock
    /// subsystem when formatting and scanning date and time values.  It
    /// composes a unique identity (<see cref="IIdentifier" />) and an
    /// associated culture (<see cref="IHaveCultureInfo" />), and supplies the
    /// time zone, format string, value, and epoch that drive a clock
    /// operation.
    /// </summary>
    [ObjectId("ce296dcd-d8dc-4768-8aef-cb9184e109b2")]
    public interface IClockData : IIdentifier, IHaveCultureInfo
    {
        /// <summary>
        /// Gets or sets the time zone used to interpret the date and time
        /// value.  This value can be null, in which case Coordinated
        /// Universal Time (UTC) is assumed.
        /// </summary>
        TimeZone TimeZone { get; set; } /* NOTE: Can be null for UTC. */
        /// <summary>
        /// Gets or sets the format string used when formatting or scanning
        /// the date and time value.
        /// </summary>
        string Format { get; set; }
        /// <summary>
        /// Gets or sets the date and time value associated with this clock
        /// operation.
        /// </summary>
        DateTime DateTime { get; set; }
        /// <summary>
        /// Gets or sets the epoch (i.e. the base date and time) that relative
        /// values are measured from.
        /// </summary>
        DateTime Epoch { get; set; }
    }
}
