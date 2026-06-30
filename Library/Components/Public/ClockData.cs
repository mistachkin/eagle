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
    /// <summary>
    /// This class holds the contextual data used by Eagle's clock subsystem
    /// when formatting and scanning dates and times -- the culture, time zone,
    /// format string, reference date/time, and epoch that govern a clock
    /// operation.  It implements <see cref="IClockData" /> and carries the
    /// standard identifier and client data associated with such an object.
    /// </summary>
#if SERIALIZATION
    [Serializable()]
#endif
    [ObjectId("280fe1b6-d4cd-42c0-94ba-6c547402e0cc")]
    public class ClockData : IClockData
    {
        /// <summary>
        /// Constructs a clock data instance from the fully specified set of
        /// identity, culture, time zone, format, and date/time parameters.
        /// </summary>
        /// <param name="name">
        /// The name of this clock data instance.  This parameter may be null.
        /// </param>
        /// <param name="cultureInfo">
        /// The culture used when formatting and scanning dates and times.  This
        /// parameter may be null.
        /// </param>
        /// <param name="timeZone">
        /// The time zone used when interpreting dates and times.  This parameter
        /// may be null.
        /// </param>
        /// <param name="format">
        /// The format string used when formatting or scanning dates and times.
        /// This parameter may be null.
        /// </param>
        /// <param name="dateTime">
        /// The reference date and time associated with this clock data instance.
        /// </param>
        /// <param name="epoch">
        /// The epoch (origin) date and time associated with this clock data
        /// instance.
        /// </param>
        /// <param name="clientData">
        /// The client data to associate with this clock data instance, if any.
        /// This parameter may be null.
        /// </param>
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
        /// <summary>
        /// The name of this clock data instance.
        /// </summary>
        private string name;

        /// <summary>
        /// Gets or sets the name of this clock data instance.
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
        /// The kind of identifier represented by this clock data instance.
        /// </summary>
        private IdentifierKind kind;

        /// <summary>
        /// Gets or sets the kind of identifier represented by this clock data
        /// instance.
        /// </summary>
        public virtual IdentifierKind Kind
        {
            get { return kind; }
            set { kind = value; }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// The unique identifier of this clock data instance.
        /// </summary>
        private Guid id;

        /// <summary>
        /// Gets or sets the unique identifier of this clock data instance.
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
        /// The client data associated with this clock data instance.
        /// </summary>
        private IClientData clientData;

        /// <summary>
        /// Gets or sets the client data associated with this clock data
        /// instance.
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
        /// The group of this clock data instance.
        /// </summary>
        private string group;

        /// <summary>
        /// Gets or sets the group of this clock data instance.
        /// </summary>
        public virtual string Group
        {
            get { return group; }
            set { group = value; }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// The description of this clock data instance.
        /// </summary>
        private string description;

        /// <summary>
        /// Gets or sets the description of this clock data instance.
        /// </summary>
        public virtual string Description
        {
            get { return description; }
            set { description = value; }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region IHaveCultureInfo Members
        /// <summary>
        /// The culture used when formatting and scanning dates and times.
        /// </summary>
        private CultureInfo cultureInfo;

        /// <summary>
        /// Gets or sets the culture used when formatting and scanning dates and
        /// times.
        /// </summary>
        public virtual CultureInfo CultureInfo
        {
            get { return cultureInfo; }
            set { cultureInfo = value; }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region IClockData Members
        /// <summary>
        /// The time zone used when interpreting dates and times.
        /// </summary>
        private TimeZone timeZone;

        /// <summary>
        /// Gets or sets the time zone used when interpreting dates and times.
        /// </summary>
        public virtual TimeZone TimeZone
        {
            get { return timeZone; }
            set { timeZone = value; }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// The format string used when formatting or scanning dates and times.
        /// </summary>
        private string format;

        /// <summary>
        /// Gets or sets the format string used when formatting or scanning dates
        /// and times.
        /// </summary>
        public virtual string Format
        {
            get { return format; }
            set { format = value; }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// The reference date and time associated with this clock data instance.
        /// </summary>
        private DateTime dateTime;

        /// <summary>
        /// Gets or sets the reference date and time associated with this clock
        /// data instance.
        /// </summary>
        public virtual DateTime DateTime
        {
            get { return dateTime; }
            set { dateTime = value; }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// The epoch (origin) date and time associated with this clock data
        /// instance.
        /// </summary>
        private DateTime epoch;

        /// <summary>
        /// Gets or sets the epoch (origin) date and time associated with this
        /// clock data instance.
        /// </summary>
        public virtual DateTime Epoch
        {
            get { return epoch; }
            set { epoch = value; }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region System.Object Overrides
        /// <summary>
        /// This method returns the string representation of this clock data
        /// instance.
        /// </summary>
        /// <returns>
        /// The name of this clock data instance, or an empty string if it has
        /// no name.
        /// </returns>
        public override string ToString()
        {
            return (name != null) ? name : String.Empty;
        }
        #endregion
    }
}
