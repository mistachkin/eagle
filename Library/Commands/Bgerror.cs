/*
 * Bgerror.cs --
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
using Eagle._Components.Public;
using Eagle._Containers.Public;
using Eagle._Interfaces.Public;

namespace Eagle._Commands
{
    [ObjectId("ed419ba9-aec5-4c49-a9a7-b41ed6b6eda0")]
    [CommandFlags(CommandFlags.Safe | CommandFlags.Standard)]
    [ObjectGroup("scriptEnvironment")]
    internal sealed class Bgerror : Core
    {
        public Bgerror(
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
                    if (arguments.Count == 2)
                    {
                        if (!interpreter.HasNoBackgroundError())
                        {
                            IInteractiveHost interactiveHost = interpreter.Host;

                            if (interactiveHost != null)
                            {
                                string message = arguments[1];

                                message = !String.IsNullOrEmpty(message) ?
                                    String.Format("{0}: {1}", this.Name, message) :
                                    this.Name;

                                interactiveHost.WriteResultLine(ReturnCode.Error,
                                    message, Interpreter.GetErrorLine(interpreter));

                                result = String.Empty;
                            }
                            else
                            {
                                result = "interpreter host not available";
                                code = ReturnCode.Error;
                            }
                        }
                        else
                        {
                            result = "background error handling disabled";
                            code = ReturnCode.Error;
                        }
                    }
                    else
                    {
                        result = "wrong # args: should be \"bgerror message\"";
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
