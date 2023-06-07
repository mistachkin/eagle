/*
 * TclWrapper.cs --
 *
 * Copyright (c) 2007-2012 by Joe Mistachkin.  All rights reserved.
 *
 * See the file "license.terms" for information on usage and redistribution of
 * this file, and for a DISCLAIMER OF ALL WARRANTIES.
 *
 * RCS: @(#) $Id: $
 */

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;

#if !NET_STANDARD_20
using Microsoft.Win32;
#endif

using Eagle._Attributes;
using Eagle._Components.Private.Tcl.Delegates;
using Eagle._Components.Public;
using Eagle._Constants;
using Eagle._Containers.Private;
using Eagle._Containers.Private.Tcl;
using Eagle._Containers.Public;
using Eagle._Interfaces.Private.Tcl;
using Eagle._Interfaces.Public;

#if NET_STANDARD_21
using Index = Eagle._Constants.Index;
#endif

///////////////////////////////////////////////////////////////////////////////////////////////
// *WARNING* *WARNING* *WARNING* *WARNING* *WARNING* *WARNING* *WARNING* *WARNING* *WARNING*
//
// Please do not add any non-static members to this class.  It is not allowed to maintain any
// kind of state information because all Tcl/Tk state information is stored in the TclApi
// object(s).
//
// *WARNING* *WARNING* *WARNING* *WARNING* *WARNING* *WARNING* *WARNING* *WARNING* *WARNING*
///////////////////////////////////////////////////////////////////////////////////////////////

namespace Eagle._Components.Private.Tcl
{
    [ObjectId("22f1829e-895c-4d61-a300-a94c42877c02")]
#if TCL_WRAPPER
    public
#else
    internal
#endif
    static class TclWrapper
    {
        #region Private Constants
        #region ActiveTcl Naming Constants
        //
        // NOTE: If the fully qualified file name for a Tcl dynamic link
        //       library matches this pattern, it likely originated from
        //       ActiveState (i.e. it is likely ActiveTcl).
        //
        // HACK: This is purposely not read-only.
        //
        private static string ActiveStatePattern = "*ActiveState*";

        ///////////////////////////////////////////////////////////////////////////////////////////////

        //
        // NOTE: If the fully qualified file name for a Tcl dynamic link
        //       library matches this pattern, it is likely ActiveTcl.
        //
        // HACK: This is purposely not read-only.
        //
        private static string ActiveTclPattern = "*ActiveTcl*";

        ///////////////////////////////////////////////////////////////////////////////////////////////

#if !NET_STANDARD_20
        //
        // NOTE: The Windows registry key where information about installed
        //       ActiveTcl distributions is known to reside.
        //
        private const string ActiveTclKeyPath =
            "Software\\ActiveState\\ActiveTcl"; /* WINDOWS */
#endif
        #endregion

        ///////////////////////////////////////////////////////////////////////////////////////////////

        #region IronTcl Naming Constants
        //
        // NOTE: If the fully qualified file name for a Tcl dynamic link
        //       library matches this pattern, it likely originated from
        //       Eyrie Solutions (i.e. it is likely IronTcl).
        //
        // HACK: This is purposely not read-only.
        //
        private static string EyriePattern = "*Eyrie*";

        ///////////////////////////////////////////////////////////////////////////////////////////////

        //
        // NOTE: If the fully qualified file name for a Tcl dynamic link
        //       library matches this pattern, it is likely IronTcl.
        //
        // HACK: This is purposely not read-only.
        //
        private static string IronTclPattern = "*IronTcl*";
        #endregion

        ///////////////////////////////////////////////////////////////////////////////////////////////

        #region Default Versions
        //
        // NOTE: This is the bare minimum version of Tcl/Tk that we support
        //       and should be changed with extreme caution (null indicates
        //       that there is no such restriction).
        //
        private static readonly Version DefaultMinimumVersion =
            GlobalState.GetTwoPartVersion(8, 4);

        //
        // NOTE: This is the maximum version of Tcl/Tk that we support and
        //       should be changed with extreme caution (null indicates that
        //       there is no such restriction).
        //
        private static readonly Version DefaultMaximumVersion = null;

        //
        // NOTE: This is the "unknown" version of Tcl/Tk when we cannot
        //       determine the version based on the file name (null indicates
        //       that it is an error if we cannot determine the version based
        //       on the file name).
        //
        private static readonly Version DefaultUnknownVersion =
            DefaultMinimumVersion;

        //
        // NOTE: This is the minimum version of Tcl/Tk when the threaded builds
        //       became the official default on Windows (e.g. 8.4.11.2.201775,
        //       13-Oct-2005 08:27) and Mac OS X.
        //
        private static readonly Version DefaultThreadedNonUnixMinimumVersion =
            GlobalState.GetThreePartVersion(8, 4, 11);

        //
        // NOTE: This is the minimum version of Tcl/Tk when the threaded builds
        //       became the official default on Unix (e.g. 8.5.0).
        //
        private static readonly Version DefaultThreadedUnixMinimumVersion =
            GlobalState.GetThreePartVersion(8, 5, 0);

        //
        // NOTE: This is the amount of increment the major version used when
        //       iterating through a range of versions.
        //
        private static readonly int DefaultMajorIncrement = 1;

        //
        // NOTE: This is the amount of increment the minor version used when
        //       iterating through a range of versions.
        //
        private static readonly int DefaultMinorIncrement = 1;

        //
        // NOTE: This is the minimum minor component of the version used when
        //       iterating through a range of versions.
        //
        private static readonly int DefaultIntermediateMinimum = 0;

        //
        // NOTE: This is the maximum minor component of the version used when
        //       iterating through a range of versions.
        //
        private static readonly int DefaultIntermediateMaximum = 9;
        #endregion

        ///////////////////////////////////////////////////////////////////////////////////////////////

        #region Default Scripts
        //
        // NOTE: This is the default script fragment used to find a native
        //       Tcl library (i.e. if the caller specifies a null script).
        //       This procedure is defined in the "pkgt.eagle" core script
        //       library file.
        //
        private static string DefaultFindViaEvaluateScript =
            "downloadAndExtractNativeTclKitDll";
        #endregion

        ///////////////////////////////////////////////////////////////////////////////////////////////

        #region Per-Platform File Extension Patterns
        //
        // NOTE: These are the shared library extension pattern fragments
        //       for the platforms we support.
        //
        // HACK: These are purposely not read-only.
        //
        private static string windowsLibraryExtensionPattern;
        private static string unixLibraryExtensionPattern;
        private static string macintoshLibraryExtensionPattern;
        #endregion

        ///////////////////////////////////////////////////////////////////////////////////////////////

        //
        // NOTE: The file name pattern(s) for finding candidate Tcl and/or
        //       Tk dynamic link library file names that we might want to
        //       load.
        //
        #region Per-Platform Library File Name Patterns
        #region Windows
        //
        // EXAMPLE: "tcl86tg.dll" (Tcl for Windows)
        //
        // HACK: These are purposely not read-only.
        //
        private static Regex windowsLibraryRegEx;
        private static Regex windowsBaseKitRegEx;
        private static Regex windowsTclKitRegEx;
        #endregion

        ///////////////////////////////////////////////////////////////////////////////////////////////

        #region Unix (FreeBSD, OpenBSD, Linux, etc)
        //
        // EXAMPLE: "libtcl8.6.so" (Tcl for FreeBSD/Linux)
        //          "libtcl8.6.so.1.0" (Tcl for OpenBSD)
        //
        // HACK: These are purposely not read-only.
        //
        private static Regex unixLibraryRegEx;
        private static Regex unixBaseKitRegEx;
        private static Regex unixTclKitRegEx;
        #endregion

        ///////////////////////////////////////////////////////////////////////////////////////////////

        #region Mac OS X
        //
        // EXAMPLE: "libtcl8.6.dylib" (Tcl for Mac OS X)
        //
        // HACK: These are purposely not read-only.
        //
        private static Regex macintoshLibraryRegEx;
        private static Regex macintoshBaseKitRegEx;
        private static Regex macintoshTclKitRegEx;
        #endregion
        #endregion

        ///////////////////////////////////////////////////////////////////////////////////////////////

        #region Global File Name Pattern Lists
        //
        // NOTE: Create the list of primary regular expression patterns
        //       to check candidate file names against.
        //
        // HACK: This is purposely not read-only.
        //
        private static RegExList primaryNameRegExList;

        ///////////////////////////////////////////////////////////////////////////////////////////////

        //
        // NOTE: Create the list of secondary regular expression patterns
        //       to check candidate file names against.
        //
        // HACK: This is purposely not read-only.
        //
        private static RegExList secondaryNameRegExList;

        ///////////////////////////////////////////////////////////////////////////////////////////////

        //
        // NOTE: Create the list of "other" regular expression patterns
        //       to check candidate file names against.
        //
        // HACK: This is purposely not read-only.
        //
        private static RegExList otherNameRegExList;
        #endregion

        ///////////////////////////////////////////////////////////////////////////////////////////////

        //
        // NOTE: The file name pattern(s) for extracting the Tcl version
        //       number (and possibly the threaded flag) from the file
        //       name.
        //
        #region Per-Platform Library File Name Patterns (Capture)
        #region Windows
        //
        // EXAMPLE: "86" OR "86tg" (Tcl for Windows)
        //
        // HACK: These are purposely not read-only.
        //
        private static Regex windowsLibraryVersionRegEx;
        private static Regex windowsBaseKitVersionRegEx;
        private static Regex windowsTclKitVersionRegEx;
        #endregion

        ///////////////////////////////////////////////////////////////////////////////////////////////

        #region Unix (FreeBSD, OpenBSD, Linux, etc)
        //
        // EXAMPLE: "8.6" (Tcl for Unix)
        //
        // HACK: These are purposely not read-only.
        //
        private static Regex unixLibraryVersionRegEx;
        private static Regex unixBaseKitVersionRegEx;
        private static Regex unixTclKitVersionRegEx;
        #endregion

        ///////////////////////////////////////////////////////////////////////////////////////////////

        #region Mac OS X
        //
        // EXAMPLE: "8.6" (Tcl for Mac OS X)
        //
        // HACK: These are purposely not read-only.
        //
        private static Regex macintoshLibraryVersionRegEx;
        private static Regex macintoshBaseKitVersionRegEx;
        private static Regex macintoshTclKitVersionRegEx;
        #endregion
        #endregion

        ///////////////////////////////////////////////////////////////////////////////////////////////

        #region Global File Name Pattern Dictionary (OperatingSystemId)
        //
        // NOTE: Create a dictionary that maps regular expression patterns
        //       to a particular operating system we support.
        //
        // HACK: This is purposely not read-only.
        //
        private static RegExEnumDictionary primaryVersionRegExDictionary;
        #endregion

        ///////////////////////////////////////////////////////////////////////////////////////////////

        //
        // NOTE: Used to delimit the major, minor, build, and revision
        //       numbers in a version string.
        //
        private const char VersionSeparator = Characters.Period;

        //
        // NOTE: This object is used to synchronize access to the Tcl modules
        //       collection and the regular expression pattern lists (below),
        //       among other things.
        //
        private static readonly object syncRoot = new object();
        #endregion

        ///////////////////////////////////////////////////////////////////////////////////////////////

        #region Private Data
        //
        // NOTE: If this flag is non-zero, all candidate Tcl library files
        //       will be tested for validity by using LoadLibrary on them;
        //       otherwise, they will be checked only for existence.
        //
        private static bool forceTestLoadTclLibraryFile = false;

        ///////////////////////////////////////////////////////////////////////////////////////////////

        //
        // NOTE: This is the cached path of a candidate Tcl library from
        //       the evaluation of the default script within the method
        //       FindViaEvaluateScript.
        //
        private static Result cachedFindViaEvaluateScriptResult = null;

        ///////////////////////////////////////////////////////////////////////////////////////////////

        //
        // NOTE: The Load/Unload methods of this class can, in theory, be
        //       called any number of times for any number of Eagle
        //       interpreters.  This collection of "Tcl modules" keeps
        //       track of the module handles we get back from the system
        //       and their asssociated reference counts.  We need to
        //       maintain these reference counts instead of simply
        //       relying on the operating system LoadLibrary/FreeLibrary
        //       functions to do so because the Tcl_Finalize function
        //       must be called to prior to the final FreeLibrary
        //       function call and we do not want one Eagle interpreter
        //       to be able to yank the entire Tcl library files out
        //       from under the other Eagle interpreters (i.e. the
        //       Tcl_Finalize function has no internal reference counting
        //       mechanism, it unconditionally tears down everything when
        //       called).
        //
        private static PathDictionary<TclModule> tclModules;

        ///////////////////////////////////////////////////////////////////////////////////////////////

        //
        // NOTE: This list is an unsupported third-party "hook" into the file
        //       name matching logic.  The only capture in these regular
        //       expressions SHOULD be of the entire file name.  This list MAY
        //       contain null elements.  If this entire list is null, it will
        //       simply be ignored.
        //
        private static RegExList extraNameRegExList = null;

        //
        // NOTE: This dictionary is an unsupported third-party "hook" into the
        //       file name matching and version extraction logic.  The regular
        //       expression patterns MUST have at least one capture containing
        //       the version number (e.g. 84 or 8.4, etc).  They MAY also have
        //       a second and third capture that indicate the build is
        //       threading and/or debugging enabled, respectively.  Since this
        //       is a dictionary of Regex object keys associated with
        //       OperatingSystemId enumerated values, the contained data MAY
        //       NOT be null (i.e. dictionaries do not support null keys and
        //       value types cannot normally be null); however, if this entire
        //       dictionary is null, it will simply be ignored.
        //
        private static RegExEnumDictionary extraVersionRegExDictionary = null;
        #endregion

        ///////////////////////////////////////////////////////////////////////////////////////////////

