/*
 * ObjectGroupAttribute.cs --
 *
 * Copyright (c) 2007-2012 by Joe Mistachkin.  All rights reserved.
 *
 * See the file "license.terms" for information on usage and redistribution of
 * this file, and for a DISCLAIMER OF ALL WARRANTIES.
 *
 * RCS: @(#) $Id: $
 */

using System;

namespace Eagle._Attributes
{
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = true, Inherited = false)]
    [ObjectId("8d4a0df8-1942-41ab-94e6-774c74d58db4")]
    public sealed class ObjectGroupAttribute : Attribute
    {
        public ObjectGroupAttribute(string value)
        {
            group = value;
        }

        ///////////////////////////////////////////////////////////////////////

        private string group;
        public string Group
        {
            get { return group; }
        }
    }
}
