/*
 * CoreClientData.cs --
 *
 * Copyright (c) 2007-2012 by Joe Mistachkin.  All rights reserved.
 *
 * See the file "license.terms" for information on usage and redistribution of
 * this file, and for a DISCLAIMER OF ALL WARRANTIES.
 *
 * RCS: @(#) $Id: $
 */

using System;
using Eagle._Attributes;

namespace Eagle._Interfaces.Public
{
    [ObjectId("ce2d0e84-99ba-4437-bb87-1529cdffc6c4")]
    public interface ICoreClientData : IClientData, IBaseClientData
    {
        Type MaybeGetDataType();
        string GetDataTypeName();
    }
}
