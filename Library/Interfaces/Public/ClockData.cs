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
    [ObjectId("ce296dcd-d8dc-4768-8aef-cb9184e109b2")]
    public interface IClockData : IIdentifier, IHaveCultureInfo
    {
        TimeZone TimeZone { get; set; } /* NOTE: Can be null for UTC. */
        string Format { get; set; }
        DateTime DateTime { get; set; }
        DateTime Epoch { get; set; }
    }
}
