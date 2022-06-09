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
using System.Collections.Generic;
using Eagle._Attributes;

namespace Eagle._Interfaces.Public
{
    [ObjectId("ce2d0e84-99ba-4437-bb87-1529cdffc6c4")]
    public interface ICoreClientData
    {
        IClientData Parent { get; }
        IEnumerable<IClientData> Children { get; }

        object DataNoThrow { get; set; }
        bool ReadOnly { get; }

        Type MaybeGetDataType();
        string GetDataTypeName();
    }
}
