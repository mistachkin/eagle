/*
 * Constants.cs --
 *
 * Copyright (c) 2007-2012 by Joe Mistachkin.  All rights reserved.
 *
 * See the file "license.terms" for information on usage and redistribution of
 * this file, and for a DISCLAIMER OF ALL WARRANTIES.
 *
 * RCS: @(#) $Id: $
 */

#if ARM || ARM64 || X86 || IA64 || X64
#define HAVE_SIZEOF
#endif

using System;
using System.Threading;
using Eagle._Attributes;
using Eagle._Components.Private;
using Eagle._Components.Public;
using Eagle._Containers.Public;

#if !CONSOLE
using ConsoleColor = Eagle._Components.Public.ConsoleColor;
#endif

namespace Eagle._Constants
{
#if NET_40
    /// <summary>
    /// This class contains the synthetic type code used to represent a big
    /// integer value, for which the <see cref="TypeCode" /> enumeration has
    /// no real member.
    /// </summary>
    [ObjectId("67e99580-6ea8-42c2-9f13-9b8cc8d9c509")]
    public static class _TypeCode
    {
        //
        // HACK: The .NET Framework does not (yet?) have a
        //       real TypeCode enumeration value for this.
        //
        /// <summary>
        /// The synthetic type code used to represent a big integer value.
        /// </summary>
        public const TypeCode BigInteger = (TypeCode)9999;
    }
#endif

    ///////////////////////////////////////////////////////////////////////////

    /// <summary>
    /// This class contains the names of the custom HTTP headers used by the
    /// Eagle core library.
    /// </summary>
    [ObjectId("71552ae2-fdc3-402f-ae9e-9eade60d96a8")]
    public static class WebHeaders
    {
        /// <summary>
        /// The name of the HTTP header that carries the Eagle build tag.
        /// </summary>
        public static readonly string Tag = "X-Eagle-Tag";
        /// <summary>
        /// The name of the HTTP header that carries the Eagle version.
        /// </summary>
        public static readonly string Version = "X-Eagle-Version";
    }

    ///////////////////////////////////////////////////////////////////////////

    /// <summary>
    /// This class contains the well-known annotation names used to attach
    /// extra metadata to various objects within the Eagle core library.
    /// </summary>
    [ObjectId("babcfc00-fa2a-4dcf-9d1b-d31dafb0b70d")]
    public static class Annotations
    {
        /// <summary>
        /// The annotation indicating that an item is private.
        /// </summary>
        public static readonly string Private = "private";
        /// <summary>
        /// The annotation indicating that an item is fast.
        /// </summary>
        public static readonly string Fast = "fast";
        /// <summary>
        /// The annotation indicating that an item is atomic.
        /// </summary>
        public static readonly string Atomic = "atomic";
        /// <summary>
        /// The annotation indicating that an item is inline.
        /// </summary>
        public static readonly string Inline = "inline";

#if ARGUMENT_CACHE || PARSE_CACHE
        /// <summary>
        /// The annotation indicating that an item is non-caching.
        /// </summary>
        public static readonly string NonCaching = "nonCaching";
#endif

        /// <summary>
        /// The annotation indicating the associated set of match types.
        /// </summary>
        public static readonly string MatchTypes = "matchTypes";

        /// <summary>
        /// The annotation indicating the associated signature.
        /// </summary>
        public static readonly string Signature = "signature";

        /// <summary>
        /// The annotation indicating that an item may be overwritten.
        /// </summary>
        public static readonly string Overwrite = "overwrite";

        /// <summary>
        /// The annotation indicating that an item is (or should be) clean.
        /// </summary>
        public static readonly string Clean = "clean";

        /// <summary>
        /// The annotation indicating the associated not-after constraint.
        /// </summary>
        public static readonly string NotAfter = "notAfter";
        /// <summary>
        /// The annotation indicating the associated not-before constraint.
        /// </summary>
        public static readonly string NotBefore = "notBefore";
    }

    ///////////////////////////////////////////////////////////////////////////

    /// <summary>
    /// This class contains the sentinel values used to represent the various
    /// special states of a numeric limit.
    /// </summary>
    [ObjectId("0c564d1c-a98a-4ed7-aabe-1685abae114f")]
    public static class Limits
    {
        /// <summary>
        /// The value indicating that there is no limit.
        /// </summary>
        public static readonly int Unlimited = 0;
        /// <summary>
        /// The value indicating that the operation is forbidden.
        /// </summary>
        public static readonly int Forbidden = -1;
        /// <summary>
        /// The value indicating that the limit is unknown.
        /// </summary>
        public static readonly int Unknown = -2;
    }

    ///////////////////////////////////////////////////////////////////////////

    /// <summary>
    /// This class contains the sentinel values used to represent the various
    /// special states of a hash code.
    /// </summary>
    [ObjectId("919f62ff-f2a9-4b24-ac1f-accb23b94324")]
    public static class HashCode
    {
        /// <summary>
        /// The value representing an invalid hash code.
        /// </summary>
        public static readonly int Invalid = -1;
        /// <summary>
        /// The value representing the absence of a hash code.
        /// </summary>
        public static readonly int None = 0;
    }

    ///////////////////////////////////////////////////////////////////////////

    /// <summary>
    /// This class contains the string used to represent a null object value.
    /// </summary>
    [ObjectId("b8693a1a-f084-4aa7-921a-88777090686a")]
    public static class _Object
    {
        /// <summary>
        /// The string used to represent a null object value.
        /// </summary>
        public static readonly string Null = _String.Null;
    }

    ///////////////////////////////////////////////////////////////////////////

    /// <summary>
    /// This class contains the sentinel value, in milliseconds, used to
    /// represent a time interval that never elapses.
    /// </summary>
    [ObjectId("548b2832-5bd1-4e7e-893c-c2e0f88d2cfc")]
    public static class Milliseconds
    {
        /// <summary>
        /// The number of milliseconds representing a time interval that never
        /// elapses.
        /// </summary>
        public static readonly double Never = -1.0;
    }

    ///////////////////////////////////////////////////////////////////////////

    /// <summary>
    /// This class contains the sentinel values used to represent the various
    /// special states of an identifier.
    /// </summary>
    [ObjectId("08289e0a-277d-453c-8209-df7e95640e7b")]
    public static class Identifier
    {
        /// <summary>
        /// The value indicating that an identifier has the wrong type.
        /// </summary>
        public static readonly int TypeMismatch = -2;
        /// <summary>
        /// The value representing an invalid identifier.
        /// </summary>
        public static readonly int Invalid = -1;
    }

    ///////////////////////////////////////////////////////////////////////////

    /// <summary>
    /// This class contains the well-known string values used to represent
    /// certain special string states.
    /// </summary>
    [ObjectId("5f7121e8-6c6b-4c72-8d67-d2e8d57eabce")]
    public static class _String
    {
        /// <summary>
        /// The string used to represent a null value.
        /// </summary>
        public static readonly string Null = "null";
        /// <summary>
        /// The string used to represent a proxy value.
        /// </summary>
        public static readonly string Proxy = "proxy";
    }

    ///////////////////////////////////////////////////////////////////////////

    /// <summary>
    /// This class contains the minimum value allowed for a version component.
    /// </summary>
    [ObjectId("0a1a7a70-18fb-4164-b5d9-aebb833e1d75")]
    public static class _Version
    {
        /// <summary>
        /// The minimum value allowed for a version component.
        /// </summary>
        public static readonly int Minimum = 0;
    }

    ///////////////////////////////////////////////////////////////////////////

    /// <summary>
    /// This class contains the names of the XML attributes recognized within
    /// an Eagle script block document.
    /// </summary>
#if XML
    [ObjectId("19989260-5883-4823-98ee-8a73533b6709")]
    public static class _XmlAttribute
    {
        /// <summary>
        /// The name of the identifier XML attribute.
        /// </summary>
        public static readonly string Id = "id";
        /// <summary>
        /// The name of the type XML attribute.
        /// </summary>
        public static readonly string Type = "type";
        /// <summary>
        /// The name of the text XML attribute.
        /// </summary>
        public static readonly string Text = "text";
        /// <summary>
        /// The name of the name XML attribute.
        /// </summary>
        public static readonly string Name = "name";
        /// <summary>
        /// The name of the group XML attribute.
        /// </summary>
        public static readonly string Group = "group";
        /// <summary>
        /// The name of the description XML attribute.
        /// </summary>
        public static readonly string Description = "description";
        /// <summary>
        /// The name of the time stamp XML attribute.
        /// </summary>
        public static readonly string TimeStamp = "timeStamp";
        /// <summary>
        /// The name of the public key token XML attribute.
        /// </summary>
        public static readonly string PublicKeyToken = "publicKeyToken";
        /// <summary>
        /// The name of the signature XML attribute.
        /// </summary>
        public static readonly string Signature = "signature";

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// The list of XML attribute names processed by the script engine.
        /// </summary>
        internal static readonly StringList EngineList = new StringList(
            new string[] { Type, Text });

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// The list of XML attribute names that are required.
        /// </summary>
        internal static readonly StringList RequiredList = new StringList(
            new string[] { Id, Type, Text });

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// The list of all recognized XML attribute names.
        /// </summary>
        internal static readonly StringList AllList = new StringList(
            new string[] {
            Id, Type, Text, Name, Group, Description, TimeStamp,
            PublicKeyToken, Signature
        });
    }

    ///////////////////////////////////////////////////////////////////////////

    /// <summary>
    /// This class contains the well-known XML schema, namespace, and XPath
    /// query values used when processing Eagle script block documents.
    /// </summary>
    [ObjectId("da7b3fc1-24f5-4a2a-8f9e-deda241ddb15")]
    public static class Xml
    {
        //
        // NOTE: The name of the resource stream that contains our XML schema
        //       data.
        //
        /// <summary>
        /// The name of the resource stream that contains the XML schema data.
        /// </summary>
        public static readonly string SchemaResourceName =
            GlobalState.GetPackageName(PackageType.Default, null, ".xsd",
                false);

        //
        // NOTE: The name of our XML namespace.
        //
        /// <summary>
        /// The name of the Eagle XML namespace.
        /// </summary>
        public static readonly string NamespaceName =
            GlobalState.GetPackageNameNoCase();

        //
        // NOTE: The URI of our XML namespace.
        //
        /// <summary>
        /// The URI of the Eagle XML namespace.
        /// </summary>
        public static readonly Uri ScriptNamespaceUri =
            GlobalState.GetAssemblyNamespaceUri();

        //
        // HACK: Hard-code the "well-known" security package (Harpy)
        //       script signature schema URI.
        //
        /// <summary>
        /// The well-known URI of the Harpy security package script signature
        /// schema namespace.
        /// </summary>
        public static readonly Uri SignatureNamespaceUri =
            new Uri("https://eagle.to/2011/harpy");

        //
        // NOTE: The candidate XPath queries used to extract (script)
        //       blocks from an XML document.  The first query that
        //       returns some nodes wins.
        //
        /// <summary>
        /// The candidate XPath queries used to extract script blocks from an
        /// XML document; the first query that returns some nodes wins.
        /// </summary>
        public static readonly StringList XPathList = new StringList(
            new string[] {
            //
            // NOTE: First, check for the necessary elements using the
            //       name of our namespace.
            //
            (NamespaceName != null) ?
                "//" + NamespaceName + ":blocks/" + NamespaceName +
                ":block" : null,

            //
            // NOTE: Second, check for the necessary elements using the
            //       default namespace.
            //
            "//blocks/block",

            //
            // NOTE: These list elements are reserved for future use by
            //       the core library.  Please do not change them.
            //
            null,
            null,
            null,
            null,

            //
            // NOTE: These list elements are reserved for future use by
            //       third-party code.
            //
            null,
            null,
            null,
            null
        });
    }
#endif

    ///////////////////////////////////////////////////////////////////////////

