/*
 * Object.cs --
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
    /// This interface is implemented by entities that represent an opaque
    /// object handle managed by an Eagle interpreter.  It composes the object
    /// metadata (<see cref="IObjectData" />), value access
    /// (<see cref="IValue" /> and <see cref="IValueData" />), and disposal
    /// state (<see cref="IMaybeDisposed" />), and adds the reference-count
    /// management used to govern the lifetime of the wrapped object.
    /// </summary>
    [ObjectId("0a0f31fa-bb82-4cbe-9ef2-0c0718ac9c3d")]
    public interface IObject : IObjectData, IValue, IValueData, IMaybeDisposed
    {
        /// <summary>
        /// Increments the reference count for this object.
        /// </summary>
        /// <returns>
        /// The updated reference count.
        /// </returns>
        int AddReference();
        /// <summary>
        /// Decrements the reference count for this object.
        /// </summary>
        /// <returns>
        /// The updated reference count.
        /// </returns>
        int RemoveReference();

        /// <summary>
        /// Increments the temporary reference count for this object.
        /// </summary>
        /// <returns>
        /// The updated temporary reference count.
        /// </returns>
        int AddTemporaryReference();
        /// <summary>
        /// Decrements the temporary reference count for this object.
        /// </summary>
        /// <returns>
        /// The updated temporary reference count.
        /// </returns>
        int RemoveTemporaryReference();

        /// <summary>
        /// Removes all temporary references to this object.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter context this object belongs to.  This parameter
        /// should not be null.
        /// </param>
        /// <param name="name">
        /// The name of the object whose temporary references are to be
        /// removed.
        /// </param>
        /// <param name="finalCount">
        /// Upon return, receives the final reference count for this object.
        /// </param>
        /// <returns>
        /// True if the temporary references were removed; otherwise, false.
        /// </returns>
        bool RemoveTemporaryReferences(
            Interpreter interpreter,
            string name,
            ref int finalCount
        );
    }
}
