/*
 * EnsembleData.cs --
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
    [ObjectId("b929c34c-26d1-4ec5-b1e7-b136ddb74994")]
    public interface IEnsembleData : IIdentifier, IWrapperData
    {
        IExecute SubCommandExecute { get; set; }
    }
}
