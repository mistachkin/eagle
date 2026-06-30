/*
 * AnyDataBase.cs --
 *
 * Copyright (c) 2007-2012 by Joe Mistachkin.  All rights reserved.
 *
 * See the file "license.terms" for information on usage and redistribution of
 * this file, and for a DISCLAIMER OF ALL WARRANTIES.
 *
 * RCS: @(#) $Id: $
 */

using System.Collections.Generic;
using Eagle._Attributes;
using Eagle._Components.Public;

namespace Eagle._Interfaces.Public
{
    /// <summary>
    /// This interface defines the lower-level methods for managing a dynamic
    /// collection of named values, including explicit error reporting and
    /// fine-grained control over how values are set.  It serves as the base
    /// contract for <see cref="IAnyData" />.
    /// </summary>
    [ObjectId("1a040ae5-b312-478b-b69e-ebfc6e66be08")]
    public interface IAnyDataBase
    {
        /// <summary>
        /// Removes all named values currently held by this instance.
        /// </summary>
        /// <param name="error">
        /// Upon failure, receives information about the error that occurred.
        /// </param>
        /// <returns>
        /// True if the named values were successfully reset; otherwise,
        /// false.
        /// </returns>
        bool TryResetAny(
            ref Result error
            );

        /// <summary>
        /// Determines whether a named value with the specified name is
        /// present.
        /// </summary>
        /// <param name="name">
        /// The name of the value to check for.  This parameter should not be
        /// null.
        /// </param>
        /// <param name="hasAny">
        /// Upon success, receives non-zero if a value with the specified name
        /// is present.
        /// </param>
        /// <param name="error">
        /// Upon failure, receives information about the error that occurred.
        /// </param>
        /// <returns>
        /// True if the presence check was successfully performed; otherwise,
        /// false.
        /// </returns>
        bool TryHasAny(
            string name,
            ref bool hasAny,
            ref Result error
            );

        /// <summary>
        /// Gets the list of names, optionally matching a pattern, for the
        /// named values held by this instance.
        /// </summary>
        /// <param name="pattern">
        /// The string-matching pattern used to select which names to include,
        /// or null to include all names.
        /// </param>
        /// <param name="noCase">
        /// Non-zero if pattern matching should be case-insensitive.
        /// </param>
        /// <param name="list">
        /// Upon success, receives the list of matching names.
        /// </param>
        /// <param name="error">
        /// Upon failure, receives information about the error that occurred.
        /// </param>
        /// <returns>
        /// True if the list of names was successfully obtained; otherwise,
        /// false.
        /// </returns>
        bool TryListAny(
            string pattern,
            bool noCase,
            ref IList<string> list,
            ref Result error
            );

        /// <summary>
        /// Attempts to get the value associated with the specified name.
        /// </summary>
        /// <param name="name">
        /// The name of the value to get.  This parameter should not be null.
        /// </param>
        /// <param name="value">
        /// Upon success, receives the value associated with the specified
        /// name.
        /// </param>
        /// <param name="error">
        /// Upon failure, receives information about the error that occurred.
        /// </param>
        /// <returns>
        /// True if the value was successfully obtained; otherwise, false.
        /// </returns>
        bool TryGetAny(
            string name,
            out object value,
            ref Result error
            );

        /// <summary>
        /// Attempts to set the value associated with the specified name.
        /// </summary>
        /// <param name="name">
        /// The name of the value to set.  This parameter should not be null.
        /// </param>
        /// <param name="value">
        /// The value to associate with the specified name.  This parameter
        /// may be null.
        /// </param>
        /// <param name="overwrite">
        /// Non-zero to allow an existing value with the same name to be
        /// overwritten.
        /// </param>
        /// <param name="create">
        /// Non-zero to allow a new value to be created when one does not
        /// already exist.
        /// </param>
        /// <param name="toString">
        /// Non-zero to store the value as its string representation.
        /// </param>
        /// <param name="error">
        /// Upon failure, receives information about the error that occurred.
        /// </param>
        /// <returns>
        /// True if the value was successfully set; otherwise, false.
        /// </returns>
        bool TrySetAny(
            string name,
            object value,
            bool overwrite,
            bool create,
            bool toString,
            ref Result error
            );

        /// <summary>
        /// Attempts to remove the value associated with the specified name.
        /// </summary>
        /// <param name="name">
        /// The name of the value to remove.  This parameter should not be
        /// null.
        /// </param>
        /// <param name="error">
        /// Upon failure, receives information about the error that occurred.
        /// </param>
        /// <returns>
        /// True if the value was successfully removed; otherwise, false.
        /// </returns>
        bool TryUnsetAny(
            string name,
            ref Result error
            );
    }
}
