/*
 * Fpclassify.cs --
 *
 * Copyright (c) 2007-2012 by Joe Mistachkin.  All rights reserved.
 *
 * See the file "license.terms" for inFpclassifyion on usage and redistribution of
 * this file, and for a DISCLAIMER OF ALL WARRANTIES.
 *
 * RCS: @(#) $Id: $
 */

using Eagle._Attributes;
using Eagle._Components.Private;
using Eagle._Components.Public;
using Eagle._Containers.Public;
using Eagle._Interfaces.Public;

namespace Eagle._Commands
{
    [ObjectId("fcf8ffd4-41d5-4956-96f4-db61570d0c94")]
    [CommandFlags(CommandFlags.Safe | CommandFlags.Standard)]
    [ObjectGroup("expression")]
    internal sealed class Fpclassify : Core
    {
        public Fpclassify(
            ICommandData commandData
            )
            : base(commandData)
        {
            // do nothing.
        }

        ///////////////////////////////////////////////////////////////////////

        #region IExecute Members
        public override ReturnCode Execute(
            Interpreter interpreter,
            IClientData clientData,
            ArgumentList arguments,
            ref Result result
            )
        {
            if (interpreter == null)
            {
                result = "invalid interpreter";
                return ReturnCode.Error;
            }

            if (arguments == null)
            {
                result = "invalid argument list";
                return ReturnCode.Error;
            }

            if (arguments.Count != 2)
            {
                result = "wrong # args: should be \"fpclassify value\"";
                return ReturnCode.Error;
            }

            double value = 0.0;

            if (Value.GetDouble(
                    (IGetValue)arguments[1],
                    interpreter.InternalCultureInfo,
                    ref value, ref result) != ReturnCode.Ok)
            {
                return ReturnCode.Error;
            }

            result = MathOps.Classify(value).ToString().ToLowerInvariant();
            return ReturnCode.Ok;
        }
        #endregion
    }
}
