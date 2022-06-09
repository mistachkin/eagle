/*
 * GetInterpreter.cs --
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
    [ObjectId("f1818462-b697-4504-bb49-cfe3c2dd6c68")]
    public interface IGetInterpreter
    {
        //
        // TODO: Change this to use the IInterpreter type.
        //
        Interpreter Interpreter { get; }
    }
}
