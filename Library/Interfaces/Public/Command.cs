/*
 * Command.cs --
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
    [ObjectId("c187713a-3b67-4b38-88ec-d7c37a8fb901")]
    public interface ICommand : ICommandData, IState, IDynamicExecuteCallback, IExecute, IEnsemble, IPolicyEnsemble, ISyntax, IUsageData
    {
        // nothing.
    }
}
