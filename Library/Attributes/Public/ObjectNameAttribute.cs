/*
 * ObjectNameAttribute.cs --
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
    /// well-known name with the class or delegate type it marks.  This name
    /// is used to reliably recognize the marked type regardless of its actual
    /// type name.
    /// </summary>
    [AttributeUsage(
        AttributeTargets.Class | AttributeTargets.Delegate,
        Inherited = false)]
    [ObjectId("4000ce04-6adc-4560-8d59-7f1eb5186c68")]
    public sealed class ObjectNameAttribute : Attribute
    {
        /// <summary>
        /// Constructs an instance of this attribute using the specified name.
        /// </summary>
        /// <param name="value">
        /// The name to associate with the marked type.
        /// </param>
        public ObjectNameAttribute(string value)
        {
            name = value;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// The name associated with the marked type.
        /// </summary>
        private string name;
        /// <summary>
        /// Gets the name associated with the marked type.
        /// </summary>
        public string Name
        {
            get { return name; }
        }
    }
}
