/*
 * SetInterpreter.cs --
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
    [ObjectId("07bf3e68-f141-4ebb-9e2a-14ca2a8f722b")]
    public interface ISetInterpreter
    {
        //
        // TODO: Change this to use the IInterpreter type.
        //
        Interpreter Interpreter { set; }
    }
}
