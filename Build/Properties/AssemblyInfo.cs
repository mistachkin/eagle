/*
 * AssemblyInfo.cs --
 *
 * Copyright (c) 2007-2012 by Joe Mistachkin.  All rights reserved.
 *
 * See the file "license.terms" for information on usage and redistribution of
 * this file, and for a DISCLAIMER OF ALL WARRANTIES.
 *
 * RCS: @(#) $Id: $
 */

using System.Reflection;
using System.Runtime.InteropServices;
using Eagle._Attributes;
using Eagle._Components.Shared;

// General Information about an assembly is controlled through the following 
// set of attributes. Change these attribute values to modify the information
// associated with an assembly.
[assembly: AssemblyTitle("Eagle Tasks")]
[assembly: AssemblyDescription("Extensible Adaptable Generalized Logic Engine")]
[assembly: AssemblyCompany("Eagle Development Team")]
[assembly: AssemblyProduct("Eagle")]
[assembly: AssemblyCopyright("Copyright © 2007-2012 by Joe Mistachkin.  All rights reserved.")]

#if DEBUG
[assembly: AssemblyConfiguration("Debug")]
#else
[assembly: AssemblyConfiguration("Release")]
#endif

// Setting ComVisible to false makes the types in this assembly not visible 
// to COM components.  If you need to access a type in this assembly from 
// COM, set the ComVisible attribute to true on that type.
[assembly: ComVisible(false)]

// The following GUID is for the ID of the typelib if this project is exposed to COM
[assembly: Guid("cd6d4298-7e02-4d71-bdd6-3ff5620ae125")]

// Version information for an assembly consists of the following four values:
//
//      Major Version
//      Minor Version 
//      Build Number
//      Revision
//
// You can specify all the values or you can default the Revision and Build Numbers 
// by using the '*' as shown below:
#if !PATCHLEVEL
[assembly: AssemblyVersion("1.0.*")]
#endif

//
// NOTE: Custom attributes for this assembly.
//
#if !ASSEMBLY_DATETIME
// [assembly: AssemblyDateTime()] /* NO: Explicit use only. */
#endif

[assembly: AssemblyTag("beta")]
[assembly: AssemblyLicense(License.Summary, License.Text)]
[assembly: AssemblyUri("https://eagle.to/")]
