/*
 * WrapperData.cs --
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
    /// This interface is implemented by entities that are tracked within an
    /// interpreter by a numeric token, which uniquely identifies the wrapped
    /// entity for later lookup.
    /// </summary>
    [ObjectId("eb107969-3e46-4b79-8ba7-3055c2ceb7a3")]
    public interface IWrapperData
    {
        /// <summary>
        /// Gets or sets the numeric token that uniquely identifies this entity
        /// within the interpreter.
        /// </summary>
        long Token { get; set; }
    }
}
