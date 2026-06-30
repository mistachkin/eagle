/*
 * DelegateData.cs --
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
using Eagle._Components.Private;
using Eagle._Interfaces.Public;

namespace Eagle._Components.Public
{
    /// <summary>
    /// This class holds the metadata describing a managed delegate that has
    /// been exposed to an interpreter, including the delegate itself, the flags
    /// that control its behavior, and the token used to identify it.
    /// </summary>
#if SERIALIZATION
    [Serializable()]
#endif
    [ObjectId("bda671f4-34f0-4ba7-b90e-cd04e780a4c0")]
    public class DelegateData : IDelegateData
    {
        /// <summary>
        /// Constructs a delegate data instance wrapping the specified delegate,
        /// flags, and token.
        /// </summary>
        /// <param name="delegate">
        /// The managed delegate to be wrapped.
        /// </param>
        /// <param name="delegateFlags">
        /// The flags controlling the behavior of the wrapped delegate.
        /// </param>
        /// <param name="token">
        /// The token used to identify this delegate within the interpreter.
        /// </param>
        public DelegateData(
            Delegate @delegate,
            DelegateFlags delegateFlags,
            long token
            )
        {
            this.kind = IdentifierKind.DelegateData;
            this.id = AttributeOps.GetObjectId(this);
            this.@delegate = @delegate;
            this.delegateFlags = delegateFlags;
            this.token = token;
        }

        ///////////////////////////////////////////////////////////////////////

        #region IIdentifierName Members
        /// <summary>
        /// Stores the name of this delegate data.
        /// </summary>
        private string name;
        /// <summary>
        /// Gets or sets the name of this delegate data.
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
        /// Stores the identifier kind of this delegate data.
        /// </summary>
        private IdentifierKind kind;
        /// <summary>
        /// Gets or sets the identifier kind of this delegate data.
        /// </summary>
        public virtual IdentifierKind Kind
        {
            get { return kind; }
            set { kind = value; }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Stores the globally unique identifier of this delegate data.
        /// </summary>
        private Guid id;
        /// <summary>
        /// Gets or sets the globally unique identifier of this delegate data.
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
        /// Stores the client data associated with this delegate data.
        /// </summary>
        private IClientData clientData;
        /// <summary>
        /// Gets or sets the client data associated with this delegate data.
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
        /// Stores the group of this delegate data.
        /// </summary>
        private string group;
        /// <summary>
        /// Gets or sets the group of this delegate data.
        /// </summary>
        public virtual string Group
        {
            get { return group; }
            set { group = value; }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Stores the description of this delegate data.
        /// </summary>
        private string description;
        /// <summary>
        /// Gets or sets the description of this delegate data.
        /// </summary>
        public virtual string Description
        {
            get { return description; }
            set { description = value; }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region IDynamicExecuteDelegate Members
        /// <summary>
        /// Stores the managed delegate wrapped by this delegate data.
        /// </summary>
        private Delegate @delegate;
        /// <summary>
        /// Gets or sets the managed delegate wrapped by this delegate data.
        /// </summary>
        public virtual Delegate Delegate
        {
            get { return @delegate; }
            set { @delegate = value; }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region IDelegateData Members
        /// <summary>
        /// Stores the flags controlling the behavior of the wrapped delegate.
        /// </summary>
        private DelegateFlags delegateFlags;
        /// <summary>
        /// Gets or sets the flags controlling the behavior of the wrapped
        /// delegate.
        /// </summary>
        public virtual DelegateFlags DelegateFlags
        {
            get { return delegateFlags; }
            set { delegateFlags = value; }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region IWrapperData Members
        /// <summary>
        /// Stores the token used to identify this delegate data within the
        /// interpreter.
        /// </summary>
        private long token;
        /// <summary>
        /// Gets or sets the token used to identify this delegate data within
        /// the interpreter.
        /// </summary>
        public virtual long Token
        {
            get { return token; }
            set { token = value; }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region System.Object Overrides
        /// <summary>
        /// This method produces a string representation of this delegate data
        /// using its name only.
        /// </summary>
        /// <returns>
        /// The name of this delegate data, or an empty string when it has no
        /// name.
        /// </returns>
        public override string ToString()
        {
            return (name != null) ? name : String.Empty;
        }
        #endregion
    }
}
