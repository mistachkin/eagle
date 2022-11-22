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
    [ObjectId("917c6350-a09e-43e0-a03f-e1a76a4cbe2e")]
    internal static class HashOps
    {
        #region Private Algorithm / Encoding Constants
        //
        // NOTE: The "SHA" algorithm name maps to "SHA1", apparently for
        //       reasons of backward compatibility with previous versions
        //       of the .NET Framework.
        //
        private static readonly string ShaAlgorithmName = "SHA";

        ///////////////////////////////////////////////////////////////////////

        private static readonly string Sha1AlgorithmName = "SHA1";

        ///////////////////////////////////////////////////////////////////////

        //
        // NOTE: *WARNING* Change this value with great care because it may
        //       break external components.
        //
        private static readonly string DefaultCreateAlgorithmName =
            Sha1AlgorithmName;

        ///////////////////////////////////////////////////////////////////////

        //
        // NOTE: *WARNING* Change this value with great care because it may
        //       break custom script, file, and stream policies that rely on
        //       the hash result.
        //
        private static readonly string DefaultBytesAlgorithmName =
            Sha1AlgorithmName;

        ///////////////////////////////////////////////////////////////////////

        //
        // NOTE: *WARNING* These are subject to change in the future (e.g. to
        //       more secure variants, etc).
        //
        internal static readonly string DefaultStringAlgorithmName = "SHA512";
        private static readonly string DefaultEncodingName = "utf-8";
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Private Data
        private static readonly object syncRoot = new object();

        ///////////////////////////////////////////////////////////////////////

        //
        // HACK: This is purposely not read-only.
        //
        private static int useFactories = CommonOps.Runtime.IsDotNetCore() ?
            1 : 0;

        ///////////////////////////////////////////////////////////////////////

        //
        // HACK: These are purposely not read-only.
        //
        private static StringList defaultAlgorithmNames;
        private static StringList keyedAlgorithmNames;
        private static StringList macAlgorithmNames;
        private static StringList normalAlgorithmNames;

        ///////////////////////////////////////////////////////////////////////

        //
        // HACK: This is purposely not read-only.
        //
        private static MemberInfo memberInfo;
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Introspection Support Methods
        //
        // NOTE: Used by the _Hosts.Default.BuildEngineInfoList method.
        //
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

        private static string GetNamespaceNameForAlgorithms()
        {
            return typeof(HashAlgorithm).Namespace;
        }

        ///////////////////////////////////////////////////////////////////////

        private static Assembly GetAssemblyForAlgorithms()
        {
            return typeof(SHA1).Assembly;
        }

        ///////////////////////////////////////////////////////////////////////

        //
        // HACK: The type name returned must include assembly information.
        //
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

        private static bool ShouldUseFactories()
        {
            return Interlocked.CompareExchange(ref useFactories, 0, 0) > 0;
        }

        ///////////////////////////////////////////////////////////////////////

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

                    ObjectOps.TryDisposeOrTrace<object>(
                        ref @object);

                    return null;
                }

                return hashAlgorithm;
            }
        }

        ///////////////////////////////////////////////////////////////////////

        public static byte[] ComputeKeyed(
            Interpreter interpreter,  /* in: OPTIONAL */
            string hashAlgorithmName, /* in */
            byte[] key,               /* in */
            string value,             /* in */
            Encoding encoding,        /* in */
            bool valueIsPath,         /* in */
            ref Result error          /* out */
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

                    if (StringOps.GetBytes(encoding, value,
                            EncodingType.Binary, true, ref bytes,
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

        public static byte[] Compute(
            Interpreter interpreter,  /* in: OPTIONAL */
            string hashAlgorithmName, /* in: OPTIONAL */
            string value,             /* in */
            Encoding encoding,        /* in: OPTIONAL */
            bool valueIsPath,         /* in */
            ref Result error          /* out */
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

                    if (StringOps.GetBytes(encoding, value,
                            EncodingType.Binary, true, ref bytes,
                            ref error) != ReturnCode.Ok)
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

        public static byte[] ComputeHMAC(
            Interpreter interpreter,  /* in: OPTIONAL */
            string hashAlgorithmName, /* in */
            byte[] key,               /* in */
            string value,             /* in */
            Encoding encoding,        /* in */
            bool valueIsPath,         /* in */
            ref Result error          /* out */
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

                    if (StringOps.GetBytes(encoding, value,
                            EncodingType.Binary, true, ref bytes,
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

        #region Public Hashing Support Methods
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

        public static byte[] HashString(
            string hashAlgorithmName,
            Encoding encoding,
            string text
            )
        {
            Result error = null;

            if (encoding == null)
            {
                error = "invalid encoding";
                goto error;
            }

            if (hashAlgorithmName == null)
                hashAlgorithmName = DefaultStringAlgorithmName;

            using (HashAlgorithm hashAlgorithm = CreateAlgorithm(
                    hashAlgorithmName, ref error))
            {
                if (hashAlgorithm == null)
                    goto error;

                try
                {
                    hashAlgorithm.Initialize(); /* throw */

                    byte[] bytes = encoding.GetBytes(
                        text); /* throw */

                    if (bytes == null)
                    {
                        error = "invalid bytes";
                        goto error;
                    }

                    return hashAlgorithm.ComputeHash(
                        bytes); /* throw */
                }
                catch (Exception e)
                {
                    error = e;
                }
            }

        error:

            TraceOps.DebugTrace(String.Format(
                "HashString: error = {0}",
                FormatOps.WrapOrNull(error)),
                typeof(HashOps).Name,
                TracePriority.InternalError);

            return null;
        }

        ///////////////////////////////////////////////////////////////////////

        public static byte[] HashBytes(
            string hashAlgorithmName,
            byte[] bytes,
            ref Result error
            )
        {
            if (bytes != null)
            {
                if (hashAlgorithmName == null)
                    hashAlgorithmName = DefaultBytesAlgorithmName;

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
                    }
                }
            }
            else
            {
                error = "invalid bytes";
            }

            return null;
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Chained Hashing Methods
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
                hashAlgorithmName = DefaultStringAlgorithmName;

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
                hashAlgorithmName = DefaultStringAlgorithmName;

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