        #region Static Constructor
        static TclWrapper()
        {
            Initialize(false, false);
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////////////////////////////

        #region Private Module Initialization Methods
        public static void Initialize(
            bool refresh,     /* in */
            bool forceWindows /* in */
            )
        {
            InitializePatternStrings(refresh, forceWindows);
            InitializeRegularExpressions(refresh);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        private static void InitializePatternStrings(
            bool refresh,     /* in */
            bool forceWindows /* in */
            )
        {
            string[] fileExtensions = { null, null, null };

            ///////////////////////////////////////////////////////////////////

            if (refresh || (windowsLibraryExtensionPattern == null))
            {
                fileExtensions[0] = FileExtension.Library;

                if (!String.IsNullOrEmpty(fileExtensions[0]))
                {
                    //
                    // EXAMPLE: ".dll"
                    //
                    windowsLibraryExtensionPattern = "\\." +
                        fileExtensions[0].Substring(1);
                }
            }

            ///////////////////////////////////////////////////////////////////

            if (refresh || (unixLibraryExtensionPattern == null))
            {
                if (forceWindows || PlatformOps.IsWindowsOperatingSystem())
                {
                    fileExtensions[0] = FileExtension.Library;
                    fileExtensions[1] = FileExtension.SharedLibrary;
                    fileExtensions[2] = FileExtension.SharedObject;

                    if (!String.IsNullOrEmpty(fileExtensions[0]) &&
                        !String.IsNullOrEmpty(fileExtensions[1]) &&
                        !String.IsNullOrEmpty(fileExtensions[2]))
                    {
                        //
                        // EXAMPLE: ".dll", ".sl", or ".so"
                        //
                        unixLibraryExtensionPattern = "\\.(?:" +
                            fileExtensions[0].Substring(1) + "|" +
                            fileExtensions[1].Substring(1) + "|" +
                            fileExtensions[2].Substring(1) + ")";
                    }
                }
                else
                {
                    fileExtensions[0] = FileExtension.SharedLibrary;
                    fileExtensions[1] = FileExtension.SharedObject;

                    if (!String.IsNullOrEmpty(fileExtensions[0]) &&
                        !String.IsNullOrEmpty(fileExtensions[1]))
                    {
                        //
                        // EXAMPLE: ".sl" or ".so"
                        //
                        unixLibraryExtensionPattern = "\\.(?:" +
                            fileExtensions[0].Substring(1) + "|" +
                            fileExtensions[1].Substring(1) + ")";
                    }
                }
            }

            ///////////////////////////////////////////////////////////////////

            if (refresh || (macintoshLibraryExtensionPattern == null))
            {
                fileExtensions[0] = FileExtension.DynamicLibrary;

                if (!String.IsNullOrEmpty(fileExtensions[0]))
                {
                    //
                    // EXAMPLE: ".dylib"
                    //
                    macintoshLibraryExtensionPattern = "\\." +
                        fileExtensions[0].Substring(1);
                }
            }
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        private static void InitializeRegularExpressions(
            bool refresh /* in */
            )
        {
            #region Regular Expression Options
            RegexOptions unixRegExOptions = RegexOptions.CultureInvariant;

            RegexOptions nonUnixRegExOptions = RegexOptions.IgnoreCase |
                unixRegExOptions;
            #endregion

            ///////////////////////////////////////////////////////////////////

            //
            // NOTE: The file name pattern(s) for finding candidate Tcl and/or
            //       Tk dynamic link library file names that we might want to
            //       load.
            //
            #region Per-Platform Library File Name Patterns
            #region Windows
            //
            // EXAMPLE: "tcl86tg.dll" (Tcl for Windows)
            //
            if (windowsLibraryExtensionPattern != null)
            {
                if (refresh || (windowsLibraryRegEx == null))
                {
                    windowsLibraryRegEx = RegExOps.Create(
                        "^(tcl\\d+[t]?[s]?[g]?[x]?" +
                        windowsLibraryExtensionPattern + ")$",
                        nonUnixRegExOptions);
                }

                ///////////////////////////////////////////////////////////////

                if (refresh || (windowsBaseKitRegEx == null))
                {
                    windowsBaseKitRegEx = RegExOps.Create(
                        "^(base-(?:tcl|tk)\\d+\\.\\d+-thread-win32-\\w+" +
                        windowsLibraryExtensionPattern + ")$",
                        nonUnixRegExOptions);
                }

                ///////////////////////////////////////////////////////////////

                if (refresh || (windowsTclKitRegEx == null))
                {
                    windowsTclKitRegEx = RegExOps.Create(
                        "^(libtclkit\\d\\.?\\d(?:\\.?\\d{1,2})?(?:(?:a|b)(?:\\d+)?)?" +
                        windowsLibraryExtensionPattern + ")$",
                        nonUnixRegExOptions);
                }
            }
            #endregion

            ///////////////////////////////////////////////////////////////////

            #region Unix (FreeBSD, OpenBSD, Linux, etc)
            //
            // EXAMPLE: "libtcl8.6.so" (Tcl for FreeBSD/Linux)
            //          "libtcl8.6.so.1.0" (Tcl for OpenBSD)
            //
            if (unixLibraryExtensionPattern != null)
            {
                if (refresh || (unixLibraryRegEx == null))
                {
                    unixLibraryRegEx = RegExOps.Create(
                        "^(libtcl\\d+\\.?\\d+" + unixLibraryExtensionPattern +
                        "(?:\\.\\d+\\.\\d+)?)$",
                        unixRegExOptions);
                }

                ///////////////////////////////////////////////////////////////

                if (refresh || (unixBaseKitRegEx == null))
                {
                    unixBaseKitRegEx = RegExOps.Create(
                        "^(base-(?:tcl|tk)\\d+\\.\\d+-thread-\\w+-\\w+" +
                        unixLibraryExtensionPattern + "(?:\\.\\d+\\.\\d+)?)$",
                        unixRegExOptions);
                }

                ///////////////////////////////////////////////////////////////

                if (refresh || (unixTclKitRegEx == null))
                {
                    unixTclKitRegEx = RegExOps.Create(
                        "^(libtclkit\\d\\.?\\d(?:\\.?\\d{1,2})?(?:(?:a|b)(?:\\d+)?)?" +
                        unixLibraryExtensionPattern + "(?:\\.\\d+\\.\\d+)?)$",
                        unixRegExOptions);
                }
            }
            #endregion

            ///////////////////////////////////////////////////////////////////

            #region Mac OS X
            //
            // EXAMPLE: "libtcl8.6.dylib" (Tcl for Mac OS X)
            //
            if (macintoshLibraryExtensionPattern != null)
            {
                if (refresh || (macintoshLibraryRegEx == null))
                {
                    macintoshLibraryRegEx = RegExOps.Create(
                        "^(libtcl\\d+\\.?\\d+" +
                        macintoshLibraryExtensionPattern + ")$",
                        nonUnixRegExOptions);
                }

                ///////////////////////////////////////////////////////////////

                if (refresh || (macintoshBaseKitRegEx == null))
                {
                    macintoshBaseKitRegEx = RegExOps.Create(
                        "^(base-(?:tcl|tk)\\d+\\.\\d+-thread-macosx-\\w+" +
                        macintoshLibraryExtensionPattern + ")$",
                        nonUnixRegExOptions);
                }

                ///////////////////////////////////////////////////////////////

                if (refresh || (macintoshTclKitRegEx == null))
                {
                    macintoshTclKitRegEx = RegExOps.Create(
                        "^(libtclkit\\d\\.?\\d(?:\\.?\\d{1,2})?(?:(?:a|b)(?:\\d+)?)?" +
                        macintoshLibraryExtensionPattern + ")$",
                        nonUnixRegExOptions);
                }
            }
            #endregion
            #endregion

            ///////////////////////////////////////////////////////////////////

            #region Global File Name Pattern Lists
            #region Primary
            //
            // NOTE: Create the list of primary regular expression patterns
            //       to check candidate file names against.
            //
            if (refresh || (primaryNameRegExList == null))
            {
                primaryNameRegExList = new RegExList(new Regex[] {
                    windowsLibraryRegEx,
                    unixLibraryRegEx,
                    macintoshLibraryRegEx
                });
            }
            #endregion

            ///////////////////////////////////////////////////////////////////

            #region Secondary
            //
            // NOTE: Create the list of secondary regular expression patterns
            //       to check candidate file names against.
            //
            if (refresh || (secondaryNameRegExList == null))
            {
                secondaryNameRegExList = new RegExList(new Regex[] {
                    windowsTclKitRegEx,
                    unixTclKitRegEx,
                    macintoshTclKitRegEx
                });
            }
            #endregion

            ///////////////////////////////////////////////////////////////////

            #region Other
            //
            // NOTE: Create the list of "other" regular expression patterns
            //       to check candidate file names against.
            //
            if (refresh || (otherNameRegExList == null))
            {
                otherNameRegExList = new RegExList(new Regex[] {
                    windowsBaseKitRegEx,
                    unixBaseKitRegEx,
                    macintoshBaseKitRegEx
                });
            }
            #endregion
            #endregion

            ///////////////////////////////////////////////////////////////////

            //
            // NOTE: The file name pattern(s) for extracting the Tcl version
            //       number (and possibly the threaded flag) from the file
            //       name.
            //
            #region Per-Platform Library File Name Patterns (Capture)
            #region Windows
            //
            // EXAMPLE: "86" OR "86tg" (Tcl for Windows)
            //
            if (windowsLibraryExtensionPattern != null)
            {
                if (refresh || (windowsLibraryVersionRegEx == null))
                {
                    windowsLibraryVersionRegEx = RegExOps.Create(
                        "^tcl(\\d+)([t])?[s]?([g])?[x]?" +
                        windowsLibraryExtensionPattern + "$",
                        nonUnixRegExOptions);
                }

                ///////////////////////////////////////////////////////////////

                if (refresh || (windowsBaseKitVersionRegEx == null))
                {
                    windowsBaseKitVersionRegEx = RegExOps.Create(
                        "^base-(?:tcl|tk)(\\d+\\.\\d+)(-thread)?-win32-\\w+" +
                        windowsLibraryExtensionPattern + "$",
                        nonUnixRegExOptions);
                }

                ///////////////////////////////////////////////////////////////

                if (refresh || (windowsTclKitVersionRegEx == null))
                {
                    windowsTclKitVersionRegEx = RegExOps.Create(
                        "^libtclkit(\\d\\.?\\d(?:\\.?\\d{1,2})?)(?:(?:a|b)(?:\\d+)?)?" +
                        windowsLibraryExtensionPattern + "$",
                        nonUnixRegExOptions);
                }
            }
            #endregion

            ///////////////////////////////////////////////////////////////////

            #region Unix (FreeBSD, OpenBSD, Linux, etc)
            //
            // EXAMPLE: "8.6" (Tcl for Unix)
            //
            if (unixLibraryExtensionPattern != null)
            {
                if (refresh || (unixLibraryVersionRegEx == null))
                {
                    unixLibraryVersionRegEx = RegExOps.Create(
                        "^libtcl(\\d+\\.?\\d+)" +
                        unixLibraryExtensionPattern + "(?:\\.\\d+\\.\\d+)?$",
                        unixRegExOptions);
                }

                ///////////////////////////////////////////////////////////////

                if (refresh || (unixBaseKitVersionRegEx == null))
                {
                    unixBaseKitVersionRegEx = RegExOps.Create(
                        "^base-(?:tcl|tk)(\\d+\\.\\d+)(-thread)?-\\w+-\\w+" +
                        unixLibraryExtensionPattern + "(?:\\.\\d+\\.\\d+)?$",
                        unixRegExOptions);
                }

                ///////////////////////////////////////////////////////////////

                if (refresh || (unixTclKitVersionRegEx == null))
                {
                    unixTclKitVersionRegEx = RegExOps.Create(
                        "^libtclkit(\\d\\.?\\d(?:\\.?\\d{1,2})?)(?:(?:a|b)(?:\\d+)?)?" +
                        unixLibraryExtensionPattern + "(?:\\.\\d+\\.\\d+)?$",
                        unixRegExOptions);
                }
            }
            #endregion

            ///////////////////////////////////////////////////////////////////

            #region Mac OS X
            //
            // EXAMPLE: "8.6" (Tcl for Mac OS X)
            //
            if (macintoshLibraryExtensionPattern != null)
            {
                if (refresh || (macintoshLibraryVersionRegEx == null))
                {
                    macintoshLibraryVersionRegEx = RegExOps.Create(
                        "^libtcl(\\d+\\.?\\d+)" +
                        macintoshLibraryExtensionPattern + "$",
                        nonUnixRegExOptions);
                }

                ///////////////////////////////////////////////////////////////

                if (refresh || (macintoshBaseKitVersionRegEx == null))
                {
                    macintoshBaseKitVersionRegEx = RegExOps.Create(
                        "^base-(?:tcl|tk)(\\d+\\.\\d+)(-thread)?-macosx-\\w+" +
                        macintoshLibraryExtensionPattern + "$",
                        nonUnixRegExOptions);
                }

                ///////////////////////////////////////////////////////////////

                if (refresh || (macintoshTclKitVersionRegEx == null))
                {
                    macintoshTclKitVersionRegEx = RegExOps.Create(
                        "^libtclkit(\\d\\.?\\d(?:\\.?\\d{1,2})?)(?:(?:a|b)(?:\\d+)?)?" +
                        macintoshLibraryExtensionPattern + "$",
                        nonUnixRegExOptions);
                }
            }
            #endregion
            #endregion

            ///////////////////////////////////////////////////////////////////

            #region Global File Name Pattern Dictionary (OperatingSystemId)
            #region Primary
            //
            // NOTE: Create a dictionary that maps regular expression patterns
            //       to a particular operating system we support.
            //
            if (refresh || (primaryVersionRegExDictionary == null))
            {
                primaryVersionRegExDictionary =
                    new RegExEnumDictionary(
                    new Regex[] {
                    windowsLibraryVersionRegEx,
                    windowsBaseKitVersionRegEx,
                    windowsTclKitVersionRegEx,
                    unixLibraryVersionRegEx,
                    unixBaseKitVersionRegEx,
                    unixTclKitVersionRegEx,
                    macintoshLibraryVersionRegEx,
                    macintoshBaseKitVersionRegEx,
                    macintoshTclKitVersionRegEx
                }, typeof(OperatingSystemId),
                    new /* OperatingSystemId */ Enum[] {
                    OperatingSystemId.WindowsNT,
                    OperatingSystemId.WindowsNT,
                    OperatingSystemId.WindowsNT,
                    OperatingSystemId.Unix,
                    OperatingSystemId.Unix,
                    OperatingSystemId.Unix,
                    OperatingSystemId.Darwin,
                    OperatingSystemId.Darwin,
                    OperatingSystemId.Darwin
                });
            }
            #endregion
            #endregion
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////////////////////////////

        private static bool HaveAnError(
            ResultList errors /* in */
            )
        {
            return ((errors != null) && (errors.Count > 0));
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        private static void MaybeAddAnError(
            ref ResultList errors, /* in, out */
            Result error           /* in */
            )
        {
            if (error == null)
                return;

            if (errors == null)
                errors = new ResultList();

            errors.Add(error);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        //
        // WARNING: This method assumes the lock is held.
        //
        private static void CheckModules()
        {
            if (tclModules == null)
                tclModules = new PathDictionary<TclModule>();
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

#if TCL_WRAPPER
        internal
#else
        public
#endif
        static bool TryCopyModule(
            string fileName,      /* in */
            ref TclModule module, /* out */
            ref Result error      /* out */
            ) /* THREAD-SAFE */
        {
            if (!CheckTclLibraryPath(fileName))
            {
                error = "cannot copy module: invalid file name";
                return false;
            }

            lock (syncRoot) /* TRANSACTIONAL */
            {
                TclModule tclModule;

                if (!tclModules.TryGetValue(fileName, out tclModule))
                {
                    error = String.Format(
                        "cannot copy module: file {0} not found",
                        FormatOps.DisplayName(fileName));

                    return false;
                }

                if (tclModule == null)
                {
                    error = String.Format(
                        "cannot copy module: file {0} not available",
                        FormatOps.DisplayName(fileName));

                    return false;
                }

                module = new TclModule(
                    tclModule.FileName, tclModule.Module,
                    tclModule.ReferenceCount, tclModule.LockCount);

                return true;
            }
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        private static bool AddModuleReference(
            string fileName, /* in */
            ref Result error /* out */
            ) /* THREAD-SAFE */
        {
            return NativeOps.IsValidHandle(
                AddModuleReference(fileName, false, false, ref error));
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        private static bool AddModuleReference(
            string fileName,      /* in */
            ref TclModule module, /* out */
            ref Result error      /* out */
            ) /* THREAD-SAFE */
        {
            return NativeOps.IsValidHandle(
                AddModuleReference(fileName, false, false, ref module, ref error));
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

#if TCL_WRAPPER
        internal
#else
        public
#endif
        static IntPtr AddModuleReference(
            string fileName,   /* in */
            bool load,         /* in */
            bool setDirectory, /* in */
            ref Result error   /* out */
            ) /* THREAD-SAFE */
        {
            TclModule module = null;

            return AddModuleReference(
                fileName, load, setDirectory, ref module, ref error);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        private static IntPtr AddModuleReference(
            string fileName,      /* in */
            bool load,            /* in */
            bool setDirectory,    /* in */
            ref TclModule module, /* out */
            ref Result error      /* out */
            ) /* THREAD-SAFE */
        {
            IntPtr result = IntPtr.Zero;

            if (CheckTclLibraryPath(fileName))
            {
                lock (syncRoot) /* TRANSACTIONAL */
                {
                    //
                    // NOTE: Make sure the modules collection is initialized.
                    //
                    CheckModules();

                    //
                    // NOTE: Check if the module should [already] be loaded.
                    //
                    TclModule tclModule;

                    if (tclModules.TryGetValue(fileName, out tclModule))
                    {
                        if (tclModule != null)
                        {
                            //
                            // NOTE: Add one reference to the module.
                            //
                            tclModule.AddReference();

                            //
                            // NOTE: If the 'load' flag has been set by the
                            //       caller, return the actual module handle
                            //       as the result; otherwise, just return a
                            //       fake module handle that appears to be
                            //       valid if the actual module handle is
                            //       valid.  If the actual module handle is
                            //       invalid, an invalid module handle (zero)
                            //       will always be returned.
                            //
                            result = tclModule.GetModule(load);

                            //
                            // NOTE: Give a reference to the pre-existing Tcl
                            //       module object to the caller.
                            //
                            module = tclModule;
                        }
                        else
                        {
                            //
                            // NOTE: The module is not valid.  Therefore, we
                            //       will forbid making any changes to it.
                            //
                            error = String.Format(
                                "cannot add module reference: file {0} not available",
                                FormatOps.DisplayName(fileName));
                        }
                    }
                    else if (load)
                    {
                        //
                        // NOTE: We have never seen this module before (or it
                        //       was previously unloaded); therefore, attempt
                        //       to have the operating system load it now.
                        //       This can throw an exception or return an
                        //       invalid module handle.  No state has been
                        //       changed at this point.  If an exception is
                        //       thrown it should simply be caught by the
                        //       caller (the Load method).  If an invalid
                        //       module handle is returned, the operating
                        //       system could not load the module for some
                        //       reason and we will return an error message
                        //       built from the underlying error information.
                        //
                        bool success = false;
                        IntPtr nativeModule = IntPtr.Zero;

                        try
                        {
                            int lastError;
                            string directory = Path.GetDirectoryName(fileName);

                            if (!setDirectory || NativeOps.SetDllDirectory(
                                    directory, out lastError)) /* throw */
                            {
                                nativeModule = NativeOps.LoadLibrary(
                                    fileName, out lastError); /* throw */

                                if (NativeOps.IsValidHandle(nativeModule))
                                {
                                    //
                                    // NOTE: Create a new Tcl module object to wrap
                                    //       the native module file name and handle
                                    //       in.
                                    //
                                    tclModule = new TclModule(fileName,
                                        nativeModule, 1);

                                    //
                                    // NOTE: Add the new Tcl module object to the
                                    //       private dictionary of loaded modules.
                                    //
                                    tclModules.Add(fileName, tclModule);

                                    //
                                    // NOTE: If the 'load' flag has been set by the
                                    //       caller, return the actual module
                                    //       handle as the result; otherwise, just
                                    //       return a fake module handle that
                                    //       appears to be valid if the actual
                                    //       module handle is valid.  If the actual
                                    //       module handle is invalid, an invalid
                                    //       module handle (zero) will always be
                                    //       returned.
                                    //
                                    result = tclModule.GetModule(load);

                                    //
                                    // NOTE: Give a reference to the newly created
                                    //       Tcl module object to the caller.
                                    //
                                    module = tclModule;

                                    //
                                    // NOTE: Set the flag indicating to the finally
                                    //       block that this code has succeeded.
                                    //
                                    success = true;
                                }
                                else
                                {
                                    error = String.Format(
                                        "LoadLibrary({1}) failed with error {0}: {2}",
                                        lastError, FormatOps.WrapOrNull(fileName),
                                        NativeOps.GetDynamicLoadingError(lastError));
                                }
                            }
                            else
                            {
                                error = String.Format(
                                    "SetDllDirectory({1}) failed with error {0}: {2}",
                                    lastError, FormatOps.WrapOrNull(directory),
                                    NativeOps.GetDynamicLoadingError(lastError));
                            }
                        }
                        finally
                        {
                            if (!success && (nativeModule != IntPtr.Zero))
                            {
                                int lastError;

                                if (NativeOps.FreeLibrary(
                                        nativeModule, out lastError)) /* throw */
                                {
                                    TraceOps.DebugTrace(String.Format(
                                        "FreeLibrary (AddModuleReference): " +
                                        "success, module = {0}", nativeModule),
                                        typeof(TclWrapper).Name,
                                        TracePriority.NativeDebug);

                                    nativeModule = IntPtr.Zero;
                                }
                                else
                                {
                                    throw new ScriptException(String.Format(
                                        "FreeLibrary(0x{1:X}) failed with error {0}: {2}",
                                        lastError, nativeModule,
                                        NativeOps.GetDynamicLoadingError(lastError)));
                                }
                            }
                        }
                    }
                    else
                    {
                        //
                        // NOTE: The module handle does not exist in our
                        //       collection and we have been told NOT to
                        //       load it; therefore, we will simply return
                        //       an error.
                        //
                        error = String.Format(
                            "cannot add module reference: file {0} not found",
                            FormatOps.DisplayName(fileName));
                    }
                }
            }
            else
            {
                error = "cannot add module reference: invalid file name";
            }

#if false
            TraceOps.DebugTrace(String.Format(
                "AddModuleReference: fileName = {0}, load = {1}, " +
                "module = {2}, result = {3}, error = {4}",
                FormatOps.WrapOrNull(fileName), load, module, result,
                FormatOps.WrapOrNull(error)), typeof(TclWrapper).Name,
                TracePriority.NativeDebug);
#endif

            return result;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        private static int ReleaseModuleReference(
            string fileName, /* in */
            bool unload,     /* in */
            bool unlock,     /* in */
            ref Result error /* out */
            ) /* THREAD-SAFE */
        {
            int result = Count.Invalid;

            if (CheckTclLibraryPath(fileName))
            {
                lock (syncRoot) /* TRANSACTIONAL */
                {
                    //
                    // NOTE: Make sure the modules collection is initialized.
                    //
                    CheckModules();

                    //
                    // NOTE: Check if the module is known (and loaded).
                    //
                    TclModule tclModule;

                    if (tclModules.TryGetValue(fileName, out tclModule))
                    {
                        if (tclModule != null)
                        {
                            //
                            // NOTE: At this point, the lock count may need to
                            //       be adjusted.  If the 'unload' flag is not
                            //       set, that means the Unload method is
                            //       taking a lock on it, which prevents any
                            //       further changes to the reference count.
                            //       If the 'unload' and 'unlock' flags are
                            //       both set, the lock on the module will be
                            //       released.  If only the 'unload' flag is
                            //       set, the exiting lock count will be
                            //       returned unchanged.  This code relies upon
                            //       the caller knowing exactly the correct
                            //       flags to set for a given call.  Currently,
                            //       there are exactly two callers of this
                            //       method:
                            //
                            //       1. The Unload method in this class.
                            //          First, it calls this method to acquire
                            //          a pending lock on the module (i.e. with
                            //          the 'unload' and 'unlock' flags not
                            //          set).  After completing the unloading
                            //          process, it calls this method again to
                            //          release its pending lock and remove its
                            //          reference to the module, possibly
                            //          causing it to be completely removed.
                            //
                            //       2. The DoOneEvent method in this class.
                            //          In this case, The 'unload' flag will be
                            //          set and the 'unlock' flag will not be
                            //          set.  If there is a pending lock on the
                            //          module when this call is executed, the
                            //          module will not be removed, even if the
                            //          reference count reaches zero.  This
                            //          prevents a very subtle race condition
                            //          between a thread calling the Unload
                            //          method and one calling the DoOneEvent
                            //          method.
                            //
                            int lockCount = tclModule.AdjustLockCount(unload,
                                unlock);

                            //
                            // NOTE: Release one reference from the module.
                            //
                            int referenceCount = tclModule.ReleaseReference();

                            //
                            // NOTE: Normalize negative reference counts to
                            //       zero.
                            //
                            if (referenceCount < 0)
                                referenceCount = 0;

                            //
                            // NOTE If the 'unload' flag is set, the result of
                            //      this method will take into account the
                            //      pending lock count; otherwise, it will not.
                            //      Also, if the unload flag is not set, the
                            //      module will never be removed, even if the
                            //      reference count reaches zero.
                            //
                            if (unload)
                            {
                                result = referenceCount + lockCount;

                                if (result == 0)
                                    tclModules.Remove(fileName);
                            }
                            else
                            {
                                //
                                // NOTE: Return the "real" reference count
                                //       without taking into account any
                                //       pending locks.  This is critically
                                //       important when this method is being
                                //       called by the Unload method to obtain
                                //       the pending lock (i.e. where we want
                                //       the reference count to reach zero
                                //       without removing the module.
                                //
                                result = referenceCount;
                            }
                        }
                        else
                        {
                            //
                            // NOTE: The module is not valid.  Therefore, we
                            //       will forbid making any changes to it.
                            //
                            error = String.Format(
                                "cannot release module reference: file {0} not available",
                                FormatOps.DisplayName(fileName));
                        }
                    }
                    else
                    {
                        //
                        // NOTE: The module handle does not exist in our
                        //       collection; therefore, we will simply return
                        //       an error.
                        //
                        error = String.Format(
                            "cannot release module reference: file {0} not found",
                            FormatOps.DisplayName(fileName));
                    }
                }
            }
            else
            {
                error = "cannot release module reference: invalid file name";
            }

#if false
            TraceOps.DebugTrace(String.Format(
                "ReleaseModuleReference: fileName = {0}, unload = {1}, " +
                "unlock = {2}, result = {3}, error = {4}",
                FormatOps.WrapOrNull(fileName), unload, unlock, result,
                FormatOps.WrapOrNull(error)), typeof(TclWrapper).Name,
                TracePriority.NativeDebug);
#endif

            return result;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        private static bool CheckTclLibraryPath(
            string path /* in */
            )
        {
            if (String.IsNullOrEmpty(path))
                return false;

            if (!PathOps.CheckForValid(
                    null, path, false, false, true,
                    PlatformOps.IsWindowsOperatingSystem()))
            {
                return false;
            }

            return true;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        private static bool CheckTclLibraryDirectory(
            string directory /* in */
            )
        {
            if (!CheckTclLibraryPath(directory))
                return false;

            if (!PlatformOps.IsMacintoshOperatingSystem() &&
                !Directory.Exists(directory))
            {
                return false;
            }

            return true;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        private static bool CheckTclLibraryFile(
            Interpreter interpreter, /* in */
            string fileName          /* in */
            )
        {
            if (!CheckTclLibraryPath(fileName))
                return false;

            if (forceTestLoadTclLibraryFile ||
                PlatformOps.IsMacintoshOperatingSystem())
            {
                if (!RuntimeOps.IsFileTrusted(
                        interpreter, null, fileName,
                        IntPtr.Zero))
                {
                    return false;
                }

                if (NativeOps.TestLoadLibrary(
                        fileName) != ReturnCode.Ok)
                {
                    return false;
                }
            }
            else if (!File.Exists(fileName))
            {
                return false;
            }

            return true;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public static bool IsReady(
            ITclApi tclApi,  /* in */
            IntPtr interp,   /* in */
            bool deleted,    /* in */
            ref Result error /* out */
            )
        {
            if (!TclApi.CheckModule(tclApi, ref error))
                return false;

            if (!tclApi.CheckInterp(interp, ref error))
                return false;

            if (deleted && GetInterpDeleted(tclApi, interp))
            {
                error = "cannot use Tcl interpreter, it is deleted";
                return false;
            }

            return true;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        private static string MarshalString( /* NOT USED */
            IntPtr bufferPtr, /* in */
            int length        /* in */
            )
        {
#if TCL_UNICODE
            return MarshalUnicodeString(bufferPtr, length);
#else
            return MarshalUtf8String(bufferPtr, length);
#endif
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        private static string MarshalUnicodeString(
            IntPtr bufferPtr, /* in */
            int length        /* in */
            )
        {
            string result = null;

            try
            {
                if (bufferPtr != IntPtr.Zero)
                {
                    if (length > 0)
                    {
                        char[] characters = new char[length];
                        Marshal.Copy(bufferPtr, characters, 0, length);
                        result = new string(characters);
                    }
                    else
                    {
                        result = String.Empty;
                    }
                }
            }
            catch (Exception e)
            {
                //
                // NOTE: Nothing we can do here except log the failure.
                //
                TraceOps.DebugTrace(
                    e, typeof(TclWrapper).Name,
                    TracePriority.MarshalError);
            }

            return result;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        private static string MarshalUtf8String(
            IntPtr bufferPtr, /* in */
            int length        /* in */
            )
        {
            string result = null;

            try
            {
                if (bufferPtr != IntPtr.Zero)
                {
                    if (length > 0)
                    {
                        Encoding encoding = TclApi.FromEncoding;

                        if (encoding != null)
                        {
                            byte[] bytes = new byte[length];
                            Marshal.Copy(bufferPtr, bytes, 0, length);
                            result = encoding.GetString(bytes); // UTF-8
                        }
                    }
                    else
                    {
                        result = String.Empty;
                    }
                }
            }
            catch (Exception e)
            {
                //
                // NOTE: Nothing we can do here except log the failure.
                //
                TraceOps.DebugTrace(
                    e, typeof(TclWrapper).Name,
                    TracePriority.MarshalError);
            }

            return result;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

#if TCL_WRAPPER
        internal
#else
        public
#endif
        static string GetString(
            ITclApi tclApi, /* in */
            IntPtr objPtr   /* in */
            )
        {
#if TCL_UNICODE
            return GetUnicodeString(tclApi, objPtr);
#else
            return GetUtf8String(tclApi, objPtr);
#endif
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        private static string GetUnicodeString(
            ITclApi tclApi, /* in */
            IntPtr objPtr   /* in */ /* DANGER: Which Tcl interpreter/thread owns this object? */
            )
        {
            string result = null;

            try
            {
                if (TclApi.CheckModule(tclApi))
                {
                    Tcl_GetUnicodeFromObj getUnicodeFromObj;

                    lock (tclApi.SyncRoot)
                    {
                        getUnicodeFromObj = tclApi.GetUnicodeFromObj;
                    }

                    if (tclApi.CheckObjPtr(objPtr))
                    {
                        if (getUnicodeFromObj != null)
                        {
                            int length = 0;

                            IntPtr bufferPtr = getUnicodeFromObj(
                                objPtr, ref length);

                            result = MarshalUnicodeString(bufferPtr, length);
                        }
                    }
                }
            }
            catch (Exception e)
            {
                //
                // NOTE: Nothing we can do here except log the failure.
                //
                TraceOps.DebugTrace(
                    e, typeof(Tcl_GetUnicodeFromObj).Name,
                    TracePriority.NativeError);
            }

            return result;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        private static string GetUtf8String(
            ITclApi tclApi, /* in */
            IntPtr objPtr   /* in */ /* DANGER: Which Tcl interpreter/thread owns this object? */
            )
        {
            string result = null;

            try
            {
                if (TclApi.CheckModule(tclApi))
                {
                    Tcl_GetStringFromObj getStringFromObj;

                    lock (tclApi.SyncRoot)
                    {
                        getStringFromObj = tclApi.GetStringFromObj;
                    }

                    if (tclApi.CheckObjPtr(objPtr))
                    {
                        if (getStringFromObj != null)
                        {
                            int length = 0;

                            IntPtr bufferPtr = getStringFromObj(
                                    objPtr, ref length);

                            result = MarshalUtf8String(bufferPtr, length);
                        }
                    }
                }
            }
            catch (Exception e)
            {
                //
                // NOTE: Nothing we can do here except log the failure.
                //
                TraceOps.DebugTrace(
                    e, typeof(Tcl_GetStringFromObj).Name,
                    TracePriority.NativeError);
            }

            return result;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public static IntPtr NewObject(
            ITclApi tclApi /* in */
            )
        {
            IntPtr result = IntPtr.Zero;

            try
            {
                if (TclApi.CheckModule(tclApi))
                {
                    Tcl_NewObj newObj;
                    Tcl_DbIncrRefCount dbIncrRefCount;

                    lock (tclApi.SyncRoot)
                    {
                        newObj = tclApi.NewObj;
                        dbIncrRefCount = tclApi.DbIncrRefCount;
                    }

                    if (newObj != null)
                    {
                        result = newObj();

                        if (result != IntPtr.Zero)
                        {
                            if (dbIncrRefCount != null)
                                /* NO RESULT */
                                dbIncrRefCount(result, String.Empty, 0);
                        }
                    }
                }
            }
            catch (Exception e)
            {
                //
                // NOTE: Nothing we can do here except log the failure.
                //
                TraceOps.DebugTrace(
                    e, typeof(Tcl_NewObj).Name,
                    TracePriority.NativeError);
            }

            return result;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public static IntPtr NewString(
            ITclApi tclApi, /* in */
            string text     /* in */
            )
        {
#if TCL_UNICODE
            return NewUnicodeString(tclApi, text);
#else
            return NewUtf8String(tclApi, text);
#endif
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

#if TCL_UNICODE
        public static IntPtr NewUnicodeString(
            ITclApi tclApi, /* in */
            string text     /* in */
            )
        {
            IntPtr result = IntPtr.Zero;

            try
            {
                if (TclApi.CheckModule(tclApi) && (text != null))
                {
                    Tcl_NewUnicodeObj newUnicodeObj;
                    Tcl_DbIncrRefCount dbIncrRefCount;

                    lock (tclApi.SyncRoot)
                    {
                        newUnicodeObj = tclApi.NewUnicodeObj;
                        dbIncrRefCount = tclApi.DbIncrRefCount;
                    }

                    if (newUnicodeObj != null)
                    {
                        result = newUnicodeObj(text, text.Length);

                        if (result != IntPtr.Zero)
                        {
                            if (dbIncrRefCount != null)
                                /* NO RESULT */
                                dbIncrRefCount(result, String.Empty, 0);
                        }
                    }
                }
            }
            catch (Exception e)
            {
                //
                // NOTE: Nothing we can do here except log the failure.
                //
                TraceOps.DebugTrace(
                    e, typeof(Tcl_NewUnicodeObj).Name,
                    TracePriority.NativeError);
            }

            return result;
        }
#endif

        ///////////////////////////////////////////////////////////////////////////////////////////////

#if !TCL_UNICODE
        public static IntPtr NewUtf8String(
            ITclApi tclApi, /* in */
            string text     /* in */
            )
        {
            IntPtr result = IntPtr.Zero;

            try
            {
                if (TclApi.CheckModule(tclApi) && (text != null))
                {
                    Tcl_NewStringObj newStringObj;
                    Tcl_DbIncrRefCount dbIncrRefCount;

                    lock (tclApi.SyncRoot)
                    {
                        newStringObj = tclApi.NewStringObj;
                        dbIncrRefCount = tclApi.DbIncrRefCount;
                    }

                    if (newStringObj != null)
                    {
                        Encoding encoding = TclApi.ToEncoding;

                        if (encoding != null)
                        {
                            byte[] bytes = encoding.GetBytes(
                                !String.IsNullOrEmpty(text) ?
                                    text : String.Empty); // UTF-8

                            result = newStringObj(bytes, bytes.Length);

                            if (result != IntPtr.Zero)
                            {
                                if (dbIncrRefCount != null)
                                    /* NO RESULT */
                                    dbIncrRefCount(result, String.Empty, 0);
                            }
                        }
                    }
                }
            }
            catch (Exception e)
            {
                //
                // NOTE: Nothing we can do here except log the failure.
                //
                TraceOps.DebugTrace(
                    e, typeof(Tcl_NewStringObj).Name,
                    TracePriority.NativeError);
            }

            return result;
        }
#endif

        ///////////////////////////////////////////////////////////////////////////////////////////////

        #region Dead Code
#if DEAD_CODE
        private static IntPtr NewByteArray( /* NOT USED */
            ITclApi tclApi, /* in */
            byte[] bytes    /* in */
            )
        {
            IntPtr result = IntPtr.Zero;

            try
            {
                if (TclApi.CheckModule(tclApi) && (bytes != null))
                {
                    Tcl_NewByteArrayObj newByteArrayObj;
                    Tcl_DbIncrRefCount dbIncrRefCount;

                    lock (tclApi.SyncRoot)
                    {
                        newByteArrayObj = tclApi.NewByteArrayObj;
                        dbIncrRefCount = tclApi.DbIncrRefCount;
                    }

                    if (newByteArrayObj != null)
                    {
                        result = newByteArrayObj(bytes, bytes.Length);

                        if (result != IntPtr.Zero)
                        {
                            if (dbIncrRefCount != null)
                                /* NO RESULT */
                                dbIncrRefCount(result, String.Empty, 0);
                        }
                    }
                }
            }
            catch (Exception e)
            {
                //
                // NOTE: Nothing we can do here except log the failure.
                //
                TraceOps.DebugTrace(
                    e, typeof(Tcl_NewByteArrayObj).Name,
                    TracePriority.NativeError);
            }

            return result;
        }
#endif
        #endregion

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public static string GetResultAsString(
            ITclApi tclApi, /* in */
            IntPtr interp   /* in */
            )
        {
            return GetResultAsString(tclApi, interp, false);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        private static string GetResultAsString(
            ITclApi tclApi, /* in */
            IntPtr interp,  /* in */
            bool noThread   /* in */
            )
        {
#if TCL_UNICODE
            return GetResultAsUnicodeString(tclApi, interp, noThread);
#else
            return GetResultAsUtf8String(tclApi, interp, noThread);
#endif
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public static string GetResultAsUnicodeString(
            ITclApi tclApi, /* in */
            IntPtr interp   /* in */
            )
        {
            return GetResultAsUnicodeString(tclApi, interp, false);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        private static string GetResultAsUnicodeString(
            ITclApi tclApi, /* in */
            IntPtr interp,  /* in */
            bool noThread   /* in */
            )
        {
            string result = null;

            try
            {
                if (TclApi.CheckModule(tclApi) &&
                    (noThread || tclApi.CheckInterp(interp)))
                {
                    result = GetUnicodeString(
                        tclApi, GetResult(tclApi, interp, noThread));
                }
            }
            catch (Exception e)
            {
                //
                // NOTE: Nothing we can do here except log the failure.
                //
                TraceOps.DebugTrace(
                    e, typeof(TclWrapper).Name,
                    TracePriority.NativeError);
            }

            return result;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public static string GetResultAsUtf8String(
            ITclApi tclApi, /* in */
            IntPtr interp   /* in */
            )
        {
            return GetResultAsUtf8String(tclApi, interp, false);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        private static string GetResultAsUtf8String(
            ITclApi tclApi, /* in */
            IntPtr interp,  /* in */
            bool noThread   /* in */
            )
        {
            string result = null;

            try
            {
                if (TclApi.CheckModule(tclApi) &&
                    (noThread || tclApi.CheckInterp(interp)))
                {
                    result = GetUtf8String(
                        tclApi, GetResult(tclApi, interp, noThread));
                }
            }
            catch (Exception e)
            {
                //
                // NOTE: Nothing we can do here except log the failure.
                //
                TraceOps.DebugTrace(
                    e, typeof(TclWrapper).Name,
                    TracePriority.NativeError);
            }

            return result;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public static IntPtr GetResult(
            ITclApi tclApi, /* in */
            IntPtr interp   /* in */
            )
        {
            return GetResult(tclApi, interp, false);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        private static IntPtr GetResult(
            ITclApi tclApi, /* in */
            IntPtr interp,  /* in */
            bool noThread   /* in */
            )
        {
            IntPtr result = IntPtr.Zero;

            try
            {
                if (TclApi.CheckModule(tclApi))
                {
                    Tcl_GetObjResult getObjResult;

                    lock (tclApi.SyncRoot)
                    {
                        getObjResult = tclApi.GetObjResult;
                    }

                    if (noThread || tclApi.CheckInterp(interp))
                    {
                        if (getObjResult != null)
                            result = getObjResult(interp);
                    }
                }
            }
            catch (Exception e)
            {
                //
                // NOTE: Nothing we can do here except log the failure.
                //
                TraceOps.DebugTrace(
                    e, typeof(Tcl_GetObjResult).Name,
                    TracePriority.NativeError);
            }

            return result;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public static bool SetResult(
            ITclApi tclApi, /* in */
            IntPtr interp,  /* in */
            IntPtr objPtr   /* in */
            )
        {
            bool result = false;

            try
            {
                if (TclApi.CheckModule(tclApi))
                {
                    Tcl_SetObjResult setObjResult;

                    lock (tclApi.SyncRoot)
                    {
                        setObjResult = tclApi.SetObjResult;
                    }

                    if (tclApi.CheckInterp(interp))
                    {
                        if (setObjResult != null)
                        {
                            /* NO RESULT */
                            setObjResult(interp, objPtr);

                            result = true;
                        }
                    }
                }
            }
            catch (Exception e)
            {
                //
                // NOTE: Nothing we can do here except log the failure.
                //
                TraceOps.DebugTrace(
                    e, typeof(Tcl_SetObjResult).Name,
                    TracePriority.NativeError);
            }

            //
            // HACK: This is a very bad situation.  We have a result [that may
            //       have come from an Eagle command or script] and we cannot
            //       send it to Tcl (probably a threading issue).  To assist
            //       others in debugging this situation, we issue a debug
            //       diagnostic.
            //
            if (!result)
            {
                TraceOps.DebugTrace(String.Format(
                    "Tcl result cannot be set: {0}",
                    GetString(tclApi, objPtr)),
                    typeof(Tcl_SetObjResult).Name,
                    TracePriority.NativeError);
            }

            return result;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public static bool ResetResult(
            ITclApi tclApi, /* in */
            IntPtr interp   /* in */
            )
        {
            bool result = false;

            try
            {
                if (TclApi.CheckModule(tclApi))
                {
                    Tcl_ResetResult resetResult;

                    lock (tclApi.SyncRoot)
                    {
                        resetResult = tclApi.ResetResult;
                    }

                    if (tclApi.CheckInterp(interp))
                    {
                        if (resetResult != null)
                        {
                            /* NO RESULT */
                            resetResult(interp);

                            result = true;
                        }
                    }
                }
            }
            catch (Exception e)
            {
                //
                // NOTE: Nothing we can do here except log the failure.
                //
                TraceOps.DebugTrace(
                    e, typeof(Tcl_ResetResult).Name,
                    TracePriority.NativeError);
            }

            return result;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public static bool GetInterpDeleted(
            ITclApi tclApi, /* in */
            IntPtr interp   /* in */
            )
        {
            bool result = false;

            try
            {
                if (TclApi.CheckModule(tclApi))
                {
                    Tcl_InterpDeleted interpDeleted;

                    lock (tclApi.SyncRoot)
                    {
                        interpDeleted = tclApi.InterpDeleted;
                    }

                    if (tclApi.CheckInterp(interp))
                    {
                        if (interpDeleted != null)
                        {
                            result = (interpDeleted(interp) != 0);
                        }
                        else
                        {
                            //
                            // NOTE: Nothing we can do here except log the failure.
                            //
                            TraceOps.DebugTrace(
                                "Tcl interpreter introspection is not available",
                                typeof(Tcl_InterpDeleted).Name,
                                TracePriority.NativeError);
                        }
                    }
                }
            }
            catch (Exception e)
            {
                //
                // NOTE: Nothing we can do here except log the failure.
                //
                TraceOps.DebugTrace(
                    e, typeof(Tcl_InterpDeleted).Name,
                    TracePriority.NativeError);
            }

            return result;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public static bool GetInterpActive(
            ITclApi tclApi, /* in */
            IntPtr interp   /* in */
            )
        {
            bool result = false;

            try
            {
                if (TclApi.CheckModule(tclApi))
                {
                    Tcl_InterpActive interpActive;

                    lock (tclApi.SyncRoot)
                    {
                        interpActive = tclApi.InterpActive;
                    }

                    if (interpActive != null)
                    {
                        if (tclApi.CheckInterp(interp))
                            result = (interpActive(interp) != 0);
                    }
                    else
                    {
                        //
                        // HACK: Pre-TIP #335.  This is required for Tcl 8.4
                        //       and 8.5.
                        //
                        result = (TclApi.GetNumLevels(tclApi, interp) > 0);
                    }
                }
            }
            catch (Exception e)
            {
                //
                // NOTE: Nothing we can do here except log the failure.
                //
                TraceOps.DebugTrace(
                    e, typeof(Tcl_InterpActive).Name,
                    TracePriority.NativeError);
            }

            return result;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public static int GetErrorLine(
            ITclApi tclApi, /* in */
            IntPtr interp   /* in */
            )
        {
            int result = 0;

            try
            {
                if (TclApi.CheckModule(tclApi))
                {
                    Tcl_GetErrorLine getErrorLine;

                    lock (tclApi.SyncRoot)
                    {
                        getErrorLine = tclApi.GetErrorLine;
                    }

                    if (getErrorLine != null)
                    {
                        if (tclApi.CheckInterp(interp))
                            result = getErrorLine(interp);
                    }
                    else
                    {
                        //
                        // HACK: Pre-TIP #336.  This is required for Tcl 8.4
                        //       and 8.5.
                        //
                        result = TclApi._GetErrorLine(tclApi, interp);
                    }
                }
            }
            catch (Exception e)
            {
                //
                // NOTE: Nothing we can do here except log the failure.
                //
                TraceOps.DebugTrace(
                    e, typeof(Tcl_GetErrorLine).Name,
                    TracePriority.NativeError);
            }

            return result;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public static ReturnCode SetErrorLine(
            ITclApi tclApi,  /* in */
            IntPtr interp,   /* in */
            int line,        /* in */
            ref Result error /* out */
            )
        {
            ReturnCode code;

            try
            {
                if (TclApi.CheckModule(tclApi, ref error))
                {
                    Tcl_SetErrorLine setErrorLine;

                    lock (tclApi.SyncRoot)
                    {
                        setErrorLine = tclApi.SetErrorLine;
                    }

                    if (setErrorLine != null)
                    {
                        if (tclApi.CheckInterp(interp, ref error))
                        {
                            /* NO RESULT */
                            setErrorLine(interp, line);

                            code = ReturnCode.Ok;
                        }
                        else
                        {
                            code = ReturnCode.Error;
                        }
                    }
                    else
                    {
                        //
                        // HACK: Pre-TIP #336.  This is required for Tcl 8.4
                        //       and 8.5.
                        //
                        code = TclApi._SetErrorLine(
                            tclApi, interp, line, ref error);
                    }
                }
                else
                {
                    code = ReturnCode.Error;
                }
            }
            catch (Exception e)
            {
                error = e;
                code = ReturnCode.Error;
            }

            return code;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        private static Version GetVersion(
            ITclApi tclApi /* in */
            )
        {
            Version result = null;

            try
            {
                if (TclApi.CheckModule(tclApi))
                {
                    Tcl_GetVersion getVersion;

                    lock (tclApi.SyncRoot)
                    {
                        getVersion = tclApi.GetVersion;
                    }

                    if (getVersion != null)
                    {
                        int major, minor, patchLevel;
                        Tcl_ReleaseLevel releaseLevel;

                        /* NO RESULT */
                        getVersion(
                            out major, out minor,
                            out patchLevel, out releaseLevel);

                        result = GlobalState.GetFourPartVersion(
                            major, minor, (int)releaseLevel,
                            patchLevel);
                    }
                }
            }
            catch (Exception e)
            {
                //
                // NOTE: Nothing we can do here except log the failure.
                //
                TraceOps.DebugTrace(
                    e, typeof(Tcl_GetVersion).Name,
                    TracePriority.NativeError);
            }

            return result;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public static Version GetFileVersion(
            string fileName /* in */
            )
        {
            Result error = null;

            return GetFileVersion(fileName, ref error);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        private static Version GetFileVersion(
            string fileName, /* in */
            ref Result error /* in */
            )
        {
            ReturnCode code;
            FileVersionInfo version = null;

            code = FileOps.GetFileVersion(
                fileName, true, ref version, ref error);

            if (code == ReturnCode.Ok)
            {
                return GlobalState.GetFourPartVersion(
                    version.FileMajorPart,
                    version.FileMinorPart,
                    version.FileBuildPart,
                    version.FilePrivatePart
                );
            }

            return null;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public static ReturnCode ConvertToType(
            ITclApi tclApi,  /* in */
            IntPtr interp,   /* in */
            string text,     /* in */
            string name,     /* in */
            ref Result error /* out */
            )
        {
            ReturnCode code;
            IntPtr objPtr = IntPtr.Zero;

            try
            {
                if (TclApi.CheckModule(tclApi, ref error))
                {
                    Tcl_GetObjType getObjType;
                    Tcl_ConvertToType convertToType;

                    lock (tclApi.SyncRoot)
                    {
                        getObjType = tclApi.GetObjType;
                        convertToType = tclApi.ConvertToType;
                    }

                    if (tclApi.CheckInterp(interp, ref error))
                    {
                        if (text != null)
                        {
                            if (name != null)
                            {
                                if (getObjType != null)
                                {
                                    IntPtr typePtr = getObjType(name);

                                    if (typePtr != IntPtr.Zero)
                                    {
                                        objPtr = NewString(tclApi, text);

                                        if (objPtr != IntPtr.Zero)
                                        {
                                            if (convertToType != null)
                                            {
                                                code = convertToType(interp, objPtr, typePtr);

                                                if (code != ReturnCode.Ok)
                                                    error = GetResultAsString(tclApi, interp);
                                            }
                                            else
                                            {
                                                error = "Tcl object type conversion is not available";
                                                code = ReturnCode.Error;
                                            }
                                        }
                                        else
                                        {
                                            error = "could not allocate Tcl object";
                                            code = ReturnCode.Error;
                                        }
                                    }
                                    else
                                    {
                                        error = String.Format(
                                            "object type {0} is not registered",
                                            FormatOps.DisplayName(name));

                                        code = ReturnCode.Error;
                                    }
                                }
                                else
                                {
                                    error = "Tcl object type introspection is not available";
                                    code = ReturnCode.Error;
                                }
                            }
                            else
                            {
                                error = "invalid object type name";
                                code = ReturnCode.Error;
                            }
                        }
                        else
                        {
                            error = "invalid string";
                            code = ReturnCode.Error;
                        }
                    }
                    else
                    {
                        code = ReturnCode.Error;
                    }
                }
                else
                {
                    code = ReturnCode.Error;
                }
            }
            catch (Exception e)
            {
                error = e;
                code = ReturnCode.Error;
            }
            finally
            {
                if (TclApi.CheckModule(tclApi) &&
                    (objPtr != IntPtr.Zero))
                {
                    Tcl_DbDecrRefCount dbDecrRefCount;

                    lock (tclApi.SyncRoot)
                    {
                        dbDecrRefCount = tclApi.DbDecrRefCount;
                    }

                    if (dbDecrRefCount != null)
                        /* NO RESULT */
                        dbDecrRefCount(objPtr, String.Empty, 0);

                    objPtr = IntPtr.Zero;
                }
            }

            return code;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public static ReturnCode GetAllObjectTypes(
            ITclApi tclApi,   /* in */
            IntPtr interp,    /* in */
            ref Result result /* out */
            )
        {
            ReturnCode code;
            IntPtr objPtr = IntPtr.Zero;

            try
            {
                if (TclApi.CheckModule(tclApi, ref result))
                {
                    Tcl_AppendAllObjTypes appendAllObjTypes;

                    lock (tclApi.SyncRoot)
                    {
                        appendAllObjTypes = tclApi.AppendAllObjTypes;
                    }

                    if (tclApi.CheckInterp(interp, ref result))
                    {
                        objPtr = NewObject(tclApi);

                        if (objPtr != IntPtr.Zero)
                        {
                            if (appendAllObjTypes != null)
                            {
                                code = appendAllObjTypes(interp, objPtr);

                                if (code == ReturnCode.Ok)
                                    result = GetString(tclApi, objPtr);
                                else
                                    result = GetResultAsString(tclApi, interp);
                            }
                            else
                            {
                                result = "Tcl object type introspection is not available";
                                code = ReturnCode.Error;
                            }
                        }
                        else
                        {
                            result = "could not allocate Tcl object";
                            code = ReturnCode.Error;
                        }
                    }
                    else
                    {
                        code = ReturnCode.Error;
                    }
                }
                else
                {
                    code = ReturnCode.Error;
                }
            }
            catch (Exception e)
            {
                result = e;
                code = ReturnCode.Error;
            }
            finally
            {
                if (TclApi.CheckModule(tclApi) &&
                    (objPtr != IntPtr.Zero))
                {
                    Tcl_DbDecrRefCount dbDecrRefCount;

                    lock (tclApi.SyncRoot)
                    {
                        dbDecrRefCount = tclApi.DbDecrRefCount;
                    }

                    if (dbDecrRefCount != null)
                        /* NO RESULT */
                        dbDecrRefCount(objPtr, String.Empty, 0);

                    objPtr = IntPtr.Zero;
                }
            }

            return code;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public static ReturnCode CreateInterpreter(
            ITclApi tclApi,    /* in */
            bool initialize,   /* in */
            bool memory,       /* in */
            bool safe,         /* in */
            ref IntPtr interp, /* out */
            ref Result error   /* out */
            )
        {
            ReturnCode code = ReturnCode.Ok;
            IntPtr newInterp = IntPtr.Zero;

            try
            {
                if (TclApi.CheckModule(tclApi, ref error))
                {
                    Tcl_CreateInterp createInterp;
                    Tcl_MakeSafe makeSafe;
                    Tcl_Init init;
                    Tcl_InitMemory initMemory;

                    lock (tclApi.SyncRoot)
                    {
                        createInterp = tclApi.CreateInterp;
                        makeSafe = tclApi.MakeSafe;
                        init = tclApi.Init;
                        initMemory = tclApi.InitMemory;
                    }

                    if (interp == IntPtr.Zero)
                    {
                        if (createInterp != null)
                        {
                            newInterp = createInterp();

                            if (newInterp != IntPtr.Zero)
                            {
                                //
                                // BUGFIX: For "safe" Tcl interpreters, we do not
                                //         initialize them here.
                                //
                                if (safe)
                                {
                                    if (makeSafe != null)
                                    {
                                        code = makeSafe(newInterp);

                                        if (code != ReturnCode.Ok)
                                            error = GetResultAsString(tclApi, newInterp);
                                    }
                                    else
                                    {
                                        error = "Tcl interpreter safety is not available";
                                        code = ReturnCode.Error;
                                    }
                                }
                                else if (initialize)
                                {
                                    if (init != null)
                                    {
                                        code = init(newInterp);

                                        if (code != ReturnCode.Ok)
                                            error = GetResultAsString(tclApi, newInterp);
                                    }
                                    else
                                    {
                                        error = "Tcl interpreter initialization is not available";
                                        code = ReturnCode.Error;
                                    }
                                }

                                if (code == ReturnCode.Ok)
                                {
                                    //
                                    // NOTE: Add the memory command(s), if requested
                                    //       and the Tcl interpreter is not "safe".
                                    //
                                    if (memory && !safe)
                                    {
                                        if (initMemory != null)
                                        {
                                            /* NO RESULT */
                                            initMemory(newInterp);
                                        }
                                        else
                                        {
                                            error = "Tcl memory debugging is not available";
                                            code = ReturnCode.Error;
                                        }
                                    }

                                    if (code == ReturnCode.Ok)
                                        interp = newInterp;
                                }
                            }
                            else
                            {
                                error = "could not create Tcl interpreter";
                                code = ReturnCode.Error;
                            }
                        }
                        else
                        {
                            error = "Tcl interpreter creation is not available";
                            code = ReturnCode.Error;
                        }
                    }
                    else
                    {
                        error = "cannot overwrite valid Tcl interpreter";
                        code = ReturnCode.Error;
                    }
                }
                else
                {
                    code = ReturnCode.Error;
                }
            }
            catch (Exception e)
            {
                error = e;
                code = ReturnCode.Error;
            }
            finally
            {
                if ((code != ReturnCode.Ok) &&
                    TclApi.CheckModule(tclApi) &&
                    (newInterp != IntPtr.Zero))
                {
                    Tcl_DeleteInterp deleteInterp;

                    lock (tclApi.SyncRoot)
                    {
                        deleteInterp = tclApi.DeleteInterp;
                    }

                    if (deleteInterp != null)
                        /* NO RESULT */
                        deleteInterp(newInterp);

                    newInterp = IntPtr.Zero;
                }
            }

            return code;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public static ReturnCode DeleteInterpreter(
            ITclApi tclApi,    /* in */
            bool force,        /* in: Non-zero here is for INTERNAL USE ONLY. */
            ref IntPtr interp, /* in, out */
            ref Result error   /* out */
            )
        {
            ReturnCode code;

            try
            {
                if (TclApi.CheckModule(tclApi, ref error))
                {
                    Tcl_DeleteInterp deleteInterp;

                    lock (tclApi.SyncRoot)
                    {
                        deleteInterp = tclApi.DeleteInterp;
                    }

                    if (tclApi.CheckInterp(interp, ref error))
                    {
                        //
                        // HACK: Prevent deleting the Tcl interpreter while it is in use. Also
                        //       note that we cannot simply rely upon the protection provided by
                        //       Tcl_Preserve because if somebody tries to unload the whole
                        //       library while one of the interps is in use, the Tcl API object
                        //       could be pulled out from underneath us.
                        //
                        if (!GetInterpDeleted(tclApi, interp))
                        {
                            if (force || !GetInterpActive(tclApi, interp))
                            {
                                if (deleteInterp != null)
                                {
                                    /* NO RESULT */
                                    deleteInterp(interp);
                                    interp = IntPtr.Zero;

                                    code = ReturnCode.Ok;
                                }
                                else
                                {
                                    error = "Tcl interpreter deletion is not available";
                                    code = ReturnCode.Error;
                                }
                            }
                            else
                            {
                                error = "cannot delete Tcl interpreter, evals are active";
                                code = ReturnCode.Error;
                            }
                        }
                        else
                        {
                            error = "cannot delete Tcl interpreter, it was already deleted";
                            code = ReturnCode.Error;
                        }
                    }
                    else
                    {
                        code = ReturnCode.Error;
                    }
                }
                else
                {
                    code = ReturnCode.Error;
                }
            }
            catch (Exception e)
            {
                error = e;
                code = ReturnCode.Error;
            }

            return code;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public static ReturnCode Preserve(
            ITclApi tclApi,  /* in */
            IntPtr interp,   /* in */
            ref Result error /* out */
            )
        {
            try
            {
                if (TclApi.CheckModule(tclApi, ref error))
                {
                    Tcl_Preserve preserve;

                    lock (tclApi.SyncRoot)
                    {
                        preserve = tclApi.Preserve;
                    }

                    if (tclApi.CheckInterp(interp, ref error))
                    {
                        if (preserve != null)
                        {
                            /* NO RESULT */
                            preserve(interp);

                            return ReturnCode.Ok;
                        }
                        else
                        {
                            error = "Tcl reference counting is not available";
                        }
                    }
                }
            }
            catch (Exception e)
            {
                error = e;
            }

            return ReturnCode.Error;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public static ReturnCode Release(
            ITclApi tclApi,  /* in */
            IntPtr interp,   /* in */
            ref Result error /* out */
            )
        {
            try
            {
                if (TclApi.CheckModule(tclApi, ref error))
                {
                    Tcl_Release release;

                    lock (tclApi.SyncRoot)
                    {
                        release = tclApi.Release;
                    }

                    if (tclApi.CheckInterp(interp, ref error))
                    {
                        if (release != null)
                        {
                            /* NO RESULT */
                            release(interp);

                            return ReturnCode.Ok;
                        }
                        else
                        {
                            error = "Tcl reference counting is not available";
                        }
                    }
                }
            }
            catch (Exception e)
            {
                error = e;
            }

            return ReturnCode.Error;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public static ReturnCode CreateCommand(
            ITclApi tclApi,               /* in */
            IntPtr interp,                /* in */
            string name,                  /* in */
            Tcl_ObjCmdProc proc,          /* in */
            IntPtr clientData,            /* in: may be NULL. */
            Tcl_CmdDeleteProc deleteProc, /* in: may be NULL. */
            ref IntPtr token,             /* out */
            ref Result error              /* out */
            )
        {
            ReturnCode code;

            try
            {
                if (TclApi.CheckModule(tclApi, ref error))
                {
                    Tcl_CreateObjCommand createObjCommand;

                    lock (tclApi.SyncRoot)
                    {
                        createObjCommand = tclApi.CreateObjCommand;
                    }

                    if (tclApi.CheckInterp(interp, ref error))
                    {
                        if (token == IntPtr.Zero)
                        {
                            //
                            // NOTE: *WARNING* Empty Tcl command/procedure names are allowed,
                            //       please do not change this to "!String.IsNullOrEmpty".
                            //
                            if (name != null)
                            {
                                if (proc != null)
                                {
                                    if (createObjCommand != null)
                                    {
                                        token = createObjCommand(
                                            interp, name, proc, clientData, deleteProc);

                                        if (token != IntPtr.Zero)
                                        {
                                            code = ReturnCode.Ok;
                                        }
                                        else
                                        {
                                            error = "could not create command";
                                            code = ReturnCode.Error;
                                        }
                                    }
                                    else
                                    {
                                        error = "Tcl command creation is not available";
                                        code = ReturnCode.Error;
                                    }
                                }
                                else
                                {
                                    error = "invalid command proc";
                                    code = ReturnCode.Error;
                                }
                            }
                            else
                            {
                                error = "invalid command name";
                                code = ReturnCode.Error;
                            }
                        }
                        else
                        {
                            error = "cannot overwrite valid Tcl command token";
                            code = ReturnCode.Error;
                        }
                    }
                    else
                    {
                        code = ReturnCode.Error;
                    }
                }
                else
                {
                    code = ReturnCode.Error;
                }
            }
            catch (Exception e)
            {
                error = e;
                code = ReturnCode.Error;
            }

            return code;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public static ReturnCode DeleteCommandFromToken(
            ITclApi tclApi,   /* in */
            IntPtr interp,    /* in */
            bool force,       /* in */
            ref IntPtr token, /* in, out */
            ref Result error  /* out */
            )
        {
            ReturnCode code;

            try
            {
                if (TclApi.CheckModule(tclApi, ref error))
                {
                    Tcl_DeleteCommandFromToken deleteCommandFromToken;

                    lock (tclApi.SyncRoot)
                    {
                        deleteCommandFromToken = tclApi.DeleteCommandFromToken;
                    }

                    if (tclApi.CheckInterp(interp, ref error))
                    {
                        if (token != IntPtr.Zero)
                        {
                            //
                            // HACK: Prevent deleting command while it may be in use. Also note that
                            //       we cannot simply rely upon the protection provided by Tcl_Preserve
                            //       because if somebody tries to unload the whole library while one
                            //       of the interps is in use, the Tcl API object could be pulled out
                            //       from underneath us.
                            //
                            if (!GetInterpDeleted(tclApi, interp))
                            {
                                //
                                // NOTE: If the Tcl interpreter was already deleted, so was the command.
                                //
                                if (force || !GetInterpActive(tclApi, interp))
                                {
                                    if (deleteCommandFromToken != null)
                                    {
                                        //
                                        // NOTE: Attempt to delete the command and check the result to see
                                        //       if it was actually deleted.  Normally, we would pass the
                                        //       result of this call back to the caller as well; however,
                                        //       in this case the result is almost useless by itself, either
                                        //       the command was deleted or it was not.  If the command was
                                        //       successfully deleted we return Ok.  If the command was not
                                        //       deleted for any reason (including the failure of
                                        //       Tcl_DeleteCommandFromToken), we return Error.
                                        //
                                        if (deleteCommandFromToken(interp, token) == 0)
                                        {
                                            token = IntPtr.Zero;
                                            code = ReturnCode.Ok;
                                        }
                                        else
                                        {
                                            error = "could not delete command";
                                            code = ReturnCode.Error;
                                        }
                                    }
                                    else
                                    {
                                        error = "Tcl command deletion is not available";
                                        code = ReturnCode.Error;
                                    }
                                }
                                else
                                {
                                    error = "cannot delete Tcl command, evals are active";
                                    code = ReturnCode.Error;
                                }
                            }
                            else
                            {
                                error = "cannot delete Tcl command, it was already deleted";
                                code = ReturnCode.Error;
                            }
                        }
                        else
                        {
                            error = "invalid command token";
                            code = ReturnCode.Error;
                        }
                    }
                    else
                    {
                        code = ReturnCode.Error;
                    }
                }
                else
                {
                    code = ReturnCode.Error;
                }
            }
            catch (Exception e)
            {
                error = e;
                code = ReturnCode.Error;
            }

            return code;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public static ReturnCode GetVariable(
            ITclApi tclApi,     /* in */
            IntPtr interp,      /* in */
            Tcl_VarFlags flags, /* in */
            string name,        /* in */
            ref Result value,   /* out */
            ref Result error    /* out */
            )
        {
            ReturnCode code;
            IntPtr part1Ptr = IntPtr.Zero;

            try
            {
                if (TclApi.CheckModule(tclApi, ref error))
                {
                    Tcl_Preserve preserve;
                    Tcl_ObjGetVar2 objGetVar2;
                    Tcl_Release release;

                    lock (tclApi.SyncRoot)
                    {
                        preserve = tclApi.Preserve;
                        objGetVar2 = tclApi.ObjGetVar2;
                        release = tclApi.Release;
                    }

                    if (tclApi.CheckInterp(interp, ref error))
                    {
                        if (name != null)
                        {
                            part1Ptr = NewString(tclApi, name);

                            if (part1Ptr != IntPtr.Zero)
                            {
                                //
                                // NOTE: Variable traces may be triggered that can execute
                                //       arbitrary code; therefore, preserve the interpreter
                                //       now.
                                //
                                if (preserve != null)
                                    /* NO RESULT */
                                    preserve(interp);

                                try
                                {
                                    if (objGetVar2 != null)
                                    {
                                        IntPtr bufferPtr = objGetVar2(
                                            interp, part1Ptr, IntPtr.Zero, flags);

                                        if (bufferPtr != IntPtr.Zero)
                                        {
                                            value = GetString(tclApi, bufferPtr);
                                            code = ReturnCode.Ok;
                                        }
                                        else
                                        {
                                            if (FlagOps.HasFlags(flags, Tcl_VarFlags.TCL_LEAVE_ERR_MSG, true))
                                                error = GetResultAsString(tclApi, interp);
                                            else
                                                error = "attempt to get variable failed";

                                            code = ReturnCode.Error;
                                        }
                                    }
                                    else
                                    {
                                        error = "Tcl variable reading is not available";
                                        code = ReturnCode.Error;
                                    }
                                }
                                finally
                                {
                                    if (release != null)
                                        /* NO RESULT */
                                        release(interp);
                                }
                            }
                            else
                            {
                                error = "could not allocate Tcl object";
                                code = ReturnCode.Error;
                            }
                        }
                        else
                        {
                            error = "invalid variable name";
                            code = ReturnCode.Error;
                        }
                    }
                    else
                    {
                        code = ReturnCode.Error;
                    }
                }
                else
                {
                    code = ReturnCode.Error;
                }
            }
            catch (Exception e)
            {
                error = e;
                code = ReturnCode.Error;
            }
            finally
            {
                if (TclApi.CheckModule(tclApi) &&
                    (part1Ptr != IntPtr.Zero))
                {
                    Tcl_DbDecrRefCount dbDecrRefCount;

                    lock (tclApi.SyncRoot)
                    {
                        dbDecrRefCount = tclApi.DbDecrRefCount;
                    }

                    if (dbDecrRefCount != null)
                        /* NO RESULT */
                        dbDecrRefCount(part1Ptr, String.Empty, 0);

                    part1Ptr = IntPtr.Zero;
                }
            }

            return code;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public static ReturnCode SetVariable(
            ITclApi tclApi,     /* in */
            IntPtr interp,      /* in */
            Tcl_VarFlags flags, /* in */
            string name,        /* in */
            ref Result value,   /* in, out: Do not change to ByVal, traces can modify the value. */
            ref Result error    /* out */
            )
        {
            ReturnCode code;
            IntPtr part1Ptr = IntPtr.Zero;
            IntPtr newValuePtr = IntPtr.Zero;

            try
            {
                if (TclApi.CheckModule(tclApi, ref error))
                {
                    Tcl_Preserve preserve;
                    Tcl_ObjSetVar2 objSetVar2;
                    Tcl_Release release;

                    lock (tclApi.SyncRoot)
                    {
                        preserve = tclApi.Preserve;
                        objSetVar2 = tclApi.ObjSetVar2;
                        release = tclApi.Release;
                    }

                    if (tclApi.CheckInterp(interp, ref error))
                    {
                        if (name != null)
                        {
                            if (value != null)
                            {
                                part1Ptr = NewString(tclApi, name);

                                if (part1Ptr != IntPtr.Zero)
                                {
                                    newValuePtr = NewString(tclApi, value);

                                    if (newValuePtr != IntPtr.Zero)
                                    {
                                        //
                                        // NOTE: Variable traces may be triggered that can execute
                                        //       arbitrary code; therefore, preserve the interpreter
                                        //       now.
                                        //
                                        if (preserve != null)
                                            /* NO RESULT */
                                            preserve(interp);

                                        try
                                        {
                                            if (objSetVar2 != null)
                                            {
                                                IntPtr bufferPtr = objSetVar2(
                                                    interp, part1Ptr, IntPtr.Zero, newValuePtr, flags);

                                                if (bufferPtr != IntPtr.Zero)
                                                {
                                                    value = GetString(tclApi, bufferPtr);
                                                    code = ReturnCode.Ok;
                                                }
                                                else
                                                {
                                                    if (FlagOps.HasFlags(flags, Tcl_VarFlags.TCL_LEAVE_ERR_MSG, true))
                                                        error = GetResultAsString(tclApi, interp);
                                                    else
                                                        error = "attempt to set variable failed";

                                                    code = ReturnCode.Error;
                                                }
                                            }
                                            else
                                            {
                                                error = "Tcl variable writing is not available";
                                                code = ReturnCode.Error;
                                            }
                                        }
                                        finally
                                        {
                                            if (release != null)
                                                /* NO RESULT */
                                                release(interp);
                                        }
                                    }
                                    else
                                    {
                                        error = "could not allocate Tcl object";
                                        code = ReturnCode.Error;
                                    }
                                }
                                else
                                {
                                    error = "could not allocate Tcl object";
                                    code = ReturnCode.Error;
                                }
                            }
                            else
                            {
                                error = "invalid variable value";
                                code = ReturnCode.Error;
                            }
                        }
                        else
                        {
                            error = "invalid variable name";
                            code = ReturnCode.Error;
                        }
                    }
                    else
                    {
                        code = ReturnCode.Error;
                    }
                }
                else
                {
                    code = ReturnCode.Error;
                }
            }
            catch (Exception e)
            {
                error = e;
                code = ReturnCode.Error;
            }
            finally
            {
                if (TclApi.CheckModule(tclApi))
                {
                    Tcl_DbDecrRefCount dbDecrRefCount;

                    lock (tclApi.SyncRoot)
                    {
                        dbDecrRefCount = tclApi.DbDecrRefCount;
                    }

                    if (newValuePtr != IntPtr.Zero)
                    {
                        if (dbDecrRefCount != null)
                            /* NO RESULT */
                            dbDecrRefCount(newValuePtr, String.Empty, 0);

                        newValuePtr = IntPtr.Zero;
                    }

                    if (part1Ptr != IntPtr.Zero)
                    {
                        if (dbDecrRefCount != null)
                            /* NO RESULT */
                            dbDecrRefCount(part1Ptr, String.Empty, 0);

                        part1Ptr = IntPtr.Zero;
                    }
                }
            }

            return code;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public static ReturnCode UnsetVariable(
            ITclApi tclApi,     /* in */
            IntPtr interp,      /* in */
            Tcl_VarFlags flags, /* in */
            string name,        /* in */
            ref Result error    /* out */
            )
        {
            ReturnCode code;

            try
            {
                if (TclApi.CheckModule(tclApi, ref error))
                {
                    Tcl_Preserve preserve;
                    Tcl_UnsetVar2 unsetVar2;
                    Tcl_Release release;

                    lock (tclApi.SyncRoot)
                    {
                        preserve = tclApi.Preserve;
                        unsetVar2 = tclApi.UnsetVar2;
                        release = tclApi.Release;
                    }

                    if (tclApi.CheckInterp(interp, ref error))
                    {
                        if (name != null)
                        {
                            //
                            // NOTE: Variable traces may be triggered that can execute
                            //       arbitrary code; therefore, preserve the interpreter
                            //       now.
                            //
                            if (preserve != null)
                                /* NO RESULT */
                                preserve(interp);

                            try
                            {
                                if (unsetVar2 != null)
                                {
                                    code = unsetVar2(interp, name, null, flags);

                                    if (code != ReturnCode.Ok)
                                    {
                                        if (FlagOps.HasFlags(flags, Tcl_VarFlags.TCL_LEAVE_ERR_MSG, true))
                                            error = GetResultAsString(tclApi, interp);
                                        else
                                            error = "attempt to unset variable failed";

                                        code = ReturnCode.Error;
                                    }
                                }
                                else
                                {
                                    error = "Tcl variable unsetting is not available";
                                    code = ReturnCode.Error;
                                }
                            }
                            finally
                            {
                                if (release != null)
                                    /* NO RESULT */
                                    release(interp);
                            }
                        }
                        else
                        {
                            error = "invalid variable name";
                            code = ReturnCode.Error;
                        }
                    }
                    else
                    {
                        code = ReturnCode.Error;
                    }
                }
                else
                {
                    code = ReturnCode.Error;
                }
            }
            catch (Exception e)
            {
                error = e;
                code = ReturnCode.Error;
            }

            return code;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public static ReturnCode IsCommandComplete(
            ITclApi tclApi,    /* in */
            string text,       /* in */
            ref bool complete, /* out */
            ref Result result  /* out */
            )
        {
            ReturnCode code;

            try
            {
                if (TclApi.CheckModule(tclApi, ref result))
                {
                    Tcl_CommandComplete commandComplete;

                    lock (tclApi.SyncRoot)
                    {
                        commandComplete = tclApi.CommandComplete;
                    }

                    if (text != null)
                    {
                        if (commandComplete != null)
                        {
                            complete = (commandComplete(text) != 0);
                            code = ReturnCode.Ok;
                        }
                        else
                        {
                            result = "Tcl command completeness checking is not available";
                            code = ReturnCode.Error;
                        }
                    }
                    else
                    {
                        result = "invalid string";
                        code = ReturnCode.Error;
                    }
                }
                else
                {
                    code = ReturnCode.Error;
                }
            }
            catch (Exception e)
            {
                result = e;
                code = ReturnCode.Error;
            }

            return code;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public static ReturnCode EvaluateScript(
            ITclApi tclApi,             /* in */
            IntPtr interp,              /* in */
            string text,                /* in */
            Tcl_EvalFlags flags,        /* in */
            bool exceptions,            /* in */
            ref IClientData clientData, /* in, out */
            ref Result result           /* out */
            )
        {
            ReturnCode code;
            IntPtr objPtr = IntPtr.Zero;

            try
            {
                if (TclApi.CheckModule(tclApi, ref result))
                {
                    Tcl_Preserve preserve;
                    Tcl_AllowExceptions allowExceptions;
                    Tcl_EvalObjEx evalObjEx;
                    Tcl_Release release;

                    lock (tclApi.SyncRoot)
                    {
                        preserve = tclApi.Preserve;
                        allowExceptions = tclApi.AllowExceptions;
                        evalObjEx = tclApi.EvalObjEx;
                        release = tclApi.Release;
                    }

                    if (tclApi.CheckInterp(interp, ref result))
                    {
                        if (text != null)
                        {
                            objPtr = NewString(tclApi, text);

                            if (objPtr != IntPtr.Zero)
                            {
                                if (preserve != null)
                                    /* NO RESULT */
                                    preserve(interp);

                                try
                                {
                                    if (evalObjEx != null)
                                    {
                                        if (exceptions && (allowExceptions != null))
                                            allowExceptions(interp);

                                        PerformanceClientData performanceClientData =
                                            clientData as PerformanceClientData;

                                        if (performanceClientData != null)
                                            performanceClientData.Start();

                                        code = evalObjEx(interp, objPtr, flags);

                                        if (performanceClientData != null)
                                            performanceClientData.Stop();

                                        result = GetResultAsString(tclApi, interp);
                                    }
                                    else
                                    {
                                        result = "Tcl script evaluation is not available";
                                        code = ReturnCode.Error;
                                    }
                                }
                                finally
                                {
                                    if (release != null)
                                        /* NO RESULT */
                                        release(interp);
                                }
                            }
                            else
                            {
                                result = "could not allocate Tcl object";
                                code = ReturnCode.Error;
                            }
                        }
                        else
                        {
                            result = "invalid string";
                            code = ReturnCode.Error;
                        }
                    }
                    else
                    {
                        code = ReturnCode.Error;
                    }
                }
                else
                {
                    code = ReturnCode.Error;
                }
            }
            catch (Exception e)
            {
                result = e;
                code = ReturnCode.Error;
            }
            finally
            {
                if (TclApi.CheckModule(tclApi) &&
                    (objPtr != IntPtr.Zero))
                {
                    Tcl_DbDecrRefCount dbDecrRefCount;

                    lock (tclApi.SyncRoot)
                    {
                        dbDecrRefCount = tclApi.DbDecrRefCount;
                    }

                    if (dbDecrRefCount != null)
                        /* NO RESULT */
                        dbDecrRefCount(objPtr, String.Empty, 0);

                    objPtr = IntPtr.Zero;
                }
            }

            return code;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public static ReturnCode EvaluateFile(
            ITclApi tclApi,             /* in */
            IntPtr interp,              /* in */
            string fileName,            /* in */
            bool exceptions,            /* in */
            ref IClientData clientData, /* in, out */
            ref Result result           /* out */
            )
        {
            ReturnCode code;

            try
            {
                if (TclApi.CheckModule(tclApi, ref result))
                {
                    Tcl_Preserve preserve;
                    Tcl_AllowExceptions allowExceptions;
                    Tcl_EvalFile evalFile;
                    Tcl_Release release;

                    lock (tclApi.SyncRoot)
                    {
                        preserve = tclApi.Preserve;
                        allowExceptions = tclApi.AllowExceptions;
                        evalFile = tclApi.EvalFile;
                        release = tclApi.Release;
                    }

                    if (tclApi.CheckInterp(interp, ref result))
                    {
                        if (fileName != null)
                        {
                            if (preserve != null)
                                /* NO RESULT */
                                preserve(interp);

                            try
                            {
                                if (evalFile != null)
                                {
                                    if (exceptions && (allowExceptions != null))
                                        allowExceptions(interp);

                                    PerformanceClientData performanceClientData =
                                        clientData as PerformanceClientData;

                                    if (performanceClientData != null)
                                        performanceClientData.Start();

                                    code = evalFile(interp, fileName);

                                    if (performanceClientData != null)
                                        performanceClientData.Stop();

                                    result = GetResultAsString(tclApi, interp);
                                }
                                else
                                {
                                    result = "Tcl script evaluation is not available";
                                    code = ReturnCode.Error;
                                }
                            }
                            finally
                            {
                                if (release != null)
                                    /* NO RESULT */
                                    release(interp);
                            }
                        }
                        else
                        {
                            result = "invalid file name";
                            code = ReturnCode.Error;
                        }
                    }
                    else
                    {
                        code = ReturnCode.Error;
                    }
                }
                else
                {
                    code = ReturnCode.Error;
                }
            }
            catch (Exception e)
            {
                result = e;
                code = ReturnCode.Error;
            }

            return code;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public static ReturnCode RecordAndEvaluateScript(
            ITclApi tclApi,             /* in */
            IntPtr interp,              /* in */
            string text,                /* in */
            Tcl_EvalFlags flags,        /* in */
            bool exceptions,            /* in */
            ref IClientData clientData, /* in, out */
            ref Result result           /* out */
            )
        {
            ReturnCode code;
            IntPtr objPtr = IntPtr.Zero;

            try
            {
                if (TclApi.CheckModule(tclApi, ref result))
                {
                    Tcl_Preserve preserve;
                    Tcl_AllowExceptions allowExceptions;
                    Tcl_RecordAndEvalObj recordAndEvalObj;
                    Tcl_Release release;

                    lock (tclApi.SyncRoot)
                    {
                        preserve = tclApi.Preserve;
                        allowExceptions = tclApi.AllowExceptions;
                        recordAndEvalObj = tclApi.RecordAndEvalObj;
                        release = tclApi.Release;
                    }

                    if (tclApi.CheckInterp(interp, ref result))
                    {
                        if (text != null)
                        {
                            objPtr = NewString(tclApi, text);

                            if (objPtr != IntPtr.Zero)
                            {
                                if (preserve != null)
                                    /* NO RESULT */
                                    preserve(interp);

                                try
                                {
                                    if (recordAndEvalObj != null)
                                    {
                                        if (exceptions && (allowExceptions != null))
                                            allowExceptions(interp);

                                        PerformanceClientData performanceClientData =
                                            clientData as PerformanceClientData;

                                        if (performanceClientData != null)
                                            performanceClientData.Start();

                                        code = recordAndEvalObj(interp, objPtr, flags);

                                        if (performanceClientData != null)
                                            performanceClientData.Stop();

                                        result = GetResultAsString(tclApi, interp);
                                    }
                                    else
                                    {
                                        result = "Tcl script evaluation with history is not available";
                                        code = ReturnCode.Error;
                                    }
                                }
                                finally
                                {
                                    if (release != null)
                                        /* NO RESULT */
                                        release(interp);
                                }
                            }
                            else
                            {
                                result = "could not allocate Tcl object";
                                code = ReturnCode.Error;
                            }
                        }
                        else
                        {
                            result = "invalid string";
                            code = ReturnCode.Error;
                        }
                    }
                    else
                    {
                        code = ReturnCode.Error;
                    }
                }
                else
                {
                    code = ReturnCode.Error;
                }
            }
            catch (Exception e)
            {
                result = e;
                code = ReturnCode.Error;
            }
            finally
            {
                if (TclApi.CheckModule(tclApi) &&
                    (objPtr != IntPtr.Zero))
                {
                    Tcl_DbDecrRefCount dbDecrRefCount;

                    lock (tclApi.SyncRoot)
                    {
                        dbDecrRefCount = tclApi.DbDecrRefCount;
                    }

                    if (dbDecrRefCount != null)
                        /* NO RESULT */
                        dbDecrRefCount(objPtr, String.Empty, 0);

                    objPtr = IntPtr.Zero;
                }
            }

            return code;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public static ReturnCode EvaluateExpression(
            ITclApi tclApi,             /* in */
            IntPtr interp,              /* in */
            string text,                /* in */
            bool exceptions,            /* in */
            ref IClientData clientData, /* in, out */
            ref Result result           /* out */
            )
        {
            ReturnCode code;
            IntPtr objPtr = IntPtr.Zero;
            IntPtr resultPtr = IntPtr.Zero;

            try
            {
                if (TclApi.CheckModule(tclApi, ref result))
                {
                    Tcl_Preserve preserve;
                    Tcl_AllowExceptions allowExceptions;
                    Tcl_ExprObj exprObj;
                    Tcl_Release release;

                    lock (tclApi.SyncRoot)
                    {
                        preserve = tclApi.Preserve;
                        allowExceptions = tclApi.AllowExceptions;
                        exprObj = tclApi.ExprObj;
                        release = tclApi.Release;
                    }

                    if (tclApi.CheckInterp(interp, ref result))
                    {
                        if (text != null)
                        {
                            objPtr = NewString(tclApi, text);

                            if (objPtr != IntPtr.Zero)
                            {
                                if (preserve != null)
                                    /* NO RESULT */
                                    preserve(interp);

                                try
                                {
                                    if (exprObj != null)
                                    {
                                        if (exceptions && (allowExceptions != null))
                                            allowExceptions(interp);

                                        PerformanceClientData performanceClientData =
                                            clientData as PerformanceClientData;

                                        if (performanceClientData != null)
                                            performanceClientData.Start();

                                        code = exprObj(interp, objPtr, ref resultPtr);

                                        if (performanceClientData != null)
                                            performanceClientData.Stop();

                                        if (code == ReturnCode.Ok)
                                            result = GetString(tclApi, resultPtr);
                                        else
                                            result = GetResultAsString(tclApi, interp);
                                    }
                                    else
                                    {
                                        result = "Tcl expression evaluation is not available";
                                        code = ReturnCode.Error;
                                    }
                                }
                                finally
                                {
                                    if (release != null)
                                        /* NO RESULT */
                                        release(interp);
                                }
                            }
                            else
                            {
                                result = "could not allocate Tcl object";
                                code = ReturnCode.Error;
                            }
                        }
                        else
                        {
                            result = "invalid string";
                            code = ReturnCode.Error;
                        }
                    }
                    else
                    {
                        code = ReturnCode.Error;
                    }
                }
                else
                {
                    code = ReturnCode.Error;
                }
            }
            catch (Exception e)
            {
                result = e;
                code = ReturnCode.Error;
            }
            finally
            {
                if (TclApi.CheckModule(tclApi))
                {
                    Tcl_DbDecrRefCount dbDecrRefCount;

                    lock (tclApi.SyncRoot)
                    {
                        dbDecrRefCount = tclApi.DbDecrRefCount;
                    }

                    if (resultPtr != IntPtr.Zero)
                    {
                        if (dbDecrRefCount != null)
                            /* NO RESULT */
                            dbDecrRefCount(resultPtr, String.Empty, 0);

                        resultPtr = IntPtr.Zero;
                    }

                    if (objPtr != IntPtr.Zero)
                    {
                        if (dbDecrRefCount != null)
                            /* NO RESULT */
                            dbDecrRefCount(objPtr, String.Empty, 0);

                        objPtr = IntPtr.Zero;
                    }
                }
            }

            return code;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public static ReturnCode SubstituteString(
            ITclApi tclApi,             /* in */
            IntPtr interp,              /* in */
            string text,                /* in */
            Tcl_SubstFlags flags,       /* in */
            bool exceptions,            /* in */
            ref IClientData clientData, /* in, out */
            ref Result result           /* out */
            )
        {
            ReturnCode code;
            IntPtr objPtr = IntPtr.Zero;
            IntPtr resultPtr = IntPtr.Zero;

            try
            {
                if (TclApi.CheckModule(tclApi, ref result))
                {
                    Tcl_Preserve preserve;
                    Tcl_AllowExceptions allowExceptions;
                    Tcl_SubstObj substObj;
                    Tcl_DbIncrRefCount dbIncrRefCount;
                    Tcl_Release release;

                    lock (tclApi.SyncRoot)
                    {
                        preserve = tclApi.Preserve;
                        allowExceptions = tclApi.AllowExceptions;
                        substObj = tclApi.SubstObj;
                        dbIncrRefCount = tclApi.DbIncrRefCount;
                        release = tclApi.Release;
                    }

                    if (tclApi.CheckInterp(interp, ref result))
                    {
                        if (text != null)
                        {
                            objPtr = NewString(tclApi, text);

                            if (objPtr != IntPtr.Zero)
                            {
                                if (preserve != null)
                                    /* NO RESULT */
                                    preserve(interp);

                                try
                                {
                                    if (substObj != null)
                                    {
                                        if (exceptions && (allowExceptions != null))
                                            allowExceptions(interp);

                                        PerformanceClientData performanceClientData =
                                            clientData as PerformanceClientData;

                                        if (performanceClientData != null)
                                            performanceClientData.Start();

                                        resultPtr = substObj(interp, objPtr, flags);

                                        if (performanceClientData != null)
                                            performanceClientData.Stop();

                                        if (resultPtr != IntPtr.Zero)
                                        {
                                            if (dbIncrRefCount != null)
                                                /* NO RESULT */
                                                dbIncrRefCount(resultPtr, String.Empty, 0);

                                            result = GetString(tclApi, resultPtr);
                                            code = ReturnCode.Ok;
                                        }
                                        else
                                        {
                                            result = GetResultAsString(tclApi, interp);
                                            code = ReturnCode.Error;
                                        }
                                    }
                                    else
                                    {
                                        result = "Tcl string substitution is not available";
                                        code = ReturnCode.Error;
                                    }
                                }
                                finally
                                {
                                    if (release != null)
                                        /* NO RESULT */
                                        release(interp);
                                }
                            }
                            else
                            {
                                result = "could not allocate Tcl object";
                                code = ReturnCode.Error;
                            }
                        }
                        else
                        {
                            result = "invalid string";
                            code = ReturnCode.Error;
                        }
                    }
                    else
                    {
                        code = ReturnCode.Error;
                    }
                }
                else
                {
                    code = ReturnCode.Error;
                }
            }
            catch (Exception e)
            {
                result = e;
                code = ReturnCode.Error;
            }
            finally
            {
                if (TclApi.CheckModule(tclApi))
                {
                    Tcl_DbDecrRefCount dbDecrRefCount;

                    lock (tclApi.SyncRoot)
                    {
                        dbDecrRefCount = tclApi.DbDecrRefCount;
                    }

                    if (resultPtr != IntPtr.Zero)
                    {
                        if (dbDecrRefCount != null)
                            /* NO RESULT */
                            dbDecrRefCount(resultPtr, String.Empty, 0);

                        resultPtr = IntPtr.Zero;
                    }

                    if (objPtr != IntPtr.Zero)
                    {
                        if (dbDecrRefCount != null)
                            /* NO RESULT */
                            dbDecrRefCount(objPtr, String.Empty, 0);

                        objPtr = IntPtr.Zero;
                    }
                }
            }

            return code;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public static ReturnCode CancelEvaluate(
            ITclApi tclApi,             /* in */
            IntPtr interp,              /* in */
            Result result,              /* in */
            Tcl_EvalFlags flags,        /* in */
            ref IClientData clientData, /* in, out */
            ref Result error            /* out */
            ) /* THREAD-SAFE */
        {
            ReturnCode code = ReturnCode.Ok;
            IntPtr objPtr = IntPtr.Zero;

            try
            {
                if (TclApi.CheckModule(tclApi, ref error))
                {
                    Tcl_CancelEval cancelEval;

                    lock (tclApi.SyncRoot)
                    {
                        cancelEval = tclApi.CancelEval;
                    }

                    if (cancelEval != null)
                    {
                        //
                        // BUGFIX: Do not use tclApi.CheckInterp here because this function
                        //         is allowed to be called from any thread (per TIP #285).
                        //
                        if (interp != IntPtr.Zero)
                        {
                            //
                            // NOTE: If a specific cancellation result was requested,
                            //       allocate an object and set it up now.
                            //
                            if (result != null)
                                objPtr = NewString(tclApi, result);

                            //
                            // NOTE: If we tried to allocate a result object make sure
                            //       it succeeded.
                            //
                            if ((result == null) || (objPtr != IntPtr.Zero))
                            {
                                PerformanceClientData performanceClientData =
                                    clientData as PerformanceClientData;

                                if (performanceClientData != null)
                                    performanceClientData.Start();

                                code = cancelEval(interp, objPtr, IntPtr.Zero, flags);

                                if (performanceClientData != null)
                                    performanceClientData.Stop();

                                if (code != ReturnCode.Ok)
                                    error = "attempt to cancel eval failed";
                            }
                            else
                            {
                                result = "could not allocate Tcl object";
                                code = ReturnCode.Error;
                            }
                        }
                        else
                        {
                            error = "invalid Tcl interpreter";
                            code = ReturnCode.Error;
                        }
                    }
                    else
                    {
                        error = "Tcl script cancellation is not available";
                        code = ReturnCode.Error;
                    }
                }
                else
                {
                    code = ReturnCode.Error;
                }
            }
            catch (Exception e)
            {
                error = e;
                code = ReturnCode.Error;
            }
            finally
            {
                //
                // NOTE: Do *NOT* try to free this object if the call to Tcl_CancelEval
                //       succeeded (i.e. do not call DbDecrRefCount on it) because
                //       success indicates that ownership of the result object has been
                //       transferred; however, if CancelEval fails, we need to free the
                //       object because we still own it in that case.
                //
                if ((code != ReturnCode.Ok) &&
                    TclApi.CheckModule(tclApi) &&
                    (objPtr != IntPtr.Zero))
                {
                    Tcl_DbDecrRefCount dbDecrRefCount;

                    lock (tclApi.SyncRoot)
                    {
                        dbDecrRefCount = tclApi.DbDecrRefCount;
                    }

                    if (dbDecrRefCount != null)
                        /* NO RESULT */
                        dbDecrRefCount(objPtr, String.Empty, 0);

                    objPtr = IntPtr.Zero;
                }
            }

            return code;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public static ReturnCode Canceled(
            ITclApi tclApi,          /* in */
            IntPtr interp,           /* in */
            Tcl_CanceledFlags flags, /* in */
            ref Result error         /* out */
            )
        {
            ReturnCode code;

            try
            {
                if (TclApi.CheckModule(tclApi, ref error))
                {
                    Tcl_Canceled canceled;

                    lock (tclApi.SyncRoot)
                    {
                        canceled = tclApi.Canceled;
                    }

                    if (tclApi.CheckInterp(interp, ref error))
                    {
                        if (canceled != null)
                        {
                            code = canceled(interp, flags);

                            if (code != ReturnCode.Ok)
                                error = GetResultAsString(tclApi, interp);
                        }
                        else
                        {
                            error = "Tcl script cancellation is not available";
                            code = ReturnCode.Error;
                        }
                    }
                    else
                    {
                        code = ReturnCode.Error;
                    }
                }
                else
                {
                    code = ReturnCode.Error;
                }
            }
            catch (Exception e)
            {
                error = e;
                code = ReturnCode.Error;
            }

            return code;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public static ReturnCode ResetCancellation(
            ITclApi tclApi,  /* in */
            IntPtr interp,   /* in */
            bool force,      /* in */
            ref Result error /* out */
            )
        {
            ReturnCode code;

            try
            {
                if (TclApi.CheckModule(tclApi, ref error))
                {
                    TclResetCancellation resetCancellation;

                    lock (tclApi.SyncRoot)
                    {
                        resetCancellation = tclApi.ResetCancellation;
                    }

                    if (tclApi.CheckInterp(interp, ref error))
                    {
                        if (resetCancellation != null)
                        {
                            code = resetCancellation(interp, ConversionOps.ToInt(force));

                            if (code != ReturnCode.Ok)
                                error = "attempt to reset cancellation failed";
                        }
                        else
                        {
                            error = "Tcl script cancellation is not available";
                            code = ReturnCode.Error;
                        }
                    }
                    else
                    {
                        code = ReturnCode.Error;
                    }
                }
                else
                {
                    code = ReturnCode.Error;
                }
            }
            catch (Exception e)
            {
                error = e;
                code = ReturnCode.Error;
            }

            return code;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public static ReturnCode SetInterpCancelFlags(
            ITclApi tclApi,      /* in */
            IntPtr interp,       /* in */
            Tcl_EvalFlags flags, /* in */
            bool force,          /* in */
            ref Result error     /* out */
            )
        {
            try
            {
                if (TclApi.CheckModule(tclApi, ref error))
                {
                    TclSetInterpCancelFlags setInterpCancelFlags;

                    lock (tclApi.SyncRoot)
                    {
                        setInterpCancelFlags = tclApi.SetInterpCancelFlags;
                    }

                    if (tclApi.CheckInterp(interp, ref error))
                    {
                        if (setInterpCancelFlags != null)
                        {
                            /* NO RESULT */
                            setInterpCancelFlags(interp, flags, ConversionOps.ToInt(force));

                            return ReturnCode.Ok;
                        }
                        else
                        {
                            error = "Tcl script cancellation is not available";
                        }
                    }
                }
            }
            catch (Exception e)
            {
                error = e;
            }

            return ReturnCode.Error;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        //
        // NOTE: This method overload is for use by the TclThread class only.
        //
#if TCL_WRAPPER
        internal
#else
        public
#endif
        static ReturnCode DoOneEvent(
            Interpreter interpreter, /* in */
            int timeout,             /* in */
            bool wait,               /* in */
            bool all,                /* in */
            bool noComplain,         /* in */
            ref ITclApi tclApi,      /* in, out */
            ref Result error         /* out */
            )
        {
            int eventCount = 0;
            int sleepCount = 0;

            return DoOneEvent(
                interpreter, timeout, wait, all, noComplain,
                ref eventCount, ref sleepCount, ref tclApi,
                ref error);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public static ReturnCode DoOneEvent(
            Interpreter interpreter, /* in */
            int timeout,             /* in */
            bool wait,               /* in */
            bool all,                /* in */
            bool noComplain,         /* in */
            ref int eventCount,      /* in, out */
            ref int sleepCount,      /* in, out */
            ref ITclApi tclApi,      /* in, out */
            ref Result error         /* out */
            )
        {
            ReturnCode code;

            try
            {
                if (interpreter != null)
                {
                    if (TclApi.CheckModule(tclApi, ref error))
                    {
                        string fileName;
                        Tcl_DoOneEvent doOneEvent;

                        lock (tclApi.SyncRoot)
                        {
                            fileName = tclApi.FileName;
                            doOneEvent = tclApi.DoOneEvent;
                        }

                        //
                        // NOTE: Does this Tcl API object want to handle events?
                        //
                        if (doOneEvent != null)
                        {
                            //
                            // NOTE: Since Tcl_DoOneEvent can execute arbitrary code we
                            //       need to protect against unloading the Tcl library
                            //       out from under ourselves (primarily via the exit
                            //       handler).
                            //
                            TclModule module = null;

                            if (
#if NATIVE_PACKAGE
                                NativePackage.IsTclInterpreterActive() ||
#endif
                                AddModuleReference(fileName, ref module, ref error))
                            {
                                try
                                {
                                    Tcl_EventFlags flags = Tcl_EventFlags.TCL_ALL_EVENTS;

                                    if (!wait)
                                        flags |= Tcl_EventFlags.TCL_DONT_WAIT;

                                    if (all)
                                    {
                                        //
                                        // NOTE: Keep going while we have not been canceled and we have Tcl
                                        //       events to process.
                                        //
                                        int newEventCount = 0;

                                        //
                                        // NOTE: The Tcl_DoOneEvent call here may never return (e.g. if
                                        //       something calls Tcl_Exit, etc).
                                        //
                                        bool sleepTrace = false;

                                        while (((code = Interpreter.TclReady(
                                                interpreter, timeout, ref error)) == ReturnCode.Ok) &&
                                            ((module == null) || ((code = module.VerifyModule(
                                                ref error)) == ReturnCode.Ok)) &&
                                            ((newEventCount = doOneEvent(flags)) != 0))
                                        {
                                            //
                                            // NOTE: We processed some more events.
                                            //
                                            eventCount += newEventCount;

                                            //
                                            // NOTE: Yield to other running threads.  This (also)
                                            //       gives them a small opportunity to cancel the
                                            //       script in progress on this thread.
                                            //
                                            Result sleepError = null;

                                            if (EventOps.Sleep(
                                                    interpreter, SleepType.TclWrapper, false,
                                                    ref sleepError)) /* throw */
                                            {
                                                sleepCount++;
                                            }
                                            else if (!sleepTrace)
                                            {
                                                sleepTrace = true;

                                                TraceOps.DebugTrace(String.Format(
                                                    "DoOneEvent: sleepError = {0}",
                                                    FormatOps.WrapOrNull(sleepError)),
                                                    typeof(TclWrapper).Name,
                                                    TracePriority.ThreadError);
                                            }
                                        }
                                    }
                                    else
                                    {
                                        code = (module != null) ?
                                            module.VerifyModule(ref error) : ReturnCode.Ok;

                                        if (code == ReturnCode.Ok)
                                        {
                                            //
                                            // NOTE: The Tcl_DoOneEvent call here may never return
                                            //       (e.g. if something calls Tcl_Exit, etc).
                                            //
                                            eventCount += doOneEvent(flags);
                                        }
                                    }
                                }
                                finally
                                {
                                    //
                                    // NOTE: Reduce the Tcl library module reference count.
                                    //       If the count reaches zero, cleanup and unload
                                    //       the Tcl library.
                                    //
                                    Result releaseError = null;

                                    if (
#if NATIVE_PACKAGE
                                        !NativePackage.IsTclInterpreterActive() &&
#endif
                                        (ReleaseModuleReference(fileName, true, false, ref releaseError) == 0))
                                    {
                                        ReturnCode unloadCode;
                                        Result unloadError = null;

                                        unloadCode = Unload(interpreter, UnloadFlags.FromDoOneEvent,
                                            ref tclApi, ref unloadError);

                                        if (unloadCode != ReturnCode.Ok)
                                            DebugOps.Complain(interpreter, unloadCode, unloadError);
                                    }
                                    else if (!noComplain && (releaseError != null))
                                    {
                                        DebugOps.Complain(interpreter, ReturnCode.Error, releaseError);
                                    }
                                }
                            }
                            else
                            {
                                code = ReturnCode.Error;
                            }
                        }
                        else
                        {
                            error = "Tcl event processing is not available";
                            code = ReturnCode.Error;
                        }
                    }
                    else
                    {
                        code = ReturnCode.Error;
                    }
                }
                else
                {
                    error = "invalid interpreter";
                    code = ReturnCode.Error;
                }
            }
            catch (Exception e)
            {
                error = e;
                code = ReturnCode.Error;
            }

            return code;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        private static bool IsBuildViaExternals(
            TclBuild build /* in */
            )
        {
            return ((build != null) &&
                FlagOps.HasFlags(build.FindFlags, FindFlags.ExternalsPath, true));
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        private static bool IsBuildInstalled(
            TclBuild build /* in */
            )
        {
            //
            // HACK: Assume that the build is installed if it was found via
            //       the registry.
            //
            return ((build != null) &&
                FlagOps.HasFlags(build.FindFlags, FindFlags.Registry, true));
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        private static bool IsBuildActiveTcl(
            TclBuild build /* in */
            )
        {
            //
            // HACK: Assume that the build is ActiveTcl if the path contains
            //       the name fragment "ActiveState" or "ActiveTcl".
            //
            if (build == null)
                return false;

            string fileName = build.FileName;

            if (!CheckTclLibraryPath(fileName))
                return false;

            //
            // NOTE: This is always done on a case-insensitive basis.
            //
            if (Parser.StringMatch(
                    null, fileName, 0, ActiveStatePattern, 0, true) ||
                Parser.StringMatch(
                    null, fileName, 0, ActiveTclPattern, 0, true))
            {
                return true;
            }

            return false;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        private static bool IsBuildIronTcl(
            TclBuild build /* in */
            )
        {
            //
            // HACK: Assume that the build is IronTcl if the path contains
            //       the name fragment "Eyrie" or "IronTcl".
            //
            if (build == null)
                return false;

            string fileName = build.FileName;

            if (!CheckTclLibraryPath(fileName))
                return false;

            //
            // NOTE: This is always done on a case-insensitive basis.
            //
            if (Parser.StringMatch(
                    null, fileName, 0, EyriePattern, 0, true) ||
                Parser.StringMatch(
                    null, fileName, 0, IronTclPattern, 0, true))
            {
                return true;
            }

            return false;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

#if TCL_WRAPPER
        internal
#else
        public
#endif
        static bool IsBuildDefaultThreaded(
            TclBuild build /* in */
            )
        {
            //
            // HACK: Assume that the build is threaded if it is officially
            //       installed and has a high enough version.  We need to
            //       do this because the official binary builds of Tcl/Tk
            //       [on Windows] are always threaded after the given
            //       version even though their file names do not indicate
            //       this.
            //
            // HACK: Assume that the build is threaded if it was found via
            //       the externals path because they are included with the
            //       express purpose of supporting this subsystem.
            //
            if (build == null)
                return false;

            if ((PackageOps.VersionCompare(
                    build.PatchLevel, GetDefaultThreadedMinimumVersion(
                    build.FindFlags)) >= 0) &&
                (IsBuildViaExternals(build) || IsBuildInstalled(build) ||
                    IsBuildActiveTcl(build) || IsBuildIronTcl(build)))
            {
                return true;
            }

            return false;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

#if TCL_WRAPPER
        internal
#else
        public
#endif
        static Version GetDefaultMinimumVersion(
            FindFlags flags /* in */
            )
        {
            if (!FlagOps.HasFlags(flags, FindFlags.ZeroComponents, true))
                return DefaultMinimumVersion;

            if (DefaultMinimumVersion == null)
                return null;

            return GlobalState.GetFourPartVersion(
                DefaultMinimumVersion.Major, DefaultMinimumVersion.Minor,
                DefaultMinimumVersion.Build, DefaultMinimumVersion.Revision);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

#if TCL_WRAPPER
        internal
#else
        public
#endif
        static Version GetDefaultMaximumVersion(
            FindFlags flags /* in */
            )
        {
            if (!FlagOps.HasFlags(flags, FindFlags.ZeroComponents, true))
                return DefaultMaximumVersion;

            if (DefaultMaximumVersion == null)
                return null;

            return GlobalState.GetFourPartVersion(
                DefaultMaximumVersion.Major, DefaultMaximumVersion.Minor,
                DefaultMaximumVersion.Build, DefaultMaximumVersion.Revision);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

#if TCL_WRAPPER
        internal
#else
        public
#endif
        static Version GetDefaultUnknownVersion(
            FindFlags flags /* in */
            )
        {
            if (!FlagOps.HasFlags(flags, FindFlags.ZeroComponents, true))
                return DefaultUnknownVersion;

            if (DefaultUnknownVersion == null)
                return null;

            return GlobalState.GetFourPartVersion(
                DefaultUnknownVersion.Major, DefaultUnknownVersion.Minor,
                DefaultUnknownVersion.Build, DefaultUnknownVersion.Revision);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        private static Version GetDefaultThreadedMinimumVersion(
            FindFlags flags /* in */
            )
        {
            if (PlatformOps.IsWindowsOperatingSystem() ||
                PlatformOps.IsMacintoshOperatingSystem())
            {
                if (!FlagOps.HasFlags(flags, FindFlags.ZeroComponents, true))
                    return DefaultThreadedNonUnixMinimumVersion;

                if (DefaultThreadedNonUnixMinimumVersion == null)
                    return null;

                return GlobalState.GetFourPartVersion(
                    DefaultThreadedNonUnixMinimumVersion.Major,
                    DefaultThreadedNonUnixMinimumVersion.Minor,
                    DefaultThreadedNonUnixMinimumVersion.Build,
                    DefaultThreadedNonUnixMinimumVersion.Revision);
            }

            if (PlatformOps.IsUnixOperatingSystem())
            {
                if (!FlagOps.HasFlags(flags, FindFlags.ZeroComponents, true))
                    return DefaultThreadedUnixMinimumVersion;

                if (DefaultThreadedUnixMinimumVersion == null)
                    return null;

                return GlobalState.GetFourPartVersion(
                    DefaultThreadedUnixMinimumVersion.Major,
                    DefaultThreadedUnixMinimumVersion.Minor,
                    DefaultThreadedUnixMinimumVersion.Build,
                    DefaultThreadedUnixMinimumVersion.Revision);
            }

            return null;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

#if TCL_WRAPPER
        internal
#else
        public
#endif
        static int GetDefaultMajorIncrement(
            FindFlags flags /* in: NOT USED. */
            )
        {
            return DefaultMajorIncrement;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

#if TCL_WRAPPER
        internal
#else
        public
#endif
        static int GetDefaultMinorIncrement(
            FindFlags flags /* in: NOT USED. */
            )
        {
            return DefaultMinorIncrement;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

#if TCL_WRAPPER
        internal
#else
        public
#endif
        static int GetDefaultIntermediateMinimum(
            FindFlags flags /* in: NOT USED. */
            )
        {
            return DefaultIntermediateMinimum;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

#if TCL_WRAPPER
        internal
#else
        public
#endif
        static int GetDefaultIntermediateMaximum(
            FindFlags flags /* in: NOT USED. */
            )
        {
            return DefaultIntermediateMaximum;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

#if TCL_WRAPPER
        internal
#else
        public
#endif
        static Tcl_EvalFlags GetCancelEvaluateFlags(
            bool unwind /* in */
            )
        {
            Tcl_EvalFlags flags = Tcl_EvalFlags.TCL_EVAL_NONE;

            if (unwind)
                flags |= Tcl_EvalFlags.TCL_CANCEL_UNWIND;

            return flags;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

#if TCL_WRAPPER
        internal
#else
        public
#endif
        static Tcl_CanceledFlags GetCanceledFlags(
            bool unwind,    /* in */
            bool needResult /* in */
            )
        {
            Tcl_CanceledFlags flags = Tcl_CanceledFlags.TCL_CANCEL_NONE;

            if (unwind)
                flags |= Tcl_CanceledFlags.TCL_CANCEL_UNWIND;

            if (needResult)
                flags |= Tcl_CanceledFlags.TCL_LEAVE_ERR_MSG;

            return flags;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        private static void ExtractReleaseLevel(
            ref Version fileVersion,          /* in, out */
            out Tcl_ReleaseLevel releaseLevel /* out */
            )
        {
            if (fileVersion != null)
            {
                //
                // HACK: Since we know that (at least on Windows) the
                //       build portion of the version is actually the
                //       release level, extract it now and rebuild the
                //       file version to exclude it.
                //
                releaseLevel = (Tcl_ReleaseLevel)fileVersion.Build;

                fileVersion = new Version(fileVersion.Major,
                    fileVersion.Minor, fileVersion.Revision);
            }
            else
            {
                releaseLevel = Tcl_ReleaseLevel.TCL_UNKNOWN_RELEASE;
            }
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        private static ReturnCode ExtractBuild(
            Interpreter interpreter, /* in */
            FindFlags findFlags,     /* in */
            FindFlags allFindFlags,  /* in */
            LoadFlags loadFlags,     /* in */
            object findData,         /* in */
            string path,             /* in */
            Version unknown,         /* in */
            Priority priority,       /* in */
            Sequence sequence,       /* in */
            ref TclBuild build,      /* out */
            ref ResultList errors    /* out */
            )
        {
            bool verbose = FlagOps.HasFlags(
                allFindFlags, FindFlags.VerboseExtractBuild, true);

            if (CheckTclLibraryPath(path))
            {
                try
                {
                    string fileName = Path.GetFileName(path);

                    if (CheckTclLibraryPath(fileName))
                    {
                        ushort magic = FileOps.IMAGE_NT_OPTIONAL_BAD_MAGIC;

#if WINDOWS
                        Result error = null;

                        //
                        // NOTE: Architecture extraction and matching support
                        //       (for PE files) really only works on Windows
                        //       for now.
                        //
                        if (!FlagOps.HasFlags(
                                allFindFlags, FindFlags.GetArchitecture, true) ||
                            FileOps.CheckPeFileArchitecture(
                                path, allFindFlags, ref magic, ref error))
#endif
                        {
                            Version fileVersion = GetFileVersion(path);
                            Tcl_ReleaseLevel releaseLevel;

                            ExtractReleaseLevel(ref fileVersion, out releaseLevel);

                            Match match = null;
                            OperatingSystemId operatingSystemId = OperatingSystemId.Unknown;

                            bool extra = FlagOps.HasFlags(allFindFlags,
                                FindFlags.ExtraVersionPatternList, true);

                            bool primary = FlagOps.HasFlags(allFindFlags,
                                FindFlags.PrimaryVersionPatternList, true);

                            lock (syncRoot) /* TRANSACTIONAL */
                            {
                                foreach (RegExEnumDictionary dictionary in
                                    new RegExEnumDictionary[] {
                                        extra ? extraVersionRegExDictionary : null,
                                        primary ? primaryVersionRegExDictionary : null
                                    })
                                {
                                    if (dictionary == null)
                                        continue;

                                    foreach (KeyValuePair<Regex, Enum> pair in dictionary)
                                    {
                                        match = pair.Key.Match(fileName);

                                        if ((match != null) && match.Success)
                                        {
                                            if (pair.Value is OperatingSystemId)
                                                operatingSystemId = (OperatingSystemId)pair.Value;

                                            break;
                                        }
                                    }
                                }
                            }

                            //
                            // NOTE: Make sure the operating systems match.
                            //
                            OperatingSystemId guessOperatingSystemId =
                                PlatformOps.GuessOperatingSystemId();

                            if (FlagOps.HasFlags(
                                    allFindFlags, FindFlags.NoOperatingSystem, true) ||
                                (operatingSystemId == guessOperatingSystemId))
                            {
                                if ((match != null) && match.Success)
                                {
                                    //
                                    // NOTE: Extract the version match value.
                                    //
                                    string matchValue = RegExOps.GetMatchValue(match, 1);

                                    //
                                    // NOTE: Does it actually contain something?
                                    //
                                    if (!String.IsNullOrEmpty(matchValue))
                                    {
                                        //
                                        // NOTE: Create a list of version components based on
                                        //       the characters of the matched string.
                                        //
                                        StringList list = new StringList();

                                        if (matchValue.IndexOf(VersionSeparator) != Index.Invalid)
                                        {
                                            //
                                            // NOTE: Just split the version string using the
                                            //       separator and use each component verbatim.
                                            //
                                            list.Add(matchValue.Split(VersionSeparator));
                                        }
                                        else
                                        {
                                            //
                                            // HACK: Here, we assume version string conforms to
                                            //       the format "X[Y[ZZ]]", where "X" is the
                                            //       major version and "Y" is the minor version
                                            //       (i.e. which are assumed to always be one
                                            //       digit) and "ZZ" is the revision, which may
                                            //       be any number of digits.
                                            //
                                            list.Add(matchValue[0].ToString());

                                            if (matchValue.Length >= 2)
                                                list.Add(matchValue[1].ToString());

                                            if (matchValue.Length >= 3)
                                                list.Add(matchValue.Substring(2));
                                        }

                                        //
                                        // NOTE: If we are supposed to zero fill missing version
                                        //       components, do so now.
                                        //
                                        if (FlagOps.HasFlags(
                                                allFindFlags, FindFlags.ZeroComponents, true))
                                        {
                                            //
                                            // NOTE: Keep going until there are four components
                                            //       for this version.
                                            //
                                            while (list.Count < 4)
                                                list.Add(Characters.Zero.ToString());
                                        }

                                        //
                                        // NOTE: Join the characters with the version separator
                                        //       character (e.g. '.') and create the Version
                                        //       object to use.
                                        //
                                        Version patchLevel = new Version(list.ToString(
                                            VersionSeparator.ToString(), null, false));

                                        //
                                        // NOTE: Figure out if this looks like a threaded build.
                                        //
                                        bool threaded = RegExOps.GetMatchSuccess(match, 2);

                                        //
                                        // NOTE: Figure out if this looks like a debug build.
                                        //
                                        bool debug = RegExOps.GetMatchSuccess(match, 3);

                                        //
                                        // NOTE: Give them their resulting "Build" object.
                                        //
                                        if (RuntimeOps.IsFileTrusted(
                                                interpreter, null, path, IntPtr.Zero))
                                        {
                                            findFlags |= FindFlags.Trusted;
                                        }

                                        //
                                        // HACK: Maybe we should be using the file version
                                        //       information instead of the parsed one?
                                        //
                                        patchLevel = GlobalState.GetMoreSpecificVersion(
                                            patchLevel, fileVersion, false, false, false,
                                            false);

                                        if (Object.ReferenceEquals(patchLevel, fileVersion))
                                            findFlags |= FindFlags.FileVersion;

                                        build = new TclBuild(
                                            findFlags, loadFlags, findData, path, priority,
                                            sequence, operatingSystemId, patchLevel,
                                            releaseLevel, magic, threaded, debug);

                                        //
                                        // NOTE: Everything was parsed Ok.
                                        //
                                        return ReturnCode.Ok;
                                    }
                                    else if (verbose)
                                    {
                                        MaybeAddAnError(ref errors, String.Format(
                                            "invalid Tcl version extracted from path {0}",
                                            FormatOps.DisplayName(path)));
                                    }
                                }
                                else if (unknown != null)
                                {
                                    //
                                    // NOTE: Give them a default "Build" object.
                                    //
                                    if (RuntimeOps.IsFileTrusted(
                                            interpreter, null, path, IntPtr.Zero))
                                    {
                                        findFlags |= FindFlags.Trusted;
                                    }

                                    build = new TclBuild(
                                        findFlags, loadFlags, findData, path, priority,
                                        sequence, operatingSystemId, unknown,
                                        releaseLevel, magic, false, false);

                                    return ReturnCode.Ok;
                                }
                                else if (verbose)
                                {
                                    MaybeAddAnError(ref errors, String.Format(
                                        "could not extract Tcl version from path {0}",
                                        FormatOps.DisplayName(path)));
                                }
                            }
                            else
                            {
                                MaybeAddAnError(ref errors, String.Format(
                                    "file {0} is not for this operating system " +
                                    "(identifier mismatch, got {1}, wanted {2}).",
                                    FormatOps.DisplayName(path),
                                    FormatOps.WrapOrNull(operatingSystemId),
                                    FormatOps.WrapOrNull(guessOperatingSystemId)));
                            }
                        }
#if WINDOWS
                        else if (verbose)
                        {
                            MaybeAddAnError(ref errors, error);
                        }
#endif
                    }
                    else if (verbose)
                    {
                        MaybeAddAnError(ref errors, String.Format(
                            "no file name in path {0} to extract Tcl version from",
                            FormatOps.DisplayName(path)));
                    }
                }
                catch (Exception e)
                {
                    if (verbose)
                        MaybeAddAnError(ref errors, e);
                }
            }
            else if (verbose)
            {
                MaybeAddAnError(ref errors, "invalid or empty path");
            }

            return ReturnCode.Error;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

#if UNIX
        private static string GetProcessorPath(
            string path /* in */
            )
        {
            string result = path;

            if (String.IsNullOrEmpty(result))
                return result;

            string processorName = PlatformOps.GetProcessorName();

            if (processorName != null)
                result = PathOps.CombinePath(null, result, processorName);

            return result;
        }
#endif

        ///////////////////////////////////////////////////////////////////////////////////////////////

        private static string GetAlternateProcessorPath(
            string path /* in */
            )
        {
            string result = path;

            if (String.IsNullOrEmpty(result))
                return result;

            string processorName = PlatformOps.GetAlternateProcessorName(
                PlatformOps.QueryProcessorArchitecture(), IfNotFoundType.Null);

            if (processorName != null)
                result = PathOps.CombinePath(null, result, processorName);

            return result;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        private static string GetExternalsPath(
            bool architecture /* in */
            )
        {
            string result = GlobalState.GetExternalsPath();

            if (String.IsNullOrEmpty(result))
                return result;

            result = PathOps.CombinePath(
                null, result, TclVars.Package.Name, TclVars.Path.Lib);

            if (!architecture)
                return result;

            return GetAlternateProcessorPath(result);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        private static string GetPeerPath(
            bool architecture /* in */
            )
        {
            //
            // NOTE: For the purposes of this class, the "peer path" is the
            //       directory named "Tcl" that is a peer of the Eagle base
            //       directory (normally named "Eagle").
            //
            string result = GlobalState.GetBasePath();

            if (String.IsNullOrEmpty(result))
                return result;

            result = Path.GetDirectoryName(result);

            if (String.IsNullOrEmpty(result))
                return result;

            result = PathOps.CombinePath(
                null, result, TclVars.Package.Name, TclVars.Path.Bin);

            if (!architecture)
                return result;

            return GetAlternateProcessorPath(result);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        private static string[] GetFileNames(
            string path,          /* in */
            FindFlags flags,      /* in */
            FindFlags allFlags,   /* in */
            ref ResultList errors /* out */
            )
        {
            //
            // NOTE: Figure out the list of files to match against, this
            //       may include files from all the directories beneath
            //       the specified one (including itself) or just the
            //       specified one, depending on the FindFlags.
            //
            bool verbose = FlagOps.HasFlags(
                allFlags, FindFlags.VerbosePath, true);

            if (!FlagOps.HasFlags(allFlags, FindFlags.RecursivePaths, true))
            {
                if (verbose)
                {
                    MaybeAddAnError(ref errors, String.Format(
                        "checking files for {0} in location {1} non-recursively...",
                        FormatOps.WrapOrNull(flags), FormatOps.DisplayName(path)));
                }

                return Directory.GetFiles(path);
            }

            if (verbose)
            {
                MaybeAddAnError(ref errors, String.Format(
                    "checking files for {0} in location {1} recursively...",
                    FormatOps.WrapOrNull(flags), FormatOps.DisplayName(path)));
            }

            string searchPattern = Characters.Asterisk.ToString();

            return Directory.GetFiles(
                path, searchPattern, FileOps.GetSearchOption(true));
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        private static ReturnCode FindViaAssembly(
            Interpreter interpreter,       /* in */
            FindFlags flags,               /* in */
            FindFlags allFlags,            /* in */
            Assembly assembly,             /* in */
            Version unknown,               /* in */
            IClientData clientData,        /* in */
            ref TclBuildDictionary builds, /* out */
            ref ResultList errors          /* out */
            )
        {
            try
            {
                if (assembly != null)
                {
                    int count = 0;

                    //
                    // NOTE: Use the location of the specified assembly.  The use of
                    //       GetExecutingAssembly or GetEntryAssembly by the caller
                    //       will be the most likely suspects.
                    //
                    string directory = assembly.Location;

                    if (CheckTclLibraryDirectory(directory))
                    {
                        string[] fileNames = GetFileNames(
                            directory, flags, allFlags, ref errors);

                        if (fileNames != null)
                        {
                            foreach (string fileName in fileNames)
                            {
                                if (CheckTclLibraryFile(interpreter, fileName))
                                {
                                    Priority priority = Priority.None;

                                    if (LooksLikeTclLibrary(
                                            fileName, allFlags,
                                            ref priority, ref errors))
                                    {
                                        TclBuild build = null;

                                        if (ExtractBuild(interpreter,
                                                flags, allFlags, LoadFlags.None,
                                                assembly, fileName, unknown,
                                                priority, GetSequence(builds),
                                                ref build, ref errors) == ReturnCode.Ok)
                                        {
                                            if (builds == null)
                                                builds = new TclBuildDictionary();

                                            Result error = null;

                                            if (builds.MaybeAddOrReplace(
                                                    interpreter, allFlags, fileName,
                                                    build, ref error))
                                            {
                                                count++;
                                            }
                                            else
                                            {
                                                MaybeAddAnError(ref errors, error);
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }

                    //
                    // NOTE: Did we find any Tcl builds?
                    //
                    if (count > 0)
                    {
                        return ReturnCode.Ok;
                    }
                    else
                    {
                        MaybeAddAnError(ref errors, String.Format(
                            "no Tcl library files found via assembly location {0}",
                            FormatOps.DisplayName(directory)));
                    }
                }
                else
                {
                    MaybeAddAnError(ref errors, "invalid assembly");
                }
            }
            catch (Exception e)
            {
                MaybeAddAnError(ref errors, e);
            }

            return ReturnCode.Error;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        private static ReturnCode FindViaEnvironment(
            Interpreter interpreter,       /* in */
            FindFlags flags,               /* in */
            FindFlags allFlags,            /* in */
            Version unknown,               /* in */
            IClientData clientData,        /* in */
            ref TclBuildDictionary builds, /* out */
            ref ResultList errors          /* out */
            )
        {
            try
            {
                StringList list = new StringList(
                    EnvVars.EagleTclDll, EnvVars.EagleTkDll,
                    EnvVars.TclDll, EnvVars.TkDll
                );

                int count = 0;

                foreach (StringPair pair in PathOps.GetPathList(list))
                {
                    if (pair == null)
                        continue;

                    string path = pair.Y;

                    if (CheckTclLibraryFile(interpreter, path))
                    {
                        Priority priority = Priority.None;

                        if (LooksLikeTclLibrary(
                                path, allFlags, ref priority,
                                ref errors))
                        {
                            TclBuild build = null;

                            if (ExtractBuild(interpreter,
                                    flags, allFlags, LoadFlags.None,
                                    pair.X, path, unknown, priority,
                                    GetSequence(builds), ref build,
                                    ref errors) == ReturnCode.Ok)
                            {
                                if (builds == null)
                                    builds = new TclBuildDictionary();

                                Result error = null;

                                if (builds.MaybeAddOrReplace(
                                        interpreter, allFlags, path,
                                        build, ref error))
                                {
                                    count++;
                                }
                                else
                                {
                                    MaybeAddAnError(
                                        ref errors, error);
                                }
                            }
                        }
                    }
                    else if (CheckTclLibraryDirectory(path))
                    {
                        string[] fileNames = GetFileNames(
                            path, flags, allFlags, ref errors);

                        if (fileNames != null)
                        {
                            foreach (string fileName in fileNames)
                            {
                                Priority priority = Priority.None;

                                if (LooksLikeTclLibrary(
                                        fileName, allFlags, ref priority, ref errors))
                                {
                                    TclBuild build = null;

                                    if (ExtractBuild(interpreter,
                                            flags, allFlags, LoadFlags.None,
                                            pair.X, fileName, unknown,
                                            priority, GetSequence(builds),
                                            ref build, ref errors) == ReturnCode.Ok)
                                    {
                                        if (builds == null)
                                            builds = new TclBuildDictionary();

                                        Result error = null;

                                        if (builds.MaybeAddOrReplace(
                                                interpreter, allFlags, fileName,
                                                build, ref error))
                                        {
                                            count++;
                                        }
                                        else
                                        {
                                            MaybeAddAnError(ref errors, error);
                                        }
                                    }
                                }
                            }
                        }
                    }
                }

                if (count > 0)
                {
                    return ReturnCode.Ok;
                }
                else
                {
                    MaybeAddAnError(ref errors, String.Format(
                        "no Tcl library files found via environment variables {0}",
                        GenericOps<string>.ListToEnglish(
                            list, ", ", Characters.Space.ToString(),
                            "or ", Characters.QuotationMark.ToString(),
                            Characters.QuotationMark.ToString())));
                }
            }
            catch (Exception e)
            {
                MaybeAddAnError(ref errors, e);
            }

            return ReturnCode.Error;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        private static ReturnCode FindViaSearchPath(
            Interpreter interpreter,       /* in */
            FindFlags flags,               /* in */
            FindFlags allFlags,            /* in */
            Version unknown,               /* in */
            IClientData clientData,        /* in */
            ref TclBuildDictionary builds, /* out */
            ref ResultList errors          /* out */
            )
        {
            try
            {
                StringList list = new StringList(
                    EnvVars.LdLibraryPath, EnvVars.Path
                );

                int count = 0;

                foreach (StringPair pair in PathOps.GetPathList(list))
                {
                    if (pair == null)
                        continue;

                    string path = pair.Y;

                    if (CheckTclLibraryDirectory(path))
                    {
                        //
                        // NOTE: Get a list of files in the directory and match
                        //       them against our regular expression to determine
                        //       if they are candidate Tcl library files.
                        //
                        string[] fileNames = GetFileNames(
                            path, flags, allFlags, ref errors);

                        if (fileNames != null)
                        {
                            foreach (string fileName in fileNames)
                            {
                                Priority priority = Priority.None;

                                if (LooksLikeTclLibrary(
                                        fileName, allFlags, ref priority, ref errors))
                                {
                                    TclBuild build = null;

                                    if (ExtractBuild(interpreter,
                                            flags, allFlags, LoadFlags.None,
                                            pair.X, fileName, unknown,
                                            priority, GetSequence(builds),
                                            ref build, ref errors) == ReturnCode.Ok)
                                    {
                                        if (builds == null)
                                            builds = new TclBuildDictionary();

                                        Result error = null;

                                        if (builds.MaybeAddOrReplace(
                                                interpreter, allFlags, fileName,
                                                build, ref error))
                                        {
                                            count++;
                                        }
                                        else
                                        {
                                            MaybeAddAnError(ref errors, error);
                                        }
                                    }
                                }
                            }
                        }
                    }
                }

                if (count > 0)
                {
                    return ReturnCode.Ok;
                }
                else
                {
                    MaybeAddAnError(ref errors, String.Format(
                        "no Tcl library files found via search path using {0}",
                        GenericOps<string>.ListToEnglish(
                            list, ", ", Characters.Space.ToString(),
                            "or ", Characters.QuotationMark.ToString(),
                            Characters.QuotationMark.ToString())));
                }
            }
            catch (Exception e)
            {
                MaybeAddAnError(ref errors, e);
            }

            return ReturnCode.Error;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

#if !NET_STANDARD_20
        private static ReturnCode FindViaRegistry(
            Interpreter interpreter,       /* in */
            FindFlags flags,               /* in */
            FindFlags allFlags,            /* in */
            RegistryKey rootKey,           /* in */
            string keyName,                /* in */
            Version unknown,               /* in */
            IClientData clientData,        /* in */
            ref TclBuildDictionary builds, /* out */
            ref ResultList errors          /* out */
            )
        {
            bool verbose = FlagOps.HasFlags(
                allFlags, FindFlags.VerboseRegistry, true);

            try
            {
                //
                // NOTE: For now, primarily search within the per-user and
                //       per-machine registry hives.
                //
                RegistryKey[] rootKeys = new RegistryKey[] {
                    rootKey,
                    Registry.CurrentUser,
                    Registry.LocalMachine
                };

                //
                // NOTE: For now, primarily search for all the ActiveState
                //       Tcl installations.
                //
                StringList keyNames = new StringList(
                    keyName,
                    ActiveTclKeyPath
                );

                int count = 0;

                foreach (RegistryKey thisRootKey in rootKeys)
                {
                    if (thisRootKey == null)
                        continue;

                    foreach (string thisKeyName in keyNames)
                    {
                        if (thisKeyName == null)
                            continue;

                        using (RegistryKey key = thisRootKey.OpenSubKey(
                                thisKeyName))
                        {
                            if (key == null)
                            {
                                if (verbose)
                                {
                                    MaybeAddAnError(ref errors, String.Format(
                                        "could not open registry key {0}",
                                        FormatOps.RegistrySubKey(
                                            thisRootKey, thisKeyName)));
                                }

                                continue;
                            }

                            foreach (string subKeyName in key.GetSubKeyNames())
                            {
                                using (RegistryKey subKey = key.OpenSubKey(
                                        subKeyName))
                                {
                                    if (subKey == null)
                                    {
                                        if (verbose)
                                        {
                                            MaybeAddAnError(ref errors, String.Format(
                                                "could not open sub-key {0} of registry key {1}",
                                                FormatOps.DisplayName(subKeyName),
                                                FormatOps.WrapOrNull(key)));
                                        }

                                        continue;
                                    }

                                    //
                                    // NOTE: Grab the "default value" for this registry
                                    //       sub-key.  This must be a string.
                                    //
                                    string directory = subKey.GetValue(null) as string;

                                    //
                                    // NOTE: If the value is not an existing directory,
                                    //       skip it.
                                    //
                                    if (!CheckTclLibraryDirectory(directory))
                                        continue;

                                    //
                                    // NOTE: If the "bin" sub-directory does not exist,
                                    //       skip it.
                                    //
                                    string binDirectory = PathOps.CombinePath(null,
                                        directory, TclVars.Path.Bin);

                                    if (!CheckTclLibraryDirectory(binDirectory))
                                        continue;

                                    //
                                    // NOTE: Get a list of files in the directory and
                                    //       match them against our regular expression
                                    //       to determine if they are candidate Tcl
                                    //       library files.
                                    //
                                    string[] fileNames = GetFileNames(
                                        binDirectory, flags, allFlags, ref errors);

                                    if (fileNames != null)
                                    {
                                        foreach (string fileName in fileNames)
                                        {
                                            Priority priority = Priority.None;

                                            if (LooksLikeTclLibrary(
                                                    fileName, allFlags, ref priority,
                                                    ref errors))
                                            {
                                                TclBuild build = null;

                                                if (ExtractBuild(interpreter,
                                                        flags, allFlags, LoadFlags.None,
                                                        new object[] {
                                                        thisRootKey, thisKeyName, subKeyName
                                                    }, fileName, unknown, priority,
                                                        GetSequence(builds), ref build,
                                                        ref errors) == ReturnCode.Ok)
                                                {
                                                    if (builds == null)
                                                        builds = new TclBuildDictionary();

                                                    Result error = null;

                                                    if (builds.MaybeAddOrReplace(
                                                            interpreter, allFlags, fileName,
                                                            build, ref error))
                                                    {
                                                        count++;
                                                    }
                                                    else
                                                    {
                                                        MaybeAddAnError(ref errors, error);
                                                    }
                                                }
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }
                }

                if (count > 0)
                {
                    return ReturnCode.Ok;
                }
                else
                {
                    MaybeAddAnError(ref errors, String.Format(
                        "no Tcl library files found via registry using {0} and {1}",
                        GenericOps<RegistryKey>.ListToEnglish(
                            rootKeys, ", ", Characters.Space.ToString(),
                            "or ", Characters.QuotationMark.ToString(),
                            Characters.QuotationMark.ToString()),
                        GenericOps<string>.ListToEnglish(
                            keyNames, ", ", Characters.Space.ToString(),
                            "or ", Characters.QuotationMark.ToString(),
                            Characters.QuotationMark.ToString())));
                }
            }
            catch (Exception e)
            {
                MaybeAddAnError(ref errors, e);
            }

            return ReturnCode.Error;
        }
#endif

        ///////////////////////////////////////////////////////////////////////////////////////////////

        private static RegExList GetVersionRegExList(
            FindFlags flags /* in */
            )
        {
            RegExList result = null;

            bool extra = FlagOps.HasFlags(flags,
                FindFlags.ExtraVersionPatternList, true);

            bool primary = FlagOps.HasFlags(flags,
                FindFlags.PrimaryVersionPatternList, true);

            lock (syncRoot) /* TRANSACTIONAL */
            {
                if (extra && (extraVersionRegExDictionary != null))
                {
                    if (result == null)
                        result = new RegExList();

                    result.Add(extraVersionRegExDictionary);
                }

                if (primary && (primaryVersionRegExDictionary != null))
                {
                    if (result == null)
                        result = new RegExList();

                    result.Add(primaryVersionRegExDictionary);
                }
            }

            return result;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        private static ReturnCode FindViaPath(
            Interpreter interpreter,       /* in */
            FindFlags flags,               /* in */
            FindFlags allFlags,            /* in */
            string path,                   /* in */
            Version unknown,               /* in */
            IClientData clientData,        /* in */
            ref TclBuildDictionary builds, /* out */
            ref ResultList errors          /* out */
            )
        {
            try
            {
                //
                // NOTE: Attempt to interpret the path as a file name.  If it is a file
                //       name use it verbatim and do not attempt to find any other Tcl
                //       library files.
                //
                if (CheckTclLibraryFile(interpreter, path))
                {
                    TclBuild build = null;

                    if (ExtractBuild(interpreter,
                            flags, allFlags, LoadFlags.None,
                            path, path, unknown, Priority.Highest,
                            GetSequence(builds), ref build,
                            ref errors) == ReturnCode.Ok)
                    {
                        if (builds == null)
                            builds = new TclBuildDictionary();

                        Result error = null;

                        if (builds.MaybeAddOrReplace(
                                interpreter, allFlags, path,
                                build, ref error))
                        {
                            return ReturnCode.Ok;
                        }
                        else
                        {
                            MaybeAddAnError(ref errors, error);
                        }
                    }

                    return ReturnCode.Error;
                }
                //
                // NOTE: Next, attempt to interpret the path as a directory name.
                //
                else if (CheckTclLibraryDirectory(path))
                {
                    //
                    // NOTE: Get a list of files in the directory and match
                    //       them against our regular expression to determine
                    //       if they are candidate Tcl library files.
                    //
                    int count = 0;

                    string[] fileNames = GetFileNames(
                        path, flags, allFlags, ref errors);

                    if (fileNames != null)
                    {
                        foreach (string fileName in fileNames)
                        {
                            Priority priority = Priority.None;

                            if (LooksLikeTclLibrary(
                                    fileName, allFlags, ref priority, ref errors))
                            {
                                TclBuild build = null;

                                if (ExtractBuild(interpreter,
                                        flags, allFlags, LoadFlags.None,
                                        path, fileName, unknown, priority,
                                        GetSequence(builds), ref build,
                                        ref errors) == ReturnCode.Ok)
                                {
                                    if (builds == null)
                                        builds = new TclBuildDictionary();

                                    Result error = null;

                                    if (builds.MaybeAddOrReplace(
                                            interpreter, allFlags, fileName,
                                            build, ref error))
                                    {
                                        count++;
                                    }
                                    else
                                    {
                                        MaybeAddAnError(ref errors, error);
                                    }
                                }
                            }
                        }
                    }

                    if (count > 0)
                    {
                        return ReturnCode.Ok;
                    }
                    else
                    {
                        MaybeAddAnError(ref errors, String.Format(
                            "no Tcl library files matching {0} found in directory {1}",
                            FormatOps.DisplayString(GenericOps<Regex>.ListToEnglish(
                                GetVersionRegExList(allFlags),
                                ", ", Characters.Space.ToString(),
                                "or ", Characters.QuotationMark.ToString(),
                                Characters.QuotationMark.ToString())),
                                FormatOps.DisplayName(path)));
                    }
                }
                else
                {
                    MaybeAddAnError(ref errors, String.Format(
                        "no such file or directory {0}",
                        FormatOps.DisplayName(path)));
                }
            }
            catch (Exception e)
            {
                MaybeAddAnError(ref errors, e);
            }

            return ReturnCode.Error;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        private static ReturnCode FindViaEvaluateScript(
            Interpreter interpreter,       /* in */
            string text,                   /* in: OPTIONAL */
            FindFlags flags,               /* in */
            FindFlags allFlags,            /* in */
            Version unknown,               /* in */
            IClientData clientData,        /* in */
            bool refresh,                  /* in */
            ref TclBuildDictionary builds, /* out */
            ref ResultList errors          /* out */
            )
        {
            try
            {
                int errorCount; /* REUSED */
                bool updateCache = false;

                if (text == null)
                {
                    if (refresh)
                        cachedFindViaEvaluateScriptResult = null;

                    string path = cachedFindViaEvaluateScriptResult;

                    if (path != null)
                    {
                        errorCount = 0;

                        if (FlagOps.HasFlags(
                                allFlags, FindFlags.FindArchitecture, true))
                        {
                            if (FindViaPath(interpreter,
                                    flags | FindFlags.Part0, allFlags,
                                    GetAlternateProcessorPath(path),
                                    unknown, clientData, ref builds,
                                    ref errors) != ReturnCode.Ok)
                            {
                                errorCount++;

                                MaybeAddAnError(ref errors,
                                    "find Tcl library builds via cached script evaluation processor path failed");
                            }
                        }

                        if (FindViaPath(interpreter,
                                flags | FindFlags.Part1, allFlags,
                                path, unknown, clientData,
                                ref builds, ref errors) != ReturnCode.Ok)
                        {
                            errorCount++;

                            MaybeAddAnError(ref errors,
                                "find Tcl library builds via cached script evaluation path failed");
                        }

                        return (errorCount > 0) ?
                            ReturnCode.Error : ReturnCode.Ok;
                    }

                    text = DefaultFindViaEvaluateScript;

                    if (text == null)
                    {
                        MaybeAddAnError(ref errors,
                            "find Tcl library builds via script evaluation failed: unavailable");

                        return ReturnCode.Error;
                    }

                    updateCache = true;
                }

                Result result = null;

                if (interpreter.EvaluateScript(
                        text, ref result) == ReturnCode.Ok)
                {
                    errorCount = 0;

                    if (FlagOps.HasFlags(
                            allFlags, FindFlags.FindArchitecture, true))
                    {
                        if (FindViaPath(interpreter,
                                flags | FindFlags.Part2, allFlags,
                                GetAlternateProcessorPath(result),
                                unknown, clientData, ref builds,
                                ref errors) != ReturnCode.Ok)
                        {
                            errorCount++;

                            MaybeAddAnError(ref errors,
                                "find Tcl library builds via script evaluation processor path failed");
                        }
                    }

                    if (FindViaPath(interpreter,
                            flags | FindFlags.Part3, allFlags,
                            result, unknown, clientData,
                            ref builds, ref errors) != ReturnCode.Ok)
                    {
                        errorCount++;

                        MaybeAddAnError(ref errors,
                            "find Tcl library builds via script evaluation path failed");
                    }

                    if (updateCache)
                        cachedFindViaEvaluateScriptResult = result;

                    return (errorCount > 0) ?
                        ReturnCode.Error : ReturnCode.Ok;
                }
                else
                {
                    MaybeAddAnError(ref errors, result);
                }
            }
            catch (Exception e)
            {
                MaybeAddAnError(ref errors, e);
            }

            return ReturnCode.Error;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        private static RegExList GetNameRegExList(
            FindFlags flags /* in */
            )
        {
            RegExList result = null;

            bool extra = FlagOps.HasFlags(flags,
                FindFlags.ExtraNamePatternList, true);

            bool primary = FlagOps.HasFlags(flags,
                FindFlags.PrimaryNamePatternList, true);

            bool secondary = FlagOps.HasFlags(flags,
                FindFlags.SecondaryNamePatternList, true);

            bool other = FlagOps.HasFlags(flags,
                FindFlags.OtherNamePatternList, true);

            lock (syncRoot) /* TRANSACTIONAL */
            {
                if (extra && (extraNameRegExList != null))
                {
                    if (result == null)
                        result = new RegExList();

                    result.Add(extraNameRegExList);
                }

                if (primary && (primaryNameRegExList != null))
                {
                    if (result == null)
                        result = new RegExList();

                    result.Add(primaryNameRegExList);
                }

                if (secondary && (secondaryNameRegExList != null))
                {
                    if (result == null)
                        result = new RegExList();

                    result.Add(secondaryNameRegExList);
                }

                if (other && (otherNameRegExList != null))
                {
                    if (result == null)
                        result = new RegExList();

                    result.Add(otherNameRegExList);
                }
            }

            return result;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        private static bool LooksLikeTclLibrary(
            string path,           /* in */
            FindFlags flags,       /* in */
            ref Priority priority, /* out */
            ref ResultList errors  /* out */
            )
        {
            bool verbose = FlagOps.HasFlags(
                flags, FindFlags.VerboseLooksLike, true);

            try
            {
                if (CheckTclLibraryPath(path))
                {
                    string fileName = Path.GetFileName(path);

                    if (CheckTclLibraryPath(fileName))
                    {
                        bool extra = FlagOps.HasFlags(flags,
                            FindFlags.ExtraNamePatternList, true);

                        bool primary = FlagOps.HasFlags(flags,
                            FindFlags.PrimaryNamePatternList, true);

                        bool secondary = FlagOps.HasFlags(flags,
                            FindFlags.SecondaryNamePatternList, true);

                        bool other = FlagOps.HasFlags(flags,
                            FindFlags.OtherNamePatternList, true);

                        lock (syncRoot) /* TRANSACTIONAL */
                        {
                            foreach (RegExList list in new RegExList[] {
                                extra ? extraNameRegExList : null,
                                primary ? primaryNameRegExList : null,
                                secondary ? secondaryNameRegExList : null,
                                other ? otherNameRegExList : null })
                            {
                                if (list == null)
                                    continue;

                                for (int index = 0; index < list.Count; index++)
                                {
                                    Regex regEx = list[index];

                                    if (regEx == null)
                                        continue;

                                    Match match = regEx.Match(fileName);

                                    if ((match != null) && match.Success)
                                    {
                                        priority = (Priority)index;
                                        return true;
                                    }
                                }
                            }
                        }

                        if (verbose)
                        {
                            MaybeAddAnError(ref errors, String.Format(
                                "file name {1} does not match {0}",
                                GenericOps<Regex>.ListToEnglish(
                                    GetNameRegExList(flags),
                                    ", ", Characters.Space.ToString(),
                                    "or ", Characters.QuotationMark.ToString(),
                                    Characters.QuotationMark.ToString()),
                                FormatOps.DisplayName(fileName)));
                        }
                    }
                    else if (verbose)
                    {
                        MaybeAddAnError(ref errors, String.Format(
                            "no file name in path {0} to check Tcl patterns against",
                            FormatOps.DisplayName(path)));
                    }
                }
                else if (verbose)
                {
                    MaybeAddAnError(ref errors, "invalid or empty path");
                }
            }
            catch (Exception e)
            {
                if (verbose)
                    MaybeAddAnError(ref errors, e);
            }

            return false;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        private static int ComparePatchLevels(
            TclBuild build1, /* in */
            TclBuild build2  /* in */
            )
        {
            Version version1 = (build1 != null) ? build1.PatchLevel : null;
            Version version2 = (build2 != null) ? build2.PatchLevel : null;

            return PackageOps.VersionCompare(version1, version2);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        private static int CompareReleaseLevels(
            TclBuild build1, /* in */
            TclBuild build2  /* in */
            )
        {
            Tcl_ReleaseLevel releaseLevel1 = (build1 != null) ?
                build1.ReleaseLevel : Tcl_ReleaseLevel.TCL_UNKNOWN_RELEASE;

            Tcl_ReleaseLevel releaseLevel2 = (build2 != null) ?
                build2.ReleaseLevel : Tcl_ReleaseLevel.TCL_UNKNOWN_RELEASE;

            return ReleaseLevelCompare(releaseLevel1, releaseLevel2);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        private static int ReleaseLevelCompare(
            Tcl_ReleaseLevel releaseLevel1, /* in */
            Tcl_ReleaseLevel releaseLevel2  /* in */
            )
        {
            if (releaseLevel1 != Tcl_ReleaseLevel.TCL_UNKNOWN_RELEASE)
            {
                if (releaseLevel2 != Tcl_ReleaseLevel.TCL_UNKNOWN_RELEASE)
                {
                    if (releaseLevel1 > releaseLevel2)
                    {
                        return 1;
                    }
                    else if (releaseLevel1 < releaseLevel2)
                    {
                        return -1;
                    }
                    else
                    {
                        return 0;
                    }
                }
                else
                {
                    return 1;
                }
            }
            else if (releaseLevel2 != Tcl_ReleaseLevel.TCL_UNKNOWN_RELEASE)
            {
                return -1;
            }
            else
            {
                return 0;
            }
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        private static int CompareTrustFlags(
            Interpreter interpreter, /* in */
            TclBuild build1,         /* in */
            TclBuild build2          /* in */
            )
        {
            bool trustFlag1 = (build1 != null) ? RuntimeOps.IsFileTrusted(
                interpreter, null, build1.FileName, IntPtr.Zero) : false;

            bool trustFlag2 = (build2 != null) ? RuntimeOps.IsFileTrusted(
                interpreter, null, build2.FileName, IntPtr.Zero) : false;

            return TrustFlagCompare(trustFlag1, trustFlag2);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        private static int TrustFlagCompare(
            bool trustFlag1, /* in */
            bool trustFlag2  /* in */
            )
        {
            if (trustFlag1)
            {
                if (trustFlag2)
                {
                    return 0;
                }
                else
                {
                    return 1;
                }
            }
            else if (trustFlag2)
            {
                return -1;
            }
            else
            {
                return 0;
            }
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        private static int ComparePriorities(
            TclBuild build1, /* in */
            TclBuild build2  /* in */
            )
        {
            Priority priority1 = (build1 != null) ?
                build1.Priority : Priority.None;

            Priority priority2 = (build2 != null) ?
                build2.Priority : Priority.None;

            return PriorityCompare(priority1, priority2);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        private static int PriorityCompare(
            Priority priority1, /* in */
            Priority priority2  /* in */
            )
        {
            if (priority1 != Priority.None)
            {
                if (priority2 != Priority.None)
                {
                    if (priority1 < priority2)
                    {
                        return 1;
                    }
                    else if (priority1 > priority2)
                    {
                        return -1;
                    }
                    else
                    {
                        return 0;
                    }
                }
                else
                {
                    return 1;
                }
            }
            else if (priority2 != Priority.None)
            {
                return -1;
            }
            else
            {
                return 0;
            }
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        private static int CompareSequences(
            TclBuild build1, /* in */
            TclBuild build2  /* in */
            )
        {
            Sequence sequence1 = (build1 != null) ?
                build1.Sequence : Sequence.None;

            Sequence sequence2 = (build2 != null) ?
                build2.Sequence : Sequence.None;

            return SequenceCompare(sequence1, sequence2);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        private static int SequenceCompare(
            Sequence sequence1, /* in */
            Sequence sequence2  /* in */
            )
        {
            if (sequence1 != Sequence.None)
            {
                if (sequence2 != Sequence.None)
                {
                    if (sequence1 < sequence2)
                    {
                        return 1;
                    }
                    else if (sequence1 > sequence2)
                    {
                        return -1;
                    }
                    else
                    {
                        return 0;
                    }
                }
                else
                {
                    return 1;
                }
            }
            else if (sequence2 != Sequence.None)
            {
                return -1;
            }
            else
            {
                return 0;
            }
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        private static Sequence GetSequence(
            TclBuildDictionary builds /* in */
            )
        {
            return (builds != null) ?
                (Sequence)(builds.Count + 1) : Sequence.First;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public static ReturnCode GetVersionRange(
            FindFlags flags,          /* in */
            Version minimumRequired,  /* in */
            Version maximumRequired,  /* in */
            int? majorIncrement,      /* in */
            int? minorIncrement,      /* in */
            int? intermediateMinimum, /* in */
            int? intermediateMaximum, /* in */
            ref Result result         /* out */
            )
        {
            if (minimumRequired == null)
                minimumRequired = GetDefaultMinimumVersion(flags);

            if (maximumRequired == null)
                maximumRequired = GetDefaultMaximumVersion(flags);

            if (majorIncrement == null)
                majorIncrement = GetDefaultMajorIncrement(flags);

            if (minorIncrement == null)
                minorIncrement = GetDefaultMinorIncrement(flags);

            if (intermediateMinimum == null)
                intermediateMinimum = GetDefaultIntermediateMinimum(flags);

            if (intermediateMaximum == null)
                intermediateMaximum = GetDefaultIntermediateMaximum(flags);

            if (minimumRequired == null)
            {
                result = "invalid minimum required version";
                return ReturnCode.Error;
            }

            if (maximumRequired == null)
            {
                result = "invalid maximum required version";
                return ReturnCode.Error;
            }

            if (PackageOps.VersionCompare(minimumRequired, maximumRequired) > 0)
            {
                result = "minimum required version cannot be greater than maximum required version";
                return ReturnCode.Error;
            }

            bool sameMajor = (minimumRequired.Major == maximumRequired.Major);
            StringList list = new StringList();

            for (int major = minimumRequired.Major;
                    major <= maximumRequired.Major;
                    major += (int)majorIncrement)
            {
                int minorMinimum;
                int minorMaximum;

                if (major == minimumRequired.Major)
                {
                    minorMinimum = minimumRequired.Minor;

                    if (sameMajor)
                        minorMaximum = maximumRequired.Minor;
                    else
                        minorMaximum = (int)intermediateMaximum;
                }
                else if (major == maximumRequired.Major)
                {
                    if (sameMajor)
                        minorMinimum = minimumRequired.Minor;
                    else
                        minorMinimum = (int)intermediateMinimum;

                    minorMaximum = maximumRequired.Minor;
                }
                else
                {
                    minorMinimum = (int)intermediateMinimum;
                    minorMaximum = (int)intermediateMaximum;
                }

                for (int minor = minorMinimum;
                        minor <= minorMaximum;
                        minor += (int)minorIncrement)
                {
                    list.Add(GlobalState.GetTwoPartVersion(
                        major, minor).ToString());
                }
            }

            result = list;
            return ReturnCode.Ok;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public static ReturnCode Find(
            Interpreter interpreter,       /* in */
            FindFlags flags,               /* in */
            Tcl_FindCallback callback,     /* in */
            IEnumerable<string> paths,     /* in */
            string text,                   /* in */
            Version minimumRequired,       /* in */
            Version maximumRequired,       /* in */
            Version unknown,               /* in */
            IClientData clientData,        /* in */
            ref TclBuildDictionary builds, /* out */
            ref ResultList errors          /* out */
            )
        {
            //
            // NOTE: Tcl library search semantics:
            //
            //        1. The argument "callback" may provide a delegate to be
            //           called before searching other locations.  If null, it
            //           is simply ignored.  If the return value is "Error", a
            //           message will be added to the list of errors.  If the
            //           return value is "Break", all other search locations
            //           are skipped.  If the return value is "Continue", all
            //           other search locations will be retried.
            //
            //        2. The argument "path" may be a directory name where Tcl
            //           libraries are located OR a fully qualified path and
            //           file name to a specific Tcl library to load.
            //
            //        3. Next, we will check the directory that contains the
            //           script currently being evaluated.
            //
            //        4. Next, we will fallback to the value of the Tcl_Dll
            //           environment variable if it is set.  If set, it may
            //           specify a file or directory name to check.
            //
            //        5. Next, we will check the directories that are present
            //           in the auto-path.
            //
            //        6. Next, we will check the various Tcl package paths for
            //           the currently executing assembly (e.g. package name
            //           path, package name root path, both with and without
            //           the "bin" sub-directory).
            //
            //        7. Next, we will check the path for the assembly
            //           containing the managed entry point for the currently
            //           running application.
            //
            //        8. Next, we will check the path for the assembly
            //           containing this class (i.e. Eagle).
            //
            //        9. Next, we will check the path for the application that
            //           started this process, native or managed.
            //
            //       10. Next, we will check the path where files imported from
            //           external projects are located.
            //
            //       11. Next, we will check the path where a Tcl "peer" would
            //           be (e.g. when Tcl and Eagle are installed in sibling
            //           directories).
            //
            //       12. Next, on Unix platforms only, we will check the paths
            //           where shared libraries are commonly installed (e.g.
            //           "/usr/local/lib" and "/usr/lib").
            //
            //       13. Next, we will check the registry for all registered
            //           ActiveTcl installations (e.g. in "HKEY_LOCAL_MACHINE\
            //           Software\ActiveState\ActiveTcl\w.x.y.z").
            //
            //       14. Next, we will search all the directories contained in
            //           the PATH environment variable.
            //
            //       15. Next, we will check the directory returned from a
            //           script (e.g. [downloadAndExtractNativeTclKitDll]).
            //
            //       16. The argument "callback" may provide a delegate to be
            //           called after searching other locations.  If null, it
            //           is simply ignored.  If the return value is "Error", a
            //           message will be added to the list of errors.  If the
            //           return value is "Break", all other search locations
            //           are skipped.  If the return value is "Continue", all
            //           other search locations will be retried.
            //
        retry:

            if (FlagOps.HasFlags(flags, FindFlags.PreCallback, true))
            {
                if (callback != null)
                {
                    ReturnCode code = callback(interpreter,
                        FindFlags.PreCallback, flags, callback, paths, minimumRequired,
                        maximumRequired, unknown, clientData, ref builds, ref errors);

                    if (code == ReturnCode.Error)
                    {
                        MaybeAddAnError(ref errors,
                            "find Tcl library builds via pre-callback failed");
                    }
                    else if (code == ReturnCode.Break)
                    {
                        goto filter;
                    }
                    else if (code == ReturnCode.Continue)
                    {
                        goto retry;
                    }
                }
            }

            if (FlagOps.HasFlags(flags, FindFlags.SpecificPath, true))
            {
                if (paths != null)
                {
                    foreach (string path in paths)
                    {
                        if (FindViaPath(interpreter,
                                FindFlags.SpecificPath | FindFlags.PartX, flags,
                                path, unknown, clientData, ref builds,
                                ref errors) != ReturnCode.Ok)
                        {
                            MaybeAddAnError(ref errors, String.Format(
                                "find Tcl library builds via specific path {0} failed",
                                FormatOps.DisplayName(path)));
                        }
                    }
                }
                else
                {
                    MaybeAddAnError(ref errors, "invalid specific paths");
                }
            }

            if (FlagOps.HasFlags(flags, FindFlags.ScriptPath, true))
            {
                string path = null;
                Result error = null;

                if ((ScriptOps.GetScriptPath(
                        interpreter, true, ref path, ref error) == ReturnCode.Ok) &&
                    (path != null))
                {
                    if (FindViaPath(interpreter,
                            FindFlags.ScriptPath, flags, path, unknown, clientData,
                            ref builds, ref errors) != ReturnCode.Ok)
                    {
                        MaybeAddAnError(ref errors, String.Format(
                            "find Tcl library builds via script path {0} failed",
                            FormatOps.DisplayName(path)));
                    }
                }
                else
                {
                    MaybeAddAnError(ref errors, String.Format(
                        "invalid script path: {0}", FormatOps.WrapOrNull(error)));
                }
            }

            if (FlagOps.HasFlags(flags, FindFlags.Environment, true))
            {
                if (FindViaEnvironment(interpreter,
                        FindFlags.Environment, flags, unknown, clientData,
                        ref builds, ref errors) != ReturnCode.Ok)
                {
                    MaybeAddAnError(ref errors,
                        "find Tcl library builds via environment failed");
                }
            }

            if (FlagOps.HasFlags(flags, FindFlags.AutoPath, true))
            {
                StringList autoPathList = GlobalState.GetAutoPathList(
                    interpreter, FlagOps.HasFlags(flags,
                    FindFlags.RefreshAutoPath, true));

                if (autoPathList != null)
                {
                    foreach (string path in autoPathList)
                    {
                        if (!CheckTclLibraryPath(path))
                            continue;

                        if (FindViaPath(interpreter,
                                FindFlags.AutoPath | FindFlags.PartX, flags,
                                path, unknown, clientData, ref builds,
                                ref errors) != ReturnCode.Ok)
                        {
                            MaybeAddAnError(ref errors, String.Format(
                                "find Tcl library builds via auto-path {0} failed",
                                FormatOps.DisplayName(path)));
                        }
                    }
                }
                else
                {
                    MaybeAddAnError(ref errors, "fetch of auto-path list failed");
                }
            }

            if (FlagOps.HasFlags(flags, FindFlags.PackageBinaryPath, true))
            {
                if (FlagOps.HasFlags(flags, FindFlags.FindArchitecture, true) &&
                    FindViaPath(interpreter,
                        FindFlags.PackageBinaryPath | FindFlags.Part0, flags,
                        GetAlternateProcessorPath(PathOps.CombinePath(
                            null, GlobalState.GetTclPackageNamePath(),
                        TclVars.Path.Bin)), unknown, clientData, ref builds,
                        ref errors) != ReturnCode.Ok)
                {
                    MaybeAddAnError(ref errors,
                        "find Tcl library builds via package name binary processor path failed");
                }

                if (FindViaPath(interpreter,
                        FindFlags.PackageBinaryPath | FindFlags.Part1, flags,
                        PathOps.CombinePath(
                            null, GlobalState.GetTclPackageNamePath(),
                        TclVars.Path.Bin), unknown, clientData, ref builds,
                        ref errors) != ReturnCode.Ok)
                {
                    MaybeAddAnError(ref errors,
                        "find Tcl library builds via package name binary path failed");
                }

                if (FlagOps.HasFlags(flags, FindFlags.FindArchitecture, true) &&
                    FindViaPath(interpreter,
                        FindFlags.PackageBinaryPath | FindFlags.Part2, flags,
                        GetAlternateProcessorPath(PathOps.CombinePath(
                            null, GlobalState.GetTclPackageNameRootPath(),
                        TclVars.Path.Bin)), unknown, clientData, ref builds,
                        ref errors) != ReturnCode.Ok)
                {
                    MaybeAddAnError(ref errors,
                        "find Tcl library builds via package name root binary processor path failed");
                }

                if (FindViaPath(interpreter,
                        FindFlags.PackageBinaryPath | FindFlags.Part3, flags,
                        PathOps.CombinePath(
                            null, GlobalState.GetTclPackageNameRootPath(),
                        TclVars.Path.Bin), unknown, clientData, ref builds,
                        ref errors) != ReturnCode.Ok)
                {
                    MaybeAddAnError(ref errors,
                        "find Tcl library builds via package name root binary path failed");
                }
            }

            if (FlagOps.HasFlags(flags, FindFlags.PackageLibraryPath, true))
            {
                if (FlagOps.HasFlags(flags, FindFlags.FindArchitecture, true) &&
                    FindViaPath(interpreter,
                        FindFlags.PackageLibraryPath | FindFlags.Part0, flags,
                        GetAlternateProcessorPath(PathOps.CombinePath(
                            null, GlobalState.GetTclPackageNamePath(),
                        TclVars.Path.Lib)), unknown, clientData, ref builds,
                        ref errors) != ReturnCode.Ok)
                {
                    MaybeAddAnError(ref errors,
                        "find Tcl library builds via package name library processor path failed");
                }

                if (FindViaPath(interpreter,
                        FindFlags.PackageLibraryPath | FindFlags.Part1, flags,
                        PathOps.CombinePath(
                            null, GlobalState.GetTclPackageNamePath(),
                        TclVars.Path.Lib), unknown, clientData, ref builds,
                        ref errors) != ReturnCode.Ok)
                {
                    MaybeAddAnError(ref errors,
                        "find Tcl library builds via package name library path failed");
                }

                if (FlagOps.HasFlags(flags, FindFlags.FindArchitecture, true) &&
                    FindViaPath(interpreter,
                        FindFlags.PackageLibraryPath | FindFlags.Part2, flags,
                        GetAlternateProcessorPath(PathOps.CombinePath(
                            null, GlobalState.GetTclPackageNameRootPath(),
                        TclVars.Path.Lib)), unknown, clientData, ref builds,
                        ref errors) != ReturnCode.Ok)
                {
                    MaybeAddAnError(ref errors,
                        "find Tcl library builds via package name root library processor path failed");
                }

                if (FindViaPath(interpreter,
                        FindFlags.PackageLibraryPath | FindFlags.Part3, flags,
                        PathOps.CombinePath(
                            null, GlobalState.GetTclPackageNameRootPath(),
                        TclVars.Path.Lib), unknown, clientData, ref builds,
                        ref errors) != ReturnCode.Ok)
                {
                    MaybeAddAnError(ref errors,
                        "find Tcl library builds via package name root library path failed");
                }
            }

            if (FlagOps.HasFlags(flags, FindFlags.PackagePath, true))
            {
                if (FlagOps.HasFlags(flags, FindFlags.FindArchitecture, true) &&
                    FindViaPath(interpreter,
                        FindFlags.PackagePath | FindFlags.Part0, flags,
                        GetAlternateProcessorPath(
                            GlobalState.GetTclPackageNamePath()),
                        unknown, clientData, ref builds, ref errors) != ReturnCode.Ok)
                {
                    MaybeAddAnError(ref errors,
                        "find Tcl library builds via package name processor path failed");
                }

                if (FindViaPath(interpreter,
                        FindFlags.PackagePath | FindFlags.Part1, flags,
                        GlobalState.GetTclPackageNamePath(), unknown,
                        clientData, ref builds, ref errors) != ReturnCode.Ok)
                {
                    MaybeAddAnError(ref errors,
                        "find Tcl library builds via package name path failed");
                }

                if (FlagOps.HasFlags(flags, FindFlags.FindArchitecture, true) &&
                    FindViaPath(interpreter,
                        FindFlags.PackagePath | FindFlags.Part2, flags,
                        GetAlternateProcessorPath(
                            GlobalState.GetTclPackageNameRootPath()),
                        unknown, clientData, ref builds, ref errors) != ReturnCode.Ok)
                {
                    MaybeAddAnError(ref errors,
                        "find Tcl library builds via package name root processor path failed");
                }

                if (FindViaPath(interpreter,
                        FindFlags.PackagePath | FindFlags.Part3, flags,
                        GlobalState.GetTclPackageNameRootPath(), unknown,
                        clientData, ref builds, ref errors) != ReturnCode.Ok)
                {
                    MaybeAddAnError(ref errors,
                        "find Tcl library builds via package name root path failed");
                }
            }

            if (FlagOps.HasFlags(flags, FindFlags.EntryAssembly, true))
            {
                if (FindViaAssembly(interpreter,
                        FindFlags.EntryAssembly, flags,
                        GlobalState.GetEntryAssembly(), unknown, clientData,
                        ref builds, ref errors) != ReturnCode.Ok)
                {
                    MaybeAddAnError(ref errors,
                        "find Tcl library builds via entry assembly location failed");
                }
            }

            if (FlagOps.HasFlags(flags, FindFlags.ExecutingAssembly, true))
            {
                if (FindViaAssembly(interpreter,
                        FindFlags.ExecutingAssembly, flags,
                        Assembly.GetExecutingAssembly(), unknown, clientData,
                        ref builds, ref errors) != ReturnCode.Ok)
                {
                    MaybeAddAnError(ref errors,
                        "find Tcl library builds via executing assembly location failed");
                }
            }

            if (FlagOps.HasFlags(flags, FindFlags.BinaryPath, true))
            {
                if (FindViaPath(interpreter,
                        FindFlags.BinaryPath, flags,
                        GlobalState.InitializeOrGetBinaryPath(false), unknown,
                        clientData, ref builds, ref errors) != ReturnCode.Ok)
                {
                    MaybeAddAnError(ref errors,
                        "find Tcl library builds via binary path failed");
                }
            }

            if (FlagOps.HasFlags(flags, FindFlags.ExternalsPath, true))
            {
                if (FlagOps.HasFlags(flags, FindFlags.FindArchitecture, true) &&
                    FindViaPath(interpreter,
                        FindFlags.ExternalsPath, flags,
                        GetExternalsPath(true), unknown, clientData,
                        ref builds, ref errors) != ReturnCode.Ok)
                {
                    MaybeAddAnError(ref errors,
                        "find Tcl library builds via externals processor path failed");
                }

                if (FindViaPath(interpreter,
                        FindFlags.ExternalsPath, flags,
                        GetExternalsPath(false), unknown, clientData,
                        ref builds, ref errors) != ReturnCode.Ok)
                {
                    MaybeAddAnError(ref errors,
                        "find Tcl library builds via externals path failed");
                }
            }

            if (FlagOps.HasFlags(flags, FindFlags.PeerPath, true))
            {
                if (FlagOps.HasFlags(flags, FindFlags.FindArchitecture, true) &&
                    FindViaPath(interpreter,
                        FindFlags.PeerPath, flags,
                        GetPeerPath(true), unknown, clientData,
                        ref builds, ref errors) != ReturnCode.Ok)
                {
                    MaybeAddAnError(ref errors,
                        "find Tcl library builds via peer processor path failed");
                }

                if (FindViaPath(interpreter,
                        FindFlags.PeerPath, flags,
                        GetPeerPath(false), unknown, clientData,
                        ref builds, ref errors) != ReturnCode.Ok)
                {
                    MaybeAddAnError(ref errors,
                        "find Tcl library builds via peer path failed");
                }
            }

#if UNIX
            if (FlagOps.HasFlags(flags, FindFlags.LocalLibraryPath, true))
            {
                if (FindViaPath(interpreter,
                        FindFlags.LocalLibraryPath, flags,
                        PathOps.GetLibPath(true, false, FlagOps.HasFlags(
                            flags, FindFlags.AlternateName, true)),
                        unknown, clientData, ref builds,
                        ref errors) != ReturnCode.Ok)
                {
                    MaybeAddAnError(ref errors,
                        "find Tcl library builds via local library processor path failed");
                }

                if (FindViaPath(interpreter,
                        FindFlags.LocalLibraryPath, flags,
                        TclVars.Path.UserLocalLib, unknown, clientData,
                        ref builds, ref errors) != ReturnCode.Ok)
                {
                    MaybeAddAnError(ref errors,
                        "find Tcl library builds via local library path failed");
                }
            }

            if (FlagOps.HasFlags(flags, FindFlags.LibraryPath, true))
            {
                if (FindViaPath(interpreter,
                        FindFlags.LibraryPath, flags,
                        PathOps.GetLibPath(false, false, FlagOps.HasFlags(
                            flags, FindFlags.AlternateName, true)),
                        unknown, clientData, ref builds,
                        ref errors) != ReturnCode.Ok)
                {
                    MaybeAddAnError(ref errors,
                        "find Tcl library builds via library processor path failed");
                }

                if (FindViaPath(interpreter,
                        FindFlags.LibraryPath, flags, TclVars.Path.UserLib,
                        unknown, clientData, ref builds, ref errors) != ReturnCode.Ok)
                {
                    MaybeAddAnError(ref errors,
                        "find Tcl library builds via library path failed");
                }
            }
#endif

#if !NET_STANDARD_20
            if (FlagOps.HasFlags(flags, FindFlags.Registry, true))
            {
                if (FindViaRegistry(interpreter,
                        FindFlags.Registry, flags, null, null, unknown, clientData,
                        ref builds, ref errors) != ReturnCode.Ok)
                {
                    MaybeAddAnError(ref errors,
                        "find Tcl library builds via registry failed");
                }
            }
#endif

            if (FlagOps.HasFlags(flags, FindFlags.SearchPath, true))
            {
                if (FindViaSearchPath(interpreter,
                        FindFlags.SearchPath, flags, unknown, clientData,
                        ref builds, ref errors) != ReturnCode.Ok)
                {
                    MaybeAddAnError(ref errors,
                        "find Tcl library builds via search path failed");
                }
            }

            if (FlagOps.HasFlags(flags, FindFlags.EvaluateScript, true))
            {
                if (FindViaEvaluateScript(
                        interpreter, text, FindFlags.EvaluateScript,
                        flags, unknown, clientData, FlagOps.HasFlags(
                        flags, FindFlags.RefreshEvaluateScript, true),
                        ref builds, ref errors) != ReturnCode.Ok)
                {
                    MaybeAddAnError(ref errors,
                        "find Tcl library builds via script evaluation failed");
                }
            }

            if (FlagOps.HasFlags(flags, FindFlags.PostCallback, true))
            {
                if (callback != null)
                {
                    ReturnCode code = callback(interpreter,
                        FindFlags.PostCallback, flags, callback, paths, minimumRequired,
                        maximumRequired, unknown, clientData, ref builds, ref errors);

                    if (code == ReturnCode.Error)
                    {
                        MaybeAddAnError(ref errors,
                            "find Tcl library builds via post-callback failed");
                    }
                    else if (code == ReturnCode.Break)
                    {
                        goto filter; // NOTE: Yes, currently redundant.
                    }
                    else if (code == ReturnCode.Continue)
                    {
                        goto retry;
                    }
                }
            }

        filter:

            //
            // NOTE: Were any builds of Tcl found at all?  If not, that is a
            //       failure.
            //
            if (builds != null)
            {
                //
                // NOTE: If requested by the caller, filter on the minimum and
                //       maximum required versions.
                //
                if ((minimumRequired != null) || (maximumRequired != null))
                {
                    StringList keys = builds.GetKeysInOrder(false);

                    if (keys != null)
                    {
                        foreach (string key in keys)
                        {
                            //
                            // NOTE: Get the build object associated with this
                            //       key (file name).
                            //
                            TclBuild build = builds[key];

                            //
                            // NOTE: Also remove invalid entries.
                            //
                            if (build != null)
                            {
                                //
                                // NOTE: Compare the version of the current
                                //       build with the minimum and/or maximum
                                //       required versions.  If the current
                                //       build does not meet the criteria,
                                //       remove it from the resulting
                                //       dictionary.
                                //
                                if (((minimumRequired == null) ||
                                        PackageOps.VersionCompare(
                                            build.PatchLevel,
                                            minimumRequired) >= 0) &&
                                    ((maximumRequired == null) ||
                                        PackageOps.VersionCompare(
                                            build.PatchLevel,
                                            maximumRequired) <= 0))
                                {
                                    //
                                    // NOTE: This build is ok, skip removing
                                    //       it.
                                    //
                                    continue;
                                }
                            }

                            builds.Remove(key);
                        }
                    }
                }

                //
                // NOTE: If any (filtered) builds of Tcl were found, indicate
                //       success to the caller.
                //
                if (builds.Count > 0)
                    return ReturnCode.Ok;
            }

            //
            // NOTE: Add a good default error message if none are present.
            //
            if (!HaveAnError(errors))
            {
                MaybeAddAnError(ref errors,
                    "find Tcl library builds failed, nothing done");
            }

            return ReturnCode.Error;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public static ReturnCode Select(
            Interpreter interpreter,   /* in */
            FindFlags flags,           /* in */
            TclBuildDictionary builds, /* in */
            Version minimumRequired,   /* in */
            Version maximumRequired,   /* in */
            ref TclBuild build,        /* out */
            ref ResultList errors      /* out */
            )
        {
            //
            // NOTE: Tcl library selection semantics:
            //
            //       1. We always attempt to select the "highest version" available.
            //          If multiple libraries have the "highest version" (i.e. not
            //          always taking into account the patch level, because some of
            //          the file names do not contain that information) the precise
            //          one selected is unspecified; however, it will be one of the
            //          libraries that shared the "highest version".
            //
            //       2. We refuse to consider any library that is not designed for
            //          the current operating system.
            //
            //       3. If the "architecture" flag is set, we refuse to consider any
            //          library that does not match the architecture for the current
            //          process.
            //
            //       4. We refuse to consider any library that does not meet the
            //          minimum required version (null means that this restriction
            //          is waived).
            //
            //       5. We refuse to consider any library that does not meet the
            //          maximum required version (null means that this restriction
            //          is waived).
            //
            //       6. We always favor libraries with a higher release level.  This
            //          allows us to prefer final releases over alphas and betas of
            //          the same base version.
            //
            //       7. We always favor libraries that are "trusted" over ones that
            //          are not (i.e. those with Authenticode signatures).
            //
            //       8. We always favor libraries with a higher relative priority
            //          (i.e. libraries that match "more important" file name
            //          patterns, where importance is determined by which pattern
            //          occurs earliest in the list).
            //
            //       9. We always favor threaded libraries (if the file name contains
            //          this information).
            //
            //      10. We always favor the DEBUG libraries if we are also compiled
            //          in DEBUG.
            //
            //      11. We always validate that we are loading a threaded build
            //          (failing if the selected library is not threaded).  This
            //          restriction is handled while attempting to load the library
            //          and not in this function because currently the library must
            //          be at least partially loaded prior to querying whether or
            //          not it is a threaded build of the Tcl library.
            //
            //      12. We always favor libraries with a lower relative sequence
            //          (i.e. libraries that were closer to the start of the logical
            //          search list).
            //
            bool verbose = FlagOps.HasFlags(
                flags, FindFlags.VerboseSelect, true);

            try
            {
                if ((builds != null) && (builds.Count > 0))
                {
                    string bestFileName = null;
                    TclBuild bestBuild = null;

                    IEnumerable<KeyValuePair<string, TclBuild>> pairs =
                        builds.GetPairsInOrder(false);

                    if (pairs != null)
                    {
                        foreach (KeyValuePair<string, TclBuild> pair in pairs)
                        {
                            //
                            // NOTE: Grab the file name and build object for diagnostic
                            //       purposes.
                            //
                            string thisFileName = pair.Key;
                            TclBuild thisBuild = pair.Value;

                            //
                            // NOTE: Skip over invalid file names and/or files that do not
                            //       actually exist.
                            //
                            if (!CheckTclLibraryFile(interpreter, thisFileName))
                            {
                                if (verbose)
                                {
                                    MaybeAddAnError(ref errors, String.Format(
                                        "skipped Tcl build {0}, rejected file name",
                                        FormatOps.DisplayTclBuild(thisBuild)));
                                }

                                continue;
                            }

                            if (thisBuild == null)
                            {
                                if (verbose)
                                {
                                    MaybeAddAnError(ref errors, String.Format(
                                        "skipped Tcl library file {0}, invalid build",
                                        FormatOps.DisplayName(thisFileName)));
                                }

                                continue;
                            }

                            //
                            // NOTE: Make sure that the build has the same operating system as
                            //       the current process.
                            //
                            OperatingSystemId guessOperatingSystemId =
                                PlatformOps.GuessOperatingSystemId();

                            if (!FlagOps.HasFlags(flags, FindFlags.NoOperatingSystem, true) &&
                                (thisBuild.OperatingSystemId != guessOperatingSystemId))
                            {
                                MaybeAddAnError(ref errors, String.Format(
                                    "skipped Tcl library file {0}, operating system {1} " +
                                    "does NOT match process operating system {2}",
                                    FormatOps.DisplayName(thisFileName),
                                    FormatOps.WrapOrNull(thisBuild.OperatingSystemId),
                                    FormatOps.WrapOrNull(guessOperatingSystemId)));

                                continue;
                            }

                            //
                            // NOTE: Make sure that the build has the same PE header magic that
                            //       this process requires.
                            //
                            ushort processMagic = FileOps.GetPeFileMagicForProcess();

                            if (FlagOps.HasFlags(flags, FindFlags.FindArchitecture, true) &&
                                (thisBuild.Magic != processMagic))
                            {
                                MaybeAddAnError(ref errors, String.Format(
                                    "skipped Tcl library file {0}, file magic {1} " +
                                    "does NOT match process magic {2}",
                                    FormatOps.DisplayName(thisFileName),
                                    FormatOps.WrapOrNull(thisBuild.Magic),
                                    FormatOps.WrapOrNull(processMagic)));

                                continue;
                            }

                            //
                            // NOTE: Compare the version of the current build with the minimum
                            //       required patch level.
                            //
                            if ((minimumRequired != null) && PackageOps.VersionCompare(
                                    thisBuild.PatchLevel, minimumRequired) < 0)
                            {
                                MaybeAddAnError(ref errors, String.Format(
                                    "skipped Tcl library file {0}, patch level {1} " +
                                    "does NOT meet minimum required version {2}",
                                    FormatOps.DisplayName(thisFileName),
                                    FormatOps.WrapOrNull(thisBuild.PatchLevel),
                                    FormatOps.WrapOrNull(minimumRequired)));

                                continue;
                            }

                            //
                            // NOTE: Compare the version of the current build with the maximum
                            //       allowed patch level.
                            //
                            if ((maximumRequired != null) && PackageOps.VersionCompare(
                                    thisBuild.PatchLevel, maximumRequired) > 0)
                            {
                                MaybeAddAnError(ref errors, String.Format(
                                    "skipped Tcl library file {0}, patch level {1} " +
                                    "does NOT meet maximum required version {2}",
                                    FormatOps.DisplayName(thisFileName),
                                    FormatOps.WrapOrNull(thisBuild.PatchLevel),
                                    FormatOps.WrapOrNull(maximumRequired)));

                                continue;
                            }

                            //
                            // NOTE: Compare the version of the current build with the patch
                            //       level of the best build we have seen so far.
                            //
                            int patchLevelResult = ComparePatchLevels(thisBuild, bestBuild);

                            if (patchLevelResult < 0)
                            {
                                if (verbose)
                                {
                                    MaybeAddAnError(ref errors, String.Format(
                                        "skipped Tcl library file {0}, patch level {1} " +
                                        "is worse than best patch level {2}",
                                        FormatOps.DisplayName(thisFileName),
                                        FormatOps.WrapOrNull(thisBuild.PatchLevel),
                                        FormatOps.WrapOrNull((bestBuild != null) ?
                                            bestBuild.PatchLevel : null)));
                                }

                                continue;
                            }

                            //
                            // NOTE: Compare the release level of the current build with the
                            //       release level of the best build we have seen so far.
                            //
                            int releaseLevelResult = CompareReleaseLevels(thisBuild, bestBuild);

                            if (releaseLevelResult < 0)
                            {
                                if (verbose)
                                {
                                    MaybeAddAnError(ref errors, String.Format(
                                        "skipped Tcl library file {0}, release level {1} " +
                                        "is worse than best release level {2}",
                                        FormatOps.DisplayName(thisFileName),
                                        FormatOps.WrapOrNull(thisBuild.ReleaseLevel),
                                        FormatOps.WrapOrNull((bestBuild != null) ?
                                            bestBuild.ReleaseLevel.ToString() : null)));
                                }

                                continue;
                            }

                            //
                            // NOTE: Compare the "trust" results of the two builds.  Always
                            //       prefer trusted builds over non-trusted ones.
                            //
                            int trustFlagResult = CompareTrustFlags(
                                interpreter, thisBuild, bestBuild);

                            if (trustFlagResult < 0)
                            {
                                if (verbose)
                                {
                                    MaybeAddAnError(ref errors, String.Format(
                                        "skipped Tcl library file {0}, it is NOT " +
                                        "more trusted than best Tcl library file {1}",
                                        FormatOps.DisplayName(thisFileName),
                                        FormatOps.DisplayName(bestFileName)));
                                }

                                continue;
                            }

                            //
                            // NOTE: Prefer to use builds that have a higher [known] relative
                            //       priority.
                            //
                            int priorityResult = ComparePriorities(thisBuild, bestBuild);

                            if (priorityResult < 0)
                            {
                                if (verbose)
                                {
                                    MaybeAddAnError(ref errors, String.Format(
                                        "skipped Tcl library file {0}, priority {1} " +
                                        "is worse than best priority {2}",
                                        FormatOps.DisplayName(thisFileName),
                                        FormatOps.WrapOrNull(thisBuild.Priority),
                                        FormatOps.WrapOrNull((bestBuild != null) ?
                                            bestBuild.Priority.ToString() : null)));
                                }

                                continue;
                            }

                            //
                            // NOTE: The version number is greater than or equal to the best
                            //       build we have seen so far.  Now, we need to make sure that
                            //       we are not abandoning a threaded build unless the current
                            //       build is also threaded.
                            //
                            bool bestThreaded = (bestBuild != null) &&
                                (bestBuild.Threaded || bestBuild.DefaultThreaded);

                            bool thisThreaded =
                                thisBuild.Threaded || thisBuild.DefaultThreaded;

                            if (bestThreaded && !thisThreaded)
                            {
                                if (verbose)
                                {
                                    MaybeAddAnError(ref errors, String.Format(
                                        "skipped Tcl library file {0}, threaded flag " +
                                        "{1} is worse than best threaded flag {2}",
                                        FormatOps.DisplayName(thisFileName),
                                        FormatOps.WrapOrNull(thisThreaded),
                                        FormatOps.WrapOrNull(bestThreaded)));
                                }

                                continue;
                            }

                            //
                            // NOTE: Do not abandon a build that matches our debugging affinity
                            //       unless it has a higher version number or relative priority
                            //       than the best build we have seen so far.
                            //
                            bool bestDebug = (bestBuild != null) && bestBuild.MatchDebug;
                            bool thisDebug = thisBuild.MatchDebug;

                            if (bestDebug &&
                                (patchLevelResult <= 0) && (priorityResult <= 0))
                            {
                                if (verbose)
                                {
                                    MaybeAddAnError(ref errors, String.Format(
                                        "skipped Tcl library file {0}, patch level " +
                                        "and priority are NOT better than best debug " +
                                        "build", FormatOps.DisplayName(thisFileName),
                                        FormatOps.WrapOrNull(thisThreaded),
                                        FormatOps.WrapOrNull(bestThreaded)));
                                }

                                continue;
                            }

                            //
                            // BUGFIX: Stick with the best build we have so far unless we
                            //         have some compelling reason not to (i.e. all other
                            //         things being equal, prefer builds that are near the
                            //         start of the logical search list).
                            //
                            if ((patchLevelResult <= 0) && (priorityResult <= 0) &&
                                (bestThreaded == thisThreaded) && (bestDebug == thisDebug))
                            {
                                int sequenceResult = CompareSequences(thisBuild, bestBuild);

                                if (sequenceResult < 0)
                                {
                                    if (verbose)
                                    {
                                        MaybeAddAnError(ref errors, String.Format(
                                            "skipped Tcl library file {0}, sequence {1} " +
                                            "is worse than best sequence {2}",
                                            FormatOps.DisplayName(thisFileName),
                                            FormatOps.WrapOrNull(thisBuild.Sequence),
                                            FormatOps.WrapOrNull((bestBuild != null) ?
                                                bestBuild.Sequence.ToString() : null)));
                                    }

                                    continue;
                                }
                            }

                            //
                            // NOTE: Either the best build has not been set yet (i.e. anything
                            //       is better than nothing) or the current build is "better"
                            //       than the best build we have seen so far (i.e. it matches
                            //       our debugging affinity better, has threading enabled, or
                            //       it has a higher version or priority).
                            //
                            if (verbose)
                            {
                                MaybeAddAnError(ref errors, String.Format(
                                    "new best Tcl library file {0}, build {1}",
                                    FormatOps.DisplayName(thisFileName),
                                    FormatOps.DisplayTclBuild(thisBuild)));
                            }

                            bestBuild = thisBuild;
                            bestFileName = thisFileName;
                        }
                    }

                    //
                    // NOTE: Did we select a build to use?
                    //
                    if (bestBuild != null)
                    {
                        build = bestBuild;

                        return ReturnCode.Ok;
                    }
                    else
                    {
                        MaybeAddAnError(ref errors, "no suitable Tcl library file found");
                    }
                }
                else
                {
                    MaybeAddAnError(ref errors, "no Tcl library builds found to select from");
                }
            }
            catch (Exception e)
            {
                MaybeAddAnError(ref errors, e);
            }

            return ReturnCode.Error;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public static ReturnCode CreateExitHandler(
            ITclApi tclApi,    /* in */
            Tcl_ExitProc proc, /* in */
            IntPtr clientData, /* in */
            ref Result error   /* out */
            )
        {
            try
            {
                if (TclApi.CheckModule(tclApi, ref error))
                {
                    Tcl_CreateExitHandler createExitHandler;

                    lock (tclApi.SyncRoot)
                    {
                        createExitHandler = tclApi.CreateExitHandler;
                    }

                    if (proc != null)
                    {
                        if (createExitHandler != null)
                        {
                            /* NO RESULT */
                            createExitHandler(proc, clientData);

                            return ReturnCode.Ok;
                        }
                        else
                        {
                            error = "Tcl exit handler creation is not available";
                        }
                    }
                    else
                    {
                        error = "invalid exit proc";
                    }
                }
            }
            catch (Exception e)
            {
                error = e;
            }

            return ReturnCode.Error;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public static ReturnCode DeleteExitHandler(
            ITclApi tclApi,    /* in */
            Tcl_ExitProc proc, /* in */
            IntPtr clientData, /* in */
            ref Result error   /* out */
            )
        {
            try
            {
                if (TclApi.CheckModule(tclApi, ref error))
                {
                    Tcl_DeleteExitHandler deleteExitHandler;

                    lock (tclApi.SyncRoot)
                    {
                        deleteExitHandler = tclApi.DeleteExitHandler;
                    }

                    if (proc != null)
                    {
                        if (deleteExitHandler != null)
                        {
                            /* NO RESULT */
                            deleteExitHandler(proc, clientData);

                            return ReturnCode.Ok;
                        }
                        else
                        {
                            error = "Tcl exit handler deletion is not available";
                        }
                    }
                    else
                    {
                        error = "invalid exit proc";
                    }
                }
            }
            catch (Exception e)
            {
                error = e;
            }

            return ReturnCode.Error;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public static ReturnCode Load(
            Interpreter interpreter,   /* in */
            FindFlags findFlags,       /* in */
            LoadFlags loadFlags,       /* in */
            Tcl_FindCallback callback, /* in */
            IEnumerable<string> paths, /* in */
            string text,               /* in */
            Version minimumRequired,   /* in */
            Version maximumRequired,   /* in */
            Version unknown,           /* in */
            IClientData clientData,    /* in */
            ref ITclApi tclApi,        /* in, out */
            ref IntPtr interp,         /* in, out */
            ref Result result          /* out */
            )
        {
            ReturnCode code;

            if (interpreter != null)
            {
                if (tclApi == null)
                {
                    if (interp == IntPtr.Zero)
                    {
                        FindFlags newFindFlags = findFlags |
                            FindFlags.Architecture;

                        TclBuildDictionary builds = null;
                        ResultList errors = null;

                        code = Find(
                            interpreter, newFindFlags, callback, paths,
                            text, minimumRequired, maximumRequired,
                            unknown, clientData, ref builds, ref errors);

                        if (code == ReturnCode.Ok)
                        {
                            TclBuild build = null;

                            code = Select(interpreter,
                                newFindFlags, builds, minimumRequired,
                                maximumRequired, ref build, ref errors);

                            if (code == ReturnCode.Ok)
                            {
                                IntPtr module = IntPtr.Zero;
#if TCL_THREADED
                                IntPtr threadedNameObjPtr = IntPtr.Zero;
#endif
                                IntPtr patchLevelNameObjPtr = IntPtr.Zero;
                                IntPtr newInterp = IntPtr.Zero;

                                try
                                {
                                    //
                                    // NOTE: *NEW* Set the load flags for the selected build to the
                                    //       ones provided by our caller.
                                    //
                                    build.LoadFlags = loadFlags;

                                    //
                                    // NOTE: Dynamically load the selected Tcl library module into
                                    //       memory.  The Tcl API object will be populated (below)
                                    //       with the delegates that wrap the actual Tcl C API based
                                    //       on this module.  This will also increment the module
                                    //       reference count.  If this is the first time we have seen
                                    //       this module, the reference count will be one; otherwise
                                    //       it will be one greater than before.
                                    //
                                    module = AddModuleReference(
                                        build.FileName, true, FlagOps.HasFlags(build.LoadFlags,
                                        LoadFlags.SetDllDirectory, true), ref result); /* throw */

                                    if (NativeOps.IsValidHandle(module))
                                    {
                                        //
                                        // NOTE: Attempt to find and setup all the Tcl API functions
                                        //       that we require.  If this step fails, we will have
                                        //       no Tcl API object; however, we must still cleanup
                                        //       the partially loaded Tcl library module.
                                        //
                                        tclApi = TclApi.Create(
                                            interpreter, build, build.FileName, module, IntPtr.Zero,
                                            build.LoadFlags, ref result);

                                        if (tclApi != null)
                                        {
                                            Tcl_FindExecutable findExecutable;
                                            Tcl_CreateInterp createInterp;
                                            Tcl_ObjGetVar2 objGetVar2;
                                            Tcl_Init init;
                                            Tcl_InitMemory initMemory;
#if TCL_KITS
                                            TclKit_SetKitPath kit_SetKitPath;
                                            TclKit_AppInit kit_AppInit;
#endif

                                            lock (tclApi.SyncRoot)
                                            {
                                                findExecutable = tclApi.FindExecutable;
                                                createInterp = tclApi.CreateInterp;
                                                objGetVar2 = tclApi.ObjGetVar2;
                                                init = tclApi.Init;
                                                initMemory = tclApi.InitMemory;
#if TCL_KITS
                                                kit_SetKitPath = tclApi.Kit_SetKitPath;
                                                kit_AppInit = tclApi.Kit_AppInit;
#endif
                                            }

                                            Version loaded = GetVersion(tclApi);

                                            //
                                            // BUGFIX: Make sure we could obtain the version before trying
                                            //         to compare it.
                                            //
                                            if (loaded != null)
                                            {
                                                if ((minimumRequired == null) ||
                                                    (PackageOps.VersionCompare(loaded, minimumRequired) >= 0))
                                                {
                                                    if ((maximumRequired == null) ||
                                                        (PackageOps.VersionCompare(loaded, maximumRequired) <= 0))
                                                    {
                                                        //
                                                        // NOTE: *REQUIRED* Help Tcl figure out where its library
                                                        //       and encodings are (among other things).
                                                        //
                                                        if (findExecutable != null)
                                                            /* NO RESULT */
                                                            findExecutable(PathOps.GetExecutableName());

                                                        //
                                                        // BUGFIX: We cannot setup the exit handler before this
                                                        //         point because Tcl_FindExecutable must be the
                                                        //         first function called in the Tcl library;
                                                        //         therefore, do it now.
                                                        //
                                                        code = tclApi.SetExitHandler(ref result);

                                                        if (code == ReturnCode.Ok)
                                                        {
#if TCL_KITS
                                                            //
                                                            // NOTE: *REQUIRED* If this API is available, it probably
                                                            //       means that we are dealing with a "stardll" and we
                                                            //       must let it know exactly where it was loaded from.
                                                            //
                                                            if (kit_SetKitPath != null)
                                                                /* IGNORED */
                                                                kit_SetKitPath(build.FileName);
#endif

                                                            //
                                                            // NOTE: If this function is null, we will fail below.
                                                            //
                                                            if (createInterp != null)
                                                                //
                                                                // NOTE: Attempt to create a parent interp.
                                                                //
                                                                newInterp = createInterp();

                                                            //
                                                            // NOTE: Make sure that we got something that at least looks
                                                            //       valid from Tcl_CreateInterp.
                                                            //
                                                            if (newInterp != IntPtr.Zero)
                                                            {
                                                                //
                                                                // NOTE: Attempt to verify that GetNumLevels works because
                                                                //       it is somewhat fragile between different Tcl builds
                                                                //       and versions.  Since no scripts are being evaluated
                                                                //       in the newly created Tcl interpreter, this should
                                                                //       always return zero here.
                                                                //
                                                                if (!GetInterpActive(tclApi, newInterp))
                                                                {
                                                                    //
                                                                    // NOTE: We want the patch level as it would be reported to
                                                                    //       Tcl scripts.
                                                                    //
                                                                    patchLevelNameObjPtr = NewString(
                                                                        tclApi, TclVars.Package.PatchLevelName);

                                                                    //
                                                                    // NOTE: Make sure we were able to allocate the Tcl object
                                                                    //       (really a string).
                                                                    //
                                                                    if (patchLevelNameObjPtr != IntPtr.Zero)
                                                                    {
#if TCL_THREADED
                                                                        //
                                                                        // NOTE: For now, we always enforce the need for a threaded
                                                                        //       build.
                                                                        //
                                                                        threadedNameObjPtr = NewString(
                                                                            tclApi, FormatOps.VariableName(TclVars.Platform.Name,
                                                                            TclVars.Platform.Threaded));

                                                                        //
                                                                        // NOTE: Make sure we were able to allocate the Tcl object
                                                                        //       (really a string).
                                                                        //
                                                                        if (threadedNameObjPtr != IntPtr.Zero)
#endif
                                                                        {
#if TCL_THREADED
                                                                            //
                                                                            // NOTE: The Tcl object where the threading variable
                                                                            //       will be stored.
                                                                            //
                                                                            IntPtr threadedValueObjPtr = IntPtr.Zero;

                                                                            //
                                                                            // NOTE: If this function is null, we will fail below.
                                                                            //
                                                                            if (objGetVar2 != null)
                                                                            {
                                                                                //
                                                                                // NOTE: Query the Tcl library to attempt to figure
                                                                                //       out if this is a threaded build of Tcl.
                                                                                //
                                                                                threadedValueObjPtr = objGetVar2(
                                                                                    newInterp, threadedNameObjPtr, IntPtr.Zero,
                                                                                    Tcl_VarFlags.TCL_GLOBAL_ONLY);
                                                                            }

                                                                            //
                                                                            // NOTE: If the returned variable value is non-NULL,
                                                                            //       then this should be a threaded build of Tcl.
                                                                            //
                                                                            if ((threadedValueObjPtr != IntPtr.Zero) ||
                                                                                FlagOps.HasFlags(loadFlags, LoadFlags.IgnoreThreaded, true))
#endif
                                                                            {
#if TCL_KITS
                                                                                if (kit_AppInit != null)
                                                                                {
                                                                                    //
                                                                                    // NOTE: This appears to be a "stardll".  Call
                                                                                    //       the provided initialization routine
                                                                                    //       instead of the normal one (i.e.
                                                                                    //       Tcl_Init).
                                                                                    //
                                                                                    code = kit_AppInit(newInterp);

                                                                                    if (code != ReturnCode.Ok)
                                                                                    {
                                                                                        //
                                                                                        // NOTE: Tcl failed to fully initialize itself,
                                                                                        //       get the error message and return it to
                                                                                        //       the caller.
                                                                                        //
                                                                                        string localResult = GetResultAsString(
                                                                                            tclApi, newInterp, true);

                                                                                        if (localResult != null)
                                                                                        {
                                                                                            result = localResult;
                                                                                        }
                                                                                        else
                                                                                        {
                                                                                            result = String.Format(
                                                                                                "Tcl interpreter initialization failed " +
                                                                                                "via {0}, result is not available",
                                                                                                typeof(TclKit_AppInit).Name);
                                                                                        }
                                                                                    }
                                                                                }
                                                                                else
#endif
                                                                                if (init != null)
                                                                                {
                                                                                    //
                                                                                    // NOTE: Attempt to initialize the Tcl interpreter
                                                                                    //       for use by scripts using the standard
                                                                                    //       initialization routine (i.e. Tcl_Init).
                                                                                    //
                                                                                    code = init(newInterp);

                                                                                    if (code != ReturnCode.Ok)
                                                                                    {
                                                                                        //
                                                                                        // NOTE: Tcl failed to fully initialize itself,
                                                                                        //       get the error message and return it to
                                                                                        //       the caller.
                                                                                        //
                                                                                        string localResult = GetResultAsString(
                                                                                            tclApi, newInterp, true);

                                                                                        if (localResult != null)
                                                                                        {
                                                                                            result = localResult;
                                                                                        }
                                                                                        else
                                                                                        {
                                                                                            result = String.Format(
                                                                                                "Tcl interpreter initialization failed " +
                                                                                                "via {0}, result is not available",
                                                                                                typeof(Tcl_Init).Name);
                                                                                        }
                                                                                    }
                                                                                }
                                                                                else
                                                                                {
                                                                                    result = "Tcl interpreter initialization is not available";
                                                                                    code = ReturnCode.Error;
                                                                                }

                                                                                if (code == ReturnCode.Ok)
                                                                                {
                                                                                    //
                                                                                    // NOTE: Initialize the memory command(s).
                                                                                    //
                                                                                    if (initMemory != null)
                                                                                        /* NO RESULT */
                                                                                        initMemory(newInterp);

                                                                                    //
                                                                                    // NOTE: The Tcl object where the patch level will
                                                                                    //       be stored.
                                                                                    //
                                                                                    IntPtr patchLevelValueObjPtr = IntPtr.Zero;

                                                                                    //
                                                                                    // NOTE: If this function is null, we will fail below.
                                                                                    //
                                                                                    if (objGetVar2 != null)
                                                                                    {
                                                                                        //
                                                                                        // NOTE: Query the Tcl library patch level.
                                                                                        //
                                                                                        patchLevelValueObjPtr = objGetVar2(
                                                                                            newInterp, patchLevelNameObjPtr, IntPtr.Zero,
                                                                                            Tcl_VarFlags.TCL_GLOBAL_ONLY);
                                                                                    }

                                                                                    //
                                                                                    // NOTE: Were we able to query the patch level for this Tcl
                                                                                    //       library?
                                                                                    //
                                                                                    if (patchLevelValueObjPtr != IntPtr.Zero)
                                                                                    {
                                                                                        //
                                                                                        // NOTE: Return the newly created parent Tcl interpreter
                                                                                        //       to the caller.
                                                                                        //
                                                                                        interp = newInterp;

                                                                                        //
                                                                                        // NOTE: Return the full patchLevel of the loaded Tcl
                                                                                        //       library to the caller.
                                                                                        //
                                                                                        result = GetString(tclApi, patchLevelValueObjPtr);
                                                                                    }
                                                                                    else
                                                                                    {
                                                                                        result = "unsuitable Tcl library, cannot query patch level";
                                                                                        code = ReturnCode.Error;
                                                                                    }
                                                                                }
                                                                            }
#if TCL_THREADED
                                                                            else
                                                                            {
                                                                                result = "unsuitable Tcl library, must be threaded";
                                                                                code = ReturnCode.Error;
                                                                            }
#endif
                                                                        }
#if TCL_THREADED
                                                                        else
                                                                        {
                                                                            result = "could not allocate Tcl object";
                                                                            code = ReturnCode.Error;
                                                                        }
#endif
                                                                    }
                                                                    else
                                                                    {
                                                                        result = "could not allocate Tcl object";
                                                                        code = ReturnCode.Error;
                                                                    }
                                                                }
                                                                else
                                                                {
                                                                    result = String.Format(
                                                                        "unsuitable Tcl library, numLevels offset is not {0}",
                                                                        TclApi.INTERP_NUMLEVELS_OFFSET);

                                                                    code = ReturnCode.Error;
                                                                }
                                                            }
                                                            else
                                                            {
                                                                result = "could not create Tcl interpreter";
                                                                code = ReturnCode.Error;
                                                            }
                                                        }
                                                    }
                                                    else
                                                    {
                                                        result = String.Format(
                                                            "unsuitable Tcl library, loaded version {0} " +
                                                            "does not meet maximum required version {1}",
                                                            FormatOps.WrapOrNull(loaded),
                                                            FormatOps.WrapOrNull(maximumRequired));

                                                        code = ReturnCode.Error;
                                                    }
                                                }
                                                else
                                                {
                                                    result = String.Format(
                                                        "unsuitable Tcl library, loaded version {0} " +
                                                        "does not meet minimum required version {1}",
                                                        FormatOps.WrapOrNull(loaded),
                                                        FormatOps.WrapOrNull(minimumRequired));

                                                    code = ReturnCode.Error;
                                                }
                                            }
                                            else
                                            {
                                                result = "unsuitable Tcl library, cannot obtain version";
                                                code = ReturnCode.Error;
                                            }
                                        }
                                        else
                                        {
                                            code = ReturnCode.Error;
                                        }
                                    }
                                    else
                                    {
                                        code = ReturnCode.Error;
                                    }

                                    return code;
                                }
                                catch (Exception e)
                                {
                                    result = e;
                                    code = ReturnCode.Error;
                                }
                                finally
                                {
                                    //
                                    // NOTE: We cannot do any real cleanup without the Tcl API object
                                    //       being available.
                                    //
                                    if (tclApi != null)
                                    {
                                        Tcl_DbDecrRefCount dbDecrRefCount;
                                        Tcl_DeleteInterp deleteInterp;

                                        lock (tclApi.SyncRoot)
                                        {
                                            dbDecrRefCount = tclApi.DbDecrRefCount;
                                            deleteInterp = tclApi.DeleteInterp;
                                        }

                                        //
                                        // NOTE: Always release the Tcl objects we allocated
                                        //       earlier.  This must be done prior to cleaning up
                                        //       partially loaded Tcl libraries (below) because it
                                        //       requires access to the Tcl API.
                                        //
#if TCL_THREADED
                                        if (threadedNameObjPtr != IntPtr.Zero)
                                        {
                                            if (dbDecrRefCount != null)
                                                /* NO RESULT */
                                                dbDecrRefCount(threadedNameObjPtr, String.Empty, 0);

                                            threadedNameObjPtr = IntPtr.Zero;
                                        }
#endif

                                        if (patchLevelNameObjPtr != IntPtr.Zero)
                                        {
                                            if (dbDecrRefCount != null)
                                                /* NO RESULT */
                                                dbDecrRefCount(patchLevelNameObjPtr, String.Empty, 0);

                                            patchLevelNameObjPtr = IntPtr.Zero;
                                        }

                                        //
                                        // NOTE: Cleanup everything that was partially loaded here.
                                        //       This requires a valid Tcl API object.  If one was
                                        //       not successfully created, we will fall through to
                                        //       the "else if" block below which will unload the Tcl
                                        //       library module itself (if one was even loaded).
                                        //
                                        if (code != ReturnCode.Ok)
                                        {
                                            //
                                            // NOTE: Delete the Tcl interpreter directly rather than
                                            //       ending up in the DeleteInterpreter method because
                                            //       we created it directly and we know it belongs to
                                            //       this thread.
                                            //
                                            if (newInterp != IntPtr.Zero)
                                            {
                                                if (deleteInterp != null)
                                                    /* NO RESULT */
                                                    deleteInterp(newInterp);

                                                newInterp = IntPtr.Zero;
                                            }

                                            ReturnCode unloadCode;
                                            Result unloadError = null;

                                            //
                                            // NOTE: We must force the interpreter to be deleted here
                                            //       because the numLevels offset could be bogus;
                                            //       however, this should be 100% safe since we never
                                            //       used it to actually evaluate anything non-trivial
                                            //       and we know it is not in use now since it belongs
                                            //       to this thread.
                                            //
                                            unloadCode = Unload(interpreter, UnloadFlags.FromLoad,
                                                ref tclApi, ref unloadError);

                                            if (unloadCode != ReturnCode.Ok)
                                                DebugOps.Complain(interpreter, unloadCode, unloadError);

                                            //
                                            // NOTE: Finally, we need to null out the Tcl API object that
                                            //       we previously assigned to the variable provided by
                                            //       the caller.
                                            //
                                            tclApi = null;
                                        }
                                    }
                                    else if (NativeOps.IsValidHandle(module))
                                    {
                                        //
                                        // NOTE: We [at least] partially loaded the Tcl library module.
                                        //       Clean it up now if we did not totally succeed.
                                        //
                                        if (code != ReturnCode.Ok)
                                        {
                                            //
                                            // NOTE: At this point, nobody is using the Tcl library module;
                                            //       therefore, calling FreeLibrary should unload it from
                                            //       memory.  If this fails, it may indicate a serious
                                            //       problem; therefore, throw an exception.
                                            //
                                            int lastError;

                                            if (NativeOps.FreeLibrary(module, out lastError)) /* throw */
                                            {
                                                TraceOps.DebugTrace(String.Format(
                                                    "FreeLibrary (Load): success, " +
                                                    "module = {0}", module),
                                                    typeof(TclWrapper).Name,
                                                    TracePriority.NativeDebug);

                                                module = IntPtr.Zero;
                                            }
                                            else
                                            {
                                                throw new ScriptException(String.Format(
                                                    "FreeLibrary(0x{1:X}) failed with error {0}: {2}",
                                                    lastError, module, NativeOps.GetDynamicLoadingError(
                                                    lastError)));
                                            }
                                        }
                                    }
                                }
                            }
                            else if (HaveAnError(errors))
                            {
                                result = ListOps.Concat(errors, 0, Environment.NewLine);
                            }
                            else
                            {
                                result = "cannot select a Tcl library";
                            }
                        }
                        else if (HaveAnError(errors))
                        {
                            result = ListOps.Concat(errors, 0, Environment.NewLine);
                        }
                        else
                        {
                            result = "cannot find a Tcl library";
                        }
                    }
                    else
                    {
                        result = "cannot overwrite valid Tcl interpreter";
                        code = ReturnCode.Error;
                    }
                }
                else
                {
                    result = "cannot overwrite valid Tcl API object";
                    code = ReturnCode.Error;
                }
            }
            else
            {
                result = "invalid interpreter";
                code = ReturnCode.Error;
            }

            return code;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

#if TCL_WRAPPER
        internal
#else
        public
#endif
        static ReturnCode Unload(
            Interpreter interpreter, /* in */
            UnloadFlags unloadFlags, /* in */
            ref ITclApi tclApi,      /* out */
            ref Result error         /* out */
            )
        {
            IntPtr interp = IntPtr.Zero;

            return Unload(interpreter, unloadFlags, ref tclApi, ref interp, ref error);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public static ReturnCode Unload(
            Interpreter interpreter, /* in: NOT USED. */
            UnloadFlags unloadFlags, /* in */
            ref ITclApi tclApi,      /* in, out */
            ref IntPtr interp,       /* in, out */
            ref Result error         /* out */
            )
        {
            //
            // BUGBUG: I'm not 100% sure that this can ever be done properly in a managed
            //         environment, especially in the presence of unmanaged callbacks into
            //         managed objects via delegates and arbitrary Tcl packages being loaded
            //         into one or more interpreters.
            //
            ReturnCode code;

            try
            {
                if (interpreter != null)
                {
                    if (TclApi.CheckModule(tclApi, ref error))
                    {
                        string fileName;
                        Tcl_Finalize _finalize;
                        IntPtr module;

                        lock (tclApi.SyncRoot)
                        {
                            fileName = tclApi.FileName;
                            _finalize = tclApi._Finalize;
                            module = tclApi.Module;
                        }

                        //
                        // NOTE: If an apparently valid Tcl interpreter was specified by the caller
                        //       then we must be on the correct thread to delete it.  If the force
                        //       flag was specified, skip this check because the Eagle interpreter
                        //       may not contain the necessary state information to validate it
                        //       against the current thread.
                        //
                        bool noThread = FlagOps.HasFlags(unloadFlags, UnloadFlags.NoInterpThread, true);

                        if (noThread || (interp == IntPtr.Zero) || tclApi.CheckInterp(interp, ref error))
                        {
                            //
                            // NOTE: Do we want to skip checking if the Tcl interpreter is active?
                            //       This flag should really only be used by the loader when
                            //       cleaning up.
                            //
                            bool noActive = FlagOps.HasFlags(unloadFlags, UnloadFlags.NoInterpActive, true);

                            //
                            // NOTE: If the parent Tcl interpreter needs to be deleted, do it now.
                            //       The parent Tcl interpreter is a per-interpreter resource, not
                            //       a shared one; therefore, it does not need to be reference
                            //       counted.
                            //
                            if ((interp != IntPtr.Zero) && !GetInterpDeleted(tclApi, interp))
                                code = DeleteInterpreter(tclApi, noActive, ref interp, ref error);
                            else
                                code = ReturnCode.Ok;

                            //
                            // NOTE: Make 100% sure that we were able to delete the parent Tcl
                            //       interpreter before continuing.  If not, it could mean that it
                            //       is still in use and we should not yank the Tcl library out
                            //       from under ourselves.
                            //
                            if (code == ReturnCode.Ok)
                            {
                                //
                                // NOTE: Do we want to release the reference for the Tcl module?
                                //       If this flag is not set, we simply assume we should skip
                                //       reference count management.
                                //
                                bool releaseModule = FlagOps.HasFlags(unloadFlags,
                                    UnloadFlags.ReleaseModule, true);

                                //
                                // NOTE: Attempt to reduce the Tcl library module reference count.
                                //       If the count reaches zero, cleanup and unload the Tcl
                                //       library.  If the returned count is invalid, we failed to
                                //       release the reference for some reason.
                                //
                                int referenceCount = releaseModule ?
                                    ReleaseModuleReference(fileName, false, false, ref error) : 0;

                                //
                                // BUGFIX: Make sure that we do [most] of the handling below even
                                //         if the reference count has not reached zero.  The only
                                //         things we CANNOT do until the reference count reaches
                                //         zero are: finalizing the Tcl library and physically
                                //         unloading the native library from memory.
                                //
                                if (referenceCount != Count.Invalid)
                                {
                                    //
                                    // NOTE: Does the caller want us to delete or clear the exit
                                    //       handler?
                                    //
                                    bool exitHandler = FlagOps.HasFlags(unloadFlags,
                                        UnloadFlags.ExitHandler, true);

                                    //
                                    // NOTE: Do we want to call the Tcl_Finalize delegate, if
                                    //       possible?  We should not do this if we are being
                                    //       called via the exit handler (i.e. because, in that
                                    //       case, we are being invoked by the exit handler,
                                    //       which was invoked by Tcl_Finalize, which may have
                                    //       been invoked by Tcl_Exit).
                                    //
                                    bool finalize = FlagOps.HasFlags(unloadFlags,
                                        UnloadFlags.Finalize, true);

                                    //
                                    // NOTE: See if we need to deal with tearing down the exit
                                    //       handler at all.
                                    //
                                    if (exitHandler)
                                    {
                                        //
                                        // NOTE: If we are unloading from the exit handler (i.e.
                                        //       via Tcl_Finalize) then there is no need to
                                        //       remove the exit handler; otherwise, we must
                                        //       succeed at doing so.
                                        //
                                        if (finalize)
                                            code = tclApi.UnsetExitHandler(ref error);
                                        else
                                            code = tclApi.ClearExitHandler(ref error);
                                    }

                                    //
                                    // NOTE: Make 100% sure that we were able to delete the exit
                                    //       handler before continuing.  If not, it could mean
                                    //       that it is still in use and we should not yank the
                                    //       Tcl library out from under ourselves.
                                    //
                                    if (code == ReturnCode.Ok)
                                    {
                                        //
                                        // NOTE: Finalize the Tcl library.  This is potentially
                                        //       dangerous because it could cause quite a number
                                        //       of side-effects to happen depending on the Tcl
                                        //       packages that have been loaded and any
                                        //       outstanding calls into the Tcl library.  It is
                                        //       the responsbility of the caller of this function
                                        //       to make sure that no outstanding calls into the
                                        //       Tcl library are pending prior to calling this
                                        //       function with the "finalize" flag enabled.
                                        //
                                        if (finalize && (referenceCount == 0) &&
                                            (_finalize != null))
                                        {
                                            /* NO RESULT */
                                            _finalize();
                                        }

                                        //
                                        // NOTE: Have we been requested by the caller to free
                                        //       the library itself (if the reference count
                                        //       reaches zero)?
                                        //
                                        bool freeLibrary = FlagOps.HasFlags(unloadFlags,
                                            UnloadFlags.FreeLibrary, true);

                                        //
                                        // NOTE: At this point, we believe that nobody is using
                                        //       the Tcl library module; therefore, calling
                                        //       FreeLibrary should unload it from memory.  We
                                        //       should not do this if we are being called via
                                        //       the exit handler because the Tcl library needs
                                        //       to remain loaded as long as the call stack
                                        //       contains code that resides in the Tcl library
                                        //       (e.g. Tcl_Finalize, Tcl_Exit, etc); otherwise,
                                        //       an access violation will occur.
                                        //
                                        if (freeLibrary && (referenceCount == 0) &&
                                            NativeOps.IsValidHandle(module))
                                        {
                                            int lastError;

                                            if (NativeOps.FreeLibrary(
                                                    module, out lastError)) /* throw */
                                            {
                                                TraceOps.DebugTrace(String.Format(
                                                    "FreeLibrary (Unload): success, " +
                                                    "module = {0}", module),
                                                    typeof(TclWrapper).Name,
                                                    TracePriority.NativeDebug);

                                                module = IntPtr.Zero;
                                            }
                                            else
                                            {
                                                throw new ScriptException(String.Format(
                                                    "FreeLibrary(0x{1:X}) failed with error {0}: {2}",
                                                    lastError, module, NativeOps.GetDynamicLoadingError(
                                                    lastError)));
                                            }
                                        }

                                        //
                                        // NOTE: If we previously reduced the reference count
                                        //       (to zero since we got to this point), completely
                                        //       remove the module now.
                                        //
                                        if (releaseModule && (referenceCount == 0))
                                        {
                                            Result releaseError = null;

                                            if (ReleaseModuleReference(fileName, true, true,
                                                    ref releaseError) == Count.Invalid)
                                            {
                                                DebugOps.Complain(interpreter,
                                                    ReturnCode.Error, releaseError);
                                            }
                                        }

                                        //
                                        // NOTE: Finally, dispose the Tcl API object, if any.
                                        //
                                        IDisposable disposable = tclApi as IDisposable;

                                        if (disposable != null)
                                        {
                                            disposable.Dispose(); /* throw */
                                            disposable = null;
                                        }

                                        //
                                        // NOTE: Clear out our Tcl API object reference.
                                        //
                                        tclApi = null;
                                    }
                                    else
                                    {
                                        //
                                        // NOTE: We did not even attempt to actually unload the
                                        //       Tcl library; therefore, re-increment the
                                        //       reference count, undoing our previous decrement
                                        //       (above), if necessary, so that this operation
                                        //       can be retried later.
                                        //
                                        if (releaseModule)
                                        {
                                            Result addError = null;

                                            if (!AddModuleReference(fileName, ref addError))
                                            {
                                                DebugOps.Complain(interpreter,
                                                    ReturnCode.Error, addError);
                                            }
                                        }
                                    }
                                }
                                else
                                {
                                    code = ReturnCode.Error;
                                }
                            }
                        }
                        else
                        {
                            code = ReturnCode.Error;
                        }
                    }
                    else
                    {
                        code = ReturnCode.Error;
                    }
                }
                else
                {
                    error = "invalid interpreter";
                    code = ReturnCode.Error;
                }
            }
            catch (Exception e)
            {
                error = e;
                code = ReturnCode.Error;
            }

            return code;
        }
    }
}
