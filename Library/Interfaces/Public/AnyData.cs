/*
 * AnyData.cs --
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
    /// This interface defines a simplified set of methods for managing a
    /// dynamic collection of named values, layered on top of the lower-level
    /// <see cref="IAnyDataBase" /> contract.  These methods omit the explicit
    /// error reporting and extra options of their base counterparts.
    /// </summary>
    [ObjectId("95d45bb8-bbc5-4eaf-ad12-2b65bc08cab4")]
    public interface IAnyData : IAnyDataBase
    {
        /// <summary>
        /// Removes all named values currently held by this instance.
        /// </summary>
        /// <returns>
        /// True if the named values were successfully reset; otherwise,
        /// false.
        /// </returns>
        bool TryResetAny();

        /// <summary>
        /// Determines whether a named value with the specified name is
        /// present.
        /// </summary>
        /// <param name="name">
        /// The name of the value to check for.  This parameter should not be
        /// null.
        /// </param>
        /// <returns>
        /// True if a value with the specified name is present; otherwise,
        /// false.
        /// </returns>
        bool HasAny(
            string name
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
        /// <returns>
        /// True if the value was successfully obtained; otherwise, false.
        /// </returns>
        bool TryGetAny(
            string name,
            out object value
            );

        /// <summary>
        /// Attempts to set the value associated with the specified name,
        /// creating it if necessary.
        /// </summary>
        /// <param name="name">
        /// The name of the value to set.  This parameter should not be null.
        /// </param>
        /// <param name="value">
        /// The value to associate with the specified name.  This parameter
        /// may be null.
        /// </param>
        /// <returns>
        /// True if the value was successfully set; otherwise, false.
        /// </returns>
        bool TrySetAny(
            string name,
            object value
            );

        /// <summary>
        /// Attempts to remove the value associated with the specified name.
        /// </summary>
        /// <param name="name">
        /// The name of the value to remove.  This parameter should not be
        /// null.
        /// </param>
        /// <returns>
        /// True if the value was successfully removed; otherwise, false.
        /// </returns>
        bool TryUnsetAny(
            string name
            );
    }
}
