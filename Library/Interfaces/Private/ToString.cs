/*
 * ToString.cs --
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

namespace Eagle._Interfaces.Private
{
    /// <summary>
    /// This interface is implemented by entities that support producing one or
    /// more customized string representations of themselves, controlled by
    /// flags, a format specifier, and/or length and strictness constraints.
    /// </summary>
    [ObjectId("235f14fd-0a1a-4f86-a5b1-a3237f1bfc88")]
    internal interface IToString
    {
        /// <summary>
        /// This method produces a string representation of this entity using
        /// the specified flags.
        /// </summary>
        /// <param name="flags">
        /// The flags used to control how the string representation is produced.
        /// </param>
        /// <returns>
        /// The string representation of this entity.
        /// </returns>
        string ToString(ToStringFlags flags);

        /// <summary>
        /// This method produces a string representation of this entity using
        /// the specified flags, returning a caller-supplied default value when
        /// no representation is available.
        /// </summary>
        /// <param name="flags">
        /// The flags used to control how the string representation is produced.
        /// </param>
        /// <param name="default">
        /// The value to return when no string representation is available.
        /// </param>
        /// <returns>
        /// The string representation of this entity, or the value of the
        /// <paramref name="default" /> parameter if no representation is
        /// available.
        /// </returns>
        string ToString(ToStringFlags flags, string @default);

        /// <summary>
        /// This method produces a string representation of this entity using
        /// the specified format specifier.
        /// </summary>
        /// <param name="format">
        /// The format specifier used to control how the string representation
        /// is produced.
        /// </param>
        /// <returns>
        /// The string representation of this entity.
        /// </returns>
        string ToString(string format);

        /// <summary>
        /// This method produces a string representation of this entity using
        /// the specified format specifier, subject to a maximum length and an
        /// optional strictness constraint.
        /// </summary>
        /// <param name="format">
        /// The format specifier used to control how the string representation
        /// is produced.
        /// </param>
        /// <param name="limit">
        /// The maximum length, in characters, of the resulting string
        /// representation.
        /// </param>
        /// <param name="strict">
        /// Non-zero if the length limit must be enforced strictly; otherwise,
        /// zero.
        /// </param>
        /// <returns>
        /// The string representation of this entity.
        /// </returns>
        string ToString(string format, int limit, bool strict);
    }
}
