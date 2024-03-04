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
