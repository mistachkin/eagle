/*
 * BaseClientData.cs --
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
    /// This interface defines the base contract for objects that carry opaque,
    /// caller-supplied data through the engine.  It exposes the underlying data
    /// value, whether that value is read-only, and an optional associated
    /// logging data object.
    /// </summary>
    [ObjectId("30e24a85-0fcb-449a-811a-585ad8e0c03c")]
    public interface IBaseClientData
    {
        /// <summary>
        /// Gets or sets the underlying data value carried by this object,
        /// without raising an exception when the value is read-only.  This
        /// value may be null.
        /// </summary>
        object DataNoThrow { get; set; }
        /// <summary>
        /// Gets a value indicating whether the underlying data value is
        /// read-only.
        /// </summary>
        bool ReadOnly { get; }

        /// <summary>
        /// Gets or sets the client data used for logging purposes that is
        /// associated with this object.  This value may be null.
        /// </summary>
        IClientData Log { get; set; }
    }
}
