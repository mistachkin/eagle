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
#if SERIALIZATION
    [Serializable()]
#endif
    [ObjectId("11d9c581-1457-43d0-b113-241b9d3f4067")]
    public class Rfc2898Data : IRfc2898Data, IDisposable
    {
        #region Private Data
        private readonly object syncRoot = new object();
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Public Constructors
        public Rfc2898Data()
        {
            // do nothing.
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Public Methods
        public virtual void ClearData()
        {
            CheckDisposed();

            ResetData();
        }

        ///////////////////////////////////////////////////////////////////////

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
        private string password;
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

        private bool passwordSet;
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

        private string salt;
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

        private bool saltSet;
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

        private int iterationCount;
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

        private bool iterationCountSet;
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

        private string hashAlgorithmName;
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

        private bool hashAlgorithmNameSet;
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

        private string signature;
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

        private bool signatureSet;
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
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region IDisposable "Pattern" Members
        private bool disposed;
        private void CheckDisposed() /* throw */
        {
#if THROW_ON_DISPOSED
            if (disposed && Engine.IsThrowOnDisposed(null, null))
                throw new ObjectDisposedException(typeof(Rfc2898Data).Name);
#endif
        }

        ///////////////////////////////////////////////////////////////////////

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
        ~Rfc2898Data()
        {
            Dispose(false);
        }
        #endregion
    }
}
