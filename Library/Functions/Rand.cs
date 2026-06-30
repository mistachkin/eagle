/*
 * Rand.cs --
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
    /// This class implements the Eagle <c>rand</c> expression function, which
    /// returns a pseudo-random floating-point value greater than or equal to
    /// <c>0.0</c> and less than <c>1.0</c>, using the random number generator
    /// associated with the interpreter.  This function takes no arguments.  See
    /// <c>core_language.md</c> for expression and function semantics.
    /// </summary>
    [ObjectId("c3c083a9-bab0-4153-8223-51ae7bc16953")]
    [FunctionFlags(FunctionFlags.Safe | FunctionFlags.Standard)]
    [Arguments(Arity.Nullary)]
    [ObjectGroup("random")]
    internal sealed class Rand : Arguments
    {
        #region Public Constructors
        /// <summary>
        /// Constructs an instance of the <c>rand</c> expression function.
        /// </summary>
        /// <param name="functionData">
        /// The data used to create and identify this function, such as its
        /// name and flags.  This parameter may be null.
        /// </param>
        public Rand(
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
        /// This method evaluates the <c>rand</c> function.  It validates the
        /// arguments using the base implementation and then produces the next
        /// pseudo-random floating-point value from the interpreter's random
        /// number generator.
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
        /// function name; this function accepts no further arguments.  This
        /// parameter should not be null.
        /// </param>
        /// <param name="value">
        /// Upon success, this is set to the computed pseudo-random value, which
        /// is greater than or equal to <c>0.0</c> and less than <c>1.0</c>.
        /// </param>
        /// <param name="error">
        /// Upon failure, this contains an appropriate error message.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success, with the result placed in
        /// <paramref name="value" />; otherwise,
        /// <see cref="ReturnCode.Error" /> when argument validation fails, the
        /// random number generator is not available, or a math exception
        /// occurs, with details placed in <paramref name="error" />.
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
                Random random;

                lock (interpreter.InternalSyncRoot)
                {
                    random = interpreter.Random; /* EXEMPT */
                }

                if (random == null)
                {
                    error = "random number generator not available";
                    return ReturnCode.Error;
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
