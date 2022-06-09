/*
 * Proc.cs --
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

namespace Eagle._Commands
{
    [ObjectId("4fdd1172-4105-4b45-864e-30ca1b70e6c6")]
    [CommandFlags(
        CommandFlags.Safe | CommandFlags.Standard |
        CommandFlags.Initialize)]
    [ObjectGroup("procedure")]
    internal sealed class Proc : Core
    {
        public Proc(
            ICommandData commandData
            )
            : base(commandData)
        {
            // do nothing.
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

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
                        string name = arguments[1];
                        StringList list = null;

                        code = ListOps.GetOrCopyOrSplitList(
                            interpreter, arguments[2], true, ref list, ref result);

                        if (code == ReturnCode.Ok)
                        {
                            StringPairList list2 = new StringPairList();

                            for (int argumentIndex = 0; argumentIndex < list.Count; argumentIndex++)
                            {
                                StringList list3 = null;

                                code = ParserOps<string>.SplitList(
                                    interpreter, list[argumentIndex], 0,
                                    Length.Invalid, true, ref list3,
                                    ref result);

                                if (code != ReturnCode.Ok)
                                    break;

                                if (list3.Count > 2)
                                {
                                    result = String.Format(
                                        "too many fields in argument specifier \"{0}\"",
                                        list[argumentIndex]);

                                    code = ReturnCode.Error;
                                    break;
                                }
                                else if ((list3.Count == 0) || String.IsNullOrEmpty(list3[0]))
                                {
                                    result = "argument with no name";
                                    code = ReturnCode.Error;
                                    break;
                                }
                                else if (!Parser.IsSimpleScalarVariableName(list3[0],
                                        String.Format(Interpreter.ArgumentNotSimpleError, list3[0]),
                                        String.Format(Interpreter.ArgumentNotScalarError, list3[0]), ref result))
                                {
                                    code = ReturnCode.Error;
                                    break;
                                }

                                string argName = list3[0];
                                string argDefault = (list3.Count >= 2) ? list3[1] : null;

                                list2.Add(new StringPair(argName, argDefault));
                            }

                            if (code == ReturnCode.Ok)
                            {
                                ArgumentList formalArguments = new ArgumentList(
                                    list2, ArgumentFlags.NameOnly);

                                lock (interpreter.InternalSyncRoot) /* TRANSACTIONAL */
                                {
                                    ProcedureFlags procedureFlags = interpreter.ProcedureFlags;

                                    procedureFlags &= ~ProcedureFlags.NamedArguments;
                                    procedureFlags |= ProcedureFlags.PositionalArguments;

                                    IProcedure procedure;
                                    Result error = null;

                                    procedure = RuntimeOps.NewProcedure(
                                        interpreter, interpreter.AreNamespacesEnabled() ?
                                        NamespaceOps.MakeQualifiedName(interpreter, name) :
                                        ScriptOps.MakeCommandName(name), null, null,
                                        procedureFlags, formalArguments, null, arguments[3],
                                        ScriptLocation.Create(arguments[3]), clientData,
                                        ref error);

                                    if (procedure != null)
                                    {
                                        code = interpreter.AddOrUpdateProcedureWithReplace(
                                            procedure, clientData, ref result);

                                        if (code == ReturnCode.Ok)
                                            result = String.Empty;
                                    }
                                    else
                                    {
                                        result = error;
                                        code = ReturnCode.Error;
                                    }
                                }
                            }
                        }

                        if (code == ReturnCode.Error)
                            Engine.AddErrorInformation(interpreter, result,
                                String.Format("{0}    (creating proc \"{1}\")",
                                    Environment.NewLine, name));
                    }
                    else
                    {
                        result = "wrong # args: should be \"proc name args body\"";
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
