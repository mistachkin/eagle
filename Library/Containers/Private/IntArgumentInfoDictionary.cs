/*
 * IntArgumentInfoDictionary.cs --
 *
 * Copyright (c) 2007-2012 by Joe Mistachkin.  All rights reserved.
 *
 * See the file "license.terms" for information on usage and redistribution of
 * this file, and for a DISCLAIMER OF ALL WARRANTIES.
 *
 * RCS: @(#) $Id: $
 */

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
    [ObjectId("a4e59dc4-3b6c-4f41-a633-8b03a086e6b0")]
    internal sealed class IntArgumentInfoDictionary :
            Dictionary<int, ArgumentInfo>
    {
        #region Public Constructors
        public IntArgumentInfoDictionary()
            : base()
        {
            // do nothing.
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region ToString Methods
        public string ToString(
            string pattern,
            bool noCase
            )
        {
            return ParserOps<ArgumentInfo>.ListToString(
                new ArgumentInfoList(this.Values),
                Index.Invalid, Index.Invalid, ToStringFlags.None,
                Characters.Space.ToString(), pattern, noCase);
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region System.Object Overrides
        public override string ToString()
        {
            return ToString(null, false);
        }
        #endregion
    }
}
