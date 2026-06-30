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
    /// <summary>
    /// This class provides a collection of static helper methods used by the
    /// Eagle networking subsystem for working with TCP clients and listeners,
    /// resolving host names and port numbers, querying socket and listener
    /// state via reflection, and parsing, matching, and collapsing CIDR
    /// (Classless Inter-Domain Routing) patterns for both IPv4 and IPv6.
    /// </summary>
    [ObjectId("71b14766-48a0-45d5-9254-640fde03509d")]
    internal static class SocketOps
    {
        #region Private Constants
        //
        // HACK: These are no longer read-only.
        //
        /// <summary>
        /// The minimum socket poll timeout, in microseconds; when non-null, it
        /// places a lower bound on the timeout computed from the configured
        /// sleep time.
        /// </summary>
        private static int? MinimumSocketPollTimeout = 500; /* microseconds */
        /// <summary>
        /// The maximum socket poll timeout, in microseconds; when non-null, it
        /// places an upper bound on the timeout computed from the configured
        /// sleep time.
        /// </summary>
        private static int? MaximumSocketPollTimeout = null; /* microseconds */

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// The number of bits in a single byte.
        /// </summary>
        private const int ByteBits = 8;
        /// <summary>
        /// The number of parts (bytes) that make up an IPv4 address.
        /// </summary>
        private const int IPv4Parts = 4;
        /// <summary>
        /// The total number of bits in an IPv4 address.
        /// </summary>
        private const byte IPv4Bits = 32;

        ///////////////////////////////////////////////////////////////////////

        //
        // HACK: This is purposely not read-only.
        //
        /// <summary>
        /// The default IPv4 CIDR prefix length, in parts (bytes), used when an
        /// explicit prefix length is not supplied.
        /// </summary>
        private static byte IPv4PrefixLength = 1; /* 1 part(s) (byte(s)), 1 byte, 8 bits */

        ///////////////////////////////////////////////////////////////////////

#if NET_40
        //
        // HACK: This is purposely not read-only.
        //
        /// <summary>
        /// The default IPv6 CIDR prefix length, in parts (words), used when an
        /// explicit prefix length is not supplied.
        /// </summary>
        private static byte IPv6PrefixLength = 1; /* 1 part(s) (word(s)), 2 bytes, 16 bits */

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// The number of parts (16-bit words) that make up an IPv6 address.
        /// </summary>
        private const int IPv6Parts = 8;
        /// <summary>
        /// The total number of bits in an IPv6 address.
        /// </summary>
        private const byte IPv6Bits = 128;
        /// <summary>
        /// The numeric format specifier used when formatting an IPv6 word as a
        /// hexadecimal string.
        /// </summary>
        private const string IPv6Format = "x";
        /// <summary>
        /// The textual token that represents one or more contiguous groups of
        /// all-zero IPv6 words within an IPv6 address.
        /// </summary>
        private const string IPv6Zeros = "::";

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// The number of bytes occupied by two unsigned long integers, equal to
        /// the number of bytes in an IPv6 address.
        /// </summary>
        private const int SizeOfTwoULong = 2 * sizeof(ulong);

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// A bit mask, all of whose IPv6-width bits are set, used to constrain
        /// IPv6 address arithmetic to the valid address range.
        /// </summary>
        private static readonly BigInteger IPv6Mask =
            (BigInteger.One << IPv6Bits) - BigInteger.One;
#endif
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Private Static Data
        /// <summary>
        /// The object used to synchronize access to the cached reflection
        /// members and other shared mutable state of this class.
        /// </summary>
        private static readonly object syncRoot = new object();

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// The cached reflection metadata for the non-public Socket property of
        /// the NetworkStream class.
        /// </summary>
        private static PropertyInfo networkStreamSocket;
        /// <summary>
        /// The cached reflection metadata for the non-public Active property of
        /// the TcpListener class.
        /// </summary>
        private static PropertyInfo tcpListenerActive;
        /// <summary>
        /// The cached reflection metadata for the non-public property of the
        /// Socket class that indicates whether the socket has been cleaned up
        /// (disposed).
        /// </summary>
        private static PropertyInfo socketCleanedUp;

        ///////////////////////////////////////////////////////////////////////

        //
        // HACK: If this is non-zero, any attempt to create a WebClient via
        //       this class will fail, preventing any network access using
        //       the WebClient class.
        //
        /// <summary>
        /// The current offline nesting level; when greater than zero, network
        /// access via this class is disallowed.
        /// </summary>
        private static int offlineLevels = 0;
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Internal State Introspection Methods
        //
        // NOTE: Used by the _Hosts.Default.BuildEngineInfoList method.
        //
        /// <summary>
        /// This method appends a section of diagnostic information about the
        /// internal state of this class to the specified list.
        /// </summary>
        /// <param name="list">
        /// The list to which the diagnostic information is appended.  If this
        /// value is null, no action is taken.
        /// </param>
        /// <param name="detailFlags">
        /// The flags used to control how much detail is included in the
        /// diagnostic information.
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
        /// <summary>
        /// This method determines whether the specified IP address is a valid
        /// IPv4 address.
        /// </summary>
        /// <param name="address">
        /// The IP address to check.
        /// </param>
        /// <param name="ipFlags">
        /// The flags used to control validation and error handling behavior.
        /// </param>
        /// <param name="error">
        /// Upon failure, receives an error message describing why the address
        /// is not a valid IPv4 address, when error keeping is enabled.
        /// </param>
        /// <returns>
        /// True if the address is a valid IPv4 address; otherwise, false.
        /// </returns>
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

        /// <summary>
        /// This method determines whether the specified address bytes form a
        /// valid IPv4 address.
        /// </summary>
        /// <param name="address">
        /// The raw address bytes to check.
        /// </param>
        /// <param name="ipFlags">
        /// The flags used to control validation and error handling behavior.
        /// </param>
        /// <param name="error">
        /// Upon failure, receives an error message describing why the bytes do
        /// not form a valid IPv4 address, when error keeping is enabled.
        /// </param>
        /// <returns>
        /// True if the bytes form a valid IPv4 address; otherwise, false.
        /// </returns>
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

        /// <summary>
        /// This method gets the default CIDR prefix length, in parts, that is
        /// associated with the specified address family.
        /// </summary>
        /// <param name="addressFamily">
        /// The address family for which to obtain the default prefix length.
        /// </param>
        /// <returns>
        /// The default prefix length for the address family, or zero if the
        /// address family is not supported.
        /// </returns>
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
        /// <summary>
        /// This method determines whether the specified IP address is a valid
        /// IPv6 address.
        /// </summary>
        /// <param name="address">
        /// The IP address to check.
        /// </param>
        /// <param name="ipFlags">
        /// The flags used to control validation and error handling behavior.
        /// </param>
        /// <param name="error">
        /// Upon failure, receives an error message describing why the address
        /// is not a valid IPv6 address, when error keeping is enabled.
        /// </param>
        /// <returns>
        /// True if the address is a valid IPv6 address; otherwise, false.
        /// </returns>
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

        /// <summary>
        /// This method determines whether the specified address bytes form a
        /// valid IPv6 address.
        /// </summary>
        /// <param name="address">
        /// The raw address bytes to check.
        /// </param>
        /// <param name="ipFlags">
        /// The flags used to control validation and error handling behavior.
        /// </param>
        /// <param name="error">
        /// Upon failure, receives an error message describing why the bytes do
        /// not form a valid IPv6 address, when error keeping is enabled.
        /// </param>
        /// <returns>
        /// True if the bytes form a valid IPv6 address; otherwise, false.
        /// </returns>
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

        /// <summary>
        /// This method removes the surrounding square brackets from a bracketed
        /// IPv6 address string, when present.
        /// </summary>
        /// <param name="value">
        /// On input, the value that may be wrapped in square brackets; on
        /// output, the value with any surrounding brackets removed.
        /// </param>
        /// <returns>
        /// True if the value is empty after processing fails, or is a valid
        /// unbracketed or properly bracketed value; false if the value is a
        /// malformed bracketed value.
        /// </returns>
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

        /// <summary>
        /// This method parses an IPv4 address string and decomposes it into the
        /// two 16-bit words that represent it within an IPv6 address.
        /// </summary>
        /// <param name="value">
        /// The IPv4 address string to parse.
        /// </param>
        /// <param name="leftWord">
        /// Upon success, receives the high-order 16-bit word of the address.
        /// </param>
        /// <param name="rightWord">
        /// Upon success, receives the low-order 16-bit word of the address.
        /// </param>
        /// <returns>
        /// True if the value was successfully parsed as an IPv4 address;
        /// otherwise, false.
        /// </returns>
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

            byte[] addressBytes = address.GetAddressBytes();

            if ((addressBytes == null) ||
                (addressBytes.Length != sizeof(uint)))
            {
                return false;
            }

            leftWord = (ushort)((addressBytes[0] << ByteBits) |
                addressBytes[1]);

            rightWord = (ushort)((addressBytes[2] << ByteBits) |
                addressBytes[3]);

            return true;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method expands an IPv6 address string that contains a compressed
        /// run of zero words (the "::" token) into its full set of address
        /// parts.
        /// </summary>
        /// <param name="value">
        /// The IPv6 address string to expand.
        /// </param>
        /// <param name="separator">
        /// The string used to separate the individual parts of the address.
        /// </param>
        /// <param name="maximumLength">
        /// The maximum number of parts that the expanded address may contain.
        /// </param>
        /// <param name="parts">
        /// Upon success, receives the array of expanded address parts.
        /// </param>
        /// <param name="length">
        /// Upon success, receives the number of significant parts produced
        /// while expanding the address.
        /// </param>
        /// <returns>
        /// True if the value was successfully expanded; otherwise, false.
        /// </returns>
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

        /// <summary>
        /// This method determines whether the specified pattern is a valid CIDR
        /// pattern and, if so, extracts its network prefix and prefix length.
        /// </summary>
        /// <param name="pattern">
        /// The CIDR pattern to validate, in address/prefix-length form.
        /// </param>
        /// <param name="ipFlags">
        /// The flags used to control validation and error handling behavior.
        /// </param>
        /// <param name="prefix">
        /// Upon success, receives the IP address that forms the network prefix
        /// portion of the pattern.
        /// </param>
        /// <param name="prefixLength">
        /// Upon success, receives the prefix length, in bits, of the pattern.
        /// </param>
        /// <param name="error">
        /// Upon failure, receives an error message describing why the pattern is
        /// not valid, when error keeping is enabled.
        /// </param>
        /// <returns>
        /// True if the pattern is a valid CIDR pattern; otherwise, false.
        /// </returns>
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

        /// <summary>
        /// This method extracts the network prefix portion of an address,
        /// truncating it to the specified prefix length and optionally appending
        /// a wildcard component.
        /// </summary>
        /// <param name="value">
        /// The IPv4 or IPv6 address string from which to extract the prefix.
        /// </param>
        /// <param name="prefixLength">
        /// The prefix length, in parts, to retain; when null, the default
        /// prefix length for the detected address family is used.
        /// </param>
        /// <param name="ipFlags">
        /// The flags used to control validation and error handling behavior.
        /// </param>
        /// <param name="wildcard">
        /// Non-zero to force a trailing wildcard component to be appended, zero
        /// to suppress it; when null, a wildcard is appended only when the
        /// prefix length is shorter than the full address.
        /// </param>
        /// <param name="error">
        /// Upon failure, receives an error message describing why the prefix
        /// could not be extracted, when error keeping is enabled.
        /// </param>
        /// <returns>
        /// The extracted address prefix string, or null if the prefix could not
        /// be extracted.
        /// </returns>
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

        /// <summary>
        /// This method determines whether the specified IPv4 address falls
        /// within the IPv4 network described by the given prefix and prefix
        /// length.
        /// </summary>
        /// <param name="address">
        /// The IPv4 address to test.
        /// </param>
        /// <param name="prefix">
        /// The IPv4 address that forms the network prefix.
        /// </param>
        /// <param name="prefixLength">
        /// The prefix length, in bits, of the network.
        /// </param>
        /// <param name="ipFlags">
        /// The flags used to control validation and error handling behavior.
        /// </param>
        /// <param name="error">
        /// Upon failure, receives an error message describing why the match
        /// could not be performed, when error keeping is enabled.
        /// </param>
        /// <returns>
        /// True if the address falls within the network, false if it does not,
        /// or null if the match could not be performed.
        /// </returns>
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
        /// <summary>
        /// This method converts an array of address bytes, in network byte
        /// order, into a non-negative big integer.
        /// </summary>
        /// <param name="addressBytes">
        /// The address bytes to convert.
        /// </param>
        /// <returns>
        /// The non-negative big integer that represents the address bytes, or
        /// the default big integer value if the bytes are null.
        /// </returns>
        private static BigInteger FromAddressBytes(
            byte[] addressBytes /* in */
            )
        {
            if (addressBytes != null)
            {
                byte[] newAddressBytes;
                int length = addressBytes.Length;

                newAddressBytes = new byte[length + 1];

                Array.Copy(ConversionOps.Reverse(
                    addressBytes), 0, newAddressBytes, 0, length);

                return new BigInteger(newAddressBytes);
            }

            return default(BigInteger);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method determines whether the specified IPv6 address falls
        /// within the IPv6 network described by the given prefix and prefix
        /// length.
        /// </summary>
        /// <param name="address">
        /// The IPv6 address to test.
        /// </param>
        /// <param name="prefix">
        /// The IPv6 address that forms the network prefix.
        /// </param>
        /// <param name="prefixLength">
        /// The prefix length, in bits, of the network.
        /// </param>
        /// <param name="ipFlags">
        /// The flags used to control validation and error handling behavior.
        /// </param>
        /// <param name="error">
        /// Upon failure, receives an error message describing why the match
        /// could not be performed, when error keeping is enabled.
        /// </param>
        /// <returns>
        /// True if the address falls within the network, false if it does not,
        /// or null if the match could not be performed.
        /// </returns>
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
                    maskValue = IPv6Mask;
                    maskValue <<= ((int)(IPv6Bits - prefixLength));
                }

                byte[] addressBytes = address.GetAddressBytes();

                if (!IsIPv6(addressBytes, ipFlags, ref error))
                    return null;

                byte[] prefixBytes = prefix.GetAddressBytes();

                if (!IsIPv6(prefixBytes, ipFlags, ref error))
                    return null;

                BigInteger addressValue = FromAddressBytes(addressBytes);
                BigInteger prefixValue = FromAddressBytes(prefixBytes);

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
        /// <summary>
        /// This method determines whether the specified pattern is a valid CIDR
        /// pattern.
        /// </summary>
        /// <param name="pattern">
        /// The CIDR pattern to validate, in address/prefix-length form.
        /// </param>
        /// <param name="ipFlags">
        /// The flags used to control validation and error handling behavior.
        /// </param>
        /// <returns>
        /// True if the pattern is a valid CIDR pattern; otherwise, false.
        /// </returns>
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

        /// <summary>
        /// This method determines whether the specified host name or address
        /// matches the given CIDR pattern.
        /// </summary>
        /// <param name="hostNameOrAddress">
        /// The host name or IP address to test against the pattern.
        /// </param>
        /// <param name="pattern">
        /// The CIDR pattern to match against, in address/prefix-length form.
        /// </param>
        /// <param name="ipFlags">
        /// The flags used to control validation and error handling behavior.
        /// </param>
        /// <param name="error">
        /// Upon failure, receives an error message describing why the match
        /// could not be performed, when error keeping is enabled.
        /// </param>
        /// <returns>
        /// True if the host name or address matches the pattern, false if it
        /// does not, or null if the match could not be performed.
        /// </returns>
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

        /// <summary>
        /// This method determines whether the specified host name or address
        /// matches any of the given CIDR patterns.
        /// </summary>
        /// <param name="hostNameOrAddress">
        /// The host name or IP address to test against the patterns.
        /// </param>
        /// <param name="patterns">
        /// The collection of CIDR patterns to match against.
        /// </param>
        /// <param name="ipFlags">
        /// The flags used to control validation and error handling behavior.
        /// </param>
        /// <param name="error">
        /// Upon failure, receives an error message describing why the match
        /// could not be performed, when error keeping is enabled.
        /// </param>
        /// <returns>
        /// True if the host name or address matches one of the patterns, false
        /// if it matches none of them, or null if the match could not be
        /// performed.
        /// </returns>
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

        /// <summary>
        /// This method determines whether the specified host name or address
        /// matches any of the given CIDR patterns, reporting the index of the
        /// matching pattern.
        /// </summary>
        /// <param name="hostNameOrAddress">
        /// The host name or IP address to test against the patterns.
        /// </param>
        /// <param name="patterns">
        /// The collection of CIDR patterns to match against.
        /// </param>
        /// <param name="ipFlags">
        /// The flags used to control validation and error handling behavior.
        /// </param>
        /// <param name="index">
        /// Upon a successful match, receives the zero-based index of the
        /// matching pattern; otherwise, receives null.
        /// </param>
        /// <param name="error">
        /// Upon failure, receives an error message describing why the match
        /// could not be performed, when error keeping is enabled.
        /// </param>
        /// <returns>
        /// True if the host name or address matches one of the patterns, false
        /// if it matches none of them, or null if the match could not be
        /// performed.
        /// </returns>
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

        /// <summary>
        /// This method reads CIDR patterns from the specified file, groups them
        /// by their extracted address prefix, and adds them to the supplied
        /// dictionary.
        /// </summary>
        /// <param name="fileName">
        /// The name of the file containing the CIDR patterns to load, one per
        /// line, with blank lines and lines starting with a number sign treated
        /// as comments.
        /// </param>
        /// <param name="prefixLength">
        /// The prefix length, in parts, used when extracting each address
        /// prefix; when null, the default prefix length is used.
        /// </param>
        /// <param name="ipFlags">
        /// The flags used to control validation and error handling behavior.
        /// </param>
        /// <param name="wildcard">
        /// Non-zero to force a trailing wildcard component to be appended to
        /// each extracted prefix, zero to suppress it, or null to use the
        /// default behavior.
        /// </param>
        /// <param name="dictionary">
        /// On input, the dictionary to populate, which is created if null; on
        /// output, the dictionary mapping each extracted prefix to the list of
        /// original patterns that produced it.
        /// </param>
        /// <param name="count">
        /// On input, a running count of loaded patterns; on output, the count
        /// is increased by the number of patterns added by this call.
        /// </param>
        /// <param name="error">
        /// Upon failure, receives an error message describing why the patterns
        /// could not be loaded.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise, an appropriate
        /// error code.
        /// </returns>
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

        /// <summary>
        /// This method stores the entries of the specified CIDR dictionary into
        /// the array variable with the given name in the specified interpreter.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter that contains the target array variable.
        /// </param>
        /// <param name="varName">
        /// The name of the array variable to update.
        /// </param>
        /// <param name="dictionary">
        /// The CIDR dictionary whose entries are stored as array elements.
        /// </param>
        /// <param name="ipFlags">
        /// The flags used to control behavior.  This parameter is not used.
        /// </param>
        /// <param name="error">
        /// Upon failure, receives an error message describing why the variable
        /// could not be updated.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise, an appropriate
        /// error code.
        /// </returns>
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

        /// <summary>
        /// This method sends an ICMP echo request to the specified host and
        /// reports the resulting status and round-trip time.
        /// </summary>
        /// <param name="hostNameOrAddress">
        /// The host name or IP address to ping.
        /// </param>
        /// <param name="timeout">
        /// The maximum number of milliseconds to wait for a reply.
        /// </param>
        /// <param name="status">
        /// Upon success, receives the status of the ping attempt.
        /// </param>
        /// <param name="roundtripTime">
        /// Upon success, receives the round-trip time, in milliseconds, of the
        /// ping attempt.
        /// </param>
        /// <param name="error">
        /// Upon failure, receives an error message describing why the ping
        /// failed.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise, an appropriate
        /// error code.
        /// </returns>
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
        /// <summary>
        /// This method creates a new TCP client bound to a local endpoint that
        /// is resolved from the specified host name or address and port name or
        /// number.
        /// </summary>
        /// <param name="hostNameOrAddress">
        /// The host name or IP address used to resolve the local endpoint.
        /// </param>
        /// <param name="portNameOrNumber">
        /// The port name or number used to resolve the local endpoint.
        /// </param>
        /// <param name="cultureInfo">
        /// The culture used when parsing the port number.  This parameter is
        /// optional and may be null.
        /// </param>
        /// <param name="keepAlive">
        /// Non-zero to enable the keep-alive socket option, zero to disable it,
        /// or null to leave it unchanged.  This parameter is optional.
        /// </param>
        /// <param name="addressFamily">
        /// On input, the preferred address family used to resolve the address,
        /// which may be null; on output, the address family of the resolved
        /// address.  This parameter is optional.
        /// </param>
        /// <param name="error">
        /// Upon failure, receives an error message describing why the client
        /// could not be created.
        /// </param>
        /// <returns>
        /// The newly created TCP client, or null if it could not be created.
        /// </returns>
        public static TcpClient NewTcpClient(
            string hostNameOrAddress,         /* in */
            string portNameOrNumber,          /* in */
            CultureInfo cultureInfo,          /* in: OPTIONAL */
            bool? keepAlive,                  /* in: OPTIONAL */
            ref AddressFamily? addressFamily, /* in, out: OPTIONAL */
            ref Result error                  /* out */
            )
        {
            IpFlags ipFlags = IpFlags.Default |
                IpFlags.AllowAnyIp | IpFlags.AllowAnyPort;

            IPAddress address = GetIpAddress(
                hostNameOrAddress, addressFamily, null, null,
                ipFlags, ref error);

            if (address == null)
                return null;

            int port = GetPortNumber(
                portNameOrNumber, cultureInfo, ipFlags,
                ref error);

            if (port == Port.Invalid)
                return null;

            if (addressFamily == null)
                addressFamily = address.AddressFamily;

            TcpClient client = new TcpClient(
                new IPEndPoint(address, port));

            if (keepAlive != null)
            {
                Socket socket = client.Client; /* throw */

                if (socket == null)
                {
                    error = String.Format(
                        "invalid client socket for {0}:{1}",
                        FormatOps.MaybeNull(address), port);

                    return null;
                }

                /* NO RESULT */
                socket.SetSocketOption(
                    SocketOptionLevel.Socket,
                    SocketOptionName.KeepAlive,
                    (bool)keepAlive);
            }

            return client;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method connects the specified TCP client to the remote endpoint
        /// resolved from the given host name or address and port name or number.
        /// </summary>
        /// <param name="client">
        /// The TCP client to connect.
        /// </param>
        /// <param name="hostNameOrAddress">
        /// The host name or IP address used to resolve the remote endpoint.
        /// </param>
        /// <param name="portNameOrNumber">
        /// The port name or number used to resolve the remote endpoint.
        /// </param>
        /// <param name="cultureInfo">
        /// The culture used when parsing the port number.  This parameter is
        /// optional and may be null.
        /// </param>
        /// <param name="addressFamily">
        /// The preferred address family used to resolve the address.  This
        /// parameter is optional and may be null.
        /// </param>
        /// <param name="error">
        /// Upon failure, receives an error message describing why the connection
        /// could not be established.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise, an appropriate
        /// error code.
        /// </returns>
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
        /// <summary>
        /// This method obtains the underlying socket associated with the
        /// specified network stream, using reflection to access the non-public
        /// property when necessary.
        /// </summary>
        /// <param name="stream">
        /// The network stream whose underlying socket is to be obtained.
        /// </param>
        /// <returns>
        /// The underlying socket, or null if it could not be obtained.
        /// </returns>
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

        /// <summary>
        /// This method determines whether the specified TCP listener is active,
        /// using reflection to access the non-public property.
        /// </summary>
        /// <param name="listener">
        /// The TCP listener to query.
        /// </param>
        /// <param name="default">
        /// The value to return if the active state cannot be determined.
        /// </param>
        /// <returns>
        /// True if the listener is active, false if it is not, or the value of
        /// <paramref name="default" /> if the state could not be determined.
        /// </returns>
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

        /// <summary>
        /// This method determines whether the specified socket has been cleaned
        /// up (disposed), using reflection to access the non-public property.
        /// </summary>
        /// <param name="socket">
        /// The socket to query.
        /// </param>
        /// <param name="default">
        /// The value to return if the cleaned-up state cannot be determined.
        /// </param>
        /// <returns>
        /// True if the socket has been cleaned up, false if it has not, or the
        /// value of <paramref name="default" /> if the state could not be
        /// determined.
        /// </returns>
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
        /// <summary>
        /// This method obtains the remote IP endpoint associated with the
        /// specified connected TCP client.
        /// </summary>
        /// <param name="client">
        /// The connected TCP client whose remote endpoint is to be obtained.
        /// </param>
        /// <param name="endPoint">
        /// Upon success, receives the remote IP endpoint of the client.
        /// </param>
        /// <param name="error">
        /// Upon failure, receives an error message describing why the remote
        /// endpoint could not be obtained.
        /// </param>
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
        /// <summary>
        /// This method builds the list of arguments that form the server script
        /// to evaluate for a newly accepted client connection.
        /// </summary>
        /// <param name="client">
        /// The newly accepted client whose remote endpoint is included in the
        /// script arguments.
        /// </param>
        /// <param name="channelId">
        /// The identifier of the channel created for the client.
        /// </param>
        /// <param name="text">
        /// The original script fragment supplied by the caller.
        /// </param>
        /// <param name="list">
        /// Upon success, receives the list of script arguments.
        /// </param>
        /// <param name="error">
        /// Upon failure, receives an error message describing why the script
        /// could not be built.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise, an appropriate
        /// error code.
        /// </returns>
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

        /// <summary>
        /// This method creates a new TCP listener bound to the endpoint resolved
        /// from the specified host name or address and port name or number.
        /// </summary>
        /// <param name="hostNameOrAddress">
        /// The host name or IP address on which to listen; when null, the
        /// listener binds to any address.
        /// </param>
        /// <param name="portNameOrNumber">
        /// The port name or number on which to listen.
        /// </param>
        /// <param name="cultureInfo">
        /// The culture used when parsing the port number.  This parameter is
        /// optional and may be null.
        /// </param>
        /// <param name="addressFamily">
        /// The preferred address family used to resolve the address.  This
        /// parameter is optional and may be null.
        /// </param>
        /// <param name="error">
        /// Upon failure, receives an error message describing why the listener
        /// could not be created.
        /// </param>
        /// <returns>
        /// The newly created TCP listener, or null if it could not be created.
        /// </returns>
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

        /// <summary>
        /// This method attempts to set the exclusive address use option on the
        /// specified TCP listener, ignoring failures on platforms that do not
        /// support the feature.
        /// </summary>
        /// <param name="listener">
        /// The TCP listener whose exclusive address use option is to be set.
        /// </param>
        /// <param name="exclusive">
        /// Non-zero to require exclusive use of the address; otherwise, zero.
        /// </param>
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

        /// <summary>
        /// This method computes the socket poll timeout, in microseconds, based
        /// on the sleep time configured for the specified interpreter, clamped to
        /// the configured minimum and maximum bounds.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter whose configured socket sleep time is used; when
        /// null, the default sleep time is used.  This parameter is optional.
        /// </param>
        /// <returns>
        /// The poll timeout, in microseconds.
        /// </returns>
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

        /// <summary>
        /// This method reports any error recorded on the specified socket client
        /// data as a background error in the given interpreter.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter used to handle the background error.  This parameter
        /// is optional and may be null.
        /// </param>
        /// <param name="clientData">
        /// The socket client data that carries the return code and result to be
        /// checked for an error.
        /// </param>
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
        /// <summary>
        /// This method is the entry point for the background thread that runs a
        /// TCP server: it creates and starts a listener, accepts incoming client
        /// connections, and queues the configured script for each one until the
        /// listener is stopped.  This method conforms to the
        /// System.Threading.ParameterizedThreadStart delegate signature.
        /// </summary>
        /// <param name="obj">
        /// The socket client data that configures the server, carrying the
        /// interpreter, address, port, and other settings, and receiving the
        /// resulting return code and result.
        /// </param>
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

                            bool? keepAlive = clientData.KeepAlive;

                            if (keepAlive != null)
                            {
                                /* NO RESULT */
                                socket.SetSocketOption(
                                    SocketOptionLevel.Socket,
                                    SocketOptionName.KeepAlive,
                                    (bool)keepAlive);
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

        /// <summary>
        /// This method adds a listener channel for the specified TCP listener to
        /// the given interpreter and records the outcome on the supplied socket
        /// client data.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter to which the listener channel is added.
        /// </param>
        /// <param name="clientData">
        /// The socket client data that receives the resulting return code and
        /// result.  If this value is null, no action is taken.
        /// </param>
        /// <param name="listener">
        /// The TCP listener for which the channel is created.
        /// </param>
        /// <param name="channelId">
        /// Upon success, receives the identifier of the newly added channel.
        /// </param>
        /// <param name="channelAdded">
        /// Upon return, indicates whether the channel was actually added to the
        /// interpreter.
        /// </param>
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

        /// <summary>
        /// This method adds a channel for a newly accepted client to the
        /// specified interpreter and queues the configured server script to be
        /// evaluated for that client.
        /// </summary>
        /// <param name="interpreter">
        /// The interpreter to which the client channel is added and in which the
        /// script is queued.
        /// </param>
        /// <param name="clientData">
        /// The socket client data that supplies the channel configuration and
        /// receives the resulting return code and result.  If this value is
        /// null, no action is taken.
        /// </param>
        /// <param name="client">
        /// The newly accepted client connection to set up.
        /// </param>
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
        /// <summary>
        /// This method verifies that this class is not currently in the offline
        /// state before allowing an address resolution to proceed.
        /// </summary>
        /// <param name="hostNameOrAddress">
        /// The host name or address being resolved, included in any error
        /// message.
        /// </param>
        /// <param name="addressFamily1">
        /// The first candidate address family, included in any error message.
        /// </param>
        /// <param name="addressFamily2">
        /// The second candidate address family, included in any error message.
        /// </param>
        /// <param name="error">
        /// Upon failure, receives an error message indicating that resolution is
        /// not allowed while offline.
        /// </param>
        /// <returns>
        /// True if resolution is allowed; false if this class is offline.
        /// </returns>
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

        /// <summary>
        /// This method determines whether the specified address family is
        /// permitted by the given flags.
        /// </summary>
        /// <param name="addressFamily">
        /// The address family to check.
        /// </param>
        /// <param name="ipFlags">
        /// The flags that indicate which address families are permitted.
        /// </param>
        /// <returns>
        /// True if the address family is permitted; otherwise, false.
        /// </returns>
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

        /// <summary>
        /// This method determines whether the specified address family matches
        /// one of the candidate address families.
        /// </summary>
        /// <param name="addressFamily0">
        /// The address family to test.
        /// </param>
        /// <param name="addressFamily1">
        /// The first candidate address family.  This parameter is optional and
        /// may be null.
        /// </param>
        /// <param name="addressFamily2">
        /// The second candidate address family.  This parameter is optional and
        /// may be null.
        /// </param>
        /// <returns>
        /// True if both candidates are null, or if the address family matches a
        /// non-null candidate; otherwise, false.
        /// </returns>
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

        /// <summary>
        /// This method gets the maximum CIDR prefix length, in bits, that is
        /// valid for the specified address family.
        /// </summary>
        /// <param name="addressFamily">
        /// The address family for which to obtain the maximum prefix length.
        /// </param>
        /// <returns>
        /// The maximum prefix length for the address family, or zero if the
        /// address family is not supported.
        /// </returns>
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

        /// <summary>
        /// This method resolves the specified host name or address to an IP
        /// address, allowing both IPv4 and IPv6 candidate address families.
        /// </summary>
        /// <param name="hostNameOrAddress">
        /// The host name or IP address to resolve.
        /// </param>
        /// <param name="prefixLength">
        /// The CIDR prefix length, in bits, that the resolved address must be
        /// able to accommodate; when null, no prefix length constraint is
        /// applied.
        /// </param>
        /// <param name="ipFlags">
        /// The flags used to control validation and error handling behavior.
        /// </param>
        /// <param name="error">
        /// Upon failure, receives an error message describing why the address
        /// could not be resolved, when error keeping is enabled.
        /// </param>
        /// <returns>
        /// The resolved IP address, or null if it could not be resolved.
        /// </returns>
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

        /// <summary>
        /// This method resolves the specified host name or address to an IP
        /// address that matches one of the candidate address families and
        /// satisfies the given flags and prefix length constraint.
        /// </summary>
        /// <param name="hostNameOrAddress">
        /// The host name or IP address to resolve.
        /// </param>
        /// <param name="addressFamily1">
        /// The first acceptable address family.  This parameter is optional and
        /// may be null.
        /// </param>
        /// <param name="addressFamily2">
        /// The second acceptable address family.  This parameter is optional and
        /// may be null.
        /// </param>
        /// <param name="prefixLength">
        /// The CIDR prefix length, in bits, that the resolved address must be
        /// able to accommodate; when null, no prefix length constraint is
        /// applied.
        /// </param>
        /// <param name="ipFlags">
        /// The flags used to control validation and error handling behavior.
        /// </param>
        /// <param name="error">
        /// Upon failure, receives an error message describing why the address
        /// could not be resolved, when error keeping is enabled.
        /// </param>
        /// <returns>
        /// The resolved IP address, or null if it could not be resolved.
        /// </returns>
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

        /// <summary>
        /// This method resolves the specified port name or number to a numeric
        /// port, parsing it as an integer and, when native support is available,
        /// falling back to a service name lookup.
        /// </summary>
        /// <param name="portNameOrNumber">
        /// The port number or service name to resolve.
        /// </param>
        /// <param name="cultureInfo">
        /// The culture used when parsing the port number.
        /// </param>
        /// <param name="ipFlags">
        /// The flags used to control validation and error handling behavior.
        /// </param>
        /// <param name="error">
        /// Upon failure, receives an error message describing why the port could
        /// not be resolved, when error keeping is enabled.
        /// </param>
        /// <returns>
        /// The resolved port number, or an invalid port value if it could not be
        /// resolved.
        /// </returns>
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

        ///////////////////////////////////////////////////////////////////////

        #region CIDR Range IPv4 Methods
        #region CIDR Range IPv4 Structure
        /// <summary>
        /// This structure represents a contiguous range of IPv4 addresses
        /// derived from a CIDR pattern, used when collapsing and merging
        /// multiple IPv4 CIDR ranges.
        /// </summary>
        [ObjectId("314a76e0-da3b-4015-b9cb-43564db30cb0")]
        private struct CIDR_Range_IPv4
        {
            /// <summary>
            /// The prefix length, in bits, of the originating CIDR pattern.
            /// </summary>
            internal byte PrefixLength;
            /// <summary>
            /// The first IPv4 address in the range, as an unsigned integer.
            /// </summary>
            internal uint Start;
            /// <summary>
            /// The last IPv4 address in the range, as an unsigned integer.
            /// </summary>
            internal uint End;
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region IComparer<CIDR_Range_IPv4> Helper Class
        /// <summary>
        /// This class compares two IPv4 address ranges, ordering them by their
        /// start address and then by their end address, so that a list of ranges
        /// can be sorted prior to merging.
        /// </summary>
        [ObjectId("4fa3431c-d634-4e05-a2b6-1f39b6670075")]
        private sealed class CIDR_Range_IPv4_Comparer :
                IComparer<CIDR_Range_IPv4>
        {
            #region IComparer<CIDR_Range_IPv4> Overrides
            /// <summary>
            /// This method compares two IPv4 address ranges.
            /// </summary>
            /// <param name="x">
            /// The first IPv4 address range to compare.
            /// </param>
            /// <param name="y">
            /// The second IPv4 address range to compare.
            /// </param>
            /// <returns>
            /// A negative number if <paramref name="x" /> precedes
            /// <paramref name="y" />, zero if they are equal, or a positive
            /// number if <paramref name="x" /> follows <paramref name="y" />.
            /// </returns>
            public int Compare(
                CIDR_Range_IPv4 x, /* in */
                CIDR_Range_IPv4 y  /* in */
                )
            {
                int result = x.Start.CompareTo(y.Start);

                if (result != 0)
                    return result;

                return x.End.CompareTo(y.End);
            }
            #endregion
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method computes the IPv4 network mask that corresponds to the
        /// specified prefix length.
        /// </summary>
        /// <param name="prefixLength">
        /// The prefix length, in bits, for which to compute the mask.
        /// </param>
        /// <returns>
        /// The network mask, as an unsigned integer.
        /// </returns>
        private static uint GetMask_IPv4(
            byte prefixLength /* in */
            )
        {
            if (prefixLength == 0)
                return 0;

            return uint.MaxValue << (IPv4Bits - prefixLength);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method counts the number of consecutive zero bits at the
        /// least-significant end of the specified unsigned integer.
        /// </summary>
        /// <param name="value">
        /// The value whose trailing zero bits are to be counted.
        /// </param>
        /// <returns>
        /// The number of trailing zero bits, or the total number of bits in the
        /// value if it is zero.
        /// </returns>
        private static int CountTrailingZeros(
            uint value /* in */
            )
        {
            if (value == 0)
                return sizeof(uint) * ByteBits;

            int count = 0;

            while ((value & 1) == 0)
            {
                count++;
                value >>= 1;
            }

            return count;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method computes the floor of the base-2 logarithm of the
        /// specified unsigned long value.
        /// </summary>
        /// <param name="value">
        /// The value whose floored base-2 logarithm is to be computed.
        /// </param>
        /// <returns>
        /// The zero-based position of the most-significant set bit, or negative
        /// one if the value is zero.
        /// </returns>
        private static int FloorLog2(
            ulong value /* in */
            )
        {
            int position = -1;

            while (value != 0)
            {
                value >>= 1;
                position++;
            }

            return position;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method converts the specified unsigned integer into a four-byte
        /// IPv4 address byte array in network byte order.
        /// </summary>
        /// <param name="value">
        /// The IPv4 address value to convert.
        /// </param>
        /// <returns>
        /// The four-byte address array, in network byte order.
        /// </returns>
        private static byte[] ToAddressBytes(
            uint value /* in */
            )
        {
            byte[] addressBytes = new byte[sizeof(uint)];

            addressBytes[0] = (byte)((value >> 24) & 0xFF);
            addressBytes[1] = (byte)((value >> 16) & 0xFF);
            addressBytes[2] = (byte)((value >> 8) & 0xFF);
            addressBytes[3] = (byte)(value & 0xFF);

            return addressBytes;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method converts the specified IPv4 CIDR pattern into the
        /// contiguous range of addresses that it represents.
        /// </summary>
        /// <param name="pattern">
        /// The IPv4 CIDR pattern to convert, in address/prefix-length form.
        /// </param>
        /// <param name="ipFlags">
        /// The flags used to control validation and error handling behavior.
        /// </param>
        /// <param name="range">
        /// Upon success, receives the IPv4 address range represented by the
        /// pattern.
        /// </param>
        /// <param name="error">
        /// Upon failure, receives an error message describing why the range
        /// could not be computed.
        /// </param>
        /// <returns>
        /// True if the range was successfully computed; otherwise, false.
        /// </returns>
        private static bool RangeFromCIDR(
            string pattern,            /* in */
            IpFlags ipFlags,           /* in */
            ref CIDR_Range_IPv4 range, /* out */
            ref Result error           /* out */
            )
        {
            IPAddress prefix;
            byte prefixLength;

            if (!IsValidCIDR(
                    pattern, ipFlags, out prefix, out prefixLength,
                    ref error))
            {
                return false;
            }

            if (prefix.AddressFamily != AddressFamily.InterNetwork)
            {
                error = "invalid IPv4 CIDR: unexpected address family";
                return false;
            }

            if (prefixLength > IPv4Bits)
            {
                error = "invalid IPv4 CIDR prefix length";
                return false;
            }

            byte[] addressBytes = prefix.GetAddressBytes();

            uint address = ((uint)addressBytes[0] << 24) |
                           ((uint)addressBytes[1] << 16) |
                           ((uint)addressBytes[2] << 8) |
                           ((uint)addressBytes[3]);

            uint mask = GetMask_IPv4(prefixLength);
            uint start = address & mask;
            uint end = start | ~mask;

            range.PrefixLength = prefixLength;
            range.Start = start;
            range.End = end;

            return true;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method decomposes the specified IPv4 address range into the
        /// minimal set of CIDR patterns that cover it, appending each pattern to
        /// the supplied output list.
        /// </summary>
        /// <param name="range">
        /// The IPv4 address range to decompose into CIDR patterns.
        /// </param>
        /// <param name="ipFlags">
        /// The flags used to control behavior.
        /// </param>
        /// <param name="output">
        /// The list to which the resulting CIDR patterns are appended.
        /// </param>
        /// <param name="error">
        /// Upon failure, receives an error message describing why the range
        /// could not be decomposed.
        /// </param>
        private static void RangeToCIDR(
            CIDR_Range_IPv4 range, /* in */
            IpFlags ipFlags,       /* in */
            StringList output,     /* in, out */
            ref Result error       /* out */
            )
        {
            uint current = range.Start;
            uint end = range.End;

            while (current <= end)
            {
                int suffixBits = CountTrailingZeros(current);
                int prefixBits = IPv4Bits - suffixBits;

                ulong remaining = (ulong)end - (ulong)current + 1;

                int remainingBits = FloorLog2(remaining);
                int remainingPrefixBits = IPv4Bits - remainingBits;

                if (remainingPrefixBits > prefixBits)
                    prefixBits = remainingPrefixBits;

                if (prefixBits < 0)
                    prefixBits = 0;

                if (prefixBits > IPv4Bits)
                    prefixBits = IPv4Bits;

                byte[] addressBytes = ToAddressBytes(current);

                output.Add(String.Format(
                    "{0}/{1}", new IPAddress(addressBytes), prefixBits));

                uint blockMask = GetMask_IPv4((byte)prefixBits);
                uint blockEnd = current | ~blockMask;

                if (blockEnd == uint.MaxValue)
                    break;

                current = blockEnd + 1;
            }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method merges a list of IPv4 address ranges, combining ranges
        /// that overlap or are adjacent into a smaller set of non-overlapping
        /// ranges.
        /// </summary>
        /// <param name="ranges">
        /// The list of IPv4 address ranges to merge.
        /// </param>
        /// <returns>
        /// A new list containing the merged, non-overlapping IPv4 address
        /// ranges.
        /// </returns>
        private static List<CIDR_Range_IPv4> MergeRanges(
            List<CIDR_Range_IPv4> ranges /* in */
            )
        {
            List<CIDR_Range_IPv4> merged = new List<CIDR_Range_IPv4>();

            if (ranges == null)
                return merged;

            int count = ranges.Count;

            if (count == 0)
                return merged;

            ranges.Sort(new CIDR_Range_IPv4_Comparer());

            CIDR_Range_IPv4 current = ranges[0];

            for (int index = 1; index < count; index++)
            {
                CIDR_Range_IPv4 next = ranges[index];

                if ((ulong)next.Start <= (ulong)current.End + 1)
                {
                    if (next.End > current.End)
                        current.End = next.End;
                }
                else
                {
                    merged.Add(current);
                    current = next;
                }
            }

            merged.Add(current);
            return merged;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method collapses a collection of IPv4 CIDR patterns into the
        /// minimal equivalent set of CIDR patterns by converting them to ranges,
        /// merging overlapping or adjacent ranges, and converting the merged
        /// ranges back to patterns.
        /// </summary>
        /// <param name="patterns">
        /// The collection of IPv4 CIDR patterns to collapse.
        /// </param>
        /// <param name="ipFlags">
        /// The flags used to control validation, sorting, and error handling
        /// behavior.
        /// </param>
        /// <param name="merged">
        /// On input, an optional existing list to which the collapsed patterns
        /// are appended; on output, the list of collapsed patterns, created if
        /// it was null.
        /// </param>
        /// <param name="error">
        /// Upon failure, receives an error message describing why the patterns
        /// could not be collapsed.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise, an appropriate
        /// error code.
        /// </returns>
        public static ReturnCode Collapse_IPv4_CIDR(
            IEnumerable<string> patterns, /* in */
            IpFlags ipFlags,              /* in */
            ref StringList merged,        /* in, out */
            ref Result error              /* out */
            )
        {
            if (patterns == null)
            {
                error = "invalid CIDR list";
                return ReturnCode.Error;
            }

            List<CIDR_Range_IPv4> ranges = new List<CIDR_Range_IPv4>();

            foreach (string pattern in patterns)
            {
                if (pattern == null)
                    continue;

                CIDR_Range_IPv4 range = default(CIDR_Range_IPv4);

                if (!RangeFromCIDR(
                        pattern, ipFlags, ref range, ref error))
                {
                    return ReturnCode.Error;
                }

                ranges.Add(range);
            }

            List<CIDR_Range_IPv4> localMerged = MergeRanges(ranges);
            int count = localMerged.Count;
            StringList output = new StringList();

            for (int index = 0; index < count; index++)
            {
                Result localError = null;

                RangeToCIDR(
                    localMerged[index], ipFlags, output,
                    ref localError);

                if (localError != null)
                {
                    error = localError;
                    return ReturnCode.Error;
                }
            }

            if (!FlagOps.HasFlags(ipFlags, IpFlags.NoSort, true))
                output.Sort(StringComparer.Ordinal);

            if (merged != null)
                merged.AddRange(output);
            else
                merged = output;

            return ReturnCode.Ok;
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region CIDR Range IPv6 Methods
#if NET_40
        #region CIDR Range IPv6 Structure
        /// <summary>
        /// This structure represents a contiguous range of IPv6 addresses
        /// derived from a CIDR pattern, used when collapsing and merging
        /// multiple IPv6 CIDR ranges.
        /// </summary>
        [ObjectId("4dcb044a-fccd-42aa-a8d9-abc530d90afe")]
        private struct CIDR_Range_IPv6
        {
            /// <summary>
            /// The prefix length, in bits, of the originating CIDR pattern.
            /// </summary>
            internal byte PrefixLength;
            /// <summary>
            /// The first IPv6 address in the range, as a big integer.
            /// </summary>
            internal BigInteger Start;
            /// <summary>
            /// The last IPv6 address in the range, as a big integer.
            /// </summary>
            internal BigInteger End;
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region IComparer<CIDR_Range_IPv6> Helper Class
        /// <summary>
        /// This class compares two IPv6 address ranges, ordering them by their
        /// start address and then by their end address, so that a list of ranges
        /// can be sorted prior to merging.
        /// </summary>
        [ObjectId("bc503872-d909-4107-821e-28bb53b96d6b")]
        private sealed class CIDR_Range_IPv6_Comparer :
                IComparer<CIDR_Range_IPv6>
        {
            #region IComparer<CIDR_Range_IPv6> Overrides
            /// <summary>
            /// This method compares two IPv6 address ranges.
            /// </summary>
            /// <param name="x">
            /// The first IPv6 address range to compare.
            /// </param>
            /// <param name="y">
            /// The second IPv6 address range to compare.
            /// </param>
            /// <returns>
            /// A negative number if <paramref name="x" /> precedes
            /// <paramref name="y" />, zero if they are equal, or a positive
            /// number if <paramref name="x" /> follows <paramref name="y" />.
            /// </returns>
            public int Compare(
                CIDR_Range_IPv6 x, /* in */
                CIDR_Range_IPv6 y  /* in */
                )
            {
                int result = x.Start.CompareTo(y.Start);

                if (result != 0)
                    return result;

                return x.End.CompareTo(y.End);
            }
            #endregion
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method computes the IPv6 network mask that corresponds to the
        /// specified prefix length.
        /// </summary>
        /// <param name="prefixLength">
        /// The prefix length, in bits, for which to compute the mask.
        /// </param>
        /// <returns>
        /// The network mask, as a big integer.
        /// </returns>
        private static BigInteger GetMask_IPv6(
            byte prefixLength /* in */
            )
        {
            if (prefixLength == 0)
                return BigInteger.Zero;

            BigInteger ones = BigInteger.One << prefixLength;

            ones -= BigInteger.One;

            return ones << (IPv6Bits - prefixLength);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method counts the number of consecutive zero bits at the
        /// least-significant end of the specified big integer.
        /// </summary>
        /// <param name="value">
        /// The value whose trailing zero bits are to be counted.
        /// </param>
        /// <returns>
        /// The number of trailing zero bits, or the total number of bits in an
        /// IPv6 address if the value is zero.
        /// </returns>
        private static int CountTrailingZeros(
            BigInteger value /* in */
            )
        {
            if (value.IsZero)
                return IPv6Bits;

            int count = 0;

            while ((value & BigInteger.One) == BigInteger.Zero)
            {
                count++;
                value >>= 1;
            }

            return count;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method computes the floor of the base-2 logarithm of the
        /// specified big integer value.
        /// </summary>
        /// <param name="value">
        /// The value whose floored base-2 logarithm is to be computed.
        /// </param>
        /// <returns>
        /// The zero-based position of the most-significant set bit, or negative
        /// one if the value is zero or negative.
        /// </returns>
        private static int FloorLog2(
            BigInteger value /* in */
            )
        {
            int position = -1;

            while (value > BigInteger.Zero)
            {
                value >>= 1;
                position++;
            }

            return position;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method converts the specified big integer into a sixteen-byte
        /// IPv6 address byte array in network byte order.
        /// </summary>
        /// <param name="value">
        /// The IPv6 address value to convert.
        /// </param>
        /// <returns>
        /// The sixteen-byte address array, in network byte order.
        /// </returns>
        private static byte[] ToAddressBytes(
            BigInteger value /* in */
            )
        {
            byte[] addressBytes = new byte[SizeOfTwoULong];
            byte[] valueBytes = value.ToByteArray();

            int valueLength = valueBytes.Length;

            int addressLength = (valueLength < SizeOfTwoULong) ?
                valueLength : SizeOfTwoULong;

            int lastIndex = SizeOfTwoULong - 1;

            for (int index = 0; index < addressLength; index++)
                addressBytes[lastIndex - index] = valueBytes[index];

            return addressBytes;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method converts the specified IPv6 CIDR pattern into the
        /// contiguous range of addresses that it represents.
        /// </summary>
        /// <param name="pattern">
        /// The IPv6 CIDR pattern to convert, in address/prefix-length form.
        /// </param>
        /// <param name="ipFlags">
        /// The flags used to control validation and error handling behavior.
        /// </param>
        /// <param name="range">
        /// Upon success, receives the IPv6 address range represented by the
        /// pattern.
        /// </param>
        /// <param name="error">
        /// Upon failure, receives an error message describing why the range
        /// could not be computed.
        /// </param>
        /// <returns>
        /// True if the range was successfully computed; otherwise, false.
        /// </returns>
        private static bool RangeFromCIDR(
            string pattern,            /* in */
            IpFlags ipFlags,           /* in */
            ref CIDR_Range_IPv6 range, /* out */
            ref Result error           /* out */
            )
        {
            IPAddress prefix;
            byte prefixLength;

            if (!IsValidCIDR(
                    pattern, ipFlags, out prefix, out prefixLength,
                    ref error))
            {
                return false;
            }

            if (prefix.AddressFamily != AddressFamily.InterNetworkV6)
            {
                error = "invalid IPv6 CIDR: unexpected address family";
                return false;
            }

            if (prefixLength > IPv6Bits)
            {
                error = "invalid IPv6 CIDR prefix length";
                return false;
            }

            byte[] addressBytes = prefix.GetAddressBytes();

            BigInteger address = FromAddressBytes(addressBytes);
            BigInteger mask = GetMask_IPv6(prefixLength);
            BigInteger start = address & mask;
            BigInteger end = start | (IPv6Mask ^ mask);

            range.PrefixLength = prefixLength;
            range.Start = start;
            range.End = end;

            return true;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method decomposes the specified IPv6 address range into the
        /// minimal set of CIDR patterns that cover it, appending each pattern to
        /// the supplied output list.
        /// </summary>
        /// <param name="range">
        /// The IPv6 address range to decompose into CIDR patterns.
        /// </param>
        /// <param name="ipFlags">
        /// The flags used to control behavior.
        /// </param>
        /// <param name="output">
        /// The list to which the resulting CIDR patterns are appended.
        /// </param>
        /// <param name="error">
        /// Upon failure, receives an error message describing why the range
        /// could not be decomposed.
        /// </param>
        private static void RangeToCIDR(
            CIDR_Range_IPv6 range, /* in */
            IpFlags ipFlags,       /* in */
            StringList output,     /* in, out */
            ref Result error       /* out */
            )
        {
            BigInteger current = range.Start;
            BigInteger end = range.End;

            while (current <= end)
            {
                int suffixBits = CountTrailingZeros(current);
                int prefixBits = IPv6Bits - suffixBits;

                BigInteger remaining = (end - current) + BigInteger.One;

                int remainingBits = FloorLog2(remaining);
                int remainingPrefixBits = IPv6Bits - remainingBits;

                if (remainingPrefixBits > prefixBits)
                    prefixBits = remainingPrefixBits;

                if (prefixBits < 0)
                    prefixBits = 0;

                if (prefixBits > IPv6Bits)
                    prefixBits = IPv6Bits;

                byte[] addressBytes = ToAddressBytes(current);

                output.Add(String.Format(
                    "{0}/{1}", new IPAddress(addressBytes), prefixBits));

                BigInteger blockMask = GetMask_IPv6((byte)prefixBits);
                BigInteger blockEnd = current | (IPv6Mask ^ blockMask);

                if (blockEnd == IPv6Mask)
                    break;

                current = blockEnd + BigInteger.One;
            }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method merges a list of IPv6 address ranges, combining ranges
        /// that overlap or are adjacent into a smaller set of non-overlapping
        /// ranges.
        /// </summary>
        /// <param name="ranges">
        /// The list of IPv6 address ranges to merge.
        /// </param>
        /// <returns>
        /// A new list containing the merged, non-overlapping IPv6 address
        /// ranges.
        /// </returns>
        private static List<CIDR_Range_IPv6> MergeRanges(
            List<CIDR_Range_IPv6> ranges /* in */
            )
        {
            List<CIDR_Range_IPv6> merged = new List<CIDR_Range_IPv6>();

            if (ranges == null)
                return merged;

            int count = ranges.Count;

            if (count == 0)
                return merged;

            ranges.Sort(new CIDR_Range_IPv6_Comparer());

            CIDR_Range_IPv6 current = ranges[0];

            for (int index = 1; index < count; index++)
            {
                CIDR_Range_IPv6 next = ranges[index];

                if (next.Start <= (current.End + BigInteger.One))
                {
                    if (next.End > current.End)
                        current.End = next.End;
                }
                else
                {
                    merged.Add(current);
                    current = next;
                }
            }

            merged.Add(current);
            return merged;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method collapses a collection of IPv6 CIDR patterns into the
        /// minimal equivalent set of CIDR patterns by converting them to ranges,
        /// merging overlapping or adjacent ranges, and converting the merged
        /// ranges back to patterns.
        /// </summary>
        /// <param name="patterns">
        /// The collection of IPv6 CIDR patterns to collapse.
        /// </param>
        /// <param name="ipFlags">
        /// The flags used to control validation, sorting, and error handling
        /// behavior.
        /// </param>
        /// <param name="merged">
        /// On input, an optional existing list to which the collapsed patterns
        /// are appended; on output, the list of collapsed patterns, created if
        /// it was null.
        /// </param>
        /// <param name="error">
        /// Upon failure, receives an error message describing why the patterns
        /// could not be collapsed.
        /// </param>
        /// <returns>
        /// <see cref="ReturnCode.Ok" /> on success; otherwise, an appropriate
        /// error code.
        /// </returns>
        public static ReturnCode Collapse_IPv6_CIDR(
            IEnumerable<string> patterns, /* in */
            IpFlags ipFlags,              /* in */
            ref StringList merged,        /* in, out */
            ref Result error              /* out */
            )
        {
            if (patterns == null)
            {
                error = "invalid CIDR list";
                return ReturnCode.Error;
            }

            List<CIDR_Range_IPv6> ranges = new List<CIDR_Range_IPv6>();

            foreach (string pattern in patterns)
            {
                if (pattern == null)
                    continue;

                CIDR_Range_IPv6 range = default(CIDR_Range_IPv6);

                if (!RangeFromCIDR(
                        pattern, ipFlags, ref range, ref error))
                {
                    return ReturnCode.Error;
                }

                ranges.Add(range);
            }

            List<CIDR_Range_IPv6> localMerged = MergeRanges(ranges);
            int count = localMerged.Count;
            StringList output = new StringList();

            for (int index = 0; index < count; index++)
            {
                Result localError = null;

                RangeToCIDR(
                    localMerged[index], ipFlags, output,
                    ref localError);

                if (localError != null)
                {
                    error = localError;
                    return ReturnCode.Error;
                }
            }

            if (!FlagOps.HasFlags(ipFlags, IpFlags.NoSort, true))
                output.Sort(StringComparer.Ordinal);

            if (merged != null)
                merged.AddRange(output);
            else
                merged = output;

            return ReturnCode.Ok;
        }
#endif
        #endregion
    }
}
