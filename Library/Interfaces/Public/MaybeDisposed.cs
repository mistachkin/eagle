/*
 * MaybeDisposed.cs --
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
    /// This interface is implemented by entities that can report their
    /// disposal status, indicating whether they have been disposed or are in
    /// the process of being disposed.
    /// </summary>
    [ObjectId("a9546e47-8bab-484c-a7e6-e3ea30aa793f")]
    public interface IMaybeDisposed
    {
        /// <summary>
        /// Gets a value indicating whether this object has been disposed.
        /// </summary>
        //
        // WARNING: This property may not throw exceptions.
        //
        [Throw(false)]
        bool Disposed { get; }

        /// <summary>
        /// Gets a value indicating whether this object is currently in the
        /// process of being disposed.
        /// </summary>
        //
        // WARNING: This property may not throw exceptions.
        //
        [Throw(false)]
        bool Disposing { get; }
    }
}
