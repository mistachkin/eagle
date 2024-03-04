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
    [ObjectId("a17c4ff4-33b1-4c97-8f19-e28e1bc800ee")]
    internal sealed class Operator : Default, IOperator
    {
        #region Public Constructors
        public Operator(
            long token,
            IOperator @operator
            )
            : base(token)
        {
            this.@operator = @operator;
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Private Data
        internal IOperator @operator;
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region IIdentifierName Members
        public string Name
        {
            get { return (@operator != null) ? @operator.Name : null; }
            set { if (@operator != null) { @operator.Name = value; } }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region IIdentifierBase Members
        public IdentifierKind Kind
        {
            get { return (@operator != null) ? @operator.Kind : IdentifierKind.None; }
            set { if (@operator != null) { @operator.Kind = value; } }
        }

        ///////////////////////////////////////////////////////////////////////

        public Guid Id
        {
            get { return (@operator != null) ? @operator.Id : Guid.Empty; }
            set { if (@operator != null) { @operator.Id = value; } }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region IGetClientData / ISetClientData Members
        public IClientData ClientData
        {
            get { return (@operator != null) ? @operator.ClientData : null; }
            set { if (@operator != null) { @operator.ClientData = value; } }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region IIdentifier Members
        public string Group
        {
            get { return (@operator != null) ? @operator.Group : null; }
            set { if (@operator != null) { @operator.Group = value; } }
        }

        ///////////////////////////////////////////////////////////////////////

        public string Description
        {
            get { return (@operator != null) ? @operator.Description : null; }
            set { if (@operator != null) { @operator.Description = value; } }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region IState Members
        public bool Initialized
        {
            get { return (@operator != null) ? @operator.Initialized : false; }
            set { if (@operator != null) { @operator.Initialized = value; } }
        }

        ///////////////////////////////////////////////////////////////////////

        public ReturnCode Initialize(
            Interpreter interpreter,
            IClientData clientData,
            ref Result result
            )
        {
            if (@operator != null)
                return @operator.Initialize(interpreter, clientData, ref result);
            else
                return ReturnCode.Error;
        }

        ///////////////////////////////////////////////////////////////////////

        public ReturnCode Terminate(
            Interpreter interpreter,
            IClientData clientData,
            ref Result result
            )
        {
            if (@operator != null)
                return @operator.Terminate(interpreter, clientData, ref result);
            else
                return ReturnCode.Error;
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region IHavePlugin Members
        public IPlugin Plugin
        {
            get { return (@operator != null) ? @operator.Plugin : null; }
            set { if (@operator != null) { @operator.Plugin = value; } }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region ITypeAndName Members
        public string TypeName
        {
            get { return (@operator != null) ? @operator.TypeName : null; }
            set { if (@operator != null) { @operator.TypeName = value; } }
        }

        ///////////////////////////////////////////////////////////////////////

        public Type Type
        {
            get { return (@operator != null) ? @operator.Type : null; }
            set { if (@operator != null) { @operator.Type = value; } }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region IOperatorData Members
        public Lexeme Lexeme
        {
            get { return (@operator != null) ? @operator.Lexeme : Lexeme.Unknown; }
            set { if (@operator != null) { @operator.Lexeme = value; } }
        }

        ///////////////////////////////////////////////////////////////////////

        public int Operands
        {
            get { return (@operator != null) ? @operator.Operands : 0; }
            set { if (@operator != null) { @operator.Operands = value; } }
        }

        ///////////////////////////////////////////////////////////////////////

        public TypeList Types
        {
            get { return (@operator != null) ? @operator.Types : null; }
            set { if (@operator != null) { @operator.Types = value; } }
        }

        ///////////////////////////////////////////////////////////////////////

        public OperatorFlags Flags
        {
            get { return (@operator != null) ? @operator.Flags : OperatorFlags.None; }
            set { if (@operator != null) { @operator.Flags = value; } }
        }

        ///////////////////////////////////////////////////////////////////////

        public StringComparison ComparisonType
        {
            get { return (@operator != null) ? @operator.ComparisonType : StringComparison.CurrentCulture; }
            set { if (@operator != null) { @operator.ComparisonType = value; } }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region IExecuteArgument Members
        public ReturnCode Execute(
            Interpreter interpreter,
            IClientData clientData,
            ArgumentList arguments,
            ref Argument value,
            ref Result error
            )
        {
            if (@operator != null)
                return @operator.Execute(
                    interpreter, clientData, arguments, ref value, ref error);
            else
                return ReturnCode.Error;
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region IUsageData Members
        public bool ResetUsage(
            UsageType type,
            ref long value
            )
        {
            return (@operator != null) ?
                @operator.ResetUsage(type, ref value) : false;
        }

        ///////////////////////////////////////////////////////////////////////

        public bool GetUsage(
            UsageType type,
            ref long value
            )
        {
            return (@operator != null) ?
                @operator.GetUsage(type, ref value) : false;
        }

        ///////////////////////////////////////////////////////////////////////

        public bool SetUsage(
            UsageType type,
            ref long value
            )
        {
            return (@operator != null) ?
                @operator.SetUsage(type, ref value) : false;
        }

        ///////////////////////////////////////////////////////////////////////

        public bool AddUsage(
            UsageType type,
            ref long value
            )
        {
            return (@operator != null) ?
                @operator.AddUsage(type, ref value) : false;
        }

        ///////////////////////////////////////////////////////////////////////

        public bool CountUsage(
            ref long count
            )
        {
            return (@operator != null) ?
                @operator.CountUsage(ref count) : false;
        }

        ///////////////////////////////////////////////////////////////////////

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
        public override bool IsDisposable
        {
            get { return false; }
        }

        ///////////////////////////////////////////////////////////////////////

        public override object Object
        {
            get { return @operator; }
        }
        #endregion
    }
}
