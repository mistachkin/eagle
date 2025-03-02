/*
 * SocketOps.cs --
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
using System.Globalization;
using System.IO;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;

#if NET_40
using System.Numerics;
#endif

using System.Reflection;
using System.Threading;
using Eagle._Attributes;
using Eagle._Components.Public;
using Eagle._Constants;
using Eagle._Containers.Public;
using Eagle._Interfaces.Public;

using CidrPair = System.Collections.Generic.KeyValuePair<
    string, Eagle._Containers.Public.StringList>;

using CidrDictionary = System.Collections.Generic.Dictionary<
    string, Eagle._Containers.Public.StringList>;

#if NET_STANDARD_21
using Index = Eagle._Constants.Index;
#endif

namespace Eagle._Components.Private
{
    [ObjectId("71b14766-48a0-45d5-9254-640fde03509d")]
    internal static class SocketOps
    {
        #region Private Constants
        //
        // HACK: These are no longer read-only.
        //
        private static int? MinimumSocketPollTimeout = 500; /* microseconds */
        private static int? MaximumSocketPollTimeout = null; /* microseconds */

        ///////////////////////////////////////////////////////////////////////

        private const int IPv4Parts = 4;
        private const byte IPv4Bits = 32;

        ///////////////////////////////////////////////////////////////////////

        //
        // HACK: This is purposely not read-only.
        //
        private static byte IPv4PrefixLength = 1; /* 1 part(s) (byte(s)), 1 byte, 8 bits */

        ///////////////////////////////////////////////////////////////////////

#if NET_40
        //
        // HACK: This is purposely not read-only.
        //
        private static byte IPv6PrefixLength = 1; /* 1 part(s) (word(s)), 2 bytes, 16 bits */

        ///////////////////////////////////////////////////////////////////////

        private const int ByteBits = 8;
        private const int IPv6Parts = 8;
        private const byte IPv6Bits = 128;
        private const string IPv6Format = "x";
        private const string IPv6Zeros = "::";

        ///////////////////////////////////////////////////////////////////////

        private const int SizeOfTwoULong = 2 * sizeof(ulong);
#endif
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Private Static Data
        private static readonly object syncRoot = new object();

        ///////////////////////////////////////////////////////////////////////

        private static PropertyInfo networkStreamSocket;
        private static PropertyInfo tcpListenerActive;
        private static PropertyInfo socketCleanedUp;

        ///////////////////////////////////////////////////////////////////////

        //
        // HACK: If this is non-zero, any attempt to create a WebClient via
        //       this class will fail, preventing any network access using
        //       the WebClient class.
        //
        private static int offlineLevels = 0;
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Internal State Introspection Methods
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

                if (empty || (networkStreamSocket != null))
                {
                    localList.Add("NetworkStreamSocket",
                        FormatOps.MemberName(networkStreamSocket));
                }

                if (empty || (tcpListenerActive != null))
                {
                    localList.Add("TcpListenerActive",
                        FormatOps.MemberName(tcpListenerActive));
                }

                if (empty || (socketCleanedUp != null))
                {
                    localList.Add("SocketCleanedUp",
                        FormatOps.MemberName(socketCleanedUp));
                }

                if (empty || (offlineLevels != 0))
                {
                    localList.Add("OfflineLevels",
                        offlineLevels.ToString());
                }

                if (localList.Count > 0)
                {
                    list.Add((IPair<string>)null);
                    list.Add("Socket Information");
                    list.Add((IPair<string>)null);
                    list.Add(localList);
                }
            }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Private Diagnostic Methods
        private static bool IsIPv4(
            IPAddress address, /* in */
            IpFlags ipFlags,   /* in */
            ref Result error   /* out */
            )
        {
            if (address == null)
            {
                if (FlagOps.HasFlags(
                        ipFlags, IpFlags.KeepErrors, true))
                {
                    error = "invalid IPv4 address";
                }

                return false;
            }

            AddressFamily addressFamily = address.AddressFamily;

            if (addressFamily != AddressFamily.InterNetwork)
            {
                if (FlagOps.HasFlags(
                        ipFlags, IpFlags.KeepErrors, true))
                {
                    error = String.Format(
                        "unsupported address family {0}",
                        addressFamily);
                }

                return false;
            }

            return true;
        }

        ///////////////////////////////////////////////////////////////////////

        private static bool IsIPv4(
            byte[] address,  /* in */
            IpFlags ipFlags, /* in */
            ref Result error /* out */
            )
        {
            if (address == null)
            {
                if (FlagOps.HasFlags(
                        ipFlags, IpFlags.KeepErrors, true))
                {
                    error = "invalid IPv4 address bytes";
                }

                return false;
            }

            if (!FlagOps.HasFlags(
                    ipFlags, IpFlags.IPv4, true))
            {
                if (FlagOps.HasFlags(
                        ipFlags, IpFlags.KeepErrors, true))
                {
                    error = "IPv4 is not allowed";
                }

                return false;
            }

            int haveLength = address.Length;
            int wantLength = sizeof(uint);

            if (haveLength != wantLength)
            {
                if (FlagOps.HasFlags(
                        ipFlags, IpFlags.KeepErrors, true))
                {
                    error = String.Format(
                        "expected {0} address bytes, got {1}",
                        wantLength, haveLength);
                }

                return false;
            }

            return true;
        }

        ///////////////////////////////////////////////////////////////////////

        private static byte GetPrefixLength(
            AddressFamily addressFamily /* in */
            )
        {
            if (addressFamily == AddressFamily.InterNetwork)
                return IPv4PrefixLength;

#if NET_40
            if (addressFamily == AddressFamily.InterNetworkV6)
                return IPv6PrefixLength;
#endif

            return 0;
        }

        ///////////////////////////////////////////////////////////////////////

#if NET_40
        private static bool IsIPv6(
            IPAddress address, /* in */
            IpFlags ipFlags,   /* in */
            ref Result error   /* out */
            )
        {
            if (address == null)
            {
                if (FlagOps.HasFlags(
                        ipFlags, IpFlags.KeepErrors, true))
                {
                    error = "invalid IPv6 address";
                }

                return false;
            }

            if (!FlagOps.HasFlags(
                    ipFlags, IpFlags.IPv6, true))
            {
                if (FlagOps.HasFlags(
                        ipFlags, IpFlags.KeepErrors, true))
                {
                    error = "IPv6 is not allowed";
                }

                return false;
            }

            AddressFamily addressFamily = address.AddressFamily;

            if (addressFamily != AddressFamily.InterNetworkV6)
            {
                if (FlagOps.HasFlags(
                        ipFlags, IpFlags.KeepErrors, true))
                {
                    error = String.Format(
                        "unsupported address family {0}",
                        addressFamily);
                }

                return false;
            }

            return true;
        }

        ///////////////////////////////////////////////////////////////////////

        private static bool IsIPv6(
            byte[] address,  /* in */
            IpFlags ipFlags, /* in */
            ref Result error /* out */
            )
        {
            if (address == null)
            {
                if (FlagOps.HasFlags(
                        ipFlags, IpFlags.KeepErrors, true))
                {
                    error = "invalid IPv6 address bytes";
                }

                return false;
            }

            if (!FlagOps.HasFlags(
                    ipFlags, IpFlags.IPv6, true))
            {
                if (FlagOps.HasFlags(
                        ipFlags, IpFlags.KeepErrors, true))
                {
                    error = "IPv6 is not allowed";
                }

                return false;
            }

            int haveLength = address.Length;
            int wantLength = SizeOfTwoULong;

            if (haveLength != wantLength)
            {
                if (FlagOps.HasFlags(
                        ipFlags, IpFlags.KeepErrors, true))
                {
                    error = String.Format(
                        "expected {0} address bytes, got {1}",
                        wantLength, haveLength);
                }

                return false;
            }

            return true;
        }

        ///////////////////////////////////////////////////////////////////////

        private static bool MaybeStripBrackets(
            ref string value /* in, out */
            )
        {
            if (String.IsNullOrEmpty(value))
                return false;

            int valueLength = value.Length;

            if (value[0] != Characters.OpenBracket) /* [2001:db8::1] */
                return true;

            if ((valueLength <= 2) ||
                (value[valueLength - 1] != Characters.CloseBracket))
            {
                return false;
            }

            value = value.Substring(1, valueLength - 2);
            return true;
        }

        ///////////////////////////////////////////////////////////////////////

        private static bool WordsFromIPv4(
            string value,        /* in */
            out ushort leftWord, /* out */
            out ushort rightWord /* out */
            )
        {
            leftWord = 0;
            rightWord = 0;

            IPAddress address;

            if (!IPAddress.TryParse(value, out address) ||
                (address.AddressFamily != AddressFamily.InterNetwork))
            {
                return false;
            }

            byte[] bytes = address.GetAddressBytes();

            if ((bytes == null) || (bytes.Length != sizeof(uint)))
                return false;

            leftWord = (ushort)((bytes[0] << ByteBits) | bytes[1]);
            rightWord = (ushort)((bytes[2] << ByteBits) | bytes[3]);

            return true;
        }

        ///////////////////////////////////////////////////////////////////////

        private static bool MaybeExpandIPv6(
            string value,       /* in */
            string separator,   /* in */
            int maximumLength,  /* in */
            out string[] parts, /* out */
            out int length      /* out */
            )
        {
            parts = null;
            length = Length.Invalid;

            if (String.IsNullOrEmpty(value))
                return false;

            parts = value.Split(
                new string[] { IPv6Zeros }, StringSplitOptions.None);

            if (parts == null)
                return false;

            length = parts.Length;

            if (length != 2) /* NOTE: Only one "::". */
                return false;

            string[] separators = new string[] { separator };

            string[] leftParts = parts[0].Split(
                separators, StringSplitOptions.None);

            if (leftParts == null)
                return false;

            int leftLength = leftParts.Length;

            string[] rightParts = parts[1].Split(
                separators, StringSplitOptions.None);

            if (rightParts == null)
                return false;

            int rightLength = rightParts.Length;

            if (rightLength == 0)
                return false;

            string lastPart = rightParts[rightLength - 1];

            if (lastPart == null)
                return false;

            ushort leftWord;
            ushort rightWord;

            if (WordsFromIPv4(lastPart, out leftWord, out rightWord))
            {
                rightLength++;

                if ((leftLength + rightLength) > maximumLength)
                    return false;

                Array.Resize(ref rightParts, rightLength);

                rightParts[rightLength - 2] = leftWord.ToString(IPv6Format);
                rightParts[rightLength - 1] = rightWord.ToString(IPv6Format);
            }

            parts = new string[maximumLength];

            int partsIndex = 0;

            leftParts.CopyTo(parts, partsIndex);

            partsIndex += leftLength;
            partsIndex += maximumLength - (leftLength + rightLength);

            rightParts.CopyTo(parts, partsIndex);

            partsIndex += rightLength; /* REDUNDANT */
            return true;
        }
