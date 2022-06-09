/*
 * NotifyFlagsAttribute.cs --
 *
 * Copyright (c) 2007-2012 by Joe Mistachkin.  All rights reserved.
 *
 * See the file "license.terms" for information on usage and redistribution of
 * this file, and for a DISCLAIMER OF ALL WARRANTIES.
 *
 * RCS: @(#) $Id: $
 */

using System;
using Eagle._Components.Public;

namespace Eagle._Attributes
{
    [AttributeUsage(AttributeTargets.Class, Inherited = false)]
    [ObjectId("3697138f-bf83-4bfd-97dd-f8597013c98d")]
    public sealed class NotifyFlagsAttribute : Attribute
    {
        public NotifyFlagsAttribute(NotifyFlags flags)
        {
            this.flags = flags;
        }

        ///////////////////////////////////////////////////////////////////////

        public NotifyFlagsAttribute(string value)
        {
            flags = (NotifyFlags)Enum.Parse(
                typeof(NotifyFlags), value); /* throw */
        }

        ///////////////////////////////////////////////////////////////////////

        private NotifyFlags flags;
        public NotifyFlags Flags
        {
            get { return flags; }
        }
    }
}
