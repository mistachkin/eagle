/*
 * BinaryEditor.cs --
 *
 * Extensible Adaptable Generalized Logic Engine (Eagle)
 * Binary File Editor Tool
 *
 * Copyright (c) 2007-2012 by Joe Mistachkin.  All rights reserved.
 *
 * See the file "license.terms" for information on usage and redistribution of
 * this file, and for a DISCLAIMER OF ALL WARRANTIES.
 *
 * RCS: @(#) $Id: $
 */

using System;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;

///////////////////////////////////////////////////////////////////////////////

#region Compile / Build Commands
//
// csc.exe "/out:..\BinaryEditor.exe" /debug:pdbonly /optimize+ /delaysign+
//         "/keyfile:..\..\..\Keys\EagleFastPublic.snk" "BinaryEditor.cs"
//
// sn.exe -Ra "..\BinaryEditor.exe" "..\..\..\Keys\EagleFastPrivate.snk"
//
// SignCode.exe -spc "%SPC_FILE%" -v "%PVK_FILE%" -n "BinaryEditor Tool"
//              -i "%SIGN_URL%" -a sha1
//              -t "http://timestamp.verisign.com/scripts/timstamp.dll"
//              -tr 10 -tw 60 "..\BinaryEditor.exe"
//
#endregion

///////////////////////////////////////////////////////////////////////////////

#region Assembly Metadata
[assembly: AssemblyTitle("BinaryEditor Tool")]
[assembly: AssemblyDescription("Search-and-replace bytes within a file.")]
[assembly: AssemblyCompany("Eagle Development Team")]
[assembly: AssemblyProduct("Eagle")]
[assembly: AssemblyCopyright(
        "Copyright © 2007-2012 by Joe Mistachkin.  All rights reserved.")]
[assembly: ComVisible(false)]
[assembly: Guid("8f759cf5-e913-46df-b339-a6dcb0c094c3")]
[assembly: AssemblyVersion("1.1.*")]

#if DEBUG
[assembly: AssemblyConfiguration("Debug")]
#else
[assembly: AssemblyConfiguration("Release")]
#endif
#endregion

///////////////////////////////////////////////////////////////////////////////

namespace Tools
{
    public class BinaryEditor
    {
        #region Private Constants
        private const StringComparison DefaultOptionComparison =
            StringComparison.OrdinalIgnoreCase;

        ///////////////////////////////////////////////////////////////////////

        private const string NoStrictOption = "-noStrict";
        private const string NoCaseOption = "-noCase";
        private const string PathsOption = "-paths";
        private const string WhatIfOption = "-whatIf";
        private const string DebugOption = "-debug";
        private const string VerboseOption = "-verbose";
        private const string EndOfOptions = "--";

        ///////////////////////////////////////////////////////////////////////

        private const char CharHighByte = (char)0xFF00;
        private const char CharLowByte = (char)byte.MaxValue;
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Introspection Methods
        private static string GetFileName()
        {
            Process process = Process.GetCurrentProcess();

            if (process != null)
            {
                try
                {
                    ProcessModule module = process.MainModule;

                    if (module != null)
                        return module.FileName;
                }
                catch
                {
                    // do nothing.
                }
            }

            return null;
        }

        ///////////////////////////////////////////////////////////////////////

