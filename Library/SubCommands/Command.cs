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
    /// <summary>
    /// This class implements a sub-command that, when executed, evaluates a
    /// pre-configured script command (optionally appending the local
    /// arguments) within a freshly pushed call frame and returns its result.
    /// It is "safe" because it does not perform any privileged operation by
    /// itself; the evaluated script is subject to the interpreter's existing
    /// "safe" restrictions.
    /// </summary>
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
        /// <summary>
        /// The script command to evaluate when this sub-command instance is
        /// executed.
        /// </summary>
        private StringList scriptCommand;
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Public Constructors
        /// <summary>
        /// Constructs an instance of this sub-command, extracting the script
        /// command to be evaluated from the associated client data.
        /// </summary>
        /// <param name="subCommandData">
        /// The data used to create and identify this sub-command, such as its
        /// name and flags.  This parameter may be null.
        /// </param>
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
        /// <summary>
        /// This method returns the name by which this sub-command was invoked,
        /// suitable for use in error messages.
        /// </summary>
        /// <returns>
        /// The invocation name of this sub-command.
        /// </returns>
        private string GetCommandName()
        {
            return ScriptOps.GetNameForExecute(null, this);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method extracts and stores the script command to be evaluated
        /// from the supplied client data.
        /// </summary>
        /// <param name="clientData">
        /// The client data that wraps the script command to evaluate.  This
        /// parameter may be null.
        /// </param>
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

        /// <summary>
        /// This method determines whether this sub-command is permitted to
        /// receive arguments beyond the names of the command and sub-command.
        /// </summary>
        /// <param name="arguments">
        /// The list of arguments for the current invocation.  This parameter
        /// may be null.
        /// </param>
        /// <param name="error">
        /// Upon failure, this contains an appropriate error message.
        /// </param>
        /// <returns>
        /// True if extra arguments are allowed (or none were supplied);
        /// otherwise, false.
        /// </returns>
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

        /// <summary>
        /// This method determines which arguments, if any, should be appended
        /// to the configured script command when it is evaluated.
        /// </summary>
        /// <param name="arguments">
        /// The list of arguments for the current invocation.  This parameter
        /// may be null.
        /// </param>
        /// <returns>
        /// The arguments to append to the script command, or null if none
        /// should be appended.
        /// </returns>
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

        /// <summary>
        /// This method determines the index of the first argument that should
        /// be appended to the configured script command, optionally skipping
        /// the command and sub-command names.
        /// </summary>
        /// <param name="arguments">
        /// The list of arguments for the current invocation.  This parameter
        /// may be null.
        /// </param>
        /// <returns>
        /// The starting index of the arguments to append to the script
        /// command.
        /// </returns>
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
        /// <summary>
        /// This method executes the sub-command for a single invocation.  It
        /// validates the arguments, then evaluates the configured script
        /// command (optionally appending the local arguments) inside a freshly
        /// pushed call frame and returns its result.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter context this sub-command is executing in.  This
        /// parameter should not be null.
        /// </param>
        /// <param name="clientData">
        /// The extra, sub-command-specific data supplied for this invocation,
        /// if any.  This parameter may be null.
        /// </param>
        /// <param name="arguments">
        /// The list of arguments for this invocation.  This parameter should
        /// not be null.
        /// </param>
        /// <param name="result">
        /// Upon success, this contains the result of evaluating the configured
        /// script command.  Upon failure, this contains an appropriate error
        /// message.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise, a non-Ok value
        /// with details placed in the <paramref name="result" /> parameter.
        /// </returns>
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
                /* IGNORED */
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
