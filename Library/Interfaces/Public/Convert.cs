/*
 * Convert.cs --
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
    [ObjectId("7d1043f5-12a6-458e-b927-5b202671b73d")]
    public interface IConvert : IValue
    {
        bool MatchNumberType(NumberType numberType);
        bool MatchTypeCode(TypeCode typeCode);

        bool ConvertTo(TypeCode typeCode);
        bool ConvertTo(Type type);

        bool MaybeConvertWith(
            IConvert convert2,
            bool skip1,
            bool skip2
        );
    }
}
