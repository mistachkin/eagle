/*
 * ResolverClientData.cs --
 *
 * Copyright (c) 2007-2012 by Joe Mistachkin.  All rights reserved.
 *
 * See the file "license.terms" for information on usage and redistribution of
 * this file, and for a DISCLAIMER OF ALL WARRANTIES.
 *
 * RCS: @(#) $Id: $
 */

using Eagle._Attributes;
using Eagle._Components.Public;
using Eagle._Interfaces.Public;

namespace Eagle._Components.Private
{
    /// <summary>
    /// This class provides a container for client data associated with a custom
    /// command, variable, or other entity resolver, augmenting the base client
    /// data payload with an additional, settable client data slot.
    /// </summary>
    [ObjectId("8838127e-42f4-4a01-bdef-0cd05c9e7e9d")]
    internal sealed class ResolverClientData : ClientData, IHaveClientData
    {
        #region Public Constructors
        /// <summary>
        /// Constructs an instance of this class wrapping the specified opaque
        /// data payload.
        /// </summary>
        /// <param name="data">
        /// The opaque data payload to associate with this object.  This
        /// parameter may be null.
        /// </param>
        public ResolverClientData(
            object data
            )
            : base(data)
        {
            // do nothing.
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region IGetClientData / ISetClientData Members
        /// <summary>
        /// Stores the additional client data associated with this object.
        /// </summary>
        private IClientData clientData;
        /// <summary>
        /// Gets or sets the additional client data associated with this object.
        /// </summary>
        public IClientData ClientData
        {
            get { return clientData; }
            set { clientData = value; }
        }
        #endregion
    }
}
