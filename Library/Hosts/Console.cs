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
using Eagle._Containers.Private;
using Eagle._Containers.Public;
using Eagle._Interfaces.Public;

using _Engine = Eagle._Components.Public.Engine;
using SharedStringOps = Eagle._Components.Shared.StringOps;

namespace Eagle._Hosts
{
    [ObjectId("e15283cf-00b4-44f2-a16e-48cf061e53d1")]
    public class Console : Core, ISynchronize, IDisposable
    {
        #region Private Static Data
        private static readonly object staticSyncRoot = new object();

        ///////////////////////////////////////////////////////////////////////////////////////////////

#if NATIVE && WINDOWS
        private static int closeCount = 0;
#endif

        ///////////////////////////////////////////////////////////////////////////////////////////////

        private static int referenceCount = 0;

        ///////////////////////////////////////////////////////////////////////////////////////////////

        private static int mustBeOpenCount = 0;

        ///////////////////////////////////////////////////////////////////////////////////////////////

#if NATIVE
        //
        // HACK: Setting this value to non-zero will disable script cancellation
        //       from being triggered via the Cancel (PrivateForceCancel) method.
        //
        private static bool defaultForceNoCancel = false;
#endif

        //
        // HACK: Setting this value to non-zero will force this class to treat
        //       non-default application domains [more-or-less] like the default
        //       application domain one (e.g. the Ctrl-C keypress handler will
        //       be added/removed).
        //
        private static bool defaultForceAppDomain = false;

        //
        // HACK: Setting this value to non-zero will force the console cancel
        //       event handler to be changed even when there may be an event
        //       handler pending.
        //
        private static bool defaultForcePending = true;

        ///////////////////////////////////////////////////////////////////////////////////////////////

#if NATIVE && WINDOWS && DRAWING && !NET_STANDARD_20
        private static Icon icon;
        private static IntPtr oldBigIcon;
        private static IntPtr oldSmallIcon;
#endif

        ///////////////////////////////////////////////////////////////////////////////////////////////

        //
        // HACK: This is purposely not read-only.
        //
        private static ConsoleCancelEventHandler consoleCancelEventHandler = null;
        #endregion

        ///////////////////////////////////////////////////////////////////////////////////////////////

        #region Private Data
        private readonly object syncRoot = new object();

        ///////////////////////////////////////////////////////////////////////////////////////////////

        private int certificateCount = 0;
        private string certificateSubject = null; /* CACHED */

        ///////////////////////////////////////////////////////////////////////////////////////////////

        private string savedTitle;

        ///////////////////////////////////////////////////////////////////////////////////////////////

        private ConsoleColor savedForegroundColor = _ConsoleColor.None;
        private ConsoleColor savedBackgroundColor = _ConsoleColor.None;

        ///////////////////////////////////////////////////////////////////////////////////////////////

        private int savedWindowWidth = _Size.Invalid;
        private int savedWindowHeight = _Size.Invalid;
        private int savedBufferWidth = _Size.Invalid;
        private int savedBufferHeight = _Size.Invalid;
        #endregion

        ///////////////////////////////////////////////////////////////////////////////////////////////

        #region Private Constants
        #region Native Console CancelKeyPress Handling
#if NATIVE
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
        internal static readonly int SafeWriteSize = 25000; /* NOTE: <=26000 */
        #endregion

        ///////////////////////////////////////////////////////////////////////////////////////////////

        #region Buffer Size Constants
        private static readonly int MaximumBufferWidthMargin = 8;
        private static readonly int MaximumBufferHeight = 9999;
        #endregion

        ///////////////////////////////////////////////////////////////////////////////////////////////

        #region Window Width Constants
        //
        // HACK: These are considered to be "best guess" values.
        //       Please adjust them to suit your taste as necessary.
        //
        private static readonly int MinimumWindowWidth = 40;
        private static readonly int CompactWindowWidth = 80;
        private static readonly int FullWindowWidth = 120;
        private static readonly int SuperFullWindowWidth = 160;
        private static readonly int JumboWindowWidth = 200;
        private static readonly int SuperJumboWindowWidth = 230;
        #endregion

