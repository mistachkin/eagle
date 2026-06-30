/*
 * Nop.cs --
 *
 * Copyright (c) 2007-2012 by Joe Mistachkin.  All rights reserved.
 *
 * See the file "license.terms" for information on usage and redistribution of
 * this file, and for a DISCLAIMER OF ALL WARRANTIES.
 *
 * RCS: @(#) $Id: $
 */

using Eagle._Attributes;
using Eagle._Components.Public;
using Eagle._Containers.Public;
using Eagle._Interfaces.Public;

namespace Eagle._Functions
{
    /// <summary>
    /// This class implements the Eagle <c>nop</c> expression function, which
    /// accepts any number of arguments of any type, ignores them, and always
    /// succeeds without producing a value.  See <c>core_language.md</c> for
    /// expression and function semantics.
    /// </summary>
    [ObjectId("801dfe01-7ec6-4fd9-98c6-8c3eada55da0")]
    [FunctionFlags(FunctionFlags.Safe | FunctionFlags.NonStandard)]
    [Arguments(Arity.Any)]
    [TypeListFlags(TypeListFlags.AllTypes)]
    [ObjectGroup("nop")]
    internal sealed class Nop : Core
    {
        #region Public Constructors
        /// <summary>
        /// Constructs an instance of the <c>nop</c> expression function.
        /// </summary>
        /// <param name="functionData">
        /// The data used to create and identify this function, such as its
        /// name and flags.  This parameter may be null.
        /// </param>
        public Nop(
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
        /// This method evaluates the <c>nop</c> function.  It performs no
        /// action and ignores all of its arguments, leaving
        /// <paramref name="value" /> unchanged and always succeeding.
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
        /// function name; any further elements are accepted and ignored.  This
        /// parameter should not be null.
        /// </param>
        /// <param name="value">
        /// This parameter is not modified by this function.
        /// </param>
        /// <param name="error">
        /// This parameter is not modified by this function.
        /// </param>
        /// <returns>
        /// Always <see cref="ReturnCode.Ok" />.
        /// </returns>
        public override ReturnCode Execute(
            Interpreter interpreter, /* in */
            IClientData clientData,  /* in */
            ArgumentList arguments,  /* in */
            ref Argument value,      /* out */
            ref Result error         /* out */
            )
        {
            return ReturnCode.Ok;
        }
        #endregion
    }
}
