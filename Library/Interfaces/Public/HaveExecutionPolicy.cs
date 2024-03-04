/*
 * HaveExecutionPolicy.cs --
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
    [ObjectId("fec373c0-eb9e-48ce-bc39-cf52eff96c29")]
    public interface IHaveExecutionPolicy
    {
        PolicyType? PolicyType { get; set; }
        ExecutionPolicy? ExecutionPolicy { get; set; }
    }
}
