/*
 * HaveInterpreter.cs --
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
    [ObjectId("9c0fb79d-c93e-4a93-b2ed-38625a4fc6a9")]
    public interface IHaveInterpreter : IGetInterpreter, ISetInterpreter
    {
        //
        // TODO: Change this to use the IInterpreter type.
        //
        new Interpreter Interpreter { get; set; }
    }
}
