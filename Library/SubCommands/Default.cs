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

namespace Eagle._SubCommands
{
    /// <summary>
    /// This class provides the default, base implementation of a sub-command.
    /// It stores the common identity, client data, ensemble, usage, and
    /// delegate-related state shared by all sub-commands, and provides a
    /// trivial successful <see cref="Execute" /> implementation that derived
    /// classes are expected to override.
    /// </summary>
    [ObjectId("dbb3b436-3d13-4b71-80ee-a633d47c8384")]
    [ObjectGroup("default")]
    public class Default :
#if ISOLATED_INTERPRETERS || ISOLATED_PLUGINS
        ScriptMarshalByRefObject,
#endif
        ISubCommand
    {
        #region Public Constructors
        /// <summary>
        /// Constructs an instance of the default sub-command, initializing its
        /// identity and other state from the supplied sub-command data.
        /// </summary>
        /// <param name="subCommandData">
        /// The data used to create and identify this sub-command, such as its
        /// name, flags, and owning command.  This parameter may be null.
        /// </param>
        public Default(
            ISubCommandData subCommandData
            )
        {
            kind = IdentifierKind.SubCommand;

            if ((subCommandData == null) ||
                !FlagOps.HasFlags(subCommandData.Flags,
                    SubCommandFlags.NoAttributes, true))
            {
                id = AttributeOps.GetObjectId(this);
                group = AttributeOps.GetObjectGroups(this);
            }

            //
            // NOTE: Is the supplied command data valid?
            //
            if (subCommandData != null)
            {
                id = subCommandData.Id;

                EntityOps.MaybeSetupId(this);

                EntityOps.MaybeSetGroup(
                    this, subCommandData.Group);

                name = subCommandData.Name;
                description = subCommandData.Description;
                nameIndex = subCommandData.NameIndex;
                commandFlags = subCommandData.CommandFlags;
                subCommandFlags = subCommandData.Flags;
                command = subCommandData.Command;
                clientData = subCommandData.ClientData;
                token = subCommandData.Token;
            }

            callback = null;
            subCommands = null;
            syntax = null;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Constructs an instance of the default sub-command, initializing its
        /// identity from the supplied sub-command data and its delegate-related
        /// state from the supplied delegate data.
        /// </summary>
        /// <param name="subCommandData">
        /// The data used to create and identify this sub-command, such as its
        /// name, flags, and owning command.  This parameter may be null.
        /// </param>
        /// <param name="delegateData">
        /// The data describing the delegate associated with this sub-command,
        /// if any.  This parameter may be null.
        /// </param>
        public Default(
            ISubCommandData subCommandData,
            IDelegateData delegateData
            )
            : this(subCommandData)
        {
            if (delegateData != null)
            {
                this.@delegate = delegateData.Delegate;
                this.delegateFlags = delegateData.DelegateFlags;
            }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region System.Object Overrides
        /// <summary>
        /// This method returns a string representation of this sub-command,
        /// consisting of its type name and name when a name is available.
        /// </summary>
        /// <returns>
        /// The string representation of this sub-command.
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
        /// The backing field for the <see cref="Name" /> property.
        /// </summary>
        private string name;

        /// <summary>
        /// Gets or sets the name of this sub-command.
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
        /// The backing field for the <see cref="Kind" /> property.
        /// </summary>
        private IdentifierKind kind;

        /// <summary>
        /// Gets or sets the kind of entity that this sub-command represents.
        /// </summary>
        public virtual IdentifierKind Kind
        {
            get { return kind; }
            set { kind = value; }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// The backing field for the <see cref="Id" /> property.
        /// </summary>
        private Guid id;

        /// <summary>
        /// Gets or sets the unique identifier of this sub-command.
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
        /// The backing field for the <see cref="ClientData" /> property.
        /// </summary>
        private IClientData clientData;

        /// <summary>
        /// Gets or sets the extra, caller-specific data associated with this
        /// sub-command.
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
        /// The backing field for the <see cref="Group" /> property.
        /// </summary>
        private string group;

        /// <summary>
        /// Gets or sets the group that this sub-command belongs to.
        /// </summary>
        public virtual string Group
        {
            get { return group; }
            set { group = value; }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// The backing field for the <see cref="Description" /> property.
        /// </summary>
        private string description;

        /// <summary>
        /// Gets or sets the human-readable description of this sub-command.
        /// </summary>
        public virtual string Description
        {
            get { return description; }
            set { description = value; }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region IDynamicExecuteCallback Members
        /// <summary>
        /// The backing field for the <see cref="Callback" /> property.
        /// </summary>
        private ExecuteCallback callback;

        /// <summary>
        /// Gets or sets the callback used to dynamically execute this
        /// sub-command, if any.
        /// </summary>
        public virtual ExecuteCallback Callback
        {
            get { return callback; }
            set { callback = value; }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region IDynamicExecuteDelegate Members
        /// <summary>
        /// The backing field for the <see cref="Delegate" /> property.
        /// </summary>
        private Delegate @delegate;

        /// <summary>
        /// Gets or sets the delegate used to dynamically execute this
        /// sub-command, if any.
        /// </summary>
        public virtual Delegate Delegate
        {
            get { return @delegate; }
            set { @delegate = value; }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region IDelegateData Members
        /// <summary>
        /// The backing field for the <see cref="DelegateFlags" /> property.
        /// </summary>
        private DelegateFlags delegateFlags;

        /// <summary>
        /// Gets or sets the flags that control how the associated delegate is
        /// invoked.
        /// </summary>
        public virtual DelegateFlags DelegateFlags
        {
            get { return delegateFlags; }
            set { delegateFlags = value; }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region IEnsemble Members
        /// <summary>
        /// The backing field for the <see cref="SubCommands" /> property.
        /// </summary>
        private EnsembleDictionary subCommands;

        /// <summary>
        /// Gets or sets the collection of sub-commands contained within this
        /// sub-command ensemble, if any.
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
        /// The backing field for the <see cref="AllowedSubCommands" />
        /// property.
        /// </summary>
        private EnsembleDictionary allowedSubCommands;

        /// <summary>
        /// Gets or sets the collection of sub-commands that are explicitly
        /// allowed by policy, if any.
        /// </summary>
        public virtual EnsembleDictionary AllowedSubCommands
        {
            get { return allowedSubCommands; }
            set { allowedSubCommands = value; }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// The backing field for the <see cref="DisallowedSubCommands" />
        /// property.
        /// </summary>
        private EnsembleDictionary disallowedSubCommands;

        /// <summary>
        /// Gets or sets the collection of sub-commands that are explicitly
        /// disallowed by policy, if any.
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
        /// This method executes the sub-command for a single invocation.  The
        /// default implementation does nothing and simply reports success;
        /// derived classes are expected to override it.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter context this sub-command is executing in.  This
        /// parameter may be null.
        /// </param>
        /// <param name="clientData">
        /// The extra, sub-command-specific data supplied for this invocation,
        /// if any.  This parameter may be null.
        /// </param>
        /// <param name="arguments">
        /// The list of arguments for this invocation.  This parameter may be
        /// null.
        /// </param>
        /// <param name="result">
        /// Upon success, this may contain the result value produced by the
        /// sub-command.  Upon failure, this must contain an appropriate error
        /// message.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise, a non-Ok value
        /// with details placed in the <paramref name="result" /> parameter.
        /// </returns>
        public virtual ReturnCode Execute(
            Interpreter interpreter,
            IClientData clientData,
            ArgumentList arguments,
            ref Result result
            )
        {
            return ReturnCode.Ok;
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region IUsageData Members
        /// <summary>
        /// The number of times this sub-command has been used.
        /// </summary>
        private long usageCount;

        /// <summary>
        /// The cumulative number of microseconds spent executing this
        /// sub-command.
        /// </summary>
        private long usageMicroseconds;

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method resets the specified usage statistic to zero, returning
        /// its previous value.
        /// </summary>
        /// <param name="type">
        /// The kind of usage statistic to reset.
        /// </param>
        /// <param name="value">
        /// Upon success, this is set to the previous value of the statistic.
        /// </param>
        /// <returns>
        /// True if the usage statistic was reset; otherwise, false.
        /// </returns>
        public virtual bool ResetUsage(
            UsageType type,
            ref long value
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
        /// This method retrieves the current value of the specified usage
        /// statistic.
        /// </summary>
        /// <param name="type">
        /// The kind of usage statistic to retrieve.
        /// </param>
        /// <param name="value">
        /// Upon success, this is set to the current value of the statistic.
        /// </param>
        /// <returns>
        /// True if the usage statistic was retrieved; otherwise, false.
        /// </returns>
        public virtual bool GetUsage(
            UsageType type,
            ref long value
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
        /// This method sets the specified usage statistic to the supplied
        /// value, returning its previous value.
        /// </summary>
        /// <param name="type">
        /// The kind of usage statistic to set.
        /// </param>
        /// <param name="value">
        /// On input, the new value of the statistic; upon success, this is set
        /// to its previous value.
        /// </param>
        /// <returns>
        /// True if the usage statistic was set; otherwise, false.
        /// </returns>
        public virtual bool SetUsage(
            UsageType type,
            ref long value
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
        /// This method adds the supplied value to the specified usage
        /// statistic, returning the resulting total.
        /// </summary>
        /// <param name="type">
        /// The kind of usage statistic to add to.
        /// </param>
        /// <param name="value">
        /// On input, the value to add to the statistic; upon success, this is
        /// set to the resulting total.
        /// </param>
        /// <returns>
        /// True if the usage statistic was updated; otherwise, false.
        /// </returns>
        public virtual bool AddUsage(
            UsageType type,
            ref long value
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
        /// This method increments the usage count of this sub-command by one,
        /// returning the resulting count.
        /// </summary>
        /// <param name="count">
        /// Upon success, this is set to the resulting usage count.
        /// </param>
        /// <returns>
        /// True if the usage count was incremented; otherwise, false.
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
        /// This method records a single profiled use of this sub-command,
        /// incrementing the usage count and adding the supplied number of
        /// microseconds to the cumulative total.
        /// </summary>
        /// <param name="microseconds">
        /// On input, the number of microseconds to add; upon success, this is
        /// set to the resulting cumulative total.
        /// </param>
        /// <returns>
        /// True if the usage was recorded; otherwise, false.
        /// </returns>
        public virtual bool ProfileUsage(
            ref long microseconds
            )
        {
            Interlocked.Increment(ref usageCount);

            microseconds = Interlocked.Add(
                ref usageMicroseconds, microseconds);

            return true;
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region ITypeAndName Members
        /// <summary>
        /// The backing field for the <see cref="TypeName" /> property.
        /// </summary>
        private string typeName;

        /// <summary>
        /// Gets or sets the name of the type associated with this sub-command.
        /// </summary>
        public virtual string TypeName
        {
            get { return typeName; }
            set { typeName = value; }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// The backing field for the <see cref="Type" /> property.
        /// </summary>
        private Type type;

        /// <summary>
        /// Gets or sets the type associated with this sub-command.
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
        /// The backing field for the <see cref="CommandFlags" /> property.
        /// </summary>
        private CommandFlags commandFlags;

        /// <summary>
        /// Gets or sets the command flags associated with this sub-command.
        /// </summary>
        public virtual CommandFlags CommandFlags
        {
            get { return commandFlags; }
            set { commandFlags = value; }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region IHaveCommand Members
        /// <summary>
        /// The backing field for the <see cref="Command" /> property.
        /// </summary>
        private ICommand command;

        /// <summary>
        /// Gets or sets the command that owns this sub-command.
        /// </summary>
        public virtual ICommand Command
        {
            get { return command; }
            set { command = value; }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region ISubCommandData Members
        /// <summary>
        /// The backing field for the <see cref="NameIndex" /> property.
        /// </summary>
        private int nameIndex;

        /// <summary>
        /// Gets or sets the index, within the argument list, of the argument
        /// that names this sub-command.
        /// </summary>
        public virtual int NameIndex
        {
            get { return nameIndex; }
            set { nameIndex = value; }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// The backing field for the <see cref="Flags" /> property.
        /// </summary>
        private SubCommandFlags subCommandFlags;

        /// <summary>
        /// Gets or sets the flags that control the behavior of this
        /// sub-command.
        /// </summary>
        public virtual SubCommandFlags Flags
        {
            get { return subCommandFlags; }
            set { subCommandFlags = value; }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region IWrapperData Members
        /// <summary>
        /// The backing field for the <see cref="Token" /> property.
        /// </summary>
        private long token;

        /// <summary>
        /// Gets or sets the token that identifies this sub-command within the
        /// interpreter.
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
        /// The backing field for the <see cref="Syntax" /> property.
        /// </summary>
        private string syntax;

        /// <summary>
        /// Gets or sets the syntax help text for this sub-command.
        /// </summary>
        public virtual string Syntax
        {
            get { return syntax; }
            set { syntax = value; }
        }
        #endregion
    }
}
