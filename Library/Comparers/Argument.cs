/*
 * Argument.cs --
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
using Eagle._Components.Public;

namespace Eagle._Comparers
{
    /// <summary>
    /// This class provides an equality comparer for
    /// <see cref="Argument" /> instances, using their natural equality and hash
    /// code semantics.
    /// </summary>
    [ObjectId("7c4437db-58ea-4b74-a08b-a3ef45d4fb0b")]
    internal sealed class _Argument : IEqualityComparer<Argument>
    {
        #region Public Constructors
        /// <summary>
        /// Constructs an instance of this class.
        /// </summary>
        public _Argument()
        {
            // do nothing.
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region IEqualityComparer<Argument> Members
        /// <summary>
        /// This method determines whether two <see cref="Argument" /> instances
        /// are equal.
        /// </summary>
        /// <param name="left">
        /// The first <see cref="Argument" /> instance to compare. This
        /// parameter may be null.
        /// </param>
        /// <param name="right">
        /// The second <see cref="Argument" /> instance to compare. This
        /// parameter may be null.
        /// </param>
        /// <returns>
        /// True if the two instances are equal; otherwise, false.
        /// </returns>
        public bool Equals(
            Argument left,
            Argument right
            )
        {
            if (Object.ReferenceEquals(left, right))
                return true;

            if ((left == null) || (right == null))
                return false;

            return left.Equals(right);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method returns a hash code for the specified
        /// <see cref="Argument" /> instance.
        /// </summary>
        /// <param name="value">
        /// The <see cref="Argument" /> instance to compute a hash code for.
        /// This parameter may be null.
        /// </param>
        /// <returns>
        /// A hash code for the specified instance, or zero if it is null.
        /// </returns>
        public int GetHashCode(
            Argument value
            )
        {
            if (value == null)
                return 0;

            return value.GetHashCode();
        }
        #endregion
    }
}