    /// <summary>
    /// This class contains the sentinel value used to represent an invalid
    /// index.
    /// </summary>
    [ObjectId("51f47a43-205f-4241-931a-e072256719be")]
    public static class Index
    {
        /// <summary>
        /// The value representing an invalid index.
        /// </summary>
        public static readonly int Invalid = -1;
    }

    ///////////////////////////////////////////////////////////////////////////

    /// <summary>
    /// This class contains the sentinel value used to represent an invalid
    /// call frame level.
    /// </summary>
    [ObjectId("ab498ee2-f654-404e-b2df-4af7c5482bb1")]
    public static class Level
    {
        /// <summary>
        /// The value representing an invalid call frame level.
        /// </summary>
        public static readonly int Invalid = -1;
    }

    ///////////////////////////////////////////////////////////////////////////

    /// <summary>
    /// This class contains the well-known and sentinel values associated with
    /// a count.
    /// </summary>
    [ObjectId("17cc4b98-cdf3-4a73-ada6-3c8dcf34d9ad")]
    public static class Count
    {
        /// <summary>
        /// The size, in characters, of the hexadecimal prefix used when
        /// formatting a count.
        /// </summary>
        public static readonly int PrefixSize = (sizeof(int) * 2) + 1;

        /// <summary>
        /// The value representing an invalid count.
        /// </summary>
        public static readonly int Invalid = -1;
        /// <summary>
        /// The value representing the absence of a count.
        /// </summary>
        public static readonly int None = -2;
    }

    ///////////////////////////////////////////////////////////////////////////

    /// <summary>
    /// This class contains the sentinel value used to represent an invalid
    /// size.
    /// </summary>
    [ObjectId("c9f34b0c-d4a5-4d63-832c-ff9515924735")]
    public static class Size
    {
        /// <summary>
        /// The value representing an invalid size.
        /// </summary>
        public static readonly long Invalid = -1;
    }

    ///////////////////////////////////////////////////////////////////////////

    /// <summary>
    /// This class contains the sentinel value used to represent an invalid
    /// percentage.
    /// </summary>
    [ObjectId("8630da9a-e4f9-40f3-ab61-10201f8007ca")]
    public static class Percent
    {
        /// <summary>
        /// The value representing an invalid percentage.
        /// </summary>
        public static readonly int Invalid = -1;
    }

    ///////////////////////////////////////////////////////////////////////////

    /// <summary>
    /// This class contains the well-known console color values used to
    /// represent the various special color states.
    /// </summary>
    [ObjectId("710e4bb2-28a1-439a-ba12-9e91f3d93ff4")]
    public static class _ConsoleColor
    {
        /// <summary>
        /// The console color representing the absence of a color.
        /// </summary>
        public static readonly ConsoleColor None =
            (ConsoleColor)HostColor.None;

        /// <summary>
        /// The console color representing an invalid color.
        /// </summary>
        public static readonly ConsoleColor Invalid =
            (ConsoleColor)HostColor.Invalid;

        /// <summary>
        /// The console color representing the default color.
        /// </summary>
        public static readonly ConsoleColor Default =
            (ConsoleColor)HostColor.None;
    }

    ///////////////////////////////////////////////////////////////////////////

    /// <summary>
    /// This class contains the file names of the native libraries used (via
    /// platform invoke) by the Eagle core library.
    /// </summary>
#if NATIVE
    [ObjectId("03fcde0a-0800-4f49-8462-dddbd66bf90f")]
    public static class DllName
    {
#if WINDOWS
#if !MONO
        //
        // NOTE: This is the file name for the "Microsoft COM Object Runtime
        //       Execution Engine" and it should be available on the "real"
        //       .NET Framework (all versions).  This DLL is not available on
        //       Mono.
        //
        /// <summary>
        /// The file name of the Microsoft COM Object Runtime Execution Engine
        /// native library.
        /// </summary>
        public const string MsCorEe = "mscoree.dll";
#endif

        /// <summary>
        /// The file name of the Microsoft Visual C runtime native library.
        /// </summary>
        public const string MsVcRt = "msvcrt.dll";
        /// <summary>
        /// The file name of the advanced Windows services native library.
        /// </summary>
        public const string AdvApi32 = "advapi32.dll";
        /// <summary>
        /// The file name of the advanced installation native library.
        /// </summary>
        public const string AdvPack = "advpack.dll";
        /// <summary>
        /// The file name of the cryptographic services native library.
        /// </summary>
        public const string Crypt32 = "crypt32.dll";
        /// <summary>
        /// The file name of the Windows kernel native library.
        /// </summary>
        public const string Kernel32 = "kernel32.dll";
        /// <summary>
        /// The file name of the NT layer native library.
        /// </summary>
        public const string NtDll = "ntdll.dll";
        /// <summary>
        /// The file name of the Windows shell native library.
        /// </summary>
        public const string Shell32 = "shell32.dll";
        /// <summary>
        /// The file name of the Windows user interface native library.
        /// </summary>
        public const string User32 = "user32.dll";
        /// <summary>
        /// The file name of the Windows trust verification native library.
        /// </summary>
        public const string WinTrust = "wintrust.dll";
        /// <summary>
        /// The file name of the Windows Sockets 2 native library.
        /// </summary>
        public const string Ws2_32 = "ws2_32.dll";
        /// <summary>
        /// The file name of the Windows Terminal Services native library.
        /// </summary>
        public const string WtsApi32 = "wtsapi32.dll";
        /// <summary>
        /// The file name of the Internet Explorer runtime utility native
        /// library.
        /// </summary>
        public const string IeRtUtil = "IeRtUtil.dll";
#endif

#if NATIVE
        /// <summary>
        /// The name of the Bolt native library.
        /// </summary>
        public const string Bolt = "Bolt";
#endif

#if NATIVE_UTILITY
        /// <summary>
        /// The file name of the native utility (Spilornis) library.
        /// </summary>
        public const string Utility = "spilornis.dll";
#endif

#if UNIX
        /// <summary>
        /// The name of the Unix C runtime native library.
        /// </summary>
        public const string LibC = "libc";
        /// <summary>
        /// The name of the Unix dynamic loader native library.
        /// </summary>
        public const string LibDL = "libdl";
        /// <summary>
        /// The file name of the GNU readline native library.
        /// </summary>
        public const string ReadLine = "libreadline.so";
        /// <summary>
        /// The file name of the BSD editline native library.
        /// </summary>
        public const string Edit = "libedit.dylib";

        /// <summary>
        /// The name used to reference symbols within the current process
        /// (i.e. statically linked) rather than an external native library.
        /// </summary>
#if !NET_STANDARD_20
        public const string Internal = "__Internal";
#else
        public const string Internal = LibC;
#endif

        /// <summary>
        /// The file name of the macOS system native library.
        /// </summary>
        public const string LibSystem = "libSystem.B.dylib";
        /// <summary>
        /// The file name of the macOS XPC native library.
        /// </summary>
        public const string LibXpc = "/usr/lib/system/libxpc.dylib";
#endif
    }
#endif

    ///////////////////////////////////////////////////////////////////////////

    /// <summary>
    /// This class contains the well-known return values produced by the native
    /// wait functions.
    /// </summary>
    [ObjectId("4adf4145-56a6-4540-987c-2447543eba40")]
    public static class WaitResult
    {
        /// <summary>
        /// The base value indicating that the first object was signaled.
        /// </summary>
        public static readonly int Object0 = 0x0;
        /// <summary>
        /// The base value indicating that the first mutex was abandoned.
        /// </summary>
        public static readonly int Abandoned0 = 0x80;
        /// <summary>
        /// The value indicating that an I/O completion callback occurred.
        /// </summary>
        public static readonly int IoCompletion = 0xC0;
        /// <summary>
        /// The value indicating that the wait timed out.
        /// </summary>
        public static readonly int Timeout = WaitHandle.WaitTimeout;
        /// <summary>
        /// The value indicating that the wait failed.
        /// </summary>
        public static readonly int Failed = unchecked((int)0xFFFFFFFF);

        ///////////////////////////////////////////////////////////////////////

#if MONO || MONO_HACKS
        //
        // HACK: *MONO* This value was stolen from:
        //
        //       "/mono/mcs/class/referencesource/ -->
        //       --> mscorlib/system/threading/waithandle.cs"
        //
        /// <summary>
        /// The value indicating that the wait failed, as used by Mono.
        /// </summary>
        public static readonly int MonoFailed = 0x7FFFFFFF;
#endif
    }

    ///////////////////////////////////////////////////////////////////////////

    /// <summary>
    /// This class contains the well-known timeout values, in milliseconds.
    /// </summary>
    [ObjectId("68a277d1-fb1b-4bbb-8d45-140e80ef0e3f")]
    public static class _Timeout
    {
        /// <summary>
        /// The value indicating an infinite (i.e. never expiring) timeout.
        /// </summary>
        public static readonly int Infinite = Timeout.Infinite;
        /// <summary>
        /// The value indicating the absence of a timeout.
        /// </summary>
        public static readonly int None = 0;
        /// <summary>
        /// The minimum permitted timeout value.
        /// </summary>
        public static readonly int Minimum = 1;
        // public static readonly int Maximum = 300000;
    }

    ///////////////////////////////////////////////////////////////////////////

    /// <summary>
    /// This class contains the sentinel and default values used when emitting
    /// a console beep.
    /// </summary>
    [ObjectId("2d9df6da-630f-463f-84f1-bc8ff28f018c")]
    public static class Beep
    {
        /// <summary>
        /// The value representing an invalid beep parameter.
        /// </summary>
        public static readonly int Invalid = -1;

        //
        // NOTE: These are the "default" beep values, per MSDN.
        //
        /// <summary>
        /// The default beep frequency, in hertz.
        /// </summary>
        public static readonly int Frequency = 800;
        /// <summary>
        /// The default beep duration, in milliseconds.
        /// </summary>
        public static readonly int Duration = 200;
    }

    ///////////////////////////////////////////////////////////////////////////

    /// <summary>
    /// This class contains the sentinel and default values associated with a
    /// width (e.g. of the console).
    /// </summary>
    [ObjectId("0407ec6f-852f-4d02-add6-36fdf51fedad")]
    public static class Width
    {
        /// <summary>
        /// The value representing an invalid width.
        /// </summary>
        public static readonly int Invalid = -1;
        /// <summary>
        /// The default width.
        /// </summary>
        public static readonly int Default = 80;
        /// <summary>
        /// The minimum permitted width.
        /// </summary>
        public static readonly int Minimum = 40;
    }

    ///////////////////////////////////////////////////////////////////////////

    /// <summary>
    /// This class contains the sentinel and default values associated with a
    /// height (e.g. of the console).
    /// </summary>
    [ObjectId("cffe4c6d-2308-430e-9ceb-b1980fb5ee9c")]
    public static class Height
    {
        /// <summary>
        /// The value representing an invalid height.
        /// </summary>
        public static readonly int Invalid = -1;
        /// <summary>
        /// The default height.
        /// </summary>
        public static readonly int Default = 25;
        /// <summary>
        /// The minimum permitted height.
        /// </summary>
        public static readonly int Minimum = 10;
    }

    ///////////////////////////////////////////////////////////////////////////

    /// <summary>
    /// This class contains the sentinel and default values associated with a
    /// margin.
    /// </summary>
    [ObjectId("efc3c71a-c818-48ed-876e-831a25418a54")]
    public static class Margin
    {
        /// <summary>
        /// The value representing an invalid margin.
        /// </summary>
        public static readonly int Invalid = -1;
        /// <summary>
        /// The default margin.
        /// </summary>
        public static readonly int Default = 4;
        /// <summary>
        /// The minimum permitted margin.
        /// </summary>
        public static readonly int Minimum = 2;
    }

    ///////////////////////////////////////////////////////////////////////////

