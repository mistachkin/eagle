/*
 * FactoryOps.cs --
 *
 * Copyright (c) 2007-2012 by Joe Mistachkin.  All rights reserved.
 *
 * See the file "license.terms" for information on usage and redistribution of
 * this file, and for a DISCLAIMER OF ALL WARRANTIES.
 *
 * RCS: @(#) $Id: $
 */

using System;
using System.Security.Cryptography;
using Eagle._Attributes;
using Eagle._Components.Private.Delegates;
using Eagle._Components.Public;
using Eagle._Containers.Private;
using Eagle._Containers.Public;
using Eagle._Interfaces.Public;

namespace Eagle._Components.Private
{
    [ObjectId("92cb69c3-bb11-4f8f-b00e-e3fbf565daa9")]
    internal static class FactoryOps
    {
        #region Private Data
        private static readonly object syncRoot = new object();

        ///////////////////////////////////////////////////////////////////////

        private static TypeFactoryCallbackDictionary factories;
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

                if (empty || ((factories != null) && (factories.Count > 0)))
                {
                    localList.Add("Factories", (factories != null) ?
                        factories.Count.ToString() : FormatOps.DisplayNull);
                }

                if (localList.Count > 0)
                {
                    list.Add((IPair<string>)null);
                    list.Add("Factories");
                    list.Add((IPair<string>)null);
                    list.Add(localList);
                }
            }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Private Methods
        private static void Initialize()
        {
            lock (syncRoot) /* TRANSACTIONAL */
            {
                if (factories == null)
                {
                    factories = new TypeFactoryCallbackDictionary();

                    ///////////////////////////////////////////////////////////

                    //
                    // NOTE: The null value here means that the algorithm will
                    //       be created via its default public constructor via
                    //       the Activator.CreateInstance method.  This ends
                    //       up being necessary because these classes do not
                    //       have their own static Create method.
                    //
                    // factories.Add(typeof(HMAC), HMAC.Create);
                    factories.Add(typeof(HMACMD5), null);
                    // factories.Add(typeof(HMACRIPEMD160), null);
                    factories.Add(typeof(HMACSHA1), null);
                    factories.Add(typeof(HMACSHA256), null);
                    factories.Add(typeof(HMACSHA384), null);
                    factories.Add(typeof(HMACSHA512), null);

                    ///////////////////////////////////////////////////////////

                    //
                    // NOTE: The null value here means that the algorithm will
                    //       be created via its default public constructor via
                    //       the Activator.CreateInstance method.  This ends
                    //       up being necessary because these classes do not
                    //       have their own static Create method.
                    //
                    // factories.Add(typeof(KeyedHashAlgorithm),
                    //     KeyedHashAlgorithm.Create);

                    // factories.Add(typeof(MACTripleDES), null);

                    ///////////////////////////////////////////////////////////

                    // factories.Add(typeof(HashAlgorithm), HashAlgorithm.Create);
                    factories.Add(typeof(MD5), MD5.Create);
                    // factories.Add(typeof(RIPEMD160), RIPEMD160.Create);
                    // factories.Add(typeof(RIPEMD160Managed), null);
                    factories.Add(typeof(SHA1), SHA1.Create);
                    factories.Add(typeof(SHA256), SHA256.Create);
                    factories.Add(typeof(SHA384), SHA384.Create);
                    factories.Add(typeof(SHA512), SHA512.Create);

                    ///////////////////////////////////////////////////////////

                    // factories.Add(typeof(SymmetricAlgorithm),
                    //     SymmetricAlgorithm.Create);

#if NET_35 || NET_40 || NET_STANDARD_20
                    factories.Add(typeof(Aes), Aes.Create);

                    factories.Add(typeof(AesCryptoServiceProvider),
                        AesCryptoServiceProvider.Create);
#endif

                    factories.Add(typeof(DES), DES.Create);
                    factories.Add(typeof(RC2), RC2.Create);
                    factories.Add(typeof(Rijndael), Rijndael.Create);
                    factories.Add(typeof(RijndaelManaged), null);
                    // factories.Add(typeof(TripleDES), TripleDES.Create);
                }
            }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Public Methods
        public static Type LookupType(
            string typeName,   /* in */
            bool allowFallback /* in */
            )
        {
            try
            {
                /* COMPAT: Eagle (must be case-insensitive). */
                Type type = Type.GetType(typeName, false, true);

                if (type != null)
                    return type;

                //
                // HACK: Try removing any dashes that may be present in the
                //       type name (e.g. "RIPEMD-160" ==> "RIPEMD160") and
                //       then fetching the type again.
                //
                if (allowFallback && (typeName != null))
                {
                    typeName = typeName.Replace(
                        Characters.MinusSign.ToString(), String.Empty);

                    /* COMPAT: Eagle (must be case-insensitive). */
                    type = Type.GetType(typeName, false, true);

                    if (type != null)
                        return type;
                }
            }
            catch (Exception e)
            {
                TraceOps.DebugTrace(
                    e, typeof(FactoryOps).Namespace,
                    TracePriority.InternalError);
            }

            return null;
        }

        ///////////////////////////////////////////////////////////////////////

        public static object Create(
            Type type,
            ref Result error
            )
        {
            Initialize();

            lock (syncRoot) /* TRANSACTIONAL */
            {
                if (factories == null)
                {
                    error = "factory callbacks not available";
                    return null;
                }

                FactoryCallback callback;

                if ((type == null) ||
                    !factories.TryGetValue(type, out callback))
                {
                    error = String.Format(
                        "unsupported factory callback type {0}",
                        FormatOps.TypeNameOrFullName(type));

                    return null;
                }

                object @object = null;

                try
                {
                    if (callback != null)
                    {
                        @object = callback();
                    }
                    else
                    {
                        @object = Activator.CreateInstance(
                            type); /* throw */
                    }

                    if (@object == null)
                    {
                        error = String.Format(
                            "bad factory callback for type {0}",
                            FormatOps.TypeNameOrFullName(type));
                    }
                }
                catch (Exception e)
                {
                    error = e;
                }

                return @object;
            }
        }

        ///////////////////////////////////////////////////////////////////////

        public static int Cleanup()
        {
            lock (syncRoot) /* TRANSACTIONAL */
            {
                int result = 0;

                if (factories != null)
                {
                    result += factories.Count;

                    factories.Clear();
                    factories = null;
                }

                return result;
            }
        }
        #endregion
    }
}
