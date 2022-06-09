/*
 * CleanupPathClientData.cs --
 *
 * Copyright (c) 2007-2012 by Joe Mistachkin.  All rights reserved.
 *
 * See the file "license.terms" for information on usage and redistribution of
 * this file, and for a DISCLAIMER OF ALL WARRANTIES.
 *
 * RCS: @(#) $Id: $
 */

using System;
using System.IO;
using Eagle._Attributes;
using Eagle._Components.Public;
using Eagle._Containers.Public;

namespace Eagle._Components.Private
{
    [ObjectId("5da54054-5d78-4e39-83bc-d13cdff84252")]
    internal sealed class CleanupPathClientData : ClientData
    {
        #region Public Constructors
        public CleanupPathClientData()
        {
            // do nothing.
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Public Properties
        private PathType pathType;
        public PathType PathType
        {
            get { return pathType; }
            set { pathType = value; }
        }

        ///////////////////////////////////////////////////////////////////////

        private bool recursive;
        public bool Recursive
        {
            get { return recursive; }
            set { recursive = value; }
        }

        ///////////////////////////////////////////////////////////////////////

        private bool force;
        public bool Force
        {
            get { return force; }
            set { force = value; }
        }

        ///////////////////////////////////////////////////////////////////////

        private bool noComplain;
        public bool NoComplain
        {
            get { return noComplain; }
            set { noComplain = value; }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Public Methods
        public bool MatchPathType(
            string path /* in */
            )
        {
            Result error = null;

            return MatchPathType(path, ref error);
        }

        ///////////////////////////////////////////////////////////////////////

        public bool MatchPathType(
            string path,     /* in */
            ref Result error /* out */
            )
        {
            if (!String.IsNullOrEmpty(path))
            {
                if (FlagOps.HasFlags(
                        pathType, PathType.Directory, true))
                {
                    if (Directory.Exists(path))
                        return true;
                    else
                        error = "directory does not exist";
                }
                else if (FlagOps.HasFlags(
                        pathType, PathType.File, true))
                {
                    if (File.Exists(path))
                        return true;
                    else
                        error = "file does not exist";
                }
                else
                {
                    error = String.Format(
                        "unsupported path type {0}",
                        FormatOps.WrapOrNull(pathType));
                }
            }
            else
            {
                error = "invalid or empty path";
            }

            return false;
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region System.Object Overrides
        public override string ToString()
        {
            return StringList.MakeList(
                "pathType", pathType, "recursive", recursive,
                "force", force, "noComplain", noComplain);
        }
        #endregion
    }
}