    /// <summary>
    /// This class contains the sentinel value used to represent an invalid
    /// length.
    /// </summary>
    [ObjectId("27c8ffcc-9656-4a55-912a-e68f5fb5bcfc")]
    public static class Length
    {
        /// <summary>
        /// The value representing an invalid length.
        /// </summary>
        public static readonly int Invalid = -1;
    }

    ///////////////////////////////////////////////////////////////////////////

    /// <summary>
    /// This class contains the sentinel value used to represent an invalid
    /// tick count.
    /// </summary>
    [ObjectId("e93c850e-3b88-49e6-b205-7752bbeefbf2")]
    public static class _Ticks
    {
        /// <summary>
        /// The value representing an invalid tick count.
        /// </summary>
        public static readonly long Invalid = -1;
    }

    ///////////////////////////////////////////////////////////////////////////

    /// <summary>
    /// This class contains the sentinel value used to represent an invalid
    /// position.
    /// </summary>
    [ObjectId("54455e76-14fc-4fb1-bfec-6273b550fefa")]
    public static class _Position
    {
        /// <summary>
        /// The value representing an invalid position.
        /// </summary>
        public static readonly int Invalid = -1;
    }

    ///////////////////////////////////////////////////////////////////////////

    /// <summary>
    /// This class contains the sentinel value used to represent an invalid
    /// size.
    /// </summary>
    [ObjectId("cab82601-534e-4c1b-8555-c936e8dc6f73")]
    public static class _Size
    {
        /// <summary>
        /// The value representing an invalid size.
        /// </summary>
        public static readonly int Invalid = -1;
    }

    ///////////////////////////////////////////////////////////////////////////

    /// <summary>
    /// This class contains the well-known and sentinel values associated with
    /// a network port number.
    /// </summary>
    [ObjectId("ff56c130-734d-4a3d-9dc1-f38d3c02a144")]
    public static class Port
    {
        /// <summary>
        /// The value representing an invalid port number.
        /// </summary>
        public static readonly int Invalid = -1;
        /// <summary>
        /// The value indicating that the port number should be selected
        /// automatically (e.g. by the operating system) for clients.
        /// </summary>
        public static readonly int Automatic = 0; // for clients.
        /// <summary>
        /// The port number used by the Network Time Protocol (NTP).
        /// </summary>
        public static readonly int NetworkTime = 123; /* NTP */
    }

    ///////////////////////////////////////////////////////////////////////////

    /// <summary>
    /// This class contains the well-known path components (directory names)
    /// used by the Eagle core library.
    /// </summary>
    [ObjectId("5cd54e77-bd26-402f-8556-17a9820cd1e1")]
    public static class _Path
    {
        /// <summary>
        /// The path component referring to the current directory.
        /// </summary>
        public static readonly string Current = ".";
        /// <summary>
        /// The path component referring to the parent directory.
        /// </summary>
        public static readonly string Parent = "..";

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// The name of the script library directory.
        /// </summary>
        public static readonly string Library = "Library";
        /// <summary>
        /// The name of the resources directory.
        /// </summary>
        public static readonly string Resources = "Resources";
        /// <summary>
        /// The name of the loader directory.
        /// </summary>
        public static readonly string Loader = "Loader1.0";
        /// <summary>
        /// The name of the tests directory.
        /// </summary>
        public static readonly string Tests = "Tests";
        /// <summary>
        /// The name of the data directory.
        /// </summary>
        public static readonly string Data = "data";
        /// <summary>
        /// The name of the native Tcl directory.
        /// </summary>
        public static readonly string Tcl = "tcl";
        /// <summary>
        /// The name of the plugins directory.
        /// </summary>
        public static readonly string Plugins = "Plugins";
        /// <summary>
        /// The name of the build tasks directory.
        /// </summary>
        public static readonly string BuildTasks = "BuildTasks";
        /// <summary>
        /// The name of the externals directory.
        /// </summary>
        public static readonly string Externals = "Externals";
        /// <summary>
        /// The prefix shared by the target framework directory names (e.g.
        /// "netstandard2.0").
        /// </summary>
        public static readonly string NetPrefix = "net"; // e.g. netstandard2.0
    }

    ///////////////////////////////////////////////////////////////////////////

    /// <summary>
    /// This class contains the file names (including any relative path) of the
    /// well-known scripts used by the Eagle core library.
    /// </summary>
    [ObjectId("c9f38457-538c-4dfc-8e16-a1b8f3d6c03a")]
    public static class FileName
    {
        /// <summary>
        /// The file name of the loader script.
        /// </summary>
        public static readonly string Loader =
            FormatOps.ScriptTypeToFileName(ScriptTypes.Loader,
                PackageType.Loader, false, false);

        /// <summary>
        /// The file name of the loader package index script.
        /// </summary>
        public static readonly string LoaderPackageIndex =
            FormatOps.ScriptTypeToFileName(ScriptTypes.PackageIndex,
                PackageType.Loader, false, false);

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// The file name of the library initialization script.
        /// </summary>
        public static readonly string Initialization =
            FormatOps.ScriptTypeToFileName(ScriptTypes.Initialization,
                PackageType.Library, false, false);

        /// <summary>
        /// The file name of the embedding script.
        /// </summary>
        public static readonly string Embedding =
            FormatOps.ScriptTypeToFileName(ScriptTypes.Embedding,
                PackageType.Library, false, false);

        /// <summary>
        /// The file name of the vendor script.
        /// </summary>
        public static readonly string Vendor =
            FormatOps.ScriptTypeToFileName(ScriptTypes.Vendor,
                PackageType.Library, false, false);

        /// <summary>
        /// The file name of the trusted remote script.
        /// </summary>
        public static readonly string TrustedRemote =
            FormatOps.ScriptTypeToFileName(ScriptTypes.TrustedRemote,
                PackageType.Library, false, false);

        /// <summary>
        /// The file name of the startup script.
        /// </summary>
        public static readonly string Startup =
            FormatOps.ScriptTypeToFileName(ScriptTypes.Startup,
                PackageType.Library, false, false);

        /// <summary>
        /// The file name of the worker script.
        /// </summary>
        public static readonly string Worker =
            FormatOps.ScriptTypeToFileName(ScriptTypes.Worker,
                PackageType.Library, false, false);

        /// <summary>
        /// The file name of the safe mode script.
        /// </summary>
        public static readonly string Safe =
            FormatOps.ScriptTypeToFileName(ScriptTypes.Safe,
                PackageType.Library, false, false);

        /// <summary>
        /// The file name of the shell script.
        /// </summary>
        public static readonly string Shell =
            FormatOps.ScriptTypeToFileName(ScriptTypes.Shell,
                PackageType.Library, false, false);

        /// <summary>
        /// The file name of the shell worker script.
        /// </summary>
        public static readonly string ShellWorker =
            FormatOps.ScriptTypeToFileName(ScriptTypes.ShellWorker,
                PackageType.Library, false, false);

        /// <summary>
        /// The file name of the test script.
        /// </summary>
        public static readonly string Test =
            FormatOps.ScriptTypeToFileName(ScriptTypes.Test,
                PackageType.Library, false, false);

        /// <summary>
        /// The file name of the library package index script.
        /// </summary>
        public static readonly string LibraryPackageIndex =
            FormatOps.ScriptTypeToFileName(ScriptTypes.PackageIndex,
                PackageType.Library, false, false);

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// The file name of the test suite "all" script.
        /// </summary>
        public static readonly string All =
            FormatOps.ScriptTypeToFileName(ScriptTypes.All,
                PackageType.Test, false, false);

        /// <summary>
        /// The file name of the test suite constraints script.
        /// </summary>
        public static readonly string Constraints =
            FormatOps.ScriptTypeToFileName(ScriptTypes.Constraints,
                PackageType.Test, false, false);

        /// <summary>
        /// The file name of the test suite epilogue script.
        /// </summary>
        public static readonly string Epilogue =
            FormatOps.ScriptTypeToFileName(ScriptTypes.Epilogue,
                PackageType.Test, false, false);

        /// <summary>
        /// The file name of the test suite prologue script.
        /// </summary>
        public static readonly string Prologue =
            FormatOps.ScriptTypeToFileName(ScriptTypes.Prologue,
                PackageType.Test, false, false);

        /// <summary>
        /// The file name of the test package index script.
        /// </summary>
        public static readonly string TestPackageIndex =
            FormatOps.ScriptTypeToFileName(ScriptTypes.PackageIndex,
                PackageType.Test, false, false);

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// The file name of the kit package index script.
        /// </summary>
        public static readonly string KitPackageIndex =
            FormatOps.ScriptTypeToFileName(ScriptTypes.PackageIndex,
                PackageType.Kit, false, false);
    }

    ///////////////////////////////////////////////////////////////////////////

    /// <summary>
    /// This class contains the file names (without any path) of the well-known
    /// scripts used by the Eagle core library.
    /// </summary>
    [ObjectId("1f6dc165-629e-4cbe-aec2-b26239f244e4")]
    public static class FileNameOnly
    {
        /// <summary>
        /// The file name of the loader script.
        /// </summary>
        public static readonly string Loader =
            FormatOps.ScriptTypeToFileName(ScriptTypes.Loader,
                PackageType.Loader, true, false);

        /// <summary>
        /// The file name of the loader package index script.
        /// </summary>
        public static readonly string LoaderPackageIndex =
            FormatOps.ScriptTypeToFileName(ScriptTypes.PackageIndex,
                PackageType.Loader, true, false);

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// The file name of the generic package index script.
        /// </summary>
        public static readonly string PackageIndex =
            FormatOps.ScriptTypeToFileName(ScriptTypes.PackageIndex,
                PackageType.None, true, false);

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// The file name of the library initialization script.
        /// </summary>
        public static readonly string Initialization =
            FormatOps.ScriptTypeToFileName(ScriptTypes.Initialization,
                PackageType.Library, true, false);

        /// <summary>
        /// The file name of the embedding script.
        /// </summary>
        public static readonly string Embedding =
            FormatOps.ScriptTypeToFileName(ScriptTypes.Embedding,
                PackageType.Library, true, false);

        /// <summary>
        /// The file name of the vendor script.
        /// </summary>
        public static readonly string Vendor =
            FormatOps.ScriptTypeToFileName(ScriptTypes.Vendor,
                PackageType.Library, true, false);

        /// <summary>
        /// The file name of the trusted remote script.
        /// </summary>
        public static readonly string TrustedRemote =
            FormatOps.ScriptTypeToFileName(ScriptTypes.TrustedRemote,
                PackageType.Library, true, false);

        /// <summary>
        /// The file name of the startup script.
        /// </summary>
        public static readonly string Startup =
            FormatOps.ScriptTypeToFileName(ScriptTypes.Startup,
                PackageType.Library, true, false);

        /// <summary>
        /// The file name of the worker script.
        /// </summary>
        public static readonly string Worker =
            FormatOps.ScriptTypeToFileName(ScriptTypes.Worker,
                PackageType.Library, true, false);

        /// <summary>
        /// The file name of the safe mode script.
        /// </summary>
        public static readonly string Safe =
            FormatOps.ScriptTypeToFileName(ScriptTypes.Safe,
                PackageType.Library, true, false);

        /// <summary>
        /// The file name of the shell script.
        /// </summary>
        public static readonly string Shell =
            FormatOps.ScriptTypeToFileName(ScriptTypes.Shell,
                PackageType.Library, true, false);

        /// <summary>
        /// The file name of the shell worker script.
        /// </summary>
        public static readonly string ShellWorker =
            FormatOps.ScriptTypeToFileName(ScriptTypes.ShellWorker,
                PackageType.Library, true, false);

        /// <summary>
        /// The file name of the test script.
        /// </summary>
        public static readonly string Test =
            FormatOps.ScriptTypeToFileName(ScriptTypes.Test,
                PackageType.Library, true, false);

