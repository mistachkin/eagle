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
    /// <summary>
    /// This class implements a WiX (Windows Installer XML) extension that
    /// exposes the Eagle preprocessor extension to the WiX toolset, allowing
    /// Eagle scripts to be used during preprocessing of installer source
    /// files.
    /// </summary>
    [ObjectId("a3364225-8d90-4d08-899c-354dfeef1231")]
    internal sealed class Eagle : WixExtension, IDisposable
    {
        #region Private Data
        /// <summary>
        /// The cached preprocessor extension instance returned to the WiX
        /// toolset, created on first access.
        /// </summary>
        private Preprocessor extension;
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Public Constructors
        /// <summary>
        /// Constructs a new instance of this WiX extension.
        /// </summary>
        public Eagle()
        {
            // do nothing.
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region WixExtension Members
        /// <summary>
        /// Gets the preprocessor extension provided by this WiX extension,
        /// creating it on first access.
        /// </summary>
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
        /// <summary>
        /// Stores a value indicating whether this extension has been disposed.
        /// </summary>
        private bool disposed;
        /// <summary>
        /// This method throws an exception if this extension has already been
        /// disposed.  It is called to guard against use after disposal.
        /// </summary>
        /// <exception cref="ObjectDisposedException">
        /// Thrown when this extension has been disposed and the engine is
        /// configured to throw on use of a disposed object.
        /// </exception>
        private void CheckDisposed() /* throw */
        {
#if THROW_ON_DISPOSED
            if (disposed && Engine.IsThrowOnDisposed(null, false))
                throw new ObjectDisposedException(typeof(Eagle).Name);
#endif
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method releases the resources held by this extension.  It
        /// implements the standard dispose pattern.
        /// </summary>
        /// <param name="disposing">
        /// Non-zero if this method is being called from
        /// <see cref="Dispose()" /> (i.e. deterministically); zero if it is
        /// being called from the finalizer.  When non-zero, managed resources
        /// are released.
        /// </param>
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
        /// <summary>
        /// This method releases all resources held by this extension and
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
        /// Finalizes this extension, releasing any resources that were not
        /// released by an explicit call to <see cref="Dispose()" />.
        /// </summary>
        ~Eagle()
        {
            Dispose(false);
        }
        #endregion
    }
}
