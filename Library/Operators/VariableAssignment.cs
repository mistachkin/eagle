/*
 * VariableAssignment.cs --
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
    [ObjectId("e46b3b67-cae1-4142-8c4f-af6cd776ced6")]
    [OperatorFlags(
        OperatorFlags.NonStandard | OperatorFlags.Assignment)]
    [Lexeme(Lexeme.VariableAssignment)]
    [Operands(Arity.Binary)]
    [TypeListFlags(TypeListFlags.AllTypes)]
    [ObjectGroup("assignment")]
    [ObjectName(Operators.VariableAssignment)]
    internal sealed class VariableAssignment : Core
    {
        #region Private Constants
        private const string SafeError =
            "permission denied: safe interpreter cannot use operator {0}";
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Public Constructors
        public VariableAssignment(
            IOperatorData operatorData /* in */
            )
            : base(operatorData)
        {
            // do nothing.
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

            if (interpreter.InternalIsSafeOrSdk())
            {
                //
                // BUGBUG: Technically, this is not quite 100%
                //         accurate.  If this interpreter was
                //         created for use by the license -OR-
                //         security SDK, it may not be "safe";
                //         however, to keep things simple, just
                //         use that error message.
                //
                error = String.Format(
                    SafeError, FormatOps.WrapOrNull(base.Name));

                return ReturnCode.Error;
            }

            try
            {
                IVariant operand1 = null;
                IVariant operand2 = null;

                if (Value.GetOperandsFromArguments(interpreter,
                        this, arguments, ValueFlags.String, ValueFlags.None,
                        interpreter.InternalCultureInfo, false, ref operand1,
                        ref operand2, ref error) != ReturnCode.Ok)
                {
                    return ReturnCode.Error;
                }

                if (interpreter.SetVariableValue2(
                        VariableFlags.None, null, (string)operand1.Value,
                        operand2.Value, ref error) != ReturnCode.Ok)
                {
                    return ReturnCode.Error;
                }

                value = Argument.InternalCreate(operand2);
                return ReturnCode.Ok;
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
