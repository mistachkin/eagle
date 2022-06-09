/*
 * AssemblySourceIdAttribute.cs --
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
    [ObjectId("90a887ec-e4b5-4628-ae30-bc52b22f3d09")]
#else
    [Guid("90a887ec-e4b5-4628-ae30-bc52b22f3d09")]
#endif
    public sealed class AssemblySourceIdAttribute : Attribute
    {
        public AssemblySourceIdAttribute(
            string value
            )
        {
            sourceId = value;
        }

        ///////////////////////////////////////////////////////////////////////

        private string sourceId;
        public string SourceId
        {
            get { return sourceId; }
        }
    }
}
