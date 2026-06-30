/*
 * Nop.cs --
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
using Eagle._Interfaces.Public;

namespace Eagle._Commands
{
    /// <summary>
    /// This class implements the Eagle <c>nop</c> command, which accepts any
    /// number of arguments, evaluates none of them, and does nothing.  It is
    /// reserved and always succeeds without producing a result.  See
    /// <c>core_language.md</c> for the command syntax and semantics.
    /// </summary>
    [ObjectId("500d712c-73cf-4a6c-8929-aee59e137047")]
    [CommandFlags(CommandFlags.Safe | CommandFlags.NonStandard)]
    [ObjectGroup("nop")]
    internal sealed class Nop : Core
    {
        /// <summary>
        /// Constructs an instance of the <c>nop</c> command.
        /// </summary>
        /// <param name="commandData">
        /// The data used to create and identify this command, such as its
        /// name and flags.  This parameter may be null.
        /// </param>
        public Nop(
            ICommandData commandData
            )
            : base(commandData)
        {
            // do nothing.
        }

        ///////////////////////////////////////////////////////////////////////

        #region IExecute Members
        /// <summary>
        /// This method executes the <c>nop</c> command.  It intentionally
        /// performs no work, ignores any arguments supplied to it, and leaves
        /// the result untouched.
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
        /// command name; any remaining elements are accepted but ignored.
        /// This parameter is not examined by this command.
        /// </param>
        /// <param name="result">
        /// This parameter is purposely left untouched by this command.
        /// </param>
        /// <returns>
        /// Always <see cref="ReturnCode.Ok" />, since this command performs no
        /// work and cannot fail.
        /// </returns>
        public override ReturnCode Execute(
            Interpreter interpreter, /* in */
            IClientData clientData,  /* in */
            ArgumentList arguments,  /* in */
            ref Result result        /* out */
            )
        {
            //
            // NOTE: This command is RESERVED and must ALWAYS do NOTHING.
            //       Output argument "result" is purposely left untouched.
            //
            return ReturnCode.Ok;
        }
        #endregion
    }
}
