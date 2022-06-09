/*
 * Flags.cs --
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

namespace Eagle._Functions
{
    [ObjectId("f19b8c9b-2a9a-48fd-9c3e-ba0cf0b373ec")]
    //
    // NOTE: *SECURITY* Modifies the state of the interpreter.
    //
    [FunctionFlags(FunctionFlags.Unsafe | FunctionFlags.NonStandard)]
    [Arguments(Arity.Unary)]
    [TypeListFlags(TypeListFlags.StringTypes)]
    [ObjectGroup("control")]
    internal sealed class Flags : Core
    {
        public Flags(
            IFunctionData functionData
            )
            : base(functionData)
        {
            // do nothing.
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        #region IExecuteArgument Members
        public override ReturnCode Execute(
            Interpreter interpreter,
            IClientData clientData,
            ArgumentList arguments,
            ref Argument value,
            ref Result error
            )
        {
            ReturnCode code = ReturnCode.Ok;

            if (interpreter != null)
            {
                if (arguments != null)
                {
                    if (arguments.Count == (this.Arguments + 1))
                    {
                        lock (interpreter.InternalSyncRoot) /* TRANSACTIONAL */
                        {
                            object enumValue = EnumOps.TryParseFlags(
                                interpreter, typeof(ExpressionFlags),
                                interpreter.ExpressionFlags.ToString(),
                                arguments[1], interpreter.InternalCultureInfo,
                                true, true, true, ref error);

                            if (enumValue is ExpressionFlags)
                            {
                                try
                                {
                                    interpreter.ExpressionFlags = (ExpressionFlags)enumValue;
                                    value = interpreter.ExpressionFlags;
                                }
                                catch (Exception e)
                                {
                                    Engine.SetExceptionErrorCode(interpreter, e);

                                    error = String.Format(
                                        "caught math exception: {0}",
                                        e);

                                    code = ReturnCode.Error;
                                }
                            }
                            else
                            {
                                code = ReturnCode.Error;
                            }
                        }
                    }
                    else
                    {
                        if (arguments.Count > (this.Arguments + 1))
                            error = String.Format(
                                "too many arguments for math function {0}",
                                FormatOps.WrapOrNull(base.Name));
                        else
                            error = String.Format(
                                "too few arguments for math function {0}",
                                FormatOps.WrapOrNull(base.Name));

                        code = ReturnCode.Error;
                    }
                }
                else
                {
                    error = "invalid argument list";
                    code = ReturnCode.Error;
                }
            }
            else
            {
                error = "invalid interpreter";
                code = ReturnCode.Error;
            }

            return code;
        }
        #endregion
    }
}
