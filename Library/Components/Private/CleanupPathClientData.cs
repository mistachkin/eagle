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
    /// <summary>
    /// This class carries the client data used when cleaning up a file system
    /// path (e.g. the path type to match, and whether the cleanup should be
    /// recursive, forced, or silent).  It derives from <see cref="ClientData" />.
    /// </summary>
    [ObjectId("5da54054-5d78-4e39-83bc-d13cdff84252")]
    internal sealed class CleanupPathClientData : ClientData
    {
        #region Public Constructors
        /// <summary>
        /// Constructs an empty cleanup path client data instance, leaving all
        /// properties at their default values.
        /// </summary>
        public CleanupPathClientData()
        {
            // do nothing.
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Public Properties
        /// <summary>
        /// Stores the type of path to be matched during cleanup.
        /// </summary>
        private PathType pathType;
        /// <summary>
        /// Gets or sets the type of path to be matched during cleanup.
        /// </summary>
        public PathType PathType
        {
            get { return pathType; }
            set { pathType = value; }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// When non-zero, the cleanup should be performed recursively.
        /// </summary>
        private bool recursive;
        /// <summary>
        /// Gets or sets a value indicating whether the cleanup should be
        /// performed recursively.
        /// </summary>
        public bool Recursive
        {
            get { return recursive; }
            set { recursive = value; }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// When non-zero, the cleanup should be forced.
        /// </summary>
        private bool force;
        /// <summary>
        /// Gets or sets a value indicating whether the cleanup should be
        /// forced.
        /// </summary>
        public bool Force
        {
            get { return force; }
            set { force = value; }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// When non-zero, errors encountered during the cleanup should be
        /// suppressed.
        /// </summary>
        private bool noComplain;
        /// <summary>
        /// Gets or sets a value indicating whether errors encountered during
        /// the cleanup should be suppressed.
        /// </summary>
        public bool NoComplain
        {
            get { return noComplain; }
            set { noComplain = value; }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Public Methods
        /// <summary>
        /// This method determines whether the specified path matches the
        /// configured path type, discarding any error message.
        /// </summary>
        /// <param name="path">
        /// The file system path to check against the configured path type.
        /// </param>
        /// <returns>
        /// True if the path matches the configured path type; otherwise,
        /// false.
        /// </returns>
        public bool MatchPathType(
            string path /* in */
            )
        {
            Result error = null;

            return MatchPathType(path, ref error);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method determines whether the specified path matches the
        /// configured path type (e.g. an existing directory or file).
        /// </summary>
        /// <param name="path">
        /// The file system path to check against the configured path type.
        /// </param>
        /// <param name="error">
        /// Upon failure, receives an error message describing why the path did
        /// not match the configured path type.
        /// </param>
        /// <returns>
        /// True if the path matches the configured path type; otherwise,
        /// false.
        /// </returns>
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
        /// <summary>
        /// This method returns a string representation of this cleanup path
        /// client data instance, listing its property names and values.
        /// </summary>
        /// <returns>
        /// A string containing the property names and values of this instance.
        /// </returns>
        public override string ToString()
        {
            return StringList.MakeList(
                "pathType", pathType, "recursive", recursive,
                "force", force, "noComplain", noComplain);
        }
        #endregion
    }
}
