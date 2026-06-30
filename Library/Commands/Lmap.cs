/*
 * Lmap.cs --
 *
 * Copyright (c) 2007-2012 by Joe Mistachkin.  All rights reserved.
 *
 * See the file "license.terms" for information on usage and redistribution of
 * this file, and for a DISCLAIMER OF ALL WARRANTIES.
 *
 * RCS: @(#) $Id: $
 */

using Eagle._Attributes;
using Eagle._Components.Private;
using Eagle._Components.Public;
using Eagle._Containers.Public;
using Eagle._Interfaces.Public;

namespace Eagle._Commands
{
    /// <summary>
    /// This class implements the Eagle <c>lmap</c> command, which iterates
    /// over one or more lists, evaluating a body script for each set of
    /// elements and collecting the results of those evaluations into a new
    /// list.  See <c>core_language.md</c> for the command syntax and
    /// semantics.
    /// </summary>
    [ObjectId("59e5c17e-d957-4bec-948c-7bce0e439e82")]
    [CommandFlags(CommandFlags.Safe | CommandFlags.Standard)]
    [ObjectGroup("loop")]
    internal sealed class Lmap : Core
    {
        #region Public Constructors
        /// <summary>
        /// Constructs an instance of the <c>lmap</c> command.
        /// </summary>
        /// <param name="commandData">
        /// The data used to create and identify this command, such as its
        /// name and flags.  This parameter may be null.
        /// </param>
        public Lmap(
            ICommandData commandData
            )
            : base(commandData)
        {
            // do nothing.
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region IExecute Members
        /// <summary>
        /// This method executes the <c>lmap</c> command.  It iterates over the
        /// supplied variable name and list pairs in lock-step, evaluating the
        /// body script once per iteration, and collects the result of each
        /// evaluation into a list that becomes the command result.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter context this command is executing in.  This
        /// parameter should not be null.
        /// </param>
        /// <param name="clientData">
        /// The extra, command-specific data supplied when this command was
        /// created, if any.  This parameter may be null.
        /// </param>
        /// <param name="arguments">
        /// The list of arguments for this invocation.  Element zero is the
        /// command name; the following elements are one or more variable name
        /// and list pairs, and the final element is the body script to
        /// evaluate for each iteration.  This parameter should not be null.
        /// </param>
        /// <param name="result">
        /// Upon success, this contains the list assembled from the per-
        /// iteration body results.  Upon failure, this contains an
        /// appropriate error message.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success, with the collected list
        /// placed in <paramref name="result" />; otherwise, a non-Ok value
        /// (e.g. <see cref="ReturnCode.Error" />) with details placed in
        /// <paramref name="result" />.
        /// </returns>
        public override ReturnCode Execute(
            Interpreter interpreter, /* in */
            IClientData clientData,  /* in */
            ArgumentList arguments,  /* in */
            ref Result result        /* out */
            )
        {
            return ScriptOps.EachLoopCommand(
                this, true, interpreter, clientData, arguments, ref result);
        }
        #endregion
    }
}
