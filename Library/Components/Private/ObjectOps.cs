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

#if NET_STANDARD_21
using Index = Eagle._Constants.Index;
#endif

namespace Eagle._Components.Private
{
    [ObjectId("21953933-d364-453c-b848-01e348a8f8ac")]
    internal static class ObjectOps
    {
        #region Private Constants
        #region Dead Code
#if DEAD_CODE
        //
        // HACK: These are purposely not read-only.
        //
        private static string[] DisposedFieldNames;
        private static string[] DisposedPropertyNames;
#endif
        #endregion

        ///////////////////////////////////////////////////////////////////////

        //
        // NOTE: *WARNING* Changes to this assembly name are considered
        //        to be a "breaking change".
        //
        // HACK: This is purposely not read-only.
        //
        private static string clrSimpleName;

        ///////////////////////////////////////////////////////////////////////

        //
        // NOTE: *WARNING* Changes to this assembly name are considered
        //        to be a "breaking change".
        //
        // HACK: This is purposely not read-only.
        //
        private static string eagleSimpleName;

        ///////////////////////////////////////////////////////////////////////

        //
        // NOTE: *WARNING* Changes to this namespace name are considered
        //        to be a "breaking change".
        //
        // HACK: This is purposely not read-only.
        //
        private static string guruNamespace;

        ///////////////////////////////////////////////////////////////////////

        //
        // HACK: This is purposely not read-only.
        //
        private static string[] DefaultClrNamespaces;

        ///////////////////////////////////////////////////////////////////////

        //
        // HACK: This is purposely not read-only.
        //
        private static string[] DefaultEagleNamespaces;

        ///////////////////////////////////////////////////////////////////////

        //
        // HACK: This is purposely not read-only.
        //
        private static MemberTypes[] metaMemberTypesMappings;

        ///////////////////////////////////////////////////////////////////////

        //
        // HACK: This is purposely not read-only.
        //
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
        private static CommandType DefaultCommandType = CommandType.Text;
        private static CommandBehavior DefaultCommandBehavior = CommandBehavior.Default;
        private static DbExecuteType DefaultExecuteType = DbExecuteType.Default;
        private static DbResultFormat DefaultResultFormat = DbResultFormat.Default;
        private static ValueFlags DefaultValueFlags = ValueFlags.AnyNonCharacter;
#endif
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region DateTime Format, Kind, and NTP Servers
        private static string DefaultDateTimeFormat = null;

        ///////////////////////////////////////////////////////////////////////

#if DATA
        //
        // HACK: This is the default for [sql execute].
        //
        private static DateTimeBehavior DefaultDateTimeBehavior =
            DateTimeBehavior.Default;
#endif

        ///////////////////////////////////////////////////////////////////////

        //
        // HACK: Default to "unspecified" for DateTime values.  Perhaps this
        //       should be "UTC" instead?
        //
        private static DateTimeKind DefaultDateTimeKind =
            DateTimeKind.Unspecified;

        ///////////////////////////////////////////////////////////////////////

        private static DateTimeStyles DefaultDateTimeStyles =
            DateTimeStyles.None;

        ///////////////////////////////////////////////////////////////////////

        //
        // HACK: This is purposely not read-only.
        //
        private static IEnumerable<string> DefaultTimeServers = null;
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Pattern-Related Flags
        private static MatchMode DefaultMatchMode = MatchMode.None;
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Object Creation
        //
        // NOTE: The default behavior for the -create / -nocreate options
        //       is controlled by these fields.
        //
        private static bool DefaultCreate = false;
        private static bool DefaultNoCreate = true;
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Reflection-Related Flags
        //
        // NOTE: This controls the default member types for all sub-command
        //       options; therefore, it cannot be (easily) kept directly in
        //       the meta member types map.
        //
        private static MemberTypes DefaultMemberTypes; /* EXEMPT */

        ///////////////////////////////////////////////////////////////////////

        //
        // NOTE: This controls the default binding flags for all sub-commands
        //       options; therefore, it cannot be (easily) kept directly in
        //       the meta binding flags map.
        //
        private static BindingFlags DefaultBindingFlags; /* EXEMPT */
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Marshal-Related Flags
        private static LoadType DefaultLoadType = LoadType.Default;

