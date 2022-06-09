/*
 * Stub.cs --
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
    [ObjectId("ebb5400c-203d-4e16-b2e3-0715421a6b0d")]
    [CommandFlags(CommandFlags.Safe | CommandFlags.NonStandard)]
    [ObjectGroup("ensemble")]
    internal sealed class Stub : Core
    {
        public Stub(
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

            int nameIndex = ScriptOps.GetSubCommandNameIndex();
            int nextIndex = nameIndex + 1;

            if (arguments.Count < nextIndex)
            {
                result = String.Format(
                    "wrong # args: should be \"{0} option ?arg ...?\"",
                    this.Name);

                return ReturnCode.Error;
            }

            ReturnCode code;
            string subCommand = arguments[nameIndex];
            bool tried = false;

            code = ScriptOps.TryExecuteSubCommandFromEnsemble(
                interpreter, this, clientData, arguments, false,
                false, ref subCommand, ref tried, ref result);

            if ((code == ReturnCode.Ok) && !tried)
            {
                result = ScriptOps.BadSubCommand(
                    interpreter, null, null, subCommand, this,
                    null, null);

                code = ReturnCode.Error;
            }

            return code;
        }
        #endregion
    }
}
