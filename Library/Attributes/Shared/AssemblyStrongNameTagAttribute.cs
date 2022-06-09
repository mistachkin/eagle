/*
 * AssemblyStrongNameTagAttribute.cs --
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
    [ObjectId("afdbd920-cb15-48d9-9469-23d03dc60d49")]
#else
    [Guid("afdbd920-cb15-48d9-9469-23d03dc60d49")]
#endif
    public sealed class AssemblyStrongNameTagAttribute : Attribute
    {
        public AssemblyStrongNameTagAttribute(
            string value
            )
        {
            strongNameTag = value;
        }

        ///////////////////////////////////////////////////////////////////////

        private string strongNameTag;
        public string StrongNameTag
        {
            get { return strongNameTag; }
        }
    }
}
