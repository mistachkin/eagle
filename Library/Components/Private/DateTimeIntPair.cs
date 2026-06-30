/*
 * DateTimeIntPair.cs --
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

namespace Eagle._Components.Private
{
    /// <summary>
    /// This class associates an optional <see cref="DateTime" /> with an
    /// integer counter, pairing a (possibly null) timestamp with a running
    /// tally.  It is a mutable specialization of
    /// <see cref="MutableAnyPair{T1, T2}" /> intended for tracking the most
    /// recent time something occurred together with how many times it has
    /// occurred.
    /// </summary>
#if SERIALIZATION
    [Serializable()]
#endif
    [ObjectId("06b93229-416f-4385-9036-e1174755c090")]
    internal sealed class DateTimeIntPair : MutableAnyPair<DateTime?, int>
    {
        #region Private Constructors
        /// <summary>
        /// Constructs an instance of this class from the specified timestamp
        /// and counter values.
        /// </summary>
        /// <param name="x">
        /// The initial timestamp value.  This parameter may be null.
        /// </param>
        /// <param name="y">
        /// The initial integer counter value.
        /// </param>
        private DateTimeIntPair(
            DateTime? x,
            int y
            )
            : base(true, x, y)
        {
            // do nothing.
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Static "Factory" Methods
        /// <summary>
        /// Creates a new instance of this class using the specified timestamp
        /// and a zero counter value.
        /// </summary>
        /// <param name="dateTime">
        /// The initial timestamp value.  This parameter may be null.
        /// </param>
        /// <returns>
        /// The newly created instance of this class.
        /// </returns>
        public static DateTimeIntPair Create(
            DateTime? dateTime
            )
        {
            return new DateTimeIntPair(dateTime, 0);
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Public Methods
        /// <summary>
        /// This method updates the stored timestamp and increments the stored
        /// counter, when the corresponding arguments are supplied.
        /// </summary>
        /// <param name="dateTime">
        /// The new timestamp value to store, or null to leave the stored
        /// timestamp unchanged.
        /// </param>
        /// <param name="count">
        /// The amount to add to the stored counter, or null to leave the stored
        /// counter unchanged.
        /// </param>
        /// <returns>
        /// The resulting value of the stored counter.
        /// </returns>
        public int Touch(
            DateTime? dateTime,
            int? count
            )
        {
            if (dateTime != null)
                this.X = dateTime;

            if (count != null)
                return this.Y += (int)count;
            else
                return this.Y;
        }
        #endregion
    }
}
