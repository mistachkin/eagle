/*
 * Append.cs --
 *
 * Copyright (c) 2007-2012 by Joe Mistachkin.  All rights reserved.
 *
 * See the file "license.terms" for information on usage and redistribution of
 * this file, and for a DISCLAIMER OF ALL WARRANTIES.
 *
 * RCS: @(#) $Id: $
 */

using System.Text;
using Eagle._Attributes;
using Eagle._Components.Private;
using Eagle._Components.Public;
using Eagle._Containers.Public;
using Eagle._Interfaces.Private;
using Eagle._Interfaces.Public;

namespace Eagle._Commands
{
    [ObjectId("c507edec-2507-4632-963f-2a0ce5d6373d")]
    [CommandFlags(CommandFlags.Safe | CommandFlags.Standard)]
    [ObjectGroup("string")]
    internal sealed class Append : Core
    {
        public Append(
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
                    if (arguments.Count >= 2)
                    {
                        string varName = arguments[1];

                        if (arguments.Count == 2)
                        {
                            //
                            // NOTE: *SPECIAL CASE* For compatibility with Tcl, we must generate 
                            //       an error if only two arguments are supplied and the variable 
                            //       does not exist.
                            //
                            code = interpreter.GetVariableValue(
                                VariableFlags.DirectGetValueMask, varName, ref result,
                                ref result);
                        }
                        else
                        {
                            lock (interpreter.InternalSyncRoot) /* TRANSACTIONAL */
                            {
                                IHaveStringBuilder haveStringBuilder;

                                if (interpreter.GetVariableValue(
                                        VariableFlags.DirectGetValueMask, varName,
                                        ref result) == ReturnCode.Ok)
                                {
                                    haveStringBuilder = StringOps.GetIHaveStringBuilderFromObject(
                                        result, true);
                                }
                                else
                                {
                                    haveStringBuilder = StringOps.NewIHaveStringBuilder();
                                }

                                StringBuilder builder = haveStringBuilder.BuilderForReadWrite;

                                for (int argumentIndex = 2; argumentIndex < arguments.Count; argumentIndex++)
                                    builder.Append(arguments[argumentIndex]);

                                haveStringBuilder.DoneWithReadWrite();

                                code = interpreter.SetVariableValue2(
                                    VariableFlags.DirectSetValueMask, varName,
                                    haveStringBuilder, (TraceList)null, ref result);

                                if (code == ReturnCode.Ok)
                                {
                                    result = haveStringBuilder.BuilderForReadOnly;
                                    result.EngineData = haveStringBuilder;
                                }
                            }
                        }
                    }
                    else
                    {
                        result = "wrong # args: should be \"append varName ?value ...?\"";
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
