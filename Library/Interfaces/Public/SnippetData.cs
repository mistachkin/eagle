/*
 * SnippetData.cs --
 *
 * Copyright (c) 2007-2012 by Joe Mistachkin.  All rights reserved.
 *
 * See the file "license.terms" for information on usage and redistribution of
 * this file, and for a DISCLAIMER OF ALL WARRANTIES.
 *
 * RCS: @(#) $Id: $
 */

using Eagle._Attributes;
using Eagle._Components.Public;

namespace Eagle._Interfaces.Public
{
    /// <summary>
    /// This interface is implemented by entities that contain the data
    /// associated with a script snippet, including its source location,
    /// content, and flags.
    /// </summary>
    [ObjectId("16c6cff8-a535-4dbe-b95c-ca808c50d737")]
    public interface ISnippetData : IIdentifier
    {
        /// <summary>
        /// Gets the fully qualified file path associated with this snippet,
        /// if any.  This value may be null.
        /// </summary>
        string Path { get; }               /* fully qualified file path, if any. */
        /// <summary>
        /// Gets the raw script bytes associated with this snippet, if any.
        /// This value may be null.
        /// </summary>
        byte[] Bytes { get; }              /* raw script bytes, if any */
        /// <summary>
        /// Gets the script text associated with this snippet, if any.  This
        /// value may be null.
        /// </summary>
        string Text { get; }               /* script text itself, if any */
        /// <summary>
        /// Gets the script certificate, in XML format, associated with this
        /// snippet, if any.  This value may be null.
        /// </summary>
        string Xml { get; }                /* associated script certificate, if any */
        /// <summary>
        /// Gets the per-instance <see cref="SnippetFlags" /> associated with
        /// this snippet, if any.
        /// </summary>
        SnippetFlags SnippetFlags { get; } /* instance flags only, if any */
    }
}
