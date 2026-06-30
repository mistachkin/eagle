/*
 * ThrottleDictionary.cs --
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
using System.Runtime.CompilerServices;
using Eagle._Attributes;
using Eagle._Components.Private;
using Eagle._Components.Public;
using Eagle._Interfaces.Public;
using SharedStringOps = Eagle._Components.Shared.StringOps;

using ThrottlePair = System.Collections.Generic.KeyValuePair<
    Eagle._Containers.Public.ThrottleDictionary.ThrottleKey, ulong>;

#if FAST_DICTIONARY
using SomeDictionary = Eagle._Containers.Public.FastDictionary<
    Eagle._Containers.Public.ThrottleDictionary.ThrottleKey, ulong>;
#else
using SomeDictionary = System.Collections.Generic.Dictionary<
    Eagle._Containers.Public.ThrottleDictionary.ThrottleKey, ulong>;
#endif

namespace Eagle._Containers.Public
{
    /// <summary>
    /// This class implements a thread-safe dictionary used to track and
    /// rate-limit ("throttle") events per host within a configurable window of
    /// time.  Each entry is keyed by a host name together with a truncated
    /// timestamp and its value is the count of events seen for that host during
    /// that time window.  It provides methods to test whether a host has
    /// exceeded its allowed count, to increment a host's count, and to reset
    /// the recorded counts.
    /// </summary>
#if SERIALIZATION
    [Serializable()]
#endif
    [ObjectId("ef8da17f-150e-48b4-9177-0dc0ab202043")]
    public sealed class ThrottleDictionary : SomeDictionary
    {
        #region ThrottleKey Helper Class
        /// <summary>
        /// This class represents the key used by the throttle dictionary.  It
        /// pairs a host name with a (typically truncated) timestamp so that
        /// events for a given host within a given window of time can be grouped
        /// and counted together.
        /// </summary>
#if SERIALIZATION
        [Serializable()]
#endif
        [ObjectId("bb6b2e06-0fae-4489-9d08-885dfd607704")]
        public sealed class ThrottleKey :
                MutableAnyPair<string, DateTime>
        {
            #region Public Constructors
            /// <summary>
            /// Constructs a new throttle key for the specified host and
            /// timestamp.
            /// </summary>
            /// <param name="host">
            /// The host name component of the key.
            /// </param>
            /// <param name="now">
            /// The timestamp component of the key, normally truncated to the
            /// throttle time window.
            /// </param>
            public ThrottleKey(
                string host, /* in */
                DateTime now /* in */
                )
                : base(false, host, now)
            {
                // do nothing.
            }
            #endregion

            ///////////////////////////////////////////////////////////////////

            #region System.Object Overrides
            /// <summary>
            /// This method returns a string representation of this throttle
            /// key, consisting of its host name and ISO-8601 formatted
            /// timestamp.
            /// </summary>
            /// <returns>
            /// The string representation of this throttle key.
            /// </returns>
            public override string ToString()
            {
                return StringList.MakeList(this.X,
                    FormatOps.Iso8601FullDateTime(this.Y));

            }
            #endregion
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region ThrottleKeyComparer Helper Class
        /// <summary>
        /// This class implements equality comparison for throttle keys.  Two
        /// keys are considered equal when their host names match (ignoring case)
        /// and their timestamps are equal.
        /// </summary>
#if SERIALIZATION
        [Serializable()]
