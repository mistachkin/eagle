/*
 * Foreach.cs --
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
    /// This class implements the Eagle <c>foreach</c> command, which iterates
    /// over the elements of one or more lists, assigning successive elements
    /// to the named loop variables and evaluating a body script for each
    /// iteration.  See <c>core_language.md</c> for the command syntax and
    /// semantics.
    /// </summary>
    [ObjectId("7aa801c2-9179-4726-a536-704063349abd")]
    [CommandFlags(
        CommandFlags.Safe | CommandFlags.Standard |
        CommandFlags.SecuritySdk)]
    [ObjectGroup("loop")]
    internal sealed class Foreach : Core
    {
        #region Public Constructors
        /// <summary>
        /// Constructs an instance of the <c>foreach</c> command.
        /// </summary>
        /// <param name="commandData">
        /// The data used to create and identify this command, such as its
        /// name and flags.  This parameter may be null.
        /// </param>
        public Foreach(
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
        /// This method executes the <c>foreach</c> command.  It iterates over
        /// the supplied value lists in lock-step, assigning successive
        /// elements to the corresponding loop variables and evaluating the
        /// body script once per iteration.
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
        /// command name, followed by one or more variable-list / value-list
        /// pairs and, finally, the body script to evaluate for each
        /// iteration.  This parameter should not be null.
        /// </param>
        /// <param name="result">
        /// Upon success, this contains the result of the last evaluation of
        /// the body script (or an empty string when no iterations occur).
        /// Upon failure, this contains an appropriate error message.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> when the loop completes successfully;
        /// otherwise, a non-Ok value (e.g. <see cref="ReturnCode.Error" />)
        /// with details placed in <paramref name="result" />.
        /// </returns>
        public override ReturnCode Execute(
            Interpreter interpreter, /* in */
            IClientData clientData,  /* in */
            ArgumentList arguments,  /* in */
            ref Result result        /* out */
            )
        {
            return ScriptOps.EachLoopCommand(
                this, false, interpreter, clientData, arguments, ref result);
        }
        #endregion
    }
}
