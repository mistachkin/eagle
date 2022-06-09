/*
 * IntList.cs --
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
    [ObjectId("778491dd-a2f1-4a89-85f0-a82b3d5d9555")]
    public sealed class IntList : List<int>, ICloneable
    {
        #region Public Constructors
        public IntList()
            : base()
        {
            // do nothing.
        }

        ///////////////////////////////////////////////////////////////////////

        public IntList(
            int capacity
            )
            : base(capacity)
        {
            // do nothing.
        }

        ///////////////////////////////////////////////////////////////////////

        public IntList(
            IEnumerable<int> collection
            )
            : base(collection)
        {
            // do nothing.
        }

        ///////////////////////////////////////////////////////////////////////

        public IntList(
            IEnumerable<uint> collection
            )
            : base()
        {
            Add(collection);
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Factory Methods
        public static IntList FromString(
            string value
            )
        {
            Result error = null;

            return FromString(value, ref error);
        }

        ///////////////////////////////////////////////////////////////////////

        public static IntList FromString(
            string value,
            ref Result error
            )
        {
            StringList list = null;

            if (ParserOps<string>.SplitList(
                    null, value, 0, Length.Invalid, true,
                    ref list, ref error) == ReturnCode.Ok)
            {
                IntList list2 = new IntList(list.Count);

                foreach (string element in list)
                {
                    int intValue = 0;

                    if (Value.GetInteger2(element, ValueFlags.AnyInteger, null,
                            ref intValue, ref error) != ReturnCode.Ok)
                    {
                        return null;
                    }

                    list2.Add(intValue);
                }

                return list2;
            }

            return null;
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Add Methods
        public void Add(
            IEnumerable<uint> collection
            )
        {
            foreach (uint element in collection)
                Add(ConversionOps.ToInt(element));
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region ToString Methods
        public string ToString(
            string pattern,
            bool noCase
            )
        {
            return ParserOps<int>.ListToString(
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
            return new IntList(this);
        }
        #endregion
    }
}
