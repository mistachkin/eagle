/*
 * Package.cs --
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

namespace Eagle._Interfaces.Public
{
    /// <summary>
    /// This interface is implemented by entities that represent a package
    /// known to an Eagle interpreter.  It composes the package metadata
    /// (<see cref="IPackageData" />) and the per-entity state
    /// (<see cref="IState" />), and adds the operations used to select and
    /// load a particular version of the package.
    /// </summary>
    [ObjectId("ccf91a70-911c-4253-9599-27fa40adcb2f")]
    public interface IPackage : IPackageData, IState
    {
        /// <summary>
        /// Selects the version of this package that best satisfies the
        /// specified preference.
        /// </summary>
        /// <param name="preference">
        /// The preference that controls which version is selected.
        /// </param>
        /// <param name="version">
        /// Upon success, receives the selected version of the package.
        /// </param>
        /// <param name="error">
        /// Upon failure, this must contain an appropriate error message.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise, a non-Ok value
        /// with details placed in the <paramref name="error" /> parameter.
        /// </returns>
        [Throw(true)]
        ReturnCode Select(PackagePreference preference, ref Version version, ref Result error);

        /// <summary>
        /// Loads the specified version of this package into the given
        /// interpreter.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter context to load this package into.  This parameter
        /// should not be null.
        /// </param>
        /// <param name="version">
        /// The version of the package to load.
        /// </param>
        /// <param name="result">
        /// Upon success, this may contain the result of loading the package.
        /// Upon failure, this must contain an appropriate error message.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise, a non-Ok value
        /// with details placed in the <paramref name="result" /> parameter.
        /// </returns>
        //
        // TODO: Change this to use the IInterpreter type.
        //
        [Throw(true)]
        ReturnCode Load(Interpreter interpreter, Version version, ref Result result);
    }
}
