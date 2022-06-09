/*
 * Policy.cs --
 *
 * Copyright (c) 2007-2012 by Joe Mistachkin.  All rights reserved.
 *
 * See the file "license.terms" for information on usage and redistribution of
 * this file, and for a DISCLAIMER OF ALL WARRANTIES.
 *
 * RCS: @(#) $Id: $
 */

using Eagle._Attributes;

namespace Eagle._Interfaces.Public
{
    [ObjectId("c94c6eed-6666-4e8e-91e3-d154dbc5b738")]
    public interface IPolicy : IPolicyData, IDynamicExecuteCallback, IExecute, ISetup
    {
        // nothing.
    }
}
