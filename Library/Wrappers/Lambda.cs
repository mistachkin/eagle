/*
 * Lambda.cs --
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
using Eagle._Interfaces.Private;
using Eagle._Interfaces.Public;

namespace Eagle._Wrappers
{
    /// <summary>
    /// This class implements a wrapper around an <see cref="ILambda" />
    /// object, forwarding the lambda interface to the wrapped instance.  It is
    /// used so a lambda can participate in the interpreter as an identifiable,
    /// token-bearing entity.
    /// </summary>
    [ObjectId("fad094ee-bd3a-4e0a-ae90-165bd7a14b26")]
    internal sealed class Lambda : Core, ILambda
    {
        #region Public Constructors
        /// <summary>
        /// Constructs an instance of this wrapper class.
        /// </summary>
        public Lambda()
            : base()
        {
            // do nothing.
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Private Data
        /// <summary>
        /// The wrapped <see cref="ILambda" /> object, or null if none has been
        /// set.
        /// </summary>
        internal ILambda lambda;
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region IIdentifierName Members
        /// <summary>
        /// Gets or sets the name of the wrapped lambda.
        /// </summary>
        public string Name
        {
            get { return (lambda != null) ? lambda.Name : null; }
            set { if (lambda != null) { lambda.Name = value; } }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region IIdentifierBase Members
        /// <summary>
        /// Gets or sets the identifier kind of the wrapped lambda.
        /// </summary>
        public IdentifierKind Kind
        {
            get { return (lambda != null) ? lambda.Kind : IdentifierKind.None; }
            set { if (lambda != null) { lambda.Kind = value; } }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Gets or sets the unique identifier of the wrapped lambda.
        /// </summary>
        public Guid Id
        {
            get { return (lambda != null) ? lambda.Id : Guid.Empty; }
            set { if (lambda != null) { lambda.Id = value; } }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region IIdentifier Members
        /// <summary>
        /// Gets or sets the group of the wrapped lambda.
        /// </summary>
        public string Group
        {
            get { return (lambda != null) ? lambda.Group : null; }
            set { if (lambda != null) { lambda.Group = value; } }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Gets or sets the description of the wrapped lambda.
        /// </summary>
        public string Description
        {
            get { return (lambda != null) ? lambda.Description : null; }
            set { if (lambda != null) { lambda.Description = value; } }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Gets or sets the client data associated with the wrapped lambda.
        /// </summary>
        public IClientData ClientData
        {
            get { return (lambda != null) ? lambda.ClientData : null; }
            set { if (lambda != null) { lambda.ClientData = value; } }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region IUsageData Members
        /// <summary>
        /// This method resets the usage statistics of the wrapped lambda.
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
            return (lambda != null) ?
                lambda.ResetUsage(type, ref value) : false;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method gets the usage statistics of the wrapped lambda.
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
            return (lambda != null) ?
                lambda.GetUsage(type, ref value) : false;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method sets the usage statistics of the wrapped lambda.
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
            return (lambda != null) ?
                lambda.SetUsage(type, ref value) : false;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method adds to the usage statistics of the wrapped lambda.
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
            return (lambda != null) ?
                lambda.AddUsage(type, ref value) : false;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method counts the usage statistics of the wrapped lambda.
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
            return (lambda != null) ?
                lambda.CountUsage(ref count) : false;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method profiles the usage statistics of the wrapped lambda.
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
            return (lambda != null) ?
                lambda.ProfileUsage(ref microseconds) : false;
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region ILevels Members
        /// <summary>
        /// Gets the current level count of the wrapped lambda.
        /// </summary>
        public int Levels
        {
            get { return (lambda != null) ? lambda.Levels : 0; }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method increments and returns the active call level of the
        /// wrapped lambda.
        /// </summary>
        /// <returns>
        /// The resulting level count, or zero when there is no wrapped object.
        /// </returns>
        public int EnterLevel()
        {
            return (lambda != null) ? lambda.EnterLevel() : 0;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method decrements and returns the active call level of the
        /// wrapped lambda.
        /// </summary>
        /// <returns>
        /// The resulting level count, or zero when there is no wrapped object.
        /// </returns>
        public int ExitLevel()
        {
            return (lambda != null) ? lambda.ExitLevel() : 0;
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region IProcedureData Members
        /// <summary>
        /// Gets or sets the procedure flags of the wrapped lambda.
        /// </summary>
        public ProcedureFlags Flags
        {
            get { return (lambda != null) ? lambda.Flags : ProcedureFlags.None; }
            set { if (lambda != null) { lambda.Flags = value; } }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Gets or sets the argument list of the wrapped lambda.
        /// </summary>
        public ArgumentList Arguments
        {
            get { return (lambda != null) ? lambda.Arguments : null; }
            set { if (lambda != null) { lambda.Arguments = value; } }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Gets or sets the named argument dictionary of the wrapped lambda.
        /// </summary>
        public ArgumentDictionary NamedArguments
        {
            get { return (lambda != null) ? lambda.NamedArguments : null; }
            set { if (lambda != null) { lambda.NamedArguments = value; } }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Gets or sets the overwrite argument list of the wrapped lambda.
        /// </summary>
        public ArgumentList OverwriteArguments
        {
            get { return (lambda != null) ? lambda.OverwriteArguments : null; }
            set { if (lambda != null) { lambda.OverwriteArguments = value; } }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Gets or sets the clean argument list of the wrapped lambda.
        /// </summary>
        public ArgumentList CleanArguments
        {
            get { return (lambda != null) ? lambda.CleanArguments : null; }
            set { if (lambda != null) { lambda.CleanArguments = value; } }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Gets or sets the body of the wrapped lambda.
        /// </summary>
        public string Body
        {
            get { return (lambda != null) ? lambda.Body : null; }
            set { if (lambda != null) { lambda.Body = value; } }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Gets or sets the script location of the wrapped lambda.
        /// </summary>
        public IScriptLocation Location
        {
            get { return (lambda != null) ? lambda.Location : null; }
            set { if (lambda != null) { lambda.Location = value; } }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region IDynamicExecuteCallback Members
        /// <summary>
        /// Gets or sets the execute callback of the wrapped lambda.
        /// </summary>
        public ExecuteCallback Callback
        {
            get { return (lambda != null) ? lambda.Callback : null; }
            set { if (lambda != null) { lambda.Callback = value; } }
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
        /// Gets or sets the underlying <see cref="ILambda" /> object wrapped by
        /// this instance.
        /// </summary>
        public override object Object
        {
            get { return lambda; }
            set
            {
                lambda = (ILambda)value; /* throw */
                execute = (IExecute)value; /* throw */
            }
        }
        #endregion
    }
}
