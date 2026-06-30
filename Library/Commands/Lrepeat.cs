/*
 * Lrepeat.cs --
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
    /// This class implements the Eagle <c>lrepeat</c> command, which builds a
    /// new list by repeating one or more supplied values a specified number of
    /// times.  See <c>core_language.md</c> for the command syntax and
    /// semantics.
    /// </summary>
    [ObjectId("f42e4182-eb1b-4951-924b-f390391ccde2")]
    [CommandFlags(CommandFlags.Safe | CommandFlags.Standard)]
    [ObjectGroup("list")]
    internal sealed class Lrepeat : Core
    {
        /// <summary>
        /// Constructs an instance of the <c>lrepeat</c> command.
        /// </summary>
        /// <param name="commandData">
        /// The data used to create and identify this command, such as its
        /// name and flags.  This parameter may be null.
        /// </param>
        public Lrepeat(
            ICommandData commandData
            )
            : base(commandData)
        {
            // do nothing.
        }

        #region IExecute Members
        /// <summary>
        /// This method executes the <c>lrepeat</c> command.  It reads the
        /// repeat count and one or more values from the argument list, then
        /// produces a list consisting of those values concatenated the
        /// requested number of times.
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
        /// command name; element one is the repeat count; the remaining
        /// elements are the values to be repeated.  This parameter should not
        /// be null.
        /// </param>
        /// <param name="result">
        /// Upon success, this contains the resulting list with the supplied
        /// values repeated the requested number of times.  Upon failure, this
        /// contains an appropriate error message.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success, with the repeated list
        /// placed in <paramref name="result" />; otherwise,
        /// <see cref="ReturnCode.Error" /> when the wrong number of arguments
        /// is supplied, the count is not a valid integer or is less than one,
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
                        int count = 0;

                        code = Value.GetInteger2(
                            (IGetValue)arguments[1], ValueFlags.AnyInteger,
                            interpreter.InternalCultureInfo, ref count, ref result);

                        if (code == ReturnCode.Ok)
                        {
                            if (count >= 1)
                            {
                                StringList subList = new StringList(arguments, 2);

#if RESULT_LIMITS
                                /* NO RESULT */
                                Engine.CheckResultAgainstLimits(
                                    interpreter, subList.Length, 1, count, 0,
                                    ref code, ref result);

                                if (code == ReturnCode.Ok)
#endif
                                {
                                    StringList list = new StringList();

                                    while (count-- > 0)
                                        list.AddRange(subList);

                                    result = list;
                                }
                            }
                            else
                            {
                                result = "must have a count of at least 1";
                                code = ReturnCode.Error;
                            }
                        }
                    }
                    else
                    {
                        result = "wrong # args: should be \"lrepeat count value ?value ...?\"";
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
