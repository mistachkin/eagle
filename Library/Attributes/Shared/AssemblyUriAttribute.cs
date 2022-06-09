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
    [AttributeUsage(AttributeTargets.Assembly,
        AllowMultiple = true, Inherited = false)]
#if EAGLE
    [ObjectId("a6489d05-e792-4d38-8fab-31ad591e59e1")]
#else
    [Guid("a6489d05-e792-4d38-8fab-31ad591e59e1")]
#endif
    public sealed class AssemblyUriAttribute : Attribute
    {
        public AssemblyUriAttribute(
            Uri uri
            )
            : this(null, uri)
        {
            // do nothing.
        }

        ///////////////////////////////////////////////////////////////////////

        public AssemblyUriAttribute(
            string name,
            Uri uri
            )
        {
            this.name = name;
            this.uri = uri;
        }

        ///////////////////////////////////////////////////////////////////////

        public AssemblyUriAttribute(
            string value
            )
            : this(null, value)
        {
            // do nothing.
        }

        ///////////////////////////////////////////////////////////////////////

        public AssemblyUriAttribute(
            string name,
            string value
            )
        {
            this.name = name;
            this.uri = new Uri(value);
        }

        ///////////////////////////////////////////////////////////////////////

        private string name;
        public string Name
        {
            get { return name; }
        }

        ///////////////////////////////////////////////////////////////////////

        private Uri uri;
        public Uri Uri
        {
            get { return uri; }
        }
    }
}
