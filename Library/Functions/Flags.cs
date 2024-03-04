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
    internal sealed class Flags : Arguments
    {
        #region Public Constructors
        public Flags(
            IFunctionData functionData /* in */
            )
            : base(functionData)
        {
            // do nothing.
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region IExecuteArgument Members
        public override ReturnCode Execute(
            Interpreter interpreter, /* in */
            IClientData clientData,  /* in */
            ArgumentList arguments,  /* in */
            ref Argument value,      /* out */
            ref Result error         /* out */
            )
        {
            if (base.Execute(
                    interpreter, clientData, arguments, ref value,
                    ref error) != ReturnCode.Ok)
            {
                return ReturnCode.Error;
            }

            lock (interpreter.InternalSyncRoot) /* TRANSACTIONAL */
            {
                object enumValue = EnumOps.TryParseFlags(
                    interpreter, typeof(ExpressionFlags),
                    interpreter.ExpressionFlags.ToString(),
                    arguments[1], interpreter.InternalCultureInfo,
                    true, true, true, ref error);

                if (!(enumValue is ExpressionFlags))
                    return ReturnCode.Error;

                try
                {
                    interpreter.ExpressionFlags =
                        (ExpressionFlags)enumValue;

                    value = interpreter.ExpressionFlags;
                }
                catch (Exception e)
                {
                    Engine.SetExceptionErrorCode(interpreter, e);

                    error = String.Format("caught math exception: {0}", e);

                    return ReturnCode.Error;
                }
            }

            return ReturnCode.Ok;
        }
        #endregion
    }
}
