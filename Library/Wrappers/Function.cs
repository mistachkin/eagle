/*
 * Function.cs --
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
using Eagle._Interfaces.Public;

namespace Eagle._Wrappers
{
    /// <summary>
    /// This class implements a wrapper around an <see cref="IFunction" />
    /// object, forwarding the function interface to the wrapped instance.  It
    /// is used so a function can participate in the interpreter as an
    /// identifiable, token-bearing entity.
    /// </summary>
    [ObjectId("3b367479-8554-46ee-9d62-6c366e153ee4")]
    internal sealed class Function : Default, IFunction
    {
        #region Public Constructors
        /// <summary>
        /// Constructs an instance of this wrapper class.
        /// </summary>
        public Function()
            : base()
        {
            // do nothing.
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Private Data
        /// <summary>
        /// The wrapped <see cref="IFunction" /> object, or null if none has
        /// been set.
        /// </summary>
        internal IFunction function;
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region IIdentifierName Members
        /// <summary>
        /// Gets or sets the name of the wrapped function.
        /// </summary>
        public string Name
        {
            get { return (function != null) ? function.Name : null; }
            set { if (function != null) { function.Name = value; } }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region IIdentifierBase Members
        /// <summary>
        /// Gets or sets the identifier kind of the wrapped function.
        /// </summary>
        public IdentifierKind Kind
        {
            get { return (function != null) ? function.Kind : IdentifierKind.None; }
            set { if (function != null) { function.Kind = value; } }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Gets or sets the unique identifier of the wrapped function.
        /// </summary>
        public Guid Id
        {
            get { return (function != null) ? function.Id : Guid.Empty; }
            set { if (function != null) { function.Id = value; } }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region IGetClientData / ISetClientData Members
        /// <summary>
        /// Gets or sets the client data associated with the wrapped function.
        /// </summary>
        public IClientData ClientData
        {
            get { return (function != null) ? function.ClientData : null; }
            set { if (function != null) { function.ClientData = value; } }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region IIdentifier Members
        /// <summary>
        /// Gets or sets the group of the wrapped function.
        /// </summary>
        public string Group
        {
            get { return (function != null) ? function.Group : null; }
            set { if (function != null) { function.Group = value; } }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Gets or sets the description of the wrapped function.
        /// </summary>
        public string Description
        {
            get { return (function != null) ? function.Description : null; }
            set { if (function != null) { function.Description = value; } }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region IState Members
        /// <summary>
        /// Gets or sets the initialized state of the wrapped function.
        /// </summary>
        public bool Initialized
        {
            get { return (function != null) ? function.Initialized : false; }
            set { if (function != null) { function.Initialized = value; } }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method forwards the initialize operation to the wrapped
        /// function.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter context this function is executing in.  This
        /// parameter should not be null.
        /// </param>
        /// <param name="clientData">
        /// The extra, function-specific data supplied when this function was
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
            if (function == null)
            {
                result = "invalid wrapper target";
                return ReturnCode.Error;
            }

            return function.Initialize(interpreter, clientData, ref result);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method forwards the terminate operation to the wrapped
        /// function.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter context this function is executing in.  This
        /// parameter should not be null.
        /// </param>
        /// <param name="clientData">
        /// The extra, function-specific data supplied when this function was
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
            if (function == null)
            {
                result = "invalid wrapper target";
                return ReturnCode.Error;
            }

            return function.Terminate(interpreter, clientData, ref result);
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region IHavePlugin Members
        /// <summary>
        /// Gets or sets the plugin of the wrapped function.
        /// </summary>
        public IPlugin Plugin
        {
            get { return (function != null) ? function.Plugin : null; }
            set { if (function != null) { function.Plugin = value; } }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region ITypeAndName Members
        /// <summary>
        /// Gets or sets the type name of the wrapped function.
        /// </summary>
        public string TypeName
        {
            get { return (function != null) ? function.TypeName : null; }
            set { if (function != null) { function.TypeName = value; } }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Gets or sets the type of the wrapped function.
        /// </summary>
        public Type Type
        {
            get { return (function != null) ? function.Type : null; }
            set { if (function != null) { function.Type = value; } }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region IFunctionData Members
        /// <summary>
        /// Gets or sets the required argument count of the wrapped function.
        /// </summary>
        public int Arguments
        {
            get { return (function != null) ? function.Arguments : 0; }
            set { if (function != null) { function.Arguments = value; } }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Gets or sets the argument type list of the wrapped function.
        /// </summary>
        public TypeList Types
        {
            get { return (function != null) ? function.Types : null; }
            set { if (function != null) { function.Types = value; } }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Gets or sets the function flags of the wrapped function.
        /// </summary>
        public FunctionFlags Flags
        {
            get { return (function != null) ? function.Flags : FunctionFlags.None; }
            set { if (function != null) { function.Flags = value; } }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region IExecuteArgument Members
        /// <summary>
        /// This method forwards the execute operation to the wrapped function.
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
        /// The list of arguments for this invocation.  This parameter should
        /// not be null.
        /// </param>
        /// <param name="value">
        /// Upon success, this is set to the result of the function.
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
            if (function == null)
            {
                error = "invalid wrapper target";
                return ReturnCode.Error;
            }

            return function.Execute(
                interpreter, clientData, arguments, ref value, ref error);
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region IUsageData Members
        /// <summary>
        /// This method resets the usage statistics of the wrapped function.
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
            return (function != null) ?
                function.ResetUsage(type, ref value) : false;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method gets the usage statistics of the wrapped function.
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
            return (function != null) ?
                function.GetUsage(type, ref value) : false;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method sets the usage statistics of the wrapped function.
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
            return (function != null) ?
                function.SetUsage(type, ref value) : false;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method adds to the usage statistics of the wrapped function.
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
            return (function != null) ?
                function.AddUsage(type, ref value) : false;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method counts the usage statistics of the wrapped function.
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
            return (function != null) ?
                function.CountUsage(ref count) : false;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method profiles the usage statistics of the wrapped function.
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
            return (function != null) ?
                function.ProfileUsage(ref microseconds) : false;
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
        /// Gets or sets the underlying <see cref="IFunction" /> object wrapped
        /// by this instance.
        /// </summary>
        public override object Object
        {
            get { return function; }
            set { function = (IFunction)value; } /* throw */
        }
        #endregion
    }
}
