/*
 * HaveFileName.cs --
 *
 * Copyright (c) 2007-2012 by Joe Mistachkin.  All rights reserved.
 *
 * See the file "license.terms" for information on usage and redistribution of
 * this file, and for a DISCLAIMER OF ALL WARRANTIES.
 *
 * RCS: @(#) $Id: $
 */

using Eagle._Attributes;

namespace Eagle._Interfaces.Public
{
    /// <summary>
    /// This interface is implemented by entities that are associated with a
    /// file name, such as the source file from which they were loaded or to
    /// which they pertain.
    /// </summary>
    [ObjectId("bef2dd92-e272-4bb1-be4c-9e628f2a1045")]
    public interface IHaveFileName
    {
        /// <summary>
        /// Gets or sets the file name associated with this entity.  This
        /// value may be null.
        /// </summary>
        string FileName { get; set; }
    }
}
