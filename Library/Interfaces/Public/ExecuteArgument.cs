/*
 * ExecuteArgument.cs --
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
using Eagle._Containers.Public;

namespace Eagle._Interfaces.Public
{
    [ObjectId("bc3d2209-6a46-4e27-b09b-661f054f052d")]
    public interface IExecuteArgument
    {
        //
        // NOTE: The arguments[0] value is current identifier name (as invoked).
        //
        // TODO: Change this to use the IInterpreter type as the first argument.
        //
        //       This means that all core commands that access non-public and/or 
        //       non-interface members of Interpreter need to be modified to use 
        //       Interpreter.IsValid(interpreter) instead of checking for 
        //       (interpreter != null) and that they must internally cast their 
        //       IInterpreter argument to an Interpreter before using it.
        //
        [Throw(true)]
        ReturnCode Execute(Interpreter interpreter, IClientData clientData, 
            ArgumentList arguments, ref Argument value, ref Result error);
    }
}
