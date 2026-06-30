/*
 * Console.cs --
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
using System.Collections.Generic;

#if DRAWING
using System.Drawing;
#endif

using System.IO;
using System.Text;
using System.Threading;
using Eagle._Attributes;
using Eagle._Components.Public;
using Eagle._Components.Private;
using Eagle._Constants;
using Eagle._Containers.Public;
using Eagle._Interfaces.Public;

using _Engine = Eagle._Components.Public.Engine;
using SharedStringOps = Eagle._Components.Shared.StringOps;

namespace Eagle._Hosts
{
    /// <summary>
    /// This class implements an interpreter host that reads from and writes to
    /// the system console (i.e. via the <see cref="System.Console" /> class
    /// and, where available, the native console subsystem).  It builds upon the
    /// <see cref="Core" /> base host to provide console-specific support for
    /// colors, window and buffer sizing, the window title, the window icon, line
    /// editing, and Ctrl-C (script cancellation) keypress handling.  Most of its
    /// behavior can be overridden by derived host classes.
    /// </summary>
    [ObjectId("e15283cf-00b4-44f2-a16e-48cf061e53d1")]
    public class Console : Core, ISynchronize, IDisposable
    {
        #region Private Static Data
        /// <summary>
        /// The object used to synchronize access to the static state shared by
        /// all instances of this class.
        /// </summary>
        private static readonly object staticSyncRoot = new object();

        ///////////////////////////////////////////////////////////////////////////////////////////////

#if NATIVE && WINDOWS
        /// <summary>
        /// The number of outstanding requests to treat the native console as
        /// closed.  When greater than zero, the console is considered closed and
        /// will not be used.
        /// </summary>
        private static int closeCount = 0;
#endif

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// The number of console host instances that have performed their
        /// one-time console setup.  This is used to coordinate setup and
        /// teardown of the shared console customizations across instances.
        /// </summary>
        private static int referenceCount = 0;

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// The number of outstanding requests to throw an exception when the
        /// system console is required but not available.  When greater than
        /// zero, the various SystemConsole*MustBeOpen methods will throw.
        /// </summary>
        private static int mustBeOpenCount = 0;

        ///////////////////////////////////////////////////////////////////////////////////////////////

#if NATIVE
        //
        // HACK: Setting this value to non-zero will disable script cancellation
        //       from being triggered via the Cancel (PrivateForceCancel) method.
        //
        /// <summary>
        /// When non-zero, script cancellation will not be triggered via the
        /// Cancel (PrivateForceCancel) method.
        /// </summary>
        private static bool defaultForceNoCancel = false;
#endif

        //
        // HACK: Setting this value to non-zero will force this class to treat
        //       non-default application domains [more-or-less] like the default
        //       application domain one (e.g. the Ctrl-C keypress handler will
        //       be added/removed).
        //
        /// <summary>
        /// When non-zero, this class treats non-default application domains like
        /// the default application domain (e.g. the Ctrl-C keypress handler will
        /// be added or removed).
        /// </summary>
        private static bool defaultForceAppDomain = false;

        //
        // HACK: Setting this value to non-zero will force the console cancel
        //       event handler to be changed even when there may be an event
        //       handler pending.
        //
        /// <summary>
        /// When non-zero, the console cancel event handler is changed even when
        /// there may be an event handler pending.
        /// </summary>
        private static bool defaultForcePending = true;

        ///////////////////////////////////////////////////////////////////////////////////////////////

#if NATIVE && WINDOWS && DRAWING && !NET_STANDARD_20
        /// <summary>
        /// The custom console window icon currently installed by this class, if
        /// any.
        /// </summary>
        private static Icon icon;

        /// <summary>
        /// The original large console window icon, saved so that it can be
        /// restored when the custom icon is uninstalled.
        /// </summary>
        private static IntPtr oldBigIcon;

        /// <summary>
        /// The original small console window icon, saved so that it can be
        /// restored when the custom icon is uninstalled.
        /// </summary>
        private static IntPtr oldSmallIcon;
#endif

        ///////////////////////////////////////////////////////////////////////////////////////////////

        //
        // HACK: This is purposely not read-only.
        //
        /// <summary>
        /// The shared event handler used to respond to console Ctrl-C (cancel)
        /// keypress events.
        /// </summary>
        private static ConsoleCancelEventHandler consoleCancelEventHandler = null;
        #endregion

        ///////////////////////////////////////////////////////////////////////////////////////////////

        #region Private Data
        /// <summary>
        /// The object used to synchronize access to the per-instance state of
        /// this console host.
        /// </summary>
        private readonly object syncRoot = new object();

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// A guard used to ensure that the certificate subject is computed only
        /// once for this console host instance.
        /// </summary>
        private int certificateCount = 0;

        /// <summary>
        /// The cached certificate subject string used when building the console
        /// window title, if any.
        /// </summary>
        private string certificateSubject = null; /* CACHED */

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// The original console window title, saved so that it can be restored
        /// later.  This is null when no title has been saved.
        /// </summary>
        private string savedTitle;

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// The original console foreground color, saved so that it can be
        /// restored later.
        /// </summary>
        private ConsoleColor savedForegroundColor = _ConsoleColor.None;

        /// <summary>
        /// The original console background color, saved so that it can be
        /// restored later.
        /// </summary>
        private ConsoleColor savedBackgroundColor = _ConsoleColor.None;

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// The original console window width, saved so that it can be restored
        /// later.
        /// </summary>
        private int savedWindowWidth = _Size.Invalid;

        /// <summary>
        /// The original console window height, saved so that it can be restored
        /// later.
        /// </summary>
        private int savedWindowHeight = _Size.Invalid;

        /// <summary>
        /// The original console buffer width, saved so that it can be restored
        /// later.
        /// </summary>
        private int savedBufferWidth = _Size.Invalid;

        /// <summary>
        /// The original console buffer height, saved so that it can be restored
        /// later.
        /// </summary>
        private int savedBufferHeight = _Size.Invalid;
        #endregion

        ///////////////////////////////////////////////////////////////////////////////////////////////

        #region Private Constants
        #region Native Console CancelKeyPress Handling
#if NATIVE
        /// <summary>
        /// The maximum amount of time, in milliseconds, to wait when forcing
        /// script cancellation via a native console signal.
        /// </summary>
        private static int forceCancelTimeout = 5000;
#endif
        #endregion

        ///////////////////////////////////////////////////////////////////////////////////////////////

        #region Output Size Constants
        //
        // NOTE: Apparently, the underlying WriteConsoleW call used by the
        //       System.Console class has an internal limit of *somewhere*
        //       between 26000 and 32768 characters (i.e. about 65536 bytes,
        //       give or take?).  This limit is not exact and cannot be
        //       readily predicted in advance.  Several sources on the web
        //       seem to indicate that <=26000 characters should be a safe
        //       write size.  Please refer to the following links for more
        //       information:
        //
        //       https://msdn.microsoft.com/en-us/library/ms687401.aspx
        //
        //       https://mail-archives.apache.org/mod_mbox/logging-log4net
        //           -dev/200501.mbox/%3CD44F10C7974F5D4BAFAC9D37A127D5600
        //           1B7B05F@raven.tdsway.com%3E
        //
        //       https://bit.ly/1Akk2YI (shortened version of above)
        //
        //       https://www.mail-archive.com/log4net-dev@logging.apache.
        //           org/msg00645.html
        //
        //       https://bit.ly/2d3EniG (shortened version of above)
        //
        /// <summary>
        /// The maximum number of characters that can be safely written to the
        /// console in a single operation, working around an internal size limit
        /// of the underlying native write console function.
        /// </summary>
        internal static readonly int SafeWriteSize = 25000; /* NOTE: <=26000 */
        #endregion

        ///////////////////////////////////////////////////////////////////////////////////////////////

        #region Buffer Size Constants
        /// <summary>
        /// The number of columns to subtract from a requested width when
        /// computing the maximum console buffer width.
        /// </summary>
        private static readonly int MaximumBufferWidthMargin = 8;

        /// <summary>
        /// The maximum "reasonable" console buffer height, in rows, used when
        /// setting up the maximum console size (e.g. for the scrollback buffer).
        /// </summary>
        private static readonly int MaximumBufferHeight = 9999;
        #endregion

        ///////////////////////////////////////////////////////////////////////////////////////////////

        #region Window Width Constants
        //
        // HACK: These are considered to be "best guess" values.
        //       Please adjust them to suit your taste as necessary.
        //
        /// <summary>
        /// The window width, in columns, at or above which the console is
        /// considered to have the minimum size.
        /// </summary>
        private static readonly int MinimumWindowWidth = 40;

        /// <summary>
        /// The window width, in columns, at or above which the console is
        /// considered to have the compact size.
        /// </summary>
        private static readonly int CompactWindowWidth = 80;

        /// <summary>
        /// The window width, in columns, at or above which the console is
        /// considered to have the full size.
        /// </summary>
        private static readonly int FullWindowWidth = 120;

        /// <summary>
        /// The window width, in columns, at or above which the console is
        /// considered to have the super-full size.
        /// </summary>
        private static readonly int SuperFullWindowWidth = 160;

        /// <summary>
        /// The window width, in columns, at or above which the console is
        /// considered to have the jumbo size.
        /// </summary>
        private static readonly int JumboWindowWidth = 200;

        /// <summary>
        /// The window width, in columns, at or above which the console is
        /// considered to have the super-jumbo size.
        /// </summary>
        private static readonly int SuperJumboWindowWidth = 230;
        #endregion

        ///////////////////////////////////////////////////////////////////////////////////////////////

        #region Window Height Constants
        /// <summary>
        /// The window height, in rows, at or above which the console is
        /// considered to have the minimum size.
        /// </summary>
        private static readonly int MinimumWindowHeight = 10;

        /// <summary>
        /// The window height, in rows, at or above which the console is
        /// considered to have the compact size.
        /// </summary>
        private static readonly int CompactWindowHeight = 25;

        /// <summary>
        /// The window height, in rows, at or above which the console is
        /// considered to have the full size.
        /// </summary>
        private static readonly int FullWindowHeight = 40;

        /// <summary>
        /// The window height, in rows, at or above which the console is
        /// considered to have the super-full size.
        /// </summary>
        private static readonly int SuperFullWindowHeight = 60;

        /// <summary>
        /// The window height, in rows, at or above which the console is
        /// considered to have the jumbo size.
        /// </summary>
        private static readonly int JumboWindowHeight = 75;

        /// <summary>
        /// The window height, in rows, at or above which the console is
        /// considered to have the super-jumbo size.
        /// </summary>
        private static readonly int SuperJumboWindowHeight = 90;
        #endregion

        ///////////////////////////////////////////////////////////////////////////////////////////////

        #region Margin Constants
        /// <summary>
        /// The number of columns to subtract from a requested width when
        /// computing the maximum console window width.
        /// </summary>
        private static readonly int MaximumWindowWidthMargin = MaximumBufferWidthMargin;

        /// <summary>
        /// The number of rows to subtract from a requested height when computing
        /// the maximum console window height.
        /// </summary>
        private static readonly int MaximumWindowHeightMargin = 6;
        #endregion

        ///////////////////////////////////////////////////////////////////////////////////////////////

        #region Window Title Constants
        /// <summary>
        /// The prefix prepended to the console window title when the current
        /// process is running with administrative privileges.
        /// </summary>
        private static readonly string AdministratorTitlePrefix = "Administrator:";

        /// <summary>
        /// The prefix prepended to the certificate subject portion of the
        /// console window title.
        /// </summary>
        private static readonly string CertificateSubjectPrefix = "- ";

        /// <summary>
        /// The placeholder shown in the console window title while the
        /// certificate subject is being checked.
        /// </summary>
        private static readonly string CertificateSubjectPending = "checking certificate...";
        #endregion

        ///////////////////////////////////////////////////////////////////////////////////////////////

        #region ConsoleKeyInfo Formatting Constants
        /// <summary>
        /// The format string used to echo a console key modifier and its
        /// separator when echoing a read key.
        /// </summary>
        private static readonly string ModifierEchoFormat = "{0}{1}";

        /// <summary>
        /// The character used to separate console key modifiers from one another
        /// when echoing a read key.
        /// </summary>
        private static readonly char ModifierEchoSeparator = Characters.MinusSign;
        #endregion

        ///////////////////////////////////////////////////////////////////////////////////////////////

        #region ANSI Escape Sequence Constants
#if UNIX
        //
        // NOTE: This is purposely not read-only.
        //
        /// <summary>
        /// The ANSI escape sequence format string used to move the cursor back
        /// (to the left) by a given number of columns.
        /// </summary>
        private static string AnsiCursorBackFormat = "\x1B[{0}D";
