/*
 * ByteList.cs --
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
    [ObjectId("09838087-059f-477f-a224-679d6e983984")]
    public sealed class ByteList : List<byte>, ICloneable
    {
        #region Public Constructors
        public ByteList()
            : base()
        {
            // do nothing.
        }

        ///////////////////////////////////////////////////////////////////////

        public ByteList(
            int capacity
            )
            : base(capacity)
        {
            // do nothing.
        }

        ///////////////////////////////////////////////////////////////////////

        public ByteList(
            IEnumerable<char> collection
            )
            : this()
        {
            foreach (char item in collection)
                this.Add(ConversionOps.ToByte(item));
        }

        ///////////////////////////////////////////////////////////////////////

        public ByteList(
            IEnumerable<byte> collection
            )
            : base(collection)
        {
            // do nothing.
        }

        ///////////////////////////////////////////////////////////////////////

        public ByteList(
            IEnumerable<byte> collection,
            bool reverse
            )
            : this(collection)
        {
            if (reverse)
                Reverse();
        }

        ///////////////////////////////////////////////////////////////////////

        public ByteList(
            byte[] array,
            int startIndex
            )
            : this()
        {
            Add(array, startIndex);
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Factory Methods
        public static ByteList FromString(
            string value
            )
        {
            Result error = null;

            return FromString(value, ref error);
        }

        ///////////////////////////////////////////////////////////////////////

        public static ByteList FromString(
            string value,
            ref Result error
            )
        {
            StringList list = null;

            if (ParserOps<string>.SplitList(
                    null, value, 0, Length.Invalid, true,
                    ref list, ref error) == ReturnCode.Ok)
            {
                ByteList list2 = new ByteList(list.Count);

                foreach (string element in list)
                {
                    byte byteValue = 0;

                    if (Value.GetByte2(element, ValueFlags.AnyByte, null,
                            ref byteValue, ref error) != ReturnCode.Ok)
                    {
                        return null;
                    }

                    list2.Add(byteValue);
                }

                return list2;
            }

            return null;
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Public Methods
        public void Add(
            byte[] array,
            int startIndex
            )
        {
            int newLength = array.Length - startIndex;

            byte[] newArray = new byte[newLength];

            Array.Copy(array, startIndex, newArray, 0, newLength);

            this.AddRange(newArray);
        }

        ///////////////////////////////////////////////////////////////////////

        public bool AddFromString(
            string value,
            ref Result error
            )
        {
            ByteList list = FromString(value, ref error);

            if (list == null)
                return false;

            this.AddRange(list);
            return true;
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region ToString Methods
        public string ToString(
            string pattern,
            bool noCase
            )
        {
            return ParserOps<byte>.ListToString(
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
            return new ByteList(this);
        }
        #endregion
    }
}
