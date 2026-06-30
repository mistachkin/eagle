/*
 * Continue.cs --
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
using Eagle._Components.Public;
using Eagle._Containers.Public;
using Eagle._Interfaces.Public;

namespace Eagle._Commands
{
    /// <summary>
    /// This class implements the Eagle <c>continue</c> command, which signals
    /// that the innermost enclosing loop (for example <c>for</c>,
    /// <c>foreach</c>, or <c>while</c>) should skip the remainder of the
    /// current iteration and proceed with its next iteration.  See
    /// <c>core_language.md</c> for the command syntax and semantics.
    /// </summary>
    [ObjectId("fedc53dc-e8c3-4892-8d1d-ef788baf194c")]
    [CommandFlags(CommandFlags.Safe | CommandFlags.Standard)]
    [ObjectGroup("control")]
    internal sealed class Continue : Core
    {
        /// <summary>
        /// Constructs an instance of the <c>continue</c> command.
        /// </summary>
        /// <param name="commandData">
        /// The data used to create and identify this command, such as its
        /// name and flags.  This parameter may be null.
        /// </param>
        public Continue(
            ICommandData commandData
            )
            : base(commandData)
        {
            // do nothing.
        }

        #region IExecute Members
        /// <summary>
        /// This method executes the <c>continue</c> command.  It accepts an
        /// optional result string and returns
        /// <see cref="ReturnCode.Continue" /> so that the enclosing loop skips
        /// to its next iteration.
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
        /// command name; an optional element one supplies the result string
        /// carried out of the current iteration.  This parameter should not be
        /// null.
        /// </param>
        /// <param name="result">
        /// Upon success, this contains the optional result string (or an
        /// empty string when none was supplied).  Upon failure, this
        /// contains an appropriate error message.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Continue" /> when invoked correctly,
        /// signaling the enclosing loop to advance to its next iteration;
        /// otherwise, <see cref="ReturnCode.Error" /> when the wrong number of
        /// arguments is supplied, the interpreter is null, or the argument
        /// list is null, with details placed in <paramref name="result" />.
        /// </returns>
        public override ReturnCode Execute(
            Interpreter interpreter, /* in */
            IClientData clientData,  /* in */
            ArgumentList arguments,  /* in */
            ref Result result        /* out */
            )
        {
            ReturnCode code = ReturnCode.Continue;

            if (interpreter != null)
            {
                if (arguments != null)
                {
                    if ((arguments.Count == 1) || (arguments.Count == 2))
                    {
                        result = (arguments.Count == 2) ?
                            (Result)arguments[1] : (Result)String.Empty;
                    }
                    else
                    {
                        result = "wrong # args: should be \"continue ?string?\"";
                        code = ReturnCode.Error;
                    }
                }
                else
                {
                    result = "invalid argument list";
                    code = ReturnCode.Error;
                }
            }
            else
            {
                result = "invalid interpreter";
                code = ReturnCode.Error;
            }

            return code;
        }
        #endregion
    }
}
