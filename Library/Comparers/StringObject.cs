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
    [ObjectId("8f58d0af-017d-4234-bca7-fa7b58e26502")]
    internal sealed class StringObject : IEqualityComparer<string>
    {
        #region Public Constructors
        public StringObject()
        {
            // do nothing.
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region IEqualityComparer<string> Members
        public bool Equals(
            string left,
            string right
            )
        {
            return Object.ReferenceEquals(left, right);
        }

        ///////////////////////////////////////////////////////////////////////

        public int GetHashCode(
            string value
            )
        {
            return RuntimeOps.GetHashCode(value);
        }
        #endregion
    }
}
