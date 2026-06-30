/*
 * Lset.cs --
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
    /// This class implements the Eagle <c>lset</c> command, which sets an
    /// element of a list value stored in a variable, optionally descending
    /// into nested sublists via one or more indexes, and stores the modified
    /// list back into the variable.  See <c>core_language.md</c> for the
    /// command syntax and semantics.
    /// </summary>
    [ObjectId("16a4192b-599c-4b6c-a09e-b932a710e2bb")]
    [CommandFlags(CommandFlags.Safe | CommandFlags.Standard)]
    [ObjectGroup("list")]
    internal sealed class Lset : Core
    {
        /// <summary>
        /// Constructs an instance of the <c>lset</c> command.
        /// </summary>
        /// <param name="commandData">
        /// The data used to create and identify this command, such as its
        /// name and flags.  This parameter may be null.
        /// </param>
        public Lset(
            ICommandData commandData
            )
            : base(commandData)
        {
            // do nothing.
        }

        #region IExecute Members
        /// <summary>
        /// This method executes the <c>lset</c> command.  It reads the list
        /// value from the named variable, navigates the supplied index path
        /// (creating per-level working copies of each nested sublist),
        /// replaces the addressed element with the final argument value,
        /// re-integrates the changes back up to the outermost list, stores the
        /// result into the variable, and returns the updated list.
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
        /// command name; element one is the variable name; the elements
        /// between are one or more indexes selecting the (possibly nested)
        /// element to set; and the final element is the new value.  This
        /// parameter should not be null.
        /// </param>
        /// <param name="result">
        /// Upon success, this contains the updated list value that was stored
        /// back into the variable.  Upon failure, this contains an appropriate
        /// error message.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success, with the updated list
        /// placed in <paramref name="result" />; otherwise,
        /// <see cref="ReturnCode.Error" /> when the wrong number of arguments
        /// is supplied, the interpreter is null, the argument list is null, an
        /// index is invalid or out of range, or the variable cannot be read or
        /// written, with details placed in <paramref name="result" />.
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
                        lock (interpreter.InternalSyncRoot) /* TRANSACTIONAL */
                        {
                            StringList list = null;

                            code = interpreter.GetListVariableValue(
                                VariableFlags.None, arguments[1], true, true, false, false,
                                ref list, ref result);

                            if (code == ReturnCode.Ok)
                            {
                                Result value = list;
                                int argumentIndex = 2; // start at first index.
                                int listIndex = argumentIndex - 2;
                                StringList[] lists = new StringList[arguments.Count - 3]; // only count index args.
                                int[] index = new int[arguments.Count - 3];

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

                                        argumentIndex++;

                                        if ((argumentIndex + 1) >= arguments.Count) // stop just before the value.
                                        {
                                            //
                                            // NOTE: Re-integrate the changes back up to the original list.
                                            //
                                            lists[listIndex][index[listIndex]] = arguments[argumentIndex];

                                            for (; listIndex > 0; listIndex--)
                                                lists[listIndex - 1][index[listIndex - 1]] = lists[listIndex].ToString();

                                            break;
                                        }

                                        //
                                        // NOTE: Advance to handling the next nested list.
                                        //
                                        value = lists[listIndex][index[listIndex]];
                                    }
                                    else
                                    {
                                        argumentIndex++;

                                        if ((argumentIndex + 1) >= arguments.Count) // stop just before the value.
                                        {
                                            //
                                            // BUGFIX: This is the empty-index "wholesale" form
                                            //         (e.g. "lset var {} value", or the nested
                                            //         "lset var index ... {} value"), which must
                                            //         REPLACE the (sub)list with the new value.
                                            //         At this point lists[listIndex] still holds
                                            //         the CURRENT (sub)list contents (captured
                                            //         earlier in this loop).  GetOrCopyOrSplitList
                                            //         only assigns a brand new list when the value
                                            //         is already an internal list object; for a
                                            //         plain string value it SPLITS and APPENDS into
                                            //         the existing list, which would wrongly yield
                                            //         the old contents followed by the new value --
                                            //         a representation-dependent result (string vs
                                            //         list).  Clear the target first so the value
                                            //         always REPLACES, regardless of whether it is
                                            //         internally a string or a list (COMPAT: Tcl).
                                            //
                                            lists[listIndex] = null;

                                            //
                                            // WARNING: Cannot cache list representation here, the list
                                            //          may be modified via the list variable in the
                                            //          future.
                                            //
                                            code = ListOps.GetOrCopyOrSplitList(
                                                interpreter, arguments[argumentIndex], false,
                                                ref lists[listIndex], ref result);

                                            if (code == ReturnCode.Ok)
                                            {
                                                for (; listIndex > 0; listIndex--)
                                                    lists[listIndex - 1][index[listIndex - 1]] = lists[listIndex].ToString();
                                            }

                                            break;
                                        }

                                        //
                                        // NOTE: Advance to handling the next nested list.
                                        //
                                        value = lists[listIndex];
                                    }

                                    listIndex++;
                                }
                                while (true);

                                if (code == ReturnCode.Ok)
                                {
                                    code = interpreter.SetListVariableValue(
                                        VariableFlags.None, arguments[1],
                                        lists[0], null, ref result);

                                    if (code == ReturnCode.Ok)
                                        result = lists[0];
                                }
                            }
                        }
                    }
                    else
                    {
                        result = "wrong # args: should be \"lset varName index ?index...? value\"";
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
