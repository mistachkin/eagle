/*
 * GetRefs.cs --
 *
 * Extensible Adaptable Generalized Logic Engine (Eagle)
 * Assembly References Tool
 *
 * Copyright (c) 2007-2012 by Joe Mistachkin.  All rights reserved.
 *
 * See the file "license.terms" for information on usage and redistribution of
 * this file, and for a DISCLAIMER OF ALL WARRANTIES.
 *
 * RCS: @(#) $Id: $
 */

using System;
using System.Reflection;
using System.Runtime.InteropServices;

///////////////////////////////////////////////////////////////////////////////

#region Compile / Build Commands
//
// csc.exe "/out:..\GetRefs.exe" /debug:pdbonly /optimize+ /delaysign+
//         "/keyfile:..\..\..\Keys\EagleFastPublic.snk" "GetRefs.cs"
//
// sn.exe -Ra "..\GetRefs.exe" "..\..\..\Keys\EagleFastPrivate.snk"
//
// SignCode.exe -spc "%SPC_FILE%" -v "%PVK_FILE%" -n "GetRefs Tool"
//              -i "%SIGN_URL%" -a sha1
//              -t "http://timestamp.verisign.com/scripts/timstamp.dll"
//              -tr 10 -tw 60 "..\GetRefs.exe"
//
#endregion

///////////////////////////////////////////////////////////////////////////////

#region Assembly Metadata
[assembly: AssemblyTitle("GetRefs Tool")]
[assembly: AssemblyDescription("Show all assembly references.")]
[assembly: AssemblyCompany("Eagle Development Team")]
[assembly: AssemblyProduct("Eagle")]
[assembly: AssemblyCopyright(
        "Copyright © 2007-2012 by Joe Mistachkin.  All rights reserved.")]
[assembly: ComVisible(false)]
[assembly: Guid("8afa70ff-0b89-4554-8413-0f4cf817c471")]
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
    public class GetRefs
    {
        #region Application Entry Point
        public static int Main(string[] args)
        {
            if (args == null)
            {
                Console.WriteLine("Missing command line arguments.");
                return 1;
            }

            if (args.Length != 1)
            {
                Console.WriteLine("usage: GetRefs.exe <assemblyFile>");
                return 2;
            }

            string fileName = args[0];

            try
            {
                Assembly assembly = Assembly.ReflectionOnlyLoadFrom(
                    fileName); /* throw */

                if (assembly == null)
                {
                    Console.WriteLine(
                        "Could not load assembly \"{0}\".",
                        fileName);

                    return 3;
                }

                AssemblyName[] assemblyNames =
                    assembly.GetReferencedAssemblies();

                if (assemblyNames == null)
                {
                    Console.WriteLine(
                        "No references for assembly \"{0}\".",
                        fileName);

                    return 4;
                }

                foreach (AssemblyName assemblyName in assemblyNames)
                {
                    if (assemblyName == null)
                        continue;

                    Console.WriteLine(assemblyName);
                }

                return 0;
            }
            catch (Exception e)
            {
                Console.WriteLine(
                    "Caught exception for assembly \"{0}\": {1}",
                    fileName, e);

                return -1;
            }
        }
        #endregion
    }
}
