/*
 * ArraySearchDictionary.cs --
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
    string, Eagle._Components.Private.ArraySearch>;
#else
using SomeDictionary = System.Collections.Generic.Dictionary<
    string, Eagle._Components.Private.ArraySearch>;
#endif

namespace Eagle._Containers.Private
{
    /// <summary>
    /// This class represents a dictionary that maps string names to array
    /// search state objects.
    /// </summary>
    [ObjectId("3f992cdc-cf6c-49c0-82fa-0f91c8ff2113")]
    internal sealed class ArraySearchDictionary : SomeDictionary
    {
        /// <summary>
        /// Constructs an empty instance of this class.
        /// </summary>
        public ArraySearchDictionary()
            : base()
        {
            // do nothing.
        }
    }
}
