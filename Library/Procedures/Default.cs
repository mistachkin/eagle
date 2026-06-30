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

namespace Eagle._Procedures
{
    /// <summary>
    /// This class provides the default implementation of the
    /// <see cref="IProcedure" /> interface and serves as the base class for the
    /// Eagle procedures.  It stores the common identification, argument, body,
    /// usage, and call-level data for a procedure and provides a default
    /// execution method that takes no action and always succeeds.
    /// </summary>
    [ObjectId("64ea360e-9474-4f70-9ae8-1e363c3a156e")]
    public class Default :
#if ISOLATED_INTERPRETERS || ISOLATED_PLUGINS
        ScriptMarshalByRefObject,
#endif
        IProcedure
    {
        #region Public Constructors
        /// <summary>
        /// Constructs an instance of the default procedure, optionally
        /// initializing it from the specified procedure metadata.
        /// </summary>
        /// <param name="procedureData">
        /// The data used to create and identify this procedure, such as its
        /// name, arguments, and body.  This parameter may be null.
        /// </param>
        public Default(
            IProcedureData procedureData
            )
        {
            kind = IdentifierKind.Procedure;

            if ((procedureData == null) ||
                !FlagOps.HasFlags(procedureData.Flags,
                    ProcedureFlags.NoAttributes, true))
            {
                id = AttributeOps.GetObjectId(this);
                group = AttributeOps.GetObjectGroups(this);
            }

            if (procedureData != null)
            {
                id = procedureData.Id;

                EntityOps.MaybeSetupId(this);

                EntityOps.MaybeSetGroup(
                    this, procedureData.Group);

                name = procedureData.Name;
                description = procedureData.Description;
                flags = procedureData.Flags;
                clientData = procedureData.ClientData;
                arguments = procedureData.Arguments;
                namedArguments = procedureData.NamedArguments;
                overwriteArguments = procedureData.OverwriteArguments;
                cleanArguments = procedureData.CleanArguments;
                body = procedureData.Body;
                location = procedureData.Location;
                token = procedureData.Token;
            }

            callback = null;
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region System.Object Overrides
        /// <summary>
        /// Returns a string representation of this procedure, consisting of its
        /// type name and, when available, its name.
        /// </summary>
        /// <returns>
        /// A string that represents this procedure.
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
        /// The name of this procedure.
        /// </summary>
        private string name;
        /// <summary>
        /// Gets or sets the name of this procedure.
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
        /// The kind of identifier represented by this procedure.
        /// </summary>
        private IdentifierKind kind;
        /// <summary>
        /// Gets or sets the kind of identifier represented by this procedure.
        /// </summary>
        public virtual IdentifierKind Kind
        {
            get { return kind; }
            set { kind = value; }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// The unique identifier (GUID) of this procedure.
        /// </summary>
        private Guid id;
        /// <summary>
        /// Gets or sets the unique identifier (GUID) of this procedure.
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
        /// The extra data associated with this procedure, if any.
        /// </summary>
        private IClientData clientData;
        /// <summary>
        /// Gets or sets the extra data associated with this procedure, if any.
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
        /// The object group that this procedure belongs to, if any.
        /// </summary>
        private string group;
        /// <summary>
        /// Gets or sets the object group that this procedure belongs to, if
        /// any.
        /// </summary>
        public virtual string Group
        {
            get { return group; }
            set { group = value; }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// The human-readable description of this procedure, if any.
        /// </summary>
        private string description;
        /// <summary>
        /// Gets or sets the human-readable description of this procedure, if
        /// any.
        /// </summary>
        public virtual string Description
        {
            get { return description; }
            set { description = value; }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region IUsageData Members
        /// <summary>
        /// The number of times this procedure has been used (executed).
        /// </summary>
        private long usageCount;
        /// <summary>
        /// The cumulative time, in microseconds, spent executing this
        /// procedure.
        /// </summary>
        private long usageMicroseconds;

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Resets the specified usage statistic for this procedure to zero,
        /// returning its previous value.
        /// </summary>
        /// <param name="type">
        /// The kind of usage statistic to reset.
        /// </param>
        /// <param name="value">
        /// Upon success, this will contain the previous value of the specified
        /// usage statistic.
        /// </param>
        /// <returns>
        /// True if the specified usage statistic was reset; otherwise, false.
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
        /// Gets the current value of the specified usage statistic for this
        /// procedure.
        /// </summary>
        /// <param name="type">
        /// The kind of usage statistic to query.
        /// </param>
        /// <param name="value">
        /// Upon success, this will contain the current value of the specified
        /// usage statistic.
        /// </param>
        /// <returns>
        /// True if the specified usage statistic was retrieved; otherwise,
        /// false.
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
        /// Sets the specified usage statistic for this procedure to the
        /// supplied value, returning its previous value.
        /// </summary>
        /// <param name="type">
        /// The kind of usage statistic to set.
        /// </param>
        /// <param name="value">
        /// Upon input, the new value for the specified usage statistic; upon
        /// success, its previous value.
        /// </param>
        /// <returns>
        /// True if the specified usage statistic was set; otherwise, false.
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
        /// Adds the supplied amount to the specified usage statistic for this
        /// procedure, returning the resulting value.
        /// </summary>
        /// <param name="type">
        /// The kind of usage statistic to add to.
        /// </param>
        /// <param name="value">
        /// Upon input, the amount to add; upon success, the resulting value of
        /// the specified usage statistic.
        /// </param>
        /// <returns>
        /// True if the specified usage statistic was updated; otherwise, false.
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
        /// Increments the usage (execution) count for this procedure,
        /// returning the resulting value.
        /// </summary>
        /// <param name="count">
        /// Upon success, this will contain the resulting usage count.
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
        /// Records the execution of this procedure by incrementing its usage
        /// count and adding the supplied elapsed time to its cumulative
        /// microseconds.
        /// </summary>
        /// <param name="microseconds">
        /// Upon input, the elapsed time, in microseconds, to add; upon success,
        /// the resulting cumulative microseconds.
        /// </param>
        /// <returns>
        /// True if the usage statistics were updated; otherwise, false.
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

        #region ILevels Members
        /// <summary>
        /// The current number of active (nested) executions of this procedure.
        /// </summary>
        private int levels;
        /// <summary>
        /// Gets the current number of active (nested) executions of this
        /// procedure.
        /// </summary>
        public virtual int Levels
        {
            get { return Interlocked.CompareExchange(ref levels, 0, 0); }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Increments the active execution level of this procedure upon entry.
        /// </summary>
        /// <returns>
        /// The resulting active execution level.
        /// </returns>
        public virtual int EnterLevel()
        {
            return Interlocked.Increment(ref levels);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Decrements the active execution level of this procedure upon exit.
        /// </summary>
        /// <returns>
        /// The resulting active execution level.
        /// </returns>
        public virtual int ExitLevel()
        {
            return Interlocked.Decrement(ref levels);
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region IProcedureData Members
        /// <summary>
        /// The flags that control the behavior of this procedure.
        /// </summary>
        private ProcedureFlags flags;
        /// <summary>
        /// Gets or sets the flags that control the behavior of this procedure.
        /// </summary>
        public virtual ProcedureFlags Flags
        {
            get { return flags; }
            set { flags = value; }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// The formal (positional) arguments of this procedure.
        /// </summary>
        private ArgumentList arguments;
        /// <summary>
        /// Gets or sets the formal (positional) arguments of this procedure.
        /// </summary>
        public virtual ArgumentList Arguments
        {
            get { return arguments; }
            set { arguments = value; }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// The named arguments of this procedure.
        /// </summary>
        private ArgumentDictionary namedArguments;
        /// <summary>
        /// Gets or sets the named arguments of this procedure.
        /// </summary>
        public virtual ArgumentDictionary NamedArguments
        {
            get { return namedArguments; }
            set { namedArguments = value; }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// The arguments whose values should overwrite any existing variables
        /// when this procedure is invoked.
        /// </summary>
        private ArgumentList overwriteArguments;
        /// <summary>
        /// Gets or sets the arguments whose values should overwrite any
        /// existing variables when this procedure is invoked.
        /// </summary>
        public virtual ArgumentList OverwriteArguments
        {
            get { return overwriteArguments; }
            set { overwriteArguments = value; }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// The arguments that should be unset (cleaned up) when this procedure
        /// exits.
        /// </summary>
        private ArgumentList cleanArguments;
        /// <summary>
        /// Gets or sets the arguments that should be unset (cleaned up) when
        /// this procedure exits.
        /// </summary>
        public virtual ArgumentList CleanArguments
        {
            get { return cleanArguments; }
            set { cleanArguments = value; }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// The script body of this procedure.
        /// </summary>
        private string body;
        /// <summary>
        /// Gets or sets the script body of this procedure.
        /// </summary>
        public virtual string Body
        {
            get { return body; }
            set { body = value; }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// The script location where this procedure was defined.
        /// </summary>
        private IScriptLocation location;
        /// <summary>
        /// Gets or sets the script location where this procedure was defined.
        /// </summary>
        public virtual IScriptLocation Location
        {
            get { return location; }
            set { location = value; }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region IWrapperData Members
        /// <summary>
        /// The token that identifies this procedure within the interpreter.
        /// </summary>
        private long token;
        /// <summary>
        /// Gets or sets the token that identifies this procedure within the
        /// interpreter.
        /// </summary>
        public virtual long Token
        {
            get { return token; }
            set { token = value; }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region IDynamicExecuteCallback Members
        /// <summary>
        /// The delegate invoked to execute this procedure, if any.
        /// </summary>
        private ExecuteCallback callback;
        /// <summary>
        /// Gets or sets the delegate invoked to execute this procedure, if any.
        /// </summary>
        public virtual ExecuteCallback Callback
        {
            get { return callback; }
            set { callback = value; }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region IExecute Members
        /// <summary>
        /// Executes this procedure.  This default implementation takes no
        /// action and always succeeds.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter context this procedure is executing in.  This
        /// parameter may be null.
        /// </param>
        /// <param name="clientData">
        /// The extra, procedure-specific data supplied when the procedure was
        /// created, if any.  This parameter may be null.
        /// </param>
        /// <param name="arguments">
        /// The list of arguments for this invocation.  This parameter may be
        /// null.
        /// </param>
        /// <param name="result">
        /// Upon success, this may contain the result value produced by the
        /// procedure.  Upon failure, this may contain an appropriate error
        /// message.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> in all cases.
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
    }
}
