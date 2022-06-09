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
    [ObjectId("14eaf1cf-213f-44da-9a27-731194ae6bc8")]
#if TCL_WRAPPER
    public
#else
    internal
#endif
    sealed class TclBuildDictionary : PathDictionary<TclBuild>
    {
        public TclBuildDictionary()
            : base()
        {
            // do nothing.
        }

        ///////////////////////////////////////////////////////////////////////

        public bool MaybeAddOrReplace(
            FindFlags flags, /* in */
            string key,      /* in */
            TclBuild value,  /* in */
            ref Result error /* out */
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
                ((value == null) ||
                !RuntimeOps.IsFileTrusted(value.FileName, IntPtr.Zero)))
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
