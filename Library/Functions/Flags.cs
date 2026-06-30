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
    /// <summary>
    /// This class implements the Eagle <c>flags</c> expression function, which
    /// sets the per-interpreter expression evaluation flags from its single
    /// argument and returns their resulting value.  Because it modifies
    /// interpreter state, this function is unsafe and non-standard.  See
    /// <c>core_language.md</c> for expression and function semantics.
    /// </summary>
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
        /// <summary>
        /// Constructs an instance of the <c>flags</c> expression function.
        /// </summary>
        /// <param name="functionData">
        /// The data used to create and identify this function, such as its
        /// name and flags.  This parameter may be null.
        /// </param>
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
        /// <summary>
        /// This method evaluates the <c>flags</c> function.  It validates the
        /// arguments using the base implementation, parses the single argument
        /// as a set of <see cref="ExpressionFlags" /> values relative to the
        /// interpreter's current expression flags, assigns the parsed flags
        /// back to the interpreter, and returns the resulting flags.  The
        /// update is performed while holding the interpreter lock.
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
        /// function name; element one is the expression flags value to apply.
        /// This parameter should not be null.
        /// </param>
        /// <param name="value">
        /// Upon success, this is set to the interpreter's resulting expression
        /// flags.
        /// </param>
        /// <param name="error">
        /// Upon failure, this contains an appropriate error message.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success, with the result placed in
        /// <paramref name="value" />; otherwise,
        /// <see cref="ReturnCode.Error" /> when the argument is missing, cannot
        /// be parsed as expression flags, or a math exception occurs, with
        /// details placed in <paramref name="error" />.
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
