/*
 * ParameterIndexAttribute.cs --
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
    [AttributeUsage(AttributeTargets.Field, Inherited = false)]
    [ObjectId("dcc2a37d-263a-472a-91df-2c37d8a303b9")]
    public sealed class ParameterIndexAttribute : Attribute
    {
        public ParameterIndexAttribute(int index)
        {
            this.index = index;
        }

        ///////////////////////////////////////////////////////////////////////

        public ParameterIndexAttribute(string value)
        {
            index = int.Parse(value); /* throw */
        }

        ///////////////////////////////////////////////////////////////////////

        private int index;
        public int Index
        {
            get { return index; }
        }
    }
}
