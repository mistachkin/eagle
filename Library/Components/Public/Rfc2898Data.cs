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
using Eagle._Interfaces.Public;

namespace Eagle._Components.Public
{
#if SERIALIZATION
    [Serializable()]
#endif
    [ObjectId("11d9c581-1457-43d0-b113-241b9d3f4067")]
    public class Rfc2898Data : IRfc2898Data
    {
        #region Public Constructors
        public Rfc2898Data()
        {
            // do nothing.
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Public Methods
        public void GetData(
            bool overwrite,              /* in */
            ref string password,         /* in, out */
            ref string salt,             /* in, out */
            ref int iterationCount,      /* in, out */
            ref string hashAlgorithmName /* in, out */
            )
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
        }

        ///////////////////////////////////////////////////////////////////////

        public void SetData(
            bool overwrite,          /* in */
            string password,         /* in */
            string salt,             /* in */
            int iterationCount,      /* in */
            string hashAlgorithmName /* in */
            )
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
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region IRfc2898Data Members
        private string password;
        public string Password
        {
            private get { return password; }
            set { password = value; passwordSet = true; }
        }

        ///////////////////////////////////////////////////////////////////////

        private bool passwordSet;
        public bool PasswordSet
        {
            get { return passwordSet; }
        }

        ///////////////////////////////////////////////////////////////////////

        private string salt;
        public string Salt
        {
            private get { return salt; }
            set { salt = value; saltSet = true; }
        }

        ///////////////////////////////////////////////////////////////////////

        private bool saltSet;
        public bool SaltSet
        {
            get { return saltSet; }
        }

        ///////////////////////////////////////////////////////////////////////

        private int iterationCount;
        public int IterationCount
        {
            private get { return iterationCount; }
            set { iterationCount = value; iterationCountSet = true; }
        }

        ///////////////////////////////////////////////////////////////////////

        private bool iterationCountSet;
        public bool IterationCountSet
        {
            get { return iterationCountSet; }
        }

        ///////////////////////////////////////////////////////////////////////

        private string hashAlgorithmName;
        public string HashAlgorithmName
        {
            private get { return hashAlgorithmName; }
            set { hashAlgorithmName = value; hashAlgorithmNameSet = true; }
        }

        ///////////////////////////////////////////////////////////////////////

        private bool hashAlgorithmNameSet;
        public bool HashAlgorithmNameSet
        {
            get { return hashAlgorithmNameSet; }
        }
        #endregion
    }
}
