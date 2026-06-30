/*
 * AssemblyDateTimeAttribute.cs --
 *
 * Copyright (c) 2007-2012 by Joe Mistachkin.  All rights reserved.
 *
 * See the file "license.terms" for information on usage and redistribution of
 * this file, and for a DISCLAIMER OF ALL WARRANTIES.
 *
 * RCS: @(#) $Id: $
 */

using System;

#if !EAGLE
using System.Runtime.InteropServices;
#endif

#if EAGLE
using Eagle._Components.Private;
#endif

namespace Eagle._Attributes
{
    /// <summary>
    /// This class implements an attribute used to associate a build (or other
    /// significant) date and time with the assembly it marks.
    /// </summary>
    [AttributeUsage(AttributeTargets.Assembly, Inherited = false)]
#if EAGLE
    [ObjectId("e1272060-51a5-4393-b276-c3018a4da739")]
#else
    [Guid("e1272060-51a5-4393-b276-c3018a4da739")]
#endif
    public sealed class AssemblyDateTimeAttribute : Attribute
    {
        /// <summary>
        /// Constructs an instance of this attribute, using the date and time
        /// obtained from the location of the containing (non-entry) assembly.
        /// </summary>
        public AssemblyDateTimeAttribute()
            : this(false)
        {
            // do nothing.
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Constructs an instance of this attribute, using the date and time
        /// obtained from the location of either the entry assembly or the
        /// containing assembly.
        /// </summary>
        /// <param name="entry">
        /// Non-zero to obtain the date and time from the location of the entry
        /// assembly; otherwise, the location of the containing assembly is
        /// used.
        /// </param>
        public AssemblyDateTimeAttribute(
            bool entry
            )
        {
#if EAGLE
            string fileName = entry ?
                GlobalState.GetEntryAssemblyLocation() :
                GlobalState.GetAssemblyLocation();

            if (FileOps.GetPeFileDateTime(fileName, ref dateTime))
                return;
#endif

            dateTime = DateTime.MinValue;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Constructs an instance of this attribute using the specified date
        /// and time.
        /// </summary>
        /// <param name="dateTime">
        /// The date and time to associate with the marked assembly.
        /// </param>
        public AssemblyDateTimeAttribute(
            DateTime dateTime
            )
            : this()
        {
            this.dateTime = dateTime;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Constructs an instance of this attribute using the specified date
        /// and time in its string form.
        /// </summary>
        /// <param name="value">
        /// The string representation of the date and time to associate with
        /// the marked assembly.  If this value is null or an empty string, no
        /// date and time is recorded.  An exception is thrown if this value
        /// cannot be parsed as a date and time.
        /// </param>
        public AssemblyDateTimeAttribute(
            string value
            )
            : this()
        {
            if (!String.IsNullOrEmpty(value))
                dateTime = DateTime.Parse(value); /* throw */
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// The date and time associated with the marked assembly.
        /// </summary>
        private DateTime dateTime;
        /// <summary>
        /// Gets the date and time associated with the marked assembly.
        /// </summary>
        public DateTime DateTime
        {
            get { return dateTime; }
        }
    }
}
