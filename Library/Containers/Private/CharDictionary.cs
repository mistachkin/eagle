/*
 * CharDictionary.cs --
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
using Eagle._Containers.Public;

#if NET_STANDARD_21
using Index = Eagle._Constants.Index;
#endif

namespace Eagle._Containers.Private
{
    [ObjectId("a292e544-fcf4-4ca7-9e13-a7978c14ebbb")]
    internal sealed class CharDictionary : Dictionary<char, object>, ICloneable
    {
        public CharDictionary()
            : base()
        {
            // do nothing.
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public CharDictionary(
            IDictionary<char, object> dictionary
            )
            : base(dictionary)
        {
            // do nothing.
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public CharDictionary(
            IEnumerable<char> collection
            )
            : this()
        {
            Add(collection);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        #region Dead Code
#if DEAD_CODE
        private CharDictionary(
            int capacity
            )
            : base(capacity)
        {
            // do nothing.
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        private CharDictionary(
            IEnumerable<byte> collection
            )
            : this()
        {
            Add(collection);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        private void Add(
            IEnumerable<byte> collection
            )
        {
            foreach (byte item in collection)
                this.Add((char)item, this.Count);
        }
#endif
        #endregion

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public void Add(
            IEnumerable<char> collection
            )
        {
            foreach (char item in collection)
                this.Add(item, this.Count);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public string ToString(
            string pattern,
            bool noCase
            )
        {
            StringList list = new StringList(this.Keys);

            return ParserOps<string>.ListToString(list, Index.Invalid, Index.Invalid,
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
            return new CharDictionary(this);
        }
        #endregion
    }
}
