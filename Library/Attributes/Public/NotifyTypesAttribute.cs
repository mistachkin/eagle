/*
 * NotifyTypesAttribute.cs --
 *
 * Copyright (c) 2007-2012 by Joe Mistachkin.  All rights reserved.
 *
 * See the file "license.terms" for information on usage and redistribution of
 * this file, and for a DISCLAIMER OF ALL WARRANTIES.
 *
 * RCS: @(#) $Id: $
 */

using System;
using Eagle._Components.Public;

namespace Eagle._Attributes
{
    /// <summary>
    /// This attribute is used to associate a set of notification types with
    /// the class it is applied to.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, Inherited = false)]
    [ObjectId("ff8808af-a151-4d6c-a6c8-3f93bc219de4")]
    public sealed class NotifyTypesAttribute : Attribute
    {
        /// <summary>
        /// Constructs an instance of this class using the specified
        /// notification types.
        /// </summary>
        /// <param name="types">
        /// The notification types to associate with the marked class.
        /// </param>
        public NotifyTypesAttribute(NotifyType types)
        {
            this.types = types;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Constructs an instance of this class using the string
        /// representation of the notification types.
        /// </summary>
        /// <param name="value">
        /// The notification types, as a string, to associate with the marked
        /// class.  This value must be parsable as a value of the
        /// <see cref="NotifyType" /> enumeration.
        /// </param>
        public NotifyTypesAttribute(string value)
        {
            types = (NotifyType)Enum.Parse(
                typeof(NotifyType), value); /* throw */
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// The notification types associated with the marked class.
        /// </summary>
        private NotifyType types;
        /// <summary>
        /// Gets the notification types associated with the marked class.
        /// </summary>
        public NotifyType Types
        {
            get { return types; }
        }
    }
}
