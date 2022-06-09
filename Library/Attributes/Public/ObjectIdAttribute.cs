/*
 * ObjectIdAttribute.cs --
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
    [AttributeUsage(AttributeTargets.All, Inherited = false)]
    [ObjectId("e79f9b7d-808a-4093-ac58-800a9bbca609")]
    public sealed class ObjectIdAttribute : Attribute
    {
        public ObjectIdAttribute(Guid id)
        {
            this.id = id;
        }

        ///////////////////////////////////////////////////////////////////////

        public ObjectIdAttribute(string value)
        {
            id = new Guid(value); /* throw */
        }

        ///////////////////////////////////////////////////////////////////////

        private Guid id;
        public Guid Id
        {
            get { return id; }
        }
    }
}
