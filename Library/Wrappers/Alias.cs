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
    /// <summary>
    /// This class implements a wrapper around an <see cref="IAlias" />
    /// object.  It forwards every member of the interface to the wrapped alias
    /// when one is present, gracefully returning default values when one is
    /// not.
    /// </summary>
    [ObjectId("e61e1b49-7b3d-4eab-9b72-7eeaba4f79dd")]
    internal sealed class Alias : Default, IAlias
    {
        #region Public Constructors
        /// <summary>
        /// Constructs an instance of this class with no wrapped alias.
        /// </summary>
        public Alias()
            : base()
        {
            // do nothing.
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Private Data
        /// <summary>
        /// The wrapped alias that this wrapper forwards to.
        /// </summary>
        internal IAlias alias;
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region IIdentifierName Members
        /// <summary>
        /// Gets or sets the name of the wrapped alias.
        /// </summary>
        public string Name
        {
            get { return (alias != null) ? alias.Name : null; }
            set { if (alias != null) { alias.Name = value; } }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region IIdentifierBase Members
        /// <summary>
        /// Gets or sets the identifier kind of the wrapped alias.
        /// </summary>
        public IdentifierKind Kind
        {
            get { return (alias != null) ? alias.Kind : IdentifierKind.None; }
            set { if (alias != null) { alias.Kind = value; } }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Gets or sets the unique identifier of the wrapped alias.
        /// </summary>
        public Guid Id
        {
            get { return (alias != null) ? alias.Id : Guid.Empty; }
            set { if (alias != null) { alias.Id = value; } }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region IGetClientData / ISetClientData Members
        /// <summary>
        /// Gets or sets the client data associated with the wrapped alias.
        /// </summary>
        public IClientData ClientData
        {
            get { return (alias != null) ? alias.ClientData : null; }
            set { if (alias != null) { alias.ClientData = value; } }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region IIdentifier Members
        /// <summary>
        /// Gets or sets the group of the wrapped alias.
        /// </summary>
        public string Group
        {
            get { return (alias != null) ? alias.Group : null; }
            set { if (alias != null) { alias.Group = value; } }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Gets or sets the description of the wrapped alias.
        /// </summary>
        public string Description
        {
            get { return (alias != null) ? alias.Description : null; }
            set { if (alias != null) { alias.Description = value; } }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region IAliasData Members
        /// <summary>
        /// Gets or sets the name token of the wrapped alias.
        /// </summary>
        public string NameToken
        {
            get { return (alias != null) ? alias.NameToken : null; }
            set { if (alias != null) { alias.NameToken = value; } }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Gets or sets the source interpreter (the one in which the alias was
        /// created) of the wrapped alias.
        /// </summary>
        public Interpreter SourceInterpreter
        {
            get { return (alias != null) ? alias.SourceInterpreter : null; }
            set { if (alias != null) { alias.SourceInterpreter = value; } }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Gets or sets the target interpreter (the one the alias dispatches
        /// to) of the wrapped alias.
        /// </summary>
        public Interpreter TargetInterpreter
        {
            get { return (alias != null) ? alias.TargetInterpreter : null; }
            set { if (alias != null) { alias.TargetInterpreter = value; } }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Gets or sets the source namespace of the wrapped alias.
        /// </summary>
        public INamespace SourceNamespace
        {
            get { return (alias != null) ? alias.SourceNamespace : null; }
            set { if (alias != null) { alias.SourceNamespace = value; } }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Gets or sets the target namespace of the wrapped alias.
        /// </summary>
        public INamespace TargetNamespace
        {
            get { return (alias != null) ? alias.TargetNamespace : null; }
            set { if (alias != null) { alias.TargetNamespace = value; } }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Gets or sets the executable entity that the wrapped alias
        /// dispatches to.
        /// </summary>
        public IExecute Target
        {
            get { return (alias != null) ? alias.Target : null; }
            set { if (alias != null) { alias.Target = value; } }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Gets or sets the list of leading arguments prepended to each
        /// invocation of the wrapped alias.
        /// </summary>
        public ArgumentList Arguments
        {
            get { return (alias != null) ? alias.Arguments : null; }
            set { if (alias != null) { alias.Arguments = value; } }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Gets or sets the options associated with the wrapped alias.
        /// </summary>
        public OptionDictionary Options
        {
            get { return (alias != null) ? alias.Options : null; }
            set { if (alias != null) { alias.Options = value; } }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Gets or sets the flags that control the behavior of the wrapped
        /// alias.
        /// </summary>
        public AliasFlags AliasFlags
        {
            get { return (alias != null) ? alias.AliasFlags : AliasFlags.None; }
            set { if (alias != null) { alias.AliasFlags = value; } }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Gets or sets the index at which the supplied arguments are spliced
        /// into the invocation of the wrapped alias.
        /// </summary>
        public int StartIndex
        {
            get { return (alias != null) ? alias.StartIndex : 0; }
            set { if (alias != null) { alias.StartIndex = value; } }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region IAlias Members
        /// <summary>
        /// Gets the callback that is invoked after the interpreter associated
        /// with the wrapped alias has been disposed.
        /// </summary>
        public DisposeCallback PostInterpreterDisposed
        {
            get { return (alias != null) ? alias.PostInterpreterDisposed : null; }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region IWrapper Members
        /// <summary>
        /// Gets a value indicating whether the wrapped object represents a
        /// disposable resource.  This wrapper never owns a disposable
        /// resource, so this property always returns false.
        /// </summary>
        public override bool IsDisposable
        {
            get { return false; }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Gets or sets the wrapped object.  The value being set must be an
        /// <see cref="IAlias" /> instance.
        /// </summary>
        public override object Object
        {
            get { return alias; }
            set { alias = (IAlias)value; } /* throw */
        }
        #endregion
    }
}
