/*
 * Interpreter.cs --
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
using Eagle._Interfaces.Public;

namespace Eagle._Comparers
{
    /// <summary>
    /// This class provides comparison and equality comparison for
    /// <see cref="IInterpreter" /> instances, based on their interpreter
    /// identifiers.
    /// </summary>
    [ObjectId("79ebb9fb-9a30-48da-9ed6-1505c6aba10f")]
    internal sealed class _Interpreter :
            IComparer<IInterpreter>, IEqualityComparer<IInterpreter>
    {
        #region Public Constructors
        /// <summary>
        /// Constructs an instance of this class.
        /// </summary>
        public _Interpreter()
        {
            // do nothing.
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region IComparer<IInterpreter> Members
        /// <summary>
        /// This method compares two <see cref="IInterpreter" /> instances by
        /// their interpreter identifiers.
        /// </summary>
        /// <param name="left">
        /// The first <see cref="IInterpreter" /> instance to compare. This
        /// parameter may be null.
        /// </param>
        /// <param name="right">
        /// The second <see cref="IInterpreter" /> instance to compare. This
        /// parameter may be null.
        /// </param>
        /// <returns>
        /// Zero if the instances are equal, a negative number if
        /// <paramref name="left" /> sorts before <paramref name="right" />, or a
        /// positive number if <paramref name="left" /> sorts after
        /// <paramref name="right" />. A null instance sorts before a non-null
        /// instance.
        /// </returns>
        public int Compare(
            IInterpreter left,
            IInterpreter right
            )
        {
            if ((left == null) && (right == null))
            {
                return 0;
            }
            else if (left == null)
            {
                return -1;
            }
            else if (right == null)
            {
                return 1;
            }
            else
            {
                return Comparer<long>.Default.Compare(
                    left.IdNoThrow, right.IdNoThrow);
            }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region IEqualityComparer<IInterpreter> Members
        /// <summary>
        /// This method determines whether two <see cref="IInterpreter" />
        /// instances are equal, based on their interpreter identifiers.
        /// </summary>
        /// <param name="left">
        /// The first <see cref="IInterpreter" /> instance to compare. This
        /// parameter may be null.
        /// </param>
        /// <param name="right">
        /// The second <see cref="IInterpreter" /> instance to compare. This
        /// parameter may be null.
        /// </param>
        /// <returns>
        /// True if the two instances are equal; otherwise, false.
        /// </returns>
        public bool Equals(
            IInterpreter left,
            IInterpreter right
            )
        {
            if ((left == null) && (right == null))
            {
                return true;
            }
            else if (left == null)
            {
                return false;
            }
            else if (right == null)
            {
                return false;
            }
            else
            {
                return EqualityComparer<long>.Default.Equals(
                    left.IdNoThrow, right.IdNoThrow);
            }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method returns a hash code for the specified
        /// <see cref="IInterpreter" /> instance, based on its interpreter
        /// identifier.
        /// </summary>
        /// <param name="value">
        /// The <see cref="IInterpreter" /> instance to compute a hash code for.
        /// This parameter may be null.
        /// </param>
        /// <returns>
        /// A hash code for the specified instance, or zero if it is null.
        /// </returns>
        public int GetHashCode(
            IInterpreter value
            )
        {
            return (value != null) ? value.GetHashCodeNoThrow() : 0;
        }
        #endregion
    }
}
