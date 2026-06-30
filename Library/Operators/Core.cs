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
using Eagle._Interfaces.Private;
using Eagle._Interfaces.Public;

namespace Eagle._Operators
{
    /// <summary>
    /// This class provides the base implementation for all expression
    /// operators that are part of the Eagle core operator set.  It derives
    /// from <see cref="Default" /> (the base available to external operators)
    /// and primarily ensures that the cached operator flags are set correctly
    /// for every core operator.  See <c>core_language.md</c> for expression
    /// and operator semantics.
    /// </summary>
    [ObjectId("49e7347b-ff6f-49cf-8ef0-810f4e1452b0")]
    [OperatorFlags(OperatorFlags.Core)]
    [ObjectGroup("core")]
    internal class Core : Default, IExecute
    {
        #region Public Constructors
        //
        // NOTE: In the future, behavior specific to operators in the core
        //       will be implemented here rather than in _Operators.Default
        //       (which is available to external operators to derive from).
        //       For now, the primary job of this class is to set the
        //       cached operator flags correctly for all operators in the
        //       core operator set.
        //
        /// <summary>
        /// Constructs an instance of the core operator base class.  When the
        /// supplied operator data does not request that attributes be skipped,
        /// the cached operator flags are augmented with the operator flags
        /// declared on this type and its base type.
        /// </summary>
        /// <param name="operatorData">
        /// The data used to create and identify this operator, such as its
        /// name and flags.  This parameter may be null.
        /// </param>
        public Core(
            IOperatorData operatorData /* in */
            )
            : base(operatorData)
        {
            if ((operatorData == null) || !FlagOps.HasFlags(
                    operatorData.Flags, OperatorFlags.NoAttributes, true))
            {
                this.Flags |=
                    AttributeOps.GetOperatorFlags(GetType().BaseType) |
                    AttributeOps.GetOperatorFlags(this);
            }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region IExecute Members
        /// <summary>
        /// This method executes the operator for a single invocation via the
        /// general <see cref="IExecute" /> entry point.  It delegates to the
        /// expression-oriented <c>Execute</c> overload that yields an
        /// <see cref="Argument" /> value, then copies either that value (on
        /// success) or the error (on failure) into the
        /// <paramref name="result" /> parameter.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter context this operator is executing in.  This
        /// parameter should not be null.
        /// </param>
        /// <param name="clientData">
        /// The extra, operator-specific data supplied when the operator was
        /// created, if any.  This parameter may be null.
        /// </param>
        /// <param name="arguments">
        /// The list of arguments for this invocation, including the operator
        /// name as the element at index zero.  This parameter should not be
        /// null.
        /// </param>
        /// <param name="result">
        /// Upon success, this contains the result value produced by the
        /// operator.  Upon failure, this contains an appropriate error
        /// message.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise, a non-Ok value
        /// with details placed in the <paramref name="result" /> parameter.
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
