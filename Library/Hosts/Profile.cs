/*
 * Profile.cs --
 *
 * Copyright (c) 2007-2012 by Joe Mistachkin.  All rights reserved.
 *
 * See the file "license.terms" for information on usage and redistribution of
 * this file, and for a DISCLAIMER OF ALL WARRANTIES.
 *
 * RCS: @(#) $Id: $
 */

using System;
using System.Globalization;
using System.Text;
using Eagle._Attributes;
using Eagle._Components.Private;
using Eagle._Components.Public;
using Eagle._Interfaces.Public;

using _Engine = Eagle._Components.Public.Engine;

#if !CONSOLE
using ConsoleColor = Eagle._Components.Public.ConsoleColor;
#endif

#if NET_STANDARD_21
using Index = Eagle._Constants.Index;
#endif

namespace Eagle._Hosts
{
    [ObjectId("77ba621d-3229-42a4-874f-315976ebf742")]
    public abstract class Profile : File, IDisposable
    {
        #region Protected Constructors
        protected Profile(
            IHostData hostData
            )
            : base(hostData)
        {
            if (hostData != null)
            {
                //
                // NOTE: This must be the name of the class that is deriving
                //       from us.  It is used to construct the file name for
                //       the host profile file.
                //
                typeName = hostData.TypeName;
            }

            ///////////////////////////////////////////////////////////////////

            //
            // BUGFIX: In case other host settings are loaded which affect
            //         the rest of the setup process, do this first.
            //
            if (!NoProfile)
            {
                Interpreter interpreter = InternalSafeGetInterpreter(false);
                CultureInfo cultureInfo = null;

                if (interpreter != null)
                    cultureInfo = interpreter.InternalCultureInfo;

                /* IGNORED */
                SettingsOps.LoadForHost(interpreter, this, GetType(),
                    HostProfileFileEncoding, HostProfileFileName,
                    cultureInfo, HostPropertyBindingFlags, false);
            }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////////////////////////////

        #region Host Flags Support
        private void PrivateResetHostFlagsOnly()
        {
            hostFlags = HostFlags.Invalid;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        private bool PrivateResetHostFlags()
        {
            PrivateResetHostFlagsOnly();
            return base.ResetHostFlags();
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        protected override HostFlags MaybeInitializeHostFlags()
        {
            if (hostFlags == HostFlags.Invalid)
            {
                //
                // NOTE: We support the "Profile" subsystem.
                //
                hostFlags = HostFlags.Profile |
                    base.MaybeInitializeHostFlags();
            }

            return hostFlags;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        protected override void SetReadException(
            bool exception
            )
        {
            base.SetReadException(exception);
            PrivateResetHostFlagsOnly();
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        protected override void SetWriteException(
            bool exception
            )
        {
            base.SetWriteException(exception);
            PrivateResetHostFlagsOnly();
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////////////////////////////

        #region Profile Support
        protected internal virtual Encoding HostProfileFileEncoding
        {
            get { return null; }
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        protected internal virtual string HostProfileFileName
        {
            get
            {
                try
                {
                    return SettingsOps.GetHostFileName(
                        UnsafeGetInterpreter(), Profile,
                        TypeName, NoColor);
                }
                catch (Exception e)
                {
                    TraceOps.DebugTrace(
                        e, typeof(Profile).Name,
                        TracePriority.HostError);

                    return null;
                }
            }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////////////////////////////

        #region Protected Properties
        private string typeName;
        protected internal virtual string TypeName
        {
            get { return typeName; }
            internal set { typeName = value; }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////////////////////////////

        #region IInteractiveHost Members
        private HostFlags hostFlags = HostFlags.Invalid;
        public override HostFlags GetHostFlags()
        {
            CheckDisposed();

            return MaybeInitializeHostFlags();
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////////////////////////////

        #region IHost Members
        public override bool ResetHostFlags()
        {
            CheckDisposed();

            return PrivateResetHostFlags();
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

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

        ///////////////////////////////////////////////////////////////////////////////////////////////

        #region IDisposable "Pattern" Members
        private bool disposed;
        private void CheckDisposed() /* throw */
        {
#if THROW_ON_DISPOSED
            if (disposed && _Engine.IsThrowOnDisposed(
                    InternalSafeGetInterpreter(false), null))
            {
                throw new InterpreterDisposedException(typeof(Profile));
            }
#endif
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

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
