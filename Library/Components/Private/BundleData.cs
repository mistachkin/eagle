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
    /// <summary>
    /// This class holds the data associated with a single script bundle (e.g.
    /// its language, sequence, vendor, path, file content, and security
    /// settings).  It implements <see cref="IBundleData" /> and provides a
    /// snapshot of bundle metadata that may optionally be made immutable for
    /// use by the policy engine.
    /// </summary>
    [ObjectId("51fe7974-eac7-4a34-8ea7-bdd7782f5edd")]
    internal sealed class BundleData :
#if ISOLATED_INTERPRETERS || ISOLATED_PLUGINS
        ScriptMarshalByRefObject,
#endif
        IBundleData, IDisposable
    {
        #region Public Constructors
        /// <summary>
        /// Constructs a bundle data instance by copying the property values
        /// from another bundle data instance.  When the supplied instance is
        /// null, all fields are left at their default values.
        /// </summary>
        /// <param name="bundleData">
        /// The bundle data instance whose property values should be copied, or
        /// null to leave this instance with default values.
        /// </param>
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

        /// <summary>
        /// Constructs a bundle data instance from the fully specified set of
        /// identity, content, and security parameters.
        /// </summary>
        /// <param name="language">
        /// The name of the scripting language associated with the bundle.
        /// </param>
        /// <param name="sequence">
        /// The sequence number used to order the bundle.
        /// </param>
        /// <param name="vendor">
        /// The name of the vendor that produced the bundle.
        /// </param>
        /// <param name="path">
        /// The file system path associated with the bundle.
        /// </param>
        /// <param name="fullName">
        /// The fully qualified name of the bundle.
        /// </param>
        /// <param name="hashAlgorithmName">
        /// The name of the hash algorithm used for the bundle.
        /// </param>
        /// <param name="fileBytes">
        /// The raw file content bytes that make up the bundle.
        /// </param>
        /// <param name="isolationLevel">
        /// The isolation level to be used for the bundle.
        /// </param>
        /// <param name="securityLevel">
        /// The security level to be used for the bundle.
        /// </param>
        /// <param name="securityFlags">
        /// The script security flags to be used for the bundle.
        /// </param>
        /// <param name="ruleSet">
        /// The rule set to be associated with the bundle.
        /// </param>
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
        /// <summary>
        /// Stores the interpreter associated with this bundle data instance.
        /// </summary>
        private Interpreter interpreter;
        /// <summary>
        /// Gets or sets the interpreter associated with this bundle data
        /// instance.
        /// </summary>
        public Interpreter Interpreter
        {
            get { CheckDisposed(); return interpreter; }
            set { CheckDisposed(); interpreter = value; }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region IBundleData Members
        /// <summary>
        /// Stores the name of the scripting language associated with this
        /// bundle.
        /// </summary>
        private string language;
        /// <summary>
        /// Gets the name of the scripting language associated with this
        /// bundle.
        /// </summary>
        public string Language
        {
            get { CheckDisposed(); return language; }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Stores the sequence number used to order this bundle.
        /// </summary>
        private long sequence;
        /// <summary>
        /// Gets the sequence number used to order this bundle.
        /// </summary>
        public long Sequence
        {
            get { CheckDisposed(); return sequence; }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Stores the name of the vendor that produced this bundle.
        /// </summary>
        private string vendor;
        /// <summary>
        /// Gets the name of the vendor that produced this bundle.
        /// </summary>
        public string Vendor
        {
            get { CheckDisposed(); return vendor; }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Stores the file system path associated with this bundle.
        /// </summary>
        private string path;
        /// <summary>
        /// Gets the file system path associated with this bundle.
        /// </summary>
        public string Path
        {
            get { CheckDisposed(); return path; }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Stores the fully qualified name of this bundle.
        /// </summary>
        private string fullName;
        /// <summary>
        /// Gets the fully qualified name of this bundle.
        /// </summary>
        public string FullName
        {
            get { CheckDisposed(); return fullName; }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Stores the name of the hash algorithm used for this bundle.
        /// </summary>
        private string hashAlgorithmName;
        /// <summary>
        /// Gets the name of the hash algorithm used for this bundle.
        /// </summary>
        public string HashAlgorithmName
        {
            get { CheckDisposed(); return hashAlgorithmName; }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Stores the raw file content bytes that make up this bundle.
        /// </summary>
        private byte[] fileBytes;
        /// <summary>
        /// Gets the raw file content bytes that make up this bundle.
        /// </summary>
        public byte[] FileBytes
        {
            get { CheckDisposed(); return fileBytes; }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Stores the isolation level used for this bundle.
        /// </summary>
        private IsolationLevel isolationLevel;
        /// <summary>
        /// Gets the isolation level used for this bundle.
        /// </summary>
        public IsolationLevel IsolationLevel
        {
            get { CheckDisposed(); return isolationLevel; }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Stores the security level used for this bundle.
        /// </summary>
        private SecurityLevel securityLevel;
        /// <summary>
        /// Gets the security level used for this bundle.
        /// </summary>
        public SecurityLevel SecurityLevel
        {
            get { CheckDisposed(); return securityLevel; }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Stores the script security flags used for this bundle.
        /// </summary>
        private ScriptSecurityFlags securityFlags;
        /// <summary>
        /// Gets the script security flags used for this bundle.
        /// </summary>
        public ScriptSecurityFlags SecurityFlags
        {
            get { CheckDisposed(); return securityFlags; }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Stores the rule set associated with this bundle.
        /// </summary>
        private IRuleSet ruleSet;
        /// <summary>
        /// Gets the rule set associated with this bundle.
        /// </summary>
        public IRuleSet RuleSet
        {
            get { CheckDisposed(); return ruleSet; }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method marks this bundle as immutable by setting the immutable
        /// script security flag.  Once called, this cannot be undone from
        /// outside this class, by design, for the sake of security.
        /// </summary>
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
        /// <summary>
        /// When non-zero, this bundle data instance has been disposed and
        /// should no longer be used.
        /// </summary>
        private bool disposed;
        /// <summary>
        /// This method throws an exception if this bundle data instance has
        /// already been disposed.  It is called at the start of most members to
        /// guard against use after disposal.
        /// </summary>
        /// <exception cref="ObjectDisposedException">
        /// Thrown when this bundle data instance has been disposed and the
        /// engine is configured to throw on use of a disposed object.
        /// </exception>
        private void CheckDisposed() /* throw */
        {
#if THROW_ON_DISPOSED
            if (disposed && Engine.IsThrowOnDisposed(null, false))
                throw new ObjectDisposedException(typeof(BundleData).Name);
#endif
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method releases the resources held by this bundle data
        /// instance.  It implements the standard dispose pattern.
        /// </summary>
        /// <param name="disposing">
        /// Non-zero if this method is being called from
        /// <see cref="Dispose()" /> (i.e. deterministically); zero if it is
        /// being called from the finalizer.  When non-zero, managed resources
        /// are released.
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
        /// <summary>
        /// This method releases all resources held by this bundle data
        /// instance and suppresses finalization.
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
        /// Finalizes this bundle data instance, releasing any resources that
        /// were not released by an explicit call to <see cref="Dispose()" />.
        /// </summary>
        ~BundleData()
        {
            Dispose(false);
        }
        #endregion
    }
}
