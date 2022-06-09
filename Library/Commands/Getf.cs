/*
 * Getf.cs --
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
using Eagle._Components.Public;
using Eagle._Containers.Public;
using Eagle._Interfaces.Public;

namespace Eagle._Commands
{
    [ObjectId("ac0a9ff6-87a3-49ed-8402-b2ab7e40aa32")]
    [Obsolete()]
    [CommandFlags(CommandFlags.Unsafe | CommandFlags.NonStandard |
                  CommandFlags.Obsolete | CommandFlags.Diagnostic)]
    [ObjectGroup("variable")]
    internal sealed class Getf : Core
    {
        #region Public Constructors
        public Getf(
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

            if (arguments.Count != 2)
            {
                result = String.Format(
                    "wrong # args: should be \"{0} varName\"",
                    this.Name);

                return ReturnCode.Error;
            }

            ReturnCode code;
            VariableFlags flags = VariableFlags.NoElement;
            IVariable variable = null;
            Result error = null;

            code = interpreter.GetVariableViaResolversWithSplit(
                arguments[1], ref flags, ref variable, ref error);

            result = StringList.MakeList(
                code, error, flags, (variable != null) ?
                variable.Flags : VariableFlags.None);

            return ReturnCode.Ok;
        }
        #endregion
    }
}
