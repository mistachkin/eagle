/*
 * Value.cs --
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
    /// This interface is implemented by entities that expose read-write access
    /// to an underlying value.  It combines the read-only access of
    /// <see cref="IGetValue" /> with the write-only access of
    /// <see cref="ISetValue" /> into a single, settable value property.
    /// </summary>
    [ObjectId("faa4913e-e3d6-4b75-bcaa-4773a2fc41f0")]
    public interface IValue : IGetValue, ISetValue
    {
        /// <summary>
        /// Gets or sets the underlying value.  This value may be null.  This
        /// member hides the read-only <see cref="IGetValue.Value" /> and
        /// write-only <see cref="ISetValue.Value" /> properties to provide
        /// unified read-write access.
        /// </summary>
        new object Value { get; set; }
    }
}
