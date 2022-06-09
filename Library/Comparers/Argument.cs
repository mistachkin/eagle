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
    [ObjectId("7c4437db-58ea-4b74-a08b-a3ef45d4fb0b")]
    internal sealed class _Argument : IEqualityComparer<Argument>
    {
        #region Public Constructors
        public _Argument()
        {
            // do nothing.
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region IEqualityComparer<Argument> Members
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
