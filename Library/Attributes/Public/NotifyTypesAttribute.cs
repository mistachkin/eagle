/*
 * NotifyTypesAttribute.cs --
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
    [ObjectId("ff8808af-a151-4d6c-a6c8-3f93bc219de4")]
    public sealed class NotifyTypesAttribute : Attribute
    {
        public NotifyTypesAttribute(NotifyType types)
        {
            this.types = types;
        }

        ///////////////////////////////////////////////////////////////////////

        public NotifyTypesAttribute(string value)
        {
            types = (NotifyType)Enum.Parse(
                typeof(NotifyType), value); /* throw */
        }

        ///////////////////////////////////////////////////////////////////////

        private NotifyType types;
        public NotifyType Types
        {
            get { return types; }
        }
    }
}
