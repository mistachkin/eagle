/*
 * Execute.cs --
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
    /// This interface is implemented by all entities that the Eagle engine
    /// can execute, including commands, sub-commands, procedures, aliases,
    /// and operators.  It defines the single entry point,
    /// <see cref="Execute" />, that the engine uses to invoke the entity
    /// with its arguments and to obtain a result.  See
    /// <c>core_language.md</c> for how a parsed script token becomes an
    /// <see cref="Execute" /> invocation.
    /// </summary>
    [ObjectId("96e07242-2bd0-47ea-b93a-b98943222407")]
    public interface IExecute
    {
        /// <summary>
        /// This method is called by the engine to execute the entity for a
        /// single invocation.  It receives the fully substituted arguments
        /// and reports its outcome both through the returned
        /// <see cref="ReturnCode" /> and through the
        /// <paramref name="result" /> parameter.
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
        /// zero is the identifier name as actually invoked (i.e. the command
        /// or sub-command name); the remaining elements are its arguments.
        /// This parameter should not be null.
        /// </param>
        /// <param name="result">
        /// Upon success, this may contain the result value produced by the
        /// entity.  Upon failure, this must contain an appropriate error
        /// message.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise, a non-Ok
        /// value (e.g. <see cref="ReturnCode.Error" />) with details placed
        /// in the <paramref name="result" /> parameter.  The control-flow
        /// values <see cref="ReturnCode.Break" />,
        /// <see cref="ReturnCode.Continue" />, and
        /// <see cref="ReturnCode.Return" /> may also be returned.
        /// </returns>
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
        ReturnCode Execute(
            Interpreter interpreter, /* in */
            IClientData clientData,  /* in */
            ArgumentList arguments,  /* in */
            ref Result result        /* out */
            );
    }
}