        ///////////////////////////////////////////////////////////////////////

        private static MarshalFlags DefaultMarshalFlags =
            MarshalFlags.Default;

        ///////////////////////////////////////////////////////////////////////

        private static MarshalFlags DefaultParameterMarshalFlags =
            MarshalFlags.None;

        ///////////////////////////////////////////////////////////////////////

        private static ReorderFlags DefaultReorderFlags =
            ReorderFlags.Default;

        ///////////////////////////////////////////////////////////////////////

        private static ByRefArgumentFlags DefaultByRefArgumentFlags =
            ByRefArgumentFlags.None;

        ///////////////////////////////////////////////////////////////////////

        private static ObjectFlags DefaultObjectFlags =
            ObjectFlags.Default;

        ///////////////////////////////////////////////////////////////////////

        private static ObjectFlags DefaultByRefObjectFlags =
            ObjectFlags.None;

        ///////////////////////////////////////////////////////////////////////

        private static CallbackFlags DefaultCallbackFlags =
            CallbackFlags.Default;

        ///////////////////////////////////////////////////////////////////////

        private static ObjectOptionType DefaultObjectOptionType =
            ObjectOptionType.Default;

        ///////////////////////////////////////////////////////////////////////

        private static ValueFlags DefaultObjectValueFlags = ValueFlags.None;

        ///////////////////////////////////////////////////////////////////////

        private static ValueFlags DefaultMemberValueFlags = ValueFlags.None;
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Object Disposal
        //
        // NOTE: Non-zero means an object should be disposed prior to it
        //       being removed fro the interpreter.
        //
        private static bool DefaultDispose = true;
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region GC Settings
        //
        // NOTE: The default behavior was to run garbage collection after
        //       removing a managed object from the interpreter; however,
        //       that did have negative performance implications.
        //
        private static bool DefaultSynchronous = false;

        ///////////////////////////////////////////////////////////////////////

        //
        // NOTE: The default behavior is not to wait for pending finalizers
        //       to finish.
        //
        private static bool DefaultWaitForGC = false;

        ///////////////////////////////////////////////////////////////////////

#if NET_451 || NET_452 || NET_46 || NET_461 || NET_462 || NET_47 || NET_471 || NET_472 || NET_48 || NET_481 || NET_STANDARD_20
        //
        // NOTE: The default behavior is not to compact the large object
        //       heap; however, compacting it can be useful if many large
        //       objects are being created and finalized.
        //
        private static bool DefaultCompactLargeObjectHeap = false;
#endif
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Option Flags
        //
        // NOTE: Any changes to these default option flag values will be
        //       library-wide.
        //
        private static OptionFlags AliasOptionFlags = OptionFlags.None;
        private static OptionFlags CreateOptionFlags = OptionFlags.None;
        private static OptionFlags NoCreateOptionFlags = OptionFlags.None;
        private static OptionFlags NoDisposeOptionFlags = OptionFlags.None;
        private static OptionFlags SynchronousOptionFlags = OptionFlags.None;
        private static OptionFlags DebugOptionFlags = OptionFlags.None;
        private static OptionFlags TraceOptionFlags = OptionFlags.None;
        private static OptionFlags VerboseOptionFlags = OptionFlags.None;
        private static OptionFlags ArrayAsLinkOptionFlags = OptionFlags.None;
        private static OptionFlags ArrayAsValueOptionFlags = OptionFlags.None;
        #endregion
        #endregion
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Public Initialization Methods
        public static void Initialize(
            bool force
            )
        {
            #region Dead Code
#if DEAD_CODE
            InitializeDisposedNames(force);
#endif
            #endregion

            ///////////////////////////////////////////////////////////////////

            InitializeNamespaces(force);
            InitializeMetaMemberTypesMappings(force);
            InitializeMetaBindingFlagsMappings(force);
            InitializeReflectionDefaults(force);
        }

