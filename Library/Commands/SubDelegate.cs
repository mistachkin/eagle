/*
 * SubDelegate.cs --
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
    /// <summary>
    /// This class implements an Eagle command that wraps an ensemble whose
    /// sub-commands are each dispatched to a managed delegate.  When invoked,
    /// it resolves the requested sub-command from the ensemble and executes
    /// (or invokes) the delegate associated with that sub-command, optionally
    /// looking up object arguments and honoring call options first.  See
    /// <c>core_language.md</c> for ensemble command syntax and semantics.
    /// </summary>
    [ObjectId("f1bdac7d-857c-49a1-9bee-0a1dd38545bd")]
    [CommandFlags(
        CommandFlags.NoPopulate | CommandFlags.NoAdd |
        CommandFlags.SubDelegate
    )]
    [ObjectGroup("delegate")]
    public class SubDelegate : _Delegate
    {
        #region Public Constructors
        /// <summary>
        /// Constructs an instance of the <c>SubDelegate</c> command, marking
        /// it as an ensemble with per sub-command delegates and (unless
        /// suppressed) applying the command flags from its attributes.
        /// </summary>
        /// <param name="commandData">
        /// The data used to create and identify this command, such as its
        /// name and flags.  This parameter may be null.
        /// </param>
        public SubDelegate(
            ICommandData commandData
            )
            : base(commandData)
        {
            //
            // NOTE: This is not a strictly vanilla "command", it is a
            //       wrapped ensemble with per sub-command delegates.
            //
            this.Kind |= IdentifierKind.Ensemble | IdentifierKind.SubDelegate;

            //
            // NOTE: Normally, this flags assignment is performed by
            //       _Commands.Core for all commands residing in the core
            //       library; however, this class does not inherit from
            //       _Commands.Core.
            //
            if ((commandData == null) || !FlagOps.HasFlags(
                    commandData.Flags, CommandFlags.NoAttributes, true))
            {
                this.Flags |=
                    AttributeOps.GetCommandFlags(GetType().BaseType) |
                    AttributeOps.GetCommandFlags(this);
            }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Constructs an instance of the <c>SubDelegate</c> command using the
        /// supplied command data together with the delegate data that backs
        /// the wrapped ensemble.
        /// </summary>
        /// <param name="commandData">
        /// The data used to create and identify this command, such as its
        /// name and flags.  This parameter may be null.
        /// </param>
        /// <param name="delegateData">
        /// The data describing the delegate(s) used by this command and its
        /// sub-commands.  This parameter may be null.
        /// </param>
        public SubDelegate(
            ICommandData commandData,  /* in */
            IDelegateData delegateData /* in */
            )
            : base(commandData, delegateData)
        {
            // do nothing.
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region IExecute Members
        /// <summary>
        /// This method executes the <c>SubDelegate</c> command.  When a
        /// delegate is configured on the base command itself, it defers to
        /// the base implementation; otherwise it resolves the requested
        /// sub-command from the ensemble and executes (or invokes) the
        /// delegate associated with that sub-command, optionally looking up
        /// object arguments and honoring call options as directed by the
        /// sub-command delegate flags.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter context this command is executing in.  This
        /// parameter should not be null.
        /// </param>
        /// <param name="clientData">
        /// The extra, command-specific data supplied when this command was
        /// created, if any.  This parameter may be null.
        /// </param>
        /// <param name="arguments">
        /// The list of arguments for this invocation.  Element zero is the
        /// command name; element one is the sub-command name to dispatch; the
        /// remaining elements are passed to the sub-command delegate.  This
        /// parameter should not be null.
        /// </param>
        /// <param name="result">
        /// Upon success, this contains the value produced by the sub-command
        /// delegate.  Upon failure, this contains an appropriate error
        /// message.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise,
        /// <see cref="ReturnCode.Error" /> (or another non-Ok value) when the
        /// interpreter is null, the argument list is null, too few arguments
        /// are supplied, the sub-command cannot be resolved, the sub-command
        /// has no delegate, or the delegate fails, with details placed in
        /// <paramref name="result" />.
        /// </returns>
        public override ReturnCode Execute(
            Interpreter interpreter, /* in */
            IClientData clientData,  /* in */
            ArgumentList arguments,  /* in */
            ref Result result        /* out */
            )
        {
            //
            // HACK: When a delegate is configured on the base command
            //       itself, it will override those that may be set on
            //       its sub-commands.  This should happen very rarely,
            //       if ever.
            //
            if (base.ShouldUseDelegate())
            {
                return base.Execute(
                    interpreter, clientData, arguments, ref result);
            }

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

            int argumentCount = arguments.Count;

            if (argumentCount < 2)
            {
                result = String.Format(
                    "wrong # args: should be \"{0} option ?arg ...?\"",
                    this.Name);

                return ReturnCode.Error;
            }

            string subCommandName = arguments[1];
            ISubCommand subCommand = null;

            if (ScriptOps.SubCommandFromEnsemble(interpreter,
                    this, null, false, false, ref subCommandName,
                    ref subCommand, ref result) != ReturnCode.Ok)
            {
                return ReturnCode.Error;
            }

            if (subCommand == null)
            {
                result = ScriptOps.BadSubCommand(
                    interpreter, null, null, subCommandName, this,
                    null, null);

                return ReturnCode.Error;
            }

            Delegate @delegate = subCommand.Delegate;

            if (@delegate == null)
            {
                result = "invalid sub-command delegate";
                return ReturnCode.Error;
            }

            DelegateFlags delegateFlags = subCommand.DelegateFlags;
            ArgumentList newArguments;

            if (FlagOps.HasFlags(delegateFlags,
                    DelegateFlags.LookupObjects, true))
            {
                ScriptOps.LookupObjectsInArguments(
                    interpreter, arguments, out newArguments);
            }
            else
            {
                newArguments = arguments;
            }

            bool allowOptions = FlagOps.HasFlags(
                delegateFlags, DelegateFlags.UseCallOptions, true);

            ReturnCode code;
            Result returnValue = null;

            code = ScriptOps.ExecuteOrInvokeDelegate(
                interpreter, @delegate, newArguments,
                allowOptions, 2 /* cmd subCmd ... */,
                2, delegateFlags, ref returnValue);

            if (code != ReturnCode.Ok)
            {
                result = returnValue;
                return code;
            }

            return ScriptOps.HandleDelegateResult(
                interpreter, @delegate, delegateFlags, returnValue,
                ref result);
        }
        #endregion
    }
}
