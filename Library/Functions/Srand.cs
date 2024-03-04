/*
 * Srand.cs --
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

namespace Eagle._Functions
{
    [ObjectId("6dc54fd6-fc06-46a5-8eb2-40a2f9d0d5d2")]
    //
    // NOTE: *SECURITY* Modifies the state of the interpreter.
    //
    [FunctionFlags(FunctionFlags.Unsafe | FunctionFlags.Standard)]
    [Arguments(Arity.Unary)]
    [TypeListFlags(TypeListFlags.IntegerTypes)]
    [ObjectGroup("random")]
    internal sealed class Srand : Arguments
    {
        #region Public Constructors
        public Srand(
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

            int intValue = 0;

            if (Value.GetInteger2(
                    (IGetValue)arguments[1], ValueFlags.AnyInteger,
                    interpreter.InternalCultureInfo, ref intValue,
                    ref error) != ReturnCode.Ok)
            {
                return ReturnCode.Error;
            }

            try
            {
                Random random;

                lock (interpreter.InternalSyncRoot)
                {
                    random = new Random(intValue);
                    interpreter.Random = random; /* EXEMPT */
                }

                value = random.NextDouble();
            }
            catch (Exception e)
            {
                Engine.SetExceptionErrorCode(interpreter, e);

                error = String.Format("caught math exception: {0}", e);

                return ReturnCode.Error;
            }

            return ReturnCode.Ok;
        }
        #endregion
    }
}
