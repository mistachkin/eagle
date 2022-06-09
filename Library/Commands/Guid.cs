/*
 * Guid.cs --
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
using Eagle._Containers.Public;
using Eagle._Interfaces.Public;

namespace Eagle._Commands
{
    [ObjectId("0214cfc3-e69e-4e85-8623-336b9ba8076d")]
    [CommandFlags(CommandFlags.Safe | CommandFlags.NonStandard)]
    [ObjectGroup("string")]
    internal sealed class _Guid : Core
    {
        public _Guid(
            ICommandData commandData
            )
            : base(commandData)
        {
            // do nothing.
        }

        #region IEnsemble Members
        private readonly EnsembleDictionary subCommands = new EnsembleDictionary(new string[] { 
            "compare", "isnull", "isvalid", "new", "null"
        });

        public override EnsembleDictionary SubCommands
        {
            get { return subCommands; }
        }
        #endregion
        
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
                        string subCommand = arguments[1];
                        bool tried = false;

                        code = ScriptOps.TryExecuteSubCommandFromEnsemble(
                            interpreter, this, clientData, arguments, true,
                            false, ref subCommand, ref tried, ref result);

                        if ((code == ReturnCode.Ok) && !tried)
                        {
                            switch (subCommand)
                            {
                                case "compare":
                                    {
                                        if (arguments.Count == 4)
                                        {
                                            Guid guid1 = Guid.Empty;

                                            code = Value.GetGuid(arguments[2], interpreter.InternalCultureInfo, ref guid1, ref result);

                                            if (code == ReturnCode.Ok)
                                            {
                                                Guid guid2 = Guid.Empty;

                                                code = Value.GetGuid(arguments[3], interpreter.InternalCultureInfo, ref guid2, ref result);

                                                if (code == ReturnCode.Ok)
                                                    result = guid1.CompareTo(guid2);
                                            }
                                        }
                                        else
                                        {
                                            result = "wrong # args: should be \"guid compare guid1 guid2\"";
                                            code = ReturnCode.Error;
                                        }
                                        break;
                                    }
                                case "isnull":
                                    {
                                        if (arguments.Count == 3)
                                        {
                                            Guid guid = Guid.Empty;

                                            code = Value.GetGuid(arguments[2], interpreter.InternalCultureInfo, ref guid, ref result);

                                            if (code == ReturnCode.Ok)
                                                result = ConversionOps.ToInt(guid.CompareTo(Guid.Empty) == 0);
                                        }
                                        else
                                        {
                                            result = "wrong # args: should be \"guid isnull guid\"";
                                            code = ReturnCode.Error;
                                        }
                                        break;
                                    }
                                case "isvalid":
                                    {
                                        if (arguments.Count == 3)
                                        {
                                            Guid guid = Guid.Empty;
                                            Result localResult = null;

                                            result = ConversionOps.ToInt(Value.GetGuid(arguments[2], interpreter.InternalCultureInfo, ref guid, ref localResult) == ReturnCode.Ok);
                                        }
                                        else
                                        {
                                            result = "wrong # args: should be \"guid isvalid guid\"";
                                            code = ReturnCode.Error;
                                        }
                                        break;
                                    }
                                case "new":
                                    {
                                        if (arguments.Count == 2)
                                        {
                                            result = Guid.NewGuid();
                                        }
                                        else
                                        {
                                            result = "wrong # args: should be \"guid new\"";
                                            code = ReturnCode.Error;
                                        }
                                        break;
                                    }
                                case "null":
                                    {
                                        if (arguments.Count == 2)
                                        {
                                            result = Guid.Empty;
                                        }
                                        else
                                        {
                                            result = "wrong # args: should be \"guid null\"";
                                            code = ReturnCode.Error;
                                        }
                                        break;
                                    }
                                default:
                                    {
                                        result = ScriptOps.BadSubCommand(
                                            interpreter, null, null, subCommand, this, null, null);

                                        code = ReturnCode.Error;
                                        break;
                                    }
                            }
                        }
                    }
                    else
                    {
                        result = "wrong # args: should be \"guid option ?arg ...?\"";
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
