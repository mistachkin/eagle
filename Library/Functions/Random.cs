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
    /// <summary>
    /// This class implements the Eagle <c>random</c> expression function,
    /// which returns a random 64-bit signed integer obtained from the
    /// interpreter's entropy source or random number generator.  See
    /// <c>core_language.md</c> for expression and function semantics.
    /// </summary>
    [ObjectId("1497187e-2051-473e-b55c-179b4c74d71d")]
    [FunctionFlags(FunctionFlags.Safe | FunctionFlags.NonStandard)]
    [Arguments(Arity.Nullary)]
    [ObjectGroup("random")]
    internal sealed class _Random : Arguments
    {
        #region Public Constructors
        /// <summary>
        /// Constructs an instance of the <c>random</c> expression function.
        /// </summary>
        /// <param name="functionData">
        /// The data used to create and identify this function, such as its
        /// name and flags.  This parameter may be null.
        /// </param>
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
        /// <summary>
        /// This method evaluates the <c>random</c> function.  It validates the
        /// arguments using the base implementation, obtains random bytes from
        /// the interpreter's entropy provider or random number generator, and
        /// produces a 64-bit signed integer from those bytes.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter context this function is executing in.  This
        /// parameter should not be null.
        /// </param>
        /// <param name="clientData">
        /// The extra, function-specific data supplied when this function was
        /// created, if any.  This parameter may be null.
        /// </param>
        /// <param name="arguments">
        /// The list of arguments for this invocation.  Element zero is the
        /// function name; this function takes no further arguments.  This
        /// parameter should not be null.
        /// </param>
        /// <param name="value">
        /// Upon success, this is set to the generated random 64-bit signed
        /// integer.
        /// </param>
        /// <param name="error">
        /// Upon failure, this contains an appropriate error message.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success, with the result placed in
        /// <paramref name="value" />; otherwise,
        /// <see cref="ReturnCode.Error" /> when argument validation fails, no
        /// random number generator is available, or a math exception occurs,
        /// with details placed in <paramref name="error" />.
        /// </returns>
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
