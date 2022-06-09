/*
 * Lrange.cs --
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
    [ObjectId("dcf266a2-0a90-436d-bd0f-2995160bc6dd")]
    [CommandFlags(CommandFlags.Safe | CommandFlags.Standard)]
    [ObjectGroup("list")]
    internal sealed class Lrange : Core
    {
        public Lrange(
            ICommandData commandData
            )
            : base(commandData)
        {
            // do nothing.
        }

        ///////////////////////////////////////////////////////////////////////

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
                    if (arguments.Count == 4)
                    {
                        StringList list = null;

                        code = ListOps.GetOrCopyOrSplitList(
                            interpreter, arguments[1], true, ref list,
                            ref result);

                        if (code == ReturnCode.Ok)
                        {
                            int firstIndex = Index.Invalid;

                            code = Value.GetIndex(
                                arguments[2], list.Count, ValueFlags.AnyIndex,
                                interpreter.InternalCultureInfo, ref firstIndex,
                                ref result);

                            if (code == ReturnCode.Ok)
                            {
                                int lastIndex = Index.Invalid;

                                code = Value.GetIndex(
                                    arguments[3], list.Count, ValueFlags.AnyIndex,
                                    interpreter.InternalCultureInfo, ref lastIndex,
                                    ref result);

                                if (code == ReturnCode.Ok)
                                {
                                    if (firstIndex < 0)
                                        firstIndex = 0;

                                    if (lastIndex >= list.Count)
                                        lastIndex = list.Count - 1;

                                    if (firstIndex <= lastIndex)
                                    {
                                        result = StringList.GetRange(
                                            list, firstIndex, lastIndex);
                                    }
                                    else
                                    {
                                        result = String.Empty;
                                    }
                                }
                            }
                        }
                    }
                    else
                    {
                        result = "wrong # args: should be \"lrange list first last\"";
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
