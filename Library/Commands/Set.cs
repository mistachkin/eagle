/*
 * Set.cs --
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
    [ObjectId("a183b0df-8f44-4e9a-955a-ebd79edcfd63")]
    [CommandFlags(
        CommandFlags.Safe | CommandFlags.Standard)]
    [ObjectGroup("variable")]
    internal sealed class Set : Core
    {
        public Set(
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
                    int count = arguments.Count;

                    if ((count == 2) || (count == 3))
                    {
                        if (count == 2)
                        {
                            code = interpreter.GetVariableValue(
                                VariableFlags.DirectGetValueMask, arguments[1],
                                ref result, ref result);
                        }
                        else if (count == 3)
                        {
                            code = interpreter.SetVariableValue2(
                                VariableFlags.DirectSetValueMask, arguments[1],
                                arguments[2].Value, null, ref result);

                            if (code == ReturnCode.Ok)
                                result = arguments[2];
                        }
                    }
                    else
                    {
                        result = "wrong # args: should be \"set varName ?newValue?\"";
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
