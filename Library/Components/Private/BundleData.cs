/*
 * BundleData.cs --
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
using Eagle._Interfaces.Public;
using _RuleSet = Eagle._Components.Public.RuleSet;

namespace Eagle._Components.Private
{
    [ObjectId("51fe7974-eac7-4a34-8ea7-bdd7782f5edd")]
    internal sealed class BundleData :
#if ISOLATED_INTERPRETERS || ISOLATED_PLUGINS
        ScriptMarshalByRefObject,
#endif
        IBundleData, IDisposable
    {
        #region Public Constructors
        public BundleData(
            IBundleData bundleData /* in */
            )
        {
            if (bundleData != null)
            {
                this.language = bundleData.Language;
                this.sequence = bundleData.Sequence;
                this.vendor = bundleData.Vendor;
                this.path = bundleData.Path;
                this.fullName = bundleData.FullName;
                this.hashAlgorithmName = bundleData.HashAlgorithmName;
                this.fileBytes = ArrayOps.Copy(bundleData.FileBytes);
                this.isolationLevel = bundleData.IsolationLevel;
                this.securityLevel = bundleData.SecurityLevel;
                this.securityFlags = bundleData.SecurityFlags;
                this.ruleSet = _RuleSet.Clone(bundleData.RuleSet);
            }
        }

        ///////////////////////////////////////////////////////////////////////

        public BundleData(
            string language,                   /* in */
            long sequence,                     /* in */
            string vendor,                     /* in */
            string path,                       /* in */
            string fullName,                   /* in */
            string hashAlgorithmName,          /* in */
            byte[] fileBytes,                  /* in */
            IsolationLevel isolationLevel,     /* in */
            SecurityLevel securityLevel,       /* in */
            ScriptSecurityFlags securityFlags, /* in */
            IRuleSet ruleSet                   /* in */
            )
        {
            this.language = language;
            this.sequence = sequence;
            this.vendor = vendor;
            this.path = path;
            this.fullName = fullName;
            this.hashAlgorithmName = hashAlgorithmName;
            this.fileBytes = fileBytes;
            this.isolationLevel = isolationLevel;
            this.securityLevel = securityLevel;
            this.securityFlags = securityFlags;
            this.ruleSet = ruleSet;
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region IGetInterpreter / ISetInterpreter Members
        private Interpreter interpreter;
        public Interpreter Interpreter
        {
            get { CheckDisposed(); return interpreter; }
            set { CheckDisposed(); interpreter = value; }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region IBundleData Members
        private string language;
        public string Language
        {
            get { CheckDisposed(); return language; }
        }

        ///////////////////////////////////////////////////////////////////////

        private long sequence;
        public long Sequence
        {
            get { CheckDisposed(); return sequence; }
        }

        ///////////////////////////////////////////////////////////////////////

        private string vendor;
        public string Vendor
        {
            get { CheckDisposed(); return vendor; }
        }

        ///////////////////////////////////////////////////////////////////////

        private string path;
        public string Path
        {
            get { CheckDisposed(); return path; }
        }

        ///////////////////////////////////////////////////////////////////////

        private string fullName;
        public string FullName
        {
            get { CheckDisposed(); return fullName; }
        }

        ///////////////////////////////////////////////////////////////////////

        private string hashAlgorithmName;
        public string HashAlgorithmName
        {
            get { CheckDisposed(); return hashAlgorithmName; }
        }

        ///////////////////////////////////////////////////////////////////////

        private byte[] fileBytes;
        public byte[] FileBytes
        {
            get { CheckDisposed(); return fileBytes; }
        }

        ///////////////////////////////////////////////////////////////////////

        private IsolationLevel isolationLevel;
        public IsolationLevel IsolationLevel
        {
            get { CheckDisposed(); return isolationLevel; }
        }

        ///////////////////////////////////////////////////////////////////////

        private SecurityLevel securityLevel;
        public SecurityLevel SecurityLevel
        {
            get { CheckDisposed(); return securityLevel; }
        }

        ///////////////////////////////////////////////////////////////////////

        private ScriptSecurityFlags securityFlags;
        public ScriptSecurityFlags SecurityFlags
        {
            get { CheckDisposed(); return securityFlags; }
        }

        ///////////////////////////////////////////////////////////////////////

        private IRuleSet ruleSet;
        public IRuleSet RuleSet
        {
            get { CheckDisposed(); return ruleSet; }
        }

        ///////////////////////////////////////////////////////////////////////

        public void MakeImmutable()
        {
            CheckDisposed();

            //
            // WARNING: Once this method is called, it cannot be undone from
            //          external to this class.  This is by design, for the
            //          sake of security (e.g. for IScript objects passed to
            //          the policy engine).  Further, there is no way for an
            //          external caller to determine if an IScript instance
            //          is read-only or immutable (i.e. via an introspection
            //          property) without causing an exception to be thrown.
            //          This restriction may be relaxed in the future.
            //
            securityFlags |= ScriptSecurityFlags.Immutable;
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region IDisposable "Pattern" Members
        private bool disposed;
        private void CheckDisposed() /* throw */
        {
#if THROW_ON_DISPOSED
            if (disposed && Engine.IsThrowOnDisposed(null, false))
                throw new ObjectDisposedException(typeof(BundleData).Name);
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

                        if (interpreter != null)
                            interpreter = null; /* NOT OWNED */
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
        ~BundleData()
        {
            Dispose(false);
        }
        #endregion
    }
}