        /// <summary>
        /// The file name of the library package index script.
        /// </summary>
        public static readonly string LibraryPackageIndex =
            FormatOps.ScriptTypeToFileName(ScriptTypes.PackageIndex,
                PackageType.Library, true, false);

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// The file name of the test suite "all" script.
        /// </summary>
        public static readonly string All =
            FormatOps.ScriptTypeToFileName(ScriptTypes.All,
                PackageType.Test, true, false);

        /// <summary>
        /// The file name of the test suite constraints script.
        /// </summary>
        public static readonly string Constraints =
            FormatOps.ScriptTypeToFileName(ScriptTypes.Constraints,
                PackageType.Test, true, false);

        /// <summary>
        /// The file name of the test suite epilogue script.
        /// </summary>
        public static readonly string Epilogue =
            FormatOps.ScriptTypeToFileName(ScriptTypes.Epilogue,
                PackageType.Test, true, false);

        /// <summary>
        /// The file name of the test suite prologue script.
        /// </summary>
        public static readonly string Prologue =
            FormatOps.ScriptTypeToFileName(ScriptTypes.Prologue,
                PackageType.Test, true, false);

        /// <summary>
        /// The file name of the test package index script.
        /// </summary>
        public static readonly string TestPackageIndex =
            FormatOps.ScriptTypeToFileName(ScriptTypes.PackageIndex,
                PackageType.Test, true, false);

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// The file name of the kit package index script.
        /// </summary>
        public static readonly string KitPackageIndex =
            FormatOps.ScriptTypeToFileName(ScriptTypes.PackageIndex,
                PackageType.Kit, true, false);

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// The file name of the tab-separated file that lists the well-known
        /// assembly file plugin names.
        /// </summary>
        internal static readonly string WellKnownAssemblyFilePluginNames =
            "WellKnownAssemblyFilePluginNames.tsv";
    }

    ///////////////////////////////////////////////////////////////////////////

    /// <summary>
    /// This class contains the names of the well-known companion assemblies
    /// associated with the Eagle shell.
    /// </summary>
#if SHELL
    [ObjectId("c5ca371c-bd5a-4215-8f41-fcd812da0a00")]
    public static class _Assembly
    {
        /// <summary>
        /// The name of the Eagle shell assembly.
        /// </summary>
        public static readonly string Shell = GlobalState.GetPackageName(
            PackageType.Default, null, "Shell", false);

        /// <summary>
        /// The name of the Eagle kit assembly.
        /// </summary>
        public static readonly string Kit = GlobalState.GetPackageName(
            PackageType.Default, null, "Kit", false);
    }
#endif

    ///////////////////////////////////////////////////////////////////////////

    /// <summary>
    /// This class contains the MIME content type strings used by the Eagle
    /// core library.
    /// </summary>
    [ObjectId("990c369f-41ee-4f5a-b71e-900d48676b10")]
    public static class ContentType
    {
        /// <summary>
        /// The content type used for plain text.
        /// </summary>
        public static readonly string Text = "text/plain";

        /// <summary>
        /// The candidate content types used to identify a script.
        /// </summary>
        public static readonly string[] Scripts = {
            "text/x-" + GlobalState.GetPackageNameNoCase(),
            "application/x-" + GlobalState.GetPackageNameNoCase(),
            "text/x-script." + GlobalState.GetPackageNameNoCase()
        };

        /// <summary>
        /// The candidate content types used to identify a safe script.
        /// </summary>
        public static readonly string[] SafeScripts = {
            "text/x-safe-" + GlobalState.GetPackageNameNoCase(),
            "application/x-safe-" + GlobalState.GetPackageNameNoCase(),
            "text/x-safe-script." + GlobalState.GetPackageNameNoCase()
        };

        /// <summary>
        /// The primary content type used to identify a script.
        /// </summary>
        public static readonly string Script = Scripts[0];
        /// <summary>
        /// The primary content type used to identify a safe script.
        /// </summary>
        public static readonly string SafeScript = SafeScripts[0];
    }

    ///////////////////////////////////////////////////////////////////////////

    /// <summary>
    /// This class contains the well-known file extensions recognized and used
    /// by the Eagle core library.
    /// </summary>
    [ObjectId("8f0c1c60-0e47-4f19-a3d7-8d6f56348946")]
    public static class FileExtension
    {
        /// <summary>
        /// The file extension used for a script file.
        /// </summary>
        public static readonly string Script = GlobalState.GetPackageName(
            PackageType.Default, Characters.Period.ToString(), null, true);

        /// <summary>
        /// The file extension used for an encrypted script file.
        /// </summary>
        public static readonly string EncryptedScript = GlobalState.GetPackageName(
            PackageType.Default, String.Format("{0}{1}", Characters.Period,
            Characters.e), null, true);

        ///////////////////////////////////////////////////////////////////////

        //
        // HACK: This list of "well-known" file extensions is used just below
        //       by this class.  Any changes made here will need to be double
        //       checked there.
        //
        /// <summary>
        /// The backing array of well-known file extensions, indexed by the
        /// public members declared below.
        /// </summary>
        private static readonly string[] WellKnown = {
            ".args",         /* 00: Arguments */
            ".bat",          /* 01: Batch */
            ".com",          /* 02: Command */
            ".config",       /* 03: Configuration */
            ".dll",          /* 04: Library */
            ".exe",          /* 05: Executable */
            ".harpy",        /* 06: Signature */
            ".ico",          /* 07: Icon */
            ".ini",          /* 08: Profile */
            ".pdb",          /* 09: Symbols */
            ".pvk",          /* 10: PrivateKey (RSA) */
            ".snk",          /* 11: StrongNameKey (RSA) */
            ".txt",          /* 12: Text */
            ".xml",          /* 13: Markup */
            ".so",           /* 14: SharedObject */
            ".dylib",        /* 15: DynamicLibrary */
            ".sl",           /* 16: SharedLibrary */
            ".db",           /* 17: Database */
            ".tmp",          /* 18: Temporary */
            ".log",          /* 19: Log */
            ".dsasnk",       /* 20: DsaStrongNameKey */
            ".dsapvk",       /* 21: DsaPrivateKey */
            ".b64sig",       /* 22: Base64Signature */
            ".exml",         /* 23: EncryptedMarkup */
            Script,          /* 24: Script */
            EncryptedScript, /* 25: EncryptedScript */
            ".bak",          /* 26: Backup */
            ".settings",     /* 27: Settings */
            ".noPkgIndex",   /* 28: NoPkgIndex */
            ".ruleSet",      /* 29: RuleSet */
            ".*"             /* 30: Any */
        };

        ///////////////////////////////////////////////////////////////////////

        //
        // HACK: This is really a dictionary with a name suffix of "List".
        //       The rationale behind that is that it is logically a list
        //       of "well-known" file extensions that is contained inside
        //       of a physical dictionary for the sole purpose of making
        //       lookups faster.
        //
        /// <summary>
        /// The lookup dictionary of well-known file extensions, used for fast
        /// membership testing.
        /// </summary>
        internal static readonly PathDictionary<object> WellKnownList =
            new PathDictionary<object>(WellKnown);

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// The file extension used for an arguments file.
        /// </summary>
        public static readonly string Arguments = WellKnown[0];
        /// <summary>
        /// The file extension used for a batch file.
        /// </summary>
        public static readonly string Batch = WellKnown[1];
        /// <summary>
        /// The file extension used for a command file.
        /// </summary>
        public static readonly string Command = WellKnown[2];
        /// <summary>
        /// The file extension used for a configuration file.
        /// </summary>
        public static readonly string Configuration = WellKnown[3];
        /// <summary>
        /// The file extension used for a managed (or native) library file.
        /// </summary>
        public static readonly string Library = WellKnown[4];
        /// <summary>
        /// The file extension used for an executable file.
        /// </summary>
        public static readonly string Executable = WellKnown[5];
        /// <summary>
        /// The file extension used for a script signature file.
        /// </summary>
        public static readonly string Signature = WellKnown[6];
        /// <summary>
        /// The file extension used for an icon file.
        /// </summary>
        public static readonly string Icon = WellKnown[7];
        /// <summary>
        /// The file extension used for a profile file.
        /// </summary>
        public static readonly string Profile = WellKnown[8];
        /// <summary>
        /// The file extension used for a debugging symbols file.
        /// </summary>
        public static readonly string Symbols = WellKnown[9];
        /// <summary>
        /// The file extension used for an RSA private key file.
        /// </summary>
        public static readonly string PrivateKey = WellKnown[10];
        /// <summary>
        /// The file extension used for an RSA strong name key file.
        /// </summary>
        public static readonly string StrongNameKey = WellKnown[11];
        /// <summary>
        /// The file extension used for a text file.
        /// </summary>
        public static readonly string Text = WellKnown[12];
        /// <summary>
        /// The file extension used for an XML markup file.
        /// </summary>
        public static readonly string Markup = WellKnown[13];
        /// <summary>
        /// The file extension used for a (Unix) shared object file.
        /// </summary>
        public static readonly string SharedObject = WellKnown[14];
        /// <summary>
        /// The file extension used for a (macOS) dynamic library file.
        /// </summary>
        public static readonly string DynamicLibrary = WellKnown[15];
        /// <summary>
        /// The file extension used for a (HP-UX) shared library file.
        /// </summary>
        public static readonly string SharedLibrary = WellKnown[16];
        /// <summary>
        /// The file extension used for a database file.
        /// </summary>
        public static readonly string Database = WellKnown[17];
        /// <summary>
        /// The file extension used for a temporary file.
        /// </summary>
        public static readonly string Temporary = WellKnown[18];
        /// <summary>
        /// The file extension used for a log file.
        /// </summary>
        public static readonly string Log = WellKnown[19];
        /// <summary>
        /// The file extension used for a DSA strong name key file.
        /// </summary>
        public static readonly string DsaStrongNameKey = WellKnown[20];
        /// <summary>
        /// The file extension used for a DSA private key file.
        /// </summary>
        public static readonly string DsaPrivateKey = WellKnown[21];
        /// <summary>
        /// The file extension used for a base64-encoded signature file.
        /// </summary>
        public static readonly string Base64Signature = WellKnown[22];
        /// <summary>
        /// The file extension used for an encrypted XML markup file.
        /// </summary>
        public static readonly string EncryptedMarkup = WellKnown[23];
        /// <summary>
        /// The file extension used for a backup file.
        /// </summary>
        public static readonly string Backup = WellKnown[26];
        /// <summary>
        /// The file extension used for a settings file.
        /// </summary>
        public static readonly string Settings = WellKnown[27];
        /// <summary>
        /// The file extension used to indicate that no package index is
        /// present.
        /// </summary>
        public static readonly string NoPkgIndex = WellKnown[28];
        /// <summary>
        /// The file extension used for a rule set file.
        /// </summary>
        public static readonly string RuleSet = WellKnown[29];
        /// <summary>
        /// The file extension wildcard matching any extension.
        /// </summary>
        public static readonly string Any = WellKnown[30];
    }

    ///////////////////////////////////////////////////////////////////////////

    /// <summary>
    /// This class contains the well-known names of the named colors used by
    /// the interactive host.
    /// </summary>
    [ObjectId("a263fc47-65e8-4b95-979b-b47b53629df9")]
    public static class ColorName
    {
        /// <summary>
        /// The name of the color used for the banner.
        /// </summary>
        public static readonly string Banner = "Banner";
        /// <summary>
        /// The name of the default color.
        /// </summary>
        public static readonly string Default = "Default";
        /// <summary>
        /// The name of the color used for help text.
        /// </summary>
        public static readonly string Help = "Help";
        /// <summary>
        /// The name of the color used for a help item.
        /// </summary>
        public static readonly string HelpItem = "HelpItem";
        /// <summary>
        /// The name of the color used for legal notices.
        /// </summary>
        public static readonly string Legal = "Legal";
        /// <summary>
        /// The name of the color used for official items.
        /// </summary>
        public static readonly string Official = "Official";
        /// <summary>
        /// The name of the color used for unofficial items.
        /// </summary>
        public static readonly string Unofficial = "Unofficial";
        /// <summary>
        /// The name of the color used for trusted items.
        /// </summary>
        public static readonly string Trusted = "Trusted";
        /// <summary>
        /// The name of the color used for untrusted items.
        /// </summary>
        public static readonly string Untrusted = "Untrusted";
        /// <summary>
        /// The name of the color used for stable items.
        /// </summary>
        public static readonly string Stable = "Stable";
        /// <summary>
        /// The name of the color used for unstable items.
        /// </summary>
        public static readonly string Unstable = "Unstable";
        /// <summary>
        /// The name of the color used for enabled items.
        /// </summary>
        public static readonly string Enabled = "Enabled";
        /// <summary>
        /// The name of the color used for disabled items.
        /// </summary>
        public static readonly string Disabled = "Disabled";
        /// <summary>
        /// The name of the color used for undefined items.
        /// </summary>
        public static readonly string Undefined = "Undefined";
    }

