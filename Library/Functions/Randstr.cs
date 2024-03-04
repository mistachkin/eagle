/*
 * Randstr.cs --
 *
 * Copyright (c) 2007-2012 by Joe Mistachkin.  All rights reserved.
 *
 * See the file "license.terms" for information on usage and redistribution of
 * this file, and for a DISCLAIMER OF ALL WARRANTIES.
 *
 * RCS: @(#) $Id: $
 */

using System;
using System.Security.Cryptography;
using Eagle._Attributes;
using Eagle._Components.Private;
using Eagle._Components.Public;
using Eagle._Containers.Public;
using Eagle._Interfaces.Public;

namespace Eagle._Functions
{
    [ObjectId("b9168098-2447-4a77-825a-7661eaeefbb6")]
    [FunctionFlags(FunctionFlags.Safe | FunctionFlags.NonStandard)]
    [Arguments(Arity.Unary)]
    [TypeListFlags(TypeListFlags.IntegerTypes)]
    [ObjectGroup("random")]
    internal sealed class Randstr : Arguments
    {
        #region Public Constructors
        public Randstr(
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

            if (intValue <= 0)
            {
                error = "number of bytes must be greater than zero";
                return ReturnCode.Error;
            }

            try
            {
                IProvideEntropy provideEntropy;
                RandomNumberGenerator randomNumberGenerator;

                lock (interpreter.InternalSyncRoot) /* TRANSACTIONAL */
                {
                    provideEntropy = interpreter.InternalProvideEntropy;
                    randomNumberGenerator = interpreter.RandomNumberGenerator;
                }

                byte[] bytes;

                if (provideEntropy != null)
                {
                    bytes = new byte[intValue];

                    /* NO RESULT */
                    provideEntropy.GetBytes(ref bytes);
                }
                else if (randomNumberGenerator != null)
                {
                    bytes = new byte[intValue];

                    /* NO RESULT */
                    randomNumberGenerator.GetBytes(bytes);
                }
                else
                {
                    error = "random number generator not available";
                    return ReturnCode.Error;
                }

                string stringValue = null;

                if (StringOps.GetString(
                        null, bytes, EncodingType.Binary,
                        ref stringValue,
                        ref error) != ReturnCode.Ok)
                {
                    return ReturnCode.Error;
                }

                value = stringValue;
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
