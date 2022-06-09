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
using Eagle._Interfaces.Private;

namespace Eagle._Lambdas
{
    [ObjectId("65fd32e4-45e7-4250-b9eb-f26405157045")]
    internal class Core : Default
    {
        #region Public Constructors
        public Core(
            ILambdaData lambdaData
            )
            : base(lambdaData)
        {
            // do nothing.
        }
        #endregion
    }
}
