/*
 * ObjectIdAttribute.cs --
 *
 * Copyright (c) 2007-2012 by Joe Mistachkin.  All rights reserved.
 *
 * See the file "license.terms" for information on usage and redistribution of
 * this file, and for a DISCLAIMER OF ALL WARRANTIES.
 *
 * RCS: @(#) $Id: $
 */

using System;

namespace Eagle._Attributes
{
    /// <summary>
    /// This class implements an attribute used to associate a stable,
    /// globally-unique identifier with the type or member it marks.  This
    /// identifier remains constant across builds and is used to reliably
    /// recognize types and members regardless of their names.
    /// </summary>
    [AttributeUsage(AttributeTargets.All, Inherited = false)]
    [ObjectId("e79f9b7d-808a-4093-ac58-800a9bbca609")]
    public sealed class ObjectIdAttribute : Attribute
    {
        /// <summary>
        /// Constructs an instance of this attribute using the specified
        /// unique identifier.
        /// </summary>
        /// <param name="id">
        /// The unique identifier to associate with the marked type or member.
        /// </param>
        public ObjectIdAttribute(Guid id)
        {
            this.id = id;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Constructs an instance of this attribute using the specified
        /// unique identifier in its string form.
        /// </summary>
        /// <param name="value">
        /// The string representation of the unique identifier to associate
        /// with the marked type or member.  An exception is thrown if this
        /// value cannot be parsed as a valid identifier.
        /// </param>
        public ObjectIdAttribute(string value)
        {
            id = new Guid(value); /* throw */
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// The unique identifier associated with the marked type or member.
        /// </summary>
        private Guid id;
        /// <summary>
        /// Gets the unique identifier associated with the marked type or
        /// member.
        /// </summary>
        public Guid Id
        {
            get { return id; }
        }
    }
}
