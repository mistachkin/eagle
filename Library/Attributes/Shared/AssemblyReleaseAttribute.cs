/*
 * AssemblyReleaseAttribute.cs --
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
    [ObjectId("1dbec2ac-950c-4201-9c4a-d0a7d173be1f")]
#else
    [Guid("1dbec2ac-950c-4201-9c4a-d0a7d173be1f")]
#endif
    public sealed class AssemblyReleaseAttribute : Attribute
    {
        public AssemblyReleaseAttribute(
            string value
            )
        {
            release = value;
        }

        ///////////////////////////////////////////////////////////////////////

        private string release;
        public string Release
        {
            get { return release; }
        }
    }
}
