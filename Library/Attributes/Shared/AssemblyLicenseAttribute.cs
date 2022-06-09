/*
 * AssemblyLicenseAttribute.cs --
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
    [ObjectId("6a53038d-a03a-4291-87fd-dac0c3f3cf70")]
#else
    [Guid("6a53038d-a03a-4291-87fd-dac0c3f3cf70")]
#endif
    public sealed class AssemblyLicenseAttribute : Attribute
    {
        public AssemblyLicenseAttribute(string summary, string text)
        {
            this.summary = summary;
            this.text = text;
        }

        ///////////////////////////////////////////////////////////////////////

        private string summary;
        public string Summary
        {
            get { return summary; }
        }

        ///////////////////////////////////////////////////////////////////////

        private string text;
        public string Text
        {
            get { return text; }
        }
    }
}
