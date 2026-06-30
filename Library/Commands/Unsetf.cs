/*
 * Unsetf.cs --
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
    /// This class implements the Eagle <c>unsetf</c> command, which is an
    /// obsolete, unsafe diagnostic variant of <c>unset</c> that deletes one
    /// or more variables using an explicit set of
    /// <see cref="VariableFlags" /> parsed from its first argument.  See
    /// <c>core_language.md</c> for the command syntax and semantics.
    /// </summary>
    [ObjectId("945b2916-422a-4cb2-a18c-4693d868887f")]
    [Obsolete()]
    [CommandFlags(CommandFlags.Unsafe | CommandFlags.NonStandard |
                  CommandFlags.Obsolete | CommandFlags.Diagnostic)]
    [ObjectGroup("variable")]
    internal sealed class Unsetf : Core
    {
        #region Public Constructors
        /// <summary>
        /// Constructs an instance of the <c>unsetf</c> command.
        /// </summary>
        /// <param name="commandData">
        /// The data used to create and identify this command, such as its
        /// name and flags.  This parameter may be null.
        /// </param>
        public Unsetf(
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
        /// This method executes the <c>unsetf</c> command.  It parses the
        /// variable flags from the first argument and then deletes each of
        /// the named variables that follow using those flags; supplying no
        /// variable names is permitted and does nothing.
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
        /// command name; element one supplies the <see cref="VariableFlags" />
        /// to use; any remaining elements name the variables to delete.  This
        /// parameter should not be null.
        /// </param>
        /// <param name="result">
        /// Upon success, this contains an empty string.  Upon failure, this
        /// contains an appropriate error message.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> when the variable flags are valid and
        /// every named variable is deleted successfully; otherwise,
        /// <see cref="ReturnCode.Error" /> when the interpreter is null, the
        /// argument list is null, too few arguments are supplied, the
        /// variable flags cannot be parsed, or a variable cannot be deleted,
        /// with details placed in <paramref name="result" />.
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

            if (arguments.Count < 2)
            {
                result = String.Format(
                    "wrong # args: should be \"{0} varFlags ?varName varName ...?\"",
                    this.Name);

                return ReturnCode.Error;
            }

            VariableFlags flags = VariableFlags.None;

            object enumValue = EnumOps.TryParseFlags(
                interpreter, typeof(VariableFlags), flags.ToString(),
                arguments[1], interpreter.InternalCultureInfo, true,
                true, true, ref result);

            if (!(enumValue is VariableFlags))
                return ReturnCode.Error;

            flags = (VariableFlags)enumValue;

            if (arguments.Count > 2)
            {
                for (int argumentIndex = 2;
                        argumentIndex < arguments.Count;
                        argumentIndex++)
                {
                    if (interpreter.UnsetVariable(
                            flags, arguments[argumentIndex],
                            ref result) != ReturnCode.Ok)
                    {
                        return ReturnCode.Error;
                    }
                }
            }

            //
            // NOTE: Do nothing if no more arguments supplied, so as
            //       to match command documentation (COMPAT: Tcl).
            //
            result = String.Empty;
            return ReturnCode.Ok;
        }
        #endregion
    }
}
