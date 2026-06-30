/*
 * PackageManager.cs --
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
    /// This interface is implemented by the component that manages package
    /// loading and version tracking for an interpreter.  It provides the
    /// operations behind the <c>package</c> command, including scanning for
    /// package index files and querying, providing, requiring, and
    /// withdrawing packages.
    /// </summary>
    [ObjectId("74ac785b-566a-407f-89ea-325053ca4976")]
    public interface IPackageManager
    {
        /// <summary>
        /// Scans the specified paths for package index files and processes
        /// them so that the contained packages become available.
        /// </summary>
        /// <param name="paths">
        /// The list of directories to scan for package index files.  This
        /// parameter may be null.
        /// </param>
        /// <param name="autoPath">
        /// Non-zero to also scan the directories on the interpreter
        /// auto-path; otherwise, only the specified paths are scanned.
        /// </param>
        /// <param name="indexes">
        /// Upon success, receives the list of package index files that were
        /// found and processed.
        /// </param>
        /// <param name="error">
        /// Upon failure, this must contain an appropriate error message.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise, a non-Ok
        /// value with details placed in the <paramref name="error" />
        /// parameter.
        /// </returns>
        ReturnCode ScanPackages(
            StringList paths,
            bool autoPath,
            ref StringList indexes,
            ref Result error
            );

        /// <summary>
        /// Asserts that the named package, optionally of a specific version,
        /// is not present (i.e. not provided) in the interpreter.
        /// </summary>
        /// <param name="name">
        /// The name of the package to check for absence.
        /// </param>
        /// <param name="version">
        /// The specific version to check for, if any.  This parameter may be
        /// null to refer to any version.
        /// </param>
        /// <param name="exact">
        /// Non-zero to require an exact match on the specified version;
        /// otherwise, a compatible version satisfies the check.
        /// </param>
        /// <param name="result">
        /// Upon success, this may contain an informational result.  Upon
        /// failure, this must contain an appropriate error message.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise, a non-Ok
        /// value with details placed in the <paramref name="result" />
        /// parameter.
        /// </returns>
        ReturnCode AbsentPackage(
            string name,
            Version version,
            bool exact,
            ref Result result
            );

        /// <summary>
        /// Asserts that the named package, optionally of a specific version,
        /// is present (i.e. provided) in the interpreter.
        /// </summary>
        /// <param name="name">
        /// The name of the package to check for presence.
        /// </param>
        /// <param name="version">
        /// The specific version to check for, if any.  This parameter may be
        /// null to refer to any version.
        /// </param>
        /// <param name="exact">
        /// Non-zero to require an exact match on the specified version;
        /// otherwise, a compatible version satisfies the check.
        /// </param>
        /// <param name="result">
        /// Upon success, this may contain the version of the package that is
        /// present.  Upon failure, this must contain an appropriate error
        /// message.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise, a non-Ok
        /// value with details placed in the <paramref name="result" />
        /// parameter.
        /// </returns>
        ReturnCode PresentPackage(
            string name,
            Version version,
            bool exact,
            ref Result result
            );

        /// <summary>
        /// Declares that the named package of the specified version is
        /// provided by (i.e. available within) the interpreter.
        /// </summary>
        /// <param name="name">
        /// The name of the package being provided.
        /// </param>
        /// <param name="version">
        /// The version of the package being provided.  This parameter may be
        /// null.
        /// </param>
        /// <param name="result">
        /// Upon success, this may contain an informational result.  Upon
        /// failure, this must contain an appropriate error message.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise, a non-Ok
        /// value with details placed in the <paramref name="result" />
        /// parameter.
        /// </returns>
        ReturnCode ProvidePackage(
            string name,
            Version version,
            ref Result result
            );

        /// <summary>
        /// Requires the named package, optionally of a specific version,
        /// loading it if necessary so that it becomes available.
        /// </summary>
        /// <param name="name">
        /// The name of the package being required.
        /// </param>
        /// <param name="version">
        /// The specific version required, if any.  This parameter may be
        /// null to require any available version.
        /// </param>
        /// <param name="exact">
        /// Non-zero to require an exact match on the specified version;
        /// otherwise, a compatible version satisfies the requirement.
        /// </param>
        /// <param name="result">
        /// Upon success, this may contain the version of the package that
        /// was loaded.  Upon failure, this must contain an appropriate error
        /// message.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise, a non-Ok
        /// value with details placed in the <paramref name="result" />
        /// parameter.
        /// </returns>
        ReturnCode RequirePackage(
            string name,
            Version version,
            bool exact,
            ref Result result
            );

        /// <summary>
        /// Withdraws a previously provided package version, making it no
        /// longer available within the interpreter.
        /// </summary>
        /// <param name="name">
        /// The name of the package being withdrawn.
        /// </param>
        /// <param name="version">
        /// The version of the package being withdrawn, if any.  This
        /// parameter may be null.
        /// </param>
        /// <param name="result">
        /// Upon success, this may contain an informational result.  Upon
        /// failure, this must contain an appropriate error message.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise, a non-Ok
        /// value with details placed in the <paramref name="result" />
        /// parameter.
        /// </returns>
        ReturnCode WithdrawPackage(
            string name,
            Version version,
            ref Result result
            );
    }
}