#endif

        ///////////////////////////////////////////////////////////////////////

        private static bool IsValidCIDR(
            string pattern,        /* in */
            IpFlags ipFlags,       /* in */
            out IPAddress prefix,  /* out */
            out byte prefixLength, /* out */
            ref Result error       /* out */
            )
        {
            prefix = null;
            prefixLength = 0;

            if (String.IsNullOrEmpty(pattern))
            {
                if (FlagOps.HasFlags(
                        ipFlags, IpFlags.KeepErrors, true))
                {
                    error = "invalid CIDR pattern";
                }

                return false;
            }

            string[] parts = pattern.Split(Characters.Slash);

            if (parts == null)
            {
                if (FlagOps.HasFlags(
                        ipFlags, IpFlags.KeepErrors, true))
                {
                    error = "could not split CIDR pattern";
                }

                return false;
            }

            int length = parts.Length;

            if (length != 2)
            {
                if (FlagOps.HasFlags(
                        ipFlags, IpFlags.KeepErrors, true))
                {
                    error = String.Format(
                        "split CIDR pattern into {0}", length);
                }

                return false;
            }

            if (!byte.TryParse(parts[1], out prefixLength))
            {
                if (FlagOps.HasFlags(
                        ipFlags, IpFlags.KeepErrors, true))
                {
                    error = "bad CIDR prefix length";
                }

                return false;
            }

            prefix = GetIpAddress(
                parts[0], prefixLength, ipFlags, ref error);

            if (prefix == null)
                return false;

            AddressFamily addressFamily = prefix.AddressFamily;

            switch (addressFamily)
            {
                case AddressFamily.InterNetwork:
                    {
                        if (!IsIPv4(prefix, ipFlags, ref error))
                            return false;

                        if (prefixLength > IPv4Bits)
                        {
                            if (FlagOps.HasFlags(
                                    ipFlags, IpFlags.KeepErrors, true))
                            {
                                error = String.Format(
                                    "bad IPv4 prefix length {0}",
                                    prefixLength);
                            }

                            return false;
                        }

                        return true;
                    }
#if NET_40
                case AddressFamily.InterNetworkV6:
                    {
                        if (!IsIPv6(prefix, ipFlags, ref error))
                            return false;

                        if (prefixLength > IPv6Bits)
                        {
                            if (FlagOps.HasFlags(
                                    ipFlags, IpFlags.KeepErrors, true))
                            {
                                error = String.Format(
                                    "bad IPv6 prefix length {0}",
                                    prefixLength);
                            }

                            return false;
                        }

                        return true;
                    }
#endif
                default:
                    {
                        if (FlagOps.HasFlags(
                                ipFlags, IpFlags.KeepErrors, true))
                        {
                            error = String.Format(
                                "unsupported address family {0}",
                                addressFamily);
                        }

                        return false;
                    }
            }
        }

        ///////////////////////////////////////////////////////////////////////

        private static string ExtractAddressPrefix(
            string value,       /* in */
            byte? prefixLength, /* in */
            IpFlags ipFlags,    /* in */
            bool? wildcard,     /* in */
            ref Result error    /* out */
            )
        {
            if (String.IsNullOrEmpty(value))
            {
                if (FlagOps.HasFlags(
                        ipFlags, IpFlags.KeepErrors, true))
                {
                    error = "invalid CIDR pattern";
                }

                return null;
            }

            string separator = null;

            foreach (char? character in new char?[] {
                FlagOps.HasFlags(ipFlags, IpFlags.IPv6, true) ?
                    (char?)Characters.Colon : null,
                FlagOps.HasFlags(ipFlags, IpFlags.IPv4, true) ?
                    (char?)Characters.Period : null
                })
            {
                if (character == null)
                    continue;

                if (value.IndexOf(
                        (char)character) != Index.Invalid)
                {
                    separator = character.ToString();
                    break;
                }
            }

            if (String.IsNullOrEmpty(separator))
            {
                if (FlagOps.HasFlags(
                        ipFlags, IpFlags.KeepErrors, true))
                {
                    error = String.Format(
                        "unknown CIDR pattern separator for {0}",
                        FormatOps.WrapOrNull(ipFlags));
                }

                return null;
            }

            AddressFamily addressFamily;

#if NET_40
            bool isIPv6 = (separator[0] == Characters.Colon);

            if (isIPv6 && !MaybeStripBrackets(ref value))
            {
                if (FlagOps.HasFlags(
                        ipFlags, IpFlags.KeepErrors, true))
                {
                    error = "bad bracketed IPv6 for CIDR pattern";
                }

                return null;
            }

            addressFamily = isIPv6 ?
                AddressFamily.InterNetworkV6 :
                AddressFamily.InterNetwork;
#else
            addressFamily = AddressFamily.InterNetwork;
#endif

            int maximumLength =
#if NET_40
                isIPv6 ? IPv6Parts :
#endif
                IPv4Parts;

            string[] separators = new string[] { separator };
            string[] parts; /* REUSED */
            int length; /* REUSED */

#if NET_40
            if (isIPv6)
            {
                if (value.IndexOf(IPv6Zeros) != Index.Invalid)
                {
                    if (!MaybeExpandIPv6(
                            value, separator, maximumLength, out parts,
                            out length))
                    {
                        if (FlagOps.HasFlags(
                                ipFlags, IpFlags.KeepErrors, true))
                        {
                            error = "could not expand IPv6 for CIDR pattern";
                        }

                        return null;
                    }
                }
                else
                {
                    parts = value.Split(
                        separators, StringSplitOptions.None);

                    if (parts == null)
                    {
                        if (FlagOps.HasFlags(
                                ipFlags, IpFlags.KeepErrors, true))
                        {
                            error = "could not split IPv6 for CIDR pattern";
                        }

                        return null;
                    }

                    length = parts.Length;

                    ushort leftWord;
                    ushort rightWord;

                    if (WordsFromIPv4(
                            parts[length - 1], out leftWord, out rightWord))
                    {
                        length++;

                        if (length > maximumLength)
                        {
                            if (FlagOps.HasFlags(
                                    ipFlags, IpFlags.KeepErrors, true))
                            {
                                error = "too many IPv6 parts for CIDR pattern";
                            }

                            return null;
                        }

                        Array.Resize(ref parts, length);

                        parts[length - 2] = leftWord.ToString(IPv6Format);
                        parts[length - 1] = rightWord.ToString(IPv6Format);
                    }
                }
            }
            else
#endif
            {
                parts = value.Split(
                    separators, StringSplitOptions.None);

                if (parts == null)
                {
                    if (FlagOps.HasFlags(
                            ipFlags, IpFlags.KeepErrors, true))
                    {
                        error = "could not split IPv4 for CIDR pattern";
                    }

                    return null;
                }

                length = parts.Length;
            }

            if ((length <= 0) || (length > maximumLength))
            {
                if (FlagOps.HasFlags(
                        ipFlags, IpFlags.KeepErrors, true))
                {
                    error = "wrong number of parts for CIDR pattern";
                }

                return null;
            }

            byte localPrefixLength = (prefixLength != null) ?
                (byte)prefixLength : GetPrefixLength(addressFamily);

            if ((localPrefixLength <= 0) ||
                (localPrefixLength > maximumLength))
            {
                if (FlagOps.HasFlags(
                        ipFlags, IpFlags.KeepErrors, true))
                {
                    error = String.Format(
                        "out-of-range prefix length {0} for CIDR pattern",
                        localPrefixLength);
                }

                return null;
            }

            StringList list = new StringList();
            int index = 0;

            for (; index < Math.Min(length, localPrefixLength); index++)
            {
                string part = parts[index];

#if NET_40
                if (isIPv6)
                {
                    ushort ushortValue;

                    if (String.IsNullOrEmpty(part))
                    {
                        ushortValue = 0;
                    }
                    else if (!ushort.TryParse(
                            part, NumberStyles.HexNumber, null,
                            out ushortValue))
                    {
                        if (FlagOps.HasFlags(
                                ipFlags, IpFlags.KeepErrors, true))
                        {
                            error = String.Format(
                                "bad {0} value for IPv6", typeof(ushort));
                        }

                        return null;
                    }

                    list.Add(ushortValue.ToString(IPv6Format));
                }
                else
#endif
                {
                    byte byteValue;

                    if (String.IsNullOrEmpty(part))
                    {
                        byteValue = 0;
                    }
                    else if (!byte.TryParse(part, out byteValue))
                    {
                        if (FlagOps.HasFlags(
                                ipFlags, IpFlags.KeepErrors, true))
                        {
                            error = String.Format(
                                "bad {0} value for IPv4", typeof(byte));
                        }

                        return null;
                    }

                    list.Add(byteValue.ToString());
                }
            }

#if NET_40
            if (isIPv6)
            {
                for (; index < Math.Min(
                    maximumLength, localPrefixLength); index++)
                {
                    //
                    // NOTE: Per RFC-4291, expand all
                    //       remaining space as zeros
                    //       (IPv6).
                    //
                    list.Add(0.ToString(IPv6Format));
                }
            }
#endif

            string result;

#if NET_40
            result = String.Join(separator, list);
#else
            result = String.Join(separator, list.ToArray());
#endif

            if (((wildcard != null) && (bool)wildcard) ||
                ((wildcard == null) &&
                    (localPrefixLength < maximumLength)))
            {
                result = String.Format("{0}{1}{2}",
                    result, separator, Characters.Asterisk);
            }

            return result;
        }

        ///////////////////////////////////////////////////////////////////////

        private static bool? Match_IPv4_CIDR(
            IPAddress address, /* in */
            IPAddress prefix,  /* in */
            byte prefixLength, /* in */
            IpFlags ipFlags,   /* in */
            ref Result error   /* out */
            )
        {
            try
            {
                if (!IsIPv4(address, ipFlags, ref error))
                    return null;

                if (!IsIPv4(prefix, ipFlags, ref error))
                    return null;

                if (prefixLength > IPv4Bits)
                {
                    if (FlagOps.HasFlags(
                            ipFlags, IpFlags.KeepErrors, true))
                    {
                        error = String.Format(
                            "bad IPv4 prefix length {0}",
                            prefixLength);
                    }

                    return null;
                }

                uint maskValue;

                if (prefixLength == 0)
                {
                    maskValue = uint.MinValue;
                }
                else
                {
                    maskValue = uint.MaxValue;
                    maskValue <<= ((int)(IPv4Bits - prefixLength));
                }

                byte[] addressBytes = address.GetAddressBytes();

                if (!IsIPv4(addressBytes, ipFlags, ref error))
                    return null;

                byte[] prefixBytes = prefix.GetAddressBytes();

                if (!IsIPv4(prefixBytes, ipFlags, ref error))
                    return null;

                if (BitConverter.IsLittleEndian)
                {
                    Array.Reverse(addressBytes);
                    Array.Reverse(prefixBytes);
                }

                uint addressValue = BitConverter.ToUInt32(
                    addressBytes, 0);

                uint prefixValue = BitConverter.ToUInt32(
                    prefixBytes, 0);

                addressValue &= maskValue;
                prefixValue &= maskValue;

                return addressValue == prefixValue;
            }
            catch (Exception e)
            {
                if (FlagOps.HasFlags(
                        ipFlags, IpFlags.KeepErrors, true))
                {
                    error = e;
                }

                return null;
            }
        }

        ///////////////////////////////////////////////////////////////////////

