/*
 * Bool.cs --
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
    [ObjectId("11e36c1b-be45-42de-ba3c-e173047e722c")]
    [FunctionFlags(FunctionFlags.Safe | FunctionFlags.Standard)]
    [Arguments(Arity.Unary)]
    [TypeListFlags(TypeListFlags.NumberTypes)]
    [ObjectGroup("conversion")]
    internal sealed class Bool : Arguments
    {
        #region Public Constructors
        public Bool(
            IFunctionData functionData /* in */
            )
            : base(functionData)
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
            if (base.Execute(
                    interpreter, clientData, arguments, ref value,
                    ref error) != ReturnCode.Ok)
            {
                return ReturnCode.Error;
            }

            IVariant variant1 = null;

            if (Value.GetVariant(interpreter,
                    (IGetValue)arguments[1], ValueFlags.AnyVariant,
                    interpreter.InternalCultureInfo, ref variant1,
                    ref error) != ReturnCode.Ok)
            {
                return ReturnCode.Error;
            }

            try
            {
                if (variant1.IsDateTime())
                {
                    value = ConversionOps.ToBool(
                        (DateTime)variant1.Value); /* LOSSY */
                }
                else if (variant1.IsDouble())
                {
                    value = ConversionOps.ToBool(
                        (double)variant1.Value); /* LOSSY */
                }
                else if (variant1.IsDecimal())
                {
                    value = ConversionOps.ToBool(
                        (decimal)variant1.Value); /* LOSSY */
                }
                else if (variant1.IsWideInteger())
                {
                    value = ConversionOps.ToBool(
                        (long)variant1.Value); /* LOSSY */
                }
                else if (variant1.IsInteger())
                {
                    value = ConversionOps.ToBool(
                        (int)variant1.Value); /* LOSSY */
                }
                else if (variant1.IsBoolean())
                {
                    value = (bool)variant1.Value; /* NOP */
                }
                else
                {
                    error = String.Format(
                        "expected boolean value but got {0}",
                        FormatOps.WrapOrNull(arguments[1]));

                    return ReturnCode.Error;
                }
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
