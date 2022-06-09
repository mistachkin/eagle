/*
 * NewProcedureCallback.cs --
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
    [ObjectId("fbf69595-5dc2-4118-ad35-cf8664f16e2e")]
    public interface INewProcedureCallback
    {
        IProcedure NewProcedure(
            Interpreter interpreter,
            IProcedureData procedureData,
            ref Result error
        );
    }
}
