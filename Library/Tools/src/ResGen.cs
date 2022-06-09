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
    public class ResGen
    {
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
                    "usage: {0} <resxFile> <resourcesFile> [baseDirectory]",
                    fileName));

                Console.WriteLine();
            }

            return 1; /* FAILURE */
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Resource Handling Methods
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
