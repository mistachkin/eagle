/*
 * Lassign.cs --
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
using Eagle._Constants;
using Eagle._Containers.Public;
using Eagle._Interfaces.Public;

namespace Eagle._Commands
{
    /// <summary>
    /// This class implements the Eagle <c>lassign</c> command, which assigns
    /// the successive elements of a list to one or more variables and returns
    /// any elements that were left over.  See <c>core_language.md</c> for the
    /// command syntax and semantics.
    /// </summary>
    [ObjectId("b19de186-5d3e-4543-82fe-2d3b9355dc4a")]
    [CommandFlags(CommandFlags.Safe | CommandFlags.Standard)]
    [ObjectGroup("list")]
    internal sealed class Lassign : Core
    {
        /// <summary>
        /// Constructs an instance of the <c>lassign</c> command.
        /// </summary>
        /// <param name="commandData">
        /// The data used to create and identify this command, such as its
        /// name and flags.  This parameter may be null.
        /// </param>
        public Lassign(
            ICommandData commandData
            )
            : base(commandData)
        {
            // do nothing.
        }

        #region IExecute Members
        /// <summary>
        /// This method executes the <c>lassign</c> command.  It splits the
        /// supplied list and assigns its elements, in order, to the named
        /// variables; any variables for which no element exists are set to the
        /// empty string, and any elements left over after the last variable
        /// are returned.
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
        /// command name; element one is the list whose elements are to be
        /// assigned; the remaining elements name the variables to receive
        /// those values.  This parameter should not be null.
        /// </param>
        /// <param name="result">
        /// Upon success, this contains the list of elements that were left
        /// over after the last variable was assigned (or an empty string when
        /// none remain).  Upon failure, this contains an appropriate error
        /// message.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise,
        /// <see cref="ReturnCode.Error" /> when the wrong number of arguments
        /// is supplied, the list cannot be parsed, a variable cannot be set,
        /// the interpreter is null, or the argument list is null, with details
        /// placed in <paramref name="result" />.
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
                    if (arguments.Count >= 3)
                    {
                        StringList list = null;

                        code = ListOps.GetOrCopyOrSplitList(
                            interpreter, arguments[1], true, ref list, ref result);

                        if (code == ReturnCode.Ok)
                        {
                            int argumentIndex;

                            for (argumentIndex = 2; argumentIndex < arguments.Count; argumentIndex++)
                            {
                                string value;

                                if ((argumentIndex - 2) < list.Count)
                                    value = list[argumentIndex - 2];
                                else
                                    value = String.Empty;

                                code = interpreter.SetVariableValue(
                                    VariableFlags.None, arguments[argumentIndex],
                                    value, null, ref result);

                                if (code != ReturnCode.Ok)
                                    break;
                            }

                            if (code == ReturnCode.Ok)
                            {
                                if ((argumentIndex - 2) < list.Count)
                                    result = StringList.GetRange(list, argumentIndex - 2);
                                else
                                    result = String.Empty;
                            }
                        }
                    }
                    else
                    {
                        result = "wrong # args: should be \"lassign list varName ?varName ...?\"";
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
