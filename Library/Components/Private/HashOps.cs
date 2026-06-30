/*
 * HashOps.cs --
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
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using System.Reflection;
using System.Text;
using System.Threading;
using Eagle._Attributes;
using Eagle._Components.Public;
using Eagle._Containers.Public;
using Eagle._Interfaces.Public;
using SharedStringOps = Eagle._Components.Shared.StringOps;

namespace Eagle._Components.Private
{
    /// <summary>
    /// This class provides static helper methods for creating cryptographic
    /// hash algorithms and computing hashes over strings, byte arrays, and
    /// files, including support for keyed and HMAC algorithms as well as for
    /// enumerating the available algorithms.
    /// </summary>
    [ObjectId("917c6350-a09e-43e0-a03f-e1a76a4cbe2e")]
    internal static class HashOps
    {
        #region Private Algorithm / Encoding Constants
        //
        // NOTE: The "SHA" algorithm name maps to "SHA1", apparently for
        //       reasons of backward compatibility with previous versions
        //       of the .NET Framework.
        //
        /// <summary>
        /// The algorithm name "SHA", which maps to "SHA1" for backward
        /// compatibility.
        /// </summary>
        private static readonly string ShaAlgorithmName = "SHA";

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// The algorithm name for the SHA1 hash algorithm.
        /// </summary>
        private static readonly string Sha1AlgorithmName = "SHA1";

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// The algorithm name for the SHA512 hash algorithm.
        /// </summary>
        private static readonly string Sha512AlgorithmName = "SHA512";

        ///////////////////////////////////////////////////////////////////////

        //
        // NOTE: *WARNING* Change this value with great care because it may
        //       break external components.
        //
        /// <summary>
        /// The default hash algorithm name used when creating a hash algorithm
        /// and none has been specified.
        /// </summary>
        private static readonly string DefaultCreateAlgorithmName =
            Sha1AlgorithmName;

        ///////////////////////////////////////////////////////////////////////

        //
        // NOTE: *WARNING* Change this value with great care because it may
        //       break custom script, file, and stream policies that rely on
        //       the hash result.
        //
        /// <summary>
        /// The legacy hash algorithm name used when hashing binary data.
        /// </summary>
        private static readonly string LegacyBytesAlgorithmName =
            Sha1AlgorithmName;

        /// <summary>
        /// The modern hash algorithm name used when hashing binary data.
        /// </summary>
        public static readonly string ModernBytesAlgorithmName =
            Sha512AlgorithmName;

        ///////////////////////////////////////////////////////////////////////

        //
        // NOTE: *WARNING* Change this value with great care because it may
        //       break snippet usage (e.g. by Harpy) that rely on the hash
        //       result.
        //
        /// <summary>
        /// The legacy hash algorithm name used when hashing script snippets.
        /// </summary>
        private static readonly string LegacySnippetAlgorithmName =
            Sha1AlgorithmName;

        /// <summary>
        /// The modern hash algorithm name used when hashing script snippets.
        /// </summary>
        private static readonly string ModernSnippetAlgorithmName =
            Sha512AlgorithmName;

        ///////////////////////////////////////////////////////////////////////

        //
        // NOTE: *WARNING* These are subject to change in the future (e.g. to
        //       more secure variants, etc).
        //
        /// <summary>
        /// The modern hash algorithm name used when hashing text.
        /// </summary>
        private static readonly string ModernStringAlgorithmName = "SHA512";

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// The default character encoding name used when converting strings to
        /// bytes prior to hashing.
        /// </summary>
        private static readonly string DefaultEncodingName = "utf-8";
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Private Data
        /// <summary>
        /// The object used to synchronize access to the static data of this
        /// class.
        /// </summary>
        private static readonly object syncRoot = new object();

        ///////////////////////////////////////////////////////////////////////

        //
        // HACK: This is purposely not read-only.
        //
        /// <summary>
        /// When non-zero, hash algorithm instances are created via the Eagle
        /// factory mechanism rather than the framework algorithm creation
        /// methods.
        /// </summary>
        private static int useFactories = CommonOps.Runtime.IsDotNetCore() ?
            1 : 0;

        ///////////////////////////////////////////////////////////////////////

        //
        // HACK: These are purposely not read-only.
        //
        /// <summary>
        /// The list of default (framework-discovered) hash algorithm names.
        /// </summary>
        private static StringList defaultAlgorithmNames;

        /// <summary>
        /// The list of keyed hash algorithm names.
        /// </summary>
        private static StringList keyedAlgorithmNames;

        /// <summary>
        /// The list of HMAC algorithm names.
        /// </summary>
        private static StringList macAlgorithmNames;

        /// <summary>
        /// The list of normal (non-keyed) hash algorithm names.
        /// </summary>
        private static StringList normalAlgorithmNames;

        ///////////////////////////////////////////////////////////////////////

        //
        // HACK: This is purposely not read-only.
        //
        /// <summary>
        /// The cached reflection member used to enumerate the hash algorithms
        /// registered with the framework.
        /// </summary>
        private static MemberInfo memberInfo;
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Introspection Support Methods
        //
        // NOTE: Used by the _Hosts.Default.BuildEngineInfoList method.
        //
        /// <summary>
        /// This method adds diagnostic information about the configured hash
        /// algorithms to the specified list.
        /// </summary>
        /// <param name="list">
        /// The list to which the diagnostic information should be added.  If
        /// this value is null, this method does nothing.
        /// </param>
        /// <param name="detailFlags">
        /// The flags used to control how much detail is included.
        /// </param>
        public static void AddInfo(
            StringPairList list,    /* in, out */
            DetailFlags detailFlags /* in */
            )
        {
            if (list == null)
                return;

            lock (syncRoot) /* TRANSACTIONAL */
            {
                bool empty = HostOps.HasEmptyContent(detailFlags);
                StringPairList localList = new StringPairList();

                if (empty || (useFactories != 0))
                    localList.Add("UseFactories", useFactories.ToString());

                if (empty || ((defaultAlgorithmNames != null) &&
                    (defaultAlgorithmNames.Count > 0)))
                {
                    localList.Add("DefaultAlgorithmNames",
                        (defaultAlgorithmNames != null) ?
                            defaultAlgorithmNames.Count.ToString() :
                            FormatOps.DisplayNull);
                }

                if (empty || ((keyedAlgorithmNames != null) &&
                    (keyedAlgorithmNames.Count > 0)))
                {
                    localList.Add("KeyedAlgorithmNames",
                        (keyedAlgorithmNames != null) ?
                            keyedAlgorithmNames.Count.ToString() :
                            FormatOps.DisplayNull);
                }

                if (empty || ((macAlgorithmNames != null) &&
                    (macAlgorithmNames.Count > 0)))
                {
                    localList.Add("MacAlgorithmNames",
                        (macAlgorithmNames != null) ?
                            macAlgorithmNames.Count.ToString() :
                            FormatOps.DisplayNull);
                }

                if (empty || ((normalAlgorithmNames != null) &&
                    (normalAlgorithmNames.Count > 0)))
                {
                    localList.Add("NormalAlgorithmNames",
                        (normalAlgorithmNames != null) ?
                            normalAlgorithmNames.Count.ToString() :
                            FormatOps.DisplayNull);
                }

                if (localList.Count > 0)
                {
                    list.Add((IPair<string>)null);
                    list.Add("Hash Algorithms");
                    list.Add((IPair<string>)null);
                    list.Add(localList);
                }
            }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Private Introspection Support Methods
        /// <summary>
        /// This method determines whether the type with the specified name is a
        /// supported hash algorithm.
        /// </summary>
        /// <param name="typeName">
        /// The name of the type to examine.
        /// </param>
        /// <param name="subTypeName">
        /// Upon success, receives the sub-type name describing the kind of hash
        /// algorithm (e.g. "mac", "keyed", or "normal").
        /// </param>
        /// <returns>
        /// True if the named type is a supported hash algorithm; otherwise,
        /// false.
        /// </returns>
        private static bool IsAlgorithm(
            string typeName,
            ref string subTypeName
            )
        {
            if (String.IsNullOrEmpty(typeName))
                return false;

            return IsAlgorithm(
                Type.GetType(typeName), typeName, ref subTypeName);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method determines whether the specified type is a supported
        /// hash algorithm.
        /// </summary>
        /// <param name="type">
        /// The type to examine.
        /// </param>
        /// <param name="typeName">
        /// The name of the type to examine, used for additional validation when
        /// running on .NET Core.
        /// </param>
        /// <param name="subTypeName">
        /// Upon success, receives the sub-type name describing the kind of hash
        /// algorithm (e.g. "mac", "keyed", or "normal").
        /// </param>
        /// <returns>
        /// True if the type is a supported hash algorithm; otherwise, false.
        /// </returns>
        private static bool IsAlgorithm(
            Type type,
            string typeName,
            ref string subTypeName
            )
        {
            if (type == null)
                return false;

            //
            // NOTE: When running on .NET Core, verify that the type name
            //       is valid and can be looked up.  This helps to filter
            //       out other, non-type name entries.  Also, for .NET 7,
            //       filter out abstract classes.
            //
            if (CommonOps.Runtime.IsDotNetCore() && (typeName != null))
            {
                Type localType = Type.GetType(
                    FormatOps.GetQualifiedTypeFullName(
                        GetNamespaceNameForAlgorithms(), typeName,
                        GetAssemblyForAlgorithms()), false, true);

                if (localType == null)
                    return false;

                //
                // BUGBUG: Why is this required here?  What changed in
                //         .NET 7 that causes this to be necessary?
                //         Please refer to tests "hash-1.1.*" for some
                //         additional context.
                //
                if (localType.IsAbstract &&
                    CommonOps.Runtime.IsDotNetCore7xOrHigher())
                {
                    return false;
                }
            }

            if (MarshalOps.IsAssignableFrom(typeof(HMAC), type))
            {
                subTypeName = "mac";
                return true;
            }

            if (MarshalOps.IsAssignableFrom(typeof(KeyedHashAlgorithm), type))
            {
                subTypeName = "keyed";
                return true;
            }

            if (MarshalOps.IsAssignableFrom(typeof(HashAlgorithm), type))
            {
                subTypeName = "normal";
                return true;
            }

            return false;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method gets the cached reflection member used to enumerate the
        /// hash algorithms registered with the framework, looking it up on first
        /// use.
        /// </summary>
        /// <returns>
        /// The reflection member used to enumerate the registered hash
        /// algorithms, or null if it could not be obtained.
        /// </returns>
        private static MemberInfo GetMemberInfo()
        {
            lock (syncRoot) /* TRANSACTIONAL */
            {
                if (memberInfo != null)
                    return memberInfo;

                if (CommonOps.Runtime.IsMono())
                {
                    memberInfo = typeof(CryptoConfig).GetField(
                        "algorithms", ObjectOps.GetBindingFlags(
                            MetaBindingFlags.PrivateStaticGetField, true));
                }
                else
                {
                    memberInfo = typeof(CryptoConfig).GetProperty(
                        "DefaultNameHT", ObjectOps.GetBindingFlags(
                        MetaBindingFlags.PrivateStaticGetProperty, true));
                }

                return memberInfo;
            }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method enumerates the hash algorithms registered with the
        /// framework, filtering them to those that are supported.
        /// </summary>
        /// <returns>
        /// A list of supported hash algorithm names, each paired with its
        /// sub-type name, or null if they could not be enumerated.
        /// </returns>
        private static StringList GetAlgorithms()
        {
            lock (syncRoot) /* TRANSACTIONAL */
            {
                MemberInfo memberInfo = GetMemberInfo();

                if (memberInfo == null)
                    return null;

                if (CommonOps.Runtime.IsMono())
                {
                    object value = ((FieldInfo)memberInfo).GetValue(null);

                    if (value is IDictionary<string, Type>) /* v3.x */
                    {
                        StringList list = new StringList();

                        foreach (KeyValuePair<string, Type> pair in
                                ((IDictionary<string, Type>)value))
                        {
                            if ((pair.Key == null) || (pair.Value == null))
                                continue;

                            string subTypeName = null;

                            if (!IsAlgorithm(
                                    pair.Value, pair.Key, ref subTypeName))
                            {
                                continue;
                            }

                            list.Add(StringList.MakeList(
                                subTypeName, pair.Key));
                        }

                        return list;
                    }
                    else if (value is Hashtable) /* v2.x */
                    {
                        StringList list = new StringList();

                        foreach (DictionaryEntry entry in ((Hashtable)value))
                        {
                            if (entry.Key == null)
                                continue;

                            string subTypeName = null;

                            if (!IsAlgorithm(
                                    entry.Value as string, ref subTypeName))
                            {
                                continue;
                            }

                            list.Add(StringList.MakeList(
                                subTypeName, entry.Key.ToString()));
                        }

                        return list;
                    }
                }
                else
                {
                    object value = ((PropertyInfo)memberInfo).GetValue(
                        null, null);

                    if (value is IDictionary<string, object>) /* v4.x */
                    {
                        StringList list = new StringList();

                        foreach (KeyValuePair<string, object> pair in
                                ((IDictionary<string, object>)value))
                        {
                            if (pair.Key == null)
                                continue;

                            string subTypeName = null;

                            if (!IsAlgorithm(
                                    pair.Value as Type, pair.Key,
                                    ref subTypeName))
                            {
                                continue;
                            }

                            list.Add(StringList.MakeList(
                                subTypeName, pair.Key));
                        }

                        return list;
                    }
                    else if (value is Hashtable) /* v2.x */
                    {
                        StringList list = new StringList();

                        foreach (DictionaryEntry entry in ((Hashtable)value))
                        {
                            if (entry.Key == null)
                                continue;

                            string subTypeName = null;

                            if (!IsAlgorithm(
                                    entry.Value as Type, entry.Key.ToString(),
                                    ref subTypeName))
                            {
                                continue;
                            }

                            list.Add(StringList.MakeList(
                                subTypeName, entry.Key.ToString()));
                        }

                        return list;
                    }
                }

                return null;
            }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Public Introspection Support Methods
        /// <summary>
        /// This method adds the names of the selected categories of hash
        /// algorithms to the specified list.
        /// </summary>
        /// <param name="addDefault">
        /// Non-zero to add the default (framework-discovered) algorithm names.
        /// </param>
        /// <param name="addMac">
        /// Non-zero to add the HMAC algorithm names.
        /// </param>
        /// <param name="addKeyed">
        /// Non-zero to add the keyed hash algorithm names.
        /// </param>
        /// <param name="addNormal">
        /// Non-zero to add the normal (non-keyed) hash algorithm names.
        /// </param>
        /// <param name="list">
        /// The list to which the algorithm names should be added, created if
        /// necessary.
        /// </param>
        public static void AddAlgorithmNames(
            bool addDefault,
            bool addMac,
            bool addKeyed,
            bool addNormal,
            ref StringList list
            )
        {
            Initialize();

            lock (syncRoot) /* TRANSACTIONAL */
            {
                if (addDefault && ((defaultAlgorithmNames != null) &&
                    (defaultAlgorithmNames.Count > 0)))
                {
                    if (list == null)
                        list = new StringList();

                    list.AddRange(defaultAlgorithmNames);
                }

                if (addMac && (macAlgorithmNames != null))
                {
                    foreach (string hashAlgorithmName in macAlgorithmNames)
                    {
                        if (list == null)
                            list = new StringList();

                        list.Add(StringList.MakeList("mac",
                            hashAlgorithmName));
                    }
                }

                if (addKeyed && (keyedAlgorithmNames != null))
                {
                    foreach (string hashAlgorithmName in keyedAlgorithmNames)
                    {
                        if (list == null)
                            list = new StringList();

                        list.Add(StringList.MakeList("keyed",
                            hashAlgorithmName));
                    }
                }

                if (addNormal && (normalAlgorithmNames != null))
                {
                    foreach (string hashAlgorithmName in normalAlgorithmNames)
                    {
                        if (list == null)
                            list = new StringList();

                        list.Add(StringList.MakeList("normal",
                            hashAlgorithmName));
                    }
                }
            }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Static "Factory" Methods
        /// <summary>
        /// This method initializes the cached lists of hash algorithm names, if
        /// they have not already been initialized.
        /// </summary>
        private static void Initialize()
        {
            lock (syncRoot) /* TRANSACTIONAL */
            {
                if (defaultAlgorithmNames == null)
                {
                    try
                    {
                        defaultAlgorithmNames = GetAlgorithms();
                    }
                    catch (Exception e)
                    {
                        TraceOps.DebugTrace(
                            e, typeof(HashOps).Name,
                            TracePriority.InternalError);
                    }

                    //
                    // HACK: Prevent this block from being entered again
                    //       for this application domain.
                    //
                    if (defaultAlgorithmNames == null)
                        defaultAlgorithmNames = new StringList();
                }

                ///////////////////////////////////////////////////////////////

                if (macAlgorithmNames == null)
                {
                    macAlgorithmNames = new StringList(new string[] {
                        "HMACMD5",
#if !NET_STANDARD_20
                        "HMACRIPEMD160",
#endif
                        "HMACSHA1", "HMACSHA256", "HMACSHA384", "HMACSHA512"
                    });
                }

                ///////////////////////////////////////////////////////////////

                if (keyedAlgorithmNames == null)
                {
                    keyedAlgorithmNames = new StringList(new string[] {
#if !NET_STANDARD_20
                        "MACTripleDES"
#endif
                    });
                }

                ///////////////////////////////////////////////////////////////

                if (normalAlgorithmNames == null)
                {
                    normalAlgorithmNames = new StringList(new string[] {
                        "MD5",
#if !NET_STANDARD_20
                        "RIPEMD160",
#endif
                        "SHA1", "SHA256", "SHA384", "SHA512"
                    });
                }
            }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method gets the name of the namespace that contains the hash
        /// algorithm types.
        /// </summary>
        /// <returns>
        /// The name of the namespace containing the hash algorithm types.
        /// </returns>
        private static string GetNamespaceNameForAlgorithms()
        {
            return typeof(HashAlgorithm).Namespace;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method gets the assembly that contains the hash algorithm
        /// types.
        /// </summary>
        /// <returns>
        /// The assembly containing the hash algorithm types.
        /// </returns>
        private static Assembly GetAssemblyForAlgorithms()
        {
            return typeof(SHA1).Assembly;
        }

        ///////////////////////////////////////////////////////////////////////

        //
        // HACK: The type name returned must include assembly information.
        //
        /// <summary>
        /// This method builds the fully-qualified type name for the hash
        /// algorithm with the specified name.
        /// </summary>
        /// <param name="hashAlgorithmName">
        /// The simple name of the hash algorithm.
        /// </param>
        /// <returns>
        /// The fully-qualified type name (including namespace and assembly
        /// information) for the hash algorithm.
        /// </returns>
        private static string GetAlgorithmTypeName(
            string hashAlgorithmName
            )
        {
            if (String.IsNullOrEmpty(hashAlgorithmName))
                return hashAlgorithmName;

            if (SharedStringOps.SystemEquals(
                    hashAlgorithmName, ShaAlgorithmName))
            {
                hashAlgorithmName = Sha1AlgorithmName;
            }

            return FormatOps.GetQualifiedTypeFullName(
                GetNamespaceNameForAlgorithms(), hashAlgorithmName,
                GetAssemblyForAlgorithms());
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method looks up the type for the hash algorithm with the
        /// specified name.
        /// </summary>
        /// <param name="hashAlgorithmName">
        /// The simple name of the hash algorithm.
        /// </param>
        /// <returns>
        /// The type for the hash algorithm, or null if it could not be found.
        /// </returns>
        private static Type LookupAlgorithmType(
            string hashAlgorithmName
            )
        {
            //
            // NOTE: Get the type name qualified with the name of its
            //       containing namespace and/or assembly.  Then, try
            //       to lookup the type based on that qualified type
            //       name.
            //
            return FactoryOps.LookupType(
                GetAlgorithmTypeName(hashAlgorithmName), true);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method determines whether hash algorithm instances should be
        /// created via the Eagle factory mechanism.
        /// </summary>
        /// <returns>
        /// True if the factory mechanism should be used; otherwise, false.
        /// </returns>
        private static bool ShouldUseFactories()
        {
            return Interlocked.CompareExchange(ref useFactories, 0, 0) > 0;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method creates an HMAC instance for the hash algorithm with the
        /// specified name.
        /// </summary>
        /// <param name="hashAlgorithmName">
        /// The name of the hash algorithm to create.
        /// </param>
        /// <param name="error">
        /// Upon failure, receives information about the error.
        /// </param>
        /// <returns>
        /// The created HMAC instance, or null if it could not be created.
        /// </returns>
        public static HMAC CreateHMAC(
            string hashAlgorithmName,
            ref Result error
            )
        {
            if (!ShouldUseFactories())
            {
                try
                {
                    HMAC hmac = HMAC.Create(hashAlgorithmName); /* throw */

                    if (hmac == null)
                    {
                        error = String.Format(
                            "could not create hash algorithm {0}",
                            hashAlgorithmName);
                    }

                    return hmac;
                }
                catch (Exception e)
                {
                    error = e;
                }

                return null;
            }
            else
            {
                HashAlgorithm hashAlgorithm = CreateAlgorithm(
                    hashAlgorithmName, ref error);

                if (hashAlgorithm == null)
                    return null;

                HMAC hmac = hashAlgorithm as HMAC;

                if (hmac == null)
                {
                    /* IGNORED */
                    ObjectOps.TryDisposeOrTrace<HashAlgorithm>(
                        ref hashAlgorithm);

                    hashAlgorithm = null;

                    error = String.Format(
                        "hash algorithm {0} is not an {1}",
                        FormatOps.WrapOrNull(hashAlgorithmName),
                        FormatOps.TypeName(typeof(HMAC)));

                    return null;
                }

                return hmac;
            }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method creates a keyed hash algorithm instance for the hash
        /// algorithm with the specified name.
        /// </summary>
        /// <param name="hashAlgorithmName">
        /// The name of the hash algorithm to create.
        /// </param>
        /// <param name="error">
        /// Upon failure, receives information about the error.
        /// </param>
        /// <returns>
        /// The created keyed hash algorithm instance, or null if it could not
        /// be created.
        /// </returns>
        public static KeyedHashAlgorithm CreateKeyedAlgorithm(
            string hashAlgorithmName,
            ref Result error
            )
        {
            if (!ShouldUseFactories())
            {
                try
                {
                    KeyedHashAlgorithm keyedHashAlgorithm =
                        KeyedHashAlgorithm.Create(
                            hashAlgorithmName); /* throw */

                    if (keyedHashAlgorithm == null)
                    {
                        error = String.Format(
                            "could not create hash algorithm {0}",
                            hashAlgorithmName);
                    }

                    return keyedHashAlgorithm;
                }
                catch (Exception e)
                {
                    error = e;
                }

                return null;
            }
            else
            {
                HashAlgorithm hashAlgorithm = CreateAlgorithm(
                    hashAlgorithmName, ref error);

                if (hashAlgorithm == null)
                    return null;

                KeyedHashAlgorithm keyedHashAlgorithm =
                    hashAlgorithm as KeyedHashAlgorithm;

                if (keyedHashAlgorithm == null)
                {
                    /* IGNORED */
                    ObjectOps.TryDisposeOrTrace<HashAlgorithm>(
                        ref hashAlgorithm);

                    hashAlgorithm = null;

                    error = String.Format(
                        "hash algorithm {0} is not an {1}",
                        FormatOps.WrapOrNull(hashAlgorithmName),
                        FormatOps.TypeName(typeof(KeyedHashAlgorithm)));

                    return null;
                }

                return keyedHashAlgorithm;
            }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method creates a hash algorithm instance for the hash algorithm
        /// with the specified name, or the default algorithm when none is
        /// specified.
        /// </summary>
        /// <param name="hashAlgorithmName">
        /// The name of the hash algorithm to create, or null to use the default
        /// hash algorithm.
        /// </param>
        /// <param name="error">
        /// Upon failure, receives information about the error.
        /// </param>
        /// <returns>
        /// The created hash algorithm instance, or null if it could not be
        /// created.
        /// </returns>
        public static HashAlgorithm CreateAlgorithm(
            string hashAlgorithmName,
            ref Result error
            )
        {
            if (!ShouldUseFactories())
            {
                try
                {
                    HashAlgorithm hashAlgorithm;

                    if (hashAlgorithmName != null)
                    {
                        hashAlgorithm = HashAlgorithm.Create(
                            hashAlgorithmName); /* throw */

                        if (hashAlgorithm == null)
                        {
                            error = String.Format(
                                "could not create hash algorithm {0}",
                                hashAlgorithmName);
                        }
                    }
                    else
                    {
                        hashAlgorithm = HashAlgorithm.Create(); /* throw */

                        if (hashAlgorithm == null)
                            error = "could not create default hash algorithm";
                    }

                    return hashAlgorithm;
                }
                catch (Exception e)
                {
                    error = e;
                }

                return null;
            }
            else
            {
                if (hashAlgorithmName == null)
                    hashAlgorithmName = DefaultCreateAlgorithmName;

                Type type = LookupAlgorithmType(hashAlgorithmName);

                if (type == null)
                {
                    error = String.Format(
                        "unrecognized hash algorithm {0}",
                        FormatOps.WrapOrNull(hashAlgorithmName));

                    return null;
                }

                object @object = FactoryOps.Create(type, ref error);

                if (@object == null)
                    return null;

                HashAlgorithm hashAlgorithm = @object as HashAlgorithm;

                if (hashAlgorithm == null)
                {
                    error = String.Format(
                        "type {0} mismatch for hash algorithm {1}",
                        FormatOps.TypeNameOrFullName(@object),
                        FormatOps.WrapOrNull(hashAlgorithmName));

                    /* IGNORED */
                    ObjectOps.TryDisposeOrTrace<object>(
                        ref @object);

                    @object = null;

                    return null;
                }

                return hashAlgorithm;
            }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method computes a keyed hash over the specified value, which
        /// may be either a string or the contents of a file.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter used when opening a file, or null.
        /// </param>
        /// <param name="hashAlgorithmName">
        /// The name of the keyed hash algorithm to use.
        /// </param>
        /// <param name="key">
        /// The key to use with the keyed hash algorithm, or null.
        /// </param>
        /// <param name="value">
        /// The value to hash, or the path of the file to hash when
        /// <paramref name="valueIsPath" /> is non-zero.
        /// </param>
        /// <param name="encoding">
        /// The character encoding used when converting the value to bytes.
        /// </param>
        /// <param name="encodingType">
        /// The encoding type used when converting the value to bytes, or null
        /// to use the binary encoding type.
        /// </param>
        /// <param name="valueIsPath">
        /// Non-zero if <paramref name="value" /> is the path of a file whose
        /// contents should be hashed.
        /// </param>
        /// <param name="error">
        /// Upon failure, receives information about the error.
        /// </param>
        /// <returns>
        /// The computed hash bytes, or null on failure.
        /// </returns>
        public static byte[] ComputeKeyed(
            Interpreter interpreter,    /* in: OPTIONAL */
            string hashAlgorithmName,   /* in */
            byte[] key,                 /* in */
            string value,               /* in */
            Encoding encoding,          /* in */
            EncodingType? encodingType, /* in */
            bool valueIsPath,           /* in */
            ref Result error            /* out */
            )
        {
            using (KeyedHashAlgorithm hashAlgorithm = CreateKeyedAlgorithm(
                    hashAlgorithmName, ref error))
            {
                if (hashAlgorithm == null)
                    return null;

                if (valueIsPath)
                {
                    Stream stream = null;

                    try
                    {
                        if (RuntimeOps.NewStream(
                                interpreter, value, FileMode.Open,
                                FileAccess.Read, ref stream,
                                ref error) != ReturnCode.Ok)
                        {
                            return null;
                        }

                        try
                        {
                            hashAlgorithm.Initialize(); /* throw */

                            if (key != null)
                                hashAlgorithm.Key = key; /* throw */

                            return hashAlgorithm.ComputeHash(
                                stream); /* throw */
                        }
                        catch (Exception e)
                        {
                            error = e;
                            return null;
                        }
                    }
                    finally
                    {
                        if (stream != null)
                        {
                            stream.Close();
                            stream = null;
                        }
                    }
                }
                else
                {
                    byte[] bytes = null;

                    if (StringOps.GetBytes(
                            encoding, value, (encodingType != null) ?
                            (EncodingType)encodingType : EncodingType.Binary,
                            true, ref bytes, ref error) != ReturnCode.Ok)
                    {
                        return null;
                    }

                    try
                    {
                        hashAlgorithm.Initialize(); /* throw */

                        if (key != null)
                            hashAlgorithm.Key = key; /* throw */

                        return hashAlgorithm.ComputeHash(
                            bytes); /* throw */
                    }
                    catch (Exception e)
                    {
                        error = e;
                        return null;
                    }
                }
            }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method computes a hash over the concatenation of an optional
        /// string value and an optional byte array value.
        /// </summary>
        /// <param name="hashAlgorithmName">
        /// The name of the hash algorithm to use, or null to use the default
        /// hash algorithm.
        /// </param>
        /// <param name="value1">
        /// The optional string value to include in the hash, or null.
        /// </param>
        /// <param name="value2">
        /// The optional byte array value to include in the hash, or null.
        /// </param>
        /// <param name="encoding">
        /// The character encoding used when converting the string value to
        /// bytes, or null.
        /// </param>
        /// <param name="error">
        /// Upon failure, receives information about the error.
        /// </param>
        /// <returns>
        /// The computed hash bytes, or null on failure.
        /// </returns>
        public static byte[] Compute(
            string hashAlgorithmName, /* in: OPTIONAL */
            string value1,            /* in: OPTIONAL */
            byte[] value2,            /* in: OPTIONAL */
            Encoding encoding,        /* in: OPTIONAL */
            ref Result error          /* out */
            )
        {
            using (HashAlgorithm hashAlgorithm = CreateAlgorithm(
                    hashAlgorithmName, ref error))
            {
                if (hashAlgorithm == null)
                    return null;

                ByteList localBytes = null;

                if (value1 != null)
                {
                    byte[] value1Bytes = null;

                    if (StringOps.GetBytes(
                            encoding, value1, EncodingType.Binary, true,
                            ref value1Bytes, ref error) != ReturnCode.Ok)
                    {
                        return null;
                    }

                    if (localBytes == null)
                        localBytes = new ByteList();

                    localBytes.AddRange(value1Bytes);
                }

                if (value2 != null)
                {
                    if (localBytes == null)
                        localBytes = new ByteList();

                    localBytes.AddRange(value2);
                }

                if (localBytes == null)
                {
                    error = "nothing to hash";
                    return null;
                }

                try
                {
                    hashAlgorithm.Initialize(); /* throw */

                    return hashAlgorithm.ComputeHash(
                        localBytes.ToArray()); /* throw */
                }
                catch (Exception e)
                {
                    error = e;
                    return null;
                }
            }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method computes a hash over the specified value, which may be
        /// either a string or the contents of a file.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter used when opening a file, or null.
        /// </param>
        /// <param name="hashAlgorithmName">
        /// The name of the hash algorithm to use, or null to use the default
        /// hash algorithm.
        /// </param>
        /// <param name="value">
        /// The value to hash, or the path of the file to hash when
        /// <paramref name="valueIsPath" /> is non-zero.
        /// </param>
        /// <param name="encoding">
        /// The character encoding used when converting the value to bytes, or
        /// null.
        /// </param>
        /// <param name="encodingType">
        /// The encoding type used when converting the value to bytes, or null
        /// to use the binary encoding type.
        /// </param>
        /// <param name="valueIsPath">
        /// Non-zero if <paramref name="value" /> is the path of a file whose
        /// contents should be hashed.
        /// </param>
        /// <param name="error">
        /// Upon failure, receives information about the error.
        /// </param>
        /// <returns>
        /// The computed hash bytes, or null on failure.
        /// </returns>
        public static byte[] Compute(
            Interpreter interpreter,    /* in: OPTIONAL */
            string hashAlgorithmName,   /* in: OPTIONAL */
            string value,               /* in */
            Encoding encoding,          /* in: OPTIONAL */
            EncodingType? encodingType, /* in */
            bool valueIsPath,           /* in */
            ref Result error            /* out */
            )
        {
            using (HashAlgorithm hashAlgorithm = CreateAlgorithm(
                    hashAlgorithmName, ref error))
            {
                if (hashAlgorithm == null)
                    return null;

                if (valueIsPath)
                {
                    Stream stream = null;

                    try
                    {
                        if (RuntimeOps.NewStream(
                                interpreter, value, FileMode.Open,
                                FileAccess.Read, ref stream,
                                ref error) != ReturnCode.Ok)
                        {
                            return null;
                        }

                        try
                        {
                            hashAlgorithm.Initialize(); /* throw */

                            return hashAlgorithm.ComputeHash(
                                stream); /* throw */
                        }
                        catch (Exception e)
                        {
                            error = e;
                            return null;
                        }
                    }
                    finally
                    {
                        if (stream != null)
                        {
                            stream.Close();
                            stream = null;
                        }
                    }
                }
                else
                {
                    byte[] bytes = null;

                    if (StringOps.GetBytes(
                            encoding, value, (encodingType != null) ?
                            (EncodingType)encodingType : EncodingType.Binary,
                            true, ref bytes, ref error) != ReturnCode.Ok)
                    {
                        return null;
                    }

                    try
                    {
                        hashAlgorithm.Initialize(); /* throw */

                        return hashAlgorithm.ComputeHash(
                            bytes); /* throw */
                    }
                    catch (Exception e)
                    {
                        error = e;
                        return null;
                    }
                }
            }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method computes an HMAC over the specified value, which may be
        /// either a string or the contents of a file.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter used when opening a file, or null.
        /// </param>
        /// <param name="hashAlgorithmName">
        /// The name of the HMAC algorithm to use.
        /// </param>
        /// <param name="key">
        /// The key to use with the HMAC algorithm, or null.
        /// </param>
        /// <param name="value">
        /// The value to hash, or the path of the file to hash when
        /// <paramref name="valueIsPath" /> is non-zero.
        /// </param>
        /// <param name="encoding">
        /// The character encoding used when converting the value to bytes.
        /// </param>
        /// <param name="encodingType">
        /// The encoding type used when converting the value to bytes, or null
        /// to use the binary encoding type.
        /// </param>
        /// <param name="valueIsPath">
        /// Non-zero if <paramref name="value" /> is the path of a file whose
        /// contents should be hashed.
        /// </param>
        /// <param name="error">
        /// Upon failure, receives information about the error.
        /// </param>
        /// <returns>
        /// The computed HMAC bytes, or null on failure.
        /// </returns>
        public static byte[] ComputeHMAC(
            Interpreter interpreter,    /* in: OPTIONAL */
            string hashAlgorithmName,   /* in */
            byte[] key,                 /* in */
            string value,               /* in */
            Encoding encoding,          /* in */
            EncodingType? encodingType, /* in */
            bool valueIsPath,           /* in */
            ref Result error            /* out */
            )
        {
            using (HMAC hashAlgorithm = CreateHMAC(
                    hashAlgorithmName, ref error))
            {
                if (hashAlgorithm == null)
                    return null;

                if (valueIsPath)
                {
                    Stream stream = null;

                    try
                    {
                        if (RuntimeOps.NewStream(
                                interpreter, value, FileMode.Open,
                                FileAccess.Read, ref stream,
                                ref error) != ReturnCode.Ok)
                        {
                            return null;
                        }

                        try
                        {
                            hashAlgorithm.Initialize(); /* throw */

                            if (key != null)
                                hashAlgorithm.Key = key; /* throw */

                            return hashAlgorithm.ComputeHash(
                                stream); /* throw */
                        }
                        catch (Exception e)
                        {
                            error = e;
                            return null;
                        }
                    }
                    finally
                    {
                        if (stream != null)
                        {
                            stream.Close();
                            stream = null;
                        }
                    }
                }
                else
                {
                    byte[] bytes = null;

                    if (StringOps.GetBytes(
                            encoding, value, (encodingType != null) ?
                            (EncodingType)encodingType : EncodingType.Binary,
                            true, ref bytes, ref error) != ReturnCode.Ok)
                    {
                        return null;
                    }

                    try
                    {
                        hashAlgorithm.Initialize(); /* throw */

                        if (key != null)
                            hashAlgorithm.Key = key; /* throw */

                        return hashAlgorithm.ComputeHash(
                            bytes); /* throw */
                    }
                    catch (Exception e)
                    {
                        error = e;
                        return null;
                    }
                }
            }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method clears the cached lists of hash algorithm names and the
        /// cached reflection member, releasing the associated resources.
        /// </summary>
        /// <returns>
        /// The total number of cached items that were released.
        /// </returns>
        public static int Cleanup()
        {
            lock (syncRoot) /* TRANSACTIONAL */
            {
                int result = 0;

                ///////////////////////////////////////////////////////////////

                if (defaultAlgorithmNames != null)
                {
                    result += defaultAlgorithmNames.Count;

                    defaultAlgorithmNames.Clear();
                    defaultAlgorithmNames = null;
                }

                ///////////////////////////////////////////////////////////////

                if (macAlgorithmNames != null)
                {
                    result += macAlgorithmNames.Count;

                    macAlgorithmNames.Clear();
                    macAlgorithmNames = null;
                }

                ///////////////////////////////////////////////////////////////

                if (keyedAlgorithmNames != null)
                {
                    result += keyedAlgorithmNames.Count;

                    keyedAlgorithmNames.Clear();
                    keyedAlgorithmNames = null;
                }

                ///////////////////////////////////////////////////////////////

                if (normalAlgorithmNames != null)
                {
                    result += normalAlgorithmNames.Count;

                    normalAlgorithmNames.Clear();
                    normalAlgorithmNames = null;
                }

                ///////////////////////////////////////////////////////////////

                if (memberInfo != null)
                {
                    result++;

                    memberInfo = null;
                }

                ///////////////////////////////////////////////////////////////

                return result;
            }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Private Hashing Support Methods
        /// <summary>
        /// This method determines whether modern hash algorithms should be used
        /// for the specified encoding type, based on the relevant environment
        /// variables.
        /// </summary>
        /// <param name="encodingType">
        /// The encoding type for which the algorithm is being selected.
        /// </param>
        /// <returns>
        /// True if modern hash algorithms should be used; otherwise, false.
        /// </returns>
        private static bool ShouldUseModernAlgorithms(
            EncodingType encodingType /* in */
            )
        {
            foreach (string envVarName in new string[] {
                    String.Format(
                        "{0}{1}{2}",
                        EnvVars.ForceModernAlgorithms,
                        Characters.Underscore, encodingType
                    ),
                    EnvVars.ForceModernAlgorithms
                })
            {
                if (String.IsNullOrEmpty(envVarName))
                    continue;

                if (CommonOps.Environment.DoesVariableExist(
                        envVarName))
                {
                    return true;
                }
            }

            return false;
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Public Hashing Support Methods
        /// <summary>
        /// This method gets the name of the hash algorithm to use for the
        /// specified encoding type, taking the relevant environment variables
        /// into account.
        /// </summary>
        /// <param name="encodingType">
        /// The encoding type for which the algorithm name is being selected.
        /// </param>
        /// <returns>
        /// The name of the hash algorithm to use, or null if the encoding type
        /// is not recognized.
        /// </returns>
        public static string GetAlgorithmName(
            EncodingType encodingType /* in */
            )
        {
            //
            // HACK: When the "ForceModernAlgorithms" environment variable
            //       is set, use more modern hash algorithms like SHA512,
            //       i.e. not SHA1.  Setting this environment variable may
            //       break backward compatibility with external plugins,
            //       e.g. Harpy.
            //
            switch (encodingType)
            {
                case EncodingType.Binary:
                    {
                        if (ShouldUseModernAlgorithms(encodingType))
                            return ModernBytesAlgorithmName;
                        else
                            return LegacyBytesAlgorithmName;
                    }
                case EncodingType.Text:
                    {
                        //
                        // NOTE: There was not a legacy hash algorithm
                        //       that was used in this context.
                        //
                        return ModernStringAlgorithmName;
                    }
                case EncodingType.Snippet:
                    {
                        if (ShouldUseModernAlgorithms(encodingType))
                            return ModernSnippetAlgorithmName;
                        else
                            return LegacySnippetAlgorithmName;
                    }
                default:
                    {
                        TraceOps.DebugTrace(String.Format(
                            "GetAlgorithmName: bad encoding type {0}",
                            encodingType), typeof(HashOps).Name,
                            TracePriority.SecurityError);

                        return null;
                    }
            }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method computes a hash over the specified text, using the named
        /// character encoding.
        /// </summary>
        /// <param name="hashAlgorithmName">
        /// The name of the hash algorithm to use.
        /// </param>
        /// <param name="encodingName">
        /// The name of the character encoding used when converting the text to
        /// bytes, or null to use the default encoding.
        /// </param>
        /// <param name="text">
        /// The text to hash.
        /// </param>
        /// <returns>
        /// The computed hash bytes, or null on failure.
        /// </returns>
        public static byte[] HashString(
            string hashAlgorithmName,
            string encodingName,
            string text
            )
        {
            if (encodingName == null)
                encodingName = DefaultEncodingName;

            return HashString(
                hashAlgorithmName, StringOps.GetEncoding(encodingName),
                text);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method computes a hash over the specified text, using the given
        /// character encoding, tracing any error that occurs.
        /// </summary>
        /// <param name="hashAlgorithmName">
        /// The name of the hash algorithm to use.
        /// </param>
        /// <param name="encoding">
        /// The character encoding used when converting the text to bytes.
        /// </param>
        /// <param name="text">
        /// The text to hash.
        /// </param>
        /// <returns>
        /// The computed hash bytes, or null on failure.
        /// </returns>
        public static byte[] HashString(
            string hashAlgorithmName,
            Encoding encoding,
            string text
            )
        {
            byte[] hashValue;
            Result error = null;

            hashValue = HashString(
                hashAlgorithmName, encoding, text, ref error);

            if (hashValue == null)
            {
                TraceOps.DebugTrace(String.Format(
                    "HashString: error = {0}",
                    FormatOps.WrapOrNull(error)),
                    typeof(HashOps).Name,
                    TracePriority.InternalError);
            }

            return hashValue;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method computes a hash over the specified text, using the given
        /// character encoding.
        /// </summary>
        /// <param name="hashAlgorithmName">
        /// The name of the hash algorithm to use.
        /// </param>
        /// <param name="encoding">
        /// The character encoding used when converting the text to bytes.
        /// </param>
        /// <param name="text">
        /// The text to hash.
        /// </param>
        /// <param name="error">
        /// Upon failure, receives information about the error.
        /// </param>
        /// <returns>
        /// The computed hash bytes, or null on failure.
        /// </returns>
        public static byte[] HashString(
            string hashAlgorithmName,
            Encoding encoding,
            string text,
            ref Result error
            )
        {
            return HashString(
                hashAlgorithmName, encoding, text, EncodingType.Text,
                ref error);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method computes a hash over the specified text, using the given
        /// character encoding, selecting a default hash algorithm based on the
        /// encoding type when none is specified.
        /// </summary>
        /// <param name="hashAlgorithmName">
        /// The name of the hash algorithm to use, or null to select one based
        /// on the encoding type.
        /// </param>
        /// <param name="encoding">
        /// The character encoding used when converting the text to bytes.
        /// </param>
        /// <param name="text">
        /// The text to hash.
        /// </param>
        /// <param name="encodingType">
        /// The encoding type used when selecting a default hash algorithm.
        /// </param>
        /// <param name="error">
        /// Upon failure, receives information about the error.
        /// </param>
        /// <returns>
        /// The computed hash bytes, or null on failure.
        /// </returns>
        public static byte[] HashString(
            string hashAlgorithmName,
            Encoding encoding,
            string text,
            EncodingType encodingType,
            ref Result error
            )
        {
            if (encoding == null)
            {
                error = "invalid encoding";
                return null;
            }

            if (hashAlgorithmName == null)
                hashAlgorithmName = GetAlgorithmName(encodingType);

            using (HashAlgorithm hashAlgorithm = CreateAlgorithm(
                    hashAlgorithmName, ref error))
            {
                if (hashAlgorithm == null)
                    return null;

                try
                {
                    hashAlgorithm.Initialize(); /* throw */

                    byte[] bytes = encoding.GetBytes(
                        text); /* throw */

                    if (bytes == null)
                    {
                        error = "invalid bytes";
                        return null;
                    }

                    return hashAlgorithm.ComputeHash(
                        bytes); /* throw */
                }
                catch (Exception e)
                {
                    error = e;
                    return null;
                }
            }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method computes a hash over the specified bytes, tracing any
        /// error that occurs.
        /// </summary>
        /// <param name="hashAlgorithmName">
        /// The name of the hash algorithm to use.
        /// </param>
        /// <param name="bytes">
        /// The bytes to hash.
        /// </param>
        /// <returns>
        /// The computed hash bytes, or null on failure.
        /// </returns>
        public static byte[] HashBytes(
            string hashAlgorithmName,
            byte[] bytes
            )
        {
            Result error = null;

            return HashBytes(hashAlgorithmName, bytes, ref error);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method computes a hash over the specified bytes.
        /// </summary>
        /// <param name="hashAlgorithmName">
        /// The name of the hash algorithm to use.
        /// </param>
        /// <param name="bytes">
        /// The bytes to hash.
        /// </param>
        /// <param name="error">
        /// Upon failure, receives information about the error.
        /// </param>
        /// <returns>
        /// The computed hash bytes, or null on failure.
        /// </returns>
        public static byte[] HashBytes(
            string hashAlgorithmName,
            byte[] bytes,
            ref Result error
            )
        {
            return HashBytes(
                hashAlgorithmName, bytes, EncodingType.Binary,
                ref error);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method computes a hash over the specified bytes, selecting a
        /// default hash algorithm based on the encoding type when none is
        /// specified.
        /// </summary>
        /// <param name="hashAlgorithmName">
        /// The name of the hash algorithm to use, or null to select one based
        /// on the encoding type.
        /// </param>
        /// <param name="bytes">
        /// The bytes to hash.
        /// </param>
        /// <param name="encodingType">
        /// The encoding type used when selecting a default hash algorithm.
        /// </param>
        /// <param name="error">
        /// Upon failure, receives information about the error.
        /// </param>
        /// <returns>
        /// The computed hash bytes, or null on failure.
        /// </returns>
        public static byte[] HashBytes(
            string hashAlgorithmName,
            byte[] bytes,
            EncodingType encodingType,
            ref Result error
            )
        {
            if (bytes == null)
            {
                error = "invalid bytes";
                return null;
            }

            if (hashAlgorithmName == null)
                hashAlgorithmName = GetAlgorithmName(encodingType);

            using (HashAlgorithm hashAlgorithm = CreateAlgorithm(
                    hashAlgorithmName, ref error))
            {
                if (hashAlgorithm == null)
                    return null;

                try
                {
                    hashAlgorithm.Initialize(); /* throw */

                    return hashAlgorithm.ComputeHash(
                        bytes); /* throw */
                }
                catch (Exception e)
                {
                    error = e;
                    return null;
                }
            }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Chained Hashing Methods
        /// <summary>
        /// This method folds the specified text into a running hash value,
        /// using the named character encoding.
        /// </summary>
        /// <param name="hashAlgorithmName">
        /// The name of the hash algorithm to use, or null to select a default.
        /// </param>
        /// <param name="encodingName">
        /// The name of the character encoding used when converting the text to
        /// bytes, or null to use the default encoding.
        /// </param>
        /// <param name="text">
        /// The text to fold into the running hash value.
        /// </param>
        /// <param name="hashBytes">
        /// The running hash value, updated in place to reflect the added text.
        /// </param>
        /// <param name="error">
        /// Upon failure, receives information about the error.
        /// </param>
        /// <returns>
        /// True if the text was folded into the running hash value
        /// successfully; otherwise, false.
        /// </returns>
        private static bool AddToHashBytes(
            string hashAlgorithmName, /* in */
            string encodingName,      /* in */
            string text,              /* in */
            ref byte[] hashBytes,     /* in, out */
            ref Result error          /* out */
            )
        {
            if (encodingName == null)
                encodingName = DefaultEncodingName;

            return AddToHashBytes(
                hashAlgorithmName, StringOps.GetEncoding(encodingName),
                text, ref hashBytes, ref error);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method folds the specified text into a running hash value,
        /// using the given character encoding.
        /// </summary>
        /// <param name="hashAlgorithmName">
        /// The name of the hash algorithm to use, or null to select a default.
        /// </param>
        /// <param name="encoding">
        /// The character encoding used when converting the text to bytes.
        /// </param>
        /// <param name="text">
        /// The text to fold into the running hash value.
        /// </param>
        /// <param name="hashBytes">
        /// The running hash value, updated in place to reflect the added text.
        /// </param>
        /// <param name="error">
        /// Upon failure, receives information about the error.
        /// </param>
        /// <returns>
        /// True if the text was folded into the running hash value
        /// successfully; otherwise, false.
        /// </returns>
        private static bool AddToHashBytes(
            string hashAlgorithmName, /* in */
            Encoding encoding,        /* in */
            string text,              /* in */
            ref byte[] hashBytes,     /* in, out */
            ref Result error          /* out */
            )
        {
            if (encoding == null)
            {
                error = "invalid encoding";
                return false;
            }

            if (text == null)
            {
                error = "invalid text";
                return false;
            }

            int capacity = encoding.GetByteCount(text);

            if (hashBytes != null)
                capacity += (2 * hashBytes.Length);

            ByteList localBytes = new ByteList(capacity);

            if (hashBytes != null)
                localBytes.AddRange(hashBytes);

            localBytes.AddRange(encoding.GetBytes(text));

            if (hashBytes != null)
                localBytes.AddRange(hashBytes);

            if (hashAlgorithmName == null)
                hashAlgorithmName = GetAlgorithmName(EncodingType.Text);

            using (HashAlgorithm hashAlgorithm = CreateAlgorithm(
                    hashAlgorithmName, ref error))
            {
                if (hashAlgorithm == null)
                    return false;

                try
                {
                    hashAlgorithm.Initialize(); /* throw */

                    hashBytes = hashAlgorithm.ComputeHash(
                        localBytes.ToArray()); /* throw */

                    return true;
                }
                catch (Exception e)
                {
                    error = e;
                    return false;
                }
            }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method folds the specified bytes into a running hash value.
        /// </summary>
        /// <param name="hashAlgorithmName">
        /// The name of the hash algorithm to use, or null to select a default.
        /// </param>
        /// <param name="bytes">
        /// The bytes to fold into the running hash value.
        /// </param>
        /// <param name="hashBytes">
        /// The running hash value, updated in place to reflect the added bytes.
        /// </param>
        /// <param name="error">
        /// Upon failure, receives information about the error.
        /// </param>
        /// <returns>
        /// True if the bytes were folded into the running hash value
        /// successfully; otherwise, false.
        /// </returns>
        private static bool AddToHashBytes(
            string hashAlgorithmName, /* in */
            byte[] bytes,             /* in */
            ref byte[] hashBytes,     /* in, out */
            ref Result error          /* out */
            )
        {
            if (bytes == null)
            {
                error = "invalid bytes";
                return false;
            }

            int capacity = bytes.Length;

            if (hashBytes != null)
                capacity += (2 * hashBytes.Length);

            ByteList localBytes = new ByteList(capacity);

            if (hashBytes != null)
                localBytes.AddRange(hashBytes);

            localBytes.AddRange(bytes);

            if (hashBytes != null)
                localBytes.AddRange(hashBytes);

            if (hashAlgorithmName == null)
                hashAlgorithmName = GetAlgorithmName(EncodingType.Text);

            using (HashAlgorithm hashAlgorithm = CreateAlgorithm(
                    hashAlgorithmName, ref error))
            {
                if (hashAlgorithm == null)
                    return false;

                try
                {
                    hashAlgorithm.Initialize(); /* throw */

                    hashBytes = hashAlgorithm.ComputeHash(
                        localBytes.ToArray()); /* throw */

                    return true;
                }
                catch (Exception e)
                {
                    error = e;
                    return false;
                }
            }
        }
        #endregion
    }
}
