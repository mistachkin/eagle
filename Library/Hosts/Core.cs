/*
 * Core.cs --
 *
 * Copyright (c) 2007-2012 by Joe Mistachkin.  All rights reserved.
 *
 * See the file "license.terms" for information on usage and redistribution of
 * this file, and for a DISCLAIMER OF ALL WARRANTIES.
 *
 * RCS: @(#) $Id: $
 */

using System;
using System.Threading;
using Eagle._Attributes;
using Eagle._Components.Private;
using Eagle._Components.Public;
using Eagle._Interfaces.Public;

using _Engine = Eagle._Components.Public.Engine;

namespace Eagle._Hosts
{
    /// <summary>
    /// This class is an abstract host base class that builds on the shell host.
    /// It adds interpreter lookup, read and write level tracking, host flag
    /// management, process-exit gating, and host cloning support.  It serves as
    /// the common core base for the concrete host classes in the host class
    /// hierarchy.
    /// </summary>
    [ObjectId("0969beae-3d4a-42bf-b514-c7bc18bd6071")]
    public abstract class Core : Shell, IDisposable
    {
        #region Protected Constructors
        /// <summary>
        /// Constructs an instance of this host class.
        /// </summary>
        /// <param name="hostData">
        /// The host data used to initialize this host, if any.  This parameter
        /// may be null.
        /// </param>
        protected Core(
            IHostData hostData
            )
            : base(hostData)
        {
            // do nothing.
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Interpreter Support
        /// <summary>
        /// This method gets the interpreter associated with this host, using a
        /// thread-safe lookup.
        /// </summary>
        /// <returns>
        /// The interpreter associated with this host, or null if there is none.
        /// </returns>
        protected Interpreter SafeGetInterpreter()
        {
            return InternalSafeGetInterpreter(true);
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Host Flags Support
        /// <summary>
        /// This method invalidates the cached host flags so that they will be
        /// recomputed the next time they are requested.
        /// </summary>
        private void PrivateResetHostFlagsOnly()
        {
            hostFlags = HostFlags.Invalid;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method invalidates the cached host flags and then resets the
        /// base host flags.
        /// </summary>
        /// <returns>
        /// True if the base host flags were reset; otherwise, false.
        /// </returns>
        private bool PrivateResetHostFlags()
        {
            PrivateResetHostFlagsOnly();
            return base.ResetHostFlags();
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method computes and caches the host flags for this host, if
        /// they have not already been computed.  It adds no flags of its own
        /// beyond those provided by the base host.
        /// </summary>
        /// <returns>
        /// The host flags for this host.
        /// </returns>
        protected override HostFlags MaybeInitializeHostFlags()
        {
            if (hostFlags == HostFlags.Invalid)
            {
                //
                // NOTE: We support nothing special.
                //
                hostFlags = base.MaybeInitializeHostFlags();
            }

            return hostFlags;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method records whether an exception was encountered while
        /// reading from the host and invalidates the cached host flags.
        /// </summary>
        /// <param name="exception">
        /// Non-zero if an exception was encountered while reading from the
        /// host; otherwise, zero.
        /// </param>
        protected override void SetReadException(
            bool exception
            )
        {
            base.SetReadException(exception);
            PrivateResetHostFlagsOnly();
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method records whether an exception was encountered while
        /// writing to the host and invalidates the cached host flags.
        /// </summary>
        /// <param name="exception">
        /// Non-zero if an exception was encountered while writing to the
        /// host; otherwise, zero.
        /// </param>
        protected override void SetWriteException(
            bool exception
            )
        {
            base.SetWriteException(exception);
            PrivateResetHostFlagsOnly();
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Host Read/Write Levels Support
        /// <summary>
        /// This method increments the count of active host read operations on
        /// this host.
        /// </summary>
        protected virtual void EnterReadLevel()
        {
            // CheckDisposed();

            Interlocked.Increment(ref readLevels);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method decrements the count of active host read operations on
        /// this host.
        /// </summary>
        protected virtual void ExitReadLevel()
        {
            // CheckDisposed();

            Interlocked.Decrement(ref readLevels);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method increments the count of active host write operations on
        /// this host.
        /// </summary>
        protected virtual void EnterWriteLevel()
        {
            // CheckDisposed();

            Interlocked.Increment(ref writeLevels);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method decrements the count of active host write operations on
        /// this host.
        /// </summary>
        protected virtual void ExitWriteLevel()
        {
            // CheckDisposed();

            Interlocked.Decrement(ref writeLevels);
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region IInteractiveHost Members
        /// <summary>
        /// The cached host flags for this host, or
        /// <see cref="HostFlags.Invalid" /> when they have not yet been
        /// computed.
        /// </summary>
        private HostFlags hostFlags = HostFlags.Invalid;
        /// <summary>
        /// This method gets the host flags for this host, computing and caching
        /// them on first use.
        /// </summary>
        /// <returns>
        /// The host flags for this host.
        /// </returns>
        public override HostFlags GetHostFlags()
        {
            CheckDisposed();

            return MaybeInitializeHostFlags();
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// The count of host read operations currently in progress on this
        /// host.
        /// </summary>
        private int readLevels;
        /// <summary>
        /// Gets the number of host read operations currently in progress on
        /// this host.
        /// </summary>
        public override int ReadLevels
        {
            get
            {
                CheckDisposed();

                int localReadLevels = Interlocked.CompareExchange(
                    ref readLevels, 0, 0);

                return localReadLevels;
            }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// The count of host write operations currently in progress on this
        /// host.
        /// </summary>
        private int writeLevels;
        /// <summary>
        /// Gets the number of host write operations currently in progress on
        /// this host.
        /// </summary>
        public override int WriteLevels
        {
            get
            {
                CheckDisposed();

                int localWriteLevels = Interlocked.CompareExchange(
                    ref writeLevels, 0, 0);

                return localWriteLevels;
            }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region IProcessHost Members
        /// <summary>
        /// Gets a value indicating whether this host permits the interpreter to
        /// exit.  This returns false when the no-exit configuration value is
        /// present; otherwise, it defers to the base host.
        /// </summary>
        public override bool CanExit
        {
            get
            {
                CheckDisposed();

                //
                // NOTE: This configuration parameter is considered to be
                //       part of the configuration of the interpreter itself,
                //       hence those flags are used here.
                //
                if (GlobalConfiguration.DoesValueExist(EnvVars.NoExit,
                        ConfigurationFlags.Interpreter)) /* EXEMPT */
                {
                    return false;
                }

                return base.CanExit;
            }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region IDebugHost Members
        /// <summary>
        /// This method creates a copy of this host that is associated with the
        /// interpreter currently associated with this host.
        /// </summary>
        /// <returns>
        /// The newly created copy of this host.
        /// </returns>
        public override IHost Clone()
        {
            CheckDisposed();

            return Clone(UnsafeGetInterpreter());
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region IHost Members
        /// <summary>
        /// This method resets this host's configuration flags to their default
        /// values.
        /// </summary>
        /// <returns>
        /// True if the flags were reset; otherwise, false.
        /// </returns>
        public override bool ResetHostFlags()
        {
            CheckDisposed();

            return PrivateResetHostFlags();
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method resets this host to its initial state, including
        /// resetting its host flags.
        /// </summary>
        /// <param name="error">
        /// Upon failure, this contains an appropriate error message.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise,
        /// <see cref="ReturnCode.Error" /> with details placed in
        /// <paramref name="error" />.
        /// </returns>
        public override ReturnCode Reset(
            ref Result error
            )
        {
            CheckDisposed();

            if (base.Reset(ref error) == ReturnCode.Ok)
            {
                if (!PrivateResetHostFlags()) /* NON-VIRTUAL */
                {
                    error = "failed to reset flags";
                    return ReturnCode.Error;
                }

                return ReturnCode.Ok;
            }

            return ReturnCode.Error;
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region IMaybeDisposed Members
        /// <summary>
        /// Gets a value indicating whether this host has been disposed.
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
        /// disposed.  It is called at the start of most members to guard
        /// against use after disposal.
        /// </summary>
        /// <exception cref="InterpreterDisposedException">
        /// Thrown when this host has been disposed and the engine is configured
        /// to throw on use of a disposed object.
        /// </exception>
        private void CheckDisposed() /* throw */
        {
#if THROW_ON_DISPOSED
            if (disposed && _Engine.IsThrowOnDisposed(
                    InternalSafeGetInterpreter(false), null))
            {
                throw new InterpreterDisposedException(typeof(Core));
            }
#endif
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method releases the resources held by this host.  It implements
        /// the standard dispose pattern.
        /// </summary>
        /// <param name="disposing">
        /// Non-zero if this method is being called from the
        /// <see cref="IDisposable.Dispose" /> method (i.e.
        /// deterministically); zero if it is being called from the finalizer.
        /// When non-zero, managed resources are released.
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
    }
}
