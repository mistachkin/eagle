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
    /// <summary>
    /// This class implements a custom attribute used to mark an assembly with
    /// the release identifier string it was built for.
    /// </summary>
    [AttributeUsage(AttributeTargets.Assembly, Inherited = false)]
#if EAGLE
    [ObjectId("1dbec2ac-950c-4201-9c4a-d0a7d173be1f")]
#else
    [Guid("1dbec2ac-950c-4201-9c4a-d0a7d173be1f")]
#endif
    public sealed class AssemblyReleaseAttribute : Attribute
    {
        /// <summary>
        /// Constructs an instance of this class, recording the specified
        /// release identifier value.
        /// </summary>
        /// <param name="value">
        /// The release identifier string to associate with the assembly.
        /// </param>
        public AssemblyReleaseAttribute(
            string value
            )
        {
            release = value;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// The release identifier string associated with the assembly.
        /// </summary>
        private string release;

        /// <summary>
        /// Gets the release identifier string associated with the assembly.
        /// </summary>
        public string Release
        {
            get { return release; }
        }
    }
}
