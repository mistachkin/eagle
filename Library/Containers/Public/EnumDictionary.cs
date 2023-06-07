/*
 * EnumDictionary.cs --
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

#if SERIALIZATION
using System.Runtime.Serialization;
#endif

using Eagle._Attributes;
using Eagle._Components.Private;
using Eagle._Components.Public;
using Eagle._Constants;
using SharedStringOps = Eagle._Components.Shared.StringOps;

#if NET_STANDARD_21
using Index = Eagle._Constants.Index;
#endif

namespace Eagle._Containers.Public
{
#if SERIALIZATION
    [Serializable()]
#endif
    [ObjectId("f09ea871-dac3-41d0-baca-454765dd8d08")]
    public sealed class EnumDictionary : Dictionary<string, Enum>
    {
        #region Public Constructors
        public EnumDictionary()
            : base(new _Comparers.StringCustom(
                SharedStringOps.GetSystemComparisonType(true)))
        {
            // do nothing.
        }

        ///////////////////////////////////////////////////////////////////////

        public EnumDictionary(
            int capacity
            )
            : base(capacity, new _Comparers.StringCustom(
                SharedStringOps.GetSystemComparisonType(true)))
        {
            // do nothing.
        }

        ///////////////////////////////////////////////////////////////////////

        public EnumDictionary(
            IDictionary<string, Enum> dictionary
            )
            : base(dictionary, new _Comparers.StringCustom(
                SharedStringOps.GetSystemComparisonType(true)))
        {
            // do nothing.
        }

        ///////////////////////////////////////////////////////////////////////

        public EnumDictionary(
            IEqualityComparer<string> comparer
            )
            : base(comparer)
        {
            // do nothing.
        }

        ///////////////////////////////////////////////////////////////////////

        public EnumDictionary(
            IDictionary<string, Enum> dictionary,
            IEqualityComparer<string> comparer
            )
            : base(dictionary, comparer)
        {
            // do nothing.
        }

        ///////////////////////////////////////////////////////////////////////

        public EnumDictionary(
            IEnumerable collection
            )
            : base(new _Comparers.StringCustom(
                SharedStringOps.GetSystemComparisonType(true)))
        {
            Add(collection);
        }

        ///////////////////////////////////////////////////////////////////////

        public EnumDictionary(
            IEnumerable<Enum> collection
            )
            : base(new _Comparers.StringCustom(
                SharedStringOps.GetSystemComparisonType(true)))
        {
            Add(collection);
        }

        ///////////////////////////////////////////////////////////////////////

        public EnumDictionary(
            IEnumerable<Enum> collection,
            IEqualityComparer<string> comparer
            )
            : base(comparer)
        {
            Add(collection);
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////

        #region Protected Constructors
#if SERIALIZATION
        private EnumDictionary(
            SerializationInfo info,
            StreamingContext context
            )
            : base(info, context)
        {
            // do nothing.
        }
#endif
        #endregion

        ///////////////////////////////////////////////////////////////////////

        public void Add(
            Enum item
            )
        {
            this.Add(item.ToString(), item);
        }

        ///////////////////////////////////////////////////////////////////////

        public void Add(
            IEnumerable collection
            )
        {
            foreach (object item in collection)
                this.Add((Enum)item);
        }

        ///////////////////////////////////////////////////////////////////////

        public void Add(
            IEnumerable<Enum> collection
            )
        {
            foreach (Enum item in collection)
                this.Add(item);
        }

        ///////////////////////////////////////////////////////////////////////

        public bool TryGetValue(
            string key,
            Type enumType,
            out object value
            )
        {
            Result error = null;

            return TryGetValue(key, enumType, out value, ref error);
        }

        ///////////////////////////////////////////////////////////////////////

        public bool TryGetValue(
            string key,
            Type enumType,
            out object value,
            ref Result error
            )
        {
            Enum enumValue;

            if (this.TryGetValue(key, out enumValue))
            {
                value = EnumOps.TryGet(enumType, enumValue, ref error);

                return true;
            }
            else
            {
                value = EnumOps.TryGet(enumType, 0, ref error);

                return false;
            }
        }

        ///////////////////////////////////////////////////////////////////////

        public string ToString(
            string pattern,
            bool noCase
            )
        {
            return ToString(Characters.Space.ToString(), pattern, noCase);
        }

        ///////////////////////////////////////////////////////////////////////

        public string ToString(
            string separator,
            string pattern,
            bool noCase
            )
        {
            StringList list = new StringList(this.Keys);

            return ParserOps<string>.ListToString(
                list, Index.Invalid, Index.Invalid, ToStringFlags.None,
                Characters.Space.ToString(), pattern, noCase);
        }

        ///////////////////////////////////////////////////////////////////////

        #region System.Object Overrides
        public override string ToString()
        {
            return ToString(Characters.Space.ToString(), null, false);
        }
        #endregion
    }
}
