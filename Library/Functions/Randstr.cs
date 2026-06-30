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
    /// <summary>
    /// This class implements the Eagle <c>randstr</c> expression function,
    /// which returns a string of cryptographically random bytes whose length
    /// is specified by its single integer argument.  See
    /// <c>core_language.md</c> for expression and function semantics.
    /// </summary>
    [ObjectId("b9168098-2447-4a77-825a-7661eaeefbb6")]
    [FunctionFlags(FunctionFlags.Safe | FunctionFlags.NonStandard)]
    [Arguments(Arity.Unary)]
    [TypeListFlags(TypeListFlags.IntegerTypes)]
    [ObjectGroup("random")]
    internal sealed class Randstr : Arguments
    {
        #region Public Constructors
        /// <summary>
        /// Constructs an instance of the <c>randstr</c> expression function.
        /// </summary>
        /// <param name="functionData">
        /// The data used to create and identify this function, such as its
        /// name and flags.  This parameter may be null.
        /// </param>
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
        /// <summary>
        /// This method evaluates the <c>randstr</c> function.  It validates the
        /// arguments using the base implementation, converts the single
        /// argument to an integer count of bytes, obtains that many bytes from
        /// the interpreter entropy provider or random number generator, and
        /// converts the bytes to a string using binary encoding.
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
        /// function name; element one is the number of random bytes to
        /// produce.  This parameter should not be null.
        /// </param>
        /// <param name="value">
        /// Upon success, this is set to the generated random string.
        /// </param>
        /// <param name="error">
        /// Upon failure, this contains an appropriate error message.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success, with the result placed in
        /// <paramref name="value" />; otherwise,
        /// <see cref="ReturnCode.Error" /> when the argument is missing or not
        /// a positive integer, no random number generator is available, or an
        /// exception occurs, with details placed in <paramref name="error" />.
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
