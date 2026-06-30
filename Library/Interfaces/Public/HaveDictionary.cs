/*
 * HaveDictionary.cs --
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
    /// This interface is implemented by entities that expose a dictionary-like
    /// store of named values, allowing values to be queried and set by name.
    /// </summary>
    /// <typeparam name="T">
    /// The type of the values stored by name.
    /// </typeparam>
    [ObjectId("bc1fd9b9-26bc-41fe-a663-076c4e38c051")]
    public interface IHaveDictionary<T>
    {
        /// <summary>
        /// Gets the value stored under the specified name.
        /// </summary>
        /// <param name="name">
        /// The name of the value to query.  This parameter should not be null.
        /// </param>
        /// <returns>
        /// The value stored under the specified name.
        /// </returns>
        T GetNamedValue(string name);
        /// <summary>
        /// Sets the value stored under the specified name.
        /// </summary>
        /// <param name="name">
        /// The name of the value to set.  This parameter should not be null.
        /// </param>
        /// <param name="value">
        /// The value to store under the specified name.
        /// </param>
        void SetNamedValue(string name, T value);
    }
}
