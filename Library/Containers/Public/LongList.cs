/*
 * LongList.cs --
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
using Eagle._Attributes;
using Eagle._Components.Private;
using Eagle._Components.Public;
using Eagle._Constants;

#if NET_STANDARD_21
using Index = Eagle._Constants.Index;
#endif

namespace Eagle._Containers.Public
{
#if SERIALIZATION
    [Serializable()]
#endif
    [ObjectId("fe3fadb3-f497-4e7c-a5fb-d7209a1c9f37")]
    public sealed class LongList : List<long>, ICloneable
    {
        #region Public Constructors
        public LongList()
            : base()
        {
            // do nothing.
        }

        ///////////////////////////////////////////////////////////////////////

        public LongList(
            int capacity
            )
            : base(capacity)
        {
            // do nothing.
        }

        ///////////////////////////////////////////////////////////////////////

        public LongList(
            IEnumerable<long> collection
            )
            : base(collection)
        {
            // do nothing.
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Factory Methods
        public static LongList FromString(
            string value
            )
        {
            Result error = null;

            return FromString(value, ref error);
        }

        ///////////////////////////////////////////////////////////////////////

        public static LongList FromString(
            string value,
            ref Result error
            )
        {
            StringList list = null;

            if (ParserOps<string>.SplitList(
                    null, value, 0, Length.Invalid, true,
                    ref list, ref error) == ReturnCode.Ok)
            {
                LongList list2 = new LongList(list.Count);

                foreach (string element in list)
                {
                    long longValue = 0;

                    if (Value.GetWideInteger2(
                            element, ValueFlags.AnyWideInteger, null,
                            ref longValue, ref error) != ReturnCode.Ok)
                    {
                        return null;
                    }

                    list2.Add(longValue);
                }

                return list2;
            }

            return null;
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Public Methods
        public void RemoveRange( /* O(N^2) */
            IEnumerable<long> collection
            )
        {
            if (collection == null)
                throw new ArgumentNullException("collection");

            if (Object.ReferenceEquals(collection, this))
            {
                this.Clear();
            }
            else
            {
                foreach (long item in collection)
                {
                    int count = this.Count;

                    for (int index = count - 1; index >= 0; index--)
                        if (this[index] == item)
                            this.RemoveAt(index);
                }
            }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region ToString Methods
        public string ToString(
            string pattern,
            bool noCase
            )
        {
            return ParserOps<long>.ListToString(
                this, Index.Invalid, Index.Invalid, ToStringFlags.None,
                Characters.Space.ToString(), pattern, noCase);
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region System.Object Overrides
        public override string ToString()
        {
            return ToString(null, false);
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region ICloneable Members
        public object Clone()
        {
            return new LongList(this);
        }
        #endregion
    }
}
