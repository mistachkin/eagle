/*
 * ToplevelDictionary.cs --
 *
 * Copyright (c) 2007-2012 by Joe Mistachkin.  All rights reserved.
 *
 * See the file "license.terms" for information on usage and redistribution of
 * this file, and for a DISCLAIMER OF ALL WARRANTIES.
 *
 * RCS: @(#) $Id: $
 */

///////////////////////////////////////////////////////////////////////////////////////////////
// *WARNING* *WARNING* *WARNING* *WARNING* *WARNING* *WARNING* *WARNING* *WARNING* *WARNING*
//
// Please do not use this code, it is a proof-of-concept only.  It is not production ready.
//
// *WARNING* *WARNING* *WARNING* *WARNING* *WARNING* *WARNING* *WARNING* *WARNING* *WARNING*
///////////////////////////////////////////////////////////////////////////////////////////////

using System.Collections.Generic;
using System.Threading;
using Eagle._Attributes;
using Eagle._Forms;
using Eagle._Interfaces.Public;

namespace Eagle._Containers.Private
{
    [ObjectId("30c1a42c-351e-4e19-a58f-5019f0b7a92e")]
    internal sealed class ToplevelDictionary :
        Dictionary<string, IAnyPair<Thread, Toplevel>>
    {
        public ToplevelDictionary()
            : base()
        {
            // do nothing.
        }

        ///////////////////////////////////////////////////////////////////////

        #region Dead Code
#if DEAD_CODE
        private ToplevelDictionary(
            IDictionary<string, IAnyPair<Thread, Toplevel>> dictionary
            )
            : base(dictionary)
        {
            // do nothing.
        }
#endif
        #endregion
    }
}
