/*
 * Stub.cs --
 *
 * Copyright (c) 2007-2012 by Joe Mistachkin.  All rights reserved.
 *
 * See the file "license.terms" for information on usage and redistribution of
 * this file, and for a DISCLAIMER OF ALL WARRANTIES.
 *
 * RCS: @(#) $Id: $
 */

using System;
using Eagle._Attributes;
using Eagle._Components.Private;
using Eagle._Components.Public;
using Eagle._Containers.Public;
using Eagle._Interfaces.Public;

namespace Eagle._Commands
{
    /// <summary>
    /// This class implements the Eagle <c>stub</c> command, which acts as a
    /// delegate ensemble command:  it dispatches an invocation to one of its
    /// sub-commands based on the first argument.  See <c>core_language.md</c>
    /// for the command syntax and semantics.
    /// </summary>
    [ObjectId("ebb5400c-203d-4e16-b2e3-0715421a6b0d")]
    [CommandFlags(
        CommandFlags.Safe | CommandFlags.NonStandard |
        CommandFlags.NoPopulate | CommandFlags.NoAdd |
        CommandFlags.Delegate
    )]
    [ObjectGroup("ensemble")]
    internal sealed class Stub : Core
    {
        /// <summary>
        /// Constructs an instance of the <c>stub</c> command.
        /// </summary>
        /// <param name="commandData">
        /// The data used to create and identify this command, such as its
        /// name and flags.  This parameter may be null.
        /// </param>
        public Stub(
            ICommandData commandData
            )
            : base(commandData)
        {
            // do nothing.
        }

        ///////////////////////////////////////////////////////////////////////

        #region IExecute Members
        /// <summary>
        /// This method executes the <c>stub</c> command.  It selects a
        /// sub-command from this ensemble based on the option argument and
        /// dispatches the invocation to it via
        /// <c>ScriptOps.TryExecuteSubCommandFromEnsemble</c>.
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
        /// command name; the sub-command name follows, along with any
        /// arguments to forward to it.  This parameter should not be null.
        /// </param>
        /// <param name="result">
        /// Upon success, this contains the result produced by the dispatched
        /// sub-command.  Upon failure, this contains an appropriate error
        /// message.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise,
        /// <see cref="ReturnCode.Error" /> when the interpreter is null, the
        /// argument list is null, too few arguments are supplied, or the
        /// requested sub-command is unknown, with details placed in
        /// <paramref name="result" />.
        /// </returns>
        public override ReturnCode Execute(
            Interpreter interpreter, /* in */
            IClientData clientData,  /* in */
            ArgumentList arguments,  /* in */
            ref Result result        /* out */
            )
        {
            if (interpreter == null)
            {
                result = "invalid interpreter";
                return ReturnCode.Error;
            }

            if (arguments == null)
            {
                result = "invalid argument list";
                return ReturnCode.Error;
            }

            int nameIndex = ScriptOps.GetSubCommandNameIndex();
            int nextIndex = nameIndex + 1;

            if (arguments.Count < nextIndex)
            {
                result = String.Format(
                    "wrong # args: should be \"{0} option ?arg ...?\"",
                    this.Name);

                return ReturnCode.Error;
            }

            ReturnCode code;
            string subCommand = arguments[nameIndex];
            bool tried = false;

            code = ScriptOps.TryExecuteSubCommandFromEnsemble(
                interpreter, this, clientData, arguments, false,
                null, ref subCommand, ref tried, ref result);

            if ((code == ReturnCode.Ok) && !tried)
            {
                result = ScriptOps.BadSubCommand(
                    interpreter, null, null, subCommand, this,
                    null, null);

                code = ReturnCode.Error;
            }

            return code;
        }
        #endregion
    }
}
