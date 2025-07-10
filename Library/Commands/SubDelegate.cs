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
    [ObjectId("f1bdac7d-857c-49a1-9bee-0a1dd38545bd")]
    [CommandFlags(
        CommandFlags.NoPopulate | CommandFlags.NoAdd |
        CommandFlags.SubDelegate
    )]
    [ObjectGroup("delegate")]
    public class SubDelegate : _Delegate
    {
        #region Public Constructors
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

        public SubDelegate(
            ICommandData commandData,
            IDelegateData delegateData
            )
            : base(commandData, delegateData)
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
