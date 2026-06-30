/*
 * Arguments.cs --
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
    /// This class provides the base implementation for Eagle expression
    /// functions that require a fixed argument count (arity).  It validates the
    /// number of supplied arguments against the function's declared arity before
    /// the derived function performs its computation.  See
    /// <c>core_language.md</c> for expression and function semantics.
    /// </summary>
    [ObjectId("b2d560ae-8a87-4ea5-a0ba-c9c1b74b3319")]
    [ObjectGroup("core")]
    internal class Arguments : Core
    {
        #region Public Constructors
        /// <summary>
        /// Constructs an instance of the argument-checking function base class.
        /// </summary>
        /// <param name="functionData">
        /// The data used to create and identify this function, such as its
        /// name and flags.  This parameter may be null.
        /// </param>
        public Arguments(
            IFunctionData functionData /* in */
            )
            : base(functionData)
        {
            if ((functionData == null) || !FlagOps.HasFlags(
                    functionData.Flags, FunctionFlags.NoAttributes, true))
            {
                this.Flags |=
                    AttributeOps.GetFunctionFlags(GetType().BaseType) |
                    AttributeOps.GetFunctionFlags(this);
            }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Protected Methods
        /// <summary>
        /// This method validates the supplied arguments against the declared
        /// arity for this function.  When the function expects a fixed number of
        /// arguments, it verifies that exactly that many were supplied (in
        /// addition to the leading function-name element) and reports the count
        /// of supplied arguments back to the caller.
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
        /// function name; the remaining elements are the function arguments.
        /// This parameter should not be null.
        /// </param>
        /// <param name="argumentCount">
        /// Upon success, this is set to the total number of supplied arguments,
        /// including the leading function-name element.
        /// </param>
        /// <param name="value">
        /// This parameter is not modified by this method; it is present so that
        /// derived overrides share an identical signature.
        /// </param>
        /// <param name="error">
        /// Upon failure, this contains an appropriate error message.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> when the argument count is valid;
        /// otherwise, <see cref="ReturnCode.Error" /> when the interpreter or
        /// argument list is invalid, or too few or too many arguments were
        /// supplied, with details placed in <paramref name="error" />.
        /// </returns>
        protected virtual ReturnCode Execute(
            Interpreter interpreter, /* in */
            IClientData clientData,  /* in */
            ArgumentList arguments,  /* in */
            ref int argumentCount,   /* out */
            ref Argument value,      /* out */
            ref Result error         /* out */
            )
        {
            if (interpreter == null)
            {
                error = "invalid interpreter";
                return ReturnCode.Error;
            }

            if (arguments == null)
            {
                error = "invalid argument list";
                return ReturnCode.Error;
            }

            int wantArgumentCount = this.Arguments;
            int haveArgumentCount = arguments.Count;

            if (wantArgumentCount != (int)Arity.Any)
            {
                wantArgumentCount++;

                if (haveArgumentCount != wantArgumentCount)
                {
                    if (haveArgumentCount > wantArgumentCount)
                    {
                        error = String.Format(
                            "too many arguments for math function {0}",
                            FormatOps.WrapOrNull(base.Name));
                    }
                    else
                    {
                        error = String.Format(
                            "too few arguments for math function {0}",
                            FormatOps.WrapOrNull(base.Name));
                    }

                    return ReturnCode.Error;
                }
            }

            argumentCount = haveArgumentCount;
            return ReturnCode.Ok;
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region IExecuteArgument Members
        /// <summary>
        /// This method evaluates the function by validating its argument count
        /// using the protected helper of the same name.  Derived functions
        /// typically call this base implementation first and then perform their
        /// computation.
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
        /// function name; the remaining elements are the function arguments.
        /// This parameter should not be null.
        /// </param>
        /// <param name="value">
        /// This parameter is not modified by this method; it is present so that
        /// the signature matches the function execution contract.
        /// </param>
        /// <param name="error">
        /// Upon failure, this contains an appropriate error message.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> when the argument count is valid;
        /// otherwise, <see cref="ReturnCode.Error" />, with details placed in
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
            int argumentCount = 0;

            return Execute(
                interpreter, clientData, arguments, ref argumentCount,
                ref value, ref error);
        }
        #endregion
    }
}
