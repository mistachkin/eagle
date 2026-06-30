/*
 * UnknownCallback.cs --
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
    /// This interface is implemented by entities that wish to resolve a
    /// command (or other executable entity) that was not otherwise found,
    /// providing a last-chance hook analogous to the Tcl <c>unknown</c>
    /// command.
    /// </summary>
    [ObjectId("43b49d67-f5bf-4d22-b564-6651105bd67f")]
    public interface IUnknownCallback
    {
        /// <summary>
        /// This method is called when an executable entity cannot be resolved,
        /// giving this callback the opportunity to supply one.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter context this resolution is occurring in.  This
        /// parameter should not be null.
        /// </param>
        /// <param name="engineFlags">
        /// The engine flags in effect for this resolution.
        /// </param>
        /// <param name="name">
        /// The name of the entity that could not be resolved.  This parameter
        /// should not be null.
        /// </param>
        /// <param name="arguments">
        /// The list of arguments for the invocation that triggered this
        /// resolution, if any.  This parameter may be null.
        /// </param>
        /// <param name="lookupFlags">
        /// The lookup flags that control how the entity is resolved.
        /// </param>
        /// <param name="ambiguous">
        /// Upon return, non-zero if the name was ambiguous (i.e. matched more
        /// than one entity).
        /// </param>
        /// <param name="execute">
        /// Upon success, receives the resolved executable entity.
        /// </param>
        /// <param name="error">
        /// Upon failure, this must contain an appropriate error message.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise, a non-Ok value
        /// with details placed in the <paramref name="error" /> parameter.
        /// </returns>
        ReturnCode Unknown(
            Interpreter interpreter, // TODO: Change to use the IInterpreter type.
            EngineFlags engineFlags,
            string name,
            ArgumentList arguments,
            LookupFlags lookupFlags,
            ref bool ambiguous,
            ref IExecute execute,
            ref Result error
        );
    }
}
