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
    /// <summary>
    /// This interface is implemented by entities that, when invoked, produce a
    /// single <see cref="Argument" /> value rather than a textual
    /// <see cref="Result" />.  It defines the single entry point,
    /// <see cref="Execute" />, that the engine uses to invoke the entity with
    /// its arguments and obtain the resulting value.
    /// </summary>
    [ObjectId("bc3d2209-6a46-4e27-b09b-661f054f052d")]
    public interface IExecuteArgument
    {
        /// <summary>
        /// This method is called by the engine to execute the entity for a
        /// single invocation, producing an <see cref="Argument" /> value as
        /// its result.  It receives the fully substituted arguments and
        /// reports its outcome both through the returned
        /// <see cref="ReturnCode" /> and through the <paramref name="value" />
        /// parameter.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter context this entity is executing in.  This
        /// parameter should not be null.
        /// </param>
        /// <param name="clientData">
        /// The extra, entity-specific data supplied when the entity was
        /// created, if any.  This parameter may be null.
        /// </param>
        /// <param name="arguments">
        /// The list of arguments for this invocation.  The element at index
        /// zero is the identifier name as actually invoked; the remaining
        /// elements are its arguments.  This parameter should not be null.
        /// </param>
        /// <param name="value">
        /// Upon success, this contains the argument value produced by the
        /// entity.
        /// </param>
        /// <param name="error">
        /// Upon failure, this contains an appropriate error message.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise, a non-Ok
        /// value with details placed in the <paramref name="error" />
        /// parameter.
        /// </returns>
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
