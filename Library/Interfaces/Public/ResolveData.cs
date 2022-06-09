/*
 * ResolveData.cs --
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
    [ObjectId("79e8f73c-d287-4715-824b-d5cc3cdc240f")]
    public interface IResolveData : IIdentifier, IHaveInterpreter, IWrapperData
    {
        // nothing.
    }
}