    ///////////////////////////////////////////////////////////////////////////

    /// <summary>
    /// This class contains the well-known data field names used for
    /// compatibility with the Harpy security package.
    /// </summary>
    [ObjectId("78e90182-cf54-4e3d-9a70-b2c2e4095024")]
    public static class DataNames
    {
        /// <summary>
        /// The name of the "as dictionary" data field.
        /// </summary>
        public static readonly string AsDictionary = "AsDictionary"; // COMPAT: Harpy.
        /// <summary>
        /// The name of the legacy identifier data field.
        /// </summary>
        public static readonly string Id = "Id"; // COMPAT: Harpy (legacy).
    }

    ///////////////////////////////////////////////////////////////////////////

    /// <summary>
    /// This class contains the names of the standard input, output, and error
    /// channels.
    /// </summary>
    [ObjectId("3ea126e0-52f7-4b34-8e80-d428da5c9d8b")]
    internal static class StandardChannel
    {
        /// <summary>
        /// The name of the standard input channel.
        /// </summary>
        public static readonly string Input = Channel.StdIn;
        /// <summary>
        /// The name of the standard output channel.
        /// </summary>
        public static readonly string Output = Channel.StdOut;
        /// <summary>
        /// The name of the standard error channel.
        /// </summary>
        public static readonly string Error = Channel.StdErr;
    }

    ///////////////////////////////////////////////////////////////////////////

    /// <summary>
    /// This class contains the names of the well-known build configurations.
    /// </summary>
    [ObjectId("8e21d7ed-fd63-4ea5-a698-740601e2d188")]
    internal static class BuildConfiguration
    {
        /// <summary>
        /// The name of the debug build configuration.
        /// </summary>
        public const string Debug = "Debug";
        /// <summary>
        /// The name of the release build configuration.
        /// </summary>
        public const string Release = "Release";
    }

    ///////////////////////////////////////////////////////////////////////////

    /// <summary>
    /// This class contains the well-known command line argument values.
    /// </summary>
#if SHELL
    [ObjectId("99951710-f239-43e3-9cea-92fdc4d1d18b")]
    internal static class CommandLineArgument
    {
        /// <summary>
        /// The argument value indicating that input should be read from
        /// standard input.
        /// </summary>
        public static readonly string StandardInput = "-";
    }

    ///////////////////////////////////////////////////////////////////////////

    /// <summary>
    /// This class contains the names of the command line options recognized by
    /// the Eagle shell.
    /// </summary>
    [ObjectId("656078c8-4735-42ca-a6fe-55eb72502a17")]
    internal static class CommandLineOption
    {
        /// <summary>
        /// The option permitting any file to be used.
        /// </summary>
        public static readonly string AnyFile = "anyFile";

#if !ENTERPRISE_LOCKDOWN
        /// <summary>
        /// The option permitting any initialization to be used.
        /// </summary>
        public static readonly string AnyInitialize = "anyInitialize";
#endif

        /// <summary>
        /// The option specifying additional interpreter arguments.
        /// </summary>
        public static readonly string Arguments = "arguments";
        /// <summary>
        /// The option requesting a break into the debugger.
        /// </summary>
        public static readonly string Break = "break";
        /// <summary>
        /// The option enabling debug mode.
        /// </summary>
        public static readonly string Debug = "debug";
        /// <summary>
        /// The option specifying the text encoding to use.
        /// </summary>
        public static readonly string Encoding = "encoding";

#if !ENTERPRISE_LOCKDOWN
        /// <summary>
        /// The option specifying a script to evaluate.
        /// </summary>
        public static readonly string Evaluate = "evaluate";
        /// <summary>
        /// The option specifying an encoded script to evaluate.
        /// </summary>
        public static readonly string EvaluateEncoded = "evaluateEncoded";
#endif

        /// <summary>
        /// The option enabling quiet mode.
        /// </summary>
        public static readonly string Quiet = "quiet";
        /// <summary>
        /// The option specifying a script file to evaluate.
        /// </summary>
        public static readonly string File = "file";
        /// <summary>
        /// The option requesting help.
        /// </summary>
        public static readonly string Help = "help";
        /// <summary>
        /// The option requesting the "about" information.
        /// </summary>
        public static readonly string About = "?";
        /// <summary>
        /// The option requesting command help.
        /// </summary>
        public static readonly string CommandHelp = "??";
        /// <summary>
        /// The option requesting environment help.
        /// </summary>
        public static readonly string EnvironmentHelp = "???";
        /// <summary>
        /// The option forcing script library initialization.
        /// </summary>
        public static readonly string ForceInitialize = "forceInitialize";
        /// <summary>
        /// The option requesting full help.
        /// </summary>
        public static readonly string FullHelp = "????";
        /// <summary>
        /// The option enabling script library initialization.
        /// </summary>
        public static readonly string Initialize = "initialize";
        /// <summary>
        /// The option enabling interactive mode.
        /// </summary>
        public static readonly string Interactive = "interactive";

#if ISOLATED_PLUGINS
        /// <summary>
        /// The option enabling plugin isolation.
        /// </summary>
        public static readonly string Isolated = "isolated";
#endif

        /// <summary>
        /// The option enabling kiosk mode.
        /// </summary>
        public static readonly string Kiosk = "kiosk";
        /// <summary>
        /// The option locking the interpreter to the host arguments.
        /// </summary>
        public static readonly string LockHostArguments = "lockHostArguments";
        /// <summary>
        /// The option enabling namespace support.
        /// </summary>
        public static readonly string Namespaces = "namespaces";
        /// <summary>
        /// The option disabling processing of arguments from file names.
        /// </summary>
        public static readonly string NoArgumentsFileNames = "noArgumentsFileNames";
        /// <summary>
        /// The option disabling processing of application settings.
        /// </summary>
        public static readonly string NoAppSettings = "noAppSettings";
        /// <summary>
        /// The option preventing the shell from exiting.
        /// </summary>
        public static readonly string NoExit = "noExit";
        /// <summary>
        /// The option disabling trimming of subsequent arguments.
        /// </summary>
        public static readonly string NoTrim = "noTrim";
        /// <summary>
        /// The option clearing the trace listeners.
        /// </summary>
        public static readonly string ClearTrace = "clearTrace";
        /// <summary>
        /// The option pausing the shell.
        /// </summary>
        public static readonly string Pause = "pause";
        /// <summary>
        /// The option specifying plugin arguments.
        /// </summary>
        public static readonly string PluginArguments = "pluginArguments";
        /// <summary>
        /// The option specifying a script file to evaluate after the main
        /// file.
        /// </summary>
        public static readonly string PostFile = "postFile";

#if !ENTERPRISE_LOCKDOWN
        /// <summary>
        /// The option specifying a script to evaluate after initialization.
        /// </summary>
        public static readonly string PostInitialize = "postInitialize";
#endif

        /// <summary>
        /// The option specifying a script file to evaluate before the main
        /// file.
        /// </summary>
        public static readonly string PreFile = "preFile";

#if !ENTERPRISE_LOCKDOWN
        /// <summary>
        /// The option specifying a script to evaluate before initialization.
        /// </summary>
        public static readonly string PreInitialize = "preInitialize";
#endif

        /// <summary>
        /// The option specifying the host profile to use.
        /// </summary>
        public static readonly string Profile = "profile";
        /// <summary>
        /// The option reconfiguring the interpreter.
        /// </summary>
        public static readonly string Reconfigure = "reconfigure";
        /// <summary>
        /// The option recreating the interpreter.
        /// </summary>
        public static readonly string Recreate = "recreate";
        /// <summary>
        /// The option setting a runtime option.
        /// </summary>
        public static readonly string RuntimeOption = "runtimeOption";
        /// <summary>
        /// The option enabling safe mode.
        /// </summary>
        public static readonly string Safe = "safe";

#if TEST
        /// <summary>
        /// The option adding a script trace listener.
        /// </summary>
        public static readonly string ScriptTrace = "scriptTrace";
#endif

        /// <summary>
        /// The option configuring script signing security.
        /// </summary>
        public static readonly string Security = "security";
        /// <summary>
        /// The option setting whether the interactive loop is entered.
        /// </summary>
        public static readonly string SetLoop = "setLoop";
        /// <summary>
        /// The option setting whether script library initialization occurs.
        /// </summary>
        public static readonly string SetInitialize = "setInitialize";
        /// <summary>
        /// The option enabling standard mode.
        /// </summary>
        public static readonly string Standard = "standard";
        /// <summary>
        /// The option specifying the startup script library path.
        /// </summary>
        public static readonly string StartupLibrary = "startupLibrary";

#if TEST
        /// <summary>
        /// The option specifying the startup log file.
        /// </summary>
        public static readonly string StartupLogFile = "startupLogFile";
#endif

#if !ENTERPRISE_LOCKDOWN
        /// <summary>
        /// The option specifying a startup pre-initialization script.
        /// </summary>
        public static readonly string StartupPreInitialize = "startupPreInitialize";
#endif

        /// <summary>
        /// The option enabling single-step debugging.
        /// </summary>
        public static readonly string Step = "step";
        /// <summary>
        /// The option stopping argument processing after an unknown argument.
        /// </summary>
        public static readonly string StopOnUnknown = "stopOnUnknown";
        /// <summary>
        /// The option enabling the test suite.
        /// </summary>
        public static readonly string Test = "test";
        /// <summary>
        /// The option enabling the plugin test suite.
        /// </summary>
        public static readonly string PluginTest = "pluginTest";
        /// <summary>
        /// The option specifying the test directory.
        /// </summary>
        public static readonly string TestDirectory = "testDirectory";
        /// <summary>
        /// The option enabling setup tracing.
        /// </summary>
        public static readonly string SetupTrace = "setupTrace";
        /// <summary>
        /// The option enabling tracing to the host.
        /// </summary>
        public static readonly string TraceToHost = "traceToHost";
        /// <summary>
        /// The option specifying the vendor path.
        /// </summary>
        public static readonly string VendorPath = "vendorPath";
        /// <summary>
        /// The option requesting the version.
        /// </summary>
        public static readonly string Version = "version";

        /// <summary>
        /// The option setting whether the interpreter is created.
        /// </summary>
        public static readonly string SetCreate = "setCreate";
        /// <summary>
        /// The option selecting the child interpreter.
        /// </summary>
        public static readonly string Child = "child";
        /// <summary>
        /// The option selecting the parent interpreter.
        /// </summary>
        public static readonly string Parent = "parent";
    }
#endif

    ///////////////////////////////////////////////////////////////////////////

    /// <summary>
    /// This class contains the (format) strings used to display status and
    /// diagnostic messages while the Eagle shell is processing its command
    /// line arguments and configuration.
    /// </summary>
    [ObjectId("6475066f-5f77-49c0-bcc0-177abe8cf24a")]
    public static class Prompt
    {
        /// <summary>
        /// The message reporting the namespace support state.
        /// </summary>
        public static readonly string Namespaces =
            "Namespace support {0}.";

#if ARGUMENT_CACHE || LIST_CACHE || PARSE_CACHE || TYPE_CACHE || COM_TYPE_CACHE
        /// <summary>
        /// The message reporting that the cache level was overridden.
        /// </summary>
        public static readonly string BumpCacheLevel =
            "Cache level overridden via configuration: {0}.";

