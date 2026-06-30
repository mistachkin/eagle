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
using Eagle._Containers.Public;
using Eagle._Interfaces.Private;
using Eagle._Interfaces.Public;

namespace Eagle._Operators
{
    /// <summary>
    /// This class provides the default, fully-featured base implementation of
    /// the <see cref="IOperator" /> interface.  Its <see cref="Execute" />
    /// method does nothing and simply returns <see cref="ReturnCode.Ok" />;
    /// concrete expression operators typically derive from a more specialized
    /// base (such as <c>Math</c>, <c>Logic</c>, <c>String</c>, or
    /// <c>MaybeString</c>) selected by their lexeme and override the behavior
    /// they need.  This class also supplies the standard identifier, client
    /// data, usage, and operator metadata accessors.  See
    /// <c>core_language.md</c> for how the engine creates and evaluates
    /// expression operators.
    /// </summary>
    [ObjectId("459dfcd1-713f-4d88-baf4-b6709674c587")]
    [ObjectGroup("default")]
    internal class Default : IOperator
    {
        #region Public Constructors
        /// <summary>
        /// Constructs an instance of the default operator, copying its identity
        /// and metadata from the supplied operator data.
        /// </summary>
        /// <param name="operatorData">
        /// The data used to create and identify this operator, such as its
        /// name, description, flags, lexeme, operands, plugin, client data, and
        /// token.  This parameter may be null.
        /// </param>
        public Default(
            IOperatorData operatorData /* in */
            )
        {
            kind = IdentifierKind.Operator;

            if ((operatorData == null) ||
                !FlagOps.HasFlags(operatorData.Flags,
                    OperatorFlags.NoAttributes, true))
            {
                id = AttributeOps.GetObjectId(this);
                group = AttributeOps.GetObjectGroups(this);
            }

            if (operatorData != null)
            {
                id = operatorData.Id;

                EntityOps.MaybeSetupId(this);

                EntityOps.MaybeSetGroup(
                    this, operatorData.Group);

                name = operatorData.Name;
                description = operatorData.Description;
                clientData = operatorData.ClientData;
                typeName = operatorData.TypeName;
                lexeme = operatorData.Lexeme;
                operands = operatorData.Operands;
                types = operatorData.Types;
                flags = operatorData.Flags;
                comparisonType = operatorData.ComparisonType;
                plugin = operatorData.Plugin;
                token = operatorData.Token;
            }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region IIdentifierName Members
        /// <summary>
        /// The name of this operator, as used to invoke it.
        /// </summary>
        private string name;
        /// <summary>
        /// Gets or sets the name of this operator, as used to invoke it.
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
        /// The kind of identifier represented by this operator (e.g.
        /// <see cref="IdentifierKind.Operator" />).
        /// </summary>
        private IdentifierKind kind;
        /// <summary>
        /// Gets or sets the kind of identifier represented by this operator.
        /// </summary>
        public virtual IdentifierKind Kind
        {
            get { return kind; }
            set { kind = value; }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// The unique identifier (object identifier) for this operator.
        /// </summary>
        private Guid id;
        /// <summary>
        /// Gets or sets the unique identifier (object identifier) for this
        /// operator.
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
        /// The extra, operator-specific data associated with this operator, if
        /// any.
        /// </summary>
        private IClientData clientData;
        /// <summary>
        /// Gets or sets the extra, operator-specific data associated with this
        /// operator, if any.
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
        /// The object group that this operator belongs to.
        /// </summary>
        private string group;
        /// <summary>
        /// Gets or sets the object group that this operator belongs to.
        /// </summary>
        public virtual string Group
        {
            get { return group; }
            set { group = value; }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// The human-readable description of this operator.
        /// </summary>
        private string description;
        /// <summary>
        /// Gets or sets the human-readable description of this operator.
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
        /// The net number of times this operator has been initialized minus the
        /// number of times it has been terminated; a positive value indicates
        /// the operator is currently initialized.
        /// </summary>
        private int initializeCount;
        /// <summary>
        /// Gets or sets a value indicating whether this operator is currently
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
        /// This method initializes this operator for use within the specified
        /// interpreter, incrementing its initialization count.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter context this operator is being initialized for.
        /// This parameter should not be null.
        /// </param>
        /// <param name="clientData">
        /// The extra, operator-specific data supplied for initialization, if
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
            Interpreter interpreter,
            IClientData clientData,
            ref Result result
            )
        {
            Interlocked.Increment(ref initializeCount);
            return ReturnCode.Ok;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method terminates this operator for the specified interpreter,
        /// decrementing its initialization count.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter context this operator is being terminated for.  This
        /// parameter should not be null.
        /// </param>
        /// <param name="clientData">
        /// The extra, operator-specific data supplied for termination, if any.
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
            Interpreter interpreter,
            IClientData clientData,
            ref Result result
            )
        {
            Interlocked.Decrement(ref initializeCount);
            return ReturnCode.Ok;
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region IHavePlugin Members
        /// <summary>
        /// The plugin that provides this operator, if any.
        /// </summary>
        private IPlugin plugin;
        /// <summary>
        /// Gets or sets the plugin that provides this operator, if any.
        /// </summary>
        public virtual IPlugin Plugin
        {
            get { return plugin; }
            set { plugin = value; }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region ITypeAndName Members
        /// <summary>
        /// The associated type name for this operator, if any.
        /// </summary>
        private string typeName;
        /// <summary>
        /// Gets or sets the associated type name for this operator, if any.
        /// </summary>
        public virtual string TypeName
        {
            get { return typeName; }
            set { typeName = value; }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// The associated type for this operator, if any.
        /// </summary>
        private Type type;
        /// <summary>
        /// Gets or sets the associated type for this operator, if any.
        /// </summary>
        public virtual Type Type
        {
            get { return type; }
            set { type = value; }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region IOperatorData Members
        /// <summary>
        /// The lexeme that identifies this operator to the expression engine
        /// and selects its evaluation behavior.
        /// </summary>
        private Lexeme lexeme;
        /// <summary>
        /// Gets or sets the lexeme that identifies this operator to the
        /// expression engine and selects its evaluation behavior.
        /// </summary>
        public virtual Lexeme Lexeme
        {
            get { return lexeme; }
            set { lexeme = value; }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// The number of operands accepted by this operator (e.g. one for a
        /// unary operator or two for a binary operator).
        /// </summary>
        private int operands;
        /// <summary>
        /// Gets or sets the number of operands accepted by this operator (e.g.
        /// one for a unary operator or two for a binary operator).
        /// </summary>
        public virtual int Operands
        {
            get { return operands; }
            set { operands = value; }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// The list of permitted operand types for this operator, if any.
        /// </summary>
        private TypeList types;
        /// <summary>
        /// Gets or sets the list of permitted operand types for this operator,
        /// if any.
        /// </summary>
        public virtual TypeList Types
        {
            get { return types; }
            set { types = value; }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// The flags that control the behavior of this operator.
        /// </summary>
        private OperatorFlags flags;
        /// <summary>
        /// Gets or sets the flags that control the behavior of this operator.
        /// </summary>
        public virtual OperatorFlags Flags
        {
            get { return flags; }
            set { flags = value; }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// The kind of string comparison used by this operator when comparing
        /// string operands.
        /// </summary>
        private StringComparison comparisonType;
        /// <summary>
        /// Gets or sets the kind of string comparison used by this operator
        /// when comparing string operands.
        /// </summary>
        public virtual StringComparison ComparisonType
        {
            get { return comparisonType; }
            set { comparisonType = value; }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region IWrapperData Members
        /// <summary>
        /// The interpreter token that identifies this operator within its
        /// containing collection.
        /// </summary>
        private long token;
        /// <summary>
        /// Gets or sets the interpreter token that identifies this operator
        /// within its containing collection.
        /// </summary>
        public virtual long Token
        {
            get { return token; }
            set { token = value; }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region IExecuteArgument Members
        /// <summary>
        /// This method evaluates the default operator.  The default
        /// implementation performs no action and simply returns
        /// <see cref="ReturnCode.Ok" />; derived operators override this method
        /// to provide their actual behavior.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter context this operator is executing in.  This
        /// parameter should not be null.
        /// </param>
        /// <param name="clientData">
        /// The extra, operator-specific data supplied when this operator was
        /// created, if any.  This parameter may be null.
        /// </param>
        /// <param name="arguments">
        /// The list of arguments for this invocation.  Element zero is the
        /// operator name; the remaining elements are its operands.  This
        /// parameter should not be null.
        /// </param>
        /// <param name="value">
        /// Upon success, this may contain the result value produced by the
        /// operator.
        /// </param>
        /// <param name="error">
        /// Upon failure, this contains an appropriate error message.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success, with the result placed in
        /// <paramref name="value" />; otherwise, a non-Ok value with details
        /// placed in <paramref name="error" />.
        /// </returns>
        public virtual ReturnCode Execute(
            Interpreter interpreter, /* in */
            IClientData clientData,  /* in */
            ArgumentList arguments,  /* in */
            ref Argument value,      /* out */
            ref Result error         /* out */
            )
        {
            return ReturnCode.Ok;
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region IUsageData Members
        /// <summary>
        /// The number of times this operator has been executed.
        /// </summary>
        private long usageCount;
        /// <summary>
        /// The total number of microseconds spent executing this operator.
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
            ref long value  /* out */
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
            ref long value  /* out */
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
            ref long value  /* out */
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
            ref long value  /* out */
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
        /// This method increments the execution count for this operator and
        /// reports the new count.
        /// </summary>
        /// <param name="count">
        /// Upon return, this contains the updated execution count.
        /// </param>
        /// <returns>
        /// Non-zero to indicate the count was updated.
        /// </returns>
        public virtual bool CountUsage(
            ref long count /* out */
            )
        {
            count = Interlocked.Increment(ref usageCount);
            return true;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method records profiling information for an execution of this
        /// operator, incrementing the execution count and adding the supplied
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
            ref long microseconds /* out */
            )
        {
            Interlocked.Increment(ref usageCount);

            microseconds = Interlocked.Add(
                ref usageMicroseconds, microseconds);

            return true;
        }
        #endregion
    }
}
