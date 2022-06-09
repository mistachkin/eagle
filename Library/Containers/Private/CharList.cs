/*
 * CharList.cs --
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
using System.Text;
using Eagle._Attributes;
using Eagle._Components.Private;
using Eagle._Components.Public;
using Eagle._Constants;

#if NET_STANDARD_21
using Index = Eagle._Constants.Index;
#endif

namespace Eagle._Containers.Private
{
    [ObjectId("a498e733-db5d-4111-ae0c-222948e8543c")]
    internal sealed class CharList : List<char>, ICloneable
    {
        public CharList()
            : base()
        {
            // do nothing.
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public CharList(
            int capacity
            )
            : base(capacity)
        {
            // do nothing.
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public CharList(
            IEnumerable<char> collection
            )
            : base(collection)
        {
            // do nothing.
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        #region Dead Code
#if DEAD_CODE
        private CharList(
            IEnumerable<byte> collection
            )
        {
            foreach (byte item in collection)
                this.Add((char)item);
        }
#endif
        #endregion

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public CharList(
            params IEnumerable<char>[] collections
            )
            : base()
        {
            foreach (IEnumerable<char> item in collections)
                Add(item);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public void Add(
            IEnumerable<char> collection
            )
        {
            foreach (char item in collection)
                this.Add(item);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        #region Dead Code
#if DEAD_CODE
        public string ToRawString()
        {
            StringBuilder result = StringOps.NewStringBuilder();

            foreach (char element in this)
                    result.Append(element);

            return result.ToString();
        }
#endif
        #endregion

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public string ToString(
            string pattern,
            bool noCase
            )
        {
            return ParserOps<char>.ListToString(this, Index.Invalid, Index.Invalid,
                ToStringFlags.None, Characters.Space.ToString(), pattern, noCase);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        #region System.Object Overrides
        public override string ToString()
        {
            return ToString(null, false);
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////////////////////////////

        #region ICloneable Members
        public object Clone()
        {
            return new CharList(this);
        }
        #endregion
    }
}
