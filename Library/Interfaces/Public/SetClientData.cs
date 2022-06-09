/*
 * SetClientData.cs --
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
    [ObjectId("e7bf1ddb-d01f-483a-bdbd-eb7df6677505")]
    public interface ISetClientData
    {
        IClientData ClientData { set; }
    }
}
