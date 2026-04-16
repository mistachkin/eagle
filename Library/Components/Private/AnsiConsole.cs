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
        private static string Escape = "\x1B";
        private static string AltScreenEnable = Escape + "[?1049h";
        private static string AltScreenDisable = Escape + "[?1049l";
        private static string CursorSave = Escape + "7";
        private static string CursorRestore = Escape + "8";
        private static string ScreenClear = Escape + "[2J";
        private static string CursorHome = Escape + "[H";
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Private Data
        //
        // NOTE: This is used to synchronize access to the screen buffer
        //       state managed by this class.
        //
        private static readonly object syncRoot = new object();

        ///////////////////////////////////////////////////////////////////////

        //
        // NOTE: Thread ID tracking for lock diagnostics.
        //
        private static long lockThreadId = 0;

        ///////////////////////////////////////////////////////////////////////

        //
        // NOTE: Stack of saved screen content for the push/pop emulation.
        //       Each entry is the screen name that was pushed.
        //
        private static ScreenStack activeScreenNames;

        ///////////////////////////////////////////////////////////////////////

        //
        // NOTE: Set of known screen buffer names.
        //
        private static ScreenDictionary screenBuffers;
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Lock Helper Methods
        private static long MaybeWhoHasLock()
        {
            return Interlocked.CompareExchange(ref lockThreadId, 0, 0);
        }

        ///////////////////////////////////////////////////////////////////////

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
        public static bool IsSupported()
        {
            //
            // NOTE: ANSI "alternative screen buffers" are supported on
            //       virtually all modern terminals.  First, check that
            //       standard output is not redirected (i.e. because an
            //       ANSI escape sequence is meaningless for files).
            //
            return !System.Console.IsOutputRedirected;
        }

        ///////////////////////////////////////////////////////////////////////

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
