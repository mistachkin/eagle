/*
 * PackageData.cs --
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
using Eagle._Components.Public;
using Eagle._Containers.Public;

namespace Eagle._Interfaces.Public
{
    [ObjectId("7fbfae8d-b24c-4281-a4a0-3a68a61d213a")]
    public interface IPackageData : IIdentifier, IWrapperData
    {
        string IndexFileName { get; set; }
        string ProvideFileName { get; set; }
        PackageFlags Flags { get; set; }
        Version Loaded { get; set; }
        VersionStringDictionary IfNeeded { get; set; }
        string WasNeeded { get; set; }
    }
}
