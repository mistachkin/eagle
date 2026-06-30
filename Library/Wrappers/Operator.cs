/*
 * Operator.cs --
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
using Eagle._Containers.Public;
using Eagle._Interfaces.Private;
using Eagle._Interfaces.Public;

namespace Eagle._Wrappers
{
    /// <summary>
    /// This class implements a wrapper around an <see cref="IOperator" />
    /// object, forwarding the operator interface to the wrapped instance.  It
    /// is used so an operator can participate in the interpreter as an
    /// identifiable, token-bearing entity.
    /// </summary>
    [ObjectId("a17c4ff4-33b1-4c97-8f19-e28e1bc800ee")]
    internal sealed class Operator : Default, IOperator
    {
        #region Public Constructors
        /// <summary>
        /// Constructs an instance of this wrapper class.
        /// </summary>
        public Operator()
            : base()
        {
            // do nothing.
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Private Data
        /// <summary>
        /// The wrapped <see cref="IOperator" /> object, or null if none has
        /// been set.
        /// </summary>
        internal IOperator @operator;
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region IIdentifierName Members
        /// <summary>
        /// Gets or sets the name of the wrapped operator.
        /// </summary>
        public string Name
        {
            get { return (@operator != null) ? @operator.Name : null; }
            set { if (@operator != null) { @operator.Name = value; } }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region IIdentifierBase Members
        /// <summary>
        /// Gets or sets the identifier kind of the wrapped operator.
        /// </summary>
        public IdentifierKind Kind
        {
            get { return (@operator != null) ? @operator.Kind : IdentifierKind.None; }
            set { if (@operator != null) { @operator.Kind = value; } }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Gets or sets the unique identifier of the wrapped operator.
        /// </summary>
        public Guid Id
        {
            get { return (@operator != null) ? @operator.Id : Guid.Empty; }
            set { if (@operator != null) { @operator.Id = value; } }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region IGetClientData / ISetClientData Members
        /// <summary>
        /// Gets or sets the client data associated with the wrapped operator.
        /// </summary>
        public IClientData ClientData
        {
            get { return (@operator != null) ? @operator.ClientData : null; }
            set { if (@operator != null) { @operator.ClientData = value; } }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region IIdentifier Members
        /// <summary>
        /// Gets or sets the group of the wrapped operator.
        /// </summary>
        public string Group
        {
            get { return (@operator != null) ? @operator.Group : null; }
            set { if (@operator != null) { @operator.Group = value; } }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Gets or sets the description of the wrapped operator.
        /// </summary>
        public string Description
        {
            get { return (@operator != null) ? @operator.Description : null; }
            set { if (@operator != null) { @operator.Description = value; } }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region IState Members
        /// <summary>
        /// Gets or sets the initialized state of the wrapped operator.
        /// </summary>
        public bool Initialized
        {
            get { return (@operator != null) ? @operator.Initialized : false; }
            set { if (@operator != null) { @operator.Initialized = value; } }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method forwards the initialize operation to the wrapped
        /// operator.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter context this operator is executing in.  This
        /// parameter should not be null.
        /// </param>
        /// <param name="clientData">
        /// The extra, operator-specific data supplied when this operator was
        /// created, if any.  This parameter may be null.
        /// </param>
        /// <param name="result">
        /// Upon success, this contains the result; upon failure, it contains
        /// an appropriate error message.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise,
        /// <see cref="ReturnCode.Error" />, including when there is no wrapped
        /// object.
        /// </returns>
        public ReturnCode Initialize(
            Interpreter interpreter,
            IClientData clientData,
            ref Result result
            )
        {
            if (@operator == null)
            {
                result = "invalid wrapper target";
                return ReturnCode.Error;
            }

            return @operator.Initialize(interpreter, clientData, ref result);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method forwards the terminate operation to the wrapped
        /// operator.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter context this operator is executing in.  This
        /// parameter should not be null.
        /// </param>
        /// <param name="clientData">
        /// The extra, operator-specific data supplied when this operator was
        /// created, if any.  This parameter may be null.
        /// </param>
        /// <param name="result">
        /// Upon success, this contains the result; upon failure, it contains
        /// an appropriate error message.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise,
        /// <see cref="ReturnCode.Error" />, including when there is no wrapped
        /// object.
        /// </returns>
        public ReturnCode Terminate(
            Interpreter interpreter,
            IClientData clientData,
            ref Result result
            )
        {
            if (@operator == null)
            {
                result = "invalid wrapper target";
                return ReturnCode.Error;
            }

            return @operator.Terminate(interpreter, clientData, ref result);
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region IHavePlugin Members
        /// <summary>
        /// Gets or sets the plugin of the wrapped operator.
        /// </summary>
        public IPlugin Plugin
        {
            get { return (@operator != null) ? @operator.Plugin : null; }
            set { if (@operator != null) { @operator.Plugin = value; } }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region ITypeAndName Members
        /// <summary>
        /// Gets or sets the type name of the wrapped operator.
        /// </summary>
        public string TypeName
        {
            get { return (@operator != null) ? @operator.TypeName : null; }
            set { if (@operator != null) { @operator.TypeName = value; } }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Gets or sets the type of the wrapped operator.
        /// </summary>
        public Type Type
        {
            get { return (@operator != null) ? @operator.Type : null; }
            set { if (@operator != null) { @operator.Type = value; } }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region IOperatorData Members
        /// <summary>
        /// Gets or sets the lexeme of the wrapped operator.
        /// </summary>
        public Lexeme Lexeme
        {
            get { return (@operator != null) ? @operator.Lexeme : Lexeme.Unknown; }
            set { if (@operator != null) { @operator.Lexeme = value; } }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Gets or sets the operand count of the wrapped operator.
        /// </summary>
        public int Operands
        {
            get { return (@operator != null) ? @operator.Operands : 0; }
            set { if (@operator != null) { @operator.Operands = value; } }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Gets or sets the operand type list of the wrapped operator.
        /// </summary>
        public TypeList Types
        {
            get { return (@operator != null) ? @operator.Types : null; }
            set { if (@operator != null) { @operator.Types = value; } }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Gets or sets the operator flags of the wrapped operator.
        /// </summary>
        public OperatorFlags Flags
        {
            get { return (@operator != null) ? @operator.Flags : OperatorFlags.None; }
            set { if (@operator != null) { @operator.Flags = value; } }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Gets or sets the string comparison type of the wrapped operator.
        /// </summary>
        public StringComparison ComparisonType
        {
            get { return (@operator != null) ? @operator.ComparisonType : StringComparison.CurrentCulture; }
            set { if (@operator != null) { @operator.ComparisonType = value; } }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region IExecuteArgument Members
        /// <summary>
        /// This method forwards the execute operation to the wrapped operator.
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
        /// The list of arguments for this invocation.  This parameter should
        /// not be null.
        /// </param>
        /// <param name="value">
        /// Upon success, this is set to the result of the operator.
        /// </param>
        /// <param name="error">
        /// Upon failure, this contains an appropriate error message.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise,
        /// <see cref="ReturnCode.Error" />, including when there is no wrapped
        /// object.
        /// </returns>
        public ReturnCode Execute(
            Interpreter interpreter,
            IClientData clientData,
            ArgumentList arguments,
            ref Argument value,
            ref Result error
            )
        {
            if (@operator == null)
            {
                error = "invalid wrapper target";
                return ReturnCode.Error;
            }

            return @operator.Execute(
                interpreter, clientData, arguments, ref value, ref error);
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region IUsageData Members
        /// <summary>
        /// This method resets the usage statistics of the wrapped operator.
        /// </summary>
        /// <param name="type">
        /// The kind of usage statistic being accessed.
        /// </param>
        /// <param name="value">
        /// Upon success, this is set to the relevant usage value.
        /// </param>
        /// <returns>
        /// True if the operation succeeded; otherwise, false.
        /// </returns>
        public bool ResetUsage(
            UsageType type,
            ref long value
            )
        {
            return (@operator != null) ?
                @operator.ResetUsage(type, ref value) : false;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method gets the usage statistics of the wrapped operator.
        /// </summary>
        /// <param name="type">
        /// The kind of usage statistic being accessed.
        /// </param>
        /// <param name="value">
        /// Upon success, this is set to the relevant usage value.
        /// </param>
        /// <returns>
        /// True if the operation succeeded; otherwise, false.
        /// </returns>
        public bool GetUsage(
            UsageType type,
            ref long value
            )
        {
            return (@operator != null) ?
                @operator.GetUsage(type, ref value) : false;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method sets the usage statistics of the wrapped operator.
        /// </summary>
        /// <param name="type">
        /// The kind of usage statistic being accessed.
        /// </param>
        /// <param name="value">
        /// Upon success, this is set to the relevant usage value.
        /// </param>
        /// <returns>
        /// True if the operation succeeded; otherwise, false.
        /// </returns>
        public bool SetUsage(
            UsageType type,
            ref long value
            )
        {
            return (@operator != null) ?
                @operator.SetUsage(type, ref value) : false;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method adds to the usage statistics of the wrapped operator.
        /// </summary>
        /// <param name="type">
        /// The kind of usage statistic being accessed.
        /// </param>
        /// <param name="value">
        /// Upon success, this is set to the relevant usage value.
        /// </param>
        /// <returns>
        /// True if the operation succeeded; otherwise, false.
        /// </returns>
        public bool AddUsage(
            UsageType type,
            ref long value
            )
        {
            return (@operator != null) ?
                @operator.AddUsage(type, ref value) : false;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method counts the usage statistics of the wrapped operator.
        /// </summary>
        /// <param name="count">
        /// Upon success, this is set to the usage count.
        /// </param>
        /// <returns>
        /// True if the operation succeeded; otherwise, false.
        /// </returns>
        public bool CountUsage(
            ref long count
            )
        {
            return (@operator != null) ?
                @operator.CountUsage(ref count) : false;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method profiles the usage statistics of the wrapped operator.
        /// </summary>
        /// <param name="microseconds">
        /// Upon success, this is set to the elapsed microseconds.
        /// </param>
        /// <returns>
        /// True if the operation succeeded; otherwise, false.
        /// </returns>
        public bool ProfileUsage(
            ref long microseconds
            )
        {
            return (@operator != null) ?
                @operator.ProfileUsage(ref microseconds) : false;
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region IWrapper Members
        /// <summary>
        /// Gets a value indicating whether the object wrapped by this instance
        /// represents a resource that requires disposal.
        /// </summary>
        public override bool IsDisposable
        {
            get { return false; }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Gets or sets the underlying <see cref="IOperator" /> object wrapped
        /// by this instance.
        /// </summary>
        public override object Object
        {
            get { return @operator; }
            set { @operator = (IOperator)value; } /* throw */
        }
        #endregion
    }
}