#if NET_40
        private static BigInteger GetMaximumValueForIPv6()
        {
            /* Step #1: 0x0000000000000000FFFFFFFFFFFFFFFF */
            BigInteger result = ulong.MaxValue;

            /* Step #2: 0xFFFFFFFFFFFFFFFF0000000000000000 */
            result <<= (IPv6Bits / 2);

            /* Step #3: 0xFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFF */
            result |= ulong.MaxValue;

            return result;
        }

        ///////////////////////////////////////////////////////////////////////

        private static bool? Match_IPv6_CIDR(
            IPAddress address, /* in */
            IPAddress prefix,  /* in */
            byte prefixLength, /* in */
            IpFlags ipFlags,   /* in */
            ref Result error   /* out */
            )
        {
            try
            {
                if (!IsIPv6(address, ipFlags, ref error))
                    return null;

                if (!IsIPv6(prefix, ipFlags, ref error))
                    return null;

                if (prefixLength > IPv6Bits)
                {
                    if (FlagOps.HasFlags(
                            ipFlags, IpFlags.KeepErrors, true))
                    {
                        error = String.Format(
                            "bad IPv6 prefix length {0}",
                            prefixLength);
                    }

                    return null;
                }

                BigInteger maskValue;

                if (prefixLength == 0)
                {
                    maskValue = 0; /* two_ulong.MinValue */
                }
                else
                {
                    maskValue = GetMaximumValueForIPv6();
                    maskValue <<= ((int)(IPv6Bits - prefixLength));
                }

                byte[] addressBytes = address.GetAddressBytes();

                if (!IsIPv6(addressBytes, ipFlags, ref error))
                    return null;

                byte[] prefixBytes = prefix.GetAddressBytes();

                if (!IsIPv6(prefixBytes, ipFlags, ref error))
                    return null;

                if (BitConverter.IsLittleEndian)
                {
                    Array.Reverse(addressBytes);
                    Array.Reverse(prefixBytes);
                }

                BigInteger addressValue = new BigInteger(addressBytes);
                BigInteger prefixValue = new BigInteger(prefixBytes);

                addressValue &= maskValue;
                prefixValue &= maskValue;

                return addressValue == prefixValue;
            }
            catch (Exception e)
            {
                if (FlagOps.HasFlags(
                        ipFlags, IpFlags.KeepErrors, true))
                {
                    error = e;
                }

                return null;
            }
        }
