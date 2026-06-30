/*
 * Rfc2898Data.cs --
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
using Eagle._Interfaces.Public;

namespace Eagle._Components.Public
{
    /// <summary>
    /// This class holds the parameters used to derive a key with the PBKDF2
    /// algorithm (RFC 2898) -- the password, salt, iteration count, hash
    /// algorithm name, and signature -- along with a flag for each value that
    /// indicates whether it has been set.  Access to the wrapped values is
    /// synchronized, and the class is disposable so that the sensitive values
    /// it holds can be cleared (and, on supported platforms, zeroed) when they
    /// are no longer needed.  It implements <see cref="IRfc2898Data" />.
    /// </summary>
#if SERIALIZATION
    [Serializable()]
#endif
    [ObjectId("11d9c581-1457-43d0-b113-241b9d3f4067")]
    public class Rfc2898Data : IRfc2898Data, IDisposable
    {
        #region Private Data
        /// <summary>
        /// The object used to synchronize access to the wrapped values of this
        /// instance.
        /// </summary>
        private readonly object syncRoot = new object();
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Public Constructors
        /// <summary>
        /// Constructs an empty instance with no values set.
        /// </summary>
        public Rfc2898Data()
        {
            // do nothing.
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Public Methods
        /// <summary>
        /// This method clears all of the wrapped values, resetting each to its
        /// default and marking each as not set.
        /// </summary>
        public virtual void ClearData()
        {
            CheckDisposed();

            ResetData();
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method copies the values that have been set on this instance
        /// into the supplied parameters.  For each value, the corresponding
        /// parameter is updated only when <paramref name="overwrite" /> is
        /// non-zero or the parameter currently holds its default (null or, for
        /// the iteration count, a non-positive value).
        /// </summary>
        /// <param name="overwrite">
        /// Non-zero to always overwrite the supplied parameters with the values
        /// set on this instance; zero to only fill in parameters that currently
        /// hold their default.
        /// </param>
        /// <param name="password">
        /// On output, receives the password when it has been set and the
        /// overwrite rules permit.
        /// </param>
        /// <param name="salt">
        /// On output, receives the salt when it has been set and the overwrite
        /// rules permit.
        /// </param>
        /// <param name="iterationCount">
        /// On output, receives the iteration count when it has been set and the
        /// overwrite rules permit.
        /// </param>
        /// <param name="hashAlgorithmName">
        /// On output, receives the hash algorithm name when it has been set and
        /// the overwrite rules permit.
        /// </param>
        /// <param name="signature">
        /// On output, receives the signature when it has been set and the
        /// overwrite rules permit.
        /// </param>
        public virtual void GetData(
            bool overwrite,               /* in */
            ref string password,          /* in, out */
            ref string salt,              /* in, out */
            ref int iterationCount,       /* in, out */
            ref string hashAlgorithmName, /* in, out */
            ref string signature          /* in, out */
            )
        {
            CheckDisposed();

            lock (syncRoot) /* TRANSACTIONAL */
            {
                if (this.PasswordSet)
                {
                    if (overwrite || (password == null))
                        password = this.Password;
                }

                if (this.SaltSet)
                {
                    if (overwrite || (salt == null))
                        salt = this.Salt;
                }

                if (this.IterationCountSet)
                {
                    if (overwrite || (iterationCount <= 0))
                        iterationCount = this.IterationCount;
                }

                if (this.HashAlgorithmNameSet)
                {
                    if (overwrite || (hashAlgorithmName == null))
                        hashAlgorithmName = this.HashAlgorithmName;
                }

                if (this.SignatureSet)
                {
                    if (overwrite || (signature == null))
                        signature = this.Signature;
                }
            }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method stores the supplied values on this instance.  For each
        /// value, the stored value is updated only when
        /// <paramref name="overwrite" /> is non-zero or the value has not yet
        /// been set, and only when the supplied value is non-default (non-null
        /// or, for the iteration count, positive).
        /// </summary>
        /// <param name="overwrite">
        /// Non-zero to always overwrite the values set on this instance with
        /// the supplied values; zero to only set values that have not yet been
        /// set.
        /// </param>
        /// <param name="password">
        /// The password to store.  This parameter may be null, in which case
        /// the password is not changed.
        /// </param>
        /// <param name="salt">
        /// The salt to store.  This parameter may be null, in which case the
        /// salt is not changed.
        /// </param>
        /// <param name="iterationCount">
        /// The iteration count to store.  When not positive, the iteration
        /// count is not changed.
        /// </param>
        /// <param name="hashAlgorithmName">
        /// The hash algorithm name to store.  This parameter may be null, in
        /// which case the hash algorithm name is not changed.
        /// </param>
        /// <param name="signature">
        /// The signature to store.  This parameter may be null, in which case
        /// the signature is not changed.
        /// </param>
        public virtual void SetData(
            bool overwrite,           /* in */
            string password,          /* in */
            string salt,              /* in */
            int iterationCount,       /* in */
            string hashAlgorithmName, /* in */
            string signature          /* in */
            )
        {
            CheckDisposed();

            lock (syncRoot) /* TRANSACTIONAL */
            {
                if (overwrite || !this.PasswordSet)
                {
                    if (password != null)
                        this.Password = password;
                }

                if (overwrite || !this.SaltSet)
                {
                    if (salt != null)
                        this.Salt = salt;
                }

                if (overwrite || !this.IterationCountSet)
                {
                    if (iterationCount > 0)
                        this.IterationCount = iterationCount;
                }

                if (overwrite || !this.HashAlgorithmNameSet)
                {
                    if (hashAlgorithmName != null)
                        this.HashAlgorithmName = hashAlgorithmName;
                }

                if (overwrite || !this.SignatureSet)
                {
                    if (signature != null)
                        this.Signature = signature;
                }
            }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method copies the values from the specified source instance
        /// into this instance, optionally clearing the source afterward.
        /// </summary>
        /// <param name="rfc2898Data">
        /// The source instance whose values are copied into this instance.
        /// This parameter may be null and must be an instance of this class.
        /// </param>
        /// <param name="overwrite">
        /// Non-zero to always overwrite the values set on this instance with
        /// the values from the source; zero to only fill in values that have
        /// not yet been set.
        /// </param>
        /// <param name="move">
        /// Non-zero to clear all of the values on the source instance after
        /// they have been copied.
        /// </param>
        /// <returns>
        /// True if the values were copied; otherwise, false (e.g. when the
        /// source instance is null or is not an instance of this class).
        /// </returns>
        public virtual bool CopyData(
            IRfc2898Data rfc2898Data, /* in, out */
            bool overwrite,           /* in */
            bool move                 /* in */
            )
        {
            lock (syncRoot) /* TRANSACTIONAL */
            {
                if (rfc2898Data == null)
                    return false;

                Rfc2898Data localRfc2898Data = rfc2898Data as Rfc2898Data;

                if (localRfc2898Data == null)
                    return false;

                localRfc2898Data.GetData(
                    overwrite, ref password, ref salt, ref iterationCount,
                    ref hashAlgorithmName, ref signature);

                if (move)
                    localRfc2898Data.ClearData();

                return true;
            }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Protected Methods
        /// <summary>
        /// This method gets the hash algorithm name currently stored on this
        /// instance.
        /// </summary>
        /// <returns>
        /// The hash algorithm name, or null if it has not been set.
        /// </returns>
        protected virtual string GetHashAlgorithmName()
        {
            lock (syncRoot)
            {
                return hashAlgorithmName;
            }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Private Methods
        /// <summary>
        /// This method resets all of the wrapped values to their defaults and
        /// marks each as not set.  On supported platforms, the sensitive string
        /// values are zeroed before being released.
        /// </summary>
        private void ResetData()
        {
            lock (syncRoot) /* TRANSACTIONAL */
            {
                if (password != null)
                {
#if !MONO && NATIVE && WINDOWS
                    /* IGNORED */
                    StringOps.ZeroStringOrTrace(password);
#endif

                    password = null;
                }

                passwordSet = false;

                if (salt != null)
                {
#if !MONO && NATIVE && WINDOWS
                    /* IGNORED */
                    StringOps.ZeroStringOrTrace(salt);
#endif

                    salt = null;
                }

                saltSet = false;

                iterationCount = 0;
                iterationCountSet = false;

                hashAlgorithmName = null;
                hashAlgorithmNameSet = false;

                if (signature != null)
                {
#if !MONO && NATIVE && WINDOWS
                    /* IGNORED */
                    StringOps.ZeroStringOrTrace(signature);
#endif

                    signature = null;
                }

                signatureSet = false;
            }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region IRfc2898Data Members
        /// <summary>
        /// The wrapped password value.
        /// </summary>
        private string password;
        /// <summary>
        /// Gets or sets the password value.
        /// </summary>
        public string Password
        {
            private get
            {
                lock (syncRoot)
                {
                    return password;
                }
            }
            set
            {
                CheckDisposed();

                lock (syncRoot) /* TRANSACTIONAL */
                {
                    password = value;
                    passwordSet = true;
                }
            }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Non-zero if the password value has been set.
        /// </summary>
        private bool passwordSet;
        /// <summary>
        /// Gets a value indicating whether the password value has been set.
        /// </summary>
        public bool PasswordSet
        {
            get
            {
                CheckDisposed();

                lock (syncRoot)
                {
                    return passwordSet;
                }
            }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// The wrapped salt value.
        /// </summary>
        private string salt;
        /// <summary>
        /// Gets or sets the salt value.
        /// </summary>
        public string Salt
        {
            private get
            {
                lock (syncRoot)
                {
                    return salt;
                }
            }
            set
            {
                CheckDisposed();

                lock (syncRoot) /* TRANSACTIONAL */
                {
                    salt = value;
                    saltSet = true;
                }
            }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Non-zero if the salt value has been set.
        /// </summary>
        private bool saltSet;
        /// <summary>
        /// Gets a value indicating whether the salt value has been set.
        /// </summary>
        public bool SaltSet
        {
            get
            {
                CheckDisposed();

                lock (syncRoot)
                {
                    return saltSet;
                }
            }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// The wrapped iteration count value.
        /// </summary>
        private int iterationCount;
        /// <summary>
        /// Gets or sets the iteration count value.
        /// </summary>
        public int IterationCount
        {
            private get
            {
                lock (syncRoot)
                {
                    return iterationCount;
                }
            }
            set
            {
                CheckDisposed();

                lock (syncRoot) /* TRANSACTIONAL */
                {
                    iterationCount = value;
                    iterationCountSet = true;
                }
            }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Non-zero if the iteration count value has been set.
        /// </summary>
        private bool iterationCountSet;
        /// <summary>
        /// Gets a value indicating whether the iteration count value has been
        /// set.
        /// </summary>
        public bool IterationCountSet
        {
            get
            {
                CheckDisposed();

                lock (syncRoot)
                {
                    return iterationCountSet;
                }
            }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// The wrapped hash algorithm name value.
        /// </summary>
        private string hashAlgorithmName;
        /// <summary>
        /// Gets or sets the hash algorithm name value.
        /// </summary>
        public string HashAlgorithmName
        {
            private get
            {
                lock (syncRoot)
                {
                    return hashAlgorithmName;
                }
            }
            set
            {
                CheckDisposed();

                lock (syncRoot) /* TRANSACTIONAL */
                {
                    hashAlgorithmName = value;
                    hashAlgorithmNameSet = true;
                }
            }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Non-zero if the hash algorithm name value has been set.
        /// </summary>
        private bool hashAlgorithmNameSet;
        /// <summary>
        /// Gets a value indicating whether the hash algorithm name value has
        /// been set.
        /// </summary>
        public bool HashAlgorithmNameSet
        {
            get
            {
                CheckDisposed();

                lock (syncRoot)
                {
                    return hashAlgorithmNameSet;
                }
            }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// The wrapped signature value.
        /// </summary>
        private string signature;
        /// <summary>
        /// Gets or sets the signature value.
        /// </summary>
        public string Signature
        {
            private get
            {
                lock (syncRoot)
                {
                    return signature;
                }
            }
            set
            {
                CheckDisposed();

                lock (syncRoot) /* TRANSACTIONAL */
                {
                    signature = value;
                    signatureSet = true;
                }
            }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Non-zero if the signature value has been set.
        /// </summary>
        private bool signatureSet;
        /// <summary>
        /// Gets a value indicating whether the signature value has been set.
        /// </summary>
        public bool SignatureSet
        {
            get
            {
                CheckDisposed();

                lock (syncRoot)
                {
                    return signatureSet;
                }
            }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region IDisposable Members
        /// <summary>
        /// This method releases all resources held by this instance, clearing
        /// and (on supported platforms) zeroing its sensitive values, and
        /// suppresses finalization.
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region IDisposable "Pattern" Members
        /// <summary>
        /// Non-zero if this instance has been disposed.
        /// </summary>
        private bool disposed;
        /// <summary>
        /// This method throws an exception if this instance has been disposed
        /// and the interpreter is configured to throw in that situation.
        /// </summary>
        private void CheckDisposed() /* throw */
        {
#if THROW_ON_DISPOSED
            if (disposed && Engine.IsThrowOnDisposed(null, null))
                throw new ObjectDisposedException(typeof(Rfc2898Data).Name);
#endif
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method releases the resources held by this instance.  When
        /// disposing, the managed sensitive values are cleared and (on
        /// supported platforms) zeroed.
        /// </summary>
        /// <param name="disposing">
        /// Non-zero if this method is being called from the
        /// <c>Dispose</c> method; zero if it is being called from the
        /// finalizer.
        /// </param>
        protected virtual void Dispose(
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

                        /* NO RESULT */
                        ResetData();
                    }

                    //////////////////////////////////////
                    // release unmanaged resources here...
                    //////////////////////////////////////
                }
            }
            finally
            {
                disposed = true;
            }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Destructor
        /// <summary>
        /// Finalizes this instance, releasing any resources it still holds.
        /// </summary>
        ~Rfc2898Data()
        {
            Dispose(false);
        }
        #endregion
    }
}