        /// <summary>
        /// The message reporting that the cache flags were overridden.
        /// </summary>
        public static readonly string CacheFlags =
            "Cache flags overridden via configuration: {0}.";
#endif

        /// <summary>
        /// The message reporting that user interactivity was overridden.
        /// </summary>
        public static readonly string UserInteractive =
            "User interactivity overridden via environment: {0}.";

#if ISOLATED_PLUGINS
        /// <summary>
        /// The message reporting the plugin isolation state.
        /// </summary>
        public static readonly string Isolated = "Plugin isolation {0}{1}.";
#endif

        /// <summary>
        /// The message reporting that result strings will include managed call
        /// stacks.
        /// </summary>
        public static readonly string IncludeResultStack =
            "Result strings will include managed call stacks.";

        /// <summary>
        /// The message reporting that result objects will populate managed
        /// call stacks.
        /// </summary>
        public static readonly string PopulateResultStack =
            "Result objects will populate managed call stacks.";

        /// <summary>
        /// The message reporting the script signing security state.
        /// </summary>
        public static readonly string Security =
            "Script signing policies and core script certificates {0}{1}.";

        /// <summary>
        /// The message reporting that the interpreter is locked to the host
        /// arguments.
        /// </summary>
        public static readonly string LockHostArguments =
            "Locked to host arguments.";

        /// <summary>
        /// The message reporting that application settings processing was
        /// skipped.
        /// </summary>
        public static readonly string NoAppSettings =
            "Arguments from application settings processing skipped{0}.";

#if CONFIGURATION
        /// <summary>
        /// The message reporting that application settings will be forcibly
        /// refreshed.
        /// </summary>
        public static readonly string RefreshAppSettings =
            "Application settings will be forcibly refreshed.";
#endif

#if XML
        /// <summary>
        /// The message reporting that application settings from XML files will
        /// be favored.
        /// </summary>
        public static readonly string UseXmlFiles =
            "Application settings from XML files will be favored.";

        /// <summary>
        /// The message reporting that application settings from XML files will
        /// be merged.
        /// </summary>
        public static readonly string MergeXmlAppSettings =
            "Application settings from XML files will be merged.";

        /// <summary>
        /// The message reporting that application settings from all sources
        /// will be merged.
        /// </summary>
        public static readonly string MergeAllAppSettings =
            "Application settings from all sources will be merged.";
#endif

        /// <summary>
        /// The message reporting that processing of arguments from files was
        /// skipped.
        /// </summary>
        public static readonly string NoArgumentsFileNames =
            "Arguments from file(s) processing skipped.";

        /// <summary>
        /// The message reporting that the interactive loop will be entered.
        /// </summary>
        public static readonly string NoExit =
            "Interactive loop will be entered.";

        /// <summary>
        /// The message reporting that whitespace will not be trimmed from
        /// subsequent arguments.
        /// </summary>
        public static readonly string NoTrim =
            "Whitespace will not be trimmed from subsequent arguments.";

        /// <summary>
        /// The message reporting that breaking into the debugger is disabled.
        /// </summary>
        public static readonly string NoBreakNotify =
            "Breaking into debugger in process {0} is disabled.";

        /// <summary>
        /// The message reporting that breaking into the debugger is disabled,
        /// prompting for a key press.
        /// </summary>
        public static readonly string NoBreak =
            "Breaking into debugger in process {0} is disabled, press any key to continue.";

        /// <summary>
        /// The message prompting the user to attach a debugger.
        /// </summary>
        public static readonly string Debugger =
            "Attach a debugger to process {0} and press any key to continue.";

        /// <summary>
        /// The message reporting that interactive mode is enabled.
        /// </summary>
        public static readonly string Interactive =
            "Interactive mode enabled{0}.";

        /// <summary>
        /// The message reporting that arguments were added for a plugin.
        /// </summary>
        public static readonly string PluginArguments =
            "Arguments added for plugin: {0}.";

#if TEST
        /// <summary>
        /// The message reporting that the log file was set up.
        /// </summary>
        public static readonly string LogFile =
            "Log file was setup.";
#endif

        /// <summary>
        /// The message reporting that the script library path was overridden.
        /// </summary>
        public static readonly string LibraryPath =
            "Script library path was overridden.";

        /// <summary>
        /// The message reporting that the pre-initialize script text was
        /// overridden.
        /// </summary>
        public static readonly string PreInitializeText =
            "Pre-initialize script text was overridden.";

        /// <summary>
        /// The message reporting that script debugger single-step mode is
        /// enabled.
        /// </summary>
        public static readonly string SingleStep =
            "Script debugger single-step enabled{0}.";

#if !DEBUGGER
        /// <summary>
        /// The message reporting that the script debugger is not available.
        /// </summary>
        public static readonly string NoDebugger =
            "Script debugger not available{0}.";
#endif

        /// <summary>
        /// The message reporting that a runtime option was changed.
        /// </summary>
        public static readonly string RuntimeOption = "Runtime option changed: {0}.";

        /// <summary>
        /// The message reporting that an interpreter was recreated.
        /// </summary>
        public static readonly string Recreate = "{0} interpreter {1} was recreated.";

        /// <summary>
        /// The message reporting that an interpreter was reconfigured.
        /// </summary>
        public static readonly string Reconfigure = "{0} interpreter {1} was reconfigured: {2}.";

        /// <summary>
        /// The message reporting that the interpreter creation flags were
        /// overridden.
        /// </summary>
        public static readonly string CreateFlags =
            "Interpreter creation flags overridden via configuration: {0}.";

        /// <summary>
        /// The message reporting that the interpreter host creation flags were
        /// overridden.
        /// </summary>
        public static readonly string HostCreateFlags =
            "Interpreter host creation flags overridden via configuration: {0}.";

        /// <summary>
        /// The message reporting that the interpreter instance flags were
        /// overridden.
        /// </summary>
        public static readonly string InterpreterFlags =
            "Interpreter instance flags overridden via configuration: {0}.";

        /// <summary>
        /// The message reporting that the interpreter initialize flags were
        /// overridden.
        /// </summary>
        public static readonly string InitializeFlags =
            "Interpreter initialize flags overridden via configuration: {0}.";

        /// <summary>
        /// The message reporting that the interpreter script flags were
        /// overridden.
        /// </summary>
        public static readonly string ScriptFlags =
            "Interpreter script flags overridden via configuration: {0}.";

        /// <summary>
        /// The message reporting that the console is enabled.
        /// </summary>
        public static readonly string Console =
            "Console enabled.";

        /// <summary>
        /// The message reporting that the console is disabled.
        /// </summary>
        public static readonly string NoConsole =
            "Console disabled.";

        /// <summary>
        /// The message reporting that debug mode is enabled.
        /// </summary>
        public static readonly string Debug =
            "Debug mode enabled{0}.";

        /// <summary>
        /// The message reporting that time measurements are enabled.
        /// </summary>
        public static readonly string MeasureTime =
            "Time measurements enabled.";

        // public static readonly string Verbose =
        //     "Verbose mode enabled.";

        /// <summary>
        /// The message reporting that creating and/or opening "setup" mutexes
        /// is disabled.
        /// </summary>
        public static readonly string NoMutexes =
            "Creating and/or opening \"setup\" mutexes disabled.";

        /// <summary>
        /// The message reporting that tracing is disabled.
        /// </summary>
        public static readonly string NoTrace =
            "Tracing disabled.";

        /// <summary>
        /// The message reporting that tracing limits are disabled.
        /// </summary>
        public static readonly string NoTraceLimits =
            "Tracing limits disabled.";

        /// <summary>
        /// The message reporting that the tracing subsystem is disabled.
        /// </summary>
        public static readonly string NoTraceOps =
            "Tracing subsystem disabled.";

        /// <summary>
        /// The message reporting that debug tracing is disabled.
        /// </summary>
        public static readonly string NoDebugTrace =
            "Debug tracing disabled.";

        /// <summary>
        /// The message reporting that tracing is enabled for something.
        /// </summary>
        public static readonly string Trace =
            "Tracing enabled for {0}.";

        /// <summary>
        /// The message reporting that the tracing subsystem is enabled.
        /// </summary>
        public static readonly string TraceOps =
            "Tracing subsystem enabled.";

        /// <summary>
        /// The message reporting that executable file certificate trust
        /// checking is disabled.
        /// </summary>
        public static readonly string NoTrusted =
            "Executable file certificate trust checking disabled.";

        /// <summary>
        /// The message reporting that assembly strong name signature
        /// verification is disabled.
        /// </summary>
        public static readonly string NoVerified =
            "Assembly strong name signature verification disabled.";

        /// <summary>
        /// The message reporting the quiet mode state.
        /// </summary>
        public static readonly string Quiet =
            "Quiet mode {0}{1}.";

#if NATIVE && WINDOWS
        /// <summary>
        /// The message reporting that native console integration is disabled.
        /// </summary>
        public static readonly string NoNativeConsole =
            "Native console integration disabled.";
#endif

#if NATIVE
        /// <summary>
        /// The message reporting that native stack checking will be disabled.
        /// </summary>
        public static readonly string NoNativeStack =
            "Native stack checking will be disabled.";
#endif

        /// <summary>
        /// The message reporting that the interpreter script data flags were
        /// overridden.
        /// </summary>
        public static readonly string DataFlags =
            "Interpreter script data flags overridden via configuration: {0}.";

        /// <summary>
        /// The message reporting that plugins will be allowed to load on any
        /// thread.
        /// </summary>
        public static readonly string AllowAnyThread =
            "Plugins will be allowed to load on any thread.";

        /// <summary>
        /// The message reporting that token interpreter creation cannot be
        /// temporarily disabled.
        /// </summary>
        public static readonly string CreateFailSafe =
            "Token interpreter creation cannot be temporarily disabled.";

        /// <summary>
        /// The message reporting that trusted hashes will not be ignored.
        /// </summary>
        public static readonly string ForceTrustedHashes =
            "Trusted hashes will not be ignored.";

        /// <summary>
        /// The message reporting that configuration prompts will not be
        /// written to the console.
        /// </summary>
        public static readonly string NoWritePrompt =
            "Configuration prompts will not be written to the console.";

        /// <summary>
        /// The message reporting that trusted hashes are disabled.
        /// </summary>
        public static readonly string NoTrustedHashes =
            "Trusted hashes disabled.";

        /// <summary>
        /// The message reporting that strict base path checking will be used.
        /// </summary>
        public static readonly string StrictBasePath =
            "Strict base path checking will be used.";

        /// <summary>
        /// The message reporting that the internal wrapper class will be used
        /// for named events.
        /// </summary>
        public static readonly string UseNamedEvents =
            "Internal wrapper class will be used for named events.";

#if NETWORK && OFFICIAL_BINARY && !ENTERPRISE_LOCKDOWN
        /// <summary>
        /// The message reporting that trusted remote script library
        /// initialization will be skipped.
        /// </summary>
        public static readonly string NoTrustedRemote =
            "Trusted remote script library initialization will be skipped.";

        /// <summary>
        /// The message reporting that trusted remote script library
        /// initialization will be forcibly enabled.
        /// </summary>
        public static readonly string ForceTrustedRemote =
            "Trusted remote script library initialization will be forcibly enabled.";

        /// <summary>
        /// The message reporting that a trusted remote script bundle password
        /// is configured.
        /// </summary>
        public static readonly string TrustedBundlePassword =
            "Trusted remote script bundle password is configured.";
#endif

        /// <summary>
        /// The message reporting that extra operating system information will
        /// not be populated.
        /// </summary>
        public static readonly string NoPopulateOsExtra =
            "Extra operating system information will not be populated.";

