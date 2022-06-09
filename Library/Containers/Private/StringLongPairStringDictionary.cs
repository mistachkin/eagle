/*
 * StringLongPairStringDictionary.cs --
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
using System.Text.RegularExpressions;
using System.Threading;
using Eagle._Attributes;
using Eagle._Components.Private;
using Eagle._Components.Public;
using Eagle._Constants;
using Eagle._Containers.Public;
using Eagle._Interfaces.Public;
using IStringLongPair = Eagle._Interfaces.Public.IAnyPair<string, long>;

#if NET_STANDARD_21
using Index = Eagle._Constants.Index;
#endif

namespace Eagle._Containers.Private
{
#if SERIALIZATION
    [Serializable()]
#endif
    [ObjectId("21469290-3380-40dc-9331-c235c6bed124")]
    internal sealed class StringLongPairStringDictionary :
            SortedDictionary<IAnyPair<string, long>, string>
    {
        #region StringLongPair Class
#if SERIALIZATION
        [Serializable()]
#endif
        [ObjectId("415b245b-ebf5-4607-9d5e-e81da3c6850b")]
        private sealed class StringLongPair : AnyPair<string, long>
        {
            #region Public Constructors
            public StringLongPair(
                string x, /* in */
                long y    /* in */
                )
                : base(x, y)
            {
                // do nothing.
            }
            #endregion

            ///////////////////////////////////////////////////////////////////

            #region IComparer<IAnyPair<string, long>> Overrides
            public override int Compare(
                IAnyPair<string, long> x, /* in */
                IAnyPair<string, long> y  /* in */
                )
            {
                if ((x == null) && (y == null))
                {
                    return 0;
                }
                else if (x == null)
                {
                    return -1;
                }
                else if (y == null)
                {
                    return 1;
                }
                else
                {
                    //
                    // HACK: Compare the sequence numbers first as they are
                    //       used to maintain the original (i.e. "as added")
                    //       ordering of the string keys.
                    //
                    int result = Comparer<long>.Default.Compare(x.Y, y.Y);

                    if (result != 0)
                        return result;

                    return Comparer<string>.Default.Compare(x.X, y.X);
                }
            }
            #endregion

            ///////////////////////////////////////////////////////////////////

            #region System.Object Overrides
            public override string ToString()
            {
                //
                // HACK: Return the string key only; ignore the long integer
                //       sequence number.
                //
                return this.X;
            }
            #endregion
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Private Static Data
        private static long nextId = 0;
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Private Data
        private IComparer<string> stringComparer = Comparer<string>.Default;
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Public Constructors
        public StringLongPairStringDictionary(
            bool useStringKeyOnly
            )
            : base()
        {
            this.useStringKeyOnly = useStringKeyOnly;
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Private Static Methods
        private static long NextId()
        {
            return Interlocked.Increment(ref nextId);
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Private Methods
        private IAnyPair<string, long> GetAnyPairForStringKey(
            string key /* in */
            )
        {
            if (useStringKeyOnly)
                return new StringLongPair(key, NextId());
            else
                return new AnyPair<string, long>(key, NextId());
        }

        ///////////////////////////////////////////////////////////////////////

        private int CompareStringKey(
            string x, /* in */
            string y  /* in */
            )
        {
            IComparer<string> comparer = stringComparer;

            if (comparer == null)
                throw new InvalidOperationException(); /* IMPOSSIBLE? */

            return comparer.Compare(x, y);
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Public Properties
        private bool useStringKeyOnly;
        public bool UseStringKeyOnly
        {
            get { return useStringKeyOnly; }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Public Methods (Dictionary<string, string>)
        public void Add( /* O(1) */
            string key,  /* in */
            string value /* in */
            )
        {
            //
            // NOTE: This method cannot fail, even for "duplicate" keys,
            //       e.g. the same namespace name.
            //
            this.Add(GetAnyPairForStringKey(key), value);
        }

        ///////////////////////////////////////////////////////////////////////

        public bool ContainsKey( /* O(N) */
            string key /* in */
            )
        {
            foreach (KeyValuePair<IStringLongPair, string> pair in this)
            {
                IStringLongPair anyPair = pair.Key;

                if (anyPair == null)
                    continue;

                int compare = CompareStringKey(anyPair.X, key);

                if (compare == 0)
                    return true;
            }

            return false;
        }

        ///////////////////////////////////////////////////////////////////////

        public bool Remove( /* O(N) */
            string key /* in */
            )
        {
            IList<IStringLongPair> keys = null;

            foreach (KeyValuePair<IStringLongPair, string> pair in this)
            {
                IStringLongPair anyPair = pair.Key;

                if (anyPair == null)
                    continue;

                int compare = CompareStringKey(anyPair.X, key);

                if (compare == 0)
                {
                    if (keys == null)
                        keys = new List<IStringLongPair>();

                    keys.Add(anyPair);
                }
            }

            if (keys != null)
            {
                int count = 0;

                foreach (IStringLongPair anyPair in keys)
                {
                    if (anyPair == null)
                        continue;

                    if (this.Remove(anyPair))
                        count++;
                }

                return (count > 0);
            }
            else
            {
                return false;
            }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Public Methods (Other)
        public void AddKeys(
            IEnumerable collection, /* in */
            string value            /* in */
            )
        {
            foreach (object item in collection)
                this.Add(StringOps.GetStringFromObject(item), value);
        }

        ///////////////////////////////////////////////////////////////////////

        public string KeysAndValuesToString(
            string pattern, /* in */
            bool noCase     /* in */
            )
        {
            StringList list = GenericOps<IStringLongPair, string>.KeysAndValues(
                this, false, true, true, StringOps.DefaultMatchMode, pattern,
                null, null, null, null, noCase, RegexOptions.None) as StringList;

            return ParserOps<string>.ListToString(
                list, Index.Invalid, Index.Invalid, ToStringFlags.None,
                Characters.Space.ToString(), null, false);
        }
        #endregion
    }
}
