/*
 * ValueData.cs --
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
    [ObjectId("a1b1f063-bd8c-4c64-a6b8-f497892320f2")]
    public interface IValueData : IHaveClientData
    {
        //
        // WARNING: This property is for core/engine use only.
        //
        IClientData ValueData { get; set; }

        //
        // WARNING: This property is for core/engine use only.
        //
        IClientData ExtraData { get; set; }

        //
        // WARNING: This property is for core/engine use only.
        //
        ICallFrame CallFrame { get; set; }
    }
}
