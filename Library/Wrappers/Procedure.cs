/*
 * Procedure.cs --
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
    /// This class wraps an <see cref="IProcedure" /> instance, forwarding all
    /// member access to the wrapped target while gracefully handling the case
    /// where no target has been set (by returning default values or an error
    /// result).
    /// </summary>
    [ObjectId("40effd9c-7211-4999-9a32-04392e4387b2")]
    internal sealed class Procedure : Core, IProcedure
    {
        #region Public Constructors
        /// <summary>
        /// Constructs an instance of this class with no wrapped procedure
        /// target.
        /// </summary>
        public Procedure()
            : base()
        {
            // do nothing.
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Private Data
        /// <summary>
        /// The wrapped procedure instance that all members forward to.
        /// </summary>
        internal IProcedure procedure;
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region IIdentifierName Members
        /// <summary>
        /// Gets or sets the name of the wrapped procedure.
        /// </summary>
        public string Name
        {
            get { return (procedure != null) ? procedure.Name : null; }
            set { if (procedure != null) { procedure.Name = value; } }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region IIdentifierBase Members
        /// <summary>
        /// Gets or sets the identifier kind of the wrapped procedure.
        /// </summary>
        public IdentifierKind Kind
        {
            get { return (procedure != null) ? procedure.Kind : IdentifierKind.None; }
            set { if (procedure != null) { procedure.Kind = value; } }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Gets or sets the unique identifier of the wrapped procedure.
        /// </summary>
        public Guid Id
        {
            get { return (procedure != null) ? procedure.Id : Guid.Empty; }
            set { if (procedure != null) { procedure.Id = value; } }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region IGetClientData / ISetClientData Members
        /// <summary>
        /// Gets or sets the client data associated with the wrapped procedure.
        /// </summary>
        public IClientData ClientData
        {
            get { return (procedure != null) ? procedure.ClientData : null; }
            set { if (procedure != null) { procedure.ClientData = value; } }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region IIdentifier Members
        /// <summary>
        /// Gets or sets the group of the wrapped procedure.
        /// </summary>
        public string Group
        {
            get { return (procedure != null) ? procedure.Group : null; }
            set { if (procedure != null) { procedure.Group = value; } }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Gets or sets the description of the wrapped procedure.
        /// </summary>
        public string Description
        {
            get { return (procedure != null) ? procedure.Description : null; }
            set { if (procedure != null) { procedure.Description = value; } }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region IUsageData Members
        /// <summary>
        /// This method resets a usage statistic of the wrapped procedure.
        /// </summary>
        /// <param name="type">
        /// The kind of usage statistic to reset.
        /// </param>
        /// <param name="value">
        /// Upon success, receives the previous value of the usage statistic.
        /// </param>
        /// <returns>
        /// True if the usage statistic was reset; otherwise, false.
        /// </returns>
        public bool ResetUsage(
            UsageType type,
            ref long value
            )
        {
            return (procedure != null) ?
                procedure.ResetUsage(type, ref value) : false;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method gets a usage statistic of the wrapped procedure.
        /// </summary>
        /// <param name="type">
        /// The kind of usage statistic to get.
        /// </param>
        /// <param name="value">
        /// Upon success, receives the current value of the usage statistic.
        /// </param>
        /// <returns>
        /// True if the usage statistic was retrieved; otherwise, false.
        /// </returns>
        public bool GetUsage(
            UsageType type,
            ref long value
            )
        {
            return (procedure != null) ?
                procedure.GetUsage(type, ref value) : false;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method sets a usage statistic of the wrapped procedure.
        /// </summary>
        /// <param name="type">
        /// The kind of usage statistic to set.
        /// </param>
        /// <param name="value">
        /// The new value for the usage statistic.
        /// </param>
        /// <returns>
        /// True if the usage statistic was set; otherwise, false.
        /// </returns>
        public bool SetUsage(
            UsageType type,
            ref long value
            )
        {
            return (procedure != null) ?
                procedure.SetUsage(type, ref value) : false;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method adds to a usage statistic of the wrapped procedure.
        /// </summary>
        /// <param name="type">
        /// The kind of usage statistic to add to.
        /// </param>
        /// <param name="value">
        /// On input, the amount to add; upon success, receives the resulting
        /// value of the usage statistic.
        /// </param>
        /// <returns>
        /// True if the usage statistic was updated; otherwise, false.
        /// </returns>
        public bool AddUsage(
            UsageType type,
            ref long value
            )
        {
            return (procedure != null) ?
                procedure.AddUsage(type, ref value) : false;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method gets the usage count of the wrapped procedure.
        /// </summary>
        /// <param name="count">
        /// Upon success, receives the usage count.
        /// </param>
        /// <returns>
        /// True if the usage count was retrieved; otherwise, false.
        /// </returns>
        public bool CountUsage(
            ref long count
            )
        {
            return (procedure != null) ?
                procedure.CountUsage(ref count) : false;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method gets the profiled usage time of the wrapped procedure.
        /// </summary>
        /// <param name="microseconds">
        /// Upon success, receives the profiled usage time, in microseconds.
        /// </param>
        /// <returns>
        /// True if the profiled usage time was retrieved; otherwise, false.
        /// </returns>
        public bool ProfileUsage(
            ref long microseconds
            )
        {
            return (procedure != null) ?
                procedure.ProfileUsage(ref microseconds) : false;
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region ILevels Members
        /// <summary>
        /// Gets the current active call level of the wrapped procedure.
        /// </summary>
        public int Levels
        {
            get { return (procedure != null) ? procedure.Levels : 0; }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method enters a new active call level of the wrapped procedure.
        /// </summary>
        /// <returns>
        /// The resulting active call level.
        /// </returns>
        public int EnterLevel()
        {
            return (procedure != null) ? procedure.EnterLevel() : 0;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method exits an active call level of the wrapped procedure.
        /// </summary>
        /// <returns>
        /// The resulting active call level.
        /// </returns>
        public int ExitLevel()
        {
            return (procedure != null) ? procedure.ExitLevel() : 0;
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region IProcedureData Members
        /// <summary>
        /// Gets or sets the flags associated with the wrapped procedure.
        /// </summary>
        public ProcedureFlags Flags
        {
            get { return (procedure != null) ? procedure.Flags : ProcedureFlags.None; }
            set { if (procedure != null) { procedure.Flags = value; } }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Gets or sets the formal argument list of the wrapped procedure.
        /// </summary>
        public ArgumentList Arguments
        {
            get { return (procedure != null) ? procedure.Arguments : null; }
            set { if (procedure != null) { procedure.Arguments = value; } }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Gets or sets the named arguments of the wrapped procedure.
        /// </summary>
        public ArgumentDictionary NamedArguments
        {
            get { return (procedure != null) ? procedure.NamedArguments : null; }
            set { if (procedure != null) { procedure.NamedArguments = value; } }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Gets or sets the overwrite arguments of the wrapped procedure.
        /// </summary>
        public ArgumentList OverwriteArguments
        {
            get { return (procedure != null) ? procedure.OverwriteArguments : null; }
            set { if (procedure != null) { procedure.OverwriteArguments = value; } }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Gets or sets the clean arguments of the wrapped procedure.
        /// </summary>
        public ArgumentList CleanArguments
        {
            get { return (procedure != null) ? procedure.CleanArguments : null; }
            set { if (procedure != null) { procedure.CleanArguments = value; } }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Gets or sets the body script of the wrapped procedure.
        /// </summary>
        public string Body
        {
            get { return (procedure != null) ? procedure.Body : null; }
            set { if (procedure != null) { procedure.Body = value; } }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Gets or sets the script location of the wrapped procedure.
        /// </summary>
        public IScriptLocation Location
        {
            get { return (procedure != null) ? procedure.Location : null; }
            set { if (procedure != null) { procedure.Location = value; } }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region IDynamicExecuteCallback Members
        /// <summary>
        /// Gets or sets the execute callback of the wrapped procedure.
        /// </summary>
        public ExecuteCallback Callback
        {
            get { return (procedure != null) ? procedure.Callback : null; }
            set { if (procedure != null) { procedure.Callback = value; } }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region IWrapper Members
        /// <summary>
        /// Gets a value indicating whether the wrapped object is disposable;
        /// always false for this wrapper.
        /// </summary>
        public override bool IsDisposable
        {
            get { return false; }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Gets or sets the wrapped object.  The value must implement both
        /// <see cref="IProcedure" /> and <see cref="IExecute" />; otherwise,
        /// setting it throws.
        /// </summary>
        public override object Object
        {
            get { return procedure; }
            set
            {
                procedure = (IProcedure)value; /* throw */
                execute = (IExecute)value; /* throw */
            }
        }
        #endregion
    }
}
