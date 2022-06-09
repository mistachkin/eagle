/*
 * Function.cs --
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
    [ObjectId("93812cb4-afcf-4600-bb7d-7de9adeb6a4f")]
    public interface IFunction : IFunctionData, IState, IExecuteArgument, IUsageData
    {
        // nothing.
    }
}
