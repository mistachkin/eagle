/*
 * CultureInfoDictionary.cs --
 *
 * Copyright (c) 2007-2012 by Joe Mistachkin.  All rights reserved.
 *
 * See the file "license.terms" for information on usage and redistribution of
 * this file, and for a DISCLAIMER OF ALL WARRANTIES.
 *
 * RCS: @(#) $Id: $
 */

using System.Collections.Generic;
using System.Globalization;
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
    [ObjectId("87f76a7b-7e68-4ac9-937a-d225520971b9")]
    internal sealed class CultureInfoDictionary : Dictionary<string, CultureInfo>
    {
        public CultureInfoDictionary()
            : base()
        {
            // do nothing.
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public CultureInfoDictionary(IEnumerable<CultureInfo> collection)
            : base()
        {
            Add(collection);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public void Add(IEnumerable<CultureInfo> collection)
        {
            foreach (CultureInfo item in collection)
                this.Add(item.ToString(), item);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        public string ToString(string pattern, bool noCase)
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
    }
}
