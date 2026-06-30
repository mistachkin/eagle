/*
 * AnsiConsole.cs --
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

#if !NATIVE || !UNIX
#error "This file cannot be compiled or used properly with native Unix code disabled."
#endif

using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using Eagle._Attributes;
using Eagle._Components.Public;
using Eagle._Containers.Public;

using SharedStringOps = Eagle._Components.Shared.StringOps;
using ScreenStack = System.Collections.Generic.Stack<string>;
using ScreenDictionary = System.Collections.Generic.Dictionary<string, bool>;

namespace Eagle._Components.Private
{
    /// <summary>
    /// This class provides static helper methods for managing ANSI terminal
    /// "alternative screen buffers" using escape sequences, including creating,
    /// listing, switching between, and closing emulated screen buffers.
    /// </summary>
    [ObjectId("a8d2023c-5031-43fe-bc5b-be49115f3f55")]
    internal static class AnsiConsole
    {
        #region Private Constants
        //
        // NOTE: The ANSI escape sequences for "alternative screen buffer"
        //       management.  These are supported by virtually all modern
        //       terminal emulators on Linux, macOS, and Windows Terminal.
        //
        // HACK: These are purposely not read-only.
        //
        /// <summary>
        /// The ANSI escape character (ESC) used to introduce each control
        /// sequence.
        /// </summary>
        private static string Escape = "\x1B";
        /// <summary>
        /// The ANSI escape sequence used to switch to the alternative screen
        /// buffer.
        /// </summary>
        private static string AltScreenEnable = Escape + "[?1049h";
        /// <summary>
        /// The ANSI escape sequence used to switch back from the alternative
        /// screen buffer.
        /// </summary>
        private static string AltScreenDisable = Escape + "[?1049l";
        /// <summary>
        /// The ANSI escape sequence used to save the current cursor position.
        /// </summary>
        private static string CursorSave = Escape + "7";
        /// <summary>
        /// The ANSI escape sequence used to restore the previously saved cursor
        /// position.
        /// </summary>
        private static string CursorRestore = Escape + "8";
        /// <summary>
        /// The ANSI escape sequence used to clear the entire screen.
        /// </summary>
        private static string ScreenClear = Escape + "[2J";
        /// <summary>
        /// The ANSI escape sequence used to move the cursor to the home
        /// (top-left) position.
        /// </summary>
        private static string CursorHome = Escape + "[H";
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Private Data
        //
        // NOTE: This is used to synchronize access to the screen buffer
        //       state managed by this class.
        //
        /// <summary>
        /// The object used to synchronize access to the screen buffer state
        /// managed by this class.
        /// </summary>
        private static readonly object syncRoot = new object();

        ///////////////////////////////////////////////////////////////////////

        //
        // NOTE: Thread ID tracking for lock diagnostics.
        //
        /// <summary>
        /// The managed thread identifier currently recorded as holding the
        /// lock, used for lock diagnostics; zero when no thread is recorded.
        /// </summary>
        private static long lockThreadId = 0;

        ///////////////////////////////////////////////////////////////////////

        //
        // NOTE: Stack of saved screen content for the push/pop emulation.
        //       Each entry is the screen name that was pushed.
        //
        /// <summary>
        /// The stack of saved screen buffer names used for the push/pop
        /// emulation; each entry is the name of a pushed screen buffer.
        /// </summary>
        private static ScreenStack activeScreenNames;

        ///////////////////////////////////////////////////////////////////////

        //
        // NOTE: Set of known screen buffer names.
        //
        /// <summary>
        /// The set of known screen buffer names.
        /// </summary>
        private static ScreenDictionary screenBuffers;
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Lock Helper Methods
        /// <summary>
        /// This method returns the managed thread identifier currently recorded
        /// as holding the lock, or zero if no thread is recorded.
        /// </summary>
        /// <returns>
        /// The managed thread identifier recorded as holding the lock, or zero
        /// if none is recorded.
        /// </returns>
        private static long MaybeWhoHasLock()
        {
            return Interlocked.CompareExchange(ref lockThreadId, 0, 0);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method records the current thread as holding the lock when the
        /// lock has been successfully acquired.
        /// </summary>
        /// <param name="locked">
        /// Non-zero if the lock was acquired by the current thread.
        /// </param>
        private static void MaybeSomebodyHasLock(
            bool locked /* in */
            )
        {
            if (locked)
            {
                /* IGNORED */
                Interlocked.CompareExchange(ref lockThreadId,
                    GlobalState.GetCurrentLockThreadId(), 0);
            }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method clears the recorded lock-holding thread when the lock is
        /// being released by the current thread.
        /// </summary>
        /// <param name="locked">
        /// Non-zero if the lock is held by the current thread.
        /// </param>
        private static void MaybeNobodyHasLock(
            bool locked /* in */
            )
        {
            if (locked)
            {
                /* IGNORED */
                Interlocked.CompareExchange(ref lockThreadId,
                    0, GlobalState.GetCurrentLockThreadId());
            }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method attempts to acquire the lock used to synchronize access
        /// to the screen buffer state.
        /// </summary>
        /// <param name="locked">
        /// Upon return, this parameter is set to non-zero if the lock was
        /// acquired by the current thread.
        /// </param>
        private static void TryLock(
            ref bool locked /* out */
            )
        {
            if (syncRoot == null)
                return;

            locked = Monitor.TryEnter(syncRoot);
            MaybeSomebodyHasLock(locked);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method releases the lock used to synchronize access to the
        /// screen buffer state, if it is currently held by the current thread.
        /// </summary>
        /// <param name="locked">
        /// On input, non-zero if the lock is held by the current thread.  Upon
        /// return, this parameter is set to false.
        /// </param>
        private static void ExitLock(
            ref bool locked /* in, out */
            )
        {
            if (syncRoot == null)
                return;

            if (locked)
            {
                MaybeNobodyHasLock(locked);
                Monitor.Exit(syncRoot);
                locked = false;
            }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Private Methods
        /// <summary>
        /// This method writes a single ANSI escape sequence to the specified
        /// text writer and flushes it.
        /// </summary>
        /// <param name="textWriter">
        /// The text writer to which the escape sequence is written.  If this
        /// parameter is null, no action is taken.
        /// </param>
        /// <param name="sequence">
        /// The ANSI escape sequence to write.
        /// </param>
        private static void WriteEscape(
            TextWriter textWriter, /* in */
            string sequence        /* in */
            )
        {
            if (textWriter == null)
                return;

            textWriter.Write(sequence);
            textWriter.Flush();
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method writes zero or more ANSI escape sequences to the
        /// specified text writer, in order, flushing each one.
        /// </summary>
        /// <param name="textWriter">
        /// The text writer to which the escape sequences are written.
        /// </param>
        /// <param name="sequences">
        /// The ANSI escape sequences to write.  If this parameter is null, no
        /// action is taken.
        /// </param>
        private static void WriteEscapes(
            TextWriter textWriter,    /* in */
            params string[] sequences /* in */
            )
        {
            if (sequences == null)
                return;

            foreach (string sequence in sequences)
                WriteEscape(textWriter, sequence);
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Public Methods
        /// <summary>
        /// This method determines whether ANSI alternative screen buffer
        /// support is available in the current environment.
        /// </summary>
        /// <returns>
        /// True if ANSI alternative screen buffers are supported and standard
        /// output is not redirected; otherwise, false.
        /// </returns>
        public static bool IsSupported()
        {
            //
            // NOTE: ANSI "alternative screen buffers" are supported on
            //       virtually all modern terminals.  First, check that
            //       standard output is not redirected (i.e. because an
            //       ANSI escape sequence is meaningless for files).
            //
#if NET_45 || NET_451 || NET_452 || NET_46 || NET_461 || NET_462 || NET_47 || NET_471 || NET_472 || NET_48 || NET_481 || NET_STANDARD_20
            return !System.Console.IsOutputRedirected;
#else
            return false;
#endif
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method determines whether there is at least one active (pushed)
        /// screen buffer name.
        /// </summary>
        /// <returns>
        /// True if there is at least one active screen buffer name; otherwise,
        /// false.
        /// </returns>
        public static bool HaveActiveScreenName()
        {
            bool locked = false;

            try
            {
                TryLock(ref locked); /* TRANSACTIONAL */

                if (locked)
                {
                    return (activeScreenNames != null) &&
                           (activeScreenNames.Count > 0);
                }
                else
                {
                    TraceOps.LockTrace(
                        "HaveActiveScreenName",
                        typeof(AnsiConsole).Name, true,
                        TracePriority.LockError,
                        MaybeWhoHasLock());
                }
            }
            finally
            {
                ExitLock(ref locked); /* TRANSACTIONAL */
            }

            return false;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method queries the name of the currently active (most recently
        /// pushed) screen buffer.
        /// </summary>
        /// <param name="result">
        /// Upon success, receives the name of the active screen buffer.  Upon
        /// failure, receives an error message.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise, an appropriate
        /// error code.
        /// </returns>
        public static ReturnCode GetActiveScreenName(
            ref Result result /* out */
            )
        {
            bool locked = false;

            try
            {
                TryLock(ref locked); /* TRANSACTIONAL */

                if (locked)
                {
                    if (activeScreenNames == null)
                    {
                        result = "active screen names not available";
                        return ReturnCode.Error;
                    }

                    if (activeScreenNames.Count == 0)
                    {
                        result = "no active screen buffer";
                        return ReturnCode.Error;
                    }

                    result = activeScreenNames.Peek();
                    return ReturnCode.Ok;
                }
                else
                {
                    TraceOps.LockTrace(
                        "GetActiveScreenName",
                        typeof(AnsiConsole).Name, true,
                        TracePriority.LockError,
                        MaybeWhoHasLock());

                    result = "unable to acquire lock";
                    return ReturnCode.Error;
                }
            }
            finally
            {
                ExitLock(ref locked); /* TRANSACTIONAL */
            }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method determines whether a screen buffer with the specified
        /// name is known.
        /// </summary>
        /// <param name="name">
        /// The name of the screen buffer to check for.
        /// </param>
        /// <param name="primary">
        /// Reserved for future use.  This parameter is not used.
        /// </param>
        /// <returns>
        /// True if a screen buffer with the specified name exists; otherwise,
        /// false.
        /// </returns>
        public static bool DoesScreenBufferExist(
            string name,   /* in */
            bool primary   /* in: NOT USED */
            )
        {
            bool locked = false;

            try
            {
                TryLock(ref locked); /* TRANSACTIONAL */

                if (locked)
                {
                    return (screenBuffers != null) &&
                           screenBuffers.ContainsKey(name);
                }
                else
                {
                    TraceOps.LockTrace(
                        "DoesScreenBufferExist",
                        typeof(AnsiConsole).Name, true,
                        TracePriority.LockError,
                        MaybeWhoHasLock());
                }
            }
            finally
            {
                ExitLock(ref locked); /* TRANSACTIONAL */
            }

            return false;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method returns the list of known screen buffer names.
        /// </summary>
        /// <param name="primary">
        /// Reserved for future use.  This parameter is not used.
        /// </param>
        /// <returns>
        /// The list of known screen buffer names, or null if the lock could not
        /// be acquired.
        /// </returns>
        public static StringList ListScreenBuffers(
            bool primary /* in: NOT USED */
            )
        {
            bool locked = false;

            try
            {
                TryLock(ref locked); /* TRANSACTIONAL */

                if (locked)
                {
                    StringList list = new StringList();

                    if (screenBuffers != null)
                    {
                        foreach (string name in screenBuffers.Keys)
                            list.Add(name);
                    }

                    return list;
                }
                else
                {
                    TraceOps.LockTrace(
                        "ListScreenBuffers",
                        typeof(AnsiConsole).Name, true,
                        TracePriority.LockError,
                        MaybeWhoHasLock());
                }
            }
            finally
            {
                ExitLock(ref locked); /* TRANSACTIONAL */
            }

            return null;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method creates a new screen buffer with an automatically
        /// generated name.
        /// </summary>
        /// <param name="name">
        /// Upon success, receives the generated name of the new screen buffer.
        /// </param>
        /// <param name="error">
        /// Upon failure, receives an error message.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise, an appropriate
        /// error code.
        /// </returns>
        public static ReturnCode CreateScreenBuffer(
            ref string name, /* out */
            ref Result error /* out */
            )
        {
            bool locked = false;

            try
            {
                TryLock(ref locked); /* TRANSACTIONAL */

                if (locked)
                {
                    name = FormatOps.Id(
                        typeof(AnsiConsole).Name, null,
                        GlobalState.NextId());

                    if (screenBuffers == null)
                        screenBuffers = new ScreenDictionary();

                    screenBuffers[name] = true;
                    return ReturnCode.Ok;
                }
                else
                {
                    TraceOps.LockTrace(
                        "CreateScreenBuffer",
                        typeof(AnsiConsole).Name, true,
                        TracePriority.LockError,
                        MaybeWhoHasLock());

                    error = "unable to acquire lock";
                    return ReturnCode.Error;
                }
            }
            finally
            {
                ExitLock(ref locked); /* TRANSACTIONAL */
            }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method switches the active screen buffer, either pushing the
        /// specified named buffer onto the alternative screen or popping the
        /// most recently pushed buffer to restore the previous screen.
        /// </summary>
        /// <param name="name">
        /// The name of the screen buffer to make active.  This parameter is
        /// null when popping the saved buffer.
        /// </param>
        /// <param name="useSaved">
        /// Non-zero to pop and restore the most recently saved screen buffer;
        /// otherwise, the specified named buffer is pushed and made active.
        /// </param>
        /// <param name="result">
        /// Upon success, receives the name of the affected screen buffer.  Upon
        /// failure, receives an error message.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise, an appropriate
        /// error code.
        /// </returns>
        public static ReturnCode ChangeActiveScreenBuffer(
            string name,      /* in: null for pop */
            bool useSaved,    /* in: true for pop */
            ref Result result /* out */
            )
        {
            bool locked = false;

            try
            {
                TryLock(ref locked); /* TRANSACTIONAL */

                if (locked)
                {
                    if (useSaved)
                    {
                        if ((activeScreenNames == null) ||
                            (activeScreenNames.Count == 0))
                        {
                            result = "no saved screen buffer to restore";
                            return ReturnCode.Error;
                        }

                        name = activeScreenNames.Pop();

                        WriteEscapes(
                            System.Console.Out, AltScreenDisable,
                            CursorRestore);

                        result = name;
                        return ReturnCode.Ok;
                    }
                    else
                    {
                        if (name == null)
                        {
                            result = "invalid screen buffer name";
                            return ReturnCode.Error;
                        }

                        if ((screenBuffers == null) ||
                            !screenBuffers.ContainsKey(name))
                        {
                            result = String.Format(
                                "screen buffer {0} not found",
                                FormatOps.WrapOrNull(name));

                            return ReturnCode.Error;
                        }

                        //
                        // NOTE: Save cursor position and switch to
                        //       alternative screen buffer.
                        //
                        WriteEscapes(
                            System.Console.Out, CursorSave,
                            AltScreenEnable, ScreenClear,
                            CursorHome);

                        if (activeScreenNames == null)
                            activeScreenNames = new ScreenStack();

                        activeScreenNames.Push(name);

                        result = name;
                        return ReturnCode.Ok;
                    }
                }
                else
                {
                    TraceOps.LockTrace(
                        "ChangeActiveScreenBuffer",
                        typeof(AnsiConsole).Name, true,
                        TracePriority.LockError,
                        MaybeWhoHasLock());

                    result = "unable to acquire lock";
                    return ReturnCode.Error;
                }
            }
            finally
            {
                ExitLock(ref locked); /* TRANSACTIONAL */
            }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method closes (removes) the screen buffer with the specified
        /// name.
        /// </summary>
        /// <param name="name">
        /// The name of the screen buffer to close.
        /// </param>
        /// <param name="active">
        /// Non-zero to permit closing the buffer even when it is currently
        /// active; otherwise, attempting to close an active buffer fails.
        /// </param>
        /// <param name="error">
        /// Upon failure, receives an error message.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise, an appropriate
        /// error code.
        /// </returns>
        public static ReturnCode CloseScreenBuffer(
            string name,     /* in */
            bool active,     /* in */
            ref Result error /* out */
            )
        {
            bool locked = false;

            try
            {
                TryLock(ref locked); /* TRANSACTIONAL */

                if (locked)
                {
                    if (name == null)
                    {
                        error = "invalid screen buffer name";
                        return ReturnCode.Error;
                    }

                    if ((screenBuffers == null) ||
                        !screenBuffers.ContainsKey(name))
                    {
                        error = String.Format(
                            "screen buffer {0} not found",
                            FormatOps.WrapOrNull(name));

                        return ReturnCode.Error;
                    }

                    if (!active &&
                        (activeScreenNames != null) &&
                        (activeScreenNames.Count > 0) &&
                        SharedStringOps.SystemEquals(
                            name, activeScreenNames.Peek()))
                    {
                        error = String.Format(
                            "screen buffer {0} is active",
                            FormatOps.WrapOrNull(name));

                        return ReturnCode.Error;
                    }

                    screenBuffers.Remove(name);
                    return ReturnCode.Ok;
                }
                else
                {
                    TraceOps.LockTrace(
                        "CloseScreenBuffer",
                        typeof(AnsiConsole).Name, true,
                        TracePriority.LockError,
                        MaybeWhoHasLock());

                    error = "unable to acquire lock";
                    return ReturnCode.Error;
                }
            }
            finally
            {
                ExitLock(ref locked); /* TRANSACTIONAL */
            }
        }
        #endregion
    }
}
