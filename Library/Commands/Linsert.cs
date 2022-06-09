/*
 * Linsert.cs --
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
    [ObjectId("2d555605-3100-483c-950b-c6e4e87446be")]
    [CommandFlags(CommandFlags.Safe | CommandFlags.Standard)]
    [ObjectGroup("list")]
    internal sealed class Linsert : Core
    {
        public Linsert(
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
                        StringList list = null;

                        //
                        // WARNING: Cannot cache list representation here, the list
                        //          is modified below.
                        //
                        code = ListOps.GetOrCopyOrSplitList(
                            interpreter, arguments[1], false, ref list, ref result);

                        if (code == ReturnCode.Ok)
                        {
                            int index = Index.Invalid;

                            code = Value.GetIndex(
                                arguments[2], list.Count, ValueFlags.AnyIndex,
                                interpreter.InternalCultureInfo, ref index, ref result);

                            if (code == ReturnCode.Ok)
                            {
                                //
                                // NOTE: Auto-normalize index to be within range.
                                //
                                if (index < 0)
                                    index = 0;
                                else if (index > list.Count)
                                    index = list.Count;

                                StringList subList = new StringList(arguments, 3);

                                list.InsertRange(index, subList);

                                result = list;
                            }
                        }
                    }
                    else
                    {
                        result = "wrong # args: should be \"linsert list index value ?value ...?\"";
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
