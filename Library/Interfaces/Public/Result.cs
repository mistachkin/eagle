/*
 * Result.cs --
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
    [ObjectId("760b31ee-4fac-47b1-a331-852c67d80102")]
    public interface IResult : IValue, IValueData, IError
    {
        ResultFlags Flags { get; set; }
        void Reset(ResultFlags flags);
        IResult Copy(ResultFlags flags);

        bool HasFlags(ResultFlags hasFlags, bool all);
    }
}
