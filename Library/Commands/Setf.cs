/*
 * Setf.cs --
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

namespace Eagle._Commands
{
    [ObjectId("7c8c73c9-41f9-496f-b1a5-1b4a9aa421c4")]
    [Obsolete()]
    [CommandFlags(CommandFlags.Unsafe | CommandFlags.NonStandard |
                  CommandFlags.Obsolete | CommandFlags.Diagnostic)]
    [ObjectGroup("variable")]
    internal sealed class Setf : Core
    {
        #region Public Constructors
        public Setf(
            ICommandData commandData
            )
            : base(commandData)
        {
            // do nothing.
        }
        #endregion

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

            if ((arguments.Count != 3) && (arguments.Count != 4))
            {
                result = String.Format(
                    "wrong # args: should be \"{0} varFlags varName ?newValue?\"",
                    this.Name);

                return ReturnCode.Error;
            }

            VariableFlags flags = VariableFlags.None;

            object enumValue = EnumOps.TryParseFlags(
                interpreter, typeof(VariableFlags), flags.ToString(),
                arguments[1], interpreter.InternalCultureInfo, true,
                true, true, ref result);

            if (!(enumValue is VariableFlags))
                return ReturnCode.Error;

            flags = (VariableFlags)enumValue;

            string varName = arguments[2];

            if (arguments.Count == 3)
            {
                return interpreter.GetVariableValue(
                    flags, varName, ref result, ref result);
            }
            else if (arguments.Count == 4)
            {
                string varValue = arguments[3];

                if (interpreter.SetVariableValue(
                        flags, varName, varValue, null,
                        ref result) == ReturnCode.Ok)
                {
                    //
                    // NOTE: Maybe append mode?  Re-get value now.
                    //
                    return interpreter.GetVariableValue(
                        flags, varName, ref result, ref result);
                }
            }

            return ReturnCode.Error;
        }
        #endregion
    }
}
