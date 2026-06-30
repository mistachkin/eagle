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
    /// <summary>
    /// This class is an abstract host base class that adds host profile support
    /// on top of the file host.  It loads and persists per-host settings from a
    /// profile file (named after the deriving class) and advertises the profile
    /// host flag.  It serves as a base for the concrete host classes in the
    /// host class hierarchy.
    /// </summary>
    [ObjectId("77ba621d-3229-42a4-874f-315976ebf742")]
    public abstract class Profile : File, IDisposable
    {
        #region Protected Constructors
        /// <summary>
        /// Constructs an instance of this host class.  Unless the host is
        /// configured to skip its profile, this also loads the host's saved
        /// settings from its profile file.
        /// </summary>
        /// <param name="hostData">
        /// The host data used to initialize this host, if any.  This parameter
        /// may be null.
        /// </param>
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
        /// <summary>
        /// This method invalidates the cached host flags so that they will be
        /// recomputed the next time they are requested.
        /// </summary>
        private void PrivateResetHostFlagsOnly()
        {
            hostFlags = HostFlags.Invalid;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

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

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method computes and caches the host flags for this host, if
        /// they have not already been computed.  It adds the profile flag to
        /// those provided by the base host.
        /// </summary>
        /// <returns>
        /// The host flags for this host.
        /// </returns>
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

        ///////////////////////////////////////////////////////////////////////////////////////////////

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

        ///////////////////////////////////////////////////////////////////////////////////////////////

        #region Profile Support
        /// <summary>
        /// Gets the text encoding used when reading and writing this host's
        /// profile file, or null to use the default encoding.
        /// </summary>
        protected internal virtual Encoding HostProfileFileEncoding
        {
            get { return null; }
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Gets the fully qualified name of this host's profile file, or null if
        /// it cannot be determined.
        /// </summary>
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
        /// <summary>
        /// Stores the name of the type that derives from this host, used when
        /// constructing the host profile file name.
        /// </summary>
        private string typeName;
        /// <summary>
        /// Gets or sets the name of the type that derives from this host.  This
        /// name is used when constructing the host profile file name.
        /// </summary>
        protected internal virtual string TypeName
        {
            get { return typeName; }
            internal set { typeName = value; }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////////////////////////////

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
        #endregion

        ///////////////////////////////////////////////////////////////////////////////////////////////

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

        ///////////////////////////////////////////////////////////////////////////////////////////////

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

        ///////////////////////////////////////////////////////////////////////////////////////////////

        #region IMaybeDisposed Members
        /// <summary>
        /// Gets a value indicating whether this host has been disposed.
        /// </summary>
        public override bool Disposed
        {
            get { return disposed; }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////////////////////////////

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
                throw new InterpreterDisposedException(typeof(Profile));
            }
#endif
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

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
