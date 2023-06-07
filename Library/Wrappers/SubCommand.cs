/*
 * SubCommand.cs --
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
    [ObjectId("d05e6f6c-1798-4301-bea1-371428f8ae53")]
    internal sealed class SubCommand : Default, ISubCommand
    {
        #region Public Constructors
        public SubCommand(
            long token,
            ISubCommand subCommand
            )
            : base(token)
        {
            this.subCommand = subCommand;
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Private Data
        internal ISubCommand subCommand;
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region IIdentifierName Members
        public string Name
        {
            get { return (subCommand != null) ? subCommand.Name : null; }
            set { if (subCommand != null) { subCommand.Name = value; } }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region IIdentifierBase Members
        public IdentifierKind Kind
        {
            get { return (subCommand != null) ? subCommand.Kind : IdentifierKind.None; }
            set { if (subCommand != null) { subCommand.Kind = value; } }
        }

        ///////////////////////////////////////////////////////////////////////

        public Guid Id
        {
            get { return (subCommand != null) ? subCommand.Id : Guid.Empty; }
            set { if (subCommand != null) { subCommand.Id = value; } }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region IIdentifier Members
        public string Group
        {
            get { return (subCommand != null) ? subCommand.Group : null; }
            set { if (subCommand != null) { subCommand.Group = value; } }
        }

        ///////////////////////////////////////////////////////////////////////

        public string Description
        {
            get { return (subCommand != null) ? subCommand.Description : null; }
            set { if (subCommand != null) { subCommand.Description = value; } }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region IGetClientData / ISetClientData Members
        public IClientData ClientData
        {
            get { return (subCommand != null) ? subCommand.ClientData : null; }
            set { if (subCommand != null) { subCommand.ClientData = value; } }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region IDynamicExecuteCallback Members
        public ExecuteCallback Callback
        {
            get { return (subCommand != null) ? subCommand.Callback : null; }
            set { if (subCommand != null) { subCommand.Callback = value; } }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region IDynamicExecuteDelegate Members
        public System.Delegate Delegate
        {
            get { return (subCommand != null) ? subCommand.Delegate : null; }
            set { if (subCommand != null) { subCommand.Delegate = value; } }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region IDelegateData Members
        public DelegateFlags DelegateFlags
        {
            get { return (subCommand != null) ? subCommand.DelegateFlags : DelegateFlags.None; }
            set { if (subCommand != null) { subCommand.DelegateFlags = value; } }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region IEnsemble Members
        public EnsembleDictionary SubCommands
        {
            get { return (subCommand != null) ? subCommand.SubCommands : null; }
            set { if (subCommand != null) { subCommand.SubCommands = value; } }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region IPolicyEnsemble Members
        public EnsembleDictionary AllowedSubCommands
        {
            get { return (subCommand != null) ? subCommand.AllowedSubCommands : null; }
            set { if (subCommand != null) { subCommand.AllowedSubCommands = value; } }
        }

        ///////////////////////////////////////////////////////////////////////

        public EnsembleDictionary DisallowedSubCommands
        {
            get { return (subCommand != null) ? subCommand.DisallowedSubCommands : null; }
            set { if (subCommand != null) { subCommand.DisallowedSubCommands = value; } }
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
            if (subCommand != null)
                return subCommand.Execute(
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
            return (subCommand != null) ?
                subCommand.ResetUsage(type, ref value) : false;
        }

        ///////////////////////////////////////////////////////////////////////

        public bool GetUsage(
            UsageType type,
            ref long value
            )
        {
            return (subCommand != null) ?
                subCommand.GetUsage(type, ref value) : false;
        }

        ///////////////////////////////////////////////////////////////////////

        public bool SetUsage(
            UsageType type,
            ref long value
            )
        {
            return (subCommand != null) ?
                subCommand.SetUsage(type, ref value) : false;
        }

        ///////////////////////////////////////////////////////////////////////

        public bool AddUsage(
            UsageType type,
            ref long value
            )
        {
            return (subCommand != null) ?
                subCommand.AddUsage(type, ref value) : false;
        }

        ///////////////////////////////////////////////////////////////////////

        public bool CountUsage(
            ref long count
            )
        {
            return (subCommand != null) ?
                subCommand.CountUsage(ref count) : false;
        }

        ///////////////////////////////////////////////////////////////////////

        public bool ProfileUsage(
            ref long microseconds
            )
        {
            return (subCommand != null) ?
                subCommand.ProfileUsage(ref microseconds) : false;
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region ITypeAndName Members
        public string TypeName
        {
            get { return (subCommand != null) ? subCommand.TypeName : null; }
            set { if (subCommand != null) { subCommand.TypeName = value; } }
        }

        ///////////////////////////////////////////////////////////////////////

        public Type Type
        {
            get { return (subCommand != null) ? subCommand.Type : null; }
            set { if (subCommand != null) { subCommand.Type = value; } }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region ICommandBaseData Members
        public CommandFlags CommandFlags
        {
            get { return (subCommand != null) ? subCommand.CommandFlags : CommandFlags.None; }
            set { if (subCommand != null) { subCommand.CommandFlags = value; } }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region IHaveCommand Members
        public ICommand Command
        {
            get { return (subCommand != null) ? subCommand.Command : null; }
            set { if (subCommand != null) { subCommand.Command = value; } }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region ISubCommandData Members
        public int NameIndex
        {
            get { return (subCommand != null) ? subCommand.NameIndex : 0; }
            set { if (subCommand != null) { subCommand.NameIndex = value; } }
        }

        ///////////////////////////////////////////////////////////////////////

        public SubCommandFlags Flags
        {
            get { return (subCommand != null) ? subCommand.Flags : SubCommandFlags.None; }
            set { if (subCommand != null) { subCommand.Flags = value; } }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region ISyntax Members
        public string Syntax
        {
            get { return (subCommand != null) ? subCommand.Syntax : null; }
            set { if (subCommand != null) { subCommand.Syntax = value; } }
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
            get { return subCommand; }
        }
        #endregion
    }
}
