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
    Eagle._Containers.Public.ThrottleDictionary.ThrottleKey,
    ulong>;

namespace Eagle._Containers.Public
{
#if SERIALIZATION
    [Serializable()]
#endif
    [ObjectId("ef8da17f-150e-48b4-9177-0dc0ab202043")]
    public sealed class ThrottleDictionary :
            Dictionary<ThrottleDictionary.ThrottleKey, ulong>
    {
        #region ThrottleKey Helper Class
#if SERIALIZATION
        [Serializable()]
#endif
        [ObjectId("bb6b2e06-0fae-4489-9d08-885dfd607704")]
        public sealed class ThrottleKey :
                MutableAnyPair<string, DateTime>
        {
            #region Public Constructors
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
#if SERIALIZATION
        [Serializable()]
#endif
        [ObjectId("924c6d8e-6212-4fba-8a10-23a51bd2afd9")]
        private sealed class ThrottleKeyComparer :
                IEqualityComparer<ThrottleKey>
        {
            #region Private Constants
            internal static IEqualityComparer<string> stringComparer =
                StringComparer.OrdinalIgnoreCase; /* CANNOT BE NULL */

            ///////////////////////////////////////////////////////////////////

            private static IEqualityComparer<DateTime> dateTimeComparer =
                EqualityComparer<DateTime>.Default; /* CANNOT BE NULL */
            #endregion

            ///////////////////////////////////////////////////////////////////

            #region IEqualityComparer<ThrottleKey> Members
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
        private static ulong DefaultCount = 1; /* TODO: Good default? */
        private static ulong DefaultSeconds = 60; /* TODO: Good default? */
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Private Data
        private readonly object syncRoot = new object();
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Public Constructors
        public ThrottleDictionary()
            : base(new ThrottleKeyComparer())
        {
            // do nothing.
        }

        ///////////////////////////////////////////////////////////////////////

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

        private ulong GetCount(
            ulong? count /* in: OPTIONAL */
            )
        {
            return (count != null) ? (ulong)count : DefaultCount;
        }

        ///////////////////////////////////////////////////////////////////////

        private ulong GetSeconds(
            ulong? seconds /* in: OPTIONAL */
            )
        {
            return (seconds != null) ? (ulong)seconds : DefaultSeconds;
        }

        ///////////////////////////////////////////////////////////////////////

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
        public override bool Equals(
            object obj /* in */
            )
        {
            return Object.ReferenceEquals(obj, this);
        }

        ///////////////////////////////////////////////////////////////////////

        public override string ToString()
        {
            return PrivateToString();
        }

        ///////////////////////////////////////////////////////////////////////

        public override int GetHashCode()
        {
            return RuntimeHelpers.GetHashCode(this);
        }
        #endregion
    }
}
