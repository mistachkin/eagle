/*
 * LineEditor.cs --
 *
 * Copyright (c) 2007-2012 by Joe Mistachkin.  All rights reserved.
 *
 * See the file "license.terms" for information on usage and redistribution of
 * this file, and for a DISCLAIMER OF ALL WARRANTIES.
 *
 * RCS: @(#) $Id: $
 */

using System;
using System.Runtime.InteropServices;
using System.Security;

#if !NET_40
using System.Security.Permissions;
#endif

using Eagle._Attributes;
using Eagle._Components.Public;
using Eagle._Constants;

#if UNIX
using UNM = Eagle._Components.Private.LineEditor.UnsafeNativeMethods;
#endif

namespace Eagle._Components.Private
{
#if NET_40
    [SecurityCritical()]
#else
    [SecurityPermission(SecurityAction.LinkDemand, UnmanagedCode = true)]
#endif
    [ObjectId("5bb66fa3-45fa-49ec-b379-970ad63c24b9")]
    internal static class LineEditor
    {
        #region Private Delegates
#if UNIX
        [ObjectId("d910dca8-e241-4b72-9926-7f80c889a949")]
        private delegate IntPtr ReadLineCallback(string prompt);

        ///////////////////////////////////////////////////////////////////////

        [ObjectId("32f5d228-5409-41a9-86e4-af4ee653d28e")]
        private delegate void AddHistoryCallback(string line);
#endif
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Private Constants
#if UNIX
        //
        // NOTE: The name of the native global variable that controls
        //       whether the prompt has already been displayed by the
        //       caller.  This symbol is exported by GNU readline and
        //       BSD libedit.
        //
        private static readonly string AlreadyPromptedName =
            "rl_already_prompted";
#endif
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Private Data
        private static bool? available = null;

        ///////////////////////////////////////////////////////////////////////

#if UNIX
        private static ReadLineCallback readLineCallback = null;
        private static AddHistoryCallback addHistoryCallback = null;

        ///////////////////////////////////////////////////////////////////////

        //
        // NOTE: Pointer to the rl_already_prompted global
        //       variable within a native readline library.
        //       When this is set to a non-zero value before
        //       calling readline, it tells readline that
        //       the specified prompt string is already on
        //       screen and should not be reprinted -- but
        //       readline still uses the specified  prompt
        //       string for cursor positioning calculations
        //       during history navigation.
        //
        private static IntPtr alreadyPrompted = IntPtr.Zero;
#endif
        #endregion

        ///////////////////////////////////////////////////////////////////////
        // Required Native APIs used via P/Invoke
        ///////////////////////////////////////////////////////////////////////

        #region Unsafe Native Methods Class
#if UNIX
        [SuppressUnmanagedCodeSecurity()]
        [ObjectId("918e4e30-5c76-4b53-8f6c-2c3230d3fa5e")]
        internal static class UnsafeNativeMethods
        {
            #region BSD "libedit" Native Methods
            [DllImport(DllName.Edit, EntryPoint = "readline",
                CallingConvention = CallingConvention.Cdecl,
                CharSet = CharSet.Ansi)]
            internal static extern IntPtr bsd_readline(
                string prompt /* in: OPTIONAL */
            );

            ///////////////////////////////////////////////////////////

            [DllImport(DllName.Edit, EntryPoint = "add_history",
                CallingConvention = CallingConvention.Cdecl,
                CharSet = CharSet.Ansi)]
            internal static extern void bsd_add_history(
                string line /* in */
            );
            #endregion

            ///////////////////////////////////////////////////////////

            #region GNU "readline" Native Methods
            [DllImport(DllName.ReadLine, EntryPoint = "readline",
                CallingConvention = CallingConvention.Cdecl,
                CharSet = CharSet.Ansi)]
            internal static extern IntPtr gnu_readline(
                string prompt /* in: OPTIONAL */
            );

            ///////////////////////////////////////////////////////////

            [DllImport(DllName.ReadLine, EntryPoint = "add_history",
                CallingConvention = CallingConvention.Cdecl,
                CharSet = CharSet.Ansi)]
            internal static extern void gnu_add_history(
                string line /* in */
            );
            #endregion

            ///////////////////////////////////////////////////////////

