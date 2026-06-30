/*
 * Getf.cs --
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
    /// This class implements the Eagle <c>getf</c> command, which is an
    /// obsolete diagnostic command that looks up a variable via the
    /// interpreter resolvers and reports the outcome together with the
    /// variable flags that were resolved.  See <c>core_language.md</c> for
    /// the command syntax and semantics.
    /// </summary>
    [ObjectId("ac0a9ff6-87a3-49ed-8402-b2ab7e40aa32")]
    [Obsolete()]
    [CommandFlags(CommandFlags.Unsafe | CommandFlags.NonStandard |
                  CommandFlags.Obsolete | CommandFlags.Diagnostic)]
    [ObjectGroup("variable")]
    internal sealed class Getf : Core
    {
        #region Public Constructors
        /// <summary>
        /// Constructs an instance of the <c>getf</c> command.
        /// </summary>
        /// <param name="commandData">
        /// The data used to create and identify this command, such as its
        /// name and flags.  This parameter may be null.
        /// </param>
        public Getf(
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
        /// This method executes the <c>getf</c> command.  It requires a
        /// single variable name argument, resolves that variable through the
        /// interpreter resolvers, and returns a list describing the lookup
        /// outcome and the resolved variable flags.
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
        /// command name and element one is the name of the variable to look
        /// up.  This parameter should not be null.
        /// </param>
        /// <param name="result">
        /// Upon success, this contains a list comprising the resolver return
        /// code, any resolver error, the variable flags used for the lookup,
        /// and the flags of the resolved variable (or
        /// <see cref="VariableFlags.None" /> when no variable was resolved).
        /// Upon failure, this contains an appropriate error message.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> when the lookup was attempted and its
        /// outcome was placed in <paramref name="result" />; otherwise,
        /// <see cref="ReturnCode.Error" /> when the wrong number of arguments
        /// is supplied, the interpreter is null, or the argument list is
        /// null, with details placed in <paramref name="result" />.
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

            if (arguments.Count != 2)
            {
                result = String.Format(
                    "wrong # args: should be \"{0} varName\"",
                    this.Name);

                return ReturnCode.Error;
            }

            ReturnCode code;
            VariableFlags flags = VariableFlags.NoElement;
            IVariable variable = null;
            Result error = null;

            code = interpreter.GetVariableViaResolversWithSplit(
                arguments[1], ref flags, ref variable, ref error);

            result = StringList.MakeList(
                code, error, flags, (variable != null) ?
                variable.Flags : VariableFlags.None);

            return ReturnCode.Ok;
        }
        #endregion
    }
}
