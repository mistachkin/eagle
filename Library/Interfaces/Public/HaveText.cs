/*
 * HaveText.cs --
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
    /// block of text, retaining both the original text and a possibly
    /// modified current version.
    /// </summary>
    [ObjectId("9ee24205-3d90-43fc-bdd9-d1f6a387c171")]
    public interface IHaveText
    {
        /// <summary>
        /// Gets or sets the original, unmodified text associated with this
        /// entity.  This value may be null.
        /// </summary>
        string OriginalText { get; set; }
        /// <summary>
        /// Gets or sets the current text associated with this entity.  This
        /// value may be null.
        /// </summary>
        string Text { get; set; }
    }
}
