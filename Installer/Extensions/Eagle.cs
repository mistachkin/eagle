/*
 * Eagle.cs --
 *
 * Copyright (c) 2007-2012 by Joe Mistachkin.  All rights reserved.
 *
 * See the file "license.terms" for information on usage and redistribution of
 * this file, and for a DISCLAIMER OF ALL WARRANTIES.
 *
 * RCS: @(#) $Id: $
 */

using System;
using Microsoft.Tools.WindowsInstallerXml;
using Eagle._Attributes;
using Eagle._Components.Public;

namespace Eagle._Extensions
{
    [ObjectId("a3364225-8d90-4d08-899c-354dfeef1231")]
    internal sealed class Eagle : WixExtension, IDisposable
    {
        #region Private Data
        private Preprocessor extension;
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Public Constructors
        public Eagle()
        {
            // do nothing.
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region WixExtension Members
        public override PreprocessorExtension PreprocessorExtension
        {
            get
            {
                CheckDisposed();

                if (extension == null)
                    extension = new Preprocessor();

                return extension;
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
                throw new ObjectDisposedException(typeof(Eagle).Name);
#endif
        }

        ///////////////////////////////////////////////////////////////////////

        private /* protected virtual */ void Dispose(bool disposing)
        {
            if (!disposed)
            {
                if (disposing)
                {
                    ////////////////////////////////////
                    // dispose managed resources here...
                    ////////////////////////////////////

                    if (extension != null)
                    {
                        extension.Dispose(); /* throw */
                        extension = null;
                    }
                }

                //////////////////////////////////////
                // release unmanaged resources here...
                //////////////////////////////////////

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
        ~Eagle()
        {
            Dispose(false);
        }
        #endregion
    }
}
