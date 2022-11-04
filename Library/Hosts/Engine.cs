/*
 * Engine.cs --
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
    [ObjectId("a188f92b-33a8-4784-9c4c-a11ebc6f1fd5")]
    public abstract class Engine : Default, IDisposable
    {
        #region Protected Constructors
        protected Engine(
            IHostData hostData
            )
            : base(hostData)
        {
            // do nothing.
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
                // NOTE: We support the "CreateThread", "QueueWorkItem",
                //       "Sleep", and "Yield" methods.
                //
                hostFlags = HostFlags.Thread | HostFlags.WorkItem |
                            HostFlags.Sleep | HostFlags.Yield |
                            base.MaybeInitializeHostFlags();
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

        #region IInteractiveHost Members
        private HostFlags hostFlags = HostFlags.Invalid;
        public override HostFlags GetHostFlags()
        {
            CheckDisposed();

            return MaybeInitializeHostFlags();
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region IThreadHost Members
        public override ReturnCode CreateThread(
            ThreadStart start,
            int maxStackSize,
            bool userInterface,
            bool isBackground,
            bool useActiveStack,
            ref Thread thread,
            ref Result error
            )
        {
            CheckDisposed();

            try
            {
                thread = _Engine.CreateThread(
                    start, maxStackSize, userInterface, isBackground,
                    useActiveStack);

                return ReturnCode.Ok;
            }
            catch (Exception e)
            {
                error = e;
            }

            return ReturnCode.Error;
        }

        ///////////////////////////////////////////////////////////////////////

        public override ReturnCode CreateThread(
            ParameterizedThreadStart start,
            int maxStackSize,
            bool userInterface,
            bool isBackground,
            bool useActiveStack,
            ref Thread thread,
            ref Result error
            )
        {
            CheckDisposed();

            try
            {
                thread = _Engine.CreateThread(
                    start, maxStackSize, userInterface, isBackground,
                    useActiveStack);

                return ReturnCode.Ok;
            }
            catch (Exception e)
            {
                error = e;
            }

            return ReturnCode.Error;
        }

        ///////////////////////////////////////////////////////////////////////

        public override ReturnCode QueueWorkItem(
            WaitCallback callback,
            object state,
            ref Result error
            )
        {
            CheckDisposed();

            try
            {
                if (_Engine.QueueWorkItem(callback, state))
                    return ReturnCode.Ok;
                else
                    error = "could not queue work item";
            }
            catch (Exception e)
            {
                error = e;
            }

            return ReturnCode.Error;
        }

        ///////////////////////////////////////////////////////////////////////

        public override bool Sleep(
            int milliseconds
            )
        {
            CheckDisposed();

            try
            {
                if (HostOps.ThreadSleepOrMaybeComplain(
                        milliseconds, false) == ReturnCode.Ok)
                {
                    return true;
                }
            }
            catch (Exception e)
            {
                TraceOps.DebugTrace(
                    e, typeof(Engine).Name,
                    TracePriority.HostError);
            }

            return false;
        }

        ///////////////////////////////////////////////////////////////////////

        public override bool Yield()
        {
            CheckDisposed();

            try
            {
                if (HostOps.YieldOrMaybeComplain() == ReturnCode.Ok)
                {
                    return true;
                }
            }
            catch (Exception e)
            {
                TraceOps.DebugTrace(
                    e, typeof(Engine).Name,
                    TracePriority.HostError);
            }

            return false;
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

        #region IMaybeDisposed Members
        public override bool Disposed
        {
            get { return disposed; }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region IDisposable "Pattern" Members
        private bool disposed;
        private void CheckDisposed() /* throw */
        {
#if THROW_ON_DISPOSED
            if (disposed && _Engine.IsThrowOnDisposed(null, false))
                throw new InterpreterDisposedException(typeof(Engine));
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
