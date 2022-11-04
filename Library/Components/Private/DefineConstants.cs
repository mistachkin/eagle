/*
 * DefineConstants.cs --
 *
 * Copyright (c) 2007-2012 by Joe Mistachkin.  All rights reserved.
 *
 * See the file "license.terms" for information on usage and redistribution of
 * this file, and for a DISCLAIMER OF ALL WARRANTIES.
 *
 * RCS: @(#) $Id: $
 */

using Eagle._Attributes;
using Eagle._Containers.Public;

namespace Eagle._Constants
{
    [ObjectId("428a5842-dcd7-46e7-8b40-decfabb331a2")]
    internal static class DefineConstants
    {
        public static readonly StringList OptionList = new StringList(new string[] {
#if APPDOMAINS
            "APPDOMAINS",
#endif

#if APPROVED_VERBS
            "APPROVED_VERBS",
#endif

#if ARGUMENT_CACHE
            "ARGUMENT_CACHE",
#endif

#if ARM
            "ARM",
#endif

#if ARM64
            "ARM64",
#endif

#if ASSEMBLY_DATETIME
            "ASSEMBLY_DATETIME",
#endif

#if ASSEMBLY_RELEASE
            "ASSEMBLY_RELEASE",
#endif

#if ASSEMBLY_STRONG_NAME_TAG
            "ASSEMBLY_STRONG_NAME_TAG",
#endif

#if ASSEMBLY_TAG
            "ASSEMBLY_TAG",
#endif

#if ASSEMBLY_TEXT
            "ASSEMBLY_TEXT",
#endif

#if ASSEMBLY_URI
            "ASSEMBLY_URI",
#endif

#if BREAK_ON_EXITING
            "BREAK_ON_EXITING",
#endif

#if CACHE_ARGUMENT_TOSTRING
            "CACHE_ARGUMENT_TOSTRING",
#endif

#if CACHE_ARGUMENTLIST_TOSTRING
            "CACHE_ARGUMENTLIST_TOSTRING",
#endif

#if CACHE_DICTIONARY
            "CACHE_DICTIONARY",
#endif

#if CACHE_RESULT_TOSTRING
            "CACHE_RESULT_TOSTRING",
#endif

#if CACHE_STATISTICS
            "CACHE_STATISTICS",
#endif

#if CACHE_STRINGLIST_TOSTRING
            "CACHE_STRINGLIST_TOSTRING",
#endif

#if CALLBACK_QUEUE
            "CALLBACK_QUEUE",
#endif

#if CAS_POLICY
            "CAS_POLICY",
#endif

#if CERTIFICATE_PLUGIN
            "CERTIFICATE_PLUGIN",
#endif

#if CERTIFICATE_POLICY
            "CERTIFICATE_POLICY",
#endif

#if CERTIFICATE_RENEWAL
            "CERTIFICATE_RENEWAL",
#endif

#if CODE_ANALYSIS
            "CODE_ANALYSIS",
#endif

#if COM_TYPE_CACHE
            "COM_TYPE_CACHE",
#endif

#if COMPRESSION
            "COMPRESSION",
#endif

#if CONFIGURATION
            "CONFIGURATION",
#endif

#if CONSOLE
            "CONSOLE",
#endif

#if DAEMON
            "DAEMON",
#endif

#if DATA
            "DATA",
#endif

#if DEAD_CODE
            "DEAD_CODE",
#endif

#if DEBUG
            "DEBUG",
#endif

#if DEBUGGER
            "DEBUGGER",
#endif

#if DEBUGGER_ARGUMENTS
            "DEBUGGER_ARGUMENTS",
#endif

#if DEBUGGER_BREAKPOINTS
            "DEBUGGER_BREAKPOINTS",
#endif

#if DEBUGGER_ENGINE
            "DEBUGGER_ENGINE",
#endif

#if DEBUGGER_EXECUTE
            "DEBUGGER_EXECUTE",
#endif

#if DEBUGGER_EXPRESSION
            "DEBUGGER_EXPRESSION",
#endif

#if DEBUGGER_VARIABLE
            "DEBUGGER_VARIABLE",
#endif

#if DEBUG_TRACE
            "DEBUG_TRACE",
#endif

#if DEBUG_WRITE
            "DEBUG_WRITE",
#endif

#if DEMO_EDITION
            "DEMO_EDITION",
#endif

#if DRAWING
            "DRAWING",
#endif

#if DYNAMIC
            "DYNAMIC",
#endif

#if EAGLE
            "EAGLE",
#endif

#if EMBEDDED_LIBRARY
            "EMBEDDED_LIBRARY",
#endif

#if EMBED_CERTIFICATES
            "EMBED_CERTIFICATES",
#endif

#if EMIT
            "EMIT",
#endif

#if ENTERPRISE_LOCKDOWN
            "ENTERPRISE_LOCKDOWN",
#endif

#if EXECUTE_CACHE
            "EXECUTE_CACHE",
#endif

#if EXPRESSION_FLAGS
            "EXPRESSION_FLAGS",
#endif

#if FAST_ERRORCODE
            "FAST_ERRORCODE",
#endif

#if FAST_ERRORINFO
            "FAST_ERRORINFO",
#endif

#if FOR_TEST_USE_ONLY
            "FOR_TEST_USE_ONLY",
#endif

#if FORCE_TRACE
            "FORCE_TRACE",
#endif

#if HAVE_SIZEOF
            "HAVE_SIZEOF",
#endif

#if HISTORY
            "HISTORY",
#endif

#if IA64
            "IA64",
#endif

#if INTERACTIVE_COMMANDS
            "INTERACTIVE_COMMANDS",
#endif

#if INTERNALS_VISIBLE_TO
            "INTERNALS_VISIBLE_TO",
#endif

#if ISOLATED_INTERPRETERS
            "ISOLATED_INTERPRETERS",
#endif

#if ISOLATED_PLUGINS
            "ISOLATED_PLUGINS",
#endif

#if LIBRARY
            "LIBRARY",
#endif

#if LICENSE_MANAGER
            "LICENSE_MANAGER",
#endif

#if LICENSING
            "LICENSING",
#endif

#if LICENSING_NOP
            "LICENSING_NOP",
#endif

#if LIMITED_EDITION
            "LIMITED_EDITION",
#endif

#if LIST_CACHE
            "LIST_CACHE",
#endif

#if MAYBE_TRACE
            "MAYBE_TRACE",
#endif

#if MONO
            "MONO",
#endif

#if MONO_BUILD
            "MONO_BUILD",
#endif

#if MONO_HACKS
            "MONO_HACKS",
#endif

#if MONO_LEGACY
            "MONO_LEGACY",
#endif

#if NATIVE
            "NATIVE",
#endif

#if NATIVE_PACKAGE
            "NATIVE_PACKAGE",
#endif

#if NATIVE_THREAD_ID
            "NATIVE_THREAD_ID",
#endif

#if NATIVE_UTILITY
            "NATIVE_UTILITY",
#endif

#if NATIVE_UTILITY_BSTR
            "NATIVE_UTILITY_BSTR",
#endif

#if NETWORK
            "NETWORK",
#endif

#if NET_20
            "NET_20",
#endif

#if NET_20_FAST_ENUM
            "NET_20_FAST_ENUM",
#endif

#if NET_20_ONLY
            "NET_20_ONLY",
#endif

#if NET_20_SP1
            "NET_20_SP1",
#endif

#if NET_20_SP2
            "NET_20_SP2",
#endif

#if NET_30
            "NET_30",
#endif

#if NET_35
            "NET_35",
#endif

#if NET_40
            "NET_40",
#endif

#if NET_45
            "NET_45",
#endif

#if NET_451
            "NET_451",
#endif

#if NET_452
            "NET_452",
#endif

#if NET_46
            "NET_46",
#endif

#if NET_461
            "NET_461",
#endif

#if NET_462
            "NET_462",
#endif

#if NET_47
            "NET_47",
#endif

#if NET_471
            "NET_471",
#endif

#if NET_472
            "NET_472",
#endif

#if NET_48
            "NET_48",
#endif

#if NET_481
            "NET_481",
#endif

#if NET_CORE_REFERENCES
            "NET_CORE_REFERENCES",
#endif

#if NET_CORE_20
            "NET_CORE_20",
#endif

#if NET_CORE_30
            "NET_CORE_30",
#endif

#if NET_STANDARD_20
            "NET_STANDARD_20",
#endif

#if NET_STANDARD_21
            "NET_STANDARD_21",
#endif

#if NON_WORKING_CODE
            "NON_WORKING_CODE",
#endif

#if NOTIFY
            "NOTIFY",
#endif

#if NOTIFY_ACTIVE
            "NOTIFY_ACTIVE",
#endif

#if NOTIFY_ARGUMENTS
            "NOTIFY_ARGUMENTS",
#endif

#if NOTIFY_EXCEPTION
            "NOTIFY_EXCEPTION",
#endif

#if NOTIFY_EXECUTE
            "NOTIFY_EXECUTE",
#endif

#if NOTIFY_EXPRESSION
            "NOTIFY_EXPRESSION",
#endif

#if NOTIFY_GLOBAL
            "NOTIFY_GLOBAL",
#endif

#if NOTIFY_OBJECT
            "NOTIFY_OBJECT",
#endif

#if NOTIFY_TCL
            "NOTIFY_TCL",
#endif

#if OBSOLETE
            "OBSOLETE",
#endif

#if OBFUSCATION
            "OBFUSCATION",
#endif

#if OFFICIAL
            "OFFICIAL",
#endif

#if PARSE_CACHE
            "PARSE_CACHE",
#endif

#if PATCHLEVEL
            "PATCHLEVEL",
#endif

#if PLUGIN_COMMANDS
            "PLUGIN_COMMANDS",
#endif

#if POLICY_TRACE
            "POLICY_TRACE",
#endif

#if PREVIOUS_RESULT
            "PREVIOUS_RESULT",
#endif

#if PROFILER
            "PROFILER",
#endif

#if RANDOMIZE_ID
            "RANDOMIZE_ID",
#endif

#if REMOTING
            "REMOTING",
#endif

#if RESULT_LIMITS
            "RESULT_LIMITS",
#endif

#if SAMPLE
            "SAMPLE",
#endif

#if SCRIPT_ARGUMENTS
            "SCRIPT_ARGUMENTS",
#endif

#if SECURITY
            "SECURITY",
#endif

#if SERIALIZATION
            "SERIALIZATION",
#endif

#if SHARED_ID_POOL
            "SHARED_ID_POOL",
#endif

#if SHELL
            "SHELL",
#endif

#if SOURCE_ID
            "SOURCE_ID",
#endif

#if SOURCE_TIMESTAMP
            "SOURCE_TIMESTAMP",
#endif

#if STABLE
            "STABLE",
#endif

#if STATIC
            "STATIC",
#endif

#if STRICT_FEATURES
            "STRICT_FEATURES",
#endif

#if TCL
            "TCL",
#endif

#if TCL_KITS
            "TCL_KITS",
#endif

#if TCL_THREADED
            "TCL_THREADED",
#endif

#if TCL_THREADS
            "TCL_THREADS",
#endif

#if TCL_UNICODE
            "TCL_UNICODE",
#endif

#if TCL_WRAPPER
            "TCL_WRAPPER",
#endif

#if TEST
            "TEST",
#endif

#if TEST_PLUGIN
            "TEST_PLUGIN",
#endif

#if THREADING
            "THREADING",
#endif

#if THROW_ON_DISPOSED
            "THROW_ON_DISPOSED",
#endif

#if TRACE
            "TRACE",
#endif

#if TYPE_CACHE
            "TYPE_CACHE",
#endif

#if UNIX
            "UNIX",
#endif

#if UNSAFE
            "UNSAFE",
#endif

#if USE_APPDOMAIN_FOR_ID
            "USE_APPDOMAIN_FOR_ID",
#endif

#if USE_NAMESPACES
            "USE_NAMESPACES",
#endif

#if VERBOSE
            "VERBOSE",
#endif

#if WEB
            "WEB",
#endif

#if WINDOWS
            "WINDOWS",
#endif

#if WINFORMS
            "WINFORMS",
#endif

#if WIX_30
            "WIX_30",
#endif

#if WIX_35
            "WIX_35",
#endif

#if WIX_36
            "WIX_36",
#endif

#if WIX_37
            "WIX_37",
#endif

#if WIX_38
            "WIX_38",
#endif

#if WIX_39
            "WIX_39",
#endif

#if WIX_310
            "WIX_310",
#endif

#if WIX_311
            "WIX_311",
#endif

#if X64
            "X64",
#endif

#if X86
            "X86",
#endif

#if XML
            "XML",
#endif

            null
        });
    }
}
