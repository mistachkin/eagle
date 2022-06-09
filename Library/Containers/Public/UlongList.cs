/*
 * UlongList.cs --
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
    [ObjectId("4ce0158e-36a3-429b-b793-f8c6622451aa")]
    public sealed class UlongList : List<ulong>, ICloneable
    {
        #region Public Constructors
        public UlongList()
            : base()
        {
            // do nothing.
        }

        ///////////////////////////////////////////////////////////////////////

        public UlongList(
            int capacity
            )
            : base(capacity)
        {
            // do nothing.
        }

        ///////////////////////////////////////////////////////////////////////

        public UlongList(
            IEnumerable<ulong> collection
            )
            : base(collection)
        {
            // do nothing.
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Factory Methods
        public static UlongList FromString(
            string value
            )
        {
            Result error = null;

            return FromString(value, ref error);
        }

        ///////////////////////////////////////////////////////////////////////

        public static UlongList FromString(
            string value,
            ref Result error
            )
        {
            StringList list = null;

            if (ParserOps<string>.SplitList(
                    null, value, 0, Length.Invalid, true,
                    ref list, ref error) == ReturnCode.Ok)
            {
                UlongList list2 = new UlongList(list.Count);

                foreach (string element in list)
                {
                    ulong ulongValue = 0;

                    if (Value.GetUnsignedWideInteger2(element,
                            ValueFlags.AnyWideInteger | ValueFlags.Unsigned,
                            null, ref ulongValue, ref error) != ReturnCode.Ok)
                    {
                        return null;
                    }

                    list2.Add(ulongValue);
                }

                return list2;
            }

            return null;
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region ToString Methods
        public string ToString(
            string pattern,
            bool noCase
            )
        {
            return ParserOps<ulong>.ListToString(
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
            return new UlongList(this);
        }
        #endregion
    }
}

