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
    [ObjectId("16a4192b-599c-4b6c-a09e-b932a710e2bb")]
    [CommandFlags(CommandFlags.Safe | CommandFlags.Standard)]
    [ObjectGroup("list")]
    internal sealed class Lset : Core
    {
        public Lset(
            ICommandData commandData
            )
            : base(commandData)
        {
            // do nothing.
        }

        #region IExecute Members
        public override ReturnCode Execute(
            Interpreter interpreter,
            IClientData clientData,
            ArgumentList arguments,
            ref Result result
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
