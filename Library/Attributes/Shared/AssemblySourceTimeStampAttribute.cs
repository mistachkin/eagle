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
    /// <summary>
    /// This class implements a custom attribute used to mark an assembly with
    /// the time stamp of the source control revision it was built from.
    /// </summary>
    [AttributeUsage(AttributeTargets.Assembly, Inherited = false)]
#if EAGLE
    [ObjectId("4cf127ce-4382-45f8-8df1-93caafa06af8")]
#else
    [Guid("4cf127ce-4382-45f8-8df1-93caafa06af8")]
#endif
    public sealed class AssemblySourceTimeStampAttribute : Attribute
    {
        /// <summary>
        /// Constructs an instance of this class, recording the specified
        /// source time stamp value.
        /// </summary>
        /// <param name="value">
        /// The source time stamp string to associate with the assembly.
        /// </param>
        public AssemblySourceTimeStampAttribute(
            string value
            )
        {
            sourceTimeStamp = value;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// The source time stamp string associated with the assembly.
        /// </summary>
        private string sourceTimeStamp;

        /// <summary>
        /// Gets the source time stamp string associated with the assembly.
        /// </summary>
        public string SourceTimeStamp
        {
            get { return sourceTimeStamp; }
        }
    }
}
