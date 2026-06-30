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
using Eagle._Interfaces.Public;

namespace Eagle._Functions
{
    /// <summary>
    /// This class provides the default, fully-featured base implementation of
    /// the <see cref="IFunction" /> interface.  Its <see cref="Execute" />
    /// method does nothing and simply returns <see cref="ReturnCode.Ok" />;
    /// concrete expression functions typically derive from a more specialized
    /// base (such as <c>Core</c> or <c>Arguments</c>) and override the behavior
    /// they need.  This class also supplies the standard identifier, client
    /// data, usage, and function metadata accessors.  See
    /// <c>core_language.md</c> for how the engine creates and evaluates
    /// expression functions.
    /// </summary>
    [ObjectId("877d1fb8-3e45-4484-855c-c6f7fa8ee676")]
    [ObjectGroup("default")]
    public class Default :
#if ISOLATED_INTERPRETERS || ISOLATED_PLUGINS
        ScriptMarshalByRefObject,
#endif
        IFunction
    {
        #region Public Constructors
        /// <summary>
        /// Constructs an instance of the default function, copying its identity
        /// and metadata from the supplied function data.
        /// </summary>
        /// <param name="functionData">
        /// The data used to create and identify this function, such as its
        /// name, description, flags, plugin, client data, and token.  This
        /// parameter may be null.
        /// </param>
        public Default(
            IFunctionData functionData /* in */
            )
        {
            kind = IdentifierKind.Function;

            if ((functionData == null) ||
                !FlagOps.HasFlags(functionData.Flags,
                    FunctionFlags.NoAttributes, true))
            {
                id = AttributeOps.GetObjectId(this);
                group = AttributeOps.GetObjectGroups(this);
            }

            if (functionData != null)
            {
                id = functionData.Id;

                EntityOps.MaybeSetupId(this);

                EntityOps.MaybeSetGroup(
                    this, functionData.Group);

                name = functionData.Name;
                description = functionData.Description;
                clientData = functionData.ClientData;
                typeName = functionData.TypeName;
                arguments = functionData.Arguments;
                types = functionData.Types;
                flags = functionData.Flags;
                plugin = functionData.Plugin;
                token = functionData.Token;
            }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region IIdentifierName Members
        /// <summary>
        /// The name of this function, as used to invoke it.
        /// </summary>
        private string name;
        /// <summary>
        /// Gets or sets the name of this function, as used to invoke it.
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
        /// The kind of identifier represented by this function (e.g.
        /// <see cref="IdentifierKind.Function" />).
        /// </summary>
        private IdentifierKind kind;
        /// <summary>
        /// Gets or sets the kind of identifier represented by this function.
        /// </summary>
        public virtual IdentifierKind Kind
        {
            get { return kind; }
            set { kind = value; }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// The unique identifier (object identifier) for this function.
        /// </summary>
        private Guid id;
        /// <summary>
        /// Gets or sets the unique identifier (object identifier) for this
        /// function.
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
        /// The extra, function-specific data associated with this function, if
        /// any.
        /// </summary>
        private IClientData clientData;
        /// <summary>
        /// Gets or sets the extra, function-specific data associated with this
        /// function, if any.
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
        /// The object group that this function belongs to.
        /// </summary>
        private string group;
        /// <summary>
        /// Gets or sets the object group that this function belongs to.
        /// </summary>
        public virtual string Group
        {
            get { return group; }
            set { group = value; }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// The human-readable description of this function.
        /// </summary>
        private string description;
        /// <summary>
        /// Gets or sets the human-readable description of this function.
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
        /// The net number of times this function has been initialized minus the
        /// number of times it has been terminated; a positive value indicates
        /// the function is currently initialized.
        /// </summary>
        private int initializeCount;
        /// <summary>
        /// Gets or sets a value indicating whether this function is currently
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
        /// This method initializes this function for use within the specified
        /// interpreter, incrementing its initialization count.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter context this function is being initialized for.
        /// This parameter should not be null.
        /// </param>
        /// <param name="clientData">
        /// The extra, function-specific data supplied for initialization, if
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
        /// This method terminates this function for the specified interpreter,
        /// decrementing its initialization count.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter context this function is being terminated for.  This
        /// parameter should not be null.
        /// </param>
        /// <param name="clientData">
        /// The extra, function-specific data supplied for termination, if any.
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

        #region IHavePlugin Members
        /// <summary>
        /// The plugin that provides this function, if any.
        /// </summary>
        private IPlugin plugin;
        /// <summary>
        /// Gets or sets the plugin that provides this function, if any.
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
        /// The associated type name for this function, if any.
        /// </summary>
        private string typeName;
        /// <summary>
        /// Gets or sets the associated type name for this function, if any.
        /// </summary>
        public virtual string TypeName
        {
            get { return typeName; }
            set { typeName = value; }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// The associated type for this function, if any.
        /// </summary>
        private Type type;
        /// <summary>
        /// Gets or sets the associated type for this function, if any.
        /// </summary>
        public virtual Type Type
        {
            get { return type; }
            set { type = value; }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region IFunctionData Members
        /// <summary>
        /// The required number of arguments accepted by this function, or a
        /// value indicating a variable arity.
        /// </summary>
        private int arguments;
        /// <summary>
        /// Gets or sets the required number of arguments accepted by this
        /// function, or a value indicating a variable arity.
        /// </summary>
        public virtual int Arguments
        {
            get { return arguments; }
            set { arguments = value; }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// The list of permitted argument types for this function, if any.
        /// </summary>
        private TypeList types;
        /// <summary>
        /// Gets or sets the list of permitted argument types for this function,
        /// if any.
        /// </summary>
        public virtual TypeList Types
        {
            get { return types; }
            set { types = value; }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// The flags that control the behavior of this function.
        /// </summary>
        private FunctionFlags flags;
        /// <summary>
        /// Gets or sets the flags that control the behavior of this function.
        /// </summary>
        public virtual FunctionFlags Flags
        {
            get { return flags; }
            set { flags = value; }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region IWrapperData Members
        /// <summary>
        /// The interpreter token that identifies this function within its
        /// containing collection.
        /// </summary>
        private long token;
        /// <summary>
        /// Gets or sets the interpreter token that identifies this function
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
        /// This method evaluates the default function.  The default
        /// implementation performs no action and simply returns
        /// <see cref="ReturnCode.Ok" />; derived functions override this method
        /// to provide their actual behavior.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter context this function is executing in.  This
        /// parameter should not be null.
        /// </param>
        /// <param name="clientData">
        /// The extra, function-specific data supplied when this function was
        /// created, if any.  This parameter may be null.
        /// </param>
        /// <param name="arguments">
        /// The list of arguments for this invocation.  Element zero is the
        /// function name; the remaining elements are its arguments.  This
        /// parameter should not be null.
        /// </param>
        /// <param name="value">
        /// Upon success, this may contain the result value produced by the
        /// function.
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
        /// The number of times this function has been executed.
        /// </summary>
        private long usageCount;
        /// <summary>
        /// The total number of microseconds spent executing this function.
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
        /// This method increments the execution count for this function and
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
        /// function, incrementing the execution count and adding the supplied
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
            ref long microseconds /* in, out */
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
