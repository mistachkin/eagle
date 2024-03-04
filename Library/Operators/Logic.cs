/*
 * Logic.cs --
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
using Eagle._Interfaces.Private;
using Eagle._Interfaces.Public;

namespace Eagle._Operators
{
    [ObjectId("88a6d706-f3c5-412a-9f4b-3948703e82c1")]
    [Operands(Arity.UnaryAndBinary)]
    [ObjectGroup("core")]
    internal class Logic : Core
    {
        #region Public Constructors
        public Logic(
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

        #region IExecuteArgument Members
        public override ReturnCode Execute(
            Interpreter interpreter, /* in */
            IClientData clientData,  /* in */
            ArgumentList arguments,  /* in */
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

            try
            {
                IVariant operand1 = null;
                IVariant operand2 = null;

                if (Value.GetOperandsFromArguments(
                        interpreter, this, arguments, ValueFlags.AnyVariant,
                        interpreter.InternalCultureInfo, false, ref operand1,
                        ref operand2, ref error) != ReturnCode.Ok)
                {
                    return ReturnCode.Error;
                }

                if ((operand2 != null) && (Value.FixupVariants(
                        this, operand1, operand2, typeof(bool), typeof(bool),
                        false, false, ref error) != ReturnCode.Ok))
                {
                    return ReturnCode.Error;
                }

                return operand1.Calculate(
                    this, this.Lexeme, operand2, ref value, ref error);
            }
            catch (Exception e)
            {
                Engine.SetExceptionErrorCode(interpreter, e);

                error = String.Format("caught math exception: {0}", e);

                return ReturnCode.Error;
            }
        }
        #endregion
    }
}
