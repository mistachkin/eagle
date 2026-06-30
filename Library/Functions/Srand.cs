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
    /// <summary>
    /// This class implements the Eagle <c>srand</c> expression function, which
    /// reseeds the interpreter's pseudo-random number generator using its
    /// single integer argument and returns the first random value produced by
    /// the freshly seeded generator.  Because it modifies interpreter state,
    /// this function is marked unsafe.  See <c>core_language.md</c> for
    /// expression and function semantics.
    /// </summary>
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
        /// <summary>
        /// Constructs an instance of the <c>srand</c> expression function.
        /// </summary>
        /// <param name="functionData">
        /// The data used to create and identify this function, such as its
        /// name and flags.  This parameter may be null.
        /// </param>
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
        /// <summary>
        /// This method evaluates the <c>srand</c> function.  It validates the
        /// arguments using the base implementation, converts the single
        /// argument to an integer seed, creates a new
        /// <see cref="Random" /> instance seeded with that value, installs it
        /// as the interpreter's random number generator, and returns the first
        /// value produced by the new generator.
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
        /// function name; element one is the integer used to seed the random
        /// number generator.  This parameter should not be null.
        /// </param>
        /// <param name="value">
        /// Upon success, this is set to the first random value, in the range
        /// from zero to one, produced by the newly seeded generator.
        /// </param>
        /// <param name="error">
        /// Upon failure, this contains an appropriate error message.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success, with the result placed in
        /// <paramref name="value" />; otherwise,
        /// <see cref="ReturnCode.Error" /> when the argument is missing or not
        /// an integer, or a math exception occurs, with details placed in
        /// <paramref name="error" />.
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
