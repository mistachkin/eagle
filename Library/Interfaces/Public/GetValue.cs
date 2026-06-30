/*
 * GetValue.cs --
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
    /// This interface is implemented by entities that expose read-only access
    /// to an underlying value along with its length and string representation.
    /// </summary>
    [ObjectId("291117b5-f1ef-4945-a983-fc01a1b5447f")]
    public interface IGetValue
    {
        /// <summary>
        /// Gets the underlying value.  This value may be null.
        /// </summary>
        object Value { get; }
        /// <summary>
        /// Gets the length associated with this value (e.g. the number of
        /// characters in its string representation).
        /// </summary>
        int Length { get; }
        /// <summary>
        /// Gets the string representation of this value.  This value may be
        /// null.
        /// </summary>
        string String { get; }
    }
}
