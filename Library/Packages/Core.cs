/*
 * Core.cs --
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
using Eagle._Components.Public;
using Eagle._Containers.Public;
using Eagle._Interfaces.Public;

namespace Eagle._Packages
{
    /// <summary>
    /// This class implements the core package used by the Eagle engine to
    /// represent a script package.  It derives from <see cref="Default" /> and
    /// provides working implementations of version selection and package
    /// loading, evaluating the appropriate <c>ifneeded</c> script to satisfy a
    /// [package require] request.  See <c>core_language.md</c> for package
    /// management semantics.
    /// </summary>
    [ObjectId("c0e022cd-2c9b-4020-9d27-312eef08a3cd")]
    public class Core : Default
    {
        #region Public Constructors
        /// <summary>
        /// Constructs an instance of the core package.
        /// </summary>
        /// <param name="packageData">
        /// The data used to create and identify this package, such as its name
        /// and the set of available <c>ifneeded</c> scripts.  This parameter
        /// may be null.
        /// </param>
        public Core(
            IPackageData packageData
            )
            : base(packageData)
        {
            //
            // NOTE: Which package are we actively trying to load?  This is an
            //       internal implementation detail and is not exposed via the
            //       IPackage interface.
            //
            loading = null;
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region IPackage Members
        /// <summary>
        /// Selects a version of this package to satisfy a request, according
        /// to the supplied preference.
        /// </summary>
        /// <param name="preference">
        /// The preference that governs how a candidate version is chosen.
        /// </param>
        /// <param name="version">
        /// Upon success, receives the selected version of this package.
        /// </param>
        /// <param name="error">
        /// Upon failure, receives an appropriate error message.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise,
        /// <see cref="ReturnCode.Error" /> with details placed in the
        /// <paramref name="error" /> parameter.
        /// </returns>
        public override ReturnCode Select(
            PackagePreference preference,
            ref Version version,
            ref Result error
            )
        {
            if (preference == PackagePreference.Default)
            {
                string name = this.Name;
                VersionStringDictionary ifNeeded = this.IfNeeded;

                if (ifNeeded != null)
                {
                    //
                    // NOTE: *HACK* For now, always select the latest version
                    //       from the list of candidate versions.
                    //
                    Version latest = null;

                    foreach (Version candidate in ifNeeded.Keys)
                        if (PackageOps.VersionCompare(candidate, latest) > 0)
                            latest = candidate;

                    //
                    // NOTE: Were we able to find the latest (i.e. any)
                    //       version?
                    //
                    if (latest != null)
                    {
                        version = latest;
                        return ReturnCode.Ok;
                    }
                    else
                    {
                        error = String.Format(
                            "can't find package {0}",
                            FormatOps.PackageName(name, null));
                    }
                }
                else
                {
                    error = String.Format(
                        "package {0} ifneeded scripts not available",
                        FormatOps.WrapOrNull(name));
                }
            }
            else
            {
                error = String.Format(
                    "unsupported package preference {0}",
                    FormatOps.WrapOrNull(preference));
            }

            return ReturnCode.Error;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// The version of this package that is currently being loaded, if any;
        /// used to detect circular package dependencies.  This is an internal
        /// implementation detail and is not exposed via the IPackage interface.
        /// </summary>
        private Version loading; // which version are we actively loading?

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Loads the specified version of this package by evaluating its
        /// corresponding <c>ifneeded</c> script.  Circular package
        /// dependencies are detected and reported as an error.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter context in which to evaluate the package script.
        /// This parameter should not be null.
        /// </param>
        /// <param name="version">
        /// The version of this package to load.  This parameter should not be
        /// null.
        /// </param>
        /// <param name="result">
        /// Upon success, this may contain the result produced by evaluating
        /// the package script.  Upon failure, this must contain an appropriate
        /// error message.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise, a non-Ok value
        /// with details placed in the <paramref name="result" /> parameter.
        /// </returns>
        public override ReturnCode Load(
            Interpreter interpreter,
            Version version,
            ref Result result
            )
        {
            if (interpreter != null)
            {
                if (version != null)
                {
                    string name = this.Name;
                    VersionStringDictionary ifNeeded = this.IfNeeded;

                    if (ifNeeded != null)
                    {
                        string text;

                        if (ifNeeded.TryGetValue(version, out text))
                        {
                            if (!FlagOps.HasFlags(Flags, PackageFlags.Loading, true))
                            {
                                Flags |= PackageFlags.Loading;
                                loading = version;

                                try
                                {
                                    ReturnCode code;

                                    code = interpreter.EvaluatePackageScript(
                                        text, ref result);

                                    if (code == ReturnCode.Ok)
                                        WasNeeded = text;

                                    return code;
                                }
                                catch (Exception e)
                                {
                                    result = String.Format(
                                        "caught exception while evaluating ifneeded script: {0}",
                                        e);
                                }
                                finally
                                {
                                    loading = null;
                                    Flags &= ~PackageFlags.Loading;
                                }
                            }
                            else
                            {
                                result = String.Format(
                                    "circular package dependency: " +
                                    "attempt to provide {0} requires {1}",
                                    FormatOps.PackageName(name, version),
                                    FormatOps.PackageName(name, loading));
                            }
                        }
                        else
                        {
                            result = String.Format(
                                "can't find package {0}",
                                FormatOps.PackageName(name, version));
                        }
                    }
                    else
                    {
                        result = String.Format(
                            "package {0} ifneeded scripts not available",
                            FormatOps.WrapOrNull(name));
                    }
                }
                else
                {
                    result = "invalid package version";
                }
            }
            else
            {
                result = "invalid interpreter";
            }

            return ReturnCode.Error;
        }
        #endregion
    }
}
