/*
 * Procedure.cs --
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
    [ObjectId("39baec06-3ddf-4e2e-9ba6-f09491556ef6")]
    public interface IProcedure : ILevels, IProcedureData, IDynamicExecuteCallback, IExecute, IUsageData
    {
        // nothing.
    }
}
