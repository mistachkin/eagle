/*
 * WrapperData.cs --
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
    [ObjectId("eb107969-3e46-4b79-8ba7-3055c2ceb7a3")]
    public interface IWrapperData
    {
        long Token { get; set; }
    }
}
