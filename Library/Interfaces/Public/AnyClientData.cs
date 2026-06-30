/*
 * AnyClientData.cs --
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
    /// This interface represents an extended container for arbitrary,
    /// caller-supplied client data.  In addition to holding a single data
    /// value (<see cref="IClientData" />), it supports a dynamic set of named
    /// values (<see cref="IAnyData" />) with strongly typed accessors
    /// (<see cref="IAnyTypeData" /> and <see cref="IAnyValueTypeData" />),
    /// thread synchronization (<see cref="ISynchronize" />), disposal state
    /// tracking (<see cref="IMaybeDisposed" />), and the ability to attach to
    /// and detach from other instances to form a chain.
    /// </summary>
    [ObjectId("4669f1ff-0fb8-4628-bf4f-1ac1637ecc2f")]
    public interface IAnyClientData :
            IClientData, IAnyData, IAnyTypeData, IAnyValueTypeData,
            ISynchronize, IMaybeDisposed
    {
        /// <summary>
        /// Gets the instance, if any, that this instance is currently
        /// attached to.  This value may be null.
        /// </summary>
        IAnyClientData Attached { get; }

        /// <summary>
        /// Gets the instance at the root of the chain formed by following the
        /// attached instances starting from this instance.
        /// </summary>
        IAnyClientData Root { get; }

        /// <summary>
        /// Attaches this instance to the specified instance, making it the
        /// attached instance.
        /// </summary>
        /// <param name="anyClientData">
        /// The instance to attach this instance to.  This parameter should
        /// not be null.
        /// </param>
        /// <returns>
        /// True if the attach operation was successful; otherwise, false.
        /// </returns>
        bool AttachTo(IAnyClientData anyClientData);

        /// <summary>
        /// Detaches this instance from the specified instance, which must be
        /// the currently attached instance.
        /// </summary>
        /// <param name="anyClientData">
        /// The instance to detach this instance from.  This parameter should
        /// not be null.
        /// </param>
        /// <returns>
        /// True if the detach operation was successful; otherwise, false.
        /// </returns>
        bool DetachFrom(IAnyClientData anyClientData);

        /// <summary>
        /// Replaces the data held by this instance with the data held by the
        /// specified instance.
        /// </summary>
        /// <param name="anyClientData">
        /// The instance whose data should be copied into this instance.  This
        /// parameter should not be null.
        /// </param>
        /// <returns>
        /// The number of data values that were replaced.
        /// </returns>
        int ReplaceData(IAnyClientData anyClientData);

        /// <summary>
        /// Converts the named values contained by this instance into a list
        /// of name/value pairs.
        /// </summary>
        /// <returns>
        /// A list containing the names and values held by this instance.
        /// </returns>
        IStringList ToList();

        /// <summary>
        /// Converts the named values contained by this instance into a list
        /// of name/value pairs, including only those whose name matches the
        /// specified pattern.
        /// </summary>
        /// <param name="pattern">
        /// The string-matching pattern used to select which names to include,
        /// or null to include all names.
        /// </param>
        /// <param name="noCase">
        /// Non-zero if pattern matching should be case-insensitive.
        /// </param>
        /// <returns>
        /// A list containing the matching names and values held by this
        /// instance.
        /// </returns>
        IStringList ToList(string pattern, bool noCase);

        /// <summary>
        /// Converts the named values contained by this instance into a list
        /// of name/value pairs, including only those whose name matches the
        /// specified pattern.
        /// </summary>
        /// <param name="pattern">
        /// The string-matching pattern used to select which names to include,
        /// or null to include all names.
        /// </param>
        /// <param name="empty">
        /// Non-zero to include names that have an empty associated value.
        /// </param>
        /// <param name="noCase">
        /// Non-zero if pattern matching should be case-insensitive.
        /// </param>
        /// <returns>
        /// A list containing the matching names and values held by this
        /// instance.
        /// </returns>
        IStringList ToList(string pattern, bool empty, bool noCase);
    }
}
