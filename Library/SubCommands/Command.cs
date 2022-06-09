/*
 * Command.cs --
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
using _Public = Eagle._Components.Public;

namespace Eagle._SubCommands
{
    [ObjectId("20783832-9aa4-4b72-8959-b2f6a7fcc6a4")]
    /*
     * NOTE: This command is "safe" because it does not accomplish anything by
     *       itself; instead, it just evaluates the configured script command.
     *       If the interpreter is marked as "safe", using this class will not
     *       permit the evaluated script to escape those restrictions.
     */
    [CommandFlags(CommandFlags.Safe | CommandFlags.SubCommand)]
    [ObjectGroup("engine")]
    internal sealed class Command : Default
    {
        #region Private Data
        //
        // NOTE: The script command to evaluate when this sub-command instance
        //       is executed.
        //
        private StringList scriptCommand;
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Public Constructors
        public Command(
            ISubCommandData subCommandData
            )
            : base(subCommandData)
        {
            SetupForSubCommandExecute(this.ClientData);
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Private Methods
        private string GetCommandName()
        {
            return ScriptOps.GetNameForExecute(null, this);
        }

        ///////////////////////////////////////////////////////////////////////

        private void SetupForSubCommandExecute(
            IClientData clientData
            )
        {
            object data = null;

            /* IGNORED */
            clientData = _Public.ClientData.UnwrapOrReturn(
                clientData, ref data);

            scriptCommand = data as StringList;
        }

        ///////////////////////////////////////////////////////////////////////

        private bool AllowedToUseArguments(
            ArgumentList arguments,
            ref Result error
            )
        {
            SubCommandFlags subCommandFlags = this.Flags;

            if (!FlagOps.HasFlags(subCommandFlags,
                    SubCommandFlags.StrictNoArguments, true))
            {
                return true;
            }

            int nameIndex = this.NameIndex;
            int nextIndex = nameIndex + 1;

            if ((arguments == null) || (arguments.Count <= nextIndex))
                return true;

            error = String.Format(
                "wrong # args: should be \"{0}\"", GetCommandName());

            return false;
        }

        ///////////////////////////////////////////////////////////////////////

        private ArgumentList GetArgumentsForExecute(
            ArgumentList arguments
            )
        {
            SubCommandFlags subCommandFlags = this.Flags;

            if (FlagOps.HasFlags(subCommandFlags,
                    SubCommandFlags.UseExecuteArguments, true))
            {
                return arguments;
            }

            return null;
        }

        ///////////////////////////////////////////////////////////////////////

        private int GetStartIndexForArguments(
            ArgumentList arguments
            )
        {
            SubCommandFlags subCommandFlags = this.Flags;

            if (!FlagOps.HasFlags(subCommandFlags,
                    SubCommandFlags.SkipNameArguments, true))
            {
                return 0;
            }

            int nameIndex = this.NameIndex;
            int nextIndex = nameIndex + 1;

            if ((arguments == null) || (arguments.Count < nextIndex))
                return 0;

            return nextIndex;
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

            int nameIndex = this.NameIndex;
            int nextIndex = nameIndex + 1;

            if (arguments.Count < nextIndex)
            {
                result = String.Format(
                    "wrong # args: should be \"{0} ?arg ...?\"",
                    GetCommandName());

                return ReturnCode.Error;
            }

            string subCommand = arguments[nameIndex];

            if (!StringOps.SubCommandEquals(subCommand, this.Name))
            {
                result = ScriptOps.BadSubCommand(
                    interpreter, null, null, subCommand, this, null,
                    null);

                return ReturnCode.Error;
            }

            //
            // NOTE: Does this sub-command accept arguments beyond
            //       the names of the command and sub-command?
            //
            if (!AllowedToUseArguments(arguments, ref result))
                return ReturnCode.Error;

            //
            // NOTE: Evaluate the configured script command, maybe
            //       adding all the local arguments, and return the
            //       results verbatim.
            //
            string name = StringList.MakeList(GetCommandName());

            ICallFrame frame = interpreter.NewTrackingCallFrame(name,
                CallFrameFlags.Evaluate | CallFrameFlags.SubCommand);

            interpreter.PushAutomaticCallFrame(frame);

            ReturnCode code = interpreter.EvaluateScript(
                ScriptOps.GetArgumentsForExecute(this,
                scriptCommand, GetArgumentsForExecute(arguments),
                GetStartIndexForArguments(arguments)), 0, ref result);

            if (code == ReturnCode.Error)
            {
                Engine.AddErrorInformation(interpreter, result,
                    String.Format("{0}    (\"{1}\" body line {2})",
                        Environment.NewLine, GetCommandName(),
                        Interpreter.GetErrorLine(interpreter)));
            }

            //
            // NOTE: Pop the original call frame that we pushed above
            //       and any intervening scope call frames that may be
            //       leftover (i.e. they were not explicitly closed).
            //
            /* IGNORED */
            interpreter.PopScopeCallFramesAndOneMore();
            return code;
        }
        #endregion
    }
}
