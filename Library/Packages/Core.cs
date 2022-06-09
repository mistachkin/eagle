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
    [ObjectId("c0e022cd-2c9b-4020-9d27-312eef08a3cd")]
    public class Core : Default
    {
        #region Public Constructors
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

        private Version loading; // which version are we actively loading?

        ///////////////////////////////////////////////////////////////////////

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
