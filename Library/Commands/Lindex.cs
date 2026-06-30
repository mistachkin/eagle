/*
 * Lindex.cs --
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
    /// This class implements the Eagle <c>lindex</c> command, which retrieves
    /// one or more elements from a list by index, optionally descending
    /// through nested sublists when multiple indexes are supplied.  See
    /// <c>core_language.md</c> for the command syntax and semantics.
    /// </summary>
    [ObjectId("e60c0a62-397d-42cf-90d2-62be391062b3")]
    [CommandFlags(CommandFlags.Safe | CommandFlags.Standard)]
    [ObjectGroup("list")]
    internal sealed class Lindex : Core
    {
        /// <summary>
        /// Constructs an instance of the <c>lindex</c> command.
        /// </summary>
        /// <param name="commandData">
        /// The data used to create and identify this command, such as its
        /// name and flags.  This parameter may be null.
        /// </param>
        public Lindex(
            ICommandData commandData
            )
            : base(commandData)
        {
            // do nothing.
        }

        ///////////////////////////////////////////////////////////////////////

        #region IExecute Members
        /// <summary>
        /// This method executes the <c>lindex</c> command.  It treats the
        /// first argument as a list and applies each subsequent index argument
        /// in turn, descending into the element selected by the previous
        /// index.  An index that addresses a value outside the list yields an
        /// empty string, and a single index argument that resolves to a list
        /// of indexes produces a list of the corresponding elements.
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
        /// command name; element one is the list to index into; any remaining
        /// elements are the index expressions applied successively.  This
        /// parameter should not be null.
        /// </param>
        /// <param name="result">
        /// Upon success, this contains the selected element (or an empty
        /// string when an index is out of range, or the original list when no
        /// index is supplied).  Upon failure, this contains an appropriate
        /// error message.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success, with the indexed value
        /// placed in <paramref name="result" />; otherwise,
        /// <see cref="ReturnCode.Error" /> when the wrong number of arguments
        /// is supplied, the list or an index cannot be parsed, the interpreter
        /// is null, or the argument list is null, with details placed in
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
                        int argumentIndex = 1;
                        Argument argument = arguments[argumentIndex];
                        StringList inputList;

                        while (++argumentIndex < arguments.Count)
                        {
                            inputList = null;

                            code = ListOps.GetOrCopyOrSplitList(
                                interpreter, argument, true, ref inputList,
                                ref result);

                            if (code != ReturnCode.Ok)
                                break;

                            int index = Index.Invalid;

                            code = Value.GetIndex(
                                arguments[argumentIndex], inputList.Count,
                                ValueFlags.AnyIndex, interpreter.InternalCultureInfo,
                                ref index, ref result);

                            if (code == ReturnCode.Ok)
                            {
                                if ((index >= 0) && (index < inputList.Count))
                                    argument = inputList[index];
                                else
                                    argument = String.Empty;
                            }
                            else
                            {
                                IntList indexList = null;

                                code = Value.GetIndexList(
                                    interpreter, arguments[argumentIndex],
                                    inputList.Count, ValueFlags.AnyIndex,
                                    interpreter.InternalCultureInfo, ref indexList);

                                if (code == ReturnCode.Ok)
                                {
                                    StringList outputList = new StringList();

                                    foreach (int index2 in indexList)
                                    {
                                        if ((index2 >= 0) && (index2 < inputList.Count))
                                            outputList.Add(inputList[index2]);
                                        else
                                            outputList.Add(String.Empty);
                                    }

                                    argument = outputList;
                                }
                            }

                            if (code != ReturnCode.Ok)
                                break;
                        }

                        if (code == ReturnCode.Ok)
                            result = argument;
                    }
                    else
                    {
                        result = "wrong # args: should be \"lindex list ?index ...?\"";
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
