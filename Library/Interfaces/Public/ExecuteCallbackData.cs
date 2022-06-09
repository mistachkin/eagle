/*
 * ExecuteCallbackData.cs --
 *
 * Copyright (c) 2007-2012 by Joe Mistachkin.  All rights reserved.
 *
 * See the file "license.terms" for information on usage and redistribution of
 * this file, and for a DISCLAIMER OF ALL WARRANTIES.
 *
 * RCS: @(#) $Id: $
 */

using Eagle._Attributes;
using Eagle._Components.Public;

namespace Eagle._Interfaces.Public
{
    [ObjectId("4fe571c6-48a0-4ddf-bf28-dd0892b930ea")]
    public interface IExecuteCallbackData : IIdentifierName, IWrapperData, IHaveClientData, IDynamicExecuteCallback
    {
        // nothing.
    }
}
