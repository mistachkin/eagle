/*
 * Setf.cs --
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
    /// This class implements the Eagle <c>setf</c> command, which gets or sets
    /// a variable value using an explicit set of <see cref="VariableFlags" />.
    /// This obsolete, diagnostic command exposes the low-level variable access
    /// path so the flags can be specified directly.  See
    /// <c>core_language.md</c> for the command syntax and semantics.
    /// </summary>
    [ObjectId("7c8c73c9-41f9-496f-b1a5-1b4a9aa421c4")]
    [Obsolete()]
    [CommandFlags(CommandFlags.Unsafe | CommandFlags.NonStandard |
                  CommandFlags.Obsolete | CommandFlags.Diagnostic)]
    [ObjectGroup("variable")]
    internal sealed class Setf : Core
    {
        #region Public Constructors
        /// <summary>
        /// Constructs an instance of the <c>setf</c> command.
        /// </summary>
        /// <param name="commandData">
        /// The data used to create and identify this command, such as its
        /// name and flags.  This parameter may be null.
        /// </param>
        public Setf(
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
        /// This method executes the <c>setf</c> command.  It parses the
        /// requested <see cref="VariableFlags" /> from the first argument and
        /// then, depending on the argument count, either gets the value of the
        /// named variable or sets it to the supplied value and re-gets the
        /// resulting value.
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
        /// command name; element one is the <see cref="VariableFlags" /> to
        /// use; element two is the variable name; an optional element three
        /// supplies the new value to assign.  This parameter should not be
        /// null.
        /// </param>
        /// <param name="result">
        /// Upon success, this contains the (possibly newly assigned) value of
        /// the named variable.  Upon failure, this contains an appropriate
        /// error message.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success, with the variable value
        /// placed in <paramref name="result" />; otherwise,
        /// <see cref="ReturnCode.Error" /> when the wrong number of arguments
        /// is supplied, the interpreter is null, the argument list is null, the
        /// variable flags cannot be parsed, or the variable access fails, with
        /// details placed in <paramref name="result" />.
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

            if ((arguments.Count != 3) && (arguments.Count != 4))
            {
                result = String.Format(
                    "wrong # args: should be \"{0} varFlags varName ?newValue?\"",
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

            string varName = arguments[2];

            if (arguments.Count == 3)
            {
                return interpreter.GetVariableValue(
                    flags, varName, ref result, ref result);
            }
            else if (arguments.Count == 4)
            {
                string varValue = arguments[3];

                if (interpreter.SetVariableValue(
                        flags, varName, varValue, null,
                        ref result) == ReturnCode.Ok)
                {
                    //
                    // NOTE: Maybe append mode?  Re-get value now.
                    //
                    return interpreter.GetVariableValue(
                        flags, varName, ref result, ref result);
                }
            }

            return ReturnCode.Error;
        }
        #endregion
    }
}
