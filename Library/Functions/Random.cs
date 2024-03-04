/*
 * Random.cs --
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
using Eagle._Components.Public;
using Eagle._Containers.Public;
using Eagle._Interfaces.Public;

namespace Eagle._Functions
{
    [ObjectId("1497187e-2051-473e-b55c-179b4c74d71d")]
    [FunctionFlags(FunctionFlags.Safe | FunctionFlags.NonStandard)]
    [Arguments(Arity.Nullary)]
    [ObjectGroup("random")]
    internal sealed class _Random : Arguments
    {
        #region Public Constructors
        public _Random(
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
                    bytes = new byte[sizeof(long)];

                    /* NO RESULT */
                    provideEntropy.GetBytes(ref bytes);
                }
                else if (randomNumberGenerator != null)
                {
                    bytes = new byte[sizeof(long)];

                    /* NO RESULT */
                    randomNumberGenerator.GetBytes(bytes);
                }
                else
                {
                    error = "random number generator not available";
                    return ReturnCode.Error;
                }

                value = BitConverter.ToInt64(bytes, 0);
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
