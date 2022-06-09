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
    [ObjectId("05e18629-7a6d-4d0f-9406-4aae8e99e666")]
    [CommandFlags(CommandFlags.Safe | CommandFlags.NonStandard)]
    [ObjectGroup("list")]
    internal sealed class Lremove : Core
    {
        public Lremove(
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
