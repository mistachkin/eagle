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
    /// <summary>
    /// This class implements an attribute used to associate license
    /// information (both a summary and the full license text) with the
    /// assembly it marks.
    /// </summary>
    [AttributeUsage(AttributeTargets.Assembly, Inherited = false)]
#if EAGLE
    [ObjectId("6a53038d-a03a-4291-87fd-dac0c3f3cf70")]
#else
    [Guid("6a53038d-a03a-4291-87fd-dac0c3f3cf70")]
#endif
    public sealed class AssemblyLicenseAttribute : Attribute
    {
        /// <summary>
        /// Constructs an instance of this attribute using the specified
        /// license summary and full license text.
        /// </summary>
        /// <param name="summary">
        /// A short summary of the license associated with the marked assembly.
        /// </param>
        /// <param name="text">
        /// The full license text associated with the marked assembly.
        /// </param>
        public AssemblyLicenseAttribute(string summary, string text)
        {
            this.summary = summary;
            this.text = text;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// A short summary of the license associated with the marked assembly.
        /// </summary>
        private string summary;
        /// <summary>
        /// Gets a short summary of the license associated with the marked
        /// assembly.
        /// </summary>
        public string Summary
        {
            get { return summary; }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// The full license text associated with the marked assembly.
        /// </summary>
        private string text;
        /// <summary>
        /// Gets the full license text associated with the marked assembly.
        /// </summary>
        public string Text
        {
            get { return text; }
        }
    }
}
