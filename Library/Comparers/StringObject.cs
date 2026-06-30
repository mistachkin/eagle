/*
 * StringObject.cs --
 *
 * Copyright (c) 2007-2012 by Joe Mistachkin.  All rights reserved.
 *
 * See the file "license.terms" for information on usage and redistribution of
 * this file, and for a DISCLAIMER OF ALL WARRANTIES.
 *
 * RCS: @(#) $Id: $
 */

using System;
using System.Collections.Generic;
using Eagle._Attributes;
using Eagle._Components.Private;

namespace Eagle._Comparers
{
    /// <summary>
    /// This class tests strings for equality by reference identity, treating two
    /// strings as equal only when they are the same object instance rather than
    /// when they merely have equal contents.
    /// </summary>
    [ObjectId("8f58d0af-017d-4234-bca7-fa7b58e26502")]
    internal sealed class StringObject : IEqualityComparer<string>
    {
        #region Public Constructors
        /// <summary>
        /// Constructs an instance of this class.
        /// </summary>
        public StringObject()
        {
            // do nothing.
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region IEqualityComparer<string> Members
        /// <summary>
        /// Determines whether two strings are the same object instance.
        /// </summary>
        /// <param name="left">
        /// The first string to compare.
        /// </param>
        /// <param name="right">
        /// The second string to compare.
        /// </param>
        /// <returns>
        /// True if both arguments refer to the same object instance; otherwise,
        /// false.
        /// </returns>
        public bool Equals(
            string left,
            string right
            )
        {
            return Object.ReferenceEquals(left, right);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Returns a hash code for the specified string based on its object
        /// identity.
        /// </summary>
        /// <param name="value">
        /// The string for which a hash code is to be computed.
        /// </param>
        /// <returns>
        /// A hash code for the specified string.
        /// </returns>
        public int GetHashCode(
            string value
            )
        {
            return RuntimeOps.GetHashCode(value);
        }
        #endregion
    }
}
