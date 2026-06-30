/*
 * Lreplace.cs --
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
    /// This class implements the Eagle <c>lreplace</c> command, which returns
    /// a new list formed by replacing the elements of a list between two
    /// indexes with zero or more replacement elements.  See
    /// <c>core_language.md</c> for the command syntax and semantics.
    /// </summary>
    [ObjectId("5525590c-6517-4ea5-bb16-520ec34a0c4d")]
    [CommandFlags(CommandFlags.Safe | CommandFlags.Standard)]
    [ObjectGroup("list")]
    internal sealed class Lreplace : Core
    {
        /// <summary>
        /// Constructs an instance of the <c>lreplace</c> command.
        /// </summary>
        /// <param name="commandData">
        /// The data used to create and identify this command, such as its
        /// name and flags.  This parameter may be null.
        /// </param>
        public Lreplace(
            ICommandData commandData
            )
            : base(commandData)
        {
            // do nothing.
        }

        #region IExecute Members
        /// <summary>
        /// This method executes the <c>lreplace</c> command.  It parses the
        /// list and the first and last index arguments, removes the elements
        /// in that index range, inserts any supplied replacement elements at
        /// that position, and returns the resulting list.
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
        /// command name; element one is the list to operate on; elements two
        /// and three are the first and last indexes of the range to replace;
        /// any remaining elements are the replacement values.  This parameter
        /// should not be null.
        /// </param>
        /// <param name="result">
        /// Upon success, this contains the new list with the specified range
        /// replaced.  Upon failure, this contains an appropriate error
        /// message.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success, with the resulting list
        /// placed in <paramref name="result" />; otherwise,
        /// <see cref="ReturnCode.Error" /> when the wrong number of arguments
        /// is supplied, the list or an index cannot be parsed, the first index
        /// does not refer to an existing element, the interpreter is null, or
        /// the argument list is null, with details placed in
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
                    if (arguments.Count >= 4)
                    {
                        StringList list = null;

                        //
                        // WARNING: Cannot cache list representation here, the list
                        //          is modified below.
                        //
                        code = ListOps.GetOrCopyOrSplitList(
                            interpreter, arguments[1], false, ref list, ref result);

                        if (code == ReturnCode.Ok)
                        {
                            int listCount = list.Count;
                            int firstIndex = Index.Invalid;

                            code = Value.GetIndex(
                                arguments[2], listCount, ValueFlags.AnyIndex,
                                interpreter.InternalCultureInfo, ref firstIndex,
                                ref result);

                            if (code == ReturnCode.Ok)
                            {
                                int lastIndex = Index.Invalid;

                                code = Value.GetIndex(
                                    arguments[3], listCount, ValueFlags.AnyIndex,
                                    interpreter.InternalCultureInfo, ref lastIndex,
                                    ref result);

                                if (code == ReturnCode.Ok)
                                {
                                    if (firstIndex < 0)
                                        firstIndex = 0;

                                    if ((firstIndex < listCount) || (listCount == 0))
                                    {
                                        if ((listCount > 0) || ScriptOps.HasFlags(
                                                interpreter, InterpreterFlags.ReplaceEmptyListOk,
                                                true))
                                        {
                                            if (firstIndex < listCount)
                                            {
                                                if (lastIndex >= listCount)
                                                    lastIndex = listCount - 1;

                                                int numToDelete;

                                                if (firstIndex <= lastIndex)
                                                    numToDelete = (lastIndex - firstIndex + 1);
                                                else
                                                    numToDelete = 0;

                                                if (numToDelete > 0)
                                                    list.RemoveRange(firstIndex, numToDelete);
                                            }

                                            if ((firstIndex <= listCount) &&
                                                (arguments.Count >= 5))
                                            {
                                                list.InsertRange(firstIndex, arguments, 4);
                                            }

                                            result = list;
                                        }
                                        else
                                        {
                                            result = String.Format(
                                                "list doesn't contain element {0}", arguments[2]);

                                            code = ReturnCode.Error;
                                        }
                                    }
                                    else
                                    {
                                        result = String.Format(
                                            "list doesn't contain element {0}", arguments[2]);

                                        code = ReturnCode.Error;
                                    }
                                }
                            }
                        }
                    }
                    else
                    {
                        result = "wrong # args: should be \"lreplace list first last ?value ...?\"";
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
