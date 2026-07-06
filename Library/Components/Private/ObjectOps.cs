/*
 * ObjectOps.cs --
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

#if DATA
using System.Data;
#endif

using System.Diagnostics;
using System.Globalization;
using System.Reflection;

#if NET_451 || NET_452 || NET_46 || NET_461 || NET_462 || NET_47 || NET_471 || NET_472 || NET_48 || NET_481 || NET_STANDARD_20
using System.Runtime;
#endif

using System.Security.Cryptography.X509Certificates;
using Eagle._Attributes;
using Eagle._Components.Public;
using Eagle._Constants;
using Eagle._Containers.Private;
using Eagle._Containers.Public;
using Eagle._Interfaces.Public;

using DelegateTriplet = Eagle._Components.Public.MutableAnyTriplet<
    System.Reflection.MethodBase, System.Delegate,
    Eagle._Components.Public.DelegateFlags>;

using DelegateList = System.Collections.Generic.List<
    Eagle._Components.Public.MutableAnyTriplet<
    System.Reflection.MethodBase, System.Delegate,
    Eagle._Components.Public.DelegateFlags>>;

#if NET_STANDARD_21
using Index = Eagle._Constants.Index;
#endif

namespace Eagle._Components.Private
{
    /// <summary>
    /// This class provides the private static helper methods and default
    /// settings used by the Eagle interpreter when integrating native CLR
    /// objects with scripts.  It centralizes the default values and option
    /// sets for the various object-related sub-commands, builds and processes
    /// the option dictionaries that drive object creation, member lookup,
    /// reflection, marshalling, and invocation, and provides supporting
    /// services for garbage collection and object disposal.
    /// </summary>
    [ObjectId("21953933-d364-453c-b848-01e348a8f8ac")]
    internal static class ObjectOps
    {
        #region Private Constants
        //
        // HACK: These are purposely not read-only.
        //
        /// <summary>
        /// The set of regular expression patterns used to recognize the names
        /// of fields that indicate whether an object has been disposed.
        /// </summary>
        private static string[] DisposedFieldNames;
        /// <summary>
        /// The set of property names used to recognize the properties that
        /// indicate whether an object has been disposed.
        /// </summary>
        private static string[] DisposedPropertyNames;

        ///////////////////////////////////////////////////////////////////////

        //
        // NOTE: *WARNING* Changes to this assembly name are considered
        //        to be a "breaking change".
        //
        // HACK: This is purposely not read-only.
        //
        /// <summary>
        /// The cached full name of the core CLR assembly (i.e. the one that
        /// contains the <see cref="System.Object" /> type).
        /// </summary>
        private static string clrSimpleName;

        ///////////////////////////////////////////////////////////////////////

        //
        // NOTE: *WARNING* Changes to this assembly name are considered
        //        to be a "breaking change".
        //
        // HACK: This is purposely not read-only.
        //
        /// <summary>
        /// The cached full name of the Eagle assembly.
        /// </summary>
        private static string eagleSimpleName;

        ///////////////////////////////////////////////////////////////////////

        //
        // NOTE: *WARNING* Changes to this namespace name are considered
        //        to be a "breaking change".
        //
        // HACK: This is purposely not read-only.
        //
        /// <summary>
        /// The cached name of the namespace that contains the private "guru"
        /// (i.e. advanced) types used by the interpreter.
        /// </summary>
        private static string guruNamespace;

        ///////////////////////////////////////////////////////////////////////

        //
        // HACK: This is purposely not read-only.
        //
        /// <summary>
        /// The default list of CLR namespaces that are implicitly searched
        /// when resolving an unqualified type name.
        /// </summary>
        private static string[] DefaultClrNamespaces;

        ///////////////////////////////////////////////////////////////////////

        //
        // HACK: This is purposely not read-only.
        //
        /// <summary>
        /// The default list of Eagle namespaces that are implicitly searched
        /// when resolving an unqualified type name.
        /// </summary>
        private static string[] DefaultEagleNamespaces;

        ///////////////////////////////////////////////////////////////////////

        //
        // HACK: This is purposely not read-only.
        //
        /// <summary>
        /// The lookup table that maps each <see cref="MetaMemberTypes" /> value
        /// to its corresponding combination of <see cref="MemberTypes" />
        /// flags.
        /// </summary>
        private static MemberTypes[] metaMemberTypesMappings;

        ///////////////////////////////////////////////////////////////////////

        //
        // HACK: This is purposely not read-only.
        //
        /// <summary>
        /// The lookup table that maps each <see cref="MetaBindingFlags" /> value
        /// to its corresponding combination of <see cref="BindingFlags" />
        /// flags.
        /// </summary>
        private static BindingFlags[] metaBindingFlagsMappings;
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Private Data
        #region System Defaults
        #region Data and Database
#if DATA
        //
        // HACK: These are the defaults for the [sql execute] sub-command.
        //
        /// <summary>
        /// The default command type used by the [sql execute] sub-command.
        /// </summary>
        private static CommandType DefaultCommandType = CommandType.Text;
        /// <summary>
        /// The default command behavior used by the [sql execute] sub-command.
        /// </summary>
        private static CommandBehavior DefaultCommandBehavior = CommandBehavior.Default;
        /// <summary>
        /// The default execution type used by the [sql execute] sub-command.
        /// </summary>
        private static DbExecuteType DefaultExecuteType = DbExecuteType.Default;
        /// <summary>
        /// The default result format used by the [sql execute] sub-command.
        /// </summary>
        private static DbResultFormat DefaultResultFormat = DbResultFormat.Default;
        /// <summary>
        /// The default value flags used when converting values for the
        /// [sql execute] sub-command.
        /// </summary>
        private static ValueFlags DefaultValueFlags = ValueFlags.AnyNonCharacter;
#endif
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region DateTime Format, Kind, and NTP Servers
        /// <summary>
        /// The default format string used when converting DateTime values to
        /// and from their string representation; null selects the built-in
        /// default behavior.
        /// </summary>
        private static string DefaultDateTimeFormat = null;

        ///////////////////////////////////////////////////////////////////////

#if DATA
        //
        // HACK: This is the default for [sql execute].
        //
        /// <summary>
        /// The default DateTime behavior used by the [sql execute]
        /// sub-command.
        /// </summary>
        private static DateTimeBehavior DefaultDateTimeBehavior =
            DateTimeBehavior.Default;

        ///////////////////////////////////////////////////////////////////////

        //
        // HACK: This is the default for [sql execute].
        //
        /// <summary>
        /// The default BLOB behavior used by the [sql execute] sub-command.
        /// </summary>
        private static BlobBehavior DefaultBlobBehavior =
            BlobBehavior.Default;
#endif

        ///////////////////////////////////////////////////////////////////////

        //
        // HACK: Default to "unspecified" for DateTime values.  Perhaps this
        //       should be "UTC" instead?
        //
        /// <summary>
        /// The default DateTimeKind used when interpreting DateTime values
        /// whose kind is otherwise unknown.
        /// </summary>
        private static DateTimeKind DefaultDateTimeKind =
            DateTimeKind.Unspecified;

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// The default DateTime styles used when parsing DateTime values.
        /// </summary>
        private static DateTimeStyles DefaultDateTimeStyles =
            DateTimeStyles.None;

        ///////////////////////////////////////////////////////////////////////

        //
        // HACK: This is purposely not read-only.
        //
        /// <summary>
        /// The default list of NTP time servers; null selects the built-in
        /// default list.
        /// </summary>
        private static IEnumerable<string> DefaultTimeServers = null;
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Pattern-Related Flags
        /// <summary>
        /// The default pattern matching mode used when matching member names
        /// and similar values.
        /// </summary>
        private static MatchMode DefaultMatchMode = MatchMode.None;
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Object Creation
        //
        // NOTE: The default behavior for the -create / -nocreate options
        //       is controlled by these fields.
        //
        /// <summary>
        /// The default value for the -create option; non-zero means a new
        /// managed object should be created by default.
        /// </summary>
        private static bool DefaultCreate = false;
        /// <summary>
        /// The default value for the -nocreate option; non-zero means a new
        /// managed object should not be created by default.
        /// </summary>
        private static bool DefaultNoCreate = true;
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Reflection-Related Flags
        //
        // NOTE: This controls the default member types for all sub-command
        //       options; therefore, it cannot be (easily) kept directly in
        //       the meta member types map.
        //
        /// <summary>
        /// The default member types used by all object sub-command options
        /// that do not specify their own.
        /// </summary>
        private static MemberTypes DefaultMemberTypes; /* EXEMPT */

        ///////////////////////////////////////////////////////////////////////

        //
        // NOTE: This controls the default binding flags for all sub-commands
        //       options; therefore, it cannot be (easily) kept directly in
        //       the meta binding flags map.
        //
        /// <summary>
        /// The default binding flags used by all object sub-command options
        /// that do not specify their own.
        /// </summary>
        private static BindingFlags DefaultBindingFlags; /* EXEMPT */
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Marshal-Related Flags
        /// <summary>
        /// The default load type used when loading an assembly.
        /// </summary>
        private static LoadType DefaultLoadType = LoadType.Default;

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// The default marshalling flags used when converting values between
        /// the script and managed worlds.
        /// </summary>
        private static MarshalFlags DefaultMarshalFlags =
            MarshalFlags.Default;

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// The default marshalling flags used when converting individual
        /// method parameters.
        /// </summary>
        private static MarshalFlags DefaultParameterMarshalFlags =
            MarshalFlags.None;

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// The default reordering flags used when matching and reordering
        /// method overload arguments.
        /// </summary>
        private static ReorderFlags DefaultReorderFlags =
            ReorderFlags.Default;

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// The default flags applied to by-reference (ref or out) method
        /// arguments.
        /// </summary>
        private static ByRefArgumentFlags DefaultByRefArgumentFlags =
            ByRefArgumentFlags.None;

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// The default object flags applied to managed objects added to the
        /// interpreter.
        /// </summary>
        private static ObjectFlags DefaultObjectFlags =
            ObjectFlags.Default;

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// The default object flags applied to managed objects produced from
        /// by-reference (ref or out) method arguments.
        /// </summary>
        private static ObjectFlags DefaultByRefObjectFlags =
            ObjectFlags.None;

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// The default callback flags used when creating script callbacks for
        /// managed delegates.
        /// </summary>
        private static CallbackFlags DefaultCallbackFlags =
            CallbackFlags.Default;

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// The default object option type used when no specific option type is
        /// requested.
        /// </summary>
        private static ObjectOptionType DefaultObjectOptionType =
            ObjectOptionType.Default;

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// The default value flags used when converting object values.
        /// </summary>
        private static ValueFlags DefaultObjectValueFlags = ValueFlags.None;

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// The default value flags used when converting member values.
        /// </summary>
        private static ValueFlags DefaultMemberValueFlags = ValueFlags.None;
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Object Disposal
        //
        // NOTE: Non-zero means an object should be disposed prior to it
        //       being removed fro the interpreter.
        //
        /// <summary>
        /// The default value controlling whether an object is disposed prior to
        /// being removed from the interpreter; non-zero means dispose.
        /// </summary>
        private static bool DefaultDispose = true;

        ///////////////////////////////////////////////////////////////////////

        //
        // HACK: These are purposely not read-only.
        //
        /// <summary>
        /// The pattern matching mode used when matching disposed field and
        /// property names.
        /// </summary>
        private static MatchMode IsDisposedPatternMode = MatchMode.RegExp;
        /// <summary>
        /// Non-zero if matching of disposed field and property names should be
        /// case-insensitive.
        /// </summary>
        private static bool IsDisposedPatterNoCase = true;
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region GC Settings
        //
        // NOTE: The default behavior was to run garbage collection after
        //       removing a managed object from the interpreter; however,
        //       that did have negative performance implications.
        //
        /// <summary>
        /// The default value controlling whether garbage collection is
        /// performed synchronously; non-zero means synchronous.
        /// </summary>
        private static bool DefaultSynchronous = false;

        ///////////////////////////////////////////////////////////////////////

        //
        // NOTE: The default behavior is not to wait for pending finalizers
        //       to finish.
        //
        /// <summary>
        /// The default value controlling whether to wait for pending
        /// finalizers to finish after collecting garbage; non-zero means wait.
        /// </summary>
        private static bool DefaultWaitForGC = false;

        ///////////////////////////////////////////////////////////////////////

#if NET_451 || NET_452 || NET_46 || NET_461 || NET_462 || NET_47 || NET_471 || NET_472 || NET_48 || NET_481 || NET_STANDARD_20
        //
        // NOTE: The default behavior is not to compact the large object
        //       heap; however, compacting it can be useful if many large
        //       objects are being created and finalized.
        //
        /// <summary>
        /// The default value controlling whether the large object heap is
        /// compacted during garbage collection; non-zero means compact.
        /// </summary>
        private static bool DefaultCompactLargeObjectHeap = false;
#endif
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Option Flags
        //
        // NOTE: Any changes to these default option flag values will be
        //       library-wide.
        //
        /// <summary>
        /// The extra option flags applied to the -alias option.
        /// </summary>
        private static OptionFlags AliasOptionFlags = OptionFlags.None;
        /// <summary>
        /// The extra option flags applied to the -create option.
        /// </summary>
        private static OptionFlags CreateOptionFlags = OptionFlags.None;
        /// <summary>
        /// The extra option flags applied to the -nocreate option.
        /// </summary>
        private static OptionFlags NoCreateOptionFlags = OptionFlags.None;
        /// <summary>
        /// The extra option flags applied to the -nodispose option.
        /// </summary>
        private static OptionFlags NoDisposeOptionFlags = OptionFlags.None;
        /// <summary>
        /// The extra option flags applied to the -synchronous option.
        /// </summary>
        private static OptionFlags SynchronousOptionFlags = OptionFlags.None;
        /// <summary>
        /// The extra option flags applied to the -debug option.
        /// </summary>
        private static OptionFlags DebugOptionFlags = OptionFlags.None;
        /// <summary>
        /// The extra option flags applied to the -trace option.
        /// </summary>
        private static OptionFlags TraceOptionFlags = OptionFlags.None;
        /// <summary>
        /// The extra option flags applied to the -verbose option.
        /// </summary>
        private static OptionFlags VerboseOptionFlags = OptionFlags.None;
        /// <summary>
        /// The extra option flags applied to the -arrayaslink option.
        /// </summary>
        private static OptionFlags ArrayAsLinkOptionFlags = OptionFlags.None;
        /// <summary>
        /// The extra option flags applied to the -arrayasvalue option.
        /// </summary>
        private static OptionFlags ArrayAsValueOptionFlags = OptionFlags.None;
        #endregion
        #endregion
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Public Initialization Methods
        /// <summary>
        /// This method initializes all of the static state used by this class,
        /// including the disposed name patterns, the default namespaces, the
        /// meta member type and binding flag mappings, and the reflection
        /// defaults.
        /// </summary>
        /// <param name="force">
        /// Non-zero to force (re)initialization even if the relevant state has
        /// already been initialized.
        /// </param>
        public static void Initialize(
            bool force
            )
        {
            InitializeDisposedNames(force);

            ///////////////////////////////////////////////////////////////////

            InitializeNamespaces(force);
            InitializeMetaMemberTypesMappings(force);
            InitializeMetaBindingFlagsMappings(force);
            InitializeReflectionDefaults(force);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method initializes the cached global-state values used by this
        /// class, including the core CLR assembly name, the Eagle assembly
        /// name, and the guru namespace name.
        /// </summary>
        /// <param name="force">
        /// Non-zero to force (re)initialization even if the relevant state has
        /// already been initialized.
        /// </param>
        public static void InitializeGlobalState(
            bool force
            )
        {
            if (force || (clrSimpleName == null))
            {
                /* mscorlib */
                clrSimpleName = AssemblyOps.GetFullName(typeof(object));
            }

            ///////////////////////////////////////////////////////////////////

            if (force || (eagleSimpleName == null))
            {
                /* Eagle */
                eagleSimpleName = GlobalState.GetAssemblyFullName();
            }

            ///////////////////////////////////////////////////////////////////

            if (force || (guruNamespace == null))
            {
                /* Eagle._Components.Private */
                guruNamespace = typeof(GlobalState).Namespace;
            }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Private Initialization Methods
        /// <summary>
        /// This method initializes the field name patterns and property names
        /// used to detect whether an object has been disposed.
        /// </summary>
        /// <param name="force">
        /// Non-zero to force (re)initialization even if the relevant state has
        /// already been initialized.
        /// </param>
        private static void InitializeDisposedNames(
            bool force
            )
        {
            if (force || (DisposedFieldNames == null))
            {
                DisposedFieldNames = new string[] {
                    "^(?:m)?(?:_)*(?:is)?disposed$",
                    null, null, null, null, null, null, null, null,
                    null, null, null, null, null, null, null, null,
                    null, null, null, null, null, null, null, null,
                    null, null, null, null, null, null, null
                };
            }

            ///////////////////////////////////////////////////////////////////

            if (force || (DisposedPropertyNames == null))
            {
                DisposedPropertyNames = new string[] {
                    "Disposed", "IsDisposed", null, null, null
                };
            }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method initializes the default lists of CLR and Eagle
        /// namespaces that are implicitly searched when resolving unqualified
        /// type names.
        /// </summary>
        /// <param name="force">
        /// Non-zero to force (re)initialization even if the relevant state has
        /// already been initialized.
        /// </param>
        private static void InitializeNamespaces(
            bool force
            )
        {
            if (force || (DefaultClrNamespaces == null))
            {
                //
                // NOTE: *WARNING* Changes to this list are considered
                //       to be a "breaking change".
                //
                DefaultClrNamespaces = new string[] {
                    /* System */
                    typeof(object).Namespace,

                    /* RESERVED FOR FUTURE USE */
                    null,
                    null,
                    null,
                    null
                };
            }

            ///////////////////////////////////////////////////////////////////

            if (force || (DefaultEagleNamespaces == null))
            {
                //
                // NOTE: *WARNING* Changes to this list are considered
                //       to be a "breaking change".
                //
                DefaultEagleNamespaces = new string[] {
                    /* Eagle._Attributes */
                    // typeof(AssemblyDateTimeAttribute).Namespace,

                    /* Eagle._Components.Public */
                    typeof(Engine).Namespace,

                    /* Eagle._Containers.Public */
                    typeof(ArgumentList).Namespace,

                    /* Eagle._Encodings */
                    // typeof(_Encodings.OneByteEncoding).Namespace,

                    /* Eagle._Interfaces.Public */
                    // typeof(IClientData).Namespace,

                    /* RESERVED FOR FUTURE USE */
                    null,
                    null,
                    null,
                    null
                };
            }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method initializes the lookup table that maps each
        /// <see cref="MetaMemberTypes" /> value to its corresponding
        /// combination of <see cref="MemberTypes" /> flags.
        /// </summary>
        /// <param name="force">
        /// Non-zero to force (re)initialization even if the relevant state has
        /// already been initialized.
        /// </param>
        private static void InitializeMetaMemberTypesMappings(
            bool force
            )
        {
            if (force || (metaMemberTypesMappings == null))
            {
                metaMemberTypesMappings = new MemberTypes[] {
                    /* MetaMemberTypes.FlagsEnum */
                    MemberTypes.Field | MemberTypes.Property,

                    /* MetaMemberTypes.UnsafeObject */
                    MemberTypes.Constructor | MemberTypes.Event |
                    MemberTypes.TypeInfo | MemberTypes.Custom |
                    MemberTypes.NestedType,

                    /* MetaMemberTypes.ObjectDefault */
                    MemberTypes.Field | MemberTypes.Method |
                    MemberTypes.Property
                };
            }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method initializes the lookup table that maps each
        /// <see cref="MetaBindingFlags" /> value to its corresponding
        /// combination of <see cref="BindingFlags" /> flags.
        /// </summary>
        /// <param name="force">
        /// Non-zero to force (re)initialization even if the relevant state has
        /// already been initialized.
        /// </param>
        private static void InitializeMetaBindingFlagsMappings(
            bool force
            )
        {
            if (force || (metaBindingFlagsMappings == null))
            {
                metaBindingFlagsMappings = new BindingFlags[] {
                    /* MetaBindingFlags.PrivateCreateInstance */
                    BindingFlags.Instance | BindingFlags.NonPublic |
                    BindingFlags.CreateInstance,

                    /* MetaBindingFlags.PrivateInstance */
                    BindingFlags.Instance | BindingFlags.NonPublic,

                    /* MetaBindingFlags.PrivateInstanceGetField */
                    BindingFlags.Instance | BindingFlags.NonPublic |
                    BindingFlags.GetField,

                    /* MetaBindingFlags.PrivateInstanceGetProperty */
                    BindingFlags.Instance | BindingFlags.NonPublic |
                    BindingFlags.GetProperty,

                    /* MetaBindingFlags.PrivateInstanceMethod */
                    BindingFlags.Instance | BindingFlags.NonPublic |
                    BindingFlags.InvokeMethod,

                    /* MetaBindingFlags.PrivateStatic */
                    BindingFlags.Static | BindingFlags.NonPublic,

                    /* MetaBindingFlags.PrivateStaticGetField */
                    BindingFlags.Static | BindingFlags.NonPublic |
                    BindingFlags.GetField,

                    /* MetaBindingFlags.PrivateStaticGetProperty */
                    BindingFlags.Static | BindingFlags.NonPublic |
                    BindingFlags.GetProperty,

                    /* MetaBindingFlags.PrivateStaticMethod */
                    BindingFlags.Static | BindingFlags.NonPublic |
                    BindingFlags.InvokeMethod,

                    /* MetaBindingFlags.PrivateStaticSetField */
                    BindingFlags.Static | BindingFlags.NonPublic |
                    BindingFlags.SetField,

                    /* MetaBindingFlags.PrivateStaticSetProperty */
                    BindingFlags.Static | BindingFlags.NonPublic |
                    BindingFlags.SetProperty,

                    /* MetaBindingFlags.PublicCreateInstance */
                    /* BindingFlags.Instance | BindingFlags.Public | */
                    BindingFlags.CreateInstance,

                    /* MetaBindingFlags.PublicInstance */
                    BindingFlags.Instance | BindingFlags.Public,

                    /* MetaBindingFlags.PublicInstanceGetField */
                    BindingFlags.Instance | BindingFlags.Public |
                    BindingFlags.GetField,

                    /* MetaBindingFlags.PublicInstanceGetProperty */
                    BindingFlags.Instance | BindingFlags.Public |
                    BindingFlags.GetProperty,

                    /* MetaBindingFlags.PublicInstanceMethod */
                    BindingFlags.Instance | BindingFlags.Public |
                    BindingFlags.InvokeMethod,

                    /* MetaBindingFlags.PublicStaticGetField */
                    BindingFlags.Static | BindingFlags.Public |
                    BindingFlags.GetField,

                    /* MetaBindingFlags.PublicStaticGetProperty */
                    BindingFlags.Static | BindingFlags.Public |
                    BindingFlags.GetProperty,

                    /* MetaBindingFlags.PublicStaticMethod */
                    BindingFlags.Static | BindingFlags.Public |
                    BindingFlags.InvokeMethod,

                    /* MetaBindingFlags.Default */
                    BindingFlags.Default,

                    /* MetaBindingFlags.EnumField */
                    BindingFlags.Static | BindingFlags.Public,

                    /* MetaBindingFlags.HostInfo */
                    BindingFlags.Instance | BindingFlags.Public |
                    BindingFlags.FlattenHierarchy | BindingFlags.InvokeMethod |
                    BindingFlags.GetField | BindingFlags.GetProperty,

                    /* MetaBindingFlags.ListProperties */
                    BindingFlags.Instance | BindingFlags.Public |
                    BindingFlags.FlattenHierarchy,

                    /* MetaBindingFlags.LooseMethod */
                    BindingFlags.IgnoreCase | BindingFlags.Instance |
                    BindingFlags.Static | BindingFlags.Public |
                    BindingFlags.FlattenHierarchy | BindingFlags.InvokeMethod,

                    /* MetaBindingFlags.NestedObject */
                    BindingFlags.FlattenHierarchy | BindingFlags.InvokeMethod |
                    BindingFlags.GetField | BindingFlags.GetProperty,

                    /* MetaBindingFlags.UnsafeObject */
                    BindingFlags.NonPublic | BindingFlags.FlattenHierarchy,

                    /* MetaBindingFlags.DomainId */
                    BindingFlags.Instance | BindingFlags.NonPublic,

                    /* MetaBindingFlags.IsLegacyCasPolicyEnabled */
                    BindingFlags.Instance | BindingFlags.NonPublic,

                    /* MetaBindingFlags.FlagsEnum */
                    BindingFlags.Instance | BindingFlags.Public |
                    BindingFlags.NonPublic,

                    /* MetaBindingFlags.ByteBuffer */
                    BindingFlags.Instance | BindingFlags.NonPublic |
                    BindingFlags.GetField,

                    /* MetaBindingFlags.HostProperty */
                    BindingFlags.IgnoreCase | BindingFlags.Instance |
                    BindingFlags.Public | BindingFlags.NonPublic |
                    BindingFlags.SetProperty,

                    /* MetaBindingFlags.Items */
                    BindingFlags.Instance | BindingFlags.NonPublic,

                    /* MetaBindingFlags.Size */
                    BindingFlags.Instance | BindingFlags.NonPublic,

                    /* MetaBindingFlags.DisposedField */
                    BindingFlags.Instance | BindingFlags.Public |
                    BindingFlags.NonPublic | BindingFlags.GetField,

                    /* MetaBindingFlags.DisposedProperty */
                    BindingFlags.Instance | BindingFlags.Public |
                    BindingFlags.NonPublic | BindingFlags.GetProperty,

                    /* MetaBindingFlags.Guru */
                    BindingFlags.NonPublic,

                    /* MetaBindingFlags.InvokeRaw */
                    BindingFlags.InvokeMethod,

                    /* MetaBindingFlags.ObjectDefault */
                    BindingFlags.Instance | BindingFlags.Static |
                    BindingFlags.Public | BindingFlags.FlattenHierarchy,

                    /* MetaBindingFlags.Delegate */
                    BindingFlags.Instance | BindingFlags.Static |
                    BindingFlags.Public | BindingFlags.NonPublic,

                    /* MetaBindingFlags.Socket */
                    BindingFlags.Instance | BindingFlags.NonPublic,

                    /* MetaBindingFlags.Socket2 */
                    BindingFlags.Instance | BindingFlags.Public,

                    /* MetaBindingFlags.Trace */
                    BindingFlags.Instance | BindingFlags.NonPublic,

                    /* MetaBindingFlags.TransferHelper */
                    BindingFlags.Static | BindingFlags.Public |
                    BindingFlags.NonPublic,

                    /* MetaBindingFlags.InterpreterSettings */
                    BindingFlags.Instance | BindingFlags.Public |
                    BindingFlags.SetProperty,

                    /* MetaBindingFlags.TypeDefaultLookup */
                    BindingFlags.Instance | BindingFlags.Static |
                    BindingFlags.Public,

                    /* MetaBindingFlags.DynamicMethodHandle */
                    BindingFlags.Instance | BindingFlags.NonPublic
                };
            }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method initializes the default member types and binding flags
        /// used by the object sub-command options, deriving them from the meta
        /// member type and binding flag mappings.
        /// </summary>
        /// <param name="force">
        /// Non-zero to force (re)initialization even if the relevant state has
        /// already been initialized.
        /// </param>
        private static void InitializeReflectionDefaults(
            bool force
            )
        {
            if (force || (DefaultMemberTypes == (MemberTypes)0))
            {
                DefaultMemberTypes = GetMemberTypes(
                    MetaMemberTypes.ObjectDefault, true); /* EXEMPT */
            }

            ///////////////////////////////////////////////////////////////////

            if (force || (DefaultBindingFlags == (BindingFlags)0))
            {
                DefaultBindingFlags = GetBindingFlags(
                    MetaBindingFlags.ObjectDefault, true); /* EXEMPT */
            }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region DateTime Default Settings Support Methods
        /// <summary>
        /// This method returns the default format string used when converting
        /// DateTime values to and from their string representation.
        /// </summary>
        /// <returns>
        /// The default DateTime format string, or null to use the built-in
        /// default behavior.
        /// </returns>
        public static string GetDefaultDateTimeFormat()
        {
            return DefaultDateTimeFormat;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method returns the default DateTimeKind used when interpreting
        /// DateTime values whose kind is otherwise unknown.
        /// </summary>
        /// <returns>
        /// The default DateTimeKind.
        /// </returns>
        public static DateTimeKind GetDefaultDateTimeKind()
        {
            return DefaultDateTimeKind;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method returns the default DateTime styles used when parsing
        /// DateTime values.
        /// </summary>
        /// <returns>
        /// The default DateTime styles.
        /// </returns>
        public static DateTimeStyles GetDefaultDateTimeStyles()
        {
            return DefaultDateTimeStyles;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method returns the default list of NTP time servers.
        /// </summary>
        /// <returns>
        /// The default list of time servers, or null to use the built-in
        /// default list.
        /// </returns>
        public static IEnumerable<string> GetDefaultTimeServers()
        {
            return DefaultTimeServers;
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Default Settings Support Methods
        /// <summary>
        /// This method returns the default value controlling whether an object
        /// is disposed prior to being removed from the interpreter.
        /// </summary>
        /// <returns>
        /// Non-zero if objects should be disposed by default; otherwise, zero.
        /// </returns>
        public static bool GetDefaultDispose()
        {
            return DefaultDispose;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method returns the default value controlling whether garbage
        /// collection is performed synchronously.
        /// </summary>
        /// <returns>
        /// Non-zero if garbage collection should be synchronous by default;
        /// otherwise, zero.
        /// </returns>
        public static bool GetDefaultSynchronous()
        {
            return DefaultSynchronous;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method maps the specified <see cref="MetaMemberTypes" /> value
        /// to its corresponding combination of <see cref="MemberTypes" />
        /// flags.
        /// </summary>
        /// <param name="metaMemberTypes">
        /// The meta member types value to map.
        /// </param>
        /// <param name="noComplain">
        /// Non-zero to suppress the error reported when the mapping cannot be
        /// performed.
        /// </param>
        /// <returns>
        /// The mapped member types, or zero if the mapping cannot be performed.
        /// </returns>
        public static MemberTypes GetMemberTypes(
            MetaMemberTypes metaMemberTypes,
            bool noComplain
            )
        {
            MemberTypes? memberTypes;
            Result error = null;

            memberTypes = GetMemberTypes(metaMemberTypes, null, ref error);

            if (memberTypes != null)
                return (MemberTypes)memberTypes;

            if (!noComplain)
            {
                DebugOps.Complain(ReturnCode.Error, String.Format(
                    "missing meta member types for {0}: {1}",
                    FormatOps.WrapOrNull(metaMemberTypes),
                    FormatOps.WrapOrNull(error)));
            }

            return (MemberTypes)0; /* MemberTypes.??? */
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method maps the specified <see cref="MetaMemberTypes" /> value
        /// to its corresponding combination of <see cref="MemberTypes" />
        /// flags, using the meta member types lookup table.
        /// </summary>
        /// <param name="metaMemberTypes">
        /// The meta member types value to map.
        /// </param>
        /// <param name="memberTypes">
        /// The member types value to return when the mapping cannot be
        /// performed; this may be null.
        /// </param>
        /// <param name="error">
        /// Upon failure, this will contain an appropriate error message.
        /// </param>
        /// <returns>
        /// The mapped member types upon success, or the value of
        /// <paramref name="memberTypes" /> upon failure.
        /// </returns>
        private static MemberTypes? GetMemberTypes(
            MetaMemberTypes metaMemberTypes, /* in */
            MemberTypes? memberTypes,        /* in: OPTIONAL */
            ref Result error                 /* out */
            )
        {
            if (metaMemberTypesMappings == null)
            {
                error = "no meta member types are available";
                return memberTypes;
            }

            int length = metaMemberTypesMappings.Length;
            int index = (int)(metaMemberTypes & MetaMemberTypes.IndexMask);

            if ((index < 0) || (index >= length))
            {
                error = String.Format(
                    "meta member types index {0} out of bounds 0 to {1}",
                    index, length - 1);

                return memberTypes;
            }

            return metaMemberTypesMappings[index];
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method maps the specified <see cref="MetaBindingFlags" /> value
        /// to its corresponding combination of <see cref="BindingFlags" />
        /// flags.
        /// </summary>
        /// <param name="metaBindingFlags">
        /// The meta binding flags value to map.
        /// </param>
        /// <param name="noComplain">
        /// Non-zero to suppress the error reported when the mapping cannot be
        /// performed.
        /// </param>
        /// <returns>
        /// The mapped binding flags, or <see cref="BindingFlags.Default" /> if
        /// the mapping cannot be performed.
        /// </returns>
        public static BindingFlags GetBindingFlags(
            MetaBindingFlags metaBindingFlags,
            bool noComplain
            )
        {
            BindingFlags? bindingFlags;
            Result error = null;

            bindingFlags = GetBindingFlags(metaBindingFlags, null, ref error);

            if (bindingFlags != null)
                return (BindingFlags)bindingFlags;

            if (!noComplain)
            {
                DebugOps.Complain(ReturnCode.Error, String.Format(
                    "missing meta binding flags for {0}: {1}",
                    FormatOps.WrapOrNull(metaBindingFlags),
                    FormatOps.WrapOrNull(error)));
            }

            return (BindingFlags)0; /* BindingFlags.Default */
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method maps the specified <see cref="MetaBindingFlags" /> value
        /// to its corresponding combination of <see cref="BindingFlags" />
        /// flags, using the meta binding flags lookup table.
        /// </summary>
        /// <param name="metaBindingFlags">
        /// The meta binding flags value to map.
        /// </param>
        /// <param name="bindingFlags">
        /// The binding flags value to return when the mapping cannot be
        /// performed; this may be null.
        /// </param>
        /// <param name="error">
        /// Upon failure, this will contain an appropriate error message.
        /// </param>
        /// <returns>
        /// The mapped binding flags upon success, or the value of
        /// <paramref name="bindingFlags" /> upon failure.
        /// </returns>
        private static BindingFlags? GetBindingFlags(
            MetaBindingFlags metaBindingFlags, /* in */
            BindingFlags? bindingFlags,        /* in: OPTIONAL */
            ref Result error                   /* out */
            )
        {
            if (metaBindingFlagsMappings == null)
            {
                error = "no meta binding flags are available";
                return bindingFlags;
            }

            int length = metaBindingFlagsMappings.Length;
            int index = (int)(metaBindingFlags & MetaBindingFlags.IndexMask);

            if ((index < 0) || (index >= length))
            {
                error = String.Format(
                    "meta binding flags index {0} out of bounds 0 to {1}",
                    index, length - 1);

                return bindingFlags;
            }

            return metaBindingFlagsMappings[index];
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method removes the member types and binding flags that are
        /// considered unsafe from the specified values.
        /// </summary>
        /// <param name="memberTypes">
        /// The member types to be masked; upon return, the unsafe member types
        /// will have been removed.
        /// </param>
        /// <param name="bindingFlags">
        /// The binding flags to be masked; upon return, the unsafe binding
        /// flags will have been removed.
        /// </param>
        /// <param name="error">
        /// Upon failure, this will contain an appropriate error message.
        /// </param>
        /// <returns>
        /// Non-zero on success; otherwise, zero.
        /// </returns>
        private static bool MaskUnsafeMemberTypesAndBindingFlags(
            ref MemberTypes memberTypes,   /* in, out */
            ref BindingFlags bindingFlags, /* in, out */
            ref Result error               /* out */
            )
        {
            MemberTypes? unsafeMemberTypes = GetMemberTypes(
                MetaMemberTypes.UnsafeObject, null, ref error);

            if (unsafeMemberTypes == null)
                return false;

            BindingFlags? unsafeBindingFlags = GetBindingFlags(
                MetaBindingFlags.UnsafeObject, null, ref error);

            if (unsafeBindingFlags == null)
                return false;

            memberTypes &= ~(MemberTypes)unsafeMemberTypes;
            bindingFlags &= ~(BindingFlags)unsafeBindingFlags;

            return true;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method returns the default binding flags used by the object
        /// sub-command options.
        /// </summary>
        /// <returns>
        /// The default binding flags.
        /// </returns>
        public static BindingFlags GetDefaultBindingFlags()
        {
            return DefaultBindingFlags;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method returns the default marshalling flags used when
        /// converting values between the script and managed worlds.
        /// </summary>
        /// <returns>
        /// The default marshalling flags.
        /// </returns>
        public static MarshalFlags GetDefaultMarshalFlags()
        {
            return DefaultMarshalFlags;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method returns the default object flags applied to managed
        /// objects added to the interpreter.
        /// </summary>
        /// <returns>
        /// The default object flags.
        /// </returns>
        public static ObjectFlags GetDefaultObjectFlags()
        {
            return DefaultObjectFlags;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method returns the default object option type used when no
        /// specific option type is requested.
        /// </summary>
        /// <returns>
        /// The default object option type.
        /// </returns>
        public static ObjectOptionType GetDefaultObjectOptionType()
        {
            return DefaultObjectOptionType;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method returns the default value flags used when converting
        /// object values.
        /// </summary>
        /// <returns>
        /// The default object value flags.
        /// </returns>
        public static ValueFlags GetDefaultObjectValueFlags()
        {
            return DefaultObjectValueFlags;
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Type Support Methods
        /// <summary>
        /// This method retrieves the client data associated with the specified
        /// object, if it implements <see cref="IGetClientData" />.
        /// </summary>
        /// <param name="object">
        /// The object from which to retrieve the client data.
        /// </param>
        /// <returns>
        /// The client data associated with the object, or null if it does not
        /// expose any.
        /// </returns>
        public static IClientData GetClientData(
            object @object
            )
        {
            IGetClientData getClientData = @object as IGetClientData;

            if (getClientData != null)
                return getClientData.ClientData;
            else
                return null;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method builds the dictionary of default namespaces that are
        /// implicitly searched when resolving unqualified type names.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter context used when reporting any error encountered
        /// while building the dictionary.
        /// </param>
        /// <param name="namespaces">
        /// Upon return, this will contain the dictionary of default namespaces.
        /// </param>
        public static void GetNamespaces(
            Interpreter interpreter,
            out StringLongPairStringDictionary namespaces
            )
        {
            namespaces = new StringLongPairStringDictionary(true);

            ReturnCode code;
            Result error = null;

            code = AddNamespaces(
                ObjectNamespace.Default, ref namespaces, ref error);

            if (code != ReturnCode.Ok)
                DebugOps.Complain(interpreter, code, error);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method adds the configured default namespaces, selected by the
        /// specified flags, to the provided dictionary.
        /// </summary>
        /// <param name="flags">
        /// The flags that select which sets of default namespaces (e.g. Eagle
        /// and/or CLR) are added.
        /// </param>
        /// <param name="namespaces">
        /// The dictionary to which the selected namespaces are added; if it is
        /// null, a new dictionary will be created.
        /// </param>
        /// <param name="error">
        /// Upon failure, this will contain an appropriate error message.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise, an appropriate
        /// error code.
        /// </returns>
        public static ReturnCode AddNamespaces(
            ObjectNamespace flags,
            ref StringLongPairStringDictionary namespaces,
            ref Result error
            )
        {
            ResultList errors = null;

            if (FlagOps.HasFlags(flags, ObjectNamespace.Eagle, true))
            {
                if (DefaultEagleNamespaces != null)
                {
                    if (namespaces == null)
                        namespaces = new StringLongPairStringDictionary(true);

                    foreach (string @namespace in DefaultEagleNamespaces)
                    {
                        if (@namespace == null)
                            continue;

                        if (!namespaces.ContainsKey(@namespace))
                            namespaces.Add(@namespace, eagleSimpleName);
                    }
                }
                else
                {
                    if (errors == null)
                        errors = new ResultList();

                    errors.Add("object namespaces for Eagle not available");
                }
            }

            if (FlagOps.HasFlags(flags, ObjectNamespace.Clr, true))
            {
                if (DefaultClrNamespaces != null)
                {
                    if (namespaces == null)
                        namespaces = new StringLongPairStringDictionary(true);

                    foreach (string @namespace in DefaultClrNamespaces)
                    {
                        if (@namespace == null)
                            continue;

                        if (!namespaces.ContainsKey(@namespace))
                            namespaces.Add(@namespace, clrSimpleName);
                    }
                }
                else
                {
                    if (errors == null)
                        errors = new ResultList();

                    errors.Add("object namespaces for CLR not available");
                }
            }

            if (errors != null)
            {
                error = errors;
                return ReturnCode.Error;
            }
            else
            {
                return ReturnCode.Ok;
            }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Garbage Collection Support Methods
        /// <summary>
        /// This method determines whether the Eagle library should manually
        /// invoke garbage collection, based on the relevant environment
        /// variable.
        /// </summary>
        /// <returns>
        /// Non-zero if manual garbage collection is permitted; otherwise, zero.
        /// </returns>
        private static bool ShouldGC()
        {
            //
            // NOTE: If this environment variable is set, the Eagle library
            //       will never manually call into the GC to have it collect
            //       garbage; otherwise, manual calls into the GC will be
            //       enabled at certain strategic points in the code where
            //       it makes sense.
            //
            if (CommonOps.Environment.DoesVariableExist(EnvVars.NeverGC))
                return false;

            return true;
        }

        ///////////////////////////////////////////////////////////////////////

#if NET_451 || NET_452 || NET_46 || NET_461 || NET_462 || NET_47 || NET_471 || NET_472 || NET_48 || NET_481 || NET_STANDARD_20
        /// <summary>
        /// This method determines whether the large object heap should be
        /// compacted during garbage collection, based on the relevant
        /// environment variable and memory load.
        /// </summary>
        /// <returns>
        /// Non-zero if the large object heap should be compacted; otherwise,
        /// zero.
        /// </returns>
        private static bool ShouldCompactForGC()
        {
            //
            // NOTE: If this environment variable is set, the Eagle library
            //       will never compact the (large object?) heap; otherwise,
            //       it may be compacted when the memory load is high.
            //
            if (CommonOps.Environment.DoesVariableExist(
                    EnvVars.NeverCompactForGC))
            {
                return false;
            }

#if (ARGUMENT_CACHE || LIST_CACHE || PARSE_CACHE || TYPE_CACHE || COM_TYPE_CACHE) && NATIVE
            return CacheConfiguration.IsCompactMemoryLoadOk(CacheFlags.None);
#else
            return true;
#endif
        }
#endif

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method determines whether to wait for pending finalizers to
        /// finish after collecting garbage, based on the relevant environment
        /// variables and the current application domain.
        /// </summary>
        /// <returns>
        /// Non-zero if pending finalizers should be awaited; otherwise, zero.
        /// </returns>
        private static bool ShouldWaitForGC()
        {
            //
            // NOTE: If this environment variable is set, always wait for
            //       the GC to finish the pending finalizers; otherwise,
            //       we will only wait if this is the default application
            //       domain to prevent a subtle deadlock that can seemingly
            //       occur in applications that contain a user-interface
            //       that may be running in an isolated application domain
            //       (see below).  Otherwise, if the "opposite" environment
            //       variable is set, never wait for the GC to finish the
            //       pending finalizers.
            //
            if (CommonOps.Environment.DoesVariableExist(
                    EnvVars.AlwaysWaitForGC))
            {
                return true;
            }
            else if (CommonOps.Environment.DoesVariableExist(
                    EnvVars.NeverWaitForGC))
            {
                return false;
            }
            else
            {
                //
                // BUGBUG: Only wait for pending finalizers in the default
                //         application domain (due to potential deadlocks?).
                //         This seems to be related to the cross-AppDomain
                //         marshalling in .NET wanting to obtain a lock on
                //         the GC from two threads at the same time, which
                //         results in a deadlock.  This issue was observed
                //         in a WPF application loaded into an isolated
                //         application domain; therefore, this issue may be
                //         limited to applications that contain some kind
                //         of user-interface thread.
                //
                if (AppDomainOps.IsCurrentDefault())
                    return true;
            }

            return false;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method invokes the garbage collector for the specified
        /// generation, optionally compacting the large object heap.
        /// </summary>
        /// <param name="generation">
        /// The generation to collect, or -1 to collect all generations.
        /// </param>
        /// <param name="collectionMode">
        /// The garbage collection mode to use.
        /// </param>
        /// <param name="compact">
        /// Non-zero to compact the large object heap during this collection.
        /// </param>
        private static void CollectGarbage(
            int generation,
            GCCollectionMode collectionMode,
            bool compact
            ) /* throw */
        {
#if NET_451 || NET_452 || NET_46 || NET_461 || NET_462 || NET_47 || NET_471 || NET_472 || NET_48 || NET_481 || NET_STANDARD_20
            GCLargeObjectHeapCompactionMode savedLOHCompactionMode =
                GCSettings.LargeObjectHeapCompactionMode;

            if (compact)
            {
                GCSettings.LargeObjectHeapCompactionMode =
                    GCLargeObjectHeapCompactionMode.CompactOnce;
            }

            try
            {
#endif
                if (generation == -1)
                    GC.Collect();
                else
                    GC.Collect(generation, collectionMode);
#if NET_451 || NET_452 || NET_46 || NET_461 || NET_462 || NET_47 || NET_471 || NET_472 || NET_48 || NET_481 || NET_STANDARD_20
            }
            finally
            {
                if (compact)
                {
                    GCSettings.LargeObjectHeapCompactionMode =
                        savedLOHCompactionMode;
                }
            }
#endif
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method invokes the garbage collector using the default garbage
        /// collection flags.
        /// </summary>
        public static void CollectGarbage() /* throw */
        {
            CollectGarbage(GarbageFlags.Default);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method invokes the garbage collector using the specified
        /// garbage collection flags.
        /// </summary>
        /// <param name="flags">
        /// The flags controlling whether and how garbage is collected,
        /// compacted, and waited upon.
        /// </param>
        public static void CollectGarbage(
            GarbageFlags flags
            ) /* throw */
        {
            CollectGarbage(-1, GCCollectionMode.Default, flags);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method invokes the garbage collector for the specified
        /// generation, using the specified garbage collection flags to control
        /// whether garbage is collected, whether the large object heap is
        /// compacted, and whether pending finalizers are awaited.
        /// </summary>
        /// <param name="generation">
        /// The generation to collect, or -1 to collect all generations.
        /// </param>
        /// <param name="collectionMode">
        /// The garbage collection mode to use.
        /// </param>
        /// <param name="flags">
        /// The flags controlling whether and how garbage is collected,
        /// compacted, and waited upon.
        /// </param>
        private static void CollectGarbage(
            int generation,
            GCCollectionMode collectionMode,
            GarbageFlags flags
            ) /* throw */
        {
            if (FlagOps.HasFlags(flags, GarbageFlags.AlwaysCollect, true))
            {
                //
                // NOTE: Do nothing.  The garbage will be collected below.
                //
            }
            else if (FlagOps.HasFlags(flags, GarbageFlags.NeverCollect, true))
            {
                //
                // NOTE: Garbage collection has been disabled by the caller,
                //       just return now.
                //
                return;
            }
            else if (FlagOps.HasFlags(flags, GarbageFlags.MaybeCollect, true))
            {
                //
                // NOTE: Attempt to automatically detect whether or not we
                //       should actually collect any garbage.
                //
                if (!ShouldGC())
                    return;
            }

            ///////////////////////////////////////////////////////////////////

#if NET_451 || NET_452 || NET_46 || NET_461 || NET_462 || NET_47 || NET_471 || NET_472 || NET_48 || NET_481 || NET_STANDARD_20
            bool reallyCompact;

            if (FlagOps.HasFlags(flags, GarbageFlags.AlwaysCompact, true))
            {
                //
                // NOTE: Yes, we should compact the (large object?) heap.
                //
                reallyCompact = true;
            }
            else if (FlagOps.HasFlags(flags, GarbageFlags.NeverCompact, true))
            {
                //
                // NOTE: No, we should not compact the (large object?) heap.
                //
                reallyCompact = false;
            }
            else if (FlagOps.HasFlags(flags, GarbageFlags.MaybeCompact, true))
            {
                //
                // NOTE: Attempt to automatically detect whether or not we
                //       should compact the (large object?) heap.
                //
                reallyCompact = ShouldCompactForGC();
            }
            else
            {
                //
                // NOTE: Fallback to the value configured as the default for
                //       this class.
                //
                reallyCompact = DefaultCompactLargeObjectHeap;
            }
#endif

            ///////////////////////////////////////////////////////////////////

            bool reallyWait;

            if (FlagOps.HasFlags(flags, GarbageFlags.AlwaysWait, true))
            {
                //
                // NOTE: Yes, we should wait for all pending finalizers.
                //
                reallyWait = true;
            }
            else if (FlagOps.HasFlags(flags, GarbageFlags.NeverWait, true))
            {
                //
                // NOTE: No, we should not wait for all pending finalizers.
                //
                reallyWait = false;
            }
            else if (FlagOps.HasFlags(flags, GarbageFlags.MaybeWait, true))
            {
                //
                // NOTE: Attempt to automatically detect whether or not we
                //       should wait for the pending finalizers to finish.
                //
                reallyWait = ShouldWaitForGC();
            }
            else
            {
                //
                // NOTE: Fallback to the value configured as the default for
                //       this class.
                //
                reallyWait = DefaultWaitForGC;
            }

            ///////////////////////////////////////////////////////////////////

#if NET_451 || NET_452 || NET_46 || NET_461 || NET_462 || NET_47 || NET_471 || NET_472 || NET_48 || NET_481 || NET_STANDARD_20
            CollectGarbage(generation, collectionMode, reallyCompact);
#else
            CollectGarbage(generation, collectionMode, false);
#endif

            if (reallyWait)
            {
                GC.WaitForPendingFinalizers();

#if NET_451 || NET_452 || NET_46 || NET_461 || NET_462 || NET_47 || NET_471 || NET_472 || NET_48 || NET_481 || NET_STANDARD_20
                CollectGarbage(generation, collectionMode, reallyCompact);
#else
                CollectGarbage(generation, collectionMode, false);
#endif
            }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method computes the change in total managed memory that results
        /// from optionally performing a garbage collection.
        /// </summary>
        /// <param name="collect">
        /// Non-zero to perform a garbage collection when measuring the memory
        /// usage after the initial measurement.
        /// </param>
        /// <returns>
        /// The number of bytes by which the total managed memory decreased.
        /// </returns>
        public static long GetTotalMemory(
            bool collect
            )
        {
            long beforeBytes = 0;
            long afterBytes = 0;

            GetTotalMemory(collect, ref beforeBytes, ref afterBytes);

            return (beforeBytes - afterBytes);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method measures the total managed memory in use before and,
        /// optionally, after performing a garbage collection.
        /// </summary>
        /// <param name="collect">
        /// Non-zero to perform a garbage collection before taking the second
        /// measurement.
        /// </param>
        /// <param name="beforeBytes">
        /// Upon return, this will contain the total managed memory, in bytes,
        /// measured before any garbage collection.
        /// </param>
        /// <param name="afterBytes">
        /// Upon return, this will contain the total managed memory, in bytes,
        /// measured after garbage collection, if it was requested.
        /// </param>
        public static void GetTotalMemory(
            bool collect,
            ref long beforeBytes,
            ref long afterBytes
            )
        {
            beforeBytes = GC.GetTotalMemory(false);

            if (collect)
                afterBytes = GC.GetTotalMemory(true);
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Object Option Support Methods
        #region Object Option Translation Methods
        //
        // HACK: This is for use by the test suite only.
        //
        /// <summary>
        /// This method queries, enables, or disables the "guru" (i.e. advanced)
        /// object access mode by adding or removing the guru namespace and the
        /// associated default binding flags.  It is intended for use by the
        /// test suite only.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter whose object namespaces are queried or modified.
        /// </param>
        /// <param name="enable">
        /// Non-zero to enable the guru mode, zero to disable it, or null to
        /// only query its current state.
        /// </param>
        /// <param name="result">
        /// Upon return, this will contain the detailed list of actions taken
        /// or, upon failure, an appropriate error message.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise, an appropriate
        /// error code.
        /// </returns>
        public static ReturnCode MaybeEnableGuru(
            Interpreter interpreter,
            bool? enable,
            ref Result result
            )
        {
            ResultList results = new ResultList();
            bool enabled = false;
            Result error; /* REUSED */

            try
            {
                BindingFlags guruBindingFlags = GetBindingFlags(
                    MetaBindingFlags.Guru, true);

                if (enable == null)
                {
                    ReturnCode code;
                    int matched = 0;

                    error = null;

                    code = interpreter.MatchObjectNamespace(
                        MatchMode.Exact, guruNamespace, false, false,
                        ref matched, ref error);

                    if (code == ReturnCode.Ok)
                    {
                        if (matched > 0)
                            enabled = true;

                        results.Add(String.Format(
                            "found {0} object namespace(s) matching {1}",
                            matched, FormatOps.WrapOrNull(guruNamespace)));
                    }
                    else
                    {
                        results.Add(error);
                    }

                    if (FlagOps.HasFlags(
                            DefaultBindingFlags, guruBindingFlags, false))
                    {
                        enabled = true;

                        BindingFlags matchedBindingFlags =
                            DefaultBindingFlags & guruBindingFlags;

                        results.Add(String.Format(
                            "found {0} in default object binding flags",
                            FormatOps.WrapOrNull(matchedBindingFlags)));
                    }
                    else
                    {
                        results.Add(String.Format(
                            "missing {0} in default object binding flags",
                            FormatOps.WrapOrNull(guruBindingFlags)));
                    }

                    results.Add(enabled); /* NOTE: Anything was enabled? */
                    return code;
                }
                else if ((bool)enable)
                {
                    StringLongPairStringDictionary dictionary =
                        new StringLongPairStringDictionary(true);

                    dictionary.Add(guruNamespace, eagleSimpleName);

                    int added = 0;

                    error = null;

                    if (interpreter.AddObjectNamespaces(
                            dictionary, MatchMode.None, null, false,
                            ref added, ref error) == ReturnCode.Ok)
                    {
                        if (added > 0)
                            enabled = true;

                        results.Add(String.Format(
                            "added {0} object namespace(s) matching {1} from {2}",
                            added, FormatOps.WrapOrNull(guruNamespace),
                            FormatOps.WrapOrNull(eagleSimpleName)));
                    }
                    else
                    {
                        results.Add(error);
                        return ReturnCode.Error;
                    }

                    if (!FlagOps.HasFlags(
                            DefaultBindingFlags, guruBindingFlags, true))
                    {
                        DefaultBindingFlags |= guruBindingFlags;
                        enabled = true;

                        results.Add(String.Format(
                            "added {0} to default object binding flags",
                            FormatOps.WrapOrNull(guruBindingFlags)));
                    }
                    else
                    {
                        results.Add(String.Format(
                            "found {0} in default object binding flags",
                            FormatOps.WrapOrNull(guruBindingFlags)));
                    }

                    results.Add(enabled); /* NOTE: Anything just enabled? */
                }
                else
                {
                    int removed = 0;

                    error = null;

                    if (interpreter.RemoveObjectNamespaces(
                            MatchMode.Exact, guruNamespace, false, false,
                            ref removed, ref error) == ReturnCode.Ok)
                    {
                        if (removed > 0)
                            enabled = true;

                        results.Add(String.Format(
                            "removed {0} object namespace(s) matching {1}",
                            removed, FormatOps.WrapOrNull(guruNamespace)));
                    }
                    else
                    {
                        results.Add(error);
                        return ReturnCode.Error;
                    }

                    if (FlagOps.HasFlags(
                            DefaultBindingFlags, guruBindingFlags, false))
                    {
                        DefaultBindingFlags &= ~guruBindingFlags;
                        enabled = true;

                        results.Add(String.Format(
                            "removed {0} from default object binding flags",
                            FormatOps.WrapOrNull(guruBindingFlags)));
                    }
                    else
                    {
                        results.Add(String.Format(
                            "missing {0} in default object binding flags",
                            FormatOps.WrapOrNull(guruBindingFlags)));
                    }

                    results.Add(enabled); /* NOTE: Anything just disabled? */
                }

                return ReturnCode.Ok;
            }
            finally
            {
                result = results;
            }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method selects the object option type to use for an object
        /// invocation, based on whether raw and/or all-overload semantics are
        /// requested.
        /// </summary>
        /// <param name="raw">
        /// Non-zero to select the raw invocation option type.
        /// </param>
        /// <param name="all">
        /// Non-zero to select the all-overload invocation option type, which
        /// takes precedence over <paramref name="raw" />.
        /// </param>
        /// <returns>
        /// The selected object option type.
        /// </returns>
        public static ObjectOptionType GetOptionType(
            bool raw,
            bool all
            )
        {
            if (all)
                return ObjectOptionType.InvokeAll;

            if (raw)
                return ObjectOptionType.InvokeRaw;

            return ObjectOptionType.Invoke;
        }

        ///////////////////////////////////////////////////////////////////////

        //
        // NOTE: This is for use with the MarshalOps.FixupReturnValue and
        //       MarshalOps.FixupByRefArguments methods.
        //
        /// <summary>
        /// This method returns the option dictionary appropriate for the
        /// specified invoke-related object option type, for use with the
        /// MarshalOps.FixupReturnValue and MarshalOps.FixupByRefArguments
        /// methods.
        /// </summary>
        /// <param name="objectOptionType">
        /// The object option type whose invoke-related options are requested.
        /// </param>
        /// <returns>
        /// The option dictionary for the requested invoke option type, or null
        /// if the masked option type does not denote a single invoke option
        /// type.
        /// </returns>
        public static OptionDictionary GetInvokeOptions(
            ObjectOptionType objectOptionType
            )
        {
            //
            // NOTE: Enforce the logical union of alias option types here,
            //       via all return paths.  In this case, if more than one
            //       invoke option type is specified, the return value will
            //       be null.
            //
            ObjectOptionType maskedObjectOptionType =
                objectOptionType & ObjectOptionType.InvokeOptionMask;

            if ((maskedObjectOptionType == ObjectOptionType.Call) ||
                (maskedObjectOptionType == ObjectOptionType.Invoke) ||
                (maskedObjectOptionType == ObjectOptionType.InvokeRaw) ||
                (maskedObjectOptionType == ObjectOptionType.InvokeAll))
            {
                return GetObjectOptions(maskedObjectOptionType);
            }

            return null;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method derives the object option type to use for the aliases
        /// created from by-reference (ref or out) arguments, combining the
        /// unrelated bits of the supplied option type with the invoke option
        /// type selected by the by-reference argument flags.
        /// </summary>
        /// <param name="objectOptionType">
        /// The base object option type whose invoke-related bits are replaced.
        /// </param>
        /// <param name="byRefArgumentFlags">
        /// The by-reference argument flags that select the invoke option type
        /// (e.g. all, raw, or normal).
        /// </param>
        /// <returns>
        /// The derived object option type.
        /// </returns>
        public static ObjectOptionType GetByRefOptionType(
            ObjectOptionType objectOptionType,
            ByRefArgumentFlags byRefArgumentFlags
            )
        {
            //
            // NOTE: Mask off the unrelated object option types first.
            //
            ObjectOptionType maskedObjectOptionType =
                objectOptionType & ~ObjectOptionType.InvokeOptionMask;

            //
            // NOTE: Enforce the logical union of alias option types here,
            //       via all return paths.
            //
            if (FlagOps.HasFlags(
                    byRefArgumentFlags, ByRefArgumentFlags.AliasAll, true))
            {
                return maskedObjectOptionType | ObjectOptionType.InvokeAll;
            }
            else if (FlagOps.HasFlags(
                    byRefArgumentFlags, ByRefArgumentFlags.AliasRaw, true))
            {
                return maskedObjectOptionType | ObjectOptionType.InvokeRaw;
            }

            return maskedObjectOptionType | ObjectOptionType.Invoke;
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Object Option "Factory" Methods
        //
        // NOTE: This is for the [object alias] sub-command.
        //
        /// <summary>
        /// This method builds the option dictionary used by the [object alias]
        /// sub-command.
        /// </summary>
        /// <returns>
        /// The newly created option dictionary.
        /// </returns>
        public static OptionDictionary GetAliasOptions()
        {
            return new OptionDictionary(
                new IOption[] {
                new Option(null, OptionFlags.MustHaveTypeListValue,
                    Index.Invalid, Index.Invalid, "-objecttypes", null),
                new Option(null, OptionFlags.MustHaveValue, Index.Invalid,
                    Index.Invalid, "-aliasname", null),
                new Option(null, OptionFlags.None, Index.Invalid,
                    Index.Invalid, "-aliasraw", null),
                new Option(null, OptionFlags.None, Index.Invalid,
                    Index.Invalid, "-aliasall", null),
                new Option(null, OptionFlags.None, Index.Invalid,
                    Index.Invalid, "-aliasreference", null),
                new Option(null, OptionFlags.None, Index.Invalid,
                    Index.Invalid, "-nocase", null),
                new Option(null, OptionFlags.None, Index.Invalid,
                    Index.Invalid, "-stricttype", null),
                new Option(null, VerboseOptionFlags, Index.Invalid,
                    Index.Invalid, "-verbose", null),
                Option.CreateEndOfOptions()
            });
        }

        ///////////////////////////////////////////////////////////////////////

        //
        // NOTE: This is primarily for the [library call] sub-command.
        //
        /// <summary>
        /// This method builds the option dictionary used primarily by the
        /// [library call] sub-command and the InvokeDelegate method.
        /// </summary>
        /// <returns>
        /// The newly created option dictionary.
        /// </returns>
        private static OptionDictionary GetCallOptions()
        {
            //
            // NOTE: These options are used by both the InvokeDelegate method
            //       (below) and the code for the [library call] command.
            //       Normally, this method would simply call into a static
            //       method exported from the _Commands.Library class; however,
            //       that class is only available when the library has been
            //       compiled with native code enabled; therefore, we define
            //       the actual options here and both the _Commands.Library
            //       class and the InvokeDelegate method can simply call us.
            //
            return new OptionDictionary(
                new IOption[] {
                new Option(typeof(DateTimeKind),
                    OptionFlags.MustHaveEnumValue, Index.Invalid,
                    Index.Invalid, "-datetimekind",
                    new Variant(DefaultDateTimeKind)),
                new Option(typeof(DateTimeStyles),
                    OptionFlags.MustHaveEnumValue, Index.Invalid,
                    Index.Invalid, "-datetimestyles",
                    new Variant(DefaultDateTimeStyles)),
                new Option(null, OptionFlags.MustHaveValue, Index.Invalid,
                    Index.Invalid, "-datetimeformat",
                    new Variant(DefaultDateTimeFormat)),
                new Option(null, OptionFlags.MustHaveValue, Index.Invalid,
                    Index.Invalid, "-objectname", null),
                new Option(null, OptionFlags.MustHaveTypeValue, Index.Invalid,
                    Index.Invalid, "-type", null),
                new Option(typeof(ByRefArgumentFlags),
                    OptionFlags.MustHaveEnumValue, Index.Invalid,
                    Index.Invalid, "-argumentflags",
                    new Variant(DefaultByRefArgumentFlags)),
                new Option(typeof(MarshalFlags),
                    OptionFlags.MustHaveEnumValue, Index.Invalid,
                    Index.Invalid, "-marshalflags",
                    new Variant(DefaultMarshalFlags)),
                new Option(null, CreateOptionFlags, Index.Invalid,
                    Index.Invalid, "-create", null),
                new Option(null, NoDisposeOptionFlags, Index.Invalid,
                    Index.Invalid, "-nodispose", null),
                new Option(null, OptionFlags.None, Index.Invalid,
                    Index.Invalid, "-noinvoke", null),
                new Option(null, OptionFlags.None, Index.Invalid,
                    Index.Invalid, "-help", null),
                new Option(null, OptionFlags.None, Index.Invalid,
                    Index.Invalid, "-noargs", null),
                new Option(null, OptionFlags.MustHaveIntegerValue,
                    Index.Invalid, Index.Invalid, "-limit", null),
                new Option(null, OptionFlags.MustHaveIntegerValue,
                    Index.Invalid, Index.Invalid, "-index", null),
                new Option(null, OptionFlags.MustHaveIntegerValue,
                    Index.Invalid, Index.Invalid, "-autolimit", null),
                new Option(null, OptionFlags.MustHaveIntegerValue,
                    Index.Invalid, Index.Invalid, "-autoindex", null),
                new Option(null, OptionFlags.MustHaveBooleanValue,
                    Index.Invalid, Index.Invalid, "-autocreate", null),
                new Option(null, OptionFlags.MustHaveBooleanValue,
                    Index.Invalid, Index.Invalid, "-autoflush", null),
                new Option(null, OptionFlags.MustHaveBooleanValue,
                    Index.Invalid, Index.Invalid, "-autostatus", null),
                new Option(null, AliasOptionFlags, Index.Invalid,
                    Index.Invalid, "-alias", null),
                new Option(null, OptionFlags.None, Index.Invalid,
                    Index.Invalid, "-aliasraw", null),
                new Option(null, OptionFlags.None, Index.Invalid,
                    Index.Invalid, "-aliasall", null),
                new Option(null, OptionFlags.None, Index.Invalid,
                    Index.Invalid, "-aliasreference", null),
#if NATIVE && TCL
                new Option(null, OptionFlags.MustHaveTclInterpreterValue,
                    Index.Invalid, Index.Invalid, "-tcl", null),
#else
                new Option(null, OptionFlags.MustHaveValue |
                    OptionFlags.Unsupported, Index.Invalid,
                    Index.Invalid, "-tcl", null),
#endif
                new Option(null, OptionFlags.None, Index.Invalid,
                    Index.Invalid, "-noforcedelete", null),
                new Option(null, OptionFlags.None, Index.Invalid,
                    Index.Invalid, "-tostring", null),
                new Option(null, ArrayAsValueOptionFlags, Index.Invalid,
                    Index.Invalid, "-arrayasvalue", null),
                new Option(null, ArrayAsLinkOptionFlags, Index.Invalid,
                    Index.Invalid, "-arrayaslink", null),
                new Option(null, OptionFlags.None, Index.Invalid,
                    Index.Invalid, "-nocase", null),
                new Option(null, OptionFlags.None, Index.Invalid,
                    Index.Invalid, "-default", null),
                new Option(null, OptionFlags.None, Index.Invalid,
                    Index.Invalid, "-strictmember", null),
                new Option(null, OptionFlags.None, Index.Invalid,
                    Index.Invalid, "-strictargs", null),
                new Option(null, VerboseOptionFlags, Index.Invalid,
                    Index.Invalid, "-verbose", null),
                new Option(null, DebugOptionFlags, Index.Invalid,
                    Index.Invalid, "-debug", null),
                new Option(null, TraceOptionFlags, Index.Invalid,
                    Index.Invalid, "-trace", null),
                new Option(null, OptionFlags.None, Index.Invalid,
                    Index.Invalid, "-nobyref", null),
                new Option(typeof(BindingFlags), OptionFlags.MustHaveEnumValue,
                    Index.Invalid, Index.Invalid, "-flags",
                    new Variant(DefaultBindingFlags)),
                new Option(typeof(BindingFlags), OptionFlags.MustHaveEnumValue,
                    Index.Invalid, Index.Invalid, "-bindingflags",
                    new Variant(DefaultBindingFlags)),
                new Option(typeof(ObjectFlags), OptionFlags.MustHaveEnumValue,
                    Index.Invalid, Index.Invalid, "-objectflags",
                    new Variant(DefaultObjectFlags)),
                new Option(typeof(ObjectFlags), OptionFlags.MustHaveEnumValue,
                    Index.Invalid, Index.Invalid, "-byrefobjectflags",
                    new Variant(DefaultByRefObjectFlags)),
                Option.CreateEndOfOptions()
            });
        }

        ///////////////////////////////////////////////////////////////////////

        //
        // NOTE: This is for the ToCommandCallback method.
        //
        // NOTE: This method must use the "Unsafe" option flag to prevent a
        //       "safe" interpreter from potentially using an option.
        //
        /// <summary>
        /// This method builds the option dictionary used by the
        /// ToCommandCallback method.  It uses the "Unsafe" option flag to
        /// prevent a "safe" interpreter from potentially using an option.
        /// </summary>
        /// <returns>
        /// The newly created option dictionary.
        /// </returns>
        private static OptionDictionary GetCallbackOptions()
        {
            //
            // HACK: The "-identifier" option here is special.  It is NOT
            //       actually processed by the core library; instead, it
            //       should be used in situations where there may be more
            //       than one outstanding (asynchronous?, fire-and-forget?)
            //       callback pending, so cleaning up (i.e. removing) one
            //       does not impact the others.  It requires a value and
            //       should be included like this to be effective:
            //
            //                -identifier [expr {random()}]
            //
            return new OptionDictionary(
                new IOption[] {
                new Option(null, OptionFlags.MustHaveValue, /* NOT USED */
                    Index.Invalid, Index.Invalid, "-identifier", null),
                new Option(null,
                    OptionFlags.MustHaveTypeValue, /* SECURITY: OK */
                    Index.Invalid, Index.Invalid, "-returntype", null),
                new Option(null, OptionFlags.MustHaveTypeListValue,
                    Index.Invalid, Index.Invalid, "-parametertypes", null),
                new Option(typeof(MarshalFlags),
                    OptionFlags.MustHaveEnumListValue | OptionFlags.Unsafe,
                    Index.Invalid, Index.Invalid, "-parametermarshalflags",
                    new Variant(DefaultParameterMarshalFlags)),
                new Option(typeof(ByRefArgumentFlags),
                    OptionFlags.MustHaveEnumValue | OptionFlags.Unsafe,
                    Index.Invalid, Index.Invalid, "-argumentflags",
                    new Variant(DefaultByRefArgumentFlags)),
                new Option(typeof(MarshalFlags),
                    OptionFlags.MustHaveEnumValue | OptionFlags.Unsafe,
                    Index.Invalid, Index.Invalid, "-marshalflags",
                    new Variant(DefaultMarshalFlags)),
                new Option(typeof(ObjectFlags),
                    OptionFlags.MustHaveEnumValue | OptionFlags.Unsafe,
                    Index.Invalid, Index.Invalid, "-objectflags",
                    new Variant(DefaultObjectFlags)),
                new Option(typeof(CallbackFlags),
                    OptionFlags.MustHaveEnumValue | OptionFlags.Unsafe,
                    Index.Invalid, Index.Invalid, "-callbackflags",
                    new Variant(DefaultCallbackFlags)),
                Option.CreateEndOfOptions()
            });
        }

        ///////////////////////////////////////////////////////////////////////

        //
        // NOTE: This is for the [library certificate] and
        //       [object certificate] sub-commands.
        //
        /// <summary>
        /// This method builds the option dictionary used by the
        /// [library certificate] and [object certificate] sub-commands.
        /// </summary>
        /// <returns>
        /// The newly created option dictionary.
        /// </returns>
        private static OptionDictionary GetCertificateOptions()
        {
            X509VerificationFlags localX509VerificationFlags;
            X509RevocationMode localX509RevocationMode;
            X509RevocationFlag localX509RevocationFlag;

            CertificateOps.QueryFlags(
                out localX509VerificationFlags,
                out localX509RevocationMode,
                out localX509RevocationFlag);

            return new OptionDictionary(
                new IOption[] {
                new Option(null, OptionFlags.None, Index.Invalid,
                    Index.Invalid, "-cache", null),
                new Option(null, OptionFlags.None, Index.Invalid,
                    Index.Invalid, "-chain", null),
                new Option(typeof(X509VerificationFlags),
                    OptionFlags.MustHaveEnumValue, Index.Invalid,
                    Index.Invalid, "-verificationflags",
                    new Variant(localX509VerificationFlags)),
                new Option(typeof(X509RevocationMode),
                    OptionFlags.MustHaveEnumValue, Index.Invalid,
                    Index.Invalid, "-revocationmode",
                    new Variant(localX509RevocationMode)),
                new Option(typeof(X509RevocationFlag),
                    OptionFlags.MustHaveEnumValue, Index.Invalid,
                    Index.Invalid, "-revocationflag",
                    new Variant(localX509RevocationFlag)),
                Option.CreateEndOfOptions()
            });
        }

        ///////////////////////////////////////////////////////////////////////

        //
        // NOTE: This is for the [object cleanup] sub-command.
        //
        /// <summary>
        /// This method builds the option dictionary used by the
        /// [object cleanup] sub-command.
        /// </summary>
        /// <returns>
        /// The newly created option dictionary.
        /// </returns>
        private static OptionDictionary GetCleanupOptions()
        {
            return new OptionDictionary(
                new IOption[] {
                new Option(null, OptionFlags.MustHaveValue, Index.Invalid,
                    Index.Invalid, "-pattern", null),
                new Option(null, OptionFlags.MustHaveIntegerValue,
                    Index.Invalid, Index.Invalid, "-referencecount", null),
                new Option(null, OptionFlags.None, Index.Invalid,
                    Index.Invalid, "-references", null),
                new Option(null, OptionFlags.None, Index.Invalid,
                    Index.Invalid, "-noremove", null),
                new Option(null, SynchronousOptionFlags, Index.Invalid,
                    Index.Invalid, "-synchronous", null),
                new Option(null, NoDisposeOptionFlags, Index.Invalid,
                    Index.Invalid, "-nodispose", null),
                new Option(null, OptionFlags.None, Index.Invalid,
                    Index.Invalid, "-nocomplain", null),
                Option.CreateEndOfOptions()
            });
        }

        ///////////////////////////////////////////////////////////////////////

        //
        // NOTE: This is for the [object create] sub-command.
        //
        /// <summary>
        /// This method builds the option dictionary used by the [object create]
        /// sub-command.
        /// </summary>
        /// <returns>
        /// The newly created option dictionary.
        /// </returns>
        private static OptionDictionary GetCreateOptions()
        {
            return new OptionDictionary(
                new IOption[] {
                new Option(null, OptionFlags.MustHaveValue, Index.Invalid,
                    Index.Invalid, "-objectname", null),
                new Option(null, OptionFlags.MustHaveTypeValue,
                    Index.Invalid, Index.Invalid, "-type", null),
                new Option(null, OptionFlags.MustHaveTypeListValue,
                    Index.Invalid, Index.Invalid, "-objecttypes", null),
                new Option(null, OptionFlags.MustHaveTypeListValue,
                    Index.Invalid, Index.Invalid, "-methodtypes", null),
                new Option(null, OptionFlags.MustHaveTypeListValue,
                    Index.Invalid, Index.Invalid, "-parametertypes", null),
                new Option(typeof(MarshalFlags),
                    OptionFlags.MustHaveEnumListValue, Index.Invalid,
                    Index.Invalid, "-parametermarshalflags",
                    new Variant(DefaultParameterMarshalFlags)),
                new Option(null, DebugOptionFlags, Index.Invalid,
                    Index.Invalid, "-debug", null),
                new Option(null, TraceOptionFlags, Index.Invalid,
                    Index.Invalid, "-trace", null),
                new Option(typeof(ByRefArgumentFlags),
                    OptionFlags.MustHaveEnumValue, Index.Invalid,
                    Index.Invalid, "-argumentflags",
                    new Variant(DefaultByRefArgumentFlags)),
                new Option(typeof(ValueFlags),
                    OptionFlags.MustHaveEnumValue, Index.Invalid,
                    Index.Invalid, "-objectvalueflags",
                    new Variant(DefaultObjectValueFlags)),
                new Option(typeof(MarshalFlags),
                    OptionFlags.MustHaveEnumValue, Index.Invalid,
                    Index.Invalid, "-marshalflags",
                    new Variant(DefaultMarshalFlags)),
                new Option(typeof(ReorderFlags),
                    OptionFlags.MustHaveEnumValue, Index.Invalid,
                    Index.Invalid, "-reorderflags",
                    new Variant(DefaultReorderFlags)),
                new Option(null, NoCreateOptionFlags, Index.Invalid,
                    Index.Invalid, "-nocreate", null),
                new Option(null, NoDisposeOptionFlags, Index.Invalid,
                    Index.Invalid, "-nodispose", null),
                new Option(null, OptionFlags.None, Index.Invalid,
                    Index.Invalid, "-noinvoke", null),
                new Option(null, OptionFlags.None, Index.Invalid,
                    Index.Invalid, "-help", null),
                new Option(null, OptionFlags.None, Index.Invalid,
                    Index.Invalid, "-noargs", null),
                new Option(null, OptionFlags.MustHaveIntegerValue,
                    Index.Invalid, Index.Invalid, "-limit", null),
                new Option(null, OptionFlags.MustHaveIntegerValue,
                    Index.Invalid, Index.Invalid, "-index", null),
                new Option(null, AliasOptionFlags, Index.Invalid,
                    Index.Invalid, "-alias", null),
                new Option(null, OptionFlags.None, Index.Invalid,
                    Index.Invalid, "-aliasraw", null),
                new Option(null, OptionFlags.None, Index.Invalid,
                    Index.Invalid, "-aliasall", null),
                new Option(null, OptionFlags.None, Index.Invalid,
                    Index.Invalid, "-aliasreference", null),
#if NATIVE && TCL
                new Option(null, OptionFlags.MustHaveTclInterpreterValue,
                    Index.Invalid, Index.Invalid, "-tcl", null),
#else
                new Option(null, OptionFlags.MustHaveValue |
                    OptionFlags.Unsupported, Index.Invalid,
                    Index.Invalid, "-tcl", null),
#endif
                new Option(null, OptionFlags.None, Index.Invalid,
                    Index.Invalid, "-noforcedelete", null),
                new Option(null, OptionFlags.None, Index.Invalid,
                    Index.Invalid, "-tostring", null),
                new Option(null, ArrayAsValueOptionFlags, Index.Invalid,
                    Index.Invalid, "-arrayasvalue", null),
                new Option(null, ArrayAsLinkOptionFlags, Index.Invalid,
                    Index.Invalid, "-arrayaslink", null),
                new Option(null, OptionFlags.None, Index.Invalid,
                    Index.Invalid, "-nomutatebindingflags", null),
                new Option(null, OptionFlags.None, Index.Invalid,
                    Index.Invalid, "-stricttype", null),
                new Option(null, OptionFlags.None, Index.Invalid,
                    Index.Invalid, "-strictmember", null),
                new Option(null, OptionFlags.None, Index.Invalid,
                    Index.Invalid, "-strictargs", null),
                new Option(null, OptionFlags.None, Index.Invalid,
                    Index.Invalid, "-nocase", null),
                new Option(null, OptionFlags.None, Index.Invalid,
                    Index.Invalid, "-default", null),
                new Option(null, VerboseOptionFlags, Index.Invalid,
                    Index.Invalid, "-verbose", null),
                new Option(null, OptionFlags.None, Index.Invalid,
                    Index.Invalid, "-nobyref", null),
                new Option(typeof(BindingFlags), OptionFlags.MustHaveEnumValue,
                    Index.Invalid, Index.Invalid, "-flags",
                    new Variant(DefaultBindingFlags)),
                new Option(typeof(BindingFlags), OptionFlags.MustHaveEnumValue,
                    Index.Invalid, Index.Invalid, "-bindingflags",
                    new Variant(DefaultBindingFlags)),
                new Option(typeof(ObjectFlags), OptionFlags.MustHaveEnumValue,
                    Index.Invalid, Index.Invalid, "-objectflags",
                    new Variant(DefaultObjectFlags)),
                new Option(typeof(ObjectFlags), OptionFlags.MustHaveEnumValue,
                    Index.Invalid, Index.Invalid, "-byrefobjectflags",
                    new Variant(DefaultByRefObjectFlags)),
                Option.CreateEndOfOptions()
            });
        }

        ///////////////////////////////////////////////////////////////////////

        //
        // NOTE: This is for the [object declare] sub-command.
        //
        /// <summary>
        /// This method builds the option dictionary used by the
        /// [object declare] sub-command.
        /// </summary>
        /// <returns>
        /// The newly created option dictionary.
        /// </returns>
        private static OptionDictionary GetDeclareOptions()
        {
            return new OptionDictionary(
                new IOption[] {
                new Option(null, OptionFlags.None, Index.Invalid,
                    Index.Invalid, "-stricttype", null),
                new Option(null, VerboseOptionFlags, Index.Invalid,
                    Index.Invalid, "-verbose", null),
                new Option(null, OptionFlags.None, Index.Invalid,
                    Index.Invalid, "-declarenonpublic", null),
                new Option(null, OptionFlags.MustHaveMatchModeValue,
                    Index.Invalid, Index.Invalid, "-declaremode",
                    new Variant(DefaultMatchMode)),
                new Option(null, OptionFlags.MustHaveValue,
                    Index.Invalid, Index.Invalid, "-declarepattern", null),
                new Option(null, OptionFlags.None, Index.Invalid,
                    Index.Invalid, "-declarenocase", null),
                Option.CreateEndOfOptions()
            });
        }

        ///////////////////////////////////////////////////////////////////////

#if CALLBACK_QUEUE
        //
        // NOTE: This is for the [callback dequeue] sub-command.
        //
        /// <summary>
        /// This method builds the option dictionary used by the
        /// [callback dequeue] sub-command.
        /// </summary>
        /// <returns>
        /// The newly created option dictionary.
        /// </returns>
        private static OptionDictionary GetDequeueOptions()
        {
            return new OptionDictionary(
                new IOption[] {
                new Option(null, OptionFlags.MustHaveValue, Index.Invalid,
                    Index.Invalid, "-objectname", null),
                new Option(null, OptionFlags.MustHaveTypeValue, Index.Invalid,
                    Index.Invalid, "-type", null),
                new Option(null, CreateOptionFlags, Index.Invalid,
                    Index.Invalid, "-create", null),
                new Option(null, NoDisposeOptionFlags, Index.Invalid,
                    Index.Invalid, "-nodispose", null),
                new Option(null, AliasOptionFlags, Index.Invalid,
                    Index.Invalid, "-alias", null),
                new Option(null, OptionFlags.None, Index.Invalid,
                    Index.Invalid, "-aliasraw", null),
                new Option(null, OptionFlags.None, Index.Invalid,
                    Index.Invalid, "-aliasall", null),
                new Option(null, OptionFlags.None, Index.Invalid,
                    Index.Invalid, "-aliasreference", null),
#if NATIVE && TCL
                new Option(null, OptionFlags.MustHaveTclInterpreterValue,
                    Index.Invalid, Index.Invalid, "-tcl", null),
#else
                new Option(null, OptionFlags.MustHaveValue |
                    OptionFlags.Unsupported, Index.Invalid,
                    Index.Invalid, "-tcl", null),
#endif
                new Option(null, OptionFlags.None, Index.Invalid,
                    Index.Invalid, "-noforcedelete", null),
                new Option(null, OptionFlags.None, Index.Invalid,
                    Index.Invalid, "-tostring", null),
                new Option(typeof(ObjectFlags), OptionFlags.MustHaveEnumValue,
                    Index.Invalid, Index.Invalid, "-objectflags",
                    new Variant(DefaultObjectFlags)),
                Option.CreateEndOfOptions()
            });
        }
#endif

        ///////////////////////////////////////////////////////////////////////

#if XML && SERIALIZATION
        //
        // NOTE: This is for the [xml deserialize] sub-command.
        //
        /// <summary>
        /// This method builds the option dictionary used by the
        /// [xml deserialize] sub-command.
        /// </summary>
        /// <returns>
        /// The newly created option dictionary.
        /// </returns>
        private static OptionDictionary GetDeserializeOptions()
        {
            return new OptionDictionary(
                new IOption[] {
                new Option(null, OptionFlags.MustHaveValue, Index.Invalid,
                    Index.Invalid, "-objectname", null),
                new Option(null, OptionFlags.MustHaveTypeValue, Index.Invalid,
                    Index.Invalid, "-type", null),
                new Option(null, NoCreateOptionFlags, Index.Invalid,
                    Index.Invalid, "-nocreate", null),
                new Option(null, NoDisposeOptionFlags, Index.Invalid,
                    Index.Invalid, "-nodispose", null),
                new Option(null, OptionFlags.None, Index.Invalid,
                    Index.Invalid, "-tostring", null),
                new Option(null, OptionFlags.None, Index.Invalid,
                    Index.Invalid, "-stricttype", null),
                new Option(null, VerboseOptionFlags, Index.Invalid,
                    Index.Invalid, "-verbose", null),
                new Option(null, OptionFlags.None, Index.Invalid,
                    Index.Invalid, "-nocase", null),
                new Option(null, AliasOptionFlags, Index.Invalid,
                    Index.Invalid, "-alias", null),
                new Option(null, OptionFlags.None, Index.Invalid,
                    Index.Invalid, "-aliasraw", null),
                new Option(null, OptionFlags.None, Index.Invalid,
                    Index.Invalid, "-aliasall", null),
                new Option(null, OptionFlags.None, Index.Invalid,
                    Index.Invalid, "-aliasreference", null),
#if NATIVE && TCL
                new Option(null, OptionFlags.MustHaveTclInterpreterValue,
                    Index.Invalid, Index.Invalid, "-tcl", null),
#else
                new Option(null, OptionFlags.MustHaveValue |
                    OptionFlags.Unsupported, Index.Invalid,
                    Index.Invalid, "-tcl", null),
#endif
                new Option(null, OptionFlags.None, Index.Invalid,
                    Index.Invalid, "-noforcedelete", null),
                new Option(null, OptionFlags.MustHaveEncodingValue,
                    Index.Invalid, Index.Invalid, "-encoding", null),
                new Option(typeof(ObjectFlags), OptionFlags.MustHaveEnumValue,
                    Index.Invalid, Index.Invalid, "-objectflags",
                    new Variant(DefaultObjectFlags)),
                Option.CreateEndOfOptions()
            });
        }
#endif

        ///////////////////////////////////////////////////////////////////////

        //
        // NOTE: This is for the [object dispose] sub-command.
        //
        // NOTE: This method must use the "Unsafe" option flag to prevent a
        //       "safe" interpreter from potentially using an option.
        //
        /// <summary>
        /// This method builds the option dictionary used by the [object dispose]
        /// sub-command.  It uses the "Unsafe" option flag to prevent a "safe"
        /// interpreter from potentially using an option.
        /// </summary>
        /// <returns>
        /// The newly created option dictionary.
        /// </returns>
        private static OptionDictionary GetDisposeOptions()
        {
            return new OptionDictionary(
                new IOption[] {
                new Option(null,
                    SynchronousOptionFlags | OptionFlags.Unsafe,
                    Index.Invalid, Index.Invalid, "-synchronous", null),
                new Option(null,
                    NoDisposeOptionFlags | OptionFlags.Unsafe,
                    Index.Invalid, Index.Invalid, "-nodispose", null),
                new Option(null, OptionFlags.Unsafe, Index.Invalid,
                    Index.Invalid, "-nocomplain", null),
                Option.CreateEndOfOptions()
            });
        }

        ///////////////////////////////////////////////////////////////////////

#if NATIVE && TCL
        //
        // NOTE: This is for the [tcl eval] sub-command.
        //
        /// <summary>
        /// This method builds the option dictionary used by the [tcl eval]
        /// sub-command.
        /// </summary>
        /// <returns>
        /// The newly created option dictionary.
        /// </returns>
        private static OptionDictionary GetEvaluateOptions()
        {
            return new OptionDictionary(
                new IOption[] {
                new Option(null, OptionFlags.None, Index.Invalid,
                    Index.Invalid, "-time", null),
                new Option(null, OptionFlags.MustHaveBooleanValue,
                    Index.Invalid, Index.Invalid, "-exceptions", null),
                Option.CreateEndOfOptions()
            });
        }
#endif

        ///////////////////////////////////////////////////////////////////////

#if PREVIOUS_RESULT
        //
        // NOTE: This is for the [debug exception] sub-command.
        //
        /// <summary>
        /// This method builds the option dictionary used by the
        /// [debug exception] sub-command.
        /// </summary>
        /// <returns>
        /// The newly created option dictionary.
        /// </returns>
        private static OptionDictionary GetExceptionOptions()
        {
            return new OptionDictionary(
                new IOption[] {
                new Option(null, OptionFlags.MustHaveValue, Index.Invalid,
                    Index.Invalid, "-objectname", null),
                new Option(null, OptionFlags.MustHaveTypeValue, Index.Invalid,
                    Index.Invalid, "-type", null),
                new Option(null, NoCreateOptionFlags, Index.Invalid,
                    Index.Invalid, "-nocreate", null),
                new Option(null, NoDisposeOptionFlags, Index.Invalid,
                    Index.Invalid, "-nodispose", null),
                new Option(null, AliasOptionFlags, Index.Invalid,
                    Index.Invalid, "-alias", null),
                new Option(null, OptionFlags.None, Index.Invalid,
                    Index.Invalid, "-aliasraw", null),
                new Option(null, OptionFlags.None, Index.Invalid,
                    Index.Invalid, "-aliasall", null),
                new Option(null, OptionFlags.None, Index.Invalid,
                    Index.Invalid, "-aliasreference", null),
#if NATIVE && TCL
                new Option(null, OptionFlags.MustHaveTclInterpreterValue,
                    Index.Invalid, Index.Invalid, "-tcl", null),
#else
                new Option(null, OptionFlags.MustHaveValue |
                    OptionFlags.Unsupported, Index.Invalid,
                    Index.Invalid, "-tcl", null),
#endif
                new Option(null, OptionFlags.None, Index.Invalid,
                    Index.Invalid, "-noforcedelete", null),
                new Option(null, OptionFlags.None, Index.Invalid,
                    Index.Invalid, "-tostring", null),
                new Option(typeof(ObjectFlags), OptionFlags.MustHaveEnumValue,
                    Index.Invalid, Index.Invalid, "-objectflags",
                    new Variant(DefaultObjectFlags)),
                Option.CreateEndOfOptions()
            });
        }
#endif

        ///////////////////////////////////////////////////////////////////////

        //
        // NOTE: This is for the [exec] command using the currently active
        //       interpreter.
        //
        /// <summary>
        /// This method builds the option dictionary used by the [exec] command,
        /// using the currently active interpreter.
        /// </summary>
        /// <returns>
        /// The newly created option dictionary.
        /// </returns>
        private static OptionDictionary GetExecOptions()
        {
            return GetExecOptions(Interpreter.GetActive());
        }

        ///////////////////////////////////////////////////////////////////////

        //
        // NOTE: This is for the [exec] command.
        //
        /// <summary>
        /// This method builds the option dictionary used by the [exec] command.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter whose engine event flags are used as the default for
        /// the -eventflags option; this may be null.
        /// </param>
        /// <returns>
        /// The newly created option dictionary.
        /// </returns>
        private static OptionDictionary GetExecOptions(
            Interpreter interpreter
            )
        {
            EventFlags eventFlags = (interpreter != null) ?
                interpreter.EngineEventFlags : EventFlags.None;

            return new OptionDictionary(
                new IOption[] {
                new Option(null, OptionFlags.None, Index.Invalid,
                    Index.Invalid, "-nopreviousprocessid", null),
                new Option(null, OptionFlags.None, Index.Invalid,
                    Index.Invalid, "-trace", null),
                new Option(null, OptionFlags.None, Index.Invalid,
                    Index.Invalid, "-debug", null),
                new Option(null, OptionFlags.None, Index.Invalid,
                    Index.Invalid, "-nonormalize", null),
                new Option(null, OptionFlags.None, Index.Invalid,
                    Index.Invalid, "-noellipsis", null),
                new Option(null, OptionFlags.None, Index.Invalid,
                    Index.Invalid, "-commandline", null),
                new Option(null, OptionFlags.None, Index.Invalid,
                    Index.Invalid, "-forprocessor", null),
                new Option(null, OptionFlags.None, Index.Invalid,
                    Index.Invalid, "-dequote", null),
                new Option(null, OptionFlags.None, Index.Invalid,
                    Index.Invalid, "-quoteall", null),
                new Option(null, OptionFlags.None, Index.Invalid,
                    Index.Invalid, "-unicode", null),
                new Option(null, OptionFlags.None, Index.Invalid,
                    Index.Invalid, "-ignorestderr", null),
                new Option(null, OptionCategory.Category01,
                    OptionFlags.None, Index.Invalid,
                    Index.Invalid, "-overridecapture", null),
                new Option(null, OptionFlags.None, Index.Invalid,
                    Index.Invalid, "-killonerror", null),
                new Option(null, OptionFlags.None, Index.Invalid,
                    Index.Invalid, "-keepnewline", null),
                new Option(null, OptionFlags.None, Index.Invalid,
                    Index.Invalid, "-noexitcode", null),
                new Option(null, OptionCategory.Category01,
                    OptionFlags.None, Index.Invalid,
                    Index.Invalid, "-nocapture", null),
                new Option(null, OptionCategory.Category01,
                    OptionFlags.None, Index.Invalid,
                    Index.Invalid, "-nocaptureinput", null),
                new Option(null, OptionCategory.Category01,
                    OptionFlags.None, Index.Invalid,
                    Index.Invalid, "-nocaptureoutput", null),
                new Option(null, OptionCategory.Category01,
                    OptionFlags.None, Index.Invalid,
                    Index.Invalid, "-background", null),
                new Option(null, OptionCategory.Category01,
                    OptionFlags.None, Index.Invalid,
                    Index.Invalid, "-shell", null),
                new Option(null, OptionFlags.None, Index.Invalid,
                    Index.Invalid, "-nocarriagereturns", null),
                new Option(null, OptionFlags.None, Index.Invalid,
                    Index.Invalid, "-setall", null),
                new Option(null, OptionFlags.None, Index.Invalid,
                    Index.Invalid, "-trimall", null),
                new Option(null, OptionFlags.None, Index.Invalid,
                    Index.Invalid, "-noevents", null),
                new Option(null, OptionFlags.None, Index.Invalid,
                    Index.Invalid, "-nosleep", null),
                new Option(null, OptionFlags.None, Index.Invalid,
                    Index.Invalid, "-nointerpreter", null),
                new Option(null, OptionFlags.None, Index.Invalid,
                    Index.Invalid, "-userinterface", null),
                new Option(typeof(ExitCode),
                    OptionFlags.MustHaveEnumValue, Index.Invalid,
                    Index.Invalid, "-success", null),
                new Option(null, OptionFlags.MustHaveValue,
                    Index.Invalid, Index.Invalid, "-domainname", null),
                new Option(null, OptionFlags.MustHaveValue,
                    Index.Invalid, Index.Invalid, "-username", null),
                new Option(null, OptionFlags.MustHaveSecureStringValue,
                    Index.Invalid, Index.Invalid, "-password", null),
                new Option(null, OptionFlags.MustHaveValue,
                    Index.Invalid, Index.Invalid, "-escaperanges",
                    null), // index range list
                new Option(null, OptionFlags.MustHaveListValue,
                    Index.Invalid, Index.Invalid, "-escapesubstring",
                    null), // command
                new Option(null, OptionFlags.MustHaveListValue,
                    Index.Invalid, Index.Invalid, "-preprocessarguments",
                    null), // command
                new Option(null, OptionFlags.MustHaveValue,
                    Index.Invalid, Index.Invalid, "-directory",
                    null), // working directory
                new Option(null, OptionFlags.MustHaveValue,
                    Index.Invalid, Index.Invalid, "-processid",
                    null), // varName
                new Option(null, OptionFlags.MustHaveValue,
                    Index.Invalid, Index.Invalid, "-exitcode",
                    null), // varName
                new Option(null, OptionFlags.MustHaveValue,
                    Index.Invalid, Index.Invalid, "-stdin",
                    null), // varName
                new Option(null, OptionFlags.MustHaveValue,
                    Index.Invalid, Index.Invalid, "-stdout",
                    null),// varName
                new Option(null, OptionFlags.MustHaveValue,
                    Index.Invalid, Index.Invalid, "-stderr",
                    null), // varName
                new Option(null, OptionFlags.MustHaveValue,
                    Index.Invalid, Index.Invalid, "-stdinobject",
                    null), // varName
                new Option(null, OptionFlags.MustHaveCallbackValue,
                    Index.Invalid, Index.Invalid, "-startcallback", null),
                new Option(null, OptionFlags.MustHaveValue,
                    Index.Invalid, Index.Invalid, "-stdoutlogpath", null),
                new Option(null, OptionFlags.MustHaveValue,
                    Index.Invalid, Index.Invalid, "-stderrlogpath", null),
                new Option(null, OptionFlags.MustHaveCallbackValue,
                    Index.Invalid, Index.Invalid, "-stdoutcallback", null),
                new Option(null, OptionFlags.MustHaveCallbackValue,
                    Index.Invalid, Index.Invalid, "-stderrcallback", null),
                new Option(null, OptionFlags.MustHaveValue,
                    Index.Invalid, Index.Invalid, "-logtag", null),
                new Option(null, OptionFlags.MustHaveIntegerValue,
                    Index.Invalid, Index.Invalid, "-timeout", null),
                new Option(typeof(ObjectFlags),
                    OptionFlags.MustHaveEnumValue, Index.Invalid,
                    Index.Invalid, "-objectflags",
                    new Variant(DefaultObjectFlags |
                        ObjectFlags.NoDispose)),
                new Option(typeof(EventFlags),
                    OptionFlags.MustHaveEnumValue, Index.Invalid,
                    Index.Invalid, "-eventflags",
                    new Variant(eventFlags)),
                new Option(typeof(ProcessWindowStyle),
                    OptionFlags.MustHaveEnumValue, Index.Invalid,
                    Index.Invalid, "-windowstyle",
                    new Variant(ProcessWindowStyle.Normal)),
                new Option(null, OptionFlags.MustHaveListValue,
                    Index.Invalid, Index.Invalid, "-tags", null),
                Option.CreateEndOfOptions()
            });
        }

        ///////////////////////////////////////////////////////////////////////

#if DATA
        //
        // NOTE: This is for the [sql execute] sub-command.
        //
        /// <summary>
        /// This method builds the option dictionary containing only the options
        /// specific to the [sql execute] sub-command.
        /// </summary>
        /// <returns>
        /// The newly created option dictionary.
        /// </returns>
        private static OptionDictionary GetSqlExecuteOnlyOptions()
        {
            return new OptionDictionary(
                new IOption[] {
                new Option(null, OptionFlags.MustHaveCallbackValue,
                    Index.Invalid, Index.Invalid, "-changed", null),
                new Option(null, OptionFlags.MustHaveCultureInfoValue,
                    Index.Invalid, Index.Invalid, "-culture", null),
                new Option(null, OptionFlags.None, Index.Invalid,
                    Index.Invalid, "-verbatim", null),
                new Option(typeof(DateTimeBehavior),
                    OptionFlags.MustHaveEnumValue, Index.Invalid,
                    Index.Invalid, "-datetimebehavior",
                    new Variant(DefaultDateTimeBehavior)),
                new Option(typeof(BlobBehavior),
                    OptionFlags.MustHaveEnumValue, Index.Invalid,
                    Index.Invalid, "-blobbehavior",
                    new Variant(DefaultBlobBehavior)),
                new Option(typeof(DateTimeKind),
                    OptionFlags.MustHaveEnumValue, Index.Invalid,
                    Index.Invalid, "-datetimekind",
                    new Variant(DefaultDateTimeKind)),
                new Option(typeof(DateTimeStyles),
                    OptionFlags.MustHaveEnumValue, Index.Invalid,
                    Index.Invalid, "-datetimestyles",
                    new Variant(DefaultDateTimeStyles)),
                new Option(typeof(ValueFlags), OptionFlags.MustHaveEnumValue,
                    Index.Invalid, Index.Invalid, "-valueflags",
                    new Variant(DefaultValueFlags)),
                new Option(null, OptionFlags.MustHaveValue, Index.Invalid,
                    Index.Invalid, "-valueformat", null),
                new Option(null, OptionFlags.MustHaveValue, Index.Invalid,
                    Index.Invalid, "-datetimeformat",
                    new Variant(DefaultDateTimeFormat)),
                new Option(null, OptionFlags.MustHaveValue, Index.Invalid,
                    Index.Invalid, "-numberformat", null),
                new Option(null, OptionFlags.MustHaveValue, Index.Invalid,
                    Index.Invalid, "-transaction", null),
                new Option(null, OptionFlags.MustHaveValue, Index.Invalid,
                    Index.Invalid, "-rowsvar", null),
                new Option(null, OptionFlags.MustHaveValue, Index.Invalid,
                    Index.Invalid, "-rowvar", null),
                new Option(null, OptionFlags.None, Index.Invalid,
                    Index.Invalid, "-time", null),
                new Option(null, OptionFlags.MustHaveBooleanValue,
                    Index.Invalid, Index.Invalid, "-nested", null),
                new Option(null, OptionFlags.MustHaveBooleanValue,
                    Index.Invalid, Index.Invalid, "-allownull", null),
                new Option(null, OptionFlags.MustHaveValue,
                    Index.Invalid, Index.Invalid, "-nullvalue", null),
                new Option(null, OptionFlags.MustHaveValue,
                    Index.Invalid, Index.Invalid, "-dbnullvalue", null),
                new Option(null, OptionFlags.MustHaveValue,
                    Index.Invalid, Index.Invalid, "-errorvalue", null),
                new Option(null, OptionFlags.MustHaveBooleanValue,
                    Index.Invalid, Index.Invalid, "-pairs", null),
                new Option(null, OptionFlags.MustHaveBooleanValue,
                    Index.Invalid, Index.Invalid, "-names", null),
                new Option(null, OptionFlags.MustHaveBooleanValue,
                    Index.Invalid, Index.Invalid, "-nofixup", null),
                new Option(null, OptionFlags.MustHaveValue, Index.Invalid,
                    Index.Invalid, "-timevar", null),
                new Option(null, OptionFlags.MustHaveIntegerValue,
                    Index.Invalid, Index.Invalid, "-timeout", null),
                new Option(null, OptionFlags.MustHaveIntegerValue,
                    Index.Invalid, Index.Invalid, "-limit", null),
                new Option(typeof(CommandType), OptionFlags.MustHaveEnumValue,
                    Index.Invalid, Index.Invalid, "-commandtype",
                    new Variant(DefaultCommandType)),
                new Option(typeof(DbResultFormat),
                    OptionFlags.MustHaveEnumValue, Index.Invalid,
                    Index.Invalid, "-format",
                    new Variant(DefaultResultFormat)),
                new Option(typeof(DbExecuteType),
                    OptionFlags.MustHaveEnumValue, Index.Invalid,
                    Index.Invalid, "-execute",
                    new Variant(DefaultExecuteType)),
                new Option(typeof(CommandBehavior),
                    OptionFlags.MustHaveEnumValue, Index.Invalid,
                    Index.Invalid, "-behavior",
                    new Variant(DefaultCommandBehavior)),
                new Option(null, NoCreateOptionFlags, Index.Invalid,
                    Index.Invalid, "-nocreate", null)
            });
        }

        ///////////////////////////////////////////////////////////////////////

        //
        // NOTE: This is for the [sql execute] sub-command.
        //
        /// <summary>
        /// This method builds the complete option dictionary used by the
        /// [sql execute] sub-command, combining the sub-command-specific options
        /// with the shared fixup-return-value options.
        /// </summary>
        /// <returns>
        /// The newly created option dictionary.
        /// </returns>
        private static OptionDictionary GetSqlExecuteOptions()
        {
            return new OptionDictionary(
                GetSqlExecuteOnlyOptions(), GetFixupReturnValueOptions());
        }
#endif

        ///////////////////////////////////////////////////////////////////////

        //
        // NOTE: This is for use with the MarshalOps.FixupReturnValue and/or
        //       Utility.FixupReturnValue methods.
        //
        // NOTE: This method must use the "Unsafe" option flag to prevent a
        //       "safe" interpreter from potentially using an option.
        //
        /// <summary>
        /// This method builds the option dictionary shared by all code paths
        /// that create opaque object handles via the MarshalOps.FixupReturnValue
        /// and/or Utility.FixupReturnValue methods.  It uses the "Unsafe" option
        /// flag to prevent a "safe" interpreter from potentially using an
        /// option.
        /// </summary>
        /// <returns>
        /// The newly created option dictionary.
        /// </returns>
        private static OptionDictionary GetFixupReturnValueOptions()
        {
            //
            // NOTE: The reason these are defined here is because they must
            //       be used anywhere that creates opaque object handles via
            //       FixupReturnValue.
            //
            return new OptionDictionary(
                new IOption[] {
                new Option(null,
                    OptionFlags.MustHaveValue | OptionFlags.Unsafe,
                    Index.Invalid, Index.Invalid, "-objectname", null),
                new Option(null,
                    OptionFlags.MustHaveTypeValue | OptionFlags.Unsafe,
                    Index.Invalid, Index.Invalid, "-returntype", null),
                new Option(null,
                    OptionFlags.MustHaveTypeValue | OptionFlags.Unsafe,
                    Index.Invalid, Index.Invalid, "-objecttype", null),
                new Option(null, CreateOptionFlags, Index.Invalid,
                    Index.Invalid, "-create", null),
                new Option(null,
                    NoDisposeOptionFlags | OptionFlags.Unsafe,
                    Index.Invalid, Index.Invalid, "-nodispose", null),
                new Option(null, AliasOptionFlags, Index.Invalid,
                    Index.Invalid, "-alias", null),
                new Option(null, OptionFlags.None, Index.Invalid,
                    Index.Invalid, "-aliasraw", null),
                new Option(null, OptionFlags.None, Index.Invalid,
                    Index.Invalid, "-aliasall", null),
                new Option(null, OptionFlags.Unsafe, Index.Invalid,
                    Index.Invalid, "-aliasreference", null),
#if NATIVE && TCL
                new Option(null,
                    OptionFlags.MustHaveTclInterpreterValue |
                    OptionFlags.Unsafe, Index.Invalid, Index.Invalid,
                    "-tcl", null),
#else
                new Option(null,
                    OptionFlags.MustHaveValue | OptionFlags.Unsafe |
                    OptionFlags.Unsupported, Index.Invalid,
                    Index.Invalid, "-tcl", null),
#endif
                new Option(null, OptionFlags.Unsafe, Index.Invalid,
                    Index.Invalid, "-noforcedelete", null),
                new Option(null, OptionFlags.None, Index.Invalid,
                    Index.Invalid, "-tostring", null),
                new Option(typeof(ObjectFlags),
                    OptionFlags.MustHaveEnumValue | OptionFlags.Unsafe,
                    Index.Invalid, Index.Invalid, "-objectflags",
                    new Variant(DefaultObjectFlags)),
                Option.CreateEndOfOptions()
            });
        }

        ///////////////////////////////////////////////////////////////////////

        //
        // NOTE: This is for the [object foreach] sub-command.
        //
        /// <summary>
        /// This method builds the option dictionary used by the [object foreach]
        /// sub-command.
        /// </summary>
        /// <returns>
        /// The newly created option dictionary.
        /// </returns>
        private static OptionDictionary GetForEachOptions()
        {
            return new OptionDictionary(
                new IOption[] {
                new Option(null, SynchronousOptionFlags, Index.Invalid,
                    Index.Invalid, "-synchronous", null),
                new Option(null, OptionFlags.MustHaveValue, Index.Invalid,
                    Index.Invalid, "-objectname", null),
                new Option(null, OptionFlags.MustHaveTypeValue, Index.Invalid,
                    Index.Invalid, "-type", null),
                new Option(null, OptionFlags.MustHaveBooleanValue,
                    Index.Invalid, Index.Invalid, "-collect", null),
                new Option(null, NoCreateOptionFlags, Index.Invalid,
                    Index.Invalid, "-nocreate", null),
                new Option(null, NoDisposeOptionFlags, Index.Invalid,
                    Index.Invalid, "-nodispose", null),
                new Option(null, AliasOptionFlags, Index.Invalid,
                    Index.Invalid, "-alias", null),
                new Option(null, OptionFlags.None, Index.Invalid,
                    Index.Invalid, "-aliasraw", null),
                new Option(null, OptionFlags.None, Index.Invalid,
                    Index.Invalid, "-aliasall", null),
                new Option(null, OptionFlags.None, Index.Invalid,
                    Index.Invalid, "-aliasreference", null),
#if NATIVE && TCL
                new Option(null, OptionFlags.MustHaveTclInterpreterValue,
                    Index.Invalid, Index.Invalid, "-tcl", null),
#else
                new Option(null, OptionFlags.MustHaveValue |
                    OptionFlags.Unsupported, Index.Invalid,
                    Index.Invalid, "-tcl", null),
#endif
                new Option(null, OptionFlags.None, Index.Invalid,
                    Index.Invalid, "-noforcedelete", null),
                new Option(null, OptionFlags.None, Index.Invalid,
                    Index.Invalid, "-tostring", null),
                new Option(null, OptionFlags.None, Index.Invalid,
                    Index.Invalid, "-nocase", null),
                new Option(typeof(ObjectFlags), OptionFlags.MustHaveEnumValue,
                    Index.Invalid, Index.Invalid, "-objectflags",
                    new Variant(DefaultObjectFlags)),
                Option.CreateEndOfOptions()
            });
        }

        ///////////////////////////////////////////////////////////////////////

        //
        // NOTE: This is for the [object get] sub-command.
        //
        /// <summary>
        /// This method builds the option dictionary used by the [object get]
        /// sub-command.
        /// </summary>
        /// <returns>
        /// The newly created option dictionary.
        /// </returns>
        private static OptionDictionary GetGetOptions()
        {
            return new OptionDictionary(
                new IOption[] {
                new Option(null, OptionFlags.MustHaveValue, Index.Invalid,
                    Index.Invalid, "-objectname", null),
                new Option(null, OptionFlags.MustHaveTypeValue, Index.Invalid,
                    Index.Invalid, "-type", null),
                new Option(null, OptionFlags.MustHaveTypeListValue,
                    Index.Invalid, Index.Invalid, "-objecttypes", null),
                new Option(null, OptionFlags.None, Index.Invalid,
                    Index.Invalid, "-nocase", null),
                new Option(null, OptionFlags.None, Index.Invalid,
                    Index.Invalid, "-stricttype", null),
                new Option(null, VerboseOptionFlags, Index.Invalid,
                    Index.Invalid, "-verbose", null),
                new Option(null, NoCreateOptionFlags, Index.Invalid,
                    Index.Invalid, "-nocreate", null),
                new Option(null, NoDisposeOptionFlags, Index.Invalid,
                    Index.Invalid, "-nodispose", null),
                new Option(null, AliasOptionFlags, Index.Invalid,
                    Index.Invalid, "-alias", null),
                new Option(null, OptionFlags.None, Index.Invalid,
                    Index.Invalid, "-aliasraw", null),
                new Option(null, OptionFlags.None, Index.Invalid,
                    Index.Invalid, "-aliasall", null),
                new Option(null, OptionFlags.None, Index.Invalid,
                    Index.Invalid, "-aliasreference", null),
#if NATIVE && TCL
                new Option(null, OptionFlags.MustHaveTclInterpreterValue,
                    Index.Invalid, Index.Invalid, "-tcl", null),
#else
                new Option(null, OptionFlags.MustHaveValue |
                    OptionFlags.Unsupported, Index.Invalid,
                    Index.Invalid, "-tcl", null),
#endif
                new Option(null, OptionFlags.None, Index.Invalid,
                    Index.Invalid, "-noforcedelete", null),
                new Option(null, OptionFlags.None, Index.Invalid,
                    Index.Invalid, "-tostring", null),
                new Option(typeof(ObjectFlags), OptionFlags.MustHaveEnumValue,
                    Index.Invalid, Index.Invalid, "-objectflags",
                    new Variant(DefaultObjectFlags)),
                Option.CreateEndOfOptions()
            });
        }

        ///////////////////////////////////////////////////////////////////////

        //
        // NOTE: This is for the [object import] sub-command.
        //
        /// <summary>
        /// This method builds the option dictionary used by the [object import]
        /// sub-command.
        /// </summary>
        /// <returns>
        /// The newly created option dictionary.
        /// </returns>
        private static OptionDictionary GetImportOptions()
        {
            return new OptionDictionary(
                new IOption[] {
                new Option(null, OptionFlags.None, Index.Invalid,
                    Index.Invalid, "-clr", null),
                new Option(null, OptionFlags.None, Index.Invalid,
                    Index.Invalid, "-eagle", null),
                new Option(null, OptionFlags.MustHaveValue, Index.Invalid,
                    Index.Invalid, "-container", null),
                new Option(null, OptionFlags.MustHaveMatchModeValue,
                    Index.Invalid, Index.Invalid, "-importmode",
                    new Variant(DefaultMatchMode)),
                new Option(null, OptionFlags.MustHaveValue, Index.Invalid,
                    Index.Invalid, "-importpattern", null),
                new Option(null, OptionFlags.None, Index.Invalid,
                    Index.Invalid, "-importnocase", null),
                Option.CreateEndOfOptions()
            });
        }

        ///////////////////////////////////////////////////////////////////////

        //
        // NOTE: These options are shared between the [object invoke] and
        //       [object invokeraw] sub-commands.
        //
        // NOTE: This method must use the "Unsafe" option flag to prevent a
        //       "safe" interpreter from potentially using an option.
        //
        /// <summary>
        /// This method builds the option dictionary containing only the options
        /// shared between the [object invoke] and [object invokeraw]
        /// sub-commands.  It uses the "Unsafe" option flag to prevent a "safe"
        /// interpreter from potentially using an option.
        /// </summary>
        /// <returns>
        /// The newly created option dictionary.
        /// </returns>
        private static OptionDictionary GetInvokeSharedOnlyOptions()
        {
            return new OptionDictionary(
                new IOption[] {
                new Option(typeof(DateTimeKind),
                    OptionFlags.MustHaveEnumValue,
                    Index.Invalid, Index.Invalid, "-datetimekind",
                    new Variant(DefaultDateTimeKind)),
                new Option(typeof(DateTimeStyles),
                    OptionFlags.MustHaveEnumValue,
                    Index.Invalid, Index.Invalid, "-datetimestyles",
                    new Variant(DefaultDateTimeStyles)),
                new Option(null,
                    OptionFlags.MustHaveValue,
                    Index.Invalid, Index.Invalid, "-datetimeformat",
                    new Variant(DefaultDateTimeFormat)),
                new Option(null,
                    OptionFlags.MustHaveTypeValue | OptionFlags.Unsafe,
                    Index.Invalid, Index.Invalid, "-type", null),
                new Option(null,
                    OptionFlags.MustHaveTypeValue | OptionFlags.Unsafe,
                    Index.Invalid, Index.Invalid, "-objecttype", null),
                new Option(null,
                    OptionFlags.MustHaveTypeValue | OptionFlags.Unsafe,
                    Index.Invalid, Index.Invalid, "-proxytype", null),
                new Option(null, OptionFlags.MustHaveTypeListValue,
                    Index.Invalid, Index.Invalid, "-objecttypes", null),
                new Option(null, OptionFlags.MustHaveTypeListValue,
                    Index.Invalid, Index.Invalid, "-methodtypes", null),
                new Option(null, OptionFlags.MustHaveTypeListValue,
                    Index.Invalid, Index.Invalid, "-parametertypes", null),
                new Option(typeof(MarshalFlags),
                    OptionFlags.MustHaveEnumListValue | OptionFlags.Unsafe,
                    Index.Invalid, Index.Invalid, "-parametermarshalflags",
                    new Variant(DefaultParameterMarshalFlags)),
                new Option(null, DebugOptionFlags | OptionFlags.Unsafe,
                    Index.Invalid, Index.Invalid, "-debug", null),
                new Option(null, TraceOptionFlags | OptionFlags.Unsafe,
                    Index.Invalid, Index.Invalid, "-trace", null),
                new Option(typeof(ByRefArgumentFlags),
                    OptionFlags.MustHaveEnumValue | OptionFlags.Unsafe,
                    Index.Invalid, Index.Invalid, "-argumentflags",
                    new Variant(DefaultByRefArgumentFlags)),
                new Option(typeof(MarshalFlags),
                    OptionFlags.MustHaveEnumValue | OptionFlags.Unsafe,
                    Index.Invalid, Index.Invalid, "-marshalflags",
                    new Variant(DefaultMarshalFlags)),
                new Option(null, OptionFlags.None, Index.Invalid,
                    Index.Invalid, "-noinvoke", null),
                new Option(null, OptionFlags.None, Index.Invalid,
                    Index.Invalid, "-help", null),
                new Option(null, OptionFlags.None, Index.Invalid,
                    Index.Invalid, "-noargs", null),
                new Option(null, OptionFlags.Ignored, Index.Invalid,
                    Index.Invalid, "-chained", null),
                new Option(null, OptionFlags.Ignored, Index.Invalid,
                    Index.Invalid, "-lastresult", null),
                new Option(null, OptionFlags.Ignored, Index.Invalid,
                    Index.Invalid, "-keepresults", null),
                new Option(null, OptionFlags.Ignored, Index.Invalid,
                    Index.Invalid, "-nocomplain", null),
                new Option(null, OptionFlags.None, Index.Invalid,
                    Index.Invalid, "-invokeall", null),
                new Option(null, ArrayAsValueOptionFlags, Index.Invalid,
                    Index.Invalid, "-arrayasvalue", null),
                new Option(null, ArrayAsLinkOptionFlags, Index.Invalid,
                    Index.Invalid, "-arrayaslink", null),
                /* FIXME: Unsafe? */
                new Option(null, VerboseOptionFlags | OptionFlags.Unsafe,
                    Index.Invalid,Index.Invalid, "-verbose", null),
                new Option(null, OptionFlags.None, Index.Invalid,
                    Index.Invalid, "-nocase", null),
                new Option(null, OptionFlags.None, Index.Invalid,
                    Index.Invalid, "-default", null),
                new Option(typeof(ValueFlags),
                    OptionFlags.MustHaveEnumValue | OptionFlags.Unsafe,
                    Index.Invalid, Index.Invalid, "-objectvalueflags",
                    new Variant(DefaultObjectValueFlags)),
                new Option(null, OptionFlags.None, Index.Invalid,
                    Index.Invalid, "-nonestedobject", null),
                new Option(null, OptionFlags.None, Index.Invalid,
                    Index.Invalid, "-stricttype", null),
                new Option(null, OptionFlags.None, Index.Invalid,
                    Index.Invalid, "-nobyref", null),
                new Option(typeof(BindingFlags),
                    OptionFlags.MustHaveEnumValue | OptionFlags.Unsafe,
                    Index.Invalid, Index.Invalid, "-flags",
                    new Variant(DefaultBindingFlags)),
                new Option(typeof(BindingFlags),
                    OptionFlags.MustHaveEnumValue | OptionFlags.Unsafe,
                    Index.Invalid, Index.Invalid, "-bindingflags",
                    new Variant(DefaultBindingFlags)),
                new Option(typeof(ObjectFlags),
                    OptionFlags.MustHaveEnumValue | OptionFlags.Unsafe,
                    Index.Invalid, Index.Invalid, "-byrefobjectflags",
                    new Variant(DefaultByRefObjectFlags))
            });
        }

        ///////////////////////////////////////////////////////////////////////

        //
        // NOTE: These options are shared between the [object invoke] and
        //       [object invokeraw] sub-commands.
        //
        // NOTE: This method must use the "Unsafe" option flag to prevent a
        //       "safe" interpreter from potentially using an option.
        //
        /// <summary>
        /// This method builds the option dictionary shared between the
        /// [object invoke] and [object invokeraw] sub-commands, combining the
        /// shared-only options with the fixup-return-value options.  It uses the
        /// "Unsafe" option flag to prevent a "safe" interpreter from potentially
        /// using an option.
        /// </summary>
        /// <returns>
        /// The newly created option dictionary.
        /// </returns>
        private static OptionDictionary GetInvokeSharedOptions()
        {
            return new OptionDictionary(
                GetInvokeSharedOnlyOptions(), GetFixupReturnValueOptions());
        }

        ///////////////////////////////////////////////////////////////////////

        //
        // NOTE: This is for the [object invoke] sub-command.
        //
        // NOTE: This method must use the "Unsafe" option flag to prevent
        //       a "safe" interpreter from potentially using an option.
        //
        /// <summary>
        /// This method builds the option dictionary containing only the options
        /// specific to the [object invoke] sub-command.  It uses the "Unsafe"
        /// option flag to prevent a "safe" interpreter from potentially using an
        /// option.
        /// </summary>
        /// <returns>
        /// The newly created option dictionary.
        /// </returns>
        private static OptionDictionary GetInvokeOnlyOptions()
        {
            return new OptionDictionary(
                new IOption[] {
                new Option(typeof(ReorderFlags),
                    OptionFlags.MustHaveEnumValue | OptionFlags.Unsafe,
                    Index.Invalid, Index.Invalid, "-reorderflags",
                    new Variant(DefaultReorderFlags)),
                new Option(null, OptionFlags.MustHaveIntegerValue,
                    Index.Invalid, Index.Invalid, "-limit", null),
                new Option(null, OptionFlags.MustHaveIntegerValue,
                    Index.Invalid, Index.Invalid, "-index", null),
                new Option(null, OptionFlags.Ignored, Index.Invalid,
                    Index.Invalid, "-invoke", null),
                new Option(null, OptionFlags.None, Index.Invalid,
                    Index.Invalid, "-invokeraw", null),
                new Option(typeof(ValueFlags),
                    OptionFlags.MustHaveEnumValue | OptionFlags.Unsafe,
                    Index.Invalid, Index.Invalid, "-membervalueflags",
                    new Variant(DefaultMemberValueFlags)),
                new Option(null, OptionFlags.None, Index.Invalid,
                    Index.Invalid, "-nonestedmember", null),
                new Option(null, OptionFlags.None, Index.Invalid,
                    Index.Invalid, "-strictmember", null),
                new Option(null, OptionFlags.None, Index.Invalid,
                    Index.Invalid, "-strictargs", null),
                new Option(typeof(MemberTypes),
                    OptionFlags.MustHaveEnumValue | OptionFlags.Unsafe,
                    Index.Invalid, Index.Invalid, "-membertypes",
                    new Variant(DefaultMemberTypes)),
                new Option(null, OptionFlags.Unsafe, Index.Invalid,
                    Index.Invalid, "-identity", null),
                new Option(null, OptionFlags.Unsafe, Index.Invalid,
                    Index.Invalid, "-typeidentity", null)
            });
        }

        ///////////////////////////////////////////////////////////////////////

        //
        // NOTE: This is for the [object invoke] sub-command.
        //
        // NOTE: This method must use the "Unsafe" option flag to prevent
        //       a "safe" interpreter from potentially using an option.
        //
        /// <summary>
        /// This method builds the complete option dictionary used by the
        /// [object invoke] sub-command, combining the invoke-only options with
        /// the shared invoke options.  It uses the "Unsafe" option flag to
        /// prevent a "safe" interpreter from potentially using an option.
        /// </summary>
        /// <returns>
        /// The newly created option dictionary.
        /// </returns>
        private static OptionDictionary GetInvokeOptions()
        {
            return new OptionDictionary(
                GetInvokeOnlyOptions(), GetInvokeSharedOptions());
        }

        ///////////////////////////////////////////////////////////////////////

        //
        // NOTE: This is for the [object invokeall] sub-command.
        //
        /// <summary>
        /// This method builds the option dictionary used by the [object invokeall]
        /// sub-command, by starting from the [object invoke] options and
        /// adjusting which options are ignored.
        /// </summary>
        /// <returns>
        /// The newly created option dictionary.
        /// </returns>
        private static OptionDictionary GetInvokeAllOptions()
        {
            OptionDictionary options = GetInvokeOptions();

            options["-invoke"].Flags &= ~OptionFlags.Ignored;
            options["-invokeraw"].Flags &= ~OptionFlags.Ignored;
            options["-invokeall"].Flags |= OptionFlags.Ignored;

            options["-chained"].Flags &= ~OptionFlags.Ignored;
            options["-lastresult"].Flags &= ~OptionFlags.Ignored;
            options["-keepresults"].Flags &= ~OptionFlags.Ignored;
            options["-nocomplain"].Flags &= ~OptionFlags.Ignored;

            return options;
        }

        ///////////////////////////////////////////////////////////////////////

        //
        // NOTE: This is for the [object invokeraw] sub-command.
        //
        // NOTE: This method must use the "Unsafe" option flag to prevent a
        //       "safe" interpreter from potentially using an option.
        //
        /// <summary>
        /// This method builds the option dictionary containing only the options
        /// specific to the [object invokeraw] sub-command.  It uses the "Unsafe"
        /// option flag to prevent a "safe" interpreter from potentially using an
        /// option.
        /// </summary>
        /// <returns>
        /// The newly created option dictionary.
        /// </returns>
        private static OptionDictionary GetInvokeRawOnlyOptions()
        {
            return new OptionDictionary(
                new IOption[] {
                new Option(null, OptionFlags.None, Index.Invalid,
                    Index.Invalid, "-invoke", null),
                new Option(null, OptionFlags.Ignored, Index.Invalid,
                    Index.Invalid, "-invokeraw", null)
            });
        }

        ///////////////////////////////////////////////////////////////////////

        //
        // NOTE: This is for the [object invokeraw] sub-command.
        //
        // NOTE: This method must use the "Unsafe" option flag to prevent a
        //       "safe" interpreter from potentially using an option.
        //
        /// <summary>
        /// This method builds the complete option dictionary used by the
        /// [object invokeraw] sub-command, combining the invokeraw-only options
        /// with the shared invoke options.  It uses the "Unsafe" option flag to
        /// prevent a "safe" interpreter from potentially using an option.
        /// </summary>
        /// <returns>
        /// The newly created option dictionary.
        /// </returns>
        private static OptionDictionary GetInvokeRawOptions()
        {
            return new OptionDictionary(
                GetInvokeRawOnlyOptions(), GetInvokeSharedOptions());
        }

        ///////////////////////////////////////////////////////////////////////

        //
        // NOTE: This is for the [object isdisposed] sub-command.
        //
        /// <summary>
        /// This method builds the option dictionary used by the
        /// [object isdisposed] sub-command.
        /// </summary>
        /// <returns>
        /// The newly created option dictionary.
        /// </returns>
        private static OptionDictionary GetIsDisposedOptions()
        {
            return new OptionDictionary(
                new IOption[] {
                new Option(null, OptionFlags.None, Index.Invalid,
                    Index.Invalid, "-nocomplain", null),
                new Option(null, OptionFlags.MustHaveBooleanValue |
                    OptionFlags.Unsafe, Index.Invalid, Index.Invalid,
                    "-force", null),
                new Option(null, OptionFlags.MustHaveBooleanValue |
                    OptionFlags.Unsafe, Index.Invalid, Index.Invalid,
                    "-cannotcheck", null),
                new Option(null, OptionFlags.MustHaveBooleanValue |
                    OptionFlags.Unsafe, Index.Invalid, Index.Invalid,
                    "-caughtexception", null),
                Option.CreateEndOfOptions()
            });
        }

        ///////////////////////////////////////////////////////////////////////

        //
        // NOTE: This is for the [object isnull] sub-command.
        //
        /// <summary>
        /// This method builds the option dictionary used by the [object isnull]
        /// sub-command.
        /// </summary>
        /// <returns>
        /// The newly created option dictionary.
        /// </returns>
        private static OptionDictionary GetIsNullOptions()
        {
            return new OptionDictionary(
                new IOption[] {
                new Option(null, OptionFlags.None, Index.Invalid,
                    Index.Invalid, "-nocomplain", null),
                new Option(null, OptionFlags.MustHaveBooleanValue |
                    OptionFlags.Unsafe, Index.Invalid, Index.Invalid,
                    "-objectdisposed", null),
                new Option(null, OptionFlags.MustHaveBooleanValue |
                    OptionFlags.Unsafe, Index.Invalid, Index.Invalid,
                    "-valuedisposed", null),
                new Option(null, OptionFlags.MustHaveBooleanValue |
                    OptionFlags.Unsafe, Index.Invalid, Index.Invalid,
                    "-force", null)
            }, GetIsDisposedOptions());
        }

        ///////////////////////////////////////////////////////////////////////

        //
        // NOTE: This is for the [object isoftype] sub-command.
        //
        /// <summary>
        /// This method builds the option dictionary used by the [object isoftype]
        /// sub-command.
        /// </summary>
        /// <returns>
        /// The newly created option dictionary.
        /// </returns>
        private static OptionDictionary GetIsOfTypeOptions()
        {
            return new OptionDictionary(
                new IOption[] {
                new Option(null, OptionFlags.MustHaveTypeListValue,
                    Index.Invalid, Index.Invalid, "-objecttypes", null),
                new Option(typeof(ValueFlags),
                    OptionFlags.MustHaveEnumValue | OptionFlags.Unsafe,
                    Index.Invalid, Index.Invalid, "-objectvalueflags",
                    new Variant(DefaultObjectValueFlags)),
                new Option(typeof(MarshalFlags),
                    OptionFlags.MustHaveEnumValue | OptionFlags.Unsafe,
                    Index.Invalid, Index.Invalid, "-marshalflags",
                    new Variant(DefaultMarshalFlags)),
                new Option(null, OptionFlags.None, Index.Invalid,
                    Index.Invalid, "-nocase", null),
                new Option(null, OptionFlags.None, Index.Invalid,
                    Index.Invalid, "-stricttype", null),
                new Option(null,
                    VerboseOptionFlags | OptionFlags.Unsafe,
                    Index.Invalid, Index.Invalid, "-verbose", null),
                new Option(null, OptionFlags.Unsafe, Index.Invalid,
                    Index.Invalid, "-nocomplain", null),
                new Option(null, OptionFlags.None, Index.Invalid,
                    Index.Invalid, "-assignable", null),
                Option.CreateEndOfOptions()
            });
        }

        ///////////////////////////////////////////////////////////////////////

        //
        // NOTE: This is for the [object load] sub-command.
        //
        /// <summary>
        /// This method builds the option dictionary used by the [object load]
        /// sub-command.
        /// </summary>
        /// <returns>
        /// The newly created option dictionary.
        /// </returns>
        private static OptionDictionary GetLoadOptions()
        {
            return new OptionDictionary(
                new IOption[] {
                new Option(null, OptionFlags.MustHaveRelativeNamespaceValue,
                    Index.Invalid, Index.Invalid, "-namespace", null),
                new Option(null, OptionFlags.MustHaveValue, Index.Invalid,
                    Index.Invalid, "-objectname", null),
                new Option(null, OptionFlags.MustHaveTypeValue, Index.Invalid,
                    Index.Invalid, "-type", null),
                new Option(null, CreateOptionFlags, Index.Invalid,
                    Index.Invalid, "-create", null),
                new Option(null, NoDisposeOptionFlags, Index.Invalid,
                    Index.Invalid, "-nodispose", null),
                new Option(null, AliasOptionFlags, Index.Invalid,
                    Index.Invalid, "-alias", null),
                new Option(null, OptionFlags.None, Index.Invalid,
                    Index.Invalid, "-aliasraw", null),
                new Option(null, OptionFlags.None, Index.Invalid,
                    Index.Invalid, "-aliasall", null),
                new Option(null, OptionFlags.None, Index.Invalid,
                    Index.Invalid, "-aliasreference", null),
#if NATIVE && TCL
                new Option(null, OptionFlags.MustHaveTclInterpreterValue,
                    Index.Invalid, Index.Invalid, "-tcl", null),
#else
                new Option(null, OptionFlags.MustHaveValue |
                    OptionFlags.Unsupported, Index.Invalid,
                    Index.Invalid, "-tcl", null),
#endif
                new Option(null, OptionFlags.None, Index.Invalid,
                    Index.Invalid, "-reflectiononly", null),
                new Option(null, OptionFlags.None, Index.Invalid,
                    Index.Invalid, "-fromobject", null),
                new Option(null, OptionFlags.None, Index.Invalid,
                    Index.Invalid, "-noforcedelete", null),
                new Option(null, OptionFlags.None, Index.Invalid,
                    Index.Invalid, "-tostring", null),
                new Option(null, OptionFlags.None, Index.Invalid,
                    Index.Invalid, "-import", null),
                new Option(null, OptionFlags.None, Index.Invalid,
                    Index.Invalid, "-importnonpublic", null),
                new Option(null, OptionFlags.MustHaveMatchModeValue,
                    Index.Invalid, Index.Invalid, "-importmode",
                    new Variant(DefaultMatchMode)),
                new Option(null, OptionFlags.MustHaveValue, Index.Invalid,
                    Index.Invalid, "-importpattern", null),
                new Option(null, OptionFlags.None, Index.Invalid,
                    Index.Invalid, "-importnocase", null),
                new Option(null, OptionFlags.None, Index.Invalid,
                    Index.Invalid, "-declare", null),
                new Option(null, OptionFlags.None, Index.Invalid,
                    Index.Invalid, "-declarenonpublic", null),
                new Option(null, OptionFlags.MustHaveMatchModeValue,
                    Index.Invalid, Index.Invalid, "-declaremode",
                    new Variant(DefaultMatchMode)),
                new Option(null, OptionFlags.MustHaveValue, Index.Invalid,
                    Index.Invalid, "-declarepattern", null),
                new Option(null, OptionFlags.None, Index.Invalid,
                    Index.Invalid, "-declarenocase", null),
                new Option(typeof(LoadType), OptionFlags.MustHaveEnumValue,
                    Index.Invalid, Index.Invalid, "-loadtype",
                    new Variant(DefaultLoadType)),
                new Option(typeof(ObjectFlags), OptionFlags.MustHaveEnumValue,
                    Index.Invalid, Index.Invalid, "-objectflags",
                    new Variant(DefaultObjectFlags | ObjectFlags.Assembly)),
                new Option(null, OptionFlags.None, Index.Invalid,
                    Index.Invalid, "-trustedonly", null),
                new Option(null, OptionFlags.None, Index.Invalid,
                    Index.Invalid, "-maybetrustedonly", null),
                new Option(null, OptionFlags.None, Index.Invalid,
                    Index.Invalid, "-verifiedonly", null),
                new Option(null, OptionFlags.None, Index.Invalid,
                    Index.Invalid, "-maybeverifiedonly", null),
                Option.CreateEndOfOptions()
            });
        }

        ///////////////////////////////////////////////////////////////////////

        //
        // NOTE: This is for the [object members] sub-command.
        //
        /// <summary>
        /// This method builds the option dictionary used by the [object members]
        /// sub-command.
        /// </summary>
        /// <returns>
        /// The newly created option dictionary.
        /// </returns>
        private static OptionDictionary GetMembersOptions()
        {
            return new OptionDictionary(
                new IOption[] {
                new Option(null, OptionFlags.MustHaveMatchModeValue,
                    Index.Invalid, Index.Invalid, "-mode",
                    new Variant(DefaultMatchMode)),
                new Option(null, OptionFlags.MustHaveTypeValue, Index.Invalid,
                    Index.Invalid, "-type", null),
                new Option(null, OptionFlags.MustHaveTypeListValue,
                    Index.Invalid, Index.Invalid, "-objecttypes", null),
                new Option(null, OptionFlags.MustHaveValue, Index.Invalid,
                    Index.Invalid, "-pattern", null),
                new Option(null, OptionFlags.None, Index.Invalid,
                    Index.Invalid, "-attributes", null),
                new Option(null, OptionFlags.None, Index.Invalid,
                    Index.Invalid, "-nocase", null),
                new Option(null, OptionFlags.None, Index.Invalid,
                    Index.Invalid, "-stricttype", null),
                new Option(null, VerboseOptionFlags, Index.Invalid,
                    Index.Invalid, "-verbose", null),
                new Option(null, OptionFlags.None, Index.Invalid,
                    Index.Invalid, "-signatures", null),
                new Option(null, OptionFlags.None, Index.Invalid,
                    Index.Invalid, "-qualified", null),
                new Option(null, OptionFlags.None, Index.Invalid,
                    Index.Invalid, "-matchnameonly", null),
                new Option(null, OptionFlags.None, Index.Invalid,
                    Index.Invalid, "-nameonly", null),
                new Option(typeof(MemberTypes), OptionFlags.MustHaveEnumValue,
                    Index.Invalid, Index.Invalid, "-membertypes",
                    new Variant(DefaultMemberTypes)),
                new Option(typeof(BindingFlags), OptionFlags.MustHaveEnumValue,
                    Index.Invalid, Index.Invalid, "-flags",
                    new Variant(DefaultBindingFlags)),
                new Option(typeof(BindingFlags), OptionFlags.MustHaveEnumValue,
                    Index.Invalid, Index.Invalid, "-bindingflags",
                    new Variant(DefaultBindingFlags)),
                new Option(typeof(ValueFlags),
                    OptionFlags.MustHaveEnumValue, Index.Invalid,
                    Index.Invalid, "-objectvalueflags",
                    new Variant(DefaultObjectValueFlags)),
                new Option(typeof(MarshalFlags),
                    OptionFlags.MustHaveEnumValue, Index.Invalid,
                    Index.Invalid, "-marshalflags",
                    new Variant(DefaultMarshalFlags)),
                Option.CreateEndOfOptions()
            });
        }

        ///////////////////////////////////////////////////////////////////////

        //
        // NOTE: This is for the [object search] sub-command.
        //
        /// <summary>
        /// This method builds the option dictionary used by the [object search]
        /// sub-command.
        /// </summary>
        /// <returns>
        /// The newly created option dictionary.
        /// </returns>
        private static OptionDictionary GetSearchOptions()
        {
            return new OptionDictionary(
                new IOption[] {
                new Option(null, OptionFlags.MustHaveTypeListValue,
                    Index.Invalid, Index.Invalid, "-objecttypes", null),
                new Option(typeof(ValueFlags),
                    OptionFlags.MustHaveEnumValue, Index.Invalid,
                    Index.Invalid, "-objectvalueflags",
                    new Variant(DefaultObjectValueFlags)),
                new Option(typeof(MarshalFlags),
                    OptionFlags.MustHaveEnumValue, Index.Invalid,
                    Index.Invalid, "-marshalflags",
                    new Variant(DefaultMarshalFlags)),
                new Option(null, OptionFlags.None, Index.Invalid,
                    Index.Invalid, "-noshowname", null),
                new Option(null, OptionFlags.None, Index.Invalid,
                    Index.Invalid, "-nonamespace", null),
                new Option(null, OptionFlags.None, Index.Invalid,
                    Index.Invalid, "-noassembly", null),
                new Option(null, OptionFlags.None, Index.Invalid,
                    Index.Invalid, "-noexception", null),
                new Option(null, OptionFlags.None, Index.Invalid,
                    Index.Invalid, "-fullname", null),
                new Option(null, OptionFlags.None, Index.Invalid,
                    Index.Invalid, "-nocase", null),
                new Option(null, OptionFlags.None, Index.Invalid,
                    Index.Invalid, "-stricttype", null),
                new Option(null, VerboseOptionFlags, Index.Invalid,
                    Index.Invalid, "-verbose", null),
                Option.CreateEndOfOptions()
            });
        }

        ///////////////////////////////////////////////////////////////////////

#if XML && SERIALIZATION
        //
        // NOTE: This is for the [xml serialize] sub-command.
        //
        /// <summary>
        /// This method builds the option dictionary used by the [xml serialize]
        /// sub-command.
        /// </summary>
        /// <returns>
        /// The newly created option dictionary.
        /// </returns>
        private static OptionDictionary GetSerializeOptions()
        {
            return new OptionDictionary(
                new IOption[] {
                new Option(null, OptionFlags.None, Index.Invalid,
                    Index.Invalid, "-stricttype", null),
                new Option(null, VerboseOptionFlags, Index.Invalid,
                    Index.Invalid, "-verbose", null),
                new Option(null, OptionFlags.None, Index.Invalid,
                    Index.Invalid, "-nocase", null),
                new Option(null, OptionFlags.MustHaveEncodingValue,
                    Index.Invalid, Index.Invalid, "-encoding", null),
                Option.CreateEndOfOptions()
            });
        }
#endif

        ///////////////////////////////////////////////////////////////////////

        //
        // NOTE: This is for the [read] command.
        //
        /// <summary>
        /// This method builds the option dictionary containing only the options
        /// specific to the [read] command.
        /// </summary>
        /// <returns>
        /// The newly created option dictionary.
        /// </returns>
        private static OptionDictionary GetReadOnlyOptions()
        {
            return new OptionDictionary(
                new IOption[] {
                new Option(null, OptionFlags.MustHaveEncodingValue |
                    OptionFlags.Unsafe, Index.Invalid, Index.Invalid,
                    "-encoding", null),
                new Option(null, OptionFlags.Unsafe, Index.Invalid,
                    Index.Invalid, "-useobject", null),
                new Option(null, OptionFlags.None, Index.Invalid,
                    Index.Invalid, "-noblock", null),
                new Option(null, OptionFlags.None, Index.Invalid,
                    Index.Invalid, "-nonewline", null),
                Option.CreateEndOfOptions()
            });
        }

        ///////////////////////////////////////////////////////////////////////

        //
        // NOTE: This is for the [read] command.
        //
        /// <summary>
        /// This method builds the complete option dictionary used by the [read]
        /// command, combining the read-only options with the fixup-return-value
        /// options.
        /// </summary>
        /// <returns>
        /// The newly created option dictionary.
        /// </returns>
        private static OptionDictionary GetReadOptions()
        {
            return new OptionDictionary(
                GetReadOnlyOptions(), GetFixupReturnValueOptions());
        }

        ///////////////////////////////////////////////////////////////////////

        //
        // NOTE: This is for the ToCommandCallback method.
        //
        // NOTE: This method must use the "Unsafe" option flag to prevent
        //       a "safe" interpreter from potentially using an option.
        //
        /// <summary>
        /// This method builds the simplified option dictionary used by the
        /// ToCommandCallback method.  It uses the "Unsafe" option flag to
        /// prevent a "safe" interpreter from potentially using an option.
        /// </summary>
        /// <returns>
        /// The newly created option dictionary.
        /// </returns>
        private static OptionDictionary GetSimpleCallbackOptions()
        {
            //
            // HACK: The "-identifier" option here is special.  It is NOT
            //       actually processed by the core library; instead, it
            //       should be used in situations where there may be more
            //       than one outstanding (asynchronous?, fire-and-forget?)
            //       callback pending, so cleaning up (i.e. removing) one
            //       does not impact the others.  It requires a value and
            //       should be included like this to be effective:
            //
            //                -identifier [expr {random()}]
            //
            return new OptionDictionary(
                new IOption[] {
                new Option(null, OptionFlags.MustHaveValue, /* NOT USED */
                    Index.Invalid, Index.Invalid, "-identifier", null),
                new Option(typeof(BindingFlags),
                    OptionFlags.MustHaveEnumValue | OptionFlags.Unsafe,
                    Index.Invalid, Index.Invalid, "-flags",
                    new Variant(DefaultBindingFlags)),
                new Option(typeof(BindingFlags),
                    OptionFlags.MustHaveEnumValue | OptionFlags.Unsafe,
                    Index.Invalid, Index.Invalid, "-bindingflags",
                    new Variant(DefaultBindingFlags)),
                new Option(typeof(MarshalFlags),
                    OptionFlags.MustHaveEnumValue | OptionFlags.Unsafe,
                    Index.Invalid, Index.Invalid, "-marshalflags",
                    new Variant(DefaultMarshalFlags)),
                Option.CreateEndOfOptions()
            });
        }

        ///////////////////////////////////////////////////////////////////////

        //
        // NOTE: This is for the [object type] and [object untype]
        //       sub-commands.
        //
        /// <summary>
        /// This method builds the option dictionary used by the [object type]
        /// and [object untype] sub-commands.
        /// </summary>
        /// <returns>
        /// The newly created option dictionary.
        /// </returns>
        private static OptionDictionary GetTypeOptions()
        {
            return new OptionDictionary(
                new IOption[] {
                new Option(null, OptionFlags.MustHaveMatchModeValue,
                    Index.Invalid, Index.Invalid, "-typemode",
                    new Variant(DefaultMatchMode)),
                new Option(null, OptionFlags.MustHaveValue, Index.Invalid,
                    Index.Invalid, "-typepattern", null),
                new Option(null, OptionFlags.None, Index.Invalid,
                    Index.Invalid, "-typenocase", null),
                Option.CreateEndOfOptions()
            });
        }

        ///////////////////////////////////////////////////////////////////////

        //
        // NOTE: This is for the [object unaliasnamespace] sub-command.
        //
        /// <summary>
        /// This method builds the option dictionary used by the
        /// [object unaliasnamespace] sub-command.
        /// </summary>
        /// <returns>
        /// The newly created option dictionary.
        /// </returns>
        private static OptionDictionary GetUnaliasNamespaceOptions()
        {
            return new OptionDictionary(
                new IOption[] {
                new Option(null, OptionFlags.None, Index.Invalid,
                    Index.Invalid, "-bycontainer", null),
                new Option(null, OptionFlags.MustHaveMatchModeValue,
                    Index.Invalid, Index.Invalid, "-aliasmode",
                    new Variant(DefaultMatchMode)),
                new Option(null, OptionFlags.MustHaveValue, Index.Invalid,
                    Index.Invalid, "-aliaspattern", null),
                new Option(null, OptionFlags.None, Index.Invalid,
                    Index.Invalid, "-aliasnocase", null),
                Option.CreateEndOfOptions()
            });
        }

        ///////////////////////////////////////////////////////////////////////

        //
        // NOTE: This is for the [object undeclare] sub-command.
        //
        /// <summary>
        /// This method builds the option dictionary used by the
        /// [object undeclare] sub-command.
        /// </summary>
        /// <returns>
        /// The newly created option dictionary.
        /// </returns>
        private static OptionDictionary GetUndeclareOptions()
        {
            return new OptionDictionary(
                new IOption[] {
                new Option(null, OptionFlags.None, Index.Invalid,
                    Index.Invalid, "-bycontainer", null),
                new Option(null, OptionFlags.MustHaveMatchModeValue,
                    Index.Invalid, Index.Invalid, "-declaremode",
                    new Variant(DefaultMatchMode)),
                new Option(null, OptionFlags.MustHaveValue, Index.Invalid,
                    Index.Invalid, "-declarepattern", null),
                new Option(null, OptionFlags.None, Index.Invalid,
                    Index.Invalid, "-declarenocase", null),
                Option.CreateEndOfOptions()
            });
        }

        ///////////////////////////////////////////////////////////////////////

        //
        // NOTE: This is for the [object unimport] sub-command.
        //
        /// <summary>
        /// This method builds the option dictionary used by the
        /// [object unimport] sub-command.
        /// </summary>
        /// <returns>
        /// The newly created option dictionary.
        /// </returns>
        private static OptionDictionary GetUnimportOptions()
        {
            return new OptionDictionary(
                new IOption[] {
                new Option(null, OptionFlags.None, Index.Invalid,
                    Index.Invalid, "-bycontainer", null),
                new Option(null, OptionFlags.MustHaveMatchModeValue,
                    Index.Invalid, Index.Invalid, "-importmode",
                    new Variant(DefaultMatchMode)),
                new Option(null, OptionFlags.MustHaveValue, Index.Invalid,
                    Index.Invalid, "-importpattern", null),
                new Option(null, OptionFlags.None, Index.Invalid,
                    Index.Invalid, "-importnocase", null),
                Option.CreateEndOfOptions()
            });
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method returns the option dictionary that corresponds to the
        /// specified object sub-command option type, building it on demand.
        /// </summary>
        /// <param name="objectOptionType">
        /// The object option type that selects which sub-command option
        /// dictionary should be built and returned.
        /// </param>
        /// <returns>
        /// The newly created option dictionary, or null if the specified
        /// object option type is not recognized.
        /// </returns>
        public static OptionDictionary GetObjectOptions(
            ObjectOptionType objectOptionType
            )
        {
            switch (objectOptionType)
            {
                case ObjectOptionType.Alias:             // [object alias]
                    return GetAliasOptions();            //
                case ObjectOptionType.Call:              // [library call]
                    return GetCallOptions();             //
                case ObjectOptionType.Callback:          // ToCommandCallback
                    return GetCallbackOptions();         //
                case ObjectOptionType.Certificate:       // [object certificate]
                    return GetCertificateOptions();      //
                case ObjectOptionType.Cleanup:           // [object cleanup]
                    return GetCleanupOptions();          //
                case ObjectOptionType.Create:            // [object create]
                    return GetCreateOptions();           //
                case ObjectOptionType.Declare:           // [object declare]
                    return GetDeclareOptions();          //
#if CALLBACK_QUEUE                                       //
                case ObjectOptionType.Dequeue:           // [callback dequeue]
                    return GetDequeueOptions();          //
#endif                                                   //
#if XML && SERIALIZATION                                 //
                case ObjectOptionType.Deserialize:       // [xml deserialize]
                    return GetDeserializeOptions();      //
#endif                                                   //
                case ObjectOptionType.Dispose:           // [object dispose]
                    return GetDisposeOptions();          //
#if NATIVE && TCL
                case ObjectOptionType.Evaluate:          // [tcl eval]
                    return GetEvaluateOptions();         //
#endif                                                   //
#if PREVIOUS_RESULT                                      //
                case ObjectOptionType.Exception:         // [debug exception]
                    return GetExceptionOptions();        //
#endif                                                   //
                case ObjectOptionType.Exec:              // [exec]
                    return GetExecOptions();             //
                case ObjectOptionType.FireCallback:      // CommandCallback
                    return null;                         // N/A
                case ObjectOptionType.FixupReturnValue:  // MarshalOps
                    return GetFixupReturnValueOptions(); //
                case ObjectOptionType.ForEach:           // [object foreach]
                    return GetForEachOptions();          //
                case ObjectOptionType.Get:               // [object get]
                    return GetGetOptions();              //
                case ObjectOptionType.Import:            // [object import]
                    return GetImportOptions();           //
                case ObjectOptionType.Invoke:            // [object invoke]
                    return GetInvokeOptions();           //
                case ObjectOptionType.InvokeOnly:        // [object invoke]
                    return GetInvokeOnlyOptions();       //
                case ObjectOptionType.InvokeRaw:         // [object invokeraw]
                    return GetInvokeRawOptions();        //
                case ObjectOptionType.InvokeRawOnly:     // [object invokeraw]
                    return GetInvokeRawOnlyOptions();    //
                case ObjectOptionType.InvokeAll:         // [object invokeall]
                    return GetInvokeAllOptions();        //
                case ObjectOptionType.InvokeShared:      // [object invoke] / [object invokeraw]
                    return GetInvokeSharedOptions();     //
                case ObjectOptionType.InvokeSharedOnly:  // [object invoke] / [object invokeraw]
                    return GetInvokeSharedOnlyOptions(); //
                case ObjectOptionType.IsDisposed:        // [object isdisposed]
                    return GetIsDisposedOptions();       //
                case ObjectOptionType.IsNull:            // [object isnull]
                    return GetIsNullOptions();           //
                case ObjectOptionType.IsOfType:          // [object isoftype]
                    return GetIsOfTypeOptions();         //
                case ObjectOptionType.Load:              // [object load]
                    return GetLoadOptions();             //
                case ObjectOptionType.Members:           // [object members]
                    return GetMembersOptions();          //
                case ObjectOptionType.Read:              // [read]
                    return GetReadOptions();             //
                case ObjectOptionType.ReadOnly:          // [read]
                    return GetReadOnlyOptions();         //
                case ObjectOptionType.Search:            // [object search]
                    return GetSearchOptions();           //
#if XML && SERIALIZATION                                 //
                case ObjectOptionType.Serialize:         // [xml serialize]
                    return GetSerializeOptions();        //
#endif                                                   //
                case ObjectOptionType.SimpleCallback:    // ToCommandCallback
                    return GetSimpleCallbackOptions();   //
#if DATA                                                 //
                case ObjectOptionType.SqlExecute:        // [sql execute]
                    return GetSqlExecuteOptions();       //
#endif                                                   //
                case ObjectOptionType.Type:              // [object type]
                    return GetTypeOptions();             //
                case ObjectOptionType.UnaliasNamespace:  // [object unaliasnamespace]
                    return GetUnaliasNamespaceOptions(); //
                case ObjectOptionType.Undeclare:         // [object undeclare]
                    return GetUndeclareOptions();        //
                case ObjectOptionType.Unimport:          // [object unimport]
                    return GetUnimportOptions();         //
                case ObjectOptionType.Untype:            // [object untype]
                    return GetTypeOptions();             //
            }

            return null;
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Object Option Processing Helper Methods
        /// <summary>
        /// This method conditionally adjusts the binding flags used when
        /// resolving members for an object sub-command.  It exists primarily to
        /// disable use of a private static constructor when creating an
        /// instance of a primitive or value type.
        /// </summary>
        /// <param name="options">
        /// The option dictionary associated with the sub-command, if any.
        /// </param>
        /// <param name="objectOptionType">
        /// The object option type that identifies the sub-command being
        /// processed.
        /// </param>
        /// <param name="objectType">
        /// The type whose members are being resolved, if any.
        /// </param>
        /// <param name="index">
        /// The argument index in use, or an invalid index when none applies.
        /// </param>
        /// <param name="invoke">
        /// Non-zero if a member is about to be invoked.
        /// </param>
        /// <param name="help">
        /// Upon return, receives non-zero if the matched member should be
        /// looked up in the help file instead of being invoked.
        /// </param>
        /// <param name="bindingFlags">
        /// The binding flags to be examined and possibly modified in place.
        /// </param>
        /// <returns>
        /// True if the binding flags were modified; otherwise, false.
        /// </returns>
        public static bool MaybeMutateBindingFlags(
            OptionDictionary options,          /* in */
            ObjectOptionType objectOptionType, /* in */
            Type objectType,                   /* in */
            int index,                         /* in */
            bool invoke,                       /* in */
            bool help,                         /* in */
            ref BindingFlags bindingFlags      /* in, out */
            )
        {
            //
            // HACK: For use with [object create] only.  This is due to its
            //       use of the Type.GetConstructors method.  Great care is
            //       needed to avoid returning a private static constructor
            //       for primitive types (e.g. System.Boolean).
            //
            if (FlagOps.HasFlags(
                    objectOptionType, ObjectOptionType.Create, true))
            {
                if ((index == Index.Invalid) && invoke && !help &&
                    (objectType != null) &&
                    (objectType.IsPrimitive || objectType.IsValueType))
                {
                    if (FlagOps.HasFlags(
                            bindingFlags, BindingFlags.Static, true) &&
                        FlagOps.HasFlags(
                            bindingFlags, BindingFlags.NonPublic, true))
                    {
                        //
                        // HACK: Using a private static constructor on a
                        //       primitive or value type does not really
                        //       make sense.  By default, disable static
                        //       constructor use in this context.
                        //
                        bindingFlags &= ~BindingFlags.Static;
                        return true;
                    }
                }
            }

            return false;
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Object Option Processing Methods
        //
        // NOTE: For use by the ConversionOps.Dynamic._ToString.FromDateTime
        //       method only.
        //
        /// <summary>
        /// This method extracts the date/time format from the supplied options,
        /// falling back to the specified default and then to the interpreter
        /// settings.  It is a convenience overload that returns only the format
        /// string.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter context that supplies fallback date/time settings,
        /// if any.
        /// </param>
        /// <param name="options">
        /// The option dictionary that may contain the date/time related
        /// options, if any.
        /// </param>
        /// <param name="defaultDateTimeFormat">
        /// The default date/time format to use when none is present in the
        /// options.
        /// </param>
        /// <param name="dateTimeFormat">
        /// Upon return, receives the resolved date/time format string.
        /// </param>
        public static void ProcessDateTimeOptions(
            Interpreter interpreter,
            OptionDictionary options,
            string defaultDateTimeFormat,
            out string dateTimeFormat
            )
        {
            DateTimeKind dateTimeKind;
            DateTimeStyles dateTimeStyles;

            ProcessDateTimeOptions(
                interpreter, options, null, null, defaultDateTimeFormat,
                out dateTimeKind, out dateTimeStyles, out dateTimeFormat);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method extracts the date/time kind, styles, and format from the
        /// supplied options, falling back to the specified defaults and then to
        /// the interpreter settings.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter context that supplies fallback date/time settings,
        /// if any.
        /// </param>
        /// <param name="options">
        /// The option dictionary that may contain the date/time related
        /// options, if any.
        /// </param>
        /// <param name="defaultDateTimeKind">
        /// The default date/time kind to use when none is present in the
        /// options, or null to use the built-in default.
        /// </param>
        /// <param name="defaultDateTimeStyles">
        /// The default date/time styles to use when none is present in the
        /// options, or null to use the built-in default.
        /// </param>
        /// <param name="defaultDateTimeFormat">
        /// The default date/time format to use when none is present in the
        /// options.
        /// </param>
        /// <param name="dateTimeKind">
        /// Upon return, receives the resolved date/time kind.
        /// </param>
        /// <param name="dateTimeStyles">
        /// Upon return, receives the resolved date/time styles.
        /// </param>
        /// <param name="dateTimeFormat">
        /// Upon return, receives the resolved date/time format string.
        /// </param>
        public static void ProcessDateTimeOptions(
            Interpreter interpreter,
            OptionDictionary options,
            DateTimeKind? defaultDateTimeKind,
            DateTimeStyles? defaultDateTimeStyles,
            string defaultDateTimeFormat,
            out DateTimeKind dateTimeKind,
            out DateTimeStyles dateTimeStyles,
            out string dateTimeFormat
            )
        {
            IVariant value = null; /* REUSED */

            ///////////////////////////////////////////////////////////////////

            dateTimeKind = (defaultDateTimeKind != null) ?
                (DateTimeKind)defaultDateTimeKind : DefaultDateTimeKind;

            if ((options != null) &&
                options.CheckPresent("-datetimekind", ref value))
            {
                dateTimeKind = (DateTimeKind)value.Value;
            }
            else if (interpreter != null)
            {
                dateTimeKind = interpreter.DateTimeKind;
            }

            ///////////////////////////////////////////////////////////////////

            dateTimeStyles = (defaultDateTimeStyles != null) ?
                (DateTimeStyles)defaultDateTimeStyles : DefaultDateTimeStyles;

            if ((options != null) &&
                options.CheckPresent("-datetimestyles", ref value))
            {
                dateTimeStyles = (DateTimeStyles)value.Value;
            }
            else if (interpreter != null)
            {
                dateTimeStyles = interpreter.DateTimeStyles;
            }

            ///////////////////////////////////////////////////////////////////

            dateTimeFormat = (defaultDateTimeFormat != null) ?
                defaultDateTimeFormat : DefaultDateTimeFormat;

            if ((options != null) &&
                options.CheckPresent("-datetimeformat", ref value))
            {
                dateTimeFormat = value.ToString();
            }
            else if (interpreter != null)
            {
                dateTimeFormat = interpreter.DateTimeFormat;
            }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method extracts the callback related options (e.g. the return
        /// type, parameter types, and the various flags) from the supplied
        /// options, falling back to the specified defaults where necessary.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter context associated with the operation, if any.
        /// </param>
        /// <param name="options">
        /// The option dictionary that may contain the callback related
        /// options, if any.
        /// </param>
        /// <param name="defaultMarshalFlags">
        /// The default marshal flags to use when none is present in the
        /// options, or null to use the built-in default.
        /// </param>
        /// <param name="defaultObjectFlags">
        /// The default object flags to use when none is present in the
        /// options, or null to use the built-in default.
        /// </param>
        /// <param name="defaultByRefArgumentFlags">
        /// The default by-reference argument flags to use when none is present
        /// in the options, or null to use the built-in default.
        /// </param>
        /// <param name="defaultCallbackFlags">
        /// The default callback flags to use when none is present in the
        /// options, or null to use the built-in default.
        /// </param>
        /// <param name="returnType">
        /// Upon return, receives the resolved callback return type, if any.
        /// </param>
        /// <param name="parameterTypes">
        /// Upon return, receives the resolved list of callback parameter
        /// types, if any.
        /// </param>
        /// <param name="parameterMarshalFlags">
        /// Upon return, receives the resolved list of per-parameter marshal
        /// flags, if any.
        /// </param>
        /// <param name="marshalFlags">
        /// Upon return, receives the resolved marshal flags.
        /// </param>
        /// <param name="objectFlags">
        /// Upon return, receives the resolved object flags.
        /// </param>
        /// <param name="byRefArgumentFlags">
        /// Upon return, receives the resolved by-reference argument flags.
        /// </param>
        /// <param name="callbackFlags">
        /// Upon return, receives the resolved callback flags.
        /// </param>
        public static void ProcessCallbackOptions(
            Interpreter interpreter,
            OptionDictionary options,
            MarshalFlags? defaultMarshalFlags,
            ObjectFlags? defaultObjectFlags,
            ByRefArgumentFlags? defaultByRefArgumentFlags,
            CallbackFlags? defaultCallbackFlags,
            out Type returnType,
            out TypeList parameterTypes,
            out MarshalFlagsList parameterMarshalFlags,
            out MarshalFlags marshalFlags,
            out ObjectFlags objectFlags,
            out ByRefArgumentFlags byRefArgumentFlags,
            out CallbackFlags callbackFlags
            )
        {
            IVariant value = null; /* REUSED */

            ///////////////////////////////////////////////////////////////////

            returnType = null;

            if ((options != null) &&
                options.CheckPresent("-returntype", ref value))
            {
                returnType = (Type)value.Value;
            }

            ///////////////////////////////////////////////////////////////////

            parameterTypes = null;

            if ((options != null) &&
                options.CheckPresent("-parametertypes", ref value))
            {
                parameterTypes = (TypeList)value.Value;
            }

            ///////////////////////////////////////////////////////////////////

            parameterMarshalFlags = null;

            if ((options != null) &&
                options.CheckPresent("-parametermarshalflags", ref value))
            {
                parameterMarshalFlags = MarshalOps.GetParameterMarshalFlags(
                    (EnumList)value.Value);
            }

            ///////////////////////////////////////////////////////////////////

            marshalFlags = (defaultMarshalFlags != null) ?
                (MarshalFlags)defaultMarshalFlags : DefaultMarshalFlags;

            if ((options != null) &&
                options.CheckPresent("-marshalflags", ref value))
            {
                marshalFlags = (MarshalFlags)value.Value;
            }

            ///////////////////////////////////////////////////////////////////

            objectFlags = (defaultObjectFlags != null) ?
                (ObjectFlags)defaultObjectFlags : DefaultObjectFlags;

            if ((options != null) &&
                options.CheckPresent("-objectflags", ref value))
            {
                objectFlags = (ObjectFlags)value.Value;
            }

            ///////////////////////////////////////////////////////////////////

            byRefArgumentFlags = (defaultByRefArgumentFlags != null) ?
                (ByRefArgumentFlags)defaultByRefArgumentFlags :
                DefaultByRefArgumentFlags;

            if ((options != null) &&
                options.CheckPresent("-argumentflags", ref value))
            {
                byRefArgumentFlags = (ByRefArgumentFlags)value.Value;
            }

            ///////////////////////////////////////////////////////////////////

            callbackFlags = (defaultCallbackFlags != null) ?
                (CallbackFlags)defaultCallbackFlags : DefaultCallbackFlags;

            if ((options != null) &&
                options.CheckPresent("-callbackflags", ref value))
            {
                callbackFlags = (CallbackFlags)value.Value;
            }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method extracts the binding flags and marshal flags used for a
        /// simple callback from the supplied options, falling back to the
        /// specified defaults where necessary.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter context associated with the operation, if any.
        /// </param>
        /// <param name="options">
        /// The option dictionary that may contain the callback related
        /// options, if any.
        /// </param>
        /// <param name="defaultBindingFlags">
        /// The default binding flags to use when none is present in the
        /// options, or null to use the built-in default.
        /// </param>
        /// <param name="defaultMarshalFlags">
        /// The default marshal flags to use when none is present in the
        /// options, or null to use the built-in default.
        /// </param>
        /// <param name="bindingFlags">
        /// Upon return, receives the resolved binding flags.
        /// </param>
        /// <param name="marshalFlags">
        /// Upon return, receives the resolved marshal flags.
        /// </param>
        public static void ProcessSimpleCallbackOptions(
            Interpreter interpreter,
            OptionDictionary options,
            BindingFlags? defaultBindingFlags,
            MarshalFlags? defaultMarshalFlags,
            out BindingFlags bindingFlags,
            out MarshalFlags marshalFlags
            )
        {
            IVariant value = null; /* REUSED */

            ///////////////////////////////////////////////////////////////////

            marshalFlags = (defaultMarshalFlags != null) ?
                (MarshalFlags)defaultMarshalFlags : DefaultMarshalFlags;

            if ((options != null) &&
                options.CheckPresent("-marshalflags", ref value))
            {
                marshalFlags = (MarshalFlags)value.Value;
            }

            ///////////////////////////////////////////////////////////////////

            bindingFlags = (defaultBindingFlags != null) ?
                (BindingFlags)defaultBindingFlags : DefaultBindingFlags;

            //
            // TODO: Is this a really bad option name?
            //
            bool hadFlags = (options != null) &&
                options.CheckPresent("-flags", ref value);

            if (hadFlags)
                bindingFlags = (BindingFlags)value.Value;

            if ((options != null) &&
                options.CheckPresent("-bindingflags", ref value))
            {
                if (hadFlags)
                    bindingFlags |= (BindingFlags)value.Value;
                else
                    bindingFlags = (BindingFlags)value.Value;
            }
        }

        ///////////////////////////////////////////////////////////////////////

#if DATA
        //
        // NOTE: This is for the [sql execute] sub-command.
        //
        /// <summary>
        /// This method extracts the many options used by the [sql execute]
        /// sub-command from the supplied options, falling back to the specified
        /// defaults and then to the interpreter settings where necessary.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter context that supplies fallback settings, if any.
        /// </param>
        /// <param name="options">
        /// The option dictionary that may contain the database execution
        /// related options, if any.
        /// </param>
        /// <param name="defaultCommandType">
        /// The default command type to use when none is present in the options,
        /// or null to use the built-in default.
        /// </param>
        /// <param name="defaultCommandBehavior">
        /// The default command behavior to use when none is present in the
        /// options, or null to use the built-in default.
        /// </param>
        /// <param name="defaultExecuteType">
        /// The default execution type to use when none is present in the
        /// options, or null to use the built-in default.
        /// </param>
        /// <param name="defaultResultFormat">
        /// The default result format to use when none is present in the
        /// options, or null to use the built-in default.
        /// </param>
        /// <param name="defaultValueFlags">
        /// The default value flags to use when none is present in the options,
        /// or null to use the built-in default.
        /// </param>
        /// <param name="defaultBlobBehavior">
        /// The default BLOB behavior to use when none is present in the
        /// options, or null to use the built-in default.
        /// </param>
        /// <param name="defaultDateTimeBehavior">
        /// The default date/time behavior to use when none is present in the
        /// options, or null to use the built-in default.
        /// </param>
        /// <param name="defaultDateTimeKind">
        /// The default date/time kind to use when none is present in the
        /// options, or null to use the built-in default.
        /// </param>
        /// <param name="defaultDateTimeStyles">
        /// The default date/time styles to use when none is present in the
        /// options, or null to use the built-in default.
        /// </param>
        /// <param name="cultureInfo">
        /// Upon return, receives the resolved culture, if any.
        /// </param>
        /// <param name="commandType">
        /// Upon return, receives the resolved command type.
        /// </param>
        /// <param name="commandBehavior">
        /// Upon return, receives the resolved command behavior.
        /// </param>
        /// <param name="executeType">
        /// Upon return, receives the resolved execution type.
        /// </param>
        /// <param name="resultFormat">
        /// Upon return, receives the resolved result format.
        /// </param>
        /// <param name="valueFlags">
        /// Upon return, receives the resolved value flags.
        /// </param>
        /// <param name="blobBehavior">
        /// Upon return, receives the resolved BLOB behavior.
        /// </param>
        /// <param name="dateTimeBehavior">
        /// Upon return, receives the resolved date/time behavior.
        /// </param>
        /// <param name="dateTimeKind">
        /// Upon return, receives the resolved date/time kind.
        /// </param>
        /// <param name="dateTimeStyles">
        /// Upon return, receives the resolved date/time styles.
        /// </param>
        /// <param name="changedCallback">
        /// Upon return, receives the resolved change-notification callback, if
        /// any.
        /// </param>
        /// <param name="rowsVarName">
        /// Upon return, receives the name of the variable that will receive the
        /// row data, if any.
        /// </param>
        /// <param name="timeVarName">
        /// Upon return, receives the name of the variable that will receive the
        /// elapsed time, if any.
        /// </param>
        /// <param name="valueFormat">
        /// Upon return, receives the resolved value format string, if any.
        /// </param>
        /// <param name="dateTimeFormat">
        /// Upon return, receives the resolved date/time format string, if any.
        /// </param>
        /// <param name="numberFormat">
        /// Upon return, receives the resolved number format string, if any.
        /// </param>
        /// <param name="nullValue">
        /// Upon return, receives the string used to represent a null value, if
        /// any.
        /// </param>
        /// <param name="dbNullValue">
        /// Upon return, receives the string used to represent a database null
        /// value, if any.
        /// </param>
        /// <param name="errorValue">
        /// Upon return, receives the string used to represent an error value,
        /// if any.
        /// </param>
        /// <param name="commandTimeout">
        /// Upon return, receives the resolved command timeout, if any.
        /// </param>
        /// <param name="limit">
        /// Upon return, receives the resolved maximum row limit.
        /// </param>
        /// <param name="nested">
        /// Upon return, receives non-zero if nested result formatting is
        /// enabled.
        /// </param>
        /// <param name="allowNull">
        /// Upon return, receives non-zero if null values are permitted.
        /// </param>
        /// <param name="pairs">
        /// Upon return, receives non-zero if results should be formatted as
        /// name/value pairs.
        /// </param>
        /// <param name="names">
        /// Upon return, receives non-zero if column names should be included.
        /// </param>
        /// <param name="time">
        /// Upon return, receives non-zero if elapsed time should be captured.
        /// </param>
        /// <param name="verbatim">
        /// Upon return, receives non-zero if values should be treated
        /// verbatim.
        /// </param>
        /// <param name="noFixup">
        /// Upon return, receives non-zero if value fixup should be skipped.
        /// </param>
        public static void ProcessExecuteOptions(
            Interpreter interpreter,
            OptionDictionary options,
            CommandType? defaultCommandType,
            CommandBehavior? defaultCommandBehavior,
            DbExecuteType? defaultExecuteType,
            DbResultFormat? defaultResultFormat,
            ValueFlags? defaultValueFlags,
            BlobBehavior? defaultBlobBehavior,
            DateTimeBehavior? defaultDateTimeBehavior,
            DateTimeKind? defaultDateTimeKind,
            DateTimeStyles? defaultDateTimeStyles,
            out CultureInfo cultureInfo,
            out CommandType commandType,
            out CommandBehavior commandBehavior,
            out DbExecuteType executeType,
            out DbResultFormat resultFormat,
            out ValueFlags valueFlags,
            out BlobBehavior blobBehavior,
            out DateTimeBehavior dateTimeBehavior,
            out DateTimeKind dateTimeKind,
            out DateTimeStyles dateTimeStyles,
            out ICallback changedCallback,
            out string rowsVarName,
            out string timeVarName,
            out string valueFormat,
            out string dateTimeFormat,
            out string numberFormat,
            out string nullValue,
            out string dbNullValue,
            out string errorValue,
            out int? commandTimeout,
            out int limit,
            out bool nested,
            out bool allowNull,
            out bool pairs,
            out bool names,
            out bool time,
            out bool verbatim,
            out bool noFixup
            )
        {
            IVariant value = null; /* REUSED */

            ///////////////////////////////////////////////////////////////////

            cultureInfo = null;

            if ((options != null) &&
                options.CheckPresent("-culture", ref value))
            {
                cultureInfo = (CultureInfo)value.Value;
            }

            ///////////////////////////////////////////////////////////////////

            commandType = (defaultCommandType != null) ?
                (CommandType)defaultCommandType : DefaultCommandType;

            if ((options != null) &&
                options.CheckPresent("-commandtype", ref value))
            {
                commandType = (CommandType)value.Value;
            }

            ///////////////////////////////////////////////////////////////////

            commandBehavior = (defaultCommandBehavior != null) ?
                (CommandBehavior)defaultCommandBehavior : DefaultCommandBehavior;

            if ((options != null) &&
                options.CheckPresent("-behavior", ref value))
            {
                commandBehavior = (CommandBehavior)value.Value;
            }

            ///////////////////////////////////////////////////////////////////

            executeType = (defaultExecuteType != null) ?
                (DbExecuteType)defaultExecuteType : DefaultExecuteType;

            if ((options != null) &&
                options.CheckPresent("-execute", ref value))
            {
                executeType = (DbExecuteType)value.Value;
            }

            ///////////////////////////////////////////////////////////////////

            resultFormat = (defaultResultFormat != null) ?
                (DbResultFormat)defaultResultFormat : DefaultResultFormat;

            if ((options != null) &&
                options.CheckPresent("-format", ref value))
            {
                resultFormat = (DbResultFormat)value.Value;
            }

            ///////////////////////////////////////////////////////////////////

            valueFlags = (defaultValueFlags != null) ?
                (ValueFlags)defaultValueFlags : DefaultValueFlags;

            if ((options != null) &&
                options.CheckPresent("-valueflags", ref value))
            {
                valueFlags = (ValueFlags)value.Value;
            }

            ///////////////////////////////////////////////////////////////////

            blobBehavior = (defaultBlobBehavior != null) ?
                (BlobBehavior)defaultBlobBehavior : DefaultBlobBehavior;

            if ((options != null) &&
                options.CheckPresent("-blobbehavior", ref value))
            {
                blobBehavior = (BlobBehavior)value.Value;
            }

            ///////////////////////////////////////////////////////////////////

            dateTimeBehavior = (defaultDateTimeBehavior != null) ?
                (DateTimeBehavior)defaultDateTimeBehavior : DefaultDateTimeBehavior;

            if ((options != null) &&
                options.CheckPresent("-datetimebehavior", ref value))
            {
                dateTimeBehavior = (DateTimeBehavior)value.Value;
            }

            ///////////////////////////////////////////////////////////////////

            dateTimeKind = (defaultDateTimeKind != null) ?
                (DateTimeKind)defaultDateTimeKind : DefaultDateTimeKind;

            if ((options != null) &&
                options.CheckPresent("-datetimekind", ref value))
            {
                dateTimeKind = (DateTimeKind)value.Value;
            }

            ///////////////////////////////////////////////////////////////////

            dateTimeStyles = (defaultDateTimeStyles != null) ?
                (DateTimeStyles)defaultDateTimeStyles : DefaultDateTimeStyles;

            if ((options != null) &&
                options.CheckPresent("-datetimestyles", ref value))
            {
                dateTimeStyles = (DateTimeStyles)value.Value;
            }

            ///////////////////////////////////////////////////////////////////

            changedCallback = null;

            if ((options != null) &&
                options.CheckPresent("-changed", ref value))
            {
                changedCallback = (ICallback)value.Value;
            }

            ///////////////////////////////////////////////////////////////////

            rowsVarName = null;

            if ((options != null) &&
                options.CheckPresent("-rowsvar", ref value))
            {
                rowsVarName = value.ToString();
            }

            if ((options != null) &&
                options.CheckPresent("-rowvar", ref value))
            {
                rowsVarName = value.ToString();
            }

            ///////////////////////////////////////////////////////////////////

            timeVarName = null;

            if ((options != null) &&
                options.CheckPresent("-timevar", ref value))
            {
                timeVarName = value.ToString();
            }

            ///////////////////////////////////////////////////////////////////

            valueFormat = null;

            if ((options != null) &&
                options.CheckPresent("-valueformat", ref value))
            {
                valueFormat = value.ToString();
            }

            ///////////////////////////////////////////////////////////////////

            dateTimeFormat = null;

            if ((options != null) &&
                options.CheckPresent("-datetimeformat", ref value))
            {
                dateTimeFormat = value.ToString();
            }

            ///////////////////////////////////////////////////////////////////

            numberFormat = null;

            if ((options != null) &&
                options.CheckPresent("-numberformat", ref value))
            {
                numberFormat = value.ToString();
            }

            ///////////////////////////////////////////////////////////////////

            nullValue = null;

            if ((options != null) &&
                options.CheckPresent("-nullvalue", ref value))
            {
                nullValue = value.ToString();
            }

            ///////////////////////////////////////////////////////////////////

            dbNullValue = null;

            if ((options != null) &&
                options.CheckPresent("-dbnullvalue", ref value))
            {
                dbNullValue = value.ToString();
            }

            ///////////////////////////////////////////////////////////////////

            errorValue = null;

            if ((options != null) &&
                options.CheckPresent("-errorvalue", ref value))
            {
                errorValue = value.ToString();
            }

            ///////////////////////////////////////////////////////////////////

            commandTimeout = null;

            if ((options != null) &&
                options.CheckPresent("-timeout", ref value))
            {
                commandTimeout = (int)value.Value;
            }

            ///////////////////////////////////////////////////////////////////

            limit = 0;

            if ((options != null) &&
                options.CheckPresent("-limit", ref value))
            {
                limit = (int)value.Value;
            }

            ///////////////////////////////////////////////////////////////////

            nested = false;

            if ((options != null) &&
                options.CheckPresent("-nested", ref value))
            {
                nested = (bool)value.Value;
            }

            ///////////////////////////////////////////////////////////////////

            allowNull = false;

            if ((options != null) &&
                options.CheckPresent("-allownull", ref value))
            {
                allowNull = (bool)value.Value;
            }

            ///////////////////////////////////////////////////////////////////

            pairs = true;

            if ((options != null) &&
                options.CheckPresent("-pairs", ref value))
            {
                pairs = (bool)value.Value;
            }

            ///////////////////////////////////////////////////////////////////

            names = true;

            if ((options != null) &&
                options.CheckPresent("-names", ref value))
            {
                names = (bool)value.Value;
            }

            ///////////////////////////////////////////////////////////////////

            time = false;

            if ((options != null) && options.CheckPresent("-time"))
                time = true;

            ///////////////////////////////////////////////////////////////////

            verbatim = false;

            if ((options != null) && options.CheckPresent("-verbatim"))
                verbatim = true;

            ///////////////////////////////////////////////////////////////////

            noFixup = false;

            if ((options != null) &&
                options.CheckPresent("-nofixup", ref value))
            {
                noFixup = (bool)value.Value;
            }
        }
#endif

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method extracts the options that control method discovery and
        /// argument fixup for an object sub-command, falling back to the
        /// specified defaults where necessary.  This convenience overload omits
        /// several of the more detailed outputs and forwards to the most
        /// general overload.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter context that supplies fallback settings, if any.
        /// </param>
        /// <param name="options">
        /// The option dictionary that may contain the member lookup related
        /// options, if any.
        /// </param>
        /// <param name="objectOptionType">
        /// The object option type that identifies the sub-command being
        /// processed.
        /// </param>
        /// <param name="defaultBindingFlags">
        /// The default binding flags to use when none is present in the
        /// options, or null to use the built-in default.
        /// </param>
        /// <param name="defaultMarshalFlags">
        /// The default marshal flags to use when none is present in the
        /// options, or null to use the built-in default.
        /// </param>
        /// <param name="defaultReorderFlags">
        /// The default reorder flags to use when none is present in the
        /// options, or null to use the built-in default.
        /// </param>
        /// <param name="defaultByRefArgumentFlags">
        /// The default by-reference argument flags to use when none is present
        /// in the options, or null to use the built-in default.
        /// </param>
        /// <param name="bindingFlags">
        /// Upon return, receives the resolved binding flags.
        /// </param>
        /// <param name="marshalFlags">
        /// Upon return, receives the resolved marshal flags.
        /// </param>
        /// <param name="reorderFlags">
        /// Upon return, receives the resolved reorder flags.
        /// </param>
        /// <param name="byRefArgumentFlags">
        /// Upon return, receives the resolved by-reference argument flags.
        /// </param>
        /// <param name="limit">
        /// Upon return, receives the resolved overload-matching limit.
        /// </param>
        /// <param name="index">
        /// Upon return, receives the resolved argument index.
        /// </param>
        /// <param name="noByRef">
        /// Upon return, receives non-zero if by-reference argument handling is
        /// disabled.
        /// </param>
        /// <param name="strictMember">
        /// Upon return, receives non-zero if strict member matching is enabled.
        /// </param>
        /// <param name="strictArgs">
        /// Upon return, receives non-zero if strict argument matching is
        /// enabled.
        /// </param>
        /// <param name="invoke">
        /// Upon return, receives non-zero if the matched member should be
        /// invoked.
        /// </param>
        /// <param name="help">
        /// Upon return, receives non-zero if the matched member should be
        /// looked up in the help file instead of being invoked.
        /// </param>
        /// <param name="noArgs">
        /// Upon return, receives non-zero if argument processing is disabled.
        /// </param>
        /// <param name="arrayAsValue">
        /// Upon return, receives non-zero if arrays should be treated as
        /// values.
        /// </param>
        /// <param name="arrayAsLink">
        /// Upon return, receives non-zero if arrays should be treated as
        /// linked variables.
        /// </param>
        /// <param name="debug">
        /// Upon return, receives non-zero if debug output is enabled.
        /// </param>
        /// <param name="trace">
        /// Upon return, receives non-zero if trace output is enabled.
        /// </param>
        public static void ProcessFindMethodsAndFixupArgumentsOptions(
            Interpreter interpreter,
            OptionDictionary options,
            ObjectOptionType objectOptionType,
            BindingFlags? defaultBindingFlags,
            MarshalFlags? defaultMarshalFlags,
            ReorderFlags? defaultReorderFlags,
            ByRefArgumentFlags? defaultByRefArgumentFlags,
            out BindingFlags bindingFlags,
            out MarshalFlags marshalFlags,
            out ReorderFlags reorderFlags,
            out ByRefArgumentFlags byRefArgumentFlags,
            out int limit,
            out int index,
            out bool noByRef,
            out bool strictMember,
            out bool strictArgs,
            out bool invoke,
            out bool help,
            out bool noArgs,
            out bool arrayAsValue,
            out bool arrayAsLink,
            out bool debug,
            out bool trace
            )
        {
            Type objectType;
            Type proxyType;
            TypeList objectTypes;
            TypeList methodTypes;
            TypeList parameterTypes;
            MarshalFlagsList parameterMarshalFlags;
            ValueFlags objectValueFlags;
            ValueFlags memberValueFlags;
            MemberTypes memberTypes;
            bool verbose;
            bool strictType;
            bool identity;
            bool typeIdentity;
            bool noNestedObject;
            bool noNestedMember;
            bool noCase;
            bool noMutateBindingFlags;

            ProcessFindMethodsAndFixupArgumentsOptions(
                interpreter, options, objectOptionType, null, null, null,
                defaultBindingFlags, defaultMarshalFlags, defaultReorderFlags,
                defaultByRefArgumentFlags, out objectType, out proxyType,
                out objectTypes, out methodTypes, out parameterTypes,
                out parameterMarshalFlags, out objectValueFlags,
                out memberValueFlags, out memberTypes, out bindingFlags,
                out marshalFlags, out reorderFlags, out byRefArgumentFlags,
                out limit, out index, out noByRef, out verbose,
                out strictType, out strictMember, out strictArgs,
                out identity, out typeIdentity, out noNestedObject,
                out noNestedMember, out noCase, out invoke, out help,
                out noArgs, out arrayAsValue, out arrayAsLink,
                out noMutateBindingFlags, out debug, out trace);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method extracts the options that control method discovery and
        /// argument fixup for an object sub-command, falling back to the
        /// specified defaults where necessary.  This convenience overload omits
        /// several of the more detailed outputs and forwards to the most
        /// general overload.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter context that supplies fallback settings, if any.
        /// </param>
        /// <param name="options">
        /// The option dictionary that may contain the member lookup related
        /// options, if any.
        /// </param>
        /// <param name="objectOptionType">
        /// The object option type that identifies the sub-command being
        /// processed.
        /// </param>
        /// <param name="defaultBindingFlags">
        /// The default binding flags to use when none is present in the
        /// options, or null to use the built-in default.
        /// </param>
        /// <param name="defaultMarshalFlags">
        /// The default marshal flags to use when none is present in the
        /// options, or null to use the built-in default.
        /// </param>
        /// <param name="defaultReorderFlags">
        /// The default reorder flags to use when none is present in the
        /// options, or null to use the built-in default.
        /// </param>
        /// <param name="defaultByRefArgumentFlags">
        /// The default by-reference argument flags to use when none is present
        /// in the options, or null to use the built-in default.
        /// </param>
        /// <param name="bindingFlags">
        /// Upon return, receives the resolved binding flags.
        /// </param>
        /// <param name="marshalFlags">
        /// Upon return, receives the resolved marshal flags.
        /// </param>
        /// <param name="reorderFlags">
        /// Upon return, receives the resolved reorder flags.
        /// </param>
        /// <param name="byRefArgumentFlags">
        /// Upon return, receives the resolved by-reference argument flags.
        /// </param>
        /// <param name="methodTypes">
        /// Upon return, receives the resolved list of method (signature) types,
        /// if any.
        /// </param>
        /// <param name="parameterTypes">
        /// Upon return, receives the resolved list of parameter types, if any.
        /// </param>
        /// <param name="parameterMarshalFlags">
        /// Upon return, receives the resolved list of per-parameter marshal
        /// flags, if any.
        /// </param>
        /// <param name="limit">
        /// Upon return, receives the resolved overload-matching limit.
        /// </param>
        /// <param name="index">
        /// Upon return, receives the resolved argument index.
        /// </param>
        /// <param name="noByRef">
        /// Upon return, receives non-zero if by-reference argument handling is
        /// disabled.
        /// </param>
        /// <param name="strictMember">
        /// Upon return, receives non-zero if strict member matching is enabled.
        /// </param>
        /// <param name="strictArgs">
        /// Upon return, receives non-zero if strict argument matching is
        /// enabled.
        /// </param>
        /// <param name="invoke">
        /// Upon return, receives non-zero if the matched member should be
        /// invoked.
        /// </param>
        /// <param name="help">
        /// Upon return, receives non-zero if the matched member should be
        /// looked up in the help file instead of being invoked.
        /// </param>
        /// <param name="noArgs">
        /// Upon return, receives non-zero if argument processing is disabled.
        /// </param>
        /// <param name="arrayAsValue">
        /// Upon return, receives non-zero if arrays should be treated as
        /// values.
        /// </param>
        /// <param name="arrayAsLink">
        /// Upon return, receives non-zero if arrays should be treated as
        /// linked variables.
        /// </param>
        /// <param name="debug">
        /// Upon return, receives non-zero if debug output is enabled.
        /// </param>
        /// <param name="trace">
        /// Upon return, receives non-zero if trace output is enabled.
        /// </param>
        private static void ProcessFindMethodsAndFixupArgumentsOptions(
            Interpreter interpreter,
            OptionDictionary options,
            ObjectOptionType objectOptionType,
            BindingFlags? defaultBindingFlags,
            MarshalFlags? defaultMarshalFlags,
            ReorderFlags? defaultReorderFlags,
            ByRefArgumentFlags? defaultByRefArgumentFlags,
            out BindingFlags bindingFlags,
            out MarshalFlags marshalFlags,
            out ReorderFlags reorderFlags,
            out ByRefArgumentFlags byRefArgumentFlags,
            out TypeList methodTypes,
            out TypeList parameterTypes,
            out MarshalFlagsList parameterMarshalFlags,
            out int limit,
            out int index,
            out bool noByRef,
            out bool strictMember,
            out bool strictArgs,
            out bool invoke,
            out bool help,
            out bool noArgs,
            out bool arrayAsValue,
            out bool arrayAsLink,
            out bool debug,
            out bool trace
            )
        {
            Type objectType;
            Type proxyType;
            TypeList objectTypes;
            ValueFlags objectValueFlags;
            ValueFlags memberValueFlags;
            MemberTypes memberTypes;
            bool verbose;
            bool strictType;
            bool identity;
            bool typeIdentity;
            bool noNestedObject;
            bool noNestedMember;
            bool noCase;
            bool noMutateBindingFlags;

            ProcessFindMethodsAndFixupArgumentsOptions(
                interpreter, options, objectOptionType, null, null, null,
                defaultBindingFlags, defaultMarshalFlags, defaultReorderFlags,
                defaultByRefArgumentFlags, out objectType, out proxyType,
                out objectTypes, out methodTypes, out parameterTypes,
                out parameterMarshalFlags, out objectValueFlags,
                out memberValueFlags, out memberTypes, out bindingFlags,
                out marshalFlags, out reorderFlags, out byRefArgumentFlags,
                out limit, out index, out noByRef, out verbose,
                out strictType, out strictMember, out strictArgs,
                out identity, out typeIdentity, out noNestedObject,
                out noNestedMember, out noCase, out invoke, out help,
                out noArgs, out arrayAsValue, out arrayAsLink,
                out noMutateBindingFlags, out debug, out trace);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method extracts the options that control method discovery and
        /// argument fixup for an object sub-command, falling back to the
        /// specified defaults where necessary.  This convenience overload omits
        /// several of the more detailed outputs and forwards to the most
        /// general overload.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter context that supplies fallback settings, if any.
        /// </param>
        /// <param name="options">
        /// The option dictionary that may contain the member lookup related
        /// options, if any.
        /// </param>
        /// <param name="objectOptionType">
        /// The object option type that identifies the sub-command being
        /// processed.
        /// </param>
        /// <param name="defaultObjectValueFlags">
        /// The default object value flags to use when none is present in the
        /// options, or null to use the built-in default.
        /// </param>
        /// <param name="defaultBindingFlags">
        /// The default binding flags to use when none is present in the
        /// options, or null to use the built-in default.
        /// </param>
        /// <param name="defaultMarshalFlags">
        /// The default marshal flags to use when none is present in the
        /// options, or null to use the built-in default.
        /// </param>
        /// <param name="defaultReorderFlags">
        /// The default reorder flags to use when none is present in the
        /// options, or null to use the built-in default.
        /// </param>
        /// <param name="defaultByRefArgumentFlags">
        /// The default by-reference argument flags to use when none is present
        /// in the options, or null to use the built-in default.
        /// </param>
        /// <param name="objectTypes">
        /// Upon return, receives the resolved list of object types, if any.
        /// </param>
        /// <param name="methodTypes">
        /// Upon return, receives the resolved list of method (signature) types,
        /// if any.
        /// </param>
        /// <param name="parameterTypes">
        /// Upon return, receives the resolved list of parameter types, if any.
        /// </param>
        /// <param name="parameterMarshalFlags">
        /// Upon return, receives the resolved list of per-parameter marshal
        /// flags, if any.
        /// </param>
        /// <param name="objectValueFlags">
        /// Upon return, receives the resolved object value flags.
        /// </param>
        /// <param name="bindingFlags">
        /// Upon return, receives the resolved binding flags.
        /// </param>
        /// <param name="marshalFlags">
        /// Upon return, receives the resolved marshal flags.
        /// </param>
        /// <param name="reorderFlags">
        /// Upon return, receives the resolved reorder flags.
        /// </param>
        /// <param name="byRefArgumentFlags">
        /// Upon return, receives the resolved by-reference argument flags.
        /// </param>
        /// <param name="limit">
        /// Upon return, receives the resolved overload-matching limit.
        /// </param>
        /// <param name="index">
        /// Upon return, receives the resolved argument index.
        /// </param>
        /// <param name="noByRef">
        /// Upon return, receives non-zero if by-reference argument handling is
        /// disabled.
        /// </param>
        /// <param name="strictType">
        /// Upon return, receives non-zero if strict type matching is enabled.
        /// </param>
        /// <param name="strictMember">
        /// Upon return, receives non-zero if strict member matching is enabled.
        /// </param>
        /// <param name="strictArgs">
        /// Upon return, receives non-zero if strict argument matching is
        /// enabled.
        /// </param>
        /// <param name="noCase">
        /// Upon return, receives non-zero if case-insensitive matching is
        /// enabled.
        /// </param>
        /// <param name="invoke">
        /// Upon return, receives non-zero if the matched member should be
        /// invoked.
        /// </param>
        /// <param name="help">
        /// Upon return, receives non-zero if the matched member should be
        /// looked up in the help file instead of being invoked.
        /// </param>
        /// <param name="noArgs">
        /// Upon return, receives non-zero if argument processing is disabled.
        /// </param>
        /// <param name="arrayAsValue">
        /// Upon return, receives non-zero if arrays should be treated as
        /// values.
        /// </param>
        /// <param name="arrayAsLink">
        /// Upon return, receives non-zero if arrays should be treated as
        /// linked variables.
        /// </param>
        /// <param name="noMutateBindingFlags">
        /// Upon return, receives non-zero if automatic mutation of the binding
        /// flags is disabled.
        /// </param>
        /// <param name="debug">
        /// Upon return, receives non-zero if debug output is enabled.
        /// </param>
        /// <param name="trace">
        /// Upon return, receives non-zero if trace output is enabled.
        /// </param>
        public static void ProcessFindMethodsAndFixupArgumentsOptions(
            Interpreter interpreter,
            OptionDictionary options,
            ObjectOptionType objectOptionType,
            ValueFlags? defaultObjectValueFlags,
            BindingFlags? defaultBindingFlags,
            MarshalFlags? defaultMarshalFlags,
            ReorderFlags? defaultReorderFlags,
            ByRefArgumentFlags? defaultByRefArgumentFlags,
            out TypeList objectTypes,
            out TypeList methodTypes,
            out TypeList parameterTypes,
            out MarshalFlagsList parameterMarshalFlags,
            out ValueFlags objectValueFlags,
            out BindingFlags bindingFlags,
            out MarshalFlags marshalFlags,
            out ReorderFlags reorderFlags,
            out ByRefArgumentFlags byRefArgumentFlags,
            out int limit,
            out int index,
            out bool noByRef,
            out bool strictType,
            out bool strictMember,
            out bool strictArgs,
            out bool noCase,
            out bool invoke,
            out bool help,
            out bool noArgs,
            out bool arrayAsValue,
            out bool arrayAsLink,
            out bool noMutateBindingFlags,
            out bool debug,
            out bool trace
            )
        {
            Type objectType;
            Type proxyType;
            ValueFlags memberValueFlags;
            MemberTypes memberTypes;
            bool verbose;
            bool identity;
            bool typeIdentity;
            bool noNestedObject;
            bool noNestedMember;

            ProcessFindMethodsAndFixupArgumentsOptions(
                interpreter, options, objectOptionType,
                defaultObjectValueFlags, null, null, defaultBindingFlags,
                defaultMarshalFlags, defaultReorderFlags,
                defaultByRefArgumentFlags, out objectType, out proxyType,
                out objectTypes, out methodTypes, out parameterTypes,
                out parameterMarshalFlags, out objectValueFlags,
                out memberValueFlags, out memberTypes, out bindingFlags,
                out marshalFlags, out reorderFlags, out byRefArgumentFlags,
                out limit, out index, out noByRef, out verbose,
                out strictType, out strictMember, out strictArgs,
                out identity, out typeIdentity, out noNestedObject,
                out noNestedMember, out noCase, out invoke, out help,
                out noArgs, out arrayAsValue, out arrayAsLink,
                out noMutateBindingFlags, out debug, out trace);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method extracts the complete set of options that control method
        /// discovery and argument fixup for an object sub-command, falling back
        /// to the specified defaults and then to the interpreter settings where
        /// necessary.  This is the most general overload to which the others
        /// forward.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter context that supplies fallback settings, if any.
        /// </param>
        /// <param name="options">
        /// The option dictionary that may contain the member lookup related
        /// options, if any.
        /// </param>
        /// <param name="objectOptionType">
        /// The object option type that identifies the sub-command being
        /// processed.
        /// </param>
        /// <param name="defaultObjectValueFlags">
        /// The default object value flags to use when none is present in the
        /// options, or null to use the built-in default.
        /// </param>
        /// <param name="defaultMemberValueFlags">
        /// The default member value flags to use when none is present in the
        /// options, or null to use the built-in default.
        /// </param>
        /// <param name="defaultMemberTypes">
        /// The default member types to use when none is present in the options,
        /// or null to use the built-in default.
        /// </param>
        /// <param name="defaultBindingFlags">
        /// The default binding flags to use when none is present in the
        /// options, or null to use the built-in default.
        /// </param>
        /// <param name="defaultMarshalFlags">
        /// The default marshal flags to use when none is present in the
        /// options, or null to use the built-in default.
        /// </param>
        /// <param name="defaultReorderFlags">
        /// The default reorder flags to use when none is present in the
        /// options, or null to use the built-in default.
        /// </param>
        /// <param name="defaultByRefArgumentFlags">
        /// The default by-reference argument flags to use when none is present
        /// in the options, or null to use the built-in default.
        /// </param>
        /// <param name="objectType">
        /// Upon return, receives the resolved object type, if any.
        /// </param>
        /// <param name="proxyType">
        /// Upon return, receives the resolved proxy type, if any.
        /// </param>
        /// <param name="objectTypes">
        /// Upon return, receives the resolved list of object types, if any.
        /// </param>
        /// <param name="methodTypes">
        /// Upon return, receives the resolved list of method (signature) types,
        /// if any.
        /// </param>
        /// <param name="parameterTypes">
        /// Upon return, receives the resolved list of parameter types, if any.
        /// </param>
        /// <param name="parameterMarshalFlags">
        /// Upon return, receives the resolved list of per-parameter marshal
        /// flags, if any.
        /// </param>
        /// <param name="objectValueFlags">
        /// Upon return, receives the resolved object value flags.
        /// </param>
        /// <param name="memberValueFlags">
        /// Upon return, receives the resolved member value flags.
        /// </param>
        /// <param name="memberTypes">
        /// Upon return, receives the resolved member types.
        /// </param>
        /// <param name="bindingFlags">
        /// Upon return, receives the resolved binding flags.
        /// </param>
        /// <param name="marshalFlags">
        /// Upon return, receives the resolved marshal flags.
        /// </param>
        /// <param name="reorderFlags">
        /// Upon return, receives the resolved reorder flags.
        /// </param>
        /// <param name="byRefArgumentFlags">
        /// Upon return, receives the resolved by-reference argument flags.
        /// </param>
        /// <param name="limit">
        /// Upon return, receives the resolved overload-matching limit.
        /// </param>
        /// <param name="index">
        /// Upon return, receives the resolved argument index.
        /// </param>
        /// <param name="noByRef">
        /// Upon return, receives non-zero if by-reference argument handling is
        /// disabled.
        /// </param>
        /// <param name="verbose">
        /// Upon return, receives non-zero if verbose output is enabled.
        /// </param>
        /// <param name="strictType">
        /// Upon return, receives non-zero if strict type matching is enabled.
        /// </param>
        /// <param name="strictMember">
        /// Upon return, receives non-zero if strict member matching is enabled.
        /// </param>
        /// <param name="strictArgs">
        /// Upon return, receives non-zero if strict argument matching is
        /// enabled.
        /// </param>
        /// <param name="identity">
        /// Upon return, receives non-zero if identity handling is requested.
        /// </param>
        /// <param name="typeIdentity">
        /// Upon return, receives non-zero if type-identity handling is
        /// requested.
        /// </param>
        /// <param name="noNestedObject">
        /// Upon return, receives non-zero if nested object resolution is
        /// disabled.
        /// </param>
        /// <param name="noNestedMember">
        /// Upon return, receives non-zero if nested member resolution is
        /// disabled.
        /// </param>
        /// <param name="noCase">
        /// Upon return, receives non-zero if case-insensitive matching is
        /// enabled.
        /// </param>
        /// <param name="invoke">
        /// Upon return, receives non-zero if the matched member should be
        /// invoked.
        /// </param>
        /// <param name="help">
        /// Upon return, receives non-zero if the matched member should be
        /// looked up in the help file instead of being invoked.
        /// </param>
        /// <param name="noArgs">
        /// Upon return, receives non-zero if argument processing is disabled.
        /// </param>
        /// <param name="arrayAsValue">
        /// Upon return, receives non-zero if arrays should be treated as
        /// values.
        /// </param>
        /// <param name="arrayAsLink">
        /// Upon return, receives non-zero if arrays should be treated as
        /// linked variables.
        /// </param>
        /// <param name="noMutateBindingFlags">
        /// Upon return, receives non-zero if automatic mutation of the binding
        /// flags is disabled.
        /// </param>
        /// <param name="debug">
        /// Upon return, receives non-zero if debug output is enabled.
        /// </param>
        /// <param name="trace">
        /// Upon return, receives non-zero if trace output is enabled.
        /// </param>
        public static void ProcessFindMethodsAndFixupArgumentsOptions(
            Interpreter interpreter,
            OptionDictionary options,
            ObjectOptionType objectOptionType,
            ValueFlags? defaultObjectValueFlags,
            ValueFlags? defaultMemberValueFlags,
            MemberTypes? defaultMemberTypes,
            BindingFlags? defaultBindingFlags,
            MarshalFlags? defaultMarshalFlags,
            ReorderFlags? defaultReorderFlags,
            ByRefArgumentFlags? defaultByRefArgumentFlags,
            out Type objectType,
            out Type proxyType,
            out TypeList objectTypes,
            out TypeList methodTypes,
            out TypeList parameterTypes,
            out MarshalFlagsList parameterMarshalFlags,
            out ValueFlags objectValueFlags,
            out ValueFlags memberValueFlags,
            out MemberTypes memberTypes,
            out BindingFlags bindingFlags,
            out MarshalFlags marshalFlags,
            out ReorderFlags reorderFlags,
            out ByRefArgumentFlags byRefArgumentFlags,
            out int limit,
            out int index,
            out bool noByRef,
            out bool verbose,
            out bool strictType,
            out bool strictMember,
            out bool strictArgs,
            out bool identity,
            out bool typeIdentity,
            out bool noNestedObject,
            out bool noNestedMember,
            out bool noCase,
            out bool invoke,
            out bool help,
            out bool noArgs,
            out bool arrayAsValue,
            out bool arrayAsLink,
            out bool noMutateBindingFlags,
            out bool debug,
            out bool trace
            )
        {
            IVariant value = null; /* REUSED */

            ///////////////////////////////////////////////////////////////////

            noByRef = false;

            if ((options != null) && options.CheckPresent("-nobyref"))
                noByRef = true;

            ///////////////////////////////////////////////////////////////////

            verbose = false;

            if ((options != null) && options.CheckPresent("-verbose"))
                verbose = true;

            ///////////////////////////////////////////////////////////////////

            strictType = false;

            if ((options != null) && options.CheckPresent("-stricttype"))
                strictType = true;

            ///////////////////////////////////////////////////////////////////

            strictMember = false;

            if ((options != null) && options.CheckPresent("-strictmember"))
                strictMember = true;

            ///////////////////////////////////////////////////////////////////

            strictArgs = false;

            if ((options != null) && options.CheckPresent("-strictargs"))
                strictArgs = true;

            ///////////////////////////////////////////////////////////////////

            identity = false;

            if ((options != null) && options.CheckPresent("-identity"))
                identity = true;

            ///////////////////////////////////////////////////////////////////

            typeIdentity = false;

            if ((options != null) && options.CheckPresent("-typeidentity"))
                typeIdentity = true;

            ///////////////////////////////////////////////////////////////////

            noNestedObject = false;

            if ((options != null) && options.CheckPresent("-nonestedobject"))
                noNestedObject = true;

            ///////////////////////////////////////////////////////////////////

            noNestedMember = false;

            if ((options != null) && options.CheckPresent("-nonestedmember"))
                noNestedMember = true;

            ///////////////////////////////////////////////////////////////////

            //
            // NOTE: Now check for and use the -nocase value.  It is also
            //       important to note here that a specifying the binding
            //       flags does not override this setting.
            //
            noCase = false;

            if ((options != null) && options.CheckPresent("-nocase"))
                noCase = true;

            ///////////////////////////////////////////////////////////////////

            invoke = true;

            if ((options != null) && options.CheckPresent("-noinvoke"))
                invoke = false;

            ///////////////////////////////////////////////////////////////////

            help = false;

            if ((options != null) && options.CheckPresent("-help"))
                help = true;

            ///////////////////////////////////////////////////////////////////

            noArgs = false;

            if ((options != null) && options.CheckPresent("-noargs"))
                noArgs = true;

            ///////////////////////////////////////////////////////////////////

            arrayAsValue = false;

            if ((options != null) && options.CheckPresent("-arrayasvalue"))
                arrayAsValue = true;

            ///////////////////////////////////////////////////////////////////

            arrayAsLink = false;

            if ((options != null) && options.CheckPresent("-arrayaslink"))
                arrayAsLink = true;

            ///////////////////////////////////////////////////////////////////

            noMutateBindingFlags = false;

            if ((options != null) &&
                options.CheckPresent("-nomutatebindingflags"))
            {
                noMutateBindingFlags = true;
            }

            ///////////////////////////////////////////////////////////////////

            debug = false;

            if ((options != null) && options.CheckPresent("-debug"))
                debug = true;

            ///////////////////////////////////////////////////////////////////

            trace = false;

            if ((options != null) && options.CheckPresent("-trace"))
                trace = true;

            ///////////////////////////////////////////////////////////////////

            objectValueFlags = (defaultObjectValueFlags != null) ?
                (ValueFlags)defaultObjectValueFlags : DefaultObjectValueFlags;

            if ((options != null) &&
                options.CheckPresent("-objectvalueflags", ref value))
            {
                objectValueFlags = (ValueFlags)value.Value;
            }

            ///////////////////////////////////////////////////////////////////

            memberValueFlags = (defaultMemberValueFlags != null) ?
                (ValueFlags)defaultMemberValueFlags : DefaultMemberValueFlags;

            if ((options != null) &&
                options.CheckPresent("-membervalueflags", ref value))
            {
                memberValueFlags = (ValueFlags)value.Value;
            }

            ///////////////////////////////////////////////////////////////////

            ProcessReflectionOptions(
                options, objectOptionType, defaultMemberTypes,
                defaultBindingFlags, out memberTypes, out bindingFlags);

            //
            // NOTE: Now check for and use the -nocase value.  It is also
            //       important to note here that a specifying the binding
            //       flags does not override this setting.
            //
            if (noCase)
                bindingFlags |= BindingFlags.IgnoreCase;

            //
            // NOTE: Now check for and use the -identity and -typeidentity
            //       values.  It is also important to note here that a
            //       specifying the binding flags does not override this
            //       setting.
            //
            if (identity || typeIdentity)
            {
                //
                // NOTE: These flags are needed because of the precise
                //       signature of the "HandleOps.Identity" method.
                //
                bindingFlags |= GetBindingFlags(
                    MetaBindingFlags.PublicStaticMethod, true);
            }

            ///////////////////////////////////////////////////////////////////

            //
            // NOTE: For "safe" interpreters, prevent the use of "unsafe"
            //       member types and binding flags (e.g. NonPublic, etc).
            //
            if ((interpreter != null) && interpreter.InternalIsSafe())
            {
                //
                // BUGBUG: At this point, we cannot easily fail as
                //         this method does not have an easy way to
                //         communicate a failure to its caller; so,
                //         since we know this method is only called
                //         from the script commands themselves, just
                //         throw an exception that will be caught by
                //         the script engine.
                //
                Result error = null;

                if (!MaskUnsafeMemberTypesAndBindingFlags(
                        ref memberTypes, ref bindingFlags, ref error))
                {
                    throw new ScriptException(ReturnCode.Error, error);
                }
            }

            ///////////////////////////////////////////////////////////////////

            marshalFlags = (defaultMarshalFlags != null) ?
                (MarshalFlags)defaultMarshalFlags : DefaultMarshalFlags;

            if (options != null)
            {
                if (options.CheckPresent("-marshalflags", ref value))
                    marshalFlags = (MarshalFlags)value.Value;

                if (options.CheckPresent("-default"))
                    marshalFlags |= MarshalFlags.DefaultValue;
            }

            if (noByRef)
                marshalFlags |= MarshalFlags.NoByRefArguments;

            if (verbose)
                marshalFlags |= MarshalFlags.Verbose;

            if (arrayAsValue)
                marshalFlags |= MarshalFlags.SkipNullSetupValue;

            ///////////////////////////////////////////////////////////////////

            reorderFlags = (defaultReorderFlags != null) ?
                (ReorderFlags)defaultReorderFlags : DefaultReorderFlags;

            if ((options != null) &&
                options.CheckPresent("-reorderflags", ref value))
            {
                reorderFlags = (ReorderFlags)value.Value;
            }

            ///////////////////////////////////////////////////////////////////

            byRefArgumentFlags = (defaultByRefArgumentFlags != null) ?
                (ByRefArgumentFlags)defaultByRefArgumentFlags :
                DefaultByRefArgumentFlags;

            if ((options != null) &&
                options.CheckPresent("-argumentflags", ref value))
            {
                byRefArgumentFlags = (ByRefArgumentFlags)value.Value;
            }

            ///////////////////////////////////////////////////////////////////

            objectType = null;

            if (options != null)
            {
                //
                // NOTE: For example, [object invoke]...
                //
                if (options.CheckPresent("-objecttype", ref value) ||
                    options.CheckPresent("-type", ref value))
                {
                    objectType = (Type)value.Value;
                }
            }

            ///////////////////////////////////////////////////////////////////

            proxyType = null;

            if ((options != null) &&
                options.CheckPresent("-proxytype", ref value))
            {
                proxyType = (Type)value.Value;
            }

            ///////////////////////////////////////////////////////////////////

            objectTypes = null;

            if ((options != null) &&
                options.CheckPresent("-objecttypes", ref value))
            {
                objectTypes = (TypeList)value.Value;
            }

            ///////////////////////////////////////////////////////////////////

            methodTypes = null;

            if ((options != null) &&
                options.CheckPresent("-methodtypes", ref value))
            {
                methodTypes = (TypeList)value.Value;
            }

            ///////////////////////////////////////////////////////////////////

            parameterTypes = null;

            if ((options != null) &&
                options.CheckPresent("-parametertypes", ref value))
            {
                parameterTypes = (TypeList)value.Value;
            }

            ///////////////////////////////////////////////////////////////////

            parameterMarshalFlags = null;

            if ((options != null) &&
                options.CheckPresent("-parametermarshalflags", ref value))
            {
                parameterMarshalFlags = MarshalOps.GetParameterMarshalFlags(
                    (EnumList)value.Value);
            }

            ///////////////////////////////////////////////////////////////////

            limit = invoke ? 1 : 0;

            if ((options != null) && options.CheckPresent("-limit", ref value))
                limit = (int)value.Value;

            ///////////////////////////////////////////////////////////////////

            index = Index.Invalid;

            if ((options != null) && options.CheckPresent("-index", ref value))
                index = (int)value.Value;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method extracts the options used when fixing up the return
        /// value of an object sub-command, falling back to the specified
        /// defaults where necessary.  This convenience overload omits several
        /// of the more detailed outputs and forwards to the most general
        /// overload.
        /// </summary>
        /// <param name="options">
        /// The option dictionary that may contain the return-value related
        /// options, if any.
        /// </param>
        /// <param name="defaultObjectFlags">
        /// The default object flags to use when none is present in the options,
        /// or null to use the built-in default.
        /// </param>
        /// <param name="objectFlags">
        /// Upon return, receives the resolved object flags.
        /// </param>
        /// <param name="objectName">
        /// Upon return, receives the resolved object name, if any.
        /// </param>
        /// <param name="interpName">
        /// Upon return, receives the resolved (Tcl) interpreter name, if any.
        /// </param>
        /// <param name="alias">
        /// Upon return, receives non-zero if an alias should be created.
        /// </param>
        /// <param name="aliasRaw">
        /// Upon return, receives non-zero if a raw alias should be created.
        /// </param>
        /// <param name="aliasAll">
        /// Upon return, receives non-zero if an all-encompassing alias should
        /// be created.
        /// </param>
        /// <param name="aliasReference">
        /// Upon return, receives non-zero if the alias should add an object
        /// reference.
        /// </param>
        public static void ProcessFixupReturnValueOptions(
            OptionDictionary options,
            ObjectFlags? defaultObjectFlags,
            out ObjectFlags objectFlags,
            out string objectName,
            out string interpName,
            out bool alias,
            out bool aliasRaw,
            out bool aliasAll,
            out bool aliasReference
            )
        {
            ObjectFlags byRefObjectFlags;
            Type returnType;
            bool create;
            bool dispose;
            bool toString;

            ProcessFixupReturnValueOptions(
                options, defaultObjectFlags, null, out returnType,
                out objectFlags, out byRefObjectFlags, out objectName,
                out interpName, out create, out dispose, out alias,
                out aliasRaw, out aliasAll, out aliasReference,
                out toString);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method extracts the options used when fixing up the return
        /// value of an object sub-command, falling back to the specified
        /// defaults where necessary.  This convenience overload omits the
        /// by-reference object flags output and forwards to the most general
        /// overload.
        /// </summary>
        /// <param name="options">
        /// The option dictionary that may contain the return-value related
        /// options, if any.
        /// </param>
        /// <param name="defaultObjectFlags">
        /// The default object flags to use when none is present in the options,
        /// or null to use the built-in default.
        /// </param>
        /// <param name="returnType">
        /// Upon return, receives the resolved return type, if any.
        /// </param>
        /// <param name="objectFlags">
        /// Upon return, receives the resolved object flags.
        /// </param>
        /// <param name="objectName">
        /// Upon return, receives the resolved object name, if any.
        /// </param>
        /// <param name="interpName">
        /// Upon return, receives the resolved (Tcl) interpreter name, if any.
        /// </param>
        /// <param name="create">
        /// Upon return, receives non-zero if an opaque object handle should be
        /// created.
        /// </param>
        /// <param name="dispose">
        /// Upon return, receives non-zero if the object may be disposed.
        /// </param>
        /// <param name="alias">
        /// Upon return, receives non-zero if an alias should be created.
        /// </param>
        /// <param name="aliasRaw">
        /// Upon return, receives non-zero if a raw alias should be created.
        /// </param>
        /// <param name="aliasAll">
        /// Upon return, receives non-zero if an all-encompassing alias should
        /// be created.
        /// </param>
        /// <param name="aliasReference">
        /// Upon return, receives non-zero if the alias should add an object
        /// reference.
        /// </param>
        /// <param name="toString">
        /// Upon return, receives non-zero if the value should be converted via
        /// its string representation.
        /// </param>
        public static void ProcessFixupReturnValueOptions(
            OptionDictionary options,
            ObjectFlags? defaultObjectFlags,
            out Type returnType,
            out ObjectFlags objectFlags,
            out string objectName,
            out string interpName,
            out bool create,
            out bool dispose,
            out bool alias,
            out bool aliasRaw,
            out bool aliasAll,
            out bool aliasReference,
            out bool toString
            )
        {
            ObjectFlags byRefObjectFlags;

            ProcessFixupReturnValueOptions(
                options, defaultObjectFlags, null, out returnType,
                out objectFlags, out byRefObjectFlags, out objectName,
                out interpName, out create, out dispose, out alias,
                out aliasRaw, out aliasAll, out aliasReference,
                out toString);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method extracts the complete set of options used when fixing up
        /// the return value of an object sub-command, falling back to the
        /// specified defaults where necessary.  This is the most general
        /// overload to which the others forward.
        /// </summary>
        /// <param name="options">
        /// The option dictionary that may contain the return-value related
        /// options, if any.
        /// </param>
        /// <param name="defaultObjectFlags">
        /// The default object flags to use when none is present in the options,
        /// or null to use the built-in default.
        /// </param>
        /// <param name="defaultByRefObjectFlags">
        /// The default by-reference object flags to use when none is present in
        /// the options, or null to use the built-in default.
        /// </param>
        /// <param name="returnType">
        /// Upon return, receives the resolved return type, if any.
        /// </param>
        /// <param name="objectFlags">
        /// Upon return, receives the resolved object flags.
        /// </param>
        /// <param name="byRefObjectFlags">
        /// Upon return, receives the resolved by-reference object flags.
        /// </param>
        /// <param name="objectName">
        /// Upon return, receives the resolved object name, if any.
        /// </param>
        /// <param name="interpName">
        /// Upon return, receives the resolved (Tcl) interpreter name, if any.
        /// </param>
        /// <param name="create">
        /// Upon return, receives non-zero if an opaque object handle should be
        /// created.
        /// </param>
        /// <param name="dispose">
        /// Upon return, receives non-zero if the object may be disposed.
        /// </param>
        /// <param name="alias">
        /// Upon return, receives non-zero if an alias should be created.
        /// </param>
        /// <param name="aliasRaw">
        /// Upon return, receives non-zero if a raw alias should be created.
        /// </param>
        /// <param name="aliasAll">
        /// Upon return, receives non-zero if an all-encompassing alias should
        /// be created.
        /// </param>
        /// <param name="aliasReference">
        /// Upon return, receives non-zero if the alias should add an object
        /// reference.
        /// </param>
        /// <param name="toString">
        /// Upon return, receives non-zero if the value should be converted via
        /// its string representation.
        /// </param>
        public static void ProcessFixupReturnValueOptions(
            OptionDictionary options,
            ObjectFlags? defaultObjectFlags,
            ObjectFlags? defaultByRefObjectFlags,
            out Type returnType,
            out ObjectFlags objectFlags,
            out ObjectFlags byRefObjectFlags,
            out string objectName,
            out string interpName,
            out bool create,
            out bool dispose,
            out bool alias,
            out bool aliasRaw,
            out bool aliasAll,
            out bool aliasReference,
            out bool toString
            )
        {
            IVariant value = null; /* REUSED */

            ///////////////////////////////////////////////////////////////////

            returnType = null;

            if (options != null)
            {
                //
                // NOTE: For example, [sql execute]...
                //
                if (options.Has("-objecttype") &&
                    options.CheckPresent("-objecttype", ref value))
                {
                    returnType = (Type)value.Value;
                    goto returnTypeDone;
                }

                //
                // NOTE: For example, [object invoke]...
                //
                if (options.Has("-returntype") &&
                    options.CheckPresent("-returntype", ref value))
                {
                    returnType = (Type)value.Value;
                    goto returnTypeDone;
                }

                //
                // NOTE: For example, [debug exception], [callback dequeue],
                //       InvokeDelegate(), etc.
                //
                if (options.CheckPresent("-type", ref value))
                {
                    returnType = (Type)value.Value;
                    goto returnTypeDone;
                }
            }

        returnTypeDone:

            ///////////////////////////////////////////////////////////////////

            objectFlags = (defaultObjectFlags != null) ?
                (ObjectFlags)defaultObjectFlags : DefaultObjectFlags;

            if (options != null)
            {
                if (options.CheckPresent("-objectflags", ref value))
                    objectFlags = (ObjectFlags)value.Value;

                if (options.CheckPresent("-noforcedelete"))
                    objectFlags &= ~ObjectFlags.ForceDelete;
            }

            ///////////////////////////////////////////////////////////////////

            byRefObjectFlags = (defaultByRefObjectFlags != null) ?
                (ObjectFlags)defaultByRefObjectFlags : DefaultByRefObjectFlags;

            if (options != null)
            {
                if (options.CheckPresent("-byrefobjectflags", ref value))
                    byRefObjectFlags = (ObjectFlags)value.Value;
            }

            ///////////////////////////////////////////////////////////////////

            objectName = null;

            if ((options != null) &&
                options.CheckPresent("-objectname", ref value))
            {
                objectName = value.ToString();
            }

            ///////////////////////////////////////////////////////////////////

            interpName = null;

#if NATIVE && TCL
            if ((options != null) && options.CheckPresent("-tcl", ref value))
                interpName = value.ToString();
#endif

            ///////////////////////////////////////////////////////////////////

            if (options != null)
            {
                if (options.Has("-nocreate"))
                {
                    create = DefaultNoCreate;

                    if (options.CheckPresent("-nocreate"))
                        create = false;
                }
                else
                {
                    create = DefaultCreate;

                    if (options.CheckPresent("-create"))
                        create = true;
                }
            }
            else
            {
                create = DefaultCreate;
            }

            ///////////////////////////////////////////////////////////////////

            dispose = true;

            if ((options != null) && options.CheckPresent("-nodispose"))
                dispose = false;

            ///////////////////////////////////////////////////////////////////

            alias = false;

            if ((options != null) && options.CheckPresent("-alias"))
                alias = true;

            ///////////////////////////////////////////////////////////////////

            aliasRaw = false;

            if ((options != null) && options.CheckPresent("-aliasraw"))
                aliasRaw = true;

            ///////////////////////////////////////////////////////////////////

            aliasAll = false;

            if ((options != null) && options.CheckPresent("-aliasall"))
                aliasAll = true;

            ///////////////////////////////////////////////////////////////////

            aliasReference = false;

            if ((options != null) && options.CheckPresent("-aliasreference"))
                aliasReference = true;

            ///////////////////////////////////////////////////////////////////

            toString = false;

            if ((options != null) && options.CheckPresent("-tostring"))
                toString = true;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method extracts the options used by the type-resolution
        /// sub-commands.  This convenience overload exposes only the boolean
        /// outputs and forwards to the most general overload.
        /// </summary>
        /// <param name="options">
        /// The option dictionary that may contain the type-resolution related
        /// options, if any.
        /// </param>
        /// <param name="verbose">
        /// Upon return, receives non-zero if verbose output is enabled.
        /// </param>
        /// <param name="strictType">
        /// Upon return, receives non-zero if strict type matching is enabled.
        /// </param>
        /// <param name="noCase">
        /// Upon return, receives non-zero if case-insensitive matching is
        /// enabled.
        /// </param>
        public static void ProcessGetTypeOptions(
            OptionDictionary options,
            out bool verbose,
            out bool strictType,
            out bool noCase
            )
        {
            TypeList objectTypes;
            ValueFlags objectValueFlags;
            MarshalFlags marshalFlags;

            ProcessGetTypeOptions(
                options, null, null, out objectTypes, out objectValueFlags,
                out marshalFlags, out verbose, out strictType, out noCase);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method extracts the options used by the type-resolution
        /// sub-commands.  This convenience overload omits the case-insensitive
        /// matching output and forwards to the most general overload.
        /// </summary>
        /// <param name="options">
        /// The option dictionary that may contain the type-resolution related
        /// options, if any.
        /// </param>
        /// <param name="objectTypes">
        /// Upon return, receives the resolved list of object types, if any.
        /// </param>
        /// <param name="verbose">
        /// Upon return, receives non-zero if verbose output is enabled.
        /// </param>
        /// <param name="strictType">
        /// Upon return, receives non-zero if strict type matching is enabled.
        /// </param>
        private static void ProcessGetTypeOptions(
            OptionDictionary options,
            out TypeList objectTypes,
            out bool verbose,
            out bool strictType
            )
        {
            ValueFlags objectValueFlags;
            MarshalFlags marshalFlags;
            bool noCase;

            ProcessGetTypeOptions(
                options, null, null, out objectTypes, out objectValueFlags,
                out marshalFlags, out verbose, out strictType, out noCase);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method extracts the options used by the type-resolution
        /// sub-commands.  This convenience overload exposes the object type list
        /// and the boolean outputs and forwards to the most general overload.
        /// </summary>
        /// <param name="options">
        /// The option dictionary that may contain the type-resolution related
        /// options, if any.
        /// </param>
        /// <param name="objectTypes">
        /// Upon return, receives the resolved list of object types, if any.
        /// </param>
        /// <param name="verbose">
        /// Upon return, receives non-zero if verbose output is enabled.
        /// </param>
        /// <param name="strictType">
        /// Upon return, receives non-zero if strict type matching is enabled.
        /// </param>
        /// <param name="noCase">
        /// Upon return, receives non-zero if case-insensitive matching is
        /// enabled.
        /// </param>
        private static void ProcessGetTypeOptions(
            OptionDictionary options,
            out TypeList objectTypes,
            out bool verbose,
            out bool strictType,
            out bool noCase
            )
        {
            ValueFlags objectValueFlags;
            MarshalFlags marshalFlags;

            ProcessGetTypeOptions(
                options, null, null, out objectTypes, out objectValueFlags,
                out marshalFlags, out verbose, out strictType, out noCase);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method extracts the complete set of options used by the
        /// type-resolution sub-commands, falling back to the specified defaults
        /// where necessary.  This is the most general overload to which the
        /// others forward.
        /// </summary>
        /// <param name="options">
        /// The option dictionary that may contain the type-resolution related
        /// options, if any.
        /// </param>
        /// <param name="defaultObjectValueFlags">
        /// The default object value flags to use when none is present in the
        /// options, or null to use the built-in default.
        /// </param>
        /// <param name="defaultMarshalFlags">
        /// The default marshal flags to use when none is present in the
        /// options, or null to use the built-in default.
        /// </param>
        /// <param name="objectTypes">
        /// Upon return, receives the resolved list of object types, if any.
        /// </param>
        /// <param name="objectValueFlags">
        /// Upon return, receives the resolved object value flags.
        /// </param>
        /// <param name="marshalFlags">
        /// Upon return, receives the resolved marshal flags.
        /// </param>
        /// <param name="verbose">
        /// Upon return, receives non-zero if verbose output is enabled.
        /// </param>
        /// <param name="strictType">
        /// Upon return, receives non-zero if strict type matching is enabled.
        /// </param>
        /// <param name="noCase">
        /// Upon return, receives non-zero if case-insensitive matching is
        /// enabled.
        /// </param>
        public static void ProcessGetTypeOptions(
            OptionDictionary options,
            ValueFlags? defaultObjectValueFlags,
            MarshalFlags? defaultMarshalFlags,
            out TypeList objectTypes,
            out ValueFlags objectValueFlags,
            out MarshalFlags marshalFlags,
            out bool verbose,
            out bool strictType,
            out bool noCase
            )
        {
            IVariant value = null; /* REUSED */

            ///////////////////////////////////////////////////////////////////

            objectValueFlags = (defaultObjectValueFlags != null) ?
                (ValueFlags)defaultObjectValueFlags : DefaultObjectValueFlags;

            if ((options != null) &&
                options.CheckPresent("-objectvalueflags", ref value))
            {
                objectValueFlags = (ValueFlags)value.Value;
            }

            ///////////////////////////////////////////////////////////////////

            marshalFlags = (defaultMarshalFlags != null) ?
                (MarshalFlags)defaultMarshalFlags : DefaultMarshalFlags;

            if ((options != null) &&
                options.CheckPresent("-marshalflags", ref value))
            {
                marshalFlags = (MarshalFlags)value.Value;
            }

            ///////////////////////////////////////////////////////////////////

            verbose = false;

            if ((options != null) && options.CheckPresent("-verbose"))
                verbose = true;

            ///////////////////////////////////////////////////////////////////

            strictType = false;

            if ((options != null) && options.CheckPresent("-stricttype"))
                strictType = true;

            ///////////////////////////////////////////////////////////////////

            noCase = false;

            if ((options != null) && options.CheckPresent("-nocase"))
                noCase = true;

            ///////////////////////////////////////////////////////////////////

            objectTypes = null;

            if ((options != null) &&
                options.CheckPresent("-objecttypes", ref value))
            {
                objectTypes = (TypeList)value.Value;
            }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method extracts the options that control marshalling for an
        /// object sub-command, falling back to the specified defaults where
        /// necessary.
        /// </summary>
        /// <param name="options">
        /// The option dictionary that may contain the marshalling related
        /// options, if any.
        /// </param>
        /// <param name="objectOptionType">
        /// The object option type that identifies the sub-command being
        /// processed.
        /// </param>
        /// <param name="defaultObjectValueFlags">
        /// The default object value flags to use when none is present in the
        /// options, or null to use the built-in default.
        /// </param>
        /// <param name="defaultMemberValueFlags">
        /// The default member value flags to use when none is present in the
        /// options, or null to use the built-in default.
        /// </param>
        /// <param name="defaultMemberTypes">
        /// The default member types to use when none is present in the options,
        /// or null to use the built-in default.
        /// </param>
        /// <param name="defaultBindingFlags">
        /// The default binding flags to use when none is present in the
        /// options, or null to use the built-in default.
        /// </param>
        /// <param name="defaultMarshalFlags">
        /// The default marshal flags to use when none is present in the
        /// options, or null to use the built-in default.
        /// </param>
        /// <param name="defaultReorderFlags">
        /// The default reorder flags to use when none is present in the
        /// options, or null to use the built-in default.
        /// </param>
        /// <param name="defaultByRefArgumentFlags">
        /// The default by-reference argument flags to use when none is present
        /// in the options, or null to use the built-in default.
        /// </param>
        /// <param name="objectType">
        /// Upon return, receives the resolved object type, if any.
        /// </param>
        /// <param name="proxyType">
        /// Upon return, receives the resolved proxy type, if any.
        /// </param>
        /// <param name="objectTypes">
        /// Upon return, receives the resolved list of object types, if any.
        /// </param>
        /// <param name="methodTypes">
        /// Upon return, receives the resolved list of method (signature) types,
        /// if any.
        /// </param>
        /// <param name="parameterTypes">
        /// Upon return, receives the resolved list of parameter types, if any.
        /// </param>
        /// <param name="parameterMarshalFlags">
        /// Upon return, receives the resolved list of per-parameter marshal
        /// flags, if any.
        /// </param>
        /// <param name="objectValueFlags">
        /// Upon return, receives the resolved object value flags.
        /// </param>
        /// <param name="memberValueFlags">
        /// Upon return, receives the resolved member value flags.
        /// </param>
        /// <param name="memberTypes">
        /// Upon return, receives the resolved member types.
        /// </param>
        /// <param name="bindingFlags">
        /// Upon return, receives the resolved binding flags.
        /// </param>
        /// <param name="marshalFlags">
        /// Upon return, receives the resolved marshal flags.
        /// </param>
        /// <param name="reorderFlags">
        /// Upon return, receives the resolved reorder flags.
        /// </param>
        /// <param name="byRefArgumentFlags">
        /// Upon return, receives the resolved by-reference argument flags.
        /// </param>
        /// <param name="noByRef">
        /// Upon return, receives non-zero if by-reference argument handling is
        /// disabled.
        /// </param>
        /// <param name="verbose">
        /// Upon return, receives non-zero if verbose output is enabled.
        /// </param>
        /// <param name="strictType">
        /// Upon return, receives non-zero if strict type matching is enabled.
        /// </param>
        /// <param name="strictArgs">
        /// Upon return, receives non-zero if strict argument matching is
        /// enabled.
        /// </param>
        /// <param name="noNestedObject">
        /// Upon return, receives non-zero if nested object resolution is
        /// disabled.
        /// </param>
        /// <param name="noCase">
        /// Upon return, receives non-zero if case-insensitive matching is
        /// enabled.
        /// </param>
        /// <param name="invoke">
        /// Upon return, receives non-zero if the matched member should be
        /// invoked.
        /// </param>
        /// <param name="help">
        /// Upon return, receives non-zero if the matched member should be
        /// looked up in the help file instead of being invoked.
        /// </param>
        /// <param name="noArgs">
        /// Upon return, receives non-zero if argument processing is disabled.
        /// </param>
        /// <param name="arrayAsValue">
        /// Upon return, receives non-zero if arrays should be treated as
        /// values.
        /// </param>
        /// <param name="arrayAsLink">
        /// Upon return, receives non-zero if arrays should be treated as
        /// linked variables.
        /// </param>
        /// <param name="trace">
        /// Upon return, receives non-zero if trace output is enabled.
        /// </param>
        private static void ProcessMarshalOptions(
            OptionDictionary options,
            ObjectOptionType objectOptionType,
            ValueFlags? defaultObjectValueFlags,
            ValueFlags? defaultMemberValueFlags,
            MemberTypes? defaultMemberTypes,
            BindingFlags? defaultBindingFlags,
            MarshalFlags? defaultMarshalFlags,
            ReorderFlags? defaultReorderFlags,
            ByRefArgumentFlags? defaultByRefArgumentFlags,
            out Type objectType,
            out Type proxyType,
            out TypeList objectTypes,
            out TypeList methodTypes,
            out TypeList parameterTypes,
            out MarshalFlagsList parameterMarshalFlags,
            out ValueFlags objectValueFlags,
            out ValueFlags memberValueFlags,
            out MemberTypes memberTypes,
            out BindingFlags bindingFlags,
            out MarshalFlags marshalFlags,
            out ReorderFlags reorderFlags,
            out ByRefArgumentFlags byRefArgumentFlags,
            out bool noByRef,
            out bool verbose,
            out bool strictType,
            out bool strictArgs,
            out bool noNestedObject,
            out bool noCase,
            out bool invoke,
            out bool help,
            out bool noArgs,
            out bool arrayAsValue,
            out bool arrayAsLink,
            out bool trace
            )
        {
            IVariant value = null; /* REUSED */

            ///////////////////////////////////////////////////////////////////

            noByRef = false;

            if ((options != null) && options.CheckPresent("-nobyref"))
                noByRef = true;

            ///////////////////////////////////////////////////////////////////

            verbose = false;

            if ((options != null) && options.CheckPresent("-verbose"))
                verbose = true;

            ///////////////////////////////////////////////////////////////////

            strictType = false;

            if ((options != null) && options.CheckPresent("-stricttype"))
                strictType = true;

            ///////////////////////////////////////////////////////////////////

            strictArgs = false;

            if ((options != null) && options.CheckPresent("-strictargs"))
                strictArgs = true;

            ///////////////////////////////////////////////////////////////////

            noNestedObject = false;

            if ((options != null) && options.CheckPresent("-nonestedobject"))
                noNestedObject = true;

            ///////////////////////////////////////////////////////////////////

            noCase = false;

            if ((options != null) && options.CheckPresent("-nocase"))
                noCase = true;

            ///////////////////////////////////////////////////////////////////

            invoke = true;

            if ((options != null) && options.CheckPresent("-noinvoke"))
                invoke = false;

            ///////////////////////////////////////////////////////////////////

            help = false;

            if ((options != null) && options.CheckPresent("-help"))
                help = true;

            ///////////////////////////////////////////////////////////////////

            noArgs = false;

            if ((options != null) && options.CheckPresent("-noargs"))
                noArgs = true;

            ///////////////////////////////////////////////////////////////////

            arrayAsValue = false;

            if ((options != null) && options.CheckPresent("-arrayasvalue"))
                arrayAsValue = true;

            ///////////////////////////////////////////////////////////////////

            arrayAsLink = false;

            if ((options != null) && options.CheckPresent("-arrayaslink"))
                arrayAsLink = true;

            ///////////////////////////////////////////////////////////////////

            trace = false;

            if ((options != null) && options.CheckPresent("-trace"))
                trace = true;

            ///////////////////////////////////////////////////////////////////

            objectValueFlags = (defaultObjectValueFlags != null) ?
                (ValueFlags)defaultObjectValueFlags : DefaultObjectValueFlags;

            if ((options != null) &&
                options.CheckPresent("-objectvalueflags", ref value))
            {
                objectValueFlags = (ValueFlags)value.Value;
            }

            ///////////////////////////////////////////////////////////////////

            memberValueFlags = (defaultMemberValueFlags != null) ?
                (ValueFlags)defaultMemberValueFlags : DefaultMemberValueFlags;

            if ((options != null) &&
                options.CheckPresent("-membervalueflags", ref value))
            {
                memberValueFlags = (ValueFlags)value.Value;
            }

            ///////////////////////////////////////////////////////////////////

            ProcessReflectionOptions(
                options, objectOptionType, defaultMemberTypes,
                defaultBindingFlags, out memberTypes, out bindingFlags);

            //
            // NOTE: Now check for and use the -nocase value.  It is also
            //       important to note here that a specifying the binding
            //       flags does not override this setting.
            //
            if (noCase)
                bindingFlags |= BindingFlags.IgnoreCase;

            ///////////////////////////////////////////////////////////////////

            byRefArgumentFlags = (defaultByRefArgumentFlags != null) ?
                (ByRefArgumentFlags)defaultByRefArgumentFlags :
                DefaultByRefArgumentFlags;

            if ((options != null) &&
                options.CheckPresent("-argumentflags", ref value))
            {
                byRefArgumentFlags = (ByRefArgumentFlags)value.Value;
            }

            ///////////////////////////////////////////////////////////////////

            reorderFlags = (defaultReorderFlags != null) ?
                (ReorderFlags)defaultReorderFlags : DefaultReorderFlags;

            if ((options != null) &&
                options.CheckPresent("-reorderflags", ref value))
            {
                reorderFlags = (ReorderFlags)value.Value;
            }

            ///////////////////////////////////////////////////////////////////

            marshalFlags = (defaultMarshalFlags != null) ?
                (MarshalFlags)defaultMarshalFlags : DefaultMarshalFlags;

            if (options != null)
            {
                if (options.CheckPresent("-marshalflags", ref value))
                    marshalFlags = (MarshalFlags)value.Value;

                if (options.CheckPresent("-default"))
                    marshalFlags |= MarshalFlags.DefaultValue;
            }

            if (noByRef)
                marshalFlags |= MarshalFlags.NoByRefArguments;

            if (verbose)
                marshalFlags |= MarshalFlags.Verbose;

            if (arrayAsValue)
                marshalFlags |= MarshalFlags.SkipNullSetupValue;

            ///////////////////////////////////////////////////////////////////

            objectType = null;

            if (options != null)
            {
                //
                // NOTE: For example, [object invokeraw]...
                //
                if (options.CheckPresent("-objecttype", ref value) ||
                    options.CheckPresent("-type", ref value))
                {
                    objectType = (Type)value.Value;
                }
            }

            ///////////////////////////////////////////////////////////////////

            proxyType = null;

            if ((options != null) &&
                options.CheckPresent("-proxytype", ref value))
            {
                proxyType = (Type)value.Value;
            }

            ///////////////////////////////////////////////////////////////////

            objectTypes = null;

            if ((options != null) &&
                options.CheckPresent("-objecttypes", ref value))
            {
                objectTypes = (TypeList)value.Value;
            }

            ///////////////////////////////////////////////////////////////////

            methodTypes = null;

            if ((options != null) &&
                options.CheckPresent("-methodtypes", ref value))
            {
                methodTypes = (TypeList)value.Value;
            }

            ///////////////////////////////////////////////////////////////////

            parameterTypes = null;

            if ((options != null) &&
                options.CheckPresent("-parametertypes", ref value))
            {
                parameterTypes = (TypeList)value.Value;
            }

            ///////////////////////////////////////////////////////////////////

            parameterMarshalFlags = null;

            if ((options != null) &&
                options.CheckPresent("-parametermarshalflags", ref value))
            {
                parameterMarshalFlags = MarshalOps.GetParameterMarshalFlags(
                    (EnumList)value.Value);
            }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method extracts the options used by the [object alias]
        /// sub-command.
        /// </summary>
        /// <param name="options">
        /// The option dictionary that may contain the alias related options, if
        /// any.
        /// </param>
        /// <param name="objectTypes">
        /// Upon return, receives the resolved list of object types, if any.
        /// </param>
        /// <param name="aliasName">
        /// Upon return, receives the resolved alias name, if any.
        /// </param>
        /// <param name="verbose">
        /// Upon return, receives non-zero if verbose output is enabled.
        /// </param>
        /// <param name="strictType">
        /// Upon return, receives non-zero if strict type matching is enabled.
        /// </param>
        /// <param name="noCase">
        /// Upon return, receives non-zero if case-insensitive matching is
        /// enabled.
        /// </param>
        /// <param name="aliasRaw">
        /// Upon return, receives non-zero if a raw alias should be created.
        /// </param>
        /// <param name="aliasAll">
        /// Upon return, receives non-zero if an all-encompassing alias should
        /// be created.
        /// </param>
        /// <param name="aliasReference">
        /// Upon return, receives non-zero if the alias should add an object
        /// reference.
        /// </param>
        public static void ProcessObjectAliasOptions(
            OptionDictionary options,
            out TypeList objectTypes,
            out string aliasName,
            out bool verbose,
            out bool strictType,
            out bool noCase,
            out bool aliasRaw,
            out bool aliasAll,
            out bool aliasReference
            )
        {
            IVariant value = null; /* REUSED */

            ///////////////////////////////////////////////////////////////////

            ProcessGetTypeOptions(
                options, out objectTypes, out verbose, out strictType,
                out noCase);

            ///////////////////////////////////////////////////////////////////

            aliasName = null;

            if ((options != null) &&
                options.CheckPresent("-aliasname", ref value))
            {
                aliasName = value.ToString();
            }

            ///////////////////////////////////////////////////////////////////

            aliasRaw = false;

            if ((options != null) && options.CheckPresent("-aliasraw"))
                aliasRaw = true;

            ///////////////////////////////////////////////////////////////////

            aliasAll = false;

            if ((options != null) && options.CheckPresent("-aliasall"))
                aliasAll = true;

            ///////////////////////////////////////////////////////////////////

            aliasReference = false;

            if ((options != null) && options.CheckPresent("-aliasreference"))
                aliasReference = true;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method extracts the X.509 certificate verification options used
        /// by the [object certificate] sub-command, falling back to the
        /// specified defaults where necessary.  This convenience overload omits
        /// the cache output and forwards to the most general overload.
        /// </summary>
        /// <param name="options">
        /// The option dictionary that may contain the certificate related
        /// options, if any.
        /// </param>
        /// <param name="defaultX509VerificationFlags">
        /// The default X.509 verification flags to use when none is present in
        /// the options, or null to use the queried default.
        /// </param>
        /// <param name="defaultX509RevocationMode">
        /// The default X.509 revocation mode to use when none is present in the
        /// options, or null to use the queried default.
        /// </param>
        /// <param name="defaultX509RevocationFlag">
        /// The default X.509 revocation flag to use when none is present in the
        /// options, or null to use the queried default.
        /// </param>
        /// <param name="x509VerificationFlags">
        /// Upon return, receives the resolved X.509 verification flags.
        /// </param>
        /// <param name="x509RevocationMode">
        /// Upon return, receives the resolved X.509 revocation mode.
        /// </param>
        /// <param name="x509RevocationFlag">
        /// Upon return, receives the resolved X.509 revocation flag.
        /// </param>
        /// <param name="chain">
        /// Upon return, receives non-zero if certificate chain building is
        /// requested.
        /// </param>
        public static void ProcessObjectCertificateOptions(
            OptionDictionary options,
            X509VerificationFlags? defaultX509VerificationFlags,
            X509RevocationMode? defaultX509RevocationMode,
            X509RevocationFlag? defaultX509RevocationFlag,
            out X509VerificationFlags x509VerificationFlags,
            out X509RevocationMode x509RevocationMode,
            out X509RevocationFlag x509RevocationFlag,
            out bool chain
            )
        {
            bool cache;

            ProcessObjectCertificateOptions(
                options, defaultX509VerificationFlags,
                defaultX509RevocationMode, defaultX509RevocationFlag,
                out x509VerificationFlags, out x509RevocationMode,
                out x509RevocationFlag, out chain, out cache);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method extracts the complete set of X.509 certificate
        /// verification options used by the [object certificate] sub-command,
        /// falling back to the specified defaults where necessary.  This is the
        /// most general overload to which the other forwards.
        /// </summary>
        /// <param name="options">
        /// The option dictionary that may contain the certificate related
        /// options, if any.
        /// </param>
        /// <param name="defaultX509VerificationFlags">
        /// The default X.509 verification flags to use when none is present in
        /// the options, or null to use the queried default.
        /// </param>
        /// <param name="defaultX509RevocationMode">
        /// The default X.509 revocation mode to use when none is present in the
        /// options, or null to use the queried default.
        /// </param>
        /// <param name="defaultX509RevocationFlag">
        /// The default X.509 revocation flag to use when none is present in the
        /// options, or null to use the queried default.
        /// </param>
        /// <param name="x509VerificationFlags">
        /// Upon return, receives the resolved X.509 verification flags.
        /// </param>
        /// <param name="x509RevocationMode">
        /// Upon return, receives the resolved X.509 revocation mode.
        /// </param>
        /// <param name="x509RevocationFlag">
        /// Upon return, receives the resolved X.509 revocation flag.
        /// </param>
        /// <param name="chain">
        /// Upon return, receives non-zero if certificate chain building is
        /// requested.
        /// </param>
        /// <param name="cache">
        /// Upon return, receives non-zero if certificate caching is requested.
        /// </param>
        public static void ProcessObjectCertificateOptions(
            OptionDictionary options,
            X509VerificationFlags? defaultX509VerificationFlags,
            X509RevocationMode? defaultX509RevocationMode,
            X509RevocationFlag? defaultX509RevocationFlag,
            out X509VerificationFlags x509VerificationFlags,
            out X509RevocationMode x509RevocationMode,
            out X509RevocationFlag x509RevocationFlag,
            out bool chain,
            out bool cache
            )
        {
            X509VerificationFlags localX509VerificationFlags;
            X509RevocationMode localX509RevocationMode;
            X509RevocationFlag localX509RevocationFlag;

            CertificateOps.QueryFlags(
                out localX509VerificationFlags,
                out localX509RevocationMode,
                out localX509RevocationFlag);

            ///////////////////////////////////////////////////////////////////

            IVariant value = null; /* REUSED */

            ///////////////////////////////////////////////////////////////////

            x509VerificationFlags = (defaultX509VerificationFlags != null) ?
                (X509VerificationFlags)defaultX509VerificationFlags :
                localX509VerificationFlags;

            if ((options != null) &&
                options.CheckPresent("-verificationflags", ref value))
            {
                x509VerificationFlags = (X509VerificationFlags)value.Value;
            }

            ///////////////////////////////////////////////////////////////////

            x509RevocationMode = (defaultX509RevocationMode != null) ?
                (X509RevocationMode)defaultX509RevocationMode :
                localX509RevocationMode;

            if ((options != null) &&
                options.CheckPresent("-revocationmode", ref value))
            {
                x509RevocationMode = (X509RevocationMode)value.Value;
            }

            ///////////////////////////////////////////////////////////////////

            x509RevocationFlag = (defaultX509RevocationFlag != null) ?
                (X509RevocationFlag)defaultX509RevocationFlag :
                localX509RevocationFlag;

            if ((options != null) &&
                options.CheckPresent("-revocationflag", ref value))
            {
                x509RevocationFlag = (X509RevocationFlag)value.Value;
            }

            ///////////////////////////////////////////////////////////////////

            chain = false;

            if ((options != null) && options.CheckPresent("-chain"))
                chain = true;

            ///////////////////////////////////////////////////////////////////

            cache = false;

            if ((options != null) && options.CheckPresent("-cache"))
                cache = true;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method extracts the options used by the [object declare]
        /// sub-command, falling back to the specified defaults where necessary.
        /// </summary>
        /// <param name="options">
        /// The option dictionary that may contain the declare related options,
        /// if any.
        /// </param>
        /// <param name="defaultMatchMode">
        /// The default match mode to use when none is present in the options,
        /// or null to use the built-in default.
        /// </param>
        /// <param name="matchMode">
        /// Upon return, receives the resolved match mode.
        /// </param>
        /// <param name="pattern">
        /// Upon return, receives the resolved match pattern, if any.
        /// </param>
        /// <param name="verbose">
        /// Upon return, receives non-zero if verbose output is enabled.
        /// </param>
        /// <param name="strictType">
        /// Upon return, receives non-zero if strict type matching is enabled.
        /// </param>
        /// <param name="nonPublic">
        /// Upon return, receives non-zero if non-public members should be
        /// declared.
        /// </param>
        /// <param name="noCase">
        /// Upon return, receives non-zero if case-insensitive matching is
        /// enabled.
        /// </param>
        public static void ProcessObjectDeclareOptions(
            OptionDictionary options,
            MatchMode? defaultMatchMode,
            out MatchMode matchMode,
            out string pattern,
            out bool verbose,
            out bool strictType,
            out bool nonPublic,
            out bool noCase
            )
        {
            TypeList objectTypes;

            ProcessGetTypeOptions(
                options, out objectTypes, out verbose, out strictType);

            ///////////////////////////////////////////////////////////////////

            ProcessPatternMatchingOptions(
                options, "-declaremode", "-declarepattern", "-declarenocase",
                defaultMatchMode, out matchMode, out pattern, out noCase);

            ///////////////////////////////////////////////////////////////////

            nonPublic = false;

            if ((options != null) && options.CheckPresent("-declarenonpublic"))
                nonPublic = true;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method extracts the options used by the [object import]
        /// sub-command, falling back to the specified defaults where necessary.
        /// This convenience overload omits the non-public output and forwards
        /// to the most general overload.
        /// </summary>
        /// <param name="options">
        /// The option dictionary that may contain the import related options,
        /// if any.
        /// </param>
        /// <param name="defaultMatchMode">
        /// The default match mode to use when none is present in the options,
        /// or null to use the built-in default.
        /// </param>
        /// <param name="matchMode">
        /// Upon return, receives the resolved match mode.
        /// </param>
        /// <param name="container">
        /// Upon return, receives the resolved container name, if any.
        /// </param>
        /// <param name="pattern">
        /// Upon return, receives the resolved match pattern, if any.
        /// </param>
        /// <param name="eagle">
        /// Upon return, receives non-zero if Eagle namespaces should be
        /// imported.
        /// </param>
        /// <param name="clr">
        /// Upon return, receives non-zero if CLR namespaces should be imported.
        /// </param>
        /// <param name="noCase">
        /// Upon return, receives non-zero if case-insensitive matching is
        /// enabled.
        /// </param>
        public static void ProcessObjectImportOptions(
            OptionDictionary options,
            MatchMode? defaultMatchMode,
            out MatchMode matchMode,
            out string container,
            out string pattern,
            out bool eagle,
            out bool clr,
            out bool noCase
            )
        {
            bool nonPublic;

            ProcessObjectImportOptions(
                options, defaultMatchMode, out matchMode, out container,
                out pattern, out eagle, out clr, out nonPublic, out noCase);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method extracts the complete set of options used by the
        /// [object import] sub-command, falling back to the specified defaults
        /// where necessary.  This is the most general overload to which the
        /// other forwards.
        /// </summary>
        /// <param name="options">
        /// The option dictionary that may contain the import related options,
        /// if any.
        /// </param>
        /// <param name="defaultMatchMode">
        /// The default match mode to use when none is present in the options,
        /// or null to use the built-in default.
        /// </param>
        /// <param name="matchMode">
        /// Upon return, receives the resolved match mode.
        /// </param>
        /// <param name="container">
        /// Upon return, receives the resolved container name, if any.
        /// </param>
        /// <param name="pattern">
        /// Upon return, receives the resolved match pattern, if any.
        /// </param>
        /// <param name="eagle">
        /// Upon return, receives non-zero if Eagle namespaces should be
        /// imported.
        /// </param>
        /// <param name="clr">
        /// Upon return, receives non-zero if CLR namespaces should be imported.
        /// </param>
        /// <param name="nonPublic">
        /// Upon return, receives non-zero if non-public members should be
        /// imported.
        /// </param>
        /// <param name="noCase">
        /// Upon return, receives non-zero if case-insensitive matching is
        /// enabled.
        /// </param>
        public static void ProcessObjectImportOptions(
            OptionDictionary options,
            MatchMode? defaultMatchMode,
            out MatchMode matchMode,
            out string container,
            out string pattern,
            out bool eagle,
            out bool clr,
            out bool nonPublic,
            out bool noCase
            )
        {
            IVariant value = null; /* REUSED */

            ///////////////////////////////////////////////////////////////////

            ProcessPatternMatchingOptions(
                options, "-importmode", "-importpattern", "-importnocase",
                defaultMatchMode, out matchMode, out pattern, out noCase);

            ///////////////////////////////////////////////////////////////////

            container = null;

            if ((options != null) &&
                options.CheckPresent("-container", ref value))
            {
                container = value.ToString();
            }

            ///////////////////////////////////////////////////////////////////

            nonPublic = false;

            if ((options != null) && options.CheckPresent("-importnonpublic"))
                nonPublic = true;

            ///////////////////////////////////////////////////////////////////

            eagle = false;

            if ((options != null) && options.CheckPresent("-eagle"))
                eagle = true;

            ///////////////////////////////////////////////////////////////////

            clr = false;

            if ((options != null) && options.CheckPresent("-clr"))
                clr = true;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method extracts the options used by the [object invokeraw]
        /// sub-command, falling back to the specified defaults where necessary.
        /// It forwards to the more general marshalling option processing
        /// method.
        /// </summary>
        /// <param name="options">
        /// The option dictionary that may contain the invocation related
        /// options, if any.
        /// </param>
        /// <param name="objectOptionType">
        /// The object option type that identifies the sub-command being
        /// processed.
        /// </param>
        /// <param name="defaultObjectValueFlags">
        /// The default object value flags to use when none is present in the
        /// options, or null to use the built-in default.
        /// </param>
        /// <param name="defaultBindingFlags">
        /// The default binding flags to use when none is present in the
        /// options, or null to use the built-in default.
        /// </param>
        /// <param name="defaultMarshalFlags">
        /// The default marshal flags to use when none is present in the
        /// options, or null to use the built-in default.
        /// </param>
        /// <param name="defaultByRefArgumentFlags">
        /// The default by-reference argument flags to use when none is present
        /// in the options, or null to use the built-in default.
        /// </param>
        /// <param name="objectType">
        /// Upon return, receives the resolved object type, if any.
        /// </param>
        /// <param name="proxyType">
        /// Upon return, receives the resolved proxy type, if any.
        /// </param>
        /// <param name="objectTypes">
        /// Upon return, receives the resolved list of object types, if any.
        /// </param>
        /// <param name="methodTypes">
        /// Upon return, receives the resolved list of method (signature) types,
        /// if any.
        /// </param>
        /// <param name="parameterTypes">
        /// Upon return, receives the resolved list of parameter types, if any.
        /// </param>
        /// <param name="parameterMarshalFlags">
        /// Upon return, receives the resolved list of per-parameter marshal
        /// flags, if any.
        /// </param>
        /// <param name="objectValueFlags">
        /// Upon return, receives the resolved object value flags.
        /// </param>
        /// <param name="bindingFlags">
        /// Upon return, receives the resolved binding flags.
        /// </param>
        /// <param name="marshalFlags">
        /// Upon return, receives the resolved marshal flags.
        /// </param>
        /// <param name="byRefArgumentFlags">
        /// Upon return, receives the resolved by-reference argument flags.
        /// </param>
        /// <param name="noByRef">
        /// Upon return, receives non-zero if by-reference argument handling is
        /// disabled.
        /// </param>
        /// <param name="strictType">
        /// Upon return, receives non-zero if strict type matching is enabled.
        /// </param>
        /// <param name="strictArgs">
        /// Upon return, receives non-zero if strict argument matching is
        /// enabled.
        /// </param>
        /// <param name="noNestedObject">
        /// Upon return, receives non-zero if nested object resolution is
        /// disabled.
        /// </param>
        /// <param name="noCase">
        /// Upon return, receives non-zero if case-insensitive matching is
        /// enabled.
        /// </param>
        /// <param name="invoke">
        /// Upon return, receives non-zero if the matched member should be
        /// invoked.
        /// </param>
        /// <param name="help">
        /// Upon return, receives non-zero if the matched member should be
        /// looked up in the help file instead of being invoked.
        /// </param>
        /// <param name="noArgs">
        /// Upon return, receives non-zero if argument processing is disabled.
        /// </param>
        /// <param name="arrayAsValue">
        /// Upon return, receives non-zero if arrays should be treated as
        /// values.
        /// </param>
        /// <param name="arrayAsLink">
        /// Upon return, receives non-zero if arrays should be treated as
        /// linked variables.
        /// </param>
        /// <param name="trace">
        /// Upon return, receives non-zero if trace output is enabled.
        /// </param>
        public static void ProcessObjectInvokeRawOptions(
            OptionDictionary options,
            ObjectOptionType objectOptionType,
            ValueFlags? defaultObjectValueFlags,
            BindingFlags? defaultBindingFlags,
            MarshalFlags? defaultMarshalFlags,
            ByRefArgumentFlags? defaultByRefArgumentFlags,
            out Type objectType,
            out Type proxyType,
            out TypeList objectTypes,
            out TypeList methodTypes,
            out TypeList parameterTypes,
            out MarshalFlagsList parameterMarshalFlags,
            out ValueFlags objectValueFlags,
            out BindingFlags bindingFlags,
            out MarshalFlags marshalFlags,
            out ByRefArgumentFlags byRefArgumentFlags,
            out bool noByRef,
            out bool strictType,
            out bool strictArgs,
            out bool noNestedObject,
            out bool noCase,
            out bool invoke,
            out bool help,
            out bool noArgs,
            out bool arrayAsValue,
            out bool arrayAsLink,
            out bool trace
            )
        {
            ValueFlags memberValueFlags;
            MemberTypes memberTypes;
            ReorderFlags reorderFlags;
            bool verbose;

            ProcessMarshalOptions(
                options, objectOptionType, null, null, null,
                defaultBindingFlags, defaultMarshalFlags, null,
                defaultByRefArgumentFlags, out objectType, out proxyType,
                out objectTypes, out methodTypes, out parameterTypes,
                out parameterMarshalFlags, out objectValueFlags,
                out memberValueFlags, out memberTypes, out bindingFlags,
                out marshalFlags, out reorderFlags, out byRefArgumentFlags,
                out noByRef, out verbose, out strictType, out strictArgs,
                out noNestedObject, out noCase, out invoke, out help,
                out noArgs, out arrayAsValue, out arrayAsLink, out trace);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method extracts the options used by the [object isdisposed]
        /// sub-command.
        /// </summary>
        /// <param name="options">
        /// The option dictionary that may contain the disposal-check related
        /// options, if any.
        /// </param>
        /// <param name="noComplain">
        /// Upon return, receives non-zero if errors should be suppressed.
        /// </param>
        /// <param name="force">
        /// Upon return, receives non-zero if the disposal check should be
        /// forced.
        /// </param>
        /// <param name="cannotCheck">
        /// Upon return, receives non-zero if the disposal state cannot be
        /// checked.
        /// </param>
        /// <param name="caughtException">
        /// Upon return, receives non-zero if an exception was caught while
        /// checking the disposal state.
        /// </param>
        public static void ProcessObjectIsDisposedOptions(
            OptionDictionary options,
            out bool noComplain,
            out bool force,
            out bool cannotCheck,
            out bool caughtException
            )
        {
            IVariant value = null; /* REUSED */

            ///////////////////////////////////////////////////////////////////

            noComplain = false;

            if ((options != null) && options.CheckPresent("-nocomplain"))
                noComplain = true;

            ///////////////////////////////////////////////////////////////////

            force = false;

            if ((options != null) &&
                options.CheckPresent("-force", ref value))
            {
                force = (bool)value.Value;
            }

            ///////////////////////////////////////////////////////////////////

            cannotCheck = false;

            if ((options != null) &&
                options.CheckPresent("-cannotcheck", ref value))
            {
                cannotCheck = (bool)value.Value;
            }

            ///////////////////////////////////////////////////////////////////

            caughtException = false;

            if ((options != null) &&
                options.CheckPresent("-caughtexception", ref value))
            {
                caughtException = (bool)value.Value;
            }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method extracts the options used by the [object isnull]
        /// sub-command, building upon the disposal-check options.
        /// </summary>
        /// <param name="options">
        /// The option dictionary that may contain the null-check related
        /// options, if any.
        /// </param>
        /// <param name="noComplain">
        /// Upon return, receives non-zero if errors should be suppressed.
        /// </param>
        /// <param name="objectDisposed">
        /// Upon return, receives non-zero if a disposed object should be
        /// treated as null.
        /// </param>
        /// <param name="valueDisposed">
        /// Upon return, receives non-zero if a disposed value should be treated
        /// as null.
        /// </param>
        /// <param name="force">
        /// Upon return, receives non-zero if the disposal check should be
        /// forced.
        /// </param>
        /// <param name="cannotCheck">
        /// Upon return, receives non-zero if the disposal state cannot be
        /// checked.
        /// </param>
        /// <param name="caughtException">
        /// Upon return, receives non-zero if an exception was caught while
        /// checking the disposal state.
        /// </param>
        public static void ProcessObjectIsNullOptions(
            OptionDictionary options,
            out bool noComplain,
            out bool objectDisposed,
            out bool valueDisposed,
            out bool force,
            out bool cannotCheck,
            out bool caughtException
            )
        {
            IVariant value = null; /* REUSED */

            ///////////////////////////////////////////////////////////////////

            ProcessObjectIsDisposedOptions(
                options, out noComplain, out force, out cannotCheck,
                out caughtException);

            ///////////////////////////////////////////////////////////////////

            objectDisposed = true; /* TODO: Good default? */

            if ((options != null) &&
                options.CheckPresent("-objectdisposed", ref value))
            {
                objectDisposed = (bool)value.Value;
            }

            ///////////////////////////////////////////////////////////////////

            valueDisposed = true; /* TODO: Good default? */

            if ((options != null) &&
                options.CheckPresent("-valuedisposed", ref value))
            {
                valueDisposed = (bool)value.Value;
            }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method extracts the options used by the [object isoftype]
        /// sub-command, falling back to the specified defaults where necessary.
        /// </summary>
        /// <param name="options">
        /// The option dictionary that may contain the type-check related
        /// options, if any.
        /// </param>
        /// <param name="defaultObjectValueFlags">
        /// The default object value flags to use when none is present in the
        /// options, or null to use the built-in default.
        /// </param>
        /// <param name="defaultMarshalFlags">
        /// The default marshal flags to use when none is present in the
        /// options, or null to use the built-in default.
        /// </param>
        /// <param name="objectTypes">
        /// Upon return, receives the resolved list of object types, if any.
        /// </param>
        /// <param name="objectValueFlags">
        /// Upon return, receives the resolved object value flags.
        /// </param>
        /// <param name="marshalFlags">
        /// Upon return, receives the resolved marshal flags.
        /// </param>
        /// <param name="verbose">
        /// Upon return, receives non-zero if verbose output is enabled.
        /// </param>
        /// <param name="strictType">
        /// Upon return, receives non-zero if strict type matching is enabled.
        /// </param>
        /// <param name="noCase">
        /// Upon return, receives non-zero if case-insensitive matching is
        /// enabled.
        /// </param>
        /// <param name="noComplain">
        /// Upon return, receives non-zero if errors should be suppressed.
        /// </param>
        /// <param name="assignable">
        /// Upon return, receives non-zero if assignment compatibility (rather
        /// than an exact type match) should be checked.
        /// </param>
        public static void ProcessObjectIsOfTypeOptions(
            OptionDictionary options,
            ValueFlags? defaultObjectValueFlags,
            MarshalFlags? defaultMarshalFlags,
            out TypeList objectTypes,
            out ValueFlags objectValueFlags,
            out MarshalFlags marshalFlags,
            out bool verbose,
            out bool strictType,
            out bool noCase,
            out bool noComplain,
            out bool assignable
            )
        {
            ProcessGetTypeOptions(
                options, defaultObjectValueFlags, defaultMarshalFlags,
                out objectTypes, out objectValueFlags, out marshalFlags,
                out verbose, out strictType, out noCase);

            ///////////////////////////////////////////////////////////////////

            noComplain = false;

            if ((options != null) && options.CheckPresent("-nocomplain"))
                noComplain = true;

            ///////////////////////////////////////////////////////////////////

            assignable = false;

            if ((options != null) && options.CheckPresent("-assignable"))
                assignable = true;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method extracts the options used by the [object load]
        /// sub-command, falling back to the specified defaults where necessary.
        /// </summary>
        /// <param name="options">
        /// The option dictionary that may contain the load related options, if
        /// any.
        /// </param>
        /// <param name="defaultLoadType">
        /// The default load type to use when none is present in the options, or
        /// null to use the built-in default.
        /// </param>
        /// <param name="defaultMatchMode">
        /// The default match mode to use when none is present in the options,
        /// or null to use the built-in default.
        /// </param>
        /// <param name="namespace">
        /// Upon return, receives the resolved target namespace, if any.
        /// </param>
        /// <param name="loadType">
        /// Upon return, receives the resolved load type.
        /// </param>
        /// <param name="declareMatchMode">
        /// Upon return, receives the resolved match mode used for declaring.
        /// </param>
        /// <param name="importMatchMode">
        /// Upon return, receives the resolved match mode used for importing.
        /// </param>
        /// <param name="declarePattern">
        /// Upon return, receives the resolved match pattern used for declaring,
        /// if any.
        /// </param>
        /// <param name="importPattern">
        /// Upon return, receives the resolved match pattern used for importing,
        /// if any.
        /// </param>
        /// <param name="declare">
        /// Upon return, receives non-zero if the loaded types should be
        /// declared.
        /// </param>
        /// <param name="import">
        /// Upon return, receives non-zero if the loaded namespaces should be
        /// imported.
        /// </param>
        /// <param name="declareNonPublic">
        /// Upon return, receives non-zero if non-public members should be
        /// declared.
        /// </param>
        /// <param name="declareNoCase">
        /// Upon return, receives non-zero if case-insensitive matching is used
        /// for declaring.
        /// </param>
        /// <param name="importNonPublic">
        /// Upon return, receives non-zero if non-public members should be
        /// imported.
        /// </param>
        /// <param name="importNoCase">
        /// Upon return, receives non-zero if case-insensitive matching is used
        /// for importing.
        /// </param>
        /// <param name="fromObject">
        /// Upon return, receives non-zero if the assembly should be loaded from
        /// an existing object.
        /// </param>
        /// <param name="reflectionOnly">
        /// Upon return, receives non-zero if the assembly should be loaded for
        /// reflection only.
        /// </param>
        /// <param name="trustedOnly">
        /// Upon return, receives non-zero if only trusted assemblies should be
        /// loaded.
        /// </param>
        /// <param name="verifiedOnly">
        /// Upon return, receives non-zero if only verified assemblies should be
        /// loaded.
        /// </param>
        public static void ProcessObjectLoadOptions(
            OptionDictionary options,
            LoadType? defaultLoadType,
            MatchMode? defaultMatchMode,
            out INamespace @namespace,
            out LoadType loadType,
            out MatchMode declareMatchMode,
            out MatchMode importMatchMode,
            out string declarePattern,
            out string importPattern,
            out bool declare,
            out bool import,
            out bool declareNonPublic,
            out bool declareNoCase,
            out bool importNonPublic,
            out bool importNoCase,
            out bool fromObject,
            out bool reflectionOnly,
            out bool trustedOnly,
            out bool verifiedOnly
            )
        {
            IVariant value = null; /* REUSED */

            ///////////////////////////////////////////////////////////////////

            ProcessPatternMatchingOptions(
                options, "-declaremode", "-declarepattern", "-declarenocase",
                defaultMatchMode, out declareMatchMode, out declarePattern,
                out declareNoCase);

            ///////////////////////////////////////////////////////////////////

            ProcessPatternMatchingOptions(
                options, "-importmode", "-importpattern", "-importnocase",
                defaultMatchMode, out importMatchMode, out importPattern,
                out importNoCase);

            ///////////////////////////////////////////////////////////////////

            @namespace = null;

            if ((options != null) &&
                options.CheckPresent("-namespace", ref value))
            {
                @namespace = (INamespace)value.Value;
            }

            ///////////////////////////////////////////////////////////////////

            loadType = (defaultLoadType != null) ?
                (LoadType)defaultLoadType : DefaultLoadType;

            if ((options != null) &&
                options.CheckPresent("-loadtype", ref value))
            {
                loadType = (LoadType)value.Value;
            }

            ///////////////////////////////////////////////////////////////////

            declare = false;

            if ((options != null) && options.CheckPresent("-declare"))
                declare = true;

            ///////////////////////////////////////////////////////////////////

            import = false;

            if ((options != null) && options.CheckPresent("-import"))
                import = true;

            ///////////////////////////////////////////////////////////////////

            declareNonPublic = false;

            if ((options != null) && options.CheckPresent("-declarenonpublic"))
                declareNonPublic = true;

            ///////////////////////////////////////////////////////////////////

            importNonPublic = false;

            if ((options != null) && options.CheckPresent("-importnonpublic"))
                importNonPublic = true;

            ///////////////////////////////////////////////////////////////////

            fromObject = false;

            if ((options != null) && options.CheckPresent("-fromobject"))
                fromObject = true;

            ///////////////////////////////////////////////////////////////////

            reflectionOnly = false;

            if ((options != null) && options.CheckPresent("-reflectiononly"))
                reflectionOnly = true;

            ///////////////////////////////////////////////////////////////////

            trustedOnly = false;

            if ((options != null) && options.CheckPresent("-trustedonly"))
                trustedOnly = true;

#if !DEBUG
            if ((options != null) && options.CheckPresent("-maybetrustedonly"))
                trustedOnly = true;
#endif

            ///////////////////////////////////////////////////////////////////

            verifiedOnly = false;

            if ((options != null) && options.CheckPresent("-verifiedonly"))
                verifiedOnly = true;

#if !DEBUG
            if ((options != null) && options.CheckPresent("-maybeverifiedonly"))
                verifiedOnly = true;
#endif
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method extracts the options used by the [object members]
        /// sub-command, falling back to the specified defaults where necessary.
        /// </summary>
        /// <param name="options">
        /// The option dictionary that may contain the member-listing related
        /// options, if any.
        /// </param>
        /// <param name="objectOptionType">
        /// The object option type that identifies the sub-command being
        /// processed.
        /// </param>
        /// <param name="defaultObjectValueFlags">
        /// The default object value flags to use when none is present in the
        /// options, or null to use the built-in default.
        /// </param>
        /// <param name="defaultMemberTypes">
        /// The default member types to use when none is present in the options,
        /// or null to use the built-in default.
        /// </param>
        /// <param name="defaultBindingFlags">
        /// The default binding flags to use when none is present in the
        /// options, or null to use the built-in default.
        /// </param>
        /// <param name="defaultMarshalFlags">
        /// The default marshal flags to use when none is present in the
        /// options, or null to use the built-in default.
        /// </param>
        /// <param name="defaultMatchMode">
        /// The default match mode to use when none is present in the options,
        /// or null to use the built-in default.
        /// </param>
        /// <param name="objectTypes">
        /// Upon return, receives the resolved list of object types, if any.
        /// </param>
        /// <param name="objectValueFlags">
        /// Upon return, receives the resolved object value flags.
        /// </param>
        /// <param name="memberTypes">
        /// Upon return, receives the resolved member types.
        /// </param>
        /// <param name="bindingFlags">
        /// Upon return, receives the resolved binding flags.
        /// </param>
        /// <param name="marshalFlags">
        /// Upon return, receives the resolved marshal flags.
        /// </param>
        /// <param name="matchMode">
        /// Upon return, receives the resolved match mode.
        /// </param>
        /// <param name="pattern">
        /// Upon return, receives the resolved match pattern, if any.
        /// </param>
        /// <param name="verbose">
        /// Upon return, receives non-zero if verbose output is enabled.
        /// </param>
        /// <param name="strictType">
        /// Upon return, receives non-zero if strict type matching is enabled.
        /// </param>
        /// <param name="noCase">
        /// Upon return, receives non-zero if case-insensitive matching is
        /// enabled.
        /// </param>
        /// <param name="attributes">
        /// Upon return, receives non-zero if member attributes should be
        /// included.
        /// </param>
        /// <param name="matchNameOnly">
        /// Upon return, receives non-zero if matching should consider the
        /// member name only.
        /// </param>
        /// <param name="nameOnly">
        /// Upon return, receives non-zero if only member names should be
        /// returned.
        /// </param>
        /// <param name="signatures">
        /// Upon return, receives non-zero if member signatures should be
        /// included.
        /// </param>
        /// <param name="qualified">
        /// Upon return, receives non-zero if fully qualified names should be
        /// returned.
        /// </param>
        public static void ProcessObjectMembersOptions(
            OptionDictionary options,
            ObjectOptionType objectOptionType,
            ValueFlags? defaultObjectValueFlags,
            MemberTypes? defaultMemberTypes,
            BindingFlags? defaultBindingFlags,
            MarshalFlags? defaultMarshalFlags,
            MatchMode? defaultMatchMode,
            out TypeList objectTypes,
            out ValueFlags objectValueFlags,
            out MemberTypes memberTypes,
            out BindingFlags bindingFlags,
            out MarshalFlags marshalFlags,
            out MatchMode matchMode,
            out string pattern,
            out bool verbose,
            out bool strictType,
            out bool noCase,
            out bool attributes,
            out bool matchNameOnly,
            out bool nameOnly,
            out bool signatures,
            out bool qualified
            )
        {
            ProcessGetTypeOptions(
                options, defaultObjectValueFlags, defaultMarshalFlags,
                out objectTypes, out objectValueFlags, out marshalFlags,
                out verbose, out strictType, out noCase);

            ///////////////////////////////////////////////////////////////////

            ProcessReflectionOptions(
                options, objectOptionType, defaultMemberTypes,
                defaultBindingFlags, out memberTypes, out bindingFlags);

            ///////////////////////////////////////////////////////////////////

            ProcessPatternMatchingOptions(
                options, "-mode", "-pattern", defaultMatchMode,
                out matchMode, out pattern);

            ///////////////////////////////////////////////////////////////////

            attributes = false;

            if ((options != null) && options.CheckPresent("-attributes"))
                attributes = true;

            ///////////////////////////////////////////////////////////////////

            matchNameOnly = false;

            if ((options != null) && options.CheckPresent("-matchnameonly"))
                matchNameOnly = true;

            ///////////////////////////////////////////////////////////////////

            nameOnly = false;

            if ((options != null) && options.CheckPresent("-nameonly"))
                nameOnly = true;

            ///////////////////////////////////////////////////////////////////

            signatures = false;

            if ((options != null) && options.CheckPresent("-signatures"))
                signatures = true;

            ///////////////////////////////////////////////////////////////////

            qualified = false;

            if ((options != null) && options.CheckPresent("-qualified"))
                qualified = true;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method extracts the options used by the [object type]
        /// sub-command, falling back to the specified defaults where necessary.
        /// </summary>
        /// <param name="options">
        /// The option dictionary that may contain the type related options, if
        /// any.
        /// </param>
        /// <param name="defaultMatchMode">
        /// The default match mode to use when none is present in the options,
        /// or null to use the built-in default.
        /// </param>
        /// <param name="matchMode">
        /// Upon return, receives the resolved match mode.
        /// </param>
        /// <param name="pattern">
        /// Upon return, receives the resolved match pattern, if any.
        /// </param>
        /// <param name="noCase">
        /// Upon return, receives non-zero if case-insensitive matching is
        /// enabled.
        /// </param>
        public static void ProcessObjectTypeOptions(
            OptionDictionary options,
            MatchMode? defaultMatchMode,
            out MatchMode matchMode,
            out string pattern,
            out bool noCase
            )
        {
            ProcessPatternMatchingOptions(
                options, "-typemode", "-typepattern", "-typenocase",
                defaultMatchMode, out matchMode, out pattern, out noCase);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method extracts the options used by the [object
        /// unaliasnamespace] sub-command, falling back to the specified
        /// defaults where necessary.
        /// </summary>
        /// <param name="options">
        /// The option dictionary that may contain the unalias related options,
        /// if any.
        /// </param>
        /// <param name="defaultMatchMode">
        /// The default match mode to use when none is present in the options,
        /// or null to use the built-in default.
        /// </param>
        /// <param name="matchMode">
        /// Upon return, receives the resolved match mode.
        /// </param>
        /// <param name="pattern">
        /// Upon return, receives the resolved match pattern, if any.
        /// </param>
        /// <param name="noCase">
        /// Upon return, receives non-zero if case-insensitive matching is
        /// enabled.
        /// </param>
        /// <param name="values">
        /// Upon return, receives non-zero if matching should be performed by
        /// container value.
        /// </param>
        public static void ProcessObjectUnaliasNamespaceOptions(
            OptionDictionary options,
            MatchMode? defaultMatchMode,
            out MatchMode matchMode,
            out string pattern,
            out bool noCase,
            out bool values
            )
        {
            ProcessPatternMatchingOptions(
                options, "-aliasmode", "-aliaspattern", "-aliasnocase",
                defaultMatchMode, out matchMode, out pattern, out noCase);

            ///////////////////////////////////////////////////////////////////

            values = false;

            if ((options != null) && options.CheckPresent("-bycontainer"))
                values = true;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method extracts the options used by the [object undeclare]
        /// sub-command, falling back to the specified defaults where necessary.
        /// </summary>
        /// <param name="options">
        /// The option dictionary that may contain the undeclare related
        /// options, if any.
        /// </param>
        /// <param name="defaultMatchMode">
        /// The default match mode to use when none is present in the options,
        /// or null to use the built-in default.
        /// </param>
        /// <param name="matchMode">
        /// Upon return, receives the resolved match mode.
        /// </param>
        /// <param name="pattern">
        /// Upon return, receives the resolved match pattern, if any.
        /// </param>
        /// <param name="noCase">
        /// Upon return, receives non-zero if case-insensitive matching is
        /// enabled.
        /// </param>
        /// <param name="values">
        /// Upon return, receives non-zero if matching should be performed by
        /// container value.
        /// </param>
        public static void ProcessObjectUndeclareOptions(
            OptionDictionary options,
            MatchMode? defaultMatchMode,
            out MatchMode matchMode,
            out string pattern,
            out bool noCase,
            out bool values
            )
        {
            ProcessPatternMatchingOptions(
                options, "-declaremode", "-declarepattern", "-declarenocase",
                defaultMatchMode, out matchMode, out pattern, out noCase);

            ///////////////////////////////////////////////////////////////////

            values = false;

            if ((options != null) && options.CheckPresent("-bycontainer"))
                values = true;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method extracts the options used by the [object unimport]
        /// sub-command, falling back to the specified defaults where necessary.
        /// </summary>
        /// <param name="options">
        /// The option dictionary that may contain the unimport related options,
        /// if any.
        /// </param>
        /// <param name="defaultMatchMode">
        /// The default match mode to use when none is present in the options,
        /// or null to use the built-in default.
        /// </param>
        /// <param name="matchMode">
        /// Upon return, receives the resolved match mode.
        /// </param>
        /// <param name="pattern">
        /// Upon return, receives the resolved match pattern, if any.
        /// </param>
        /// <param name="noCase">
        /// Upon return, receives non-zero if case-insensitive matching is
        /// enabled.
        /// </param>
        /// <param name="values">
        /// Upon return, receives non-zero if matching should be performed by
        /// container value.
        /// </param>
        public static void ProcessObjectUnimportOptions(
            OptionDictionary options,
            MatchMode? defaultMatchMode,
            out MatchMode matchMode,
            out string pattern,
            out bool noCase,
            out bool values
            )
        {
            ProcessPatternMatchingOptions(
                options, "-importmode", "-importpattern", "-importnocase",
                defaultMatchMode, out matchMode, out pattern, out noCase);

            ///////////////////////////////////////////////////////////////////

            values = false;

            if ((options != null) && options.CheckPresent("-bycontainer"))
                values = true;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method extracts the options used by the [object untype]
        /// sub-command, falling back to the specified defaults where necessary.
        /// </summary>
        /// <param name="options">
        /// The option dictionary that may contain the untype related options,
        /// if any.
        /// </param>
        /// <param name="defaultMatchMode">
        /// The default match mode to use when none is present in the options,
        /// or null to use the built-in default.
        /// </param>
        /// <param name="matchMode">
        /// Upon return, receives the resolved match mode.
        /// </param>
        /// <param name="pattern">
        /// Upon return, receives the resolved match pattern, if any.
        /// </param>
        /// <param name="noCase">
        /// Upon return, receives non-zero if case-insensitive matching is
        /// enabled.
        /// </param>
        public static void ProcessObjectUntypeOptions(
            OptionDictionary options,
            MatchMode? defaultMatchMode,
            out MatchMode matchMode,
            out string pattern,
            out bool noCase
            )
        {
            ProcessPatternMatchingOptions(
                options, "-typemode", "-typepattern", "-typenocase",
                defaultMatchMode, out matchMode, out pattern, out noCase);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method extracts the match mode and pattern options identified by
        /// the specified option names, falling back to the specified default
        /// where necessary.  This convenience overload omits the
        /// case-insensitive matching support and forwards to the most general
        /// overload.
        /// </summary>
        /// <param name="options">
        /// The option dictionary that may contain the pattern-matching related
        /// options, if any.
        /// </param>
        /// <param name="matchModeOptionName">
        /// The name of the option that specifies the match mode, if any.
        /// </param>
        /// <param name="patternOptionName">
        /// The name of the option that specifies the match pattern, if any.
        /// </param>
        /// <param name="defaultMatchMode">
        /// The default match mode to use when none is present in the options,
        /// or null to use the built-in default.
        /// </param>
        /// <param name="matchMode">
        /// Upon return, receives the resolved match mode.
        /// </param>
        /// <param name="pattern">
        /// Upon return, receives the resolved match pattern, if any.
        /// </param>
        private static void ProcessPatternMatchingOptions(
            OptionDictionary options,
            string matchModeOptionName,
            string patternOptionName,
            MatchMode? defaultMatchMode,
            out MatchMode matchMode,
            out string pattern
            )
        {
            bool noCase;

            ProcessPatternMatchingOptions(
                options, matchModeOptionName, patternOptionName, null,
                defaultMatchMode, out matchMode, out pattern, out noCase);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method extracts the match mode, pattern, and case-sensitivity
        /// options identified by the specified option names, falling back to the
        /// specified default where necessary.  This is the most general overload
        /// to which the other forwards.
        /// </summary>
        /// <param name="options">
        /// The option dictionary that may contain the pattern-matching related
        /// options, if any.
        /// </param>
        /// <param name="matchModeOptionName">
        /// The name of the option that specifies the match mode, if any.
        /// </param>
        /// <param name="patternOptionName">
        /// The name of the option that specifies the match pattern, if any.
        /// </param>
        /// <param name="noCaseOptionName">
        /// The name of the option that specifies case-insensitive matching, if
        /// any.
        /// </param>
        /// <param name="defaultMatchMode">
        /// The default match mode to use when none is present in the options,
        /// or null to use the built-in default.
        /// </param>
        /// <param name="matchMode">
        /// Upon return, receives the resolved match mode.
        /// </param>
        /// <param name="pattern">
        /// Upon return, receives the resolved match pattern, if any.
        /// </param>
        /// <param name="noCase">
        /// Upon return, receives non-zero if case-insensitive matching is
        /// enabled.
        /// </param>
        private static void ProcessPatternMatchingOptions(
            OptionDictionary options,
            string matchModeOptionName,
            string patternOptionName,
            string noCaseOptionName,
            MatchMode? defaultMatchMode,
            out MatchMode matchMode,
            out string pattern,
            out bool noCase
            )
        {
            IVariant value = null; /* REUSED */

            ///////////////////////////////////////////////////////////////////

            matchMode = (defaultMatchMode != null) ?
                (MatchMode)defaultMatchMode : DefaultMatchMode;

            if ((options != null) &&
                (matchModeOptionName != null) &&
                options.CheckPresent(matchModeOptionName, ref value))
            {
                matchMode = (MatchMode)value.Value;
            }

            ///////////////////////////////////////////////////////////////////

            pattern = null;

            if ((options != null) &&
                (patternOptionName != null) &&
                options.CheckPresent(patternOptionName, ref value))
            {
                pattern = value.ToString();
            }

            ///////////////////////////////////////////////////////////////////

            noCase = false;

            if ((options != null) &&
                (noCaseOptionName != null) &&
                options.CheckPresent(noCaseOptionName))
            {
                noCase = true;
            }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method extracts the reflection options (i.e. the member types
        /// and binding flags) from the supplied options, falling back to the
        /// specified defaults where necessary.  This is the core overload to
        /// which the other forwards.
        /// </summary>
        /// <param name="options">
        /// The option dictionary that may contain the reflection related
        /// options, if any.
        /// </param>
        /// <param name="objectOptionType">
        /// The object option type that identifies the sub-command being
        /// processed.
        /// </param>
        /// <param name="defaultMemberTypes">
        /// The default member types to use when none is present in the options,
        /// or null to use the built-in default.
        /// </param>
        /// <param name="defaultBindingFlags">
        /// The default binding flags to use when none is present in the
        /// options, or null to use the built-in default.
        /// </param>
        /// <param name="memberTypes">
        /// Upon return, receives the resolved member types.
        /// </param>
        /// <param name="bindingFlags">
        /// Upon return, receives the resolved binding flags.
        /// </param>
        private static void ProcessReflectionOptions(
            OptionDictionary options,
            ObjectOptionType objectOptionType, /* NOT USED */
            MemberTypes? defaultMemberTypes,
            BindingFlags? defaultBindingFlags,
            out MemberTypes memberTypes,
            out BindingFlags bindingFlags
            )
        {
            IVariant value = null; /* REUSED */

            ///////////////////////////////////////////////////////////////////

            memberTypes = (defaultMemberTypes != null) ?
                (MemberTypes)defaultMemberTypes : DefaultMemberTypes;

            if ((options != null) &&
                options.CheckPresent("-membertypes", ref value))
            {
                memberTypes = (MemberTypes)value.Value;
            }

            ///////////////////////////////////////////////////////////////////

            bindingFlags = (defaultBindingFlags != null) ?
                (BindingFlags)defaultBindingFlags : DefaultBindingFlags;

            //
            // TODO: Is this a really bad option name?
            //
            bool hadFlags = (options != null) &&
                options.CheckPresent("-flags", ref value);

            if (hadFlags)
                bindingFlags = (BindingFlags)value.Value;

            if ((options != null) &&
                options.CheckPresent("-bindingflags", ref value))
            {
                if (hadFlags)
                    bindingFlags |= (BindingFlags)value.Value;
                else
                    bindingFlags = (BindingFlags)value.Value;
            }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method extracts the reflection options (i.e. the member types,
        /// binding flags, and value flags) from the supplied options, falling
        /// back to the specified defaults where necessary.  This overload also
        /// resolves the object and member value flags before forwarding to the
        /// core overload.
        /// </summary>
        /// <param name="options">
        /// The option dictionary that may contain the reflection related
        /// options, if any.
        /// </param>
        /// <param name="objectOptionType">
        /// The object option type that identifies the sub-command being
        /// processed.
        /// </param>
        /// <param name="defaultMemberTypes">
        /// The default member types to use when none is present in the options,
        /// or null to use the built-in default.
        /// </param>
        /// <param name="defaultBindingFlags">
        /// The default binding flags to use when none is present in the
        /// options, or null to use the built-in default.
        /// </param>
        /// <param name="defaultObjectValueFlags">
        /// The default object value flags to use when none is present in the
        /// options, or null to use the built-in default.
        /// </param>
        /// <param name="defaultMemberValueFlags">
        /// The default member value flags to use when none is present in the
        /// options, or null to use the built-in default.
        /// </param>
        /// <param name="memberTypes">
        /// Upon return, receives the resolved member types.
        /// </param>
        /// <param name="bindingFlags">
        /// Upon return, receives the resolved binding flags.
        /// </param>
        /// <param name="objectValueFlags">
        /// Upon return, receives the resolved object value flags.
        /// </param>
        /// <param name="memberValueFlags">
        /// Upon return, receives the resolved member value flags.
        /// </param>
        public static void ProcessReflectionOptions(
            OptionDictionary options,
            ObjectOptionType objectOptionType,
            MemberTypes? defaultMemberTypes,
            BindingFlags? defaultBindingFlags,
            ValueFlags? defaultObjectValueFlags,
            ValueFlags? defaultMemberValueFlags,
            out MemberTypes memberTypes,
            out BindingFlags bindingFlags,
            out ValueFlags objectValueFlags,
            out ValueFlags memberValueFlags
            )
        {
            IVariant value = null; /* REUSED */

            ///////////////////////////////////////////////////////////////////

            objectValueFlags = (defaultObjectValueFlags != null) ?
                (ValueFlags)defaultObjectValueFlags : DefaultObjectValueFlags;

            if ((options != null) &&
                options.CheckPresent("-objectvalueflags", ref value))
            {
                objectValueFlags = (ValueFlags)value.Value;
            }

            ///////////////////////////////////////////////////////////////////

            memberValueFlags = (defaultMemberValueFlags != null) ?
                (ValueFlags)defaultMemberValueFlags : DefaultMemberValueFlags;

            if ((options != null) &&
                options.CheckPresent("-membervalueflags", ref value))
            {
                memberValueFlags = (ValueFlags)value.Value;
            }

            ///////////////////////////////////////////////////////////////////

            ProcessReflectionOptions(
                options, objectOptionType, defaultMemberTypes,
                defaultBindingFlags, out memberTypes, out bindingFlags);
        }
        #endregion
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Object Invocation Support Methods
        /// <summary>
        /// This method conditionally breaks into the debugger to assist with
        /// diagnosing method overload resolution.  It does nothing unless debug
        /// mode is enabled and either more than one matching overload was found
        /// or an error occurred.
        /// </summary>
        /// <param name="code">
        /// The return code produced by the overload-matching operation.
        /// </param>
        /// <param name="methodIndexList">
        /// The list of indexes identifying the matching method overloads, if
        /// any.
        /// </param>
        /// <param name="errors">
        /// The list of errors produced by the overload-matching operation, if
        /// any.
        /// </param>
        /// <param name="debug">
        /// Non-zero to enable the conditional debugger break.
        /// </param>
        public static void MaybeBreakForMethodOverloadResolution(
            ReturnCode code,
            IntList methodIndexList,
            ResultList errors,
            bool debug
            )
        {
            if (debug)
            {
                if ((methodIndexList != null) && (methodIndexList.Count > 1))
                {
                    //
                    // NOTE: There is more than one matching method overload;
                    //       breaking into the debugger at this point can be
                    //       very helpful when trying to figure out which one
                    //       should be selected.
                    //
                    DebugOps.MaybeBreak();
                }
                else if (code != ReturnCode.Ok)
                {
                    //
                    // NOTE: There was an error of some kind when matching
                    //       with the available method overloads; breaking
                    //       into the debugger at this point can be very
                    //       helpful when trying to figure out what went
                    //       wrong.
                    //
                    Result error = (errors != null) ? errors : null;

                    if (error != null)
                        DebugOps.MaybeBreak(error);
                    else
                        DebugOps.MaybeBreak();
                }
            }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method extracts the underlying methods, delegate type, object
        /// name, and member name from the specified list of delegates.  All of
        /// the delegates must share the same delegate type, object name, and
        /// member name.
        /// </summary>
        /// <param name="delegates">
        /// The list of delegate triplets to examine.
        /// </param>
        /// <param name="delegateType">
        /// Upon success, receives the common delegate type.
        /// </param>
        /// <param name="objectName">
        /// Upon success, receives the common object name, if any.
        /// </param>
        /// <param name="memberName">
        /// Upon success, receives the common member name, if any.
        /// </param>
        /// <param name="methods">
        /// Upon success, receives the array of methods extracted from the
        /// delegates.
        /// </param>
        /// <param name="error">
        /// Upon failure, receives information about the error.
        /// </param>
        /// <returns>
        /// True if the methods were successfully extracted; otherwise, false.
        /// </returns>
        private static bool GetMethodsFromDelegates(
            DelegateList delegates,   /* in */
            ref Type delegateType,    /* out */
            ref string objectName,    /* out */
            ref string memberName,    /* out */
            ref MethodInfo[] methods, /* out */
            ref Result error          /* out */
            )
        {
            if (delegates == null)
            {
                error = "cannot invoke, invalid delegates";
                return false;
            }

            int count = delegates.Count;

            if (delegates.Count == 0)
            {
                error = "cannot invoke, no delegates";
                return false;
            }

            Type localDelegateType = null;
            string localObjectName = null;
            string localMemberName = null;
            MethodInfo[] localMethods = new MethodInfo[count];

            for (int index = 0; index < count; index++)
            {
                DelegateTriplet outerDelegate = delegates[index];

                if (outerDelegate == null)
                {
                    error = String.Format(
                        "cannot invoke #{0}, bad delegate",
                        index);

                    return false;
                }

                Delegate innerDelegate = outerDelegate.Y;

                if (innerDelegate == null)
                {
                    error = String.Format(
                        "cannot invoke #{0}, no delegate",
                        index);

                    return false;
                }

                MethodInfo method = innerDelegate.Method;

                if (method == null)
                {
                    error = String.Format(
                        "cannot invoke #{0}, no method",
                        index);

                    return false;
                }

                localDelegateType = innerDelegate.GetType();

                if (localDelegateType == null)
                {
                    error = String.Format(
                        "cannot invoke #{0}, invalid type",
                        index);

                    return false;
                }

                object target = innerDelegate.Target;

                Type targetType = (target != null) ?
                    AppDomainOps.MaybeGetTypeOrNull(target) :
                    localDelegateType;

                localObjectName = (targetType != null) ?
                    targetType.FullName : null;

                if (localObjectName == null)
                {
                    error = String.Format(
                        "cannot invoke #{0}, invalid object name",
                        index);

                    return false;
                }

                localMemberName = method.Name;

                if (localMemberName == null)
                {
                    error = String.Format(
                        "cannot invoke #{0}, invalid member name",
                        index);

                    return false;
                }

                localMethods[index] = method;
            }

            delegateType = localDelegateType;
            objectName = localObjectName;
            memberName = localMemberName;
            methods = localMethods;

            return true;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method invokes one of the methods represented by the specified
        /// list of delegates, optionally processing options and converting the
        /// supplied arguments as necessary.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter context in which the invocation is performed.
        /// </param>
        /// <param name="delegates">
        /// The list of delegate triplets representing the candidate methods.
        /// </param>
        /// <param name="arguments">
        /// The list of arguments to be processed and passed to the invoked
        /// method.
        /// </param>
        /// <param name="allowOptions">
        /// Non-zero if leading options are permitted within the argument list.
        /// </param>
        /// <param name="nameCount">
        /// The number of arguments that make up the leading method name.
        /// </param>
        /// <param name="nameIndex">
        /// The argument index at which the method name begins.
        /// </param>
        /// <param name="delegate">
        /// Upon success, receives the delegate that was actually invoked.
        /// </param>
        /// <param name="result">
        /// Upon success, receives the result of the invocation; upon failure,
        /// receives information about the error.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise, an appropriate
        /// error code.
        /// </returns>
        public static ReturnCode InvokeDelegate(
            Interpreter interpreter, /* in */
            DelegateList delegates,  /* in */
            ArgumentList arguments,  /* in */
            bool allowOptions,       /* in */
            int nameCount,           /* in */
            int nameIndex,           /* in */
            ref Delegate @delegate,  /* out */
            ref Result result        /* out */
            )
        {
            ///////////////////////////////////////////////////////////////////
            //                       ARGUMENT VALIDATION
            ///////////////////////////////////////////////////////////////////

            if (interpreter == null)
            {
                result = "invalid interpreter";
                return ReturnCode.Error;
            }

            if (delegates == null)
            {
                result = "invalid delegates";
                return ReturnCode.Error;
            }

            if (arguments == null)
            {
                result = "invalid argument list";
                return ReturnCode.Error;
            }

            int argumentCount = arguments.Count; /* MAY BE ZERO */

            ///////////////////////////////////////////////////////////////////

            ReturnCode code;
            OptionDictionary options;
            int argumentIndex;

            if (allowOptions)
            {
                options = GetCallOptions();
                argumentIndex = Index.Invalid;

                if (argumentCount > nameCount)
                {
                    code = interpreter.GetOptions(options,
                        arguments, 0, nameIndex, Index.Invalid,
                        false, ref argumentIndex, ref result);

                    if (code == ReturnCode.Ok)
                    {
                        //
                        // NOTE: The argument count is 4, e.g.:
                        //
                        //       [some -flags +NonPublic get_Token]
                        //          0      1      2      3
                        //
                        //       The "name count" is 2 and the
                        //       "name (start) index" is 1, e.g.:
                        //
                        //         cmd ?options? subCmd ?arg ...?
                        //          0      1      2      3
                        //
                        //       This means we were starting the
                        //       option scan at argument index #1
                        //       and that this (sub-)command name
                        //       must take up at least 2 arguments.
                        //
                        if (argumentIndex != Index.Invalid)
                        {
                            argumentIndex += (nameCount - nameIndex);
                        }
                        else if (nameIndex < nameCount)
                        {
                            result = "missing non-option argument(s)";
                            return ReturnCode.Error;
                        }
                    }
                    else
                    {
                        return code;
                    }
                }
            }
            else
            {
                options = null;
                argumentIndex = nameCount;
            }

            ///////////////////////////////////////////////////////////////////

            BindingFlags bindingFlags;
            MarshalFlags marshalFlags;
            ReorderFlags reorderFlags;
            ByRefArgumentFlags byRefArgumentFlags;
            TypeList methodTypes;
            TypeList parameterTypes;
            MarshalFlagsList parameterMarshalFlags;
            int limit;
            int index;
            bool noByRef;
            bool strictMember;
            bool strictArgs;
            bool invoke;
            bool help;
            bool noArgs;
            bool arrayAsValue;
            bool arrayAsLink;
            bool debug;
            bool trace;

            ProcessFindMethodsAndFixupArgumentsOptions(
                interpreter, options, ObjectOptionType.Call, null,
                null, null, null, out bindingFlags, out marshalFlags,
                out reorderFlags, out byRefArgumentFlags, out methodTypes,
                out parameterTypes, out parameterMarshalFlags, out limit,
                out index, out noByRef, out strictMember, out strictArgs,
                out invoke, out help, out noArgs, out arrayAsValue,
                out arrayAsLink, out debug, out trace);

            ///////////////////////////////////////////////////////////////////

            Type returnType;
            ObjectFlags objectFlags;
            ObjectFlags byRefObjectFlags;
            string objectName;
            string interpName;
            bool create;
            bool dispose;
            bool alias;
            bool aliasRaw;
            bool aliasAll;
            bool aliasReference;
            bool toString;

            ProcessFixupReturnValueOptions(
                options, null, null, out returnType, out objectFlags,
                out byRefObjectFlags, out objectName, out interpName,
                out create, out dispose, out alias, out aliasRaw,
                out aliasAll, out aliasReference, out toString);

            ///////////////////////////////////////////////////////////////////
            //                    METHOD ARGUMENT BUILDING
            ///////////////////////////////////////////////////////////////////

            object[] args = null;

            if ((argumentIndex != Index.Invalid) &&
                (argumentIndex < argumentCount))
            {
                //
                // NOTE: How many arguments were supplied?
                //
                int newArgumentCount = (argumentCount - argumentIndex);

                //
                // NOTE: Create and populate the array of
                //       arguments for the invocation.
                //
                args = new object[newArgumentCount];

                for (int newArgumentIndex = argumentIndex;
                        newArgumentIndex < argumentCount;
                        newArgumentIndex++)
                {
                    /* need String, not Argument */
                    args[newArgumentIndex - argumentIndex] =
                        arguments[newArgumentIndex].String;
                }
            }
            else if (invoke || help || !noArgs)
            {
                //
                // FIXME: When no arguments are specified,
                //        we actually need an array of zero
                //        arguments for the parameter to
                //        argument matching code to work
                //        correctly.
                //
                args = new object[0];
            }

            //
            // HACK: We want to use the existing marshalling
            //       code; therefore, we pre-bake some of
            //       the required arguments here (i.e. since
            //       we KNOW what method we are going to call,
            //       however we want magical bi-directional
            //       type coercion, etc).
            //
            Type delegateType = null;
            string newObjectName = null;
            string newMemberName = null;
            MethodInfo[] methodInfos = null;

            if (!GetMethodsFromDelegates(
                    delegates, ref delegateType, ref newObjectName,
                    ref newMemberName, ref methodInfos, ref result))
            {
                return ReturnCode.Error;
            }

            //
            // NOTE: These checks are largely redundant [for now].
            //
            if ((methodInfos == null) || (methodInfos.Length == 0))
            {
                result = String.Format(
                    "delegate {0} has no methods matching {1}",
                    FormatOps.WrapOrNull(newObjectName), bindingFlags);

                return ReturnCode.Error;
            }

            ///////////////////////////////////////////////////////////////////
            //                    METHOD ARGUMENT CONVERSION
            ///////////////////////////////////////////////////////////////////

            IBinder binder = interpreter.InternalBinder;
            CultureInfo cultureInfo = interpreter.InternalCultureInfo;
            IntList methodIndexList = null;
            ObjectArrayList argsList = null;
            IntArgumentInfoListDictionary argumentInfoListDictionary = null;
            ResultList errors = null;

            //
            // NOTE: Attempt to convert the argument strings to something
            //       potentially more meaningful and find the corresponding
            //       method.
            //
            code = MarshalOps.FindMethodsAndFixupArguments(
                interpreter, binder, options, cultureInfo,
                delegateType, newObjectName, newObjectName,
                newMemberName, newMemberName, MemberTypes.Method,
                bindingFlags, methodInfos, methodTypes,
                parameterTypes, parameterMarshalFlags, args, limit,
                marshalFlags, ref methodIndexList, ref argsList,
                ref argumentInfoListDictionary, ref errors);

            MaybeBreakForMethodOverloadResolution(
                code, methodIndexList, errors, debug);

            if (code != ReturnCode.Ok)
            {
                result = errors;
                return code;
            }

            ///////////////////////////////////////////////////////////////////
            //                   METHOD OVERLOAD REORDERING
            ///////////////////////////////////////////////////////////////////

            if (FlagOps.HasFlags(
                    marshalFlags, MarshalFlags.ReorderMatches, true))
            {
                IntList savedMethodIndexList = new IntList(methodIndexList);

                code = MarshalOps.ReorderMethodIndexes(
                    interpreter, binder, cultureInfo, delegateType,
                    methodInfos, marshalFlags, reorderFlags,
                    ref methodIndexList, ref argsList, ref errors);

                if (code == ReturnCode.Ok)
                {
                    if (trace)
                    {
                        TraceOps.DebugTrace(String.Format(
                            "InvokeDelegate: savedMethodIndexList = {0}, " +
                            "methodIndexList = {1}", savedMethodIndexList,
                            methodIndexList), typeof(ObjectOps).Name,
                            TracePriority.MarshalDebug);
                    }
                }
                else
                {
                    result = errors;
                    return code;
                }
            }

            ///////////////////////////////////////////////////////////////////
            //                   METHOD OVERLOAD VALIDATION
            ///////////////////////////////////////////////////////////////////

            if ((methodIndexList == null) || (argsList == null))
            {
                result = String.Format(
                    "method {0} of delegate {1} not found, " +
                    "invalid index list or arguments list",
                    FormatOps.WrapOrNull(newMemberName),
                    FormatOps.WrapOrNull(newObjectName));

                return ReturnCode.Error;
            }

            int methodIndexCount = methodIndexList.Count;
            int argsCount = argsList.Count;

            if (methodIndexCount == 0 || (argsCount == 0))
            {
                result = String.Format(
                    "method {0} of delegate {1} not found, " +
                    "empty index list or arguments list",
                    FormatOps.WrapOrNull(newMemberName),
                    FormatOps.WrapOrNull(newObjectName));

                return ReturnCode.Error;
            }

            if (methodIndexCount != argsCount)
            {
                result = String.Format(
                    "method {0} of delegate {1} not found, " +
                    "mismatched index count {2} and arguments count {3}",
                    FormatOps.WrapOrNull(newMemberName),
                    FormatOps.WrapOrNull(newObjectName),
                    methodIndexCount, argsCount);

                return ReturnCode.Error;
            }

            if ((index != Index.Invalid) && ((index < 0) ||
                (index >= methodIndexCount) || (index >= argsCount)))
            {
                result = String.Format(
                    "method {0} of delegate {1} not found, " +
                    "invalid method index {2}, must be {3}",
                    FormatOps.WrapOrNull(newMemberName),
                    FormatOps.WrapOrNull(newObjectName), index,
                    FormatOps.BetweenOrExact(0, methodIndexCount - 1));

                return ReturnCode.Error;
            }

            ///////////////////////////////////////////////////////////////////
            //                      OPTION TYPE SELECTION
            ///////////////////////////////////////////////////////////////////

            //
            // NOTE: Figure out which type of options are needed for created
            //       aliases.
            //
            ObjectOptionType objectOptionType = ObjectOptionType.Delegate |
                GetOptionType(aliasRaw, aliasAll);

            //
            // NOTE: Are we actually going to invoke the method or are we simply
            //       returning the list of matching method overloads?  For this
            //       method, the list of method overloads should always have
            //       exactly one result (i.e. it is somewhat redundant; however,
            //       it is designed to match the semantics of [object invoke]).
            //
            if (invoke && !help)
            {
                ///////////////////////////////////////////////////////////////
                //                  METHOD OVERLOAD SELECTION
                ///////////////////////////////////////////////////////////////

                if (strictMember && (methodIndexCount != 1))
                {
                    result = String.Format(
                        "matched {0} method overloads of {1} on delegate " +
                        "{2}, need exactly 1", methodIndexCount,
                        FormatOps.WrapOrNull(newMemberName),
                        FormatOps.WrapOrNull(newObjectName));

                    return ReturnCode.Error;
                }

                //
                // FIXME: Select the first method that matches.  More
                //        sophisticated logic may need to be added here later.
                //
                int methodIndex = (index != Index.Invalid) ?
                    methodIndexList[index] : methodIndexList[0];

                if (methodIndex == Index.Invalid)
                {
                    result = String.Format(
                        "method {0} of delegate {1} not found",
                        FormatOps.WrapOrNull(newMemberName),
                        FormatOps.WrapOrNull(newObjectName));

                    return ReturnCode.Error;
                }

                ///////////////////////////////////////////////////////////////
                //                  METHOD DELEGATE SELECTION
                ///////////////////////////////////////////////////////////////

                Delegate localDelegate = delegates[methodIndex].Y;

                ///////////////////////////////////////////////////////////////
                //               METHOD ARGUMENT ARRAY SELECTION
                ///////////////////////////////////////////////////////////////

                //
                // NOTE: Get the arguments we are going to use to perform
                //       the actual method call.
                //
                args = (index != Index.Invalid) ? argsList[index] : argsList[0];

                //
                // NOTE: Lookup the output argument list associated with the
                //       method to be invoked.  This may end up being null.
                //       In that case, no output argument handling will be
                //       done.
                //
                ArgumentInfoList argumentInfoList;

                /* IGNORED */
                MarshalOps.TryGetArgumentInfoList(argumentInfoListDictionary,
                    methodIndex, out argumentInfoList);

                ///////////////////////////////////////////////////////////////
                //                       METHOD TRACING
                ///////////////////////////////////////////////////////////////

                if (trace)
                {
                    TraceOps.DebugTrace(String.Format(
                        "InvokeDelegate: methodIndex = {0}, delegate = {1}, " +
                        "args = {2}, argumentInfoList = {3}", methodIndex,
                        FormatOps.WrapOrNull(localDelegate),
                        FormatOps.WrapOrNull(new StringList(args)),
                        FormatOps.WrapOrNull(argumentInfoList)),
                        typeof(ObjectOps).Name, TracePriority.MarshalDebug);
                }

                ///////////////////////////////////////////////////////////////
                //                      METHOD INVOCATION
                ///////////////////////////////////////////////////////////////

                object returnValue = null;

                code = Engine.ExecuteDelegate(
                    localDelegate, args, ref returnValue, ref result);

                @delegate = localDelegate;

                ///////////////////////////////////////////////////////////////
                //                   BYREF ARGUMENT HANDLING
                ///////////////////////////////////////////////////////////////

                if ((code == ReturnCode.Ok) &&
                    !noByRef && (argumentInfoList != null))
                {
                    code = MarshalOps.FixupByRefArguments(
                        interpreter, binder, cultureInfo, argumentInfoList,
                        objectFlags | byRefObjectFlags, options,
                        allowOptions ?
                            GetInvokeOptions(objectOptionType) : null,
                        objectOptionType, interpName, args, marshalFlags,
                        byRefArgumentFlags, strictArgs, create, dispose,
                        alias, aliasReference, toString, arrayAsValue,
                        arrayAsLink, ref result);
                }

                ///////////////////////////////////////////////////////////////
                //                    RETURN VALUE HANDLING
                ///////////////////////////////////////////////////////////////

                if (code == ReturnCode.Ok)
                {
                    code = MarshalOps.FixupReturnValue(
                        interpreter, binder, cultureInfo, returnType,
                        objectFlags, options, allowOptions ?
                            GetInvokeOptions(objectOptionType) : null,
                        objectOptionType, objectName, interpName,
                        returnValue, create, dispose, alias,
                        aliasReference, toString, ref result);
                }
            }
            else
            {
                ///////////////////////////////////////////////////////////////
                //                 METHOD OVERLOAD DIAGNOSTICS
                ///////////////////////////////////////////////////////////////

                MethodInfoList methodInfoList = new MethodInfoList();

                if (index != Index.Invalid)
                {
                    methodInfoList.Add(methodInfos[methodIndexList[index]]);
                }
                else
                {
                    foreach (int methodIndex in methodIndexList)
                        methodInfoList.Add(methodInfos[methodIndex]);
                }

                ///////////////////////////////////////////////////////////////
                //                    RETURN VALUE HANDLING
                ///////////////////////////////////////////////////////////////

                if (help)
                {
#if SHELL && INTERACTIVE_COMMANDS && XML
                    code = HelpOps.GetMemberHelp(
                        interpreter, methodInfoList, false, ref result);
#else
                    result = "not implemented";
                    code = ReturnCode.Error;
#endif
                }
                else
                {
                    code = MarshalOps.FixupReturnValue(
                        interpreter, binder, cultureInfo, returnType,
                        objectFlags, options, allowOptions ?
                            GetInvokeOptions(objectOptionType) : null,
                        objectOptionType, objectName, interpName,
                        methodInfoList, create, dispose, alias,
                        aliasReference, toString, ref result);
                }
            }

            return code;
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Object Disposal Support Methods
        /// <summary>
        /// This method returns the fields of the specified type whose names
        /// match any of the specified patterns, using the specified binding
        /// flags and match mode.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter context used for pattern matching, if any.
        /// </param>
        /// <param name="type">
        /// The type whose fields are to be examined.
        /// </param>
        /// <param name="bindingFlags">
        /// The binding flags used to enumerate the fields of the type.
        /// </param>
        /// <param name="mode">
        /// The match mode used to compare field names against the patterns.
        /// </param>
        /// <param name="patterns">
        /// The patterns used to match the field names.
        /// </param>
        /// <param name="noCase">
        /// Non-zero to perform case-insensitive matching.
        /// </param>
        /// <returns>
        /// The matching fields, or null if the type or patterns are invalid.
        /// </returns>
        private static IEnumerable<FieldInfo> GetDisposedFieldInfos(
            Interpreter interpreter,      /* in: OPTIONAL */
            Type type,                    /* in */
            BindingFlags bindingFlags,    /* in */
            MatchMode mode,               /* in */
            IEnumerable<string> patterns, /* in */
            bool noCase                   /* in */
            )
        {
            if ((type == null) || (patterns == null))
                return null;

            IList<FieldInfo> allFieldInfos = type.GetFields(bindingFlags);

            if (allFieldInfos == null)
                return null;

            IList<FieldInfo> matchFieldInfos = new List<FieldInfo>();

            foreach (string pattern in patterns)
            {
                if (pattern == null)
                    continue;

                foreach (FieldInfo fieldInfo in allFieldInfos)
                {
                    if (fieldInfo == null)
                        continue;

                    if (StringOps.Match(
                            interpreter, mode, fieldInfo.Name,
                            pattern, noCase))
                    {
                        matchFieldInfos.Add(fieldInfo);
                    }
                }
            }

            return matchFieldInfos;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method attempts to determine whether the specified object has
        /// been disposed by examining its known disposal-indicating fields and
        /// properties and, optionally, by forcing a benign member access.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter context used for pattern matching, if any.
        /// </param>
        /// <param name="object">
        /// The object whose disposal state is to be checked.
        /// </param>
        /// <param name="force">
        /// Non-zero to force a benign member access in order to detect disposal.
        /// </param>
        /// <param name="cannotCheck">
        /// The value to return when the disposal state cannot be determined.
        /// </param>
        /// <param name="caughtException">
        /// The value to return when an exception (other than
        /// <see cref="ObjectDisposedException" />) is caught while checking the
        /// disposal state.
        /// </param>
        /// <returns>
        /// True if the object appears to be disposed, false if it does not, or
        /// the supplied fallback value when the state cannot be determined.
        /// </returns>
        public static bool? IsDisposed(
            Interpreter interpreter, /* in: OPTIONAL */
            object @object,          /* in */
            bool force,              /* in */
            bool? cannotCheck,       /* in */
            bool? caughtException    /* in */
            )
        {
            if (@object == null)
                return cannotCheck;

            if (IsDisposed(@object))
                return true;

            Type type = AppDomainOps.MaybeGetTypeOrNull(@object);

            if (type == null)
                return cannotCheck;

            string[] fieldNames = DisposedFieldNames;

            if (fieldNames != null)
            {
                try
                {
                    BindingFlags bindingFlags = GetBindingFlags(
                        MetaBindingFlags.DisposedField, false);

                    IEnumerable<FieldInfo> fieldInfos = GetDisposedFieldInfos(
                        interpreter, type, bindingFlags, IsDisposedPatternMode,
                        fieldNames, IsDisposedPatterNoCase);

                    if (fieldInfos != null)
                    {
                        foreach (FieldInfo fieldInfo in fieldInfos)
                        {
                            if (fieldInfo == null)
                                continue;

                            if (fieldInfo.FieldType != typeof(bool))
                                continue;

                            if ((bool)fieldInfo.GetValue(@object))
                                return true;
                        }
                    }
                }
                catch (ObjectDisposedException)
                {
                    return true;
                }
                catch
                {
                    return caughtException;
                }
            }

            string[] propertyNames = DisposedPropertyNames;

            if (propertyNames != null)
            {
                try
                {
                    BindingFlags bindingFlags = GetBindingFlags(
                        MetaBindingFlags.DisposedProperty, false);

                    foreach (string propertyName in propertyNames)
                    {
                        if (propertyName == null)
                            continue;

                        PropertyInfo propertyInfo = type.GetProperty(
                            propertyName, bindingFlags);

                        if (propertyInfo == null)
                            continue;

                        if (propertyInfo.PropertyType != typeof(bool))
                            continue;

                        if ((bool)propertyInfo.GetValue(@object, null))
                            return true;
                    }
                }
                catch (ObjectDisposedException)
                {
                    return true;
                }
                catch
                {
                    return caughtException;
                }
            }

            if (force)
            {
                try
                {
                    /* IGNORED */
                    @object.ToString(); /* Baaaaaang? */
                }
                catch (ObjectDisposedException)
                {
                    return true;
                }
                catch
                {
                    return caughtException;
                }
            }

            return false;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method determines whether the specified object reports itself as
        /// disposed via the <see cref="IMaybeDisposed" /> interface.  Transparent
        /// proxies and objects that do not implement that interface are assumed
        /// to be not disposed.
        /// </summary>
        /// <param name="object">
        /// The object whose disposal state is to be checked.
        /// </param>
        /// <returns>
        /// True if the object reports itself as disposed; otherwise, false.
        /// </returns>
        public static bool IsDisposed(
            object @object /* in */
            )
        {
            //
            // BUGBUG: Apparently, we cannot simply cast just any old
            //         transparent proxy to IMaybeDisposed and attempt
            //         to use it.  Therefore, avoid doing that here.
            //
            if (AppDomainOps.IsTransparentProxy(@object))
                return false; /* WRONG: Remote proxy, assume false. */

            IMaybeDisposed maybeDisposed = @object as IMaybeDisposed;

            if (maybeDisposed == null)
                return false; /* WRONG: Not queryable, assume false. */

            return maybeDisposed.Disposed;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method disposes the specified object and complains (via the
        /// debugging subsystem) if disposal fails.
        /// </summary>
        /// <typeparam name="T">
        /// The type of the object to be disposed.
        /// </typeparam>
        /// <param name="interpreter">
        /// The interpreter context used when complaining about a disposal
        /// failure, if any.
        /// </param>
        /// <param name="object">
        /// The object to be disposed.  Upon return, it is reset to its default
        /// value.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise, an appropriate
        /// error code.
        /// </returns>
        public static ReturnCode DisposeOrComplain<T>(
            Interpreter interpreter, /* in */
            ref T @object            /* in, out */
            )
        {
            ReturnCode code;
            Result error = null;

            code = Dispose(ref @object, ref error);

            if (code != ReturnCode.Ok)
                DebugOps.Complain(interpreter, code, error);

            return code;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method disposes the specified object and emits a diagnostic
        /// trace message if disposal fails.
        /// </summary>
        /// <typeparam name="T">
        /// The type of the object to be disposed.
        /// </typeparam>
        /// <param name="interpreter">
        /// The interpreter context associated with the operation, if any.
        /// </param>
        /// <param name="object">
        /// The object to be disposed.  Upon return, it is reset to its default
        /// value.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise, an appropriate
        /// error code.
        /// </returns>
        public static ReturnCode DisposeOrTrace<T>(
            Interpreter interpreter, /* in: NOT USED */
            ref T @object            /* in, out */
            )
        {
            ReturnCode code;
            Result error = null;

            code = Dispose(ref @object, ref error);

            if (code != ReturnCode.Ok)
            {
                TraceOps.DebugTrace(String.Format(
                    "DisposeOrTrace: code = {0}, error = {1}",
                    FormatOps.WrapOrNull(code),
                    FormatOps.WrapOrNull(error)),
                    typeof(ObjectOps).Name,
                    TracePriority.CleanupError);
            }

            return code;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method disposes the specified object if it implements
        /// <see cref="IDisposable" />, always resetting it to its default value
        /// afterward.
        /// </summary>
        /// <typeparam name="T">
        /// The type of the object to be disposed.
        /// </typeparam>
        /// <param name="object">
        /// The object to be disposed.  Upon return, it is reset to its default
        /// value.
        /// </param>
        /// <param name="error">
        /// Upon failure, receives information about the error.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise, an appropriate
        /// error code.
        /// </returns>
        private static ReturnCode Dispose<T>(
            ref T @object,   /* in, out */
            ref Result error /* out */
            )
        {
            try
            {
                IDisposable disposable = null;

                try
                {
                    disposable = @object as IDisposable;

                    if (disposable != null)
                    {
                        try
                        {
                            disposable.Dispose(); /* throw */
                        }
                        catch (Exception e)
                        {
                            error = e;
                            return ReturnCode.Error;
                        }
                    }
                }
                finally
                {
                    disposable = null; /* REDUNDANT? */
                }
            }
            finally
            {
                @object = default(T); /* REDUNDANT? */
            }

            return ReturnCode.Ok;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method attempts to dispose the specified object (skipping
        /// objects that are already disposed or not disposable) and complains
        /// (via the debugging subsystem) if disposal fails.
        /// </summary>
        /// <typeparam name="T">
        /// The type of the object to be disposed.
        /// </typeparam>
        /// <param name="interpreter">
        /// The interpreter context used when complaining about a disposal
        /// failure, if any.
        /// </param>
        /// <param name="object">
        /// The object to be disposed.  Upon return, it is reset to its default
        /// value.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise, an appropriate
        /// error code.
        /// </returns>
        public static ReturnCode TryDisposeOrComplain<T>(
            Interpreter interpreter, /* in */
            ref T @object            /* in, out */
            )
        {
            ReturnCode code;
            Result error = null;

            code = TryDispose<T>(ref @object, ref error);

            if (code != ReturnCode.Ok)
                DebugOps.Complain(interpreter, code, error);

            return code;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method attempts to dispose the specified object (skipping
        /// objects that are already disposed or not disposable) and emits a
        /// diagnostic trace message if disposal fails.
        /// </summary>
        /// <typeparam name="T">
        /// The type of the object to be disposed.
        /// </typeparam>
        /// <param name="object">
        /// The object to be disposed.  Upon return, it is reset to its default
        /// value.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise, an appropriate
        /// error code.
        /// </returns>
        public static ReturnCode TryDisposeOrTrace<T>(
            ref T @object /* in, out */
            )
        {
            ReturnCode code;
            Result error = null;

            code = TryDispose<T>(ref @object, ref error);

            if (code != ReturnCode.Ok)
            {
                TraceOps.DebugTrace(String.Format(
                    "TryDisposeOrTrace: code = {0}, error = {1}",
                    FormatOps.WrapOrNull(code),
                    FormatOps.WrapOrNull(error)),
                    typeof(ObjectOps).Name,
                    TracePriority.CleanupError);
            }

            return code;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method attempts to dispose the specified object using the
        /// default disposal behavior.  This convenience overload forwards to the
        /// most general overload.
        /// </summary>
        /// <typeparam name="T">
        /// The type of the object to be disposed.
        /// </typeparam>
        /// <param name="object">
        /// The object to be disposed.  Upon return, it is reset to its default
        /// value.
        /// </param>
        /// <param name="error">
        /// Upon failure, receives information about the error.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise, an appropriate
        /// error code.
        /// </returns>
        public static ReturnCode TryDispose<T>(
            ref T @object,   /* in */
            ref Result error /* out */
            )
        {
            bool dispose = DefaultDispose;

            return TryDispose<T>(ref @object, ref dispose, ref error);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method attempts to dispose the specified object, honoring the
        /// supplied disposal flag.  This convenience overload forwards to the
        /// most general overload.
        /// </summary>
        /// <typeparam name="T">
        /// The type of the object to be disposed.
        /// </typeparam>
        /// <param name="object">
        /// The object to be disposed.  Upon return, it is reset to its default
        /// value.
        /// </param>
        /// <param name="dispose">
        /// On input, non-zero to permit disposal; upon return, indicates whether
        /// the object was actually disposed.
        /// </param>
        /// <param name="error">
        /// Upon failure, receives information about the error.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise, an appropriate
        /// error code.
        /// </returns>
        public static ReturnCode TryDispose<T>(
            ref T @object,    /* in */
            ref bool dispose, /* in, out */
            ref Result error  /* out */
            )
        {
            Exception exception = null;

            return TryDispose<T>(
                ref @object, ref dispose, ref error, ref exception);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method attempts to dispose the specified object, honoring the
        /// supplied disposal flag and capturing any exception that occurs.
        /// Objects that are invalid, already disposed, or not disposable are
        /// skipped.  This is the most general overload to which the others
        /// forward.
        /// </summary>
        /// <typeparam name="T">
        /// The type of the object to be disposed.
        /// </typeparam>
        /// <param name="object">
        /// The object to be disposed.  Upon return, it is reset to its default
        /// value.
        /// </param>
        /// <param name="dispose">
        /// On input, non-zero to permit disposal; upon return, indicates whether
        /// the object was actually disposed.
        /// </param>
        /// <param name="error">
        /// Upon failure, receives information about the error.
        /// </param>
        /// <param name="exception">
        /// Upon failure, receives the exception that was caught, if any.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise, an appropriate
        /// error code.
        /// </returns>
        public static ReturnCode TryDispose<T>(
            ref T @object,          /* in */
            ref bool dispose,       /* in, out: No, not really. */
            ref Result error,       /* out */
            ref Exception exception /* out */
            )
        {
            if (@object == null)
            {
                @object = default(T); /* REDUNDANT */
                dispose = false; /* invalid object */

                return ReturnCode.Ok;
            }

            if (IsDisposed(@object))
            {
                @object = default(T);
                dispose = false; /* already disposed */

                return ReturnCode.Ok;
            }

            IDisposable disposable = null;

            try
            {
                disposable = @object as IDisposable;

                if (disposable == null)
                {
                    @object = default(T);
                    dispose = false; /* not disposable */

                    return ReturnCode.Ok; /* success */
                }

                disposable.Dispose(); /* throw */

                @object = default(T);
                dispose = true; /* disposed */

                return ReturnCode.Ok; /* success */
            }
            catch (Exception e)
            {
                //
                // NOTE: Apparently, the object threw an exception
                //       during its disposal.  This is technically
                //       allowed; however, it is typically highly
                //       discouraged.  Save this information and
                //       report it to our caller.
                //
                error = e;
                exception = e;

                return ReturnCode.Error; /* failure */
            }
            finally
            {
                disposable = null;
            }
        }
        #endregion
    }
}