#endif
        [ObjectId("924c6d8e-6212-4fba-8a10-23a51bd2afd9")]
        private sealed class ThrottleKeyComparer :
                IEqualityComparer<ThrottleKey>
        {
            #region Private Constants
            /// <summary>
            /// The comparer used to compare the host name component of throttle
            /// keys.  This comparison is case-insensitive and this value can
            /// never be null.
            /// </summary>
            internal static IEqualityComparer<string> stringComparer =
                StringComparer.OrdinalIgnoreCase; /* CANNOT BE NULL */

            ///////////////////////////////////////////////////////////////////

            /// <summary>
            /// The comparer used to compare the timestamp component of throttle
            /// keys.  This value can never be null.
            /// </summary>
            private static IEqualityComparer<DateTime> dateTimeComparer =
                EqualityComparer<DateTime>.Default; /* CANNOT BE NULL */
            #endregion

            ///////////////////////////////////////////////////////////////////

            #region IEqualityComparer<ThrottleKey> Members
            /// <summary>
            /// This method determines whether two throttle keys are equal.
            /// </summary>
            /// <param name="x">
            /// The first throttle key to compare.  This parameter may be null.
            /// </param>
            /// <param name="y">
            /// The second throttle key to compare.  This parameter may be null.
            /// </param>
            /// <returns>
            /// True if the two throttle keys are equal; otherwise, false.
            /// </returns>
            public bool Equals(
                ThrottleKey x, /* in */
                ThrottleKey y  /* in */
                )
            {
                if ((x == null) && (y == null))
                {
                    return true;
                }
                else if ((x == null) || (y == null))
                {
                    return false;
                }
                else
                {
                    if (!stringComparer.Equals(x.X, y.X))
                        return false;

                    if (!dateTimeComparer.Equals(x.Y, y.Y))
                        return false;

                    return true;
                }
            }

            ///////////////////////////////////////////////////////////////////

            /// <summary>
            /// This method returns a hash code for the specified throttle key,
            /// computed from its host name and timestamp components.
            /// </summary>
            /// <param name="obj">
            /// The throttle key for which a hash code is computed.  This
            /// parameter may be null.
            /// </param>
            /// <returns>
            /// The hash code for the specified throttle key, or zero if it is
            /// null.
            /// </returns>
            public int GetHashCode(
                ThrottleKey obj /* in */
                )
            {
                if (obj == null)
                    return 0;

                return stringComparer.GetHashCode(obj.X) ^
                    dateTimeComparer.GetHashCode(obj.Y);
            }
            #endregion
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Private Constants
        //
        // HACK: These are purposely not read-only.
        //
        /// <summary>
        /// The default maximum event count used when no explicit count is
        /// supplied.
        /// </summary>
        private static ulong DefaultCount = 1; /* TODO: Good default? */
        /// <summary>
        /// The default time window, in seconds, used when no explicit number of
        /// seconds is supplied.
        /// </summary>
        private static ulong DefaultSeconds = 60; /* TODO: Good default? */
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Private Data
        /// <summary>
        /// The object used to synchronize access to this dictionary across
        /// threads.
        /// </summary>
        private readonly object syncRoot = new object();
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Public Constructors
        /// <summary>
        /// Constructs an empty throttle dictionary that uses the throttle key
        /// comparer for its keys.
        /// </summary>
        public ThrottleDictionary()
            : base(new ThrottleKeyComparer())
        {
            // do nothing.
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Constructs a throttle dictionary that is initialized with the
        /// contents of the specified dictionary and uses the throttle key
        /// comparer for its keys.
        /// </summary>
        /// <param name="dictionary">
        /// The dictionary whose entries are copied into the new throttle
        /// dictionary.
        /// </param>
        public ThrottleDictionary(
            IDictionary<ThrottleKey, ulong> dictionary /* in */
            )
            : base(dictionary, new ThrottleKeyComparer())
        {
            // do nothing.
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Private Methods
        /// <summary>
        /// This method computes the timestamp to use for a throttle key,
        /// truncated to the specified time window.
        /// </summary>
        /// <param name="now">
        /// The timestamp to use.  This parameter may be null, in which case the
        /// current UTC time is used.
        /// </param>
        /// <param name="seconds">
        /// The time window, in seconds, to which the timestamp is truncated.
        /// </param>
        /// <returns>
        /// The truncated timestamp.
        /// </returns>
        private DateTime GetNow(
            DateTime? now, /* in: OPTIONAL */
            ulong seconds  /* in */
            )
        {
            return TimeOps.MaybeTruncate(
                (now != null) ? (DateTime)now : TimeOps.GetUtcNow(),
                ConversionOps.ToLong(seconds));
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method returns the effective maximum event count, using the
        /// default count when none is supplied.
        /// </summary>
        /// <param name="count">
        /// The requested maximum event count.  This parameter may be null, in
        /// which case the default count is used.
        /// </param>
        /// <returns>
        /// The effective maximum event count.
        /// </returns>
        private ulong GetCount(
            ulong? count /* in: OPTIONAL */
            )
        {
            return (count != null) ? (ulong)count : DefaultCount;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method returns the effective time window, in seconds, using the
        /// default number of seconds when none is supplied.
        /// </summary>
        /// <param name="seconds">
        /// The requested time window, in seconds.  This parameter may be null,
        /// in which case the default number of seconds is used.
        /// </param>
        /// <returns>
        /// The effective time window, in seconds.
        /// </returns>
        private ulong GetSeconds(
            ulong? seconds /* in: OPTIONAL */
            )
        {
            return (seconds != null) ? (ulong)seconds : DefaultSeconds;
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method builds a throttle key for the specified host and
        /// timestamp, truncating the timestamp to the effective time window.
        /// </summary>
        /// <param name="host">
        /// The host name for the key.  This parameter may be null, in which
        /// case a null key is returned.
        /// </param>
        /// <param name="now">
        /// The timestamp for the key.  This parameter may be null.
        /// </param>
        /// <param name="seconds">
        /// The time window, in seconds, used to truncate the timestamp.  This
        /// parameter may be null, in which case the default number of seconds is
        /// used.
        /// </param>
        /// <param name="forReset">
        /// Non-zero if the key is being built for a reset operation, in which
        /// case a null timestamp results in a null key.
        /// </param>
        /// <returns>
        /// The constructed throttle key, or null if a key cannot be built from
        /// the supplied arguments.
        /// </returns>
        private ThrottleKey GetKey(
            string host,    /* in: OPTIONAL */
            DateTime? now,  /* in: OPTIONAL */
            ulong? seconds, /* in: OPTIONAL */
            bool forReset   /* in */
            )
        {
            if (host == null)
                return null;

            if (forReset && (now == null))
                return null;

            return new ThrottleKey(
                host, TimeOps.MaybeTruncate(now, ConversionOps.ToLong(
                GetSeconds(seconds))));
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method determines whether the recorded event count for the
        /// specified host within the specified time window has exceeded the
        /// allowed count.  Access to the dictionary is synchronized.
        /// </summary>
        /// <param name="host">
        /// The host name to check.  This parameter may be null, in which case
        /// the host is treated as not exceeded.
        /// </param>
        /// <param name="now">
        /// The timestamp used to locate the time window.  This parameter may be
        /// null.
        /// </param>
        /// <param name="count">
        /// The maximum allowed event count.  This parameter may be null, in
        /// which case the default count is used.
        /// </param>
        /// <param name="seconds">
        /// The time window, in seconds.  This parameter may be null, in which
        /// case the default number of seconds is used.
        /// </param>
        /// <param name="inclusive">
        /// Non-zero to treat reaching the allowed count as exceeded; otherwise,
        /// the count must be strictly greater than the allowed count.
        /// </param>
        /// <param name="value">
        /// Upon success, receives the recorded event count for the host;
        /// otherwise, receives null.
        /// </param>
        /// <returns>
        /// True if the recorded count has exceeded the allowed count;
        /// otherwise, false.
        /// </returns>
        private bool PrivateIsExceeded(
            string host,     /* in */
            DateTime? now,   /* in: OPTIONAL */
            ulong? count,    /* in: OPTIONAL */
            ulong? seconds,  /* in: OPTIONAL */
            bool inclusive,  /* in */
            out ulong? value /* out */
            )
        {
            lock (syncRoot) /* TRANSACTIONAL */
            {
                if (host == null)
                {
                    value = null;
                    return false;
                }

                ThrottleKey wantKey = GetKey(host, now, seconds, false);
                ulong someSecondsCount;

                if (!this.TryGetValue(wantKey, out someSecondsCount))
                {
                    value = null;
                    return false;
                }

                value = someSecondsCount;

                return inclusive ?
                    (someSecondsCount >= GetCount(count)) :
                    (someSecondsCount > GetCount(count));
            }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method increments the recorded event count for the specified
        /// host within the specified time window, creating the entry if it does
        /// not yet exist.  Access to the dictionary is synchronized.
        /// </summary>
        /// <param name="host">
        /// The host name whose count is incremented.  This parameter may be
        /// null, in which case nothing is incremented.
        /// </param>
        /// <param name="now">
        /// The timestamp used to locate the time window.  This parameter may be
        /// null.
        /// </param>
        /// <param name="seconds">
        /// The time window, in seconds.  This parameter may be null, in which
        /// case the default number of seconds is used.
        /// </param>
        /// <param name="value">
        /// Upon success, receives the new recorded event count for the host;
        /// otherwise, receives null.
        /// </param>
        /// <returns>
        /// True if the count was incremented; otherwise, false.
        /// </returns>
        private bool PrivateIncrement(
            string host,     /* in */
            DateTime? now,   /* in: OPTIONAL */
            ulong? seconds,  /* in: OPTIONAL */
            out ulong? value /* out */
            )
        {
            lock (syncRoot) /* TRANSACTIONAL */
            {
                if (host == null)
                {
                    value = null;
                    return false;
                }

                ThrottleKey wantKey = GetKey(host, now, seconds, false);
                ulong someSecondsCount;

                if (this.TryGetValue(wantKey, out someSecondsCount))
                    someSecondsCount++;
                else
                    someSecondsCount = 1;

                value = this[wantKey] = someSecondsCount;
                return true;
            }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method removes recorded event counts and returns their total.
        /// When a host is specified, only matching entries are removed;
        /// otherwise, all entries are removed.  Access to the dictionary is
        /// synchronized.
        /// </summary>
        /// <param name="host">
        /// The host name whose entries are removed.  This parameter may be
        /// null, in which case all entries are removed.
        /// </param>
        /// <param name="now">
        /// The timestamp used to locate a specific time window.  This parameter
        /// may be null.
        /// </param>
        /// <param name="seconds">
        /// The time window, in seconds.  This parameter may be null, in which
        /// case the default number of seconds is used.
        /// </param>
        /// <returns>
        /// The total of the event counts that were removed.
        /// </returns>
        private ulong PrivateReset(
            string host,   /* in: OPTIONAL */
            DateTime? now, /* in: OPTIONAL */
            ulong? seconds /* in: OPTIONAL */
            )
        {
            lock (syncRoot) /* TRANSACTIONAL */
            {
                ulong count = 0;

                if (host != null)
                {
                    List<ThrottleKey> haveKeys = null;

                    ThrottleKey wantKey = GetKey(
                        host, now, seconds, true);

                    IEqualityComparer<string> comparer =
                        ThrottleKeyComparer.stringComparer;

                    foreach (ThrottlePair pair in this)
                    {
                        ThrottleKey haveKey = pair.Key;

                        if (haveKey == null) /* IMPOSSIBLE? */
                            continue;

                        if ((wantKey != null) &&
                            !haveKey.Equals(wantKey))
                        {
                            continue;
                        }
                        else if ((comparer == null) ||
                            !comparer.Equals(haveKey.X, host))
                        {
                            continue;
                        }

                        if (haveKeys == null)
                            haveKeys = new List<ThrottleKey>();

                        haveKeys.Add(haveKey);
                        count += pair.Value;
                    }

                    if (haveKeys != null)
                    {
                        foreach (ThrottleKey haveKey in haveKeys)
                        {
                            if (haveKey == null) /* IMPOSSIBLE? */
                                continue;

                            /* IGNORED */
                            this.Remove(haveKey);
                        }
                    }
                }
                else
                {
                    foreach (ThrottlePair pair in this)
                        count += pair.Value;

                    /* NO RESULT */
                    this.Clear();
                }

                return count;
            }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method returns a string representation of the recorded event
        /// counts, including a total count entry.  Access to the dictionary is
        /// synchronized.
        /// </summary>
        /// <returns>
        /// The string representation of the recorded event counts.
        /// </returns>
        private string PrivateToString()
        {
            lock (syncRoot) /* TRANSACTIONAL */
            {
                StringList list = new StringList();
                ulong count = 0;

                foreach (ThrottlePair pair in this)
                {
                    ThrottleKey haveKey = pair.Key;

                    if (haveKey == null)
                        continue;

                    list.Add(haveKey.ToString());

                    ulong someSecondsCount = pair.Value;

                    list.Add(someSecondsCount.ToString());

                    if (someSecondsCount > 0)
                        count += someSecondsCount;
                }

                list.Add("TOTAL");
                list.Add(count.ToString());

                return list.ToString();
            }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Public Methods
        /// <summary>
        /// This method determines whether the recorded event count for the
        /// specified host within the specified time window has exceeded the
        /// allowed count.
        /// </summary>
        /// <param name="host">
        /// The host name to check.  This parameter may be null, in which case
        /// the host is treated as not exceeded.
        /// </param>
        /// <param name="now">
        /// The timestamp used to locate the time window.  This parameter may be
        /// null.
        /// </param>
        /// <param name="count">
        /// The maximum allowed event count.  This parameter may be null, in
        /// which case the default count is used.
        /// </param>
        /// <param name="seconds">
        /// The time window, in seconds.  This parameter may be null, in which
        /// case the default number of seconds is used.
        /// </param>
        /// <param name="inclusive">
        /// Non-zero to treat reaching the allowed count as exceeded; otherwise,
        /// the count must be strictly greater than the allowed count.
        /// </param>
        /// <param name="value">
        /// Upon success, receives the recorded event count for the host;
        /// otherwise, receives null.
        /// </param>
        /// <returns>
        /// True if the recorded count has exceeded the allowed count;
        /// otherwise, false.
        /// </returns>
        public bool IsExceeded(
            string host,     /* in */
            DateTime? now,   /* in: OPTIONAL */
            ulong? count,    /* in: OPTIONAL */
            ulong? seconds,  /* in: OPTIONAL */
            bool inclusive,  /* in */
            out ulong? value /* out */
            )
        {
            return PrivateIsExceeded(
                host, now, count, seconds, inclusive, out value);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method increments the recorded event count for the specified
        /// host within the specified time window.
        /// </summary>
        /// <param name="host">
        /// The host name whose count is incremented.  This parameter may be
        /// null, in which case nothing is incremented.
        /// </param>
        /// <param name="now">
        /// The timestamp used to locate the time window.  This parameter may be
        /// null.
        /// </param>
        /// <param name="seconds">
        /// The time window, in seconds.  This parameter may be null, in which
        /// case the default number of seconds is used.
        /// </param>
        /// <param name="value">
        /// Upon success, receives the new recorded event count for the host;
        /// otherwise, receives null.
        /// </param>
        /// <returns>
        /// True if the count was incremented; otherwise, false.
        /// </returns>
        public bool Increment(
            string host,     /* in */
            DateTime? now,   /* in: OPTIONAL */
            ulong? seconds,  /* in: OPTIONAL */
            out ulong? value /* out */
            )
        {
            return PrivateIncrement(
                host, now, seconds, out value);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method increments the recorded event count for the specified
        /// host within the specified time window, unless doing so would exceed
        /// the allowed count.  The check and increment are performed as a single
        /// synchronized operation.
        /// </summary>
        /// <param name="host">
        /// The host name whose count is incremented.  This parameter may be
        /// null, in which case nothing is incremented.
        /// </param>
        /// <param name="now">
        /// The timestamp used to locate the time window.  This parameter may be
        /// null.
        /// </param>
        /// <param name="count">
        /// The maximum allowed event count.  This parameter may be null, in
        /// which case the default count is used.
        /// </param>
        /// <param name="seconds">
        /// The time window, in seconds.  This parameter may be null, in which
        /// case the default number of seconds is used.
        /// </param>
        /// <param name="inclusive">
        /// Non-zero to treat reaching the allowed count as exceeded; otherwise,
        /// the count must be strictly greater than the allowed count.
        /// </param>
        /// <param name="value">
        /// Upon success, receives the new recorded event count for the host; if
        /// the allowed count was exceeded, receives the existing recorded count;
        /// otherwise, receives null.
        /// </param>
        /// <returns>
        /// True if the count was incremented; false if the allowed count was
        /// already exceeded or the count could not be incremented.
        /// </returns>
        public bool TryIncrement(
            string host,     /* in */
            DateTime? now,   /* in: OPTIONAL */
            ulong? count,    /* in: OPTIONAL */
            ulong? seconds,  /* in: OPTIONAL */
            bool inclusive,  /* in */
            out ulong? value /* out */
            )
        {
            lock (syncRoot) /* TRANSACTIONAL */
            {
                if (PrivateIsExceeded(
                        host, now, count, seconds, inclusive,
                        out value))
                {
                    return false;
                }

                return PrivateIncrement(
                    host, now, seconds, out value);
            }
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method removes recorded event counts and returns their total.
        /// When a host is specified, only matching entries are removed;
        /// otherwise, all entries are removed.
        /// </summary>
        /// <param name="host">
        /// The host name whose entries are removed.  This parameter may be
        /// null, in which case all entries are removed.
        /// </param>
        /// <param name="now">
        /// The timestamp used to locate a specific time window.  This parameter
        /// may be null.
        /// </param>
        /// <param name="seconds">
        /// The time window, in seconds.  This parameter may be null, in which
        /// case the default number of seconds is used.
        /// </param>
        /// <returns>
        /// The total of the event counts that were removed.
        /// </returns>
        public ulong Reset(
            string host,   /* in: OPTIONAL */
            DateTime? now, /* in: OPTIONAL */
            ulong? seconds /* in: OPTIONAL */
            )
        {
            return PrivateReset(host, now, seconds);
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region System.Object Overrides
        /// <summary>
        /// This method determines whether the specified object is the same
        /// instance as this dictionary, using reference equality.
        /// </summary>
        /// <param name="obj">
        /// The object to compare with this dictionary.  This parameter may be
        /// null.
        /// </param>
        /// <returns>
        /// True if the specified object is the same instance as this
        /// dictionary; otherwise, false.
        /// </returns>
        public override bool Equals(
            object obj /* in */
            )
        {
            return Object.ReferenceEquals(obj, this);
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method returns a string representation of this dictionary,
        /// including the recorded event counts and their total.
        /// </summary>
        /// <returns>
        /// The string representation of this dictionary.
        /// </returns>
        public override string ToString()
        {
            return PrivateToString();
        }

        ///////////////////////////////////////////////////////////////////////

        /// <summary>
        /// This method returns a hash code for this dictionary, based on its
        /// instance identity.
        /// </summary>
        /// <returns>
        /// The hash code for this dictionary.
        /// </returns>
        public override int GetHashCode()
        {
            return RuntimeHelpers.GetHashCode(this);
        }
        #endregion
    }
}