        ///////////////////////////////////////////////////////////////////////

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
        #region Dead Code
#if DEAD_CODE
        private static void InitializeDisposedNames(
            bool force
            )
        {
            if (force || (DisposedFieldNames == null))
            {
                DisposedFieldNames = new string[] {
                    "disposed", "_disposed", "_isDisposed",
                    "m_disposed", "m_isDisposed", null, null, null
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
#endif
        #endregion

        ///////////////////////////////////////////////////////////////////////

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
        public static string GetDefaultDateTimeFormat()
        {
            return DefaultDateTimeFormat;
        }

        ///////////////////////////////////////////////////////////////////////

        public static DateTimeKind GetDefaultDateTimeKind()
        {
            return DefaultDateTimeKind;
        }

        ///////////////////////////////////////////////////////////////////////

        public static DateTimeStyles GetDefaultDateTimeStyles()
        {
            return DefaultDateTimeStyles;
        }

        ///////////////////////////////////////////////////////////////////////

        public static IEnumerable<string> GetDefaultTimeServers()
        {
            return DefaultTimeServers;
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Default Settings Support Methods
        public static bool GetDefaultDispose()
        {
            return DefaultDispose;
        }

        ///////////////////////////////////////////////////////////////////////

        public static bool GetDefaultSynchronous()
        {
            return DefaultSynchronous;
        }

        ///////////////////////////////////////////////////////////////////////

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

        public static MarshalFlags GetDefaultMarshalFlags()
        {
            return DefaultMarshalFlags;
        }

        ///////////////////////////////////////////////////////////////////////

        public static ObjectFlags GetDefaultObjectFlags()
        {
            return DefaultObjectFlags;
        }

        ///////////////////////////////////////////////////////////////////////

        public static ObjectOptionType GetDefaultObjectOptionType()
        {
            return DefaultObjectOptionType;
        }

        ///////////////////////////////////////////////////////////////////////

        public static ValueFlags GetDefaultObjectValueFlags()
        {
            return DefaultObjectValueFlags;
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Type Support Methods
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

        public static void CollectGarbage() /* throw */
        {
            CollectGarbage(GarbageFlags.Default);
        }

        ///////////////////////////////////////////////////////////////////////

        public static void CollectGarbage(
            GarbageFlags flags
            ) /* throw */
        {
            CollectGarbage(-1, GCCollectionMode.Default, flags);
        }

        ///////////////////////////////////////////////////////////////////////

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
        public static OptionDictionary GetCallOptions()
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
        public static OptionDictionary GetCallbackOptions()
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
        public static OptionDictionary GetCertificateOptions()
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
        public static OptionDictionary GetCleanupOptions()
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
        public static OptionDictionary GetCreateOptions()
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
        public static OptionDictionary GetDeclareOptions()
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
        public static OptionDictionary GetDequeueOptions()
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
        public static OptionDictionary GetDeserializeOptions()
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
        public static OptionDictionary GetDisposeOptions()
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
        public static OptionDictionary GetEvaluateOptions()
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
        public static OptionDictionary GetExceptionOptions()
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

#if DATA
        //
        // NOTE: This is for the [sql execute] sub-command.
        //
        public static OptionDictionary GetExecuteOnlyOptions()
        {
            return new OptionDictionary(
                new IOption[] {
                new Option(null, OptionFlags.MustHaveCultureInfoValue,
                    Index.Invalid, Index.Invalid, "-culture", null),
                new Option(null, OptionFlags.None, Index.Invalid,
                    Index.Invalid, "-verbatim", null),
                new Option(typeof(DateTimeBehavior),
                    OptionFlags.MustHaveEnumValue, Index.Invalid,
                    Index.Invalid, "-datetimebehavior",
                    new Variant(DefaultDateTimeBehavior)),
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
        public static OptionDictionary GetExecuteOptions()
        {
            return new OptionDictionary(
                GetExecuteOnlyOptions(), GetFixupReturnValueOptions());
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
        public static OptionDictionary GetFixupReturnValueOptions()
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
        public static OptionDictionary GetForEachOptions()
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
        public static OptionDictionary GetGetOptions()
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
        public static OptionDictionary GetImportOptions()
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
        public static OptionDictionary GetInvokeOnlyOptions()
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
        public static OptionDictionary GetInvokeOptions()
        {
            return new OptionDictionary(
                GetInvokeOnlyOptions(), GetInvokeSharedOptions());
        }

        ///////////////////////////////////////////////////////////////////////

        //
        // NOTE: This is for the [object invokeall] sub-command.
        //
        public static OptionDictionary GetInvokeAllOptions()
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
        public static OptionDictionary GetInvokeRawOnlyOptions()
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
        public static OptionDictionary GetInvokeRawOptions()
        {
            return new OptionDictionary(
                GetInvokeRawOnlyOptions(), GetInvokeSharedOptions());
        }

        ///////////////////////////////////////////////////////////////////////

        //
        // NOTE: This is for the [object isnull] sub-command.
        //
        public static OptionDictionary GetIsNullOptions()
        {
            return new OptionDictionary(
                new IOption[] {
                new Option(null, OptionFlags.None, Index.Invalid,
                    Index.Invalid, "-nocomplain", null),
                new Option(null, OptionFlags.MustHaveBooleanValue |
                    OptionFlags.Unsafe, Index.Invalid, Index.Invalid,
                    "-default", null),
                Option.CreateEndOfOptions()
            });
        }

        ///////////////////////////////////////////////////////////////////////

        //
        // NOTE: This is for the [object isoftype] sub-command.
        //
        public static OptionDictionary GetIsOfTypeOptions()
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
        public static OptionDictionary GetLoadOptions()
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
                    Index.Invalid, "-verifiedonly", null),
                Option.CreateEndOfOptions()
            });
        }

        ///////////////////////////////////////////////////////////////////////

        //
        // NOTE: This is for the [object members] sub-command.
        //
        public static OptionDictionary GetMembersOptions()
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
        public static OptionDictionary GetSearchOptions()
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
        public static OptionDictionary GetSerializeOptions()
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
        public static OptionDictionary GetReadOnlyOptions()
        {
            return new OptionDictionary(
                new IOption[] {
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
        public static OptionDictionary GetReadOptions()
        {
            return new OptionDictionary(
                GetReadOnlyOptions(), GetFixupReturnValueOptions());
        }

        ///////////////////////////////////////////////////////////////////////

        //
        // NOTE: This is for the ToCommandCallback method.
        //
        // NOTE: This method must use the "Unsafe" option flag to prevent a
        //       "safe" interpreter from potentially using an option.
        //
        public static OptionDictionary GetSimpleCallbackOptions()
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
        public static OptionDictionary GetTypeOptions()
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
        public static OptionDictionary GetUnaliasNamespaceOptions()
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
        public static OptionDictionary GetUndeclareOptions()
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
        public static OptionDictionary GetUnimportOptions()
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
#if DATA                                                 //
                case ObjectOptionType.Execute:           // [sql execute]
                    return GetExecuteOptions();          //
#endif                                                   //
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
        public static bool MaybeMutateBindingFlags(
            OptionDictionary options,          /* in */
            ObjectOptionType objectOptionType, /* in */
            Type objectType,                   /* in */
            int index,                         /* in */
            bool invoke,                       /* in */
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
                if ((index == Index.Invalid) && invoke &&
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
            IVariant value = null;

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
            IVariant value = null;

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

        public static void ProcessSimpleCallbackOptions(
            Interpreter interpreter,
            OptionDictionary options,
            BindingFlags? defaultBindingFlags,
            MarshalFlags? defaultMarshalFlags,
            out BindingFlags bindingFlags,
            out MarshalFlags marshalFlags
            )
        {
            IVariant value = null;

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
        public static void ProcessExecuteOptions(
            Interpreter interpreter,
            OptionDictionary options,
            CommandType? defaultCommandType,
            CommandBehavior? defaultCommandBehavior,
            DbExecuteType? defaultExecuteType,
            DbResultFormat? defaultResultFormat,
            ValueFlags? defaultValueFlags,
            DateTimeBehavior? defaultDateTimeBehavior,
            DateTimeKind? defaultDateTimeKind,
            DateTimeStyles? defaultDateTimeStyles,
            out CultureInfo cultureInfo,
            out CommandType commandType,
            out CommandBehavior commandBehavior,
            out DbExecuteType executeType,
            out DbResultFormat resultFormat,
            out ValueFlags valueFlags,
            out DateTimeBehavior dateTimeBehavior,
            out DateTimeKind dateTimeKind,
            out DateTimeStyles dateTimeStyles,
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
            IVariant value = null;

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
                out noNestedMember, out noCase, out invoke, out noArgs,
                out arrayAsValue, out arrayAsLink, out noMutateBindingFlags,
                out debug, out trace);
        }

        ///////////////////////////////////////////////////////////////////////

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
                out noNestedMember, out noCase, out invoke, out noArgs,
                out arrayAsValue, out arrayAsLink, out noMutateBindingFlags,
                out debug, out trace);
        }

        ///////////////////////////////////////////////////////////////////////

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
            out bool noArgs,
            out bool arrayAsValue,
            out bool arrayAsLink,
            out bool noMutateBindingFlags,
            out bool debug,
            out bool trace
            )
        {
            IVariant value = null;

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
                bindingFlags |= ObjectOps.GetBindingFlags(
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
            IVariant value = null;

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
            IVariant value = null;

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
            out bool noArgs,
            out bool arrayAsValue,
            out bool arrayAsLink,
            out bool trace
            )
        {
            IVariant value = null;

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
            IVariant value = null;

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

            IVariant value = null;

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
            IVariant value = null;

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
                out noNestedObject, out noCase, out invoke, out noArgs,
                out arrayAsValue, out arrayAsLink, out trace);
        }

        ///////////////////////////////////////////////////////////////////////

        public static void ProcessObjectIsNullOptions(
            OptionDictionary options,
            out bool noComplain,
            out bool @default
            )
        {
            IVariant value = null;

            ///////////////////////////////////////////////////////////////////

            noComplain = false;

            if ((options != null) && options.CheckPresent("-nocomplain"))
                noComplain = true;

            ///////////////////////////////////////////////////////////////////

            @default = false;

            if ((options != null) &&
                options.CheckPresent("-default", ref value))
            {
                @default = (bool)value.Value;
            }
        }

        ///////////////////////////////////////////////////////////////////////

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
            IVariant value = null;

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

            ///////////////////////////////////////////////////////////////////

            verifiedOnly = false;

            if ((options != null) && options.CheckPresent("-verifiedonly"))
                verifiedOnly = true;
        }

        ///////////////////////////////////////////////////////////////////////

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
            IVariant value = null;

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

        private static void ProcessReflectionOptions(
            OptionDictionary options,
            ObjectOptionType objectOptionType, /* NOT USED */
            MemberTypes? defaultMemberTypes,
            BindingFlags? defaultBindingFlags,
            out MemberTypes memberTypes,
            out BindingFlags bindingFlags
            )
        {
            IVariant value = null;

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
            IVariant value = null;

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

        public static ReturnCode InvokeDelegate(
            Interpreter interpreter,
            Delegate @delegate,
            DelegateFlags delegateFlags,
            ArgumentList arguments,
            int nameCount,
            ref Result result
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

            if (@delegate == null)
            {
                result = "invalid delegate";
                return ReturnCode.Error;
            }

            if (arguments == null)
            {
                result = "invalid arguments";
                return ReturnCode.Error;
            }

            ///////////////////////////////////////////////////////////////////
            //                        OPTION PROCESSING
            ///////////////////////////////////////////////////////////////////

            //
            // HACK: Does the caller permit the use of any options at all?
            //
            bool useCallOptions = FlagOps.HasFlags(
                delegateFlags, DelegateFlags.UseCallOptions, true);

            ///////////////////////////////////////////////////////////////////

            ReturnCode code;
            OptionDictionary options;
            int argumentIndex;

            if (useCallOptions)
            {
                options = GetCallOptions();
                argumentIndex = Index.Invalid;

                if (arguments.Count > nameCount)
                {
                    code = interpreter.GetOptions(
                        options, arguments, 0, nameCount,
                        Index.Invalid, true, ref argumentIndex,
                        ref result);

                    if (code != ReturnCode.Ok)
                        return code;
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
            int limit;
            int index;
            bool noByRef;
            bool strictMember;
            bool strictArgs;
            bool invoke;
            bool noArgs;
            bool arrayAsValue;
            bool arrayAsLink;
            bool debug;
            bool trace;

            ProcessFindMethodsAndFixupArgumentsOptions(
                interpreter, options, ObjectOptionType.Call, null, null,
                null, null, out bindingFlags, out marshalFlags,
                out reorderFlags, out byRefArgumentFlags, out limit,
                out index, out noByRef, out strictMember, out strictArgs,
                out invoke, out noArgs, out arrayAsValue, out arrayAsLink,
                out debug, out trace);

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
            int argumentCount = 0;

            if ((argumentIndex != Index.Invalid) &&
                (argumentIndex < arguments.Count))
            {
                //
                // NOTE: How many arguments were supplied?
                //
                argumentCount = (arguments.Count - argumentIndex);

                //
                // NOTE: Create and populate the array of arguments for the
                //       invocation.
                //
                args = new object[argumentCount];

                for (int index2 = argumentIndex; index2 < arguments.Count;
                        index2++)
                {
                    /* need String, not Argument */
                    args[index2 - argumentIndex] = arguments[index2].String;
                }
            }
            else if (invoke || !noArgs)
            {
                //
                // FIXME: When no arguments are specified, we actually need an
                //        array of zero arguments for the parameter to argument
                //        matching code to work correctly.
                //
                args = new object[0];
            }

            //
            // HACK: We want to use the existing marshalling code; therefore,
            //       we pre-bake some of the required arguments here (i.e. we
            //       KNOW what method we are going to call, however we want
            //       magical bi-directional type coercion, etc).
            //
            object delegateTarget = @delegate.Target;
            MethodInfo delegateMethodInfo = @delegate.Method;

            if (delegateMethodInfo == null)
            {
                result = "cannot invoke delegate, no method";
                return ReturnCode.Error;
            }

            Type delegateTargetType = AppDomainOps.MaybeGetTypeOrNull(
                delegateTarget);

            string newObjectName = (delegateTargetType != null) ?
                delegateTargetType.FullName : null;

            string newMemberName = delegateMethodInfo.Name;
            MethodInfo[] methodInfo = new MethodInfo[] { delegateMethodInfo };

            if (methodInfo == null) // NOTE: Redundant [for now].
            {
                result = String.Format(
                    "delegate \"{0}\" has no methods matching \"{1}\"",
                    newObjectName, bindingFlags);

                return ReturnCode.Error;
            }

            ///////////////////////////////////////////////////////////////////
            //                    METHOD ARGUMENT CONVERSION
            ///////////////////////////////////////////////////////////////////

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
                interpreter, interpreter.InternalBinder, options,
                interpreter.InternalCultureInfo, @delegate.GetType(),
                newObjectName, newObjectName, newMemberName,
                newMemberName, MemberTypes.Method, bindingFlags,
                methodInfo, null, null, null, args, limit,
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
                    interpreter, interpreter.InternalBinder,
                    interpreter.InternalCultureInfo, @delegate.GetType(),
                    methodInfo, marshalFlags, reorderFlags,
                    ref methodIndexList, ref argsList,
                    ref errors);

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

            if ((methodIndexList == null) || (methodIndexList.Count == 0) ||
                (argsList == null) || (argsList.Count == 0))
            {
                result = String.Format(
                    "method \"{0}\" of delegate \"{1}\" not found",
                    newMemberName, newObjectName);

                return ReturnCode.Error;
            }

            if ((index != Index.Invalid) &&
                ((index < 0) || (index >= methodIndexList.Count) ||
                (index >= argsList.Count)))
            {
                result = String.Format(
                    "method \"{0}\" of delegate \"{1}\" not found, " +
                    "invalid method index {2}, must be {3}",
                    newMemberName, newObjectName, index,
                    FormatOps.BetweenOrExact(0, methodIndexList.Count - 1));

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
            if (invoke)
            {
                ///////////////////////////////////////////////////////////////
                //                  METHOD OVERLOAD SELECTION
                ///////////////////////////////////////////////////////////////

                if (strictMember && (methodIndexList.Count != 1))
                {
                    result = String.Format(
                        "matched {0} method overloads of \"{1}\" on delegate " +
                        "\"{2}\", need exactly 1", methodIndexList.Count,
                        newMemberName, newObjectName);

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
                        "method \"{0}\" of delegate \"{1}\" not found",
                        newMemberName, newObjectName);

                    return ReturnCode.Error;
                }

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
                        FormatOps.WrapOrNull(@delegate), FormatOps.WrapOrNull(
                        new StringList(args)), FormatOps.WrapOrNull(
                        argumentInfoList)), typeof(ObjectOps).Name,
                        TracePriority.MarshalDebug);
                }

                ///////////////////////////////////////////////////////////////
                //                      METHOD INVOCATION
                ///////////////////////////////////////////////////////////////

                object returnValue = null;

                code = Engine.ExecuteDelegate(
                    @delegate, args, ref returnValue, ref result);

                ///////////////////////////////////////////////////////////////
                //                   BYREF ARGUMENT HANDLING
                ///////////////////////////////////////////////////////////////

                if ((code == ReturnCode.Ok) &&
                    !noByRef && (argumentInfoList != null))
                {
                    code = MarshalOps.FixupByRefArguments(
                        interpreter, interpreter.InternalBinder,
                        interpreter.InternalCultureInfo, argumentInfoList,
                        objectFlags | byRefObjectFlags, options,
                        useCallOptions ?
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
                        interpreter, interpreter.InternalBinder,
                        interpreter.InternalCultureInfo, returnType,
                        objectFlags, options, useCallOptions ?
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
                    methodInfoList.Add(methodInfo[methodIndexList[index]]);
                }
                else
                {
                    foreach (int methodIndex in methodIndexList)
                        methodInfoList.Add(methodInfo[methodIndex]);
                }

                ///////////////////////////////////////////////////////////////
                //                    RETURN VALUE HANDLING
                ///////////////////////////////////////////////////////////////

                code = MarshalOps.FixupReturnValue(
                    interpreter, interpreter.InternalBinder,
                    interpreter.InternalCultureInfo, returnType,
                    objectFlags, options, useCallOptions ?
                    GetInvokeOptions(objectOptionType) : null,
                    objectOptionType, objectName, interpName,
                    methodInfoList, create, dispose, alias,
                    aliasReference, toString, ref result);
            }

            return code;
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Object Disposal Support Methods
        #region Dead Code
#if DEAD_CODE
        public static bool IsDisposed( /* NOT USED */
            object @object,
            bool @default
            )
        {
            if (@object == null)
                return false;

            if (IsDisposed(@object))
                return true;

            Type type = AppDomainOps.MaybeGetTypeOrNull(@object);

            if (type == null)
                return false;

            string[] fieldNames = DisposedFieldNames;

            if (fieldNames != null)
            {
                try
                {
                    BindingFlags bindingFlags = GetBindingFlags(
                        MetaBindingFlags.DisposedField, false);

                    foreach (string fieldName in fieldNames)
                    {
                        if (fieldName == null)
                            continue;

                        FieldInfo fieldInfo = type.GetField(
                            fieldName, bindingFlags);

                        if (fieldInfo == null)
                            continue;

                        if (fieldInfo.FieldType != typeof(bool))
                            continue;

                        if ((bool)fieldInfo.GetValue(@object))
                            return true;
                    }
                }
                catch (ObjectDisposedException)
                {
                    return true;
                }
                catch
                {
                    return @default;
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
                    return @default;
                }
            }

            return false;
        }
#endif
        #endregion

        ///////////////////////////////////////////////////////////////////////

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

        public static ReturnCode TryDispose<T>(
            ref T @object,   /* in */
            ref Result error /* out */
            )
        {
            bool dispose = DefaultDispose;

            return TryDispose<T>(ref @object, ref dispose, ref error);
        }

        ///////////////////////////////////////////////////////////////////////

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
