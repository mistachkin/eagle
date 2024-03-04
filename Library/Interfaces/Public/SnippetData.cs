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
    [ObjectId("16c6cff8-a535-4dbe-b95c-ca808c50d737")]
    public interface ISnippetData : IIdentifier
    {
        string Path { get; }               /* fully qualified file path, if any. */
        byte[] Bytes { get; }              /* raw script bytes, if any */
        string Text { get; }               /* script text itself, if any */
        string Xml { get; }                /* associated script certificate, if any */
        SnippetFlags SnippetFlags { get; } /* instance flags only, if any */
    }
}
