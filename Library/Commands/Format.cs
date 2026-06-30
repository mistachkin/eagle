/*
 * Format.cs --
 *
 * Copyright (c) 2007-2012 by Joe Mistachkin.  All rights reserved.
 *
 * See the file "license.terms" for information on usage and redistribution of
 * this file, and for a DISCLAIMER OF ALL WARRANTIES.
 *
 * RCS: @(#) $Id: $
 */

using System.Text;
using Eagle._Attributes;
using Eagle._Components.Private;
using Eagle._Components.Public;
using Eagle._Containers.Public;
using Eagle._Interfaces.Public;

namespace Eagle._Commands
{
    /// <summary>
    /// This class implements the Eagle <c>format</c> command, which formats
    /// one or more arguments into a string according to a format string in
    /// the style of the C <c>sprintf</c> function.  See
    /// <c>core_language.md</c> for the command syntax and semantics.
    /// </summary>
    [ObjectId("617d524a-6567-46d1-bdf4-5814f08e9558")]
    [CommandFlags(CommandFlags.Safe | CommandFlags.Standard)]
    [ObjectGroup("string")]
    internal sealed class Format : Core
    {
        /// <summary>
        /// Constructs an instance of the <c>format</c> command.
        /// </summary>
        /// <param name="commandData">
        /// The data used to create and identify this command, such as its
        /// name and flags.  This parameter may be null.
        /// </param>
        public Format(
            ICommandData commandData
            )
            : base(commandData)
        {
            // do nothing.
        }

        #region IExecute Members
        /// <summary>
        /// This method executes the <c>format</c> command.  It applies the
        /// supplied format string to the remaining arguments and returns the
        /// resulting formatted string.
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
        /// command name; element one is the format string; any remaining
        /// elements are the values to be substituted into the format string.
        /// This parameter should not be null.
        /// </param>
        /// <param name="result">
        /// Upon success, this contains the formatted string.  Upon failure,
        /// this contains an appropriate error message.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success, with the formatted string
        /// placed in <paramref name="result" />; otherwise,
        /// <see cref="ReturnCode.Error" /> when too few arguments are
        /// supplied, the formatting operation fails, the interpreter is null,
        /// or the argument list is null, with details placed in
        /// <paramref name="result" />.
        /// </returns>
        public override ReturnCode Execute(
            Interpreter interpreter, /* in */
            IClientData clientData,  /* in */
            ArgumentList arguments,  /* in */
            ref Result result        /* out */
            )
        {
            ReturnCode code = ReturnCode.Ok;

            if (interpreter != null)
            {
                if (arguments != null)
                {
                    if (arguments.Count >= 2)
                    {
                        StringBuilder builder = null;

                        code = StringOps.AppendWithFormat(
                            interpreter, arguments[1], ArgumentList.GetRange(arguments, 2),
                            interpreter.InternalCultureInfo, ref builder, ref result);

                        if (code == ReturnCode.Ok)
                            result = StringBuilderCache.GetStringAndRelease(ref builder);
                    }
                    else
                    {
                        result = "wrong # args: should be \"format formatString ?arg ...?\"";
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
