/*
 * RegExList.cs --
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
using System.Text.RegularExpressions;
using Eagle._Attributes;
using Eagle._Components.Private;
using Eagle._Components.Public;
using Eagle._Constants;

#if NET_STANDARD_21
using Index = Eagle._Constants.Index;
#endif

namespace Eagle._Containers.Private
{
    [ObjectId("2ea84c90-4599-4776-bef4-db5c6b10758d")]
    internal sealed class RegExList : List<Regex>, ICloneable
    {
        public RegExList()
            : base()
        {
            // do nothing.
        }

        ///////////////////////////////////////////////////////////////////////

        public RegExList(IEnumerable<Regex> collection)
            : base(collection)
        {
            // do nothing.
        }

        ///////////////////////////////////////////////////////////////////////

        #region Dead Code
#if DEAD_CODE
        public RegExList(
            IEnumerable<Regex> collection1,
            IEnumerable<Regex> collection2
            )
            : base()
        {
            if (collection1 != null)
                Add(collection1);

            if (collection2 != null)
                Add(collection2);
        }
#endif
        #endregion

        ///////////////////////////////////////////////////////////////////////

        public RegExList(
            IDictionary<Regex, Enum> dictionary1,
            IDictionary<Regex, Enum> dictionary2
            )
            : base()
        {
            if (dictionary1 != null)
                Add(dictionary1);

            if (dictionary2 != null)
                Add(dictionary2);
        }

        ///////////////////////////////////////////////////////////////////////

        public void Add(
            IEnumerable<Regex> collection
            )
        {
            foreach (Regex item in collection)
                base.Add(item);
        }

        ///////////////////////////////////////////////////////////////////////

        public void Add(
            IDictionary<Regex, Enum> dictionary
            )
        {
            Add(dictionary.Keys);
        }

        ///////////////////////////////////////////////////////////////////////

        public string ToString(string pattern, bool noCase)
        {
            return ParserOps<Regex>.ListToString(
                this, Index.Invalid, Index.Invalid, ToStringFlags.None,
                Characters.Space.ToString(), pattern, noCase);
        }

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
            return new RegExList(this);
        }
        #endregion
    }
}
