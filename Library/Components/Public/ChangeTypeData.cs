/*
 * ChangeTypeData.cs --
 *
 * Copyright (c) 2007-2012 by Joe Mistachkin.  All rights reserved.
 *
 * See the file "license.terms" for information on usage and redistribution of
 * this file, and for a DISCLAIMER OF ALL WARRANTIES.
 *
 * RCS: @(#) $Id: $
 */

using System;
using System.Globalization;
using Eagle._Attributes;
using Eagle._Components.Private;
using Eagle._Containers.Public;
using Eagle._Interfaces.Public;

namespace Eagle._Components.Public
{
    [ObjectId("b952ca92-8794-40d0-a89b-95e23ca533e0")]
    public class ChangeTypeData : IChangeTypeData
    {
        #region Public Constructors
        public ChangeTypeData(
            string caller,
            Type type,
            object oldValue,
            OptionDictionary options,
            CultureInfo cultureInfo,
            IClientData clientData,
            MarshalFlags marshalFlags
            )
        {
            this.caller = caller;
            this.type = type;
            this.oldValue = oldValue;
            this.options = options;
            this.cultureInfo = cultureInfo;
            this.clientData = clientData;
            this.marshalFlags = marshalFlags;

            ///////////////////////////////////////////////////////////////////

            Initialize();
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Private Methods
        private void Initialize()
        {
            noHandle = FlagOps.HasFlags(
                marshalFlags, MarshalFlags.NoHandle, true);
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region IHaveCultureInfo Members
        private CultureInfo cultureInfo;
        public virtual CultureInfo CultureInfo
        {
            get { return cultureInfo; }
            set { throw new NotImplementedException(); }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region IChangeTypeData Members
        private string caller;
        public virtual string Caller
        {
            get { return caller; }
        }

        ///////////////////////////////////////////////////////////////////////

        private Type type;
        public virtual Type Type
        {
            get { return type; }
        }

        ///////////////////////////////////////////////////////////////////////

        private object oldValue;
        public virtual object OldValue
        {
            get { return oldValue; }
        }

        ///////////////////////////////////////////////////////////////////////

        private OptionDictionary options;
        public virtual OptionDictionary Options
        {
            get { return options; }
        }

        ///////////////////////////////////////////////////////////////////////

        private IClientData clientData;
        public virtual IClientData ClientData
        {
            get { return clientData; }
        }

        ///////////////////////////////////////////////////////////////////////

        private MarshalFlags marshalFlags;
        public MarshalFlags MarshalFlags
        {
            get { return marshalFlags; }
            set { marshalFlags = value; }
        }

        ///////////////////////////////////////////////////////////////////////

        private object newValue;
        public virtual object NewValue
        {
            get { return newValue; }
            set { newValue = value; }
        }

        ///////////////////////////////////////////////////////////////////////

        private bool noHandle;
        public virtual bool NoHandle
        {
            get { return noHandle; }
            set { noHandle = value; }
        }

        ///////////////////////////////////////////////////////////////////////

        private bool wasObject;
        public virtual bool WasObject
        {
            get { return wasObject; }
            set { wasObject = value; }
        }

        ///////////////////////////////////////////////////////////////////////

        private bool attempted;
        public virtual bool Attempted
        {
            get { return attempted; }
            set { attempted = value; }
        }

        ///////////////////////////////////////////////////////////////////////

        private bool converted;
        public virtual bool Converted
        {
            get { return converted; }
            set { converted = value; }
        }

        ///////////////////////////////////////////////////////////////////////

        private bool doesMatch;
        public virtual bool DoesMatch
        {
            get { return doesMatch; }
            set { doesMatch = value; }
        }
        #endregion
    }
}
