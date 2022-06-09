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
#if SERIALIZATION
    [Serializable()]
#endif
    [ObjectId("06b93229-416f-4385-9036-e1174755c090")]
    internal sealed class DateTimeIntPair : MutableAnyPair<DateTime?, int>
    {
        #region Private Constructors
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
        public static DateTimeIntPair Create(
            DateTime? dateTime
            )
        {
            return new DateTimeIntPair(dateTime, 0);
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Public Methods
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
