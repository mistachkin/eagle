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
    [ObjectId("9ee24205-3d90-43fc-bdd9-d1f6a387c171")]
    public interface IHaveText
    {
        string OriginalText { get; set; }
        string Text { get; set; }
    }
}