        ///////////////////////////////////////////////////////////////////////////////////////////////

        #region Window Height Constants
        private static readonly int MinimumWindowHeight = 10;
        private static readonly int CompactWindowHeight = 25;
        private static readonly int FullWindowHeight = 40;
        private static readonly int SuperFullWindowHeight = 60;
        private static readonly int JumboWindowHeight = 75;
        private static readonly int SuperJumboWindowHeight = 90;
        #endregion

        ///////////////////////////////////////////////////////////////////////////////////////////////

        #region Margin Constants
        private static readonly int MaximumWindowWidthMargin = MaximumBufferWidthMargin;
        private static readonly int MaximumWindowHeightMargin = 6;
        #endregion

        ///////////////////////////////////////////////////////////////////////////////////////////////

        #region Window Title Constants
        private static readonly string AdministratorTitlePrefix = "Administrator:";
        private static readonly string CertificateSubjectPrefix = "- ";
        private static readonly string CertificateSubjectPending = "checking certificate...";
        #endregion

        ///////////////////////////////////////////////////////////////////////////////////////////////

        #region ConsoleKeyInfo Formatting Constants
        private static readonly string ModifierEchoFormat = "{0}{1}";
        private static readonly char ModifierEchoSeparator = Characters.MinusSign;
        #endregion
        #endregion

        ///////////////////////////////////////////////////////////////////////////////////////////////

        #region Public Constructors
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
                    // NOTE: No idea on this platform as Mono does not support
                    //       console window width and height on Unix (?).  Fake
                    //       it.
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

