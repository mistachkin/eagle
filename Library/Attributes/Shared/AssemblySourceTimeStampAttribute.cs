/*
 * AssemblySourceTimeStampAttribute.cs --
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
    [ObjectId("4cf127ce-4382-45f8-8df1-93caafa06af8")]
#else
    [Guid("4cf127ce-4382-45f8-8df1-93caafa06af8")]
#endif
    public sealed class AssemblySourceTimeStampAttribute : Attribute
    {
        public AssemblySourceTimeStampAttribute(
            string value
            )
        {
            sourceTimeStamp = value;
        }

        ///////////////////////////////////////////////////////////////////////

        private string sourceTimeStamp;
        public string SourceTimeStamp
        {
            get { return sourceTimeStamp; }
        }
    }
}
