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
    [ObjectId("0969beae-3d4a-42bf-b514-c7bc18bd6071")]
    public abstract class Core : Shell, IDisposable
    {
        #region Protected Constructors
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
        protected Interpreter SafeGetInterpreter()
        {
            return InternalSafeGetInterpreter(true);
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Host Flags Support
        private void PrivateResetHostFlagsOnly()
        {
            hostFlags = HostFlags.Invalid;
        }

        ///////////////////////////////////////////////////////////////////////

        private bool PrivateResetHostFlags()
        {
            PrivateResetHostFlagsOnly();
            return base.ResetHostFlags();
        }

        ///////////////////////////////////////////////////////////////////////

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

        protected override void SetReadException(
            bool exception
            )
        {
            base.SetReadException(exception);
            PrivateResetHostFlagsOnly();
        }

        ///////////////////////////////////////////////////////////////////////

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
        protected virtual void EnterReadLevel()
        {
            // CheckDisposed();

            Interlocked.Increment(ref readLevels);
        }

        ///////////////////////////////////////////////////////////////////////

        protected virtual void ExitReadLevel()
        {
            // CheckDisposed();

            Interlocked.Decrement(ref readLevels);
        }

        ///////////////////////////////////////////////////////////////////////

        protected virtual void EnterWriteLevel()
        {
            // CheckDisposed();

            Interlocked.Increment(ref writeLevels);
        }

        ///////////////////////////////////////////////////////////////////////

        protected virtual void ExitWriteLevel()
        {
            // CheckDisposed();

            Interlocked.Decrement(ref writeLevels);
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region IInteractiveHost Members
        private HostFlags hostFlags = HostFlags.Invalid;
        public override HostFlags GetHostFlags()
        {
            CheckDisposed();

            return MaybeInitializeHostFlags();
        }

        ///////////////////////////////////////////////////////////////////////

        private int readLevels;
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

        private int writeLevels;
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
        public override IHost Clone()
        {
            CheckDisposed();

            return Clone(UnsafeGetInterpreter());
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region IHost Members
        public override bool ResetHostFlags()
        {
            CheckDisposed();

            return PrivateResetHostFlags();
        }

        ///////////////////////////////////////////////////////////////////////

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

        #region IDisposable "Pattern" Members
        private bool disposed;
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
