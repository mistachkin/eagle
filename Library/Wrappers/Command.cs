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
using Eagle._Components.Public;
using Eagle._Components.Public.Delegates;
using Eagle._Containers.Public;
using Eagle._Interfaces.Public;

namespace Eagle._Wrappers
{
    [ObjectId("ff6aa9bc-def1-4ae1-9264-e3d653374767")]
    internal sealed class Command : Default, ICommand
    {
        #region Public Constructors
        public Command(
            long token,
            ICommand command
            )
            : base(token)
        {
            this.command = command;
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Private Data
        internal ICommand command;
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region IIdentifierName Members
        public string Name
        {
            get { return (command != null) ? command.Name : null; }
            set { if (command != null) { command.Name = value; } }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region IIdentifierBase Members
        public IdentifierKind Kind
        {
            get { return (command != null) ? command.Kind : IdentifierKind.None; }
            set { if (command != null) { command.Kind = value; } }
        }

        ///////////////////////////////////////////////////////////////////////

        public Guid Id
        {
            get { return (command != null) ? command.Id : Guid.Empty; }
            set { if (command != null) { command.Id = value; } }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region IIdentifier Members
        public string Group
        {
            get { return (command != null) ? command.Group : null; }
            set { if (command != null) { command.Group = value; } }
        }

        ///////////////////////////////////////////////////////////////////////

        public string Description
        {
            get { return (command != null) ? command.Description : null; }
            set { if (command != null) { command.Description = value; } }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region IGetClientData / ISetClientData Members
        public IClientData ClientData
        {
            get { return (command != null) ? command.ClientData : null; }
            set { if (command != null) { command.ClientData = value; } }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region IState Members
        public bool Initialized
        {
            get { return (command != null) ? command.Initialized : false; }
            set { if (command != null) { command.Initialized = value; } }
        }

        ///////////////////////////////////////////////////////////////////////

        public ReturnCode Initialize(
            Interpreter interpreter,
            IClientData clientData,
            ref Result result
            )
        {
            if (command != null)
                return command.Initialize(interpreter, clientData, ref result);
            else
                return ReturnCode.Error;
        }

        ///////////////////////////////////////////////////////////////////////

        public ReturnCode Terminate(
            Interpreter interpreter,
            IClientData clientData,
            ref Result result
            )
        {
            if (command != null)
                return command.Terminate(interpreter, clientData, ref result);
            else
                return ReturnCode.Error;
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region IDynamicExecuteCallback Members
        public ExecuteCallback Callback
        {
            get { return (command != null) ? command.Callback : null; }
            set { if (command != null) { command.Callback = value; } }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region IEnsemble Members
        public EnsembleDictionary SubCommands
        {
            get { return (command != null) ? command.SubCommands : null; }
            set { if (command != null) { command.SubCommands = value; } }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region IPolicyEnsemble Members
        public EnsembleDictionary AllowedSubCommands
        {
            get { return (command != null) ? command.AllowedSubCommands : null; }
            set { if (command != null) { command.AllowedSubCommands = value; } }
        }

        ///////////////////////////////////////////////////////////////////////

        public EnsembleDictionary DisallowedSubCommands
        {
            get { return (command != null) ? command.DisallowedSubCommands : null; }
            set { if (command != null) { command.DisallowedSubCommands = value; } }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region IExecute Members
        public ReturnCode Execute(
            Interpreter interpreter,
            IClientData clientData,
            ArgumentList arguments,
            ref Result result
            )
        {
            if (command != null)
                return command.Execute(
                    interpreter, clientData, arguments, ref result);
            else
                return ReturnCode.Error;
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region IUsageData Members
        public bool ResetUsage(
            UsageType type,
            ref long value
            )
        {
            return (command != null) ?
                command.ResetUsage(type, ref value) : false;
        }

        ///////////////////////////////////////////////////////////////////////

        public bool GetUsage(
            UsageType type,
            ref long value
            )
        {
            return (command != null) ?
                command.GetUsage(type, ref value) : false;
        }

        ///////////////////////////////////////////////////////////////////////

        public bool SetUsage(
            UsageType type,
            ref long value
            )
        {
            return (command != null) ?
                command.SetUsage(type, ref value) : false;
        }

        ///////////////////////////////////////////////////////////////////////

        public bool AddUsage(
            UsageType type,
            ref long value
            )
        {
            return (command != null) ?
                command.AddUsage(type, ref value) : false;
        }

        ///////////////////////////////////////////////////////////////////////

        public bool CountUsage(
            ref long count
            )
        {
            return (command != null) ?
                command.CountUsage(ref count) : false;
        }

        ///////////////////////////////////////////////////////////////////////

        public bool ProfileUsage(
            ref long microseconds
            )
        {
            return (command != null) ?
                command.ProfileUsage(ref microseconds) : false;
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region ICommandBaseData Members
        public string TypeName
        {
            get { return (command != null) ? command.TypeName : null; }
            set { if (command != null) { command.TypeName = value; } }
        }

        ///////////////////////////////////////////////////////////////////////

        public CommandFlags CommandFlags
        {
            get { return (command != null) ? command.CommandFlags : CommandFlags.None; }
            set { if (command != null) { command.CommandFlags = value; } }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region IHavePlugin Members
        public IPlugin Plugin
        {
            get { return (command != null) ? command.Plugin : null; }
            set { if (command != null) { command.Plugin = value; } }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region ICommandData Members
        public CommandFlags Flags
        {
            get { return (command != null) ? command.Flags : CommandFlags.None; }
            set { if (command != null) { command.Flags = value; } }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region ISyntax Members
        public string Syntax
        {
            get { return (command != null) ? command.Syntax : null; }
            set { if (command != null) { command.Syntax = value; } }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region IWrapper Members
        public override bool IsDisposable
        {
            get { return false; }
        }

        ///////////////////////////////////////////////////////////////////////

        public override object Object
        {
            get { return command; }
        }
        #endregion
    }
}
