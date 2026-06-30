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
    /// <summary>
    /// This class implements a custom attribute used to mark an assembly with
    /// the source control identifier (e.g. the revision hash) of the sources
    /// it was built from.
    /// </summary>
    [AttributeUsage(AttributeTargets.Assembly, Inherited = false)]
#if EAGLE
    [ObjectId("90a887ec-e4b5-4628-ae30-bc52b22f3d09")]
#else
    [Guid("90a887ec-e4b5-4628-ae30-bc52b22f3d09")]
#endif
    public sealed class AssemblySourceIdAttribute : Attribute
    {
        /// <summary>
        /// Constructs an instance of this class, recording the specified
        /// source identifier value.
        /// </summary>
        /// <param name="value">
        /// The source control identifier string to associate with the
        /// assembly.
        /// </param>
        public AssemblySourceIdAttribute(
            string value
            )
        {
            sourceId = value;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// The source control identifier string associated with the assembly.
        /// </summary>
        private string sourceId;

        /// <summary>
        /// Gets the source control identifier string associated with the
        /// assembly.
        /// </summary>
        public string SourceId
        {
            get { return sourceId; }
        }
    }
}
