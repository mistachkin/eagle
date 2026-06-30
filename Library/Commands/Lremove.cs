/*
 * Lremove.cs --
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
using Eagle._Constants;
using Eagle._Containers.Public;
using Eagle._Interfaces.Public;

#if NET_STANDARD_21
using Index = Eagle._Constants.Index;
#endif

namespace Eagle._Commands
{
    /// <summary>
    /// This class implements the Eagle <c>lremove</c> command, which returns a
    /// new list formed by removing the element at the specified index (or, when
    /// multiple indexes are given, the element reached by descending through
    /// successive nested sub-lists) from the supplied list.  See
    /// <c>core_language.md</c> for the command syntax and semantics.
    /// </summary>
    [ObjectId("05e18629-7a6d-4d0f-9406-4aae8e99e666")]
    [CommandFlags(CommandFlags.Safe | CommandFlags.NonStandard)]
    [ObjectGroup("list")]
    internal sealed class Lremove : Core
    {
        /// <summary>
        /// Constructs an instance of the <c>lremove</c> command.
        /// </summary>
        /// <param name="commandData">
        /// The data used to create and identify this command, such as its
        /// name and flags.  This parameter may be null.
        /// </param>
        public Lremove(
            ICommandData commandData
            )
            : base(commandData)
        {
            // do nothing.
        }

        #region IExecute Members
        /// <summary>
        /// This method executes the <c>lremove</c> command.  It removes the
        /// element identified by the supplied index (or chain of indexes for
        /// nested sub-lists) from the given list and returns the resulting
        /// list.
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
        /// command name; element one is the list to operate on; the remaining
        /// elements are one or more indexes that select the element to remove,
        /// descending into nested sub-lists when more than one index is given.
        /// This parameter should not be null.
        /// </param>
        /// <param name="result">
        /// Upon success, this contains the new list with the selected element
        /// removed.  Upon failure, this contains an appropriate error message.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success, with the resulting list
        /// placed in <paramref name="result" />; otherwise,
        /// <see cref="ReturnCode.Error" /> when the wrong number of arguments
        /// is supplied, the list cannot be parsed, an index is invalid or out
        /// of range, the interpreter is null, or the argument list is null,
        /// with details placed in <paramref name="result" />.
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
                        Argument argument = arguments[1];
                        int argumentIndex = 2; // start at first index arg.
                        int listIndex = argumentIndex - 2;
                        StringList[] list = new StringList[arguments.Count - 2]; // only count index args.
                        int[] index = new int[arguments.Count - 2];

                        do
                        {
                            list[listIndex] = null;

                            //
                            // WARNING: Cannot cache list representation here,
                            //          the list is modified below.
                            //
                            code = ListOps.GetOrCopyOrSplitList(
                                interpreter, argument, false, ref list[listIndex],
                                ref result);

                            if (code != ReturnCode.Ok)
                                break;

                            index[listIndex] = Index.Invalid;

                            code = Value.GetIndex(
                                arguments[argumentIndex], list[listIndex].Count,
                                ValueFlags.AnyIndex, interpreter.InternalCultureInfo,
                                ref index[listIndex], ref result);

                            if (code != ReturnCode.Ok)
                                break;

                            if ((index[listIndex] < 0) ||
                                (index[listIndex] >= list[listIndex].Count))
                            {
                                result = "list index out of range";
                                code = ReturnCode.Error;
                                break;
                            }

                            argumentIndex++;

                            if (argumentIndex >= arguments.Count)
                            {
                                //
                                // NOTE: Re-integrate the changes back up to the
                                //       original list.
                                //
                                list[listIndex].RemoveAt(index[listIndex]);

                                for (; listIndex > 0; listIndex--)
                                    list[listIndex - 1][index[listIndex - 1]] =
                                        list[listIndex].ToString();

                                break;
                            }

                            //
                            // NOTE: Advance to handling the next nested list.
                            //
                            argument = list[listIndex][index[listIndex]];
                            listIndex++;
                        }
                        while (true);

                        if (code == ReturnCode.Ok)
                            result = list[0];
                    }
                    else
                    {
                        result = "wrong # args: should be \"lremove list index ?index...?\"";
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
