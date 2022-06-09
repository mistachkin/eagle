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
    [AttributeUsage(AttributeTargets.Assembly, Inherited = false)]
#if EAGLE
    [ObjectId("e1272060-51a5-4393-b276-c3018a4da739")]
#else
    [Guid("e1272060-51a5-4393-b276-c3018a4da739")]
#endif
    public sealed class AssemblyDateTimeAttribute : Attribute
    {
        public AssemblyDateTimeAttribute()
            : this(false)
        {
            // do nothing.
        }

        ///////////////////////////////////////////////////////////////////////

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

        public AssemblyDateTimeAttribute(
            DateTime dateTime
            )
            : this()
        {
            this.dateTime = dateTime;
        }

        ///////////////////////////////////////////////////////////////////////

        public AssemblyDateTimeAttribute(
            string value
            )
            : this()
        {
            if (!String.IsNullOrEmpty(value))
                dateTime = DateTime.Parse(value); /* throw */
        }

        ///////////////////////////////////////////////////////////////////////

        private DateTime dateTime;
        public DateTime DateTime
        {
            get { return dateTime; }
        }
    }
}
