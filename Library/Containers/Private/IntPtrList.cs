/*
 * IntPtrList.cs --
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
using Eagle._Components.Public;
using Eagle._Constants;

#if NET_STANDARD_21
using Index = Eagle._Constants.Index;
#endif

namespace Eagle._Containers.Private
{
    [ObjectId("120b198d-4480-435f-8025-3efc21cf4856")]
    internal sealed class IntPtrList : List<IntPtr>, ICloneable
    {
        public IntPtrList()
            : base()
        {
            // do nothing.
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public IntPtrList(IEnumerable<IntPtr> collection)
            : base(collection)
        {
            // do nothing.
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public string ToString(string pattern, bool noCase)
        {
            return ParserOps<IntPtr>.ListToString(this, Index.Invalid, Index.Invalid,
                ToStringFlags.None, Characters.Space.ToString(), pattern, noCase);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        #region System.Object Overrides
        public override string ToString()
        {
            return ToString(null, false);
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////////////////////////////

        #region ICloneable Members
        public object Clone()
        {
            return new IntPtrList(this);
        }
        #endregion
    }
}
