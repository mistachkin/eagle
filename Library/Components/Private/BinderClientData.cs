/*
 * BinderClientData.cs --
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
using Eagle._Containers.Public;
using Eagle._Interfaces.Public;

namespace Eagle._Components.Private
{
    [ObjectId("df8adcbd-138a-4045-9a34-806af8bc3aae")]
    internal sealed class BinderClientData : ClientData, IHaveClientData
    {
        #region Private Constructors
        private BinderClientData(
            object data
            )
            : base(data)
        {
            // do nothing.
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Public Constructors
        public BinderClientData(
            object data,
            OptionDictionary options,
            IClientData clientData
            )
            : this(data)
        {
            this.options = options;
            this.clientData = clientData;
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Public Properties
        private OptionDictionary options;
        public OptionDictionary Options
        {
            get { return options; }
            set { options = value; }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region IGetClientData / ISetClientData Members
        private IClientData clientData;
        public IClientData ClientData
        {
            get { return clientData; }
            set { clientData = value; }
        }
        #endregion
    }
}
