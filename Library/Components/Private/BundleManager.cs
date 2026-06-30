/*
 * BundleManager.cs --
 *
 * Copyright (c) 2007-2012 by Joe Mistachkin.  All rights reserved.
 *
 * See the file "license.terms" for information on usage and redistribution of
 * this file, and for a DISCLAIMER OF ALL WARRANTIES.
 *
 * RCS: @(#) $Id: $
 */

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using Eagle._Attributes;
using Eagle._Components.Public;
using Eagle._Constants;
using Eagle._Containers.Public;
using Eagle._Interfaces.Public;
using BundlePair = System.Collections.Generic.KeyValuePair<string, byte[]>;
using BundleDictionary = System.Collections.Generic.Dictionary<string, byte[]>;

namespace Eagle._Components.Private
{
    /// <summary>
    /// This class manages the set of mounted script "bundle" files for an
    /// interpreter, mapping each bundle file name to the password used to
    /// decrypt and verify its contents.  It tracks the bundle currently being
    /// evaluated, supports mounting and unmounting bundles, listing the
    /// mounted bundles, and extracting the decrypted script data for a path
    /// within a bundle.  It implements <see cref="IBundleManager" /> and is
    /// disposable; the mounted bundle table is released on disposal.
    /// </summary>
    [ObjectId("795ebdf0-d7e1-47d3-8929-af6e4eb8a85e")]
    internal sealed class BundleManager :
#if ISOLATED_INTERPRETERS || ISOLATED_PLUGINS
        ScriptMarshalByRefObject,
#endif
        IBundleManager, IDisposable
    {
        #region Private Data
        /// <summary>
        /// The object used to synchronize access to the mutable state of this
        /// bundle manager.
        /// </summary>
        private readonly object syncRoot = new object();
        /// <summary>
        /// The table mapping each mounted bundle file name to the password
        /// bytes used to decrypt and verify it.  This is set to null when this
        /// bundle manager has been disposed.
        /// </summary>
        private BundleDictionary fileNames;
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Public Constructors
        /// <summary>
        /// Constructs a new bundle manager with an empty table of mounted
        /// bundles.
        /// </summary>
        public BundleManager()
        {
            lock (syncRoot)
            {
                fileNames = new BundleDictionary(PathOps.Comparer);
            }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region IBundleManager Members
        /// <summary>
        /// The file name of the bundle currently being evaluated, or null if
        /// no bundle is being evaluated.
        /// </summary>
        private string fileName;
        /// <summary>
        /// Gets the file name of the bundle currently being evaluated, or null
        /// if no bundle is being evaluated.
        /// </summary>
        public string FileName
        {
            get
            {
                CheckDisposed();

                lock (syncRoot)
                {
                    return fileName;
                }
            }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Gets a snapshot copy of the table mapping each mounted bundle file
        /// name to its associated password bytes.
        /// </summary>
        public IDictionary<string, byte[]> FileNames
        {
            get
            {
                CheckDisposed();

                lock (syncRoot) /* TRANSACTIONAL */
                {
                    return new BundleDictionary(fileNames);
                }
            }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method marks the beginning of evaluation of a bundle, recording
        /// the specified file name as the bundle currently being evaluated and
        /// returning the file name that was previously current so it can be
        /// restored later.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter associated with this evaluation.  This parameter is
        /// not used.
        /// </param>
        /// <param name="fileName">
        /// The file name of the bundle that is about to be evaluated.
        /// </param>
        /// <param name="savedFileName">
        /// Upon return, receives the file name of the bundle that was current
        /// before this call, so that it may be restored by a subsequent call to
        /// <see cref="EndEvaluation" />.
        /// </param>
        public void BeginEvaluation(
            Interpreter interpreter, /* in: NOT USED */
            string fileName,         /* in */
            out string savedFileName /* out */
            )
        {
            CheckDisposed();

            lock (syncRoot) /* TRANSACTIONAL */
            {
                savedFileName = this.fileName;
                this.fileName = fileName;
            }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method marks the end of evaluation of a bundle, restoring the
        /// previously current bundle file name that was captured by a matching
        /// call to <see cref="BeginEvaluation" />.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter associated with this evaluation.  This parameter is
        /// not used.
        /// </param>
        /// <param name="savedFileName">
        /// On input, the previously current bundle file name to restore; upon
        /// return, this is reset to null.
        /// </param>
        public void EndEvaluation(
            Interpreter interpreter, /* in: NOT USED */
            ref string savedFileName /* in, out */
            )
        {
            CheckDisposed();

            lock (syncRoot) /* TRANSACTIONAL */
            {
                this.fileName = savedFileName;
                savedFileName = null;
            }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method builds a list of the file names of the currently mounted
        /// bundles, optionally filtered by a glob match pattern.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter to use when performing pattern matching.
        /// </param>
        /// <param name="pattern">
        /// The glob pattern used to filter the bundle file names, or null to
        /// include every mounted bundle.
        /// </param>
        /// <param name="noCase">
        /// Non-zero to perform case-insensitive pattern matching.
        /// </param>
        /// <param name="result">
        /// Upon success, receives the list of matching mounted bundle file
        /// names; upon failure, receives an error message.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise,
        /// <see cref="ReturnCode.Error" />.
        /// </returns>
        public ReturnCode ListMounts(
            Interpreter interpreter, /* in */
            string pattern,          /* in */
            bool noCase,             /* in */
            ref Result result        /* out */
            )
        {
            CheckDisposed();

            lock (syncRoot) /* TRANSACTIONAL */
            {
                if (fileNames == null)
                {
                    result = "bundles unavailable";
                    return ReturnCode.Error;
                }

                StringList list = new StringList();

                foreach (BundlePair pair in fileNames)
                {
                    string fileName = pair.Key;

                    if ((pattern != null) && !StringOps.Match(
                            interpreter, MatchMode.Glob, fileName,
                            pattern, noCase))
                    {
                        continue;
                    }

                    list.Add(fileName);
                }

                result = list;
                return ReturnCode.Ok;
            }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method mounts a bundle, associating the specified file name
        /// with the specified password so that scripts may later be extracted
        /// from it.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter associated with this operation.  This parameter is
        /// not used.
        /// </param>
        /// <param name="fileName">
        /// The file name of the bundle to mount.  It is verified before use.
        /// </param>
        /// <param name="password">
        /// The password bytes used to decrypt and verify the bundle contents.
        /// </param>
        /// <param name="errorOnMounted">
        /// Non-zero to return an error if the bundle is already mounted; zero
        /// to treat an already mounted bundle as success.
        /// </param>
        /// <param name="error">
        /// Upon failure, receives an error message describing the problem.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise,
        /// <see cref="ReturnCode.Error" />.
        /// </returns>
        public ReturnCode Mount(
            Interpreter interpreter, /* in: NOT USED */
            string fileName,         /* in */
            byte[] password,         /* in */
            bool errorOnMounted,     /* in */
            ref Result error         /* out */
            )
        {
            CheckDisposed();

            lock (syncRoot) /* TRANSACTIONAL */
            {
                if (!DataOps.VerifyBundleFileName(
                        ref fileName, ref error))
                {
                    return ReturnCode.Error;
                }

                if (fileNames == null)
                {
                    error = "bundles unavailable";
                    return ReturnCode.Error;
                }

                if (fileNames.ContainsKey(fileName))
                {
                    if (errorOnMounted)
                    {
                        error = String.Format(
                            "bundle {0} already mounted",
                            FormatOps.WrapOrNull(fileName));

                        return ReturnCode.Error;
                    }
                    else
                    {
                        return ReturnCode.Ok;
                    }
                }

                fileNames.Add(fileName, password);
                return ReturnCode.Ok;
            }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method extracts the decrypted script data for a path within a
        /// mounted bundle, gathering and verifying the single bundle script
        /// identified by the path.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter to use when gathering the bundle script.
        /// </param>
        /// <param name="cultureInfo">
        /// The culture information to use when gathering the bundle script.
        /// </param>
        /// <param name="encoding">
        /// The encoding to use when interpreting the bundle script data.
        /// </param>
        /// <param name="path">
        /// The path that identifies the bundle file name and the script within
        /// it.  It is verified before use.
        /// </param>
        /// <param name="data">
        /// Upon success, receives the decrypted script data bytes.
        /// </param>
        /// <param name="error">
        /// Upon failure, receives an error message describing the problem.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise,
        /// <see cref="ReturnCode.Error" />.
        /// </returns>
        public ReturnCode GetData(
            Interpreter interpreter, /* in */
            CultureInfo cultureInfo, /* in */
            Encoding encoding,       /* in */
            string path,             /* in */
            ref byte[] data,         /* out */
            ref Result error         /* out */
            )
        {
            CheckDisposed();

            lock (syncRoot) /* TRANSACTIONAL */
            {
                string fileName;
                string fullName;

                if (!DataOps.VerifyBundlePath(
                        path, true, out fileName, out fullName,
                        ref error))
                {
                    return ReturnCode.Error;
                }

                if (fileNames == null)
                {
                    error = "bundle file names unavailable";
                    return ReturnCode.Error;
                }

                byte[] password;

                if (!fileNames.TryGetValue(fileName, out password))
                {
                    error = String.Format(
                        "bundle {0} not mounted",
                        FormatOps.WrapOrNull(fileName));

                    return ReturnCode.Error;
                }

                List<Script> scripts = null;

                if (DataOps.GatherBundleScripts(
                        interpreter, cultureInfo, null, null, encoding,
                        fileName, password, fullName, false, true,
                        ref scripts, ref error) != ReturnCode.Ok)
                {
                    return ReturnCode.Error;
                }

                if (!DataOps.VerifyOneBundleScript(
                        fileName, fullName, encoding, scripts,
                        ref data, ref error))
                {
                    return ReturnCode.Error;
                }

                return ReturnCode.Ok;
            }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method unmounts a previously mounted bundle, removing its file
        /// name and associated password from the table of mounted bundles.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter associated with this operation.  This parameter is
        /// not used.
        /// </param>
        /// <param name="fileName">
        /// The file name of the bundle to unmount.  It is verified before use.
        /// </param>
        /// <param name="errorOnNotMounted">
        /// Non-zero to return an error if the bundle is not currently mounted;
        /// zero to treat a not-mounted bundle as success.
        /// </param>
        /// <param name="error">
        /// Upon failure, receives an error message describing the problem.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise,
        /// <see cref="ReturnCode.Error" />.
        /// </returns>
        public ReturnCode Unmount(
            Interpreter interpreter, /* in: NOT USED */
            string fileName,         /* in */
            bool errorOnNotMounted,  /* in */
            ref Result error         /* out */
            )
        {
            CheckDisposed();

            lock (syncRoot) /* TRANSACTIONAL */
            {
                if (!DataOps.VerifyBundleFileName(
                        ref fileName, ref error))
                {
                    return ReturnCode.Error;
                }

                if (fileNames == null)
                {
                    error = "bundle file names unavailable";
                    return ReturnCode.Error;
                }

                if (!fileNames.ContainsKey(fileName))
                {
                    if (errorOnNotMounted)
                    {
                        error = String.Format(
                            "bundle {0} not mounted",
                            FormatOps.WrapOrNull(fileName));

                        return ReturnCode.Error;
                    }
                    else
                    {
                        return ReturnCode.Ok;
                    }
                }

                if (!fileNames.Remove(fileName))
                {
                    error = String.Format(
                        "could not unmount bundle {0}",
                        FormatOps.WrapOrNull(fileName));

                    return ReturnCode.Error;
                }

                return ReturnCode.Ok;
            }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region IDisposable "Pattern" Members
        /// <summary>
        /// Stores a value indicating whether this bundle manager has been
        /// disposed.
        /// </summary>
        private bool disposed;
        /// <summary>
        /// This method throws an exception if this bundle manager has already
        /// been disposed.  It is called at the start of most members to guard
        /// against use after disposal.
        /// </summary>
        /// <exception cref="ObjectDisposedException">
        /// Thrown when this bundle manager has been disposed and the engine is
        /// configured to throw on use of a disposed object.
        /// </exception>
        private void CheckDisposed() /* throw */
        {
#if THROW_ON_DISPOSED
            if (disposed && Engine.IsThrowOnDisposed(null, false))
                throw new ObjectDisposedException(typeof(BundleManager).Name);
#endif
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method releases the resources held by this bundle manager.  It
        /// implements the standard dispose pattern.
        /// </summary>
        /// <param name="disposing">
        /// Non-zero if this method is being called from <see cref="Dispose()" />
        /// (i.e. deterministically); zero if it is being called from the
        /// finalizer.  When non-zero, managed resources are released.
        /// </param>
        private /* protected virtual */ void Dispose(
            bool disposing /* in */
            )
        {
            try
            {
                if (!disposed)
                {
                    if (disposing)
                    {
                        ////////////////////////////////////
                        // dispose managed resources here...
                        ////////////////////////////////////

                        lock (syncRoot) /* TRANSACTIONAL */
                        {
                            fileName = null;

                            if (fileNames != null)
                            {
                                fileNames.Clear();
                                fileNames = null;
                            }
                        }
                    }

                    //////////////////////////////////////
                    // release unmanaged resources here...
                    //////////////////////////////////////
                }
            }
            finally
            {
                // base.Dispose(disposing);

                disposed = true;
            }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region IDisposable Members
        /// <summary>
        /// This method releases all resources held by this bundle manager and
        /// suppresses finalization.
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Destructor
        /// <summary>
        /// Finalizes this bundle manager, releasing any resources that were not
        /// released by an explicit call to <see cref="Dispose()" />.
        /// </summary>
        ~BundleManager()
        {
            Dispose(false);
        }
        #endregion
    }
}
