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
using System.Resources;
using System.Runtime.InteropServices;
using Eagle._Attributes;
using Eagle._Components.Shared;

// General Information about an assembly is controlled through the following
// set of attributes. Change these attribute values to modify the information
// associated with an assembly.
#if SAMPLE && TCL
[assembly: AssemblyTitle("Sample Tcl Application")]
[assembly: AssemblyDescription("Sample of application Tcl integration with Eagle.")]
#elif SAMPLE
[assembly: AssemblyTitle("Sample Application")]
[assembly: AssemblyDescription("Sample of application integration with Eagle.")]
#else
[assembly: AssemblyTitle("Sample Plugin (TEST)")]
[assembly: AssemblyDescription("Sample of plugin/command extensibility with Eagle.")]
#endif

[assembly: AssemblyCompany("Eagle Development Team")]
[assembly: AssemblyProduct("Eagle")]
[assembly: AssemblyCopyright("Copyright © 2007-2012 by Joe Mistachkin.  All rights reserved.")]
[assembly: NeutralResourcesLanguage("en-US")]

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
#if SAMPLE && TCL
[assembly: Guid("69613aaf-dc17-4f52-9f3d-6605f8ec61a8")]
#elif SAMPLE
[assembly: Guid("0c2f95e3-4470-4ce8-bfc1-a8a9938a44d3")]
#else
[assembly: Guid("cf334272-2827-445b-90ff-7b34e28c45b3")]
#endif

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

#if SAMPLE && TCL
[assembly: AssemblyTag("TCLSAMPLE")]
#elif SAMPLE
[assembly: AssemblyTag("SAMPLE")]
#else
[assembly: AssemblyTag("PLUGIN")]
#endif

[assembly: AssemblyLicense(License.Summary, License.Text)]
[assembly: AssemblyUri("https://eagle.to/")]
[assembly: AssemblyUri("update", "https://urn.to/r/update_sample")]
[assembly: AssemblyUri("license", "https://urn.to/r/license")]
[assembly: AssemblyUri("provision", "https://urn.to/r/provision")]
