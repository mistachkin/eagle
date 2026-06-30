/*
 * EnsembleData.cs --
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
    /// This class holds the metadata describing an ensemble command exposed to
    /// an interpreter, including the executable entity used to dispatch its
    /// sub-commands and the token used to identify it.
    /// </summary>
#if SERIALIZATION
    [Serializable()]
#endif
    [ObjectId("f7c02ee0-550e-4a60-9b1a-784aa029506c")]
    public class EnsembleData : IEnsembleData
    {
        /// <summary>
        /// Constructs an ensemble data instance wrapping the specified
        /// sub-command executable and token.
        /// </summary>
        /// <param name="subCommandExecute">
        /// The executable entity used to dispatch the sub-commands of this
        /// ensemble.
        /// </param>
        /// <param name="token">
        /// The token used to identify this ensemble within the interpreter.
        /// </param>
        public EnsembleData(
            IExecute subCommandExecute,
            long token
            )
        {
            this.kind = IdentifierKind.EnsembleData;
            this.id = AttributeOps.GetObjectId(this);
            this.subCommandExecute = subCommandExecute;
            this.token = token;
        }

        ///////////////////////////////////////////////////////////////////////

        #region IIdentifierName Members
        /// <summary>
        /// Stores the name of this ensemble data.
        /// </summary>
        private string name;
        /// <summary>
        /// Gets or sets the name of this ensemble data.
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
        /// Stores the identifier kind of this ensemble data.
        /// </summary>
        private IdentifierKind kind;
        /// <summary>
        /// Gets or sets the identifier kind of this ensemble data.
        /// </summary>
        public virtual IdentifierKind Kind
        {
            get { return kind; }
            set { kind = value; }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Stores the globally unique identifier of this ensemble data.
        /// </summary>
        private Guid id;
        /// <summary>
        /// Gets or sets the globally unique identifier of this ensemble data.
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
        /// Stores the client data associated with this ensemble data.
        /// </summary>
        private IClientData clientData;
        /// <summary>
        /// Gets or sets the client data associated with this ensemble data.
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
        /// Stores the group of this ensemble data.
        /// </summary>
        private string group;
        /// <summary>
        /// Gets or sets the group of this ensemble data.
        /// </summary>
        public virtual string Group
        {
            get { return group; }
            set { group = value; }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Stores the description of this ensemble data.
        /// </summary>
        private string description;
        /// <summary>
        /// Gets or sets the description of this ensemble data.
        /// </summary>
        public virtual string Description
        {
            get { return description; }
            set { description = value; }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region IEnsembleData Members
        /// <summary>
        /// Stores the executable entity used to dispatch the sub-commands of
        /// this ensemble.
        /// </summary>
        private IExecute subCommandExecute;
        /// <summary>
        /// Gets or sets the executable entity used to dispatch the sub-commands
        /// of this ensemble.
        /// </summary>
        public virtual IExecute SubCommandExecute
        {
            get { return subCommandExecute; }
            set { subCommandExecute = value; }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region IWrapperData Members
        /// <summary>
        /// Stores the token used to identify this ensemble data within the
        /// interpreter.
        /// </summary>
        private long token;
        /// <summary>
        /// Gets or sets the token used to identify this ensemble data within
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
        /// This method produces a string representation of this ensemble data
        /// using its name only.
        /// </summary>
        /// <returns>
        /// The name of this ensemble data, or an empty string when it has no
        /// name.
        /// </returns>
        public override string ToString()
        {
            return (name != null) ? name : String.Empty;
        }
        #endregion
    }
}
