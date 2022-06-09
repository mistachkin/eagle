/*
 * AssemblyTagAttribute.cs --
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

namespace Eagle._Attributes
{
    [AttributeUsage(AttributeTargets.Assembly, Inherited = false)]
#if EAGLE
    [ObjectId("367b72f5-08e2-4c74-a5a6-460ae90ec1bc")]
#else
    [Guid("367b72f5-08e2-4c74-a5a6-460ae90ec1bc")]
#endif
    public sealed class AssemblyTagAttribute : Attribute
    {
        public AssemblyTagAttribute(
            string value
            )
        {
            tag = value;
        }

        ///////////////////////////////////////////////////////////////////////

        private string tag;
        public string Tag
        {
            get { return tag; }
        }
    }
}
