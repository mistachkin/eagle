/*
 * Alias.cs --
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
    [ObjectId("e61e1b49-7b3d-4eab-9b72-7eeaba4f79dd")]
    internal sealed class Alias : Default, IAlias
    {
        #region Public Constructors
        public Alias(
            long token,
            IAlias alias
            )
            : base(token)
        {
            this.alias = alias;
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Private Data
        internal IAlias alias;
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region IIdentifierName Members
        public string Name
        {
            get { return (alias != null) ? alias.Name : null; }
            set { if (alias != null) { alias.Name = value; } }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region IIdentifierBase Members
        public IdentifierKind Kind
        {
            get { return (alias != null) ? alias.Kind : IdentifierKind.None; }
            set { if (alias != null) { alias.Kind = value; } }
        }

        ///////////////////////////////////////////////////////////////////////

        public Guid Id
        {
            get { return (alias != null) ? alias.Id : Guid.Empty; }
            set { if (alias != null) { alias.Id = value; } }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region IGetClientData / ISetClientData Members
        public IClientData ClientData
        {
            get { return (alias != null) ? alias.ClientData : null; }
            set { if (alias != null) { alias.ClientData = value; } }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region IIdentifier Members
        public string Group
        {
            get { return (alias != null) ? alias.Group : null; }
            set { if (alias != null) { alias.Group = value; } }
        }

        ///////////////////////////////////////////////////////////////////////

        public string Description
        {
            get { return (alias != null) ? alias.Description : null; }
            set { if (alias != null) { alias.Description = value; } }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region IAliasData Members
        public string NameToken
        {
            get { return (alias != null) ? alias.NameToken : null; }
            set { if (alias != null) { alias.NameToken = value; } }
        }

        ///////////////////////////////////////////////////////////////////////

        public Interpreter SourceInterpreter
        {
            get { return (alias != null) ? alias.SourceInterpreter : null; }
            set { if (alias != null) { alias.SourceInterpreter = value; } }
        }

        ///////////////////////////////////////////////////////////////////////

        public Interpreter TargetInterpreter
        {
            get { return (alias != null) ? alias.TargetInterpreter : null; }
            set { if (alias != null) { alias.TargetInterpreter = value; } }
        }

        ///////////////////////////////////////////////////////////////////////

        public INamespace SourceNamespace
        {
            get { return (alias != null) ? alias.SourceNamespace : null; }
            set { if (alias != null) { alias.SourceNamespace = value; } }
        }

        ///////////////////////////////////////////////////////////////////////

        public INamespace TargetNamespace
        {
            get { return (alias != null) ? alias.TargetNamespace : null; }
            set { if (alias != null) { alias.TargetNamespace = value; } }
        }

        ///////////////////////////////////////////////////////////////////////

        public IExecute Target
        {
            get { return (alias != null) ? alias.Target : null; }
            set { if (alias != null) { alias.Target = value; } }
        }

        ///////////////////////////////////////////////////////////////////////

        public ArgumentList Arguments
        {
            get { return (alias != null) ? alias.Arguments : null; }
            set { if (alias != null) { alias.Arguments = value; } }
        }

        ///////////////////////////////////////////////////////////////////////

        public OptionDictionary Options
        {
            get { return (alias != null) ? alias.Options : null; }
            set { if (alias != null) { alias.Options = value; } }
        }

        ///////////////////////////////////////////////////////////////////////

        public AliasFlags AliasFlags
        {
            get { return (alias != null) ? alias.AliasFlags : AliasFlags.None; }
            set { if (alias != null) { alias.AliasFlags = value; } }
        }

        ///////////////////////////////////////////////////////////////////////

        public int StartIndex
        {
            get { return (alias != null) ? alias.StartIndex : 0; }
            set { if (alias != null) { alias.StartIndex = value; } }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region IAlias Members
        public DisposeCallback PostInterpreterDisposed
        {
            get { return (alias != null) ? alias.PostInterpreterDisposed : null; }
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
            get { return alias; }
        }
        #endregion
    }
}