#endif
        #endregion
        #endregion

        ///////////////////////////////////////////////////////////////////////////////////////////////

        #region Public Constructors
        /// <summary>
        /// Constructs a new console host instance, saving the original console
        /// size and colors and performing the initial console setup.
        /// </summary>
        /// <param name="hostData">
        /// The host data used to initialize this console host.  This parameter
        /// may be null.
        /// </param>
        public Console(
            IHostData hostData
            )
            : base(hostData)
        {
            //
            // NOTE: Enable throwing exceptions when the various
            //       SystemConsole*MustBeOpen() methods are called.
            //
            EnableThrowOnMustBeOpen();

            //
            // NOTE: Save the original buffer and window sizes.
            //
            /* IGNORED */
            SaveSize();

            //
            // NOTE: Save the original colors.
            //
            /* IGNORED */
            SaveColors();

            /* IGNORED */
            Setup(this, true, false);
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////////////////////////////

        #region Host Flags Support
        /// <summary>
        /// Resets only the cached host flags for this console host so that they
        /// will be recomputed on the next request.
        /// </summary>
        private void PrivateResetHostFlagsOnly()
        {
            hostFlags = HostFlags.Invalid;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Resets the cached host flags for this console host and the base host.
        /// </summary>
        /// <returns>
        /// True if the host flags were reset successfully; otherwise, false.
        /// </returns>
        private bool PrivateResetHostFlags()
        {
            PrivateResetHostFlagsOnly();
            return base.ResetHostFlags();
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Computes the host flags for this console host, if they have not
        /// already been computed, based on the supported capabilities and the
        /// current console size and platform.
        /// </summary>
        /// <returns>
        /// The host flags describing the capabilities of this console host.
        /// </returns>
        protected override HostFlags MaybeInitializeHostFlags()
        {
            if (hostFlags == HostFlags.Invalid)
            {
                //
                // NOTE: We support text output, colors, positioning,
                //       sizing, and resizing.
                //
                hostFlags = HostFlags.Resizable | HostFlags.Color |
                            HostFlags.ReversedColor | HostFlags.Text |
                            HostFlags.Sizing | HostFlags.Positioning |
                            HostFlags.QueryState | HostFlags.NoColorNewLine |
                            base.MaybeInitializeHostFlags();

                if (ShouldTreatAsMono() || IsWindowsTerminal())
                    hostFlags |= HostFlags.NormalizeToNewLine;

                if (ShouldTreatAsMono() &&
                    !PlatformOps.IsWindowsOperatingSystem())
                {
                    //
                    // HACK: Apparently, there are various levels of
                    //       broken behavior for console colors when
                    //       using Mono in an X11 terminal.
                    //
                    if (IsX11Terminal())
                    {
                        hostFlags &= ~HostFlags.Positioning;
                        hostFlags |= HostFlags.NoSetForegroundColor;
                        hostFlags |= HostFlags.NoSetBackgroundColor;
                    }
                }

                if (ShouldTreatAsDotNetCore() &&
                    !PlatformOps.IsWindowsOperatingSystem())
                {
                    hostFlags |= HostFlags.RestoreColorAfterWrite;
                    hostFlags |= HostFlags.ResetColorForRestore;
                }

                if ((WindowWidth >= SuperJumboWindowWidth) &&
                    (WindowHeight >= SuperJumboWindowHeight))
                {
                    hostFlags |= HostFlags.SuperJumboSize;
                }
                else if ((WindowWidth >= JumboWindowWidth) &&
                    (WindowHeight >= JumboWindowHeight))
                {
                    hostFlags |= HostFlags.JumboSize;
                }
                else if ((WindowWidth >= SuperFullWindowWidth) &&
                    (WindowHeight >= SuperFullWindowHeight))
                {
                    hostFlags |= HostFlags.SuperFullSize;
                }
                else if ((WindowWidth >= FullWindowWidth) &&
                    (WindowHeight >= FullWindowHeight))
                {
                    hostFlags |= HostFlags.FullSize;
                }
                else if ((WindowWidth >= CompactWindowWidth) &&
                    (WindowHeight >= CompactWindowHeight))
                {
                    hostFlags |= HostFlags.CompactSize;
                }
                else if ((WindowWidth >= MinimumWindowWidth) &&
                    (WindowHeight >= MinimumWindowHeight))
                {
                    hostFlags |= HostFlags.MinimumSize;
                }
                else if (!PlatformOps.IsWindowsOperatingSystem())
                {
                    //
                    // NOTE: Mono does not reliably support console
                    //       window width and height on Unix.  .NET
                    //       Core does; however, if we reach this
                    //       point, the query has already failed, so
                    //       fall back to a reasonable default.
                    //
                    hostFlags |= HostFlags.CompactSize;
                }
                else
                {
                    //
                    // NOTE: We should not get here.
                    //
                    hostFlags |= HostFlags.ZeroSize;
                }
            }

            //
            // WARNING: Do not use the InTestMode method here, it calls
            //          this method.
            //
            if (FlagOps.HasFlags(hostFlags, HostFlags.Test, true))
                hostFlags |= HostFlags.CustomInfo;
            else
                hostFlags &= ~HostFlags.CustomInfo;

            return hostFlags;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Records whether a read exception has occurred and resets the cached
        /// host flags accordingly.
        /// </summary>
        /// <param name="exception">
        /// Non-zero if a read exception has occurred; otherwise, zero.
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
        /// Records whether a write exception has occurred and resets the cached
        /// host flags accordingly.
        /// </summary>
        /// <param name="exception">
        /// Non-zero if a write exception has occurred; otherwise, zero.
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

        #region Host Read/Write Levels Support
        /// <summary>
        /// Increments the shared and base read levels upon entering a console
        /// read operation.
        /// </summary>
        protected override void EnterReadLevel()
        {
            // CheckDisposed();

            Interlocked.Increment(ref sharedReadLevels);
            base.EnterReadLevel();
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Decrements the shared and base read levels upon exiting a console
        /// read operation.
        /// </summary>
        protected override void ExitReadLevel()
        {
            // CheckDisposed();

            base.ExitReadLevel();
            Interlocked.Decrement(ref sharedReadLevels);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Increments the shared and base write levels upon entering a console
        /// write operation.
        /// </summary>
        protected override void EnterWriteLevel()
        {
            // CheckDisposed();

            Interlocked.Increment(ref sharedWriteLevels);
            base.EnterWriteLevel();
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Decrements the shared and base write levels upon exiting a console
        /// write operation.
        /// </summary>
        protected override void ExitWriteLevel()
        {
            // CheckDisposed();

            base.ExitWriteLevel();
            Interlocked.Decrement(ref sharedWriteLevels);
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////////////////////////////

        #region Console Handling
        #region Native Console Stream Handling
        /// <summary>
        /// Determines whether the specified system console channel has been
        /// redirected (e.g. to or from a file or pipe).
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter to use when reporting any error encountered while
        /// querying the channel.  This parameter may be null.
        /// </param>
        /// <param name="channelType">
        /// The console channel to query.
        /// </param>
        /// <param name="default">
        /// The value to return when the redirection state cannot be determined.
        /// </param>
        /// <returns>
        /// True if the specified channel has been redirected; otherwise, false.
        /// </returns>
        private static bool SystemConsoleIsRedirected(
            Interpreter interpreter,
            ChannelType channelType,
            bool @default
            )
        {
#if NATIVE && WINDOWS
            if (NativeConsole.IsSupported())
            {
                IntPtr handle;
                Result error = null;

                handle = NativeConsole.GetHandle(channelType, ref error);

                if (NativeOps.IsValidHandle(handle))
                {
                    bool redirected = false;

                    if (NativeConsole.IsHandleRedirected(handle,
                            ref redirected, ref error) == ReturnCode.Ok)
                    {
                        return redirected;
                    }
                }

                //
                // NOTE: Either we failed to get the handle or we could
                //       not determine if it has been redirected.  This
                //       condition should be relatively rare, complain.
                //       Skip complaining if/when we are already being
                //       invoked from the complaint subsystem (i.e. due
                //       to the complaint subsystem attempting to write
                //       to the interpreter host).
                //
                if (NativeConsole.IsOpen())
                {
                    if (!DebugOps.IsComplainPending())
                    {
                        //
                        // NOTE: Always complain here instead of using
                        //       the MaybeComplain method because this
                        //       method does not have a way to indicate
                        //       failure to the caller.
                        //
                        DebugOps.Complain(
                            interpreter, ReturnCode.Error, error);
                    }
                }

                return false;
            }
            else
            {
                return @default;
            }
#else
            return @default;
#endif
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Determines whether the system console input channel has been
        /// redirected.
        /// </summary>
        /// <returns>
        /// True if the console input channel has been redirected; otherwise,
        /// false.
        /// </returns>
        protected virtual bool SystemConsoleInputIsRedirected()
        {
            return SystemConsoleIsRedirected(
                InternalSafeGetInterpreter(false),
                ChannelType.Input, false);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Determines whether the system console output or error channel has
        /// been redirected.
        /// </summary>
        /// <returns>
        /// True if the console output or error channel has been redirected;
        /// otherwise, false.
        /// </returns>
        protected virtual bool SystemConsoleOutputIsRedirected()
        {
            Interpreter interpreter = InternalSafeGetInterpreter(false);

            if (SystemConsoleIsRedirected(
                    interpreter, ChannelType.Output, false) ||
                SystemConsoleIsRedirected(
                    interpreter, ChannelType.Error, false))
            {
                return true;
            }

            return false;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Determines whether the system console error channel has been
        /// redirected.
        /// </summary>
        /// <returns>
        /// True if the console error channel has been redirected; otherwise,
        /// false.
        /// </returns>
        protected virtual bool SystemConsoleErrorIsRedirected()
        {
            return SystemConsoleIsRedirected(
                InternalSafeGetInterpreter(false),
                ChannelType.Error, false);
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////////////////////////////

        #region System.Console Open/Close Handling
        /// <summary>
        /// Enables throwing exceptions from the various SystemConsole*MustBeOpen
        /// methods when the system console is required but not available, if not
        /// already enabled.
        /// </summary>
        private static void EnableThrowOnMustBeOpen()
        {
            //
            // NOTE: If necessary, enable throwing exceptions from
            //       within the SystemConsole*MustBeOpen() methods.
            //
            if (!ThrowOnMustBeOpen) ThrowOnMustBeOpen = true;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Temporarily changes whether the various SystemConsole*MustBeOpen
        /// methods throw an exception when the console is unavailable, saving the
        /// previous setting so that it can be restored later.
        /// </summary>
        /// <param name="throwOnMustBeOpen">
        /// Non-zero to enable throwing; otherwise, zero.
        /// </param>
        /// <param name="savedThrowOnMustBeOpen">
        /// Upon return, receives the previous setting so that it can be passed
        /// to <see cref="EndThrowOnMustBeOpen" />.
        /// </param>
        internal static void BeginThrowOnMustBeOpen(
            bool throwOnMustBeOpen,          /* in */
            out bool? savedThrowOnMustBeOpen /* out */
            )
        {
            savedThrowOnMustBeOpen = ThrowOnMustBeOpen;
            ThrowOnMustBeOpen = throwOnMustBeOpen;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Restores the previously saved setting controlling whether the various
        /// SystemConsole*MustBeOpen methods throw an exception when the console
        /// is unavailable.
        /// </summary>
        /// <param name="savedThrowOnMustBeOpen">
        /// The setting saved by <see cref="BeginThrowOnMustBeOpen" />; it is set
        /// to null upon return.
        /// </param>
        internal static void EndThrowOnMustBeOpen(
            ref bool? savedThrowOnMustBeOpen /* in, out */
            )
        {
            if (savedThrowOnMustBeOpen != null)
            {
                ThrowOnMustBeOpen = (bool)savedThrowOnMustBeOpen;
                savedThrowOnMustBeOpen = null;
            }
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Gets or sets a value indicating whether the various
        /// SystemConsole*MustBeOpen methods throw an exception when the console
        /// is required but not available.  Setting this property to true
        /// increments, and to false decrements, the underlying request count.
        /// </summary>
        internal static bool ThrowOnMustBeOpen
        {
            get
            {
                return Interlocked.CompareExchange(
                    ref mustBeOpenCount, 0, 0) > 0;
            }
            set
            {
                if (value)
                    Interlocked.Increment(ref mustBeOpenCount);
                else
                    Interlocked.Decrement(ref mustBeOpenCount);
            }
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Determines whether the system console is open (i.e. available for
        /// use), optionally requiring that the console window itself is open.
        /// </summary>
        /// <param name="window">
        /// Non-zero to also require that the console window is open; otherwise,
        /// zero.
        /// </param>
        /// <returns>
        /// True if the system console is open; otherwise, false.
        /// </returns>
        private static bool SystemConsoleIsOpen(
            bool window
            )
        {
#if NATIVE && WINDOWS
            //
            // NOTE: Are there outstanding calls to the NativeConsole.Close
            //       method (i.e. those that have not been matched by calls
            //       to the NativeConsole.Open method)?
            //
            if (WasConsoleClosed())
                return false;

            if (window &&
                NativeConsole.IsSupported() &&
                !NativeConsole.IsOpen())
            {
                return false;
            }

            return SystemConsoleInputIsOpen(); /* COMPAT: Eagle beta. */
#else
            return true;
#endif
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Determines whether the system console input channel is open by
        /// probing an input-related console property.
        /// </summary>
        /// <returns>
        /// True if the console input channel is open; otherwise, false.
        /// </returns>
        private static bool SystemConsoleInputIsOpen()
        {
            try
            {
#if MONO_BUILD
#pragma warning disable 219
#endif
                /* IGNORED */
                bool value = System.Console.TreatControlCAsInput; /* EXEMPT */
#if MONO_BUILD
#pragma warning restore 219
#endif

                return true;
            }
            catch (Exception e)
            {
                //
                // NOTE: Something failed, just return false.
                //
                TraceOps.DebugTrace(
                    e, typeof(Console).Name,
                    TracePriority.ConsoleError);

                return false;
            }
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Determines whether the system console output channel is open by
        /// probing an output-related console property.
        /// </summary>
        /// <returns>
        /// True if the console output channel is open; otherwise, false.
        /// </returns>
        private static bool SystemConsoleOutputIsOpen()
        {
            try
            {
#if MONO_BUILD
#pragma warning disable 219
#endif
                /* IGNORED */
                bool value = System.Console.CursorVisible; /* EXEMPT */
#if MONO_BUILD
#pragma warning restore 219
#endif

                return true;
            }
            catch (Exception e)
            {
                //
                // NOTE: Something failed, just return false.
                //
                TraceOps.DebugTrace(
                    e, typeof(Console).Name,
                    TracePriority.ConsoleError);

                return false;
            }
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Determines whether the system console output channel is open,
        /// returning a fallback value on platforms where the underlying check is
        /// known to be unreliable (e.g. .NET Core on Linux or macOS).
        /// </summary>
        /// <param name="default">
        /// The value to return when the output-open check cannot be performed
        /// reliably.
        /// </param>
        /// <returns>
        /// True if the console output channel is open; otherwise, false.
        /// </returns>
        private static bool MaybeSystemConsoleOutputIsOpen(
            bool @default
            )
        {
            //
            // HACK: The System.Console.CursorVisible property being
            //       used by SystemConsoleOutputIsOpen does not work
            //       on .NET Core 2.0 when running on Linux / macOS.
            //
            // BUGBUG: Cannot use the ShouldTreatAsDotNetCore method
            //         here because it is not static.
            //
            if (PlatformOps.IsWindowsOperatingSystem() ||
                !CommonOps.Runtime.IsDotNetCore())
            {
                return SystemConsoleOutputIsOpen();
            }

            return @default;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Throws an exception if the system console is required but not
        /// available, optionally requiring that the console window is open.  Does
        /// nothing when throwing is disabled.
        /// </summary>
        /// <param name="window">
        /// Non-zero to also require that the console window is open; otherwise,
        /// zero.
        /// </param>
        protected static void SystemConsoleMustBeOpen(
            bool window
            )
        {
            if (!ThrowOnMustBeOpen)
                return;

            if (!SystemConsoleIsOpen(window))
            {
                throw new ScriptException(String.Format(
                    "system console {0}is not available",
                    window ? "window " : String.Empty));
            }
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Throws an exception if the system console input channel is required
        /// but not available.  Does nothing when throwing is disabled or when
        /// input is redirected.
        /// </summary>
        /// <param name="interactiveHost">
        /// The interactive host whose input redirection state is consulted.
        /// This parameter may be null.
        /// </param>
        protected static void SystemConsoleInputMustBeOpen(
            IInteractiveHost interactiveHost
            )
        {
            if (!ThrowOnMustBeOpen)
                return;

            if (((interactiveHost == null) || !interactiveHost.IsInputRedirected()) &&
                !SystemConsoleInputIsOpen() &&
                !SystemConsoleIsRedirected(null, ChannelType.Input, true))
            {
                throw new ScriptException(
                    "system console input channel is not available");
            }
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Throws an exception if the system console output channel is required
        /// but not available.  Does nothing when throwing is disabled or when
        /// output is redirected.
        /// </summary>
        /// <param name="streamHost">
        /// The stream host whose output redirection state is consulted.  This
        /// parameter may be null.
        /// </param>
        protected static void SystemConsoleOutputMustBeOpen(
            IStreamHost streamHost
            )
        {
            if (!ThrowOnMustBeOpen)
                return;

            if (((streamHost == null) || !streamHost.IsOutputRedirected()) &&
                !MaybeSystemConsoleOutputIsOpen(true) &&
                !SystemConsoleIsRedirected(null, ChannelType.Output, true))
            {
                throw new ScriptException(
                    "system console output channel is not available");
            }
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Throws an exception if the system console error channel is required
        /// but not available.  Does nothing when throwing is disabled or when
        /// the error channel is redirected.
        /// </summary>
        /// <param name="streamHost">
        /// The stream host whose error redirection state is consulted.  This
        /// parameter may be null.
        /// </param>
        protected static void SystemConsoleErrorMustBeOpen(
            IStreamHost streamHost
            )
        {
            if (!ThrowOnMustBeOpen)
                return;

            if (((streamHost == null) || !streamHost.IsErrorRedirected()) &&
                !MaybeSystemConsoleOutputIsOpen(true) &&
                !SystemConsoleIsRedirected(null, ChannelType.Error, true))
            {
                throw new ScriptException(
                    "system console error channel is not available");
            }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////////////////////////////

        #region System.Console ReadLine Handling
#if UNIX
        /// <summary>
        /// Moves the console cursor back to the start of the current line by
        /// emitting the appropriate ANSI escape sequence for the length of the
        /// supplied prompt.
        /// </summary>
        /// <param name="textWriter">
        /// The text writer to which the cursor-movement escape sequence is
        /// written.  This parameter may be null.
        /// </param>
        /// <param name="prompt">
        /// The prompt whose length determines how far the cursor is moved.  Upon
        /// failure, it may be set to null.  This parameter may be null.
        /// </param>
        /// <returns>
        /// True if the cursor was moved successfully; otherwise, false.
        /// </returns>
        protected virtual bool CursorBackToStartOfLine(
            TextWriter textWriter, /* in */
            ref string prompt      /* in, out */
            )
        {
            if ((textWriter != null) && (prompt != null))
            {
                try
                {
                    textWriter.Write(String.Format(
                        AnsiCursorBackFormat, prompt.Length));

                    textWriter.Flush();
                    return true;
                }
                catch (IOException)
                {
                    SetWriteException(true);

                    return false;
                }
                catch (Exception e)
                {
                    prompt = null;

                    TraceOps.DebugTrace(
                        e, typeof(Console).Name,
                        TracePriority.ConsoleError);
                }
            }

            return false;
        }
#endif

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Reads a line of input from the system console, using native line
        /// editing and history navigation when available on non-Windows
        /// platforms.
        /// </summary>
        /// <returns>
        /// The line of text read from the console, or null if the end of input
        /// has been reached.
        /// </returns>
        protected virtual string SystemConsoleReadLine()
        {
#if NATIVE && UNIX
            //
            // NOTE: On non-Windows platforms, attempt to
            //       use native line editing and history
            //       navigation (arrow keys, etc.).
            //
            if (!PlatformOps.IsWindowsOperatingSystem() &&
                LineEditor.IsAvailable())
            {
                string prompt = MaybeGetPrompt();

                if (!String.IsNullOrEmpty(prompt) &&
                    !LineEditor.HasAlreadyPrompted())
                {
                    /* IGNORED */
                    CursorBackToStartOfLine(
                        System.Console.Out, ref prompt);
                }

                return LineEditor.ReadLine(prompt);
            }
#endif

            return System.Console.ReadLine();
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////////////////////////////

        #region System.Console Size Handling
        /// <summary>
        /// Gets the largest possible console window size using the managed
        /// console properties, as a fallback when the native size query is not
        /// available.
        /// </summary>
        /// <param name="width">
        /// Upon success, receives the largest window width, in columns.
        /// </param>
        /// <param name="height">
        /// Upon success, receives the largest window height, in rows.
        /// </param>
        /// <returns>
        /// True if the largest window size was obtained successfully; otherwise,
        /// false.
        /// </returns>
        protected virtual bool FallbackGetLargestWindowSize(
            ref int width,
            ref int height
            )
        {
            try
            {
                SystemConsoleMustBeOpen(true); /* throw */

                width = System.Console.LargestWindowWidth;
                height = System.Console.LargestWindowHeight;

                return true;
            }
            catch (ScriptException)
            {
                //
                // NOTE: The console is not open, just return false.
                //
                return false;
            }
            catch (Exception e)
            {
                //
                // NOTE: Something failed, just return false.
                //
                TraceOps.DebugTrace(
                    e, typeof(Console).Name,
                    TracePriority.ConsoleError);

                return false;
            }
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Gets the current console window width, in columns, falling back to
        /// the base host value if the console is not open or the query fails.
        /// </summary>
        protected override int WindowWidth
        {
            get
            {
                try
                {
                    SystemConsoleMustBeOpen(true); /* throw */
                    return System.Console.WindowWidth;
                }
                catch (ScriptException)
                {
                    return base.WindowWidth;
                }
                catch (Exception e)
                {
                    TraceOps.DebugTrace(
                        e, typeof(Console).Name,
                        TracePriority.ConsoleError);

                    return base.WindowWidth;
                }
            }
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Gets the current console window height, in rows, falling back to the
        /// base host value if the console is not open or the query fails.
        /// </summary>
        protected override int WindowHeight
        {
            get
            {
                try
                {
                    SystemConsoleMustBeOpen(true); /* throw */
                    return System.Console.WindowHeight;
                }
                catch (ScriptException)
                {
                    return base.WindowHeight;
                }
                catch (Exception e)
                {
                    TraceOps.DebugTrace(
                        e, typeof(Console).Name,
                        TracePriority.ConsoleError);

                    return base.WindowHeight;
                }
            }
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Saves the current console buffer and window sizes into this
        /// instance's saved-size fields so that they can be restored later.
        /// </summary>
        /// <returns>
        /// True if the sizes were saved successfully; otherwise, false.
        /// </returns>
        protected virtual bool SaveSize()
        {
            bool locked = false;

            try
            {
                TryLockWithWait(ref locked); /* TRANSACTIONAL */

                if (locked)
                {
                    return SaveSize(
                        ref savedBufferWidth, ref savedBufferHeight,
                        ref savedWindowWidth, ref savedWindowHeight);
                }
                else
                {
                    TraceOps.LockTrace(
                        "SaveSize",
                        typeof(Console).Name, false,
                        TracePriority.LockError,
                        null);
                }
            }
            catch (Exception e)
            {
                //
                // NOTE: Something failed, just return false.
                //
                TraceOps.DebugTrace(
                    e, typeof(Console).Name,
                    TracePriority.HostError);
            }
            finally
            {
                ExitLock(ref locked); /* TRANSACTIONAL */
            }

            return false;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Saves the current console buffer and window sizes into the supplied
        /// output parameters.
        /// </summary>
        /// <param name="bufferWidth">
        /// Upon success, receives the current console buffer width, in columns.
        /// </param>
        /// <param name="bufferHeight">
        /// Upon success, receives the current console buffer height, in rows.
        /// </param>
        /// <param name="windowWidth">
        /// Upon success, receives the current console window width, in columns.
        /// </param>
        /// <param name="windowHeight">
        /// Upon success, receives the current console window height, in rows.
        /// </param>
        /// <returns>
        /// True if the sizes were saved successfully; otherwise, false.
        /// </returns>
        protected virtual bool SaveSize(
            ref int bufferWidth,
            ref int bufferHeight,
            ref int windowWidth,
            ref int windowHeight
            )
        {
            //
            // NOTE: Save original console dimensions in case we need
            //       to restore from the later (e.g. ResetSize).
            //
            try
            {
                SystemConsoleMustBeOpen(true); /* throw */

                bufferWidth = System.Console.BufferWidth;
                bufferHeight = System.Console.BufferHeight;

                windowWidth = System.Console.WindowWidth;
                windowHeight = System.Console.WindowHeight;

                return true;
            }
            catch (ScriptException)
            {
                //
                // NOTE: The console is not open, just return false.
                //
                return false;
            }
            catch (Exception e)
            {
                //
                // NOTE: Something failed, just return false.
                //
                TraceOps.DebugTrace(
                    e, typeof(Console).Name,
                    TracePriority.ConsoleError);

                return false;
            }
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Sets the console buffer and window sizes to the specified values.
        /// </summary>
        /// <param name="bufferWidth">
        /// The new console buffer width, in columns.
        /// </param>
        /// <param name="bufferHeight">
        /// The new console buffer height, in rows.
        /// </param>
        /// <param name="windowWidth">
        /// The new console window width, in columns.
        /// </param>
        /// <param name="windowHeight">
        /// The new console window height, in rows.
        /// </param>
        /// <returns>
        /// True if the sizes were set successfully; otherwise, false.
        /// </returns>
        protected virtual bool SetSize(
            int bufferWidth,
            int bufferHeight,
            int windowWidth,
            int windowHeight
            )
        {
            try
            {
                SystemConsoleMustBeOpen(true); /* throw */

                //
                // NOTE: Set the window size to the minimum possible so that
                //       any buffer size we set (within bounds) is valid.
                //
                System.Console.SetWindowSize(1, 1);

                //
                // NOTE: Set the new buffer size.
                //
                System.Console.SetBufferSize(bufferWidth, bufferHeight);

                //
                // NOTE: Set the new window size.
                //
                System.Console.SetWindowSize(windowWidth, windowHeight);

                //
                // NOTE: If we get this far, we've succeeded.
                //
                return true;
            }
            catch (ScriptException)
            {
                //
                // NOTE: The console is not open, just return false.
                //
                return false;
            }
            catch (Exception e)
            {
                //
                // NOTE: Something failed, just return false.
                //
                TraceOps.DebugTrace(
                    e, typeof(Console).Name,
                    TracePriority.ConsoleError);

                return false;
            }
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Computes valid console buffer and window sizes from the requested
        /// width and height, applying margins and clamping to reasonable limits.
        /// </summary>
        /// <param name="width">
        /// The requested width, in columns, or an invalid size to keep the
        /// current width.
        /// </param>
        /// <param name="height">
        /// The requested height, in rows, or an invalid size to keep the current
        /// height.
        /// </param>
        /// <param name="maximum">
        /// Non-zero to compute sizes suitable for the maximum console size;
        /// otherwise, zero.
        /// </param>
        /// <param name="bufferWidth">
        /// Upon success, receives the computed console buffer width, in columns.
        /// </param>
        /// <param name="bufferHeight">
        /// Upon success, receives the computed console buffer height, in rows.
        /// </param>
        /// <param name="windowWidth">
        /// Upon success, receives the computed console window width, in columns.
        /// </param>
        /// <param name="windowHeight">
        /// Upon success, receives the computed console window height, in rows.
        /// </param>
        /// <returns>
        /// True if the sizes were computed successfully; otherwise, false.
        /// </returns>
        protected virtual bool CalculateSize(
            int width,
            int height,
            bool maximum,
            ref int bufferWidth,
            ref int bufferHeight,
            ref int windowWidth,
            ref int windowHeight
            )
        {
            try
            {
                SystemConsoleMustBeOpen(true); /* throw */

                //
                // NOTE: If the caller does not want to set the width (i.e. it
                //       is invalid) then use the current window width;
                //       otherwise, if setting up for the maximum console size,
                //       subtract the necessary width margin from the provided
                //       width value.
                //
                int newWindowWidth = width;

                if (newWindowWidth == _Size.Invalid)
                    newWindowWidth = System.Console.WindowWidth;
                else if (maximum)
                    newWindowWidth -= MaximumWindowWidthMargin;

                ///////////////////////////////////////////////////////////////

                //
                // NOTE: If the caller does not want to set the height (i.e. it
                //       is invalid) then use the current window height;
                //       otherwise, if setting up for the maximum console size,
                //       subtract the necessary height margin from the provided
                //       height value.
                //
                int newWindowHeight = height;

                if (newWindowHeight == _Size.Invalid)
                    newWindowHeight = System.Console.WindowHeight;
                else if (maximum)
                    newWindowHeight -= MaximumWindowHeightMargin;

                ///////////////////////////////////////////////////////////////

                //
                // NOTE: If the caller does not want to set the width (i.e. it
                //       is invalid) then use the current buffer width;
                //       otherwise, if setting up for the maximum console size,
                //       subtract the necessary width margin from the provided
                //       width value.
                //
                int newBufferWidth = width;

                if (newBufferWidth == _Size.Invalid)
                    newBufferWidth = System.Console.BufferWidth;
                else if (maximum)
                    newBufferWidth -= MaximumBufferWidthMargin;

                ///////////////////////////////////////////////////////////////

                //
                // HACK: *SPECIAL CASE* If the caller does not want to set the
                //       height (i.e. it is invalid) then use the current
                //       buffer height; otherwise, if setting up for the
                //       maximum console size, we always want to set the buffer
                //       height to the maximum "reasonable" value (i.e. for use
                //       as a scrollback buffer).  The maximum "reasonable"
                //       value is typically 9999 because that is what modern
                //       (all?) versions of Windows recognize for console-based
                //       applications via the shell properties dialog.
                //
                int newBufferHeight = height;

                if (newBufferHeight == _Size.Invalid)
                    newBufferHeight = System.Console.BufferHeight;
                else if (maximum)
                    newBufferHeight = MaximumBufferHeight;

                ///////////////////////////////////////////////////////////////

                //
                // NOTE: Try to make sure that we do not attempt to set an
                //       unreasonable window width.
                //
                if ((newWindowWidth > newBufferWidth) ||
                    (newWindowWidth > System.Console.LargestWindowWidth))
                {
                    newWindowWidth = Math.Min(newBufferWidth,
                        System.Console.LargestWindowWidth);
                }

                ///////////////////////////////////////////////////////////////

                //
                // NOTE: Try to make sure that we do not attempt to set an
                //       unreasonable window height.
                //
                if ((newWindowHeight > newBufferHeight) ||
                    (newWindowHeight > System.Console.LargestWindowHeight))
                {
                    newWindowHeight = Math.Min(newBufferHeight,
                        System.Console.LargestWindowHeight);
                }

                ///////////////////////////////////////////////////////////////

                //
                // NOTE: Commit all changes to the output parameters provided
                //       by the caller.
                //
                bufferWidth = newBufferWidth;
                bufferHeight = newBufferHeight;
                windowWidth = newWindowWidth;
                windowHeight = newWindowHeight;

                ///////////////////////////////////////////////////////////////

                //
                // NOTE: If we get this far, we succeeded.
                //
                return true;
            }
            catch (ScriptException)
            {
                //
                // NOTE: The console is not open, just return false.
                //
                return false;
            }
            catch (Exception e)
            {
                //
                // NOTE: Something failed, just return false.
                //
                TraceOps.DebugTrace(
                    e, typeof(Console).Name,
                    TracePriority.ConsoleError);

                return false;
            }
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Computes and sets the console buffer and window sizes from the
        /// requested width and height.
        /// </summary>
        /// <param name="width">
        /// The requested width, in columns, or an invalid size to keep the
        /// current width.
        /// </param>
        /// <param name="height">
        /// The requested height, in rows, or an invalid size to keep the current
        /// height.
        /// </param>
        /// <param name="maximum">
        /// Non-zero to set sizes suitable for the maximum console size;
        /// otherwise, zero.
        /// </param>
        /// <returns>
        /// True if the sizes were set successfully; otherwise, false.
        /// </returns>
        protected virtual bool SetSize(
            int width,
            int height,
            bool maximum
            )
        {
            try
            {
                int newBufferWidth = _Size.Invalid;
                int newBufferHeight = _Size.Invalid;
                int newWindowWidth = _Size.Invalid;
                int newWindowHeight = _Size.Invalid;

                if (!CalculateSize(
                        width, height, maximum, ref newBufferWidth,
                        ref newBufferHeight, ref newWindowWidth,
                        ref newWindowHeight))
                {
                    return false;
                }

                ///////////////////////////////////////////////////////////////

                //
                // NOTE: Reset the console buffer and window sizes.
                //
                return SetSize(
                    newBufferWidth, newBufferHeight,
                    newWindowWidth, newWindowHeight);
            }
            catch
            {
                //
                // NOTE: Something failed, just return false.
                //
                return false;
            }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////////////////////////////

        #region System.Console Color Save/Restore
        /// <summary>
        /// Saves the current console foreground and background colors into this
        /// instance's saved-color fields so that they can be restored later.
        /// </summary>
        /// <returns>
        /// True if the colors were saved successfully; otherwise, false.
        /// </returns>
        protected virtual bool SaveColors()
        {
            //
            // NOTE: Save original console colors in case we need to restore
            //       from the later.
            //
            bool locked = false;

            try
            {
                TryLockWithWait(ref locked); /* TRANSACTIONAL */

                if (locked)
                {
                    return GetColors(
                        ref savedForegroundColor, ref savedBackgroundColor);
                }
                else
                {
                    TraceOps.LockTrace(
                        "SaveColors",
                        typeof(Console).Name, false,
                        TracePriority.LockError,
                        null);
                }
            }
            catch (Exception e)
            {
                //
                // NOTE: Something failed, just return false.
                //
                TraceOps.DebugTrace(
                    e, typeof(Console).Name,
                    TracePriority.HostError);
            }
            finally
            {
                ExitLock(ref locked); /* TRANSACTIONAL */
            }

            return false;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Determines whether the console colors should be reset prior to
        /// setting them, based on whether the requested colors match the saved
        /// colors.
        /// </summary>
        /// <param name="foreground">
        /// Non-zero if the foreground color is being set; otherwise, zero.
        /// </param>
        /// <param name="background">
        /// Non-zero if the background color is being set; otherwise, zero.
        /// </param>
        /// <param name="foregroundColor">
        /// The foreground color being set.
        /// </param>
        /// <param name="backgroundColor">
        /// The background color being set.
        /// </param>
        /// <returns>
        /// True if the colors should be reset before being set; otherwise,
        /// false.
        /// </returns>
        protected internal virtual bool ShouldResetColorsForSetColors(
            bool foreground,
            bool background,
            ConsoleColor foregroundColor,
            ConsoleColor backgroundColor
            )
        {
            bool locked = false;

            try
            {
                TryLockWithWait(ref locked); /* TRANSACTIONAL */

                if (locked)
                {
                    if (foreground && background &&
                        (foregroundColor == savedForegroundColor) &&
                        (backgroundColor == savedBackgroundColor))
                    {
                        return true;
                    }
                }
                else
                {
                    TraceOps.LockTrace(
                        "ShouldResetColorsForSetColors",
                        typeof(Console).Name, false,
                        TracePriority.LockError,
                        null);
                }
            }
            catch (Exception e)
            {
                //
                // NOTE: Something failed, just return false.
                //
                TraceOps.DebugTrace(
                    e, typeof(Console).Name,
                    TracePriority.HostError);
            }
            finally
            {
                ExitLock(ref locked); /* TRANSACTIONAL */
            }

            return false;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Restores the originally saved console foreground and background
        /// colors.
        /// </summary>
        /// <returns>
        /// True if the colors were restored successfully; otherwise, false.
        /// </returns>
        protected internal virtual bool RestoreColors()
        {
            //
            // NOTE: Restore the originally saved console colors.
            //
            bool locked = false;

            try
            {
                TryLockWithWait(ref locked); /* TRANSACTIONAL */

                if (locked)
                {
                    if (DoesResetColorForRestore() &&
                        ((savedForegroundColor == _ConsoleColor.None) ||
                        (savedBackgroundColor == _ConsoleColor.None)) &&
                        !ResetColors())
                    {
                        return false;
                    }

                    return SetColors(true, true,
                        savedForegroundColor, savedBackgroundColor);
                }
                else
                {
                    TraceOps.LockTrace(
                        "RestoreColors",
                        typeof(Console).Name, false,
                        TracePriority.LockError,
                        null);
                }
            }
            catch (Exception e)
            {
                //
                // NOTE: Something failed, just return false.
                //
                TraceOps.DebugTrace(
                    e, typeof(Console).Name,
                    TracePriority.HostError);
            }
            finally
            {
                ExitLock(ref locked); /* TRANSACTIONAL */
            }

            return false;
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////////////////////////////

        #region System.Console Title Handling
        /// <summary>
        /// Gets the certificate subject to display in the console window title,
        /// computing it once by verifying that the core and entry assemblies
        /// share the same certificate subject.
        /// </summary>
        /// <returns>
        /// The certificate subject string, or null if it is unavailable or the
        /// subjects do not match.
        /// </returns>
        protected virtual string GetCertificateSubject()
        {
            lock (syncRoot) /* TRANSACTIONAL */
            {
                if (Interlocked.Increment(ref certificateCount) == 1)
                {
                    //
                    // NOTE: Is trust checking enabled for executable files that are
                    //       signed with X509 certificates?  If not, no work will be
                    //       done by the RuntimeOps.GetCertificateSubject method.
                    //
                    // NOTE: Technically, this should [probably] not be using the
                    //       SetupOps.ShouldCheckCoreTrusted method here (i.e. since
                    //       the entry assembly could be a third-party executable;
                    //       however, in this particular context, "core" is intended
                    //       to include the shell as well, just not plugins).  Also,
                    //       the certificate subjects are checked (just below here)
                    //       for equality, prior to being displayed to the user.
                    //
                    bool trusted = RuntimeOps.ShouldCheckCoreFileTrusted();

                    //
                    // NOTE: An interpreter context can now be used to supply the
                    //       list of implicitly trusted file hashes, i.e. in case
                    //       the underlying platform cannot recognize Authenticode
                    //       signatures on managed assemblies.  When null, it will
                    //       fallback to the legacy (Authenticode-only) handling.
                    //
                    Interpreter interpreter = SafeGetInterpreter();

                    //
                    // BUGFIX: Verify that the certificate subjects are the same for
                    //         this assembly (i.e. the Eagle core library) and the
                    //         entry assembly (e.g. the Eagle shell).
                    //
                    string thisCertificateSubject = RuntimeOps.GetCertificateSubject(
                        interpreter, GlobalState.GetAssemblyLocation(),
                        CertificateSubjectPrefix, trusted, true, false);

                    if (thisCertificateSubject != null)
                    {
                        string entryCertificateSubject = RuntimeOps.GetCertificateSubject(
                            interpreter, GlobalState.GetEntryAssemblyLocation(),
                            CertificateSubjectPrefix, trusted, true, false);

                        if (entryCertificateSubject != null)
                        {
                            if (SharedStringOps.SystemEquals(
                                    thisCertificateSubject, entryCertificateSubject))
                            {
                                //
                                // NOTE: If we get to this point, the core assembly
                                //       (i.e. this one) and the entry assembly have
                                //       the same certificate subject.  Most likely,
                                //       this is because the entry assembly is the
                                //       standard Eagle shell assembly.
                                //
                                certificateSubject = thisCertificateSubject;
                            }
                            else
                            {
                                TraceOps.DebugTrace(String.Format(
                                    "GetCertificateSubject: certificate subject " +
                                    "mismatch, core = {0}, entry = {1}",
                                    FormatOps.WrapOrNull(thisCertificateSubject),
                                    FormatOps.WrapOrNull(entryCertificateSubject)),
                                    typeof(Console).Name, TracePriority.HostDebug);

                                certificateSubject = null;
                            }
                        }
                        else
                        {
                            TraceOps.DebugTrace(
                                "GetCertificateSubject: no certificate subject for entry assembly",
                                typeof(Console).Name, TracePriority.HostDebug);

                            certificateSubject = null;
                        }
                    }
                    else
                    {
                        TraceOps.DebugTrace(
                            "GetCertificateSubject: no certificate subject for core assembly",
                            typeof(Console).Name, TracePriority.HostDebug);

                        certificateSubject = null;
                    }
                }
                else
                {
                    Interlocked.Decrement(ref certificateCount);
                }

                return certificateSubject;
            }
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Saves the current console window title so that it can be restored
        /// later, unless it has already been saved or the platform does not
        /// support fetching the title.
        /// </summary>
        /// <param name="error">
        /// Upon failure, receives information about the error encountered.
        /// </param>
        /// <returns>
        /// True if the title was saved (or saving was not required); otherwise,
        /// false.
        /// </returns>
        protected virtual bool SaveTitle(
            ref Result error
            )
        {
            try
            {
                SystemConsoleMustBeOpen(true); /* throw */

                //
                // HACK: When running on .NET Core under Unix, fetching
                //       the console title is not supported.
                //
                if (PlatformOps.IsWindowsOperatingSystem() ||
                    !ShouldTreatAsDotNetCore())
                {
                    lock (syncRoot) /* TRANSACTIONAL */
                    {
                        //
                        // NOTE: Since this method can be called any number
                        //       of times (i.e. via the RefreshTitle method),
                        //       only save the title if it has not already
                        //       been saved.
                        //
                        if (savedTitle == null)
                            savedTitle = System.Console.Title;
                    }
                }

                return true;
            }
            catch (Exception e)
            {
                error = e;
            }

            return false;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Restores the previously saved console window title, if any.
        /// </summary>
        /// <param name="error">
        /// Upon failure, receives information about the error encountered.
        /// </param>
        /// <returns>
        /// True if the title was restored successfully; otherwise, false.
        /// </returns>
        protected virtual bool RestoreTitle(
            ref Result error
            )
        {
            try
            {
                SystemConsoleMustBeOpen(true); /* throw */

                lock (syncRoot) /* TRANSACTIONAL */
                {
                    if (savedTitle != null)
                    {
                        System.Console.Title = savedTitle;
                        savedTitle = null;
                    }
                }

                return true;
            }
            catch (Exception e)
            {
                error = e;
            }

            return false;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Builds the console window title from the administrator prefix, the
        /// default and base titles, the optional certificate subject, and the
        /// interactive mode.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter whose interactive mode is included in the title.  This
        /// parameter may be null.
        /// </param>
        /// <param name="useCertificate">
        /// Non-zero to include the certificate subject; zero to include the
        /// pending-certificate placeholder; null to omit any certificate text.
        /// </param>
        /// <returns>
        /// The constructed console window title.
        /// </returns>
        protected virtual string BuildTitle(
            Interpreter interpreter,
            bool? useCertificate
            )
        {
            StringBuilder result = StringBuilderFactory.Create();

            string[] values = {
                RuntimeOps.IsAdministrator() ?
                    AdministratorTitlePrefix : String.Empty,
                DefaultTitle, base.Title,
                (useCertificate != null) ?
                    ((bool)useCertificate ?
                        GetCertificateSubject() :
                        CertificateSubjectPending) :
                    null,
                HostOps.GetInteractiveMode(interpreter)
            };

            foreach (string value in values)
            {
                if (!String.IsNullOrEmpty(value))
                {
                    if (result.Length > 0)
                        result.Append(Characters.Space);

                    result.Append(value);
                }
            }

            return StringBuilderCache.GetStringAndRelease(ref result);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Sets the console window title, first showing the original title while
        /// the certificate is being checked and then showing the final title.
        /// </summary>
        /// <param name="error">
        /// Upon failure, receives information about the error encountered.
        /// </param>
        /// <returns>
        /// True if the title was set successfully; otherwise, false.
        /// </returns>
        protected virtual bool SetTitle(
            ref Result error
            )
        {
            try
            {
                SystemConsoleMustBeOpen(true); /* throw */

                Interpreter interpreter = InternalSafeGetInterpreter(false);

                lock (syncRoot) /* TRANSACTIONAL */
                {
                    //
                    // HACK: Permit the user to see the original
                    //       title while the certificate is being
                    //       checked, if applicable.
                    //
                    IList<bool?> useCertificates = new List<bool?>(2);

                    //
                    // HACK: Also, permit Authenticode certificate
                    //       to be skipped when explicitly disabled
                    //       by the user (e.g. via the "NoTrusted"
                    //       environment variable).
                    //
                    useCertificates.Add(false);

                    if (RuntimeOps.ShouldCheckCoreFileTrusted())
                        useCertificates.Add(true);
                    else
                        useCertificates.Add(null);

                    foreach (bool? useCertificate in useCertificates)
                    {
                        string title = BuildTitle(
                            interpreter, useCertificate);

                        if (title == null)
                            continue;

                        System.Console.Title = title;
                    }
                }

                return true;
            }
            catch (Exception e)
            {
                error = e;
            }

            return false;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Sets up or tears down the console window title, saving and setting the
        /// title during setup and restoring it during teardown, unless changing
        /// the title has been disabled.
        /// </summary>
        /// <param name="setup">
        /// Non-zero to set up (save and set) the title; zero to tear down
        /// (restore) the title.
        /// </param>
        /// <returns>
        /// True if the operation succeeded; otherwise, false.
        /// </returns>
        protected virtual bool SetupTitle(
            bool setup
            )
        {
            try
            {
                //
                // NOTE: Has changing the title been explicitly disabled?
                //
                if (!NoTitle && IsOpen())
                {
                    Result error = null;

                    if (setup)
                    {
                        if (SaveTitle(ref error))
                        {
                            if (SetTitle(ref error))
                            {
                                return true;
                            }
                            else
                            {
                                TraceOps.DebugTrace(String.Format(
                                    "SetupTitle: set error = {0}",
                                    error), typeof(Console).Name,
                                    TracePriority.HostError);
                            }
                        }
                        else
                        {
                            TraceOps.DebugTrace(String.Format(
                                "SetupTitle: save error = {0}",
                                error), typeof(Console).Name,
                                TracePriority.HostError);
                        }
                    }
                    else
                    {
                        if (RestoreTitle(ref error))
                        {
                            return true;
                        }
                        else
                        {
                            TraceOps.DebugTrace(String.Format(
                                "SetupTitle: restore error = {0}",
                                error), typeof(Console).Name,
                                TracePriority.HostError);
                        }
                    }
                }
                else
                {
                    return true; /* BUGFIX: Fake success. */
                }
            }
            catch (Exception e)
            {
                //
                // NOTE: Something failed, just return false.
                //
                TraceOps.DebugTrace(
                    e, typeof(Console).Name,
                    TracePriority.ConsoleError);
            }

            return false;
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////////////////////////////

        #region Console Stream Handling
        /// <summary>
        /// Determines whether the specified console channel has been redirected.
        /// </summary>
        /// <param name="channelType">
        /// The console channel to query.
        /// </param>
        /// <returns>
        /// True if the specified channel has been redirected; otherwise, false.
        /// </returns>
        protected virtual bool IsChannelRedirected(
            ChannelType channelType
            )
        {
            return SystemConsoleIsRedirected(
                InternalSafeGetInterpreter(false), channelType, false);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        #region System.Console Stream Handling
        /// <summary>
        /// Gets the system console input stream.
        /// </summary>
        /// <param name="interactiveHost">
        /// The interactive host whose input redirection state is consulted.
        /// This parameter may be null.
        /// </param>
        /// <returns>
        /// The console input stream, or null if it could not be obtained.
        /// </returns>
        private static Stream GetInputStream(
            IInteractiveHost interactiveHost
            )
        {
            Stream stream = null;
            Result error = null;

            if (GetInputStream(
                    interactiveHost, ref stream,
                    ref error) == ReturnCode.Ok)
            {
                return stream;
            }

            return null;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Gets the system console input stream.
        /// </summary>
        /// <param name="interactiveHost">
        /// The interactive host whose input redirection state is consulted.
        /// This parameter may be null.
        /// </param>
        /// <param name="stream">
        /// Upon success, receives the console input stream.
        /// </param>
        /// <param name="error">
        /// Upon failure, receives information about the error encountered.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise,
        /// <see cref="ReturnCode.Error" />.
        /// </returns>
        private static ReturnCode GetInputStream(
            IInteractiveHost interactiveHost,
            ref Stream stream,
            ref Result error
            )
        {
            try
            {
                SystemConsoleInputMustBeOpen(interactiveHost); /* throw */

                if (ConsoleOps.GetInputStream(
                        ref stream, ref error) == ReturnCode.Ok)
                {
                    return ReturnCode.Ok;
                }
            }
            catch (Exception e)
            {
                error = e;
            }

            return ReturnCode.Error;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Gets the system console output stream.
        /// </summary>
        /// <param name="streamHost">
        /// The stream host whose output redirection state is consulted.  This
        /// parameter may be null.
        /// </param>
        /// <returns>
        /// The console output stream, or null if it could not be obtained.
        /// </returns>
        private static Stream GetOutputStream(
            IStreamHost streamHost
            )
        {
            Stream stream = null;
            Result error = null;

            if (GetOutputStream(
                    streamHost, ref stream,
                    ref error) == ReturnCode.Ok)
            {
                return stream;
            }

            return null;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Gets the system console output stream.
        /// </summary>
        /// <param name="streamHost">
        /// The stream host whose output redirection state is consulted.  This
        /// parameter may be null.
        /// </param>
        /// <param name="stream">
        /// Upon success, receives the console output stream.
        /// </param>
        /// <param name="error">
        /// Upon failure, receives information about the error encountered.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise,
        /// <see cref="ReturnCode.Error" />.
        /// </returns>
        private static ReturnCode GetOutputStream(
            IStreamHost streamHost,
            ref Stream stream,
            ref Result error
            )
        {
            try
            {
                SystemConsoleOutputMustBeOpen(streamHost); /* throw */

                if (ConsoleOps.GetOutputStream(
                        ref stream, ref error) == ReturnCode.Ok)
                {
                    return ReturnCode.Ok;
                }
            }
            catch (Exception e)
            {
                error = e;
            }

            return ReturnCode.Error;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Gets the system console error stream.
        /// </summary>
        /// <param name="streamHost">
        /// The stream host whose error redirection state is consulted.  This
        /// parameter may be null.
        /// </param>
        /// <returns>
        /// The console error stream, or null if it could not be obtained.
        /// </returns>
        private static Stream GetErrorStream(
            IStreamHost streamHost
            )
        {
            Stream stream = null;
            Result error = null;

            if (GetErrorStream(
                    streamHost, ref stream,
                    ref error) == ReturnCode.Ok)
            {
                return stream;
            }

            return null;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Gets the system console error stream.
        /// </summary>
        /// <param name="streamHost">
        /// The stream host whose error redirection state is consulted.  This
        /// parameter may be null.
        /// </param>
        /// <param name="stream">
        /// Upon success, receives the console error stream.
        /// </param>
        /// <param name="error">
        /// Upon failure, receives information about the error encountered.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise,
        /// <see cref="ReturnCode.Error" />.
        /// </returns>
        private static ReturnCode GetErrorStream(
            IStreamHost streamHost,
            ref Stream stream,
            ref Result error
            )
        {
            try
            {
                SystemConsoleErrorMustBeOpen(streamHost); /* throw */

                if (ConsoleOps.GetErrorStream(
                        ref stream, ref error) == ReturnCode.Ok)
                {
                    return ReturnCode.Ok;
                }
            }
            catch (Exception e)
            {
                error = e;
            }

            return ReturnCode.Error;
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////////////////////////////

        #region Static Stream "Factory" Methods
        /// <summary>
        /// Creates a new stream reader over the specified stream using the
        /// specified encoding.
        /// </summary>
        /// <param name="stream">
        /// The stream to read from.  This parameter may be null.
        /// </param>
        /// <param name="encoding">
        /// The encoding to use when reading.  This parameter may be null.
        /// </param>
        /// <returns>
        /// The new stream reader, or null if the stream or encoding is null.
        /// </returns>
        private static StreamReader NewStreamReader(
            Stream stream,
            Encoding encoding
            )
        {
            if ((stream != null) && (encoding != null))
                return new StreamReader(stream, encoding);

            return null;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Creates a new stream writer over the specified stream using the
        /// specified encoding and auto-flush setting.
        /// </summary>
        /// <param name="stream">
        /// The stream to write to.  This parameter may be null.
        /// </param>
        /// <param name="encoding">
        /// The encoding to use when writing.  This parameter may be null.
        /// </param>
        /// <param name="autoFlush">
        /// Non-zero to flush the writer after every write; otherwise, zero.
        /// </param>
        /// <returns>
        /// The new stream writer, or null if the stream or encoding is null.
        /// </returns>
        private static StreamWriter NewStreamWriter(
            Stream stream,
            Encoding encoding,
            bool autoFlush
            )
        {
            if ((stream != null) && (encoding != null))
            {
                StreamWriter streamWriter =
                    new StreamWriter(stream, encoding);

                streamWriter.AutoFlush = autoFlush;

                return streamWriter;
            }

            return null;
        }
        #endregion
        #endregion

        ///////////////////////////////////////////////////////////////////////////////////////////////

        #region Console Setup Handling
        #region Console CancelKeyPress Handling
        #region ConsoleCancelEventHandler Handling
        /// <summary>
        /// Gets the shared console Ctrl-C (cancel) event handler, creating it on
        /// first use.
        /// </summary>
        /// <returns>
        /// The shared console cancel event handler.
        /// </returns>
        private static ConsoleCancelEventHandler GetConsoleCancelEventHandler()
        {
            lock (staticSyncRoot) /* TRANSACTIONAL */
            {
                if (consoleCancelEventHandler == null)
                {
                    consoleCancelEventHandler =
                        Interpreter.NewConsoleCancelEventHandler();
                }

                return consoleCancelEventHandler;
            }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////////////////////////////

        #region Native Console CancelKeyPress Handling
#if NATIVE && WINDOWS
        /// <summary>
        /// Unhooks the native Win32 console control handler installed by the
        /// managed console subsystem, working around incorrect internal state
        /// management.
        /// </summary>
        /// <param name="force">
        /// Non-zero to unhook even when script cancellation has been disabled;
        /// otherwise, zero.
        /// </param>
        /// <param name="strict">
        /// Non-zero to treat the absence of an expected handler as an error;
        /// otherwise, zero.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise,
        /// <see cref="ReturnCode.Error" />.
        /// </returns>
        private ReturnCode UnhookSystemConsoleControlHandler(
            bool force,
            bool strict
            )
        {
            Result error = null;

            return UnhookSystemConsoleControlHandler(force, strict, ref error);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Unhooks the native Win32 console control handler installed by the
        /// managed console subsystem, working around incorrect internal state
        /// management.
        /// </summary>
        /// <param name="force">
        /// Non-zero to unhook even when script cancellation has been disabled;
        /// otherwise, zero.
        /// </param>
        /// <param name="strict">
        /// Non-zero to treat the absence of an expected handler as an error;
        /// otherwise, zero.
        /// </param>
        /// <param name="error">
        /// Upon failure, receives information about the error encountered.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise,
        /// <see cref="ReturnCode.Error" />.
        /// </returns>
        private ReturnCode UnhookSystemConsoleControlHandler(
            bool force,
            bool strict,
            ref Result error
            )
        {
            try
            {
                if (force || !NoCancel)
                {
                    ReturnCode code;
                    StringList list = new StringList();

                    code = ConsoleOps.UnhookControlHandler(
                        strict, list, ref error);

                    TraceOps.DebugTrace(
                        "UnhookSystemConsoleControlHandler",
                        "UnhookControlHandler",
                        typeof(Console).Name,
                        (code == ReturnCode.Ok) ?
                            TracePriority.HostDebug2 :
                            TracePriority.HostError2,
                        false, "code", code, "list", list,
                        "error", error);

                    return code;
                }
                else
                {
                    return ReturnCode.Ok; // NOTE: Fake success.
                }
            }
            catch (Exception e)
            {
                error = e;
            }

            return ReturnCode.Error;
        }
#endif
        #endregion

        ///////////////////////////////////////////////////////////////////////////////////////////////

        #region System.Console CancelKeyPress Handling
        /// <summary>
        /// Determines whether a script cancellation triggered via the console is
        /// currently pending.
        /// </summary>
        /// <returns>
        /// True if a console-triggered cancellation is pending; otherwise,
        /// false.
        /// </returns>
        protected virtual bool IsCancelViaConsolePending()
        {
            return Interpreter.IsCancelViaConsolePending();
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Installs the console Ctrl-C (cancel) keypress handler, unless a cancel
        /// event is already pending and installation is not being forced.
        /// </summary>
        /// <param name="force">
        /// Non-zero to install the handler even when a cancel event is pending;
        /// otherwise, zero.
        /// </param>
        /// <returns>
        /// True if the handler was installed; otherwise, false.
        /// </returns>
        protected virtual bool InstallCancelKeyPressHandler(
            bool force
            )
        {
            try
            {
                SystemConsoleMustBeOpen(false); /* throw */

                ConsoleCancelEventHandler handler =
                    GetConsoleCancelEventHandler();

                if (handler != null)
                {
                    if (force || !IsCancelViaConsolePending())
                    {
                        System.Console.CancelKeyPress += handler;
                        return true; // success.
                    }
                    else
                    {
                        return false; // event pending.
                    }
                }
                else
                {
                    return false; // no handler.
                }
            }
            catch (ScriptException)
            {
                //
                // NOTE: The console is not open, just return false.
                //
                return false;
            }
            catch (Exception e)
            {
                //
                // NOTE: Something failed, just return false.
                //
                TraceOps.DebugTrace(
                    e, typeof(Console).Name,
                    TracePriority.ConsoleError);

                return false; // failure.
            }
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Uninstalls the console Ctrl-C (cancel) keypress handler, unless a
        /// cancel event is already pending and uninstallation is not being
        /// forced.
        /// </summary>
        /// <param name="force">
        /// Non-zero to uninstall the handler even when a cancel event is
        /// pending; otherwise, zero.
        /// </param>
        /// <returns>
        /// True if the handler was uninstalled; otherwise, false.
        /// </returns>
        protected internal virtual bool UninstallCancelKeyPressHandler(
            bool force
            )
        {
            try
            {
                SystemConsoleMustBeOpen(false); /* throw */

                ConsoleCancelEventHandler handler =
                    GetConsoleCancelEventHandler();

                if (handler != null)
                {
                    if (force || !IsCancelViaConsolePending())
                    {
                        System.Console.CancelKeyPress -= handler;

#if NATIVE && WINDOWS
                        if (!CommonOps.Runtime.IsMono() &&
                            !CommonOps.Runtime.IsDotNetCore())
                        {
                            //
                            // HACK: Prior to .NET Framework 4.x (?),
                            //       the System.Console handling for
                            //       Ctrl-C events had a problem with
                            //       fully unhooking from the native
                            //       Win32 subsystem due to incorrect
                            //       internal state management.  This
                            //       works around that issue.
                            //
                            if (CommonOps.Runtime.IsFramework20() ||
                                CommonOps.Runtime.IsFramework40())
                            {
                                UnhookSystemConsoleControlHandler(
                                    true, true);
                            }
                        }
#endif

                        return true; // success.
                    }
                    else
                    {
                        return false; // event pending.
                    }
                }
                else
                {
                    return false; // no handler.
                }
            }
            catch (ScriptException)
            {
                //
                // NOTE: The console is not open, just return false.
                //
                return false;
            }
            catch (Exception e)
            {
                //
                // NOTE: Something failed, just return false.
                //
                TraceOps.DebugTrace(
                    e, typeof(Console).Name,
                    TracePriority.ConsoleError);

                return false; // failure.
            }
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Installs or uninstalls the console Ctrl-C (cancel) keypress handler,
        /// unless cancellation handling has been disabled or the current
        /// application domain is not eligible.
        /// </summary>
        /// <param name="setup">
        /// Non-zero to install the handler; zero to uninstall it.
        /// </param>
        /// <param name="forceAppDomain">
        /// Non-zero to set up the handler even in a non-default application
        /// domain; otherwise, zero.
        /// </param>
        /// <param name="forcePending">
        /// Non-zero to install or uninstall the handler even when a cancel event
        /// is pending; otherwise, zero.
        /// </param>
        /// <returns>
        /// True if the operation succeeded; otherwise, false.
        /// </returns>
        protected virtual bool SetupCancelKeyPressHandler(
            bool setup,
            bool forceAppDomain,
            bool forcePending
            )
        {
            try
            {
                //
                // NOTE: Has setting up the script cancellation
                //       keypress been explicitly disabled?
                //
                if (!NoCancel && (forceAppDomain ||
                        AppDomainOps.IsCurrentDefault()))
                {
                    return setup ?
                        InstallCancelKeyPressHandler(forcePending) :
                        UninstallCancelKeyPressHandler(forcePending);
                }
                else
                {
                    return true; // fake success.
                }
            }
            catch (Exception e)
            {
                //
                // NOTE: Something failed, just return false.
                //
                TraceOps.DebugTrace(
                    e, typeof(Console).Name,
                    TracePriority.ConsoleError);

                return false; // failure.
            }
        }
        #endregion
        #endregion

        ///////////////////////////////////////////////////////////////////////////////////////////////

        #region Native Console Icon Handling
#if NATIVE && WINDOWS && DRAWING && !NET_STANDARD_20
        /// <summary>
        /// Installs the custom console window icon from the entry assembly,
        /// unless changing the icon has been disabled or the platform does not
        /// support it.
        /// </summary>
        /// <returns>
        /// True if the icon was installed (or installation was not required);
        /// otherwise, false.
        /// </returns>
        private bool SetupIcon()
        {
            try
            {
                //
                // NOTE: Has changing the icon been explicitly disabled?
                //
                if (!NoIcon)
                {
                    if (PlatformOps.IsWindowsOperatingSystem())
                    {
                        return SetupIcon(
                            true, AssemblyOps.GetIconStream());
                    }
                    else
                    {
                        return true; /* BUGFIX: Fake success. */
                    }
                }
                else
                {
                    return true; /* BUGFIX: Fake success. */
                }
            }
            catch (Exception e)
            {
                //
                // NOTE: Something failed, just return false.
                //
                TraceOps.DebugTrace(
                    e, typeof(Console).Name,
                    TracePriority.HostError);
            }

            return false;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Installs or uninstalls the custom console window icon, unless changing
        /// the icon has been disabled or the platform does not support it.
        /// </summary>
        /// <param name="setup">
        /// Non-zero to install the icon; zero to uninstall it.
        /// </param>
        /// <param name="stream">
        /// The stream containing the icon to install.  This parameter may be
        /// null.
        /// </param>
        /// <returns>
        /// True if the operation succeeded (or was not required); otherwise,
        /// false.
        /// </returns>
        private bool SetupIcon(
            bool setup,   /* in */
            Stream stream /* in */
            )
        {
            try
            {
                //
                // NOTE: Has changing the icon been explicitly disabled?
                //
                if (!NoIcon && IsOpen())
                {
                    if (PlatformOps.IsWindowsOperatingSystem())
                    {
                        IntPtr handle = WindowOps.GetIconWindow();

                        if (handle != IntPtr.Zero)
                        {
                            if (setup)
                            {
                                if (stream != null)
                                {
                                    InstallIcon(handle, stream);
                                    return true;
                                }
                            }
                            else
                            {
                                UninstallIcon(handle);
                                return true;
                            }
                        }
                        else
                        {
                            return true; /* NOTE: Fake success. */
                        }
                    }
                    else
                    {
                        return true; /* NOTE: Fake success. */
                    }
                }
                else
                {
                    return true; /* BUGFIX: Fake success. */
                }
            }
            catch (Exception e)
            {
                //
                // NOTE: Something failed, just return false.
                //
                TraceOps.DebugTrace(
                    e, typeof(Console).Name,
                    TracePriority.HostError);
            }

            return false;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Installs a custom icon on the specified console window, saving the
        /// original icons so that they can be restored later.
        /// </summary>
        /// <param name="handle">
        /// The handle of the console window whose icon is being changed.
        /// </param>
        /// <param name="stream">
        /// The stream containing the icon to install.
        /// </param>
        private static void InstallIcon(
            IntPtr handle, /* in */
            Stream stream  /* in */
            )
        {
            lock (staticSyncRoot) /* TRANSACTIONAL */
            {
                if (icon != null)
                {
                    icon.Dispose();
                    icon = null;
                }

                icon = new Icon(stream);

                /* IGNORED */
                WindowOps.GetIcons(
                    handle, out oldSmallIcon, out oldBigIcon);

                /* IGNORED */
                WindowOps.SetIcons(handle, icon.Handle);
            }
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Restores the original icons on the specified console window and
        /// disposes the custom icon, if any.
        /// </summary>
        /// <param name="handle">
        /// The handle of the console window whose icon is being restored.
        /// </param>
        private static void UninstallIcon(
            IntPtr handle /* in */
            )
        {
            lock (staticSyncRoot) /* TRANSACTIONAL */
            {
                /* IGNORED */
                WindowOps.SetIcons(
                    handle, oldSmallIcon, oldBigIcon);

                oldSmallIcon = IntPtr.Zero;
                oldBigIcon = IntPtr.Zero;

                if (icon != null)
                {
                    icon.Dispose();
                    icon = null;
                }
            }
        }
#endif
        #endregion

        ///////////////////////////////////////////////////////////////////////////////////////////////

        #region Native Console Mode Handling
#if NATIVE && WINDOWS
        /// <summary>
        /// Sets up or tears down the native console input mode, disabling mouse
        /// input during setup so that right-click works as expected.
        /// </summary>
        /// <param name="setup">
        /// Non-zero to set up the console mode; zero to tear it down.
        /// </param>
        /// <returns>
        /// True if the operation succeeded; otherwise, false.
        /// </returns>
        private bool SetupMode(
            bool setup
            )
        {
            if (IsOpen())
            {
                //
                // HACK: Disable this flag so that right-click works properly in
                //       the shell (i.e. it brings up the context menu, just like
                //       it does by default in cmd.exe).
                //
                uint mode = NativeConsole.UnsafeNativeMethods.ENABLE_MOUSE_INPUT;

                if (PrivateChangeMode(
                        ChannelType.Input, !setup, mode) != ReturnCode.Ok)
                {
                    return false;
                }
            }

            return true; // NOTE: Fake success.
        }
#endif
        #endregion

        ///////////////////////////////////////////////////////////////////////////////////////////////

        #region Global Setup Methods
        /// <summary>
        /// Determines whether the shared console customizations should be set up
        /// or torn down, based on the reference count and any explicit override,
        /// and marks the setup state accordingly.
        /// </summary>
        /// <param name="newReferenceCount">
        /// The updated console host reference count.
        /// </param>
        /// <param name="setup">
        /// Non-zero if setup is being requested; zero if teardown is being
        /// requested.
        /// </param>
        /// <param name="force">
        /// Non-zero to ignore the reference count; otherwise, zero.
        /// </param>
        /// <returns>
        /// True if the caller should perform the setup or teardown; otherwise,
        /// false.
        /// </returns>
        protected virtual bool ShouldSetup(
            int newReferenceCount,
            bool setup,
            bool force
            )
        {
            bool result = false;
            bool isSetup = false; /* TRACE ONLY */
            bool markSetup = false;

            try
            {
                if (!CommonOps.Environment.DoesVariableExist(
                        EnvVars.NoConsoleSetup))
                {
                    isSetup = ConsoleOps.IsSetup(); /* TRACE ONLY */

                    if (setup)
                    {
                        if (force || (newReferenceCount == 1))
                        {
                            markSetup = ConsoleOps.MarkSetup(setup);

                            if (markSetup)
                                result = true;
                        }
                    }
                    else
                    {
                        if (force || (newReferenceCount <= 0))
                        {
                            markSetup = ConsoleOps.MarkSetup(setup);

                            if (markSetup)
                                result = true;
                        }
                    }
                }
                else
                {
                    result = false;
                }

                return result;
            }
            finally
            {
                TraceOps.DebugTrace(String.Format(
                    "ShouldSetup: newReferenceCount = {0}, setup = {1}, " +
                    "force = {2}, isSetup = {3}, markSetup = {4}, result = {5}",
                    newReferenceCount, setup, force, isSetup, markSetup, result),
                    typeof(Console).Name, TracePriority.HostDebug);
            }
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Performs the one-time setup or teardown of the shared console
        /// customizations (title, icon, mode, and cancel keypress handler),
        /// coordinated via the reference count.
        /// </summary>
        /// <param name="host">
        /// The console host performing the setup or teardown.  This parameter
        /// may be null.
        /// </param>
        /// <param name="setup">
        /// Non-zero to set up the customizations; zero to tear them down.
        /// </param>
        /// <param name="force">
        /// Non-zero to ignore the reference count; otherwise, zero.
        /// </param>
        /// <returns>
        /// True if the operation succeeded; otherwise, false.
        /// </returns>
        private static bool Setup(
            Console host,
            bool setup,
            bool force
            )
        {
            if (setup)
            {
                if (host != null)
                {
                    int newReferenceCount = Interlocked.Increment(
                        ref referenceCount);

                    if (host.ShouldSetup(newReferenceCount, setup, force))
                    {
                        TraceOps.DebugTrace(String.Format(
                            "Setup: INITIALIZING, newReferenceCount = {0}, " +
                            "setup = {1}, force = {2}", newReferenceCount, setup,
                            force), typeof(Console).Name, TracePriority.HostDebug);

                        bool result = true;

                        if (!host.SetupTitle(true))
                            result = false;

#if NATIVE && WINDOWS && DRAWING && !NET_STANDARD_20
                        if (!host.SetupIcon())
                            result = false;
#endif

#if NATIVE && WINDOWS
                        if (NativeConsole.IsSupported() && !host.SetupMode(true))
                            result = false;
#endif

                        if (!host.SetupCancelKeyPressHandler(
                                true, defaultForceAppDomain,
                                defaultForcePending))
                        {
                            result = false;
                        }

                        if (force)
                        {
                            //
                            // NOTE: When the caller forces the reference
                            //       count to be ignored, undo the initial
                            //       increment.
                            //
                            /* IGNORED */
                            Interlocked.Decrement(ref referenceCount);
                        }

                        return result;
                    }
                    else
                    {
                        return true;
                    }
                }
            }
            else
            {
                if (host != null)
                {
                    int newReferenceCount = Interlocked.Decrement(
                        ref referenceCount);

                    if (host.ShouldSetup(newReferenceCount, setup, force))
                    {
                        TraceOps.DebugTrace(String.Format(
                            "Setup: UNINITIALIZING, newReferenceCount = {0}, " +
                            "setup = {1}, force = {2}", newReferenceCount, setup,
                            force), typeof(Console).Name, TracePriority.HostDebug);

                        bool result = true;

                        if (!host.SetupCancelKeyPressHandler(
                                false, defaultForceAppDomain,
                                defaultForcePending))
                        {
                            result = false;
                        }

#if NATIVE && WINDOWS
                        if (NativeConsole.IsSupported() && !host.SetupMode(false))
                            result = false;
#endif

#if NATIVE && WINDOWS && DRAWING && !NET_STANDARD_20
                        if (!host.SetupIcon(false, null))
                            result = false;
#endif

                        if (!host.SetupTitle(false))
                            result = false;

                        if (force)
                        {
                            //
                            // NOTE: When the caller forces the reference
                            //       count to be ignored, undo the initial
                            //       decrement.
                            //
                            /* IGNORED */
                            Interlocked.Increment(ref referenceCount);
                        }

                        return result;
                    }
                    else
                    {
                        return true;
                    }
                }
            }

            return false;
        }
        #endregion
        #endregion

        ///////////////////////////////////////////////////////////////////////////////////////////////

        #region Console Test Mode Handling
        /// <summary>
        /// Enables or disables console test mode, adjusting the host flags and
        /// host test flags accordingly.
        /// </summary>
        /// <param name="enable">
        /// Non-zero to enable test mode; zero to disable it.
        /// </param>
        internal void EnableTests(
            bool enable
            )
        {
            hostFlags = MaybeInitializeHostFlags();
            hostTestFlags = GetTestFlags();

            if (enable)
            {
                //
                // NOTE: Enable test mode.
                //
                hostFlags |= HostFlags.Test;

                //
                // NOTE: Enable each of the individual tests.
                //
                hostTestFlags |= HostTestFlags.CustomInfo;
            }
            else
            {
                //
                // NOTE: Disable test mode.
                //
                hostFlags &= ~HostFlags.Test;

                //
                // NOTE: Disable each of the individual tests.
                //
                hostTestFlags &= ~HostTestFlags.CustomInfo;
            }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////////////////////////////

        #region Console Read Cancellation Handling
        #region Read Cancellation Properties
        /// <summary>
        /// The number of outstanding requests to cancel a pending console read.
        /// When greater than zero, in-progress reads are considered canceled.
        /// </summary>
        private int cancelReadLevels;

        /// <summary>
        /// Gets the number of outstanding requests to cancel a pending console
        /// read.
        /// </summary>
        protected internal virtual int CancelReadLevels
        {
            get
            {
                // CheckDisposed();

                return Interlocked.CompareExchange(
                    ref cancelReadLevels, 0, 0);
            }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////////////////////////////

        #region Check Read Cancellation
        /// <summary>
        /// Determines whether a pending console read has been canceled.
        /// </summary>
        /// <returns>
        /// True if a console read has been canceled; otherwise, false.
        /// </returns>
        protected virtual bool WasReadCanceled()
        {
            return Interlocked.CompareExchange(
                ref cancelReadLevels, 0, 0) > 0;
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////////////////////////////

        #region Reset Read Cancellation
        /// <summary>
        /// Resets the console read cancellation state so that subsequent reads
        /// are not considered canceled.
        /// </summary>
        protected virtual void ResetCancelRead()
        {
            // CheckDisposed();

            /* IGNORED */
            Interlocked.Exchange(ref cancelReadLevels, 0);
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////////////////////////////

        #region Initiate Read Cancellation
        /// <summary>
        /// Requests cancellation of any pending console read.
        /// </summary>
        protected virtual void CancelRead()
        {
            // CheckDisposed();

            Interlocked.Increment(ref cancelReadLevels);
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////////////////////////////

        #region Read / ReadLine Mutators
        /// <summary>
        /// Clears the value read from the console if the read has been canceled.
        /// </summary>
        /// <param name="value">
        /// The value read from the console; it is set to null if the read has
        /// been canceled.  This parameter may be null.
        /// </param>
        protected virtual void GetValueForRead(
            ref string value /* in, out */
            )
        {
            if (WasReadCanceled())
                value = null;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Clears the value read from the console if the read has been canceled.
        /// </summary>
        /// <param name="value">
        /// The value read from the console; it is set to null if the read has
        /// been canceled.  This parameter may be null.
        /// </param>
        protected virtual void GetValueForRead(
            ref int? value /* in, out */
            )
        {
            if (WasReadCanceled())
                value = null;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Clears the key information read from the console if the read has been
        /// canceled.
        /// </summary>
        /// <param name="value">
        /// The key information read from the console; it is set to null if the
        /// read has been canceled.  This parameter may be null.
        /// </param>
        [Obsolete()]
        protected virtual void GetValueForRead(
            ref ConsoleKeyInfo? value /* in, out */
            )
        {
            if (WasReadCanceled())
                value = null;
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////////////////////////////

        #region Read / ReadLine Echo Helper Methods
        /// <summary>
        /// Echoes a string value that was read from the console back to the
        /// console output.
        /// </summary>
        /// <param name="value">
        /// The string value to echo.  This parameter may be null.
        /// </param>
        /// <param name="newLine">
        /// Non-zero to append a line terminator after the value; otherwise,
        /// zero.
        /// </param>
        protected virtual void EchoValueForRead(
            string value, /* in */
            bool newLine  /* in */
            )
        {
            PrivateWrite(value, newLine); /* throw */
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Echoes an integer character value that was read from the console back
        /// to the console output.
        /// </summary>
        /// <param name="value">
        /// The integer character value to echo.
        /// </param>
        /// <param name="newLine">
        /// Non-zero to append a line terminator after the value; otherwise,
        /// zero.
        /// </param>
        protected virtual void EchoValueForRead(
            int value,   /* in */
            bool newLine /* in */
            )
        {
            char character = Convert.ToChar(value); /* throw */

            PrivateWrite(character, newLine); /* throw */
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Echoes a console key, including its modifiers, that was read from the
        /// console back to the console output, skipping non-printable
        /// characters.
        /// </summary>
        /// <param name="value">
        /// The console key information to echo.
        /// </param>
        /// <param name="newLine">
        /// Non-zero to append a line terminator after the key; otherwise, zero.
        /// </param>
        [Obsolete()]
        protected virtual void EchoValueForRead(
            ConsoleKeyInfo value, /* in */
            bool newLine          /* in */
            )
        {
            char character = value.KeyChar;

            //
            // NOTE: Skip attempting to print any key character that
            //       is not actually printable.
            //
            if (!StringOps.CharIsPrint(character))
                return;

            //
            // HACK: Print the modifiers, if any, in order from the
            //       most to least "significant" (i.e. those with a
            //       higher numeric value will be printed first).
            //
            ConsoleModifiers modifiers = value.Modifiers;

            if (FlagOps.HasFlags(
                    modifiers, ConsoleModifiers.Control, true))
            {
                PrivateWrite(String.Format(
                    ModifierEchoFormat, ConsoleModifiers.Control,
                    ModifierEchoSeparator), false); /* throw */
            }

            if (FlagOps.HasFlags(
                    modifiers, ConsoleModifiers.Shift, true))
            {
                PrivateWrite(String.Format(
                    ModifierEchoFormat, ConsoleModifiers.Shift,
                    ModifierEchoSeparator), false); /* throw */
            }

            if (FlagOps.HasFlags(
                    modifiers, ConsoleModifiers.Alt, true))
            {
                PrivateWrite(String.Format(
                    ModifierEchoFormat, ConsoleModifiers.Alt,
                    ModifierEchoSeparator), false); /* throw */
            }

            PrivateWrite(character, newLine); /* throw */
        }
        #endregion
        #endregion

        ///////////////////////////////////////////////////////////////////////////////////////////////

        #region Console Pending Reads/Writes Handling
        /// <summary>
        /// The number of console read operations currently in progress across
        /// all instances sharing the system console.
        /// </summary>
        private static int sharedReadLevels;

        /// <summary>
        /// Gets the number of console read operations currently in progress
        /// across all instances sharing the system console.
        /// </summary>
        protected internal virtual int SharedReadLevels
        {
            get
            {
                // CheckDisposed();

                int localReadLevels = Interlocked.CompareExchange(
                    ref sharedReadLevels, 0, 0);

                return localReadLevels;
            }
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// The number of console write operations currently in progress across
        /// all instances sharing the system console.
        /// </summary>
        private static int sharedWriteLevels;

        /// <summary>
        /// Gets the number of console write operations currently in progress
        /// across all instances sharing the system console.
        /// </summary>
        protected internal virtual int SharedWriteLevels
        {
            get
            {
                // CheckDisposed();

                int localWriteLevels = Interlocked.CompareExchange(
                    ref sharedWriteLevels, 0, 0);

                return localWriteLevels;
            }
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Verifies that there are no pending console reads or writes (local or
        /// shared) and that the console is not in use by other application
        /// domains, so that the console may be safely closed.
        /// </summary>
        /// <param name="error">
        /// Upon failure, receives a message describing why the console cannot be
        /// closed.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> if there are no pending operations;
        /// otherwise, <see cref="ReturnCode.Error" />.
        /// </returns>
        protected virtual ReturnCode CheckActiveReadsAndWrites(
            ref Result error
            )
        {
            // CheckDisposed();

            int localReadLevels = ReadLevels;

            if (localReadLevels > 0)
            {
                error = String.Format(
                    "cannot close console, there are {0} local reads pending",
                    localReadLevels);

                return ReturnCode.Error;
            }

            localReadLevels = SharedReadLevels;

            if (localReadLevels > 0)
            {
                error = String.Format(
                    "cannot close console, there are {0} shared reads pending",
                    localReadLevels);

                return ReturnCode.Error;
            }

            int localWriteLevels = WriteLevels;

            if (localWriteLevels > 0)
            {
                error = String.Format(
                    "cannot close console, there are {0} local writes pending",
                    localWriteLevels);

                return ReturnCode.Error;
            }

            localWriteLevels = SharedWriteLevels;

            if (localWriteLevels > 0)
            {
                error = String.Format(
                    "cannot close console, there are {0} shared writes pending",
                    localWriteLevels);

                return ReturnCode.Error;
            }

            if (ConsoleOps.IsShared())
            {
                error = "cannot close console, it may be in use by other application domains";
                return ReturnCode.Error;
            }

            return ReturnCode.Ok;
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////////////////////////////

        #region Native Console Handling
        /// <summary>
        /// Reports a native console error via the complaint subsystem, but only
        /// when there is a result and verbose mode is enabled.
        /// </summary>
        /// <param name="code">
        /// The return code associated with the error.
        /// </param>
        /// <param name="result">
        /// The result describing the error.  This parameter may be null, in
        /// which case nothing is reported.
        /// </param>
        private void MaybeComplain(
            ReturnCode code,
            Result result
            )
        {
            if (result == null)
                return;

            if (!IsVerboseMode())
                return;

            DebugOps.Complain(
                InternalSafeGetInterpreter(false), code, result);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        #region Native Console Prompt Handling
        /// <summary>
        /// Gets the prompt flags from the specified interpreter, falling back to
        /// the default prompt flags when unavailable.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter whose prompt flags are retrieved.  This parameter may
        /// be null.
        /// </param>
        /// <returns>
        /// The prompt flags for the interpreter, or the default prompt flags.
        /// </returns>
        protected virtual PromptFlags GetPromptFlags(
            Interpreter interpreter /* in */
            )
        {
#if SHELL
            if (interpreter != null)
            {
                bool locked = false;

                try
                {
                    interpreter.InternalSoftTryLock(
                        ref locked); /* TRANSACTIONAL */

                    if (locked)
                        return interpreter.InternalPromptFlags;
                }
                finally
                {
                    interpreter.InternalExitLock(
                        ref locked); /* TRANSACTIONAL */
                }
            }
#endif

            return PromptFlags.Default;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Gets the default interactive start prompt for the current
        /// interpreter, if any.
        /// </summary>
        /// <returns>
        /// The prompt string, or null if no interpreter is available.
        /// </returns>
        protected virtual string MaybeGetPrompt()
        {
            Interpreter interpreter = SafeGetInterpreter();

            if (interpreter == null)
                return null;

            PromptFlags promptFlags = GetPromptFlags(interpreter);
            long id = 0;

            HostOps.MaybeAdjustPromptFlags(
                interpreter, ref promptFlags, ref id);

            return HostOps.GetDefaultPrompt(
                interpreter, PromptType.Start, promptFlags,
                id, interpreter.TotalInteractiveInputs);
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////////////////////////////

        #region Native Console Open/Close Handling
#if NATIVE && WINDOWS
        /// <summary>
        /// Attaches to an existing native console or opens a new one, and
        /// prevents the console window from being closed when appropriate.
        /// </summary>
        /// <param name="force">
        /// Non-zero to force the attach or open operation; otherwise, zero.
        /// </param>
        /// <param name="attach">
        /// Non-zero to attach to an existing console; zero to open a new one.
        /// </param>
        /// <param name="error">
        /// Upon failure, receives information about the error encountered.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise,
        /// <see cref="ReturnCode.Error" />.
        /// </returns>
        private ReturnCode PrivateAttachOrOpen(
            bool force,
            bool attach,
            ref Result error
            )
        {
            if (NativeConsole.IsSupported())
            {
                ReturnCode code;
                bool? attached = null;
                Result localError; /* REUSED */
                ResultList errors = null;

                localError = null;

                code = NativeConsole.AttachOrOpen(
                    force, attach, ref attached,
                    ref localError);

                if (code != ReturnCode.Ok)
                {
                    if (localError != null)
                    {
                        if (errors == null)
                            errors = new ResultList();

                        errors.Add(localError);
                    }
                }

                if ((code == ReturnCode.Ok) &&
                    NativeConsole.ShouldPreventClose(attached))
                {
                    localError = null;

                    code = NativeConsole.PreventClose(
                        ref localError);

                    if (code != ReturnCode.Ok)
                    {
                        if (localError != null)
                        {
                            if (errors == null)
                                errors = new ResultList();

                            errors.Add(localError);
                        }
                    }
                }

                localError = errors;

                if (code != ReturnCode.Ok)
                    MaybeComplain(code, localError);

                error = localError;
                return code;
            }
            else
            {
                error = "not implemented";
                return ReturnCode.Error;
            }
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Closes the native console standard input handle, e.g. to break out of
        /// a blocking read.
        /// </summary>
        /// <param name="error">
        /// Upon failure, receives information about the error encountered.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise,
        /// <see cref="ReturnCode.Error" />.
        /// </returns>
        private ReturnCode PrivateCloseStandardInput(
            ref Result error
            )
        {
            if (NativeConsole.IsSupported())
            {
                ReturnCode code;

                code = NativeConsole.CloseStandardInput(ref error);

                if (code != ReturnCode.Ok)
                    MaybeComplain(code, error);

                return code;
            }
            else
            {
                error = "not implemented";
                return ReturnCode.Error;
            }
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Closes the native console.
        /// </summary>
        /// <param name="error">
        /// Upon failure, receives information about the error encountered.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise,
        /// <see cref="ReturnCode.Error" />.
        /// </returns>
        private ReturnCode PrivateClose(
            ref Result error
            )
        {
            if (NativeConsole.IsSupported())
            {
                ReturnCode code;

                code = NativeConsole.Close(ref error);

                if (code != ReturnCode.Ok)
                    MaybeComplain(code, error);

                return code;
            }
            else
            {
                error = "not implemented";
                return ReturnCode.Error;
            }
        }
#endif
        #endregion

        ///////////////////////////////////////////////////////////////////////////////////////////////

        #region Native Console Size Handling
#if NATIVE && WINDOWS
        /// <summary>
        /// Gets the largest possible native console window size.
        /// </summary>
        /// <param name="width">
        /// Upon success, receives the largest window width, in columns.
        /// </param>
        /// <param name="height">
        /// Upon success, receives the largest window height, in rows.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise,
        /// <see cref="ReturnCode.Error" />.
        /// </returns>
        private ReturnCode PrivateGetLargestWindowSize(
            ref int width,
            ref int height
            )
        {
            ReturnCode code;
            Result error = null;

            if (NativeConsole.IsSupported())
            {
                code = NativeConsole.GetLargestWindowSize(
                    ref width, ref height, ref error);
            }
            else
            {
                error = "not implemented";
                code = ReturnCode.Error;
            }

            if (code != ReturnCode.Ok)
                MaybeComplain(code, error);

            return code;
        }
#endif
        #endregion

        ///////////////////////////////////////////////////////////////////////////////////////////////

        #region Native Console Mode Handling
#if NATIVE && WINDOWS
        /// <summary>
        /// Gets the native console mode flags for the specified channel.
        /// </summary>
        /// <param name="channelType">
        /// The console channel whose mode is retrieved.
        /// </param>
        /// <param name="mode">
        /// Upon success, receives the mode flags for the channel.
        /// </param>
        /// <param name="error">
        /// Upon failure, receives information about the error encountered.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise,
        /// <see cref="ReturnCode.Error" />.
        /// </returns>
        private ReturnCode PrivateGetMode(
            ChannelType channelType,
            ref uint mode,
            ref Result error
            )
        {
            if (NativeConsole.IsSupported())
            {
                ReturnCode code = NativeConsole.GetMode(
                    channelType, ref mode, ref error);

                if (code != ReturnCode.Ok)
                    MaybeComplain(code, error);

                return code;
            }
            else
            {
                error = "not implemented";
                return ReturnCode.Error;
            }
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Sets the native console mode flags for the specified channel.
        /// </summary>
        /// <param name="channelType">
        /// The console channel whose mode is set.
        /// </param>
        /// <param name="mode">
        /// The mode flags to set for the channel.
        /// </param>
        /// <param name="error">
        /// Upon failure, receives information about the error encountered.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise,
        /// <see cref="ReturnCode.Error" />.
        /// </returns>
        private ReturnCode PrivateSetMode(
            ChannelType channelType,
            uint mode,
            ref Result error
            )
        {
            if (NativeConsole.IsSupported())
            {
                ReturnCode code = NativeConsole.SetMode(
                    channelType, mode, ref error);

                if (code != ReturnCode.Ok)
                    MaybeComplain(code, error);

                return code;
            }
            else
            {
                error = "not implemented";
                return ReturnCode.Error;
            }
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Enables or disables the specified native console mode flags for the
        /// input channel.
        /// </summary>
        /// <param name="channelType">
        /// The console channel whose mode is changed.
        /// </param>
        /// <param name="enable">
        /// Non-zero to enable the specified mode flags; zero to disable them.
        /// </param>
        /// <param name="mode">
        /// The mode flags to enable or disable.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise,
        /// <see cref="ReturnCode.Error" />.
        /// </returns>
        private ReturnCode PrivateChangeMode(
            ChannelType channelType,
            bool enable,
            uint mode
            )
        {
            ReturnCode code;
            Result error = null;

            if (NativeConsole.IsSupported())
            {
                code = NativeConsole.ChangeMode(
                    ChannelType.Input, enable, mode, ref error);
            }
            else
            {
                error = "not implemented";
                code = ReturnCode.Error;
            }

            if (code != ReturnCode.Ok)
                MaybeComplain(code, error);

            return code;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Enables or disables the specified native console mode flags when the
        /// console is open.
        /// </summary>
        /// <param name="channelType">
        /// The console channel whose mode is changed.
        /// </param>
        /// <param name="enable">
        /// Non-zero to enable the specified mode flags; zero to disable them.
        /// </param>
        /// <param name="mode">
        /// The mode flags to enable or disable.
        /// </param>
        /// <returns>
        /// True if the mode was changed successfully; otherwise, false.
        /// </returns>
        protected virtual bool ChangeMode( /* NOT USED? */
            ChannelType channelType,
            bool enable,
            uint mode
            )
        {
            if (IsOpen() &&
                (PrivateChangeMode(channelType, enable, mode) == ReturnCode.Ok))
            {
                return true;
            }

            return false;
        }
#endif
        #endregion

        ///////////////////////////////////////////////////////////////////////////////////////////////

        #region Native Console Window Handling
#if NATIVE && WINDOWS
        /// <summary>
        /// Determines whether the native console has been marked as closed.
        /// </summary>
        /// <returns>
        /// True if the console has been marked as closed; otherwise, false.
        /// </returns>
        private static bool WasConsoleClosed()
        {
            return Interlocked.CompareExchange(
                ref closeCount, 0, 0) > 0;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Increments the count of outstanding requests to treat the native
        /// console as closed.
        /// </summary>
        /// <returns>
        /// The updated close count.
        /// </returns>
        private static int BumpConsoleClosed()
        {
            return Interlocked.Increment(ref closeCount);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Decrements the count of outstanding requests to treat the native
        /// console as closed.
        /// </summary>
        /// <returns>
        /// The updated close count.
        /// </returns>
        private static int UnbumpConsoleClosed()
        {
            return Interlocked.Decrement(ref closeCount);
        }
#endif
        #endregion

        ///////////////////////////////////////////////////////////////////////////////////////////////

        #region Native Console Input Handling
#if NATIVE && WINDOWS
        /// <summary>
        /// Flushes the native console input buffer, discarding any pending
        /// input.
        /// </summary>
        /// <param name="error">
        /// Upon failure, receives information about the error encountered.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise,
        /// <see cref="ReturnCode.Error" />.
        /// </returns>
        private ReturnCode PrivateFlushInputBuffer(
            ref Result error
            )
        {
            if (NativeConsole.IsSupported())
            {
                ReturnCode code;

                code = NativeConsole.FlushInputBuffer(ref error);

                if (code != ReturnCode.Ok)
                    MaybeComplain(code, error);

                return code;
            }
            else
            {
                error = "not implemented";
                return ReturnCode.Error;
            }
        }
#endif
        #endregion

        ///////////////////////////////////////////////////////////////////////////////////////////////

        #region Native Console CancelKeyPress Handling
#if NATIVE
        /// <summary>
        /// Forces script cancellation by raising a native console signal (e.g.
        /// simulating Ctrl-C).
        /// </summary>
        /// <param name="noCancel">
        /// Non-zero to raise the signal without triggering script cancellation;
        /// otherwise, zero.
        /// </param>
        /// <param name="error">
        /// Upon failure, receives information about the error encountered.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise,
        /// <see cref="ReturnCode.Error" />.
        /// </returns>
        private static ReturnCode PrivateForceCancel(
            bool noCancel,
            ref Result error
            )
        {
            int result;
            Result localError = null;

            result = noCancel ?
                NativeOps.RaiseConsoleSignalNoCancel(
                    forceCancelTimeout, ref localError) :
                NativeOps.RaiseConsoleSignal(ref localError);

            if (result == 0)
            {
                return ReturnCode.Ok;
            }
            else
            {
                if (localError != null)
                    error = localError;
                else
                    error = NativeOps.GetErrorMessage();

                TraceOps.DebugTrace(String.Format(
                    "PrivateForceCancel: result = {0}, error = {1}",
                    result, FormatOps.WrapOrNull(true, true, error)),
                    typeof(Console).Name, TracePriority.HostError);
            }

            return ReturnCode.Error;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Attempts to break out of a synchronous console read, optionally
        /// forcing script cancellation first, so that the interactive loop can
        /// observe interpreter state changes.
        /// </summary>
        /// <param name="force">
        /// Non-zero to force script cancellation before simulating input;
        /// otherwise, zero.
        /// </param>
        /// <param name="error">
        /// Upon failure, receives information about the error encountered.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise,
        /// <see cref="ReturnCode.Error" />.
        /// </returns>
        private ReturnCode PrivateCancel(
            bool force,
            ref Result error
            )
        {
            ReturnCode code;

            ///////////////////////////////////////////////////////////////////////////////////////////

            #region Platform Neutral (Native)
            //
            // NOTE: This general idea behind simulating a Ctrl-C event before
            //       simulating the return key (below) is that it will prevent
            //       any existing text that happens to be on the console from
            //       being evaluated.  Experiments indicate that this method
            //       is not 100% reliable; however, a more reliable method
            //       (that will work properly from any thread) is not known.
            //       That being said, when this call is combined with the new
            //       read cancellation handling (see above), it should be very
            //       reliable.
            //
            code = force ? PrivateForceCancel(defaultForceNoCancel, ref error) : ReturnCode.Ok;
            #endregion

            ///////////////////////////////////////////////////////////////////////////////////////////

            #region Windows Specific
#if WINDOWS
            if ((code == ReturnCode.Ok) && PlatformOps.IsWindowsOperatingSystem())
            {
                //
                // NOTE: This is an attempt to "nicely" break out of the
                //       synchronous Console.ReadLine call so that the
                //       interactive loop can realize any changes in the
                //       interpreter state (i.e. has the interpreter been
                //       marked as "exited"?).
                //
                code = WindowOps.SimulateReturnKey(ref error);
            }
#endif
            #endregion

            ///////////////////////////////////////////////////////////////////////////////////////////

            #region Unix Specific
#if UNIX
            if ((code == ReturnCode.Ok) && PlatformOps.IsUnixOperatingSystem())
            {
                //
                // NOTE: This is an attempt to "nicely" break out of the
                //       synchronous Console.ReadLine call so that the
                //       interactive loop can realize any changes in the
                //       interpreter state (i.e. has the interpreter been
                //       marked as "exited"?).
                //
                code = ConsoleOps.SimulateEndOfTransmission(ref error);
            }
#endif
            #endregion

            ///////////////////////////////////////////////////////////////////////////////////////////

            //
            // NOTE: If we encountered an error calling the Win32 API, report
            //       that now.
            //
            if (code != ReturnCode.Ok)
                MaybeComplain(code, error);

            return code;
        }
#endif
        #endregion

        ///////////////////////////////////////////////////////////////////////////////////////////////

        #region Native Console History Handling
#if NATIVE && WINDOWS
        /// <summary>
        /// Clears the native console command history.
        /// </summary>
        /// <param name="error">
        /// Upon failure, receives information about the error encountered.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise,
        /// <see cref="ReturnCode.Error" />.
        /// </returns>
        private ReturnCode PrivateResetHistory(
            ref Result error
            )
        {
            if (NativeConsole.IsSupported())
            {
                ReturnCode code;

                code = NativeConsole.ClearHistory(ref error);

                if (code != ReturnCode.Ok)
                    MaybeComplain(code, error);

                return code;
            }
            else
            {
                error = "not implemented";
                return ReturnCode.Error;
            }
        }
#endif
        #endregion

        ///////////////////////////////////////////////////////////////////////////////////////////////

        #region Native Console Write Handling
#if NATIVE && WINDOWS
        /// <summary>
        /// Determines whether output can be written using the native console
        /// write functions (i.e. native console support is available, native
        /// window output is enabled, and output is not redirected).
        /// </summary>
        /// <returns>
        /// True if native writing is possible; otherwise, false.
        /// </returns>
        private bool CanWriteNative()
        {
            if (!NativeConsole.IsSupported() ||
                !DoesNativeWindows() ||
                IsChannelRedirected(ChannelType.Output))
            {
                return false;
            }

            return true;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Enables or disables writing output using the native console write
        /// functions by adjusting the host flags.
        /// </summary>
        /// <param name="writeNative">
        /// Non-zero to enable native writing; zero to disable it.
        /// </param>
        internal void SetWriteNative(
            bool writeNative
            )
        {
            if (writeNative)
                hostFlags |= HostFlags.NativeWindows;
            else
                hostFlags &= ~HostFlags.NativeWindows;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Writes a value to the console using the native console write
        /// functions, if native writing is possible.
        /// </summary>
        /// <typeparam name="T">
        /// The type of the value to write.
        /// </typeparam>
        /// <param name="value">
        /// The value to write.
        /// </param>
        /// <param name="newLine">
        /// Non-zero to append a line terminator after the value; otherwise,
        /// zero.
        /// </param>
        /// <returns>
        /// True if the value was written natively; otherwise, false.
        /// </returns>
        private bool WriteNative<T>(
            T value,
            bool newLine
            )
        {
            if (!CanWriteNative())
                return false;

            ConsoleOps.WriteNative<T>(value, newLine); /* throw */
            return true;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Writes a line terminator to the console using the native console
        /// write functions, if native writing is possible.
        /// </summary>
        /// <returns>
        /// True if the line terminator was written natively; otherwise, false.
        /// </returns>
        private bool WriteLineNative()
        {
            if (!CanWriteNative())
                return false;

            ConsoleOps.WriteNativeLine(); /* throw */
            return true;
        }
#endif

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Writes a character to the console, preferring the native console
        /// write functions and falling back to the managed console.
        /// </summary>
        /// <param name="value">
        /// The character to write.
        /// </param>
        /// <param name="newLine">
        /// Non-zero to append a line terminator after the character; otherwise,
        /// zero.
        /// </param>
        private void PrivateWrite(
            char value,
            bool newLine
            )
        {
#if NATIVE && WINDOWS
            if (WriteNative<char>(value, newLine)) /* throw */
                return;
#endif

            if (newLine)
                System.Console.WriteLine(value); /* throw */
            else
                System.Console.Write(value); /* throw */
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Writes a string to the console, preferring the native console write
        /// functions and falling back to the managed console.
        /// </summary>
        /// <param name="value">
        /// The string to write.
        /// </param>
        /// <param name="newLine">
        /// Non-zero to append a line terminator after the string; otherwise,
        /// zero.
        /// </param>
        private void PrivateWrite(
            string value,
            bool newLine
            )
        {
#if NATIVE && WINDOWS
            if (WriteNative<string>(value, newLine)) /* throw */
                return;
#endif

            if (newLine)
                System.Console.WriteLine(value); /* throw */
            else
                System.Console.Write(value); /* throw */
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Writes a line terminator to the console, preferring the native
        /// console write functions and falling back to the managed console.
        /// </summary>
        private void PrivateWriteLine()
        {
#if NATIVE && WINDOWS
            if (WriteLineNative()) /* throw */
                return;
#endif

            System.Console.WriteLine(); /* throw */
        }
        #endregion
        #endregion
        #endregion

        ///////////////////////////////////////////////////////////////////////////////////////////////

        #region Content Section Methods
        /// <summary>
        /// Determines whether this console host supports colored output,
        /// returning false when console output is redirected.
        /// </summary>
        /// <returns>
        /// True if colored output is supported; otherwise, false.
        /// </returns>
        protected override bool DoesSupportColor()
        {
            if (SystemConsoleOutputIsRedirected())
                return false;

            return base.DoesSupportColor();
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Determines whether this console host adjusts output colors, returning
        /// false when console output is redirected.
        /// </summary>
        /// <returns>
        /// True if color adjustment is supported; otherwise, false.
        /// </returns>
        protected override bool DoesAdjustColor()
        {
            if (SystemConsoleOutputIsRedirected())
                return false;

            return base.DoesAdjustColor();
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Determines whether this console host supports window and buffer
        /// sizing, returning false when console output is redirected.
        /// </summary>
        /// <returns>
        /// True if sizing is supported; otherwise, false.
        /// </returns>
        protected override bool DoesSupportSizing()
        {
            if (SystemConsoleOutputIsRedirected())
                return false;

            return base.DoesSupportSizing();
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Determines whether this console host supports cursor positioning,
        /// returning false when console output is redirected.
        /// </summary>
        /// <returns>
        /// True if positioning is supported; otherwise, false.
        /// </returns>
        protected override bool DoesSupportPositioning()
        {
            if (SystemConsoleOutputIsRedirected())
                return false;

            return base.DoesSupportPositioning();
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////////////////////////////

        #region IInteractiveHost Members
        /// <summary>
        /// Gets or sets the base title for this host; setting it also refreshes
        /// the console window title.  This property is write-only on this host.
        /// </summary>
        public override string Title
        {
            set
            {
                CheckDisposed();

                base.Title = value;
                RefreshTitle();
            }
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Rebuilds and reapplies the console window title.
        /// </summary>
        /// <returns>
        /// True if the title was refreshed successfully; otherwise, false.
        /// </returns>
        public override bool RefreshTitle()
        {
            CheckDisposed();

            return SetupTitle(true);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Determines whether console input has been redirected.
        /// </summary>
        /// <returns>
        /// True if console input has been redirected; otherwise, false.
        /// </returns>
        public override bool IsInputRedirected()
        {
            CheckDisposed();

#if NATIVE && WINDOWS
            //
            // NOTE: Are there outstanding calls to the NativeConsole.Close
            //       method (i.e. those that have not been matched by calls
            //       to the NativeConsole.Open method)?
            //
            if (WasConsoleClosed())
                return false;
#endif

#if NET_45 || NET_451 || NET_452 || NET_46 || NET_461 || NET_462 || NET_47 || NET_471 || NET_472 || NET_48 || NET_481 || NET_STANDARD_20
            try
            {
                return System.Console.IsInputRedirected;
            }
            catch (Exception e)
            {
                //
                // NOTE: Something failed, just return false.
                //
                TraceOps.DebugTrace(
                    e, typeof(Console).Name,
                    TracePriority.ConsoleError);

                return false;
            }
#else
            try
            {
#if MONO_BUILD
#pragma warning disable 219
#endif
                /* IGNORED */
                bool value = System.Console.KeyAvailable; // NOTE: Flagged by the Mono C# compiler.
#if MONO_BUILD
#pragma warning restore 219
#endif

                //
                // NOTE: If we got this far, input has not been
                //       redirected (i.e. there was no exception
                //       thrown by KeyAvailable).
                //
                return false;
            }
            catch (InvalidOperationException)
            {
                //
                // NOTE: Per MSDN, input is being redirected from
                //       a "file".
                //
                return true;
            }
            catch (Exception e)
            {
                //
                // NOTE: Something failed, just return false.
                //
                TraceOps.DebugTrace(
                    e, typeof(Console).Name,
                    TracePriority.ConsoleError);

                return false;
            }
#endif
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Determines whether the console (including its window) is open.
        /// </summary>
        /// <returns>
        /// True if the console is open; otherwise, false.
        /// </returns>
        public override bool IsOpen()
        {
            CheckDisposed();

            return SystemConsoleIsOpen(true);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Pauses execution until a key is pressed on the console, discarding the
        /// key.
        /// </summary>
        /// <returns>
        /// True if a key was read successfully; otherwise, false.
        /// </returns>
        public override bool Pause()
        {
            CheckDisposed();
            EnterReadLevel();

            try
            {
                SystemConsoleInputMustBeOpen(this); /* throw */
                System.Console.ReadKey(true);

                return true;
            }
            catch (InvalidOperationException)
            {
                SetReadException(true);

                return false;
            }
            catch (ScriptException)
            {
                //
                // NOTE: The console is not open, just return false.
                //
                return false;
            }
            catch (Exception e)
            {
                //
                // NOTE: Something failed, just return false.
                //
                TraceOps.DebugTrace(
                    e, typeof(Console).Name,
                    TracePriority.ConsoleError);

                return false;
            }
            finally
            {
                ExitReadLevel();
            }
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Flushes the console output and error channels.
        /// </summary>
        /// <returns>
        /// True if at least one channel was flushed successfully; otherwise,
        /// false.
        /// </returns>
        public override bool Flush()
        {
            CheckDisposed();
            EnterWriteLevel();

            try
            {
                int count = 0;

                ///////////////////////////////////////////////////////////////

                try
                {
                    SystemConsoleOutputMustBeOpen(this); /* throw */
                    System.Console.Out.Flush(); /* throw */

                    count++;
                }
                catch (IOException)
                {
                    SetWriteException(true);
                }
                catch (ScriptException)
                {
                    // do nothing.
                }
                catch (Exception e)
                {
                    TraceOps.DebugTrace(
                        e, typeof(Console).Name,
                        TracePriority.ConsoleError);
                }

                ///////////////////////////////////////////////////////////////

                try
                {
                    SystemConsoleErrorMustBeOpen(this); /* throw */
                    System.Console.Error.Flush(); /* throw */

                    count++;
                }
                catch (IOException)
                {
                    SetWriteException(true);
                }
                catch (ScriptException)
                {
                    // do nothing.
                }
                catch (Exception e)
                {
                    TraceOps.DebugTrace(
                        e, typeof(Console).Name,
                        TracePriority.ConsoleError);
                }

                ///////////////////////////////////////////////////////////////

                return (count > 0);
            }
            catch (ScriptException) /* REDUNDANT? */
            {
                //
                // NOTE: The console is not open, just return false.
                //
                return false;
            }
            catch (Exception e) /* REDUNDANT? */
            {
                //
                // NOTE: Something failed, just return false.
                //
                TraceOps.DebugTrace(
                    e, typeof(Console).Name,
                    TracePriority.ConsoleError);

                return false;
            }
            finally
            {
                ExitWriteLevel();
            }
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// The cached host flags for this console host, or
        /// <see cref="HostFlags.Invalid" /> if they have not yet been computed.
        /// </summary>
        private HostFlags hostFlags = HostFlags.Invalid;

        /// <summary>
        /// Gets the host flags describing the capabilities of this console host.
        /// </summary>
        /// <returns>
        /// The host flags for this console host.
        /// </returns>
        public override HostFlags GetHostFlags()
        {
            CheckDisposed();

            return MaybeInitializeHostFlags();
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Reads a line of input from the console, optionally echoing it.
        /// </summary>
        /// <param name="value">
        /// Upon success, receives the line of text read from the console.
        /// </param>
        /// <returns>
        /// True if a line was read successfully; otherwise, false.
        /// </returns>
        public override bool ReadLine(
            ref string value
            )
        {
            CheckDisposed();
            EnterReadLevel();

            try
            {
                SystemConsoleInputMustBeOpen(this); /* throw */

                ResetCancelRead();

                string localValue = SystemConsoleReadLine();

                GetValueForRead(ref localValue);

                if (localValue != null)
                {
                    if (Echo)
                        EchoValueForRead(localValue, true);

                    value = localValue;
                    return true;
                }
                else
                {
                    return false;
                }
            }
            catch (IOException)
            {
                SetReadException(true);

                return false;
            }
            catch (ScriptException)
            {
                //
                // NOTE: The console is not open, just return false.
                //
                return false;
            }
            catch (Exception e)
            {
                //
                // NOTE: Something failed, just return false.
                //
                TraceOps.DebugTrace(
                    e, typeof(Console).Name,
                    TracePriority.ConsoleError);

                return false;
            }
            finally
            {
                ExitReadLevel();
            }
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Writes a line terminator to the console output.
        /// </summary>
        /// <returns>
        /// True if the line terminator was written successfully; otherwise,
        /// false.
        /// </returns>
        public override bool WriteLine()
        {
            CheckDisposed();
            EnterWriteLevel();

            try
            {
                SystemConsoleOutputMustBeOpen(this); /* throw */

                PrivateWriteLine(); /* throw */

                return true;
            }
            catch (IOException)
            {
                SetWriteException(true);

                return false;
            }
            catch (ScriptException)
            {
                //
                // NOTE: The console is not open, just return false.
                //
                return false;
            }
            catch (Exception e)
            {
                //
                // NOTE: Something failed, just return false.
                //
                TraceOps.DebugTrace(
                    e, typeof(Console).Name,
                    TracePriority.ConsoleError);

                return false;
            }
            finally
            {
                ExitWriteLevel();
            }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////////////////////////////

        #region IStreamHost Members
        /// <summary>
        /// Gets a new stream over the standard console input.
        /// </summary>
        public override Stream DefaultIn
        {
            get
            {
                CheckDisposed();
                SystemConsoleMustBeOpen(true); /* throw */

                return System.Console.OpenStandardInput();
            }
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Gets a new stream over the standard console output.
        /// </summary>
        public override Stream DefaultOut
        {
            get
            {
                CheckDisposed();
                SystemConsoleMustBeOpen(true); /* throw */

                return System.Console.OpenStandardOutput();
            }
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Gets a new stream over the standard console error.
        /// </summary>
        public override Stream DefaultError
        {
            get
            {
                CheckDisposed();
                SystemConsoleMustBeOpen(true); /* throw */

                return System.Console.OpenStandardError();
            }
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Gets the console input stream; setting it redirects console input to
        /// read from the supplied stream using the current input encoding.
        /// </summary>
        public override Stream In
        {
            get { CheckDisposed(); return GetInputStream(this); }
            set
            {
                CheckDisposed();
                SystemConsoleInputMustBeOpen(this); /* throw */

                System.Console.SetIn(NewStreamReader(
                    value, InputEncoding));
            }
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Gets the console output stream; setting it redirects console output
        /// to write to the supplied stream using the current output encoding.
        /// </summary>
        public override Stream Out
        {
            get { CheckDisposed(); return GetOutputStream(this); }
            set
            {
                CheckDisposed();
                SystemConsoleOutputMustBeOpen(this); /* throw */

                System.Console.SetOut(NewStreamWriter(
                    value, OutputEncoding, DoesAutoFlushWriter()));
            }
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Gets the console error stream; setting it redirects console error
        /// output to write to the supplied stream using the current error
        /// encoding.
        /// </summary>
        public override Stream Error
        {
            get { CheckDisposed(); return GetErrorStream(this); }
            set
            {
                CheckDisposed();
                SystemConsoleErrorMustBeOpen(this); /* throw */

                System.Console.SetError(NewStreamWriter(
                    value, ErrorEncoding, DoesAutoFlushWriter()));
            }
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Gets or sets the encoding used when reading from the console input
        /// channel.
        /// </summary>
        public override Encoding InputEncoding
        {
            get
            {
                CheckDisposed();
                SystemConsoleInputMustBeOpen(this); /* throw */

                return System.Console.InputEncoding;
            }
            set
            {
                CheckDisposed();
                SystemConsoleInputMustBeOpen(this); /* throw */

                System.Console.InputEncoding = value;
            }
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Gets or sets the encoding used when writing to the console output
        /// channel.
        /// </summary>
        public override Encoding OutputEncoding
        {
            get
            {
                CheckDisposed();
                SystemConsoleOutputMustBeOpen(this); /* throw */

                return System.Console.OutputEncoding;
            }
            set
            {
                CheckDisposed();
                SystemConsoleOutputMustBeOpen(this); /* throw */

                System.Console.OutputEncoding = value;
            }
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        //
        // HACK: This uses OutputEncoding since there is no ErrorEncoding
        //       property of the System.Console class.
        //
        /// <summary>
        /// Gets or sets the encoding used when writing to the console error
        /// channel.  Because the underlying console class has no separate error
        /// encoding, this uses the console output encoding.
        /// </summary>
        public override Encoding ErrorEncoding
        {
            get
            {
                CheckDisposed();
                SystemConsoleOutputMustBeOpen(this); /* throw */

                return System.Console.OutputEncoding;
            }
            set
            {
                CheckDisposed();
                SystemConsoleOutputMustBeOpen(this); /* throw */

                System.Console.OutputEncoding = value;
            }
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Resets the console input channel by reapplying its encoding and
        /// re-acquiring the input reader.
        /// </summary>
        /// <returns>
        /// True if the input channel was reset successfully; otherwise, false.
        /// </returns>
        public override bool ResetIn()
        {
            CheckDisposed();

            try
            {
                SystemConsoleInputMustBeOpen(this); /* throw */

                Encoding encoding = System.Console.InputEncoding;
                System.Console.InputEncoding = encoding;

#if MONO_BUILD
#pragma warning disable 219
#endif
                /* IGNORED */
                TextReader value = System.Console.In; // NOTE: Flagged by the Mono C# compiler.
#if MONO_BUILD
#pragma warning restore 219
#endif

                return true;
            }
            catch (ScriptException)
            {
                //
                // NOTE: The console is not open, just return false.
                //
                return false;
            }
            catch (Exception e)
            {
                //
                // NOTE: Something failed, just return false.
                //
                TraceOps.DebugTrace(
                    e, typeof(Console).Name,
                    TracePriority.ConsoleError);

                return false;
            }
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Resets the console output channel by reapplying its encoding and
        /// re-acquiring the output writer.
        /// </summary>
        /// <returns>
        /// True if the output channel was reset successfully; otherwise, false.
        /// </returns>
        public override bool ResetOut()
        {
            CheckDisposed();

            try
            {
                SystemConsoleOutputMustBeOpen(this); /* throw */

                Encoding encoding = System.Console.OutputEncoding;
                System.Console.OutputEncoding = encoding;

#if MONO_BUILD
#pragma warning disable 219
#endif
                /* IGNORED */
                TextWriter value = System.Console.Out; // NOTE: Flagged by the Mono C# compiler.
#if MONO_BUILD
#pragma warning restore 219
#endif

                return true;
            }
            catch (ScriptException)
            {
                //
                // NOTE: The console is not open, just return false.
                //
                return false;
            }
            catch (Exception e)
            {
                //
                // NOTE: Something failed, just return false.
                //
                TraceOps.DebugTrace(
                    e, typeof(Console).Name,
                    TracePriority.ConsoleError);

                return false;
            }
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Resets the console error channel by reapplying the output encoding and
        /// re-acquiring the error writer.
        /// </summary>
        /// <returns>
        /// True if the error channel was reset successfully; otherwise, false.
        /// </returns>
        public override bool ResetError()
        {
            CheckDisposed();

            try
            {
                SystemConsoleErrorMustBeOpen(this); /* throw */

                Encoding encoding = System.Console.OutputEncoding;
                System.Console.OutputEncoding = encoding;

#if MONO_BUILD
#pragma warning disable 219
#endif
                /* IGNORED */
                TextWriter value = System.Console.Error; // NOTE: Flagged by the Mono C# compiler.
#if MONO_BUILD
#pragma warning restore 219
#endif

                return true;
            }
            catch (ScriptException)
            {
                //
                // NOTE: The console is not open, just return false.
                //
                return false;
            }
            catch (Exception e)
            {
                //
                // NOTE: Something failed, just return false.
                //
                TraceOps.DebugTrace(
                    e, typeof(Console).Name,
                    TracePriority.ConsoleError);

                return false;
            }
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Determines whether console output has been redirected.
        /// </summary>
        /// <returns>
        /// True if console output has been redirected; otherwise, false.
        /// </returns>
        public override bool IsOutputRedirected()
        {
            CheckDisposed();

#if NATIVE && WINDOWS
            //
            // NOTE: Are there outstanding calls to the NativeConsole.Close
            //       method (i.e. those that have not been matched by calls
            //       to the NativeConsole.Open method)?
            //
            if (WasConsoleClosed())
                return false;
#endif

#if NET_45 || NET_451 || NET_452 || NET_46 || NET_461 || NET_462 || NET_47 || NET_471 || NET_472 || NET_48 || NET_481 || NET_STANDARD_20
            try
            {
                return System.Console.IsOutputRedirected;
            }
            catch (Exception e)
            {
                //
                // NOTE: Something failed, just return false.
                //
                TraceOps.DebugTrace(
                    e, typeof(Console).Name,
                    TracePriority.ConsoleError);

                return false;
            }
#elif NATIVE && WINDOWS
            return IsChannelRedirected(ChannelType.Output);
#else
            return false; /* NOT YET IMPLEMENTED */
#endif
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Determines whether the console error channel has been redirected.
        /// </summary>
        /// <returns>
        /// True if the console error channel has been redirected; otherwise,
        /// false.
        /// </returns>
        public override bool IsErrorRedirected()
        {
            CheckDisposed();

#if NATIVE && WINDOWS
            //
            // NOTE: Are there outstanding calls to the NativeConsole.Close
            //       method (i.e. those that have not been matched by calls
            //       to the NativeConsole.Open method)?
            //
            if (WasConsoleClosed())
                return false;
#endif

#if NET_45 || NET_451 || NET_452 || NET_46 || NET_461 || NET_462 || NET_47 || NET_471 || NET_472 || NET_48 || NET_481 || NET_STANDARD_20
            try
            {
                return System.Console.IsErrorRedirected;
            }
            catch (Exception e)
            {
                //
                // NOTE: Something failed, just return false.
                //
                TraceOps.DebugTrace(
                    e, typeof(Console).Name,
                    TracePriority.ConsoleError);

                return false;
            }
#elif NATIVE && WINDOWS
            return IsChannelRedirected(ChannelType.Error);
#else
            return false; /* NOT YET IMPLEMENTED */
#endif
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Sets up the console input, output, and error channels.  This is not
        /// implemented on this host.
        /// </summary>
        /// <returns>
        /// True if the channels were set up successfully; otherwise, false.
        /// </returns>
        public override bool SetupChannels()
        {
            CheckDisposed();

            return false; /* NOT IMPLEMENTED */
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////////////////////////////

        #region IDebugHost Members
        /// <summary>
        /// Creates a copy of this console host associated with the specified
        /// interpreter.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter to associate with the new host.  This parameter may
        /// be null.
        /// </param>
        /// <returns>
        /// The newly created console host.
        /// </returns>
        public override IHost Clone(
            Interpreter interpreter
            )
        {
            CheckDisposed();

            return new Console(new HostData(
                Name, Group, Description, ClientData, TypeName, interpreter,
                ResourceManager, Profile, HostCreateFlags));
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// The cached host test flags for this console host, or
        /// <see cref="HostTestFlags.Invalid" /> if they have not yet been
        /// computed.
        /// </summary>
        private HostTestFlags hostTestFlags = HostTestFlags.Invalid;

        /// <summary>
        /// Gets the host test flags for this console host, computing them if
        /// necessary.
        /// </summary>
        /// <returns>
        /// The host test flags for this console host.
        /// </returns>
        public override HostTestFlags GetTestFlags()
        {
            CheckDisposed();

            if (hostTestFlags == HostTestFlags.Invalid)
                hostTestFlags = HostTestFlags.None;

            return hostTestFlags;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Cancels any pending console read and attempts to break out of a
        /// synchronous console read so that interpreter state changes can be
        /// observed.
        /// </summary>
        /// <param name="force">
        /// Non-zero to force script cancellation; otherwise, zero.
        /// </param>
        /// <param name="error">
        /// Upon failure, receives information about the error encountered.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise,
        /// <see cref="ReturnCode.Error" />.
        /// </returns>
        public override ReturnCode Cancel(
            bool force,
            ref Result error
            )
        {
            CheckDisposed();

            ///////////////////////////////////////////////////////////////////////////////////////////

            #region Platform Neutral (Managed)
            //
            // NOTE: Prior to doing anything else, attempt to make sure that any
            //       pending input is discarded by the current calls into Read()
            //       and/or ReadLine(), if any.  This is designed to work on all
            //       supported platforms.
            //
            CancelRead();
            #endregion

            ///////////////////////////////////////////////////////////////////////////////////////////

            #region Platform Neutral (Native)
#if NATIVE
            return PrivateCancel(force, ref error);
#else
            error = "not implemented";
            return ReturnCode.Error;
#endif
            #endregion
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Initiates exit of the interpreter by stopping further activity and
        /// breaking out of any blocking console read.
        /// </summary>
        /// <param name="force">
        /// Non-zero to force the interpreter to stop all activity; otherwise,
        /// zero.
        /// </param>
        /// <param name="error">
        /// Upon failure, receives information about the error encountered.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise,
        /// <see cref="ReturnCode.Error" />.
        /// </returns>
        public override ReturnCode Exit(
            bool force,
            ref Result error
            )
        {
            CheckDisposed();

#if NATIVE && WINDOWS
            try
            {
                Interpreter interpreter = InternalSafeGetInterpreter(false);

                if (interpreter != null)
                {
                    //
                    // NOTE: Stop any further activity in the interpreter.
                    //
                    if (force)
                        interpreter.ExitNoThrow = true;

                    //
                    // NOTE: Bail out of Console.ReadLine, etc.
                    //
                    return PrivateCloseStandardInput(ref error);
                }
                else
                {
                    error = "invalid interpreter";
                }
            }
            catch (Exception e)
            {
                error = e;
            }

            return ReturnCode.Error;
#else
            error = "not implemented";
            return ReturnCode.Error;
#endif
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Writes a line terminator to the debug output, which is forwarded to
        /// the normal console output.
        /// </summary>
        /// <returns>
        /// True if the line terminator was written successfully; otherwise,
        /// false.
        /// </returns>
        public override bool WriteDebugLine()
        {
            CheckDisposed();

            //
            // TODO: We have no dedicated place for debug output;
            //       therefore, just forward it as normal output.
            //
            return WriteLine();
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Writes a character to the debug output, which is forwarded to the
        /// normal console output using the debug colors.
        /// </summary>
        /// <param name="value">
        /// The character to write.
        /// </param>
        /// <param name="newLine">
        /// Non-zero to append a line terminator after the character; otherwise,
        /// zero.
        /// </param>
        /// <returns>
        /// True if the character was written successfully; otherwise, false.
        /// </returns>
        public override bool WriteDebug(
            char value,
            bool newLine
            )
        {
            CheckDisposed();

            //
            // TODO: We have no dedicated place for debug output;
            //       therefore, just forward it as normal output
            //       [with the correct colors].
            //
            return Write(value, 1, newLine, DebugForegroundColor, DebugBackgroundColor);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Writes a string to the debug output, which is forwarded to the normal
        /// console output using the debug colors.
        /// </summary>
        /// <param name="value">
        /// The string to write.
        /// </param>
        /// <param name="newLine">
        /// Non-zero to append a line terminator after the string; otherwise,
        /// zero.
        /// </param>
        /// <returns>
        /// True if the string was written successfully; otherwise, false.
        /// </returns>
        public override bool WriteDebug(
            string value,
            bool newLine
            )
        {
            CheckDisposed();

            //
            // TODO: We have no dedicated place for debug output;
            //       therefore, just forward it as normal output
            //       [with the correct colors].
            //
            return Write(value, newLine, DebugForegroundColor, DebugBackgroundColor);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Writes a line terminator to the console error channel.
        /// </summary>
        /// <returns>
        /// True if the line terminator was written successfully; otherwise,
        /// false.
        /// </returns>
        public override bool WriteErrorLine()
        {
            CheckDisposed();
            EnterWriteLevel();

            try
            {
                SystemConsoleErrorMustBeOpen(this); /* throw */
                System.Console.Error.WriteLine();

                return true;
            }
            catch (IOException)
            {
                SetWriteException(true);

                return false;
            }
            catch (ScriptException)
            {
                //
                // NOTE: The console is not open, just return false.
                //
                return false;
            }
            catch (Exception e)
            {
                //
                // NOTE: Something failed, just return false.
                //
                TraceOps.DebugTrace(
                    e, typeof(Console).Name,
                    TracePriority.ConsoleError);

                return false;
            }
            finally
            {
                ExitWriteLevel();
            }
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Writes a character to the console error channel using the error (or
        /// fatal) colors.
        /// </summary>
        /// <param name="value">
        /// The character to write.
        /// </param>
        /// <param name="newLine">
        /// Non-zero to append a line terminator after the character; otherwise,
        /// zero.
        /// </param>
        /// <returns>
        /// True if the character was written successfully; otherwise, false.
        /// </returns>
        public override bool WriteError(
            char value,
            bool newLine
            )
        {
            CheckDisposed();
            EnterWriteLevel();

            try
            {
                SystemConsoleErrorMustBeOpen(this); /* throw */

                bool isFatal = ShouldTreatAsFatalError();

                return WriteCore(
                    System.Console.Error.Write, System.Console.Error.WriteLine,
                    value, 1, newLine, isFatal ? FatalForegroundColor :
                    ErrorForegroundColor, isFatal ? FatalBackgroundColor :
                    ErrorBackgroundColor);
            }
            catch (ScriptException)
            {
                //
                // NOTE: The console is not open, just return false.
                //
                return false;
            }
            catch (Exception e)
            {
                //
                // NOTE: Something failed, just return false.
                //
                TraceOps.DebugTrace(
                    e, typeof(Console).Name,
                    TracePriority.ConsoleError);

                return false;
            }
            finally
            {
                ExitWriteLevel();
            }
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Writes a string to the console error channel using the error (or
        /// fatal) colors.
        /// </summary>
        /// <param name="value">
        /// The string to write.
        /// </param>
        /// <param name="newLine">
        /// Non-zero to append a line terminator after the string; otherwise,
        /// zero.
        /// </param>
        /// <returns>
        /// True if the string was written successfully; otherwise, false.
        /// </returns>
        public override bool WriteError(
            string value,
            bool newLine
            )
        {
            CheckDisposed();
            EnterWriteLevel();

            try
            {
                SystemConsoleErrorMustBeOpen(this); /* throw */

                bool isFatal = ShouldTreatAsFatalError();

                return WriteCore(
                    System.Console.Error.Write, System.Console.Error.WriteLine,
                    value, newLine, isFatal ? FatalForegroundColor :
                    ErrorForegroundColor, isFatal ? FatalBackgroundColor :
                    ErrorBackgroundColor);
            }
            catch (ScriptException)
            {
                //
                // NOTE: The console is not open, just return false.
                //
                return false;
            }
            catch (Exception e)
            {
                //
                // NOTE: Something failed, just return false.
                //
                TraceOps.DebugTrace(
                    e, typeof(Console).Name,
                    TracePriority.ConsoleError);

                return false;
            }
            finally
            {
                ExitWriteLevel();
            }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////////////////////////////

        #region IInformationHost Members
        /// <summary>
        /// Writes host-specific custom information, used only in test mode by
        /// this host.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter for which the information is written.  This parameter
        /// may be null.
        /// </param>
        /// <param name="detailFlags">
        /// The flags controlling which details are written.
        /// </param>
        /// <param name="newLine">
        /// Non-zero to append a line terminator after the information;
        /// otherwise, zero.
        /// </param>
        /// <param name="foregroundColor">
        /// The foreground color to use when writing the information.
        /// </param>
        /// <param name="backgroundColor">
        /// The background color to use when writing the information.
        /// </param>
        /// <returns>
        /// True if the information was written successfully; otherwise, false.
        /// </returns>
        public override bool WriteCustomInfo(
            Interpreter interpreter,
            DetailFlags detailFlags,
            bool newLine,
            ConsoleColor foregroundColor,
            ConsoleColor backgroundColor
            )
        {
            CheckDisposed();

#if TEST
            if (InTestMode() && HasTestFlags(HostTestFlags.CustomInfo, true))
            {
                return _Tests.Default.TestWriteCustomInfo(
                    interpreter, detailFlags, newLine,
                    foregroundColor, backgroundColor);
            }
#endif

            return true;
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////////////////////////////

        #region IBoxHost Members
        /// <summary>
        /// Begins a visual box (grouping) on the host.  This host does not draw
        /// boxes and always reports success.
        /// </summary>
        /// <param name="name">
        /// The name of the box.  This parameter may be null.
        /// </param>
        /// <param name="list">
        /// The list of name/value pairs to display in the box.  This parameter
        /// may be null.
        /// </param>
        /// <param name="clientData">
        /// The client data associated with the box.  This parameter may be null.
        /// </param>
        /// <returns>
        /// True if the box was begun successfully; otherwise, false.
        /// </returns>
        public override bool BeginBox(
            string name,
            StringPairList list,
            IClientData clientData
            )
        {
            CheckDisposed();

            return true;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Ends a visual box (grouping) on the host.  This host does not draw
        /// boxes and always reports success.
        /// </summary>
        /// <param name="name">
        /// The name of the box.  This parameter may be null.
        /// </param>
        /// <param name="list">
        /// The list of name/value pairs displayed in the box.  This parameter
        /// may be null.
        /// </param>
        /// <param name="clientData">
        /// The client data associated with the box.  This parameter may be null.
        /// </param>
        /// <returns>
        /// True if the box was ended successfully; otherwise, false.
        /// </returns>
        public override bool EndBox(
            string name,
            StringPairList list,
            IClientData clientData
            )
        {
            CheckDisposed();

            return true;
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////////////////////////////

        #region IColorHost Members
        /// <summary>
        /// Resets the console foreground and background colors to their
        /// defaults.
        /// </summary>
        /// <returns>
        /// True if the colors were reset successfully; otherwise, false.
        /// </returns>
        public override bool ResetColors()
        {
            CheckDisposed();

            try
            {
                SystemConsoleMustBeOpen(true); /* throw */
                System.Console.ResetColor();

                return true;
            }
            catch (ScriptException)
            {
                //
                // NOTE: The console is not open, just return false.
                //
                return false;
            }
            catch (Exception e)
            {
                //
                // NOTE: Something failed, just return false.
                //
                TraceOps.DebugTrace(
                    e, typeof(Console).Name,
                    TracePriority.ConsoleError);

                return false;
            }
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Gets the current console foreground and background colors.
        /// </summary>
        /// <param name="foregroundColor">
        /// Upon success, receives the current foreground color.
        /// </param>
        /// <param name="backgroundColor">
        /// Upon success, receives the current background color.
        /// </param>
        /// <returns>
        /// True if the colors were obtained successfully; otherwise, false.
        /// </returns>
        public override bool GetColors(
            ref ConsoleColor foregroundColor,
            ref ConsoleColor backgroundColor
            )
        {
            CheckDisposed();

            try
            {
                SystemConsoleMustBeOpen(true); /* throw */

                foregroundColor = System.Console.ForegroundColor;
                backgroundColor = System.Console.BackgroundColor;

                return true;
            }
            catch (ScriptException)
            {
                //
                // NOTE: The console is not open, just return false.
                //
                return false;
            }
            catch (Exception e)
            {
                //
                // NOTE: Something failed, just return false.
                //
                TraceOps.DebugTrace(
                    e, typeof(Console).Name,
                    TracePriority.ConsoleError);

                return false;
            }
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Adjusts the supplied colors by swapping the foreground and background
        /// colors (i.e. reverse video).
        /// </summary>
        /// <param name="foregroundColor">
        /// On input, the foreground color; on output, the swapped color.
        /// </param>
        /// <param name="backgroundColor">
        /// On input, the background color; on output, the swapped color.
        /// </param>
        /// <returns>
        /// True if the colors were adjusted successfully; otherwise, false.
        /// </returns>
        public override bool AdjustColors(
            ref ConsoleColor foregroundColor,
            ref ConsoleColor backgroundColor
            )
        {
            CheckDisposed();

            //
            // NOTE: This is implemented as "reverse video".
            //
            ConsoleColor color = foregroundColor;
            foregroundColor = backgroundColor;
            backgroundColor = color;

            return true;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Sets the console foreground color, optionally substituting the saved
        /// foreground color when no color is specified.
        /// </summary>
        /// <param name="foregroundColor">
        /// The foreground color to set.
        /// </param>
        /// <returns>
        /// True if the foreground color was set successfully; otherwise, false.
        /// </returns>
        public override bool SetForegroundColor(
            ConsoleColor foregroundColor
            )
        {
            CheckDisposed();

            if (DoesNoSetForegroundColor())
                return true; /* NOTE: Fake success. */

            bool wasChanged = false;
            Result error = null;

            try
            {
                SystemConsoleMustBeOpen(true); /* throw */

                if ((foregroundColor == _ConsoleColor.None) &&
                    DoesSavedColorForNone())
                {
                    bool locked = false;

                    try
                    {
                        TryLockWithWait(
                            ref locked); /* TRANSACTIONAL */

                        if (locked)
                        {
                            foregroundColor =
                                savedForegroundColor;
                        }
                        else
                        {
                            TraceOps.LockTrace(
                                "SetForegroundColor",
                                typeof(Console).Name,
                                false,
                                TracePriority.LockWarning,
                                null);
                        }
                    }
                    finally
                    {
                        ExitLock(
                            ref locked); /* TRANSACTIONAL */
                    }
                }

                if (foregroundColor != _ConsoleColor.None)
                {
                    ConsoleColor oldForegroundColor =
                        System.Console.ForegroundColor;

                    if (foregroundColor != oldForegroundColor)
                    {
                        System.Console.ForegroundColor = foregroundColor;
                        wasChanged = true;

#if DEBUG && VERBOSE
                        TraceOps.DebugTrace(String.Format(
                            "SetForegroundColor: changed from {0} to {1}",
                            oldForegroundColor, foregroundColor),
                            typeof(Console).Name,
                            TracePriority.ConsoleDebug);
#endif
                    }
                }

                return true;
            }
            catch (Exception e)
            {
                error = e;
                return false;
            }
            finally
            {
                if (!wasChanged && DoesTraceColorNotChanged())
                {
                    TraceOps.DebugTrace(String.Format(
                        "SetForegroundColor: change to {0} not done: {1}",
                        foregroundColor, FormatOps.WrapOrNull(error)),
                        typeof(Console).Name, TracePriority.ConsoleError2);
                }
            }
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Sets the console background color, optionally substituting the saved
        /// background color when no color is specified.
        /// </summary>
        /// <param name="backgroundColor">
        /// The background color to set.
        /// </param>
        /// <returns>
        /// True if the background color was set successfully; otherwise, false.
        /// </returns>
        public override bool SetBackgroundColor(
            ConsoleColor backgroundColor
            )
        {
            CheckDisposed();

            if (DoesNoSetBackgroundColor())
                return true; /* NOTE: Fake success. */

            bool wasChanged = false;
            Result error = null;

            try
            {
                SystemConsoleMustBeOpen(true); /* throw */

                if ((backgroundColor == _ConsoleColor.None) &&
                    DoesSavedColorForNone())
                {
                    bool locked = false;

                    try
                    {
                        TryLockWithWait(
                            ref locked); /* TRANSACTIONAL */

                        if (locked)
                        {
                            backgroundColor =
                                savedBackgroundColor;
                        }
                        else
                        {
                            TraceOps.LockTrace(
                                "SetBackgroundColor",
                                typeof(Console).Name,
                                false,
                                TracePriority.LockWarning,
                                null);
                        }
                    }
                    finally
                    {
                        ExitLock(
                            ref locked); /* TRANSACTIONAL */
                    }
                }

                if (backgroundColor != _ConsoleColor.None)
                {
                    ConsoleColor oldBackgroundColor =
                        System.Console.BackgroundColor;

                    if (backgroundColor != oldBackgroundColor)
                    {
                        System.Console.BackgroundColor = backgroundColor;
                        wasChanged = true;

#if DEBUG && VERBOSE
                        TraceOps.DebugTrace(String.Format(
                            "SetBackgroundColor: changed from {0} to {1}",
                            oldBackgroundColor, backgroundColor),
                            typeof(Console).Name,
                            TracePriority.ConsoleDebug);
#endif
                    }
                }

                return true;
            }
            catch (Exception e)
            {
                error = e;
                return false;
            }
            finally
            {
                if (!wasChanged && DoesTraceColorNotChanged())
                {
                    TraceOps.DebugTrace(String.Format(
                        "SetBackgroundColor: change to {0} not done: {1}",
                        backgroundColor, FormatOps.WrapOrNull(error)),
                        typeof(Console).Name, TracePriority.ConsoleError2);
                }
            }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////////////////////////////

        #region IPositionHost Members
        /// <summary>
        /// Gets the current cursor position on the console.
        /// </summary>
        /// <param name="left">
        /// Upon success, receives the cursor column (zero-based).
        /// </param>
        /// <param name="top">
        /// Upon success, receives the cursor row (zero-based).
        /// </param>
        /// <returns>
        /// True if the position was obtained successfully; otherwise, false.
        /// </returns>
        public override bool GetPosition(
            ref int left,
            ref int top
            )
        {
            CheckDisposed();

            try
            {
                SystemConsoleMustBeOpen(true); /* throw */

                left = System.Console.CursorLeft;
                top = System.Console.CursorTop;

                return true;
            }
            catch (ScriptException)
            {
                //
                // NOTE: The console is not open, just return false.
                //
                return false;
            }
            catch (Exception e)
            {
                //
                // NOTE: Something failed, just return false.
                //
                TraceOps.DebugTrace(
                    e, typeof(Console).Name,
                    TracePriority.ConsoleError);

                return false;
            }
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Sets the cursor position on the console, ignoring either coordinate
        /// that is invalid.
        /// </summary>
        /// <param name="left">
        /// The cursor column (zero-based), or an invalid position to leave the
        /// column unchanged.
        /// </param>
        /// <param name="top">
        /// The cursor row (zero-based), or an invalid position to leave the row
        /// unchanged.
        /// </param>
        /// <returns>
        /// True if the position was set successfully; otherwise, false.
        /// </returns>
        public override bool SetPosition(
            int left,
            int top
            )
        {
            CheckDisposed();

            try
            {
                SystemConsoleMustBeOpen(true); /* throw */

                if ((left != _Position.Invalid) && (top != _Position.Invalid))
                    System.Console.SetCursorPosition(left, top);
                else if (left != _Position.Invalid)
                    System.Console.CursorLeft = left;
                else if (top != _Position.Invalid)
                    System.Console.CursorTop = top;

                return true;
            }
            catch (ScriptException)
            {
                //
                // NOTE: The console is not open, just return false.
                //
                return false;
            }
            catch (Exception e)
            {
                //
                // NOTE: Something failed, just return false.
                //
                TraceOps.DebugTrace(
                    e, typeof(Console).Name,
                    TracePriority.ConsoleError);

                return false;
            }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////////////////////////////

        #region ISizeHost Members
        /// <summary>
        /// Restores the console buffer and window sizes to the originally saved
        /// sizes, restoring the prior sizes on failure.
        /// </summary>
        /// <param name="hostSizeType">
        /// The type of size to reset; only the window (current) size is
        /// supported.
        /// </param>
        /// <returns>
        /// True if the size was reset successfully; otherwise, false.
        /// </returns>
        public override bool ResetSize(
            HostSizeType hostSizeType
            )
        {
            CheckDisposed();

            if ((hostSizeType != HostSizeType.Any) &&
                (hostSizeType != HostSizeType.WindowCurrent))
            {
                return false;
            }

            int currentBufferWidth = _Size.Invalid;
            int currentBufferHeight = _Size.Invalid;
            int currentWindowWidth = _Size.Invalid;
            int currentWindowHeight = _Size.Invalid;

            if (!SaveSize(
                    ref currentBufferWidth, ref currentBufferHeight,
                    ref currentWindowWidth, ref currentWindowHeight))
            {
                return false;
            }

            bool result = false;

            try
            {
                //
                // NOTE: Make sure we successfully saved the original buffer
                //       and window sizes earlier.
                //
                lock (syncRoot) /* TRANSACTIONAL */
                {
                    if ((savedBufferWidth != _Size.Invalid) &&
                        (savedBufferHeight != _Size.Invalid) &&
                        (savedWindowWidth != _Size.Invalid) &&
                        (savedWindowHeight != _Size.Invalid))
                    {
                        result = SetSize(
                            savedBufferWidth, savedBufferHeight,
                            savedWindowWidth, savedWindowHeight);
                    }
                }
            }
            catch /* REDUNDANT? */
            {
                // do nothing.
            }
            finally
            {
                //
                // NOTE: *FAIL* Restore the previous buffer and window sizes
                //       (i.e. those that were current at the start of this
                //       method).
                //
                if (!result)
                {
                    if ((currentBufferWidth != _Size.Invalid) &&
                        (currentBufferHeight != _Size.Invalid) &&
                        (currentWindowWidth != _Size.Invalid) &&
                        (currentWindowHeight != _Size.Invalid))
                    {
                        /* IGNORED */
                        SetSize(
                            currentBufferWidth, currentBufferHeight,
                            currentWindowWidth, currentWindowHeight);
                    }
                }
            }

            return result;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Gets the console size of the specified type (buffer or window,
        /// current or maximum).
        /// </summary>
        /// <param name="hostSizeType">
        /// The type of size to get.
        /// </param>
        /// <param name="width">
        /// Upon success, receives the width, in columns.
        /// </param>
        /// <param name="height">
        /// Upon success, receives the height, in rows.
        /// </param>
        /// <returns>
        /// True if the size was obtained successfully; otherwise, false.
        /// </returns>
        public override bool GetSize(
            HostSizeType hostSizeType,
            ref int width,
            ref int height
            )
        {
            CheckDisposed();

            if ((hostSizeType == HostSizeType.BufferCurrent) ||
                (hostSizeType == HostSizeType.BufferMaximum))
            {
                try
                {
                    SystemConsoleMustBeOpen(true); /* throw */

                    width = System.Console.BufferWidth;
                    height = System.Console.BufferHeight;

                    return true;
                }
                catch (ScriptException)
                {
                    //
                    // NOTE: The console is not open, just return false.
                    //
                    return false;
                }
                catch (Exception e)
                {
                    //
                    // NOTE: Something failed, just return false.
                    //
                    TraceOps.DebugTrace(
                        e, typeof(Console).Name,
                        TracePriority.ConsoleError);

                    return false;
                }
            }
            else if ((hostSizeType == HostSizeType.Any) ||
                (hostSizeType == HostSizeType.WindowCurrent))
            {
                try
                {
                    SystemConsoleMustBeOpen(true); /* throw */

                    width = System.Console.WindowWidth;
                    height = System.Console.WindowHeight;

                    return true;
                }
                catch (ScriptException)
                {
                    //
                    // NOTE: The console is not open, just return false.
                    //
                    return false;
                }
                catch (Exception e)
                {
                    //
                    // NOTE: Something failed, just return false.
                    //
                    TraceOps.DebugTrace(
                        e, typeof(Console).Name,
                        TracePriority.ConsoleError);

                    return false;
                }
            }
            else if (hostSizeType == HostSizeType.WindowMaximum)
            {
#if NATIVE && WINDOWS
                ReturnCode code = PrivateGetLargestWindowSize(
                    ref width, ref height);

                if (code == ReturnCode.Ok)
                    return true;
#endif

                return FallbackGetLargestWindowSize(
                    ref width, ref height);

            }
            else
            {
                return false;
            }
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Sets the console size of the specified type (window current or
        /// maximum); setting the buffer size is not supported.
        /// </summary>
        /// <param name="hostSizeType">
        /// The type of size to set.
        /// </param>
        /// <param name="width">
        /// The width to set, in columns.
        /// </param>
        /// <param name="height">
        /// The height to set, in rows.
        /// </param>
        /// <returns>
        /// True if the size was set successfully; otherwise, false.
        /// </returns>
        public override bool SetSize(
            HostSizeType hostSizeType,
            int width,
            int height
            )
        {
            CheckDisposed();

            if ((hostSizeType == HostSizeType.BufferCurrent) ||
                (hostSizeType == HostSizeType.BufferMaximum))
            {
                //
                // TODO: Figure out a clean way to support this.
                //
                return false;
            }
            else if ((hostSizeType == HostSizeType.Any) ||
                (hostSizeType == HostSizeType.WindowCurrent))
            {
                return SetSize(width, height, false);
            }
            else if (hostSizeType == HostSizeType.WindowMaximum)
            {
                return SetSize(width, height, true);
            }
            else
            {
                return false;
            }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////////////////////////////

        #region IReadHost Members
        /// <summary>
        /// Reads a single character from the console input, optionally echoing
        /// it.
        /// </summary>
        /// <param name="value">
        /// Upon success, receives the character read, as an integer.
        /// </param>
        /// <returns>
        /// True if a character was read successfully; otherwise, false.
        /// </returns>
        public override bool Read(
            ref int value
            )
        {
            CheckDisposed();
            EnterReadLevel();

            try
            {
                SystemConsoleInputMustBeOpen(this); /* throw */

                ResetCancelRead();

                int? localValue = System.Console.Read();

                GetValueForRead(ref localValue);

                if (localValue != null)
                {
                    int intValue = (int)localValue;

                    if (Echo)
                        EchoValueForRead(intValue, false);

                    value = intValue;
                    return true;
                }
                else
                {
                    return false;
                }
            }
            catch (IOException)
            {
                SetReadException(true);

                return false;
            }
            catch (ScriptException)
            {
                //
                // NOTE: The console is not open, just return false.
                //
                return false;
            }
            catch (Exception e)
            {
                //
                // NOTE: Something failed, just return false.
                //
                TraceOps.DebugTrace(
                    e, typeof(Console).Name,
                    TracePriority.ConsoleError);

                return false;
            }
            finally
            {
                ExitReadLevel();
            }
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Reads a single key from the console input, optionally echoing it, and
        /// returns the key information wrapped in client data.
        /// </summary>
        /// <param name="intercept">
        /// Non-zero to intercept the key (i.e. not display it); otherwise, zero.
        /// </param>
        /// <param name="value">
        /// Upon success, receives the key information wrapped in client data.
        /// </param>
        /// <returns>
        /// True if a key was read successfully; otherwise, false.
        /// </returns>
        public override bool ReadKey(
            bool intercept,
            ref IClientData value
            )
        {
            CheckDisposed();
            EnterReadLevel();

            try
            {
                SystemConsoleInputMustBeOpen(this); /* throw */

                ResetCancelRead();

                ConsoleKeyInfo? localValue = System.Console.ReadKey(
                    intercept);

                GetValueForRead(ref localValue);

                if (localValue != null)
                {
                    ConsoleKeyInfo keyValue = (ConsoleKeyInfo)localValue;

                    if (Echo)
                        EchoValueForRead(keyValue, false);

                    value = new ClientData(keyValue);
                    return true;
                }
                else
                {
                    return false;
                }
            }
            catch (InvalidOperationException)
            {
                SetReadException(true);

                return false;
            }
            catch (ScriptException)
            {
                //
                // NOTE: The console is not open, just return false.
                //
                return false;
            }
            catch (Exception e)
            {
                //
                // NOTE: Something failed, just return false.
                //
                TraceOps.DebugTrace(
                    e, typeof(Console).Name,
                    TracePriority.ConsoleError);

                return false;
            }
            finally
            {
                ExitReadLevel();
            }
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Reads a single key from the console input, optionally echoing it, and
        /// returns the key information directly.
        /// </summary>
        /// <param name="intercept">
        /// Non-zero to intercept the key (i.e. not display it); otherwise, zero.
        /// </param>
        /// <param name="value">
        /// Upon success, receives the key information.
        /// </param>
        /// <returns>
        /// True if a key was read successfully; otherwise, false.
        /// </returns>
        [Obsolete()]
        public override bool ReadKey(
            bool intercept,
            ref ConsoleKeyInfo value
            )
        {
            CheckDisposed();
            EnterReadLevel();

            try
            {
                SystemConsoleInputMustBeOpen(this); /* throw */

                ResetCancelRead();

                ConsoleKeyInfo? localValue = System.Console.ReadKey(
                    intercept);

                GetValueForRead(ref localValue);

                if (localValue != null)
                {
                    ConsoleKeyInfo keyValue = (ConsoleKeyInfo)localValue;

                    if (Echo)
                        EchoValueForRead(keyValue, false);

                    value = keyValue;
                    return true;
                }
                else
                {
                    return false;
                }
            }
            catch (InvalidOperationException)
            {
                SetReadException(true);

                return false;
            }
            catch (ScriptException)
            {
                //
                // NOTE: The console is not open, just return false.
                //
                return false;
            }
            catch (Exception e)
            {
                //
                // NOTE: Something failed, just return false.
                //
                TraceOps.DebugTrace(
                    e, typeof(Console).Name,
                    TracePriority.ConsoleError);

                return false;
            }
            finally
            {
                ExitReadLevel();
            }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////////////////////////////

        #region IWriteHost Members
        /// <summary>
        /// Writes a character to the console output.
        /// </summary>
        /// <param name="value">
        /// The character to write.
        /// </param>
        /// <param name="newLine">
        /// Non-zero to append a line terminator after the character; otherwise,
        /// zero.
        /// </param>
        /// <returns>
        /// True if the character was written successfully; otherwise, false.
        /// </returns>
        public override bool Write(
            char value,
            bool newLine
            )
        {
            CheckDisposed();
            EnterWriteLevel();

            try
            {
                SystemConsoleOutputMustBeOpen(this); /* throw */

                PrivateWrite(value, newLine);

                return true;
            }
            catch (IOException)
            {
                SetWriteException(true);

                return false;
            }
            catch (ScriptException)
            {
                //
                // NOTE: The console is not open, just return false.
                //
                return false;
            }
            catch (Exception e)
            {
                //
                // NOTE: Something failed, just return false.
                //
                TraceOps.DebugTrace(
                    e, typeof(Console).Name,
                    TracePriority.ConsoleError);

                return false;
            }
            finally
            {
                ExitWriteLevel();
            }
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Writes a string to the console output.
        /// </summary>
        /// <param name="value">
        /// The string to write.
        /// </param>
        /// <param name="newLine">
        /// Non-zero to append a line terminator after the string; otherwise,
        /// zero.
        /// </param>
        /// <returns>
        /// True if the string was written successfully; otherwise, false.
        /// </returns>
        public override bool Write(
            string value,
            bool newLine
            )
        {
            CheckDisposed();
            EnterWriteLevel();

            try
            {
                SystemConsoleOutputMustBeOpen(this); /* throw */

                PrivateWrite(value, newLine);

                return true;
            }
            catch (IOException)
            {
                SetWriteException(true);

                return false;
            }
            catch (ScriptException)
            {
                //
                // NOTE: The console is not open, just return false.
                //
                return false;
            }
            catch (Exception e)
            {
                //
                // NOTE: Something failed, just return false.
                //
                TraceOps.DebugTrace(
                    e, typeof(Console).Name,
                    TracePriority.ConsoleError);

                return false;
            }
            finally
            {
                ExitWriteLevel();
            }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////////////////////////////

        #region IHost Members
        /// <summary>
        /// Builds a list of name/value pairs describing the current state of
        /// this console host, for diagnostic purposes.
        /// </summary>
        /// <param name="detailFlags">
        /// The flags controlling which details are included.
        /// </param>
        /// <returns>
        /// A list of name/value pairs describing the host state.
        /// </returns>
        public override StringList QueryState(
            DetailFlags detailFlags
            )
        {
            CheckDisposed();

            StringList result = new StringList();

            lock (syncRoot) /* TRANSACTIONAL */
            {
                result.Add("HeaderFlags", GetHeaderFlags().ToString());
                result.Add("DetailFlags", GetDetailFlags().ToString());
                result.Add("HostFlags", GetHostFlags().ToString());
                result.Add("StaticReadLevels", SharedReadLevels.ToString());
                result.Add("StaticWriteLevels", SharedWriteLevels.ToString());
                result.Add("ReadLevels", ReadLevels.ToString());
                result.Add("WriteLevels", WriteLevels.ToString());
                result.Add("CancelReadLevels", CancelReadLevels.ToString());
                result.Add("IsOpen", SystemConsoleIsOpen(false).ToString());
                result.Add("WindowIsOpen", SystemConsoleIsOpen(true).ToString());
                result.Add("InputIsOpen", SystemConsoleInputIsOpen().ToString());
                result.Add("OutputIsOpen", SystemConsoleOutputIsOpen().ToString());
                result.Add("InputIsRedirected", SystemConsoleInputIsRedirected().ToString());
                result.Add("OutputIsRedirected", SystemConsoleOutputIsRedirected().ToString());
                result.Add("ErrorIsRedirected", SystemConsoleErrorIsRedirected().ToString());

#if NATIVE && WINDOWS
                result.Add("CloseCount", closeCount.ToString());
#endif

                result.Add("ReferenceCount", referenceCount.ToString());
                result.Add("MustBeOpenCount", mustBeOpenCount.ToString());
                result.Add("CertificateCount", certificateCount.ToString());
                result.Add("CertificateSubject", certificateSubject);
                result.Add("SavedTitle", savedTitle);
                result.Add("SavedForegroundColor", savedForegroundColor.ToString());
                result.Add("SavedBackgroundColor", savedBackgroundColor.ToString());
                result.Add("SavedWindowWidth", savedWindowWidth.ToString());
                result.Add("SavedWindowHeight", savedWindowHeight.ToString());
                result.Add("SavedBufferWidth", savedBufferWidth.ToString());
                result.Add("SavedBufferHeight", savedBufferHeight.ToString());
            }

            ///////////////////////////////////////////////////////////////////////////////////////////

#if NATIVE && WINDOWS && DRAWING && !NET_STANDARD_20
            lock (staticSyncRoot) /* TRANSACTIONAL */
            {
                result.Add("OldBigIcon", oldBigIcon.ToString());
                result.Add("OldSmallIcon", oldSmallIcon.ToString());
            }
#endif

            ///////////////////////////////////////////////////////////////////////////////////////////

#if NATIVE && WINDOWS
            lock (syncRoot)
            {
                StringPairList list = new StringPairList();

                if (FlagOps.HasFlags(detailFlags, DetailFlags.NativeConsole, true))
                    NativeConsole.AddInfo(list, detailFlags);

                foreach (IPair<string> element in list)
                {
                    if ((element == null) || (element.X == null) || (element.Y == null))
                        continue;

                    result.Add(element.X, element.Y);
                }
            }
#endif

            return result;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Plays a beep on the console at the specified frequency and duration.
        /// </summary>
        /// <param name="frequency">
        /// The frequency of the beep, in hertz.
        /// </param>
        /// <param name="duration">
        /// The duration of the beep, in milliseconds.
        /// </param>
        /// <returns>
        /// True if the beep was played successfully; otherwise, false.
        /// </returns>
        public override bool Beep(
            int frequency,
            int duration
            )
        {
            CheckDisposed();

            try
            {
                SystemConsoleMustBeOpen(false); /* throw */
                System.Console.Beep(frequency, duration);

                return true;
            }
            catch (ScriptException)
            {
                //
                // NOTE: The console is not open, just return false.
                //
                return false;
            }
            catch (Exception e)
            {
                //
                // NOTE: Something failed, just return false.
                //
                TraceOps.DebugTrace(
                    e, typeof(Console).Name,
                    TracePriority.ConsoleError);

                return false;
            }
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Determines whether the host is idle.  This host has no idle detection
        /// and always reports that it is idle.
        /// </summary>
        /// <returns>
        /// True if the host is idle; otherwise, false.
        /// </returns>
        public override bool IsIdle()
        {
            CheckDisposed();

            //
            // STUB: We have no better idle detection.
            //
            return true;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Clears the console screen.
        /// </summary>
        /// <returns>
        /// True if the console was cleared successfully; otherwise, false.
        /// </returns>
        public override bool Clear()
        {
            CheckDisposed();
            EnterWriteLevel();

            try
            {
                SystemConsoleMustBeOpen(true); /* throw */
                System.Console.Clear(); /* throw */

                return true;
            }
            catch (ScriptException)
            {
                //
                // NOTE: The console is not open, just return false.
                //
                return false;
            }
            catch (Exception e)
            {
                //
                // NOTE: Something failed, just return false.
                //
                TraceOps.DebugTrace(
                    e, typeof(Console).Name,
                    TracePriority.ConsoleError);

                return false;
            }
            finally
            {
                ExitWriteLevel();
            }
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Resets the cached host flags for this console host so that they will
        /// be recomputed on the next request.
        /// </summary>
        /// <returns>
        /// True if the host flags were reset successfully; otherwise, false.
        /// </returns>
        public override bool ResetHostFlags()
        {
            CheckDisposed();

            return PrivateResetHostFlags();
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Clears the console command history.
        /// </summary>
        /// <param name="error">
        /// Upon failure, receives information about the error encountered.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise,
        /// <see cref="ReturnCode.Error" />.
        /// </returns>
        public override ReturnCode ResetHistory(
            ref Result error
            )
        {
            CheckDisposed();

#if NATIVE && WINDOWS
            return PrivateResetHistory(ref error);
#else
            error = "not implemented";
            return ReturnCode.Error;
#endif
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Gets the console mode flags for the specified channel.
        /// </summary>
        /// <param name="channelType">
        /// The console channel whose mode is retrieved.
        /// </param>
        /// <param name="mode">
        /// Upon success, receives the mode flags for the channel.
        /// </param>
        /// <param name="error">
        /// Upon failure, receives information about the error encountered.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise,
        /// <see cref="ReturnCode.Error" />.
        /// </returns>
        public override ReturnCode GetMode(
            ChannelType channelType,
            ref uint mode,
            ref Result error
            )
        {
            CheckDisposed();

#if NATIVE && WINDOWS
            return PrivateGetMode(channelType, ref mode, ref error);
#else
            error = "not implemented";
            return ReturnCode.Error;
#endif
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Sets the console mode flags for the specified channel.
        /// </summary>
        /// <param name="channelType">
        /// The console channel whose mode is set.
        /// </param>
        /// <param name="mode">
        /// The mode flags to set for the channel.
        /// </param>
        /// <param name="error">
        /// Upon failure, receives information about the error encountered.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise,
        /// <see cref="ReturnCode.Error" />.
        /// </returns>
        public override ReturnCode SetMode(
            ChannelType channelType,
            uint mode,
            ref Result error
            )
        {
            CheckDisposed();

#if NATIVE && WINDOWS
            return PrivateSetMode(channelType, mode, ref error);
#else
            error = "not implemented";
            return ReturnCode.Error;
#endif
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Opens (or attaches to) the native console and re-applies this host's
        /// console customizations.
        /// </summary>
        /// <param name="error">
        /// Upon failure, receives information about the error encountered.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise,
        /// <see cref="ReturnCode.Error" />.
        /// </returns>
        public override ReturnCode Open(
            ref Result error
            )
        {
            CheckDisposed();

#if NATIVE && WINDOWS
            ReturnCode code = PrivateAttachOrOpen(
                UseForce, UseAttach, ref error);

            if (code == ReturnCode.Ok)
            {
                //
                // NOTE: The call to NativeConsole.Open succeeded,
                //       decrease the "close" count by one.  Do not
                //       let the count fall [and stay] below zero.
                //
                if (UnbumpConsoleClosed() < 0) BumpConsoleClosed();

                //
                // NOTE: Now, re-setup our console customizations.
                //
                if (!Setup(this, true, true))
                {
                    error = "failed to re-setup console";
                    code = ReturnCode.Error;
                }
            }

            return code;
#else
            error = "not implemented";
            return ReturnCode.Error;
#endif
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Tears down this host's console customizations and closes the native
        /// console, provided there are no pending reads or writes and the host
        /// is not locked as a kiosk.
        /// </summary>
        /// <param name="error">
        /// Upon failure, receives information about the error encountered.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise,
        /// <see cref="ReturnCode.Error" />.
        /// </returns>
        public override ReturnCode Close(
            ref Result error
            )
        {
            CheckDisposed();

#if SHELL
            Interpreter interpreter = InternalSafeGetInterpreter(false);

            if ((interpreter != null) && interpreter.IsKioskLock())
            {
                error = "cannot close host when a kiosk";
                return ReturnCode.Error;
            }
#endif

#if NATIVE && WINDOWS
            ReturnCode code = CheckActiveReadsAndWrites(ref error);

            if (code == ReturnCode.Ok)
            {
                if (Setup(this, false, true))
                {
                    code = UnhookSystemConsoleControlHandler(
                        false, false, ref error);

                    if (code == ReturnCode.Ok)
                    {
                        //
                        // NOTE: Prior to actually closing the console,
                        //       prevent other threads from attempting
                        //       to use it by adding a "lock" to the
                        //       close count.  Then, if the call to the
                        //       NativeConsole.Close method succeeds, add
                        //       another "lock" on the close count.
                        //       Finally, remove the outer "lock" prior
                        //       to returning from this method, leaving
                        //       the inner one in place.
                        //
                        BumpConsoleClosed();

                        try
                        {
                            code = PrivateClose(ref error);

                            if (code == ReturnCode.Ok)
                                BumpConsoleClosed();
                        }
                        finally
                        {
                            UnbumpConsoleClosed();
                        }
                    }
                }
                else
                {
                    error = "failed to un-setup console";
                    code = ReturnCode.Error;
                }
            }

            return code;
#else
            error = "not implemented";
            return ReturnCode.Error;
#endif
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Discards any pending or cached console input.
        /// </summary>
        /// <param name="error">
        /// Upon failure, receives information about the error encountered.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise,
        /// <see cref="ReturnCode.Error" />.
        /// </returns>
        public override ReturnCode Discard(
            ref Result error
            )
        {
            CheckDisposed();

#if NATIVE && WINDOWS
            if (ConsoleOps.ResetCachedInputRecord(
                    ref error) == ReturnCode.Ok)
            {
                return PrivateFlushInputBuffer(ref error);
            }
            else
            {
                return ReturnCode.Error;
            }
#else
            error = "not implemented";
            return ReturnCode.Error;
#endif
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Resets the console streams, the base host state, and the cached host
        /// flags to their default state.
        /// </summary>
        /// <param name="error">
        /// Upon failure, receives information about the error encountered.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise,
        /// <see cref="ReturnCode.Error" />.
        /// </returns>
        public override ReturnCode Reset(
            ref Result error
            )
        {
            CheckDisposed();

            if ((ConsoleOps.ResetStreams(
                    ChannelType.StandardChannels,
                    ref error) == ReturnCode.Ok) &&
                (base.Reset(ref error) == ReturnCode.Ok))
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

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Begins a named output section on the host.  This host does not render
        /// sections and always reports success.
        /// </summary>
        /// <param name="name">
        /// The name of the section.  This parameter may be null.
        /// </param>
        /// <param name="clientData">
        /// The client data associated with the section.  This parameter may be
        /// null.
        /// </param>
        /// <returns>
        /// True if the section was begun successfully; otherwise, false.
        /// </returns>
        public override bool BeginSection(
            string name,
            IClientData clientData
            )
        {
            CheckDisposed();

            return true;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Ends a named output section on the host.  This host does not render
        /// sections and always reports success.
        /// </summary>
        /// <param name="name">
        /// The name of the section.  This parameter may be null.
        /// </param>
        /// <param name="clientData">
        /// The client data associated with the section.  This parameter may be
        /// null.
        /// </param>
        /// <returns>
        /// True if the section was ended successfully; otherwise, false.
        /// </returns>
        public override bool EndSection(
            string name,
            IClientData clientData
            )
        {
            CheckDisposed();

            return true;
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////////////////////////////

        #region ISynchronizeBase Members
        /// <summary>
        /// Gets the object used to synchronize access to the per-instance state
        /// of this console host.
        /// </summary>
        public virtual object SyncRoot
        {
            get { CheckDisposed(); return syncRoot; }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////////////////////////////

        #region ISynchronize Members
        /// <summary>
        /// Attempts to acquire the synchronization lock without waiting.
        /// </summary>
        /// <param name="locked">
        /// Upon return, set to true if the lock was acquired; otherwise, false.
        /// </param>
        public virtual void TryLock(
            ref bool locked
            )
        {
            CheckDisposed();

            if (syncRoot == null)
                return;

            locked = Monitor.TryEnter(syncRoot);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Attempts to acquire the synchronization lock, waiting up to the
        /// configured wait-lock timeout.
        /// </summary>
        /// <param name="locked">
        /// Upon return, set to true if the lock was acquired; otherwise, false.
        /// </param>
        public void TryLockWithWait(
            ref bool locked
            )
        {
            CheckDisposed();

            if (syncRoot == null)
                return;

            locked = Monitor.TryEnter(
                syncRoot, ThreadOps.GetTimeout(
                null, null, TimeoutType.WaitLock));
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Attempts to acquire the synchronization lock without waiting and
        /// without checking whether this object has been disposed.
        /// </summary>
        /// <param name="locked">
        /// Upon return, set to true if the lock was acquired; otherwise, false.
        /// </param>
        public void TryLockNoThrow(
            ref bool locked
            )
        {
            // CheckDisposed(); /* EXEMPT */

            if (syncRoot == null)
                return;

            locked = Monitor.TryEnter(syncRoot);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Attempts to acquire the synchronization lock, waiting up to the
        /// specified timeout.
        /// </summary>
        /// <param name="timeout">
        /// The maximum amount of time, in milliseconds, to wait for the lock.
        /// </param>
        /// <param name="locked">
        /// Upon return, set to true if the lock was acquired; otherwise, false.
        /// </param>
        public virtual void TryLock(
            int timeout,
            ref bool locked
            )
        {
            CheckDisposed();

            if (syncRoot == null)
                return;

            locked = Monitor.TryEnter(syncRoot, timeout);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Releases the synchronization lock if it is currently held.
        /// </summary>
        /// <param name="locked">
        /// On input, true if the lock is held; set to false upon release.
        /// </param>
        public virtual void ExitLock(
            ref bool locked
            )
        {
            if (RuntimeOps.ShouldCheckDisposedOnExitLock(locked)) /* EXEMPT */
                CheckDisposed();

            if (syncRoot == null)
                return;

            if (locked)
            {
                Monitor.Exit(syncRoot);
                locked = false;
            }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////////////////////////////

        #region IMaybeDisposed Members
        /// <summary>
        /// Gets a value indicating whether this console host has been disposed.
        /// </summary>
        public override bool Disposed
        {
            get { return disposed; }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////////////////////////////

        #region IDisposable "Pattern" Members
        /// <summary>
        /// Non-zero if this console host has been disposed.
        /// </summary>
        private bool disposed;

        /// <summary>
        /// Throws an exception if this console host has been disposed and the
        /// interpreter is configured to throw on disposed objects.
        /// </summary>
        private void CheckDisposed() /* throw */
        {
#if THROW_ON_DISPOSED
            if (disposed && _Engine.IsThrowOnDisposed(
                    InternalSafeGetInterpreter(false), null))
            {
                throw new InterpreterDisposedException(typeof(Console));
            }
#endif
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Releases the resources used by this console host, tearing down the
        /// console customizations during disposal.
        /// </summary>
        /// <param name="disposing">
        /// Non-zero if this method is being called from the
        /// <see cref="IDisposable.Dispose" /> method; zero if it is being called
        /// from the finalizer.
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

                    Setup(this, false, false);
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
