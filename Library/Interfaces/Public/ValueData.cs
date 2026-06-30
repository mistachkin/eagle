/*
 * ValueData.cs --
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
    /// This interface is implemented by entities that carry extra, engine-level
    /// data alongside a value, including a pair of opaque client data slots and
    /// the call frame associated with the value.
    /// </summary>
    [ObjectId("a1b1f063-bd8c-4c64-a6b8-f497892320f2")]
    public interface IValueData : IHaveClientData
    {
        //
        // WARNING: This property is for core/engine use only.
        //
        /// <summary>
        /// Gets or sets the opaque, engine-specific client data associated with
        /// this value.  This value may be null.
        /// </summary>
        IClientData ValueData { get; set; }

        //
        // WARNING: This property is for core/engine use only.
        //
        /// <summary>
        /// Gets or sets the extra, opaque client data associated with this
        /// value.  This value may be null.
        /// </summary>
        IClientData ExtraData { get; set; }

        //
        // WARNING: This property is for core/engine use only.
        //
        /// <summary>
        /// Gets or sets the call frame associated with this value.  This value
        /// may be null.
        /// </summary>
        ICallFrame CallFrame { get; set; }
    }
}
