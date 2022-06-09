/*
 * TypedInstance.cs --
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
    [ObjectId("b3e65c6d-0831-4d74-8ed9-6b6fcd085517")]
    public interface ITypedInstance
    {
        Type Type { get; }
        string ObjectName { get; }
        string FullObjectName { get; }
        object Object { get; }
        string[] ExtraParts { get; }
    }
}
