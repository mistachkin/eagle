/*
 * ThreadContext.cs --
 *
 * Copyright (c) 2007-2012 by Joe Mistachkin.  All rights reserved.
 *
 * See the file "license.terms" for information on usage and redistribution of
 * this file, and for a DISCLAIMER OF ALL WARRANTIES.
 *
 * RCS: @(#) $Id: $
 */

using Eagle._Attributes;
using Eagle._Interfaces.Public;

namespace Eagle._Interfaces.Private
{
    /// <summary>
    /// This interface is implemented by per-thread context objects associated
    /// with an interpreter.  It provides access to the identity of the thread
    /// that owns the context and, by way of the interfaces it extends, the
    /// ability to query whether the context has been disposed and to obtain its
    /// owning interpreter.
    /// </summary>
    [ObjectId("3cd21398-740b-4986-a86f-e7ad4738d322")]
    internal interface IThreadContext : IMaybeDisposed, IGetInterpreter
    {
        //
        // WARNING: This property may not throw exceptions.
        //
        /// <summary>
        /// Gets the identifier of the thread that owns this context.
        /// </summary>
        [Throw(false)]
        long ThreadId { get; }
    }
}
