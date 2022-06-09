/*
 * FormatDataValue.cs --
 *
 * Copyright (c) 2007-2012 by Joe Mistachkin.  All rights reserved.
 *
 * See the file "license.terms" for information on usage and redistribution of
 * this file, and for a DISCLAIMER OF ALL WARRANTIES.
 *
 * RCS: @(#) $Id: $
 */

using Eagle._Attributes;

namespace Eagle._Interfaces.Public
{
    [ObjectId("a795a441-63e9-4959-ae8c-fea686007dff")]
    public interface IFormatDataValue : IFormatValue
    {
        int Limit { get; set; }
        bool Nested { get; set; }
        bool Clear { get; set; }
        bool AllowNull { get; set; }
        bool Pairs { get; set; }
        bool Names { get; set; }
    }
}
