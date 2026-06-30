/*
 * TclBuildDictionary.cs --
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
using Eagle._Components.Private.Tcl;
using Eagle._Components.Public;
using Eagle._Containers.Public;

namespace Eagle._Containers.Private.Tcl
{
    /// <summary>
    /// This class represents a dictionary that maps file system paths to native
    /// Tcl build descriptors (<see cref="TclBuild" />).  It extends the path
    /// dictionary with a helper for conditionally adding or replacing entries
    /// based on trust and overwrite policy.
    /// </summary>
    [ObjectId("14eaf1cf-213f-44da-9a27-731194ae6bc8")]
#if TCL_WRAPPER
    public
#else
    internal
#endif
    sealed class TclBuildDictionary : PathDictionary<TclBuild>
    {
        /// <summary>
        /// Constructs an empty Tcl build dictionary.
        /// </summary>
        public TclBuildDictionary()
            : base()
        {
            // do nothing.
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method adds the specified Tcl build under the specified key, or
        /// replaces an existing entry when overwriting is permitted.  The build
        /// is rejected if the key is invalid or, when trusted-only mode is in
        /// effect, if the build's file is not trusted.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter used to evaluate whether the build's file is trusted.
        /// </param>
        /// <param name="flags">
        /// The flags that control trust checking and whether existing entries
        /// may be overwritten.
        /// </param>
        /// <param name="key">
        /// The key under which the build is added.
        /// </param>
        /// <param name="value">
        /// The Tcl build to add or use as the replacement.
        /// </param>
        /// <param name="error">
        /// Upon failure, receives an error message describing why the build
        /// could not be added.
        /// </param>
        /// <returns>
        /// True if the build was added or replaced; otherwise, false.
        /// </returns>
        public bool MaybeAddOrReplace(
            Interpreter interpreter, /* in */
            FindFlags flags,         /* in */
            string key,              /* in */
            TclBuild value,          /* in */
            ref Result error         /* out */
            )
        {
            if (key == null)
            {
                error = String.Format(
                    "can't add Tcl build file {0}: invalid key",
                    FormatOps.TclBuildFileName(value));

                return false;
            }

            if (FlagOps.HasFlags(flags, FindFlags.TrustedOnly, true) &&
                ((value == null) || !RuntimeOps.IsFileTrusted(
                    interpreter, null, value.FileName, IntPtr.Zero)))
            {
                error = String.Format(
                    "can't add Tcl build file {0}: not trusted",
                    FormatOps.TclBuildFileName(value));

                return false;
            }

            if (!this.ContainsKey(key))
            {
                this.Add(key, value);
                return true;
            }

            if (FlagOps.HasFlags(
                    flags, FindFlags.OverwriteBuilds, true))
            {
                this[key] = value;
                return true;
            }

            error = String.Format(
                "can't add Tcl build file {0}: already present",
                FormatOps.TclBuildFileName(value));

            return false;
        }
    }
}
