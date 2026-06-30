/*
 * Default.cs --
 *
 * Copyright (c) 2007-2012 by Joe Mistachkin.  All rights reserved.
 *
 * See the file "license.terms" for information on usage and redistribution of
 * this file, and for a DISCLAIMER OF ALL WARRANTIES.
 *
 * RCS: @(#) $Id: $
 */

using System;
using System.Threading;
using Eagle._Attributes;
using Eagle._Components.Private;
using Eagle._Components.Public;
using Eagle._Components.Public.Delegates;
using Eagle._Containers.Public;
using Eagle._Interfaces.Public;

namespace Eagle._Commands
{
    /// <summary>
    /// This class provides the default, fully-featured base implementation of
    /// the <see cref="ICommand" /> interface.  Its <see cref="Execute" />
    /// method does nothing and simply returns <see cref="ReturnCode.Ok" />;
    /// concrete commands typically derive from a more specialized base (such
    /// as <c>Core</c>) and override the behavior they need.  This class also
    /// supplies the standard identifier, client data, ensemble, usage, and
    /// command metadata accessors.  See <c>core_language.md</c> for how the
    /// engine creates and invokes commands.
    /// </summary>
    [ObjectId("f8fbc42f-5b6d-4a34-be0a-c328220eb40a")]
    [ObjectGroup("default")]
    public class Default :
#if ISOLATED_INTERPRETERS || ISOLATED_PLUGINS
        ScriptMarshalByRefObject,
#endif
        ICommand,
        IHaveNoCase
    {
        #region Public Constructors
        /// <summary>
        /// Constructs an instance of the default command, copying its identity
        /// and metadata from the supplied command data.
        /// </summary>
        /// <param name="commandData">
        /// The data used to create and identify this command, such as its
        /// name, description, flags, plugin, client data, and token.  This
        /// parameter may be null.
        /// </param>
        public Default(
            ICommandData commandData
            )
        {
            kind = IdentifierKind.Command;

            if ((commandData == null) ||
                !FlagOps.HasFlags(commandData.Flags,
                    CommandFlags.NoAttributes, true))
            {
                id = AttributeOps.GetObjectId(this);
                group = AttributeOps.GetObjectGroups(this);
            }

            if (commandData != null)
            {
                EntityOps.MaybeSetGroup(
                    this, commandData.Group);

                name = commandData.Name;
                description = commandData.Description;
                flags = commandData.Flags;
                plugin = commandData.Plugin;
                clientData = commandData.ClientData;
                token = commandData.Token;
            }

            callback = null;
            subCommands = null;
            syntax = null;

            ///////////////////////////////////////////////////////////////////

            noCase = FlagOps.HasFlags(flags, CommandFlags.NoCase, true);
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region System.Object Overrides
        /// <summary>
        /// This method returns a string representation of this command,
        /// consisting of its raw type name and its name (when one is set);
        /// otherwise, it falls back to the base implementation.
        /// </summary>
        /// <returns>
        /// A string that represents this command.
        /// </returns>
        public override string ToString()
        {
            return (name != null) ?
                StringList.MakeList(FormatOps.RawTypeName(GetType()), name) :
                base.ToString();
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region IIdentifierName Members
        /// <summary>
        /// The name of this command, as used to invoke it.
        /// </summary>
        private string name;
        /// <summary>
        /// Gets or sets the name of this command, as used to invoke it.
        /// </summary>
        public virtual string Name
        {
            get { return name; }
            set { name = value; }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region IIdentifierBase Members
        /// <summary>
        /// The kind of identifier represented by this command (e.g.
        /// <see cref="IdentifierKind.Command" />).
        /// </summary>
        private IdentifierKind kind;
        /// <summary>
        /// Gets or sets the kind of identifier represented by this command.
        /// </summary>
        public virtual IdentifierKind Kind
        {
            get { return kind; }
            set { kind = value; }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// The unique identifier (object identifier) for this command.
        /// </summary>
        private Guid id;
        /// <summary>
        /// Gets or sets the unique identifier (object identifier) for this
        /// command.
        /// </summary>
        public virtual Guid Id
        {
            get { return id; }
            set { id = value; }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region IGetClientData / ISetClientData Members
        /// <summary>
        /// The extra, command-specific data associated with this command, if
        /// any.
        /// </summary>
        private IClientData clientData;
        /// <summary>
        /// Gets or sets the extra, command-specific data associated with this
        /// command, if any.
        /// </summary>
        public virtual IClientData ClientData
        {
            get { return clientData; }
            set { clientData = value; }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region IIdentifier Members
        /// <summary>
        /// The object group that this command belongs to.
        /// </summary>
        private string group;
        /// <summary>
        /// Gets or sets the object group that this command belongs to.
        /// </summary>
        public virtual string Group
        {
            get { return group; }
            set { group = value; }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// The human-readable description of this command.
        /// </summary>
        private string description;
        /// <summary>
        /// Gets or sets the human-readable description of this command.
        /// </summary>
        public virtual string Description
        {
            get { return description; }
            set { description = value; }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region IState Members
        /// <summary>
        /// The net number of times this command has been initialized minus the
        /// number of times it has been terminated; a positive value indicates
        /// the command is currently initialized.
        /// </summary>
        private int initializeCount;
        /// <summary>
        /// Gets or sets a value indicating whether this command is currently
        /// initialized.  Setting this to true increments the initialization
        /// count; setting it to false decrements it.
        /// </summary>
        public virtual bool Initialized
        {
            get
            {
                return Interlocked.CompareExchange(
                    ref initializeCount, 0, 0) > 0;
            }
            set
            {
                if (value)
                    Interlocked.Increment(ref initializeCount);
                else
                    Interlocked.Decrement(ref initializeCount);
            }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method initializes this command for use within the specified
        /// interpreter, incrementing its initialization count.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter context this command is being initialized for.
        /// This parameter should not be null.
        /// </param>
        /// <param name="clientData">
        /// The extra, command-specific data supplied for initialization, if
        /// any.  This parameter may be null.
        /// </param>
        /// <param name="result">
        /// Upon success, this may contain an optional result; upon failure,
        /// this contains an appropriate error message.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise, a non-Ok value
        /// with details placed in <paramref name="result" />.
        /// </returns>
        public virtual ReturnCode Initialize(
            Interpreter interpreter, /* in */
            IClientData clientData,  /* in */
            ref Result result        /* out */
            )
        {
            Interlocked.Increment(ref initializeCount);
            return ReturnCode.Ok;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method terminates this command for the specified interpreter,
        /// decrementing its initialization count.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter context this command is being terminated for.  This
        /// parameter should not be null.
        /// </param>
        /// <param name="clientData">
        /// The extra, command-specific data supplied for termination, if any.
        /// This parameter may be null.
        /// </param>
        /// <param name="result">
        /// Upon success, this may contain an optional result; upon failure,
        /// this contains an appropriate error message.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise, a non-Ok value
        /// with details placed in <paramref name="result" />.
        /// </returns>
        public virtual ReturnCode Terminate(
            Interpreter interpreter, /* in */
            IClientData clientData,  /* in */
            ref Result result        /* out */
            )
        {
            Interlocked.Decrement(ref initializeCount);
            return ReturnCode.Ok;
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region IDynamicExecuteCallback Members
        /// <summary>
        /// The optional delegate that, when set, supplies the dynamic
        /// execution behavior for this command.
        /// </summary>
        private ExecuteCallback callback;
        /// <summary>
        /// Gets or sets the optional delegate that supplies the dynamic
        /// execution behavior for this command.
        /// </summary>
        public virtual ExecuteCallback Callback
        {
            get { return callback; }
            set { callback = value; }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region IHaveNoCase Members
        /// <summary>
        /// Non-zero if this command (and its sub-commands) should be matched
        /// in a case-insensitive manner.
        /// </summary>
        private bool noCase;
        /// <summary>
        /// Gets or sets a value indicating whether this command (and its
        /// sub-commands) should be matched in a case-insensitive manner.
        /// </summary>
        public virtual bool NoCase
        {
            get { return noCase; }
            set { noCase = value; }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region IEnsemble Members
        /// <summary>
        /// The dictionary of sub-commands belonging to this command when it is
        /// used as an ensemble, if any.
        /// </summary>
        private EnsembleDictionary subCommands;
        /// <summary>
        /// Gets or sets the dictionary of sub-commands belonging to this
        /// command when it is used as an ensemble, if any.
        /// </summary>
        public virtual EnsembleDictionary SubCommands
        {
            get { return subCommands; }
            set { subCommands = value; }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region IPolicyEnsemble Members
        /// <summary>
        /// The dictionary of sub-commands explicitly permitted by policy, if
        /// any.
        /// </summary>
        private EnsembleDictionary allowedSubCommands;
        /// <summary>
        /// Gets or sets the dictionary of sub-commands explicitly permitted by
        /// policy, if any.
        /// </summary>
        public virtual EnsembleDictionary AllowedSubCommands
        {
            get { return allowedSubCommands; }
            set { allowedSubCommands = value; }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// The dictionary of sub-commands explicitly forbidden by policy, if
        /// any.
        /// </summary>
        private EnsembleDictionary disallowedSubCommands;
        /// <summary>
        /// Gets or sets the dictionary of sub-commands explicitly forbidden by
        /// policy, if any.
        /// </summary>
        public virtual EnsembleDictionary DisallowedSubCommands
        {
            get { return disallowedSubCommands; }
            set { disallowedSubCommands = value; }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region IExecute Members
        /// <summary>
        /// This method executes the default command.  The default
        /// implementation performs no action and simply returns
        /// <see cref="ReturnCode.Ok" />; derived commands override this method
        /// to provide their actual behavior.
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
        /// command name; the remaining elements are its arguments.  This
        /// parameter should not be null.
        /// </param>
        /// <param name="result">
        /// Upon success, this may contain the result value produced by the
        /// command.  Upon failure, this contains an appropriate error message.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise, a non-Ok value
        /// with details placed in <paramref name="result" />.
        /// </returns>
        public virtual ReturnCode Execute(
            Interpreter interpreter, /* in */
            IClientData clientData,  /* in */
            ArgumentList arguments,  /* in */
            ref Result result        /* out */
            )
        {
            return ReturnCode.Ok;
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region IUsageData Members
        /// <summary>
        /// The number of times this command has been executed.
        /// </summary>
        private long usageCount;
        /// <summary>
        /// The total number of microseconds spent executing this command.
        /// </summary>
        private long usageMicroseconds;

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method resets a usage statistic of the specified type to zero,
        /// returning its previous value.
        /// </summary>
        /// <param name="type">
        /// The kind of usage statistic to reset (e.g.
        /// <see cref="UsageType.Count" /> or
        /// <see cref="UsageType.Microseconds" />).
        /// </param>
        /// <param name="value">
        /// Upon return, this contains the previous value of the requested
        /// usage statistic.
        /// </param>
        /// <returns>
        /// Non-zero if the specified usage type was recognized and reset;
        /// otherwise, zero.
        /// </returns>
        public virtual bool ResetUsage(
            UsageType type, /* in */
            ref long value  /* in, out */
            )
        {
            switch (type)
            {
                case UsageType.Count:
                    {
                        value = Interlocked.Exchange(
                            ref usageCount, 0);

                        return true;
                    }
                case UsageType.Microseconds:
                    {
                        value = Interlocked.Exchange(
                            ref usageMicroseconds, 0);

                        return true;
                    }
                default:
                    {
                        return false;
                    }
            }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method retrieves the current value of a usage statistic of the
        /// specified type.
        /// </summary>
        /// <param name="type">
        /// The kind of usage statistic to retrieve (e.g.
        /// <see cref="UsageType.Count" /> or
        /// <see cref="UsageType.Microseconds" />).
        /// </param>
        /// <param name="value">
        /// Upon return, this contains the current value of the requested usage
        /// statistic.
        /// </param>
        /// <returns>
        /// Non-zero if the specified usage type was recognized and retrieved;
        /// otherwise, zero.
        /// </returns>
        public virtual bool GetUsage(
            UsageType type, /* in */
            ref long value  /* in, out */
            )
        {
            switch (type)
            {
                case UsageType.Count:
                    {
                        value = Interlocked.CompareExchange(
                            ref usageCount, 0, 0);

                        return true;
                    }
                case UsageType.Microseconds:
                    {
                        value = Interlocked.CompareExchange(
                            ref usageMicroseconds, 0, 0);

                        return true;
                    }
                default:
                    {
                        return false;
                    }
            }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method sets a usage statistic of the specified type to the
        /// supplied value, returning the value that was set.
        /// </summary>
        /// <param name="type">
        /// The kind of usage statistic to set (e.g.
        /// <see cref="UsageType.Count" /> or
        /// <see cref="UsageType.Microseconds" />).
        /// </param>
        /// <param name="value">
        /// On input, the new value for the requested usage statistic; upon
        /// return, the value that was set.
        /// </param>
        /// <returns>
        /// Non-zero if the specified usage type was recognized and set;
        /// otherwise, zero.
        /// </returns>
        public virtual bool SetUsage(
            UsageType type, /* in */
            ref long value  /* in, out */
            )
        {
            switch (type)
            {
                case UsageType.Count:
                    {
                        value = Interlocked.Exchange(
                            ref usageCount, value);

                        return true;
                    }
                case UsageType.Microseconds:
                    {
                        value = Interlocked.Exchange(
                            ref usageMicroseconds, value);

                        return true;
                    }
                default:
                    {
                        return false;
                    }
            }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method adds the supplied value to a usage statistic of the
        /// specified type, returning the resulting total.
        /// </summary>
        /// <param name="type">
        /// The kind of usage statistic to add to (e.g.
        /// <see cref="UsageType.Count" /> or
        /// <see cref="UsageType.Microseconds" />).
        /// </param>
        /// <param name="value">
        /// On input, the amount to add to the requested usage statistic; upon
        /// return, the resulting total.
        /// </param>
        /// <returns>
        /// Non-zero if the specified usage type was recognized and updated;
        /// otherwise, zero.
        /// </returns>
        public virtual bool AddUsage(
            UsageType type, /* in */
            ref long value  /* in, out */
            )
        {
            switch (type)
            {
                case UsageType.Count:
                    {
                        value = Interlocked.Add(
                            ref usageCount, value);

                        return true;
                    }
                case UsageType.Microseconds:
                    {
                        value = Interlocked.Add(
                            ref usageMicroseconds, value);

                        return true;
                    }
                default:
                    {
                        return false;
                    }
            }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method increments the execution count for this command and
        /// reports the new count.
        /// </summary>
        /// <param name="count">
        /// Upon return, this contains the updated execution count.
        /// </param>
        /// <returns>
        /// Non-zero to indicate the count was updated.
        /// </returns>
        public virtual bool CountUsage(
            ref long count
            )
        {
            count = Interlocked.Increment(ref usageCount);
            return true;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method records profiling information for an execution of this
        /// command, incrementing the execution count and adding the supplied
        /// elapsed time to the accumulated total.
        /// </summary>
        /// <param name="microseconds">
        /// On input, the elapsed time, in microseconds, to add to the
        /// accumulated total; upon return, the resulting total.
        /// </param>
        /// <returns>
        /// Non-zero to indicate the profiling information was recorded.
        /// </returns>
        public virtual bool ProfileUsage(
            ref long microseconds
            )
        {
            /* IGNORED */
            Interlocked.Increment(ref usageCount);

            microseconds = Interlocked.Add(
                ref usageMicroseconds, microseconds);

            return true;
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region ITypeAndName Members
        /// <summary>
        /// The associated type name for this command, if any.
        /// </summary>
        private string typeName;
        /// <summary>
        /// Gets or sets the associated type name for this command, if any.
        /// </summary>
        public virtual string TypeName
        {
            get { return typeName; }
            set { typeName = value; }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// The associated type for this command, if any.
        /// </summary>
        private Type type;
        /// <summary>
        /// Gets or sets the associated type for this command, if any.
        /// </summary>
        public virtual Type Type
        {
            get { return type; }
            set { type = value; }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region ICommandBaseData Members
        /// <summary>
        /// Gets or sets the flags associated with this command; this accessor
        /// shares the same underlying storage as <see cref="Flags" />.
        /// </summary>
        public virtual CommandFlags CommandFlags
        {
            get { return flags; }
            set { flags = value; }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region IHavePlugin Members
        /// <summary>
        /// The plugin that provides this command, if any.
        /// </summary>
        private IPlugin plugin;
        /// <summary>
        /// Gets or sets the plugin that provides this command, if any.
        /// </summary>
        public virtual IPlugin Plugin
        {
            get { return plugin; }
            set { plugin = value; }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region ICommandData Members
        /// <summary>
        /// The flags that control the behavior of this command.
        /// </summary>
        private CommandFlags flags;
        /// <summary>
        /// Gets or sets the flags that control the behavior of this command.
        /// </summary>
        public virtual CommandFlags Flags
        {
            get { return flags; }
            set { flags = value; }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region IWrapperData Members
        /// <summary>
        /// The interpreter token that identifies this command within its
        /// containing collection.
        /// </summary>
        private long token;
        /// <summary>
        /// Gets or sets the interpreter token that identifies this command
        /// within its containing collection.
        /// </summary>
        public virtual long Token
        {
            get { return token; }
            set { token = value; }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region ISyntax Members
        /// <summary>
        /// The usage syntax string describing how this command is invoked, if
        /// any.
        /// </summary>
        private string syntax;
        /// <summary>
        /// Gets or sets the usage syntax string describing how this command is
        /// invoked, if any.
        /// </summary>
        public virtual string Syntax
        {
            get { return syntax; }
            set { syntax = value; }
        }
        #endregion
    }
}