        /// <summary>
        /// The message reporting that default quiet mode is enabled.
        /// </summary>
        public static readonly string DefaultQuiet =
            "Default quiet mode enabled.";

        /// <summary>
        /// The message reporting that default tracing of the managed call
        /// stack is enabled.
        /// </summary>
        public static readonly string DefaultTraceStack =
            "Default tracing of managed call stack enabled.";

        /// <summary>
        /// The message reporting that selected diagnostic messages are
        /// disabled.
        /// </summary>
        public static readonly string NoVerbose =
            "Selected diagnostic messages are disabled.";

#if THREADING
        /// <summary>
        /// The message reporting that worker threads are disabled.
        /// </summary>
        public static readonly string NoWorkers =
            "Worker threads disabled.";
#endif

        /// <summary>
        /// The message reporting that selected diagnostic messages are
        /// enabled.
        /// </summary>
        public static readonly string Verbose =
            "Selected diagnostic messages are enabled.";

        /// <summary>
        /// The message reporting that the utility path was overridden.
        /// </summary>
        public static readonly string UtilityPath =
            "Utility path overridden via configuration: {0}.";

        /// <summary>
        /// The message reporting that the vendor path was overridden.
        /// </summary>
        public static readonly string VendorPath =
            "Vendor path overridden via configuration: {0}.";

        /// <summary>
        /// The message reporting that the ellipsis limit was overridden.
        /// </summary>
        public static readonly string EllipsisLimit =
            "Ellipsis limit overridden via configuration: {0}.";

        /// <summary>
        /// The message reporting that re-throwing of unhandled exceptions is
        /// enabled.
        /// </summary>
        public static readonly string Throw =
            "Re-throwing of unhandled exceptions enabled.";

        /// <summary>
        /// The message reporting that internal calls to collect garbage are
        /// always disabled.
        /// </summary>
        public static readonly string NeverGC =
            "Internal calls to collect garbage always disabled.";

        /// <summary>
        /// The message reporting that internal waits for pending finalizers
        /// are always disabled.
        /// </summary>
        public static readonly string NeverWaitForGC =
            "Internal waits for pending finalizers always disabled.";

        /// <summary>
        /// The message reporting that internal waits for pending finalizers
        /// are always enabled.
        /// </summary>
        public static readonly string AlwaysWaitForGC =
            "Internal waits for pending finalizers always enabled.";

        /// <summary>
        /// The message reporting that the runtime will be treated as though it
        /// were .NET Core.
        /// </summary>
        public static readonly string TreatAsDotNetCore =
            "Will attempt to treat runtime as though it were .NET Core.";

        /// <summary>
        /// The message reporting that the runtime will be treated as though it
        /// were Mono.
        /// </summary>
        public static readonly string TreatAsMono =
            "Will attempt to treat runtime as though it were Mono.";

        /// <summary>
        /// The message reporting that the runtime will be treated as though it
        /// were .NET Framework 2.0.
        /// </summary>
        public static readonly string TreatAsFramework20 =
            "Will attempt to treat runtime as though it were .NET Framework 2.0.";

        /// <summary>
        /// The message reporting that the runtime will be treated as though it
        /// were .NET Framework 4.0.
        /// </summary>
        public static readonly string TreatAsFramework40 =
            "Will attempt to treat runtime as though it were .NET Framework 4.0.";

        /// <summary>
        /// The message reporting that the default trace categories were
        /// overridden.
        /// </summary>
        public static readonly string DefaultTraceCategories =
            "Default {0} trace categories overridden via environment: {1}.";

        /// <summary>
        /// The message reporting that the default trace categories could not
        /// be overridden.
        /// </summary>
        public static readonly string DefaultTraceCategoriesError =
            "Default {0} trace categories could not be overridden via environment: {1}.";

        /// <summary>
        /// The message reporting that the default trace format was overridden.
        /// </summary>
        public static readonly string DefaultTraceFormat =
            "Default trace format overridden via environment: {0}.";

        /// <summary>
        /// The message reporting that the default trace format could not be
        /// overridden.
        /// </summary>
        public static readonly string DefaultTraceFormatError =
            "Default trace format could not be overridden via environment: {0}.";

        /// <summary>
        /// The message reporting that the default trace priority mask was
        /// overridden.
        /// </summary>
        public static readonly string DefaultTracePriorities =
            "Default trace priority mask overridden via environment: {0}.";

        /// <summary>
        /// The message reporting that the default trace priority flags were
        /// overridden.
        /// </summary>
        public static readonly string DefaultGlobalPriorities =
            "Default trace priority flags overridden via environment: {0}.";

        /// <summary>
        /// The message reporting that the default trace priority mask could
        /// not be overridden.
        /// </summary>
        public static readonly string DefaultTracePrioritiesError =
            "Default trace priority mask could not be overridden via environment: {0}.";

        /// <summary>
        /// The message reporting that the default trace priority flags could
        /// not be overridden.
        /// </summary>
        public static readonly string DefaultGlobalPrioritiesError =
            "Default trace priority flags could not be overridden via environment: {0}.";

        /// <summary>
        /// The message reporting that the default trace priority was
        /// overridden.
        /// </summary>
        public static readonly string DefaultTracePriority =
            "Default trace priority overridden via environment: {0}.";

        /// <summary>
        /// The message reporting that the default trace priority could not be
        /// overridden.
        /// </summary>
        public static readonly string DefaultTracePriorityError =
            "Default trace priority could not be overridden via environment: {0}.";

        /// <summary>
        /// The message reporting that the trace categories were overridden.
        /// </summary>
        public static readonly string TraceCategories =
            "{0} trace categories overridden via configuration: {1}.";

        /// <summary>
        /// The message reporting that the trace priority mask was overridden.
        /// </summary>
        public static readonly string TracePriorities =
            "Trace priority mask overridden via configuration: {0}.";

        /// <summary>
        /// The message reporting that the trace priority flags were
        /// overridden.
        /// </summary>
        public static readonly string GlobalPriorities =
            "Trace priority flags overridden via configuration: {0}.";

        /// <summary>
        /// The message reporting that the trace priority was overridden.
        /// </summary>
        public static readonly string TracePriority =
            "Trace priority overridden via configuration: {0}.";

        /// <summary>
        /// The message reporting that tracing to the host is enabled.
        /// </summary>
        public static readonly string TraceToHost =
            "Tracing to host enabled{0}.";

        /// <summary>
        /// The message reporting that tracing cannot be enabled.
        /// </summary>
        public static readonly string TraceError =
            "Tracing cannot be enabled: {0}.";

#if TEST
        /// <summary>
        /// The message reporting that a script trace listener was added.
        /// </summary>
        public static readonly string ScriptTrace =
            "Script trace listener added{0}.";
#endif

        /// <summary>
        /// The message reporting that debug tracing is enabled.
        /// </summary>
        public static readonly string DebugTrace =
            "Debug tracing enabled.";

        /// <summary>
        /// The message reporting that debug tracing cannot be enabled.
        /// </summary>
        public static readonly string DebugTraceError =
            "Debug tracing cannot be enabled: {0}.";

        /// <summary>
        /// The message reporting that script library initialization will be
        /// forced.
        /// </summary>
        public static readonly string ForceInitialize =
            "Script library initialization will be forced.";

        /// <summary>
        /// The message reporting that the most modern cryptographic algorithms
        /// will be used wherever applicable.
        /// </summary>
        public static readonly string ForceModernAlgorithms =
            "The most modern cryptographic algorithms will be used wherever applicable.";

        /// <summary>
        /// The message reporting that script library initialization will be
        /// enabled.
        /// </summary>
        public static readonly string Initialize =
            "Script library initialization will be enabled.";

        /// <summary>
        /// The message reporting that script library initialization will be
        /// skipped.
        /// </summary>
        public static readonly string NoInitialize =
            "Script library initialization will be skipped{0}.";

#if SHELL
        /// <summary>
        /// The message reporting that shell script library initialization will
        /// be skipped.
        /// </summary>
        public static readonly string NoInitializeShell =
            "Shell script library initialization will be skipped.";
#endif

        /// <summary>
        /// The message reporting the kiosk lock state.
        /// </summary>
        public static readonly string Kiosk =
            "Kiosk lock {0}.";

        /// <summary>
        /// The message reporting that disabling the kiosk lock was denied.
        /// </summary>
        public static readonly string DeniedKiosk =
            "Denied disable of kiosk lock after {0} loop(s).";

        /// <summary>
        /// The message reporting that exit from the interactive shell was
        /// denied.
        /// </summary>
        public static readonly string DeniedExit =
            "Denied exit from interactive shell{0}.";

        /// <summary>
        /// The message reporting that the interactive loop will be enabled.
        /// </summary>
        public static readonly string Loop =
            "Interactive loop will be enabled.";

        /// <summary>
        /// The message reporting that the interactive loop will be skipped.
        /// </summary>
        public static readonly string NoLoop =
            "Interactive loop will be skipped{0}.";

        /// <summary>
        /// The message reporting that argument processing will stop after an
        /// unknown argument.
        /// </summary>
        public static readonly string StopOnUnknown =
            "Argument processing will stop after unknown argument.";

        /// <summary>
        /// The message reporting that argument processing will continue after
        /// an unknown argument.
        /// </summary>
        public static readonly string NoStopOnUnknown =
            "Argument processing will continue after unknown argument{0}.";

        /// <summary>
        /// The message reporting that the parent interpreter will be used.
        /// </summary>
        public static readonly string Parent =
            "Parent interpreter will be used.";

        /// <summary>
        /// The message reporting that the child interpreter will be used.
        /// </summary>
        public static readonly string Child =
            "Child interpreter will be used.";

        /// <summary>
        /// The message reporting that the interpreter will be recreated.
        /// </summary>
        public static readonly string Create =
            "Interpreter will be recreated.";

        /// <summary>
        /// The message reporting that the interpreter will not be recreated.
        /// </summary>
        public static readonly string NoCreate =
            "Interpreter will not be recreated.";

        /// <summary>
        /// The message reporting that an interpreter was disposed.
        /// </summary>
        public static readonly string Disposed =
            "{0} interpreter {1} was {2}disposed.";

        /// <summary>
        /// The message reporting that an interpreter could not be disposed.
        /// </summary>
        public static readonly string DisposedError =
            "{0} interpreter {1} could not be disposed: {2}.";

        /// <summary>
        /// The message reporting that exceptions will not be thrown when
        /// disposed objects are accessed.
        /// </summary>
        public static readonly string NoThrowOnDisposed =
            "Exceptions will not be thrown when disposed objects are accessed.";

        /// <summary>
        /// The message reporting that the host profile was set.
        /// </summary>
        public static readonly string Profile =
            "Host profile set to \"{0}\".";

        /// <summary>
        /// The message reporting that the console will be attached or opened.
        /// </summary>
        public static readonly string UseAttach =
            "Console will be attached or opened.";

        /// <summary>
        /// The message reporting that the console will be forcibly attached or
        /// opened.
        /// </summary>
        public static readonly string UseForce =
            "Console will be forcibly attached or opened.";

        /// <summary>
        /// The message reporting that console output will not be in color.
        /// </summary>
        public static readonly string NoColor =
            "Console output will not be in color.";

        /// <summary>
        /// The message reporting that the console window will not be closable.
        /// </summary>
        public static readonly string NoClose =
            "Console window will not be closable.";

        /// <summary>
        /// The message reporting that the console title will not be changed.
        /// </summary>
        public static readonly string NoTitle =
            "Console title will not be changed.";

#if NATIVE && NATIVE_UTILITY
        /// <summary>
        /// The message reporting that the native utility library will not be
        /// loaded.
        /// </summary>
        public static readonly string NoNativeUtility =
            "Native utility library will not be loaded.";
#endif

