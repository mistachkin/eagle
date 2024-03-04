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
    [ObjectId("b2d560ae-8a87-4ea5-a0ba-c9c1b74b3319")]
    [ObjectGroup("core")]
    internal class Arguments : Core
    {
        #region Public Constructors
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

            if (wantArgumentCount != (int)Arity.None)
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
