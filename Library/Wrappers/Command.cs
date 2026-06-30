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
    /// <summary>
    /// This class implements a wrapper around an <see cref="ICommand" />
    /// object.  It forwards every member of the interface to the wrapped
    /// command when one is present, gracefully returning default values when
    /// one is not.
    /// </summary>
    [ObjectId("ff6aa9bc-def1-4ae1-9264-e3d653374767")]
    internal sealed class Command : Core, ICommand
    {
        #region Public Constructors
        /// <summary>
        /// Constructs an instance of this class with no wrapped command.
        /// </summary>
        public Command()
            : base()
        {
            // do nothing.
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Private Data
        /// <summary>
        /// The wrapped command that this wrapper forwards to.
        /// </summary>
        internal ICommand command;
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region IIdentifierName Members
        /// <summary>
        /// Gets or sets the name of the wrapped command.
        /// </summary>
        public string Name
        {
            get { return (command != null) ? command.Name : null; }
            set { if (command != null) { command.Name = value; } }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region IIdentifierBase Members
        /// <summary>
        /// Gets or sets the identifier kind of the wrapped command.
        /// </summary>
        public IdentifierKind Kind
        {
            get { return (command != null) ? command.Kind : IdentifierKind.None; }
            set { if (command != null) { command.Kind = value; } }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Gets or sets the unique identifier of the wrapped command.
        /// </summary>
        public Guid Id
        {
            get { return (command != null) ? command.Id : Guid.Empty; }
            set { if (command != null) { command.Id = value; } }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region IGetClientData / ISetClientData Members
        /// <summary>
        /// Gets or sets the client data associated with the wrapped command.
        /// </summary>
        public IClientData ClientData
        {
            get { return (command != null) ? command.ClientData : null; }
            set { if (command != null) { command.ClientData = value; } }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region IIdentifier Members
        /// <summary>
        /// Gets or sets the group of the wrapped command.
        /// </summary>
        public string Group
        {
            get { return (command != null) ? command.Group : null; }
            set { if (command != null) { command.Group = value; } }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Gets or sets the description of the wrapped command.
        /// </summary>
        public string Description
        {
            get { return (command != null) ? command.Description : null; }
            set { if (command != null) { command.Description = value; } }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region IState Members
        /// <summary>
        /// Gets or sets a value indicating whether the wrapped command has
        /// been initialized.
        /// </summary>
        public bool Initialized
        {
            get { return (command != null) ? command.Initialized : false; }
            set { if (command != null) { command.Initialized = value; } }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Forwards initialization to the wrapped command, allowing it to set
        /// up any state it requires.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter context the wrapped command is being initialized
        /// in.
        /// </param>
        /// <param name="clientData">
        /// The extra, command-specific data supplied for initialization, if
        /// any.  This parameter may be null.
        /// </param>
        /// <param name="result">
        /// Upon success, this may contain an informational result.  Upon
        /// failure, this will contain an appropriate error message.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise, a non-Ok value
        /// with details placed in the <paramref name="result" /> parameter.
        /// If there is no wrapped command, <see cref="ReturnCode.Error" /> is
        /// returned.
        /// </returns>
        public ReturnCode Initialize(
            Interpreter interpreter,
            IClientData clientData,
            ref Result result
            )
        {
            if (command == null)
            {
                result = "invalid wrapper target";
                return ReturnCode.Error;
            }

            return command.Initialize(interpreter, clientData, ref result);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Forwards termination to the wrapped command, allowing it to tear
        /// down any state it established during initialization.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter context the wrapped command is being terminated in.
        /// </param>
        /// <param name="clientData">
        /// The extra, command-specific data supplied for termination, if any.
        /// This parameter may be null.
        /// </param>
        /// <param name="result">
        /// Upon success, this may contain an informational result.  Upon
        /// failure, this will contain an appropriate error message.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise, a non-Ok value
        /// with details placed in the <paramref name="result" /> parameter.
        /// If there is no wrapped command, <see cref="ReturnCode.Error" /> is
        /// returned.
        /// </returns>
        public ReturnCode Terminate(
            Interpreter interpreter,
            IClientData clientData,
            ref Result result
            )
        {
            if (command == null)
            {
                result = "invalid wrapper target";
                return ReturnCode.Error;
            }

            return command.Terminate(interpreter, clientData, ref result);
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region IDynamicExecuteCallback Members
        /// <summary>
        /// Gets or sets the dynamic execution callback of the wrapped command.
        /// </summary>
        public ExecuteCallback Callback
        {
            get { return (command != null) ? command.Callback : null; }
            set { if (command != null) { command.Callback = value; } }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region IEnsemble Members
        /// <summary>
        /// Gets or sets the dictionary of sub-commands of the wrapped command.
        /// </summary>
        public EnsembleDictionary SubCommands
        {
            get { return (command != null) ? command.SubCommands : null; }
            set { if (command != null) { command.SubCommands = value; } }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region IPolicyEnsemble Members
        /// <summary>
        /// Gets or sets the dictionary of explicitly allowed sub-commands of
        /// the wrapped command.
        /// </summary>
        public EnsembleDictionary AllowedSubCommands
        {
            get { return (command != null) ? command.AllowedSubCommands : null; }
            set { if (command != null) { command.AllowedSubCommands = value; } }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Gets or sets the dictionary of explicitly disallowed sub-commands
        /// of the wrapped command.
        /// </summary>
        public EnsembleDictionary DisallowedSubCommands
        {
            get { return (command != null) ? command.DisallowedSubCommands : null; }
            set { if (command != null) { command.DisallowedSubCommands = value; } }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region IUsageData Members
        /// <summary>
        /// Forwards a request to reset the specified usage statistic of the
        /// wrapped command.
        /// </summary>
        /// <param name="type">
        /// The type of usage statistic to reset.
        /// </param>
        /// <param name="value">
        /// Upon success, this receives the previous value of the usage
        /// statistic.  Upon failure, this is not modified.
        /// </param>
        /// <returns>
        /// True if the usage statistic was reset; otherwise, false.
        /// </returns>
        public bool ResetUsage(
            UsageType type,
            ref long value
            )
        {
            return (command != null) ?
                command.ResetUsage(type, ref value) : false;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Forwards a request to query the specified usage statistic of the
        /// wrapped command.
        /// </summary>
        /// <param name="type">
        /// The type of usage statistic to query.
        /// </param>
        /// <param name="value">
        /// Upon success, this receives the current value of the usage
        /// statistic.  Upon failure, this is not modified.
        /// </param>
        /// <returns>
        /// True if the usage statistic was queried; otherwise, false.
        /// </returns>
        public bool GetUsage(
            UsageType type,
            ref long value
            )
        {
            return (command != null) ?
                command.GetUsage(type, ref value) : false;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Forwards a request to set the specified usage statistic of the
        /// wrapped command.
        /// </summary>
        /// <param name="type">
        /// The type of usage statistic to set.
        /// </param>
        /// <param name="value">
        /// Upon entry, this contains the new value for the usage statistic.
        /// Upon success, this receives the previous value of the usage
        /// statistic.  Upon failure, this is not modified.
        /// </param>
        /// <returns>
        /// True if the usage statistic was set; otherwise, false.
        /// </returns>
        public bool SetUsage(
            UsageType type,
            ref long value
            )
        {
            return (command != null) ?
                command.SetUsage(type, ref value) : false;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Forwards a request to add to the specified usage statistic of the
        /// wrapped command.
        /// </summary>
        /// <param name="type">
        /// The type of usage statistic to add to.
        /// </param>
        /// <param name="value">
        /// Upon entry, this contains the amount to add to the usage statistic.
        /// Upon success, this receives the resulting value of the usage
        /// statistic.  Upon failure, this is not modified.
        /// </param>
        /// <returns>
        /// True if the usage statistic was modified; otherwise, false.
        /// </returns>
        public bool AddUsage(
            UsageType type,
            ref long value
            )
        {
            return (command != null) ?
                command.AddUsage(type, ref value) : false;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Forwards a request to query the invocation count of the wrapped
        /// command.
        /// </summary>
        /// <param name="count">
        /// Upon success, this receives the number of times the wrapped command
        /// has been invoked.  Upon failure, this is not modified.
        /// </param>
        /// <returns>
        /// True if the count was queried; otherwise, false.
        /// </returns>
        public bool CountUsage(
            ref long count
            )
        {
            return (command != null) ?
                command.CountUsage(ref count) : false;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Forwards a request to query the accumulated execution time of the
        /// wrapped command.
        /// </summary>
        /// <param name="microseconds">
        /// Upon success, this receives the total time, in microseconds, spent
        /// executing the wrapped command.  Upon failure, this is not modified.
        /// </param>
        /// <returns>
        /// True if the profiling data was queried; otherwise, false.
        /// </returns>
        public bool ProfileUsage(
            ref long microseconds
            )
        {
            return (command != null) ?
                command.ProfileUsage(ref microseconds) : false;
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region ITypeAndName Members
        /// <summary>
        /// Gets or sets the type name of the wrapped command.
        /// </summary>
        public string TypeName
        {
            get { return (command != null) ? command.TypeName : null; }
            set { if (command != null) { command.TypeName = value; } }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Gets or sets the type of the wrapped command.
        /// </summary>
        public Type Type
        {
            get { return (command != null) ? command.Type : null; }
            set { if (command != null) { command.Type = value; } }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region ICommandBaseData Members
        /// <summary>
        /// Gets or sets the base flags of the wrapped command.
        /// </summary>
        public CommandFlags CommandFlags
        {
            get { return (command != null) ? command.CommandFlags : CommandFlags.None; }
            set { if (command != null) { command.CommandFlags = value; } }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region IHavePlugin Members
        /// <summary>
        /// Gets or sets the plugin that owns the wrapped command.
        /// </summary>
        public IPlugin Plugin
        {
            get { return (command != null) ? command.Plugin : null; }
            set { if (command != null) { command.Plugin = value; } }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region ICommandData Members
        /// <summary>
        /// Gets or sets the flags of the wrapped command.
        /// </summary>
        public CommandFlags Flags
        {
            get { return (command != null) ? command.Flags : CommandFlags.None; }
            set { if (command != null) { command.Flags = value; } }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region ISyntax Members
        /// <summary>
        /// Gets or sets the syntax help string of the wrapped command.
        /// </summary>
        public string Syntax
        {
            get { return (command != null) ? command.Syntax : null; }
            set { if (command != null) { command.Syntax = value; } }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region IWrapper Members
        /// <summary>
        /// Gets a value indicating whether the wrapped object represents a
        /// disposable resource.  This wrapper never owns a disposable
        /// resource, so this property always returns false.
        /// </summary>
        public override bool IsDisposable
        {
            get { return false; }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Gets or sets the wrapped object.  The value being set must be an
        /// <see cref="ICommand" /> instance, which is also used as the
        /// underlying <see cref="IExecute" /> entity.
        /// </summary>
        public override object Object
        {
            get { return command; }
            set
            {
                command = (ICommand)value; /* throw */
                execute = (IExecute)value; /* throw */
            }
        }
        #endregion
    }
}
