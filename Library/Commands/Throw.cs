/*
 * Throw.cs --
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
    [ObjectId("c51886a9-e2d6-4cb9-ab39-9258bc2baeb9")]
    [CommandFlags(CommandFlags.Safe | CommandFlags.NonStandard)]
    [ObjectGroup("control")]
    internal sealed class Throw : Core
    {
        private static readonly string WrongNumArgs =
            "wrong # args: should be \"throw message ?returnCode? ?innerException?\"";

        ///////////////////////////////////////////////////////////////////////

        #region Public Constructors
        public Throw(
            ICommandData commandData /* in */
            )
            : base(commandData)
        {
            // do nothing.
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region IExecute Members
        public override ReturnCode Execute(
            Interpreter interpreter, /* in */
            IClientData clientData,  /* in */
            ArgumentList arguments,  /* in */
            ref Result result        /* out */
            )
        {
            if (interpreter == null)
            {
                result = "invalid interpreter";
                return ReturnCode.Error;
            }

            if (arguments == null)
            {
                result = "invalid argument list";
                return ReturnCode.Error;
            }

            int argumentCount = arguments.Count;

            if ((argumentCount < 2) || (argumentCount > 4))
            {
                result = WrongNumArgs;
                return ReturnCode.Error;
            }

            ReturnCode returnCode = ReturnCode.Error;

            if (argumentCount >= 3)
            {
                if (Value.GetReturnCode2(
                        arguments[2], ValueFlags.AnyReturnCode,
                        interpreter.InternalCultureInfo,
                        ref returnCode, ref result) != ReturnCode.Ok)
                {
                    return ReturnCode.Error;
                }
            }

            Exception innerException = null;

            if (argumentCount >= 4)
            {
                IObject @object = null;

                if (interpreter.GetObject(
                        arguments[3], LookupFlags.Default, ref @object,
                        ref result) != ReturnCode.Ok)
                {
                    return ReturnCode.Error;
                }

                if (@object != null)
                {
                    object objectValue = @object.Value;

                    if (objectValue != null)
                    {
                        innerException = objectValue as Exception;

                        if (innerException == null)
                        {
                            result = String.Format(
                                "object \"{0}\" is not an exception",
                                arguments[3]);

                            return ReturnCode.Error;
                        }
                    }
                }
            }

            //
            // NOTE: If we managed to process all arguments correctly, use
            //       requested error message and throw a script exception.
            //       This exception is guaranteed not to escape the script
            //       engine; however, it will be the script result.
            //
            Result message = arguments[1]; /* NOTE: Implicit conversion. */

            throw new ScriptException(returnCode, message, innerException);
        }
        #endregion
    }
}
