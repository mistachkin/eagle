/*
 * AssemblyUriAttribute.cs --
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
    /// a named URI (e.g. its home page, update location, or other related
    /// resource).  This attribute may be applied more than once to associate
    /// several URIs with the same assembly.
    /// </summary>
    [AttributeUsage(AttributeTargets.Assembly,
        AllowMultiple = true, Inherited = false)]
#if EAGLE
    [ObjectId("a6489d05-e792-4d38-8fab-31ad591e59e1")]
#else
    [Guid("a6489d05-e792-4d38-8fab-31ad591e59e1")]
#endif
    public sealed class AssemblyUriAttribute : Attribute
    {
        /// <summary>
        /// Constructs an instance of this class with no name, recording the
        /// specified URI.
        /// </summary>
        /// <param name="uri">
        /// The URI to associate with the assembly.
        /// </param>
        public AssemblyUriAttribute(
            Uri uri
            )
            : this(null, uri)
        {
            // do nothing.
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Constructs an instance of this class, recording the specified name
        /// and URI.
        /// </summary>
        /// <param name="name">
        /// The name used to identify the URI being associated with the
        /// assembly.  This parameter may be null.
        /// </param>
        /// <param name="uri">
        /// The URI to associate with the assembly.
        /// </param>
        public AssemblyUriAttribute(
            string name,
            Uri uri
            )
        {
            this.name = name;
            this.uri = uri;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Constructs an instance of this class with no name, recording a URI
        /// parsed from the specified string.
        /// </summary>
        /// <param name="value">
        /// The string to parse into the URI to associate with the assembly.
        /// </param>
        public AssemblyUriAttribute(
            string value
            )
            : this(null, value)
        {
            // do nothing.
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Constructs an instance of this class, recording the specified name
        /// and a URI parsed from the specified string.
        /// </summary>
        /// <param name="name">
        /// The name used to identify the URI being associated with the
        /// assembly.  This parameter may be null.
        /// </param>
        /// <param name="value">
        /// The string to parse into the URI to associate with the assembly.
        /// </param>
        public AssemblyUriAttribute(
            string name,
            string value
            )
        {
            this.name = name;
            this.uri = new Uri(value);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// The name used to identify the associated URI, if any.
        /// </summary>
        private string name;

        /// <summary>
        /// Gets the name used to identify the associated URI, if any.
        /// </summary>
        public string Name
        {
            get { return name; }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// The URI associated with the assembly.
        /// </summary>
        private Uri uri;

        /// <summary>
        /// Gets the URI associated with the assembly.
        /// </summary>
        public Uri Uri
        {
            get { return uri; }
        }
    }
}
