/*
 * State.cs --
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
    /// <summary>
    /// This interface is implemented by entities that maintain per-entity
    /// state which must be initialized before use and terminated when it is
    /// no longer needed (e.g. commands and sub-commands).
    /// </summary>
    [ObjectId("3105de58-a7c0-4cf0-bb57-36872afe8942")]
    public interface IState
    {
        /// <summary>
        /// Gets or sets a value indicating whether this entity has been
        /// successfully initialized.
        /// </summary>
        bool Initialized { get; set; }

        /// <summary>
        /// This method is called to initialize the per-entity state prior to
        /// its first use.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter context this entity is being initialized for.  This
        /// parameter should not be null.
        /// </param>
        /// <param name="clientData">
        /// The extra, entity-specific data supplied when the entity was
        /// created, if any.  This parameter may be null.
        /// </param>
        /// <param name="result">
        /// Upon success, this may contain an informational result value.  Upon
        /// failure, this must contain an appropriate error message.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise, a non-Ok value
        /// with details placed in the <paramref name="result" /> parameter.
        /// </returns>
        //
        // TODO: Change these to use the IInterpreter type.
        //
        [Throw(true)]
        ReturnCode Initialize(Interpreter interpreter, IClientData clientData, ref Result result);

        /// <summary>
        /// This method is called to terminate the per-entity state when it is
        /// no longer needed.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter context this entity is being terminated for.  This
        /// parameter should not be null.
        /// </param>
        /// <param name="clientData">
        /// The extra, entity-specific data supplied when the entity was
        /// created, if any.  This parameter may be null.
        /// </param>
        /// <param name="result">
        /// Upon success, this may contain an informational result value.  Upon
        /// failure, this must contain an appropriate error message.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise, a non-Ok value
        /// with details placed in the <paramref name="result" /> parameter.
        /// </returns>
        [Throw(true)]
        ReturnCode Terminate(Interpreter interpreter, IClientData clientData, ref Result result);
    }
}