/*
 * ClockData.cs --
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
using Eagle._Interfaces.Public;

namespace Eagle._Components.Public
{
#if SERIALIZATION
    [Serializable()]
#endif
    [ObjectId("280fe1b6-d4cd-42c0-94ba-6c547402e0cc")]
    public class ClockData : IClockData
    {
        public ClockData(
            string name,
            CultureInfo cultureInfo,
            TimeZone timeZone,
            string format,
            DateTime dateTime,
            DateTime epoch,
            IClientData clientData
            )
        {
            this.kind = IdentifierKind.ClockData;
            this.id = AttributeOps.GetObjectId(this);
            this.name = name;
            this.clientData = clientData;
            this.cultureInfo = cultureInfo;
            this.timeZone = timeZone;
            this.format = format;
            this.dateTime = dateTime;
            this.epoch = epoch;
        }

        ///////////////////////////////////////////////////////////////////////

        #region IIdentifierName Members
        private string name;
        public virtual string Name
        {
            get { return name; }
            set { name = value; }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region IIdentifierBase Members
        private IdentifierKind kind;
        public virtual IdentifierKind Kind
        {
            get { return kind; }
            set { kind = value; }
        }

        ///////////////////////////////////////////////////////////////////////

        private Guid id;
        public virtual Guid Id
        {
            get { return id; }
            set { id = value; }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region IIdentifier Members
        private string group;
        public virtual string Group
        {
            get { return group; }
            set { group = value; }
        }

        ///////////////////////////////////////////////////////////////////////

        private string description;
        public virtual string Description
        {
            get { return description; }
            set { description = value; }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region IGetClientData / ISetClientData Members
        private IClientData clientData;
        public virtual IClientData ClientData
        {
            get { return clientData; }
            set { clientData = value; }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region IHaveCultureInfo Members
        private CultureInfo cultureInfo;
        public virtual CultureInfo CultureInfo
        {
            get { return cultureInfo; }
            set { cultureInfo = value; }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region IClockData Members
        private TimeZone timeZone;
        public virtual TimeZone TimeZone
        {
            get { return timeZone; }
            set { timeZone = value; }
        }

        ///////////////////////////////////////////////////////////////////////

        private string format;
        public virtual string Format
        {
            get { return format; }
            set { format = value; }
        }

        ///////////////////////////////////////////////////////////////////////

        private DateTime dateTime;
        public virtual DateTime DateTime
        {
            get { return dateTime; }
            set { dateTime = value; }
        }

        ///////////////////////////////////////////////////////////////////////

        private DateTime epoch;
        public virtual DateTime Epoch
        {
            get { return epoch; }
            set { epoch = value; }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region System.Object Overrides
        public override string ToString()
        {
            return (name != null) ? name : String.Empty;
        }
        #endregion
    }
}
