/*
 * Lget.cs --
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

#if NET_STANDARD_21
using Index = Eagle._Constants.Index;
#endif

namespace Eagle._Commands
{
    /// <summary>
    /// This class implements the Eagle <c>lget</c> command, which retrieves an
    /// element (or nested element) from the list value stored in a named
    /// variable, using zero or more list indexes to descend through nested
    /// lists.  See <c>core_language.md</c> for the command syntax and
    /// semantics.
    /// </summary>
    [ObjectId("55e4e145-6aa0-47d8-8a1c-c1c50d9d459e")]
    [CommandFlags(CommandFlags.Safe | CommandFlags.NonStandard)]
    [ObjectGroup("list")]
    internal sealed class Lget : Core
    {
        /// <summary>
        /// Constructs an instance of the <c>lget</c> command.
        /// </summary>
        /// <param name="commandData">
        /// The data used to create and identify this command, such as its
        /// name and flags.  This parameter may be null.
        /// </param>
        public Lget(
            ICommandData commandData
            )
            : base(commandData)
        {
            // do nothing.
        }

        ///////////////////////////////////////////////////////////////////////

        #region IExecute Members
        /// <summary>
        /// This method executes the <c>lget</c> command.  It reads the list
        /// value of the named variable and, for each supplied index, descends
        /// into the corresponding nested list element, returning the value
        /// reached by the final index (or the whole list when no indexes are
        /// supplied).
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
        /// command name; element one is the name of the variable holding the
        /// list; any remaining elements are list indexes used to descend
        /// through nested lists.  This parameter should not be null.
        /// </param>
        /// <param name="result">
        /// Upon success, this contains the value reached by the supplied
        /// indexes (or the entire list when none are supplied).  Upon failure,
        /// this contains an appropriate error message.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success, with the selected value
        /// placed in <paramref name="result" />; otherwise,
        /// <see cref="ReturnCode.Error" /> when the wrong number of arguments
        /// is supplied, the variable cannot be read as a list, an index is
        /// invalid or out of range, the interpreter is null, or the argument
        /// list is null, with details placed in <paramref name="result" />.
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
                        lock (interpreter.InternalSyncRoot) /* TRANSACTIONAL */
                        {
                            StringList list = null;

                            code = interpreter.GetListVariableValue(
                                VariableFlags.None, arguments[1], false, true, false, true,
                                ref list, ref result);

                            if (code == ReturnCode.Ok)
                            {
                                Result value = list;
                                int argumentIndex = 2; // start at first index.
                                int listIndex = argumentIndex - 2;

                                if (arguments.Count > 2)
                                {
                                    StringList[] lists = new StringList[arguments.Count - 2]; // only count index args.
                                    int[] index = new int[arguments.Count - 2];

                                    do
                                    {
                                        lists[listIndex] = null;

                                        //
                                        // WARNING: Cannot cache list representation here, the list
                                        //          is modified below.
                                        //
                                        code = ListOps.GetOrCopyOrSplitList(
                                            interpreter, value, false, ref lists[listIndex],
                                            ref result);

                                        if (code != ReturnCode.Ok)
                                            break;

                                        string indexText = arguments[argumentIndex];

                                        if (!String.IsNullOrEmpty(indexText))
                                        {
                                            index[listIndex] = Index.Invalid;

                                            code = Value.GetIndex(
                                                indexText, lists[listIndex].Count,
                                                ValueFlags.AnyIndex,
                                                interpreter.InternalCultureInfo,
                                                ref index[listIndex], ref result);

                                            if (code != ReturnCode.Ok)
                                                break;

                                            if ((index[listIndex] < 0) ||
                                                (index[listIndex] >= lists[listIndex].Count))
                                            {
                                                result = "list index out of range";
                                                code = ReturnCode.Error;
                                                break;
                                            }

                                            value = lists[listIndex][index[listIndex]];
                                        }
                                        else
                                        {
                                            value = lists[listIndex];
                                        }

                                        argumentIndex++;

                                        if (argumentIndex >= arguments.Count)
                                            break;

                                        //
                                        // NOTE: Advance to handling the next nested list.
                                        //
                                        listIndex++;
                                    }
                                    while (true);
                                }

                                if (code == ReturnCode.Ok)
                                    result = value;
                            }
                        }
                    }
                    else
                    {
                        result = "wrong # args: should be \"lget varName ?index ...?\"";
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
