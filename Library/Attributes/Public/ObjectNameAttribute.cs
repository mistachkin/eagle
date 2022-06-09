/*
 * ObjectNameAttribute.cs --
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
    [AttributeUsage(
        AttributeTargets.Class | AttributeTargets.Delegate,
        Inherited = false)]
    [ObjectId("4000ce04-6adc-4560-8d59-7f1eb5186c68")]
    public sealed class ObjectNameAttribute : Attribute
    {
        public ObjectNameAttribute(string value)
        {
            name = value;
        }

        ///////////////////////////////////////////////////////////////////////

        private string name;
        public string Name
        {
            get { return name; }
        }
    }
}
