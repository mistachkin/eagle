/*
 * Unsetf.cs --
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
    [ObjectId("945b2916-422a-4cb2-a18c-4693d868887f")]
    [Obsolete()]
    [CommandFlags(CommandFlags.Unsafe | CommandFlags.NonStandard |
                  CommandFlags.Obsolete | CommandFlags.Diagnostic)]
    [ObjectGroup("variable")]
    internal sealed class Unsetf : Core
    {
        #region Public Constructors
        public Unsetf(
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

            if (arguments.Count < 2)
            {
                result = String.Format(
                    "wrong # args: should be \"{0} varFlags ?varName varName ...?\"",
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

            if (arguments.Count > 2)
            {
                for (int argumentIndex = 2;
                        argumentIndex < arguments.Count;
                        argumentIndex++)
                {
                    if (interpreter.UnsetVariable(
                            flags, arguments[argumentIndex],
                            ref result) != ReturnCode.Ok)
                    {
                        return ReturnCode.Error;
                    }
                }
            }

            //
            // NOTE: Do nothing if no more arguments supplied, so as
            //       to match command documentation (COMPAT: Tcl).
            //
            result = String.Empty;
            return ReturnCode.Ok;
        }
        #endregion
    }
}
