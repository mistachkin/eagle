/*
 * Core.cs --
 *
 * Copyright (c) 2007-2012 by Joe Mistachkin.  All rights reserved.
 *
 * See the file "license.terms" for information on usage and redistribution of
 * this file, and for a DISCLAIMER OF ALL WARRANTIES.
 *
 * RCS: @(#) $Id: $
 */

using Eagle._Attributes;
using Eagle._Components.Private;
using Eagle._Components.Public;
using Eagle._Containers.Public;
using Eagle._Interfaces.Public;

namespace Eagle._Functions
{
    /// <summary>
    /// This class provides the base implementation for expression functions
    /// that belong to the Eagle core function set.  It extends
    /// <see cref="Default" /> and implements <see cref="IExecute" /> so that a
    /// core function may also be invoked through the command-style execution
    /// interface.  Its primary job is to set the cached function flags
    /// correctly for all functions in the core function set.  See
    /// <c>core_language.md</c> for expression and function semantics.
    /// </summary>
    [ObjectId("91de9380-c952-4fef-8839-4bde3f6f16a4")]
    [FunctionFlags(FunctionFlags.Core)]
    [ObjectGroup("core")]
    internal class Core : Default, IExecute
    {
        #region Public Constructors
        //
        // NOTE: In the future, behavior specific to functions in the core
        //       will be implemented here rather than in _Functions.Default
        //       (which is available to external functions to derive from).
        //       For now, the primary job of this class is to set the
        //       cached function flags correctly for all functions in the
        //       core function set.
        //
        /// <summary>
        /// Constructs an instance of the core expression function base class.
        /// When the supplied function data does not request that attributes be
        /// ignored, the function flags declared on the base type and on this
        /// instance are merged into the cached flags.
        /// </summary>
        /// <param name="functionData">
        /// The data used to create and identify this function, such as its
        /// name and flags.  This parameter may be null.
        /// </param>
        public Core(
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

        #region IExecute Members
        /// <summary>
        /// This method executes the function using the command-style execution
        /// interface.  It forwards to the expression-style
        /// <c>Execute</c> overload and then transfers the computed value or the
        /// error message into <paramref name="result" /> depending on the
        /// outcome.
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
        /// function name; the remaining elements are its arguments.  This
        /// parameter should not be null.
        /// </param>
        /// <param name="result">
        /// Upon success, this is set to the computed value; upon failure, this
        /// contains an appropriate error message.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise,
        /// <see cref="ReturnCode.Error" />, with details placed in
        /// <paramref name="result" />.
        /// </returns>
        public virtual ReturnCode Execute(
            Interpreter interpreter, /* in */
            IClientData clientData,  /* in */
            ArgumentList arguments,  /* in */
            ref Result result        /* out */
            )
        {
            ReturnCode code;
            Argument value = null;
            Result error = null;

            code = Execute(
                interpreter, clientData, arguments, ref value, ref error);

            if (code == ReturnCode.Ok)
                result = value;
            else
                result = error;

            return code;
        }
        #endregion
    }
}
