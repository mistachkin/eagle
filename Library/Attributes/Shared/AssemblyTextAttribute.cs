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
    /// <summary>
    /// This class implements a custom attribute used to mark an assembly with
    /// an arbitrary block of descriptive text.
    /// </summary>
    [AttributeUsage(AttributeTargets.Assembly, Inherited = false)]
#if EAGLE
    [ObjectId("78379ac8-ead8-40b2-a6d8-e81f3e5006b0")]
#else
    [Guid("78379ac8-ead8-40b2-a6d8-e81f3e5006b0")]
#endif
    public sealed class AssemblyTextAttribute : Attribute
    {
        /// <summary>
        /// Constructs an instance of this class, recording the specified text
        /// value.
        /// </summary>
        /// <param name="value">
        /// The descriptive text string to associate with the assembly.
        /// </param>
        public AssemblyTextAttribute(
            string value
            )
        {
            text = value;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// The descriptive text string associated with the assembly.
        /// </summary>
        private string text;

        /// <summary>
        /// Gets the descriptive text string associated with the assembly.
        /// </summary>
        public string Text
        {
            get { return text; }
        }
    }
}
