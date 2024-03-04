/*
 * Snippet.cs --
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
    [ObjectId("53aba19d-07df-4261-a1f5-861538e17e0e")]
    public interface ISnippet : ISnippetData
    {
        bool HaveName();
        void MaybeSetName(string name);
        void SetName(string name); /* throw */

        bool IsHidden();
        void SetHidden();

        bool IsLocked();
        void SetLocked();

        bool IsDisabled();
        void SetDisabled();

        IStringList ToList();
    }
}
