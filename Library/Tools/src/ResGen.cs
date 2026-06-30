/*
 * ResGen.cs --
 *
 * Extensible Adaptable Generalized Logic Engine (Eagle)
 * Resource Generator Tool
 *
 * Copyright (c) 2007-2012 by Joe Mistachkin.  All rights reserved.
 *
 * See the file "license.terms" for information on usage and redistribution of
 * this file, and for a DISCLAIMER OF ALL WARRANTIES.
 *
 * RCS: @(#) $Id: $
 */

using System;
using System.Collections;
using System.ComponentModel.Design;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Resources;
using System.Runtime.InteropServices;
using System.Text;

///////////////////////////////////////////////////////////////////////////////

#region Compile / Build Commands
//
// csc.exe "/out:..\ResGen.exe" /debug:pdbonly /optimize+ /delaysign+
//         "/keyfile:..\..\..\Keys\EagleFastPublic.snk" "ResGen.cs"
//
// sn.exe -Ra "..\ResGen.exe" "..\..\..\Keys\EagleFastPrivate.snk"
//
// SignCode.exe -spc "%SPC_FILE%" -v "%PVK_FILE%" -n "ResGen Tool"
//              -i "%SIGN_URL%" -a sha1
//              -t "http://timestamp.verisign.com/scripts/timstamp.dll"
//              -tr 10 -tw 60 "..\ResGen.exe"
//
#endregion

///////////////////////////////////////////////////////////////////////////////

#region Assembly Metadata
[assembly: AssemblyTitle("ResGen Tool")]
[assembly: AssemblyDescription("Generate resources from a \"resx\" file.")]
[assembly: AssemblyCompany("Eagle Development Team")]
[assembly: AssemblyProduct("Eagle")]
[assembly: AssemblyCopyright(
        "Copyright © 2007-2012 by Joe Mistachkin.  All rights reserved.")]
[assembly: ComVisible(false)]
[assembly: Guid("b731d083-6a3b-47e6-b004-ee9e8fb8e227")]
[assembly: AssemblyVersion("1.0.*")]

#if DEBUG
[assembly: AssemblyConfiguration("Debug")]
#else
[assembly: AssemblyConfiguration("Release")]
#endif
#endregion

///////////////////////////////////////////////////////////////////////////////

namespace Tools
{
    /// <summary>
    /// This class implements the ResGen tool, a command line utility that
    /// reads a managed resource definition ("resx") file and generates the
    /// corresponding binary resources file from it.
    /// </summary>
    public class ResGen
    {
        #region Introspection Methods
        /// <summary>
        /// This method attempts to determine the fully qualified file name of
        /// the main module for the current process.
        /// </summary>
        /// <returns>
        /// The file name of the main module for the current process -OR- null
        /// if it cannot be determined.
        /// </returns>
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

        /// <summary>
        /// This method attempts to determine the version of the specified
        /// assembly.
        /// </summary>
        /// <param name="assembly">
        /// The assembly to query.
        /// </param>
        /// <returns>
        /// The version of the specified assembly -OR- null if it cannot be
        /// determined.
        /// </returns>
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
        /// <summary>
        /// This method conditionally writes a formatted message, followed by a
        /// line terminator, to the console.
        /// </summary>
        /// <param name="condition">
        /// Non-zero to write the message; otherwise, nothing is written.
        /// </param>
        /// <param name="format">
        /// The composite format string to write.
        /// </param>
        /// <param name="args">
        /// The array of objects to format and write.
        /// </param>
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