            #region Common "libc" Native Methods
            [DllImport(DllName.LibC, EntryPoint = "free",
                CallingConvention = CallingConvention.Cdecl)]
            internal static extern void libc_free(
                IntPtr ptr /* in */
            );
            #endregion
        }
#endif
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Private Methods
#if UNIX
        private static bool TrySetupAlreadyPrompted(
            string fileName /* in */
            )
        {
            if ((available == null) || !(bool)available)
                return false;

            if (String.IsNullOrEmpty(fileName))
                return false;

            IntPtr module = IntPtr.Zero;

            try
            {
                //
                // NOTE: Try to resolve address of the rl_already_prompted
                //       variable in the native library.  This is exported
                //       by both GNU readline and BSD libedit.
                //
                int lastError; /* NOT USED */

                module = NativeOps.LoadLibrary(fileName, out lastError);

                if (module != IntPtr.Zero)
                {
                    alreadyPrompted = NativeOps.GetProcAddress(
                        module, AlreadyPromptedName, out lastError);

                    return (alreadyPrompted != IntPtr.Zero);
                }
            }
            catch (Exception e)
            {
                TraceOps.DebugTrace(
                    e, typeof(LineEditor).Name,
                    TracePriority.NativeError);
            }

            return false;
        }
#endif
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Public Methods
        //
        // NOTE: Checks whether native readline is available on this platform.
        //       The result is cached after the first call.  Returns false on
        //       Windows, as "conhost.exe" provides line editing and history
        //       via System.Console, et al.
        //
        public static bool IsAvailable()
        {
            if (available != null)
                return (bool)available;

#if UNIX
            if (!PlatformOps.IsWindowsOperatingSystem())
            {
                try
                {
                    string fileName = null;

                    if (PlatformOps.IsLinuxOperatingSystem())
                    {
                        /* NO RESULT */
                        UNM.gnu_add_history(String.Empty); /* throw */

                        readLineCallback = new ReadLineCallback(
                            UNM.gnu_readline);

                        addHistoryCallback = new AddHistoryCallback(
                            UNM.gnu_add_history);

                        fileName = DllName.ReadLine;
                        available = true;
                    }
                    else if (PlatformOps.IsMacintoshOperatingSystem())
                    {
                        /* NO RESULT */
                        UNM.bsd_add_history(String.Empty); /* throw */

                        readLineCallback = new ReadLineCallback(
                            UNM.bsd_readline);

                        addHistoryCallback = new AddHistoryCallback(
                            UNM.bsd_add_history);

                        fileName = DllName.Edit;
                        available = true;
                    }
                    else
                    {
                        available = false;
                    }

                    if ((bool)available && (fileName != null) &&
                        !TrySetupAlreadyPrompted(fileName))
                    {
                        TraceOps.DebugTrace(String.Format(
                            "IsAvailable: native line editor " +
                            "{0} symbol {1} unavailable",
                            FormatOps.WrapOrNull(fileName),
                            AlreadyPromptedName),
                            typeof(LineEditor).Name,
                            TracePriority.NativeWarning);
                    }
                }
                catch (Exception e)
                {
                    TraceOps.DebugTrace(
                        e, typeof(LineEditor).Name,
                        TracePriority.NativeError);

                    available = false;
                }
            }
            else
#endif
            {
                available = false;
            }

            TracePriority priority = PlatformOps.IsWindowsOperatingSystem() ?
                TracePriority.NativeDebug : TracePriority.NativeWarning;

            TraceOps.DebugTrace(String.Format(
                "IsAvailable: native line editor is {0}",
                (bool)available ? "available" : "unavailable"),
                typeof(LineEditor).Name, priority);

            return (bool)available;
        }

        ///////////////////////////////////////////////////////////////////////

        //
        // NOTE: Returns non-zero if the rl_already_prompted variable was
        //       resolved and should be used to suppress prompt redisplay.
        //       On GNU readline (Linux) this works.  On BSD libedit (e.g.
        //       macOS) this variable may exist but is (apparently) not
        //       honored.
        //
        public static bool HasAlreadyPrompted()
        {
#if UNIX
            return (alreadyPrompted != IntPtr.Zero) &&
                   PlatformOps.IsLinuxOperatingSystem();
#else
            return false;
#endif
        }

        ///////////////////////////////////////////////////////////////////////

        //
        // NOTE: Read one line of input using native readline, providing full
        //       interactive line editing (arrow keys, Home, End, etc.) -AND-
        //       history navigation (up / down arrows).  Returns null on EOF
        //       (Ctrl+D).  Non-empty lines are automatically added to the
        //       readline history.
        //
        public static string ReadLine(
            string prompt /* in: OPTIONAL */
            )
        {
            if ((available == null) || !(bool)available)
                return null;

#if UNIX
            if (readLineCallback == null)
                return null;

            IntPtr line = IntPtr.Zero;
            bool setAlreadyPrompted = false;

            try
            {
                //
                // NOTE: If a prompt was provided and the
                //       rl_already_prompted variable was
                //       resolved, set it to non-zero to
                //       tell readline that the prompt is
                //       already on-screen.  It will skip
                //       displaying the prompt but still
                //       use it for cursor positioning
                //       calculations during navigation.
                //
                if (!String.IsNullOrEmpty(prompt) &&
                    (alreadyPrompted != IntPtr.Zero))
                {
                    Marshal.WriteInt32(
                        alreadyPrompted, 1); /* throw */

                    setAlreadyPrompted = true;
                }

                line = readLineCallback(prompt);

                if (line == IntPtr.Zero)
                    return null; /* end-of-file? */

                string text;

#if NET_STANDARD_20 && NET_STANDARD_21
                text = Marshal.PtrToStringUTF8(line);
#else
                text = Marshal.PtrToStringAnsi(line);
#endif

                if (!String.IsNullOrEmpty(text) &&
                    (addHistoryCallback != null))
                {
                    /* NO RESULT */
                    addHistoryCallback(text);
                }

                return text;
            }
            catch (Exception e)
            {
                TraceOps.DebugTrace(
                    e, typeof(LineEditor).Name,
                    TracePriority.NativeError);
            }
            finally
            {
                if (line != IntPtr.Zero)
                {
                    UNM.libc_free(line); /* throw */
                    line = IntPtr.Zero;
                }

                if (setAlreadyPrompted &&
                    (alreadyPrompted != IntPtr.Zero))
                {
                    Marshal.WriteInt32(
                        alreadyPrompted, 0); /* throw */
                }
            }
#endif

            return null;
        }
        #endregion
    }
}
