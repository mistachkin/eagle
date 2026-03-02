/*
 * IntArgumentInfoListDictionary.cs --
 *
 * Copyright (c) 2007-2012 by Joe Mistachkin.  All rights reserved.
 *
 * See the file "license.terms" for information on usage and redistribution of
 * this file, and for a DISCLAIMER OF ALL WARRANTIES.
 *
 * RCS: @(#) $Id: $
 */

using System.Collections.Generic;
using Eagle._Attributes;
using Eagle._Components.Private;

#if FAST_DICTIONARY
using SomeDictionary = Eagle._Containers.Public.FastDictionary<
    int, Eagle._Containers.Private.ArgumentInfoList>;
#else
using SomeDictionary = System.Collections.Generic.Dictionary<
    int, Eagle._Containers.Private.ArgumentInfoList>;
#endif

namespace Eagle._Containers.Private
{
    [ObjectId("996a0f63-6389-48d8-840a-3be0de5d3857")]
    internal sealed class IntArgumentInfoListDictionary : SomeDictionary
    {
        public IntArgumentInfoListDictionary()
            : base()
        {
            // do nothing.
        }
    }
}