        private static Version GetVersion(
            Assembly assembly
            )
        {
            if (assembly != null)
            {
                try
                {
                    AssemblyName assemblyName = assembly.GetName();

                    if (assemblyName != null)
                        return assemblyName.Version;
                }
                catch
                {
                    // do nothing.
                }
            }

            return null;
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Diagnostic Output Methods
        private static void WriteLineIf(
            bool condition,
            string format,
            params object[] args
            )
        {
            if (condition)
                Console.WriteLine(format, args);
        }

        ///////////////////////////////////////////////////////////////////////

        private static void WriteErrorIf(
            bool condition,
            string format,
            params object[] args
            )
        {
            if (condition)
            {
                ConsoleColor savedForegroundColor = Console.ForegroundColor;
                Console.ForegroundColor = ConsoleColor.Red;

                try
                {
                    Console.WriteLine(String.Format(
                        "Error: {0}", format), args);
                }
                finally
                {
                    Console.ForegroundColor = savedForegroundColor;
                }
            }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Error Reporting Methods
        private static int Fail(
            string error,
            bool usage
            )
        {
            if (!String.IsNullOrEmpty(error))
            {
                WriteErrorIf(true, error);
                Console.WriteLine();
            }

            if (usage)
            {
                Assembly assembly = Assembly.GetExecutingAssembly();

                ///////////////////////////////////////////////////////////////

                string fileName = GetFileName();

                if ((fileName == null) && (assembly != null))
                    fileName = assembly.Location;

                if (!String.IsNullOrEmpty(fileName))
                    fileName = Path.GetFileName(fileName);
                else
                    fileName = "<unknown>";

                ///////////////////////////////////////////////////////////////

                ConsoleColor savedForegroundColor = Console.ForegroundColor;

                Console.ForegroundColor = ConsoleColor.White;

                Console.WriteLine(String.Format(
                    "{0} v{1}", fileName, GetVersion(assembly)));

                Console.ForegroundColor = savedForegroundColor;

                Console.WriteLine();

                ///////////////////////////////////////////////////////////////

                Console.WriteLine(String.Format(
                    "usage: {0} [options] <inFile> <outFile> <oldValue> " +
                    "<newValue>", fileName));

                Console.WriteLine();

                ///////////////////////////////////////////////////////////////

                Console.WriteLine(String.Format(
                    "The \"{0}\" option can be used to disable strict " +
                    "mode.  No error will be{1}generated if the original " +
                    "string is not found.", NoStrictOption,
                    Environment.NewLine));

                Console.WriteLine();

                ///////////////////////////////////////////////////////////////

                Console.WriteLine(String.Format(
                    "The \"{0}\" option can be used to cause the string " +
                    "comparisons to be{1}case-insensitive.", NoCaseOption,
                    Environment.NewLine));

                Console.WriteLine();

                ///////////////////////////////////////////////////////////////

                Console.WriteLine(String.Format(
                    "The \"{0}\" option can be used treat the original " +
                    "and replacement strings as{1}paths.  If either of the " +
                    "strings is a rooted path it will be converted to a{1}" +
                    "canonicalized path.", PathsOption, Environment.NewLine));

                Console.WriteLine();

                ///////////////////////////////////////////////////////////////

                Console.WriteLine(String.Format(
                    "The \"{0}\" option can be used to simulate the " +
                    "operation without making any{1}persistent modifications.",
                    WhatIfOption, Environment.NewLine));

                Console.WriteLine();

                ///////////////////////////////////////////////////////////////

                Console.WriteLine(String.Format(
                    "The \"{0}\" option can be used to output extra " +
                    "diagnostic information (i.e.{1}information which can " +
                    "be useful in troubleshooting).", VerboseOption,
                    Environment.NewLine));

                Console.WriteLine();

                ///////////////////////////////////////////////////////////////

                Console.WriteLine(String.Format(
                    "The \"{0}\" option can be used to output extra " +
                    "diagnostic information (i.e.{1}information which can " +
                    "be useful in debugging this tool).", DebugOption,
                    Environment.NewLine));

                Console.WriteLine();

                ///////////////////////////////////////////////////////////////

                Console.WriteLine(String.Format(
                    "The \"{0}\" option marks the end of options.  The " +
                    "argument following this one will{1}not be treated as " +
                    "an option even if it starts with a dash.",
                    EndOfOptions, Environment.NewLine));

                Console.WriteLine();
            }

            return 1; /* FAILURE */
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Path Handling Methods
        private static string CanonicalizePath(
            string path
            )
        {
            if (!String.IsNullOrEmpty(path) && Path.IsPathRooted(path))
                return Path.GetFullPath(path);

            return path;
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Bytes-To-Chars / Chars-To-Bytes Methods
        private static bool ConvertBytesToChars(
            bool verbose,
            byte[] bytes,
            ref char[] chars
            )
        {
            try
            {
                if (bytes == null)
                {
                    WriteErrorIf(verbose,
                        "invalid byte array");

                    return false;
                }

                int count = bytes.Length;

                if (count <= 0)
                {
                    WriteErrorIf(verbose,
                        "no bytes to convert");

                    return false;
                }

                chars = new char[count];

                for (int index = 0; index < count; index++)
                    chars[index] = (char)bytes[index];

                return true;
            }
            catch (Exception e)
            {
                WriteErrorIf(verbose, "{0}", e);
            }

            return false;
        }

        ///////////////////////////////////////////////////////////////////////

        private static bool ConvertCharsToBytes(
            bool verbose,
            char[] chars,
            ref byte[] bytes
            )
        {
            try
            {
                if (chars == null)
                {
                    WriteErrorIf(verbose,
                        "invalid character array");

                    return false;
                }

                int count = chars.Length;

                if (count <= 0)
                {
                    WriteErrorIf(verbose,
                        "no characters to convert");

                    return false;
                }

                bytes = new byte[count];

                for (int index = 0; index < count; index++)
                {
                    //
                    // NOTE: Make double-sure that we do not discard any
                    //       information contained in the character.  If we
                    //       would be forced to discard something, raise an
                    //       error and abort the conversion.
                    //
                    if ((chars[index] & CharHighByte) != 0)
                    {
                        WriteErrorIf(verbose,
                            "character at index {0} has a value " +
                            "of {1}, which cannot fit into a byte",
                            index, (short)chars[index]);

                        return false;
                    }

                    bytes[index] = (byte)(chars[index] & CharLowByte);
                }

                return true;
            }
            catch (Exception e)
            {
                WriteErrorIf(verbose, "{0}", e);
            }

            return false;
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Search / Replace Methods
        private static bool SearchAndReplace(
            bool strict,
            bool verbose,
            bool sameLength,
            bool noCase,
            string oldValue,
            string newValue,
            char[] oldChars,
            ref char[] newChars
            )
        {
            try
            {
                if (oldChars == null)
                {
                    WriteErrorIf(verbose,
                        "invalid old character array");

                    return false;
                }

                if (String.IsNullOrEmpty(oldValue))
                {
                    WriteErrorIf(verbose,
                        "old value cannot be empty");

                    return false;
                }

                if (newValue == null)
                    newValue = String.Empty;

                if (sameLength)
                {
                    //
                    // NOTE: The caller specified that the length of the old
                    //       and new character arrays must be identical.
                    //
                    int oldLength = oldValue.Length;
                    int newLength = newValue.Length;

                    if (newLength > oldLength)
                    {
                        //
                        // NOTE: New string is longer, truncate to the same
                        //       length.
                        //
                        newValue = newValue.Substring(0, oldLength);
                    }
                    else if (newLength < oldLength)
                    {
                        //
                        // NOTE: New string is shorter, pad with null
                        //       characters to the same length.
                        //
                        newValue = newValue +
                            new string('\0', oldLength - newLength);
                    }
                }

                //
                // NOTE: Nothing fancy here.  We do not even bother using a
                //       StringBuilder because we are loop-free.
                //
                string value = new string(oldChars);

                if (noCase)
                {
                    //
                    // NOTE: The regular expression is only being used here
                    //       because it supplies us with a very cheap way to
                    //       replace the string while ignoring case.
                    //
                    RegexOptions regexOptions = noCase ?
                        RegexOptions.IgnoreCase : RegexOptions.None;

                    value = Regex.Replace(
                        value, Regex.Escape(oldValue), newValue,
                        regexOptions);
                }
                else
                {
                    value = value.Replace(oldValue, newValue);
                }

                //
                // NOTE: Grab the underlying character array from the newly
                //       modified value.
                //
                newChars = value.ToCharArray();

                //
                // NOTE: Make sure we have exactly the same number of
                //       characters in the new character array if the caller
                //       specified that the lengths must be the same.  Also,
                //       make sure at least one character has been changed.
                //
                if (sameLength)
                {
                    if (newChars.Length == oldChars.Length)
                    {
                        for (int index = 0; index < newChars.Length; index++)
                            if (newChars[index] != oldChars[index])
                                return true;

                        WriteErrorIf(verbose,
                            "old value was not found");

                        return !strict;
                    }
                    else
                    {
                        WriteErrorIf(verbose,
                            "length mismatch between old and new " +
                            "character arrays");
                    }
                }
                else
                {
                    return true;
                }
            }
            catch (Exception e)
            {
                WriteErrorIf(verbose, "{0}", e);
            }

            return false;
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Application Entry Point
        public static int Main(string[] args)
        {
            int exitCode = 0;

            //
            // NOTE: Make sure the array of arguments is valid.
            //
            if (args == null)
            {
                exitCode = Fail(
                    "invalid arguments",
                    true);

                goto done;
            }

            //
            // NOTE: We always require at least 3 (non-option) arguments.
            //
            int argc = args.Length;

            if (argc < 4)
            {
                exitCode = Fail(
                    "not enough arguments",
                    true);

                goto done;
            }

            //
            // NOTE: Setup the default values for all the available options
            //       (e.g. "strict", "noCase", "paths", "whatIf", "verbose",
            //       and "debug").  By default, strict mode is enabled; all
            //       other options are disabled by default.
            //
            bool strict = true;
            bool noCase = false;
            bool paths = false;
            bool whatIf = false;
            bool verbose = false;
            bool debug = false;

            //
            // NOTE: Process all the command line arguments in order starting
            //       with the first one.  After the option processing 'for'
            //       loop, this index should point to the first non-option
            //       argument.
            //
            int argi = 0;

            //
            // NOTE: Search for all the supported options.  If an argument is
            //       not recognized as a supported option it will be treated
            //       as the first non-option argument.
            //
            for (; argi < argc; argi++)
            {
                if (String.Compare(args[argi], NoStrictOption,
                        DefaultOptionComparison) == 0)
                {
                    strict = false;

                    WriteLineIf(debug,
                        "strict mode is now disabled");

                    continue;
                }
                else if (String.Compare(args[argi], NoCaseOption,
                        DefaultOptionComparison) == 0)
                {
                    noCase = true;

                    WriteLineIf(debug,
                        "case-insensitive mode is now enabled");

                    continue;
                }
                if (String.Compare(args[argi], PathsOption,
                        DefaultOptionComparison) == 0)
                {
                    paths = true;

                    WriteLineIf(debug,
                        "path canonicalization mode is now enabled");

                    continue;
                }
                else if (String.Compare(args[argi], WhatIfOption,
                        DefaultOptionComparison) == 0)
                {
                    whatIf = true;

                    WriteLineIf(debug,
                        "what-if mode is now enabled");

                    continue;
                }
                else if (String.Compare(args[argi], VerboseOption,
                        DefaultOptionComparison) == 0)
                {
                    verbose = true;

                    WriteLineIf(debug,
                        "verbose mode is now enabled");

                    continue;
                }
                else if (String.Compare(args[argi], DebugOption,
                        DefaultOptionComparison) == 0)
                {
                    debug = true;

                    WriteLineIf(debug,
                        "debug mode is now enabled");

                    continue;
                }
                else if (String.Compare(args[argi], EndOfOptions,
                        DefaultOptionComparison) == 0)
                {
                    //
                    // NOTE: Advance past this option.  We break out of this
                    //       loop to continue processing all the non-option
                    //       arguments.
                    //
                    argi++;

                    WriteLineIf(debug,
                        "found explicit end-of-options at index {0}",
                        argi);

                    break;
                }
                else
                {
                    //
                    // NOTE: This argument is not a valid option; therefore,
                    //       it must be the first non-option argument.  We
                    //       break out of this loop to continue processing all
                    //       the non-option arguments.
                    //
                    WriteLineIf(debug,
                        "found implicit end-of-options at index {0}",
                        argi);

                    break;
                }
            }

            //
            // NOTE: Make sure there are exactly enough arguments left to
            //       satisfy the required arguments we need.
            //
            if ((argi + 4) != argc)
            {
                exitCode = Fail(
                    "wrong number of arguments after options",
                    true);

                goto done;
            }

            //
            // NOTE: Make sure the input file name is valid.
            //
            string inputFileName = args[argi];

            WriteLineIf(debug,
                "input file name is \"{0}\"",
                inputFileName);

            if (String.IsNullOrEmpty(inputFileName))
            {
                exitCode = Fail(
                    "invalid or empty input file name",
                    true);

                goto done;
            }

            //
            // NOTE: Make sure the input file exists.
            //
            if (!File.Exists(inputFileName))
            {
                exitCode = Fail(
                    "input file must already exist",
                    true);

                goto done;
            }

            //
            // NOTE: Make sure the output file name is valid.
            //
            string outputFileName = args[argi + 1];

            WriteLineIf(debug,
                "output file name is \"{0}\"",
                outputFileName);

            if (String.IsNullOrEmpty(outputFileName))
            {
                exitCode = Fail(
                    "invalid or empty output file name",
                    true);

                goto done;
            }

            //
            // NOTE: Grab and transform the old value, if necessary, and then
            //       make sure it is [still] valid.
            //
            string oldValue = args[argi + 2];

            WriteLineIf(debug,
                "original string is \"{0}\"",
                oldValue);

            if (paths)
            {
                oldValue = CanonicalizePath(oldValue);

                WriteLineIf(debug,
                    "original string as canonicalized path is \"{0}\"",
                    oldValue);
            }

            if (String.IsNullOrEmpty(oldValue))
            {
                exitCode = Fail(
                    "original string cannot be empty",
                    true);

                goto done;
            }

            //
            // NOTE: Grab and transform the new value, if necessary, and then
            //       make sure it is [still] valid.
            //
            string newValue = args[argi + 3];

            WriteLineIf(debug,
                "replacement string is \"{0}\"",
                newValue);

            if (paths)
            {
                newValue = CanonicalizePath(newValue);

                WriteLineIf(debug,
                    "replacement string as canonicalized path is \"{0}\"",
                    newValue);
            }

            //
            // NOTE: Make sure the length of the new value does not exceed the
            //       length of the old value (i.e. we cannot support expanding
            //       the value inline because we do not [necessarily] know
            //       anything about the underlying file format).
            //
            if (newValue.Length > oldValue.Length)
            {
                exitCode = Fail(
                    "replacement string cannot be longer than original string",
                    true);

                goto done;
            }

            //
            // NOTE: Attempt to read all the old bytes from the input file.
            //
            byte[] oldBytes = File.ReadAllBytes(inputFileName); /* throw */

            WriteLineIf(debug,
                "read {0} bytes from file \"{1}\"",
                oldBytes.Length, inputFileName);

            //
            // NOTE: Attempt to convert the input bytes to characters.
            //
            char[] oldChars = null;

            if (!ConvertBytesToChars(verbose, oldBytes, ref oldChars))
            {
                exitCode = Fail(
                    "could not convert bytes to characters",
                    false);

                goto done;
            }

            //
            // NOTE: Search for and replace all instances of the old value
            //       with the new value.
            //
            char[] newChars = null;

            if (!SearchAndReplace(
                    strict, verbose, true, noCase, oldValue, newValue,
                    oldChars, ref newChars))
            {
                exitCode = Fail(
                    "could not search and replace",
                    false);

                goto done;
            }

            //
            // NOTE: Attempt to convert the output characters to bytes.
            //
            byte[] newBytes = null;

            if (!ConvertCharsToBytes(verbose, newChars, ref newBytes))
            {
                exitCode = Fail(
                    "could not convert characters to bytes",
                    false);

                goto done;
            }

            //
            // NOTE: Attempt to write all the new bytes to the output file.
            //
            if (!whatIf)
                File.WriteAllBytes(outputFileName, newBytes); /* throw */

            WriteLineIf(debug,
                "wrote {0} bytes to file \"{1}\"",
                newBytes.Length, outputFileName);

        done:
            return exitCode;
        }
        #endregion
    }
}
