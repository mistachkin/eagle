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
    /// <summary>
    /// This class wraps an <see cref="ISubCommand" /> instance, forwarding all
    /// member access to the wrapped target while gracefully handling the case
    /// where no target has been set (by returning default values or an error
    /// result).
    /// </summary>
    [ObjectId("d05e6f6c-1798-4301-bea1-371428f8ae53")]
    internal sealed class SubCommand : Core, ISubCommand
    {
        #region Public Constructors
        /// <summary>
        /// Constructs an instance of this class with no wrapped sub-command
        /// target.
        /// </summary>
        public SubCommand()
            : base()
        {
            // do nothing.
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Private Data
        /// <summary>
        /// The wrapped sub-command instance that all members forward to.
        /// </summary>
        internal ISubCommand subCommand;
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region IIdentifierName Members
        /// <summary>
        /// Gets or sets the name of the wrapped sub-command.
        /// </summary>
        public string Name
        {
            get { return (subCommand != null) ? subCommand.Name : null; }
            set { if (subCommand != null) { subCommand.Name = value; } }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region IIdentifierBase Members
        /// <summary>
        /// Gets or sets the identifier kind of the wrapped sub-command.
        /// </summary>
        public IdentifierKind Kind
        {
            get { return (subCommand != null) ? subCommand.Kind : IdentifierKind.None; }
            set { if (subCommand != null) { subCommand.Kind = value; } }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Gets or sets the unique identifier of the wrapped sub-command.
        /// </summary>
        public Guid Id
        {
            get { return (subCommand != null) ? subCommand.Id : Guid.Empty; }
            set { if (subCommand != null) { subCommand.Id = value; } }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region IGetClientData / ISetClientData Members
        /// <summary>
        /// Gets or sets the client data associated with the wrapped
        /// sub-command.
        /// </summary>
        public IClientData ClientData
        {
            get { return (subCommand != null) ? subCommand.ClientData : null; }
            set { if (subCommand != null) { subCommand.ClientData = value; } }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region IIdentifier Members
        /// <summary>
        /// Gets or sets the group of the wrapped sub-command.
        /// </summary>
        public string Group
        {
            get { return (subCommand != null) ? subCommand.Group : null; }
            set { if (subCommand != null) { subCommand.Group = value; } }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Gets or sets the description of the wrapped sub-command.
        /// </summary>
        public string Description
        {
            get { return (subCommand != null) ? subCommand.Description : null; }
            set { if (subCommand != null) { subCommand.Description = value; } }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region IDynamicExecuteCallback Members
        /// <summary>
        /// Gets or sets the execute callback of the wrapped sub-command.
        /// </summary>
        public ExecuteCallback Callback
        {
            get { return (subCommand != null) ? subCommand.Callback : null; }
            set { if (subCommand != null) { subCommand.Callback = value; } }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region IDynamicExecuteDelegate Members
        /// <summary>
        /// Gets or sets the delegate invoked by the wrapped sub-command.
        /// </summary>
        public System.Delegate Delegate
        {
            get { return (subCommand != null) ? subCommand.Delegate : null; }
            set { if (subCommand != null) { subCommand.Delegate = value; } }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region IDelegateData Members
        /// <summary>
        /// Gets or sets the delegate flags of the wrapped sub-command.
        /// </summary>
        public DelegateFlags DelegateFlags
        {
            get { return (subCommand != null) ? subCommand.DelegateFlags : DelegateFlags.None; }
            set { if (subCommand != null) { subCommand.DelegateFlags = value; } }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region IEnsemble Members
        /// <summary>
        /// Gets or sets the dictionary of sub-commands belonging to the wrapped
        /// sub-command.
        /// </summary>
        public EnsembleDictionary SubCommands
        {
            get { return (subCommand != null) ? subCommand.SubCommands : null; }
            set { if (subCommand != null) { subCommand.SubCommands = value; } }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region IPolicyEnsemble Members
        /// <summary>
        /// Gets or sets the dictionary of sub-commands explicitly allowed by
        /// the wrapped sub-command.
        /// </summary>
        public EnsembleDictionary AllowedSubCommands
        {
            get { return (subCommand != null) ? subCommand.AllowedSubCommands : null; }
            set { if (subCommand != null) { subCommand.AllowedSubCommands = value; } }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Gets or sets the dictionary of sub-commands explicitly disallowed by
        /// the wrapped sub-command.
        /// </summary>
        public EnsembleDictionary DisallowedSubCommands
        {
            get { return (subCommand != null) ? subCommand.DisallowedSubCommands : null; }
            set { if (subCommand != null) { subCommand.DisallowedSubCommands = value; } }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region IUsageData Members
        /// <summary>
        /// This method resets a usage statistic of the wrapped sub-command.
        /// </summary>
        /// <param name="type">
        /// The kind of usage statistic to reset.
        /// </param>
        /// <param name="value">
        /// Upon success, receives the previous value of the usage statistic.
        /// </param>
        /// <returns>
        /// True if the usage statistic was reset; otherwise, false.
        /// </returns>
        public bool ResetUsage(
            UsageType type,
            ref long value
            )
        {
            return (subCommand != null) ?
                subCommand.ResetUsage(type, ref value) : false;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method gets a usage statistic of the wrapped sub-command.
        /// </summary>
        /// <param name="type">
        /// The kind of usage statistic to get.
        /// </param>
        /// <param name="value">
        /// Upon success, receives the current value of the usage statistic.
        /// </param>
        /// <returns>
        /// True if the usage statistic was retrieved; otherwise, false.
        /// </returns>
        public bool GetUsage(
            UsageType type,
            ref long value
            )
        {
            return (subCommand != null) ?
                subCommand.GetUsage(type, ref value) : false;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method sets a usage statistic of the wrapped sub-command.
        /// </summary>
        /// <param name="type">
        /// The kind of usage statistic to set.
        /// </param>
        /// <param name="value">
        /// The new value for the usage statistic.
        /// </param>
        /// <returns>
        /// True if the usage statistic was set; otherwise, false.
        /// </returns>
        public bool SetUsage(
            UsageType type,
            ref long value
            )
        {
            return (subCommand != null) ?
                subCommand.SetUsage(type, ref value) : false;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method adds to a usage statistic of the wrapped sub-command.
        /// </summary>
        /// <param name="type">
        /// The kind of usage statistic to add to.
        /// </param>
        /// <param name="value">
        /// On input, the amount to add; upon success, receives the resulting
        /// value of the usage statistic.
        /// </param>
        /// <returns>
        /// True if the usage statistic was updated; otherwise, false.
        /// </returns>
        public bool AddUsage(
            UsageType type,
            ref long value
            )
        {
            return (subCommand != null) ?
                subCommand.AddUsage(type, ref value) : false;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method gets the usage count of the wrapped sub-command.
        /// </summary>
        /// <param name="count">
        /// Upon success, receives the usage count.
        /// </param>
        /// <returns>
        /// True if the usage count was retrieved; otherwise, false.
        /// </returns>
        public bool CountUsage(
            ref long count
            )
        {
            return (subCommand != null) ?
                subCommand.CountUsage(ref count) : false;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method gets the profiled usage time of the wrapped sub-command.
        /// </summary>
        /// <param name="microseconds">
        /// Upon success, receives the profiled usage time, in microseconds.
        /// </param>
        /// <returns>
        /// True if the profiled usage time was retrieved; otherwise, false.
        /// </returns>
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
        /// <summary>
        /// Gets or sets the type name of the wrapped sub-command.
        /// </summary>
        public string TypeName
        {
            get { return (subCommand != null) ? subCommand.TypeName : null; }
            set { if (subCommand != null) { subCommand.TypeName = value; } }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Gets or sets the type of the wrapped sub-command.
        /// </summary>
        public Type Type
        {
            get { return (subCommand != null) ? subCommand.Type : null; }
            set { if (subCommand != null) { subCommand.Type = value; } }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region ICommandBaseData Members
        /// <summary>
        /// Gets or sets the command flags of the wrapped sub-command.
        /// </summary>
        public CommandFlags CommandFlags
        {
            get { return (subCommand != null) ? subCommand.CommandFlags : CommandFlags.None; }
            set { if (subCommand != null) { subCommand.CommandFlags = value; } }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region IHaveCommand Members
        /// <summary>
        /// Gets or sets the command that owns the wrapped sub-command.
        /// </summary>
        public ICommand Command
        {
            get { return (subCommand != null) ? subCommand.Command : null; }
            set { if (subCommand != null) { subCommand.Command = value; } }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region ISubCommandData Members
        /// <summary>
        /// Gets or sets the index, within the argument list, of the name of the
        /// wrapped sub-command.
        /// </summary>
        public int NameIndex
        {
            get { return (subCommand != null) ? subCommand.NameIndex : 0; }
            set { if (subCommand != null) { subCommand.NameIndex = value; } }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Gets or sets the flags associated with the wrapped sub-command.
        /// </summary>
        public SubCommandFlags Flags
        {
            get { return (subCommand != null) ? subCommand.Flags : SubCommandFlags.None; }
            set { if (subCommand != null) { subCommand.Flags = value; } }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region ISyntax Members
        /// <summary>
        /// Gets or sets the syntax help text of the wrapped sub-command.
        /// </summary>
        public string Syntax
        {
            get { return (subCommand != null) ? subCommand.Syntax : null; }
            set { if (subCommand != null) { subCommand.Syntax = value; } }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region IWrapper Members
        /// <summary>
        /// Gets a value indicating whether the wrapped object is disposable;
        /// always false for this wrapper.
        /// </summary>
        public override bool IsDisposable
        {
            get { return false; }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Gets or sets the wrapped object.  The value must implement both
        /// <see cref="ISubCommand" /> and <see cref="IExecute" />; otherwise,
        /// setting it throws.
        /// </summary>
        public override object Object
        {
            get { return subCommand; }
            set
            {
                subCommand = (ISubCommand)value; /* throw */
                execute = (IExecute)value; /* throw */
            }
        }
        #endregion
    }
}