        /// <summary>
        /// This method conditionally writes a formatted error message,
        /// followed by a line terminator, to the console using the color
        /// reserved for errors.  The previous console foreground color is
        /// restored before this method returns.
        /// </summary>
        /// <param name="condition">
        /// Non-zero to write the error message; otherwise, nothing is written.
        /// </param>
        /// <param name="format">
        /// The composite format string to write.
        /// </param>
        /// <param name="args">
        /// The array of objects to format and write.
        /// </param>
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
        /// <summary>
        /// This method displays an error message to the console and/or
        /// displays the version and command line usage information for this
        /// tool.
        /// </summary>
        /// <param name="error">
        /// The error message to display, if any.
        /// </param>
        /// <param name="usage">
        /// Non-zero to display the version and command line usage information.
        /// </param>
        /// <returns>
        /// Always returns one, the failure exit code for this tool.
        /// </returns>
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
                    "usage: {0} <resxFile> <resourcesFile> [baseDirectory]",
                    fileName));

                Console.WriteLine();
            }

            return 1; /* FAILURE */
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Resource Handling Methods
        /// <summary>
        /// This method reads all of the resources from the specified "resx"
        /// file and writes them, in binary form, to the specified resources
        /// file.  Resources that refer to an external file are read from that
        /// file using the appropriate encoding, if any.
        /// </summary>
        /// <param name="resxFileName">
        /// The name of the input "resx" file to read.
        /// </param>
        /// <param name="resourcesFileName">
        /// The name of the output binary resources file to write.
        /// </param>
        /// <param name="baseDirectory">
        /// The base directory used to resolve any relative file references
        /// contained within the input "resx" file, if any.
        /// </param>
        /// <returns>
        /// True if the resources were generated successfully; otherwise,
        /// false.
        /// </returns>
        private static bool GenerateResources(
            string resxFileName,
            string resourcesFileName,
            string baseDirectory
            )
        {
            try
            {
                using (ResXResourceReader reader = new ResXResourceReader(
                        resxFileName))
                {
                    using (ResourceWriter writer = new ResourceWriter(
                            resourcesFileName))
                    {
                        if (baseDirectory != null)
                            reader.BasePath = baseDirectory;

                        reader.UseResXDataNodes = true;

                        foreach (DictionaryEntry entry in reader)
                        {
                            ResXDataNode node = entry.Value as ResXDataNode;

                            if (node == null)
                                continue;

                            ResXFileRef file = node.FileRef;

                            if (file != null)
                            {
                                string fileName = file.FileName;
                                Encoding encoding = file.TextFileEncoding;

                                if (encoding != null)
                                {
                                    writer.AddResource(
                                        node.Name, File.ReadAllText(
                                        fileName, encoding));
                                }
                                else
                                {
                                    writer.AddResource(
                                        node.Name, File.ReadAllBytes(
                                        fileName));
                                }
                            }
                            else
                            {
                                object value = node.GetValue(
                                    (ITypeResolutionService)null);

                                writer.AddResource(node.Name, value);
                            }
                        }

                        writer.Generate();
                    }
                }

                return true;
            }
            catch (Exception e)
            {
                WriteErrorIf(true, "{0}", e);
            }

            return false;
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Application Entry Point
        /// <summary>
        /// This is the entry-point for this tool.  It handles processing the
        /// command line arguments, validating the input and output file names,
        /// and generating the binary resources file from the input "resx"
        /// file.
        /// </summary>
        /// <param name="args">
        /// The command line arguments.  Either two or three arguments are
        /// required: the input "resx" file name, the output resources file
        /// name, and an optional base directory.
        /// </param>
        /// <returns>
        /// Zero upon success; non-zero on failure.
        /// </returns>
        public static int Main(string[] args)
        {
            int exitCode = 0;

            //
            // NOTE: Make sure the array of arguments is valid.
            //
            if (args == null)
            {
                exitCode = Fail(
                    "invalid arguments", true);

                goto done;
            }

            //
            // NOTE: We always require either 2 or 3 arguments.
            //
            int argc = args.Length;

            if ((argc < 2) || (argc > 3))
            {
                exitCode = Fail(
                    "wrong number of arguments", true);

                goto done;
            }

            //
            // NOTE: Make sure the input file name is valid.
            //
            string resxFileName = args[0];

            WriteLineIf(true,
                "input file name is \"{0}\"", resxFileName);

            if (String.IsNullOrEmpty(resxFileName))
            {
                exitCode = Fail(
                    "invalid or empty input file name", true);

                goto done;
            }

            //
            // NOTE: Make sure the input file exists.
            //
            if (!File.Exists(resxFileName))
            {
                exitCode = Fail(
                    "input file must already exist", true);

                goto done;
            }

            //
            // NOTE: Make sure the output file name is valid.
            //
            string resourcesFileName = args[1];

            WriteLineIf(true,
                "output file name is \"{0}\"", resourcesFileName);

            if (String.IsNullOrEmpty(resourcesFileName))
            {
                exitCode = Fail(
                    "invalid or empty output file name",
                    true);

                goto done;
            }

            //
            // NOTE: Make sure the output file does not exist.
            //
            if (File.Exists(resourcesFileName))
            {
                exitCode = Fail(
                    "output file must not exist", true);

                goto done;
            }

            //
            // NOTE: Make sure the directory name is valid.
            //
            string baseDirectory = (argc >= 3) ? args[2] : null;

            WriteLineIf(true,
                "base directory is \"{0}\"", baseDirectory);

            //
            // NOTE: Attempt to actually generate the resources.
            //       This is the fun part.
            //
            if (!GenerateResources(
                    resxFileName, resourcesFileName, baseDirectory))
            {
                exitCode = Fail(
                    "failed to generate resources", false);

                goto done;
            }

        done:
            return exitCode;
        }
        #endregion
    }
}
