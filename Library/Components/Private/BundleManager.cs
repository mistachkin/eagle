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
    [ObjectId("795ebdf0-d7e1-47d3-8929-af6e4eb8a85e")]
    internal sealed class BundleManager :
#if ISOLATED_INTERPRETERS || ISOLATED_PLUGINS
        ScriptMarshalByRefObject,
#endif
        IBundleManager, IDisposable
    {
        #region Private Data
        private readonly object syncRoot = new object();
        private BundleDictionary fileNames;
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Public Constructors
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
        private string fileName;
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
        private bool disposed;
        private void CheckDisposed() /* throw */
        {
#if THROW_ON_DISPOSED
            if (disposed && Engine.IsThrowOnDisposed(null, false))
                throw new ObjectDisposedException(typeof(BundleManager).Name);
#endif
        }

        ///////////////////////////////////////////////////////////////////////

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
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Destructor
        ~BundleManager()
        {
            Dispose(false);
        }
        #endregion
    }
}
