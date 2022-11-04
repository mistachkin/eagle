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
using Eagle._Components.Private;
using Eagle._Components.Public;
using Eagle._Containers.Public;
using Eagle._Interfaces.Public;

namespace Eagle._Functions
{
    [ObjectId("c3c083a9-bab0-4153-8223-51ae7bc16953")]
    [FunctionFlags(FunctionFlags.Safe | FunctionFlags.Standard)]
    [Arguments(Arity.Nullary)]
    [ObjectGroup("random")]
    internal sealed class Rand : Core
    {
        #region Public Constructors
        public Rand(
            IFunctionData functionData
            )
            : base(functionData)
        {
            // do nothing.
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region IExecuteArgument Members
        public override ReturnCode Execute(
            Interpreter interpreter,
            IClientData clientData,
            ArgumentList arguments,
            ref Argument value,
            ref Result error
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

            if (arguments.Count != (this.Arguments + 1))
            {
                if (arguments.Count > (this.Arguments + 1))
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

            try
            {
                Random random;

                lock (interpreter.InternalSyncRoot)
                {
                    random = interpreter.Random;
                }

                if (random == null)
                {
                    error = "random number generator not available";
                    return ReturnCode.Error;
                }

                value = random.NextDouble();
                return ReturnCode.Ok;
            }
            catch (Exception e)
            {
                Engine.SetExceptionErrorCode(interpreter, e);

                error = String.Format(
                    "caught math exception: {0}",
                    e);

                return ReturnCode.Error;
            }
        }
        #endregion
    }
}
