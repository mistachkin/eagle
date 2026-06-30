/*
 * ScriptPaths.cs --
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
using Eagle._Components.Private;

namespace Eagle._Components.Public
{
    /// <summary>
    /// This class provides the well-known relative path fragments used to
    /// locate the standard Eagle library packages (the plugin loader, the core
    /// script library, the test package, and the kit package).
    /// </summary>
    [ObjectId("b57cce30-e11d-4b31-b3a8-aedc2d7207f3")]
    public static class ScriptPaths
    {
        /// <summary>
        /// The relative path fragment to the Eagle plugin loader library
        /// package.  It should look something like "lib/Loader1.0".
        /// </summary>
        //
        // NOTE: This is the "path fragment" to the Eagle plugin loader library
        //       package.  It should look something like "lib/Loader1.0".
        //
        public static readonly string LoaderPackage = PathOps.GetUnixPath(
            PathOps.CombinePath(null, TclVars.Path.Lib, GlobalState.GetPackagePath(
            PackageType.Loader, GlobalState.GetPackageVersion(null), String.Empty)));

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// The relative path fragment to the Eagle core script library
        /// package.  It should look something like "lib/Eagle1.0".
        /// </summary>
        //
        // NOTE: This is the "path fragment" to the Eagle core script library
        //       package.  It should look something like "lib/Eagle1.0".
        //
        public static readonly string LibraryPackage = PathOps.GetUnixPath(
            PathOps.CombinePath(null, TclVars.Path.Lib, GlobalState.GetPackagePath(
            PackageType.Library, GlobalState.GetPackageVersion(null), String.Empty)));

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// The relative path fragment to the Eagle test package.  It should
        /// look something like "lib/Test1.0".
        /// </summary>
        //
        // NOTE: This is the "path fragment" to the Eagle test package.  It
        //       should look something like "lib/Test1.0".
        //
        public static readonly string TestPackage = PathOps.GetUnixPath(
            PathOps.CombinePath(null, TclVars.Path.Lib, GlobalState.GetPackagePath(
            PackageType.Test, GlobalState.GetPackageVersion(null), String.Empty)));

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// The relative path fragment to the Eagle kit package.  It should
        /// look something like "lib/Kit1.0".
        /// </summary>
        //
        // NOTE: This is the "path fragment" to the Eagle test package.  It
        //       should look something like "lib/Kit1.0".
        //
        public static readonly string KitPackage = PathOps.GetUnixPath(
            PathOps.CombinePath(null, TclVars.Path.Lib, GlobalState.GetPackagePath(
            PackageType.Kit, GlobalState.GetPackageVersion(null), String.Empty)));
    }
}