        /// <summary>
        /// The message reporting that the console icon will not be changed.
        /// </summary>
        public static readonly string NoIcon =
            "Console icon will not be changed.";

        /// <summary>
        /// The message reporting that the host profile will not be loaded.
        /// </summary>
        public static readonly string NoProfile =
            "Host profile will not be loaded.";

        /// <summary>
        /// The message reporting that the host script cancellation interface
        /// will not be enabled.
        /// </summary>
        public static readonly string NoCancel =
            "Host script cancellation interface will not be enabled.";

        /// <summary>
        /// The message reporting that safe mode is enabled.
        /// </summary>
        public static readonly string Safe =
            "Safe mode enabled{0}.";

        /// <summary>
        /// The message reporting that standard mode is enabled.
        /// </summary>
        public static readonly string Standard =
            "Standard mode enabled{0}.";

        /// <summary>
        /// The suffix indicating that a setting originated from the command
        /// line.
        /// </summary>
        public static readonly string ViaCommandLine = " via command line";
        /// <summary>
        /// The suffix indicating that a setting originated from the
        /// environment.
        /// </summary>
        public static readonly string ViaEnvironment = " via environment";
        /// <summary>
        /// The suffix indicating that a setting originated from the host
        /// environment.
        /// </summary>
        public static readonly string ViaHostEnvironment = " via host environment";

#if !NET_STANDARD_20
        /// <summary>
        /// The suffix indicating that a setting originated from the registry
        /// hive.
        /// </summary>
        public static readonly string ViaRegistry = " via registry hive";
#endif
    }

    ///////////////////////////////////////////////////////////////////////////

    /// <summary>
    /// This class contains the well-known public key tokens used to identify
    /// the assemblies and signing keys recognized by the Eagle core library.
    /// </summary>
    [ObjectId("332b7844-2734-4413-b835-704abe7547c1")]
    public static class PublicKeyToken
    {
        //
        // NOTE: This string value appears to be used by the CLR when
        //       it is looking for an unsigned assembly.
        //
        /// <summary>
        /// The public key token string used by the CLR for an unsigned
        /// assembly.
        /// </summary>
        public static readonly string Null = "null";

        //
        // NOTE: The .NET Standard reference assemblies appear to use
        //       this public key token.
        //
        /// <summary>
        /// The public key token used by the .NET Standard reference
        /// assemblies.
        /// </summary>
        public static readonly string NetStandard = "cc7b13ffcd2ddd51";

        //
        // NOTE: Most of the ECMA compliant parts of the CLR use this
        //       public key token.
        //
        /// <summary>
        /// The public key token used by most of the ECMA compliant parts of
        /// the CLR.
        /// </summary>
        public static readonly string Ecma = "b77a5c561934e089";

        //
        // NOTE: Most of the Microsoft specific extensions to the CLR
        //       use this public key token.
        //
        /// <summary>
        /// The public key token used by most of the Microsoft-specific
        /// extensions to the CLR.
        /// </summary>
        public static readonly string Microsoft = "b03f5f7f11d50a3a";

        //
        // NOTE: Another public key token used for extensions to the
        //       CLR (mostly for Silverlight?).
        //
        /// <summary>
        /// Another public key token used for extensions to the CLR.
        /// </summary>
        public static readonly string SharedLib = "31bf3856ad364e35";

        //
        // NOTE: The Visual C++ runtime libraries use this public key
        //       token; however, only the managed C++ assemblies (e.g.
        //       "msvcm??.dll") are actually signed with it (i.e. the
        //       pure native libraries are not actually signed with
        //       it).
        //
        /// <summary>
        /// The public key token used by the managed Visual C++ runtime
        /// assemblies.
        /// </summary>
        public static readonly string VcRuntime = "1fc8b3b9a1e18e3b";

        //
        // NOTE: This public key token was first used by Silverlight;
        //       However, it is now used by the .NET Core runtime as
        //       well.
        //
        /// <summary>
        /// The public key token first used by Silverlight and now also used by
        /// the .NET Core runtime.
        /// </summary>
        public static readonly string Silverlight = "7cec85d7bea7798e";

        //
        // NOTE: The Windows Common Controls library uses this public
        //       key, as of version 6.0.
        //
        /// <summary>
        /// The public key token used by the Windows Common Controls library.
        /// </summary>
        public static readonly string CommonControls = "6595b64144ccf1df";

        //
        // NOTE: The (open source) WiX project uses this public key
        //       token, as of version 3.x.
        //
        /// <summary>
        /// The public key token used by the WiX project.
        /// </summary>
        public static readonly string WiX = "ce35f76fcda82bad";

        //
        // NOTE: The SQL Server managed assemblies use this public key
        //       token.
        //
        /// <summary>
        /// The public key token used by the SQL Server managed assemblies.
        /// </summary>
        public static readonly string SqlServer = "89845dcd8080cc91";

        //
        // NOTE: The (open source) System.Data.SQLite project uses this
        //       public key token.
        //
        /// <summary>
        /// The public key token used by the System.Data.SQLite project.
        /// </summary>
        public static readonly string SQLite = "db937bc2d44ff139";

        //
        // NOTE: The System.Data.SQLite.Enterprise project uses this
        //       public key token.
        //
        /// <summary>
        /// The public key token used by the System.Data.SQLite.Enterprise
        /// project.
        /// </summary>
        public static readonly string SQLiteEnterprise = "920bc3e7f8841675";

        //
        // NOTE: The Microsoft Office managed assemblies use this public
        //       key token.
        //
        /// <summary>
        /// The public key token used by the Microsoft Office managed
        /// assemblies.
        /// </summary>
        public static readonly string Office = "71e9bce111e9429c";

        ///////////////////////////////////////////////////////////////////////

        //
        // NOTE: Official demo licenses for Eagle Enterprise Edition are
        //       signed with this public key token ("EagleDemoPublic.snk",
        //       8192 bits).
        //
        /// <summary>
        /// The public key token used to sign official Eagle Enterprise Edition
        /// demo licenses.
        /// </summary>
        public static readonly string Demo = "5f8230f3e7b9b317";

        //
        // NOTE: Official debug builds (whether public or private) of
        //       the Eagle runtime library are signed with this public
        //       key token ("EagleFastPublic.snk", 4096 bits).
        //
        /// <summary>
        /// The public key token used to sign official debug builds of the
        /// Eagle runtime library.
        /// </summary>
        public static readonly string Fast = "29c6297630be05eb";

        //
        // NOTE: Official public release builds of the Eagle runtime
        //       library are signed with this public key token
        //       ("EagleStrongPublic.snk", 16384 bits).
        //
        /// <summary>
        /// The public key token used to sign official public release builds of
        /// the Eagle runtime library.
        /// </summary>
        public static readonly string Strong = "1e22ec67879739a2";

        //
        // NOTE: Official pre-release builds of the Eagle runtime
        //       library may be signed with this public key token
        //       ("EagleBetaPublic.snk", 8200 bits).
        //
        /// <summary>
        /// The public key token used to sign official pre-release builds of
        /// the Eagle runtime library.
        /// </summary>
        public static readonly string Beta = "358030063a832bc3";

        ///////////////////////////////////////////////////////////////////////

        //
        // NOTE: Official licenses for the security packages within
        //       Eagle Enterprise Edition (i.e. "Harpy" and "Badge")
        //       are normally signed with this public key token
        //       ("EagleEnterprisePluginRootPublic.snk", 4096 bits).
        //
        /// <summary>
        /// The public key token used to sign official licenses for the Eagle
        /// Enterprise Edition security packages.
        /// </summary>
        public static readonly string Security = "8bf43b4749e46a0b";

        ///////////////////////////////////////////////////////////////////////

        //
        // NOTE: Official key rings for the security packages within
        //       Eagle Enterprise Edition (i.e. "Harpy" and "Badge")
        //       are normally signed with this public key token
        //       ("EagleEnterpriseTrustRootPublic.snk", 16384 bits).
        //
        /// <summary>
        /// The public key token used to sign official key rings for the Eagle
        /// Enterprise Edition security packages.
        /// </summary>
        public static readonly string TrustRoot = "26f17c3a1a544324";

        ///////////////////////////////////////////////////////////////////////

        //
        // NOTE: Official scripts for the core library and the security
        //       package plugins are normally signed with this public
        //       key token ("EagleEnterpriseClass0RootPublic.snk", 16384
        //       bits).
        //
        /// <summary>
        /// The public key token used to sign official scripts for the core
        /// library and the security package plugins.
        /// </summary>
        public static readonly string Class0 = "9559f6017247e3e2";

        ///////////////////////////////////////////////////////////////////////

        //
        // NOTE: Official script bundles for the core library and the
        //       security package plugins are normally signed with this
        //       public key token ("EagleEnterpriseClass1RootPublic.snk",
        //       16384 bits).
        //
        /// <summary>
        /// The public key token used to sign official script bundles for the
        /// core library and the security package plugins.
        /// </summary>
        public static readonly string Class1 = "ab4e2d63da72214e";

        ///////////////////////////////////////////////////////////////////////

        //
        // NOTE: Official script responses for the core library and the
        //       security package plugins are normally signed with this
        //       public key token ("EagleEnterpriseClass2RootPublic.snk",
        //       8192 bits).
        //
        /// <summary>
        /// The public key token used to sign official script responses for the
        /// core library and the security package plugins.
        /// </summary>
        public static readonly string Class2 = "180558840e482cda";

        ///////////////////////////////////////////////////////////////////////

        //
        // NOTE: Personal scripts, bundles, etc, for "personal" use by
        //       the original author of this library, Joe Mistachkin,
        //       ("MistachkinPublic.snk", 8192 bits).
        //
        /// <summary>
        /// The public key token used for personal scripts and bundles
        /// belonging to the original author of this library.
        /// </summary>
        public static readonly string Mistachkin = "2c322765603b5278";

        ///////////////////////////////////////////////////////////////////////

        //
        // NOTE: For build lab use only.  This key should not be trusted
        //       for anything important ("EagleMonoPublic.snk", 1024 bits).
        //
        /// <summary>
        /// The public key token used for build lab purposes only; it should
        /// not be trusted for anything important.
        /// </summary>
        public static readonly string Build = "645d697a1b3acac5";

        ///////////////////////////////////////////////////////////////////////

        //
        // NOTE: For test lab use only.  This key should not be trusted for
        //       anything important ("TestTrustRootPublic.snk", 16384 bits).
        //
        /// <summary>
        /// The public key token used for test lab purposes only; it should not
        /// be trusted for anything important.
        /// </summary>
        public static readonly string Test = "a6086e3ec99207b8";
    }

    ///////////////////////////////////////////////////////////////////////////

    /// <summary>
    /// This class contains the compile-time characteristics of the current
    /// build of the Eagle core library.
    /// </summary>
    [ObjectId("c094f589-7ec2-4494-9cc0-498ea9fe2926")]
    public static class Build
    {
#if HAVE_SIZEOF
        /// <summary>
        /// The size, in bytes, of a native pointer for the target processor
        /// architecture.
        /// </summary>
#if ARM64 || IA64 || X64
        public const int SizeOfIntPtr = 8;
#elif ARM || X86
        public const int SizeOfIntPtr = 4;
#else
        #warning "Missing define for ARM, ARM64, X86, X64, or IA64."
#endif
#endif

        /// <summary>
        /// Non-zero if this is a debug build of the library.
        /// </summary>
#if DEBUG
        public static readonly bool Debug = true;
#else
        public static readonly bool Debug = false;
#endif

        /// <summary>
        /// Non-zero if this build of the library was compiled with console
        /// support.
        /// </summary>
#if CONSOLE
        public static readonly bool Console = true;
#else
        public static readonly bool Console = false;
#endif

        /// <summary>
        /// Non-zero if this is a verbose build of the library.
        /// </summary>
#if VERBOSE
        public static readonly bool Verbose = true;
#else
        public static readonly bool Verbose = false;
#endif
    }
}
