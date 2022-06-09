/*
 * AssemblyTextAttribute.cs --
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
    [ObjectId("78379ac8-ead8-40b2-a6d8-e81f3e5006b0")]
#else
    [Guid("78379ac8-ead8-40b2-a6d8-e81f3e5006b0")]
#endif
    public sealed class AssemblyTextAttribute : Attribute
    {
        public AssemblyTextAttribute(
            string value
            )
        {
            text = value;
        }

        ///////////////////////////////////////////////////////////////////////

        private string text;
        public string Text
        {
            get { return text; }
        }
    }
}
