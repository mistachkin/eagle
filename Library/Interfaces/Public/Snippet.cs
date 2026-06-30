/*
 * Snippet.cs --
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
    /// This interface is implemented by entities that represent a script
    /// snippet.  It extends <see cref="ISnippetData" /> with methods to query
    /// and modify the name and state of the snippet.
    /// </summary>
    [ObjectId("53aba19d-07df-4261-a1f5-861538e17e0e")]
    public interface ISnippet : ISnippetData
    {
        /// <summary>
        /// Determines whether this snippet has a name.
        /// </summary>
        /// <returns>
        /// True if this snippet has a name; otherwise, false.
        /// </returns>
        bool HaveName();
        /// <summary>
        /// Sets the name of this snippet only if it does not already have
        /// one.
        /// </summary>
        /// <param name="name">
        /// The name to set, if this snippet does not already have one.  This
        /// parameter may be null.
        /// </param>
        void MaybeSetName(string name);
        /// <summary>
        /// Sets the name of this snippet, throwing an exception if it already
        /// has one.
        /// </summary>
        /// <param name="name">
        /// The name to set.  This parameter may be null.
        /// </param>
        void SetName(string name); /* throw */

        /// <summary>
        /// Determines whether this snippet is hidden.
        /// </summary>
        /// <returns>
        /// True if this snippet is hidden; otherwise, false.
        /// </returns>
        bool IsHidden();
        /// <summary>
        /// Marks this snippet as hidden.
        /// </summary>
        void SetHidden();

        /// <summary>
        /// Determines whether this snippet is locked.
        /// </summary>
        /// <returns>
        /// True if this snippet is locked; otherwise, false.
        /// </returns>
        bool IsLocked();
        /// <summary>
        /// Marks this snippet as locked.
        /// </summary>
        void SetLocked();

        /// <summary>
        /// Determines whether this snippet is disabled.
        /// </summary>
        /// <returns>
        /// True if this snippet is disabled; otherwise, false.
        /// </returns>
        bool IsDisabled();
        /// <summary>
        /// Marks this snippet as disabled.
        /// </summary>
        void SetDisabled();

        /// <summary>
        /// Converts this snippet to a list of its constituent values.
        /// </summary>
        /// <returns>
        /// A new <see cref="IStringList" /> representing this snippet.
        /// </returns>
        IStringList ToList();
    }
}
