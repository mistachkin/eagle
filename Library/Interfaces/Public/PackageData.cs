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
    /// <summary>
    /// This interface defines the metadata associated with a package known to
    /// an Eagle interpreter.  It composes the unique identity
    /// (<see cref="IIdentifier" />) and the wrapper bookkeeping
    /// (<see cref="IWrapperData" />).
    /// </summary>
    [ObjectId("7fbfae8d-b24c-4281-a4a0-3a68a61d213a")]
    public interface IPackageData : IIdentifier, IWrapperData
    {
        /// <summary>
        /// Gets or sets the name of the package index file.
        /// </summary>
        string IndexFileName { get; set; }
        /// <summary>
        /// Gets or sets the name of the file that provides this package.
        /// </summary>
        string ProvideFileName { get; set; }
        /// <summary>
        /// Gets or sets the flags that control the behavior of this package.
        /// </summary>
        PackageFlags Flags { get; set; }
        /// <summary>
        /// Gets or sets the version of this package that is currently loaded,
        /// if any.
        /// </summary>
        Version Loaded { get; set; }
        /// <summary>
        /// Gets or sets the mapping of package versions to the scripts used to
        /// load them on demand.
        /// </summary>
        VersionStringDictionary IfNeeded { get; set; }
        /// <summary>
        /// Gets or sets the script that was used to load this package on
        /// demand, if any.
        /// </summary>
        string WasNeeded { get; set; }
    }
}
