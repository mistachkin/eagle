/*
 * ArgumentListStack.cs --
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
using Eagle._Containers.Public;

namespace Eagle._Containers.Private
{
    [ObjectId("e1b84568-804a-4fba-8b23-c5ce5b7bf878")]
    internal sealed class ArgumentListStack : Stack<ArgumentList>
    {
        #region Public Constructors
        public ArgumentListStack()
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
            return GenericOps<ArgumentList>.EnumerableToString(
                this, ToStringFlags.None, Characters.Space.ToString(),
                null, false);
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
