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
    /// <summary>
    /// This interface represents an alias, a named entity in one interpreter
    /// that forwards its invocation to a target entity, possibly residing in
    /// another interpreter, optionally supplying additional leading
    /// arguments.  It extends <see cref="IAliasData" /> with the runtime
    /// callback used when the associated interpreter is disposed.
    /// </summary>
    [ObjectId("3ca04f8b-d5e7-4bc3-a109-fc3d5f1eb53d")]
    public interface IAlias : IAliasData
    {
        /// <summary>
        /// Gets the callback, if any, to be invoked after the interpreter
        /// associated with this alias has been disposed.  This value may be
        /// null.
        /// </summary>
        DisposeCallback PostInterpreterDisposed { get; }
    }
}
