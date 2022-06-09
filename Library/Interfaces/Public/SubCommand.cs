/*
 * SubCommand.cs --
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
    [ObjectId("c0757ae1-4732-44db-8a1f-ed7e925834dc")]
    public interface ISubCommand : ISubCommandData, IDynamicExecuteCallback, IDelegateData, IExecute, IEnsemble, IPolicyEnsemble, ISyntax, IUsageData
    {
        // nothing.
    }
}
