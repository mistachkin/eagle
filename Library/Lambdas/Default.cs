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
using Eagle._Interfaces.Private;
using Eagle._Interfaces.Public;

namespace Eagle._Lambdas
{
    /// <summary>
    /// This class provides the default implementation of the
    /// <see cref="ILambda" /> interface, which represents a lambda term (an
    /// anonymous procedure) within the Eagle engine.  It supplies the common
    /// identity, usage tracking, recursion-level, and procedure-data storage
    /// shared by all lambda variants; its <see cref="Execute" /> method is a
    /// no-op placeholder that derived classes override to bind arguments and
    /// evaluate the lambda body.  See <c>core_language.md</c> for procedure
    /// and lambda semantics.
    /// </summary>
    [ObjectId("e55df68b-77d6-4d25-8a9c-8932bf375941")]
    internal class Default : ILambda
    {
        #region Public Constructors
        /// <summary>
        /// Constructs an instance of the default lambda term, optionally
        /// initializing it from the supplied lambda data.
        /// </summary>
        /// <param name="lambdaData">
        /// The data used to create and identify this lambda term, such as its
        /// name, arguments, and body.  This parameter may be null, in which
        /// case the lambda term is left with default property values.
        /// </param>
        public Default(
            ILambdaData lambdaData
            )
        {
            kind = IdentifierKind.Lambda;

            if ((lambdaData == null) ||
                !FlagOps.HasFlags(lambdaData.Flags,
                    ProcedureFlags.NoAttributes, true))
            {
                id = AttributeOps.GetObjectId(this);
                group = AttributeOps.GetObjectGroups(this);
            }

            if (lambdaData != null)
            {
                id = lambdaData.Id;

                EntityOps.MaybeSetupId(this);

                EntityOps.MaybeSetGroup(
                    this, lambdaData.Group);

                name = lambdaData.Name;
                description = lambdaData.Description;
                flags = lambdaData.Flags;
                clientData = lambdaData.ClientData;
                arguments = lambdaData.Arguments;
                namedArguments = lambdaData.NamedArguments;
                overwriteArguments = lambdaData.OverwriteArguments;
                cleanArguments = lambdaData.CleanArguments;
                body = lambdaData.Body;
                location = lambdaData.Location;
                token = lambdaData.Token;
            }

            callback = null;
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region System.Object Overrides
        /// <summary>
        /// Returns a string representation of this lambda term.
        /// </summary>
        /// <returns>
        /// A list-formatted string containing the type name and the name of
        /// this lambda term; if this lambda term has no name, the default
        /// string representation from the base class is returned instead.
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
        /// The name of this lambda term.
        /// </summary>
        private string name;
        /// <summary>
        /// Gets or sets the name of this lambda term.
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
        /// The kind of identifier represented by this lambda term.
        /// </summary>
        private IdentifierKind kind;
        /// <summary>
        /// Gets or sets the kind of identifier represented by this lambda
        /// term.
        /// </summary>
        public virtual IdentifierKind Kind
        {
            get { return kind; }
            set { kind = value; }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// The unique identifier for this lambda term.
        /// </summary>
        private Guid id;
        /// <summary>
        /// Gets or sets the unique identifier for this lambda term.
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
        /// The extra, caller-specific data associated with this lambda term,
        /// if any.
        /// </summary>
        private IClientData clientData;
        /// <summary>
        /// Gets or sets the extra, caller-specific data associated with this
        /// lambda term.
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
        /// The name of the group this lambda term belongs to, if any.
        /// </summary>
        private string group;
        /// <summary>
        /// Gets or sets the name of the group this lambda term belongs to.
        /// </summary>
        public virtual string Group
        {
            get { return group; }
            set { group = value; }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// The human-readable description of this lambda term, if any.
        /// </summary>
        private string description;
        /// <summary>
        /// Gets or sets the human-readable description of this lambda term.
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
        /// The number of times this lambda term has been used (i.e.
        /// executed).
        /// </summary>
        private long usageCount;

        /// <summary>
        /// The total amount of time, in microseconds, spent executing this
        /// lambda term.
        /// </summary>
        private long usageMicroseconds;

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Resets the specified usage statistic for this lambda term to zero.
        /// </summary>
        /// <param name="type">
        /// The kind of usage statistic to reset.
        /// </param>
        /// <param name="value">
        /// Upon success, receives the previous value of the usage statistic
        /// prior to it being reset.
        /// </param>
        /// <returns>
        /// True if the specified usage statistic is supported and was reset;
        /// otherwise, false.
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
        /// lambda term.
        /// </summary>
        /// <param name="type">
        /// The kind of usage statistic to query.
        /// </param>
        /// <param name="value">
        /// Upon success, receives the current value of the usage statistic.
        /// </param>
        /// <returns>
        /// True if the specified usage statistic is supported and was queried;
        /// otherwise, false.
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
        /// Sets the specified usage statistic for this lambda term to the
        /// supplied value.
        /// </summary>
        /// <param name="type">
        /// The kind of usage statistic to set.
        /// </param>
        /// <param name="value">
        /// On input, the new value for the usage statistic.  Upon success,
        /// receives the previous value of the usage statistic.
        /// </param>
        /// <returns>
        /// True if the specified usage statistic is supported and was set;
        /// otherwise, false.
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
        /// Adds the supplied value to the specified usage statistic for this
        /// lambda term.
        /// </summary>
        /// <param name="type">
        /// The kind of usage statistic to add to.
        /// </param>
        /// <param name="value">
        /// On input, the amount to add to the usage statistic.  Upon success,
        /// receives the new value of the usage statistic after the addition.
        /// </param>
        /// <returns>
        /// True if the specified usage statistic is supported and was added
        /// to; otherwise, false.
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
        /// Increments the usage count for this lambda term by one.
        /// </summary>
        /// <param name="count">
        /// Upon success, receives the new usage count after it has been
        /// incremented.
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
        /// Increments the usage count for this lambda term by one and adds
        /// the supplied elapsed time to its accumulated execution time.
        /// </summary>
        /// <param name="microseconds">
        /// On input, the amount of elapsed time, in microseconds, to add to
        /// the accumulated execution time.  Upon success, receives the new
        /// accumulated execution time after the addition.
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
        /// The current number of active (nested) executions of this lambda
        /// term.
        /// </summary>
        private int levels;
        /// <summary>
        /// Gets the current number of active (nested) executions of this
        /// lambda term.
        /// </summary>
        public virtual int Levels
        {
            get { return Interlocked.CompareExchange(ref levels, 0, 0); }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Increments and returns the active execution level for this lambda
        /// term.  This method is called upon entry to an execution.
        /// </summary>
        /// <returns>
        /// The new active execution level after it has been incremented.
        /// </returns>
        public virtual int EnterLevel()
        {
            return Interlocked.Increment(ref levels);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Decrements and returns the active execution level for this lambda
        /// term.  This method is called upon exit from an execution.
        /// </summary>
        /// <returns>
        /// The new active execution level after it has been decremented.
        /// </returns>
        public virtual int ExitLevel()
        {
            return Interlocked.Decrement(ref levels);
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region IProcedureData Members
        /// <summary>
        /// The flags that control the behavior of this lambda term.
        /// </summary>
        private ProcedureFlags flags;
        /// <summary>
        /// Gets or sets the flags that control the behavior of this lambda
        /// term.
        /// </summary>
        public virtual ProcedureFlags Flags
        {
            get { return flags; }
            set { flags = value; }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// The list of formal (positional) arguments for this lambda term.
        /// </summary>
        private ArgumentList arguments;
        /// <summary>
        /// Gets or sets the list of formal (positional) arguments for this
        /// lambda term.
        /// </summary>
        public virtual ArgumentList Arguments
        {
            get { return arguments; }
            set { arguments = value; }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// The collection of named arguments for this lambda term, keyed by
        /// argument name.
        /// </summary>
        private ArgumentDictionary namedArguments;
        /// <summary>
        /// Gets or sets the collection of named arguments for this lambda
        /// term, keyed by argument name.
        /// </summary>
        public virtual ArgumentDictionary NamedArguments
        {
            get { return namedArguments; }
            set { namedArguments = value; }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// The list of arguments whose values should overwrite any existing
        /// variables when this lambda term is executed.
        /// </summary>
        private ArgumentList overwriteArguments;
        /// <summary>
        /// Gets or sets the list of arguments whose values should overwrite
        /// any existing variables when this lambda term is executed.
        /// </summary>
        public virtual ArgumentList OverwriteArguments
        {
            get { return overwriteArguments; }
            set { overwriteArguments = value; }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// The list of arguments whose variables should be removed (cleaned
        /// up) after this lambda term has finished executing.
        /// </summary>
        private ArgumentList cleanArguments;
        /// <summary>
        /// Gets or sets the list of arguments whose variables should be
        /// removed (cleaned up) after this lambda term has finished
        /// executing.
        /// </summary>
        public virtual ArgumentList CleanArguments
        {
            get { return cleanArguments; }
            set { cleanArguments = value; }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// The script body that is evaluated when this lambda term is
        /// executed.
        /// </summary>
        private string body;
        /// <summary>
        /// Gets or sets the script body that is evaluated when this lambda
        /// term is executed.
        /// </summary>
        public virtual string Body
        {
            get { return body; }
            set { body = value; }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// The source script location associated with the body of this lambda
        /// term, if any.
        /// </summary>
        private IScriptLocation location;
        /// <summary>
        /// Gets or sets the source script location associated with the body of
        /// this lambda term.
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
        /// The interpreter token that uniquely identifies this lambda term
        /// within its containing collection.
        /// </summary>
        private long token;
        /// <summary>
        /// Gets or sets the interpreter token that uniquely identifies this
        /// lambda term within its containing collection.
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
        /// The delegate, if any, used to dynamically execute this lambda term.
        /// </summary>
        private ExecuteCallback callback;
        /// <summary>
        /// Gets or sets the delegate used to dynamically execute this lambda
        /// term.
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
        /// Executes this lambda term.  This default implementation performs no
        /// work and always succeeds; derived classes override it to bind the
        /// supplied arguments and evaluate the lambda body.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter context this lambda term is executing in.  This
        /// parameter should not be null.
        /// </param>
        /// <param name="clientData">
        /// The extra, caller-specific data supplied for this invocation, if
        /// any.  This parameter may be null.
        /// </param>
        /// <param name="arguments">
        /// The list of arguments for this invocation.  This parameter should
        /// not be null.
        /// </param>
        /// <param name="result">
        /// Upon success, this may contain the result value produced by the
        /// lambda term.  Upon failure, this must contain an appropriate error
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
    }
}
