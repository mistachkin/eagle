/*
 * Core.cs --
 *
 * Copyright (c) 2007-2012 by Joe Mistachkin.  All rights reserved.
 *
 * See the file "license.terms" for information on usage and redistribution of
 * this file, and for a DISCLAIMER OF ALL WARRANTIES.
 *
 * RCS: @(#) $Id: $
 */

using Eagle._Attributes;
using Eagle._Interfaces.Public;

namespace Eagle._Procedures
{
    [ObjectId("5765ee79-add6-444a-a4e5-d6f80d501125")]
    public class Core : Default
    {
        #region Public Constructors
        public Core(
            IProcedureData procedureData
            )
            : base(procedureData)
        {
            // do nothing.
        }
        #endregion
    }
}
