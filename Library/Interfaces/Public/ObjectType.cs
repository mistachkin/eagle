/*
 * ObjectType.cs --
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
    [ObjectId("8388e998-f8d7-4deb-b664-9aa88aefa54a")]
    public interface IObjectType : IObjectTypeData
    {
        //
        // TODO: Change these to use the IInterpreter type.
        //
        [Throw(true)]
        ReturnCode SetFromAny(Interpreter interpreter, string text,
            ref IntPtr value, ref Result error);

        [Throw(true)]
        ReturnCode UpdateString(Interpreter interpreter, ref string text,
            IntPtr value, ref Result error);

        [Throw(true)]
        ReturnCode Duplicate(Interpreter interpreter, IntPtr oldValue,
            ref IntPtr newValue, ref Result error);

        [Throw(true)]
        ReturnCode Shimmer(Interpreter interpreter, string text,
            ref IntPtr value, ref Result error); // a.k.a. FreeInternalRep
    }
}
