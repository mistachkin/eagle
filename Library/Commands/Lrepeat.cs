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
    [ObjectId("f42e4182-eb1b-4951-924b-f390391ccde2")]
    [CommandFlags(CommandFlags.Safe | CommandFlags.Standard)]
    [ObjectGroup("list")]
    internal sealed class Lrepeat : Core
    {
        public Lrepeat(
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
