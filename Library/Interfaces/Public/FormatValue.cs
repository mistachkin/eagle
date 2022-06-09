/*
 * FormatValue.cs --
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
using Eagle._Components.Public;

namespace Eagle._Interfaces.Public
{
    [ObjectId("0defea4e-72fb-4f4b-b491-31e49e4cd802")]
    public interface IFormatValue : IHaveCultureInfo
    {
#if DATA
        DateTimeBehavior DateTimeBehavior { get; set; }
#endif

        DateTimeKind DateTimeKind { get; set; }
        string DateTimeFormat { get; set; }
        string NumberFormat { get; set; }
        string NullValue { get; set; }
        string DbNullValue { get; set; }
        string ErrorValue { get; set; }
    }
}
