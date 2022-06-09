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
    [ObjectId("79ebb9fb-9a30-48da-9ed6-1505c6aba10f")]
    internal sealed class _Interpreter :
            IComparer<IInterpreter>, IEqualityComparer<IInterpreter>
    {
        #region Public Constructors
        public _Interpreter()
        {
            // do nothing.
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region IComparer<IInterpreter> Members
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

        public int GetHashCode(
            IInterpreter value
            )
        {
            return (value != null) ? value.GetHashCodeNoThrow() : 0;
        }
        #endregion
    }
}
