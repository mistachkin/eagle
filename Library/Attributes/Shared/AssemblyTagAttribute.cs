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
    /// <summary>
    /// This class implements a custom attribute used to mark an assembly with
    /// an arbitrary tag string.
    /// </summary>
    [AttributeUsage(AttributeTargets.Assembly, Inherited = false)]
#if EAGLE
    [ObjectId("367b72f5-08e2-4c74-a5a6-460ae90ec1bc")]
#else
    [Guid("367b72f5-08e2-4c74-a5a6-460ae90ec1bc")]
#endif
    public sealed class AssemblyTagAttribute : Attribute
    {
        /// <summary>
        /// Constructs an instance of this class, recording the specified tag
        /// value.
        /// </summary>
        /// <param name="value">
        /// The tag string to associate with the assembly.
        /// </param>
        public AssemblyTagAttribute(
            string value
            )
        {
            tag = value;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// The tag string associated with the assembly.
        /// </summary>
        private string tag;

        /// <summary>
        /// Gets the tag string associated with the assembly.
        /// </summary>
        public string Tag
        {
            get { return tag; }
        }
    }
}