#endif
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Public Diagnostic Methods
        public static bool IsValidCIDR(
            string pattern, /* in */
            IpFlags ipFlags /* in */
            )
        {
            IPAddress prefix; /* NOT USED */
            byte prefixLength; /* NOT USED */
            Result error = null; /* NOT USED */

            return IsValidCIDR(
                pattern, ipFlags, out prefix, out prefixLength,
                ref error);
        }

        ///////////////////////////////////////////////////////////////////////

        public static bool? MatchViaCIDR(
            string hostNameOrAddress, /* in */
            string pattern,           /* in */
            IpFlags ipFlags,          /* in */
            ref Result error          /* out */
            )
        {
            IPAddress prefix;
            byte prefixLength;

            if (!IsValidCIDR(
                    pattern, ipFlags, out prefix,
                    out prefixLength, ref error))
            {
                return null;
            }

            if (String.IsNullOrEmpty(hostNameOrAddress))
                return null;

            IPAddress address = GetIpAddress(
                hostNameOrAddress, prefixLength, ipFlags,
                ref error);

            if (address == null)
                return null;

            AddressFamily addressFamily = address.AddressFamily;

            if (addressFamily != prefix.AddressFamily)
                return null;

            if (addressFamily == AddressFamily.InterNetwork)
            {
                return Match_IPv4_CIDR(
                    address, prefix, prefixLength, ipFlags,
                    ref error);
            }
#if NET_40
            else if (addressFamily == AddressFamily.InterNetworkV6)
            {
                return Match_IPv6_CIDR(
                    address, prefix, prefixLength, ipFlags,
                    ref error);
            }
#endif
            else
            {
                return null;
            }
        }

        ///////////////////////////////////////////////////////////////////////

        public static bool? MatchViaCIDR(
            string hostNameOrAddress,     /* in */
            IEnumerable<string> patterns, /* in */
            IpFlags ipFlags,              /* in */
            ref Result error              /* out */
            )
        {
            int? index; /* NOT USED */

            return MatchViaCIDR(
                hostNameOrAddress, patterns, ipFlags, out index,
                ref error);
        }

        ///////////////////////////////////////////////////////////////////////

        public static bool? MatchViaCIDR(
            string hostNameOrAddress,     /* in */
            IEnumerable<string> patterns, /* in */
            IpFlags ipFlags,              /* in */
            out int? index,               /* in */
            ref Result error              /* out */
            )
        {
            if (patterns == null)
            {
                index = null;
                error = "invalid CIDR pattern list";

                return null;
            }

            ResultList errors = null;
            int localIndex = 0;

            foreach (string pattern in patterns)
            {
                bool? match;
                Result localError = null;

                match = MatchViaCIDR(
                    hostNameOrAddress, pattern, ipFlags,
                    ref localError);

                if (match == null)
                {
                    if (FlagOps.HasFlags(
                            ipFlags, IpFlags.StopOnError, true))
                    {
                        index = null;

                        if (FlagOps.HasFlags(
                                ipFlags, IpFlags.KeepErrors, true) &&
                            (localError != null))
                        {
                            if (errors == null)
                                errors = new ResultList();

                            errors.Add(localError);
                        }

                        if (errors != null)
                            error = errors;

                        return null;
                    }
                    else if (FlagOps.HasFlags(
                            ipFlags, IpFlags.KeepErrors, true) &&
                        (localError != null))
                    {
                        if (errors == null)
                            errors = new ResultList();

                        errors.Add(localError);
                    }

                    continue;
                }

                if ((bool)match)
                {
                    index = localIndex;
                    return true;
                }

                localIndex++;
            }

            if (errors != null)
                error = errors;

            index = null;
            return false;
        }

        ///////////////////////////////////////////////////////////////////////

        public static ReturnCode LoadForCIDR(
            string fileName,               /* in */
            byte? prefixLength,            /* in */
            IpFlags ipFlags,               /* in */
            bool? wildcard,                /* in */
            ref CidrDictionary dictionary, /* in, out */
            ref int count,                 /* in, out */
            ref Result error               /* out */
            )
        {
            string text;

            try
            {
                text = File.ReadAllText(fileName);
            }
            catch (Exception e)
            {
                if (FlagOps.HasFlags(
                        ipFlags, IpFlags.KeepErrors, true))
                {
                    error = e;
                }

                return ReturnCode.Error;
            }

            if (String.IsNullOrEmpty(text))
            {
                if (FlagOps.HasFlags(
                        ipFlags, IpFlags.ErrorOnEmpty, true))
                {
                    if (FlagOps.HasFlags(
                            ipFlags, IpFlags.KeepErrors, true))
                    {
                        error = "no CIDR text found";
                    }

                    return ReturnCode.Error;
                }
                else
                {
                    return ReturnCode.Ok;
                }
            }

            text = StringOps.NormalizeLineEndings(text);

            if (String.IsNullOrEmpty(text))
            {
                if (FlagOps.HasFlags(
                        ipFlags, IpFlags.KeepErrors, true))
                {
                    error = "could not normalize CIDR text";
                }

                return ReturnCode.Error;
            }

            string[] lines = text.Split(Characters.NewLine);

            if (lines == null)
            {
                if (FlagOps.HasFlags(
                        ipFlags, IpFlags.KeepErrors, true))
                {
                    error = "could not split CIDR text";
                }

                return ReturnCode.Error;
            }

            int localCount = 0;
            ResultList errors = null;
            Result localError; /* REUSED */

            if (dictionary == null)
                dictionary = new CidrDictionary();

            foreach (string line in lines)
            {
                if (line == null)
                    continue;

                string trimLine = line.Trim();

                if (String.IsNullOrEmpty(trimLine))
                    continue;

                if (trimLine[0] == Characters.NumberSign)
                    continue;

                localError = null;

                string prefix = ExtractAddressPrefix(
                    trimLine, prefixLength, ipFlags, wildcard,
                    ref localError);

                if (prefix == null)
                {
                    if (FlagOps.HasFlags(
                            ipFlags, IpFlags.StopOnError, true))
                    {
                        if (FlagOps.HasFlags(
                                ipFlags, IpFlags.KeepErrors, true) &&
                            (localError != null))
                        {
                            if (errors == null)
                                errors = new ResultList();

                            errors.Add(localError);
                        }

                        if (errors != null)
                            error = errors;

                        return ReturnCode.Error;
                    }
                    else if (FlagOps.HasFlags(
                            ipFlags, IpFlags.KeepErrors, true) &&
                        (localError != null))
                    {
                        if (errors == null)
                            errors = new ResultList();

                        errors.Add(localError);
                    }

                    continue;
                }

                StringList list;

                if (!dictionary.TryGetValue(prefix, out list))
                {
                    list = new StringList();
                    dictionary[prefix] = list;
                }

                list.Add(trimLine);
                localCount++;
            }

            if (FlagOps.HasFlags(
                    ipFlags, IpFlags.ErrorOnEmpty, true) &&
                (localCount == 0))
            {
                if (FlagOps.HasFlags(
                        ipFlags, IpFlags.KeepErrors, true))
                {
                    localError = "no CIDR entries added";

                    if (errors == null)
                        errors = new ResultList();

                    errors.Add(localError);
                }

                if (errors != null)
                    error = errors;

                return ReturnCode.Error;
            }

            if (errors != null)
                error = errors;

            count += localCount;
            return ReturnCode.Ok;
        }

        ///////////////////////////////////////////////////////////////////////

        public static ReturnCode UpdateVariableWithCIDR(
            Interpreter interpreter,   /* in */
            string varName,            /* in */
            CidrDictionary dictionary, /* in */
            IpFlags ipFlags,           /* in: NOT USED */
            ref Result error           /* out */
            )
        {
            if (interpreter == null)
            {
                error = "invalid interpreter";
                return ReturnCode.Error;
            }

            if (varName == null)
            {
                error = "invalid variable name";
                return ReturnCode.Error;
            }

            if (dictionary == null)
            {
                error = "invalid CIDR dictionary";
                return ReturnCode.Error;
            }

            lock (interpreter.InternalSyncRoot) /* TRANSACTIONAL */
            {
                VariableFlags variableFlags = VariableFlags.NoElement;
                IVariable variable = null;

                if (interpreter.GetVariableViaResolversWithSplit(
                        varName, ref variableFlags, ref variable,
                        ref error) != ReturnCode.Ok)
                {
                    return ReturnCode.Error;
                }

                variable = EntityOps.FollowLinks(
                    variable, variableFlags);

                if ((variable == null) ||
                    EntityOps.IsUndefined(variable))
                {
                    error = "variable is invalid or undefined";
                    return ReturnCode.Error;
                }

                if (EntityOps.IsSystem(variable))
                {
                    error = "cannot write to system variable";
                    return ReturnCode.Error;
                }

                if (EntityOps.IsReadOnlyOrInvariant(variable))
                {
                    error = "variable is not writable";
                    return ReturnCode.Error;
                }

                ElementDictionary arrayValue = null;

                if (!EntityOps.IsArray(variable, ref arrayValue))
                {
                    error = "variable is not an array";
                    return ReturnCode.Error;
                }

                foreach (CidrPair pair in dictionary)
                    arrayValue[pair.Key] = pair.Value;

                return ReturnCode.Ok;
            }
        }

        ///////////////////////////////////////////////////////////////////////

        public static ReturnCode Ping(
            string hostNameOrAddress, /* in */
            int timeout,              /* in */
            ref IPStatus status,      /* out */
            ref long roundtripTime,   /* out */
            ref Result error          /* out */
            )
        {
            try
            {
                using (Ping ping = new Ping())
                {
                    PingReply reply = ping.Send(
                        hostNameOrAddress, timeout); /* throw */

                    status = reply.Status;
                    roundtripTime = reply.RoundtripTime;
                }

                return ReturnCode.Ok;
            }
            catch (Exception e)
            {
                error = e;
                return ReturnCode.Error;
            }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Public Network Client Methods
        public static TcpClient NewTcpClient(
            string hostNameOrAddress,         /* in */
            string portNameOrNumber,          /* in */
            CultureInfo cultureInfo,          /* in: OPTIONAL */
            ref AddressFamily? addressFamily, /* in, out: OPTIONAL */
            ref Result error                  /* out */
            )
        {
            IpFlags ipFlags = IpFlags.Default |
                IpFlags.AllowAnyIp | IpFlags.AllowAnyPort;

            IPAddress address = GetIpAddress(
                hostNameOrAddress, addressFamily, null, null, ipFlags,
                ref error);

            if (address == null)
                return null;

            int port = GetPortNumber(
                portNameOrNumber, cultureInfo, ipFlags, ref error);

            if (port == Port.Invalid)
                return null;

            if (addressFamily == null)
                addressFamily = address.AddressFamily;

            return new TcpClient(new IPEndPoint(address, port));
        }

        ///////////////////////////////////////////////////////////////////////

        public static ReturnCode Connect(
            TcpClient client,             /* in */
            string hostNameOrAddress,     /* in */
            string portNameOrNumber,      /* in */
            CultureInfo cultureInfo,      /* in: OPTIONAL */
            AddressFamily? addressFamily, /* in: OPTIONAL */
            ref Result error              /* out */
            )
        {
            IpFlags ipFlags = IpFlags.Default;

            IPAddress address = GetIpAddress(
                hostNameOrAddress, addressFamily, null, null, ipFlags,
                ref error);

            if (address == null)
                return ReturnCode.Error;

            int port = GetPortNumber(
                portNameOrNumber, cultureInfo, ipFlags, ref error);

            if (port == Port.Invalid)
                return ReturnCode.Error;

            try
            {
                client.Connect(new IPEndPoint(address, port));

                TraceOps.DebugTrace(String.Format(
                    "Connect: SUCCESS {0} ==> {1}",
                    FormatOps.NetworkHostAndPort(
                        hostNameOrAddress, portNameOrNumber),
                    FormatOps.IpAddressAndPort(address, port)),
                    typeof(SocketOps).Name, TracePriority.NetworkDebug2);

                return ReturnCode.Ok;
            }
            catch (Exception e)
            {
                error = e;

                TraceOps.DebugTrace(String.Format(
                    "Connect: FAILURE {0} ==> {1}: {2}",
                    FormatOps.NetworkHostAndPort(
                        hostNameOrAddress, portNameOrNumber),
                    FormatOps.IpAddressAndPort(address, port),
                    FormatOps.WrapOrNull(error)),
                    typeof(SocketOps).Name, TracePriority.NetworkError);

                return ReturnCode.Error;
            }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Public Network Object Introspection Methods
        public static Socket GetSocket(
            NetworkStream stream /* in */
            )
        {
            try
            {
                PropertyInfo propertyInfo;

                ///////////////////////////////////////////////////////////////

                #region Static Lock Held
                lock (syncRoot) /* TRANSACTIONAL */
                {
                    //
                    // HACK: Why must we do this?  This member is marked
                    //       as "protected"; however, we need to know this
                    //       information and we do not want to derive a
                    //       custom class to get it; therefore, just use
                    //       reflection.  We cache the PropertyInfo object
                    //       so that we do not need to look it up more than
                    //       once.
                    //
                    if (networkStreamSocket == null)
                    {
                        //
                        // HACK: As of the .NET 5.0 runtime (apparently),
                        //       this property is now public; however, we
                        //       still use reflection
                        //
                        MetaBindingFlags metaBindingFlags;

                        if (CommonOps.Runtime.IsDotNetCore5xOrHigher())
                            metaBindingFlags = MetaBindingFlags.SocketPublic;
                        else
                            metaBindingFlags = MetaBindingFlags.SocketPrivate;

                        networkStreamSocket =
                            typeof(NetworkStream).GetProperty(
                                "Socket", ObjectOps.GetBindingFlags(
                                    metaBindingFlags, true));
                    }

                    propertyInfo = networkStreamSocket;
                }
                #endregion

                ///////////////////////////////////////////////////////////////

                if ((propertyInfo != null) && (stream != null))
                    return propertyInfo.GetValue(stream, null) as Socket;
            }
            catch (Exception e)
            {
                TraceOps.DebugTrace(
                    e, typeof(SocketOps).Name,
                    TracePriority.NetworkError2);
            }

            return null;
        }

        ///////////////////////////////////////////////////////////////////////

        public static bool IsListenerActive(
            TcpListener listener, /* in */
            bool @default         /* in */
            )
        {
            try
            {
                PropertyInfo propertyInfo;

                ///////////////////////////////////////////////////////////////

                #region Static Lock Held
                lock (syncRoot) /* TRANSACTIONAL */
                {
                    //
                    // HACK: Why must we do this?  This member is marked
                    //       as "protected"; however, we need to know this
                    //       information and we do not want to derive a
                    //       custom class to get it; therefore, just use
                    //       reflection.  We cache the PropertyInfo object
                    //       so that we do not need to look it up more than
                    //       once.
                    //
                    if (tcpListenerActive == null)
                    {
                        tcpListenerActive = typeof(TcpListener).GetProperty(
                            "Active", ObjectOps.GetBindingFlags(
                                MetaBindingFlags.SocketPrivate, true));
                    }

                    propertyInfo = tcpListenerActive;
                }
                #endregion

                ///////////////////////////////////////////////////////////////

                if ((propertyInfo != null) && (listener != null))
                    return (bool)propertyInfo.GetValue(listener, null);
            }
            catch (Exception e)
            {
                TraceOps.DebugTrace(
                    e, typeof(SocketOps).Name,
                    TracePriority.NetworkError2);
            }

            return @default;
        }

        ///////////////////////////////////////////////////////////////////////

        public static bool IsCleanedUp(
            Socket socket, /* in */
            bool @default  /* in */
            )
        {
            try
            {
                PropertyInfo propertyInfo;

                ///////////////////////////////////////////////////////////////

                #region Static Lock Held
                lock (syncRoot) /* TRANSACTIONAL */
                {
                    //
                    // HACK: Why must we do this?  This member is marked
                    //       as "internal"; however, we need to know this
                    //       information.  Therefore, just use reflection.
                    //       We cache the PropertyInfo object so that we
                    //       do not need to look it up more than once.
                    //
                    if (socketCleanedUp == null)
                    {
                        //
                        // HACK: The name of this property was changed in
                        //       the timeframe of .NET 5.0.
                        //
                        socketCleanedUp = typeof(Socket).GetProperty(
                            CommonOps.Runtime.IsDotNetCore5xOrHigher() ?
                                "Disposed" : "CleanedUp",
                            ObjectOps.GetBindingFlags(
                                MetaBindingFlags.SocketPrivate, true));
                    }

                    propertyInfo = socketCleanedUp;
                }
                #endregion

                ///////////////////////////////////////////////////////////////

                if ((propertyInfo != null) && (socket != null))
                    return (bool)propertyInfo.GetValue(socket, null);
            }
            catch (Exception e)
            {
                TraceOps.DebugTrace(
                    e, typeof(SocketOps).Name,
                    TracePriority.NetworkError2);
            }

            return @default;
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Private Network Server Methods
        private static void GetRemoteEndPoint(
            TcpClient client,        /* in */
            out IPEndPoint endPoint, /* out */
            ref Result error         /* out */
            )
        {
            endPoint = null;

            try
            {
                if (client == null)
                {
                    error = "invalid client";
                    return;
                }

                Socket socket = client.Client; /* throw */

                if (socket == null)
                {
                    error = "invalid client socket";
                    return;
                }

                endPoint = socket.RemoteEndPoint as IPEndPoint; /* throw */

                if (endPoint == null)
                {
                    error = "invalid remote endpoint";
                    return;
                }
            }
            catch (Exception e)
            {
                error = e;
            }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Private Network Server Methods
        private static ReturnCode GetServerScript(
            TcpClient client,    /* in */
            string channelId,    /* in */
            string text,         /* in */
            ref StringList list, /* out */
            ref Result error     /* out */
            )
        {
            IPEndPoint endPoint;

            GetRemoteEndPoint(client, out endPoint, ref error);

            if (endPoint == null)
                return ReturnCode.Error;

            StringList localList = new StringList();

            localList.Add(text);
            localList.Add(channelId);
            localList.Add(StringOps.GetStringFromObject(endPoint.Address));
            localList.Add(StringOps.GetStringFromObject(endPoint.Port));

            list = localList;
            return ReturnCode.Ok;
        }

        ///////////////////////////////////////////////////////////////////////

        private static TcpListener NewTcpListener(
            string hostNameOrAddress,     /* in */
            string portNameOrNumber,      /* in */
            CultureInfo cultureInfo,      /* in: OPTIONAL */
            AddressFamily? addressFamily, /* in: OPTIONAL */
            ref Result error              /* out */
            )
        {
            try
            {
                IpFlags ipFlags = IpFlags.Default;
                IPAddress address = null;

                if (hostNameOrAddress != null)
                {
                    address = GetIpAddress(
                        hostNameOrAddress, addressFamily, null, null,
                        ipFlags, ref error);
                }

                if ((hostNameOrAddress == null) || (address != null))
                {
                    int port = GetPortNumber(
                        portNameOrNumber, cultureInfo, ipFlags,
                        ref error);

                    if (port != Port.Invalid)
                    {
                        TcpListener listener = (address != null) ?
                            new TcpListener(address, port) :
                            new TcpListener(port);

                        TraceOps.DebugTrace(String.Format(
                            "NewTcpListener: {0} ==> {1}",
                            FormatOps.NetworkHostAndPort(
                                hostNameOrAddress, portNameOrNumber),
                            FormatOps.IpAddressAndPort(address, port)),
                            typeof(SocketOps).Name,
                            TracePriority.NetworkDebug2);

                        return listener;
                    }
                }
            }
            catch (Exception e)
            {
                error = e;
            }

            return null;
        }

        ///////////////////////////////////////////////////////////////////////

        private static void MaybeExclusiveAddressUse(
            TcpListener listener, /* in */
            bool exclusive        /* in */
            )
        {
            try
            {
                //
                // NOTE: Mono does not support this feature on Unix.
                //
                if (!CommonOps.Runtime.IsMono() ||
                    PlatformOps.IsWindowsOperatingSystem())
                {
                    listener.ExclusiveAddressUse = exclusive; /* throw */
                }
            }
            catch (Exception e)
            {
                //
                // NOTE: Mono 2.0/2.2 does not support this feature.
                //
                TraceOps.DebugTrace(
                    e, typeof(SocketOps).Name,
                    TracePriority.NetworkError);
            }
        }

        ///////////////////////////////////////////////////////////////////////

        private static int GetPollTimeout(
            Interpreter interpreter /* in: OPTIONAL */
            )
        {
            int milliseconds;

            if (interpreter != null)
            {
                milliseconds = interpreter.GetSleepTime(
                    SleepType.Socket); /* REFRESH */
            }
            else
            {
                milliseconds = EventManager.DefaultSleepTime;
            }

            return PerformanceOps.GetMicrosecondsFromMilliseconds(
                milliseconds, MinimumSocketPollTimeout,
                MaximumSocketPollTimeout);
        }

        ///////////////////////////////////////////////////////////////////////

        private static void MaybeHandleServerError(
            Interpreter interpreter,    /* in: OPTIONAL */
            SocketClientData clientData /* in */
            )
        {
            if (clientData == null)
                return;

            ReturnCode code = clientData.ReturnCode;

            if (code != ReturnCode.Ok)
            {
                Result result = clientData.Result;

                TraceOps.DebugTrace(String.Format(
                    "MaybeHandleServerError: interpreter = {0}, " +
                    "code = {1}, result = {2}",
                    FormatOps.InterpreterNoThrow(interpreter), code,
                    FormatOps.WrapOrNull(result)),
                    typeof(SocketOps).Name,
                    TracePriority.NetworkError);

                /* IGNORED */
                EventOps.HandleBackgroundError(
                    interpreter, code, result);
            }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Server Thread Support Methods
        public static void ServerThreadStart(
            object obj /* in, out */
            ) /* System.Threading.ParameterizedThreadStart */
        {
            DateTime now = TimeOps.GetUtcNow();

            try
            {
                SocketClientData clientData = obj as SocketClientData;

                if (clientData == null)
                    return; /* NOTE: There is no event to set. */

                bool setEvent = false;
                EventWaitHandle localEvent = clientData.Event;

                try
                {
                    Interpreter interpreter = clientData.Interpreter;

                    if (interpreter == null)
                    {
                        clientData.Result = "invalid interpreter";
                        clientData.ReturnCode = ReturnCode.Error;

                        return;
                    }

                    interpreter.EnterSocketThread();

                    try
                    {
                        Result result; /* REUSED */
                        TcpListener listener;

                        result = null;

                        listener = NewTcpListener(
                            clientData.Address, clientData.Port,
                            interpreter.InternalCultureInfo,
                            clientData.AddressFamily, ref result);

                        if (listener == null)
                        {
                            clientData.Result = result;
                            clientData.ReturnCode = ReturnCode.Error;

                            return;
                        }

                        /* NO RESULT */
                        MaybeExclusiveAddressUse(
                            listener, clientData.Exclusive);

                        //
                        // NOTE: So far, so good, so start listening...
                        //       This may raise an exception, e.g. if a
                        //       port is already in use, etc.
                        //
                        listener.Start(); /* throw */

                        try
                        {
                            Socket socket = listener.Server;

                            if (socket == null)
                            {
                                clientData.Result = "missing server socket";
                                clientData.ReturnCode = ReturnCode.Error;

                                return;
                            }

                            bool channelAdded = false;
                            string channelId = null;

                            try
                            {
                                //
                                // NOTE: Add the "listener" channel to the
                                //       interpreter.
                                //
                                /* NO RESULT */
                                AddServerAndSetChannel(
                                    interpreter, clientData, listener,
                                    ref channelId, ref channelAdded);

                                //
                                // NOTE: At this point, attempt to signal
                                //       the caller to receive the return
                                //       code and result that indicate our
                                //       success entering the server loop.
                                //
                                setEvent = ThreadOps.SetEvent(localEvent);

                                if (!setEvent && (localEvent != null))
                                {
                                    TraceOps.DebugTrace(
                                        "ServerThreadStart: " +
                                        "FAILED TO SIGNAL PARENT",
                                        typeof(SocketOps).Name,
                                        TracePriority.NetworkWarning);
                                }

                                //
                                // NOTE: The listener channel could not
                                //       be added to the interpreter?
                                //       Basically, this should almost
                                //       never happen, i.e. except for
                                //       during interpreter disposal,
                                //       etc.
                                //
                                if (clientData.ReturnCode != ReturnCode.Ok)
                                    return;

                                //
                                // NOTE: Poll the listener for incoming
                                //       connections.  For an incoming
                                //       connection, accept a TcpClient
                                //       and queue the supplied command
                                //       to be evaluated.
                                //
                                TraceOps.DebugTrace(
                                    "ServerThreadStart: STARTED",
                                    typeof(SocketOps).Name,
                                    TracePriority.NetworkDebug);

                                while (true)
                                {
                                    //
                                    // NOTE: If the underlying socket has been
                                    //       cleaned up (i.e. the other thread
                                    //       called [close] on it), then bail
                                    //       out now.
                                    //
                                    if (IsCleanedUp(socket, true))
                                    {
                                        TraceOps.DebugTrace(
                                            "ServerThreadStart: server " +
                                            "socket cleaned up (outer)",
                                            typeof(SocketOps).Name,
                                            TracePriority.NetworkDebug);

                                        break;
                                    }

                                    //
                                    // NOTE: If the TCP listener is no longer
                                    //       active then bail out now.
                                    //
                                    if (!IsListenerActive(listener, false))
                                    {
                                        TraceOps.DebugTrace(
                                            "ServerThreadStart: " +
                                            "listener inactive (outer)",
                                            typeof(SocketOps).Name,
                                            TracePriority.NetworkDebug);

                                        break;
                                    }

                                    int timeout = GetPollTimeout(interpreter);

                                    while (true)
                                    {
                                        if (!socket.Poll(
                                                timeout, SelectMode.SelectRead))
                                        {
                                            break;
                                        }

                                        if (IsCleanedUp(socket, true))
                                        {
                                            TraceOps.DebugTrace(
                                                "ServerThreadStart: server " +
                                                "socket cleaned up (inner)",
                                                typeof(SocketOps).Name,
                                                TracePriority.NetworkDebug);

                                            break;
                                        }

                                        if (!IsListenerActive(listener, false))
                                        {
                                            TraceOps.DebugTrace(
                                                "ServerThreadStart: " +
                                                "listener inactive (inner)",
                                                typeof(SocketOps).Name,
                                                TracePriority.NetworkDebug);

                                            break;
                                        }

                                        //
                                        // NOTE: Attempt to accept the client
                                        //       connection and deal with it.
                                        //
                                        AddClientAndQueueScript(
                                            interpreter, clientData,
                                            listener.AcceptTcpClient());

                                        MaybeHandleServerError(
                                            interpreter, clientData);
                                    }
                                }

                                TraceOps.DebugTrace(
                                    "ServerThreadStart: STOPPED",
                                    typeof(SocketOps).Name,
                                    TracePriority.NetworkDebug);
                            }
                            finally
                            {
                                if (channelAdded && (channelId != null) &&
                                    interpreter.InternalHasChannels())
                                {
                                    ReturnCode removeCode;
                                    Result removeError = null;

                                    removeCode = interpreter.RemoveChannel(
                                        channelId, ChannelType.None, false,
                                        false, false, ref removeError);

                                    if (removeCode == ReturnCode.Ok)
                                    {
                                        channelAdded = false;
                                    }
                                    else
                                    {
                                        DebugOps.Complain(
                                            interpreter, removeCode,
                                            removeError);
                                    }
                                }
                            }
                        }
                        finally
                        {
                            //
                            // NOTE: Stop listening for incoming clients,
                            //       we are done.  This call is (probably)
                            //       pointless because the only known way
                            //       we can exit the loop is by externally
                            //       stopping its channel; however, this
                            //       should be fairly harmless.
                            //
                            listener.Stop(); /* throw */
                        }
                    }
                    finally
                    {
                        interpreter.ExitSocketThread();
                    }
                }
                catch (ThreadAbortException e)
                {
                    Thread.ResetAbort();

                    clientData.Result = e;
                    clientData.ReturnCode = ReturnCode.Error;
                }
                catch (ThreadInterruptedException e)
                {
                    clientData.Result = e;
                    clientData.ReturnCode = ReturnCode.Error;
                }
                catch (Exception e)
                {
                    clientData.Result = e;
                    clientData.ReturnCode = ReturnCode.Error;
                }
                finally
                {
                    if (!setEvent)
                        ThreadOps.SetEvent(localEvent);
                }
            }
            catch (ThreadAbortException)
            {
                Thread.ResetAbort();
            }
            catch (ThreadInterruptedException)
            {
                // do nothing.
            }
            finally
            {
                TraceOps.DebugTrace(String.Format(
                    "ServerThreadStart: TIME {0}",
                    TimeOps.GetUtcNow().Subtract(now)),
                    typeof(SocketOps).Name,
                    TracePriority.NetworkDebug2);
            }
        }

        ///////////////////////////////////////////////////////////////////////

        private static void AddServerAndSetChannel(
            Interpreter interpreter,     /* in */
            SocketClientData clientData, /* in */
            TcpListener listener,        /* in */
            ref string channelId,        /* out */
            ref bool channelAdded        /* out */
            )
        {
            if (clientData == null)
                return;

            ReturnCode code = ReturnCode.Ok; /* REUSED */
            Result result = null; /* REUSED */

            try
            {
                if (interpreter == null)
                {
                    result = "invalid interpreter";
                    code = ReturnCode.Error;

                    return;
                }

                channelId = FormatOps.Id("listenSocket", null,
                    interpreter.NextId()); /* COMPAT: Eagle beta. */

                result = null;

                code = interpreter.AddTcpListenerChannel(
                    channelId, ChannelType.None, listener,
                    clientData, ref channelAdded, ref result);

                if (code != ReturnCode.Ok)
                {
                    TraceOps.DebugTrace(String.Format(
                        "AddServerAndSetChannel: " +
                        "could not add channel {0}: {1}",
                        FormatOps.WrapOrNull(channelId),
                        FormatOps.WrapOrNull(result)),
                        typeof(SocketOps).Name,
                        TracePriority.NetworkError);

                    // return; /* REDUNDANT */
                }

                result = channelId;
            }
            finally
            {
                clientData.Result = result;
                clientData.ReturnCode = code;
            }
        }

        ///////////////////////////////////////////////////////////////////////

        private static void AddClientAndQueueScript(
            Interpreter interpreter,     /* in */
            SocketClientData clientData, /* in */
            TcpClient client             /* in */
            )
        {
            if (clientData == null)
                return;

            ReturnCode code = ReturnCode.Ok; /* REUSED */
            Result result = null; /* REUSED */

            try
            {
                if (interpreter == null)
                {
                    result = "invalid interpreter";
                    code = ReturnCode.Error;

                    return;
                }

                //
                // NOTE: Create unique Id for client channel.
                //
                string channelId = FormatOps.Id(
                    "serverSocket", null,
                    interpreter.NextId()); /* COMPAT: Eagle beta. */

                //
                // NOTE: Grab underlying network stream and
                //       setup the read/write timeouts.
                //
                NetworkStream stream = client.GetStream();

                clientData.MaybeSetTimeouts(stream);

                //
                // NOTE: Add the new channel for this client
                //       to the interpreter.
                //
                result = null;

                code = interpreter.AddFileOrSocketChannel(channelId,
                    stream, clientData.Options, clientData.StreamFlags,
                    clientData.AvailableTimeout, false, false, false,
                    false, new ClientData(client), ref result);

                if (code != ReturnCode.Ok)
                {
                    TraceOps.DebugTrace(String.Format(
                        "AddClientAndQueueScript: " +
                        "could not add channel {0}: {1}",
                        FormatOps.WrapOrNull(channelId),
                        FormatOps.WrapOrNull(result)),
                        typeof(SocketOps).Name,
                        TracePriority.NetworkError);

                    return;
                }

                //
                // NOTE: Construct and queue full script when
                //       a new client connection is accepted,
                //       based on the original script fragment
                //       used by the caller.
                //
                StringList list = null;

                result = null;

                code = GetServerScript(
                    client, channelId, clientData.Text,
                    ref list, ref result);

                if (code != ReturnCode.Ok)
                {
                    TraceOps.DebugTrace(String.Format(
                        "AddClientAndQueueScript: " +
                        "could not get script {0}: {1}",
                        FormatOps.WrapOrNull(channelId),
                        FormatOps.WrapOrNull(result)),
                        typeof(SocketOps).Name,
                        TracePriority.NetworkError);

                    return;
                }

                result = null;

                code = interpreter.QueueScript(
                    TimeOps.GetUtcNow(), list.ToString(),
                    ref result);

                if (code != ReturnCode.Ok)
                {
                    TraceOps.DebugTrace(String.Format(
                        "AddClientAndQueueScript: " +
                        "could not queue script {0}: {1}",
                        FormatOps.WrapOrNull(channelId),
                        FormatOps.WrapOrNull(result)),
                        typeof(SocketOps).Name,
                        TracePriority.NetworkError);

                    // return; /* REDUNDANT */
                }
            }
            finally
            {
                clientData.Result = result;
                clientData.ReturnCode = code;
            }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Private Network Address Methods
        private static bool MakeSureNotOffline(
            string hostNameOrAddress,      /* in */
            AddressFamily? addressFamily1, /* in */
            AddressFamily? addressFamily2, /* in */
            ref Result error               /* out */
            )
        {
            if (Interlocked.CompareExchange(ref offlineLevels, 0, 0) > 0)
            {
                error = String.Format(
                    "cannot resolve {0} or {1} address {2} while offline",
                    FormatOps.WrapOrNull(addressFamily1),
                    FormatOps.WrapOrNull(addressFamily2),
                    FormatOps.NetworkHostAndPort(hostNameOrAddress, null));

                return false;
            }
            else
            {
                return true;
            }
        }

        ///////////////////////////////////////////////////////////////////////

        private static bool IsAllowedAddressFamily(
            AddressFamily addressFamily, /* in */
            IpFlags ipFlags              /* in */
            )
        {
            if ((addressFamily == AddressFamily.InterNetwork) &&
                FlagOps.HasFlags(ipFlags, IpFlags.IPv4, true))
            {
                return true;
            }

            if ((addressFamily == AddressFamily.InterNetworkV6) &&
                FlagOps.HasFlags(ipFlags, IpFlags.IPv6, true))
            {
                return true;
            }

            return false;
        }

        ///////////////////////////////////////////////////////////////////////

        private static bool DoesMatchAddressFamily(
            AddressFamily addressFamily0,  /* in */
            AddressFamily? addressFamily1, /* in: OPTIONAL */
            AddressFamily? addressFamily2  /* in: OPTIONAL */
            )
        {
            if ((addressFamily1 == null) && (addressFamily2 == null))
                return true;

            if ((addressFamily1 != null) &&
                (addressFamily0 == (AddressFamily)addressFamily1))
            {
                return true;
            }

            if ((addressFamily2 != null) &&
                (addressFamily0 == (AddressFamily)addressFamily2))
            {
                return true;
            }

            return false;
        }

        ///////////////////////////////////////////////////////////////////////

        private static byte GetMaximumPrefixLength(
            AddressFamily addressFamily /* in */
            )
        {
            if (addressFamily == AddressFamily.InterNetwork)
                return IPv4Bits;

#if NET_40
            if (addressFamily == AddressFamily.InterNetworkV6)
                return IPv6Bits;
#endif

            return 0;
        }

        ///////////////////////////////////////////////////////////////////////

        private static IPAddress GetIpAddress(
            string hostNameOrAddress, /* in */
            byte? prefixLength,       /* in */
            IpFlags ipFlags,          /* in */
            ref Result error          /* out */
            )
        {
            AddressFamily? addressFamily1 = AddressFamily.InterNetwork;
            AddressFamily? addressFamily2;

#if NET_40
            addressFamily2 = AddressFamily.InterNetworkV6;
#else
            addressFamily2 = null;
#endif

            return GetIpAddress(
                hostNameOrAddress, addressFamily1, addressFamily2,
                prefixLength, ipFlags, ref error);
        }

        ///////////////////////////////////////////////////////////////////////

        private static IPAddress GetIpAddress(
            string hostNameOrAddress,      /* in */
            AddressFamily? addressFamily1, /* in: OPTIONAL */
            AddressFamily? addressFamily2, /* in: OPTIONAL */
            byte? prefixLength,            /* in */
            IpFlags ipFlags,               /* in */
            ref Result error               /* out */
            )
        {
            IPAddress result = null;
            Result localError = null;

            if (!String.IsNullOrEmpty(hostNameOrAddress) &&
                MakeSureNotOffline(
                    hostNameOrAddress, addressFamily1, addressFamily2,
                    ref localError))
            {
                AddressFamily addressFamily0;

                if (!IPAddress.TryParse(hostNameOrAddress, out result))
                {
                    try
                    {
                        //
                        // NOTE: Attempt to resolve the host name to one
                        //       or more IP addresses. This is required
                        //       even for things like "localhost", etc.
                        //
                        IPAddress[] addresses = Dns.GetHostAddresses(
                            hostNameOrAddress);

                        if (addresses != null)
                        {
                            int length = addresses.Length;

                            for (int index = 0; index < length; index++)
                            {
                                IPAddress address = addresses[index];

                                if (address == null)
                                    continue;

                                addressFamily0 = address.AddressFamily;

                                if (!IsAllowedAddressFamily(
                                        addressFamily0, ipFlags) ||
                                    !DoesMatchAddressFamily(
                                        addressFamily0, addressFamily1,
                                        addressFamily2))
                                {
                                    continue;
                                }

                                if (prefixLength != null)
                                {
                                    byte maximumPrefixLength =
                                        GetMaximumPrefixLength(addressFamily0);

                                    if ((byte)prefixLength > maximumPrefixLength)
                                        continue;
                                }

                                result = address;
                                break;
                            }

                            if ((result == null) && FlagOps.HasFlags(
                                    ipFlags, IpFlags.KeepErrors, true))
                            {
                                localError = String.Format(
                                    "no {0} or {1} address {2}was found for {3}",
                                    FormatOps.WrapOrNull(addressFamily1),
                                    FormatOps.WrapOrNull(addressFamily2),
                                    (prefixLength != null) ? String.Format(
                                        "allowing for a prefix length of {0} ",
                                        (byte)prefixLength) : String.Empty,
                                    FormatOps.NetworkHostAndPort(
                                        hostNameOrAddress, null));
                            }
                        }
                        else if (FlagOps.HasFlags(
                                ipFlags, IpFlags.KeepErrors, true))
                        {
                            localError = String.Format(
                                "no addresses were found for {0}",
                                FormatOps.NetworkHostAndPort(
                                    hostNameOrAddress, null));
                        }
                    }
                    catch (Exception e)
                    {
                        if (FlagOps.HasFlags(
                                ipFlags, IpFlags.KeepErrors, true))
                        {
                            localError = e;
                        }
                    }
                }
                else if (result != null)
                {
                    addressFamily0 = result.AddressFamily;

                    if (!IsAllowedAddressFamily(addressFamily0, ipFlags))
                    {
                        if (FlagOps.HasFlags(
                                ipFlags, IpFlags.KeepErrors, true))
                        {
                            localError = String.Format(
                                "address family {0} is not allowed",
                                FormatOps.WrapOrNull(addressFamily0));
                        }

                        result = null;
                    }
                }
                else if (FlagOps.HasFlags(
                        ipFlags, IpFlags.KeepErrors, true))
                {
                    localError = "invalid parsed IP address";
                }
            }
            else if (FlagOps.HasFlags(ipFlags, IpFlags.AllowAnyIp, true))
            {
                result = IPAddress.Any;
            }
            else if (localError == null)
            {
                //
                // NOTE: This failure CANNOT be from MakeSureNotOffline
                //       as that would have set the local error message
                //       to something other than null.
                //
                localError = "invalid host name or IP address";
            }

            if (localError != null)
                error = localError;

            return result;
        }

        ///////////////////////////////////////////////////////////////////////

        private static int GetPortNumber(
            string portNameOrNumber, /* in */
            CultureInfo cultureInfo, /* in */
            IpFlags ipFlags,         /* in */
            ref Result error         /* out */
            )
        {
            ResultList errors = null;

            if (!String.IsNullOrEmpty(portNameOrNumber))
            {
                int port = Port.Invalid;
                Result localError; /* REUSED */

                localError = null;

                if (Value.GetInteger2(
                        portNameOrNumber, ValueFlags.AnyInteger,
                        cultureInfo, ref port,
                        ref localError) == ReturnCode.Ok)
                {
                    return port;
                }

                if (FlagOps.HasFlags(
                        ipFlags, IpFlags.KeepErrors, true) &&
                    (localError != null))
                {
                    if (errors == null)
                        errors = new ResultList();

                    errors.Add(localError);
                }

                ///////////////////////////////////////////////////////////////

#if NATIVE
                //
                // NOTE: Lookup the service name using getservbyname()
                //       API; the .NET Framework does not expose this
                //       functionality; therefore, use P/Invoke to do
                //       it ourselves.
                //
                int? nativePort;

                localError = null;

                nativePort = NativeSocket.GetPortNumberByNameAndProtocol(
                    portNameOrNumber, null, ref localError);

                if (nativePort != null)
                    return (int)nativePort;

                if (FlagOps.HasFlags(
                        ipFlags, IpFlags.KeepErrors, true) &&
                    (localError != null))
                {
                    if (errors == null)
                        errors = new ResultList();

                    errors.Add(localError);
                }
#endif
            }
            else if (FlagOps.HasFlags(ipFlags, IpFlags.AllowAnyPort, true))
            {
                return Port.Automatic;
            }

            if (errors != null)
                error = errors;

            return Port.Invalid;
        }
#endregion
    }
}
