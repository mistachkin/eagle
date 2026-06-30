/*
 * Custom.cs --
 *
 * Copyright (c) 2007-2012 by Joe Mistachkin.  All rights reserved.
 *
 * See the file "license.terms" for information on usage and redistribution of
 * this file, and for a DISCLAIMER OF ALL WARRANTIES.
 *
 * RCS: @(#) $Id: $
 */

#if !CONSOLE
#error "This file cannot be compiled or used properly with console support disabled."
#endif

using System;
using Eagle._Attributes;
using Eagle._Components.Public;
using Eagle._Interfaces.Public;

using _Engine = Eagle._Components.Public.Engine;

namespace Eagle._Hosts
{
    /// <summary>
    /// This class implements a custom console host used for testing purposes.
    /// It extends the standard console host without adding any specialized
    /// behavior.
    /// </summary>
    [ObjectId("fbce00e2-9408-42bf-ac13-06865c5928ad")]
    internal sealed class Custom : Eagle._Hosts.Console, IDisposable
    {
        #region Public Constructors
        /// <summary>
        /// Constructs an instance of this custom console host.
        /// </summary>
        /// <param name="hostData">
        /// The data used to create and configure this host.  This parameter may
        /// be null.
        /// </param>
        public Custom(
            IHostData hostData
            )
            : base(hostData)
        {
            // do nothing.
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region IDebugHost Members
        /// <summary>
        /// Creates a copy of this host.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter context the cloned host will be associated with.
        /// </param>
        /// <returns>
        /// The newly created copy of this host, or null if it could not be
        /// created.
        /// </returns>
        public override IHost Clone(
            Interpreter interpreter
            )
        {
            CheckDisposed();

            return new Custom(new HostData(
                Name, Group, Description, ClientData, typeof(Custom).Name,
                interpreter, ResourceManager, Profile, HostCreateFlags));
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region IMaybeDisposed Members
        /// <summary>
        /// Gets a value indicating whether this object has been disposed.
        /// </summary>
        public override bool Disposed
        {
            get { return disposed; }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region IDisposable "Pattern" Members
        /// <summary>
        /// Stores a value indicating whether this host has been disposed.
        /// </summary>
        private bool disposed;
        /// <summary>
        /// This method throws an exception if this host has already been
        /// disposed.  It is called at the start of most members to guard against
        /// use after disposal.
        /// </summary>
        private void CheckDisposed() /* throw */
        {
#if THROW_ON_DISPOSED
            if (disposed && _Engine.IsThrowOnDisposed(
                    SafeGetInterpreter(), null))
            {
                throw new InterpreterDisposedException(typeof(Custom));
            }
#endif
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method releases the resources held by this host.  It implements
        /// the standard dispose pattern.
        /// </summary>
        /// <param name="disposing">
        /// Non-zero if this method is being called from <see cref="Dispose()" />
        /// (i.e. deterministically); zero if it is being called from the
        /// finalizer.  When non-zero, managed resources are released.
        /// </param>
        protected override void Dispose(bool disposing)
        {
            try
            {
                if (!disposed)
                {
                    //if (disposing)
                    //{
                    //    ////////////////////////////////////
                    //    // dispose managed resources here...
                    //    ////////////////////////////////////
                    //}

                    //////////////////////////////////////
                    // release unmanaged resources here...
                    //////////////////////////////////////
                }
            }
            finally
            {
                base.Dispose(disposing);

                disposed = true;
            }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Destructor
        /// <summary>
        /// Finalizes this host, releasing any resources that were not released
        /// by an explicit call to <see cref="Dispose()" />.
        /// </summary>
        ~Custom()
        {
            Dispose(false);
        }
        #endregion
    }
}
