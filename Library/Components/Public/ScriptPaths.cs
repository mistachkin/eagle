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
    [ObjectId("b57cce30-e11d-4b31-b3a8-aedc2d7207f3")]
    public static class ScriptPaths
    {
        //
        // NOTE: This is the "path fragment" to the Eagle core script library
        //       package.  It should look something like "lib/Eagle1.0".
        //
        public static readonly string LibraryPackage = PathOps.GetUnixPath(
            PathOps.CombinePath(null, TclVars.Path.Lib, GlobalState.GetPackagePath(
            PackageType.Library, GlobalState.GetPackageVersion(), String.Empty)));

        ///////////////////////////////////////////////////////////////////////

        //
        // NOTE: This is the "path fragment" to the Eagle test package.  It
        //       should look something like "lib/Test1.0".
        //
        public static readonly string TestPackage = PathOps.GetUnixPath(
            PathOps.CombinePath(null, TclVars.Path.Lib, GlobalState.GetPackagePath(
            PackageType.Test, GlobalState.GetPackageVersion(), String.Empty)));

        ///////////////////////////////////////////////////////////////////////

        //
        // NOTE: This is the "path fragment" to the Eagle test package.  It
        //       should look something like "lib/Kit1.0".
        //
        public static readonly string KitPackage = PathOps.GetUnixPath(
            PathOps.CombinePath(null, TclVars.Path.Lib, GlobalState.GetPackagePath(
            PackageType.Kit, GlobalState.GetPackageVersion(), String.Empty)));
    }
}
