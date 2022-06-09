/*
 * Alias.cs --
 *
 * Copyright (c) 2007-2012 by Joe Mistachkin.  All rights reserved.
 *
 * See the file "license.terms" for information on usage and redistribution of
 * this file, and for a DISCLAIMER OF ALL WARRANTIES.
 *
 * RCS: @(#) $Id: $
 */

using Eagle._Attributes;
using Eagle._Components.Public.Delegates;

namespace Eagle._Interfaces.Public
{
    [ObjectId("3ca04f8b-d5e7-4bc3-a109-fc3d5f1eb53d")]
    public interface IAlias : IAliasData
    {
        DisposeCallback PostInterpreterDisposed { get; }
    }
}
