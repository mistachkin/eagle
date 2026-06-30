/*
 * HaveStringBuilder.cs --
 *
 * Copyright (c) 2007-2012 by Joe Mistachkin.  All rights reserved.
 *
 * See the file "license.terms" for information on usage and redistribution of
 * this file, and for a DISCLAIMER OF ALL WARRANTIES.
 *
 * RCS: @(#) $Id: $
 */

using System.Text;
using Eagle._Attributes;
using Eagle._Containers.Public;

namespace Eagle._Interfaces.Private
{
    /// <summary>
    /// This interface is implemented by entities that are backed by a
    /// <see cref="StringBuilder" /> and provide coordinated read-only and
    /// read-write access to it.  It exposes the backing builder, along with
    /// the bookkeeping needed to track and release outstanding read-write
    /// and read-only access to it.
    /// </summary>
    [ObjectId("5f4db407-e0e8-42f6-8b7f-8905c2a57eec")]
    internal interface IHaveStringBuilder
    {
        /// <summary>
        /// Gets the unique identifier associated with this instance.
        /// </summary>
        long Id { get; }

        /// <summary>
        /// Gets the number of outstanding read-write accesses to the backing
        /// string builder.
        /// </summary>
        int ReadWriteCount { get; }

        /// <summary>
        /// Gets or sets the list of arguments associated with this instance.
        /// </summary>
        ArgumentList Arguments { get; set; }

        /// <summary>
        /// Gets or sets the string builder that backs this instance.
        /// </summary>
        StringBuilder Builder { get; set; }

        /// <summary>
        /// Gets the backing string builder for read-write access, tracking the
        /// access so it can later be released via
        /// <see cref="DoneWithReadWrite" />.
        /// </summary>
        StringBuilder BuilderForReadWrite { get; }

        /// <summary>
        /// Gets the backing string builder for read-only access, tracking the
        /// access so it can later be released via
        /// <see cref="DoneWithReadOnly" />.
        /// </summary>
        StringBuilder BuilderForReadOnly { get; }

        /// <summary>
        /// This method releases a read-write access to the backing string
        /// builder previously obtained via
        /// <see cref="BuilderForReadWrite" />.
        /// </summary>
        void DoneWithReadWrite();

        /// <summary>
        /// This method releases a read-only access to the backing string
        /// builder previously obtained via <see cref="BuilderForReadOnly" />.
        /// </summary>
        void DoneWithReadOnly();
    }
}