        #region Host Read/Write Levels Support
        protected override void EnterReadLevel()
        {
            // CheckDisposed();

            Interlocked.Increment(ref sharedReadLevels);
            base.EnterReadLevel();
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        protected override void ExitReadLevel()
        {
            // CheckDisposed();

            base.ExitReadLevel();
            Interlocked.Decrement(ref sharedReadLevels);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        protected override void EnterWriteLevel()
        {
            // CheckDisposed();

            Interlocked.Increment(ref sharedWriteLevels);
            base.EnterWriteLevel();
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

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

        protected virtual bool SystemConsoleInputIsRedirected()
        {
            return SystemConsoleIsRedirected(
                InternalSafeGetInterpreter(false),
                ChannelType.Input, false);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

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

        protected virtual bool SystemConsoleErrorIsRedirected()
        {
            return SystemConsoleIsRedirected(
                InternalSafeGetInterpreter(false),
                ChannelType.Error, false);
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////////////////////////////

        #region System.Console Open/Close Handling
        private static void EnableThrowOnMustBeOpen()
        {
            //
            // NOTE: If necessary, enable throwing exceptions from
            //       within the SystemConsole*MustBeOpen() methods.
            //
            if (!ThrowOnMustBeOpen) ThrowOnMustBeOpen = true;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        internal static bool ThrowOnMustBeOpen
        {
            get
            {
                try
                {
                    return Interlocked.Increment(ref mustBeOpenCount) > 1;
                }
                finally
                {
                    Interlocked.Decrement(ref mustBeOpenCount);
                }
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

        #region System.Console Size Handling
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
                        System.Console.Title = BuildTitle(
                            interpreter, useCertificate);
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
        protected virtual bool IsChannelRedirected(
            ChannelType channelType
            )
        {
            return SystemConsoleIsRedirected(
                InternalSafeGetInterpreter(false), channelType, false);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        #region System.Console Stream Handling
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
        private ReturnCode UnhookSystemConsoleControlHandler(
            bool force,
            bool strict
            )
        {
            Result error = null;

            return UnhookSystemConsoleControlHandler(force, strict, ref error);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

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
        protected virtual bool IsCancelViaConsolePending()
        {
            return Interpreter.IsCancelViaConsolePending();
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

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

        private static void UninstallIcon(
            IntPtr handle /* in */
            )
        {
            lock (staticSyncRoot) /* TRANSACTIONAL */
            {
                WindowOps.UnsafeNativeMethods.SendMessage(
                    handle, WindowOps.UnsafeNativeMethods.WM_SETICON,
                    new UIntPtr(WindowOps.UnsafeNativeMethods.ICON_BIG),
                    oldBigIcon);

                oldBigIcon = IntPtr.Zero;

                WindowOps.UnsafeNativeMethods.SendMessage(
                    handle, WindowOps.UnsafeNativeMethods.WM_SETICON,
                    new UIntPtr(WindowOps.UnsafeNativeMethods.ICON_SMALL),
                    oldSmallIcon);

                oldSmallIcon = IntPtr.Zero;

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
        private int cancelReadLevels;
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
        protected virtual bool WasReadCanceled()
        {
            return Interlocked.CompareExchange(
                ref cancelReadLevels, 0, 0) > 0;
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////////////////////////////

        #region Reset Read Cancellation
        protected virtual void ResetCancelRead()
        {
            // CheckDisposed();

            /* IGNORED */
            Interlocked.Exchange(ref cancelReadLevels, 0);
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////////////////////////////

        #region Initiate Read Cancellation
        protected virtual void CancelRead()
        {
            // CheckDisposed();

            Interlocked.Increment(ref cancelReadLevels);
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////////////////////////////

        #region Read / ReadLine Mutators
        protected virtual void GetValueForRead(
            ref string value /* in, out */
            )
        {
            if (WasReadCanceled())
                value = null;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        protected virtual void GetValueForRead(
            ref int? value /* in, out */
            )
        {
            if (WasReadCanceled())
                value = null;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

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
        protected virtual void EchoValueForRead(
            string value, /* in */
            bool newLine  /* in */
            )
        {
            if (newLine)
                System.Console.WriteLine(value); /* throw */
            else
                System.Console.Write(value); /* throw */
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        protected virtual void EchoValueForRead(
            int value,   /* in */
            bool newLine /* in */
            )
        {
            char character = Convert.ToChar(value); /* throw */

            if (newLine)
                System.Console.WriteLine(character); /* throw */
            else
                System.Console.Write(character); /* throw */
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

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
                System.Console.Write(String.Format(
                    ModifierEchoFormat, ConsoleModifiers.Control,
                    ModifierEchoSeparator));
            }

            if (FlagOps.HasFlags(
                    modifiers, ConsoleModifiers.Shift, true))
            {
                System.Console.Write(String.Format(
                    ModifierEchoFormat, ConsoleModifiers.Shift,
                    ModifierEchoSeparator));
            }

            if (FlagOps.HasFlags(
                    modifiers, ConsoleModifiers.Alt, true))
            {
                System.Console.Write(String.Format(
                    ModifierEchoFormat, ConsoleModifiers.Alt,
                    ModifierEchoSeparator));
            }

            if (newLine)
                System.Console.WriteLine(character); /* throw */
            else
                System.Console.Write(character); /* throw */
        }
        #endregion
        #endregion

        ///////////////////////////////////////////////////////////////////////////////////////////////

        #region Console Pending Reads/Writes Handling
        private static int sharedReadLevels;
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

        private static int sharedWriteLevels;
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
        private void MaybeComplain(
            ReturnCode code,
            Result result
            )
        {
            if (!IsVerboseMode())
                return;

            DebugOps.Complain(
                InternalSafeGetInterpreter(false), code, result);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        #region Native Console Open/Close Handling
#if NATIVE && WINDOWS
        private ReturnCode PrivateAttachOrOpen(
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
                    false, attach, ref attached,
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

                if (errors != null)
                    MaybeComplain(code, errors);

                return code;
            }
            else
            {
                error = "not implemented";
                return ReturnCode.Error;
            }
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

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
        private static bool WasConsoleClosed()
        {
            return Interlocked.CompareExchange(
                ref closeCount, 0, 0) > 0;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        private static int BumpConsoleClosed()
        {
            return Interlocked.Increment(ref closeCount);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        private static int UnbumpConsoleClosed()
        {
            return Interlocked.Decrement(ref closeCount);
        }
#endif
        #endregion

        ///////////////////////////////////////////////////////////////////////////////////////////////

        #region Native Console Input Handling
#if NATIVE && WINDOWS
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
        #endregion
        #endregion

        ///////////////////////////////////////////////////////////////////////////////////////////////

        #region Content Section Methods
        protected override bool DoesSupportColor()
        {
            if (SystemConsoleOutputIsRedirected())
                return false;

            return base.DoesSupportColor();
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        protected override bool DoesAdjustColor()
        {
            if (SystemConsoleOutputIsRedirected())
                return false;

            return base.DoesAdjustColor();
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        protected override bool DoesSupportSizing()
        {
            if (SystemConsoleOutputIsRedirected())
                return false;

            return base.DoesSupportSizing();
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        protected override bool DoesSupportPositioning()
        {
            if (SystemConsoleOutputIsRedirected())
                return false;

            return base.DoesSupportPositioning();
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////////////////////////////

        #region IInteractiveHost Members
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

        public override bool RefreshTitle()
        {
            CheckDisposed();

            return SetupTitle(true);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

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

        public override bool IsOpen()
        {
            CheckDisposed();

            return SystemConsoleIsOpen(true);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

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

        private HostFlags hostFlags = HostFlags.Invalid;
        public override HostFlags GetHostFlags()
        {
            CheckDisposed();

            return MaybeInitializeHostFlags();
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

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

                string localValue = System.Console.ReadLine();

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

        public override bool WriteLine()
        {
            CheckDisposed();
            EnterWriteLevel();

            try
            {
                SystemConsoleOutputMustBeOpen(this); /* throw */
                System.Console.WriteLine();

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

        public override bool SetupChannels()
        {
            CheckDisposed();

            return false; /* NOT IMPLEMENTED */
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////////////////////////////

        #region IDebugHost Members
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

        private HostTestFlags hostTestFlags = HostTestFlags.Invalid;
        public override HostTestFlags GetTestFlags()
        {
            CheckDisposed();

            if (hostTestFlags == HostTestFlags.Invalid)
                hostTestFlags = HostTestFlags.None;

            return hostTestFlags;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

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
                    lock (syncRoot) /* TRANSACTIONAL */
                    {
                        foregroundColor = savedForegroundColor;
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
                    lock (syncRoot) /* TRANSACTIONAL */
                    {
                        backgroundColor = savedBackgroundColor;
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

                if (newLine)
                    System.Console.WriteLine(value);
                else
                    System.Console.Write(value);

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

                if (newLine)
                    System.Console.WriteLine(value);
                else
                    System.Console.Write(value);

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
        public override StringList QueryState(
            DetailFlags detailFlags
            )
        {
            CheckDisposed();

            StringList result = new StringList();

            lock (syncRoot) /* TRANSACTIONAL */
            {
                result.Add("HeaderFlags", GetHeaderFlags().ToString());
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

        public override bool IsIdle()
        {
            CheckDisposed();

            //
            // STUB: We have no better idle detection.
            //
            return true;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

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

        public override bool ResetHostFlags()
        {
            CheckDisposed();

            return PrivateResetHostFlags();
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

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

        public override ReturnCode Open(
            ref Result error
            )
        {
            CheckDisposed();

#if NATIVE && WINDOWS
            ReturnCode code = PrivateAttachOrOpen(UseAttach, ref error);

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

        public override bool BeginSection(
            string name,
            IClientData clientData
            )
        {
            CheckDisposed();

            return true;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

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
        public virtual object SyncRoot
        {
            get { CheckDisposed(); return syncRoot; }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////////////////////////////

        #region ISynchronize Members
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
        public override bool Disposed
        {
            get { return disposed; }
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
                throw new InterpreterDisposedException(typeof(Console));
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
