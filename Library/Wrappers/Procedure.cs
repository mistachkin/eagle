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
    [ObjectId("40effd9c-7211-4999-9a32-04392e4387b2")]
    internal sealed class Procedure : Default, IProcedure
    {
        #region Public Constructors
        public Procedure(
            long token,
            IProcedure procedure
            )
            : base(token)
        {
            this.procedure = procedure;
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Private Data
        internal IProcedure procedure;
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region IIdentifierName Members
        public string Name
        {
            get { return (procedure != null) ? procedure.Name : null; }
            set { if (procedure != null) { procedure.Name = value; } }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region IIdentifierBase Members
        public IdentifierKind Kind
        {
            get { return (procedure != null) ? procedure.Kind : IdentifierKind.None; }
            set { if (procedure != null) { procedure.Kind = value; } }
        }

        ///////////////////////////////////////////////////////////////////////

        public Guid Id
        {
            get { return (procedure != null) ? procedure.Id : Guid.Empty; }
            set { if (procedure != null) { procedure.Id = value; } }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region IIdentifier Members
        public string Group
        {
            get { return (procedure != null) ? procedure.Group : null; }
            set { if (procedure != null) { procedure.Group = value; } }
        }

        ///////////////////////////////////////////////////////////////////////

        public string Description
        {
            get { return (procedure != null) ? procedure.Description : null; }
            set { if (procedure != null) { procedure.Description = value; } }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region IGetClientData / ISetClientData Members
        public IClientData ClientData
        {
            get { return (procedure != null) ? procedure.ClientData : null; }
            set { if (procedure != null) { procedure.ClientData = value; } }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region IUsageData Members
        public bool ResetUsage(
            UsageType type,
            ref long value
            )
        {
            return (procedure != null) ?
                procedure.ResetUsage(type, ref value) : false;
        }

        ///////////////////////////////////////////////////////////////////////

        public bool GetUsage(
            UsageType type,
            ref long value
            )
        {
            return (procedure != null) ?
                procedure.GetUsage(type, ref value) : false;
        }

        ///////////////////////////////////////////////////////////////////////

        public bool SetUsage(
            UsageType type,
            ref long value
            )
        {
            return (procedure != null) ?
                procedure.SetUsage(type, ref value) : false;
        }

        ///////////////////////////////////////////////////////////////////////

        public bool AddUsage(
            UsageType type,
            ref long value
            )
        {
            return (procedure != null) ?
                procedure.AddUsage(type, ref value) : false;
        }

        ///////////////////////////////////////////////////////////////////////

        public bool CountUsage(
            ref long count
            )
        {
            return (procedure != null) ?
                procedure.CountUsage(ref count) : false;
        }

        ///////////////////////////////////////////////////////////////////////

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
        public int Levels
        {
            get { return (procedure != null) ? procedure.Levels : 0; }
        }

        ///////////////////////////////////////////////////////////////////////

        public int EnterLevel()
        {
            return (procedure != null) ? procedure.EnterLevel() : 0;
        }

        ///////////////////////////////////////////////////////////////////////

        public int ExitLevel()
        {
            return (procedure != null) ? procedure.ExitLevel() : 0;
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region IProcedureData Members
        public ProcedureFlags Flags
        {
            get { return (procedure != null) ? procedure.Flags : ProcedureFlags.None; }
            set { if (procedure != null) { procedure.Flags = value; } }
        }

        ///////////////////////////////////////////////////////////////////////

        public ArgumentList Arguments
        {
            get { return (procedure != null) ? procedure.Arguments : null; }
            set { if (procedure != null) { procedure.Arguments = value; } }
        }

        ///////////////////////////////////////////////////////////////////////

        public ArgumentDictionary NamedArguments
        {
            get { return (procedure != null) ? procedure.NamedArguments : null; }
            set { if (procedure != null) { procedure.NamedArguments = value; } }
        }

        ///////////////////////////////////////////////////////////////////////

        public string Body
        {
            get { return (procedure != null) ? procedure.Body : null; }
            set { if (procedure != null) { procedure.Body = value; } }
        }

        ///////////////////////////////////////////////////////////////////////

        public IScriptLocation Location
        {
            get { return (procedure != null) ? procedure.Location : null; }
            set { if (procedure != null) { procedure.Location = value; } }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region IDynamicExecuteCallback Members
        public ExecuteCallback Callback
        {
            get { return (procedure != null) ? procedure.Callback : null; }
            set { if (procedure != null) { procedure.Callback = value; } }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region IExecute Members
        public ReturnCode Execute(
            Interpreter interpreter,
            IClientData clientData,
            ArgumentList arguments,
            ref Result result
            )
        {
            if (procedure != null)
                return procedure.Execute(
                    interpreter, clientData, arguments, ref result);
            else
                return ReturnCode.Error;
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region IWrapper Members
        public override bool IsDisposable
        {
            get { return false; }
        }

        ///////////////////////////////////////////////////////////////////////

        public override object Object
        {
            get { return procedure; }
        }
        #endregion
    }
}
