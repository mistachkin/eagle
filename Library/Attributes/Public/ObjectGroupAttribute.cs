/*
 * ObjectGroupAttribute.cs --
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
    /// This attribute is used to associate a group name with the class it is
    /// applied to.  It may be applied more than once to the same class.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = true, Inherited = false)]
    [ObjectId("8d4a0df8-1942-41ab-94e6-774c74d58db4")]
    public sealed class ObjectGroupAttribute : Attribute
    {
        /// <summary>
        /// Constructs an instance of this class using the specified group
        /// name.
        /// </summary>
        /// <param name="value">
        /// The group name to associate with the marked class.
        /// </param>
        public ObjectGroupAttribute(string value)
        {
            group = value;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// The group name associated with the marked class.
        /// </summary>
        private string group;
        /// <summary>
        /// Gets the group name associated with the marked class.
        /// </summary>
        public string Group
        {
            get { return group; }
        }
    }
}
